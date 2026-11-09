using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace GameDev.Core.Localization
{
    /// <summary>
    /// Every player-facing string goes through here (owner, 2026-11-09; 17 X.7): lines, names and UI are keys in the
    /// project's string tables, the CSV files in <c>Resources/Localization</c>, with their text in each language,
    /// Russian and English for now. A new language is a new column, never new code. Data and saves hold keys or ids,
    /// never text, so changing the language changes everything shown at once.
    /// </summary>
    public static class Loc
    {
        public const string Folder = "Localization";
        public const string English = StringTables.English;
        public const string Russian = "ru";

        /// <summary>The machine setting that keeps the chosen language; it belongs to the machine, not to a save.</summary>
        public const string Setting = "settings.language";

        private static StringTables _tables;
        private static string _language;

        /// <summary>The language changed: whatever shows text redraws it.</summary>
        public static event Action Changed;

        public static StringTables Tables => _tables ??= Load();

        public static IReadOnlyList<string> Languages => Tables.Languages;

        /// <summary>
        /// The language shown: the one chosen last on this machine, else the system's when the tables have it, else
        /// English. Setting it keeps it as the machine setting.
        /// </summary>
        public static string Language
        {
            get => _language ??= Initial();
            set
            {
                var language = Languages.Contains(value) ? value : English;
                if (language == Language)
                {
                    return;
                }

                _language = language;
                PlayerPrefs.SetString(Setting, language);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        /// <summary>The key's text in the language shown; English when it lacks one, the key itself when English does too.</summary>
        public static string Get(string key) => Tables.Get(key, Language);

        /// <summary>The key's text with its <c>{0}</c>… places filled. Numbers are written the invariant way.</summary>
        // lean-ctx: invariant numbers, upgrade to the language's culture when a string formats a fraction or a date.
        public static string Format(string key, params object[] args) => string.Format(CultureInfo.InvariantCulture, Get(key), args);

        /// <summary>True when some table has the key, in any language.</summary>
        public static bool Exists(string key) => Tables.Exists(key);

        /// <summary>The next language of the tables, round: the switch key's work until a settings page exists.</summary>
        public static void Next()
        {
            var languages = Languages;
            if (languages.Count > 1)
            {
                Language = languages[(Math.Max(0, languages.ToList().IndexOf(Language)) + 1) % languages.Count];
            }
        }

        private static string Initial()
        {
            var chosen = PlayerPrefs.GetString(Setting, string.Empty);
            if (Languages.Contains(chosen))
            {
                return chosen;
            }

            var system = Application.systemLanguage == SystemLanguage.Russian ? Russian : English;
            return Languages.Contains(system) ? system : English;
        }

        private static StringTables Load()
        {
            var tables = StringTables.Parse(Resources.LoadAll<TextAsset>(Folder).OrderBy(t => t.name, StringComparer.Ordinal).Select(t => (t.name, t.text)));
            foreach (var problem in tables.Problems)
            {
                Debug.LogError($"[Loc] {problem}");
            }

            return tables;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _tables = null;
            _language = null;
            Changed = null;
        }
    }
}
