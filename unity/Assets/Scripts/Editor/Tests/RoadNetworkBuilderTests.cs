using System.Collections.Generic;
using GameDev.World.Landscape;
using GameDev.World.Roads;
using GameDev.World.Terrain;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Roads.Tests
{
    public sealed class RoadNetworkBuilderTests
    {
        private static RoadGridSpec MakeGrid() => new RoadGridSpec(0f, 0f, 10, 10, 10f);

        [Test]
        public void BuildNetwork_FewerThanTwoNodes_ReturnsEmpty()
        {
            var grid = MakeGrid();
            Assert.AreEqual(0, RoadNetworkBuilder.BuildNetwork(new List<Vector2>(), grid, (x, z) => 1f).Count);
            Assert.AreEqual(0, RoadNetworkBuilder.BuildNetwork(new List<Vector2> { new Vector2(5f, 5f) }, grid, (x, z) => 1f).Count);
        }

        [Test]
        public void BuildNetwork_FourCornersUniformCost_ConnectsWithSidesNotDiagonals()
        {
            var grid = MakeGrid();
            var nodes = new List<Vector2>
            {
                new Vector2(10f, 10f), // 0: bottom-left
                new Vector2(10f, 90f), // 1: top-left
                new Vector2(90f, 10f), // 2: bottom-right
                new Vector2(90f, 90f), // 3: top-right
            };

            var segments = RoadNetworkBuilder.BuildNetwork(nodes, grid, (x, z) => 1f);

            // A spanning tree over 4 nodes has exactly 3 edges.
            Assert.AreEqual(3, segments.Count);

            var connected = new HashSet<int>();
            foreach (var segment in segments)
            {
                connected.Add(segment.NodeA);
                connected.Add(segment.NodeB);

                var isDiagonal =
                    (segment.NodeA == 0 && segment.NodeB == 3) || (segment.NodeA == 3 && segment.NodeB == 0) ||
                    (segment.NodeA == 1 && segment.NodeB == 2) || (segment.NodeA == 2 && segment.NodeB == 1);
                Assert.IsFalse(isDiagonal, "with uniform cost, the shorter side edges must win over the longer diagonals");
            }

            Assert.AreEqual(4, connected.Count, "every node must be reachable in the resulting tree");
        }

        [Test]
        public void BuildNetwork_NoGapInWall_FindsNoRoute()
        {
            // Band (220m) exceeds RoadCost.MaxBridgeSpanMeters and spans every z - neither a
            // bridge nor a walk-around detour exists, so BuildNetwork must report that honestly
            // rather than fabricating a segment.
            var grid = new RoadGridSpec(0f, 0f, 35, 10, 10f);
            var nodes = new List<Vector2> { new Vector2(5f, 50f), new Vector2(345f, 50f) };

            float Cost(float x, float z) => x > 50f && x < 270f ? float.PositiveInfinity : 1f;

            var segments = RoadNetworkBuilder.BuildNetwork(nodes, grid, Cost);

            Assert.AreEqual(0, segments.Count);
        }

        [Test]
        public void BuildNetwork_NarrowWaterGap_BridgesAcross()
        {
            var grid = MakeGrid();
            var nodes = new List<Vector2> { new Vector2(10f, 50f), new Vector2(90f, 50f) };

            // 20m gap spanning every z - no walk-around detour exists on this grid, so the only
            // way BuildNetwork can connect these nodes is a bridge.
            float Cost(float x, float z) => x > 40f && x < 60f ? float.PositiveInfinity : 1f;

            var segments = RoadNetworkBuilder.BuildNetwork(nodes, grid, Cost);

            Assert.AreEqual(1, segments.Count, "a 20m gap is well within MaxBridgeSpanMeters and should connect");
        }

        [Test]
        public void BuildNetwork_StraightLineBlockedButDetourExists_StillConnects()
        {
            // Regression for the real bug: two POIs on opposite sides of an impassable band, where
            // the straight line between them is fully blocked but a route around the band's edge
            // exists. An MST that only estimates edge weight along the straight line (the original,
            // buggy implementation) would have marked this pair as unreachable and produced zero
            // roads even though a real path exists - BuildNetwork now runs actual A* per pair, so it
            // must find the detour. (A sea inlet is the V0 case; the test only needs infinite cost.)
            var grid = new RoadGridSpec(0f, 0f, 35, 10, 10f);
            var nodes = new List<Vector2> { new Vector2(10f, 50f), new Vector2(340f, 50f) };

            // The impassable band blocks a rectangle 220m wide in x (wider than RoadCost.MaxBridgeSpanMeters, so
            // a direct bridge can't shortcut it) and 60m tall in z - both nodes sit exactly on the
            // blocked z row, so the direct line is blocked end to end and open only above/below.
            float Cost(float x, float z) => x > 50f && x < 270f && z > 20f && z < 80f ? float.PositiveInfinity : 1f;

            var segments = RoadNetworkBuilder.BuildNetwork(nodes, grid, Cost);

            Assert.AreEqual(1, segments.Count, "a detour around the band exists and must be found");
            foreach (var p in segments[0].Points)
            {
                if (p.x > 50f && p.x < 270f)
                {
                    Assert.IsTrue(p.y <= 20f || p.y >= 80f, "the route must cross the band only where it is actually open, not bridge straight across");
                }
            }
        }

        [Test]
        public void BuildNetwork_ManyNodesInALine_AllConnectedDespiteKnnCandidateLimit()
        {
            // Regression for demo-scale POI counts (PoiDemoScenario: 20+ markers): each node's KNN
            // candidate list only covers its 6 nearest neighbours, not every other node - this
            // proves the resulting graph still spans everyone instead of silently stranding nodes
            // outside that local neighbourhood.
            var grid = new RoadGridSpec(0f, 0f, 100, 10, 10f);
            var nodes = new List<Vector2>();
            for (var i = 0; i < 20; i++)
            {
                nodes.Add(new Vector2(20f + i * 45f, 50f));
            }

            var segments = RoadNetworkBuilder.BuildNetwork(nodes, grid, (x, z) => 1f);

            Assert.AreEqual(19, segments.Count, "a spanning tree over 20 nodes has exactly 19 edges");

            var connected = new HashSet<int>();
            foreach (var segment in segments)
            {
                connected.Add(segment.NodeA);
                connected.Add(segment.NodeB);
            }

            Assert.AreEqual(20, connected.Count, "every node must end up reachable despite the KNN candidate limit");
        }

        [Test]
        public void BuildHierarchicalNetwork_TownsAreMain_OtherPoisAreBranches()
        {
            var grid = new RoadGridSpec(0f, 0f, 40, 40, 10f);
            // Three hubs (towns) + two camps.
            var nodes = new List<Vector2>
            {
                new Vector2(20f, 20f),  // 0 hub
                new Vector2(20f, 380f), // 1 hub
                new Vector2(380f, 20f), // 2 hub
                new Vector2(100f, 40f), // 3 branch near hub 0
                new Vector2(40f, 300f), // 4 branch near hub 1
            };
            var isHub = new List<bool> { true, true, true, false, false };

            var segments = RoadNetworkBuilder.BuildHierarchicalNetwork(nodes, isHub, grid, (x, z) => 1f);

            var main = 0;
            var branch = 0;
            foreach (var seg in segments)
            {
                if (seg.Tier == RoadTier.Primary)
                {
                    main++;
                    Assert.IsTrue(isHub[seg.NodeA] && isHub[seg.NodeB], "Main edges must connect hubs only");
                }
                else
                {
                    branch++;
                    Assert.AreEqual(RoadTier.Secondary, seg.Tier);
                    Assert.IsFalse(isHub[seg.NodeA], "Secondary NodeA is the non-town POI");
                }
            }

            Assert.AreEqual(2, main, "MST trade tree over 3 hubs has 2 main edges");
            Assert.AreEqual(2, branch, "each non-hub should get one branch spur onto the main road");

            // Main tract must span every hub (trade tree).
            var hubDegree = new Dictionary<int, int>();
            foreach (var seg in segments)
            {
                if (seg.Tier != RoadTier.Primary)
                {
                    continue;
                }

                hubDegree[seg.NodeA] = hubDegree.TryGetValue(seg.NodeA, out var da) ? da + 1 : 1;
                hubDegree[seg.NodeB] = hubDegree.TryGetValue(seg.NodeB, out var db) ? db + 1 : 1;
            }

            Assert.AreEqual(3, hubDegree.Count, "every hub must lie on the main tract");

            foreach (var seg in segments)
            {
                if (seg.Tier != RoadTier.Secondary)
                {
                    continue;
                }

                // Secondary NodeA is the POI; NodeB is a metadata anchor on the network (hub or prior POI).
                Assert.IsFalse(isHub[seg.NodeA], "secondary NodeA is the non-town POI");
                Assert.GreaterOrEqual(seg.Points.Count, 2);
            }
        }

        [Test]
        public void BuildHierarchicalNetwork_NoHubs_FallsBackToFlatMst()
        {
            var grid = MakeGrid();
            var nodes = new List<Vector2>
            {
                new Vector2(10f, 10f),
                new Vector2(10f, 90f),
                new Vector2(90f, 10f),
            };
            var isHub = new List<bool> { false, false, false };

            var segments = RoadNetworkBuilder.BuildHierarchicalNetwork(nodes, isHub, grid, (x, z) => 1f);
            Assert.AreEqual(2, segments.Count);
            foreach (var seg in segments)
            {
                Assert.AreEqual(RoadTier.Primary, seg.Tier);
            }
        }

        [Test]
        public void BuildHierarchicalNetwork_PaintsCorridor_SoSecondHubPrefersExistingRoad()
        {
            var grid = new RoadGridSpec(0f, 0f, 40, 40, 10f);
            float Height(float x, float z) => 10f;
            BiomeId Biome(float x, float z) => BiomeId.Meadow;
            var field = new LandscapeCostField(0f, 0f, 400f, 400f, 10f, Height, Biome);

            var nodes = new List<Vector2>
            {
                new Vector2(20f, 50f),
                new Vector2(380f, 50f),
                new Vector2(200f, 80f),
            };
            var isHub = new List<bool> { true, true, true };

            var seedPath = new List<Vector2>();
            Assert.IsTrue(RoadPathfinder.TryFindPath(
                grid, (x, z) => field.EvaluateAtTerrain(x, z), nodes[0], nodes[1], seedPath, out _));
            field.PaintExistingRoad(seedPath);

            var afterPaint = field.EvaluateAtTerrain(200f, 50f);
            var offRoad = field.EvaluateAtTerrain(200f, 200f);
            Assert.Less(afterPaint, offRoad * 0.5f, "painted corridor must be much cheaper");

            var segments = RoadNetworkBuilder.BuildHierarchicalNetwork(
                nodes, isHub, grid, field, Height);
            Assert.GreaterOrEqual(segments.Count, 2);

            var touchesMidHub = false;
            foreach (var seg in segments)
            {
                if (seg.NodeA == 2 || seg.NodeB == 2)
                {
                    touchesMidHub = true;
                    break;
                }
            }

            Assert.IsTrue(touchesMidHub, "the mid hub must end up connected to the network");

            // What this test is really about: the painted corridor gets reused instead of a second
            // road being laid beside it. It used to assert that instead through the length of the
            // shortest segment touching the mid hub, on the assumption that the hub ends up a leaf
            // hanging off the trunk. That assumption is not part of the contract - which hub Prim
            // reaches first is decided by path cost, and the mid hub becomes a trunk endpoint the
            // moment its route is priced correctly. Length also cannot tell the two apart: with the
            // discount switched off entirely the same measurement reads 188.9 m against a healthy
            // 183.4 m, so it would wave through the very regression it was written to catch.
            //
            // These two measure the contract directly. Corridor reuse is the discriminating one:
            // 95.2 % with the discount live, 44.0 % without it.
            var corridor = new List<Vector2> { nodes[0], nodes[1] };
            MeasureNetwork(segments, nodes, corridor, out var nearCorridor, out var alongsideAnother);

            Assert.Greater(nearCorridor, 0.80f,
                "most of the network should run on the painted corridor rather than beside it");
            Assert.Less(alongsideAnother, 0.05f,
                "no road should run parallel to another road away from its junctions");
        }

        /// <summary>
        /// Fractions of total network length that (a) hug <paramref name="corridor"/> and (b) run
        /// close to a <em>different</em> segment away from any node.
        /// </summary>
        private static void MeasureNetwork(
            IReadOnlyList<RoadSegment> segments,
            IReadOnlyList<Vector2> nodes,
            IReadOnlyList<Vector2> corridor,
            out float nearCorridorFraction,
            out float alongsideAnotherFraction)
        {
            const float CorridorTolerance = 15f;
            const float ParallelTolerance = 25f;
            const float JunctionRadius = 40f;
            const float SampleStep = 2f;

            var total = 0f;
            var nearCorridor = 0f;
            var alongside = 0f;

            for (var s = 0; s < segments.Count; s++)
            {
                var points = segments[s].Points;
                if (points == null || points.Count < 2)
                {
                    continue;
                }

                for (var i = 0; i < points.Count - 1; i++)
                {
                    var a = points[i];
                    var b = points[i + 1];
                    var length = Vector2.Distance(a, b);
                    if (length < 1e-4f)
                    {
                        continue;
                    }

                    var steps = Mathf.Max(1, Mathf.FloorToInt(length / SampleStep));
                    var share = length / steps;
                    for (var k = 0; k < steps; k++)
                    {
                        var p = Vector2.Lerp(a, b, (k + 0.5f) / steps);
                        total += share;
                        if (DistanceToPolyline(p, corridor) <= CorridorTolerance)
                        {
                            nearCorridor += share;
                        }

                        var atJunction = false;
                        for (var n = 0; n < nodes.Count; n++)
                        {
                            if (Vector2.Distance(p, nodes[n]) < JunctionRadius)
                            {
                                atJunction = true;
                                break;
                            }
                        }

                        if (atJunction)
                        {
                            continue;
                        }

                        for (var other = 0; other < segments.Count; other++)
                        {
                            if (other == s || segments[other].Points == null)
                            {
                                continue;
                            }

                            if (DistanceToPolyline(p, segments[other].Points) < ParallelTolerance)
                            {
                                alongside += share;
                                break;
                            }
                        }
                    }
                }
            }

            total = Mathf.Max(total, 1e-4f);
            nearCorridorFraction = nearCorridor / total;
            alongsideAnotherFraction = alongside / total;
        }

        private static float DistanceToPolyline(Vector2 p, IReadOnlyList<Vector2> polyline)
        {
            var best = float.MaxValue;
            for (var i = 0; i < polyline.Count - 1; i++)
            {
                var a = polyline[i];
                var ab = polyline[i + 1] - a;
                var lengthSquared = ab.sqrMagnitude;
                var closest = lengthSquared < 1e-8f
                    ? a
                    : a + (ab * Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSquared));
                best = Mathf.Min(best, Vector2.Distance(p, closest));
            }

            return best;
        }

        [Test]
        public void BuildHierarchicalNetwork_SecondaryAttachesToPrimaryPolyline()
        {
            var grid = new RoadGridSpec(0f, 0f, 40, 40, 10f);
            float Height(float x, float z) => 10f;
            BiomeId Biome(float x, float z) => BiomeId.Meadow;
            var field = new LandscapeCostField(0f, 0f, 400f, 400f, 10f, Height, Biome);

            var nodes = new List<Vector2>
            {
                new Vector2(20f, 50f),
                new Vector2(380f, 50f),
                new Vector2(200f, 200f),
            };
            var isHub = new List<bool> { true, true, false };

            var segments = RoadNetworkBuilder.BuildHierarchicalNetwork(
                nodes, isHub, grid, field, Height);

            Assert.GreaterOrEqual(segments.Count, 2);
            var secondary = 0;
            var primary = 0;
            foreach (var seg in segments)
            {
                if (seg.Tier == RoadTier.Secondary)
                {
                    secondary++;
                    Assert.IsTrue(seg.NodeA == 2 || seg.NodeB == 2);
                }
                else
                {
                    primary++;
                }
            }

            Assert.GreaterOrEqual(primary, 1);
            Assert.AreEqual(1, secondary, "one secondary spur for the non-town POI");
        }
    }
}
