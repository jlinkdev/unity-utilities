# Render timing and transparent output

## Injection point

Set **Injection Point** on `RainRendererFeature`. Timing is a renderer-level choice,
not a blendable profile value. Supported points are:

| Setting | Position | Consequence |
| --- | --- | --- |
| Before Post Processing (default) | After transparent geometry, before post-processing | Fog/rain receives exposure, bloom, and tone mapping. Transparent geometry is already in scene color. |
| Before Transparent Capture | After skybox, before URP's opaque-color copy | The opaque texture used by refraction can contain the effect. Transparent objects render afterward. |
| Before Transparents | After opaque-color copy, before transparent geometry | Transparent objects render over the effect; the earlier opaque capture does not contain it. |
| After Post Processing | After URP's main post-processing pass | Fog/rain bypasses that pass's exposure, bloom, and tone mapping. Final AA/output conversion may still follow. |

The capture setting targets URP's standard opaque-color copy. It does not reorder
custom refraction captures; those must be scheduled explicitly relative to this
feature. Enable Opaque Texture if your refractive materials need that capture.
The feature requests depth and an intermediate color target at every supported
point. Its composite preserves the active color attachment's MSAA sample count,
so later geometry can pair it with the existing depth attachment. It does not
force MSAA on a target that URP has already resolved. Verify custom renderer features and camera setups with Frame Debugger.

Timing cannot reconstruct transparent depth. Ordinary blended surfaces generally
leave only the opaque surface behind them in the depth buffer. Running after them
fogs their already-composited color with that depth; running before them lets them
composite over the fog. Neither is a general solution for multiple transparent
layers or refraction through spatially varying fog. Such materials need their own
fog evaluation or an additional depth/layer integration strategy. Avoid applying
the same fog twice in those materials and the fullscreen pass.

## Interactive ground fog

On the **Volumetric Rain** renderer feature used by the camera, enable **Share Atmosphere With Ground Fog**. Enable Interactive Fog's renderer feature or camera pass on the same camera. Fog Laboratory ships with this setup. Existing renderer assets leave the option off.

While sharing active fog, the weather renderer runs after the skybox and before the transparent pass. It supplies the accumulated light and transmission between the camera and a set of distances along each view ray. Interactive Fog samples that information at each contributing point in its volume. A bank of weather fog in front of a wisp lowers its contrast and adds haze color. A bank behind the wisp affects the view beyond it. This also works when the sky is visible through the ground mist.

The atmospheric field uses the active weather settings, including the camera intensity, maximum range, inclusion and exclusion boxes, feathered edges, noise, height falloff and main-light scattering. The renderer stores 32 depth slices with closer spacing near the camera. At 1920 × 1080 the field occupies about 2 MiB of temporary GPU memory. Narrow weather boundaries can soften between samples. Render Graph skips this work when no pass reads the field. Rain streaks retain their existing rendering. Ground-mist simulation, wind and obstacles are owned by Interactive Fog.

With **Preserve Scene Alpha**, the combined fog keeps the camera's input alpha. With **Premultiplied**, both the atmospheric fog and the ground mist contribute to the output opacity.

## Output mode

**Preserve Scene Alpha** is the compatibility default. It changes scene RGB and
retains source alpha. Use it for an ordinary scene render. Its alpha does not
represent the added medium and must not be treated as a reusable fog layer.

**Premultiplied** is for a camera/render texture composited over another image:

- Input RGB must already be premultiplied by input alpha, in the working linear
  color space. This setting does not convert arbitrary straight-alpha inputs.
- The pass attenuates source RGB and adds in-scattered radiance. It also increases
  opacity for the medium. RGB is not clamped to alpha or to 1: HDR light is valid.
- For fog transmittance `T`, input `(C, A)`, and integrated fog radiance `L`, the
  output is `Cout = T*C + L`, `Aout = 1 - T*(1-A)`. Rain's streak layer applies its
  own opacity afterward. Composite externally with
  `Cfinal = Cout + background*(1-Aout)` (blend One, OneMinusSrcAlpha).
- Empty input pixels inside the effect discard undefined hidden RGB. Zero fog and
  zero rain skip the effect and preserve the source.
- Use a floating-point RGBA target for HDR (for example ARGBHalf), keep alpha
  available through the rest of the pipeline, and configure URP post-processing
  alpha support if post-processing is enabled. Other passes may overwrite alpha.

This describes a medium applied over the available image/depth. Correct RGBA
algebra does not restore transparent surfaces' missing depth layers. Empty pixels
integrate to the profile's maximum distance or volume exit.

The GPU tests include empty, partial, and opaque inputs, actual transparent
geometry, HDR fog, black/white/colored external backgrounds, zero extinction,
opaque capture order, and post-processing order. See [validation](validation.md).
