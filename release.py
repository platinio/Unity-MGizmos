#!/usr/bin/env python3
"""
release.py - one-shot release automation for a standalone Unity tool repo.

Given a version and (optionally) a path to the repo, this script does the whole
release by itself:

    1. Sanity-checks the working tree and that the version isn't already tagged.
    2. Verifies the tool LOCALLY: scaffolds a throwaway Unity project, copies the
       tool in, makes sure it compiles, runs any EditMode tests, and builds a
       Windows player. Any failure aborts the release. (Skipped with --skip-tests.)
    3. Builds a <name>.unitypackage in pure Python (no Unity, no license needed).
    4. Pushes the branch, tags the release, and pushes the tag.
    5. Creates the GitHub Release (auto-generated notes) and uploads the package.
    6. Prints the permanent "latest download" URL for the docs.

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
        "Authorization": f"Bearer {tok}",
        "Accept": "application/vnd.github+json",
        "X-GitHub-Api-Version": "2022-11-28",
        "User-Agent": "unity-tool-release-script",
    }
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
// tool's runtime code compiles for a real player, not just the editor.
public static class CIBuild
{
    public static void BuildWindows()
    {
        try
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled)
                                            .Select(s => s.path).ToArray();
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


def verify_locally(repo, name, unity_version, deps, unity_exe, platforms, keep):
    info(f"scaffolding throwaway Unity {unity_version} project ({len(deps)} deps) ...")
    proj = scaffold_project(repo, name, unity_version, deps)
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
        info("building StandaloneWindows64 player ...")
        blog = os.path.join(proj, "build.log")
        rc, logtxt, cs = run_unity(
            unity_exe, proj,
            ["-quit", "-executeMethod", "CIBuild.BuildWindows"],
            blog, timeout=2400,
        )
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
def build_unitypackage(repo, install_path, out_file, exclude_prefixes):
    tracked = git(repo, "ls-files").splitlines()
    metas = [f for f in tracked if f.endswith(".meta")]
    added, skipped = 0, 0
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
            added += 1
    os.makedirs(os.path.dirname(out_file), exist_ok=True)
    with open(out_file, "wb") as fh:
        fh.write(gzip.compress(buf.getvalue()))
    info(f"packed {added} assets ({skipped} skipped) -> {out_file}")


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
    name = args.name or os.path.basename(repo.rstrip("/\\"))
    install_path = args.install_path or f"Assets/{name}"
    tag = args.tag or args.version
    out_file = os.path.join(repo, "dist", f"{name}.unitypackage")
    tok = token(repo)
    owner, gh_name = owner_repo(repo)
    info(f"repo={owner}/{gh_name}  name={name}  tag={tag}  install={install_path}")

    # 1. guards -------------------------------------------------------------
    if not args.allow_dirty and git(repo, "status", "--porcelain"):
        fail("working tree is dirty. Commit/stash first, or pass --allow-dirty.")
    if tag in git(repo, "tag").splitlines():
        fail(f"tag '{tag}' already exists.")
    branch = git(repo, "rev-parse", "--abbrev-ref", "HEAD")

    # 2. local Unity verify (compile + tests + Windows build) ---------------
    if args.skip_tests:
        info("skipping local Unity verify (--skip-tests).")
    else:
        parent = find_parent_project(repo)
        unity_version = detect_unity_version(parent, args.unity_version)
        deps = scaffold_deps(parent, detect_tf_version(parent))
        unity_exe = find_unity(unity_version, args.unity)
        if not unity_exe:
            fail(f"could not find Unity {unity_version}. Pass --unity <path> or set "
                 f"UNITY_PATH, or --skip-tests to bypass.")
        verify_locally(repo, name, unity_version, deps, unity_exe,
                       args.test_platforms, args.keep_test_project)

    # 3. pack ---------------------------------------------------------------
    build_unitypackage(repo, install_path, out_file, args.exclude)

    # 4. push branch + tag --------------------------------------------------
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
