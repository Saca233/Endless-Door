namespace OwariNakiTobira
{
    public sealed class PlayerJumpState : PlayerState
    {
        public PlayerJumpState(PlayerStateMachine machine) : base(machine)
        {
        }

        public override PlayerStateId Id => PlayerStateId.Jump;

        public override void Enter()
        {
            Motor.TryConsumeBufferedJump(UnityEngine.Time.time);
        }

        public override void Tick(float deltaTime)
        {
            Machine.ApplyHorizontalInput();

            if (Motor.VerticalSpeed <= 0f)
            {
                Machine.TransitionTo(Motor.IsGrounded ? PlayerStateId.Land : PlayerStateId.Fall);
            }
        }
    }
}
