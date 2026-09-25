# Contributing and releasing packages

## Package boundaries

Keep each package independently importable. Runtime code and assets belong in
`Runtime`; editor-only tooling belongs in `Editor` with an Editor-only assembly.
Tests belong in `Tests` with test assemblies. Samples must work without the host
project's `Assets/PackageDevelopment` files. Keep host benchmarks, sample builders,
and integration fixtures outside the distributable packages.

Declare UPM dependencies in `package.json` and assembly references in `.asmdef`
files when a package consumes another package. Do not rely on another utility
merely being embedded in this repository. Preserve `.meta` files and GUIDs when
moving assets; commit metadata for new Unity-imported assets. Unity ignores
`Documentation~` and `Samples~` until appropriate tooling or sample import uses them.

## Documentation convention

Keep the README focused on these sections, in order:

1. **Requirements**: declared Unity, pipeline, platform, and dependency requirements.
2. **Installation**: exact Git subfolder URL and local `package.json` option.
3. **Quick start**: a few setup steps, with a complete example when code is required.
4. **Sample**: Package Manager sample name, scene to open, controls, pipeline setup.
5. **Limitations**: current maturity and practical constraints.
6. **Documentation**: links to deeper guidance and the changelog.
7. **License**: the package's actual license notice.

Detailed API, architecture, performance, and troubleshooting explanations belong
in `Documentation~`. Small packages can use a single manual. Describe implemented
behavior in present tense and mark proposed behavior explicitly. Do not describe
minimum requirements as tested compatibility without validation evidence.

Every declared sample needs a README and a usable scene. Start with running the
included scene, then explain optional reconstruction or customization. Document
any global pipeline changes. When sample authoring copies exist, update their
guides too so publishing the sample does not overwrite newer instructions.

## Static checks

Run `python scripts/validate-packages.py` and `git diff --check` from the repository
root. The Python check requires Python 3.9 or newer and no third-party dependencies. It checks all owned
packages, registered samples, local Markdown destinations, and assembly JSON. It
can be called from CI; it does not launch Unity, access the network, resolve Git
revisions, or validate external URLs.

These checks do not validate C# examples, shader compilation, rendering, asset
references, or API compatibility. Verify documented menu names and examples against
the implementation when editing them.

## Release checks

Before publishing a version:

- Update the package changelog and version intentionally. Documentation under
  **Unreleased** does not imply that a new release is already published.
- Run the static checks and review the package license and dependency metadata.
- Install the distributable package in a clean project using the minimum declared
  editor and dependencies. Test the current host editor separately if different.
  Record exact editor, URP, OS, graphics API, target platform, and results; record
  gaps instead of implying they passed.
- Import each registered sample through Package Manager, open the documented scene,
  and follow its quick start. Check for missing assets/scripts, setup errors, and
  unintended project-setting changes. Confirm temporary settings are restored.
- Run applicable package EditMode and PlayMode tests. Where tests are absent, record
  manual coverage and the gap; Object Pooling currently has no automated suite.
- Build and run a representative player, including shaders and serialized assets.
  Exercise relevant lifecycle cases such as disable, scene unload, and repeated use.
- Measure performance-sensitive changes on target hardware. Visual quality and
  runtime cost need separate checks; a passing structure check proves neither.
- Verify the exported package contains its docs, licenses, metadata, tests, and
  samples without depending on host-only files. Reinstall that artifact for the
  final smoke check, then publish and provide a pinned Git revision or archive.

Keep validation records with the package when available. Volumetric Rain's
[validation record](Packages/com.jlinkdev.volumetric-rain/Documentation~/validation.md)
is an existing example; its results apply to that package and those tested versions.
