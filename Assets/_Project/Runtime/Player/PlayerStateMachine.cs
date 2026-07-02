using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SideScrollerMotor))]
    public sealed class PlayerStateMachine : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private SideScrollerMotor motor;
        [SerializeField] private PlayerControlGate controlGate;
        [SerializeField] private PlayerFacingController facingController;
        [SerializeField] private PlayerAnimatorBridge animatorBridge;
        [SerializeField] private PlayerStateId initialState = PlayerStateId.Idle;
        [SerializeField] private float moveDeadZone = 0.05f;
        [SerializeField] private float landStateDuration = 0.08f;

        private readonly PlayerState[] states = new PlayerState[6];
        private PlayerState currentState;
        private bool initialized;

        public PlayerStateId CurrentStateId { get; private set; }
        public SideScrollerMotor Motor => motor;
        public float MoveDeadZone => moveDeadZone;
        public float LandStateDuration => landStateDuration;
        public bool InputLocked => controlGate != null && controlGate.IsLocked;

        public float HorizontalInput
        {
            get
            {
                if (InputLocked || inputReader == null)
                {
                    return 0f;
                }

                return Mathf.Clamp(inputReader.Movement.x, -1f, 1f);
            }
        }

        private void Awake()
        {
            ResolveReferences();
            CreateStates();
            SetState(initialState, true);
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            if (InputLocked)
            {
                motor.SetHorizontalInput(0f);
                motor.ClearQueuedJump();
                if (CurrentStateId != PlayerStateId.Disabled)
                {
                    TransitionTo(PlayerStateId.Disabled);
                }
            }
            else
            {
                ReadJumpInput();
            }

            currentState?.Tick(Time.deltaTime);
            facingController?.UpdateFacing(HorizontalInput);
            animatorBridge?.Apply(motor.HorizontalSpeed, motor.VerticalSpeed, motor.IsGrounded, CurrentStateId);
        }

        public bool TransitionTo(PlayerStateId targetState)
        {
            if (!initialized)
            {
                SetState(targetState, true);
                return true;
            }

            if (!PlayerStateTransitionRules.IsValid(CurrentStateId, targetState))
            {
                return false;
            }

            SetState(targetState, false);
            return true;
        }

        public bool CanTransitionTo(PlayerStateId targetState)
        {
            return PlayerStateTransitionRules.IsValid(CurrentStateId, targetState);
        }

        public void ApplyHorizontalInput()
        {
            motor.SetHorizontalInput(HorizontalInput);
        }

        public bool ShouldStartJump()
        {
            return !InputLocked && motor.HasBufferedJump && motor.CanJump;
        }

        public PlayerStateId GetUnlockedLocomotionState()
        {
            if (!motor.IsGrounded)
            {
                return PlayerStateId.Fall;
            }

            return Mathf.Abs(HorizontalInput) > moveDeadZone ? PlayerStateId.Run : PlayerStateId.Idle;
        }

        private void ResolveReferences()
        {
            if (motor == null)
            {
                motor = GetComponent<SideScrollerMotor>();
            }

            if (inputReader == null)
            {
                inputReader = GetComponent<PlayerInputReader>();
            }

            if (controlGate == null)
            {
                controlGate = GetComponent<PlayerControlGate>();
            }

            if (facingController == null)
            {
                facingController = GetComponent<PlayerFacingController>();
            }

            if (animatorBridge == null)
            {
                animatorBridge = GetComponent<PlayerAnimatorBridge>();
            }
        }

        private void CreateStates()
        {
            states[(int)PlayerStateId.Idle] = new PlayerIdleState(this);
            states[(int)PlayerStateId.Run] = new PlayerRunState(this);
            states[(int)PlayerStateId.Jump] = new PlayerJumpState(this);
            states[(int)PlayerStateId.Fall] = new PlayerFallState(this);
            states[(int)PlayerStateId.Land] = new PlayerLandState(this);
            states[(int)PlayerStateId.Disabled] = new PlayerDisabledState(this);
        }

        private void ReadJumpInput()
        {
            if (inputReader == null)
            {
                return;
            }

            if (inputReader.ConsumeJumpPressed())
            {
                motor.QueueJump(Time.time);
            }

            if (inputReader.ConsumeJumpReleased())
            {
                motor.RequestJumpCut();
            }
        }

        private void SetState(PlayerStateId targetState, bool force)
        {
            PlayerState nextState = states[(int)targetState];
            if (nextState == null)
            {
                return;
            }

            if (!force && currentState == nextState)
            {
                return;
            }

            currentState?.Exit();
            currentState = nextState;
            CurrentStateId = targetState;
            initialized = true;
            currentState.Enter();
        }
    }
}
