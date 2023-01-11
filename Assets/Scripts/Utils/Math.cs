using UnityEngine;

namespace PortalFramework
{
    /// <summary>
    /// Utility class containing some math methods.
    /// </summary>
    public static class Math
    {
        /// <summary>
        /// Return the max between 4 values.
        /// </summary>
        public static float Max4f(float a, float b, float c, float d)
        {
            float e = a > b ? a : b;
            float f = c > d ? c : d;
            return e > f ? e : f;
        }

        /// <summary>
        /// Return the min between 4 values.
        /// </summary>
        public static float Min4f(float a, float b, float c, float d)
        {
            float e = a < b ? a : b;
            float f = c < d ? c : d;
            return e < f ? e : f;
        }

        /// <summary>
        /// Return the singed distance between a plane and a point.
        /// </summary>
        public static float SignedDistancePlanePoint(Vector3 planeNormal, Vector3 planePoint, Vector3 point)
        {
            return Vector3.Dot(planeNormal, point - planePoint);
        }
    }
}