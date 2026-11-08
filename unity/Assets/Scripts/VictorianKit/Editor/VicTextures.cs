using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace GameDev.VictorianKit.EditorTools
{
    /// <summary>
    /// The seven tiling texture sets of the kit's material slots (Plans/VICTORIAN_STREETS_PLAN.md §4.1), made from
    /// code so they are reproducible: per slot a base map (sRGB), a normal map, and a mask map (R roughness, G the
    /// second-tint mask, B cavity). The kit's UVs are metres from each module's pivot, so each set covers one rapport
    /// in metres. Everything is deterministic: integer hashes, no UnityEngine.Random.
    /// </summary>
    public static class VicTextures
    {
        public static readonly string[] Slots = { "Brick", "Stucco", "Stone", "Slate", "Wood", "Iron", "Glass" };

        /// <summary>The rapport in metres, u × v.</summary>
        public static Vector2 Rapport(string slot) => slot == "Brick" ? new Vector2(2f, 1.5f) : new Vector2(2f, 2f);

        /// <summary>Pixels per rapport: 1024 px/m on both axes.</summary>
        public static Vector2Int Size(string slot) => slot == "Brick" ? new Vector2Int(2048, 1536) : new Vector2Int(2048, 2048);

        /// <summary>The brick joint pattern the seam check holds the meshes to: head joints every 0.25 m, courses 0.075 m.</summary>
        public const float BrickLength = 0.25f;
        public const float BrickCourse = 0.075f;

        public static string PathOf(string slot, string map) => $"{VicKitPaths.TextureFolder}/T_Vic_{slot}_{map}.jpg";

        /// <summary>A texture's mean colour in linear space, read from its small mip through a float target.</summary>
        public static Color LinearMean(Texture texture)
        {
            const int size = 32;
            var target = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            Graphics.Blit(texture, target);
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var read = new Texture2D(size, size, TextureFormat.RGBAFloat, false, true);
            read.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            read.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            var sum = Vector4.zero;
            var pixels = read.GetPixels();
            foreach (var p in pixels)
            {
                sum += (Vector4)p;
            }

            UnityEngine.Object.DestroyImmediate(read);
            return sum / pixels.Length;
        }

        [MenuItem("GameDev/Victorian Kit/Build Textures")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(VicKitPaths.TextureFolder);
            foreach (var slot in Slots)
            {
                var m = Make(slot);
                Write(m, slot);
            }

            AssetDatabase.Refresh();
            foreach (var slot in Slots)
            {
                Configure(PathOf(slot, "Base"), TextureImporterType.Default, true);
                Configure(PathOf(slot, "Normal"), TextureImporterType.NormalMap, false);
                Configure(PathOf(slot, "Mask"), TextureImporterType.Default, false);
            }
        }

        /// <summary>Builds the sets only when one is missing.</summary>
        public static void EnsureAll()
        {
            foreach (var slot in Slots)
            {
                foreach (var map in new[] { "Base", "Normal", "Mask" })
                {
                    if (!File.Exists(PathOf(slot, map)))
                    {
                        BuildAll();
                        return;
                    }
                }
            }
        }

        private static void Configure(string path, TextureImporterType type, bool srgb)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = type;
            importer.sRGBTexture = srgb;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;

            // Trilinear blends the mip levels instead of meeting in lines across a wall (reference plan R2.6).
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 16;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        /// <summary>One slot's maps before encoding: linear floats, height in metres.</summary>
        private sealed class Maps
        {
            public readonly int W, H;
            public readonly float Wm, Hm;
            public readonly float[] R, G, B, Height, Rough, Mask, Cavity;

            public Maps(int w, int h, float wm, float hm)
            {
                W = w;
                H = h;
                Wm = wm;
                Hm = hm;
                var n = w * h;
                R = new float[n];
                G = new float[n];
                B = new float[n];
                Height = new float[n];
                Rough = new float[n];
                Mask = new float[n];
                Cavity = new float[n];
            }

            public void Set(int i, float r, float g, float b, float height, float rough, float mask, float cavity)
            {
                R[i] = r;
                G[i] = g;
                B[i] = b;
                Height[i] = height;
                Rough[i] = rough;
                Mask[i] = mask;
                Cavity[i] = cavity;
            }
        }

        private static Maps Make(string slot)
        {
            var size = Size(slot);
            var rapport = Rapport(slot);
            var m = new Maps(size.x, size.y, rapport.x, rapport.y);
            Action<Maps, int, float, float> texel = slot switch
            {
                "Brick" => Brick,
                "Stucco" => Stucco,
                "Stone" => Stone,
                "Slate" => Slate,
                "Wood" => Wood,
                "Iron" => Iron,
                _ => Glass,
            };
            Parallel.For(0, m.H, y =>
            {
                for (var x = 0; x < m.W; x++)
                {
                    texel(m, (y * m.W) + x, (x + 0.5f) * m.Wm / m.W, (y + 0.5f) * m.Hm / m.H);
                }
            });
            return m;
        }

        // ---------------------------------------------------------------- slots

        /// <summary>
        /// Stretcher bond: bricks 0.25 m with a 0.125 m half-bond, courses 0.075 m, 10 mm joints. Brick colours repeat
        /// every 2 m; on odd courses the brick across u = 1 m takes the identity of the brick across u = 0, and each
        /// brick's detail is drawn in its own frame, so a 1 m corner leg meets the next wall with whole bricks.
        /// </summary>
        private static void Brick(Maps m, int i, float xm, float ym)
        {
            const float joint = 0.010f;
            var course = Mathf.Min((int)(ym / BrickCourse), 19);
            var cy = ym - (course * BrickCourse);
            var odd = (course & 1) == 1;
            var sx = xm + (odd ? BrickLength * 0.5f : 0f);
            var bi = (int)Mathf.Floor(sx / BrickLength);
            var fx = sx - (bi * BrickLength);
            var id = Mod(bi, 8);
            if (odd && id == 4)
            {
                id = 0;
            }

            var seed = (id * 31) + (course * 977);
            var jitter = (Value(fx * 90f, cy * 90f + seed, 4096, 4096, 7) - 0.5f) * 0.0028f;
            var d = Mathf.Min(Mathf.Min(fx, BrickLength - fx), Mathf.Min(cy, BrickCourse - cy)) - (joint * 0.5f) + jitter;
            var grain = Fbm(fx * 70f + seed, cy * 70f, 4096, 4096, 3, 21);
            var mortarMask = 1f - Smooth(-0.0007f, 0.0007f, d);
            var hb = H01(id, course, 11);
            var light = 0.80f * (0.80f + (0.32f * hb));
            if (H01(id, course, 12) < 0.07f)
            {
                light *= 0.62f;
            }

            var shift = (H01(id, course, 13) - 0.5f) * 0.10f;
            light *= 0.90f + (0.20f * grain);
            if (H01((int)(xm * 900f), (int)(ym * 900f), 14) < 0.015f)
            {
                light *= 0.8f;
            }

            var edge = Smooth(0f, 0.004f, d);
            light *= 0.86f + (0.14f * edge);
            var mortar = 0.88f * (0.92f + (0.10f * Fbm(xm * 300f, ym * 300f, 600, 450, 2, 31)));
            var r = Mathf.Lerp(light * (1f + shift), mortar, mortarMask);
            var g = Mathf.Lerp(light, mortar * 0.98f, mortarMask);
            var b = Mathf.Lerp(light * (1f - (shift * 0.8f)), mortar * 0.94f, mortarMask);
            var height = d < 0f ? (0.0006f * Fbm(xm * 400f, ym * 400f, 800, 600, 2, 41)) - 0.0005f : (0.005f * edge) + (0.0006f * (grain - 0.5f));
            var rough = Mathf.Lerp(0.80f + (0.12f * (hb - 0.5f)) + (0.06f * (grain - 0.5f)), 0.95f, mortarMask);
            var cavity = Mathf.Lerp(0.75f + (0.25f * edge), 0.55f, mortarMask);
            m.Set(i, r, g, b, height, rough, mortarMask, cavity);
        }

        /// <summary>Render scored as ashlar: blocks 1.0 × 0.5 m, half-bond, 3 mm score lines.</summary>
        private static void Stucco(Maps m, int i, float xm, float ym)
        {
            var course = (int)(ym / 0.5f);
            var cy = ym - (course * 0.5f);
            var sx = xm + ((course & 1) * 0.5f);
            var fx = sx - Mathf.Floor(sx);
            var d = Mathf.Min(Mathf.Min(fx, 1f - fx), Mathf.Min(cy, 0.5f - cy)) - 0.0015f;
            var score = 1f - Smooth(-0.0006f, 0.0006f, d);
            var trowel = Fbm(xm * 6f, ym * 6f, 12, 12, 4, 51);
            var fine = Fbm(xm * 200f, ym * 200f, 400, 400, 3, 52);
            var light = 0.86f * (0.97f + (0.04f * trowel)) * (0.97f + (0.06f * fine));
            light = Mathf.Lerp(light, light * 0.82f, score);
            var height = (0.0004f * (trowel - 0.5f)) + (0.0002f * (fine - 0.5f)) - (0.0018f * score);
            var rough = Mathf.Lerp(0.80f + (0.08f * (fine - 0.5f)), 0.92f, score);
            m.Set(i, light * 1.01f, light, light * 0.97f, height, rough, score, Mathf.Lerp(1f, 0.6f, score));
        }

        /// <summary>Fine-grained limestone: grain, faint bedding, pitting; no joints, the meshes make the blocks.</summary>
        private static void Stone(Maps m, int i, float xm, float ym)
        {
            var big = Fbm(xm * 2f, ym * 2f, 4, 4, 4, 61);
            var bed = Fbm(xm * 3f, ym * 40f, 6, 80, 3, 62);
            var fine = Fbm(xm * 150f, ym * 150f, 300, 300, 3, 63);
            var pit = Value(xm * 120f, ym * 120f, 240, 240, 64);
            var pitMask = Smooth(0.82f, 0.92f, pit);
            var light = 0.78f * (0.94f + (0.10f * big)) * (0.96f + (0.06f * bed)) * (0.93f + (0.12f * fine));
            light *= 1f - (0.35f * pitMask);
            var height = (0.0008f * (big - 0.5f)) + (0.0003f * (fine - 0.5f)) - (0.0008f * pitMask);
            var rough = 0.74f + (0.10f * (fine - 0.5f)) + (0.12f * pitMask);
            var grime = Mathf.Clamp01((0.6f * pitMask) + (0.6f * big * fine));
            m.Set(i, light * 1.02f, light, light * 0.95f, height, rough, grime, 1f - (0.4f * pitMask));
        }

        /// <summary>
        /// Slates 0.25 m wide with a 0.20 m exposure, alternate rows half-bonded; U along the eave, V up the slope,
        /// so each row's butt edge stands at its lower edge and the surface falls toward the next butt.
        /// </summary>
        private static void Slate(Maps m, int i, float xm, float ym)
        {
            const float width = 0.25f;
            const float exposure = 0.20f;
            var row = Mathf.Min((int)(ym / exposure), 9);
            var odd = (row & 1) == 1;
            var sx = xm + (odd ? width * 0.5f : 0f);
            var si = (int)Mathf.Floor(sx / width);
            var fx = sx - (si * width);
            var id = Mod(si, 8);
            var t = (ym - (row * exposure)) / exposure;
            var seed = (id * 17) + (row * 401);
            var butt = (Value(fx * 40f + seed, 0.5f, 4096, 4096, 71) - 0.5f) * 0.012f;
            var tb = Mathf.Clamp01(t - (butt / exposure));
            var gap = Mathf.Min(fx, width - fx) - 0.002f;
            var gapMask = 1f - Smooth(-0.0006f, 0.0006f, gap);
            var hs = H01(id, row, 72);
            var surface = Fbm(fx * 60f + seed, ym * 60f, 4096, 4096, 3, 73);
            var light = 0.75f * (0.86f + (0.22f * hs)) * (0.92f + (0.14f * surface));
            light *= 0.82f + (0.18f * Smooth(0f, 0.08f, tb));
            light = Mathf.Lerp(light, 0.18f, gapMask);
            var height = (0.006f * (1f - tb)) + (0.0004f * (surface - 0.5f)) + (0.001f * (hs - 0.5f)) - (0.003f * gapMask);
            var rough = 0.62f + (0.14f * (hs - 0.5f)) + (0.08f * (surface - 0.5f));
            var odds = H01(id, row, 74);
            var cavity = Mathf.Lerp(0.7f + (0.3f * Smooth(0f, 0.15f, tb)), 0.4f, gapMask);
            m.Set(i, light * 0.98f, light, light * 1.04f, height, rough, odds, cavity);
        }

        /// <summary>Painted boards, grain along U; G is the chip field (bare wood where the material asks).</summary>
        private static void Wood(Maps m, int i, float xm, float ym)
        {
            var grain = Fbm(xm * 3f, ym * 90f, 6, 180, 4, 81);
            var streak = Fbm(xm * 1.5f, ym * 220f, 3, 440, 2, 82);
            var chips = Fbm(xm * 9f, ym * 14f, 18, 28, 5, 83);
            var brush = Fbm(xm * 20f, ym * 400f, 40, 800, 2, 84);
            var strength = Mathf.Lerp(0.05f, 0.35f, chips * chips);
            var light = 0.85f * (1f - (strength * (0.5f + grain + (0.5f * streak) - 1f))) * (0.98f + (0.04f * brush));
            var height = (0.0003f * (grain - 0.5f)) + (0.0002f * (brush - 0.5f)) - (0.0006f * Smooth(0.6f, 0.8f, chips));
            var rough = 0.45f + (0.06f * (brush - 0.5f));
            m.Set(i, light, light * 0.98f, light * 0.95f, height, rough, chips, 1f - (0.25f * Smooth(0.6f, 0.9f, chips)));
        }

        /// <summary>Cast iron: mottling and pitting; G is the rust field.</summary>
        private static void Iron(Maps m, int i, float xm, float ym)
        {
            var mottle = Fbm(xm * 6f, ym * 6f, 12, 12, 4, 91);
            var pit = Value(xm * 160f, ym * 160f, 320, 320, 92);
            var pitMask = Smooth(0.8f, 0.92f, pit);
            var rust = Fbm(xm * 5f, ym * 5f, 10, 10, 5, 93);
            var light = 0.8f * (0.9f + (0.2f * mottle)) * (1f - (0.3f * pitMask));
            var height = (0.0003f * (mottle - 0.5f)) - (0.0005f * pitMask);
            var rough = 0.55f + (0.25f * (mottle - 0.5f)) + (0.2f * pitMask);
            m.Set(i, light, light, light * 1.02f, height, rough, Mathf.Clamp01(rust + (0.3f * pitMask)), 1f - (0.3f * pitMask));
        }

        /// <summary>Old crown glass: faint horizontal ripple and smudges; G is the grime field.</summary>
        private static void Glass(Maps m, int i, float xm, float ym)
        {
            var ripple = Fbm(xm * 4f, ym * 30f, 8, 60, 3, 101);
            var smudge = Fbm(xm * 3f, ym * 3f, 6, 6, 5, 102);
            var light = 0.95f * (0.97f + (0.03f * smudge));
            var height = 0.0003f * (ripple - 0.5f);
            var rough = 0.06f + (0.2f * Smooth(0.55f, 0.85f, smudge));
            m.Set(i, light, light, light, height, rough, smudge, 1f);
        }

        // ---------------------------------------------------------------- encoding

        private static void Write(Maps m, string slot)
        {
            var n = m.W * m.H;
            var albedo = new Color32[n];
            var normal = new Color32[n];
            var mask = new Color32[n];
            var px = m.Wm / m.W;
            var py = m.Hm / m.H;
            Parallel.For(0, m.H, y =>
            {
                for (var x = 0; x < m.W; x++)
                {
                    var i = (y * m.W) + x;
                    albedo[i] = new Color32(Srgb(m.R[i]), Srgb(m.G[i]), Srgb(m.B[i]), 255);
                    var l = (y * m.W) + Mod(x - 1, m.W);
                    var r = (y * m.W) + Mod(x + 1, m.W);
                    var d = (Mod(y - 1, m.H) * m.W) + x;
                    var u = (Mod(y + 1, m.H) * m.W) + x;
                    var dx = (m.Height[r] - m.Height[l]) / (2f * px);
                    var dy = (m.Height[u] - m.Height[d]) / (2f * py);
                    var nv = new Vector3(-dx, -dy, 1f).normalized;
                    normal[i] = new Color32(Byte((nv.x * 0.5f) + 0.5f), Byte((nv.y * 0.5f) + 0.5f), Byte((nv.z * 0.5f) + 0.5f), 255);
                    mask[i] = new Color32(Byte(m.Rough[i]), Byte(m.Mask[i]), Byte(m.Cavity[i]), 255);
                }
            });

            Encode(albedo, m, PathOf(slot, "Base"));
            Encode(normal, m, PathOf(slot, "Normal"));
            Encode(mask, m, PathOf(slot, "Mask"));
        }

        private static void Encode(Color32[] pixels, Maps m, string path)
        {
            var texture = new Texture2D(m.W, m.H, TextureFormat.RGBA32, false, true);
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToJPG(92));
            UnityEngine.Object.DestroyImmediate(texture);
        }

        /// <summary>The base map holds sRGB-encoded colour; the generator works in linear.</summary>
        private static byte Srgb(float linear) => Byte(Mathf.LinearToGammaSpace(Mathf.Clamp01(linear)));

        private static byte Byte(float v) => (byte)Mathf.Clamp(Mathf.RoundToInt(v * 255f), 0, 255);

        // ---------------------------------------------------------------- noise

        private static int Mod(int a, int n) => ((a % n) + n) % n;

        private static float Smooth(float a, float b, float x)
        {
            var t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - (2f * t));
        }

        private static uint Hash(int x, int y, int seed)
        {
            unchecked
            {
                var h = ((uint)x * 374761393u) + ((uint)y * 668265263u) + ((uint)seed * 2246822519u);
                h = (h ^ (h >> 13)) * 1274126177u;
                return h ^ (h >> 16);
            }
        }

        private static float H01(int x, int y, int seed) => (Hash(x, y, seed) & 0xFFFFFF) / 16777216f;

        /// <summary>Value noise on a lattice that wraps every <paramref name="px"/> × <paramref name="py"/> cells.</summary>
        private static float Value(float x, float y, int px, int py, int seed)
        {
            var x0 = (int)Mathf.Floor(x);
            var y0 = (int)Mathf.Floor(y);
            var fx = x - x0;
            var fy = y - y0;
            fx = fx * fx * fx * ((fx * ((fx * 6f) - 15f)) + 10f);
            fy = fy * fy * fy * ((fy * ((fy * 6f) - 15f)) + 10f);
            int xa = Mod(x0, px), xb = Mod(x0 + 1, px), ya = Mod(y0, py), yb = Mod(y0 + 1, py);
            var a = Mathf.Lerp(H01(xa, ya, seed), H01(xb, ya, seed), fx);
            var b = Mathf.Lerp(H01(xa, yb, seed), H01(xb, yb, seed), fx);
            return Mathf.Lerp(a, b, fy);
        }

        /// <summary>Wrapping fractal noise in [0, 1]: each octave doubles frequency and period, so it still tiles.</summary>
        private static float Fbm(float x, float y, int px, int py, int octaves, int seed)
        {
            var sum = 0f;
            var amp = 0.5f;
            var norm = 0f;
            var f = 1;
            for (var o = 0; o < octaves; o++)
            {
                sum += amp * Value(x * f, y * f, px * f, py * f, seed + (o * 1013));
                norm += amp;
                amp *= 0.5f;
                f *= 2;
            }

            return sum / norm;
        }
    }
}
