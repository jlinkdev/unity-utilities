# Beams manual

## Mental model

A `Beam` resolves two endpoints, asks one path provider for base strands, applies
path modifiers in inspector order, then sends the resulting `BeamPathBuffer` to
a renderer. Contacts and endpoint visuals observe that same resolved state.

```text
Endpoints -> Path provider -> Ordered modifiers -> Strand buffer
                                             |-> Renderer(s)
                                             |-> Physics contacts
                                             |-> Custom consumers
```

The CPU strand is authoritative. Shader vertex and fragment animation add visual
motion without changing physics or contact results.

## Creating beams

Use **GameObject > jlinkdev > Beams** to create configured continuous, curved,
electrical, or branching examples. A source provider is optional: without one,
the `Beam` object's position and forward direction are used. A target provider,
path provider, and renderer are required.

## Endpoints

- `TransformBeamEndpoint` follows a Transform with local position/direction offsets.
- `RaycastBeamEndpoint` returns surface position, normal, collider, and Transform;
  it can use a ray or sphere cast and optionally remain valid at maximum range.
- `SmoothedBeamEndpoint` decorates any other provider with half-life smoothing.
- `BeamEndpointVisuals` positions and orients authored source/target effect objects.

Custom targeting is implemented by deriving from `BeamEndpointProvider`.

## Paths and modifier order

- `StraightBeamPath` emits a two-point primary strand.
- `BezierBeamPath` uses endpoint forward directions as outward Bezier handles.
- `BeamResampleModifier` adds uniformly spaced CPU samples.
- `BeamSagModifier` adds a smooth directional bow.
- `BeamNoiseModifier` adds continuously evolving structural noise.
- `BeamBranchModifier` creates deterministic child strands.
- `BeamElectricalModifier` creates angular electrical geometry and restrike motion.

Order is significant. For branching lightning, apply the branch modifier before
the electrical modifier so both primary and child strands receive electrical
displacement. For broad noisy lightning, apply structural noise after electrical
displacement. Contact queries always use the final CPU path.

## Rendering

`BeamRibbonRenderer` supports every strand and publishes the full shader vertex
contract. `BeamLineRendererAdapter` maps strands to authored Line Renderers for
compatibility. `BeamRendererGroup` fans the same buffer out to several renderers
for layered core/halo or mixed presentations.

`BeamRenderProfile` centralizes material, width curve, color gradient, branch
width falloff, intensity, and shader-bound padding. Materials control GPU vertex
motion and fragment detail independently of CPU modifiers.

## Contacts

`BeamPhysicsContacts` raycasts or sphere-casts each final CPU segment. It deduplicates
contacts per collider and strand, then reports:

- `ContactEntered`
- `ContactStayed`
- `ContactExited`
- `ContactTicked`

Each `BeamContact` contains collider, position, normal, strand and segment indices,
and distance along the strand. A project decides whether that data means damage,
healing, pulling, highlighting, resource transfer, or something else.

## Time and pulses

Beam time can be scaled, unscaled, or manual. Manual time makes deterministic
previews, replays, and network-controlled presentations possible. `BeamPulseDriver`
animates the standard single-pulse shader property and reports pulse completion.
