# Changelog

## [Unreleased]

- Standardize installation, quick start, sample, requirements, and limitations documentation; preserve detailed guidance in linked manuals.

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
