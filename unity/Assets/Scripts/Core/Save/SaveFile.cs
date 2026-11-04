using System;
using System.Collections.Generic;

namespace GameDev.Core.Save
{
    /// <summary>One section of a save: its key, the owning system's version of it, and that system's own payload.</summary>
    [Serializable]
    public sealed class SaveSection
    {
        public string key;
        public int version;
        public string payload;
    }

    /// <summary>A whole save: the schema version and its sections. Public fields, so any serializer can write it.</summary>
    [Serializable]
    public sealed class SaveFile
    {
        public int schemaVersion;
        public List<SaveSection> sections = new List<SaveSection>();
    }

    /// <summary>A system whose state lives in one section of a save.</summary>
    public interface ISaveContributor
    {
        /// <summary>One of the keys in <see cref="SaveSchema"/>.</summary>
        string SectionKey { get; }

        /// <summary>The version of this section's payload; raise it when the payload's layout changes.</summary>
        int SectionVersion { get; }

        string CapturePayload();

        /// <summary>Reads a payload written by this or an older version of the section.</summary>
        void RestorePayload(int version, string payload);
    }

    /// <summary>A save that this build cannot read.</summary>
    public sealed class SaveVersionException : Exception
    {
        public SaveVersionException(string message) : base(message)
        {
        }
    }
}
