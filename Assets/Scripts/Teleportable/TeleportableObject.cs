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

        /// <summary>
        /// Reference to the Clone object.
        /// </summary>
        public Clone Clone
        { 
            get;
            private set;
        }

        /// <summary>
        /// Reference to the current Portal.
        /// </summary>
        public Portal CurrentPortal
        { 
            get;
            private set;
        }

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

            this.CurrentPortal = portal;
            this.Clone.gameObject.SetActive(true);
        }

        /// <summary>
        /// Called when exiting a portal trigger.
        /// </summary>
        public void OnExitPortalTrigger(Portal portal)
        {
            Assert.IsNotNull(portal);

            this.Clone.gameObject.SetActive(false);
            this.CurrentPortal = null;
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

            this.Clone = cloneGameObject.GetComponent<Clone>();
            if (this.Clone == null)
            {
                Debug.LogWarning($"Clone object '{cloneGameObject.name}' doesn't contains a Clone component. Clone component added.");
                this.Clone = cloneGameObject.AddComponent<Clone>();
            }

            this.Clone.SetTeleportableObject(this);
            cloneGameObject.SetActive(false);
        }

        private void Update()
        {
            if (this.CurrentPortal == null)
            {
                return;
            }

            // Check if we need to teleport the object
            float distance = Math.SignedDistancePlanePoint(this.CurrentPortal.transform.forward, this.CurrentPortal.transform.position, this.transformTestPosition.position);
            if (distance < 0f)
            {
                this.Teleport(this.CurrentPortal);
            }

            // Update the clone transform
            this.CurrentPortal.TransformThroughPortal(this.transform, this.Clone.transform, true);
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
            this.CurrentPortal = portal.DestinationPortal;
        }
    }
}
