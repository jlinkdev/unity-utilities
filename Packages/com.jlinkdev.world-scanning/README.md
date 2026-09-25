# jlinkdev World Scanning

World-space scan pulses for URP, with surface bands, grids, geometry accents,
material reveal helpers, receiver callbacks, and Timeline integration.

## Requirements

- Unity 6 (`6000.0` or newer).
- URP `17.0.3` or a compatible newer version, with **Render Graph enabled**.
- Timeline `1.8.7` (declared as a package dependency).

These are declared requirements, not a test matrix for every editor and platform.

## Installation

In **Window > Package Manager**, choose **Add package from git URL** and paste:

```text
https://github.com/jlinkdev/unity-utilities.git?path=/Packages/com.jlinkdev.world-scanning
```

Append `#<tag-or-commit>` to pin a revision. For a local checkout, use **Add package
from disk** and select this package's `package.json`.

## Quick start

1. Add **World Scan Renderer Feature** to the Universal Renderer Data used by your camera, or use **Tools > jlinkdev > World Scanning > Add Renderer Feature**.
2. Create a profile with **Assets > Create > jlinkdev > World Scanning > Scan Profile**.
3. Add `ScanEmitter` to a GameObject and assign the profile.
4. Call `Emit()` from gameplay, a UnityEvent, or the component inspector during Play.

For a code-driven scan, see the complete example in the API guide.

## Sample

Import **World Scan Demo** from the package's **Samples** tab. Open
`Scenes/World Scan Demo.unity` and press Play. Use **Emit Pulse** and **Next Profile**
to explore spherical and cylindrical scans. Configure the renderer feature first;
the sample does not change your project's pipeline settings.
See the [sample guide](<Samples~/World Scan Demo/README.md>).

## Limitations

- Initial `0.1.0` release; validate camera stacks, graphics APIs, and target hardware.
- Up to 16 concurrent pulses; overlapping scans increase per-pixel rendering cost.
- Render Graph URP only; no Compatibility Mode, Built-in, or HDRP support.
- No sonar simulation, persistent fog of war, terrain discovery data, or minimap rendering.

## Documentation

[Manual](Documentation~/manual.md) · [API](Documentation~/api.md) ·
[Performance and troubleshooting](Documentation~/performance-and-troubleshooting.md) · [Changelog](CHANGELOG.md)

## License

MIT. See [LICENSE.md](LICENSE.md).
