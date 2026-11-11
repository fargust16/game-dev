using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameDev.Slice.Kit;
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
        private const float Texel = 0.5f, Margin = 2f;

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

        /// <summary>Every kit house's body: the box colliders on its root, which run from the ground to its wall's top.</summary>
        internal static IEnumerable<BoxCollider> Bodies() =>
            Object.FindObjectsByType<KitBlock>(FindObjectsSortMode.None)
                .Where(b => b.enabled && b.transform.Find("Model") != null)
                .SelectMany(b => b.GetComponents<BoxCollider>())
                .Where(c => c.enabled);

        internal static float TopOf(BoxCollider box) => box.transform.TransformPoint(box.center + (Vector3.up * (box.size.y / 2f))).y;
    }
}
