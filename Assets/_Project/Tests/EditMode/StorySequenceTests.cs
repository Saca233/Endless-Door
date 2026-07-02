using NUnit.Framework;
using UnityEngine;

namespace OwariNakiTobira.Tests
{
    public sealed class StorySequenceTests
    {
        [Test]
        public void StorySequencePreservesCommandOrder()
        {
            StorySequence sequence = ScriptableObject.CreateInstance<StorySequence>();
            try
            {
                sequence.SetCommands(new[]
                {
                    new StorySequenceCommandData(StoryCommandType.LockPlayer),
                    StorySequenceCommandData.Wait(0.25f),
                    StorySequenceCommandData.SetBoolFlag("test.flag", true),
                    new StorySequenceCommandData(StoryCommandType.UnlockPlayer)
                });

                Assert.AreEqual(StoryCommandType.LockPlayer, sequence.GetCommand(0).CommandType);
                Assert.AreEqual(StoryCommandType.Wait, sequence.GetCommand(1).CommandType);
                Assert.AreEqual(StoryCommandType.SetFlag, sequence.GetCommand(2).CommandType);
                Assert.AreEqual(StoryCommandType.UnlockPlayer, sequence.GetCommand(3).CommandType);
            }
            finally
            {
                Object.DestroyImmediate(sequence);
            }
        }
    }
}
