using System;
using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class LevelEntryPoint : MonoBehaviour, IRuntimeResettable
    {
        [SerializeField] private LevelDefinition levelDefinition;
        [SerializeField] private PlayerSpawnPoint playerSpawn;
        [SerializeField] private WindowPuzzleTarget[] puzzleTargets = Array.Empty<WindowPuzzleTarget>();
        [SerializeField] private StoryTriggerVolume[] storyTriggers = Array.Empty<StoryTriggerVolume>();
        [SerializeField] private LevelCompletionTrigger[] completionTriggers = Array.Empty<LevelCompletionTrigger>();
        [SerializeField] private DoorController[] doors = Array.Empty<DoorController>();
        [SerializeField] private int resetOrder = 0;

        private RuntimeResetService runtimeResetService;

        public int ResetOrder => resetOrder;
        public LevelDefinition LevelDefinition => levelDefinition;
        public PlayerSpawnPoint PlayerSpawn => playerSpawn;
        public WindowPuzzleTarget[] PuzzleTargets => puzzleTargets;

        private void OnDisable()
        {
            runtimeResetService?.Unregister(this);
        }

        public void BindRuntime(
            StorySequenceRunner sequenceRunner,
            StoryFlagService storyFlagService,
            AdditiveLevelLoader levelLoader,
            RuntimeResetService resetService,
            GameFlowController flowController)
        {
            runtimeResetService?.Unregister(this);
            runtimeResetService = resetService;
            runtimeResetService?.Register(this);

            for (int i = 0; i < storyTriggers.Length; i++)
            {
                if (storyTriggers[i] != null)
                {
                    storyTriggers[i].SetRuntimeServices(sequenceRunner, storyFlagService, resetService);
                }
            }

            for (int i = 0; i < completionTriggers.Length; i++)
            {
                if (completionTriggers[i] != null)
                {
                    completionTriggers[i].SetRuntimeServices(levelLoader, flowController, resetService);
                }
            }

            for (int i = 0; i < doors.Length; i++)
            {
                if (doors[i] != null)
                {
                    doors[i].SetRuntimeResetService(resetService);
                }
            }
        }

        public void RuntimeReset()
        {
            for (int i = 0; i < puzzleTargets.Length; i++)
            {
                puzzleTargets[i]?.RuntimeReset();
            }

            for (int i = 0; i < storyTriggers.Length; i++)
            {
                storyTriggers[i]?.RuntimeReset();
            }

            for (int i = 0; i < completionTriggers.Length; i++)
            {
                completionTriggers[i]?.RuntimeReset();
            }

            for (int i = 0; i < doors.Length; i++)
            {
                doors[i]?.RuntimeReset();
            }
        }
    }
}
