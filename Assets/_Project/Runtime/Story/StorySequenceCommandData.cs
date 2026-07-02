using System;
using UnityEngine;
using UnityEngine.Events;

namespace OwariNakiTobira
{
    [Serializable]
    public sealed class StorySequenceCommandData
    {
        [SerializeField] private StoryCommandType commandType;
        [SerializeField] private DialogueSequence dialogueSequence;
        [SerializeField, Min(0f)] private float duration = 1f;
        [SerializeField] private string flagId;
        [SerializeField] private StoryFlagValueKind flagValueKind;
        [SerializeField] private bool boolValue = true;
        [SerializeField] private int intValue;
        [SerializeField] private bool temporaryFlag;
        [SerializeField] private CoverToEraseRule puzzleRule;
        [SerializeField] private DesktopWindowController desktopWindow;
        [SerializeField] private Vector2 windowPosition;
        [SerializeField, Min(0f)] private float windowMoveDuration = 0.25f;
        [SerializeField] private ScreenFadeView fadeView;
        [SerializeField] private string sceneName;
        [SerializeField] private DoorController door;
        [SerializeField] private UnityEvent configuredEvent = new UnityEvent();

        public StorySequenceCommandData()
        {
        }

        public StoryCommandType CommandType => commandType;
        public DialogueSequence DialogueSequence => dialogueSequence;
        public float Duration => duration;
        public string FlagId => flagId;
        public StoryFlagValueKind FlagValueKind => flagValueKind;
        public bool BoolValue => boolValue;
        public int IntValue => intValue;
        public bool TemporaryFlag => temporaryFlag;
        public CoverToEraseRule PuzzleRule => puzzleRule;
        public DesktopWindowController DesktopWindow => desktopWindow;
        public Vector2 WindowPosition => windowPosition;
        public float WindowMoveDuration => windowMoveDuration;
        public ScreenFadeView FadeView => fadeView;
        public string SceneName => sceneName;
        public DoorController Door => door;
        public UnityEvent ConfiguredEvent => configuredEvent;

        public StorySequenceCommandData(StoryCommandType commandType)
        {
            this.commandType = commandType;
        }

        public static StorySequenceCommandData ShowDialogue(DialogueSequence sequence)
        {
            StorySequenceCommandData command = new StorySequenceCommandData(StoryCommandType.ShowDialogue);
            command.dialogueSequence = sequence;
            return command;
        }

        public static StorySequenceCommandData Wait(float seconds)
        {
            StorySequenceCommandData command = new StorySequenceCommandData(StoryCommandType.Wait);
            command.duration = Mathf.Max(0f, seconds);
            return command;
        }

        public static StorySequenceCommandData SetBoolFlag(string stableId, bool value, bool temporary = false)
        {
            StorySequenceCommandData command = new StorySequenceCommandData(StoryCommandType.SetFlag);
            command.flagId = stableId;
            command.flagValueKind = StoryFlagValueKind.Boolean;
            command.boolValue = value;
            command.temporaryFlag = temporary;
            return command;
        }

        public static StorySequenceCommandData SetIntFlag(string stableId, int value, bool temporary = false)
        {
            StorySequenceCommandData command = new StorySequenceCommandData(StoryCommandType.SetFlag);
            command.flagId = stableId;
            command.flagValueKind = StoryFlagValueKind.Integer;
            command.intValue = value;
            command.temporaryFlag = temporary;
            return command;
        }
    }
}
