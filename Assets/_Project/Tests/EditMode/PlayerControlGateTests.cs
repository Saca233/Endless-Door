using NUnit.Framework;
using UnityEngine;

namespace OwariNakiTobira.Tests
{
    public sealed class PlayerControlGateTests
    {
        [Test]
        public void ReleasesInputOnlyAfterEveryOwnerUnlocks()
        {
            GameObject player = new GameObject("ControlGateTestPlayer");
            PlayerControlGate gate = player.AddComponent<PlayerControlGate>();

            PlayerControlLockToken dialogueLock = gate.AcquireLock("Dialogue");
            PlayerControlLockToken windowLock = gate.AcquireLock("WindowDrag");

            Assert.IsTrue(gate.IsLocked);
            Assert.AreEqual(2, gate.ActiveLockCount);

            Assert.IsTrue(gate.ReleaseLock(dialogueLock));
            Assert.IsTrue(gate.IsLocked);
            Assert.AreEqual(1, gate.ActiveLockCount);

            Assert.IsTrue(gate.ReleaseLock(windowLock));
            Assert.IsFalse(gate.IsLocked);
            Assert.AreEqual(0, gate.ActiveLockCount);

            Object.DestroyImmediate(player);
        }

        [Test]
        public void ClearAllLocksRestoresInputForRuntimeReset()
        {
            GameObject player = new GameObject("ControlGateResetTestPlayer");
            PlayerControlGate gate = player.AddComponent<PlayerControlGate>();
            gate.AcquireLock("EndingReset");
            gate.AcquireLock("Dialogue");

            gate.ClearAllLocks();

            Assert.IsFalse(gate.IsLocked);
            Assert.AreEqual(0, gate.ActiveLockCount);

            Object.DestroyImmediate(player);
        }
    }
}
