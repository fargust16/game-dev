using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// VI-1's measurements of Victorian Interiors (VICTORIAN_INTERIORS_PLAN §3), before the pack is wrapped: which sides
    /// of each wall are drawn, the clear opening of each doorway and window wall, whether a window holds glass, the
    /// stair's step profile, and the lights and fire inside the prefabs. It instantiates the vendor prefabs in a new
    /// unsaved scene, writes only to Logs/SliceSurvey/vinterior_probe.txt, and changes no asset.
    /// Batch: -executeMethod GameDev.Slice.EditorTools.VictorianInteriorsProbe.Run
    /// </summary>
    public static class VictorianInteriorsProbe
    {
        private const string Folder = "Assets/Victorian Interiors/Victorian Interiors/Prefabs";
        private const string Report = "Logs/SliceSurvey/vinterior_probe.txt";
        private const float Step = 0.025f; // m, the raster of the opening and stair probes

        public static void Run()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var hitBack = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;
            var report = new StringBuilder();
            try
            {
                report.AppendLine("# Victorian Interiors probe, " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                report.AppendLine("Each wall: area of faces turned to +Z and to -Z (m2) and their mean z; the opening seen along Z.");
                foreach (var name in new[] { "Wall_1", "Wall_2", "Wall_3", "Wall_4", "Wall_5", "Wall_6", "Wall_7", "Wall_8" })
                {
                    Wall(name, report);
                }

                report.AppendLine();
                foreach (var name in new[] { "Door_1", "Door_2", "Curtain", "Mantel", "Bed", "Stair", "Banister", "Floor", "Ceiling" })
                {
                    Faces(name, report);
                }

                report.AppendLine();
                StairProfile(report);
                report.AppendLine();
                report.AppendLine("Lights, particle systems and transparent materials inside the prefabs:");
                foreach (var path in Directory.GetFiles(Folder, "*.prefab", SearchOption.AllDirectories).OrderBy(p => p))
                {
                    Contents(path.Replace('\\', '/'), report);
                }
            }
            finally
            {
                Physics.queriesHitBackfaces = hitBack;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Report));
            File.WriteAllText(Report, report.ToString());
            Debug.Log($"[VictorianInteriorsProbe] written to {Report}");
        }

        private static GameObject Place(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/{name}.prefab");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = Vector3.zero;
            foreach (var c in instance.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(c);
            }

            foreach (var f in instance.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh != null && f.GetComponent<MeshRenderer>() != null && f.GetComponent<MeshRenderer>().enabled)
                {
                    f.gameObject.AddComponent<MeshCollider>().sharedMesh = f.sharedMesh;
                }
            }

            Physics.SyncTransforms();
            return instance;
        }

        private static Bounds Seen(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>().Where(r => r.enabled && !(r is ParticleSystemRenderer)).ToArray();
            var b = renderers[0].bounds;
            foreach (var r in renderers.Skip(1))
            {
                b.Encapsulate(r.bounds);
            }

            return b;
        }

        /// <summary>World-space area and mean position of faces whose normal points along +Z, -Z, +X, -X, +Y, -Y.</summary>
        private static Dictionary<string, (float Area, Vector3 Mean)> FaceAreas(GameObject go)
        {
            var sums = new Dictionary<string, (float Area, Vector3 Weighted)>();
            foreach (var f in go.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = f.sharedMesh;
                if (mesh == null || !mesh.isReadable)
                {
                    continue;
                }

                var m = f.transform.localToWorldMatrix;
                var v = mesh.vertices.Select(p => m.MultiplyPoint3x4(p)).ToArray();
                var t = mesh.triangles;
                for (var i = 0; i + 2 < t.Length; i += 3)
                {
                    var a = v[t[i]];
                    var cross = Vector3.Cross(v[t[i + 1]] - a, v[t[i + 2]] - a);
                    var area = cross.magnitude * 0.5f;
                    if (area < 1e-6f)
                    {
                        continue;
                    }

                    var n = cross.normalized;
                    var key = n.z > 0.7f ? "+Z" : n.z < -0.7f ? "-Z" : n.x > 0.7f ? "+X" : n.x < -0.7f ? "-X" : n.y > 0.7f ? "+Y" : n.y < -0.7f ? "-Y" : null;
                    if (key == null)
                    {
                        continue;
                    }

                    var centre = (a + v[t[i + 1]] + v[t[i + 2]]) / 3f;
                    sums.TryGetValue(key, out var s);
                    sums[key] = (s.Area + area, s.Weighted + centre * area);
                }
            }

            return sums.ToDictionary(kv => kv.Key, kv => (kv.Value.Area, kv.Value.Weighted / Mathf.Max(1e-6f, kv.Value.Area)));
        }

        private static string Describe(Dictionary<string, (float Area, Vector3 Mean)> faces, params string[] keys) =>
            string.Join("; ", keys.Where(faces.ContainsKey).Select(k => $"{k} {faces[k].Area:0.00} m2 at ({faces[k].Mean.x:0.00}, {faces[k].Mean.y:0.00}, {faces[k].Mean.z:0.00})"));

        private static void Wall(string name, StringBuilder report)
        {
            var go = Place(name);
            var b = Seen(go);
            var faces = FaceAreas(go);
            var materials = go.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials).Where(m => m != null).Select(m => m.name + (Transparent(m) ? " (transparent)" : "")).Distinct();
            report.AppendLine($"{name}: bounds x {b.min.x:0.00}..{b.max.x:0.00}, y {b.min.y:0.00}..{b.max.y:0.00}, z {b.min.z:0.00}..{b.max.z:0.00}; materials {string.Join(", ", materials)}");
            report.AppendLine($"  faces {Describe(faces, "+Z", "-Z")}");

            // The opening: rays along -Z through the wall on a raster; a free cell lets a ray through.
            var free = new HashSet<(int, int)>();
            var nx = Mathf.RoundToInt(b.size.x / Step);
            var ny = Mathf.RoundToInt(b.size.y / Step);
            for (var ix = 1; ix < nx; ix++)
            {
                for (var iy = 1; iy < ny; iy++)
                {
                    var origin = new Vector3(b.min.x + ix * Step, b.min.y + iy * Step, b.max.z + 1f);
                    if (!Physics.Raycast(origin, Vector3.back, b.size.z + 2f))
                    {
                        free.Add((ix, iy));
                    }
                }
            }

            if (free.Count == 0)
            {
                report.AppendLine("  opening: none");
            }
            else
            {
                // The largest connected free region.
                var best = new List<(int, int)>();
                var seen = new HashSet<(int, int)>();
                foreach (var start in free)
                {
                    if (!seen.Add(start))
                    {
                        continue;
                    }

                    var region = new List<(int, int)>();
                    var queue = new Queue<(int, int)>();
                    queue.Enqueue(start);
                    while (queue.Count > 0)
                    {
                        var c = queue.Dequeue();
                        region.Add(c);
                        foreach (var d in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                        {
                            var n = (c.Item1 + d.Item1, c.Item2 + d.Item2);
                            if (free.Contains(n) && seen.Add(n))
                            {
                                queue.Enqueue(n);
                            }
                        }
                    }

                    if (region.Count > best.Count)
                    {
                        best = region;
                    }
                }

                float X(int i) => b.min.x + i * Step;
                float Y(int i) => b.min.y + i * Step;
                report.AppendLine($"  opening: x {X(best.Min(c => c.Item1)):0.00}..{X(best.Max(c => c.Item1)):0.00}, y {Y(best.Min(c => c.Item2)):0.00}..{Y(best.Max(c => c.Item2)):0.00}, {best.Count * Step * Step:0.00} m2 (of {free.Count * Step * Step:0.00} m2 free in all)");
            }

            Object.DestroyImmediate(go);
        }

        private static void Faces(string name, StringBuilder report)
        {
            var go = Place(name);
            var b = Seen(go);
            report.AppendLine($"{name}: bounds x {b.min.x:0.00}..{b.max.x:0.00}, y {b.min.y:0.00}..{b.max.y:0.00}, z {b.min.z:0.00}..{b.max.z:0.00}");
            report.AppendLine($"  faces {Describe(FaceAreas(go), "+Z", "-Z", "+X", "-X", "+Y", "-Y")}");
            Object.DestroyImmediate(go);
        }

        /// <summary>The top surface under rays straight down, along the stair's long axis at its middle and its sides.</summary>
        private static void StairProfile(StringBuilder report)
        {
            var go = Place("Stair");
            var b = Seen(go);
            report.AppendLine($"Stair profile (heights of the top surface along z, at three x):");
            foreach (var fx in new[] { 0.25f, 0.5f, 0.75f })
            {
                var x = Mathf.Lerp(b.min.x, b.max.x, fx);
                var line = new StringBuilder($"  x {x:0.00}: ");
                var last = float.NaN;
                for (var z = b.min.z; z <= b.max.z; z += Step)
                {
                    var h = Physics.Raycast(new Vector3(x, b.max.y + 1f, z), Vector3.down, out var hit, b.size.y + 2f) ? hit.point.y : float.NaN;
                    if (float.IsNaN(h) != float.IsNaN(last) || (!float.IsNaN(h) && Mathf.Abs(h - last) > 0.02f))
                    {
                        line.Append($"[z {z:0.00}: {(float.IsNaN(h) ? "none" : h.ToString("0.00"))}] ");
                        last = h;
                    }
                }

                report.AppendLine(line.ToString());
            }

            Object.DestroyImmediate(go);
        }

        private static bool Transparent(Material m) => m.HasProperty("_Surface") && m.GetFloat("_Surface") > 0.5f;

        private static void Contents(string path, StringBuilder report)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var lights = prefab.GetComponentsInChildren<Light>(true);
            var particles = prefab.GetComponentsInChildren<ParticleSystem>(true);
            var transparent = prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Where(m => m != null && Transparent(m)).Select(m => m.name).Distinct().ToArray();
            var emissive = prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials)
                .Where(m => m != null && m.IsKeywordEnabled("_EMISSION")).Select(m => m.name).Distinct().ToArray();
            if (lights.Length + particles.Length + transparent.Length + emissive.Length == 0)
            {
                return;
            }

            var lightText = string.Join(", ", lights.Select(l => $"{l.type} r{l.range:0.#} i{l.intensity:0.##} at {l.transform.position}"));
            var particleText = string.Join(", ", particles.Select(p => $"{p.name} at {p.transform.position}"));
            report.AppendLine($"{Path.GetFileNameWithoutExtension(path)}: lights [{lightText}]; particles [{particleText}]; transparent [{string.Join(", ", transparent)}]; emissive [{string.Join(", ", emissive)}]");
        }
    }
}
