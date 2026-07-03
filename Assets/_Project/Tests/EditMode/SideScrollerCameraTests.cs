using NUnit.Framework;
using UnityEngine;

namespace OwariNakiTobira.Tests
{
    public sealed class SideScrollerCameraTests
    {
        [Test]
        public void CameraCenterBoundsClampInsideWorldArea()
        {
            Vector3 desired = new Vector3(20f, -10f, -8f);

            Vector3 clamped = SideScrollerCameraBounds.ClampCameraCenter(
                desired,
                new Vector2(-10f, -4f),
                new Vector2(10f, 6f),
                new Vector2(2f, 1f));

            Assert.AreEqual(8f, clamped.x, 0.0001f);
            Assert.AreEqual(-3f, clamped.y, 0.0001f);
            Assert.AreEqual(-8f, clamped.z, 0.0001f);
        }

        [Test]
        public void OrthographicSizeCalculatesCameraCenterLimits()
        {
            Vector2 halfExtents = SideScrollerCameraBounds.CalculateOrthographicHalfExtents(4.5f, 16f / 9f);

            Vector2 minimumCenter = SideScrollerCameraBounds.CalculateMinimumCameraCenter(
                new Vector2(-16f, -2f),
                new Vector2(22f, 8f),
                halfExtents);
            Vector2 maximumCenter = SideScrollerCameraBounds.CalculateMaximumCameraCenter(
                new Vector2(-16f, -2f),
                new Vector2(22f, 8f),
                halfExtents);

            Assert.AreEqual(-8f, minimumCenter.x, 0.0001f);
            Assert.AreEqual(2.5f, minimumCenter.y, 0.0001f);
            Assert.AreEqual(14f, maximumCenter.x, 0.0001f);
            Assert.AreEqual(3.5f, maximumCenter.y, 0.0001f);
        }

        [Test]
        public void DisabledYFollowKeepsCurrentY()
        {
            Vector3 desired = SideScrollerCameraController.CalculateDesiredCenter(
                new Vector3(0f, 3f, -10f),
                new Vector3(5f, -6f, 0f),
                false,
                Vector2.zero,
                0f,
                0f,
                -10f);

            Assert.AreEqual(5f, desired.x, 0.0001f);
            Assert.AreEqual(3f, desired.y, 0.0001f);
        }

        [Test]
        public void FixedZPositionOverridesTargetAndCurrentZ()
        {
            Vector3 desired = SideScrollerCameraController.CalculateDesiredCenter(
                new Vector3(0f, 3f, 4f),
                new Vector3(5f, 6f, 99f),
                true,
                Vector2.zero,
                0f,
                0f,
                -12f);

            Assert.AreEqual(-12f, desired.z, 0.0001f);
        }
    }
}
