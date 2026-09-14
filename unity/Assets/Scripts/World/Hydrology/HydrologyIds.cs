using System;

namespace GameDev.World.Hydrology
{
    /// <summary>Stable id for one river reach within a single generated hydrology.</summary>
    /// <remarks>
    /// An int rather than a string, unlike <c>PoiId</c>: reaches are produced by the thousand during
    /// analysis and compared constantly while the drainage tree is walked, and the id is never shown
    /// to a designer. Ordinal string comparison would cost both allocations and clarity here.
    /// <para>
    /// The value is the reach's index in the planner's own emission order, which is itself derived
    /// from a deterministic traversal — so sorting by id reproduces the same sequence on every run.
    /// </para>
    /// </remarks>
    public readonly struct ReachId : IEquatable<ReachId>, IComparable<ReachId>
    {
        /// <summary>Reserved value meaning "no reach". Valid ids start at 0.</summary>
        public const int NoneValue = -1;

        public static readonly ReachId None = new ReachId(NoneValue);

        public int Value { get; }

        public ReachId(int value)
        {
            Value = value;
        }

        public bool IsValid => Value >= 0;

        public bool Equals(ReachId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is ReachId other && Equals(other);

        public override int GetHashCode() => Value;

        public int CompareTo(ReachId other) => Value.CompareTo(other.Value);

        public override string ToString() => IsValid ? $"reach_{Value}" : "reach_none";

        public static bool operator ==(ReachId a, ReachId b) => a.Equals(b);

        public static bool operator !=(ReachId a, ReachId b) => !a.Equals(b);
    }

    /// <summary>Stable id for one lake basin within a single generated hydrology.</summary>
    /// <remarks>Same reasoning as <see cref="ReachId"/>.</remarks>
    public readonly struct LakeId : IEquatable<LakeId>, IComparable<LakeId>
    {
        public const int NoneValue = -1;

        public static readonly LakeId None = new LakeId(NoneValue);

        public int Value { get; }

        public LakeId(int value)
        {
            Value = value;
        }

        public bool IsValid => Value >= 0;

        public bool Equals(LakeId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is LakeId other && Equals(other);

        public override int GetHashCode() => Value;

        public int CompareTo(LakeId other) => Value.CompareTo(other.Value);

        public override string ToString() => IsValid ? $"lake_{Value}" : "lake_none";

        public static bool operator ==(LakeId a, LakeId b) => a.Equals(b);

        public static bool operator !=(LakeId a, LakeId b) => !a.Equals(b);
    }
}
