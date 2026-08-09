namespace GameDev.World.Buildings.Shell
{
    /// <summary>
    /// Social / functional district classes for Exterior Shell Grammar.
    /// Ids match <see cref="DistrictProfileDef.Id"/>.
    /// </summary>
    public enum DistrictKind
    {
        Poor = 0,
        Worker = 1,
        Affluent = 2,
        Rich = 3,
        Commercial = 4,
        Elite = 5
    }

    public static class DistrictIds
    {
        public const string Poor = "Poor";
        public const string Worker = "Worker";
        public const string Affluent = "Affluent";
        public const string Rich = "Rich";
        public const string Commercial = "Commercial";
        public const string Elite = "Elite";

        public static string FromKind(DistrictKind kind)
        {
            switch (kind)
            {
                case DistrictKind.Poor: return Poor;
                case DistrictKind.Worker: return Worker;
                case DistrictKind.Affluent: return Affluent;
                case DistrictKind.Rich: return Rich;
                case DistrictKind.Commercial: return Commercial;
                case DistrictKind.Elite: return Elite;
                default: return Worker;
            }
        }
    }
}
