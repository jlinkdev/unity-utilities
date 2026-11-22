# Sample Framework

Shared presentation assets for the Unity Utilities samples.

## Requirements

Unity 2022.3 or newer with UI Toolkit and IMGUI. The lit grid uses URP.

## Installation

In the Unity Utilities checkout this package is embedded. Use **Package Manager > Install package from disk** and select this package's `package.json` in another project. The planned Git release uses the repository subfolder and dedicated tag below.

```text
https://github.com/jlinkdev/unity-utilities.git?path=/Packages/com.jlinkdev.sample-framework#sample-framework-v0.1.0
```

## Quick start

Add a UI Document to the scene and assign **Runtime/Presentation/UI/Sample Panel.asset**. In its UXML, use the **sample-identity** container with **sample-symbol**, **sample-package**, and **sample-name** labels. Keep the identity at the shared upper-left inset in every scene. Place controls below the header or elsewhere with enough clearance at different Game view sizes. Set picking mode to Ignore on the identity and its children so input reaches the demo. Caption Preview includes an editable example.

The **Sample Panel** scales against a 1024 × 576 reference. Use it for every package and sample heading. The **Studio Panel** uses the same scale and draws workshop controls below the heading. The shared heading has a 21-point package name, 13-point sample name and 40-point icon slot. The shared gradient fades past the text on every background. Inside **sample-identity**, group the icon and labels in a **sample-identity-content** row. Its first child is an empty, non-picking **sample-identity-gradient** element, so the icon and text draw above it.

**Parameter Slider.uxml** contains a UI Toolkit slider with an editable numeric readout, unit, filled track and thumb. Instantiate the template, query the standard Unity `Slider`, and set its label, range and value. A label such as `Height (mm)` puts the unit beside the numeric field. Apply **Sample Controls.uss** on a parent with the **sample-studio** class. **IMGUI Slider.asset** provides the matching IMGUI control. Frame Workshop and Pottery Workshop use this control for their editable dimensions.

**Workshop Shelf.uxml** provides the illustrated preset row and previous/next buttons used by the pottery, frame and plant workshops. Clone the template into a **sample-studio** parent styled with **Sample Controls.uss**. Add a Unity **Button** for each preset to **preset-cards**, give it the **preset** class, and mark the current selection with **active-preset**. Each sample supplies its own preview images and selection action.

**Studio Plinth** is the same prefab used under the selection parts and the pottery vessel. **Architectural Grid** uses half-metre world-space tiles on walls, floors and curved surfaces. Drag **Studio Grid** into a URP scene for the larger ground plane. Assign **Studio Sky** to a Skybox component on its camera, and set camera clear flags to Skybox. The studio samples enable exponential fog in their own scene lighting settings and use the same haze color as the sky (sRGB 0.186, 0.212, 0.253). An isolated ground prefab also fades with viewing distance when scene fog is disabled. Orthographic and downward-facing cameras can show ground across the whole frame.

**Amber Sconce** is a wall-mounted bronze capsule with an emissive diffuser and a warm point light. Place the prefab origin against the mounting surface, with local +Y up and +Z facing into the room. The samples use the same fixture on brackets, courtyard posts, and low bollards. In Interactive Fog, assign the prefab's child Light to the fog volume's Local Lights to illuminate the mist.

The **Courtyard Stone** folder contains the neutral albedo, surface normal and roughness used in both weather laboratories. They share a physical texture scale so dry and wet stone can sit next to each other. [Stone scan credits](<Runtime/Presentation/Environment/Courtyard Stone/Source.md>)

## Sample

Import **Caption Preview**, open `Scenes/Caption Preview.unity`, and press Play. [Preview guide](<Samples~/Caption Preview/README.md>)

## Limitations

The identity panel uses the optional framework. A utility can compile and run its own controls before the framework is installed. Installation from the public Git URL requires the release tag.

## Documentation

Keep each asset and its `.meta` file together when moving or releasing it. Utility samples serialize its GUID and local file ID. [Changelog](CHANGELOG.md)

## License

Framework code and original artwork use the [MIT license](LICENSE.md). Barlow uses the [SIL Open Font License](Runtime/Presentation/Fonts/OFL.txt). Material Icons Outlined uses the [Apache License 2.0](Runtime/Presentation/Fonts/Material-Icons-LICENSE.txt).
