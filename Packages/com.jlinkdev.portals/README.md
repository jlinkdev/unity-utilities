# jlinkdev Portals

Seamless, linked planar portals for the Universal Render Pipeline. The package renders the world beyond each portal, teleports supported travellers while preserving motion, and includes clipping support for clean transitions.

## Requirements

- Unity 2022.3 or newer
- Universal Render Pipeline 14 or newer
- One gameplay camera tagged `MainCamera`

## Quick start

1. Choose **GameObject > jlinkdev > Portals > Create Linked Portal Pair**.
2. Position and rotate the two generated portal roots.
3. Add `PortalTraveller` to anything that should pass through a portal.
4. Use a supplied portal-clipped material on traveller visuals when you want clean cross-plane slicing.

The runtime does not depend on any input package. Import **Portal Playground** from Package Manager for a complete scene with 1:1 traversal, bounded recursion, Rigidbody transitions, and a recursive 1:4 tabletop Size Lab.

## Rendering

Each `Portal` references exactly one linked portal. Rendering uses an off-screen URP camera, an oblique projection plane, frustum checks, reusable render textures, and a configurable recursion limit. Repeated views accumulate the same entry-to-exit transform, and the final level resolves to a configurable animated energy horizon instead of an accidental black frame. `PortalRenderSettings` assets can be shared by any number of pairs.

When the gameplay camera reaches a portal's near plane, a separate camera-local aperture cap preserves the live view through the crossing frame. The cap intersects camera rays with the real portal plane, clips itself to the portal bounds, and is disabled for every recursive portal-camera pass. This avoids both the one-frame source-world flash and invalid projection-matrix workarounds.

Portals are one-sided: the front renders and accepts traversal, while the reverse side displays a dark inactive panel and ignores entry. This keeps freestanding portal geometry visually and mechanically unambiguous.

## Traversal and scaling

`PortalTraveller` supports ordinary transforms, `Rigidbody`, and `CharacterController`. Rigidbody linear and angular velocities are mapped through the pair. Character motors can implement `IPortalVelocityProvider` so their velocity is mapped as well. Uniform portal scaling is optional and scales traveller position, local scale, and linear velocity by the exit-to-entry ratio.

## Custom shaders and Shader Graph

The included **Portal Clipped Lit** shader responds to per-renderer `_PortalClipPlane` and `_PortalClipEnabled` values. For Shader Graph, add a Custom Function node that references `Runtime/Shaders/PortalClip.hlsl`, select function `PortalClip_float`, and route its `Keep` output into Alpha with an Alpha Clip Threshold of `0.5`. Use **Position (World)** as `PositionWS`.

## Limitations

- URP only in this release.
- Portals are planar and linked in pairs.
- Scaling is uniform; non-uniform portal scale is reduced to a uniform aperture ratio.
- A traveller's materials must implement the clipping properties to be sliced during crossing.
- Recursive rendering is intentionally capped to control GPU cost.
