# Fog Laboratory

Open Fog Laboratory.unity alone and press Play. Its camera starts in a clear
canopy, looking into a fog-only box. Rain Demo Pipeline activates the included
URP pipeline during Play and restores your previous settings on exit.

Select Rain Profile: Render Mode is Fog Only, Extent is Volumes Only. Adjust Fog
Density, Fog Extinction, Fog Color, Fog Brightness and Fog Scattering. Rain density,
cell size, motion and Mid/Far distances do not affect this mode.

Select "Fog Volume - move or resize this box" to change its bounds. Disable
"Fog Exclusion - canopy interior" to fill the canopy too. Move Rain Camera forward
through the opening to enter the fog.

The mode is shared by all boxes for a camera. The demo enables ground-height falloff, drifting fog noise, soft inclusion/exclusion
edges, and optional main-directional-light glow. Shadows and light shafts are not integrated.
See Documentation~/fog.md in the package for details.
Set Fog Height Falloff, Fog Noise Velocity, and Directional Scattering to zero,
and Feather Distance to zero on both boxes, to compare the original fog.
See Documentation~/rendering.md for render timing and transparent output, and
Documentation~/runtime-settings.md for the profile blending API.
