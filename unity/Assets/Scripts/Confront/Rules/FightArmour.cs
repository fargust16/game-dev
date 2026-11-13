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

        [Tooltip("10.2 §5f Infusion: the share of its wearer's Infusion that passes through it: 1, chain 0.5, plate 0.")]
        [SerializeField, Range(0f, 1f)] private float channel = 1f;

        [Tooltip("10.2 §5b Load in a Fight: kg, worn armour still counting.")]
        [SerializeField] private float weight;

        [Tooltip("10.2 §5b: the item stage; the Stage Factor and the Attribute Requirements follow it.")]
        [SerializeField, Range(0, 9)] private int stage = 9;

        [Tooltip("10.2 §5b Attribute Requirements, at stage 9.")]
        [SerializeField] private AttributeRequirement[] requirements = { };

        public string Section => section;
        public float Physical => physical;
        public float Magical => magical;
        public float Channel => channel;
        public float Weight => weight;
        public int Stage => stage;
        public IReadOnlyList<AttributeRequirement> Requirements => requirements;

        /// <summary>
        /// Pieces of several families worn together as one armour (S5a.2): its defence already counts each piece's
        /// Stage Factor and the share of it its wearer meets, so it is of stage 9 and asks nothing more. Its channel is the
        /// pieces' own, weighed by how much of a whole set each is.
        /// </summary>
        public static FightArmour Worn(float physicalDefence, float magicalDefence, float kg, float channelShare = 1f)
        {
            var armour = CreateInstance<FightArmour>();
            armour.name = "Worn";
            armour.hideFlags = HideFlags.DontSave;
            armour.section = "S5a.2 worn pieces";
            armour.physical = physicalDefence;
            armour.magical = magicalDefence;
            armour.channel = Mathf.Clamp01(channelShare);
            armour.weight = kg;
            armour.stage = 9;
            armour.requirements = new AttributeRequirement[0];
            return armour;
        }
    }
}
