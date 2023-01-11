using UnityEngine;

namespace PortalFramework
{
    /// <summary>
    /// Allow a directional light to generate seamless lighting through portals.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class DirectionalLight : MonoBehaviour
    {
        private new Light light;
        private Quaternion baseRotation;

        /// <summary>
        /// The Light attached to this object.
        /// </summary>
        public Light Light => this.light;

        /// <summary>
        /// Set the rotation of the light relative to its base rotation.
        /// </summary>
        public void SetRotation(Quaternion rotation)
        {
            this.transform.rotation = rotation * this.baseRotation;
        }

        private void Awake()
        {
            // Retrieve the Light component
            this.light = this.GetComponent<Light>();
            if (this.light == null ||
                this.light.type != LightType.Directional)
            {
                Debug.LogError($"DirectionalLight '{this.name}' don't have a Light component on it or is not of type 'Directional'.");
                this.enabled = false;
            }

            // Set the base rotation
            this.baseRotation = transform.rotation;
        }

        private void OnEnable()
        {
            // Register to the PortalRenderSystem
            PortalRenderSystem.RegisterDirectionalLight(this);
        }

        private void OnDisable()
        {
            // Unregister from the PortalRenderSystem
            PortalRenderSystem.UnregisterDirectionalLight(this);
        }
    }
}
