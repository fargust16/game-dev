using System.Collections.Generic;
using GameDev.World.Roads;
using GameDev.World.Terrain;
using GameDev.World.Water;
using UnityEngine;

namespace GameDev.World.Vegetation
{
    /// <summary>
    /// Grass coverage from ecology + clearance. Forests are patchy understory (not a carpet);
    /// roads/settlements/water keep soft falloffs. Tree trunks are stamped during chunk bake.
    /// </summary>
    public static class GrassMask
    {
        public const float MinDensity = 0.05f;
        public const float ShoreFadeMeters = 3f;

        /// <summary>Bare packed road bed (no grass).</summary>
        public const float RoadClearCoreMeters = 2.4f;

        /// <summary>
        /// Soft shoulder width (memo: 1.5–3.0 m). Kept in sync with road splat blend.
        /// </summary>
        public const float RoadEdgeFalloffMeters = RoadSurfaceStyle.TransitionMeters;

        /// <summary>Bare ground under trunk (memo: 0–0.5 m).</summary>
        public const float TreeClearCoreMeters = 0.5f;

        /// <summary>Low→full grass around trunk (memo: full by ~2 m → falloff 1.5 m).</summary>
        public const float TreeFalloffMeters = 1.5f;

        /// <summary>Forest understory target multiplier at full canopy (memo ~0.3–0.6).</summary>
        public const float ForestCanopyDensityMul = 0.42f;

        /// <summary>Extra patch noise scale in forests (metres wavelength ≈ 1/freq).</summary>
        public const float ForestPatchFrequency = 0.28f;

        /// <summary>0..1 how strongly forest patch noise punches gaps.</summary>
        public const float ForestPatchStrength = 0.55f;

        /// <summary>0..1 how strongly edge shortens blades (shader + bake).</summary>
        public const float HeightReduceIntensity = 0.9f;

        /// <summary>0..1 how strongly edge thins blades (shader dither + bake chance).</summary>
        public const float DensityReduceIntensity = 0.85f;

        /// <summary>0..1 how strongly edge flattens/tilts blades toward the ground.</summary>
        public const float EdgeTiltStrength = 0.7f;

        /// <summary>0..1 dry packed-earth tint on short edge blades.</summary>
        public const float DryEdgeStrength = 0.9f;

        /// <summary>Extra height variation (± fraction) when placing clumps.</summary>
        public const float HeightVariation = 0.35f;

        public static float Sample(
            float worldX,
            float worldZ,
            float groundY,
            VegetationEcology.Sample ecology,
            float clearanceMul,
            IWaterSampler water,
            GrassExclusion settlement,
            float shoreBufferMeters = 1.5f,
            VegetationEcology.NoiseOffsets offsets = default)
        {
            if (clearanceMul <= 0.01f || ecology.Grass <= MinDensity)
            {
                return 0f;
            }

            if (WaterProximity.IsUnderOrTooCloseToWater(water, worldX, worldZ, groundY, shoreBufferMeters))
            {
                return 0f;
            }

            var settlementMul = settlement != null ? settlement.Multiplier(worldX, worldZ) : 1f;
            if (settlementMul <= 0.01f)
            {
                return 0f;
            }

            var waterFactor = WaterShoreFactor(water, worldX, worldZ, ShoreFadeMeters);
            if (waterFactor <= 0.01f)
            {
                return 0f;
            }

            // Drive coverage from ecology grass (already slope-aware), not a flat carpet.
            var density = ecology.Grass * clearanceMul * settlementMul * waterFactor;

            // Dense canopy → understory target ~0.3–0.6 of open meadow.
            var forest = Mathf.Clamp01(ecology.Forest);
            density *= Mathf.Lerp(1f, ForestCanopyDensityMul, forest);

            // Extra patchiness under trees (breaks the green sheet).
            if (forest > 0.05f)
            {
                density *= ForestPatchFactor(worldX, worldZ, offsets, forest);
            }

            return density < MinDensity ? 0f : Mathf.Clamp01(density);
        }

        public static float Evaluate(
            float worldX,
            float worldZ,
            float groundY,
            BiomeId biome,
            float slope01,
            float moisture01,
            VegetationEcology.NoiseOffsets offsets,
            VegetationClearance clearance,
            IWaterSampler water,
            GrassExclusion settlement,
            float shoreBufferMeters = 1.5f)
        {
            var clearMul = clearance != null ? clearance.Multiplier(worldX, worldZ) : 1f;
            if (clearMul <= 0.01f)
            {
                return 0f;
            }

            var wetness = WaterProximity.SampleWetness01(
                water, worldX, worldZ, VegetationEcology.WetBoostMeters);
            var ecology = VegetationEcology.Evaluate(
                worldX, worldZ, biome, slope01, moisture01, wetness, offsets);
            return Sample(
                worldX, worldZ, groundY, ecology, clearMul, water, settlement,
                shoreBufferMeters, offsets);
        }

        public static float WaterShoreFactor(IWaterSampler water, float worldX, float worldZ, float fadeMeters)
        {
            if (water == null || fadeMeters <= 0f)
            {
                return 1f;
            }

            var wet = WaterProximity.SampleWetness01(water, worldX, worldZ, fadeMeters);
            return 1f - wet;
        }

        /// <summary>
        /// Forest patch mask: 1 = keep, lower = gap. Strength scales with canopy.
        /// </summary>
        public static float ForestPatchFactor(
            float worldX,
            float worldZ,
            VegetationEcology.NoiseOffsets offsets,
            float forest01)
        {
            var n = Mathf.PerlinNoise(
                (worldX + offsets.GrassX) * ForestPatchFrequency,
                (worldZ + offsets.GrassZ) * ForestPatchFrequency);
            n = n * n * (3f - 2f * n);
            var strength = ForestPatchStrength * Mathf.Clamp01(forest01);
            // Keep a floor so understory never goes fully blank from noise alone.
            return Mathf.Lerp(1f, Mathf.Lerp(0.15f, 1f, n), strength);
        }

        /// <summary>Stamp trunk exclusion into a chunk mask (min with existing values).</summary>
        public static void StampTreeInfluence(
            float[,] mask,
            int maskRes,
            Vector3 origin,
            float chunkSize,
            float sampleMeters,
            IReadOnlyList<Vector2> treeTrunks)
        {
            if (mask == null || treeTrunks == null || treeTrunks.Count == 0)
            {
                return;
            }

            var core = TreeClearCoreMeters;
            var falloff = TreeFalloffMeters;
            var stampR = core + falloff;
            var stampRSq = stampR * stampR;

            for (var i = 0; i < treeTrunks.Count; i++)
            {
                var t = treeTrunks[i];
                if (t.x < origin.x - stampR || t.x > origin.x + chunkSize + stampR ||
                    t.y < origin.z - stampR || t.y > origin.z + chunkSize + stampR)
                {
                    continue;
                }

                var minMx = Mathf.Max(0, Mathf.FloorToInt((t.x - stampR - origin.x) / sampleMeters));
                var maxMx = Mathf.Min(maskRes - 1, Mathf.CeilToInt((t.x + stampR - origin.x) / sampleMeters));
                var minMz = Mathf.Max(0, Mathf.FloorToInt((t.y - stampR - origin.z) / sampleMeters));
                var maxMz = Mathf.Min(maskRes - 1, Mathf.CeilToInt((t.y + stampR - origin.z) / sampleMeters));

                for (var mz = minMz; mz <= maxMz; mz++)
                {
                    var z = origin.z + mz * sampleMeters;
                    for (var mx = minMx; mx <= maxMx; mx++)
                    {
                        var x = origin.x + mx * sampleMeters;
                        var dx = x - t.x;
                        var dz = z - t.y;
                        var dSq = dx * dx + dz * dz;
                        if (dSq > stampRSq)
                        {
                            continue;
                        }

                        var dist = Mathf.Sqrt(dSq);
                        var mul = VegetationClearance.FalloffMultiplier(dist, core, falloff);
                        if (mul < mask[mx, mz])
                        {
                            mask[mx, mz] = mul;
                        }
                    }
                }
            }
        }

        /// <summary>Height multiplier for soft edges (short grass near roads).</summary>
        public static float EdgeHeightFactor(float density01)
        {
            var d = Mathf.Clamp01(density01);
            var s = d * d * (3f - 2f * d);
            return Mathf.Lerp(1f - HeightReduceIntensity, 1f, s);
        }

        /// <summary>Placement probability in the soft band (avoids hard isosurface walls).</summary>
        public static float EdgePlaceChance(float density01)
        {
            var d = Mathf.Clamp01(density01);
            var s = d * d * (3f - 2f * d);
            var minKeep = 1f - DensityReduceIntensity * 0.55f;
            return Mathf.Lerp(Mathf.Max(0.2f, minKeep), 1f, s);
        }
    }
}
