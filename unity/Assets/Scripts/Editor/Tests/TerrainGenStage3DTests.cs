using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.TerrainGen.Tests
{
    /// <summary>
    /// Stage 3D: the depth-2 ridge curves and their strips.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two halves. The first asserts that a rib is what the design says it is: inside its parent's
    /// strip, attached inside the parent's own depth, one tier below it, crossing nothing, and never
    /// in the terminal apron. The second asserts that building any of it leaves Stage 3C exactly where
    /// it was - the same spurs, the same corridors, the same topology, the same trajectories and the
    /// same eight baseline apron crossings.
    /// </para>
    /// <para>
    /// The baseline is not something Stage 3D is asked to improve. Those eight crossings belong to
    /// Stage 3C and are asserted here only to prove they did not move.
    /// </para>
    /// </remarks>
    public sealed class TerrainGenStage3DTests
    {
        private static readonly int[] Seeds =
        {
            110201, 424242, 20260905, 7, 101010, 20261231, 1337,
        };

        /// <summary>The Stage 3C apron crossings, per seed, as recorded at the freeze.</summary>
        private static readonly Dictionary<int, int> BaselineCrossings = new Dictionary<int, int>
        {
            { 110201, 0 }, { 424242, 0 }, { 20260905, 3 }, { 7, 1 }, { 101010, 4 }, { 20261231, 0 },
            { 1337, 0 },
        };

        private static TerrainGenSettings TerrainSettings => TerrainGenSettings.Default;
        private static MacroStructureSettings MacroSettings => MacroStructureSettings.Default;
        private static MassifSettings MassifSettings => MassifSettings.Default;
        private static RidgeSettings RidgeSettings => RidgeSettings.Default;

        private RidgeSkeleton[] primaries;
        private ValleyTrajectoryNetwork[] finals;
        private SecondaryRidgeSkeleton[] secondaries;

        [OneTimeSetUp]
        public void BuildWorlds()
        {
            primaries = new RidgeSkeleton[Seeds.Length];
            finals = new ValleyTrajectoryNetwork[Seeds.Length];
            secondaries = new SecondaryRidgeSkeleton[Seeds.Length];

            for (var i = 0; i < Seeds.Length; i++)
            {
                var built = BuildStage3C(Seeds[i]);
                primaries[i] = built.Ridges;
                finals[i] = built.Final;
                secondaries[i] = SecondarySpurBuilder.Build(
                    built.Ridges, built.Final, RidgeSettings, new Seed.WorldSeed(Seeds[i]));
            }
        }

        // ---------- 1..5: a rib is what the design says it is ----------

        /// <summary>1. Depth 2, hard. No rib is the parent of anything.</summary>
        [Test]
        public void DepthIsCappedAtTwo()
        {
            for (var w = 0; w < secondaries.Length; w++)
            {
                var secondary = secondaries[w];
                foreach (var rib in secondary.Spurs)
                {
                    Assert.That(
                        rib.ParentSpurId, Is.InRange(0, primaries[w].Spurs.Count - 1),
                        $"seed {Seeds[w]}: S{rib.ParentSpurId}.{rib.Id} has no primary parent");
                    Assert.AreEqual(
                        rib.ParentSpurId, primaries[w].Spurs[rib.ParentSpurId].Id,
                        $"seed {Seeds[w]}: parent id is not its own index");

                    Assert.AreEqual(
                        primaries[w].Spurs[rib.ParentSpurId].MassifIndex, rib.MassifIndex,
                        $"seed {Seeds[w]}: S{rib.ParentSpurId}.{rib.Id} left its parent's massif");
                    Assert.AreEqual(
                        primaries[w].Spurs[rib.ParentSpurId].FlankSign, rib.FlankSign,
                        $"seed {Seeds[w]}: S{rib.ParentSpurId}.{rib.Id} left its parent's flank");
                }

                // A rib is never anyone's parent. The child lookup is indexed by primary id alone, so
                // an id from the secondary space addresses nothing and a third level has nowhere to
                // attach. The two identity spaces are separate, which is what makes the cap structural
                // rather than a rule someone has to remember.
                for (var i = 0; i < secondary.Spurs.Count; i++)
                {
                    var beyond = primaries[w].Spurs.Count + i;
                    Assert.AreEqual(
                        0, secondary.ChildrenOf(beyond).Count,
                        $"seed {Seeds[w]}: something answers to a parent id past the primary list");
                }

                Assert.AreEqual(
                    secondary.Spurs.Count, CountChildren(secondary, primaries[w]),
                    $"seed {Seeds[w]}: the child lookup and the rib list disagree");
            }
        }

        /// <summary>2. Every rib sample lies inside its parent's strip.</summary>
        [Test]
        public void SecondarySpursRemainInsideParentStrip()
        {
            for (var w = 0; w < secondaries.Length; w++)
            {
                foreach (var rib in secondaries[w].Spurs)
                {
                    var parent = primaries[w].Spurs[rib.ParentSpurId];
                    var massif = primaries[w].Ownership.Massifs[parent.MassifIndex];
                    var tolerance = RidgeSettings.SpurSampleSpacingMeters
                                    / Mathf.Max(1f, massif.LengthMeters);

                    for (var k = 0; k < rib.Points.Length; k++)
                    {
                        massif.Project(rib.Points[k].x, rib.Points[k].y, out var s, out _);
                        Assert.That(
                            s,
                            Is.GreaterThanOrEqualTo(parent.StripInterval.x - tolerance)
                                .And.LessThanOrEqualTo(parent.StripInterval.y + tolerance),
                            $"seed {Seeds[w]}: S{rib.ParentSpurId}.{rib.Id} sample {k} left the strip");
                    }
                }
            }
        }

        /// <summary>3. No rib reaches the terminal apron.</summary>
        [Test]
        public void SecondarySpursDoNotEnterApron()
        {
            for (var w = 0; w < secondaries.Length; w++)
            {
                foreach (var rib in secondaries[w].Spurs)
                {
                    for (var k = 0; k < rib.Points.Length; k++)
                    {
                        var owner = primaries[w].Ownership.Ownership.At(
                            rib.Points[k].x, rib.Points[k].y);
                        Assert.IsTrue(
                            owner.IsStructural,
                            $"seed {Seeds[w]}: S{rib.ParentSpurId}.{rib.Id} sample {k} is on"
                            + $" {owner.Kind} ground");
                        Assert.AreEqual(
                            rib.MassifIndex, owner.MassifIndex,
                            $"seed {Seeds[w]}: S{rib.ParentSpurId}.{rib.Id} sample {k} left its massif");
                    }
                }
            }
        }

        /// <summary>4. The attachment is strictly inside the parent, and on it.</summary>
        [Test]
        public void SecondaryAttachmentIsInsideParentDepth()
        {
            for (var w = 0; w < secondaries.Length; w++)
            {
                foreach (var rib in secondaries[w].Spurs)
                {
                    var parent = primaries[w].Spurs[rib.ParentSpurId];
                    var massif = primaries[w].Ownership.Massifs[parent.MassifIndex];

                    Assert.That(
                        rib.AttachT, Is.GreaterThan(0f).And.LessThan(1f),
                        $"seed {Seeds[w]}: S{rib.ParentSpurId}.{rib.Id} attaches outside (0,1)");
                    Assert.AreEqual(
                        rib.AttachPoint, rib.Points[0],
                        $"seed {Seeds[w]}: S{rib.ParentSpurId}.{rib.Id} does not start at its attachment");

                    // On the parent's own polyline, to the resolution the skeleton is sampled at.
                    Assert.That(
                        SecondarySpurBuilder.DistanceToPolyline(parent.Points, rib.AttachPoint),
                        Is.LessThan(RidgeSettings.SpurSampleSpacingMeters),
                        $"seed {Seeds[w]}: S{rib.ParentSpurId}.{rib.Id} does not attach to its parent");

                    var rebuilt = SecondarySpurBuilder.ParentPointAt(
                        massif, parent, RidgeSettings, rib.AttachT);
                    Assert.That(
                        (rebuilt - rib.AttachPoint).magnitude, Is.LessThan(1e-3f),
                        $"seed {Seeds[w]}: S{rib.ParentSpurId}.{rib.Id} attachment is not the parent"
                        + " curve at AttachT");
                }
            }
        }

        /// <summary>5. The child cells partition each side of each parent exactly.</summary>
        [Test]
        public void SecondaryPartitionIsExact()
        {
            var checkedSides = 0;

            for (var w = 0; w < secondaries.Length; w++)
            {
                var secondary = secondaries[w];
                for (var p = 0; p < primaries[w].Spurs.Count; p++)
                {
                    var parent = primaries[w].Spurs[p];
                    var realised = Realised(parent);

                    for (var k = 0; k < 2; k++)
                    {
                        var side = k == 0 ? -1 : 1;
                        var cells = secondary.StripsOf(parent.Id, side);
                        var ribs = RibsOn(secondary, parent.Id, side);

                        if (cells.Count == 0)
                        {
                            Assert.AreEqual(
                                0, ribs.Count,
                                $"seed {Seeds[w]}: S{parent.Id} side {side} has ribs but no cells");
                            continue;
                        }

                        checkedSides++;
                        Assert.AreEqual(
                            ribs.Count + 1, cells.Count,
                            $"seed {Seeds[w]}: S{parent.Id} side {side}: {ribs.Count} ribs should"
                            + " leave one more cell");

                        Assert.AreEqual(
                            0f, cells[0].ParentInterval.x, 1e-6f,
                            $"seed {Seeds[w]}: S{parent.Id} side {side} does not start at the head");
                        Assert.AreEqual(
                            realised, cells[cells.Count - 1].ParentInterval.y, 1e-5f,
                            $"seed {Seeds[w]}: S{parent.Id} side {side} does not end at the toe");

                        for (var i = 0; i < cells.Count; i++)
                        {
                            Assert.That(
                                cells[i].ParentInterval.x,
                                Is.GreaterThanOrEqualTo(0f).And.LessThanOrEqualTo(1f),
                                $"seed {Seeds[w]}: S{parent.Id} cell {i} starts outside (0,1)");
                            Assert.That(
                                cells[i].ParentInterval.y,
                                Is.GreaterThanOrEqualTo(0f).And.LessThanOrEqualTo(1f),
                                $"seed {Seeds[w]}: S{parent.Id} cell {i} ends outside (0,1)");
                            Assert.That(
                                cells[i].Span, Is.GreaterThan(0f),
                                $"seed {Seeds[w]}: S{parent.Id} cell {i} is empty");

                            if (i > 0)
                            {
                                Assert.AreEqual(
                                    cells[i - 1].ParentInterval.y, cells[i].ParentInterval.x, 1e-6f,
                                    $"seed {Seeds[w]}: S{parent.Id} cells {i - 1} and {i} do not meet");
                                Assert.AreEqual(
                                    ribs[i - 1].AttachT, cells[i].ParentInterval.x, 1e-6f,
                                    $"seed {Seeds[w]}: S{parent.Id} cell {i} does not start at its rib");
                            }
                        }
                    }
                }
            }

            Assert.That(checkedSides, Is.GreaterThan(0), "no parent side carries a partition");
        }

        // ---------- 6..10: crossings and tier ----------

        /// <summary>6. A rib meets a primary ridge only where it leaves its own parent.</summary>
        [Test]
        public void SecondarySpursDoNotCrossPrimarySpursExceptAtDeclaredAttachment()
        {
            for (var w = 0; w < secondaries.Length; w++)
            {
                foreach (var rib in secondaries[w].Spurs)
                {
                    foreach (var spur in primaries[w].Spurs)
                    {
                        // The segment carrying the attachment is the declared meeting with its own
                        // parent. Nothing else may touch any primary ridge.
                        var skip = spur.Id == rib.ParentSpurId ? 1 : 0;
                        Assert.IsFalse(
                            Crosses(rib.Points, spur.Points, skip),
                            $"seed {Seeds[w]}: S{rib.ParentSpurId}.{rib.Id} crosses primary S{spur.Id}");
                    }
                }
            }
        }

        /// <summary>7. Ribs do not cross one another.</summary>
        [Test]
        public void SecondarySpursDoNotCrossEachOther()
        {
            for (var w = 0; w < secondaries.Length; w++)
            {
                var ribs = secondaries[w].Spurs;
                for (var a = 0; a < ribs.Count; a++)
                {
                    for (var b = a + 1; b < ribs.Count; b++)
                    {
                        Assert.IsFalse(
                            Crosses(ribs[a].Points, ribs[b].Points, 0),
                            $"seed {Seeds[w]}: S{ribs[a].ParentSpurId}.{ribs[a].Id} crosses"
                            + $" S{ribs[b].ParentSpurId}.{ribs[b].Id}");
                    }
                }
            }
        }

        /// <summary>8. A rib dies beside the valley, never in it.</summary>
        [Test]
        public void SecondarySpursDoNotCrossPrimaryValleys()
        {
            for (var w = 0; w < secondaries.Length; w++)
            {
                foreach (var rib in secondaries[w].Spurs)
                {
                    foreach (var trajectory in finals[w].Trajectories)
                    {
                        if (!trajectory.HasGeometry)
                        {
                            continue;
                        }

                        Assert.IsFalse(
                            Crosses(rib.Points, trajectory.Points, 0),
                            $"seed {Seeds[w]}: S{rib.ParentSpurId}.{rib.Id} crosses valley"
                            + $" V{trajectory.Id}");

                        for (var k = 0; k < rib.Points.Length; k++)
                        {
                            Assert.That(
                                SecondarySpurBuilder.DistanceToPolyline(
                                    trajectory.Points, rib.Points[k]),
                                Is.GreaterThanOrEqualTo(RidgeSettings.SpurSampleSpacingMeters),
                                $"seed {Seeds[w]}: S{rib.ParentSpurId}.{rib.Id} sample {k} is inside"
                                + $" V{trajectory.Id}");
                        }
                    }
                }
            }
        }

        /// <summary>9. No rib crosses itself.</summary>
        [Test]
        public void SecondarySpursDoNotSelfIntersect()
        {
            for (var w = 0; w < secondaries.Length; w++)
            {
                foreach (var rib in secondaries[w].Spurs)
                {
                    Assert.IsFalse(
                        rib.SelfIntersects(),
                        $"seed {Seeds[w]}: S{rib.ParentSpurId}.{rib.Id} crosses itself");
                }
            }
        }

        /// <summary>10. Each rib sits a full tier below its parent.</summary>
        [Test]
        public void TierSeparationIsAtLeast2_5x()
        {
            var checkedRibs = 0;

            for (var w = 0; w < secondaries.Length; w++)
            {
                foreach (var rib in secondaries[w].Spurs)
                {
                    Assert.That(
                        rib.ReliefBudgetMeters, Is.GreaterThan(0f),
                        $"seed {Seeds[w]}: S{rib.ParentSpurId}.{rib.Id} has no structural budget");

                    checkedRibs++;
                    Assert.That(
                        rib.ParentReliefBudgetMeters / rib.ReliefBudgetMeters,
                        Is.GreaterThanOrEqualTo(SecondarySpurBuilder.TierRatio - 1e-4f),
                        $"seed {Seeds[w]}: S{rib.ParentSpurId}.{rib.Id} is not a tier below its parent");

                    // The parent's own budget is one tier below the crest it hangs off, so the rib is
                    // two tiers below the massif. Depth 2 spends both levels, as the design requires.
                    var parent = primaries[w].Spurs[rib.ParentSpurId];
                    var massif = primaries[w].Ownership.Massifs[parent.MassifIndex];
                    Assert.AreEqual(
                        massif.CrestAt(parent.AttachS) / SecondarySpurBuilder.TierRatio,
                        rib.ParentReliefBudgetMeters, 1e-3f,
                        $"seed {Seeds[w]}: S{rib.ParentSpurId}.{rib.Id} parent budget is not the"
                        + " crest a tier down");
                }
            }

            Assert.That(checkedRibs, Is.GreaterThan(0), "no rib was built anywhere");
        }

        /// <summary>16. The layer is not a comb of straight sticks, and no rib is a winding line.</summary>
        /// <remarks>
        /// Two halves of one contract. Something must actually bend - a layer of perfectly straight
        /// ribs is the artificiality the correction exists to remove - and nothing may bend further
        /// than the declared bound, which is Stage 3B's own drift coefficient one tier down. No exact
        /// curvature is asserted: the numbers here are the bound and zero, both of which are stated by
        /// the construction rather than measured off it.
        /// </remarks>
        [Test]
        public void SecondaryLayerIsNotUniversallyStraightAndItsBendIsBounded()
        {
            var bent = 0;
            var total = 0;
            var worst = 0f;

            for (var w = 0; w < secondaries.Length; w++)
            {
                foreach (var rib in secondaries[w].Spurs)
                {
                    total++;

                    // The drawn drift never exceeds the declared fraction of the rib's own length.
                    var bound = SecondarySpurBuilder.BendPerLength(RidgeSettings)
                                * rib.TargetReachMeters;
                    Assert.That(
                        Mathf.Abs(rib.LateralDriftMeters), Is.LessThanOrEqualTo(bound + 1e-3f),
                        $"seed {Seeds[w]}: S{rib.ParentSpurId}.{rib.Id} drifts {rib.LateralDriftMeters}m,"
                        + $" past the {bound}m the bound allows");

                    // A quadratic offset departs from its chord by a quarter of its end offset, so the
                    // bow can never exceed that however the draw fell.
                    Assert.That(
                        rib.ChordDeviationMeters, Is.LessThanOrEqualTo(bound * 0.25f + 1e-2f),
                        $"seed {Seeds[w]}: S{rib.ParentSpurId}.{rib.Id} bows"
                        + $" {rib.ChordDeviationMeters}m, past a quarter of its bound");

                    worst = Mathf.Max(worst, rib.ChordDeviationMeters / Mathf.Max(1f, rib.LengthMeters));

                    // "Meaningful" is read at the resolution the skeleton is drawn at: a tenth of a
                    // sample is a bend a reader can see, and less than that is a straight line.
                    if (rib.ChordDeviationMeters > RidgeSettings.SpurSampleSpacingMeters * 0.1f)
                    {
                        bent++;
                    }
                }
            }

            Assert.That(total, Is.GreaterThan(0), "no rib was built anywhere");
            Assert.That(
                bent, Is.GreaterThan(0),
                "not one rib in the canonical set bends: the layer is a comb of straight sticks");
            Assert.That(
                worst, Is.LessThan(0.25f),
                $"a rib departs from its chord by {worst:P0} of its length - that is a winding line,"
                + " not a subordinate ridge");
        }

        /// <summary>17. A rib keeps one sample clear of every ridge that is not its parent.</summary>
        /// <remarks>
        /// Not crossing is not enough. The skeleton is defined at
        /// <see cref="RidgeSettings.SpurSampleSpacingMeters"/>, and Stage 3C's own merge rule says two
        /// structural lines closer than one sample are one line; a rib running a few metres off a
        /// neighbouring spur would be that spur.
        /// </remarks>
        [Test]
        public void SecondarySpursMaintainStructuralClearanceFromUnrelatedPrimarySpurs()
        {
            for (var w = 0; w < secondaries.Length; w++)
            {
                foreach (var rib in secondaries[w].Spurs)
                {
                    foreach (var spur in primaries[w].Spurs)
                    {
                        if (spur.Id == rib.ParentSpurId)
                        {
                            continue;
                        }

                        for (var k = 0; k < rib.Points.Length; k++)
                        {
                            Assert.That(
                                SecondarySpurBuilder.DistanceToPolyline(spur.Points, rib.Points[k]),
                                Is.GreaterThanOrEqualTo(RidgeSettings.SpurSampleSpacingMeters),
                                $"seed {Seeds[w]}: S{rib.ParentSpurId}.{rib.Id} sample {k} runs inside"
                                + $" one sample of primary S{spur.Id}");
                        }
                    }

                    Assert.That(
                        rib.ClearanceMeters,
                        Is.GreaterThanOrEqualTo(RidgeSettings.SpurSampleSpacingMeters),
                        $"seed {Seeds[w]}: S{rib.ParentSpurId}.{rib.Id} records a clearance under one"
                        + " sample");
                }
            }
        }

        /// <summary>18. The strict 2.5 still holds on the reach the tier allows.</summary>
        [Test]
        public void TierTargetReachIsStrictlyOneTierBelowTheParent()
        {
            for (var w = 0; w < secondaries.Length; w++)
            {
                foreach (var rib in secondaries[w].Spurs)
                {
                    var parent = primaries[w].Spurs[rib.ParentSpurId];
                    Assert.AreEqual(
                        parent.TargetReachMeters / SecondarySpurBuilder.TierRatio,
                        rib.TierTargetReachMeters, 1e-3f,
                        $"seed {Seeds[w]}: S{rib.ParentSpurId}.{rib.Id} tier target is not the parent"
                        + " a tier down");

                    // What it actually got is the tier or the room, whichever was less, and never more.
                    Assert.That(
                        rib.TargetReachMeters, Is.LessThanOrEqualTo(rib.TierTargetReachMeters + 1e-3f),
                        $"seed {Seeds[w]}: S{rib.ParentSpurId}.{rib.Id} reaches past its tier target");
                }
            }
        }

        // ---------- 11..15: determinism and the frozen boundary ----------

        /// <summary>11. The same seed rebuilds the same ribs, sample for sample.</summary>
        [Test]
        public void Stage3DIsDeterministic()
        {
            for (var w = 0; w < secondaries.Length; w++)
            {
                var built = BuildStage3C(Seeds[w]);
                var again = SecondarySpurBuilder.Build(
                    built.Ridges, built.Final, RidgeSettings, new Seed.WorldSeed(Seeds[w]));
                var first = secondaries[w];

                Assert.AreEqual(first.Spurs.Count, again.Spurs.Count, $"seed {Seeds[w]}: rib count");
                Assert.AreEqual(first.Strips.Count, again.Strips.Count, $"seed {Seeds[w]}: cell count");

                for (var i = 0; i < first.Spurs.Count; i++)
                {
                    var a = first.Spurs[i];
                    var b = again.Spurs[i];
                    Assert.AreEqual(a.Id, b.Id, $"seed {Seeds[w]} rib {i}: id");
                    Assert.AreEqual(a.ParentSpurId, b.ParentSpurId, $"seed {Seeds[w]} rib {i}: parent");
                    Assert.AreEqual(a.Side, b.Side, $"seed {Seeds[w]} rib {i}: side");
                    Assert.AreEqual(a.AttachT, b.AttachT, 0f, $"seed {Seeds[w]} rib {i}: attachT");
                    Assert.AreEqual(a.Termination, b.Termination, $"seed {Seeds[w]} rib {i}: end");
                    Assert.AreEqual(
                        a.ReliefBudgetMeters, b.ReliefBudgetMeters, 0f,
                        $"seed {Seeds[w]} rib {i}: budget");
                    Assert.AreEqual(
                        a.LateralDriftMeters, b.LateralDriftMeters, 0f,
                        $"seed {Seeds[w]} rib {i}: drift draw");
                    Assert.AreEqual(
                        a.TargetReachMeters, b.TargetReachMeters, 0f,
                        $"seed {Seeds[w]} rib {i}: reach draw");
                    Assert.AreEqual(
                        a.ClearanceMeters, b.ClearanceMeters, 0f,
                        $"seed {Seeds[w]} rib {i}: clearance");
                    Assert.AreEqual(a.Points.Length, b.Points.Length, $"seed {Seeds[w]} rib {i}: samples");

                    for (var k = 0; k < a.Points.Length; k++)
                    {
                        Assert.AreEqual(a.Points[k].x, b.Points[k].x, 0f, $"seed {Seeds[w]} {i}:{k} x");
                        Assert.AreEqual(a.Points[k].y, b.Points[k].y, 0f, $"seed {Seeds[w]} {i}:{k} y");
                    }
                }

                for (var i = 0; i < first.Strips.Count; i++)
                {
                    Assert.AreEqual(
                        first.Strips[i].ParentInterval, again.Strips[i].ParentInterval,
                        $"seed {Seeds[w]} cell {i}: interval");
                }
            }
        }

        /// <summary>12. Stage 3C comes out identical whether or not Stage 3D is built.</summary>
        [Test]
        public void Stage3COutputIsBitIdenticalWithAndWithoutStage3D()
        {
            for (var w = 0; w < Seeds.Length; w++)
            {
                // A: the frozen pipeline alone.
                var a = BuildStage3C(Seeds[w]);

                // B: the same pipeline, then Stage 3D on top of it.
                var b = BuildStage3C(Seeds[w]);
                SecondarySpurBuilder.Build(
                    b.Ridges, b.Final, RidgeSettings, new Seed.WorldSeed(Seeds[w]));

                Assert.AreEqual(
                    a.Ridges.Spurs.Count, b.Ridges.Spurs.Count, $"seed {Seeds[w]}: primary spur count");
                for (var i = 0; i < a.Ridges.Spurs.Count; i++)
                {
                    var x = a.Ridges.Spurs[i];
                    var y = b.Ridges.Spurs[i];
                    Assert.AreEqual(x.Id, y.Id, $"seed {Seeds[w]} S{i}: id");
                    Assert.AreEqual(x.AttachS, y.AttachS, 0f, $"seed {Seeds[w]} S{i}: attachment");
                    Assert.AreEqual(x.DriftArc, y.DriftArc, 0f, $"seed {Seeds[w]} S{i}: drift");
                    Assert.AreEqual(x.StripInterval, y.StripInterval, $"seed {Seeds[w]} S{i}: strip");
                    Assert.AreEqual(x.Termination, y.Termination, $"seed {Seeds[w]} S{i}: termination");
                    Assert.AreEqual(x.Points.Length, y.Points.Length, $"seed {Seeds[w]} S{i}: samples");
                    for (var k = 0; k < x.Points.Length; k++)
                    {
                        Assert.AreEqual(x.Points[k], y.Points[k], $"seed {Seeds[w]} S{i} sample {k}");
                    }
                }

                Assert.AreEqual(
                    a.Ridges.Valleys.Count, b.Ridges.Valleys.Count, $"seed {Seeds[w]}: corridor count");
                for (var i = 0; i < a.Ridges.Valleys.Count; i++)
                {
                    Assert.AreEqual(
                        a.Ridges.Valleys[i].LowerSpurId, b.Ridges.Valleys[i].LowerSpurId,
                        $"seed {Seeds[w]} V{i}: lower spur");
                    Assert.AreEqual(
                        a.Ridges.Valleys[i].UpperSpurId, b.Ridges.Valleys[i].UpperSpurId,
                        $"seed {Seeds[w]} V{i}: upper spur");
                    Assert.AreEqual(
                        a.Ridges.Valleys[i].SInterval, b.Ridges.Valleys[i].SInterval,
                        $"seed {Seeds[w]} V{i}: interval");
                }

                for (var i = 0; i < a.Network.Ranks.Count; i++)
                {
                    Assert.AreEqual(
                        a.Network.Ranks[i].ReliefVolumeCum, b.Network.Ranks[i].ReliefVolumeCum, 0f,
                        $"seed {Seeds[w]} V{i}: rank");
                    Assert.AreEqual(
                        a.Network.CandidateRays[i].Direction, b.Network.CandidateRays[i].Direction,
                        $"seed {Seeds[w]} V{i}: candidate ray");
                }

                CollectionAssert.AreEqual(
                    a.Topology.DownstreamOf, b.Topology.DownstreamOf, $"seed {Seeds[w]}: topology");
                Assert.AreEqual(
                    a.Topology.Stats.MergeCount, b.Topology.Stats.MergeCount,
                    $"seed {Seeds[w]}: merge count");
                Assert.AreEqual(
                    a.Final.Stats.ConfluenceCount, b.Final.Stats.ConfluenceCount,
                    $"seed {Seeds[w]}: confluence count");

                for (var i = 0; i < a.Final.Trajectories.Count; i++)
                {
                    var x = a.Final.Trajectories[i];
                    var y = b.Final.Trajectories[i];
                    Assert.AreEqual(x.LengthMeters, y.LengthMeters, 0f, $"seed {Seeds[w]} V{i}: length");
                    Assert.AreEqual(x.Termination, y.Termination, $"seed {Seeds[w]} V{i}: termination");
                    Assert.AreEqual(x.Points.Length, y.Points.Length, $"seed {Seeds[w]} V{i}: samples");
                    for (var k = 0; k < x.Points.Length; k++)
                    {
                        Assert.AreEqual(x.Points[k], y.Points[k], $"seed {Seeds[w]} V{i} sample {k}");
                    }
                }

                var before = ApronCrossings(a.Final);
                var after = ApronCrossings(b.Final);
                Assert.AreEqual(before, after, $"seed {Seeds[w]}: apron crossing count moved");
                Assert.AreEqual(
                    BaselineCrossings[Seeds[w]], after,
                    $"seed {Seeds[w]}: apron crossings differ from the Stage 3C baseline");
            }
        }

        /// <summary>13. No rib is anywhere in the primary skeleton.</summary>
        [Test]
        public void NoSecondarySpurEntersPrimaryRidgeSkeleton()
        {
            for (var w = 0; w < secondaries.Length; w++)
            {
                var fresh = BuildStage3C(Seeds[w]).Ridges;
                Assert.AreEqual(
                    fresh.Spurs.Count, primaries[w].Spurs.Count,
                    $"seed {Seeds[w]}: the primary spur list grew");
                Assert.AreEqual(
                    fresh.Spurs.Count, primaries[w].Stats.SpurCount,
                    $"seed {Seeds[w]}: the primary spur stat moved");

                for (var i = 0; i < primaries[w].Spurs.Count; i++)
                {
                    Assert.AreEqual(
                        i, primaries[w].Spurs[i].Id,
                        $"seed {Seeds[w]}: primary ids are no longer their own index");
                }

                // The two collections do not share a type, so a rib cannot be put in the primary list
                // by accident; this pins that the types stayed apart.
                Assert.IsFalse(
                    typeof(SpurCurve).IsAssignableFrom(typeof(SecondarySpur)),
                    "a secondary became a SpurCurve");
            }
        }

        /// <summary>14. Stage 3D created no corridor of any kind.</summary>
        [Test]
        public void NoSecondaryCorridorExists()
        {
            for (var w = 0; w < secondaries.Length; w++)
            {
                var fresh = BuildStage3C(Seeds[w]).Ridges;
                Assert.AreEqual(
                    fresh.Valleys.Count, primaries[w].Valleys.Count,
                    $"seed {Seeds[w]}: the corridor set grew");

                foreach (var corridor in primaries[w].Valleys)
                {
                    Assert.That(
                        corridor.LowerSpurId, Is.LessThan(primaries[w].Spurs.Count),
                        $"seed {Seeds[w]} V{corridor.Id}: bounded by something that is not a primary");
                    Assert.That(
                        corridor.UpperSpurId, Is.LessThan(primaries[w].Spurs.Count),
                        $"seed {Seeds[w]} V{corridor.Id}: bounded by something that is not a primary");
                }

                Assert.IsFalse(
                    typeof(ValleyCorridor).IsAssignableFrom(typeof(SecondaryStrip)),
                    "a child strip became a corridor");
                Assert.AreEqual(
                    finals[w].Trajectories.Count, primaries[w].Valleys.Count,
                    $"seed {Seeds[w]}: trajectory count no longer matches the corridor set");
            }
        }

        /// <summary>15. Building Stage 3D disturbs no Stage 3B draw.</summary>
        [Test]
        public void Stage3DDoesNotPerturbStage3BRng()
        {
            for (var w = 0; w < Seeds.Length; w++)
            {
                var before = BuildStage3C(Seeds[w]).Ridges;

                var carrier = BuildStage3C(Seeds[w]);
                SecondarySpurBuilder.Build(
                    carrier.Ridges, carrier.Final, RidgeSettings, new Seed.WorldSeed(Seeds[w]));

                var after = BuildStage3C(Seeds[w]).Ridges;

                Assert.AreEqual(before.Spurs.Count, after.Spurs.Count, $"seed {Seeds[w]}: spur count");
                for (var i = 0; i < before.Spurs.Count; i++)
                {
                    Assert.AreEqual(
                        before.Spurs[i].AttachS, after.Spurs[i].AttachS, 0f,
                        $"seed {Seeds[w]} S{i}: station draw moved");
                    Assert.AreEqual(
                        before.Spurs[i].DriftArc, after.Spurs[i].DriftArc, 0f,
                        $"seed {Seeds[w]} S{i}: drift draw moved");
                    Assert.AreEqual(
                        before.Spurs[i].TargetReachMeters, after.Spurs[i].TargetReachMeters, 0f,
                        $"seed {Seeds[w]} S{i}: reach draw moved");
                }

                Assert.AreNotEqual(
                    SecondarySpurBuilder.StationStream, "terrain.ridge.station",
                    "Stage 3D shares Stage 3B's station stream");
                Assert.AreNotEqual(
                    SecondarySpurBuilder.ShapeStream, "terrain.ridge.shape",
                    "Stage 3D shares Stage 3B's shape stream");
            }
        }

        // ---------- fixtures ----------

        private readonly struct Built
        {
            public readonly RidgeSkeleton Ridges;
            public readonly ValleyNetwork Network;
            public readonly ValleyTopology Topology;
            public readonly ValleyTrajectoryNetwork Final;

            public Built(
                RidgeSkeleton ridges,
                ValleyNetwork network,
                ValleyTopology topology,
                ValleyTrajectoryNetwork final)
            {
                Ridges = ridges;
                Network = network;
                Topology = topology;
                Final = final;
            }
        }

        private static Built BuildStage3C(int seed)
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
            return new Built(ridges, network, topology, final);
        }

        /// <summary>The depth a parent actually achieved, as the builder reads it.</summary>
        private static float Realised(SpurCurve parent) =>
            parent.Termination == SpurTermination.Toe
                ? 1f
                : Mathf.Clamp01(parent.ReachMeters / Mathf.Max(1e-3f, parent.TargetReachMeters));

        private static List<SecondarySpur> RibsOn(
            SecondaryRidgeSkeleton secondary, int parentSpurId, int side)
        {
            var found = new List<SecondarySpur>();
            foreach (var rib in secondary.Spurs)
            {
                if (rib.ParentSpurId == parentSpurId && rib.Side == side)
                {
                    found.Add(rib);
                }
            }

            return found;
        }

        private static int CountChildren(SecondaryRidgeSkeleton secondary, RidgeSkeleton primary)
        {
            var total = 0;
            for (var p = 0; p < primary.Spurs.Count; p++)
            {
                total += secondary.ChildrenOf(primary.Spurs[p].Id).Count;
            }

            return total;
        }

        /// <summary>
        /// The Stage 3C apron crossings: strictly-interior intersections past both toes.
        /// </summary>
        /// <remarks>
        /// The same predicate and the same window as the frozen Slice C test, so the number this
        /// returns is comparable with the recorded baseline rather than a second opinion about it.
        /// </remarks>
        private static int ApronCrossings(ValleyTrajectoryNetwork final)
        {
            var count = 0;
            for (var a = 0; a < final.Trajectories.Count; a++)
            {
                for (var b = a + 1; b < final.Trajectories.Count; b++)
                {
                    var first = final.Trajectories[a];
                    var second = final.Trajectories[b];
                    for (var i = first.ToeIndex; i + 1 < first.Points.Length; i++)
                    {
                        for (var j = second.ToeIndex; j + 1 < second.Points.Length; j++)
                        {
                            if (Interior(
                                    first.Points[i], first.Points[i + 1],
                                    second.Points[j], second.Points[j + 1]))
                            {
                                count++;
                            }
                        }
                    }
                }
            }

            return count;
        }

        private static bool Crosses(Vector2[] a, Vector2[] b, int skipFirstOfA)
        {
            for (var i = skipFirstOfA; i + 1 < a.Length; i++)
            {
                for (var j = 0; j + 1 < b.Length; j++)
                {
                    if (Town.Geometry.Geo2.SegmentIntersect(a[i], a[i + 1], b[j], b[j + 1], out _))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool Interior(Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1)
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
    }
}
