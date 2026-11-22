# jlinkdev Water System

A bounded water system built around one shared wave surface: animated surface
shading, a per-pixel waterline, underwater atmosphere, procedural caustics,
surface queries, point buoyancy, and suspended particles.

## Requirements

- Unity 6 (`6000.0` or newer).
- Universal Render Pipeline `17.0.3` or compatible newer version, with **Render Graph enabled**.
- A forward Universal Renderer with **Water Renderer Feature**.
- A desktop graphics API with the render-target formats required by URP.

These are declared requirements. See the [validation record](Documentation~/validation.md)
for the environments actually checked: Unity `6000.0.58f1`, URP `17.0.4`, and
Windows Direct3D 11 in Gamma color space. Built-in, HDRP, XR, and camera stacking
are outside this version's supported scope.

## Installation

In **Window > Package Manager**, choose **Add package from git URL** and enter:

```text
https://github.com/jlinkdev/unity-utilities.git?path=/Packages/com.jlinkdev.water-system
```

Append `#<tag-or-commit>` to pin a revision. For a local checkout, choose **Add
package from disk** and select this package's `package.json`. For an exported
`.tgz`, choose **Add package from tarball** and select the archive.

Version `0.1.0` is currently local and unpublished. The Git URL requires a remote
commit containing this package.

## Quick start

1. Select the **Universal Renderer Data** asset used by your camera. Choose
   **Tools > jlinkdev > Water System > Add Feature to Selected Renderer**.
2. Create a profile with **Assets > Create > jlinkdev > Water System > Calm Water
   Profile**. Coastal and Stylized presets are also available.
3. Choose **GameObject > jlinkdev > Water System > Water Body**. Assign the
   profile, set **Size**, **Depth**, and mesh **Resolution**, and position the body.
   Keep its world rotation at zero and world scale at one.
4. Add **Water Camera** to your camera for crossing events, body override, and
   debug views. The renderer works without this optional component.
5. For floating objects, add a `Rigidbody` and `WaterBuoyancy`. Set local float
   points around the hull and choose a `Body Override` when bodies overlap.

The feature requests depth and opaque color textures. Importing the package
does not change project render settings. See the [manual](Documentation~/manual.md)
for setup, tuning, and a complete surface query example.

## Sample

Import **Water Laboratory** from the package's **Samples** tab. Open
`Scenes/Water Laboratory.unity` and press Play. Click **Above**, **Waterline**, or
**Below**; use the height slider, automatic tour, and Calm/Coastal/Stylized presets.
The scene contains submerged steps, depth columns, seabed markings, and floating
crates. Controls use IMGUI and do not require an input package.

Water Camera Pass runs on the laboratory camera. Use the Game view to preview its output.
See the [sample guide](<Samples~/Water Laboratory/README.md>).

## Limitations

- Initial local `0.1.0` implementation, not yet published. Automated tests,
  editor/player render checks, and candidate archive sample import passed on
  the recorded configuration. Performance and broader compatibility remain unmeasured.
- Horizontal, finite rectangular bodies; translation only. No rivers, arbitrary
  volume meshes, an infinite ocean, or external water-shader integration.
- One water volume is composited per camera. Multiple separated surface bodies
  can exist; overlapping and simultaneously visible volumes need an explicit
  `WaterCamera.BodyOverride` and are not blended together.
- Broad waves are vertical height fields with at most eight directional waves.
  Detail shading, foam, and caustics are visual approximations.
- Refraction and underwater depth use opaque scene color/depth. The surface
  writes depth for wave self-occlusion; transparent objects behind the interface
  need custom integration. Screen-edge refraction and reflection cameras also
  have screen-space limitations.
- Point buoyancy supplies forces to rigidbodies. It does not implement swimming,
  boat propulsion, displaced-volume fluid simulation, or a character controller.
- XR, overlay cameras, planar reflections, splash effects, and audio
  are outside this release. Hooks are provided for project-specific responses.

## Documentation

- [Manual and API](Documentation~/manual.md)
- [Design and scope](Documentation~/design.md)
- [Validation and release checklist](Documentation~/validation.md)
- [Changelog](CHANGELOG.md)

## License

MIT. See [LICENSE.md](LICENSE.md).
