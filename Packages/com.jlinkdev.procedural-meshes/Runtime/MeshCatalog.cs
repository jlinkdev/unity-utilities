using System;
using System.Collections.Generic;

namespace JLink.ProceduralMeshes
{
    public sealed class MeshDefinition
    {
        public readonly string Id;
        public readonly string Menu;
        public readonly IReadOnlyList<string> Fields;

        internal MeshDefinition(string id, string menu, string fields)
        {
            Id = id;
            Menu = menu;
            Fields = Array.AsReadOnly(fields.Split(' '));
        }
    }

    public static class MeshCatalog
    {
        public static readonly IReadOnlyList<string> SharedFields = Array.AsReadOnly(
            "heightInColorR uvScale uvRotation uvOffset pivot orientation insideFacing".Split(' '));

        public static readonly IReadOnlyList<MeshDefinition> All = Array.AsReadOnly(new[]
        {
            new MeshDefinition("cylinder", "Cylinder and Frustum", "bottomRadius topRadius height sides heightSegments seamDegrees sweep capEnds closeCut separateEnds sideUV"),
            new MeshDefinition("cone", "Cone", "bottomRadius height sides heightSegments seamDegrees sweep capEnds closeCut separateEnds sideUV"),
            new MeshDefinition("plane", "Plane and Grid", "width depth columns rows"),
            new MeshDefinition("box", "Box", "width height depth columns rows depthSegments"),
            new MeshDefinition("uv-sphere", "UV Sphere", "radius sides rings seamDegrees"),
            new MeshDefinition("icosphere", "Icosphere", "radius subdivisions"),
            new MeshDefinition("capsule", "Capsule", "radius bodyLength sides rings heightSegments seamDegrees"),
            new MeshDefinition("disc", "Disc", "radius sides bands sweep discUV seamDegrees"),
            new MeshDefinition("annulus", "Ring and Annulus", "radius innerRadius sides bands sweep discUV seamDegrees"),
            new MeshDefinition("torus", "Torus", "radius tubeRadius sides tubeSegments sweep seamDegrees"),
            new MeshDefinition("ribbon", "Ribbon and Arc", "width length bend rows columns"),
        });

        public static MeshDefinition Find(string id)
        {
            foreach (var definition in All)
                if (definition.Id == id) return definition;
            throw new ArgumentException($"Unknown mesh generator '{id}'.");
        }
    }
}
