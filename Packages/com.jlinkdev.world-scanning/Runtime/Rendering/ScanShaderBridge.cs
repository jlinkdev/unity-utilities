using UnityEngine;

namespace jlinkdev.UnityUtilities.WorldScanning.Rendering
{
    internal static class ScanShaderBridge
    {
        private static readonly int PulseCountId = Shader.PropertyToID("_WorldScanPulseCount");
        private static readonly int OriginRadiusId = Shader.PropertyToID("_WorldScanOriginRadius");
        private static readonly int AxisShapeId = Shader.PropertyToID("_WorldScanAxisShape");
        private static readonly int ColorIntensityId = Shader.PropertyToID("_WorldScanColorIntensity");
        private static readonly int Parameters0Id = Shader.PropertyToID("_WorldScanParameters0");
        private static readonly int Parameters1Id = Shader.PropertyToID("_WorldScanParameters1");
        private static readonly int Parameters2Id = Shader.PropertyToID("_WorldScanParameters2");
        private static readonly int Parameters3Id = Shader.PropertyToID("_WorldScanParameters3");
        private static readonly int Parameters4Id = Shader.PropertyToID("_WorldScanParameters4");

        private static readonly ScanSystem.ScanRenderData[] RenderData = new ScanSystem.ScanRenderData[ScanSystem.MaximumActiveScans];
        private static readonly Vector4[] OriginRadius = new Vector4[ScanSystem.MaximumActiveScans];
        private static readonly Vector4[] AxisShape = new Vector4[ScanSystem.MaximumActiveScans];
        private static readonly Vector4[] ColorIntensity = new Vector4[ScanSystem.MaximumActiveScans];
        private static readonly Vector4[] Parameters0 = new Vector4[ScanSystem.MaximumActiveScans];
        private static readonly Vector4[] Parameters1 = new Vector4[ScanSystem.MaximumActiveScans];
        private static readonly Vector4[] Parameters2 = new Vector4[ScanSystem.MaximumActiveScans];
        private static readonly Vector4[] Parameters3 = new Vector4[ScanSystem.MaximumActiveScans];
        private static readonly Vector4[] Parameters4 = new Vector4[ScanSystem.MaximumActiveScans];

        internal static void UploadGlobals()
        {
            int count = ScanSystem.FillRenderData(RenderData);
            for (int i = 0; i < count; i++)
            {
                ScanSystem.ScanRenderData data = RenderData[i];
                ScanVisualSettings visuals = data.Visuals;
                OriginRadius[i] = new Vector4(data.Origin.x, data.Origin.y, data.Origin.z, data.Radius);
                AxisShape[i] = new Vector4(data.Axis.x, data.Axis.y, data.Axis.z, (float)data.Shape);
                Color color = data.Color.linear;
                ColorIntensity[i] = new Vector4(color.r, color.g, color.b, data.Intensity);
                Parameters0[i] = new Vector4(visuals.BandWidth, visuals.BandSoftness, visuals.TrailLength, visuals.TrailIntensity);
                Parameters1[i] = new Vector4(visuals.GridCellSize, visuals.GridLineWidth, visuals.GridMajorEvery, visuals.GridIntensity);
                Parameters2[i] = new Vector4(visuals.GridMajorIntensity, visuals.EdgeIntensity, visuals.DepthEdgeThreshold, visuals.NormalEdgeThreshold);
                Parameters3[i] = new Vector4(visuals.EdgeThickness, visuals.NoiseScale, visuals.NoiseStrength, visuals.NoiseSpeed);
                Parameters4[i] = new Vector4(visuals.CameraDistanceFadeStart, visuals.CameraDistanceFadeEnd, data.CylinderHalfHeight, data.NormalizedTime);
            }

            Shader.SetGlobalInt(PulseCountId, count);
            Shader.SetGlobalVectorArray(OriginRadiusId, OriginRadius);
            Shader.SetGlobalVectorArray(AxisShapeId, AxisShape);
            Shader.SetGlobalVectorArray(ColorIntensityId, ColorIntensity);
            Shader.SetGlobalVectorArray(Parameters0Id, Parameters0);
            Shader.SetGlobalVectorArray(Parameters1Id, Parameters1);
            Shader.SetGlobalVectorArray(Parameters2Id, Parameters2);
            Shader.SetGlobalVectorArray(Parameters3Id, Parameters3);
            Shader.SetGlobalVectorArray(Parameters4Id, Parameters4);
        }
    }
}
