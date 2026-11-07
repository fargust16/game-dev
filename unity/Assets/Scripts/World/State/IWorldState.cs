using System;
using System.Collections.Generic;

namespace GameDev.World.State
{
    /// <summary>
    /// The world state store (17 C.6, VERTICAL_SLICE_IMPLEMENTATION_PLAN S1.1): one store of named values that residue,
    /// Aftermath, and decision outcomes write and the world reads. A value is a whole number; a flag is a value that is
    /// not zero. A key is a dotted name owned by the system that writes it, for example <c>poi.a.aftermath</c>. A key
    /// never written reads as zero, so a new game needs no setup and a missing save section leaves the world as new.
    /// </summary>
    public interface IWorldState
    {
        /// <summary>Raised after a value changes, with its key, the old value, and the new one.</summary>
        event Action<string, int, int> Changed;

        /// <summary>Every key that holds a value other than zero.</summary>
        IEnumerable<string> Keys { get; }

        int Get(string key);

        void Set(string key, int value);

        bool IsSet(string key);

        void Raise(string key);
    }
}
