using GameDev.World;

namespace GameDev.Decisions
{
    public sealed class DecisionOptionDef
    {
        public string Name;
        public string Axes;
        public ApproachType PrimaryApproach;
        public string ApproachNote;
        public string Cost;
        public string Consequence;
        public string Opens;
        public string Closes;
        public string Failure;
        public string[] ResidueFlags;
        public bool CountsAsWithdraw;
    }

    public sealed class DecisionNodeDef
    {
        public PoiShellId PoiId;
        public string Title;
        public string Situation;
        public string PrimaryAxis;
        public string SecondaryAxis;
        public DecisionOptionDef[] Options;
    }
}
