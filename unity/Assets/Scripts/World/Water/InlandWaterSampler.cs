using System.Collections.Generic;
using GameDev.World.Hydrology;
using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// Answers the project's one water question for rivers and lakes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The whole road, settlement and POI integration is this class plus the existing
    /// <see cref="CompositeWaterSampler"/>. Nothing else needs to learn that inland water exists:
    /// <c>RoadCostSampler</c>, <c>SettlementSiteValidator</c> and the bridge-span checks were all
    /// written against <see cref="IWaterSampler"/> already, and they keep asking exactly the
    /// question they asked before.
    /// </para>
    /// <para>
    /// Dependency direction is hydrology to roads, never the reverse: this reads a finished
    /// <see cref="HydrologyDefinition"/> and no caller can write water back through it.
    /// </para>
    /// <para>
    /// Queries are answered off a uniform bucket grid built once at construction. Road costing asks
    /// this millions of times per plan — the same access pattern that once made a naive terrain
    /// height lookup cost 69 million scene searches — so a linear scan over every segment would make
    /// the roads step unusable.
    /// </para>
    /// </remarks>
    public sealed class InlandWaterSampler : IWaterSampler
    {
        /// <summary>Bucket size in metres. Comfortably wider than the widest river.</summary>
        private const float BucketSizeMeters = 64f;

        /// <summary>
        /// Extra half-width added to the river's own, so a bank counts as water's edge.
        /// </summary>
        /// <remarks>
        /// Road costing treats water as impassable, and a road laid exactly along a river bank is
        /// as wrong as one laid in the channel. Half a metre of margin keeps the query honest
        /// without widening the river the renderer draws.
        /// </remarks>
        private const float BankMarginMeters = 0.5f;

        private readonly HydrologyDefinition definition;
        private readonly Dictionary<long, List<Segment>> buckets = new Dictionary<long, List<Segment>>();
        private readonly List<LakeBounds> lakes = new List<LakeBounds>();

        public InlandWaterSampler(HydrologyDefinition definition)
        {
            this.definition = definition ?? HydrologyDefinition.Empty;
            Index();
        }

        public bool IsEmpty => definition.IsEmpty;

        public bool TryGetWaterLevel(float worldX, float worldZ, out float waterY)
        {
            // Lakes first: a lake surface is flat and unambiguous, and an outlet reach starts inside
            // one, so asking the river question there would return the channel's level instead.
            for (var i = 0; i < lakes.Count; i++)
            {
                if (lakes[i].Contains(worldX, worldZ))
                {
                    waterY = lakes[i].SurfaceElevation;
                    return true;
                }
            }

            var point = new Vector2(worldX, worldZ);
            var bx = Mathf.FloorToInt(worldX / BucketSizeMeters);
            var bz = Mathf.FloorToInt(worldZ / BucketSizeMeters);

            var found = false;
            var best = float.MaxValue;
            waterY = 0f;

            // The neighbouring buckets too: a segment is filed by the buckets its own span touches,
            // and a query just outside one can still be inside the river's half width.
            for (var dz = -1; dz <= 1; dz++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    if (!buckets.TryGetValue(Key(bx + dx, bz + dz), out var list))
                    {
                        continue;
                    }

                    for (var s = 0; s < list.Count; s++)
                    {
                        var segment = list[s];
                        var t = segment.Project(point);
                        var closest = Vector2.Lerp(segment.A, segment.B, t);
                        var distance = Vector2.Distance(point, closest);
                        if (distance > segment.HalfWidth || distance >= best)
                        {
                            continue;
                        }

                        best = distance;
                        waterY = Mathf.Lerp(segment.WaterA, segment.WaterB, t);
                        found = true;
                    }
                }
            }

            return found;
        }

        /// <summary>Half-width of the water surface at the nearest reach, or 0 away from rivers.</summary>
        /// <remarks>
        /// Exposed for the crossing question a bridge will eventually ask: how wide the span has to
        /// be. Nothing consumes it yet, and it deliberately answers about geometry rather than about
        /// whether a crossing is allowed — that is a gameplay rule this system does not invent.
        /// </remarks>
        public float RiverHalfWidthAt(float worldX, float worldZ)
        {
            var point = new Vector2(worldX, worldZ);
            var bx = Mathf.FloorToInt(worldX / BucketSizeMeters);
            var bz = Mathf.FloorToInt(worldZ / BucketSizeMeters);
            var best = 0f;

            for (var dz = -1; dz <= 1; dz++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    if (!buckets.TryGetValue(Key(bx + dx, bz + dz), out var list))
                    {
                        continue;
                    }

                    for (var s = 0; s < list.Count; s++)
                    {
                        var segment = list[s];
                        var t = segment.Project(point);
                        var distance = Vector2.Distance(
                            point, Vector2.Lerp(segment.A, segment.B, t));
                        if (distance <= segment.HalfWidth && segment.HalfWidth > best)
                        {
                            best = segment.HalfWidth;
                        }
                    }
                }
            }

            return best;
        }

        private void Index()
        {
            for (var l = 0; l < definition.Lakes.Count; l++)
            {
                lakes.Add(new LakeBounds(definition.Lakes[l]));
            }

            for (var r = 0; r < definition.Reaches.Count; r++)
            {
                var reach = definition.Reaches[r];
                var halfWidth = reach.WidthMeters * 0.5f + BankMarginMeters;
                var nodes = reach.Nodes;

                for (var i = 1; i < nodes.Count; i++)
                {
                    var segment = new Segment(
                        nodes[i - 1].PositionXZ,
                        nodes[i].PositionXZ,
                        nodes[i - 1].WaterElevation,
                        nodes[i].WaterElevation,
                        halfWidth);

                    var minX = Mathf.Min(segment.A.x, segment.B.x) - halfWidth;
                    var maxX = Mathf.Max(segment.A.x, segment.B.x) + halfWidth;
                    var minZ = Mathf.Min(segment.A.y, segment.B.y) - halfWidth;
                    var maxZ = Mathf.Max(segment.A.y, segment.B.y) + halfWidth;

                    var bx0 = Mathf.FloorToInt(minX / BucketSizeMeters);
                    var bx1 = Mathf.FloorToInt(maxX / BucketSizeMeters);
                    var bz0 = Mathf.FloorToInt(minZ / BucketSizeMeters);
                    var bz1 = Mathf.FloorToInt(maxZ / BucketSizeMeters);

                    for (var bz = bz0; bz <= bz1; bz++)
                    {
                        for (var bx = bx0; bx <= bx1; bx++)
                        {
                            var key = Key(bx, bz);
                            if (!buckets.TryGetValue(key, out var list))
                            {
                                list = new List<Segment>(4);
                                buckets.Add(key, list);
                            }

                            list.Add(segment);
                        }
                    }
                }
            }
        }

        private static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;

        private readonly struct Segment
        {
            public Vector2 A { get; }
            public Vector2 B { get; }
            public float WaterA { get; }
            public float WaterB { get; }
            public float HalfWidth { get; }

            public Segment(Vector2 a, Vector2 b, float waterA, float waterB, float halfWidth)
            {
                A = a;
                B = b;
                WaterA = waterA;
                WaterB = waterB;
                HalfWidth = halfWidth;
            }

            public float Project(Vector2 point)
            {
                var ab = B - A;
                var lengthSq = ab.sqrMagnitude;
                return lengthSq < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(point - A, ab) / lengthSq);
            }
        }

        private readonly struct LakeBounds
        {
            private readonly IReadOnlyList<Vector2> boundary;
            private readonly float minX;
            private readonly float maxX;
            private readonly float minZ;
            private readonly float maxZ;

            public float SurfaceElevation { get; }

            public LakeBounds(LakeBasin lake)
            {
                boundary = lake.Boundary;
                SurfaceElevation = lake.SurfaceElevation;

                minX = float.MaxValue;
                maxX = float.MinValue;
                minZ = float.MaxValue;
                maxZ = float.MinValue;
                for (var i = 0; i < boundary.Count; i++)
                {
                    minX = Mathf.Min(minX, boundary[i].x);
                    maxX = Mathf.Max(maxX, boundary[i].x);
                    minZ = Mathf.Min(minZ, boundary[i].y);
                    maxZ = Mathf.Max(maxZ, boundary[i].y);
                }
            }

            public bool Contains(float x, float z)
            {
                if (x < minX || x > maxX || z < minZ || z > maxZ)
                {
                    return false;
                }

                var inside = false;
                for (int i = 0, j = boundary.Count - 1; i < boundary.Count; j = i++)
                {
                    var a = boundary[i];
                    var b = boundary[j];
                    if (a.y > z != b.y > z && x < (b.x - a.x) * (z - a.y) / (b.y - a.y) + a.x)
                    {
                        inside = !inside;
                    }
                }

                return inside;
            }
        }
    }
}
