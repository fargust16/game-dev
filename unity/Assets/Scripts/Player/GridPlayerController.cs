using GameDev.Environment;
using GameDev.Grid;
using GameDev.Interaction;
using UnityEngine;

namespace GameDev.Player
{
    public enum MovementMode
    {
        FreeExploration = 0,
        TurnBased = 1
    }

    /// <summary>
    /// Free exploration: click-to-move + arrow keys (WASD is camera pan, BG3-style).
    /// Turn-Based: discrete cell steps with arrows.
    /// Design: 10 Turn Action Economy, 11.2 Milestone S0.
    /// </summary>
    public sealed class GridPlayerController : MonoBehaviour
    {
        [SerializeField] private GridMap grid;
        [SerializeField] private GridCoord startCoord = new GridCoord(18, 25);
        [SerializeField] private MovementMode mode = MovementMode.FreeExploration;

        [Header("Free exploration")]
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float rotateSpeed = 12f;
        [SerializeField] private float clickArriveDistance = 0.15f;

        [Header("Turn-based (stub)")]
        [SerializeField] private float turnStepCooldown = 0.18f;

        [Header("Interact")]
        [SerializeField] private float interactRadius = 1.75f;

        private GridCoord _coord;
        private float _nextTurnStepTime;
        private bool _ready;
        private bool _hasClickTarget;
        private Vector3 _clickTarget;
        private float _standHeight;

        public MovementMode Mode => mode;
        public GridCoord CurrentCoord => _coord;

        public void PlaceOnGrid(GridMap map, GridCoord start)
        {
            grid = map;
            startCoord = start;
            _coord = start;
            _ready = true;
            SnapToCellImmediate();
        }

        public void SetMode(MovementMode newMode)
        {
            mode = newMode;
            _hasClickTarget = false;
            SyncCoordFromWorld();
            if (mode == MovementMode.TurnBased)
            {
                SnapToCellImmediate();
            }

            InteractionFeedback.Show(
                mode == MovementMode.TurnBased ? "Turn-Based" : "Free Exploration",
                mode == MovementMode.TurnBased
                    ? "Cell steps with Arrow keys. Press T to return to free exploration."
                    : "LMB click-to-move, Arrows walk. WASD pans camera. Press T for Turn-Based.");
        }

        private void Awake()
        {
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

            _ready = true;
            SnapToCellImmediate();
            InteractionFeedback.Show(
                "Lower Veil — S0",
                "LMB move · Arrows walk · WASD camera · Q/E rotate · Wheel zoom · RMB orbit · F focus · E interact · T turn-based");
        }

        private void Update()
        {
            if (!_ready || grid == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.T))
            {
                SetMode(mode == MovementMode.FreeExploration
                    ? MovementMode.TurnBased
                    : MovementMode.FreeExploration);
            }

            if (mode == MovementMode.FreeExploration)
            {
                UpdateFreeExploration();
            }
            else
            {
                UpdateTurnBased();
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                TryInteract();
            }
        }

        private void UpdateFreeExploration()
        {
            // RMB is camera rotate — only LMB sets move target.
            if (Input.GetMouseButtonDown(0) && !IsPointerOverBlockingUi())
            {
                TrySetClickTarget();
            }

            var input = ReadArrowMoveInput();
            if (input.sqrMagnitude > 0.001f)
            {
                _hasClickTarget = false;
                var worldDir = CameraRelativeDirection(input);
                TrySteer(worldDir, moveSpeed * Time.deltaTime);
                return;
            }

            if (!_hasClickTarget)
            {
                return;
            }

            var toTarget = _clickTarget - transform.position;
            toTarget.y = 0f;
            if (toTarget.magnitude <= clickArriveDistance)
            {
                _hasClickTarget = false;
                return;
            }

            TrySteer(toTarget.normalized, moveSpeed * Time.deltaTime);
        }

        private void UpdateTurnBased()
        {
            _hasClickTarget = false;
            if (Time.time < _nextTurnStepTime)
            {
                return;
            }

            var input = ReadArrowMoveInput();
            if (input.sqrMagnitude < 0.001f)
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
        }

        private static Vector3 ReadArrowMoveInput()
        {
            var x = 0f;
            var y = 0f;
            if (Input.GetKey(KeyCode.RightArrow)) x += 1f;
            if (Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
            if (Input.GetKey(KeyCode.UpArrow)) y += 1f;
            if (Input.GetKey(KeyCode.DownArrow)) y -= 1f;
            var raw = new Vector2(x, y);
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

        private void TrySetClickTarget()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            var ray = cam.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
            if (!plane.Raycast(ray, out var enter))
            {
                return;
            }

            var point = ray.GetPoint(enter);
            if (!CanStandAt(point))
            {
                InteractionFeedback.Show("Blocked", "That ground is not walkable.");
                return;
            }

            _clickTarget = point;
            _hasClickTarget = true;
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
            var world = grid.CoordToWorldCenter(_coord);
            SyncStandHeight(world);
            world.y = _standHeight + 0.1f;
            transform.position = world;
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

        private static bool IsPointerOverBlockingUi()
        {
            return false;
        }

        private void TryInteract()
        {
            var interactables = FindObjectsByType<SimpleInteractable>(FindObjectsSortMode.None);
            SimpleInteractable bestInteract = null;
            var bestDist = float.MaxValue;
            foreach (var item in interactables)
            {
                var d = HorizontalDistance(transform.position, item.transform.position);
                if (d <= interactRadius && d < bestDist)
                {
                    bestDist = d;
                    bestInteract = item;
                }
            }

            if (bestInteract != null)
            {
                bestInteract.Interact();
                return;
            }

            var envActions = FindObjectsByType<EnvironmentActionMarker>(FindObjectsSortMode.None);
            EnvironmentActionMarker bestEnv = null;
            bestDist = float.MaxValue;
            foreach (var item in envActions)
            {
                var d = HorizontalDistance(transform.position, item.transform.position);
                if (d <= interactRadius && d < bestDist)
                {
                    bestDist = d;
                    bestEnv = item;
                }
            }

            if (bestEnv != null)
            {
                bestEnv.TryPerform();
                return;
            }

            InteractionFeedback.Show(
                "Nothing nearby",
                "Walk to Soot & Bell / notice board / orange POI, then press E.");
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
