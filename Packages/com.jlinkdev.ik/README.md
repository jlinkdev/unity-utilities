# jlinkdev IK

Lightweight, prototype-friendly inverse-kinematics components under the
`jlinkdev.UnityUtilities.IK` namespace.

## Included components

- `TwoBoneIK` solves a root/mid/tip limb with an optional pole.
- `FABRIKChain` solves a configurable joint chain.
- `AimIK` rotates a joint chain toward a target.
- `GroundProbe` positions a target from a physics raycast.
- `IKTarget`, `PoleHint`, and `RotationLimit` provide supporting scene components.

Solvers run in `LateUpdate` by default and can also be invoked through `Solve()`.

## Status

This package is experimental. Its initial interactive validation is not yet
complete, and solver behavior may change while the remaining IK issues are
identified and corrected.

Import the included sample from Package Manager for setup and runtime controls.
