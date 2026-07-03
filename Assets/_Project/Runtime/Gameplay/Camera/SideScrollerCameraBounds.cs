using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class SideScrollerCameraBounds : MonoBehaviour
    {
        [SerializeField] private Vector2 minimumWorldPosition = new Vector2(-16f, -2f);
        [SerializeField] private Vector2 maximumWorldPosition = new Vector2(22f, 8f);
        [SerializeField] private bool accountForOrthographicSize = true;
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private Color gizmoColor = new Color(0.2f, 0.75f, 1f, 0.45f);

        public Vector2 MinimumWorldPosition => minimumWorldPosition;
        public Vector2 MaximumWorldPosition => maximumWorldPosition;
        public bool AccountForOrthographicSize => accountForOrthographicSize;

        private void OnValidate()
        {
            NormalizeBounds();
        }

        public void Configure(Vector2 minimum, Vector2 maximum, bool accountForOrthographicView)
        {
            minimumWorldPosition = minimum;
            maximumWorldPosition = maximum;
            accountForOrthographicSize = accountForOrthographicView;
            NormalizeBounds();
        }

        public Vector3 ClampCameraCenter(Vector3 desiredCenter, Camera orthographicCamera)
        {
            Vector2 halfExtents = Vector2.zero;
            if (accountForOrthographicSize && orthographicCamera != null && orthographicCamera.orthographic)
            {
                halfExtents = CalculateOrthographicHalfExtents(orthographicCamera.orthographicSize, orthographicCamera.aspect);
            }

            return ClampCameraCenter(desiredCenter, minimumWorldPosition, maximumWorldPosition, halfExtents);
        }

        public static Vector2 CalculateOrthographicHalfExtents(float orthographicSize, float aspect)
        {
            float safeSize = Mathf.Max(0f, orthographicSize);
            float safeAspect = Mathf.Max(0.0001f, aspect);
            return new Vector2(safeSize * safeAspect, safeSize);
        }

        public static Vector2 CalculateMinimumCameraCenter(Vector2 minimumWorldPosition, Vector2 maximumWorldPosition, Vector2 cameraHalfExtents)
        {
            Vector2 normalizedMinimum = Vector2.Min(minimumWorldPosition, maximumWorldPosition);
            Vector2 normalizedMaximum = Vector2.Max(minimumWorldPosition, maximumWorldPosition);
            return new Vector2(
                CalculateMinimumCenterAxis(normalizedMinimum.x, normalizedMaximum.x, cameraHalfExtents.x),
                CalculateMinimumCenterAxis(normalizedMinimum.y, normalizedMaximum.y, cameraHalfExtents.y));
        }

        public static Vector2 CalculateMaximumCameraCenter(Vector2 minimumWorldPosition, Vector2 maximumWorldPosition, Vector2 cameraHalfExtents)
        {
            Vector2 normalizedMinimum = Vector2.Min(minimumWorldPosition, maximumWorldPosition);
            Vector2 normalizedMaximum = Vector2.Max(minimumWorldPosition, maximumWorldPosition);
            return new Vector2(
                CalculateMaximumCenterAxis(normalizedMinimum.x, normalizedMaximum.x, cameraHalfExtents.x),
                CalculateMaximumCenterAxis(normalizedMinimum.y, normalizedMaximum.y, cameraHalfExtents.y));
        }

        public static Vector3 ClampCameraCenter(Vector3 desiredCenter, Vector2 minimumWorldPosition, Vector2 maximumWorldPosition, Vector2 cameraHalfExtents)
        {
            Vector2 minimumCenter = CalculateMinimumCameraCenter(minimumWorldPosition, maximumWorldPosition, cameraHalfExtents);
            Vector2 maximumCenter = CalculateMaximumCameraCenter(minimumWorldPosition, maximumWorldPosition, cameraHalfExtents);

            desiredCenter.x = Mathf.Clamp(desiredCenter.x, minimumCenter.x, maximumCenter.x);
            desiredCenter.y = Mathf.Clamp(desiredCenter.y, minimumCenter.y, maximumCenter.y);
            return desiredCenter;
        }

        private static float CalculateMinimumCenterAxis(float minimum, float maximum, float halfExtent)
        {
            float minCenter = minimum + Mathf.Max(0f, halfExtent);
            float maxCenter = maximum - Mathf.Max(0f, halfExtent);
            return minCenter <= maxCenter ? minCenter : (minimum + maximum) * 0.5f;
        }

        private static float CalculateMaximumCenterAxis(float minimum, float maximum, float halfExtent)
        {
            float minCenter = minimum + Mathf.Max(0f, halfExtent);
            float maxCenter = maximum - Mathf.Max(0f, halfExtent);
            return minCenter <= maxCenter ? maxCenter : (minimum + maximum) * 0.5f;
        }

        private void NormalizeBounds()
        {
            Vector2 normalizedMinimum = Vector2.Min(minimumWorldPosition, maximumWorldPosition);
            Vector2 normalizedMaximum = Vector2.Max(minimumWorldPosition, maximumWorldPosition);
            minimumWorldPosition = normalizedMinimum;
            maximumWorldPosition = normalizedMaximum;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
            {
                return;
            }

            Vector2 center = (minimumWorldPosition + maximumWorldPosition) * 0.5f;
            Vector2 size = maximumWorldPosition - minimumWorldPosition;
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(new Vector3(center.x, center.y, transform.position.z), new Vector3(size.x, size.y, 0.1f));
        }
    }
}
