using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.World.Landscape;
using GameDev.World.Seed;
using GameDev.World.Streaming;
using GameDev.World.Terrain;
using UnityEngine;
using WorldScale = GameDev.World.WorldScale;

namespace GameDev.World.Poi
{
    /// <summary>
    /// Profile-driven POI placement: candidates → hard reject → soft score → pick.
    /// TownApproach requests should be listed first so towns claim terraces before flavor POIs.
    /// Each marker uses an isolated <c>Fork("poi", kind, index)</c> stream.
    /// Spatial distribution uses a 3×3 <see cref="WorldSectorMap"/> when requests declare preferred sectors.
    /// </summary>
    public sealed class PoiPlacementEngine
    {
        private readonly Dictionary<ChunkCoord, PoiMarker> markersByChunk =
            new Dictionary<ChunkCoord, PoiMarker>();

        public PoiPlacementEngine(
            IWorldSeed worldSeed,
            IBiomeSampler biomeSampler,
            IEnumerable<ChunkCoord> candidateChunks,
            IEnumerable<PoiRequest> requests,
            LandscapeCostField analysis = null)
        {
            var candidates = BuildCandidates(biomeSampler, candidateChunks);
            PlaceRequests(worldSeed, requests, candidates, analysis, biomeSampler);
        }

        public IReadOnlyList<PoiMarker> PlaceForChunk(ChunkCoord chunk) =>
            markersByChunk.TryGetValue(chunk, out var marker)
                ? new[] { marker }
                : Array.Empty<PoiMarker>();

        public IReadOnlyList<PoiMarker> AllMarkers()
        {
            if (markersByChunk.Count == 0)
            {
                return Array.Empty<PoiMarker>();
            }

            var list = new List<PoiMarker>(markersByChunk.Count);
            foreach (var kv in markersByChunk)
            {
                list.Add(kv.Value);
            }

            list.Sort(WorldDefinition.CompareMarkers);
            return list;
        }

        private static List<Candidate> BuildCandidates(
            IBiomeSampler biomeSampler, IEnumerable<ChunkCoord> candidateChunks)
        {
            var candidates = new List<Candidate>();
            foreach (var chunk in candidateChunks)
            {
                var center = chunk.Center;
                var height = biomeSampler.SampleHeight(center.x, center.z);
                candidates.Add(new Candidate(
                    chunk,
                    center,
                    biomeSampler.SampleBiome(center.x, center.z),
                    height));
            }

            // Stable candidate order so iteration never depends on unordered sets.
            candidates.Sort((a, b) =>
            {
                var x = a.Chunk.X.CompareTo(b.Chunk.X);
                return x != 0 ? x : a.Chunk.Z.CompareTo(b.Chunk.Z);
            });
            return candidates;
        }

        private void PlaceRequests(
            IWorldSeed worldSeed,
            IEnumerable<PoiRequest> requests,
            List<Candidate> candidates,
            LandscapeCostField analysis,
            IBiomeSampler biomeSampler)
        {
            var perKindIndex = new Dictionary<PoiKind, int>();
            var placedByKind = new Dictionary<PoiKind, List<Vector3>>();
            var townCenters = new List<Vector3>();
            var sectors = WorldSectorMap.FromCostField(analysis);
            var townOccupied = new bool[WorldSectorMap.GridSize, WorldSectorMap.GridSize];

            var ordered = requests
                .OrderBy(r => r.Kind == PoiKind.TownApproach ? 0 : 1)
                .ThenBy(r => r.IsMainHub ? 0 : 1)
                .ThenBy(r => (int)r.Kind)
                .ToList();

            foreach (var request in ordered)
            {
                var index = perKindIndex.TryGetValue(request.Kind, out var existing) ? existing : 0;
                perKindIndex[request.Kind] = index + 1;
                var poiRng = worldSeed.Fork("poi", (int)request.Kind, index);

                var profile = PoiPlacementCatalog.Get(request.Kind);
                var preferred = request.PreferredBiomes != null && request.PreferredBiomes.Count > 0
                    ? request.PreferredBiomes
                    : (IReadOnlyList<BiomeId>)profile.PreferredBiomes;

                if (!placedByKind.TryGetValue(request.Kind, out var sameKind))
                {
                    sameKind = new List<Vector3>();
                    placedByKind[request.Kind] = sameKind;
                }

                var resolvedSector = ResolveSector(request, sectors, analysis, townOccupied);
                var sectorAnchor = resolvedSector.HasValue
                    ? sectors.SectorCenter(resolvedSector.Value.x, resolvedSector.Value.y)
                    : (Vector2?)null;

                var bestScore = float.NegativeInfinity;
                var bestList = new List<Candidate>();

                for (var i = 0; i < candidates.Count; i++)
                {
                    var c = candidates[i];
                    if (!PassesHard(c, profile, sameKind, townCenters, analysis, preferred))
                    {
                        continue;
                    }

                    var score = Score(
                        c, profile, preferred, sameKind, townCenters, analysis, poiRng,
                        sectors, resolvedSector, sectorAnchor, request.IsMainHub);
                    if (score > bestScore + 1e-4f)
                    {
                        bestScore = score;
                        bestList.Clear();
                        bestList.Add(c);
                    }
                    else if (Mathf.Abs(score - bestScore) <= 1e-4f)
                    {
                        bestList.Add(c);
                    }
                }

                if (bestList.Count == 0)
                {
                    for (var i = 0; i < candidates.Count; i++)
                    {
                        var c = candidates[i];
                        if (c.Height01 < profile.MinHeight01)
                        {
                            continue;
                        }

                        if (analysis != null &&
                            analysis.SampleDistToLake(c.Center.x, c.Center.z) >
                            -profile.MinDistanceToLakeMeters)
                        {
                            continue;
                        }

                        var score = Score(
                            c, profile, preferred, sameKind, townCenters, analysis, poiRng,
                            sectors, resolvedSector, sectorAnchor, request.IsMainHub);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestList.Clear();
                            bestList.Add(c);
                        }
                    }
                }

                if (bestList.Count == 0)
                {
                    continue;
                }

                Candidate picked;
                if (sectorAnchor.HasValue && bestList.Count > 1)
                {
                    picked = bestList[0];
                    var bestDist = float.PositiveInfinity;
                    for (var i = 0; i < bestList.Count; i++)
                    {
                        var dx = bestList[i].Center.x - sectorAnchor.Value.x;
                        var dz = bestList[i].Center.z - sectorAnchor.Value.y;
                        var d = dx * dx + dz * dz;
                        if (d < bestDist)
                        {
                            bestDist = d;
                            picked = bestList[i];
                        }
                    }
                }
                else
                {
                    picked = bestList[poiRng.Next(bestList.Count)];
                }

                var position = RefineInsideChunk(
                    picked, profile, analysis, biomeSampler, preferred, sameKind, townCenters, poiRng,
                    sectors, resolvedSector, sectorAnchor);

                candidates.RemoveAll(c => c.Chunk.Equals(picked.Chunk));

                var id = new PoiId($"{request.Kind}_{index}");
                var biome = biomeSampler.SampleBiome(position.x, position.z);
                markersByChunk[picked.Chunk] = new PoiMarker(id, request.Kind, position, biome);
                sameKind.Add(position);
                if (request.Kind == PoiKind.TownApproach)
                {
                    townCenters.Add(position);
                    if (resolvedSector.HasValue)
                    {
                        townOccupied[resolvedSector.Value.x, resolvedSector.Value.y] = true;
                    }
                    else
                    {
                        sectors.WorldToSector(position.x, position.z, out var sx, out var sz);
                        townOccupied[sx, sz] = true;
                    }
                }
            }
        }

        private static Vector2Int? ResolveSector(
            PoiRequest request,
            WorldSectorMap sectors,
            LandscapeCostField analysis,
            bool[,] townOccupied)
        {
            if (!request.HasPreferredSector)
            {
                return null;
            }

            var occupied = request.Kind == PoiKind.TownApproach ? townOccupied : null;
            return sectors.ResolveHabitableSector(
                request.PreferredSectorX,
                request.PreferredSectorZ,
                analysis,
                occupied);
        }

        private static Vector3 RefineInsideChunk(
            Candidate chunkCandidate,
            PoiPlacementProfile profile,
            LandscapeCostField analysis,
            IBiomeSampler biomeSampler,
            IReadOnlyList<BiomeId> preferred,
            List<Vector3> sameKind,
            List<Vector3> townCenters,
            System.Random poiRng,
            WorldSectorMap sectors,
            Vector2Int? resolvedSector,
            Vector2? sectorAnchor)
        {
            if (analysis == null || profile.FootprintRadiusMeters <= 0f)
            {
                return chunkCandidate.Center;
            }

            var half = WorldScale.ChunkSizeMeters * 0.5f;
            var step = Mathf.Max(analysis.CellSize, 16f);
            var bestScore = float.NegativeInfinity;
            var best = chunkCandidate.Center;
            var cx = chunkCandidate.Center.x;
            var cz = chunkCandidate.Center.z;

            for (var z = cz - half + step * 0.5f; z < cz + half; z += step)
            {
                for (var x = cx - half + step * 0.5f; x < cx + half; x += step)
                {
                    var c = new Candidate(
                        chunkCandidate.Chunk,
                        new Vector3(x, 0f, z),
                        biomeSampler.SampleBiome(x, z),
                        biomeSampler.SampleHeight(x, z));
                    if (!PassesHard(c, profile, sameKind, townCenters, analysis, preferred))
                    {
                        continue;
                    }

                    var score = Score(
                        c, profile, preferred, sameKind, townCenters, analysis, poiRng,
                        sectors, resolvedSector, sectorAnchor, isMainHub: false);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = c.Center;
                    }
                }
            }

            return best;
        }

        private static bool PassesHard(
            Candidate c,
            PoiPlacementProfile profile,
            List<Vector3> sameKind,
            List<Vector3> townCenters,
            LandscapeCostField analysis,
            IReadOnlyList<BiomeId> preferred)
        {
            if (c.Height01 < profile.MinHeight01)
            {
                return false;
            }

            if (profile.RejectMountainBiome && c.Biome == BiomeId.Mountain)
            {
                return false;
            }

            if (analysis != null)
            {
                var lake = analysis.SampleDistToLake(c.Center.x, c.Center.z);
                if (lake > -profile.MinDistanceToLakeMeters)
                {
                    return false;
                }

                var slope = analysis.SampleSlope01(c.Center.x, c.Center.z);
                if (slope > profile.MaxSlope01)
                {
                    return false;
                }

                if (profile.FootprintRadiusMeters > 0f)
                {
                    var fp = analysis.SampleFootprint(
                        c.Center.x, c.Center.z, profile.FootprintRadiusMeters);
                    if (fp.SampleCount > 0 && fp.MaxSlope01 > profile.MaxSlope01 + 0.05f)
                    {
                        return false;
                    }
                }
            }

            if (profile.MinDistanceToSameKindMeters > 0f && sameKind.Count > 0)
            {
                if (MinDistance(c.Center, sameKind) < profile.MinDistanceToSameKindMeters)
                {
                    return false;
                }
            }

            if (profile.MinDistanceToTownMeters > 0f && townCenters.Count > 0)
            {
                if (MinDistance(c.Center, townCenters) < profile.MinDistanceToTownMeters)
                {
                    return false;
                }
            }

            _ = preferred;
            return true;
        }

        private static float Score(
            Candidate c,
            PoiPlacementProfile profile,
            IReadOnlyList<BiomeId> preferred,
            List<Vector3> sameKind,
            List<Vector3> townCenters,
            LandscapeCostField analysis,
            System.Random poiRng,
            WorldSectorMap sectors,
            Vector2Int? resolvedSector,
            Vector2? sectorAnchor,
            bool isMainHub)
        {
            // Tiny deterministic jitter from this POI's stream + chunk — not sequential over
            // all candidates (that would cascade when a new kind is added).
            var score = TieBreakJitter(poiRng, c.Chunk);

            if (preferred != null && preferred.Count > 0 && preferred.Contains(c.Biome))
            {
                score += profile.PreferBiomeWeight;
            }

            if (sameKind.Count > 0 && profile.PreferSpreadWeight > 0f)
            {
                var d = MinDistance(c.Center, sameKind);
                score += profile.PreferSpreadWeight * Mathf.Clamp01(d / 400f);
            }

            // Sector-first: stay in the resolved sector, then closest to its anchor.
            if (resolvedSector.HasValue)
            {
                if (sectors.Contains(resolvedSector.Value.x, resolvedSector.Value.y, c.Center.x, c.Center.z))
                {
                    score += 28f;
                    if (isMainHub)
                    {
                        score += 6f;
                    }
                }
                else
                {
                    score -= 12f;
                }
            }

            if (sectorAnchor.HasValue)
            {
                var dx = c.Center.x - sectorAnchor.Value.x;
                var dz = c.Center.z - sectorAnchor.Value.y;
                var dist = Mathf.Sqrt(dx * dx + dz * dz);
                var diagonal = Mathf.Sqrt(
                    sectors.SectorSizeX * sectors.SectorSizeX +
                    sectors.SectorSizeZ * sectors.SectorSizeZ);
                score += 10f * (1f - Mathf.Clamp01(dist / Mathf.Max(diagonal, 1f)));
            }

            if (analysis == null)
            {
                return score;
            }

            var slope = analysis.SampleSlope01(c.Center.x, c.Center.z);
            var height = analysis.SampleHeight(c.Center.x, c.Center.z);
            var curv = analysis.SampleCurvature(c.Center.x, c.Center.z);
            FootprintStats fp = default;
            if (profile.FootprintRadiusMeters > 0f)
            {
                fp = analysis.SampleFootprint(c.Center.x, c.Center.z, profile.FootprintRadiusMeters);
            }

            if (profile.PreferFlatWeight > 0f)
            {
                var flat = fp.SampleCount > 0 ? fp.FlatAreaRatio : 1f - slope;
                score += profile.PreferFlatWeight * flat;
                score -= profile.PreferFlatWeight * slope;
            }

            if (profile.PreferLowVarianceWeight > 0f && fp.SampleCount > 0)
            {
                score += profile.PreferLowVarianceWeight *
                    (1f - Mathf.Clamp01(fp.HeightVariance / 40f));
            }

            if (profile.PreferOpenWeight > 0f && fp.SampleCount > 0)
            {
                score += profile.PreferOpenWeight * fp.MeadowRatio;
            }

            if (profile.PreferElevatedWeight > 0f)
            {
                score += profile.PreferElevatedWeight *
                    Mathf.Clamp01(height / WorldScale.TerrainHeightBudgetMeters);
            }

            if (profile.PreferIsolationWeight > 0f)
            {
                var nearTown = townCenters.Count > 0 ? MinDistance(c.Center, townCenters) : 400f;
                score += profile.PreferIsolationWeight * Mathf.Clamp01(nearTown / 300f);
            }

            if (profile.PreferForestEdgeWeight > 0f && fp.SampleCount > 0)
            {
                var edge = 1f - Mathf.Abs(fp.ForestRatio - 0.45f) * 2f;
                score += profile.PreferForestEdgeWeight * Mathf.Clamp01(edge);
            }

            if (profile.PreferScenicWeight > 0f)
            {
                score += profile.PreferScenicWeight * (curv + Mathf.Clamp01(height / 150f) * 0.5f);
            }

            return score;
        }

        /// <summary>
        /// Hash-like jitter in [0, 0.05) from chunk coords. Does not advance the POI stream,
        /// so scoring many candidates of one marker cannot cascade into later markers.
        /// Per-marker stream still isolates pick/refine RNG across kinds/indices.
        /// </summary>
        private static float TieBreakJitter(System.Random poiRng, ChunkCoord chunk)
        {
            _ = poiRng;
            unchecked
            {
                var h = (chunk.X * 73856093) ^ (chunk.Z * 19349663);
                if (h < 0)
                {
                    h = ~h;
                }

                return (h % 1000) / 1000f * 0.05f;
            }
        }

        private static float MinDistance(Vector3 point, List<Vector3> others)
        {
            var best = float.PositiveInfinity;
            for (var i = 0; i < others.Count; i++)
            {
                var dx = point.x - others[i].x;
                var dz = point.z - others[i].z;
                var d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d < best)
                {
                    best = d;
                }
            }

            return best;
        }

        private readonly struct Candidate
        {
            public ChunkCoord Chunk { get; }
            public Vector3 Center { get; }
            public BiomeId Biome { get; }
            public float Height01 { get; }

            public Candidate(ChunkCoord chunk, Vector3 center, BiomeId biome, float height01)
            {
                Chunk = chunk;
                Center = center;
                Biome = biome;
                Height01 = height01;
            }
        }
    }
}
