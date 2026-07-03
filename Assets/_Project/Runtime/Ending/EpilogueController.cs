using System.Collections;
using TMPro;
using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class EpilogueController : MonoBehaviour
    {
        [SerializeField] private ScreenFadeView fadeView;
        [SerializeField] private DOSBootView bootView;
        [SerializeField] private RuntimeResetCoordinator resetCoordinator;
        [SerializeField] private AdditiveLevelLoader levelLoader;
        [SerializeField] private LevelDefinition level01Definition;
        [SerializeField] private GameFlowController gameFlowController;
        [SerializeField] private StorySequenceRunner storySequenceRunner;
        [SerializeField] private StorySequence repeatedOpeningSequence;
        [SerializeField] private PlayerRepresentationTransfer playerTransfer;
        [SerializeField] private DesktopWindowController mainGameWindow;
        [SerializeField] private LoopMenuController loopMenu;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField, Min(0f)] private float blackDelay = 1f;
        [SerializeField] private string title = "\u7d42\u308f\u308a\u306a\u304d\u6249";

        private Coroutine activeRoutine;

        public bool IsRunning => activeRoutine != null;

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

            activeRoutine = StartCoroutine(RunEpilogue());
            return true;
        }

        public void Cancel()
        {
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }
        }

        private IEnumerator RunEpilogue()
        {
            fadeView?.SetAlpha(1f);
            gameFlowController?.TryTransitionTo(GameFlowState.Epilogue);
            yield return WaitUnscaled(blackDelay);

            if (bootView != null)
            {
                yield return bootView.Play();
            }

            resetCoordinator?.ExecuteTransientReset();
            playerTransfer?.ResetToWindowRepresentation();
            SetMainGameWindowVisible(true);

            if (titleText != null)
            {
                titleText.text = title;
                titleText.gameObject.SetActive(true);
            }

            if (levelLoader != null && level01Definition != null)
            {
                while (levelLoader.IsBusy)
                {
                    yield return null;
                }

                yield return levelLoader.LoadLevel(level01Definition);
            }

            if (fadeView != null)
            {
                yield return fadeView.FadeIn(0.75f);
            }

            StorySequence sequence = repeatedOpeningSequence != null ? repeatedOpeningSequence : level01Definition != null ? level01Definition.OpeningSequence : null;
            if (storySequenceRunner != null && sequence != null && storySequenceRunner.StartSequence(sequence))
            {
                while (storySequenceRunner.IsRunning)
                {
                    yield return null;
                }
            }

            gameFlowController?.TryTransitionTo(GameFlowState.LoopMenu);
            loopMenu?.Show();
            activeRoutine = null;
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

        private static IEnumerator WaitUnscaled(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }
}
