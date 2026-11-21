# Procedural Plants validation

## 2.3.1

Tested the imported samples in Unity 6000.0.58f1 on Windows.

| Check | Result |
| --- | --- |
| Matched renders | Captured v2.3.0 and v2.3.1 in Built-in and compared the serialized plant state, camera pose and orthographic size, and both lights' rotations, colors and intensities for each pair. [Jade](polish-jade.png) · [Variegated rubber](polish-rubber.png) · [Climbing and trailing pothos](polish-pothos.png) |
| Foliage and stage | Rendered all seven species using both Studio and PBR in Built-in and URP 17.0.4. All 28 views contained visible plant geometry and zero error-shader pixels. Adjusting the jade normal strength and roughness changed the rendered image in both pipelines. |
| Camera | Used the Game view buttons to switch between both pots and a four-node jade in pot 2. Portrait and landscape tests kept every vertex of a detached spider plantlet on screen and preserved its checkpoint. [Game view comparison](windowsill-close-up.png) |
| Growth and baking | All 62 package Edit Mode tests, eight sample integration tests and two Play Mode tests passed. Baked all seven species and reloaded their meshes after regenerating the source plants and removing the components. The saved vertices, UV0 and UV3 matched. All six archived v2.2.1 specimens reproduced their original growth continuations. |
| Windows x64 player | Ran the release player on Direct3D 11. Exercised care, propagation, training, persistence, and close-up framing when switching pots, inspecting a frozen specimen, entering the tray and starting a comparison. |

## 2.3.0

Validated on Windows with Unity 6000.0.58f1, using the imported Windowsill and Plant Studio samples.

| Check | Result |
| --- | --- |
| Package regression suite | All 55 Edit Mode and two Play Mode tests passed. Jade cases cover opposite pairs at every mature node, branching at one, three and seven crowns, deterministic continuation, all three leaf detail levels, closed mesh topology, care deformation and UV mesh baking. |
| Sample integration | Eight additional editor and Game view tests passed. Operated the Jade shoot preset and growth button, reopened a saved Unity scene and reproduced the same fork. Also exercised plantlet propagation, vine training, saved comparisons, the variation tray and Plant Studio controls. |
| Windows x64 release player | Grew seed 23 from four paired-leaf nodes to ten on the first six-unit step and a visible fork on the second. Restoring the intermediate bench produced identical growth JSON. Reached maturity at 35 nodes. [Player captures](windowsill-jade.png) |
| Compatibility | The archived 2.2.1 package produced six full specimens and their expected continuations. Version 2.3.0 loaded all six, advanced each by 5.25 units, and reproduced the original full specimen JSON byte for byte. |
| Rendering | Rendered the jade plant in URP 17.0.4 with zero error-shader pixels. The normal-strength and roughness probes changed 10,282 and 9,858 pixels respectively. The Windows player used Built-in on Direct3D 11. |

## 2.2.1

Verified in isolated projects on Windows with Unity 6000.0.58f1. The repository host targets 6000.3.21f1.

| Check | Result |
| --- | --- |
| Edit and Play modes | 47 package Edit Mode tests, three Windowsill editor or Game view integration tests, and two Play Mode tests passed. Compared positions, colors, normals, tangents, both UV channels, triangle indices and bounds with a fresh mesh build at three care levels across all six species. |
| Saved family | v2.2.0 produced a checkpoint with a dry spider mother, a watered daughter, the harvested runner and a pinned tray specimen. After restoration in v2.2.1 and further watering and growth, the complete Windowsill save matched the v2.2.0 continuation byte for byte. |
| Windowsill presentation | Compared final 1120 × 759 Game view captures against the previous release. The two-species preset view differed at 36 pixels and the daughter view at six. The maximum channel difference in either image was one level out of 255. |
| Release player | Built the imported Windowsill for Windows x64 and exercised training, cutting placement, plantlet detachment, per-pot care, freezing, living copies, and restoration of the bench and comparison. |
| URP 17.0.4 | Both presets rendered with zero error-shader pixels. PBR normal-strength and roughness checks passed. |
| Shelf profile | Repeated the synchronized 18-pot workload before and after the cleanup, including an idle frame and an allocation control. [Timings, managed allocations and methodology](architecture-review.md) |

## 2.2.0

Verified on Windows with Unity 6000.0.58f1 in isolated Built-in and URP projects. The repository host targets 6000.3.21f1.

| Check | Result |
| --- | --- |
| Package tests | 37 Edit Mode and two Play Mode tests passed. The new species pass closed leaf topology, winding, UV-area and detail checks. Plantlet tests compare attached and detached geometry and resume both plants from JSON. Care tests cover optional settings, reversible wilt, split time steps, per-species response and independent saves. |
| Windowsill integration | Two additional tests passed. Saved and reopened the editor scene with a detached plantlet and dry soil. Used Game view input to select a runner, plant the daughter, switch rubber presets, enable care, water the pot and turn care off. [Daughter after six growth units](spider-plantlet.png) · [Both species](windowsill-houseplants.png) |
| Standalone player | Built the imported Windowsill for Windows x64. Verified one-time runner detachment, independent daughter growth, watering and care persistence alongside existing cutting, comparison and tray workflows. |
| URP 17.0.4 | Rendered both species with zero error-shader pixels. The PBR normal-strength and roughness probes changed 3,217 and 10,287 pixels respectively. |

## 2.1.0

Verified on Windows 11 with Unity 6000.0.58f1 using an isolated project and Package Manager sample imports.

| Check | Result |
| --- | --- |
| Package tests | 32 Edit Mode tests and two Play Mode tests passed. New cases cover pose reproducibility, contact and branch forecasts, Monstera node eligibility, retained geometry, and independent new shoots on the donor and cutting. |
| Windowsill editor | Saved and reopened a comparison scene, checked both growth records, returned to the original bench, then planted the previously previewed Monstera with its saved pole position. |
| Windowsill Game view | Operated Compare, advanced both copies, opened Cuttings, switched the placement presets, rotated the preview and planted it. The new plant matched the preview. [Comparison](windowsill-comparison.png) · [Placement preview](cutting-placement.png) |
| Windows x64 player | Both copies started with eight nodes. After six growth units they had fourteen each, with six new pole contacts on the climbing specimen and zero on the trailing specimen. Verified checkpoint continuation, restoration of the original pots, Monstera propagation, live placement, and loading a 2.0-format bench with a pending cutting. |
| Rendering and packaging | Rendered the comparison under URP 17.0.4 and Built-in on Direct3D 11. Verified package metadata, unique asset GUIDs, and matching host and distributable sample scenes. |

## 2.0.0

Verified the local UPM package on Windows 11 with Unity 6000.0.58f1 on November 18, 2026. Both samples were imported through the Package Manager sample API.

| Check | Result |
| --- | --- |
| Package Edit Mode | 29 passed. Coverage includes v1 save migration, deterministic growth, rim and shelf contact, pole attachment, persistent contacts, pruning a mature plant, multiple dormant buds, and upright propagation with preserved stem lengths. |
| Leaf geometry | All four species passed closed-edge, consistent-winding and nonzero geometric and UV triangle-area checks at Simple, Standard and Close-up. Separate tests checked enclosed openings through mature Monstera blades at each detail level. |
| Prefab baking | Both living and static prefab assets reopened with the expected mesh and materials. A living instance resumed independently. The static instance referenced a persistent mesh asset. |
| Imported Windowsill | An additional Edit Mode test operated the authored bench, then entered Play mode and used Game view input to cut, plant in pot 2, select Climbing Monstera, pin and reroll a seed, and place a static copy. |
| Original Plant Studio | Its Game view interaction test also passed against the 2.0.0 sample import, covering PBR switching, long pothos, species and mesh detail selection, wireframe, stepping and restoration. |
| Package Play Mode | Two tests passed, including independently owned meshes, freezing a living cutting, and releasing the mesh on destruction. |
| Windows x64 player | Built the imported scene and exercised growth, clipping and planting, independent advancement, all six tray slots, pinning, living and frozen copies, and restoring a checkpoint on Direct3D 11. |
| URP | The imported Monstera rendered in URP 17.0.4 with zero error-shader pixels in the 900 × 640 capture. Changing normal strength changed 2,752 pixels and changing roughness changed 18,995 pixels in the separate 720 × 540 material check. |
| Packaging | Verified both sample scenes match their authoring copies, distinct asset GUIDs, package metadata and embedded dependencies. |

The [two-pot Windowsill](windowsill.png) and [six-specimen variation tray](variation-tray.png) are Game view captures from the interaction test. Validation covered Built-in and URP on desktop. The repository host's 6000.3.21f1 editor was unavailable for this run.

## 1.1.0

Validated November 18, 2026 on Windows 11 using Unity 6000.0.58f1. The package was installed from its local UPM directory in isolated projects. The authoring scene and the importable scene have identical serialized contents and distinct asset GUIDs.

| Check | Result |
| --- | --- |
| Package Edit Mode suite | 18 passed. Covered deterministic growth, exact clock and shape persistence, changed light, moisture, growth limits, UVs, indices, mesh baking, PBR import settings, long-vine geometry, continuing v1.0 saves and invalid input. |
| Leaf topology | All three species at all three detail levels passed checks for nonzero geometric and UV triangle area. Every indexed foliage edge had two opposite windings. Detail changes preserved the growth record. |
| Editor sample interaction | An additional Game view test switched PBR off and on, generated the long pothos, switched species and detail, enabled the wireframe, advanced growth, saved a specimen and restored it with the on-screen controls. |
| Package Play Mode suite | One test passed. Two plants kept independent meshes, preserved state through disable and re-enable, and rejected invalid saves. |
| Windows player | Imported Plant Studio through the Package Manager sample API, built Windows x64, and ran on Direct3D 11. Generated, rendered, saved and restored all three species. |
| PBR rendering | Under fixed lights and camera, changing only normal strength changed 8,661 pixels in Built-in and 8,695 in URP 17.0.4. Changing only roughness changed more than 48,000 pixels in each pipeline. Captures were 720 × 540 on Direct3D 11. |
| Packaging | The plant package passed the repository's package-specific checks for manifest, documentation, sample and assembly definitions. |

The [pothos comparison](pothos-variations.png) shows Windows player captures of the two presets under the same lighting. Each view is fitted independently. The lowest mesh points measured 0.90 m and 3.11 m below soil. The earlier species overview shows the original studio shading and isolated blade topology.

The repository host targets 6000.3.21f1. That editor version, mobile players and HDRP were outside this validation run.
