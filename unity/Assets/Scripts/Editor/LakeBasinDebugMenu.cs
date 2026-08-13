#if UNITY_EDITOR
using System.IO;
using GameDev.World.Water;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Dumps lake mask / distance / depth debug PNGs for the first Overworld water zone.
    /// Use to verify the distance field is a filled basin (not groove stripes) before blaming Depth(u).
    /// </summary>
    public static class LakeBasinDebugMenu
    {
        private const string OutputDir = "Assets/Debug/LakeBathymetry";

        [MenuItem("GameDev/Overworld/Debug/Dump Lake Distance Field")]
        public static void DumpLakeDistanceField()
        {
            var holder = Object.FindFirstObjectByType<OverworldWaterZones>();
            if (holder == null || holder.Zones == null || holder.Zones.Count == 0)
            {
                Debug.LogWarning("[LakeDebug] No OverworldWaterZones with lakes in the open scene.");
                return;
            }

            var zone = holder.Zones[0];
            float[,] maskSdf = null;
            if (zone.Silhouette == null)
            {
                maskSdf = LakeShoreField.BuildSignedDistance(zone, exteriorCapMeters: 40f);
            }

            var aabb = zone.GetFloodAabbXZ();
            const int res = 256;
            var dMax = LakeBathymetry.MaxInteriorDistanceMeters(
                zone, (x, z) => zone.ShoreSignedDistance(x, z, maskSdf));
            var profile = LakeTerrainCarver.Profile;
            profile.NoiseAmplitude = 0f;
            var maxDepth = LakeTerrainCarver.DepthForLakeSize(
                zone, LakeTerrainCarver.MaxLargeBasinDepthMeters);

            var mask = new Texture2D(res, res, TextureFormat.RGBA32, false);
            var dist = new Texture2D(res, res, TextureFormat.RGBA32, false);
            var depth = new Texture2D(res, res, TextureFormat.RGBA32, false);

            for (var y = 0; y < res; y++)
            {
                var v = y / (res - 1f);
                var worldZ = Mathf.Lerp(aabb.yMin, aabb.yMax, v);
                for (var x = 0; x < res; x++)
                {
                    var u = x / (res - 1f);
                    var worldX = Mathf.Lerp(aabb.xMin, aabb.xMax, u);
                    var signed = zone.ShoreSignedDistance(worldX, worldZ, maskSdf);
                    var inside = signed >= 0f;
                    mask.SetPixel(x, y, inside ? Color.white : Color.black);

                    var dn = inside ? LakeBathymetry.NormalizeU(signed, dMax) : 0f;
                    dist.SetPixel(x, y, new Color(dn, dn, dn, 1f));

                    var dig = inside
                        ? LakeBathymetry.BaseDepthMeters(dn, maxDepth, profile) / Mathf.Max(maxDepth, 1e-3f)
                        : 0f;
                    depth.SetPixel(x, y, new Color(dig, dig * 0.7f, dig * 0.4f, 1f));
                }
            }

            mask.Apply();
            dist.Apply();
            depth.Apply();

            if (!Directory.Exists(OutputDir))
            {
                Directory.CreateDirectory(OutputDir);
            }

            WritePng(mask, $"{OutputDir}/lake_mask.png");
            WritePng(dist, $"{OutputDir}/lake_distance_u.png");
            WritePng(depth, $"{OutputDir}/lake_depth.png");
            Object.DestroyImmediate(mask);
            Object.DestroyImmediate(dist);
            Object.DestroyImmediate(depth);
            AssetDatabase.Refresh();

            Debug.Log(
                $"[LakeDebug] zone='{zone.Id}' dMax={dMax:F1}m Dmax={maxDepth:F1}m p={profile.ShapeExponent:F2} " +
                $"waterY={zone.WaterLevelY:F2} → {OutputDir}/ " +
                $"(distance should be smooth black shore → bright interior, not stripes)");
        }

        private static void WritePng(Texture2D tex, string assetPath)
        {
            var abs = Path.GetFullPath(assetPath);
            File.WriteAllBytes(abs, tex.EncodeToPNG());
        }
    }
}
#endif
