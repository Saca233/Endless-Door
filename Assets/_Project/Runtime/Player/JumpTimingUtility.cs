namespace OwariNakiTobira
{
    public static class JumpTimingUtility
    {
        public static bool IsWithinCoyoteTime(float timeSinceGrounded, float coyoteTime)
        {
            return coyoteTime > 0f && timeSinceGrounded >= 0f && timeSinceGrounded <= coyoteTime;
        }

        public static bool IsJumpBuffered(float timeSinceJumpPressed, float jumpBufferTime)
        {
            return jumpBufferTime > 0f && timeSinceJumpPressed >= 0f && timeSinceJumpPressed <= jumpBufferTime;
        }

        public static bool ShouldJump(bool isGrounded, float timeSinceGrounded, float coyoteTime, float timeSinceJumpPressed, float jumpBufferTime)
        {
            return (isGrounded || IsWithinCoyoteTime(timeSinceGrounded, coyoteTime)) && IsJumpBuffered(timeSinceJumpPressed, jumpBufferTime);
        }
    }
}
