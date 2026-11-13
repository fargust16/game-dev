using System;
using GameDev.World.State;

namespace GameDev.Slice.Shops
{
    /// <summary>
    /// 11 *Shops and Masters* (S5c.7; SLICE_PEOPLE §1): if Harrow is lost, his apprentice Pip Vance opens the shop the
    /// next morning, at 08:00, and teaches at double price. Harrow is lost when the Wardens took him, when he vanished
    /// before them, or when he fell. A shop the Wardens sealed stays shut, its door not opening, so Pip keeps only an
    /// unsealed one (the agent's reading, 99 VS-NEWS). When the loss happened is kept in the world state, so a save keeps it.
    /// </summary>
    public sealed class ShopStandIn
    {
        public const string Shop = "harrow";
        public const string Apprentice = "pip";

        /// <summary>11: one who took a shop over teaches at this many times the price.</summary>
        public const int LessonFactor = 2;

        /// <summary>The tick, plus one, from which Pip keeps the shop.</summary>
        public const string FromKey = "shop.harrow.pip.from";

        public const string TakenKey = "aftermath.harrow.taken";
        public const string VanishedKey = "aftermath.harrow.vanished";
        public const string SealedKey = "aftermath.harrow.shop_sealed";

        private const int Morning = 8 * 60;
        private const int Day = 1440;

        private readonly IWorldState _state;
        private readonly Func<string, bool> _gone;

        /// <param name="gone">Whether a named person has fallen for good (<c>PersonHours.Gone</c>); null for never.</param>
        public ShopStandIn(IWorldState state, Func<string, bool> gone)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _gone = gone;
        }

        public bool Lost => _state.IsSet(TakenKey) || _state.IsSet(VanishedKey) || (_gone != null && _gone(Shop));

        public bool Sealed => _state.IsSet(SealedKey);

        /// <summary>Notes the morning Pip opens, once Harrow is lost; the world calls it each tick.</summary>
        public void Notice(long tick)
        {
            if (Lost && !_state.IsSet(FromKey))
            {
                _state.Set(FromKey, (int)NextMorning(tick) + 1);
            }
        }

        /// <summary>Whether Pip keeps Harrow's shop at <paramref name="tick"/>.</summary>
        public bool Keeps(long tick)
        {
            Notice(tick);
            return Lost && !Sealed && _state.IsSet(FromKey) && tick >= _state.Get(FromKey) - 1L;
        }

        /// <summary>Who keeps <paramref name="keeper"/>'s shop at <paramref name="tick"/>.</summary>
        public string KeptBy(string keeper, long tick) => keeper == Shop && Keeps(tick) ? Apprentice : keeper;

        /// <summary>The first 08:00 after <paramref name="tick"/>.</summary>
        public static long NextMorning(long tick)
        {
            var morning = ((tick / Day) * Day) + Morning;
            return morning > tick ? morning : morning + Day;
        }
    }
}
