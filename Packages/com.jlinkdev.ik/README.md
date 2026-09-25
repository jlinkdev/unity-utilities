# jlinkdev IK

Lightweight limb, chain, aiming, and ground-target IK components in the
`jlinkdev.UnityUtilities.IK` namespace.

## Requirements

- Unity `2022.3` or newer.
- No render pipeline dependency.
- Unity Physics module for ground probing (installed as a package dependency).

These are declared requirements, not a test matrix for every editor and platform.

## Installation

In **Window > Package Manager**, choose **Add package from git URL** and paste:

```text
https://github.com/jlinkdev/unity-utilities.git?path=/Packages/com.jlinkdev.ik
```

Append `#<tag-or-commit>` to pin a revision. For a local checkout, use **Add package
from disk** and select this package's `package.json`.

## Quick start

1. Add `TwoBoneIK` to a GameObject for the limb you want to control.
2. Assign **Root**, **Mid**, and **Tip** to an upper-arm/forearm/hand hierarchy (or equivalent).
3. Create a separate target Transform and assign **Target**; optionally assign a **Pole** for bend direction.
4. Enter Play mode and move the target. Adjust **Position Weight**, **Rotation Weight**, and **Pole Weight**.

Solvers run in `LateUpdate` by default. For explicit Animator ordering, disable
`SolveInLateUpdate` and call `Solve()` after updating targets; see the manual.

## Sample

Import **IK** from the package's **Samples** tab. Open `Scenes/IK Demo.unity`
and press Play. Use the overlay to choose a solver station and drag its target
or pole controls. See the [sample guide](Samples~/IK/README.md).

## Limitations

- Experimental. `TwoBoneIK` has automated coverage, but avatar-specific interactive validation remains necessary.
- Other solver components may change as remaining issues are corrected.
- Two-bone chains require nonzero bone lengths and the expected ancestor hierarchy.
- Contact-pose calibration and target smoothing belong to the consuming project.

## Documentation

[Components and animated-rig integration](Documentation~/manual.md) · [Changelog](CHANGELOG.md)

## License

MIT. See [LICENSE.md](LICENSE.md).
