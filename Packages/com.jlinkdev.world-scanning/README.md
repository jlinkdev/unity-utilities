# jlinkdev World Scanning

A focused, production-oriented world scan system for Unity's Universal Render Pipeline. Emit spherical or cylindrical pulses that travel through scene geometry, draw a triplanar world grid and geometry accents, reveal compatible materials, and notify gameplay objects when the scan front reaches them.

## Requirements

- Unity 6 (`6000.0` or newer)
- Universal Render Pipeline 17
- Render Graph enabled (the Unity 6 URP default)

## Features

- Up to 16 concurrent world-space pulses with allocation-free runtime updates
- Spherical and height-limited cylindrical scan shapes
- Profile assets for range, duration, curves, color, band, trail, grid, edges, noise, and distance fade
- URP Renderer Feature compositing from camera depth and normals
- `ScanEmitter`, handle-based scripting API, receiver callbacks, and completion events
- HLSL helpers for Shader Graph and a ready-to-use reveal Lit shader
- Timeline track and clip support
- Scene-view gizmos, setup validation, custom inspectors, EditMode tests, and a focused greybox sample

## Quick start

1. Add **World Scan Renderer Feature** to the Universal Renderer Data used by your camera. You can do this with **Tools > jlinkdev > World Scanning > Add Renderer Feature**.
2. Create a profile with **Assets > Create > jlinkdev > World Scanning > Scan Profile**.
3. Add a `ScanEmitter` to a GameObject and assign the profile.
4. Call `Emit()` from gameplay, animation, a UnityEvent, or the component inspector while playing.

```csharp
using jlinkdev.UnityUtilities.WorldScanning;
using UnityEngine;

public sealed class ScannerTool : MonoBehaviour
{
    [SerializeField] private ScanProfile profile;

    public void Scan()
    {
        ScanHandle handle = ScanSystem.Emit(transform.position, profile);
        handle.SetIntensity(0.8f);
    }
}
```

Import **World Scan Demo** from Package Manager's Samples tab for a complete renderer setup and a professional greybox showcase.

See [the manual](Documentation~/manual.md), [API guide](Documentation~/api.md), and [performance and troubleshooting guide](Documentation~/performance-and-troubleshooting.md).

## Scope

This package owns world scan pulses and scan-driven reveal hooks. It intentionally does not include waterline, underwater, sonar simulation, fog-of-war persistence, terrain discovery data, or minimap rendering.

## License

MIT. See [LICENSE.md](LICENSE.md).
