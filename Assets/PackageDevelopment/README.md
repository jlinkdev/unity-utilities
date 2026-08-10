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
