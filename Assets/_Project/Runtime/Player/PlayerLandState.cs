using UnityEngine;

namespace OwariNakiTobira
{
    public sealed class PlayerLandState : PlayerState
    {
        private float elapsed;

        public PlayerLandState(PlayerStateMachine machine) : base(machine)
        {
        }

        public override PlayerStateId Id => PlayerStateId.Land;

        public override void Enter()
        {
            elapsed = 0f;
        }

        public override void Tick(float deltaTime)
        {
            elapsed += deltaTime;
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

            if (elapsed < Machine.LandStateDuration)
            {
                return;
            }

            Machine.TransitionTo(Mathf.Abs(Machine.HorizontalInput) > Machine.MoveDeadZone ? PlayerStateId.Run : PlayerStateId.Idle);
        }
    }
}
