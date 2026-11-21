# Water Laboratory

Open `Scenes/Water Laboratory.unity` and press **Play**. The pool scene contains
submerged steps, five depth columns, marked seabed geometry, and three floating
rigidbody crates. It ships with three profiles and its own URP renderer/pipeline.
An included global Volume applies ACES tonemapping and mild exposure adjustment
to retain detail in the bright sunlit geometry.

## Controls

- **Above**, **Waterline**, **Below** position the camera for each transition state.
- The height slider moves continuously through the wave surface.
- **Look left / right** changes the view without an input-system dependency.
- **Automatic above / below tour** crosses the surface repeatedly.
- **Calm**, **Coastal**, and **Stylized** select the included water profiles.
- **Reset floating props** restores the initial crate transforms and velocity.
- **Normal view**, **Water mask**, and **Path length** inspect the rendered volume.

All controls are mouse-operated IMGUI. The camera-state readout uses five sample
points; the renderer computes the underwater portion per pixel independently.

## Pipeline setup and restoration

Use Unity 6 with URP 17.0.3 and Render Graph enabled. The sample controller
temporarily assigns `Settings/Water Pipeline.asset` to Graphics and the active
Quality setting while enabled in Play Mode. On disable or scene unload, it
restores the previous assets if they are still set to the sample pipeline.
It does not overwrite a pipeline change made by other code while it is active.

This is a global runtime pipeline override. Run the sample as a standalone scene;
do not combine multiple sample controllers in an additive scene. Before Play,
the Scene view uses your existing project pipeline. To preview the full effect
there, explicitly add the Water Renderer Feature to your normal renderer or
select the sample pipeline in your project settings yourself.

The sample does not change color space or the Render Graph compatibility switch.
The package setup menu is separate and changes only the renderer you select.

## Customize

Duplicate a profile asset, assign it to `Water Body`, and edit its waves, tint,
foam, waterline, and caustics. Keep the body's world rotation and scale at identity.
Inspect the floating crates' `WaterBuoyancy` components to tune local float points.
The controller has an explicit body reference so camera and buoyancy selection
remain clear.

The scene and assets are ready to use after Package Manager sample import. No
development builder or other files from the source repository's `Assets` folder
are required. Source-repository maintainers can rebuild this scene with
`WaterSystemDevelopmentBuilder.BuildAll`; it publishes the authoring assets into
the package sample and does not alter project Graphics or Quality settings.
