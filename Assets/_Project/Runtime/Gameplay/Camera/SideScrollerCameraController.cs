using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class SideScrollerCameraController : MonoBehaviour
    {
        [SerializeField] private Transform cameraRig;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private Transform followTarget;
        [SerializeField] private SideScrollerCameraBounds bounds;
        [SerializeField] private bool followY;
        [SerializeField, Min(0f)] private float smoothTime = 0.15f;
        [SerializeField, Min(0f)] private float lookAheadDistance = 1.1f;
        [SerializeField, Min(0f)] private float deadZoneWidth = 0.75f;
        [SerializeField, Min(0f)] private float deadZoneHeight = 0.6f;
        [SerializeField] private float fixedZPosition = -10f;

        private Vector3 smoothVelocity;
        private Vector3 previousTargetPosition;
        private bool hasPreviousTargetPosition;
        private float lookDirection = 1f;

        public Transform CameraRig => cameraRig;
        public Camera GameplayCamera => gameplayCamera;
        public Transform FollowTarget => followTarget;
        public bool FollowY => followY;
        public float FixedZPosition => fixedZPosition;

        private void Reset()
        {
            cameraRig = transform;
            gameplayCamera = GetComponentInChildren<Camera>();
            bounds = GetComponent<SideScrollerCameraBounds>();
            fixedZPosition = transform.position.z;
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void LateUpdate()
        {
            if (followTarget == null)
            {
                return;
            }

            ResolveReferences();
            if (cameraRig == null)
            {
                return;
            }

            Vector3 desiredCenter = CalculateDesiredCenter();
            if (bounds != null)
            {
                desiredCenter = bounds.ClampCameraCenter(desiredCenter, gameplayCamera);
            }

            if (smoothTime <= 0f)
            {
                cameraRig.position = desiredCenter;
            }
            else
            {
                Vector3 nextPosition = Vector3.SmoothDamp(cameraRig.position, desiredCenter, ref smoothVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);
                nextPosition.z = fixedZPosition;
                cameraRig.position = nextPosition;
            }

            previousTargetPosition = followTarget.position;
            hasPreviousTargetPosition = true;
        }

        public void SetFollowTarget(Transform target, bool snapImmediately = false)
        {
            followTarget = target;
            hasPreviousTargetPosition = false;
            smoothVelocity = Vector3.zero;

            if (snapImmediately)
            {
                SnapToTarget();
            }
        }

        public void SnapToTarget()
        {
            if (followTarget == null)
            {
                return;
            }

            ResolveReferences();
            if (cameraRig == null)
            {
                return;
            }

            Vector3 desiredCenter = CalculateDesiredCenter();
            if (bounds != null)
            {
                desiredCenter = bounds.ClampCameraCenter(desiredCenter, gameplayCamera);
            }

            cameraRig.position = desiredCenter;
            smoothVelocity = Vector3.zero;
            previousTargetPosition = followTarget.position;
            hasPreviousTargetPosition = true;
        }

        public void SetFixedZPosition(float zPosition)
        {
            fixedZPosition = zPosition;
            if (cameraRig != null)
            {
                Vector3 position = cameraRig.position;
                position.z = fixedZPosition;
                cameraRig.position = position;
            }
        }

        public static Vector3 CalculateDesiredCenter(
            Vector3 currentCenter,
            Vector3 targetPosition,
            bool followY,
            Vector2 deadZone,
            float lookAheadDistance,
            float lookDirection,
            float fixedZPosition)
        {
            Vector3 desiredCenter = currentCenter;
            float signedLookAhead = Mathf.Abs(lookDirection) > 0.001f ? Mathf.Sign(lookDirection) * Mathf.Max(0f, lookAheadDistance) : 0f;
            float targetX = targetPosition.x + signedLookAhead;
            desiredCenter.x = CalculateAxisCenter(currentCenter.x, targetX, deadZone.x);

            if (followY)
            {
                desiredCenter.y = CalculateAxisCenter(currentCenter.y, targetPosition.y, deadZone.y);
            }

            desiredCenter.z = fixedZPosition;
            return desiredCenter;
        }

        private Vector3 CalculateDesiredCenter()
        {
            UpdateLookDirection();
            return CalculateDesiredCenter(
                cameraRig.position,
                followTarget.position,
                followY,
                new Vector2(deadZoneWidth, deadZoneHeight),
                lookAheadDistance,
                lookDirection,
                fixedZPosition);
        }

        private void UpdateLookDirection()
        {
            if (!hasPreviousTargetPosition || followTarget == null)
            {
                return;
            }

            float deltaX = followTarget.position.x - previousTargetPosition.x;
            if (Mathf.Abs(deltaX) > 0.001f)
            {
                lookDirection = Mathf.Sign(deltaX);
            }
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
                bounds = GetComponent<SideScrollerCameraBounds>();
            }
        }

        private static float CalculateAxisCenter(float currentCenter, float targetPosition, float deadZoneSize)
        {
            float halfDeadZone = Mathf.Max(0f, deadZoneSize) * 0.5f;
            float delta = targetPosition - currentCenter;
            if (Mathf.Abs(delta) <= halfDeadZone)
            {
                return currentCenter;
            }

            return targetPosition - Mathf.Sign(delta) * halfDeadZone;
        }
    }
}
