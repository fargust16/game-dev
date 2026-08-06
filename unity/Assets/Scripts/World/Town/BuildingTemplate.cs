using System.Collections.Generic;

namespace GameDev.World.Town
{
    /// <summary>Local cell paint for an enterable building schematic (Milestone D1).</summary>
    public enum BuildingInteriorCellKind : byte
    {
        Empty = 0,
        Floor = 1,
        Wall = 2,
        Door = 3,
        PropSocket = 4,
        NpcSocket = 5
    }

    /// <summary>Catalog id for a reusable interior schematic. Append-only for save stability.</summary>
    public enum BuildingTemplateId : byte
    {
        None = 0,
        PeasantHouse = 1,
        WorkerHouse = 2,
        Tavern = 3,
        Shop = 4,
        SmallChurch = 5,
        Cottage = 6,
        AlleyShop = 7,
        Booth = 8,
        Apothecary = 9,
        NobleHouse = 10,
        MarketHall = 11,
        Garrison = 12,
        Station = 13,
        School = 14,
        DeadHouse = 15,
        University = 16
    }

    /// <summary>
    /// Schematic interior: rectangular cell grid in local space.
    /// Local +X = width, local +Z = depth; door faces local −Z (street side).
    /// </summary>
    public sealed class BuildingTemplate
    {
        public BuildingTemplateId Id;
        public string DisplayName;
        public int Width;
        public int Height;
        public float CellSize = 1f;
        public BuildingInteriorCellKind[] Cells;
        public BuildingKind[] CompatibleKinds;
        public WealthClass[] PreferredWealth;

        /// <summary>
        /// Optional explicit socket table. When null/empty, sockets are scanned from painted cells.
        /// Reserved for future authoring without breaking ASCII templates.
        /// </summary>
        public List<TemplateSocketDef> ExplicitSockets;

        public float WorldWidth => Width * CellSize;
        public float WorldDepth => Height * CellSize;
        public int CellCount => Width * Height;

        public BuildingInteriorCellKind GetCell(int x, int y)
        {
            if (Cells == null || x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return BuildingInteriorCellKind.Empty;
            }

            return Cells[y * Width + x];
        }

        public bool SupportsKind(BuildingKind kind)
        {
            if (CompatibleKinds == null || CompatibleKinds.Length == 0)
            {
                return true;
            }

            for (var i = 0; i < CompatibleKinds.Length; i++)
            {
                if (CompatibleKinds[i] == kind)
                {
                    return true;
                }
            }

            return false;
        }

        public bool PrefersWealth(WealthClass wealth)
        {
            if (PreferredWealth == null || PreferredWealth.Length == 0)
            {
                return true;
            }

            for (var i = 0; i < PreferredWealth.Length; i++)
            {
                if (PreferredWealth[i] == wealth)
                {
                    return true;
                }
            }

            return false;
        }

        public int CountCells(BuildingInteriorCellKind kind)
        {
            if (Cells == null)
            {
                return 0;
            }

            var n = 0;
            for (var i = 0; i < Cells.Length; i++)
            {
                if (Cells[i] == kind)
                {
                    n++;
                }
            }

            return n;
        }
    }
}
