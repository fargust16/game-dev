#if UNITY_EDITOR
using GameDev.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDev.EditorTools
{
    public static class CityGenerateMenu
    {
        private const string ScenePath = "Assets/Scenes/S0_LowerVeil.unity";

        [MenuItem("GameDev/City/Generate From Seed")]
        public static void GenerateFromSeed()
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
            if (string.IsNullOrEmpty(scene.path))
            {
                EnsureSceneFolder();
                EditorSceneManager.SaveScene(scene, ScenePath);
                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(ScenePath, true)
                };
            }
            else
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Selection.activeGameObject = city.gameObject;
            Debug.Log($"[City] Generated from seed {city.SeedValue}. Menu: GameDev → City → Generate From Seed.");
        }

        [MenuItem("GameDev/City/Generate From Seed (New Random)")]
        public static void GenerateRandomSeed()
        {
            var city = Object.FindFirstObjectByType<CityGenerator>();
            if (city == null)
            {
                var root = new GameObject("City_Root");
                city = root.AddComponent<CityGenerator>();
            }

            city.SetSeed(System.Environment.TickCount);
            GenerateFromSeed();
        }

        private static void EnsureSceneFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
        }
    }
}
#endif
