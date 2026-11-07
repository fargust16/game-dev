using System;
using System.Collections.Generic;
using System.Linq;

namespace GameDev.World.Poi
{
    /// <summary>
    /// The Character Notes (11; 09): one line for each fact found, in the order found, in the character's own words.
    /// They never sort, mark, or link facts, name a place still to visit, or set a task, and they need no limit. In S1
    /// every fact comes from a heard tip; Physical Traces and scenes add theirs with S2.
    /// </summary>
    public sealed class CharacterNotes
    {
        private readonly TipBook _tips;

        public CharacterNotes(TipBook tips)
        {
            _tips = tips ?? throw new ArgumentNullException(nameof(tips));
            _tips.Heard += tip =>
            {
                if (!string.IsNullOrWhiteSpace(tip.note))
                {
                    Added?.Invoke(tip.note);
                }
            };
        }

        /// <summary>A fact was noted.</summary>
        public event Action<string> Added;

        /// <summary>Every noted fact, first found first.</summary>
        public IReadOnlyList<string> Lines =>
            _tips.HeardInOrder().Where(t => !string.IsNullOrWhiteSpace(t.note)).Select(t => t.note).ToList();
    }
}
