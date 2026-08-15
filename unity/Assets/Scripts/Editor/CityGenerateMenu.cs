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
        public const string CityScenePath = "Assets/Scenes/City_OrganicTown.unity";

        [MenuItem("GameDev/City/Open Or Create Town Scene")]
        public static void OpenOrCreateTownScene()
        {
            EnsureSceneFolder();
            if (System.IO.File.Exists(CityScenePath))
            {
                EditorSceneManager.OpenScene(CityScenePath, OpenSceneMode.Single);
            }
            else
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var root = new GameObject("City_Root");
                var city = root.AddComponent<CityGenerator>();
                city.SetSeed(WorldSeed.DefaultPrototypeSeed);
                city.Generate();
                EditorSceneManager.SaveScene(scene, CityScenePath);
                AddToBuildSettings(CityScenePath);
            }

            Debug.Log($"[City] Town scene: {CityScenePath}");
        }

        [MenuItem("GameDev/City/Generate From Seed")]
        public static void GenerateFromSeed()
        {
            EnsureTownSceneActive();

            var city = Object.FindAnyObjectByType<CityGenerator>();
            if (city == null)
            {
                var root = new GameObject("City_Root");
                city = root.AddComponent<CityGenerator>();
                city.SetSeed(WorldSeed.DefaultPrototypeSeed);
            }

            city.Generate();
            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CityScenePath);
            AddToBuildSettings(CityScenePath);

            Selection.activeGameObject = city.gameObject;
            Debug.Log($"[City] Generated seed={city.SeedValue} into {CityScenePath}.");
        }

        [MenuItem("GameDev/City/Generate From Seed (New Random)")]
        public static void GenerateRandomSeed()
        {
            EnsureTownSceneActive();

            var city = Object.FindAnyObjectByType<CityGenerator>();
            if (city == null)
            {
                var root = new GameObject("City_Root");
                city = root.AddComponent<CityGenerator>();
            }

            city.SetSeed(System.Environment.TickCount);
            city.Generate();
            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CityScenePath);
            AddToBuildSettings(CityScenePath);

            Selection.activeGameObject = city.gameObject;
            Debug.Log($"[City] Generated random seed={city.SeedValue} into {CityScenePath}.");
        }

        private static void EnsureTownSceneActive()
        {
            EnsureSceneFolder();
            var active = SceneManager.GetActiveScene();
            if (active.path == CityScenePath)
            {
                return;
            }

            if (System.IO.File.Exists(CityScenePath))
            {
                EditorSceneManager.OpenScene(CityScenePath, OpenSceneMode.Single);
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, CityScenePath);
        }

        private static void EnsureSceneFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
        }

        private static void AddToBuildSettings(string path)
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].path == path)
                {
                    list[i] = new EditorBuildSettingsScene(path, true);
                    EditorBuildSettings.scenes = list.ToArray();
                    return;
                }
            }

            list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
#endif
