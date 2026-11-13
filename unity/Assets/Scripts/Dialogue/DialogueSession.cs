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

        /// <summary>What the sources say and the player has heard (09); null hides every option that asks for a tip.</summary>
        public TipBook Tips { get; set; }

        /// <summary>A Social Check was rolled: the option, its listener's id, and whether it succeeded (05.1 Conduct Progress, S5b.7).</summary>
        public Action<DialogueOption, string, bool> Checked { get; set; }

        /// <summary>Points a troubled mind and Fatigue put on every Social Check (07.2 §3.3–3.4, S5b.6); null for none.</summary>
        public Func<int> MindPenalty { get; set; }

        /// <summary>Points an ability gives the next check of a skill, inside the cap, as Внушение (05.1, S5b.8); null for none.</summary>
        public Func<CharacterSkill, int> SkillBonus { get; set; }
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

        /// <param name="startNode">S4.3: the node it opens on, for a conversation with more than one way in; null for its start.</param>
        public DialogueSession(DialogueDefinition dialogue, DialogueContext world, string startNode = null)
        {
            Dialogue = dialogue != null ? dialogue : throw new ArgumentNullException(nameof(dialogue));
            _world = world ?? throw new ArgumentNullException(nameof(world));
            if (world.State == null || world.Clock == null || world.Seed == null || world.Rules == null
                || world.Standing == null || world.Character == null || string.IsNullOrWhiteSpace(world.CheckStream))
            {
                throw new ArgumentException("A conversation needs the world state, the clock, the seed and its stream, the rules, the standing and the character.", nameof(world));
            }

            _pause = world.Clock.Pause();
            var first = string.IsNullOrEmpty(startNode) ? dialogue.Start : dialogue.Node(startNode);
            Arrive(first ?? throw new ArgumentException($"Dialogue {dialogue.Id} has no node '{startNode ?? "start"}'.", nameof(dialogue)), new Tip[0]);
        }

        public event Action<DialogueEnd> Ended;

        public DialogueDefinition Dialogue { get; }

        public bool IsOpen => _pause != null;

        public DialogueNode Node { get; private set; }

        public IReadOnlyList<Tip> Said { get; private set; } = new Tip[0];

        public DialogueEnd? EndedHow { get; private set; }

        public DialogueListener FightWith { get; private set; }

        public DialogueOption Fought { get; private set; }

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
            var mind = _world.MindPenalty != null ? _world.MindPenalty() : 0;
            var bonus = _world.SkillBonus != null ? _world.SkillBonus(option.skill) : 0;
            var modifiers = SocialCheck.Modifiers(rules, attitude + standing.Modifier + arguments + mind + bonus);
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
                if (option.success.fights)
                {
                    Fight(option, option.success);
                }
                else
                {
                    Follow(option.success);
                }

                return CheckResult.None;
            }

            var listener = ListenerOf(option);
            var chance = Preview(option).Chance;
            var rolls = _world.Seed.Fork(_world.CheckStream, StableHash.Of(Dialogue.Id + "." + option.id), (int)_world.Clock.Tick);
            var success = SocialCheck.Roll(chance, rolls);
            SocialCheck.Train(_world.Character, option.skill, listener.Sheet(), success);
            Pay(option, listener, success);
            _world.Checked?.Invoke(option, listener.id, success);
            if (success)
            {
                Follow(option.success);
                return CheckResult.Success;
            }

            _world.State.Raise(LockKey(Dialogue, option));
            if (listener.enemy)
            {
                Fight(option, option.failure);
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

        public void AfterFight(bool won)
        {
            var option = Fought;
            if (option == null || IsOpen)
            {
                return;
            }

            Fought = null;
            Apply(won ? option.won : option.lost);
        }

        /// <summary>10.2, 10.4 §6: the conversation ends in a fight with the option's listener; the fight decides the rest.</summary>
        private void Fight(DialogueOption option, DialogueOutcome first)
        {
            Apply(first);
            Fought = option;
            FightWith = ListenerOf(option);
            End(DialogueEnd.Fight);
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

            if (!string.IsNullOrEmpty(option.asks) && (_world.Tips == null || !_world.Tips.CanTell(option.asks, _world.Clock.Tick)))
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
            var told = Apply(outcome);
            if (string.IsNullOrEmpty(outcome.next))
            {
                End(DialogueEnd.Finished);
                return;
            }

            Arrive(Dialogue.Node(outcome.next) ?? throw new InvalidOperationException($"Dialogue {Dialogue.Id} has no node {outcome.next}."), told);
        }

        /// <summary>The node shown now, and the tips said on arriving: those the outcome told, then its source's.</summary>
        private void Arrive(DialogueNode node, IReadOnlyList<Tip> told)
        {
            Node = node;
            var said = told.ToList();
            if (node.saysTips && _world.Tips != null)
            {
                said.AddRange(_world.Tips.Read(node.source, _world.Clock.Tick).Where(t => !said.Contains(t)));
            }

            Said = said;
        }

        /// <summary>Raises its keys, settles its POI, tells its tips, then lets its minutes pass; returns the tips told.</summary>
        private IReadOnlyList<Tip> Apply(DialogueOutcome outcome)
        {
            foreach (var key in outcome.raises)
            {
                _world.State.Raise(key);
            }

            if (outcome.settles != null && _world.Director != null && _world.Director.Phase(outcome.settles) == PoiPhase.Open)
            {
                _world.Director.ResolveParticipated(outcome.settles);
            }

            var told = new List<Tip>();
            foreach (var id in outcome.tells)
            {
                var tip = _world.Tips?.Tell(id, _world.Clock.Tick);
                if (tip != null)
                {
                    told.Add(tip);
                }
            }

            if (outcome.minutes > 0)
            {
                _world.Clock.AdvanceTicks(outcome.minutes);
            }

            return told;
        }

        private void End(DialogueEnd how)
        {
            _pause.Dispose();
            _pause = null;
            Node = null;
            Said = new Tip[0];
            EndedHow = how;
            _world.Clock.AdvanceTicks(1);
            Ended?.Invoke(how);
        }
    }
}
