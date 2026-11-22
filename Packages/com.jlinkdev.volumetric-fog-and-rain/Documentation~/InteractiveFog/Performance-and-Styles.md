# Performance and style profiles

## Create and assign a style

The supplied `Runtime/InteractiveFog/Styles/Sanctuary Ground Mist.asset` provides a billowy ground-mist preset. Assign it to **Fog Volume > Style**, or use **Save Current Look as Style Profile** on an existing volume to preserve that volume's exact tuning. Create a new asset through **Assets > Create > jlinkdev > Volumetric Fog and Rain > Interactive Fog > Style Profile**. Use **Duplicate Profile...** to create an unassigned copy, or **Save Current Look as Style Profile...** on a volume to create and assign a copy. Neither overwrites an existing asset. The profile Inspector reports whether any loaded volume uses it.

A profile supplies appearance, motion and recovery settings. It does not change simulation backend, render resolution, bounds, grid resolution, simulation rate, pressure iterations, ray steps or scene light references. The inline style fields remain serialized and take effect when Style is cleared. `ResolvedStyle` reports the effective values; changing an inline style field while a profile is assigned does not override the asset.

At runtime, assign `fog.style = anotherProfile`. Appearance and motion use the new values; changing Style Type, Puff Size, Cloud Coverage or Fog Height rebuilds the resting-density recovery target without clearing live density, velocity or simulation time. In Planar2D, these controls also update the reconstructed shape immediately while preserving transported column mass and velocity. With Rest Recovery zero, transported density will not converge to the new target. Call `ResetSimulation()` when an immediate restart is desired. Profile edits affect all volumes sharing the asset; the simulation never writes into the asset.

The volume Inspector groups **Bounds**, **Simulation and Rendering Quality**, **Lighting**, and **Advanced**. Compute shader assignment and Automatic Simulation live under Advanced. Cell Size and Allocated Fog Textures help judge detail and memory; the texture total excludes camera targets and driver overhead.

## Choose simulation and rendering quality

**Fog Volume > Simulation Backend** selects `Volume3D` (default) or `Planar2D`. Switching backend reallocates resources and restarts the simulation. Both use the same style profile and volumetric renderer.

- **Volume3D:** independent density and velocity throughout the volume, including vertical displacement and height-dependent obstacle response.

- **Planar2D:** horizontal column-mass transport, pressure projection, paired wakes, advected detail and wake-gated recovery. A static 3D shape cache reconstructs billows; a lighting volume at half X/Z resolution retains height-dependent shading. Vertical rolling and height-selective interaction are approximated. X/Z set fluid resolution; Y samples the shape and lighting caches, not additional simulated fluid layers. This is a separate solver, not Y resolution set to one.

**Fog Volume > Render Resolution** selects `Full` (default), `Half` or `Quarter`. These divide each screen dimension for fog integration only: half produces one quarter as many fog pixels; quarter produces one sixteenth. Opaque scene rendering remains full resolution. The setting does not restart the simulation.

Reduced modes store HDR radiance, transmittance and depth, then reconstruct at full resolution with depth-weighted taps. Pixels without a compatible-depth tap fall back to full-resolution integration to preserve thin silhouettes. This fallback limits the saving in scenes with many depth discontinuities. No temporal history is used. Reduced modes can soften fine detail and produce spatial or moving-edge differences; compare at the intended camera angle and display resolution.

```csharp
fog.simulationBackend = FogSimulationBackend.Planar2D;
fog.renderResolution = FogRenderResolution.Half;
fog.Initialize(); // Backend changes restart the field; render resolution alone does not.
```

For the closest behavior to Volume3D/Full, start with Volume3D/Half and compare at the intended camera angle. Planar2D/Half is a lower-memory option for ground-focused effects where approximate vertical behavior is acceptable. Keep Full available for close inspection and fine silhouettes.

## Cost controls

| Control | Main cost | Tradeoff |
| --- | --- | --- |
| Resolution X/Y/Z | Volume3D fluid work scales with X * Y * Z; Planar2D fluid work scales with X * Z, with separate 3D shape/lighting storage | Coarser cells lose small wakes and obstacle detail. Halving all axes gives one eighth as many cells, not half. |
| Simulation Rate | Number of simulation and lighting updates | Lower rates increase interpolation latency and reduce response fidelity. Rendering still runs each frame. |
| Pressure Iterations | Repeated pressure-grid dispatches per simulation step | Fewer iterations reduce the accuracy of the approximate pressure projection; validate moving wakes and obstacles. |
| Render Resolution | Half: quarter of integration pixels; Quarter: one sixteenth, plus reconstruction and edge fallback | Can soften detail and alter moving edges. |
| Ray Steps | Volume samples per visible pixel | Fewer steps can introduce banding, jitter and thin-feature loss, especially at grazing angles. |
| Screen resolution | Full-resolution raymarch/composite pixel count | 1920 x 1080 has 2.25 times as many pixels as 1280 x 720; total frame time does not scale by exactly that ratio. |
| Point lights and their range | Per-voxel light/shadow evaluation | More overlapping lights cost more. Keep only relevant lights assigned. |
| Physical volume size | Cell size at a fixed resolution | Reducing bounds alone does not reduce allocated voxel count; it improves spatial resolution. Lower the grid along with bounds to reduce simulation work. |
| Volume/camera count | Separate simulations and composites | Avoid overlapping volumes; each rendered camera repeats rendering work. |

Fog Height, extinction and coverage also change how much raymarch work survives empty-space checks and early opacity termination. They are artistic controls, not reliable quality tiers. **Detail = 0**, **Turbulence = 0** and **Wisp Displacement = 0** currently change the result but do not eliminate all associated shader calculations. Rest Recovery is behavior, not a meaningful primary cost knob.

Defaults are 128 x 24 x 128, 30 Hz, 24 pressure iterations and 96 ray steps. The Mist Sanctuary sample uses 160 x 32 x 160 and 112 ray steps. For Ground Mist, Volume3D texture allocations are 40 MiB and 82.25 MiB respectively. Planar2D allocations are 4.625 MiB and 7.836 MiB respectively. These exclude camera targets and driver overhead. Reduced rendering adds 12 bytes per integration pixel for radiance/transmittance and depth: about 2.64 MiB at half resolution or 0.66 MiB at quarter resolution for a 1280 x 720 camera, plus the full-resolution composite target.

### Low camera angles

Check quality and frame time at floor level as well as the normal gameplay view. Grazing views can cover more of the screen with fog and keep more ray samples inside the volume; depth-edge reconstruction fallback can also reduce the benefit of Half/Quarter rendering. Compare milliseconds in the same scene and camera path with VSync and frame caps disabled for measurement. Further optimization of this workload remains an open investigation; current measurements do not isolate its dominant GPU cost.

## Exact-work optimizations

- Point-light shadow marching is skipped outside that light's finite range, where its contribution is exactly zero.

- The solid obstacle mask is reused while the obstacle matrices, count and volume origin remain unchanged. Movement, resizing, activation/deactivation and volume translation invalidate it.

- Compute kernel indices are cached per volume allocation.

- Volumes outside the camera frustum skip rendering; their simulation continues so wake and recovery state remain continuous.

These changes retain grid dimensions, step counts, noise, density, lighting equations and recovery tuning. Matched rendering is checked separately from timing.

## Style capabilities

Profiles select a density shape and supply appearance, motion and recovery controls. **Sanctuary Ground Mist** provides dense rolling mist. **Dawn Ground Haze** uses the Ground Patchiness control to separate cool banks with clear channels. At zero, Ground Patchiness preserves a continuous layer. The simulation moves the banks and restores them according to Rest Recovery. **Cumulus** and **Dry Ice** supply the other two shapes. All four presets work with the volumetric renderer and either simulation backend. Dry Ice adds procedural billows and released mist, as described below.

Assign `Runtime/InteractiveFog/Styles/Cumulus.asset` for dense connected cloud banks (Density 12, Cloud Coverage 0.75). It retains quiet-wake recovery. Press **Reset Simulation** for an immediate resting preview; otherwise quiet density transitions through Rest Recovery. Assign Sanctuary Ground Mist to return to low mist. Profiles do not change local quality settings.

Cloud-style controls:

- **Style Type** selects Ground Mist, Cumulus or Dry Ice. Choose Cumulus to expose the cloud-specific controls below.
- **Puff Size** controls the horizontal scale of cloud clusters in metres (preset 1.25). It affects Cumulus only.
- **Fog Height** controls cloud and plume height (preset 1.6 m). Keep volume Size Y above the style height with room for displacement; 2 m accommodates the preset.
- **Plume Lift** adds gentle vertical stirring to Cumulus in Volume3D. It has no vertical transport effect in Planar2D. This is an artistic force, not a temperature or buoyancy simulation.
- **Detail** scales the opacity of low-density fringes; it does not change cache resolution or disable detail work at zero. **Wisp Displacement** bends the contours. **Fold Size** controls the scale of that deformation independently of Puff Size.
- **Wisp Opacity** scales Cumulus's separate curled wisps independently of the cloud bodies. Zero hides these wisps, but does not free the detail cache or remove all of its shader work.
- **Density** (0-32) controls optical thickness. Higher values make existing puffs more opaque; they do not fill empty gaps. The serialized field and scripting API remain `extinction`, so existing profiles retain their values. **Albedo** and **Ambient** control color; scene light references remain on the volume.
- **Cloud Coverage** (0-1) adds overlapping smaller billows between the main clusters, with varied sizes, heights and irregular gaps. Zero omits these additional billows. It affects Cumulus only. Increasing it rebuilds the recovery target and detail cache without clearing the active wake; use **Reset Simulation** for an immediate resting preview. Density around 10-12 and Coverage around 0.6-0.8 provide a starting point for dense cloud banks.

Cumulus uses a separate renderer variant and an additional RGBAHalf 3D cache containing fine cloud density, the filtered resting transport field, unsigned distance to curled filaments and their taper opacity. Gaussian-filtered coverage softens the thin wisps. Unsigned distances prevent false zero crossings where the nearest filament changes; very thin curls can fade below the cache resolution. The two advected coordinate maps carry this detail through the flow. Ribbon coverage and each map's fine-density/transport-proxy ratio are evaluated before blending the maps. This avoids both invented intermediate surfaces and abrupt opacity changes as a reset map fades into an empty region. Lighting uses the same simulation tick as the rendered coordinates. Lighting evaluates the detailed density as well as transported mass. A second, less attenuated scattering term softens cloud self-shadowing without an additional shadow march; a density-dependent scattering response gives dense cores more definition than thin edges. These are artistic approximations, not a multiple-scattering solver. Solid obstacle samples still block the light. The normalization field is rebuilt after the resting target so live shape edits use matching data.

The optional cache has twice the simulation resolution per axis, clamped to 64â€“256 cells in X/Z and 32â€“96 in Y, plus mip levels. At the sample's 160 x 32 x 160 simulation resolution it adds approximately 36.57 MiB to either backend. Ground Mist does not allocate this cache. Shape, Puff Size, Cloud Coverage and Fog Height changes rebuild it; frequent animation of these controls can cause expensive regeneration. Color and opacity edits do not regenerate it.

Cumulus adds texture sampling to both lighting and rendering. Taller coverage and thin filaments can cost more to render and can expose reduced-resolution softness or insufficient ray steps. The published Ground Mist timings do not establish a Cumulus frame budget. Compare Full/Half/Quarter and ray steps using the intended camera and style. Ground Mist keeps its original density and shading paths. Ground Mist and the static Cumulus shapes do not emit billows. Dry Ice adds procedural billow release; none of the styles model heat or combustion.

## Measuring performance

The [Mist Sanctuary sample guide](<../../Samples~/Mist Sanctuary/README.md>) documents player capture and benchmark arguments. Measure with the camera, screen resolution, lights and simulation workload held constant. Benchmark without video recording or other GPU workloads. The sample's synchronized whole-frame timing includes CPU and synchronization overhead; use a GPU profiler for individual shader costs.

See [tested configurations and validation](Validation.md) for measured results and their limits. Lower grid/ray budgets, reduced simulation frequency and reduced screen resolution can improve performance but require visual evaluation. Skipping offscreen simulation also changes wake and recovery continuity.

## Dry Ice billows

Assign **Dry Ice** to Fog Volume > Style, then use **Select Style Asset** to edit the assigned profile. Dry Ice always uses a procedural field; it requires no emitter component or mode selection. The Inspector hides controls belonging to other shapes. Profile edits are shared and persist after Play Mode; duplicate the asset for independent tuning.

| Control | Effect |
| --- | --- |
| Spacing | World-space distance between jittered cells. Effective spacing is at least 1.7 times maximum radius; the Inspector shows the effective value. |
| Coverage | Probability that a cell cycle contains a billow. One enables every cycle, not constant full floor coverage. |
| Radius Range | Reference radii in metres, clamped to 0.1â€“2. Sizes favor the smaller end; footprints widen during settling. |
| Growth / Settling / Quiet Range | Minimum and maximum durations in seconds. Rise and settling overlap; quiet time separates cycles. Independent phases avoid synchronized pulsing. |
| Field Seed | Repeatable variation in positions, sizes and timing. |
| Billows / Density | Visible bubble-body density. Overall Density scales extinction for both bodies and low mist. |
| Mist Amount | Integrated density released to the fluid, scaled by reference radius cubed. Zero disables density transfer. |
| Spread Speed | Outward stirring during settling. Zero removes this force but does not stop the authored footprint widening. |
| Fog Height / Rest Recovery | Depth of low mist and its return toward the resting pattern after disturbances. |

There is no stored bubble list or global bubble-count cap. A bounded 5-by-5 neighboring-cell search reconstructs features from seed, position and simulation time. Count is statistical; smaller spacing, higher coverage, longer lifetimes and shorter quiet intervals increase population. Increasing maximum radius can also increase effective spacing. The field drifts with horizontal wind and is reduced locally by wake activity. Released mist remains in the selected solver; Planar2D integrates release through the column but cannot simulate independent vertical circulation.

Layout, seed, radius and timing edits affect the live field and can rearrange it immediately. Use **Reset Simulation** for a clean comparison. Reset starts with staggered phases and clears released mist. Switching away from Dry Ice frees its billow resources while preserving the fluid state. Backend changes restart the whole simulation. `UsesProceduralPuffs` reports whether Dry Ice is active; `PuffDensity` exposes the current body volume. There is no individual-bubble emission API.

Allow volume height above the mist for the crowns. Bounds and obstacles clip bodies and release. Bubbles smaller than the simulation cells or buried under the low mist may be hard to see, particularly with reduced-resolution rendering. These are authored billow shapes with simulated released mist, not colliding bubbles, bubble films, heat or physical phase changes.

Dry Ice allocates two RFloat body volumes and an RGBAHalf source volume: **16 bytes per simulation voxel**, or **12.5 MiB** at 160 Ã— 32 Ã— 160. Sample totals are **94.75 MiB Volume3D** and **20.34 MiB Planar2D**, excluding camera targets and driver overhead. It does not allocate the Cumulus detail cache. One compute pass rebuilds the field per simulation tick; fluid passes read the cached sources. Full/Half/Quarter changes rendering cost, not this simulation work. Use the intended profile and camera for timing; Ground Mist timings are not Dry Ice budgets.
