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
    /// two in each of the five beds farthest west, a third and two thirds along the bed's long side. One pass: a scene that
    /// already holds flax is left as it is.
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
            if (blocks.Any(b => b.name.StartsWith("Kit_Resource_Flax")))
            {
                Debug.Log("[SliceFlaxBeds] Veilmouth already holds flax; left as it is.");
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

            Physics.SyncTransforms();
            var ground = LayerMask.GetMask(KitLayers.Ground);
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
                    at.y = Physics.Raycast(new Vector3(at.x, box.max.y + 5f, at.z), Vector3.down, out var hit, 20f, ground, QueryTriggerInteraction.Ignore)
                        ? hit.point.y
                        : box.min.y;
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
