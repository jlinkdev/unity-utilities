#!/usr/bin/env python3
"""Validate this repository's UPM packaging and docs using only the standard library.

Run from any directory: python /path/to/unity-utilities/scripts/validate-packages.py
This is a structural check, not a Unity import, compile, test, or compatibility run.
"""
import json
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET
from urllib.parse import unquote, urlsplit
from camera_passes import camera_pass_sources
from sample_installers import sources as installer_sources
from package_samples import META_GUID, sync_sample

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
    if "com.jlinkdev.sample-framework" in manifest.get("dependencies", {}):
        fail(manifest_path, "the Sample Framework must remain an optional sample dependency")
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
    if not isinstance(samples, list):
        fail(manifest_path, "samples must be a list")
        return 0
    preview_without_samples = ("samples" in manifest and
                               re.search(r"-preview\.\d+$", str(manifest.get("version", ""))))
    if not samples and not preview_without_samples:
        fail(manifest_path, "expected at least one registered sample for a release")
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
            if re.search(rb"^  m_RendererIndex: [0-9]+", scene.read_bytes(), re.M):
                fail(scene, "sample camera references a project-specific URP renderer slot")
        identity = directory / "UI" / "Sample Identity.uxml"
        try:
            document = ET.parse(identity)
            rows = [element for element in document.iter() if "sample-identity-content" in element.get("class", "").split()]
            if len(rows) != 1 or len(rows[0]) == 0 or "sample-identity-gradient" not in rows[0][0].get("class", "").split():
                fail(identity, "the shared header needs a gradient behind the icon and title")
            elif rows[0][0].get("picking-mode") != "Ignore":
                fail(identity, "the header gradient must let input through to the sample")
            labels = {element.get("name"): element.get("text") for element in document.iter()}
            if labels.get("package-name") != manifest["displayName"].removeprefix("jlinkdev ") or labels.get("sample-name") != sample["displayName"]:
                fail(identity, "package and sample labels must match their manifest display names")
            marker = META_GUID.search(Path(str(identity) + ".meta").read_bytes())[1]
            for scene in scenes:
                if marker not in scene.read_bytes():
                    fail(scene, "scene does not reference its sample identity")
        except (OSError, ET.ParseError, TypeError) as error:
            fail(identity, str(error))
        try:
            for change in sync_sample(package, sample):
                fail(directory, f"editable and published samples differ: {change}")
        except (ValueError, OSError) as error:
            fail(directory, str(error))
        editable = ROOT / "Assets/PackageSamples" / package.name / directory.name
        for markdown in editable.rglob("*.md"):
            check_links(markdown, editable)
    for markdown in package.rglob("*.md"):
        check_links(markdown, package)
    return len(samples)


def check_sample_references(packages):
    # Include hidden package samples in the index as if they had all been imported.
    owners = {}
    sample_assets = []
    for base in [ROOT / "Assets", *packages]:
        for meta in base.rglob("*.meta"):
            # Package Manager installs the published assets here with their original
            # GUIDs. Index their sources in Samples~ once, independent of local imports.
            if meta.is_relative_to(ROOT / "Assets/Samples"):
                continue
            match = META_GUID.search(meta.read_bytes())
            if not match:
                continue
            path = meta.with_suffix("")
            sample = None
            if "Samples~" in path.parts:
                index = path.parts.index("Samples~")
                sample = Path(*path.parts[:index + 2])
            elif path.is_relative_to(ROOT / "Assets/PackageSamples"):
                parts = path.relative_to(ROOT / "Assets/PackageSamples").parts
                if len(parts) >= 2:
                    sample = ROOT / "Assets/PackageSamples" / parts[0] / parts[1]
            previous = owners.get(match[1])
            if previous and (sample or previous[1]):
                fail(meta, f"sample GUID already used by {previous[0].relative_to(ROOT)}")
            owners[match[1]] = (path, sample)
            if sample and path.is_file():
                sample_assets.append((path, sample))
    for path, sample in sample_assets:
        if path.suffix not in {".unity", ".prefab", ".asset", ".mat", ".anim", ".controller", ".asmdef"}:
            continue
        for guid in set(re.findall(rb"(?:guid:\s*|GUID:)([0-9a-f]{32})", path.read_bytes())):
            target, owner = owners.get(guid, (None, None))
            if target and owner != sample and (owner or target.is_relative_to(ROOT / "Assets")):
                fail(path, f"sample depends on an asset outside its distributable scope: {target.relative_to(ROOT)}")


def main():
    packages = sorted((ROOT / "Packages").glob("com.jlinkdev.*"))
    packages = [package for package in packages if package.is_dir()]
    if not packages:
        fail(ROOT / "Packages", "no owned packages found")
    sample_count = sum(check_package(package) for package in packages)
    for source, content in camera_pass_sources():
        if not source.exists() or source.read_text(encoding="utf-8") != content:
            fail(source, "camera pass differs from its template; run python scripts/camera_passes.py")
    for source, content in installer_sources():
        if not source.exists() or source.read_text(encoding="utf-8") != content:
            fail(source, "sample installer differs from its source; run python scripts/sample_installers.py")
    framework = ROOT / "Packages/com.jlinkdev.sample-framework"
    framework_refs = set()
    for asmdef in (framework / "Runtime").rglob("*.asmdef"):
        framework_refs.add(read_json(asmdef).get("name"))
        meta = Path(str(asmdef) + ".meta")
        guid = META_GUID.search(meta.read_bytes()) if meta.is_file() else None
        if guid:
            framework_refs.add("GUID:" + guid[1].decode())
        else:
            fail(asmdef, "missing framework assembly GUID")
    for package in packages:
        if package != framework:
            for asmdef in (package / "Runtime").rglob("*.asmdef"):
                if framework_refs.intersection(read_json(asmdef).get("references", [])):
                    fail(asmdef, "utility runtime assembly depends on the optional Sample Framework")
    check_sample_references(packages)
    registered = {Path(package.name) / Path(sample["path"]).name
                  for package in packages for sample in read_json(package / "package.json").get("samples", [])
                  if isinstance(sample, dict) and isinstance(sample.get("path"), str)}
    for folder in (ROOT / "Assets/PackageSamples").glob("*/*"):
        if folder.is_dir() and Path(folder.parent.name, folder.name) not in registered:
            fail(folder, "editable sample has no package registration")
    for package in packages:
        for folder in (package / "Samples~").glob("*"):
            if folder.is_dir() and Path(package.name, folder.name) not in registered:
                fail(folder, "published sample has no package registration")
    for document in (
        "README.md",
        "docs/maintainer-guide.md",
        "Assets/PackageDevelopment/README.md",
        "Assets/PackageSamples/README.md",
    ):
        check_links(ROOT / document, ROOT)
    if ERRORS:
        print("\n".join(ERRORS), file=sys.stderr)
        print(f"FAILED: {len(ERRORS)} packaging/documentation issue(s).", file=sys.stderr)
        return 1
    print(f"PASS: {len(packages)} packages, {sample_count} registered samples; manifests, "
          "README sections, local links, matching editable samples, scenes, and assembly JSON.")
    print("Unity compilation, rendering, tests, external links, and compatibility were not checked.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
