using System.Collections.Generic;
using GameDev.Decisions;
using GameDev.Environment;
using GameDev.Grid;
using GameDev.Input;
using GameDev.Interaction;
using GameDev.World;
using UnityEngine;

namespace GameDev.Player
{
    public enum MovementMode
    {
        FreeExploration = 0,
        TurnBased = 1
    }

    public enum ControlScheme
    {
        Mouse = 0,
        Keyboard = 1
    }

    /// <summary>
    /// Mouse: click A* path, free camera, auto env on path.
    /// Keyboard: WASD move, follow camera, manual E for env/NPC.
    /// Turn-Based: same schemes; click still pathfinds.
    /// </summary>
    public sealed class GridPlayerController : MonoBehaviour
    {
        [SerializeField] private GridMap grid;
        [SerializeField] private GridCoord startCoord = new GridCoord(16, 21);
        [SerializeField] private MovementMode mode = MovementMode.FreeExploration;
        [SerializeField] private ControlScheme controlScheme = ControlScheme.Mouse;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5.5f;
        [SerializeField] private float rotateSpeed = 12f;
        [SerializeField] private float pathArriveDistance = 0.12f;
        [SerializeField] private float turnStepCooldown = 0.16f;

        [Header("Interact")]
        [SerializeField] private float interactRadius = 1.75f;

        private readonly List<GridCoord> _path = new List<GridCoord>(64);
        private int _pathIndex;
        private GridCoord _coord;
        private float _nextTurnStepTime;
        private bool _ready;
        private float _standHeight;
        private GridCoord _lastAutoEnvCoord;
        private bool _hasLastAutoEnv;
        private SimpleFollowCamera _camera;

        public MovementMode Mode => mode;
        public ControlScheme Scheme => controlScheme;
        public GridCoord CurrentCoord => _coord;

        public void PlaceOnGrid(GridMap map, GridCoord start)
        {
            grid = map;
            startCoord = start;
            _coord = start;
            _ready = true;
            ClearPath();
            SnapToCellImmediate();
        }

        public void SetMode(MovementMode newMode)
        {
            mode = newMode;
            ClearPath();
            SyncCoordFromWorld();
            if (mode == MovementMode.TurnBased)
            {
                SnapToCellImmediate();
            }

            InteractionFeedback.Show(
                mode == MovementMode.TurnBased ? "Turn-Based" : "Free Exploration",
                mode == MovementMode.TurnBased
                    ? "Click pathfinds cell route. WASD = keyboard mode. T exits Turn-Based."
                    : "Click = mouse path. WASD = keyboard walk + follow cam. T = Turn-Based.");
        }

        private void Awake()
        {
            GameInputService.EnsureExists();

            if (grid == null)
            {
                grid = FindFirstObjectByType<GridMap>();
            }

            if (grid == null)
            {
                Debug.LogError("[Player] No GridMap found. Rebuild greybox: GameDev → S0 → Rebuild Greybox In Open Scene.");
                return;
            }

            grid.EnsureCells();
            _coord = grid.InBounds(startCoord) ? startCoord : grid.WorldToCoord(transform.position);
            if (!grid.IsWalkable(_coord))
            {
                _coord = startCoord;
            }

            _camera = FindFirstObjectByType<SimpleFollowCamera>();
            _ready = true;
            SnapToCellImmediate();
            ApplyControlScheme(ControlScheme.Mouse, announce: false);
            InteractionFeedback.ShowLegend();
        }

        private void Update()
        {
            if (!_ready || grid == null)
            {
                return;
            }

            // Full world pause: no move, click-path, interact, or mode toggles.
            if (WorldClock.Instance != null && WorldClock.Instance.IsPaused)
            {
                return;
            }

            // Decision Node UI owns input until resolved / Esc.
            if (DecisionNodeSession.IsOpen)
            {
                return;
            }

            var input = GameInputService.Instance;
            if (input == null)
            {
                return;
            }

            if (input.LegendPressed)
            {
                InteractionFeedback.ShowLegend();
            }

            if (input.ToggleTurnBasedPressed)
            {
                SetMode(mode == MovementMode.FreeExploration
                    ? MovementMode.TurnBased
                    : MovementMode.FreeExploration);
            }

            if (input.ToggleSchemePressed)
            {
                ApplyControlScheme(
                    controlScheme == ControlScheme.Mouse ? ControlScheme.Keyboard : ControlScheme.Mouse,
                    announce: true);
            }

            if (controlScheme == ControlScheme.Mouse)
            {
                UpdateMouseScheme(input);
            }
            else
            {
                UpdateKeyboardScheme(input);
            }

            UpdateProximityPrompt();

            if (input.InteractPressed)
            {
                // Manual E only meaningful in keyboard scheme for env; always OK for talk stubs.
                TryInteract(manual: true);
            }
        }

        private void UpdateMouseScheme(GameInputService input)
        {
            // In mouse scheme WASD pans the free camera (handled by SimpleFollowCamera).
            if (input.ClickMovePressed && !IsPointerOverBlockingUi())
            {
                TryClickPath();
            }

            FollowPath(mode == MovementMode.TurnBased);
        }

        private void UpdateKeyboardScheme(GameInputService input)
        {
            // Optional: click still pathfinds while staying in WASD/follow-cam scheme.
            if (input.ClickMovePressed && !IsPointerOverBlockingUi())
            {
                TryClickPath();
            }

            // If a click-path is active, let it finish unless the player steers with WASD.
            var input = ReadWasdMoveInput();
            if (input.sqrMagnitude > 0.001f)
            {
                ClearPath();
            }
            else
            {
                FollowPath(mode == MovementMode.TurnBased);
                return;
            }

            if (mode == MovementMode.TurnBased)
            {
                if (Time.time < _nextTurnStepTime)
                {
                    return;
                }

                var dx = input.x > 0.5f ? 1 : input.x < -0.5f ? -1 : 0;
                var dy = input.z > 0.5f ? 1 : input.z < -0.5f ? -1 : 0;
                if (dx == 0 && dy == 0)
                {
                    return;
                }

                var target = new GridCoord(_coord.x + dx, _coord.y + dy);
                if (!grid.IsWalkable(target))
                {
                    return;
                }

                if (dx != 0 && dy != 0)
                {
                    if (!grid.IsWalkable(new GridCoord(_coord.x + dx, _coord.y)) ||
                        !grid.IsWalkable(new GridCoord(_coord.x, _coord.y + dy)))
                    {
                        return;
                    }
                }

                _coord = target;
                SnapToCellImmediate();
                _nextTurnStepTime = Time.time + turnStepCooldown;
                return;
            }

            var worldDir = CameraRelativeDirection(input);
            TrySteer(worldDir, moveSpeed * Time.deltaTime);
        }

        private void ApplyControlScheme(ControlScheme scheme, bool announce)
        {
            controlScheme = scheme;
            if (_camera == null)
            {
                _camera = FindFirstObjectByType<SimpleFollowCamera>();
            }

            if (_camera != null)
            {
                _camera.SetRigMode(scheme == ControlScheme.Keyboard ? CameraRigMode.Follow : CameraRigMode.Free);
            }

            if (scheme == ControlScheme.Keyboard)
            {
                ClearPath();
            }

            if (announce)
            {
                InteractionFeedback.Show(
                    scheme == ControlScheme.Mouse ? "Mouse control" : "WASD control",
                    scheme == ControlScheme.Mouse
                        ? "LMB pathfinds around obstacles (auto env). WASD pans free camera. Tab → WASD walk mode."
                        : "WASD walks; camera follows. E = manual talk/env. LMB can still pathfind. Tab → mouse/free cam.",
                    4f);
            }
        }

        private void TryClickPath()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            var mouse = GameInputService.Instance != null
                ? GameInputService.Instance.MousePosition
                : Vector2.zero;
            var ray = cam.ScreenPointToRay(mouse);
            var plane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
            if (!plane.Raycast(ray, out var enter))
            {
                return;
            }

            var point = ray.GetPoint(enter);
            var goal = grid.WorldToCoord(point);
            SyncCoordFromWorld();

            if (!GridPathfinder.TryFindPath(grid, _coord, goal, _path))
            {
                InteractionFeedback.Show("No path", "That spot is blocked or unreachable.");
                ClearPath();
                return;
            }

            // Skip the cell we already stand on.
            _pathIndex = 0;
            if (_path.Count > 0 && _path[0].Equals(_coord))
            {
                _pathIndex = 1;
            }

            _hasLastAutoEnv = false;
            if (_pathIndex >= _path.Count)
            {
                ClearPath();
            }
        }

        private void FollowPath(bool turnBasedSteps)
        {
            if (_pathIndex >= _path.Count)
            {
                return;
            }

            var nextCoord = _path[_pathIndex];
            var nextWorld = CellWorld(nextCoord);

            if (turnBasedSteps)
            {
                if (Time.time < _nextTurnStepTime)
                {
                    return;
                }

                _coord = nextCoord;
                SnapToCellImmediate();
                MaybeAutoEnvOnCell(nextCoord);
                _pathIndex++;
                _nextTurnStepTime = Time.time + turnStepCooldown;
                if (_pathIndex >= _path.Count)
                {
                    ClearPath();
                }

                return;
            }

            var to = nextWorld - transform.position;
            to.y = 0f;
            if (to.magnitude <= pathArriveDistance)
            {
                _coord = nextCoord;
                ApplyWorldPosition(nextWorld);
                MaybeAutoEnvOnCell(nextCoord);
                _pathIndex++;
                if (_pathIndex >= _path.Count)
                {
                    ClearPath();
                }

                return;
            }

            TrySteer(to.normalized, moveSpeed * Time.deltaTime);
            SyncCoordFromWorld();
        }

        private void MaybeAutoEnvOnCell(GridCoord coord)
        {
            if (_hasLastAutoEnv && _lastAutoEnvCoord.Equals(coord))
            {
                return;
            }

            if (!grid.TryGetCell(coord, out var cell))
            {
                return;
            }

            var special = GridCellFlags.Vaultable | GridCellFlags.Window | GridCellFlags.Climbable | GridCellFlags.Stairs;
            if ((cell.flags & special) == 0)
            {
                return;
            }

            var env = FindEnvNear(CellWorld(coord), 1.25f);
            if (env == null)
            {
                return;
            }

            env.PerformForPath();
            _lastAutoEnvCoord = coord;
            _hasLastAutoEnv = true;
        }

        private void ClearPath()
        {
            _path.Clear();
            _pathIndex = 0;
        }

        private Vector3 CellWorld(GridCoord coord)
        {
            var world = grid.CoordToWorldCenter(coord);
            SyncStandHeight(world);
            world.y = _standHeight + 0.1f;
            return world;
        }

        private static Vector3 ReadWasdMoveInput()
        {
            var raw = GameInputService.Instance != null
                ? GameInputService.Instance.MoveVector
                : Vector2.zero;
            if (raw.sqrMagnitude > 1f)
            {
                raw.Normalize();
            }

            return new Vector3(raw.x, 0f, raw.y);
        }

        private static Vector3 CameraRelativeDirection(Vector3 input)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                return input.normalized;
            }

            var forward = cam.transform.forward;
            forward.y = 0f;
            forward.Normalize();
            var right = cam.transform.right;
            right.y = 0f;
            right.Normalize();
            var dir = forward * input.z + right * input.x;
            return dir.sqrMagnitude > 0.001f ? dir.normalized : Vector3.zero;
        }

        private void TrySteer(Vector3 direction, float distance)
        {
            if (direction.sqrMagnitude < 0.0001f || distance <= 0f)
            {
                return;
            }

            var next = transform.position + direction * distance;
            if (!CanStandAt(next))
            {
                var onlyX = transform.position + new Vector3(direction.x, 0f, 0f) * distance;
                var onlyZ = transform.position + new Vector3(0f, 0f, direction.z) * distance;
                if (CanStandAt(onlyX))
                {
                    next = onlyX;
                }
                else if (CanStandAt(onlyZ))
                {
                    next = onlyZ;
                }
                else
                {
                    return;
                }
            }

            ApplyWorldPosition(next);
            if (direction.sqrMagnitude > 0.0001f)
            {
                var targetRot = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
            }
        }

        private bool CanStandAt(Vector3 world)
        {
            var coord = grid.WorldToCoord(world);
            if (!grid.IsWalkable(coord))
            {
                return false;
            }

            var center = grid.CoordToWorldCenter(coord);
            var local = world - center;
            var half = grid.CellSize * 0.45f;
            if (Mathf.Abs(local.x) > half)
            {
                var side = new GridCoord(coord.x + (local.x > 0f ? 1 : -1), coord.y);
                if (!grid.IsWalkable(side))
                {
                    return false;
                }
            }

            if (Mathf.Abs(local.z) > half)
            {
                var side = new GridCoord(coord.x, coord.y + (local.z > 0f ? 1 : -1));
                if (!grid.IsWalkable(side))
                {
                    return false;
                }
            }

            return true;
        }

        private void ApplyWorldPosition(Vector3 world)
        {
            SyncStandHeight(world);
            world.y = _standHeight + 0.1f;
            transform.position = world;
            SyncCoordFromWorld();
        }

        private void SnapToCellImmediate()
        {
            transform.position = CellWorld(_coord);
        }

        private void SyncCoordFromWorld()
        {
            _coord = grid.WorldToCoord(transform.position);
        }

        private void SyncStandHeight(Vector3 world)
        {
            var coord = grid.WorldToCoord(world);
            _standHeight = grid.TryGetCell(coord, out var cell) ? cell.height * grid.CellSize : 0f;
        }

        private static bool IsPointerOverBlockingUi() => false;

        private void UpdateProximityPrompt()
        {
            // In mouse path mode, don't nag E for env — path uses them automatically.
            if (TryFindNearestInteractable(out var interact, out _))
            {
                InteractionFeedback.SetProximityPrompt($"[E] {interact.Prompt}");
                return;
            }

            if (controlScheme == ControlScheme.Keyboard && TryFindNearestEnvAction(out var env, out _))
            {
                InteractionFeedback.SetProximityPrompt($"[E] {env.Prompt}");
                return;
            }

            InteractionFeedback.ClearProximityPrompt();
        }

        private void TryInteract(bool manual)
        {
            if (TryFindNearestInteractable(out var bestInteract, out _))
            {
                bestInteract.Interact();
                return;
            }

            if (TryFindNearestEnvAction(out var bestEnv, out _))
            {
                // Manual only in keyboard scheme (or if user still presses E in mouse mode near env).
                if (manual && controlScheme == ControlScheme.Keyboard)
                {
                    bestEnv.TryPerform();
                    return;
                }

                if (manual && controlScheme == ControlScheme.Mouse)
                {
                    InteractionFeedback.Show(
                        "Mouse mode",
                        "Environment on a click-path is used automatically. Switch to WASD (or Tab) for manual E checks.");
                    return;
                }
            }

            InteractionFeedback.Show(
                "Nothing nearby",
                "Talk stubs are brown blocks. Press H for legend.");
        }

        private bool TryFindNearestInteractable(out SimpleInteractable best, out float bestDist)
        {
            best = null;
            bestDist = float.MaxValue;
            var interactables = FindObjectsByType<SimpleInteractable>(FindObjectsSortMode.None);
            foreach (var item in interactables)
            {
                var d = HorizontalDistance(transform.position, item.transform.position);
                if (d <= interactRadius && d < bestDist)
                {
                    bestDist = d;
                    best = item;
                }
            }

            return best != null;
        }

        private bool TryFindNearestEnvAction(out EnvironmentActionMarker best, out float bestDist)
        {
            best = FindEnvNear(transform.position, interactRadius);
            bestDist = best != null ? HorizontalDistance(transform.position, best.transform.position) : float.MaxValue;
            return best != null;
        }

        private static EnvironmentActionMarker FindEnvNear(Vector3 world, float radius)
        {
            EnvironmentActionMarker best = null;
            var bestDist = float.MaxValue;
            var envActions = FindObjectsByType<EnvironmentActionMarker>(FindObjectsSortMode.None);
            foreach (var item in envActions)
            {
                var d = HorizontalDistance(world, item.transform.position);
                if (d <= radius && d < bestDist)
                {
                    bestDist = d;
                    best = item;
                }
            }

            return best;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
