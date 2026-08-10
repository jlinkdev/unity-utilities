using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace jlinkdev.UnityUtilities.Portals
{
    [DisallowMultipleComponent]
    [AddComponentMenu("jlinkdev/Portals/Portal Traveller")]
    public sealed class PortalTraveller : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private bool createTransitionClone = true;
        [SerializeField] private bool enableClipping = true;
        [SerializeField, Min(0f)] private float crossingEpsilon = 0.001f;

        private readonly List<Renderer> sourceRenderers = new List<Renderer>();
        private readonly List<Renderer> cloneRenderers = new List<Renderer>();
        private readonly List<Transform> sourceTransforms = new List<Transform>();
        private readonly List<Transform> cloneTransforms = new List<Transform>();
        private MaterialPropertyBlock propertyBlock;

        private Portal activePortal;
        private GameObject transitionClone;
        private Rigidbody body;
        private CharacterController characterController;
        private IPortalVelocityProvider velocityProvider;
        private float previousDistance;
        private int lastTeleportFrame = -1;

        public event Action<Portal, Portal> Teleported;

        public Transform VisualRoot => visualRoot != null ? visualRoot : transform;
        public Portal ActivePortal => activePortal;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            body = GetComponent<Rigidbody>();
            characterController = GetComponent<CharacterController>();
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IPortalVelocityProvider provider)
                {
                    velocityProvider = provider;
                    break;
                }
            }

            CacheSourceRenderers();
        }

        private void LateUpdate()
        {
            if (activePortal == null || !activePortal.IsLinked)
                return;

            UpdateTransitionVisuals();

            float currentDistance = PortalMath.SignedDistance(activePortal.transform, transform.position);
            bool crossed = Mathf.Abs(previousDistance) > crossingEpsilon &&
                           Mathf.Abs(currentDistance) > crossingEpsilon &&
                           Mathf.Sign(previousDistance) != Mathf.Sign(currentDistance);

            if (crossed && lastTeleportFrame != Time.frameCount)
                Teleport(activePortal, activePortal.LinkedPortal);
            else
                previousDistance = currentDistance;
        }

        private void OnDisable()
        {
            EndTransition();
        }

        private void OnDestroy()
        {
            if (transitionClone == null)
                return;

            if (Application.isPlaying)
                Destroy(transitionClone);
            else
                DestroyImmediate(transitionClone);
        }

        internal void EnterPortal(Portal portal)
        {
            if (portal == null || !portal.IsLinked || activePortal == portal)
                return;

            if (activePortal != null && Time.frameCount - lastTeleportFrame <= 1)
                return;

            activePortal = portal;
            previousDistance = PortalMath.SignedDistance(portal.transform, transform.position);
            BeginTransition();
        }

        internal void ExitPortal(Portal portal)
        {
            if (activePortal != portal || Time.frameCount - lastTeleportFrame <= 1)
                return;

            activePortal = null;
            EndTransition();
        }

        public void Teleport(Portal entry, Portal exit)
        {
            if (entry == null || exit == null || entry == exit)
                throw new ArgumentException("Portal traversal requires two different portals.");

            Vector3 mappedPosition = PortalMath.MapPoint(entry.transform, exit.transform, transform.position);
            Quaternion mappedRotation = PortalMath.MapRotation(entry.transform, exit.transform, transform.rotation);
            float scaleRatio = entry.ScaleTravellers ? PortalMath.UniformScaleRatio(entry.transform, exit.transform) : 1f;

            Vector3 mappedVelocity = Vector3.zero;
            Vector3 mappedAngularVelocity = Vector3.zero;
            bool hasBody = body != null;
            if (hasBody)
            {
#if UNITY_6000_0_OR_NEWER
                mappedVelocity = PortalMath.MapDirection(entry.transform, exit.transform, body.linearVelocity) * scaleRatio;
#else
                mappedVelocity = PortalMath.MapDirection(entry.transform, exit.transform, body.velocity) * scaleRatio;
#endif
                mappedAngularVelocity = PortalMath.MapDirection(entry.transform, exit.transform, body.angularVelocity);
            }
            else if (velocityProvider != null)
            {
                mappedVelocity = PortalMath.MapDirection(entry.transform, exit.transform, velocityProvider.PortalVelocity) * scaleRatio;
            }

            bool controllerWasEnabled = characterController != null && characterController.enabled;
            if (controllerWasEnabled)
                characterController.enabled = false;

            transform.SetPositionAndRotation(mappedPosition, mappedRotation);
            if (!Mathf.Approximately(scaleRatio, 1f))
                transform.localScale *= scaleRatio;

            if (controllerWasEnabled)
                characterController.enabled = true;

            if (hasBody)
            {
                body.position = mappedPosition;
                body.rotation = mappedRotation;
#if UNITY_6000_0_OR_NEWER
                body.linearVelocity = mappedVelocity;
#else
                body.velocity = mappedVelocity;
#endif
                body.angularVelocity = mappedAngularVelocity;
            }
            else if (velocityProvider != null)
            {
                velocityProvider.PortalVelocity = mappedVelocity;
            }

            Physics.SyncTransforms();
            lastTeleportFrame = Time.frameCount;
            activePortal = exit;
            previousDistance = PortalMath.SignedDistance(exit.transform, transform.position);
            UpdateTransitionVisuals();
            Teleported?.Invoke(entry, exit);
        }

        private void BeginTransition()
        {
            CacheSourceRenderers();
            if (createTransitionClone && transitionClone == null)
                BuildTransitionClone();

            if (transitionClone != null)
                transitionClone.SetActive(true);
            UpdateTransitionVisuals();
        }

        private void EndTransition()
        {
            SetClipPlane(sourceRenderers, Vector4.zero, false);
            SetClipPlane(cloneRenderers, Vector4.zero, false);
            if (transitionClone != null)
                transitionClone.SetActive(false);
        }

        private void UpdateTransitionVisuals()
        {
            if (activePortal == null || !activePortal.IsLinked)
                return;

            if (transitionClone != null && transitionClone.activeSelf)
            {
                for (int i = 1; i < sourceTransforms.Count && i < cloneTransforms.Count; i++)
                {
                    cloneTransforms[i].SetLocalPositionAndRotation(sourceTransforms[i].localPosition, sourceTransforms[i].localRotation);
                    cloneTransforms[i].localScale = sourceTransforms[i].localScale;
                }

                Transform sourceRoot = VisualRoot;
                Portal exit = activePortal.LinkedPortal;
                transitionClone.transform.SetPositionAndRotation(
                    activePortal.MapPoint(sourceRoot.position),
                    activePortal.MapRotation(sourceRoot.rotation));
                float ratio = activePortal.ScaleTravellers
                    ? PortalMath.UniformScaleRatio(activePortal.transform, exit.transform)
                    : 1f;
                transitionClone.transform.localScale = sourceRoot.lossyScale * ratio;
            }

            if (!enableClipping)
                return;

            float side = Mathf.Sign(PortalMath.SignedDistance(activePortal.transform, transform.position));
            if (Mathf.Approximately(side, 0f))
                side = 1f;

            Vector3 sourceNormal = activePortal.transform.forward * side;
            Vector4 sourcePlane = WorldPlane(activePortal.transform.position, sourceNormal);
            Portal destination = activePortal.LinkedPortal;
            Vector3 destinationNormal = -destination.transform.forward * side;
            Vector4 destinationPlane = WorldPlane(destination.transform.position, destinationNormal);
            SetClipPlane(sourceRenderers, sourcePlane, true);
            SetClipPlane(cloneRenderers, destinationPlane, true);
        }

        private void CacheSourceRenderers()
        {
            sourceRenderers.Clear();
            VisualRoot.GetComponentsInChildren(true, sourceRenderers);
        }

        private void BuildTransitionClone()
        {
            sourceTransforms.Clear();
            cloneTransforms.Clear();
            cloneRenderers.Clear();

            Transform sourceRoot = VisualRoot;
            transitionClone = new GameObject($"{name} (Portal Transition)");
            transitionClone.hideFlags = HideFlags.DontSave;
            CopyHierarchy(sourceRoot, transitionClone.transform, true);
        }

        private void CopyHierarchy(Transform source, Transform clone, bool isRoot)
        {
            if (!isRoot)
            {
                clone.localPosition = source.localPosition;
                clone.localRotation = source.localRotation;
                clone.localScale = source.localScale;
            }

            clone.gameObject.layer = source.gameObject.layer;
            sourceTransforms.Add(source);
            cloneTransforms.Add(clone);

            MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
            MeshRenderer sourceMeshRenderer = source.GetComponent<MeshRenderer>();
            if (sourceFilter != null && sourceMeshRenderer != null)
            {
                MeshFilter targetFilter = clone.gameObject.AddComponent<MeshFilter>();
                targetFilter.sharedMesh = sourceFilter.sharedMesh;
                MeshRenderer targetRenderer = clone.gameObject.AddComponent<MeshRenderer>();
                CopyRenderer(sourceMeshRenderer, targetRenderer);
                cloneRenderers.Add(targetRenderer);
            }

            SkinnedMeshRenderer sourceSkinned = source.GetComponent<SkinnedMeshRenderer>();
            if (sourceSkinned != null)
            {
                SkinnedMeshRenderer targetSkinned = clone.gameObject.AddComponent<SkinnedMeshRenderer>();
                CopyRenderer(sourceSkinned, targetSkinned);
                targetSkinned.sharedMesh = sourceSkinned.sharedMesh;
                targetSkinned.localBounds = sourceSkinned.localBounds;
                targetSkinned.updateWhenOffscreen = true;
                cloneRenderers.Add(targetSkinned);
            }

            for (int i = 0; i < source.childCount; i++)
            {
                Transform sourceChild = source.GetChild(i);
                GameObject cloneChild = new GameObject(sourceChild.name);
                cloneChild.transform.SetParent(clone, false);
                CopyHierarchy(sourceChild, cloneChild.transform, false);
            }

            if (isRoot)
                RemapSkinnedMeshes();
        }

        private void RemapSkinnedMeshes()
        {
            for (int i = 0; i < sourceRenderers.Count; i++)
            {
                if (!(sourceRenderers[i] is SkinnedMeshRenderer sourceSkinned))
                    continue;

                SkinnedMeshRenderer target = FindCloneRenderer(sourceSkinned.transform) as SkinnedMeshRenderer;
                if (target == null)
                    continue;

                Transform[] mappedBones = new Transform[sourceSkinned.bones.Length];
                for (int boneIndex = 0; boneIndex < sourceSkinned.bones.Length; boneIndex++)
                    mappedBones[boneIndex] = FindCloneTransform(sourceSkinned.bones[boneIndex]);
                target.bones = mappedBones;
                target.rootBone = FindCloneTransform(sourceSkinned.rootBone);
            }
        }

        private Renderer FindCloneRenderer(Transform source)
        {
            Transform mapped = FindCloneTransform(source);
            return mapped != null ? mapped.GetComponent<Renderer>() : null;
        }

        private Transform FindCloneTransform(Transform source)
        {
            int index = sourceTransforms.IndexOf(source);
            return index >= 0 && index < cloneTransforms.Count ? cloneTransforms[index] : null;
        }

        private static void CopyRenderer(Renderer source, Renderer target)
        {
            target.sharedMaterials = source.sharedMaterials;
            target.shadowCastingMode = source.shadowCastingMode;
            target.receiveShadows = source.receiveShadows;
            target.lightProbeUsage = source.lightProbeUsage;
            target.reflectionProbeUsage = source.reflectionProbeUsage;
            target.renderingLayerMask = source.renderingLayerMask;
            target.sortingLayerID = source.sortingLayerID;
            target.sortingOrder = source.sortingOrder;
        }

        private void SetClipPlane(List<Renderer> renderers, Vector4 plane, bool enabled)
        {
            propertyBlock ??= new MaterialPropertyBlock();
            for (int i = 0; i < renderers.Count; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetVector(PortalShaderProperties.ClipPlane, plane);
                propertyBlock.SetFloat(PortalShaderProperties.ClipEnabled, enabled ? 1f : 0f);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        private static Vector4 WorldPlane(Vector3 point, Vector3 normal)
        {
            normal.Normalize();
            return new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(normal, point));
        }
    }
}
