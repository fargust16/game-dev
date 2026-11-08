using System.Collections.Generic;
using System.Linq;
using GameDev.Slice.Kit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The owner's garden ornaments in Veilmouth (2026-11-08): the angel pouring water at the heart of Wardley Garden,
    /// where the city plan asks for "a fountain or a statue" in a square (§6.1), in place of the garden's middle tree;
    /// stone birdbaths in the front gardens of two Ridge villas and the faction's house. The scene is the owner's, so
    /// this pass touches only its own group, `Ornaments`, and that one tree; run again, it replaces its own group.
    /// Positions are the city plan's metres (px, py); each ornament stands on a grid node on the ground.
    /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthOrnaments.Place
    /// </summary>
    internal static class VeilmouthOrnaments
    {
        private const string GroupName = "Ornaments";

        // (key, px, py of the footprint's north-west corner, quarter turns, name). Facing: the angel looks north to
        // Wardley Garden's gate on the Garden Walk: the model faces -Z, so it is turned twice.
        private static readonly (string Key, int Px, int Py, int Turns, string Name)[] Spots =
        {
            ("AngelStatue", 557, 696, 2, "Angel fountain, Wardley Garden"),
            ("Birdbath", 528, 652, 0, "Birdbath, Ridge villa"),
            ("Birdbath", 708, 652, 0, "Birdbath, Ridge villa"),
            ("Birdbath", 636, 652, 0, "Birdbath, the faction's garden"),
        };

        [MenuItem("GameDev/Slice/Place Veilmouth Ornaments")]
        public static void Place()
        {
            var kit = SliceKitCatalogue.Load();
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var old = scene.GetRootGameObjects().FirstOrDefault(g => g.name == GroupName);
            if (old != null)
            {
                Object.DestroyImmediate(old);
            }

            // The middle tree of Wardley Garden gives its place to the angel.
            var heart = World(557.5f, 696.5f);
            foreach (var tree in Object.FindObjectsByType<KitBlock>(FindObjectsInactive.Include)
                         .Where(k => k.name.Contains("Tree") && Flat(k.transform.position + new Vector3(0.5f, 0f, 0.5f) - heart) < 1.2f).ToList())
            {
                Debug.Log($"[VeilmouthOrnaments] '{tree.name}' at {tree.transform.position:F1} gives its place to the angel.");
                Object.DestroyImmediate(tree.gameObject);
            }

            var group = new GameObject(GroupName).transform;
            Physics.SyncTransforms();
            var placed = new List<string>();
            foreach (var (key, px, py, turns, name) in Spots)
            {
                var corner = World(px, py + 1); // its south-west corner: plan py grows southward, world z northward
                var (min, max) = GroundUnder(corner);
                var y = Mathf.Ceil(Mathf.Max(min, max - 0.15f) * 10f) / 10f;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(kit[key].Prefab, group);
                go.name = name;
                var rotation = Quaternion.Euler(0f, 90f * turns, 0f);
                var pivot = new Vector3(corner.x, y, corner.z) + new Vector3(0.5f, 0f, 0.5f) - (rotation * new Vector3(0.5f, 0f, 0.5f));
                go.transform.SetPositionAndRotation(pivot, rotation);
                var clash = Physics.OverlapBox(corner + new Vector3(0.5f, y + 1f, 0.5f), new Vector3(0.45f, 0.8f, 0.45f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)
                    .Where(c => !c.transform.IsChildOf(go.transform) && c.gameObject.layer != LayerMask.NameToLayer(KitLayers.Ground))
                    .Select(c => c.name).Distinct().ToList();
                placed.Add($"{name} at {pivot:F1}" + (clash.Count > 0 ? $", touching {string.Join(", ", clash)}" : string.Empty));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[VeilmouthOrnaments] {placed.Count} ornaments:\n  " + string.Join("\n  ", placed));
        }

        private static Vector3 World(float px, float py) => new Vector3(px - 500f, 0f, 500f - py);

        private static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;

        private static (float Min, float Max) GroundUnder(Vector3 corner)
        {
            var heights = new List<float>();
            foreach (var (dx, dz) in new[] { (0f, 0f), (1f, 0f), (0f, 1f), (1f, 1f), (0.5f, 0.5f) })
            {
                var from = new Vector3(corner.x + dx, 300f, corner.z + dz);
                if (Physics.Raycast(from, Vector3.down, out var hit, 400f, LayerMask.GetMask(KitLayers.Ground), QueryTriggerInteraction.Ignore))
                {
                    heights.Add(hit.point.y);
                }
            }

            return heights.Count == 0 ? (0f, 0f) : (heights.Min(), heights.Max());
        }
    }
}
