# Selection Gallery

All three stands are instances of the framework’s Studio Plinth prefab, also used by Pottery Workshop.

Open **Scenes/Selection Gallery.unity** and enter Play Mode. The left spur gear shows a cyan hover outline, the center impeller shows an amber selection outline with a soft outer glow, and the right coupling starts idle.

The import dialog offers the shared Sample Framework header, caption skin, and studio grid. **Later** keeps the default captions. To install it afterward, choose **Tools > jlinkdev > Object Highlighting > Install Sample Framework**. For an unpublished release, use Package Manager to install the framework from disk from the Unity Utilities checkout.

Move the pointer over a part to focus it. Click a part to select it. Click it again or click empty space to clear selection. Hover can move independently while the selected part retains its glow. Each machined assembly uses one highlight component at its root.

The sample reads the mouse through the Input System when enabled and uses the legacy Input Manager in legacy-input projects. The UI Document draws the station names and live state badges. The fallback uses IMGUI. The runtime highlighting component accepts state changes from any interaction system.

The scene includes its meshes, materials, colliders, camera, and **Gallery Renderer** with the feature installed. The camera references the highlighting feature on Gallery Renderer. Its pass runs when that camera renders. To compare glow budgets, select **Gallery Renderer** and change **Glow Resolution**. The outline stays at the camera's full render resolution.
