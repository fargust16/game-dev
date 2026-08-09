using GameDev.World.Terrain;
using UnityEngine;

namespace GameDev.World.Poi
{
    /// <summary>
    /// A placed POI marker. <see cref="Position"/> carries world XZ; Y is left at 0 here since
    /// placement is a pure function of seed + biome samples (no live terrain/mesh dependency, per
    /// W.4's "no mesh ownership") — callers that need exact ground height (e.g. for a debug gizmo
    /// or spawning dressing) resolve Y themselves, the same way vegetation scatter does.
    /// </summary>
    public readonly struct PoiMarker
    {
        public PoiId Id { get; }
        public PoiKind Kind { get; }
        public Vector3 Position { get; }
        public BiomeId Biome { get; }

        public PoiMarker(PoiId id, PoiKind kind, Vector3 position, BiomeId biome)
        {
            Id = id;
            Kind = kind;
            Position = position;
            Biome = biome;
        }
    }
}
