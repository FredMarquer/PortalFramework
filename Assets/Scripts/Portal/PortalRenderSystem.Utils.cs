using UnityEngine;

namespace PortalFramework
{
    public partial class PortalRenderSystem
    {
        private static Plane[] frustumPlanes = new Plane[6];

        private static bool IsPortalFacingCamera(Portal portal, Camera camera)
        {
            Vector3 cameraDirection = camera.transform.position - portal.transform.position;
            return Vector3.Dot(cameraDirection, portal.transform.forward) > 0f;
        }

        private static bool IsPortalInFrustrumView(Portal portal, Camera camera)
        {
            // TODO : Optim -> don't recalculate plane everytime
            GeometryUtility.CalculateFrustumPlanes(camera, frustumPlanes);
            return GeometryUtility.TestPlanesAABB(frustumPlanes, portal.Bounds);
        }

        private static bool IsPortalInViewport(ref Rect viewportPortal, ref Rect viewport)
        {
            return
                viewportPortal.xMax > viewport.xMin &&
                viewportPortal.xMin < viewport.xMax &&
                viewportPortal.yMax > viewport.yMin &&
                viewportPortal.yMin < viewport.yMax;
        }

        private static Rect CalculatePortalViewportRect(Portal portal, Camera camera, Rect cameraViewport)
        {
            // If at least one portal corner is located behind the camera, render all the viewport.
            // TODO : Find a way to not render all the viewport
            Vector3 cameraPosition = camera.transform.position;
            Vector3 cameraForward = camera.transform.forward;
            if (Vector3.Dot(cameraForward, portal.TopRight - cameraPosition) <= 0f ||
                Vector3.Dot(cameraForward, portal.TopLeft - cameraPosition) <= 0f ||
                Vector3.Dot(cameraForward, portal.BottomRight - cameraPosition) <= 0f ||
                Vector3.Dot(cameraForward, portal.BottomLeft - cameraPosition) <= 0f)
            {
                return cameraViewport;
            }

            // Compute portal corners screen positions
            Vector2 topRight = camera.WorldToViewportPoint(portal.TopRight);
            Vector2 topLeft = camera.WorldToViewportPoint(portal.TopLeft);
            Vector2 bottomRight = camera.WorldToViewportPoint(portal.BottomRight);
            Vector2 bottomLeft = camera.WorldToViewportPoint(portal.BottomLeft);

            // Find portal bounds on screen
            float xMin = Math.Min4f(topRight.x, topLeft.x, bottomRight.x, bottomLeft.x);
            float xMax = Math.Max4f(topRight.x, topLeft.x, bottomRight.x, bottomLeft.x);
            float yMin = Math.Min4f(topRight.y, topLeft.y, bottomRight.y, bottomLeft.y);
            float yMax = Math.Max4f(topRight.y, topLeft.y, bottomRight.y, bottomLeft.y);

            // Compute the portal viewport, taking into account the current viewport of the camera
            Rect portalViewport = default;
            portalViewport.x = cameraViewport.x + (xMin * cameraViewport.width);
            portalViewport.y = cameraViewport.y + (yMin * cameraViewport.height);
            portalViewport.width = (xMax - xMin) * cameraViewport.width;
            portalViewport.height = (yMax - yMin) * cameraViewport.height;
            return portalViewport;
        }

        private static void ClampViewportRect(ref Rect viewport, ref Rect bounds)
        {
            float xMin = Mathf.Clamp(viewport.xMin, bounds.xMin, bounds.xMax);
            float yMin = Mathf.Clamp(viewport.yMin, bounds.yMin, bounds.yMax);
            float xMax = Mathf.Clamp(viewport.xMax, bounds.xMin, bounds.xMax);
            float yMax = Mathf.Clamp(viewport.yMax, bounds.yMin, bounds.yMax);

            viewport = new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        private static void SetViewportRectPixelPerfect(ref Rect viewport, int pixelWidth, int pixelHeight)
        {
            float xMin = Mathf.Floor(viewport.xMin * pixelWidth) / pixelWidth;
            float yMin = Mathf.Floor(viewport.yMin * pixelHeight) / pixelHeight;
            float xMax = Mathf.Ceil(viewport.xMax * pixelWidth) / pixelWidth;
            float yMax = Mathf.Ceil(viewport.yMax * pixelHeight) / pixelHeight;

            viewport = new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        private static float CalculateNearClip(Camera camera, Portal portal, float minNearClip)
        {
            Vector3 cameraPosition = camera.transform.position;
            Vector3 cameraForward = camera.transform.forward;

            // Find corner with the minimum distance to the 'camera plane'
            float distanceTopRight = Math.SignedDistancePlanePoint(cameraForward, cameraPosition, portal.TopRight);
            float distanceTopLeft = Math.SignedDistancePlanePoint(cameraForward, cameraPosition, portal.TopLeft);
            float distanceBottomRight = Math.SignedDistancePlanePoint(cameraForward, cameraPosition, portal.BottomRight);
            float distanceBottomLeft = Math.SignedDistancePlanePoint(cameraForward, cameraPosition, portal.BottomLeft);
            float minDistance = Math.Min4f(distanceTopRight, distanceTopLeft, distanceBottomRight, distanceBottomLeft);

            return Mathf.Max(minDistance, minNearClip);
        }

        private static Matrix4x4 CalculateProjectionMatrix(Camera portalCamera, int pixelWidth, int pixelHeight, ref Rect viewport)
        {
            // Retrieve camera parameters
            float near = portalCamera.nearClipPlane;
            float far = portalCamera.farClipPlane;
            float ratio = (float)pixelWidth / pixelHeight;
            float fov = portalCamera.fieldOfView;

            return PortalRenderSystem.BuildProjectionMatrix(near, far, ratio, fov, ref viewport);
        }

        private static Matrix4x4 BuildProjectionMatrix(float near, float far, float ratio, float fov, ref Rect viewport)
        {
            float cameraTop = near * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            float cameraBottom = -cameraTop;
            float cameraRight = cameraTop * ratio;
            float cameraLeft = -cameraRight;

            float top = Mathf.Lerp(cameraBottom, cameraTop, viewport.yMax);
            float bottom = Mathf.Lerp(cameraBottom, cameraTop, viewport.yMin);
            float right = Mathf.Lerp(cameraLeft, cameraRight, viewport.xMax);
            float left = Mathf.Lerp(cameraLeft, cameraRight, viewport.xMin);

            float x = 2.0F * near / (right - left);
            float y = 2.0F * near / (top - bottom);
            float a = (right + left) / (right - left);
            float b = (top + bottom) / (top - bottom);
            float c = -(far + near) / (far - near);
            float d = -(2.0F * far * near) / (far - near);
            float e = -1.0F;

            return new Matrix4x4(
                new Vector4(x, 0, 0, 0),
                new Vector4(0, y, 0, 0),
                new Vector4(a, b, c, e),
                new Vector4(0, 0, d, 0));
        }

        private static void ClipProjectionMatrixNear(Camera camera, Portal portal)
        {
            Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
            Vector3 planeNormal = viewMatrix.MultiplyVector(portal.transform.forward).normalized;
            Vector3 planePosition = viewMatrix.MultiplyPoint(portal.transform.position) - (planeNormal * 0.01f);

            float w = -Vector3.Dot(planePosition, planeNormal);
            if (w > 0f)
            {
                return;
            }

            Vector4 clipPlane = new Vector4(planeNormal.x, planeNormal.y, planeNormal.z, w);
            camera.projectionMatrix = camera.CalculateObliqueMatrix(clipPlane);
        }
    }
}
