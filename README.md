# jlinkdev Unity Packages

Independent Unity Package Manager utilities. Each package includes setup
instructions, a license, and a changelog. Released packages include an importable demo.

## Packages

| Package and quick start | Declared requirements | Maturity |
| --- | --- | --- |
| [Procedural Textures](Packages/com.jlinkdev.procedural-textures/README.md) | Unity 6.0 | Choose among 34 noise, gradient, mask and pattern generators inside a texture asset. |
| [Procedural Meshes](Packages/com.jlinkdev.procedural-meshes/README.md) | Unity 6.0 | Choose among 11 configurable primitives inside a mesh asset. |
| [Procedural Plants](Packages/com.jlinkdev.procedural-plants/README.md) | Unity 6.0 | 2.3.1. Seven seeded houseplants with textured foliage, optional care and propagation. Inspect a young plant up close or grow the same cutting in two pots on the Windowsill. |
| [IK](Packages/com.jlinkdev.ik/README.md) | Unity 2022.3; any pipeline | Experimental; avatar validation remains necessary. |
| [Object Pooling](Packages/com.jlinkdev.object-pooling/README.md) | Unity 2022.3; any pipeline | Initial extraction; no package-specific automated tests yet. |
| [Sample Framework](Packages/com.jlinkdev.sample-framework/README.md) | Unity 2022.3, UI Toolkit and IMGUI | Optional package and sample headers, controls, fonts, icons, and grid environments. Includes Caption Preview. |
| [Object Highlighting](Packages/com.jlinkdev.object-highlighting/README.md) | Unity 6.0, URP 17.0.4, Render Graph, desktop | Per-object hover contours and selection glows with an interactive Selection Gallery. |
| [Forcefields](Packages/com.jlinkdev.forcefields/README.md) | Unity 2022.3; URP 14.0.11 | Initial 0.1.0 release. |
| [Portals](Packages/com.jlinkdev.portals/README.md) | Unity 2022.3; URP 14.0.11 | Initial implementation; validate camera and motor integration. |
| [World Scanning](Packages/com.jlinkdev.world-scanning/README.md) | Unity 6; URP 17.0.3; Render Graph | Initial 0.1.0 release. |
| [Blueprint Rendering](Packages/com.jlinkdev.blueprint-rendering/README.md) | Unity 6, URP 17, Render Graph, desktop | Bake-free technical drawings and a ten-piece exploded gearbox demo. |
| [Volumetric Fog and Rain](Packages/com.jlinkdev.volumetric-fog-and-rain/README.md) | Unity 6, URP 17.0.3, Render Graph, desktop. Interactive ground mist requires compute shaders. | Volumetric weather, wet materials, splashes and interactive GPU ground mist. |
| [Procedural Nebulae](Packages/com.jlinkdev.procedural-nebulae/README.md) | Unity 6, URP 17, Render Graph, compute-capable desktop GPU | Seeded star-lit cloud volumes and the Starlit Passage fly-through. |
| [Water System](Packages/com.jlinkdev.water-system/README.md) | Unity 6; URP 17.0.3; Render Graph; desktop | Local 0.1.0 implementation; [tests, Windows player, and sample import checked](Packages/com.jlinkdev.water-system/Documentation~/validation.md). Not yet published. |
| [Beams](Packages/com.jlinkdev.beams/README.md) | Unity 6; URP 17.0.3 | 1.0.0-pre.1 release candidate. |
| [Procedural Picture Frames](Packages/com.jlinkdev.procedural-frames/README.md) | Unity 6.0 | Sculptable frame mouldings, a playable workshop and mesh export for tiled materials or Substance Painter. |

Requirements are declared minimums, not a claim that every newer editor, graphics
API, or platform has been tested. Each guide describes scope and known limitations.
Consult the license inside each package; Portals carries a different notice from
the MIT packages.

## Install

In **Window > Package Manager**, choose **Add package from git URL**. Copy the
package-specific URL from its quick start. For example:

```text
https://github.com/jlinkdev/unity-utilities.git?path=/Packages/com.jlinkdev.volumetric-fog-and-rain
```

Append `#<tag-or-commit>` to pin a revision. For a local checkout, choose **Add
package from disk** and select the desired package's `package.json`.

## Try a demo

Select an installed package in Package Manager, expand **Samples**, and import a
demo. Open the scene described in its sample README under
`Assets/Samples/<display-name>/<version>/`. Rendering samples require their stated
pipeline; sample guides explain any additional renderer setup or temporary changes.

Volumetric Fog and Rain includes **Rain Laboratory** (courtyard and shelter stations), **Fog Laboratory**, and **Mist Sanctuary**.
Water includes **Water Laboratory**, with animated waves, waterline transitions,
underwater atmosphere, and floating props. Procedural Plants includes **Windowsill**
and **Plant Studio**. Beams includes **Beam Kit Demo**, **Energy Studies**, and **Orbital Descent**.

## Develop and validate

The procedural asset records cover the [asset lifecycle](docs/procedural-assets-phase-1.md) and the [foundational generators and Inspector selector](docs/procedural-assets-phase-2.md).

Open this repository root in Unity `6000.3.21f1` to work on the embedded packages.
Package runtime code lives in `Runtime`, editor tooling in `Editor`, detailed docs
in `Documentation~`, importable demos in `Samples~`, and existing automated tests
in `Tests`. Edit demos under `Assets/PackageSamples/<package-id>/<sample>/Scenes`. Publish from the Project window with **Assets > jlinkdev > Publish Package Sample**. See the [sample authoring guide](Assets/PackageSamples/README.md). Host-only fixtures and builders live under [Assets/PackageDevelopment](Assets/PackageDevelopment/README.md).

Run the static packaging check from the repository root with Python 3.9 or newer:

```text
python scripts/validate-packages.py
```

This checks manifests, guide structure, local documentation links, sample scenes,
and assembly definitions. It does not compile Unity code or certify compatibility.
See [contributing and release checks](CONTRIBUTING.md) for development conventions
and the Unity validation required before publishing a release.
