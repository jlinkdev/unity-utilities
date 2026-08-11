using UnityEngine;

namespace jlinkdev.UnityUtilities.WorldScanning.Samples
{
    [RequireComponent(typeof(ScanReceiver))]
    public sealed class WorldScanDemoBeacon : MonoBehaviour
    {
        [SerializeField] private Light beaconLight;
        [SerializeField] private Renderer beaconRenderer;
        [SerializeField] private Color activeColor = new Color(0.05f, 0.9f, 1f);
        [SerializeField, Min(0.05f)] private float flashDuration = 1.2f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private ScanReceiver receiver;
        private MaterialPropertyBlock propertyBlock;
        private float remaining;

        private void Awake()
        {
            receiver = GetComponent<ScanReceiver>();
            propertyBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            if (receiver == null)
                receiver = GetComponent<ScanReceiver>();
            receiver.Scanned += OnScanned;
        }

        private void OnDisable()
        {
            if (receiver != null)
                receiver.Scanned -= OnScanned;
        }

        private void Update()
        {
            remaining = Mathf.Max(0f, remaining - Time.deltaTime);
            float strength = flashDuration <= 0f ? 0f : remaining / flashDuration;
            if (beaconLight != null)
                beaconLight.intensity = strength * 8f;
            if (beaconRenderer != null)
            {
                beaconRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(EmissionColorId, activeColor * (strength * 5f));
                beaconRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void OnScanned(ScanHit hit)
        {
            remaining = flashDuration;
        }
    }
}
