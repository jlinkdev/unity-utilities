# Evaluate the prototype

Use a standalone Development player with VSync disabled, a fixed output resolution,
and the same camera/scene for all comparisons. Warm up shaders before recording.
Use the GPU Profiler marker **Volumetric Rain**, not editor frame rate or the debug
cost colors, to measure the effect. Record hardware, API, resolution, URP version,
render scale, AA, and camera path alongside results.

## Visual checks

1. Freeze time. Strafe and orbit around near, mid, and far occluders. Streaks must
   exhibit depth parallax instead of following the screen. Compare the same view
   repeatedly to detect non-determinism.
2. Unfreeze. Observe slow and fast fall speeds, wind, and a paused game. Look for
   discontinuities at cell crossings and long-time animation wraps.
3. Inspect camera rays along the rain direction and along grid axes; inspect negative
   world coordinates. Look for missing streaks at capsule ends or DDA corner ties.
4. Use a wall immediately beyond the near clip plane, then move it farther away.
   Rain behind it should vanish; rain between camera and wall should remain.
5. Toggle perspective/orthographic, resize the view, and try wide/narrow FOV.
6. Inspect Streaks and Haze views while moving through the mid/far transition. Lower
   the cell-step budget deliberately and inspect how the effective range changes.
7. Check thin geometry, alpha-clipped foliage, transparent windows, sky, and a camera
   stack. Ordinary transparent objects are a known depth limitation. Overlay cameras
   must not apply the rain twice.
8. Test both an in-editor scene and a player build to catch hidden shader stripping.

## Performance matrix

| Sweep | Values | Hold fixed |
| --- | --- | --- |
| Density | 0, 0.1, 0.5, 0.8, 1 | Grid, resolution, range, noise, budgets |
| Cell visits | 32, 64, 96, 128 | Resolution and all appearance settings |
| Resolution | 1280x720, 1920x1080, 2560x1440 | Camera FOV and profile |
| Far range | 15, 30, 60 m | Sufficient traversal budget |
| Broad noise | 0, 0.35, 1 strength | Haze sample count |

Record median and 95th-percentile GPU time over at least 300 warmed frames, plus
total frame time and screenshots. Density zero skips the whole pass, so report that
as a separate no-effect baseline. Occupancy branches make nonzero density affect
cost even though it never increases traversal limits. Resolution/range are expected
to dominate. Haze samples also carry noise cost, independent of cell traversal.

Compare against a GPU particle/VFX rain implementation at matched perceived streak
coverage, length, brightness, depth range, and output resolution. Include the cost
of its update/simulation and overdraw. This package does not include a particle
reference renderer; no relative speedup is claimed until that comparison is measured.

The first acceptance gate is convincing depth and stable identity under camera motion.
If that fails, investigate containment patterns and temporal aliasing before tuning
performance. A successful compile or static screenshot does not establish perceptual
quality in motion.

## Volume checks

Open Volume Laboratory and compare the dry canopy with its exclusion disabled.
Move across the opening and view rain from outside the inclusion box. Rotate and
scale the boxes, overlap wet and dry boxes, and test thin distant regions in Haze
view. Check both depth clipping and the absence of foreground rain inside shelter.
Box count, screen coverage, and excluded distance should be separate performance
sweeps. Exploratory measurements (300 measured frames per case) are in
[performance.md](performance.md); the broader matrix above remains recommended
for production benchmarking.