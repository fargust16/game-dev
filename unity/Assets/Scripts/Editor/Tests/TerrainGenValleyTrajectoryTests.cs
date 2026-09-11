using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.TerrainGen.Tests
{
    /// <summary>
    /// Stage 3C slice C: the final valley trajectories.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The one thing this slice must get right is that it <em>consumes</em> topology. Slice B2 decided
    /// which channels converge and where; these assert that the final network reproduces that decision
    /// exactly - no link Slice B2 did not make, none of the links it did make missing, no second merge
    /// test hiding in the geometry - and that the curves laid along it are continuous, acyclic and
    /// shared rather than duplicated.
    /// </para>
    /// <para>
    /// They also assert what this slice must not touch: rank, candidate beds and rays, event ordering,
    /// the corridors themselves and Stage 1's height field.
    /// </para>
    /// </remarks>
    public sealed class TerrainGenValleyTrajectoryTests
    {
        private static readonly int[] Seeds =
        {
            110201, 424242, 20260905, 7, 101010, 20261231, 1337,
        };

        private static TerrainGenSettings TerrainSettings => TerrainGenSettings.Default;
        private static MacroStructureSettings MacroSettings => MacroStructureSettings.Default;
        private static MassifSettings MassifSettings => MassifSettings.Default;
        private static RidgeSettings RidgeSettings => RidgeSettings.Default;

        private Stage1Result[] stage1;
        private RidgeSkeleton[] skeletons;
        private ValleyNetwork[] networks;
        private ValleyTopology[] topologies;
        private ValleyTrajectoryNetwork[] finals;

        [OneTimeSetUp]
        public void BuildWorlds()
        {
            stage1 = new Stage1Result[Seeds.Length];
            skeletons = new RidgeSkeleton[Seeds.Length];
            networks = new ValleyNetwork[Seeds.Length];
            topologies = new ValleyTopology[Seeds.Length];
            finals = new ValleyTrajectoryNetwork[Seeds.Length];

            for (var i = 0; i < Seeds.Length; i++)
            {
                stage1[i] = Stage1LandmassGenerator.Generate(Seeds[i], TerrainSettings);
                skeletons[i] = Skeleton(Seeds[i]);
                networks[i] = ValleyNetwork.Build(skeletons[i], RidgeSettings);
                topologies[i] = ValleyTopology.Build(networks[i], RidgeSettings);
                finals[i] = ValleyTrajectoryNetwork.Build(topologies[i], RidgeSettings, stage1[i]);
            }
        }

        // ---------- 1..8: the network mirrors the topology ----------

        /// <summary>1. Every corridor Slice B2 knows about comes out with a usable trajectory.</summary>
        [Test]
        public void EverySurvivorProducesAValidTrajectory()
        {
            for (var w = 0; w < finals.Length; w++)
            {
                var final = finals[w];
                Assert.AreEqual(
                    skeletons[w].Valleys.Count, final.Trajectories.Count,
                    $"seed {Seeds[w]}: trajectory count");

                for (var i = 0; i < final.Trajectories.Count; i++)
                {
                    var trajectory = final.Trajectories[i];
                    Assert.AreEqual(i, trajectory.Id, $"seed {Seeds[w]}: trajectory {i} identity");
                    Assert.That(
                        trajectory.Points.Length, Is.GreaterThanOrEqualTo(2),
                        $"seed {Seeds[w]} V{i}: no geometry");
                    Assert.AreNotEqual(
                        ValleyTermination.Unresolved, trajectory.Termination,
                        $"seed {Seeds[w]} V{i}: unresolved termination");

                    for (var k = 0; k < trajectory.Points.Length; k++)
                    {
                        var p = trajectory.Points[k];
                        Assert.IsFalse(
                            float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsInfinity(p.x)
                            || float.IsInfinity(p.y),
                            $"seed {Seeds[w]} V{i} sample {k}: not a finite point");
                    }
                }

                Assert.AreEqual(
                    0, final.Stats.WithoutGeometry, $"seed {Seeds[w]}: corridors without geometry");
            }
        }

        /// <summary>2. Each accepted merge appears once, on the channel that survived it.</summary>
        [Test]
        public void EveryAcceptedMergeAppearsExactlyOnce()
        {
            for (var w = 0; w < finals.Length; w++)
            {
                var final = finals[w];
                var seen = new Dictionary<int, int>();

                for (var i = 0; i < final.Trajectories.Count; i++)
                {
                    var trajectory = final.Trajectories[i];
                    Assert.AreEqual(
                        trajectory.ConfluenceIndices.Length, trajectory.TributaryIds.Length,
                        $"seed {Seeds[w]} V{i}: confluence bookkeeping");

                    for (var k = 0; k < trajectory.TributaryIds.Length; k++)
                    {
                        var tributary = trajectory.TributaryIds[k];
                        Assert.IsFalse(
                            seen.ContainsKey(tributary),
                            $"seed {Seeds[w]}: V{tributary} joins more than one channel, V{i} among them");
                        seen[tributary] = i;
                    }
                }

                Assert.AreEqual(
                    topologies[w].Stats.MergeCount, seen.Count,
                    $"seed {Seeds[w]}: confluence count against Slice B2's merge count");
                Assert.AreEqual(
                    topologies[w].Stats.MergeCount, final.Stats.ConfluenceCount,
                    $"seed {Seeds[w]}: stats disagree with the geometry");
            }
        }

        /// <summary>3. No pair Slice B2 turned down becomes connected here.</summary>
        [Test]
        public void NoRejectedPairBecomesConnected()
        {
            for (var w = 0; w < finals.Length; w++)
            {
                var final = finals[w];
                var topology = topologies[w];

                for (var i = 0; i < final.Trajectories.Count; i++)
                {
                    Assert.AreEqual(
                        topology.DownstreamOf[i], final.Trajectories[i].DownstreamId,
                        $"seed {Seeds[w]} V{i}: downstream link differs from Slice B2's");

                    if (topology.DownstreamOf[i] < 0)
                    {
                        Assert.AreNotEqual(
                            ValleyTermination.Merge, final.Trajectories[i].Termination,
                            $"seed {Seeds[w]} V{i}: merged without Slice B2 saying so");
                    }
                }
            }
        }

        /// <summary>4. Every link joins two physically adjacent corridors.</summary>
        [Test]
        public void NoSyntheticAdjacencyIsIntroduced()
        {
            for (var w = 0; w < finals.Length; w++)
            {
                var network = networks[w];
                foreach (var trajectory in finals[w].Trajectories)
                {
                    if (!trajectory.IsTributary)
                    {
                        continue;
                    }

                    Assert.IsTrue(
                        network.AreAdjacent(trajectory.Id, trajectory.DownstreamId),
                        $"seed {Seeds[w]}: V{trajectory.Id} joins non-adjacent V{trajectory.DownstreamId}");
                    Assert.AreEqual(
                        network.ChainOf(trajectory.Id), network.ChainOf(trajectory.DownstreamId),
                        $"seed {Seeds[w]}: V{trajectory.Id} joins across chains");
                }
            }
        }

        /// <summary>5. No chain closes: the ends of a flank never join each other.</summary>
        [Test]
        public void NoChainClosureOccurs()
        {
            for (var w = 0; w < finals.Length; w++)
            {
                var network = networks[w];
                for (var c = 0; c < network.Chains.Count; c++)
                {
                    var chain = network.Chains[c];
                    if (chain.Length < 3)
                    {
                        continue;
                    }

                    var first = chain.Corridors[0];
                    var last = chain.Corridors[chain.Length - 1];
                    var a = finals[w].Trajectories[first];
                    var b = finals[w].Trajectories[last];

                    Assert.AreNotEqual(
                        last, a.DownstreamId, $"seed {Seeds[w]} chain {c}: first joins last");
                    Assert.AreNotEqual(
                        first, b.DownstreamId, $"seed {Seeds[w]} chain {c}: last joins first");
                }
            }
        }

        /// <summary>6. The final network is a forest: every walk downstream terminates.</summary>
        [Test]
        public void FinalNetworkIsAcyclic()
        {
            for (var w = 0; w < finals.Length; w++)
            {
                var final = finals[w];
                for (var i = 0; i < final.Trajectories.Count; i++)
                {
                    var depth = 0;
                    var here = i;
                    while (final.Trajectories[here].IsTributary)
                    {
                        here = final.Trajectories[here].DownstreamId;
                        Assert.That(
                            ++depth, Is.LessThanOrEqualTo(final.Trajectories.Count),
                            $"seed {Seeds[w]}: V{i} is in a downstream cycle");
                    }

                    Assert.IsFalse(
                        final.Trajectories[here].IsTributary, $"seed {Seeds[w]}: V{i} has no trunk");
                    Assert.AreEqual(here, final.RootOf(i), $"seed {Seeds[w]} V{i}: root");
                    Assert.AreEqual(depth, final.DepthOf(i), $"seed {Seeds[w]} V{i}: depth");
                }
            }
        }

        /// <summary>7. Ordering is fixed: trajectories by id, confluences downstream along the channel.</summary>
        [Test]
        public void OrderingIsDeterministic()
        {
            for (var w = 0; w < finals.Length; w++)
            {
                var final = finals[w];
                for (var i = 1; i < final.Trunks.Count; i++)
                {
                    Assert.That(
                        final.Trunks[i], Is.GreaterThan(final.Trunks[i - 1]),
                        $"seed {Seeds[w]}: trunks are not in ascending id order");
                }

                foreach (var trajectory in final.Trajectories)
                {
                    for (var k = 1; k < trajectory.ConfluenceIndices.Length; k++)
                    {
                        Assert.That(
                            trajectory.ConfluenceIndices[k],
                            Is.GreaterThanOrEqualTo(trajectory.ConfluenceIndices[k - 1]),
                            $"seed {Seeds[w]} V{trajectory.Id}: confluences are not downstream-ordered");
                    }
                }
            }
        }

        /// <summary>8. The same seed rebuilds the same trajectories, sample for sample.</summary>
        [Test]
        public void SameSeedProducesIdenticalTrajectories()
        {
            for (var w = 0; w < finals.Length; w++)
            {
                var rebuilt = ValleyTrajectoryNetwork.Build(
                    ValleyTopology.Build(ValleyNetwork.Build(Skeleton(Seeds[w]), RidgeSettings)),
                    RidgeSettings,
                    Stage1LandmassGenerator.Generate(Seeds[w], TerrainSettings));

                Assert.AreEqual(
                    finals[w].Trajectories.Count, rebuilt.Trajectories.Count,
                    $"seed {Seeds[w]}: trajectory count");

                for (var i = 0; i < finals[w].Trajectories.Count; i++)
                {
                    var a = finals[w].Trajectories[i];
                    var b = rebuilt.Trajectories[i];

                    Assert.AreEqual(a.Points.Length, b.Points.Length, $"seed {Seeds[w]} V{i}: samples");
                    Assert.AreEqual(a.DownstreamId, b.DownstreamId, $"seed {Seeds[w]} V{i}: downstream");
                    Assert.AreEqual(a.Termination, b.Termination, $"seed {Seeds[w]} V{i}: termination");
                    Assert.AreEqual(a.LengthMeters, b.LengthMeters, 0f, $"seed {Seeds[w]} V{i}: length");
                    CollectionAssert.AreEqual(
                        a.ConfluenceIndices, b.ConfluenceIndices,
                        $"seed {Seeds[w]} V{i}: confluence indices");
                    CollectionAssert.AreEqual(
                        a.TributaryIds, b.TributaryIds, $"seed {Seeds[w]} V{i}: tributaries");

                    for (var k = 0; k < a.Points.Length; k++)
                    {
                        Assert.AreEqual(a.Points[k].x, b.Points[k].x, 0f, $"seed {Seeds[w]} V{i}:{k} x");
                        Assert.AreEqual(a.Points[k].y, b.Points[k].y, 0f, $"seed {Seeds[w]} V{i}:{k} y");
                    }
                }
            }
        }

        // ---------- 9..13: the confluences themselves ----------

        /// <summary>9. A confluence is never upstream of the divide whose end allowed it.</summary>
        [Test]
        public void ConfluenceIsNoEarlierThanItsToe()
        {
            var checkedAny = 0;

            for (var w = 0; w < finals.Length; w++)
            {
                var topology = topologies[w];
                var skeleton = skeletons[w];

                for (var e = 0; e < topology.Events.Count; e++)
                {
                    var result = topology.Events[e];
                    if (!result.Merged)
                    {
                        continue;
                    }

                    var toeEvent = networks[w].ToeEvents[result.EventIndex];
                    var corridor = skeleton.Valleys[result.WinnerId];
                    var massif = skeleton.Ownership.Massifs[corridor.MassifIndex];

                    massif.Project(toeEvent.ToePoint.x, toeEvent.ToePoint.y, out _, out var toeOffset);
                    massif.Project(
                        result.ConfluencePoint.x, result.ConfluencePoint.y, out _, out var atOffset);

                    checkedAny++;
                    Assert.That(
                        Mathf.Abs(atOffset), Is.GreaterThanOrEqualTo(Mathf.Abs(toeOffset) - 1e-2f),
                        $"seed {Seeds[w]}: confluence of V{result.LoserId} is upstream of S{toeEvent.SpurId}");
                }
            }

            Assert.That(checkedAny, Is.GreaterThan(0), "the seed set no longer contains a merge");
        }

        /// <summary>10. A confluence is on land and inside structural ownership.</summary>
        [Test]
        public void ConfluenceIsOnLandInsideStructuralDomain()
        {
            for (var w = 0; w < finals.Length; w++)
            {
                var skeleton = skeletons[w];
                foreach (var trajectory in finals[w].Trajectories)
                {
                    if (!trajectory.IsTributary)
                    {
                        continue;
                    }

                    var at = trajectory.EndPoint;
                    Assert.That(
                        stage1[w].Coast.SignedDistance(at.x, at.y), Is.GreaterThan(0f),
                        $"seed {Seeds[w]}: V{trajectory.Id} joins at sea");
                    Assert.IsTrue(
                        skeleton.Ownership.Ownership.At(at.x, at.y).IsStructural,
                        $"seed {Seeds[w]}: V{trajectory.Id} joins outside structural ownership");
                }
            }
        }

        /// <summary>11. A tributary reaches its confluence exactly, and arrives heading downstream.</summary>
        [Test]
        public void UpstreamTrajectoriesConnectContinuouslyToConfluences()
        {
            for (var w = 0; w < finals.Length; w++)
            {
                var final = finals[w];
                foreach (var trajectory in final.Trajectories)
                {
                    if (!trajectory.IsTributary)
                    {
                        continue;
                    }

                    var survivor = final.Trajectories[trajectory.DownstreamId];
                    var index = ConfluenceIndexOf(survivor, trajectory.Id);
                    Assert.That(
                        index, Is.GreaterThanOrEqualTo(0),
                        $"seed {Seeds[w]}: V{survivor.Id} does not carry V{trajectory.Id}");

                    Assert.That(
                        (trajectory.EndPoint - survivor.Points[index]).magnitude, Is.LessThan(1e-3f),
                        $"seed {Seeds[w]}: V{trajectory.Id} does not reach its confluence");

                    var onward = survivor.OutgoingAt(index);
                    var into = trajectory.TangentAt(trajectory.Points.Length - 1);
                    if (onward == Vector2.zero || into == Vector2.zero)
                    {
                        continue;
                    }

                    Assert.That(
                        Vector2.Angle(into, onward), Is.LessThan(90f),
                        $"seed {Seeds[w]}: V{trajectory.Id} arrives against the survivor's heading");
                }
            }
        }

        /// <summary>12. The survivor runs through its confluence as one curve, with no step in it.</summary>
        [Test]
        public void DownstreamSurvivorContinuesFromConfluenceWithoutDiscontinuity()
        {
            for (var w = 0; w < finals.Length; w++)
            {
                foreach (var survivor in finals[w].Trajectories)
                {
                    for (var k = 0; k < survivor.ConfluenceIndices.Length; k++)
                    {
                        var index = survivor.ConfluenceIndices[k];
                        Assert.That(
                            index, Is.GreaterThan(survivor.ToeIndex).And.LessThan(survivor.Points.Length),
                            $"seed {Seeds[w]} V{survivor.Id}: confluence sits inside the massif");

                        var into = survivor.TangentAt(index);
                        var onward = survivor.OutgoingAt(index);
                        if (into == Vector2.zero || onward == Vector2.zero)
                        {
                            continue;
                        }

                        Assert.That(
                            Vector2.Angle(into, onward), Is.LessThan(JoinTurnDegrees),
                            $"seed {Seeds[w]} V{survivor.Id}: the survivor kinks at its confluence");
                    }
                }
            }
        }

        /// <summary>13. A tributary stops at its confluence; the downstream stretch exists once.</summary>
        [Test]
        public void SharedDownstreamGeometryIsNotDuplicated()
        {
            for (var w = 0; w < finals.Length; w++)
            {
                var final = finals[w];
                foreach (var trajectory in final.Trajectories)
                {
                    if (!trajectory.IsTributary)
                    {
                        continue;
                    }

                    Assert.AreEqual(
                        ValleyTermination.Merge, trajectory.Termination,
                        $"seed {Seeds[w]} V{trajectory.Id}: a tributary that does not end at its merge");

                    var survivor = final.Trajectories[trajectory.DownstreamId];
                    var index = ConfluenceIndexOf(survivor, trajectory.Id);

                    // Nothing of the tributary exists below the junction, so the shared stretch is
                    // the survivor's and only the survivor's.
                    for (var k = index + 1; k < survivor.Points.Length; k++)
                    {
                        Assert.That(
                            Near(trajectory.Points, survivor.Points[k]), Is.GreaterThan(1e-3f),
                            $"seed {Seeds[w]}: V{trajectory.Id} duplicates the shared stretch of"
                            + $" V{survivor.Id}");
                    }
                }
            }
        }

        // ---------- 14..17: the curves ----------

        /// <summary>14. No trajectory crosses itself.</summary>
        [Test]
        public void NoTrajectorySelfIntersects()
        {
            for (var w = 0; w < finals.Length; w++)
            {
                foreach (var trajectory in finals[w].Trajectories)
                {
                    var points = trajectory.Points;
                    for (var i = 0; i + 1 < points.Length; i++)
                    {
                        for (var j = i + 2; j + 1 < points.Length; j++)
                        {
                            Assert.IsFalse(
                                SegmentsCross(points[i], points[i + 1], points[j], points[j + 1]),
                                $"seed {Seeds[w]} V{trajectory.Id}: segments {i} and {j} cross");
                        }
                    }
                }
            }
        }

        /// <summary>15. Every step goes forward: no zero steps, no reversals.</summary>
        [Test]
        public void NoBackwardMovement()
        {
            for (var w = 0; w < finals.Length; w++)
            {
                foreach (var trajectory in finals[w].Trajectories)
                {
                    var points = trajectory.Points;
                    for (var i = 1; i < points.Length; i++)
                    {
                        Assert.That(
                            (points[i] - points[i - 1]).magnitude, Is.GreaterThan(1e-4f),
                            $"seed {Seeds[w]} V{trajectory.Id}: repeated sample at {i}");

                        if (i < 2)
                        {
                            continue;
                        }

                        var a = points[i - 1] - points[i - 2];
                        var b = points[i] - points[i - 1];
                        Assert.That(
                            Vector2.Angle(a, b), Is.LessThan(90f),
                            $"seed {Seeds[w]} V{trajectory.Id}: step {i} doubles back");
                    }
                }
            }
        }

        /// <summary>16. C0 everywhere: consecutive samples never jump.</summary>
        [Test]
        public void C0ContinuityAtAllJoins()
        {
            var limit = RidgeSettings.SpurSampleSpacingMeters * 2f;

            for (var w = 0; w < finals.Length; w++)
            {
                foreach (var trajectory in finals[w].Trajectories)
                {
                    for (var i = 1; i < trajectory.Points.Length; i++)
                    {
                        Assert.That(
                            (trajectory.Points[i] - trajectory.Points[i - 1]).magnitude,
                            Is.LessThan(limit),
                            $"seed {Seeds[w]} V{trajectory.Id}: gap at sample {i}");
                    }
                }

                Assert.That(
                    finals[w].Stats.MaxConfluenceGapMeters, Is.LessThan(1e-3f),
                    $"seed {Seeds[w]}: a confluence is not shared exactly");
                Assert.That(
                    finals[w].Stats.MaxConfluenceOffRayMeters, Is.LessThan(1e-2f),
                    $"seed {Seeds[w]}: a confluence does not lie on the ray that established it");
            }
        }

        /// <summary>
        /// 17. Tangent breaks stay under the structural threshold the geometry itself sets.
        /// </summary>
        /// <remarks>
        /// The bound is not a taste: a continuation step may leave its predecessor's heading only by
        /// the angle its corridor's cone allows, and that cone is the two bounding spurs' terminal
        /// tangents. Nothing in this slice can turn further than that in one step, so a violation is a
        /// real defect rather than a threshold wanting adjusting.
        /// </remarks>
        [Test]
        public void TangentDiscontinuityStaysBelowTheStructuralThreshold()
        {
            for (var w = 0; w < finals.Length; w++)
            {
                foreach (var trajectory in finals[w].Trajectories)
                {
                    var points = trajectory.Points;
                    for (var i = 2; i < points.Length; i++)
                    {
                        var a = points[i - 1] - points[i - 2];
                        var b = points[i] - points[i - 1];
                        if (a.sqrMagnitude < 1e-10f || b.sqrMagnitude < 1e-10f)
                        {
                            continue;
                        }

                        Assert.That(
                            Vector2.Angle(a, b), Is.LessThan(JoinTurnDegrees),
                            $"seed {Seeds[w]} V{trajectory.Id}: sample {i} turns further than a"
                            + " continuation step may");
                    }
                }
            }
        }

        // ---------- 18..22: what must not have moved ----------

        /// <summary>18, 19, 20. Rank, candidate geometry and event ordering are untouched.</summary>
        [Test]
        public void RankCandidateGeometryAndEventOrderingAreUnchanged()
        {
            for (var w = 0; w < finals.Length; w++)
            {
                var before = ValleyNetwork.Build(Skeleton(Seeds[w]), RidgeSettings);
                var after = networks[w];

                for (var i = 0; i < before.Ranks.Count; i++)
                {
                    Assert.AreEqual(
                        before.Ranks[i].ReliefVolumeCum, after.Ranks[i].ReliefVolumeCum, 0f,
                        $"seed {Seeds[w]} V{i}: rank moved");
                    Assert.AreEqual(
                        before.Ranks[i].HeadCrestMeters, after.Ranks[i].HeadCrestMeters, 0f,
                        $"seed {Seeds[w]} V{i}: head crest moved");

                    var a = before.CandidateBeds[i];
                    var b = after.CandidateBeds[i];
                    Assert.AreEqual(a.Length, b.Length, $"seed {Seeds[w]} V{i}: bed samples");
                    for (var k = 0; k < a.Length; k++)
                    {
                        Assert.AreEqual(a[k], b[k], $"seed {Seeds[w]} V{i}: bed sample {k} moved");
                    }

                    Assert.AreEqual(
                        before.CandidateRays[i].Origin, after.CandidateRays[i].Origin,
                        $"seed {Seeds[w]} V{i}: ray origin moved");
                    Assert.AreEqual(
                        before.CandidateRays[i].Direction, after.CandidateRays[i].Direction,
                        $"seed {Seeds[w]} V{i}: ray direction moved");
                }

                CollectionAssert.AreEqual(
                    before.RankOrder, after.RankOrder, $"seed {Seeds[w]}: rank order moved");

                for (var e = 0; e < before.ToeEvents.Count; e++)
                {
                    Assert.AreEqual(
                        before.ToeEvents[e].SpurId, after.ToeEvents[e].SpurId,
                        $"seed {Seeds[w]} event {e}: ordering moved");
                    Assert.AreEqual(
                        before.ToeEvents[e].ToeFraction, after.ToeEvents[e].ToeFraction, 0f,
                        $"seed {Seeds[w]} event {e}: toe fraction moved");
                }
            }
        }

        /// <summary>21, 22. Slice C writes nothing: not the corridors, not Stage 1's heights.</summary>
        [Test]
        public void SliceCMutatesNothing()
        {
            for (var w = 0; w < finals.Length; w++)
            {
                foreach (var corridor in skeletons[w].Valleys)
                {
                    Assert.AreEqual(
                        0, corridor.Floor.Length, $"seed {Seeds[w]} V{corridor.Id}: floor written");
                    Assert.AreEqual(
                        -1, corridor.DownstreamId,
                        $"seed {Seeds[w]} V{corridor.Id}: downstream written");
                    Assert.AreEqual(
                        ValleyTermination.Unresolved, corridor.Termination,
                        $"seed {Seeds[w]} V{corridor.Id}: termination written");
                }

                var heights = stage1[w].Field.Heights;
                var fresh = Stage1LandmassGenerator.Generate(Seeds[w], TerrainSettings).Field.Heights;
                Assert.AreEqual(fresh.Length, heights.Length, $"seed {Seeds[w]}: height field resized");

                var checksum = 0.0;
                var reference = 0.0;
                for (var i = 0; i < heights.Length; i++)
                {
                    checksum += heights[i];
                    reference += fresh[i];
                }

                Assert.AreEqual(reference, checksum, 0.0, $"seed {Seeds[w]}: Stage 1 heights mutated");
            }
        }

        /// <summary>
        /// 23. Two valleys never cross inside a massif, and none crosses a divide.
        /// </summary>
        /// <remarks>
        /// Inside the massif each floor is contained by the two spurs that bound it, so a crossing
        /// there would mean a corridor had left its own ground. Past the foot that containment ends
        /// with the divides, and two continuations that Slice B2 found converging - but at a meeting
        /// point it refused, outside the domain or at sea - do run into each other on the apron. That
        /// is measured and reported rather than papered over; what this asserts is that it never
        /// happens where the structure still holds.
        /// </remarks>
        [Test]
        public void TrajectoriesNeverCrossInsideAMassifOrOverARidge()
        {
            for (var w = 0; w < finals.Length; w++)
            {
                var trajectories = finals[w].Trajectories;

                for (var a = 0; a < trajectories.Count; a++)
                {
                    for (var b = a + 1; b < trajectories.Count; b++)
                    {
                        var first = trajectories[a];
                        var second = trajectories[b];

                        for (var i = 0; i + 1 < first.Points.Length; i++)
                        {
                            for (var j = 0; j + 1 < second.Points.Length; j++)
                            {
                                if (!SegmentsCross(
                                        first.Points[i], first.Points[i + 1],
                                        second.Points[j], second.Points[j + 1]))
                                {
                                    continue;
                                }

                                Assert.That(
                                    i, Is.GreaterThan(first.ToeIndex),
                                    $"seed {Seeds[w]}: V{first.Id} and V{second.Id} cross inside the"
                                    + " massif");
                                Assert.That(
                                    j, Is.GreaterThan(second.ToeIndex),
                                    $"seed {Seeds[w]}: V{second.Id} and V{first.Id} cross inside the"
                                    + " massif");
                            }
                        }
                    }
                }

                foreach (var trajectory in trajectories)
                {
                    foreach (var spur in skeletons[w].Spurs)
                    {
                        for (var i = 0; i + 1 < trajectory.Points.Length; i++)
                        {
                            for (var j = 0; j + 1 < spur.Points.Length; j++)
                            {
                                Assert.IsFalse(
                                    SegmentsCross(
                                        trajectory.Points[i], trajectory.Points[i + 1],
                                        spur.Points[j], spur.Points[j + 1]),
                                    $"seed {Seeds[w]}: V{trajectory.Id} crosses divide S{spur.Id}");
                            }
                        }
                    }
                }
            }
        }

        // ---------- fixtures A..E: topology consumption, stated directly ----------

        /// <summary>A. A tributary hands over at the confluence; the survivor carries on alone.</summary>
        [Test]
        public void FixtureA_TributaryHandsOverToTheSurvivor()
        {
            var w = WorldWithAChainOf(3);
            var chain = LongestChain(networks[w]);
            var v0 = chain.Corridors[0];
            var v1 = chain.Corridors[1];

            var final = BuildRewired(w, new[] { v1 }, new[] { v0 }, new[] { OnRay(w, v0, 120f) });

            Assert.AreEqual(v0, final.Trajectories[v1].DownstreamId, "V1 joins V0");
            Assert.AreEqual(-1, final.Trajectories[v0].DownstreamId, "V0 survives");
            Assert.AreEqual(ValleyTermination.Merge, final.Trajectories[v1].Termination, "V1 ends merged");
            Assert.AreEqual(1, final.Trajectories[v0].TributaryIds.Length, "V0 carries one tributary");
            Assert.AreEqual(v1, final.Trajectories[v0].TributaryIds[0], "V0 carries V1");

            var index = final.Trajectories[v0].ConfluenceIndices[0];
            Assert.That(
                index, Is.LessThan(final.Trajectories[v0].Points.Length - 1),
                "V0 stops at the confluence instead of continuing past it");
            Assert.That(
                (final.Trajectories[v1].EndPoint - final.Trajectories[v0].Points[index]).magnitude,
                Is.LessThan(1e-3f), "V1 does not reach the confluence");
        }

        /// <summary>B. The same rules with the merge the other way up the chain.</summary>
        [Test]
        public void FixtureB_ReversedEventOrderingProducesTheSameShape()
        {
            var w = WorldWithAChainOf(3);
            var chain = LongestChain(networks[w]);
            var v1 = chain.Corridors[1];
            var v2 = chain.Corridors[2];

            var final = BuildRewired(w, new[] { v2 }, new[] { v1 }, new[] { OnRay(w, v1, 120f) });

            Assert.AreEqual(v1, final.Trajectories[v2].DownstreamId, "V2 joins V1");
            Assert.AreEqual(-1, final.Trajectories[v1].DownstreamId, "V1 survives");
            Assert.AreEqual(1, final.Trajectories[v1].TributaryIds.Length, "V1 carries one tributary");
            Assert.AreEqual(
                0, final.Trajectories[v2].TributaryIds.Length, "V2 carries a tributary it has not got");
            Assert.AreEqual(1, final.Stats.ConfluenceCount, "one confluence");
        }

        /// <summary>C. With no merges, every valley stays its own.</summary>
        [Test]
        public void FixtureC_IndependentValleysStayIndependent()
        {
            var w = WorldWithAChainOf(3);
            var final = BuildRewired(w, new int[0], new int[0], new Vector2[0]);

            Assert.AreEqual(0, final.Stats.ConfluenceCount, "a confluence appeared from nowhere");
            Assert.AreEqual(
                final.Trajectories.Count, final.Trunks.Count, "not every valley is its own trunk");

            foreach (var trajectory in final.Trajectories)
            {
                Assert.IsFalse(trajectory.IsTributary, $"V{trajectory.Id} acquired a downstream");
                Assert.AreNotEqual(
                    ValleyTermination.Merge, trajectory.Termination, $"V{trajectory.Id} merged");
                Assert.AreEqual(0, trajectory.TributaryIds.Length, $"V{trajectory.Id} acquired inflow");
            }
        }

        /// <summary>D. Two tributaries of one channel do not become connected to each other.</summary>
        [Test]
        public void FixtureD_NonAdjacentPairIsNotConnected()
        {
            var w = WorldWithAChainOf(3);
            var chain = LongestChain(networks[w]);
            var v0 = chain.Corridors[0];
            var v1 = chain.Corridors[1];
            var v2 = chain.Corridors[2];

            var final = BuildRewired(
                w, new[] { v0, v2 }, new[] { v1, v1 },
                new[] { OnRay(w, v1, 90f), OnRay(w, v1, 160f) });

            Assert.AreEqual(v1, final.Trajectories[v0].DownstreamId, "V0 joins V1");
            Assert.AreEqual(v1, final.Trajectories[v2].DownstreamId, "V2 joins V1");
            Assert.AreNotEqual(v2, final.Trajectories[v0].DownstreamId, "V0 joined V2");
            Assert.AreNotEqual(v0, final.Trajectories[v2].DownstreamId, "V2 joined V0");
            Assert.AreEqual(2, final.Trajectories[v1].TributaryIds.Length, "V1 carries both");

            // Two separate junctions on one channel, in downstream order, not one shared point.
            var indices = final.Trajectories[v1].ConfluenceIndices;
            Assert.That(indices[0], Is.LessThan(indices[1]), "the two junctions collapsed into one");
            Assert.AreEqual(v0, final.Trajectories[v1].TributaryIds[0], "the nearer junction is V0's");
        }

        /// <summary>E. A chain of merges stays acyclic, ordered and shared exactly once.</summary>
        [Test]
        public void FixtureE_ChainMergesIntoOneDownstreamPath()
        {
            var w = WorldWithAChainOf(3);
            var chain = LongestChain(networks[w]);
            var v0 = chain.Corridors[0];
            var v1 = chain.Corridors[1];
            var v2 = chain.Corridors[2];

            // V2 joins V1 partway down V1's own run, and V1 joins V0 further down still, so the two
            // confluences are in downstream order on the surviving channel.
            var near = OnRay(w, v1, 80f);
            var far = OnRay(w, v1, 240f);
            var final = BuildRewired(w, new[] { v2, v1 }, new[] { v1, v0 }, new[] { near, far });

            Assert.AreEqual(v1, final.Trajectories[v2].DownstreamId, "V2 joins V1");
            Assert.AreEqual(v0, final.Trajectories[v1].DownstreamId, "V1 joins V0");
            Assert.AreEqual(v0, final.RootOf(v2), "V2's trunk is V0");
            Assert.AreEqual(2, final.DepthOf(v2), "V2 is two merges from its trunk");
            Assert.AreEqual(0, final.Stats.ConfluencesOutOfOrder, "a confluence came out upstream");

            // V1 carries V2's junction and then ends at its own, in that order.
            var middle = final.Trajectories[v1];
            Assert.AreEqual(1, middle.TributaryIds.Length, "V1 carries one tributary");
            Assert.AreEqual(ValleyTermination.Merge, middle.Termination, "V1 ends merged");
            Assert.That(
                middle.ConfluenceIndices[0], Is.LessThan(middle.Points.Length - 1),
                "V1's own end is not downstream of the tributary it carries");

            // The stretch below V1's end belongs to V0 alone.
            var trunk = final.Trajectories[v0];
            var index = ConfluenceIndexOf(trunk, v1);
            for (var k = index + 1; k < trunk.Points.Length; k++)
            {
                Assert.That(
                    Near(middle.Points, trunk.Points[k]), Is.GreaterThan(1e-3f),
                    "V1 duplicates the stretch below its own confluence");
            }
        }

        // ---------- fixtures ----------

        /// <summary>
        /// The largest turn one step of a trajectory may take.
        /// </summary>
        /// <remarks>
        /// A continuation step is clamped into its corridor's cone, whose arms are the terminal
        /// tangents of the two bounding spurs; a quarter turn is well outside anything those can
        /// subtend, and a break beyond it could only be a join gone wrong.
        /// </remarks>
        private const float JoinTurnDegrees = 90f;

        private ValleyTrajectoryNetwork BuildRewired(
            int world, int[] losers, int[] winners, Vector2[] confluences)
        {
            var real = topologies[world];
            var network = real.Network;
            var count = network.Ranks.Count;

            var downstream = new int[count];
            var points = new Vector2[count];
            for (var i = 0; i < count; i++)
            {
                downstream[i] = -1;
            }

            for (var k = 0; k < losers.Length; k++)
            {
                downstream[losers[k]] = winners[k];
                points[losers[k]] = confluences[k];
            }

            var trunks = new List<int>();
            for (var i = 0; i < count; i++)
            {
                if (downstream[i] < 0)
                {
                    trunks.Add(i);
                }
            }

            var stats = new ValleyTopologyStats
            {
                Seed = real.Seed,
                CorridorCount = count,
                MergeCount = losers.Length,
                TrunkCount = trunks.Count,
            };

            var topology = new ValleyTopology(
                real.Seed, network, real.Events, downstream, points, real.Occupancy, trunks, stats);

            return ValleyTrajectoryNetwork.Build(topology, RidgeSettings, stage1[world]);
        }

        /// <summary>A point a given distance down a corridor's own candidate ray.</summary>
        private Vector2 OnRay(int world, int corridorId, float metres)
        {
            var ray = networks[world].CandidateRays[corridorId];
            return ray.Origin + ray.Direction * metres;
        }

        private int WorldWithAChainOf(int length)
        {
            for (var w = 0; w < networks.Length; w++)
            {
                for (var c = 0; c < networks[w].Chains.Count; c++)
                {
                    if (networks[w].Chains[c].Length >= length)
                    {
                        return w;
                    }
                }
            }

            Assert.Fail($"no seed has a chain of {length} corridors");
            return -1;
        }

        private static ValleyChain LongestChain(ValleyNetwork network)
        {
            var best = network.Chains[0];
            for (var c = 1; c < network.Chains.Count; c++)
            {
                if (network.Chains[c].Length > best.Length)
                {
                    best = network.Chains[c];
                }
            }

            return best;
        }

        private static int ConfluenceIndexOf(FinalValleyTrajectory survivor, int tributaryId)
        {
            for (var k = 0; k < survivor.TributaryIds.Length; k++)
            {
                if (survivor.TributaryIds[k] == tributaryId)
                {
                    return survivor.ConfluenceIndices[k];
                }
            }

            return -1;
        }

        /// <summary>Distance from a point to the nearest sample of a polyline.</summary>
        private static float Near(Vector2[] points, Vector2 p)
        {
            var best = float.MaxValue;
            for (var i = 0; i < points.Length; i++)
            {
                best = Mathf.Min(best, (points[i] - p).magnitude);
            }

            return best;
        }

        private static bool SegmentsCross(Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1)
        {
            var d = (a1.x - a0.x) * (b1.y - b0.y) - (a1.y - a0.y) * (b1.x - b0.x);
            if (Mathf.Abs(d) < 1e-9f)
            {
                return false;
            }

            var u = ((b0.x - a0.x) * (b1.y - b0.y) - (b0.y - a0.y) * (b1.x - b0.x)) / d;
            var v = ((b0.x - a0.x) * (a1.y - a0.y) - (b0.y - a0.y) * (a1.x - a0.x)) / d;
            return u > 1e-6f && u < 1f - 1e-6f && v > 1e-6f && v < 1f - 1e-6f;
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
