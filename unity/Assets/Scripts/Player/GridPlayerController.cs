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
        [SerializeField] private float moveCooldown = 0.12f;
        [SerializeField] private float interactRadius = 1.35f;

        private GridCoord _coord;
        private float _nextMoveTime;

        public void PlaceOnGrid(GridMap map, GridCoord start)
        {
            grid = map;
            _coord = start;
            SnapToCell();
        }

        private void Update()
        {
            if (grid == null)
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
                var d = Vector3.Distance(transform.position, item.transform.position);
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
                var d = Vector3.Distance(transform.position, item.transform.position);
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

            Debug.Log("[Player] Nothing to interact with nearby.");
        }
    }
}
