using System.Collections.Generic;
using UnityEngine;

namespace GameDev.Confront.Rules
{
    /// <summary>
    /// One kind of weapon, or a beast's natural weapon, with the values 10.2 §5b gives it: the weapon table (*Damage*),
    /// its guard (*Blocks and parries*), its Attribute Requirements, and the weight that counts in *Load in a Fight*.
    /// The values are 10.2's authoring defaults, checked by `tools/balance/build_sim.py` and `beast_sim.py`.
    /// </summary>
    [CreateAssetMenu(menuName = "GameDev/Confront/Fight Weapon", fileName = "Weapon")]
    public sealed class FightWeapon : ScriptableObject
    {
        [Tooltip("The sections of 10.2 that set these values.")]
        [SerializeField] private string section = "10.2 §5b Damage";

        [Tooltip("10.2 §5b Damage: base damage before the Stage Factor and Strength.")]
        [SerializeField] private float baseDamage = 5f;

        [Tooltip("10.2 §5b Damage: AP of one attack; a firearm's shot includes its loading (Firearms).")]
        [SerializeField] private int attackAp = 4;

        [SerializeField] private bool twoHanded;

        [Tooltip("10.2 §5b Firearms: a firearm takes no Strength, meets doubled armour beyond the next cell, and is held only with a Cartridge Pouch.")]
        [SerializeField] private bool ranged;

        [Tooltip("10.2 §5b Damage: cells; a melee weapon reaches across a cell's side only (Position).")]
        [SerializeField] private int range = 1;

        [Tooltip("10.2 §5b Damage: percentage points added to the fighter's accuracy.")]
        [SerializeField] private float toHit;

        [Tooltip("10.2 §5b Damage: percentage points added to the fighter's critical chance, before Luck.")]
        [SerializeField] private float critical;

        [Tooltip("10.2 §5b Blocks and parries.")]
        [SerializeField] private Guard guard;

        [SerializeField, Range(0f, 1f)] private float guardChance;

        [Tooltip("10.2 §5b Blocks and parries: the share of a hit's damage a successful guard stops.")]
        [SerializeField, Range(0f, 1f)] private float guardStops;

        [Tooltip("10.2 §5b Damage: the share of hits that take the target's next turn.")]
        [SerializeField, Range(0f, 1f)] private float stun;

        [Tooltip("10.2 §5b Damage: the share of damage the second strike of a cleave deals; zero for a weapon that does not cleave.")]
        [SerializeField, Range(0f, 1f)] private float cleave;

        [Tooltip("10.2 §5b Damage: added to the critical multiplier of a stealth attack (10.3).")]
        [SerializeField] private float stealthCritical;

        [Tooltip("10.2 §5b Blocks and parries: a shield goes with a sword, an axe or a mace only.")]
        [SerializeField] private bool takesShield;

        [Tooltip("10.2 §5b Beasts of the slice: a critical hit causes Bleeding.")]
        [SerializeField] private bool bleedsOnCritical;

        [Tooltip("10.2 §5b Beasts of the slice: the chance that a hit poisons.")]
        [SerializeField, Range(0f, 1f)] private float poisonOnHit;

        [Tooltip("10.2 §5b Load in a Fight: kg.")]
        [SerializeField] private float weight = 2f;

        [Tooltip("10.2 §5b Damage: the item stage; the Stage Factor and the Attribute Requirements follow it.")]
        [SerializeField, Range(0, 9)] private int stage = 9;

        [Tooltip("10.2 §5b Attribute Requirements, at stage 9.")]
        [SerializeField] private AttributeRequirement[] requirements = { };

        public string Section => section;
        public float BaseDamage => baseDamage;
        public int AttackAp => attackAp;
        public bool TwoHanded => twoHanded;
        public bool Ranged => ranged;
        public int RangeCells => range;
        public float ToHit => toHit;
        public float Critical => critical;
        public Guard Guard => guard;
        public float GuardChance => guardChance;
        public float GuardStops => guardStops;
        public float Stun => stun;
        public float Cleave => cleave;
        public float StealthCritical => stealthCritical;
        public bool TakesShield => takesShield;
        public bool BleedsOnCritical => bleedsOnCritical;
        public float PoisonOnHit => poisonOnHit;
        public float Weight => weight;
        public int Stage => stage;
        public IReadOnlyList<AttributeRequirement> Requirements => requirements;
    }
}
