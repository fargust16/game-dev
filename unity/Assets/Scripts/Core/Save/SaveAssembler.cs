using System;
using System.Collections.Generic;
using System.Linq;

namespace GameDev.Core.Save
{
    /// <summary>
    /// Gathers contributors' sections into a <see cref="SaveFile"/> and hands them back. A save from a newer build is
    /// refused whole, never half-read; a section a contributor does not find leaves that system as a new game has it.
    /// </summary>
    public static class SaveAssembler
    {
        public static SaveFile Capture(IEnumerable<ISaveContributor> contributors)
        {
            var file = new SaveFile { schemaVersion = SaveSchema.Version };
            foreach (var contributor in Checked(contributors))
            {
                file.sections.Add(new SaveSection
                {
                    key = contributor.SectionKey,
                    version = contributor.SectionVersion,
                    payload = contributor.CapturePayload(),
                });
            }

            return file;
        }

        /// <summary>Restores every contributor that has a section; returns the keys of those that had none.</summary>
        public static IReadOnlyList<string> Restore(SaveFile file, IEnumerable<ISaveContributor> contributors)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            if (file.schemaVersion > SaveSchema.Version)
            {
                throw new SaveVersionException($"The save has schema {file.schemaVersion}; this build reads up to {SaveSchema.Version}.");
            }

            var list = Checked(contributors);
            var sections = file.sections.ToDictionary(s => s.key);
            foreach (var contributor in list)
            {
                if (sections.TryGetValue(contributor.SectionKey, out var section) && section.version > contributor.SectionVersion)
                {
                    throw new SaveVersionException(
                        $"Section '{section.key}' has version {section.version}; this build reads up to {contributor.SectionVersion}.");
                }
            }

            var missing = new List<string>();
            foreach (var contributor in list)
            {
                if (sections.TryGetValue(contributor.SectionKey, out var section))
                {
                    contributor.RestorePayload(section.version, section.payload);
                }
                else
                {
                    missing.Add(contributor.SectionKey);
                }
            }

            return missing;
        }

        private static List<ISaveContributor> Checked(IEnumerable<ISaveContributor> contributors)
        {
            var list = contributors.ToList();
            foreach (var contributor in list)
            {
                if (Array.IndexOf(SaveSchema.Sections, contributor.SectionKey) < 0)
                {
                    throw new ArgumentException($"Section '{contributor.SectionKey}' is not in the save schema.");
                }
            }

            var duplicate = list.GroupBy(c => c.SectionKey).FirstOrDefault(g => g.Count() > 1);
            if (duplicate != null)
            {
                throw new ArgumentException($"Two contributors write section '{duplicate.Key}'.");
            }

            return list.OrderBy(c => Array.IndexOf(SaveSchema.Sections, c.SectionKey)).ToList();
        }
    }
}
