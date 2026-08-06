using GameDev.Grid;

namespace GameDev.World.Town
{
    public enum TownWealth
    {
        Rich = 0,
        Mixed = 1,
        Poor = 2
    }

    /// <summary>
    /// Human-scale constants for organic late-18th/19th town growth on the square grid.
    /// </summary>
    public static class TownGrowthParams
    {
        public const int MapWidth = 180;
        public const int MapHeight = 140;

        public const int TownRadiusMin = 55;
        public const int TownRadiusMax = 68;

        public const int PlazaRadiusMin = 5;
        public const int PlazaRadiusMax = 8;

        public const int MajorWidthRidge = 4;
        public const int MajorWidthVeil = 3;
        public const int MajorWidthUndercroft = 2;
        public const int SecondaryWidth = 2;
        public const int AlleyWidth = 1;

        public const int RingRadiusFactorNum = 55;
        public const int RingRadiusFactorDen = 100;

        public const int SecondaryCountMin = 10;
        public const int SecondaryCountMax = 16;
        public const int AlleyCountPoorMin = 22;
        public const int AlleyCountPoorMax = 34;
        public const int AlleyCountRichMin = 4;
        public const int AlleyCountRichMax = 8;

        public const int CourtyardCountMin = 6;
        public const int CourtyardCountMax = 12;

        public static GridCoord MapCenter => new GridCoord(MapWidth / 2, MapHeight / 2);
    }
}
