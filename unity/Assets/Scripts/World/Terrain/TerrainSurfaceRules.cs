namespace GameDev.World.Terrain
{
    /// <summary>Canonical surface layers, in alphamap order. The value is the layer's index.</summary>
    public enum TerrainSurfaceLayer
    {
        Grass = 0,
        Sand = 1,
        Soil = 2,
        Cliff = 3,
        Rock = 4,
    }

    /// <summary>
    /// The overworld surface classification, as data. Every threshold is a world quantity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The rules are the project's own, from <c>OVERWORLD_VISUAL_DIRECTION.md</c>: <b>shore → sand,
    /// hollows and open land → grass, drainage → soil, ridges, high massif ground and bare steep
    /// ground → rock, sheer faces → cliff</b>. Layers are applied in that priority order, each one covering the ones before
    /// it by its own mask, so the weights at a point always sum to exactly one and the highest-priority
    /// rule that holds wins.
    /// </para>
    /// <para>
    /// Values marked <b>measured</b> carry a production measurement. Values marked <b>authoring
    /// default</b> have no numeric source in the project: the rule is documented, the number is a
    /// starting point for art review, and <c>UNITY_NATIVE_TERRAIN_MIGRATION_REPORT.md</c> lists them.
    /// </para>
    /// </remarks>
    [System.Serializable]
    public sealed class TerrainSurfaceRules
    {
        // ---- shore -------------------------------------------------------------------------------

        /// <summary>
        /// Sand fully covers ground below this, including the whole seabed. <b>Measured:</b> the island's
        /// authored bare-sand band ends where its vegetation gate starts, 27.0 m world.
        /// </summary>
        public float SandFullBelowMeters = 27.0f;

        /// <summary>No shore sand above this. <b>Measured:</b> the vegetation gate is full at 28.4 m world.</summary>
        public float SandNoneAboveMeters = 28.4f;

        /// <summary>Breaks up the shore contour. <b>Authoring default.</b></summary>
        public float ShoreNoiseAmplitudeMeters = 1.0f;

        public float ShoreNoiseWavelengthMeters = 60f;

        // ---- seabed rock -------------------------------------------------------------------------

        /// <summary>
        /// Seabed rock starts on slopes this steep. <b>Measured:</b> the underwater rock the overworld had
        /// before the migration appears from about 2° of true slope and is complete by about 4°.
        /// </summary>
        public float SeabedRockSlopeStartDegrees = 2f;

        public float SeabedRockSlopeFullDegrees = 4f;

        /// <summary>
        /// Seabed rock is allowed below this and fades out up to <see cref="SeabedRockNoneAboveMeters"/>.
        /// <b>Measured:</b> the verified coastal-rock reference — rock allowed at 22 m, suppressed at 24 m,
        /// the waterline — so coastal land never receives seabed rock.
        /// </summary>
        public float SeabedRockFullBelowMeters = 22f;

        public float SeabedRockNoneAboveMeters = 24f;

        // ---- soil --------------------------------------------------------------------------------

        /// <summary>Soil starts where this much ground drains through a point. <b>Authoring default.</b></summary>
        public float SoilDrainageStartSqm = 20000f;

        public float SoilDrainageFullSqm = 150000f;

        /// <summary>Soil does not hold on slopes steeper than this. <b>Authoring default.</b></summary>
        public float SoilMaxSlopeDegrees = 30f;

        // ---- massif rock -------------------------------------------------------------------------

        /// <summary>
        /// Massif uplift, metres, over which ground counts as massif ground. Uplift is TerrainGen's own
        /// massif contribution to the height. <b>Authoring default.</b>
        /// </summary>
        public float MassifUpliftStartMeters = 2f;

        public float MassifUpliftFullMeters = 25f;

        /// <summary>Massif ground turns to rock with height ("grass → rock by height"). <b>Authoring default.</b></summary>
        public float RockHeightStartMeters = 110f;

        public float RockHeightFullMeters = 230f;

        public float RockHeightNoiseMeters = 20f;

        public float RockHeightNoiseWavelengthMeters = 90f;

        /// <summary>
        /// Height above the local mean (<see cref="OverworldTerrainField.ConvexityRadiusMeters"/>) that reads
        /// as a ridge or crest on massif ground. <b>Authoring default.</b>
        /// </summary>
        public float RidgeConvexityStartMeters = 1.5f;

        public float RidgeConvexityFullMeters = 5f;

        // ---- steep bare ground ---------------------------------------------------------------------

        /// <summary>
        /// Dry ground steeper than this cannot hold soil or grass and reads as bare rock
        /// ("grass → rock by height and slope"). The start is the angle of repose of loose ground, about
        /// 35°. <b>Authoring default</b> with a physical basis.
        /// </summary>
        public float SteepRockSlopeStartDegrees = 35f;

        public float SteepRockSlopeFullDegrees = 42f;

        // ---- cliff -------------------------------------------------------------------------------

        /// <summary>
        /// Sheer faces on dry land. Reserved for faces well past the angle of repose, so moderately steep
        /// ground stays rock. <b>Authoring default.</b> Measured context: the massif flanks of the
        /// authoritative relief run 44–61° (p10–p90), so this band decides how much of a flank reads as
        /// cliff rather than rock.
        /// </summary>
        public float CliffSlopeStartDegrees = 50f;

        public float CliffSlopeFullDegrees = 58f;

        public static TerrainSurfaceRules Default => new TerrainSurfaceRules();
    }
}
