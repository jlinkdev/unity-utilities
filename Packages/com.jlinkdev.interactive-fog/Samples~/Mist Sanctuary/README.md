# Mist Sanctuary

The floor tiles, column shafts, capitals, wall ribs, side walls and back wall use the same framework Architectural Grid material. Its half-metre tiles are projected in world space, so scaling a wall keeps the same tile size as a column.

Open `Scenes/Mist Sanctuary.unity`. Press Play to view the camera’s fog effect. The character automatically follows a closed, 20-second spline path, using the authored Humanoid idle, straight and turning strides, and one-handed sword animation. There is no input controller, CharacterController, keyboard/mouse handling, or input-backend requirement.

The `MistSanctuary` component exposes the world-space `pathPoints` array. Use at least four points to reshape the closed Catmull–Rom path. The fixed observation camera makes residual wakes easy to follow; the final two seconds switch to a low view. This is an animation demonstration, not a navigation or collision system; author the path clear of solid geometry.

The sequence pauses for two seconds, walks until five seconds, moves faster until eight seconds, turns until ten seconds, pauses and sweeps the blade at twelve seconds, then returns along the path from 14.75 to 18 seconds, after the arm and feet settle. It loops without teleporting the character. Foot motion settles smoothly when the character stops.

The sample uses a **160 × 32 × 160** grid and **112** ray steps to resolve smaller curls. The package defaults remain **128 × 24 × 128** and **96** ray steps. `turbulence` controls extra motion in wakes, `eddySize` controls its scale, `wispDisplacement` shapes the rendered contours, and `detail` controls fold contrast and thin-fringe erosion. Setting wisp displacement to zero disables contour displacement without changing the simulated density.

The **Fog Camera Pass** on Sanctuary camera references the feature in Mist Renderer. The camera requests depth directly. The scene requires compute shaders, floating-point 3D UAV support, and Render Graph. Its serialized fog shader reference retains the shader in builds. The sample has no host development-script dependency.

To capture a built player, use `--fog-capture <absolute-output-directory>`. It writes five PNG views and a JSON record, then exits after 20 simulated seconds. Add `--fog-full-rate` for all 1,200 frames at 60 fps, or `--fog-sequence` for five sampled frames per second. `--fog-coarse` disables added turbulence and wisp displacement and returns fine-detail intensity to 0.22 for a matched control. Use `--fog-benchmark` without sequence flags for synchronized timing, and add `--fog-disabled` for the scene baseline. Do not use `-nographics`.

Interaction tuning: Explorer and its animated left and right foot bones each have an independent Fog Interactor. Set Drag to zero on all three (with Clearing zero) to disable walking forces. Existing wakes still dissipate, ambient fog still moves, and the sweep samples the sword tip attached to the animated right hand.

Enable **Recovery Preview** on the **Mist Sanctuary controls** object's `MistSanctuary` component to walk the first pass and remain stationary at ten seconds. This skips the subsequent blade sweep and return loop. Restart Play Mode for a repeatable comparison. **Rest Recovery** on **Interactive ground fog** defaults to 0.25/s; zero disables interior recovery. **Fold Size** defaults to 1.5 and controls the width of the rendered folds. Recovery rebuilds the density shape gradually after wake activity decays; it does not reset velocity or stamp the whole volume in one frame.

For recovery captures, add `--fog-recovery`: the player runs 40 seconds (2,400 full-rate frames), with extra checkpoints at 10, 24, and 38 seconds. Add `--fog-no-recovery` to the same command for the control with interior recovery disabled. The resting-density texture and 1 MiB local-curl displacement cache bring sample allocation to 82.25 MiB, versus 40 MiB for the default grid. The full billowy density profile maintains connected coverage. Sparse local deformations bend its three-dimensional noise into irregular curls and are carried by the existing flow coordinates.

The scene uses **Profiles/Sanctuary Mist.asset** for its charcoal-and-amber temple lighting. Its camera uses the Bloom and Tonemapping components in **Settings/Sanctuary grading.asset**.

Assign a **Fog Style Profile** to the fog volume's **Style** slot to swap appearance and motion while retaining local quality settings. `Sanctuary Ground Mist` supplies a billowy ground-mist preset. Use **Save Current Look as Style Profile** to capture your own tuning; clearing Style restores the inline fields.

Additional capture diagnostics: `--fog-profile` selects the controller's Reference Style asset when assigned; `--fog-no-render` keeps simulation but excludes fog from this camera; `--fog-no-simulation` renders the initial field; `--fog-no-lights` removes fog point lights; `--fog-default-grid` uses 128 x 24 x 128 and 96 ray steps. These are workload comparisons, not isolated GPU stage timers.

Use **Simulation Backend** on the fog volume to choose Volume3D or Planar2D, and **Render Resolution** to choose Full, Half or Quarter. Switching backend resets the field; changing render resolution preserves simulation state. The supplied scene defaults remain Volume3D and Full.

Capture switches: `--fog-planar`, `--fog-half`, `--fog-quarter`, `--fog-low-camera`, `--fog-ortho`, and `--fog-1080` select the backend, fog integration resolution, camera checks, and a 1920 x 1080 output target. Without `--fog-1080`, capture output is 1280 x 720. `--fog-profile-no-detail` is a development-build diagnostic that removes rendered detail for cost attribution; it is not a visual-quality preset. GPU pass timing fields report -1 if the platform does not supply samples; synchronized whole-frame timings remain separate.

Normal playback leaves the project frame-rate and VSync settings unchanged. Explicit `--fog-benchmark` captures disable both limits for timing comparisons.

Assign **Cumulus** for dense connected cloud banks. **Density** controls opacity, **Cloud Coverage** connects the main banks with varied smaller billows, **Puff Size** controls horizontal lobe scale, and **Fog Height** controls height. **Wisp Opacity** adjusts curled fringes independently. Press **Reset Simulation** for an immediate resting preview. Capture the Cumulus preset with `--fog-cumulus`; add `--fog-recovery` to inspect its return after disturbance.

`--fog-fine-render` selects Full rendering with 192 ray steps for fine-silhouette inspection. It does not change saved assets. Cumulus adds a detail cache and extra sampling; see `Documentation~/Performance-and-Styles.md` in the Interactive Fog package.

Assign **Dry Ice** to the volume for procedural billows with released low mist. Click **Select Style Asset** on the volume and edit **Billows** for spacing, coverage, radius and growth/settling/quiet-time ranges. No emitter is required. Reset Simulation restarts the field deterministically. The Dry Ice profile retains its tuned values when selected in a scene.

Normal Play Mode uses the profile assigned to **Fog Volume > Style**. There is no sample preview toggle that overrides it. **Low Camera** forces the low view; otherwise the final two seconds of each path loop use it automatically.

Explicit capture arguments `--fog-dry-ice` and `--fog-cumulus` create temporary default styles without modifying assets. Do not combine them. To capture a specific tuned profile, assign the controller's **Reference Style** and pass `--fog-profile`; it takes precedence and resets the field to that profile before capture. Reference Style has no effect during normal playback. Capture-only diagnostic edits use a temporary copy when needed.

## Sample presentation

On import, choose **Install** to use the shared fonts, icons, and sample header. To install later, choose **Tools > jlinkdev > Interactive Fog > Install Sample Framework**. Before the release tag is published, use Package Manager's **Install package from disk** and select `Packages/com.jlinkdev.sample-framework/package.json` from a Unity Utilities checkout.
