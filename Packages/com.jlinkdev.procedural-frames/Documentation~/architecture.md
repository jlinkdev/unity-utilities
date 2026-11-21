# Procedural frame architecture review

## Ownership

| Responsibility | Owner | Contract |
| --- | --- | --- |
| Editable design | `FrameRecipe`, `FrameBand`, `FrameEdges`, `FrameCorners` | Values live in the serializable recipe. Applying a saved design to a component makes an independent copy. |
| Outline and loop placement | `FrameContour` and `EdgeSection` | A contour supplies positions around the opening. A section maps distances across the rail to their sculpted positions, respecting each loop’s follow value. Handles and geometry use the same map. |
| Mesh generation | `FrameMesh` | Validates the recipe and writes positions, indices, normals, UVs and tangents to a caller-owned mesh. A bounded, reusable build workspace holds intermediate buffers. |
| Scene instance | `ProceduralFrame` | Owns the generated Unity mesh, current contour and revision. Picking reads the moulding triangles on demand and invalidates those reads when the revision changes. |
| Editing | Workshop and custom Inspector | Each view owns its active mode, camera projection and undo integration. The band, edge and corner tools convert projected gestures into recipe edits for both views. |
| Output | `FrameInspector.Bake` and `FrameObj.Write` | Baking copies the mesh into an independent Unity asset. OBJ export uses the mesh’s current UVs, normals and material slots. |

## Findings and changes

**Interactive rebuilds.** Adjacent profile strips sampled the same rail positions and tangents repeatedly. The builder now reuses each sampled ring at the next strip and still computes separate normals at profile creases. Shared build buffers are released for reuse after the mesh upload. An unusually large design can be built and exported without permanently growing the idle cache.

**Editing state.** The workshop and Inspector each tracked three independent mode flags. Each view now has a single active mode. Selecting an existing loop updates its controls without rebuilding its geometry. Corner hit testing uses the current generated contour, and profile and loop picking share one per-frame mesh cache.

**Design recall and framing.** Recall and Inspector Apply each rebuilt the same design twice. Both now rebuild once. Unreadable saved JSON leaves the active design and undo history intact. Workshop Fit centres on the generated bounds and accounts for the viewing angle, including frames with an asymmetric top crest.

## Verification on the six shipped designs

On Unity 6000.0.58f1, all twelve combinations of shipped design and UV layout produced identical before-and-after mesh hashes. The comparison included positions, normals, tangents, UVs, triangle winding and submesh boundaries.

The table shows mean main-thread time for eight warmed, synchronous component rebuilds in a Windows batch editor, using the tiled layout. These timings measure editing cost on the review machine.

| Design | Before | After |
| --- | ---: | ---: |
| Dramatic scalloped oval | 61.0 ms | 35.1 ms |
| Dramatic swept frame | 71.3 ms | 43.1 ms |
| Ornament study | 17.8 ms | 15.9 ms |
| Gallery loops | 0.82 ms | 0.36 ms |

## Remaining runtime cost

Changing geometry still rebuilds the complete mesh synchronously, including ornaments and tangents. Dense designs can exceed a 16.7 ms frame budget while dragging. Hover picking on a decorated rail still scans moulding triangles. Static display uses the generated mesh across frames. If continuous editing has a strict frame budget, profile a representative player build before choosing between a lower-detail drag preview and a more local mesh update.
