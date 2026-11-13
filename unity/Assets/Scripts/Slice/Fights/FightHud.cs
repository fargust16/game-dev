using System;
using System.Collections.Generic;
using System.Text;
using GameDev.Confront.Rules;
using GameDev.Confront.Turns;
using GameDev.Core.Localization;
using GameDev.Player.Progression;
using GameDev.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// The fight on the HUD (S3a.5): the order of Initiative across the top with the acting fighter lit, the player's HP
    /// as a number and every enemy's condition as a word; the player's HP and AP at the bottom; what the cursor points at
    /// beside it, its AP, and from Intelligence 10 the chance and the damage, from 20 the parts of the chance (10.4 §3);
    /// and the last blow; under the HP and AP, the hotbar of the turn's actions (<see cref="FightHotbar"/>). Every word
    /// is a string table key (17 X.7). It writes into the HUD's own document.
    /// </summary>
    public sealed class FightHud
    {
        private const string Visible = "hud__fight--visible";
        private const string TipVisible = "hud__fight-tip--visible";
        private const string Chip = "hud__fight-chip";
        private const string ChipActing = "hud__fight-chip--acting";
        private const string ChipEnemy = "hud__fight-chip--enemy";
        private const string Pip = "hud__fight-pip";
        private const string PipFull = "hud__fight-pip--full";
        private const string PipCost = "hud__fight-pip--cost";

        // Authoring defaults, S3a.5: the share of maximum HP from which each condition word holds, the highest first.
        private static readonly (double Share, string Key)[] Wounds =
        {
            (1d, "ui.fight.condition.unhurt"),
            (0.75d, "ui.fight.condition.scratched"),
            (0.5d, "ui.fight.condition.wounded"),
            (0.25d, "ui.fight.condition.badly"),
            (0d, "ui.fight.condition.dying"),
        };

        private readonly Func<Combatant, string> _name;
        private readonly SocialCheckRules _disclosure;
        private readonly StringBuilder _text = new StringBuilder();
        private readonly FightHotbar _bar;
        private UIDocument _document;
        private VisualElement _fight;
        private VisualElement _order;
        private VisualElement _ap;
        private Label _who;
        private Label _log;
        private Label _hp;
        private Label _tip;
        private Label _life;
        private bool _dirty = true;

        /// <param name="name">A combatant's name in the language shown.</param>
        /// <param name="disclosure">The Intelligence from which the chance and its parts show (10.4 §3).</param>
        /// <param name="icons">The hotbar's icons, one for each <see cref="FightAction"/> after None.</param>
        public FightHud(Func<Combatant, string> name, SocialCheckRules disclosure, IReadOnlyList<Sprite> icons = null)
        {
            _name = name;
            _disclosure = disclosure;
            _bar = new FightHotbar(icons);
        }

        /// <summary>The hotbar of the turn's actions.</summary>
        public FightHotbar Bar => Bind() ? _bar : null;

        /// <summary>True while the pointer is over the hotbar: no cell is pointed at and no click walks.</summary>
        public bool OverBar => Bind() && _bar.Over;

        /// <summary>The action clicked on the hotbar since the last call, once.</summary>
        public FightAction Take() => _bar.Take();

        public bool Shown => Bind() && _fight.ClassListContains(Visible);

        /// <summary>The fighters the order shows, in order.</summary>
        public int OrderShown => Bind() ? _order.childCount : 0;

        public string WhoText => Bind() ? _who.text : null;

        public string LogText => Bind() ? _log.text : null;

        public string HpText => Bind() ? _hp.text : null;

        /// <summary>What the tip by the cursor says, or null while it is hidden.</summary>
        public string TipText => Bind() && _tip.ClassListContains(TipVisible) ? _tip.text : null;

        public void Show(bool on)
        {
            if (!Bind())
            {
                return;
            }

            _fight.EnableInClassList(Visible, on);
            _bar.Reset();
            _log.text = string.Empty;
            _tip.RemoveFromClassList(TipVisible);
            _dirty = true;
        }

        /// <summary>The order and the bar are written again on the next <see cref="Refresh"/>.</summary>
        public void MarkDirty() => _dirty = true;

        /// <summary>What the life line by the bottom left says, or null while it is hidden.</summary>
        public string LifeText => Bind() && _life != null && _life.ClassListContains("hud__life--visible") ? _life.text : null;

        /// <summary>S3a.7: the character's HP and conditions, in and out of fights; empty hides the line.</summary>
        public void Life(string text)
        {
            if (!Bind() || _life == null)
            {
                return;
            }

            _life.text = text ?? string.Empty;
            _life.EnableInClassList("hud__life--visible", !string.IsNullOrEmpty(text));
        }

        /// <summary>A line of the fight's own, said in the log's place: an Environment Action, a treatment, a condition's tick (S3a.7).</summary>
        public void Line(string text)
        {
            if (Bind())
            {
                _log.text = text;
            }
        }

        /// <summary>The last blow, said as the log's line.</summary>
        public void Log(Combatant attacker, Combatant target, StrikeResult result)
        {
            if (!Bind() || attacker == null || target == null)
            {
                return;
            }

            var who = _name(attacker);
            var whom = _name(target);
            var line = !result.Hit
                ? Loc.Format("ui.fight.log.miss", who, whom)
                : Loc.Format(result.Guarded ? "ui.fight.log.guarded" : result.Critical ? "ui.fight.log.critical" : "ui.fight.log.hit", who, whom,
                    Math.Max(0, (int)Math.Round(result.Damage)));
            if (result.Downed)
            {
                line += "  " + Loc.Format("ui.fight.log.down", whom);
            }

            _log.text = line;
            _dirty = true;
        }

        /// <summary>
        /// Writes the order, the bar and the tip. <paramref name="settled"/> is false while the scene still shows earlier
        /// blows, and the order and the bar wait for it, so they never tell a blow before it is seen.
        /// </summary>
        /// <param name="actions">Whether each hotbar action is usable now and its AP; null dims them all.</param>
        public void Refresh(ITurnEconomy turns, Combatant player, FightCursor cursor, int turnAp, Vector2 mouse, bool settled,
            Func<FightAction, (bool Usable, int Ap)> actions = null)
        {
            if (!Bind() || turns == null)
            {
                return;
            }

            var playersTurn = settled && player != null && turns.Acting == player;
            if (_dirty && settled)
            {
                _dirty = false;
                Order(turns);
                _who.text = turns.Acting == null ? string.Empty
                    : turns.Acting == player ? Loc.Get("ui.fight.turn.yours")
                    : Loc.Format("ui.fight.turn.other", _name(turns.Acting));
                if (player != null)
                {
                    _hp.text = Loc.Format("ui.fight.hp", Hp(player.Fighter.Hp), Hp(player.Fighter.MaxHp));
                }
            }

            Pips(playersTurn ? turns.ApLeft : player?.Fighter.Reserve ?? 0, Math.Max(turnAp, playersTurn ? turns.ApLeft : 0),
                playersTurn && cursor.Affordable ? cursor.Cost : 0);
            Tip(playersTurn ? cursor : null, player, mouse);
            _bar.Refresh(actions);
        }

        private void Order(ITurnEconomy turns)
        {
            _order.Clear();
            foreach (var c in turns.Order)
            {
                if (!c.Standing)
                {
                    continue;
                }

                var chip = new VisualElement { pickingMode = PickingMode.Ignore };
                chip.AddToClassList(Chip);
                chip.EnableInClassList(ChipActing, c == turns.Acting);
                chip.EnableInClassList(ChipEnemy, c.Side != SliceConfront.PlayerSide);
                chip.Add(Text(_name(c), "hud__fight-chip-name"));
                var state = c.Side == SliceConfront.PlayerSide ? $"{Hp(c.Fighter.Hp)} / {Hp(c.Fighter.MaxHp)}" : Condition(c.Fighter);
                if (c.Fighter.Stance)
                {
                    state += " · " + Loc.Get("ui.fight.stance");
                }
                else if (c.Waiting)
                {
                    state += " · " + Loc.Get("ui.fight.waiting");
                }

                var conditions = Conditions(c.Fighter);
                if (conditions.Length > 0)
                {
                    state += " · " + conditions;
                }

                chip.Add(Text(state, "hud__fight-chip-state"));
                _order.Add(chip);
            }
        }

        private void Pips(int full, int count, int cost)
        {
            while (_ap.childCount < count)
            {
                var pip = new VisualElement { pickingMode = PickingMode.Ignore };
                pip.AddToClassList(Pip);
                _ap.Add(pip);
            }

            for (var i = 0; i < _ap.childCount; i++)
            {
                var pip = _ap[i];
                pip.style.display = i < count ? DisplayStyle.Flex : DisplayStyle.None;
                pip.EnableInClassList(PipFull, i < full - cost);
                pip.EnableInClassList(PipCost, i >= full - cost && i < full);
            }
        }

        private void Tip(FightCursor cursor, Combatant player, Vector2 mouse)
        {
            if (cursor == null || (cursor.Target == null && !cursor.Cell.HasValue) || _tip.panel == null)
            {
                _tip.RemoveFromClassList(TipVisible);
                return;
            }

            _text.Clear();
            if (cursor.Target != null)
            {
                _text.Append(_name(cursor.Target)).Append(" — ").Append(Condition(cursor.Target.Fighter)).Append('\n');
                _text.Append(cursor.Steps < 0 ? Loc.Get("ui.fight.noway")
                    : Loc.Format(cursor.Affordable ? "ui.fight.attack" : "ui.fight.attack.far", cursor.Cost));
                var intelligence = DebugSight.Read(player.Fighter.Get(CharacterAttribute.Intelligence));
                if (_disclosure != null && intelligence >= _disclosure.ChanceFromIntelligence && cursor.Steps >= 0)
                {
                    var f = cursor.Forecast;
                    _text.Append('\n').Append(Loc.Format("ui.fight.chance", Percent(f.Chance), Math.Max(1, (int)Math.Round(f.Low)), Math.Max(1, (int)Math.Round(f.High))));
                    if (intelligence >= _disclosure.ModifiersFromIntelligence)
                    {
                        _text.Append('\n').Append(Loc.Format("ui.fight.mod.base", Percent(f.BaseHit)));
                        _text.Append('\n').Append(Loc.Format("ui.fight.mod.accuracy", Signed(f.Accuracy)));
                        _text.Append('\n').Append(Loc.Format("ui.fight.mod.evasion", Signed(-f.Evasion)));
                        if (Math.Abs(f.Situational) > 0.05d)
                        {
                            _text.Append('\n').Append(Loc.Format("ui.fight.mod.position", Signed(f.Situational)));
                        }

                        _text.Append('\n').Append(Loc.Format("ui.fight.mod.luck", (1d + (f.Luck / 100d)).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));
                        if (f.Fit < 0.999d)
                        {
                            _text.Append('\n').Append(Loc.Format("ui.fight.mod.fit", f.Fit.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));
                        }
                    }
                }
            }
            else if (cursor.HasOption)
            {
                // S3a.7: an Environment Action across the edge; its chance shows as a blow's does (10.4 §3).
                var option = cursor.Option;
                _text.Append(Loc.Format("ui.fight.env." + option.Kind, cursor.Cost));
                if (!cursor.Affordable)
                {
                    _text.Append('\n').Append(Loc.Format("ui.fight.far", cursor.Cost));
                }

                if (_disclosure != null && DebugSight.Read(player.Fighter.Get(CharacterAttribute.Intelligence)) >= _disclosure.ChanceFromIntelligence)
                {
                    _text.Append('\n').Append(Loc.Format("ui.fight.env.chance", Percent(option.Chance)));
                }

                if (option.Kind == Confront.Grid.EnvironmentActionKind.ClimbUp || option.Kind == Confront.Grid.EnvironmentActionKind.MantleOnto)
                {
                    _text.Append('\n').Append(Loc.Format("ui.fight.env.fall", option.Rise.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)));
                }
            }
            else
            {
                _text.Append(cursor.Steps < 0 ? Loc.Get("ui.fight.noway") : Loc.Format(cursor.Affordable ? "ui.fight.move" : "ui.fight.far", cursor.Cost));
            }

            if (cursor.Provokes)
            {
                _text.Append('\n').Append(Loc.Get("ui.fight.provokes"));
            }

            // 10.1 *Shot Line* (CR2): what the eye sees on the line is told whatever the Intelligence.
            if (cursor.Target != null && cursor.Obscured)
            {
                _text.Append('\n').Append(Loc.Get("ui.fight.obscured"));
            }

            if (cursor.Target != null && cursor.OnLine > 0)
            {
                _text.Append('\n').Append(Loc.Format("ui.fight.online", cursor.OnLine));
            }

            _tip.text = _text.ToString();
            // The mouse is read in the camera's pixels, which are the screen's unless the camera draws into a texture.
            var height = Camera.main != null ? Camera.main.pixelHeight : Screen.height;
            var at = RuntimePanelUtils.ScreenToPanel(_tip.panel, new Vector2(mouse.x, height - mouse.y));
            _tip.style.left = at.x + 26f;
            _tip.style.top = at.y + 22f;
            _tip.AddToClassList(TipVisible);
        }

        private static string Condition(Fighter fighter)
        {
            if (fighter.Down)
            {
                return Loc.Get("ui.fight.condition.down");
            }

            var share = fighter.MaxHp > 0d ? fighter.Hp / fighter.MaxHp : 0d;
            foreach (var (from, key) in Wounds)
            {
                if (share >= from - 1e-9)
                {
                    return Loc.Get(key);
                }
            }

            return Loc.Get(Wounds[Wounds.Length - 1].Key);
        }

        /// <summary>The fighter's conditions as words, joined by a dot; empty when it has none (S3a.7).</summary>
        public static string Conditions(Fighter fighter)
        {
            var words = string.Empty;
            if (fighter.Bleeding)
            {
                words = Loc.Get("ui.condition.bleeding");
            }

            if (fighter.Poisoned)
            {
                words += (words.Length > 0 ? " · " : string.Empty) + Loc.Get("ui.condition.poisoned");
            }

            if (fighter.LegBroken)
            {
                words += (words.Length > 0 ? " · " : string.Empty) + Loc.Get("ui.condition.leg");
            }

            return words;
        }

        /// <summary>HP as the player reads it: whole, and never 0 while standing.</summary>
        public static int Hp(double hp) => hp <= 0d ? 0 : Math.Max(1, (int)Math.Ceiling(hp - 1e-9));

        private static int Percent(double share) => (int)Math.Round(share * 100d);

        private static string Signed(double points)
        {
            var rounded = (int)Math.Round(points);
            return rounded >= 0 ? "+" + rounded : "−" + (-rounded);
        }

        private static Label Text(string text, string className)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(className);
            return label;
        }

        // A UIDocument builds its tree again when it is enabled again, as a city's HUD is after an interior (17 I.2).
        private bool Bind()
        {
            if (_fight != null && _fight.panel != null)
            {
                return true;
            }

            if (_document == null)
            {
                var hud = UnityEngine.Object.FindAnyObjectByType<HudView>();
                _document = hud != null ? hud.GetComponent<UIDocument>() : null;
            }

            var root = _document != null ? _document.rootVisualElement : null;
            _fight = root?.Q<VisualElement>("fight");
            if (_fight == null)
            {
                return false;
            }

            _order = root.Q<VisualElement>("fight-order");
            _ap = root.Q<VisualElement>("fight-ap");
            _who = root.Q<Label>("fight-who");
            _log = root.Q<Label>("fight-log");
            _hp = root.Q<Label>("fight-hp");
            _tip = root.Q<Label>("fight-tip");
            _life = root.Q<Label>("life");
            _bar.Bind(root.Q<VisualElement>("fight-panel"), root.Q<VisualElement>("fight-actions"), root.Q<Label>("fight-action-tip"));
            _dirty = true;
            if (_order == null || _ap == null || _who == null || _log == null || _hp == null || _tip == null)
            {
                Debug.LogError("The HUD's fight needs 'fight-order', 'fight-ap', 'fight-who', 'fight-log', 'fight-hp' and 'fight-tip'.");
                _fight = null;
                return false;
            }

            return true;
        }
    }
}
