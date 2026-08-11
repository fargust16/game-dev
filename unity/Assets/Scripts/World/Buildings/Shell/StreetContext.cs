using System;

namespace GameDev.World.Buildings.Shell
{
    [Serializable]
    public struct StreetContext
    {
        public FaceKind Front;
        public FaceKind Right;
        public FaceKind Back;
        public FaceKind Left;

        public static StreetContext StreetFrontYardBack()
        {
            return new StreetContext
            {
                Front = FaceKind.Street,
                Right = FaceKind.Party,
                Back = FaceKind.Yard,
                Left = FaceKind.Party
            };
        }

        public static StreetContext CornerFrontLeft()
        {
            return new StreetContext
            {
                Front = FaceKind.Street,
                Right = FaceKind.Party,
                Back = FaceKind.Yard,
                Left = FaceKind.Street
            };
        }

        public static StreetContext CornerFrontRight()
        {
            return new StreetContext
            {
                Front = FaceKind.Street,
                Right = FaceKind.Street,
                Back = FaceKind.Yard,
                Left = FaceKind.Party
            };
        }

        /// <summary>Street frontage with service lane / alley at the rear (S4).</summary>
        public static StreetContext StreetFrontAlleyBack()
        {
            return new StreetContext
            {
                Front = FaceKind.Street,
                Right = FaceKind.Party,
                Back = FaceKind.Alley,
                Left = FaceKind.Party
            };
        }

        public StreetContext WithBack(FaceKind back)
        {
            var c = this;
            c.Back = back;
            return c;
        }

        public FaceKind Get(PlotFace face)
        {
            switch (face)
            {
                case PlotFace.Front: return Front;
                case PlotFace.Right: return Right;
                case PlotFace.Back: return Back;
                case PlotFace.Left: return Left;
                default: return FaceKind.Party;
            }
        }

        public bool IsPublicFront(FaceKind kind)
        {
            return kind == FaceKind.Street || kind == FaceKind.Square;
        }
    }
}
