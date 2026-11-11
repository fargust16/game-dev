using UnityEngine;

namespace GameDev.Confront.Rules
{
    /// <summary>
    /// S3a.8: the numbers of Target Rules V0 (10.2 §5a), as `tools/balance/tactical_sim.py` plays them. They weigh a
    /// target's Target Impact; they are authoring defaults of the same asset.
    /// </summary>
    public sealed partial class FightRules
    {
        [Header("10.2 §5a Target Rules V0 (tactical_sim.py)")]
        [Tooltip("Each full turn of walking a target needs multiplies its score by this.")]
        [SerializeField] private float reachDiscount = 0.7f;

        [Tooltip("A target the fighter can bring down this turn counts this many times.")]
        [SerializeField] private float killBonus = 2.5f;

        [Tooltip("A target losing its turn counts this much.")]
        [SerializeField] private float helplessFactor = 0.2f;

        [Tooltip("A target with this many of the fighter's allies already beside it counts crowdFactor.")]
        [SerializeField] private int crowdAllies = 3;

        [SerializeField] private float crowdFactor = 0.5f;

        [Tooltip("A melee fighter beside an enemy leaves it for its best target only when that target scores this many times more.")]
        [SerializeField] private float breakOffFactor = 1.5f;

        [Tooltip("Added to every target's harm, so a harmless target still scores above nothing (tactical_sim.py).")]
        [SerializeField] private float threatFloor = 0.1f;

        public double ReachDiscount => reachDiscount;

        public double KillBonus => killBonus;

        public double HelplessFactor => helplessFactor;

        public int CrowdAllies => crowdAllies;

        public double CrowdFactor => crowdFactor;

        public double BreakOffFactor => breakOffFactor;

        public double ThreatFloor => threatFloor;

        /// <summary>A whole turn's AP, as <see cref="TurnAp"/> gives them but without the carried fraction or carrying one: for estimates.</summary>
        public int FullAp(Fighter fighter) =>
            (int)System.Math.Floor(apBase + fighter.Get(GameDev.Player.Progression.CharacterAttribute.Dexterity) / apDexterityDivisor + fighter.Modifiers.ap - fighter.LoadAp);
    }
}
