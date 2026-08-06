#if UNITY_EDITOR
using GameDev.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDev.EditorTools
{
    public static class S0GreyboxMenu
    {
        [MenuItem("GameDev/S0/Create Lower Veil Greybox Scene")]
        public static void CreateLowerVeilScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("S0_LowerVeil");
            var builder = root.AddComponent<LowerVeilGreyboxBuilder>();
            builder.Build();

            const string dir = "Assets/Scenes";
            if (!AssetDatabase.IsValidFolder(dir))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            const string path = "Assets/Scenes/S0_LowerVeil.unity";
            EditorSceneManager.SaveScene(scene, path);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(path, true)
            };

            Selection.activeGameObject = root;
            Debug.Log($"[S0] Scene saved to {path}. Press Play. WASD/Arrows move, E interacts.");
        }

        [MenuItem("GameDev/S0/Rebuild Greybox In Open Scene")]
        public static void RebuildInOpenScene()
        {
            var builder = Object.FindFirstObjectByType<LowerVeilGreyboxBuilder>();
            if (builder == null)
            {
                var root = new GameObject("S0_LowerVeil");
                builder = root.AddComponent<LowerVeilGreyboxBuilder>();
            }

            builder.Build();
            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[S0] Greybox rebuilt and saved. Press Play — WASD moves on grid, E interacts near tavern/POIs.");
        }
    }
}
#endif
