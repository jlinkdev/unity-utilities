# Changelog

## Unreleased

- Added an optional tabletop easel with a backing panel, hinge, fitted leg, adjustable lean and a separate UV-mapped material slot in baked meshes and OBJ exports.

- Reused contour samples and mesh build buffers during editing, shared surface picking across tools, and removed redundant rebuilds when selecting loops or applying designs. Workshop Fit now centres asymmetric outlines, and a failed recall preserves the current design.

- Added oval scallop handles for the top, bottom and linked sides in the runtime workshop and Scene view, with adjustable reach, per-loop follow and two editable oval designs.

- Added rectangular silhouette sculpting with linked corner, waist and centre handles, adjustable reach, an independent top rail, per-loop edge follow and two swept-frame example designs.

- Added linked leaf fans, twin scrolls and scallop shells at the four corners of rectangular and rounded frames, with shared position, size and relief handles in the workshop and Scene view.

- Added rope twists, stopped fluting and egg-and-dart pairs to loop ornaments. The same size and spacing handles edit all five patterns. A zero-gap rope and the continuous fluted band meet at rectangular mitres.

- Added bead and leaf-scroll ornaments with whole-loop size and gap controls in the runtime workshop and Scene view, mirrored scrolls on rectangular rails, tiled and paintable UVs, and the Ornament study sample design.

- Added continuous ridge and groove loops with on-frame position, width and signed height handles in the runtime workshop and Scene view. Each loop stores its cross section and anchor in the editable design and bakes into the moulding mesh.

## 0.1.0

- Added rectangle, rounded and oval outlines, six sculptable moulding profiles, matted prints, deep shadow boxes and canvas floaters.
- Added a playable frame workshop, Scene view handles, editable designs, tiled and paintable UVs, mesh asset baking and runtime OBJ export.
