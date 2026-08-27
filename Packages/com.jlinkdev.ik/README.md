# jlinkdev IK

Lightweight inverse-kinematics components under the
`jlinkdev.UnityUtilities.IK` namespace.

## Included components

- `TwoBoneIK` solves a root/mid/tip limb with independent position, rotation,
  and pole weights, safe reach clamping, and optional soft reach.
- `FABRIKChain` solves a configurable joint chain.
- `AimIK` rotates a joint chain toward a target.
- `GroundProbe` positions a target from a physics raycast.
- `IKTarget`, `PoleHint`, and `RotationLimit` provide supporting scene components.

Solvers run in `LateUpdate` by default and can also be invoked through `Solve()`.

## Animated two-arm integration

Use one `TwoBoneIK` component per arm and assign a disjoint
upper-arm/forearm/hand hierarchy to each. Target and pole transforms are read in
world space. A uniformly scaled or world-rotated character hierarchy is
supported because bone lengths are measured from the evaluated pose.

For deterministic Animator layering, disable `SolveInLateUpdate` and call
`Solve()` manually from a project-owned `LateUpdate` driver after target poses
have been updated:

1. Let the Animator evaluate the base pose.
2. Update both wrist target transforms and any elbow hints.
3. Set `PositionWeight`, `RotationWeight`, and `PoleWeight`.
4. Call `Solve()` once for each arm.

At zero position and rotation weight, the solver leaves the evaluated Animator
pose unchanged. `Weight` remains available as a combined backwards-compatible
control that sets both position and rotation weights.

`SoftReach` is the fraction of total limb length used to soften the approach to
full extension. Set it to zero for hard clamping. `IsTargetReachable`,
`ReachError`, and `HasValidChain` expose runtime diagnostics.

The solver expects non-zero bone lengths and a hierarchy in which the forearm is
a descendant of the upper arm and the hand is a descendant of the forearm.
End-effector/contact-pose calibration and target smoothing belong to the
implementing project.

## Status

This package is experimental. `TwoBoneIK` has automated coverage for its core
two-arm use cases, but avatar-specific interactive validation is still required.
The other solver components may change while their remaining IK issues are
identified and corrected.

Import the included sample from Package Manager for setup and runtime controls.
