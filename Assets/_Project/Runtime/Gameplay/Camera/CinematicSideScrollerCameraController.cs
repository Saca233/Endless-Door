using UnityEngine;

namespace OwariNakiTobira
{
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public sealed class CinematicSideScrollerCameraController : MonoBehaviour
    {
        [SerializeField] private Transform cameraRig;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private Transform followTarget;
        [SerializeField] private CameraBounds2D bounds;
        [SerializeField, Min(0.1f)] private float orthographicSize = 6.25f;
        [SerializeField] private float fixedZPosition = -10f;
        [SerializeField] private bool followY;
        [SerializeField, Min(0f)] private float xSmoothTime = 0.35f;
        [SerializeField, Min(0f)] private float ySmoothTime = 0.8f;
        [SerializeField] private Vector2 targetScreenOffset = new Vector2(0.18f, -0.15f);
        [SerializeField, Min(0f)] private float walkLookAheadDistance = 1.25f;
        [SerializeField, Min(0f)] private float runLookAheadDistance = 2f;
        [SerializeField, Min(0f)] private float stoppingLookAheadDistance = 0.75f;
        [SerializeField, Min(0f)] private float lookAheadSmoothTime = 0.45f;
        [SerializeField, Min(0f)] private float walkSpeedThreshold = 0.1f;
        [SerializeField, Min(0f)] private float runSpeedThreshold = 4f;
        [SerializeField, Min(0f)] private float deadZoneWidth = 2.5f;
        [SerializeField, Min(0f)] private float deadZoneHeight = 1.5f;
        [SerializeField, Min(0f)] private float softZoneWidth = 4.25f;
        [SerializeField, Min(0f)] private float softZoneHeight = 2.75f;
        [SerializeField] private bool positionLocked;
        [SerializeField] private bool drawGizmos = true;

        private Vector3 smoothVelocity;
        private float lookAheadVelocity;
        private float currentLookAhead;
        private float lastMoveDirection = 1f;
        private Vector3 previousTargetPosition;
        private bool hasPreviousTargetPosition;
        private Vector3 lastDesiredCenter;

        public Transform CameraRig => cameraRig;
        public Camera GameplayCamera => gameplayCamera;
        public Transform FollowTarget => followTarget;
        public CameraBounds2D Bounds => bounds;
        public float OrthographicSize => orthographicSize;
        public bool FollowY => followY;
        public float FixedZPosition => fixedZPosition;
        public bool PositionLocked => positionLocked;

        private void Reset()
        {
            cameraRig = transform;
            gameplayCamera = GetComponentInChildren<Camera>();
            bounds = GetComponent<CameraBounds2D>();
            fixedZPosition = transform.position.z;
        }

        private void Awake()
        {
            ResolveReferences();
            ApplyCameraSettings();
        }

        private void LateUpdate()
        {
            if (followTarget == null || positionLocked)
            {
                return;
            }

            ResolveReferences();
            if (cameraRig == null)
            {
                return;
            }

            ApplyCameraSettings();
            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            float horizontalSpeed = CalculateHorizontalSpeed(deltaTime);
            float lookAheadTarget = CalculateLookAheadTarget(
                horizontalSpeed,
                lastMoveDirection,
                walkSpeedThreshold,
                runSpeedThreshold,
                walkLookAheadDistance,
                runLookAheadDistance,
                stoppingLookAheadDistance,
                out lastMoveDirection);

            currentLookAhead = lookAheadSmoothTime <= 0f
                ? lookAheadTarget
                : Mathf.SmoothDamp(currentLookAhead, lookAheadTarget, ref lookAheadVelocity, lookAheadSmoothTime, Mathf.Infinity, deltaTime);

            Vector3 desiredCenter = CalculateDesiredCenter(
                cameraRig.position,
                followTarget.position,
                GetCameraHalfExtents(),
                targetScreenOffset,
                new Vector2(deadZoneWidth, deadZoneHeight),
                new Vector2(softZoneWidth, softZoneHeight),
                followY,
                currentLookAhead,
                fixedZPosition);

            if (bounds != null)
            {
                desiredCenter = bounds.ClampCameraCenter(desiredCenter, gameplayCamera);
            }

            lastDesiredCenter = desiredCenter;
            cameraRig.position = SmoothCameraPosition(cameraRig.position, desiredCenter, deltaTime);
            previousTargetPosition = followTarget.position;
            hasPreviousTargetPosition = true;
        }

        public void SetFollowTarget(Transform target, bool snapImmediately = false)
        {
            followTarget = target;
            hasPreviousTargetPosition = false;
            smoothVelocity = Vector3.zero;
            lookAheadVelocity = 0f;

            if (snapImmediately)
            {
                SnapToTarget();
            }
        }

        public void SetPositionLocked(bool locked)
        {
            positionLocked = locked;
            smoothVelocity = Vector3.zero;
            lookAheadVelocity = 0f;
        }

        public void SnapToTarget()
        {
            if (followTarget == null)
            {
                return;
            }

            ResolveReferences();
            ApplyCameraSettings();
            Vector3 desiredCenter = CalculateDesiredCenter(
                cameraRig.position,
                followTarget.position,
                GetCameraHalfExtents(),
                targetScreenOffset,
                Vector2.zero,
                Vector2.zero,
                true,
                currentLookAhead,
                fixedZPosition);

            if (bounds != null)
            {
                desiredCenter = bounds.ClampCameraCenter(desiredCenter, gameplayCamera);
            }

            cameraRig.position = desiredCenter;
            lastDesiredCenter = desiredCenter;
            smoothVelocity = Vector3.zero;
            previousTargetPosition = followTarget.position;
            hasPreviousTargetPosition = true;
        }

        public static Vector3 CalculateDesiredCenter(
            Vector3 currentCenter,
            Vector3 targetPosition,
            Vector2 cameraHalfExtents,
            Vector2 targetScreenOffset,
            Vector2 deadZone,
            Vector2 softZone,
            bool followY,
            float lookAhead,
            float fixedZPosition)
        {
            Vector3 desiredFocus = new Vector3(
                targetPosition.x + lookAhead - targetScreenOffset.x * cameraHalfExtents.x,
                targetPosition.y - targetScreenOffset.y * cameraHalfExtents.y,
                fixedZPosition);

            return new Vector3(
                CalculateAxisCenterWithZones(currentCenter.x, desiredFocus.x, deadZone.x, softZone.x),
                followY ? CalculateAxisCenterWithZones(currentCenter.y, desiredFocus.y, deadZone.y, softZone.y) : currentCenter.y,
                fixedZPosition);
        }

        public static float CalculateAxisCenterWithZones(float currentCenter, float desiredFocus, float deadZoneSize, float softZoneSize)
        {
            float halfDeadZone = Mathf.Max(0f, deadZoneSize) * 0.5f;
            float halfSoftZone = Mathf.Max(halfDeadZone, softZoneSize * 0.5f);
            float delta = desiredFocus - currentCenter;
            float distance = Mathf.Abs(delta);
            if (distance <= halfDeadZone)
            {
                return currentCenter;
            }

            float movement = distance - halfDeadZone;
            if (halfSoftZone > halfDeadZone && distance < halfSoftZone)
            {
                movement *= Mathf.InverseLerp(halfDeadZone, halfSoftZone, distance);
            }

            return currentCenter + Mathf.Sign(delta) * movement;
        }

        public static float CalculateLookAheadTarget(
            float horizontalSpeed,
            float previousDirection,
            float walkSpeedThreshold,
            float runSpeedThreshold,
            float walkDistance,
            float runDistance,
            float stoppingDistance,
            out float nextDirection)
        {
            float safeDirection = Mathf.Abs(previousDirection) > 0.001f ? Mathf.Sign(previousDirection) : 1f;
            float absoluteSpeed = Mathf.Abs(horizontalSpeed);
            if (absoluteSpeed <= Mathf.Max(0f, walkSpeedThreshold))
            {
                nextDirection = safeDirection;
                return safeDirection * Mathf.Max(0f, stoppingDistance);
            }

            nextDirection = Mathf.Sign(horizontalSpeed);
            float distance = absoluteSpeed >= Mathf.Max(walkSpeedThreshold, runSpeedThreshold)
                ? runDistance
                : walkDistance;
            return nextDirection * Mathf.Max(0f, distance);
        }

        private Vector3 SmoothCameraPosition(Vector3 currentPosition, Vector3 desiredPosition, float deltaTime)
        {
            if (xSmoothTime <= 0f && (ySmoothTime <= 0f || !followY))
            {
                return desiredPosition;
            }

            Vector3 nextPosition = currentPosition;
            nextPosition.x = xSmoothTime <= 0f
                ? desiredPosition.x
                : Mathf.SmoothDamp(currentPosition.x, desiredPosition.x, ref smoothVelocity.x, xSmoothTime, Mathf.Infinity, deltaTime);

            nextPosition.y = !followY
                ? currentPosition.y
                : ySmoothTime <= 0f
                    ? desiredPosition.y
                    : Mathf.SmoothDamp(currentPosition.y, desiredPosition.y, ref smoothVelocity.y, ySmoothTime, Mathf.Infinity, deltaTime);

            nextPosition.z = fixedZPosition;
            return nextPosition;
        }

        private float CalculateHorizontalSpeed(float deltaTime)
        {
            if (!hasPreviousTargetPosition || followTarget == null)
            {
                return 0f;
            }

            return (followTarget.position.x - previousTargetPosition.x) / deltaTime;
        }

        private Vector2 GetCameraHalfExtents()
        {
            if (gameplayCamera != null && gameplayCamera.orthographic)
            {
                return CameraBounds2D.CalculateOrthographicHalfExtents(gameplayCamera.orthographicSize, gameplayCamera.aspect);
            }

            return CameraBounds2D.CalculateOrthographicHalfExtents(orthographicSize, 16f / 9f);
        }

        private void ResolveReferences()
        {
            if (cameraRig == null)
            {
                cameraRig = transform;
            }

            if (gameplayCamera == null)
            {
                gameplayCamera = GetComponentInChildren<Camera>();
            }

            if (bounds == null)
            {
                bounds = GetComponent<CameraBounds2D>();
            }
        }

        private void ApplyCameraSettings()
        {
            if (gameplayCamera == null)
            {
                return;
            }

            gameplayCamera.orthographic = true;
            gameplayCamera.orthographicSize = orthographicSize;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
            {
                return;
            }

            ResolveReferences();
            if (cameraRig == null)
            {
                return;
            }

            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.8f);
            DrawZone(cameraRig.position, deadZoneWidth, deadZoneHeight);
            Gizmos.color = new Color(1f, 0.75f, 0.25f, 0.6f);
            DrawZone(cameraRig.position, softZoneWidth, softZoneHeight);
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(lastDesiredCenter, 0.15f);
            if (followTarget != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(followTarget.position, followTarget.position + Vector3.right * currentLookAhead);
            }
        }

        private static void DrawZone(Vector3 center, float width, float height)
        {
            Gizmos.DrawWireCube(center, new Vector3(Mathf.Max(0f, width), Mathf.Max(0f, height), 0.1f));
        }
    }
}
