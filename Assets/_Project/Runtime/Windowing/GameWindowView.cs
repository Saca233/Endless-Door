using UnityEngine;
using UnityEngine.UI;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class GameWindowView : MonoBehaviour
    {
        [SerializeField] private RawImage rawImage;
        [SerializeField] private bool preserveAspectRatio = true;
        [SerializeField] private Vector2Int renderResolution = new Vector2Int(1280, 720);

        private readonly Vector3[] worldCorners = new Vector3[4];

        public RawImage RawImage => rawImage;
        public RectTransform RawImageRect => rawImage != null ? rawImage.rectTransform : null;
        public bool PreserveAspectRatio => preserveAspectRatio;
        public Vector2Int RenderResolution => renderResolution;

        private void Awake()
        {
            ApplyAspectRatio();
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyAspectRatio();
        }

        public void SetRenderResolution(Vector2Int value)
        {
            renderResolution = new Vector2Int(Mathf.Max(1, value.x), Mathf.Max(1, value.y));
            ApplyAspectRatio();
        }

        public Rect GetVisibleScreenRect()
        {
            RectTransform rectTransform = RawImageRect;
            if (rectTransform == null)
            {
                return Rect.zero;
            }

            rectTransform.GetWorldCorners(worldCorners);
            return Rect.MinMaxRect(worldCorners[0].x, worldCorners[0].y, worldCorners[2].x, worldCorners[2].y);
        }

        public Vector2 ViewportToDesktopScreenPoint(Vector2 viewportPoint)
        {
            return GameWindowCoordinateUtility.ViewportToPoint(GetVisibleScreenRect(), viewportPoint);
        }

        public bool TryDesktopScreenPointToViewport(Vector2 screenPoint, out Vector2 viewportPoint)
        {
            return GameWindowCoordinateUtility.TryPointToViewport(GetVisibleScreenRect(), screenPoint, out viewportPoint);
        }

        public void ApplyAspectRatio()
        {
            RectTransform rectTransform = RawImageRect;
            if (rectTransform == null)
            {
                return;
            }

            RectTransform parent = rectTransform.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            if (!preserveAspectRatio)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                return;
            }

            float targetAspect = (float)renderResolution.x / renderResolution.y;
            Rect fitted = GameWindowCoordinateUtility.FitAspect(parent.rect, targetAspect);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = fitted.center;
            rectTransform.sizeDelta = fitted.size;
        }
    }
}
