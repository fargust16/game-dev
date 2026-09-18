using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>Stage 4A relief at one point, with the parts it is made of.</summary>
    /// <remarks>
    /// <see cref="Plate"/> and <see cref="Knoll"/> are the ungated terms, so that
    /// <c>Total == CoastalFloor + Gate * (Plate + Knoll) + Apron</c> holds as written; the apron is
    /// already a fill against the gated apron surface and carries its own gate.
    /// </remarks>
    public readonly struct IslandReliefSample
    {
        /// <summary>Shore gate Γ: 0 at and seaward of the shoreline, 1 from the top of the coastal rise.</summary>
        public float Gate { get; }

        /// <summary>Plate level Λ in [0, 1].</summary>
        public float PlateLevel { get; }

        /// <summary>Coastal-floor term M(d), metres. Zero at and seaward of the shore.</summary>
        public float CoastalFloor { get; }

        /// <summary>Plate-block uplift P before the gate, metres.</summary>
        public float Plate { get; }

        /// <summary>Knoll height K before the gate, metres.</summary>
        public float Knoll { get; }

        /// <summary>Apron fill A against the massif uplift, metres. Never negative.</summary>
        public float Apron { get; }

        /// <summary>R4A, metres.</summary>
        public float Total { get; }

        public IslandReliefSample(
            float gate, float plateLevel, float coastalFloor, float plate, float knoll, float apron, float total)
        {
            Gate = gate;
            PlateLevel = plateLevel;
            CoastalFloor = coastalFloor;
            Plate = plate;
            Knoll = knoll;
            Apron = apron;
            Total = total;
        }
    }

    /// <summary>
    /// Stage 4A: the island-floor relief added on top of the frozen structural surface.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Revision R1. Production composition:
    /// </para>
    /// <code>
    /// H4A = H1 + U2 + R4A
    /// R4A = M(d) + Γ · (P + K) + A
    /// M(d) = ∫₀^d max(0, τ − H1'(u)) du     d &gt; 0;  0 at and seaward of the shore
    /// τ    = InteriorRiseMeters / InteriorScaleMeters
    /// Γ    = smoothstep(clamp01((H1 − SeaLevel) / CoastalRiseMeters))
    /// A    = SmoothMax(U2, Γ · P_A, ApronBlendMeters) − U2
    /// </code>
    /// <para>
    /// <b>Nothing here lowers land.</b> Every term is non-negative and Γ and M are exactly zero wherever
    /// Stage 1 puts the ground at or below the sea, so the coastline, the ocean and every Stage 1–3
    /// structure are read and never moved. Lowlands are the plates left unraised; genuinely lowered
    /// ground is a declared basin and belongs to Stage 3F.
    /// </para>
    /// <para>
    /// <b>A pure function of position.</b> <see cref="At"/> reads the world coordinate, the caller's
    /// already-evaluated signed coast distance, <c>H1</c> and <c>U2</c>, and immutable structure built
    /// once by <see cref="Build"/>. The lattice evaluator calls the same function per sample, so a
    /// point query and the bulk field agree to the bit. Nothing knows tiles exist.
    /// </para>
    /// <para>
    /// <b>Not on the production path yet.</b> <c>TerrainAuthorityField</c> still evaluates
    /// <see cref="StructuralElevation"/>; this stage is validated through its dump and tests first.
    /// </para>
    /// </remarks>
    public sealed partial class IslandRelief
    {
        /// <summary>The only stream Stage 4A draws from, forked per crust site index.</summary>
        public const string KnollStream = "terrain.relief.knoll";

        /// <summary>Step of the 1-D coastal-floor table, metres.</summary>
        public const float CoastalFloorStepMeters = 0.5f;

        /// <summary>
        /// Maximum directional slope of <c>a (1 − q/9)³</c> along the minor axis, as a multiple of
        /// <c>a / σ_minor</c>.
        /// </summary>
        public const float KnollMaxSlopeCoefficient = 0.5726f;

        public IslandReliefSettings Settings { get; }

        /// <summary>Whether at least one massif was certified on this world.</summary>
        public bool HasMassif { get; }

        /// <summary>The multiplier the plate uplift runs at: 1, or the no-massif scale.</summary>
        public float WorldUpliftScale { get; }

        /// <summary>Plate uplift at level 1 on this world, metres.</summary>
        public float PlateUpliftMeters => Settings.PlateUpliftMeters * WorldUpliftScale;

        /// <summary>The inland slope budget τ this world was built with, m/m.</summary>
        public float SlopeBudget { get; private set; }

        /// <summary>Share of island land assigned level 0 by the lowland reservation.</summary>
        public float ReservedLandShare { get; private set; }

        /// <summary>How many districts the reservation put at level 0.</summary>
        public int ReservedDistrictCount { get; private set; }

        private readonly float seaLevel;
        private readonly float coastalRise;
        private CoastQuery coast;
        private CoastHeightModel heightModel;

        private float[] coastalFloor = Array.Empty<float>();
        private float coastalFloorStep = CoastalFloorStepMeters;

        private IslandRelief(
            IslandReliefSettings settings, TerrainGenSettings terrainSettings, IReadOnlyList<MountainMassif> massifs)
        {
            Settings = settings;
            HasMassif = massifs != null && massifs.Count > 0;
            WorldUpliftScale = HasMassif ? 1f : settings.NoMassifUpliftScale;
            seaLevel = WorldScale.SeaLevelMeters;
            coastalRise = Mathf.Max(1e-3f, terrainSettings.CoastalRiseMeters);
            SlopeBudget = settings.InlandSlopeBudget;
        }

        /// <summary>
        /// Builds every structure the relief is evaluated from. Reads its inputs; mutates none of them.
        /// </summary>
        /// <param name="worldSeed">World seed; only <see cref="KnollStream"/> is forked.</param>
        /// <param name="stage1">Coast datum, crust partition and coastline.</param>
        /// <param name="plates">The macro plates Stage 2A built from this Stage 1.</param>
        /// <param name="massifs">Certified massifs, possibly none.</param>
        /// <param name="settings">Stage 4A numbers; clamped here.</param>
        public static IslandRelief Build(
            Seed.IWorldSeed worldSeed,
            Stage1Result stage1,
            MacroPlates plates,
            IReadOnlyList<MountainMassif> massifs,
            IslandReliefSettings settings)
        {
            settings.Clamp();

            var relief = new IslandRelief(settings, stage1.Settings, massifs);
            relief.BuildCoastalFloor(stage1);
            relief.BuildPlates(stage1, plates);
            relief.BuildApron(massifs);

            // Knolls last: their eligibility and their fall line read the finished level field.
            relief.BuildKnolls(worldSeed, stage1);
            return relief;
        }

        /// <summary>
        /// Relief at a world point, metres: non-negative, and exactly zero at and seaward of the shore.
        /// </summary>
        /// <param name="signedDistance">Stage 1 signed coast distance at the point, metres, positive inland.</param>
        /// <param name="stage1Height">Stage 1 height at the point, <c>H1</c>.</param>
        /// <param name="upliftMeters">Massif uplift at the point, <c>U2</c>; zero off the massif.</param>
        public float At(
            float worldX, float worldZ, float signedDistance, float stage1Height, float upliftMeters)
        {
            var floor = AppliedCoastalFloor(signedDistance, stage1Height);
            var gate = GateFor(stage1Height);
            if (gate <= 0f)
            {
                return floor;
            }

            return Evaluate(
                worldX, worldZ, upliftMeters, gate, floor,
                out _, out _, out _, out _);
        }

        /// <summary>The relief at a point with its parts. Same total as <see cref="At"/>, to the bit.</summary>
        public IslandReliefSample Sample(
            float worldX, float worldZ, float signedDistance, float stage1Height, float upliftMeters)
        {
            var floor = AppliedCoastalFloor(signedDistance, stage1Height);
            var gate = GateFor(stage1Height);
            var total = Evaluate(
                worldX, worldZ, upliftMeters, gate, floor,
                out var level, out var plate, out var knoll, out var apron);
            return new IslandReliefSample(gate, level, floor, plate, knoll, apron, total);
        }

        /// <summary>
        /// The relief on a whole lattice, one <see cref="At"/> per sample.
        /// </summary>
        /// <param name="stage1Field">Stage 1 heights on the lattice.</param>
        /// <param name="upliftField">Massif uplift on the same lattice, or null for none.</param>
        /// <param name="signedDistance">Stage 1 signed coast distance on the same lattice.</param>
        public WorldHeightField EvaluateField(
            WorldHeightField stage1Field, WorldHeightField upliftField, float[] signedDistance)
        {
            if (signedDistance == null || signedDistance.Length != stage1Field.SampleCount)
            {
                throw new ArgumentException(
                    "Signed-distance lattice must match the Stage 1 field.", nameof(signedDistance));
            }

            var relief = new WorldHeightField(
                stage1Field.OriginX, stage1Field.OriginZ, stage1Field.Width, stage1Field.Height,
                stage1Field.CellSize, new float[stage1Field.SampleCount]);

            for (var z = 0; z < stage1Field.Height; z++)
            {
                var worldZ = stage1Field.WorldZ(z);
                var row = z * stage1Field.Width;
                for (var x = 0; x < stage1Field.Width; x++)
                {
                    var index = row + x;
                    relief.Heights[index] = At(
                        stage1Field.WorldX(x), worldZ, signedDistance[index], stage1Field.Heights[index],
                        upliftField != null ? upliftField.Heights[index] : 0f);
                }
            }

            return relief;
        }

        /// <summary>
        /// Applied coastal-floor term: the 1-D table <see cref="CoastalFloorAt"/>, forced to zero
        /// wherever Stage 1 is at or below the sea so M cannot move the coastline.
        /// </summary>
        private float AppliedCoastalFloor(float signedDistance, float stage1Height) =>
            stage1Height <= seaLevel ? 0f : CoastalFloorAt(signedDistance);

        /// <summary>Coastal-floor term M at a signed coast distance, metres.</summary>
        public float CoastalFloorAt(float signedDistance)
        {
            if (signedDistance <= 0f || coastalFloor.Length == 0)
            {
                return 0f;
            }

            var t = signedDistance / coastalFloorStep;
            var last = coastalFloor.Length - 1;
            if (t >= last)
            {
                return coastalFloor[last] + SlopeBudget * (signedDistance - last * coastalFloorStep);
            }

            var i = (int)t;
            var f = t - i;
            return coastalFloor[i] + (coastalFloor[i + 1] - coastalFloor[i]) * f;
        }

        /// <summary>
        /// Whether the point sits in the massif influence zone Z_M: massif uplift or ungated apron surface.
        /// </summary>
        public bool InfluenceZone(float worldX, float worldZ, float upliftMeters) =>
            upliftMeters > 0f || ApronSurfaceAt(worldX, worldZ) > 0f;

        /// <summary>Pair half-width w_kj, metres. Zero when either index is out of range.</summary>
        public float PairHalfWidth(int k, int j)
        {
            var count = siteX.Length;
            if ((uint)k >= (uint)count || (uint)j >= (uint)count || k == j)
            {
                return 0f;
            }

            return (float)pairHalfWidth[k * count + j];
        }

        /// <summary>Shore gate for a Stage 1 height.</summary>
        /// <remarks>
        /// Follows Stage 1's own coastal rise, so the relief fades in exactly where the datum does and
        /// nowhere else. <c>H1 ≤ SeaLevel</c> at and seaward of the shoreline, which makes the gate - and
        /// with it every gated term - exactly zero there by construction.
        /// </remarks>
        private float GateFor(float stage1Height)
        {
            var u = Mathf.Clamp01((stage1Height - seaLevel) / coastalRise);
            return u * u * (3f - 2f * u);
        }

        private float Evaluate(
            float worldX,
            float worldZ,
            float upliftMeters,
            float gate,
            float floor,
            out float level,
            out float plate,
            out float knoll,
            out float apron)
        {
            level = LevelAt(worldX, worldZ);
            plate = PlateUpliftMeters * level;
            knoll = KnollAt(worldX, worldZ);
            apron = ApronFill(worldX, worldZ, upliftMeters, gate);
            return floor + gate * (plate + knoll) + apron;
        }

        /// <summary>
        /// M(d) as a 1-D table: cumulative <c>max(0, τ − H1')</c> at <see cref="CoastalFloorStepMeters"/>.
        /// </summary>
        private void BuildCoastalFloor(Stage1Result stage1)
        {
            coast = stage1.Coast;
            heightModel = stage1.Model;
            var model = stage1.Model;
            var maxD = Mathf.Max(coastalFloorStep, stage1.Settings.MaxCoastDistanceMeters);
            var n = Mathf.Max(1, Mathf.CeilToInt(maxD / coastalFloorStep));
            coastalFloor = new float[n + 1];
            var prevH = model.HeightAt(0f);
            for (var i = 1; i <= n; i++)
            {
                var d = i * coastalFloorStep;
                var h = model.HeightAt(d);
                var h1p = (h - prevH) / coastalFloorStep;
                coastalFloor[i] = coastalFloor[i - 1] + Mathf.Max(0f, SlopeBudget - h1p) * coastalFloorStep;
                prevH = h;
            }
        }

        private static float Smooth01(float u)
        {
            u = Mathf.Clamp01(u);
            return u * u * (3f - 2f * u);
        }
    }
}
