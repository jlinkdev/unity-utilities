# Rendering design

## Field and animation

The field is evaluated in an orthonormal basis whose long axis follows the total
rain velocity. Cells have extents `(cellSize, 3 * cellSize, cellSize)` in this basis.
Two grids, offset by a noninteger vector, each contain one potential capsule per cell.
An integer avalanche hash supplies all capsule attributes and occupancy. There is
no frame-dependent randomness and no screen-space drop placement.

The bulk velocity translates sampling coordinates using absolute time. Integer and
fractional displacement are uploaded separately: only fractional displacement enters
the floating-point DDA position; whole cells enter the integer hash. The hash wraps
at 65,536 cells per axis, so whole-cell animation wraps without changing the field.
World-space float precision still limits very large worlds; floating-origin integration
is not included. Pause using the camera's frozen time to distinguish animation from
camera parallax.

Capsules, including maximum filter support, stay inside their owner cells. This
avoids a 27-neighbor search and prevents cell-boundary clipping. Independent grid
offsets and randomized lengths reduce visible spacing; containment is still a visual
tradeoff to evaluate, especially near the maximum allowed length/width.

## Analytic streak queries

The fullscreen pass reconstructs two points from inverse view-projection to obtain
a near-plane origin and direction. This works for perspective and orthographic
cameras. Scene depth gives a finite ray segment, including sky at the far plane.
Non-reversed-Z depth is converted to the API's NDC range before reconstruction.

Each grid is traversed by a 3D DDA. The ray interval inside each cell is tested
against its procedural line segment. Closest-point evaluation clamps to both ray
and capsule extents, including near-parallel directions. Radius supplies coverage;
an analytic pixel footprint widens subpixel streaks with approximate energy
compensation. Derivatives are evaluated outside divergent traversal loops. All tied
axes advance together and zero direction components use a finite sentinel.

This is an approximate filtered capsule response, not exact participating-medium
transport through a cylinder. Width filtering is capped to maintain cell containment.

## Budget and representation change

For a normalized ray `d` in field coordinates and cell extents `s`, the approximate
boundary crossing rate is `sum(abs(d) / s)` per metre. Six visits are reserved for
boundary effects. The effective streak range is the lesser of the requested far
distance and `(maxCellSteps - 6) / crossingRate`. Its mid transition is shortened in the same proportion. There is no fixed 65%
cap on the authored Mid Distance; with sufficient budget it is honored exactly.
Thus the fade reaches zero before the hard traversal limit can cut off a streak.
This means aggressive budgets can change the visual range with viewing direction.

A separate fixed-count quadrature samples broad world-space density along the
depth-clipped ray. It integrates only the complementary far representation, then
uses Beer–Lambert transmittance `exp(-extinction * integratedDensity)`. Capsules are
never rendered by dense raymarch sampling. The haze transition is artistic and does
not preserve an exact physical extinction equivalence with the streak field.

## URP integration

The Render Graph raster pass explicitly reads camera color and depth, writes a new
color texture, and publishes it as `cameraColor`. Parameters are recorded per camera
and applied when its render function executes. Shader/material ownership belongs to
the renderer feature and is released when the feature is recreated/disposed.

API reference: [Unity 6 Render Graph raster pass workflow](https://docs.unity.com/en-us/engine/6000.0/manual/render-pipelines/universal-render-pipeline/customizing-urp/render-graph/write-render-pass).

## Optimization and bounded domains

Density noise and brightness hashing are evaluated only after capsule coverage and
fade are known to contribute. Haze skips work when extinction is zero or the
transition contributes nothing. Neither optimization changes the default output.

Rain and exclusion boxes clip the integration domain analytically before DDA.
See [volume implementation](volumes.md) for interval union/subtraction, per-grid
budget sharing, haze sampling, and limits. See [performance results](performance.md)
for controlled before/after captures and GPU timings.