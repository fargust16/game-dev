using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.VictorianKit.EditorTools
{
    /// <summary>
    /// Measures the Victorian Kit's modules as Unity imports them: each prefab's hierarchy, mesh bounds in the prefab's
    /// space, material slots and UV ranges, written to Logs/VictorianKit. Builds the kit's prefabs first when the
    /// package has none. Batch: -executeMethod GameDev.VictorianKit.EditorTools.VicKitProbe.Run
    /// </summary>
    public static class VicKitProbe
    {
        [MenuItem("GameDev/Victorian Kit/Probe Modules")]
        public static void Run()
        {
            VicKitPaths.EnsureKitPrefabs();
            Directory.CreateDirectory(VicKitPaths.LogFolder);
            var inv = CultureInfo.InvariantCulture;
            var table = new StringBuilder("id\tchildren\tmin\tmax\tsubmeshes\tmaterials\tuv0min\tuv0max\tuv2\n");
            foreach (var id in VicKitPaths.ModuleIds().OrderBy(s => s))
            {
                var prefab = VicKitPaths.LoadModule(id);
                if (prefab == null)
                {
                    table.Append(id).Append("\tMISSING\n");
                    continue;
                }

                var filters = prefab.GetComponentsInChildren<MeshFilter>(true);
                var b = new Bounds();
                var first = true;
                var uvMin = new Vector2(float.MaxValue, float.MaxValue);
                var uvMax = new Vector2(float.MinValue, float.MinValue);
                var uv2 = true;
                var subs = 0;
                foreach (var f in filters)
                {
                    var mesh = f.sharedMesh;
                    if (mesh == null)
                    {
                        continue;
                    }

                    subs += mesh.subMeshCount;
                    var m = prefab.transform.worldToLocalMatrix * f.transform.localToWorldMatrix;
                    foreach (var v in mesh.vertices)
                    {
                        var p = m.MultiplyPoint3x4(v);
                        if (first)
                        {
                            b = new Bounds(p, Vector3.zero);
                            first = false;
                        }
                        else
                        {
                            b.Encapsulate(p);
                        }
                    }

                    foreach (var uv in mesh.uv)
                    {
                        uvMin = Vector2.Min(uvMin, uv);
                        uvMax = Vector2.Max(uvMax, uv);
                    }

                    uv2 &= mesh.uv2 != null && mesh.uv2.Length == mesh.vertexCount;
                }

                var mats = string.Join(",", prefab.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r => r.sharedMaterials).Select(mt => mt == null ? "null" : mt.name));
                var kids = string.Join("|", prefab.GetComponentsInChildren<Transform>(true).Select(t =>
                    $"{t.name}@{t.localPosition.ToString("F3")}r{t.localEulerAngles.ToString("F1")}s{t.localScale.ToString("F2")}"));
                table.Append(id).Append('\t').Append(kids).Append('\t')
                    .Append(b.min.ToString("F3")).Append('\t').Append(b.max.ToString("F3")).Append('\t')
                    .Append(subs.ToString(inv)).Append('\t').Append(mats).Append('\t')
                    .Append(uvMin.ToString("F3")).Append('\t').Append(uvMax.ToString("F3")).Append('\t').Append(uv2 ? "yes" : "no").Append('\n');
            }

            File.WriteAllText(Path.Combine(VicKitPaths.LogFolder, "modules.tsv"), table.ToString());

            // The kit's own House U from its manifest, and the same house from this assembler, compared piece by piece.
            var manifest = VicManifest.Load(VicKitPaths.KitManifestFolder + "/House_U_Modular.json").Pieces().ToList();
            var mine = VicHousePlans.HouseU().Pieces;
            var report = new StringBuilder();
            var missing = VicHousePlans.Missing(manifest, mine);
            var extra = VicHousePlans.Missing(mine, manifest);
            report.Append($"manifest {manifest.Count} pieces, assembler {mine.Count}; missing {missing.Count}, extra {extra.Count}\n");
            foreach (var k in missing)
            {
                report.Append("MISSING ").Append(k).Append('\n');
            }

            foreach (var k in extra)
            {
                report.Append("EXTRA ").Append(k).Append('\n');
            }

            File.WriteAllText(Path.Combine(VicKitPaths.LogFolder, "house_u_compare.txt"), report.ToString());

            // Views: the street facade faces Unity −Z, the house runs x 0..16, z 0..10.
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            VicHouseFactory.Build("House_U_Manifest", manifest);
            VicShots.Eye("probe_u_front", new Vector3(8f, 1.7f, -18f), new Vector3(8f, 6f, 4f));
            VicShots.Eye("probe_u_frontleft", new Vector3(-10f, 6f, -14f), new Vector3(8f, 6f, 5f));
            VicShots.Eye("probe_u_backright", new Vector3(28f, 8f, 24f), new Vector3(8f, 6f, 5f));
            VicShots.Eye("probe_u_court_left", new Vector3(10.5f, 1.7f, -1.5f), new Vector3(5.5f, 6f, 4.5f));
            VicShots.Eye("probe_u_roof", new Vector3(8f, 22f, -10f), new Vector3(8f, 10f, 5f));
            VicShots.Top("probe_u_top", new Vector3(8f, 0f, 5f), 10f);

            Debug.Log($"[VicKit] probe written to {VicKitPaths.LogFolder}: {report.ToString().Split('\n')[0]}");
        }
    }
}
