using System;
using UnityEngine;

namespace OwariNakiTobira
{
    [Serializable]
    public sealed class DialogueLineData
    {
        [SerializeField] private string speaker;
        [SerializeField, TextArea(2, 5)] private string text;
        [SerializeField] private string emotionTag;
        [SerializeField] private Sprite portrait;
        [SerializeField] private AudioClip voiceClip;
        [SerializeField, Min(0f)] private float autoAdvanceTime;
        [SerializeField] private bool skippable = true;
        [SerializeField, Min(0f)] private float pauseAfterLine;

        public DialogueLineData()
        {
        }

        public string Speaker => speaker;
        public string Text => text;
        public string EmotionTag => emotionTag;
        public Sprite Portrait => portrait;
        public AudioClip VoiceClip => voiceClip;
        public float AutoAdvanceTime => autoAdvanceTime;
        public bool Skippable => skippable;
        public float PauseAfterLine => pauseAfterLine;

        public DialogueLineData(string speaker, string text, string emotionTag = "", Sprite portrait = null, AudioClip voiceClip = null, float autoAdvanceTime = 0f, bool skippable = true, float pauseAfterLine = 0f)
        {
            this.speaker = speaker;
            this.text = text;
            this.emotionTag = emotionTag;
            this.portrait = portrait;
            this.voiceClip = voiceClip;
            this.autoAdvanceTime = Mathf.Max(0f, autoAdvanceTime);
            this.skippable = skippable;
            this.pauseAfterLine = Mathf.Max(0f, pauseAfterLine);
        }
    }
}
