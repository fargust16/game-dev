using System;
using System.Collections.Generic;
using GameDev.Confront.Turns;
using GameDev.Core.Localization;
using GameDev.Core.Scenes;
using GameDev.Input;
using GameDev.Player.Standing;
using GameDev.UI;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// S3a.9: leaving a fight in the slice (08 §5, §7; 10.2 §9). Since the owner's direction of 2026-11-12 the player
    /// breaks away by Сбежать, Z, for 2 AP, seen by no enemy and 15 cells or more from the nearest, and the fight ends.
    /// Y yields to people, for 2 AP: the fight ends at once, and their terms are said in the
    /// dialogue column. The Wardens take the character to their lock-up until 08:00 and keep its weapons till it pays a
    /// fine of 1 000 bronze; Veinmarket and robbers take what it carries. The cult and beasts take no surrender. A
    /// person of the other side who is losing asks for mercy, and V spares every one asking. At 0 HP the character
    /// dies, in a fight or bleeding out after one, and the run ends (<see cref="SliceDeathScreen"/>). What the fight
    /// leaves: the Deeds Record's kills, the bodies, which stay until the player is away, a world-state count of fights
    /// with each group, and a fight with a group cancels the debt it owed the player.
    /// </summary>
    public sealed partial class SliceConfront
    {
        // The groups a slice fighter belongs to; their names are the keys of the world state they write.
        public const string Cult = "cult";
        public const string Wardens = "wardens";
        public const string Veinmarket = "veinmarket";
        public const string Robbers = "robbers";
        public const string Beasts = "beasts";

        /// <summary>The fine the Wardens hold the character's weapons for, in bronze (10.2 §9, owner-approved).</summary>
        public const int WardensFine = 1000;

        /// <summary>The bronze the character owes a group, until S5a's purse settles it (plan, S3 authoring default).</summary>
        public static string OwedKey(string group) => "owed." + group;

        /// <summary>A take owed to Veinmarket or robbers: all money, and the most valuable worn item under 1 000 bronze (10.2 §9).</summary>
        public static string TakeKey(string group) => "owed." + group + ".take";

        /// <summary>How many fights the character has had with a group.</summary>
        public static string FoughtKey(string group) => "fought." + group;

        /// <summary>How many of a group the character has spared (08 §7).</summary>
        public static string SparedKey(string group) => "spared." + group;

        /// <summary>A group in the character's debt (S2.5, S2.6); a fight with the group cancels it.</summary>
        public static string DebtKey(string group) => "debt." + group;

        public const string WeaponsHeldKey = "held.wardens.weapons";
        public const string LockupsKey = "wardens.lockups";

        // The lock-up's interior, and the door the character is let out by (VEILMOUTH_CITY_PLAN §5.3).
        public const string LockupScene = "WardensLockup";
        public const string LockupArrival = "wardens-lockup.inside";

        // Authoring defaults: the Wardens let the character out at 08:00; a body is cleared once the player is this far away, m.
        private const int ReleaseMinute = 8 * 60;
        private const float BodyGone = 60f;

        private readonly Dictionary<Combatant, string> _groups = new Dictionary<Combatant, string>();
        private readonly HashSet<Combatant> _killed = new HashSet<Combatant>();
        private readonly List<GameObject> _left = new List<GameObject>();
        private bool _brokeAway;
        private bool _yielded;

        // 10.2 §5f: the character was subdued by a Warden's baton, and goes to the lock-up once the fight's beats are over.
        private bool _subdued;

        /// <summary>Authoring default, HP: what a subdued character wakes with, the Wardens binding its wound (COMBAT-9).</summary>
        private const double SubduedHp = 1d;
        private bool _died;
        private string _yieldedTo;
        private string _terms;
        private IDisposable _termsPause;
        private HudView _hudView;

        /// <summary>The Deeds Record, which counts the character's kills (S2.2); set by the world.</summary>
        public SocialStanding Standing { get; set; }

        /// <summary>True when the last fight ended with the player broken away.</summary>
        public bool BrokeAway => _brokeAway;

        /// <summary>The group whose terms the dialogue column shows after a Yield, or null.</summary>
        public string Terms => _terms;

        /// <summary>True once the character has died; the run is over.</summary>
        public bool Dead => _died;

        /// <summary>The bodies a fight left that still lie where they fell.</summary>
        public int BodiesLeft => _left.Count;

        public static bool TakesYield(string group) => group == Wardens || group == Veinmarket || group == Robbers;

        private string Group(Combatant c) => c != null && _groups.TryGetValue(c, out var group) ? group : null;

        private void ResetLeaving()
        {
            _killed.Clear();
            _brokeAway = _yielded = false;
        }

        /// <summary>
        /// Сбежать in the player's turn (Z or its slot; 10.2 §9, owner 2026-11-12): a refusal says why, an enemy's sight
        /// first, then one too near; AP short only dims the slot.
        /// </summary>
        private bool BreakAwayByWill(Combatant player)
        {
            if (_turns.SubmitIntent(TurnIntent.BreakAway))
            {
                return true;
            }

            if (_turns.Seen(player))
            {
                _hud.Line(Loc.Get("ui.fight.noflee.seen"));
            }
            else if (_turns.NearestEnemy(player) < _settings.FightRules.BreakAwayCells)
            {
                _hud.Line(Loc.Get("ui.fight.noflee.near"));
            }

            return true;
        }

        /// <summary>The player's Yield and Spare in its turn (Y, V, or their hotbar slots); a refused Yield says why.</summary>
        private bool LeaveByWill(Combatant player, bool yield, bool spare)
        {
            if (yield)
            {
                if (!_turns.SubmitIntent(TurnIntent.Yield))
                {
                    _hud.Line(Loc.Get("ui.fight.noyield"));
                }

                return true;
            }

            if (!spare)
            {
                return false;
            }

            var spared = false;
            foreach (var c in new List<Combatant>(_turns.Order))
            {
                if (c.AsksMercy && c.Standing && c.Side != player.Side && _turns.IsRunning)
                {
                    spared |= _turns.SubmitIntent(TurnIntent.Spare(c.Id));
                }
            }

            return spared;
        }

        private void OnDeparted(Combatant c)
        {
            var key = c.Departure switch
            {
                Departure.BrokeAway => "ui.fight.log.brokeaway",
                Departure.Spared => "ui.fight.log.spared",
                Departure.Fled => "ui.fight.log.fled",
                Departure.Withdrew => "ui.fight.log.withdrew",
                _ => "ui.fight.log.yield",
            };
            _hud.Line(Loc.Format(key, Name(c)));
            _hud.MarkDirty();
            ConductDeparted(c);
            if (c.Departure == Departure.Spared && Group(c) is string group && _state != null)
            {
                _state.Set(SparedKey(group), _state.Get(SparedKey(group)) + 1);
            }

            if (c != _playerCombatant)
            {
                if (_bodies.TryGetValue(c, out var body) && body != null)
                {
                    Guard(c, body, false);
                    if (c.Departure == Departure.BrokeAway && c.HelpAt is Vector3 help)
                    {
                        HelpRun.Begin(body, help, _clock); // it goes to find help (10.2 §5a)
                        SendRunner(body, help); // the runner who fetches the post's Wardens (§5e)
                    }
                }

                return;
            }

            if (c.Departure == Departure.Yielded)
            {
                _yielded = true;
            }
            else if (c.Departure == Departure.BrokeAway)
            {
                // The fight was kept round the player; once it has left, what remains of it is not played here. Those who
                // meant to strike it and saw it search where they last saw it (10.2 §9).
                _brokeAway = true;
                ChooseSearchers(c);
                if (_turns.IsRunning)
                {
                    _turns.Stop();
                }
            }
        }

        private void OnMercyAsked(Combatant c)
        {
            Crouch(c);
            _hud.Line(Loc.Format("ui.fight.log.mercy", Name(c)) + "     " + Loc.Get("ui.fight.mercy"));
            _hud.MarkDirty();
        }

        /// <summary>A blow that brings a fighter down by the player's hand is a kill in the Deeds Record (S2.2).</summary>
        private void CountKill(Combatant attacker, Combatant target)
        {
            if (attacker == _playerCombatant && target != null && target.DoubleOf == null && target.Fighter.Down && _killed.Add(target))
            {
                Standing?.RecordKill();
                if (Innocent(target))
                {
                    // 10.2 §5c: known even unseen, as the goddess tells the Wardens.
                    Standing?.Record(Deed.InnocentKilled);
                }
            }
        }

        /// <summary>
        /// 08 §5: a fight with each group is counted, and cancels the debt the group owed the player. The player's own
        /// death or Yield is decided in <see cref="OnEnded"/>.
        /// </summary>
        private void Residue()
        {
            if (_state == null || _playerCombatant == null)
            {
                return;
            }

            var counted = new HashSet<string>();
            foreach (var c in _turns.Order)
            {
                if (c.Side != _playerCombatant.Side && Group(c) is string group && counted.Add(group))
                {
                    _state.Set(FoughtKey(group), _state.Get(FoughtKey(group)) + 1);
                    _state.Set(DebtKey(group), 0);
                }
            }
        }

        /// <summary>
        /// After the last beat: a fallen development opponent stays where it lies until the player is away; the rest of
        /// them leave, and a Yield's terms open or the character's death ends the run.
        /// </summary>
        private void AfterFight(string yieldedTo)
        {
            foreach (var pair in _bodies)
            {
                var body = pair.Value;
                if (body != null && pair.Key.Fighter.Down && _developmentOpponents.Remove(body.gameObject))
                {
                    _left.Add(body.gameObject);
                }
            }

            if (_died)
            {
                if (_player != null)
                {
                    _player.enabled = false;
                }

                SliceDeathScreen.Show(_clock);
            }
            else if (_yielded && yieldedTo != null)
            {
                OpenTerms(yieldedTo);
            }
        }

        /// <summary>Clears the bodies a fight left once the player is far enough away that nobody sees them go.</summary>
        private void ClearBodies()
        {
            if (_left.Count == 0 || _player == null)
            {
                return;
            }

            var at = _player.transform.position;
            for (var i = _left.Count - 1; i >= 0; i--)
            {
                var body = _left[i];
                if (body == null)
                {
                    _left.RemoveAt(i);
                }
                else if (body.activeInHierarchy && (body.transform.position - at).sqrMagnitude > BodyGone * BodyGone)
                {
                    Destroy(body); // a body in the city switched off while the player is indoors waits
                    _left.RemoveAt(i);
                }
            }
        }

        /// <summary>10.2 §9: the terms of the people the character yielded to, in the dialogue column; the world waits.</summary>
        private void OpenTerms(string group)
        {
            _terms = group;
            _termsPause = _clock?.Pause();
            if (_player != null)
            {
                _player.enabled = false;
            }

            var lines = new List<(string, string)>
            {
                (Loc.Get(group == Wardens ? "ui.fight.warden" : "ui.fight.veinmarket"), Loc.Get(group == Wardens ? "ui.yield.wardens.line" : "ui.yield.take.line")),
            };
            var options = new List<string> { "1. " + Loc.Get(group == Wardens ? "ui.yield.wardens.go" : "ui.yield.take.go") };
            DialogueColumn()?.ShowDialogue(lines, options);
        }

        private void TermsInput(GameInputService input)
        {
            if (input != null && (input.OptionPressed(1) || input.EndTurnPressed))
            {
                AcceptTerms();
            }
        }

        /// <summary>
        /// The character goes with them. The dialogue's end costs its tick. The Wardens hold it until 08:00, the night
        /// passing on the clock, keep its weapons, and let it out by the lock-up's door, owing the fine; Veinmarket and
        /// robbers leave it where it stands, owing their take until S5a's purse settles it.
        /// </summary>
        public void AcceptTerms()
        {
            var group = _terms;
            if (group == null)
            {
                return;
            }

            _terms = null;
            DialogueColumn()?.CloseDialogue();
            _termsPause?.Dispose();
            _termsPause = null;
            _clock?.AdvanceTicks(1);
            if (_player != null)
            {
                _player.enabled = true;
            }

            if (group != Wardens)
            {
                _state?.Set(TakeKey(group), 1);
                Note(Loc.Get("ui.yield.taken"));
                return;
            }

            if (_state != null)
            {
                _state.Set(OwedKey(Wardens), _state.Get(OwedKey(Wardens)) + WardensFine);
                _state.Set(WeaponsHeldKey, 1);
                _state.Set(LockupsKey, _state.Get(LockupsKey) + 1);
            }

            // lean-ctx: the night always lasts until 08:00; upgrade when S4's invasion exists to cut it short.
            if (_clock != null)
            {
                var wait = (ReleaseMinute - _clock.MinuteOfDay + 1440) % 1440;
                _clock.AdvanceTicks(wait == 0 ? 1440 : wait);
            }

            Note(Loc.Get("ui.lockup.released"));
            SceneTransition.Travel(new TravelRequest(LockupScene, LockupArrival));
        }

        /// <summary>
        /// 10.4 §6 *Accusation*: the character goes with the Wardens to the lock-up, on a Yield's terms, owing
        /// <paramref name="extraFine"/> bronze more, what an honest Warden refused.
        /// </summary>
        public void GoToLockUp(int extraFine = 0)
        {
            if (_state != null && extraFine > 0)
            {
                _state.Set(OwedKey(Wardens), _state.Get(OwedKey(Wardens)) + extraFine);
            }

            _terms = Wardens;
            AcceptTerms();
        }

        private HudView DialogueColumn()
        {
            if (_hudView == null)
            {
                _hudView = FindAnyObjectByType<HudView>();
            }

            return _hudView;
        }
    }
}
