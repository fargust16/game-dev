using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Player.Progression;

namespace GameDev.Slice.Creation
{
    /// <summary>One Character Archetype (07 *Attributes at the start*): its name's key and what it adds to the starting 1.</summary>
    public sealed class Archetype
    {
        public readonly string Key;
        public readonly IReadOnlyDictionary<CharacterAttribute, int> Added;

        public Archetype(string key, params (CharacterAttribute attribute, int added)[] added)
        {
            Key = key;
            Added = added.ToDictionary(a => a.attribute, a => a.added);
        }

        public int Points => Added.Values.Sum();
    }

    /// <summary>
    /// 07 *Character Start*: every attribute starts at 1, and once in a life the player either spends 9 free points by
    /// hand or takes one of three archetypes of 11 points. The 9 points and the three names are the owner's; the 11 points
    /// and the spreads are authoring defaults, measured again after the combat rework by `tools/balance/archetype_sim.py`
    /// (S5b.1): in the same armour the three stand within a few points of each other in a first fight, and the Боец's
    /// Strength 6 is what lets it wear chain, so the spreads stand.
    /// </summary>
    public static class CharacterStart
    {
        public const int FreePoints = 9;

        public static readonly Archetype[] Archetypes =
        {
            new Archetype("fighter", (CharacterAttribute.Strength, 5), (CharacterAttribute.Endurance, 4), (CharacterAttribute.Dexterity, 2)),
            new Archetype("sleuth", (CharacterAttribute.Perception, 5), (CharacterAttribute.Intelligence, 4), (CharacterAttribute.Dexterity, 2)),
            new Archetype("ragamuffin", (CharacterAttribute.Will, 5), (CharacterAttribute.Luck, 4), (CharacterAttribute.Dexterity, 2)),
        };

        /// <summary>The longest name a character may take, in letters (authoring default).</summary>
        public const int NameLength = 24;

        /// <summary>A name as kept: trimmed, inner runs of space made one, cut to <see cref="NameLength"/>.</summary>
        public static string CleanName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            var words = name.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            var clean = string.Join(" ", words);
            return clean.Length > NameLength ? clean.Substring(0, NameLength).TrimEnd() : clean;
        }

        /// <summary>
        /// The sheet's attributes set to the start: 1 and <paramref name="added"/>. Skills stay at 1. False, with the
        /// sheet unchanged, when the points are not exactly an archetype's spread or exactly 9 free ones.
        /// </summary>
        public static bool Apply(CharacterSheet sheet, IReadOnlyDictionary<CharacterAttribute, int> added, Archetype archetype)
        {
            if (sheet == null || added == null || added.Values.Any(v => v < 0))
            {
                return false;
            }

            var points = added.Values.Sum();
            var fits = archetype != null
                ? Enum.GetValues(typeof(CharacterAttribute)).Cast<CharacterAttribute>()
                    .All(a => (added.TryGetValue(a, out var v) ? v : 0) == (archetype.Added.TryGetValue(a, out var w) ? w : 0))
                : points == FreePoints;
            if (!fits)
            {
                return false;
            }

            foreach (CharacterAttribute attribute in Enum.GetValues(typeof(CharacterAttribute)))
            {
                sheet.Set(attribute, CharacterSheet.Lowest + (added.TryGetValue(attribute, out var v) ? v : 0));
            }

            return true;
        }
    }
}
