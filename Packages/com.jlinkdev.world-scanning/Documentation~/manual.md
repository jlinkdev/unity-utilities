# World Scanning manual

## Rendering setup

World Scanning is a URP Render Graph effect. Every camera that should display scans must use Universal Renderer Data containing `ScanRendererFeature`.

Use **Tools > jlinkdev > World Scanning > Validate Project Setup** to inspect the active default renderer. If it is missing, choose **Add Renderer Feature** or add **World Scan Renderer Feature** from the renderer asset's feature list.

The feature runs before post-processing by default so Bloom and tonemapping can shape the emissive result. It requests depth and normals and can be enabled independently for Game, Scene, Preview, and Reflection cameras.

## Profiles

A `ScanProfile` is the reusable visual and timing contract for a pulse:

- **Pulse:** shape, duration, range, animated radius/intensity/color, cylinder height, and scaled or unscaled time.
- **Surface Band:** width, softness, trailing fill, and emission.
- **World Grid:** triplanar cell sizing, line width, major-line cadence, and layer intensity.
- **Geometry Accents:** depth and normal discontinuity highlighting.
- **Variation:** procedural breakup, animation speed, and camera-distance fading.

Keep gameplay meaning outside the profile. A profile describes how a scan travels and looks; `ScanReceiver` and your own event listeners determine what being scanned does.

## Emitting scans

`ScanEmitter` is the authoring-friendly entry point. Its origin and cylindrical axis can come from optional transforms, and each emitter can override shape, range, duration, and intensity.

For code-driven scans, call `ScanSystem.Emit`. A `ScanEmission` allows per-use overrides without duplicating profiles. The returned `ScanHandle` supports cancellation, intensity changes, radius queries, and normalized-time queries.

When the 16-pulse capacity is reached, the oldest active pulse ends with `ScanCompletionReason.Replaced` before the new pulse starts.

## Gameplay receivers

Add `ScanReceiver` to an object that should react when a pulse front reaches it. Use its serialized `On Scanned` UnityEvent for simple authoring, or subscribe to `Scanned` for a typed `ScanHit` containing the handle, origin, receiver point, distance, and normalized hit time.

A receiver is notified once for each expanding pulse. For a cylinder, it must also be within the profile's half-height along the scan axis.

## Timeline

Add a **World Scan Track** to a Timeline and place a **World Scan Clip** on it. Assign the exposed `ScanEmitter` reference on the clip. In Play mode, the clip emits when playback enters it and rearms when Timeline rewinds or stops. Edit-mode Timeline preview intentionally does not create runtime scans.

## Scan-aware materials

Use the included **jlinkdev/World Scanning/Scan Reveal Lit** shader for a ready-made reveal material, or integrate `WorldScan.hlsl` into Shader Graph with a Custom Function node.

For Shader Graph, set the source to **File**, point it at:

`Packages/com.jlinkdev.world-scanning/Runtime/Shaders/WorldScan.hlsl`

Set the Custom Function name to `WorldScanEvaluate` for `Band`, `Fill`, `Coverage`, and HDR `Color`, or `WorldScanReveal` for a single reveal mask. Shader Graph selects the `_float` implementation from the include. Supply world-space position and normalized world-space normal.

## Camera stacking

The feature evaluates independently on each eligible camera. In a URP camera stack, normally enable the feature on the base renderer and avoid applying it again on an overlay renderer unless the overlay has independent depth-bearing world content.
