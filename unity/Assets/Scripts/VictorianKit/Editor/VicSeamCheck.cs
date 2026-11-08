using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameDev.VictorianKit.EditorTools
{
    /// <summary>
    /// Measures whether the brick joints meet across module seams on a built house (Plans/VICTORIAN_STREETS_PLAN.md
    /// §4.1, §8). For every outer face of a brick-slot mesh (vertical, axis-aligned, on a whole-metre plane of the
    /// outline) each vertex must hold the joint phase: along the wall, u ≡ ±s (mod 0.25 m); up the wall,
    /// v ≡ y − 0.60 (mod 0.15 m, two courses, so the half-bond's parity holds). Reveals and canted oriel faces are not
    /// outline faces and are left out.
    /// </summary>
    public static class VicSeamCheck
    {
        public const float Tolerance = 2e-3f;

        public sealed class Result
        {
            public int Faces;
            public int Broken;
            public readonly List<string> Samples = new List<string>();

            public override string ToString()
            {
                var sb = new StringBuilder($"brick outline faces {Faces}, broken {Broken}\n");
                foreach (var s in Samples)
                {
                    sb.Append(s).Append('\n');
                }

                return sb.ToString();
            }
        }

        /// <summary>Checks a house built with the kit's own materials (before any palette is applied).</summary>
        public static Result Run(GameObject house)
        {
            var result = new Result();
            var toHouse = house.transform.worldToLocalMatrix;
            foreach (var renderer in house.GetComponentsInChildren<MeshRenderer>(true))
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    continue;
                }

                var mesh = filter.sharedMesh;
                var mats = renderer.sharedMaterials;
                var m = toHouse * renderer.transform.localToWorldMatrix;
                var vertices = mesh.vertices;
                var uvs = mesh.uv;
                for (var sub = 0; sub < mesh.subMeshCount && sub < mats.Length; sub++)
                {
                    if (mats[sub] == null || mats[sub].name != "MAT_Vic_Brick")
                    {
                        continue;
                    }

                    var tris = mesh.GetTriangles(sub);
                    for (var t = 0; t < tris.Length; t += 3)
                    {
                        var p0 = m.MultiplyPoint3x4(vertices[tris[t]]);
                        var p1 = m.MultiplyPoint3x4(vertices[tris[t + 1]]);
                        var p2 = m.MultiplyPoint3x4(vertices[tris[t + 2]]);
                        var n = Vector3.Cross(p1 - p0, p2 - p0);
                        if (n.sqrMagnitude < 1e-10f)
                        {
                            continue;
                        }

                        n.Normalize();
                        if (Mathf.Abs(n.y) > 0.01f || (Mathf.Abs(n.x) < 0.999f && Mathf.Abs(n.z) < 0.999f))
                        {
                            continue;
                        }

                        // Along the wall: x for a face looking ±Z, z for one looking ±X; the plane must be a whole metre.
                        var alongX = Mathf.Abs(n.z) > 0.5f;
                        var plane = alongX ? p0.z : p0.x;
                        if (Mathf.Abs(plane - Mathf.Round(plane)) > Tolerance)
                        {
                            continue;
                        }

                        result.Faces++;
                        var idx = new[] { tris[t], tris[t + 1], tris[t + 2] };
                        var pts = new[] { p0, p1, p2 };
                        var forward = true;
                        var backward = true;
                        var vertical = true;
                        for (var k = 0; k < 3; k++)
                        {
                            var s = alongX ? pts[k].x : pts[k].z;
                            var uv = uvs[idx[k]];
                            forward &= Phase(s - uv.x, VicTextures.BrickLength);
                            backward &= Phase(s + uv.x, VicTextures.BrickLength);
                            vertical &= Phase(pts[k].y - 0.60f - uv.y, VicTextures.BrickCourse * 2f);
                        }

                        if (!(forward || backward) || !vertical)
                        {
                            result.Broken++;
                            if (result.Samples.Count < 40)
                            {
                                result.Samples.Add($"{renderer.transform.parent?.name ?? renderer.name}: face at {p0:F3} n {n:F2} uv {uvs[idx[0]]:F3} along {(forward || backward ? "ok" : "BROKEN")} up {(vertical ? "ok" : "BROKEN")}");
                            }
                        }
                    }
                }
            }

            return result;
        }

        private static bool Phase(float value, float period)
        {
            var r = Mathf.Repeat(value, period);
            return r < Tolerance || period - r < Tolerance;
        }
    }
}
