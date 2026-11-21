using System.Collections.Generic;
using UnityEngine;

namespace JLink.Plants
{
    public sealed partial class PlantMesher
    {
        readonly List<(int start, int end, Vector3 socket)> blades = new List<(int, int, Vector3)>();
        readonly List<Vector3> posedVertices = new List<Vector3>();
        readonly List<Color32> posedColors = new List<Color32>();
        static readonly Color WiltColor = new Color(.53f, .46f, .24f);

        void RecordBlade(int start, Vector3 socket) => blades.Add((start, vertices.Count, socket));

        // Only the component that owns this mesher may reuse its last structural build.
        internal void UpdateLeafPose(Mesh mesh, float droop)
        {
            UploadLeafPose(mesh, droop);
            RecalculateSurface(mesh);
        }

        void UploadLeafPose(Mesh mesh, float droop)
        {
            if (droop == 0)
            {
                mesh.SetVertices(vertices);
                mesh.SetColors(colors);
                return;
            }

            // Always deform from the hydrated blade. Repeated watering cannot accumulate drift.
            posedVertices.Clear();
            posedVertices.AddRange(vertices);
            posedColors.Clear();
            posedColors.AddRange(colors);
            foreach (var blade in blades)
            {
                for (int i = blade.start; i < blade.end; i++)
                {
                    Vector3 point = vertices[i];
                    point.y -= droop * Vector3.Distance(point, blade.socket) * uvs[i].y;
                    posedVertices[i] = point;
                    posedColors[i] = Color.Lerp(colors[i], WiltColor, droop * .46f);
                }
            }
            mesh.SetVertices(posedVertices);
            mesh.SetColors(posedColors);
        }

        static void RecalculateSurface(Mesh mesh)
        {
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
        }
    }
}
