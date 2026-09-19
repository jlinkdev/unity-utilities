# Validation record — 2026-09-19

## 0.1.0 baseline validation

- Unity **6000.3.21f1**, URP **17.3.0**, Windows: C# and shader import succeeded;
  **5/5 EditMode tests passed**.
- Clean independent Unity **6000.0.58f1**, URP **17.0.3** project: package installed
  as a local UPM dependency, with no other jlinkdev packages; **5/5 tests passed**.
- Installed the exported `.tgz` in that independent project; UPM recorded
  `source: local-tarball` and **5/5 tests passed** again.
- GPU render regression test: nonzero rain changes a sky image; a frozen frame is
  repeatable; time advancement and camera translation change streak placement;
  orthographic rendering produces rain; an opaque near wall occludes it.
- Generated the distributed laboratory assets and scene with Unity 6000.0.58f1 /
  URP 17.0.3, using the editor setup tool.
- Built a Windows x64 Development player with Unity 6000.3.21f1. Build log confirms
  compilation and serialization of `Hidden/jlinkdev/Volumetric Rain`.
- Started that player in batch mode at 960x540 on **D3D11 / NVIDIA GeForce RTX 2060
  SUPER**. Startup completed without logged rendering errors; stopped the smoke
  player after inspection. This is a startup check, not a performance measurement.
- Visually inspected the GPU test's 384x216 frozen render: dense distinct streaks,
  apparent size variation, and far haze are present.

Development artifacts are under the repository's ignored `Logs` directory:
`volumetric-rain-tests.xml`, `volumetric-rain-minimum-tests.xml`,
`volumetric-rain-tarball-tests.xml`, `volumetric-rain-build.log`, `volumetric-rain-player.log`, and
`VolumetricRain/rain-perspective.png`. Test output and built players are not shipped
inside the UPM package.

## Demo pipeline follow-up

- Unity 6000.0.58f1: **3/3 PlayMode tests passed** for the opt-in demo pipeline
  component. Tested activation, disable/destroy restoration, quality-level changes,
  and an unconfigured component leaving settings untouched.
- Both saved demo scenes and the scene generator include the component. The core
  renderer continues to use the project pipeline unless the demo component is present.
- Focused results: `Logs/volumetric-rain-demo-tests.xml`.

## 0.2.0 performance and volumes

- Independent Unity **6000.0.58f1 / URP 17.0.3 / D3D11**: **5/5 package EditMode
  tests passed**, plus the development GPU benchmark (**6/6** in the combined run).
  UPM installed the exported **0.2.0 tarball**, with `source: local-tarball`.
- **3/3 PlayMode tests passed** against that tarball, verifying demo pipeline
  activation/restoration and quality-level changes.
- Expanded GPU render assertions cover bounded mode with no boxes, an outside
  camera viewing a box, duplicate overlapping rain/exclusion boxes, dry interiors
  with outdoor rain visible, unbounded exclusions, culling layers, capacity
  overflow, rotated/nonuniform/negative-scale boxes, parallel orthographic rays,
  dry pixels outside silhouettes, and thin distant haze volumes.
- Built the new Volume Laboratory into a Windows x64 Development player with
  Unity 6000.0.58f1. Both rain shader variants compiled without shader warnings or
  errors. D3D11 player startup completed without rendering errors.
- Captured and visually inspected the new canopy demo at 960x540. The foreground
  is dry while outdoor rain, world bounds, geometry occlusion, and haze remain.
- GPU profiling and exact original/final capture comparisons are documented in
  [performance.md](performance.md). No quality-reducing defaults were introduced.

Artifacts: `Logs/rain-0.2.0-tarball-edit-tests.xml`,
`Logs/rain-0.2.0-tarball-play-tests.xml`, `Logs/rain-volume-build-final.log`,
`Logs/rain-volume-player.log`, `Logs/VolumetricRain/volume-demo.png`.
The 0.2.0 changes were tested in the isolated minimum-version project; the host
Unity 6000.3 editor was left open and its current scene was not replaced.

## 0.2.1 authoring and exclusion fixes

- Added regressions before implementation: the previous code failed two tests.
  Editing Mid Distance to 35 rewrote Far Distance from 30 to 35.1; flush-surface
  rendering produced 192 leaked pixel samples over 72 captures.
- The exported **0.2.1 UPM tarball** passes **7/7 EditMode tests** in the independent
  Unity 6000.0.58f1 / URP 17.0.3 / D3D11 project. The flush-wall/floor test now
  reports zero leaked samples, with rain still visible when shelter is removed
  or the wall is opened. Poses include a floor and a rotated, translated,
  nonuniformly scaled room over 24 animation times each.
- Serialized inspector edits preserve other authored distances. Explicit runtime
  Sanitize remains available. GPU coverage verifies Mid 55 / Far 70 honors the
  requested late transition when sufficient traversal budget is available.
- Result artifacts: `Logs/rain-boundary-before.xml`, `Logs/rain-0.2.1-tests.xml`.
  The performance table and player-build records above are for 0.2.0; they were
  not remeasured/rebuilt for this patch.

## Still requires evaluation

- Matched GPU timing against particle/VFX rain at target resolution and visual density.
- Human assessment in motion: temporal aliasing, cell patterns, extreme camera angles,
  long-running sessions, and the artistic streak/haze transition.
- D3D12, Vulkan, Metal, dynamic resolution, deferred rendering, and camera stacking.
- XR, mobile, Built-in pipeline, HDRP, and URP Compatibility Mode are not supported.

Passing tests establish basic rendering behavior and package importability; they do
not establish production visual quality or a performance advantage over particles.

## Reproduce

In the host repository, run:

```text
unity test . --mode EditMode --filter jlinkdev.UnityUtilities.VolumetricRain.Tests --output Logs/volumetric-rain-tests.xml
unity run . -- -executeMethod jlinkdev.UnityUtilities.VolumetricRain.Development.RainBuildValidation.Build -logFile Logs/volumetric-rain-build.log
```

For a package installed as a local/Git dependency in another project, add
`"testables": ["com.jlinkdev.volumetric-rain"]` to that project's `Packages/manifest.json`
and install Unity Test Framework to expose the package tests. The development-only
player builder lives in this repository's `Assets/PackageDevelopment` and is not
part of the distributed package.
