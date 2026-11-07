using System;
using System.Collections.Generic;
using System.Linq;

namespace GameDev.World.State
{
    /// <summary>The plain model of <see cref="IWorldState"/>: a value set to zero is removed, so the store holds only what happened.</summary>
    public sealed class WorldState : IWorldState
    {
        private readonly Dictionary<string, int> _values = new Dictionary<string, int>(StringComparer.Ordinal);

        public event Action<string, int, int> Changed;

        public IEnumerable<string> Keys => _values.Keys.OrderBy(k => k, StringComparer.Ordinal);

        public int Get(string key)
        {
            Check(key);
            return _values.TryGetValue(key, out var value) ? value : 0;
        }

        public void Set(string key, int value)
        {
            var old = Get(key);
            if (old == value)
            {
                return;
            }

            if (value == 0)
            {
                _values.Remove(key);
            }
            else
            {
                _values[key] = value;
            }

            Changed?.Invoke(key, old, value);
        }

        public bool IsSet(string key) => Get(key) != 0;

        public void Raise(string key) => Set(key, 1);

        /// <summary>Every value, in key order, for the save.</summary>
        public WorldStateEntries Capture() => new WorldStateEntries
        {
            entries = Keys.Select(k => new WorldStateEntry { key = k, value = _values[k] }).ToList(),
        };

        /// <summary>Replaces the whole store, without raising <see cref="Changed"/>: a load is not an event in the world.</summary>
        public void Restore(WorldStateEntries saved)
        {
            _values.Clear();
            foreach (var e in saved?.entries ?? new List<WorldStateEntry>())
            {
                Check(e.key);
                if (e.value != 0)
                {
                    _values[e.key] = e.value;
                }
            }
        }

        private static void Check(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("A world state key is a non-empty name.", nameof(key));
            }
        }
    }

    [Serializable]
    public sealed class WorldStateEntry
    {
        public string key;
        public int value;
    }

    [Serializable]
    public sealed class WorldStateEntries
    {
        public List<WorldStateEntry> entries = new List<WorldStateEntry>();
    }
}
