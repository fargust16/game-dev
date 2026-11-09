using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Player.Progression;
using GameDev.World.Poi;
using UnityEngine;

namespace GameDev.Dialogue
{
    /// <summary>The Approaches of 06 an option may name; None for plain talk.</summary>
    public enum Approach
    {
        None,
        Convince,
        Threaten,
        Exchange,
        Veil,
        Confront,
        Withdraw,
    }

    /// <summary>What choosing an option does: talk on, make a Social Check (10.4 §2), or show an item.</summary>
    public enum OptionKind
    {
        Talk,
        Check,
        ShowItem,
    }

    /// <summary>
    /// Someone the character can address in a conversation: a named person by its id, or a group such as a POI's
    /// collectors. Attitude and lies are kept under its id (10.4 §4–5), and its three attributes give its Listener Value.
    /// </summary>
    [Serializable]
    public sealed class DialogueListener
    {
        [Tooltip("A named person's id, or a group's; Attitude and lies are kept under it.")]
        public string id;

        [Tooltip("Its name's key in the string tables, e.g. person.nell.name.")]
        public string nameKey;

        [Tooltip("10.4 §6: a check on an enemy is a Final Check, and one that can fight starts a fight when it fails.")]
        public bool enemy;

        [Range(CharacterSheet.Lowest, CharacterSheet.Highest)] public int strength = CharacterSheet.Lowest;
        [Range(CharacterSheet.Lowest, CharacterSheet.Highest)] public int intelligence = CharacterSheet.Lowest;
        [Range(CharacterSheet.Lowest, CharacterSheet.Highest)] public int will = CharacterSheet.Lowest;

        /// <summary>Its values as a sheet, for <see cref="SocialCheck.ListenerValue"/>.</summary>
        public CharacterSheet Sheet()
        {
            var sheet = new CharacterSheet();
            sheet.Set(CharacterAttribute.Strength, strength);
            sheet.Set(CharacterAttribute.Intelligence, intelligence);
            sheet.Set(CharacterAttribute.Will, will);
            return sheet;
        }
    }

    /// <summary>What follows an option: the world state keys it raises, the POI it settles, and where the talk goes.</summary>
    [Serializable]
    public sealed class DialogueOutcome
    {
        [Tooltip("The node the talk moves to; empty ends the conversation.")]
        public string next = "";

        [Tooltip("World state keys raised: residue, Arguments, Aftermath.")]
        public string[] raises = new string[0];

        [Tooltip("A POI this settles through ResolveParticipated, if its window is still open.")]
        public PoiDefinition settles;
    }

    /// <summary>One numbered option (10.4; S2.3). Its words are <c>dialogue.&lt;dialogue&gt;.&lt;node&gt;.&lt;option&gt;</c>.</summary>
    [Serializable]
    public sealed class DialogueOption
    {
        [Tooltip("Unique in its dialogue; a failed check is locked under it, so never change it after a save exists.")]
        public string id;

        [Tooltip("The Approach it is, or None for plain talk. Approaches a later milestone builds are withheld.")]
        public Approach approach;

        public OptionKind kind;

        [Tooltip("10.4 §1: world state keys the character gained in this life; any one opens the option, each further adds +1 to a check, at most +2. A check always needs one.")]
        public string[] arguments = new string[0];

        [Tooltip("World state keys that must all be set for it to appear.")]
        public string[] needs = new string[0];

        [Tooltip("World state keys any of which hides it, e.g. the key its success raises.")]
        public string[] hiddenBy = new string[0];

        [Tooltip("The item shown, for ShowItem.")]
        public string item = "";

        public CharacterSkill skill;

        public RequestSize request = RequestSize.Ordinary;

        [Tooltip("The listener a check addresses; empty for the node's speaker.")]
        public string listener = "";

        public DialogueOutcome success = new DialogueOutcome();

        [Tooltip("Only for a check that fails.")]
        public DialogueOutcome failure = new DialogueOutcome();
    }

    /// <summary>One step of a conversation: what its speaker says, <c>dialogue.&lt;dialogue&gt;.&lt;node&gt;</c>, and the options after it.</summary>
    [Serializable]
    public sealed class DialogueNode
    {
        public string id;

        [Tooltip("The listener who says the line, or empty for the scene itself.")]
        public string speaker = "";

        public DialogueOption[] options = new DialogueOption[0];
    }

    /// <summary>
    /// A conversation as data (17 A.3; 10.4; VERTICAL_SLICE_IMPLEMENTATION_PLAN S2.3): who can be addressed, its
    /// nodes, and the first. Immutable at run time; a run's state lives in the world state. Every word is a string
    /// table key derived from the ids (17 X.7).
    /// </summary>
    [CreateAssetMenu(menuName = "GameDev/Dialogue/Dialogue", fileName = "Dialogue")]
    public sealed class DialogueDefinition : ScriptableObject
    {
        [Tooltip("A short stable id; keys of words and of locked options use it.")]
        [SerializeField] private string id = "talk";

        [SerializeField] private DialogueListener[] listeners = new DialogueListener[0];

        [SerializeField] private DialogueNode[] nodes = new DialogueNode[0];

        [Tooltip("The node the conversation opens on; empty for the first.")]
        [SerializeField] private string start = "";

        public string Id => id;
        public IReadOnlyList<DialogueListener> Listeners => listeners;
        public IReadOnlyList<DialogueNode> Nodes => nodes;
        public DialogueNode Start => string.IsNullOrEmpty(start) ? nodes.FirstOrDefault() : Node(start);

        public DialogueNode Node(string nodeId) => nodes.FirstOrDefault(n => n.id == nodeId);

        public DialogueListener Listener(string listenerId) => listeners.FirstOrDefault(l => l.id == listenerId);

        public string LineKey(DialogueNode node) => $"dialogue.{id}.{node.id}";

        public string OptionKey(DialogueNode node, DialogueOption option) => $"dialogue.{id}.{node.id}.{option.id}";

        public string Line(DialogueNode node) => Loc.Get(LineKey(node));

        public string Text(DialogueNode node, DialogueOption option) => Loc.Get(OptionKey(node, option));

        /// <summary>Every string table key it shows.</summary>
        public IEnumerable<string> Keys()
        {
            foreach (var listener in listeners)
            {
                yield return listener.nameKey;
            }

            foreach (var node in nodes)
            {
                yield return LineKey(node);
                foreach (var option in node.options)
                {
                    yield return OptionKey(node, option);
                }
            }
        }

        /// <summary>What is wrong with it as data: ids, links, listeners, and checks without an Argument.</summary>
        public IReadOnlyList<string> Problems()
        {
            var problems = new List<string>();
            var nodeIds = new HashSet<string>();
            var optionIds = new HashSet<string>();
            foreach (var node in nodes)
            {
                if (string.IsNullOrWhiteSpace(node.id) || !nodeIds.Add(node.id))
                {
                    problems.Add($"{id}: node '{node.id}' has no id or a taken one");
                }

                if (!string.IsNullOrEmpty(node.speaker) && Listener(node.speaker) == null)
                {
                    problems.Add($"{id}.{node.id}: speaker '{node.speaker}' is not a listener");
                }

                foreach (var option in node.options)
                {
                    var name = $"{id}.{node.id}.{option.id}";
                    if (string.IsNullOrWhiteSpace(option.id) || !optionIds.Add(option.id))
                    {
                        problems.Add($"{name}: no id or a taken one");
                    }

                    foreach (var outcome in new[] { option.success, option.failure })
                    {
                        if (!string.IsNullOrEmpty(outcome.next) && Node(outcome.next) == null)
                        {
                            problems.Add($"{name}: no node '{outcome.next}'");
                        }
                    }

                    if (option.kind == OptionKind.Check)
                    {
                        if (option.arguments.Length == 0)
                        {
                            problems.Add($"{name}: a Social Check needs an Argument (10.4 §1)");
                        }

                        var listener = string.IsNullOrEmpty(option.listener) ? node.speaker : option.listener;
                        if (Listener(listener) == null)
                        {
                            problems.Add($"{name}: the check addresses no listener");
                        }
                    }

                    if (option.kind == OptionKind.ShowItem && string.IsNullOrWhiteSpace(option.item))
                    {
                        problems.Add($"{name}: shows no item");
                    }
                }
            }

            if (Start == null)
            {
                problems.Add($"{id}: no start node");
            }

            return problems;
        }

        /// <summary>Authoring and tests.</summary>
        public void Configure(string dialogueId, DialogueListener[] who, DialogueNode[] steps, string first = "")
        {
            id = dialogueId;
            listeners = who ?? new DialogueListener[0];
            nodes = steps ?? new DialogueNode[0];
            start = first ?? "";
        }
    }
}
