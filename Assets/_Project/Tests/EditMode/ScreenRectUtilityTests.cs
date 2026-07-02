using NUnit.Framework;
using UnityEngine;

namespace OwariNakiTobira.Tests
{
    public sealed class ScreenRectUtilityTests
    {
        [Test]
        public void NoRectangleOverlapHasZeroIntersectionAndCoverage()
        {
            Rect target = new Rect(0f, 0f, 100f, 100f);
            Rect covering = new Rect(120f, 0f, 100f, 100f);

            Rect intersection = ScreenRectUtility.GetIntersection(target, covering);

            Assert.AreEqual(0f, ScreenRectUtility.GetArea(intersection));
            Assert.AreEqual(0f, ScreenRectUtility.GetCoverageRatio(target, covering));
        }

        [Test]
        public void PartialOverlapReturnsExpectedIntersection()
        {
            Rect target = new Rect(0f, 0f, 100f, 100f);
            Rect covering = new Rect(50f, 50f, 100f, 100f);

            Rect intersection = ScreenRectUtility.GetIntersection(target, covering);

            Assert.AreEqual(50f, intersection.xMin);
            Assert.AreEqual(50f, intersection.yMin);
            Assert.AreEqual(50f, intersection.width);
            Assert.AreEqual(50f, intersection.height);
            Assert.AreEqual(2500f, ScreenRectUtility.GetIntersectionArea(target, covering));
        }

        [Test]
        public void FullOverlapCoversEntireTarget()
        {
            Rect target = new Rect(10f, 20f, 80f, 40f);
            Rect covering = new Rect(0f, 0f, 200f, 200f);

            Rect intersection = ScreenRectUtility.GetIntersection(target, covering);

            Assert.AreEqual(target, intersection);
            Assert.AreEqual(1f, ScreenRectUtility.GetCoverageRatio(target, covering));
        }

        [Test]
        public void CoverageRatioUsesTargetArea()
        {
            Rect target = new Rect(0f, 0f, 100f, 50f);
            Rect covering = new Rect(50f, 0f, 100f, 100f);

            float coverage = ScreenRectUtility.GetCoverageRatio(target, covering);

            Assert.AreEqual(0.5f, coverage);
        }
    }
}
