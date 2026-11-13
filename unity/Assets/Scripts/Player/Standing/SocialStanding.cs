using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Player.Progression;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;

namespace GameDev.Player.Standing
{
    /// <summary>What the Deeds Record counts toward Reputation (10.4 §4).</summary>
    public enum Deed
    {
        FactionTask,
        NpcHelped,
        CityLiberated,
        TheftNoticed,
        BreakInNoticed,

        /// <summary>A blow at a person who was not fighting (10.2 §5c).</summary>
        Assault,

        /// <summary>The kill of a person who was not an enemy, known even unseen (10.2 §5c).</summary>
        InnocentKilled,
    }

    /// <summary>The reading of the Deeds Record a Social Check takes (10.4 §4).</summary>
    public enum StandingSource
    {
        Reputation,
        Danger,
    }

    /// <summary>A reading and the modifier it gives, for the check and for the list the player reads from Intelligence 20.</summary>
    public readonly struct StandingReading
    {
        public StandingReading(StandingSource source, int modifier)
        {
            Source = source;
            Modifier = modifier;
        }

        public StandingSource Source { get; }

        public int Modifier { get; }
    }

    /// <summary>
    /// The character's standing in conversation (10.4 §4–5): the Deeds Record and its two readings, Reputation and
    /// Danger; each named person's Attitude; and the lies that may come out at the next day boundary. All of it lives in
    /// the world state, so the world.state section saves it. In the slice Reputation is one number, the character's
    /// name in Veilmouth, because everyone in the one city counts as having heard (10.4 §4); the full game keeps one
    /// for each faction.
    /// </summary>
    public sealed class SocialStanding : IDisposable
    {
        public const string ReputationKey = "deeds.reputation";
        public const string KillsKey = "deeds.kills";
        public const string LimitsKey = "deeds.limits";

        /// <summary>10.4 §6 Accusation: a talk let the character off, and the Reputation at or below which a Warden accuses it again.</summary>
        public const string LetOffKey = "accusation.letoff";
        public const string LetOffBelowKey = "accusation.letoff.below";

        /// <summary>
        /// 10.4 §6 *Accusation*, VS-ASSAULT (a): the Promise given, once a life, and the Promise believed, after which an
        /// Assault's every cost to Reputation, its harm and its kills, counts double.
        /// </summary>
        public const string PromiseKey = "accusation.promise";
        public const string PromisedKey = "accusation.promised";

        private const string AttitudePrefix = "attitude.";
        private const string LiePrefix = "lie.";
        private const string ExposedPrefix = "exposed.";

        /// <summary>
        /// How many of the character's lies to this person have come out (S2.7), a world state key that a conversation
        /// can read, as Nell does the next day.
        /// </summary>
        public static string ExposedKey(string personId) => ExposedPrefix + personId;

        private static readonly RequestSize[] Requests = (RequestSize[])Enum.GetValues(typeof(RequestSize));

        private readonly IWorldState _state;
        private readonly StandingRules _rules;
        private readonly IWorldClock _clock;
        private readonly IWorldSeed _seed;
        private readonly string _lieStream;

        /// <param name="lieStream">The seed stream that decides whether a lie comes out, forked by the listener's id and the day.</param>
        public SocialStanding(IWorldState state, StandingRules rules, IWorldClock clock, IWorldSeed seed, string lieStream)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _rules = rules != null ? rules : throw new ArgumentNullException(nameof(rules));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _seed = seed ?? throw new ArgumentNullException(nameof(seed));
            _lieStream = string.IsNullOrWhiteSpace(lieStream) ? throw new ArgumentException("A stream is required.", nameof(lieStream)) : lieStream;
            _clock.TickBoundary += OnTick;
        }

        /// <summary>A lie to this person came out at the day boundary, after its cost was paid.</summary>
        public event Action<string> LieFoundOut;

        public int ReputationPoints => _state.Get(ReputationKey);

        public int Kills => _state.Get(KillsKey);

        public int LimitsReached => _state.Get(LimitsKey);

        /// <summary>10.4 §4: kills, and 5 for each Limit reached.</summary>
        public int Danger => Kills + _rules.DangerPerLimit * LimitsReached;

        public int ReputationModifier => _rules.ReputationModifier(ReputationPoints);

        public int DangerModifier => _rules.DangerModifier(Danger);

        public void Record(Deed deed) => Add(ReputationKey, _rules.Points(deed) * (deed == Deed.Assault || deed == Deed.InnocentKilled ? AssaultFactor : 1));

        public void RecordKill() => Add(KillsKey, 1);

        public void RecordLimitReached() => Add(LimitsKey, 1);

        /// <summary>10.4 §4, 10.2 §5c: an Assault's harm, the HP the people who were not the character's enemies lost over its fight.</summary>
        public void RecordHarm(double hp) => Add(ReputationKey, -_rules.HarmPoints(hp) * AssaultFactor);

        /// <summary>Whether the character may still give the Promise: once a life.</summary>
        public bool PromiseOpen => _state.Get(PromiseKey) == 0;

        /// <summary>2 once a Promise was believed, else 1: what an Assault's costs are multiplied by.</summary>
        public int AssaultFactor => _state.Get(PromisedKey) != 0 ? 2 : 1;

        /// <summary>Gives the Promise, spent whether or not it is <paramref name="believed"/>.</summary>
        public void Promise(bool believed)
        {
            _state.Set(PromiseKey, 1);
            if (believed)
            {
                _state.Set(PromisedKey, 1);
            }
        }

        /// <summary>10.4 §6: a Warden accuses the character: its Reputation is low, and no talk let it off at this level.</summary>
        public bool Accused => ReputationPoints <= _rules.AccusationAt && (_state.Get(LetOffKey) == 0 || ReputationPoints <= _state.Get(LetOffBelowKey));

        /// <summary>10.4 §6: an Accusation paid, by the fine, the lock-up or a bribe: Reputation rises to the settled level.</summary>
        public void Settle()
        {
            if (ReputationPoints < _rules.SettledAt)
            {
                _state.Set(ReputationKey, _rules.SettledAt);
            }

            _state.Set(LetOffKey, 0);
        }

        /// <summary>10.4 §6: a talk lets the character off until its Reputation falls a few points lower.</summary>
        public void LetOff()
        {
            _state.Set(LetOffKey, 1);
            _state.Set(LetOffBelowKey, ReputationPoints - _rules.LetOff);
        }

        /// <summary>How this person feels about the character, from −2 to +2; 0 for anyone never met.</summary>
        public int Attitude(string personId) => _state.Get(AttitudeKey(personId));

        public void ChangeAttitude(string personId, int steps)
        {
            var range = _rules.AttitudeRange;
            _state.Set(AttitudeKey(personId), Math.Clamp(Attitude(personId) + steps, -range, range));
        }

        /// <summary>
        /// 10.4 §4: Persuasion reads Reputation, or Danger with an enemy; Intimidation always Danger; Deception reads
        /// Reputation, and with an enemy a positive Reputation counts against the character.
        /// </summary>
        public StandingReading Reading(CharacterSkill skill, bool enemy) => skill switch
        {
            CharacterSkill.Intimidation => new StandingReading(StandingSource.Danger, DangerModifier),
            CharacterSkill.Persuasion when enemy => new StandingReading(StandingSource.Danger, DangerModifier),
            CharacterSkill.Deception when enemy => new StandingReading(StandingSource.Reputation, -Math.Max(0, ReputationModifier)),
            _ => new StandingReading(StandingSource.Reputation, ReputationModifier),
        };

        /// <summary>10.4 §5: an Intimidation costs this person's Attitude, even when it works.</summary>
        public void Intimidated(string personId) => ChangeAttitude(personId, -_rules.IntimidationCost);

        /// <summary>10.4 §5: a failed lie costs Reputation.</summary>
        public void LieFailed() => Add(ReputationKey, -_rules.LieCost);

        /// <summary>10.4 §5: a lie that worked may come out at the next day boundary; a Small one never does.</summary>
        public void LieTold(string personId, RequestSize request)
        {
            if (_rules.Exposure(request) > 0f)
            {
                Add(LieKey(request, personId), 1);
            }
        }

        /// <summary>10.4 §5: a lie to someone dead or gone can no longer come out.</summary>
        public void ListenerGone(string personId)
        {
            foreach (var request in Requests)
            {
                _state.Set(LieKey(request, personId), 0);
            }
        }

        public void Dispose()
        {
            _clock.TickBoundary -= OnTick;
        }

        private void OnTick(long tick)
        {
            if (tick % GameClock.MinutesPerDay == 0)
            {
                FindOut((int)(tick / GameClock.MinutesPerDay) + 1);
            }
        }

        /// <summary>Each lie told before the boundary gets its one chance, drawn for each listener from the new day's fork.</summary>
        private void FindOut(int day)
        {
            var listeners = _state.Keys
                .Where(k => k.StartsWith(LiePrefix, StringComparison.Ordinal))
                .Select(k => k.Substring(k.IndexOf('.', LiePrefix.Length) + 1))
                .Distinct()
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();

            foreach (var personId in listeners)
            {
                var rolls = _seed.Fork(_lieStream, StableHash.Of(personId), day);
                var found = 0;
                foreach (var request in Requests)
                {
                    var key = LieKey(request, personId);
                    for (var i = _state.Get(key); i > 0; i--)
                    {
                        found += rolls.NextDouble() < _rules.Exposure(request) ? 1 : 0;
                    }

                    _state.Set(key, 0);
                }

                for (var i = 0; i < found; i++)
                {
                    Add(ReputationKey, -_rules.LieCost);
                    ChangeAttitude(personId, -_rules.ExposedCost);
                    Add(ExposedKey(personId), 1);
                    LieFoundOut?.Invoke(personId);
                }
            }
        }

        private void Add(string key, int amount) => _state.Set(key, _state.Get(key) + amount);

        private static string AttitudeKey(string personId) => AttitudePrefix + personId;

        private static string LieKey(RequestSize request, string personId) =>
            LiePrefix + request.ToString().ToLowerInvariant() + "." + personId;
    }
}
