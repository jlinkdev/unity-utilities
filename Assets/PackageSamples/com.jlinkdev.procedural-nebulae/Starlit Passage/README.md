# Starlit Passage

Each scene camera has a **Nebula Camera Pass** pointing to the included Nebula Feature. Preview its output in the Game view using URP with Render Graph.

Open **Scenes/Ember Arc.unity** and select a 3:2 Game view. Press Play to slide past the warm curved bank and see the cooler fold behind it. Select **Ember Arc** in the hierarchy to change seeds or move its two stars. Reroll **Form seed** to change the sweep, shoulder, hook, and canopy independently. **Reset** restores the profile default of 91.

Open **Scenes/Shoulder Surface Detail.unity** for the fine-detail variation. Press Play to compare it with the same sideways move in **Shoulders and Pockets**.

Open **Scenes/Shoulders and Pockets.unity** for the grayscale shape-texture study. Press Play for the sideways camera move. The scene has one off-axis star and zero gas emission. To compare the broad bank on its own, set **Shape Displacement** to zero on a copy of the study profile. The same density field controls camera opacity and stellar shadows.

Open **Scenes/Tidal Veil.unity** and use a square Game view. The **Sculpted dust bank** volume uses the Tidal Veil profile to color the shoulders and add a lower foreground shelf. Press Play for the sideways camera move.

For the grayscale lighting study, open **Scenes/Backlit Bank Study.unity** and use a square Game view. The camera starts inside the volume facing the dark bank. Select the star under the **Backlit Bank** prefab and move it left to expose it through the opening.

Open **Scenes/Starlit Passage.unity**. Press Play to start a 64-second looping flight from within the clearing.

Pause or disable **Nebula Flight** to hold a viewpoint. Move **Star** behind a dust lane to see it dim, or move **Violet Companion** to shift the smaller pool of light. Move **Clearing** to open a different route.

Select **Starlit Banks** to explore variations. The default **Painterly Cavity** profile uses form seed 91 and detail seed 1049. Reroll either seed on the volume and use **Reset** to get the profile default back. Leave **Vary Star Positions** off to preserve the two authored lights. Switch **Style** to **Stellar Plumes** to try the same flight through denser lobes. The package also includes **Single Star Plumes**, a prefab with one off-axis star.

## Sample presentation

On import, choose **Install** to use the shared fonts, icons, and sample header. To install later, choose **Tools > jlinkdev > Procedural Nebulae > Install Sample Framework**. Before the release tag is published, use Package Manager's **Install package from disk** and select `Packages/com.jlinkdev.sample-framework/package.json` from a Unity Utilities checkout.
