# jlinkdev Volumetric Rain

Particleless world-space rain and fog for Unity 6 URP. Render globally or inside
box volumes, with dry/clear exclusions and scene-depth occlusion.

## Requirements

- Unity 6 (`6000.0` or newer), desktop.
- URP `17.0.3` or a compatible newer version, using a **Universal Renderer**.
- **Render Graph enabled** (Compatibility Mode off). No VFX Graph dependency.

These are declared requirements, not a test matrix for every editor and platform.

## Installation

In **Window > Package Manager**, choose **Add package from git URL** and paste:

```text
https://github.com/jlinkdev/unity-utilities.git?path=/Packages/com.jlinkdev.volumetric-rain
```

Append `#<tag-or-commit>` to pin a revision. For a local checkout, use **Add package
from disk** and select this package's `package.json`.

## Quick start

1. Select the renderer data asset used by your camera. Run **Tools > jlinkdev > Volumetric Rain > Add Feature to Selected Renderer** and save the asset.
2. Create a profile with **Assets > Create > jlinkdev > Volumetric Rain > Profile**.
3. Add **Volumetric Rain Camera** to a base Game camera and assign the profile.
4. Choose **Rain And Fog**, **Rain Only**, or **Fog Only** on the profile.
5. For a bounded effect, set **Extent > Volumes Only** and add a **Rain Volume** to an empty GameObject. Add **Rain Exclusion Volume** to carve out shelter.

The original component names also apply to fog. All boxes viewed by one camera
share its profile and mode. Assign **Scene View Profile** on the renderer feature
for an editor preview. The feature requests scene depth automatically.

## Sample

Import a laboratory from the package's **Samples** tab, open its matching scene
alone, and press Play:

| Sample | Demonstrates |
| --- | --- |
| [Rain Laboratory](<Samples~/Rain Laboratory/README.md>) | Global rain, depth occlusion, and debug views. |
| [Volume Laboratory](<Samples~/Volume Laboratory/README.md>) | Bounded rain and a dry canopy. |
| [Fog Laboratory](<Samples~/Fog Laboratory/README.md>) | Fog without streaks and a clear interior. |

Each demo temporarily activates its supplied pipeline during Play and restores
the previous settings afterward. This affects all loaded scenes; run one demo at
a time. Opening a scene does not change the edit-mode pipeline.

## Limitations

- Prototype; performance depends on resolution, range, grid spacing, and budgets.
- Up to **4 visible inclusion boxes and 8 exclusions per camera**; see volume limits.
- Opaque/depth-writing geometry occludes the effect; ordinary transparent surfaces do not.
- No XR, Built-in/HDRP, or Compatibility Mode support. Mobile is outside the validated scope.
- Fog uses approximate scattering without scene lights or shadows; it does not integrate with other fog systems.
- Streak filtering has no temporal history or motion vectors; fast rain can alias.

## Documentation

[Controls and setup](Documentation~/manual.md) · [Volumes](Documentation~/volumes.md) ·
[Fog](Documentation~/fog.md) · [Algorithm](Documentation~/algorithm.md) ·
[Performance](Documentation~/performance.md) · [Evaluation](Documentation~/evaluation.md) ·
[Tested versions and validation](Documentation~/validation.md) · [Changelog](CHANGELOG.md)

## License

MIT. See [LICENSE.md](LICENSE.md).
