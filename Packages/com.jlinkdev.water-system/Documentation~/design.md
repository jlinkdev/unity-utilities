# Design and initial scope

The package follows the broader drop-in water-system direction from the design
discussion. The waterline is its quality bar, and surface rendering, underwater
optics, buoyancy, and queries all use the same water description.

## Shared authority

`WaterProfile` stores shape and appearance. `WaterBody` owns the finite footprint,
depth, surface mesh, and time source. `WaterWaves` evaluates the same broad waves
as the shaders and interpolates the rendered grid. The renderer handles the
displaced boundary and near-plane closure; interaction components consume the
body's query API. No parallel gameplay wave implementation is required.

The first implementation uses a bounded height field: directional sine waves,
an axis-aligned rectangular footprint, and vertical displacement. This keeps
surface intersection, finite volume behavior, and physical interaction coherent
without claiming a full ocean or general fluid solver.

## Included in the initial implementation

- Animated surface with shallow/deep color, refraction, Fresnel/specular shading,
  detail variation, and depth-intersection foam.
- Above-water, partial-submersion, and underwater views with a configurable
  waterline band following the same displaced surface.
- Underwater absorption, fog color/density, saturation, distortion, and procedural
  caustics over submerged opaque geometry.
- Queries, camera crossing hooks, object contact hooks, and multi-point buoyancy.
- An optional suspended-particle cloud clipped to the finite animated volume.
- Calm, Coastal, and Stylized profile presets, explicit setup tools, and a
  self-contained laboratory scene.

## Deliberate boundaries

The first release does not add character movement, swimming, boat propulsion,
ocean simulation, arbitrary river meshes, external shader adapters, physically
derived caustics, reflected cameras, splash particles, or underwater audio.
The contact and camera events are extension points for game-specific behavior.

Potential future work includes richer surface reflections, shoreline inputs,
splash/audio examples, multiple-volume compositing, and wider platform support.
These are future directions, not present API promises.

## Quality criteria

The camera must be able to move slowly through a wave without a whole-screen
effect popping on, leave a finite water body cleanly, and observe floating props
tracking the same visible waves. A profile should explain one consistent water
appearance above and below its surface. Passing compile/tests alone does not
establish those visual criteria; see the [validation record](validation.md).
