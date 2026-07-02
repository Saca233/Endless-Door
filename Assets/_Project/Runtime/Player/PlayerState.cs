namespace OwariNakiTobira
{
    public abstract class PlayerState
    {
        protected PlayerState(PlayerStateMachine machine)
        {
            Machine = machine;
        }

        public abstract PlayerStateId Id { get; }
        protected PlayerStateMachine Machine { get; }
        protected SideScrollerMotor Motor => Machine.Motor;

        public virtual void Enter()
        {
        }

        public virtual void Exit()
        {
        }

        public abstract void Tick(float deltaTime);
    }
}
