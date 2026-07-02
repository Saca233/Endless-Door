using System;

namespace OwariNakiTobira
{
    public readonly struct DesktopWindowMovementLockToken : IEquatable<DesktopWindowMovementLockToken>
    {
        internal DesktopWindowMovementLockToken(int id, string ownerName)
        {
            Id = id;
            OwnerName = ownerName;
        }

        public int Id { get; }
        public string OwnerName { get; }
        public bool IsValid => Id > 0;

        public bool Equals(DesktopWindowMovementLockToken other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is DesktopWindowMovementLockToken other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }
}
