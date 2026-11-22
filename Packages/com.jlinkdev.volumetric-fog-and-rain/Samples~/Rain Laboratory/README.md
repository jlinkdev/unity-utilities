# Rain Laboratory

Open the included **Scenes/Rain Laboratory.unity** scene, or generate a fresh copy with
**Tools > jlinkdev > Volumetric Fog and Rain > Create Rain Laboratory** after installing the package. The editor generator creates an independent scene, rain profile, URP
renderer and pipeline, and greybox occluders under a unique `Assets/VolumetricFogAndRain`
folder. It is included with the package and requires no other utility or input system.

Press **Play**. The Rain Camera Pass on Rain Camera references the feature in Rain Renderer. Select Rain Camera to freeze time, change the debug view, or adjust intensity. Select its
profile to change field appearance and budgets. Preview the effect in the Game view.

See the package evaluation guide for the visual checks and GPU benchmark matrix.

## Sample presentation

On import, choose **Install** to use the shared fonts, icons, and sample header. To install later, choose **Tools > jlinkdev > Volumetric Fog and Rain > Install Sample Framework**. Before the release tag is published, use Package Manager's **Install package from disk** and select `Packages/com.jlinkdev.sample-framework/package.json` from a Unity Utilities checkout.
