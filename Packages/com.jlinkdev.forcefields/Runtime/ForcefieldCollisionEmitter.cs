using UnityEngine;

namespace jlinkdev.UnityUtilities.Forcefields
{
    /// <summary>Optional adapter that converts physics contacts into visual forcefield impacts.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("jlinkdev/Forcefields/Forcefield Collision Emitter")]
    public sealed class ForcefieldCollisionEmitter : MonoBehaviour
    {
        [SerializeField] private Forcefield forcefield;
        [SerializeField] private LayerMask layers = ~0;
        [SerializeField, Range(1, 4)] private int contactsPerCollision = 1;
        [SerializeField, Min(0f)] private float strengthMultiplier = 0.12f;
        [SerializeField, Min(0f)] private float minimumStrength = 0.15f;
        [SerializeField, Min(0f)] private float maximumStrength = 2f;
        [SerializeField, Min(0f)] private float impactRadius = 0.04f;

        public Forcefield Target
        {
            get => forcefield;
            set => forcefield = value;
        }

        private void Reset()
        {
            forcefield = GetComponentInParent<Forcefield>();
        }

        private void OnValidate()
        {
            contactsPerCollision = Mathf.Clamp(contactsPerCollision, 1, 4);
            strengthMultiplier = Mathf.Max(0f, strengthMultiplier);
            minimumStrength = Mathf.Max(0f, minimumStrength);
            maximumStrength = Mathf.Max(minimumStrength, maximumStrength);
            impactRadius = Mathf.Max(0f, impactRadius);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (forcefield == null || (layers.value & (1 << collision.gameObject.layer)) == 0)
                return;

            float strength = Mathf.Clamp(
                collision.relativeVelocity.magnitude * strengthMultiplier,
                minimumStrength,
                maximumStrength);
            int count = Mathf.Min(contactsPerCollision, collision.contactCount);
            for (int i = 0; i < count; i++)
            {
                ContactPoint contact = collision.GetContact(i);
                forcefield.AddImpact(contact.point, contact.normal, strength, impactRadius);
            }
        }
    }
}
