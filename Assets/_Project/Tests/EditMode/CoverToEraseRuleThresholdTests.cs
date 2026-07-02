using NUnit.Framework;

namespace OwariNakiTobira.Tests
{
    public sealed class CoverToEraseRuleThresholdTests
    {
        [Test]
        public void ActivatesAtActivationThreshold()
        {
            bool active = CoverToEraseRule.EvaluateHysteresis(false, 0.55f, 0.55f, 0.45f);

            Assert.IsTrue(active);
        }

        [Test]
        public void DeactivatesAtDeactivationThreshold()
        {
            bool active = CoverToEraseRule.EvaluateHysteresis(true, 0.45f, 0.55f, 0.45f);

            Assert.IsFalse(active);
        }

        [Test]
        public void HysteresisKeepsCurrentStateBetweenThresholds()
        {
            Assert.IsTrue(CoverToEraseRule.EvaluateHysteresis(true, 0.5f, 0.55f, 0.45f));
            Assert.IsFalse(CoverToEraseRule.EvaluateHysteresis(false, 0.5f, 0.55f, 0.45f));
        }
    }
}
