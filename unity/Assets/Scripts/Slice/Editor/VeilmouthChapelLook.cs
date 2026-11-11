using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The old chapel in the city's palette (VEILMOUTH_CITY_PLAN.md §44; WORLD_STYLE_GUIDE.md §2). The Colonial pack's
    /// church wears two painted sheets: dusty pink plaster, cream dressings, a red cupola and orange pantiles, a colonial
    /// mission and no part of a soot-dark Victorian port, against the guide's "no pink walls" and "no tiled roof". Each
    /// sheet's colour is painted afresh into a project copy, pixel by pixel by its hue, saturation and value, keeping the
    /// pack's own stains, streaks and tile courses: the pink plaster becomes a warm grey stone at the guide's lower bound,
    /// the cream dressings a lighter stone, the cupola lead and the pantiles slate, each a little smoother where they
    /// meet; wood, iron, glass and the inner floor's pattern keep the pack's. A copy of each of the pack's materials wears
    /// the new sheet with the pack's own normal, occlusion and metal maps, and is swapped in on every kit wrapper that
    /// wears the pack's, in place, so the scene's chapel keeps its link; the kit catalogue swaps them in on a rebuilt
    /// wrapper. Deleting the copies' folder and rebuilding the chapel's wrapper returns the pack's look. Every colour is an
    /// authoring default, in sRGB.
    /// </summary>
    public static class VeilmouthChapelLook
    {
        public const string Folder = VeilmouthLook.Folder + "/Chapel";
        private const string Pack = "Assets/3DShapes/Colonial City LittlePack – Church Graveyard Environment/";

        /// <summary>The pack's two sheets: its material, its colour texture, and the copy's name.</summary>
        private static readonly (string Vendor, string Texture, string Copy)[] Sheets =
        {
            (Pack + "Materials/Graveyard/Church_From.mat", Pack + "Textures/GraveyardChurch_Textures/ChurchFront_BaseColor.png", "Veilmouth_Chapel_Front"),
            (Pack + "Materials/Graveyard/Church_Back.mat", Pack + "Textures/GraveyardChurch_Textures/ChurchBack_BaseColor.png", "Veilmouth_Chapel_Back"),
        };

        /// <summary>
        /// Each class of the pack's paint: the colour it becomes, the pack's own luminance it is measured against, and how
        /// much of the pack's light and dark it keeps. Stone at the guide's 0.55 and 0.68, slate and lead at 0.25 and under.
        /// </summary>
        private static readonly Color Plaster = new Color(0.55f, 0.515f, 0.46f), Dressing = new Color(0.68f, 0.65f, 0.585f), Lead = new Color(0.215f, 0.23f, 0.255f), Slate = new Color(0.2f, 0.215f, 0.24f);

        private const float PlasterRef = 0.535f, DressingRef = 0.89f, LeadRef = 0.316f, SlateRef = 0.46f;
        private const float PlasterKeep = 1.2f, DressingKeep = 1.0f, LeadKeep = 0.8f, SlateKeep = 0.75f;

        /// <summary>
        /// Paints the chapel's sheets into the copies again and swaps them in on the kit wrappers that wear the pack's.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthChapelLook.RetintChapel
        /// </summary>
        [MenuItem("GameDev/Slice/Reference Look/Retint Chapel")]
        public static void RetintChapel()
        {
            Directory.CreateDirectory(Folder);
            foreach (var sheet in Sheets)
            {
                Paint(sheet);
            }

            AssetDatabase.SaveAssets();
            var changed = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { SliceKitCatalogue.Folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!AssetDatabase.GetDependencies(path, true).Any(d => Sheets.Any(s => d == s.Vendor)))
                {
                    continue;
                }

                var root = PrefabUtility.LoadPrefabContents(path);
                if (Retint(root) > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    changed++;
                }

                PrefabUtility.UnloadPrefabContents(root);
            }

            Debug.Log($"[VeilmouthChapelLook] {Sheets.Length} chapel sheets painted, {changed} kit wrappers changed.");
        }

        /// <summary>
        /// Swaps the pack's chapel materials for their copies on every renderer under <paramref name="root"/>, where the
        /// copies exist; the kit catalogue calls it when it wraps a model, so a rebuilt wrapper keeps the retint.
        /// </summary>
        public static int Retint(GameObject root) =>
            VeilmouthLook.Swap(root, Sheets
                .Select(s => (From: AssetDatabase.LoadAssetAtPath<Material>(s.Vendor), To: AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/{s.Copy}.mat")))
                .Where(s => s.From != null && s.To != null)
                .ToDictionary(s => s.From, s => s.To));

        /// <summary>One sheet painted into its texture and its material copy, both made on first use and kept in place after.</summary>
        private static void Paint((string Vendor, string Texture, string Copy) sheet)
        {
            var vendor = AssetDatabase.LoadAssetAtPath<Material>(sheet.Vendor);
            if (vendor == null || !File.Exists(sheet.Texture))
            {
                Debug.LogError($"[VeilmouthChapelLook] {sheet.Vendor} or its sheet is missing from the Colonial pack.");
                return;
            }

            // The pack's sheet read from its file, so its import settings do not matter, painted, and written beside the copy.
            var source = new Texture2D(2, 2, TextureFormat.RGB24, false, false);
            source.LoadImage(File.ReadAllBytes(sheet.Texture));
            var pixels = source.GetPixels32();
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Repaint(pixels[i]);
            }

            source.SetPixels32(pixels);
            source.Apply(false, false);
            var texturePath = $"{Folder}/{sheet.Copy}_BaseColor.png";
            File.WriteAllBytes(texturePath, source.EncodeToPNG());
            Object.DestroyImmediate(source);
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(texturePath) is TextureImporter importer && AssetImporter.GetAtPath(sheet.Texture) is TextureImporter pack)
            {
                importer.sRGBTexture = true;
                importer.maxTextureSize = pack.maxTextureSize;
                importer.textureCompression = pack.textureCompression;
                importer.compressionQuality = pack.compressionQuality;
                importer.SaveAndReimport();
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            var path = $"{Folder}/{sheet.Copy}.mat";
            var copy = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (copy == null)
            {
                copy = new Material(vendor) { name = sheet.Copy };
                AssetDatabase.CreateAsset(copy, path);
            }
            else
            {
                copy.CopyPropertiesFromMaterial(vendor);
            }

            copy.SetTexture("_BaseMap", texture);
            copy.SetTexture("_MainTex", texture);
            EditorUtility.SetDirty(copy);
        }

        /// <summary>
        /// One pixel of the pack's paint in the city's: its weight in each class by hue, saturation and value, each class's
        /// colour scaled by the pixel's luminance against the class's own, and what no class claims left as it was.
        /// </summary>
        private static Color32 Repaint(Color32 p)
        {
            var c = (Color)p; // the sheet's sRGB values as stored, 0–1
            Color.RGBToHSV(c, out var h, out var s, out var v);
            h *= 360f;
            var lum = (0.299f * c.r) + (0.587f * c.g) + (0.114f * c.b);

            // The plaster is a light, dusty red; the cupola a strong red, darker and greyer toward its rim.
            var plaster = Near(h, 0f, 8f, 13f) * Rise(s, 0.12f, 0.22f) * (1f - Rise(s, 0.5f, 0.6f)) * Rise(v, 0.45f, 0.6f);
            var lead = Near(h, 3f, 8f, 14f) * Mathf.Max(Rise(s, 0.5f, 0.6f), Rise(s, 0.2f, 0.3f) * (1f - Rise(v, 0.45f, 0.6f))) * Rise(v, 0.08f, 0.2f);
            var slate = Near(h, 18f, 7f, 12f) * Rise(s, 0.3f, 0.4f) * Rise(v, 0.3f, 0.45f);
            var dressing = (1f - Rise(s, 0.1f, 0.2f)) * Rise(v, 0.6f, 0.8f);
            var total = plaster + lead + slate + dressing;
            if (total > 1f)
            {
                plaster /= total;
                lead /= total;
                slate /= total;
                dressing /= total;
                total = 1f;
            }

            // What no class claims keeps the pack's colour, but its light, faintly coloured specks (the plaster's stains)
            // lose most of their tint; dark wood and the strong gold of the inner floor keep theirs.
            var grey = new Color(lum, lum, lum);
            var rest = Color.Lerp(c, grey, 0.65f * Rise(v, 0.35f, 0.5f) * (1f - Rise(s, 0.45f, 0.55f)));
            var o = (rest * (1f - total)) +(Mapped(Plaster, lum, PlasterRef, PlasterKeep) * plaster) + (Mapped(Lead, lum, LeadRef, LeadKeep) * lead) +
                    (Mapped(Slate, lum, SlateRef, SlateKeep) * slate) + (Mapped(Dressing, lum, DressingRef, DressingKeep) * dressing);
            o.a = 1f;
            return o;
        }

        private static Color Mapped(Color target, float lum, float reference, float keep)
        {
            var k = Mathf.Max(0f, 1f + (((lum / reference) - 1f) * keep));
            return new Color(Mathf.Clamp01(target.r * k), Mathf.Clamp01(target.g * k), Mathf.Clamp01(target.b * k));
        }

        /// <summary>1 within <paramref name="full"/> degrees of <paramref name="centre"/>, 0 beyond <paramref name="zero"/>, smooth between.</summary>
        private static float Near(float hue, float centre, float full, float zero)
        {
            var d = Mathf.Abs(Mathf.Repeat(hue - centre + 180f, 360f) - 180f);
            return 1f - Rise(d, full, zero);
        }

        private static float Rise(float x, float e0, float e1) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(e0, e1, x));
    }
}
