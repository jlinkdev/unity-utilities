using UnityEngine;

namespace jlinkdev.UnityUtilities.IK
{
    internal static class IKMath
    {
        public static float ClampWeight(float value)
        {
            return Mathf.Clamp01(value);
        }

        public static Vector3 ProjectOntoPlane(Vector3 vector, Vector3 planeNormal)
        {
            return vector - Vector3.Project(vector, planeNormal);
        }

        public static Quaternion SafeLookRotation(Vector3 forward, Vector3 up)
        {
            if (forward.sqrMagnitude <= 0.000001f)
            {
                return Quaternion.identity;
            }

            Vector3 safeUp = up.sqrMagnitude > 0.000001f ? up : Vector3.up;
            return Quaternion.LookRotation(forward.normalized, safeUp.normalized);
        }

        public static float SumLengths(Vector3[] points)
        {
            float total = 0f;
            for (int i = 0; i < points.Length - 1; i++)
            {
                total += Vector3.Distance(points[i], points[i + 1]);
            }

            return total;
        }
    }
}
