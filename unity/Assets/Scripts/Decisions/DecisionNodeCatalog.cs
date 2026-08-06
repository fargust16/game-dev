using GameDev.World;

namespace GameDev.Decisions
{
    /// <summary>
    /// Vertical Slice Decision Node cards from 11.1 — stubbed for S2 playable choice.
    /// </summary>
    public static class DecisionNodeCatalog
    {
        public static DecisionNodeDef Get(PoiShellId id)
        {
            switch (id)
            {
                case PoiShellId.PoiA_HarrowArrest:
                    return BuildPoiA();
                case PoiShellId.PoiB_EleventhBellCollectors:
                    return BuildPoiB();
                default:
                    return null;
            }
        }

        private static DecisionNodeDef BuildPoiA()
        {
            return new DecisionNodeDef
            {
                PoiId = PoiShellId.PoiA_HarrowArrest,
                Title = "Brass-Number Arrest at Harrow & Tincture",
                Situation = "Brass-numbers haul old Harrow toward a sealed wagon. A third Warden watches the cabinet, not the man. The crowd is splitting.",
                PrimaryAxis = "Allegiance",
                SecondaryAxis = "Approach",
                Options = new[]
                {
                    Opt(
                        "Defend Harrow",
                        "Allegiance + Approach",
                        ApproachType.Confront,
                        "Confront / Convince / Threaten",
                        "Warden heat; possible Confront; fine or injury",
                        "Shop stays open tonight; Harrow owes a debt",
                        "Cheap medicine; mixture clue; Veinmarket soft favour",
                        "Easy Warden mentorship; clean legal cover",
                        "Cabinet still seized; you learn the list mattered more than Harrow",
                        new[] { "residue_harrow_open", "residue_harrow_debt", "rumour_merrin_harrow_saved" }),
                    Opt(
                        "Help the Wardens seize him",
                        "Allegiance + Approach",
                        ApproachType.Threaten,
                        "Confront / Threaten / Convince / Exchange",
                        "Neighbour hostility; Veinmarket suspicion",
                        "Brass seal on the door; Wardens mark you useful",
                        "Night-patrol tip; temporary legal cover; drill mentor stub",
                        "Harrow aid; soft Veinmarket doors",
                        "Harrow flees with the black bottle; you still hear 'crowned buyer'",
                        new[] { "residue_harrow_sealed", "residue_warden_favour", "rumour_merrin_seal" }),
                    Opt(
                        "Veil and listen first",
                        "Attention + Approach",
                        ApproachType.Veil,
                        "Veil",
                        "Spends Short window time; may miss a clean public save",
                        "You catch a crowned-buyer / list fragment",
                        "Better Exchange leverage; smarter POI C start",
                        "Innocent-hero timing with the crowd",
                        "Spotted; both sides colder; still one partial tell",
                        new[] { "residue_heard_crowned_list", "rumour_merrin_whisper" }),
                    Opt(
                        "Buy a minute / Exchange",
                        "Commitment + Approach",
                        ApproachType.Exchange,
                        "Exchange",
                        "Scarce coin or favour",
                        "Temporary delay or partial access without full Allegiance lock-in",
                        "Hybrid path into defend or seize with more info",
                        "Looking neutral forever; someone remembers the bribe",
                        "Money lost; whistle closer; scene hardens toward Confront",
                        new[] { "residue_bribe_spent", "rumour_merrin_bribe" }),
                    Opt(
                        "Walk away before the whistle",
                        "Attention + Commitment",
                        ApproachType.Withdraw,
                        "Withdraw",
                        "Miss live rewards",
                        "Unattended-style aftermath; morning Symptoms",
                        "Cleaner investigator entry into POI C",
                        "Harrow debt and Warden favour",
                        "World teaches sealed-shop language without a reload",
                        new[] { "residue_harrow_sealed", "rumour_merrin_missed_arrest" },
                        withdraw: true)
                }
            };
        }

        private static DecisionNodeDef BuildPoiB()
        {
            return new DecisionNodeDef
            {
                PoiId = PoiShellId.PoiB_EleventhBellCollectors,
                Title = "Eleventh-Bell Collectors",
                Situation = "Coach-coats drag a mill girl toward the alley after the eleventh bell. Another fights free for one breath. The coach has no honest plate.",
                PrimaryAxis = "Allegiance",
                SecondaryAxis = "Approach",
                Options = new[]
                {
                    Opt(
                        "Protect the victims now",
                        "Allegiance + Approach",
                        ApproachType.Confront,
                        "Confront / Convince / Threaten",
                        "Injury risk; noise; maybe kill the best informant",
                        "One or more girls free tonight",
                        "Gratitude: stair whisper / small fund / fear of crowned coach",
                        "Clean trail to destination",
                        "Partial rescue; glove or chemical-sweet smell remains",
                        new[] { "residue_girls_freed", "residue_collectors_noisy", "rumour_merrin_rescue" }),
                    Opt(
                        "Follow the coach instead",
                        "Allegiance + Approach + Commitment",
                        ApproachType.Veil,
                        "Veil",
                        "Victims remain in danger; spotted risk",
                        "Learn coal-stair landing or warehouse shutters",
                        "Strong POI C setup",
                        "Rescue gratitude; some victims lost",
                        "Spotted into worse Confront, or lose coach but see Veinmarket chalk",
                        new[] { "residue_followed_coach", "residue_stair_lead", "rumour_merrin_trail" }),
                    Opt(
                        "Signal or fetch Wardens",
                        "Allegiance + Approach",
                        ApproachType.Convince,
                        "Convince / Threaten / Exchange",
                        "Time; response may be late or brutal",
                        "Official version written into the lane",
                        "Warden favour; report access",
                        "Underworld informants; rescued girls may distrust batons",
                        "Wardens botch it; you see if this post is corrupt, scared, or late",
                        new[] { "residue_wardens_called", "residue_warden_favour", "rumour_merrin_batons" }),
                    Opt(
                        "Exchange for a tip before moving",
                        "Commitment + Approach",
                        ApproachType.Exchange,
                        "Exchange",
                        "Coin/favour; spends Brief minutes",
                        "Better Veil route or ambush corner",
                        "Smarter rescue or follow",
                        "Tip-seller may sell you out later",
                        "Bad tip into a dead yard; coach already rolling",
                        new[] { "residue_tip_bought", "rumour_merrin_tipster" }),
                    Opt(
                        "Leave Loom Lane",
                        "Attention + Commitment",
                        ApproachType.Withdraw,
                        "Withdraw",
                        "Miss the live scene",
                        "Morning Symptoms + colder Trace",
                        "Investigator angle into POI C without blood on hands",
                        "Rescue debts and live destination",
                        "World shows what collector night looks like after",
                        new[] { "residue_collectors_took_girls", "rumour_merrin_missed_collectors" },
                        withdraw: true)
                }
            };
        }

        private static DecisionOptionDef Opt(
            string name,
            string axes,
            ApproachType approach,
            string approachNote,
            string cost,
            string consequence,
            string opens,
            string closes,
            string failure,
            string[] flags,
            bool withdraw = false)
        {
            return new DecisionOptionDef
            {
                Name = name,
                Axes = axes,
                PrimaryApproach = approach,
                ApproachNote = approachNote,
                Cost = cost,
                Consequence = consequence,
                Opens = opens,
                Closes = closes,
                Failure = failure,
                ResidueFlags = flags,
                CountsAsWithdraw = withdraw
            };
        }
    }
}
