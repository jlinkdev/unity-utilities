# Procedural assets · Phase 2

Phase 2 reached `0.2.0-preview.3` for Procedural Textures and `0.2.0-preview.1` for Procedural Meshes. Each package has one Create entry. Choose the generator in the recipe Inspector. Texture recipes also have a shared **Processing** foldout for coordinate transforms, domain warp, isotropic or directional blur, and edge detection.

- [Texture generator catalog and baked examples](../Packages/com.jlinkdev.procedural-textures/Documentation~/generators.md)
- [Mesh generator catalog and baked icons](../Packages/com.jlinkdev.procedural-meshes/Documentation~/generators.md)

## Verified in Unity 6000.0.58f1 on Windows

| Check | Result |
| --- | --- |
| Isolated package installs | The texture-only project passed 62 Edit Mode cases. The mesh-only project passed 19 in the foundational generator run. The suites include all generator IDs and the Phase 1 lifecycle tests. |
| Shared texture processing | All 34 generators produced varying output with warp, blur and edge detection combined. Tests checked seeded displacement, direction, boundary sampling, transform-dependent wrapping, preview scaling, single-texel axes, runtime queries and Inspector Apply. The published Texture Atlas imported into a single-package project and rendered all five scenes, including the eight Processing comparisons. |
| Legacy assets and switching | Literal schema-1 recipes imported and stayed untouched during draft edits. Tests switched generator, adjusted settings, switched away and back, and applied. Current writes use texture schema 4 and mesh schema 2. A saved material and a saved prefab retained their references and the native GUID/file-ID pairs were unchanged. |
| Texture output | All 34 definitions produced varying output and matched the same-resolution preview. Periodicity tests covered both axes individually and together. A non-square ring test checked its shape proportions and signed-distance boundary. |
| Mesh output | All 11 definitions produced indexed faces with outward winding, normals and UVs. Additional checks covered the icosphere seam, partial-annulus UVs, tapered tips, separate cap/cut-face submeshes and world-unit cylinder mapping. |
| Graphical Editor | The real texture picker accepted a search for Star and applied the selection. The mesh Inspector previewed a draft switch to Torus. Unity returned oblique cached asset icons for all 11 mesh outputs, including the planar surfaces. |
| Batch and worker import | Graphics-free batch import generated the full catalog. Separate asynchronous worker jobs ran the actual importers on representative switched recipes. |
| Saved consumer and Windows player | A prefab originally using value noise and a cylinder rendered with simplex noise and a UV sphere after switching the same assets. The Direct3D 11 player verified the saved native references and sampled shaded pixels from a camera render. |

For the package suites, install Unity Test Framework, add the package ID to the consumer manifest's `testables`, and run Edit Mode tests. Testing covered the built-in renderer and the Windows x64 player. The exact host editor version (6000.3.21f1), other player targets and performance at maximum generator settings remain unverified.
