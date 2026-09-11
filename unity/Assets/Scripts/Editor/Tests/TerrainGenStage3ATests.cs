using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.TerrainGen.Tests
{
    /// <summary>
    /// Stage 3A: massif ownership, flanks and the medial divides between massifs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Stage 3A raises no ground, so nothing here asserts anything about height. What it asserts is
    /// that the partition is a partition: total, exclusive, deterministic down to its tie-breaks, and
    /// bounded by real geography rather than by an arbitrary cut.
    /// </para>
    /// <para>
    /// Most tests run on Stage 2A's cheap path - partition, landmass, coastline, plates, axes - and
    /// raise the massifs on the accepted axes without ever evaluating the 1025 x 1025 lattice, which
    /// is what Stage 3A actually consumes. Only <see cref="Stage2RemainsUntouchedByBuildingASkeleton"/>
    /// runs a full Stage 2B, because only that test needs a height field to prove it did not move.
    /// </para>
    /// <para>
    /// Stage 2A caps a world at two systems and, across the twenty-nine worlds audited, produced one
    /// on every world that got one at all. The multi-massif path is therefore unreachable from a real
    /// seed and is exercised on hand-built geometry instead - which is the point of
    /// <see cref="Stage3ASkeleton.Build(int, IReadOnlyList{MountainMassif}, CoastQuery, RidgeSettings)"/>
    /// taking the parts rather than a <see cref="Stage2BResult"/>.
    /// </para>
    /// </remarks>
    public sealed class TerrainGenStage3ATests
    {
        /// <summary>The canonical five, the two the selection pass restored, and the refusals.</summary>
        private static readonly int[] Seeds =
        {
            110201, 424242, 20260905, 7, 101010, 20261231, 1337, 256, 777, 31337,
        };

        private static TerrainGenSettings TerrainSettings => TerrainGenSettings.Default;
        private static MacroStructureSettings MacroSettings => MacroStructureSettings.Default;
        private static MassifSettings MassifSettings => MassifSettings.Default;
        private static RidgeSettings RidgeSettings => RidgeSettings.Default;

        private Stage2AResult[] stage2a;
        private List<MountainMassif>[] massifs;
        private Stage3ASkeleton[] worlds;

        [OneTimeSetUp]
        public void BuildWorlds()
        {
            stage2a = new Stage2AResult[Seeds.Length];
            massifs = new List<MountainMassif>[Seeds.Length];
            worlds = new Stage3ASkeleton[Seeds.Length];

            for (var i = 0; i < Seeds.Length; i++)
            {
                stage2a[i] = Stage2AMacroStructure.Run(Seeds[i], TerrainSettings, MacroSettings);
                massifs[i] = RaiseMassifs(Seeds[i], stage2a[i]);
                worlds[i] = Stage3ASkeleton.Build(
                    Seeds[i], massifs[i], stage2a[i].Coast, RidgeSettings);
            }
        }

        // ---------- 1. determinism ----------

        /// <summary>The same Stage 2 input produces the same skeleton, down to every coordinate.</summary>
        /// <remarks>
        /// Stage 3A draws no seed stream, so this is really asserting that nothing crept in that
        /// depends on allocation order, iteration order or accumulated floating point state.
        /// </remarks>
        [Test]
        public void TheSameStage2InputProducesTheSameSkeleton()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                var again = Stage3ASkeleton.Build(
                    Seeds[w], massifs[w], stage2a[w].Coast, RidgeSettings);

                Assert.AreEqual(
                    worlds[w].Stats.ToLine(), again.Stats.ToLine(), $"seed {Seeds[w]}: stats");

                Assert.AreEqual(
                    worlds[w].Divides.Count, again.Divides.Count, $"seed {Seeds[w]}: divide count");

                for (var d = 0; d < worlds[w].Divides.Count; d++)
                {
                    var first = worlds[w].Divides[d];
                    var second = again.Divides[d];

                    Assert.AreEqual(
                        first.Points.Length, second.Points.Length,
                        $"seed {Seeds[w]} divide {d}: sample count");

                    for (var p = 0; p < first.Points.Length; p++)
                    {
                        Assert.AreEqual(
                            first.Points[p], second.Points[p],
                            $"seed {Seeds[w]} divide {d} point {p}");
                    }
                }

                foreach (var p in SampleGrid(24))
                {
                    var a = worlds[w].Ownership.At(p.x, p.y);
                    var b = again.Ownership.At(p.x, p.y);

                    Assert.AreEqual(a.Kind, b.Kind, $"seed {Seeds[w]} at {p}: kind");
                    Assert.AreEqual(a.MassifIndex, b.MassifIndex, $"seed {Seeds[w]} at {p}: owner");

                    var hasA = a.TryGetFrame(out var frameA);
                    var hasB = b.TryGetFrame(out var frameB);
                    Assert.AreEqual(hasA, hasB, $"seed {Seeds[w]} at {p}: frame availability");
                    if (!hasA)
                    {
                        continue;
                    }

                    Assert.AreEqual(
                        frameA.FlankSign, frameB.FlankSign, $"seed {Seeds[w]} at {p}: flank");
                    Assert.AreEqual(frameA.S, frameB.S, $"seed {Seeds[w]} at {p}: s");
                    Assert.AreEqual(
                        frameA.OffsetMeters, frameB.OffsetMeters, $"seed {Seeds[w]} at {p}: offset");
                }
            }
        }

        /// <summary>Repeating a query, in any order, gives the same answer.</summary>
        [Test]
        public void OwnershipIsAPureFunctionOfPosition()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                var points = new List<Vector2>(SampleGrid(20));
                var forward = new MassifOwner[points.Count];
                for (var i = 0; i < points.Count; i++)
                {
                    forward[i] = worlds[w].Ownership.At(points[i].x, points[i].y);
                }

                // Backwards, so any state carried between calls would have to survive being read in
                // the opposite order to be missed here.
                for (var i = points.Count - 1; i >= 0; i--)
                {
                    var again = worlds[w].Ownership.At(points[i].x, points[i].y);
                    Assert.AreEqual(forward[i].Kind, again.Kind, $"seed {Seeds[w]} at {points[i]}");
                    Assert.AreEqual(
                        forward[i].MassifIndex, again.MassifIndex, $"seed {Seeds[w]} at {points[i]}");

                    if (!forward[i].TryGetFrame(out var before))
                    {
                        continue;
                    }

                    Assert.IsTrue(again.TryGetFrame(out var after), $"seed {Seeds[w]}: frame vanished");
                    Assert.AreEqual(
                        before.OffsetMeters, after.OffsetMeters, $"seed {Seeds[w]} at {points[i]}");
                    Assert.AreEqual(before.S, after.S, $"seed {Seeds[w]} at {points[i]}");
                }
            }
        }

        // ---------- 2. exclusivity ----------

        /// <summary>No point is claimed by two massifs, and the claimant is genuinely the nearest.</summary>
        /// <remarks>
        /// Exclusivity is trivial as a data structure - the query returns one index - so what is worth
        /// asserting is that the index it returns is the right one: no other axis is nearer than the
        /// owner's, beyond the tie epsilon.
        /// </remarks>
        [Test]
        public void EveryPointHasExactlyOneOwnerAndItIsTheNearest()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                var world = worlds[w];
                foreach (var p in SampleGrid(40))
                {
                    var owner = world.Ownership.At(p.x, p.y);

                    // Every owned kind, not only the structural one: ownership stayed total when the
                    // frame stopped being, and the nearest-axis rule has to hold across all of it.
                    if (!owner.IsOwned)
                    {
                        Assert.That(
                            owner.Kind,
                            Is.EqualTo(MassifOwnerKind.Ocean).Or.EqualTo(MassifOwnerKind.Unclaimed),
                            $"seed {Seeds[w]} at {p}: unowned but {owner.Kind}");
                        continue;
                    }

                    var owned = world.Massifs[owner.MassifIndex].Axis.DistanceTo(p.x, p.y);
                    for (var m = 0; m < world.Massifs.Count; m++)
                    {
                        if (m == owner.MassifIndex)
                        {
                            continue;
                        }

                        Assert.That(
                            world.Massifs[m].Axis.DistanceTo(p.x, p.y),
                            Is.GreaterThanOrEqualTo(owned - RidgeSettings.OwnershipTieEpsilonMeters),
                            $"seed {Seeds[w]} at {p}: massif {m} is nearer than the owner "
                            + $"{owner.MassifIndex}");
                    }
                }
            }
        }

        /// <summary>The ocean belongs to nobody, and every land point has an answer.</summary>
        /// <remarks>
        /// The half of totality that keeps Stage 3 out of the water: the coastline is where ownership
        /// stops, which is what lets a divide's termination there be a property of the partition
        /// rather than a clip applied afterwards. A world with no massif answers Unclaimed, which is
        /// a legitimate geography - three of these ten worlds are like that.
        /// </remarks>
        [Test]
        public void TheOceanIsUnownedAndTheLandAlwaysHasAnAnswer()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                var world = worlds[w];
                var coast = stage2a[w].Coast;
                var hasMassif = world.Massifs.Count > 0;

                foreach (var p in SampleGrid(40))
                {
                    var owner = world.Ownership.At(p.x, p.y);
                    var land = coast.IsInside(p.x, p.y);

                    if (!land)
                    {
                        Assert.AreEqual(
                            MassifOwnerKind.Ocean, owner.Kind, $"seed {Seeds[w]} at {p}: ocean");
                        continue;
                    }

                    // Land always has an owner where there is a massif to own it, whether or not that
                    // ground carries a usable frame. Where there is none, it is Unclaimed.
                    if (hasMassif)
                    {
                        Assert.IsTrue(owner.IsOwned, $"seed {Seeds[w]} at {p}: land with no owner");
                    }
                    else
                    {
                        Assert.AreEqual(
                            MassifOwnerKind.Unclaimed, owner.Kind, $"seed {Seeds[w]} at {p}");
                    }
                }
            }
        }

        // ---------- 3. tie handling ----------

        /// <summary>Equidistant ground always resolves to the same massif.</summary>
        /// <remarks>
        /// Built rather than searched for: every point of a divide is equidistant by construction, so
        /// the divide is exactly the set of ties and testing it tests the rule where it actually
        /// fires. The rule is the lower index, and it has to hold from both approaches - a point
        /// nudged onto the line from either side must land on the same owner.
        /// </remarks>
        [Test]
        public void EquidistantGroundGoesToTheLowerMassifIndex()
        {
            var world = TwoMassifWorld();
            Assert.AreEqual(1, world.Divides.Count, "the two-massif fixture should share one divide");

            var divide = world.Divides[0];
            for (var i = 0; i < divide.Points.Length; i++)
            {
                var p = divide.Points[i];
                var owner = world.Ownership.At(p.x, p.y);
                if (!owner.IsOwned)
                {
                    continue;
                }

                Assert.That(
                    owner.MarginMeters,
                    Is.LessThanOrEqualTo(RidgeSettings.OwnershipTieEpsilonMeters + 1e-3f),
                    $"divide point {i} is not actually a tie");

                Assert.AreEqual(
                    divide.MassifA, owner.MassifIndex,
                    $"divide point {i}: a tie went to massif {owner.MassifIndex}, not the lower index");
            }
        }

        /// <summary>Reversing the order the massifs are given reverses nothing about the geometry.</summary>
        /// <remarks>
        /// The tie rule is "lowest index wins", so swapping the two massifs must swap which side of
        /// the divide each owns while leaving the divide itself exactly where it was. If the partition
        /// depended on iteration order for anything but the tie, the line would move.
        /// </remarks>
        [Test]
        public void SwappingTheMassifOrderMovesTheTieAndNothingElse()
        {
            var coast = SquareCoast();
            var straight = new[] { StraightMassif(0, 1100f), StraightMassif(1, 1700f) };
            var swapped = new[] { StraightMassif(0, 1700f), StraightMassif(1, 1100f) };

            var first = Stage3ASkeleton.Build(0, straight, coast, RidgeSettings);
            var second = Stage3ASkeleton.Build(0, swapped, coast, RidgeSettings);

            Assert.AreEqual(
                first.Divides.Count, second.Divides.Count, "divide count changed with the order");

            Assert.AreEqual(
                first.Divides[0].LengthMeters, second.Divides[0].LengthMeters, 1f,
                "the divide moved when the massifs were merely renumbered");

            foreach (var p in SampleGrid(24))
            {
                var a = first.Ownership.At(p.x, p.y);
                var b = second.Ownership.At(p.x, p.y);
                if (!a.IsOwned || a.MarginMeters <= 1f)
                {
                    continue;
                }

                Assert.AreEqual(
                    a.MassifIndex, 1 - b.MassifIndex,
                    $"at {p}: ownership did not simply swap with the numbering");
            }
        }

        // ---------- 4. the axis is the divide ----------

        /// <summary>A massif's own axis separates its two flanks, everywhere along it.</summary>
        /// <remarks>
        /// The property Stage 3B depends on when it puts a spur system on each flank: crossing the
        /// axis changes the flank and nothing else. Sampled either side of the axis at five metres,
        /// which is far inside every certified half-width.
        /// </remarks>
        [Test]
        public void TheAxisSeparatesTheTwoFlanks()
        {
            const float probe = 5f;

            for (var w = 0; w < worlds.Length; w++)
            {
                var world = worlds[w];
                for (var m = 0; m < world.Massifs.Count; m++)
                {
                    var massif = world.Massifs[m];
                    for (var k = 1; k < 40; k++)
                    {
                        var s = k / 40f;
                        var here = massif.PointAt(s);
                        var normal = AxisNormal(massif, s);

                        var left = here + normal * probe;
                        var right = here - normal * probe;

                        massif.Project(left.x, left.y, out _, out var leftOffset);
                        massif.Project(right.x, right.y, out _, out var rightOffset);

                        Assert.AreEqual(
                            -MassifOwnership.FlankSignOf(leftOffset),
                            MassifOwnership.FlankSignOf(rightOffset),
                            $"seed {Seeds[w]} massif {m} at s={s}: both probes read the same flank");

                        Assert.AreEqual(
                            probe, Mathf.Abs(leftOffset), 1.5f,
                            $"seed {Seeds[w]} massif {m} at s={s}: transverse coordinate is wrong");
                    }
                }
            }
        }

        /// <summary>Every massif has exactly two flanks, one of them the steep one.</summary>
        [Test]
        public void EveryMassifHasTwoFlanksAndOneIsSteep()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                var world = worlds[w];
                Assert.AreEqual(
                    world.Massifs.Count * 2, world.Flanks.Count, $"seed {Seeds[w]}: flank count");

                for (var m = 0; m < world.Massifs.Count; m++)
                {
                    var negative = world.Flanks[m * 2 + 0];
                    var positive = world.Flanks[m * 2 + 1];

                    Assert.AreEqual(-1, negative.Sign, $"seed {Seeds[w]} massif {m}");
                    Assert.AreEqual(+1, positive.Sign, $"seed {Seeds[w]} massif {m}");
                    Assert.AreEqual(m, negative.MassifIndex, $"seed {Seeds[w]} massif {m}");
                    Assert.AreEqual(m, positive.MassifIndex, $"seed {Seeds[w]} massif {m}");

                    Assert.AreNotEqual(
                        negative.IsSteep, positive.IsSteep,
                        $"seed {Seeds[w]} massif {m}: both flanks claim the same character");
                }
            }
        }

        // ---------- 5. inter-massif divides ----------

        /// <summary>The divide is equidistant, stays off both axes, and does not cross itself.</summary>
        [Test]
        public void TheDivideIsMedialAndCrossesNeitherAxis()
        {
            var world = TwoMassifWorld();
            var divide = world.Divides[0];

            Assert.That(
                divide.MaxImbalanceMeters,
                Is.LessThanOrEqualTo(RidgeSettings.DivideToleranceMeters + 1e-3f),
                "the divide is not equidistant from the two axes");

            Assert.That(
                divide.MinAxisDistanceMeters, Is.GreaterThan(1f),
                "the divide touches an axis");

            Assert.IsFalse(divide.SelfIntersects(), "the divide crosses itself");

            // Half the gap between the axes is the closest a medial line can ever come to either, so
            // this is the construction's own bound rather than a tolerance chosen to pass.
            Assert.That(
                divide.MinAxisDistanceMeters, Is.GreaterThan(250f),
                "the divide came nearer an axis than half the gap between them");

            for (var i = 0; i + 1 < divide.Points.Length; i++)
            {
                for (var m = 0; m < world.Massifs.Count; m++)
                {
                    var axis = world.Massifs[m].Axis;
                    for (var k = 0; k + 1 < axis.Points.Length; k++)
                    {
                        Assert.IsFalse(
                            Town.Geometry.Geo2.SegmentIntersect(
                                divide.Points[i], divide.Points[i + 1],
                                axis.Points[k], axis.Points[k + 1], out _),
                            $"divide segment {i} crosses massif {m} axis segment {k}");
                    }
                }
            }
        }

        /// <summary>Three massifs produce three divides, each stopping at a boundary, none crossing an axis.</summary>
        /// <remarks>
        /// Not reachable from a seed - Stage 2A caps a world at two systems - but the code that
        /// handles it exists, and code that exists and is never run is worse than code that does not.
        /// The arrangement is a triangle, so the three bisectors meet at a triple junction, which is
        /// the one structural termination that is neither the coast nor the world edge.
        /// </remarks>
        [Test]
        public void ThreeMassifsMeetAtATripleJunction()
        {
            var coast = SquareCoast();
            var three = new[]
            {
                ShortMassif(0, new Vector2(1000f, 1000f)),
                ShortMassif(1, new Vector2(1800f, 1000f)),
                ShortMassif(2, new Vector2(1400f, 1700f)),
            };

            var world = Stage3ASkeleton.Build(0, three, coast, RidgeSettings);

            Assert.AreEqual(3, world.Divides.Count, "three massifs share three boundaries");
            Assert.AreEqual(0, world.Stats.UnresolvedDivideEnds, "an arm stopped nowhere");
            Assert.AreEqual(0, world.Stats.SelfIntersectingDivides, "a divide crossed itself");

            var junctions = 0;
            for (var d = 0; d < world.Divides.Count; d++)
            {
                var divide = world.Divides[d];
                Assert.That(
                    divide.MinAxisDistanceMeters, Is.GreaterThan(1f),
                    $"divide {divide.MassifA}-{divide.MassifB} touches an axis");

                if (divide.StartTermination == DivideTermination.TripleJunction)
                {
                    junctions++;
                }

                if (divide.EndTermination == DivideTermination.TripleJunction)
                {
                    junctions++;
                }
            }

            Assert.That(
                junctions, Is.GreaterThan(0),
                "three massifs in a triangle should meet somewhere");

            foreach (var p in SampleGrid(40))
            {
                var owner = world.Ownership.At(p.x, p.y);
                if (owner.Kind != MassifOwnerKind.Massif)
                {
                    continue;
                }

                var owned = three[owner.MassifIndex].Axis.DistanceTo(p.x, p.y);
                for (var m = 0; m < three.Length; m++)
                {
                    Assert.That(
                        three[m].Axis.DistanceTo(p.x, p.y),
                        Is.GreaterThanOrEqualTo(owned - RidgeSettings.OwnershipTieEpsilonMeters),
                        $"at {p}: massif {m} is nearer than the owner");
                }
            }
        }

        // ---------- 6. termination ----------

        /// <summary>Every divide arm ends at the coast, the world edge or a triple junction.</summary>
        /// <remarks>
        /// The list is exhaustive on purpose. An arm that simply stopped would be an arbitrary cut
        /// through the world, which is what the design forbids, and it is reported as Unresolved
        /// rather than silently accepted as a shorter divide.
        /// </remarks>
        [Test]
        public void EveryDivideTerminatesAtARealBoundary()
        {
            var fixtures = new[] { TwoMassifWorld(), ThreeMassifWorld() };

            foreach (var world in fixtures)
            {
                Assert.That(world.Divides.Count, Is.GreaterThan(0), "fixture built no divide");

                for (var d = 0; d < world.Divides.Count; d++)
                {
                    var divide = world.Divides[d];
                    Assert.AreNotEqual(
                        DivideTermination.Unresolved, divide.StartTermination,
                        $"divide {divide.MassifA}-{divide.MassifB} start");
                    Assert.AreNotEqual(
                        DivideTermination.Unresolved, divide.EndTermination,
                        $"divide {divide.MassifA}-{divide.MassifB} end");
                }
            }
        }

        /// <summary>A divide that ends at the coastline ends on the coastline, not near it.</summary>
        [Test]
        public void ACoastalTerminationLandsOnTheShore()
        {
            var coast = SquareCoast();
            var world = TwoMassifWorld();

            for (var d = 0; d < world.Divides.Count; d++)
            {
                var divide = world.Divides[d];
                var points = divide.Points;

                CheckEnd(divide.StartTermination, points[0]);
                CheckEnd(divide.EndTermination, points[points.Length - 1]);
            }

            void CheckEnd(DivideTermination termination, Vector2 end)
            {
                if (termination != DivideTermination.Coastline)
                {
                    return;
                }

                Assert.That(
                    coast.NearestDistance(end.x, end.y), Is.LessThanOrEqualTo(1f),
                    $"a coastal termination sits {coast.NearestDistance(end.x, end.y):F1} m inland");
            }
        }

        // ---------- 7. Stage 2 immutability ----------

        /// <summary>Building the skeleton changes nothing about Stage 1 or Stage 2.</summary>
        /// <remarks>
        /// <para>
        /// Stage 3A holds references to the very <see cref="MountainMassif"/> and
        /// <see cref="MacroAxis"/> objects Stage 2B produced, so a mutation anywhere in the slice
        /// would be visible here rather than merely improbable. The field is hashed before and after,
        /// and the two contract values Stage 2A certifies are compared by value.
        /// </para>
        /// <para>
        /// One seed and one full Stage 2B run, which is about seven seconds. The five Stage 1 hashes
        /// are already pinned by <c>TerrainGenStage2ATests.Stage1HeightField_MatchesItsRecordedHashes</c>
        /// and running them again here would cost half a minute to learn nothing new; the recorded
        /// hash for this seed is asserted anyway, so a Stage 1 change cannot pass unnoticed.
        /// </para>
        /// </remarks>
        [Test]
        public void Stage2RemainsUntouchedByBuildingASkeleton()
        {
            const int seed = 110201;
            var stage2b = Stage2BMassifGenerator.Generate(
                seed, TerrainSettings, MacroSettings, MassifSettings);

            Assert.AreEqual(
                "7599879a71250a06", stage2b.Stage1.Stats.FieldHash, "Stage 1 changed");

            var fieldBefore = stage2b.Field.Hash();
            var upliftBefore = stage2b.UpliftField.Hash();
            var heightsBefore = (float[])stage2b.Field.Heights.Clone();

            var cores = new Vector2[stage2b.Massifs.Count];
            var widths = new float[stage2b.Massifs.Count];
            var crests = new float[stage2b.Massifs.Count];
            var lengths = new float[stage2b.Massifs.Count];
            for (var m = 0; m < stage2b.Massifs.Count; m++)
            {
                cores[m] = stage2b.Massifs[m].Axis.CertifiedCore;
                widths[m] = stage2b.Massifs[m].Axis.UsableHalfWidthMeters;
                crests[m] = stage2b.Massifs[m].CrestHeightMeters;
                lengths[m] = stage2b.Massifs[m].LengthMeters;
            }

            var skeleton = Stage3ASkeleton.Build(stage2b);

            // Force the whole partition to be queried, so a lazily built cache would have to have run.
            foreach (var p in SampleGrid(40))
            {
                skeleton.Ownership.At(p.x, p.y);
            }

            Assert.AreEqual(fieldBefore, stage2b.Field.Hash(), "the Stage 2 height field moved");
            Assert.AreEqual(upliftBefore, stage2b.UpliftField.Hash(), "the uplift field moved");

            var differing = 0;
            for (var i = 0; i < heightsBefore.Length; i++)
            {
                if (heightsBefore[i] != stage2b.Field.Heights[i])
                {
                    differing++;
                }
            }

            Assert.AreEqual(0, differing, $"{differing} height samples changed");

            for (var m = 0; m < stage2b.Massifs.Count; m++)
            {
                Assert.AreEqual(cores[m], stage2b.Massifs[m].Axis.CertifiedCore, $"massif {m}: core");
                Assert.AreEqual(
                    widths[m], stage2b.Massifs[m].Axis.UsableHalfWidthMeters, 0f, $"massif {m}: width");
                Assert.AreEqual(
                    crests[m], stage2b.Massifs[m].CrestHeightMeters, 0f, $"massif {m}: crest");
                Assert.AreEqual(
                    lengths[m], stage2b.Massifs[m].LengthMeters, 0f, $"massif {m}: length");
            }

            Assert.AreSame(
                stage2b.Massifs[0], skeleton.Massifs[0],
                "the skeleton copied the massif instead of referring to it");
        }

        // ---------- 8. coordinate purity ----------

        /// <summary>
        /// The four-tile seam is invisible to the partition.
        /// </summary>
        /// <remarks>
        /// The world is one 1025 x 1025 lattice presented to Unity as four TerrainData windows, so
        /// the only place a tile could leak into the geometry is the line where those windows meet.
        /// Nothing in Stage 3A knows that line exists; this walks it and asserts the answer is
        /// continuous across it, except where the point is genuinely on a divide, which is a property
        /// of the massifs and not of the seam.
        /// </remarks>
        [Test]
        public void OwnershipHasNoSeamAtTheTileBoundary()
        {
            const float epsilon = 0.01f;
            var seamX = WorldScale.WorldExtentXMeters * 0.5f;
            var seamZ = WorldScale.WorldExtentZMeters * 0.5f;

            for (var w = 0; w < worlds.Length; w++)
            {
                var world = worlds[w];
                for (var k = 1; k < 200; k++)
                {
                    var t = k / 200f;

                    Compare(
                        world, new Vector2(seamX, t * WorldScale.WorldExtentZMeters), Vector2.right,
                        $"seed {Seeds[w]} across x={seamX}");

                    Compare(
                        world, new Vector2(t * WorldScale.WorldExtentXMeters, seamZ), Vector2.up,
                        $"seed {Seeds[w]} across z={seamZ}");
                }
            }

            void Compare(Stage3ASkeleton skeleton, Vector2 on, Vector2 across, string what)
            {
                var here = skeleton.Ownership.At(on.x, on.y);
                var before = skeleton.Ownership.At(
                    on.x - across.x * epsilon, on.y - across.y * epsilon);
                var after = skeleton.Ownership.At(
                    on.x + across.x * epsilon, on.y + across.y * epsilon);

                // A point within a hair of the coastline or of a divide legitimately changes answer
                // over a centimetre. Everywhere else the seam must be undetectable.
                if (here.Kind != before.Kind || here.Kind != after.Kind)
                {
                    return;
                }

                if (!here.IsOwned || here.MarginMeters < 1f)
                {
                    return;
                }

                Assert.AreEqual(here.MassifIndex, before.MassifIndex, $"{what}: owner before");
                Assert.AreEqual(here.MassifIndex, after.MassifIndex, $"{what}: owner after");

                if (!here.TryGetFrame(out var frame))
                {
                    return;
                }

                Assert.IsTrue(before.TryGetFrame(out var frameBefore), $"{what}: frame before");
                Assert.IsTrue(after.TryGetFrame(out var frameAfter), $"{what}: frame after");
                Assert.AreEqual(
                    frame.OffsetMeters, frameBefore.OffsetMeters, 0.1f, $"{what}: offset before");
                Assert.AreEqual(
                    frame.OffsetMeters, frameAfter.OffsetMeters, 0.1f, $"{what}: offset after");
            }
        }

        /// <summary>
        /// The ownership metric and the massif frame agree to well inside a metre.
        /// </summary>
        /// <remarks>
        /// Ownership compares distance to <see cref="MacroAxis.Points"/>; the frame comes from
        /// <see cref="MountainMassif.Project"/>, which measures against the Catmull-Rom curve Stage 2B
        /// smoothed that polyline into. They are two curves and the gap between them is real, so it is
        /// measured rather than assumed away. It bounds only how far from the true medial line a point
        /// could be misassigned, and the divide itself is built entirely on the ownership metric, so
        /// the partition and its boundary cannot disagree.
        /// </remarks>
        [Test]
        public void TheFrameAndTheOwnershipMetricAgree()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                if (worlds[w].Massifs.Count == 0)
                {
                    continue;
                }

                var worst = worlds[w].Ownership.MeasureFrameAgreement();
                Assert.That(
                    worst, Is.LessThan(1f),
                    $"seed {Seeds[w]}: the smoothed frame and the stored polyline differ by "
                    + $"{worst:F2} m");
            }
        }

        // ---------- worlds without a massif ----------

        /// <summary>A world with no massif has no flanks, no divides and unclaimed land.</summary>
        /// <remarks>
        /// The refusal worlds. Stage 2A declines to certify an axis on 1337, 256, 777 and 31337, and
        /// Stage 3A has to say so plainly rather than fail, produce an empty massif or invent one.
        /// </remarks>
        [Test]
        public void AWorldWithoutAMassifIsUnclaimedRatherThanBroken()
        {
            var withoutAMassif = 0;
            for (var w = 0; w < worlds.Length; w++)
            {
                if (worlds[w].Massifs.Count > 0)
                {
                    continue;
                }

                withoutAMassif++;
                Assert.AreEqual(0, worlds[w].Flanks.Count, $"seed {Seeds[w]}: flanks without a massif");
                Assert.AreEqual(0, worlds[w].Divides.Count, $"seed {Seeds[w]}: divides without a massif");

                var landSamples = 0;
                foreach (var p in SampleGrid(40))
                {
                    var owner = worlds[w].Ownership.At(p.x, p.y);
                    if (owner.Kind == MassifOwnerKind.Ocean)
                    {
                        continue;
                    }

                    landSamples++;
                    Assert.AreEqual(
                        MassifOwnerKind.Unclaimed, owner.Kind, $"seed {Seeds[w]} at {p}");
                    Assert.IsFalse(
                        owner.TryGetFrame(out _),
                        $"seed {Seeds[w]} at {p}: unclaimed land was given a frame");
                }

                Assert.That(landSamples, Is.GreaterThan(0), $"seed {Seeds[w]}: found no land at all");
            }

            Assert.That(
                withoutAMassif, Is.GreaterThan(0),
                "the seed list is supposed to include the worlds Stage 2A refuses");
        }

        /// <summary>A single-massif world owns all its land and has nothing to divide with.</summary>
        [Test]
        public void ASingleMassifOwnsTheWholeIsland()
        {
            var single = 0;
            for (var w = 0; w < worlds.Length; w++)
            {
                if (worlds[w].Massifs.Count != 1)
                {
                    continue;
                }

                single++;
                Assert.AreEqual(0, worlds[w].Divides.Count, $"seed {Seeds[w]}: divide without a pair");

                foreach (var p in SampleGrid(40))
                {
                    var owner = worlds[w].Ownership.At(p.x, p.y);
                    if (!owner.IsOwned)
                    {
                        continue;
                    }

                    Assert.AreEqual(0, owner.MassifIndex, $"seed {Seeds[w]} at {p}");
                    Assert.That(
                        owner.MarginMeters, Is.EqualTo(float.PositiveInfinity),
                        $"seed {Seeds[w]} at {p}: a lone massif reported a runner-up");
                }
            }

            Assert.That(single, Is.GreaterThan(0), "no single-massif world in the seed list");
        }

        // ---------- P1-1: the structural domain ----------

        /// <summary>
        /// The frame is offered inside the axis reach and alongside the axis, and nowhere else.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The whole of P1-1 in one assertion set. Ownership is total over land - that is still true
        /// and still tested - but a coordinate is only produced where it means something, and this
        /// walks the ground around every massif on every real seed to prove the two boundaries are
        /// where the contract says they are.
        /// </para>
        /// <para>
        /// The reach is not a number this stage picked. It is Federer's reach, which
        /// <see cref="MacroAxis"/> computes and Stage 2A certifies against, and it is exactly the
        /// radius inside which the nearest point on a curve is unique.
        /// </para>
        /// </remarks>
        [Test]
        public void TheFrameIsOfferedInsideTheReachAndAlongsideTheAxisAndNowhereElse()
        {
            var structuralWorlds = 0;

            for (var w = 0; w < worlds.Length; w++)
            {
                var world = worlds[w];
                for (var m = 0; m < world.Massifs.Count; m++)
                {
                    var reach = world.Ownership.DomainRadiusMeters(m);
                    var counts = new int[5];

                    foreach (var sample in SweepMassif(world, stage2a[w].Coast, m))
                    {
                        var owner = sample.Owner;
                        counts[(int)owner.Kind]++;
                        var where = $"seed {Seeds[w]} massif {m} at {sample.Point}";

                        switch (owner.Kind)
                        {
                            case MassifOwnerKind.Massif:
                                Assert.That(
                                    owner.AxisDistanceMeters, Is.LessThan(reach),
                                    $"{where}: structural but past the reach");
                                Assert.IsTrue(
                                    owner.TryGetFrame(out var frame), $"{where}: structural, no frame");
                                Assert.That(
                                    frame.S, Is.GreaterThan(0f).And.LessThan(1f),
                                    $"{where}: structural but the arc coordinate is saturated");
                                break;

                            case MassifOwnerKind.BeyondReach:
                                Assert.That(
                                    owner.AxisDistanceMeters, Is.GreaterThanOrEqualTo(reach),
                                    $"{where}: called beyond the reach while inside it");
                                Assert.IsFalse(owner.TryGetFrame(out _), $"{where}: frame past reach");
                                break;

                            case MassifOwnerKind.BeyondAxisEnd:
                                Assert.That(
                                    owner.AxisDistanceMeters, Is.LessThan(reach),
                                    $"{where}: endpoint case should have been the reach case");
                                Assert.IsFalse(owner.TryGetFrame(out _), $"{where}: frame past the end");
                                break;
                        }
                    }

                    Assert.That(
                        counts[(int)MassifOwnerKind.Massif], Is.GreaterThan(0),
                        $"seed {Seeds[w]} massif {m}: the structural domain came out empty");

                    structuralWorlds++;
                }
            }

            Assert.That(structuralWorlds, Is.GreaterThan(0), "no massif in the seed list");
        }

        /// <summary>A frame is handed out if and only if the point is structural.</summary>
        /// <remarks>
        /// The gate itself, over every world including the four that carry no massif. There is no
        /// second accessor and no overload that skips the test, so this is the only way a coordinate
        /// leaves the type.
        /// </remarks>
        [Test]
        public void NoFrameEscapesTheDomainGate()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                foreach (var p in SampleGrid(40))
                {
                    var owner = worlds[w].Ownership.At(p.x, p.y);
                    var got = owner.TryGetFrame(out var frame);

                    Assert.AreEqual(
                        owner.IsStructural, got,
                        $"seed {Seeds[w]} at {p}: {owner.Kind} and frame={got}");

                    if (got)
                    {
                        continue;
                    }

                    Assert.AreEqual(default(MassifFrame), frame, $"seed {Seeds[w]} at {p}");
                }
            }
        }

        /// <summary>
        /// Inside the domain the frame is the true nearest point, not a local minimum.
        /// </summary>
        /// <remarks>
        /// <see cref="MountainMassif.Project"/> searches coarsely and then refines, which it documents
        /// as safe inside the body because the reach exceeds the width. Stage 3A asks it about ground
        /// the body never covers, so the precondition has to be re-established rather than inherited:
        /// this compares it against an exhaustive search over the same smoothed curve.
        /// </remarks>
        [Test]
        public void InsideTheDomainTheFrameIsTheTrueNearestPoint()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                var world = worlds[w];
                for (var m = 0; m < world.Massifs.Count; m++)
                {
                    var massif = world.Massifs[m];
                    var curve = SmoothedCurve(massif);

                    foreach (var sample in SweepMassif(world, stage2a[w].Coast, m))
                    {
                        if (!sample.Owner.TryGetFrame(out var frame))
                        {
                            continue;
                        }

                        ExactProject(curve, sample.Point, out var exactS, out var exactSign);
                        var where = $"seed {Seeds[w]} massif {m} at {sample.Point}";

                        Assert.AreEqual(exactSign, frame.FlankSign, $"{where}: flank side");
                        Assert.That(
                            Mathf.Abs(exactS - frame.S) * massif.LengthMeters,
                            Is.LessThan(1f),
                            $"{where}: the projection landed on a different part of the axis");
                    }
                }
            }
        }

        /// <summary>The arc coordinate is continuous across the domain.</summary>
        /// <remarks>
        /// <para>
        /// Two bounds, both derived rather than chosen. Moving a step along a curve offset by
        /// <c>d</c> changes arc length by at most <c>step / (1 - d / reach)</c>, which is exact for a
        /// curve of curvature at most <c>1 / reach</c> and is what makes the reach the right domain
        /// boundary. The second bound is that same expression evaluated inside the body, where it is
        /// a small number and the assertion is therefore tight.
        /// </para>
        /// <para>
        /// The audit measured jumps of 404 m to 1592 m between samples ten metres apart outside this
        /// domain, on five of the six worlds that carry a massif. Inside it there are none.
        /// </para>
        /// </remarks>
        [Test]
        public void TheArcCoordinateIsContinuousAcrossTheDomain()
        {
            const float step = SweepStepMeters;

            for (var w = 0; w < worlds.Length; w++)
            {
                var world = worlds[w];
                for (var m = 0; m < world.Massifs.Count; m++)
                {
                    var massif = world.Massifs[m];
                    var reach = world.Ownership.DomainRadiusMeters(m);
                    var grid = SweepGrid(world, stage2a[w].Coast, m);

                    for (var zi = 0; zi < grid.Rows; zi++)
                    {
                        for (var xi = 0; xi < grid.Cols; xi++)
                        {
                            Compare(grid, xi, zi, xi + 1, zi);
                            Compare(grid, xi, zi, xi, zi + 1);
                        }
                    }

                    void Compare(SweepResult g, int x0, int z0, int x1, int z1)
                    {
                        if (x1 >= g.Cols || z1 >= g.Rows)
                        {
                            return;
                        }

                        var a = g.Owners[z0 * g.Cols + x0];
                        var b = g.Owners[z1 * g.Cols + x1];
                        if (!a.TryGetFrame(out var frameA) || !b.TryGetFrame(out var frameB))
                        {
                            return;
                        }

                        var jump = Mathf.Abs(frameA.S - frameB.S) * massif.LengthMeters;
                        var far = Mathf.Max(a.AxisDistanceMeters, b.AxisDistanceMeters);
                        var stretch = float.IsPositiveInfinity(reach)
                            ? 1f
                            : 1f / Mathf.Max(1e-3f, 1f - far / reach);

                        // One extra step for the lattice: the two samples are a grid step apart, and
                        // the curve they project onto is itself sampled rather than analytic.
                        var bound = step * stretch + step;

                        Assert.That(
                            jump, Is.LessThanOrEqualTo(bound),
                            $"seed {Seeds[w]} massif {m} at {g.PointAt(x0, z0)}: arc jumped {jump:F0} m "
                            + $"over a {step:F0} m step at {far:F0} m from the axis (reach {reach:F0})");

                        if (far > massif.HalfWidthMeters)
                        {
                            return;
                        }

                        // Inside the body the stretch factor is under three on every world, so this is
                        // a tight assertion rather than a formality.
                        var bodyBound = step * (float.IsPositiveInfinity(reach)
                            ? 1f
                            : 1f / (1f - massif.HalfWidthMeters / reach)) + step;

                        Assert.That(
                            jump, Is.LessThanOrEqualTo(bodyBound),
                            $"seed {Seeds[w]} massif {m} at {g.PointAt(x0, z0)}: arc jumped {jump:F0} m "
                            + "inside the body");
                    }
                }
            }
        }

        /// <summary>The domain contains the body Stage 2B actually raised.</summary>
        /// <remarks>
        /// A domain that excluded part of the massif would be the wrong domain. Stage 2B's widest
        /// flank reaches <c>(2 - Asymmetry)</c> times its effective half-width, and that has to stay
        /// under the reach - which is the reason the boundary is the reach itself and not Stage 2's
        /// narrower width bound, <c>CurvatureWidthFactor * reach</c>, which the widest flank exceeds.
        /// </remarks>
        [Test]
        public void TheDomainContainsTheBodyStage2BRaised()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                var world = worlds[w];
                for (var m = 0; m < world.Massifs.Count; m++)
                {
                    var massif = world.Massifs[m];
                    var effective = Mathf.Min(
                        massif.HalfWidthMeters, massif.ReachLimitedHalfWidthMeters);
                    var widest = effective * (2f - massif.Asymmetry);

                    Assert.That(
                        widest, Is.LessThan(world.Ownership.DomainRadiusMeters(m)),
                        $"seed {Seeds[w]} massif {m}: the body reaches {widest:F0} m but the domain "
                        + $"stops at {world.Ownership.DomainRadiusMeters(m):F0} m");
                }
            }
        }

        /// <summary>Ground past an axis end is refused a flank, and there is a lot of it.</summary>
        /// <remarks>
        /// Both halves matter. The refusal is the contract; the quantity is why the contract is worth
        /// having. Measured over the owned land of the worlds that carry a massif, the endpoint region
        /// runs from a twelfth to nearly half of it and reaches more than a kilometre past the tip,
        /// so a Stage 3B that trusted the flank sign out there would be building on a half-plane.
        /// </remarks>
        [Test]
        public void EndpointGroundIsRefusedAFlank()
        {
            var worldsWithEndpointGround = 0;

            for (var w = 0; w < worlds.Length; w++)
            {
                var world = worlds[w];
                for (var m = 0; m < world.Massifs.Count; m++)
                {
                    var massif = world.Massifs[m];
                    var endpointSamples = 0;

                    foreach (var sample in SweepMassif(world, stage2a[w].Coast, m))
                    {
                        if (sample.Owner.Kind != MassifOwnerKind.BeyondAxisEnd)
                        {
                            continue;
                        }

                        endpointSamples++;
                        Assert.IsFalse(
                            sample.Owner.TryGetFrame(out _),
                            $"seed {Seeds[w]} at {sample.Point}: endpoint ground handed out a flank");

                        // And the projection there really is saturated, which is what makes it an
                        // endpoint rather than merely distant.
                        massif.Project(sample.Point.x, sample.Point.y, out var s, out _);
                        Assert.That(
                            s <= 0f || s >= 1f, Is.True,
                            $"seed {Seeds[w]} at {sample.Point}: refused but the arc is interior");
                    }

                    if (endpointSamples > 0)
                    {
                        worldsWithEndpointGround++;
                    }
                }
            }

            Assert.That(
                worldsWithEndpointGround, Is.GreaterThan(0),
                "no endpoint ground anywhere - the test is not exercising the case it exists for");
        }

        /// <summary>Ground the old contract already answered well answers identically.</summary>
        /// <remarks>
        /// The hardening removed coordinates from places they should never have been offered; it must
        /// not have moved the ones that were right. Inside the body, where the frame was always valid,
        /// it still equals <see cref="MountainMassif.Project"/> exactly - the same call, not a
        /// re-derivation.
        /// </remarks>
        [Test]
        public void HealthyGroundInsideTheBodyIsUnchanged()
        {
            var checkedSamples = 0;

            for (var w = 0; w < worlds.Length; w++)
            {
                var world = worlds[w];
                for (var m = 0; m < world.Massifs.Count; m++)
                {
                    var massif = world.Massifs[m];

                    foreach (var sample in SweepMassif(world, stage2a[w].Coast, m))
                    {
                        if (sample.Owner.AxisDistanceMeters > massif.HalfWidthMeters)
                        {
                            continue;
                        }

                        massif.Project(sample.Point.x, sample.Point.y, out var s, out var offset);
                        if (s <= 0f || s >= 1f)
                        {
                            continue;
                        }

                        var where = $"seed {Seeds[w]} massif {m} at {sample.Point}";
                        Assert.IsTrue(
                            sample.Owner.IsStructural, $"{where}: body ground lost its frame");
                        Assert.IsTrue(sample.Owner.TryGetFrame(out var frame), where);
                        Assert.AreEqual(s, frame.S, $"{where}: s");
                        Assert.AreEqual(offset, frame.OffsetMeters, $"{where}: offset");
                        Assert.AreEqual(
                            MassifOwnership.FlankSignOf(offset), frame.FlankSign, $"{where}: flank");
                        checkedSamples++;
                    }
                }
            }

            Assert.That(checkedSamples, Is.GreaterThan(0), "no body ground was sampled");
        }

        /// <summary>The domain classification repeats exactly.</summary>
        [Test]
        public void TheDomainClassificationIsDeterministic()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                var again = Stage3ASkeleton.Build(
                    Seeds[w], massifs[w], stage2a[w].Coast, RidgeSettings);

                foreach (var p in SampleGrid(40))
                {
                    var a = worlds[w].Ownership.At(p.x, p.y);
                    var b = again.Ownership.At(p.x, p.y);

                    Assert.AreEqual(a.Kind, b.Kind, $"seed {Seeds[w]} at {p}: kind");
                    Assert.AreEqual(
                        a.AxisDistanceMeters, b.AxisDistanceMeters, $"seed {Seeds[w]} at {p}");
                }
            }
        }

        // ---------- P1-2: the attachment interval ----------

        /// <summary>A flank offers Stage 2B's own usable core, forwarded rather than copied.</summary>
        /// <remarks>
        /// One interval in the world. The property reads through to
        /// <see cref="MountainMassif.CoreInterval"/>, so there is no second value that could drift,
        /// and this asserts identity rather than approximate agreement.
        /// </remarks>
        [Test]
        public void AFlankOffersStage2BsCoreIntervalForAttachment()
        {
            var flanksChecked = 0;

            for (var w = 0; w < worlds.Length; w++)
            {
                var world = worlds[w];
                for (var f = 0; f < world.Flanks.Count; f++)
                {
                    var flank = world.Flanks[f];
                    var massif = world.Massifs[flank.MassifIndex];

                    Assert.AreEqual(
                        massif.CoreInterval, flank.AttachmentInterval,
                        $"seed {Seeds[w]} flank {f}: the attachment interval is not CoreInterval");

                    Assert.That(
                        flank.AttachmentInterval.x, Is.GreaterThan(0f),
                        $"seed {Seeds[w]} flank {f}: attachment starts on the axis tip");
                    Assert.That(
                        flank.AttachmentInterval.y, Is.LessThan(1f),
                        $"seed {Seeds[w]} flank {f}: attachment ends on the axis tip");
                    Assert.That(
                        flank.AttachmentInterval.y, Is.GreaterThan(flank.AttachmentInterval.x),
                        $"seed {Seeds[w]} flank {f}: empty attachment interval");

                    flanksChecked++;
                }
            }

            Assert.That(flanksChecked, Is.GreaterThan(0), "no flanks in the seed list");
        }

        /// <summary>
        /// CertifiedCore and the attachment interval are different things, and really do differ.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>CertifiedCore</c> says where the massif has coastal clearance; <c>CoreInterval</c> says
        /// where Stage 2B considers it usable along its axis, ends included. Treating the first as the
        /// second is the mistake P1-2 exists to prevent, and it is not hypothetical: on these seeds
        /// the certified core runs to within a few tens of metres of an axis tip, and on one it starts
        /// on the tip.
        /// </para>
        /// <para>
        /// A regression guard, not a special case - the assertion is that the two differ somewhere and
        /// that where they differ, attachment follows the usable core. Nothing here names a seed.
        /// </para>
        /// </remarks>
        [Test]
        public void TheAttachmentIntervalIsNotTheCertifiedCore()
        {
            var worldsWhereTheyDiffer = 0;
            var certifiedCoreTouchingATip = 0;

            for (var w = 0; w < worlds.Length; w++)
            {
                var world = worlds[w];
                for (var m = 0; m < world.Massifs.Count; m++)
                {
                    var massif = world.Massifs[m];
                    var certified = massif.Axis.CertifiedCore;
                    var attach = massif.CoreInterval;
                    var length = massif.Axis.LengthMeters;

                    if (certified != attach)
                    {
                        worldsWhereTheyDiffer++;
                    }

                    var nearestTip = Mathf.Min(certified.x, 1f - certified.y) * length;
                    if (nearestTip < massif.HalfWidthMeters)
                    {
                        certifiedCoreTouchingATip++;
                    }
                }
            }

            Assert.That(
                worldsWhereTheyDiffer, Is.GreaterThan(0),
                "the two intervals never differed, so this seed set cannot show they are different "
                + "concepts");

            Assert.That(
                certifiedCoreTouchingATip, Is.GreaterThan(0),
                "no certified core came within a half-width of an axis tip, so the seed set no longer "
                + "covers the case the attachment contract exists for");
        }

        /// <summary>Attachment is refused inside CertifiedCore but outside the usable core.</summary>
        /// <remarks>
        /// The enforcement. Positions in that gap are exactly the ones a Stage 3B reading
        /// <c>CertifiedCore</c> would have accepted, and <see cref="MassifFlank.CanAttachAt"/> turns
        /// every one of them down.
        /// </remarks>
        [Test]
        public void AttachmentIsRefusedWhereTheCertifiedCoreOverhangsTheUsableCore()
        {
            var refusals = 0;

            for (var w = 0; w < worlds.Length; w++)
            {
                var world = worlds[w];
                for (var f = 0; f < world.Flanks.Count; f++)
                {
                    var flank = world.Flanks[f];
                    var massif = world.Massifs[flank.MassifIndex];
                    var certified = massif.Axis.CertifiedCore;
                    var attach = flank.AttachmentInterval;

                    for (var k = 0; k <= 200; k++)
                    {
                        var s = Mathf.Lerp(certified.x, certified.y, k / 200f);
                        var insideUsable = s >= attach.x && s <= attach.y;

                        Assert.AreEqual(
                            insideUsable, flank.CanAttachAt(s),
                            $"seed {Seeds[w]} flank {f}: s={s:F3} is inside the certified core, "
                            + $"usable={insideUsable}, but CanAttachAt said {flank.CanAttachAt(s)}");

                        if (!insideUsable)
                        {
                            refusals++;
                        }
                    }
                }
            }

            Assert.That(
                refusals, Is.GreaterThan(0),
                "no position inside a certified core fell outside the usable core, so this test "
                + "proved nothing");
        }

        /// <summary>Every position the attachment interval allows resolves to a real flank.</summary>
        /// <remarks>
        /// The two contracts meeting. P1-2 says where structure may attach; P1-1 says where a flank
        /// coordinate exists. An attachment position that landed in
        /// <see cref="MassifOwnerKind.BeyondAxisEnd"/> would mean the two disagree, which is precisely
        /// what attaching on <c>CertifiedCore</c> would have produced.
        /// </remarks>
        [Test]
        public void EveryAllowedAttachmentPositionResolvesToARealFlank()
        {
            const float probeMeters = 5f;
            var probes = 0;

            for (var w = 0; w < worlds.Length; w++)
            {
                var world = worlds[w];
                for (var f = 0; f < world.Flanks.Count; f++)
                {
                    var flank = world.Flanks[f];
                    var massif = world.Massifs[flank.MassifIndex];
                    var attach = flank.AttachmentInterval;

                    for (var k = 0; k <= 40; k++)
                    {
                        var s = Mathf.Lerp(attach.x, attach.y, k / 40f);
                        Assert.IsTrue(flank.CanAttachAt(s), $"seed {Seeds[w]} flank {f}: s={s:F3}");

                        var here = massif.PointAt(s);
                        var probe = here + AxisNormal(massif, s) * (probeMeters * flank.Sign);
                        var owner = world.Ownership.At(probe.x, probe.y);

                        // Only where the probe is on land: an axis close to the shore can put five
                        // metres of its own flank in the water, which is Stage 1's business.
                        if (owner.Kind == MassifOwnerKind.Ocean)
                        {
                            continue;
                        }

                        var where = $"seed {Seeds[w]} flank {f} at s={s:F3}";
                        Assert.AreEqual(
                            MassifOwnerKind.Massif, owner.Kind,
                            $"{where}: an allowed attachment sits on {owner.Kind} ground");
                        Assert.IsTrue(owner.TryGetFrame(out var frame), where);
                        Assert.AreEqual(flank.MassifIndex, frame.MassifIndex, $"{where}: owner");
                        Assert.AreEqual(flank.Sign, frame.FlankSign, $"{where}: flank side");
                        Assert.That(
                            Mathf.Abs(frame.S - s) * massif.LengthMeters, Is.LessThan(probeMeters * 2f),
                            $"{where}: the probe projected somewhere else along the axis");
                        probes++;
                    }
                }
            }

            Assert.That(probes, Is.GreaterThan(0), "no attachment position was probed");
        }

        /// <summary>Stage 2B still computes the interval this contract forwards.</summary>
        /// <remarks>
        /// Nothing in this pass may move a Stage 2B number. The ramp fraction that produces
        /// <see cref="MountainMassif.CoreInterval"/> is pinned here, and the massif is rebuilt from
        /// the same inputs to prove the interval itself is unchanged and is Stage 2B's, not a copy
        /// taken at some other moment.
        /// </remarks>
        [Test]
        public void Stage2BStillOwnsTheCoreIntervalAndItsNumbers()
        {
            Assert.AreEqual(
                0.18f, MassifSettings.Default.MinRampFraction, 1e-6f,
                "MinRampFraction moved - the attachment contract is built on Stage 2B's ramp rule");

            for (var w = 0; w < worlds.Length; w++)
            {
                var world = worlds[w];
                var accepted = stage2a[w].AcceptedAxes();

                for (var m = 0; m < world.Massifs.Count; m++)
                {
                    var rebuilt = MountainMassif.Build(
                        new Seed.WorldSeed(Seeds[w]), m, accepted[m], TerrainSettings, MassifSettings,
                        accepted[m].CertifiedCore);

                    Assert.AreEqual(
                        world.Massifs[m].CoreInterval, rebuilt.CoreInterval,
                        $"seed {Seeds[w]} massif {m}: CoreInterval is not reproducible");
                    Assert.AreEqual(
                        world.Massifs[m].Axis.CertifiedCore, rebuilt.Axis.CertifiedCore,
                        $"seed {Seeds[w]} massif {m}: CertifiedCore moved");
                    Assert.AreEqual(
                        world.Flanks[m * 2].AttachmentInterval, rebuilt.CoreInterval,
                        $"seed {Seeds[w]} massif {m}: the flank stopped forwarding CoreInterval");
                }
            }
        }

        // ---------- sweeps ----------

        private const float SweepStepMeters = 20f;

        private readonly struct SweepSample
        {
            public Vector2 Point { get; }
            public MassifOwner Owner { get; }

            public SweepSample(Vector2 point, MassifOwner owner)
            {
                Point = point;
                Owner = owner;
            }
        }

        private sealed class SweepResult
        {
            public int Cols;
            public int Rows;
            public float OriginX;
            public float OriginZ;
            public MassifOwner[] Owners;

            public Vector2 PointAt(int x, int z) =>
                new Vector2(OriginX + x * SweepStepMeters, OriginZ + z * SweepStepMeters);
        }

        /// <summary>
        /// Sweeps the ground a massif could possibly own, at <see cref="SweepStepMeters"/>.
        /// </summary>
        /// <remarks>
        /// The axis bounds inflated by its reach, which is where the domain boundary is and therefore
        /// where the samples are worth spending. Points owned by another massif are skipped, so what
        /// comes back is this massif's territory and nothing else.
        /// </remarks>
        private static SweepResult SweepGrid(Stage3ASkeleton world, CoastQuery coast, int massifIndex)
        {
            var reach = world.Ownership.DomainRadiusMeters(massifIndex);
            var pad = float.IsPositiveInfinity(reach)
                ? WorldScale.WorldExtentXMeters
                : reach + 200f;

            var bounds = world.Massifs[massifIndex].Axis.Bounds(pad);
            var minX = Mathf.Max(0f, bounds.xMin);
            var minZ = Mathf.Max(0f, bounds.yMin);
            var maxX = Mathf.Min(WorldScale.WorldExtentXMeters, bounds.xMax);
            var maxZ = Mathf.Min(WorldScale.WorldExtentZMeters, bounds.yMax);

            var result = new SweepResult
            {
                OriginX = minX,
                OriginZ = minZ,
                Cols = Mathf.Max(1, Mathf.FloorToInt((maxX - minX) / SweepStepMeters) + 1),
                Rows = Mathf.Max(1, Mathf.FloorToInt((maxZ - minZ) / SweepStepMeters) + 1),
            };

            result.Owners = new MassifOwner[result.Cols * result.Rows];

            var crossings = new System.Collections.Generic.List<float>(16);
            for (var z = 0; z < result.Rows; z++)
            {
                var worldZ = minZ + z * SweepStepMeters;
                coast.RowCrossings(worldZ, crossings);

                for (var x = 0; x < result.Cols; x++)
                {
                    var worldX = minX + x * SweepStepMeters;
                    var land = CoastQuery.InsideFromSortedCrossings(crossings, worldX);
                    var owner = world.Ownership.At(worldX, worldZ, land);

                    result.Owners[z * result.Cols + x] =
                        owner.MassifIndex == massifIndex
                            ? owner
                            : MassifOwner.Unowned(MassifOwnerKind.Ocean);
                }
            }

            return result;
        }

        private static IEnumerable<SweepSample> SweepMassif(
            Stage3ASkeleton world, CoastQuery coast, int massifIndex)
        {
            var grid = SweepGrid(world, coast, massifIndex);
            for (var z = 0; z < grid.Rows; z++)
            {
                for (var x = 0; x < grid.Cols; x++)
                {
                    var owner = grid.Owners[z * grid.Cols + x];
                    if (owner.MassifIndex != massifIndex)
                    {
                        continue;
                    }

                    yield return new SweepSample(grid.PointAt(x, z), owner);
                }
            }
        }

        /// <summary>The curve Stage 2B projects onto, rebuilt the way its constructor builds it.</summary>
        private static Vector2[] SmoothedCurve(MountainMassif massif)
        {
            var axis = massif.Axis;
            return Town.Geometry.Geo2.SmoothCatmullRom(
                    axis.Points,
                    0f,
                    axis.Points.Length > 2 ? MountainMassif.ProjectionSubdivisions : 1)
                .ToArray();
        }

        /// <summary>Exhaustive nearest point on a polyline: what a coarse search should agree with.</summary>
        private static void ExactProject(Vector2[] curve, Vector2 p, out float s, out int sign)
        {
            var bestSq = float.PositiveInfinity;
            var bestSegment = 0;
            var bestT = 0f;
            var bestPoint = curve[0];

            for (var i = 0; i + 1 < curve.Length; i++)
            {
                var a = curve[i];
                var b = curve[i + 1];
                var ab = b - a;
                var lengthSq = ab.sqrMagnitude;
                var t = lengthSq < 1e-8f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq);
                var q = a + ab * t;
                var sq = (p - q).sqrMagnitude;
                if (sq >= bestSq)
                {
                    continue;
                }

                bestSq = sq;
                bestSegment = i;
                bestT = t;
                bestPoint = q;
            }

            var total = 0f;
            var upTo = 0f;
            for (var i = 0; i + 1 < curve.Length; i++)
            {
                var length = (curve[i + 1] - curve[i]).magnitude;
                if (i < bestSegment)
                {
                    upTo += length;
                }
                else if (i == bestSegment)
                {
                    upTo += length * bestT;
                }

                total += length;
            }

            s = total > 1e-4f ? Mathf.Clamp01(upTo / total) : 0f;

            var ahead = curve[Mathf.Min(curve.Length - 1, bestSegment + 1)];
            var behind = curve[Mathf.Max(0, bestSegment)];
            var tangent = (ahead - behind).normalized;
            var toPoint = p - bestPoint;
            sign = tangent.x * toPoint.y - tangent.y * toPoint.x >= 0f ? 1 : -1;
        }

        // ---------- fixtures ----------

        private static List<MountainMassif> RaiseMassifs(int seed, Stage2AResult stage2a)
        {
            var worldSeed = new Seed.WorldSeed(seed);
            var built = new List<MountainMassif>();
            var accepted = stage2a.AcceptedAxes();
            for (var i = 0; i < accepted.Count; i++)
            {
                built.Add(MountainMassif.Build(
                    worldSeed, built.Count, accepted[i], TerrainSettings, MassifSettings,
                    accepted[i].CertifiedCore));
            }

            return built;
        }

        /// <summary>A square island, well clear of the world edge.</summary>
        private static CoastQuery SquareCoast()
        {
            var ring = new[]
            {
                new Vector2(400f, 400f),
                new Vector2(2400f, 400f),
                new Vector2(2400f, 2400f),
                new Vector2(400f, 2400f),
            };

            return new CoastQuery(new List<Vector2[]> { ring }, 1000f);
        }

        /// <summary>Two parallel axes 600 m apart: the medial line is the level z = 1400.</summary>
        private static Stage3ASkeleton TwoMassifWorld() =>
            Stage3ASkeleton.Build(
                0,
                new[] { StraightMassif(0, 1100f), StraightMassif(1, 1700f) },
                SquareCoast(),
                RidgeSettings);

        private static Stage3ASkeleton ThreeMassifWorld() =>
            Stage3ASkeleton.Build(
                0,
                new[]
                {
                    ShortMassif(0, new Vector2(1000f, 1000f)),
                    ShortMassif(1, new Vector2(1800f, 1000f)),
                    ShortMassif(2, new Vector2(1400f, 1700f)),
                },
                SquareCoast(),
                RidgeSettings);

        private static MountainMassif StraightMassif(int index, float z) =>
            Massif(index, Segment(new Vector2(800f, z), new Vector2(1700f, z)));

        private static MountainMassif ShortMassif(int index, Vector2 centre) =>
            Massif(index, Segment(centre - new Vector2(50f, 0f), centre + new Vector2(50f, 0f)));

        private static MountainMassif Massif(int index, Vector2[] points)
        {
            var axis = new MacroAxis(
                points,
                new[] { points[0], points[points.Length - 1] },
                new[] { 0, 1 },
                float.PositiveInfinity,
                0.5f,
                250f);

            return new MountainMassif(
                index,
                axis,
                new ProfileCurve(new[] { 0f, 0.5f, 1f }, new[] { 0f, 1f, 0f }),
                300f,
                250f,
                0.85f,
                1,
                new[] { 0.5f },
                MassifSettings,
                new Vector2(0.2f, 0.8f));
        }

        private static Vector2[] Segment(Vector2 from, Vector2 to)
        {
            var steps = Mathf.Max(2, Mathf.RoundToInt((to - from).magnitude / 20f));
            var points = new Vector2[steps + 1];
            for (var i = 0; i <= steps; i++)
            {
                points[i] = Vector2.Lerp(from, to, i / (float)steps);
            }

            return points;
        }

        /// <summary>Unit normal of the massif's projection curve at a normalised arc position.</summary>
        private static Vector2 AxisNormal(MountainMassif massif, float s)
        {
            var ds = 0.005f;
            var ahead = massif.PointAt(Mathf.Min(1f, s + ds));
            var behind = massif.PointAt(Mathf.Max(0f, s - ds));
            var tangent = ahead - behind;
            if (tangent.sqrMagnitude < 1e-8f)
            {
                return Vector2.up;
            }

            tangent.Normalize();
            return new Vector2(-tangent.y, tangent.x);
        }

        /// <summary>A regular grid over the world, excluding the very edge.</summary>
        private static IEnumerable<Vector2> SampleGrid(int steps)
        {
            for (var z = 1; z < steps; z++)
            {
                for (var x = 1; x < steps; x++)
                {
                    yield return new Vector2(
                        WorldScale.WorldExtentXMeters * (x / (float)steps),
                        WorldScale.WorldExtentZMeters * (z / (float)steps));
                }
            }
        }
    }
}
