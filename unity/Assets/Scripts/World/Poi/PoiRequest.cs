using System.Collections.Generic;
using GameDev.World.Terrain;
using UnityEngine;

namespace GameDev.World.Poi
{
    /// <summary>
    /// One requested marker for <see cref="StubPoiPlacer"/>'s quota constructor - repeat the same
    /// kind to request multiple markers of it (e.g. ten <see cref="PoiKind.Camp"/> requests place
    /// ten camps), replacing the original stub's "at most one marker per kind" ceiling.
    /// Optional preferred 3×3 sector guides spatial distribution without cascading RNG.
    /// </summary>
    public readonly struct PoiRequest
    {
        public const int AnySector = -1;

        public PoiKind Kind { get; }
        public IReadOnlyList<BiomeId> PreferredBiomes { get; }

        /// <summary>Preferred sector X in [0,2], or <see cref="AnySector"/>.</summary>
        public int PreferredSectorX { get; }

        /// <summary>Preferred sector Z in [0,2], or <see cref="AnySector"/>.</summary>
        public int PreferredSectorZ { get; }

        /// <summary>When true, this TownApproach is the map hub (prefer center sector).</summary>
        public bool IsMainHub { get; }

        public PoiRequest(PoiKind kind, IReadOnlyList<BiomeId> preferredBiomes)
            : this(kind, preferredBiomes, AnySector, AnySector, isMainHub: false)
        {
        }

        public PoiRequest(
            PoiKind kind,
            IReadOnlyList<BiomeId> preferredBiomes,
            int preferredSectorX,
            int preferredSectorZ,
            bool isMainHub = false)
        {
            Kind = kind;
            PreferredBiomes = preferredBiomes;
            PreferredSectorX = preferredSectorX;
            PreferredSectorZ = preferredSectorZ;
            IsMainHub = isMainHub;
        }

        public bool HasPreferredSector =>
            PreferredSectorX >= 0 && PreferredSectorZ >= 0;
    }
}
