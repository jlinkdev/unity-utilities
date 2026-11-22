# Rain Laboratory

Open **Scenes/Rain Laboratory.unity** and press **Play**. Use the two buttons in the lower left or press **1** and **2** to switch stations.

- **Courtyard** looks across the wind-driven downpour. Lamps reflect on wet paving, water runs down the ochre screen, and beads catch the sky on the entry parapet.
- **Shelter** looks out onto the same storm from a dry observation bay. Compare the scene through the wet glass and through the open bay beside it.

Move or rotate **Courtyard view** and **Shelter view** in Edit Mode to change the shots. Set their fields of view on the **Rain Station Selector** component of **Rain Camera**. Both stations use this camera and its rain profile.

Select **Rain Camera** and lower **Intensity** to zero to stop the rain. The local impact emitters stop releasing drops and the paving gradually dries. Raise Intensity to wet the court again. The camera's **Rain Camera Pass** supplies the volumetric effect on that camera. **Rain Profile** controls wind, strand size, visibility in the distance, and height falloff of the haze.

The **Storm courtyard** hierarchy contains the editable wall bays, lamp piers, ceramic screen and parapet. Its **Wet Rain Surface** component drives the exposed renderers. The materials have individual water film, bead, and runoff settings. Under **Rain contact zones**, nearby collision emitters add rings and airborne droplets where they strike the stone. The scene's own reflection probe captures the courtyard for the wet surfaces.

In **Observation shelter**, select **Rain-facing glass** to inspect its wet material. The rain exclusion keeps the viewer dry right up to the pane. The **Window downlight** lights the interior floor, and the splash emitter just outside the threshold shows where the exposed paving begins.

## Sample presentation

On import, choose **Install** to resolve the shared lamps, stone texture maps, and sample header. To install later, choose **Tools > jlinkdev > Volumetric Fog and Rain > Install Sample Framework**. Before the release tag is published, use Package Manager's **Install package from disk** and select `Packages/com.jlinkdev.sample-framework/package.json` from a Unity Utilities checkout.
