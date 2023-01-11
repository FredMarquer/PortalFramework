using UnityEngine;
using UnityEngine.Assertions;

namespace PortalFramework
{
    /// <summary>
    /// Controls a rigidbody behaviour when passing through portals.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(ConstantForce))]
    public class PhysicsObject : MonoBehaviour, ITeleportCallback
    {
        [SerializeField]
        [Tooltip("Gravity applied to the physics object.")]
        private Vector3 baseGravity;

        private TeleportableObject teleportableObject;
        private new Rigidbody rigidbody;
        private new ConstantForce constantForce;

        private float baseMass;

        /// <summary>
        /// The TeleportableObject attached to this object.
        /// </summary>
        public TeleportableObject TeleportableObject => this.teleportableObject;

        /// <summary>
        /// The Rigidbody attached to this object.
        /// </summary>
        public Rigidbody Rigidbody => this.rigidbody;

        /// <summary>
        /// Allow to enable/disable the gravity on this object.
        /// </summary>
        public void EnableGravity(bool enable)
        {
            this.constantForce.enabled = enable;
        }

        void ITeleportCallback.OnTeleport(Portal portal, float newScale)
        {
            // Transform the velocity
            Vector3 newVelocity = portal.DirectionThroughPortal(this.rigidbody.velocity);
            this.rigidbody.velocity = newVelocity * portal.GetDestinationScaleRatio();
            this.rigidbody.angularVelocity = portal.DirectionThroughPortal(this.rigidbody.angularVelocity);

            // Transform the mass
            this.rigidbody.mass = this.baseMass * Mathf.Pow(newScale, 3); // Cubic scale

            // Transform the gravity
            this.baseGravity = portal.DirectionThroughPortal(this.baseGravity);
            this.constantForce.force = this.baseGravity * Mathf.Pow(newScale, 4); // 3 (for the mass cubic scale) + 1 (for the distance scale)
        }

        private void Awake()
        {
            // Retrieve the TeleportableObject component
            this.teleportableObject = this.GetComponent<TeleportableObject>();
            Assert.IsNotNull(this.teleportableObject);

            // Retrieve the Rigidbody component
            this.rigidbody = this.GetComponent<Rigidbody>();
            Assert.IsNotNull(this.rigidbody);
            this.baseMass = this.rigidbody.mass;

            // Retrieve the ConstantForce component and set the gravity
            this.constantForce = this.GetComponent<ConstantForce>();
            Assert.IsNotNull(this.constantForce);
            this.constantForce.force = this.baseGravity;
            this.constantForce.relativeForce = Vector3.zero;
            this.constantForce.torque = Vector3.zero;
            this.constantForce.relativeTorque = Vector3.zero;
        }
    }
}
