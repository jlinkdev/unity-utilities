# Volume Laboratory

Open Volume Laboratory.unity alone and press Play. The camera starts inside a dry
canopy, looking at outdoor rain. Rain Demo Pipeline temporarily activates the
included URP pipeline and restores your previous settings when Play mode stops.

Select "Dry Volume - canopy interior" and disable its Rain Exclusion Volume to
compare. Move Rain Camera forward through the open front to enter the rain.
Select "Rain Volume - move or resize this box" to change the outer boundary.
The profile uses Extent = Volumes Only. Boxes affect both streaks and rain haze.

Edit-mode Scene view preview requires the demo pipeline to be assigned, or its
rain feature and Scene View Profile to be added to your current renderer.
Full documentation: Packages/com.jlinkdev.volumetric-fog-and-rain/Documentation~/volumes.md.