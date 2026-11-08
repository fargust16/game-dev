using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameDev.VictorianKit.EditorTools
{
    /// <summary>Where the Victorian Kit package keeps its models, prefabs and manifests, and where this project keeps its own.</summary>
    public static class VicKitPaths
    {
        public const string PackageRoot = "Packages/com.victorian.kit";
        public const string KitModelFolder = PackageRoot + "/Runtime/Models";
        public const string KitModuleFolder = PackageRoot + "/Runtime/Prefabs/Modules";
        public const string KitHouseFolder = PackageRoot + "/Runtime/Prefabs/Houses";
        public const string KitManifestFolder = PackageRoot + "/Runtime/Manifests";

        public const string ArtFolder = "Assets/Art/VictorianKit";
        public const string TextureFolder = ArtFolder + "/Textures";
        public const string MaterialFolder = ArtFolder + "/Materials";
        public const string HouseFolder = ArtFolder + "/Houses";
        public const string ShaderFolder = "Assets/Shaders/VictorianKit";
        public const string SceneFolder = "Assets/Scenes/VictorianKit";
        public const string StreetScene = SceneFolder + "/VictorianStreets.unity";

        /// <summary>Renders and reports, outside Assets.</summary>
        public const string LogFolder = "Logs/VictorianKit";

        /// <summary>The ids of every module the package ships, from its FBX names.</summary>
        public static IEnumerable<string> ModuleIds() =>
            AssetDatabase.FindAssets(string.Empty, new[] { KitModelFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                .Select(Path.GetFileNameWithoutExtension)
                .Distinct();

        public static GameObject LoadModule(string id) => AssetDatabase.LoadAssetAtPath<GameObject>(KitModuleFolder + "/" + id + ".prefab");

        /// <summary>Runs the package's own Victorian Kit / Build Prefabs when a module prefab is missing.</summary>
        public static void EnsureKitPrefabs()
        {
            var missing = ModuleIds().Any(id => LoadModule(id) == null);
            if (!missing)
            {
                return;
            }

            if (!EditorApplication.ExecuteMenuItem("Victorian Kit/Build Prefabs"))
            {
                throw new System.InvalidOperationException("The menu Victorian Kit / Build Prefabs is missing: is com.victorian.kit installed?");
            }
        }
    }
}
