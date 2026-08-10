# jlinkdev Object Pooling

Reusable pooling utilities under the
`jlinkdev.UnityUtilities.ObjectPooling` namespace.

## Included APIs

- `ObjectPool<T>` for generic managed objects.
- `GameObjectPool` for code-owned, single-prefab pools.
- `ComponentPool<T>` when callers want a pooled component directly.
- `GameObjectPoolHandle` for inspector-configured single-prefab pools.
- `GameObjectPoolDefinitionSet` for reusable prefab capacity definitions.
- `GameObjectPoolRegistry` for multiple prefab pools selected by prefab reference.

The registry accepts scene-local definitions and reusable definition-set assets.
When asked to spawn an unknown prefab, it logs a warning and creates a pool from
its configured runtime fallback capacities.

Import the included sample from Package Manager for a complete demonstration.
