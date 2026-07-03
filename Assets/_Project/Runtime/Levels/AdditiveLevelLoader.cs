using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class AdditiveLevelLoader : MonoBehaviour, IRuntimeResettable
    {
        [SerializeField] private RuntimeResetService runtimeResetService;
        [SerializeField] private StorySequenceRunner storySequenceRunner;
        [SerializeField] private StoryFlagService storyFlagService;
        [SerializeField] private GameFlowController gameFlowController;
        [SerializeField] private Transform playerRoot;
        [SerializeField] private bool unloadCurrentBeforeLoad = true;
        [SerializeField] private int resetOrder = -80;

        private Coroutine activeRoutine;
        private bool cancelRequested;
        private Scene currentLevelScene;

        public event Action<LevelDefinition, LevelEntryPoint> LevelLoaded;
        public event Action<LevelDefinition> LevelLoadFailed;
        public event Action<Scene> LevelUnloaded;

        public int ResetOrder => resetOrder;
        public bool IsBusy => activeRoutine != null;
        public LevelDefinition CurrentLevel { get; private set; }
        public LevelEntryPoint CurrentEntryPoint { get; private set; }

        private void OnEnable()
        {
            runtimeResetService?.Register(this);
        }

        private void OnDisable()
        {
            runtimeResetService?.Unregister(this);
            CancelActiveRequest();
        }

        public Coroutine LoadLevel(LevelDefinition level)
        {
            if (level == null || string.IsNullOrWhiteSpace(level.SceneName))
            {
                LevelLoadFailed?.Invoke(level);
                return null;
            }

            if (IsBusy)
            {
                return activeRoutine;
            }

            if (CurrentLevel == level && currentLevelScene.IsValid() && currentLevelScene.isLoaded)
            {
                LevelLoaded?.Invoke(level, CurrentEntryPoint);
                return null;
            }

            cancelRequested = false;
            activeRoutine = StartCoroutine(LoadLevelRoutine(level));
            return activeRoutine;
        }

        public Coroutine UnloadCurrentLevel()
        {
            if (IsBusy)
            {
                cancelRequested = true;
                return activeRoutine;
            }

            if (!currentLevelScene.IsValid() || !currentLevelScene.isLoaded)
            {
                CurrentLevel = null;
                CurrentEntryPoint = null;
                return null;
            }

            activeRoutine = StartCoroutine(UnloadCurrentLevelRoutine());
            return activeRoutine;
        }

        public void CancelActiveRequest()
        {
            cancelRequested = true;
        }

        public void RuntimeReset()
        {
            CancelActiveRequest();
            CurrentEntryPoint?.RuntimeReset();
        }

        private IEnumerator LoadLevelRoutine(LevelDefinition level)
        {
            if (unloadCurrentBeforeLoad && currentLevelScene.IsValid() && currentLevelScene.isLoaded)
            {
                yield return UnloadScene(currentLevelScene);
            }

            if (cancelRequested)
            {
                CompleteFailedLoad(level);
                yield break;
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(level.SceneName, LoadSceneMode.Additive);
            if (operation == null)
            {
                CompleteFailedLoad(level);
                yield break;
            }

            while (!operation.isDone)
            {
                if (cancelRequested)
                {
                    operation.allowSceneActivation = true;
                }

                yield return null;
            }

            Scene loadedScene = SceneManager.GetSceneByName(level.SceneName);
            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
            {
                CompleteFailedLoad(level);
                yield break;
            }

            if (cancelRequested)
            {
                yield return UnloadScene(loadedScene);
                CompleteFailedLoad(level);
                yield break;
            }

            currentLevelScene = loadedScene;
            CurrentLevel = level;
            CurrentEntryPoint = FindEntryPointInScene(loadedScene);
            CurrentEntryPoint?.BindRuntime(storySequenceRunner, storyFlagService, this, runtimeResetService, gameFlowController);
            MovePlayerToSpawn(CurrentEntryPoint);
            gameFlowController?.TryTransitionTo(level.FlowState);
            LevelLoaded?.Invoke(level, CurrentEntryPoint);
            activeRoutine = null;
        }

        private IEnumerator UnloadCurrentLevelRoutine()
        {
            Scene sceneToUnload = currentLevelScene;
            yield return UnloadScene(sceneToUnload);
            CurrentLevel = null;
            CurrentEntryPoint = null;
            activeRoutine = null;
        }

        private IEnumerator UnloadScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || scene == gameObject.scene)
            {
                yield break;
            }

            AsyncOperation operation = SceneManager.UnloadSceneAsync(scene);
            while (operation != null && !operation.isDone)
            {
                yield return null;
            }

            LevelUnloaded?.Invoke(scene);
        }

        private void CompleteFailedLoad(LevelDefinition level)
        {
            activeRoutine = null;
            LevelLoadFailed?.Invoke(level);
        }

        private void MovePlayerToSpawn(LevelEntryPoint entryPoint)
        {
            if (playerRoot == null || entryPoint == null || entryPoint.PlayerSpawn == null)
            {
                return;
            }

            playerRoot.SetPositionAndRotation(entryPoint.PlayerSpawn.Position, entryPoint.PlayerSpawn.Rotation);
            if (playerRoot.TryGetComponent(out Rigidbody body))
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        private static LevelEntryPoint FindEntryPointInScene(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                LevelEntryPoint entryPoint = roots[i].GetComponentInChildren<LevelEntryPoint>(true);
                if (entryPoint != null)
                {
                    return entryPoint;
                }
            }

            return null;
        }
    }
}
