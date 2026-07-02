namespace OwariNakiTobira
{
    public sealed class PlayerFallState : PlayerState
    {
        public PlayerFallState(PlayerStateMachine machine) : base(machine)
        {
        }

        public override PlayerStateId Id => PlayerStateId.Fall;

        public override void Tick(float deltaTime)
        {
            Machine.ApplyHorizontalInput();

            if (Machine.ShouldStartJump())
            {
                Machine.TransitionTo(PlayerStateId.Jump);
                return;
            }

            if (Motor.IsGrounded)
            {
                Machine.TransitionTo(PlayerStateId.Land);
            }
        }
    }
}
