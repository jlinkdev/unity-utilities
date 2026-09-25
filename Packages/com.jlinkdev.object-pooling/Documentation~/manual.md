# Object Pooling manual

## Choose an API

| API | Use it for |
| --- | --- |
| `ObjectPool<T>` | Managed reference types, with factory and lifecycle callbacks. |
| `GameObjectPool` | A code-owned pool for one prefab. |
| `ComponentPool<T>` | A prefab pool that returns a component directly. |
| `GameObjectPoolHandle` | One prefab configured in the Inspector. |
| `GameObjectPoolDefinitionSet` | Reusable prefab/capacity definitions; it does not own live instances. |
| `GameObjectPoolRegistry` | Multiple prefab pools, selected by prefab reference. |

## Capacity and return values

Initial capacity prewarms instances. Maximum capacity limits the total tracked
instances, both active and inactive. Initial capacity must be nonnegative and no
larger than the positive maximum. `Prewarm(count)` adds up to `count` instances
within that limit; it is not a target total.

`Get()` / `Spawn()` returns null at exhaustion. `Return()` / `Despawn()` reports
success as a bool. Returning an unknown or already returned instance fails;
GameObject pools also log warnings. Check return values in calling code.

## Lifecycle and ownership

- A code-owned `GameObjectPool` or `ComponentPool<T>` requires its owner to call
  `ClearAll()` when it is finished. `ClearInactive()` only destroys idle instances;
  `ClearAll()` also destroys checked-out instances, so discard outstanding references.
- `GameObjectPoolHandle` calls `ClearAll()` on destruction. A registry does so when
  **Clear All On Destroy** is enabled (the default). Disabling the component alone
  does not destroy its pool.
- `ObjectPool<T>` invokes the supplied `onGet`, `onReturn`, and `onDestroy` callbacks.
  Its factory must produce a fresh, valid instance. Prewarming calls `onReturn`;
  generic cleanup only disposes resources if your `onDestroy` callback does so.
- GameObject pools activate on retrieval and deactivate on return by default.
  An assigned inactive parent receives returned instances. Deactivating an instance
  yourself does not return it to the pool.
- Activation happens before the position/rotation overload applies the new pose.
  Do not assume `OnEnable` sees the spawn pose. For explicit initialization, create
  a code-owned pool with `activateOnGet: false`, use an inactive prefab, set the
  object's state and pose, then activate it yourself.
- Reset project-owned state such as Rigidbody velocity, timers, particles, trails,
  and event subscriptions when spawning or returning. There is no automatic reset interface.
- Avoid destroying pooled instances directly. Destroyed inactive instances are
  discarded when retrieved, but checked-out objects destroyed externally can leave
  stale bookkeeping until the pool is cleared.

## Code-owned pool

```csharp
using jlinkdev.UnityUtilities.ObjectPooling;
using UnityEngine;

public sealed class PoolOwner : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    private GameObjectPool pool;

    private void Awake()
    {
        pool = new GameObjectPool(prefab, initialCapacity: 8, maxCapacity: 64);
    }

    public GameObject Spawn(Vector3 position, Quaternion rotation)
        => pool.Get(position, rotation);

    public bool Return(GameObject instance) => pool.Return(instance);

    private void OnDestroy() => pool?.ClearAll();
}
```

Assign a prefab before Play. The returned object remains owned by `PoolOwner`
until returned or until the owner clears its pool.

## Multiple prefabs

Add `GameObjectPoolRegistry` and assign scene-local definitions or definition-set
assets. Call `registry.Spawn(prefab, position, rotation)` and later
`registry.Despawn(instance)`; the registry remembers the originating pool.
Scene-local definitions are processed first. Duplicate prefab definitions warn and
are ignored. An unknown prefab warns and creates a pool using the registry's
runtime fallback capacities.

## Validation

The importable demo exercises the public workflows. No package-specific automated
test suite is currently included. Before shipping, validate exhaustion, repeated
return/reuse, component cleanup, and scene unload with your actual pooled prefabs.
