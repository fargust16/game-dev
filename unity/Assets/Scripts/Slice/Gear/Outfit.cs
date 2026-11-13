using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Confront.Rules;
using GameDev.Items;
using GameDev.Player.Progression;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameDev.Slice.Gear
{
    /// <summary>Where a piece is worn or held (19 §4.2): seven body places, two rings, the necklace, and two Weapon Sets.</summary>
    public enum WearSlot
    {
        Head,
        Body,
        Shoulders,
        Gloves,
        Belt,
        Trousers,
        Boots,
        RingOne,
        RingTwo,
        Necklace,
        WeaponOne,
        OffHandOne,
        WeaponTwo,
        OffHandTwo,
    }

    /// <summary>
    /// What the character wears and holds (S5a.2; 19 §4.2, 10.2 §5b). A worn piece stays in the bag, whose weight it keeps.
    /// Each Weapon Set is a weapon and its other hand: a shield only beside a sword, an axe or a mace, a Cartridge Pouch
    /// only beside a firearm, which is never held without one, and nothing beside a two-handed melee weapon. A firearm
    /// takes a free pouch from the bag with it, and a weapon taken off takes its other hand with it. Nothing changes during
    /// a fight. What is worn and held makes the character's fighter: fists in a set with no weapon, and the armour pieces
    /// as one armour, each giving its family's whole-set defence by its share of the set's weight, at its own stage and by
    /// the share of its Attribute Requirements its wearer meets.
    /// </summary>
    public sealed class Outfit
    {
        private static readonly WearSlot[] ArmourSlots =
            { WearSlot.Head, WearSlot.Body, WearSlot.Shoulders, WearSlot.Gloves, WearSlot.Trousers, WearSlot.Boots };

        private readonly Bag _bag;
        private readonly Dictionary<WearSlot, ItemPiece> _worn = new Dictionary<WearSlot, ItemPiece>();
        private FightArmour _armour;
        private (float, float, float, float) _armourKey;

        public Outfit(Bag bag)
        {
            _bag = bag;
            _bag.Changed += Forget;
        }

        /// <summary>Something was put on or taken off.</summary>
        public event Action Changed;

        /// <summary>True while nothing may change, as during a fight.</summary>
        public Func<bool> Locked { get; set; }

        public Bag Bag => _bag;

        public ItemPiece this[WearSlot slot] => _worn.TryGetValue(slot, out var piece) ? piece : null;

        public WearSlot? SlotOf(ItemPiece piece)
        {
            foreach (var pair in _worn)
            {
                if (pair.Value == piece)
                {
                    return pair.Key;
                }
            }

            return null;
        }

        public bool IsWorn(ItemPiece piece) => SlotOf(piece).HasValue;

        public static bool IsSecondSet(WearSlot slot) => slot == WearSlot.WeaponTwo || slot == WearSlot.OffHandTwo;

        /// <summary>Puts a worn piece off, or an unworn one on; the key of the reason it cannot, or null.</summary>
        public string Toggle(ItemPiece piece) => IsWorn(piece) ? TakeOff(piece) : Wear(piece);

        /// <summary>Puts a carried piece on where it goes; the key of the reason it cannot, or null.</summary>
        public string Wear(ItemPiece piece)
        {
            if (Locked != null && Locked())
            {
                return "ui.bag.fault.fight";
            }

            var item = piece != null && _bag.Pieces.Contains(piece) ? _bag.Catalogue.Find(piece.item) : null;
            if (item == null || item.kind != ItemKind.Equipment)
            {
                return "ui.bag.fault.not_worn";
            }

            if (IsWorn(piece))
            {
                return null;
            }

            switch (item.slot)
            {
                case EquipSlot.OneHand:
                case EquipSlot.TwoHands:
                    return item.fight is FightWeapon weapon ? Hold(piece, weapon) : "ui.bag.fault.not_worn";
                case EquipSlot.OffHand:
                    return item.fight is FightOffHand off ? HoldOff(piece, off) : "ui.bag.fault.not_worn";
                case EquipSlot.Ring:
                    Put(this[WearSlot.RingOne] == null || this[WearSlot.RingTwo] != null ? WearSlot.RingOne : WearSlot.RingTwo, piece);
                    break;
                case EquipSlot.None:
                    return "ui.bag.fault.not_worn";
                default:
                    Put(BodySlot(item.slot), piece);
                    break;
            }

            Changed?.Invoke();
            return null;
        }

        /// <summary>
        /// 10.2 §5b *Two weapons*: puts a carried one-handed melee weapon in the other hand of the first set whose main hand
        /// holds one, in place of what that hand held; any other piece is put on as <see cref="Wear"/> puts it. The key of
        /// the reason it cannot, or null.
        /// </summary>
        public string WearOffHand(ItemPiece piece)
        {
            if (Locked != null && Locked())
            {
                return "ui.bag.fault.fight";
            }

            var item = piece != null && _bag.Pieces.Contains(piece) ? _bag.Catalogue.Find(piece.item) : null;
            if (!(item?.fight is FightWeapon weapon) || !WeaponSet.OneHandedMelee(weapon))
            {
                return Wear(piece);
            }

            if (IsWorn(piece))
            {
                return null;
            }

            foreach (var (main, off) in new[] { (WearSlot.WeaponOne, WearSlot.OffHandOne), (WearSlot.WeaponTwo, WearSlot.OffHandTwo) })
            {
                if (WeaponOf(this[main]) is FightWeapon held && new WeaponSet(held, null, weapon).Fault() == null)
                {
                    Remove(off);
                    Put(off, piece);
                    Changed?.Invoke();
                    return null;
                }
            }

            return "ui.bag.fault.pair";
        }

        /// <summary>Takes a piece off, and a weapon's other hand with it, or a firearm with its pouch; the key of the reason it cannot, or null.</summary>
        public string TakeOff(ItemPiece piece)
        {
            if (Locked != null && Locked())
            {
                return "ui.bag.fault.fight";
            }

            if (!(SlotOf(piece) is WearSlot slot))
            {
                return null;
            }

            Drop(slot);
            Changed?.Invoke();
            return null;
        }

        /// <summary>
        /// The character as a fighter in what it wears and holds; <paramref name="fists"/> in a set with no weapon, and at
        /// <paramref name="limit"/>, null for the Uninitiated (05.1).
        /// </summary>
        public Fighter Fighter(FightRules rules, ICharacterValues values, FightWeapon fists, int? limit = null)
        {
            var first = SetOf(WearSlot.WeaponOne, WearSlot.OffHandOne) ?? new WeaponSet(fists);
            var second = SetOf(WearSlot.WeaponTwo, WearSlot.OffHandTwo) ?? default(WeaponSet);
            var armour = Armour(rules, values);
            var held = first.Weight + second.Weight + (armour != null ? armour.Weight : 0f);
            return rules.Fighter(values, limit, first, armour, second, Modifiers(values), Math.Max(0d, _bag.Weight - held));
        }

        /// <summary>
        /// What the modifiers of every worn and held piece add up to in a fight (19 §2.4, 10.2 §5b), each piece's by the share
        /// of it its wearer meets: Magical Damage a share of the weapon's base on every blow, Magical defence flat, Maximum
        /// Will Power a share, and Will against control points in a Will Contest (CR4, 10.2 §5f).
        /// </summary>
        public FightModifiers Modifiers(ICharacterValues values)
        {
            var sum = new FightModifiers();
            foreach (var piece in _worn.Values)
            {
                var fit = (float)FitOf(piece, values);
                foreach (var modifier in piece.modifiers)
                {
                    if (!ItemMaker.TryType(modifier, out var type))
                    {
                        continue;
                    }

                    var v = modifier.value * fit;
                    switch (type)
                    {
                        case ModifierType.Damage: sum.damage += v; break;
                        case ModifierType.Accuracy: sum.accuracy += v; break;
                        case ModifierType.CriticalChance: sum.critical += v; break;
                        case ModifierType.CriticalDamage: sum.criticalDamage += v; break;
                        case ModifierType.MaxHp: sum.hp += v; break;
                        case ModifierType.PhysicalDefence: sum.defence += v; break;
                        case ModifierType.Evasion: sum.evasion += v; break;
                        case ModifierType.Ap: sum.ap += v; break;
                        case ModifierType.Initiative: sum.initiative += v; break;
                        case ModifierType.MagicalDamage: sum.magic += v; break;
                        case ModifierType.MagicalDefence: sum.ward += v; break;
                        case ModifierType.MaxWillPower: sum.willPower += v; break;
                        case ModifierType.WillAgainstControl: sum.will += v; break;
                    }
                }
            }

            return sum;
        }

        /// <summary>
        /// The worn armour pieces as one armour, or null when none is worn. Its channel (10.2 §5f) is what is left of 1 when
        /// each piece holds back its share of a whole set by what its family stops: a whole set of plate stops Infusion,
        /// plate on the body alone half of it.
        /// </summary>
        public FightArmour Armour(FightRules rules, ICharacterValues values)
        {
            float physical = 0f, magical = 0f, kg = 0f, held = 0f;
            var any = false;
            foreach (var slot in ArmourSlots)
            {
                var piece = this[slot];
                var item = piece != null ? _bag.Catalogue.Find(piece.item) : null;
                if (item == null || !(item.fight is FightArmour family))
                {
                    continue;
                }

                any = true;
                var share = item.weight / SetWeight(item.family);
                var fit = (float)(rules.StageFactor(piece.stage) * FightRules.Fit(values, family.Requirements, piece.stage) * share);
                physical += family.Physical * fit;
                magical += family.Magical * fit;
                held += share * (1f - family.Channel);
                kg += item.weight;
            }

            if (!any)
            {
                return null;
            }

            var key = (physical, magical, kg, held);
            if (_armour == null || !_armourKey.Equals(key))
            {
                if (_armour != null)
                {
                    // Nothing changes during a fight, so no fighter still in one holds the old armour.
                    if (Application.isPlaying)
                    {
                        Object.Destroy(_armour);
                    }
                    else
                    {
                        Object.DestroyImmediate(_armour);
                    }
                }

                _armour = FightArmour.Worn(physical, magical, kg, 1f - held);
                _armourKey = key;
            }

            return _armour;
        }

        /// <summary>The share of a piece its wearer meets by its Attribute Requirements (10.2 §5b), 1 for a piece that asks nothing.</summary>
        public double FitOf(ItemPiece piece, ICharacterValues values)
        {
            var item = _bag.Catalogue.Find(piece.item);
            var asked = item?.fight is FightWeapon weapon ? weapon.Requirements
                : item?.fight is FightArmour armour ? armour.Requirements : null;
            return asked == null ? 1d : FightRules.Fit(values, asked, piece.stage);
        }

        private string Hold(ItemPiece piece, FightWeapon weapon)
        {
            var main = this[WearSlot.WeaponOne] == null || this[WearSlot.WeaponTwo] != null ? WearSlot.WeaponOne : WearSlot.WeaponTwo;
            var off = main == WearSlot.WeaponOne ? WearSlot.OffHandOne : WearSlot.OffHandTwo;
            ItemPiece pouch = null;
            if (weapon.Ranged)
            {
                // The set's own pouch stays when one firearm replaces another; otherwise a pouch nothing holds.
                pouch = IsPouch(this[off]) ? this[off] : _bag.Pieces.FirstOrDefault(p => !IsWorn(p) && IsPouch(p));
                if (pouch == null)
                {
                    return "ui.bag.fault.firearm";
                }
            }

            Remove(main);
            Put(main, piece);
            if (pouch != null)
            {
                Remove(off);
                Put(off, pouch);
            }
            else if (this[off] != null && new WeaponSet(weapon, OffOf(this[off]), WeaponOf(this[off])).Fault() != null)
            {
                Remove(off);
            }

            Changed?.Invoke();
            return null;
        }

        private string HoldOff(ItemPiece piece, FightOffHand offHand)
        {
            foreach (var (main, off) in new[] { (WearSlot.WeaponOne, WearSlot.OffHandOne), (WearSlot.WeaponTwo, WearSlot.OffHandTwo) })
            {
                if (WeaponOf(this[main]) is FightWeapon weapon && new WeaponSet(weapon, offHand).Fault() == null)
                {
                    Remove(off);
                    Put(off, piece);
                    Changed?.Invoke();
                    return null;
                }
            }

            return offHand.Kind == OffHandKind.Shield ? "ui.bag.fault.shield" : "ui.bag.fault.pouch";
        }

        private WeaponSet? SetOf(WearSlot main, WearSlot off) =>
            WeaponOf(this[main]) is FightWeapon weapon ? new WeaponSet(weapon, OffOf(this[off]), WeaponOf(this[off])) : (WeaponSet?)null;

        private FightWeapon WeaponOf(ItemPiece piece) => piece != null ? _bag.Catalogue.Find(piece.item)?.fight as FightWeapon : null;

        private FightOffHand OffOf(ItemPiece piece) => piece != null ? _bag.Catalogue.Find(piece.item)?.fight as FightOffHand : null;

        private bool IsPouch(ItemPiece piece) => OffOf(piece) is FightOffHand off && off.Kind == OffHandKind.CartridgePouch;

        private float SetWeight(string family) =>
            _bag.Catalogue.Items.Where(i => i.family == family && i.kind == ItemKind.Equipment).Sum(i => i.weight);

        private void Put(WearSlot slot, ItemPiece piece) => _worn[slot] = piece;

        private void Remove(WearSlot slot) => _worn.Remove(slot);

        /// <summary>Takes a slot's piece off, with what cannot stay without it.</summary>
        private void Drop(WearSlot slot)
        {
            var off = slot == WearSlot.WeaponOne ? WearSlot.OffHandOne : slot == WearSlot.WeaponTwo ? WearSlot.OffHandTwo : (WearSlot?)null;
            var main = slot == WearSlot.OffHandOne ? WearSlot.WeaponOne : slot == WearSlot.OffHandTwo ? WearSlot.WeaponTwo : (WearSlot?)null;
            var firearm = main is WearSlot m && WeaponOf(this[m]) is FightWeapon w && w.Ranged;
            Remove(slot);
            if (off is WearSlot o)
            {
                Remove(o);
            }

            if (firearm)
            {
                Remove(main.Value);
            }
        }

        /// <summary>A piece that left the bag is no longer worn.</summary>
        private void Forget()
        {
            var gone = _worn.Where(p => !_bag.Pieces.Contains(p.Value)).Select(p => p.Key).ToList();
            if (gone.Count == 0)
            {
                return;
            }

            foreach (var slot in gone)
            {
                if (_worn.ContainsKey(slot))
                {
                    Drop(slot);
                }
            }

            Changed?.Invoke();
        }

        private static WearSlot BodySlot(EquipSlot slot) => slot switch
        {
            EquipSlot.Head => WearSlot.Head,
            EquipSlot.Body => WearSlot.Body,
            EquipSlot.Shoulders => WearSlot.Shoulders,
            EquipSlot.Gloves => WearSlot.Gloves,
            EquipSlot.Belt => WearSlot.Belt,
            EquipSlot.Trousers => WearSlot.Trousers,
            EquipSlot.Boots => WearSlot.Boots,
            _ => WearSlot.Necklace,
        };
    }
}
