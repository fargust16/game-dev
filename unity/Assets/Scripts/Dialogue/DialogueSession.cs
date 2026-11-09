using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Player.Progression;
using GameDev.Player.Standing;
using GameDev.World.Poi;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;

namespace GameDev.Dialogue
{
    /// <summary>What a conversation reads and writes in the world. One context serves every conversation of a run.</summary>
    public sealed class DialogueContext
    {
        public IWorldState State { get; set; }
        public IWorldClock Clock { get; set; }
        public IWorldSeed Seed { get; set; }

        /// <summary>The seed stream Social Checks roll from, forked by the option and the tick.</summary>
        public string CheckStream { get; set; }

        public SocialCheckRules Rules { get; set; }
        public SocialStanding Standing { get; set; }

        /// <summary>The character who speaks.</summary>
        public ICharacterValues Character { get; set; }

        /// <summary>Settles the POIs outcomes name; may be null where none is named.</summary>
        public PoiDirector Director { get; set; }

        /// <summary>Approaches a later milestone builds: their options stay hidden until it comes.</summary>
        public ISet<Approach> Withheld { get; set; } = new HashSet<Approach>();

        /// <summary>Whether the character holds an item; null hides every option that shows one.</summary>
        public Func<string, bool> Holds { get; set; }
    }

    /// <summary>
    /// One conversation (10.4; S2.3). It pauses the world while open and costs one tick when it ends, whatever ends it.
    /// An option appears only with one of its Arguments; a failed check locks its option for good, in the world state;
    /// a failed check on an enemy is Final and ends the conversation in a fight. Deception and Intimidation pay their
    /// costs through the character's standing (10.4 §5).
    /// </summary>
    public sealed class DialogueSession : IDialogueRunner
    {
        private readonly DialogueContext _world;
        private IDisposable _pause;

        public DialogueSession(DialogueDefinition dialogue, DialogueContext world)
        {
            Dialogue = dialogue != null ? dialogue : throw new ArgumentNullException(nameof(dialogue));
            _world = world ?? throw new ArgumentNullException(nameof(world));
            if (world.State == null || world.Clock == null || world.Seed == null || world.Rules == null
                || world.Standing == null || world.Character == null || string.IsNullOrWhiteSpace(world.CheckStream))
            {
                throw new ArgumentException("A conversation needs the world state, the clock, the seed and its stream, the rules, the standing and the character.", nameof(world));
            }

            Node = dialogue.Start ?? throw new ArgumentException($"Dialogue {dialogue.Id} has no start node.", nameof(dialogue));
            _pause = world.Clock.Pause();
        }

        public event Action<DialogueEnd> Ended;

        public DialogueDefinition Dialogue { get; }

        public bool IsOpen => _pause != null;

        public DialogueNode Node { get; private set; }

        public DialogueEnd? EndedHow { get; private set; }

        public DialogueListener FightWith { get; private set; }

        public IReadOnlyList<DialogueOption> Options => IsOpen ? Node.options.Where(Offered).ToList() : new List<DialogueOption>();

        /// <summary>The key under which a failed check stays locked.</summary>
        public static string LockKey(DialogueDefinition dialogue, DialogueOption option) => $"dialogue.{dialogue.Id}.{option.id}.failed";

        public CheckPreview Preview(DialogueOption option)
        {
            if (option.kind != OptionKind.Check)
            {
                throw new ArgumentException($"Option {option.id} is not a check.", nameof(option));
            }

            var rules = _world.Rules;
            var listener = ListenerOf(option);
            var characterValue = SocialCheck.CharacterValue(rules, _world.Character, option.skill);
            var listenerValue = SocialCheck.ListenerValue(rules, listener.Sheet(), option.skill);
            var difficulty = SocialCheck.Difficulty(rules, option.request, listenerValue, listener.enemy);
            var attitude = _world.Standing.Attitude(listener.id);
            var standing = _world.Standing.Reading(option.skill, listener.enemy);
            var arguments = Math.Min(2, Math.Max(0, option.arguments.Count(_world.State.IsSet) - 1));
            var modifiers = SocialCheck.Modifiers(rules, attitude + standing.Modifier + arguments);
            var luck = _world.Character.Get(CharacterAttribute.Luck);
            var chance = SocialCheck.Chance(rules, characterValue, modifiers, difficulty, luck);
            return new CheckPreview(chance, characterValue, listenerValue, difficulty, attitude, standing, arguments, modifiers);
        }

        public CheckResult Choose(DialogueOption option)
        {
            if (!IsOpen || !Node.options.Contains(option) || !Offered(option))
            {
                throw new InvalidOperationException($"Option {option?.id} is not offered now.");
            }

            if (option.kind != OptionKind.Check)
            {
                Follow(option.success);
                return CheckResult.None;
            }

            var listener = ListenerOf(option);
            var chance = Preview(option).Chance;
            var rolls = _world.Seed.Fork(_world.CheckStream, StableHash.Of(Dialogue.Id + "." + option.id), (int)_world.Clock.Tick);
            var success = SocialCheck.Roll(chance, rolls);
            Pay(option, listener, success);
            if (success)
            {
                Follow(option.success);
                return CheckResult.Success;
            }

            _world.State.Raise(LockKey(Dialogue, option));
            if (listener.enemy)
            {
                Apply(option.failure);
                FightWith = listener;
                End(DialogueEnd.Fight);
            }
            else
            {
                Follow(option.failure);
            }

            return CheckResult.Failure;
        }

        public void Leave()
        {
            if (IsOpen)
            {
                End(DialogueEnd.Left);
            }
        }

        private bool Offered(DialogueOption option)
        {
            var state = _world.State;
            if (_world.Withheld.Contains(option.approach)
                || state.IsSet(LockKey(Dialogue, option))
                || option.hiddenBy.Any(state.IsSet)
                || !option.needs.All(state.IsSet))
            {
                return false;
            }

            if (option.kind == OptionKind.Check && option.arguments.Length == 0)
            {
                return false;
            }

            if (option.arguments.Length > 0 && !option.arguments.Any(state.IsSet))
            {
                return false;
            }

            return option.kind != OptionKind.ShowItem || (_world.Holds != null && _world.Holds(option.item));
        }

        private DialogueListener ListenerOf(DialogueOption option)
        {
            var id = string.IsNullOrEmpty(option.listener) ? Node.speaker : option.listener;
            return Dialogue.Listener(id) ?? throw new InvalidOperationException($"Option {option.id} addresses no listener.");
        }

        /// <summary>10.4 §5: a lie that worked may come out later, one that failed costs Reputation; a threat costs Attitude either way.</summary>
        private void Pay(DialogueOption option, DialogueListener listener, bool success)
        {
            var standing = _world.Standing;
            switch (option.skill)
            {
                case CharacterSkill.Deception when success:
                    standing.LieTold(listener.id, option.request);
                    break;
                case CharacterSkill.Deception:
                    standing.LieFailed();
                    break;
                case CharacterSkill.Intimidation:
                    standing.Intimidated(listener.id);
                    break;
            }
        }

        private void Follow(DialogueOutcome outcome)
        {
            Apply(outcome);
            if (string.IsNullOrEmpty(outcome.next))
            {
                End(DialogueEnd.Finished);
                return;
            }

            Node = Dialogue.Node(outcome.next) ?? throw new InvalidOperationException($"Dialogue {Dialogue.Id} has no node {outcome.next}.");
        }

        private void Apply(DialogueOutcome outcome)
        {
            foreach (var key in outcome.raises)
            {
                _world.State.Raise(key);
            }

            if (outcome.settles != null && _world.Director != null && _world.Director.Phase(outcome.settles) == PoiPhase.Open)
            {
                _world.Director.ResolveParticipated(outcome.settles);
            }
        }

        private void End(DialogueEnd how)
        {
            _pause.Dispose();
            _pause = null;
            Node = null;
            EndedHow = how;
            _world.Clock.AdvanceTicks(1);
            Ended?.Invoke(how);
        }
    }
}
