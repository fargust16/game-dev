using System;

namespace GameDev.World.Poi
{
    /// <summary>Stable id for a POI marker — same seed + placement pass reproduces the same value.</summary>
    public readonly struct PoiId : IEquatable<PoiId>
    {
        public string Value { get; }

        public PoiId(string value)
        {
            Value = value;
        }

        public bool Equals(PoiId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is PoiId other && Equals(other);

        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;

        public override string ToString() => Value;

        public static bool operator ==(PoiId a, PoiId b) => a.Equals(b);

        public static bool operator !=(PoiId a, PoiId b) => !a.Equals(b);
    }
}
