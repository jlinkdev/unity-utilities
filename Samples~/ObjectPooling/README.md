# Object Pooling Sample

This sample demonstrates two object pooling setup styles:

- `GameObjectPoolHandle`: one scene component configured with one prefab, capacity, and inactive parent.
- `GameObjectPoolRegistry`: one scene component configured with multiple `GameObjectPoolDefinition` entries, then used by passing prefab references to `Spawn`.

## Scene Setup

1. Create three simple prefabs, for example a cube, sphere, and capsule. Add `PooledDemoObject` to each prefab, or let `ObjectPoolingDemoController` add it at runtime.
2. Add scene objects named `Pool Handle`, `Pool Registry`, `Inactive Pooled Objects`, `Active Pooled Objects`, `Spawn Center`, `Demo Controller`, and `Demo Overlay`.
3. Add `GameObjectPoolHandle` to `Pool Handle`. Assign one prefab, set `Initial Capacity` to `10`, `Max Capacity` to `25`, and assign `Inactive Pooled Objects`.
4. Add `GameObjectPoolRegistry` to `Pool Registry`. Assign `Inactive Pooled Objects` as the default inactive parent. Add one definition for each prefab.
5. Add `ObjectPoolingDemoController` to `Demo Controller`. Assign the pool handle, pool registry, active parent, spawn center, and the registry prefabs array. The registry prefab colors array controls which color each registry prefab uses while spawned, making a cube/sphere/capsule setup easy to distinguish.
6. Add `ObjectPoolingDemoOverlay` to `Demo Overlay`. Assign the demo controller, and optionally assign the handle and registry.

The included sample assets are organized into `Scenes`, `Prefabs`, and `Scripts`. The prefabs use their default material and runtime color overrides, so no material assets are required.

## Overlay Controls

- Toggle auto spawn.
- Toggle between single-pool-handle mode and registry mode.
- Spawn one object.
- Spawn a burst.
- Return all active demo objects.
- Prewarm more objects.
- Clear inactive objects.
- Clear all pooled objects for the active mode.

Keyboard shortcuts are intentionally not used. Some Unity projects disable the old input manager, and this sample should run without requiring either input system.

Watch the hierarchy while the scene runs. Active objects move under `Active Pooled Objects`; returned objects move under `Inactive Pooled Objects` when that inactive parent is assigned.
