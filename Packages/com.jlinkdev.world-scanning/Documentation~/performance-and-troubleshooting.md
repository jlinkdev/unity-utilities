# Performance and troubleshooting

## Performance model

Runtime pulse simulation uses fixed storage and performs no managed allocation per frame. Rendering evaluates every active pulse for each visible pixel touched by the fullscreen pass, so GPU cost scales primarily with resolution, eligible cameras, and active pulse count rather than scene object count.

For cost-sensitive targets:

- Keep normal gameplay concurrency well below the hard limit of 16.
- Disable Scene, Preview, and Reflection camera rendering when unnecessary.
- Prefer dynamic resolution or a lower render scale before weakening the visual model.
- Profile representative overlapping pulses on target hardware; desktop results do not predict mobile or XR cost.
- Avoid adding the renderer feature to multiple renderers used by the same camera stack without intent.

## No visible pulse

1. Confirm the project uses URP 17 with Render Graph enabled.
2. Run **Tools > jlinkdev > World Scanning > Validate Project Setup**.
3. Confirm the camera uses the renderer containing `ScanRendererFeature`.
4. Confirm the profile is assigned and `ScanSystem.ActiveCount` becomes nonzero.
5. Ensure the pulse range reaches visible geometry and the profile has nonzero emission.

## Grid or edges look unstable

- Keep the grid cell size large enough for the target resolution and camera distance.
- Raise distance-fade start/end values for large worlds only when needed.
- Normals must be available; the feature requests them, but custom renderers and shaders still need compatible URP passes.
- Very thin geometry and extreme depth discontinuities can need a lower depth threshold or edge intensity.

## Pulse is too bright

Lower profile emission, trail, grid, and edge intensities first. Then tune renderer opacity. When using Bloom, prefer an HDR-aware tonemapper such as ACES and avoid compensating with excessive negative exposure.

## Compatibility notes

The implementation targets Unity 6 URP's Render Graph path. Compatibility mode and built-in/HDRP rendering are intentionally outside this package's focused scope. Validate custom renderer assets, camera stacks, XR, dynamic resolution, and platform graphics APIs in the consuming project before shipping.
