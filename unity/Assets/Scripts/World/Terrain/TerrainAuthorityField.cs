using System.Collections.Generic;
using GameDev.World.TerrainGen;

namespace GameDev.World.Terrain
{
    /// <summary>
    /// The authoritative terrain relief for a seed: TerrainGen's island surface through Stage 4A,
    /// evaluable at any world point.
    /// </summary>
    /// <remarks>
    /// <para>
    /// TerrainGen owns the relief and this class only makes it answerable. It builds Stage 1, Stage 2A,
    /// the accepted massifs and the Stage 4A island relief exactly the way the terrain generator does,
    /// and then evaluates <see cref="IslandElevation"/> — <c>H4A = H1 + U2 + R4A</c>. Nothing here
    /// reshapes, smooths, clamps or re-derives a height, and nothing downstream ever feeds a height
    /// back into it.
    /// </para>
    /// <para>
    /// <b>Stage 4A is additive and gated.</b> <c>R4A</c> is non-negative everywhere and exactly zero at
    /// and seaward of the shoreline, so the land mask, the coastline and every Stage 1-3E structure are
    /// read and never moved. Stage 3D/3E still declare their basins and outlets on
    /// <see cref="StructuralElevation"/> (<c>H2</c>): that is the surface their topology was frozen
    /// against, and the future Stage 3F carve composes after this one as <c>H = H4A - C3F</c>.
    /// </para>
    /// <para>
    /// The two components are exposed separately because the surface rules need them: the massif
    /// uplift in metres is what "massif ground" means, and the signed coast distance is the world
    /// distance to the shoreline. Both are quantities the height is made of, so a rule that reads them
    /// cannot disagree with the relief it classifies. Stage 4A relief is deliberately not one of them —
    /// it is island floor, not massif.
    /// </para>
    /// <para>
    /// <b>Island-only.</b> <c>includeMassifs: false</c> is the diagnostic reference surface and stays
    /// Stage 1 alone: without Stage 2A there are no macro plates for Stage 4A to stand on, and building
    /// the relief against an emptied massif list on a world that certified one is outside the envelope
    /// Stage 4A was accepted on. Production bakes include the massifs and therefore Stage 4A.
    /// </para>
    /// </remarks>
    public sealed class TerrainAuthorityField
    {
        private readonly Stage1Result m_stage1;
        private readonly MassifUpliftModel m_uplift;
        private readonly IslandRelief m_relief;

        /// <param name="seed">World seed.</param>
        /// <param name="includeMassifs">
        /// False evaluates Stage 1 alone (no massif uplift, no Stage 4A relief) — the reference surface
        /// for terrain diagnostics. Production bakes include the massifs and Stage 4A.
        /// </param>
        public TerrainAuthorityField(int seed, bool includeMassifs = true)
        {
            Seed = seed;
            IncludesMassifs = includeMassifs;

            var terrain = TerrainGenSettings.Default;
            m_stage1 = Stage1LandmassGenerator.Generate(seed, terrain);

            if (!includeMassifs)
            {
                return;
            }

            var macro = MacroStructureSettings.Default;
            var massifSettings = MassifSettings.Default;
            var stage2A = Stage2AMacroStructure.Run(seed, terrain, macro);
            var worldSeed = new GameDev.World.Seed.WorldSeed(seed);

            var massifs = new List<MountainMassif>();
            var accepted = stage2A.AcceptedAxes();
            for (var i = 0; i < accepted.Count; i++)
            {
                massifs.Add(MountainMassif.Build(
                    worldSeed, massifs.Count, accepted[i], terrain, massifSettings, accepted[i].CertifiedCore));
            }

            MassifCount = massifs.Count;
            m_uplift = new MassifUpliftModel(massifs, massifSettings);

            // Stage 4A reads the same Stage 1, the same macro plates and the same massifs the stages
            // above were built from, so the relief is a function of this seed and nothing else.
            m_relief = IslandRelief.Build(
                worldSeed, m_stage1, stage2A.Plates, massifs, IslandReliefSettings.Default);
        }

        public int Seed { get; }

        public bool IncludesMassifs { get; }

        public int MassifCount { get; }

        /// <summary>Authoritative ground height, metres. Identical to <see cref="IslandElevation.At"/>.</summary>
        public float HeightAt(float worldX, float worldZ) =>
            IslandElevation.At(m_stage1, m_uplift, m_relief, worldX, worldZ);

        /// <summary>
        /// Height and the two components the surface rules read, evaluating the coast distance once.
        /// </summary>
        /// <param name="upliftMeters">
        /// Massif uplift at the point; zero on the island alone. This is <c>U2</c> only — Stage 4A
        /// relief is island floor and is not massif ground.
        /// </param>
        /// <param name="coastSignedDistanceMeters">Signed world distance to the coastline.</param>
        /// <returns>
        /// The authoritative height. Equal to <see cref="HeightAt"/> by construction: the same terms
        /// in the same order as <see cref="IslandElevation.At"/>, with the shared coast distance,
        /// <c>H1</c> and <c>U2</c> evaluated once and handed to the relief rather than recomputed.
        /// </returns>
        public float Sample(
            float worldX, float worldZ, out float upliftMeters, out float coastSignedDistanceMeters)
        {
            coastSignedDistanceMeters = m_stage1.Coast.SignedDistance(worldX, worldZ);
            var island = m_stage1.Model.HeightAt(coastSignedDistanceMeters);
            upliftMeters = m_uplift == null
                ? 0f
                : m_uplift.UpliftAt(worldX, worldZ, coastSignedDistanceMeters);
            var structural = island + upliftMeters;
            return m_relief == null
                ? structural
                : structural + m_relief.At(
                    worldX, worldZ, coastSignedDistanceMeters, island, upliftMeters);
        }
    }
}
