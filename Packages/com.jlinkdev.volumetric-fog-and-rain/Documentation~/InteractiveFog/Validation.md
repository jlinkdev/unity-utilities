# Tested configurations and validation

## Scope

These results apply to the current Unreleased source. They do not establish compatibility with every platform meeting the declared requirements or validate previously exported package archives.

| Setting | Tested configuration |

| --- | --- |

| Editor | Unity 6000.0.58f1 |

| Pipeline | URP 17.0.3; Core/Shader Graph 17.0.4; Render Graph |

| Platform | Windows, Direct3D 11, Linear color space |

| GPU | NVIDIA GeForce RTX 2060 SUPER, 8 GB VRAM |

| Player | Windows development build |

| Sample | 160 x 32 x 160 cells over 16 x 2 x 16 metres; 30 Hz simulation; 24 pressure iterations; 112 ray steps; six point lights |

The Unity 6000.3.21f1 host also imports and compiles the package, with the tuned profile and scene references checked. GPU regression tests and player captures use the editor version above. Vulkan, Direct3D 12, Metal and mobile remain unvalidated. XR, compatibility-mode rendering and integrated fog on transparent objects are unsupported.

## Automated and player checks

- **66 Edit Mode tests pass.** Coverage includes conservative transport, initial coverage, long-running height, interaction, zero drag, coordinate resets, wake decay, recovery, profile equivalence and swapping, obstacle-mask invalidation, optimized/reference work equivalence, and Planar2D column-mass conservation, recovery, zero drag, obstacle invalidation, backend switching and cached-shape equivalence. Cumulus-specific checks cover morphology swaps, recovery, agreement between the detail cache and filtered resting target, solid-wall light occlusion, coverage changes that preserve live wakes, and detail-map fades against empty regions on both backends.

- **6 Play Mode tests pass**, covering camera-independent simulation and complete resource release for both backends, including the optional Cumulus detail cache.

- The Windows development-player build succeeds. Four matched 40-second, 2,400-frame captures cover Volume3D at Full/Half/Quarter and Planar2D at Half without shader errors or runtime exceptions. Playback checks cover walking, stopping and recovery; additional perspective low-angle and orthographic captures complete successfully.

- Package structure, local documentation links, sample registration and assembly JSON checks pass.

The recovery regression measures normalized resting-pattern error of 1.06268 without recovery versus 0.15929 with recovery. A separate fresh-wake test retains approximately 98% of the control disturbance at a recovery rate of 2/s. These are test-specific measurements, not guarantees for arbitrary settings.

The optimization regression compares all slices of density, velocity and lighting textures with redundant work restored. Two same-executable player comparisons also match bit-for-bit at eight image checkpoints across walking and recovery; one uses the supplied style profile. Exact image equivalence has not been established between separately built executables.

## Performance measurements

Measurements use a 1280 x 720 offscreen development player, a fixed clock, uncapped rendering and a GPU fence each frame over a 40-second walking/recovery sequence. Warmup and frames following PNG writes are excluded. They are synchronized whole-frame wall times, including CPU and synchronization overhead. GPU pass recorders and frame-timing queries returned no valid samples on this configuration; the results are not isolated GPU pass timings.

The table below uses Sanctuary Ground Mist. Each 720p configuration was run twice in opposite order. All eight checkpoint PNGs matched exactly between repeats of each configuration. The same camera, style, animation, ray-step budget and horizontal simulation resolution are used across configurations.

| Backend | Fog render resolution | Mean frame ms, range of two runs | p95 frame ms, range | Fog textures MiB |

| --- | --- | ---: | ---: | ---: |

| Volume3D | Full | 10.01â€“10.95 | 12.84â€“13.85 | 82.25 |

| Volume3D | Half | 5.62â€“5.75 | 8.16â€“8.33 | 82.25 |

| Volume3D | Quarter | 4.89â€“4.89 | 7.36â€“7.56 | 82.25 |

| Planar2D | Full | 8.51â€“8.74 | 10.14â€“10.36 | 7.84 |

| Planar2D | Half | 3.92â€“4.01 | 5.02â€“5.21 | 7.84 |

| Planar2D | Quarter | 3.31â€“3.33 | 4.49â€“4.53 | 7.84 |

At 1920 x 1080 output, single Half-resolution runs measured 8.78 ms mean / 11.43 ms p95 for Volume3D and 6.93 ms mean / 8.33 ms p95 for Planar2D. Additional target-hardware profiling is required before setting a frame budget.

Texture totals include simulation, cached shape/detail and lighting. They exclude camera targets, reconstruction targets and driver overhead. Reduced rendering adds approximately 2.64 MiB at Half or 0.66 MiB at Quarter for 720p integration targets, plus the full-resolution composite target.

A separate diagnostic removing rendered detail reduced mean whole-frame time from 10.23â€“10.75 ms to 6.80â€“6.84 ms. This changes opacity and ray termination as well as shader work; it identifies expensive detail evaluation but is not a precise isolated cost or a recommended visual setting. Offscreen frustum rejection avoids the fog render passes while preserving simulation continuity.

## Quality and behavior checks

Across eight 720p checkpoints, the lower image region's mean absolute RGB difference from Volume3D/Full is 0.14â€“0.22 levels on a 0â€“255 scale for Half and 0.27â€“0.39 for Quarter. Playback retains the broad curls and gradual recovery in both backends. Planar2D produces a narrower horizontal wake with less vertical lifting. Half closely retains the Volume3D appearance in the checked sequence; Quarter softens detail more. These observations and averages do not establish perceptual equivalence: localized differences near edges and fine detail are larger, and moving-camera evaluation remains important. Five orthographic Full/Half checkpoints have mean RGB errors of 0.07â€“0.19/255; low-angle captures show the reduced vertical response of Planar2D.

Planar2D shares the initialized 3D shape cache but evolves a different flow field. Its transport test conserves total column mass within 0.01% over 600 steps with recovery, replenishment and clearing disabled. The recovery test's pattern error falls from 158.56 without recovery to 32.84 with recovery after a disturbance. These values measure column mass, not rendered opacity. The backend cannot reproduce independent vertical vortices or layered obstacle response.

## Cloud reference workload

The earlier sparse-cloud preset (formerly Cauldron) was measured separately at 1280 x 720, 160 x 32 x 160 cells, 30 Hz simulation, 24 pressure iterations and 112 ray steps over the same 40-second walking/recovery workload. Each configuration below has one measured run. Video encoding was disabled. These are synchronized whole-frame wall times on the RTX 2060 SUPER configuration above, not isolated GPU pass timings or a target-platform frame-rate guarantee.

| Backend | Fog render resolution | Mean frame ms | p95 frame ms | Fog textures MiB |

| --- | --- | ---: | ---: | ---: |

| Volume3D | Full | 15.42 | 18.45 | 118.82 |

| Volume3D | Half | 7.71 | 10.62 | 118.82 |

| Volume3D | Quarter | 6.68 | 9.63 | 118.82 |

| Planar2D | Full | 12.14 | 13.35 | 44.41 |

| Planar2D | Half | 5.08 | 6.22 | 44.41 |

| Planar2D | Quarter | 3.77 | 4.96 | 44.41 |

These timing measurements use the earlier default Cauldron morphology with no added Cloud Coverage; they are a reference workload, not a new benchmark of dense cloud banks.

The extra detail cache adds approximately 36.57 MiB to either backend at this grid size. Texture totals exclude camera/reconstruction targets and driver overhead. Initialization and morphology-cache regeneration are outside the steady-state timing interval.

Across eight matched checkpoints, Volume3D/Half has a lower-image mean RGB difference of 0.25â€“0.40/255 from Full; Quarter has 0.41â€“0.63/255. Thin wisps and silhouettes can differ more than these averages suggest. Planar2D preserves the preset's broad clustered shape but produces a substantially different wake because it transports columns rather than independent height layers. Choose it for its lower cost, not for frame-by-frame equivalence to Volume3D.

Separate 60 fps captures at Full resolution and 192 ray steps cover walking, a blade gust, a low camera, and a 40-second recovery sequence. The profiles and recovery remain functional on both backends. Fine curls remain limited by the detail-cache resolution; grazing views can merge dense billows into an opaque bank. The Cumulus branch is a procedural cloud morphology, not a temperature-driven smoke simulation.

## Reproduction

Run the package's Edit Mode and Play Mode test assemblies through Unity Test Runner in a project with the package enabled for testing. GPU tests require a graphics device and supported compute/UAV formats.

Import and build the Mist Sanctuary sample. Its [sample guide](<../../Samples~/Mist Sanctuary/README.md>) documents capture and diagnostic arguments. Use `--fog-capture <absolute-directory> --fog-recovery --fog-benchmark` for the 40-second timing workload. Add `--fog-planar` for Planar2D, `--fog-half` or `--fog-quarter` for reduced rendering, and `--fog-1080` for 1920 x 1080 output. In development builds, `--fog-reference-work` restores redundant obstacle rebuilding, uncached kernel lookup and out-of-range point-light shadow work for a same-executable control. Use `--fog-full-rate` without benchmark timing for motion review. Do not use `-nographics`.

See [performance controls and style profiles](Performance-and-Styles.md) for cost scaling and quality tradeoffs.

## Dry Ice and style-preservation checks

On 2026-11-17, 56 Edit Mode and six Play Mode tests passed. Dry Ice checks cover generation without emitters, deterministic replay across frame subdivisions, live radius edits, density transfer in both backends, coverage removal while released fluid remains, style swaps that preserve live fluid, and release/recreation of all three billow textures. Tests for the removed tracked-emitter API are no longer part of the suite. The Windows player builds without C# errors or shader warnings.

Matched before/after Windows players use the same serialized Ground Mist, Cumulus and tuned Dry Ice profiles, 1280 x 720 Full rendering, 112 ray steps and the same 20-second animated path. Five checkpoints per style/backend cover initial fog, moving-character wakes, a gust and a low camera: 30 matched image pairs across Volume3D and Planar2D. Maximum frame-mean absolute RGB differences (0â€“255 scale) are:

| Style | Volume3D | Planar2D |
| --- | --- | --- |
| Ground Mist | 0.753 | 0.432 |
| Cumulus | 0.479 | 0.238 |
| Tuned Dry Ice | 0.678 | 0.352 |

The shader shape, flow and lighting equations are preserved; Cumulus identifiers retain the same serialized enum value. These independently built captures are visually close, not pixel-identical. Local channel differences reach 54/255 near changing detail, so the averages do not prove pixel-level equivalence. Every retained field of the tuned Dry Ice profile matches its saved pre-cleanup JSON exactly, and its asset GUID is preserved. The host scene has no missing scripts after removing the unused emitter.

Dry Ice adds two RFloat body volumes and an RGBAHalf source cache. Sample allocation is 94.75 MiB in Volume3D and 20.34 MiB in Planar2D, excluding camera targets and driver overhead. A prior 20-second, 1,200-frame synchronized whole-frame run of the default procedural workload measured a 14.09 ms median and 18.13 ms P95 on the configuration above. That is a single workload measurement, not a fresh benchmark of the tuned profile or an isolated GPU stage cost.

Dry Ice uses authored procedural bodies with simulated released mist. Tests establish lifecycle, transfer and resource behavior; they do not establish a physically conserved phase change, photographic realism or a target frame rate on other hardware.

Three additional 20-second, 1,200-frame Volume3D videos cover Ground Mist, Cumulus and tuned Dry Ice. Playback review includes character wakes, a gust and low camera angles; the retained styles remain visually consistent with the reference checkpoints. All encoders exit successfully and the captures report no runtime shader errors or exceptions.

## Setup tooling checks

Editor tests cover all three style-creation commands, independent profile copies with assignment Undo and unique paths, renderer configuration without duplicate features, serialized shader/feature-map references, and visible Dry Ice billows when a new generic profile changes style type. Six Play Mode lifecycle tests also pass. The polished Windows sample builds successfully. All shader files and the three supplied profile assets are byte-identical to their pre-polish versions; the live tuned Dry Ice settings also match their saved JSON exactly.

The host editor compiles and exposes the setup menu. Native Inspector layout inspection was not completed because the window capture did not expose Unity while another foreground application was active; the setup logic and serialized behavior are covered by the checks above.


## Camera renderer setup

The suite passes 66 Edit Mode tests and six Play Mode tests on Unity 6000.0.58f1 / URP 17.0.3 / Windows DX11. Camera-specific checks cover new and repeated renderer registration, preservation of the existing list/default and global pipeline references, grouped Undo, camera override/default fallback lookup without adding components, and overlay-camera rejection before asset changes. The sample builds and completes a 20-second capture after removing runtime pipeline switching. A host-editor render on Unity 6000.3.21f1 confirms the camera override draws fog under the existing project pipeline; the three profiles, simulation and shader files are unchanged.

Renderer selection is a per-camera list index. Projects using multiple quality pipelines must maintain compatible renderer indices or update the camera override when changing pipelines. The standalone validation build explicitly selects its own pipeline at build time; sample runtime code does not select a pipeline.
