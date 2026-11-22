# Fog Laboratory

Open `Scenes/Fog Laboratory.unity` and press Play. The camera looks diagonally across a straight row of seven identical stone slabs, spaced eight metres apart. Each gap gives another view of how distance reduces contrast. The three-sided shelter opens onto the paved courtyard. A four-metre entrance at its south end interrupts the raised stone sill. The camera, shelter, slabs, paving, light and weather bounds are editable in the scene.

## Inspect the fog layers

- **Atmospheric fog** comes from Volumetric Fog and Rain. Select `Profiles/Rain Profile` to adjust extinction, height falloff, drifting noise and directional scattering. Set **Intensity** to zero on the Volumetric Rain Camera component of **Rain Camera** to see the same shot without atmospheric fog. The camera's Rain Camera Pass uses the sample renderer feature.
- **Ground mist** comes from the **Fog Volume** on **Courtyard Ground Mist**. Toggle that object to compare the low moving banks around the slab bases. Select the volume to inspect its bounds, simulation resolution and **Dawn Ground Haze** style. Each slab, the low block, the two perimeter walls, the sill and its entry landing carry a **Fog Obstacle** component. Duplicate the style and assign the copy here to experiment. **Ground Patchiness** controls the gaps between banks. Set it to zero for a continuous layer.
- **Under the canopy**, the weather exclusion keeps the viewpoint clear. Disable **Fog Exclusion - canopy interior** or move **Rain Camera** across the sill to inspect the transition. Resize **Fog Volume - move or resize this box** to change the extent of the courtyard haze. **Distant fog bank** softens the far skyline.

**Share Atmosphere With Ground Fog** is enabled on the **Volumetric Rain** feature in `Settings/Rain Renderer`. Look from the first slab to the last. The nearby mist keeps its curls, while the more distant banks pick up the haze color and lose contrast. Toggle the setting to compare both renderings with the same fog density.

The **Fog Camera Pass** on **Rain Camera** renders the simulated mist through the sample camera. The volume, its obstacles and both camera passes are editable in the scene.

## Materials and lighting

The **Dawn Sky** material controls the morning glow and the cloud veil. **Main Light** sets the direction of grazing light across the stone, and **Dawn bounce at shelter opening** lights the threshold. **Courtyard Basalt**, **Shelter Slate**, **Shelter Soffit** and **Threshold Sill** share albedo, roughness and normal maps with world-space tile joints. The Sample Framework supplies the shared **Courtyard Stone** texture set and its source credits under `Runtime/Presentation/Environment/Courtyard Stone`.

## Sample header

On import, choose **Install** to use the shared fonts, icons and sample header. To install later, choose **Tools > jlinkdev > Volumetric Fog and Rain > Install Sample Framework**. For a local framework installation, use **Package Manager > Install package from disk** and select `Packages/com.jlinkdev.sample-framework/package.json` from a Unity Utilities checkout.
