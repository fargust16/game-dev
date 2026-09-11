using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.TerrainGen.Tests
{
    /// <summary>
    /// Stage 2 of the terrain generator: structural mountain uplift on the Stage 1 landmass.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These assert the contract of the uplift model - determinism, boundedness, and the two
    /// invariants that make the stage additive: the coastline does not move and the ocean does not
    /// change. They say nothing about ridges, valleys, drainage or refinement, none of which exists
    /// at this stage.
    /// </para>
    /// <para>
    /// Generation runs once per seed in <c>OneTimeSetUp</c>. A Stage 2 world is a Stage 1 world plus
    /// an uplift pass, so it costs what Stage 1 costs plus a little.
    /// </para>
    /// </remarks>
    public sealed class TerrainGenStage2Tests
    {
        private const int SeedA = 110201;
        private const int SeedB = 424242;

        private static TerrainGenSettings Settings => TerrainGenSettings.Default;

        private Stage2Result a;
        private Stage2Result b;

        [OneTimeSetUp]
        public void GenerateWorlds()
        {
            a = Stage2MountainGenerator.Generate(SeedA, Settings);
            b = Stage2MountainGenerator.Generate(SeedB, Settings);
        }

        private static float Sea => WorldScale.SeaLevelMeters;

        // ---------- determinism ----------

        [Test]
        public void SameSeed_ProducesAnIdenticalField()
        {
            var repeat = Stage2MountainGenerator.Generate(SeedA, Settings);

            Assert.AreEqual(a.Stats.FieldHash, repeat.Stats.FieldHash, "field hash differs on a repeat run");
            Assert.AreEqual(a.Structure.Ranges.Count, repeat.Structure.Ranges.Count);

            for (var r = 0; r < a.Structure.Ranges.Count; r++)
            {
                var x = a.Structure.Ranges[r];
                var y = repeat.Structure.Ranges[r];
                Assert.AreEqual(x.ParentIndex, y.ParentIndex);
                Assert.AreEqual(x.CrestHeightMeters, y.CrestHeightMeters, 1e-4f);
                Assert.AreEqual(x.HalfWidthMeters, y.HalfWidthMeters, 1e-4f);
                Assert.AreEqual(x.Asymmetry, y.Asymmetry, 1e-4f);
                Assert.AreEqual(x.SteepSide, y.SteepSide);
                Assert.AreEqual(x.Spine.Length, y.Spine.Length);
                CollectionAssert.AreEqual(x.SourceSitePairs, y.SourceSitePairs);

                for (var i = 0; i < x.Spine.Length; i++)
                {
                    Assert.AreEqual(x.Spine[i], y.Spine[i]);
                }
            }

            for (var i = 0; i < a.Field.Heights.Length; i++)
            {
                if (a.Field.Heights[i] != repeat.Field.Heights[i])
                {
                    Assert.Fail($"height differs at sample {i}");
                }
            }
        }

        [Test]
        public void DifferentSeed_ProducesADifferentWorld()
        {
            Assert.AreNotEqual(a.Stats.FieldHash, b.Stats.FieldHash);

            // Not just a different coastline: the mountain layout has to move too, or the belts are
            // not really a function of the seeded crustal structure.
            var sameLayout = a.Structure.Ranges.Count == b.Structure.Ranges.Count;
            if (sameLayout)
            {
                var differs = false;
                for (var r = 0; r < a.Structure.Ranges.Count && !differs; r++)
                {
                    differs = Mathf.Abs(
                                  a.Structure.Ranges[r].LengthMeters
                                  - b.Structure.Ranges[r].LengthMeters) > 1f
                              || Mathf.Abs(
                                  a.Structure.Ranges[r].CrestHeightMeters
                                  - b.Structure.Ranges[r].CrestHeightMeters) > 1f;
                }

                Assert.IsTrue(differs, "two seeds produced the same mountain layout");
            }
        }

        [Test]
        public void PointQuery_AgreesWithTheBulkField()
        {
            var field = a.Field;
            var mismatches = 0;
            var firstMessage = string.Empty;

            const int step = 17;
            for (var i = 0; i < field.Heights.Length; i += step)
            {
                var x = i % field.Width;
                var z = i / field.Width;
                var bulk = field.Heights[i];
                var point = a.HeightAt(field.WorldX(x), field.WorldZ(z));
                if (bulk == point)
                {
                    continue;
                }

                if (mismatches == 0)
                {
                    firstMessage = $"({x},{z}) bulk={bulk} point={point}";
                }

                mismatches++;
            }

            Assert.AreEqual(
                0, mismatches, $"point query disagreed with the field {mismatches} times; first {firstMessage}");
        }

        [Test]
        public void TraversalOrder_DoesNotChangeTheField()
        {
            // Nothing about the uplift pass may depend on the order samples are visited. Guards
            // against a future optimisation that carries state between rows or between ranges.
            var forward = a.Field;
            var reverse = WorldHeightField.ForWorld(Settings.FieldResolution);

            for (var z = reverse.Height - 1; z >= 0; z--)
            {
                var worldZ = reverse.WorldZ(z);
                for (var x = reverse.Width - 1; x >= 0; x--)
                {
                    var index = z * reverse.Width + x;
                    reverse.Heights[index] =
                        a.Stage1.Field.Heights[index]
                        + a.Uplift.UpliftAt(reverse.WorldX(x), worldZ, a.Stage1.SignedDistance[index]);
                }
            }

            Assert.AreEqual(forward.Hash(), reverse.Hash());
        }

        [Test]
        public void Stage1_IsUnchangedByStage2()
        {
            // Stage 2 must be additive: Stage 1 remains a valid output on its own, which is what
            // makes the stage reversible.
            var stage1 = Stage1LandmassGenerator.Generate(SeedA, Settings);
            Assert.AreEqual(stage1.Stats.FieldHash, a.Stage1.Stats.FieldHash);
            Assert.AreEqual(stage1.Stats.CoastLengthMeters, a.Stats.CoastLengthMeters, 1e-3f);
            Assert.AreEqual(stage1.Structure.CoastRings.Count, a.Structure.CoastRings.Count);
        }

        // ---------- the field ----------

        [Test]
        public void Field_IsFiniteAndInsideTheHeightBudget()
        {
            foreach (var result in new[] { a, b })
            {
                Assert.IsFalse(
                    result.Stats.HasNonFinite,
                    $"seed {result.Structure.Seed} produced NaN or infinity");

                Assert.GreaterOrEqual(result.Stats.MinHeightMeters, 0f);
                Assert.LessOrEqual(
                    result.Stats.MaxHeightMeters,
                    WorldScale.TerrainHeightBudgetMeters,
                    "the field must fit the terrain height budget");

                Assert.GreaterOrEqual(
                    result.Stats.MinHeightMeters, result.Settings.MinSeaFloorMeters - 0.01f);
                Assert.LessOrEqual(
                    result.Stats.MaxHeightMeters, result.Settings.MaxStage2HeightMeters + 0.01f);

                // Every sample, not just the reported extremes.
                for (var i = 0; i < result.Field.Heights.Length; i++)
                {
                    var h = result.Field.Heights[i];
                    if (float.IsNaN(h) || float.IsInfinity(h))
                    {
                        Assert.Fail($"sample {i} is not finite");
                    }
                }
            }
        }

        [Test]
        public void Coastline_StaysAtSeaLevel()
        {
            foreach (var result in new[] { a, b })
            {
                for (var r = 0; r < result.Structure.CoastRings.Count; r++)
                {
                    var ring = result.Structure.CoastRings[r];
                    for (var i = 0; i < ring.Length; i += Mathf.Max(1, ring.Length / 48))
                    {
                        var h = result.HeightAt(ring[i].x, ring[i].y);
                        Assert.AreEqual(
                            Sea, h, 0.5f,
                            $"seed {result.Structure.Seed} ring {r} vertex {i} left sea level");
                    }
                }
            }
        }

        [Test]
        public void Ocean_IsUnchangedAndStaysBelowSeaLevel()
        {
            foreach (var result in new[] { a, b })
            {
                Assert.AreEqual(
                    0,
                    result.Stats.OceanSamplesChanged,
                    "uplift altered the sea, so the coastline is not preserved");

                // Counted in a plain loop and asserted once. An NUnit assertion per sample over a
                // million-sample field costs minutes of constraint construction and reports the same
                // thing.
                //
                // Two different definitions of "sea", checked separately on purpose. The model's own
                // boundary is the signed distance: uplift is zero at and seaward of it, exactly. The
                // requirement is about height: nothing under water may come out of this stage above
                // water. They disagree on a handful of samples that sit on the coastline itself,
                // where the distance is a hair above zero but the coastal profile has not yet risen
                // by a representable amount, so the height is still exactly sea level. Conflating the
                // two would either hide a real defect or report a rounding artefact as one.
                var outsideSamples = 0;
                var outsideLifted = 0;
                var outsideMoved = 0;

                var seaSamples = 0;
                var seaMoved = 0;
                var seaRose = 0;

                for (var i = 0; i < result.Field.Heights.Length; i++)
                {
                    var stage1Height = result.Stage1.Field.Heights[i];
                    var height = result.Field.Heights[i];

                    if (result.Stage1.SignedDistance[i] <= 0f)
                    {
                        outsideSamples++;
                        if (result.UpliftField.Heights[i] != 0f)
                        {
                            outsideLifted++;
                        }

                        if (height != stage1Height)
                        {
                            outsideMoved++;
                        }
                    }

                    if (stage1Height > Sea)
                    {
                        continue;
                    }

                    seaSamples++;
                    if (height != stage1Height)
                    {
                        seaMoved++;
                    }

                    if (height > Sea)
                    {
                        seaRose++;
                    }
                }

                var seed = result.Structure.Seed;
                Assert.Greater(outsideSamples, 0, "no samples seaward of the coastline to check");
                Assert.AreEqual(0, outsideLifted, $"seed {seed}: uplift reached seaward of the coastline");
                Assert.AreEqual(0, outsideMoved, $"seed {seed}: the sea floor moved");

                Assert.Greater(seaSamples, 0, "no samples at or below sea level to check");
                Assert.AreEqual(0, seaMoved, $"seed {seed}: a sample at or below sea level moved");
                Assert.AreEqual(0, seaRose, $"seed {seed}: uplift lifted water into land");
            }
        }

        [Test]
        public void LandFraction_IsUnchangedByUplift()
        {
            // Uplift only adds height on land, so the set of dry samples cannot change.
            foreach (var result in new[] { a, b })
            {
                Assert.AreEqual(
                    result.Stats.Stage1LandFraction,
                    result.Stats.FieldLandFraction,
                    1e-6f,
                    "uplift changed how much of the world is dry land");
            }
        }

        // ---------- the uplift contribution ----------

        [Test]
        public void Uplift_IsNonNegativeAndBounded()
        {
            foreach (var result in new[] { a, b })
            {
                var ceiling = result.Uplift.CeilingMeters;
                var nonFinite = 0;
                var negative = 0;
                var overCeiling = 0;

                for (var i = 0; i < result.UpliftField.Heights.Length; i++)
                {
                    var u = result.UpliftField.Heights[i];
                    if (float.IsNaN(u) || float.IsInfinity(u))
                    {
                        nonFinite++;
                    }
                    else if (u < 0f)
                    {
                        negative++;
                    }
                    else if (u > result.Settings.MaxUpliftMeters + 1e-3f)
                    {
                        overCeiling++;
                    }
                }

                Assert.AreEqual(0, nonFinite, $"seed {result.Structure.Seed}: uplift samples not finite");
                Assert.AreEqual(0, negative, $"seed {result.Structure.Seed}: uplift samples negative");
                Assert.AreEqual(0, overCeiling, $"seed {result.Structure.Seed}: uplift samples over the ceiling");

                Assert.LessOrEqual(
                    result.Stats.UpliftMaxMeters,
                    ceiling + 1e-3f,
                    "uplift exceeded the tallest crest plus the blend allowance");

                Assert.AreEqual(
                    0,
                    result.Stats.UpliftClampedSamples,
                    "the hard uplift ceiling had to bind, which means the profiles are mis-scaled");
            }
        }

        [Test]
        public void SmoothMax_ReducesToMaxWhereThereIsNothingToBlend()
        {
            // The textbook polynomial form adds a quarter of the blend width wherever the inputs are
            // equal - including where both are zero, which would raise a phantom ridge across every
            // point equidistant from two ranges.
            Assert.AreEqual(0f, MountainUpliftModel.SmoothMax(0f, 0f, 40f), 1e-6f);
            Assert.AreEqual(5f, MountainUpliftModel.SmoothMax(0f, 5f, 40f), 1e-6f);
            Assert.AreEqual(5f, MountainUpliftModel.SmoothMax(5f, 0f, 40f), 1e-6f);
            Assert.AreEqual(0.02f, MountainUpliftModel.SmoothMax(0.01f, 0.02f, 40f), 1e-4f);

            var blended = MountainUpliftModel.SmoothMax(100f, 100f, 40f);
            Assert.GreaterOrEqual(blended, 100f);
            Assert.LessOrEqual(blended, 110f, "the blend must stay within a quarter of its width");
        }

        // ---------- the mountain systems ----------

        [Test]
        public void MountainSystems_ExistAndRespectTheBudget()
        {
            foreach (var result in new[] { a, b })
            {
                Assert.Greater(
                    result.Stats.MountainSystemCount,
                    0,
                    $"seed {result.Structure.Seed} produced no mountain system at all");

                Assert.LessOrEqual(
                    result.Stats.MountainSystemCount,
                    result.Settings.MaxMountainSystems,
                    "more primary belts than the world allows");

                Assert.LessOrEqual(
                    result.Stats.BranchCount,
                    result.Stats.MountainSystemCount * result.Settings.MaxBranchesPerRange,
                    "more branches than the primaries can carry");

                Assert.AreEqual(
                    result.Stats.MountainSystemCount,
                    result.Structure.PrimaryRangeCount,
                    "the reported system count disagrees with the structure");
            }
        }

        [Test]
        public void MountainSpines_AreValidAndInsideTheLandmass()
        {
            foreach (var result in new[] { a, b })
            {
                for (var r = 0; r < result.Structure.Ranges.Count; r++)
                {
                    var range = result.Structure.Ranges[r];

                    Assert.GreaterOrEqual(range.Spine.Length, 2, $"range {r} is not a polyline");
                    Assert.Greater(range.LengthMeters, 0f, $"range {r} has no length");

                    var minimum = range.ParentIndex < 0
                        ? result.Settings.MinSpineLengthMeters
                        : result.Settings.MinBranchLengthMeters;

                    // The chain length is measured on the unsmoothed boundaries; corner cutting
                    // shortens it, so the spine is allowed to be a little under the chain minimum.
                    Assert.Greater(
                        range.LengthMeters,
                        minimum * 0.6f,
                        $"range {r} is far shorter than the chain it came from");

                    for (var i = 0; i < range.Spine.Length; i++)
                    {
                        var p = range.Spine[i];
                        Assert.IsFalse(
                            float.IsNaN(p.x) || float.IsNaN(p.y), $"range {r} vertex {i} is not finite");

                        Assert.IsTrue(
                            result.Coast.IsInside(p.x, p.y),
                            $"range {r} vertex {i} at ({p.x:F0},{p.y:F0}) is outside the landmass");

                        Assert.Greater(
                            result.Stage1.Field.Heights[NearestSample(result, p)],
                            Sea,
                            $"range {r} vertex {i} sits on a sample that is not dry land");

                        if (i > 0)
                        {
                            Assert.Greater(
                                (range.Spine[i] - range.Spine[i - 1]).sqrMagnitude,
                                1e-6f,
                                $"range {r} repeats vertex {i}");
                        }
                    }
                }
            }
        }

        [Test]
        public void MountainSpines_FollowChainedCrustalBoundaries()
        {
            // The structural claim of this stage: a spine is a chain of boundaries between crustal
            // blocks. Consecutive boundaries of a chain meet at a crustal vertex, and two boundaries
            // meeting at a vertex share exactly one of their two sites.
            foreach (var result in new[] { a, b })
            {
                var landSites = new HashSet<int>(result.Structure.LandSites);

                for (var r = 0; r < result.Structure.Ranges.Count; r++)
                {
                    var pairs = result.Structure.Ranges[r].SourceSitePairs;
                    Assert.Greater(pairs.Length, 0, $"range {r} records no crustal boundaries");
                    Assert.AreEqual(0, pairs.Length % 2, $"range {r} has a dangling site");

                    for (var i = 0; i + 1 < pairs.Length; i += 2)
                    {
                        Assert.IsTrue(
                            landSites.Contains(pairs[i]) && landSites.Contains(pairs[i + 1]),
                            $"range {r} boundary {i / 2} touches a cell that is not land");
                        Assert.AreNotEqual(pairs[i], pairs[i + 1], $"range {r} boundary {i / 2} is degenerate");
                    }

                    for (var i = 0; i + 3 < pairs.Length; i += 2)
                    {
                        var shared =
                            pairs[i] == pairs[i + 2] || pairs[i] == pairs[i + 3] ||
                            pairs[i + 1] == pairs[i + 2] || pairs[i + 1] == pairs[i + 3];
                        Assert.IsTrue(
                            shared,
                            $"range {r}: boundaries {i / 2} and {i / 2 + 1} do not meet at a crustal vertex");
                    }
                }
            }
        }

        [Test]
        public void CrestProfiles_PeakOffCentre()
        {
            // A range whose summit sits exactly halfway along it is one of the artificiality tells
            // WORLD_RELIEF_PRINCIPLES bans by name.
            foreach (var result in new[] { a, b })
            {
                for (var r = 0; r < result.Structure.Ranges.Count; r++)
                {
                    var crest = result.Structure.Ranges[r].Crest;
                    var bestT = 0f;
                    var bestValue = float.NegativeInfinity;
                    for (var t = 0f; t <= 1f; t += 0.005f)
                    {
                        var v = crest.Evaluate(t);
                        if (v > bestValue)
                        {
                            bestValue = v;
                            bestT = t;
                        }
                    }

                    Assert.Greater(
                        Mathf.Abs(bestT - 0.5f), 0.04f, $"range {r} peaks at its own midpoint");
                    Assert.AreEqual(0f, crest.Evaluate(0f), 1e-4f, $"range {r} does not taper at its start");
                    Assert.AreEqual(0f, crest.Evaluate(1f), 1e-4f, $"range {r} does not taper at its end");
                }
            }
        }

        [Test]
        public void Ranges_AreAsymmetricAcrossTheirSpine()
        {
            // The single parameter that stops a belt reading as a radial mound: the two flanks fall
            // away over different distances.
            foreach (var result in new[] { a, b })
            {
                for (var r = 0; r < result.Structure.Ranges.Count; r++)
                {
                    var range = result.Structure.Ranges[r];
                    Assert.Less(range.Asymmetry, 1f, $"range {r} has mirror-image flanks");
                    Assert.GreaterOrEqual(range.Asymmetry, result.Settings.RangeAsymmetryMin - 1e-4f);
                    Assert.LessOrEqual(range.Asymmetry, result.Settings.RangeAsymmetryMax + 1e-4f);
                }
            }
        }

        // ---------- the corrected geometry ----------

        [Test]
        public void MajorRanges_AreLongerThanTheyAreWide()
        {
            // The correction's headline requirement: a belt has to be clearly longer than it is wide
            // or it reads as a hill. The width is capped from the length that was found, so this is a
            // property of the construction rather than a hope.
            foreach (var result in new[] { a, b })
            {
                for (var i = 0; i < result.Structure.Ranges.Count; i++)
                {
                    var metrics = result.Metrics[i];
                    Assert.GreaterOrEqual(
                        metrics.LengthToWidthRatio,
                        result.Settings.MinLengthToHalfWidthRatio * 0.95f,
                        $"seed {result.Structure.Seed} range {i}: "
                        + $"{metrics.LengthMeters:F0} m long, {metrics.CharacteristicHalfWidthMeters:F0} m "
                        + "half-width - that is a hill, not a range");
                }
            }
        }

        [Test]
        public void Ranges_ProduceElongatedReliefRatherThanDomes()
        {
            // The dome detector, measured on the relief each belt actually produces. A circular hill
            // scores about 1. This is the test the previous implementation would have failed: its
            // spines were long while the relief around them was a row of radial blobs.
            foreach (var result in new[] { a, b })
            {
                for (var i = 0; i < result.Structure.Ranges.Count; i++)
                {
                    Assert.GreaterOrEqual(
                        result.Metrics[i].ReliefElongation,
                        2f,
                        $"seed {result.Structure.Seed} range {i}: relief elongation "
                        + $"{result.Metrics[i].ReliefElongation:F2} - the belt reads as a dome");
                }
            }
        }

        [Test]
        public void LongitudinalProfile_IsContinuousAndKeepsItsSaddlesElevated()
        {
            foreach (var result in new[] { a, b })
            {
                for (var r = 0; r < result.Structure.Ranges.Count; r++)
                {
                    var range = result.Structure.Ranges[r];
                    var previous = range.Crest.Evaluate(0f);
                    for (var t = 0.002f; t <= 1f; t += 0.002f)
                    {
                        var value = range.Crest.Evaluate(t);
                        Assert.Less(
                            Mathf.Abs(value - previous),
                            0.05f,
                            $"seed {result.Structure.Seed} range {r}: crest profile jumps at t={t:F3}");
                        previous = value;
                    }

                    // A backbone, not a chain of separate mountains: where a range has more than one
                    // major summit the ground between them has to stay high enough to read as one
                    // structure.
                    if (result.Metrics[r].MajorSummits >= 2)
                    {
                        Assert.GreaterOrEqual(
                            result.Metrics[r].MinSaddleFraction,
                            0.4f,
                            $"seed {result.Structure.Seed} range {r}: the saddle between summits drops "
                            + "so far that the range reads as separate mountains");
                    }
                }
            }
        }

        [Test]
        public void UpliftField_HasNoDiscontinuities()
        {
            // What a folded coordinate chart looks like from the outside. When the half-width exceeds
            // the spine's local curvature radius the longitudinal coordinate jumps across the medial
            // axis, and the field steps by tens of metres along a straight line - the ray artifact
            // that started this correction. A Lipschitz bound over adjacent samples catches it
            // without needing to look at an image.
            foreach (var result in new[] { a, b })
            {
                var field = result.UpliftField;
                var worst = 0f;
                var worstAt = -1;

                for (var z = 0; z < field.Height; z++)
                {
                    var row = z * field.Width;
                    for (var x = 1; x < field.Width; x++)
                    {
                        var step = Mathf.Abs(field.Heights[row + x] - field.Heights[row + x - 1]);
                        if (step > worst)
                        {
                            worst = step;
                            worstAt = row + x;
                        }
                    }
                }

                for (var z = 1; z < field.Height; z++)
                {
                    var row = z * field.Width;
                    var previousRow = row - field.Width;
                    for (var x = 0; x < field.Width; x++)
                    {
                        var step = Mathf.Abs(field.Heights[row + x] - field.Heights[previousRow + x]);
                        if (step > worst)
                        {
                            worst = step;
                            worstAt = row + x;
                        }
                    }
                }

                Assert.Less(
                    worst,
                    25f,
                    $"seed {result.Structure.Seed}: uplift steps {worst:F1} m between adjacent samples "
                    + $"at index {worstAt} - the field is discontinuous");
            }
        }

        [Test]
        public void SpineIsStructurallyConnected()
        {
            // Every consecutive pair of spine points is one resampling step apart, so the axis is one
            // continuous path rather than a set of fragments.
            foreach (var result in new[] { a, b })
            {
                for (var r = 0; r < result.Structure.Ranges.Count; r++)
                {
                    var range = result.Structure.Ranges[r];
                    var longest = 0f;
                    for (var i = 1; i < range.Spine.Length; i++)
                    {
                        longest = Mathf.Max(longest, (range.Spine[i] - range.Spine[i - 1]).magnitude);
                    }

                    Assert.Less(
                        longest,
                        40f,
                        $"seed {result.Structure.Seed} range {r}: spine has a {longest:F0} m gap");
                }
            }
        }

        private static int NearestSample(Stage2Result result, Vector2 p)
        {
            var field = result.Field;
            var x = Mathf.Clamp(Mathf.RoundToInt(p.x / field.CellSize), 0, field.Width - 1);
            var z = Mathf.Clamp(Mathf.RoundToInt(p.y / field.CellSize), 0, field.Height - 1);
            return z * field.Width + x;
        }
    }
}
