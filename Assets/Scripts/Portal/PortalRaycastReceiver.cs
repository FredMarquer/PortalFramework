using UnityEngine;

namespace PortalFramework
{
    /// <summary>
    /// Allows raycast (from the PortalRaycast class) to pass through the portal.
    /// </summary>
    public class PortalRaycastReceiver : MonoBehaviour
    {
        private Portal portal;

        /// <summary>
        /// The Portal reference owning this object.
        /// </summary>
        public Portal Portal => this.portal;

        /// <summary>
        /// Set the reference of the Portal owning this object.
        /// </summary>
        public void SetPortal(Portal portal)
        {
            this.portal = portal;
        }
    }
}
