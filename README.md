# unity-utilities
Reusable Unity utilities, shaders, editor tools, and systems for Unity projects.


## Installation Instructions

Requires Unity 2022.3 LTS or later.

There are several ways to install Unity Utilities:

### Package Manager

The recommended way is to install this library as a Git package using the Unity
Package Manager. First, make sure Git is installed and available in your
system's PATH.

Then add the package using this Git URL:

```text
https://github.com/jlinkdev/unity-utilities.git
```

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
