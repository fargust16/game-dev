#if UNITY_EDITOR
using System.IO;
using GameDev.World;
using GameDev.World.Town;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDev.EditorTools
{
    public static class TownGenerateMenu
    {
        public const string TownScenePath = "Assets/Scenes/Town_Generator.unity";
        private const string DefaultProfilePath = "Assets/Settings/DefaultTownProfile.asset";

        [MenuItem("GameDev/Town/Open Or Create Town Scene")]
        public static void OpenOrCreateTownScene()
        {
            EnsureSceneFolder();
            if (System.IO.File.Exists(TownScenePath))
            {
                EditorSceneManager.OpenScene(TownScenePath, OpenSceneMode.Single);
            }
            else
            {
                CreateFreshSceneAndGenerate(WorldSeed.DefaultPrototypeSeed);
            }

            Debug.Log($"[Town] Scene: {TownScenePath}");
        }

        [MenuItem("GameDev/Town/Generate From Seed")]
        public static void GenerateFromSeed()
        {
            EnsureTownSceneActive();
            var gen = FindOrCreateGenerator();
            gen.SetSeed(WorldSeed.DefaultPrototypeSeed);
            gen.Generate(gen.Seed);
            SaveActive();
            Selection.activeGameObject = gen.gameObject;
        }

        [MenuItem("GameDev/Town/Generate Random Seed")]
        public static void GenerateRandomSeed()
        {
            EnsureTownSceneActive();
            var gen = FindOrCreateGenerator();
            gen.SetSeed(System.Environment.TickCount);
            gen.Generate(gen.Seed);
            SaveActive();
            Selection.activeGameObject = gen.gameObject;
        }

        [MenuItem("GameDev/Town/Export Debug Report")]
        public static void ExportDebugReport()
        {
            EnsureTownSceneActive();
            var gen = FindOrCreateGenerator();
            if (gen.Model == null || gen.Model.Bounds == null || gen.Model.Bounds.Count < 3)
            {
                gen.Generate(gen.Seed);
            }

            ExportDebugReportCore(gen.Model, gen.Seed, reveal: true);
        }

        /// <summary>Batchmode entry: Unity -executeMethod GameDev.EditorTools.TownGenerateMenu.ExportDebugReportBatch</summary>
        public static void ExportDebugReportBatch()
        {
            var profile = AssetDatabase.LoadAssetAtPath<TownProfile>(DefaultProfilePath);
            var model = new TownModel();
            var seed = WorldSeed.DefaultPrototypeSeed;
            model.Reset(seed);
            var ws = new WorldSeed(seed);
            var log = new System.Collections.Generic.List<string>();
            Stage_SiteAndLandmark.Run(model, profile != null ? profile : ScriptableObject.CreateInstance<TownProfile>(), ws.Fork("site"));
            Stage_PrimaryRoads.Run(model, profile != null ? profile : ScriptableObject.CreateInstance<TownProfile>(), ws.Fork("primaryRoads"));
            var p = profile != null ? profile : ScriptableObject.CreateInstance<TownProfile>();
            Stage_Districts.Run(model, p, ws.Fork("districts"));
            Stage_Streets.Run(model, p, ws.Fork("streets"));
            Stage_Plots.Run(model, p, ws.Fork("plots"));
            Stage_Buildings.Run(model, p, ws.Fork("buildings"));
            Stage_OpenSpaces.Run(model, p, ws.Fork("openSpaces"));
            Stage_ConnectivityAndScale.Run(model, p, ws.Fork("connectivity"), log);
            ExportDebugReportCore(model, seed, reveal: false);
        }

        private static void ExportDebugReportCore(TownModel model, int seed, bool reveal)
        {
            var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "Plans/debug"));
            var result = TownDebugReport.Write(model, dir, $"town_seed_{seed}");
            AssetDatabase.Refresh();
            Debug.Log(
                $"[TownDebug] wrote {result.MarkdownPath} and {result.SvgPath}. " +
                $"road-in-buildings={result.RoadSamplesInsideBuildings}/{result.RoadSamples}");
            for (var i = 0; i < result.Findings.Count; i++)
            {
                Debug.Log($"[TownDebug] {result.Findings[i]}");
            }

            if (reveal)
            {
                EditorUtility.RevealInFinder(result.MarkdownPath);
            }
        }

        [MenuItem("GameDev/Town/Bake Greybox")]
        public static void BakeGreybox()
        {
            EnsureTownSceneActive();
            var gen = FindOrCreateGenerator();
            if (gen.Model == null || gen.Model.Bounds == null || gen.Model.Bounds.Count < 3)
            {
                gen.Generate(gen.Seed);
            }
            else
            {
                gen.BakeGreyboxNow();
            }

            SaveActive();
            Selection.activeGameObject = gen.gameObject;
        }

        [MenuItem("GameDev/Town/Clear")]
        public static void ClearTown()
        {
            var gen = Object.FindFirstObjectByType<TownGenerator>();
            if (gen == null)
            {
                Debug.LogWarning("[Town] No TownGenerator in scene.");
                return;
            }

            gen.ClearModel();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private static void CreateFreshSceneAndGenerate(int seed)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("TownGenerator");
            var gen = root.AddComponent<TownGenerator>();
            var profile = AssetDatabase.LoadAssetAtPath<TownProfile>(DefaultProfilePath);
            gen.SetProfile(profile);
            gen.SetSeed(seed);
            gen.Generate(seed);
            EditorSceneManager.SaveScene(scene, TownScenePath);
            AddToBuildSettings(TownScenePath);
        }

        private static void EnsureTownSceneActive()
        {
            EnsureSceneFolder();
            var active = SceneManager.GetActiveScene();
            if (active.path == TownScenePath)
            {
                return;
            }

            if (System.IO.File.Exists(TownScenePath))
            {
                EditorSceneManager.OpenScene(TownScenePath, OpenSceneMode.Single);
                return;
            }

            CreateFreshSceneAndGenerate(WorldSeed.DefaultPrototypeSeed);
        }

        private static TownGenerator FindOrCreateGenerator()
        {
            var gen = Object.FindFirstObjectByType<TownGenerator>();
            if (gen != null)
            {
                if (gen.Profile == null)
                {
                    gen.SetProfile(AssetDatabase.LoadAssetAtPath<TownProfile>(DefaultProfilePath));
                }

                return gen;
            }

            var root = new GameObject("TownGenerator");
            gen = root.AddComponent<TownGenerator>();
            gen.SetProfile(AssetDatabase.LoadAssetAtPath<TownProfile>(DefaultProfilePath));
            return gen;
        }

        private static void SaveActive()
        {
            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            if (string.IsNullOrEmpty(scene.path))
            {
                EditorSceneManager.SaveScene(scene, TownScenePath);
            }
            else
            {
                EditorSceneManager.SaveScene(scene);
            }

            AddToBuildSettings(TownScenePath);
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
