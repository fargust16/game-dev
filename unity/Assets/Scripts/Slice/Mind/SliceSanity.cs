using System;
using GameDev.World.State;

namespace GameDev.Slice.Mind
{
    /// <summary>
    /// The character's Sanity (07.2 §3.4), 0 to 100, kept in the world state as the points lost so a new game starts it
    /// full. S5b.5 spends it on the Limit potions (05.1 *Requirements and Sanity in the slice*); its bands' penalties and
    /// symptoms, full exhaustion's damage and sleep's return come with S5b.6.
    /// </summary>
    public sealed class SliceSanity
    {
        public const int Max = 100;

        /// <summary>World state key: the Sanity points lost, 0 to 100.</summary>
        public const string LostKey = "player.sanity.lost";

        private readonly IWorldState _state;

        public SliceSanity(IWorldState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// <summary>Sanity changed: its new value.</summary>
        public event Action<int> Changed;

        public int Value => Max - Math.Max(0, Math.Min(Max, _state.Get(LostKey)));

        /// <summary>Takes <paramref name="points"/> away, never below 0.</summary>
        public void Lose(int points)
        {
            if (points <= 0)
            {
                return;
            }

            _state.Set(LostKey, Math.Min(Max, _state.Get(LostKey) + points));
            Changed?.Invoke(Value);
        }

        /// <summary>Gives <paramref name="points"/> back, never above 100.</summary>
        public void Restore(int points)
        {
            if (points <= 0)
            {
                return;
            }

            _state.Set(LostKey, Math.Max(0, _state.Get(LostKey) - points));
            Changed?.Invoke(Value);
        }
    }
}
