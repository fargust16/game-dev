using UnityEngine;

namespace GameDev.World.Terrain
{
    /// <summary>
    /// Biome terrain remaps from the overworld paint memo.
    /// Readable biome identity, no neon plastics — fields warm, forest cool-green,
    /// swamp teal/grey, city dusty beige, roads light dry soil.
    /// </summary>
    public static class TerrainBiomePalette
    {
        /// <summary>Memo albedo tile size (6–12 m).</summary>
        public const float AlbedoTileMeters = 8f;

        public const float HighlandTileMeters = 10f;
        public const float RockTileMeters = 11f;
        public const float SnowTileMeters = 12f;

        public readonly struct Look
        {
            public readonly Vector4 RemapMin;
            public readonly Vector4 RemapMax;
            public readonly float Smoothness;

            public Look(Color min, Color max, float smoothness = 0.05f)
            {
                RemapMin = new Vector4(min.r, min.g, min.b, 0f);
                RemapMax = new Vector4(max.r, max.g, max.b, 1f);
                Smoothness = smoothness;
            }
        }

        /// <summary>Fields — warm golden / olive greens (sunny, dry).</summary>
        public static readonly Look Fields = new Look(
            new Color(0.14f, 0.16f, 0.06f),
            new Color(0.55f, 0.58f, 0.30f));

        /// <summary>Forest — cool saturated greens; deep cool shadow.</summary>
        public static readonly Look Forest = new Look(
            new Color(0.02f, 0.07f, 0.04f),
            new Color(0.24f, 0.48f, 0.28f));

        /// <summary>Swamp — desaturated teal / murky blue-grey-green.</summary>
        public static readonly Look Swamp = new Look(
            new Color(0.04f, 0.07f, 0.08f),
            new Color(0.28f, 0.36f, 0.34f));

        /// <summary>City / dusty packed soil — beige, light brown, grey.</summary>
        public static readonly Look Dust = new Look(
            new Color(0.14f, 0.12f, 0.09f),
            new Color(0.58f, 0.50f, 0.38f));

        /// <summary>Highland gravel — muted stone-beige.</summary>
        public static readonly Look Highland = new Look(
            new Color(0.10f, 0.10f, 0.09f),
            new Color(0.58f, 0.54f, 0.46f));

        /// <summary>Cliff / rock — desaturated stone, high roughness feel.</summary>
        public static readonly Look Rock = new Look(
            new Color(0.07f, 0.07f, 0.07f),
            new Color(0.50f, 0.48f, 0.44f));

        /// <summary>Snow — bright but low sat; cool shadow bias.</summary>
        public static readonly Look Snow = new Look(
            new Color(0.62f, 0.66f, 0.72f),
            new Color(0.95f, 0.96f, 0.98f),
            smoothness: 0.14f);

        /// <summary>Road center — light dry trodden earth vs surrounding biome.</summary>
        public static readonly Look RoadCenter = new Look(
            new Color(0.26f, 0.22f, 0.14f),
            new Color(0.78f, 0.68f, 0.48f),
            smoothness: 0.1f);

        /// <summary>Road edge — darker blend into biome soil.</summary>
        public static readonly Look RoadEdge = new Look(
            new Color(0.05f, 0.04f, 0.025f),
            new Color(0.36f, 0.28f, 0.18f));

        /// <summary>Road gravel — light stones/dust.</summary>
        public static readonly Look RoadGravel = new Look(
            new Color(0.14f, 0.13f, 0.10f),
            new Color(0.66f, 0.62f, 0.52f));

        public static void Apply(TerrainLayer layer, Look look, float tileMeters)
        {
            if (layer == null)
            {
                return;
            }

            layer.tileSize = new Vector2(tileMeters, tileMeters);
            layer.diffuseRemapMin = look.RemapMin;
            layer.diffuseRemapMax = look.RemapMax;
            layer.smoothness = look.Smoothness;
            layer.metallic = 0f;
        }
    }
}
