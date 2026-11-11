using System;
using GameDev.Confront.Rules;
using GameDev.Player.Progression;
using UnityEngine;

namespace GameDev.Slice.Beasts
{
    public enum BeastKind
    {
        Wolf,
        Boar,
    }

    /// <summary>
    /// The slice's beasts (10.2 *Beasts of the slice*, 11 content piece C3, S3b.2): the four kinds' values and gear from
    /// 10.2's table, their territory, warnings and reach, how many glow, and what each leaves (19). The kinds' values are
    /// 10.2's authoring defaults; the territory's radius, the eye heights and the glow are this step's.
    /// </summary>
    [CreateAssetMenu(menuName = "Game Dev/Slice/Beast Rules", fileName = "BeastRules")]
    public sealed class BeastRules : ScriptableObject
    {
        [Serializable]
        public sealed class Values
        {
            public int strength = 1;
            public int endurance = 1;
            public int dexterity = 1;
            public int perception = 1;
            public int meleeSkill = 1;
            public FightWeapon weapon;
            public FightArmour armour;

            [Tooltip("10.2: the share of HP added, 1 for a glowing wolf (twice a wolf's HP), 0.25 for a glowing boar.")]
            public float hpAdded;

            [Tooltip("Its name in a fight, a string-table key.")]
            public string nameKey;

            [Tooltip("19 §3, 11 C3: leather by Skinning (which needs a lesson), the ordinary ingredient, and a glowing beast's magical one. Taking them waits for S5a's inventory.")]
            public int leather;
            public int ingredients;
            public int magical;
        }

        [SerializeField] private Values wolf = new Values();
        [SerializeField] private Values boar = new Values();
        [SerializeField] private Values glowingWolf = new Values();
        [SerializeField] private Values glowingBoar = new Values();

        [Tooltip("10.2: a territory about 30 m across, so 15 m from its centre (m).")]
        [SerializeField] private float territoryRadius = 15f;

        [Tooltip("10.2: it warns twice, a tick each, before it attacks.")]
        [SerializeField] private int warnings = 2;

        [Tooltip("10.2: it attacks at once a character that comes within this many cells (1 m each in the slice).")]
        [SerializeField] private int attackCells = 3;

        [Tooltip("11: the seed makes two or three beasts glow, each alone.")]
        [SerializeField] private int glowingLeast = 2;
        [SerializeField] private int glowingMost = 3;

        [Tooltip("Eye heights for the Vision Cone, wolf and boar (m, authoring defaults; a person's is 1.6).")]
        [SerializeField] private float wolfEyes = 0.7f;
        [SerializeField] private float boarEyes = 0.6f;

        [Tooltip("The glow that shows from afar (10.2): a pale light on the beast and its fur lit from within (authoring defaults).")]
        [SerializeField] private Color glow = new Color(0.55f, 0.95f, 0.85f);
        [SerializeField] private float glowLight = 1.6f;
        [SerializeField] private float glowRange = 7f;
        [SerializeField] private float glowEmission = 0.06f;

        public float TerritoryRadius => territoryRadius;

        public int Warnings => warnings;

        public int AttackCells => attackCells;

        public int GlowingLeast => glowingLeast;

        public int GlowingMost => glowingMost;

        public Color Glow => glow;

        public float GlowLight => glowLight;

        public float GlowRange => glowRange;

        public float GlowEmission => glowEmission;

        public float EyesOf(BeastKind kind) => kind == BeastKind.Wolf ? wolfEyes : boarEyes;

        public Values Of(BeastKind kind, bool glowing) => kind == BeastKind.Wolf ? (glowing ? glowingWolf : wolf) : (glowing ? glowingBoar : boar);

        /// <summary>The beast's fighter by 10.2's table.</summary>
        public Fighter Fighter(FightRules rules, BeastKind kind, bool glowing)
        {
            var v = Of(kind, glowing);
            var sheet = new CharacterSheet();
            sheet.Set(CharacterAttribute.Strength, v.strength);
            sheet.Set(CharacterAttribute.Endurance, v.endurance);
            sheet.Set(CharacterAttribute.Dexterity, v.dexterity);
            sheet.Set(CharacterAttribute.Perception, v.perception);
            sheet.Set(CharacterSkill.MeleeSkill, v.meleeSkill);
            return rules.Fighter(sheet, null, new WeaponSet(v.weapon), v.armour, modifiers: new FightModifiers { hp = v.hpAdded });
        }
    }
}
