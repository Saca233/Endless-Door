using NUnit.Framework;

namespace OwariNakiTobira.Tests
{
    public sealed class JumpTimingUtilityTests
    {
        [Test]
        public void CoyoteTimeAllowsJumpShortlyAfterLeavingGround()
        {
            Assert.IsTrue(JumpTimingUtility.IsWithinCoyoteTime(0.08f, 0.12f));
            Assert.IsFalse(JumpTimingUtility.IsWithinCoyoteTime(0.2f, 0.12f));
        }

        [Test]
        public void JumpBufferAllowsRecentlyPressedJump()
        {
            Assert.IsTrue(JumpTimingUtility.IsJumpBuffered(0.05f, 0.12f));
            Assert.IsFalse(JumpTimingUtility.IsJumpBuffered(0.2f, 0.12f));
        }

        [Test]
        public void ShouldJumpRequiresGraceAndBufferedPress()
        {
            Assert.IsTrue(JumpTimingUtility.ShouldJump(false, 0.08f, 0.12f, 0.04f, 0.12f));
            Assert.IsFalse(JumpTimingUtility.ShouldJump(false, 0.2f, 0.12f, 0.04f, 0.12f));
            Assert.IsFalse(JumpTimingUtility.ShouldJump(true, 0f, 0.12f, 0.2f, 0.12f));
        }
    }
}
