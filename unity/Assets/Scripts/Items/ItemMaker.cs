using System;
using System.Collections.Generic;
using System.Globalization;
using GameDev.Core.Localization;
using Random = System.Random;

namespace GameDev.Items
{
    /// <summary>The Working Modifier Catalogue's types (19 §2.4).</summary>
    public enum ModifierType
    {
        Damage,
        Accuracy,
        CriticalChance,
        CriticalDamage,
        MaxHp,
        PhysicalDefence,
        MagicalDefence,
        MagicalDamage,
        MaxWillPower,
        Evasion,
        Ap,
        Initiative,
        WillAgainstControl,
    }

    /// <summary>
    /// One maker for every piece (19 §3, §2.2–2.4; S5a.3): the count of modifiers is Binomial(6, p), each modifier is
    /// major or minor at even odds while its group has room, of one of its base's favoured types in 75 % of rolls and of
    /// any type in the rest, of a tier by the base's stage, and of a value within ±20 % of its tier's. A shop makes at its
    /// Shop Skill with no Perception or Luck; a character makes at its Item Creation, Perception and Luck. The numbers are
    /// 19's authoring defaults, checked by <c>tools/balance/item_sim.py</c>.
    /// </summary>
    public static class ItemMaker
    {
        public const int MostModifiers = 6;
        public const int GroupCap = 3;

        private static readonly ModifierType[] Weapon =
            { ModifierType.Damage, ModifierType.Accuracy, ModifierType.CriticalChance, ModifierType.CriticalDamage, ModifierType.MagicalDamage };

        private static readonly ModifierType[] Pouch =
            { ModifierType.Damage, ModifierType.Accuracy, ModifierType.CriticalChance, ModifierType.MagicalDamage };

        private static readonly ModifierType[] Head =
            { ModifierType.Accuracy, ModifierType.CriticalChance, ModifierType.WillAgainstControl, ModifierType.MaxHp };

        private static readonly ModifierType[] Body = { ModifierType.PhysicalDefence, ModifierType.MaxHp };

        private static readonly ModifierType[] Shoulders =
            { ModifierType.PhysicalDefence, ModifierType.MagicalDefence, ModifierType.WillAgainstControl, ModifierType.MaxHp };

        private static readonly ModifierType[] Gloves = { ModifierType.Damage, ModifierType.Accuracy, ModifierType.CriticalChance };

        private static readonly ModifierType[] Trousers = { ModifierType.MaxHp, ModifierType.PhysicalDefence, ModifierType.Evasion };

        private static readonly ModifierType[] Boots = { ModifierType.Evasion, ModifierType.Initiative, ModifierType.Ap };

        private static readonly ModifierType[] Belt = { ModifierType.MaxHp, ModifierType.Ap, ModifierType.PhysicalDefence };

        private static readonly ModifierType[] Ring =
            { ModifierType.Damage, ModifierType.CriticalChance, ModifierType.CriticalDamage, ModifierType.MagicalDamage, ModifierType.MaxWillPower };

        private static readonly ModifierType[] Necklace =
        {
            ModifierType.WillAgainstControl, ModifierType.MaxHp, ModifierType.Initiative, ModifierType.Accuracy, ModifierType.MaxWillPower,
            ModifierType.MagicalDefence,
        };

        private static readonly ModifierType[] All = (ModifierType[])Enum.GetValues(typeof(ModifierType));

        /// <summary>19 §3: the chance of each of the six additions at a shop's skill, with no Perception or Luck.</summary>
        public static double ShopChance(int shopSkill) => Math.Min(0.99d, BaseChance(shopSkill));

        /// <summary>19 §3: the chance of each addition for a character: Item Creation, then Perception, then Luck.</summary>
        public static double Chance(int itemCreation, int perception, int luck) =>
            Math.Min(0.99d, BaseChance(itemCreation) * (1d + (0.10d * (perception - 1) / 99d)) * (1d + (luck / 100d)));

        private static double BaseChance(int skill) => 0.10d + (0.60d * (Math.Max(1, skill) - 1) / 99d);

        /// <summary>Makes one piece of <paramref name="item"/> at <paramref name="stage"/>, each addition at chance <paramref name="p"/>.</summary>
        public static ItemPiece Make(ItemDefinition item, int stage, double p, Random rolls)
        {
            var count = 0;
            for (var i = 0; i < MostModifiers; i++)
            {
                if (rolls.NextDouble() < p)
                {
                    count++;
                }
            }

            return MakeWith(item, stage, count, rolls);
        }

        /// <summary>
        /// Makes one piece of <paramref name="item"/> at <paramref name="stage"/> with <paramref name="count"/> modifiers
        /// already decided, as a Faction Gift's quality decides them (19 §6.2).
        /// </summary>
        public static ItemPiece MakeWith(ItemDefinition item, int stage, int count, Random rolls)
        {
            var piece = new ItemPiece(item.id, stage);
            count = Math.Max(0, Math.Min(MostModifiers, count));
            int majors = 0, minors = 0;
            var favoured = Favoured(item);
            for (var i = 0; i < count; i++)
            {
                var major = majors < GroupCap && (minors >= GroupCap || rolls.NextDouble() < 0.5d);
                if (major)
                {
                    majors++;
                }
                else
                {
                    minors++;
                }

                var type = favoured.Length > 0 && rolls.NextDouble() < 0.75d ? favoured[rolls.Next(favoured.Length)] : All[rolls.Next(All.Length)];
                var tier = Tier(stage, rolls.NextDouble());
                var value = MajorValue(type) * (1d + (0.5d * (9 - tier))) * (major ? 1d : 0.5d) * (0.8d + (0.4d * rolls.NextDouble()));
                piece.modifiers.Add(new ItemModifier { type = type.ToString(), major = major, tier = tier, value = (float)value });
            }

            return piece;
        }

        /// <summary>
        /// 19 §2.4: at stage 9 a modifier is tier 9 in 70 % of rolls, tier 8 in 25 % and tier 7 in 5 %; a higher stage
        /// moves the same odds up a tier a stage (working reading).
        /// </summary>
        public static int Tier(int stage, double roll) => Math.Max(0, (roll < 0.70d ? 9 : roll < 0.95d ? 8 : 7) - (9 - stage));

        /// <summary>19 §2.4: a major modifier of tier 9, in its own unit: shares for damage, HP, Will Power, magical damage and the critical multiplier.</summary>
        public static double MajorValue(ModifierType type) => type switch
        {
            ModifierType.Damage => 0.11d,
            ModifierType.Accuracy => 5d,
            ModifierType.CriticalChance => 11d,
            ModifierType.CriticalDamage => 0.30d,
            ModifierType.MaxHp => 0.09d,
            ModifierType.PhysicalDefence => 5d,
            ModifierType.MagicalDefence => 4d,
            ModifierType.MagicalDamage => 0.11d,
            ModifierType.MaxWillPower => 0.09d,
            ModifierType.Evasion => 4d,
            ModifierType.Ap => 0.8d,
            ModifierType.Initiative => 3d,
            _ => 4d,
        };

        /// <summary>Whether a type's value is a share, shown as a percentage.</summary>
        public static bool IsShare(ModifierType type) => type == ModifierType.Damage || type == ModifierType.CriticalDamage
            || type == ModifierType.MaxHp || type == ModifierType.MagicalDamage || type == ModifierType.MaxWillPower;

        /// <summary>19 §2.4's favoured types of a base; shields take the body's, a working reading.</summary>
        public static ModifierType[] Favoured(ItemDefinition item)
        {
            switch (item.slot)
            {
                case EquipSlot.OneHand:
                case EquipSlot.TwoHands:
                    return Weapon;
                case EquipSlot.OffHand:
                    return item.id == "cartridge_pouch" ? Pouch : Body;
                case EquipSlot.Head:
                    return Head;
                case EquipSlot.Body:
                    return Body;
                case EquipSlot.Shoulders:
                    return Shoulders;
                case EquipSlot.Gloves:
                    return Gloves;
                case EquipSlot.Trousers:
                    return Trousers;
                case EquipSlot.Boots:
                    return Boots;
                case EquipSlot.Belt:
                    return Belt;
                case EquipSlot.Ring:
                    return Ring;
                case EquipSlot.Necklace:
                    return Necklace;
                default:
                    return new ModifierType[0];
            }
        }

        public static bool TryType(ItemModifier modifier, out ModifierType type) => Enum.TryParse(modifier.type, out type);

        /// <summary>A modifier in words, as "+12 % damage".</summary>
        public static string Describe(ItemModifier modifier)
        {
            if (!TryType(modifier, out var type))
            {
                return modifier.type;
            }

            var shown = IsShare(type) ? modifier.value * 100d : modifier.value;
            return Loc.Format("ui.mod." + type.ToString().ToLowerInvariant(), shown.ToString("0.#", CultureInfo.InvariantCulture));
        }

        /// <summary>A piece's modifiers in words, joined, or empty for a Normal piece.</summary>
        public static string Describe(IReadOnlyList<ItemModifier> modifiers)
        {
            var parts = new List<string>();
            foreach (var modifier in modifiers)
            {
                parts.Add(Describe(modifier));
            }

            return string.Join(", ", parts);
        }
    }
}
