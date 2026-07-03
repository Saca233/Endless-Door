using UnityEngine;

namespace OwariNakiTobira
{
    public static class OrthographicViewportWorldConverter
    {
        public static Rect ViewportRectToWorldRect(Rect viewportRect, Vector2 cameraCenter, float orthographicSize, float aspect)
        {
            float worldHeight = Mathf.Max(0f, orthographicSize) * 2f;
            float worldWidth = worldHeight * Mathf.Max(0.0001f, aspect);
            float xMin = cameraCenter.x - worldWidth * 0.5f + viewportRect.xMin * worldWidth;
            float xMax = cameraCenter.x - worldWidth * 0.5f + viewportRect.xMax * worldWidth;
            float yMin = cameraCenter.y - worldHeight * 0.5f + viewportRect.yMin * worldHeight;
            float yMax = cameraCenter.y - worldHeight * 0.5f + viewportRect.yMax * worldHeight;
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        public static bool TryViewportRectToWorldRect(Rect viewportRect, Camera orthographicCamera, out Rect worldRect)
        {
            worldRect = Rect.zero;
            if (orthographicCamera == null || !orthographicCamera.orthographic)
            {
                return false;
            }

            worldRect = ViewportRectToWorldRect(
                viewportRect,
                new Vector2(orthographicCamera.transform.position.x, orthographicCamera.transform.position.y),
                orthographicCamera.orthographicSize,
                orthographicCamera.aspect);

            return worldRect.width > 0f && worldRect.height > 0f;
        }

        public static bool TryViewportRectToWorldBounds(Rect viewportRect, Camera orthographicCamera, float gameplayPlaneZ, float depth, out Bounds bounds)
        {
            bounds = default;
            if (!TryViewportRectToWorldRect(viewportRect, orthographicCamera, out Rect worldRect))
            {
                return false;
            }

            Vector3 center = new Vector3(worldRect.center.x, worldRect.center.y, gameplayPlaneZ);
            Vector3 size = new Vector3(worldRect.width, worldRect.height, Mathf.Max(0.01f, depth));
            bounds = new Bounds(center, size);
            return true;
        }
    }
}
