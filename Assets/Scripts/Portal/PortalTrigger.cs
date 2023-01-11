using UnityEngine;
using UnityEngine.Assertions;

namespace PortalFramework
{
    /// <summary>
    /// Detects the obects that enter the portal.
    /// </summary>
    public class PortalTrigger : MonoBehaviour
    {
        private Portal portal;

        /// <summary>
        /// Set the reference of the Portal owning this object.
        /// </summary>
        public void SetPortal(Portal portal)
        {
            this.portal = portal;
        }

        private void OnTriggerEnter(Collider collider)
        {
            // Handle teleportable objects that enters the trigger
            TeleportableObject teleportableObject = collider.GetComponent<TeleportableObject>();
            if (teleportableObject != null)
            {
                Assert.IsNotNull(this.portal);
                teleportableObject.OnEnterPortalTrigger(this.portal);
            }
        }

        private void OnTriggerExit(Collider collider)
        {
            // Handle teleportable objects that exits the trigger
            TeleportableObject teleportableObject = collider.GetComponent<TeleportableObject>();
            if (teleportableObject != null)
            {
                Assert.IsNotNull(this.portal);
                teleportableObject.OnExitPortalTrigger(this.portal);
            }
        }
    }
}
