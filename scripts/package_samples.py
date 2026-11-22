"""Check or publish editable Unity samples using only the Python standard library."""
import argparse
import filecmp
import json
from pathlib import Path
import re
import shutil
import tempfile


ROOT = Path(__file__).resolve().parents[1]
GUID = re.compile(rb"\b[0-9a-f]{32}\b")
META_GUID = re.compile(rb"^guid: ([0-9a-f]{32})\s*$", re.MULTILINE)
# A reversible bijection keeps both identities stable across asset and folder renames.
GUID_MASK = 0xC309D84513CF4824A153509DB6E2AD41
BINARY_EXTENSIONS = {".fbx", ".png", ".jpg", ".tga", ".wav", ".dll"}


def paired_guid(guid):
    return f"{int(guid, 16) ^ GUID_MASK:032x}".encode("ascii")


def inventory(root):
    files = {Path("../" + root.name + ".meta"): Path(str(root) + ".meta")}
    for path in root.rglob("*"):
        if path.is_symlink():
            raise ValueError(f"Unexpected symlink in sample: {path}")
        if path.is_file():
            files[path.relative_to(root)] = path
    return files


def comparable(path, data):
    # Git checkouts may mix CRLF and LF between existing and newly added files.
    return data if path.suffix.lower() in BINARY_EXTENSIONS or b"\0" in data else data.replace(b"\r\n", b"\n")


def publish_bytes(path, data, ids, package):
    if b"\0" in data or path.suffix.lower() in BINARY_EXTENSIONS:
        return data
    data = GUID.sub(lambda match: ids.get(match[0], match[0]), data)
    if path.suffix in {".asmdef", ".cs", ".unity", ".asset", ".prefab", ".anim", ".controller"}:
        data = data.replace(b".EditableSamples", b".Samples").replace(b"EditableAssembly.", b"")
    if path.suffix == ".shader":
        data = data.replace(b"/EditableSamples/", b"/Samples/")
    return data


def sync_sample(package, sample, source_project=ROOT, publish=False):
    relative = Path(sample["path"])
    if len(relative.parts) != 2 or relative.parts[0] != "Samples~" or relative.name == "..":
        raise ValueError(f"Invalid sample path in {package.name}: {relative}")
    source = source_project / "Assets/PackageSamples" / package.name / relative.name
    destination = package / relative
    if not (source / "README.md").is_file() or not list((source / "Scenes").glob("*.unity")):
        raise ValueError(f"Expected README.md and Scenes/*.unity in {source}")
    files = inventory(source)
    ids = {}
    for rel, path in files.items():
        if rel.suffix != ".meta":
            if rel.parent == Path(".") and rel.name != "README.md":
                raise ValueError(f"Put {path} in an asset folder")
            continue
        if not path.with_suffix("").exists():
            raise ValueError(f"Orphan metadata: {path}")
        match = META_GUID.search(path.read_bytes())
        if not match or match[1] in ids:
            raise ValueError(f"Missing or duplicate Unity GUID: {path}")
        ids[match[1]] = paired_guid(match[1])
    for path in source.rglob("*"):
        if path.suffix != ".meta" and not Path(str(path) + ".meta").is_file():
            raise ValueError(f"Missing Unity metadata: {path}")

    changes = mirror(files, destination, lambda path, data: publish_bytes(path, data, ids, package.name), publish)
    # Builders can run in an isolated editor against packages installed from this checkout.
    # Keep that checkout's editable source in step with its distributed copy.
    canonical = package.parents[1] / "Assets/PackageSamples" / package.name / relative.name
    if publish and source.resolve() != canonical.resolve():
        mirror(files, canonical, lambda path, data: data, True)
    return changes


def mirror(files, destination, transform, publish):
    shipped = inventory(destination)
    differences = []
    # Prepare the complete projection before replacing any published content.
    with tempfile.TemporaryDirectory() as staging:
        updates = []
        for rel, path in files.items():
            target = shipped.get(rel)
            if path.suffix.lower() in BINARY_EXTENSIONS and target and target.exists() and filecmp.cmp(path, target, shallow=False):
                continue
            expected = transform(path, path.read_bytes())
            if target and target.exists() and comparable(path, target.read_bytes()) == comparable(path, expected):
                continue
            differences.append(str(rel))
            if publish:
                prepared = Path(staging) / str(len(updates))
                prepared.write_bytes(expected)
                updates.append((prepared, destination / rel))
        obsolete = sorted(shipped.keys() - files.keys())
        differences.extend(f"obsolete {rel}" for rel in obsolete)
        if publish:
            for prepared, target in updates:
                target.parent.mkdir(parents=True, exist_ok=True)
                # Replace each file atomically, including on cross-volume temp directories.
                with tempfile.NamedTemporaryFile(dir=target.parent, delete=False) as tmp:
                    temp_path = Path(tmp.name)
                try:
                    shutil.copyfile(prepared, temp_path)
                    temp_path.replace(target)
                finally:
                    temp_path.unlink(missing_ok=True)
            for rel in obsolete:
                (destination / rel).unlink()
            for folder in sorted(destination.rglob("*"), reverse=True):
                if folder.is_dir() and not any(folder.iterdir()):
                    folder.rmdir()
    return differences


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--publish", action="store_true", help="write the package copies (default: check)")
    parser.add_argument("--source-project", type=Path, default=ROOT, help="host project containing Assets/PackageSamples")
    parser.add_argument("package", nargs="?", help="embedded package ID (default: all)")
    parser.add_argument("sample", nargs="?", help="registered sample folder (default: all in package)")
    args = parser.parse_args()
    checked = 0
    dirty = False
    for package in sorted((ROOT / "Packages").glob("com.jlinkdev.*")):
        if args.package and args.package != package.name:
            continue
        for sample in json.loads((package / "package.json").read_text("utf-8-sig"))["samples"]:
            if args.sample and args.sample != Path(sample["path"]).name:
                continue
            checked += 1
            changes = sync_sample(package, sample, args.source_project, args.publish)
            if changes:
                dirty = True
                print(f"{package.name}/{sample['displayName']}: {', '.join(changes)}")
    if not checked:
        parser.error("No matching registered samples")
    print(f"{checked} sample pairs {'published' if args.publish else 'checked'}.")
    return int(dirty and not args.publish)


if __name__ == "__main__":
    raise SystemExit(main())
