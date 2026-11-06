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

        /// <summary>
        /// Every loose prop must rest where it stands (kit standard §4): the scene's bodies are woken and left to
        /// gravity for <paramref name="seconds"/>, untouched, and a prop that moves more than <see cref="RestTolerance"/>
        /// floats or was set into another. Read without saving. Returns one line for each.
        /// </summary>
        public static System.Collections.Generic.List<string> Unrested(float seconds = 3f)
        {
            var props = Object.FindObjectsByType<LooseProp>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var p in props.Where(p => p.IsThing))
            {
                var instance = PrefabUtility.GetOutermostPrefabInstanceRoot(p.gameObject);
                if (instance != null)
                {
                    // Edit mode cannot move a child out of a prefab instance; the scene is never saved here.
                    PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }

                var block = p.GetComponentInParent<KitBlock>();
                p.transform.SetParent(block != null ? block.transform.parent : null, true); // as LooseProp.Awake
            }

            var start = props.ToDictionary(p => p, p => p.transform.position);
            var mode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            Physics.SyncTransforms();
            for (var t = 0f; t < seconds; t += 0.02f)
            {
                Physics.Simulate(0.02f);
            }

            Physics.simulationMode = mode;
            return props.Select(p => (p, d: p.transform.position - start[p])).Where(x => x.d.magnitude > RestTolerance)
                .OrderByDescending(x => x.d.magnitude)
                .Select(x => $"{Path(x.p.transform)} at {start[x.p]:F2} moved {x.d.magnitude:F2} m (down {-x.d.y:F2})").ToList();
        }

        public const float RestTolerance = 0.1f; // m, authoring default: an apple may roll a few centimetres

        private static string Path(Transform t)
        {
            var name = t.name;
            for (var p = t.parent; p != null && p.parent != null; p = p.parent)
            {
                name = p.name + "/" + name;
            }

            return name;
        }

        /// <summary>
        /// Loose props that do not rest, in Veilmouth, its interiors, and the Dwelling scene. Batch:
        /// -executeMethod GameDev.Slice.EditorTools.LoosePropsPass.RestCheckBatch (without -quit).
        /// </summary>
        public static void RestCheckBatch()
        {
            var total = 0;
            var scenes = new[] { VeilmouthScene.ScenePath }.Concat(
                System.IO.Directory.GetFiles("Assets/Scenes/Slice/Interiors", "*.unity").Select(p => p.Replace('\\', '/')));
            foreach (var path in scenes)
            {
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var lines = Unrested();
                total += lines.Count;
                Debug.Log($"[LoosePropsPass] {path}: {lines.Count} props do not rest" + string.Concat(lines.Take(40).Select(l => "\n  " + l)));
            }

            // The Dwelling scene builds its rooms on arrival: a sample of every archetype and wealth.
            foreach (var archetype in (Slice.Dwellings.DwellingArchetype[])System.Enum.GetValues(typeof(Slice.Dwellings.DwellingArchetype)))
            {
                foreach (var wealth in (Slice.Dwellings.DwellingWealth[])System.Enum.GetValues(typeof(Slice.Dwellings.DwellingWealth)))
                {
                    for (var n = 0; n < 3; n++)
                    {
                        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                        var key = VeilmouthDwellings.PreviewKey(archetype, wealth, 300 + (n * 31), -700 - (n * 17));
                        VeilmouthDwellings.BuildAndCheck(key, VeilmouthDwellings.Kit, VeilmouthDwellings.Settings, out _);
                        var lines = Unrested();
                        total += lines.Count;
                        if (lines.Count > 0)
                        {
                            Debug.Log($"[LoosePropsPass] dwelling {archetype} {wealth} {n}: {lines.Count} props do not rest" +
                                      string.Concat(lines.Take(20).Select(l => "\n  " + l)));
                        }
                    }
                }
            }

            Debug.Log($"[LoosePropsPass] RESULT: {total} props do not rest.");
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
