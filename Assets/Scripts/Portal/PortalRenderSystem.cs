using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace PortalFramework
{
    /// <summary>
    /// Allows a camera to render portals.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public partial class PortalRenderSystem : MonoBehaviour, ITeleportCallback
    {
        private static Camera portalRenderCamera;
    
        [SerializeField]
        [Range(0f, 100f)]
        [Tooltip("Maximum number of recursion that the camera can render.")]
        private int maximumNumberOfRecursion = 4;

        [SerializeField]
        [Tooltip("Allow to have an oblique near clip when rendering a portal. Allowing to not render objects on the backside of the destination portal.")]
        private bool obliqueNearClipEnabled = true;

        [SerializeField]
        [Tooltip("Allow to have a seamless lighting through portal from the directional lights.")]
        private bool seamlessDirectionalLightEnabled = true;

        [SerializeField]
        [Tooltip("TeleportableObject reference of the object owning the camera.")]
        private TeleportableObject teleportableObject;

        [SerializeField]
        [Tooltip("Renderer reference of the object owning the camera.")]
        private new Renderer renderer;

        private Renderer cloneRenderer;

        private new Camera camera;

        private float baseNearClip;
        private float baseFarClip;

        private Quaternion directionalLightRotation = Quaternion.identity;

        private int pixelWidth;
        private int pixelHeight;

        private List<PortalRenderResult> portalRenderResults = new List<PortalRenderResult>();
        private List<PortalRenderResult>[] portalRenderResultPerRecursion;

        void ITeleportCallback.OnTeleport(Portal portal, float newScale)
        {
            // Scale the camera near and far clip
            this.camera.nearClipPlane = this.baseNearClip * newScale;
            this.camera.farClipPlane = this.baseFarClip * newScale;

            // Transform the directional light rotation through the portal
            this.directionalLightRotation = portal.RotationThroughPortal(this.directionalLightRotation);
        }

        private static void InitializeStatics()
        {
            if (PortalRenderSystem.portalRenderCamera != null)
            {
                return;
            }

            // Create the portal render camera
            PortalRenderSystem.portalRenderCamera = new GameObject("PortalCamera").AddComponent<Camera>();
            PortalRenderSystem.portalRenderCamera.enabled = false;
            GameObject.DontDestroyOnLoad(PortalRenderSystem.portalRenderCamera);
        }

        private static void SetPortalTextures(List<PortalRenderResult> results, Rect viewport)
        {
            for (int i = 0; i < results.Count; ++i)
            {
                PortalRenderResult portalRenderResult = results[i];
                portalRenderResult.Portal.SetMaterialTexture(portalRenderResult.RenderTexture);
                portalRenderResult.Portal.SetMaterialViewport(viewport);
            }
        }

        private static void ReleasePortalTextures(List<PortalRenderResult> results)
        {
            for (int i = 0; i < results.Count; ++i)
            {
                PortalRenderResult portalRenderResult = results[i];
                portalRenderResult.Portal.SetMaterialTexture(null);
                RenderTexture.ReleaseTemporary(portalRenderResult.RenderTexture);
            }

            results.Clear();
        }

        private void Start()
        {
            PortalRenderSystem.InitializeStatics();

            // Initialize camera fields
            this.camera = this.GetComponent<Camera>();
            this.baseNearClip = this.camera.nearClipPlane;
            this.baseFarClip = this.camera.farClipPlane;

            // TODO: Rename method to Awake when get rid of this
            this.cloneRenderer = this.teleportableObject.Clone.GetComponentInChildren<Renderer>();
        }

        private void OnPreCull()
        {
            Assert.AreEqual(this.portalRenderResults.Count, 0);

            // Resize the portalRenderResultPerRecursion array if necessary
            if (this.portalRenderResultPerRecursion == null ||
                this.portalRenderResultPerRecursion.Length != this.maximumNumberOfRecursion)
            {
                int oldSize = this.portalRenderResultPerRecursion?.Length ?? 0;
                System.Array.Resize(ref this.portalRenderResultPerRecursion, this.maximumNumberOfRecursion);

                for (int i = oldSize; i < this.maximumNumberOfRecursion; ++i)
                {
                    this.portalRenderResultPerRecursion[i] = new List<PortalRenderResult>();
                }
            }

            // Update the pixel width and height
            this.pixelWidth = this.camera.pixelWidth;
            this.pixelHeight = this.camera.pixelHeight;

            // Initialize the portal render camera
            PortalRenderSystem.portalRenderCamera.CopyFrom(this.camera);
            PortalRenderSystem.portalRenderCamera.transform.SetPositionAndRotation(this.transform.position, this.transform.rotation);

            if (this.renderer != null)
            {
                this.renderer.enabled = true;
            }

            // Try render portals
            CameraConfiguration cameraConfig = new CameraConfiguration(this.camera);
            for (int i = 0; i < portals.Count; ++i)
            {
                Portal portal = portals[i];
                bool drawClone = this.teleportableObject.CurrentPortal != portal && portal != portal.DestinationPortal;
                if (this.TryRenderPortal(portal, ref cameraConfig, 0, drawClone, out PortalRenderResult result))
                {
                    this.portalRenderResults.Add(result);
                }
            }

            // Set the portal textures to render
            PortalRenderSystem.SetPortalTextures(this.portalRenderResults, cameraConfig.Viewport);

            // Set the direction light rotation
            if (this.seamlessDirectionalLightEnabled)
            {
                PortalRenderSystem.SetDirectionalLightsRotation(this.directionalLightRotation);
            }

            if (this.cloneRenderer != null)
            {
                this.cloneRenderer.enabled = true;
            }

            if (this.renderer != null)
            {
                this.renderer.enabled = false;
            }
        }

        private void OnPostRender()
        {
            // Release the portal render textures
            PortalRenderSystem.ReleasePortalTextures(this.portalRenderResults);

            // Set the direction light rotation to its default rotation
            if (this.seamlessDirectionalLightEnabled)
            {
                PortalRenderSystem.SetDirectionalLightsRotation(Quaternion.identity);
            }

            if (this.renderer != null)
            {
                this.renderer.enabled = true;
            }
        }

        private bool TryRenderPortal(Portal portalToRender, ref CameraConfiguration cameraConfig, int recursionLevel, bool drawClone, out PortalRenderResult outResult)
        {
            Assert.IsNotNull(portalToRender);

            // Don't render a portal if it don't have a destination
            if (portalToRender.DestinationPortal == null)
            {
                outResult = default;
                return false;
            }

            // Only render a portal if it's facing the camera
            if (!PortalRenderSystem.IsPortalFacingCamera(portalToRender, PortalRenderSystem.portalRenderCamera))
            {
                outResult = default;
                return false;
            }

            // Frustrum culling
            if (!PortalRenderSystem.IsPortalInFrustrumView(portalToRender, PortalRenderSystem.portalRenderCamera))
            {
                outResult = default;
                return false;
            }

            Rect portalViewport = PortalRenderSystem.CalculatePortalViewportRect(portalToRender, PortalRenderSystem.portalRenderCamera, cameraConfig.Viewport);

            // Check if in camera viewport
            if (!PortalRenderSystem.IsPortalInViewport(ref portalViewport, ref cameraConfig.Viewport))
            {
                outResult = default;
                return false;
            }

            // Correct portal viewport
            PortalRenderSystem.SetViewportRectPixelPerfect(ref portalViewport, this.pixelWidth, this.pixelHeight);
            PortalRenderSystem.ClampViewportRect(ref portalViewport, ref cameraConfig.Viewport);

            if (portalViewport.width <= 0f || portalViewport.height <= 0f)
            {
                outResult = default;
                return false;
            }

            // Apply portal position and rotation transformation to camera
            portalToRender.TransformThroughPortal(PortalRenderSystem.portalRenderCamera.transform, PortalRenderSystem.portalRenderCamera.transform);

            // Compute near and far clip and projection matrix
            // TODO : Compute a minimum near clip modified by portalToRender.GetDestinationScaleRatio()
            PortalRenderSystem.portalRenderCamera.rect = portalViewport;
            PortalRenderSystem.portalRenderCamera.nearClipPlane = PortalRenderSystem.CalculateNearClip(PortalRenderSystem.portalRenderCamera, portalToRender.DestinationPortal, this.baseNearClip);
            PortalRenderSystem.portalRenderCamera.farClipPlane *= portalToRender.GetDestinationScaleRatio();
            PortalRenderSystem.portalRenderCamera.projectionMatrix = PortalRenderSystem.CalculateProjectionMatrix(PortalRenderSystem.portalRenderCamera, this.pixelWidth, this.pixelHeight, ref portalViewport);

            // Apply the portal transformation to the directional light
            Quaternion previousLightRotation = this.directionalLightRotation;
            this.directionalLightRotation = portalToRender.RotationThroughPortal(this.directionalLightRotation);

            if (recursionLevel < this.maximumNumberOfRecursion)
            {
                List<PortalRenderResult> recursionPortalRenderResults = this.portalRenderResultPerRecursion[recursionLevel];
                Debug.Assert(recursionPortalRenderResults.Count == 0);

                CameraConfiguration newCameraConfig = new CameraConfiguration(PortalRenderSystem.portalRenderCamera);

                // Try to render all portals.
                int portalCount = portals.Count;
                for (int i = 0; i < portalCount; ++i)
                {
                    Portal portal = PortalRenderSystem.portals[i];
                    if (portal == portalToRender.DestinationPortal)
                    {
                        continue;
                    }

                    if (this.TryRenderPortal(portal, ref newCameraConfig, recursionLevel + 1, true, out PortalRenderResult result))
                    {
                        recursionPortalRenderResults.Add(result);
                    }
                }

                // Set the portal textures to render
                PortalRenderSystem.SetPortalTextures(recursionPortalRenderResults, newCameraConfig.Viewport);
            }

            if (this.cloneRenderer != null)
            {
                this.cloneRenderer.enabled = drawClone;
            }

            // Set the direction light rotation
            if (this.seamlessDirectionalLightEnabled)
            {
                PortalRenderSystem.SetDirectionalLightsRotation(this.directionalLightRotation);
            }

            // Clip the projection matrix near clip, so that we don't see object on the back of the destination portal
            if (this.obliqueNearClipEnabled)
            {
                PortalRenderSystem.ClipProjectionMatrixNear(PortalRenderSystem.portalRenderCamera, portalToRender.DestinationPortal);
            }

            // Render the portal texture
            RenderTexture renderTexture = RenderTexture.GetTemporary(this.pixelWidth, this.pixelHeight, 32);
            PortalRenderSystem.portalRenderCamera.targetTexture = renderTexture;
            PortalRenderSystem.portalRenderCamera.Render();
            PortalRenderSystem.portalRenderCamera.targetTexture = null;

            // Release the portal render textures
            if (recursionLevel < this.maximumNumberOfRecursion)
            {
                PortalRenderSystem.ReleasePortalTextures(this.portalRenderResultPerRecursion[recursionLevel]);
            }

            // Set back intial values
            cameraConfig.Apply(PortalRenderSystem.portalRenderCamera);
            this.directionalLightRotation = previousLightRotation;

            // Set the portal texture result
            outResult = new PortalRenderResult()
            {
                Portal = portalToRender,
                RenderTexture = renderTexture,
            };

            return true;
        }

        private struct CameraConfiguration
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Rect Viewport;
            public float NearClip;
            public float FarClip;
            public Matrix4x4 ProjectionMatrix;

            public CameraConfiguration(Camera camera)
            {
                this.Position = camera.transform.position;
                this.Rotation = camera.transform.rotation;
                this.Viewport = camera.rect;
                this.NearClip = camera.nearClipPlane;
                this.FarClip = camera.farClipPlane;
                this.ProjectionMatrix = camera.projectionMatrix;
            }

            public void Apply(Camera camera)
            {
                camera.transform.SetPositionAndRotation(this.Position, this.Rotation);
                camera.rect = this.Viewport;
                camera.nearClipPlane = this.NearClip;
                camera.farClipPlane = this.FarClip;
                camera.projectionMatrix = this.ProjectionMatrix;
            }
        }

        private struct PortalRenderResult
        {
            public Portal Portal;
            public RenderTexture RenderTexture;
        }
    }
}
