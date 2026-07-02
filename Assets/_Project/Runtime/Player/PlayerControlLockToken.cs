using System;

namespace OwariNakiTobira
{
    public readonly struct PlayerControlLockToken : IEquatable<PlayerControlLockToken>
    {
        internal PlayerControlLockToken(int id, string ownerName)
        {
            Id = id;
            OwnerName = ownerName;
        }

        public int Id { get; }
        public string OwnerName { get; }
        public bool IsValid => Id > 0;

        public bool Equals(PlayerControlLockToken other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerControlLockToken other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public static bool operator ==(PlayerControlLockToken left, PlayerControlLockToken right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PlayerControlLockToken left, PlayerControlLockToken right)
        {
            return !left.Equals(right);
        }
    }
}
