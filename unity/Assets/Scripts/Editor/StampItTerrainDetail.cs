#if UNITY_EDITOR
using System.Collections.Generic;
using GameDev.World;
using GameDev.World.Terrain;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Applies StampIT heightmap PNGs as detail onto a Vista-generated Unity Terrain.
    /// Stand-in for Vista LoadTexture/Splatter nodes until editable graphs under Assets/Settings/Vista
    /// are authored; same StampIT assets, post-generate blend with elevation/coast masks.
    /// </summary>
    public static class StampItTerrainDetail
    {
        private const string HeightmapRoot = "Assets/Rowlan/Terrain/Stamps/Examples/Heightmaps";

        private struct StampPass
        {
            public string FileName;
            public float Strength01;
            public float MinLand01;
            public float MaxLand01;
            public bool CoastOnly;
            public bool InteriorPeaksOnly;
        }

        private static readonly StampPass[] Passes =
        {
            new StampPass
            {
                FileName = "Shores 09.png",
                Strength01 = 0.035f,
                MinLand01 = 0f,
                MaxLand01 = 0.12f,
                CoastOnly = true,
            },
            new StampPass
            {
                FileName = "Sandy Beaches 05.png",
                Strength01 = 0.02f,
                MinLand01 = 0f,
                MaxLand01 = 0.08f,
                CoastOnly = true,
            },
            new StampPass
            {
                FileName = "Meadows 12.png",
                Strength01 = 0.025f,
                MinLand01 = 0.05f,
                MaxLand01 = 0.45f,
            },
            new StampPass
            {
                FileName = "Highlands 09.png",
                Strength01 = 0.04f,
                MinLand01 = 0.35f,
                MaxLand01 = 0.75f,
            },
            new StampPass
            {
                FileName = "Alpine Mountains 23.png",
                Strength01 = 0.07f,
                MinLand01 = 0.55f,
                MaxLand01 = 1f,
                InteriorPeaksOnly = true,
            },
            new StampPass
            {
                FileName = "Rugged Rocks 14.png",
                Strength01 = 0.03f,
                MinLand01 = 0.5f,
                MaxLand01 = 1f,
                InteriorPeaksOnly = true,
            },
        };

        /// <summary>
        /// Soft StampIT detail on dry land only. Does not replace island occupancy; call after it.
        /// </summary>
        public static void ApplyDetail(UnityEngine.Terrain terrain, int seed)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                return;
            }

            TerrainHeightWrite.ForceBudgetSize(terrain);
            var data = terrain.terrainData;
            var res = data.heightmapResolution;
            var size = data.size;
            var heights = data.GetHeights(0, 0, res, res);
            var sea01 = Mathf.Clamp01(WorldScale.SeaLevelMeters / Mathf.Max(0.01f, size.y));
            var landBudget01 = Mathf.Max(0.01f, 1f - sea01);
            var rng = new System.Random(seed);
            var offsetU = (float)rng.NextDouble();
            var offsetV = (float)rng.NextDouble();

            var readable = new Dictionary<string, Color32[]>();
            var dims = new Dictionary<string, Vector2Int>();

            foreach (var pass in Passes)
            {
                var path = $"{HeightmapRoot}/{pass.FileName}";
                if (!TryGetPixels(path, out var pixels, out var w, out var h))
                {
                    Debug.LogWarning($"[Overworld] StampIT heightmap missing or unreadable: {path}");
                    continue;
                }

                readable[pass.FileName] = pixels;
                dims[pass.FileName] = new Vector2Int(w, h);
            }

            if (readable.Count == 0)
            {
                Debug.LogWarning("[Overworld] No StampIT heightmaps applied.");
                return;
            }

            var cx = size.x * 0.5f;
            var cz = size.z * 0.5f;
            var islandRadius = Mathf.Min(size.x, size.z) * 0.5f - WorldScale.IslandOceanMarginMeters;

            for (var z = 0; z < res; z++)
            {
                var v = z / (float)(res - 1);
                var wz = v * size.z;
                for (var x = 0; x < res; x++)
                {
                    var u = x / (float)(res - 1);
                    var wx = u * size.x;
                    var h = heights[z, x];
                    if (h <= sea01 + 0.002f)
                    {
                        continue;
                    }

                    var land01 = Mathf.Clamp01((h - sea01) / landBudget01);
                    var dx = wx - cx;
                    var dz = wz - cz;
                    var radial = Mathf.Sqrt(dx * dx + dz * dz) / Mathf.Max(1f, islandRadius);
                    var coastMask = Mathf.Clamp01(1f - Mathf.Abs(radial - 0.92f) / 0.12f);
                    var interiorMask = Mathf.Clamp01(1f - radial);

                    var add = 0f;
                    foreach (var pass in Passes)
                    {
                        if (!readable.TryGetValue(pass.FileName, out var pixels))
                        {
                            continue;
                        }

                        if (land01 < pass.MinLand01 || land01 > pass.MaxLand01)
                        {
                            continue;
                        }

                        var dim = dims[pass.FileName];
                        var sample = SampleBilinear(pixels, dim.x, dim.y, u + offsetU, v + offsetV);
                        var weight = pass.Strength01;
                        if (pass.CoastOnly)
                        {
                            weight *= coastMask;
                        }

                        if (pass.InteriorPeaksOnly)
                        {
                            weight *= interiorMask * Mathf.InverseLerp(pass.MinLand01, pass.MaxLand01, land01);
                        }

                        add += (sample - 0.5f) * 2f * weight;
                    }

                    heights[z, x] = Mathf.Clamp01(h + add);
                    // Never raise ocean/shelf cells or fill below sea after stamp.
                    if (h <= sea01)
                    {
                        heights[z, x] = Mathf.Min(heights[z, x], sea01 - 0.001f);
                    }
                }
            }

            // Keep tile border submerged after stamps.
            var maxBorder = sea01 - WorldScale.MinimumSubmersionMeters / Mathf.Max(0.01f, size.y);
            for (var i = 0; i < res; i++)
            {
                heights[0, i] = Mathf.Min(heights[0, i], maxBorder);
                heights[res - 1, i] = Mathf.Min(heights[res - 1, i], maxBorder);
                heights[i, 0] = Mathf.Min(heights[i, 0], maxBorder);
                heights[i, res - 1] = Mathf.Min(heights[i, res - 1], maxBorder);
            }

            TerrainHeightWrite.Commit(terrain, heights);
            Debug.Log($"[Overworld] StampIT detail applied ({readable.Count} stamps, seed={seed}).");
        }

        private static bool TryGetPixels(string assetPath, out Color32[] pixels, out int width, out int height)
        {
            pixels = null;
            width = 0;
            height = 0;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex == null)
            {
                return false;
            }

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            var changed = false;
            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
                changed = true;
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            }

            if (tex == null)
            {
                return false;
            }

            try
            {
                pixels = tex.GetPixels32();
                width = tex.width;
                height = tex.height;
                return pixels != null && pixels.Length > 0;
            }
            catch (UnityException ex)
            {
                Debug.LogWarning($"[Overworld] StampIT GetPixels failed for {assetPath}: {ex.Message}");
                return false;
            }
            finally
            {
                // Leave readable on so subsequent Generate runs stay fast; stamps are authoring assets.
                if (changed)
                {
                    EditorUtility.SetDirty(importer);
                }
            }
        }

        private static float SampleBilinear(Color32[] pixels, int w, int h, float u, float v)
        {
            u = u - Mathf.Floor(u);
            v = v - Mathf.Floor(v);
            var x = u * (w - 1);
            var y = v * (h - 1);
            var x0 = Mathf.FloorToInt(x);
            var y0 = Mathf.FloorToInt(y);
            var x1 = Mathf.Min(w - 1, x0 + 1);
            var y1 = Mathf.Min(h - 1, y0 + 1);
            var tx = x - x0;
            var ty = y - y0;

            float Lum(Color32 c) => (c.r + c.g + c.b) / (3f * 255f);
            var a = Lum(pixels[y0 * w + x0]);
            var b = Lum(pixels[y0 * w + x1]);
            var c0 = Lum(pixels[y1 * w + x0]);
            var d = Lum(pixels[y1 * w + x1]);
            var top = Mathf.Lerp(a, b, tx);
            var bot = Mathf.Lerp(c0, d, tx);
            return Mathf.Lerp(top, bot, ty);
        }
    }
}
#endif
