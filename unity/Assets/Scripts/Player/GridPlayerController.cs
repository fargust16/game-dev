using GameDev.Environment;
using GameDev.Grid;
using GameDev.Interaction;
using UnityEngine;

namespace GameDev.Player
{
    /// <summary>
    /// S0 grid mover: arrow/WASD steps one cell, E interacts with nearby markers.
    /// </summary>
    public sealed class GridPlayerController : MonoBehaviour
    {
        [SerializeField] private GridMap grid;
        [SerializeField] private GridCoord startCoord = new GridCoord(18, 25);
        [SerializeField] private float moveCooldown = 0.12f;
        [SerializeField] private float interactRadius = 1.75f;

        private GridCoord _coord;
        private float _nextMoveTime;
        private bool _ready;

        public void PlaceOnGrid(GridMap map, GridCoord start)
        {
            grid = map;
            startCoord = start;
            _coord = start;
            _ready = true;
            SnapToCell();
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
            SnapToCell();
        }

        private void Update()
        {
            if (!_ready || grid == null)
            {
                return;
            }

            if (Time.time >= _nextMoveTime)
            {
                var dx = 0;
                var dy = 0;
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) dy = 1;
                else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) dy = -1;
                else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) dx = 1;
                else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) dx = -1;

                if (dx != 0 || dy != 0)
                {
                    TryMove(new GridCoord(_coord.x + dx, _coord.y + dy));
                    _nextMoveTime = Time.time + moveCooldown;
                }
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                TryInteract();
            }
        }

        private void TryMove(GridCoord target)
        {
            if (!grid.IsWalkable(target))
            {
                return;
            }

            _coord = target;
            SnapToCell();
        }

        private void SnapToCell()
        {
            var world = grid.CoordToWorldCenter(_coord);
            if (grid.TryGetCell(_coord, out var cell))
            {
                world.y = cell.height * grid.CellSize;
            }

            transform.position = world + Vector3.up * 0.1f;
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

            Debug.Log("[Player] Nothing nearby. Walk south-west to Soot & Bell / notice board, then press E.");
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
