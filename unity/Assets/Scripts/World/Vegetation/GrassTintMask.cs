using GameDev.World;
using GameDev.World.Terrain;
using UnityEngine;

namespace GameDev.World.Vegetation
{
    /// <summary>
    /// World-space grass tint from biome (Fields warm lime, Forest cool moss, etc.).
    /// Bound as a global RGB texture for <c>GameDev/Overworld/Grass</c>.
    /// </summary>
    public static class GrassTintMask
    {
        public static readonly int TextureId = Shader.PropertyToID("_OverworldGrassTintMask");
        public static readonly int WorldId = Shader.PropertyToID("_OverworldGrassTintMask_World");
        public static readonly int StrengthId = Shader.PropertyToID("_OverworldGrassTintStrength");

        public const int DefaultResolution = 384;
        public const float DefaultStrength = 0.55f;

        private static Texture2D boundTexture;

        public static Color TintForBiome(BiomeId biome)
        {
            // Midpoint of TerrainBiomePalette remaps so blades match ground underfoot.
            switch (biome)
            {
                case BiomeId.Meadow:
                    return Mid(TerrainBiomePalette.Fields);
                case BiomeId.Forest:
                    return Mid(TerrainBiomePalette.Forest);
                case BiomeId.Swamp:
                    return Mid(TerrainBiomePalette.Swamp);
                case BiomeId.Highland:
                    return Mid(TerrainBiomePalette.Highland);
                case BiomeId.Mountain:
                    return Color.Lerp(Mid(TerrainBiomePalette.Rock), Mid(TerrainBiomePalette.Highland), 0.35f);
                default:
                    return Mid(TerrainBiomePalette.Fields);
            }
        }

        private static Color Mid(TerrainBiomePalette.Look look)
        {
            return new Color(
                (look.RemapMin.x + look.RemapMax.x) * 0.5f,
                (look.RemapMin.y + look.RemapMax.y) * 0.5f,
                (look.RemapMin.z + look.RemapMax.z) * 0.5f);
        }

        /// <summary>Soft-blended tint across Classify neighbors (no hard hue seams).</summary>
        public static Color TintSoft(float height01, float slope01, float moisture01)
        {
            const float hDelta = 0.045f;
            const float mDelta = 0.08f;
            var c = TintForBiome(BiomeClassifier.Classify(height01, slope01, moisture01)) * 1f;
            c += TintForBiome(BiomeClassifier.Classify(height01 - hDelta, slope01, moisture01)) * 0.55f;
            c += TintForBiome(BiomeClassifier.Classify(height01 + hDelta, slope01, moisture01)) * 0.55f;
            c += TintForBiome(BiomeClassifier.Classify(height01, slope01, moisture01 - mDelta)) * 0.35f;
            c += TintForBiome(BiomeClassifier.Classify(height01, slope01, moisture01 + mDelta)) * 0.35f;
            return c / 2.8f;
        }

        public static void BindDefaults()
        {
            Shader.SetGlobalTexture(TextureId, Texture2D.whiteTexture);
            Shader.SetGlobalVector(WorldId, new Vector4(0f, 0f, 1f, 1f));
            Shader.SetGlobalFloat(StrengthId, 0f);
        }

        public static void BakeAndBind(
            VistaTerrainSampler sampler,
            float worldMinX,
            float worldMinZ,
            float worldSizeX,
            float worldSizeZ,
            int resolution = DefaultResolution)
        {
            if (sampler == null || worldSizeX < 1f || worldSizeZ < 1f)
            {
                BindDefaults();
                return;
            }

            var res = Mathf.Clamp(resolution, 32, 512);
            var tex = boundTexture;
            if (tex == null || tex.width != res || tex.height != res)
            {
                if (tex != null)
                {
                    Object.DestroyImmediate(tex);
                }

                tex = new Texture2D(res, res, TextureFormat.RGB24, false, true)
                {
                    name = "OverworldGrassTintMask",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.DontSave
                };
                boundTexture = tex;
            }

            var metersPerPxX = worldSizeX / res;
            var metersPerPxZ = worldSizeZ / res;
            var pixels = new Color[res * res];
            for (var z = 0; z < res; z++)
            {
                var worldZ = worldMinZ + (z + 0.5f) * metersPerPxZ;
                for (var x = 0; x < res; x++)
                {
                    var worldX = worldMinX + (x + 0.5f) * metersPerPxX;
                    var h = sampler.SampleHeight(worldX, worldZ);
                    var slope = sampler.SampleSlope01(worldX, worldZ);
                    var moisture = sampler.SampleMoisture01(worldX, worldZ);
                    pixels[z * res + x] = TintSoft(h, slope, moisture);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, false);
            Shader.SetGlobalTexture(TextureId, tex);
            Shader.SetGlobalVector(WorldId, new Vector4(worldMinX, worldMinZ, worldSizeX, worldSizeZ));
            Shader.SetGlobalFloat(StrengthId, DefaultStrength);
        }

        public static void BakeFromScene()
        {
            var terrain = OverworldTerrainAccess.FindTerrain();
            if (terrain == null || terrain.terrainData == null)
            {
                BindDefaults();
                return;
            }

            var sampler = new VistaTerrainSampler(terrain, 0);
            BakeAndBind(sampler, 0f, 0f, WorldScale.WorldExtentXMeters, WorldScale.WorldExtentZMeters);
        }
    }
}
