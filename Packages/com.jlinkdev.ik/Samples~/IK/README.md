# IK Sample

This sample supports a runtime UI similar to the object pooling sample, but uses grid controls to move IK targets and pole hints.

## Scene Setup

Create one scene named `IK Demo` with separate station roots for the fixed controller stations:

- `TwoBoneIK`: simple upper/mid/tip limb, one target grid, and one pole grid.
- `FABRIKChain`: 4-6 joint chain, one target grid, and optionally one pole grid.
- `AimIK`: turret, camera, or spine/neck chain with one target grid.
- Optional `GroundProbe`: ray origin above uneven ground, with the probe transform used as a foot target.

## Controller Setup

1. Add an empty GameObject named `Demo Controller` and add `IKDemoController`.
2. Assign the fixed `Two Bone`, `FABRIK`, `Aim`, and `Ground Probe` station groups you want to test.
3. For each station you are testing, assign its optional station root, IK component, target grid, and optional pole grid.
4. Add an empty GameObject named `Demo Overlay`, add `IKDemoOverlay`, and assign the controller.

If `Show Only Active Station` is enabled, the controller will toggle station roots so only the selected station is visible.

## Grid Setup

For each target or pole:

1. Create a visible target object, usually a small green sphere for targets or magenta cube for poles.
2. Add `IKDemoTargetGrid` to a separate control object or directly to the target object.
3. Assign the target transform.
4. Assign an origin transform near the rig. If you leave origin empty, the grid captures a fallback origin from the target's current position.
5. Choose the grid axes and cell size. A limb facing forward usually works well with horizontal `Vector3.right` and vertical `Vector3.up`.

The overlay shows a 2D pad for each assigned target or pole grid. Dragging the pad point moves the target continuously and solves the active rig.
