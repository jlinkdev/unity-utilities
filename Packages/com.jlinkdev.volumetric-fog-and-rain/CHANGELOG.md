# Changelog

## Unreleased

- Consolidate Interactive Fog into this package. Ground Mist, Cumulus, Dry Ice, their setup tools and the Mist Sanctuary sample are included in one install. Existing interactive-fog assembly names, serialized types and asset GUIDs are preserved. [Ground fog guide](Documentation~/InteractiveFog/GroundFog.md) · [Previous standalone changelog](Documentation~/InteractiveFog/StandaloneChangelog.md)

- Bring the observation shelter into Rain Laboratory. Two selectable stations share one courtyard and storm. The shelter shows wet fixed glass beside a full-height open bay. The Create Volume Laboratory command still generates the minimal volume example.

- Add an opaque wet-surface shader with rain film, beads and runoff. Rain Laboratory uses it on an editable stone courtyard with a photographed overcast sky, shared amber fixtures and localized splash particles.

- Add wet-glass refraction with retained beads and sliding runoff, plus reusable particle prefabs for impact rings and droplet spray.

- Add optional atmospheric scattering and transmission for Interactive Fog. Fog Laboratory enables the integration on its camera renderer.

- Rebuild Fog Laboratory as an editable dawn courtyard with gridded PBR stone, a sheltered viewpoint, atmospheric haze and a depth-integrated ground-mist volume.

## 0.4.1 - 2026-09-25

- Preserve the active color attachment's MSAA sample count in the fog/rain composite. Early injection now remains compatible with the existing depth attachment when transparent geometry renders afterward.
- Add 1x and 8x MSAA rendering regressions for both early injection points, including transparent blending, opaque-color capture, and actual color/depth attachment sample counts.

## 0.4.0 — 2026-09-25

- Rename the package ID/folder to com.jlinkdev.volumetric-fog-and-rain, namespace and assemblies to VolumetricFogAndRain, and public branding/menu paths to Volumetric Fog and Rain. Preserve asset GUIDs and add Unity type migration metadata.
- Add supported render timing choices and explicit premultiplied transparent-output compositing, retaining the existing scene-alpha mode and timing by default.
- Add world-unit inclusion/exclusion feathering, fog height falloff, independent combined-mode fog distances, integrated fog noise movement, and optional main-directional-light scattering.
- Add camera-owned runtime settings and a profile appearance blending API with explicit discrete-setting policies.
- Extend the fog demo and GPU tests for new controls, actual transparents, refraction-style opaque captures, HDR/alpha output, post-processing order, and runtime blending.
- Standardize package quick starts, sample guides, deeper manuals, migration instructions, and release checks.

## 0.3.0 — 2026-09-25

- Add Rain And Fog, Rain Only, and Fog Only profile modes. Fog Only compiles out streak traversal and integrates the full visible volume from the near plane; Rain Only compiles out fog integration.
- Add independent fog density, color, brightness and scattering. Existing combined profiles retain rain-linked haze by default; Independent Fog Settings opts into separate appearance controls.
- Make Fog Only independent of rain density, geometry, motion, transition distances and cell budget, including pass eligibility and Max Distance.
- Adapt the profile inspector to each mode and add Fog Laboratory, mode-switching/depth/exclusion render regressions, documentation and fog benchmark cases.

## 0.2.1 — 2026-09-19

- Distance fields commit on Enter/focus loss and no longer silently rewrite other distance fields during inspector validation. Invalid ordering and insufficient traversal budgets are explained in the profile inspector; rendering still applies safe limits without modifying the asset.
- Remove the hidden 65% Mid/Far cap. With sufficient budget, the authored Mid Distance is honored. Budget-limited rays shorten Mid and Far proportionally.
- Seal exclusion boundaries against flush walls/floors with a 1 mm world-space tolerance baked into their inverse transforms on the CPU. No added per-pixel padding calculation.
- Regression tests reproduce and prevent inspector distance rewriting and animated rain specks on flush/transformed shelter surfaces; verify late fade starts remain honored.

## 0.2.0 — 2026-09-19

- Skip density-noise evaluation for capsules with zero pixel contribution, and skip zero-contribution haze work. Full resolution and existing visual defaults are preserved.
- Add bounded Rain Volume boxes and Rain Exclusion Volume boxes, with transformed interval clipping of both streaks and haze. Overlapping rain regions share one field.
- Add volume Scene view resize handles, camera layer/frustum filtering, and a separate Volume Laboratory sample and generator.
- Extend GPU rendering regression coverage for dry interiors, overlapping boxes, transformed bounds, silhouettes, and thin-volume haze.
- Add reproducible development-only GPU benchmarks with the original shader reference and a measured performance report.

## 0.1.0 — 2026-09-19

- Rain Laboratory now activates its pipeline only in Play mode and restores the prior Graphics/Quality settings on exit or scene unload.

- Initial Unity 6 URP Render Graph prototype, packaged independently for UPM.
- Two deterministic, animated world-space capsule grids with bounded analytic DDA traversal.
- Opaque-depth occlusion, perspective/orthographic rays, subpixel filtering, and distant haze.
- Reusable profiles, per-camera intensity/frozen time/debug views, and optional Scene view preview.
- Non-destructive renderer setup and self-contained laboratory scene generator.
- EditMode and GPU render regression tests, algorithm notes, and performance evaluation guide.
