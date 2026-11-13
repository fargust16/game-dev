using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GameDev.Core.Save
{
    /// <summary>One system's share of a section that several systems write (17 X.3): its key and its own payload.</summary>
    public interface ISavePart
    {
        string PartKey { get; }

        string Capture();

        void Restore(string payload);
    }

    /// <summary>A part made of two functions, for a system that keeps its own save code beside it.</summary>
    public sealed class SavePart : ISavePart
    {
        private readonly Func<string> _capture;
        private readonly Action<string> _restore;

        public SavePart(string key, Func<string> capture, Action<string> restore)
        {
            PartKey = key ?? throw new ArgumentNullException(nameof(key));
            _capture = capture ?? throw new ArgumentNullException(nameof(capture));
            _restore = restore ?? throw new ArgumentNullException(nameof(restore));
        }

        public string PartKey { get; }

        public string Capture() => _capture();

        public void Restore(string payload) => _restore(payload);
    }

    /// <summary>
    /// A save section that several systems write, each its own part (17 X.3): the character's sheet beside its Stamina,
    /// the bag beside what is worn. A part missing from the payload leaves its system as it is; a part the payload has
    /// and this build does not is skipped.
    /// </summary>
    public sealed class SavePartsSection : ISaveContributor
    {
        private readonly List<ISavePart> _parts;

        public SavePartsSection(string sectionKey, int sectionVersion, IEnumerable<ISavePart> parts)
        {
            SectionKey = sectionKey;
            SectionVersion = sectionVersion;
            _parts = new List<ISavePart>(parts);
        }

        public string SectionKey { get; }

        public int SectionVersion { get; }

        public IReadOnlyList<ISavePart> Parts => _parts;

        public string CapturePayload()
        {
            var entries = new List<KeyValuePair<string, string>>();
            foreach (var part in _parts)
            {
                entries.Add(new KeyValuePair<string, string>(part.PartKey, part.Capture() ?? string.Empty));
            }

            return SaveText.Join(entries);
        }

        public void RestorePayload(int version, string payload)
        {
            var entries = new Dictionary<string, string>();
            foreach (var entry in SaveText.Split(payload))
            {
                entries[entry.Key] = entry.Value;
            }

            foreach (var part in _parts)
            {
                if (entries.TryGetValue(part.PartKey, out var value))
                {
                    part.Restore(value);
                }
            }
        }
    }

    /// <summary>
    /// A list of keyed strings as one string, each key and value written with its length first, so any text, JSON or
    /// another such list, nests without escaping.
    /// </summary>
    public static class SaveText
    {
        public static string Join(IEnumerable<KeyValuePair<string, string>> entries)
        {
            var text = new StringBuilder();
            foreach (var entry in entries)
            {
                Write(text, entry.Key ?? string.Empty);
                Write(text, entry.Value ?? string.Empty);
            }

            return text.ToString();
        }

        public static List<KeyValuePair<string, string>> Split(string text)
        {
            var entries = new List<KeyValuePair<string, string>>();
            var at = 0;
            while (text != null && at < text.Length)
            {
                var key = Read(text, ref at);
                var value = Read(text, ref at);
                entries.Add(new KeyValuePair<string, string>(key, value));
            }

            return entries;
        }

        private static void Write(StringBuilder text, string value)
        {
            text.Append(value.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(value);
        }

        private static string Read(string text, ref int at)
        {
            var colon = text.IndexOf(':', at);
            if (colon < 0 || !int.TryParse(text.Substring(at, colon - at), NumberStyles.None, CultureInfo.InvariantCulture, out var length)
                || length < 0 || colon + 1 + length > text.Length)
            {
                throw new SaveVersionException($"A saved list is broken at {at}.");
            }

            at = colon + 1 + length;
            return text.Substring(colon + 1, length);
        }
    }

    /// <summary>
    /// A thing of a scene whose state a Suspend Save keeps exactly (17 X.3): a person's place and what it is doing, a
    /// beast's, a chair knocked over. Its id is the same on every run of the same scene.
    /// </summary>
    public interface ISavedObject
    {
        string SaveId { get; }

        string CaptureState();

        /// <summary>Lets go of what it holds in the scene, a seat or a place, before any saved object is restored.</summary>
        void ClearState();

        void RestoreState(string payload);
    }
}
