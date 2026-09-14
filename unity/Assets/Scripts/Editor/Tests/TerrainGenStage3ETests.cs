using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.TerrainGen.Tests
{
    /// <summary>
    /// Stage 3E slice E1: cirque candidate enumeration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Candidates, not basins. These assert that an enclosure is closed, has area, crosses itself
    /// nowhere, stays inside the structural domain, and that every run of its boundary says which
    /// frozen structure it was read off. They assert nothing about whether it holds a hollow: that is
    /// validation, and validation is blocked on a threshold the design never states and on what the
    /// outlet's elevation is measured over.
    /// </para>
    /// <para>
    /// They also assert what E1 must not touch: no height field, no corridor, no Stage 3C or 3D
    /// output, and no dependence on any of them having been built at all.
    /// </para>
    /// </remarks>
    public sealed class TerrainGenStage3ETests
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
        private CirqueCandidateSet[] sets;

        [OneTimeSetUp]
        public void BuildWorlds()
        {
            skeletons = new RidgeSkeleton[Seeds.Length];
            sets = new CirqueCandidateSet[Seeds.Length];

            for (var i = 0; i < Seeds.Length; i++)
            {
                skeletons[i] = Skeleton(Seeds[i]);
                sets[i] = CirqueCandidateBuilder.Build(skeletons[i], RidgeSettings);
            }
        }

        // ---------- enumeration ----------

        /// <summary>1. The same skeleton enumerates the same candidates, sample for sample.</summary>
        [Test]
        public void CandidateEnumerationIsDeterministic()
        {
            for (var w = 0; w < sets.Length; w++)
            {
                var again = CirqueCandidateBuilder.Build(Skeleton(Seeds[w]), RidgeSettings);
                var first = sets[w];

                Assert.AreEqual(
                    first.Candidates.Count, again.Candidates.Count, $"seed {Seeds[w]}: candidate count");

                for (var i = 0; i < first.Candidates.Count; i++)
                {
                    var a = first.Candidates[i];
                    var b = again.Candidates[i];
                    Assert.AreEqual(a.Id, b.Id, $"seed {Seeds[w]} C{i}: id");
                    Assert.AreEqual(a.CorridorId, b.CorridorId, $"seed {Seeds[w]} C{i}: corridor");
                    Assert.AreEqual(a.LowerSpurId, b.LowerSpurId, $"seed {Seeds[w]} C{i}: lower spur");
                    Assert.AreEqual(a.UpperSpurId, b.UpperSpurId, $"seed {Seeds[w]} C{i}: upper spur");
                    Assert.AreEqual(a.AreaSqm, b.AreaSqm, 0f, $"seed {Seeds[w]} C{i}: area");
                    Assert.AreEqual(
                        a.ConvergenceRatio, b.ConvergenceRatio, 0f, $"seed {Seeds[w]} C{i}: convergence");
                    Assert.AreEqual(
                        a.Boundary.Length, b.Boundary.Length, $"seed {Seeds[w]} C{i}: boundary samples");

                    for (var k = 0; k < a.Boundary.Length; k++)
                    {
                        Assert.AreEqual(a.Boundary[k], b.Boundary[k], $"seed {Seeds[w]} C{i}: point {k}");
                    }
                }
            }
        }

        /// <summary>2. Ids are dense, ordered, and follow corridor order.</summary>
        [Test]
        public void CandidateIdsAreStableAndOrdered()
        {
            for (var w = 0; w < sets.Length; w++)
            {
                var candidates = sets[w].Candidates;
                for (var i = 0; i < candidates.Count; i++)
                {
                    Assert.AreEqual(i, candidates[i].Id, $"seed {Seeds[w]}: id is not its own index");

                    if (i > 0)
                    {
                        Assert.That(
                            candidates[i].CorridorId, Is.GreaterThan(candidates[i - 1].CorridorId),
                            $"seed {Seeds[w]}: candidates are not in corridor order");
                    }

                    Assert.AreSame(
                        candidates[i], sets[w].ForCorridor(candidates[i].CorridorId),
                        $"seed {Seeds[w]} C{i}: corridor lookup disagrees");
                }
            }
        }

        // ---------- the ring ----------

        /// <summary>3. Every boundary is a closed ring with no jump in it.</summary>
        [Test]
        public void BoundariesAreClosed()
        {
            var limit = RidgeSettings.SpurSampleSpacingMeters * 2f;

            ForEach((seed, candidate) =>
            {
                Assert.That(
                    candidate.Boundary.Length, Is.GreaterThanOrEqualTo(4),
                    $"seed {seed} C{candidate.Id}: too few points to be a ring");

                for (var i = 0; i < candidate.Boundary.Length; i++)
                {
                    var a = candidate.Boundary[i];
                    var b = candidate.Boundary[(i + 1) % candidate.Boundary.Length];
                    Assert.That(
                        (b - a).magnitude, Is.LessThan(limit),
                        $"seed {seed} C{candidate.Id}: gap at edge {i}");
                    Assert.IsFalse(
                        float.IsNaN(a.x) || float.IsNaN(a.y),
                        $"seed {seed} C{candidate.Id}: point {i} is not finite");
                }
            });
        }

        /// <summary>4. Every boundary encloses ground.</summary>
        [Test]
        public void BoundariesHavePositiveArea()
        {
            ForEach((seed, candidate) =>
            {
                Assert.That(
                    candidate.AreaSqm, Is.GreaterThan(0f),
                    $"seed {seed} C{candidate.Id}: encloses nothing");
                Assert.AreEqual(
                    Mathf.Abs(CirqueCandidateBuilder.SignedArea(candidate.Boundary)),
                    candidate.AreaSqm, Mathf.Max(1f, candidate.AreaSqm * 1e-4f),
                    $"seed {seed} C{candidate.Id}: recorded area disagrees with the ring");
            });
        }

        /// <summary>5. No boundary crosses itself.</summary>
        [Test]
        public void BoundariesDoNotSelfIntersect()
        {
            ForEach((seed, candidate) =>
                Assert.IsFalse(
                    CirqueCandidateBuilder.SelfIntersects(candidate.Boundary),
                    $"seed {seed} C{candidate.Id}: boundary crosses itself"));
        }

        // ---------- provenance ----------

        /// <summary>6. The four runs tile the ring exactly, and each one measures itself.</summary>
        [Test]
        public void EveryBoundarySegmentCarriesProvenance()
        {
            ForEach((seed, candidate) =>
            {
                Assert.AreEqual(
                    4, candidate.Segments.Length,
                    $"seed {seed} C{candidate.Id}: a cirque has four runs, not {candidate.Segments.Length}");

                var covered = 0;
                var expected = 0;
                var total = 0f;

                for (var k = 0; k < candidate.Segments.Length; k++)
                {
                    var run = candidate.Segments[k];
                    Assert.AreEqual(
                        expected, run.FirstIndex,
                        $"seed {seed} C{candidate.Id}: run {k} does not start where run {k - 1} ended");
                    Assert.That(
                        run.PointCount, Is.GreaterThan(0),
                        $"seed {seed} C{candidate.Id}: run {k} is empty");
                    Assert.That(
                        run.LengthMeters, Is.GreaterThan(0f),
                        $"seed {seed} C{candidate.Id}: run {k} has no length");

                    covered += run.PointCount;
                    expected += run.PointCount;
                    total += run.LengthMeters;

                    Assert.AreEqual(
                        run.PointCount, candidate.PointsOf(k).Length,
                        $"seed {seed} C{candidate.Id}: run {k} does not resolve to its own points");
                }

                Assert.AreEqual(
                    candidate.Boundary.Length, covered,
                    $"seed {seed} C{candidate.Id}: the runs do not tile the ring");
                Assert.AreEqual(
                    candidate.PerimeterMeters, total, Mathf.Max(0.5f, candidate.PerimeterMeters * 1e-3f),
                    $"seed {seed} C{candidate.Id}: the runs do not add up to the perimeter");
            });
        }

        /// <summary>7. Crest, two primary spurs, one outlet gap. Nothing else bounds a cirque.</summary>
        [Test]
        public void OnlyCrestSpurAndOutletProvenance()
        {
            ForEach((seed, candidate) =>
            {
                var crest = 0;
                var spur = 0;
                var outlet = 0;
                var ridge = 0f;

                foreach (var run in candidate.Segments)
                {
                    switch (run.Source)
                    {
                        case BoundarySource.Crest:
                            crest++;
                            Assert.AreEqual(
                                candidate.MassifIndex, run.SourceId,
                                $"seed {seed} C{candidate.Id}: crest run names another massif");
                            break;
                        case BoundarySource.Spur:
                            spur++;
                            Assert.That(
                                run.SourceId == candidate.LowerSpurId
                                || run.SourceId == candidate.UpperSpurId, Is.True,
                                $"seed {seed} C{candidate.Id}: spur run names S{run.SourceId}, which"
                                + " does not bound this corridor");
                            break;
                        case BoundarySource.Outlet:
                            outlet++;
                            Assert.AreEqual(
                                -1, run.SourceId,
                                $"seed {seed} C{candidate.Id}: the gap names a structure it has none of");
                            break;
                        default:
                            Assert.Fail($"seed {seed} C{candidate.Id}: unknown provenance {run.Source}");
                            break;
                    }

                    if (run.IsRidge)
                    {
                        ridge += run.LengthMeters;
                    }
                }

                Assert.AreEqual(1, crest, $"seed {seed} C{candidate.Id}: crest runs");
                Assert.AreEqual(2, spur, $"seed {seed} C{candidate.Id}: spur runs");
                Assert.AreEqual(1, outlet, $"seed {seed} C{candidate.Id}: outlet runs");

                Assert.AreEqual(
                    ridge / candidate.PerimeterMeters, candidate.RidgePerimeterFraction, 1e-3f,
                    $"seed {seed} C{candidate.Id}: the recorded ridge fraction is not what the runs say");
            });
        }

        /// <summary>8. No secondary ridge takes part in an enclosure, and none changes one.</summary>
        [Test]
        public void NoSecondaryRidgeInEnclosure()
        {
            for (var w = 0; w < sets.Length; w++)
            {
                foreach (var candidate in sets[w].Candidates)
                {
                    foreach (var run in candidate.Segments)
                    {
                        if (run.Source != BoundarySource.Spur)
                        {
                            continue;
                        }

                        // Spur ids address the primary list and nothing else. A secondary id lives in
                        // its own space, so it cannot name a run even by accident.
                        Assert.That(
                            run.SourceId, Is.InRange(0, skeletons[w].Spurs.Count - 1),
                            $"seed {Seeds[w]} C{candidate.Id}: run names a spur outside the primary list");
                        Assert.AreEqual(
                            run.SourceId, skeletons[w].Spurs[run.SourceId].Id,
                            $"seed {Seeds[w]} C{candidate.Id}: primary id is not its own index");
                    }
                }

                // Building the depth-2 layer changes no enclosure.
                var trajectories = BuildStage3C(Seeds[w]);
                SecondarySpurBuilder.Build(
                    skeletons[w], trajectories, RidgeSettings, new Seed.WorldSeed(Seeds[w]));
                var after = CirqueCandidateBuilder.Build(skeletons[w], RidgeSettings);

                Assert.AreEqual(
                    sets[w].Candidates.Count, after.Candidates.Count,
                    $"seed {Seeds[w]}: the secondary layer changed the candidate count");
                for (var i = 0; i < after.Candidates.Count; i++)
                {
                    Assert.AreEqual(
                        sets[w].Candidates[i].AreaSqm, after.Candidates[i].AreaSqm, 0f,
                        $"seed {Seeds[w]} C{i}: the secondary layer moved an enclosure");
                }
            }
        }

        /// <summary>9. A corridor with an open side is never a candidate.</summary>
        [Test]
        public void NoOuterCorridorCandidate()
        {
            var outer = 0;

            for (var w = 0; w < sets.Length; w++)
            {
                foreach (var corridor in skeletons[w].Valleys)
                {
                    if (!corridor.IsOuter)
                    {
                        continue;
                    }

                    outer++;
                    Assert.IsNull(
                        sets[w].ForCorridor(corridor.Id),
                        $"seed {Seeds[w]}: outer corridor V{corridor.Id} became a candidate");
                }

                foreach (var candidate in sets[w].Candidates)
                {
                    Assert.IsFalse(
                        skeletons[w].Valleys[candidate.CorridorId].IsOuter,
                        $"seed {Seeds[w]} C{candidate.Id}: framed by an outer corridor");
                    Assert.That(
                        candidate.LowerSpurId, Is.GreaterThanOrEqualTo(0),
                        $"seed {Seeds[w]} C{candidate.Id}: no lower spur");
                    Assert.That(
                        candidate.UpperSpurId, Is.GreaterThanOrEqualTo(0),
                        $"seed {Seeds[w]} C{candidate.Id}: no upper spur");
                }
            }

            Assert.That(outer, Is.GreaterThan(0), "the seed set no longer contains an outer corridor");
        }

        /// <summary>10. Every boundary sample is on its own massif's structural ground.</summary>
        [Test]
        public void CandidatesRemainInsideStructuralDomain()
        {
            for (var w = 0; w < sets.Length; w++)
            {
                foreach (var candidate in sets[w].Candidates)
                {
                    for (var k = 0; k < candidate.Boundary.Length; k++)
                    {
                        var owner = skeletons[w].Ownership.Ownership.At(
                            candidate.Boundary[k].x, candidate.Boundary[k].y);
                        Assert.IsTrue(
                            owner.IsStructural,
                            $"seed {Seeds[w]} C{candidate.Id}: point {k} is on {owner.Kind} ground");
                        Assert.AreEqual(
                            candidate.MassifIndex, owner.MassifIndex,
                            $"seed {Seeds[w]} C{candidate.Id}: point {k} left its massif");
                    }
                }
            }
        }

        /// <summary>
        /// 8b. <c>ConvergenceRatio</c> is the outlet gap over the crest span, and nothing else.
        /// </summary>
        /// <remarks>
        /// One definition, pinned so it cannot drift into its own inverse: the length of the
        /// <see cref="BoundarySource.Outlet"/> run divided by the length of the
        /// <see cref="BoundarySource.Crest"/> run. Under one the spurs close on each other, over one
        /// they splay. Nothing here compares it against a threshold, because the design states none.
        /// </remarks>
        [Test]
        public void ConvergenceRatioIsTheOutletGapOverTheCrestSpan()
        {
            ForEach((seed, candidate) =>
            {
                var crest = candidate.Segments[0];
                var gap = candidate.Segments[2];

                Assert.AreEqual(
                    BoundarySource.Crest, crest.Source,
                    $"seed {seed} C{candidate.Id}: run 0 is not the crest");
                Assert.AreEqual(
                    BoundarySource.Outlet, gap.Source,
                    $"seed {seed} C{candidate.Id}: run 2 is not the outlet gap");

                Assert.AreEqual(
                    candidate.CrestSpanMeters, crest.LengthMeters, Mathf.Max(0.1f, crest.LengthMeters * 1e-3f),
                    $"seed {seed} C{candidate.Id}: the crest span is not the crest run's length");
                Assert.AreEqual(
                    candidate.OutletGapMeters, gap.LengthMeters, Mathf.Max(0.1f, gap.LengthMeters * 1e-3f),
                    $"seed {seed} C{candidate.Id}: the outlet gap is not the gap run's length");

                Assert.AreEqual(
                    candidate.OutletGapMeters / candidate.CrestSpanMeters,
                    candidate.ConvergenceRatio, 1e-4f,
                    $"seed {seed} C{candidate.Id}: convergence is not gap over crest span");
                Assert.That(
                    candidate.ConvergenceRatio, Is.GreaterThan(0f),
                    $"seed {seed} C{candidate.Id}: a degenerate convergence ratio");
            });
        }

        // ---------- what E1 must not touch ----------

        /// <summary>11, 12. Enumerating changes neither Stage 3C nor Stage 3D.</summary>
        [Test]
        public void EnumerationMutatesNeitherStage3CNorStage3D()
        {
            for (var w = 0; w < Seeds.Length; w++)
            {
                var a = Skeleton(Seeds[w]);
                var trajectoriesA = BuildStage3C(Seeds[w]);
                var secondaryA = SecondarySpurBuilder.Build(
                    a, trajectoriesA, RidgeSettings, new Seed.WorldSeed(Seeds[w]));

                CirqueCandidateBuilder.Build(a, RidgeSettings);

                var b = Skeleton(Seeds[w]);
                var trajectoriesB = BuildStage3C(Seeds[w]);
                var secondaryB = SecondarySpurBuilder.Build(
                    b, trajectoriesB, RidgeSettings, new Seed.WorldSeed(Seeds[w]));

                Assert.AreEqual(a.Spurs.Count, b.Spurs.Count, $"seed {Seeds[w]}: spur count");
                for (var i = 0; i < a.Spurs.Count; i++)
                {
                    Assert.AreEqual(
                        a.Spurs[i].AttachS, b.Spurs[i].AttachS, 0f, $"seed {Seeds[w]} S{i}: attachment");
                    for (var k = 0; k < a.Spurs[i].Points.Length; k++)
                    {
                        Assert.AreEqual(
                            a.Spurs[i].Points[k], b.Spurs[i].Points[k],
                            $"seed {Seeds[w]} S{i}: sample {k} moved");
                    }
                }

                for (var i = 0; i < trajectoriesA.Trajectories.Count; i++)
                {
                    Assert.AreEqual(
                        trajectoriesA.Trajectories[i].LengthMeters,
                        trajectoriesB.Trajectories[i].LengthMeters, 0f,
                        $"seed {Seeds[w]} V{i}: trajectory length moved");
                    Assert.AreEqual(
                        trajectoriesA.Trajectories[i].DownstreamId,
                        trajectoriesB.Trajectories[i].DownstreamId,
                        $"seed {Seeds[w]} V{i}: topology moved");
                }

                Assert.AreEqual(
                    secondaryA.Spurs.Count, secondaryB.Spurs.Count, $"seed {Seeds[w]}: rib count");
                for (var i = 0; i < secondaryA.Spurs.Count; i++)
                {
                    Assert.AreEqual(
                        secondaryA.Spurs[i].LateralDriftMeters,
                        secondaryB.Spurs[i].LateralDriftMeters, 0f,
                        $"seed {Seeds[w]} rib {i}: drift moved");
                }
            }
        }

        /// <summary>13. No corridor is created, and the corridor set is untouched.</summary>
        [Test]
        public void NoNewValleyCorridor()
        {
            for (var w = 0; w < sets.Length; w++)
            {
                var fresh = Skeleton(Seeds[w]);
                Assert.AreEqual(
                    fresh.Valleys.Count, skeletons[w].Valleys.Count,
                    $"seed {Seeds[w]}: the corridor set grew");

                foreach (var candidate in sets[w].Candidates)
                {
                    Assert.That(
                        candidate.CorridorId, Is.InRange(0, skeletons[w].Valleys.Count - 1),
                        $"seed {Seeds[w]} C{candidate.Id}: names a corridor that does not exist");
                }

                Assert.IsFalse(
                    typeof(ValleyCorridor).IsAssignableFrom(typeof(CirqueCandidate)),
                    "a candidate became a corridor");
            }
        }

        /// <summary>
        /// 14. Enumeration needs no field, no trajectory and no hydrology - not even built.
        /// </summary>
        /// <remarks>
        /// The builder's signature takes a skeleton and settings and nothing else, so there is no
        /// height field it could read. This asserts the stronger runtime fact: candidates come out
        /// identical whether or not Stage 1's field, Stage 3C and Stage 3D have been built at all.
        /// </remarks>
        [Test]
        public void EnumerationDependsOnNoFieldOrTrajectory()
        {
            for (var w = 0; w < Seeds.Length; w++)
            {
                // Bare: a skeleton and nothing else in the process.
                var bare = CirqueCandidateBuilder.Build(Skeleton(Seeds[w]), RidgeSettings);

                // Loaded: the whole Stage 3C and 3D pipeline built first.
                var loaded = Skeleton(Seeds[w]);
                var trajectories = BuildStage3C(Seeds[w]);
                SecondarySpurBuilder.Build(
                    loaded, trajectories, RidgeSettings, new Seed.WorldSeed(Seeds[w]));
                var after = CirqueCandidateBuilder.Build(loaded, RidgeSettings);

                Assert.AreEqual(
                    bare.Candidates.Count, after.Candidates.Count,
                    $"seed {Seeds[w]}: candidate count depends on what else was built");

                for (var i = 0; i < bare.Candidates.Count; i++)
                {
                    Assert.AreEqual(
                        bare.Candidates[i].Boundary.Length, after.Candidates[i].Boundary.Length,
                        $"seed {Seeds[w]} C{i}: boundary size");
                    for (var k = 0; k < bare.Candidates[i].Boundary.Length; k++)
                    {
                        Assert.AreEqual(
                            bare.Candidates[i].Boundary[k], after.Candidates[i].Boundary[k],
                            $"seed {Seeds[w]} C{i}: point {k}");
                    }
                }
            }
        }

        /// <summary>15. A candidate carries no verdict, because E1 reaches none.</summary>
        /// <remarks>
        /// Guards the distinction the slice exists to keep. If a floor or an outlet elevation ever
        /// appears on this type, it will have been decided by whoever added the field rather than by
        /// the design, which states neither the perimeter threshold nor what the outlet's elevation is
        /// measured over.
        /// </remarks>
        [Test]
        public void CandidatesCarryNoBasinVerdict()
        {
            var type = typeof(CirqueCandidate);
            foreach (var name in new[]
                     {
                         "FloorElevationMeters", "OutletElevationMeters", "OutletCorridorId",
                         "InletCorridorIds", "IsValid", "IsBasin",
                     })
            {
                Assert.IsNull(
                    type.GetProperty(name),
                    $"CirqueCandidate carries {name}: E1 has started deciding what E2 is blocked on");
            }
        }

        // ---------- fixtures ----------

        private void ForEach(System.Action<int, CirqueCandidate> body)
        {
            for (var w = 0; w < sets.Length; w++)
            {
                foreach (var candidate in sets[w].Candidates)
                {
                    body(Seeds[w], candidate);
                }
            }
        }

        private static ValleyTrajectoryNetwork BuildStage3C(int seed)
        {
            var stage1 = Stage1LandmassGenerator.Generate(seed, TerrainSettings);
            var ridges = Skeleton(seed);
            var network = ValleyNetwork.Build(ridges, RidgeSettings);
            var topology = ValleyTopology.Build(network, RidgeSettings);
            return ValleyTrajectoryNetwork.Build(topology, RidgeSettings, stage1);
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
