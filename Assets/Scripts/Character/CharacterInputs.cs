using UnityEngine;

namespace PortalFramework
{
    /// <summary>
    /// Basic (hardcoded) handling of the character inputs.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class CharacterInputs : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Sensitivity of the mouse look.")]
        private float mouseSensitivity = 5f;

        private CharacterMovement movement;
        private CharacterGrab grab;

        private void Awake()
        {
            this.movement = this.GetComponent<CharacterMovement>();
            this.grab = this.GetComponent<CharacterGrab>();
        }

        private void Update()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            this.UpdateMovementInput();
            this.UpdateGrabInput();
        }

        private void UpdateMovementInput()
        {
            if (this.movement == null || !this.movement.enabled)
            {
                return;
            }

            // Update look at
            float yaw = Input.GetAxis("Mouse X") * this.mouseSensitivity;
            float pitch = Input.GetAxis("Mouse Y") * this.mouseSensitivity;
            this.movement.RotateLookAt(yaw, pitch);

            // Update movement direction
            Vector3 inputDirection = this.GetInputDirection();
            this.movement.SetInputDirection(inputDirection);

            // Update jump
            if (Input.GetKey(KeyCode.Space))
            {
                this.movement.TryJump();
            }
        }

        private Vector3 GetInputDirection()
        {
            Vector3 inputDirection = Vector3.zero;

            if (Input.GetKey(KeyCode.D))
            {
                inputDirection.x += 1f;
            }

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.Q))
            {
                inputDirection.x -= 1f;
            }

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Z))
            {
                inputDirection.z += 1f;
            }

            if (Input.GetKey(KeyCode.S))
            {
                inputDirection.z -= 1f;
            }

            return inputDirection;
        }

        private void UpdateGrabInput()
        {
            if (this.grab == null || !this.grab.enabled)
            {
                return;
            }

            // Update grab object
            if (Input.GetMouseButtonDown(0))
            {
                this.grab.TryGrabObject();
            }

            // Update drop object
            if (Input.GetMouseButtonUp(0))
            {
                this.grab.TryDropObject();
            }

            // Update launch object
            if (Input.GetMouseButtonDown(1))
            {
                this.grab.TryLaunchObject();
            }
        }
    }
}
