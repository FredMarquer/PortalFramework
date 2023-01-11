using UnityEngine;
using UnityEngine.Assertions;

namespace PortalFramework
{
    /// <summary>
    /// Utility class containing helper methods for character movement.
    /// </summary>
    public static class CharacterMovementHelper
    {
        /// <summary>
        /// Compute a jump height given a current up velocity and gravity.
        /// </summary>
        public static float ComputeJumpHeight(float upVelocity, float gravity)
        {
            if (upVelocity <= 0f)
            {
                return 0f;
            }

            return upVelocity * upVelocity / (2f * gravity);
        }

        /// <summary>
        /// Compute a jump velocity given a wanted jump height and gravity.
        /// </summary>
        public static float ComputeJumpVelocity(float jumpHeight, float gravity)
        {
            Assert.IsTrue(jumpHeight > 0f);

            return Mathf.Sqrt(2f * gravity * jumpHeight);
        }

        /// <summary>
        /// Project a vector on a ground plane.
        /// </summary>
        public static Vector3 ProjectVectorOnGroundPlane(Vector3 vector, Vector3 planeNormal, Vector3 characterUp)
        {
            float d = Vector3.Dot(planeNormal, characterUp);
            Assert.IsTrue(d > 0f);

            return vector + (characterUp * (Vector3.Dot(-vector, planeNormal) / d));
        }
    }
}