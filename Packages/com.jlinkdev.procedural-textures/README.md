# Procedural Textures

## Requirements

Unity 6000.0 or newer. Generation uses the CPU and supports any render pipeline. Noise uses Unity Mathematics. Package Manager resolves Newtonsoft Json for the Editor recipe writer.

## Installation

In Package Manager, add the Git URL `https://github.com/jlinkdev/unity-utilities.git?path=/Packages/com.jlinkdev.procedural-textures` or use **Add package from disk** and select this package’s `package.json`.

## Quick start

1. Choose **Assets → Create → jlinkdev → Procedural Texture** in a writable Assets folder.
2. Open **Generator** in the Inspector and search for **Ring** or browse the Noise, Gradients, Masks and Patterns families. Edit its settings and preview the draft. Switch back to revisit an earlier draft, or choose **Apply** to update consumers.
3. Drag the asset into a material texture slot. Use **Apply as Copy** to save a separate variation.

Choose **Output and remapping → Color Mode → Alpha Mask** for white RGB with the generator result in alpha. Switch the preview from **Result** to **Alpha** to inspect that mask, or **Scalar** to see the processed field before color mapping. The pixel menu sets the preview resolution cap. Scroll the wheel to zoom around the pointer, drag to pan, and choose **Fit** to recenter.

Enable **Tile** to check seams, then **Scroll** to animate U and V in tiles per second. Choose **Roll** beside a generator or warp seed for a new variation. The recipe actions menu beside **Generator settings** can load settings from another `.proctex`, reset the generator, or reset the entire draft. Choose **Apply** to update the asset.

Choose **Color Ramp** and click the **Gradient** swatch to place color and opacity keys. For a horizontal shader lookup strip, start with **Linear**, set the dimensions to **256 × 1**, set **Transition width** to **1**, and enable **Fit to texel endpoints**.

Expand **Processing** on any generator to transform its coordinates, warp its shape with seeded noise, soften or streak it with blur, or extract an outline. These controls feed the draft preview and the generated texture.

```csharp
using JLink.ProceduralTextures;

var recipe = new TextureRecipe { generator = "ring", width = 256, height = 256, colorMode = TextureColorMode.AlphaMask };
var texture = TextureGenerator.Generate(recipe);
```

The caller owns the generated Unity object. Enumerate `TextureCatalog.All` for stable IDs and display names.

## Sample

Import **Texture Atlas** from this package’s **Samples** tab in Package Manager. Open **Resolution** to compare the same ridged field at 64 × 64 and 512 × 512. **Processing** shows warp, blur and outlines, and **Gradients and LUTs** shows a 256 × 1 palette shading noise. The other scenes cover noise, masks and patterns. [Sample guide](Samples~/Texture%20Atlas/README.md)

## Limitations

Output is linear RGBA32 with bilinear filtering. Noise and scattered patterns support independent period axes. Regular patterns repeat on both axes at their default transform. Arbitrary coordinate rotations and fractional scales can break periodicity. Filters clamp on open axes. Undo and redo edit the Inspector draft. Applied recipes can be restored from version control.

## Documentation

Schemas 1–3 import with neutral processing defaults. Apply writes schema 4 and preserves the native texture reference. Keep the Unity .meta file when moving the source outside the Editor. [Generator catalog and processing controls](Documentation~/generators.md) · [Changelog](CHANGELOG.md)

## License

[MIT](LICENSE.md)
