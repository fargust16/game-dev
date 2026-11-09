using UnityEngine;

namespace GameDev.Confront.Rules
{
    /// <summary>
    /// What the other hand holds beside a one-handed weapon or a firearm (10.2 §5b): a shield, which blocks, or the
    /// Cartridge Pouch, whose cartridges never run out and without which no firearm is held (*Firearms*).
    /// </summary>
    [CreateAssetMenu(menuName = "GameDev/Confront/Fight Off Hand", fileName = "OffHand")]
    public sealed class FightOffHand : ScriptableObject
    {
        [Tooltip("The sections of 10.2 that set these values.")]
        [SerializeField] private string section = "10.2 §5b Blocks and parries";

        [SerializeField] private OffHandKind kind;

        [Tooltip("10.2 §5b Blocks and parries: a shield's guard replaces its weapon's.")]
        [SerializeField] private Guard guard;

        [SerializeField, Range(0f, 1f)] private float guardChance;

        [SerializeField, Range(0f, 1f)] private float guardStops;

        [Tooltip("10.2 §5b Load in a Fight: kg.")]
        [SerializeField] private float weight;

        public string Section => section;
        public OffHandKind Kind => kind;
        public Guard Guard => guard;
        public float GuardChance => guardChance;
        public float GuardStops => guardStops;
        public float Weight => weight;
    }
}
