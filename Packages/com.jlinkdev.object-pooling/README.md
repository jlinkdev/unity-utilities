# jlinkdev Object Pooling

Reusable managed-object, GameObject, component, and prefab-registry pools in the
`jlinkdev.UnityUtilities.ObjectPooling` namespace.

## Requirements

- Unity `2022.3` or newer.
- No render pipeline or other jlinkdev package dependency.

These are declared requirements, not a test matrix for every editor and platform.

## Installation

In **Window > Package Manager**, choose **Add package from git URL** and paste:

```text
https://github.com/jlinkdev/unity-utilities.git?path=/Packages/com.jlinkdev.object-pooling
```

Append `#<tag-or-commit>` to pin a revision. For a local checkout, use **Add package
from disk** and select this package's `package.json`.

## Quick start

1. Add `GameObjectPoolHandle` to a scene object and assign a prefab.
2. Set **Initial Capacity** and **Max Capacity** (for example, 8 and 64).
3. Add the component below to another object and assign the handle to its **Pool** field.
4. Call `SpawnOne()` and `ReturnOne()` from your game or UI.

```csharp
using jlinkdev.UnityUtilities.ObjectPooling;
using UnityEngine;

public sealed class PoolExample : MonoBehaviour
{
    [SerializeField] private GameObjectPoolHandle pool;
    private GameObject instance;

    public void SpawnOne()
    {
        if (pool == null || instance != null) return;
        instance = pool.Spawn(transform.position, transform.rotation);
        // Null means no instance was available (for example, capacity was reached).
    }

    public void ReturnOne()
    {
        if (pool != null && instance != null) pool.Despawn(instance);
        instance = null;
    }

    private void OnDisable() => ReturnOne();
}
```

The handle owns its instances and destroys them when the handle is destroyed.

## Sample

Import **Object Pooling** from the package's **Samples** tab. Open
`Scenes/Object Pooling Demo.unity` and press Play. The overlay switches between
single-prefab and registry pools and exposes spawn, return, prewarm, and clear controls.
See the [sample guide](Samples~/ObjectPooling/README.md).

## Limitations

- Initial extraction; there is no package-specific automated test suite yet.
- Capacity limits all tracked instances, including those currently checked out.
- Return objects to their owner instead of destroying them or only deactivating them.
- Gameplay state is not reset automatically; reset velocity, timers, subscriptions, and other state yourself.
- Unity-backed pools must be used on the main thread; the generic pool is not thread-safe.

## Documentation

[API choices, lifecycle, and ownership](Documentation~/manual.md) · [Changelog](CHANGELOG.md)

## License

MIT. See [LICENSE.md](LICENSE.md).
