# jlinkdev Forcefields

Professional, renderer-focused forcefields for the Universal Render Pipeline. The package combines Fresnel energy, screen-space refraction, depth intersections, procedural patterns, and allocation-free impact ripples without imposing health, damage, or combat rules.

## Requirements

- Unity 2022.3 or newer
- Universal Render Pipeline 14 or newer
- **Opaque Texture** enabled for refraction
- **Depth Texture** enabled for intersection glow

The effect still renders its surface, Fresnel, pattern, and impacts when either camera texture is unavailable.

## Quick start

1. Choose **GameObject > jlinkdev > Forcefields > Create Forcefield Sphere**.
2. Assign any supplied `ForcefieldPreset`.
3. Trigger visual hits from your own systems:

```csharp
using jlinkdev.UnityUtilities.Forcefields;

forcefield.AddImpact(hit.point, hit.normal, strength: 1f, radius: 0.04f);
```

Add `ForcefieldCollisionEmitter` when ordinary physics contacts should produce visual hits automatically.

## Design

`Forcefield` writes per-instance state through `MaterialPropertyBlock`, allowing many shields to share the supplied material. Impacts are stored in a fixed ring buffer and evaluated in the shader; no impact objects, coroutines, or material clones are created. Impact positions use the forcefield root's local space, so ripples remain attached while a field moves.

Use **Spherical** propagation for sphere-like shells. Use **Surface Distance** for arbitrary convex closed meshes. Concave meshes can produce visually unexpected propagation because the generic mode measures direct world-space distance rather than mesh geodesics.

## Presets

Six production-ready starting points are included under `Runtime/Presets`: Clean Energy, Hex Defense, Plasma Containment, Stealth Field, Overloaded, and Minimal Mobile. Presets contain only effect configuration and can be applied or blended at runtime.

```csharp
forcefield.BlendToPreset(overloadedPreset, 0.75f);
forcefield.Intensity = 0.65f;
forcefield.ClearImpacts();
```

## Sample

Import **Forcefield Showcase** from Package Manager. Open the included scene and enter Play mode to explore every preset, click-to-impact interaction, automatic impacts, and a stress-test wall.

Additional guidance is available under `Documentation~`.
