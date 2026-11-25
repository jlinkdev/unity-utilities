# jlinkdev Unity Packages

Unity Package Manager utilities for procedural assets, rendering, animation,
and gameplay.

> [!NOTE]
> **From jlink**
>
> This is an experimental collection of Unity packages. I use it to try out
> features I've been thinking about, bring over systems from other projects,
> and capture ideas as they come up. I decide what to build and use Codex to
> write the code.
>
> I've reviewed some packages and use some in my own projects. Others I haven't
> looked at since Codex generated them. They're at different stages of
> completeness, so check the individual package READMEs for current status and
> known limitations.
>
> My goal is for each package to have clear docs, a runnable sample, and
> performance suited to what it does. I'm working on unifying the samples
> visually, as well as implementing more cohesive and human-friendly architecture.

## Packages

| Package | Declared requirements | What it does |
| --- | --- | --- |
| [Beams](Packages/com.jlinkdev.beams/README.md) | Unity 6.0, URP 17.0.3 | Straight, curved, and branching beams with animated materials and physics contact events. |
| [Blueprint Rendering](Packages/com.jlinkdev.blueprint-rendering/README.md) | Unity 6.0, URP 17.0.3, Render Graph, desktop | Draw opaque 3D assemblies as technical blueprints at runtime. |
| [Forcefields](Packages/com.jlinkdev.forcefields/README.md) | Unity 2022.3, URP 14.0.11 | Forcefield surfaces with glow, refraction, and impact ripples. |
| [IK](Packages/com.jlinkdev.ik/README.md) | Unity 2022.3, any render pipeline | Limb, chain, and aim solvers with ground probes. |
| [Object Highlighting](Packages/com.jlinkdev.object-highlighting/README.md) | Unity 6.0, URP 17.0.4, Render Graph, desktop | Separate hover and selection outlines and glows for 3D objects. |
| [Object Pooling](Packages/com.jlinkdev.object-pooling/README.md) | Unity 2022.3, any render pipeline | Reuse managed objects, GameObjects, components, and prefabs. |
| [Portals](Packages/com.jlinkdev.portals/README.md) | Unity 2022.3, URP 14.0.11 | Paired planar portals with recursive views, teleportation, and velocity mapping. |
| [Procedural Meshes](Packages/com.jlinkdev.procedural-meshes/README.md) | Unity 6.0 | Generate mesh assets from configurable primitives with UV and pivot controls. |
| [Procedural Nebulae](Packages/com.jlinkdev.procedural-nebulae/README.md) | Unity 6.0, URP 17.0.3, Render Graph, desktop compute shaders | Generate seeded volumetric clouds lit by movable stars. |
| [Procedural Picture Frames](Packages/com.jlinkdev.procedural-frames/README.md) | Unity 6.0 | Shape rectangular and oval picture frames with sculpted moulding and OBJ export. |
| [Procedural Plants](Packages/com.jlinkdev.procedural-plants/README.md) | Unity 6.0 | Grow seeded houseplants with optional watering and propagation. |
| [Procedural Pots](Packages/com.jlinkdev.procedural-pots/README.md) | Unity 6.0 | Shape vessels on a virtual turntable and generate meshes from editable recipes. |
| [Procedural Textures](Packages/com.jlinkdev.procedural-textures/README.md) | Unity 6.0 | Generate noise, gradients, masks, and patterns as editable texture assets. |
| [Sample Framework](Packages/com.jlinkdev.sample-framework/README.md) | Unity 2022.3, UI Toolkit and IMGUI | Shared UI controls, headers, fonts, icons, and environments for samples. |
| [Volumetric Fog and Rain](Packages/com.jlinkdev.volumetric-fog-and-rain/README.md) | Unity 6.0, URP 17.0.3, Render Graph, desktop. Ground mist requires compute shaders. | World-space rain and fog, wet surfaces, splashes, and interactive ground mist. |
| [Water System](Packages/com.jlinkdev.water-system/README.md) | Unity 6.0, URP 17.0.3, Render Graph, desktop | Animated waves, waterlines, underwater effects, and buoyancy. |
| [World Scanning](Packages/com.jlinkdev.world-scanning/README.md) | Unity 6.0, URP 17.0.3, Render Graph | World-space scan pulses with material reveals, receiver callbacks, and Timeline integration. |

Each package README covers setup, samples, and licensing.

## Install

In Unity, open **Window > Package Manager**. From the **+** menu, choose
**Install package from git URL** (**Add package from git URL** in Unity 2022.3).
Use the URL below with the folder name of the package you want.

```text
https://github.com/jlinkdev/unity-utilities.git?path=/Packages/com.jlinkdev.volumetric-fog-and-rain
```

Append `#<full-commit-hash>` to pin a revision. For a local checkout, choose
the disk option from the same menu and select the package's `package.json`.
