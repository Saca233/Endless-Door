using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class StorySequenceRunner : MonoBehaviour, IRuntimeResettable
    {
        [SerializeField] private DialogueView dialogueView;
        [SerializeField] private StoryFlagService storyFlagService;
        [SerializeField] private PlayerControlGate playerControlGate;
        [SerializeField] private ScreenFadeView screenFadeView;
        [SerializeField] private RuntimeResetService runtimeResetService;
        [SerializeField] private int resetOrder = -90;

        private readonly List<PlayerControlLockToken> ownedPlayerLocks = new List<PlayerControlLockToken>();
        private Coroutine activeCoroutine;

        public int ResetOrder => resetOrder;
        public StorySequence ActiveSequence { get; private set; }
        public bool IsRunning => activeCoroutine != null;
        public int OwnedPlayerLockCount => ownedPlayerLocks.Count;

        private void OnEnable()
        {
            runtimeResetService?.Register(this);
        }

        private void OnDisable()
        {
            runtimeResetService?.Unregister(this);
            CancelCurrentSequence();
        }

        public void SetServices(DialogueView dialogue, StoryFlagService flags, PlayerControlGate controlGate, ScreenFadeView fadeView, RuntimeResetService resetService)
        {
            runtimeResetService?.Unregister(this);
            dialogueView = dialogue;
            storyFlagService = flags;
            playerControlGate = controlGate;
            screenFadeView = fadeView;
            runtimeResetService = resetService;
            if (isActiveAndEnabled)
            {
                runtimeResetService?.Register(this);
            }
        }

        public void SetPlayerControlGate(PlayerControlGate controlGate)
        {
            playerControlGate = controlGate;
        }

        public bool StartSequence(StorySequence sequence)
        {
            if (sequence == null || IsRunning)
            {
                return false;
            }

            ActiveSequence = sequence;
            activeCoroutine = StartCoroutine(RunSequence(sequence));
            return true;
        }

        public void CancelCurrentSequence()
        {
            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
                activeCoroutine = null;
            }

            ActiveSequence = null;
            ReleaseAllOwnedPlayerLocks();
            dialogueView?.Hide();
        }

        public void LockPlayerForSequence(string ownerName)
        {
            if (playerControlGate == null)
            {
                return;
            }

            ownedPlayerLocks.Add(playerControlGate.AcquireLock(string.IsNullOrWhiteSpace(ownerName) ? "StorySequence" : ownerName));
        }

        public void UnlockPlayerForSequence()
        {
            if (playerControlGate == null || ownedPlayerLocks.Count == 0)
            {
                return;
            }

            int lastIndex = ownedPlayerLocks.Count - 1;
            playerControlGate.ReleaseLock(ownedPlayerLocks[lastIndex]);
            ownedPlayerLocks.RemoveAt(lastIndex);
        }

        public void RuntimeReset()
        {
            CancelCurrentSequence();
        }

        private IEnumerator RunSequence(StorySequence sequence)
        {
            for (int i = 0; i < sequence.CommandCount; i++)
            {
                yield return ExecuteCommand(sequence.GetCommand(i));
            }

            activeCoroutine = null;
            ActiveSequence = null;
            ReleaseAllOwnedPlayerLocks();
        }

        private IEnumerator ExecuteCommand(StorySequenceCommandData command)
        {
            if (command == null)
            {
                yield break;
            }

            switch (command.CommandType)
            {
                case StoryCommandType.ShowDialogue:
                    yield return RunDialogue(command.DialogueSequence);
                    break;
                case StoryCommandType.Wait:
                    yield return WaitSeconds(command.Duration);
                    break;
                case StoryCommandType.SetFlag:
                    ApplySetFlag(command);
                    break;
                case StoryCommandType.WaitForFlag:
                    yield return WaitForFlag(command);
                    break;
                case StoryCommandType.LockPlayer:
                    LockPlayerForSequence("StorySequence");
                    break;
                case StoryCommandType.UnlockPlayer:
                    UnlockPlayerForSequence();
                    break;
                case StoryCommandType.EnablePuzzleRule:
                    command.PuzzleRule?.SetRuleEnabled(true);
                    break;
                case StoryCommandType.DisablePuzzleRule:
                    command.PuzzleRule?.SetRuleEnabled(false);
                    break;
                case StoryCommandType.OpenDesktopWindow:
                    SetWindowVisible(command.DesktopWindow, true);
                    break;
                case StoryCommandType.CloseDesktopWindow:
                    SetWindowVisible(command.DesktopWindow, false);
                    break;
                case StoryCommandType.MoveDesktopWindow:
                    yield return MoveWindow(command.DesktopWindow, command.WindowPosition, command.WindowMoveDuration);
                    break;
                case StoryCommandType.FadeIn:
                    yield return Fade(command.FadeView, 0f, command.Duration);
                    break;
                case StoryCommandType.FadeOut:
                    yield return Fade(command.FadeView, 1f, command.Duration);
                    break;
                case StoryCommandType.LoadAdditiveLevel:
                    yield return LoadAdditiveLevel(command.SceneName);
                    break;
                case StoryCommandType.UnloadLevel:
                    yield return UnloadLevel(command.SceneName);
                    break;
                case StoryCommandType.TriggerDoor:
                    command.Door?.TriggerDoor();
                    break;
                case StoryCommandType.InvokeConfiguredEvent:
                    command.ConfiguredEvent?.Invoke();
                    break;
            }
        }

        private IEnumerator RunDialogue(DialogueSequence sequence)
        {
            if (dialogueView == null || sequence == null)
            {
                yield break;
            }

            dialogueView.Show();
            for (int i = 0; i < sequence.LineCount; i++)
            {
                DialogueLineData line = sequence.GetLine(i);
                dialogueView.DisplayLine(line);
                bool skippable = line == null || line.Skippable;
                float autoAdvanceTime = line != null ? line.AutoAdvanceTime : 0f;
                float pauseAfterLine = line != null ? line.PauseAfterLine : 0f;
                float autoAdvanceTimer = 0f;

                while (true)
                {
                    bool advance = dialogueView.ConsumeAdvanceRequest();
                    if (advance && !dialogueView.LineFullyRevealed && skippable)
                    {
                        dialogueView.CompleteLine();
                    }
                    else if (advance && dialogueView.LineFullyRevealed && skippable)
                    {
                        break;
                    }

                    if (dialogueView.LineFullyRevealed && autoAdvanceTime > 0f)
                    {
                        autoAdvanceTimer += Time.unscaledDeltaTime;
                        if (autoAdvanceTimer >= autoAdvanceTime)
                        {
                            break;
                        }
                    }

                    yield return null;
                }

                if (pauseAfterLine > 0f)
                {
                    yield return WaitSeconds(pauseAfterLine);
                }
            }

            dialogueView.Hide();
        }

        private IEnumerator WaitSeconds(float seconds)
        {
            if (seconds <= 0f)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void ApplySetFlag(StorySequenceCommandData command)
        {
            if (storyFlagService == null)
            {
                return;
            }

            if (command.FlagValueKind == StoryFlagValueKind.Integer)
            {
                storyFlagService.SetInt(command.FlagId, command.IntValue, command.TemporaryFlag);
            }
            else
            {
                storyFlagService.SetBool(command.FlagId, command.BoolValue, command.TemporaryFlag);
            }
        }

        private IEnumerator WaitForFlag(StorySequenceCommandData command)
        {
            if (storyFlagService == null)
            {
                yield break;
            }

            while (!FlagMatches(command))
            {
                yield return null;
            }
        }

        private bool FlagMatches(StorySequenceCommandData command)
        {
            if (command.FlagValueKind == StoryFlagValueKind.Integer)
            {
                return storyFlagService.CheckInt(command.FlagId, command.IntValue);
            }

            return storyFlagService.CheckBool(command.FlagId, command.BoolValue);
        }

        private static void SetWindowVisible(DesktopWindowController window, bool visible)
        {
            if (window == null || window.Model == null)
            {
                return;
            }

            window.Model.SetVisible(visible);
            window.ApplyModelToView();
        }

        private IEnumerator MoveWindow(DesktopWindowController window, Vector2 targetPosition, float duration)
        {
            if (window == null || window.View == null || window.View.WindowRoot == null || window.Model == null)
            {
                yield break;
            }

            RectTransform windowRoot = window.View.WindowRoot;
            Vector2 start = windowRoot.anchoredPosition;
            if (duration <= 0f)
            {
                windowRoot.anchoredPosition = targetPosition;
                window.Model.SetPosition(targetPosition);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                Vector2 nextPosition = Vector2.Lerp(start, targetPosition, elapsed / duration);
                windowRoot.anchoredPosition = nextPosition;
                window.Model.SetPosition(nextPosition);
                yield return null;
            }

            windowRoot.anchoredPosition = targetPosition;
            window.Model.SetPosition(targetPosition);
        }

        private IEnumerator Fade(ScreenFadeView commandFadeView, float targetAlpha, float duration)
        {
            ScreenFadeView fadeView = commandFadeView != null ? commandFadeView : screenFadeView;
            if (fadeView == null)
            {
                yield return WaitSeconds(duration);
                yield break;
            }

            yield return fadeView.FadeTo(targetAlpha, duration);
        }

        private static IEnumerator LoadAdditiveLevel(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                yield break;
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (operation != null && !operation.isDone)
            {
                yield return null;
            }
        }

        private static IEnumerator UnloadLevel(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                yield break;
            }

            AsyncOperation operation = SceneManager.UnloadSceneAsync(sceneName);
            while (operation != null && !operation.isDone)
            {
                yield return null;
            }
        }

        private void ReleaseAllOwnedPlayerLocks()
        {
            if (playerControlGate != null)
            {
                for (int i = 0; i < ownedPlayerLocks.Count; i++)
                {
                    playerControlGate.ReleaseLock(ownedPlayerLocks[i]);
                }
            }

            ownedPlayerLocks.Clear();
        }
    }
}
