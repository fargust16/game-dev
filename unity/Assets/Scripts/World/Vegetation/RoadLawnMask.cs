using System.Collections.Generic;
using GameDev.World;
using GameDev.World.Roads;
using UnityEngine;

namespace GameDev.World.Vegetation
{
    /// <summary>
    /// World-space lawn factor for roads: sparse residual at bed → 1 = full lawn.
    /// Bound as a global shader texture so grass height/density/tilt stay continuous
    /// without extra meshes or draw calls.
    /// </summary>
    public static class RoadLawnMask
    {
        public static readonly int TextureId = Shader.PropertyToID("_OverworldRoadLawnMask");
        public static readonly int WorldId = Shader.PropertyToID("_OverworldRoadLawnMask_World");
        public static readonly int EdgeParamsId = Shader.PropertyToID("_OverworldGrassEdgeParams");

        /// <summary>~1 m/px on a 1400 m tile keeps 0.5–1.5 m shoulders continuous.</summary>
        public const int DefaultResolution = 1536;

        private static Texture2D boundTexture;

        /// <summary>
        /// Sample lawn factor from road clearance samples (CPU mirror of the baked mask).
        /// Uses segment distance so shoulders are not scalloped disks.
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

            var best = RoadSampleDistance.NearestMeters(new Vector2(worldX, worldZ), roadSamples);
            return VegetationClearance.FalloffMultiplier(
                best, clearCoreMeters, transitionMeters, GrassMask.RoadLawnMinAtCenter);
        }

        public static void BindDefaults()
        {
            Shader.SetGlobalTexture(TextureId, Texture2D.whiteTexture);
            Shader.SetGlobalVector(WorldId, new Vector4(0f, 0f, 1f, 1f));
            BindEdgeParams();
            cpuPixels = null;
            cpuRes = 0;
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
        /// Stamp road polylines into an R8 mask and bind globals. Safe to call from EditMode.
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
            var metersPerPx = Mathf.Max(worldSizeX / res, worldSizeZ / res);
            var falloff = RoadSurfaceStyle.SoftRimMeters(metersPerPx);
            var stampR = core + falloff;
            var metersPerPxX = worldSizeX / res;
            var metersPerPxZ = worldSizeZ / res;
            var pad = stampR + 2f;

            for (var i = 0; i < roadSamples.Count - 1; i++)
            {
                var a = roadSamples[i];
                var b = roadSamples[i + 1];
                var len = Vector2.Distance(a, b);
                if (len < 1e-4f || len > RoadSampleDistance.MaxSegmentGapMeters)
                {
                    continue;
                }

                var minX = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.x, b.x) - pad - worldMinX) / metersPerPxX));
                var maxX = Mathf.Min(res - 1, Mathf.CeilToInt((Mathf.Max(a.x, b.x) + pad - worldMinX) / metersPerPxX));
                var minZ = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.y, b.y) - pad - worldMinZ) / metersPerPxZ));
                var maxZ = Mathf.Min(res - 1, Mathf.CeilToInt((Mathf.Max(a.y, b.y) + pad - worldMinZ) / metersPerPxZ));

                for (var z = minZ; z <= maxZ; z++)
                {
                    var worldZ = worldMinZ + (z + 0.5f) * metersPerPxZ;
                    for (var x = minX; x <= maxX; x++)
                    {
                        var worldX = worldMinX + (x + 0.5f) * metersPerPxX;
                        var dist = RoadSampleDistance.PointToSegmentMeters(
                            new Vector2(worldX, worldZ), a, b);
                        var lawn = VegetationClearance.FalloffMultiplier(
                            dist, core, falloff, GrassMask.RoadLawnMinAtCenter);
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
            boundTexture = tex;
            cpuPixels = pixels;
            cpuRes = res;
            cpuMinX = worldMinX;
            cpuMinZ = worldMinZ;
            cpuSizeX = worldSizeX;
            cpuSizeZ = worldSizeZ;
            Shader.SetGlobalTexture(TextureId, tex);
            Shader.SetGlobalVector(
                WorldId,
                new Vector4(worldMinX, worldMinZ, worldSizeX, worldSizeZ));
        }

        /// <summary>True after <see cref="BakeAndBind"/> with road data (CPU buffer ready for grass bake).</summary>
        public static bool HasCpuBake =>
            cpuPixels != null && cpuRes > 0 && cpuSizeX > 0f && cpuSizeZ > 0f;

        /// <summary>
        /// Bilinear sample of the baked lawn texture (O(1)). Prefer this over polyline scans in grass bake.
        /// </summary>
        public static float SampleBakedLawn01(float worldX, float worldZ)
        {
            if (!HasCpuBake)
            {
                return 1f;
            }

            var u = (worldX - cpuMinX) / cpuSizeX;
            var v = (worldZ - cpuMinZ) / cpuSizeZ;
            if (u < 0f || v < 0f || u > 1f || v > 1f)
            {
                return 1f;
            }

            var fx = u * (cpuRes - 1);
            var fz = v * (cpuRes - 1);
            var x0 = Mathf.FloorToInt(fx);
            var z0 = Mathf.FloorToInt(fz);
            var x1 = Mathf.Min(x0 + 1, cpuRes - 1);
            var z1 = Mathf.Min(z0 + 1, cpuRes - 1);
            var tx = fx - x0;
            var tz = fz - z0;
            tx = tx * tx * (3f - 2f * tx);
            tz = tz * tz * (3f - 2f * tz);

            float A = cpuPixels[z0 * cpuRes + x0] / 255f;
            float B = cpuPixels[z0 * cpuRes + x1] / 255f;
            float C = cpuPixels[z1 * cpuRes + x0] / 255f;
            float D = cpuPixels[z1 * cpuRes + x1] / 255f;
            return Mathf.Lerp(Mathf.Lerp(A, B, tx), Mathf.Lerp(C, D, tx), tz);
        }

        public static void BakeFromScene()
        {
            var clearance = Object.FindAnyObjectByType<OverworldRoadClearance>();
            var terrain = UnityEngine.Terrain.activeTerrains != null &&
                          UnityEngine.Terrain.activeTerrains.Length > 0
                ? UnityEngine.Terrain.activeTerrains[0]
                : Object.FindAnyObjectByType<UnityEngine.Terrain>();
            if (terrain == null || terrain.terrainData == null)
            {
                BindDefaults();
                cpuPixels = null;
                cpuRes = 0;
                return;
            }

            BakeAndBind(
                clearance != null ? clearance.Samples : null,
                0f,
                0f,
                WorldScale.WorldExtentXMeters,
                WorldScale.WorldExtentZMeters);
        }

        private static byte[] cpuPixels;
        private static int cpuRes;
        private static float cpuMinX;
        private static float cpuMinZ;
        private static float cpuSizeX;
        private static float cpuSizeZ;
    }
}
