using UnityEngine;

namespace jlinkdev.UnityUtilities.WorldScanning.Samples
{
    public sealed class WorldScanDemoCamera : MonoBehaviour
    {
        [SerializeField] private Transform focus;
        [SerializeField] private float angularSpeed = 5f;
        [SerializeField] private float verticalBob = 0.35f;

        private Vector3 baseLocalPosition;

        private void Start()
        {
            baseLocalPosition = transform.localPosition;
        }

        private void LateUpdate()
        {
            if (focus == null)
                return;
            transform.RotateAround(focus.position, Vector3.up, angularSpeed * Time.deltaTime);
            Vector3 position = transform.position;
            position.y = baseLocalPosition.y + Mathf.Sin(Time.time * 0.35f) * verticalBob;
            transform.position = position;
            transform.LookAt(focus.position + Vector3.up * 1.5f);
        }
    }
}
