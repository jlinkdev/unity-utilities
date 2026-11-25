# Shape Studies

Open **Scenes/Solids** or **Scenes/Surfaces** and use the Game view to compare the labeled meshes in Edit mode. Select a `.procmesh` under **Recipes**, edit its settings and choose **Apply**. The corresponding Mesh Filter updates in place. Choose **Apply as Copy** to keep a separate variation. The optional jlinkdev Sample Framework supplies the corner title.

| Comparison | What to try |
| --- | --- |
| Cylinder and cutaway frustum | In **Solids**, select **Cutaway frustum** and reduce its sweep. The gold material marks the caps and radial cut faces in the second material slot. Toggle **Close cut** to expose the interior. |
| UV sphere and icospheres | Compare the latitude rings of **UV sphere** with the triangular topology of **Icosahedron** and **Icosphere**. Switch the Scene view to **Shaded Wireframe** to inspect the edges. Increase icosphere subdivision from 0 to 2 to go from 20 to 320 triangles. |
| Capsule height | The material reads normalized height from vertex color R, ranging from blue at the base to gold at the top. Change **Body length** to see the range follow the new silhouette. |
| Disc and annulus | In **Surfaces**, compare the Cartesian grid on **Annulus** with the curved grid on **Radial UV strip**. Switch the latter to planar mapping to isolate the effect of the UV setting. |
| Torus and ribbon | Compare a complete torus with its open sweep, then compare the straight and curved versions of the same ribbon. Set **Bent ribbon** to a negative bend to reverse its arc. |

The grid material draws lines at regular UV intervals. To inspect actual triangle density on the grid plane, box or any other shape, use the Scene view wireframe. Each display centers and scales its mesh for comparison. Drag a recipe into an empty scene to inspect its authored dimensions and pivot at unit scale.
