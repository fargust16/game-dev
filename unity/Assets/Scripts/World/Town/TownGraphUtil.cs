using System.Collections.Generic;
using GameDev.World.Town.Geometry;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Town
{
    /// <summary>Shared helpers for stage pipeline (deterministic, list-order safe).</summary>
    public static class TownGraphUtil
    {
        public static int AddNode(TownModel model, Vector2 pos)
        {
            var node = new RoadNode(pos);
            model.RoadNodes.Add(node);
            return model.RoadNodes.Count - 1;
        }

        /// <summary>
        /// Reuse an existing road node within <paramref name="snapMetres"/>, else create one.
        /// Keeps the street graph connected across splits / arterials.
        /// </summary>
        public static int FindOrAddNode(TownModel model, Vector2 pos, float snapMetres = 10f)
        {
            var snapSq = snapMetres * snapMetres;
            var best = -1;
            var bestD = snapSq;
            for (var i = 0; i < model.RoadNodes.Count; i++)
            {
                var d = (model.RoadNodes[i].Pos - pos).sqrMagnitude;
                if (d <= bestD)
                {
                    bestD = d;
                    best = i;
                }
            }

            return best >= 0 ? best : AddNode(model, pos);
        }

        /// <summary>
        /// Collapse nodes closer than <paramref name="snapMetres"/> so T-junctions share vertices.
        /// Deterministic: always keep the lower index.
        /// </summary>
        public static void MergeNearbyNodes(TownModel model, float snapMetres)
        {
            if (model.RoadNodes.Count < 2 || snapMetres <= 0f)
            {
                return;
            }

            var snapSq = snapMetres * snapMetres;
            var map = new int[model.RoadNodes.Count];
            for (var i = 0; i < map.Length; i++)
            {
                map[i] = i;
            }

            int Find(int i)
            {
                while (map[i] != i)
                {
                    map[i] = map[map[i]];
                    i = map[i];
                }

                return i;
            }

            for (var i = 0; i < model.RoadNodes.Count; i++)
            {
                for (var j = i + 1; j < model.RoadNodes.Count; j++)
                {
                    if ((model.RoadNodes[i].Pos - model.RoadNodes[j].Pos).sqrMagnitude > snapSq)
                    {
                        continue;
                    }

                    var a = Find(i);
                    var b = Find(j);
                    if (a != b)
                    {
                        map[Mathf.Max(a, b)] = Mathf.Min(a, b);
                    }
                }
            }

            for (var e = 0; e < model.RoadEdges.Count; e++)
            {
                var edge = model.RoadEdges[e];
                edge.NodeA = Find(edge.NodeA);
                edge.NodeB = Find(edge.NodeB);
                if (edge.Polyline != null && edge.Polyline.Count >= 2)
                {
                    edge.Polyline[0] = model.RoadNodes[edge.NodeA].Pos;
                    edge.Polyline[edge.Polyline.Count - 1] = model.RoadNodes[edge.NodeB].Pos;
                }
            }

            // Rebuild adjacency lists (list order = edge index order).
            for (var i = 0; i < model.RoadNodes.Count; i++)
            {
                model.RoadNodes[i].Edges.Clear();
            }

            for (var e = 0; e < model.RoadEdges.Count; e++)
            {
                var edge = model.RoadEdges[e];
                if (edge.NodeA == edge.NodeB)
                {
                    continue;
                }

                model.RoadNodes[edge.NodeA].Edges.Add(e);
                model.RoadNodes[edge.NodeB].Edges.Add(e);
            }
        }

        public static int AddEdge(
            TownModel model,
            int nodeA,
            int nodeB,
            List<Vector2> polyline,
            RoadClass cls,
            float width,
            bool deadEnd = false,
            bool shortcut = false)
        {
            if (nodeA == nodeB)
            {
                return -1;
            }

            var edge = new RoadEdge
            {
                NodeA = nodeA,
                NodeB = nodeB,
                Class = cls,
                Width = width,
                DeadEnd = deadEnd,
                Shortcut = shortcut
            };
            edge.Polyline.AddRange(polyline);
            model.RoadEdges.Add(edge);
            var ei = model.RoadEdges.Count - 1;
            model.RoadNodes[nodeA].Edges.Add(ei);
            model.RoadNodes[nodeB].Edges.Add(ei);
            return ei;
        }

        /// <summary>
        /// Mild corridor polyline along a split chord. Jitter is capped so the
        /// centreline stays near the shared block edge (does not wander through fabric).
        /// </summary>
        public static List<Vector2> BuildCorridorPolyline(
            Vector2 from,
            Vector2 to,
            RoadClassSettings settings,
            Random rng)
        {
            var delta = to - from;
            var len = delta.magnitude;
            if (len < 0.5f)
            {
                return new List<Vector2> { from, to };
            }

            var dir = delta / len;
            var perp = new Vector2(-dir.y, dir.x);
            var maxJitter = Mathf.Min(1.2f, settings.JitterAmplitude);
            var controls = new List<Vector2>(4) { from };
            var mids = len > 36f ? 2 : 1;
            for (var i = 1; i <= mids; i++)
            {
                var t = i / (float)(mids + 1);
                var jitter = NextRange(rng, -maxJitter, maxJitter);
                controls.Add(Vector2.Lerp(from, to, t) + perp * jitter);
            }

            controls.Add(to);
            var smooth = Geo2.SmoothCatmullRom(controls, tension: 0.4f, samples: 4);
            return Geo2.ResamplePolyline(smooth, spacing: 3f);
        }

        public static float NextRange(Random rng, float min, float max)
        {
            if (max <= min)
            {
                return min;
            }

            return min + (float)rng.NextDouble() * (max - min);
        }

        public static int WeightedIndex(Random rng, float[] weights)
        {
            var sum = 0f;
            for (var i = 0; i < weights.Length; i++)
            {
                sum += Mathf.Max(0f, weights[i]);
            }

            if (sum <= 0f)
            {
                return 0;
            }

            var roll = (float)rng.NextDouble() * sum;
            var acc = 0f;
            for (var i = 0; i < weights.Length; i++)
            {
                acc += Mathf.Max(0f, weights[i]);
                if (roll <= acc)
                {
                    return i;
                }
            }

            return weights.Length - 1;
        }

        public static List<Vector2> MakeRegularPolygon(Vector2 center, float radius, int sides, float angleOffset = 0f)
        {
            var verts = new List<Vector2>(sides);
            for (var i = 0; i < sides; i++)
            {
                var a = angleOffset + i * (Mathf.PI * 2f / sides);
                verts.Add(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
            }

            return verts;
        }

        public static List<Vector2> MakeIrregularDisk(
            Vector2 center,
            float radius,
            float irregularity,
            int sides,
            Random rng)
        {
            var verts = new List<Vector2>(sides);
            for (var i = 0; i < sides; i++)
            {
                var a = i * (Mathf.PI * 2f / sides);
                var r = radius + NextRange(rng, -irregularity, irregularity);
                r = Mathf.Max(radius * 0.55f, r);
                verts.Add(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }

            return Geo2.ChaikinSmooth(verts, 1);
        }

        public static Vector2 PointOnBoundary(Polygon2 bounds, float t01)
        {
            var verts = bounds.Vertices;
            if (verts.Count < 2)
            {
                return Vector2.zero;
            }

            var total = 0f;
            for (var i = 0; i < verts.Count; i++)
            {
                total += Vector2.Distance(verts[i], verts[(i + 1) % verts.Count]);
            }

            var target = Mathf.Clamp01(t01) * total;
            var acc = 0f;
            for (var i = 0; i < verts.Count; i++)
            {
                var a = verts[i];
                var b = verts[(i + 1) % verts.Count];
                var len = Vector2.Distance(a, b);
                if (acc + len >= target || i == verts.Count - 1)
                {
                    var u = len > 1e-5f ? (target - acc) / len : 0f;
                    return Vector2.Lerp(a, b, Mathf.Clamp01(u));
                }

                acc += len;
            }

            return verts[0];
        }

        public static List<Vector2> BuildCurvedArterial(
            Vector2 from,
            Vector2 to,
            RoadClassSettings settings,
            Random rng,
            int controlPoints = 4)
        {
            var controls = new List<Vector2>(controlPoints + 2) { from };
            var dir = to - from;
            var len = dir.magnitude;
            if (len < 1e-3f)
            {
                controls.Add(to);
                return controls;
            }

            dir /= len;
            var perp = new Vector2(-dir.y, dir.x);
            for (var i = 1; i <= controlPoints; i++)
            {
                var t = i / (float)(controlPoints + 1);
                var basePt = Vector2.Lerp(from, to, t);
                var curve = Mathf.Sin(t * Mathf.PI) * settings.CurvatureAmplitude;
                var side = NextRange(rng, -1f, 1f);
                var jitter = NextRange(rng, -settings.JitterAmplitude, settings.JitterAmplitude);
                controls.Add(basePt + perp * (curve * side + jitter));
            }

            controls.Add(to);
            var smooth = Geo2.SmoothCatmullRom(controls, tension: 0.15f, samples: 6);
            return Geo2.ResamplePolyline(smooth, spacing: 4f);
        }

        public static float RoadWidth(TownProfile profile, RoadClass cls, Random rng)
        {
            var s = profile.GetRoadClassSettings(cls);
            return NextRange(rng, s.WidthMin, s.WidthMax);
        }

        public static void EnsureCcW(List<Vector2> poly)
        {
            if (Geo2.PolygonArea(poly) < 0f)
            {
                poly.Reverse();
            }
        }

        public static Polygon2 ToPolygon(List<Vector2> verts)
        {
            EnsureCcW(verts);
            var p = new Polygon2(verts.Count);
            p.SetVertices(verts);
            return p;
        }
    }
}
