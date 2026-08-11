# Forcefields manual

## Rendering setup

The supplied shader is transparent and unlit. It samples URP's camera opaque texture for refraction and the camera depth texture for intersections. Enable both settings on the active Universal Render Pipeline asset for the complete effect. Camera overrides can still disable either texture on individual cameras.

Transparent objects are not present in the opaque texture, so a forcefield refracts opaque scene geometry but not other transparent surfaces. This is standard screen-space refraction behavior. Overlapping transparent forcefields can also require explicit renderer sorting priorities in complex scenes.

## Mesh authoring

Use a closed mesh with outward-facing normals. Smooth normals generally produce the cleanest Fresnel and refraction. Spherical propagation expects the forcefield root to be near the visual center of the shell. Non-uniform scale is supported; spherical distance uses the renderer's largest world-space extent as its propagation radius.

The shader is double-sided. Back faces receive a configurable opacity multiplier so a camera inside the volume still sees the field.

## Multiple renderers

A single `Forcefield` may drive multiple renderers. Impact positions live in the controller root's local space and are transformed into the current world pose in the shader. This supports moving compound effects while retaining one impact history and one preset.

All assigned renderers should use the supplied Forcefield material. Other material properties already present in their property blocks are preserved.

## Impact capacity

Capacity choices are 4, 8, 16, and 32. The buffer overwrites its oldest slot after reaching capacity. Reducing or changing capacity clears current impacts. Sixteen is recommended for desktop and console presentation; four or eight is appropriate for inexpensive background fields.

Each rendered fragment evaluates active impact slots. Capacity therefore affects GPU cost even though the CPU-side buffer is allocation-free.

## Preset quality

- **Low** omits refraction, intersections, procedural noise, and patterns.
- **Medium** enables ordinary refraction, noise, patterns, and intersections.
- **High** additionally enables chromatic refraction splitting.

Individual preset switches remain authoritative. Selecting a higher quality does not force a disabled feature on.

## Runtime integration

The core API intentionally has no damage semantics. Convert whatever meaning your game uses into a position, normal, strength, and initial radius.

```csharp
field.AddImpact(worldPosition);
field.AddImpact(worldPosition, strength);
field.AddImpact(worldPosition, worldNormal, strength, radius);
```

Subscribe to `ImpactAdded` to coordinate audio, particles, decals, or camera feedback without making those systems package dependencies.

`ForcefieldCollisionEmitter` is an optional convenience adapter. It filters collision layers, maps relative velocity to visual strength, and forwards up to four contact points.
