# unity-utilities
Reusable Unity utilities, shaders, editor tools, and systems for Unity projects.


## Installation Instructions

Requires Unity 2022.3 LTS or later.

There are several ways to install Unity Utilities:

### Package Manager Git URL

The recommended way is to install this library as a Git package using the Unity
Package Manager. First, make sure Git is installed and available in your
system's PATH.

Then add the package using this Git URL:

```text
https://github.com/jlinkdev/unity-utilities.git
```

### Local Package

If you do not want to use Git, download this repository as an archive and
extract it somewhere in your project or on your machine. Then open Unity's
Package Manager and add it with **Add package from disk**.

## Included utilities
- **ObjectPooling**: reusable generic/object-component pooling utilities under `Runtime/ObjectPooling`.

### Object pooling

Use the pooling APIs at the level that matches the project need:

- `ObjectPool<T>`: generic non-Unity-object pooling.
- `GameObjectPool`: code-owned pooling for one prefab.
- `ComponentPool<T>`: code-owned pooling when callers want a component directly.
- `GameObjectPoolHandle`: inspector-friendly scene component for one pooled prefab.
- `GameObjectPoolRegistry`: inspector-friendly scene component for multiple prefab pools looked up by prefab reference.

`GameObjectPoolRegistry` is intended as the scalable default when a project has many pooled prefabs. It owns a serialized list of `GameObjectPoolDefinition` entries, initializes one `GameObjectPool` per prefab, tracks which pool spawned each active instance, and supports despawning by instance.

## IK module (`Runtime/IK`)
A tiny, prototype-friendly set of IK components under `jlinkdev.UnityUtilities.IK` for common procedural rig problems.

### What it is for
- Quick limb, chain, and aiming behaviors
- Procedural gameplay and rapid iteration
- Drop-in scene components with minimal setup

### What it is NOT for
- Full-body or VR IK
- Animator/animation-graph integration frameworks
- Heavy constraint systems or advanced editor tooling

### Quick setup
- **TwoBoneIK**: add to any GameObject, assign `root`, `mid`, `tip`, `target` (optional `pole`), then play.
- **FABRIKChain**: add component, assign `joints` root-to-tip and `target`, tweak iterations/tolerance if needed.
- **AimIK**: assign a joint chain and target, set `localAimAxis` to match the rig's forward axis.
- **GroundProbe**: assign `rayOrigin`, set cast distance/layers, then use this transform as an IK target.

### Update timing
- Solvers run in `LateUpdate` by default and can also be called manually via `Solve()`.

### Known limitations
- These are intentionally lightweight solvers and do not cover advanced animation workflows.
- Pole behavior in FABRIK is a simple bias and may need scene-specific tuning.
- RotationLimit is intentionally basic (single hinge or cone clamp only).
