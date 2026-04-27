# unity-utilities
Reusable Unity utilities, shaders, editor tools, and systems for Unity projects.

## Installation

### Install from Git URL
In Unity Package Manager, use **Add package from git URL...** and provide this repository URL.

### Install from local disk
In Unity Package Manager, use **Add package from disk...** and select this package's `package.json`.

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
