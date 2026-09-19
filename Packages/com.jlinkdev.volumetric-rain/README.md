# Volumetric Rain

An experimental, particleless rain renderer for **Unity 6 / URP 17, desktop, Render Graph**.
A procedural world-space field supplies analytic capsule streaks, globally or inside
authored box volumes; a separate
low-frequency density integral supplies distant haze. No drop GameObjects, particle
buffers, simulation, compute dispatches, or VFX Graph dependencies are used.

## Install

Use Package Manager > Add package from disk and select this package's `package.json`,
or install the supplied `.tgz` with Add package from tarball. Once these changes are
published to the repository, the Git URL is:

```text
https://github.com/jlinkdev/unity-utilities.git?path=/Packages/com.jlinkdev.volumetric-rain
```

Only URP is required; this package does not depend on the other jlinkdev utilities.
The minimum declared editor is Unity 6000.0; see `Documentation~/validation.md` for
the versions and platforms actually tested.

## Setup

1. Use a **Universal Renderer** with Render Graph enabled (Compatibility Mode off).
2. Select the renderer data asset used by the camera. Run **Tools > jlinkdev >
   Volumetric Rain > Add Feature to Selected Renderer**, or add **Rain Renderer
   Feature** in its inspector. Save the renderer asset; it retains the shader reference
   so the hidden shader is included in player builds.
3. Create a profile with **Assets > Create > jlinkdev > Volumetric Rain > Profile**.
4. Add **Volumetric Rain Camera** to a base Game camera and assign that profile.
5. Adjust density and brightness first. Optionally assign **Scene View Profile** on
   the renderer feature to preview rain in Scene view.

The feature requests scene depth itself. It composites before post-processing so
exposure, tone mapping, and bloom can affect the rain. It preserves the source alpha.
Overlay, reflection, preview, and XR cameras are skipped.

## Try the laboratory

Import **Rain Laboratory** from the package Samples tab and open its scene. Alternatively,
run **Tools > jlinkdev > Volumetric Rain > Create Rain Laboratory**. It creates a unique
folder under `Assets/VolumetricRain` containing a scene, profile, material, renderer,
and URP pipeline. The normal save prompt protects unsaved scene work before opening the new scene.
Open the scene on its own and press **Play**. Its **Rain Demo Pipeline** component
temporarily assigns the demo pipeline to Graphics and the active Quality level, then
restores previous settings on disable, scene unload, or exiting Play mode.
This opt-in demo component changes the pipeline globally while active; use one
laboratory at a time. Edit-mode preview still requires manually assigning the demo
pipeline or adding the rain feature to your current renderer. The component does
not change settings simply by opening the scene.

Select Rain Camera to freeze time, adjust intensity, or switch debug views:

- **Composite**: scene with rain and haze.
- **Streaks**: isolated streak coverage, black background.
- **Haze**: integrated haze opacity.
- **Traversal Cost**: visited cells relative to the configured cap; blue is low,
  red/yellow is high. This is work count, not GPU milliseconds.

Use Scene view navigation with its profile assigned, or move Rain Camera while its
time is frozen, to inspect parallax. Play mode uses scaled game time by default.

## Bounded rain and interiors

Set **Rain Profile > Extent > Volumes Only** and add **Rain Volume** to an empty
GameObject. Its box defines where rain and haze exist. Add **Rain Exclusion Volume**
to carve out a dry room or canopy. Exclusions also work with the original Unbounded
mode. They remove foreground rain indoors while preserving rain visible outside.

Boxes support rotation, scale, runtime movement, and Scene view resize handles.
Overlaps share one field. The current limit is 4 visible rain boxes and 8 visible
exclusions per camera. See [volume authoring and limits](Documentation~/volumes.md).

Import the **Volume Laboratory** sample, or run **Tools > jlinkdev > Volumetric
Rain > Create Volume Laboratory**, then press Play for a dry-canopy demonstration.

## Controls

| Control | Meaning |
| --- | --- |
| Extent | Unbounded global field, or union of Rain Volume boxes; exclusions apply in both modes |
| Density / camera intensity | Nested, smoothly activated streak subsets and haze strength; either zero skips the pass |
| Cell size | Spatial frequency of candidates; smaller cells add detail and cost independently of density |
| Streak length / width | Metres; width is capsule radius. Values are clamped for cell containment |
| Direction / fall speed / wind | Normalized fall direction times speed, plus world-space wind velocity |
| Near fade | Soft fade over the first metres after the camera near plane |
| Mid / far distance | Start/end of discrete-streak fade; haze takes over across the same interval |
| Max distance | Depth-clipped integration extent, including sky rays |
| Max cell steps | 16–256 visits per grid; two grids are always used |
| Haze steps | 4–32 density integration samples; volume fragments may add up to 11 samples |
| Noise scale / strength | World-anchored 3D value noise controlling broad rain distribution |
| Brightness / color | Unlit rain radiance shared by streaks and haze |
| Haze extinction / scattering | Extinction per metre and approximate in-scattered radiance multiplier |
| Seed | Deterministic world-space field identity |

Distance fields commit on **Enter** or focus loss. Editing one distance does not
rewrite the other distances. Keep Near < Mid < Far <= Max; the inspector warns
about invalid ordering and shows the temporary safe values used for rendering.
With sufficient cell budget, Mid Distance is honored without a fixed Mid/Far ratio.
When the budget shortens Far Distance, Mid Distance shortens proportionally too.

Profiles are shared assets. Clone a profile before making per-camera runtime edits.
Call `Sanitize()` after programmatically editing its fields; rendering also clamps
geometric ranges without modifying the asset. Changing cell size, seed, direction,
or wind can reorient/reseed the apparent field; these are authoring controls, not a
smooth weather-transition system.

## Scope and tradeoffs

The renderer proves the technique, not a guaranteed performance win over GPU particles.
Full-resolution per-pixel traversal can be expensive. Work is bounded by pixel count,
range, grid spacing, and iteration budget. Density never changes loop limits, but
more occupied cells execute more capsule/noise work, so GPU time is **not constant**.

Streaks are approximately filtered, not temporally accumulated. Fast rain can still
alias; there is no motion blur, TAA history, or rain motion-vector output. The field
uses one bulk velocity with randomized length, radius, brightness, and slight tilt;
per-drop speed variation is deliberately deferred. Two offset, elongated grids
reduce alignment, but very long/wide streaks can expose the cell-containment pattern.

Opaque and depth-writing alpha-clipped geometry occlude rain. Ordinary transparent
surfaces do not write the depth used by this effect. Depth occlusion also does not
prevent rain in front of the camera under roofs: author Rain Exclusion Volumes for shelter. Haze is a simple independent approximation; it
does not integrate with other fog systems or shadowed lighting. Avoid double fogging.

See [measured performance](Documentation~/performance.md), [algorithm](Documentation~/algorithm.md), [evaluation guide](Documentation~/evaluation.md),
and [validation record](Documentation~/validation.md).
