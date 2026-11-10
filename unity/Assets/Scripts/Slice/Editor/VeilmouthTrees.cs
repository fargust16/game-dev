using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Veilmouth's trees made in Blender from Easy Tree (VEILMOUTH_CITY_PLAN.md §27; tools/blender/veilmouth_trees.py):
    /// how their FBX and textures are imported, and their materials on GameDev/Slice/Tree. An FBX's material names
    /// (VeilmouthBark, VeilmouthLeaves_&lt;kind&gt; for cards, VeilmouthLeaf_&lt;kind&gt; for single leaves) pick the
    /// project's materials; its _LOD0, _LOD1 (and _LOD2, _LOD3) objects become a LOD Group. Every number is an authoring
    /// default. Batch:
    /// -executeMethod GameDev.Slice.EditorTools.VeilmouthTrees.Setup
    /// </summary>
    public sealed class VeilmouthTrees : AssetPostprocessor
    {
        public const string ArtFolder = "Assets/Art/Trees";
        public const string MaterialFolder = SliceKitCatalogue.MaterialFolder + "/Trees";
        private const string Textures = ArtFolder + "/Textures";

        /// <summary>The trees tools/blender/veilmouth_trees.py makes: thin street trees of 8–9 m, garden trees of 9.6–10 m, and the mature tree.</summary>
        public static readonly string[] Street = { "StreetHornbeamA", "StreetHornbeamB", "StreetBeechA", "StreetBeechB" };

        public static readonly string[] Garden = { "GardenBeechA", "GardenHornbeamB" };

        /// <summary>The owner's mature tree, 11.4 m high and 12.7 × 11.6 m across: Easy Tree's own tree from the owner's values, with our leaves (§27.7–§28.3).</summary>
        public static readonly string[] Mature = { "MatureHornbeamA" };

        /// <summary>The kinds whose single leaves (the leaf atlas itself) a tree wears: the mature tree's hornbeam (§27.8).</summary>
        private static readonly string[] SingleLeaves = { "Hornbeam" };

        // A tree of two LODs (street and garden) is LOD0 while it fills over 18 % of the screen's height (about 45 m away
        // for 9 m at 60°), LOD1 down to 2.5 % (about 310 m), and is not drawn beyond, which the haze already hides. A
        // tree of three is LOD0 to 25 % (about 38 m for 11 m), LOD1 to 7 % (about 135 m) and LOD2 to 2.5 % (about
        // 380 m). The mature tree has four (§27.9; 162.4, 94.8, 5.7 and 2.3 thousand triangles): Easy Tree's bark with
        // its twigs only near, to 80 % (about 13.5 m for its LOD Group's 12.5 m, where a twig of 4 mm is under a third of
        // a pixel), then the same without twigs to 25 % (about 43 m), then the thinned leaves.
        private static readonly float[] TwoLods = { 0.18f, 0.025f }, ThreeLods = { 0.25f, 0.07f, 0.025f };
        private static readonly float[] FourLods = { 0.8f, 0.25f, 0.07f, 0.025f };

        // The wind, the same on bark and leaves so a twig never leaves its branch: a lean of 0.22 m at the top that
        // breathes about every 7 s (2π / 0.9), gusts travelling along the wind as waves about 105 m long (2π / 0.06),
        // leaves fluttering 3.5 cm.
        private static readonly Vector4 WindDirection = new Vector4(1f, 0f, 0.35f, 0f);
        private const float WindStrength = 0.22f, WindSpeed = 0.9f, GustScale = 0.06f, Flutter = 0.035f, FlutterSpeed = 7f;

        /// <summary>
        /// The leaves' tint over the twig cards' Easy Tree green, toward the city's muted green (§25.1: a crown's middle
        /// about sRGB 0.30, 0.37, 0.20), and how much sun shows through them.
        /// </summary>
        private static readonly (string Kind, Color Tint)[] Leaves =
        {
            ("Beech", new Color(0.70f, 0.64f, 0.58f)), ("Hornbeam", new Color(0.66f, 0.62f, 0.56f)),
        };

        private const float LeafTranslucency = 0.55f;

        public static string BarkMaterial => $"{MaterialFolder}/VeilmouthBark.mat";

        public static string LeavesMaterial(string kind) => $"{MaterialFolder}/VeilmouthLeaves_{kind}.mat";

        public static string LeafMaterial(string kind) => $"{MaterialFolder}/VeilmouthLeaf_{kind}.mat";

        /// <summary>The materials made or brought up to date, then every tree imported again so it wears them.</summary>
        public static void Setup()
        {
            CreateMaterials();
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { ArtFolder }))
            {
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
            }

            // A tree new to the kit gets its wrapper (a wrapper already on disk is kept as it is).
            SliceKitCatalogue.Load();
            Debug.Log($"[VeilmouthTrees] materials under {MaterialFolder}; the trees under {ArtFolder} imported again.");
        }

        /// <summary>
        /// <see cref="Setup"/>, then the mature tree's wrapper built again in place with its GUID, as after the tree is grown
        /// again; logs each LOD's triangles as imported. Batch:
        /// -executeMethod GameDev.Slice.EditorTools.VeilmouthTrees.SetupMature
        /// </summary>
        public static void SetupMature()
        {
            Setup();
            SliceKitCatalogue.LoadRebuilding(Mature.Select(m => "CityTree_" + m).ToArray());
            foreach (var name in Mature)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArtFolder}/Tree_{name}.fbx");
                var tris = model.GetComponentsInChildren<MeshFilter>(true).OrderBy(m => m.name)
                    .Select(m => $"{m.name} {m.sharedMesh.triangles.Length / 3}");
                Debug.Log($"[VeilmouthTrees] {name}: {string.Join(", ", tris)}");
            }
        }

        [MenuItem("GameDev/Slice/Trees/Create Materials")]
        public static void CreateMaterials()
        {
            Directory.CreateDirectory(MaterialFolder);
            var bark = MaterialAt(BarkMaterial);
            bark.SetTexture("_BaseMap", TextureAt("Tree_Bark.jpg"));
            bark.SetTexture("_BumpMap", TextureAt("Tree_Bark_Normal.jpg"));
            bark.SetColor("_BaseColor", new Color(0.86f, 0.84f, 0.8f));
            bark.SetFloat("_Smoothness", 0.08f);
            bark.SetFloat("_Cull", (float)CullMode.Back);
            bark.SetFloat("_AlphaClip", 0f);
            bark.DisableKeyword("_ALPHATEST_ON");
            bark.SetFloat("_Translucency", 0f);
            bark.renderQueue = (int)RenderQueue.Geometry;
            Wind(bark);

            foreach (var (kind, tint) in Leaves)
            {
                Foliage(MaterialAt(LeavesMaterial(kind)), $"Tree_Twig_{kind}", tint);
                if (SingleLeaves.Contains(kind))
                {
                    Foliage(MaterialAt(LeafMaterial(kind)), $"Tree_Leaf_{kind}", tint);
                }
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>A leaves material: cut out, both sides, the kind's tint and the sun through it, with the wind.</summary>
        private static void Foliage(Material leaves, string texture, Color tint)
        {
            leaves.SetTexture("_BaseMap", TextureAt($"{texture}.png"));
            leaves.SetTexture("_BumpMap", TextureAt($"{texture}_Normal.png"));
            leaves.SetColor("_BaseColor", tint);
            leaves.SetFloat("_Smoothness", 0.2f);
            leaves.SetFloat("_Cull", (float)CullMode.Off);
            leaves.SetFloat("_AlphaClip", 1f);
            leaves.SetFloat("_Cutoff", 0.5f);
            leaves.EnableKeyword("_ALPHATEST_ON");
            leaves.SetFloat("_Translucency", LeafTranslucency);
            leaves.renderQueue = (int)RenderQueue.AlphaTest;
            Wind(leaves);
        }

        private static void Wind(Material material)
        {
            material.SetVector("_WindDirection", WindDirection);
            material.SetFloat("_WindStrength", WindStrength);
            material.SetFloat("_WindSpeed", WindSpeed);
            material.SetFloat("_GustScale", GustScale);
            material.SetFloat("_Flutter", Flutter);
            material.SetFloat("_FlutterSpeed", FlutterSpeed);
            EditorUtility.SetDirty(material);
        }

        private static Material MaterialAt(string path)
        {
            var shader = Shader.Find("GameDev/Slice/Tree");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.enableInstancing = true;
            return material;
        }

        private static Texture2D TextureAt(string file)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Textures}/{file}");
            if (texture == null)
            {
                throw new FileNotFoundException($"[VeilmouthTrees] {Textures}/{file} is missing; run tools/blender/veilmouth_trees.py");
            }

            return texture;
        }

        // ------------------------------------------------------------------------------------------- import

        private bool IsTree => assetPath.StartsWith(ArtFolder + "/");

        private void OnPreprocessModel()
        {
            if (!IsTree)
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importNormals = ModelImporterNormals.Import; // the crown's normals, set in Blender
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.addCollider = false;
        }

        private void OnPostprocessModel(GameObject root)
        {
            if (!IsTree)
            {
                return;
            }

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(Project).ToArray();
                renderer.shadowCastingMode = ShadowCastingMode.On;
            }

            var group = root.GetComponent<LODGroup>();
            if (group == null)
            {
                return;
            }

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var levels = Enumerable.Range(0, FourLods.Length)
                .Select(k => renderers.Where(r => r.name.EndsWith($"_LOD{k}")).ToArray())
                .TakeWhile(level => level.Length > 0)
                .ToArray();
            if (levels.Length < 2)
            {
                Debug.LogWarning($"[VeilmouthTrees] {assetPath}: {levels.Length} LOD object(s); its LOD Group is left as imported.");
                return;
            }

            var until = new[] { TwoLods, ThreeLods, FourLods }[levels.Length - 2];
            group.SetLODs(levels.Select((level, k) => new LOD(until[k], level)).ToArray());
            group.fadeMode = LODFadeMode.CrossFade;
            group.animateCrossFading = true;
        }

        /// <summary>The project's material for an FBX material of the same name, where it exists yet.</summary>
        private static Material Project(Material imported)
        {
            if (imported == null)
            {
                return null;
            }

            var name = imported.name.Replace(" (Instance)", "");
            var path = $"{MaterialFolder}/{name}.mat";
            return AssetDatabase.LoadAssetAtPath<Material>(path) ?? imported;
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Textures + "/"))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            var normal = Path.GetFileNameWithoutExtension(assetPath).EndsWith("_Normal");
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normal;
            importer.maxTextureSize = 1024;
            importer.mipmapEnabled = true;
            importer.wrapMode = Path.GetFileName(assetPath).StartsWith("Tree_Bark") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            if (!normal && assetPath.EndsWith(".png"))
            {
                // A twig card or the leaf atlas: its colour spread under the cut-out, and its leaves' coverage kept at
                // every mip so a far crown does not thin out.
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.mipMapsPreserveCoverage = true;
                importer.alphaTestReferenceValue = 0.5f;
            }
        }
    }
}
