# Picture frame manual

## Choosing a construction

| Mount | What the generator builds | Most useful control |
| --- | --- | --- |
| Matted | A shaped rail with a rebate under its sight edge, a planar mat and an optional artwork face. Zero mat width gives a direct opening. | Pull the top handle to set the mat reveal. |
| Shadow box | The rail extends in front of a deeper seat for the mat and insert. | Increase rail depth and orbit the frame to see the recess. |
| Floater | The rail includes an inward-facing rear shelf. The canvas face and its side walls sit inside the rail with a visible channel. | Pull the top handle to set the float gap. |

All dimensions use local metres. Artwork size is the visible face. For a straight rectangle, outside width equals artwork width plus twice the sum of the border and rail width. The same formula applies to height. For a sculpted silhouette use the generated mesh bounds for its overall dimensions. The border is the mat width or the float gap. A frame faces local −Z, local Y points up, and its wall-mounted origin lies at the opening centre on the front reference plane. **Include Artwork** can be turned off to mount game content in the opening. **FrameRecipe.Seat** gives the mounting depth for positioning your own insert.

## Shaping the moulding

Start from Flat, Bevel, Scoop, Reverse Scoop, Ogee or Stepped. The cross-section curve runs from the sight edge at 0 to the outer rail edge at 1. A point’s horizontal position is a fraction of rail width. Its height is a fraction of relief. Face heights clamp to 0–1. The builder samples the profile keys and fills in the intervals with regular subdivisions. Nearly coincident samples merge to avoid collapsed triangles. Increasing **Curve Segments** also refines the rounded and oval outlines.

Rectangle rails meet on mitre lines. Rounded frames have tangent corner arcs, while oval frames follow outward offsets from an ellipse. All four sides use the same contour. Moulding width and depth can be adjusted independently of artwork size. The wall behind the lip and the floater shelf are part of the moulding mesh.

## Freestanding tabletop frames

Enable **Easel back** in the Inspector or under **Frame → Mount** in the workshop. Set **Lean angle** from 5° to 25°. Use **Back** in the workshop to inspect the support. Rectangle, rounded and oval outlines can all use the stand with any artwork mount. Adjust artwork width and height to switch between portrait and landscape proportions.

The backing panel, barrel hinge and tapered leg are part of the generated mesh. The foot is cut flat at the same height as the lowest point of the sculpted frame. With the easel enabled, local Y = 0 is the tabletop, so placing the object origin on a level surface sets the frame down. The mesh includes the lean, including baked mesh assets and OBJ exports. Keep the GameObject upright for placement. **EaselBack** is the fourth material slot and supports both UV layouts. Disabling the option restores the upright, opening-centred mesh.

Set `recipe.easelBack` and `recipe.leanAngle` then call `frame.Rebuild()`. Use `frame.DesignToWorld` to position gameplay content authored in upright design coordinates, including an insert at `recipe.Seat`. [Front and rear views](tabletop-easel.png)

## Sculpting oval scallops

Choose **Oval** under **Frame**, then **Edges**. In the Inspector choose **Sculpt oval edges in Scene view**. Pull the gold top and green bottom independently. Either blue side handle edits the left and right together. Each handle shapes a central crest with a smaller shoulder on either side. **Projection** is measured in millimetres. **Reach** spreads that feature 12–45 degrees in each direction around the ellipse. Negative projection draws the scallops inward.

Use **Follow edge (%)** on each loop to choose how much it traces the scallops. The artwork opening stays elliptical. The **Subtle scalloped oval** and **Dramatic scalloped oval** designs use 0% on the bead row, 45% on the middle ridge and 100% on the outer ridge. For gameplay edits set `recipe.edges.ovalTop`, `ovalBottom` and `ovalSides` in metres, or `ovalTopReach`, `ovalBottomReach` and `ovalSideReach` in degrees, then call `frame.Rebuild()`. [Side-by-side renders](oval-scallop-studies.png) · [Workshop handles](oval-scallop-controls.png)

## Sculpting rectangular edges

Select **Edges** in the workshop or **Sculpt rectangular edges in Scene view** in the Inspector. The four gold handles mark the mitres, the eight green handles mark the waists, and the four blue handles mark the side centres. Drag outward to widen the rail locally or inward to narrow it. **Reach** adjusts the distance from the selected feature over which its projection falls back to zero, as a percentage of the side length. Opposite sides share a shape by default. **Shape top rail separately** lets the top centre and waists have their own projection. Corners stay linked so the mitres meet.

In **Loops**, **Follow edge (%)** specifies how much of the local projection reaches that loop. Zero keeps a straight run, 50 follows half the projection, and 100 follows the outer edge. The generator moves the shoulders and crest together while space permits. At tight waists or where loop paths would cross, it compresses their spacing to keep the rail surface ordered. Loops with coincident crests share that section of the surface. Inward pulls preserve at least 28% of the original rail width. The artwork, mat, and seat retain their original opening. Edge settings are stored when switching to a rounded or oval frame.

**Subtle swept frame** and **Dramatic swept frame** in `Runtime/Designs` reproduce the comparison. Apply either FrameDesign in the Inspector or assign it as the workshop’s **Starting Design**. Both use the same artwork dimensions, inner beads and corner scrolls. The three loops use 0%, 45% and 100% follow respectively. [Side-by-side renders](rectangular-edge-studies.png) · [Workshop edge handles](rectangular-edge-controls.png)

For gameplay edits, set `recipe.edges.corner`, `waist` and `centre` in metres, and `band.edgeFollow` from zero to one. Call `frame.Rebuild()` after changing the recipe.

## Profile loops

Each editable loop belongs to `FrameRecipe.bands`. Its cross section adds a signed height to the underlying moulding all the way around the frame. Rounded, sharp, bevelled and stepped sections can overlap. Grooves stop short of the seat or shelf to keep the rail closed.

Set **Relative** to keep the centre at a fraction of rail width. **Inner** and **Outer** measure a physical distance from their respective edges. All three retain the physical band width while there is room on the rail. A narrower rail temporarily constrains the visible width and centre. For gameplay edits, call `band.MoveTo(railWidth, metresFromInnerEdge)`, `band.SetWidth(railWidth, metres)` or set `band.height` in metres and call `frame.Rebuild()`. Loop edits update the rail geometry, normals, tangents and UVs. Existing recipes with an empty band list use their original profile.

## Repeating ornament

Select a loop in the Scene view editor or in the runtime workshop’s **Loops** mode. Each pattern follows the selected loop as you move it or change the frame outline.

| Pattern | Geometry | Effect of increasing Gap |
| --- | --- | --- |
| Beads | Rounded bosses | Opens space between beads |
| Leaf scroll | A vine with pointed leaves and a curled tip | Separates complete sprigs |
| Rope twist | Two rounded plies winding around a common axis | Separates turns into pieces with finished ends. A zero gap makes one continuous cord |
| Fluting | Shallow stopped channels across a raised band | Widens the flat fillet between channels |
| Egg-and-dart | A tapered egg in a U-shaped cup alternating with a faceted spear | Separates complete egg-and-dart pairs |

**Size** is the nominal motif width across the rail. It also scales the motif’s length and relief. **Gap** is the requested clear distance along the loop, measured between motif footprints. Drag the green dot along the lower rail to change size or the coral dot to change spacing. The inspector and workshop also show the controls in millimetres. **Plain** removes the ornament and keeps the underlying ridge or groove.

On a rectangle, each rail centres a whole number of repeats. Continuous rope and fluting meet on the mitres. Individual ornaments leave corner clearance. **Mirror opposite rails** reverses the scroll direction on the opposite pair. Rounded and oval frames distribute the repeats over a single closed path. The generator fits whole repeats, so the visible gap can differ slightly from the requested gap. It constrains motif width near a rail edge, retains at least one repeat per run, and caps each loop at 256 repeats.

From gameplay code set `band.ornament = BandOrnament.LeafScroll`, `band.detailSize = .024f` and `band.detailGap = .003f`, then call `frame.Rebuild()`. Those lengths are in metres. Repeats are relief surfaces embedded in the moulding. They share its material slot and bake into the same mesh and OBJ. The Painter atlas gives each ornament its own UV space.

## Linked corner ornaments

Choose **Corners** in the workshop or **Edit linked corners in Scene view** in the Inspector. Choose **Leaf fan**, **Twin scroll** or **Scallop shell** once for the whole frame. Select **Position** and drag any gold corner handle along the diagonal, select **Size** and drag along the rail, or select **Relief** and drag upward. The sliders edit the same values. Position runs from the inner to the outer end of the available travel. Size is the nominal ornament width across the rail. Relief is the height of its crest above the moulding. The generator fits size to the rail and to the distance between corners.

Rectangles orient each ornament on its mitre. Rounded frames carry the wings around the adjoining corner arc. Ovals keep the chosen settings for a later outline change and omit corner geometry. A new or previously saved frame starts without a corner ornament unless its recipe specifies one.

A `ProceduralFrame` renders one generated mesh through a MeshFilter and MeshRenderer. A continuous ridge or groove changes the swept cross section of the rail itself. Beads, scrolls and corner leaves contribute additional triangle patches in the moulding submesh. Each corner vertex samples the front of the rail at its position. The underside of the relief meets or slightly penetrates that surface. The rail faces continue underneath, and the ornament has its own vertex indices. Overlapping ornaments layer geometrically. UV seams and hard edges also use separate vertices. This construction suits shading and game rendering. A watertight solid for printing would require a separate mesh-union step.

The corner selection and its three linked parameters live in `frame.recipe.corners`. Set them and call `frame.Rebuild()` after a gameplay edit. The OBJ and baked mesh include the corner triangles and their UVs. [Corner variations](corner-variations.png) · [Triangle topology](corner-topology.png) · [Full-frame wireframe](corner-full-wire.png)

## Materials and UVs

The Mesh Renderer slots stay in **Moulding, Mat, Artwork** order when a mount omits a surface. **Easel back** appends its material after Artwork. Assign a Unity material produced from a Substance graph to the appropriate slot.

- **Tiled** gives the moulding a longitudinal U axis and an unrolled V axis across the profile, sides and back. Each rail or curved quadrant has a seam. At a mitre the two grain directions meet along the diagonal. **Tile Size** gives metres per repeat. Curved strips accumulate distance along each offset contour. The mat uses planar physical coordinates. The artwork face fills one upright 0–1 image rectangle. Rounded and oval shapes crop its corners. Canvas sides use strip mapping.
- **Paintable** gives the rails, mat, insert and canvas sides a common, non-overlapping UV0 atlas in 0–1 with space between islands. The islands share a uniform scale. Very long rails and thin details can need a higher texture resolution. Tangents and normals are generated for both layouts.

For Substance 3D Painter, switch to **Paintable** (called **Painter atlas** in the workshop), export the OBJ, then bake the Unity mesh asset from that same design and layout. Keep the companion MTL alongside the OBJ. It names the texture sets for each visible material slot. The OBJ is in metres and uses right-handed Y-up coordinates. Frame edits can change the packing, so finish shaping before painting. For tileable Substance materials, keep the mesh in **Tiled** mode.

## Runtime integration

Attach a **Procedural Frame** to a GameObject for automatic mesh lifetime management. Assign a saved FrameDesign through the inspector’s **Apply design**, or apply it from gameplay code. Apply deep-copies the recipe, including its profile curve.

```csharp
using JLink.Frames;
using UnityEngine;

public sealed class GalleryFrame : MonoBehaviour
{
    public ProceduralFrame frame;
    public FrameDesign design;

    void Start()
    {
        frame.Apply(design.recipe);
        frame.recipe.outline = FrameOutline.Oval;
        frame.recipe.mount = FrameMount.Floater;
        frame.recipe.floatGap = .012f;
        frame.Rebuild();
    }
}
```

With a caller-owned Mesh, call `FrameMesh.Build(recipe, mesh)`. Build validates dimensions in the supplied recipe, regenerates UVs and tangents, and replaces the mesh contents. Call when a design changes. Export with `FrameObj.Write(mesh, path)` on platforms with writable files. Reuse the same Mesh between edits and destroy caller-owned meshes when finished. Unity JSON serialization can round-trip a FrameRecipe for game saves. The sample keeps one local design in PlayerPrefs.

## Design references

Larson-Juhl’s moulding catalogue distinguishes flat, sloped, scoop, reverse-scoop and shadow-box families. That informed the separation of moulding profile from mount construction. [Larson-Juhl moulding catalogue](https://www.larsonjuhl.com/en-US/mouldings/float-liner.html)

American Frame distinguishes the visible sight opening from the frame’s interior measurement and describes the lip and rabbet that seat artwork. The package uses visible artwork dimensions for its controls and models the seat behind the lip. [Measuring art for custom framing](https://www.americanframe.com/resources/measuring-art)

A canvas floater leaves the front edge exposed with a gap around the canvas and support from behind. The floater mount models that gap and its supporting shelf. [American Frame canvas guide](https://www.americanframe.com/picture-frames/canvas-frames/why-frame-a-canvas)
