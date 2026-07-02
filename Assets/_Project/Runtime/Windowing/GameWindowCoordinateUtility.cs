using UnityEngine;

namespace OwariNakiTobira
{
    public static class GameWindowCoordinateUtility
    {
        public static Vector2 ViewportToPoint(Rect visibleRect, Vector2 viewportPoint)
        {
            return new Vector2(
                visibleRect.xMin + visibleRect.width * viewportPoint.x,
                visibleRect.yMin + visibleRect.height * viewportPoint.y);
        }

        public static bool TryPointToViewport(Rect visibleRect, Vector2 point, out Vector2 viewportPoint)
        {
            viewportPoint = Vector2.zero;
            if (visibleRect.width <= 0f || visibleRect.height <= 0f || !visibleRect.Contains(point))
            {
                return false;
            }

            viewportPoint = new Vector2(
                (point.x - visibleRect.xMin) / visibleRect.width,
                (point.y - visibleRect.yMin) / visibleRect.height);
            return true;
        }

        public static Rect FitAspect(Rect container, float targetAspect)
        {
            if (container.width <= 0f || container.height <= 0f || targetAspect <= 0f)
            {
                return new Rect(container.center, Vector2.zero);
            }

            float containerAspect = container.width / container.height;
            if (containerAspect > targetAspect)
            {
                float width = container.height * targetAspect;
                return new Rect(container.center.x - width * 0.5f, container.yMin, width, container.height);
            }

            float height = container.width / targetAspect;
            return new Rect(container.xMin, container.center.y - height * 0.5f, container.width, height);
        }
    }
}
