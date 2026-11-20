using UnityEngine;

namespace JLinkDev.ProceduralNebulae.SampleAuthoring
{
    [RequireComponent(typeof(Camera))]
    public sealed class NebulaCameraSlide : MonoBehaviour
    {
        [Min(0)] public float travel = 36;
        [Min(1)] public float focusDistance = 187;
        [Min(1)] public float loopSeconds = 8;

        Vector3 start;
        Quaternion orientation;
        float elapsed;

        void Awake()
        {
            start = transform.position;
            orientation = transform.rotation;
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            float fraction = .5f-.5f*Mathf.Cos(2*Mathf.PI*elapsed/Mathf.Max(1,loopSeconds));
            SetView(transform,start,orientation,travel*fraction,focusDistance);
        }

        public static void SetView(Transform cameraTransform, Vector3 start, Quaternion orientation,
                                   float offset, float focusDistance)
        {
            Vector3 target = start+orientation*Vector3.forward*focusDistance;
            Vector3 position = start+orientation*Vector3.right*offset;
            cameraTransform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position,orientation*Vector3.up));
        }
    }
}
