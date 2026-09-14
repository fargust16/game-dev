namespace GameDev.World.Terrain
{
    /// <summary>Canopy species, in tree-prototype order.</summary>
    public enum TerrainTreeSpecies
    {
        Pine = 0,
        PinePale = 1,
        Fruit = 2,
        FruitDeep = 3,
        Orchard = 4,
    }

    /// <summary>
    /// Where trees and ground cover grow, as data. Every threshold is a world quantity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The rules are the project's own. Vegetation starts above the bare shore — the island's authored
    /// vegetation floor. Grass grows "on green dry land with minimum density above zero" and reads as
    /// the same ecosystem as the ground under it, so its density follows the grass weight of the
    /// surface. Trees keep to groves rather than an even carpet and prefer the drainages; the massif
    /// carries pine, the lowland mixes fruit and pine. Nothing grows on sand, rock or sheer faces.
    /// </para>
    /// <para>
    /// <b>Measured</b> and <b>authoring default</b> have the same meaning as on
    /// <see cref="TerrainSurfaceRules"/>.
    /// </para>
    /// </remarks>
    [System.Serializable]
    public sealed class TerrainVegetationRules
    {
        // ---- shared gate -------------------------------------------------------------------------

        /// <summary><b>Measured:</b> the island's vegetation gate starts at 27.0 m world.</summary>
        public float VegetationFloorStartMeters = 27.0f;

        /// <summary><b>Measured:</b> and is full at 28.4 m world.</summary>
        public float VegetationFloorFullMeters = 28.4f;

        // ---- trees -------------------------------------------------------------------------------

        /// <summary>One candidate tree per world cell of this size. <b>Authoring default.</b></summary>
        public float TreeCellMeters = 7f;

        /// <summary>Upper bound on the chance a candidate becomes a tree. <b>Authoring default.</b></summary>
        public float TreeMaxProbability = 0.65f;

        /// <summary>Trees thin out from this slope and stop at <see cref="TreeMaxSlopeDegrees"/>. <b>Authoring default.</b></summary>
        public float TreeSlopeFadeStartDegrees = 20f;

        public float TreeMaxSlopeDegrees = 28f;

        /// <summary>Grove size and the grove mask band. <b>Authoring default.</b></summary>
        public float GroveWavelengthMeters = 140f;

        public float GroveThresholdStart = 0.42f;

        public float GroveThresholdFull = 0.62f;

        /// <summary>Drainage area over which a point counts as a drainage for grove affinity. <b>Authoring default.</b></summary>
        public float DrainageGroveStartSqm = 8000f;

        public float DrainageGroveFullSqm = 60000f;

        /// <summary>How much a drainage lifts the grove mask. <b>Authoring default.</b></summary>
        public float DrainageGroveBoost = 0.25f;

        /// <summary>Massif uplift at which the canopy becomes all pine. <b>Authoring default.</b></summary>
        public float PineUpliftStartMeters = 8f;

        public float PineUpliftFullMeters = 30f;

        /// <summary>Lowland species patches: pine where the patch noise exceeds this. <b>Authoring default.</b></summary>
        public float LowlandPineThreshold = 0.62f;

        public float SpeciesWavelengthMeters = 220f;

        /// <summary>Colour-variant shares within a family. <b>Authoring default.</b></summary>
        public float PinePaleShare = 0.3f;

        public float FruitDeepShare = 0.3f;

        public float OrchardShare = 0.15f;

        /// <summary>Uniform scale range for tree instances. <b>Authoring default.</b></summary>
        public float TreeScaleMin = 0.85f;

        public float TreeScaleMax = 1.15f;

        // ---- ground cover ------------------------------------------------------------------------

        /// <summary>
        /// Grass instances per detail cell at full grass weight. <b>Measured:</b> the island plateau
        /// carried 14–15 per cell before the migration.
        /// </summary>
        public int GrassMaxInstancesPerCell = 15;

        /// <summary>Meadow patchiness: density varies between this share and full. <b>Authoring default.</b></summary>
        public float GrassMinShare = 0.75f;

        public float GrassNoiseWavelengthMeters = 45f;

        /// <summary>Ground cover stops on slopes steeper than this. <b>Authoring default.</b></summary>
        public float GrassMaxSlopeDegrees = 35f;

        public static TerrainVegetationRules Default => new TerrainVegetationRules();
    }
}
