using UnityEngine;

namespace jlinkdev.UnityUtilities.Portals
{
    public static class PortalMath
    {
        private static readonly Matrix4x4 HalfTurn = Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f));

        public static Matrix4x4 MapMatrix(Transform entry, Transform exit, Matrix4x4 matrix)
        {
            return exit.localToWorldMatrix * HalfTurn * entry.worldToLocalMatrix * matrix;
        }

        public static Vector3 MapPoint(Transform entry, Transform exit, Vector3 point)
        {
            return MapMatrix(entry, exit, Matrix4x4.Translate(point)).GetColumn(3);
        }

        public static Vector3 MapDirection(Transform entry, Transform exit, Vector3 direction)
        {
            Vector3 local = entry.InverseTransformDirection(direction);
            return exit.TransformDirection(Quaternion.Euler(0f, 180f, 0f) * local);
        }

        public static Quaternion MapRotation(Transform entry, Transform exit, Quaternion rotation)
        {
            return exit.rotation * Quaternion.Euler(0f, 180f, 0f) * Quaternion.Inverse(entry.rotation) * rotation;
        }

        public static float SignedDistance(Transform portal, Vector3 point)
        {
            return Vector3.Dot(portal.forward, point - portal.position);
        }

        public static float UniformScaleRatio(Transform entry, Transform exit)
        {
            return ApertureScale(exit) / Mathf.Max(ApertureScale(entry), 0.0001f);
        }

        private static float ApertureScale(Transform portal)
        {
            Vector3 scale = portal.lossyScale;
            return Mathf.Sqrt(Mathf.Max(Mathf.Abs(scale.x * scale.y), 0.0001f));
        }

        internal static Vector4 CameraSpacePlane(Camera camera, Vector3 point, Vector3 normal, float offset)
        {
            float side = Mathf.Sign(Vector3.Dot(normal, camera.transform.position - point));
            if (Mathf.Approximately(side, 0f))
                side = 1f;

            // The oblique plane normal must point away from the portal camera so
            // the destination side is retained and the camera side is discarded.
            normal *= -side;
            point += normal * offset;
            Matrix4x4 worldToCamera = camera.worldToCameraMatrix;
            Vector3 cameraPoint = worldToCamera.MultiplyPoint(point);
            Vector3 cameraNormal = worldToCamera.MultiplyVector(normal).normalized;
            return new Vector4(cameraNormal.x, cameraNormal.y, cameraNormal.z, -Vector3.Dot(cameraPoint, cameraNormal));
        }
    }
}
