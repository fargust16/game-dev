using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Dialogue;
using GameDev.World.Poi;
using UnityEngine;

namespace GameDev.Decisions
{
    /// <summary>The five Decision Axes of 03.</summary>
    public enum DecisionAxis
    {
        Attention,
        Approach,
        Allegiance,
        Commitment,
        Bargain,
    }

    /// <summary>
    /// One option of a Decision Node's card (03.1 §5), in the shape of the old <c>DecisionOptionDef</c>: the axes it
    /// uses, the Approaches it may take, its cost and consequences, and the residue its success leaves. The options of
    /// its scene's conversation that take it are named by id, so the card and the conversation are checked together.
    /// </summary>
    [Serializable]
    public sealed class DecisionNodeOption
    {
        [Tooltip("The card's name for it, e.g. Defend Harrow. A design note, never shown to the player.")]
        public string name;

        public DecisionAxis[] axes = new DecisionAxis[0];

        [Tooltip("The Approaches it may take; one built later stays on the card and out of the conversation.")]
        public Approach[] approaches = new Approach[0];

        [Tooltip("The ids of the scene's conversation options that take it.")]
        public string[] dialogueOptions = new string[0];

        [Tooltip("Design notes from the card (03.1 §5), never shown to the player.")]
        [TextArea] public string cost;

        [TextArea] public string consequence;
        [TextArea] public string opens;
        [TextArea] public string closes;
        [TextArea] public string failure;

        [Tooltip("World state keys every one of its options raises on success: the POI's outcome and its residue (03.1 §6).")]
        public string[] residue = new string[0];

        [Tooltip("Walking away: it settles nothing, and the POI resolves unattended when its window closes (04).")]
        public bool countsAsWithdraw;

        [Tooltip("A step before the choice, such as buying a tip (03.1 *Exchange for a tip before moving*): it settles nothing and leaves the POI open.")]
        public bool prepares;

        [Tooltip("The milestone that builds it, e.g. S3, while no conversation option takes it yet.")]
        public string later = "";
    }

    /// <summary>
    /// A Decision Node as data (03.1; 17 Q.3; VERTICAL_SLICE_IMPLEMENTATION_PLAN S2.5): the card's axes and options, the POI
    /// that delivers it, and the conversation in which the player chooses. Immutable at run time; the conversation's
    /// outcomes write the POI's resolution and residue into the world state. Its texts are design notes; what the player
    /// reads is the conversation's, in the string tables (17 X.7).
    /// </summary>
    [CreateAssetMenu(menuName = "GameDev/Decisions/Decision Node", fileName = "DecisionNode")]
    public sealed class DecisionNodeDefinition : ScriptableObject
    {
        [Tooltip("A short stable id, e.g. poi-a.")]
        [SerializeField] private string id = "node";

        [Tooltip("The card's title. A design note.")]
        [SerializeField] private string title = "";

        [Tooltip("03.1 §1, a design note.")]
        [TextArea(3, 8)] [SerializeField] private string situation = "";

        [SerializeField] private PoiDefinition poi;

        [Tooltip("The conversation in which the player chooses.")]
        [SerializeField] private DialogueDefinition scene;

        [SerializeField] private DecisionAxis primaryAxis = DecisionAxis.Allegiance;
        [SerializeField] private DecisionAxis secondaryAxis = DecisionAxis.Approach;

        [SerializeField] private DecisionNodeOption[] options = new DecisionNodeOption[0];

        public string Id => id;
        public string Title => title;
        public string Situation => situation;
        public PoiDefinition Poi => poi;
        public DialogueDefinition Scene => scene;
        public DecisionAxis PrimaryAxis => primaryAxis;
        public DecisionAxis SecondaryAxis => secondaryAxis;
        public IReadOnlyList<DecisionNodeOption> Options => options;

        /// <summary>
        /// What is wrong with the card against 03.1 and against its conversation: two distinct axes, a way to walk away
        /// when Attention is active, and every option's conversation options present, of its Approaches, raising its
        /// residue and settling its POI unless it is the walk away.
        /// </summary>
        public IReadOnlyList<string> Problems()
        {
            var problems = new List<string>();
            if (primaryAxis == secondaryAxis)
            {
                problems.Add($"{id}: an important node activates two different axes (03.1 V1 Composition Rule)");
            }

            if (poi == null || scene == null)
            {
                problems.Add($"{id}: names no POI or no conversation");
                return problems;
            }

            if (options.Any(o => o.axes.Contains(DecisionAxis.Attention)) && !options.Any(o => o.countsAsWithdraw))
            {
                problems.Add($"{id}: Attention is active, so walking away is an option (03.1 *Options*)");
            }

            var spoken = scene.Nodes.SelectMany(n => n.options).ToList();
            foreach (var option in options)
            {
                if (option.dialogueOptions.Length == 0 && string.IsNullOrWhiteSpace(option.later))
                {
                    problems.Add($"{id}.{option.name}: no conversation option takes it, and no later milestone is named");
                }

                foreach (var optionId in option.dialogueOptions)
                {
                    var said = spoken.FirstOrDefault(o => o.id == optionId);
                    if (said == null)
                    {
                        problems.Add($"{id}.{option.name}: the conversation has no option '{optionId}'");
                        continue;
                    }

                    if (!option.approaches.Contains(said.approach))
                    {
                        problems.Add($"{id}.{option.name}: '{optionId}' is {said.approach}, not one of the card's Approaches");
                    }

                    foreach (var key in option.residue.Where(k => !said.success.raises.Contains(k)))
                    {
                        problems.Add($"{id}.{option.name}: '{optionId}' does not raise '{key}'");
                    }

                    var settles = said.success.settles == poi;
                    if (settles == (option.countsAsWithdraw || option.prepares))
                    {
                        problems.Add($"{id}.{option.name}: '{optionId}' {(settles ? "settles" : "does not settle")} POI {poi.Id}");
                    }
                }
            }

            return problems;
        }

        /// <summary>Authoring and tests.</summary>
        public void Configure(string nodeId, PoiDefinition delivered, DialogueDefinition conversation,
            DecisionAxis primary, DecisionAxis secondary, DecisionNodeOption[] choices)
        {
            id = nodeId;
            poi = delivered;
            scene = conversation;
            primaryAxis = primary;
            secondaryAxis = secondary;
            options = choices ?? new DecisionNodeOption[0];
        }
    }
}
