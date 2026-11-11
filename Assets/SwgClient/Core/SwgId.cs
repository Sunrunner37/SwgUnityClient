using System;
using System.Globalization;

namespace Swg.Client.Core
{
    /// <summary>An SWG object identity, distinct from a template CRC or Unity instance ID.</summary>
    public readonly struct SwgId : IEquatable<SwgId>, IComparable<SwgId>, IFormattable
    {
        public ulong Value { get; }
        public bool IsNone => Value == 0;
        public static SwgId None => default;

        public SwgId(ulong value) => Value = value;
        public bool Equals(SwgId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is SwgId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public int CompareTo(SwgId other) => Value.CompareTo(other.Value);
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
        public string ToString(IFormatProvider provider) => Value.ToString(provider);
        public string ToString(string format, IFormatProvider provider) => Value.ToString(format, provider);
        public static implicit operator SwgId(ulong value) => new SwgId(value);
        public static explicit operator ulong (SwgId swgId) => swgId.Value;
        public static bool operator ==(SwgId left, SwgId right) => left.Equals(right);
        public static bool operator !=(SwgId left, SwgId right) => !left.Equals(right);
    }
}
