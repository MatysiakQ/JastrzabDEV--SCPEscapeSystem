"""Publish the checked-in plugin bundle after regression tests have passed in Actions."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import tempfile
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent.parent


def run(*args):
    return subprocess.run(args, cwd=ROOT, check=True, capture_output=True, text=True).stdout


def api(path, missing_ok=False):
    result = subprocess.run(["gh", "api", path], cwd=ROOT, capture_output=True, text=True)
    if result.returncode:
        if missing_ok and "HTTP 404" in result.stderr:
            return None
        raise RuntimeError(result.stderr.strip())
    return json.loads(result.stdout)


def verify_bundle():
    version = ET.parse(ROOT / "SCPRUN.csproj").findtext("./PropertyGroup/Version")
    if not version or not re.fullmatch(r"\d+\.\d+\.\d+", version):
        raise ValueError("The project must declare a numeric major.minor.patch version")
    metadata = json.loads((ROOT / "dist/BUILDINFO.json").read_text())
    if metadata["version"] != version:
        raise ValueError("Project and artifact versions differ")
    assets = [ROOT / "dist" / name for name in ("ScpEscapeSystem.dll", "SHA256SUMS.txt", "BUILDINFO.json")]
    digest = hashlib.sha256(assets[0].read_bytes()).hexdigest()
    checksum = assets[1].read_text().split()
    if checksum != [digest, "ScpEscapeSystem.dll"] or metadata["sha256"] != digest:
        raise ValueError("DLL checksum does not match the bundle metadata")
    notes = ROOT / "release-notes" / ("v" + version + ".md")
    body = notes.read_text(encoding="utf-8")
    if "## 🇵🇱 Polski" not in body or "## 🇬🇧 English" not in body:
        raise ValueError("Both Polish and English release descriptions are required")
    return "v" + version, assets, notes, body


def tag_commit(repository, tag):
    ref = api(f"repos/{repository}/git/ref/tags/{tag}", missing_ok=True)
    if ref is None:
        return None
    obj = ref["object"]
    for _ in range(5):
        if obj["type"] == "commit":
            return obj["sha"]
        if obj["type"] != "tag":
            break
        obj = api(f"repos/{repository}/git/tags/{obj['sha']}")["object"]
    raise ValueError("The release tag does not resolve to a commit")


def get_release(repository, tag):
    release = api(f"repos/{repository}/releases/tags/{tag}", missing_ok=True)
    if release is not None:
        return release
    # Draft releases may need to be found through the authenticated release list.
    releases = api(f"repos/{repository}/releases?per_page=100")
    return next((item for item in releases if item["tag_name"] == tag), None)


def verify_asset(repository, tag, asset, local):
    expected = "sha256:" + hashlib.sha256(local.read_bytes()).hexdigest()
    if asset["size"] != local.stat().st_size:
        raise ValueError(f"Remote asset size differs: {local.name}")
    if asset.get("digest"):
        if asset["digest"] != expected:
            raise ValueError(f"Remote asset checksum differs: {local.name}")
    else:
        # Older GitHub responses may omit digest; verify downloaded bytes instead.
        with tempfile.TemporaryDirectory() as directory:
            run("gh", "release", "download", tag, "--repo", repository,
                "--pattern", local.name, "--dir", directory)
            actual = "sha256:" + hashlib.sha256((Path(directory) / local.name).read_bytes()).hexdigest()
            if actual != expected:
                raise ValueError(f"Downloaded asset checksum differs: {local.name}")


def publish(tag, assets, notes, body):
    repository = os.environ["GITHUB_REPOSITORY"]
    target = os.environ["GITHUB_SHA"]
    if os.environ.get("GITHUB_REF") != "refs/heads/master":
        raise ValueError("Publishing is restricted to master")
    if not re.fullmatch(r"[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+", repository):
        raise ValueError("Invalid repository")
    if not re.fullmatch(r"[0-9a-f]{40}", target) or run("git", "rev-parse", "HEAD").strip() != target:
        raise ValueError("The checked-out commit differs from the requested release target")
    existing_tag = tag_commit(repository, tag)
    if existing_tag is not None and existing_tag != target:
        raise ValueError("An existing tag points to a different commit; refusing to move it")
    release = get_release(repository, tag)
    if release is None:
        run("gh", "release", "create", tag, "--repo", repository, "--target", target,
            "--draft", "--title", "SCP Escape System " + tag, "--notes-file", str(notes))
        release = get_release(repository, tag)
        if release is None:
            raise RuntimeError("The created draft release could not be read")
    if (release.get("body") or "").replace("\r\n", "\n").strip() != body.strip():
        raise ValueError("Existing release notes differ; refusing to overwrite them")
    current_assets = {item["name"]: item for item in release["assets"]}
    for local in assets:
        if local.name in current_assets:
            verify_asset(repository, tag, current_assets[local.name], local)
        elif release["draft"]:
            run("gh", "release", "upload", tag, str(local), "--repo", repository)
        else:
            raise ValueError("A published release is missing an expected asset; refusing to modify it")
    release = get_release(repository, tag)
    current_assets = {item["name"]: item for item in release["assets"]}
    for local in assets:
        verify_asset(repository, tag, current_assets[local.name], local)
    if release["draft"]:
        run("gh", "release", "edit", tag, "--repo", repository,
            "--draft=false", "--prerelease=false", "--latest")
    release = get_release(repository, tag)
    if release["draft"] or release["prerelease"] or tag_commit(repository, tag) != target:
        raise ValueError("Published release or tag does not match the requested result")
    print(json.dumps({"tag": tag, "commit": target, "url": release["html_url"],
                      "assets": [path.name for path in assets]}, indent=2))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--verify-only", action="store_true")
    args = parser.parse_args()
    bundle = verify_bundle()
    if args.verify_only:
        print(json.dumps({"tag": bundle[0], "assets": [path.name for path in bundle[1]],
                          "notes": str(bundle[2].relative_to(ROOT))}, indent=2))
    else:
        publish(*bundle)
