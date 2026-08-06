using System.Collections.Generic;

namespace GameDev.World.Town
{
    /// <summary>
    /// Coded starter interior library for D1.1 (headless-safe, no ScriptableObject assets).
    /// ASCII rows: top row = north (high local Z); <c>D</c> = door on street (local −Z) edge.
    /// </summary>
    public static class BuildingTemplateCatalog
    {
        private static BuildingTemplate[] _all;

        public static IReadOnlyList<BuildingTemplate> All
        {
            get
            {
                EnsureBuilt();
                return _all;
            }
        }

        public static BuildingTemplate Get(BuildingTemplateId id)
        {
            if (id == BuildingTemplateId.None)
            {
                return null;
            }

            EnsureBuilt();
            for (var i = 0; i < _all.Length; i++)
            {
                if (_all[i].Id == id)
                {
                    return _all[i];
                }
            }

            return null;
        }

        private static void EnsureBuilt()
        {
            if (_all != null)
            {
                return;
            }

            _all = new[]
            {
                FromAscii(
                    BuildingTemplateId.PeasantHouse,
                    "Peasant House",
                    new[]
                    {
                        "WWWWWW",
                        "WFFFFW",
                        "WFPFFW",
                        "WFFFFW",
                        "WFFNFW",
                        "WFFFFW",
                        "WFFFFW",
                        "WWWDWW"
                    },
                    new[] { BuildingKind.Residential },
                    new[] { WealthClass.Poor, WealthClass.Mixed }),
                FromAscii(
                    BuildingTemplateId.WorkerHouse,
                    "Worker House",
                    new[]
                    {
                        "WWWWW",
                        "WFFFW",
                        "WFPFW",
                        "WFFFW",
                        "WFFFW",
                        "WFNFW",
                        "WFFFW",
                        "WFFFW",
                        "WFFFW",
                        "WWDWW"
                    },
                    new[] { BuildingKind.Residential },
                    new[] { WealthClass.Poor, WealthClass.Mixed }),
                FromAscii(
                    BuildingTemplateId.Tavern,
                    "Tavern",
                    new[]
                    {
                        "WWWWWWWW",
                        "WFFFFFFW",
                        "WFFPPFFW",
                        "WFFFFFFW",
                        "WFFWWFFW",
                        "WFFWWFFW",
                        "WFFFFFFW",
                        "WFNNFFFW",
                        "WFFFFFFW",
                        "WFFFFFFW",
                        "WFFFFFFW",
                        "WWWDWWWW"
                    },
                    new[] { BuildingKind.Commercial },
                    new[] { WealthClass.Mixed, WealthClass.Poor }),
                FromAscii(
                    BuildingTemplateId.Shop,
                    "Shop",
                    new[]
                    {
                        "WWWWWW",
                        "WFFFFW",
                        "WFPPFW",
                        "WFFFFW",
                        "WFFFFW",
                        "WFNFFW",
                        "WFFFFW",
                        "WWWDWW"
                    },
                    new[] { BuildingKind.Commercial, BuildingKind.Industrial },
                    null),
                FromAscii(
                    BuildingTemplateId.SmallChurch,
                    "Small Church",
                    new[]
                    {
                        "WWWWWWWWWW",
                        "WFFFFFFFFW",
                        "WFFFFFFFFW",
                        "WFFWWWWFFW",
                        "WFFWNNWFFW",
                        "WFFWFFWFFW",
                        "WFFWFFWFFW",
                        "WFFFFFFFFW",
                        "WFFFFFFFFW",
                        "WFFPPPPFFW",
                        "WFFFFFFFFW",
                        "WFFFFFFFFW",
                        "WFFFFFFFFW",
                        "WWWWWDWWWW"
                    },
                    new[] { BuildingKind.Religious, BuildingKind.Civic },
                    new[] { WealthClass.Rich, WealthClass.Mixed, WealthClass.Poor })
            };
        }

        /// <summary>
        /// Parse north-first ASCII rows into a template. Legend: W wall, F floor, D door,
        /// P prop socket, N NPC socket, . or space empty.
        /// </summary>
        public static BuildingTemplate FromAscii(
            BuildingTemplateId id,
            string displayName,
            string[] northFirstRows,
            BuildingKind[] compatibleKinds,
            WealthClass[] preferredWealth)
        {
            if (northFirstRows == null || northFirstRows.Length == 0)
            {
                throw new System.ArgumentException("Template needs at least one row.", nameof(northFirstRows));
            }

            var height = northFirstRows.Length;
            var width = northFirstRows[0].Length;
            for (var r = 1; r < height; r++)
            {
                if (northFirstRows[r] == null || northFirstRows[r].Length != width)
                {
                    throw new System.ArgumentException(
                        $"Row {r} width mismatch for template {id}.",
                        nameof(northFirstRows));
                }
            }

            var cells = new BuildingInteriorCellKind[width * height];
            for (var row = 0; row < height; row++)
            {
                // ASCII top = north = high local Z.
                var y = height - 1 - row;
                var line = northFirstRows[row];
                for (var x = 0; x < width; x++)
                {
                    cells[y * width + x] = ParseCell(line[x]);
                }
            }

            return new BuildingTemplate
            {
                Id = id,
                DisplayName = displayName,
                Width = width,
                Height = height,
                CellSize = 1f,
                Cells = cells,
                CompatibleKinds = compatibleKinds ?? System.Array.Empty<BuildingKind>(),
                PreferredWealth = preferredWealth ?? System.Array.Empty<WealthClass>()
            };
        }

        private static BuildingInteriorCellKind ParseCell(char c)
        {
            switch (c)
            {
                case 'W':
                case 'w':
                case '#':
                    return BuildingInteriorCellKind.Wall;
                case 'F':
                case 'f':
                case '.':
                    return BuildingInteriorCellKind.Floor;
                case 'D':
                case 'd':
                    return BuildingInteriorCellKind.Door;
                case 'P':
                case 'p':
                    return BuildingInteriorCellKind.PropSocket;
                case 'N':
                case 'n':
                    return BuildingInteriorCellKind.NpcSocket;
                default:
                    return BuildingInteriorCellKind.Empty;
            }
        }
    }
}
