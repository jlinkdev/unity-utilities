using UnityEngine;
using UnityEngine.Rendering;

namespace jlinkdev.UnityUtilities.Portals
{
    /// <summary>
    /// Draws a temporary portal aperture immediately beyond the gameplay
    /// camera's near plane. This is deliberately separate from the ordinary
    /// portal surface: recursive cameras never see it, and the portal's real
    /// projection and depth remain untouched.
    /// </summary>
    internal static class PortalNearClipFix
    {
        private const string ShaderResourceName = "PortalNearClipFix";
        private static readonly Vector3[] Vertices = new Vector3[4];
        private static readonly Vector2[] Uvs =
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        };
        private static readonly int[] Triangles = { 0, 2, 1, 0, 3, 2 };

        private static GameObject capObject;
        private static Mesh capMesh;
        private static MeshRenderer capRenderer;
        private static Material capMaterial;
        private static MaterialPropertyBlock properties;

        internal static void Show(Camera camera, Portal portal, Texture portalTexture)
        {
            if (camera == null || portal == null || portalTexture == null || portal.SurfaceRenderer == null)
                return;
            if (!EnsureResources())
                return;

            Transform capTransform = capObject.transform;
            if (capTransform.parent != camera.transform)
                capTransform.SetParent(camera.transform, false);
            capTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            capTransform.localScale = Vector3.one;

            int visibleLayer = FirstVisibleLayer(camera.cullingMask);
            if (visibleLayer < 0)
                return;
            capObject.layer = visibleLayer;

            float capDistance = Mathf.Max(camera.nearClipPlane * 1.01f, camera.nearClipPlane + 0.0001f);
            SetViewportQuad(camera, capDistance);

            Renderer surface = portal.SurfaceRenderer;
            Bounds localBounds = surface.localBounds;
            Vector3 portalNormal = portal.transform.forward;
            Vector3 portalPoint = portal.transform.position;

            properties.Clear();
            properties.SetTexture(PortalShaderProperties.PortalTexture, portalTexture);
            properties.SetMatrix(PortalShaderProperties.PortalWorldToLocal, surface.transform.worldToLocalMatrix);
            properties.SetVector(
                PortalShaderProperties.PortalBounds,
                new Vector4(localBounds.min.x, localBounds.min.y, localBounds.max.x, localBounds.max.y));
            properties.SetVector(
                PortalShaderProperties.PortalPlane,
                new Vector4(portalNormal.x, portalNormal.y, portalNormal.z, -Vector3.Dot(portalNormal, portalPoint)));
            properties.SetVector(PortalShaderProperties.CameraForward, camera.transform.forward);
            properties.SetFloat(PortalShaderProperties.CapDistance, capDistance);

            Material sourceMaterial = surface.sharedMaterial;
            properties.SetColor(
                PortalShaderProperties.Tint,
                sourceMaterial != null && sourceMaterial.HasProperty(PortalShaderProperties.Tint)
                    ? sourceMaterial.GetColor(PortalShaderProperties.Tint)
                    : Color.white);
            properties.SetColor(
                PortalShaderProperties.EdgeColor,
                sourceMaterial != null && sourceMaterial.HasProperty(PortalShaderProperties.EdgeColor)
                    ? sourceMaterial.GetColor(PortalShaderProperties.EdgeColor)
                    : new Color(0.08f, 0.75f, 1f, 1f));
            properties.SetFloat(
                PortalShaderProperties.EdgeWidth,
                sourceMaterial != null && sourceMaterial.HasProperty(PortalShaderProperties.EdgeWidth)
                    ? sourceMaterial.GetFloat(PortalShaderProperties.EdgeWidth)
                    : 0.025f);

            capRenderer.SetPropertyBlock(properties);
            capRenderer.enabled = true;
        }

        internal static void Disable()
        {
            if (capRenderer != null)
                capRenderer.enabled = false;
        }

        internal static void Dispose()
        {
            DestroyObject(capObject);
            DestroyObject(capMaterial);
            DestroyObject(capMesh);
            capObject = null;
            capMaterial = null;
            capMesh = null;
            capRenderer = null;
            properties = null;
        }

        private static bool EnsureResources()
        {
            if (capRenderer != null && capMaterial != null && capMesh != null)
                return true;

            Shader shader = Resources.Load<Shader>(ShaderResourceName);
            if (shader == null || !shader.isSupported)
            {
                Debug.LogError("jlinkdev Portals could not load the URP near-clip fix shader.");
                return false;
            }

            capMesh = new Mesh
            {
                name = "jlinkdev Portal Near Clip Fix",
                hideFlags = HideFlags.HideAndDontSave
            };
            capMesh.vertices = Vertices;
            capMesh.uv = Uvs;
            capMesh.triangles = Triangles;

            capMaterial = new Material(shader)
            {
                name = "jlinkdev Portal Near Clip Fix",
                hideFlags = HideFlags.HideAndDontSave
            };

            capObject = new GameObject("jlinkdev Portal Near Clip Fix")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            capObject.AddComponent<MeshFilter>().sharedMesh = capMesh;
            capRenderer = capObject.AddComponent<MeshRenderer>();
            capRenderer.sharedMaterial = capMaterial;
            capRenderer.shadowCastingMode = ShadowCastingMode.Off;
            capRenderer.receiveShadows = false;
            capRenderer.lightProbeUsage = LightProbeUsage.Off;
            capRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            capRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            capRenderer.allowOcclusionWhenDynamic = false;
            capRenderer.enabled = false;
            properties = new MaterialPropertyBlock();
            return true;
        }

        private static void SetViewportQuad(Camera camera, float distance)
        {
            Transform cameraTransform = camera.transform;
            Vertices[0] = cameraTransform.InverseTransformPoint(camera.ViewportToWorldPoint(new Vector3(0f, 0f, distance)));
            Vertices[1] = cameraTransform.InverseTransformPoint(camera.ViewportToWorldPoint(new Vector3(1f, 0f, distance)));
            Vertices[2] = cameraTransform.InverseTransformPoint(camera.ViewportToWorldPoint(new Vector3(1f, 1f, distance)));
            Vertices[3] = cameraTransform.InverseTransformPoint(camera.ViewportToWorldPoint(new Vector3(0f, 1f, distance)));
            capMesh.vertices = Vertices;
            capMesh.RecalculateBounds();
        }

        private static int FirstVisibleLayer(int cullingMask)
        {
            for (int layer = 0; layer < 32; layer++)
            {
                if ((cullingMask & (1 << layer)) != 0)
                    return layer;
            }
            return -1;
        }

        private static void DestroyObject(Object value)
        {
            if (value == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(value);
            else
                Object.DestroyImmediate(value);
        }
    }
}
