using System.Collections.Generic;
using System.Linq;
using GameDev.Slice.Kit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// S5a.5: 11's ten flax patches (19 §6.4), which the C4 city left out, planted in Crooked Hamlet's fenced farm beds:
    /// two in each of the five beds farthest west, a third and two thirds along the bed's long side, each on the kit's 1 m
    /// grid and 0.1 m height step, the step below the ground so it never floats. One pass: a scene that already holds flax
    /// only has its patches set on that grid.
    /// </summary>
    public static class SliceFlaxBeds
    {
        private const string Scene = "Assets/Scenes/Slice/Veilmouth.unity";
        private const string Flax = "Assets/Prefabs/SliceKit/Resource/Kit_Resource_Flax_1x1.prefab";
        private const int Beds = 5;
        private const float SameBed = 3f; // m: fence pieces of one bed stand within this of a neighbour

        [MenuItem("GameDev/Slice/Gathering/Plant the Flax in Crooked Hamlet's Beds (once)")]
        public static void Plant()
        {
            EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);
            var blocks = Object.FindObjectsByType<KitBlock>(FindObjectsInactive.Include);
            Physics.SyncTransforms();
            var ground = LayerMask.GetMask(KitLayers.Ground);
            var held = blocks.Where(b => b.name.StartsWith("Kit_Resource_Flax")).ToList();
            if (held.Count > 0)
            {
                foreach (var b in held)
                {
                    b.transform.position = OnGrid(b.transform.position, ground);
                    EditorUtility.SetDirty(b.transform);
                }

                EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
                Debug.Log($"[SliceFlaxBeds] Veilmouth already holds flax; its {held.Count} patches set on the kit grid.");
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Flax);
            var fences = blocks.Where(b => b.name.StartsWith("Kit_Roadside_Bed")).ToList();
            var beds = Cluster(fences).OrderBy(bed => bed.Min(f => f.transform.position.x)).Take(Beds).ToList();
            if (prefab == null || beds.Count < Beds)
            {
                Debug.LogError($"[SliceFlaxBeds] Needs the flax block and {Beds} beds; found {beds.Count} beds.");
                return;
            }

            var planted = 0;
            foreach (var bed in beds)
            {
                var box = bed.SelectMany(f => f.GetComponentsInChildren<Renderer>()).Select(r => r.bounds)
                    .Aggregate((a, b) => { a.Encapsulate(b); return a; });
                var alongX = box.size.x >= box.size.z;
                foreach (var t in new[] { 1f / 3f, 2f / 3f })
                {
                    var at = alongX
                        ? new Vector3(Mathf.Lerp(box.min.x, box.max.x, t), 0f, box.center.z)
                        : new Vector3(box.center.x, 0f, Mathf.Lerp(box.min.z, box.max.z, t));
                    at.y = box.min.y;
                    at = OnGrid(at, ground);
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, bed[0].transform.parent);
                    go.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, 90f * planted, 0f));
                    planted++;
                    Debug.Log($"[SliceFlaxBeds] Flax at {at}.");
                }
            }

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[SliceFlaxBeds] {planted} flax patches planted in {beds.Count} beds of {Cluster(fences).Count}.");
        }

        /// <summary>
        /// The kit grid's node nearest a place, on the ground there at the 0.1 m step below it (the kit standard's §2):
        /// the flax sinks up to 0.1 m more, and never floats.
        /// </summary>
        private static Vector3 OnGrid(Vector3 at, int ground)
        {
            var x = Mathf.Round(at.x);
            var z = Mathf.Round(at.z);
            var y = Physics.Raycast(new Vector3(x, at.y + 10f, z), Vector3.down, out var hit, 30f, ground, QueryTriggerInteraction.Ignore) ? hit.point.y : at.y;
            return new Vector3(x, Mathf.Floor((y * 10f) + 0.001f) / 10f, z);
        }

        /// <summary>The fence pieces gathered into beds: a piece within <see cref="SameBed"/> of one in a bed joins it.</summary>
        private static List<List<KitBlock>> Cluster(List<KitBlock> pieces)
        {
            var beds = new List<List<KitBlock>>();
            var left = new List<KitBlock>(pieces);
            while (left.Count > 0)
            {
                var bed = new List<KitBlock> { left[0] };
                left.RemoveAt(0);
                for (var grew = true; grew;)
                {
                    grew = false;
                    for (var i = left.Count - 1; i >= 0; i--)
                    {
                        var p = left[i].transform.position;
                        if (bed.Any(b => (b.transform.position - p).sqrMagnitude <= SameBed * SameBed))
                        {
                            bed.Add(left[i]);
                            left.RemoveAt(i);
                            grew = true;
                        }
                    }
                }

                beds.Add(bed);
            }

            return beds;
        }
    }
}
