using System;
using System.Collections.Generic;
using GameDev.Confront.Grid;
using GameDev.Confront.Rules;

namespace GameDev.Confront.Turns
{
    /// <summary>
    /// CR5, 05.1 Двойник (10.2 §5f *In the slice*): a still copy stays where its caster stood, and the caster is unseen to
    /// the end of its next turn: its enemies neither target it nor see it by their cones, and they take the copy for it.
    /// The copy breaks at the first hit or ability that lands on it, and the enemies then know; it never acts, holds the
    /// cell its caster left, surrounds nobody and takes no shot meant for another. Damage the caster deals shows it
    /// again, and its copy goes with the veil. Nothing of it outlives the fight.
    /// </summary>
    public sealed partial class TurnEconomy
    {
        // An occupant id for a copy, far above any combatant's.
        private const int CopyIds = 1 << 20;

        private readonly List<Combatant> _doubles = new List<Combatant>();

        /// <summary>05.1 Двойник: a copy ended: its caster, the copy, and true when it broke at a hit, false when it faded with the veil.</summary>
        public event Action<Combatant, Combatant, bool> DoubleEnded;

        /// <summary>05.1 Двойник: a fighter became unseen, or was seen again.</summary>
        public event Action<Combatant> VeilChanged;

        /// <summary>The copies standing now.</summary>
        public IReadOnlyList<Combatant> Doubles => _doubles;

        /// <summary>Enemies neither target nor see it: in Stealth Mode (10.3 §8), or veiled by an ability.</summary>
        private static bool Unseen(Combatant c) => c.Hidden || c.Fighter.Veiled > 0;

        /// <summary>05.1 Двойник: the copy where the caster stands, and the caster unseen for the rest of this turn and its next.</summary>
        private void RaiseDouble(Combatant c, FightAbility ability)
        {
            var copy = new Combatant(CopyIds + c.Id, c.Side, c.Fighter.Copy(), c.Position)
            {
                DoubleOf = c,
                Cell = c.Cell,
                State = CombatantState.Fighting,
                Facing = c.Facing,
                Initiative = c.Initiative,
            };
            c.Double = copy;
            _doubles.Add(copy);
            c.Fighter.Veiled = Math.Max(1, ability.Rounds) + 1;
            foreach (var e in _order)
            {
                if (e.Intent == c)
                {
                    e.Intent = copy; // those who meant to strike it now mean the copy
                }
            }

            AbilityAt?.Invoke(c, ability, c.Cell);
            VeilChanged?.Invoke(c);
        }

        /// <summary>The copy holds the cell its caster has left, when nobody else has stepped in.</summary>
        private void Settle(Combatant c)
        {
            var copy = c.Double;
            if (copy != null && copy.Standing && c.Cell != copy.Cell && _grid.OccupantAt(copy.Cell) < 0)
            {
                _grid.Occupy(copy.Id, copy.Cell);
            }
        }

        /// <summary>A turn of <paramref name="c"/> ends: one turn's end less of its veil, and seen again at the last.</summary>
        private void TurnEnds(Combatant c)
        {
            if (c.Fighter.Veiled == 1)
            {
                Unveil(c);
            }
            else if (c.Fighter.Veiled > 1)
            {
                c.Fighter.Veiled--;
            }
        }

        /// <summary>A hit or an ability that landed: a copy struck breaks; damage an unseen attacker dealt shows it.</summary>
        private void AfterHit(Combatant attacker, Combatant target, bool landed, double damage)
        {
            if (target != null && target.DoubleOf != null && landed)
            {
                EndDouble(target, true);
            }

            if (damage > 0d)
            {
                Unveil(attacker);
            }
        }

        /// <summary>Seen again, its copy gone with the veil.</summary>
        private void Unveil(Combatant c)
        {
            if (c == null || c.Fighter.Veiled <= 0)
            {
                return;
            }

            c.Fighter.Veiled = 0;
            if (c.Double != null)
            {
                EndDouble(c.Double, false);
            }

            VeilChanged?.Invoke(c);
        }

        private void EndDouble(Combatant copy, bool broke)
        {
            var caster = copy.DoubleOf;
            if (caster == null || caster.Double != copy)
            {
                return;
            }

            caster.Double = null;
            copy.State = CombatantState.Out;
            _doubles.Remove(copy);
            _grid.Vacate(copy.Id);
            foreach (var e in _order)
            {
                if (e.Intent == copy)
                {
                    e.Intent = null; // they know they were deceived
                }
            }

            DoubleEnded?.Invoke(caster, copy, broke);
        }

        /// <summary>A fight begins or ends: no copy, veil or cloud is left, and the scene is told of each that was.</summary>
        private void ClearUnseen()
        {
            for (var i = _doubles.Count - 1; i >= 0; i--)
            {
                EndDouble(_doubles[i], false);
            }

            _doubles.Clear();
            foreach (var c in _order)
            {
                if (c.Fighter.Veiled > 0)
                {
                    c.Fighter.Veiled = 0;
                    VeilChanged?.Invoke(c);
                }
            }

            foreach (var cloud in _screens)
            {
                Haze.Clear(cloud.Body);
            }

            _screens.Clear();
        }
    }
}
