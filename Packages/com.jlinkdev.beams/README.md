# jlinkdev Beams

A composable beam kit with endpoints, curved and branching paths, animated URP
materials, and physics contact events. Gameplay behavior stays in your project.

## Requirements

- Unity 6 (`6000.0` or newer).
- URP `17.0.3` or a compatible newer version; the included materials use URP.
- Unity Physics module (installed as a package dependency).
- The animated samples also use Unity's built-in Animation module; the orbital sample uses Particle System.

These are declared requirements, not a test matrix for every editor and platform.

## Installation

In **Window > Package Manager**, choose **Add package from git URL** and paste:

```text
https://github.com/jlinkdev/unity-utilities.git?path=/Packages/com.jlinkdev.beams
```

Append `#<tag-or-commit>` to pin a revision. For a local checkout, use **Add package
from disk** and select this package's `package.json`.

## Quick start

1. Use an active URP pipeline, then choose **GameObject > jlinkdev > Beams > Continuous Beam**.
2. Move the generated `Beam Target` child to set the endpoint.
3. Add or reorder path modifiers on `Beam` to change its shape.
4. Assign a material from `Runtime/Materials`, or author one using the shader contract.

Subscribe to `BeamPhysicsContacts` events when your game needs contact responses.

## Samples

Import **Beam Kit Demo** from the package's **Samples** tab. Open
`Scenes/Beam Kit Demo.unity` in the imported folder and press Play. Targets animate,
pulses trigger automatically, and the overlay displays contact counts.
See the [sample guide](<Samples~/Beam Kit Demo/README.md>).

Import **Energy Studies** for a side-by-side scene and four standalone prefabs. The trip laser has a pencil-thin red bore. Mining Plasma has a broad amber sheath, Golden Tether follows a moving target with two orbiting filaments, and Tractor Stream sends cyan bands back to the source.

Import **Orbital Descent** for a sweeping column over a basalt basin. The prefab combines three tube layers, a raycast endpoint, surface glow, a cooling trail, sparks, and a drifting thermal plume. Its scene camera has a local URP bloom volume.

Camera captures: [orbital sweep](Documentation~/orbital-descent.webp) · [energy presets](Documentation~/energy-studies.png) · [ribbon and lightning studies](Documentation~/ribbon-studies.png).

On a Beam Ribbon Renderer, enable **Volume Tube** for a capped cross-section with view-dependent optical thickness. Leave it off for a camera-facing ribbon. **Ribbon Flow** is an editable ribbon material and Shader Graph using all four beam subgraphs.

## Limitations

- Release candidate (`1.0.0-pre.1`); validate the target project before shipping.
- Contacts follow CPU paths, not visual shader displacement.
- Branch count, segment count, and physics query frequency affect cost.
- Damage, health, forces, teams, and resource transfer are outside the package.

## Documentation

[Manual](Documentation~/manual.md) · [API](Documentation~/api.md) ·
[Architecture](Documentation~/architecture.md) · [Shader contract](Documentation~/shader-contract.md) ·
[Performance and troubleshooting](Documentation~/performance-and-troubleshooting.md) · [Changelog](CHANGELOG.md)

## License

MIT. See [LICENSE.md](LICENSE.md).
