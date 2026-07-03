using NUnit.Framework;
using UnityEngine;

namespace OwariNakiTobira.Tests
{
    public sealed class OrthographicViewportWorldConverterTests
    {
        [Test]
        public void ViewportRectConvertsToOrthographicWorldRect()
        {
            Rect viewportRect = new Rect(0.25f, 0.25f, 0.5f, 0.5f);

            Rect worldRect = OrthographicViewportWorldConverter.ViewportRectToWorldRect(
                viewportRect,
                new Vector2(10f, 5f),
                5f,
                2f);

            Assert.AreEqual(5f, worldRect.xMin, 0.0001f);
            Assert.AreEqual(15f, worldRect.xMax, 0.0001f);
            Assert.AreEqual(2.5f, worldRect.yMin, 0.0001f);
            Assert.AreEqual(7.5f, worldRect.yMax, 0.0001f);
        }

        [Test]
        public void ViewportRectConvertsToWorldBoundsOnGameplayPlane()
        {
            GameObject cameraObject = new GameObject("Camera");
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 4f;
                camera.aspect = 1.5f;
                camera.transform.position = new Vector3(2f, 3f, -10f);

                bool result = OrthographicViewportWorldConverter.TryViewportRectToWorldBounds(
                    new Rect(0f, 0f, 0.5f, 0.5f),
                    camera,
                    0f,
                    1.25f,
                    out Bounds bounds);

                Assert.IsTrue(result);
                Assert.AreEqual(1.25f, bounds.size.z, 0.0001f);
                Assert.AreEqual(0f, bounds.center.z, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
