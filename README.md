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
- `GameObjectPoolDefinitionSet`: reusable ScriptableObject asset containing prefab capacity definitions.
- `GameObjectPoolRegistry`: inspector-friendly scene component for multiple prefab pools looked up by prefab reference.

`GameObjectPoolRegistry` is intended as the scalable default when a project has many pooled prefabs. It can initialize pools from multiple `GameObjectPoolDefinitionSet` assets plus scene-local `GameObjectPoolDefinition` entries. Definitions specify prefab, initial capacity, and max capacity; shared behavior such as inactive parent, activate-on-get, and deactivate-on-return lives on the registry. Scene-local definitions are processed before definition-set assets, so they can override shared assets by prefab reference.

When `Spawn` or `Prewarm` receives a prefab with no initialized definition, the registry logs a warning, creates a runtime pool using its runtime fallback capacity settings, and continues instead of failing the spawn.
