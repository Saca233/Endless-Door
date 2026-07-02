using UnityEngine;

namespace OwariNakiTobira
{
    public sealed class PlayerIdleState : PlayerState
    {
        public PlayerIdleState(PlayerStateMachine machine) : base(machine)
        {
        }

        public override PlayerStateId Id => PlayerStateId.Idle;

        public override void Enter()
        {
            Motor.SetHorizontalInput(0f);
        }

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

            if (Mathf.Abs(Machine.HorizontalInput) > Machine.MoveDeadZone)
            {
                Machine.TransitionTo(PlayerStateId.Run);
            }
        }
    }
}
