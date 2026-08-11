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
    /// V0 placer (OVERWORLD_V0_PLAN.md W0.6): places one marker per <see cref="PoiRequest"/> across
    /// a caller-supplied set of candidate chunks, seeded from <see cref="IWorldSeed"/>.Fork("poi"),
    /// each request preferring a biome band read from <see cref="IBiomeSampler"/> at the chunk
    /// center. Repeating the same kind in the request list places multiple markers of that kind
    /// (see <see cref="PoiDemoScenario"/>) - this replaces the original stub's "3-5 markers, at most
    /// one per kind" ceiling, which couldn't scale to "ten camps" style demo requests. The
    /// no-request constructor keeps that original behaviour byte-for-byte (same rng call order) for
    /// existing callers/tests that still want a small randomized set. All markers are computed once
    /// at construction (not on every <see cref="PlaceForChunk"/> call), matching the System Card's
    /// "no global scans each frame" performance note.
    /// </summary>
    public sealed class StubPoiPlacer : IPoiPlacer
    {
        // Coarse pre-filter so candidate chunks skip the lowest, most lake-prone ground before
        // the real per-position water check (GameDev.World.Water.OverworldWaterZones, queried by
        // PoiMarkerMenu against the actual raycast surface point) rejects any marker that still
        // lands inside a water zone.
        private const float MinHeight01 = 0.12f;

        private static readonly Dictionary<PoiKind, BiomeId[]> DefaultPreferredBiomes = new Dictionary<PoiKind, BiomeId[]>
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
            var candidates = BuildCandidates(biomeSampler, candidateChunks);
            var requests = BuildDefaultRequests(rng);
            PlaceRequests(requests, candidates, rng);
        }

        public StubPoiPlacer(
            IWorldSeed worldSeed,
            IBiomeSampler biomeSampler,
            IEnumerable<ChunkCoord> candidateChunks,
            IEnumerable<PoiRequest> requests)
        {
            var rng = worldSeed.Fork("poi");
            var candidates = BuildCandidates(biomeSampler, candidateChunks);
            PlaceRequests(requests, candidates, rng);
        }

        public IReadOnlyList<PoiMarker> PlaceForChunk(ChunkCoord chunk)
        {
            return markersByChunk.TryGetValue(chunk, out var marker)
                ? new[] { marker }
                : Array.Empty<PoiMarker>();
        }

        /// <summary>All markers from the construction pass (S6 TownApproach lookup).</summary>
        public IReadOnlyList<PoiMarker> AllMarkers()
        {
            if (markersByChunk.Count == 0)
                return Array.Empty<PoiMarker>();
            var list = new List<PoiMarker>(markersByChunk.Count);
            foreach (var kv in markersByChunk)
                list.Add(kv.Value);
            return list;
        }

        private static List<(ChunkCoord Chunk, Vector3 Center, BiomeId Biome)> BuildCandidates(
            IBiomeSampler biomeSampler, IEnumerable<ChunkCoord> candidateChunks)
        {
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

            return candidates;
        }

        // Reproduces the original stub's exact rng call order (count roll, then shuffle) so
        // existing seed -> marker-set expectations (StubPoiPlacerTests) stay unchanged. Landmark is
        // deliberately excluded from the shuffled pool - it's a quota-only kind (one per biome via
        // PoiDemoScenario), not part of this grab-bag of flavor kinds.
        private static List<PoiRequest> BuildDefaultRequests(System.Random rng)
        {
            var count = rng.Next(3, 6); // 3..5 inclusive
            // S6: always reserve a TownApproach when possible so settlement hook has a target.
            var requests = new List<PoiRequest>(count)
            {
                new PoiRequest(PoiKind.TownApproach, DefaultPreferredBiomes[PoiKind.TownApproach])
            };

            var others = Shuffle(
                ((PoiKind[])Enum.GetValues(typeof(PoiKind)))
                .Where(k => k != PoiKind.TownApproach && DefaultPreferredBiomes.ContainsKey(k))
                .ToArray(),
                rng);
            for (var o = 0; o < others.Length && requests.Count < count; o++)
            {
                requests.Add(new PoiRequest(others[o], DefaultPreferredBiomes[others[o]]));
            }

            return requests;
        }

        private void PlaceRequests(
            IEnumerable<PoiRequest> requests,
            List<(ChunkCoord Chunk, Vector3 Center, BiomeId Biome)> candidates,
            System.Random rng)
        {
            var perKindIndex = new Dictionary<PoiKind, int>();
            foreach (var request in requests)
            {
                var pool = candidates.Where(c => request.PreferredBiomes.Contains(c.Biome)).ToList();
                if (pool.Count == 0)
                {
                    pool = candidates;
                }

                if (pool.Count == 0)
                {
                    break; // no viable chunk left at all - place fewer markers than requested
                }

                var picked = pool[rng.Next(pool.Count)];
                candidates.RemoveAll(c => c.Chunk == picked.Chunk);

                var index = perKindIndex.TryGetValue(request.Kind, out var existing) ? existing : 0;
                perKindIndex[request.Kind] = index + 1;

                var id = new PoiId($"{request.Kind}_{index}");
                markersByChunk[picked.Chunk] = new PoiMarker(id, request.Kind, picked.Center, picked.Biome);
            }
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
