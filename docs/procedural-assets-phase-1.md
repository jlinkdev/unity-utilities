# Procedural assets · Phase 1

Two independent UPM previews implement the editable-asset workflow. Use **Assets → Create → jlinkdev → Procedural Texture** for value noise or **Procedural Mesh** for an open cylinder, frustum or cone. The asset extension selects the native output type. The recipe stores the generator identity.

## Asset contract

- Each UTF-8 JSON file contains a schema version, generator identifier, generator version and typed settings. The importer rejects unknown versions. Keep its Unity .meta file in source control.
- The importer registers one main object with a fixed identifier (`texture` or `mesh`). Dimensions, seed, segmentation, filename and display name can change independently of that identifier.
- The Inspector keeps a temporary serialized draft. Preview controls operate on transient objects. Apply validates and replaces the source file, then synchronously regenerates the native output. Apply as Copy imports a new file before discarding the original asset's draft.
- Runtime callers can use the same CPU generators. A scene referencing the imported objects loads the baked outputs. The Windows proof scene uses a Mesh Filter and a material texture slot.

## Verified on Windows with Unity 6000.0.58f1

| Exercise | Observed result |
| --- | --- |
| Each package installed alone in a fresh consumer | Five Edit Mode cases passed in each project. Both compile independently of the sibling package, URP and the Sample Framework. |
| Project window asset creation | Both Create entries accepted inline naming and produced native main objects of the expected type. |
| Graphical Inspector and asset previews | The graphical Editor drew each custom Inspector and generated a 128-pixel Project preview for each asset. |
| Draft Undo, Redo, Revert and Apply | Source remained unchanged through draft edits. Revert restored the starting value. Apply changed the file and the loaded native output immediately. Each package includes a regression test. |
| Duplicate and reimport | The duplicate received a new GUID and retained the original settings. Each regenerated original preserved its GUID and local file ID. |
| Cross-asset references | A saved material and prefab retained their texture and mesh after a texture width change from 256 to 512, a height subdivision change from 4 to 12, and a mesh rename. |
| Fresh import and headless operation | Copied the saved fixture, source recipes and .meta files into a second project with an empty Library. A graphics-free batch import resolved the saved prefab and completed the Windows build. |
| Worker execution | Queued each actual importer explicitly as an asynchronous artifact job on a copy of its recipe. Unity worker logs recorded completed artifacts for both importer types. |
| Windows x64 player, built-in renderer, Direct3D 11 | Loaded the saved prefab with a 325-vertex mesh and a 512-pixel-wide texture. A camera readback found 1,738 shaded grayscale pixels against the magenta background. |
| Generator checks | Noise tests cover independent periodicity, repeatable raster output and feature placement. Mesh tests check outward triangles at both cone orientations, matched normals and positions at the UV seam, and height in vertex color R. |

The exact host project editor (6000.3.21f1), other platforms, interactive save-dialog input, and performance at maximum output sizes remain unverified. Phase 1 ships schema version 1. Migration fixtures become necessary when the first incompatible schema or generator change is introduced.

## Reproduce the lifecycle check

1. Create a noise texture and a mesh, assign the texture to a material, then assign the material and mesh to a prefab. Save a scene containing the prefab.
2. Record both source GUIDs and the material and prefab reference file IDs. Duplicate each recipe in the Project window.
3. Edit the original texture width and mesh height segments. Exercise Undo, Redo and Revert, then apply a change. Inspect the diffs in the two JSON files and confirm the copies keep their previous settings.
4. Move the mesh in the Project window, close the editor, and import the saved assets and their .meta files into a fresh consumer. Open the scene and build a player for the target under test.
5. For the automated cases, include the package ID in the consumer manifest's `testables` array, install Unity Test Framework, and run the package's Edit Mode suite.

## Milestone status

All four milestones are complete. Phase 1 established the asset lifecycle, Phase 2 added foundational generators and shared processing, and Phase 3 completed the Inspector and preview workflows. [Phase 2 verification](procedural-assets-phase-2.md)

Phase 4 delivered two independent Package Manager galleries, Texture Atlas and Shape Studies. Texture Atlas imported in a texture-only Unity 6000.0.58f1 project with 45 recipes across six scenes.
