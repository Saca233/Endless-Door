using NUnit.Framework;
using UnityEngine;

namespace OwariNakiTobira.Tests
{
    public sealed class StoryFlagServiceTests
    {
        [Test]
        public void RuntimeResetClearsTemporaryFlagsAndKeepsPersistentFlags()
        {
            GameObject host = new GameObject("StoryFlagService");
            try
            {
                StoryFlagService flags = host.AddComponent<StoryFlagService>();
                flags.SetBool("persistent.bool", true);
                flags.SetBool("temporary.bool", true, true);
                flags.SetInt("persistent.int", 7);
                flags.SetInt("temporary.int", 3, true);

                flags.RuntimeReset();

                Assert.IsTrue(flags.GetBool("persistent.bool"));
                Assert.IsFalse(flags.TryGetBool("temporary.bool", out _));
                Assert.AreEqual(7, flags.GetInt("persistent.int"));
                Assert.IsFalse(flags.TryGetInt("temporary.int", out _));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}
