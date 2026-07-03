using System.Collections;
using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class BootSequenceController : MonoBehaviour
    {
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private DOSBootView bootView;
        [SerializeField] private ScreenFadeView desktopFadeView;
        [SerializeField] private AdditiveLevelLoader levelLoader;
        [SerializeField] private LevelDefinition firstLevel;
        [SerializeField] private StorySequenceRunner storySequenceRunner;
        [SerializeField] private PlayerControlGate playerControlGate;
        [SerializeField] private GameFlowController gameFlowController;
        [SerializeField] private DesktopWindowController mainGameWindow;
        [SerializeField] private CoverToEraseRule firstPuzzleRule;
        [SerializeField] private Transform playerRoot;
        [SerializeField, Min(0f)] private float desktopFadeDuration = 1f;
        [SerializeField, Min(0f)] private float windowOpenDelay = 0.25f;

        private Coroutine bootRoutine;
        private PlayerControlLockToken bootLock;

        public bool IsRunning => bootRoutine != null;

        private void Start()
        {
            if (playOnStart)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            Cancel();
        }

        public void Play()
        {
            if (bootRoutine != null)
            {
                return;
            }

            bootRoutine = StartCoroutine(RunBootSequence());
        }

        public void Cancel()
        {
            if (bootRoutine != null)
            {
                StopCoroutine(bootRoutine);
                bootRoutine = null;
            }

            ReleaseBootLock();
        }

        private IEnumerator RunBootSequence()
        {
            SetMainWindowVisible(false);
            desktopFadeView?.SetAlpha(1f);
            AcquireBootLock();

            if (bootView != null)
            {
                yield return bootView.Play();
            }

            if (gameFlowController != null)
            {
                gameFlowController.TryTransitionTo(GameFlowState.DesktopIntro);
            }

            if (desktopFadeView != null)
            {
                yield return desktopFadeView.FadeIn(desktopFadeDuration);
            }

            LevelEntryPoint entryPoint = null;
            if (levelLoader != null && firstLevel != null)
            {
                yield return levelLoader.LoadLevel(firstLevel);
                entryPoint = levelLoader.CurrentEntryPoint;
            }

            BindLoadedLevel(entryPoint);

            if (windowOpenDelay > 0f)
            {
                yield return WaitUnscaled(windowOpenDelay);
            }

            SetMainWindowVisible(true);

            StorySequence openingSequence = firstLevel != null ? firstLevel.OpeningSequence : null;
            if (storySequenceRunner != null && openingSequence != null && storySequenceRunner.StartSequence(openingSequence))
            {
                while (storySequenceRunner.IsRunning)
                {
                    yield return null;
                }
            }

            ReleaseBootLock();
            bootRoutine = null;
        }

        private void BindLoadedLevel(LevelEntryPoint entryPoint)
        {
            if (entryPoint == null)
            {
                return;
            }

            if (firstPuzzleRule != null)
            {
                firstPuzzleRule.SetTargets(entryPoint.PuzzleTargets);
            }

            if (playerRoot != null && entryPoint.PlayerSpawn != null)
            {
                playerRoot.SetPositionAndRotation(entryPoint.PlayerSpawn.Position, entryPoint.PlayerSpawn.Rotation);
            }
        }

        private void SetMainWindowVisible(bool visible)
        {
            if (mainGameWindow == null || mainGameWindow.Model == null)
            {
                return;
            }

            mainGameWindow.Model.SetVisible(visible);
            mainGameWindow.ApplyModelToView();
        }

        private void AcquireBootLock()
        {
            if (playerControlGate == null || bootLock.IsValid)
            {
                return;
            }

            bootLock = playerControlGate.AcquireLock("BootSequence");
        }

        private void ReleaseBootLock()
        {
            if (playerControlGate == null || !bootLock.IsValid)
            {
                bootLock = default;
                return;
            }

            playerControlGate.ReleaseLock(bootLock);
            bootLock = default;
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
