using NUnit.Framework;
using UnityEngine;

namespace OwariNakiTobira.Tests
{
    public sealed class DesktopWindowGeometryTests
    {
        [Test]
        public void ClampAnchoredPositionKeepsWindowInsideBounds()
        {
            Rect bounds = new Rect(-500f, -300f, 1000f, 600f);
            Vector2 windowSize = new Vector2(200f, 100f);
            Vector2 pivot = new Vector2(0.5f, 0.5f);

            Vector2 clamped = DesktopWindowGeometry.ClampAnchoredPosition(bounds, new Vector2(600f, 400f), windowSize, pivot);

            Assert.AreEqual(400f, clamped.x);
            Assert.AreEqual(250f, clamped.y);
        }

        [Test]
        public void ClampAnchoredPositionCentersOversizedWindowOnAxis()
        {
            Rect bounds = new Rect(-100f, -100f, 200f, 200f);
            Vector2 clamped = DesktopWindowGeometry.ClampAnchoredPosition(bounds, new Vector2(80f, 80f), new Vector2(400f, 50f), new Vector2(0.5f, 0.5f));

            Assert.AreEqual(0f, clamped.x);
            Assert.AreEqual(75f, clamped.y);
        }
    }
}
