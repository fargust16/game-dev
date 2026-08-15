#if UNITY_EDITOR
using System.IO;
using GameDev.World;
using GameDev.World.Terrain;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Debug PNGs for natural-island geography (mask, coastline, peaks, below-sea, connectivity).
    /// </summary>
    public static class IslandGeographyDebug
    {
        private const string OutputFolder = "Assets/Debug/IslandGeography";

        [MenuItem("GameDev/Overworld/Debug/Export Island Geography PNGs")]
        public static void ExportMenu()
        {
            var terrain = OverworldTerrainAccess.FindTerrain();
            if (terrain == null)
            {
                Debug.LogWarning("[IslandGeographyDebug] No terrain.");
                return;
            }

            Export(terrain, OverworldTerrainMenu.LastSeed);
        }

        public static void Export(UnityEngine.Terrain terrain, int worldSeed)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                return;
            }

            EnsureFolder();
            var data = terrain.terrainData;
            var res = data.heightmapResolution;
            var size = data.size;
            var heights = data.GetHeights(0, 0, res, res);
            var sea01 = WorldScale.SeaLevelMeters / Mathf.Max(0.01f, size.y);

            var maskTex = new Texture2D(res, res, TextureFormat.RGB24, false);
            var heightTex = new Texture2D(res, res, TextureFormat.RGB24, false);
            var waterTex = new Texture2D(res, res, TextureFormat.RGB24, false);

            var analysis = WaterConnectivity.Analyze(terrain);
            var inland = new bool[res, res];
            for (var i = 0; i < analysis.InlandRegions.Count; i++)
            {
                var region = analysis.InlandRegions[i];
                for (var c = 0; c < region.Cells.Count; c++)
                {
                    var cell = region.Cells[c];
                    inland[cell.y, cell.x] = true;
                }
            }

            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    var wx = (x / (float)(res - 1)) * size.x;
                    var wz = (z / (float)(res - 1)) * size.z;
                    var mask = IslandNaturalGeography.SampleMaskPublic(wx, wz, size, worldSeed);
                    var h = heights[z, x];
                    var landY = (h - sea01) / Mathf.Max(1e-4f, 1f - sea01);

                    // Mask: white = land influence.
                    maskTex.SetPixel(x, z, new Color(mask.LandMask, mask.LandMask, mask.LandMask));

                    // Height bands: blue water, green lowland, yellow hills, red mountains, white peaks.
                    Color band;
                    if (h <= sea01)
                    {
                        band = inland[z, x] ? new Color(0.55f, 0.2f, 0.85f) : Color.blue; // purple inland / blue ocean
                    }
                    else if (landY < 0.15f)
                    {
                        band = Color.green;
                    }
                    else if (landY < 0.4f)
                    {
                        band = Color.yellow;
                    }
                    else if (landY < 0.7f)
                    {
                        band = Color.red;
                    }
                    else
                    {
                        band = Color.white;
                    }

                    heightTex.SetPixel(x, z, band);
                    waterTex.SetPixel(x, z, h <= sea01 ? Color.blue : Color.black);
                }
            }

            maskTex.Apply();
            heightTex.Apply();
            waterTex.Apply();
            WritePng(maskTex, $"{OutputFolder}/island_mask.png");
            WritePng(heightTex, $"{OutputFolder}/height_bands.png");
            WritePng(waterTex, $"{OutputFolder}/below_sea.png");
            Object.DestroyImmediate(maskTex);
            Object.DestroyImmediate(heightTex);
            Object.DestroyImmediate(waterTex);

            Debug.Log(
                $"[IslandGeographyDebug] Exported PNGs. inlandLakes={analysis.InlandLakeCount} " +
                $"borderSubmerged={analysis.BorderSubmerged} minBorderY={analysis.MinBorderHeightMeters:F2}");
        }

        private static void WritePng(Texture2D tex, string path)
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Debug"))
            {
                AssetDatabase.CreateFolder("Assets", "Debug");
            }

            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                AssetDatabase.CreateFolder("Assets/Debug", "IslandGeography");
            }
        }
    }
}
#endif
