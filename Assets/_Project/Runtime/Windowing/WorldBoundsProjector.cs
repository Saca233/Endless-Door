using UnityEngine;

namespace OwariNakiTobira
{
    public static class WorldBoundsProjector
    {
        public const int CornerCount = 8;

        public static bool TryProjectBoundsToDesktopRect(Bounds bounds, Camera gameplayCamera, GameWindowView gameWindowView, Vector3[] cornerBuffer, out Rect desktopScreenRect)
        {
            desktopScreenRect = Rect.zero;
            if (gameplayCamera == null || gameWindowView == null)
            {
                return false;
            }

            if (!TryProjectBoundsToViewportRect(bounds, gameplayCamera, cornerBuffer, out Rect viewportRect))
            {
                return false;
            }

            Rect visibleRawImageRect = gameWindowView.GetVisibleScreenRect();
            if (ScreenRectUtility.GetArea(visibleRawImageRect) <= 0f)
            {
                return false;
            }

            Vector2 min = GameWindowCoordinateUtility.ViewportToPoint(visibleRawImageRect, viewportRect.min);
            Vector2 max = GameWindowCoordinateUtility.ViewportToPoint(visibleRawImageRect, viewportRect.max);
            desktopScreenRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return ScreenRectUtility.GetArea(desktopScreenRect) > 0f;
        }

        public static bool TryProjectBoundsToViewportRect(Bounds bounds, Camera gameplayCamera, Vector3[] cornerBuffer, out Rect viewportRect)
        {
            viewportRect = Rect.zero;
            if (gameplayCamera == null)
            {
                return false;
            }

            if (cornerBuffer == null || cornerBuffer.Length < CornerCount)
            {
                cornerBuffer = new Vector3[CornerCount];
            }

            FillCorners(bounds, cornerBuffer);
            bool hasPointInFront = false;
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            for (int i = 0; i < CornerCount; i++)
            {
                Vector3 viewportPoint = gameplayCamera.WorldToViewportPoint(cornerBuffer[i]);
                if (viewportPoint.z <= 0f)
                {
                    continue;
                }

                hasPointInFront = true;
                Vector2 clipped = new Vector2(Mathf.Clamp01(viewportPoint.x), Mathf.Clamp01(viewportPoint.y));
                min = Vector2.Min(min, clipped);
                max = Vector2.Max(max, clipped);
            }

            if (!hasPointInFront)
            {
                return false;
            }

            viewportRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return ScreenRectUtility.GetArea(viewportRect) > 0f;
        }

        private static void FillCorners(Bounds bounds, Vector3[] corners)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            corners[0] = new Vector3(min.x, min.y, min.z);
            corners[1] = new Vector3(max.x, min.y, min.z);
            corners[2] = new Vector3(min.x, max.y, min.z);
            corners[3] = new Vector3(max.x, max.y, min.z);
            corners[4] = new Vector3(min.x, min.y, max.z);
            corners[5] = new Vector3(max.x, min.y, max.z);
            corners[6] = new Vector3(min.x, max.y, max.z);
            corners[7] = new Vector3(max.x, max.y, max.z);
        }
    }
}
