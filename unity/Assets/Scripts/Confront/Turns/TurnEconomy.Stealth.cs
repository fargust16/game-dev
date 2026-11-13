using System;
using System.Collections.Generic;
using GameDev.Confront.Grid;
using GameDev.Confront.Stealth;
using GameDev.Player.Progression;
using UnityEngine;

namespace GameDev.Confront.Turns
{
    /// <summary>
    /// S3b.1, stealth in a fight (10.3 §7–8): a fighter enters Stealth Mode for 3 AP when no enemy's cone covers it and
    /// walks crouching at 3 AP for two cells; a cone that comes to touch it is one check on 07.1's curve at 10.3 §3's Δ
    /// without distance, and a failure reveals it at once. Enemies do not pick a hidden fighter as a target, nor strike
    /// it as it walks past. An attack from Stealth Mode is a stealth attack and reveals the attacker; a fight may open
    /// with one, before Initiative. Allies never break stealth in a fight (10.3 §5).
    /// </summary>
    public sealed partial class TurnEconomy
    {
        private readonly HashSet<(int Observer, int Hider)> _covering = new HashSet<(int, int)>();

        /// <summary>10.3's numbers; without them no fighter can hide.</summary>
        public StealthRules Stealth { get; set; }

        /// <summary>The Darkness of 10.3 §3 at a cell, added to a hider's Stealth skill; none if unset.</summary>
        public Func<CombatCell, int> DarknessAt { get; set; }

        /// <summary>A fighter entered or left Stealth Mode.</summary>
        public event Action<Combatant> HiddenChanged;

        /// <summary>A cone touched a hidden fighter: the observer, the hider, the chance, and whether it stayed hidden.</summary>
        public event Action<Combatant, Combatant, double, bool> ConeChecked;

        /// <summary>Whether this fighter may enter Stealth Mode now: rules set, not hidden, the AP, and no enemy cone on it.</summary>
        public bool CanHide(Combatant c) =>
            Stealth != null && c != null && c.Standing && !c.Hidden && ApLeft >= Stealth.EnterAp && Acting == c && Watcher(c) == null;

        /// <summary>The AP a walk of this many steps costs the fighter: 3 for two cells crouching.</summary>
        public int WalkAp(Combatant c, int steps) => c != null && c.Hidden && Stealth != null ? Stealth.CrouchAp(steps) : steps;

        private bool Hide(Combatant c)
        {
            if (!CanHide(c))
            {
                return false;
            }

            ApLeft -= Stealth.EnterAp;
            SetHidden(c, true);
            return true;
        }

        private void SetHidden(Combatant c, bool hidden)
        {
            if (c.Hidden == hidden)
            {
                return;
            }

            c.Hidden = hidden;
            _covering.RemoveWhere(p => p.Hider == c.Id);
            if (hidden)
            {
                // The cones on it now were weighed when it hid; only a cone that comes to touch it later checks.
                foreach (var e in _order)
                {
                    if (Covers(e, c))
                    {
                        _covering.Add((e.Id, c.Id));
                    }
                }
            }

            HiddenChanged?.Invoke(c);
        }

        /// <summary>An enemy standing whose cone covers the fighter's cell in sight, or null.</summary>
        private Combatant Watcher(Combatant c)
        {
            foreach (var e in _order)
            {
                if (Covers(e, c))
                {
                    return e;
                }
            }

            return null;
        }

        /// <summary>10.3 §2 on the grid: an enemy with a cone that can act, its cone along its facing, the hider in reach and in sight.</summary>
        private bool Covers(Combatant observer, Combatant hider)
        {
            if (Stealth == null || observer == hider || !observer.Watches || !observer.Standing || observer.Side == hider.Side || observer.Helpless
                || hider.Fighter.Veiled > 0)
            {
                return false; // 05.1 Двойник: no cone sees the unseen
            }

            var facing = new Vector3(observer.Facing.x, 0f, observer.Facing.y);
            return Stealth.InCone(_grid.Center(observer.Cell), facing, _grid.Center(hider.Cell), _grid.CellSize)
                && _grid.CanSee(observer.Cell, hider.Cell);
        }

        /// <summary>
        /// 10.3 §8: each cone that has come to touch a hidden fighter since the last look is one check, and a failure
        /// reveals the fighter at once. A cone that stays on it does not check again.
        /// </summary>
        private void CheckCones()
        {
            if (Stealth == null || !IsRunning)
            {
                return;
            }

            Look();

            foreach (var h in _order)
            {
                if (!h.Hidden)
                {
                    continue;
                }

                if (!h.Standing)
                {
                    SetHidden(h, false);
                    continue;
                }

                foreach (var e in _order)
                {
                    var pair = (e.Id, h.Id);
                    if (!Covers(e, h))
                    {
                        _covering.Remove(pair);
                        continue;
                    }

                    if (!_covering.Add(pair))
                    {
                        continue;
                    }

                    var chance = ConeChance(e, h);
                    var stays = _rolls.NextDouble() < chance;
                    ConeChecked?.Invoke(e, h, chance, stays);
                    if (!stays)
                    {
                        SetHidden(h, false);
                        break;
                    }
                }
            }
        }

        /// <summary>10.3 §8: the chance to stay hidden from one observer, 07.1's curve at §3's Δ without distance.</summary>
        public double ConeChance(Combatant observer, Combatant hider)
        {
            var values = hider.Fighter.Values;
            var darkness = DarknessAt?.Invoke(hider.Cell) ?? 0;
            var delta = Stealth.Delta(values.Get(CharacterAttribute.Dexterity), values.Get(CharacterSkill.Stealth), darkness,
                observer.Fighter.Get(CharacterAttribute.Perception), false);
            return Stealth.TurnCheckChance(delta);
        }

        /// <summary>
        /// 10.3 §7: a stealth attack opening the fight, before anyone's turn; it costs no AP of the attacker's first
        /// turn, and Initiative orders the round after it, so an attacker who wins Initiative acts again. An attacker
        /// who is not hidden strikes an ordinary blow, as an Assault's opening one (10.2 §5c).
        /// </summary>
        private void Ambush(Combatant attacker, Combatant target)
        {
            if (attacker == null || target == null || !attacker.Standing || !target.Standing || attacker.Side == target.Side
                || !Reaches(attacker, target))
            {
                return;
            }

            Face(attacker, target.Cell);
            if (attacker.Fighter.Weapon.Ranged)
            {
                target = ShotTaker(attacker, attacker.Cell, target); // 10.1 *Shot Line*
            }

            var context = ContextFor(attacker, attacker.Cell, target);
            context.Stealth = attacker.Hidden;
            attacker.Fired |= attacker.Fighter.Weapon.Ranged;
            context.MagicShare = Infuse(attacker);
            var result = _rules.Strike(attacker.Fighter, target.Fighter, context, _rolls);
            Spent(target);
            Afflict(target, result);
            Subdue(attacker, target);
            if (target.Fighter.Down)
            {
                _grid.Vacate(target.Id);
            }

            SetHidden(attacker, false);
            Struck?.Invoke(attacker, target, result);
        }

        /// <summary>
        /// 10.2 §9: each fighter whose cone has the enemy it chose in sight notes where that enemy stands; a hidden one is
        /// not seen by being under a cone, only by a failed check, which reveals it.
        /// </summary>
        private void Look()
        {
            foreach (var o in _order)
            {
                if (o.Intent != null && !Unseen(o.Intent) && Covers(o, o.Intent))
                {
                    o.SawAt = o.Intent.Position;
                    o.SawWho = o.Intent;
                }
            }
        }

        /// <summary>A step of <paramref name="c"/>: each fighter that chose it and has it in its cone notes the cell.</summary>
        private void Spotted(Combatant c)
        {
            if (Stealth == null || Unseen(c))
            {
                return;
            }

            foreach (var o in _order)
            {
                if (o.Intent == c && Covers(o, c))
                {
                    o.SawAt = c.Position;
                    o.SawWho = c;
                }
            }
        }

        /// <summary>
        /// At the start, a fighter that has faced nowhere yet faces the nearest enemy, and each means to strike its nearest
        /// until its first turn chooses (10.2 §9).
        /// </summary>
        private void FaceTheFight()
        {
            foreach (var c in _order)
            {
                Combatant nearest = null;
                var best = int.MaxValue;
                foreach (var e in _order)
                {
                    var d = e.Side == c.Side ? int.MaxValue : Distance(c.Cell, e.Cell);
                    if (d < best)
                    {
                        best = d;
                        nearest = e;
                    }
                }

                if (nearest == null)
                {
                    continue;
                }

                c.Intent ??= nearest;
                if (c.Facing == Vector2Int.zero)
                {
                    Face(c, nearest.Cell);
                }

            }
        }
    }
}
