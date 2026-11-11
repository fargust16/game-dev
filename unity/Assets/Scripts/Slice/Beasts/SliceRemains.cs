using System;
using System.Collections.Generic;
using GameDev.Items;
using GameDev.Player.Progression;
using GameDev.Slice.Shops;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;

namespace GameDev.Slice.Beasts
{
    /// <summary>How a skinning ended.</summary>
    public enum SkinOutcome
    {
        /// <summary>It never began: the reason is the fault's key.</summary>
        Refused,

        /// <summary>A fight began in its ticks; the hide is still on the carcass.</summary>
        Interrupted,

        /// <summary>Its ticks passed and the check was rolled; the hide is off, whole or spoiled by half.</summary>
        Done,
    }

    /// <summary>
    /// What a fallen beast leaves (S5a.6; 19 §6.4, 11 C3): its ordinary ingredients, and a glowing beast's magical one,
    /// taken by anyone with no lesson; its hide only by Skinning, once its lesson is learned from the Master of Item
    /// Creation, in 3 ticks with the world running, a check on (Dexterity + Skinning) / 2 against D 2. A success gives the
    /// beast's whole leather, a failure half of it, rounded down. What was taken lives in the world state by the beast's id.
    /// </summary>
    public sealed class SliceRemains
    {
        /// <summary>The lesson's id, as the Master's page and the world flag name it.</summary>
        public const string Lesson = "skinning";

        private readonly BeastRules _rules;
        private readonly Bag _bag;
        private readonly IWorldState _state;
        private readonly IWorldClock _clock;
        private readonly ICharacterValues _character;
        private readonly ActionCheckRules _checks;
        private readonly IWorldSeed _seed;

        public SliceRemains(BeastRules rules, Bag bag, IWorldState state, IWorldClock clock, ICharacterValues character,
            ActionCheckRules checks, IWorldSeed seed)
        {
            _rules = rules;
            _bag = bag;
            _state = state;
            _clock = clock;
            _character = character;
            _checks = checks;
            _seed = seed;
        }

        public BeastRules Rules => _rules;

        public static string PartsKey(string beast) => $"beast.{beast}.parts";

        public static string SkinnedKey(string beast) => $"beast.{beast}.skinned";

        public bool KnowsSkinning => _state.IsSet(SliceShops.LessonKey(Lesson));

        public bool PartsTaken(string beast) => _state.IsSet(PartsKey(beast));

        public bool Skinned(string beast) => _state.IsSet(SkinnedKey(beast));

        /// <summary>The ingredients the beast leaves to anyone: its ordinary ones, and a glowing beast's magical one.</summary>
        public IReadOnlyList<(string item, int count)> PartsOf(BeastKind kind, bool glowing)
        {
            var v = _rules.Of(kind, glowing);
            var parts = new List<(string, int)>(2);
            if (v.ingredients > 0 && !string.IsNullOrEmpty(v.ingredient))
            {
                parts.Add((v.ingredient, v.ingredients));
            }

            if (v.magical > 0 && !string.IsNullOrEmpty(_rules.MagicalItem))
            {
                parts.Add((_rules.MagicalItem, v.magical));
            }

            return parts;
        }

        /// <summary>The leather a skinning gives: the beast's whole on a success or a critical, half rounded down on a failure.</summary>
        public int LeatherOf(BeastKind kind, bool glowing, CheckOutcome outcome)
        {
            var whole = _rules.Of(kind, glowing).leather;
            return outcome == CheckOutcome.Failure ? whole / 2 : whole;
        }

        /// <summary>Takes the beast's ingredients into the bag, all or none; the key of why not, or null.</summary>
        public string TakeParts(string beast, BeastKind kind, bool glowing)
        {
            if (PartsTaken(beast))
            {
                return "ui.remains.fault.taken";
            }

            var parts = PartsOf(kind, glowing);
            var kg = 0f;
            foreach (var (item, count) in parts)
            {
                kg += WeightOf(item) * count;
            }

            if (!_bag.CanCarry(kg))
            {
                return "ui.remains.fault.heavy";
            }

            foreach (var (item, count) in parts)
            {
                _bag.Add(item, count);
            }

            _state.Set(PartsKey(beast), 1);
            return null;
        }

        /// <summary>The key of why the beast cannot be skinned now, or null.</summary>
        public string SkinFault(string beast, BeastKind kind, bool glowing)
        {
            if (Skinned(beast))
            {
                return "ui.remains.fault.skinned";
            }

            if (!KnowsSkinning)
            {
                return "ui.remains.fault.lesson";
            }

            var whole = _rules.Of(kind, glowing).leather;
            return _bag.CanCarry(WeightOf(_rules.HideItem) * whole) ? null : "ui.remains.fault.heavy";
        }

        public ActionCheckChances Chances()
        {
            var preparation = ActionCheck.Preparation(_character.Get(CharacterAttribute.Dexterity), _character.Get(CharacterSkill.Skinning));
            return ActionCheck.Chances(_checks, preparation, _rules.SkinningDifficulty, _character.Get(CharacterAttribute.Perception),
                _character.Get(CharacterAttribute.Luck));
        }

        /// <summary>
        /// Skins the beast through its ticks, the world running through each. <paramref name="interrupted"/> is asked
        /// after each tick; when it answers true the work stops and the hide stays on. Otherwise the check is rolled from
        /// the seed by the beast, the leather goes into the bag, and the hide is off for good.
        /// </summary>
        public SkinOutcome Skin(string beast, BeastKind kind, bool glowing, Func<bool> interrupted, out int leather, out string fault)
        {
            leather = 0;
            fault = SkinFault(beast, kind, glowing);
            if (fault != null)
            {
                return SkinOutcome.Refused;
            }

            for (var t = 0; t < _rules.SkinningTicks; t++)
            {
                _clock.AdvanceTicks(1);
                if (interrupted != null && interrupted())
                {
                    return SkinOutcome.Interrupted;
                }
            }

            var outcome = ActionCheck.Roll(Chances(), _seed.Fork(SliceSeedStreams.Skinning, Hash(beast), 0));
            leather = LeatherOf(kind, glowing, outcome);
            if (leather > 0)
            {
                _bag.Add(_rules.HideItem, leather);
            }

            _state.Set(SkinnedKey(beast), 1);
            return SkinOutcome.Done;
        }

        private float WeightOf(string item) => _bag.Catalogue != null ? _bag.Catalogue.Find(item)?.weight ?? 0f : 0f;

        /// <summary>FNV-1a of the beast's id: the same on every machine and run.</summary>
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
