using System.Collections.Generic;
using GameDev.World.Terrain;

namespace GameDev.World.Poi
{
    /// <summary>
    /// One requested marker for <see cref="StubPoiPlacer"/>'s quota constructor - repeat the same
    /// kind to request multiple markers of it (e.g. ten <see cref="PoiKind.Camp"/> requests place
    /// ten camps), replacing the original stub's "at most one marker per kind" ceiling.
    /// </summary>
    public readonly struct PoiRequest
    {
        public PoiKind Kind { get; }
        public IReadOnlyList<BiomeId> PreferredBiomes { get; }

        public PoiRequest(PoiKind kind, IReadOnlyList<BiomeId> preferredBiomes)
        {
            Kind = kind;
            PreferredBiomes = preferredBiomes;
        }
    }
}
