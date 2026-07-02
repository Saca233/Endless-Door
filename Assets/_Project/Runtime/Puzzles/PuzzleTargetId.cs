using System;
using UnityEngine;

namespace OwariNakiTobira
{
    [Serializable]
    public struct PuzzleTargetId : IEquatable<PuzzleTargetId>
    {
        [SerializeField] private string value;

        public PuzzleTargetId(string value)
        {
            this.value = value;
        }

        public string Value => value;
        public bool IsValid => !string.IsNullOrWhiteSpace(value);

        public static PuzzleTargetId NewId()
        {
            return new PuzzleTargetId(Guid.NewGuid().ToString("N"));
        }

        public bool Equals(PuzzleTargetId other)
        {
            return string.Equals(value, other.value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PuzzleTargetId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return value == null ? 0 : StringComparer.Ordinal.GetHashCode(value);
        }

        public override string ToString()
        {
            return IsValid ? value : "<invalid-puzzle-target-id>";
        }
    }
}
