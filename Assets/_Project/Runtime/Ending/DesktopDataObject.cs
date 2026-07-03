using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class DesktopDataObject : MonoBehaviour, IRuntimeResettable
    {
        [SerializeField, Min(0f)] private float pullDelay;
        [SerializeField, Min(0.01f)] private float pullSpeed = 4f;
        [SerializeField, Min(0f)] private float rotationSpeed = 360f;
        [SerializeField] private AnimationCurve pullCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private int resetOrder = 40;

        private Transform pullTarget;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private Vector3 originalScale;
        private Vector3 pullStartPosition;
        private Quaternion pullStartRotation;
        private float pullElapsed;
        private float pullDuration;
        private bool originalCaptured;

        public int ResetOrder => resetOrder;
        public bool IsPulling { get; private set; }
        public bool IsPulled { get; private set; }

        private void Awake()
        {
            CaptureOriginalTransform();
        }

        public void CaptureOriginalTransform()
        {
            originalPosition = transform.position;
            originalRotation = transform.rotation;
            originalScale = transform.localScale;
            originalCaptured = true;
        }

        public void BeginPull(Transform target)
        {
            if (!originalCaptured)
            {
                CaptureOriginalTransform();
            }

            pullTarget = target;
            pullStartPosition = transform.position;
            pullStartRotation = transform.rotation;
            pullElapsed = 0f;
            float distance = pullTarget != null ? Vector3.Distance(transform.position, pullTarget.position) : 0f;
            pullDuration = Mathf.Max(0.01f, distance / Mathf.Max(0.01f, pullSpeed));
            IsPulling = pullTarget != null;
            IsPulled = false;
        }

        public bool StepPull(float deltaTime)
        {
            if (!IsPulling || pullTarget == null)
            {
                return false;
            }

            pullElapsed += Mathf.Max(0f, deltaTime);
            if (pullElapsed < pullDelay)
            {
                return true;
            }

            float t = Mathf.Clamp01((pullElapsed - pullDelay) / pullDuration);
            float curved = pullCurve != null ? pullCurve.Evaluate(t) : t;
            transform.position = Vector3.LerpUnclamped(pullStartPosition, pullTarget.position, curved);

            if (rotationSpeed > 0f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, pullTarget.rotation, rotationSpeed * deltaTime);
            }
            else
            {
                transform.rotation = Quaternion.SlerpUnclamped(pullStartRotation, pullTarget.rotation, curved);
            }

            if (t >= 1f)
            {
                IsPulling = false;
                IsPulled = true;
                return false;
            }

            return true;
        }

        public void RuntimeReset()
        {
            if (!originalCaptured)
            {
                CaptureOriginalTransform();
            }

            IsPulling = false;
            IsPulled = false;
            pullTarget = null;
            pullElapsed = 0f;
            transform.SetPositionAndRotation(originalPosition, originalRotation);
            transform.localScale = originalScale;
        }
    }
}
