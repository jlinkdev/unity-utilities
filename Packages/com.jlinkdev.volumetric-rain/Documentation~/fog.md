# Fog without rain

Set the camera's **Rain Profile > Render Mode** to **Fog Only**. For a local fog
bank, select **Extent > Volumes Only** and add a **Rain Volume** component to an
empty GameObject. Despite its original name, this box bounds both rain and fog.
Use **Rain Exclusion Volume** to cut out clear areas. The existing renderer feature,
camera component, depth occlusion, box handles, layers and volume limits all apply.

## Modes

| Mode | Behavior |
| --- | --- |
| Rain And Fog | Original near streaks and distant haze. Existing profiles default to this mode. |
| Rain Only | Streaks with their existing distance fade; fog integration is compiled out. |
| Fog Only | Fog throughout each volume, from the camera near plane to Max Distance. Streak traversal is compiled out. |

The mode is selected per camera profile. All inclusion/exclusion boxes seen by that
camera share it; independently selecting rain for one box and fog for another in
the same camera is not supported. No second renderer feature is needed.

## Fog controls

- **Fog Density** multiplies extinction. Zero disables fog.
- **Fog Extinction** is extinction per metre before the density multiplier.
- **Fog Color / Fog Brightness / Fog Scattering** control the approximate light
  added by the fog. Zero scattering gives extinction without added light.
- **Noise Scale / Noise Strength / Seed** control world-space density structure.
  Zero noise strength produces homogeneous fog.
- **Fog Samples** controls density integration accuracy (the existing `hazeSteps`
  scripting field). Thin box fragments receive at least one sample each.
- **Max Distance** limits integration measured from the viewing camera's near
  plane. Empty space outside the boxes adds no fog.
- The camera's **Intensity** affects both fog density and rain density where enabled.

Fog Only ignores rain density, streak appearance, motion, cell size, cell-step
budget, Near Fade, Mid Distance and Far Distance. Its Max Distance is independent
of the rain transition fields. The inspector hides controls that do not apply.

Rain And Fog keeps the original rain-linked haze by default: rain density, color,
brightness and Scattering still determine the haze appearance. Enable **Independent
Fog Settings** to use the separate fog controls instead. Its distance transition
still follows the rain's Mid/Far distances. This preserves existing authored rain
profiles without automatic migration or changes to their serialized values.

## Lighting and performance

Fog uses Beer–Lambert extinction and simple constant-color scattering, with optional
world-space density noise. It does not sample scene lights, shadows or other fog
systems, and does not produce shadowed light shafts. Ordinary transparent surfaces
have the same depth limitation as the rain renderer.

Fog Only has a dedicated shader variant: no capsule generation, DDA cell traversal,
rain-space transforms or streak pixel-footprint calculations. Cost depends on pixel
coverage, density sampling and volume intersections, rather than Cell Size or Max
Cell Steps. **Streaks** debug view is black; **Traversal Cost** reports zero visits;
**Haze** shows fog opacity. Both a zero Fog Density and a zero Fog Extinction skip
the fog-only pass entirely.

## Demo

Import **Fog Laboratory** from the package's Samples tab, or run **Tools > jlinkdev >
Volumetric Rain > Create Fog Laboratory**. Open it alone and press Play. The demo
pipeline activates temporarily, as in the rain laboratories. The camera starts in
a clear canopy looking into a fog volume. Move the camera through the opening,
resize the fog box, and toggle the orange exclusion to inspect the boundaries.
