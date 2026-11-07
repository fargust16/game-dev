using System;
using System.Collections.Generic;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;

namespace GameDev.World.Poi
{
    /// <summary>Where a POI stands in its life. Kept as a number in the world state under <c>poi.&lt;id&gt;.phase</c>.</summary>
    public enum PoiPhase
    {
        Pending = 0,
        Open = 1,
        Resolved = 2,
    }

    /// <summary>What the player knows of a POI (04; 09). It rises and never falls.</summary>
    public enum PoiDiscovery
    {
        Unknown = 0,
        Hinted = 1,
        Marked = 2,
    }

    /// <summary>How a POI ended: nobody came, or the player's scene ended it (S2).</summary>
    public enum PoiResolution
    {
        None = 0,
        Unattended = 1,
        Participated = 2,
    }

    /// <summary>
    /// Runs each POI's Active Window on the clock's ticks (04 *Lazy activation*; 17 Q.3): it opens at its minute, or once
    /// one of the POIs it follows has resolved, and when its window closes with nobody there it resolves unattended,
    /// drawing one outcome from the seed and raising that outcome's keys as Aftermath. The whole state lives in the
    /// world state store, so a save keeps it and a load lets the director catch up. Urgency is the definition's words;
    /// no countdown is ever exposed (04 rule 5).
    /// </summary>
    public sealed class PoiDirector : IDisposable
    {
        private readonly IReadOnlyList<PoiDefinition> _pois;
        private readonly IWorldState _state;
        private readonly IWorldSeed _seed;
        private readonly string _stream;
        private readonly IWorldClock _clock;

        /// <param name="unattendedStream">The seed stream unattended outcomes are drawn from, forked per POI by its id.</param>
        public PoiDirector(IReadOnlyList<PoiDefinition> pois, IWorldState state, IWorldSeed seed, string unattendedStream, IWorldClock clock)
        {
            _pois = pois ?? throw new ArgumentNullException(nameof(pois));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _seed = seed ?? throw new ArgumentNullException(nameof(seed));
            _stream = string.IsNullOrWhiteSpace(unattendedStream) ? throw new ArgumentException("A stream is required.", nameof(unattendedStream)) : unattendedStream;
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _clock.TickBoundary += Evaluate;
        }

        /// <summary>A POI's window has opened.</summary>
        public event Action<PoiDefinition> Opened;

        /// <summary>A POI has resolved; <c>known</c> says whether the player had heard of it, so the world may announce it.</summary>
        public event Action<PoiDefinition, PoiResolution, bool> Resolved;

        public IReadOnlyList<PoiDefinition> Pois => _pois;

        public PoiPhase Phase(PoiDefinition poi) => (PoiPhase)_state.Get(Key(poi, "phase"));

        public PoiDiscovery Discovery(PoiDefinition poi) => (PoiDiscovery)_state.Get(Key(poi, "discovery"));

        public PoiResolution Resolution(PoiDefinition poi) => (PoiResolution)_state.Get(Key(poi, "resolution"));

        /// <summary>The name of the unattended outcome drawn, or null.</summary>
        public string Outcome(PoiDefinition poi)
        {
            var index = _state.Get(Key(poi, "outcome")) - 1;
            return index >= 0 && index < poi.Unattended.Length ? poi.Unattended[index].name : null;
        }

        /// <summary>A source told the player of it (S1.3). Knowledge may come before the window opens and never falls.</summary>
        public void Reveal(PoiDefinition poi, PoiDiscovery level)
        {
            var key = Key(poi, "discovery");
            if ((int)level > _state.Get(key))
            {
                _state.Set(key, (int)level);
            }
        }

        /// <summary>The player's scene ended an open POI (S2); the scene writes its own Aftermath.</summary>
        public void ResolveParticipated(PoiDefinition poi)
        {
            if (Phase(poi) != PoiPhase.Open)
            {
                throw new InvalidOperationException($"POI {poi.Id} is not open.");
            }

            Resolve(poi, PoiResolution.Participated);
            Evaluate(_clock.Tick);
        }

        /// <summary>Opens and closes whatever is due by <paramref name="tick"/>; the clock calls it every tick, and a load calls it once to catch up.</summary>
        public void Evaluate(long tick)
        {
            bool changed;
            do
            {
                changed = false;
                foreach (var poi in _pois)
                {
                    var phase = Phase(poi);
                    if (phase == PoiPhase.Pending && Appears(poi, tick))
                    {
                        _state.Set(Key(poi, "opened_at"), (int)tick);
                        _state.Set(Key(poi, "phase"), (int)PoiPhase.Open);
                        Opened?.Invoke(poi);
                        changed = true;
                        phase = PoiPhase.Open;
                    }

                    if (phase == PoiPhase.Open && tick >= ClosesAt(poi))
                    {
                        ResolveUnattended(poi);
                        changed = true;
                    }
                }
            }
            while (changed);
        }

        public void Dispose()
        {
            _clock.TickBoundary -= Evaluate;
        }

        private bool Appears(PoiDefinition poi, long tick)
        {
            if (poi.AppearsAtMinute >= 0 && tick >= poi.AppearsAtMinute)
            {
                return true;
            }

            foreach (var before in poi.AppearsAfterAny)
            {
                if (before != null && Phase(before) == PoiPhase.Resolved)
                {
                    return true;
                }
            }

            return false;
        }

        private long ClosesAt(PoiDefinition poi)
        {
            var closes = long.MaxValue;
            if (poi.WindowMinutes > 0)
            {
                closes = (long)_state.Get(Key(poi, "opened_at")) + poi.WindowMinutes;
            }

            if (poi.ClosesAtMinute >= 0)
            {
                closes = Math.Min(closes, poi.ClosesAtMinute);
            }

            return closes;
        }

        private void ResolveUnattended(PoiDefinition poi)
        {
            var index = Draw(poi);
            if (index >= 0)
            {
                _state.Set(Key(poi, "outcome"), index + 1);
                foreach (var key in poi.Unattended[index].raises)
                {
                    _state.Raise(key);
                }
            }

            Resolve(poi, PoiResolution.Unattended);
        }

        private void Resolve(PoiDefinition poi, PoiResolution how)
        {
            _state.Set(Key(poi, "resolution"), (int)how);
            _state.Set(Key(poi, "phase"), (int)PoiPhase.Resolved);
            Resolved?.Invoke(poi, how, Discovery(poi) != PoiDiscovery.Unknown);
        }

        private int Draw(PoiDefinition poi)
        {
            var outcomes = poi.Unattended;
            var total = 0d;
            foreach (var outcome in outcomes)
            {
                total += Math.Max(0f, outcome.weight);
            }

            if (total <= 0d)
            {
                return outcomes.Length > 0 ? 0 : -1;
            }

            var roll = _seed.Fork(_stream, StableHash(poi.Id)).NextDouble() * total;
            for (var i = 0; i < outcomes.Length; i++)
            {
                roll -= Math.Max(0f, outcomes[i].weight);
                if (roll < 0d)
                {
                    return i;
                }
            }

            return outcomes.Length - 1;
        }

        private static string Key(PoiDefinition poi, string part) => "poi." + poi.Id + "." + part;

        /// <summary>FNV-1a over the id: string.GetHashCode differs between runtimes, and the draw must not.</summary>
        private static int StableHash(string text)
        {
            unchecked
            {
                var hash = (int)2166136261;
                foreach (var c in text)
                {
                    hash = (hash ^ c) * 16777619;
                }

                return hash;
            }
        }
    }
}
