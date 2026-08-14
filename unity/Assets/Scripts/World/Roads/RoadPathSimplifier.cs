using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Roads
{
    /// <summary>
    /// Softens A* polylines into natural curves: light stair cleanup + Chaikin corner-cutting that
    /// refuses to cut through lakes/mountains. Not a taut string-pull (that made artificial chords).
    /// </summary>
    public static class RoadPathSimplifier
    {
        /// <summary>Set from WorldGenSettings before planning; metres of preferred turn radius.</summary>
        public static float ActiveMinTurnRadiusMeters = 18f;

        /// <summary>Max heading change per sample step (degrees). Derived from turn radius.</summary>
        public static float ActiveMaxHeadingChangeDegrees = 28f;

        public static List<Vector2> SoftenNatural(
            IReadOnlyList<Vector2> path,
            Func<float, float, float> costAtWorld,
            float sampleStepMeters)
        {
            var result = new List<Vector2>();
            if (path == null || path.Count == 0)
            {
                return result;
            }

            if (path.Count <= 2 || costAtWorld == null)
            {
                result.AddRange(path);
                return result;
            }

            var cleaned = CollapseGridStairs(path, costAtWorld, sampleStepMeters);
            var softened = ChaikinSafe(cleaned, costAtWorld, sampleStepMeters, iterations: 2);
            return LimitHeadingChange(softened, costAtWorld, sampleStepMeters);
        }

        /// <summary>
        /// Soft-clamp sharp corners: if heading change exceeds the limit for the segment length,
        /// insert a midpoint (when the chord stays clear) so the road arcs instead of kinking.
        /// </summary>
        public static List<Vector2> LimitHeadingChange(
            IReadOnlyList<Vector2> path,
            Func<float, float, float> costAtWorld,
            float sampleStepMeters)
        {
            if (path == null || path.Count < 3)
            {
                return path == null ? new List<Vector2>() : new List<Vector2>(path);
            }

            var turnR = Mathf.Max(ActiveMinTurnRadiusMeters, 1f);
            var maxDeg = Mathf.Clamp(ActiveMaxHeadingChangeDegrees, 5f, 90f);
            var step = Mathf.Max(sampleStepMeters, 1f);
            var pts = new List<Vector2>(path);

            for (var pass = 0; pass < 2; pass++)
            {
                var next = new List<Vector2> { pts[0] };
                for (var i = 1; i < pts.Count - 1; i++)
                {
                    var a = pts[i - 1];
                    var b = pts[i];
                    var c = pts[i + 1];
                    var ab = b - a;
                    var bc = c - b;
                    var len0 = ab.magnitude;
                    var len1 = bc.magnitude;
                    if (len0 < 0.01f || len1 < 0.01f)
                    {
                        next.Add(b);
                        continue;
                    }

                    var ang = Vector2.Angle(ab, bc);
                    var maxForLen = maxDeg * Mathf.Clamp01(Mathf.Min(len0, len1) / turnR);
                    maxForLen = Mathf.Max(maxForLen, 8f);
                    if (ang <= maxForLen + 0.5f)
                    {
                        next.Add(b);
                        continue;
                    }

                    // Split the corner with a chord midpoint when clear.
                    var mid = (a + c) * 0.5f;
                    if (SegmentClear(a, mid, costAtWorld, step) &&
                        SegmentClear(mid, c, costAtWorld, step))
                    {
                        next.Add(Vector2.Lerp(b, mid, 0.55f));
                    }
                    else
                    {
                        next.Add(b);
                    }
                }

                next.Add(pts[pts.Count - 1]);
                pts = next;
            }

            return pts;
        }

        /// <summary>Legacy name — now softens rather than taut-pulling.</summary>
        public static List<Vector2> StringPull(
            IReadOnlyList<Vector2> path,
            Func<float, float, float> costAtWorld,
            float sampleStepMeters) =>
            SoftenNatural(path, costAtWorld, sampleStepMeters);

        /// <summary>
        /// Drop intermediate vertices that lie nearly on the chord of neighbours when the chord
        /// stays clear — removes A* stair noise without long artificial shortcuts.
        /// </summary>
        public static List<Vector2> CollapseGridStairs(
            IReadOnlyList<Vector2> path,
            Func<float, float, float> costAtWorld,
            float sampleStepMeters)
        {
            var pts = new List<Vector2>(path);
            if (pts.Count < 3)
            {
                return pts;
            }

            var step = Mathf.Max(sampleStepMeters, 1f);
            var maxChord = step * 3.5f;
            var keep = new bool[pts.Count];
            keep[0] = true;
            keep[pts.Count - 1] = true;

            for (var i = 1; i < pts.Count - 1; i++)
            {
                keep[i] = true;
            }

            for (var i = 1; i < pts.Count - 1; i++)
            {
                var a = pts[i - 1];
                var b = pts[i];
                var c = pts[i + 1];
                var chord = Vector2.Distance(a, c);
                if (chord > maxChord)
                {
                    continue;
                }

                var ab = b - a;
                var ac = c - a;
                var cross = Mathf.Abs(ab.x * ac.y - ab.y * ac.x);
                var area = cross * 0.5f;
                if (area > step * step * 0.35f)
                {
                    continue;
                }

                if (SegmentClear(a, c, costAtWorld, step))
                {
                    keep[i] = false;
                }
            }

            var result = new List<Vector2>();
            for (var i = 0; i < pts.Count; i++)
            {
                if (keep[i])
                {
                    result.Add(pts[i]);
                }
            }

            return result.Count >= 2 ? result : new List<Vector2>(path);
        }

        public static List<Vector2> ChaikinSafe(
            IReadOnlyList<Vector2> path,
            Func<float, float, float> costAtWorld,
            float sampleStepMeters,
            int iterations)
        {
            var cur = new List<Vector2>(path);
            var step = Mathf.Max(sampleStepMeters, 1f);
            var n = Mathf.Clamp(iterations, 0, 4);
            for (var iter = 0; iter < n; iter++)
            {
                if (cur.Count < 3)
                {
                    break;
                }

                var next = new List<Vector2> { cur[0] };
                for (var i = 0; i < cur.Count - 1; i++)
                {
                    var a = cur[i];
                    var b = cur[i + 1];
                    var q = Vector2.Lerp(a, b, 0.25f);
                    var r = Vector2.Lerp(a, b, 0.75f);
                    if (!SegmentClear(a, b, costAtWorld, step) ||
                        RoadCost.IsHardObstacleCost(costAtWorld(q.x, q.y)) ||
                        RoadCost.IsHardObstacleCost(costAtWorld(r.x, r.y)))
                    {
                        // Keep the original corner — do not cut into an obstacle.
                        if (i > 0)
                        {
                            next.Add(a);
                        }

                        continue;
                    }

                    next.Add(q);
                    next.Add(r);
                }

                next.Add(cur[cur.Count - 1]);
                cur = next;
            }

            return cur;
        }

        public static bool SegmentClear(
            Vector2 a,
            Vector2 b,
            Func<float, float, float> costAtWorld,
            float sampleStepMeters)
        {
            var dist = Vector2.Distance(a, b);
            if (dist < 0.01f)
            {
                return !RoadCost.IsHardObstacleCost(costAtWorld(a.x, a.y));
            }

            var step = Mathf.Max(sampleStepMeters, 1f);
            var samples = Mathf.Max(1, Mathf.CeilToInt(dist / step));
            for (var s = 0; s <= samples; s++)
            {
                var t = s / (float)samples;
                var p = Vector2.Lerp(a, b, t);
                if (RoadCost.IsHardObstacleCost(costAtWorld(p.x, p.y)))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Closest point on a polyline to <paramref name="query"/> (XZ).</summary>
        public static Vector2 ClosestPointOnPolyline(IReadOnlyList<Vector2> line, Vector2 query)
        {
            if (line == null || line.Count == 0)
            {
                return query;
            }

            if (line.Count == 1)
            {
                return line[0];
            }

            var best = line[0];
            var bestDist = float.PositiveInfinity;
            for (var i = 0; i < line.Count - 1; i++)
            {
                var a = line[i];
                var b = line[i + 1];
                var ab = b - a;
                var lenSq = ab.sqrMagnitude;
                var t = lenSq < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(query - a, ab) / lenSq);
                var p = a + ab * t;
                var d = Vector2.Distance(query, p);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = p;
                }
            }

            return best;
        }
    }
}
