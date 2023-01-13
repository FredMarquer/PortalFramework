using UnityEngine;
using UnityEngine.Assertions;

namespace PortalFramework
{
    /// <summary>
    /// Representation of a portal entity.
    /// </summary>
    public class Portal : MonoBehaviour
    {
        private static readonly string ShaderName = "Hidden/Portal";
        private static readonly string MainTexPropertyName = "_MainTex";
        private static readonly string CameraViewportPropertyName = "_CameraViewport";

        private static Shader portalShader;
        private static int mainTexPropertyId;
        private static int cameraViewportPropertyId;

        [SerializeField]
        [Range(0.01f, 100f)]
        [Tooltip("Height of the portal.")]
        private float height = 2f;

        [SerializeField]
        [Range(0.01f, 100f)]
        [Tooltip("Ratio between the width and the height of the portal.")]
        private float widthRatio = 1f;

        [SerializeField]
        [Tooltip("Reference to the destination portal.")]
        private Portal destinationPortal;

        [SerializeField, Layer]
        [Tooltip("Layer of the portal renderer.")]
        private int renderLayer;

        [SerializeField, Layer]
        [Tooltip("Layer of the portal trigger. Used to detect teleportable objects.")]
        private int triggerLayer;

        [SerializeField, Layer]
        [Tooltip("Layer of the portal raycast receiver. Used for raycasting through portals (from the PortalRaycast class).")]
        private int raycastReceiverLayer;

        private new Renderer renderer;
        private Material material;

        private Transform inverseTranform;

        /// <summary>
        /// Width of the portal.
        /// </summary>
        public float Width => this.height * this.widthRatio;

        /// <summary>
        /// Height of the portal.
        /// </summary>
        public float Height => this.height;

        /// <summary>
        /// Ratio between the width and the height of the portal.
        /// </summary>
        public float WidthRatio => this.widthRatio;

        /// <summary>
        /// Reference to the destination portal.
        /// </summary>
        public Portal DestinationPortal => this.destinationPortal;

        /// <summary>
        /// Top right corner of the portal.
        /// </summary>
        public Vector3 TopRight
        {
            get;
            private set;
        }

        /// <summary>
        /// Top left corner of the portal.
        /// </summary>
        public Vector3 TopLeft
        {
            get;
            private set;
        }

        /// <summary>
        /// Bottom right corner of the portal.
        /// </summary>
        public Vector3 BottomRight
        {
            get;
            private set;
        }

        /// <summary>
        /// Bottom left corner of the portal.
        /// </summary>
        public Vector3 BottomLeft
        {
            get;
            private set;
        }

        /// <summary>
        /// Bounds of the portal.
        /// </summary>
        public Bounds Bounds
        {
            get;
            private set;
        }

        /// <summary>
        /// Reference to the raycast receiver collider.
        /// </summary>
        public Collider RaycastRecieverCollider
        {
            get;
            private set;
        }

        /// <summary>
        /// Return the scale ratio between this portal and the destination portal.
        /// </summary>
        public float GetDestinationScaleRatio()
        {
            Assert.IsNotNull(this.destinationPortal);
            return this.destinationPortal.height / this.height;
        }

        /// <summary>
        /// Set the texture property of the portal's material.
        /// </summary>
        public void SetMaterialTexture(Texture texture)
        {
            this.material.SetTexture(Portal.mainTexPropertyId, texture);

            // Disable the renderer if there is not texture
            this.renderer.enabled = texture != null;
        }

        /// <summary>
        /// Set the viewport property of the portal's material.
        /// </summary>
        public void SetMaterialViewport(Rect viewport)
        {
            this.material.SetVector(Portal.cameraViewportPropertyId, new Vector4(viewport.x, viewport.y, viewport.width, viewport.height));
        }

        /// <summary>
        /// Transforms a Transform through this portal.
        /// </summary>
        public void TransformThroughPortal(Transform transformRef, Transform transformToMove, bool modifyScale = false)
        {
            if (this.destinationPortal == null)
            {
                Debug.LogError("No destination portal.");
                return;
            }

            // Transform the position and rotation of the transform
            Vector3 position = this.PositionThroughPortal(transformRef.position);
            Quaternion rotation = this.RotationThroughPortal(transformRef.rotation);
            transformToMove.SetPositionAndRotation(position, rotation);

            // Udate the scale of the transform if wanted
            if (modifyScale)
            {
                float scaleRatio = this.GetDestinationScaleRatio();
                transformToMove.localScale = transformRef.localScale * scaleRatio;
            }
        }

        /// <summary>
        /// Transforms a position through this portal.
        /// </summary>
        public Vector3 PositionThroughPortal(Vector3 position)
        {
            if (this.destinationPortal == null)
            {
                Debug.LogError("No destination portal.");
                return position;
            }

            // TODO : Should we compute a single transformation matrix for static portals ?
            position = this.transform.InverseTransformPoint(position);
            position *= this.GetDestinationScaleRatio();
            return this.destinationPortal.inverseTranform.TransformPoint(position);
        }

        /// <summary>
        /// Transforms a direction through this portal.
        /// </summary>
        public Vector3 DirectionThroughPortal(Vector3 direction)
        {
            if (this.destinationPortal == null)
            {
                Debug.LogError("No destination portal.");
                return direction;
            }

            // TODO : Should we compute a single transformation matrix for static portals ?
            direction = this.transform.InverseTransformDirection(direction);
            return this.destinationPortal.inverseTranform.TransformDirection(direction);
        }

        /// <summary>
        /// Transforms a rotation through this portal.
        /// </summary>
        public Quaternion RotationThroughPortal(Quaternion rotation)
        {
            if (this.destinationPortal == null)
            {
                Debug.LogError("No destination portal.");
                return rotation;
            }

            // Compute the new rotation by transforming the rotation forward and up vector trought the portal
            Vector3 forward = this.DirectionThroughPortal(rotation * Vector3.forward);
            Vector3 up = this.DirectionThroughPortal(rotation * Vector3.up);
            return Quaternion.LookRotation(forward, up);
        }

        private static void InitializeStatics()
        {
            if (Portal.portalShader != null)
            {
                return;
            }

            // Initialize shader related fields
            Portal.portalShader = Shader.Find(Portal.ShaderName);
            Portal.mainTexPropertyId = Shader.PropertyToID(Portal.MainTexPropertyName);
            Portal.cameraViewportPropertyId = Shader.PropertyToID(Portal.CameraViewportPropertyName);
        }

        private void Awake()
        {
            Portal.InitializeStatics();

            this.ValidateDestination();
            this.ComputeCornersAndBounds();
            this.CreateHierarchy();
        }

        private void OnEnable()
        {
            PortalRenderSystem.RegisterPortal(this);
        }

        private void OnDisable()
        {
            PortalRenderSystem.UnregisterPortal(this);
        }

        private void ValidateDestination()
        {
            if (this.destinationPortal != null &&
                this.destinationPortal.widthRatio != this.widthRatio)
            {
                Debug.LogWarning("Destination portal removed because not the same width ratio.");

                if (this.destinationPortal.destinationPortal == this)
                {
                    this.destinationPortal.destinationPortal = null;
                }

                this.destinationPortal = null;
            }
        }

        private void ComputeCornersAndBounds()
        {
            Assert.IsTrue(this.height > 0f);
            Assert.IsTrue(this.widthRatio > 0f);

            float halfHeight = this.height * 0.5f;
            float halfWidth = halfHeight * this.widthRatio;

            Vector3 position = this.transform.position;
            Vector3 halfUp = this.transform.up * halfHeight;
            Vector3 halfRight = this.transform.right * halfWidth;

            // Set corners
            this.TopRight = position + halfUp + halfRight;
            this.TopLeft = position + halfUp - halfRight;
            this.BottomRight = position - halfUp + halfRight;
            this.BottomLeft = position - halfUp - halfRight;

            // Set bounds
            Bounds bounds = new Bounds(position, Vector3.zero);
            bounds.Encapsulate(this.TopRight);
            bounds.Encapsulate(this.TopLeft);
            bounds.Encapsulate(this.BottomRight);
            bounds.Encapsulate(this.BottomLeft);
            this.Bounds = bounds;
        }

        private void CreateHierarchy()
        {
            // Create the mesh render
            {
                this.material = new Material(Portal.portalShader);

                GameObject rendererGameObject = new GameObject("Renderer");
                rendererGameObject.transform.SetParent(this.transform, false);
                rendererGameObject.transform.localScale = new Vector3(this.Width, this.height, 1f);
                rendererGameObject.layer = this.renderLayer;

                MeshFilter meshFilter = rendererGameObject.AddComponent<MeshFilter>();
                meshFilter.mesh = MeshBuilder.GetPortalMesh();

                this.renderer = rendererGameObject.AddComponent<MeshRenderer>();
                this.renderer.sharedMaterial = this.material;
                this.renderer.enabled = false;
            }

            // Create the trigger
            {
                GameObject triggerGameObject = new GameObject("Trigger");
                triggerGameObject.transform.SetParent(this.transform, false);
                triggerGameObject.layer = this.triggerLayer;

                BoxCollider collider = triggerGameObject.AddComponent<BoxCollider>();
                collider.center = new Vector3(0f, 0f, 0.1f);
                collider.size = new Vector3(this.Width, this.height, 0.2f);
                collider.isTrigger = true;

                PortalTrigger portalTrigger = triggerGameObject.AddComponent<PortalTrigger>();
                portalTrigger.SetPortal(this);
            }

            // Create the raycast receiver
            {
                if (this.raycastReceiverLayer == this.triggerLayer)
                {
                    Debug.LogError($"The trigger layer and the raycast receiver layer (= {this.raycastReceiverLayer}) should be different. Otherwise teleportable objects won't be able to traverse the portal.");
                }

                GameObject raycastReceiverGameObject = new GameObject("RaycastReceiver");
                raycastReceiverGameObject.transform.SetParent(this.transform, false);
                raycastReceiverGameObject.transform.localScale = new Vector3(this.Width, this.height, 1f);
                raycastReceiverGameObject.layer = this.raycastReceiverLayer;

                MeshFilter meshFilter = raycastReceiverGameObject.AddComponent<MeshFilter>();
                meshFilter.mesh = MeshBuilder.GetQuadMesh();

                this.RaycastRecieverCollider = raycastReceiverGameObject.AddComponent<MeshCollider>();

                PortalRaycastReceiver raycastReceiver = raycastReceiverGameObject.AddComponent<PortalRaycastReceiver>();
                raycastReceiver.SetPortal(this);
            }

            // Create the inverse transform
            {
                this.inverseTranform = new GameObject("Inverse").transform;
                this.inverseTranform.SetParent(this.transform, false);
                this.inverseTranform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Vector3 position = this.transform.position;
            Quaternion rotation = this.transform.rotation;

            // Select colors
            Color opaqueColor;
            if (this.destinationPortal != null &&
                this.destinationPortal.widthRatio != this.widthRatio)
            {
                opaqueColor = Color.red;
            }
            else
            {
                opaqueColor = Color.yellow;
            }

            Color transparentColor = opaqueColor;
            transparentColor.a = 0.6f;

            // Draw sphere
            Vector3 spherePosition = position - (this.transform.up * this.height * 0.5f);
            Gizmos.color = opaqueColor;
            Gizmos.DrawSphere(spherePosition, 0.25f);

            // Draw quad
            Vector3 quadScale = new Vector3(this.height, this.Width, 1f);
            Gizmos.color = transparentColor;
            Mesh quadMesh = MeshBuilder.GetQuadMesh();
            Gizmos.DrawMesh(quadMesh, position, rotation, quadScale);
            Gizmos.DrawMesh(quadMesh, position, rotation * Quaternion.Euler(0f, 180f, 0f), quadScale);

            if (this.destinationPortal != null)
            {
                // Draw bezier
                Vector3 destinationPosition = this.destinationPortal.transform.position;
                Vector3 destinationForward = this.destinationPortal.transform.forward;
                Vector3 startTangent = position + (-this.transform.forward * 10f);
                Vector3 endTangent = destinationPosition + (-destinationForward * 10f);
                UnityEditor.Handles.DrawBezier(position, destinationPosition, startTangent, endTangent, opaqueColor, null, 2.5f);
            }
        }
#endif
    }
}
