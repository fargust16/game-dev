using System.Collections.Generic;
using GameDev.Confront.Turns;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// A body in the scene that can fight (S3a.4): it carries its <see cref="Combatant"/>, and while it is enabled the
    /// encounter sees it, so one that comes within the Combat Grid joins a running fight at the next round.
    /// </summary>
    public sealed class SliceFighter : MonoBehaviour
    {
        private static readonly List<SliceFighter> Present = new List<SliceFighter>();

        public static IReadOnlyList<SliceFighter> All => Present;

        public Combatant Combatant { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Present.Clear();

        private void OnEnable() => Present.Add(this);

        private void OnDisable() => Present.Remove(this);
    }
}
