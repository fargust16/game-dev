using System;
using System.Collections.Generic;
using GameDev.Player.Progression;
using UnityEngine;

namespace GameDev.Slice.Gathering
{
    /// <summary>The four kinds of node of 19 §6.4.</summary>
    public enum GatherKind
    {
        Herb,
        Flax,
        Tree,
        Ore,
    }

    /// <summary>One kind of node as 19 §6.4's table gives it: its units, tool, check and return.</summary>
    [Serializable]
    public sealed class GatherNodeRule
    {
        public GatherKind kind;

        [Tooltip("Kit blocks whose name starts with one of these are nodes of this kind: the kit's name, or the one the city gave them.")]
        public string[] blocks = new string[0];

        [Tooltip("The item a unit is, by id.")]
        public string resource;

        public int units = 3;

        [Tooltip("The tool it needs in the bag, by item id; empty for hands.")]
        public string tool;

        public CharacterSkill skill;
        public CharacterAttribute attribute;
        public int difficulty = 1;

        [Tooltip("Game hours after it was emptied that it comes back; 0 for never within a run.")]
        public int returnsAfterHours;
    }

    /// <summary>
    /// Gathering's numbers (S5a.5): 19 §6.4's nodes, a cycle's Stamina, and 07.2 §3.2's Stamina, its maximum and its
    /// recovery. The reach and the step that cancels gathering are the agent's authoring defaults.
    /// </summary>
    [CreateAssetMenu(menuName = "GameDev/Slice/Gathering", fileName = "Gathering")]
    public sealed class GatherRules : ScriptableObject
    {
        [SerializeField] private GatherNodeRule[] nodes = new GatherNodeRule[0];

        [Tooltip("19 §6.4: a cycle's cost, as a share of maximum Stamina.")]
        [SerializeField] private float cycleStamina = 0.03f;

        [Tooltip("Authoring default, m: how near a node the character stands to gather it.")]
        [SerializeField] private float reach = 2.5f;

        [Tooltip("Authoring default, m: a step this long from where gathering began cancels it (19 §6.4 Moving).")]
        [SerializeField] private float step = 0.3f;

        [Tooltip("07.2 §3.2: Stamina_max = (base + perEndurance × Endurance) × S.")]
        [SerializeField] private float staminaBase = 20f;

        [SerializeField] private float staminaPerEndurance = 1.6f;

        [Tooltip("07.2 §3.2: the share of maximum Stamina recovered each world tick without exertion (20 %, the owner's change after the play test of S5a).")]
        [SerializeField] private float staminaRecovery = 0.2f;

        public IReadOnlyList<GatherNodeRule> Nodes => nodes;
        public float CycleStamina => cycleStamina;
        public float Reach => reach;
        public float Step => step;
        public float StaminaBase => staminaBase;
        public float StaminaPerEndurance => staminaPerEndurance;
        public float StaminaRecovery => staminaRecovery;

        /// <summary>The rule of the kit block named <paramref name="blockName"/>, or null for a block that is no node.</summary>
        public GatherNodeRule RuleFor(string blockName)
        {
            foreach (var node in nodes)
            {
                foreach (var prefix in node.blocks)
                {
                    if (!string.IsNullOrEmpty(prefix) && blockName.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        return node;
                    }
                }
            }

            return null;
        }

        public GatherNodeRule RuleFor(GatherKind kind)
        {
            foreach (var node in nodes)
            {
                if (node.kind == kind)
                {
                    return node;
                }
            }

            return null;
        }
    }
}
