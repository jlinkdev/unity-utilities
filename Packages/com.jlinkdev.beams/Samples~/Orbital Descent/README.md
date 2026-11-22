# Orbital Descent

Open **Scenes/Orbital Descent.unity** and press Play. Watch the contact point as the animated emitter sweeps the basin: the flare rides the surface, while the sparks and cooling trail stay behind in world space.

Drag **Prefabs/Orbital Column.prefab** into your own scene, aim its forward axis at a collider, and set its Raycast Beam Endpoint layer mask to include that collider (Default in the sample). The prefab doesn't need the landscape or the animation. Its three tube renderers share one path. Disable the plume, sparks, or outer layers individually when tuning for a smaller effect.

The demo camera uses HDR and a local URP bloom volume on layer 31. The molten footprint conforms a small grid to the collider under the beam. Disable Heated surface for a cheaper flare-only impact. No project settings or renderer features need to be replaced.

## Sample presentation

On import, choose **Install** to use the shared fonts, icons, and sample header. To install later, choose **Tools > jlinkdev > Beams > Install Sample Framework**. Before the release tag is published, use Package Manager's **Install package from disk** and select `Packages/com.jlinkdev.sample-framework/package.json` from a Unity Utilities checkout.
