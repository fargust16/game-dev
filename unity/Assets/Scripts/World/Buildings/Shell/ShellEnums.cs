namespace GameDev.World.Buildings.Shell
{
    public enum PlotShape
    {
        Rectangle = 0,
        Corner = 1
    }

    public enum FaceKind
    {
        Street = 0,
        Alley = 1,
        Yard = 2,
        Square = 3,
        Party = 4
    }

    public enum PlotFace
    {
        Front = 0,
        Right = 1,
        Back = 2,
        Left = 3
    }

    public enum LandmarkClass
    {
        Background = 0,
        Notable = 1,
        Landmark = 2
    }

    public enum InteriorPolicy
    {
        None = 0,
        Portal = 1
    }

    public enum ExteriorPieceCategory
    {
        Foundation = 0,
        Wall = 1,
        Corner = 2,
        Window = 3,
        Door = 4,
        Shopfront = 5,
        Blank = 6,
        RoofGable = 7,
        RoofFlat = 8,
        Chimney = 9,
        Trim = 10,
        Bay = 11,
        Stoop = 12,
        Fence = 13,
        /// <summary>Window on a step face above a lower roof (not a standard storey window).</summary>
        AtticWindow = 14
    }

    public enum RoofType
    {
        Gable = 0,
        Flat = 1
    }

    /// <summary>
    /// Silhouette via floor-stack footprints (bottom-up), not side annex masses.
    /// Poor fabric stays Box; richer districts use L / Stepped / RearWing.
    /// </summary>
    public enum MassingType
    {
        Box = 0,
        LShape = 1,
        Stepped = 2,
        RearWing = 3
    }
}
