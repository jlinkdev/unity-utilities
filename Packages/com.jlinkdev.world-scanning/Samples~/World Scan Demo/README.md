# World Scan Demo

Open `Scenes/World Scan Demo.unity` and enter Play mode.

The camera orbits a purpose-built greybox scan facility. The demo emits automatically; use **Emit Pulse** to trigger another scan or **Next Profile** to switch between the spherical survey pulse and cylindrical sector pulse.

Watch for five layers working together:

1. The leading emissive surface band.
2. The fading interior trail.
3. The triplanar grid wrapping floors, walls, and props.
4. Depth and normal accents around silhouettes and structural breaks.
5. Material reveal nodes and receiver beacons reacting to the same runtime pulse.

The sample contains its own profiles, materials, volume profile, scripts, and scene. Its camera expects a URP renderer configured with `ScanRendererFeature`; importing the sample does not replace the consuming project's render pipeline settings.
