using NUnit.Framework;
using UnityEngine;

namespace OwariNakiTobira.Tests
{
    public sealed class WindowIntersectionUtilityTests
    {
        [Test]
        public void WindowIntersectionReturnsOverlap()
        {
            Rect first = new Rect(0f, 0f, 100f, 100f);
            Rect second = new Rect(50f, 25f, 100f, 50f);

            Rect intersection = WindowIntersectionUtility.GetIntersection(first, second);

            Assert.AreEqual(new Rect(50f, 25f, 50f, 50f), intersection);
        }

        [Test]
        public void WindowIntersectionReturnsZeroForNoOverlap()
        {
            Rect first = new Rect(0f, 0f, 100f, 100f);
            Rect second = new Rect(125f, 0f, 50f, 50f);

            Rect intersection = WindowIntersectionUtility.GetIntersection(first, second);

            Assert.AreEqual(Rect.zero, intersection);
        }

        [Test]
        public void NormalizedIntersectionUsesMainGameWindowRect()
        {
            Rect mainGameWindow = new Rect(100f, 50f, 400f, 200f);
            Rect errorWindow = new Rect(300f, 100f, 300f, 200f);

            bool result = WindowIntersectionUtility.TryGetNormalizedIntersection(errorWindow, mainGameWindow, out Rect normalized);

            Assert.IsTrue(result);
            Assert.AreEqual(0.5f, normalized.xMin, 0.0001f);
            Assert.AreEqual(1f, normalized.xMax, 0.0001f);
            Assert.AreEqual(0.25f, normalized.yMin, 0.0001f);
            Assert.AreEqual(1f, normalized.yMax, 0.0001f);
        }
    }
}
