using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.TerrainGen.Tests
{
    /// <summary>
    /// Stage 3C candidate geometry: what a corridor's floor does where a bounding divide ends.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The change under test is one sentence - a divide stops shaping the floor where the divide
    /// stops - and these assert both halves of it. Above a bounding spur's toe the floor is exactly
    /// what it always was; below it the floor descends into the ground that spur released, towards
    /// that spur's own line and never past it.
    /// </para>
    /// <para>
    /// They also assert what the change must <em>not</em> reach: rank, adjacency, divide-end ordering
    /// and every field on <see cref="ValleyCorridor"/> are untouched, so Slice B2 still decides the
    /// topology on geometry it did not influence.
    /// </para>
    /// </remarks>
    public sealed class TerrainGenCandidateGeometryTests
    {
        private static readonly int[] Seeds =
        {
            110201, 424242, 20260905, 7, 101010, 20261231, 1337,
        };

        /// <summary>Transverse samples per corridor, dense enough to straddle any toe depth.</summary>
        private const int Samples = 200;

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

        // ---------- G1..G4: the divide still standing ----------

        /// <summary>G1. Nothing has ended at the crest, so the head is where it always was.</summary>
        [Test]
        public void NoMigrationAtTheCorridorHead()
        {
            ForEachCorridor((seed, ridges, massif, corridor) =>
            {
                var floor = ValleyTrajectoryBuilder.FloorArcAt(massif, corridor, ridges.Spurs, 0f);
                Assert.AreEqual(
                    PlainMid(corridor, ridges.Spurs, 0f), floor, 1e-6f,
                    $"seed {seed} V{corridor.Id}: head arc moved");
                Assert.AreEqual(
                    corridor.HeadS, floor, 1e-5f,
                    $"seed {seed} V{corridor.Id}: head is no longer HeadS");
            });
        }

        /// <summary>G2. While both bounding divides exist, the floor is the plain mid-line.</summary>
        [Test]
        public void FloorIsThePlainMidLineWhileBothDividesLive()
        {
            ForEachCorridor((seed, ridges, massif, corridor) =>
            {
                var live = Mathf.Min(
                    ToeFraction(massif, ridges.Spurs, corridor.LowerSpurId),
                    ToeFraction(massif, ridges.Spurs, corridor.UpperSpurId));

                for (var k = 0; k <= Samples; k++)
                {
                    var t = k / (float)Samples;
                    if (t > live)
                    {
                        continue;
                    }

                    Assert.AreEqual(
                        PlainMid(corridor, ridges.Spurs, t),
                        ValleyTrajectoryBuilder.FloorArcAt(massif, corridor, ridges.Spurs, t),
                        1e-6f,
                        $"seed {seed} V{corridor.Id} t={t}: floor moved while both divides stand");
                }
            });
        }

        /// <summary>G3. The floor leaves the mid-line only below a divide's toe - and it does leave.</summary>
        [Test]
        public void FloorLeavesTheMidLineOnlyBelowADivideToe()
        {
            var moved = 0;

            ForEachCorridor((seed, ridges, massif, corridor) =>
            {
                var live = Mathf.Min(
                    ToeFraction(massif, ridges.Spurs, corridor.LowerSpurId),
                    ToeFraction(massif, ridges.Spurs, corridor.UpperSpurId));

                for (var k = 0; k <= Samples; k++)
                {
                    var t = k / (float)Samples;
                    var delta = Mathf.Abs(
                        ValleyTrajectoryBuilder.FloorArcAt(massif, corridor, ridges.Spurs, t)
                        - PlainMid(corridor, ridges.Spurs, t));

                    if (delta <= 1e-6f)
                    {
                        continue;
                    }

                    moved++;
                    Assert.That(
                        t, Is.GreaterThan(live),
                        $"seed {seed} V{corridor.Id}: floor moved at t={t} with divides alive to {live}");
                }
            });

            Assert.That(
                moved, Is.GreaterThan(0),
                "no corridor on any seed responds to a divide ending - the change is inert");
        }

        /// <summary>
        /// G4. A divide that survives releases nothing: where only one bound ends, the floor moves
        /// towards that one and never towards the other.
        /// </summary>
        /// <remarks>
        /// This is the anti-attraction property stated as a sign. If the floor were being pulled
        /// towards a neighbouring channel rather than into vacated ground, nothing would tie the
        /// direction it moves to which of its own divides ended.
        /// </remarks>
        [Test]
        public void OnlyTheDivideThatEndedMovesTheFloor()
        {
            var oneSided = 0;

            ForEachCorridor((seed, ridges, massif, corridor) =>
            {
                var lower = ToeFraction(massif, ridges.Spurs, corridor.LowerSpurId);
                var upper = ToeFraction(massif, ridges.Spurs, corridor.UpperSpurId);
                var lowerDies = corridor.LowerSpurId >= 0 && lower < 1f;
                var upperDies = corridor.UpperSpurId >= 0 && upper < 1f;
                if (lowerDies == upperDies)
                {
                    return;
                }

                oneSided++;
                var dying = lowerDies ? lower : upper;

                for (var k = 0; k <= Samples; k++)
                {
                    var t = k / (float)Samples;
                    var lowerLine = BoundLine(corridor, ridges.Spurs, corridor.LowerSpurId, true, t);
                    var upperLine = BoundLine(corridor, ridges.Spurs, corridor.UpperSpurId, false, t);

                    // Measured against the plain mid-line of the two divides, which is what the
                    // release is defined on top of.
                    var offset =
                        ValleyTrajectoryBuilder.FloorArcAt(massif, corridor, ridges.Spurs, t)
                        - (lowerLine + upperLine) * 0.5f;

                    if (t <= dying)
                    {
                        Assert.AreEqual(
                            0f, offset, 1e-6f,
                            $"seed {seed} V{corridor.Id} t={t}: floor moved above the toe");
                        continue;
                    }

                    if (Mathf.Abs(offset) <= 1e-6f)
                    {
                        continue;
                    }

                    Assert.That(
                        lowerDies ? offset : -offset, Is.LessThan(0f),
                        $"seed {seed} V{corridor.Id} t={t}: floor moved away from the divide that ended");

                    // Bounded by the ground that divide actually released.
                    var halfGap = Mathf.Abs(upperLine - lowerLine) * 0.5f;
                    var released = (t - dying) * (t - dying);
                    Assert.That(
                        Mathf.Abs(offset), Is.LessThan(halfGap * released + 1e-6f),
                        $"seed {seed} V{corridor.Id} t={t}: floor took more than the released ground");
                }
            });

            Assert.That(
                oneSided, Is.GreaterThan(0),
                "the seed set no longer covers a corridor with exactly one divide that ends early");
        }

        // ---------- G5..G7: how far, and in which direction ----------

        /// <summary>G5. The floor stays strictly between the two divides that bound it.</summary>
        [Test]
        public void FloorStaysStrictlyBetweenItsBounds()
        {
            ForEachCorridor((seed, ridges, massif, corridor) =>
            {
                for (var k = 0; k <= Samples; k++)
                {
                    var t = k / (float)Samples;
                    var lower = BoundLine(corridor, ridges.Spurs, corridor.LowerSpurId, true, t);
                    var upper = BoundLine(corridor, ridges.Spurs, corridor.UpperSpurId, false, t);
                    var floor = ValleyTrajectoryBuilder.FloorArcAt(massif, corridor, ridges.Spurs, t);

                    Assert.That(
                        floor, Is.GreaterThan(lower).And.LessThan(upper),
                        $"seed {seed} V{corridor.Id} t={t}: floor {floor} left [{lower},{upper}]");
                }
            });
        }

        /// <summary>G6. It moves towards the divide that ended, and closes on it without arriving.</summary>
        [Test]
        public void FloorMovesTowardsTheDeadDivideAndNeverReachesIt()
        {
            ForEachCorridor((seed, ridges, massif, corridor) =>
            {
                Approach(seed, ridges, massif, corridor, corridor.LowerSpurId);
                Approach(seed, ridges, massif, corridor, corridor.UpperSpurId);
            });
        }

        /// <summary>
        /// G7. The terminal along-axis rate is alive where a divide died and zero where none did.
        /// </summary>
        /// <remarks>
        /// This is the defect the spike exists for. <c>ArcAt</c> eases with a smoothstep, whose slope
        /// is identically zero at <c>t = 1</c>, so the plain mid-line's terminal tangent carried no
        /// lateral component at all and every candidate ray came out as the flank normal.
        /// </remarks>
        [Test]
        public void TerminalArcRateIsLiveWhereADivideDied()
        {
            const float h = 1e-3f;
            var live = 0;
            var plainTotal = 0f;
            var liveTotal = 0f;

            ForEachCorridor((seed, ridges, massif, corridor) =>
            {
                // The defect itself: ArcAt eases with a smoothstep, so the mid-line's own along-axis
                // rate is identically zero at the foot on every corridor of every seed.
                var plain = massif.LengthMeters * Mathf.Abs(
                    PlainMid(corridor, ridges.Spurs, 1f)
                    - PlainMid(corridor, ridges.Spurs, 1f - h)) / h;
                Assert.That(
                    plain, Is.LessThan(1f),
                    $"seed {seed} V{corridor.Id}: the plain mid-line is no longer flat at the foot,"
                    + " so this test no longer measures what it was written for");

                plainTotal += plain;

                var rate = massif.LengthMeters * Mathf.Abs(
                    ValleyTrajectoryBuilder.FloorArcAt(massif, corridor, ridges.Spurs, 1f)
                    - ValleyTrajectoryBuilder.FloorArcAt(massif, corridor, ridges.Spurs, 1f - h)) / h;

                var lower = ToeFraction(massif, ridges.Spurs, corridor.LowerSpurId);
                var upper = ToeFraction(massif, ridges.Spurs, corridor.UpperSpurId);
                var lowerDies = corridor.LowerSpurId >= 0 && lower < 1f;
                var upperDies = corridor.UpperSpurId >= 0 && upper < 1f;

                if (!lowerDies && !upperDies)
                {
                    Assert.That(
                        rate, Is.LessThan(1f),
                        $"seed {seed} V{corridor.Id}: no divide ended, yet the terminal rate is {rate}");
                    return;
                }

                // Two divides ending at the same depth release symmetrically and the floor is right to
                // stay put; a rate is only owed where the corridor is lopsided.
                var lopsided = !lowerDies || !upperDies || Mathf.Abs(lower - upper) > 0.05f;
                if (!lopsided)
                {
                    return;
                }

                live++;
                liveTotal += rate;

                // A divide dying at the very foot releases almost nothing, so the bar per corridor is
                // only that the tangent is no longer dead. The weight of the claim is the total.
                Assert.That(
                    rate, Is.GreaterThan(1f),
                    $"seed {seed} V{corridor.Id}: divides ended at {lower} and {upper}, yet the"
                    + $" terminal rate is {rate}");
            });

            Assert.That(live, Is.GreaterThan(0), "no corridor has a lopsided pair of divides");
            Assert.That(
                liveTotal, Is.GreaterThan(plainTotal * 10f),
                $"terminal rates total {liveTotal}m against {plainTotal}m for the plain mid-line -"
                + " the candidate rays have not come off the flank normal");
        }

        // ---------- G8..G11: the curve itself ----------

        /// <summary>G8. The bed's curvature is bounded, and it joins the live divide with no kink.</summary>
        /// <remarks>
        /// A kink is a break in the tangent, and there is none: the release starts from rest, so the
        /// one-sided slopes agree at the toe. Curvature does step there, from zero to a constant, the
        /// way it does at any clothoid's entry; C1 with bounded curvature is what a valley floor owes.
        /// </remarks>
        [Test]
        public void BedCurvatureIsBoundedAndTheJoinIsC1()
        {
            ForEachBed((seed, ridges, massif, corridor, bed) =>
            {
                var worst = 0f;
                var worstAt = 1;
                var previous = -1f;
                var count = 0;

                for (var i = 2; i < bed.Length; i++)
                {
                    var a = bed[i - 1] - bed[i - 2];
                    var b = bed[i] - bed[i - 1];
                    if (a.sqrMagnitude < 1e-10f || b.sqrMagnitude < 1e-10f)
                    {
                        continue;
                    }

                    var turn = Mathf.Abs(Vector2.SignedAngle(a, b));
                    if (turn > worst)
                    {
                        worst = turn;
                        worstAt = i;
                    }

                    previous = turn;
                    count++;
                }

                if (count == 0)
                {
                    return;
                }

                var where = worstAt / (float)(bed.Length - 1);

                // Bounded by the corridor the floor lies in. The release gives a constant lateral
                // acceleration below a toe, so the bend is a circular arc rather than a spike, and the
                // bar is that the arc still sits inside the valley: a radius under a quarter of the
                // corridor's width would have the floor turning tighter than it has room to. Measured
                // across the seed set the tightest is 0.90 half-widths against a median of 7.6, so
                // this is a guard rail and not a fitted number.
                var span = (bed[worstAt] - bed[worstAt - 1]).magnitude;
                var radius = span / Mathf.Max(1e-4f, worst * Mathf.Deg2Rad);
                var halfWidth = ValleyTrajectoryBuilder.CorridorHalfWidthMeters(
                    massif, corridor, ridges.Spurs,
                    ValleyTrajectoryBuilder.FloorArcAt(massif, corridor, ridges.Spurs, where), where);
                Assert.That(
                    radius, Is.GreaterThan(halfWidth * 0.5f),
                    $"seed {seed} V{corridor.Id}: bed turns {worst} degrees at t={where}, a radius of"
                    + $" {radius}m inside a corridor {halfWidth}m half-wide");

                // C1 at the join, read off the arc itself rather than off the sampled polyline.
                NoKinkAtTheToe(seed, ridges, massif, corridor, corridor.LowerSpurId);
                NoKinkAtTheToe(seed, ridges, massif, corridor, corridor.UpperSpurId);
            });
        }

        /// <summary>G9. The bed only ever descends: its distance from the axis never falls back.</summary>
        [Test]
        public void BedDescendsMonotonically()
        {
            ForEachBed((seed, ridges, massif, corridor, bed) =>
            {
                var previous = -1f;
                for (var i = 0; i < bed.Length; i++)
                {
                    massif.Project(bed[i].x, bed[i].y, out _, out var offset);
                    var depth = Mathf.Abs(offset);
                    Assert.That(
                        depth, Is.GreaterThanOrEqualTo(previous - 1f),
                        $"seed {seed} V{corridor.Id} sample {i}: bed climbed back towards the crest");
                    previous = depth;
                }
            });
        }

        /// <summary>G10. No bed crosses itself.</summary>
        [Test]
        public void BedDoesNotSelfIntersect()
        {
            ForEachBed((seed, ridges, massif, corridor, bed) =>
            {
                for (var i = 0; i + 1 < bed.Length; i++)
                {
                    for (var j = i + 2; j + 1 < bed.Length; j++)
                    {
                        Assert.IsFalse(
                            SegmentsCross(bed[i], bed[i + 1], bed[j], bed[j + 1]),
                            $"seed {seed} V{corridor.Id}: bed segments {i} and {j} cross");
                    }
                }
            });
        }

        /// <summary>G11. Physically adjacent beds approach but never touch or swap sides.</summary>
        [Test]
        public void AdjacentBedsNeitherCrossNorCoincide()
        {
            for (var w = 0; w < networks.Length; w++)
            {
                var network = networks[w];
                for (var e = 0; e < network.ToeEvents.Count; e++)
                {
                    var toeEvent = network.ToeEvents[e];
                    var a = network.CandidateBeds[toeEvent.LowerCorridorId];
                    var b = network.CandidateBeds[toeEvent.UpperCorridorId];
                    if (a.Length < 2 || b.Length < 2)
                    {
                        continue;
                    }

                    var closest = float.MaxValue;
                    for (var i = 0; i + 1 < a.Length; i++)
                    {
                        for (var j = 0; j + 1 < b.Length; j++)
                        {
                            Assert.IsFalse(
                                SegmentsCross(a[i], a[i + 1], b[j], b[j + 1]),
                                $"seed {Seeds[w]}: beds V{toeEvent.LowerCorridorId} and"
                                + $" V{toeEvent.UpperCorridorId} cross inside the massif");
                            closest = Mathf.Min(closest, (a[i] - b[j]).magnitude);
                        }
                    }

                    Assert.That(
                        closest, Is.GreaterThan(0.5f),
                        $"seed {Seeds[w]}: beds V{toeEvent.LowerCorridorId} and"
                        + $" V{toeEvent.UpperCorridorId} coincide");
                }
            }
        }

        // ---------- G12, G13: what must not have moved ----------

        /// <summary>G12. Rank, adjacency and divide-end ordering owe nothing to the bed.</summary>
        [Test]
        public void RankAdjacencyAndToeEventsAreUnchangedByTheGeometry()
        {
            for (var w = 0; w < networks.Length; w++)
            {
                var ridges = skeletons[w];
                var network = networks[w];

                for (var i = 0; i < network.Ranks.Count; i++)
                {
                    var corridor = ridges.Valleys[i];
                    var massif = ridges.Ownership.Massifs[corridor.MassifIndex];
                    var crest = massif.CrestAt(corridor.HeadS);
                    var footprint = corridor.SWidth * massif.LengthMeters
                                    * ValleyTrajectoryBuilder.FlankHalfWidth(
                                        massif, corridor.HeadS, corridor.FlankSign);

                    Assert.AreEqual(
                        footprint, network.Ranks[i].FlankFootprintSqm, Mathf.Max(1e-3f, footprint * 1e-5f),
                        $"seed {Seeds[w]} V{i}: footprint no longer the head-section formula");
                    Assert.AreEqual(
                        footprint * crest, network.Ranks[i].ReliefVolumeCum,
                        Mathf.Max(1e-3f, footprint * crest * 1e-5f),
                        $"seed {Seeds[w]} V{i}: relief volume no longer the head-section formula");
                }

                for (var i = 0; i < ridges.Valleys.Count; i++)
                {
                    var upper = network.UpperNeighbourOf(i);
                    if (upper < 0)
                    {
                        continue;
                    }

                    Assert.AreEqual(
                        ridges.Valleys[i].UpperSpurId, ridges.Valleys[upper].LowerSpurId,
                        $"seed {Seeds[w]} V{i}: adjacency is not a shared divide");
                }

                for (var e = 0; e < network.ToeEvents.Count; e++)
                {
                    var toeEvent = network.ToeEvents[e];
                    var spur = ridges.Spurs[toeEvent.SpurId];
                    var massif = ridges.Ownership.Massifs[spur.MassifIndex];
                    var width = ValleyTrajectoryBuilder.FlankHalfWidth(
                        massif, spur.AttachS, spur.FlankSign);

                    Assert.AreEqual(
                        width > 1e-3f ? spur.ReachMeters / width : 1f, toeEvent.ToeFraction, 1e-4f,
                        $"seed {Seeds[w]} event {e}: toe fraction is no longer reach over flank");

                    if (e > 0)
                    {
                        Assert.That(
                            toeEvent.ToeFraction,
                            Is.GreaterThanOrEqualTo(network.ToeEvents[e - 1].ToeFraction - 1e-6f),
                            $"seed {Seeds[w]} event {e}: divide-end ordering broken");
                    }
                }
            }
        }

        /// <summary>G13. The geometry is reproducible and writes nothing onto the corridors.</summary>
        [Test]
        public void CandidateGeometryIsDeterministicAndWritesNothing()
        {
            for (var w = 0; w < networks.Length; w++)
            {
                var again = ValleyNetwork.Build(Skeleton(Seeds[w]), RidgeSettings);
                var first = networks[w];

                Assert.AreEqual(
                    first.CandidateBeds.Count, again.CandidateBeds.Count, $"seed {Seeds[w]}: bed count");

                for (var i = 0; i < first.CandidateBeds.Count; i++)
                {
                    var a = first.CandidateBeds[i];
                    var b = again.CandidateBeds[i];
                    Assert.AreEqual(a.Length, b.Length, $"seed {Seeds[w]} V{i}: sample count");
                    for (var k = 0; k < a.Length; k++)
                    {
                        Assert.AreEqual(a[k].x, b[k].x, 0f, $"seed {Seeds[w]} V{i} sample {k}: x");
                        Assert.AreEqual(a[k].y, b[k].y, 0f, $"seed {Seeds[w]} V{i} sample {k}: y");
                    }

                    Assert.AreEqual(
                        first.CandidateRays[i].Direction, again.CandidateRays[i].Direction,
                        $"seed {Seeds[w]} V{i}: ray direction");

                    var corridor = skeletons[w].Valleys[i];
                    Assert.AreEqual(0, corridor.Floor.Length, $"seed {Seeds[w]} V{i}: floor written");
                    Assert.AreEqual(-1, corridor.DownstreamId, $"seed {Seeds[w]} V{i}: downstream written");
                    Assert.AreEqual(
                        ValleyTermination.Unresolved, corridor.Termination,
                        $"seed {Seeds[w]} V{i}: termination written");
                }
            }
        }

        // ---------- fixtures ----------

        private void Approach(
            int seed, RidgeSkeleton ridges, MountainMassif massif, ValleyCorridor corridor, int spurId)
        {
            if (spurId < 0)
            {
                return;
            }

            var toe = ToeFraction(massif, ridges.Spurs, spurId);
            if (toe >= 1f)
            {
                return;
            }

            var line = ridges.Spurs[spurId].ArcAt(toe);
            var previous = float.MaxValue;

            for (var k = 0; k <= Samples; k++)
            {
                var t = k / (float)Samples;
                if (t < toe)
                {
                    continue;
                }

                var gap = Mathf.Abs(
                    ValleyTrajectoryBuilder.FloorArcAt(massif, corridor, ridges.Spurs, t) - line);

                Assert.That(
                    gap, Is.GreaterThan(0f),
                    $"seed {seed} V{corridor.Id} t={t}: floor reached the dead divide S{spurId}");

                if (previous < float.MaxValue)
                {
                    // The corridor's other bound may still be drifting, so allow a metre of give.
                    Assert.That(
                        gap, Is.LessThanOrEqualTo(previous + 1f / Mathf.Max(1f, massif.LengthMeters)),
                        $"seed {seed} V{corridor.Id} t={t}: floor backed away from S{spurId}");
                }

                previous = gap;
            }
        }

        /// <summary>The one-sided arc slopes agree where a bounding divide ends.</summary>
        private static void NoKinkAtTheToe(
            int seed, RidgeSkeleton ridges, MountainMassif massif, ValleyCorridor corridor, int spurId)
        {
            if (spurId < 0)
            {
                return;
            }

            var toe = ToeFraction(massif, ridges.Spurs, spurId);
            if (toe >= 1f || toe <= 0f)
            {
                return;
            }

            // Wide enough that differencing a float arc does not swamp the slope it is measuring.
            const float h = 1e-3f;
            var here = ValleyTrajectoryBuilder.FloorArcAt(massif, corridor, ridges.Spurs, toe);
            var above = massif.LengthMeters * (here - ValleyTrajectoryBuilder.FloorArcAt(
                massif, corridor, ridges.Spurs, toe - h)) / h;
            var below = massif.LengthMeters * (ValleyTrajectoryBuilder.FloorArcAt(
                massif, corridor, ridges.Spurs, toe + h) - here) / h;

            Assert.AreEqual(
                above, below, Mathf.Max(2f, Mathf.Abs(above) * 0.05f),
                $"seed {seed} V{corridor.Id}: the arc slope breaks where S{spurId} ends,"
                + $" {above}m above against {below}m below");
        }

        private void ForEachCorridor(System.Action<int, RidgeSkeleton, MountainMassif, ValleyCorridor> body)
        {
            for (var w = 0; w < skeletons.Length; w++)
            {
                var ridges = skeletons[w];
                for (var i = 0; i < ridges.Valleys.Count; i++)
                {
                    var corridor = ridges.Valleys[i];
                    body(Seeds[w], ridges, ridges.Ownership.Massifs[corridor.MassifIndex], corridor);
                }
            }
        }

        private void ForEachBed(
            System.Action<int, RidgeSkeleton, MountainMassif, ValleyCorridor, Vector2[]> body)
        {
            for (var w = 0; w < networks.Length; w++)
            {
                var ridges = skeletons[w];
                for (var i = 0; i < ridges.Valleys.Count; i++)
                {
                    var bed = networks[w].CandidateBeds[i];
                    if (bed.Length < 2)
                    {
                        continue;
                    }

                    var corridor = ridges.Valleys[i];
                    body(
                        Seeds[w], ridges, ridges.Ownership.Massifs[corridor.MassifIndex], corridor, bed);
                }
            }
        }


        /// <summary>The mid-line as it was before the change: both divides assumed endless.</summary>
        private static float PlainMid(ValleyCorridor corridor, IReadOnlyList<SpurCurve> spurs, float t)
        {
            var lower = corridor.LowerSpurId >= 0
                ? spurs[corridor.LowerSpurId].ArcAt(t)
                : corridor.SInterval.x;
            var upper = corridor.UpperSpurId >= 0
                ? spurs[corridor.UpperSpurId].ArcAt(t)
                : corridor.SInterval.y;
            return (lower + upper) * 0.5f;
        }

        /// <summary>Where a divide ends, as a fraction of the flank. One means it reaches the foot.</summary>
        private static float ToeFraction(
            MountainMassif massif, IReadOnlyList<SpurCurve> spurs, int spurId)
        {
            if (spurId < 0 || spurId >= spurs.Count)
            {
                return 1f;
            }

            var spur = spurs[spurId];
            var width = ValleyTrajectoryBuilder.FlankHalfWidth(massif, spur.AttachS, spur.FlankSign);
            return width > 1e-3f ? Mathf.Clamp01(spur.ReachMeters / width) : 1f;
        }

        /// <summary>A bounding divide's line at a transverse depth, or the interval edge.</summary>
        private static float BoundLine(
            ValleyCorridor corridor, IReadOnlyList<SpurCurve> spurs, int spurId, bool isLower, float t)
        {
            if (spurId < 0 || spurId >= spurs.Count)
            {
                return isLower ? corridor.SInterval.x : corridor.SInterval.y;
            }

            return spurs[spurId].ArcAt(t);
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
