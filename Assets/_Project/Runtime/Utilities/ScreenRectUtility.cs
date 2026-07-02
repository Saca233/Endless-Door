using UnityEngine;

namespace OwariNakiTobira
{
    public static class ScreenRectUtility
    {
        public static bool TryGetRectTransformScreenRect(RectTransform rectTransform, Canvas canvas, out Rect screenRect)
        {
            screenRect = Rect.zero;
            if (rectTransform == null)
            {
                return false;
            }

            Camera eventCamera = GetEventCamera(canvas);
            return TryGetRectTransformScreenRect(rectTransform, eventCamera, null, out screenRect);
        }

        public static bool TryGetRectTransformScreenRect(RectTransform rectTransform, Camera eventCamera, out Rect screenRect)
        {
            return TryGetRectTransformScreenRect(rectTransform, eventCamera, null, out screenRect);
        }

        public static bool TryGetRectTransformScreenRect(RectTransform rectTransform, Camera eventCamera, Vector3[] cornerBuffer, out Rect screenRect)
        {
            screenRect = Rect.zero;
            if (rectTransform == null)
            {
                return false;
            }

            if (cornerBuffer == null || cornerBuffer.Length < 4)
            {
                cornerBuffer = new Vector3[4];
            }

            rectTransform.GetWorldCorners(cornerBuffer);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(eventCamera, cornerBuffer[0]);
            Vector2 max = min;
            for (int i = 1; i < 4; i++)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(eventCamera, cornerBuffer[i]);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            screenRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return screenRect.width > 0f && screenRect.height > 0f;
        }

        public static Rect GetIntersection(Rect a, Rect b)
        {
            float xMin = Mathf.Max(a.xMin, b.xMin);
            float yMin = Mathf.Max(a.yMin, b.yMin);
            float xMax = Mathf.Min(a.xMax, b.xMax);
            float yMax = Mathf.Min(a.yMax, b.yMax);

            if (xMax <= xMin || yMax <= yMin)
            {
                return Rect.zero;
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        public static float GetArea(Rect rect)
        {
            return Mathf.Max(0f, rect.width) * Mathf.Max(0f, rect.height);
        }

        public static float GetIntersectionArea(Rect a, Rect b)
        {
            return GetArea(GetIntersection(a, b));
        }

        public static float GetCoverageRatio(Rect targetRect, Rect coveringRect)
        {
            float targetArea = GetArea(targetRect);
            if (targetArea <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(GetIntersectionArea(targetRect, coveringRect) / targetArea);
        }

        private static Camera GetEventCamera(Canvas canvas)
        {
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return canvas.worldCamera;
        }
    }
}
