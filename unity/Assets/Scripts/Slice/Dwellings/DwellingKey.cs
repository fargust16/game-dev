using System;
using System.Globalization;

namespace GameDev.Slice.Dwellings
{
    /// <summary>The kinds of house a dwelling's rooms come from (city plan C1 §8; 15).</summary>
    public enum DwellingArchetype
    {
        Terrace,
        Tenement,
        Shop,
        Villa,
        Warehouse,
    }

    /// <summary>How well off a household is: it sets the Lock and the Stash (10.3 §6.1; 19 §6.3).</summary>
    public enum DwellingWealth
    {
        Poor,
        Middling,
        Rich,
    }

    /// <summary>
    /// Which dwelling a street door opens: its door's place, the kind of house, and the household's wealth. The street
    /// door passes it to the shared Dwelling scene as the journey's context, and the scene builds the same rooms from it
    /// every time (15, *The slice's Dwellings*).
    /// </summary>
    public readonly struct DwellingKey : IEquatable<DwellingKey>
    {
        private const char Separator = '|';

        public DwellingKey(string streetPortalId, string streetScene, int doorX, int doorZ, DwellingArchetype archetype, DwellingWealth wealth)
        {
            StreetPortalId = streetPortalId;
            StreetScene = streetScene;
            DoorX = doorX;
            DoorZ = doorZ;
            Archetype = archetype;
            Wealth = wealth;
        }

        /// <summary>The street door's portal id, which is also the dwelling's id.</summary>
        public string StreetPortalId { get; }

        /// <summary>The scene the street door stands in, where the dwelling's door leads back to.</summary>
        public string StreetScene { get; }

        /// <summary>The street door's place in decimetres, world X and Z: the keys of the dwelling's seed stream.</summary>
        public int DoorX { get; }

        public int DoorZ { get; }

        public DwellingArchetype Archetype { get; }

        public DwellingWealth Wealth { get; }

        /// <summary>The Lock of 10.3 §6.1: 2 in a poor home, 4 in a middling one, 6 in a rich one (owner-approved).</summary>
        public int Lock => Wealth == DwellingWealth.Poor ? 2 : Wealth == DwellingWealth.Middling ? 4 : 6;

        /// <summary>A street door's id from its place, in decimetres so two doors of a house never share one.</summary>
        public static string IdAt(int doorX, int doorZ) => $"dwelling.{doorX}.{doorZ}";

        public string ToContext()
        {
            return string.Join(Separator.ToString(), StreetPortalId, StreetScene, DoorX.ToString(CultureInfo.InvariantCulture),
                DoorZ.ToString(CultureInfo.InvariantCulture), Archetype.ToString(), Wealth.ToString());
        }

        public static bool TryParse(string context, out DwellingKey key)
        {
            key = default;
            var parts = string.IsNullOrEmpty(context) ? Array.Empty<string>() : context.Split(Separator);
            if (parts.Length != 6
                || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x)
                || !int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var z)
                || !Enum.TryParse(parts[4], out DwellingArchetype archetype)
                || !Enum.TryParse(parts[5], out DwellingWealth wealth)
                || string.IsNullOrEmpty(parts[0]) || string.IsNullOrEmpty(parts[1]))
            {
                return false;
            }

            key = new DwellingKey(parts[0], parts[1], x, z, archetype, wealth);
            return true;
        }

        public bool Equals(DwellingKey other) => ToContext() == other.ToContext();

        public override bool Equals(object obj) => obj is DwellingKey other && Equals(other);

        public override int GetHashCode() => ToContext().GetHashCode();

        public override string ToString() => ToContext();
    }
}
