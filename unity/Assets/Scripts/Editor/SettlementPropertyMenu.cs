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
    public static class SettlementPropertyMenu
    {
        public const string ScenePath = "Assets/Scenes/Building_Shell_Sandbox.unity";

        [MenuItem("GameDev/Settlement/Open Property Lab Scene")]
        public static void OpenLabScene()
        {
            EnsureSceneFolder();
            if (File.Exists(ScenePath))
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            else
                ShellSandboxMenu.OpenOrCreateScene();

            EnsureLab();
            Debug.Log($"[SettlementProperty] Lab scene: {ScenePath}");
        }

        [MenuItem("GameDev/Settlement/Generate Block Properties")]
        public static void GenerateBlockProperties()
        {
            var lab = EnsureLab();
            lab.Generate();
            SaveActive();
            Selection.activeGameObject = lab.gameObject;
            var count = lab.Layout != null ? lab.Layout.Properties.Count : 0;
            Debug.Log(
                $"[SettlementProperty] Generated {count} properties ({lab.District}, {lab.BlockMode}, seed {lab.Seed}).");
        }

        [MenuItem("GameDev/Settlement/Generate/Worker Block")]
        public static void GenerateWorker() => GenerateDistrict(DistrictKind.Worker);

        [MenuItem("GameDev/Settlement/Generate/Affluent Block")]
        public static void GenerateAffluent() => GenerateDistrict(DistrictKind.Affluent);

        [MenuItem("GameDev/Settlement/Generate/Poor Block")]
        public static void GeneratePoor() => GenerateDistrict(DistrictKind.Poor);

        [MenuItem("GameDev/Settlement/Generate/Rich Block")]
        public static void GenerateRich() => GenerateDistrict(DistrictKind.Rich);

        [MenuItem("GameDev/Settlement/Generate/Commercial Block")]
        public static void GenerateCommercial() => GenerateDistrict(DistrictKind.Commercial);

        [MenuItem("GameDev/Settlement/Generate/Elite Block")]
        public static void GenerateElite() => GenerateDistrict(DistrictKind.Elite);

        [MenuItem("GameDev/Settlement/Generate/With Rear Lane")]
        public static void GenerateWithRearLane()
        {
            var lab = EnsureLab();
            lab.SetBlockMode(SettlementLabBlockMode.RearLane);
            lab.Generate();
            SaveActive();
            Selection.activeGameObject = lab.gameObject;
            Debug.Log(
                $"[SettlementProperty] Rear-lane block, seed {lab.Seed}, plots {lab.Layout.Properties.Count}.");
        }

        [MenuItem("GameDev/Settlement/Generate/Double Frontage Block")]
        public static void GenerateDoubleFrontage()
        {
            var lab = EnsureLab();
            lab.SetBlockMode(SettlementLabBlockMode.DoubleFrontage);
            lab.Generate();
            SaveActive();
            Selection.activeGameObject = lab.gameObject;
            Debug.Log(
                $"[SettlementProperty] Double-frontage block, seed {lab.Seed}, plots {lab.Layout.Properties.Count}.");
        }

        [MenuItem("GameDev/Settlement/Generate/Single Strip Block")]
        public static void GenerateSingleStrip()
        {
            var lab = EnsureLab();
            lab.SetBlockMode(SettlementLabBlockMode.SingleStrip);
            lab.Generate();
            SaveActive();
            Selection.activeGameObject = lab.gameObject;
            Debug.Log(
                $"[SettlementProperty] Single-strip block, seed {lab.Seed}, plots {lab.Layout.Properties.Count}.");
        }

        [MenuItem("GameDev/Settlement/Generate/Street Spine Fabric")]
        public static void GenerateStreetSpine()
        {
            var lab = EnsureLab();
            lab.SetBlockMode(SettlementLabBlockMode.StreetSpine);
            lab.SetBlockCount(2);
            lab.SetPlotCount(4);
            lab.Generate();
            SaveActive();
            Selection.activeGameObject = lab.gameObject;
            var streets = lab.Fabric != null ? lab.Fabric.Streets.Count : 0;
            Debug.Log(
                $"[SettlementProperty] Street spine, seed {lab.Seed}, plots {lab.Layout.Properties.Count}, streets {streets}.");
        }

        [MenuItem("GameDev/Settlement/Generate/District Mix Fabric")]
        public static void GenerateDistrictMix()
        {
            var lab = EnsureLab();
            lab.SetBlockMode(SettlementLabBlockMode.DistrictMix);
            lab.SetPlotCount(3);
            lab.Generate();
            SaveActive();
            Selection.activeGameObject = lab.gameObject;
            Debug.Log(
                $"[SettlementProperty] District mix (Commercial + GardenSquare + Worker), seed {lab.Seed}, plots {lab.Layout.Properties.Count}.");
        }

        [MenuItem("GameDev/Settlement/Clear Block Properties")]
        public static void Clear()
        {
            var lab = Object.FindAnyObjectByType<SettlementPropertyLab>();
            if (lab == null)
            {
                Debug.LogWarning("[SettlementProperty] No SettlementPropertyLab in scene.");
                return;
            }

            lab.Clear();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        [MenuItem("GameDev/Settlement/Reseed And Generate Block")]
        public static void ReseedAndGenerate()
        {
            var lab = EnsureLab();
            lab.ReseedAndGenerate();
            SaveActive();
            Selection.activeGameObject = lab.gameObject;
        }

        private static void GenerateDistrict(DistrictKind kind)
        {
            var lab = EnsureLab();
            lab.SetDistrict(kind);
            lab.SetBlockMode(SettlementLabBlockMode.SingleStrip);
            lab.Generate();
            SaveActive();
            Selection.activeGameObject = lab.gameObject;
            Debug.Log($"[SettlementProperty] {kind} block, seed {lab.Seed}, plots {lab.Layout.Properties.Count}.");
        }

        private static SettlementPropertyLab EnsureLab()
        {
            EnsureSceneFolder();
            var active = SceneManager.GetActiveScene();
            if (active.path != ScenePath && File.Exists(ScenePath))
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            else if (!File.Exists(ScenePath))
                ShellSandboxMenu.OpenOrCreateScene();

            var lab = Object.FindAnyObjectByType<SettlementPropertyLab>();
            if (lab != null)
                return lab;

            var go = new GameObject("SettlementPropertyLab");
            lab = go.AddComponent<SettlementPropertyLab>();
            lab.SetSeed(110201);
            lab.SetDistrict(DistrictKind.Worker);
            return lab;
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
