# Plant Studio

Open **Plant Studio.unity** and press **Play**. The scene opens on a trailing pothos rooted at the soil surface of a fluted ceramic planter. Drag the specimen view to orbit and scroll to zoom. **Fit view** restores the starting angle and automatic framing.

- **Planting** switches species or picks a new seed immediately. Set the seed, crown shoots, spread, leaf size and starting growth, then choose **Generate plant** to apply those values.
- **Long pothos** generates a longer cascade. **PBR** toggles the textured, scene-lit material on the visible specimen.
- **Leaf mesh detail** switches between Simple, Standard and Close-up while preserving the specimen. Toggle **Leaf wireframe** above the viewport to inspect the foliage triangles and their count at the current resolution.
- **Growth** changes the direction and elevation of the simulated light, the strength of its influence, and moisture. **Step +1** advances one supplied growth unit. **Grow** runs at the selected speed until paused or mature. Try turning the light after a few leaves have opened, then advance several units.
- **Save specimen** keeps one plant and its conditions in this application's local preferences. **Load specimen** restores it and pauses the clock.
- **Save mesh with UVs…** writes a Unity mesh asset when playing in the editor. Assign it to a Mesh Filter and use **Stem PBR** and **Leaf PBR** for the two renderer slots.

Outside Play Mode, select **Specimen** in the Hierarchy to generate, step or bake directly in the Inspector. The scene includes its planter, shelf, orthographic camera and two lights. PBR works in Built-in and URP 17.
