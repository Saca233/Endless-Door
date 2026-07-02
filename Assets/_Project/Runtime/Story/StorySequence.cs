using System;
using System.Collections.Generic;
using UnityEngine;

namespace OwariNakiTobira
{
    [CreateAssetMenu(menuName = "OwariNakiTobira/Story/Story Sequence", fileName = "StorySequence")]
    public sealed class StorySequence : ScriptableObject
    {
        [SerializeField] private StorySequenceCommandData[] commands = Array.Empty<StorySequenceCommandData>();

        public IReadOnlyList<StorySequenceCommandData> Commands => commands;
        public int CommandCount => commands.Length;

        public StorySequenceCommandData GetCommand(int index)
        {
            return commands[index];
        }

        public void SetCommands(StorySequenceCommandData[] value)
        {
            commands = value ?? Array.Empty<StorySequenceCommandData>();
        }
    }
}
