# Changelog

## Unreleased

- Mist Sanctuary uses an explicit camera renderer override instead of switching the project pipeline at runtime. Setup can register and assign a dedicated camera renderer with Undo while preserving the pipeline default.

- Added a targeted renderer setup window and creation commands for all three styles. Renderer configuration is idempotent and records Undo; independent profile copies preserve tuning and avoid overwriting assets.
- Grouped volume/profile Inspectors, clarified billow Min/Max units, reported loaded profile usage and effective spacing, and made mixed selections avoid misleading inline/shape-specific edits.
- Removed the sample's implicit Dry Ice preview override, repaired its Low Camera toggle, and fixed temporary capture-style replacement and reference-profile initialization. Updated setup/troubleshooting guides and documentation encoding.

- Standardized three styles: Ground Mist, Cumulus and procedural Dry Ice. Renamed the Cumulus implementation without changing its serialized shape value; preserved the Dry Ice profile GUID and tuning. Removed redundant presets and hid irrelevant Inspector controls.
- Removed tracked billow emitters, their mode/settings, population diagnostics and individual `EmitPuff` API. Dry Ice uses seeded cells with spacing, coverage, radius and timing ranges. Body density and cached release support both fluid backends, interaction and recovery.
- Added live procedural-radius regression coverage and retained deterministic replay, mist transfer, style-swap and resource-lifecycle tests. Fixed clearing recycled billow textures.
- Dry Ice uses a connected ridged mist bed, single-crown billows and overlapping rise/settling motion.

- Added a Density slider (0-32, retaining the `extinction` field) and Cloud Coverage for overlapping secondary billows. Refined rounded cloud cores and attached lobes, and added a Cumulus profile for dense connected banks. Live coverage edits preserve wakes and simulation time.
- Fixed periodic Cumulus opacity pops by normalizing each advected detail map before crossfading. Aligned lighting with the completed simulation tick.

- Added the Cumulus style profile with varied cloud clusters, advected density detail, distance-filtered curled ribbons and detail-aware lighting. Shape/size changes rebuild the recovery target without clearing active wakes. Its optional detail cache is released when returning to Ground Mist or disabling the volume.
- Normal sample playback respects project frame-rate settings; only explicit benchmark captures disable frame-rate and VSync limits.

- Added Full/Half/Quarter fog rendering with depth-aware reconstruction and full-resolution fallback at unmatched depth edges.
- Added an optional Planar2D column-fluid backend with conservative mass transport, wakes, recovery, a cached volumetric shape and height-dependent lighting. Volume3D remains the default.
- Added frustum culling for fog rendering without suspending simulation.
- Added planar-backend conservation, recovery, interaction, obstacle and resource-lifecycle tests, plus render-resolution profiling controls.

- Added swappable Fog Style Profile ScriptableObjects, the Sanctuary Ground Mist preset, and an inspector command to save the current look. Existing inline settings remain intact when no profile is assigned.
- Runtime height/style changes rebuild the recovery target without resetting active density, velocity or time. Quality and light references remain per-volume.
- Skipped point-light shadow marches outside light range, cached unchanged obstacle masks and cached compute kernel indices without lowering quality settings.
- Added profile equivalence/live-swap and obstacle-cache invalidation regressions, diagnostic benchmark modes, and performance/style documentation.

- Added a full, softly tapered billowy density profile.
- Added sparse local coordinate deformations with jittered centres, varied radii/strength and both turn directions; preserved three-dimensional billow noise and wake-gated recovery.
- Cached local curl displacement in a 512 x 512 RGHalf texture (1 MiB); current total is 40 MiB default / 82.25 MiB sample.
- Extended lifecycle validation to check release of all owned fog textures, including the new cache.

- Added adjustable, wake-gated Rest Recovery to rebuild the initialized density folds after disturbances; zero retains unassisted transport.
- Added Fold Size for tuning the width of rendered curved bands independently of simulated eddies; default 1.5.
- Added a sample Recovery Preview that stops after one pass and a matched 40-second capture mode.
- Cached the resting density in one extra RFloat texture (4 bytes per cell, included in the totals above).
- Added long-idle recovery and fresh-wake preservation GPU regressions.

- Fixed zero-drag interactors still injecting paired vortex forces and wake turbulence. Supplemental wakes fade with drag between zero and one; existing tuning at drag one and above is unchanged.
- Clarified that child interactors and density clearing are independent controls.

## 0.4.0

- Replaced sparse initialized banks with a connected lower layer and curved density ridges.
- Added broad horizontal circulation and wider paired wakes with less radial clearing and upward lift.
- Enlarged advected eddies, reduced small-scale stirring, and damped vertical velocity independently.
- Reworked render detail into curved folds; erosion now affects only the thin fringe.
- Added a GPU coverage regression across a room-sized field; retained the animation-only sample path.

## 0.3.0

- Added flow-advected, motion-driven curl stirring with a decaying wake marker.
- Added displaced and eroded wisp contours; interpolated detail coordinates between simulation ticks.
- Replaced minmod reconstruction with a less diffusive monotonized-central limiter while preserving conservative face transfers.
- Made staggered detail-map resets use exact integer simulation ticks.
- Replaced sample input/CharacterController movement with a closed animation spline and fixed observation camera.
- Added a finer sample grid, full-rate matched control capture, and regressions for reset timing and wake decay.

## 0.2.0

- Conservative density transport prevents long-term mass growth; stationary and moving default interactors no longer delete fog.
- Corrected velocity advection, paired rolling wakes, stronger settling, and broader, lower initial banks.
- Flow-advected fine detail with staggered map resets and interpolated simulation density.
- Added swept-force API and an animated foot/blade sample rig.
- Neutral temple-style lighting, closer gameplay framing, and a 20-second full-rate capture path.
- Expanded GPU regression coverage for long-term stability, mass conservation, stationary interaction, and transported detail.

## 0.1.0

- Initial shallow 3D fluid, character wakes, finite gusts, and oriented solid boxes.
- URP Render Graph volumetric composite, directional self-shadowing, and point-light scattering.
- Mist Sanctuary interactive sample and GPU regression tests.
