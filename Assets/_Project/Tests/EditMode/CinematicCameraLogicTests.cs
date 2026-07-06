using NUnit.Framework;
using UnityEngine;

namespace OwariNakiTobira.Tests
{
    public sealed class CinematicCameraLogicTests
    {
        [Test]
        public void DeadZoneKeepsCameraStill()
        {
            float center = CinematicSideScrollerCameraController.CalculateAxisCenterWithZones(0f, 0.4f, 2f, 4f);

            Assert.AreEqual(0f, center, 0.0001f);
        }

        [Test]
        public void SoftZoneMovesCameraGradually()
        {
            float center = CinematicSideScrollerCameraController.CalculateAxisCenterWithZones(0f, 1.5f, 2f, 4f);

            Assert.Greater(center, 0f);
            Assert.Less(center, 0.5f);
        }

        [Test]
        public void LookAheadUsesRunDistanceAtRunSpeed()
        {
            float lookAhead = CinematicSideScrollerCameraController.CalculateLookAheadTarget(
                5f,
                1f,
                0.1f,
                4f,
                1.25f,
                2f,
                0.75f,
                out float direction);

            Assert.AreEqual(2f, lookAhead, 0.0001f);
            Assert.AreEqual(1f, direction, 0.0001f);
        }

        [Test]
        public void LookAheadUsesStoppingDistanceWhenIdle()
        {
            float lookAhead = CinematicSideScrollerCameraController.CalculateLookAheadTarget(
                0f,
                -1f,
                0.1f,
                4f,
                1.25f,
                2f,
                0.75f,
                out float direction);

            Assert.AreEqual(-0.75f, lookAhead, 0.0001f);
            Assert.AreEqual(-1f, direction, 0.0001f);
        }

        [Test]
        public void DisabledYFollowKeepsCurrentY()
        {
            Vector3 desired = CinematicSideScrollerCameraController.CalculateDesiredCenter(
                new Vector3(0f, 3f, 2f),
                new Vector3(8f, -5f, 0f),
                new Vector2(10f, 6f),
                new Vector2(0.18f, -0.15f),
                Vector2.zero,
                Vector2.zero,
                false,
                0f,
                -10f);

            Assert.AreEqual(3f, desired.y, 0.0001f);
        }

        [Test]
        public void FixedZPositionOverridesCurrentAndTargetZ()
        {
            Vector3 desired = CinematicSideScrollerCameraController.CalculateDesiredCenter(
                new Vector3(0f, 3f, 25f),
                new Vector3(8f, -5f, 12f),
                new Vector2(10f, 6f),
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                true,
                0f,
                -10f);

            Assert.AreEqual(-10f, desired.z, 0.0001f);
        }

        [Test]
        public void CameraBoundsClampWithOrthographicSizeAndAspect()
        {
            Vector2 halfExtents = CameraBounds2D.CalculateOrthographicHalfExtents(6f, 16f / 9f);
            Vector3 clamped = CameraBounds2D.ClampCameraCenter(
                new Vector3(40f, -10f, -10f),
                new Vector2(-22f, -4f),
                new Vector2(34f, 12f),
                halfExtents);

            Assert.AreEqual(34f - halfExtents.x, clamped.x, 0.0001f);
            Assert.AreEqual(2f, clamped.y, 0.0001f);
        }

        [Test]
        public void NarrowBoundsCollapseToMiddleWhenCameraIsWiderThanBounds()
        {
            Vector3 clamped = CameraBounds2D.ClampCameraCenter(
                new Vector3(50f, 20f, -10f),
                new Vector2(-2f, -1f),
                new Vector2(2f, 1f),
                new Vector2(8f, 4f));

            Assert.AreEqual(0f, clamped.x, 0.0001f);
            Assert.AreEqual(0f, clamped.y, 0.0001f);
        }

        [Test]
        public void ParallaxFarLayerMovesSlowerOnScreen()
        {
            Vector3 position = ParallaxLayer.CalculateParallaxPosition(
                Vector3.zero,
                new Vector3(10f, 4f, 0f),
                new Vector2(0.15f, 0.15f),
                false);

            Assert.AreEqual(8.5f, position.x, 0.0001f);
            Assert.AreEqual(0f, position.y, 0.0001f);
        }

        [Test]
        public void ParallaxCanApplyYWhenEnabled()
        {
            Vector3 position = ParallaxLayer.CalculateParallaxPosition(
                Vector3.zero,
                new Vector3(10f, 4f, 0f),
                new Vector2(0.5f, 0.25f),
                true);

            Assert.AreEqual(5f, position.x, 0.0001f);
            Assert.AreEqual(3f, position.y, 0.0001f);
        }
    }
}
