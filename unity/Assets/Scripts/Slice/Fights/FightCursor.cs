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

        /// <summary>Reads what the cursor points at for the player's turn, draws the path, and sets the cursor's shape.</summary>
        public void Point(Combatant player, Camera camera, Vector2 mouse)
        {
            Target = null;
            Cell = null;
            Cost = 0;
            Steps = 0;
            Affordable = false;
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
                Cost = Steps < 0 ? 0 : Steps + player.Fighter.Weapon.AttackAp;
                Affordable = Steps >= 0 && Cost <= _turns.ApLeft;
                Forecast = _rules.Forecast(player.Fighter, Target.Fighter, _turns.ContextFor(from, Target));
                Show(Affordable ? Shape.Attack : Shape.AttackBlocked, Affordable ? Steps : Math.Min(Math.Max(Steps, 0), _turns.ApLeft));
                return;
            }

            if (!CellAt(camera, mouse, out var cell) || cell == player.Cell)
            {
                Show(Shape.Basic, 0);
                return;
            }

            Cell = cell;
            Steps = _grid.FindPath(player.Cell, cell, _path);
            Cost = Math.Max(Steps, 0);
            Affordable = Steps > 0 && Steps <= _turns.ApLeft;
            Show(Affordable ? Shape.Move : Shape.Blocked, Math.Min(Math.Max(Steps, 0), _turns.ApLeft));
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

            if (best != null || !CellAt(camera, mouse, out var cell))
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
        /// in the way is passed through; else where the ray meets the level of the grid's centre.
        /// </summary>
        private bool CellAt(Camera camera, Vector2 mouse, out CombatCell cell)
        {
            var ray = camera.ScreenPointToRay(mouse);
            var count = Physics.RaycastNonAlloc(ray, _hits, RayLength, CombatGridReader.Mask, QueryTriggerInteraction.Ignore);
            var best = float.MaxValue;
            var found = false;
            cell = default;
            for (var i = 0; i < count; i++)
            {
                if (_hits[i].distance < best && _grid.TryCellAt(_hits[i].point + (Vector3.up * 0.05f), out var at) && _grid.IsReachable(at))
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
