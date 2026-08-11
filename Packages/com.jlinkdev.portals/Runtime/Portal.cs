using System.Collections.Generic;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Portals
{
    [DisallowMultipleComponent]
    [AddComponentMenu("jlinkdev/Portals/Portal")]
    public sealed class Portal : MonoBehaviour
    {
        private static readonly List<Portal> ActivePortalsInternal = new List<Portal>();
        private readonly HashSet<PortalTraveller> backsideTravellers = new HashSet<PortalTraveller>();

        [Header("Pair")]
        [SerializeField] private Portal linkedPortal;
        [Header("Scene references")]
        [SerializeField] private Renderer surfaceRenderer;
        [SerializeField] private Collider traversalTrigger;
        [Header("Behavior")]
        [SerializeField] private PortalRenderSettings renderSettings;
        [SerializeField] private bool traversalEnabled = true;
        [SerializeField] private bool scaleTravellers = true;
        [SerializeField] private bool drawDebugGizmos = true;

        private MaterialPropertyBlock surfaceProperties;

        public Portal LinkedPortal { get => linkedPortal; set => linkedPortal = value; }
        public Renderer SurfaceRenderer { get => surfaceRenderer; set => surfaceRenderer = value; }
        public Collider TraversalTrigger { get => traversalTrigger; set => traversalTrigger = value; }
        public PortalRenderSettings RenderSettings { get => renderSettings; set => renderSettings = value; }
        public bool TraversalEnabled { get => traversalEnabled; set => traversalEnabled = value; }
        public bool ScaleTravellers { get => scaleTravellers; set => scaleTravellers = value; }
        public bool IsLinked => linkedPortal != null && linkedPortal != this;
        public static IReadOnlyList<Portal> ActivePortals => ActivePortalsInternal;

        internal int RecursionLimit => renderSettings != null ? renderSettings.RecursionLimit : PortalRenderSettings.DefaultRecursionLimit;
        internal float RenderScale => renderSettings != null ? renderSettings.RenderScale : PortalRenderSettings.DefaultRenderScale;
        internal float NearClipOffset => renderSettings != null ? renderSettings.NearClipOffset : PortalRenderSettings.DefaultNearClipOffset;
        internal LayerMask CullingMask => renderSettings != null ? renderSettings.CullingMask : ~0;
        internal bool RenderShadows => renderSettings == null || renderSettings.RenderShadows;
        internal bool UseHdr => renderSettings == null || renderSettings.UseHdr;
        internal bool RenderInSceneView => renderSettings != null && renderSettings.RenderInSceneView;

        private void Reset()
        {
            surfaceRenderer = GetComponentInChildren<Renderer>();
            traversalTrigger = GetComponent<Collider>();
            if (traversalTrigger != null)
                traversalTrigger.isTrigger = true;
        }

        private void OnEnable()
        {
            if (!ActivePortalsInternal.Contains(this))
                ActivePortalsInternal.Add(this);

            PortalRenderSystem.EnsureInitialized();
        }

        private void OnDisable()
        {
            ActivePortalsInternal.Remove(this);
            backsideTravellers.Clear();
            PortalRenderSystem.Release(this);
        }

        private void OnValidate()
        {
            if (traversalTrigger != null)
                traversalTrigger.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!traversalEnabled || !IsLinked)
                return;

            PortalTraveller traveller = other.GetComponentInParent<PortalTraveller>();
            if (traveller == null)
                return;

            if (!IsInFrontOfPortal(traveller.transform.position))
            {
                backsideTravellers.Add(traveller);
                return;
            }

            backsideTravellers.Remove(traveller);
            traveller.EnterPortal(this);
        }

        private void OnTriggerStay(Collider other)
        {
            if (!traversalEnabled || !IsLinked)
                return;

            PortalTraveller traveller = other.GetComponentInParent<PortalTraveller>();
            if (traveller == null || backsideTravellers.Contains(traveller) || !IsInFrontOfPortal(traveller.transform.position))
                return;

            traveller.EnterPortal(this);
        }

        private void OnTriggerExit(Collider other)
        {
            PortalTraveller traveller = other.GetComponentInParent<PortalTraveller>();
            if (traveller == null)
                return;

            backsideTravellers.Remove(traveller);
            traveller.ExitPortal(this);
        }

        public Matrix4x4 MapMatrix(Matrix4x4 matrix)
        {
            return IsLinked ? PortalMath.MapMatrix(transform, linkedPortal.transform, matrix) : matrix;
        }

        public Vector3 MapPoint(Vector3 point)
        {
            return IsLinked ? PortalMath.MapPoint(transform, linkedPortal.transform, point) : point;
        }

        public Vector3 MapDirection(Vector3 direction)
        {
            return IsLinked ? PortalMath.MapDirection(transform, linkedPortal.transform, direction) : direction;
        }

        public Quaternion MapRotation(Quaternion rotation)
        {
            return IsLinked ? PortalMath.MapRotation(transform, linkedPortal.transform, rotation) : rotation;
        }

        internal bool IsVisibleFrom(Camera camera)
        {
            if (surfaceRenderer == null || !surfaceRenderer.enabled || !surfaceRenderer.gameObject.activeInHierarchy)
                return false;
            if (!IsInFrontOfPortal(camera.transform.position))
                return false;

            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
            return GeometryUtility.TestPlanesAABB(planes, surfaceRenderer.bounds);
        }

        private bool IsInFrontOfPortal(Vector3 position)
        {
            return PortalMath.SignedDistance(transform, position) > 0f;
        }

        internal void SetViewTexture(Texture texture, bool showRecursionEnd = false)
        {
            if (surfaceRenderer == null)
                return;

            surfaceProperties ??= new MaterialPropertyBlock();
            surfaceRenderer.GetPropertyBlock(surfaceProperties);
            surfaceProperties.SetTexture(PortalShaderProperties.PortalTexture, texture);
            surfaceProperties.SetFloat(PortalShaderProperties.PortalTerminal, showRecursionEnd ? 1f : 0f);
            surfaceProperties.SetColor(
                PortalShaderProperties.TerminalColor,
                renderSettings != null ? renderSettings.RecursionEndColor : PortalRenderSettings.DefaultRecursionEndColor);
            surfaceProperties.SetColor(
                PortalShaderProperties.TerminalGlowColor,
                renderSettings != null ? renderSettings.RecursionEndGlowColor : PortalRenderSettings.DefaultRecursionEndGlowColor);
            surfaceProperties.SetFloat(
                PortalShaderProperties.TerminalGlowIntensity,
                renderSettings != null ? renderSettings.RecursionEndGlowIntensity : PortalRenderSettings.DefaultRecursionEndGlowIntensity);
            surfaceRenderer.SetPropertyBlock(surfaceProperties);
        }

        private void OnDrawGizmos()
        {
            if (!drawDebugGizmos)
                return;

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = IsLinked ? new Color(0.15f, 0.85f, 1f, 0.9f) : new Color(1f, 0.35f, 0.2f, 0.9f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(1f, 2f, 0.05f));
            Gizmos.DrawRay(Vector3.zero, Vector3.forward * 0.75f);

            if (IsLinked)
            {
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.color = new Color(0.15f, 0.85f, 1f, 0.35f);
                Gizmos.DrawLine(transform.position, linkedPortal.transform.position);
            }
        }
    }
}
