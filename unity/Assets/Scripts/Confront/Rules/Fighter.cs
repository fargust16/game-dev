using System.Collections.Generic;
using GameDev.Player.Progression;

namespace GameDev.Confront.Rules
{
    /// <summary>
    /// One fighter in a fight: its character sheet, Limit, Weapon Sets, armour and modifiers, what they give once
    /// (maximum HP and Load in a Fight), and what changes as the fight goes: HP, the AP it holds for reactions, the
    /// fraction of AP it carries to its next turn, and the turns it loses. <see cref="FightRules.Fighter"/> builds it and
    /// checks its gear.
    /// </summary>
    public sealed class Fighter
    {
        internal Fighter(ICharacterValues values, int? limit, WeaponSet set, WeaponSet second, FightArmour armour, FightModifiers modifiers)
        {
            Values = values;
            Limit = limit;
            Set = set;
            Second = second;
            Armour = armour;
            Modifiers = modifiers;
        }

        public ICharacterValues Values { get; }

        /// <summary>The fighter's Limit, 0 to 9; null for an Uninitiated character or a beast.</summary>
        public int? Limit { get; }

        /// <summary>The Weapon Set in hand.</summary>
        public WeaponSet Set { get; private set; }

        /// <summary>The second Weapon Set, empty if it carries none; its weight counts in Load in a Fight.</summary>
        public WeaponSet Second { get; private set; }

        /// <summary>
        /// 10.2 §5b Wait and Defensive Stance: until its next turn, each AP it holds adds to its Evasion and to its chance
        /// to block or parry, and it answers a melee attack with a Counterattack (S3a.6).
        /// </summary>
        public bool Stance { get; set; }

        /// <summary>Null for no armour.</summary>
        public FightArmour Armour { get; }

        public FightModifiers Modifiers { get; }

        /// <summary>What it carries besides its armour and Weapon Sets, in kg; null for 10.2's reference load (S5a.2).</summary>
        public double? Carried { get; internal set; }

        /// <summary>
        /// 10.2 §5b *Two weapons*: the next blow is struck with the other hand's weapon. The turns set it for one blow and
        /// clear it after, so outside a blow it is false and <see cref="Weapon"/> is the main hand's.
        /// </summary>
        public bool OffHand { get; internal set; }

        /// <summary>The weapon that strikes: the main hand's, or the other hand's while <see cref="OffHand"/> is set.</summary>
        public FightWeapon Weapon => OffHand && Set.OffWeapon != null ? Set.OffWeapon : Set.Weapon;

        /// <summary>The AP of the cheapest blow in hand: its weapon's, or with two weapons the lighter one's.</summary>
        public int BlowAp => Set.Paired ? System.Math.Min(Set.Weapon.AttackAp, Set.OffWeapon.AttackAp) : Weapon.AttackAp;

        public double MaxHp { get; internal set; }

        public double Hp { get; set; }

        /// <summary>10.2 §5b Load in a Fight: what the load takes from Evasion, in points.</summary>
        public double LoadEvasion { get; internal set; }

        /// <summary>10.2 §5b Load in a Fight: what the load takes from each turn's AP.</summary>
        public int LoadAp { get; internal set; }

        /// <summary>10.2 §5b Blocks and parries: the AP left at the end of its turn, which reactions spend.</summary>
        public int Reserve { get; set; }

        /// <summary>10.2 §5b Action Points: the fraction of AP carried into the next turn.</summary>
        public double ApFraction { get; set; }

        /// <summary>The turns it will lose, as a stun takes them.</summary>
        public int LostTurns { get; set; }

        /// <summary>10.2 Bleeding: it loses HP on each world tick until it ends by itself or a bandage ends it (S3a.7).</summary>
        public bool Bleeding { get; set; }

        /// <summary>10.2 Poison: no HP returns while it lasts, until it ends by itself or an antidote ends it.</summary>
        public bool Poisoned { get; set; }

        /// <summary>10.2: a broken leg is Immobilisation until a splint is set, so it takes no step and no Environment Action.</summary>
        public bool LegBroken { get; set; }

        /// <summary>False for a target that cannot evade, with an Evasion of zero (10.2 §5b Attack); it cannot guard either.</summary>
        public bool CanEvade { get; set; } = true;

        public bool Down => Hp <= 0d;

        /// <summary>05.1: the Arcana it entered; <see cref="Arcana.None"/> for the Uninitiated and beasts.</summary>
        public Arcana Arcana { get; internal set; }

        /// <summary>07.2 §3.3: `(20 + 0.8 × Will) × S`, and the items' share.</summary>
        public double MaxWillPower { get; internal set; }

        /// <summary>07.2 §3.3: what is left to spend on abilities and Infusion; it comes back slowly, so a fight is fought on it.</summary>
        public double WillPower { get; set; }

        private readonly List<FightAbility> _prepared = new List<FightAbility>();
        private readonly List<FightAbility> _passives = new List<FightAbility>();

        /// <summary>07.2 §4.5: the active abilities prepared for this fight, the only ones it can use in it.</summary>
        public IReadOnlyList<FightAbility> Prepared => _prepared;

        /// <summary>05.1: the passive abilities it knows, which always work.</summary>
        public IReadOnlyList<FightAbility> Passives => _passives;

        /// <summary>
        /// 10.2 §5f Infusion: an NPC that pours Will Power into each blow or shot while it can spare it, as the slice's
        /// allied fighters do; the player's choice is the turns' own.
        /// </summary>
        public bool Infuses { get; set; }

        /// <summary>05.1 Дрёма: asleep, its Evasion zero and no guard, until its next turn is lost, damage, or an ally's waking.</summary>
        public int Asleep { get; set; }

        /// <summary>05.1 Укол разума: Disorientation, which aims its next turn worse.</summary>
        public int Disoriented { get; set; }

        /// <summary>This turn it aims worse, being disoriented as it began.</summary>
        public bool Dazed { get; set; }

        /// <summary>05.1: a failed ability leaves it open; the next attack on it aims better, once.</summary>
        public bool Exposed { get; set; }

        /// <summary>05.1 Не сдвинуть: until its next turn, the physical damage it takes is lower.</summary>
        public bool Braced { get; set; }

        /// <summary>
        /// 05.1 Двойник: the ends of its own turns it stays unseen, the one it is in counted; its enemies neither target it
        /// nor see it by their cones, and damage it deals ends it (CR5).
        /// </summary>
        public int Veiled { get; set; }

        /// <summary>
        /// 05.1 Двойник: a still copy of the fighter as its enemies take it, with its sheet, gear, HP and abilities; what
        /// strikes the copy never touches the fighter.
        /// </summary>
        internal Fighter Copy()
        {
            var copy = new Fighter(Values, Limit, Set, Second, Armour, Modifiers)
            {
                MaxHp = MaxHp,
                Hp = Hp,
                LoadEvasion = LoadEvasion,
                LoadAp = LoadAp,
                Carried = Carried,
                CanEvade = CanEvade,
                Arcana = Arcana,
                MaxWillPower = MaxWillPower,
                WillPower = WillPower,
            };
            copy._prepared.AddRange(_prepared);
            copy._passives.AddRange(_passives);
            return copy;
        }

        /// <summary>
        /// 07.2 §4.5: what it knows of its Arcana for this fight: every passive one, and the active ones a fight can use,
        /// as many as <paramref name="slots"/> hold, in the order given.
        /// </summary>
        public void Prepare(Arcana arcana, IEnumerable<FightAbility> known, int slots)
        {
            Arcana = arcana;
            _prepared.Clear();
            _passives.Clear();
            if (known == null)
            {
                return;
            }

            foreach (var ability in known)
            {
                if (ability == null)
                {
                    continue;
                }

                if (ability.Passive)
                {
                    _passives.Add(ability);
                }
                else if (_prepared.Count < slots && !_prepared.Contains(ability))
                {
                    _prepared.Add(ability);
                }
            }
        }

        /// <summary>The prepared or passive ability with this effect, or null.</summary>
        public FightAbility Knows(AbilityEffect effect)
        {
            foreach (var a in _prepared)
            {
                if (a.Effect == effect)
                {
                    return a;
                }
            }

            foreach (var a in _passives)
            {
                if (a.Effect == effect)
                {
                    return a;
                }
            }

            return null;
        }

        /// <summary>10.2 §5b: the second Weapon Set comes to hand and the first goes to the back; false if it carries none.</summary>
        public bool SwapSets()
        {
            if (Second.IsEmpty)
            {
                return false;
            }

            (Set, Second) = (Second, Set);
            OffHand = false;
            return true;
        }

        public int Get(CharacterAttribute attribute) => Values.Get(attribute);

        public int Get(CharacterSkill skill) => Values.Get(skill);
    }
}
