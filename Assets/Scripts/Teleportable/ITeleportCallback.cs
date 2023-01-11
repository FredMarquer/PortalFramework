namespace PortalFramework
{
    /// <summary>
    /// Allows a component to receive callback from teleportation trough a portal.
    /// </summary>
    public interface ITeleportCallback
    {
        /// <summary>
        /// Called after being teleported by a portal.
        /// </summary>
        void OnTeleport(Portal portal, float newScale);
    }
}
