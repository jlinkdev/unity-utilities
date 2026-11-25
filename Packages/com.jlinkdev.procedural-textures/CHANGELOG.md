# Changelog

## Unreleased

- Add the Resolution scene to Texture Atlas, with matching full-field and center-crop views at 64 × 64 and 512 × 512.
- Add scalar, RGB and individual channel views, selectable preview resolution, zoom, pan and animated UV tiling. Seed rolls, recipe reuse and resets edit the draft.
- Add shared coordinate transforms, seeded directional domain warp, isotropic and directional blur, and Sobel edge detection. Schema 4 adds processing controls to every generator and the Texture Atlas includes a Processing comparison scene.
- Add the Texture Atlas sample with editable recipes for all 34 generators and a shader-driven LUT comparison.
- Edit multi-stop color and opacity ramps, save interpolation settings, and configure endpoint-sampled 1D LUTs. Existing two-color assets open with their original endpoint colors.
- Add the Phase 2 noise, gradient, mask and pattern catalog, an in-asset generator picker and the `TextureGenerator` runtime entry point.
- Read schema 1 sources and upgrade on explicit Apply. Preserve dimensions, draft settings and references when switching generators.

- Add the Value Noise recipe, CPU generator, native asset importer and draft Inspector.
