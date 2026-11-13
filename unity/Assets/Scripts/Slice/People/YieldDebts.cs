using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Confront.Rules;
using GameDev.Items;
using GameDev.Slice.BreakIns;
using GameDev.Slice.Fights;
using GameDev.Slice.Gear;
using GameDev.World.State;
using UnityEngine;

namespace GameDev.Slice.People
{
    /// <summary>
    /// A Yield's terms paid from the bag (10.2 §9 *Yield in the slice*; S5c.7), with no scene code. Veinmarket and street
    /// robbers take every coin the character carries, and its most valuable worn piece too when the coins come to less
    /// than 1 000 bronze. The Wardens take the character to their lock-up: the stolen goods it carries go back to their
    /// owners, it is no longer Wanted, and its weapons go into their store, every weapon and other-hand piece it carries,
    /// until it pays the fine it owes at the lock-up's desk (<see cref="WardensDesk"/>).
    /// </summary>
    public sealed class YieldDebts
    {
        /// <summary>10.2 §9: coins under this, and the robbers take the most valuable worn piece too.</summary>
        public const int RobbersSpare = 1000;

        private readonly IWorldState _state;
        private readonly Bag _bag;
        private readonly Outfit _outfit;
        private readonly SliceBreakIns _breakIns;
        private readonly List<ItemPiece> _held = new List<ItemPiece>();

        public YieldDebts(IWorldState state, Bag bag, Outfit outfit, SliceBreakIns breakIns)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _bag = bag ?? throw new ArgumentNullException(nameof(bag));
            _outfit = outfit;
            _breakIns = breakIns;
        }

        /// <summary>The weapons in the Wardens' store.</summary>
        public IReadOnlyList<ItemPiece> Held => _held;

        /// <summary>The fine owed to the Wardens, in bronze.</summary>
        public int Owed => _state.Get(SliceConfront.OwedKey(SliceConfront.Wardens));

        /// <summary>Whether the desk has anything to settle: a fine owed or weapons held.</summary>
        public bool Owing => Owed > 0 || _held.Count > 0;

        /// <summary>A weapon or the other hand's piece: what the Wardens keep.</summary>
        public bool IsWeapon(ItemPiece piece)
        {
            var item = piece != null ? _bag.Catalogue.Find(piece.item) : null;
            return item != null && (item.fight is FightWeapon || item.fight is FightOffHand);
        }

        /// <summary>The robbers' take: every coin, and the most valuable worn piece when the coins came to less than 1 000.</summary>
        public (int Bronze, ItemPiece Piece) Rob()
        {
            var bronze = _bag.Bronze;
            if (bronze > 0)
            {
                _bag.Spend(bronze);
            }

            ItemPiece piece = null;
            if (bronze < RobbersSpare && _outfit != null)
            {
                piece = ((WearSlot[])Enum.GetValues(typeof(WearSlot)))
                    .Select(slot => _outfit[slot])
                    .Where(p => p != null)
                    .Distinct()
                    .OrderByDescending(p => p.Price)
                    .FirstOrDefault();
                if (piece != null)
                {
                    _bag.RemovePiece(piece);
                }
            }

            return (bronze, piece);
        }

        /// <summary>
        /// The lock-up's terms: the fine owed grows by <paramref name="fine"/>, the night's stolen goods go back, the
        /// character is no longer Wanted, and every weapon it carries goes into the store. Returns how many were held.
        /// </summary>
        public int LockUp(int fine)
        {
            _state.Set(SliceConfront.OwedKey(SliceConfront.Wardens), Owed + Math.Max(0, fine));
            _breakIns?.GiveBack();
            _breakIns?.ClearWanted();
            var weapons = _bag.Pieces.Where(IsWeapon).ToList();
            foreach (var piece in weapons)
            {
                if (_bag.RemovePiece(piece))
                {
                    _held.Add(piece);
                }
            }

            Mark();
            return weapons.Count;
        }

        /// <summary>
        /// Pays the fine at the desk and takes the weapons back, those the bag can carry; the rest stay in the store
        /// until there is room. False when the character carries less than it owes.
        /// </summary>
        public bool Pay()
        {
            var owed = Owed;
            if (owed > 0 && !_bag.Spend(owed))
            {
                return false;
            }

            _state.Set(SliceConfront.OwedKey(SliceConfront.Wardens), 0);
            foreach (var piece in _held.ToList())
            {
                if (_bag.AddPiece(piece))
                {
                    _held.Remove(piece);
                }
            }

            Mark();
            return true;
        }

        private void Mark() => _state.Set(SliceConfront.WeaponsHeldKey, _held.Count > 0 ? 1 : 0);

        [Serializable]
        private sealed class Saved
        {
            public List<ItemPiece> held = new List<ItemPiece>();
        }

        /// <summary>The store's weapons for the Suspend Save (17 X.3).</summary>
        public string Capture() => JsonUtility.ToJson(new Saved { held = new List<ItemPiece>(_held) });

        public void Restore(string payload)
        {
            _held.Clear();
            if (!string.IsNullOrEmpty(payload))
            {
                _held.AddRange(JsonUtility.FromJson<Saved>(payload).held.Where(p => p != null));
            }

            Mark();
        }
    }
}
