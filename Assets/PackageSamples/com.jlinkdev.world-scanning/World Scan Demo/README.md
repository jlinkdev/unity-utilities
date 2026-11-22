# World Scan Demo

Open `Scenes/World Scan Demo.unity` and enter Play mode.

The camera orbits a purpose-built greybox scan facility. The demo emits automatically; use **Emit Pulse** to trigger another scan or **Next Profile** to switch between the spherical survey pulse and cylindrical sector pulse.

Watch for five layers working together:

1. The leading emissive surface band.
2. The fading interior trail.
3. The triplanar grid wrapping floors, walls, and props.
4. Depth and normal accents around silhouettes and structural breaks.
5. Material reveal nodes and receiver beacons reacting to the same runtime pulse.

The sample contains its own profiles, materials, volume profile, scripts, and scene. Its camera references the included Scan Feature and requests depth for the scan pass.

## Sample presentation

On import, choose **Install** to use the shared fonts, icons, and sample header. To install later, choose **Tools > jlinkdev > World Scanning > Install Sample Framework**. Before the release tag is published, use Package Manager's **Install package from disk** and select `Packages/com.jlinkdev.sample-framework/package.json` from a Unity Utilities checkout.
