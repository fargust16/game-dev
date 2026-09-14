using System.Collections.Generic;
using GameDev.World.TerrainGen;

namespace GameDev.World.Terrain
{
    /// <summary>
    /// The authoritative terrain relief for a seed: TerrainGen's Stage 2 structural surface, evaluable
    /// at any world point.
    /// </summary>
    /// <remarks>
    /// <para>
    /// TerrainGen owns the relief and this class only makes it answerable. It builds Stage 1, Stage 2A
    /// and the accepted massifs exactly the way the terrain generator does and then evaluates
    /// <see cref="StructuralElevation"/> — the frozen Stage 2 expression
    /// <c>stage1.HeightAt + uplift.UpliftAt(signed coast distance)</c>. Nothing here reshapes, smooths,
    /// clamps or re-derives a height, and nothing downstream ever feeds a height back into it.
    /// </para>
    /// <para>
    /// The two components are exposed separately because the surface rules need them: the massif
    /// uplift in metres is what "massif ground" means, and the signed coast distance is the world
    /// distance to the shoreline. Both are the same quantities the height is made of, so a rule that
    /// reads them cannot disagree with the relief it classifies.
    /// </para>
    /// </remarks>
    public sealed class TerrainAuthorityField
    {
        private readonly Stage1Result m_stage1;
        private readonly MassifUpliftModel m_uplift;

        /// <param name="seed">World seed.</param>
        /// <param name="includeMassifs">
        /// False evaluates the island alone (no massif uplift) — the reference surface for terrain
        /// diagnostics. Production bakes include the massifs.
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
        }

        public int Seed { get; }

        public bool IncludesMassifs { get; }

        public int MassifCount { get; }

        /// <summary>Authoritative ground height, metres. Identical to <see cref="StructuralElevation.At"/>.</summary>
        public float HeightAt(float worldX, float worldZ) =>
            StructuralElevation.At(m_stage1, m_uplift, worldX, worldZ);

        /// <summary>
        /// Height and both of its components at one point, evaluating the coast distance once.
        /// </summary>
        /// <param name="upliftMeters">Massif uplift at the point; zero on the island alone.</param>
        /// <param name="coastSignedDistanceMeters">Signed world distance to the coastline.</param>
        /// <returns>
        /// The authoritative height. Equal to <see cref="HeightAt"/> by construction: it is the same
        /// expression with the shared term evaluated once.
        /// </returns>
        public float Sample(
            float worldX, float worldZ, out float upliftMeters, out float coastSignedDistanceMeters)
        {
            var island = m_stage1.HeightAt(worldX, worldZ);
            coastSignedDistanceMeters = m_stage1.Coast.SignedDistance(worldX, worldZ);
            upliftMeters = m_uplift == null
                ? 0f
                : m_uplift.UpliftAt(worldX, worldZ, coastSignedDistanceMeters);
            return island + upliftMeters;
        }
    }
}
