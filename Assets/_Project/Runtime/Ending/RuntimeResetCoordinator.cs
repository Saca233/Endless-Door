using System;
using System.Collections.Generic;
using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class RuntimeResetCoordinator : MonoBehaviour
    {
        private static readonly string[] resetOrderDescription =
        {
            "Story flags",
            "Story triggers",
            "Dialogue UI",
            "Window positions",
            "Puzzle effects",
            "Error window pools",
            "Door state",
            "Desktop data objects",
            "Player representations",
            "Player input locks",
            "Additive level state"
        };

        [SerializeField] private StoryFlagService storyFlagService;
        [SerializeField] private StorySequenceRunner storySequenceRunner;
        [SerializeField] private DialogueView dialogueView;
        [SerializeField] private PlayerControlGate playerControlGate;
        [SerializeField] private AdditiveLevelLoader additiveLevelLoader;
        [SerializeField] private StoryTriggerVolume[] storyTriggers = Array.Empty<StoryTriggerVolume>();
        [SerializeField] private DesktopWindowController[] desktopWindows = Array.Empty<DesktopWindowController>();
        [SerializeField] private WindowPuzzleTarget[] puzzleTargets = Array.Empty<WindowPuzzleTarget>();
        [SerializeField] private CoverToEraseRule[] coverRules = Array.Empty<CoverToEraseRule>();
        [SerializeField] private ProjectedWindowBarrierRule[] projectedBarrierRules = Array.Empty<ProjectedWindowBarrierRule>();
        [SerializeField] private ErrorWindowPool[] errorWindowPools = Array.Empty<ErrorWindowPool>();
        [SerializeField] private DoorController[] doors = Array.Empty<DoorController>();
        [SerializeField] private FinalDoorController[] finalDoors = Array.Empty<FinalDoorController>();
        [SerializeField] private DesktopDataObject[] desktopDataObjects = Array.Empty<DesktopDataObject>();
        [SerializeField] private PlayerRepresentationTransfer[] playerTransfers = Array.Empty<PlayerRepresentationTransfer>();

        public static IReadOnlyList<string> ResetOrderDescription => resetOrderDescription;

        public void SetCoreServices(
            StoryFlagService flags,
            StorySequenceRunner runner,
            DialogueView dialogue,
            PlayerControlGate controlGate,
            AdditiveLevelLoader levelLoader)
        {
            storyFlagService = flags;
            storySequenceRunner = runner;
            dialogueView = dialogue;
            playerControlGate = controlGate;
            additiveLevelLoader = levelLoader;
        }

        public void SetResetTargets(
            StoryTriggerVolume[] triggers,
            DesktopWindowController[] windows,
            WindowPuzzleTarget[] targets,
            ErrorWindowPool[] pools,
            DoorController[] levelDoors,
            DesktopDataObject[] dataObjects,
            PlayerRepresentationTransfer[] transfers)
        {
            storyTriggers = triggers ?? Array.Empty<StoryTriggerVolume>();
            desktopWindows = windows ?? Array.Empty<DesktopWindowController>();
            puzzleTargets = targets ?? Array.Empty<WindowPuzzleTarget>();
            errorWindowPools = pools ?? Array.Empty<ErrorWindowPool>();
            doors = levelDoors ?? Array.Empty<DoorController>();
            desktopDataObjects = dataObjects ?? Array.Empty<DesktopDataObject>();
            playerTransfers = transfers ?? Array.Empty<PlayerRepresentationTransfer>();
        }

        public void ExecuteTransientReset()
        {
            storySequenceRunner?.CancelCurrentSequence();
            storyFlagService?.ResetTemporaryFlags();

            ResetArray(storyTriggers);
            dialogueView?.Hide();
            ResetArray(desktopWindows);
            ResetArray(puzzleTargets);
            ResetArray(coverRules);
            ResetArray(projectedBarrierRules);
            ResetArray(errorWindowPools);
            ResetArray(doors);
            ResetArray(finalDoors);
            ResetArray(desktopDataObjects);
            ResetArray(playerTransfers);
            playerControlGate?.ClearAllLocks();

            additiveLevelLoader?.CancelActiveRequest();
            additiveLevelLoader?.UnloadCurrentLevel();
        }

        private static void ResetArray<T>(T[] resetTargets) where T : class, IRuntimeResettable
        {
            if (resetTargets == null)
            {
                return;
            }

            for (int i = 0; i < resetTargets.Length; i++)
            {
                resetTargets[i]?.RuntimeReset();
            }
        }
    }
}
