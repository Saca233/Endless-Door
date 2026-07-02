using System.Collections.Generic;
using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class PlayerControlGate : MonoBehaviour, IRuntimeResettable
    {
        private readonly HashSet<PlayerControlLockToken> activeLocks = new HashSet<PlayerControlLockToken>();
        private int nextLockId = 1;

        public bool IsLocked => activeLocks.Count > 0;
        public int ActiveLockCount => activeLocks.Count;
        public int ResetOrder => -100;

        public PlayerControlLockToken AcquireLock(string ownerName)
        {
            string safeOwnerName = string.IsNullOrWhiteSpace(ownerName) ? "Unnamed" : ownerName;
            PlayerControlLockToken token = new PlayerControlLockToken(nextLockId++, safeOwnerName);
            activeLocks.Add(token);
            return token;
        }

        public bool ReleaseLock(PlayerControlLockToken token)
        {
            return token.IsValid && activeLocks.Remove(token);
        }

        public void ClearAllLocks()
        {
            activeLocks.Clear();
        }

        public void RuntimeReset()
        {
            ClearAllLocks();
        }
    }
}
