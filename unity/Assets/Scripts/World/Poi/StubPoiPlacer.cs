using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.World.Seed;
using GameDev.World.Streaming;
using GameDev.World.Terrain;
using UnityEngine;

namespace GameDev.World.Poi
{
    /// <summary>
    /// V0 placer (OVERWORLD_V0_PLAN.md W0.6): 3-5 markers across a caller-supplied set of
    /// candidate chunks, seeded from <see cref="IWorldSeed"/>.Fork("poi"), each kind preferring a
    /// biome band read from <see cref="IBiomeSampler"/> at the chunk center. All markers are
    /// computed once at construction (not on every <see cref="PlaceForChunk"/> call), matching the
    /// System Card's "no global scans each frame" performance note.
    /// </summary>
    public sealed class StubPoiPlacer : IPoiPlacer
    {
        // Above the V0 water level (0.1) so markers don't land in a lake.
        private const float MinHeight01 = 0.12f;

        private static readonly Dictionary<PoiKind, BiomeId[]> PreferredBiomes = new Dictionary<PoiKind, BiomeId[]>
        {
            { PoiKind.TownApproach, new[] { BiomeId.Meadow } },
            { PoiKind.Ruin, new[] { BiomeId.Forest, BiomeId.Meadow } },
            { PoiKind.Camp, new[] { BiomeId.Meadow, BiomeId.Forest } },
            { PoiKind.Shrine, new[] { BiomeId.Forest, BiomeId.Mountain } },
            { PoiKind.CaveMouth, new[] { BiomeId.Mountain } },
        };

        private readonly Dictionary<ChunkCoord, PoiMarker> markersByChunk = new Dictionary<ChunkCoord, PoiMarker>();

        public StubPoiPlacer(IWorldSeed worldSeed, IBiomeSampler biomeSampler, IEnumerable<ChunkCoord> candidateChunks)
        {
            var rng = worldSeed.Fork("poi");

            var candidates = new List<(ChunkCoord Chunk, Vector3 Center, BiomeId Biome)>();
            foreach (var chunk in candidateChunks)
            {
                var center = chunk.Center;
                var height = biomeSampler.SampleHeight(center.x, center.z);
                if (height < MinHeight01)
                {
                    continue;
                }

                candidates.Add((chunk, center, biomeSampler.SampleBiome(center.x, center.z)));
            }

            var kinds = Shuffle((PoiKind[])Enum.GetValues(typeof(PoiKind)), rng);
            var count = rng.Next(3, 6); // 3..5 inclusive

            for (var i = 0; i < count && i < kinds.Length; i++)
            {
                var kind = kinds[i];
                var preferredBiomes = PreferredBiomes[kind];
                var pool = candidates.Where(c => Array.IndexOf(preferredBiomes, c.Biome) >= 0).ToList();
                if (pool.Count == 0)
                {
                    pool = candidates;
                }

                if (pool.Count == 0)
                {
                    break; // no viable chunk left at all - place fewer than `count` markers
                }

                var picked = pool[rng.Next(pool.Count)];
                candidates.RemoveAll(c => c.Chunk == picked.Chunk);

                var id = new PoiId($"{kind}_{i}");
                markersByChunk[picked.Chunk] = new PoiMarker(id, kind, picked.Center, picked.Biome);
            }
        }

        public IReadOnlyList<PoiMarker> PlaceForChunk(ChunkCoord chunk)
        {
            return markersByChunk.TryGetValue(chunk, out var marker)
                ? new[] { marker }
                : Array.Empty<PoiMarker>();
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
