using jlinkdev.UnityUtilities.Beams;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams.Samples
{
    public sealed class BeamKitDemoController : MonoBehaviour
    {
        [SerializeField] private Transform[] movingTargets = new Transform[0];
        [SerializeField] private BeamPulseDriver[] pulseDrivers = new BeamPulseDriver[0];
        [SerializeField] private BeamPhysicsContacts[] contactProbes = new BeamPhysicsContacts[0];
        [SerializeField, Min(0f)] private float movementAmplitude = 0.65f;
        [SerializeField, Min(0f)] private float movementSpeed = 0.75f;
        [SerializeField, Min(0.1f)] private float autoPulseInterval = 2f;

        private Vector3[] startingPositions;
        private float nextPulseAt;

        private void Start()
        {
            startingPositions = new Vector3[movingTargets.Length];
            for (int i = 0; i < movingTargets.Length; i++)
                startingPositions[i] = movingTargets[i] != null ? movingTargets[i].position : Vector3.zero;
            nextPulseAt = Time.unscaledTime + 0.5f;
        }

        private void Update()
        {
            if (startingPositions == null || startingPositions.Length != movingTargets.Length)
                Start();
            for (int i = 0; i < movingTargets.Length; i++)
            {
                if (movingTargets[i] == null)
                    continue;
                Vector3 position = startingPositions[i];
                position.y += Mathf.Sin(Time.time * movementSpeed + i * 1.37f) * movementAmplitude;
                position.x += Mathf.Cos(Time.time * movementSpeed * 0.63f + i) * movementAmplitude * 0.3f;
                movingTargets[i].position = position;
            }

            if (Time.unscaledTime >= nextPulseAt)
            {
                TriggerPulses();
                nextPulseAt = Time.unscaledTime + autoPulseInterval;
            }
        }

        private void TriggerPulses()
        {
            for (int i = 0; i < pulseDrivers.Length; i++)
                pulseDrivers[i]?.Trigger();
        }

        private void OnGUI()
        {
            int contacts = 0;
            for (int i = 0; i < contactProbes.Length; i++)
            {
                if (contactProbes[i] != null)
                    contacts += contactProbes[i].ContactCount;
            }

            GUI.Box(new Rect(18f, 18f, 330f, 84f), "jlinkdev Beams - Beam Kit Demo");
            GUI.Label(new Rect(34f, 48f, 300f, 22f), "Shader pulses trigger automatically");
            GUI.Label(new Rect(34f, 70f, 300f, 22f), $"Neutral physics contacts: {contacts}");
        }
    }
}
