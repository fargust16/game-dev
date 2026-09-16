using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>A knoll candidate: one land crust site, and whether it became a knoll.</summary>
    public sealed class ReliefKnoll
    {
        public int SiteIndex { get; internal set; }

        /// <summary>Centroid of the crust block's raster cells, world XZ.</summary>
        public Vector2 Center { get; internal set; }

        /// <summary>Equivalent diameter of the crust block, metres.</summary>
        public float DiameterMeters { get; internal set; }

        /// <summary>The block's resistance draw, [0, 1).</summary>
        public float Resistance { get; internal set; }

        /// <summary>Plate level Λ at the centroid.</summary>
        public float Level { get; internal set; }

        /// <summary>Shore gate Γ at the centroid.</summary>
        public float Gate { get; internal set; }

        /// <summary>Whether the block keeps a knoll.</summary>
        public bool Placed { get; internal set; }

        /// <summary>Why it did not, when it did not: "gate", "level" or "resistance".</summary>
        public string Rejection { get; internal set; }

        /// <summary>Direction of the major axis, radians from +X towards +Z.</summary>
        public float YawRadians { get; internal set; }

        public float SigmaMajorMeters { get; internal set; }
        public float SigmaMinorMeters { get; internal set; }

        /// <summary>Peak height of the knoll, metres; zero when not placed.</summary>
        public float HeightMeters { get; internal set; }
    }

    public sealed partial class IslandRelief
    {
        /// <summary>Every land crust site in ascending site order, placed or not.</summary>
        public IReadOnlyList<ReliefKnoll> KnollCandidates => knollCandidates;

        /// <summary>The placed knolls only, ascending by site index.</summary>
        public IReadOnlyList<ReliefKnoll> Knolls => knolls;

        private ReliefKnoll[] knollCandidates = Array.Empty<ReliefKnoll>();
        private ReliefKnoll[] knolls = Array.Empty<ReliefKnoll>();

        private float[] knollX = Array.Empty<float>();
        private float[] knollZ = Array.Empty<float>();
        private float[] knollCos = Array.Empty<float>();
        private float[] knollSin = Array.Empty<float>();
        private float[] knollInvMajorSq = Array.Empty<float>();
        private float[] knollInvMinorSq = Array.Empty<float>();
        private float[] knollHeight = Array.Empty<float>();
        private float[] knollReach = Array.Empty<float>();

        /// <summary>
        /// One candidate per land crust site; a candidate keeps a knoll when it stands on a raised plate,
        /// clear of the outer coastal band, and its block drew enough resistance.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The draw is made for every land site, from its own fork</b>
        /// (<c>Fork(terrain.relief.knoll, siteIndex)</c>), before eligibility is known, so a change in who
        /// is eligible can never shift another site's numbers.
        /// </para>
        /// <para>
        /// The knoll is elongated along the fall line of the floor it stands on - Stage 1 plus the gated
        /// plate uplift, measured by central differences at the centroid - so the swales between knolls
        /// lead downslope instead of closing into hollows.
        /// </para>
        /// </remarks>
        private void BuildKnolls(Seed.IWorldSeed worldSeed, Stage1Result stage1)
        {
            var partition = stage1.Structure.Partition;
            var landSites = stage1.Landmass.LandSites;
            var candidates = new List<ReliefKnoll>(landSites.Count);
            var placed = new List<ReliefKnoll>(landSites.Count);
            var step = Settings.KnollFallLineStepMeters;
            var jitterRadians = Settings.KnollYawJitterDegrees * Mathf.Deg2Rad;

            for (var i = 0; i < landSites.Count; i++)
            {
                var site = landSites[i];
                if (site < 0 || site >= partition.SiteCount || partition.SiteCellCount[site] <= 0)
                {
                    continue;
                }

                var rnd = worldSeed.Fork(KnollStream, site);
                var resistance = (float)rnd.NextDouble();
                var jitter = (float)rnd.NextDouble();

                var center = partition.SiteCentroid[site];
                var area = partition.SiteCellCount[site] * partition.CellAreaSqm;
                var diameter = 2f * Mathf.Sqrt(area / Mathf.PI);
                var gate = GateFor(stage1.HeightAt(center.x, center.y));
                var level = LevelAt(center.x, center.y);

                var candidate = new ReliefKnoll
                {
                    SiteIndex = site,
                    Center = center,
                    DiameterMeters = diameter,
                    Resistance = resistance,
                    Level = level,
                    Gate = gate,
                };
                candidates.Add(candidate);

                if (gate < Settings.KnollMinGate)
                {
                    candidate.Rejection = "gate";
                    continue;
                }

                if (level < Settings.KnollMinLevel)
                {
                    candidate.Rejection = "level";
                    continue;
                }

                if (resistance < 1f - Settings.KnollKeepFraction)
                {
                    candidate.Rejection = "resistance";
                    continue;
                }

                var height = Settings.KnollHeightMeters * (0.6f + 0.4f * resistance) * (0.5f + 0.5f * level);
                height = Mathf.Min(height, Settings.KnollHeightCapMeters);

                var gradientX = (FloorAt(stage1, center.x + step, center.y) - FloorAt(stage1, center.x - step, center.y))
                                / (2f * step);
                var gradientZ = (FloorAt(stage1, center.x, center.y + step) - FloorAt(stage1, center.x, center.y - step))
                                / (2f * step);
                var yaw = gradientX * gradientX + gradientZ * gradientZ > 1e-12f
                    ? Mathf.Atan2(gradientZ, gradientX)
                    : 0f;
                yaw += (2f * jitter - 1f) * jitterRadians;

                candidate.Placed = true;
                candidate.YawRadians = yaw;
                candidate.SigmaMajorMeters = Settings.KnollMajorSigmaPerDiameter * diameter;
                candidate.SigmaMinorMeters = Settings.KnollMinorSigmaPerDiameter * diameter;
                candidate.HeightMeters = height;
                placed.Add(candidate);
            }

            knollCandidates = candidates.ToArray();
            knolls = placed.ToArray();

            var n = knolls.Length;
            knollX = new float[n];
            knollZ = new float[n];
            knollCos = new float[n];
            knollSin = new float[n];
            knollInvMajorSq = new float[n];
            knollInvMinorSq = new float[n];
            knollHeight = new float[n];
            knollReach = new float[n];
            for (var i = 0; i < n; i++)
            {
                var k = knolls[i];
                knollX[i] = k.Center.x;
                knollZ[i] = k.Center.y;
                knollCos[i] = Mathf.Cos(k.YawRadians);
                knollSin[i] = Mathf.Sin(k.YawRadians);
                knollInvMajorSq[i] = 1f / (k.SigmaMajorMeters * k.SigmaMajorMeters);
                knollInvMinorSq[i] = 1f / (k.SigmaMinorMeters * k.SigmaMinorMeters);
                knollHeight[i] = k.HeightMeters;
                knollReach[i] = 3f * k.SigmaMajorMeters;
            }
        }

        /// <summary>The floor a knoll stands on: Stage 1 plus the gated plate uplift.</summary>
        private float FloorAt(Stage1Result stage1, float worldX, float worldZ)
        {
            var h1 = stage1.HeightAt(worldX, worldZ);
            return h1 + GateFor(h1) * PlateUpliftMeters * LevelAt(worldX, worldZ);
        }

        /// <summary>
        /// Knoll height at a point before the gate: the tallest of the compact kernels covering it.
        /// </summary>
        /// <remarks>
        /// <c>a (1 − q/9)³</c> with <c>q = (u/σ_major)² + (v/σ_minor)²</c>, exactly zero from three sigmas
        /// out. Combined by maximum, never summed: a point is on one knoll's slope, overlapping kernels never
        /// stack into a taller hill, and the result does not depend on the order knolls are visited in.
        /// </remarks>
        private float KnollAt(float worldX, float worldZ)
        {
            var best = 0f;
            for (var i = 0; i < knollX.Length; i++)
            {
                var dx = worldX - knollX[i];
                var dz = worldZ - knollZ[i];
                var reach = knollReach[i];
                if (dx > reach || dx < -reach || dz > reach || dz < -reach)
                {
                    continue;
                }

                var u = dx * knollCos[i] + dz * knollSin[i];
                var v = -dx * knollSin[i] + dz * knollCos[i];
                var q = u * u * knollInvMajorSq[i] + v * v * knollInvMinorSq[i];
                if (q >= 9f)
                {
                    continue;
                }

                var t = 1f - q / 9f;
                var value = knollHeight[i] * t * t * t;
                if (value > best)
                {
                    best = value;
                }
            }

            return best;
        }
    }
}
