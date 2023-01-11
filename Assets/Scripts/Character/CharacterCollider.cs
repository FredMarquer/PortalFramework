using UnityEngine;
using UnityEngine.Assertions;

namespace PortalFramework
{
    /// <summary>
    /// Basic character collider that can be rotated in any direction.
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class CharacterCollider : MonoBehaviour
    {
        private const float SkinWidth = 0.002f;

        private static Collider[] overlappedColliders = new Collider[10];

        [SerializeField]
        [Tooltip("Maximum angle at which a surface is considered a to be a walkable ground.")]
        private float maxWalkAngle = 45f;

        private float minWalkCos;

        private Vector3 position;
        private Quaternion rotation;
        private Vector3 up;
        private Vector3 center;
        private float halfHeight;
        private float radius;
        private Vector3 point1Offset;
        private Vector3 point2Offset;
        private Vector3 velocity;

        private bool isGrounded;
        private Hit groundHit;

        private int collisionLayerMask;

        /// <summary>
        /// Capsule collider used for tracing the physics world.
        /// </summary>
        public CapsuleCollider CapsuleCollider
        {
            get;
            private set;
        }

        /// <summary>
        /// Resulting velocity of the last move.
        /// </summary>
        public Vector3 Velocity => this.velocity;

        /// <summary>
        /// Does the character was grounded at the end of the last move.
        /// </summary>
        public bool IsGrounded => this.isGrounded;

        /// <summary>
        /// Ground normal at the end of the last move.
        /// </summary>
        public Vector3? GroundNormal => this.groundHit.IsValid ? this.groundHit.Normal : null;

        /// <summary>
        /// Move the character.
        /// </summary>
        public void Move(Vector3 move)
        {
            this.UpdateCachedData();

            if (move != Vector3.zero)
            {
                this.MoveInternal(move);
            }

            this.Depenetrate();
            this.UpdateGround();

            this.transform.position = this.position;
        }

        private void Awake()
        {
            // Initialize collider
            this.CapsuleCollider = this.GetComponent<CapsuleCollider>();
            Assert.IsNotNull(this.CapsuleCollider);
            this.CapsuleCollider.isTrigger = false;

            // Initialize rigidbody
            Rigidbody rigidbody = this.GetComponent<Rigidbody>();
            Assert.IsNotNull(rigidbody);
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            rigidbody.interpolation = RigidbodyInterpolation.None;

            // Compute the cosinus of the max walk angle
            this.minWalkCos = Mathf.Cos(this.maxWalkAngle * Mathf.Deg2Rad);

            // Compute the collision layer
            int layer = this.gameObject.layer;
            for (int i = 0; i < 32; ++i)
            {
                if (!Physics.GetIgnoreLayerCollision(layer, i))
                {
                    this.collisionLayerMask |= 1 << i;
                }
            }

            // First depenetration and ground update
            this.Move(Vector3.zero);
        }

        private void UpdateCachedData()
        {
            Vector3 scale = this.transform.lossyScale;
            float heightScale = scale.y;
            float radiusScale = Mathf.Max(scale.x, scale.z);
            this.position = this.transform.position;
            this.rotation = this.transform.rotation;
            this.up = this.transform.up;
            this.center = this.CapsuleCollider.center;
            this.halfHeight = this.CapsuleCollider.height * 0.5f * heightScale;
            this.radius = this.CapsuleCollider.radius * radiusScale;
            float offsetDistance = Mathf.Max(this.halfHeight - this.radius, 0f);
            this.point1Offset = this.center + (this.up * offsetDistance);
            this.point2Offset = this.center - (this.up * offsetDistance);
        }

        private void MoveInternal(Vector3 move)
        {
            Vector3 currentMove = move;
            float remaingFraction = 1f;
            Hit previousHit = this.groundHit;
            Hit previousPreviousHit = new Hit();

            for (int i = 0; i < 3; ++i)
            {
                Vector3 wantedMove = currentMove * remaingFraction;
                Vector3 endPosition = this.position + wantedMove;

                // Find collision along the move vector
                Hit currentHit = this.Trace(this.position, endPosition);
                if (!currentHit.IsValid)
                {
                    // Didn't hit anything, move position to the end position
                    this.position = endPosition;
                    this.velocity = currentMove / Time.deltaTime;
                    return;
                }

                // Move to the hit
                if (currentHit.Distance > CharacterCollider.SkinWidth)
                {
                    float traveledDistance = currentHit.Distance - CharacterCollider.SkinWidth;
                    float fraction = traveledDistance / wantedMove.magnitude;
                    this.position += wantedMove * fraction;
                    remaingFraction -= fraction;
                }

                // Handle the collision
                currentMove = this.Collide(currentMove, currentHit);

                // Check if we are moving into to the previous hits normals
                if (previousHit.IsValid && Vector3.Dot(currentMove, previousHit.Normal) < 0f)
                {
                    Vector3 n = Vector3.Cross(currentHit.Normal, previousHit.Normal);
                    float nLength = n.magnitude;
                    if (nLength > 0.05f) // Need to have some epsilon here because unity CapsuleCast doesn't return a precise hit normal
                    {
                        currentMove = Vector3.Project(currentMove, n / nLength);
                    }
                    else
                    {
                        currentMove = Vector3.ProjectOnPlane(currentMove, previousHit.Normal);
                    }

                    if (previousPreviousHit.IsValid && Vector3.Dot(currentMove, previousPreviousHit.Normal) < 0f)
                    {
                        this.velocity = Vector3.zero;
                        return;
                    }
                }

                if (currentMove == Vector3.zero)
                {
                    this.velocity = Vector3.zero;
                    return;
                }

                // If we start going backward, stop the move
                if (Vector3.Dot(currentMove, move) < 0f)
                {
                    this.velocity = Vector3.zero;
                    return;
                }

                // Update the previous hits
                previousPreviousHit = previousHit;
                previousHit = currentHit;
            }

            // Too much collision for this move. Don't go further.
            this.velocity = currentMove / Time.deltaTime;
        }

        private Vector3 Collide(Vector3 move, Hit hit)
        {
            float d = Vector3.Dot(hit.Normal, this.up);
            bool isGround = d >= this.minWalkCos;
            if (isGround)
            {
                // Collide on to ground
                if (this.isGrounded)
                {
                    move = this.CollideFromGroundToGround(move, hit.Normal);
                }
                else
                {
                    move = this.CollideFromAirToGround(move, hit.Normal);
                }

                // Update ground data
                this.isGrounded = true;
                this.groundHit = hit;
            }
            else
            {
                // Collide on to wall
                move = this.CollideOntoWall(move, hit.Normal);

                // Update ground data
                if (this.isGrounded)
                {
                    if (this.IsGettingThrownOfTheGround(move))
                    {
                        // Getting thrown off the ground
                        this.isGrounded = false;
                        this.groundHit = hit;
                    }
                }
                else if (d > 0f)
                {
                    this.groundHit = hit;
                }
            }

            return move;
        }

        private Vector3 CollideFromGroundToGround(Vector3 move, Vector3 groundNormal)
        {
            Vector3 newDirection = CharacterMovementHelper.ProjectVectorOnGroundPlane(move, groundNormal, this.up).normalized;
            return newDirection * move.magnitude;
        }

        private Vector3 CollideFromAirToGround(Vector3 move, Vector3 groundNormal)
        {
            Vector3 newDirection = CharacterMovementHelper.ProjectVectorOnGroundPlane(move, groundNormal, this.up).normalized;
            float horizontalSpeed = Vector3.ProjectOnPlane(move, this.up).magnitude;
            return newDirection * horizontalSpeed;
        }

        private Vector3 CollideOntoWall(Vector3 move, Vector3 wallNormal)
        {
            return Vector3.ProjectOnPlane(move, wallNormal);
        }

        private bool IsGettingThrownOfTheGround(Vector3 move)
        {
            float moveLength = move.magnitude;
            if (moveLength < 0.01f)
            {
                return false;
            }

            Vector3 normaliedMove = move / moveLength;
            return Vector3.Dot(normaliedMove, this.groundHit.Normal) > 0.01f;
        }

        private void UpdateGround()
        {
            const float TraceStartOffset = 0.01f;
            const float TraceDistance = (CharacterCollider.SkinWidth * 3f) + TraceStartOffset;
            Vector3 traceVector = this.up * -TraceDistance;
            Vector3 startPosition = this.position + this.up * TraceStartOffset;
            Vector3 endPosition = startPosition + traceVector;

            // Trace in ground direction
            this.groundHit = this.Trace(startPosition, endPosition);
            if (!this.groundHit.IsValid)
            {
                // Don't hit anything
                this.isGrounded = false;
                return;
            }

            this.groundHit.Distance -= TraceStartOffset;

            // Check if getting thrown off the ground
            if (this.IsGettingThrownOfTheGround(this.velocity))
            {
                this.isGrounded = false;
                this.groundHit.IsValid = false;
                return;
            }

            // Slopes that are too steep will not be considered on ground
            bool newIsGrounded = Vector3.Dot(this.groundHit.Normal, this.up) >= this.minWalkCos;
            if (newIsGrounded)
            {
                if (this.isGrounded)
                {
                    this.velocity = this.CollideFromGroundToGround(this.velocity, this.groundHit.Normal);
                }
                else
                {
                    this.velocity = this.CollideFromAirToGround(this.velocity, this.groundHit.Normal);
                }
            }

            // Snap to the ground
            this.SnapToGround(this.groundHit.Distance);
            this.isGrounded = newIsGrounded;
        }

        private void SnapToGround(float currentGroundDistance)
        {
            // Compute the wanted position
            float wantedGroundDistance = CharacterCollider.SkinWidth;
            float offset = wantedGroundDistance - currentGroundDistance;
            Vector3 newPosition = this.position + (this.up * offset);

            // If we are moving up, ensure that we don't end up inside a collider
            if (offset > 0f)
            {
                Hit hitUp = this.Trace(this.position, newPosition);
                if (hitUp.IsValid)
                {
                    offset = (hitUp.Distance - currentGroundDistance) * 0.5f;
                    newPosition = this.position + (this.up * offset);
                }
            }

            // Update the position
            this.position = newPosition;
        }

        private Hit Trace(Vector3 startPosition, Vector3 endPosition)
        {
            if (startPosition == endPosition)
            {
                return default;
            }

            // Compute the capsule cast parameters
            Vector3 point1 = startPosition + this.point1Offset;
            Vector3 point2 = startPosition + this.point2Offset;
            Vector3 direction = endPosition - startPosition;
            float distance = direction.magnitude;

            // Capsule cast to find collision
            RaycastHit hit;
            bool hitResult = Physics.CapsuleCast(point1, point2, this.radius, direction, out hit, distance, this.collisionLayerMask, QueryTriggerInteraction.Ignore);
            if (hitResult)
            {
                return new Hit()
                {
                    IsValid = true,
                    Collider = hit.collider,
                    Position = hit.point,
                    Normal = hit.normal,
                    Distance = hit.distance,
                };
            }

            return default;
        }

        private void Depenetrate()
        {
            // Find overlapped colliders
            Vector3 point1 = this.position + this.point1Offset;
            Vector3 point2 = this.position + this.point2Offset;
            int colliderCount = Physics.OverlapCapsuleNonAlloc(point1, point2, this.radius, overlappedColliders, this.collisionLayerMask, QueryTriggerInteraction.Ignore);

            // Compute the translation for depenetration
            Vector3 translation = Vector3.zero;
            int translationCount = 0;
            for (int i = 0; i < colliderCount; ++i)
            {
                Collider collider = overlappedColliders[i];
                if (collider == this.CapsuleCollider)
                {
                    continue;
                }

                Vector3 direction;
                float distance;
                if (Physics.ComputePenetration(this.CapsuleCollider, this.position, this.rotation, collider, collider.transform.position, collider.transform.rotation, out direction, out distance))
                {
                    translation += direction * (distance + 0.005f);
                    ++translationCount;
                }
            }

            if (translationCount > 0)
            {
                translation /= translationCount;
                this.position += translation;
            }
        }

        private struct Hit
        {
            public bool IsValid;
            public Collider Collider;
            public Vector3 Position;
            public Vector3 Normal;
            public float Distance;
        }
    }
}
