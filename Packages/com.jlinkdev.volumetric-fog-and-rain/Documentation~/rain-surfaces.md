# Wet windows and rain impacts

## Wet glass

1. Create a material using **jlinkdev/Volumetric Fog and Rain/Wet Glass**. Assign it to the broad face of a window pane.
2. Add **Wet Glass Refraction Feature** to the Universal Renderer Data used by the viewing camera. Keep its regular transparent layers enabled. If the camera also renders atmospheric fog, put the glass feature below the fog features and use **Before Transparent Capture** on Volumetric Rain to include its rain and haze in the refracted image.
3. Add **Wet Rain Surface** to the pane. With **Glass** enabled and **Surfaces** empty, the component drives its own renderer. Set **Rain Amount** above zero and enter Play Mode. **Wet Seconds** and **Dry Seconds** control how long the wetness takes to rise and fall. Lower Rain Amount to zero to stop bead replacement while the water already on the pane dries.

For a single base Game camera, create a **Wet Glass Capture** asset under **Create > jlinkdev > Volumetric Fog and Rain** and assign it to a **Wet Glass Camera Pass** on that camera. The observation shelter in Rain Laboratory uses this setup. Its camera captures the rainy courtyard before URP draws the pane. Other cameras can see through the same pane without adding a renderer feature to the project.

The material controls droplet density, scale, runoff speed, trail length, lens displacement, background softness, glints and optional grime. Give neighboring panes different **Pane seed** values. The pattern follows the pane when it moves. For custom or hinge-relative meshes, use the shader's UV and bounds options to keep runoff oriented down the glass. The shader preserves destination alpha and uses scene depth to prevent the pane from distorting objects in front of it.

The standard renderer feature captures the scene before ordinary transparent objects draw. If a project needs transparent scenery in the refracted image, it can derive from `WetGlassRefractionFeature`, enable `DrawTransparentQueues`, and set the renderer's ordinary transparent layer mask to **Nothing**. The feature then draws queues 2501–3009, captures color, and draws queues 3010–5000. Configure `ShouldCapture(Camera)` to skip the copy when the project's panes are dry. Use this split only on a renderer configured for it.

For an existing weather controller, `RainSurfaceState.Advance` accepts exposure, whether to advance the bead clock, elapsed seconds and wet/dry durations. `Apply` writes per-renderer properties. `Restore` releases the overrides and `ResetWetness` also clears the clocks. An ordinary lit material can use **Wet Rain Surface** with **Glass** off for wet darkening and smoothness.

To drive wetness from the package's camera profile, assign the **Volumetric Rain Camera** and **Wet Rain Surface** to a **Rain Surface Exposure** component. Its optional **Impacts** array takes parent splash emitters. Turning off the rain stops new impacts and bead replacement while the surface dries over its configured **Dry Seconds**.

## Wet stone and other opaque surfaces

The **Rain ripples on pooled water** material setting adds rings to exposed paving. Raise it on a wet, near-horizontal surface to see the reflection of nearby lights break across the water. With **Wet Rain Surface** driving the material, the ripples settle after rain stops while the stone stays wet. The default strength is zero.

1. Create a material with **jlinkdev/Volumetric Fog and Rain/Wet Surface**. Assign albedo, normal and roughness maps. Import albedo as sRGB, normal as a normal map, and roughness as linear. **Texture repeat** sets their physical scale in metres. A positive **Grid spacing** adds recessed joints. By default the pattern moves with the object. Enable **Align stone and joints across objects** for modular architecture that shares a world-space grid, such as the Rain Laboratory window surround.
2. In the material inspector, adjust **Wetness** to see the dry and soaked surfaces under the scene lights. **Water film smoothness** controls the reflective coat, and **Uneven film coverage** breaks it into patches. **Surface grain contrast** brings out fine variation in the substrate maps. **Beads** adds individually lit drops, with a denser population on upward faces. **Downhill rivulets** follows gravity across tilted and upright faces. These effects use opaque URP lighting and reflection probes. They work with the normal URP renderer.
3. For changing weather in Play Mode, add **Wet Rain Surface** and clear **Glass**. Set **Initial Wetness** to match the authored preview if the surface starts soaked. Drive **Rain Amount** from the local weather or exposure value. Wetting and drying take the configured durations. Author separate exposure groups for sheltered and exposed objects. **Wet darkening** and **Water film smoothness** on the material control this shader. The component's **Wet Darkening** and **Wet Smoothness** fields apply to ordinary Lit materials.

For a courtyard, try a continuous film and low bead strength on the paving, stronger beads on a close coping stone, and rivulets on the face below it. Warm local lights and a reflection probe give the film highlights that change with the viewpoint. Keep drop spacing in physical units so a scaled wall and its cap show compatible drop sizes.

## Ground splashes

Place **Runtime/SurfaceImpacts/Rain Surface Impacts.prefab** at ground height over a floor with a 3D collider. In Play Mode its invisible drops fall from three metres above the origin across an eight-by-eight-metre area. On contact, the two collision sub-emitters make an expanding ring and a spray of droplets. Set the parent Particle System's **Collision > Collides With** to the intended surface layers and adjust its emission rate to match local rain.

If a project already has particles that strike surfaces, use **Rain Impact Ring** and **Rain Impact Spray** directly as **Collision** sub-emitters. The parent supplies impact position and emission probability. Child instances can override size and material for specific surfaces. The volumetric streak renderer is analytic and emits no particle collision events. Using the surface effects in an existing rain volume therefore requires a local particle emitter or another source of impacts.
