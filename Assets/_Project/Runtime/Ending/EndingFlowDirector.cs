using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class EndingFlowDirector : MonoBehaviour
    {
        [SerializeField] private GameFlowController gameFlowController;
        [SerializeField] private WindowBreakSequence windowBreakSequence;
        [SerializeField] private FormattingSequenceController formattingSequence;
        [SerializeField] private EpilogueController epilogueController;

        private bool finalSequenceStarted;
        private bool epilogueStarted;

        private void OnEnable()
        {
            if (gameFlowController != null)
            {
                gameFlowController.StateChanged += OnGameFlowStateChanged;
            }

            if (windowBreakSequence != null)
            {
                windowBreakSequence.SequenceCompleted.AddListener(OnWindowBreakCompleted);
            }
        }

        private void OnDisable()
        {
            if (gameFlowController != null)
            {
                gameFlowController.StateChanged -= OnGameFlowStateChanged;
            }

            if (windowBreakSequence != null)
            {
                windowBreakSequence.SequenceCompleted.RemoveListener(OnWindowBreakCompleted);
            }
        }

        public void ResetDirectorState()
        {
            finalSequenceStarted = false;
            epilogueStarted = false;
        }

        private void OnGameFlowStateChanged(GameFlowState previous, GameFlowState next)
        {
            if (next == GameFlowState.FinalSequence)
            {
                StartFinalSequence();
            }
            else if (next == GameFlowState.Blackout)
            {
                StartEpilogue();
            }
            else if (next == GameFlowState.Level01)
            {
                ResetDirectorState();
            }
        }

        private void StartFinalSequence()
        {
            if (finalSequenceStarted)
            {
                return;
            }

            finalSequenceStarted = true;
            if (windowBreakSequence != null)
            {
                windowBreakSequence.Play();
            }
            else
            {
                formattingSequence?.StartSequence();
            }
        }

        private void OnWindowBreakCompleted()
        {
            formattingSequence?.StartSequence();
        }

        private void StartEpilogue()
        {
            if (epilogueStarted)
            {
                return;
            }

            epilogueStarted = true;
            epilogueController?.Play();
        }
    }
}
