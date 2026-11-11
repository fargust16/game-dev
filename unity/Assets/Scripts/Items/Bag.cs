using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Player.Progression;

namespace GameDev.Items
{
    /// <summary>07.2 §5.2: total weight as a share of Comfortable Capacity.</summary>
    public enum LoadBand
    {
        Normal,
        High,
        Overloaded,
        CriticalOverload,
    }

    /// <summary>
    /// The character's bag (19 §4.1; S5a.1): unlimited, weight the only limit. Goods stack by id, pieces of equipment are
    /// carried one by one, and money is a whole number of bronze that weighs nothing. Comfortable Capacity is 20 kg plus
    /// Strength, read at each use, and nothing is taken that would carry the load past twice that (07.2 §5.2).
    /// </summary>
    public sealed class Bag
    {
        /// <summary>07.2 §5.2: the hard limit, as a share of Comfortable Capacity.</summary>
        public const float HardLimit = 2f;

        private readonly ItemCatalogue _catalogue;
        private readonly ICharacterValues _character;
        private readonly Dictionary<string, int> _stacks = new Dictionary<string, int>();
        private readonly List<ItemPiece> _pieces = new List<ItemPiece>();

        public Bag(ItemCatalogue catalogue, ICharacterValues character)
        {
            _catalogue = catalogue;
            _character = character;
        }

        /// <summary>Anything in the bag or the purse changed.</summary>
        public event Action Changed;

        public ItemCatalogue Catalogue => _catalogue;

        public int Bronze { get; private set; }

        public IReadOnlyList<ItemPiece> Pieces => _pieces;

        /// <summary>Each stack: its item and how many, in the catalogue's order.</summary>
        public IReadOnlyList<(ItemDefinition item, int count)> Stacks =>
            _catalogue.Items.Where(i => _stacks.ContainsKey(i.id)).Select(i => (i, _stacks[i.id])).ToList();

        public int Count(string id) => _stacks.TryGetValue(id, out var n) ? n : 0;

        /// <summary>The total carried weight in kg, equipment included (07.2 §5.2).</summary>
        public float Weight => _stacks.Sum(s => WeightOf(s.Key) * s.Value) + _pieces.Sum(p => WeightOf(p.item));

        public float Comfortable => 20f + _character.Get(CharacterAttribute.Strength);

        public LoadBand Band => BandOf(Weight / Comfortable);

        /// <summary>Normal up to 100 %, High to 125 %, Overloaded to 150 %, Critical Overload beyond (07.2 §5.2).</summary>
        public static LoadBand BandOf(float share) =>
            share <= 1f ? LoadBand.Normal : share <= 1.25f ? LoadBand.High : share <= 1.5f ? LoadBand.Overloaded : LoadBand.CriticalOverload;

        /// <summary>Whether <paramref name="kg"/> more stays within the hard limit.</summary>
        public bool CanCarry(float kg) => Weight + kg <= (HardLimit * Comfortable) + 1e-4f;

        /// <summary>Takes <paramref name="count"/> of a stacking item, or refuses the whole of it beyond the hard limit.</summary>
        public bool Add(string id, int count = 1)
        {
            var item = _catalogue.Find(id);
            if (item == null || !item.Stacks || count <= 0 || !CanCarry(item.weight * count))
            {
                return false;
            }

            _stacks[id] = Count(id) + count;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Gives up <paramref name="count"/> of a stacking item; false, changing nothing, if there are fewer.</summary>
        public bool Remove(string id, int count = 1)
        {
            var have = Count(id);
            if (count <= 0 || have < count)
            {
                return false;
            }

            if (have == count)
            {
                _stacks.Remove(id);
            }
            else
            {
                _stacks[id] = have - count;
            }

            Changed?.Invoke();
            return true;
        }

        /// <summary>Takes a piece of equipment, or refuses it beyond the hard limit.</summary>
        public bool AddPiece(ItemPiece piece)
        {
            var item = piece != null ? _catalogue.Find(piece.item) : null;
            if (item == null || item.Stacks || !CanCarry(item.weight))
            {
                return false;
            }

            _pieces.Add(piece);
            Changed?.Invoke();
            return true;
        }

        public bool RemovePiece(ItemPiece piece)
        {
            if (!_pieces.Remove(piece))
            {
                return false;
            }

            Changed?.Invoke();
            return true;
        }

        public void Earn(int bronze)
        {
            if (bronze <= 0)
            {
                return;
            }

            Bronze += bronze;
            Changed?.Invoke();
        }

        /// <summary>Pays <paramref name="bronze"/>; false, paying nothing, if the purse is short.</summary>
        public bool Spend(int bronze)
        {
            if (bronze < 0 || bronze > Bronze)
            {
                return false;
            }

            Bronze -= bronze;
            Changed?.Invoke();
            return true;
        }

        private float WeightOf(string id) => _catalogue.Find(id)?.weight ?? 0f;
    }
}
