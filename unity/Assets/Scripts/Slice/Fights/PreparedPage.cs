using System.Collections.Generic;
using GameDev.Confront.Rules;
using GameDev.Core.Localization;
using GameDev.Input;
using GameDev.UI;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// CR4, 07.2 §4.5: the page of Prepared Abilities, on K out of a fight, in the page a shop uses. It lists the
    /// character's abilities a fight can use, each marked prepared or not, with its share of Will Power and its AP; then
    /// those of exploration, open out of a fight without a slot; then its passive ones, which always work and take no
    /// slot. A click prepares an ability or puts it away for one tick, while a slot is free. K again, Escape or a fight
    /// closes it. Every word is a string table key (17 X.7).
    /// </summary>
    public sealed class PreparedPage : MonoBehaviour
    {
        private SliceConfront _fights;
        private HudView _hud;
        private string _note;
        private bool _open;

        public bool IsOpen => _open && _hud != null && _hud.IsShopOpen;

        public void Configure(SliceConfront fights) => _fights = fights;

        /// <summary>Opens the page, as K does; false in a fight or with no HUD.</summary>
        public bool Open()
        {
            _hud ??= FindAnyObjectByType<HudView>();
            if (_fights == null || _hud == null || _fights.InFight || (_hud.AnyWindowOpen && !IsOpen))
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
            _note = null;
        }

        private void Show()
        {
            var known = _fights.KnownAbilities();
            var lines = new List<PageLine>();
            if (known.Count == 0)
            {
                lines.Add(new PageLine(Loc.Get("ui.prepared.none")));
            }
            else
            {
                var slots = _fights.PreparedSlots();
                var prepared = _fights.PreparedAbilities(slots);
                lines.Add(new PageLine(Loc.Format("ui.prepared.slots", prepared.Count, slots), heading: true));
                lines.Add(new PageLine(Loc.Get("ui.prepared.active"), heading: true));
                foreach (var a in known)
                {
                    if (!a.InFight)
                    {
                        continue;
                    }

                    var ability = a;
                    var mark = Loc.Get(prepared.Contains(a) ? "ui.prepared.mark.on" : "ui.prepared.mark.off");
                    var text = Loc.Format("ui.prepared.line.ap", mark, Loc.Get($"ability.{a.Key}.name"), Mathf.RoundToInt(a.Cost * 100f), a.Ap);
                    lines.Add(new PageLine(text, () => Toggle(ability)));
                }

                // 05.1 *Вне боя* (S5b.8): every active ability with a use out of a fight, open there without a slot, by its key.
                var outside = _fights.OutsideAbilities();
                if (outside.Count > 0)
                {
                    lines.Add(new PageLine(Loc.Get("ui.prepared.outside"), heading: true));
                    for (var i = 0; i < outside.Count && i < 9; i++)
                    {
                        var a = outside[i];
                        lines.Add(new PageLine(Loc.Format("ui.prepared.line", (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                            Loc.Get($"ability.{a.Key}.name"), Mathf.RoundToInt(a.Cost * 100f)) + "  ·  " + Loc.Get($"ability.{a.Key}.outside")));
                    }
                }

                lines.Add(new PageLine(Loc.Get("ui.prepared.passive"), heading: true));
                foreach (var a in known)
                {
                    if (a.Passive)
                    {
                        lines.Add(new PageLine(Loc.Get($"ability.{a.Key}.name") + "  ·  " + Loc.Get($"ability.{a.Key}.text")));
                    }
                }
            }

            var arcana = _fights.PlayerArcana;
            var title = arcana == GameDev.Player.Progression.Arcana.None
                ? Loc.Get("ui.prepared.title")
                : Loc.Get("ui.prepared.title") + "  ·  " + Loc.Get(SliceConfront.ArcanaName(arcana));
            _hud.ShowShop(title, string.Empty, lines, _note ?? Loc.Get("ui.prepared.hint"));
        }

        private void Toggle(FightAbility ability)
        {
            var fault = _fights.TogglePrepared(ability);
            _note = Loc.Get(fault ?? "ui.prepared.changed");
            Show();
        }

        private void Update()
        {
            var input = GameInputService.Instance;
            if (!_open)
            {
                if (input != null && input.PreparedPressed)
                {
                    _note = null;
                    Open();
                }

                return;
            }

            if (_hud == null || !_hud.IsShopOpen || _fights == null || _fights.InFight
                || (input != null && (input.CancelPressed || input.PreparedPressed)))
            {
                Close();
            }
        }

        private void OnEnable() => Loc.Changed += Relabel;

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
