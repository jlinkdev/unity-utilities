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
`Assets/PackageDevelopment/VolumetricRain/SampleAuthoring/Rain Laboratory`.
Its standalone UPM sample is in
`Packages/com.jlinkdev.volumetric-rain/Samples~/Rain Laboratory`.
The package's Tools menu can generate a fresh laboratory without replacing existing assets.
The development-only `RainBuildValidation.Build` method produces a Windows smoke
player under `Logs/VolumetricRain/Player` and restores pipeline settings afterward.

The new `Volume Laboratory` folder alongside Rain Laboratory demonstrates a bounded
rain box and a dry canopy. Both samples are distributed independently through UPM.
Use **Tools > jlinkdev > Volumetric Rain > Create Volume Laboratory** to generate
another copy without changing the original laboratory.

`VolumetricRain/Benchmark` contains the development-only GPU benchmark and original
reference shader. Run it in an isolated project: it opens an empty scene. See the
package's `Documentation~/performance.md` for measurements and reproduction.
`VolumetricRain/SampleAuthoring/Fog Laboratory` demonstrates Fog Only with the same
inclusion/exclusion boxes. The package's **Create Fog Laboratory** menu generates a
separate scene and assets, leaving existing rain laboratories intact.