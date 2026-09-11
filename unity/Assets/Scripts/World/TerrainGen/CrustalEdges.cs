using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// The interior boundaries of the crustal partition, as world-space segments joined at their
    /// shared vertices.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A mountain belt forms where two crustal blocks meet, so the geometry a range spine is built
    /// from is the boundary <em>between</em> cells, not a path through their middles. This extracts
    /// those boundaries from the same partition raster the coastline was traced from, so a range's
    /// direction and its relationship to the coast are consequences of one structure rather than two
    /// independent ones.
    /// </para>
    /// <para>
    /// Extracted from the raster rather than from a Voronoi diagram, for the same reason the
    /// coastline was: a polygon partition would need a Delaunay triangulation with its cocircular and
    /// hull cases, and the raster is already here. The cost is that an endpoint sits within about a
    /// raster cell of the true Voronoi vertex, which is why edges are joined by proximity rather than
    /// by exact identity.
    /// </para>
    /// <para>
    /// Only edges between two land cells are kept, and only those whose endpoints are inside the
    /// published coastline. A belt is a feature of the landmass; an edge running out to sea is not a
    /// candidate for one.
    /// </para>
    /// </remarks>
    public sealed class CrustalEdges
    {
        /// <summary>One interior boundary between two land cells.</summary>
        public readonly struct Edge
        {
            /// <summary>Lower site index of the pair.</summary>
            public int SiteA { get; }

            /// <summary>Higher site index of the pair.</summary>
            public int SiteB { get; }

            public Vector2 A { get; }
            public Vector2 B { get; }
            public float Length { get; }

            public Edge(int siteA, int siteB, Vector2 a, Vector2 b)
            {
                SiteA = siteA;
                SiteB = siteB;
                A = a;
                B = b;
                Length = (b - a).magnitude;
            }

            public Vector2 Mid => (A + B) * 0.5f;

            /// <summary>Unit direction from <see cref="A"/> to <see cref="B"/>.</summary>
            public Vector2 Direction => Length > 1e-4f ? (B - A) / Length : Vector2.right;
        }

        /// <summary>Edges, ordered by site pair so the ordering never depends on traversal.</summary>
        public Edge[] Edges { get; }

        /// <summary>Vertex index of each edge's A and B endpoints, as <c>[edge * 2 + 0/1]</c>.</summary>
        public int[] EndpointVertex { get; }

        /// <summary>Edge indices meeting at each vertex, ascending.</summary>
        public int[][] VertexEdges { get; }

        public int VertexCount => VertexEdges.Length;

        private CrustalEdges(Edge[] edges, int[] endpointVertex, int[][] vertexEdges)
        {
            Edges = edges;
            EndpointVertex = endpointVertex;
            VertexEdges = vertexEdges;
        }

        /// <summary>Shortest boundary kept, metres. Below this a "boundary" is a corner touch.</summary>
        private const float MinEdgeLengthMeters = 40f;

        public static CrustalEdges Build(
            CrustalPartition partition, IReadOnlyList<int> landSites, CoastQuery coast)
        {
            var isLand = new bool[partition.SiteCount];
            for (var i = 0; i < landSites.Count; i++)
            {
                isLand[landSites[i]] = true;
            }

            var contacts = new Dictionary<long, List<Vector2>>(256);
            var cell = partition.CellMeters;

            void Record(int siteHere, int siteThere, Vector2 point)
            {
                if (siteHere == siteThere || !isLand[siteHere] || !isLand[siteThere])
                {
                    return;
                }

                var lo = Mathf.Min(siteHere, siteThere);
                var hi = Mathf.Max(siteHere, siteThere);
                var key = ((long)lo << 32) | (uint)hi;
                if (!contacts.TryGetValue(key, out var list))
                {
                    list = new List<Vector2>(16);
                    contacts[key] = list;
                }

                list.Add(point);
            }

            for (var z = 0; z < partition.RasterHeight; z++)
            {
                for (var x = 0; x < partition.RasterWidth; x++)
                {
                    var here = partition.Cells[z * partition.RasterWidth + x];

                    if (x + 1 < partition.RasterWidth)
                    {
                        Record(
                            here,
                            partition.Cells[z * partition.RasterWidth + x + 1],
                            new Vector2((x + 1) * cell, (z + 0.5f) * cell));
                    }

                    if (z + 1 < partition.RasterHeight)
                    {
                        Record(
                            here,
                            partition.Cells[(z + 1) * partition.RasterWidth + x],
                            new Vector2((x + 0.5f) * cell, (z + 1) * cell));
                    }
                }
            }

            // Sorted by site pair: the dictionary's own order is not a determinism guarantee, and
            // edge index decides both the seeded weight lookup and every tie-break after it.
            var keys = new List<long>(contacts.Keys);
            keys.Sort();

            var edges = new List<Edge>(keys.Count);
            for (var k = 0; k < keys.Count; k++)
            {
                var points = contacts[keys[k]];
                if (points.Count < 2)
                {
                    continue;
                }

                FarthestPair(points, out var a, out var b);
                if ((b - a).magnitude < MinEdgeLengthMeters)
                {
                    continue;
                }

                if (!coast.IsInside(a.x, a.y) || !coast.IsInside(b.x, b.y))
                {
                    continue;
                }

                var siteA = (int)(keys[k] >> 32);
                var siteB = (int)(keys[k] & 0xffffffff);
                edges.Add(new Edge(siteA, siteB, a, b));
            }

            BuildVertices(edges, cell * 2.5f, out var endpointVertex, out var vertexEdges);
            return new CrustalEdges(edges.ToArray(), endpointVertex, vertexEdges);
        }

        /// <summary>The two contact points farthest apart, which are the boundary's endpoints.</summary>
        private static void FarthestPair(List<Vector2> points, out Vector2 a, out Vector2 b)
        {
            a = points[0];
            b = points[0];
            var bestSq = -1f;
            for (var i = 0; i < points.Count; i++)
            {
                for (var j = i + 1; j < points.Count; j++)
                {
                    var sq = (points[i] - points[j]).sqrMagnitude;
                    if (sq > bestSq)
                    {
                        bestSq = sq;
                        a = points[i];
                        b = points[j];
                    }
                }
            }
        }

        /// <summary>
        /// Groups endpoints that lie within <paramref name="tolerance"/> into shared vertices.
        /// </summary>
        /// <remarks>
        /// Union-find over a grid neighbourhood rather than a rounded grid key: two endpoints of the
        /// same vertex can straddle a key boundary, and a chain that silently loses a junction ends
        /// early for no visible reason.
        /// </remarks>
        private static void BuildVertices(
            List<Edge> edges, float tolerance, out int[] endpointVertex, out int[][] vertexEdges)
        {
            var count = edges.Count * 2;
            var points = new Vector2[count];
            for (var i = 0; i < edges.Count; i++)
            {
                points[i * 2] = edges[i].A;
                points[i * 2 + 1] = edges[i].B;
            }

            var parent = new int[count];
            for (var i = 0; i < count; i++)
            {
                parent[i] = i;
            }

            int Find(int i)
            {
                while (parent[i] != i)
                {
                    parent[i] = parent[parent[i]];
                    i = parent[i];
                }

                return i;
            }

            void Union(int i, int j)
            {
                var ri = Find(i);
                var rj = Find(j);
                if (ri == rj)
                {
                    return;
                }

                // Lower root wins, so the grouping does not depend on visit order.
                if (ri < rj)
                {
                    parent[rj] = ri;
                }
                else
                {
                    parent[ri] = rj;
                }
            }

            var buckets = new Dictionary<long, List<int>>(count);
            for (var i = 0; i < count; i++)
            {
                var bx = Mathf.FloorToInt(points[i].x / tolerance);
                var bz = Mathf.FloorToInt(points[i].y / tolerance);
                var key = ((long)bx << 32) ^ (uint)bz;
                if (!buckets.TryGetValue(key, out var list))
                {
                    list = new List<int>(4);
                    buckets[key] = list;
                }

                list.Add(i);
            }

            var toleranceSq = tolerance * tolerance;
            for (var i = 0; i < count; i++)
            {
                var bx = Mathf.FloorToInt(points[i].x / tolerance);
                var bz = Mathf.FloorToInt(points[i].y / tolerance);
                for (var dz = -1; dz <= 1; dz++)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        var key = ((long)(bx + dx) << 32) ^ (uint)(bz + dz);
                        if (!buckets.TryGetValue(key, out var list))
                        {
                            continue;
                        }

                        for (var k = 0; k < list.Count; k++)
                        {
                            var j = list[k];
                            if (j <= i)
                            {
                                continue;
                            }

                            if ((points[i] - points[j]).sqrMagnitude <= toleranceSq)
                            {
                                Union(i, j);
                            }
                        }
                    }
                }
            }

            var rootToVertex = new Dictionary<int, int>(count);
            endpointVertex = new int[count];
            var members = new List<List<int>>();

            for (var i = 0; i < count; i++)
            {
                var root = Find(i);
                if (!rootToVertex.TryGetValue(root, out var vertex))
                {
                    vertex = members.Count;
                    rootToVertex[root] = vertex;
                    members.Add(new List<int>(4));
                }

                endpointVertex[i] = vertex;
                var edgeIndex = i / 2;
                if (!members[vertex].Contains(edgeIndex))
                {
                    members[vertex].Add(edgeIndex);
                }
            }

            vertexEdges = new int[members.Count][];
            for (var v = 0; v < members.Count; v++)
            {
                members[v].Sort();
                vertexEdges[v] = members[v].ToArray();
            }
        }
    }
}
