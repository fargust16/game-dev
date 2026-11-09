using System.Collections.Generic;
using UnityEngine;

namespace GameDev.Confront.Rules
{
    /// <summary>
    /// A whole set of armour, or a beast's hide, with the values 10.2 §5b gives it: physical and magical defence
    /// (*Damage*, the armour table), its Attribute Requirements, and the weight that counts in *Load in a Fight*.
    /// </summary>
    [CreateAssetMenu(menuName = "GameDev/Confront/Fight Armour", fileName = "Armour")]
    public sealed class FightArmour : ScriptableObject
    {
        [Tooltip("The sections of 10.2 that set these values.")]
        [SerializeField] private string section = "10.2 §5b Damage";

        [Tooltip("10.2 §5b Damage: physical defence at stage 9, before the Stage Factor.")]
        [SerializeField] private float physical;

        [Tooltip("10.2 §5b Damage: magical defence at stage 9; it answers Infusion and abilities, which come later.")]
        [SerializeField] private float magical;

        [Tooltip("10.2 §5b Load in a Fight: kg, worn armour still counting.")]
        [SerializeField] private float weight;

        [Tooltip("10.2 §5b: the item stage; the Stage Factor and the Attribute Requirements follow it.")]
        [SerializeField, Range(0, 9)] private int stage = 9;

        [Tooltip("10.2 §5b Attribute Requirements, at stage 9.")]
        [SerializeField] private AttributeRequirement[] requirements = { };

        public string Section => section;
        public float Physical => physical;
        public float Magical => magical;
        public float Weight => weight;
        public int Stage => stage;
        public IReadOnlyList<AttributeRequirement> Requirements => requirements;
    }
}
