# Runtime settings and profile blending

`RainProfile` is an authoring asset. `RainSettings` is a plain runtime object with
the same controls. A camera uses its profile until `RuntimeSettings` is assigned;
setting that property back to null resumes the profile. One camera should own each
mutable settings object. Render Graph records a snapshot for each camera/pass.

```csharp
using UnityEngine;
using jlinkdev.UnityUtilities.VolumetricFogAndRain;

public sealed class FogTransition : MonoBehaviour
{
    [SerializeField] private VolumetricRainCamera fogCamera;
    [SerializeField] private RainProfile from;
    [SerializeField] private RainProfile to;
    [SerializeField, Min(0.01f)] private float duration = 3;
    private RainSettings settings;
    private float elapsed;

    private void OnEnable()
    {
        elapsed = 0;
        settings = new RainSettings(from);
        fogCamera.RuntimeSettings = settings;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.SmoothStep(0, 1, elapsed / Mathf.Max(.01f, duration));
        settings.BlendAppearance(from, to, t, BlendDiscreteSettings.AtEnd);
    }

    private void OnDisable()
    {
        if (fogCamera != null && fogCamera.RuntimeSettings == settings)
            fogCamera.RuntimeSettings = null;
    }
}
```

Assign all three references before Play. The API takes a blend factor, so the
calling project owns duration, easing, pause behavior, and networking. No profile
asset is modified. `CreateRuntimeSettings()` on the camera copies its current
profile as a convenience. `CopyFrom(profile)` and `CopyFrom(settings)` reuse an
existing instance. Call `Sanitize()` after direct programmatic edits if you want
explicitly normalized ranges; rendering also clamps the values it uses.

## What blends

Linear interpolation applies to rain/fog density, colors (including HDR values),
brightness, streak opacity, fog extinction/scattering, noise strength, fog distance
controls, base height, height falloff, directional strength/anisotropy, and fog
noise velocity. Author colors in the project's intended working color space; the
API interpolates stored color components and does not apply a second gamma conversion.

Render mode, extent, independent appearance/distance switches, seed, noise scale,
cell/streak geometry, rain motion, rain distances, and sample budgets come from
one endpoint. `AtEnd` (default) keeps the source until `t == 1`; `AtStart` selects
the target immediately. This is explicit switching, not an automatic crossfade
between shader modes. For a smooth weather-to-fog transition, use Rain And Fog and
independent fog on both endpoints, then fade their rain/fog densities. Do not expect
changing seeds or grid geometry to preserve the identity of individual streaks.

The method clamps t to [0,1], overwrites settings from the chosen endpoints, and
allocates no new settings instance per call. Settings changed after a blend call
can be used as additional project-specific overrides for that frame.

## Noise motion

Fog noise velocity is world-space metres per second. Each camera integrates motion
using its evaluation clock; changing velocity does not retroactively reposition
the noise. Zero velocity stops at the current offset. Freeze Time and the scaled/
unscaled clock settings apply to fog and rain. `ResetFogMotion()` resets the motion
history; the next evaluation initializes displacement from that time and velocity.
Changing noise scale/seed or seeking time is an authoring/replay operation, not a
continuous transition. Scene view has its own shared preview motion history.
