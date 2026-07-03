using UnityEngine;

namespace OwariNakiTobira
{
    public static class WindowIntersectionUtility
    {
        public static Rect GetIntersection(Rect first, Rect second)
        {
            return ScreenRectUtility.GetIntersection(first, second);
        }

        public static bool TryGetRectTransformIntersection(RectTransform first, RectTransform second, Canvas canvas, out Rect intersection)
        {
            intersection = Rect.zero;
            if (!ScreenRectUtility.TryGetRectTransformScreenRect(first, canvas, out Rect firstRect)
                || !ScreenRectUtility.TryGetRectTransformScreenRect(second, canvas, out Rect secondRect))
            {
                return false;
            }

            intersection = GetIntersection(firstRect, secondRect);
            return ScreenRectUtility.GetArea(intersection) > 0f;
        }

        public static bool TryGetNormalizedIntersection(Rect windowScreenRect, Rect mainGameWindowVisibleRect, out Rect normalizedRect)
        {
            Rect intersection = GetIntersection(windowScreenRect, mainGameWindowVisibleRect);
            return TryNormalizeRect(intersection, mainGameWindowVisibleRect, out normalizedRect);
        }

        public static bool TryNormalizeRect(Rect screenRect, Rect referenceRect, out Rect normalizedRect)
        {
            normalizedRect = Rect.zero;
            if (ScreenRectUtility.GetArea(screenRect) <= 0f || referenceRect.width <= 0f || referenceRect.height <= 0f)
            {
                return false;
            }

            float xMin = Mathf.Clamp01((screenRect.xMin - referenceRect.xMin) / referenceRect.width);
            float xMax = Mathf.Clamp01((screenRect.xMax - referenceRect.xMin) / referenceRect.width);
            float yMin = Mathf.Clamp01((screenRect.yMin - referenceRect.yMin) / referenceRect.height);
            float yMax = Mathf.Clamp01((screenRect.yMax - referenceRect.yMin) / referenceRect.height);

            if (xMax <= xMin || yMax <= yMin)
            {
                return false;
            }

            normalizedRect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
            return true;
        }

        public static bool TryGetErrorWindowOverlapInMainWindow(
            RectTransform errorWindowRoot,
            GameWindowView mainGameWindowView,
            Canvas errorWindowCanvas,
            out Rect normalizedRect)
        {
            normalizedRect = Rect.zero;
            if (errorWindowRoot == null || mainGameWindowView == null)
            {
                return false;
            }

            if (!ScreenRectUtility.TryGetRectTransformScreenRect(errorWindowRoot, errorWindowCanvas, out Rect errorWindowRect))
            {
                return false;
            }

            return TryGetNormalizedIntersection(errorWindowRect, mainGameWindowView.GetVisibleScreenRect(), out normalizedRect);
        }
    }
}
