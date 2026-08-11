# Troubleshooting

## The field renders but does not refract

Enable **Opaque Texture** on the active URP asset and confirm that the camera has not overridden the requirement. Refraction only contains opaque geometry rendered before transparents.

## Intersection glow is absent

Enable **Depth Texture** on the active URP asset. Increase the preset's intersection width when working at a large world scale.

## Ripples move away from the object

Keep the forcefield controller as the stable root of the rendered shell. Reparenting the renderer independently after an impact changes the relationship between the controller's stored local position and the visual mesh.

## Ripples look unnatural on a custom mesh

Select **Surface Distance** propagation. Spherical mode assumes a centered, sphere-like shell. Generic propagation is intended for convex closed meshes and does not solve mesh geodesics on concave surfaces.

## The effect is too bright in post-processing

Preset colors support HDR and are intended to drive bloom. Reduce surface, Fresnel, impact, and intersection intensities before lowering the whole component intensity if you want to preserve opacity.

## Transparent fields sort incorrectly

Adjust the material render queue or renderer sorting priority for the scene. Screen-space refraction cannot include transparent objects rendered later in the frame.
