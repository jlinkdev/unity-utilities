# Volumetric Rain manual

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

## Rain and fog modes

Choose **Render Mode** on the profile: **Rain And Fog**, **Rain Only**, or **Fog Only**.
Fog Only uses the same volume boxes and exclusions, renders from the near plane,
and skips all streak traversal. It has its own fog density, color, brightness and
scattering controls. Rain cell size and transition distances have no effect on it.

Existing profiles retain their rain-linked distant haze. In Rain And Fog mode,
enable **Independent Fog Settings** to separate fog appearance from rain.
The mode is per profile; boxes within one camera share it. See [fog setup](fog.md).

Import **Fog Laboratory**, or run **Tools > jlinkdev > Volumetric Rain > Create Fog
Laboratory**, for a fog-only volume and clear canopy demonstration.

## Bounded rain and interiors

Set **Rain Profile > Extent > Volumes Only** and add **Rain Volume** to an empty
GameObject. Its box defines where rain and haze exist. Add **Rain Exclusion Volume**
to carve out a dry room or canopy. Exclusions also work with the original Unbounded
mode. They remove foreground rain indoors while preserving rain visible outside.

Boxes support rotation, scale, runtime movement, and Scene view resize handles.
Overlaps share one field. The current limit is 4 visible rain boxes and 8 visible
exclusions per camera. See [volume authoring and limits](volumes.md).

Import the **Volume Laboratory** sample, or run **Tools > jlinkdev > Volumetric
Rain > Create Volume Laboratory**, then press Play for a dry-canopy demonstration.

## Controls

| Control | Meaning |
| --- | --- |
| Extent | Unbounded global field, or union of Rain Volume boxes; exclusions apply in both modes |
| Rain density / camera intensity | Nested streak subsets; intensity also controls fog. Linked haze follows rain density; independent fog uses Fog Density |
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
| Fog extinction / scattering | Extinction per metre and approximate in-scattered radiance multiplier; Fog Only and independent combined fog use their own appearance controls |
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

See [measured performance](performance.md), [algorithm](algorithm.md), [evaluation guide](evaluation.md),
and [validation record](validation.md).
