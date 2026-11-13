using System;
using GameDev.Dialogue;
using GameDev.Player.Progression;
using GameDev.World.Seed;
using GameDev.World.State;

namespace GameDev.Slice.Alchemy
{
    /// <summary>The kinds of deed of 05.1 *Conduct Progress in the slice*, four for each Arcana.</summary>
    public enum ConductDeed
    {
        /// <summary>Жрица: a Convince check won, with Внушение or without it.</summary>
        Convince,

        /// <summary>Жрица: a conversation overheard unseen.</summary>
        Overhear,

        /// <summary>Жрица: a watched or forbidden place gone through with no Notice Meter filled.</summary>
        Unnoticed,

        /// <summary>Жрица: something taken unseen, by theft or from a watched place.</summary>
        TakeUnseen,

        /// <summary>Император: a fight won against a side at least as strong.</summary>
        StrongerSide,

        /// <summary>Император: a Threaten check won, Стой! outside a fight included.</summary>
        Threaten,

        /// <summary>Император: a stronger single enemy beaten, a deeper Limit or a glowing beast.</summary>
        StrongerFoe,

        /// <summary>Император: a fight finished when a way out was open.</summary>
        Stand,
    }

    /// <summary>
    /// Conduct Progress in the slice (S5b.7; 05.1 *Conduct Progress in the slice*), with no scene code. A deed that fits
    /// the Arcana the character has entered is worth 1 point, and 2 when it settles a POI's Decision Node; it counts only
    /// at Limit 9, each kind at most three times, each time a different person or place; an act against the Arcana takes a
    /// point away, never below zero. No counter is shown: each deed is felt in one short line, and the threshold of Limit 8
    /// in another. The points are the world state's <see cref="SliceAdvancement.ConductKey"/>, which the drink reads and a
    /// new Limit sets to zero; the kinds' counts and the people and places are kept beside it, so a save keeps them.
    /// </summary>
    public sealed class SliceConduct
    {
        /// <summary>05.1: each kind of deed counts at most three times.</summary>
        public const int KindLimit = 3;

        /// <summary>The only Limit whose deeds count in the slice, toward Limit 8.</summary>
        public const int CountingLimit = 9;

        private const string Prefix = "player.conduct.9.";

        private readonly IWorldState _state;
        private readonly SliceAdvancement _advancement;

        public SliceConduct(IWorldState state, SliceAdvancement advancement)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _advancement = advancement ?? throw new ArgumentNullException(nameof(advancement));
        }

        /// <summary>A deed or an act against the Arcana was felt: the string table key of its line.</summary>
        public event Action<string> Felt;

        public int Points => Math.Max(0, _state.Get(SliceAdvancement.ConductKey));

        public static Arcana ArcanaOf(ConductDeed deed) => deed <= ConductDeed.TakeUnseen ? Arcana.Priestess : Arcana.Emperor;

        /// <summary>Whether deeds and acts of <paramref name="arcana"/> count now: its own character at Limit 9.</summary>
        public bool Counting(Arcana arcana) =>
            arcana != Arcana.None && _advancement.CurrentArcana == arcana && _advancement.CurrentLimit == CountingLimit;

        /// <summary>How many times a kind has counted.</summary>
        public int Count(ConductDeed deed) => _state.Get(Prefix + deed);

        /// <summary>
        /// A deed done to or at <paramref name="who"/>, a person or a place; it counts, and is felt, only if the character
        /// is of its Arcana at Limit 9, the kind has counted fewer than three times, and not yet with that person or place.
        /// </summary>
        public bool Deed(ConductDeed deed, string who, bool settles = false)
        {
            if (!Counting(ArcanaOf(deed)) || string.IsNullOrEmpty(who) || Count(deed) >= KindLimit)
            {
                return false;
            }

            var met = Prefix + deed + "." + StableHash.Of(who);
            if (_state.IsSet(met))
            {
                return false;
            }

            _state.Raise(met);
            _state.Set(Prefix + deed, Count(deed) + 1);
            var before = Points;
            var after = before + (settles ? 2 : 1);
            _state.Set(SliceAdvancement.ConductKey, after);
            var ready = before < SliceAdvancement.ConductForLimit8 && after >= SliceAdvancement.ConductForLimit8;
            Felt?.Invoke(ready ? "ui.conduct.ready" : "ui.conduct.deed." + Name(ArcanaOf(deed)));
            return true;
        }

        /// <summary>A Social Check won with <paramref name="listener"/>: Convince is Жрица's deed, Threaten Император's.</summary>
        public bool Checked(Approach approach, string listener, bool settles) =>
            approach == Approach.Convince ? Deed(ConductDeed.Convince, "talk." + listener, settles)
            : approach == Approach.Threaten && Deed(ConductDeed.Threaten, "talk." + listener, settles);

        /// <summary>An act against <paramref name="arcana"/>: a point away, felt, if the character is of it at Limit 9 and has one.</summary>
        public bool Against(Arcana arcana)
        {
            if (!Counting(arcana) || Points <= 0)
            {
                return false;
            }

            _state.Set(SliceAdvancement.ConductKey, Points - 1);
            Felt?.Invoke("ui.conduct.against." + Name(arcana));
            return true;
        }

        private static string Name(Arcana arcana) => arcana.ToString().ToLowerInvariant();
    }
}
