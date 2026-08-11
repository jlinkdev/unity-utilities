# Architecture

The package treats a beam as a collection of ordered strands. A simple beam has
one strand; reflected, chained, and branching beams can produce several while
using the same rendering and contact systems.

```text
Endpoint providers
    -> Path provider
    -> Ordered path modifiers
    -> CPU strand data
        -> Contact queries and ticks
        -> Renderer
            -> GPU vertex animation
            -> GPU fragment animation
```

CPU strand geometry is authoritative for endpoint management and physics. Shader
deformation is visual, allowing high-frequency motion without rebuilding physics
or mesh data every frame.

## Gameplay neutrality

Future contact APIs will report colliders, positions, normals, strand indices,
distance along the strand, and contact lifecycle. They will not define damage,
healing, forces, factions, resources, or receiver semantics.
