#!/usr/bin/env python3
"""Validate this repository's UPM packaging and docs using only the standard library.

Run from any directory: python /path/to/unity-utilities/scripts/validate-packages.py
This is a structural check, not a Unity import, compile, test, or compatibility run.
"""
import json
from pathlib import Path
import re
import sys
from urllib.parse import unquote, urlsplit

ROOT = Path(__file__).resolve().parents[1]
SECTIONS = ("Requirements", "Installation", "Quick start", "Sample", "Limitations",
            "Documentation", "License")
ERRORS = []


def fail(path, message):
    ERRORS.append(f"{path.relative_to(ROOT)}: {message}")


def read_json(path):
    try:
        return json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, ValueError) as error:
        fail(path, f"cannot read JSON: {error}")
        return {}


def check_links(path, boundary):
    text = path.read_text(encoding="utf-8-sig")
    text = re.sub(r"^```.*?^```[^\n]*$", "", text, flags=re.M | re.S)
    # Repository convention: inline links, angle brackets around paths with spaces.
    for match in re.finditer(r"\[[^\]\n]*\]\((<[^>]+>|[^)\s]+)\)", text):
        link = match.group(1).strip("<>")
        url = urlsplit(link)
        if url.scheme or url.netloc or not url.path:
            continue
        destination = (path.parent / unquote(url.path)).resolve()
        if not destination.is_relative_to(boundary.resolve()):
            fail(path, f"link leaves its distributable boundary: {link}")
        elif not destination.exists():
            fail(path, f"missing local link destination: {link}")


def check_package(package):
    manifest_path = package / "package.json"
    manifest = read_json(manifest_path)
    if not isinstance(manifest, dict):
        fail(manifest_path, "manifest must be a JSON object")
        return 0
    for field in ("name", "displayName", "version", "unity", "description", "license", "author"):
        if not manifest.get(field):
            fail(manifest_path, f"missing {field}")
    if manifest.get("name") != package.name:
        fail(manifest_path, "name does not match package folder")
    if not re.fullmatch(r"\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?", str(manifest.get("version", ""))):
        fail(manifest_path, "version must have major.minor.patch and optional prerelease/build suffix")
    if not re.fullmatch(r"\d+\.\d+", str(manifest.get("unity", ""))):
        fail(manifest_path, "unity must have major.minor")
    for filename in ("README.md", "CHANGELOG.md", "LICENSE.md"):
        file = package / filename
        if not file.is_file() or not file.read_text(encoding="utf-8-sig").strip():
            fail(file, "required nonempty file missing")
    readme = package / "README.md"
    if readme.is_file():
        headings = re.findall(r"^## (.+)$", readme.read_text(encoding="utf-8-sig"), re.M)
        if headings != list(SECTIONS):
            fail(readme, "expected ordered sections: " + ", ".join(SECTIONS))
    if not list((package / "Runtime").glob("*.asmdef")):
        fail(package, "missing Runtime assembly definition")
    for asmdef in package.rglob("*.asmdef"):
        assembly = read_json(asmdef)
        if not isinstance(assembly, dict) or not assembly.get("name"):
            fail(asmdef, "assembly name missing")
        elif "Editor" in asmdef.relative_to(package).parts and assembly.get("includePlatforms") != ["Editor"]:
            fail(asmdef, "Editor assembly must target Editor only")
    samples = manifest.get("samples", [])
    if not isinstance(samples, list) or not samples:
        fail(manifest_path, "expected at least one registered sample")
        return 0
    for sample in samples:
        if not isinstance(sample, dict) or not all(sample.get(k) for k in ("displayName", "description", "path")):
            fail(manifest_path, "sample needs displayName, description, and path")
            continue
        directory = (package / sample["path"]).resolve()
        if not directory.is_relative_to((package / "Samples~").resolve()):
            fail(manifest_path, "sample must be within Samples~")
            continue
        if not (directory / "README.md").is_file():
            fail(manifest_path, f"sample guide missing: {sample['path']}")
        scenes = list(directory.rglob("*.unity"))
        if not scenes:
            fail(manifest_path, f"sample scene missing: {sample['path']}")
        for scene in scenes:
            if not scene.with_suffix(".unity.meta").is_file():
                fail(scene, "scene metadata missing")
    for markdown in package.rglob("*.md"):
        check_links(markdown, package)
    return len(samples)


def main():
    packages = sorted((ROOT / "Packages").glob("com.jlinkdev.*"))
    packages = [package for package in packages if package.is_dir()]
    if not packages:
        fail(ROOT / "Packages", "no owned packages found")
    sample_count = sum(check_package(package) for package in packages)
    for document in ("README.md", "CONTRIBUTING.md"):
        check_links(ROOT / document, ROOT)
    if ERRORS:
        print("\n".join(ERRORS), file=sys.stderr)
        print(f"FAILED: {len(ERRORS)} packaging/documentation issue(s).", file=sys.stderr)
        return 1
    print(f"PASS: {len(packages)} packages, {sample_count} registered samples; manifests, "
          "README sections, local link destinations, sample scenes, and assembly JSON.")
    print("Unity compilation, rendering, tests, external links, and compatibility were not checked.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
