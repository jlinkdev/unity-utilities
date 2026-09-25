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
follows the rain's Mid/Far distances unless **Fog Distance Mode** is **Independent**. This preserves existing authored rain
profiles without automatic migration or changes to their serialized values.

## Lighting and performance

Fog uses Beer–Lambert extinction and simple constant-color scattering, with optional
world-space density noise. Optional main-directional-light scattering adds an unshadowed glow. It does not
sample shadows or other fog systems, and does not produce shadowed light shafts. Ordinary transparent surfaces
have the same depth limitation as the rain renderer.

Fog Only has a dedicated shader variant: no capsule generation, DDA cell traversal,
rain-space transforms or streak pixel-footprint calculations. Cost depends on pixel
coverage, density sampling and volume intersections, rather than Cell Size or Max
Cell Steps. **Streaks** debug view is black; **Traversal Cost** reports zero visits;
**Haze** shows fog opacity. Both a zero Fog Density and a zero Fog Extinction skip
the fog-only pass entirely.

## Demo

Import **Fog Laboratory** from the package's Samples tab, or run **Tools > jlinkdev >
Volumetric Fog and Rain > Create Fog Laboratory**. Open it alone and press Play. The demo
pipeline activates temporarily, as in the rain laboratories. The camera starts in
a clear canopy looking into a fog volume. Move the camera through the opening,
resize the fog box, and toggle the orange exclusion to inspect the boundaries.

## Height, distance, movement, and light

- **Fog Distance Mode > Rain Linked** retains the original combined-mode haze fade,
  including changes caused by the streak budget. **Independent** uses Fog Start
  Distance, Fog Distance Fade, and Fog Max Distance instead. Start/fade default to
  zero, filling the visible volume. Appearance and distance independence are separate.
- **Fog Only** is always distance-independent and uses the original Max Distance
  field, plus Fog Start Distance and Fog Distance Fade. Distances are measured
  along the viewing ray from its near-plane origin, not from volume entry.
- **Fog Base Height / Fog Height Falloff** control ground mist. Above the base,
  density is multiplied by `exp(-falloff * (worldY-baseHeight))`. At/below the
  base it remains unchanged; zero falloff disables it. Rain streak density is unaffected.
- **Fog Noise Velocity** moves only fog's density noise in world metres/second.
  Zero is stationary. Noise scale/strength/seed remain shared authoring controls.
- **Directional Scattering** adds the URP main directional light's color and
  intensity. Zero disables it. Positive **Scattering Anisotropy** brightens toward
  the light; zero is isotropic and negative values favor backscatter. It is an
  unshadowed Henyey-Greenstein phase approximation, not shadowed volumetric lighting.

Height and lighting are disabled by default. Soft volume boundaries are described
in [volumes](volumes.md); runtime blending and movement clocks are described in
[runtime settings](runtime-settings.md). Fog Samples controls approximation quality
for thin feather regions or sharply varying height/noise density.
