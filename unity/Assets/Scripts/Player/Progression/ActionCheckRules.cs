using System.Collections.Generic;
using UnityEngine;

namespace GameDev.Player.Progression
{
    /// <summary>The numbers of the action check (07.1 §4–7). Changing one changes only this asset.</summary>
    [CreateAssetMenu(menuName = "GameDev/Progression/Action Check Rules", fileName = "ActionCheckRules")]
    public sealed class ActionCheckRules : ScriptableObject
    {
        [Tooltip("07.1 §4: the Δ of the curve's first entry, each next entry one step higher.")]
        [SerializeField] private int firstDelta = -8;

        [Tooltip("07.1 §4, owner-approved 2026-10-30: the base success chance at each whole Δ from firstDelta, 0 to 1. Linear between whole Δ, flat beyond both ends.")]
        [SerializeField] private float[] curve = { 0.01f, 0.02f, 0.05f, 0.10f, 0.20f, 0.35f, 0.50f, 0.65f, 0.80f, 0.90f, 0.95f, 0.98f, 0.99f };

        [Tooltip("07.1 §5: the most secondary attributes add, as a share of the base chance.")]
        [SerializeField, Range(0f, 1f)] private float secondaryBonus = 0.10f;

        [Tooltip("07.1 §6: no chance exceeds this after its modifiers, Luck included.")]
        [SerializeField, Range(0f, 1f)] private float chanceCap = 0.99f;

        [Tooltip("07.1 §7: the critical chance of a success at Perception 1.")]
        [SerializeField, Range(0f, 1f)] private float criticalAtLowest = 0.05f;

        [Tooltip("07.1 §7: the critical chance of a success at Perception 100, linear between.")]
        [SerializeField, Range(0f, 1f)] private float criticalAtHighest = 0.95f;

        [Tooltip("07.1 §7: no critical chance exceeds this, Luck included; what Luck adds above it is overflow.")]
        [SerializeField, Range(0f, 1f)] private float criticalCap = 0.95f;

        public int FirstDelta => firstDelta;
        public IReadOnlyList<float> Curve => curve;
        public float SecondaryBonus => secondaryBonus;
        public float ChanceCap => chanceCap;
        public float CriticalAtLowest => criticalAtLowest;
        public float CriticalAtHighest => criticalAtHighest;
        public float CriticalCap => criticalCap;
    }
}
