using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class LoopMenuController : MonoBehaviour, IRuntimeResettable
    {
        [SerializeField] private LoopStateService loopStateService;
        [SerializeField] private RuntimeResetCoordinator resetCoordinator;
        [SerializeField] private AdditiveLevelLoader levelLoader;
        [SerializeField] private LevelDefinition level01Definition;
        [SerializeField] private GameFlowController gameFlowController;
        [SerializeField] private PlayerControlGate playerControlGate;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI menuText;
        [SerializeField] private TextMeshProUGUI finalMessageText;
        [SerializeField, TextArea(1, 3)] private string staticEndingMessage = "\u9759\u6b62\u306f\u3001\u3042\u306a\u305f\u304c\u9078\u3093\u3060\u6700\u5f8c\u306e\u6249\u3002";
        [SerializeField] private int resetOrder = -30;

        private PlayerControlLockToken menuLock;
        private PlayerControlLockToken staticEndingLock;
        private Coroutine restartRoutine;

        public int ResetOrder => resetOrder;
        public bool IsVisible { get; private set; }
        public bool StaticEndingChosen { get; private set; }

        private void Awake()
        {
            ApplyVisible(false);
            if (menuText != null)
            {
                menuText.text = "[ENTER] \u518d\u6b21\u63a8\u5f00\u90a3\u6247\u95e8\n[ESC] \u4fdd\u6301\u6b64\u523b\u7684\u9759\u6b62";
            }
        }

        private void Update()
        {
            if (!IsVisible || StaticEndingChosen)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                ChooseRestart();
            }
            else if (keyboard.escapeKey.wasPressedThisFrame)
            {
                ChooseStaticEnding();
            }
        }

        public void Show()
        {
            StaticEndingChosen = false;
            ApplyVisible(true);
            AcquireMenuLock();
        }

        public void Hide()
        {
            ApplyVisible(false);
            ReleaseMenuLock();
        }

        public void ChooseRestart()
        {
            if (restartRoutine != null)
            {
                return;
            }

            restartRoutine = StartCoroutine(RestartLoopRoutine());
        }

        public void ChooseStaticEnding()
        {
            ReleaseMenuLock();
            StaticEndingChosen = true;
            gameFlowController?.EnterStaticEnding();
            AcquireStaticEndingLock();
            ApplyVisible(false);

            if (finalMessageText != null)
            {
                finalMessageText.text = staticEndingMessage;
                finalMessageText.gameObject.SetActive(true);
            }
        }

        public void RuntimeReset()
        {
            if (restartRoutine != null)
            {
                StopCoroutine(restartRoutine);
                restartRoutine = null;
            }

            ReleaseMenuLock();
            ReleaseStaticEndingLock();
            StaticEndingChosen = false;
            ApplyVisible(false);
            if (finalMessageText != null)
            {
                finalMessageText.gameObject.SetActive(false);
            }
        }

        private IEnumerator RestartLoopRoutine()
        {
            ReleaseMenuLock();
            ReleaseStaticEndingLock();
            loopStateService?.IncrementLoop();
            resetCoordinator?.ExecuteTransientReset();

            if (levelLoader != null)
            {
                while (levelLoader.IsBusy)
                {
                    yield return null;
                }
            }

            gameFlowController?.TryTransitionTo(GameFlowState.Level01);
            if (levelLoader != null && level01Definition != null)
            {
                yield return levelLoader.LoadLevel(level01Definition);
            }

            ApplyVisible(false);
            StaticEndingChosen = false;
            restartRoutine = null;
        }

        private void ApplyVisible(bool visible)
        {
            IsVisible = visible;
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        private void AcquireMenuLock()
        {
            if (playerControlGate == null || menuLock.IsValid)
            {
                return;
            }

            menuLock = playerControlGate.AcquireLock("LoopMenu");
        }

        private void ReleaseMenuLock()
        {
            if (playerControlGate != null && menuLock.IsValid)
            {
                playerControlGate.ReleaseLock(menuLock);
            }

            menuLock = default;
        }

        private void AcquireStaticEndingLock()
        {
            if (playerControlGate == null || staticEndingLock.IsValid)
            {
                return;
            }

            staticEndingLock = playerControlGate.AcquireLock("StaticEnding");
        }

        private void ReleaseStaticEndingLock()
        {
            if (playerControlGate != null && staticEndingLock.IsValid)
            {
                playerControlGate.ReleaseLock(staticEndingLock);
            }

            staticEndingLock = default;
        }
    }
}
