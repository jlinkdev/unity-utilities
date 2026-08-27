# Changelog

## [Unreleased]

- Expanded the Portal Playground sample with a tabletop Size Lab, recursive 1:4 traversal, scale-aware first-person movement, and a live scale readout.
- Added a main-camera-only near-plane aperture cap to prevent the source world flashing through the portal during camera traversal.
- Stabilized close-range oblique portal-camera planes to prevent degenerate-frustum errors during camera traversal.
- Enabled traversal on the recursive playground pair and audited all sample portal links and trigger references.
- Added a distinct inactive portal backside and restricted traversal and off-screen rendering to the active front face.
- Fixed recursive camera poses so every level advances through the same entry-to-exit transform instead of alternating back toward the source camera.
- Added a configurable animated energy horizon for the final bounded recursion level.
- Fixed traveller transition clones retaining the wrong side of the destination portal plane.
- Added a small configurable clip-plane overlap to prevent cracks at the transition seam.
- Added portal-surface depth bias so directly rendered travellers win sub-pixel depth ties at the seam.
- Reduced the oblique near-clip safety offset and clamp to avoid visible gaps without allowing zero-plane flicker.
- Removed the sample portal surface tint so portal views preserve the source camera color.

## [0.1.0] - 2026-08-10

- Added paired planar portal rendering for URP.
- Added recursive views with oblique near-plane clipping and reusable render textures.
- Added Transform, Rigidbody, and CharacterController-compatible traversal.
- Added uniform scale mapping and renderer cloning during transitions.
- Added clip-compatible shaders, Shader Graph helper, editor setup tools, tests, and a playground sample.
