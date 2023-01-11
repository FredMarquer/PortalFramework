using UnityEngine;

namespace PortalFramework
{
    /// <summary>
    /// Component to identify teleportable object's clone when inside a portal trigger.
    /// </summary>
    public class Clone : MonoBehaviour
    {
        /// <summary>
        /// The TeleportableObject that owns this clone.
        /// </summary>
        public TeleportableObject TeleportableObject
        {
            get;
            private set;
        }

        /// <summary>
        /// Set the TeleportableObject that owns this clone.
        /// </summary>
        public void SetTeleportableObject(TeleportableObject teleportableObject)
        {
            this.TeleportableObject = teleportableObject;
        }
    }
}
