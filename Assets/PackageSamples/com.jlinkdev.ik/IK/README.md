# IK Sample

## Run the included scene

Open `Scenes/IK Demo.unity` in this imported sample and press Play.
Use the on-screen overlay to control the demo. The editable Studio Grid uses the optional Sample Framework floor and lighting assets.

Use the grid controls to move IK targets and pole hints.

## Optional: rebuild or customize the scene

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

1. Create a visible target object, usually a small teal sphere for targets or amber cube for poles.
2. Add `IKDemoTargetGrid` to a separate control object or directly to the target object.
3. Assign the target transform.
4. Assign an origin transform near the rig. If you leave origin empty, the grid captures a fallback origin from the target's current position.
5. Choose the grid axes and cell size. A limb facing forward usually works well with horizontal `Vector3.right` and vertical `Vector3.up`.

The overlay shows a 2D pad for each assigned target or pole grid. Dragging the pad point moves the target continuously and solves the active rig.

## Sample presentation

On import, choose **Install** to use the shared fonts, icons, and sample header. To install later, choose **Tools > jlinkdev > IK > Install Sample Framework**. Before the release tag is published, use Package Manager's **Install package from disk** and select `Packages/com.jlinkdev.sample-framework/package.json` from a Unity Utilities checkout.
