using System;
using System.Collections.Generic;
using GameDev.Confront.Grid;
using GameDev.Confront.Rules;
using UnityEngine;

namespace GameDev.Confront.Turns
{
    /// <summary>
    /// CR5, the abilities aimed at a place (05.1 *Abilities of the slice*, 10.1 *Shot Line*, 10.2 §5f): a point needs a
    /// cell within the ability's range with a line free of Solid Obstacles from the caster. Пелена raises a cloud of
    /// haze over the area, a Partial Obstacle for its rounds; Гнёт strikes everyone in the area with a clear line from
    /// the point, the caster's own people too, with no roll and no critical hit; Рывок puts its caster in a free cell
    /// behind an enemy in sight, with no walk and so no Reaction Attack.
    /// </summary>
    public sealed partial class TurnEconomy
    {
        // Authoring defaults: how far, m, a person's cell may stand above or below an area's point and still be in it,
        // as on one floor (the Shot Line's band); and what a cloud leaves short of its cells' width, m, so that a line
        // along its edge is not taken for one through it.
        private const float AreaHeightBand = 1.5f;
        private const float HazeMargin = 0.02f;

        private readonly List<Cloud> _screens = new List<Cloud>();
        private readonly List<Combatant> _pressed = new List<Combatant>();

        /// <summary>
        /// Raised as an ability takes its place, before what it does is told: Пелена's and Гнёт's point, Рывок's cell
        /// behind the enemy, Двойник's copy where its caster stood.
        /// </summary>
        public event Action<Combatant, FightAbility, CombatCell> AbilityAt;

        /// <summary>05.1 Пелена: the clouds standing now.</summary>
        public IReadOnlyList<Cloud> Clouds => _screens;

        /// <summary>Whether an ability is aimed at a cell, so the player picks one after the ability.</summary>
        public static bool NeedsPoint(FightAbility ability) => ability.Effect == AbilityEffect.Screen || ability.Effect == AbilityEffect.Press;

        public bool CanAimAt(Combatant c, int index, CombatCell cell)
        {
            var ability = Prepared(c, index);
            return ability != null && AimsAt(c, ability, cell);
        }

        /// <summary>The walkable cells of <paramref name="ability"/>'s area round <paramref name="center"/>, the centre first.</summary>
        public void AreaOf(CombatCell center, FightAbility ability, List<CombatCell> into)
        {
            into.Clear();
            into.Add(center);
            var half = Math.Max(1, ability.Area) / 2;
            var at = _grid.Center(center);
            var size = _grid.CellSize;
            for (var dx = -half; dx <= half; dx++)
            {
                for (var dz = -half; dz <= half; dz++)
                {
                    if ((dx != 0 || dz != 0) && _grid.TryCellAt(new Vector3(at.x + (dx * size), at.y, at.z + (dz * size)), out var cell)
                        && InArea(cell, center, half))
                    {
                        into.Add(cell);
                    }
                }
            }
        }

        /// <summary>05.1 Гнёт: how many enemies of <paramref name="c"/> and of its own people a cast at <paramref name="center"/> would strike.</summary>
        public (int Foes, int Friends) PressCount(Combatant c, FightAbility ability, CombatCell center)
        {
            Pressed(c, ability, center);
            var foes = 0;
            foreach (var e in _pressed)
            {
                foes += e.Side != c.Side ? 1 : 0;
            }

            return (foes, _pressed.Count - foes);
        }

        /// <summary>A cell in the ability's range with a line free of Solid Obstacles from the caster (10.1 *Shot Line*).</summary>
        private bool AimsAt(Combatant c, FightAbility ability, CombatCell cell) =>
            NeedsPoint(ability) && _grid.IsWalkable(cell) && Distance(c.Cell, cell) <= ability.Range && (cell == c.Cell || _grid.Shot(c.Cell, cell).Clear);

        private bool InArea(CombatCell cell, CombatCell center, int half) =>
            Math.Abs(cell.X - center.X) <= half && Math.Abs(cell.Z - center.Z) <= half
            && Math.Abs(_grid.Center(cell).y - _grid.Center(center).y) <= AreaHeightBand;

        /// <summary>05.1 Пелена: a cloud over the area for the ability's rounds, a Partial Obstacle a shot from inside meets too.</summary>
        private void RaiseScreen(Combatant c, FightAbility ability, CombatCell center)
        {
            var size = Math.Max(1, ability.Area);
            _screens.Add(new Cloud(c, center, Math.Max(1, ability.Rounds), Haze.Raise(_grid.Center(center), (size * _grid.CellSize) - HazeMargin)));
            AbilityAt?.Invoke(c, ability, center);
        }

        /// <summary>A round has ended: each cloud has one less, and those with none left are gone.</summary>
        private void FadeScreens()
        {
            for (var i = _screens.Count - 1; i >= 0; i--)
            {
                var screen = _screens[i];
                screen.RoundsLeft--;
                if (screen.RoundsLeft <= 0)
                {
                    Haze.Clear(screen.Body);
                    _screens.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 05.1 Гнёт: everyone standing in the area with a clear line from the point, a copy too, takes its harm; each is
        /// told as the ability used on it. True when it struck anyone.
        /// </summary>
        private bool Press(Combatant c, FightAbility ability, CombatCell center)
        {
            AbilityAt?.Invoke(c, ability, center);
            Pressed(c, ability, center);
            var harmed = false;
            for (var i = 0; i < _pressed.Count; i++)
            {
                var e = _pressed[i];
                var result = _rules.Press(e.Fighter, ability, _rolls);
                harmed |= result.Damage > 0d;
                if (e.Fighter.Down)
                {
                    _grid.Vacate(e.Id);
                }

                AbilityUsed?.Invoke(c, e, ability, true, result);
                if (e.DoubleOf != null)
                {
                    EndDouble(e, true);
                }
            }

            if (harmed)
            {
                Unveil(c);
            }

            return _pressed.Count > 0;
        }

        /// <summary>Fills who a Гнёт at <paramref name="center"/> strikes: everyone standing in its area but its caster, with a clear line from the point.</summary>
        private void Pressed(Combatant c, FightAbility ability, CombatCell center)
        {
            _pressed.Clear();
            var half = Math.Max(1, ability.Area) / 2;
            foreach (var e in _order)
            {
                if (e != c && e.Standing && InArea(e.Cell, center, half) && (e.Cell == center || _grid.Shot(center, e.Cell).Clear))
                {
                    _pressed.Add(e);
                }
            }

            foreach (var d in _doubles)
            {
                if (d.Standing && InArea(d.Cell, center, half) && (d.Cell == center || _grid.Shot(center, d.Cell).Clear))
                {
                    _pressed.Add(d);
                }
            }
        }

        /// <summary>
        /// 05.1 Рывок: the free cell behind <paramref name="target"/> as <paramref name="c"/> sees it: straight behind, then
        /// the two beside that one that also touch the target, each a step from the target with nothing between.
        /// </summary>
        private bool Behind(Combatant c, Combatant target, out CombatCell cell)
        {
            cell = default;
            var dx = Math.Sign(target.Cell.X - c.Cell.X);
            var dz = Math.Sign(target.Cell.Z - c.Cell.Z);
            if (dx == 0 && dz == 0)
            {
                return false;
            }

            var at = _grid.Center(target.Cell);
            var size = _grid.CellSize;
            for (var k = 0; k < 3; k++)
            {
                int ox, oz;
                if (dx != 0 && dz != 0)
                {
                    ox = k == 2 ? 0 : dx;
                    oz = k == 1 ? 0 : dz;
                }
                else if (dx != 0)
                {
                    ox = dx;
                    oz = k == 0 ? 0 : (k == 1 ? 1 : -1);
                }
                else
                {
                    oz = dz;
                    ox = k == 0 ? 0 : (k == 1 ? 1 : -1);
                }

                if (_grid.TryCellAt(new Vector3(at.x + (ox * size), at.y, at.z + (oz * size)), out var behind)
                    && behind != c.Cell && _grid.OccupantAt(behind) < 0 && _grid.CanStep(target.Cell, behind))
                {
                    cell = behind;
                    return true;
                }
            }

            return false;
        }

        /// <summary>05.1 Рывок: at once into the cell behind the enemy, facing it; no walk, so no Reaction Attack.</summary>
        private void Dash(Combatant c, FightAbility ability, Combatant target)
        {
            if (!Behind(c, target, out var cell) || !_grid.Occupy(c.Id, cell))
            {
                return;
            }

            c.Cell = cell;
            c.Position = _grid.Center(cell);
            Face(c, target.Cell);
            Settle(c);
            Spotted(c);
            AbilityAt?.Invoke(c, ability, cell);
        }

        /// <summary>05.1 Пелена: one cloud of haze, its caster, its centre, the rounds it has left, and its Partial Obstacle.</summary>
        public sealed class Cloud
        {
            internal Cloud(Combatant owner, CombatCell center, int rounds, GameObject body)
            {
                Owner = owner;
                Center = center;
                RoundsLeft = rounds;
                Body = body;
            }

            public Combatant Owner { get; }

            public CombatCell Center { get; }

            public int RoundsLeft { get; internal set; }

            /// <summary>The cloud's Partial Obstacle in the scene, which the scene may dress; it goes with the cloud.</summary>
            public GameObject Body { get; }
        }
    }
}
