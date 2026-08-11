namespace GameDev.World.Settlement
{
    /// <summary>
    /// Semantic zones inside a <see cref="PlotParcel"/>.
    /// <see cref="BuildingPad"/> is the lot region that becomes Shell <c>PlotFootprint</c>.
    /// </summary>
    public enum PropertyZoneKind
    {
        BuildingPad = 0,
        FrontGarden = 1,
        RearYard = 2,
        Service = 3,
        Drive = 4,
        Boundary = 5,
        /// <summary>Secondary structure pad (stable / coach house) — Elite S2.</summary>
        SecondaryPad = 6
    }
}
