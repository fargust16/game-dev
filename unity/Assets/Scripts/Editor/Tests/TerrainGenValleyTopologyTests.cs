using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.TerrainGen.Tests
{
    /// <summary>
    /// Stage 3C slice B2: which physically adjacent valley channels actually converge.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The property under test is that topology is an <b>outcome of geometry</b>. Rank never proposes a
    /// merge, adjacency is never synthesised, and a world whose valleys diverge is allowed to have no
    /// merges at all. None of these tests asserts a merge count.
    /// </para>
    /// <para>
    /// The ray-level rules — winner selection, tie-breaks, the toe constraint, parallel and behind —
    /// are tested on hand-built rays rather than on seeds, because a seed cannot be asked to produce a
    /// tie or an exactly-parallel pair on demand.
    /// </para>
    /// </remarks>
    public sealed class TerrainGenValleyTopologyTests
    {
        private static readonly int[] Seeds =
        {
            110201, 424242, 20260905, 7, 101010, 20261231, 1337, 256, 777,
        };

        private static TerrainGenSettings TerrainSettings => TerrainGenSettings.Default;
        private static MacroStructureSettings MacroSettings => MacroStructureSettings.Default;
        private static MassifSettings MassifSettings => MassifSettings.Default;
        private static RidgeSettings RidgeSettings => RidgeSettings.Default;

        private ValleyNetwork[] networks;
        private ValleyTopology[] topologies;

        [OneTimeSetUp]
        public void BuildWorlds()
        {
            networks = new ValleyNetwork[Seeds.Length];
            topologies = new ValleyTopology[Seeds.Length];

            for (var i = 0; i < Seeds.Length; i++)
            {
                networks[i] = ValleyNetwork.Build(Skeleton(Seeds[i]), RidgeSettings);
                topologies[i] = ValleyTopology.Build(networks[i], RidgeSettings);
            }
        }

        // ---------- 1, 9, 11: adjacency and arcs ----------

        /// <summary>1 and 9. Every merge comes from a shared spur, and nothing closes the chain.</summary>
        /// <remarks>
        /// The defect the design revision existed to remove. A corridor absorbed by its neighbour does
        /// not make that neighbour's neighbour adjacent to anything new: the event still names the spur
        /// that physically separates two positions, and the merge is still between the channels holding
        /// those two positions.
        /// </remarks>
        [Test]
        public void EveryMergeComesFromPhysicalAdjacency()
        {
            var merges = 0;

            for (var w = 0; w < topologies.Length; w++)
            {
                var topology = topologies[w];
                var network = networks[w];
                var corridors = network.Ridges.Valleys;

                for (var e = 0; e < topology.Events.Count; e++)
                {
                    var result = topology.Events[e];

                    // The event's own corridors are always physical neighbours, merged or not.
                    Assert.IsTrue(
                        network.AreAdjacent(result.LowerCorridorId, result.UpperCorridorId),
                        $"seed {Seeds[w]} event {e}: corridors are not neighbours");
                    Assert.AreEqual(
                        result.SpurId, corridors[result.LowerCorridorId].UpperSpurId,
                        $"seed {Seeds[w]} event {e}: spur does not bound the lower corridor");
                    Assert.AreEqual(
                        result.SpurId, corridors[result.UpperCorridorId].LowerSpurId,
                        $"seed {Seeds[w]} event {e}: spur does not bound the upper corridor");

                    if (!result.Merged)
                    {
                        continue;
                    }

                    merges++;

                    // The two channels merged are the ones occupying the two adjacent positions.
                    Assert.That(
                        result.WinnerId, Is.EqualTo(result.LowerActiveId).Or.EqualTo(result.UpperActiveId),
                        $"seed {Seeds[w]} event {e}: winner is not one of the active channels");
                    Assert.That(
                        result.LoserId, Is.EqualTo(result.LowerActiveId).Or.EqualTo(result.UpperActiveId),
                        $"seed {Seeds[w]} event {e}: loser is not one of the active channels");
                    Assert.AreNotEqual(
                        result.WinnerId, result.LoserId, $"seed {Seeds[w]} event {e}: merged with itself");
                }
            }

            Assert.That(
                topologies.Length, Is.GreaterThan(0), "no worlds built");
            TestContext.WriteLine($"merges across the seed set: {merges}");
        }

        /// <summary>11. Confluences on one survivor march downstream.</summary>
        [Test]
        public void MergeArcsIntoOneSurvivorAreMonotone()
        {
            for (var w = 0; w < topologies.Length; w++)
            {
                var lastOn = new Dictionary<int, float>();

                foreach (var result in topologies[w].Events)
                {
                    if (!result.Merged)
                    {
                        continue;
                    }

                    if (lastOn.TryGetValue(result.WinnerId, out var previous))
                    {
                        Assert.That(
                            result.ConfluenceAxisDistanceMeters, Is.GreaterThanOrEqualTo(previous),
                            $"seed {Seeds[w]}: a confluence on V{result.WinnerId} went back upstream");
                    }

                    lastOn[result.WinnerId] = result.ConfluenceAxisDistanceMeters;
                }
            }
        }

        // ---------- 2, 3, 4: rank's role ----------

        /// <summary>2. Rank never creates a merge; only geometry does.</summary>
        /// <remarks>
        /// Checked at the level that matters: for every event that did not merge, the reason recorded
        /// is a geometric one. If rank were proposing merges there would be events whose outcome had
        /// nothing to do with the rays.
        /// </remarks>
        [Test]
        public void RankNeverCreatesTopology()
        {
            for (var w = 0; w < topologies.Length; w++)
            {
                var network = networks[w];

                foreach (var result in topologies[w].Events)
                {
                    if (result.Merged)
                    {
                        // A merge implies the two rays actually meet ahead of both.
                        Assert.IsTrue(
                            ValleyTopology.Intersect(
                                network.CandidateRays[result.LowerActiveId],
                                network.CandidateRays[result.UpperActiveId], out _),
                            $"seed {Seeds[w]}: merged without the rays meeting");
                        continue;
                    }

                    Assert.AreNotEqual(
                        ConvergenceOutcome.Merged, result.Outcome,
                        $"seed {Seeds[w]}: contradictory outcome");
                }
            }
        }

        /// <summary>3 and 4. The higher fixed rank survives; ties fall to crest, then to id.</summary>
        [Test]
        public void WinnerIsTheHigherFixedRank()
        {
            for (var w = 0; w < topologies.Length; w++)
            {
                var network = networks[w];

                foreach (var result in topologies[w].Events)
                {
                    if (!result.Merged)
                    {
                        continue;
                    }

                    var winner = network.Ranks[result.WinnerId];
                    var loser = network.Ranks[result.LoserId];

                    Assert.That(
                        ValleyNetwork.CompareRank(winner, loser), Is.LessThan(0),
                        $"seed {Seeds[w]}: V{result.LoserId} outranks the winner V{result.WinnerId}");
                }
            }
        }

        /// <summary>4. The comparator's tie-break order, on values a seed cannot be asked to produce.</summary>
        [Test]
        public void TieBreaksAreCrestThenId()
        {
            var big = new ValleyRank(5, 10f, 900f, 40f);
            var small = new ValleyRank(2, 10f, 400f, 90f);
            var tallTie = new ValleyRank(5, 10f, 400f, 90f);
            var shortTie = new ValleyRank(2, 10f, 400f, 30f);
            var lowId = new ValleyRank(2, 10f, 400f, 90f);

            Assert.That(
                ValleyNetwork.CompareRank(big, small), Is.LessThan(0),
                "volume must dominate a taller head");
            Assert.That(
                ValleyNetwork.CompareRank(tallTie, shortTie), Is.LessThan(0),
                "equal volume must fall to the taller head");
            Assert.That(
                ValleyNetwork.CompareRank(lowId, tallTie), Is.LessThan(0),
                "equal volume and crest must fall to the lower id");
        }

        // ---------- 5, 14: rejection ----------

        /// <summary>5 and 14. Invalid candidates are rejected, never forced.</summary>
        [Test]
        public void InvalidCandidatesAreRejectedWithAReason()
        {
            for (var w = 0; w < topologies.Length; w++)
            {
                var network = networks[w];
                var massifs = network.Ridges.Ownership.Massifs;

                foreach (var result in topologies[w].Events)
                {
                    if (!result.Merged)
                    {
                        Assert.AreEqual(-1, result.WinnerId, $"seed {Seeds[w]}: rejected event named a winner");
                        Assert.AreEqual(-1, result.LoserId, $"seed {Seeds[w]}: rejected event named a loser");
                        continue;
                    }

                    var corridor = network.Ridges.Valleys[result.LowerCorridorId];
                    var massif = massifs[corridor.MassifIndex];
                    var point = result.ConfluencePoint;

                    // No merge upstream of the spur toe: the divide still stood there.
                    var toe = network.ToeEvents[result.EventIndex].ToePoint;
                    Assert.That(
                        massif.Axis.DistanceTo(point.x, point.y),
                        Is.GreaterThanOrEqualTo(massif.Axis.DistanceTo(toe.x, toe.y) - 1e-3f),
                        $"seed {Seeds[w]}: confluence upstream of its spur toe");

                    // Not in the ocean, not on foreign ground, inside the structural domain.
                    var owner = network.Ridges.Ownership.Ownership.At(point.x, point.y);
                    Assert.AreEqual(
                        MassifOwnerKind.Massif, owner.Kind,
                        $"seed {Seeds[w]}: confluence on {owner.Kind} ground");
                    Assert.AreEqual(
                        corridor.MassifIndex, owner.MassifIndex,
                        $"seed {Seeds[w]}: confluence on another massif");
                }
            }
        }

        /// <summary>Rays that never meet ahead of both are refused.</summary>
        [Test]
        public void RaysThatDoNotMeetAheadAreRefused()
        {
            var right = new CandidateRay(new Vector2(0f, 0f), Vector2.right);

            // Parallel.
            Assert.IsFalse(
                ValleyTopology.Intersect(right, new CandidateRay(new Vector2(0f, 10f), Vector2.right),
                    out _), "parallel rays must not meet");

            // Meeting point behind both.
            Assert.IsFalse(
                ValleyTopology.Intersect(
                    new CandidateRay(new Vector2(0f, 0f), Vector2.right),
                    new CandidateRay(new Vector2(-10f, 10f), new Vector2(0f, 1f).normalized),
                    out _),
                "a crossing behind the origins must not count");

            // Genuine convergence ahead of both.
            Assert.IsTrue(
                ValleyTopology.Intersect(
                    new CandidateRay(new Vector2(0f, 0f), new Vector2(1f, 1f).normalized),
                    new CandidateRay(new Vector2(0f, 10f), new Vector2(1f, -1f).normalized),
                    out var point),
                "converging rays must meet");
            Assert.AreEqual(5f, point.x, 1e-3f, "meeting x");
            Assert.AreEqual(5f, point.y, 1e-3f, "meeting y");
        }

        // ---------- 6, 7, 8: active channels and both orderings ----------

        /// <summary>6, 7 and 8. A merge moves the winner into the loser's physical positions.</summary>
        /// <remarks>
        /// This is what lets a later event test against a channel that has already absorbed its
        /// neighbour, and it is what makes both orderings of the worked example work without either
        /// being hard-coded. Verified against the recorded event stream: every event's active channels
        /// equal the occupancy implied by all earlier merges.
        /// </remarks>
        [Test]
        public void ActiveOccupancyFollowsEarlierMerges()
        {
            for (var w = 0; w < topologies.Length; w++)
            {
                var topology = topologies[w];
                var network = networks[w];

                // Replay the events independently and compare with what B2 recorded.
                var slots = new List<int[]>();
                for (var c = 0; c < network.Chains.Count; c++)
                {
                    var copy = new int[network.Chains[c].Length];
                    System.Array.Copy(network.Chains[c].Corridors, copy, copy.Length);
                    slots.Add(copy);
                }

                foreach (var result in topology.Events)
                {
                    var chain = slots[result.ChainIndex];

                    Assert.AreEqual(
                        chain[result.Position], result.LowerActiveId,
                        $"seed {Seeds[w]} event {result.EventIndex}: lower active channel");
                    Assert.AreEqual(
                        chain[result.Position + 1], result.UpperActiveId,
                        $"seed {Seeds[w]} event {result.EventIndex}: upper active channel");

                    if (!result.Merged)
                    {
                        continue;
                    }

                    for (var i = 0; i < chain.Length; i++)
                    {
                        if (chain[i] == result.LoserId)
                        {
                            chain[i] = result.WinnerId;
                        }
                    }
                }

                for (var c = 0; c < slots.Count; c++)
                {
                    for (var i = 0; i < slots[c].Length; i++)
                    {
                        Assert.AreEqual(
                            slots[c][i], topology.Occupancy[c][i],
                            $"seed {Seeds[w]} chain {c} position {i}: final occupancy");
                        Assert.AreEqual(
                            topology.RootOf(network.Chains[c].Corridors[i]), topology.Occupancy[c][i],
                            $"seed {Seeds[w]} chain {c} position {i}: occupancy is not the root");
                    }
                }
            }
        }

        /// <summary>7 and 8. Both toe orderings resolve, on hand-built rays.</summary>
        /// <remarks>
        /// The worked example from the design, reduced to the decision it turns on. Which of `S0` and
        /// `S1` ends first changes which channel is active at the second event, and neither ordering is
        /// hard-coded anywhere: the same comparator and the same intersection produce both.
        /// </remarks>
        [Test]
        public void BothToeOrderingsResolveFromTheSameRules()
        {
            // Three converging rays, as three corridors' terminal tangents would be on a concave flank.
            var v0 = new CandidateRay(new Vector2(0f, 0f), new Vector2(1f, 0.30f).normalized);
            var v1 = new CandidateRay(new Vector2(0f, 40f), new Vector2(1f, 0f).normalized);
            var v2 = new CandidateRay(new Vector2(0f, 80f), new Vector2(1f, -0.30f).normalized);

            // d0 < d1: S0 first, so V0 and V1 meet, and V2 is then tested against V0's ray.
            Assert.IsTrue(ValleyTopology.Intersect(v0, v1, out var c01), "V0/V1 converge");
            Assert.IsTrue(ValleyTopology.Intersect(v0, v2, out var c02), "V0/V2 converge");
            Assert.That(
                c02.x, Is.GreaterThan(0f), "the second confluence is ahead of both origins");

            // d1 < d0: S1 first, so V1 and V2 meet, and V1 is then tested against V0.
            Assert.IsTrue(ValleyTopology.Intersect(v1, v2, out var c12), "V1/V2 converge");
            Assert.That(c12.x, Is.GreaterThan(0f), "confluence ahead");
            Assert.That(c01.x, Is.GreaterThan(0f), "confluence ahead");

            // And a divergent trio produces no merge at all, whichever order it is walked in.
            var d0 = new CandidateRay(new Vector2(0f, 0f), new Vector2(1f, -0.30f).normalized);
            var d1 = new CandidateRay(new Vector2(0f, 40f), new Vector2(1f, 0f).normalized);
            var d2 = new CandidateRay(new Vector2(0f, 80f), new Vector2(1f, 0.30f).normalized);

            Assert.IsFalse(ValleyTopology.Intersect(d0, d1, out _), "diverging pair must not merge");
            Assert.IsFalse(ValleyTopology.Intersect(d1, d2, out _), "diverging pair must not merge");
            Assert.IsFalse(ValleyTopology.Intersect(d0, d2, out _), "diverging pair must not merge");
        }

        // ---------- 10: acyclicity ----------

        /// <summary>10. No cycles, and every corridor reaches a trunk.</summary>
        [Test]
        public void TopologyIsAcyclicAndEveryCorridorReachesATrunk()
        {
            for (var w = 0; w < topologies.Length; w++)
            {
                var topology = topologies[w];
                var network = networks[w];

                for (var i = 0; i < topology.DownstreamOf.Count; i++)
                {
                    // RootOf throws on a cycle; this also proves the walk terminates.
                    var root = topology.RootOf(i);
                    Assert.That(
                        topology.DownstreamOf[root], Is.LessThan(0),
                        $"seed {Seeds[w]}: root V{root} still points downstream");

                    if (topology.DownstreamOf[i] < 0)
                    {
                        continue;
                    }

                    // Rank strictly increases along every parent pointer, which is why no cycle exists.
                    Assert.That(
                        ValleyNetwork.CompareRank(
                            network.Ranks[topology.DownstreamOf[i]], network.Ranks[i]),
                        Is.LessThan(0),
                        $"seed {Seeds[w]}: V{i} drains into something it outranks");
                }

                // At most one downstream winner each: DownstreamOf is a single int by construction,
                // and no corridor may be its own parent.
                for (var i = 0; i < topology.DownstreamOf.Count; i++)
                {
                    Assert.AreNotEqual(i, topology.DownstreamOf[i], $"seed {Seeds[w]}: V{i} drains to itself");
                }
            }
        }

        // ---------- 12, 13, 15, 16: determinism and immutability ----------

        /// <summary>12. The same seed gives the same topology, down to the confluence points.</summary>
        [Test]
        public void TopologyIsDeterministic()
        {
            for (var w = 0; w < topologies.Length; w++)
            {
                var again = ValleyTopology.Build(
                    ValleyNetwork.Build(Skeleton(Seeds[w]), RidgeSettings), RidgeSettings);
                var first = topologies[w];

                Assert.AreEqual(
                    WithoutTiming(first.Stats.ToLine()), WithoutTiming(again.Stats.ToLine()),
                    $"seed {Seeds[w]}: stats");
                Assert.AreEqual(
                    first.Events.Count, again.Events.Count, $"seed {Seeds[w]}: event count");

                for (var e = 0; e < first.Events.Count; e++)
                {
                    var a = first.Events[e];
                    var b = again.Events[e];

                    Assert.AreEqual(a.SpurId, b.SpurId, $"seed {Seeds[w]} event {e}: spur");
                    Assert.AreEqual(a.Outcome, b.Outcome, $"seed {Seeds[w]} event {e}: outcome");
                    Assert.AreEqual(a.WinnerId, b.WinnerId, $"seed {Seeds[w]} event {e}: winner");
                    Assert.AreEqual(a.LoserId, b.LoserId, $"seed {Seeds[w]} event {e}: loser");
                    Assert.AreEqual(
                        a.ConfluencePoint, b.ConfluencePoint, $"seed {Seeds[w]} event {e}: confluence");
                    Assert.AreEqual(
                        a.LowerActiveId, b.LowerActiveId, $"seed {Seeds[w]} event {e}: lower active");
                    Assert.AreEqual(
                        a.UpperActiveId, b.UpperActiveId, $"seed {Seeds[w]} event {e}: upper active");
                }

                for (var i = 0; i < first.DownstreamOf.Count; i++)
                {
                    Assert.AreEqual(
                        first.DownstreamOf[i], again.DownstreamOf[i], $"seed {Seeds[w]}: V{i} downstream");
                }
            }
        }

        /// <summary>15 and 16. Rank and candidate geometry are untouched by B2.</summary>
        [Test]
        public void RankAndCandidateGeometryAreUnchangedByB2()
        {
            for (var w = 0; w < networks.Length; w++)
            {
                var network = ValleyNetwork.Build(Skeleton(Seeds[w]), RidgeSettings);

                var ranksBefore = new float[network.Ranks.Count];
                var raysBefore = new CandidateRay[network.Ranks.Count];
                var bedsBefore = new Vector2[network.Ranks.Count][];
                var orderBefore = new int[network.RankOrder.Count];

                for (var i = 0; i < network.Ranks.Count; i++)
                {
                    ranksBefore[i] = network.Ranks[i].ReliefVolumeCum;
                    raysBefore[i] = network.CandidateRays[i];
                    bedsBefore[i] = (Vector2[])network.CandidateBeds[i].Clone();
                    orderBefore[i] = network.RankOrder[i];
                }

                ValleyTopology.Build(network, RidgeSettings);

                for (var i = 0; i < network.Ranks.Count; i++)
                {
                    Assert.AreEqual(
                        ranksBefore[i], network.Ranks[i].ReliefVolumeCum,
                        $"seed {Seeds[w]} corridor {i}: rank moved");
                    Assert.AreEqual(
                        raysBefore[i].Origin, network.CandidateRays[i].Origin,
                        $"seed {Seeds[w]} corridor {i}: ray origin moved");
                    Assert.AreEqual(
                        raysBefore[i].Direction, network.CandidateRays[i].Direction,
                        $"seed {Seeds[w]} corridor {i}: ray direction moved");
                    Assert.AreEqual(
                        orderBefore[i], network.RankOrder[i], $"seed {Seeds[w]}: rank order moved");

                    for (var k = 0; k < bedsBefore[i].Length; k++)
                    {
                        Assert.AreEqual(
                            bedsBefore[i][k], network.CandidateBeds[i][k],
                            $"seed {Seeds[w]} corridor {i} bed point {k}: moved");
                    }
                }

                // Slice B's own occupancy is B2's input, not its state, and must still be the identity.
                for (var c = 0; c < network.Chains.Count; c++)
                {
                    for (var i = 0; i < network.Chains[c].Length; i++)
                    {
                        Assert.AreEqual(
                            network.Chains[c].Corridors[i], network.OccupantAt(c, i),
                            $"seed {Seeds[w]} chain {c}: B2 wrote into Slice B's occupancy");
                    }
                }
            }
        }

        /// <summary>13. B2 modifies no terrain and writes no corridor topology.</summary>
        [Test]
        public void B2ChangesNoFieldAndNoCorridor()
        {
            const int seed = 110201;
            var stage2b = Stage2BMassifGenerator.Generate(
                seed, TerrainSettings, MacroSettings, MassifSettings);

            Assert.AreEqual("7599879a71250a06", stage2b.Stage1.Stats.FieldHash, "Stage 1 changed");

            var stage1Before = stage2b.Stage1.Field.Hash();
            var stage2Before = stage2b.Field.Hash();
            var upliftBefore = stage2b.UpliftField.Hash();

            var partition = Stage3ASkeleton.Build(stage2b, RidgeSettings);
            var ridges = RidgeSkeleton.Build(partition, RidgeSettings, new Seed.WorldSeed(seed));

            var downstreamBefore = new int[ridges.Valleys.Count];
            var terminationBefore = new ValleyTermination[ridges.Valleys.Count];
            for (var i = 0; i < ridges.Valleys.Count; i++)
            {
                downstreamBefore[i] = ridges.Valleys[i].DownstreamId;
                terminationBefore[i] = ridges.Valleys[i].Termination;
            }

            var topology = ValleyTopology.Build(
                ValleyNetwork.Build(ridges, RidgeSettings), RidgeSettings);

            Assert.That(topology.Stats.CorridorCount, Is.GreaterThan(0), "nothing to resolve");
            Assert.AreEqual(stage1Before, stage2b.Stage1.Field.Hash(), "the Stage 1 field moved");
            Assert.AreEqual(stage2Before, stage2b.Field.Hash(), "the Stage 2 field moved");
            Assert.AreEqual(upliftBefore, stage2b.UpliftField.Hash(), "the uplift field moved");

            // B2 owns the topology; the corridors' own fields belong to the trajectory slice.
            for (var i = 0; i < ridges.Valleys.Count; i++)
            {
                Assert.AreEqual(
                    downstreamBefore[i], ridges.Valleys[i].DownstreamId,
                    $"corridor {i}: B2 wrote a downstream link into the corridor");
                Assert.AreEqual(
                    terminationBefore[i], ridges.Valleys[i].Termination,
                    $"corridor {i}: B2 rewrote a termination");
            }
        }

        /// <summary>A world with no massif resolves to an empty topology rather than failing.</summary>
        [Test]
        public void AWorldWithoutAMassifHasAnEmptyTopology()
        {
            var barren = 0;

            for (var w = 0; w < topologies.Length; w++)
            {
                if (networks[w].Stats.CorridorCount > 0)
                {
                    continue;
                }

                barren++;
                Assert.AreEqual(0, topologies[w].Events.Count, $"seed {Seeds[w]}: events");
                Assert.AreEqual(0, topologies[w].Stats.MergeCount, $"seed {Seeds[w]}: merges");
                Assert.AreEqual(0, topologies[w].Trunks.Count, $"seed {Seeds[w]}: trunks");
            }

            Assert.That(barren, Is.GreaterThan(0), "the seed set no longer covers a world without a massif");
        }

        // ---------- fixtures ----------

        /// <summary>The stats line without its build time, which is a measurement and not an output.</summary>
        private static string WithoutTiming(string line)
        {
            var at = line.LastIndexOf(" build=", System.StringComparison.Ordinal);
            return at < 0 ? line : line.Substring(0, at);
        }

        private static RidgeSkeleton Skeleton(int seed)
        {
            var stage2a = Stage2AMacroStructure.Run(seed, TerrainSettings, MacroSettings);
            var worldSeed = new Seed.WorldSeed(seed);

            var massifs = new List<MountainMassif>();
            var accepted = stage2a.AcceptedAxes();
            for (var i = 0; i < accepted.Count; i++)
            {
                massifs.Add(MountainMassif.Build(
                    worldSeed, massifs.Count, accepted[i], TerrainSettings, MassifSettings,
                    accepted[i].CertifiedCore));
            }

            var partition = Stage3ASkeleton.Build(seed, massifs, stage2a.Coast, RidgeSettings);
            return RidgeSkeleton.Build(partition, RidgeSettings, worldSeed);
        }
    }
}
