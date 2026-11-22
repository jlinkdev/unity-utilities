# Planetary Gearbox

Open **Scenes/Planetary Gearbox.unity** and press Play. Drag the drawing to orbit the assembly. Scrub **Explode** to lift the gears, washers, ring and carrier along the assembly axis. The lower case stays fixed. Switch **Layers** from 1 to 4 while assembled to see features behind the front surface.

The Blueprint Camera Pass draws the assembly through the sample camera. Use a Unity 6 URP project with Render Graph enabled.

## Model

Innerscene's open 4:1 planetary gearbox contains ten separate solids. The three carrier pins are part of the carrier. The FBX in this sample was tessellated from the original STEP assembly, converted from millimetres to metres and given per-part pivots. The model is dedicated to the public domain under CC0 1.0. [Model and source download](https://www.innerscene.com/tools/library/3d-parts/three-planet-4-1-gearbox-open-assembly-2dfb48c4) · [CC0 dedication](https://creativecommons.org/publicdomain/zero/1.0/)

To use the effect on another assembly, add the renderer feature to your Universal Renderer and add Blueprint Camera to the camera viewing the assembly. The explode offsets in this scene belong to the sample controller.

## Sample presentation

On import, choose **Install** to use the shared fonts, icons, and sample header. To install later, choose **Tools > jlinkdev > Blueprint Rendering > Install Sample Framework**. Before the release tag is published, use Package Manager's **Install package from disk** and select `Packages/com.jlinkdev.sample-framework/package.json` from a Unity Utilities checkout.
