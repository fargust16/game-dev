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

        /// <summary>
        /// Why it did not, when it did not: Gate, Level, Resistance, Budget or MassifBody.
        /// </summary>
        public string Rejection { get; internal set; }

        /// <summary>Direction of the major axis, radians from +X towards +Z.</summary>
        public float YawRadians { get; internal set; }

        public float SigmaMajorMeters { get; internal set; }
        public float SigmaMinorMeters { get; internal set; }

        /// <summary>Peak height of the knoll, metres; zero when not placed.</summary>
        public float HeightMeters { get; internal set; }

        /// <summary>Slope-budget amplitude cap, metres. Zero when omitted for budget.</summary>
        public float Cap { get; internal set; }

        /// <summary>Analytic upper bound of the plate term's inland descent over the support, m/m.</summary>
        public float PlateDescentBound { get; internal set; }
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
        /// clear of the outer coastal band, off the massif body, and inside the inland slope budget.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The draw is made for every land site, from its own fork</b>
        /// (<c>Fork(terrain.relief.knoll, siteIndex)</c>), before eligibility is known, so a change in who
        /// is eligible can never shift another site's numbers. Both draws are taken for every land site.
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
                    candidate.Rejection = "Gate";
                    continue;
                }

                if (level < Settings.KnollMinLevel)
                {
                    candidate.Rejection = "Level";
                    continue;
                }

                if (resistance < 1f - Settings.KnollKeepFraction)
                {
                    candidate.Rejection = "Resistance";
                    continue;
                }

                if (OnMassifBody(center.x, center.y))
                {
                    candidate.Rejection = "MassifBody";
                    continue;
                }

                var sigmaMajor = Settings.KnollMajorSigmaPerDiameter * diameter;
                var sigmaMinor = Settings.KnollMinorSigmaPerDiameter * diameter;
                var support = 3f * sigmaMajor;
                var gP = PlateDescentBound(center, support);
                candidate.PlateDescentBound = gP;
                var cap = (SlopeBudget - gP) * sigmaMinor / KnollMaxSlopeCoefficient;
                candidate.Cap = cap;
                if (cap <= 0f)
                {
                    candidate.Rejection = "Budget";
                    continue;
                }

                var height = Settings.KnollHeightMeters * (0.6f + 0.4f * resistance) * (0.5f + 0.5f * level);
                height = Mathf.Min(height, Settings.KnollHeightCapMeters, cap);

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
                candidate.SigmaMajorMeters = sigmaMajor;
                candidate.SigmaMinorMeters = sigmaMinor;
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

        private bool OnMassifBody(float worldX, float worldZ)
        {
            for (var i = 0; i < apronMassifs.Length; i++)
            {
                if (apronMassifs[i].Evaluate(worldX, worldZ) > 0f)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Analytic upper bound of the plate term's inland descent over a disc, from the pair bands
        /// and certified junction maxima the disc intersects.
        /// </summary>
        private float PlateDescentBound(Vector2 center, float supportRadius)
        {
            var bound = 0f;
            var count = siteX.Length;
            var u = PlateUpliftMeters;
            for (var k = 0; k < count; k++)
            {
                for (var j = k + 1; j < count; j++)
                {
                    var w = pairHalfWidth[k * count + j];
                    if (w <= 0.0)
                    {
                        continue;
                    }

                    var dxk = center.x - siteX[k];
                    var dzk = center.y - siteZ[k];
                    var dxj = center.x - siteX[j];
                    var dzj = center.y - siteZ[j];
                    var delta = Math.Abs((dxj * dxj + dzj * dzj) - (dxk * dxk + dzk * dzk))
                                * inverseTwoSpan[k * count + j];
                    if (delta > w + supportRadius)
                    {
                        continue;
                    }

                    var dLambda = Math.Abs(plateLevel[k] - plateLevel[j]);
                    var slope = 1.5 * u * dLambda / (2.0 * w);
                    bound = Mathf.Max(bound, (float)slope);
                }
            }

            for (var i = 0; i < junctions.Length; i++)
            {
                var radius = junctions[i].RadiusMeters + supportRadius;
                if ((junctions[i].Position - center).sqrMagnitude > radius * radius)
                {
                    continue;
                }

                var certified = junctions[i].Certificate != null
                    ? junctions[i].Certificate.MaxAdverseDerivative
                    : 0f;
                bound = Mathf.Max(bound, certified);
            }

            return Mathf.Min(SlopeBudget, Mathf.Max(0f, bound));
        }

        /// <summary>The floor a knoll stands on: Stage 1 plus M plus the gated plate uplift.</summary>
        private float FloorAt(Stage1Result stage1, float worldX, float worldZ)
        {
            var sd = stage1.Coast.SignedDistance(worldX, worldZ);
            var h1 = stage1.Model.HeightAt(sd);
            return h1 + CoastalFloorAt(sd) + GateFor(h1) * PlateUpliftMeters * LevelAt(worldX, worldZ);
        }

        /// <summary>
        /// Knoll height at a point before the gate: the tallest of the compact kernels covering it.
        /// </summary>
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
