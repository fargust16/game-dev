using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Town.Geometry
{
    /// <summary>
    /// Static 2D geometry utilities operating in world-space XZ plane
    /// (Vector2.x → X, Vector2.y → Z).  All polygon parameters are
    /// <see cref="IReadOnlyList{T}"/> so callers can pass
    /// <see cref="List{T}"/>, arrays, or <see cref="Polygon2.Vertices"/>
    /// without extra allocation.
    /// </summary>
    public static class Geo2
    {
        private const float Epsilon = 1e-7f;

        // ================================================================
        //  Primitives
        // ================================================================

        /// <summary>2D cross product (scalar z of the 3D cross of a and b).</summary>
        public static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        /// <summary>2D dot product.</summary>
        public static float Dot(Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;

        // ================================================================
        //  Polygon area & centroid
        // ================================================================

        /// <summary>
        /// Signed polygon area via the shoelace formula.
        /// Returns positive for CCW winding, negative for CW.
        /// </summary>
        public static float PolygonArea(IReadOnlyList<Vector2> verts)
        {
            int n = verts.Count;
            if (n < 3) return 0f;
            float sum = 0f;
            for (int i = 0, j = n - 1; i < n; j = i++)
                sum += Cross(verts[j], verts[i]);
            return sum * 0.5f;
        }

        /// <summary>
        /// Polygon centroid via the standard area-weighted formula.
        /// Falls back to the average of vertices for degenerate polygons.
        /// </summary>
        public static Vector2 PolygonCentroid(IReadOnlyList<Vector2> verts)
        {
            int n = verts.Count;
            if (n == 0) return Vector2.zero;
            if (n == 1) return verts[0];
            if (n == 2) return (verts[0] + verts[1]) * 0.5f;

            float cx = 0f, cy = 0f, area = 0f;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float c = Cross(verts[j], verts[i]);
                area += c;
                cx += (verts[j].x + verts[i].x) * c;
                cy += (verts[j].y + verts[i].y) * c;
            }

            if (Mathf.Abs(area) < Epsilon)
            {
                // Degenerate: return arithmetic average
                Vector2 avg = Vector2.zero;
                for (int i = 0; i < n; i++) avg += verts[i];
                return avg / n;
            }

            float inv = 1f / (3f * area);
            return new Vector2(cx * inv, cy * inv);
        }

        // ================================================================
        //  Point-in-polygon (ray casting)
        // ================================================================

        /// <summary>
        /// Tests whether point <paramref name="p"/> lies inside <paramref name="poly"/>
        /// using the ray-casting algorithm. On-edge result is undefined (treat as outside).
        /// </summary>
        public static bool PointInPolygon(Vector2 p, IReadOnlyList<Vector2> poly)
        {
            int n = poly.Count;
            bool inside = false;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                var vi = poly[i];
                var vj = poly[j];
                if (((vi.y > p.y) != (vj.y > p.y)) &&
                    (p.x < (vj.x - vi.x) * (p.y - vi.y) / (vj.y - vi.y) + vi.x))
                    inside = !inside;
            }
            return inside;
        }

        // ================================================================
        //  Segment intersection
        // ================================================================

        /// <summary>
        /// Tests whether segment [<paramref name="a1"/>, <paramref name="a2"/>] and
        /// [<paramref name="b1"/>, <paramref name="b2"/>] intersect (endpoints exclusive).
        /// Returns the intersection point in <paramref name="pt"/> when they do.
        /// </summary>
        public static bool SegmentIntersect(
            Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 pt)
        {
            var da = a2 - a1;
            var db = b2 - b1;
            float denom = Cross(da, db);
            if (Mathf.Abs(denom) < Epsilon) { pt = a1; return false; }
            var dc = b1 - a1;
            float t = Cross(dc, db) / denom;
            float s = Cross(dc, da) / denom;
            if (t < 0f || t > 1f || s < 0f || s > 1f) { pt = a1; return false; }
            pt = a1 + da * t;
            return true;
        }

        // ------------------------------------------------------------------ private: infinite-line variant used by InsetPolygon

        private static bool LineIntersect(
            Vector2 a, Vector2 da, Vector2 b, Vector2 db, out Vector2 pt)
        {
            float denom = Cross(da, db);
            if (Mathf.Abs(denom) < Epsilon) { pt = a; return false; }
            float t = Cross(b - a, db) / denom;
            pt = a + da * t;
            return true;
        }

        // ================================================================
        //  Closest point on segment
        // ================================================================

        /// <summary>Returns the closest point on segment [<paramref name="a"/>, <paramref name="b"/>] to <paramref name="p"/>.</summary>
        public static Vector2 ClosestPointOnSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float lenSq = ab.sqrMagnitude;
            if (lenSq < Epsilon) return a;
            float t = Mathf.Clamp01(Dot(p - a, ab) / lenSq);
            return a + ab * t;
        }

        // ================================================================
        //  Inset polygon (miter join, self-intersection guard)
        // ================================================================

        /// <summary>
        /// Returns a new polygon that is <paramref name="poly"/> inset inward by
        /// <paramref name="distance"/> metres on every edge using miter joins.
        /// A miter-length cap of 5× <paramref name="distance"/> guards against
        /// very sharp corners producing unbounded spikes.
        /// </summary>
        public static List<Vector2> InsetPolygon(IReadOnlyList<Vector2> poly, float distance)
        {
            int n = poly.Count;
            if (n < 3) return new List<Vector2>(0);

            // Determine winding: for CCW (positive area) the inward normal for edge
            // A→B is to the LEFT of the edge direction, i.e. (-dy, dx).
            // For CW we flip.
            float area = PolygonArea(poly);
            float normalSign = (area >= 0f) ? 1f : -1f;

            float maxMiter = distance * 5f;
            var result = new List<Vector2>(n);

            for (int i = 0; i < n; i++)
            {
                int prev = (i - 1 + n) % n;
                int next = (i + 1) % n;

                var p0 = poly[prev];
                var p1 = poly[i];
                var p2 = poly[next];

                // Compute normalised directions of the two edges meeting at p1
                var rawD1 = p1 - p0;
                float len1 = rawD1.magnitude;
                if (len1 < Epsilon) { result.Add(p1); continue; }
                var d1 = rawD1 / len1;

                var rawD2 = p2 - p1;
                float len2 = rawD2.magnitude;
                if (len2 < Epsilon) { result.Add(p1); continue; }
                var d2 = rawD2 / len2;

                // Inward normals (left of edge direction for CCW polygon)
                var n1 = new Vector2(-d1.y, d1.x) * normalSign;
                var n2 = new Vector2(-d2.y, d2.x) * normalSign;

                // Origin points for the two offset lines passing through p1
                var o1 = p1 + n1 * distance;   // prev-edge offset line, through p1
                var o2 = p1 + n2 * distance;   // next-edge offset line, through p1

                // Intersect the two offset lines to find the miter vertex
                Vector2 miter;
                if (!LineIntersect(o1, d1, o2, d2, out miter))
                {
                    // Parallel edges — average the two offset origins
                    miter = (o1 + o2) * 0.5f;
                }

                // Self-intersection guard: clamp miter distance from original vertex
                var delta = miter - p1;
                float miterLen = delta.magnitude;
                if (miterLen > maxMiter && miterLen > Epsilon)
                    miter = p1 + delta * (maxMiter / miterLen);

                result.Add(miter);
            }

            return result;
        }

        // ================================================================
        //  OBB longest axis (rotating calipers over convex hull)
        // ================================================================

        /// <summary>
        /// Computes the oriented bounding box of <paramref name="poly"/> via rotating calipers
        /// over its convex hull and returns:
        /// <list type="bullet">
        ///   <item><term>axis</term><description>direction of the OBB's longest dimension</description></item>
        ///   <item><term>splitDir</term><description>direction perpendicular to the longest axis
        ///   (the line direction along which to split the polygon)</description></item>
        /// </list>
        /// </summary>
        public static (Vector2 axis, Vector2 splitDir) ObbLongestAxis(IReadOnlyList<Vector2> poly)
        {
            var hull = ConvexHull(poly);
            int n = hull.Count;
            if (n < 2) return (Vector2.right, Vector2.up);

            float bestLength = -1f;
            Vector2 bestAxis = Vector2.right;
            Vector2 bestSplitDir = Vector2.up;

            for (int i = 0; i < n; i++)
            {
                var edge = hull[(i + 1) % n] - hull[i];
                float edgeLen = edge.magnitude;
                if (edgeLen < Epsilon) continue;
                var edgeDir = edge / edgeLen;
                var perpDir = new Vector2(-edgeDir.y, edgeDir.x);

                float minProj = float.MaxValue, maxProj = float.MinValue;

                for (int j = 0; j < n; j++)
                {
                    float proj = Dot(hull[j], edgeDir);
                    if (proj < minProj) minProj = proj;
                    if (proj > maxProj) maxProj = proj;
                }

                float length = maxProj - minProj;
                if (length > bestLength)
                {
                    bestLength = length;
                    bestAxis = edgeDir;
                    // Split perpendicular to the longest axis
                    bestSplitDir = perpDir;
                }
            }

            return (bestAxis, bestSplitDir);
        }

        /// <summary>
        /// Andrew's monotone-chain convex hull (O(n log n), deterministic).
        /// Returns vertices in CCW order. Input need not be sorted.
        /// </summary>
        public static List<Vector2> ConvexHull(IReadOnlyList<Vector2> points)
        {
            int n = points.Count;
            if (n < 3)
            {
                var copy = new List<Vector2>(n);
                for (int i = 0; i < n; i++) copy.Add(points[i]);
                return copy;
            }

            // Sort lexicographically (deterministic, insertion-order-independent)
            var sorted = new List<Vector2>(n);
            for (int i = 0; i < n; i++) sorted.Add(points[i]);
            sorted.Sort((a, b) =>
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));

            var hull = new List<Vector2>(n * 2);

            // Lower hull
            for (int i = 0; i < n; i++)
            {
                while (hull.Count >= 2 &&
                       Cross(hull[hull.Count - 1] - hull[hull.Count - 2],
                             sorted[i] - hull[hull.Count - 2]) <= 0f)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(sorted[i]);
            }

            // Upper hull
            int lowerCount = hull.Count + 1;
            for (int i = n - 2; i >= 0; i--)
            {
                while (hull.Count >= lowerCount &&
                       Cross(hull[hull.Count - 1] - hull[hull.Count - 2],
                             sorted[i] - hull[hull.Count - 2]) <= 0f)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(sorted[i]);
            }

            hull.RemoveAt(hull.Count - 1); // last point equals first
            return hull;
        }

        // ================================================================
        //  Split polygon by line (Sutherland–Hodgman half-plane clip)
        // ================================================================

        /// <summary>
        /// Splits <paramref name="poly"/> into two sub-polygons along the infinite line
        /// passing through <paramref name="point"/> in direction <paramref name="dir"/>.
        /// <list type="bullet">
        ///   <item><term>left</term><description>vertices where Cross(dir, v−point) ≥ 0</description></item>
        ///   <item><term>right</term><description>vertices where Cross(dir, v−point) &lt; 0</description></item>
        /// </list>
        /// Both output lists include the intersection points so they tile the original polygon.
        /// </summary>
        public static (List<Vector2> left, List<Vector2> right) SplitPolygonByLine(
            IReadOnlyList<Vector2> poly, Vector2 point, Vector2 dir)
        {
            int n = poly.Count;
            var left  = new List<Vector2>(n + 2);
            var right = new List<Vector2>(n + 2);

            for (int i = 0; i < n; i++)
            {
                var a = poly[i];
                var b = poly[(i + 1) % n];

                float sideA = Cross(dir, a - point);
                float sideB = Cross(dir, b - point);

                // Classify and add vertex a
                if (sideA >= 0f) left.Add(a);
                else             right.Add(a);

                // Check whether edge a→b crosses the split line
                bool aCrossing = (sideA > 0f && sideB < 0f) || (sideA < 0f && sideB > 0f);
                if (aCrossing)
                {
                    var da = b - a;
                    float denom = Cross(dir, da);
                    if (Mathf.Abs(denom) > Epsilon)
                    {
                        float t = Cross(dir, point - a) / denom;
                        var intersection = a + da * Mathf.Clamp01(t);
                        left.Add(intersection);
                        right.Add(intersection);
                    }
                }
            }

            return (left, right);
        }

        // ================================================================
        //  Polyline resampling
        // ================================================================

        /// <summary>
        /// Resamples <paramref name="pts"/> into a new polyline whose consecutive
        /// vertices are approximately <paramref name="spacing"/> apart (in 2D distance).
        /// The first and last original points are always included.
        /// </summary>
        public static List<Vector2> ResamplePolyline(IReadOnlyList<Vector2> pts, float spacing)
        {
            if (pts.Count < 2 || spacing <= 0f)
            {
                var copy = new List<Vector2>(pts.Count);
                for (int i = 0; i < pts.Count; i++) copy.Add(pts[i]);
                return copy;
            }

            var result = new List<Vector2>();
            result.Add(pts[0]);

            float accumulated = 0f; // distance walked since last sample point

            for (int i = 1; i < pts.Count; i++)
            {
                float segLen = Vector2.Distance(pts[i - 1], pts[i]);
                float consumed = 0f;

                while (consumed < segLen)
                {
                    float needed   = spacing - accumulated;
                    float remaining = segLen - consumed;

                    if (needed <= remaining)
                    {
                        consumed   += needed;
                        float t     = consumed / segLen;
                        result.Add(Vector2.Lerp(pts[i - 1], pts[i], t));
                        accumulated = 0f;
                    }
                    else
                    {
                        accumulated += remaining;
                        consumed     = segLen;
                    }
                }
            }

            // Always close with the last original point if not already placed
            var last = pts[pts.Count - 1];
            if (Vector2.Distance(result[result.Count - 1], last) > Epsilon)
                result.Add(last);

            return result;
        }

        // ================================================================
        //  Smooth Catmull-Rom / Cardinal spline
        // ================================================================

        /// <summary>
        /// Produces a smooth polyline through <paramref name="pts"/> using a cardinal
        /// Catmull-Rom spline.
        /// <paramref name="tension"/> in [0, 1]: 0 = loose (standard Catmull-Rom),
        /// 1 = tight (linear interpolation).
        /// <paramref name="samples"/> is the number of interpolated points per segment
        /// (inclusive of the segment end-point).
        /// </summary>
        public static List<Vector2> SmoothCatmullRom(
            IReadOnlyList<Vector2> pts, float tension, int samples)
        {
            int n = pts.Count;
            if (n < 2)
            {
                var copy = new List<Vector2>(n);
                for (int i = 0; i < n; i++) copy.Add(pts[i]);
                return copy;
            }
            if (samples < 1) samples = 1;

            var result = new List<Vector2>((n - 1) * samples + 1);
            result.Add(pts[0]);

            for (int i = 0; i < n - 1; i++)
            {
                var p0 = pts[Mathf.Max(0, i - 1)];
                var p1 = pts[i];
                var p2 = pts[i + 1];
                var p3 = pts[Mathf.Min(n - 1, i + 2)];

                for (int s = 1; s <= samples; s++)
                {
                    float t = s / (float)samples;
                    result.Add(CardinalSpline(p0, p1, p2, p3, t, tension));
                }
            }

            return result;
        }

        // Cardinal spline (Catmull-Rom family).
        // tension=0 → standard Catmull-Rom; tension=1 → linear.
        private static Vector2 CardinalSpline(
            Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t, float tension)
        {
            float s = (1f - tension) * 0.5f; // tangent scale
            float t2 = t * t;
            float t3 = t2 * t;

            // Hermite basis
            float h00 =  2f * t3 - 3f * t2 + 1f;
            float h10 =       t3 - 2f * t2 + t;
            float h01 = -2f * t3 + 3f * t2;
            float h11 =       t3 -      t2;

            var m1 = s * (p2 - p0);
            var m2 = s * (p3 - p1);

            return h00 * p1 + h10 * m1 + h01 * p2 + h11 * m2;
        }

        // ================================================================
        //  Chaikin corner-cutting smoothing
        // ================================================================

        /// <summary>
        /// Smooths a closed polygon by Chaikin corner-cutting.
        /// Each iteration replaces every edge with points at ¼ and ¾ along it.
        /// </summary>
        public static List<Vector2> ChaikinSmooth(IReadOnlyList<Vector2> poly, int iters)
        {
            int count = poly.Count;
            if (iters <= 0 || count < 3)
            {
                var copy = new List<Vector2>(count);
                for (int i = 0; i < count; i++) copy.Add(poly[i]);
                return copy;
            }

            // Work on a mutable List to avoid repeated casts
            var current = new List<Vector2>(count);
            for (int i = 0; i < count; i++) current.Add(poly[i]);

            for (int iter = 0; iter < iters; iter++)
            {
                int n = current.Count;
                var next = new List<Vector2>(n * 2);
                for (int i = 0; i < n; i++)
                {
                    var a = current[i];
                    var b = current[(i + 1) % n];
                    next.Add(a * 0.75f + b * 0.25f);
                    next.Add(a * 0.25f + b * 0.75f);
                }
                current = next;
            }

            return current;
        }

        // ================================================================
        //  Vector2 → Vector3 mapping (x → X, y → Z)
        // ================================================================

        /// <summary>
        /// Maps a 2D XZ-plane coordinate to world space: v.x → X, v.y → Z.
        /// <paramref name="y"/> sets the world-space Y (height), defaulting to 0.
        /// </summary>
        public static Vector3 ToVector3(Vector2 v, float y = 0f) =>
            new Vector3(v.x, y, v.y);
    }
}
