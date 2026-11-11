using System.IO;
using System.Linq;
using System.Text;
using GameDev.Slice.Kit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The owner's play test of S5a (2026-11-11) in Veilmouth: what the resource nodes stand on, and what stands solid
    /// round the Master of Item Creation's stall. Writes <c>Logs/SliceSurvey/s5a_playtest.txt</c> and changes nothing.
    /// Batch: -executeMethod GameDev.Slice.EditorTools.SlicePlayTestS5a.Survey
    /// </summary>
    public static class SlicePlayTestS5a
    {
        private const string Output = "Logs/SliceSurvey";

        /// <summary>
        /// The kit fixes of the play test: the herb and flax set into the ground by their clear leaf edges, and the market
        /// stalls' bodies made of their own meshes. Then the survey again.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.SlicePlayTestS5a.RebuildKit
        /// </summary>
        public static void RebuildKit()
        {
            SliceKitCatalogue.LoadRebuilding("Herb", "Flax", "Stall_Food", "Stall_Empty", "Stall_Fish", "Stall_Meat", "Stall_Goods", "Stall_Weapons");
            AssetDatabase.SaveAssets();
            Survey();
        }

        /// <summary>
        /// The herbs whose model keeps its own height in the scene (set on their slope by an earlier pass) are set into the
        /// ground by the kit's sink too. Only a model still standing above the sink is moved, so a second run moves none.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.SlicePlayTestS5a.SinkKeptHerbs
        /// </summary>
        public static void SinkKeptHerbs()
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var moved = 0;
            foreach (var block in Object.FindObjectsByType<KitBlock>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                         .Where(b => (b.name.StartsWith("Kit_Resource_Herb") || b.name.StartsWith("Kit_Resource_Flax")) && b.transform.childCount > 0))
            {
                var renderers = block.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0 || renderers.Min(r => r.bounds.min.y) - block.transform.position.y < -0.15f)
                {
                    continue;
                }

                var model = block.transform.GetChild(0);
                Undo.RecordObject(model, "Sink herb");
                model.localPosition -= new Vector3(0f, 0.18f, 0f); // the kit's Herb.Sink, measured
                EditorUtility.SetDirty(model);
                moved++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SlicePlayTestS5a] {moved} herb and flax models that kept their own height set 0.18 m into the ground.");
            Survey();
        }

        public static void Survey()
        {
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
            var report = new StringBuilder();
            Nodes(report);
            Stall(report);
            Directory.CreateDirectory(Output);
            File.WriteAllText($"{Output}/s5a_playtest.txt", report.ToString());
            Debug.Log("[SlicePlayTestS5a]\n" + report);
        }

        /// <summary>Each resource block's place against the ground under it, and its model's lowest point against the block.</summary>
        private static void Nodes(StringBuilder report)
        {
            var ground = LayerMask.GetMask(KitLayers.Ground);
            foreach (var group in Object.FindObjectsByType<KitBlock>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                         .Where(b => b.name.StartsWith("Kit_Resource_")).GroupBy(b => b.name.Split('_')[2]))
            {
                var gaps = group.Select(b =>
                {
                    var at = b.transform.position;
                    var floor = Physics.Raycast(at + (Vector3.up * 20f), Vector3.down, out var hit, 60f, ground, QueryTriggerInteraction.Ignore) ? hit.point.y : float.NaN;
                    var renderers = b.GetComponentsInChildren<Renderer>(true);
                    var low = renderers.Length > 0 ? renderers.Min(r => r.bounds.min.y) : float.NaN;
                    return (Name: b.name, At: at, Block: at.y - floor, Model: low - at.y);
                }).ToList();
                report.AppendLine($"{group.Key}: {gaps.Count} blocks; block above ground min {gaps.Min(g => g.Block):0.00} max {gaps.Max(g => g.Block):0.00} m; model's lowest point above the block min {gaps.Min(g => g.Model):0.00} max {gaps.Max(g => g.Model):0.00} m; {Leaves(group.First())}");
                var overridden = group.Count(b => b.transform.childCount > 0 && PrefabUtility.GetPropertyModifications(b.gameObject) is { } mods
                    && mods.Any(m => m.target is Transform t && t != null && t.name == b.transform.GetChild(0).name && m.propertyPath == "m_LocalPosition.y"));
                report.AppendLine($"   {overridden} of {gaps.Count} keep their own height for the model; lowest points: {string.Join(", ", gaps.GroupBy(g => Mathf.Round(g.Model * 100f) / 100f).Select(k => $"{k.Key:0.00}×{k.Count()}"))}");
                foreach (var g in gaps.OrderByDescending(g => Mathf.Abs(g.Block)).Take(3))
                {
                    report.AppendLine($"   {g.Name} at ({g.At.x:0.0}, {g.At.y:0.00}, {g.At.z:0.0}): block {g.Block:0.00} m, model {g.Model:0.00} m");
                }
            }
        }

        /// <summary>
        /// Where the block's leaves are, over the block: the heights below which 2 %, 5 % and 10 % of the triangles' area
        /// lies (by their centres) and the model's whole height. Cut-out leaf cards are clear at their edges, so the bounds'
        /// lowest point is not where the leaves are seen to start.
        /// </summary>
        private static string Leaves(KitBlock block)
        {
            var heights = new System.Collections.Generic.List<(float Y, float Area)>();
            foreach (var filter in block.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter.sharedMesh;
                if (mesh == null || !mesh.isReadable)
                {
                    continue;
                }

                var v = mesh.vertices;
                var t = mesh.triangles;
                var m = filter.transform.localToWorldMatrix;
                for (var i = 0; i + 2 < t.Length; i += 3)
                {
                    var a = m.MultiplyPoint3x4(v[t[i]]);
                    var b = m.MultiplyPoint3x4(v[t[i + 1]]);
                    var c = m.MultiplyPoint3x4(v[t[i + 2]]);
                    heights.Add(((a.y + b.y + c.y) / 3f - block.transform.position.y, Vector3.Cross(b - a, c - a).magnitude * 0.5f));
                }
            }

            if (heights.Count == 0)
            {
                return "no readable mesh";
            }

            heights.Sort((p, q) => p.Y.CompareTo(q.Y));
            var total = heights.Sum(h => h.Area);
            float At(float share)
            {
                var sum = 0f;
                foreach (var h in heights)
                {
                    sum += h.Area;
                    if (sum >= share * total)
                    {
                        return h.Y;
                    }
                }

                return heights[heights.Count - 1].Y;
            }

            return $"leaves from 2 % {At(0.02f):0.00}, 5 % {At(0.05f):0.00}, 10 % {At(0.10f):0.00}, top {heights[heights.Count - 1].Y:0.00} m";
        }

        /// <summary>Every solid collider within 7 m of the stall, and whether anything is drawn where it stands.</summary>
        private static void Stall(StringBuilder report)
        {
            var stall = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name.Contains("Master of Item Creation"));
            if (stall == null)
            {
                report.AppendLine("No Master of Item Creation stall.");
                return;
            }

            var centre = stall.position;
            report.AppendLine($"Stall '{stall.name}' at ({centre.x:0.0}, {centre.y:0.0}, {centre.z:0.0})");
            foreach (var c in Physics.OverlapSphere(centre, 7f, ~0, QueryTriggerInteraction.Ignore)
                         .Where(c => c.gameObject.layer != LayerMask.NameToLayer(KitLayers.Ground)))
            {
                var b = c.bounds;
                var drawn = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .Any(r => r.enabled && r.bounds.Intersects(b) && !(r is ParticleSystemRenderer));
                var owner = c.GetComponentInParent<KitBlock>();
                report.AppendLine($"   {Path(c.transform)} [{LayerMask.LayerToName(c.gameObject.layer)}] {c.GetType().Name} centre ({b.center.x:0.0}, {b.center.y:0.0}, {b.center.z:0.0}) size ({b.size.x:0.0}, {b.size.y:0.0}, {b.size.z:0.0}) enabled {c.enabled} drawn {drawn} block {(owner != null ? owner.name : "-")}");
            }
        }

        private static string Path(Transform t)
        {
            var parts = new System.Collections.Generic.List<string>();
            for (var i = 0; t != null && i < 4; i++, t = t.parent)
            {
                parts.Insert(0, t.name);
            }

            return string.Join("/", parts);
        }
    }
}
