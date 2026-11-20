# V1 validation

The Windows development player used Unity 6000.0.58f1, URP 17.0.4, Direct3D 11 and an NVIDIA GeForce RTX 2060 SUPER. A clean validation project installed the local UPM package without Interactive Fog. Package Manager imported Starlit Passage. The player was built from the imported scene and loaded its prefab, sample material and scripts. A separate project used Unity 6000.3.21f1 and URP 17.3.0.

## Three-style regression check

The Unity 6000.0.58f1 EditMode suite passed all 29 cases, including GPU shadow integrals, physical clearings, preset loading, legacy warp migration, deterministic seed overrides, and shadow-cache isolation between volumes. On Direct3D 11 the three imported scenes produced 33 comparison frames: for each style, the unwarped control, six warps, zero strength, a combined warp, an independent warp seed, and a moved star. All 33 matched the pre-refactor PNGs byte-for-byte. Releasing the cached textures and disabling and re-enabling the volume each reproduced the relit frame for every style. A camera excluding the volume’s layer allocated no volume material. For each style, moving the primary star after rendering another volume produced the same image as rebuilding the relit volume from scratch.

## Earlier lighting and player checks

- The original ten EditMode tests passed on both editor versions. Eight GPU cases cover uniform dust and gas, including all three field shapes. Two authoring tests cover the shipped profiles and lighting arrangements, local seed overrides and deterministic star offsets.
- At the same flight camera position before and after assigning Painterly Cavity, 575,417 of 576,000 pixels matched exactly. The largest difference in any 8-bit channel was 2. Resetting plume overrides or setting variation strength to zero reproduced the original plume capture byte-for-byte with star offsets disabled.
- The [plume comparison](stellar-plumes.jpg) shows a fixed field from three angles. In the second front view, only the star moved.
- The player rendered the opening view, a second flight position, dust alone, each star moved and disabled separately, an alternate seed, full and half resolution, and a disabled and re-enabled volume. Re-enabling the volume reproduced the original frame pixel-for-pixel.
- In the filamentary preset validation, moving the star behind a sampled foreground lane reduced its brightest center-pixel channel from 1.00 to 0.17 with gas emission disabled. An opaque black object in front of the disk reduced the center pixel to zero at both render resolutions.
- The package README image is a Play Mode capture after switching the sample background to solid black.

## Render timing

The camera moved laterally in the imported sample. Each phase rendered offscreen at 960 × 540 with full-resolution integration and 160 steps. Direct3D 11 timestamp queries bracketed the volume update and camera render on the GPU. Each median uses 90 samples after 120 warm-up frames.

| Condition | Median GPU interval |
| --- | ---: |
| Camera moving, both stars fixed | 7.29 ms |
| Camera and primary star moving, companion fixed | 8.97 ms |

The interval includes the sample camera render. Initial texture allocation and density generation occur before the measured phases. The figures apply to this scene, driver, resolution and hardware.
