# JLinkDev Unity Packages

A Unity development project and monorepo for independently versioned UPM
packages. Open the repository root directly in Unity to develop and validate
all embedded packages together.

## Packages

| Package | Package ID | Status |
| --- | --- | --- |
| IK | `com.jlinkdev.ik` | Experimental; interactive validation remains incomplete |
| Object Pooling | `com.jlinkdev.object-pooling` | Initial package extraction |

## Install from Git

Add only the packages a project needs through Package Manager using their
repository subfolder URLs.

IK:

```text
https://github.com/jlinkdev/unity-utilities.git?path=/Packages/com.jlinkdev.ik
```

Object Pooling:

```text
https://github.com/jlinkdev/unity-utilities.git?path=/Packages/com.jlinkdev.object-pooling
```

Pin a tag or commit by appending `#<revision>` to the URL.

## Development

The host project currently targets Unity `6000.0.58f1`. Package manifests keep
their minimum supported Editor at Unity `2022.3`; compatibility with that
minimum should be covered by a separate validation run before stable releases.

Each package owns its manifest, assemblies, documentation, changelog, and
samples, and should keep its tests within its own package directory. If one
package begins using another, declare both the UPM package dependency in
`package.json` and the assembly reference in its `.asmdef`.
