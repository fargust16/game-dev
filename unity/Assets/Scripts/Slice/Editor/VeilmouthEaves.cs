using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameDev.Slice.Kit;
using GameDev.VictorianKit;
using UnityEditor;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The eaves map (VEILMOUTH_REFERENCE_PLAN.md R3 step 2, the soot gradient under the cornice): a top-down map of the
    /// city, one texel each 0.5 m, holding the height of the wall's top of the kit house standing there, 0 where none
    /// stands. It is an 8-bit PNG, a step of 0.25 m from 20 m (<see cref="GameDev.VictorianKit.VicGround.EavesBase"/>),
    /// so the mostly empty map weighs little in the repository. Each kit house's body colliders run from the ground to its wall's top (<see cref="VeilmouthKitHouses"/>),
    /// so the map is read from them exactly. The facade shader reads it 0.75 m inside a wall (<see cref="GameDev.VictorianKit.VicGround"/>)
    /// and gathers soot under that height. Baked again with the facades' ground whenever the city or its mystic layer
    /// is built. The texel is an authoring default.
    /// </summary>
    public static class VeilmouthEaves
    {
        public const string MapPath = VeilmouthMystic.Folder + "/Veilmouth_Eaves.png";
        public const string SillsPath = VeilmouthMystic.Folder + "/Veilmouth_Sills.png";
        private const float Texel = 0.5f, Margin = 2f;

        // A column's footprint reaches this far either side of its panes' plane, m, so the facade under a window, read
        // 0.75 m inside the wall, finds it whichever way the pane faces. An authoring default.
        private const float Strip = 1.1f;

        /// <summary>Bakes the map from the open scene's kit houses and saves it; returns it and its Rect (x0, z0, 1/size x, 1/size z), or null with no house.</summary>
        public static (Texture2D Map, Vector4 Rect) Bake()
        {
            var bodies = Bodies().ToList();
            if (bodies.Count == 0)
            {
                return (null, Vector4.zero);
            }

            var bounds = bodies[0].bounds;
            foreach (var b in bodies.Skip(1))
            {
                bounds.Encapsulate(b.bounds);
            }

            var x0 = Mathf.Floor(bounds.min.x - Margin);
            var z0 = Mathf.Floor(bounds.min.z - Margin);
            var w = Mathf.CeilToInt((bounds.max.x + Margin - x0) / Texel);
            var h = Mathf.CeilToInt((bounds.max.z + Margin - z0) / Texel);
            var tops = new float[w * h];
            var maxSize = Mathf.NextPowerOfTwo(Mathf.Max(w, h));
            foreach (var box in bodies)
            {
                var t = box.transform;
                var top = t.TransformPoint(box.center + (Vector3.up * (box.size.y / 2f))).y;
                var b = box.bounds;
                for (var j = Mathf.Max(0, Mathf.FloorToInt((b.min.z - z0) / Texel)); j < Mathf.Min(h, Mathf.CeilToInt((b.max.z - z0) / Texel)); j++)
                {
                    for (var i = Mathf.Max(0, Mathf.FloorToInt((b.min.x - x0) / Texel)); i < Mathf.Min(w, Mathf.CeilToInt((b.max.x - x0) / Texel)); i++)
                    {
                        var local = t.InverseTransformPoint(new Vector3(x0 + ((i + 0.5f) * Texel), top - 0.1f, z0 + ((j + 0.5f) * Texel))) - box.center;
                        if (Mathf.Abs(local.x) <= box.size.x / 2f && Mathf.Abs(local.z) <= box.size.z / 2f)
                        {
                            tops[(j * w) + i] = Mathf.Max(tops[(j * w) + i], top);
                        }
                    }
                }
            }

            var bytes = new byte[w * h];
            for (var k = 0; k < bytes.Length; k++)
            {
                bytes[k] = tops[k] > 0f ? (byte)Mathf.Clamp(Mathf.RoundToInt((tops[k] - GameDev.VictorianKit.VicGround.EavesBase) / GameDev.VictorianKit.VicGround.EavesStep), 1, 255) : (byte)0;
            }

            var source = new Texture2D(w, h, TextureFormat.R8, false, true);
            source.SetPixelData(bytes, 0);
            source.Apply(false, false);
            File.WriteAllBytes(MapPath, source.EncodeToPNG());
            Object.DestroyImmediate(source);
            AssetDatabase.ImportAsset(MapPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(MapPath);
            importer.textureType = TextureImporterType.SingleChannel;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.sRGBTexture = false;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = Mathf.Max(maxSize, 2048);
            importer.isReadable = true; // VeilmouthCityTests reads it back
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.singleChannelComponent = TextureImporterSingleChannelComponent.Red;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            var map = AssetDatabase.LoadAssetAtPath<Texture2D>(MapPath);
            Debug.Log($"[VeilmouthEaves] {bodies.Count} house bodies over {w} × {h} texels of {Texel} m from ({x0}, {z0}); walls' tops {bodies.Min(TopOf):0.0}–{bodies.Max(TopOf):0.0} m.");
            return (map, new Vector4(x0, z0, 1f / (w * Texel), 1f / (h * Texel)));
        }

        /// <summary>
        /// Bakes the sill map (R3 step 2, the stains under the sills): one texel each 0.5 m over every window pane of the
        /// city's kit houses (<see cref="VicWindowRooms"/>), holding in R the column's lowest sill, in G its second, in B
        /// its highest, each in steps of 0.125 m from 20 m rounded up (<see cref="VicGround.SillsStep"/>), and in A how
        /// much of the texel the column covers. Above the second sill every sill stands one storey of 3.75 m over the one
        /// under it: measured over the city's panes, only a column's first gap differs (4.8 m under a tall ground floor,
        /// 7.5 m where a storey has no window), which is why three heights hold the whole column. A pane's sill is the
        /// bottom of its glass, and the stone sill under it hides where a stain starts. Returns the map and its Rect, or
        /// null with no pane.
        /// </summary>
        public static (Texture2D Map, Vector4 Rect) BakeSills()
        {
            var panes = Object.FindObjectsByType<VicWindowRooms>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .SelectMany(r => Enumerable.Range(0, r.Count).Select(i => r[i].Pane))
                .Where(p => p != null && p.enabled)
                .Select(Footprint)
                .ToList();
            if (panes.Count == 0)
            {
                return (null, Vector4.zero);
            }

            var reach = panes.Max(p => p.Half) + Strip + Margin;
            var x0 = Mathf.Floor(panes.Min(p => p.Centre.x) - reach);
            var z0 = Mathf.Floor(panes.Min(p => p.Centre.y) - reach);
            var w = Mathf.CeilToInt((panes.Max(p => p.Centre.x) + reach - x0) / Texel);
            var h = Mathf.CeilToInt((panes.Max(p => p.Centre.y) + reach - z0) / Texel);
            var cover = new float[w * h];
            var sills = new Dictionary<int, List<float>>();
            const int sub = 4;
            foreach (var pane in panes)
            {
                var r = pane.Half + Strip;
                for (var j = Mathf.Max(0, Mathf.FloorToInt((pane.Centre.y - r - z0) / Texel)); j < Mathf.Min(h, Mathf.CeilToInt((pane.Centre.y + r - z0) / Texel)); j++)
                {
                    for (var i = Mathf.Max(0, Mathf.FloorToInt((pane.Centre.x - r - x0) / Texel)); i < Mathf.Min(w, Mathf.CeilToInt((pane.Centre.x + r - x0) / Texel)); i++)
                    {
                        var inside = 0;
                        for (var sj = 0; sj < sub; sj++)
                        {
                            for (var si = 0; si < sub; si++)
                            {
                                var d = new Vector2(x0 + ((i + ((si + 0.5f) / sub)) * Texel), z0 + ((j + ((sj + 0.5f) / sub)) * Texel)) - pane.Centre;
                                if (Mathf.Abs(Vector2.Dot(d, pane.Right)) <= pane.Half && Mathf.Abs(Vector2.Dot(d, pane.Normal)) <= Strip)
                                {
                                    inside++;
                                }
                            }
                        }

                        if (inside == 0)
                        {
                            continue;
                        }

                        var k = (j * w) + i;
                        cover[k] = Mathf.Min(1f, cover[k] + ((float)inside / (sub * sub)));
                        if (!sills.TryGetValue(k, out var list))
                        {
                            sills[k] = list = new List<float>();
                        }

                        list.Add(pane.Sill);
                    }
                }
            }

            static byte Encode(float y) => (byte)Mathf.Clamp(Mathf.CeilToInt(((y - VicGround.SillsBase) / VicGround.SillsStep) - 0.01f), 1, 255);
            var bytes = new byte[w * h * 4];
            var irregular = 0;
            foreach (var (k, list) in sills)
            {
                var column = new List<float>();
                foreach (var y in list.OrderBy(y => y))
                {
                    if (column.Count == 0 || y - column[^1] > 0.06f)
                    {
                        column.Add(y);
                    }
                }

                if (column.Skip(1).Any(y => Mathf.Abs(Mathf.Repeat(y - column[Mathf.Min(1, column.Count - 1)] + 0.5f, VicGround.Storey) - 0.5f) > 0.06f))
                {
                    irregular++;
                }

                bytes[(k * 4) + 0] = Encode(column[0]);
                bytes[(k * 4) + 1] = column.Count > 1 ? Encode(column[1]) : (byte)0;
                bytes[(k * 4) + 2] = Encode(column[^1]);
            }

            // The heights reach one texel past the column, so the cover's soft edge, read between texels, still finds them.
            var grown = (byte[])bytes.Clone();
            for (var j = 0; j < h; j++)
            {
                for (var i = 0; i < w; i++)
                {
                    var k = (j * w) + i;
                    if (bytes[k * 4] != 0)
                    {
                        continue;
                    }

                    for (var n = 0; n < 8 && grown[k * 4] == 0; n++)
                    {
                        var (di, dj) = n switch { 0 => (1, 0), 1 => (-1, 0), 2 => (0, 1), 3 => (0, -1), 4 => (1, 1), 5 => (-1, 1), 6 => (1, -1), _ => (-1, -1) };
                        var (ni, nj) = (i + di, j + dj);
                        if (ni >= 0 && ni < w && nj >= 0 && nj < h && bytes[((nj * w) + ni) * 4] != 0)
                        {
                            System.Array.Copy(bytes, ((nj * w) + ni) * 4, grown, k * 4, 3);
                        }
                    }
                }
            }

            for (var k = 0; k < cover.Length; k++)
            {
                grown[(k * 4) + 3] = (byte)Mathf.RoundToInt(cover[k] * 255f);
            }

            var source = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
            source.SetPixelData(grown, 0);
            source.Apply(false, false);
            File.WriteAllBytes(SillsPath, source.EncodeToPNG());
            Object.DestroyImmediate(source);
            AssetDatabase.ImportAsset(SillsPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SillsPath);
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.sRGBTexture = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear; // the cover; the shader reads the heights by its own point sampler
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = Mathf.Max(Mathf.NextPowerOfTwo(Mathf.Max(w, h)), 2048);
            importer.isReadable = true; // VeilmouthCityTests reads it back
            importer.SaveAndReimport();
            Debug.Log($"[VeilmouthEaves] sill map: {panes.Count} panes over {w} × {h} texels of {Texel} m from ({x0}, {z0}); {sills.Count} texels hold a column, {irregular} of them off the storey's rhythm.");
            return (AssetDatabase.LoadAssetAtPath<Texture2D>(SillsPath), new Vector4(x0, z0, 1f / (w * Texel), 1f / (h * Texel)));
        }

        /// <summary>
        /// A pane seen from above: its middle, its width's direction and half width, its face's direction, and its sill,
        /// the bottom of its glass. The kit's pane is a plane in its local X (width) and Z (height), thin in Y.
        /// </summary>
        public static (Vector2 Centre, Vector2 Right, Vector2 Normal, float Half, float Sill) Footprint(MeshRenderer pane)
        {
            var t = pane.transform;
            var local = pane.localBounds;
            var centre = t.TransformPoint(local.center);
            var across = t.TransformVector(new Vector3(local.extents.x, 0f, 0f));
            var flat = new Vector2(across.x, across.z);
            var right = flat.normalized;
            return (new Vector2(centre.x, centre.z), right, new Vector2(-right.y, right.x), flat.magnitude, pane.bounds.min.y);
        }

        /// <summary>Every kit house's body: the box colliders on its root, which run from the ground to its wall's top.</summary>
        internal static IEnumerable<BoxCollider> Bodies() =>
            Object.FindObjectsByType<KitBlock>(FindObjectsSortMode.None)
                .Where(b => b.enabled && b.transform.Find("Model") != null)
                .SelectMany(b => b.GetComponents<BoxCollider>())
                .Where(c => c.enabled);

        internal static float TopOf(BoxCollider box) => box.transform.TransformPoint(box.center + (Vector3.up * (box.size.y / 2f))).y;
    }
}
