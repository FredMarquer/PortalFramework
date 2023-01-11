using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace PortalFramework
{
    /// <summary>
    /// Utility class for raycasting through portals.
    /// </summary>
    public static class PortalRaycast
    {
        /// <summary>
        /// Raycast that pass through portals.
        /// </summary>
        public static bool Raycast(Vector3 start, Vector3 direction, float distanceMax, LayerMask layer, int maxPortalCount, out RaycastHit hit, ref List<Portal> throughPortals)
        {
            if (!Physics.Raycast(start, direction, out hit, distanceMax, layer))
            {
                // We don't have hit anything
                throughPortals?.Clear();
                return false;
            }

            // Check if we have hit a portal
            PortalRaycastReceiver raycastReceiver = hit.transform.GetComponent<PortalRaycastReceiver>();
            if (raycastReceiver == null)
            {
                // We have hit a non portal object!
                return true;
            }

            if (maxPortalCount <= 0)
            {
                // We have reached the max portal count
                throughPortals?.Clear();
                return false;
            }

            Portal portal = raycastReceiver.Portal;
            if (portal == null)
            {
                Debug.LogError($"PortalRaycastReceiver '{raycastReceiver}' don't have a Portal.");
                throughPortals?.Clear();
                return false;
            }

            // Update the ray
            start = portal.PositionThroughPortal(hit.point);
            direction = portal.DirectionThroughPortal(direction);
            distanceMax -= hit.distance;

            // Add the portal to the list
            throughPortals?.Add(portal);

            // Continue to raycast from the destination portal
            return PortalRaycast.Raycast(start, direction, distanceMax, layer, maxPortalCount - 1, out hit, ref throughPortals);
        }
    }
}
