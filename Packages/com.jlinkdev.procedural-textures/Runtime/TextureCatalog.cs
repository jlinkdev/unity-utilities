using System;
using System.Collections.Generic;

namespace JLink.ProceduralTextures
{
    public sealed class TextureDefinition
    {
        public readonly string Id;
        public readonly string Menu;
        public readonly IReadOnlyList<string> Fields;

        internal TextureDefinition(string id, string menu, string fields)
        {
            Id = id;
            Menu = menu;
            Fields = Array.AsReadOnly(fields.Split(' '));
        }
    }

    public static class TextureCatalog
    {
        public static readonly IReadOnlyList<string> OutputFields = Array.AsReadOnly(
            "width height colorMode colorRamp remap blackPoint whitePoint threshold remapCurve invert contrast brightness".Split(' '));

        public static readonly IReadOnlyList<string> ProcessingFields = Array.AsReadOnly(
            "offset coordinateScale coordinateRotation domainWarp warpAmount warpFrequency warpSeed warpDirection blur blurRadius blurAngle detectEdges edgeWidth edgeStrength".Split(' '));

        public static readonly IReadOnlyList<string> SharedFields = CombineSharedFields();

        static IReadOnlyList<string> CombineSharedFields()
        {
            var fields = new List<string>(OutputFields);
            fields.AddRange(ProcessingFields);
            return fields.AsReadOnly();
        }

        public static readonly IReadOnlyList<TextureDefinition> All = Array.AsReadOnly(new[]
        {
            new TextureDefinition("value-noise", "Noise/Value Noise", "seed cellsX cellsY seamlessX seamlessY"),
            new TextureDefinition("white-noise", "Noise/White Noise", "seed cellsX cellsY seamlessX seamlessY"),
            new TextureDefinition("perlin", "Noise/Perlin", "seed cellsX cellsY seamlessX seamlessY"),
            new TextureDefinition("simplex", "Noise/Simplex", "seed cellsX cellsY seamlessX seamlessY"),
            new TextureDefinition("worley", "Noise/Worley", "seed cellsX cellsY seamlessX seamlessY jitter"),
            new TextureDefinition("fbm", "Noise/Fractal Brownian Motion", "seed cellsX cellsY seamlessX seamlessY basis octaves persistence lacunarity"),
            new TextureDefinition("ridged", "Noise/Ridged", "seed cellsX cellsY seamlessX seamlessY basis octaves persistence lacunarity"),
            new TextureDefinition("billow", "Noise/Billow", "seed cellsX cellsY seamlessX seamlessY basis octaves persistence lacunarity"),
            new TextureDefinition("directional-noise", "Noise/Directional", "seed cellsX cellsY seamlessX seamlessY alongU stretch"),
            new TextureDefinition("turbulence", "Noise/Turbulence", "seed cellsX cellsY seamlessX seamlessY basis octaves persistence lacunarity warp"),
            new TextureDefinition("linear", "Gradients/Linear", "rotation sizeX fitTexelEndpoints"),
            new TextureDefinition("radial", "Gradients/Radial", "sizeX"),
            new TextureDefinition("angular", "Gradients/Angular", "rotation"),
            new TextureDefinition("diamond", "Gradients/Diamond", "rotation sizeX"),
            new TextureDefinition("box-gradient", "Gradients/Box", "rotation sizeX"),
            new TextureDefinition("curve-gradient", "Gradients/Curve", "rotation sizeX fitTexelEndpoints profile"),
            new TextureDefinition("circle", "Masks/Circle", "sizeX softness signedDistance distanceRange"),
            new TextureDefinition("ellipse", "Masks/Ellipse", "sizeX softness sizeY rotation"),
            new TextureDefinition("rectangle", "Masks/Rectangle", "sizeX softness sizeY rotation signedDistance distanceRange"),
            new TextureDefinition("rounded-rectangle", "Masks/Rounded Rectangle", "sizeX softness sizeY rotation corner signedDistance distanceRange"),
            new TextureDefinition("polygon", "Masks/Polygon", "sizeX softness points rotation signedDistance distanceRange"),
            new TextureDefinition("star", "Masks/Star", "sizeX softness points innerRatio rotation signedDistance distanceRange"),
            new TextureDefinition("ring", "Masks/Ring", "sizeX softness thickness signedDistance distanceRange"),
            new TextureDefinition("arc", "Masks/Arc", "sizeX softness thickness sweep rotation signedDistance distanceRange"),
            new TextureDefinition("line", "Masks/Line", "sizeX softness thickness rotation signedDistance distanceRange"),
            new TextureDefinition("cross", "Masks/Cross", "sizeX softness thickness rotation signedDistance distanceRange"),
            new TextureDefinition("soft-disc", "Masks/Soft Particle", "sizeX"),
            new TextureDefinition("checker", "Patterns/Checkerboard", "cellsX cellsY"),
            new TextureDefinition("stripes", "Patterns/Stripes", "cellsX duty alongU"),
            new TextureDefinition("grid", "Patterns/Grid", "cellsX cellsY thickness"),
            new TextureDefinition("dots", "Patterns/Dot Grid", "cellsX cellsY sizeX softness"),
            new TextureDefinition("hex-grid", "Patterns/Hex Grid", "cellsX cellsY thickness"),
            new TextureDefinition("repeated-shapes", "Patterns/Repeated Shapes", "cellsX cellsY stamp sizeX softness rotation"),
            new TextureDefinition("scattered-shapes", "Patterns/Scattered Shapes", "cellsX cellsY seed stamp sizeX softness rotation jitter sizeJitter angleJitter seamlessX seamlessY"),
        });

        public static TextureDefinition Find(string id)
        {
            foreach (var definition in All)
                if (definition.Id == id) return definition;
            throw new ArgumentException($"Unknown texture generator '{id}'.");
        }
    }
}
