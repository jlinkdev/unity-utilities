# Procedural Meshes

## Requirements

Unity 6000.0 or newer. Generation uses the CPU and supports any render pipeline. Package Manager resolves Newtonsoft Json for the Editor recipe writer.

## Installation

In Package Manager, add the Git URL `https://github.com/jlinkdev/unity-utilities.git?path=/Packages/com.jlinkdev.procedural-meshes` or use **Add package from disk** and select this package’s `package.json`.

## Quick start

1. Choose **Assets → Create → jlinkdev → Procedural Mesh** in a writable Assets folder.
2. Open **Generator** in the Inspector and choose a shape such as **Torus**. Edit its settings and preview the draft. Switch back to revisit an earlier draft, or choose **Apply** to update consumers.
3. Drag the asset into a Mesh Filter. Use **Apply as Copy** to save a separate variation.

Expand **Placement, UV and vertex data** to rotate or scale UVs, choose a pivot or orientation, invert facing, or write normalized height into vertex color R.

Use the preview toolbar for shaded geometry, wireframe, UV checker, UV layout, normals, and vertex colors when enabled. Drag to orbit, middle-drag to pan, and use the wheel to zoom. Expand **Local bounds** for front and top projections, XYZ size and center. The cross marks the local pivot.

The recipe actions menu beside **Geometry** loads another `.procmesh` as a draft or resets generator settings. **Reset all settings** also resets placement, UVs and vertex data. Use **Revert** or **Apply** after inspecting the result.

```csharp
using JLink.ProceduralMeshes;

var recipe = new MeshRecipe { generator = "torus", radius = 1f, tubeRadius = 0.15f, sides = 48 };
var mesh = MeshGenerator.Generate(recipe);
```

The caller owns the generated Unity object. Enumerate `MeshCatalog.All` for stable IDs and display names.

## Sample

Import **Shape Studies** from this package’s **Samples** tab in Package Manager. Open **Solids** for topology, cut faces and vertex height, or **Surfaces** for UV mapping, sweeps and bends. [Sample guide](Samples~/Shape%20Studies/README.md)

## Limitations

Meshes use triangles and one primary UV channel. Cylinders and cones can assign closures to a second submesh. Partial torus sweeps have open ends. Undo and redo edit the Inspector draft. Applied recipes can be restored from version control.

## Documentation

Schema 1 recipes import with their original generator. Apply writes schema 2 with the selected settings and preserves the asset identity. Keep the Unity .meta file when moving the source outside the Editor. [Generator catalog](Documentation~/generators.md) · [Changelog](CHANGELOG.md)

## License

[MIT](LICENSE.md)
