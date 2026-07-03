using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class WindowBreakSequence : MonoBehaviour, IRuntimeResettable
    {
        [SerializeField] private PlayerControlGate playerControlGate;
        [SerializeField] private DesktopWindowController mainGameWindow;
        [SerializeField] private PlayerRepresentationTransfer playerTransfer;
        [SerializeField] private StorySequenceRunner storySequenceRunner;
        [SerializeField] private StorySequence breakoutDialogue;
        [SerializeField] private ScreenCorruptionView corruptionView;
        [SerializeField] private RectTransform[] crackingStages;
        [SerializeField] private Transform cameraShakeRoot;
        [SerializeField, Min(0f)] private float crackingStageDuration = 0.22f;
        [SerializeField, Min(0f)] private float cameraShakeAmplitude = 0.04f;
        [SerializeField] private bool hideMainGameWindow = true;
        [SerializeField] private int resetOrder = -50;
        [SerializeField] private UnityEvent sequenceStarted = new UnityEvent();
        [SerializeField] private UnityEvent playerTransferred = new UnityEvent();
        [SerializeField] private UnityEvent sequenceCompleted = new UnityEvent();

        private Coroutine activeRoutine;
        private PlayerControlLockToken inputLock;
        private Vector3 cameraShakeStartPosition;
        private bool hasCameraShakeStartPosition;

        public int ResetOrder => resetOrder;
        public bool IsRunning => activeRoutine != null;
        public UnityEvent SequenceStarted => sequenceStarted;
        public UnityEvent PlayerTransferred => playerTransferred;
        public UnityEvent SequenceCompleted => sequenceCompleted;

        private void OnDisable()
        {
            Cancel();
        }

        public bool Play()
        {
            if (activeRoutine != null)
            {
                return false;
            }

            activeRoutine = StartCoroutine(RunSequence());
            return true;
        }

        public void Cancel()
        {
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }

            ReleaseInputLock();
            RestoreCameraShakeRoot();
        }

        public void RuntimeReset()
        {
            Cancel();
            SetCrackingStagesVisible(false);
            corruptionView?.Clear();
            playerTransfer?.ResetToWindowRepresentation();
        }

        private IEnumerator RunSequence()
        {
            AcquireInputLock();
            sequenceStarted.Invoke();
            SetCrackingStagesVisible(false);

            if (corruptionView != null)
            {
                corruptionView.SetIntensity(0.2f);
            }

            int stageCount = crackingStages != null ? crackingStages.Length : 0;
            for (int i = 0; i < stageCount; i++)
            {
                if (crackingStages[i] != null)
                {
                    crackingStages[i].gameObject.SetActive(true);
                }

                float normalized = stageCount <= 1 ? 1f : (i + 1f) / stageCount;
                corruptionView?.SetIntensity(normalized);
                yield return ShakeAndWait(crackingStageDuration);
            }

            if (hideMainGameWindow)
            {
                SetMainGameWindowVisible(false);
            }

            playerTransfer?.TransferToDesktop();
            playerTransferred.Invoke();
            ReleaseInputLock();

            if (storySequenceRunner != null && breakoutDialogue != null && storySequenceRunner.StartSequence(breakoutDialogue))
            {
                while (storySequenceRunner.IsRunning)
                {
                    yield return null;
                }
            }

            sequenceCompleted.Invoke();
            activeRoutine = null;
        }

        private IEnumerator ShakeAndWait(float duration)
        {
            if (duration <= 0f)
            {
                yield break;
            }

            CacheCameraShakeStart();
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (cameraShakeRoot != null && cameraShakeAmplitude > 0f)
                {
                    float x = Mathf.Sin(elapsed * 82.1f) * cameraShakeAmplitude;
                    float y = Mathf.Cos(elapsed * 63.7f) * cameraShakeAmplitude;
                    cameraShakeRoot.localPosition = cameraShakeStartPosition + new Vector3(x, y, 0f);
                }

                yield return null;
            }

            RestoreCameraShakeRoot();
        }

        private void SetMainGameWindowVisible(bool visible)
        {
            if (mainGameWindow == null || mainGameWindow.Model == null)
            {
                return;
            }

            mainGameWindow.Model.SetVisible(visible);
            mainGameWindow.ApplyModelToView();
        }

        private void SetCrackingStagesVisible(bool visible)
        {
            if (crackingStages == null)
            {
                return;
            }

            int stageCount = crackingStages != null ? crackingStages.Length : 0;
            for (int i = 0; i < stageCount; i++)
            {
                if (crackingStages[i] != null)
                {
                    crackingStages[i].gameObject.SetActive(visible);
                }
            }
        }

        private void AcquireInputLock()
        {
            if (playerControlGate == null || inputLock.IsValid)
            {
                return;
            }

            inputLock = playerControlGate.AcquireLock("WindowBreakSequence");
        }

        private void ReleaseInputLock()
        {
            if (playerControlGate != null && inputLock.IsValid)
            {
                playerControlGate.ReleaseLock(inputLock);
            }

            inputLock = default;
        }

        private void CacheCameraShakeStart()
        {
            if (cameraShakeRoot == null || hasCameraShakeStartPosition)
            {
                return;
            }

            cameraShakeStartPosition = cameraShakeRoot.localPosition;
            hasCameraShakeStartPosition = true;
        }

        private void RestoreCameraShakeRoot()
        {
            if (cameraShakeRoot != null && hasCameraShakeStartPosition)
            {
                cameraShakeRoot.localPosition = cameraShakeStartPosition;
            }

            hasCameraShakeStartPosition = false;
        }
    }
}
