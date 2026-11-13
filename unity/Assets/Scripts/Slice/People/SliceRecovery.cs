using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Items;
using GameDev.Player.Progression;
using GameDev.Slice.Save;
using GameDev.World.Seed;
using GameDev.World.State;
using UnityEngine;
using Random = System.Random;

namespace GameDev.Slice.People
{
    /// <summary>
    /// 19 §6 (S5c.7): on a person's death each piece of its equipment is checked for recovery on its own, at
    /// <c>p = min(0.99, 0.35 × (1 + Luck / 100))</c>, the looter's Luck. In the slice what a person carried is written
    /// down when it falls, and the roll is made when the character searches the body, once, by the seed forked with the
    /// body, so a save keeps both. A piece the bag cannot carry stays with the body; the roll trains Luck. Nobody carries
    /// coin to recover, and a beast leaves its ingredients by Skinning instead.
    /// </summary>
    public sealed class SliceRecovery
    {
        /// <summary>19 §6: the base chance of each piece and the most any Luck gives.</summary>
        public const double BaseChance = 0.35;

        public const double MostChance = 0.99;

        private readonly IWorldState _state;
        private readonly IWorldSeed _seed;
        private readonly Bag _bag;
        private readonly CharacterSheet _sheet;
        private readonly ItemCatalogue _items;

        public SliceRecovery(IWorldState state, IWorldSeed seed, Bag bag, CharacterSheet sheet, ItemCatalogue items)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _seed = seed;
            _bag = bag ?? throw new ArgumentNullException(nameof(bag));
            _sheet = sheet ?? throw new ArgumentNullException(nameof(sheet));
            _items = items ?? throw new ArgumentNullException(nameof(items));
        }

        /// <summary>19 §6: the chance of each piece at the looter's <paramref name="luck"/>.</summary>
        public static double Chance(int luck) => Math.Min(MostChance, BaseChance * (1d + (luck / 100d)));

        /// <summary>The number a body is known by: its path in its scene, which a saved scene keeps.</summary>
        public static int Of(Transform body) => StableHash.Of(SavedObjects.PathOf(body));

        /// <summary>Writes down what a person carried when it fell: the items of its equipment, by id.</summary>
        public void Leave(Transform body, IEnumerable<string> items)
        {
            if (body == null)
            {
                return;
            }

            var who = Of(body);
            var ids = (items ?? Enumerable.Empty<string>()).Where(id => _items.Find(id) != null).ToList();
            _state.Set(Key(who, "gear"), ids.Count);
            _state.Set(Key(who, "left"), 1);
            for (var i = 0; i < ids.Count; i++)
            {
                _state.Set(Key(who, $"gear.{i}"), StableHash.Of(ids[i]));
            }
        }

        /// <summary>What the person carried, by id, as written down when it fell.</summary>
        public IReadOnlyList<string> Gear(Transform body)
        {
            var who = Of(body);
            var count = _state.Get(Key(who, "gear"));
            var ids = new List<string>();
            for (var i = 0; i < count; i++)
            {
                var hash = _state.Get(Key(who, $"gear.{i}"));
                var item = _items.Items.FirstOrDefault(x => x != null && StableHash.Of(x.id) == hash);
                if (item != null)
                {
                    ids.Add(item.id);
                }
            }

            return ids;
        }

        /// <summary>Whether a body can be searched: a person's whose fall was written down, not yet searched.</summary>
        public bool CanSearch(Transform body) => body != null && _state.IsSet(Key(Of(body), "left")) && !_state.IsSet(Key(Of(body), "searched"));

        /// <summary>
        /// Searches the body once: each piece is recovered at <see cref="Chance"/> of the character's Luck, into the bag
        /// when it can carry it; the rest stay. Returns the pieces taken and those found too heavy to carry.
        /// </summary>
        public (IReadOnlyList<ItemPiece> Taken, IReadOnlyList<string> TooHeavy) Search(Transform body)
        {
            var taken = new List<ItemPiece>();
            var heavy = new List<string>();
            if (!CanSearch(body))
            {
                return (taken, heavy);
            }

            var who = Of(body);
            _state.Set(Key(who, "searched"), 1);
            var gear = Gear(body);
            if (gear.Count == 0)
            {
                return (taken, heavy);
            }

            var chance = Chance(_sheet.Get(CharacterAttribute.Luck));
            var rolls = _seed != null ? _seed.Fork(SliceSeedStreams.Recovery, who) : new Random(who);
            foreach (var id in gear)
            {
                if (rolls.NextDouble() >= chance)
                {
                    continue;
                }

                var piece = new ItemPiece(id);
                if (_bag.AddPiece(piece))
                {
                    taken.Add(piece);
                }
                else
                {
                    heavy.Add(id);
                }
            }

            _sheet.Gain(CharacterAttribute.Luck, Growth.RecoveryLuckXp);
            return (taken, heavy);
        }

        private static string Key(int who, string part) => $"fallen.{who}.{part}";
    }
}
