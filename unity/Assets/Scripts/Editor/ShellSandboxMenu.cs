#if UNITY_EDITOR
using System.IO;
using GameDev.World.Buildings.Shell;
using GameDev.World.Settlement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDev.EditorTools
{
    public static class ShellSandboxMenu
    {
        public const string ScenePath = "Assets/Scenes/Building_Shell_Sandbox.unity";

        [MenuItem("GameDev/Buildings/Shell Sandbox/Open Or Create Scene")]
        public static void OpenOrCreateScene()
        {
            EnsureSceneFolder();
            if (File.Exists(ScenePath))
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            else
                CreateFreshScene(42);
            Debug.Log($"[ShellSandbox] Scene: {ScenePath}");
        }

        [MenuItem("GameDev/Buildings/Shell Sandbox/Generate Active District")]
        public static void Generate()
        {
            var lab = EnsureLab();
            lab.Generate();
            SaveActive();
            Selection.activeGameObject = lab.gameObject;
        }

        [MenuItem("GameDev/Buildings/Shell Sandbox/Generate/Poor")]
        public static void GeneratePoor() => GenerateDistrict(DistrictKind.Poor);

        [MenuItem("GameDev/Buildings/Shell Sandbox/Generate/Worker")]
        public static void GenerateWorker() => GenerateDistrict(DistrictKind.Worker);

        [MenuItem("GameDev/Buildings/Shell Sandbox/Generate/Affluent")]
        public static void GenerateAffluent() => GenerateDistrict(DistrictKind.Affluent);

        [MenuItem("GameDev/Buildings/Shell Sandbox/Generate/Rich")]
        public static void GenerateRich() => GenerateDistrict(DistrictKind.Rich);

        [MenuItem("GameDev/Buildings/Shell Sandbox/Generate/Elite")]
        public static void GenerateElite() => GenerateDistrict(DistrictKind.Elite);

        [MenuItem("GameDev/Buildings/Shell Sandbox/Generate/Commercial")]
        public static void GenerateCommercial() => GenerateDistrict(DistrictKind.Commercial);

        [MenuItem("GameDev/Buildings/Shell Sandbox/Generate/Compare All Districts")]
        public static void GenerateCompareAll()
        {
            var lab = EnsureLab();
            lab.SetCompareAll();
            lab.Generate();
            SaveActive();
            Selection.activeGameObject = lab.gameObject;
            Debug.Log("[ShellSandbox] Compare All: Poor / Worker / Commercial / Affluent / Rich / Elite");
        }

        [MenuItem("GameDev/Buildings/Shell Sandbox/Clear")]
        public static void Clear()
        {
            var lab = Object.FindAnyObjectByType<ShellSandboxLab>();
            if (lab == null)
            {
                Debug.LogWarning("[ShellSandbox] No ShellSandboxLab in scene.");
                return;
            }

            lab.Clear();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        [MenuItem("GameDev/Buildings/Shell Sandbox/Reseed And Generate")]
        public static void ReseedAndGenerate()
        {
            var lab = EnsureLab();
            lab.ReseedAndGenerate();
            SaveActive();
            Selection.activeGameObject = lab.gameObject;
        }

        [MenuItem("GameDev/Buildings/Shell Sandbox/Bind Settlement Stub Demo")]
        public static void BindSettlementStubDemo()
        {
            var lab = EnsureLab();
            lab.Clear();
            var archetypes = ShellCatalogFactory.CreateDefaultArchetypes();
            var districts = ShellCatalogFactory.CreateDefaultDistricts();
            var binder = new BuildingShellBinder();
            var bridge = new SettlementShellBridge(binder, archetypes, districts);
            var layout = SettlementLayoutStub.CreateDemo(lab.Seed, 10);
            var root = bridge.SpawnAll(layout, lab.Seed, lab.transform);
            root.name = "SettlementStubRoot";
            SaveActive();
            Selection.activeGameObject = root;
            Debug.Log($"[ShellSandbox] Settlement stub bound {layout.Plots.Count} plots → shells.");
        }

        [MenuItem("GameDev/Buildings/Shell Sandbox/Bind Settlement Property Block Demo")]
        public static void BindSettlementPropertyBlockDemo()
        {
            SettlementPropertyMenu.GenerateBlockProperties();
        }

        private static void GenerateDistrict(DistrictKind kind)
        {
            var lab = EnsureLab();
            lab.SetDistrict(kind);
            lab.Generate();
            SaveActive();
            Selection.activeGameObject = lab.gameObject;
        }

        private static ShellSandboxLab EnsureLab()
        {
            EnsureSceneFolder();
            var active = SceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (File.Exists(ScenePath))
                    EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                else
                    CreateFreshScene(42);
            }

            var lab = Object.FindAnyObjectByType<ShellSandboxLab>();
            if (lab != null)
                return lab;

            var go = new GameObject("ShellSandboxLab");
            lab = go.AddComponent<ShellSandboxLab>();
            lab.SetSeed(42);
            return lab;
        }

        private static void CreateFreshScene(int seed)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("ShellSandboxLab");
            var lab = go.AddComponent<ShellSandboxLab>();
            lab.SetSeed(seed);
            lab.SetDistrict(DistrictKind.Worker);
            lab.Generate();

            var light = new GameObject("Directional Light");
            var l = light.AddComponent<Light>();
            l.type = LightType.Directional;
            l.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            cam.AddComponent<Camera>();
            cam.transform.position = new Vector3(20f, 18f, -22f);
            cam.transform.LookAt(new Vector3(20f, 2f, 2f));

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void SaveActive()
        {
            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            if (string.IsNullOrEmpty(scene.path))
                EditorSceneManager.SaveScene(scene, ScenePath);
            else
                EditorSceneManager.SaveScene(scene);
        }

        private static void EnsureSceneFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
        }
    }
}
#endif
