using UnityEngine;
using UnityEngine.InputSystem;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string gameplayActionMapName = "Gameplay";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string jumpActionName = "Jump";
        [SerializeField] private string interactActionName = "Interact";
        [SerializeField] private string pauseActionName = "Pause";

        private PlayerInput playerInput;
        private InputActionMap gameplayMap;
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction interactAction;
        private InputAction pauseAction;
        private bool actionsCached;
        private bool subscribed;
        private bool jumpPressed;
        private bool jumpReleased;
        private bool interactPressed;
        private bool pausePressed;

        public Vector2 Movement { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool GameplayEnabled => gameplayMap != null && gameplayMap.enabled;

        private void Awake()
        {
            CacheActions();
        }

        private void OnEnable()
        {
            CacheActions();
            SubscribeActions();
            EnableGameplay();
        }

        private void OnDisable()
        {
            UnsubscribeActions();
            DisableGameplay();
        }

        public void EnableGameplay()
        {
            CacheActions();
            gameplayMap?.Enable();
        }

        public void DisableGameplay()
        {
            gameplayMap?.Disable();
            ClearInput();
        }

        public bool ConsumeJumpPressed()
        {
            if (!jumpPressed)
            {
                return false;
            }

            jumpPressed = false;
            return true;
        }

        public bool ConsumeJumpReleased()
        {
            if (!jumpReleased)
            {
                return false;
            }

            jumpReleased = false;
            return true;
        }

        public bool ConsumeInteractPressed()
        {
            if (!interactPressed)
            {
                return false;
            }

            interactPressed = false;
            return true;
        }

        public bool ConsumePausePressed()
        {
            if (!pausePressed)
            {
                return false;
            }

            pausePressed = false;
            return true;
        }

        public void ClearInput()
        {
            Movement = Vector2.zero;
            JumpHeld = false;
            jumpPressed = false;
            jumpReleased = false;
            interactPressed = false;
            pausePressed = false;
        }

        private void CacheActions()
        {
            if (actionsCached)
            {
                return;
            }

            playerInput = GetComponent<PlayerInput>();
            if (inputActions == null && playerInput != null)
            {
                inputActions = playerInput.actions;
            }
            else if (inputActions != null && playerInput != null && playerInput.actions == null)
            {
                playerInput.actions = inputActions;
            }

            if (inputActions == null)
            {
                return;
            }

            gameplayMap = inputActions.FindActionMap(gameplayActionMapName, false);
            if (gameplayMap == null)
            {
                return;
            }

            moveAction = gameplayMap.FindAction(moveActionName, false);
            jumpAction = gameplayMap.FindAction(jumpActionName, false);
            interactAction = gameplayMap.FindAction(interactActionName, false);
            pauseAction = gameplayMap.FindAction(pauseActionName, false);
            actionsCached = moveAction != null && jumpAction != null;
        }

        private void SubscribeActions()
        {
            if (subscribed || !actionsCached)
            {
                return;
            }

            moveAction.performed += OnMoveChanged;
            moveAction.canceled += OnMoveChanged;
            jumpAction.performed += OnJumpPerformed;
            jumpAction.canceled += OnJumpCanceled;

            if (interactAction != null)
            {
                interactAction.performed += OnInteractPerformed;
            }

            if (pauseAction != null)
            {
                pauseAction.performed += OnPausePerformed;
            }

            subscribed = true;
        }

        private void UnsubscribeActions()
        {
            if (!subscribed)
            {
                return;
            }

            moveAction.performed -= OnMoveChanged;
            moveAction.canceled -= OnMoveChanged;
            jumpAction.performed -= OnJumpPerformed;
            jumpAction.canceled -= OnJumpCanceled;

            if (interactAction != null)
            {
                interactAction.performed -= OnInteractPerformed;
            }

            if (pauseAction != null)
            {
                pauseAction.performed -= OnPausePerformed;
            }

            subscribed = false;
        }

        private void OnMoveChanged(InputAction.CallbackContext context)
        {
            Movement = context.ReadValue<Vector2>();
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            JumpHeld = true;
            jumpPressed = true;
        }

        private void OnJumpCanceled(InputAction.CallbackContext context)
        {
            JumpHeld = false;
            jumpReleased = true;
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            interactPressed = true;
        }

        private void OnPausePerformed(InputAction.CallbackContext context)
        {
            pausePressed = true;
        }
    }
}
