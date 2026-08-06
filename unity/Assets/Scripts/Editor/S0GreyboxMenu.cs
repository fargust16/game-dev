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
            var root = new GameObject("City_Root");
            var city = root.AddComponent<CityGenerator>();
            city.SetSeed(WorldSeed.DefaultPrototypeSeed);
            city.Generate();

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
            Debug.Log($"[City] Scene saved to {path}. Seed={city.SeedValue}. Prefer GameDev/City/Generate From Seed.");
        }

        [MenuItem("GameDev/S0/Rebuild Greybox In Open Scene")]
        public static void RebuildInOpenScene()
        {
            var city = Object.FindFirstObjectByType<CityGenerator>();
            if (city == null)
            {
                var root = new GameObject("City_Root");
                city = root.AddComponent<CityGenerator>();
                city.SetSeed(WorldSeed.DefaultPrototypeSeed);
            }

            city.Generate();
            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[City] Rebuilt seed={city.SeedValue}. Press Play — click A* / WASD / E still work.");
        }
    }
}
#endif
