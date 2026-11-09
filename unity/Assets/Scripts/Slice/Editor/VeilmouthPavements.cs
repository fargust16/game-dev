using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameDev.Slice.Kit;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Veilmouth's raised pavements as islands (Plans/VEILMOUTH_REFERENCE_PLAN.md R4). A pavement is every pavement lane
    /// of the city's streets taken together, less the carriageways: a carriageway that meets another is carried on into
    /// it, so it cuts the pavement across its mouth, and every corner where two carriageways meet is rounded with a kerb
    /// radius of <see cref="Radius"/>. A lane or passage with no carriages does not cut it: the pavement runs across its
    /// mouth. The island stands <see cref="Kerb"/> over the ground, its top in slabs with a kerb stone's top along the
    /// carriageway, its edge a stone kerb face, with a mesh collider on the Ground layer. The wet band that darkened the
    /// carriageway along the kerb is gone (owner, fifth play test, 2026-11-09): laid 3 cm over the terrain, it came and
    /// went with the terrain's level of detail. A street tree stands in a square pit on the carriageway by the kerb
    /// (<see cref="Pit"/>), a stone edge round its earth.
    /// <para>
    /// The outline is the zero line of a distance field sampled every 0.25 m, so straight kerbs stay straight and the
    /// rounded corners are smooth. The top follows the ground through heights sampled once a metre and blended, so a
    /// slab of 1 m and the finer cells along the kerb meet without a crack. Every size is an authoring default.
    /// </para>
    /// </summary>
    public sealed class VeilmouthPavements
    {
        /// <summary>The pavement's top over the carriageway, m (R4: 0.15 m; the player's step is 0.4 m).</summary>
        public const float Kerb = 0.15f;

        /// <summary>The kerb's radius where carriageways meet, m (R4).</summary>
        public const float Radius = 2f;

        /// <summary>How far a carriageway is carried into one it meets, at most, m: through the pavement between.</summary>
        public const float Reach = 6f;

        /// <summary>A carriageway is carried into another only if their ground differs by less than this, m (not through the Ridge's wall).</summary>
        public const float SameLevel = 1f;

        /// <summary>The kerb stone's top along the carriageway, m.</summary>
        public const float KerbTop = 0.3f;

        /// <summary>
        /// A tree pit, m (authoring defaults): 1.6 m square outside, flush on the kerb, so a ring of earth shows round the
        /// fruit tree's trunk, which is 1.1 m across at its foot at 1.5 times its height (measured, pt6 pit views); the
        /// trunk stands in its middle, moved inside its block's cell. Its stone edge is 0.1 m wide and as high as the kerb,
        /// its earth 0.08 m over the carriageway. Both stand clear of the terrain's level of detail, as the kerb does.
        /// </summary>
        public const float PitSize = 1.6f, PitEdge = 0.1f, PitEarth = 0.08f;

        public const string RootName = "Pavements";
        public const string MaterialFolder = "Assets/Materials/Slice";
        public const string EarthName = "Veilmouth_TreePit_Earth";

        /// <summary>The terrain layer whose earth the pits show.</summary>
        public const string EarthLayer = "Assets/Settings/TerrainLayers/Overworld/Overworld_Soil.terrainlayer";

        private const float Cell = 0.25f;
        private const float Foot = 0.15f;     // how far a kerb face goes under the ground, m
        private const float TexelMetres = 2.5f; // the kit's texture scale

        private readonly Convex[] _walks;
        private readonly Convex[] _roads;
        private readonly (Convex Kite, Vector2 O)[] _fillets;
        private readonly Func<float, float, float> _terrain;
        private readonly Dictionary<(int, int), float> _lattice = new Dictionary<(int, int), float>();
        private readonly List<Vector2> _pits = new List<Vector2>();

        /// <summary>A tree pit centred on a plan point, built with the islands.</summary>
        public void Pit(Vector2 centre) => _pits.Add(centre);

        /// <summary>The rounded corners found where carriageways meet.</summary>
        public int Fillets => _fillets.Length;

        /// <summary>The carriageways carried on into another.</summary>
        public int Carried { get; }

        /// <param name="walks">The pavement lanes, plan quads.</param>
        /// <param name="roads">The carriageways with their street's name, plan quads.</param>
        /// <param name="terrain">The ground's height at a plan point.</param>
        public VeilmouthPavements(IEnumerable<Vector2[]> walks, IEnumerable<(string Street, Vector2[] Quad)> roads, Func<float, float, float> terrain)
        {
            _terrain = terrain;
            _walks = walks.Select(q => new Convex(q)).ToArray();
            var list = roads.ToList();
            var carried = 0;
            _roads = list.Select(r =>
            {
                var quad = Carry(r, list, ref carried);
                return new Convex(quad);
            }).ToArray();
            Carried = carried;
            _fillets = FindFillets(_roads).ToArray();
        }

        // ------------------------------------------------------------------------------------------- the field

        /// <summary>Signed: below zero on the pavement, about the distance to its edge.</summary>
        public float Field(float px, float py)
        {
            var p = new Vector2(px, py);
            var f = Walk(p);
            return f > 2f * Cell ? f : Mathf.Max(f, -Road(p));
        }

        /// <summary>On the pavement, 5 cm or more inside its edge.</summary>
        public bool On(float px, float py) => Field(px, py) < -0.05f;

        /// <summary>In a pavement lane but taken by a carriageway or a rounded corner.</summary>
        public bool Cut(float px, float py) => Walk(new Vector2(px, py)) < 0f && Field(px, py) >= 0f;

        /// <summary>The pavement's top at a plan point.</summary>
        public float Top(float px, float py) => Height(px, py) + Kerb;

        private float Walk(Vector2 p)
        {
            var f = float.MaxValue;
            foreach (var w in _walks)
            {
                f = Mathf.Min(f, w.Distance(p));
            }

            return f;
        }

        private float Road(Vector2 p)
        {
            var c = float.MaxValue;
            foreach (var r in _roads)
            {
                c = Mathf.Min(c, r.Distance(p));
            }

            foreach (var (kite, o) in _fillets)
            {
                c = Mathf.Min(c, Mathf.Max(kite.Distance(p), Radius - Vector2.Distance(p, o)));
            }

            return c;
        }

        /// <summary>The ground, sampled once a metre on a lattice offset by 0.125 m and blended between.</summary>
        private float Height(float px, float py)
        {
            var u = px - 0.125f;
            var v = py - 0.125f;
            var i = Mathf.FloorToInt(u);
            var j = Mathf.FloorToInt(v);
            var fu = u - i;
            var fv = v - j;
            float At(int a, int b)
            {
                if (!_lattice.TryGetValue((a, b), out var h))
                {
                    h = _terrain(a + 0.125f, b + 0.125f);
                    _lattice[(a, b)] = h;
                }

                return h;
            }

            return Mathf.Lerp(Mathf.Lerp(At(i, j), At(i + 1, j), fu), Mathf.Lerp(At(i, j + 1), At(i + 1, j + 1), fu), fv);
        }

        // ------------------------------------------------------------------------------------------- carriageways

        /// <summary>A carriageway carried on at each end into another street's carriageway it meets, to that one's middle.</summary>
        private Vector2[] Carry((string Street, Vector2[] Quad) road, List<(string Street, Vector2[] Quad)> all, ref int carried)
        {
            var q = road.Quad;
            if (!Axis(q))
            {
                return q;
            }

            var lo = new Vector2(q.Min(p => p.x), q.Min(p => p.y));
            var hi = new Vector2(q.Max(p => p.x), q.Max(p => p.y));
            var alongX = hi.x - lo.x > hi.y - lo.y;
            var mid = (lo + hi) / 2f;
            foreach (var high in new[] { false, true })
            {
                var end = alongX ? (high ? hi.x : lo.x) : (high ? hi.y : lo.y);
                var dir = high ? 1f : -1f;
                var probe = alongX
                    ? Rect(Mathf.Min(end, end + (dir * Reach)), lo.y, Mathf.Max(end, end + (dir * Reach)), hi.y)
                    : Rect(lo.x, Mathf.Min(end, end + (dir * Reach)), hi.x, Mathf.Max(end, end + (dir * Reach)));
                var met = all.Where(o => o.Street != road.Street && Overlap(o.Quad, probe)).ToList();
                if (met.Count == 0)
                {
                    continue;
                }

                var other = met.OrderBy(o => Mathf.Abs((alongX ? Centre(o.Quad).x : Centre(o.Quad).y) - end)).First();
                var target = Axis(other.Quad)
                    ? (alongX ? Centre(other.Quad).x : Centre(other.Quad).y)
                    : end + (dir * Reach);
                if ((target - end) * dir <= 0f)
                {
                    continue;
                }

                target = end + (dir * Mathf.Min(Reach, (target - end) * dir));
                var inside = end - (dir * 0.5f);
                var here = alongX ? _terrain(inside, mid.y) : _terrain(mid.x, inside);
                var there = alongX ? _terrain(target, mid.y) : _terrain(mid.x, target);
                if (Mathf.Abs(here - there) >= SameLevel)
                {
                    continue;
                }

                if (alongX)
                {
                    if (high) hi.x = target; else lo.x = target;
                }
                else
                {
                    if (high) hi.y = target; else lo.y = target;
                }

                carried++;
            }

            return Rect(lo.x, lo.y, hi.x, hi.y);
        }

        /// <summary>
        /// A rounded corner wherever two carriageways' edges cross: the wedge outside both, between their edges and a
        /// circle of <see cref="Radius"/> touching both.
        /// </summary>
        private static IEnumerable<(Convex, Vector2)> FindFillets(Convex[] roads)
        {
            for (var i = 0; i < roads.Length; i++)
            {
                for (var j = i + 1; j < roads.Length; j++)
                {
                    var a = roads[i];
                    var b = roads[j];
                    for (var ea = 0; ea < a.P.Length; ea++)
                    {
                        for (var eb = 0; eb < b.P.Length; eb++)
                        {
                            var a0 = a.P[ea];
                            var a1 = a.P[(ea + 1) % a.P.Length];
                            var b0 = b.P[eb];
                            var b1 = b.P[(eb + 1) % b.P.Length];
                            if (!Cross(a0, a1, b0, b1, out var c))
                            {
                                continue;
                            }

                            var others = roads.Where((r, k) => k != i && k != j);
                            if (others.Any(r => r.Distance(c) < -0.01f))
                            {
                                continue; // inside a third carriageway: no corner
                            }

                            var na = a.N[ea];
                            var nb = b.N[eb];
                            var k2 = Vector2.Dot(na, nb);
                            if (k2 <= -0.99f)
                            {
                                continue;
                            }

                            var o = c + (Radius * (na + nb) / (1f + k2));
                            var ta = o - (Radius * na);
                            var tb = o - (Radius * nb);
                            if (!OnSegment(ta, a0, a1) || !OnSegment(tb, b0, b1))
                            {
                                continue; // the corner would run past an edge's end
                            }

                            yield return (new Convex(new[] { c, ta, o, tb }), o);
                        }
                    }
                }
            }
        }

        private static bool Cross(Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1, out Vector2 at)
        {
            at = default;
            var r = a1 - a0;
            var s = b1 - b0;
            var d = (r.x * s.y) - (r.y * s.x);
            if (Mathf.Abs(d) < 1e-6f)
            {
                return false;
            }

            var w = b0 - a0;
            var t = ((w.x * s.y) - (w.y * s.x)) / d;
            var u = ((w.x * r.y) - (w.y * r.x)) / d;
            const float e = 1e-4f;
            if (t <= e || t >= 1f - e || u <= e || u >= 1f - e)
            {
                return false;
            }

            at = a0 + (t * r);
            return true;
        }

        private static bool OnSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var d = b - a;
            var t = Vector2.Dot(p - a, d) / d.sqrMagnitude;
            return t >= -1e-3f && t <= 1f + 1e-3f;
        }

        // ------------------------------------------------------------------------------------------- the mesh

        /// <summary>What <see cref="Build"/> made.</summary>
        public readonly struct Report
        {
            public readonly int Triangles, Islands, Slabs, Pits;
            public readonly float KerbLength, Area;

            public Report(int triangles, int islands, int slabs, int pits, float kerbLength, float area)
            {
                Triangles = triangles;
                Islands = islands;
                Slabs = slabs;
                Pits = pits;
                KerbLength = kerbLength;
                Area = area;
            }
        }

        private struct V
        {
            public Vector2 At;
            public float P, C;

            public V(Vector2 at, float p, float c)
            {
                At = at;
                P = p;
                C = c;
            }

            public static V Lerp(V a, V b, float t) => new V(Vector2.Lerp(a.At, b.At, t), Mathf.Lerp(a.P, b.P, t), Mathf.Lerp(a.C, b.C, t));
        }

        private sealed class Part
        {
            public readonly List<Vector3> V = new List<Vector3>();
            public readonly List<Vector3> N = new List<Vector3>();
            public readonly List<Vector2> UV = new List<Vector2>();
            public readonly List<int> I = new List<int>();
        }

        /// <summary>
        /// Builds the islands under <paramref name="parent"/>: one mesh of slabs, kerb stone tops and kerb faces with its
        /// collider, and the tree pits apart with theirs. A kerb face against a house is left out.
        /// </summary>
        public Report Build(Transform parent, IReadOnlyList<Vector2[]> houses, string meshFolder)
        {
            var lo = new Vector2(_walks.Min(w => w.Min.x), _walks.Min(w => w.Min.y));
            var hi = new Vector2(_walks.Max(w => w.Max.x), _walks.Max(w => w.Max.y));
            var x0 = Mathf.Floor(lo.x) - 1f;
            var y0 = Mathf.Floor(lo.y) - 1f;
            // Whole slabs of 4 × 4 cells sit on the height lattice: (4k + 0.5) cells = k + 0.125 m.
            var nx = (Mathf.CeilToInt((hi.x - x0 + 1f) / Cell / 4f) * 4) + 1;
            var ny = (Mathf.CeilToInt((hi.y - y0 + 1f) / Cell / 4f) * 4) + 1;
            Vector2 At(int i, int j) => new Vector2(x0 + ((i + 0.5f) * Cell), y0 + ((j + 0.5f) * Cell));

            var pv = new float[nx, ny];
            var cv = new float[nx, ny];
            for (var j = 0; j < ny; j++)
            {
                for (var i = 0; i < nx; i++)
                {
                    var p = At(i, j);
                    var f = Walk(p);
                    if (f > 2f * Cell)
                    {
                        pv[i, j] = f;
                        cv[i, j] = 99f;
                        continue;
                    }

                    var c = Road(p);
                    cv[i, j] = c;
                    pv[i, j] = Mathf.Max(f, -c);
                }
            }

            var slabs = new Part();
            var stone = new Part();
            var done = new bool[nx - 1, ny - 1];
            var slabCount = 0;
            var area = 0f;

            // Whole slabs first, where all 25 samples are on the pavement and clear of the kerb stone.
            for (var bj = 0; bj + 4 < ny; bj += 4)
            {
                for (var bi = 0; bi + 4 < nx; bi += 4)
                {
                    var whole = true;
                    for (var j = bj; j <= bj + 4 && whole; j++)
                    {
                        for (var i = bi; i <= bi + 4 && whole; i++)
                        {
                            whole = pv[i, j] < 0f && cv[i, j] >= KerbTop;
                        }
                    }

                    if (!whole)
                    {
                        continue;
                    }

                    Top(slabs, new List<Vector2> { At(bi, bj), At(bi + 4, bj), At(bi + 4, bj + 4), At(bi, bj + 4) });
                    slabCount++;
                    area += 1f;
                    for (var j = bj; j < bj + 4; j++)
                    {
                        for (var i = bi; i < bi + 4; i++)
                        {
                            done[i, j] = true;
                        }
                    }
                }
            }

            // The cells left: clipped to the pavement, then split into slabs and the kerb stone's top.
            var segments = new List<(long KeyA, long KeyB, Vector2 A, Vector2 B)>();
            for (var j = 0; j < ny - 1; j++)
            {
                for (var i = 0; i < nx - 1; i++)
                {
                    var corners = new[]
                    {
                        new V(At(i, j), pv[i, j], cv[i, j]), new V(At(i + 1, j), pv[i + 1, j], cv[i + 1, j]),
                        new V(At(i + 1, j + 1), pv[i + 1, j + 1], cv[i + 1, j + 1]), new V(At(i, j + 1), pv[i, j + 1], cv[i, j + 1]),
                    };
                    var inside = corners.Select(c => c.P < 0f).ToArray();
                    if (!inside.Any(b => b))
                    {
                        continue;
                    }

                    if (!inside.All(b => b))
                    {
                        CellEdge(i, j, nx, corners, inside, segments);
                    }

                    if (done[i, j])
                    {
                        continue;
                    }

                    var poly = Clip(corners.ToList(), v => v.P, true);
                    var slab = Clip(poly, v => v.C - KerbTop, false);
                    var band = Clip(poly, v => v.C - KerbTop, true);
                    area += Area(poly);
                    if (slab.Count >= 3)
                    {
                        Top(slabs, slab.Select(v => v.At).ToList());
                    }

                    if (band.Count >= 3)
                    {
                        Top(stone, band.Select(v => v.At).ToList());
                    }
                }
            }

            // The kerbs: the outline chained into lines, a face down each.
            var lines = Chain(segments);
            var shapes = houses.Select(h => new Convex(h)).ToArray();
            var kerbLength = 0f;
            foreach (var line in lines)
            {
                kerbLength += Edge(line, shapes, stone);
            }

            var root = new GameObject(RootName);
            root.transform.SetParent(parent, false);
            SliceKitCatalogue.SetLayer(root, KitLayers.Ground);
            var island = Finish(root.transform, "Pavement Islands", $"{meshFolder}/Pavement Islands.asset",
                new[] { (slabs, "VIC_Ground_Pave"), (stone, "VIC_Stone_Medium") }, KitLayers.Ground);
            island.AddComponent<MeshCollider>().sharedMesh = island.GetComponent<MeshFilter>().sharedMesh;
            AssetDatabase.DeleteAsset($"{meshFolder}/Wet Gutters.asset");

            // The tree pits: a stone edge of four boxes round the earth.
            var edge = new Part();
            var earth = new Part();
            foreach (var c in _pits)
            {
                const float h = PitSize / 2f, i = h - PitEdge;
                Box(edge, c + new Vector2(-h, -h), c + new Vector2(h, -i));
                Box(edge, c + new Vector2(-h, i), c + new Vector2(h, h));
                Box(edge, c + new Vector2(-h, -i), c + new Vector2(-i, i));
                Box(edge, c + new Vector2(i, -i), c + new Vector2(h, i));
                Top(earth, new List<Vector2> { c + new Vector2(-i, -i), c + new Vector2(i, -i), c + new Vector2(i, i), c + new Vector2(-i, i) }, PitEarth);
            }

            if (_pits.Count > 0)
            {
                var pits = Finish(root.transform, "Tree Pits", $"{meshFolder}/Tree Pits.asset",
                    new[] { (edge, "VIC_Stone_Medium"), (earth, (string)null) }, KitLayers.Ground);
                var materials = pits.GetComponent<MeshRenderer>().sharedMaterials;
                materials[1] = EarthMaterial();
                pits.GetComponent<MeshRenderer>().sharedMaterials = materials;
                pits.AddComponent<MeshCollider>().sharedMesh = pits.GetComponent<MeshFilter>().sharedMesh;
            }

            var triangles = (slabs.I.Count + stone.I.Count) / 3;
            return new Report(triangles, lines.Count, slabCount, _pits.Count, kerbLength, area);
        }

        /// <summary>A stone box on the plan rectangle, from under the ground to the kerb's height, its top and four sides.</summary>
        private void Box(Part part, Vector2 lo, Vector2 hi)
        {
            var p = new[] { lo, new Vector2(hi.x, lo.y), hi, new Vector2(lo.x, hi.y) };
            Top(part, p.ToList(), Kerb);
            for (var k = 0; k < 4; k++)
            {
                var a = p[k];
                var b = p[(k + 1) % 4];
                var d = b - a;
                var outward = new Vector2(d.y, -d.x).normalized;
                if (Vector2.Dot(outward, a - ((lo + hi) / 2f)) < 0f)
                {
                    outward = -outward;
                }

                float ha = Height(a.x, a.y), hb = Height(b.x, b.y), length = d.magnitude;
                Quad(part, World(a, ha - Foot), World(b, hb - Foot), World(b, hb + Kerb), World(a, ha + Kerb),
                    Normal(outward), Normal(outward), Normal(outward), Normal(outward),
                    Vector2.zero, new Vector2(length, 0f), new Vector2(length, Foot + Kerb), new Vector2(0f, Foot + Kerb), outward);
            }
        }

        /// <summary>The crossings on a cell's edges, paired round each outside corner, as outline segments.</summary>
        private void CellEdge(int i, int j, int nx, V[] c, bool[] inside, List<(long, long, Vector2, Vector2)> segments)
        {
            // Edge keys: 0 the edge (i,j)–(i+1,j), 1 (i+1,j)–(i+1,j+1), 2 (i,j+1)–(i+1,j+1), 3 (i,j)–(i,j+1).
            long Key(int k) => k switch
            {
                0 => 2L * ((long)j * nx + i),
                1 => (2L * ((long)j * nx + i + 1)) + 1,
                2 => 2L * ((long)(j + 1) * nx + i),
                _ => (2L * ((long)j * nx + i)) + 1,
            };

            // The crossing on a grid edge, always from its lower corner, so both cells find the same point.
            Vector2 On(int k)
            {
                var (a, b) = k switch { 0 => (c[0], c[1]), 1 => (c[1], c[2]), 2 => (c[3], c[2]), _ => (c[0], c[3]) };
                return Vector2.Lerp(a.At, b.At, a.P / (a.P - b.P));
            }

            // Walk round the cell; a leaving crossing pairs with the next entering one.
            var events = new List<(bool Leaving, int Edge)>();
            for (var k = 0; k < 4; k++)
            {
                var from = inside[k];
                var to = inside[(k + 1) % 4];
                if (from != to)
                {
                    events.Add((from, k));
                }
            }

            for (var e = 0; e < events.Count; e++)
            {
                if (!events[e].Leaving)
                {
                    continue;
                }

                var next = events[(e + 1) % events.Count];
                // Walked round every cell the same way, a leaving crossing to the next entering one always has the
                // pavement on its left (plan x east, y south), so the lines chain one way. A test of the field beside the
                // segment would fail at a corner, where the segment cuts across inside the pavement.
                segments.Add((Key(events[e].Edge), Key(next.Edge), On(events[e].Edge), On(next.Edge)));
            }
        }

        /// <summary>The outline's segments chained into lines; every island's outline closes on itself.</summary>
        private static List<List<Vector2>> Chain(List<(long KeyA, long KeyB, Vector2 A, Vector2 B)> segments)
        {
            var from = new Dictionary<long, int>();
            for (var s = 0; s < segments.Count; s++)
            {
                from[segments[s].KeyA] = s;
            }

            var used = new bool[segments.Count];
            var lines = new List<List<Vector2>>();
            for (var s = 0; s < segments.Count; s++)
            {
                if (used[s])
                {
                    continue;
                }

                var line = new List<Vector2> { segments[s].A };
                var k = s;
                while (k >= 0 && !used[k])
                {
                    used[k] = true;
                    line.Add(segments[k].B);
                    k = from.TryGetValue(segments[k].KeyB, out var next) ? next : -1;
                }

                lines.Add(line);
            }

            return lines;
        }

        /// <summary>One outline's kerb faces. Returns their length.</summary>
        private float Edge(List<Vector2> line, Convex[] houses, Part stone)
        {
            var closed = line.Count > 3 && (line[0] - line[line.Count - 1]).sqrMagnitude < 1e-6f;
            var n = line.Count - 1;
            var normals = new Vector2[n];
            for (var s = 0; s < n; s++)
            {
                var d = line[s + 1] - line[s];
                normals[s] = new Vector2(d.y, -d.x).normalized; // the right side, away from the pavement
            }

            // A vertex's normal: the mean of its two segments' where they turn by less than 35°, so arcs read round.
            Vector2 Smooth(int s, bool end)
            {
                var other = end ? s + 1 : s - 1;
                if (closed)
                {
                    other = (other + n) % n;
                }

                if (other < 0 || other >= n || Vector2.Angle(normals[s], normals[other]) > 35f)
                {
                    return normals[s];
                }

                return (normals[s] + normals[other]).normalized;
            }

            float kerb = 0f, along = 0f;
            for (var s = 0; s < n; s++)
            {
                var a = line[s];
                var b = line[s + 1];
                var length = Vector2.Distance(a, b);
                if (length < 1e-5f)
                {
                    continue; // the outline passed through a sample point
                }

                var mid = (a + b) / 2f;
                var probe = mid + (normals[s] * 0.3f);
                var hidden = houses.Any(h => probe.x > h.Min.x && probe.x < h.Max.x && probe.y > h.Min.y && probe.y < h.Max.y && h.Distance(probe) < 0f);
                var na = Smooth(s, false);
                var nb = Smooth(s, true);
                if (!hidden)
                {
                    var ha = Height(a.x, a.y);
                    var hb = Height(b.x, b.y);
                    Quad(stone,
                        World(a, ha - Foot), World(b, hb - Foot), World(b, hb + Kerb), World(a, ha + Kerb),
                        Normal(na), Normal(nb), Normal(nb), Normal(na),
                        new Vector2(along, 0f), new Vector2(along + length, 0f),
                        new Vector2(along + length, Foot + Kerb), new Vector2(along, Foot + Kerb), normals[s]);
                    kerb += length;
                }

                along += length;
            }

            return kerb;
        }

        private static Vector3 World(Vector2 plan, float y) => new Vector3(plan.x - 500f, y, 500f - plan.y);

        private static Vector3 Normal(Vector2 plan) => new Vector3(plan.x, 0f, -plan.y);

        /// <summary>A quad, turned to face <paramref name="facing"/> (plan) when given, else up. UVs in metres are scaled to the kit's.</summary>
        private static void Quad(Part part, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 na, Vector3 nb, Vector3 nc, Vector3 nd,
            Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, Vector2? facing)
        {
            var s = part.V.Count;
            part.V.AddRange(new[] { a, b, c, d });
            part.N.AddRange(new[] { na, nb, nc, nd });
            var scale = facing.HasValue ? 1f / TexelMetres : 1f;
            part.UV.AddRange(new[] { ua * scale, ub * scale, uc * scale, ud * scale });
            var want = facing.HasValue ? Normal(facing.Value) : Vector3.up;
            var face = Vector3.Cross(b - a, c - a);
            if (Vector3.Dot(face, want) >= 0f)
            {
                part.I.AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3 });
            }
            else
            {
                part.I.AddRange(new[] { s, s + 2, s + 1, s, s + 3, s + 2 });
            }
        }

        /// <summary>A convex top polygon on the pavement's top, fanned, facing up, its normal from the ground's slope.</summary>
        private void Top(Part part, List<Vector2> poly) => Top(part, poly, Kerb);

        /// <summary>A convex top polygon <paramref name="lift"/> over the ground, fanned, facing up.</summary>
        private void Top(Part part, List<Vector2> poly, float lift)
        {
            var s = part.V.Count;
            foreach (var p in poly)
            {
                var h = Height(p.x, p.y);
                part.V.Add(World(p, h + lift));
                const float e = 0.25f;
                var gx = (Height(p.x + e, p.y) - Height(p.x - e, p.y)) / (2f * e);
                var gy = (Height(p.x, p.y + e) - Height(p.x, p.y - e)) / (2f * e);
                part.N.Add(new Vector3(-gx, 1f, gy).normalized); // plan y runs to world −Z
                part.UV.Add(new Vector2(p.x - 500f, 500f - p.y) / TexelMetres);
            }

            for (var k = 1; k < poly.Count - 1; k++)
            {
                var a = part.V[s];
                var b = part.V[s + k];
                var c = part.V[s + k + 1];
                if (Vector3.Cross(b - a, c - a).y >= 0f)
                {
                    part.I.AddRange(new[] { s, s + k, s + k + 1 });
                }
                else
                {
                    part.I.AddRange(new[] { s, s + k + 1, s + k });
                }
            }
        }

        /// <summary>Sutherland–Hodgman against a value carried on the vertices: keeps where it is below zero, or not.</summary>
        private static List<V> Clip(List<V> poly, Func<V, float> value, bool keepBelow)
        {
            var result = new List<V>();
            for (var k = 0; k < poly.Count; k++)
            {
                var a = poly[k];
                var b = poly[(k + 1) % poly.Count];
                var va = value(a);
                var vb = value(b);
                var inA = keepBelow ? va < 0f : va >= 0f;
                var inB = keepBelow ? vb < 0f : vb >= 0f;
                if (inA)
                {
                    result.Add(a);
                }

                if (inA != inB)
                {
                    result.Add(V.Lerp(a, b, va / (va - vb)));
                }
            }

            return result;
        }

        private static float Area(List<V> poly)
        {
            var sum = 0f;
            for (var k = 0; k < poly.Count; k++)
            {
                var a = poly[k].At;
                var b = poly[(k + 1) % poly.Count].At;
                sum += (a.x * b.y) - (b.x * a.y);
            }

            return Mathf.Abs(sum) / 2f;
        }

        private static GameObject Finish(Transform parent, string name, string path, (Part Part, string Material)[] parts, string layer)
        {
            KitStandIns.EnsureFolder(Path.GetDirectoryName(path)!.Replace('\\', '/'));
            // The asset's own mesh is written in place, as the city's stone works are (VeilmouthCity.Works).
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            var created = mesh == null;
            if (created)
            {
                mesh = new Mesh();
            }

            mesh.Clear();
            mesh.name = name;
            mesh.indexFormat = IndexFormat.UInt32;
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var offsets = new List<int>();
            foreach (var (part, _) in parts)
            {
                offsets.Add(v.Count);
                v.AddRange(part.V);
                n.AddRange(part.N);
                uv.AddRange(part.UV);
            }

            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.subMeshCount = parts.Length;
            for (var k = 0; k < parts.Length; k++)
            {
                var offset = offsets[k];
                mesh.SetTriangles(parts[k].Part.I.Select(i => i + offset).ToList(), k);
            }

            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            if (created)
            {
                AssetDatabase.CreateAsset(mesh, path);
            }
            else
            {
                EditorUtility.SetDirty(mesh);
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            SliceKitCatalogue.SetLayer(go, layer);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = parts
                .Select(p => p.Material == null ? null
                    : AssetDatabase.LoadAssetAtPath<Material>($"{GameDev.VictorianKit.EditorTools.VicKitPaths.MaterialFolder}/{p.Material}.mat"))
                .ToArray();
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            return go;
        }

        /// <summary>
        /// The tree pits' earth, created once and then tuned in the Inspector: URP Lit with the soil terrain layer's own
        /// texture, matt.
        /// </summary>
        private static Material EarthMaterial()
        {
            var path = $"{MaterialFolder}/{EarthName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(EarthLayer);
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = EarthName };
            if (layer != null && layer.diffuseTexture != null)
            {
                material.SetTexture("_BaseMap", layer.diffuseTexture);
                material.SetTextureScale("_BaseMap", Vector2.one * (TexelMetres / Mathf.Max(0.5f, layer.tileSize.x)));
            }

            material.SetColor("_BaseColor", new Color(0.62f, 0.55f, 0.48f));
            material.SetFloat("_Smoothness", 0.05f);
            material.SetFloat("_Metallic", 0f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // ------------------------------------------------------------------------------------------- plan geometry

        private static Vector2[] Rect(float x0, float y0, float x1, float y1) =>
            new[] { new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x1, y1), new Vector2(x0, y1) };

        private static Vector2 Centre(Vector2[] q) => q.Aggregate(Vector2.zero, (s, p) => s + p) / q.Length;

        /// <summary>Every edge runs along x or along y.</summary>
        private static bool Axis(Vector2[] q) =>
            q.Select((p, k) => (p, q[(k + 1) % q.Length])).All(e => Mathf.Abs(e.Item1.x - e.Item2.x) < 0.01f || Mathf.Abs(e.Item1.y - e.Item2.y) < 0.01f);

        private static bool Overlap(Vector2[] a, Vector2[] b)
        {
            foreach (var poly in new[] { a, b })
            {
                for (var i = 0; i < poly.Length; i++)
                {
                    var e = poly[(i + 1) % poly.Length] - poly[i];
                    var axis = new Vector2(-e.y, e.x).normalized;
                    float minA = float.MaxValue, maxA = float.MinValue, minB = float.MaxValue, maxB = float.MinValue;
                    foreach (var p in a)
                    {
                        var d = Vector2.Dot(p, axis);
                        minA = Mathf.Min(minA, d);
                        maxA = Mathf.Max(maxA, d);
                    }

                    foreach (var p in b)
                    {
                        var d = Vector2.Dot(p, axis);
                        minB = Mathf.Min(minB, d);
                        maxB = Mathf.Max(maxB, d);
                    }

                    if (maxA <= minB + 0.01f || maxB <= minA + 0.01f)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>A convex polygon with its edges' outward normals: its signed distance is the largest over its edges.</summary>
        private sealed class Convex
        {
            public readonly Vector2[] P;
            public readonly Vector2[] N;
            private readonly float[] _d;
            public readonly Vector2 Min, Max;

            public Convex(Vector2[] points)
            {
                P = points;
                N = new Vector2[points.Length];
                _d = new float[points.Length];
                var centre = Centre(points);
                for (var k = 0; k < points.Length; k++)
                {
                    var a = points[k];
                    var b = points[(k + 1) % points.Length];
                    var e = b - a;
                    var n = new Vector2(e.y, -e.x).normalized;
                    if (Vector2.Dot(n, ((a + b) / 2f) - centre) < 0f)
                    {
                        n = -n;
                    }

                    N[k] = n;
                    _d[k] = Vector2.Dot(n, a);
                }

                Min = new Vector2(points.Min(p => p.x), points.Min(p => p.y));
                Max = new Vector2(points.Max(p => p.x), points.Max(p => p.y));
            }

            public float Distance(Vector2 p)
            {
                var d = float.MinValue;
                for (var k = 0; k < N.Length; k++)
                {
                    d = Mathf.Max(d, Vector2.Dot(N[k], p) - _d[k]);
                }

                return d;
            }
        }
    }
}
