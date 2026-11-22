# Pottery Workshop

The wheel uses the shared Studio Plinth prefab. The sidebar and its editable numeric sliders use the same framework controls as Frame Workshop.

Open **Scenes/Pottery Workshop.unity** and press **Play**. Choose a silhouette in the tray. In **Form**, grab the body and drag right to widen a band all the way around, or left to pinch it. Set **Band height (%)** to control how much clay moves above and below your hand. Pull a diamond for precise profile edits, or double-click the right outline to insert a ring. Drag empty space to orbit and use the wheel to zoom.

Try widening an urn after placing handles. Choose **Clay** and drag left on its shoulder for a thumbprint. **Keep a copy** stores a version in the local shelf. Use **Save mesh** or **Save design** in the Unity Editor for project assets.

In **UVs**, turn on **Numbered checker** and shape a band to see how the texture follows the clay. Adjust **Tile size (cm)** or **Refit to current shape**. To paint the finished pot, use **Export Painter OBJ** and **Save paintable mesh** with the same texture-set selection.

Expand **Pottery turntable** to inspect the wheel, camera, light, UI Document, and Vessel. Edit the Vessel recipe in the Inspector. Assign a Pot Design to **Starting Design** before entering Play Mode to reopen a saved vessel.

The upper-right controls dim the studio, toggle the grid lines, and expand or restore the vessel viewport. The preset tray shows renders of the six starting meshes.

## Sample presentation

On import, choose **Install** to use the shared fonts, icons, and sample header. To install later, choose **Tools > jlinkdev > Procedural Pots > Install Sample Framework**. Before the release tag is published, use Package Manager's **Install package from disk** and select `Packages/com.jlinkdev.sample-framework/package.json` from a Unity Utilities checkout.
