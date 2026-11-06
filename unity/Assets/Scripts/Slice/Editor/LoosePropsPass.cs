using System.Linq;
using GameDev.Slice.Kit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The loose-props pass (kit standard §4, owner 2026-11-06): rebuilds the kit and the Dwelling scene, whose prefab
    /// instances in Veilmouth and its interiors then carry their bodies, and lets the saved player push them.
    /// </summary>
    public static class LoosePropsPass
    {
        [MenuItem("GameDev/Slice/Loose Props Pass")]
        public static void Run()
        {
            var player = PrefabUtility.LoadPrefabContents(SliceTestScenes.PlayerPrefabPath);
            if (player.GetComponent<PushLooseProps>() == null)
            {
                player.AddComponent<PushLooseProps>();
                PrefabUtility.SaveAsPrefabAsset(player, SliceTestScenes.PlayerPrefabPath);
            }

            PrefabUtility.UnloadPrefabContents(player);
            foreach (var path in SliceTownsfolk.Prefabs)
            {
                var walker = PrefabUtility.LoadPrefabContents(path);
                SliceTownsfolk.ShoulderLooseProps(walker);
                PrefabUtility.SaveAsPrefabAsset(walker, path);
                PrefabUtility.UnloadPrefabContents(walker);
            }

            VeilmouthDwellings.BuildAll();
            Debug.Log("[LoosePropsPass] done.");
        }

        /// <summary>
        /// Kit faults in Veilmouth and its seven interiors, read without saving. Batch:
        /// -executeMethod GameDev.Slice.EditorTools.LoosePropsPass.CheckScenesBatch (without -quit).
        /// </summary>
        public static void CheckScenesBatch()
        {
            var total = 0;
            var scenes = new[] { VeilmouthScene.ScenePath }.Concat(
                System.IO.Directory.GetFiles("Assets/Scenes/Slice/Interiors", "*.unity").Select(p => p.Replace('\\', '/'))
                    .Where(p => p != VeilmouthDwellings.ScenePath));
            foreach (var path in scenes)
            {
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var blocks = Object.FindObjectsByType<KitBlock>(FindObjectsInactive.Exclude);
                var faults = KitLayoutCheck.Check(blocks);
                total += faults.Count;
                Debug.Log($"[LoosePropsPass] {path}: {blocks.Length} blocks, " +
                          $"{blocks.Count(b => b.GetComponent<LooseProp>() != null)} loose, " +
                          $"{blocks.Sum(b => b.GetComponentsInChildren<LooseProp>().Count(p => p.IsThing))} things, {faults.Count} kit faults" +
                          string.Concat(faults.Take(20).Select(f => "\n  " + f)));
            }

            Debug.Log($"[LoosePropsPass] RESULT: {total} kit faults.");
            EditorApplication.Exit(total == 0 ? 0 : 1);
        }

        /// <summary>Batch entry: -executeMethod GameDev.Slice.EditorTools.LoosePropsPass.RunBatch (without -quit).</summary>
        public static void RunBatch()
        {
            var code = 0;
            try
            {
                Run();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                code = 1;
            }

            EditorApplication.Exit(code);
        }
    }
}
