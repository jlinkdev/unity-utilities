# Setup and troubleshooting

## Scene setup

Open **Tools > jlinkdev > Interactive Fog > Setup**. The window initially selects the active URP pipeline and the selected scene camera (or Main Camera). Choosing a camera or pipeline selects the renderer that camera uses, including overrides and fallback to the pipeline default. Confirm the renderer before clicking **Configure Selected Renderer**. Configuration reuses an existing Fog Renderer Feature or adds one, enables it and assigns the package shader. Repeating the action does not add another fog feature. Other features are retained; the action records Undo. All cameras using that renderer share the configured feature.

**Dedicated camera renderer:** select the active pipeline and a scene Camera first, then a separate renderer asset (such as the sample's Mist Renderer). Click **Register Renderer and Assign Camera**. This appends the renderer only if needed and sets the camera override; it preserves other list entries, the default renderer, and Graphics/Quality pipeline references. Registration and camera assignment can be undone together. Save the scene. Configure Selected Renderer is separate: it is needed for a new renderer without the fog feature, but the sample renderer is already configured.

Camera overrides are renderer-list indices, not asset references. If different quality levels select different pipelines, ensure the intended renderer occupies that index in each pipeline, or update the override when switching. The setup window warns when the selected pipeline is inactive or the chosen renderer differs from the camera's current renderer; it never activates a pipeline. **Use Camera's Current Renderer** restores the camera's existing selection without modifying assets. Overlay cameras cannot be assigned through this tool; choose the stack's base camera.

The depth-texture action changes only the selected pipeline asset. Ensure Render Graph is enabled in Graphics settings; the tool does not switch project render modes. The feature runs before transparents. Overlay cameras, XR and compatibility-mode rendering are unsupported.

Create a volume for **Ground Mist**, **Cumulus** or **Dry Ice**. The same commands are available under **GameObject > Effects > Interactive Fog**. Each assigns the compute shader and corresponding shared preset. Position is the floor center; Size controls world-aligned bounds. Rotation and scale do not alter the bounds. Assign a directional Sun and up to eight point lights. Fit Fog Obstacle boxes to solid architecture; add Fog Interactor at the feet of moving characters.

## Tuning the correct profile

The assigned **Style Profile** owns appearance, motion and recovery. Click **Select Style Asset** on the volume to edit that exact asset. Its Inspector reports how many loaded volumes use it. A profile opened separately in the Project window may not be assigned to the effect being viewed.

Shared asset edits persist after Play Mode. **Save Current Look as Style Profile...** on a volume creates and assigns an independent copy of its effective settings, including an existing profile's tuning. **Duplicate Profile...** on a profile creates a copy without assigning it. Both use a unique asset path rather than replacing another profile. Clearing Style Profile reveals the volume's preserved inline settings.

Quality and lighting remain on the volume. Backend, grid and bounds changes restart the field; changing render resolution does not. Rest Recovery restores quiet density after wakes. Reset Simulation clears the current field for a fresh comparison. Dry Ice starts with staggered billow phases rather than an empty room.

## Symptoms and checks

| Symptom | Check |
| --- | --- |
| Style edits do nothing | Use Select Style Asset on the volume. Check the profile usage message. Normal sample playback uses the assigned profile; Reference Style is capture-only. |
| No fog | Assign the compute shader, check enabled volume/feature and camera bounds, and configure the renderer actually used by the camera. Confirm Render Graph, depth and supported GPU formats. |
| Dry Ice bubbles are too sparse | Raise Billows Coverage, shorten Quiet intervals, or reduce Spacing. Effective spacing cannot fall below 1.7 times maximum radius. |
| Bubble size changes are hard to see | Check the active profile, compare radius with Cell Size, and ensure the low mist does not bury small crowns. Reset for a clean comparison. |
| Mist keeps appearing with invisible bubbles | Billow Density controls bodies; Mist Amount separately controls release. Coverage zero disables new procedural bodies and release. Existing fluid remains. |
| Clouds have empty gaps despite high Density | Cumulus Cloud Coverage fills gaps; Density thickens existing clouds. Dry Ice instead uses Billows distribution controls. |
| Shape clips at the top | Increase volume Size Y or reduce Fog Height/billow radius. Bounds clip the entire effect. |
| Fog does not recover | Check Rest Recovery and continuing interaction/gust activity. Active wakes suppress recovery. |
| Drag zero still shows motion | Existing flow, wind, other interactors and Dry Ice stirring continue. Clearing is a separate density-removal control. |
| Lower rendering resolution barely changes performance | Simulation may dominate. Compare backend, grid, simulation rate and pressure iterations as well as render resolution/ray steps. Use an uncapped development player and intended camera. |

See [performance and styles](Performance-and-Styles.md) for units, costs and backend limitations, and [validation](Validation.md) for tested configurations. The sample guide documents fixed-clock captures; ordinary playback does not change frame-rate or VSync settings.
