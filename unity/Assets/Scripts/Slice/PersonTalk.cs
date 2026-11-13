using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Dialogue;
using GameDev.Input;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Player.Standing;
using GameDev.UI;
using GameDev.World.Portals;
using UnityEngine;

namespace GameDev.Slice
{
    /// <summary>
    /// A named person the player talks to (10.4; S2.4). Interact in reach opens the person's conversation in the HUD's
    /// dialogue column at the right edge: what was said stays above, and the options are numbered below. The number keys
    /// answer; Escape, or walking away, leaves. A Social Check shows its skill, its chance from Intelligence 10 and the
    /// modifiers behind it from 20 (10.4 §3). Every word is a string table key, and an open column follows a change of
    /// language.
    /// </summary>
    public sealed class PersonTalk : MonoBehaviour, IPromptSource
    {
        /// <summary>A conversation numbers up to nine options, one for each number key.</summary>
        public const int MostOptions = 9;

        private static readonly int TalkingHash = Animator.StringToHash("Talking");

        [SerializeField] private DialogueDefinition dialogue;

        [Tooltip("Authoring default, m: horizontal reach from the person.")]
        [SerializeField] private float interactRadius = 2f;

        [Tooltip("S4.3: the node Interact opens on; empty for the conversation's start.")]
        [SerializeField] private string startNode = "";

        [Tooltip("S4.3: whether Interact in reach opens it; off for a conversation another component opens, such as POI C's nook.")]
        [SerializeField] private bool opensItself = true;

        [Tooltip("The prompt's key while it can be opened.")]
        [SerializeField] private string promptKey = "ui.prompt.talk";

        // Each entry gives its speaker and words afresh, so the column follows a change of language.
        private readonly List<Func<(string speaker, string text)>> _history = new List<Func<(string, string)>>();
        private PlayerLocomotion _player;
        private HudView _hud;
        private IDialogueRunner _talk;
        private Portal[] _doors;

        public DialogueDefinition Dialogue => dialogue;

        /// <summary>The running conversation, or the last one once it has ended; null before the first.</summary>
        public IDialogueRunner Conversation => _talk;

        public bool IsOpen => _talk != null && _talk.IsOpen;

        /// <summary>Raised when a conversation opened here ends, after the dialogue column closes (S3b.3: a POI's fight).</summary>
        public event Action<PersonTalk, DialogueEnd> Ended;

        /// <summary>Whether the conversation can be opened now; a POI's scene closes it while it is not there (S2.5). An open one runs on.</summary>
        public bool Available { get; set; } = true;

        /// <summary>Setup for scenes built by code; a placed person is set in the Inspector.</summary>
        public void Configure(DialogueDefinition definition, float radius)
        {
            dialogue = definition;
            interactRadius = radius;
        }

        /// <summary>Setup for scenes built by code (S4.3): the node it opens on, whether Interact opens it, and its prompt.</summary>
        public void Configure(DialogueDefinition definition, float radius, string start, bool opens, string prompt = "ui.prompt.talk")
        {
            Configure(definition, radius);
            startNode = start ?? "";
            opensItself = opens;
            promptKey = string.IsNullOrEmpty(prompt) ? "ui.prompt.talk" : prompt;
        }

        public string StartNode => startNode;

        public bool OpensItself => opensItself;

        public bool InReach(Vector3 position) => InteractReach.Within(transform, position, interactRadius);

        public string PromptFor(Vector3 player) => opensItself && Available && dialogue != null && !IsOpen && InReach(player) && !Asleep ? Loc.Get(promptKey) : null;

        /// <summary>05.1 Дрёма (S5b.8): one asleep is not talked to.</summary>
        private bool Asleep => GameDev.Slice.Fights.HeldStill.Sleeping(transform);

        /// <summary>
        /// 05.1 Чуткое ухо (S5b.8): a line in which the speaker lies is marked for a character who knows it, which tells
        /// that it lies, never in what.
        /// </summary>
        public static string Heard(DialogueDefinition dialogue, DialogueNode node)
        {
            var line = dialogue.Line(node);
            var fights = SliceWorldDriver.Running != null ? SliceWorldDriver.Running.Fights : null;
            return node.lies && fights != null && fights.Knows("keen_ear") ? line + "  " + Loc.Get("ui.ability.keen_ear.lie") : line;
        }

        /// <summary>What Interact does: opens the conversation on its first node.</summary>
        public void Open() => Open(null);

        /// <summary>Opens the conversation on <paramref name="node"/>, or on its own first node when null (S4.3).</summary>
        public void Open(string node)
        {
            var world = SliceWorldDriver.Running;
            if (IsOpen || !Available || world == null || dialogue == null || Hud() == null || Asleep)
            {
                return;
            }

            _history.Clear();
            var first = string.IsNullOrEmpty(node) ? startNode : node;
            _talk = world.Talk(dialogue, string.IsNullOrEmpty(first) ? null : first);
            _talk.Ended += how =>
            {
                Hud()?.CloseDialogue();
                Talking(false);
                Ended?.Invoke(this, how);
            };
            Talking(true);
            Hear();
            Show();
        }

        /// <summary>Answers with the option numbered <paramref name="index"/> + 1 among those offered now.</summary>
        public void Choose(int index)
        {
            var options = IsOpen ? _talk.Options : new List<DialogueOption>();
            if (index < 0 || index >= Math.Min(options.Count, MostOptions))
            {
                return;
            }

            var option = options[index];
            var node = _talk.Node;
            _history.Add(() => (Loc.Get("ui.dialogue.you"), dialogue.Text(node, option)));
            var result = _talk.Choose(option);
            if (result != CheckResult.None)
            {
                var key = result == CheckResult.Success ? "ui.dialogue.success" : "ui.dialogue.failure";
                _history.Add(() => (null, Loc.Format(key, SkillName(option.skill))));
            }

            if (IsOpen)
            {
                Hear();
                Show();
            }
        }

        public void Leave()
        {
            if (IsOpen)
            {
                _talk.Leave();
            }
        }

        /// <summary>
        /// A check's tag before its words: the skill alone below Intelligence 10, its chance from 10, and the modifiers
        /// behind the chance from 20 (10.4 §3).
        /// </summary>
        public static string CheckTag(SocialCheckRules rules, CharacterSkill skill, CheckPreview preview, int intelligence)
        {
            var name = SkillName(skill);
            if (intelligence < rules.ChanceFromIntelligence)
            {
                return Loc.Format("ui.dialogue.check", name);
            }

            var percent = Math.Round(preview.Chance * 100d, MidpointRounding.AwayFromZero);
            var tag = Loc.Format("ui.dialogue.check.chance", name, percent);
            if (intelligence < rules.ModifiersFromIntelligence)
            {
                return tag;
            }

            var standing = preview.Standing.Source == StandingSource.Danger ? "ui.dialogue.danger" : "ui.dialogue.reputation";
            return tag + " " + Loc.Format("ui.dialogue.check.modifiers", Signed(preview.Attitude), Loc.Get(standing), Signed(preview.Standing.Modifier), Signed(preview.Arguments));
        }

        public static string SkillName(CharacterSkill skill) => Loc.Get("ui.skill." + skill.ToString().ToLowerInvariant());

        private static string Signed(int value)
        {
            if (value > 0)
            {
                return "+" + value.ToString(CultureInfo.InvariantCulture);
            }

            return value < 0 ? "−" + (-value).ToString(CultureInfo.InvariantCulture) : "0";
        }

        // The tips said on arriving take the place of the node's line; the line is said when there are none.
        private void Hear()
        {
            var node = _talk.Node;
            var speaker = dialogue.Listener(node.speaker)?.nameKey;
            Func<string> name = () => string.IsNullOrEmpty(speaker) ? null : Loc.Get(speaker);
            if (_talk.Said.Count == 0)
            {
                _history.Add(() => (name(), Heard(dialogue, node)));
                return;
            }

            foreach (var tip in _talk.Said)
            {
                _history.Add(() => (name(), tip.Line));
            }
        }

        private void Show()
        {
            if (!IsOpen)
            {
                return;
            }

            var world = SliceWorldDriver.Running;
            var intelligence = DebugSight.Read(world != null ? world.Character.Get(CharacterAttribute.Intelligence) : CharacterSheet.Lowest);
            var node = _talk.Node;
            var options = new List<string>();
            foreach (var option in _talk.Options.Take(MostOptions))
            {
                var words = dialogue.Text(node, option);
                if (option.kind == OptionKind.Check && world != null)
                {
                    words = CheckTag(world.CheckRules, option.skill, _talk.Preview(option), intelligence) + " " + words;
                }

                options.Add((options.Count + 1).ToString(CultureInfo.InvariantCulture) + ". " + words);
            }

            Hud()?.ShowDialogue(_history.Select(h => h()).ToList(), options);
        }

        private void Update()
        {
            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerLocomotion>();
                if (_player == null)
                {
                    return;
                }
            }

            var input = GameInputService.Instance;
            var inReach = InReach(_player.transform.position);
            if (IsOpen)
            {
                if (!inReach || (input != null && input.CancelPressed))
                {
                    Leave();
                    return;
                }

                for (var number = 1; input != null && number <= MostOptions; number++)
                {
                    if (input.OptionPressed(number))
                    {
                        Choose(number - 1);
                        return;
                    }
                }

                return;
            }

            // A door in reach takes Interact, as it takes the HUD's prompt: a scene before a shop's door (S2.5) is
            // not opened by going in.
            _doors ??= FindObjectsByType<Portal>(FindObjectsInactive.Exclude);
            if (opensItself && inReach && input != null && input.InteractPressed
                && !_doors.Any(d => d != null && d.isActiveAndEnabled && d.InReach(_player.transform.position)))
            {
                Open();
            }
        }

        private void OnEnable()
        {
            Loc.Changed += Show;
        }

        private void OnDisable()
        {
            Loc.Changed -= Show;
            Leave();
        }

        /// <summary>The person's body plays its talk while the conversation is open, where its controller has one (ANIMATION_PIPELINE_PLAN.md A1).</summary>
        private void Talking(bool on)
        {
            var animator = GetComponentInChildren<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null && animator.parameters.Any(p => p.nameHash == TalkingHash))
            {
                animator.SetBool(TalkingHash, on);
            }
        }

        private HudView Hud()
        {
            if (_hud == null)
            {
                _hud = FindAnyObjectByType<HudView>();
            }

            return _hud;
        }
    }
}
