using System;
using System.Collections.Generic;
using System.Linq;

namespace GameDev.World.Poi
{
    /// <summary>
    /// The Character Notes (11; 09): one line for each fact found, in the order found, in the character's own words.
    /// They never sort, mark, or link facts, name a place still to visit, or set a task, and they need no limit. In S1
    /// every fact comes from a heard tip; Physical Traces and scenes add theirs with S2, and the News Pool's heard items
    /// theirs with S5c.7 (<see cref="Include"/>). The notes keep which facts were
    /// found and read their words from the string tables when shown, so they follow the language.
    /// </summary>
    public sealed class CharacterNotes
    {
        private readonly TipBook _tips;

        public CharacterNotes(TipBook tips)
        {
            _tips = tips ?? throw new ArgumentNullException(nameof(tips));
            _tips.Heard += tip =>
            {
                if (tip.HasNote)
                {
                    Added?.Invoke(tip.Note);
                }
            };
        }

        /// <summary>A fact was noted.</summary>
        public event Action<string> Added;

        private readonly List<Func<IEnumerable<(int Place, string Line)>>> _more = new List<Func<IEnumerable<(int Place, string Line)>>>();

        /// <summary>
        /// Another kind of fact heard, each with its place in the order the Tips are heard: the News Pool's items
        /// (09 reading 5; S5c.7).
        /// </summary>
        public void Include(Func<IEnumerable<(int Place, string Line)>> facts) => _more.Add(facts ?? throw new ArgumentNullException(nameof(facts)));

        /// <summary>Says that a fact of an included kind was noted.</summary>
        public void Noted(string line) => Added?.Invoke(line);

        /// <summary>Every noted fact, first found first, in the language shown.</summary>
        public IReadOnlyList<string> Lines => _tips.HeardInOrder().Where(t => t.HasNote).Select(t => (Place: _tips.PlaceOf(t), Line: t.Note))
            .Concat(_more.SelectMany(f => f()))
            .OrderBy(p => p.Place)
            .Select(p => p.Line)
            .ToList();
    }
}
