# jlinkdev Portals

Linked planar URP portals with recursive views, traveller teleportation,
velocity mapping, and material clipping during crossings.

## Requirements

- Unity `2022.3` or newer.
- URP `14.0.11` or a compatible newer version.
- One gameplay camera tagged `MainCamera`; runtime has no input-package dependency.

These are declared requirements, not a test matrix for every editor and platform.

## Installation

In **Window > Package Manager**, choose **Add package from git URL** and paste:

```text
https://github.com/jlinkdev/unity-utilities.git?path=/Packages/com.jlinkdev.portals
```

Append `#<tag-or-commit>` to pin a revision. For a local checkout, use **Add package
from disk** and select this package's `package.json`.

## Quick start

1. Choose **GameObject > jlinkdev > Portals > Create Linked Portal Pair**.
2. Position and rotate the two generated portal roots.
3. Add `PortalTraveller` to objects that should pass through a portal.
4. Assign a supplied portal-clipped material to traveller visuals for cross-plane slicing.

Portal fronts show the linked view and accept traversal; backs are inactive.

## Sample

Import **Portal Playground** from the package's **Samples** tab. Open
`Scenes/Portal Playground.unity` and press Play in a URP project. Use WASD and
mouse look; press R to reset and Escape to release the pointer. The scene includes
1:1 traversal, recursion, Rigidbody transitions, and a 1:4 tabletop Size Lab.
See the [sample guide](<Samples~/Portal Playground/README.md>).

## Limitations

- Initial implementation; validate your camera and character motor integration.
- URP only; portals are planar and linked in pairs.
- Scaling is uniform; non-uniform portal scale is reduced to a uniform aperture ratio.
- Traveller materials must implement portal clipping to slice during crossings.
- Recursion is capped to control rendering cost. Custom motors can implement
  `IPortalVelocityProvider` to preserve their own velocity state.

## Documentation

[Rendering, traversal, and custom shaders](Documentation~/manual.md) · [Changelog](CHANGELOG.md)

## License

See [LICENSE.md](LICENSE.md). This package carries an all-rights-reserved notice.
