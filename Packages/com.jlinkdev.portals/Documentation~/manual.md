# Portals manual

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
