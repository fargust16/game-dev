using UnityEngine;

namespace GameDev.World.Terrain
{
    /// <summary>
    /// Biome terrain remaps from OVERWORLD_VISUAL_DIRECTION (warm Fields, cool Forest, murky Swamp).
    /// </summary>
    public static class TerrainBiomePalette
    {
        /// <summary>Memo albedo tile size (8–16 m).</summary>
        public const float AlbedoTileMeters = 10f;

        public const float HighlandTileMeters = 12f;
        public const float RockTileMeters = 14f;
        public const float SnowTileMeters = 16f;

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

        /// <summary>Fields — warm lime / olive / ochre (sunny).</summary>
        public static readonly Look Fields = new Look(
            new Color(0.16f, 0.20f, 0.05f),
            new Color(0.62f, 0.72f, 0.28f));

        /// <summary>Forest — cool deep greens (still readable under grass tint).</summary>
        public static readonly Look Forest = new Look(
            new Color(0.04f, 0.10f, 0.05f),
            new Color(0.30f, 0.50f, 0.30f));

        /// <summary>Swamp — murky desaturated green-grey / mud.</summary>
        public static readonly Look Swamp = new Look(
            new Color(0.06f, 0.09f, 0.07f),
            new Color(0.34f, 0.40f, 0.32f));

        /// <summary>City / dusty packed soil — beige, light brown, grey.</summary>
        public static readonly Look Dust = new Look(
            new Color(0.16f, 0.14f, 0.10f),
            new Color(0.62f, 0.54f, 0.40f));

        /// <summary>Highland gravel — muted stone-beige.</summary>
        public static readonly Look Highland = new Look(
            new Color(0.12f, 0.11f, 0.09f),
            new Color(0.60f, 0.56f, 0.46f));

        /// <summary>Cliff / rock — desaturated stone.</summary>
        public static readonly Look Rock = new Look(
            new Color(0.08f, 0.08f, 0.07f),
            new Color(0.52f, 0.50f, 0.45f));

        /// <summary>Snow — bright low-sat; cool shadow bias.</summary>
        public static readonly Look Snow = new Look(
            new Color(0.66f, 0.70f, 0.76f),
            new Color(0.96f, 0.97f, 0.99f),
            smoothness: 0.14f);

        /// <summary>Road center — light dry trodden earth.</summary>
        public static readonly Look RoadCenter = new Look(
            new Color(0.30f, 0.24f, 0.14f),
            new Color(0.82f, 0.72f, 0.50f),
            smoothness: 0.1f);

        /// <summary>Road edge — darker blend into biome soil.</summary>
        public static readonly Look RoadEdge = new Look(
            new Color(0.06f, 0.05f, 0.03f),
            new Color(0.38f, 0.30f, 0.18f));

        /// <summary>Road gravel — light stones/dust.</summary>
        public static readonly Look RoadGravel = new Look(
            new Color(0.16f, 0.15f, 0.11f),
            new Color(0.70f, 0.66f, 0.54f));

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
