using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.World.Landscape;
using GameDev.World.Seed;
using GameDev.World.Streaming;
using GameDev.World.Terrain;
using UnityEngine;

namespace GameDev.World.Poi
{
    /// <summary>
    /// <see cref="IPoiPlacer"/> facade over <see cref="PoiPlacementEngine"/> +
    /// <see cref="PoiPlacementCatalog"/>. Optional <see cref="LandscapeCostField"/> enables
    /// slope / footprint / lake hard constraints; without it, biome + height + spread still apply.
    /// </summary>
    public sealed class StubPoiPlacer : IPoiPlacer
    {
        private readonly PoiPlacementEngine engine;

        public StubPoiPlacer(IWorldSeed worldSeed, IBiomeSampler biomeSampler, IEnumerable<ChunkCoord> candidateChunks)
            : this(worldSeed, biomeSampler, candidateChunks, BuildDefaultRequests(worldSeed.Fork("poi")), null)
        {
        }

        public StubPoiPlacer(
            IWorldSeed worldSeed,
            IBiomeSampler biomeSampler,
            IEnumerable<ChunkCoord> candidateChunks,
            IEnumerable<PoiRequest> requests)
            : this(worldSeed, biomeSampler, candidateChunks, requests, null)
        {
        }

        public StubPoiPlacer(
            IWorldSeed worldSeed,
            IBiomeSampler biomeSampler,
            IEnumerable<ChunkCoord> candidateChunks,
            IEnumerable<PoiRequest> requests,
            LandscapeCostField analysis)
        {
            engine = new PoiPlacementEngine(worldSeed, biomeSampler, candidateChunks, requests, analysis);
        }

        public IReadOnlyList<PoiMarker> PlaceForChunk(ChunkCoord chunk) => engine.PlaceForChunk(chunk);

        public IReadOnlyList<PoiMarker> AllMarkers() => engine.AllMarkers();

        private static List<PoiRequest> BuildDefaultRequests(System.Random rng)
        {
            var count = rng.Next(3, 6);
            var requests = new List<PoiRequest>(count)
            {
                new PoiRequest(PoiKind.TownApproach, PoiPlacementCatalog.Get(PoiKind.TownApproach).PreferredBiomes)
            };

            var others = Shuffle(
                ((PoiKind[])Enum.GetValues(typeof(PoiKind)))
                .Where(k => k != PoiKind.TownApproach && k != PoiKind.Landmark && PoiPlacementCatalog.All.ContainsKey(k))
                .ToArray(),
                rng);
            for (var o = 0; o < others.Length && requests.Count < count; o++)
            {
                requests.Add(new PoiRequest(others[o], PoiPlacementCatalog.Get(others[o]).PreferredBiomes));
            }

            return requests;
        }

        private static T[] Shuffle<T>(T[] source, System.Random rng)
        {
            var result = (T[])source.Clone();
            for (var i = result.Length - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (result[i], result[j]) = (result[j], result[i]);
            }

            return result;
        }
    }
}
