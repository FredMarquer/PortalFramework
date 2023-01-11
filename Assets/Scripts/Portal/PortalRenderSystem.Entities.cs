using System.Collections.Generic;
using UnityEngine;

namespace PortalFramework
{
    public partial class PortalRenderSystem
    {
        private static List<Portal> portals = new List<Portal>();
        private static List<DirectionalLight> directionalLights = new List<DirectionalLight>();

        /// <summary>
        /// Register a Portal to the PortalRenderSystem.
        /// </summary>
        public static void RegisterPortal(Portal portal)
        {
            PortalRenderSystem.portals.Add(portal);
        }

        /// <summary>
        /// Unregister a Portal from the PortalRenderSystem.
        /// </summary>
        public static void UnregisterPortal(Portal portal)
        {
            PortalRenderSystem.portals.Remove(portal);
        }

        /// <summary>
        /// Register a DirectionalLight to the PortalRenderSystem.
        /// </summary>
        public static void RegisterDirectionalLight(DirectionalLight light)
        {
            PortalRenderSystem.directionalLights.Add(light);
        }

        /// <summary>
        /// Unregister a DirectionalLight from the PortalRenderSystem.
        /// </summary>
        public static void UnregisterDirectionalLight(DirectionalLight light)
        {
            PortalRenderSystem.directionalLights.Remove(light);
        }

        private static void SetDirectionalLightsRotation(Quaternion rotation)
        {
            for (int i = 0; i < PortalRenderSystem.directionalLights.Count; ++i)
            {
                PortalRenderSystem.directionalLights[i].SetRotation(rotation);
            }
        }
    }
}
