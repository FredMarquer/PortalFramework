using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace PortalFramework
{
    /// <summary>
    /// Controls the teleportation of an object through the portals.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class TeleportableObject : MonoBehaviour
    {
        private static Transform clonesParent;

        [SerializeField]
        [Tooltip("Transform used to test the position of the object when passing through a portal.")]
        private Transform transformTestPosition;

        [SerializeField]
        [Tooltip("Reference to the clone prefab used when passing through a portal.")]
        private GameObject prefabClone;

        private ITeleportCallback[] teleportables;

        private Vector3 baseScale;
        private float scaleMultiplier = 1f;

        private event Action<TeleportableObject, Portal> OnTeleport;

        private Clone clone;
        private Portal currentPortal;

        /// <summary>
        /// Reference to the Clone object.
        /// </summary>
        public Clone Clone => this.clone;

        /// <summary>
        /// Reference to the current Portal.
        /// </summary>
        public Portal CurrentPortal => this.currentPortal;

        /// <summary>
        /// Register to the teleportation callback.
        /// </summary>
        public void RegisterOnTeleport(Action<TeleportableObject, Portal> onTeleportDelegate)
        {
            this.OnTeleport += onTeleportDelegate;
        }

        /// <summary>
        /// Unregister from the teleportation callback.
        /// </summary>
        public void UnregisterOnTeleport(Action<TeleportableObject, Portal> onTeleportDelegate)
        {
            this.OnTeleport -= onTeleportDelegate;
        }

        /// <summary>
        /// Called when entering a portal trigger.
        /// </summary>
        public void OnEnterPortalTrigger(Portal portal)
        {
            Assert.IsNotNull(portal);

            this.currentPortal = portal;
            this.clone.gameObject.SetActive(true);

            // OnEnterPortalTrigger is called form OnTriggerEnter which is call before the update method.
            // Therefore, the clone will be move to the correct position before the rendering.
        }

        /// <summary>
        /// Called when exiting a portal trigger.
        /// </summary>
        public void OnExitPortalTrigger(Portal portal)
        {
            Assert.IsNotNull(portal);

            // Don't clear the current portal if we just teleported
            if (portal == this.currentPortal)
            {
                this.clone.gameObject.SetActive(false);
                this.currentPortal = null;
            }
        }

        private void Awake()
        {
            this.baseScale = this.transform.localScale;
            this.teleportables = this.GetComponentsInChildren<ITeleportCallback>();

            if (TeleportableObject.clonesParent == null)
            {
                TeleportableObject.clonesParent = new GameObject("Clones").transform;
            }

            // Instantiate the clone
            GameObject cloneGameObject = GameObject.Instantiate<GameObject>(this.prefabClone);
            cloneGameObject.transform.parent = TeleportableObject.clonesParent;

            this.clone = cloneGameObject.GetComponent<Clone>();
            if (this.clone == null)
            {
                Debug.LogWarning($"Clone object '{cloneGameObject.name}' doesn't contains a Clone component. Clone component added.");
                this.clone = cloneGameObject.AddComponent<Clone>();
            }

            this.clone.SetTeleportableObject(this);
            cloneGameObject.SetActive(false);
        }

        private void Update()
        {
            if (this.currentPortal == null)
            {
                return;
            }

            // Check if we need to teleport the object
            float distance = Math.SignedDistancePlanePoint(this.currentPortal.transform.forward, this.currentPortal.transform.position, this.transformTestPosition.position);
            if (distance < 0f)
            {
                this.Teleport(this.currentPortal);
            }

            // Update the clone transform
            this.currentPortal.TransformThroughPortal(this.transform, this.clone.transform, true);
        }

        private void Teleport(Portal portal)
        {
            // Teleport the transform through the portal
            portal.TransformThroughPortal(this.transform, this.transform);

            // Update the scale of the object
            float destinationScaleRatio = portal.GetDestinationScaleRatio();
            if (destinationScaleRatio != 1f)
            {
                this.scaleMultiplier *= portal.GetDestinationScaleRatio();
                this.transform.localScale = this.baseScale * this.scaleMultiplier;
            }

            // Call the callbacks
            for (int i = 0; i < this.teleportables.Length; ++i)
            {
                this.teleportables[i].OnTeleport(portal, this.scaleMultiplier);
            }

            this.OnTeleport?.Invoke(this, portal);

            // Update the current portal
            // TODO : Is there a way to ensure that we are in the DestinationPortal's trigger ?
            this.currentPortal = portal.DestinationPortal;
        }
    }
}
