using System;
using System.Collections.Generic;
using GameDev.Confront.Grid;
using GameDev.Confront.Rules;
using GameDev.Confront.Turns;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// The player's turn by the cursor (S3a.5): over a cell it shows the path and its AP before the click, over an enemy
    /// the walk to it and the blow; a click walks or strikes. The cursor's shape is Basic RPG Cursors' (asset map §5):
    /// the four arrows to walk, the sword to strike, and their red-marked forms when the AP do not reach.
    /// </summary>
    public sealed class FightCursor
    {
        public enum Shape
        {
            Basic,
            Move,
            Attack,
            Blocked,
            AttackBlocked,
        }

        // Authoring defaults, S3a.5: how far from a body's line, feet to head, the cursor still points at it, in pixels;
        // the person's height for that line, m; and how far the ray looks, m.
        private const float BodyPixels = 26f;
        private const float BodyHeight = 1.7f;
        private const float RayLength = 300f;

        private readonly ITurnEconomy _turns;
        private readonly ICombatGrid _grid;
        private readonly FightRules _rules;
        private readonly FightPathView _view;
        private readonly Texture2D[] _textures;
        private readonly Vector2[] _hotspots;
        private readonly List<CombatCell> _path = new List<CombatCell>();
        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private Shape _shown = (Shape)(-1);

        /// <param name="textures">The cursor for each <see cref="Shape"/>, in its order; a missing one leaves the system's.</param>
        /// <param name="hotspots">Each cursor's point, in pixels from its top left.</param>
        public FightCursor(ITurnEconomy turns, ICombatGrid grid, FightRules rules, FightPathView view, Texture2D[] textures, Vector2[] hotspots)
        {
            _turns = turns;
            _grid = grid;
            _rules = rules;
            _view = view;
            _textures = textures;
            _hotspots = hotspots;
        }

        public Shape Current { get; private set; }

        /// <summary>The enemy under the cursor, or null.</summary>
        public Combatant Target { get; private set; }

        /// <summary>The cell under the cursor, when it points at no enemy.</summary>
        public CombatCell? Cell { get; private set; }

        /// <summary>The walk and the blow's AP together, or the walk's.</summary>
        public int Cost { get; private set; }

        /// <summary>The steps of the walk before the blow, or of the walk.</summary>
        public int Steps { get; private set; }

        public bool Affordable { get; private set; }

        /// <summary>What the blow is likely to do from where it would be struck; meaningful while <see cref="Target"/> is set.</summary>
        public StrikeForecast Forecast { get; private set; }

        public IReadOnlyList<CombatCell> Path => _path;

        /// <summary>The walk shown leaves an enemy's side and draws its Reaction Attack (S3a.6).</summary>
        public bool Provokes { get; private set; }

        /// <summary>The cell pointed at lies across a tagged edge, and an Environment Action is the way there (S3a.7).</summary>
        public bool HasOption { get; private set; }

        public TraverseOption Option { get; private set; }

        /// <summary>Reads what the cursor points at for the player's turn, draws the path, and sets the cursor's shape.</summary>
        public void Point(Combatant player, Camera camera, Vector2 mouse)
        {
            Target = null;
            Cell = null;
            Cost = 0;
            Steps = 0;
            Affordable = false;
            Provokes = false;
            HasOption = false;
            _path.Clear();
            if (player == null || camera == null || !_turns.IsRunning || _turns.Acting != player)
            {
                Clear();
                return;
            }

            Target = EnemyAt(player, camera, mouse);
            if (Target != null)
            {
                Steps = _turns.PathToStrike(player, Target, _path);
                var from = Steps > 0 ? _path[_path.Count - 1] : player.Cell;
                Cost = Steps < 0 ? 0 : _turns.WalkAp(player, Steps) + player.Fighter.Weapon.AttackAp;
                Affordable = Steps >= 0 && Cost <= _turns.ApLeft;
                Forecast = _rules.Forecast(player.Fighter, Target.Fighter, _turns.ContextFor(player, from, Target));
                Provokes = Steps > 0 && Leaves(player);
                Show(Affordable ? Shape.Attack : Shape.AttackBlocked, Affordable ? Steps : Math.Min(Math.Max(Steps, 0), _turns.ApLeft));
                return;
            }

            if (!CellAt(camera, mouse, player.Cell, out var cell) || cell == player.Cell)
            {
                Show(Shape.Basic, 0);
                return;
            }

            Cell = cell;
            Steps = _grid.FindPath(player.Cell, cell, _path);

            // S3a.7: across a tagged edge beside the player, the Environment Action, unless a walk round costs no more.
            if (_turns.TraverseFrom(player, cell, out var option) && (Steps < 0 || Steps > option.Ap))
            {
                _path.Clear();
                HasOption = true;
                Option = option;
                Steps = 1;
                Cost = option.Ap;
                Affordable = Cost <= _turns.ApLeft && !player.Fighter.LegBroken;
                Provokes = option.Kind != EnvironmentActionKind.KickBreach && Leaves(player);
                Show(Affordable ? Shape.Move : Shape.Blocked, 0);
                return;
            }

            if (player.Fighter.LegBroken)
            {
                _path.Clear();
                Steps = -1; // a broken leg takes no step (10.2)
            }
            Cost = _turns.WalkAp(player, Math.Max(Steps, 0)); // crouching: 3 AP for two cells (10.3 §8)
            Affordable = Steps > 0 && Cost <= _turns.ApLeft;
            Provokes = Steps > 0 && Leaves(player);
            Show(Affordable ? Shape.Move : Shape.Blocked, Math.Min(Math.Max(Steps, 0), _turns.ApLeft));
        }

        /// <summary>
        /// Whether the walk shown leaves a cell at the side of an enemy holding a melee weapon, which draws its Reaction
        /// Attack (10.2 §5b Position): the player's own cell and every cell on the way but the last.
        /// </summary>
        private bool Leaves(Combatant player)
        {
            var from = player.Cell;
            for (var i = 0; i < Math.Max(_path.Count, HasOption ? 1 : 0); i++)
            {
                foreach (var e in _turns.Order)
                {
                    var weapon = e.Fighter.Weapon;
                    if (e.Standing && e.Side != player.Side && weapon != null && !weapon.Ranged && e.Fighter.LostTurns == 0
                        && Math.Abs(e.Cell.X - from.X) + Math.Abs(e.Cell.Z - from.Z) == 1 && _grid.CanStep(from, e.Cell))
                    {
                        return true;
                    }
                }

                if (i < _path.Count)
                {
                    from = _path[i];
                }
            }

            return false;
        }

        /// <summary>Walks to the cell, or walks to the enemy and strikes it, as last pointed at; false when nothing could be done.</summary>
        public bool Click(Combatant player)
        {
            if (!Affordable || player == null || _turns.Acting != player)
            {
                return false;
            }

            if (Target != null)
            {
                if (Steps > 0 && !_turns.SubmitIntent(TurnIntent.MoveTo(_path[_path.Count - 1])))
                {
                    return false;
                }

                return _turns.SubmitIntent(TurnIntent.Attack(Target.Id));
            }

            if (HasOption && Cell.HasValue)
            {
                return _turns.SubmitIntent(TurnIntent.Traverse(Cell.Value));
            }

            return Cell.HasValue && _turns.SubmitIntent(TurnIntent.MoveTo(Cell.Value));
        }

        /// <summary>No path, and the plain cursor.</summary>
        public void Clear()
        {
            _view.Hide();
            Set(Shape.Basic);
        }

        /// <summary>The system's cursor again, as the fight ends.</summary>
        public void Release()
        {
            _view.Hide();
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            _shown = (Shape)(-1);
            Current = Shape.Basic;
        }

        private void Show(Shape shape, int affordable)
        {
            if (_path.Count > 0)
            {
                _view.Show(_grid, _path, affordable);
            }
            else
            {
                _view.Hide();
            }

            Set(shape);
        }

        private void Set(Shape shape)
        {
            Current = shape;
            if (shape == _shown)
            {
                return;
            }

            _shown = shape;
            var i = (int)shape;
            var texture = _textures != null && i < _textures.Length ? _textures[i] : null;
            Cursor.SetCursor(texture, texture != null && _hotspots != null && i < _hotspots.Length ? _hotspots[i] : Vector2.zero, CursorMode.Auto);
        }

        /// <summary>
        /// The standing enemy the cursor points at: one whose body, feet to head, passes within a few pixels of it, the
        /// nearest to the camera first; else the one standing in the cell under it.
        /// </summary>
        private Combatant EnemyAt(Combatant player, Camera camera, Vector2 mouse)
        {
            Combatant best = null;
            var nearest = float.MaxValue;
            foreach (var c in _turns.Order)
            {
                if (!c.Standing || c.Side == player.Side)
                {
                    continue;
                }

                var feet = camera.WorldToScreenPoint(c.Position);
                var head = camera.WorldToScreenPoint(c.Position + (Vector3.up * BodyHeight));
                if (feet.z <= 0f || head.z <= 0f)
                {
                    continue;
                }

                if (DistanceToSegment(mouse, feet, head) <= BodyPixels && feet.z < nearest)
                {
                    nearest = feet.z;
                    best = c;
                }
            }

            if (best != null || !CellAt(camera, mouse, player.Cell, out var cell))
            {
                return best;
            }

            var occupant = _grid.OccupantAt(cell);
            foreach (var c in _turns.Order)
            {
                if (c.Id == occupant && c.Standing && c.Side != player.Side)
                {
                    return c;
                }
            }

            return null;
        }

        /// <summary>
        /// The reachable cell under the cursor: the nearest surface along the ray that is one, so a hidden house's roof
        /// in the way is passed through; else where the ray meets the level of the grid's centre. A walkable cell at a
        /// side of the player's counts too, for an Environment Action across the edge between them (S3a.7). The ray passes
        /// a tagged block's side, so a low wall seen from behind the player does not hide the cell past it.
        /// </summary>
        private bool CellAt(Camera camera, Vector2 mouse, CombatCell from, out CombatCell cell)
        {
            var ray = camera.ScreenPointToRay(mouse);
            var count = Physics.RaycastNonAlloc(ray, _hits, RayLength, CombatGridReader.Mask, QueryTriggerInteraction.Ignore);
            var best = float.MaxValue;
            var found = false;
            cell = default;
            for (var i = 0; i < count; i++)
            {
                if (_hits[i].normal.y < 0.5f && _hits[i].collider.GetComponentInParent<IEnvironmentAction>() != null)
                {
                    continue;
                }

                if (_hits[i].distance < best && _grid.TryCellAt(_hits[i].point + (Vector3.up * 0.05f), out var at)
                    && (_grid.IsReachable(at) || Math.Abs(at.X - from.X) + Math.Abs(at.Z - from.Z) == 1))
                {
                    best = _hits[i].distance;
                    cell = at;
                    found = true;
                }
            }

            if (found)
            {
                return true;
            }

            var level = new Plane(Vector3.up, _grid.Center(_grid.Origin));
            return level.Raycast(ray, out var along) && _grid.TryCellAt(ray.GetPoint(along) + (Vector3.up * 0.05f), out cell) && _grid.IsReachable(cell);
        }

        private static float DistanceToSegment(Vector2 p, Vector3 a, Vector3 b)
        {
            var a2 = new Vector2(a.x, a.y);
            var ab = new Vector2(b.x, b.y) - a2;
            var t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p - a2, ab) / ab.sqrMagnitude) : 0f;
            return Vector2.Distance(p, a2 + (ab * t));
        }
    }
}
