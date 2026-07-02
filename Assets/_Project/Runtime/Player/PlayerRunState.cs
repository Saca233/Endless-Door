using UnityEngine;

namespace OwariNakiTobira
{
    public sealed class PlayerRunState : PlayerState
    {
        public PlayerRunState(PlayerStateMachine machine) : base(machine)
        {
        }

        public override PlayerStateId Id => PlayerStateId.Run;

        public override void Tick(float deltaTime)
        {
            Machine.ApplyHorizontalInput();

            if (Machine.ShouldStartJump())
            {
                Machine.TransitionTo(PlayerStateId.Jump);
                return;
            }

            if (!Motor.IsGrounded)
            {
                Machine.TransitionTo(PlayerStateId.Fall);
                return;
            }

            if (Mathf.Abs(Machine.HorizontalInput) <= Machine.MoveDeadZone)
            {
                Machine.TransitionTo(PlayerStateId.Idle);
            }
        }
    }
}
