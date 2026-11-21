# Validation

The package was installed by local UPM path in two isolated Windows 11 projects and rendered on an NVIDIA GeForce RTX 2060 SUPER using Direct3D 11.

| Editor | URP | Color space | Result |
| --- | --- | --- | --- |
| 6000.0.58f1 | 17.0.4 | Gamma | 9 of 9 EditMode rendering tests passed |
| 6000.3.21f1 | 17.3.0 | Linear | 9 of 9 EditMode rendering tests passed |

The tests read camera pixels for hover and selection priority, pointer exit, restoration of hover, disabling the component, exterior-only composition, adjacent styles, opaque wall occlusion, culling masks, all four glow settings, a skinned child with a glow-only override, and 4× MSAA.

## Selection Gallery

Unity Package Manager imported the sample in the 6000.0.58f1 project. A scan of the imported scene found zero missing scripts. The scene was built and run as a Windows x64 player. A validation harness captured its camera to a 1280 × 720 render texture with hover and selection active, then captured the same scene with both effects disabled. At a threshold of five 8-bit channel levels, 2,358 pixels changed around the gear, 10,218 around the impeller, and zero around the idle coupling.

The preview shows the player camera output. The interaction labels are drawn separately by the sample's IMGUI controller. [Gallery capture](selection-gallery.png)

The gallery also passed an end-to-end Play Mode check in the 6000.3.21f1 project with Input System 1.20.0 as its only input backend. A virtual mouse hovered the coupling, clicked to transfer selection from the impeller, moved to empty space with selection preserved, then clicked to clear it. The 6000.0.58f1 player used the legacy Input Manager.

## Quality measurements

The 6000.0.58f1 editor rendered the gallery to a 1280 × 720 target at 4× MSAA on the same GPU. Each mode received 40 warm-up renders and 71 measured renders in rotating order. The times include the whole camera render and a synchronous one-pixel readback.

| Gallery state | Median elapsed time |
| --- | ---: |
| Both parts idle | 0.59 ms |
| Hover and selection, glow off | 0.93 ms |
| Quarter-resolution glow | 0.97 ms |
| Half-resolution glow | 1.00 ms |
| Full-resolution glow | 1.10 ms |

These measurements include editor CPU work and GPU synchronization. Game scenes with different resolutions, hardware, or counts of distinct style assets will have different costs.

## Coverage

These results cover the tested desktop GPU and D3D11 configurations. Compatibility with other graphics APIs, mobile GPUs, XR, renderer compatibility mode, alpha-clipped targets, and custom vertex deformation has not been established.
