using UnityEngine;

namespace GameDev.Player.Progression
{
    /// <summary>The size of what a character asks for in a Social Check (10.4 §2), set by the dialogue's designer.</summary>
    public enum RequestSize
    {
        Small,
        Ordinary,
        Large,
        Huge,
    }

    /// <summary>The numbers of the Social Check (10.4 §2). Changing one changes only this asset.</summary>
    [CreateAssetMenu(menuName = "GameDev/Progression/Social Check Rules", fileName = "SocialCheckRules")]
    public sealed class SocialCheckRules : ScriptableObject
    {
        [Tooltip("10.4 §2 Request size: the base of the Difficulty for Small, Ordinary, Large and Huge, in that order.")]
        [SerializeField] private int[] requests = { 5, 7, 9, 11 };

        [Tooltip("10.4 §2 Calculation: added to the Difficulty when the listener is an enemy.")]
        [SerializeField] private int enemy = 2;

        [Tooltip("10.4 §2 Calculation: the points of attributes and skill that make one step of Character or Listener Value.")]
        [SerializeField, Min(1)] private int valueStep = 20;

        [Tooltip("10.4 §2 Modifiers: their sum stays between minus and plus this.")]
        [SerializeField, Min(0)] private int modifierCap = 4;

        [Tooltip("10.4 §3: the Intelligence from which an option shows the percentage of its check.")]
        [SerializeField, Min(1)] private int chanceFromIntelligence = 10;

        [Tooltip("10.4 §3: the Intelligence from which an option shows the modifiers behind its chance.")]
        [SerializeField, Min(1)] private int modifiersFromIntelligence = 20;

        public int Request(RequestSize size) => requests[(int)size];
        public int Enemy => enemy;
        public int ValueStep => valueStep;
        public int ModifierCap => modifierCap;
        public int ChanceFromIntelligence => chanceFromIntelligence;
        public int ModifiersFromIntelligence => modifiersFromIntelligence;
    }
}
