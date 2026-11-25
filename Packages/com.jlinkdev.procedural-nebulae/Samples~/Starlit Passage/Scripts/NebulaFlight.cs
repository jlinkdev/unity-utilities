using UnityEngine;

namespace jlinkdev.ProceduralNebulae.Samples
{
    [RequireComponent(typeof(Camera))]
    public sealed class NebulaFlight : MonoBehaviour
    {
        [Min(1)] public float loopSeconds = 64;
        public Transform star;
        Vector3 start;
        float elapsed;

        void Awake() { start = transform.localPosition; }

        void Update()
        {
            elapsed += Time.deltaTime;
            float angle = elapsed * (2*Mathf.PI/Mathf.Max(1,loopSeconds));
            transform.localPosition = start + new Vector3(17*Mathf.Sin(angle),
                5*Mathf.Sin(angle*2),12*(1-Mathf.Cos(angle)));
            if (star)
            {
                Vector3 aim = star.position + new Vector3(-17+5*Mathf.Sin(angle),12,0);
                transform.rotation = Quaternion.LookRotation(aim-transform.position,Vector3.up);
            }
        }
    }
}
