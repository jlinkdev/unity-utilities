# jlinkdev Unity Packages

Independent Unity Package Manager utilities. Install only the packages your
project needs; each package includes setup instructions, a license, a changelog,
and an importable demo.

## Packages

| Package and quick start | Declared requirements | Maturity |
| --- | --- | --- |
| [IK](Packages/com.jlinkdev.ik/README.md) | Unity 2022.3; any pipeline | Experimental; avatar validation remains necessary. |
| [Object Pooling](Packages/com.jlinkdev.object-pooling/README.md) | Unity 2022.3; any pipeline | Initial extraction; no package-specific automated tests yet. |
| [Forcefields](Packages/com.jlinkdev.forcefields/README.md) | Unity 2022.3; URP 14.0.11 | Initial 0.1.0 release. |
| [Portals](Packages/com.jlinkdev.portals/README.md) | Unity 2022.3; URP 14.0.11 | Initial implementation; validate camera and motor integration. |
| [World Scanning](Packages/com.jlinkdev.world-scanning/README.md) | Unity 6; URP 17.0.3; Render Graph | Initial 0.1.0 release. |
| [Volumetric Fog and Rain](Packages/com.jlinkdev.volumetric-fog-and-rain/README.md) | Unity 6; URP 17.0.3; Render Graph; desktop | Prototype with recorded render tests and performance measurements. |
| [Beams](Packages/com.jlinkdev.beams/README.md) | Unity 6; URP 17.0.3 | 1.0.0-pre.1 release candidate. |

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

Rain includes **Rain Laboratory**, **Volume Laboratory**, and **Fog Laboratory**.
Every other package includes one focused demo.

## Develop and validate

Open this repository root in Unity `6000.3.21f1` to work on the embedded packages.
Package runtime code lives in `Runtime`, editor tooling in `Editor`, detailed docs
in `Documentation~`, importable demos in `Samples~`, and existing automated tests
in `Tests`. Host-only fixtures and authoring assets live under
[Assets/PackageDevelopment](Assets/PackageDevelopment/README.md).

Run the static packaging check from the repository root with Python 3.9 or newer:

```text
python scripts/validate-packages.py
```

This checks manifests, guide structure, local documentation links, sample scenes,
and assembly definitions. It does not compile Unity code or certify compatibility.
See [contributing and release checks](CONTRIBUTING.md) for development conventions
and the Unity validation required before publishing a release.
