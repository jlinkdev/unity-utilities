# Changelog

## [0.2.0] - 2026-08-27

- Added independent position, rotation, and pole weights to `TwoBoneIK`.
- Added inner- and outer-reach clamping, configurable soft reach, stable bend
  history, and guards for invalid or degenerate chains.
- Added target reachability and solve-error diagnostics.
- Preserved the combined `Weight` property as a backwards-compatible control.
- Added EditMode coverage for animation preservation, independent weights,
  poles, unreachable targets, transformed rigs, invalid chains, and dual arms.

## [0.1.0] - 2026-08-09

- Extracted the IK module into an independently versioned UPM package.
- Included TwoBoneIK, FABRIKChain, AimIK, GroundProbe, targets, hints, limits,
  custom FABRIK inspector support, and the IK sample scripts.
