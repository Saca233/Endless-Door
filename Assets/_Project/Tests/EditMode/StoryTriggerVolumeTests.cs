using NUnit.Framework;
using UnityEngine;

namespace OwariNakiTobira.Tests
{
    public sealed class StoryTriggerVolumeTests
    {
        [Test]
        public void TriggerCanFireOncePerLoopAndReset()
        {
            GameObject host = new GameObject("StoryTriggerVolume");
            try
            {
                host.AddComponent<BoxCollider>();
                StoryTriggerVolume trigger = host.AddComponent<StoryTriggerVolume>();

                Assert.IsTrue(trigger.TryFire(host));
                Assert.IsFalse(trigger.TryFire(host));

                trigger.RuntimeReset();

                Assert.IsTrue(trigger.TryFire(host));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}
