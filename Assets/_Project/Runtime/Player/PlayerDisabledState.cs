namespace OwariNakiTobira
{
    public sealed class PlayerDisabledState : PlayerState
    {
        public PlayerDisabledState(PlayerStateMachine machine) : base(machine)
        {
        }

        public override PlayerStateId Id => PlayerStateId.Disabled;

        public override void Enter()
        {
            Motor.SetHorizontalInput(0f);
            Motor.ClearQueuedJump();
        }

        public override void Tick(float deltaTime)
        {
            Motor.SetHorizontalInput(0f);

            if (!Machine.InputLocked)
            {
                Machine.TransitionTo(Machine.GetUnlockedLocomotionState());
            }
        }
    }
}
