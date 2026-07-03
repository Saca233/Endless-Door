using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class FormattingSequenceController : MonoBehaviour, IRuntimeResettable
    {
        [SerializeField] private GameFlowController gameFlowController;
        [SerializeField] private PlayerControlGate playerControlGate;
        [SerializeField] private FinalDoorController finalDoor;
        [SerializeField] private FormattingAttractor formattingAttractor;
        [SerializeField] private PlayerSacrificeSequence sacrificeSequence;
        [SerializeField] private StorySequenceRunner storySequenceRunner;
        [SerializeField] private StorySequence warningDialogue;
        [SerializeField] private StorySequence revelationDialogue;
        [SerializeField] private ScreenFadeView fadeView;
        [SerializeField] private ScreenCorruptionView corruptionView;
        [SerializeField, Min(0f)] private float doorAppearingDelay = 0.7f;
        [SerializeField, Min(0f)] private float pullingDuration = 2.5f;
        [SerializeField, Min(0f)] private float blackoutFadeDuration = 1f;
        [SerializeField] private int resetOrder = -40;
        [SerializeField] private UnityEvent sequenceCompleted = new UnityEvent();

        private Coroutine activeRoutine;
        private PlayerControlLockToken sequenceLock;

        public event Action<FormattingSequenceState, FormattingSequenceState> StateChanged;

        public int ResetOrder => resetOrder;
        public FormattingSequenceState CurrentState { get; private set; } = FormattingSequenceState.Idle;
        public bool IsRunning => activeRoutine != null;
        public UnityEvent SequenceCompleted => sequenceCompleted;

        private void OnDisable()
        {
            Cancel();
        }

        public bool StartSequence()
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

            ReleaseSequenceLock();
            formattingAttractor?.SetPaused(false);
        }

        public bool TrySetState(FormattingSequenceState nextState)
        {
            if (!CanTransition(CurrentState, nextState))
            {
                return false;
            }

            SetState(nextState);
            return true;
        }

        public void RuntimeReset()
        {
            Cancel();
            SetState(FormattingSequenceState.Idle);
            finalDoor?.RuntimeReset();
            formattingAttractor?.RuntimeReset();
            sacrificeSequence?.RuntimeReset();
            corruptionView?.Clear();
        }

        public static bool CanTransition(FormattingSequenceState from, FormattingSequenceState to)
        {
            if (from == to)
            {
                return true;
            }

            switch (from)
            {
                case FormattingSequenceState.Idle:
                    return to == FormattingSequenceState.Warning;
                case FormattingSequenceState.Warning:
                    return to == FormattingSequenceState.DoorAppearing;
                case FormattingSequenceState.DoorAppearing:
                    return to == FormattingSequenceState.PullingData;
                case FormattingSequenceState.PullingData:
                    return to == FormattingSequenceState.RevelationDialogue;
                case FormattingSequenceState.RevelationDialogue:
                    return to == FormattingSequenceState.Sacrifice;
                case FormattingSequenceState.Sacrifice:
                    return to == FormattingSequenceState.DoorClosing;
                case FormattingSequenceState.DoorClosing:
                    return to == FormattingSequenceState.Blackout;
                case FormattingSequenceState.Blackout:
                    return to == FormattingSequenceState.Complete;
                case FormattingSequenceState.Complete:
                    return to == FormattingSequenceState.Idle;
                default:
                    return false;
            }
        }

        private IEnumerator RunSequence()
        {
            AcquireSequenceLock();
            SetState(FormattingSequenceState.Warning);
            corruptionView?.SetIntensity(0.35f);
            yield return RunStorySequence(warningDialogue);

            SetState(FormattingSequenceState.DoorAppearing);
            finalDoor?.Open();
            yield return WaitUnscaled(doorAppearingDelay);

            SetState(FormattingSequenceState.PullingData);
            formattingAttractor?.BeginPull();
            corruptionView?.SetIntensity(0.65f);
            yield return WaitUnscaled(pullingDuration);

            SetState(FormattingSequenceState.RevelationDialogue);
            formattingAttractor?.SetPaused(true);
            yield return RunStorySequence(revelationDialogue);
            formattingAttractor?.SetPaused(false);

            SetState(FormattingSequenceState.Sacrifice);
            ReleaseSequenceLock();
            if (sacrificeSequence != null)
            {
                sacrificeSequence.Play();
                while (sacrificeSequence.IsRunning)
                {
                    yield return null;
                }
            }

            SetState(FormattingSequenceState.DoorClosing);
            finalDoor?.Close();

            SetState(FormattingSequenceState.Blackout);
            if (fadeView != null)
            {
                yield return fadeView.FadeOut(blackoutFadeDuration);
            }

            gameFlowController?.TryTransitionTo(GameFlowState.Blackout);
            SetState(FormattingSequenceState.Complete);
            sequenceCompleted.Invoke();
            activeRoutine = null;
        }

        private IEnumerator RunStorySequence(StorySequence sequence)
        {
            if (storySequenceRunner == null || sequence == null)
            {
                yield break;
            }

            if (!storySequenceRunner.StartSequence(sequence))
            {
                yield break;
            }

            while (storySequenceRunner.IsRunning)
            {
                yield return null;
            }
        }

        private IEnumerator WaitUnscaled(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void SetState(FormattingSequenceState nextState)
        {
            FormattingSequenceState previous = CurrentState;
            CurrentState = nextState;
            if (previous != nextState)
            {
                StateChanged?.Invoke(previous, nextState);
            }
        }

        private void AcquireSequenceLock()
        {
            if (playerControlGate == null || sequenceLock.IsValid)
            {
                return;
            }

            sequenceLock = playerControlGate.AcquireLock("FormattingSequence");
        }

        private void ReleaseSequenceLock()
        {
            if (playerControlGate != null && sequenceLock.IsValid)
            {
                playerControlGate.ReleaseLock(sequenceLock);
            }

            sequenceLock = default;
        }
    }
}
