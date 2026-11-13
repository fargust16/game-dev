using System;
using GameDev.Items;
using GameDev.Player.Progression;
using GameDev.Player.Standing;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;

namespace GameDev.Slice.Theft
{
    /// <summary>How a theft from a person ended.</summary>
    public enum TheftOutcome
    {
        /// <summary>The purse taken unseen.</summary>
        Lifted,

        /// <summary>The target noticed; nothing taken.</summary>
        Noticed,
    }

    /// <summary>
    /// Theft from a person (S5a.8; 10.3 §6, 19 §6.3): one Reaction Check, whose good zone Theft skill and Dexterity widen
    /// and the target's Perception narrows. A good result takes the purse unseen, its bronze from 19's range for a
    /// townsman; a bad one means the target notices, and the Deeds Record keeps a noticed theft against Reputation. Each
    /// person is tried once a game day; what was tried lives in the world state.
    /// </summary>
    public sealed class SliceTheft
    {
        /// <summary>19 §6.3: a townsman's purse, by theft, in bronze.</summary>
        public const int PurseLeast = 100;
        public const int PurseMost = 600;

        private readonly Bag _bag;
        private readonly IWorldState _state;
        private readonly IWorldClock _clock;
        private readonly ICharacterValues _character;
        private readonly SocialStanding _standing;
        private readonly IWorldSeed _seed;

        public SliceTheft(Bag bag, IWorldState state, IWorldClock clock, ICharacterValues character, SocialStanding standing, IWorldSeed seed)
        {
            _bag = bag;
            _state = state;
            _clock = clock;
            _character = character;
            _standing = standing;
            _seed = seed;
        }

        /// <summary>World state: the day, plus one, that the person was last tried; 0 for never.</summary>
        public static string TriedKey(string person) => $"theft.{person}.tried";

        public static string LiftedKey => "player.theft.lifted";

        public bool TriedToday(string person) => _state.Get(TriedKey(person)) == _clock.Day + 1;

        /// <summary>The good zone's width against a target of <paramref name="perception"/>, Alert or not.</summary>
        public double Width(int perception, bool alert) => ReactionCheck.Width(Delta(perception, alert));

        public double Delta(int perception, bool alert) =>
            ReactionCheck.TheftDelta(_character.Get(CharacterAttribute.Dexterity), _character.Get(CharacterSkill.Theft), perception, alert);

        /// <summary>The bar for a try at <paramref name="person"/> today, its zone and pace by 07.1 §8a, placed by the seed.</summary>
        public ReactionBar Bar(string person, int perception, bool alert) =>
            ReactionCheck.For(Delta(perception, alert), _seed.Fork(SliceSeedStreams.Theft, Hash(person), _clock.Day * 2));

        /// <summary>The person's purse today, by the seed.</summary>
        public int Purse(string person) => _seed.Fork(SliceSeedStreams.Theft, Hash(person), (_clock.Day * 2) + 1).Next(PurseLeast, PurseMost + 1);

        /// <summary>
        /// Settles a try by the Reaction Check's result; <paramref name="bronze"/> is what was taken. Every try grows Theft
        /// and Dexterity, a noticed one half, the target's <paramref name="perception"/> being the difficulty read on §1's
        /// bands (07.1 §3, S5-1).
        /// </summary>
        public TheftOutcome Resolve(string person, ReactionResult result, out int bronze, int perception = 1, bool alert = false)
        {
            _state.Set(TriedKey(person), _clock.Day + 1);
            var difficulty = (int)Math.Round(perception * (alert ? ReactionCheck.AlertPerception : 1d));
            _character.Train(CharacterSkill.Theft, Growth.StageOfDifficulty(difficulty), Growth.Of(result), new[] { CharacterAttribute.Dexterity });
            if (result == ReactionResult.Bad)
            {
                bronze = 0;
                _standing?.Record(Deed.TheftNoticed);
                return TheftOutcome.Noticed;
            }

            bronze = Purse(person);
            _bag.Earn(bronze);
            _state.Set(LiftedKey, _state.Get(LiftedKey) + 1);
            return TheftOutcome.Lifted;
        }

        /// <summary>FNV-1a of the person's name: the same on every machine and run.</summary>
        private static int Hash(string id)
        {
            unchecked
            {
                var h = 2166136261u;
                foreach (var c in id)
                {
                    h = (h ^ c) * 16777619u;
                }

                return (int)(h & 0x7fffffff);
            }
        }
    }
}
