# Volume Laboratory

Open Scenes/Volume Laboratory.unity alone and press Play. The camera starts inside a dry
canopy, looking at outdoor rain. Rain Camera Pass renders the effect through the sample camera.

Select "Dry Volume - canopy interior" and disable its Rain Exclusion Volume to
compare. Move Rain Camera forward through the open front to enter the rain.
Select "Rain Volume - move or resize this box" to change the outer boundary.
The profile uses Extent = Volumes Only. Boxes affect both streaks and rain haze.

Use the Game view to preview the effect through Rain Camera in Edit mode.
Full documentation: Packages/com.jlinkdev.volumetric-fog-and-rain/Documentation~/volumes.md.

## Sample presentation

On import, choose **Install** to use the shared fonts, icons, and sample header. To install later, choose **Tools > jlinkdev > Volumetric Fog and Rain > Install Sample Framework**. Before the release tag is published, use Package Manager's **Install package from disk** and select `Packages/com.jlinkdev.sample-framework/package.json` from a Unity Utilities checkout.
