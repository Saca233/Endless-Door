using NUnit.Framework;
using UnityEngine;

namespace OwariNakiTobira.Tests
{
    public sealed class GameWindowCoordinateUtilityTests
    {
        [Test]
        public void ViewportToPointMapsIntoVisibleRect()
        {
            Rect visibleRect = new Rect(100f, 50f, 800f, 450f);
            Vector2 point = GameWindowCoordinateUtility.ViewportToPoint(visibleRect, new Vector2(0.25f, 0.5f));

            Assert.AreEqual(300f, point.x);
            Assert.AreEqual(275f, point.y);
        }

        [Test]
        public void PointToViewportRejectsOutsidePoint()
        {
            Rect visibleRect = new Rect(100f, 50f, 800f, 450f);

            Assert.IsFalse(GameWindowCoordinateUtility.TryPointToViewport(visibleRect, new Vector2(20f, 20f), out _));
        }

        [Test]
        public void FitAspectLetterboxesWideContainer()
        {
            Rect fitted = GameWindowCoordinateUtility.FitAspect(new Rect(0f, 0f, 1200f, 600f), 16f / 9f);

            Assert.AreEqual(1066.6666f, fitted.width, 0.01f);
            Assert.AreEqual(600f, fitted.height, 0.01f);
            Assert.AreEqual(66.6666f, fitted.xMin, 0.01f);
        }
    }
}
