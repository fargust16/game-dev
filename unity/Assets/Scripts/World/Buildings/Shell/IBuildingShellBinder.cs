namespace GameDev.World.Buildings.Shell
{
    public interface IBuildingShellBinder
    {
        ShellInstance Bind(
            BuildingDna dna,
            PlotFootprint plot,
            StreetContext street,
            BuildingArchetypeDef archetype,
            DistrictProfileDef district);
    }
}
