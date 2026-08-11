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
            {
                RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
                RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            }

            foreach (PortalRenderState state in States.Values)
                ReleaseTextures(state);
            States.Clear();
            PortalNearClipFix.Dispose();
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
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
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

            // The cap belongs exclusively to the final gameplay-camera pass. It
            // must never appear in a recursive portal camera or in Scene view.
            PortalNearClipFix.Disable();

            PortalSnapshot.Clear();
            IReadOnlyList<Portal> active = Portal.ActivePortals;
            for (int i = 0; i < active.Count; i++)
                PortalSnapshot.Add(active[i]);

            Portal nearClipPortal = gameplayCamera ? FindNearClipPortal(camera) : null;

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
                    if (portal != nearClipPortal && !portal.IsVisibleFrom(camera))
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

                if (nearClipPortal != null &&
                    States.TryGetValue(nearClipPortal, out PortalRenderState nearState) &&
                    nearState.textures != null && nearState.textures.Length > 0)
                {
                    PortalNearClipFix.Show(camera, nearClipPortal, nearState.textures[0]);
                }
            }
            finally
            {
                rendering = false;
            }
        }

        private static void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera != null && camera.cameraType == CameraType.Game && camera.CompareTag("MainCamera"))
                PortalNearClipFix.Disable();
        }

        private static Portal FindNearClipPortal(Camera camera)
        {
            Portal closest = null;
            float closestDistance = float.PositiveInfinity;
            float activationDistance = Mathf.Max(camera.nearClipPlane * 1.5f, 0.05f);

            for (int i = 0; i < PortalSnapshot.Count; i++)
            {
                Portal portal = PortalSnapshot[i];
                if (portal == null || !portal.isActiveAndEnabled || !portal.IsLinked)
                    continue;

                Renderer surface = portal.SurfaceRenderer;
                if (surface == null || !surface.enabled || !surface.gameObject.activeInHierarchy)
                    continue;

                float distance = PortalMath.SignedDistance(portal.transform, camera.transform.position);
                if (distance <= 0f || distance > activationDistance || distance >= closestDistance)
                    continue;

                // Only arm the cap for the aperture the camera is actually
                // approaching. Nearby off-screen portals must not steal the one
                // main-camera cap from the portal being crossed.
                Vector3 projectedPoint = camera.transform.position - portal.transform.forward * distance;
                Vector3 projectedLocal = surface.transform.InverseTransformPoint(projectedPoint);
                Bounds localBounds = surface.localBounds;
                if (projectedLocal.x < localBounds.min.x || projectedLocal.x > localBounds.max.x ||
                    projectedLocal.y < localBounds.min.y || projectedLocal.y > localBounds.max.y)
                {
                    continue;
                }

                closest = portal;
                closestDistance = distance;
            }

            return closest;
        }

        private static void RenderPortal(ScriptableRenderContext context, Camera sourceCamera, Portal sourcePortal)
        {
            EnsurePortalCamera();
            int depthCount = Mathf.Max(1, sourcePortal.RecursionLimit + 1);
            PortalRenderState state = GetState(sourcePortal, sourceCamera, depthCount);
            Matrix4x4[] poses = new Matrix4x4[depthCount];

            Matrix4x4 sourcePose = sourceCamera.transform.localToWorldMatrix;
            Portal exitPortal = sourcePortal.LinkedPortal;
            for (int depth = 0; depth < depthCount; depth++)
            {
                // Every nested view crosses the same visible portal pair again.
                // Alternating the pair direction would apply the inverse transform
                // on every second pass and collapse the recursion back toward the
                // source camera instead of advancing the view through the chain.
                poses[depth] = PortalMath.MapMatrixRepeated(
                    sourcePortal.transform,
                    exitPortal.transform,
                    sourcePose,
                    depth + 1);
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

                Vector4 clipPlane = PortalMath.CameraSpacePlane(
                    portalCamera,
                    exitPortal.transform.position,
                    exitPortal.transform.forward,
                    sourcePortal.NearClipOffset);
                clipPlane = PortalMath.StabilizeCameraSpacePlane(
                    clipPlane,
                    Mathf.Max(sourceCamera.nearClipPlane * 0.5f, 0.01f));
                portalCamera.projectionMatrix = sourceCamera.CalculateObliqueMatrix(clipPlane);

                bool deepestPass = depth + 1 >= depthCount;
                sourcePortal.SetViewTexture(
                    deepestPass ? Texture2D.blackTexture : state.textures[depth + 1],
                    deepestPass);

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
