using System;
using UnityEngine;
using UnityEngine.Events;

namespace jlinkdev.UnityUtilities.WorldScanning
{
    [DisallowMultipleComponent]
    [AddComponentMenu("jlinkdev/World Scanning/Scan Receiver")]
    public sealed class ScanReceiver : MonoBehaviour
    {
        [SerializeField] private Transform samplePoint;
        [SerializeField] private UnityEvent onScanned = new UnityEvent();

        public event Action<ScanHit> Scanned;
        public ScanHit LastHit { get; private set; }
        internal Vector3 Position => samplePoint != null ? samplePoint.position : transform.position;

        private void OnEnable()
        {
            ScanSystem.RegisterReceiver(this);
        }

        private void OnDisable()
        {
            ScanSystem.UnregisterReceiver(this);
        }

        internal void Notify(in ScanHit hit)
        {
            LastHit = hit;
            onScanned.Invoke();
            Scanned?.Invoke(hit);
        }
    }
}
