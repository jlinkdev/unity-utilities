using UnityEngine;

namespace jlinkdev.UnityUtilities.Portals.Samples
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PortalSampleRigidbodyLoop : MonoBehaviour
    {
        [SerializeField] private Vector3 launchVelocity = new Vector3(0f, 0f, 4.5f);
        [SerializeField, Min(1f)] private float resetAfterSeconds = 7f;

        private Rigidbody body;
        private Vector3 startPosition;
        private Quaternion startRotation;
        private Vector3 startScale;
        private float elapsed;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            startPosition = transform.position;
            startRotation = transform.rotation;
            startScale = transform.localScale;
            ResetBody();
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= resetAfterSeconds || transform.position.sqrMagnitude > 2500f)
                ResetBody();
        }

        private void ResetBody()
        {
            elapsed = 0f;
            transform.SetPositionAndRotation(startPosition, startRotation);
            transform.localScale = startScale;
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = launchVelocity;
#else
            body.velocity = launchVelocity;
#endif
            body.angularVelocity = new Vector3(0.4f, 0.8f, 0.2f);
        }
    }
}
