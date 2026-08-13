using System.Collections.Generic;
using GameDev.World.Roads;
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
            // Band (220m) exceeds RoadCost.MaxBridgeSpanMeters (200m) and spans every z - neither a
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
            // Regression for the real bug: two POIs directly across a lake from each other, where
            // the straight line between them is fully submerged but a route around the lake's edge
            // exists. An MST that only estimates edge weight along the straight line (the original,
            // buggy implementation) would have marked this pair as unreachable and produced zero
            // roads even though a real path exists - BuildNetwork now runs actual A* per pair, so it
            // must find the detour.
            var grid = new RoadGridSpec(0f, 0f, 35, 10, 10f);
            var nodes = new List<Vector2> { new Vector2(10f, 50f), new Vector2(340f, 50f) };

            // "Lake" blocks a rectangle 220m wide in x (wider than RoadCost.MaxBridgeSpanMeters, so
            // a direct bridge can't shortcut it) and 60m tall in z - both nodes sit exactly on the
            // blocked z row, so the direct line is blocked end to end and open only above/below.
            float Cost(float x, float z) => x > 50f && x < 270f && z > 20f && z < 80f ? float.PositiveInfinity : 1f;

            var segments = RoadNetworkBuilder.BuildNetwork(nodes, grid, Cost);

            Assert.AreEqual(1, segments.Count, "a detour around the lake exists and must be found");
            foreach (var p in segments[0].Points)
            {
                if (p.x > 50f && p.x < 270f)
                {
                    Assert.IsTrue(p.y <= 20f || p.y >= 80f, "the route must cross the lake band only where it's actually open, not bridge straight across");
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
                if (seg.Tier == RoadTier.Main)
                {
                    main++;
                    Assert.IsTrue(isHub[seg.NodeA] && isHub[seg.NodeB], "Main edges must connect hubs only");
                }
                else
                {
                    branch++;
                    Assert.IsTrue(
                        (isHub[seg.NodeA] && !isHub[seg.NodeB]) || (!isHub[seg.NodeA] && isHub[seg.NodeB]),
                        "Branch edges must attach a non-hub to a hub");
                }
            }

            Assert.AreEqual(2, main, "open TSP path over 3 hubs has 2 main edges");
            Assert.AreEqual(2, branch, "each non-hub should get one branch spur");

            // Main tract must be a single path visiting every hub (TSP open path), not a star.
            var hubDegree = new Dictionary<int, int>();
            foreach (var seg in segments)
            {
                if (seg.Tier != RoadTier.Main)
                {
                    continue;
                }

                hubDegree[seg.NodeA] = hubDegree.TryGetValue(seg.NodeA, out var da) ? da + 1 : 1;
                hubDegree[seg.NodeB] = hubDegree.TryGetValue(seg.NodeB, out var db) ? db + 1 : 1;
            }

            Assert.AreEqual(3, hubDegree.Count, "every hub must lie on the main tract");
            var endpoints = 0;
            foreach (var kv in hubDegree)
            {
                Assert.IsTrue(kv.Value == 1 || kv.Value == 2);
                if (kv.Value == 1)
                {
                    endpoints++;
                }
            }

            Assert.AreEqual(2, endpoints, "open TSP path has exactly two endpoints");
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
                Assert.AreEqual(RoadTier.Main, seg.Tier);
            }
        }
    }
}
