using System.Collections.Generic;
using GameDev.World.Streaming;
using GameDev.World.Terrain;
using GameDev.World.Water;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Vegetation
{
    /// <summary>
    /// Seeded grid+jitter grass bake. Open land stays denser; forests use ecology patches
    /// plus trunk stamps so coverage is not a uniform carpet.
    /// Fork stream id <c>"grass"</c> — independent of tree/shrub <c>"vegetation"</c>.
    /// </summary>
    public static class GrassDistribution
    {
        /// <summary>
        /// Instance spacing. PT_Grass_02 clump is ~1.2 m wide — overlap still reads as a lawn
        /// in open meadows; forest density is driven by the mask, not cell size alone.
        /// </summary>
        public const float CellMeters = 0.4f;

        /// <summary>Mask sample spacing (metres). Bilinear-upsampled to <see cref="CellMeters"/>.</summary>
        public const float MaskSampleMeters = 1.5f;

        public const float ScaleMin = 0.85f;
        public const float ScaleMax = 1.4f;

        /// <summary>Reference mesh height of PT_Grass_02 at scale 1.</summary>
        public const float MeshRefHeightMeters = 0.66f;

        /// <summary>Only skip near-zero mask; soft edges use probability + height falloff.</summary>
        public const float PlaceThreshold = 0.08f;

        public static GrassChunk BakeChunk(
            int chunkX,
            int chunkZ,
            Random rng,
            MapMagicBiomeSampler sampler,
            UnityEngine.Terrain terrain,
            VegetationEcology.NoiseOffsets offsets,
            VegetationClearance clearance,
            IWaterSampler water,
            GrassExclusion settlement,
            float minScaleMeters,
            float maxScaleMeters,
            IReadOnlyList<Vector2> treeTrunks = null)
        {
            var origin = new Vector3(chunkX * ChunkCoord.ChunkSize, 0f, chunkZ * ChunkCoord.ChunkSize);
            var size = ChunkCoord.ChunkSize;
            var cells = Mathf.FloorToInt(size / CellMeters);
            if (cells < 1)
            {
                return GrassChunk.Empty(chunkX, chunkZ, origin);
            }

            var maskRes = Mathf.Max(2, Mathf.CeilToInt(size / MaskSampleMeters) + 1);
            var mask = new float[maskRes, maskRes];
            for (var mz = 0; mz < maskRes; mz++)
            {
                for (var mx = 0; mx < maskRes; mx++)
                {
                    var x = origin.x + mx * MaskSampleMeters;
                    var z = origin.z + mz * MaskSampleMeters;
                    if (x > origin.x + size)
                    {
                        x = origin.x + size;
                    }

                    if (z > origin.z + size)
                    {
                        z = origin.z + size;
                    }

                    if (!TrySampleHeight(terrain, x, z, out var groundY))
                    {
                        mask[mx, mz] = 0f;
                        continue;
                    }

                    var biome = sampler.SampleBiome(x, z);
                    var slope = sampler.SampleSlope01(x, z);
                    var moisture = sampler.SampleMoisture01(x, z);
                    mask[mx, mz] = GrassMask.Evaluate(
                        x, z, groundY, biome, slope, moisture, offsets,
                        clearance, water, settlement);
                }
            }

            GrassMask.StampTreeInfluence(
                mask, maskRes, origin, size, MaskSampleMeters, treeTrunks);

            var list = new List<Matrix4x4>(cells * cells / 2);
            var terrainData = terrain != null ? terrain.terrainData : null;
            var terrainPos = terrain != null ? terrain.transform.position : Vector3.zero;

            for (var iz = 0; iz < cells; iz++)
            {
                for (var ix = 0; ix < cells; ix++)
                {
                    var jx = ((float)rng.NextDouble() - 0.5f) * CellMeters * 0.45f;
                    var jz = ((float)rng.NextDouble() - 0.5f) * CellMeters * 0.45f;
                    var x = origin.x + (ix + 0.5f) * CellMeters + jx;
                    var z = origin.z + (iz + 0.5f) * CellMeters + jz;

                    var density = SampleMaskBilinear(mask, maskRes, origin, size, x, z);
                    if (density < PlaceThreshold)
                    {
                        continue;
                    }

                    // Soft shoulders + forest gaps: stochastic thin by density.
                    if (rng.NextDouble() > GrassMask.EdgePlaceChance(density))
                    {
                        continue;
                    }

                    if (!TrySampleHeight(terrain, x, z, out var groundY))
                    {
                        continue;
                    }

                    var yaw = (float)rng.NextDouble() * 360f;
                    var t = (float)rng.NextDouble();
                    var edgeH = GrassMask.EdgeHeightFactor(density);
                    // Density also shortens understory (low mask → short grass).
                    var understoryH = Mathf.Lerp(0.35f, 1f, density);
                    var heightVar = Mathf.Lerp(1f - GrassMask.HeightVariation, 1f + GrassMask.HeightVariation, t);
                    var heightM = Mathf.Lerp(minScaleMeters, maxScaleMeters, 0.25f + t * 0.75f)
                                  * edgeH * understoryH * heightVar;
                    heightM = Mathf.Max(heightM, minScaleMeters * 0.25f * Mathf.Max(edgeH, 0.15f));
                    var scale = Mathf.Lerp(ScaleMin, ScaleMax, t) * Mathf.Lerp(0.65f, 1f, edgeH);
                    var uniform = (heightM / MeshRefHeightMeters) * scale;

                    var pos = new Vector3(x, groundY, z);
                    var rot = Quaternion.Euler(0f, yaw, 0f);
                    if (terrainData != null)
                    {
                        var nx = (x - terrainPos.x) / terrainData.size.x;
                        var nz = (z - terrainPos.z) / terrainData.size.z;
                        if (nx >= 0f && nz >= 0f && nx <= 1f && nz <= 1f)
                        {
                            var normal = terrainData.GetInterpolatedNormal(nx, nz);
                            if (normal.sqrMagnitude > 1e-6f)
                            {
                                rot = Quaternion.FromToRotation(Vector3.up, normal.normalized) * rot;
                            }
                        }
                    }

                    list.Add(Matrix4x4.TRS(pos, rot, Vector3.one * uniform));
                }
            }

            return new GrassChunk(chunkX, chunkZ, origin, list.ToArray(), list.Count);
        }

        public static List<GrassChunk> BakeTile(
            float minX,
            float minZ,
            float sizeX,
            float sizeZ,
            Random rng,
            MapMagicBiomeSampler sampler,
            UnityEngine.Terrain terrain,
            VegetationEcology.NoiseOffsets offsets,
            VegetationClearance clearance,
            IWaterSampler water,
            GrassExclusion settlement,
            float minScaleMeters,
            float maxScaleMeters,
            IReadOnlyList<Vector2> treeTrunks = null)
        {
            var minChunkX = Mathf.FloorToInt(minX / ChunkCoord.ChunkSize);
            var maxChunkX = Mathf.FloorToInt((minX + sizeX - 0.01f) / ChunkCoord.ChunkSize);
            var minChunkZ = Mathf.FloorToInt(minZ / ChunkCoord.ChunkSize);
            var maxChunkZ = Mathf.FloorToInt((minZ + sizeZ - 0.01f) / ChunkCoord.ChunkSize);

            var chunks = new List<GrassChunk>(
                (maxChunkX - minChunkX + 1) * (maxChunkZ - minChunkZ + 1));

            for (var cx = minChunkX; cx <= maxChunkX; cx++)
            {
                for (var cz = minChunkZ; cz <= maxChunkZ; cz++)
                {
                    var salt = rng.Next();
                    var chunkRng = new Random(unchecked(salt ^ (cx * 73856093) ^ (cz * 19349663)));
                    chunks.Add(BakeChunk(
                        cx, cz, chunkRng, sampler, terrain, offsets,
                        clearance, water, settlement, minScaleMeters, maxScaleMeters, treeTrunks));
                }
            }

            return chunks;
        }

        public static float SampleMaskBilinear(
            float[,] mask,
            int maskRes,
            Vector3 origin,
            float size,
            float worldX,
            float worldZ)
        {
            var u = (worldX - origin.x) / MaskSampleMeters;
            var v = (worldZ - origin.z) / MaskSampleMeters;
            var x0 = Mathf.FloorToInt(u);
            var z0 = Mathf.FloorToInt(v);
            var tx = u - x0;
            var tz = v - z0;
            tx = tx * tx * (3f - 2f * tx);
            tz = tz * tz * (3f - 2f * tz);

            var a = At(mask, maskRes, x0, z0);
            var b = At(mask, maskRes, x0 + 1, z0);
            var c = At(mask, maskRes, x0, z0 + 1);
            var d = At(mask, maskRes, x0 + 1, z0 + 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), tz);
        }

        private static float At(float[,] mask, int res, int x, int z)
        {
            if (x < 0 || z < 0 || x >= res || z >= res)
            {
                return 0f;
            }

            return mask[x, z];
        }

        private static bool TrySampleHeight(UnityEngine.Terrain terrain, float x, float z, out float y)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                y = 0f;
                return false;
            }

            var pos = terrain.transform.position;
            var size = terrain.terrainData.size;
            if (x < pos.x || z < pos.z || x > pos.x + size.x || z > pos.z + size.z)
            {
                y = 0f;
                return false;
            }

            y = terrain.SampleHeight(new Vector3(x, 0f, z)) + pos.y;
            return true;
        }
    }
}
