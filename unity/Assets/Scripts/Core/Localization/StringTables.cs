using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameDev.Core.Localization
{
    /// <summary>
    /// The string tables read from CSV text (17 X.7): the header is <c>key</c> followed by one column per language code,
    /// and each row is a key with its text in each language. Fields follow RFC 4180 (a field with a comma, a quote or a
    /// line break is quoted, a quote inside is doubled); blank rows and rows whose key starts with <c>#</c> are skipped.
    /// Immutable once read.
    /// </summary>
    public sealed class StringTables
    {
        public const string English = "en";

        private readonly Dictionary<string, Dictionary<string, string>> _rows = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        private readonly List<string> _languages = new List<string>();
        private readonly List<string> _problems = new List<string>();

        /// <summary>Every language some table has a column for, in the order first met.</summary>
        public IReadOnlyList<string> Languages => _languages;

        public IReadOnlyCollection<string> Keys => _rows.Keys;

        /// <summary>What was wrong in the tables: a key twice, a row with the wrong number of fields, a table without a key column.</summary>
        public IReadOnlyList<string> Problems => _problems;

        /// <summary>Reads the tables, each a (name, CSV text) pair; the name only labels problems.</summary>
        public static StringTables Parse(IEnumerable<(string name, string csv)> tables)
        {
            var result = new StringTables();
            foreach (var (name, csv) in tables)
            {
                result.Read(name, csv ?? string.Empty);
            }

            return result;
        }

        /// <summary>True when the key has text in the language.</summary>
        public bool Has(string key, string language) => TryGet(key, language, out _);

        /// <summary>True when some table has the key.</summary>
        public bool Exists(string key) => key != null && _rows.ContainsKey(key);

        public bool TryGet(string key, string language, out string text)
        {
            text = null;
            return key != null && language != null && _rows.TryGetValue(key, out var row) && row.TryGetValue(language, out text) && text.Length > 0;
        }

        /// <summary>The key's text in the language, else in English, else the key itself, so a gap shows and never breaks.</summary>
        public string Get(string key, string language)
        {
            if (TryGet(key, language, out var text) || TryGet(key, English, out text))
            {
                return text;
            }

            return key ?? string.Empty;
        }

        private void Read(string name, string csv)
        {
            var rows = Rows(csv.TrimStart('﻿')).ToList();
            if (rows.Count == 0)
            {
                return;
            }

            var header = rows[0];
            if (header.Count < 2 || header[0].Trim() != "key")
            {
                _problems.Add($"{name}: the first row must be 'key' and the language codes.");
                return;
            }

            var columns = header.Skip(1).Select(c => c.Trim()).ToList();
            foreach (var language in columns.Where(l => !_languages.Contains(l)))
            {
                _languages.Add(language);
            }

            for (var i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                var key = row[0].Trim();
                if (key.Length == 0 || key.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                if (row.Count != header.Count)
                {
                    _problems.Add($"{name}: '{key}' has {row.Count} fields, the header {header.Count}.");
                }

                if (_rows.ContainsKey(key))
                {
                    _problems.Add($"{name}: '{key}' is already in a table.");
                    continue;
                }

                var texts = new Dictionary<string, string>(StringComparer.Ordinal);
                for (var c = 0; c < columns.Count && c + 1 < row.Count; c++)
                {
                    texts[columns[c]] = row[c + 1];
                }

                _rows[key] = texts;
            }
        }

        /// <summary>The CSV's rows, each a list of fields; a blank line is skipped.</summary>
        private static IEnumerable<List<string>> Rows(string csv)
        {
            var row = new List<string>();
            var field = new StringBuilder();
            var quoted = false;
            var any = false;
            for (var i = 0; i < csv.Length; i++)
            {
                var c = csv[i];
                if (quoted)
                {
                    if (c != '"')
                    {
                        field.Append(c);
                    }
                    else if (i + 1 < csv.Length && csv[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = false;
                    }

                    continue;
                }

                switch (c)
                {
                    case '"':
                        quoted = true;
                        any = true;
                        break;
                    case ',':
                        row.Add(field.ToString());
                        field.Clear();
                        any = true;
                        break;
                    case '\r':
                        break;
                    case '\n':
                        if (any || field.Length > 0)
                        {
                            row.Add(field.ToString());
                            yield return row;
                        }

                        row = new List<string>();
                        field.Clear();
                        any = false;
                        break;
                    default:
                        field.Append(c);
                        any = true;
                        break;
                }
            }

            if (any || field.Length > 0)
            {
                row.Add(field.ToString());
                yield return row;
            }
        }
    }
}
