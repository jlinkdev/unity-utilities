# Water System manual

## Setup and ownership

Add `WaterRendererFeature` to the Universal Renderer Data used by the base camera.
Render Graph must be enabled in the URP settings. The explicit setup menu adds
the feature to the selected renderer asset and supports Undo; it never picks a
renderer or changes global Graphics settings on your behalf. Duplicate feature
creation through that menu selects the existing feature instead.

`WaterBody` creates an owned transient grid and surface material. Its profile is
a shareable asset; assigning the same profile to several bodies shares settings.
Changing a profile changes all bodies that use it. The surface's base elevation
is `transform.position.y`. Size is the X/Z extent; depth extends downward from
that elevation. World rotation and lossy scale must remain identity. Use an
untransformed parent or keep the body at the scene root.

The package's shaders live in Resources so player builds retain them. Do not
replace the generated material or mesh to customize appearance; edit the profile.
The body releases its transient resources and restores earlier mesh/material
references when disabled.

## Waves and mesh resolution

A profile accepts up to eight directional waves. Each wave has:

| Setting | Unit / meaning |
| --- | --- |
| Direction | Angle in degrees in the horizontal plane |
| Amplitude | Vertical displacement in metres |
| Wavelength | Horizontal wavelength in metres |
| Speed | Propagation speed in metres per second |
| Phase | Phase offset in radians |

The GPU displaces mesh vertices from the same packed parameters used by CPU
queries. CPU samples interpolate the actual grid triangle, so camera state and
buoyancy follow the tessellated geometry, including coarse grids. Resolution
defines the number of cells along each axis; `(resolution + 1)^2` vertices and
`2 * resolution^2` triangles are created. Start around 64–128 for a small body
and inspect the shortest waves at grazing angles. More cells improve the silhouette
at the cost of mesh memory and the surface/boundary rendering passes.

Detail normals change lighting and distortion only. They do not displace the
surface or affect buoyancy. Caustics are an animated artistic pattern, not a
simulation derived from wave focusing.

Opaque color/depth is copied before transparent rendering. The surface then
writes hardware depth so near waves occlude farther waves regardless of triangle
draw order. Later transparent geometry behind the interface is depth-occluded
and is absent from the opaque refraction source; it needs custom integration to
appear through the water. Transparent geometry on the camera's side of the
interface can still render normally.

## Surface and underwater controls

Shallow/deep colors, absorption, refraction, smoothness, foam width/strength,
and detail-normal strength control the surface. Depth-dependent effects use
the opaque scene depth texture. Place submerged opaque geometry below the water
to make depth tint, shoreline intersection foam, and caustics visible.

Underwater absorption uses RGB coefficients per metre. Larger red absorption
removes red light faster. Density blends distant submerged geometry toward
Fog Color. Underwater Saturation reduces color variation, and Distortion offsets
the scene sampling. Small values are usually sufficient.

Waterline Width is measured in **render-target pixels**, not world metres. Its
color and strength are artistic boundary controls. Profile presets use a three
pixel band for Calm/Coastal and a six pixel band for Stylized.

The waterline is evaluated per pixel from the displaced boundary and the water
volume at the camera near plane. A camera can see dry and submerged portions of
the same frame. Rendering does not switch the whole frame based on the camera
center or the coarse state component.

## Camera state and events

`WaterCamera` is optional for rendering. Add it when gameplay needs the camera's
water state, an explicit body selection, or debug output.

- `BodyOverride` selects a specific body. Automatic selection is useful with one
  body; use an override where selection is ambiguous.
- `IsUnderwater` tests the camera center against the bounded water volume.
- `State` is `Above`, `Intersecting`, or `Submerged` from center/near-plane samples.
- `Submersion01` is the fraction of five samples inside water. It is a coarse
  gameplay diagnostic, not a percentage of rendered underwater pixels.
- `EnteredWater` and `ExitedWater` are UnityEvents for center crossings.
- `StateChanged` reports changes to the coarse camera state.
- `DebugMode` exposes the underwater mask, surface boundary depth, water path
  length, and caustics visualization.

Use crossing events to connect audio, particles, or a controller from your game.
Those crossing effects are not automatically installed by the package.
`WaterContactProbe` provides object contact hooks independently of the camera.
Its `EnteredWater` and `ExitedWater` UnityEvents pass a `WaterContact` snapshot
containing the body, probe position/velocity, sampled surface, signed depth, and
submerged state. A small hysteresis setting avoids repeated shallow crossings.

## Querying a surface

Use `TrySample` when a point may lie outside the horizontal footprint:

```csharp
using UnityEngine;
using jlinkdev.UnityUtilities.WaterSystem;

public sealed class WaterSurfaceMarker : MonoBehaviour
{
    [SerializeField] private WaterBody water;
    [SerializeField] private float heightOffset = 0.1f;

    private void LateUpdate()
    {
        if (water == null || !water.TrySample(transform.position, water.SurfaceTime, out var sample))
            return;

        Vector3 position = transform.position;
        position.y = sample.Height + heightOffset;
        transform.position = position;
        transform.up = sample.Normal;
    }
}
```

`Contains(point, time)` also checks the animated top and finite bottom of the
volume. `Sample(point, time)` is available for explicit queries where the caller
already controls the footprint. `WaterBody.Find(point)` discovers an active body;
it is not a replacement for a deliberate selection when volumes overlap.

## Buoyancy

Add `WaterBuoyancy` beside a `Rigidbody`. Its local-space float points distribute
upward acceleration and torque across the rigidbody. Put points near the hull
corners; a single point cannot stabilize orientation. `Buoyancy` controls lift
relative to gravity, `FullSubmersionDepth` controls how quickly points reach full
lift, and linear/angular damping reduce submerged motion. Forces are applied in
physics updates and are independent of rigidbody mass.

The component does not change gravity, mass, drag settings, constraints, or
interpolation on your rigidbody. Tune the rigidbody and collision geometry for
your game. It skips disabled, unsupported, or out-of-bounds bodies. Kinematic
rigidbodies do not receive forces. This is a practical floating-prop model, not
fluid displacement, a boat controller, or a swimming implementation.

## Rendering scope and cost

`WaterParticles` provides an optional suspended-particle cloud. Assign a
`FollowTarget` (usually the viewing camera), `BodyOverride`, count, box radius,
particle size, drift, and color. The helper owns its particle system/material
and clips particles against the animated finite body. It is decorative and
does not simulate bubbles, fluid transport, or surface splashes.

The renderer selects one body for each camera. An early `SurfaceInputPass`
requests opaque color and depth before transparent water is drawn, so surface
refraction has valid inputs. A displaced boundary draw and a full-screen
underwater composite are added to the normal surface draw. The composite runs
before post-processing so the normal URP post stack can process the result.
A Scene View option is exposed on the renderer feature. Preview, reflection,
overlay, and XR cameras are skipped.

Finite bounds clip the water volume; the camera is not considered underwater
outside the rectangle or below its bottom. Multiple visible bodies share no
global wave settings, but the underwater composite handles only the selected
body. No claim of full stacked-camera or multi-volume compositing support is
made in this release.

The generated mesh covers the top surface. The finite sides and bottom bound
underwater classification and optical path length; they are not rendered walls.
Use terrain or opaque pool geometry to contain the visible body. Underwater
attenuation follows the first water segment along each viewing ray, rather than
integrating repeated entries and exits through several wave crests.

The boundary uses a full-resolution R32F target and D32 depth attachment; the
composite writes a full-resolution color target. Opaque color/depth inputs also
carry URP's copy and storage costs. These are architectural costs, not measured
frame-time or memory budgets.

Render cost scales with screen resolution and grid resolution. Reduce the grid
only after verifying the shortest wave silhouette. Profile strength zeroes
disable visual contributions but do not necessarily eliminate pass cost. Measure
the exact target hardware before establishing a production budget.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| Surface visible, underwater effects missing | Correct renderer data, active feature, Render Graph enabled, base camera |
| Entire sample looks wrong before Play | The scene's scoped pipeline is activated by its controller in Play |
| Wrong body selected | Assign `WaterCamera.BodyOverride`; one volume is composited |
| Mesh and physics disagree | Identity world rotation/scale, shared profile, adequate grid resolution |
| Foam or caustics missing | Opaque receiving geometry, surface/depth separation, nonzero strength |
| Transparent props disappear or tint incorrectly | The water surface writes depth, and refraction uses opaque scene color/depth; integrate transparent content explicitly |
| Floating prop sinks or tips | Float point placement, lift strength, constraints, collision geometry, depth bounds |
| Waterline looks stepped | Increase mesh resolution and inspect the shortest wave wavelength |

Record visual failures separately from compile and test results. The
[validation record](validation.md) states the current evidence and remaining gaps.
