using NUnit.Framework;
using UnityEngine;

namespace OwariNakiTobira.Tests
{
    public sealed class DoorControllerTests
    {
        [Test]
        public void DoorOpensAndClosesWithoutFinalArtModel()
        {
            GameObject host = new GameObject("DoorController");
            try
            {
                DoorController door = host.AddComponent<DoorController>();

                Assert.IsTrue(door.Open());
                Assert.AreEqual(DoorState.Open, door.State);

                door.Close();

                Assert.AreEqual(DoorState.Closed, door.State);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void LockedDoorRejectsOpenUntilUnlocked()
        {
            GameObject host = new GameObject("DoorController");
            try
            {
                DoorController door = host.AddComponent<DoorController>();
                door.Lock();

                Assert.IsFalse(door.Open());
                Assert.AreEqual(DoorState.Locked, door.State);

                door.Unlock();
                Assert.IsTrue(door.Open());
                Assert.AreEqual(DoorState.Open, door.State);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}
