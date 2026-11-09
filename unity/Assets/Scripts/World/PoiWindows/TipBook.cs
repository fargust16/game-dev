using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.World.State;

namespace GameDev.World.Poi
{
    /// <summary>
    /// What the sources say and what the player has heard (17 Q.2; 09). Reading a source hears every line it says at
    /// that moment; a heard tip raises its POI's discovery by 09's rule: Marks marks it, Hints hints it, and Hinted tips
    /// about one POI from two different sources mark it. Heard tips are kept in the world state in the order they were
    /// heard (<c>tip.&lt;id&gt;.heard</c> = its place), so a save keeps them and the Character Notes (S1.4) read them.
    /// </summary>
    public sealed class TipBook
    {
        private const string HeardCount = "tips.heard";
        private const int MinutesPerDay = 1440;

        private readonly Tip[] _tips;
        private readonly IWorldState _state;
        private readonly PoiDirector _director;

        public TipBook(TipCatalogue catalogue, IWorldState state, PoiDirector director)
        {
            _tips = (catalogue ?? throw new ArgumentNullException(nameof(catalogue))).Tips;
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _director = director ?? throw new ArgumentNullException(nameof(director));
        }

        /// <summary>A tip was heard for the first time.</summary>
        public event Action<Tip> Heard;

        /// <summary>The lines <paramref name="source"/> says at <paramref name="tick"/>, in catalogue order.</summary>
        public IReadOnlyList<Tip> Available(TipSource source, long tick) =>
            _tips.Where(t => t.source == source && !t.needsApproach && Says(t, tick)).ToList();

        /// <summary>
        /// Whether the tip <paramref name="id"/> could be told now and has not been heard: its source's day, hour and
        /// condition hold. A tip that needs an Approach is told only this way, by the conversation the Approach wins (S2.4).
        /// </summary>
        public bool CanTell(string id, long tick)
        {
            var tip = Find(id);
            return tip != null && !HasHeard(tip) && Says(tip, tick);
        }

        /// <summary>Tells the tip <paramref name="id"/> if it can be told now: hears it and returns it, or returns null.</summary>
        public Tip Tell(string id, long tick)
        {
            if (!CanTell(id, tick))
            {
                return null;
            }

            var tip = Find(id);
            Hear(tip);
            return tip;
        }

        /// <summary>The tip of this id in the catalogue, or null.</summary>
        public Tip Find(string id) => _tips.FirstOrDefault(t => t.id == id);

        private bool Says(Tip tip, long tick)
        {
            var day = (int)(tick / MinutesPerDay) + 1;
            var minute = (int)(tick % MinutesPerDay);
            return (tip.day == 0 || tip.day == day)
                   && minute >= tip.fromMinute
                   && (string.IsNullOrWhiteSpace(tip.requiresKey) || _state.Get(tip.requiresKey) >= tip.requiresAtLeast);
        }

        /// <summary>Reads the board or asks the person: hears and returns every line the source says now.</summary>
        public IReadOnlyList<Tip> Read(TipSource source, long tick)
        {
            var lines = Available(source, tick);
            foreach (var tip in lines)
            {
                Hear(tip);
            }

            return lines;
        }

        public bool HasHeard(Tip tip) => _state.IsSet(HeardKey(tip));

        /// <summary>Every heard tip, first heard first.</summary>
        public IReadOnlyList<Tip> HeardInOrder() =>
            _tips.Where(HasHeard).OrderBy(t => _state.Get(HeardKey(t))).ToList();

        private void Hear(Tip tip)
        {
            if (HasHeard(tip))
            {
                return;
            }

            var place = _state.Get(HeardCount) + 1;
            _state.Set(HeardCount, place);
            _state.Set(HeardKey(tip), place);
            Discover(tip);
            Heard?.Invoke(tip);
        }

        private void Discover(Tip tip)
        {
            if (tip.poi == null || tip.effect == TipEffect.None)
            {
                return;
            }

            if (tip.effect == TipEffect.Marks)
            {
                _director.Reveal(tip.poi, PoiDiscovery.Marked);
                return;
            }

            _director.Reveal(tip.poi, PoiDiscovery.Hinted);
            var sources = _tips.Where(t => t.poi == tip.poi && t.effect == TipEffect.Hints && HasHeard(t))
                .Select(t => t.source)
                .Distinct()
                .Count();
            if (sources >= 2)
            {
                _director.Reveal(tip.poi, PoiDiscovery.Marked);
            }
        }

        private static string HeardKey(Tip tip) => "tip." + tip.id + ".heard";
    }
}
