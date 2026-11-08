using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The pack's dark walls (Wall_5, Wall_6, Wall_8) carry a frieze of light red damask under their cornice. Under a
    /// ceiling, and between the storeys of the 7 m taproom, it read as a missing panel (owner, VI-8 remarks 1 and 3). Our
    /// own copy of each material recolours the damask in the walls' dark wood, its pattern kept faintly, like the
    /// embossed papers of Victorian pubs; the pack's files and import settings stay as they are. The texture is read by
    /// a blit, since the pack's textures are not readable. Colours are authoring defaults.
    /// </summary>
    internal static class ViFrieze
    {
        public const string Folder = SliceKitCatalogue.MaterialFolder + "/Vi";
        private const string Source = "Assets/Victorian Interiors/Victorian Interiors/Materials";

        // The frieze's wood in sRGB, near the panels' own: the damask's dark ground becomes Dark, its pink pattern Light,
        // so the pattern stays as a faint emboss in the wood.
        private static readonly Color Light = new(0.33f, 0.17f, 0.105f), Dark = new(0.22f, 0.105f, 0.065f);

        /// <summary>The path of the wall's material with its frieze in wood, made once.</summary>
        public static string Ensure(string wall)
        {
            var path = $"{Folder}/{wall}_WoodFrieze.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            {
                return path;
            }

            var source = AssetDatabase.LoadAssetAtPath<Material>($"{Source}/{wall}.mat");
            var albedo = (Texture2D)source.GetTexture("_BaseMap");
            var image = Read(albedo);
            var pixels = image.GetPixels();
            int w = image.width, h = image.height;

            // The damask's red marks the frieze; its light ground and soft edges are taken within that box.
            int x0 = w, y0 = h, x1 = -1, y1 = -1;
            for (var i = 0; i < pixels.Length; i++)
            {
                if (Redness(pixels[i]) > 0.5f)
                {
                    int x = i % w, y = i / w;
                    x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x);
                    y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y);
                }
            }

            if (x1 < 0)
            {
                Debug.LogWarning($"[ViFrieze] {wall}: no damask found; the pack's material is kept.");
                return $"{Source}/{wall}.mat";
            }

            const int pad = 8;
            x0 = Mathf.Max(0, x0 - pad); y0 = Mathf.Max(0, y0 - pad);
            x1 = Mathf.Min(w - 1, x1 + pad); y1 = Mathf.Min(h - 1, y1 + pad);
            for (var y = y0; y <= y1; y++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    // Every pixel of the strip is recoloured (VI-8: a blend by redness left the damask as it was), but the
                    // black of the atlas's gaps, which no face samples.
                    var c = pixels[(y * w) + x];
                    var lum = (0.299f * c.r) + (0.587f * c.g) + (0.114f * c.b);
                    if (lum < 0.05f)
                    {
                        continue;
                    }

                    var wood = Color.Lerp(Dark, Light, Mathf.SmoothStep(0.1f, 0.65f, lum));
                    wood.a = c.a;
                    pixels[(y * w) + x] = wood;
                }
            }

            image.SetPixels(pixels);
            image.Apply();
            KitStandIns.EnsureFolder(Folder);
            var texturePath = $"{Folder}/{wall}_WoodFrieze_Albedo.png";
            File.WriteAllBytes(texturePath, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            AssetDatabase.ImportAsset(texturePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.sRGBTexture = true;
            importer.maxTextureSize = Mathf.Max(albedo.width, albedo.height);
            importer.SaveAndReimport();

            var copy = new Material(source);
            copy.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            if (copy.HasProperty("_MainTex"))
            {
                copy.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            }

            AssetDatabase.CreateAsset(copy, path);
            Debug.Log($"[ViFrieze] {wall}: the frieze x {x0}..{x1}, y {y0}..{y1} recoloured in wood.");
            return path;
        }

        private static float Redness(Color c) => Mathf.Clamp01((c.r - c.g - 0.25f) / 0.2f) * (c.r > 0.4f ? 1f : 0f);

        private static Texture2D Read(Texture2D texture)
        {
            var rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(texture, rt);
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var image = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false, false);
            image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            return image;
        }
    }
}
