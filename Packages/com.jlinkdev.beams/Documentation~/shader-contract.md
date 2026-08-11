# Shader contract

`BeamRibbonRenderer` writes the following mesh channels:

| Channel | Value |
| --- | --- |
| Position | Camera-facing ribbon vertex in renderer-local space |
| Normal | Transported strand deformation axis |
| Tangent.xyz | Strand tangent |
| UV0.x | Normalized distance along the strand |
| UV0.y | Ribbon side from -1 to 1 |
| UV1.x | World-space distance along the strand |
| UV1.y | Authored beam width |
| UV2.x | Strand seed |
| UV2.y | Strand index |
| Color | Per-vertex tint/mask reserved for renderers and modifiers |

Compatible renderers publish `_BeamColor`, `_BeamIntensity`, `_BeamLength`,
`_BeamTime`, `_BeamAge`, `_BeamSeed`, `_BeamPulsePosition`, and
`_BeamActivation` through a `MaterialPropertyBlock`.

`Runtime/Shaders/BeamGraphFunctions.hlsl` is usable by Shader Graph Custom
Function nodes. It provides beam coordinates, endpoint masks, seeded noise,
vertex-noise offsets, directional flow, pulses, and core/halo masks.

Four native wrappers under `Runtime/ShaderGraph/Subgraphs` expose the most common
compositions without requiring a hand-authored Custom Function node:

| Subgraph | Purpose |
| --- | --- |
| Beam Endpoint Mask | Soft opacity at the start and end of a strand |
| Beam Flow | Directional, time-driven pattern motion along a strand |
| Beam Pulse | A controllable moving pulse mask |
| Beam Core Halo | Separate ribbon core and halo masks |

Use the mesh channels above as the inputs to these subgraphs. They intentionally
return masks and coordinates rather than a complete material, so a project keeps
control of color, blending, distortion, and the final Shader Graph target.
