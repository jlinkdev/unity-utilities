# Rendering and configuration

## Styles

The included **Hover** style draws a 2 px cyan outline. **Selected** draws a 3 px amber outline and a 15 px glow at intensity 1.6. Each target can override either style. Empty slots inherit the corresponding renderer feature setting, which falls back to the included asset. Create your own project assets for editable presets.

| Setting | Range | Effect |
| --- | --- | --- |
| Outline Width | 0–8 camera render pixels | Radius of the exterior contour. Zero skips the distance pass. |
| Outline Color | RGBA | Alpha controls contour opacity. |
| Glow Radius | 0–32 camera render pixels | Extent of the separable blur. Zero skips glow processing. |
| Glow Color | HDR RGB and alpha | RGB tints the added light and alpha scales its contribution. |
| Glow Intensity | 0–4 | Scales the halo independently of the line. Zero skips glow processing. |
| Glow Resolution | Off, Quarter, Half, Full | Applies only to the glow. Half is the default. |
| Show In Scene View | On or off | Shows active highlights in the Scene view when its camera uses the feature. |

Use an outline by itself for precise focus feedback. For a glow by itself, set Outline Width to zero and give the glow a positive radius and intensity. Screen-space widths stay stable as objects move toward the camera. URP render scale changes the number of camera render pixels per display pixel.

## Quality and cost

**Half** processes the blur at roughly a quarter of the full-resolution pixel count. **Quarter** uses roughly one sixteenth and works well for broad halos on large silhouettes. Use **Full** for thin parts, narrow halos, or close-up captures. Downsampling averages coverage before blurring. Small features can lose glow contrast at the lower settings. **Off** removes both blur passes and their downsample while preserving outlines.

The feature skips inactive cameras and enqueues no effect when every target is idle. It groups the visible draw items by shared style asset, captures each active style against opaque depth, then composites only outside the combined highlighted silhouettes. An outline uses a bounded horizontal distance search and a vertical distance comparison for rounded corners. Glow uses horizontal and vertical Gaussian passes with paired bilinear samples and adds light directly to the camera color. Scene Bloom is optional. Different styles require additional full-screen work. Reuse style assets across targets to keep that cost bounded.

Use the Frame Debugger or Render Graph Viewer to inspect **Highlight silhouettes**, **Highlight outline distance**, **Highlight glow downsample**, **Highlight glow horizontal**, **Highlight glow vertical**, and **Highlight exterior composite**. Compare resolutions on the target device at the intended camera resolution.

## State and hierarchy

`SetHovered` and `SetSelected` are independent and idempotent. Read `IsHovered`, `IsSelected`, and `CurrentState` for the two flags and the resolved visual state. Clearing selection restores the hover style if focus remains. Disabling the component hides the effect and re-enabling restores its flags. For pooled objects that should return idle, clear both flags when returning the object.

Each component includes child mesh renderers, including a skinned mesh with conventional opaque skinning. The closest Object Highlight in the hierarchy owns a renderer. Direct child additions/removals and reactivation refresh the cache. After replacing renderer components or changing a deeper hierarchy while active, call `RefreshRenderers()`. Changes to material assignment, object layers, renderer enablement and child active state are checked at draw time.

## Visibility and cameras

The capture tests the mesh against existing opaque depth. An opaque wall completely covering the target hides its highlight. At partial occlusion the contour follows the exposed portion, and its soft halo can extend over the nearby occluder. The package draws before transparent geometry so later transparent surfaces can tint or cover the result. Normal object materials and GameObject layers can be shared with other game systems.

Use a base Game camera with the 3D Universal Renderer. Overlay cameras can draw UI over the result. Preview and reflection cameras are skipped. An ordinary camera render texture is supported. XR, hardware dynamic resolution, split-screen viewports, and compatibility mode are outside this release's validation scope.

The capture shader assumes ordinary opaque triangle meshes with matching vertex positions and depth. Materials with custom vertex animation, depth offsets, double-sided surfaces, special clipping, or custom depth passes can give incomplete contours. Highlight separate parts with the same style to get one silhouette. Individual borders on contacting parts would require a per-object ID capture.

## Troubleshooting

- **No effect.** Verify that the camera's selected renderer has the feature, Render Graph is enabled, the camera includes the object's layer, and the target has an active state. Check whether another opaque object fully covers it.
- **Glow is faint.** Check Glow Resolution, intensity and color alpha. Use a wider radius than the outline to expose more of the halo.
- **A new child is missing.** Call `RefreshRenderers()` after runtime hierarchy surgery and check whether a nested highlight owns that renderer.
