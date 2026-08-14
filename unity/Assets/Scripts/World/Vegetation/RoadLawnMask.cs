using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Vegetation
{
    /// <summary>
    /// World-space lawn factor for roads: 0 = packed road bed, 1 = full lawn.
    /// Bound as a global shader texture so grass height/density/tilt stay continuous
    /// without extra meshes or draw calls.
    /// </summary>
    public static class RoadLawnMask
    {
        public static readonly int TextureId = Shader.PropertyToID("_OverworldRoadLawnMask");
        public static readonly int WorldId = Shader.PropertyToID("_OverworldRoadLawnMask_World");
        public static readonly int EdgeParamsId = Shader.PropertyToID("_OverworldGrassEdgeParams");

        public const int DefaultResolution = 768;

        private static Texture2D boundTexture;

        /// <summary>
        /// Sample lawn factor from road clearance samples (CPU mirror of the baked mask).
        /// </summary>
        public static float SampleLawn01(
            float worldX,
            float worldZ,
            IReadOnlyList<Vector2> roadSamples,
            float clearCoreMeters,
            float transitionMeters)
        {
            if (roadSamples == null || roadSamples.Count == 0)
            {
                return 1f;
            }

            var best = float.MaxValue;
            for (var i = 0; i < roadSamples.Count; i++)
            {
                var s = roadSamples[i];
                var dx = worldX - s.x;
                var dz = worldZ - s.y;
                var d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d < best)
                {
                    best = d;
                }
            }

            return VegetationClearance.FalloffMultiplier(best, clearCoreMeters, transitionMeters);
        }

        public static void BindDefaults()
        {
            Shader.SetGlobalTexture(TextureId, Texture2D.whiteTexture);
            Shader.SetGlobalVector(WorldId, new Vector4(0f, 0f, 1f, 1f));
            BindEdgeParams();
        }

        public static void BindEdgeParams()
        {
            Shader.SetGlobalVector(
                EdgeParamsId,
                new Vector4(
                    GrassMask.HeightReduceIntensity,
                    GrassMask.DensityReduceIntensity,
                    GrassMask.EdgeTiltStrength,
                    GrassMask.DryEdgeStrength));
        }

        /// <summary>
        /// Stamp road samples into an R8 mask and bind globals. Safe to call from EditMode.
        /// </summary>
        public static void BakeAndBind(
            IReadOnlyList<Vector2> roadSamples,
            float worldMinX,
            float worldMinZ,
            float worldSizeX,
            float worldSizeZ,
            int resolution = DefaultResolution)
        {
            BindEdgeParams();

            if (roadSamples == null || roadSamples.Count == 0 ||
                worldSizeX < 1f || worldSizeZ < 1f)
            {
                BindDefaults();
                return;
            }

            var res = Mathf.Clamp(resolution, 64, 2048);
            var tex = boundTexture;
            if (tex == null || tex.width != res || tex.height != res)
            {
                if (tex != null)
                {
                    Object.DestroyImmediate(tex);
                }

                tex = new Texture2D(res, res, TextureFormat.R8, false, true)
                {
                    name = "OverworldRoadLawnMask",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.DontSave
                };
                boundTexture = tex;
            }

            var pixels = new byte[res * res];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = 255;
            }

            var core = GrassMask.RoadClearCoreMeters;
            var falloff = GrassMask.RoadEdgeFalloffMeters;
            var stampR = core + falloff;
            var invSizeX = 1f / worldSizeX;
            var invSizeZ = 1f / worldSizeZ;
            var metersPerPxX = worldSizeX / res;
            var metersPerPxZ = worldSizeZ / res;
            var stampPx = Mathf.CeilToInt(stampR / Mathf.Min(metersPerPxX, metersPerPxZ)) + 1;

            for (var i = 0; i < roadSamples.Count; i++)
            {
                var s = roadSamples[i];
                var cx = Mathf.FloorToInt((s.x - worldMinX) * invSizeX * res);
                var cz = Mathf.FloorToInt((s.y - worldMinZ) * invSizeZ * res);
                if (cx < -stampPx || cz < -stampPx || cx >= res + stampPx || cz >= res + stampPx)
                {
                    continue;
                }

                var minX = Mathf.Max(0, cx - stampPx);
                var maxX = Mathf.Min(res - 1, cx + stampPx);
                var minZ = Mathf.Max(0, cz - stampPx);
                var maxZ = Mathf.Min(res - 1, cz + stampPx);

                for (var z = minZ; z <= maxZ; z++)
                {
                    var worldZ = worldMinZ + (z + 0.5f) * metersPerPxZ;
                    for (var x = minX; x <= maxX; x++)
                    {
                        var worldX = worldMinX + (x + 0.5f) * metersPerPxX;
                        var dx = worldX - s.x;
                        var dz = worldZ - s.y;
                        var dist = Mathf.Sqrt(dx * dx + dz * dz);
                        var lawn = VegetationClearance.FalloffMultiplier(dist, core, falloff);
                        var v = (byte)Mathf.Clamp(Mathf.RoundToInt(lawn * 255f), 0, 255);
                        var idx = z * res + x;
                        if (v < pixels[idx])
                        {
                            pixels[idx] = v;
                        }
                    }
                }
            }

            tex.SetPixelData(pixels, 0);
            tex.Apply(false, false);

            Shader.SetGlobalTexture(TextureId, tex);
            Shader.SetGlobalVector(
                WorldId,
                new Vector4(worldMinX, worldMinZ, worldSizeX, worldSizeZ));
        }

        public static void BakeFromScene()
        {
            var clearance = Object.FindFirstObjectByType<Roads.OverworldRoadClearance>();
            var terrain = UnityEngine.Terrain.activeTerrains != null &&
                          UnityEngine.Terrain.activeTerrains.Length > 0
                ? UnityEngine.Terrain.activeTerrains[0]
                : Object.FindFirstObjectByType<UnityEngine.Terrain>();

            if (terrain == null || terrain.terrainData == null)
            {
                BindDefaults();
                return;
            }

            var pos = terrain.transform.position;
            var size = terrain.terrainData.size;
            BakeAndBind(
                clearance != null ? clearance.Samples : null,
                pos.x,
                pos.z,
                size.x,
                size.z);
        }
    }
}
