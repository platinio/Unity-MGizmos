#!/usr/bin/env python3
"""
release.py - one-shot release automation for a standalone Unity tool/module repo.

Run it from inside a module repo with a version number and it does the whole GitHub
release by itself:

    1. Guards: refuses on a dirty working tree or an existing tag, and requires
       module.json's "version" to equal the version you are releasing.
    2. Resolves the dependency closure from module.json: downloads each REQUIRED
       dependency's LATEST release .unitypackage, reads the module.json embedded in
       it to discover transitive deps, and recurses. Optional deps are skipped.
    3. If module.json declares a "sample", packs it into a separate
       <name>.Samples.unitypackage (--sample separate, the default) or folds it into
       the core package (--sample bundled, the default with --asset-store, whose
       listing holds one package). A Samples~/ folder has the `~` stripped so it
       installs as an importable Samples/; a plain folder ships as it is.
    4. Verifies LOCALLY in a throwaway Unity project (skip with --skip-tests):
       the module's committed source + the downloaded dependency packages + the
       sample package (imported, so the sample is compiled too) + the UPM "packages".
       Compiles, runs any EditMode tests, and builds a Windows player (of "buildScene"
       if set, else an empty scene). ANY failure aborts before anything is pushed.
    5. Packs the core <name>.unitypackage in pure Python (no Unity/license needed),
       merging the required dependency packages in (deduplicated by GUID) so the core
       release is self-contained.
    6. Pushes the branch, creates + pushes the tag, creates the GitHub Release (with
       auto-generated notes), and uploads the core package (+ the sample package).
    7. Prints the permanent "releases/latest/download/..." URLs for your docs.

With --asset-store, steps 1-5 are the same and 6-7 are replaced: nothing is pushed or
tagged, and the core package is uploaded to the module's DRAFT on the Unity Asset
Store instead (see "ASSET STORE" below).

--------------------------------------------------------------------------------
USAGE
--------------------------------------------------------------------------------
From inside the module repo (most common):

    python release.py 1.2.0

Point at another repo / override the derived name:

    python release.py 1.2.0 --repo /path/to/module --name MyModule

Handy flags:
    --skip-tests          skip the local Unity verify (faster; when already confirmed)
    --exclude A B         extra path prefixes to keep OUT of the core package
                          (default: ReadmeResources; the "sample" path is auto-excluded
                          unless --sample bundled, and so is everything in module.json's
                          "exclude")
    --sample separate|bundled
                          ship module.json's "sample" as its own package, or inside the
                          core (default: separate; bundled with --asset-store)
    --draft/--prerelease  make the GitHub release a draft / prerelease
    --asset-store         upload to the Asset Store draft instead of releasing on GitHub
    --asset-store-list    print the publisher's Asset Store packages and exit
    --unity <path>        explicit Unity.exe (else auto-detected from the Unity Hub for
                          the project's version, or set UNITY_PATH)
    --test-platforms EditMode PlayMode
    --keep-test-project   keep the throwaway verify project on disk (to debug a failure)

REQUIREMENTS
    Python 3.9+, git on PATH, a Unity install matching the project (for the verify),
    and a GitHub token with Contents:write. The token is read from $GITHUB_TOKEN /
    $GH_TOKEN, or from a gitignored `.release_token` file in the repo. The `gh` CLI is
    NOT needed -- this talks to the GitHub REST API directly.

--------------------------------------------------------------------------------
module.json  --  how to create one for a new module
--------------------------------------------------------------------------------
Put module.json at the repo ROOT and COMMIT it together with module.json.meta: it
ships inside the package so ModuleManager can read it in the buyer's project, and the
packer only packs files whose .meta is committed. Also gitignore the Unity-generated
metas for the tooling files: release.py.meta, dist.meta, __pycache__.meta (otherwise
release.py itself would get packed). Only "name" + "version" are required.

    {
        "name": "MyModule",                     // module id + default install folder
        "version": "1.0.0",                     // MUST equal the released version (enforced)
        "installPath": "Assets/MyModule",       // where the package lands on import
        "unity": "6000.1",                      // informational / ModuleManager

        "packages": {                           // optional: UPM deps the CORE needs; added to
            "com.unity.nuget.newtonsoft-json": "3.2.1"  // the verify scaffold's manifest. NOT
        },                                      // bundled -- buyers get them from the registry.

        "defines": ["MODULE_MYMODULE_EXIST"],   // optional; a define this module declares
                                                // (see "optional dependencies" below)

        "buildScene": "Samples/Demo/Demo.unity",// optional; the scene the verify's Windows
                                                // build builds (post ~-strip path for a
                                                // sample scene). Empty scene if omitted.

        "sample": {                             // optional; a sample folder (Samples~/ or a plain
            "path": "Samples~",                 // Samples/) shipped as a SEPARATE <name>.Samples
            "packages": {                       // .unitypackage (a `~` is stripped). Excluded
                "com.unity.ugui": "2.0.0",      // from the core, and compile-tested by importing
                "com.unity.inputsystem": "1.14.0"// it into the verify. "packages" = extra UPM
            }                                   // deps the SAMPLE needs. Shorthand: "sample":
        },                                      // "Samples~" (no extra packages).

        "exclude": ["docs"],                    // optional; path prefixes (repo-relative)
                                                // kept OUT of the package and of the verify
                                                // scaffold, e.g. a docs folder that is only
                                                // read on GitHub. Added to --exclude.

        "assetStore": {                         // optional; the Asset Store listing that
            "package": "My Module"              // --asset-store uploads to: its name or its
        },                                      // package id. Shorthand: "assetStore": "My Module".

        "dependencyRoot": "Assets/ArcaneOnyx/Modules", // optional; every BUNDLED module is
                                                // relocated to <root>/<Name>, whatever its
                                                // own installPath says (see "layout" below).

        "dependencies": {                       // other MODULES this one needs
            "OtherModule": {
                "repo": "platinio/Unity-OtherModule", // fetched from its LATEST release and
                "version": "1.0.0",                   // BUNDLED (deduped). "version" = the
                "installPath": "Assets/Vendor/Other"  // minimum needed (ModuleManager warnings).
            },                                        // "installPath" = optional per-dependency
            "Zenject": {                              // override of dependencyRoot.
                "repo": "platinio/Unity-Zenject",
                "version": "9.2.0",
                "optional": true                // NOT fetched/bundled; the module compiles and
            }                                   // works without it (see below).
        }
    }

A leaf module just omits "dependencies".

LAYOUT (where bundled dependencies land):
  A module's own "installPath" is where it lands when imported on its own. A module
  that BUNDLES it may put it somewhere else: "dependencyRoot" relocates every bundled
  module (transitive ones included) to <root>/<Name>, and a dependency entry's own
  "installPath" overrides that for one module. The relocation rewrites the pathnames
  inside the downloaded package and the "installPath" in its embedded module.json,
  and the verify scaffold uses the same layout, so what is tested is what ships. Two
  routes to the same module (listed directly and nested inside another dependency)
  collapse to one copy because the paths agree and the packer dedupes by GUID.

OPTIONAL DEPENDENCIES (make an integration optional):
  1. Gate the integration code behind `#if MODULE_<DEP>_EXIST`.
  2. Keep the asmdef reference to the dep -- a missing asmdef reference is a non-fatal
     WARNING as long as the code using those types is #if'd out.
  3. Have the DEPENDENCY module ship an [InitializeOnLoad] editor script that adds
     MODULE_<DEP>_EXIST to the scripting define symbols when it is present (so the
     define is set exactly when the module is there).
  4. Mark it "optional": true here so it is not fetched/bundled.

ASSET STORE (--asset-store):
  Packs and verifies exactly as a GitHub release does, then uploads the core package
  to the listing named by module.json's "assetStore" (or --asset-store-package). It
  does NOT create the listing, edit its metadata, or submit it for review: the
  listing must already have a DRAFT version in the Publisher Portal, and Submit is
  still pressed there. A listing holds one package, so the "sample" is folded into the
  core by default (--sample bundled); with --sample separate it is packed but not uploaded.

  The upload goes through Unity's own Asset Store Tools package, driven in a throwaway
  batch-mode project. That needs:
    * the com.unity.asset-store-tools package on disk -- found in the parent project's
      Packages/ or Library/PackageCache, or pass --asset-store-tools <folder>;
    * a publisher login on this machine. Nothing is typed or stored by this script:
      it reuses the session Asset Store Tools cached the last time you logged in
      through its Uploader window (any project), falling back to the Editor's own
      signed-in account. If both fail, open Tools > Asset Store > Uploader and log in.

  `python release.py --asset-store-list` prints every package the publisher has (name,
  package id, status) -- use it to find what to put in "assetStore".

The .unitypackage format is a gzip-compressed tar: one folder per asset, named by the
asset's GUID (from its .meta), holding `asset`, `asset.meta`, `pathname`. That is why
packing needs no Unity install.
"""

import argparse
import atexit
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


class _DropAuthOnRedirect(urllib.request.HTTPRedirectHandler):
    """GitHub redirects asset downloads to its storage backend, which refuses a
    request carrying both its signed URL and a forwarded Authorization header."""

    def redirect_request(self, req, fp, code, msg, headers, newurl):
        new = super().redirect_request(req, fp, code, msg, headers, newurl)
        if new is not None:
            new.remove_header("Authorization")
        return new


def download_release_package(slug, tok):
    """Download the .unitypackage asset of `slug`'s latest GitHub release.

    Returns (tag, raw_tar_bytes). The asset is matched by extension, not by
    name, so older releases with versioned filenames still work. The download
    goes through the API with the token so private repos work too.
    """
    rel = api("GET", f"{API}/repos/{slug}/releases/latest", tok=tok)
    packs = [a for a in rel.get("assets", []) if a["name"].endswith(".unitypackage")]
    # a module release may also carry a separate <name>.Samples.unitypackage;
    # always bundle the CORE package, never the sample one.
    core = [a for a in packs if not a["name"].endswith(".Samples.unitypackage")]
    asset = (core or packs)[0] if (core or packs) else None
    if not asset:
        fail(f"latest release of {slug} ({rel.get('tag_name')}) has no "
             f".unitypackage asset. Release that module first.")
    req = urllib.request.Request(asset["url"], headers={
        "Accept": "application/octet-stream",
        "Authorization": f"Bearer {tok}",
        "User-Agent": "unity-tool-release-script",
        "X-GitHub-Api-Version": "2022-11-28",
    })
    try:
        with urllib.request.build_opener(_DropAuthOnRedirect()).open(req) as resp:
            data = resp.read()
    except urllib.error.HTTPError as e:
        fail(f"downloading {asset['name']} from {slug} {rel.get('tag_name')} -> HTTP {e.code}")
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


def read_pathname(tar, member):
    return tar.extractfile(member).read().decode().splitlines()[0]


def module_roots(tar, groups):
    """{install root: module name} for every module.json inside a package."""
    roots = {}
    for parts in groups.values():
        if "pathname" not in parts or "asset" not in parts:
            continue
        pathname = read_pathname(tar, parts["pathname"])
        if os.path.basename(pathname) != "module.json":
            continue
        root = os.path.dirname(pathname)
        sub = json.loads(tar.extractfile(parts["asset"]).read().decode("utf-8-sig"))
        roots[root] = sub.get("name") or os.path.basename(root)
    return roots


def dependency_layout(manifest):
    """The bundler's layout rules: a root for every bundled module plus per-name overrides."""
    manifest = manifest or {}
    overrides = {}
    for name, spec in manifest.get("dependencies", {}).items():
        if isinstance(spec, dict) and spec.get("installPath"):
            overrides[name] = spec["installPath"].rstrip("/")
    root = (manifest.get("dependencyRoot") or "").rstrip("/") or None
    if root is None and not overrides:
        return None
    return {"root": root, "overrides": overrides}


def relocate_package(raw, dep_name, layout):
    """Rewrite a dependency package so each module in it lands where the bundler wants.

    Every module.json inside the package names a module and its current install
    root (a bundled package carries its own dependencies' manifests too). Each root
    moves to the override for that name, else <dependencyRoot>/<name>; the embedded
    module.json is rewritten to say so. A package without a manifest is relocated
    as one module rooted at the common folder of its assets.
    """
    tar, groups = package_groups(raw)
    roots = module_roots(tar, groups)
    if not roots:
        pathnames = [read_pathname(tar, p["pathname"]) for p in groups.values() if "pathname" in p]
        common = os.path.commonpath(pathnames).replace("\\", "/") if pathnames else ""
        if common.count("/") < 1:
            info(f"  ({dep_name} has no module.json and no single root folder; not relocated)")
            return raw
        roots[common] = dep_name
    moves = {}
    for old_root, name in roots.items():
        new_root = layout["overrides"].get(name) or (f"{layout['root']}/{name}" if layout["root"] else None)
        if new_root and new_root != old_root:
            moves[old_root] = new_root
    if not moves:
        return raw
    ordered = sorted(moves.items(), key=lambda kv: -len(kv[0]))  # nested roots first

    def moved(pathname):
        for old, new in ordered:
            if pathname == old or pathname.startswith(old + "/"):
                return new + pathname[len(old):]
        return pathname

    buf = io.BytesIO()
    with tarfile.open(fileobj=buf, mode="w") as out:
        for guid, parts in groups.items():
            pathname = read_pathname(tar, parts["pathname"]) if "pathname" in parts else None
            for part, member in parts.items():
                data = tar.extractfile(member).read()
                if part == "pathname" and pathname is not None:
                    lines = data.decode().splitlines()
                    lines[0] = moved(pathname)
                    data = "\n".join(lines).encode()
                elif part == "asset" and pathname is not None and os.path.dirname(pathname) in moves \
                        and os.path.basename(pathname) == "module.json":
                    sub = json.loads(data.decode("utf-8-sig"))
                    sub["installPath"] = moves[os.path.dirname(pathname)]
                    data = (json.dumps(sub, indent=4) + "\n").encode()
                ti = tarfile.TarInfo(f"{guid}/{part}")
                ti.size = len(data)
                ti.mtime = member.mtime
                ti.mode = 0o644
                out.addfile(ti, io.BytesIO(data))
    for old, new in ordered:
        info(f"  {roots[old]}: {old} -> {new}")
    return buf.getvalue()


def write_package(tar, groups):
    """Serialise a {guid: {part: member}} selection back into a raw tar."""
    buf = io.BytesIO()
    with tarfile.open(fileobj=buf, mode="w") as out:
        for guid, parts in groups.items():
            for part, member in parts.items():
                data = tar.extractfile(member).read()
                ti = tarfile.TarInfo(f"{guid}/{part}")
                ti.size = len(data)
                ti.mtime = member.mtime
                ti.mode = 0o644
                out.addfile(ti, io.BytesIO(data))
    return buf.getvalue()


def prune_nested_duplicates(dep_packages):
    """Drop, from each package, the modules that are also fetched directly.

    A bundled release carries copies of its own dependencies, frozen at the
    version it was built against. When this module fetches that dependency
    itself it gets the LATEST release, and that copy must win: the packer keeps
    the first GUID it sees and the scaffold keeps the last file written, so a
    stale nested copy would leak into one or the other. Modules only reachable
    through nesting are kept.
    """
    direct = {dep["name"] for dep in dep_packages}
    for dep in dep_packages:
        tar, groups = package_groups(dep["raw"])
        roots = module_roots(tar, groups)
        foreign = {root: name for root, name in roots.items()
                   if name != dep["name"] and name in direct}
        if not foreign:
            continue
        ordered = sorted(foreign, key=len, reverse=True)
        kept = {}
        for guid, parts in groups.items():
            pathname = read_pathname(tar, parts["pathname"]) if "pathname" in parts else None
            if pathname and any(pathname == r or pathname.startswith(r + "/") for r in ordered):
                continue
            kept[guid] = parts
        dropped = len(groups) - len(kept)
        dep["raw"] = write_package(tar, kept)
        info(f"  {dep['name']}: dropped its nested copy of "
             f"{', '.join(sorted(set(foreign.values())))} ({dropped} assets; fetched directly)")


def resolve_dependency_closure(manifest, tok, layout=None):
    """BFS the dependency graph, downloading each REQUIRED dependency's latest release.

    Returns a list of {name, repo, tag, raw} dicts, deduplicated by repo. Each
    downloaded package's embedded module.json supplies its own dependencies;
    releases that predate module.json are treated as leaves.

    Dependencies marked "optional": true are NOT bundled -- they're integrations
    (e.g. Zenject) whose code lives behind a define-constrained assembly, so the
    module works without them. ModuleManager still sees them in module.json.

    Each entry also carries the "packages" its manifest declares, so the verify
    scaffold can install the UPM packages a bundled dependency compiles against.

    With a layout (see dependency_layout) every downloaded package is relocated
    right here, so the verify scaffold and the bundled release agree.
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
        if layout:
            raw = relocate_package(raw, name, layout)
        sub = package_manifest(raw)
        if sub:
            queue.extend(sub.get("dependencies", {}).items())
        else:
            info(f"  ({name} release has no module.json; treating as leaf)")
        upm = (sub or {}).get("packages", {})
        if upm:
            info(f"  {name} needs UPM packages: {', '.join(upm)}")
        packages.append({"name": name, "repo": slug, "tag": tag, "raw": raw,
                         "packages": upm})
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


# --------------------------------------------------------------------------- #
# Asset Store upload (through Unity's Asset Store Tools, in a throwaway project)
# --------------------------------------------------------------------------- #
ASSET_STORE_TOOLS_PACKAGE = "com.unity.asset-store-tools"

# Asset Store Tools keeps its API internal and grants access to an assembly with this
# name (its own tests). Compiling against it means an API change in a newer Asset
# Store Tools shows up as a compile error naming the member, not as a failed upload.
ASSET_STORE_UPLOAD_ASMDEF = {
    "name": "AssetStoreTools.Tests",
    "references": ["asset-store-tools-editor"],
    "includePlatforms": ["Editor"],
    "autoReferenced": False,
}

ASSET_STORE_UPLOAD_CS = r"""
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AssetStoreTools;
using AssetStoreTools.Api;
using AssetStoreTools.Api.Models;
using AssetStoreTools.Api.Responses;
using UnityEditor;
using UnityEngine;

// Injected by release.py. "-asMode list" prints the publisher's packages;
// "-asMode check -asPackage <name or id>" finds that package's draft;
// "-asMode upload -asPackage <name or id> -asFile <.unitypackage>" uploads to it.
public static class ReleaseAssetStoreUpload
{
    private const string Tag = "[AssetStore] ";

    private static string Arg(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name) return args[i + 1];
        return null;
    }

    private static void Say(string message) => Debug.Log(Tag + message);

    public static async void Run()
    {
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        int code = 1;
        try
        {
            code = await RunAsync();
        }
        catch (Exception e)
        {
            Say("error: " + e);
        }
        EditorApplication.Exit(code);
    }

    private static async Task<int> RunAsync()
    {
        string mode = Arg("-asMode");
        var api = new AssetStoreApi(new AssetStoreClient());

        AuthenticationResponse auth = null;
        string session = EditorPrefs.GetString(Constants.Cache.SessionTokenKey, string.Empty);
        if (!string.IsNullOrEmpty(session))
            auth = await api.Authenticate(new SessionAuthentication(session));
        if (auth == null || !auth.Success)
        {
            string cloudToken = CloudProjectSettings.accessToken;
            if (!string.IsNullOrEmpty(cloudToken))
                auth = await api.Authenticate(new CloudTokenAuthentication(cloudToken));
        }
        if (auth == null || !auth.Success)
        {
            Say("error: not logged in" + (auth?.Exception != null ? " (" + auth.Exception.Message + ")" : ""));
            return 3;
        }
        if (!auth.User.IsPublisher)
        {
            Say("error: account '" + auth.User.Username + "' is not an Asset Store publisher");
            return 3;
        }
        Say("user=" + auth.User.Username);

        var data = await api.GetPackages();
        if (!data.Success)
        {
            Say("error: could not read the publisher's packages: " + data.Exception?.Message);
            return 1;
        }
        List<Package> packages = data.Packages ?? new List<Package>();
        foreach (var p in packages)
            Say("package id=" + p.PackageId + " status=" + p.Status + " name=" + p.Name);
        if (mode == "list")
            return 0;

        string key = Arg("-asPackage");
        var matches = packages.Where(p => p.PackageId == key
            || string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count == 0)
        {
            Say("error: no package named or numbered '" + key + "'");
            return 4;
        }
        var drafts = matches.Where(p => string.Equals(p.Status, "draft", StringComparison.OrdinalIgnoreCase)).ToList();
        if (drafts.Count != 1)
        {
            Say("error: '" + key + "' has " + drafts.Count + " draft versions (status: "
                + string.Join(", ", matches.Select(p => p.Status)) + "); exactly one is needed");
            return 4;
        }
        var draft = drafts[0];
        Say("draft id=" + draft.PackageId + " version=" + draft.VersionId + " name=" + draft.Name);
        if (mode == "check")
            return 0;

        string file = Arg("-asFile");
        var uploader = new UnityPackageUploader(new UnityPackageUploadSettings
        {
            VersionId = draft.VersionId,
            UnityPackagePath = file,
            RootGuid = string.Empty,
            RootPath = string.Empty,
            ProjectPath = file,
        });
        var result = await api.UploadPackage(uploader, new ProgressLog());
        Say("upload status=" + result.Status + (result.Exception != null ? " (" + result.Exception.Message + ")" : ""));
        if (result.Status == UploadStatus.Success) return 0;
        return result.Status == UploadStatus.ResponseTimeout ? 5 : 1;
    }

    private class ProgressLog : IProgress<float>
    {
        private int lastStep = -1;

        public void Report(float percent)
        {
            int step = (int)(percent / 10f);
            if (step <= lastStep) return;
            lastStep = step;
            Debug.Log(Tag + "uploaded " + step * 10 + "%");
        }
    }
}
"""


def asset_store_package_key(manifest, override):
    """The listing --asset-store targets: a package name or id, or None."""
    if override:
        return override
    spec = (manifest or {}).get("assetStore")
    if isinstance(spec, dict):
        spec = spec.get("package")
    if spec is None:
        return None
    if not isinstance(spec, (str, int)) or not str(spec).strip():
        fail('module.json "assetStore" must be the listing\'s name or package id, '
             'or {"package": <name or id>}.')
    return str(spec).strip()


def find_asset_store_tools(parent, override):
    """Folder of the com.unity.asset-store-tools package, or None."""
    candidates = [override] if override else []
    if parent and not override:
        candidates.append(os.path.join(parent, "Packages", ASSET_STORE_TOOLS_PACKAGE))
        cache = os.path.join(parent, "Library", "PackageCache")
        if os.path.isdir(cache):
            candidates += [os.path.join(cache, d) for d in sorted(os.listdir(cache))
                           if d.startswith(ASSET_STORE_TOOLS_PACKAGE)]
    for c in candidates:
        if os.path.isfile(os.path.join(c, "package.json")):
            return os.path.abspath(c)
    return None


class AssetStoreSession:
    """A throwaway project holding Asset Store Tools and the injected uploader.

    One project serves every call, so only the first pays for the import. Deleted
    when the script exits, however it exits.
    """

    def __init__(self, repo, args):
        parent = find_parent_project(repo)
        tools = find_asset_store_tools(parent, args.asset_store_tools)
        if not tools:
            fail(f"could not find the {ASSET_STORE_TOOLS_PACKAGE} package. Install Asset "
                 "Store Tools in the parent project, or pass --asset-store-tools <folder>.")
        unity_version = detect_unity_version(parent, args.unity_version)
        self.unity_exe = find_unity(unity_version, args.unity)
        if not self.unity_exe:
            fail(f"could not find Unity {unity_version}. Pass --unity <path> or set UNITY_PATH.")
        self.keep = args.keep_test_project

        deps = scaffold_deps(parent, DEFAULT_TF_VERSION)
        del deps["com.unity.test-framework"]
        deps.update(json.load(open(os.path.join(tools, "package.json"))).get("dependencies", {}))
        deps[ASSET_STORE_TOOLS_PACKAGE] = "file:" + tools.replace("\\", "/")

        self.proj = tempfile.mkdtemp(prefix="asset_store_upload_")
        os.makedirs(os.path.join(self.proj, "ProjectSettings"))
        os.makedirs(os.path.join(self.proj, "Packages"))
        with open(os.path.join(self.proj, "ProjectSettings", "ProjectVersion.txt"), "w") as fh:
            fh.write(f"m_EditorVersion: {unity_version}\n")
        with open(os.path.join(self.proj, "Packages", "manifest.json"), "w") as fh:
            json.dump({"dependencies": deps}, fh, indent=2)
        code_dir = os.path.join(self.proj, "Assets", "AssetStoreUpload", "Editor")
        os.makedirs(code_dir)
        with open(os.path.join(code_dir, "AssetStoreTools.Tests.asmdef"), "w") as fh:
            json.dump(ASSET_STORE_UPLOAD_ASMDEF, fh, indent=2)
        with open(os.path.join(code_dir, "ReleaseAssetStoreUpload.cs"), "w") as fh:
            fh.write(ASSET_STORE_UPLOAD_CS)
        self.runs = 0
        atexit.register(self.close)

    def run(self, mode, package=None, file=None, timeout=1800):
        """Returns the uploader's own log lines, tag stripped. Fails on any error."""
        extra = ["-executeMethod", "ReleaseAssetStoreUpload.Run", "-asMode", mode]
        if package:
            extra += ["-asPackage", package]
        if file:
            extra += ["-asFile", os.path.abspath(file)]
        self.runs += 1
        log = os.path.join(self.proj, f"asset-store-{self.runs}-{mode}.log")
        rc, logtxt, cs = run_unity(self.unity_exe, self.proj, extra, log, timeout=timeout)
        if cs:
            fail("the Asset Store uploader did not compile against this Asset Store Tools "
                 "version:\n  " + "\n  ".join(cs[:25]))
        tag = "[AssetStore] "
        lines = [ln.split(tag, 1)[1].strip() for ln in logtxt.splitlines() if tag in ln]
        errors = [ln for ln in lines if ln.startswith("error:")]
        if rc == 3:
            fail("Asset Store login failed (" + "; ".join(errors) + "). Open Tools > Asset "
                 "Store > Uploader in any Unity project, log in as the publisher, and retry.")
        if rc == 5:
            fail("the package was sent but the Asset Store did not answer in time. Check the "
                 "draft in the Publisher Portal before uploading again.")
        if rc != 0:
            if mode != "list" and any(ln.startswith("package ") for ln in lines):
                errors += ["the publisher's packages:"] + \
                          ["  " + ln[len("package "):] for ln in lines if ln.startswith("package ")]
            fail(f"Asset Store {mode} failed (exit {rc}).\n  "
                 + ("\n  ".join(errors) if errors
                    else "Log tail:\n" + "\n".join(logtxt.splitlines()[-25:])))
        return lines

    def close(self):
        if not os.path.isdir(self.proj):
            return
        if self.keep:
            info(f"keeping Asset Store upload project at: {self.proj}")
        else:
            shutil.rmtree(self.proj, ignore_errors=True)


def scaffold_project(repo, name, unity_version, deps, exclude_prefixes=(), install_path=None):
    """Create a temp Unity project containing the tracked tool content at its install path."""
    proj = tempfile.mkdtemp(prefix=f"ci_{name}_")
    os.makedirs(os.path.join(proj, "ProjectSettings"))
    os.makedirs(os.path.join(proj, "Packages"))
    with open(os.path.join(proj, "ProjectSettings", "ProjectVersion.txt"), "w") as fh:
        fh.write(f"m_EditorVersion: {unity_version}\n")
    with open(os.path.join(proj, "Packages", "manifest.json"), "w") as fh:
        json.dump({"dependencies": deps}, fh, indent=2)

    # copy every tracked file to the module's install path, preserving structure
    dest_root = os.path.join(proj, *(install_path or f"Assets/{name}").split("/"))
    for rel in git(repo, "ls-files").splitlines():
        if any(rel == p or rel.startswith(p + "/") for p in exclude_prefixes):
            continue
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


def run_unity(unity_exe, project, extra_args, log_path, timeout, nographics=True):
    """nographics=False keeps the graphics device: EditMode tests that touch editor
    GUI resources (EditorStyles, plugin icon sets) find them null without it."""
    args = [unity_exe, "-batchmode", "-projectPath", project, "-logFile", log_path]
    if nographics:
        args.insert(2, "-nographics")
    args += extra_args
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
                   dep_packages=(), build_scene=None, exclude_prefixes=(),
                   sample_raw=None, install_path=None):
    install_path = install_path or f"Assets/{name}"
    info(f"scaffolding throwaway Unity {unity_version} project ({len(deps)} deps) ...")
    proj = scaffold_project(repo, name, unity_version, deps, exclude_prefixes, install_path)
    for dep in dep_packages:
        n = extract_package_into_project(dep["raw"], proj)
        info(f"  added dependency {dep['name']} {dep['tag']} ({n} assets)")
    if sample_raw:
        n = extract_package_into_project(sample_raw, proj)
        info(f"  imported sample package to compile-test it ({n} assets)")
    try:
        # --- tests + compile check (per platform) ---
        for platform in platforms:
            res_xml = os.path.join(proj, f"results-{platform}.xml")
            log = os.path.join(proj, f"tests-{platform}.log")
            info(f"running {platform} tests (compiles the project) ...")
            rc, logtxt, cs = run_unity(
                unity_exe, proj,
                ["-runTests", "-testPlatform", platform, "-testResults", res_xml],
                log, timeout=2400, nographics=False,
            )
            parsed = parse_test_results(res_xml)
            if cs:
                # Unity runs no tests while any script fails to compile, so results mean the
                # errors came from an earlier pass the API updater fixed (6000.4's Shader Graph).
                if parsed is None:
                    fail("compilation failed:\n  " + "\n  ".join(cs[:25]))
                info("  compile errors from a pass the API updater fixed; the tests ran:\n    "
                     + "\n    ".join(cs[:25]))
            # -runTests: 0 = all passed / no tests, 2 = some failed, else = error
            if rc not in (0, 2):
                fail(f"Unity exited {rc} during {platform} tests. Log tail:\n"
                     + "\n".join(logtxt.splitlines()[-25:]))
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
            scene_path = f"{install_path}/{build_scene}"
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
def build_unitypackage(repo, install_path, out_file, exclude_prefixes, dep_packages=(),
                       only_prefix=None, strip_tilde=False):
    """Pack tracked assets into a .unitypackage.

    only_prefix: if set, pack ONLY assets under this path (used to build a
    separate sample package). exclude_prefixes still applies.
    strip_tilde: rewrite `~/` out of install paths so a `Samples~/` source folder
    lands as an importable `Samples/` (Unity ignores `~`-suffixed folders).
    """
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
            if only_prefix and not (asset_rel == only_prefix
                                    or asset_rel.startswith(only_prefix + "/")):
                continue
            # Unity never imports a `~` folder, so packing one ships dead files (test
            # fixtures, for instance) into the buyer's project. Only a sample package,
            # which strips the `~` on the way out, has any use for them.
            if not strip_tilde and any(seg.endswith("~") for seg in asset_rel.split("/")[:-1]):
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
            if strip_tilde:
                pathname = pathname.replace("~/", "/")

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
    info(f"packed {added} assets + {len(dep_packages)} bundled deps -> {out_file}")
    return added


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
    ap.add_argument("version", nargs="?", help="version / tag to release, e.g. 1.1")
    ap.add_argument("--repo", default=".", help="path to the tool repo (default: cwd)")
    ap.add_argument("--name", help="package + install-folder name (default: repo folder name)")
    ap.add_argument("--install-path", help="import path inside a project (default: Assets/<name>)")
    ap.add_argument("--tag", help="git tag to use (default: the version verbatim)")
    ap.add_argument("--exclude", nargs="*", default=["ReadmeResources"],
                    help="path prefixes to keep OUT of the package")
    ap.add_argument("--sample", choices=["separate", "bundled"],
                    help="ship module.json's \"sample\" as its own <name>.Samples.unitypackage, "
                         "or inside the core (default: separate; bundled with --asset-store)")
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
    ap.add_argument("--asset-store", action="store_true",
                    help="upload to the module's Asset Store draft instead of releasing on GitHub")
    ap.add_argument("--asset-store-list", action="store_true",
                    help="print the publisher's Asset Store packages and exit")
    ap.add_argument("--asset-store-package",
                    help="listing name or package id (default: module.json's \"assetStore\")")
    ap.add_argument("--asset-store-tools",
                    help="path to the com.unity.asset-store-tools package folder "
                         "(default: found in the parent project)")
    args = ap.parse_args()

    repo = os.path.abspath(args.repo)
    if not os.path.exists(os.path.join(repo, ".git")):
        fail(f"{repo} is not a git repo.")
    if args.asset_store_list:
        info("reading the publisher's Asset Store packages ...")
        for line in AssetStoreSession(repo, args).run("list"):
            print("  " + line)
        return
    if not args.version:
        ap.error("the version to release is required")
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
    layout = dependency_layout(manifest)
    info(f"repo={owner}/{gh_name}  name={name}  tag={tag}  install={install_path}"
         + (f"  dependencies={layout['root']}/<Name>" if layout and layout["root"] else ""))

    # 1. guards -------------------------------------------------------------
    if not args.allow_dirty and git(repo, "status", "--porcelain"):
        fail("working tree is dirty. Commit/stash first, or pass --allow-dirty.")
    if not args.asset_store and tag in git(repo, "tag").splitlines():
        fail(f"tag '{tag}' already exists.")
    branch = git(repo, "rev-parse", "--abbrev-ref", "HEAD")

    # Find the Asset Store draft now, so a wrong listing name or a missing login
    # fails here and not after the verify.
    store, store_package = None, None
    if args.asset_store:
        store_package = asset_store_package_key(manifest, args.asset_store_package)
        if not store_package:
            fail('--asset-store needs the listing to upload to: add "assetStore": '
                 '{"package": <name or id>} to module.json, or pass --asset-store-package. '
                 '`release.py --asset-store-list` prints the candidates.')
        store = AssetStoreSession(repo, args)
        info(f"looking up the Asset Store draft of '{store_package}' ...")
        found = store.run("check", package=store_package)
        info("  " + next(ln for ln in found if ln.startswith("draft ")))

    # A "sample" in module.json ships as a SEPARATE <name>.Samples.unitypackage by
    # default: excluded from the CORE package, packed with any ~ stripped so Samples~/
    # installs as an importable Samples/, and -- so it is actually tested -- imported
    # into the verify scaffold (exercising the real shipped artifact). With --sample
    # bundled it stays inside the core and is verified as part of its source. "sample"
    # is a path string, or {path, packages} where packages are extra UPM deps the
    # sample needs (e.g. TextMeshPro / Input System) added to the verify scaffold.
    sample = (manifest or {}).get("sample")
    if isinstance(sample, str):
        sample_path, sample_packages = sample, {}
    else:
        sample = sample or {}
        sample_path, sample_packages = sample.get("path"), sample.get("packages", {})
    manifest_exclude = (manifest or {}).get("exclude", [])
    if not isinstance(manifest_exclude, list) or not all(isinstance(p, str) for p in manifest_exclude):
        fail('module.json "exclude" must be a list of path prefixes, e.g. ["docs"].')
    manifest_exclude = [p.replace("\\", "/").strip("/") for p in manifest_exclude]
    exclude = list(args.exclude) + manifest_exclude
    sample_mode = args.sample or ("bundled" if args.asset_store else "separate")
    ships_sample_separately = bool(sample_path) and sample_mode == "separate"
    core_exclude = exclude + ([sample_path] if ships_sample_separately else [])

    # 2. resolve + download the dependency closure ---------------------------
    dep_packages = resolve_dependency_closure(manifest, tok, layout)
    prune_nested_duplicates(dep_packages)

    # 3. pack the separate sample package early (so the verify can import it) -
    sample_out, sample_raw = None, None
    if sample_path and not ships_sample_separately:
        info(f"bundling sample '{sample_path}' into the core package (--sample bundled).")
    if ships_sample_separately:
        sample_out = os.path.join(repo, "dist", f"{name}.Samples.unitypackage")
        info(f"packing separate sample package from '{sample_path}' ...")
        if not build_unitypackage(repo, install_path, sample_out, exclude,
                                  only_prefix=sample_path, strip_tilde=True):
            fail(f"sample path '{sample_path}' contains no packable assets.")
        sample_raw = gzip.decompress(open(sample_out, "rb").read())

    # 4. local Unity verify (core source + deps + the imported sample package)
    if args.skip_tests:
        info("skipping local Unity verify (--skip-tests).")
    else:
        parent = find_parent_project(repo)
        unity_version = detect_unity_version(parent, args.unity_version)
        deps = scaffold_deps(parent, detect_tf_version(parent))
        for dep in dep_packages:                            # bundled deps' UPM deps
            deps.update(dep.get("packages", {}))
        deps.update((manifest or {}).get("packages", {}))  # core UPM deps (win on conflict)
        deps.update(sample_packages)                        # sample UPM deps (TMP/InputSystem)
        unity_exe = find_unity(unity_version, args.unity)
        if not unity_exe:
            fail(f"could not find Unity {unity_version}. Pass --unity <path> or set "
                 f"UNITY_PATH, or --skip-tests to bypass.")
        verify_locally(repo, name, unity_version, deps, unity_exe,
                       args.test_platforms, args.keep_test_project,
                       dep_packages=dep_packages,
                       build_scene=(manifest or {}).get("buildScene"),
                       exclude_prefixes=manifest_exclude + ([sample_path] if ships_sample_separately else []),
                       sample_raw=sample_raw, install_path=install_path)

    # 5. pack the core package (+ bundled dependency closure) ----------------
    build_unitypackage(repo, install_path, out_file, core_exclude,
                       dep_packages=dep_packages)

    if store:
        if sample_out:
            info("note: the sample package is not uploaded; an Asset Store listing holds one package.")
        info(f"uploading {os.path.basename(out_file)} to the Asset Store draft of '{store_package}' ...")
        for line in store.run("upload", package=store_package, file=out_file, timeout=7200):
            if line.startswith("upload "):
                info("  " + line)
        info("done. Nothing was pushed or tagged.")
        print(f"\n  Package : {out_file}")
        print("  Uploaded to the draft. Review it and press Submit in the Publisher Portal:")
        print("    https://publisher.unity.com/packages\n")
        return

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

    def upload(path, asset):
        info(f"uploading {asset} ...")
        with open(path, "rb") as fh:
            api("POST", f"{upload_url}?name={asset}", tok=tok, raw=True,
                data=fh.read(), headers={"Content-Type": "application/octet-stream"})

    asset_name = f"{name}.unitypackage"
    upload(out_file, asset_name)
    sample_asset = None
    if sample_out:
        sample_asset = f"{name}.Samples.unitypackage"
        upload(sample_out, sample_asset)

    # 6. done ---------------------------------------------------------------
    info("done.")
    base = f"https://github.com/{owner}/{gh_name}/releases/latest/download"
    print(f"\n  Release : {rel['html_url']}")
    print(f"  Package : {asset_name}")
    if sample_asset:
        print(f"  Sample  : {sample_asset}  (import the core package first)")
    print(f"  Docs links (always newest):")
    print(f"    {base}/{asset_name}")
    if sample_asset:
        print(f"    {base}/{sample_asset}")
    print()


if __name__ == "__main__":
    main()
