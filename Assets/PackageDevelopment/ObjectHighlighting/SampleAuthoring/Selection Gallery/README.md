# Selection Gallery

Open **Selection Gallery.unity** and enter Play Mode. The left spur gear shows a cyan hover outline, the center impeller shows an amber selection outline with a soft outer glow, and the right coupling starts idle.

Move the pointer over a part to focus it. Click a part to select it. Click it again or click empty space to clear selection. Hover can move independently while the selected part retains its glow. Each machined assembly uses one highlight component at its root.

The sample reads the mouse through the Input System when enabled and uses the legacy Input Manager in legacy-input projects. IMGUI draws the captions. The runtime highlighting component accepts state changes from any interaction system.

The scene includes its meshes, materials, colliders, camera, and **Gallery Renderer** with the feature installed. **Gallery Pipeline** is active only while the sample controller is enabled during play. It restores the previous quality and default pipelines when disabled. To compare glow budgets, select **Gallery Renderer** and change **Glow Resolution**. The outline stays at the camera's full render resolution.
