using UnityEngine;

namespace GameDev.Confront.Rules
{
    /// <summary>
    /// S3a.9: the numbers of leaving a fight (08 §7; 10.2 §9): Break Away, a Yield, and an NPC asking for mercy. They are
    /// authoring defaults of the same asset, as the plan's S3 section proposes them.
    /// </summary>
    public sealed partial class FightRules
    {
        [Header("08 §7, 10.2 §9 Leaving a fight (S3a.9)")]
        [Tooltip("Cells, counted as moves are, a diagonal one: a step that ends this far from every enemy, and farther than the fighter stood, breaks away.")]
        [SerializeField] private int breakAwayCells = 10;

        [Tooltip("10.2 §9: the AP a Yield costs; it opens a dialogue and ends the fight at once.")]
        [SerializeField] private int yieldAp = 2;

        [Tooltip("A fighter of people asks for mercy below this share of its HP, when its side has fewer fighters standing.")]
        [SerializeField] private float mercyShare = 0.25f;

        public int BreakAwayCells => breakAwayCells;

        public int YieldAp => yieldAp;

        public double MercyShare => mercyShare;
    }
}
