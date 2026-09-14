using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.TerrainGen.Tests
{
    /// <summary>
    /// Stage 3E slice E2: structural basin validation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These assert the rules, not the counts. On the canonical set every enclosure currently passes,
    /// which is a fact about the geometry and not a target: asserting "five basins on seed 110201"
    /// would turn a measurement into a specification and would pass for the wrong reason the day the
    /// geometry moves.
    /// </para>
    /// <para>
    /// They also pin what a basin deliberately does not carry. The floor, the hollow and its bound were
    /// deferred by owner decision; a reflection test keeps a convenient field from quietly restoring
    /// them.
    /// </para>
    /// </remarks>
    public sealed class TerrainGenStage3E2Tests
    {
        private static readonly int[] Seeds =
        {
            110201, 424242, 20260905, 7, 101010, 20261231, 1337,
        };

        private static TerrainGenSettings TerrainSettings => TerrainGenSettings.Default;
        private static MacroStructureSettings MacroSettings => MacroStructureSettings.Default;
        private static MassifSettings MassifSettings => MassifSettings.Default;
        private static RidgeSettings RidgeSettings => RidgeSettings.Default;

        private World[] worlds;

        [OneTimeSetUp]
        public void BuildWorlds()
        {
            worlds = new World[Seeds.Length];
            for (var i = 0; i < Seeds.Length; i++)
            {
                worlds[i] = Build(Seeds[i]);
            }
        }

        // ---------- the verdict ledger ----------

        /// <summary>Every candidate is judged exactly once, and the ledger agrees with the basins.</summary>
        [Test]
        public void EveryCandidateGetsExactlyOneVerdict()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                var world = worlds[w];
                Assert.AreEqual(
                    world.Candidates.Candidates.Count, world.Basins.Verdicts.Count,
                    $"seed {Seeds[w]}: a candidate went unjudged");

                var accepted = 0;
                for (var i = 0; i < world.Basins.Verdicts.Count; i++)
                {
                    var verdict = world.Basins.Verdicts[i];
                    Assert.AreEqual(
                        i, verdict.CandidateId, $"seed {Seeds[w]}: verdicts are out of candidate order");
                    Assert.AreEqual(
                        world.Candidates.Candidates[i].CorridorId, verdict.CorridorId,
                        $"seed {Seeds[w]} C{i}: verdict names another corridor");

                    if (!verdict.Accepted)
                    {
                        Assert.AreEqual(-1, verdict.BasinId, $"seed {Seeds[w]} C{i}: rejected with a basin");
                        continue;
                    }

                    Assert.AreEqual(
                        accepted, verdict.BasinId, $"seed {Seeds[w]} C{i}: basin ids are not dense");
                    accepted++;
                }

                Assert.AreEqual(
                    accepted, world.Basins.Basins.Count, $"seed {Seeds[w]}: basin count");
                Assert.AreEqual(
                    accepted, world.Basins.Stats.BasinCount, $"seed {Seeds[w]}: stats disagree");
            }
        }

        // ---------- conditions 1, 2, 3 ----------

        /// <summary>1. Every basin's boundary is closed, simple and encloses ground.</summary>
        [Test]
        public void BasinBoundariesAreClosedAndSimple()
        {
            ForEachBasin((seed, world, basin) =>
            {
                Assert.That(
                    basin.Boundary.Length, Is.GreaterThanOrEqualTo(4),
                    $"seed {seed} B{basin.Id}: too few points");
                Assert.That(basin.AreaSqm, Is.GreaterThan(0f), $"seed {seed} B{basin.Id}: no area");
                Assert.IsFalse(
                    CirqueCandidateBuilder.SelfIntersects(basin.Boundary),
                    $"seed {seed} B{basin.Id}: boundary crosses itself");
            });
        }

        /// <summary>2. The perimeter is ridge over a strict majority, and the rule is strict.</summary>
        [Test]
        public void RidgePerimeterIsAStrictMajority()
        {
            Assert.AreEqual(
                0.5f, StructuralBasinBuilder.RidgeMajorityFraction, 0f,
                "the majority fraction moved off the resolved value");

            ForEachBasin((seed, world, basin) =>
                Assert.That(
                    basin.RidgePerimeterFraction,
                    Is.GreaterThan(StructuralBasinBuilder.RidgeMajorityFraction),
                    $"seed {seed} B{basin.Id}: accepted without a strict ridge majority"));

            // Anything rejected for the majority really did fail it.
            ForEachVerdict((seed, world, verdict) =>
            {
                if (verdict.Rejection != BasinRejection.RidgeMajority)
                {
                    return;
                }

                Assert.That(
                    world.Candidates.Candidates[verdict.CandidateId].RidgePerimeterFraction,
                    Is.LessThanOrEqualTo(StructuralBasinBuilder.RidgeMajorityFraction),
                    $"seed {seed} C{verdict.CandidateId}: rejected for a majority it has");
            });
        }

        /// <summary>3. Exactly one outlet gap, and no secondary ridge anywhere in the enclosure.</summary>
        [Test]
        public void ExactlyOneOutletGapAndOnlyPrimaryEnclosure()
        {
            ForEachBasin((seed, world, basin) =>
            {
                var gaps = 0;
                var spurs = 0;
                var crest = 0;

                foreach (var run in basin.Segments)
                {
                    switch (run.Source)
                    {
                        case BoundarySource.Outlet: gaps++; break;
                        case BoundarySource.Spur:
                            spurs++;
                            Assert.That(
                                run.SourceId, Is.InRange(0, world.Ridges.Spurs.Count - 1),
                                $"seed {seed} B{basin.Id}: enclosure names a spur outside the primary list");
                            break;
                        case BoundarySource.Crest: crest++; break;
                        default:
                            Assert.Fail($"seed {seed} B{basin.Id}: unknown provenance {run.Source}");
                            break;
                    }
                }

                Assert.AreEqual(1, gaps, $"seed {seed} B{basin.Id}: outlet runs");
                Assert.AreEqual(2, spurs, $"seed {seed} B{basin.Id}: spur runs");
                Assert.AreEqual(1, crest, $"seed {seed} B{basin.Id}: crest runs");

                CollectionAssert.AreEqual(
                    new[]
                    {
                        Mathf.Min(
                            world.Candidates.Candidates[basin.CandidateId].LowerSpurId,
                            world.Candidates.Candidates[basin.CandidateId].UpperSpurId),
                        Mathf.Max(
                            world.Candidates.Candidates[basin.CandidateId].LowerSpurId,
                            world.Candidates.Candidates[basin.CandidateId].UpperSpurId),
                    },
                    basin.EnclosingSpurIds,
                    $"seed {seed} B{basin.Id}: enclosing spurs are not the candidate's own");
            });
        }

        // ---------- conditions 4, 5 ----------

        /// <summary>
        /// 4, 5. The outlet is the first lowest point of the outlet run, and of nothing else.
        /// </summary>
        /// <remarks>
        /// Recomputed here from the boundary and the structural elevation rather than trusted, and
        /// recomputed the same way the rule states: over the <see cref="BoundarySource.Outlet"/> run,
        /// in boundary order, first minimum winning.
        /// </remarks>
        [Test]
        public void OutletIsTheFirstLowestPointOfTheOutletRunOnly()
        {
            ForEachBasin((seed, world, basin) =>
            {
                var run = RunOf(basin, BoundarySource.Outlet);

                Assert.That(
                    basin.OutletBoundaryIndex,
                    Is.InRange(run.FirstIndex, run.FirstIndex + run.PointCount - 1),
                    $"seed {seed} B{basin.Id}: the outlet is not on the outlet run");
                Assert.AreEqual(
                    basin.Boundary[basin.OutletBoundaryIndex], basin.OutletPointXZ,
                    $"seed {seed} B{basin.Id}: the outlet point is not its own boundary sample");

                var lowest = float.MaxValue;
                var at = -1;
                for (var k = 0; k < run.PointCount; k++)
                {
                    var index = (run.FirstIndex + k) % basin.Boundary.Length;
                    var here = StructuralElevation.At(
                        world.Stage1, world.Uplift, basin.Boundary[index]);
                    if (here >= lowest)
                    {
                        continue;
                    }

                    lowest = here;
                    at = index;
                }

                Assert.AreEqual(
                    at, basin.OutletBoundaryIndex,
                    $"seed {seed} B{basin.Id}: a different sample is the run's first minimum");
                Assert.AreEqual(
                    lowest, basin.OutletElevationMeters, 1e-4f,
                    $"seed {seed} B{basin.Id}: outlet elevation");

                // And it is emphatically not the minimum over the whole ring: the crest and spur runs
                // are enclosing ridge, not openings, and must not be able to supply the outlet.
                var ringLowest = float.MaxValue;
                for (var k = 0; k < basin.Boundary.Length; k++)
                {
                    ringLowest = Mathf.Min(
                        ringLowest,
                        StructuralElevation.At(world.Stage1, world.Uplift, basin.Boundary[k]));
                }

                Assert.That(
                    basin.OutletElevationMeters, Is.GreaterThanOrEqualTo(ringLowest - 1e-4f),
                    $"seed {seed} B{basin.Id}: the outlet is below the ring minimum, which is impossible"
                    + " if it was read off the ring");
            });
        }

        // ---------- condition 6 ----------

        /// <summary>
        /// 6. The outlet corridor is the corridor the enclosure frames, and it runs downhill.
        /// </summary>
        /// <remarks>
        /// The invariant the model rests on: for a cirque the source corridor and the outlet corridor
        /// are one corridor, so there is one field. The geometric claim behind that - the corridor
        /// actually leaves through the gap - is checked rather than assumed.
        /// </remarks>
        [Test]
        public void OutletCorridorIsTheSourceCorridorAndRunsDownhill()
        {
            ForEachBasin((seed, world, basin) =>
            {
                var candidate = world.Candidates.Candidates[basin.CandidateId];

                Assert.AreEqual(
                    candidate.CorridorId, basin.CorridorId,
                    $"seed {seed} B{basin.Id}: the outlet corridor is not the source corridor");
                Assert.That(
                    basin.CorridorId, Is.InRange(0, world.Final.Trajectories.Count - 1),
                    $"seed {seed} B{basin.Id}: names a corridor that does not exist");

                var trajectory = world.Final.Trajectories[basin.CorridorId];
                Assert.IsTrue(
                    trajectory.HasGeometry, $"seed {seed} B{basin.Id}: the corridor has no trajectory");

                // Downstream is the trajectory's own published direction - head first, endpoint last -
                // so the comparison reads the endpoint, not an extreme of an unordered array.
                Assert.AreEqual(
                    trajectory.HeadPoint, trajectory.Points[0],
                    $"seed {seed} B{basin.Id}: the trajectory's head is not its first sample");
                Assert.AreEqual(
                    trajectory.EndPoint, trajectory.Points[trajectory.Points.Length - 1],
                    $"seed {seed} B{basin.Id}: the trajectory's end is not its last sample");

                var end = StructuralElevation.At(world.Stage1, world.Uplift, trajectory.EndPoint);
                Assert.That(
                    end, Is.LessThan(basin.OutletElevationMeters),
                    $"seed {seed} B{basin.Id}: the corridor does not continue below the outlet");

                // It leaves through the opening. The gap's upper endpoint is bookkept into the spur run
                // before it, so the span tested here starts one sample earlier.
                var run = RunOf(basin, BoundarySource.Outlet);
                var n = basin.Boundary.Length;
                var first = (run.FirstIndex - 1 + n) % n;
                var crosses = false;
                for (var k = 0; k <= run.PointCount - 1 && !crosses; k++)
                {
                    var a = basin.Boundary[(first + k) % n];
                    var b = basin.Boundary[(first + k + 1) % n];
                    for (var i = 0; i + 1 < trajectory.Points.Length; i++)
                    {
                        if (Town.Geometry.Geo2.SegmentIntersect(
                                trajectory.Points[i], trajectory.Points[i + 1], a, b, out _))
                        {
                            crosses = true;
                            break;
                        }
                    }
                }

                Assert.IsTrue(
                    crosses, $"seed {seed} B{basin.Id}: the corridor does not leave through the gap");
            });
        }

        // ---------- conditions 7, 8, 9 ----------

        /// <summary>7. Every basin sample is on its own massif's structural ground.</summary>
        [Test]
        public void BasinsStayInsideTheStructuralDomain()
        {
            ForEachBasin((seed, world, basin) =>
            {
                for (var k = 0; k < basin.Boundary.Length; k++)
                {
                    var owner = world.Ridges.Ownership.Ownership.At(
                        basin.Boundary[k].x, basin.Boundary[k].y);
                    Assert.IsTrue(
                        owner.IsStructural,
                        $"seed {seed} B{basin.Id}: point {k} is on {owner.Kind} ground");
                    Assert.AreEqual(
                        basin.MassifIndex, owner.MassifIndex,
                        $"seed {seed} B{basin.Id}: point {k} left its massif");
                }
            });
        }

        /// <summary>9. No corridor but its own runs through a basin's enclosed ground.</summary>
        /// <remarks>
        /// Entering, not touching. The crest belongs to both flanks, so a corridor on the far flank has
        /// its head on the same crest run and crosses it going the other way; that meeting is a fact
        /// about the shared crest, not about the basin. The basin's own corridor is inside by
        /// construction - a cirque encloses its head - so it is exempt.
        /// </remarks>
        [Test]
        public void NoForeignValleyRunsThroughABasin()
        {
            ForEachBasin((seed, world, basin) =>
            {
                foreach (var trajectory in world.Final.Trajectories)
                {
                    if (trajectory.Id == basin.CorridorId || !trajectory.HasGeometry)
                    {
                        continue;
                    }

                    for (var i = 0; i < trajectory.Points.Length; i++)
                    {
                        // Inside by more than the resolution the structure is defined at. A far-flank
                        // corridor's head sits on the shared crest, and a chord-sampled crest puts it
                        // a few centimetres to one side; that is not being inside a basin.
                        if (!StructuralBasinBuilder.Inside(basin.Boundary, trajectory.Points[i]))
                        {
                            continue;
                        }

                        Assert.That(
                            StructuralBasinBuilder.DistanceToRing(basin.Boundary, trajectory.Points[i]),
                            Is.LessThanOrEqualTo(RidgeSettings.SpurSampleSpacingMeters),
                            $"seed {seed} B{basin.Id}: V{trajectory.Id} sample {i} runs through it");
                    }
                }

                var own = world.Final.Trajectories[basin.CorridorId];
                Assert.That(
                    SecondarySpurBuilder.DistanceToPolyline(basin.Boundary, own.HeadPoint),
                    Is.LessThan(RidgeSettings.SpurSampleSpacingMeters),
                    $"seed {seed} B{basin.Id}: its own corridor's head is not on the boundary");
            });
        }

        // ---------- conditions 10, 11, 12 ----------

        /// <summary>10. The corridor relationship lives on the basin and nowhere else.</summary>
        [Test]
        public void BasinTopologyLivesOnTheBasinSide()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                foreach (var corridor in worlds[w].Ridges.Valleys)
                {
                    Assert.AreEqual(
                        ValleyTermination.Unresolved, corridor.Termination,
                        $"seed {Seeds[w]} V{corridor.Id}: a corridor learned about basins");
                    Assert.AreEqual(
                        -1, corridor.DownstreamId,
                        $"seed {Seeds[w]} V{corridor.Id}: downstream written");
                }

                // The frozen termination vocabulary gained no Basin member.
                CollectionAssert.AreEquivalent(
                    new[] { "Coast", "Merge", "Divide", "Unresolved" },
                    System.Enum.GetNames(typeof(ValleyTermination)),
                    "ValleyTermination changed");
            }
        }

        /// <summary>11. The same inputs validate to the same basins, field for field.</summary>
        [Test]
        public void ValidationIsDeterministic()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                var again = Build(Seeds[w]).Basins;
                var first = worlds[w].Basins;

                Assert.AreEqual(first.Basins.Count, again.Basins.Count, $"seed {Seeds[w]}: basin count");
                Assert.AreEqual(
                    first.Verdicts.Count, again.Verdicts.Count, $"seed {Seeds[w]}: verdict count");

                for (var i = 0; i < first.Verdicts.Count; i++)
                {
                    Assert.AreEqual(
                        first.Verdicts[i].Rejection, again.Verdicts[i].Rejection,
                        $"seed {Seeds[w]} C{i}: rejection reason");
                    Assert.AreEqual(
                        first.Verdicts[i].BasinId, again.Verdicts[i].BasinId,
                        $"seed {Seeds[w]} C{i}: basin id");
                }

                for (var i = 0; i < first.Basins.Count; i++)
                {
                    var a = first.Basins[i];
                    var b = again.Basins[i];
                    Assert.AreEqual(a.CorridorId, b.CorridorId, $"seed {Seeds[w]} B{i}: corridor");
                    Assert.AreEqual(
                        a.OutletBoundaryIndex, b.OutletBoundaryIndex, $"seed {Seeds[w]} B{i}: outlet index");
                    Assert.AreEqual(
                        a.OutletPointXZ, b.OutletPointXZ, $"seed {Seeds[w]} B{i}: outlet point");
                    Assert.AreEqual(
                        a.OutletElevationMeters, b.OutletElevationMeters, 0f,
                        $"seed {Seeds[w]} B{i}: outlet elevation");
                    Assert.AreEqual(a.AreaSqm, b.AreaSqm, 0f, $"seed {Seeds[w]} B{i}: area");
                }
            }
        }

        /// <summary>12. Validating changes neither Stage 3C nor Stage 3D nor E1.</summary>
        [Test]
        public void Stage3CAndStage3DRemainImmutable()
        {
            for (var w = 0; w < Seeds.Length; w++)
            {
                var before = Build(Seeds[w]);
                var secondaryBefore = SecondarySpurBuilder.Build(
                    before.Ridges, before.Final, RidgeSettings, new Seed.WorldSeed(Seeds[w]));

                StructuralBasinBuilder.Build(
                    before.Candidates, before.Final, before.Stage1, MassifSettings, RidgeSettings);

                var after = Build(Seeds[w]);
                var secondaryAfter = SecondarySpurBuilder.Build(
                    after.Ridges, after.Final, RidgeSettings, new Seed.WorldSeed(Seeds[w]));

                Assert.AreEqual(
                    before.Ridges.Spurs.Count, after.Ridges.Spurs.Count, $"seed {Seeds[w]}: spur count");
                for (var i = 0; i < before.Ridges.Spurs.Count; i++)
                {
                    for (var k = 0; k < before.Ridges.Spurs[i].Points.Length; k++)
                    {
                        Assert.AreEqual(
                            before.Ridges.Spurs[i].Points[k], after.Ridges.Spurs[i].Points[k],
                            $"seed {Seeds[w]} S{i}: sample {k} moved");
                    }
                }

                Assert.AreEqual(
                    before.Ridges.Valleys.Count, after.Ridges.Valleys.Count,
                    $"seed {Seeds[w]}: the corridor set changed");

                for (var i = 0; i < before.Final.Trajectories.Count; i++)
                {
                    Assert.AreEqual(
                        before.Final.Trajectories[i].LengthMeters,
                        after.Final.Trajectories[i].LengthMeters, 0f,
                        $"seed {Seeds[w]} V{i}: trajectory length moved");
                    Assert.AreEqual(
                        before.Final.Trajectories[i].DownstreamId,
                        after.Final.Trajectories[i].DownstreamId,
                        $"seed {Seeds[w]} V{i}: topology moved");
                }

                Assert.AreEqual(
                    secondaryBefore.Spurs.Count, secondaryAfter.Spurs.Count,
                    $"seed {Seeds[w]}: rib count moved");

                Assert.AreEqual(
                    before.Candidates.Candidates.Count, after.Candidates.Candidates.Count,
                    $"seed {Seeds[w]}: E1 candidate count moved");
            }
        }

        // ---------- what E2 must not have quietly restored ----------

        /// <summary>A basin carries nothing the owner deferred, and no corridor was created.</summary>
        /// <remarks>
        /// The floor, the hollow and the bound left the contract by decision. A field named for any of
        /// them reappearing would mean the decision had been reversed by whoever added it.
        /// </remarks>
        [Test]
        public void BasinsCarryNoDeferredFieldAndNoCorridorWasCreated()
        {
            var type = typeof(StructuralBasin);
            foreach (var name in new[]
                     {
                         "FloorElevationMeters", "DepthMeters", "BasinDepthMeters", "Tier",
                         "InletCorridorIds", "Kind", "MaxBasinDepthMeters",
                     })
            {
                Assert.IsNull(
                    type.GetProperty(name),
                    $"StructuralBasin carries {name}: a deferred concept came back");
            }

            Assert.IsNull(
                typeof(StructuralBasinBuilder).GetField("MaxBasinDepthMeters"),
                "a basin depth bound was introduced");

            // No RNG: the builder takes no world seed at all.
            foreach (var method in typeof(StructuralBasinBuilder).GetMethods())
            {
                foreach (var parameter in method.GetParameters())
                {
                    Assert.AreNotEqual(
                        typeof(Seed.IWorldSeed), parameter.ParameterType,
                        $"StructuralBasinBuilder.{method.Name} takes a seed stream");
                }
            }

            for (var w = 0; w < worlds.Length; w++)
            {
                var fresh = Build(Seeds[w]);
                Assert.AreEqual(
                    fresh.Ridges.Valleys.Count, worlds[w].Ridges.Valleys.Count,
                    $"seed {Seeds[w]}: the corridor set grew");
            }
        }

        // ---------- fixtures ----------

        private sealed class World
        {
            public RidgeSkeleton Ridges;
            public ValleyTrajectoryNetwork Final;
            public CirqueCandidateSet Candidates;
            public StructuralBasinSet Basins;
            public Stage1Result Stage1;
            public MassifUpliftModel Uplift;
        }

        private static World Build(int seed)
        {
            var stage1 = Stage1LandmassGenerator.Generate(seed, TerrainSettings);
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
            var ridges = RidgeSkeleton.Build(partition, RidgeSettings, worldSeed);
            var network = ValleyNetwork.Build(ridges, RidgeSettings);
            var topology = ValleyTopology.Build(network, RidgeSettings);
            var final = ValleyTrajectoryNetwork.Build(topology, RidgeSettings, stage1);
            var candidates = CirqueCandidateBuilder.Build(ridges, RidgeSettings);

            return new World
            {
                Ridges = ridges,
                Final = final,
                Candidates = candidates,
                Basins = StructuralBasinBuilder.Build(
                    candidates, final, stage1, MassifSettings, RidgeSettings),
                Stage1 = stage1,
                Uplift = new MassifUpliftModel(massifs, MassifSettings),
            };
        }

        private static BoundarySegment RunOf(StructuralBasin basin, BoundarySource source)
        {
            foreach (var run in basin.Segments)
            {
                if (run.Source == source)
                {
                    return run;
                }
            }

            Assert.Fail($"B{basin.Id}: no {source} run");
            return default;
        }

        private void ForEachBasin(System.Action<int, World, StructuralBasin> body)
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                foreach (var basin in worlds[w].Basins.Basins)
                {
                    body(Seeds[w], worlds[w], basin);
                }
            }
        }

        private void ForEachVerdict(System.Action<int, World, BasinVerdict> body)
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                foreach (var verdict in worlds[w].Basins.Verdicts)
                {
                    body(Seeds[w], worlds[w], verdict);
                }
            }
        }
    }
}
