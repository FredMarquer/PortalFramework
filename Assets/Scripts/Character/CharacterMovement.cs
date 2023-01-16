using UnityEngine;
using UnityEngine.Assertions;

namespace PortalFramework
{
    /// <summary>
    /// Controls a character's movement.
    /// </summary>
    [RequireComponent(typeof(CharacterCollider))]
    public class CharacterMovement : MonoBehaviour, ITeleportCallback
    {
        [SerializeField]
        [Tooltip("Reference of the head Transform.")]
        private Transform headTransform;

        [Header("Movement - Ground")]
        [SerializeField]
        [Tooltip("Speed of the character when grounded.")]
        private float groundSpeed = 6f;

        [SerializeField]
        [Tooltip("Acceleration applied to the character when grounded.")]
        private float groundAcceleration = 10f;

        [SerializeField]
        [Tooltip("Friction applied to the character when grounded.")]
        private float groundFriction = 6f;

        [SerializeField]
        [Tooltip("Speed below which the applied friction is constant, when grounded.")]
        private float groundStopSpeed = 1f;

        [Header("Movement - Air")]
        [SerializeField]
        [Tooltip("Speed of the character when in the air.")]
        private float airSpeed = 6f;

        [SerializeField]
        [Tooltip("Acceleration applied to the character when in the air.")]
        private float airAcceleration = 2f;

        [SerializeField]
        [Tooltip("Friction applied to the character when in the air.")]
        private float airFriction = 0f;

        [SerializeField]
        [Tooltip("Speed below which the applied friction is constant, when in the air.")]
        private float airStopSpeed = 0f;

        [SerializeField]
        [Tooltip("Strength of the air control. It's a Counter Strike like air control!")]
        private float airControl = 25f;

        [Header("Movement - Other")]

        [SerializeField]
        [Tooltip("Height at which the character jumps.")]
        private float jumpHeight = 1f;

        [SerializeField]
        [Tooltip("Gravity force applied to the characeter.")]
        private float gravity = 18f;

        [SerializeField]
        [Tooltip("Is the character able to gain speed beyond the max speed by strafing. Quake style!")]
        private bool canStrafe = true;

        private CharacterCollider characterCollider;

        Vector3 localInputDirection;
        private Vector3 up;
        private float scale = 1f;
        private float rotationY;
        private Vector3 velocity;
        private bool isGrounded;
        private Vector3? groundNormal;

        /// <summary>
        /// Current velocity of the character.
        /// </summary>
        public Vector3 Velocity => this.velocity * this.scale;

        /// <summary>
        /// Is the character grounded.
        /// </summary>
        public bool IsGrounded => this.isGrounded;

        /// <summary>
        /// Current ground normal of the character.
        /// </summary>
        public Vector3? GroundNormal => this.groundNormal;

        /// <summary>
        /// Set the input direction of the character.
        /// </summary>
        public void SetInputDirection(Vector3 inputDirection)
        {
            if (inputDirection.sqrMagnitude > 1f)
            {
                inputDirection.Normalize();
            }

            this.localInputDirection = inputDirection;
        }

        /// <summary>
        /// Rotate the look direction of the character.
        /// </summary>
        public void RotateLookAt(float yaw, float pitch)
        {
            // Update the character yaw.
            this.transform.Rotate(0f, yaw, 0f, Space.Self);

            // Update the character head pitch.
            this.rotationY -= pitch;
            this.rotationY = Mathf.Clamp(this.rotationY, -90f, 90f);
            this.headTransform.localEulerAngles = new Vector3(this.rotationY, 0f, 0f);
        }

        /// <summary>
        /// Jump if grounded.
        /// </summary>
        public void TryJump()
        {
            if (this.isGrounded)
            {
                this.Jump();
            }
        }

        void ITeleportCallback.OnTeleport(Portal portal, float newScale)
        {
            this.scale = newScale;

            // Transform the velocity through the portal
            this.velocity = portal.DirectionThroughPortal(this.velocity);

            // Transform the ground normal through the portal
            if (this.groundNormal.HasValue)
            {
                this.groundNormal = portal.DirectionThroughPortal(this.groundNormal.Value);
            }
        }

        private void Awake()
        {
            this.characterCollider = this.GetComponent<CharacterCollider>();
            Assert.IsNotNull(this.characterCollider);
        }

        private void Start()
        {
            this.isGrounded = this.characterCollider.IsGrounded;
            this.groundNormal = this.characterCollider.GroundNormal;
        }

        private void Update()
        {
            this.up = this.transform.up;

            float deltaTime = Time.deltaTime;

            // Update velocity
            this.UpdateVelocity(deltaTime);

            // Move the character
            Vector3 move = this.velocity * this.scale * deltaTime;
            this.characterCollider.Move(move);

            if (!this.isGrounded && this.characterCollider.IsGrounded)
            {
                this.OnLanded();
            }

            // Update data
            this.velocity = this.characterCollider.Velocity / this.scale;
            this.isGrounded = this.characterCollider.IsGrounded;
            this.groundNormal = this.characterCollider.GroundNormal;
        }

        private void UpdateVelocity(float deltaTime)
        {
            Vector3 worldInputDirection = this.transform.rotation * this.localInputDirection;

            if (this.isGrounded)
            {
                this.ApplyFriction(this.groundFriction, this.groundStopSpeed, deltaTime);
                this.ApplyAcceleration(worldInputDirection, this.groundSpeed, this.groundAcceleration, deltaTime);
            }
            else
            {
                this.ApplyFriction(this.airFriction, this.airStopSpeed, deltaTime);
                this.ApplyAcceleration(worldInputDirection, this.airSpeed, this.airAcceleration, deltaTime);
                this.ApplyGravity(deltaTime);
                this.ApplyAirControl(worldInputDirection, deltaTime);
            }
        }

        private void ApplyFriction(float friction, float stopSpeed, float deltaTime)
        {
            if (friction <= 0f || this.velocity == Vector3.zero)
            {
                return;
            }

            float currentSpeed = this.velocity.magnitude;
            if (currentSpeed == 0f)
            {
                return;
            }

            // Compute friction the same way as Quake
            float control = currentSpeed < stopSpeed ? stopSpeed : currentSpeed;
            float drop = control * friction * deltaTime;

            float newSpeed = currentSpeed - drop;
            if (newSpeed < 0)
            {
                newSpeed = 0;
            }

            newSpeed /= currentSpeed;
            this.velocity = this.velocity * newSpeed;
        }

        private void ApplyAcceleration(Vector3 direction, float speed, float acceleration, float deltaTime)
        {
            if (acceleration <= 0f || direction == Vector3.zero)
            {
                return;
            }

            // If character grounded, accelerate along the ground surface
            if (this.isGrounded)
            {
                Assert.IsTrue(this.groundNormal.HasValue);
                direction = CharacterMovementHelper.ProjectVectorOnGroundPlane(direction, this.groundNormal.Value, this.up).normalized;
            }

            // Compute accelerate the same way as Quake, allowing for strafe juming
            float currentSpeed = Vector3.Dot(this.velocity, direction);
            float addSpeed = speed - currentSpeed;
            if (addSpeed <= 0)
            {
                return;
            }

            float speedLimit = Mathf.Max(this.velocity.magnitude, speed);
            float accelSpeed = acceleration * deltaTime * speed;
            if (accelSpeed > addSpeed)
            {
                accelSpeed = addSpeed;
            }

            this.velocity += accelSpeed * direction;

            // If 'strafe' is disable, don't allow to accelerate beyond the speed limit
            if (!this.canStrafe)
            {
                float newSpeed = this.velocity.magnitude;
                if (newSpeed > 0f && newSpeed > speedLimit)
                {
                    this.velocity *= speedLimit / newSpeed;
                }
            }
        }

        private void ApplyGravity(float deltaTime)
        {
            if (this.gravity == 0f)
            {
                return;
            }

            // Compute the gravity vector
            Vector3 gravity = this.up * -(this.gravity * deltaTime);
            if (this.groundNormal.HasValue)
            {
                gravity = Vector3.ProjectOnPlane(gravity, this.groundNormal.Value);
            }

            // Apply the gravity vector
            this.velocity += gravity;
        }

        private void ApplyAirControl(Vector3 worldInputDirection, float deltaTime)
        {
            if (this.airControl <= 0f || this.velocity == Vector3.zero || worldInputDirection == Vector3.zero)
            {
                return;
            }

            if (Vector3.Dot(this.velocity, worldInputDirection) >= 0f)
            {
                return;
            }

            // Apply an air control similar way as Counter Strike (also allowing for the surf mechanics!!!)
            Vector3 targetVelocity = Vector3.ProjectOnPlane(this.velocity, worldInputDirection);
            float ratio = Mathf.Min(this.airControl * deltaTime, 1f);
            this.velocity = Vector3.Lerp(this.velocity, targetVelocity, ratio);

            if (this.groundNormal.HasValue && Vector3.Dot(this.velocity, this.groundNormal.Value) < 0f)
            {
                Assert.IsTrue(this.groundNormal.HasValue);
                this.velocity = Vector3.ProjectOnPlane(this.velocity, this.groundNormal.Value);
            }
        }

        private void Jump()
        {
            // Not grounded anymore
            this.isGrounded = false;
            this.groundNormal = null;

            // Don't jump if our 'up velocity' is too high
            float oldUpSpeed = Vector3.Dot(this.velocity, this.up);
            float currentJumpHeight = CharacterMovementHelper.ComputeJumpHeight(oldUpSpeed, this.gravity);
            if (currentJumpHeight >= this.jumpHeight)
            {
                return;
            }

            // Compute the new velocity
            float newJumpSpeed = CharacterMovementHelper.ComputeJumpVelocity(this.jumpHeight, this.gravity);
            Vector3 oldUpVelocity = Vector3.Project(this.velocity, this.up);
            Vector3 newJumpVelocity = this.up * newJumpSpeed;
            this.velocity += newJumpVelocity - oldUpVelocity;
        }

        private void OnLanded()
        {
            // Compute the fall height
            float upVelocity = Vector3.Dot(this.velocity, this.up);
            float fallHeight = CharacterMovementHelper.ComputeJumpHeight(-upVelocity, this.gravity);

            // Raise the fall event
            if (fallHeight > 0.1f)
            {
                // TODO
            }
        }
    }
}
