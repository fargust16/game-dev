using System;
using System.Collections.Generic;
using GameDev.Core.Localization;
using GameDev.Input;
using GameDev.Player.Progression;
using GameDev.Slice.Fights;
using GameDev.UI;
using UnityEngine;

namespace GameDev.Slice.Creation
{
    /// <summary>
    /// S5b.2 (07.1 §3; owner, 2026-11-13, S5-1): the character's page on L out of a fight, in the page a shop uses. It
    /// names the character, its Arcana and Limit, then its seven attributes and its skills, each with its level and how
    /// far it has gone toward the next, since every use grows its skill and the attributes of its formula. L again,
    /// Escape or a fight closes it. Every word is a string table key (17 X.7).
    /// </summary>
    public sealed class CharacterPage : MonoBehaviour
    {
        /// <summary>The skills in the order the page shows them, under their headings.</summary>
        public static readonly (string Heading, CharacterSkill[] Skills)[] Groups =
        {
            ("ui.character.skills.fight", new[]
            {
                CharacterSkill.LightFirearms, CharacterSkill.HeavyFirearms, CharacterSkill.LightMelee, CharacterSkill.HeavyMelee,
                CharacterSkill.Arcana,
            }),
            ("ui.character.skills.talk", new[] { CharacterSkill.Persuasion, CharacterSkill.Deception, CharacterSkill.Intimidation }),
            ("ui.character.skills.shadow", new[] { CharacterSkill.Stealth, CharacterSkill.Theft }),
            ("ui.character.skills.craft", new[]
            {
                CharacterSkill.FirstAid, CharacterSkill.ItemCreation, CharacterSkill.Alchemy, CharacterSkill.Herbalism, CharacterSkill.Woodcutting,
                CharacterSkill.Mining, CharacterSkill.Skinning,
            }),
        };

        private CharacterSheet _sheet;
        private SliceConfront _fights;
        private Func<string> _name;
        private HudView _hud;
        private bool _open;

        public bool IsOpen => _open && _hud != null && _hud.IsShopOpen;

        private GameDev.Slice.Mind.SliceMind _mind;

        public void Configure(CharacterSheet sheet, SliceConfront fights, Func<string> name, GameDev.Slice.Mind.SliceMind mind = null)
        {
            _sheet = sheet;
            _fights = fights;
            _name = name;
            _mind = mind;
        }

        /// <summary>07.2 §3.3–3.4 (S5b.6): Sanity as its band's words, never a number, and Fatigue's words when it is felt.</summary>
        public static IEnumerable<string> MindLines(GameDev.Slice.Mind.SliceMind mind)
        {
            if (mind == null)
            {
                yield break;
            }

            yield return Loc.Format("ui.character.mind", Loc.Get(GameDev.Slice.Mind.SliceMind.BandKey(mind.Band)));
            var fatigue = GameDev.Slice.Mind.SliceMind.FatigueKey(mind.Fatigue);
            if (fatigue != null)
            {
                yield return Loc.Format("ui.character.fatigue", Loc.Get(fatigue));
            }
        }

        /// <summary>Opens the page, as L does; false in a fight, under another window, or with no HUD.</summary>
        public bool Open()
        {
            _hud ??= FindAnyObjectByType<HudView>();
            if (_sheet == null || _hud == null || (_fights != null && _fights.InFight) || (_hud.AnyWindowOpen && !IsOpen))
            {
                return false;
            }

            _open = true;
            Show();
            return true;
        }

        public void Close()
        {
            if (_open && _hud != null)
            {
                _hud.CloseShop();
            }

            _open = false;
        }

        public static string AttributeName(CharacterAttribute attribute) => Loc.Get("attribute." + attribute.ToString().ToLowerInvariant());

        /// <summary>A value's line: its name, its level and the share of the way to the next, or the level alone at the top.</summary>
        public static string Line(string name, int level, double xp) => level >= Growth.Top
            ? Loc.Format("ui.character.line.top", name, level)
            : Loc.Format("ui.character.line", name, level, Mathf.FloorToInt((float)(100d * xp / Growth.ToNext(level))));

        private void Show()
        {
            var lines = new List<PageLine> { new PageLine(Standing(), heading: true) };
            foreach (var line in MindLines(_mind))
            {
                lines.Add(new PageLine(line));
            }

            lines.Add(new PageLine(Loc.Get("ui.character.attributes"), heading: true));
            foreach (CharacterAttribute a in Enum.GetValues(typeof(CharacterAttribute)))
            {
                lines.Add(new PageLine(Line(AttributeName(a), _sheet.Get(a), _sheet.Xp(a))));
            }

            foreach (var (heading, skills) in Groups)
            {
                lines.Add(new PageLine(Loc.Get(heading), heading: true));
                foreach (var s in skills)
                {
                    lines.Add(new PageLine(Line(PersonTalk.SkillName(s), _sheet.Get(s), _sheet.Xp(s))));
                }
            }

            var name = _name?.Invoke();
            var title = string.IsNullOrWhiteSpace(name) ? Loc.Get("ui.character.title") : Loc.Get("ui.character.title") + "  ·  " + name;
            _hud.ShowShop(title, string.Empty, lines, Loc.Get("ui.character.hint"));
        }

        /// <summary>The Arcana and the Limit, or none (05.1).</summary>
        private string Standing()
        {
            if (_fights == null || _fights.PlayerArcana == Arcana.None || !_fights.PlayerLimit.HasValue)
            {
                return Loc.Get("ui.character.uninitiated");
            }

            return Loc.Format("ui.character.limit", Loc.Get(SliceConfront.ArcanaName(_fights.PlayerArcana)), _fights.PlayerLimit.Value);
        }

        private void Update()
        {
            var input = GameInputService.Instance;
            if (!_open)
            {
                if (input != null && input.CharacterPagePressed)
                {
                    Open();
                }

                return;
            }

            if (_hud == null || !_hud.IsShopOpen || (_fights != null && _fights.InFight)
                || (input != null && (input.CancelPressed || input.CharacterPagePressed)))
            {
                Close();
            }
        }

        private void OnEnable()
        {
            Loc.Changed += Relabel;
        }

        private void OnDisable()
        {
            Loc.Changed -= Relabel;
            Close();
        }

        private void Relabel()
        {
            if (IsOpen)
            {
                Show();
            }
        }
    }
}
