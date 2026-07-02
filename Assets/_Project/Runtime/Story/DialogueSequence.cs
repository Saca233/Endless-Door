using System;
using System.Collections.Generic;
using UnityEngine;

namespace OwariNakiTobira
{
    [CreateAssetMenu(menuName = "OwariNakiTobira/Story/Dialogue Sequence", fileName = "DialogueSequence")]
    public sealed class DialogueSequence : ScriptableObject
    {
        [SerializeField] private DialogueLineData[] lines = Array.Empty<DialogueLineData>();

        public IReadOnlyList<DialogueLineData> Lines => lines;
        public int LineCount => lines.Length;

        public DialogueLineData GetLine(int index)
        {
            return lines[index];
        }

        public void SetLines(DialogueLineData[] value)
        {
            lines = value ?? Array.Empty<DialogueLineData>();
        }
    }
}
