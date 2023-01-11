using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace PortalFramework
{
    /// <summary>
    /// Give to a character the ability to grab physical objects.
    /// </summary>
    public class CharacterGrab : MonoBehaviour, ITeleportCallback
    {
        [SerializeField]
        [Tooltip("Transform from which the grab raycast is performed.")]
        private Transform head;

        [SerializeField]
        [Tooltip("Layer mask use in the grab raycast.")]
        private LayerMask raycastLayerMask;

        [SerializeField]
        [Tooltip("Maximum mass of the objects that can be grabbed.")]
        private float baseGrabMaxMass = 1.5f;

        [SerializeField]
        [Tooltip("Distance of the grab raycast.")]
        private float baseGrabDistance = 3f;

        [SerializeField]
        [Tooltip("Distance at which the object is held.")]
        private float baseHoldDistance = 2f;

        [SerializeField]
        [Tooltip("Force at which the object is lanuched.")]
        private float baseLaunchForce = 10f;

        private float grabMaxMass;
        private float grabDistance;
        private float holdDistance;
        private float launchForce;

        private TeleportableObject teleportableObject;
        private PhysicsObject currentPhysicsObject;
        private Quaternion relativeRotation;
        private Portal heldThroughPortal;

        private List<Portal> raycastPortals = new List<Portal>();

        private Action<TeleportableObject, Portal> onPlayerTeleportDelegate;
        private Action<TeleportableObject, Portal> onObjectTeleportDelegate;

        /// <summary>
        /// Does the character hold an object.
        /// </summary>
        public bool IsHoldingObject => this.currentPhysicsObject != null;

        /// <summary>
        /// PhysicsObject held by the character.
        /// </summary>
        public PhysicsObject HeldObject => this.currentPhysicsObject;

        /// <summary>
        /// Try to grab an object if none is held.
        /// </summary>
        public void TryGrabObject()
        {
            if (!this.IsHoldingObject)
            {
                this.TryGrabInternal();
            }
        }

        /// <summary>
        /// Drop object if one is held.
        /// </summary>
        public void TryDropObject()
        {
            if (this.IsHoldingObject)
            {
                this.DropObject();
            }
        }

        /// <summary>
        /// Launch object if one is held.
        /// </summary>
        public void TryLaunchObject()
        {
            if (this.IsHoldingObject)
            {
                this.LaunchObject();
            }
        }

        void ITeleportCallback.OnTeleport(Portal portal, float newScale)
        {
            // Scales the grab parameters
            this.grabMaxMass = this.baseGrabMaxMass * Mathf.Pow(newScale, 3); // Cubic scale
            this.grabDistance = this.baseGrabDistance * newScale;
            this.holdDistance = this.baseHoldDistance * newScale;
            this.launchForce = this.baseLaunchForce * newScale;
        }

        private void Awake()
        {
            this.teleportableObject = this.GetComponent<TeleportableObject>();

            // Set the initial grab parameters
            this.grabMaxMass = this.baseGrabMaxMass;
            this.grabDistance = this.baseGrabDistance;
            this.holdDistance = this.baseHoldDistance;
            this.launchForce = this.baseLaunchForce;

            this.onPlayerTeleportDelegate = this.OnPlayerTeleport;
            this.onObjectTeleportDelegate = this.OnObjectTeleport;
        }

        private void OnDisable()
        {
            if (this.IsHoldingObject)
            {
                this.DropObject();
            }
        }

        private void FixedUpdate()
        {
            if (this.IsHoldingObject)
            {
                if (this.ShouldDropObject())
                {
                    this.DropObject();
                }
                else
                {
                    this.UpdateObject();
                }
            }
        }

        private void TryGrabInternal()
        {
            Assert.IsTrue(this.raycastPortals.Count == 0);
            Assert.IsNull(this.currentPhysicsObject);
            Assert.IsNull(this.heldThroughPortal);

            // Find the collider aimed by the camera 
            RaycastHit hit;
            if (!PortalRaycast.Raycast(this.head.position, this.head.forward, this.grabDistance, this.raycastLayerMask, 1, out hit, ref this.raycastPortals))
            {
                return;
            }

            // TODO : Handle multiple portals ?
            Assert.IsTrue(this.raycastPortals.Count <= 1);
            Portal portal = this.raycastPortals.Count > 0 ? this.raycastPortals[0] : null;
            this.raycastPortals.Clear();

            // Try grab if it's a PhysicsObject
            PhysicsObject physicsObject = hit.transform.GetComponent<PhysicsObject>();
            if (physicsObject != null)
            {
                this.TryGrabPhysicsObject(physicsObject, portal);
                return;
            }

            // Try grab if it's a Clone
            Clone clone = hit.transform.GetComponent<Clone>();
            if (clone != null)
            {
                this.TryGrabClone(clone, portal);
                return;
            }
        }

        private void TryGrabPhysicsObject(PhysicsObject physicsObject, Portal portal)
        {
            // Compute the object mass
            float objectMass = physicsObject.Rigidbody.mass;
            if (portal != null)
            {
                float portalScaleRatio = portal.DestinationPortal.GetDestinationScaleRatio();
                objectMass *= Mathf.Pow(portalScaleRatio, 3);
            }

            // Grab the object if it's not to heavy
            if (objectMass <= this.grabMaxMass)
            {
                this.GrabObject(physicsObject, portal);
            }
        }

        private void TryGrabClone(Clone clone, Portal portal)
        {
            PhysicsObject physicsObject = clone.TeleportableObject.GetComponent<PhysicsObject>();
            if (physicsObject == null)
            {
                // Not a grabbable object
                return;
            }

            // Compute the object mass
            float objectMass = physicsObject.Rigidbody.mass;
            Portal objectPortal = physicsObject.TeleportableObject.CurrentPortal;
            if (portal == null)
            {
                float portalScaleRatio = objectPortal.GetDestinationScaleRatio();
                objectMass *= Mathf.Pow(portalScaleRatio, 3);
            }
            else if (portal != objectPortal)
            {
                float portalScaleRatio = portal.DestinationPortal.GetDestinationScaleRatio();
                float objectPortalScaleRatio = objectPortal.GetDestinationScaleRatio();
                objectMass *= Mathf.Pow(portalScaleRatio, 3) * Mathf.Pow(objectPortalScaleRatio, 3);
            }

            // Grab the object if it's not to heavy
            if (objectMass <= this.grabMaxMass)
            {
                if (portal != null)
                {
                    if (portal == physicsObject.TeleportableObject.CurrentPortal)
                    {
                        portal = null;
                    }
                    else
                    {
                        // The object is on the other side of another portal.
                        // We don't handle the grab through multiple portals for now.
                        return;
                    }
                }
                else
                {
                    portal = physicsObject.TeleportableObject.CurrentPortal.DestinationPortal;
                }

                this.GrabObject(physicsObject, portal);
            }
        }

        private void GrabObject(PhysicsObject physicsObject, Portal heldThroughPortal)
        {
            Assert.IsNull(this.currentPhysicsObject);
            Assert.IsNull(this.heldThroughPortal);
            Assert.IsNotNull(physicsObject);

            // Set the held object
            this.currentPhysicsObject = physicsObject;
            this.currentPhysicsObject.EnableGravity(false);
            this.currentPhysicsObject.TeleportableObject.RegisterOnTeleport(this.onObjectTeleportDelegate);
            this.teleportableObject.RegisterOnTeleport(this.onPlayerTeleportDelegate);

            this.heldThroughPortal = heldThroughPortal;

            // Compute the relative rotation
            Quaternion objectRotation = this.currentPhysicsObject.transform.rotation;

            if (this.heldThroughPortal != null)
            {
                objectRotation = this.heldThroughPortal.DestinationPortal.RotationThroughPortal(objectRotation);
            }

            this.relativeRotation = Quaternion.Inverse(this.head.rotation) * objectRotation;
        }

        private void UpdateObject()
        {
            Assert.IsNotNull(this.currentPhysicsObject);

            // Compute the target position and rotation
            Vector3 positionTarget = this.head.position + (this.head.forward * this.holdDistance);
            Quaternion rotationTarget = this.head.rotation * this.relativeRotation;

            if (this.heldThroughPortal != null)
            {
                // TODO : Handle multiple portals ?
                positionTarget = this.heldThroughPortal.PositionThroughPortal(positionTarget);
                rotationTarget = this.heldThroughPortal.RotationThroughPortal(rotationTarget);
            }

            // Update the velocity and rotation
            this.currentPhysicsObject.Rigidbody.velocity = (positionTarget - this.currentPhysicsObject.transform.position) * 10f;
            this.currentPhysicsObject.Rigidbody.angularVelocity = Vector3.zero;
            this.currentPhysicsObject.transform.rotation = Quaternion.Lerp(this.currentPhysicsObject.transform.rotation, rotationTarget, Time.deltaTime * 10f);
            // TODO : Should we use the angular velocity to rotate the object ?
        }

        private void DropObject()
        {
            Assert.IsNotNull(this.currentPhysicsObject);

            // Limit the velocity of the object
            if (this.currentPhysicsObject.Rigidbody.velocity.magnitude > this.launchForce)
            {
                this.currentPhysicsObject.Rigidbody.velocity = this.currentPhysicsObject.Rigidbody.velocity.normalized * this.launchForce;
            }

            // Unset the held object
            this.teleportableObject.UnregisterOnTeleport(this.onPlayerTeleportDelegate);
            this.currentPhysicsObject.TeleportableObject.UnregisterOnTeleport(this.onObjectTeleportDelegate);
            this.currentPhysicsObject.EnableGravity(true);
            this.currentPhysicsObject = null;
            this.heldThroughPortal = null;
        }

        private void LaunchObject()
        {
            Assert.IsNotNull(this.currentPhysicsObject);

            // Compute the launch direction
            Vector3 direction = this.head.forward;
            if (this.heldThroughPortal)
            {
                // TODO : Handle multiple portals ?
                direction = this.heldThroughPortal.DirectionThroughPortal(direction);
            }

            // Add the launch force to the object
            Vector3 force = direction * this.launchForce;
            this.currentPhysicsObject.Rigidbody.AddForce(force, ForceMode.VelocityChange);

            // Let the object go
            this.DropObject();
        }

        private bool ShouldDropObject()
        {
            Assert.IsNotNull(this.currentPhysicsObject);

            if (this.heldThroughPortal == null)
            {
                return false;
            }

            // Check if the object is held thourgh a portal but the line of sight is outside the portal frame
            // TODO : Handle multiple portals ?
            Vector3 objectPosition = this.heldThroughPortal.DestinationPortal.PositionThroughPortal(this.currentPhysicsObject.transform.position);
            Vector3 objectDirecion = objectPosition - this.transform.position;

            Ray ray = new Ray(this.transform.position, objectDirecion);
            return !this.heldThroughPortal.RaycastRecieverCollider.Raycast(ray, out _, this.grabDistance * 2f);
        }

        private void OnPlayerTeleport(TeleportableObject teleportableObject, Portal portal)
        {
            // Update the portal through which the object is held
            if (this.heldThroughPortal == null)
            {
                this.heldThroughPortal = portal.DestinationPortal;
            }
            else if (portal == this.heldThroughPortal)
            {
                this.heldThroughPortal = null;
            }
            else
            {
                // There is multiple portals between the character and the object. 
                // We don't handle the grab through multiple portals for now.
                this.DropObject();
            }
        }

        private void OnObjectTeleport(TeleportableObject teleportableObject, Portal portal)
        {
            // Update the portal through which the object is held
            if (this.heldThroughPortal == null)
            {
                this.heldThroughPortal = portal;
            }
            else if (portal == this.heldThroughPortal.DestinationPortal)
            {
                this.heldThroughPortal = null;
            }
            else
            {
                // There is multiple portals between the character and the object. 
                // We don't handle the grab through multiple portals for now.
                this.DropObject();
            }
        }
    }
}
