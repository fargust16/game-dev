using System;
using GameDev.World.Seed;
using Random = System.Random;

namespace GameDev.World.Buildings.Shell
{
    public sealed class BuildingShellBinder : IBuildingShellBinder
    {
        private readonly ShellGrammarAssembler _assembler = new ShellGrammarAssembler();

        public ShellInstance Bind(
            BuildingDna dna,
            PlotFootprint plot,
            StreetContext street,
            BuildingArchetypeDef archetype,
            DistrictProfileDef district)
        {
            var stream = $"shell_{dna.PlotIndex}_{dna.ArchetypeId}_{dna.DistrictId}";
            var rng = new WorldSeed(dna.Seed).Fork(stream);
            return _assembler.Assemble(dna, plot, street, archetype, district, rng);
        }

        public ShellInstance BindWithRandom(
            BuildingDna dna,
            PlotFootprint plot,
            StreetContext street,
            BuildingArchetypeDef archetype,
            DistrictProfileDef district,
            Random rng)
        {
            return _assembler.Assemble(dna, plot, street, archetype, district, rng);
        }
    }
}
