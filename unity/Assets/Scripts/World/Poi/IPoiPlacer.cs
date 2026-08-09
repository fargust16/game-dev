using System.Collections.Generic;
using GameDev.World.Streaming;

namespace GameDev.World.Poi
{
    /// <summary>
    /// Deterministically places POI markers per chunk (17 Game Systems Architecture, W.4).
    /// Implementations must stay cheap enough to call per resident chunk, not scan the world.
    /// </summary>
    public interface IPoiPlacer
    {
        IReadOnlyList<PoiMarker> PlaceForChunk(ChunkCoord chunk);
    }
}
