# Package development tools

See the [editable sample workflow](../PackageSamples/README.md) for authoring and publishing.

This folder contains the host-only builders, renderer configurations, benchmarks,
and validation helpers. Rebuild commands regenerate example content. Use the
publish command directly to distribute hand edits.

## Object Highlighting sample workflow

Choose **Tools > jlinkdev > Object Highlighting > Open Selection Gallery** or open the
[editable scene](<../PackageSamples/com.jlinkdev.object-highlighting/Selection Gallery/Scenes/Selection Gallery.unity>) and press **Play**.
The gear shows the hover outline and the impeller shows the selection glow. Move the mouse and click to change their states.
Tune the authoring copy's **Gallery Renderer** to compare glow resolutions.

The Package Manager copy lives in `Packages/com.jlinkdev.object-highlighting/Samples~/Selection Gallery`.

## Procedural Nebulae sample workflow

Open [Starlit Passage](<../PackageSamples/com.jlinkdev.procedural-nebulae/Starlit Passage/Scenes/Starlit Passage.unity>) in the host project and press **Play**. Its Flight Camera references the sample’s Nebula Feature.

The distributable copy lives in `Packages/com.jlinkdev.procedural-nebulae/Samples~/Starlit Passage`. Both scenes use the package’s Starlit Banks prefab.

## Procedural Plants sample workflow

Open the [Plant Studio scene](<../PackageSamples/com.jlinkdev.procedural-plants/Plant Studio/Scenes/Plant Studio.unity>) and press **Play**. Select **Specimen** to generate and bake in edit mode.

## Procedural Picture Frames sample workflow

Open the [Frame Workshop scene](<../PackageSamples/com.jlinkdev.procedural-frames/Frame Workshop/Scenes/Frame Workshop.unity>) and press **Play**.

## Procedural Pots sample workflow

Open the [Pottery Workshop scene](<../PackageSamples/com.jlinkdev.procedural-pots/Pottery Workshop/Scenes/Pottery Workshop.unity>) and press **Play**. Edit this copy when developing the sample.

## Portal sample workflow

The editable Portal Playground source lives at
`Assets/PackageSamples/com.jlinkdev.portals/Portal Playground`. Use
**Tools > jlinkdev > Portals > Rebuild Development Content** to regenerate it.
The builder regenerates the scene and prefab and publishes the sample to
`Packages/com.jlinkdev.portals/Samples~/Portal Playground` for UPM import.

## Volumetric Rain sample workflow

The saved Rain Laboratory source lives at
`Assets/PackageSamples/com.jlinkdev.volumetric-fog-and-rain/Rain Laboratory`.
Its standalone UPM sample is in
`Packages/com.jlinkdev.volumetric-fog-and-rain/Samples~/Rain Laboratory`.
The package's Tools menu can generate a fresh laboratory without replacing existing assets.
The development-only `RainBuildValidation.Build` method produces a Windows smoke
player under `Logs/VolumetricRain/Player` using the host project pipeline.

Rain Laboratory's shelter station shows how a rain exclusion keeps the viewer dry
while rain remains visible through a fixed pane and an open bay. For a minimal
scene with just the volume mechanics, use **Tools > jlinkdev > Volumetric Fog and Rain > Create Volume Laboratory**.

`VolumetricFogAndRain/Benchmark` contains the development-only GPU benchmark and original
reference shader. Run it in an isolated project: it opens an empty scene. See the
package's `Documentation~/performance.md` for measurements and reproduction.
`../PackageSamples/com.jlinkdev.volumetric-fog-and-rain/Fog Laboratory` demonstrates Fog Only with the same
inclusion/exclusion boxes. The package's **Create Fog Laboratory** menu generates a
separate scene and assets, leaving existing rain laboratories intact.

## Water System sample workflow

The editable Water Laboratory source lives at
`Assets/PackageSamples/com.jlinkdev.water-system/Water Laboratory`.
Use **Tools > jlinkdev > Water System > Rebuild Water Laboratory (Development)**
to regenerate the pool scene, submerged geometry, floating props, profiles, and
scoped URP pipeline. The builder is
`Assets/PackageDevelopment/Editor/WaterSystemDevelopmentBuilder.cs`; its batch
entry point is
`jlinkdev.UnityUtilities.WaterSystem.Development.WaterSystemDevelopmentBuilder.BuildAll`.

The builder opens a new scene and replaces generated assets in the authoring
folder. Run it in an isolated validation project if another workflow owns the
open editor. It does not change project Graphics, Quality, color space, or Render
Graph settings. It finds the installed Water System package and publishes a
complete sample to `Samples~/Water Laboratory` inside that package. The imported
sample has no dependency on this host development folder.

The laboratory camera runs the Water Camera Pass. Read the [package guide](../../Packages/com.jlinkdev.water-system/README.md)
for scope and [validation record](../../Packages/com.jlinkdev.water-system/Documentation~/validation.md)
for observed results. The local 0.1.0 implementation passed automated tests,
editor render checks, a Windows player smoke run, and candidate archive sample
import. It is not yet published; performance and broader compatibility remain
unmeasured.

For a clean consumer check, copy only `Editor/WaterPackageImportValidation.cs`
into the consumer project's `Assets/Editor` after installing the package archive.
Run `WaterPackageImportValidation.ImportSample` and then, in a separate editor
invocation after the imported scripts compile, `VerifyImportedSample`. Both
methods use the `jlinkdev.UnityUtilities.WaterSystem.Development` namespace.
The helper imports through Package Manager's sample API and writes
`WaterImportedSamplePath.txt` and `WaterPackageImportValidation.txt` at the
consumer project root. It checks references and sample independence without
depending on authoring content or the sample controller's type at compile time.

## Mist Sanctuary sample workflow

`../PackageSamples/com.jlinkdev.volumetric-fog-and-rain/Mist Sanctuary` contains the editable interactive mist sample.
`Editor/InteractiveFogDevelopmentBuilder.cs` builds its stone hall, dedicated URP
pipeline, and demonstration. Use **Tools > jlinkdev > Volumetric Fog and Rain > Interactive Fog > Rebuild
Mist Sanctuary (Development)**, or the `BuildAll` batch entry point in
`jlinkdev.UnityUtilities.InteractiveFog.Development.InteractiveFogDevelopmentBuilder`.
The builder replaces its generated scene; run it in an isolated project.

`BuildPlayer` is intended only for a disposable validation project: it regenerates the scene and builds a Windows sample with the host build settings. `Publish` copies the authoring sample into the installed local
package's `Samples~` folder. `InteractiveFogValidation.Capture` provides editor
diagnostic images; final quality checks use the running player's offscreen capture
sequence. See the [package validation record](../../Packages/com.jlinkdev.volumetric-fog-and-rain/Documentation~/InteractiveFog/Validation.md).

The sample follows a closed animation spline and has no input controller. Use
`--fog-full-rate` for its 60 fps capture and `--fog-coarse` for the matched
curl/wisp control; omit sequence flags when benchmarking.

Interactive Fog recovery review: `InteractiveFogDevelopmentBuilder.BuildPreviewPlayer` builds the isolated project's generated sample without publishing it over the host or package authoring assets. Use `--fog-recovery` to stop the animated explorer after ten seconds and capture a 40-second sequence; add `--fog-no-recovery` for a matched control. The regular `BuildPlayer` still publishes its generated sample.
