#!/usr/bin/env python3
"""
release.py - one-shot release automation for a standalone Unity tool repo.

Given a version and (optionally) a path to the repo, this script does the whole
release by itself:

    1. Sanity-checks the working tree and that the version isn't already tagged.
    2. Reads module.json (if present) and resolves the dependency closure: each
       dependency's latest GitHub release .unitypackage is downloaded, and its
       embedded module.json is read to discover transitive dependencies.
    3. Verifies the tool LOCALLY: scaffolds a throwaway Unity project, copies the
       tool in (plus the downloaded dependencies), makes sure it compiles, runs any
       EditMode tests, and builds a Windows player. (Skipped with --skip-tests.)
    4. Builds a <name>.unitypackage in pure Python (no Unity, no license needed),
       merging the dependency packages in (deduplicated by GUID) so the release is
       fully self-contained.
    5. Pushes the branch, tags the release, and pushes the tag.
    6. Creates the GitHub Release (auto-generated notes) and uploads the package.
    7. Prints the permanent "latest download" URL for the docs.

module.json (at the repo root, committed together with its .meta so it ships
inside the package -- ModuleManager reads it in the buyer's project):

    {
        "name": "HermesEventGenerator",
        "version": "1.1.0",
        "installPath": "Assets/HermesEventGenerator",
        "unity": "6000.1",
        "defines": ["MODULE_HERMES_EXIST"],           # optional
        "buildScene": "Samples/DamageSample/Sample.unity",  # optional, path within the module
        "packages": {                                 # optional: UPM deps for the verify scaffold
            "com.unity.visualscripting": "1.9.4"
        },
        "dependencies": {
            "UnityExtensions": { "repo": "platinio/unity-extensions", "version": "1.0.0" },
            "Zenject":         { "repo": "platinio/Unity-Zenject", "version": "9.2.0", "optional": true }
        }
    }

Field notes:
  version      must match the version being released (enforced).
  defines      optional; ModuleManager unions the defines of PRESENT modules.
  buildScene   optional; the scene the pre-release Windows build builds (so the
               build exercises real content). Empty scene if omitted.
  packages     optional; UPM packages the module needs, added to the verify
               scaffold's manifest (NOT bundled -- buyers get them from the registry).
  dependencies fetched from each repo's LATEST release and bundled into the package.
               "version" is the minimum the module needs (ModuleManager warnings).
               "optional": true  -> NOT bundled (integration behind a define-constrained
               assembly, e.g. Zenject); the module compiles/works without it.

Local verification (step 2) needs a Unity install matching the project version.
The script auto-detects Unity from the standard Unity Hub location; override with
--unity or the UNITY_PATH env var. Dependencies for non-self-contained tools will
later come from a per-repo module.json; for now the scaffold is minimal.

Requirements: Python 3.9+, git on PATH, a Unity install (for step 2), and a GitHub
token in GITHUB_TOKEN (or GH_TOKEN) with `repo` + `workflow` scope.

Usage (from inside the repo):
    GITHUB_TOKEN=xxxx  python release.py 1.1

Usage (pointing at a repo):
    python release.py 1.1 --repo /path/to/tool --name MGizmos

The .unitypackage format is just a gzip-compressed tar: one folder per asset,
named by the asset's GUID (read from its .meta), containing `asset`,
`asset.meta`, and `pathname`. That's why packing needs no Unity install.
"""

import argparse
import gzip
import io
import json
import os
import re
import shutil
import subprocess
import sys
import tarfile
import tempfile
import time
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET

API = "https://api.github.com"
GUID_RE = re.compile(rb"^guid:\s*([0-9a-fA-F]+)", re.MULTILINE)
DEFAULT_UNITY_VERSION = "6000.1.15f1"
DEFAULT_TF_VERSION = "1.4.6"  # com.unity.test-framework fallback


# --------------------------------------------------------------------------- #
# small helpers
# --------------------------------------------------------------------------- #
def fail(msg):
    print(f"\n[release] ERROR: {msg}", file=sys.stderr)
    sys.exit(1)


def info(msg):
    print(f"[release] {msg}")


def git(repo, *args, check=True):
    result = subprocess.run(
        ["git", "-C", repo, *args], capture_output=True, text=True
    )
    if check and result.returncode != 0:
        fail(f"git {' '.join(args)} failed:\n{result.stderr or result.stdout}")
    return (result.stdout or "").strip()


def token(repo):
    """GitHub token: env var first, then a gitignored .release_token file in the repo."""
    tok = os.environ.get("GITHUB_TOKEN") or os.environ.get("GH_TOKEN")
    if not tok:
        f = os.path.join(repo, ".release_token")
        if os.path.isfile(f):
            tok = open(f).read().strip()
    if not tok:
        fail(
            "no GitHub token found. Set GITHUB_TOKEN (or GH_TOKEN), or put the token "
            "in a gitignored .release_token file in the repo. Needs Contents: write."
        )
    return tok


def api(method, url, data=None, tok=None, headers=None, raw=False):
    hdrs = {
        "Accept": "application/vnd.github+json",
        "X-GitHub-Api-Version": "2022-11-28",
        "User-Agent": "unity-tool-release-script",
    }
    if tok:
        hdrs["Authorization"] = f"Bearer {tok}"
    if headers:
        hdrs.update(headers)
    body = None
    if data is not None and not raw:
        body = json.dumps(data).encode()
        hdrs["Content-Type"] = "application/json"
    elif raw:
        body = data
    req = urllib.request.Request(url, data=body, method=method, headers=hdrs)
    try:
        with urllib.request.urlopen(req) as resp:
            payload = resp.read()
            return json.loads(payload) if payload else {}
    except urllib.error.HTTPError as e:
        fail(f"{method} {url} -> HTTP {e.code}\n{e.read().decode(errors='replace')}")


def owner_repo(repo):
    url = git(repo, "remote", "get-url", "origin")
    m = re.search(r"github\.com[:/]+([^/]+)/(.+?)(?:\.git)?/?$", url)
    if not m:
        fail(f"could not parse a github owner/repo from origin url: {url}")
    return m.group(1), m.group(2)


# --------------------------------------------------------------------------- #
# module.json + dependency closure
# --------------------------------------------------------------------------- #
def load_manifest(repo):
    """The module's own module.json, or None if it doesn't have one."""
    path = os.path.join(repo, "module.json")
    if not os.path.isfile(path):
        return None
    manifest = json.load(open(path, encoding="utf-8-sig"))
    if not git(repo, "ls-files", "module.json.meta"):
        fail("module.json exists but module.json.meta is not tracked by git. The "
             "manifest must ship inside the package (ModuleManager reads it), and "
             "the packer only packs files whose .meta is committed.")
    return manifest


def download_release_package(slug, tok):
    """Download the .unitypackage asset of `slug`'s latest GitHub release.

    Returns (tag, raw_tar_bytes). The asset is matched by extension, not by
    name, so older releases with versioned filenames still work.
    """
    rel = api("GET", f"{API}/repos/{slug}/releases/latest", tok=tok)
    asset = next((a for a in rel.get("assets", [])
                  if a["name"].endswith(".unitypackage")), None)
    if not asset:
        fail(f"latest release of {slug} ({rel.get('tag_name')}) has no "
             f".unitypackage asset. Release that module first.")
    # public asset download; deliberately unauthenticated so the S3 redirect
    # doesn't choke on a forwarded Authorization header
    req = urllib.request.Request(asset["browser_download_url"],
                                 headers={"User-Agent": "unity-tool-release-script"})
    with urllib.request.urlopen(req) as resp:
        data = resp.read()
    return rel["tag_name"], gzip.decompress(data)


def package_groups(raw):
    """Group a raw unitypackage tar's members by asset GUID."""
    tar = tarfile.open(fileobj=io.BytesIO(raw))
    groups = {}
    for member in tar.getmembers():
        name = member.name[2:] if member.name.startswith("./") else member.name
        guid, _, part = name.partition("/")
        if guid and part and member.isfile():
            groups.setdefault(guid, {})[part] = member
    return tar, groups


def package_manifest(raw):
    """The module's own module.json inside a package (shallowest one), or None.

    A bundled package can contain the manifests of its own bundled dependencies
    too; the module's own manifest is the one closest to its install root.
    """
    tar, groups = package_groups(raw)
    best = None
    for parts in groups.values():
        if "pathname" not in parts or "asset" not in parts:
            continue
        pathname = tar.extractfile(parts["pathname"]).read().decode().splitlines()[0]
        if os.path.basename(pathname) == "module.json":
            if best is None or pathname.count("/") < best[0].count("/"):
                best = (pathname, parts["asset"])
    if best is None:
        return None
    return json.loads(tar.extractfile(best[1]).read().decode("utf-8-sig"))


def resolve_dependency_closure(manifest, tok):
    """BFS the dependency graph, downloading each REQUIRED dependency's latest release.

    Returns a list of {name, repo, tag, raw} dicts, deduplicated by repo. Each
    downloaded package's embedded module.json supplies its own dependencies;
    releases that predate module.json are treated as leaves.

    Dependencies marked "optional": true are NOT bundled -- they're integrations
    (e.g. Zenject) whose code lives behind a define-constrained assembly, so the
    module works without them. ModuleManager still sees them in module.json.
    """
    packages, visited = [], set()
    queue = list((manifest or {}).get("dependencies", {}).items())
    while queue:
        name, spec = queue.pop(0)
        if isinstance(spec, dict) and spec.get("optional"):
            info(f"skipping optional dependency {name} (not bundled)")
            continue
        slug = spec["repo"] if isinstance(spec, dict) else spec
        if slug.lower() in visited:
            continue
        visited.add(slug.lower())
        info(f"fetching dependency {name} ({slug}) ...")
        tag, raw = download_release_package(slug, tok)
        info(f"  got {name} {tag}")
        sub = package_manifest(raw)
        if sub:
            queue.extend(sub.get("dependencies", {}).items())
        else:
            info(f"  ({name} release has no module.json; treating as leaf)")
        packages.append({"name": name, "repo": slug, "tag": tag, "raw": raw})
    return packages


# --------------------------------------------------------------------------- #
# environment discovery (parent project -> Unity version / test-framework)
# --------------------------------------------------------------------------- #
def find_parent_project(start):
    """Walk up looking for a real Unity project (ProjectSettings/ProjectVersion.txt)."""
    cur = os.path.abspath(start)
    while True:
        if os.path.isfile(os.path.join(cur, "ProjectSettings", "ProjectVersion.txt")):
            return cur
        parent = os.path.dirname(cur)
        if parent == cur:
            return None
        cur = parent


def detect_unity_version(parent, override):
    if override:
        return override
    if parent:
        txt = open(os.path.join(parent, "ProjectSettings", "ProjectVersion.txt")).read()
        m = re.search(r"m_EditorVersion:\s*(\S+)", txt)
        if m:
            return m.group(1)
    return DEFAULT_UNITY_VERSION


def detect_tf_version(parent):
    if parent:
        mf = os.path.join(parent, "Packages", "manifest.json")
        if os.path.isfile(mf):
            deps = json.load(open(mf)).get("dependencies", {})
            if "com.unity.test-framework" in deps:
                return deps["com.unity.test-framework"]
    return DEFAULT_TF_VERSION


# Built-in engine modules to fall back on when there's no parent project to copy
# them from (e.g. the tool repo cloned standalone in CI).
FALLBACK_MODULES = [
    "ai", "androidjni", "animation", "assetbundle", "audio", "cloth",
    "director", "imageconversion", "imgui", "jsonserialize", "particlesystem",
    "physics", "physics2d", "screencapture", "terrain", "terrainphysics",
    "tilemap", "ui", "uielements", "umbra", "unityanalytics", "unitywebrequest",
    "unitywebrequestassetbundle", "unitywebrequestaudio", "unitywebrequesttexture",
    "unitywebrequestwww", "vehicles", "video", "wind", "xr",
]


def scaffold_deps(parent, tf_version):
    """manifest dependencies for the throwaway project: engine modules + test framework."""
    deps = {"com.unity.test-framework": tf_version}
    if parent:
        mf = os.path.join(parent, "Packages", "manifest.json")
        if os.path.isfile(mf):
            for k, v in json.load(open(mf)).get("dependencies", {}).items():
                if k.startswith("com.unity.modules."):
                    deps[k] = v
    if not any(k.startswith("com.unity.modules.") for k in deps):
        for m in FALLBACK_MODULES:
            deps[f"com.unity.modules.{m}"] = "1.0.0"
    return deps


def find_unity(version, override):
    """Locate Unity.exe/binary for `version`. Returns path or None."""
    if override:
        return override if os.path.exists(override) else None
    if os.environ.get("UNITY_PATH") and os.path.exists(os.environ["UNITY_PATH"]):
        return os.environ["UNITY_PATH"]
    candidates = []
    if sys.platform.startswith("win"):
        for base in (r"C:\Program Files\Unity\Hub\Editor",
                     r"C:\Program Files\Unity Hub\Editor"):
            candidates.append(os.path.join(base, version, "Editor", "Unity.exe"))
    elif sys.platform == "darwin":
        candidates.append(f"/Applications/Unity/Hub/Editor/{version}/Unity.app/Contents/MacOS/Unity")
    else:
        candidates.append(os.path.expanduser(f"~/Unity/Hub/Editor/{version}/Editor/Unity"))
    return next((c for c in candidates if os.path.exists(c)), None)


# --------------------------------------------------------------------------- #
# scaffold throwaway project + copy tool content
# --------------------------------------------------------------------------- #
CI_BUILD_CS = r"""
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Injected by release.py: builds a StandaloneWindows64 player so we know the
// tool's runtime code compiles for a real player, not just the editor. If
// release.py passes "-ciScene <path>" (from module.json's buildScene), that
// scene is built; otherwise a throwaway empty scene is used.
public static class CIBuild
{
    private static string CiSceneArg()
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "-ciScene") return args[i + 1];
        return null;
    }

    public static void BuildWindows()
    {
        try
        {
            string sceneArg = CiSceneArg();
            string[] scenes;
            if (!string.IsNullOrEmpty(sceneArg))
            {
                if (!File.Exists(sceneArg))
                {
                    Debug.LogError($"[CIBuild] buildScene not found: {sceneArg}");
                    EditorApplication.Exit(1);
                    return;
                }
                scenes = new[] { sceneArg };
            }
            else
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled)
                                            .Select(s => s.path).ToArray();
            }
            if (scenes.Length == 0)
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                                                        NewSceneMode.Single);
                var p = "Assets/CI/_ci_empty.unity";
                EditorSceneManager.SaveScene(scene, p);
                scenes = new[] { p };
            }
            var outDir = Path.Combine(Path.GetTempPath(), "ci_build_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(outDir);
            var opts = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outDir, "player.exe"),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            Debug.Log($"[CIBuild] result={s.result} errors={s.totalErrors} size={s.totalSize}");
            try { Directory.Delete(outDir, true); } catch {}
            EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }
        catch (Exception e)
        {
            Debug.LogError("[CIBuild] exception: " + e);
            EditorApplication.Exit(1);
        }
    }
}
"""


def scaffold_project(repo, name, unity_version, deps):
    """Create a temp Unity project containing the tracked tool content."""
    proj = tempfile.mkdtemp(prefix=f"ci_{name}_")
    os.makedirs(os.path.join(proj, "ProjectSettings"))
    os.makedirs(os.path.join(proj, "Packages"))
    with open(os.path.join(proj, "ProjectSettings", "ProjectVersion.txt"), "w") as fh:
        fh.write(f"m_EditorVersion: {unity_version}\n")
    with open(os.path.join(proj, "Packages", "manifest.json"), "w") as fh:
        json.dump({"dependencies": deps}, fh, indent=2)

    # copy every tracked file into Assets/<name>/, preserving structure
    dest_root = os.path.join(proj, "Assets", name)
    for rel in git(repo, "ls-files").splitlines():
        src = os.path.join(repo, rel)
        if not os.path.isfile(src):
            continue
        dst = os.path.join(dest_root, rel)
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        shutil.copy2(src, dst)

    # inject the CI build script
    ci_dir = os.path.join(proj, "Assets", "CI", "Editor")
    os.makedirs(ci_dir)
    with open(os.path.join(ci_dir, "CIBuild.cs"), "w") as fh:
        fh.write(CI_BUILD_CS)
    return proj


def run_unity(unity_exe, project, extra_args, log_path, timeout):
    args = [unity_exe, "-batchmode", "-nographics", "-projectPath", project,
            "-logFile", log_path, *extra_args]
    proc = subprocess.run(args, timeout=timeout)
    log = ""
    if os.path.isfile(log_path):
        log = open(log_path, errors="replace").read()
    cs_errors = [ln for ln in log.splitlines() if ": error CS" in ln]
    return proc.returncode, log, cs_errors


def parse_test_results(xml_path):
    if not os.path.isfile(xml_path):
        return None
    root = ET.parse(xml_path).getroot()  # <test-run ...>
    total = int(root.get("total", 0))
    passed = int(root.get("passed", 0))
    failed = int(root.get("failed", 0))
    skipped = int(root.get("skipped", 0))
    failures = [tc.get("fullname") for tc in root.iter("test-case")
                if tc.get("result") == "Failed"]
    return total, passed, failed, skipped, failures


def verify_locally(repo, name, unity_version, deps, unity_exe, platforms, keep,
                   dep_packages=(), build_scene=None):
    info(f"scaffolding throwaway Unity {unity_version} project ({len(deps)} deps) ...")
    proj = scaffold_project(repo, name, unity_version, deps)
    for dep in dep_packages:
        n = extract_package_into_project(dep["raw"], proj)
        info(f"  added dependency {dep['name']} {dep['tag']} ({n} assets)")
    try:
        # --- tests + compile check (per platform) ---
        for platform in platforms:
            res_xml = os.path.join(proj, f"results-{platform}.xml")
            log = os.path.join(proj, f"tests-{platform}.log")
            info(f"running {platform} tests (compiles the project) ...")
            rc, logtxt, cs = run_unity(
                unity_exe, proj,
                ["-runTests", "-testPlatform", platform, "-testResults", res_xml],
                log, timeout=2400,
            )
            if cs:
                fail("compilation failed:\n  " + "\n  ".join(cs[:25]))
            # -runTests: 0 = all passed / no tests, 2 = some failed, else = error
            if rc not in (0, 2):
                fail(f"Unity exited {rc} during {platform} tests. Log tail:\n"
                     + "\n".join(logtxt.splitlines()[-25:]))
            parsed = parse_test_results(res_xml)
            if parsed is None:
                info(f"  {platform}: compiled OK, no test results produced (no tests).")
            else:
                total, passed, failed, skipped, failures = parsed
                info(f"  {platform}: {passed}/{total} passed, {failed} failed, {skipped} skipped.")
                if failed:
                    fail(f"{platform} tests failed:\n  " + "\n  ".join(failures[:25]))

        # --- Windows player build ---
        build_args = ["-quit", "-executeMethod", "CIBuild.BuildWindows"]
        if build_scene:
            scene_path = f"Assets/{name}/{build_scene}"
            info(f"building StandaloneWindows64 player with scene {scene_path} ...")
            build_args += ["-ciScene", scene_path]
        else:
            info("building StandaloneWindows64 player (empty scene) ...")
        blog = os.path.join(proj, "build.log")
        rc, logtxt, cs = run_unity(unity_exe, proj, build_args, blog, timeout=2400)
        if cs:
            fail("player build failed to compile:\n  " + "\n  ".join(cs[:25]))
        result_line = next((ln for ln in logtxt.splitlines() if "[CIBuild] result=" in ln), "")
        if rc != 0:
            fail(f"Windows build failed (exit {rc}). {result_line}\nLog tail:\n"
                 + "\n".join(logtxt.splitlines()[-25:]))
        info(f"  build OK. {result_line.strip()}")
    finally:
        if keep:
            info(f"keeping scaffold project at: {proj}")
        else:
            shutil.rmtree(proj, ignore_errors=True)


# --------------------------------------------------------------------------- #
# unitypackage builder
# --------------------------------------------------------------------------- #
def build_unitypackage(repo, install_path, out_file, exclude_prefixes, dep_packages=()):
    tracked = git(repo, "ls-files").splitlines()
    metas = [f for f in tracked if f.endswith(".meta")]
    added, skipped = 0, 0
    seen_guids = set()
    buf = io.BytesIO()
    with tarfile.open(fileobj=buf, mode="w") as tar:
        mtime = int(time.time())
        for meta_rel in sorted(metas):
            asset_rel = meta_rel[:-5]
            if any(asset_rel == p or asset_rel.startswith(p + "/") for p in exclude_prefixes):
                continue
            abs_meta = os.path.join(repo, meta_rel)
            abs_asset = os.path.join(repo, asset_rel)
            meta_bytes = open(abs_meta, "rb").read()
            m = GUID_RE.search(meta_bytes)
            if not m:
                info(f"  skip (no guid in meta): {meta_rel}")
                skipped += 1
                continue
            guid = m.group(1).decode()
            pathname = f"{install_path}/{asset_rel}".replace("\\", "/")

            def add(part, data):
                ti = tarfile.TarInfo(f"{guid}/{part}")
                ti.size = len(data)
                ti.mtime = mtime
                ti.mode = 0o644
                tar.addfile(ti, io.BytesIO(data))

            add("pathname", pathname.encode())
            add("asset.meta", meta_bytes)
            if os.path.isfile(abs_asset):
                add("asset", open(abs_asset, "rb").read())
            elif not os.path.isdir(abs_asset):
                info(f"  skip (orphan meta): {meta_rel}")
                skipped += 1
                continue
            seen_guids.add(guid)
            added += 1

        # merge dependency packages, deduplicated by GUID (the module's own
        # assets win; overlapping deps-of-deps collapse to a single copy)
        for dep in dep_packages:
            dep_tar, groups = package_groups(dep["raw"])
            merged = 0
            for dguid, parts in groups.items():
                if dguid in seen_guids:
                    continue
                seen_guids.add(dguid)
                for part, member in parts.items():
                    data = dep_tar.extractfile(member).read() if member.isfile() else b""
                    ti = tarfile.TarInfo(f"{dguid}/{part}")
                    ti.size = len(data)
                    ti.mtime = mtime
                    ti.mode = 0o644
                    tar.addfile(ti, io.BytesIO(data))
                merged += 1
            info(f"  bundled {dep['name']} {dep['tag']}: {merged} assets")

    os.makedirs(os.path.dirname(out_file), exist_ok=True)
    with open(out_file, "wb") as fh:
        fh.write(gzip.compress(buf.getvalue()))
    info(f"packed {added} own assets + {len(dep_packages)} bundled deps -> {out_file}")


def extract_package_into_project(raw, proj):
    """Unpack a .unitypackage's assets into a scaffold project at their pathnames."""
    tar, groups = package_groups(raw)
    count = 0
    for parts in groups.values():
        if "pathname" not in parts:
            continue
        pathname = tar.extractfile(parts["pathname"]).read().decode().splitlines()[0]
        dest = os.path.join(proj, *pathname.split("/"))
        if "asset" in parts:
            os.makedirs(os.path.dirname(dest), exist_ok=True)
            with open(dest, "wb") as fh:
                fh.write(tar.extractfile(parts["asset"]).read())
        else:
            os.makedirs(dest, exist_ok=True)
        if "asset.meta" in parts:
            with open(dest + ".meta", "wb") as fh:
                fh.write(tar.extractfile(parts["asset.meta"]).read())
        count += 1
    return count


# --------------------------------------------------------------------------- #
# main
# --------------------------------------------------------------------------- #
def main():
    ap = argparse.ArgumentParser(description="Release a standalone Unity tool repo.")
    ap.add_argument("version", help="version / tag to release, e.g. 1.1")
    ap.add_argument("--repo", default=".", help="path to the tool repo (default: cwd)")
    ap.add_argument("--name", help="package + install-folder name (default: repo folder name)")
    ap.add_argument("--install-path", help="import path inside a project (default: Assets/<name>)")
    ap.add_argument("--tag", help="git tag to use (default: the version verbatim)")
    ap.add_argument("--exclude", nargs="*", default=["ReadmeResources"],
                    help="path prefixes to keep OUT of the package")
    ap.add_argument("--unity", help="path to Unity.exe (default: auto-detect from version)")
    ap.add_argument("--unity-version", help="Unity version to test with (default: parent project's)")
    ap.add_argument("--test-platforms", nargs="+", default=["EditMode"],
                    help="test platforms to run (e.g. EditMode PlayMode)")
    ap.add_argument("--keep-test-project", action="store_true",
                    help="don't delete the scaffold project (for debugging)")
    ap.add_argument("--skip-tests", action="store_true", help="skip local Unity verify entirely")
    ap.add_argument("--allow-dirty", action="store_true", help="pack despite uncommitted changes")
    ap.add_argument("--draft", action="store_true", help="create the release as a draft")
    ap.add_argument("--prerelease", action="store_true", help="mark the release as a prerelease")
    args = ap.parse_args()

    repo = os.path.abspath(args.repo)
    if not os.path.exists(os.path.join(repo, ".git")):
        fail(f"{repo} is not a git repo.")
    manifest = load_manifest(repo)
    if manifest and manifest.get("version") != args.version:
        fail(f"module.json says version {manifest.get('version')!r} but you are "
             f"releasing {args.version!r}. Update module.json (and commit) first -- "
             f"ModuleManager reads the installed version from it.")
    name = args.name or (manifest or {}).get("name") or os.path.basename(repo.rstrip("/\\"))
    install_path = args.install_path or (manifest or {}).get("installPath") or f"Assets/{name}"
    tag = args.tag or args.version
    out_file = os.path.join(repo, "dist", f"{name}.unitypackage")
    tok = token(repo)
    owner, gh_name = owner_repo(repo)
    # Resolve the canonical owner/repo (follows GitHub repo renames) and validate
    # the token up front, so we never push a tag and then fail on the release call.
    gh = api("GET", f"{API}/repos/{owner}/{gh_name}", tok=tok)
    owner, gh_name = gh["full_name"].split("/", 1)
    info(f"repo={owner}/{gh_name}  name={name}  tag={tag}  install={install_path}")

    # 1. guards -------------------------------------------------------------
    if not args.allow_dirty and git(repo, "status", "--porcelain"):
        fail("working tree is dirty. Commit/stash first, or pass --allow-dirty.")
    if tag in git(repo, "tag").splitlines():
        fail(f"tag '{tag}' already exists.")
    branch = git(repo, "rev-parse", "--abbrev-ref", "HEAD")

    # 2. resolve + download the dependency closure ---------------------------
    dep_packages = resolve_dependency_closure(manifest, tok)

    # 3. local Unity verify (compile + tests + Windows build) ---------------
    if args.skip_tests:
        info("skipping local Unity verify (--skip-tests).")
    else:
        parent = find_parent_project(repo)
        unity_version = detect_unity_version(parent, args.unity_version)
        deps = scaffold_deps(parent, detect_tf_version(parent))
        deps.update((manifest or {}).get("packages", {}))  # UPM deps for the scaffold
        unity_exe = find_unity(unity_version, args.unity)
        if not unity_exe:
            fail(f"could not find Unity {unity_version}. Pass --unity <path> or set "
                 f"UNITY_PATH, or --skip-tests to bypass.")
        verify_locally(repo, name, unity_version, deps, unity_exe,
                       args.test_platforms, args.keep_test_project,
                       dep_packages=dep_packages,
                       build_scene=(manifest or {}).get("buildScene"))

    # 4. pack (own assets + bundled dependency closure) ----------------------
    build_unitypackage(repo, install_path, out_file, args.exclude,
                       dep_packages=dep_packages)

    # 5. push branch + tag --------------------------------------------------
    info(f"pushing branch '{branch}' ...")
    git(repo, "push", "origin", branch)
    info(f"tagging {tag} ...")
    git(repo, "tag", "-a", tag, "-m", f"Release {tag}")
    git(repo, "push", "origin", tag)

    # 5. create release + upload asset --------------------------------------
    info("creating GitHub release ...")
    rel = api("POST", f"{API}/repos/{owner}/{gh_name}/releases", tok=tok, data={
        "tag_name": tag,
        "name": tag,
        "generate_release_notes": True,
        "draft": args.draft,
        "prerelease": args.prerelease,
    })
    upload_url = rel["upload_url"].split("{")[0]
    asset_name = f"{name}.unitypackage"
    info("uploading .unitypackage ...")
    with open(out_file, "rb") as fh:
        api("POST", f"{upload_url}?name={asset_name}", tok=tok, raw=True,
            data=fh.read(), headers={"Content-Type": "application/octet-stream"})

    # 6. done ---------------------------------------------------------------
    info("done.")
    print(f"\n  Release : {rel['html_url']}")
    print(f"  Package : {asset_name}")
    print(f"  Docs link (always newest):")
    print(f"    https://github.com/{owner}/{gh_name}/releases/latest/download/{asset_name}\n")


if __name__ == "__main__":
    main()
