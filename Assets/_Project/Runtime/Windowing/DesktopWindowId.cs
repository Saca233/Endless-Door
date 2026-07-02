using System;
using UnityEngine;

namespace OwariNakiTobira
{
    [Serializable]
    public struct DesktopWindowId : IEquatable<DesktopWindowId>
    {
        [SerializeField] private string value;

        public DesktopWindowId(string value)
        {
            this.value = value;
        }

        public string Value => value;
        public bool IsValid => !string.IsNullOrWhiteSpace(value);

        public static DesktopWindowId NewId()
        {
            return new DesktopWindowId(Guid.NewGuid().ToString("N"));
        }

        public bool Equals(DesktopWindowId other)
        {
            return string.Equals(value, other.value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is DesktopWindowId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return value == null ? 0 : StringComparer.Ordinal.GetHashCode(value);
        }

        public override string ToString()
        {
            return IsValid ? value : "<invalid-window-id>";
        }

        public static bool operator ==(DesktopWindowId left, DesktopWindowId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DesktopWindowId left, DesktopWindowId right)
        {
            return !left.Equals(right);
        }
    }
}
