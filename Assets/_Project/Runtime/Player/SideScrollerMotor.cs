using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class SideScrollerMotor : MonoBehaviour
    {
        private const float NoRecordedTime = -9999f;

        [Header("Movement")]
        [SerializeField] private float maximumSpeed = 6f;
        [SerializeField] private float acceleration = 70f;
        [SerializeField] private float deceleration = 90f;
        [SerializeField] private float jumpVelocity = 8f;
        [SerializeField, Range(0.1f, 1f)] private float jumpReleaseVelocityMultiplier = 0.45f;

        [Header("Jump Grace")]
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBufferTime = 0.12f;

        [Header("Ground Detection")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private Vector3 fallbackGroundCheckOffset = new Vector3(0f, -1.05f, 0f);
        [SerializeField] private float groundCheckRadius = 0.22f;
        [SerializeField] private LayerMask groundLayers = ~0;

        private Rigidbody body;
        private float horizontalInput;
        private float lastGroundedTime = NoRecordedTime;
        private float lastJumpPressedTime = NoRecordedTime;
        private bool jumpCutRequested;

        public bool IsGrounded { get; private set; }
        public float HorizontalInput => horizontalInput;
        public float HorizontalSpeed => body != null ? body.linearVelocity.x : 0f;
        public float VerticalSpeed => body != null ? body.linearVelocity.y : 0f;
        public bool HasBufferedJump => JumpTimingUtility.IsJumpBuffered(Time.time - lastJumpPressedTime, jumpBufferTime);
        public bool CanUseCoyoteTime => JumpTimingUtility.IsWithinCoyoteTime(Time.time - lastGroundedTime, coyoteTime);
        public bool CanJump => IsGrounded || CanUseCoyoteTime;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            ConfigureRigidbody();
            RefreshGrounded(Time.time);
        }

        private void OnValidate()
        {
            maximumSpeed = Mathf.Max(0f, maximumSpeed);
            acceleration = Mathf.Max(0f, acceleration);
            deceleration = Mathf.Max(0f, deceleration);
            jumpVelocity = Mathf.Max(0f, jumpVelocity);
            coyoteTime = Mathf.Max(0f, coyoteTime);
            jumpBufferTime = Mathf.Max(0f, jumpBufferTime);
            groundCheckRadius = Mathf.Max(0.01f, groundCheckRadius);

            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            if (body != null)
            {
                ConfigureRigidbody();
            }
        }

        private void FixedUpdate()
        {
            RefreshGrounded(Time.time);
            TryConsumeBufferedJump(Time.time);
            ApplyHorizontalMovement(Time.fixedDeltaTime);
            ConstrainToGameplayPlane();
        }

        public void SetHorizontalInput(float input)
        {
            horizontalInput = Mathf.Clamp(input, -1f, 1f);
        }

        public void QueueJump(float time)
        {
            lastJumpPressedTime = time;
            jumpCutRequested = false;
        }

        public void RequestJumpCut()
        {
            jumpCutRequested = true;
            if (body == null || body.linearVelocity.y <= 0f)
            {
                return;
            }

            Vector3 velocity = body.linearVelocity;
            velocity.y *= jumpReleaseVelocityMultiplier;
            body.linearVelocity = velocity;
            jumpCutRequested = false;
        }

        public void ClearQueuedJump()
        {
            lastJumpPressedTime = NoRecordedTime;
            jumpCutRequested = false;
        }

        public bool TryConsumeBufferedJump(float time)
        {
            if (!JumpTimingUtility.ShouldJump(IsGrounded, time - lastGroundedTime, coyoteTime, time - lastJumpPressedTime, jumpBufferTime))
            {
                return false;
            }

            Vector3 velocity = body.linearVelocity;
            velocity.y = jumpCutRequested ? jumpVelocity * jumpReleaseVelocityMultiplier : jumpVelocity;
            body.linearVelocity = velocity;
            IsGrounded = false;
            lastGroundedTime = NoRecordedTime;
            lastJumpPressedTime = NoRecordedTime;
            jumpCutRequested = false;
            return true;
        }

        private void ApplyHorizontalMovement(float fixedDeltaTime)
        {
            Vector3 velocity = body.linearVelocity;
            float targetSpeed = horizontalInput * maximumSpeed;
            float rate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
            velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, rate * fixedDeltaTime);
            velocity.z = 0f;
            body.linearVelocity = velocity;
        }

        private void RefreshGrounded(float time)
        {
            Vector3 checkPosition = groundCheck != null ? groundCheck.position : transform.TransformPoint(fallbackGroundCheckOffset);
            IsGrounded = Physics.CheckSphere(checkPosition, groundCheckRadius, groundLayers, QueryTriggerInteraction.Ignore);
            if (IsGrounded)
            {
                lastGroundedTime = time;
            }
        }

        private void ConstrainToGameplayPlane()
        {
            Vector3 position = body.position;
            if (!Mathf.Approximately(position.z, 0f))
            {
                position.z = 0f;
                body.position = position;
            }

            Vector3 velocity = body.linearVelocity;
            if (!Mathf.Approximately(velocity.z, 0f))
            {
                velocity.z = 0f;
                body.linearVelocity = velocity;
            }
        }

        private void ConfigureRigidbody()
        {
            body.useGravity = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        }
    }
}
