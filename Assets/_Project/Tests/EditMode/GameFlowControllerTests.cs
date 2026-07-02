using NUnit.Framework;

namespace OwariNakiTobira.Tests
{
    public sealed class GameFlowControllerTests
    {
        [Test]
        public void ValidGameFlowTransitionsAreAccepted()
        {
            Assert.IsTrue(GameFlowTransitionRules.IsValid(GameFlowState.Boot, GameFlowState.DesktopIntro));
            Assert.IsTrue(GameFlowTransitionRules.IsValid(GameFlowState.DesktopIntro, GameFlowState.Level01));
            Assert.IsTrue(GameFlowTransitionRules.IsValid(GameFlowState.Level03, GameFlowState.FinalSequence));
            Assert.IsTrue(GameFlowTransitionRules.IsValid(GameFlowState.Epilogue, GameFlowState.StaticEnding));
        }

        [Test]
        public void InvalidGameFlowTransitionsAreRejected()
        {
            Assert.IsFalse(GameFlowTransitionRules.IsValid(GameFlowState.Boot, GameFlowState.Level03));
            Assert.IsFalse(GameFlowTransitionRules.IsValid(GameFlowState.StaticEnding, GameFlowState.Level01));
        }
    }
}
