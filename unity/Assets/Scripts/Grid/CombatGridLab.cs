using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using GameDev.Cameras;
using GameDev.Confront.Grid;
using GameDev.Player;
using GameDev.World.Buildings.Shell;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Debug = UnityEngine.Debug;

namespace GameDev.Grid
{
    /// <summary>
    /// Phase B of the Combat Grid technical test: the grid, the tactical camera and the transparent
    /// walls in a scene the owner can walk through. A lab, not the production grid.
    /// </summary>
    /// <remarks>
    /// Started by <c>GameDev/Combat Grid Test/Play Lab</c>, which builds everything in memory.
    /// Plan: <c>Assets/Plans/COMBAT_GRID_TECH_TEST.md</c>.
    /// </remarks>
    public sealed class CombatGridLab : MonoBehaviour
    {
        private const float CellMeters = 1f;
        private const float OverlayLift = 0.12f;
        private const float CellInset = 0.05f;
        private const float CornerMatchMeters = 1.2f;
        private const float LookHeight = 1f;
        private const float RotateDegreesPerSecond = 110f;
        private const float MouseDegreesPerPixel = 0.18f;
        private const float MinPitch = 25f;
        private const float MaxPitch = 80f;
        private const float MinDistance = 8f;
        private const float MaxDistance = 30f;
        private const float ZoomStep = 1.5f;
        private const float OccluderRadius = 1.6f;
        private const float OccluderHoldSeconds = 0.4f;
        private const int IgnoreRaycastLayer = 2;

        private static readonly Color Open = new Color(0.75f, 0.9f, 1f, 0.3f);
        private static readonly Color LowCover = new Color(0.95f, 0.85f, 0.3f, 0.5f);
        private static readonly Color HighCover = new Color(0.95f, 0.55f, 0.15f, 0.55f);
        private static readonly Color PlayerCell = new Color(1f, 1f, 1f, 0.75f);
        private static readonly Color Outline = new Color(1f, 1f, 1f, 0.85f);

        [SerializeField] private Transform player;
        [SerializeField] private Transform shells;
        [SerializeField] private string smokeOut = "";

        private readonly Dictionary<ShellDebugGizmo, float> _hiddenUntil = new Dictionary<ShellDebugGizmo, float>();
        private readonly List<ShellDebugGizmo> _expired = new List<ShellDebugGizmo>();
        private readonly RaycastHit[] _occluders = new RaycastHit[128];
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Color> _colors = new List<Color>();
        private readonly List<int> _indices = new List<int>();

        private CombatGridReader _reader;
        private CombatGridWindow _window;
        private readonly List<CombatCell> _cells = new List<CombatCell>();
        private Camera _camera;
        private ThirdPersonCamera _freeCamera;
        private Mesh _overlay;
        private Mesh _outline;
        private GameObject _overlayObject;
        private GameObject _outlineObject;
        private bool _fightView;
        private bool _hasCell;
        private int _cellX;
        private int _cellZ;
        private int _gridMoves;
        private double _lastStepMs;
        private float _yaw;
        private float _pitch = 52f;
        private float _distance = 16f;
        private Vector3 _focus;
        private Vector3 _focusVelocity;

        public void Configure(Transform playerTransform, Transform shellsRoot, string smokeFolder)
        {
            player = playerTransform;
            shells = shellsRoot;
            smokeOut = smokeFolder ?? "";
        }

        private void Start()
        {
            // The grid reads the place, not the people in it: a character's own capsule would
            // otherwise block the cells around them. Who stands where is occupancy, a separate thing.
            player.gameObject.layer = IgnoreRaycastLayer;

            _reader = new CombatGridReader(CellMeters);
            _camera = Camera.main;
            _freeCamera = _camera != null ? _camera.GetComponent<ThirdPersonCamera>() : null;

            var material = new Material(Shader.Find("Sprites/Default"));
            _overlay = CreateMeshObject("CombatGridOverlay", material, out _overlayObject);
            _outline = CreateMeshObject("CombatGridOutlines", material, out _outlineObject);
            _overlayObject.SetActive(false);
            _outlineObject.SetActive(false);

#if UNITY_EDITOR
            if (smokeOut.Length > 0)
            {
                StartCoroutine(Smoke());
            }
#endif
        }

        private Mesh CreateMeshObject(string objectName, Material material, out GameObject go)
        {
            go = new GameObject(objectName);
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            var mesh = new Mesh { name = objectName };
            mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            return mesh;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.gKey.wasPressedThisFrame)
            {
                SetFightView(!_fightView);
            }

            if (_fightView)
            {
                FollowPlayer();
            }
        }

        private void LateUpdate()
        {
            if (!_fightView)
            {
                return;
            }

            UpdateCamera();
            UpdateWalls();
        }

        private void SetFightView(bool on)
        {
            _fightView = on;
            if (_freeCamera != null)
            {
                _freeCamera.enabled = !on;
            }

            _overlayObject.SetActive(on);
            _outlineObject.SetActive(on);
            if (on)
            {
                _yaw = _camera.transform.eulerAngles.y;
                _focus = player.position + Vector3.up * LookHeight;
                _focusVelocity = Vector3.zero;
                _hasCell = false;
                return;
            }

            foreach (var pair in _hiddenUntil)
            {
                SetShellVisible(pair.Key, true);
            }

            _hiddenUntil.Clear();
        }

        /// <summary>The grid moves only when the player enters a new cell.</summary>
        private void FollowPlayer()
        {
            var position = player.position;
            var ix = Mathf.FloorToInt(position.x / CellMeters);
            var iz = Mathf.FloorToInt(position.z / CellMeters);
            if (_hasCell && ix == _cellX && iz == _cellZ)
            {
                return;
            }

            if (!TryLevelAt(ix, iz, position.y, out var level))
            {
                return;
            }

            var started = Stopwatch.GetTimestamp();
            _window = _reader.Build(ix, iz, level);
            RebuildOverlay();
            _lastStepMs = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;

            _hasCell = true;
            _cellX = ix;
            _cellZ = iz;
            _gridMoves++;
        }

        private bool TryLevelAt(int ix, int iz, float y, out int level)
        {
            var column = _reader.Column(ix, iz);
            var best = float.MaxValue;
            level = -1;
            for (var l = 0; l < column.Length; l++)
            {
                var gap = Mathf.Abs(column[l].Y - y);
                if (column[l].Standable && gap < best)
                {
                    best = gap;
                    level = l;
                }
            }

            return level >= 0;
        }

        private void RebuildOverlay()
        {
            _vertices.Clear();
            _colors.Clear();
            _indices.Clear();

            _reader.CopyReachable(_cells);
            foreach (var cell in _cells)
            {
                int ix = cell.X, iz = cell.Z, level = cell.Level;
                var y = _reader.Column(ix, iz)[level].Y;
                var cover = _reader.Cover(ix, iz, level);
                var color = cover.High != 0 ? HighCover : cover.Low != 0 ? LowCover : Open;
                if (ix == _window.X && iz == _window.Z)
                {
                    color = PlayerCell;
                }

                AddCell(ix, iz, y, color);
            }

            _overlay.Clear();
            _overlay.SetVertices(_vertices);
            _overlay.SetColors(_colors);
            _overlay.SetTriangles(_indices, 0);
            _overlay.RecalculateBounds();
        }

        private void AddCell(int ix, int iz, float y, Color color)
        {
            var first = _vertices.Count;
            var low = CellInset * CellMeters;
            var high = (1f - CellInset) * CellMeters;
            var x = ix * CellMeters;
            var z = iz * CellMeters;
            _vertices.Add(new Vector3(x + low, CornerHeight(ix, iz, y), z + low));
            _vertices.Add(new Vector3(x + low, CornerHeight(ix, iz + 1, y), z + high));
            _vertices.Add(new Vector3(x + high, CornerHeight(ix + 1, iz + 1, y), z + high));
            _vertices.Add(new Vector3(x + high, CornerHeight(ix + 1, iz, y), z + low));
            for (var i = 0; i < 4; i++)
            {
                _colors.Add(color);
            }

            _indices.Add(first);
            _indices.Add(first + 1);
            _indices.Add(first + 2);
            _indices.Add(first);
            _indices.Add(first + 2);
            _indices.Add(first + 3);
        }

        /// <summary>A corner lies on the slope when it averages the four cells around it.</summary>
        private float CornerHeight(int cornerX, int cornerZ, float y)
        {
            var sum = 0f;
            var count = 0;
            for (var dz = -1; dz <= 0; dz++)
            {
                for (var dx = -1; dx <= 0; dx++)
                {
                    var column = _reader.Column(cornerX + dx, cornerZ + dz);
                    for (var l = 0; l < column.Length; l++)
                    {
                        if (column[l].Standable && Mathf.Abs(column[l].Y - y) < CornerMatchMeters)
                        {
                            sum += column[l].Y;
                            count++;
                            break;
                        }
                    }
                }
            }

            return (count > 0 ? sum / count : y) + OverlayLift;
        }

        private void UpdateCamera()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard != null)
            {
                if (keyboard.qKey.isPressed)
                {
                    _yaw -= RotateDegreesPerSecond * Time.deltaTime;
                }

                if (keyboard.eKey.isPressed)
                {
                    _yaw += RotateDegreesPerSecond * Time.deltaTime;
                }
            }

            if (mouse != null)
            {
                if (mouse.rightButton.isPressed)
                {
                    var delta = mouse.delta.ReadValue();
                    _yaw += delta.x * MouseDegreesPerPixel;
                    _pitch = Mathf.Clamp(_pitch - delta.y * MouseDegreesPerPixel, MinPitch, MaxPitch);
                }

                var scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    _distance = Mathf.Clamp(_distance - Mathf.Sign(scroll) * ZoomStep, MinDistance, MaxDistance);
                }
            }

            _focus = Vector3.SmoothDamp(_focus, player.position + Vector3.up * LookHeight, ref _focusVelocity, 0.12f);
            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            _camera.transform.SetPositionAndRotation(_focus + rotation * (Vector3.back * _distance), rotation);
        }

        /// <summary>
        /// Turns every building between the camera and the player into the outline of its walls.
        /// A whole building goes, because a hole cut in one shows the empty shell inside.
        /// </summary>
        private void UpdateWalls()
        {
            var from = _camera.transform.position;
            var to = player.position + Vector3.up * LookHeight;
            var distance = Vector3.Distance(from, to);
            var count = Physics.SphereCastNonAlloc(
                from,
                OccluderRadius,
                (to - from) / distance,
                _occluders,
                distance,
                CombatGridReader.Mask,
                QueryTriggerInteraction.Ignore);

            var changed = false;
            for (var i = 0; i < count; i++)
            {
                var shell = _occluders[i].transform.GetComponentInParent<ShellDebugGizmo>();
                if (shell == null || !shell.transform.IsChildOf(shells))
                {
                    continue;
                }

                if (!_hiddenUntil.ContainsKey(shell))
                {
                    SetShellVisible(shell, false);
                    changed = true;
                }

                _hiddenUntil[shell] = Time.time + OccluderHoldSeconds;
            }

            _expired.Clear();
            foreach (var pair in _hiddenUntil)
            {
                if (pair.Value < Time.time)
                {
                    _expired.Add(pair.Key);
                }
            }

            for (var i = 0; i < _expired.Count; i++)
            {
                SetShellVisible(_expired[i], true);
                _hiddenUntil.Remove(_expired[i]);
                changed = true;
            }

            if (changed)
            {
                RebuildOutlines();
            }
        }

        /// <summary>The foundation stays, so the footprint remains and no inside is shown.</summary>
        private static void SetShellVisible(ShellDebugGizmo shell, bool visible)
        {
            var renderers = shell.GetComponentsInChildren<MeshRenderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (!renderers[i].name.Contains("FOUNDATION"))
                {
                    renderers[i].enabled = visible;
                }
            }
        }

        private static bool IsGroundWall(string pieceName)
        {
            return pieceName.StartsWith("F0_", System.StringComparison.Ordinal)
                   && (pieceName.Contains("WALL") || pieceName.Contains("CORNER") || pieceName.Contains("BLANK"));
        }

        private void RebuildOutlines()
        {
            _vertices.Clear();
            _colors.Clear();
            _indices.Clear();
            foreach (var pair in _hiddenUntil)
            {
                var renderers = pair.Key.GetComponentsInChildren<MeshRenderer>(true);
                for (var i = 0; i < renderers.Length; i++)
                {
                    if (IsGroundWall(renderers[i].name))
                    {
                        AddBoxEdges(renderers[i].transform);
                    }
                }
            }

            _outline.Clear();
            _outline.SetVertices(_vertices);
            _outline.SetColors(_colors);
            _outline.SetIndices(_indices, MeshTopology.Lines, 0);
            _outline.RecalculateBounds();
        }

        private void AddBoxEdges(Transform box)
        {
            var first = _vertices.Count;
            for (var i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) - 0.5f, ((i >> 1) & 1) - 0.5f, ((i >> 2) & 1) - 0.5f);
                _vertices.Add(box.TransformPoint(corner));
                _colors.Add(Outline);
            }

            // Two corners share an edge when their indices differ in exactly one bit.
            for (var a = 0; a < 8; a++)
            {
                for (var bit = 1; bit < 8; bit <<= 1)
                {
                    if ((a & bit) == 0)
                    {
                        _indices.Add(first + a);
                        _indices.Add(first + (a | bit));
                    }
                }
            }
        }

        private void OnGUI()
        {
            var text = _fightView
                ? "G: выйти из боя   Q / E или ПКМ: поворот камеры   колесо: зум   WASD: ходьба\n"
                  + $"Клеток достижимо: {(_window != null ? _window.ReachableCount : 0)}   "
                  + $"сдвигов сетки: {_gridMoves}   последний сдвиг: {_lastStepMs:F1} мс   "
                  + $"скрыто зданий: {_hiddenUntil.Count}"
                : "G: режим боя";
            GUI.Label(new Rect(12f, 12f, 900f, 60f), text);
        }

#if UNITY_EDITOR
        /// <summary>Batch check: turns the camera, walks the player, and saves what the camera sees.</summary>
        private IEnumerator Smoke()
        {
            Application.runInBackground = true;
            yield return null;
            SetFightView(true);
            for (var i = 0; i < 4; i++)
            {
                _yaw = i * 90f;
                for (var frame = 0; frame < 20; frame++)
                {
                    yield return null;
                }

                Capture($"view_{i}.png");
            }

            var movesBefore = _gridMoves;
            var controller = player.GetComponent<CharacterController>();
            for (var frame = 0; frame < 80; frame++)
            {
                controller.Move(new Vector3(0.1f, -0.2f, 0f));
                yield return null;
            }

            _focus = player.position + Vector3.up * LookHeight;
            yield return null;
            yield return null;
            Capture("after_walk.png");
            Debug.Log($"[CombatGridLab] grid moves while walking: {_gridMoves - movesBefore}, last {_lastStepMs:F2} ms");
            EditorApplication.Exit(0);
        }

        private void Capture(string fileName)
        {
            var target = new RenderTexture(1280, 720, 24);
            _camera.targetTexture = target;
            _camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            RenderTexture.active = null;
            _camera.targetTexture = null;
            File.WriteAllBytes(Path.Combine(smokeOut, fileName), image.EncodeToPNG());
            Debug.Log($"[CombatGridLab] {fileName}: {_window.ReachableCount} cells, {_hiddenUntil.Count} hidden buildings");
            Destroy(image);
            Destroy(target);
        }
#endif
    }
}
