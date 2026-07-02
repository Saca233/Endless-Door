using UnityEngine;

namespace OwariNakiTobira
{
    public static class DesktopWindowGeometry
    {
        public static Vector2 ClampAnchoredPosition(Rect bounds, Vector2 desiredPosition, Vector2 windowSize, Vector2 pivot)
        {
            float minX = bounds.xMin + windowSize.x * pivot.x;
            float maxX = bounds.xMax - windowSize.x * (1f - pivot.x);
            float minY = bounds.yMin + windowSize.y * pivot.y;
            float maxY = bounds.yMax - windowSize.y * (1f - pivot.y);

            if (minX > maxX)
            {
                desiredPosition.x = bounds.center.x;
            }
            else
            {
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            }

            if (minY > maxY)
            {
                desiredPosition.y = bounds.center.y;
            }
            else
            {
                desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
            }

            return desiredPosition;
        }
    }
}
