using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.UnityUtilities.Portals
{
    internal static class PortalRenderSystem
    {
        private sealed class PortalRenderState
        {
            public RenderTexture[] textures;
            public int width;
            public int height;
            public bool hdr;
        }

        private static readonly Dictionary<Portal, PortalRenderState> States = new Dictionary<Portal, PortalRenderState>();
        private static readonly List<Portal> PortalSnapshot = new List<Portal>();
        private static Camera portalCamera;
        private static UniversalAdditionalCameraData portalCameraData;
        private static bool initialized;
        private static bool rendering;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            if (initialized)
                RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

            foreach (PortalRenderState state in States.Values)
                ReleaseTextures(state);
            States.Clear();
            initialized = false;
            rendering = false;
            portalCamera = null;
            portalCameraData = null;
        }

        internal static void EnsureInitialized()
        {
            if (initialized)
                return;

            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            initialized = true;
        }

        internal static void Release(Portal portal)
        {
            if (portal == null || !States.TryGetValue(portal, out PortalRenderState state))
                return;

            ReleaseTextures(state);
            States.Remove(portal);
        }

        private static void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (rendering || camera == null || camera == portalCamera)
                return;

            bool gameplayCamera = camera.cameraType == CameraType.Game && camera.CompareTag("MainCamera");
            bool sceneCamera = camera.cameraType == CameraType.SceneView;
            if (!gameplayCamera && !sceneCamera)
                return;

            PortalSnapshot.Clear();
            IReadOnlyList<Portal> active = Portal.ActivePortals;
            for (int i = 0; i < active.Count; i++)
                PortalSnapshot.Add(active[i]);

            rendering = true;
            try
            {
                for (int i = 0; i < PortalSnapshot.Count; i++)
                {
                    Portal portal = PortalSnapshot[i];
                    if (portal == null || !portal.isActiveAndEnabled || !portal.IsLinked)
                        continue;
                    if (sceneCamera && !portal.RenderInSceneView)
                        continue;
                    if (!portal.IsVisibleFrom(camera))
                        continue;

                    RenderPortal(context, camera, portal);
                }

                // Recursive passes temporarily point linked surfaces at deeper textures.
                // Restore every rendered portal's first-level view after the full camera pass.
                for (int i = 0; i < PortalSnapshot.Count; i++)
                {
                    Portal portal = PortalSnapshot[i];
                    if (portal != null && States.TryGetValue(portal, out PortalRenderState state) && state.textures != null && state.textures.Length > 0)
                        portal.SetViewTexture(state.textures[0]);
                }
            }
            finally
            {
                rendering = false;
            }
        }

        private static void RenderPortal(ScriptableRenderContext context, Camera sourceCamera, Portal sourcePortal)
        {
            EnsurePortalCamera();
            int depthCount = Mathf.Max(1, sourcePortal.RecursionLimit + 1);
            PortalRenderState state = GetState(sourcePortal, sourceCamera, depthCount);
            Matrix4x4[] poses = new Matrix4x4[depthCount];
            Portal[] entries = new Portal[depthCount];
            Portal[] exits = new Portal[depthCount];

            Matrix4x4 pose = sourceCamera.transform.localToWorldMatrix;
            Portal entry = sourcePortal;
            Portal exit = sourcePortal.LinkedPortal;
            for (int depth = 0; depth < depthCount; depth++)
            {
                pose = PortalMath.MapMatrix(entry.transform, exit.transform, pose);
                poses[depth] = pose;
                entries[depth] = entry;
                exits[depth] = exit;
                Portal swap = entry;
                entry = exit;
                exit = swap;
            }

            for (int depth = depthCount - 1; depth >= 0; depth--)
            {
                portalCamera.CopyFrom(sourceCamera);
                portalCamera.enabled = false;
                portalCamera.cameraType = CameraType.Game;
                portalCamera.cullingMask = sourcePortal.CullingMask;
                portalCamera.targetTexture = state.textures[depth];
                portalCamera.transform.SetPositionAndRotation(poses[depth].GetColumn(3), poses[depth].rotation);
                portalCameraData.renderType = CameraRenderType.Base;
                portalCameraData.renderShadows = sourcePortal.RenderShadows;
                portalCameraData.requiresColorTexture = false;
                portalCameraData.requiresDepthTexture = true;

                Portal clipPortal = exits[depth];
                Vector4 clipPlane = PortalMath.CameraSpacePlane(
                    portalCamera,
                    clipPortal.transform.position,
                    clipPortal.transform.forward,
                    sourcePortal.NearClipOffset);
                portalCamera.projectionMatrix = sourceCamera.CalculateObliqueMatrix(clipPlane);

                Portal portalVisibleInView = entries[depth];
                portalVisibleInView.SetViewTexture(depth + 1 < depthCount ? state.textures[depth + 1] : Texture2D.blackTexture);

#pragma warning disable CS0618
                UniversalRenderPipeline.RenderSingleCamera(context, portalCamera);
#pragma warning restore CS0618
            }

            sourcePortal.SetViewTexture(state.textures[0]);
            portalCamera.targetTexture = null;
        }

        private static void EnsurePortalCamera()
        {
            if (portalCamera != null)
                return;

            GameObject cameraObject = new GameObject("jlinkdev Portal Camera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            portalCamera = cameraObject.AddComponent<Camera>();
            portalCamera.enabled = false;
            portalCameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        }

        private static PortalRenderState GetState(Portal portal, Camera sourceCamera, int depthCount)
        {
            int width = Mathf.Max(64, Mathf.RoundToInt(sourceCamera.pixelWidth * portal.RenderScale));
            int height = Mathf.Max(64, Mathf.RoundToInt(sourceCamera.pixelHeight * portal.RenderScale));
            bool hdr = portal.UseHdr && sourceCamera.allowHDR;

            if (!States.TryGetValue(portal, out PortalRenderState state))
            {
                state = new PortalRenderState();
                States.Add(portal, state);
            }

            if (state.textures == null || state.textures.Length != depthCount || state.width != width || state.height != height || state.hdr != hdr)
            {
                ReleaseTextures(state);
                state.width = width;
                state.height = height;
                state.hdr = hdr;
                state.textures = new RenderTexture[depthCount];
                RenderTextureFormat format = hdr ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
                for (int i = 0; i < depthCount; i++)
                {
                    RenderTexture texture = new RenderTexture(width, height, 24, format)
                    {
                        name = $"{portal.name} Portal View {i}",
                        hideFlags = HideFlags.HideAndDontSave,
                        useMipMap = false,
                        autoGenerateMips = false
                    };
                    texture.Create();
                    state.textures[i] = texture;
                }
            }

            return state;
        }

        private static void ReleaseTextures(PortalRenderState state)
        {
            if (state?.textures == null)
                return;

            for (int i = 0; i < state.textures.Length; i++)
            {
                if (state.textures[i] == null)
                    continue;
                state.textures[i].Release();
                if (Application.isPlaying)
                    Object.Destroy(state.textures[i]);
                else
                    Object.DestroyImmediate(state.textures[i]);
            }
            state.textures = null;
        }
    }
}
