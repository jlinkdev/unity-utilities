# jlinkdev Beams

A composable, gameplay-neutral beam construction kit for Unity. The package owns
beam endpoints, strand geometry, presentation, and contact-ready paths; projects
decide what a beam means.

## Features

- Transform, ray/sphere-cast, and smoothed endpoint providers with surface metadata
- Straight and cubic Bezier paths
- Ordered resampling, sag, structural noise, branching, and electrical modifiers
- Strand-based path data for primary, reflected-style custom, chained, and branching extensions
- Neutral ray/sphere contact enter, stay, exit, tick, and polling APIs
- Procedural camera-facing ribbon meshes and a Line Renderer compatibility adapter
- Layered renderer groups, reusable render profiles, endpoint visuals, and shader pulse driving
- A documented shader vertex-data and material-property contract
- URP energy-beam shader with vertex displacement, flow, pulses, flicker, core, and halo controls
- Shader Graph-compatible HLSL functions and native endpoint-mask, flow, pulse, and core/halo subgraphs
- Four ready-to-assign URP beam materials
- Editor creation menus, diagnostics, tests, and an importable demo

## Quick start

1. Choose **GameObject > jlinkdev > Beams > Continuous Beam**.
2. Move its `Beam Target` child.
3. Reorder or add path modifiers on the `Beam` component.
4. Assign one of the materials under `Runtime/Materials`, or create a custom
   material using the documented shader contract.

For code-driven behavior, subscribe to `BeamPhysicsContacts` events or read the
beam's current `BeamPathBuffer`; the package never assigns gameplay meaning.

## Scope

The package reports geometry and contacts but does not implement damage, health,
forces, resource transfer, teams, or other gameplay concepts.

See the [manual](Documentation~/manual.md), [API guide](Documentation~/api.md),
[architecture notes](Documentation~/architecture.md), [shader contract](Documentation~/shader-contract.md),
and [performance guide](Documentation~/performance-and-troubleshooting.md).
