using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The CC0 models from Blender's asset libraries (owner, 2026-11-11: rileyb3d, Charge, Poly Haven, ambientCG; every
    /// asset CC0-1.0). <c>tools/blender/assets/cc0_export.py</c> writes each into <c>Assets/Art/CC0/&lt;Name&gt;/</c>: one
    /// FBX in metres on its foot, and for each of its materials a base map, a tangent normal map and a URP metallic map
    /// (R metallic, A smoothness), with a JSON note. This gives each material a URP Lit material of the project's beside
    /// it and binds the FBX's material of the same name to it, so the kit wraps the FBX itself (family <c>Cc0</c>,
    /// <see cref="SliceKitCatalogue"/>). Texture and model settings are set on import.
    /// Batch: -executeMethod GameDev.Slice.EditorTools.SliceCc0Import.Run
    /// </summary>
    public sealed class SliceCc0Import : AssetPostprocessor
    {
        public const string Root = "Assets/Art/CC0";

        [Serializable]
        public sealed class MaterialNote
        {
            public string material;
            public bool alpha_cut;
            public float metallic_mean;
            public float smoothness_mean;
            public string @base;
            public string normal;
            public string metal;
        }

        [Serializable]
        public sealed class Note
        {
            public string name;
            public string asset;
            public string source;
            public string license;
            public int triangles_source;
            public int triangles;
            public float[] size_m;
            public int texture;
            public MaterialNote[] materials;
        }

        public static Note Read(string folder)
        {
            var path = Path.Combine(folder, Path.GetFileName(folder) + ".json");
            return File.Exists(path) ? JsonUtility.FromJson<Note>(File.ReadAllText(path)) : null;
        }

        /// <summary>Every CC0 model's folder.</summary>
        public static IEnumerable<string> Folders() =>
            Directory.Exists(Root)
                ? Directory.GetDirectories(Root).Select(d => d.Replace('\\', '/')).Where(d => File.Exists($"{d}/{Path.GetFileName(d)}.json"))
                : Enumerable.Empty<string>();

        public static void Run()
        {
            AssetDatabase.Refresh();
            var lines = new List<string>();
            foreach (var folder in Folders())
            {
                lines.Add(Build(folder));
            }

            lines.Add(SnowLayer());
            AssetDatabase.SaveAssets();
            Debug.Log("[SliceCc0Import]\n" + string.Join("\n", lines));
        }

        /// <summary>One model's materials made or refreshed, and its FBX bound to them.</summary>
        public static string Build(string folder)
        {
            var note = Read(folder);
            var name = Path.GetFileName(folder);
            var fbx = $"{folder}/{name}.fbx";
            if (note == null || AssetImporter.GetAtPath(fbx) is not ModelImporter importer)
            {
                return $"{name}: NO NOTE OR FBX";
            }

            foreach (var m in note.materials)
            {
                MakeMaterial(folder, m);
            }

            Bind(importer, folder, note);
            importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            var bounds = model.GetComponentsInChildren<MeshFilter>().Where(f => f.sharedMesh != null)
                .Select(f => f.sharedMesh.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            return $"{name}: {note.asset} from {note.source} ({note.license}), {note.triangles} triangles, {note.materials.Length} materials; " +
                   $"seen {bounds.size.x:0.00} × {bounds.size.z:0.00} × {bounds.size.y:0.00} m (Blender's {string.Join(" × ", note.size_m.Select(v => v.ToString("0.00")))})";
        }

        /// <summary>The snow terrain layer for the far mountains (city plan §26.2 12a), from ambientCG's Snow010A.</summary>
        public const string SnowLayerPath = Root + "/Snow010A/Snow010A.terrainlayer";

        /// <summary>
        /// Makes the snow's terrain layer once, when it is missing: its colour and normal map and a 4 m tile (authoring
        /// default). Never written again: a terrain layer carries its Inspector tuning (AGENTS.md, terrain guardrails), and
        /// the land pass that paints the far mountains is still to come.
        /// </summary>
        private static string SnowLayer()
        {
            if (AssetDatabase.LoadAssetAtPath<TerrainLayer>(SnowLayerPath) != null)
            {
                return "Snow010A: its terrain layer is there and left as it is";
            }

            var folder = Path.GetDirectoryName(SnowLayerPath)?.Replace('\\', '/');
            var colour = AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/Snow010A_base.jpg");
            if (colour == null)
            {
                return "Snow010A: NO TEXTURES";
            }

            var layer = new TerrainLayer
            {
                diffuseTexture = colour,
                normalMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/Snow010A_normal.jpg"),
                tileSize = new Vector2(4f, 4f),
            };
            AssetDatabase.CreateAsset(layer, SnowLayerPath);
            return "Snow010A: its terrain layer made, a 4 m tile, for the far mountains' snow (12a)";
        }

        private static void Bind(ModelImporter importer, string folder, Note note)
        {
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            foreach (var m in note.materials)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>($"{folder}/{m.material}.mat");
                if (mat != null)
                {
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), m.material), mat);
                }
            }
        }

        private static Material MakeMaterial(string folder, MaterialNote m)
        {
            var path = $"{folder}/{m.material}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = m.material };
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetColor("_BaseColor", Color.white);
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/{m.@base}"));
            mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/{m.normal}"));
            mat.SetFloat("_BumpScale", 1f);
            mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/{m.metal}"));
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.SetFloat("_Smoothness", 1f); // the map's alpha is the smoothness itself
            if (m.alpha_cut)
            {
                mat.SetFloat("_AlphaClip", 1f);
                mat.SetFloat("_Cutoff", 0.5f);
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.SetFloat("_Cull", (float)CullMode.Off); // a cut-out is a card or a thin leaf, seen from both sides
                mat.renderQueue = (int)RenderQueue.AlphaTest;
            }
            else
            {
                mat.SetFloat("_AlphaClip", 0f);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.SetFloat("_Cull", (float)CullMode.Back);
                mat.renderQueue = -1;
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root + "/"))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            var file = Path.GetFileNameWithoutExtension(assetPath);
            var normal = file.EndsWith("_normal");
            var colour = file.EndsWith("_base");
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = colour;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.alphaSource = colour || file.EndsWith("_metal") ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = colour;
            importer.mipMapsPreserveCoverage = colour;
            importer.alphaTestReferenceValue = 0.5f;
            var note = Read(Path.GetDirectoryName(assetPath)?.Replace('\\', '/'));
            importer.maxTextureSize = note != null && note.texture > 0 ? note.texture : 1024;
        }

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Root + "/"))
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.addCollider = false;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            var folder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            var note = Read(folder);
            if (note != null)
            {
                Bind(importer, folder, note);
            }
        }
    }
}
