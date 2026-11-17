# Package Development Assets

Use this folder for integration scenes, fixtures, and other development assets
that exercise multiple packages but should not ship in any UPM package.

Package-specific samples belong in that package's `Samples~` directory, and
package-specific automated tests belong in that package's `Tests` directory.

## Portal sample workflow

The editable Portal Playground source lives at
`Assets/PackageDevelopment/Portals/SampleAuthoring/Portal Playground`. Use
**Tools > jlinkdev > Portals > Rebuild Development Content** after editing it.
The builder regenerates the scene and prefab, configures this host project for
URP, and publishes an identical copy to
`Packages/com.jlinkdev.portals/Samples~/Portal Playground` for UPM import.

## Volumetric Rain sample workflow

The saved Rain Laboratory source lives at
`Assets/PackageDevelopment/VolumetricFogAndRain/SampleAuthoring/Rain Laboratory`.
Its standalone UPM sample is in
`Packages/com.jlinkdev.volumetric-fog-and-rain/Samples~/Rain Laboratory`.
The package's Tools menu can generate a fresh laboratory without replacing existing assets.
The development-only `RainBuildValidation.Build` method produces a Windows smoke
player under `Logs/VolumetricRain/Player` and restores pipeline settings afterward.

The new `Volume Laboratory` folder alongside Rain Laboratory demonstrates a bounded
rain box and a dry canopy. Both samples are distributed independently through UPM.
Use **Tools > jlinkdev > Volumetric Fog and Rain > Create Volume Laboratory** to generate
another copy without changing the original laboratory.

`VolumetricFogAndRain/Benchmark` contains the development-only GPU benchmark and original
reference shader. Run it in an isolated project: it opens an empty scene. See the
package's `Documentation~/performance.md` for measurements and reproduction.
`VolumetricFogAndRain/SampleAuthoring/Fog Laboratory` demonstrates Fog Only with the same
inclusion/exclusion boxes. The package's **Create Fog Laboratory** menu generates a
separate scene and assets, leaving existing rain laboratories intact.

## Interactive Fog sample workflow

`InteractiveFog/SampleAuthoring/Mist Sanctuary` contains the editable fog sample.
`Editor/InteractiveFogDevelopmentBuilder.cs` builds its stone hall, dedicated URP
pipeline, and demonstration. Use **Tools > jlinkdev > Interactive Fog > Rebuild
Mist Sanctuary (Development)**, or the `BuildAll` batch entry point in
`jlinkdev.UnityUtilities.InteractiveFog.Development.InteractiveFogDevelopmentBuilder`.
The builder replaces its generated scene; run it in an isolated project.

`BuildPlayer` is intended only for a disposable validation project: it selects
Linear color space, windowed player dimensions, and frame timing, then builds a
Windows sample. `Publish` copies the authoring sample into the installed local
package's `Samples~` folder. `InteractiveFogValidation.Capture` provides editor
diagnostic images; final quality checks use the running player's offscreen capture
sequence. See the [package validation record](../../Packages/com.jlinkdev.interactive-fog/Documentation~/Validation.md).

The sample follows a closed animation spline and has no input controller. Use
`--fog-full-rate` for its 60 fps capture and `--fog-coarse` for the matched
curl/wisp control; omit sequence flags when benchmarking.

Interactive Fog recovery review: `InteractiveFogDevelopmentBuilder.BuildPreviewPlayer` builds the isolated project's generated sample without publishing it over the host or package authoring assets. Use `--fog-recovery` to stop the animated explorer after ten seconds and capture a 40-second sequence; add `--fog-no-recovery` for a matched control. The regular `BuildPlayer` still publishes its generated sample.
