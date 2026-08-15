using GameDev.World.Terrain;
using UnityEngine;

namespace GameDev.World.Vegetation
{
    /// <summary>
    /// Multi-scale ecological density fields for overworld flora (Iteration 07 refactor).
    /// Macro noise decides where forests/meadows exist; meso noise punches clearings and
    /// thickets; micro jitter stays in the scatter loop. Layers are correlated but not identical:
    /// forest interiors suppress grass, edges boost shrubs, wetness favours shrubs/grass over trees.
    /// </summary>
    public static class VegetationEcology
    {
        // ~350m features — large forest masses (prompt: not small sparse islands).
        public const float MacroFrequency = 0.0028f;

        // ~60m clearings and local openings inside forest regions.
        public const float MesoFrequency = 0.016f;

        // ~28m shrub thicket blobs.
        public const float ThicketFrequency = 0.036f;

        // ~50m grass coverage patches (thick / medium / sparse / bare).
        public const float GrassFrequency = 0.02f;

        public const float WetBoostMeters = 45f;

        public readonly struct Sample
        {
            public readonly float Forest;
            public readonly float Shrub;
            public readonly float Grass;
            public readonly float Edge;
            public readonly float Thicket;

            public Sample(float forest, float shrub, float grass, float edge, float thicket)
            {
                Forest = forest;
                Shrub = shrub;
                Grass = grass;
                Edge = edge;
                Thicket = thicket;
            }
        }

        public readonly struct NoiseOffsets
        {
            public readonly float MacroX;
            public readonly float MacroZ;
            public readonly float MesoX;
            public readonly float MesoZ;
            public readonly float ThicketX;
            public readonly float ThicketZ;
            public readonly float GrassX;
            public readonly float GrassZ;

            public NoiseOffsets(
                float macroX, float macroZ,
                float mesoX, float mesoZ,
                float thicketX, float thicketZ,
                float grassX, float grassZ)
            {
                MacroX = macroX;
                MacroZ = macroZ;
                MesoX = mesoX;
                MesoZ = mesoZ;
                ThicketX = thicketX;
                ThicketZ = thicketZ;
                GrassX = grassX;
                GrassZ = grassZ;
            }

            public static NoiseOffsets FromRng(System.Random rng)
            {
                return new NoiseOffsets(
                    (float)rng.NextDouble() * 10000f,
                    (float)rng.NextDouble() * 10000f,
                    (float)rng.NextDouble() * 10000f,
                    (float)rng.NextDouble() * 10000f,
                    (float)rng.NextDouble() * 10000f,
                    (float)rng.NextDouble() * 10000f,
                    (float)rng.NextDouble() * 10000f,
                    (float)rng.NextDouble() * 10000f);
            }
        }

        public static Sample Evaluate(
            float worldX,
            float worldZ,
            BiomeId biome,
            float slope01,
            float moisture01,
            float wetness01,
            NoiseOffsets offsets)
        {
            GetBiomeWeights(biome, moisture01, out var forestBias, out var shrubBias, out var grassBias);
            return EvaluateWithBiases(
                worldX, worldZ, slope01, moisture01, wetness01, offsets,
                forestBias, shrubBias, grassBias);
        }

        /// <summary>
        /// Soft biome blend from continuous height/slope/moisture so grass density/hue drivers
        /// do not jump when hard <see cref="BiomeId"/> flips at a band edge.
        /// </summary>
        public static Sample EvaluateSoft(
            float worldX,
            float worldZ,
            float height01,
            float slope01,
            float moisture01,
            float wetness01,
            NoiseOffsets offsets)
        {
            SoftBiomeBiases(height01, slope01, moisture01, out var forestBias, out var shrubBias, out var grassBias);
            return EvaluateWithBiases(
                worldX, worldZ, slope01, moisture01, wetness01, offsets,
                forestBias, shrubBias, grassBias);
        }

        private static Sample EvaluateWithBiases(
            float worldX,
            float worldZ,
            float slope01,
            float moisture01,
            float wetness01,
            NoiseOffsets offsets,
            float forestBias,
            float shrubBias,
            float grassBias)
        {
            var slopeMul = VegetationDensity.SlopeMultiplier(slope01);
            if (slopeMul <= 0f)
            {
                return new Sample(0f, 0f, 0f, 0f, 0f);
            }

            var macro = Smooth01(Perlin(worldX, worldZ, offsets.MacroX, offsets.MacroZ, MacroFrequency));
            var meso = Smooth01(Perlin(worldX, worldZ, offsets.MesoX, offsets.MesoZ, MesoFrequency));
            var thicketNoise = Smooth01(Perlin(worldX, worldZ, offsets.ThicketX, offsets.ThicketZ, ThicketFrequency));
            var grassPatch = Smooth01(Perlin(worldX, worldZ, offsets.GrassX, offsets.GrassZ, GrassFrequency));

            // Clearings are coherent meso holes — smaller/rarer so dense interiors stay massifs.
            var clearing = Mathf.Clamp01((meso - 0.68f) / 0.32f);
            clearing = clearing * clearing * (3f - 2f * clearing);

            // Macro field decides whether a forest region exists; biome bias gates how strongly.
            var forestRegion = SoftThreshold(macro, forestBias);
            var forest = forestRegion * (1f - clearing * 0.78f) * slopeMul;
            // Push interiors toward dense canopy once a forest region exists.
            forest = Mathf.Clamp01(forest * 1.15f);
            forest = RemapForestStates(forest);

            // Edge peaks in the mid-density band (ecotone), not in barren or solid canopy.
            var edge = 4f * forest * (1f - forest);
            edge = Mathf.Clamp01(edge);

            var thicket = Mathf.Clamp01((thicketNoise - 0.62f) / 0.38f);
            thicket = thicket * thicket * (3f - 2f * thicket);
            // Thickets prefer edges, clearings, and wet pockets — not identical to tree density.
            var thicketGate = Mathf.Clamp01(0.25f + edge * 0.55f + clearing * 0.45f + wetness01 * 0.35f);
            thicket *= thicketGate * shrubBias * slopeMul;

            var shrub = shrubBias * slopeMul;
            shrub *= 0.35f + edge * 0.75f + clearing * forestRegion * 0.55f + wetness01 * 0.4f;
            shrub *= 0.55f + thicketNoise * 0.7f;
            shrub = Mathf.Clamp01(shrub + thicket * 0.55f);
            // Dense canopy still allows understory, but not as a 1:1 copy of trees.
            shrub *= Mathf.Lerp(1f, 0.7f, Mathf.Clamp01((forest - 0.55f) / 0.45f));

            // Grass: open meadows stay lush; forests become patchy understory (not a carpet).
            var grass = grassBias * slopeMul;
            grass *= 0.35f + grassPatch * 0.65f;
            grass *= 1f - forest * 0.58f;
            grass *= 0.85f + moisture01 * 0.25f + wetness01 * 0.2f;
            grass *= 1f - thicket * 0.28f;
            var grassFloor = grassBias * slopeMul * Mathf.Lerp(0.28f, 0.08f, forest);
            grass = Mathf.Clamp01(Mathf.Max(grass, grassFloor));

            // Near water: soft wet meadow, not a hard decorative ring of trees.
            forest *= 1f - wetness01 * 0.55f;

            return new Sample(
                Mathf.Clamp01(forest),
                Mathf.Clamp01(shrub),
                Mathf.Clamp01(grass),
                edge,
                Mathf.Clamp01(thicket));
        }

        /// <summary>
        /// Configurable forest density states so large regions can sit in woodland vs dense canopy
        /// instead of a flat mid-grey field.
        /// </summary>
        public static float RemapForestStates(float raw)
        {
            var t = Mathf.Clamp01(raw);
            if (t < 0.08f)
            {
                return t * 0.4f; // barren / isolated
            }

            if (t < 0.22f)
            {
                return Mathf.Lerp(0.05f, 0.15f, (t - 0.08f) / 0.14f);
            }

            if (t < 0.4f)
            {
                return Mathf.Lerp(0.15f, 0.35f, (t - 0.22f) / 0.18f);
            }

            if (t < 0.62f)
            {
                return Mathf.Lerp(0.35f, 0.55f, (t - 0.4f) / 0.22f);
            }

            if (t < 0.8f)
            {
                return Mathf.Lerp(0.55f, 0.75f, (t - 0.62f) / 0.18f);
            }

            return Mathf.Lerp(0.75f, 0.95f, (t - 0.8f) / 0.2f);
        }

        private static void GetBiomeWeights(
            BiomeId biome, float moisture01, out float forestBias, out float shrubBias, out float grassBias)
        {
            switch (biome)
            {
                case BiomeId.Forest:
                    forestBias = 0.97f;
                    shrubBias = 0.8f;
                    grassBias = 0.75f;
                    break;
                case BiomeId.Meadow:
                    forestBias = 0.14f;
                    shrubBias = 0.5f;
                    grassBias = 1f;
                    break;
                case BiomeId.Swamp:
                    forestBias = 0.32f;
                    shrubBias = 0.9f;
                    grassBias = 0.85f;
                    break;
                case BiomeId.Highland:
                    forestBias = 0.4f;
                    shrubBias = 0.6f;
                    grassBias = 0.55f;
                    break;
                case BiomeId.Mountain:
                    forestBias = 0.04f;
                    shrubBias = 0.08f;
                    grassBias = 0.12f;
                    break;
                default:
                    forestBias = 0.2f;
                    shrubBias = 0.4f;
                    grassBias = 0.7f;
                    break;
            }

            // Moisture gently nudges green biomes wetter without inventing a second biome system.
            var wet = Mathf.Clamp01(moisture01);
            forestBias = Mathf.Clamp01(forestBias + (wet - 0.5f) * 0.12f);
            shrubBias = Mathf.Clamp01(shrubBias + (wet - 0.4f) * 0.18f);
            grassBias = Mathf.Clamp01(grassBias + (wet - 0.35f) * 0.2f);
        }

        /// <summary>
        /// Blend discrete biome ecology biases across nearby height/moisture samples so ecotones
        /// change character gradually instead of flipping with hard Classify bands.
        /// </summary>
        public static void SoftBiomeBiases(
            float height01,
            float slope01,
            float moisture01,
            out float forestBias,
            out float shrubBias,
            out float grassBias)
        {
            const float hDelta = 0.045f;
            const float mDelta = 0.08f;

            forestBias = 0f;
            shrubBias = 0f;
            grassBias = 0f;
            var wSum = 0f;

            AccumulateBiomeBias(height01, slope01, moisture01, 1f, ref forestBias, ref shrubBias, ref grassBias, ref wSum);
            AccumulateBiomeBias(height01 - hDelta, slope01, moisture01, 0.55f, ref forestBias, ref shrubBias, ref grassBias, ref wSum);
            AccumulateBiomeBias(height01 + hDelta, slope01, moisture01, 0.55f, ref forestBias, ref shrubBias, ref grassBias, ref wSum);
            AccumulateBiomeBias(height01, slope01, moisture01 - mDelta, 0.35f, ref forestBias, ref shrubBias, ref grassBias, ref wSum);
            AccumulateBiomeBias(height01, slope01, moisture01 + mDelta, 0.35f, ref forestBias, ref shrubBias, ref grassBias, ref wSum);

            if (wSum < 1e-5f)
            {
                GetBiomeWeights(BiomeId.Meadow, moisture01, out forestBias, out shrubBias, out grassBias);
                return;
            }

            forestBias /= wSum;
            shrubBias /= wSum;
            grassBias /= wSum;
        }

        private static void AccumulateBiomeBias(
            float height01,
            float slope01,
            float moisture01,
            float weight,
            ref float forestBias,
            ref float shrubBias,
            ref float grassBias,
            ref float wSum)
        {
            var biome = BiomeClassifier.Classify(height01, slope01, moisture01);
            GetBiomeWeights(biome, moisture01, out var f, out var s, out var g);
            forestBias += f * weight;
            shrubBias += s * weight;
            grassBias += g * weight;
            wSum += weight;
        }

        private static float SoftThreshold(float field, float bias)
        {
            // Higher biome bias lowers the macro threshold so Forest biomes form large masses;
            // Meadow keeps only the strongest macro peaks as scattered woodland.
            // Forest bias → large contiguous masses; meadow → only rare woodland peaks.
            var threshold = Mathf.Lerp(0.78f, 0.18f, Mathf.Clamp01(bias));
            var t = Mathf.Clamp01((field - threshold) / Mathf.Max(0.05f, 1f - threshold));
            t = t * t * (3f - 2f * t);
            return Mathf.Clamp01(t * Mathf.Lerp(0.85f, 1.05f, Mathf.Clamp01(bias)));
        }

        private static float Perlin(float worldX, float worldZ, float ox, float oz, float frequency)
        {
            return Mathf.PerlinNoise((worldX + ox) * frequency, (worldZ + oz) * frequency);
        }

        private static float Smooth01(float n)
        {
            var t = Mathf.Clamp01(n);
            return t * t * (3f - 2f * t);
        }
    }
}
