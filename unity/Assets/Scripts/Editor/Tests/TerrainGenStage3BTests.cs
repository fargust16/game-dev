using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.TerrainGen.Tests
{
    /// <summary>
    /// Stage 3B: the primary spur skeleton and the valleys it leaves between the spurs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These assert topology, not appearance. A spur skeleton can be numerically perfect and still
    /// look like a comb, which is what the diagnostics are for; what tests can prove is that nothing
    /// crosses, nothing escapes the domain Stage 3A hardened, every attachment sits where Stage 2B
    /// says a massif is usable, and the valleys are the exact complement of the spurs.
    /// </para>
    /// <para>
    /// Built on Stage 2A's cheap path, so no seed costs more than a fifth of a second: Stage 3B
    /// consumes axes and a coastline, never a height field.
    /// </para>
    /// </remarks>
    public sealed class TerrainGenStage3BTests
    {
        private static readonly int[] Seeds =
        {
            110201, 424242, 20260905, 7, 101010, 20261231, 1337,
        };

        private static TerrainGenSettings TerrainSettings => TerrainGenSettings.Default;
        private static MacroStructureSettings MacroSettings => MacroStructureSettings.Default;
        private static MassifSettings MassifSettings => MassifSettings.Default;
        private static RidgeSettings RidgeSettings => RidgeSettings.Default;

        private Stage2AResult[] stage2a;
        private Stage3ASkeleton[] partitions;
        private RidgeSkeleton[] worlds;

        [OneTimeSetUp]
        public void BuildWorlds()
        {
            stage2a = new Stage2AResult[Seeds.Length];
            partitions = new Stage3ASkeleton[Seeds.Length];
            worlds = new RidgeSkeleton[Seeds.Length];

            for (var i = 0; i < Seeds.Length; i++)
            {
                stage2a[i] = Stage2AMacroStructure.Run(Seeds[i], TerrainSettings, MacroSettings);
                partitions[i] = Partition(Seeds[i], stage2a[i]);
                worlds[i] = RidgeSkeleton.Build(
                    partitions[i], RidgeSettings, new Seed.WorldSeed(Seeds[i]));
            }
        }

        // ---------- determinism ----------

        /// <summary>The same seed produces the same skeleton, point for point.</summary>
        [Test]
        public void TheSameSeedProducesTheSameSkeleton()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                var again = RidgeSkeleton.Build(
                    partitions[w], RidgeSettings, new Seed.WorldSeed(Seeds[w]));

                // Everything the stats report except how long the build took, which is a measurement
                // of this machine rather than of the world.
                Assert.AreEqual(
                    WithoutTiming(worlds[w].Stats.ToLine()), WithoutTiming(again.Stats.ToLine()),
                    $"seed {Seeds[w]}");
                Assert.AreEqual(
                    worlds[w].Spurs.Count, again.Spurs.Count, $"seed {Seeds[w]}: spur count");

                for (var i = 0; i < worlds[w].Spurs.Count; i++)
                {
                    var a = worlds[w].Spurs[i];
                    var b = again.Spurs[i];

                    Assert.AreEqual(a.MassifIndex, b.MassifIndex, $"seed {Seeds[w]} spur {i}: massif");
                    Assert.AreEqual(a.FlankSign, b.FlankSign, $"seed {Seeds[w]} spur {i}: flank");
                    Assert.AreEqual(a.AttachS, b.AttachS, $"seed {Seeds[w]} spur {i}: attachment");
                    Assert.AreEqual(
                        a.StripInterval, b.StripInterval, $"seed {Seeds[w]} spur {i}: strip");
                    Assert.AreEqual(
                        a.Points.Length, b.Points.Length, $"seed {Seeds[w]} spur {i}: samples");

                    for (var k = 0; k < a.Points.Length; k++)
                    {
                        Assert.AreEqual(a.Points[k], b.Points[k], $"seed {Seeds[w]} spur {i} pt {k}");
                    }
                }

                Assert.AreEqual(
                    worlds[w].Valleys.Count, again.Valleys.Count, $"seed {Seeds[w]}: valley count");

                for (var i = 0; i < worlds[w].Valleys.Count; i++)
                {
                    Assert.AreEqual(
                        worlds[w].Valleys[i].SInterval, again.Valleys[i].SInterval,
                        $"seed {Seeds[w]} valley {i}");
                }
            }
        }

        /// <summary>Different seeds produce different skeletons.</summary>
        [Test]
        public void DifferentSeedsProduceDifferentSkeletons()
        {
            var withSpurs = new List<int>();
            for (var w = 0; w < worlds.Length; w++)
            {
                if (worlds[w].Spurs.Count > 0)
                {
                    withSpurs.Add(w);
                }
            }

            Assert.That(withSpurs.Count, Is.GreaterThan(1), "not enough worlds carry spurs");

            for (var i = 1; i < withSpurs.Count; i++)
            {
                var a = worlds[withSpurs[0]];
                var b = worlds[withSpurs[i]];
                Assert.AreNotEqual(
                    a.Spurs[0].Origin, b.Spurs[0].Origin,
                    $"seeds {Seeds[withSpurs[0]]} and {Seeds[withSpurs[i]]} share a spur origin");
            }
        }

        /// <summary>The two seed streams and the two flanks stay independent of each other.</summary>
        /// <remarks>
        /// Stations and shape are forked separately, and each is forked again per massif and per
        /// flank. If the fork keys were not doing their job the two flanks of a massif would carry the
        /// same station pattern, which is both a determinism defect and a visible one.
        /// </remarks>
        [Test]
        public void EachFlankDrawsFromItsOwnStream()
        {
            var compared = 0;

            for (var w = 0; w < worlds.Length; w++)
            {
                var world = worlds[w];
                for (var m = 0; m < world.Ownership.Massifs.Count; m++)
                {
                    var negative = world.SpursOn(m, -1);
                    var positive = world.SpursOn(m, +1);
                    if (negative.Count == 0 || positive.Count == 0)
                    {
                        continue;
                    }

                    // Different counts already prove the layouts are not the same one twice; equal
                    // counts have to be told apart by the stations themselves.
                    var identical = negative.Count == positive.Count;
                    if (identical)
                    {
                        for (var i = 0; i < negative.Count; i++)
                        {
                            if (Mathf.Abs(negative[i].AttachS - positive[i].AttachS) > 1e-6f)
                            {
                                identical = false;
                                break;
                            }
                        }
                    }

                    Assert.IsFalse(
                        identical,
                        $"seed {Seeds[w]} massif {m}: both flanks drew the same stations");
                    compared++;
                }
            }

            Assert.That(compared, Is.GreaterThan(0), "no flank pair was comparable");
        }

        // ---------- attachment ----------

        /// <summary>Every attachment lies inside the flank's attachment interval.</summary>
        [Test]
        public void EveryAttachmentLiesInsideTheAttachmentInterval()
        {
            var checkedSpurs = 0;

            for (var w = 0; w < worlds.Length; w++)
            {
                var world = worlds[w];
                for (var i = 0; i < world.Spurs.Count; i++)
                {
                    var spur = world.Spurs[i];
                    var flank = FlankOf(world, spur);

                    Assert.IsTrue(
                        flank.CanAttachAt(spur.AttachS),
                        $"seed {Seeds[w]} spur {i}: s={spur.AttachS:F4} is outside "
                        + $"[{flank.AttachmentInterval.x:F4},{flank.AttachmentInterval.y:F4}]");

                    checkedSpurs++;
                }
            }

            Assert.That(checkedSpurs, Is.GreaterThan(0), "no spurs to check");
        }

        /// <summary>
        /// No attachment sits in the overhang where CertifiedCore extends past the usable core.
        /// </summary>
        /// <remarks>
        /// The Stage 3A hardening exists because that overhang reaches to 20 m of an axis tip on seed 7
        /// and starts on the tip on 20261231. This proves Stage 3B never puts a spur there - and that
        /// the overhang is real on this seed set, so the assertion is not vacuous.
        /// </remarks>
        [Test]
        public void NoAttachmentSitsInTheCertifiedCoreOverhang()
        {
            var overhangWorlds = 0;

            for (var w = 0; w < worlds.Length; w++)
            {
                var world = worlds[w];
                for (var m = 0; m < world.Ownership.Massifs.Count; m++)
                {
                    var massif = world.Ownership.Massifs[m];
                    var certified = massif.Axis.CertifiedCore;
                    var usable = massif.CoreInterval;

                    if (certified.x < usable.x - 1e-5f || certified.y > usable.y + 1e-5f)
                    {
                        overhangWorlds++;
                    }

                    foreach (var spur in world.SpursOn(m, -1))
                    {
                        AssertOutsideOverhang(spur, certified, usable, Seeds[w]);
                    }

                    foreach (var spur in world.SpursOn(m, +1))
                    {
                        AssertOutsideOverhang(spur, certified, usable, Seeds[w]);
                    }
                }
            }

            Assert.That(
                overhangWorlds, Is.GreaterThan(0),
                "no world in the seed set has a certified core that overhangs its usable core, so "
                + "this test cannot show the distinction is honoured");
        }

        private static void AssertOutsideOverhang(
            SpurCurve spur, Vector2 certified, Vector2 usable, int seed)
        {
            var inCertified = spur.AttachS >= certified.x && spur.AttachS <= certified.y;
            var inUsable = spur.AttachS >= usable.x && spur.AttachS <= usable.y;

            Assert.IsTrue(
                inUsable,
                $"seed {seed} spur {spur.Id}: attached at {spur.AttachS:F4}, inside the certified "
                + $"core={inCertified}, but outside the usable core "
                + $"[{usable.x:F4},{usable.y:F4}]");
        }

        // ---------- domain ----------

        /// <summary>
        /// Every point of every spur is a structural coordinate of its own massif and flank.
        /// </summary>
        /// <remarks>
        /// One sweep covering six of the brief's rules at once: no spur in the ocean, none on
        /// unclaimed land, none beyond the reach, none past an axis end, none across the inter-massif
        /// divide, and none on the wrong flank. They are one assertion because Stage 3A made them one
        /// question.
        /// </remarks>
        [Test]
        public void EverySpurSampleIsStructuralGroundOfItsOwnFlank()
        {
            var samples = 0;

            for (var w = 0; w < worlds.Length; w++)
            {
                var world = worlds[w];
                var ownership = world.Ownership.Ownership;

                for (var i = 0; i < world.Spurs.Count; i++)
                {
                    var spur = world.Spurs[i];

                    // Every sample, and the midpoint of every segment, so a spur cannot step over a
                    // thin band of forbidden ground between two legal samples.
                    for (var k = 0; k < spur.Points.Length; k++)
                    {
                        Check(spur, spur.Points[k], k, Seeds[w], ownership);
                        samples++;

                        if (k + 1 >= spur.Points.Length)
                        {
                            continue;
                        }

                        Check(spur, (spur.Points[k] + spur.Points[k + 1]) * 0.5f, k, Seeds[w],
                            ownership);
                        samples++;
                    }
                }
            }

            Assert.That(samples, Is.GreaterThan(0), "no spur samples");

            void Check(SpurCurve spur, Vector2 p, int k, int seed, MassifOwnership ownership)
            {
                var owner = ownership.At(p.x, p.y);
                var where = $"seed {seed} spur {spur.Id} pt {k} at {p}";

                Assert.AreEqual(
                    MassifOwnerKind.Massif, owner.Kind,
                    $"{where}: spur geometry on {owner.Kind} ground");
                Assert.AreEqual(spur.MassifIndex, owner.MassifIndex, $"{where}: crossed to another massif");
                Assert.IsTrue(owner.TryGetFrame(out var frame), $"{where}: no frame");

                // On the axis the sign is a tie-break rather than a side, so only off it does the
                // flank have to match.
                if (Mathf.Abs(frame.OffsetMeters) > 1f)
                {
                    Assert.AreEqual(spur.FlankSign, frame.FlankSign, $"{where}: wrong flank");
                }
            }
        }

        /// <summary>Spurs on opposite flanks stay on their own side of the crest.</summary>
        [Test]
        public void OppositeFlanksStayOnTheirOwnSides()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                var world = worlds[w];
                for (var i = 0; i < world.Spurs.Count; i++)
                {
                    var spur = world.Spurs[i];
                    var massif = world.Ownership.Massifs[spur.MassifIndex];

                    for (var k = 1; k < spur.Points.Length; k++)
                    {
                        massif.Project(spur.Points[k].x, spur.Points[k].y, out _, out var offset);
                        if (Mathf.Abs(offset) <= 1f)
                        {
                            continue;
                        }

                        Assert.AreEqual(
                            spur.FlankSign, MassifOwnership.FlankSignOf(offset),
                            $"seed {Seeds[w]} spur {i} pt {k}: offset {offset:F1} m is on the other "
                            + "side of the crest");
                    }
                }
            }
        }

        // ---------- topology ----------

        /// <summary>No spur crosses itself and no two spurs cross.</summary>
        [Test]
        public void NoSpurCrossesItselfOrAnother()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                var world = worlds[w];

                Assert.AreEqual(
                    0, world.Stats.SelfIntersectingSpurs, $"seed {Seeds[w]}: a spur crosses itself");
                Assert.AreEqual(
                    0, world.Stats.CrossingSpurPairs, $"seed {Seeds[w]}: two spurs cross");

                for (var i = 0; i < world.Spurs.Count; i++)
                {
                    Assert.IsFalse(
                        world.Spurs[i].SelfIntersects(), $"seed {Seeds[w]} spur {i} self-intersects");

                    for (var j = i + 1; j < world.Spurs.Count; j++)
                    {
                        Assert.IsFalse(
                            world.Spurs[i].Crosses(world.Spurs[j]),
                            $"seed {Seeds[w]}: spurs {i} and {j} cross");
                    }
                }
            }
        }

        /// <summary>
        /// The strips of one flank partition the attachment interval, in order and without overlap.
        /// </summary>
        /// <remarks>
        /// The construction that makes crossing impossible rather than unlikely. If the strips ever
        /// overlapped, two spurs would have somewhere to meet.
        /// </remarks>
        [Test]
        public void TheStripsOfAFlankPartitionTheAttachmentInterval()
        {
            var flanksChecked = 0;

            for (var w = 0; w < worlds.Length; w++)
            {
                var world = worlds[w];
                for (var m = 0; m < world.Ownership.Massifs.Count; m++)
                {
                    foreach (var sign in new[] { -1, 1 })
                    {
                        var spurs = world.SpursOn(m, sign);
                        if (spurs.Count == 0)
                        {
                            continue;
                        }

                        var interval = world.Ownership.Massifs[m].CoreInterval;

                        Assert.AreEqual(
                            interval.x, spurs[0].StripInterval.x, 1e-4f,
                            $"seed {Seeds[w]} massif {m} flank {sign}: first strip");
                        Assert.AreEqual(
                            interval.y, spurs[spurs.Count - 1].StripInterval.y, 1e-4f,
                            $"seed {Seeds[w]} massif {m} flank {sign}: last strip");

                        for (var i = 0; i < spurs.Count; i++)
                        {
                            var strip = spurs[i].StripInterval;

                            Assert.That(
                                strip.y, Is.GreaterThan(strip.x),
                                $"seed {Seeds[w]} spur {spurs[i].Id}: empty strip");
                            Assert.That(
                                spurs[i].AttachS,
                                Is.GreaterThanOrEqualTo(strip.x).And.LessThanOrEqualTo(strip.y),
                                $"seed {Seeds[w]} spur {spurs[i].Id}: attached outside its own strip");

                            if (i == 0)
                            {
                                continue;
                            }

                            Assert.AreEqual(
                                spurs[i - 1].StripInterval.y, strip.x, 1e-4f,
                                $"seed {Seeds[w]} massif {m} flank {sign}: strips {i - 1} and {i} "
                                + "do not meet");
                            Assert.That(
                                spurs[i].AttachS, Is.GreaterThan(spurs[i - 1].AttachS),
                                $"seed {Seeds[w]} massif {m} flank {sign}: stations out of order");
                        }

                        flanksChecked++;
                    }
                }
            }

            Assert.That(flanksChecked, Is.GreaterThan(0), "no flank carried spurs");
        }

        /// <summary>Every spur starts on the crest and is a connected polyline.</summary>
        /// <remarks>
        /// The "no isolated fragment" rule. A spur that began part-way down a flank would be a rib
        /// floating in the middle of a slope, which is the same defect as a headless valley.
        /// </remarks>
        [Test]
        public void EverySpurStartsOnTheCrestAndIsConnected()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                var world = worlds[w];
                for (var i = 0; i < world.Spurs.Count; i++)
                {
                    var spur = world.Spurs[i];
                    var massif = world.Ownership.Massifs[spur.MassifIndex];

                    Assert.That(
                        spur.Points.Length, Is.GreaterThanOrEqualTo(2),
                        $"seed {Seeds[w]} spur {i}: fewer than two samples");

                    var toAxis = massif.Axis.DistanceTo(spur.Origin.x, spur.Origin.y);
                    Assert.That(
                        toAxis, Is.LessThan(RidgeSettings.SpurSampleSpacingMeters),
                        $"seed {Seeds[w]} spur {i}: starts {toAxis:F0} m off the crest");

                    Assert.That(
                        spur.LengthMeters, Is.GreaterThan(RidgeSettings.SpurSampleSpacingMeters),
                        $"seed {Seeds[w]} spur {i}: a fragment, not a spur");

                    for (var k = 0; k + 1 < spur.Points.Length; k++)
                    {
                        var step = (spur.Points[k + 1] - spur.Points[k]).magnitude;
                        Assert.That(
                            step, Is.GreaterThan(0f).And.LessThanOrEqualTo(
                                RidgeSettings.SpurSampleSpacingMeters * 2f),
                            $"seed {Seeds[w]} spur {i}: {step:F0} m gap between samples {k} and {k + 1}");
                    }

                    // The toe is where a boundary or the intended reach put it, and both are real.
                    Assert.That(
                        (spur.Toe - spur.Origin).magnitude, Is.GreaterThan(0f),
                        $"seed {Seeds[w]} spur {i}: toe on the origin");
                }
            }
        }

        // ---------- valleys ----------

        /// <summary>Valleys are the complement of the spurs: one more, per flank, always.</summary>
        [Test]
        public void ValleyCountFollowsSpurTopology()
        {
            var flanksChecked = 0;

            for (var w = 0; w < worlds.Length; w++)
            {
                var world = worlds[w];
                for (var m = 0; m < world.Ownership.Massifs.Count; m++)
                {
                    foreach (var sign in new[] { -1, 1 })
                    {
                        var spurs = world.SpursOn(m, sign);
                        var valleys = world.ValleysOn(m, sign);

                        if (spurs.Count == 0)
                        {
                            Assert.AreEqual(
                                0, valleys.Count,
                                $"seed {Seeds[w]} massif {m} flank {sign}: valleys without spurs");
                            continue;
                        }

                        Assert.AreEqual(
                            spurs.Count + 1, valleys.Count,
                            $"seed {Seeds[w]} massif {m} flank {sign}: {spurs.Count} spurs but "
                            + $"{valleys.Count} valleys");

                        var interval = world.Ownership.Massifs[m].CoreInterval;
                        Assert.AreEqual(
                            interval.x, valleys[0].SInterval.x, 1e-4f, $"seed {Seeds[w]}: first valley");
                        Assert.AreEqual(
                            interval.y, valleys[valleys.Count - 1].SInterval.y, 1e-4f,
                            $"seed {Seeds[w]}: last valley");

                        for (var i = 0; i < valleys.Count; i++)
                        {
                            var valley = valleys[i];

                            Assert.That(
                                valley.SWidth, Is.GreaterThan(0f),
                                $"seed {Seeds[w]} valley {valley.Id}: zero width");

                            // Its head is on the crest between the two spurs that bound it, which is
                            // the "valleys need headwaters" rule holding by arithmetic.
                            if (valley.LowerSpurId >= 0)
                            {
                                Assert.That(
                                    valley.HeadS,
                                    Is.GreaterThan(world.Spurs[valley.LowerSpurId].AttachS),
                                    $"seed {Seeds[w]} valley {valley.Id}: head below its lower spur");
                            }

                            if (valley.UpperSpurId >= 0)
                            {
                                Assert.That(
                                    valley.HeadS,
                                    Is.LessThan(world.Spurs[valley.UpperSpurId].AttachS),
                                    $"seed {Seeds[w]} valley {valley.Id}: head above its upper spur");
                            }

                            if (i > 0)
                            {
                                Assert.AreEqual(
                                    valleys[i - 1].SInterval.y, valley.SInterval.x, 1e-4f,
                                    $"seed {Seeds[w]}: valleys {i - 1} and {i} do not meet");
                            }
                        }

                        flanksChecked++;
                    }
                }
            }

            Assert.That(flanksChecked, Is.GreaterThan(0), "no flank carried spurs");
        }

        /// <summary>A world with no massif carries no spurs and no valleys, and is still valid.</summary>
        [Test]
        public void AWorldWithoutAMassifCarriesNoSpurs()
        {
            var barren = 0;

            for (var w = 0; w < worlds.Length; w++)
            {
                if (worlds[w].Ownership.Massifs.Count > 0)
                {
                    continue;
                }

                barren++;
                Assert.AreEqual(0, worlds[w].Spurs.Count, $"seed {Seeds[w]}: spurs without a massif");
                Assert.AreEqual(0, worlds[w].Valleys.Count, $"seed {Seeds[w]}: valleys without a massif");
                Assert.AreEqual(0, worlds[w].Stats.StationsPlaced, $"seed {Seeds[w]}: stations placed");
            }

            Assert.That(barren, Is.GreaterThan(0), "the seed set no longer contains a world without a massif");
        }

        // ---------- the multi-massif path ----------

        /// <summary>Two massifs build independent spur systems and neither crosses the divide.</summary>
        /// <remarks>
        /// Unreachable from a seed - Stage 2A caps a world at two systems and never produced two - so
        /// the fixture is the only way the inter-massif rule is exercised at all.
        /// </remarks>
        [Test]
        public void TwoMassifsBuildIndependentSystems()
        {
            var world = TwoMassifFixture();

            Assert.That(world.Spurs.Count, Is.GreaterThan(0), "the fixture built no spurs");
            Assert.AreEqual(0, world.Stats.CrossingSpurPairs, "two spurs cross");
            Assert.AreEqual(0, world.Stats.SelfIntersectingSpurs, "a spur crosses itself");

            var perMassif = new int[2];
            for (var i = 0; i < world.Spurs.Count; i++)
            {
                var spur = world.Spurs[i];
                perMassif[spur.MassifIndex]++;

                for (var k = 0; k < spur.Points.Length; k++)
                {
                    var owner = world.Ownership.Ownership.At(spur.Points[k].x, spur.Points[k].y);
                    Assert.AreEqual(
                        MassifOwnerKind.Massif, owner.Kind,
                        $"fixture spur {i} pt {k}: on {owner.Kind} ground");
                    Assert.AreEqual(
                        spur.MassifIndex, owner.MassifIndex,
                        $"fixture spur {i} pt {k}: crossed the inter-massif divide");
                }
            }

            Assert.That(perMassif[0], Is.GreaterThan(0), "massif 0 built nothing");
            Assert.That(perMassif[1], Is.GreaterThan(0), "massif 1 built nothing");

            // The divide is the boundary of the partition, so nothing may touch it.
            for (var d = 0; d < world.Ownership.Divides.Count; d++)
            {
                var divide = world.Ownership.Divides[d];
                for (var i = 0; i < world.Spurs.Count; i++)
                {
                    var points = world.Spurs[i].Points;
                    for (var a = 0; a + 1 < points.Length; a++)
                    {
                        for (var b = 0; b + 1 < divide.Points.Length; b++)
                        {
                            Assert.IsFalse(
                                Town.Geometry.Geo2.SegmentIntersect(
                                    points[a], points[a + 1],
                                    divide.Points[b], divide.Points[b + 1], out _),
                                $"fixture spur {i} crosses the inter-massif divide");
                        }
                    }
                }
            }
        }

        // ---------- regression ----------

        /// <summary>Building the spur skeleton changes nothing about Stage 3A.</summary>
        [Test]
        public void Stage3ARemainsUnchanged()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                var before = Partition(Seeds[w], stage2a[w]);
                var beforeLine = before.Stats.ToLine();

                RidgeSkeleton.Build(before, RidgeSettings, new Seed.WorldSeed(Seeds[w]));

                Assert.AreEqual(beforeLine, before.Stats.ToLine(), $"seed {Seeds[w]}: stats moved");

                foreach (var p in SampleGrid(24))
                {
                    var live = partitions[w].Ownership.At(p.x, p.y);
                    var fresh = before.Ownership.At(p.x, p.y);

                    Assert.AreEqual(live.Kind, fresh.Kind, $"seed {Seeds[w]} at {p}: kind");
                    Assert.AreEqual(
                        live.MassifIndex, fresh.MassifIndex, $"seed {Seeds[w]} at {p}: owner");
                    Assert.AreEqual(
                        live.AxisDistanceMeters, fresh.AxisDistanceMeters,
                        $"seed {Seeds[w]} at {p}: distance");
                }
            }
        }

        /// <summary>Building the spur skeleton changes nothing about Stage 2B.</summary>
        /// <remarks>
        /// Stage 3B holds references to the very massifs Stage 2B produced, so a mutation would be
        /// visible here rather than merely improbable. The Stage 1 hash for this seed is asserted too,
        /// so an earlier-stage change cannot pass unnoticed.
        /// </remarks>
        [Test]
        public void Stage2BRemainsUnchanged()
        {
            const int seed = 110201;
            var stage2b = Stage2BMassifGenerator.Generate(
                seed, TerrainSettings, MacroSettings, MassifSettings);

            Assert.AreEqual("7599879a71250a06", stage2b.Stage1.Stats.FieldHash, "Stage 1 changed");

            var fieldBefore = stage2b.Field.Hash();
            var upliftBefore = stage2b.UpliftField.Hash();

            var cores = new Vector2[stage2b.Massifs.Count];
            var certified = new Vector2[stage2b.Massifs.Count];
            var widths = new float[stage2b.Massifs.Count];
            for (var m = 0; m < stage2b.Massifs.Count; m++)
            {
                cores[m] = stage2b.Massifs[m].CoreInterval;
                certified[m] = stage2b.Massifs[m].Axis.CertifiedCore;
                widths[m] = stage2b.Massifs[m].HalfWidthMeters;
            }

            var partition = Stage3ASkeleton.Build(stage2b, RidgeSettings);
            var ridges = RidgeSkeleton.Build(partition, RidgeSettings, new Seed.WorldSeed(seed));

            Assert.That(ridges.Spurs.Count, Is.GreaterThan(0), "the run produced no spurs to speak of");
            Assert.AreEqual(fieldBefore, stage2b.Field.Hash(), "the Stage 2 height field moved");
            Assert.AreEqual(upliftBefore, stage2b.UpliftField.Hash(), "the uplift field moved");

            for (var m = 0; m < stage2b.Massifs.Count; m++)
            {
                Assert.AreEqual(cores[m], stage2b.Massifs[m].CoreInterval, $"massif {m}: CoreInterval");
                Assert.AreEqual(
                    certified[m], stage2b.Massifs[m].Axis.CertifiedCore, $"massif {m}: CertifiedCore");
                Assert.AreEqual(
                    widths[m], stage2b.Massifs[m].HalfWidthMeters, 0f, $"massif {m}: half-width");
            }

            Assert.AreSame(
                stage2b.Massifs[0], ridges.Ownership.Massifs[0],
                "Stage 3B copied the massif instead of referring to it");
        }

        // ---------- fixtures ----------

        /// <summary>A stats line without its build time, which repeats only as fast as the machine.</summary>
        private static string WithoutTiming(string line)
        {
            var at = line.IndexOf(" build=", System.StringComparison.Ordinal);
            return at < 0 ? line : line.Substring(0, at);
        }

        private static Stage3ASkeleton Partition(int seed, Stage2AResult result)
        {
            var worldSeed = new Seed.WorldSeed(seed);
            var massifs = new List<MountainMassif>();
            var accepted = result.AcceptedAxes();
            for (var i = 0; i < accepted.Count; i++)
            {
                massifs.Add(MountainMassif.Build(
                    worldSeed, massifs.Count, accepted[i], TerrainSettings, MassifSettings,
                    accepted[i].CertifiedCore));
            }

            return Stage3ASkeleton.Build(seed, massifs, result.Coast, RidgeSettings);
        }

        private static MassifFlank FlankOf(RidgeSkeleton world, SpurCurve spur)
        {
            for (var i = 0; i < world.Ownership.Flanks.Count; i++)
            {
                var flank = world.Ownership.Flanks[i];
                if (flank.MassifIndex == spur.MassifIndex && flank.Sign == spur.FlankSign)
                {
                    return flank;
                }
            }

            throw new AssertionException($"spur {spur.Id} has no flank");
        }

        private static RidgeSkeleton TwoMassifFixture()
        {
            var ring = new[]
            {
                new Vector2(400f, 400f),
                new Vector2(2400f, 400f),
                new Vector2(2400f, 2400f),
                new Vector2(400f, 2400f),
            };

            var coast = new CoastQuery(new List<Vector2[]> { ring }, 1000f);
            var massifs = new[]
            {
                FixtureMassif(0, new Vector2(800f, 1100f), new Vector2(1700f, 1100f)),
                FixtureMassif(1, new Vector2(800f, 1700f), new Vector2(1700f, 1700f)),
            };

            var partition = Stage3ASkeleton.Build(0, massifs, coast, RidgeSettings);
            return RidgeSkeleton.Build(partition, RidgeSettings, new Seed.WorldSeed(0));
        }

        private static MountainMassif FixtureMassif(int index, Vector2 from, Vector2 to)
        {
            var steps = Mathf.Max(2, Mathf.RoundToInt((to - from).magnitude / 20f));
            var points = new Vector2[steps + 1];
            for (var i = 0; i <= steps; i++)
            {
                points[i] = Vector2.Lerp(from, to, i / (float)steps);
            }

            var axis = new MacroAxis(
                points, new[] { from, to }, new[] { 0, 1 }, float.PositiveInfinity, 0.5f, 250f);

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
