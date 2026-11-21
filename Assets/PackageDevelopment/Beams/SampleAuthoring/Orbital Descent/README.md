# Orbital Descent

Open Orbital Descent and press Play. Watch the contact point as the animated emitter sweeps the basin: the flare rides the surface, while the sparks and cooling trail stay behind in world space.

Drag Orbital Column into your own scene, aim its forward axis at a collider, and set its Raycast Beam Endpoint layer mask to include that collider (Default in the sample). The prefab doesn't need the landscape or the animation. Its three tube renderers share one path. Disable the plume, sparks, or outer layers individually when tuning for a smaller effect.

The demo camera uses HDR and a local URP bloom volume on layer 31. The molten footprint conforms a small grid to the collider under the beam. Disable Heated surface for a cheaper flare-only impact. No project settings or renderer features need to be replaced.
