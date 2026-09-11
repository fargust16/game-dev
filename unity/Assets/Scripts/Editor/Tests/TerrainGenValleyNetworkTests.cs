using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.TerrainGen.Tests
{
    /// <summary>
    /// Stage 3C slices A and B: corridor rank, physical flank chains, and candidate geometry.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These assert the property the whole network construction rests on: <b>none of this depends on
    /// topology</b>. Rank, adjacency, candidate beds, candidate rays and the divide-end ordering are
    /// all functions of Stage 3B output, so the convergence test a later slice runs on them cannot be
    /// circular.
    /// </para>
    /// <para>
    /// They also assert what this slice must <em>not</em> do: no merge, no downstream link, no
    /// reclassified termination, no trajectory, no height.
    /// </para>
    /// </remarks>
    public sealed class TerrainGenValleyNetworkTests
    {
        private static readonly int[] Seeds =
        {
            110201, 424242, 20260905, 7, 101010, 20261231, 1337,
        };

        private static TerrainGenSettings TerrainSettings => TerrainGenSettings.Default;
        private static MacroStructureSettings MacroSettings => MacroStructureSettings.Default;
        private static MassifSettings MassifSettings => MassifSettings.Default;
        private static RidgeSettings RidgeSettings => RidgeSettings.Default;

        private RidgeSkeleton[] skeletons;
        private ValleyNetwork[] networks;

        [OneTimeSetUp]
        public void BuildWorlds()
        {
            skeletons = new RidgeSkeleton[Seeds.Length];
            networks = new ValleyNetwork[Seeds.Length];

            for (var i = 0; i < Seeds.Length; i++)
            {
                skeletons[i] = Skeleton(Seeds[i]);
                networks[i] = ValleyNetwork.Build(skeletons[i], RidgeSettings);
            }
        }

        // ---------- A1, A2, A3: rank ----------

        /// <summary>A1. The same seed produces the same ranks and the same ordering.</summary>
        [Test]
        public void RankIsDeterministic()
        {
            for (var w = 0; w < networks.Length; w++)
            {
                var again = ValleyNetwork.Build(Skeleton(Seeds[w]), RidgeSettings);
                var first = networks[w];

                Assert.AreEqual(
                    first.Ranks.Count, again.Ranks.Count, $"seed {Seeds[w]}: corridor count");

                for (var i = 0; i < first.Ranks.Count; i++)
                {
                    Assert.AreEqual(
                        first.Ranks[i].ReliefVolumeCum, again.Ranks[i].ReliefVolumeCum,
                        $"seed {Seeds[w]} corridor {i}: relief volume");
                    Assert.AreEqual(
                        first.Ranks[i].FlankFootprintSqm, again.Ranks[i].FlankFootprintSqm,
                        $"seed {Seeds[w]} corridor {i}: footprint");
                    Assert.AreEqual(
                        first.Ranks[i].HeadCrestMeters, again.Ranks[i].HeadCrestMeters,
                        $"seed {Seeds[w]} corridor {i}: head crest");
                }

                for (var i = 0; i < first.RankOrder.Count; i++)
                {
                    Assert.AreEqual(
                        first.RankOrder[i], again.RankOrder[i], $"seed {Seeds[w]}: rank order {i}");
                }
            }
        }

        /// <summary>
        /// A2 and A9. Rank and candidate geometry are identical whether or not topology exists.
        /// </summary>
        /// <remarks>
        /// The proof that the construction is acyclic, and it is measured rather than argued. One
        /// skeleton is built without a Stage 1 result, so its corridors carry no trajectory, no
        /// downstream link and an unresolved termination; the other is built with one, so the existing
        /// Stage 3C path populates all three. Rank, chains, beds, rays and the divide-end order must
        /// come out identical.
        /// </remarks>
        [Test]
        public void RankAndCandidateGeometryDoNotDependOnTopology()
        {
            const int seed = 110201;
            var stage2b = Stage2BMassifGenerator.Generate(
                seed, TerrainSettings, MacroSettings, MassifSettings);

            var partition = Stage3ASkeleton.Build(stage2b, RidgeSettings);

            var bare = RidgeSkeleton.Build(partition, RidgeSettings, new Seed.WorldSeed(seed));
            var populated = RidgeSkeleton.Build(
                partition, RidgeSettings, new Seed.WorldSeed(seed), stage2b.Stage1);

            // The second really does carry topology, or the comparison proves nothing.
            var withTopology = 0;
            for (var i = 0; i < populated.Valleys.Count; i++)
            {
                if (populated.Valleys[i].HasTrajectory
                    || populated.Valleys[i].DownstreamId >= 0
                    || populated.Valleys[i].Termination != ValleyTermination.Unresolved)
                {
                    withTopology++;
                }
            }

            Assert.That(
                withTopology, Is.GreaterThan(0),
                "the populated skeleton carries no topology, so this test cannot show independence");

            var a = ValleyNetwork.Build(bare, RidgeSettings);
            var b = ValleyNetwork.Build(populated, RidgeSettings);

            Assert.AreEqual(a.Ranks.Count, b.Ranks.Count, "corridor count");

            for (var i = 0; i < a.Ranks.Count; i++)
            {
                Assert.AreEqual(
                    a.Ranks[i].ReliefVolumeCum, b.Ranks[i].ReliefVolumeCum, $"corridor {i}: rank");
                Assert.AreEqual(
                    a.CandidateBeds[i].Length, b.CandidateBeds[i].Length, $"corridor {i}: bed samples");

                for (var k = 0; k < a.CandidateBeds[i].Length; k++)
                {
                    Assert.AreEqual(
                        a.CandidateBeds[i][k], b.CandidateBeds[i][k], $"corridor {i} bed point {k}");
                }

                Assert.AreEqual(
                    a.CandidateRays[i].Origin, b.CandidateRays[i].Origin, $"corridor {i}: ray origin");
                Assert.AreEqual(
                    a.CandidateRays[i].Direction, b.CandidateRays[i].Direction,
                    $"corridor {i}: ray direction");
            }

            Assert.AreEqual(a.ToeEvents.Count, b.ToeEvents.Count, "toe events");
            for (var i = 0; i < a.ToeEvents.Count; i++)
            {
                Assert.AreEqual(a.ToeEvents[i].SpurId, b.ToeEvents[i].SpurId, $"toe event {i}");
                Assert.AreEqual(
                    a.ToeEvents[i].ToeFraction, b.ToeEvents[i].ToeFraction, $"toe event {i}: fraction");
            }

            for (var i = 0; i < a.RankOrder.Count; i++)
            {
                Assert.AreEqual(a.RankOrder[i], b.RankOrder[i], $"rank order {i}");
            }
        }

        /// <summary>A3. Ties resolve by head crest, then by id, and never ambiguously.</summary>
        [Test]
        public void RankTiesBreakDeterministically()
        {
            var lowId = new ValleyRank(3, 100f, 1000f, 50f);
            var highId = new ValleyRank(9, 100f, 1000f, 50f);
            var taller = new ValleyRank(9, 100f, 1000f, 80f);
            var bigger = new ValleyRank(9, 100f, 2000f, 10f);

            // Equal volume and equal crest: the lower id wins.
            Assert.That(ValleyNetwork.CompareRank(lowId, highId), Is.LessThan(0), "id tie-break");
            Assert.That(ValleyNetwork.CompareRank(highId, lowId), Is.GreaterThan(0), "id tie-break is antisymmetric");

            // Equal volume, different crest: the taller head wins regardless of id.
            Assert.That(ValleyNetwork.CompareRank(taller, lowId), Is.LessThan(0), "crest tie-break");

            // Volume dominates both tie-breaks.
            Assert.That(ValleyNetwork.CompareRank(bigger, taller), Is.LessThan(0), "volume dominates");

            // A total order: comparing anything with itself is zero.
            Assert.AreEqual(0, ValleyNetwork.CompareRank(lowId, lowId), "reflexive");
        }

        /// <summary>The ranking quantity has the dimensions the design claims.</summary>
        /// <remarks>
        /// <c>SWidth</c> is a normalised arc interval and therefore dimensionless, so the footprint is
        /// metres times metres and the ranking quantity is metres cubed. Checked against the parts
        /// rather than assumed, because a units slip here would be invisible in an ordering.
        /// </remarks>
        [Test]
        public void ReliefVolumeIsFootprintTimesHeadCrest()
        {
            for (var w = 0; w < networks.Length; w++)
            {
                var network = networks[w];
                for (var i = 0; i < network.Ranks.Count; i++)
                {
                    var corridor = network.Ridges.Valleys[i];
                    var massif = network.Ridges.Ownership.Massifs[corridor.MassifIndex];
                    var rank = network.Ranks[i];

                    var expectedFootprint = corridor.SWidth * massif.LengthMeters
                        * ValleyTrajectoryBuilder.FlankHalfWidth(
                            massif, corridor.HeadS, corridor.FlankSign);

                    Assert.AreEqual(
                        expectedFootprint, rank.FlankFootprintSqm, expectedFootprint * 1e-4f + 1e-3f,
                        $"seed {Seeds[w]} corridor {i}: footprint");
                    Assert.AreEqual(
                        massif.CrestAt(corridor.HeadS), rank.HeadCrestMeters, 1e-3f,
                        $"seed {Seeds[w]} corridor {i}: head crest");
                    Assert.AreEqual(
                        rank.FlankFootprintSqm * rank.HeadCrestMeters, rank.ReliefVolumeCum,
                        rank.ReliefVolumeCum * 1e-4f + 1e-3f,
                        $"seed {Seeds[w]} corridor {i}: volume");
                    Assert.That(
                        rank.FlankFootprintSqm, Is.GreaterThan(0f),
                        $"seed {Seeds[w]} corridor {i}: non-positive footprint");
                }
            }
        }

        // ---------- A4, A5, A6: physical adjacency ----------

        /// <summary>A4. Every neighbour relationship comes from a shared bounding spur.</summary>
        [Test]
        public void AdjacencyComesOnlyFromSharedBoundingSpurs()
        {
            var pairs = 0;

            for (var w = 0; w < networks.Length; w++)
            {
                var network = networks[w];
                for (var i = 0; i < network.Ridges.Valleys.Count; i++)
                {
                    var corridor = network.Ridges.Valleys[i];

                    var upperId = network.UpperNeighbourOf(i);
                    if (upperId >= 0)
                    {
                        var upper = network.Ridges.Valleys[upperId];
                        Assert.That(
                            corridor.UpperSpurId, Is.GreaterThanOrEqualTo(0),
                            $"seed {Seeds[w]} corridor {i}: neighbour without a bounding spur");
                        Assert.AreEqual(
                            corridor.UpperSpurId, upper.LowerSpurId,
                            $"seed {Seeds[w]} corridors {i}/{upperId}: no shared spur");
                        Assert.AreEqual(
                            corridor.MassifIndex, upper.MassifIndex, $"seed {Seeds[w]}: cross-massif");
                        Assert.AreEqual(
                            corridor.FlankSign, upper.FlankSign, $"seed {Seeds[w]}: cross-flank");
                        pairs++;
                    }

                    var lowerId = network.LowerNeighbourOf(i);
                    if (lowerId >= 0)
                    {
                        Assert.AreEqual(
                            i, network.UpperNeighbourOf(lowerId),
                            $"seed {Seeds[w]} corridor {i}: adjacency is not symmetric");
                    }
                }
            }

            Assert.That(pairs, Is.GreaterThan(0), "no adjacency anywhere in the seed set");
        }

        /// <summary>A5. Corridors that share no spur are not adjacent, however close they are.</summary>
        [Test]
        public void NonAdjacentCorridorsStayNonAdjacent()
        {
            for (var w = 0; w < networks.Length; w++)
            {
                var network = networks[w];
                var corridors = network.Ridges.Valleys;

                for (var a = 0; a < corridors.Count; a++)
                {
                    for (var b = 0; b < corridors.Count; b++)
                    {
                        if (a == b)
                        {
                            continue;
                        }

                        var shares =
                            (corridors[a].UpperSpurId >= 0
                             && corridors[a].UpperSpurId == corridors[b].LowerSpurId)
                            || (corridors[a].LowerSpurId >= 0
                                && corridors[a].LowerSpurId == corridors[b].UpperSpurId);

                        Assert.AreEqual(
                            shares, network.AreAdjacent(a, b),
                            $"seed {Seeds[w]}: corridors {a}/{b} adjacency disagrees with their spurs");
                    }
                }
            }
        }

        /// <summary>A6. The chain is the physical arc order, and its spurs sit between its members.</summary>
        [Test]
        public void ChainIsThePhysicalArcOrder()
        {
            var chainsChecked = 0;

            for (var w = 0; w < networks.Length; w++)
            {
                var network = networks[w];
                for (var c = 0; c < network.Chains.Count; c++)
                {
                    var chain = network.Chains[c];
                    Assert.That(chain.Length, Is.GreaterThan(0), $"seed {Seeds[w]}: empty chain");
                    Assert.AreEqual(
                        chain.Length - 1, chain.SeparatingSpurs.Length,
                        $"seed {Seeds[w]} chain {c}: spur count");

                    for (var i = 0; i < chain.Length; i++)
                    {
                        var corridor = network.Ridges.Valleys[chain.Corridors[i]];
                        Assert.AreEqual(
                            chain.MassifIndex, corridor.MassifIndex, $"seed {Seeds[w]}: massif");
                        Assert.AreEqual(chain.FlankSign, corridor.FlankSign, $"seed {Seeds[w]}: flank");

                        if (i == 0)
                        {
                            continue;
                        }

                        var previous = network.Ridges.Valleys[chain.Corridors[i - 1]];
                        Assert.That(
                            corridor.SInterval.x, Is.GreaterThanOrEqualTo(previous.SInterval.x),
                            $"seed {Seeds[w]} chain {c}: out of arc order at {i}");
                        Assert.AreEqual(
                            previous.UpperSpurId, chain.SeparatingSpurs[i - 1],
                            $"seed {Seeds[w]} chain {c}: separating spur {i - 1}");
                        Assert.AreEqual(
                            corridor.LowerSpurId, chain.SeparatingSpurs[i - 1],
                            $"seed {Seeds[w]} chain {c}: separating spur seen from above");
                    }

                    chainsChecked++;
                }
            }

            Assert.That(chainsChecked, Is.GreaterThan(0), "no chains built");
        }

        // ---------- A7: divide-end ordering ----------

        /// <summary>A7. Divide-end events are ordered by toe position, not by rank.</summary>
        [Test]
        public void ToeEventsAreOrderedByToePositionAndNotByRank()
        {
            var events = 0;

            for (var w = 0; w < networks.Length; w++)
            {
                var network = networks[w];

                for (var i = 1; i < network.ToeEvents.Count; i++)
                {
                    var previous = network.ToeEvents[i - 1];
                    var here = network.ToeEvents[i];

                    Assert.That(
                        here.ToeFraction, Is.GreaterThanOrEqualTo(previous.ToeFraction),
                        $"seed {Seeds[w]}: toe events out of order at {i}");

                    if (Mathf.Approximately(here.ToeFraction, previous.ToeFraction))
                    {
                        Assert.That(
                            here.SpurId, Is.GreaterThan(previous.SpurId),
                            $"seed {Seeds[w]}: tied toe fractions not broken by spur id");
                    }
                }

                // Each event names the spur that actually separates its two corridors.
                for (var i = 0; i < network.ToeEvents.Count; i++)
                {
                    var e = network.ToeEvents[i];
                    var lower = network.Ridges.Valleys[e.LowerCorridorId];
                    var upper = network.Ridges.Valleys[e.UpperCorridorId];

                    Assert.AreEqual(e.SpurId, lower.UpperSpurId, $"seed {Seeds[w]} event {i}: lower");
                    Assert.AreEqual(e.SpurId, upper.LowerSpurId, $"seed {Seeds[w]} event {i}: upper");
                    Assert.IsTrue(
                        network.AreAdjacent(e.LowerCorridorId, e.UpperCorridorId),
                        $"seed {Seeds[w]} event {i}: corridors are not neighbours");
                    Assert.That(
                        e.ToeFraction, Is.GreaterThan(0f), $"seed {Seeds[w]} event {i}: toe at the crest");
                    events++;
                }
            }

            Assert.That(events, Is.GreaterThan(0), "no divide-end events");
        }

        // ---------- A8, A10: candidate geometry ----------

        /// <summary>A8. Candidate beds and rays repeat exactly.</summary>
        [Test]
        public void CandidateGeometryIsDeterministic()
        {
            for (var w = 0; w < networks.Length; w++)
            {
                var again = ValleyNetwork.Build(Skeleton(Seeds[w]), RidgeSettings);
                var first = networks[w];

                for (var i = 0; i < first.CandidateBeds.Count; i++)
                {
                    Assert.AreEqual(
                        first.CandidateBeds[i].Length, again.CandidateBeds[i].Length,
                        $"seed {Seeds[w]} corridor {i}: bed samples");

                    for (var k = 0; k < first.CandidateBeds[i].Length; k++)
                    {
                        Assert.AreEqual(
                            first.CandidateBeds[i][k], again.CandidateBeds[i][k],
                            $"seed {Seeds[w]} corridor {i} bed point {k}");
                    }

                    Assert.AreEqual(
                        first.CandidateRays[i].Origin, again.CandidateRays[i].Origin,
                        $"seed {Seeds[w]} corridor {i}: ray origin");
                    Assert.AreEqual(
                        first.CandidateRays[i].Direction, again.CandidateRays[i].Direction,
                        $"seed {Seeds[w]} corridor {i}: ray direction");
                }
            }
        }

        /// <summary>A candidate bed starts on the crest, ends at the flank toe, and has a tangent.</summary>
        [Test]
        public void CandidateBedsRunFromCrestToFlankToe()
        {
            for (var w = 0; w < networks.Length; w++)
            {
                var network = networks[w];
                Assert.AreEqual(0, network.Stats.InvalidRays, $"seed {Seeds[w]}: a bed had no tangent");

                for (var i = 0; i < network.CandidateBeds.Count; i++)
                {
                    var bed = network.CandidateBeds[i];
                    var corridor = network.Ridges.Valleys[i];
                    var massif = network.Ridges.Ownership.Massifs[corridor.MassifIndex];

                    Assert.That(
                        bed.Length, Is.GreaterThanOrEqualTo(2),
                        $"seed {Seeds[w]} corridor {i}: bed too short");

                    var headToAxis = massif.Axis.DistanceTo(bed[0].x, bed[0].y);
                    Assert.That(
                        headToAxis, Is.LessThan(RidgeSettings.SpurSampleSpacingMeters),
                        $"seed {Seeds[w]} corridor {i}: head {headToAxis:F0} m off the crest");

                    var toeToAxis = massif.Axis.DistanceTo(
                        bed[bed.Length - 1].x, bed[bed.Length - 1].y);
                    Assert.That(
                        toeToAxis, Is.GreaterThan(headToAxis),
                        $"seed {Seeds[w]} corridor {i}: bed does not descend the flank");

                    Assert.IsTrue(
                        network.CandidateRays[i].IsValid, $"seed {Seeds[w]} corridor {i}: invalid ray");
                    Assert.AreEqual(
                        bed[bed.Length - 1], network.CandidateRays[i].Origin,
                        $"seed {Seeds[w]} corridor {i}: ray does not start at the bed's end");
                }
            }
        }

        /// <summary>A10. Building the network edits no terrain.</summary>
        [Test]
        public void BuildingTheNetworkChangesNoField()
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
            var network = ValleyNetwork.Build(ridges, RidgeSettings);

            Assert.That(network.Stats.CorridorCount, Is.GreaterThan(0), "no corridors to speak of");
            Assert.AreEqual(stage1Before, stage2b.Stage1.Field.Hash(), "the Stage 1 field moved");
            Assert.AreEqual(stage2Before, stage2b.Field.Hash(), "the Stage 2 field moved");
            Assert.AreEqual(upliftBefore, stage2b.UpliftField.Hash(), "the uplift field moved");
        }

        // ---------- A11, A12: occupancy and non-mutation ----------

        /// <summary>A11. Every physical position starts holding its own corridor.</summary>
        [Test]
        public void OccupancyStartsAsTheIdentity()
        {
            for (var w = 0; w < networks.Length; w++)
            {
                var network = networks[w];
                for (var c = 0; c < network.Chains.Count; c++)
                {
                    var chain = network.Chains[c];
                    for (var i = 0; i < chain.Length; i++)
                    {
                        Assert.AreEqual(
                            chain.Corridors[i], network.OccupantAt(c, i),
                            $"seed {Seeds[w]} chain {c} position {i}: occupancy is not the identity");
                        Assert.AreEqual(
                            i, chain.PositionOf(chain.Corridors[i]),
                            $"seed {Seeds[w]} chain {c}: position lookup disagrees");
                    }
                }
            }
        }

        /// <summary>A12. This slice performs no merge and writes no topology.</summary>
        /// <remarks>
        /// The whole point of stopping here. If any of these moved, the convergence test a later slice
        /// runs would be consuming something it had already influenced.
        /// </remarks>
        [Test]
        public void NoTopologyIsWritten()
        {
            for (var w = 0; w < networks.Length; w++)
            {
                var skeleton = Skeleton(Seeds[w]);

                var before = new int[skeleton.Valleys.Count];
                var terminations = new ValleyTermination[skeleton.Valleys.Count];
                for (var i = 0; i < skeleton.Valleys.Count; i++)
                {
                    before[i] = skeleton.Valleys[i].DownstreamId;
                    terminations[i] = skeleton.Valleys[i].Termination;
                }

                var count = skeleton.Valleys.Count;
                ValleyNetwork.Build(skeleton, RidgeSettings);

                Assert.AreEqual(count, skeleton.Valleys.Count, $"seed {Seeds[w]}: corridor count changed");

                for (var i = 0; i < skeleton.Valleys.Count; i++)
                {
                    Assert.AreEqual(
                        before[i], skeleton.Valleys[i].DownstreamId,
                        $"seed {Seeds[w]} corridor {i}: a downstream link was written");
                    Assert.AreEqual(
                        terminations[i], skeleton.Valleys[i].Termination,
                        $"seed {Seeds[w]} corridor {i}: a termination was rewritten");
                }
            }
        }

        /// <summary>A world with no massif builds an empty network rather than failing.</summary>
        [Test]
        public void AWorldWithoutAMassifBuildsAnEmptyNetwork()
        {
            var barren = 0;

            for (var w = 0; w < networks.Length; w++)
            {
                if (networks[w].Ridges.Ownership.Massifs.Count > 0)
                {
                    continue;
                }

                barren++;
                Assert.AreEqual(0, networks[w].Stats.CorridorCount, $"seed {Seeds[w]}: corridors");
                Assert.AreEqual(0, networks[w].Chains.Count, $"seed {Seeds[w]}: chains");
                Assert.AreEqual(0, networks[w].ToeEvents.Count, $"seed {Seeds[w]}: toe events");
            }

            Assert.That(barren, Is.GreaterThan(0), "the seed set no longer covers a world without a massif");
        }

        // ---------- fixtures ----------

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
