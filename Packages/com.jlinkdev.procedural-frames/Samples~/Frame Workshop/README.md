# Frame Workshop

The inspector tabs, filled parameter sliders, history buttons and view tools share their presentation with Pottery Workshop. The frame-specific rail, loop, edge and corner tools remain editable in the sidebar.

Open **Scenes/Frame Workshop.unity** and press **Play**. Expand **Framing bench** to edit the camera, light, Frame, and UI Document before Play. The saved frame uses the **Ornament study** recipe and has a beaded inner lip and an outer leaf-scroll loop ready to edit. It works in a new empty scene with a **Frame Workshop** component as well.

- Choose a moulding on the bottom shelf to apply its cross section to the current frame. The arrows step through the six profiles. **Undo** restores the preceding profile.
- Choose **Loops** to edit continuous ridges and grooves. Click a loop and drag it across the rail, pull a side dot to change its width, or drag the blue top dot up or down to change its height. Use **+ Ridge** or **+ Groove** to preview and place another loop. **Frame** returns to the base moulding.
- Select a loop and choose **Beads**, **Leaf scroll**, **Rope twist**, **Fluting** or **Egg-and-dart**. Choose **Plain** to clear its ornament. Drag the green dot to resize its repeats or the coral dot to change their spacing. The **Size** and **Gap** sliders measure millimetres.
- Choose **Corners** on a rectangular or rounded frame. Select a shared motif, choose **Position**, **Size** or **Relief**, and drag a gold handle on any corner to update all four.
- Choose **Oval** then **Edges** to drag the top and bottom independently or both sides together. Adjust **Reach** in degrees. The **Subtle scalloped oval** and **Dramatic scalloped oval** starting designs show the range.
- Choose **Edges** on a rectangular frame. Drag gold corners, green waists or blue centres and set the reach. Under **Loops**, set **Follow edge (%)** to keep a border straight or carry it into the sweep. Use **Subtle swept frame** or **Dramatic swept frame** as a Starting Design to try the pictured examples.
- Grab the moulding between the gold dots and drag up or down. A ridge or hollow follows the entire perimeter. A stroke near an existing profile point moves that point. A stroke between points introduces a new one.
- Pull the left or bottom dot to resize the visible artwork. Pull the right dot to resize all rails. The top dot adjusts the mat width or canvas float gap.
- Drag empty space to turn the frame. Scroll over the model to zoom. **Front** restores a face-on view and **Fit** reframes the current size.
- Drag points in the cross section for finer control. Its left edge faces the opening. Double-click to add a point and right-click an interior point to remove it.
- **Undo** and **Redo** step through shape edits. **Keep design** stores one recipe on this device. **Recall** restores it. Assign a **Starting Design** to the workshop component to control the opening design.
- Choose **Tiled material** for a repeating finish or **Painter atlas** for baking and painting. **Export OBJ** writes to `Application.persistentDataPath/Picture Frames`. The status tooltip and player log give the full path. In the Unity editor **Save mesh** also appears in Play Mode.
- Assign optional moulding, mat and artwork materials on the workshop component before pressing Play. The three preview materials provide a texture-free starting point.

## Sample presentation

On import, choose **Install** to use the shared fonts, icons, and sample header. To install later, choose **Tools > jlinkdev > Procedural Picture Frames > Install Sample Framework**. Before the release tag is published, use Package Manager's **Install package from disk** and select `Packages/com.jlinkdev.sample-framework/package.json` from a Unity Utilities checkout.
