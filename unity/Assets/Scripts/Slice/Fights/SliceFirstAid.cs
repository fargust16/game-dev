using System;
using GameDev.Confront.Rules;
using GameDev.Confront.Turns;
using GameDev.Items;
using GameDev.Player.Progression;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// First Aid with what the character carries (S5a.7; 10.2 *First Aid*): the bandages, antidotes and splints are
    /// items in the bag, bought from the Master of First Aid; an antidote and a splint need the Master's lesson. In a fight
    /// a treatment costs its AP, outside one its ticks, the world running through them; then a check on Dexterity and
    /// First Aid. A success ends the condition and uses the item, a failure keeps it.
    /// </summary>
    public sealed class SliceFirstAid
    {
        /// <summary>The supplies' item ids, by <see cref="Treatment"/>.</summary>
        public static readonly string[] SupplyItems = { "bandage", "antidote", "splint" };

        /// <summary>Set by the Master of First Aid's lesson (S5a.3); an antidote and a splint need it (10.2).</summary>
        public const string LessonKey = "player.lesson.firstaid";

        // Authoring default, S3a.7: the order First Aid takes: the leg, which holds the character in place, then
        // Bleeding, which takes HP, then Poison.
        private static readonly Treatment[] Urgency = { Treatment.Splint, Treatment.Bandage, Treatment.Antidote };

        private readonly FightRules _rules;
        private readonly Bag _bag;
        private readonly IWorldState _state;
        private readonly IWorldClock _clock;
        private readonly ICharacterValues _character;
        private readonly IWorldSeed _seed;

        public SliceFirstAid(FightRules rules, Bag bag, IWorldState state, IWorldClock clock, ICharacterValues character, IWorldSeed seed)
        {
            _rules = rules;
            _bag = bag;
            _state = state;
            _clock = clock;
            _character = character;
            _seed = seed;
        }

        public bool Knows => _state != null && _state.IsSet(LessonKey);

        /// <summary>How many of a treatment's supply the bag holds.</summary>
        public int Count(Treatment treatment) => _bag != null ? _bag.Count(SupplyItems[(int)treatment]) : 0;

        /// <summary>The most urgent treatment the fighter can be given with <paramref name="supplies"/>, or null.</summary>
        public Treatment? Choose(Fighter fighter, Func<Treatment, int> supplies, bool knows)
        {
            foreach (var treatment in Urgency)
            {
                if (FightRules.Needs(fighter, treatment) && supplies(treatment) > 0 && (knows || !_rules.TreatmentNeedsLesson(treatment)))
                {
                    return treatment;
                }
            }

            return null;
        }

        /// <summary>The development fight's kit (S3a.4): the bag holds at least <paramref name="least"/> of the supply.</summary>
        public void TopUp(Treatment treatment, int least)
        {
            var missing = least - Count(treatment);
            if (missing > 0 && _bag != null)
            {
                _bag.Add(SupplyItems[(int)treatment], missing);
            }
        }

        /// <summary>A fight's player takes the lesson and what the bag holds into the fight.</summary>
        public void Load(Combatant player)
        {
            player.KnowsFirstAid = Knows;
            for (var i = 0; i < SupplyItems.Length; i++)
            {
                player.Supplies[i] = Count((Treatment)i);
            }
        }

        /// <summary>After a fight, what it used leaves the bag.</summary>
        public void Spend(Combatant player)
        {
            for (var i = 0; i < SupplyItems.Length && _bag != null; i++)
            {
                var used = Count((Treatment)i) - player.Supplies[i];
                if (used > 0)
                {
                    _bag.Remove(SupplyItems[i], used);
                }
            }
        }

        /// <summary>
        /// First Aid outside a fight on the character itself, its most urgent condition first: the treatment's ticks pass,
        /// the world running through them, and then its check. <paramref name="load"/> reads the character's life as it
        /// stands and <paramref name="save"/> keeps it. Returns the key of why nothing was done, or null.
        /// </summary>
        public string TreatOutside(Func<Fighter> load, Action<Fighter> save, out Treatment treatment, out bool success)
        {
            treatment = default;
            success = false;
            if (!(Choose(load(), Count, Knows) is Treatment chosen))
            {
                return "ui.life.nosupply";
            }

            treatment = chosen;
            _clock.AdvanceTicks(_rules.TreatmentTicks(chosen));
            var fighter = load();
            var rolls = _seed.Fork(SliceSeedStreams.ActionChecks, (int)_clock.Tick, 7000 + (int)chosen);
            success = rolls.NextDouble() < _rules.TreatmentChance(_character, chosen);
            if (success)
            {
                FightRules.Cure(fighter, chosen);
                save(fighter);
                _bag.Remove(SupplyItems[(int)chosen]);
            }

            return null;
        }
    }
}
