# Planetary Gearbox

Open **Planetary Gearbox.unity** and press Play. Drag the drawing to orbit the assembly. Scrub **Explode** to lift the gears, washers, ring and carrier along the assembly axis. The lower case stays fixed. Switch **Layers** from 1 to 4 while assembled to see features behind the front surface.

The scene uses its own Universal Renderer and URP pipeline during play. The demo saves and restores the previous Graphics and current Quality pipeline assignments when enabled and disabled. Enable URP Render Graph in the project. For a player build, include a URP asset in Graphics or the active Quality level so Unity includes URP shader variants.

## Model

Innerscene's open 4:1 planetary gearbox contains ten separate solids. The three carrier pins are part of the carrier. The FBX in this sample was tessellated from the original STEP assembly, converted from millimetres to metres and given per-part pivots. The model is dedicated to the public domain under CC0 1.0. [Model and source download](https://www.innerscene.com/tools/library/3d-parts/three-planet-4-1-gearbox-open-assembly-2dfb48c4) · [CC0 dedication](https://creativecommons.org/publicdomain/zero/1.0/)

To use the effect on another assembly, add the renderer feature to your Universal Renderer and add Blueprint Camera to the camera viewing the assembly. The explode offsets in this scene belong to the sample controller.
