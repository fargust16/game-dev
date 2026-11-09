using GameDev.Player.Progression;
using UnityEngine;

namespace GameDev.Player.Standing
{
    /// <summary>The numbers of the character's standing in conversation (10.4 §2, §4–5). Changing one changes only this asset.</summary>
    [CreateAssetMenu(menuName = "GameDev/Player/Standing Rules", fileName = "StandingRules")]
    public sealed class StandingRules : ScriptableObject
    {
        [Tooltip("10.4 §4: the Reputation points of a task done for the faction, an NPC helped, a city liberated, a theft noticed and a break-in noticed, in the order of Deed.")]
        [SerializeField] private int[] deedPoints = { 3, 1, 10, -3, -5 };

        [Tooltip("10.4 §4: the Reputation modifier below the first step.")]
        [SerializeField] private int lowestReputation = -2;

        [Tooltip("10.4 §4: the Reputation points from which the modifier is one higher, lowest first.")]
        [SerializeField] private int[] reputationSteps = { -9, -2, 5, 15 };

        [Tooltip("10.4 §4: the Danger each Limit reached adds; each kill adds one.")]
        [SerializeField, Min(0)] private int dangerPerLimit = 5;

        [Tooltip("10.4 §4: the Danger modifier below the first step.")]
        [SerializeField] private int lowestDanger = -1;

        [Tooltip("10.4 §4: the Danger from which the modifier is one higher, lowest first.")]
        [SerializeField] private int[] dangerSteps = { 3, 10, 20 };

        [Tooltip("10.4 §2 Modifiers: an NPC's Attitude stays between minus and plus this.")]
        [SerializeField, Min(0)] private int attitudeRange = 2;

        [Tooltip("10.4 §5: the steps of Attitude an Intimidation costs, even when it works.")]
        [SerializeField, Min(0)] private int intimidationCost = 1;

        [Tooltip("10.4 §5: the Reputation points a failed lie costs, and a lie that comes out.")]
        [SerializeField, Min(0)] private int lieCost = 5;

        [Tooltip("10.4 §5: the steps of Attitude a listener loses when a lie to it comes out.")]
        [SerializeField, Min(0)] private int exposedCost = 2;

        [Tooltip("10.4 §5: the chance a lie that worked comes out at the next day boundary, for Small, Ordinary, Large and Huge. Huge is the agent's authoring default: the table's step continued.")]
        [SerializeField] private float[] exposure = { 0f, 0.2f, 0.4f, 0.6f };

        public int Points(Deed deed) => deedPoints[(int)deed];
        public int ReputationModifier(int points) => Band(lowestReputation, reputationSteps, points);
        public int DangerPerLimit => dangerPerLimit;
        public int DangerModifier(int danger) => Band(lowestDanger, dangerSteps, danger);
        public int AttitudeRange => attitudeRange;
        public int IntimidationCost => intimidationCost;
        public int LieCost => lieCost;
        public int ExposedCost => exposedCost;
        public float Exposure(RequestSize request) => exposure[(int)request];

        private static int Band(int lowest, int[] steps, int value)
        {
            var modifier = lowest;
            foreach (var step in steps)
            {
                if (value >= step)
                {
                    modifier++;
                }
            }

            return modifier;
        }
    }
}
