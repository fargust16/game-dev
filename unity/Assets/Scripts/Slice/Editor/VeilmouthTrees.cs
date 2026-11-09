using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Veilmouth's trees made in Blender from Easy Tree (VEILMOUTH_CITY_PLAN.md §27; tools/blender/veilmouth_trees.py):
    /// how their FBX and textures are imported, and their three materials on GameDev/Slice/Tree. An FBX's material
    /// names (VeilmouthBark, VeilmouthLeaves_&lt;kind&gt;) pick the project's materials; its _LOD0 and _LOD1 objects become
    /// a LOD Group. Every number is an authoring default. Batch:
    /// -executeMethod GameDev.Slice.EditorTools.VeilmouthTrees.Setup
    /// </summary>
    public sealed class VeilmouthTrees : AssetPostprocessor
    {
        public const string ArtFolder = "Assets/Art/Trees";
        public const string MaterialFolder = SliceKitCatalogue.MaterialFolder + "/Trees";
        private const string Textures = ArtFolder + "/Textures";

        /// <summary>The trees tools/blender/veilmouth_trees.py makes: thin street trees of 8–9 m, and garden trees of 9.6–10 m.</summary>
        public static readonly string[] Street = { "StreetHornbeamA", "StreetHornbeamB", "StreetBeechA", "StreetBeechB" };

        public static readonly string[] Garden = { "GardenBeechA", "GardenHornbeamB" };

        // A street tree is LOD0 while it fills over 18 % of the screen's height (about 45 m away for 9 m at 60°),
        // LOD1 down to 2.5 % (about 310 m), and is not drawn beyond, which the haze already hides.
        private const float Lod0Until = 0.18f, Lod1Until = 0.025f;

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

        /// <summary>The materials made or brought up to date, then every tree imported again so it wears them.</summary>
        public static void Setup()
        {
            CreateMaterials();
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { ArtFolder }))
            {
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
            }

            Debug.Log($"[VeilmouthTrees] materials under {MaterialFolder}; the trees under {ArtFolder} imported again.");
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
                var leaves = MaterialAt(LeavesMaterial(kind));
                leaves.SetTexture("_BaseMap", TextureAt($"Tree_Twig_{kind}.png"));
                leaves.SetTexture("_BumpMap", TextureAt($"Tree_Twig_{kind}_Normal.png"));
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

            AssetDatabase.SaveAssets();
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

            var lod0 = root.GetComponentsInChildren<Renderer>(true).Where(r => r.name.EndsWith("_LOD0")).ToArray();
            var lod1 = root.GetComponentsInChildren<Renderer>(true).Where(r => r.name.EndsWith("_LOD1")).ToArray();
            group.SetLODs(new[] { new LOD(Lod0Until, lod0), new LOD(Lod1Until, lod1) });
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
                // A twig card: its colour spread under the cut-out, and its leaves' coverage kept at every mip so a
                // far crown does not thin out.
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.mipMapsPreserveCoverage = true;
                importer.alphaTestReferenceValue = 0.5f;
            }
        }
    }
}
