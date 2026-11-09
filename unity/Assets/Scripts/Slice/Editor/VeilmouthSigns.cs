using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The owner's hanging sign (99 Open Questions VS-SIGN (b), 2026-11-09; city plan §25.6): over the street doors of the
    /// shops among the seven that open, Harrow &amp; Tincture and the Soot &amp; Bell, one each. It hangs beside the door, its
    /// bracket's plate on the facade and its board out over the pavement, on the side away from the other shop's door so
    /// the two do not crowd. The scene is the owner's, so this pass touches only its own group, `Shop Signs`, and run again
    /// it replaces it. Then each sign is rendered from the pavement.
    /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthSigns.Hang
    /// </summary>
    internal static class VeilmouthSigns
    {
        private const string GroupName = "Shop Signs";

        private static readonly string[] Shops = { "Harrow & Tincture", "The Soot & Bell" };

        /// <summary>Authoring default: from the door's middle to the bracket, along the facade, m.</summary>
        private const float Beside = 1.6f;

        /// <summary>Authoring default: from the sill to the board's foot, so a tall man passes under it, m.</summary>
        private const float Clear = 2.6f;

        [MenuItem("GameDev/Slice/Hang Veilmouth Shop Signs")]
        public static void Hang()
        {
            var kit = SliceKitCatalogue.Load();
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var old = scene.GetRootGameObjects().FirstOrDefault(g => g.name == GroupName);
            if (old != null)
            {
                Object.DestroyImmediate(old);
            }

            var houses = Shops.Select(name => Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Single(t => t.name == name && t.Find(VictorianHouses.DoorMarker) != null)).ToList();
            var doors = houses.Select(h => h.Cast<Transform>().First(t => t.name == VictorianHouses.DoorMarker)).ToList();
            var group = new GameObject(GroupName).transform;
            var entry = kit["TrSign"];
            var report = new List<string>();
            var shots = new List<(Vector3 At, Vector3 Out, Vector3 Right)>();
            Physics.SyncTransforms();
            for (var i = 0; i < houses.Count; i++)
            {
                var house = houses[i];
                var door = doors[i];
                var other = doors[(i + 1) % doors.Count].position;
                var right = house.right;
                var side = Vector3.Dot(door.position - other, right) >= 0f ? 1f : -1f;
                var sill = door.position.y;

                // The facade's face, found from the pavement at the bracket's height; the door's line if nothing answers.
                var probe = door.position + (right * (side * Beside)) + (Vector3.up * (Clear + 0.8f));
                var face = probe;
                var found = Physics.Raycast(probe + (house.forward * 1.5f), -house.forward, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore)
                            && hit.transform.IsChildOf(house);
                if (found)
                {
                    face = hit.point;
                }
                else
                {
                    Debug.LogWarning($"[VeilmouthSigns] {house.name}: no facade answered beside the door; the sign hangs on the door's line.");
                }

                var sign = (GameObject)PrefabUtility.InstantiatePrefab(entry.Prefab, group);
                sign.name = $"Sign - {house.name}";
                var foot = new Vector3(face.x, sill + Clear, face.z);
                // The block on the kit's grid and height step, the board moved inside it to hang where it should.
                var at = foot - (right * (entry.Width * 0.5f));
                var node = new Vector3(Mathf.Round(at.x), Mathf.Floor((at.y * 10f) + 0.02f) / 10f, Mathf.Round(at.z));
                sign.transform.SetPositionAndRotation(node, house.rotation);
                var model = sign.transform.Find("Model");
                if (model != null)
                {
                    model.position += at - node;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(model);
                }
                report.Add($"{house.name}: {(side > 0f ? "right" : "left")} of the door, bracket at {foot:F2}, " +
                           $"{(found ? $"on {hit.transform.name}" : "on the door's line")}");
                shots.Add((foot + (Vector3.up * 0.6f), house.forward, right * side));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[VeilmouthSigns] {report.Count} signs:\n  " + string.Join("\n  ", report));

            var n = 0;
            foreach (var (at, outward, side) in shots)
            {
                VeilmouthRenders.Eye($"sign_{n}_front", at + (outward * 6f) + (side * 2f) - (Vector3.up * 1.2f), at, 50f);
                VeilmouthRenders.Eye($"sign_{n}_along", at + (outward * 1.2f) + (side * 5f) - (Vector3.up * 1.4f), at, 50f);
                n++;
            }
        }
    }
}
