# jlinkdev Forcefields

URP forcefield surfaces with Fresnel glow, refraction, depth intersections,
reusable presets, and impact ripples. The package supplies visuals without combat rules.

## Requirements

- Unity `2022.3` or newer.
- URP `14.0.11` or a compatible newer version.
- Enable **Opaque Texture** for refraction and **Depth Texture** for intersection glow.

These are declared requirements, not a test matrix for every editor and platform.

## Installation

In **Window > Package Manager**, choose **Add package from git URL** and paste:

```text
https://github.com/jlinkdev/unity-utilities.git?path=/Packages/com.jlinkdev.forcefields
```

Append `#<tag-or-commit>` to pin a revision. For a local checkout, use **Add package
from disk** and select this package's `package.json`.

## Quick start

1. Choose **GameObject > jlinkdev > Forcefields > Create Forcefield Sphere**.
2. Assign a preset from `Runtime/Presets` to its `Forcefield` component.
3. Add `ForcefieldCollisionEmitter` if physics contacts should create visual hits.
4. For scripted hits, call `AddImpact` on your `Forcefield` reference:

```csharp
// field is a Forcefield; hit is a RaycastHit from your game's targeting code.
field.AddImpact(hit.point, hit.normal, strength: 1f, radius: 0.04f);
```

## Sample

Import **Forcefield Showcase** from the package's **Samples** tab. Open
`Scenes/Forcefield Showcase.unity` and press Play. Click fields, blend presets,
or enable the stress wall. The sample uses your active URP pipeline.
See the [sample guide](<Samples~/Forcefield Showcase/README.md>).

## Limitations

- Initial `0.1.0` release; profile representative scenes on your target hardware.
- Screen-space refraction includes opaque geometry, not other transparent surfaces.
- Missing depth/opaque textures disable their associated features, not the entire effect.
- Generic ripple propagation measures direct distance, not paths along a concave mesh.
- Transparent surface overlap can require sorting adjustments.

## Documentation

[Manual and design](Documentation~/Forcefields.md) · [Performance](Documentation~/Performance.md) ·
[Troubleshooting](Documentation~/Troubleshooting.md) · [Changelog](CHANGELOG.md)

## License

MIT. See [LICENSE.md](LICENSE.md).
