using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>Stage 4A relief at one point, with the parts it is made of.</summary>
    /// <remarks>
    /// <see cref="Plate"/> and <see cref="Knoll"/> are the ungated terms, so that
    /// <c>Total == Gate * (Plate + Knoll) + Apron</c> holds as written; the apron is already a fill against
    /// the gated apron surface and carries its own gate.
    /// </remarks>
    public readonly struct IslandReliefSample
    {
        /// <summary>Shore gate Γ: 0 at and seaward of the shoreline, 1 from the top of the coastal rise.</summary>
        public float Gate { get; }

        /// <summary>Plate level Λ in [0, 1].</summary>
        public float PlateLevel { get; }

        /// <summary>Plate-block uplift P before the gate, metres.</summary>
        public float Plate { get; }

        /// <summary>Knoll height K before the gate, metres.</summary>
        public float Knoll { get; }

        /// <summary>Apron fill A against the massif uplift, metres. Never negative.</summary>
        public float Apron { get; }

        /// <summary>R4A, metres.</summary>
        public float Total { get; }

        public IslandReliefSample(float gate, float plateLevel, float plate, float knoll, float apron, float total)
        {
            Gate = gate;
            PlateLevel = plateLevel;
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
    /// Three positive terms, each caused by one structure TerrainGen already publishes and each living at
    /// one scale (PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md §4):
    /// </para>
    /// <list type="bullet">
    /// <item><b>plate-block uplift</b> - every macro plate stands at a level set by its role in the island's
    /// collision; uplands and lowlands are the plates;</item>
    /// <item><b>knolls</b> - the resistant cores of selected crust blocks, on raised plates only;</item>
    /// <item><b>massif apron</b> - talus and piedmont filled against the foot of the Stage 2B body.</item>
    /// </list>
    /// <code>
    /// R4A = Γ · (P + K) + A        Γ = smoothstep(clamp01((H1 − SeaLevel) / CoastalRiseMeters))
    /// A   = SmoothMax(U2, Γ · P_A, ApronBlendMeters) − U2
    /// </code>
    /// <para>
    /// <b>Nothing here lowers land.</b> Every term is non-negative and Γ is exactly zero wherever Stage 1
    /// puts the ground at or below the sea, so the coastline, the ocean and every Stage 1-3 structure are
    /// read and never moved. Lowlands are the plates left unraised; genuinely lowered ground is a declared
    /// basin and belongs to Stage 3F.
    /// </para>
    /// <para>
    /// <b>A pure function of position.</b> <see cref="At"/> reads the world coordinate, the caller's
    /// already-evaluated <c>H1</c> and <c>U2</c>, and immutable structure built once by <see cref="Build"/>.
    /// The lattice evaluator calls the same function per sample, so a point query and the bulk field agree
    /// to the bit. Nothing knows tiles exist.
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

        public IslandReliefSettings Settings { get; }

        /// <summary>Whether at least one massif was certified on this world.</summary>
        public bool HasMassif { get; }

        /// <summary>The multiplier the plate uplift runs at: 1, or the no-massif scale.</summary>
        public float WorldUpliftScale { get; }

        /// <summary>Plate uplift at level 1 on this world, metres.</summary>
        public float PlateUpliftMeters => Settings.PlateUpliftMeters * WorldUpliftScale;

        private readonly float seaLevel;
        private readonly float coastalRise;

        private IslandRelief(
            IslandReliefSettings settings, TerrainGenSettings terrainSettings, IReadOnlyList<MountainMassif> massifs)
        {
            Settings = settings;
            HasMassif = massifs != null && massifs.Count > 0;
            WorldUpliftScale = HasMassif ? 1f : settings.NoMassifUpliftScale;
            seaLevel = WorldScale.SeaLevelMeters;
            coastalRise = Mathf.Max(1e-3f, terrainSettings.CoastalRiseMeters);
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
            relief.BuildPlates(stage1, plates);
            relief.BuildApron(massifs);

            // Knolls last: their eligibility and their fall line read the finished level field.
            relief.BuildKnolls(worldSeed, stage1);
            return relief;
        }

        /// <summary>
        /// Relief at a world point, metres: non-negative, and exactly zero at and seaward of the shore.
        /// </summary>
        /// <param name="stage1Height">Stage 1 height at the point, <c>H1</c>.</param>
        /// <param name="upliftMeters">Massif uplift at the point, <c>U2</c>; zero off the massif.</param>
        public float At(float worldX, float worldZ, float stage1Height, float upliftMeters)
        {
            var gate = GateFor(stage1Height);
            if (gate <= 0f)
            {
                // Γ = 0 zeroes the floor terms and the apron surface, and a fill against a zero surface
                // is zero; Sample returns the same total through the full expression.
                return 0f;
            }

            return Evaluate(worldX, worldZ, stage1Height, upliftMeters, gate, out _, out _, out _, out _);
        }

        /// <summary>The relief at a point with its parts. Same total as <see cref="At"/>, to the bit.</summary>
        public IslandReliefSample Sample(float worldX, float worldZ, float stage1Height, float upliftMeters)
        {
            var gate = GateFor(stage1Height);
            var total = Evaluate(
                worldX, worldZ, stage1Height, upliftMeters, gate,
                out var level, out var plate, out var knoll, out var apron);
            return new IslandReliefSample(gate, level, plate, knoll, apron, total);
        }

        /// <summary>
        /// The relief on a whole lattice, one <see cref="At"/> per sample.
        /// </summary>
        /// <param name="stage1Field">Stage 1 heights on the lattice.</param>
        /// <param name="upliftField">Massif uplift on the same lattice, or null for none.</param>
        public WorldHeightField EvaluateField(WorldHeightField stage1Field, WorldHeightField upliftField)
        {
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
                        stage1Field.WorldX(x), worldZ, stage1Field.Heights[index],
                        upliftField != null ? upliftField.Heights[index] : 0f);
                }
            }

            return relief;
        }

        /// <summary>Shore gate for a Stage 1 height.</summary>
        /// <remarks>
        /// Follows Stage 1's own coastal rise, so the relief fades in exactly where the datum does and
        /// nowhere else. <c>H1 ≤ SeaLevel</c> at and seaward of the shoreline, which makes the gate - and
        /// with it the whole relief - exactly zero there by construction.
        /// </remarks>
        private float GateFor(float stage1Height)
        {
            var u = Mathf.Clamp01((stage1Height - seaLevel) / coastalRise);
            return u * u * (3f - 2f * u);
        }

        private float Evaluate(
            float worldX,
            float worldZ,
            float stage1Height,
            float upliftMeters,
            float gate,
            out float level,
            out float plate,
            out float knoll,
            out float apron)
        {
            level = LevelAt(worldX, worldZ);
            plate = PlateUpliftMeters * level;
            knoll = KnollAt(worldX, worldZ);
            apron = ApronFill(worldX, worldZ, upliftMeters, gate);
            return gate * (plate + knoll) + apron;
        }

        private static float Smooth01(float u)
        {
            u = Mathf.Clamp01(u);
            return u * u * (3f - 2f * u);
        }
    }
}
