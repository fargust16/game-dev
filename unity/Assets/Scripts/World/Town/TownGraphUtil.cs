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
