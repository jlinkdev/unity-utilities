# Rendering and authoring

## Authoring a static volume

**Starlit Banks** and the **Starlit Passage** fly-through use the **Painterly Cavity** profile. It builds a corrugated shell, a crossing dust sheet and a smaller gas pocket. The authored composition uses form seed 91, detail seed 1049 and two manually placed stars. **Stellar Plumes** builds overlapping lobes with soft margins and darker interiors. Its dust scattering responds to both the local density gradient and the light that reaches each sample through intervening dust. Use the **Single Star Plumes** prefab to see it lit from one side.

Select a volume and assign its **Style**. To make a shared look, duplicate a shipped profile into Assets or choose **Create > jlinkdev > Procedural Nebulae > Style Profile**. Edit the asset to adjust its default seeds, field shape, coverage, fold frequency, wisps, extinction, gas brightness and colors. **Plume Scale** and **Edge Softness** apply to Plumes. **Clouds And Lanes** blends filamentary lanes and broader masses through **Cloud Body**. Existing volumes with an empty Style field use their serialized appearance and original form seed.

**Form seed** changes the large-scale composition. **Detail seed** changes the placement of fine erosion. Type a seed or use **Reroll** on the volume to set a local override. **Reset** returns that channel to the profile’s current default. **Variation Strength** continuously scales both overrides from the profile defaults at zero to the selected variation at one. These edits support Unity Undo. Rerolling a volume leaves the shared asset and other instances untouched.

The stars and clearing are scene transforms. By default the renderer uses each star’s authored position. Enable **Vary Star Positions** to generate deterministic offsets with an independent **Star seed** and a maximum **Star Offset Radius** in world units. The disk and lighting use the same offset. Disabling the option immediately restores the authored lighting, and the star transforms stay available for hand editing. The volume supports position and rotation. Its explicit **Width**, clearing radius, clearing softness and star radii are in world units. Transform scale is ignored. Density fades out at the box faces.

## Domain warps

Choose **Structure Warp** on Painterly Cavity, Tidal Veil, or Ember Arc. Each profile defaults to **None** for its authored composition. Vortex, Switchback, Overturning Billows, Carapace Plates, Branching Gills, and Pore Cups deform the full volume, carrying dust, excited gas, and their color regions together. Camera rays and stellar shadows sample the same deformed medium. The star transforms, box edges, and authored clearing stay in scene space.

**Structure Warp Strength** ranges from −2 to 2. Zero removes that warp and a negative value reverses its displacement. The warp follows the form seed by default. **Override Warp Seed** lets you vary the warp independently. For the four patterned warps, **Base Warp** optionally supplies an underlying Vortex or Switchback with its own strength.

Older profiles with a saved bank formation load the corresponding full-domain warp at strength 1. A saved Vortex or Switchback paired with that formation becomes the base warp at its original strength.

## Rendering

A 128³ RGBA16 float texture stores absorbing dust, emitting gas, and two coordinates describing the bends in the gas. Evaluating strand cross-sections in those coordinates preserves detail below the cache's voxel width. The camera march filters narrow strands according to its step length. A tileable 64³ cellular texture supplies fine erosion. A second 64³ texture caches integrated dust density toward the star, stopping at the star or box boundary. The first eligible camera prepares the volume on demand. Camera movement reuses all three textures. Shape changes rebuild the medium and stellar shadow. Changing a repeating pattern or its strength reuses the broad density bake while pattern sampling stays enabled and the underlying bend stays fixed. It refreshes the stellar shadows for the new surface. In cavity mode the two auxiliary channels store the cool gas fraction and a signed fold coordinate. An optional companion uses its own shadow cache. Moving either star rebuilds only its cache. The volume textures use 17 MiB with one star and 17.5 MiB with a companion, excluding driver overhead and camera targets.

Star color and intensity affect the disk and scattered light. Distance and shielding by dust determine stellar excitation. Excited gas emits along the camera ray even in regions with little dust. In cavity mode, Inner Gas colors the main cavity and Outer Gas colors the secondary gas pocket. In the other field shape the colors follow stellar excitation. Gas Brightness can be set to zero to isolate dust scattering. The disk clips the march at the star surface. Dust in front of the star also attenuates its small corona, while dust behind it leaves the star visible. Each enabled star contributes its own light. With both disabled, only the shadow fill contributes.

The full-resolution pass integrates along each camera ray. Half resolution integrates first and reconstructs against opaque depth at full resolution. Small, high-contrast features can soften during reconstruction. Raise Ray Steps if diagonal views show banding. The effect composites before transparents and post-processing.

The Scene view uses the URP asset’s default renderer. If the Game camera uses a renderer override, add the Procedural Nebulae feature to the default renderer as well and enable **Scene View Only** on that copy to preview the volume while editing.

For a dark frame, check the camera renderer, Render Graph, camera culling mask, star assignment and distance to the cloud. Clearings can extend beyond the camera or carve away the entire view. Use Scene view and select the volume to see its bounds.
