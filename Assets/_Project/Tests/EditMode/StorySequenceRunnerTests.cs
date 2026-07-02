using NUnit.Framework;
using UnityEngine;

namespace OwariNakiTobira.Tests
{
    public sealed class StorySequenceRunnerTests
    {
        [Test]
        public void CancellationReleasesOwnedPlayerLocks()
        {
            GameObject host = new GameObject("StorySequenceRunner");
            try
            {
                PlayerControlGate gate = host.AddComponent<PlayerControlGate>();
                StorySequenceRunner runner = host.AddComponent<StorySequenceRunner>();
                runner.SetPlayerControlGate(gate);

                runner.LockPlayerForSequence("test-a");
                runner.LockPlayerForSequence("test-b");

                Assert.IsTrue(gate.IsLocked);
                Assert.AreEqual(2, runner.OwnedPlayerLockCount);

                runner.CancelCurrentSequence();

                Assert.IsFalse(gate.IsLocked);
                Assert.AreEqual(0, runner.OwnedPlayerLockCount);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void MultiplePlayerLocksRequireEveryOwnerToRelease()
        {
            GameObject host = new GameObject("PlayerControlGate");
            try
            {
                PlayerControlGate gate = host.AddComponent<PlayerControlGate>();
                PlayerControlLockToken first = gate.AcquireLock("first");
                PlayerControlLockToken second = gate.AcquireLock("second");

                gate.ReleaseLock(first);

                Assert.IsTrue(gate.IsLocked);
                Assert.AreEqual(1, gate.ActiveLockCount);

                gate.ReleaseLock(second);

                Assert.IsFalse(gate.IsLocked);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}
