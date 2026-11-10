using System.Collections.Generic;
using GameDev.Cameras;
using GameDev.Input;
using GameDev.Slice.Kit;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// The fight's camera (S3a.5), the grid lab's: high over the player at an angle, turned by Q and E or by dragging
    /// with the right button, brought nearer or farther by the wheel. A house standing between it and the player stops
    /// drawing, its shadow kept, and its walls show as outlines (16), so the fight is always seen. The free camera
    /// takes over again when the fight ends.
    /// </summary>
    public sealed class FightCamera : MonoBehaviour
    {
        // Authoring defaults, S3a.5, from the grid lab: degrees a second for Q and E, degrees a pixel for the mouse, the
        // pitch and distance bounds, the zoom's step, the eye's height over the feet, and how long a house stays hidden
        // once nothing hides the player behind it.
        private const float TurnRate = 110f;
        private const float MouseRate = 0.18f;
        private const float MinPitch = 30f;
        private const float MaxPitch = 75f;
        private const float MinDistance = 7f;
        private const float MaxDistance = 26f;
        private const float StartPitch = 50f;
        private const float StartDistance = 13f;
        private const float ZoomStep = 1.5f;
        private const float LookHeight = 1f;
        private const float OccluderRadius = 1.2f;
        private const float HoldSeconds = 0.4f;

        private readonly Dictionary<KitBlock, float> _hiddenUntil = new Dictionary<KitBlock, float>();
        private readonly Dictionary<Renderer, ShadowCastingMode> _hidden = new Dictionary<Renderer, ShadowCastingMode>();
        private readonly List<KitBlock> _expired = new List<KitBlock>();
        private readonly RaycastHit[] _hits = new RaycastHit[64];
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<int> _indices = new List<int>();
        private readonly List<BoxCollider> _boxes = new List<BoxCollider>();
        private Camera _camera;
        private ThirdPersonCamera _free;
        private Transform _focus;
        private Mesh _outline;
        private GameObject _outlineObject;
        private Vector3 _eye;
        private Vector3 _eyeVelocity;
        private float _yaw;
        private float _pitch = StartPitch;
        private float _distance = StartDistance;
        private int _mask;

        public bool Active { get; private set; }

        /// <summary>Houses hidden now.</summary>
        public int Hidden => _hiddenUntil.Count;

        public Camera View => _camera;

        public static FightCamera Create(Transform parent, Material outline)
        {
            var go = new GameObject("Fight Camera");
            go.transform.SetParent(parent, false);
            var camera = go.AddComponent<FightCamera>();
            camera._outline = new Mesh { name = "Fight Outlines" };
            camera._outlineObject = new GameObject("Fight Outlines");
            camera._outlineObject.transform.SetParent(go.transform, false);
            camera._outlineObject.AddComponent<MeshFilter>().sharedMesh = camera._outline;
            var renderer = camera._outlineObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = outline;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            camera._outlineObject.SetActive(false);
            return camera;
        }

        /// <summary>Takes the main camera over the player.</summary>
        public void Begin(Transform focus)
        {
            _camera = Camera.main;
            _focus = focus;
            if (_camera == null || focus == null)
            {
                return;
            }

            _mask = LayerMask.GetMask(KitLayers.Structure);
            _free = _camera.GetComponent<ThirdPersonCamera>();
            if (_free != null)
            {
                _free.enabled = false; // which frees the mouse
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _yaw = _camera.transform.eulerAngles.y;
            _pitch = StartPitch;
            _distance = StartDistance;
            _eye = Eye();
            _eyeVelocity = Vector3.zero;
            Active = true;
            Place();
        }

        /// <summary>Gives the camera back to the free camera, with every house drawn again.</summary>
        public void End()
        {
            if (!Active)
            {
                return;
            }

            Active = false;
            foreach (var pair in _hidden)
            {
                if (pair.Key != null)
                {
                    pair.Key.shadowCastingMode = pair.Value;
                }
            }

            _hidden.Clear();
            _hiddenUntil.Clear();
            _outlineObject.SetActive(false);
            if (_free != null)
            {
                _free.enabled = true; // it snaps behind the player and takes the mouse again
            }
        }

        private void LateUpdate()
        {
            if (!Active || _camera == null || _focus == null)
            {
                return;
            }

            var input = GameInputService.Instance;
            if (input != null)
            {
                _yaw += input.CameraTurn * TurnRate * Time.unscaledDeltaTime;
                if (input.CameraOrbitHeld)
                {
                    var delta = input.MouseDelta;
                    _yaw += delta.x * MouseRate;
                    _pitch = Mathf.Clamp(_pitch - (delta.y * MouseRate), MinPitch, MaxPitch);
                }

                var zoom = input.CameraZoomDelta;
                if (Mathf.Abs(zoom) > 0.01f)
                {
                    _distance = Mathf.Clamp(_distance - (Mathf.Sign(zoom) * ZoomStep), MinDistance, MaxDistance);
                }
            }

            _eye = Vector3.SmoothDamp(_eye, Eye(), ref _eyeVelocity, 0.15f, Mathf.Infinity, Time.unscaledDeltaTime);
            Place();
            HideWhatHides();
        }

        private Vector3 Eye() => _focus.position + (Vector3.up * LookHeight);

        private void Place()
        {
            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            _camera.transform.SetPositionAndRotation(_eye + (rotation * (Vector3.back * _distance)), rotation);
        }

        /// <summary>Every house the line from the camera to the player passes through is hidden, its shadow kept, and outlined.</summary>
        private void HideWhatHides()
        {
            var from = _camera.transform.position;
            var to = _eye;
            var distance = Vector3.Distance(from, to);
            var count = distance > 0.01f
                ? Physics.SphereCastNonAlloc(from, OccluderRadius, (to - from) / distance, _hits, distance, _mask, QueryTriggerInteraction.Ignore)
                : 0;
            var changed = false;
            var now = Time.unscaledTime;
            for (var i = 0; i < count; i++)
            {
                var block = _hits[i].collider.GetComponentInParent<KitBlock>();
                if (block == null)
                {
                    continue;
                }

                if (!_hiddenUntil.ContainsKey(block))
                {
                    Hide(block);
                    changed = true;
                }

                _hiddenUntil[block] = now + HoldSeconds;
            }

            _expired.Clear();
            foreach (var pair in _hiddenUntil)
            {
                if (pair.Value < now)
                {
                    _expired.Add(pair.Key);
                }
            }

            foreach (var block in _expired)
            {
                Show(block);
                _hiddenUntil.Remove(block);
                changed = true;
            }

            if (changed)
            {
                Outline();
            }
        }

        private void Hide(KitBlock block)
        {
            foreach (var r in block.GetComponentsInChildren<Renderer>())
            {
                if (!_hidden.ContainsKey(r))
                {
                    _hidden[r] = r.shadowCastingMode;
                    r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
            }
        }

        private void Show(KitBlock block)
        {
            if (block == null)
            {
                return;
            }

            foreach (var r in block.GetComponentsInChildren<Renderer>())
            {
                if (_hidden.TryGetValue(r, out var mode))
                {
                    r.shadowCastingMode = mode;
                    _hidden.Remove(r);
                }
            }
        }

        /// <summary>The hidden houses' walls as lines: the edges of every box collider of their structure.</summary>
        private void Outline()
        {
            _vertices.Clear();
            _indices.Clear();
            foreach (var block in _hiddenUntil.Keys)
            {
                if (block == null)
                {
                    continue;
                }

                block.GetComponentsInChildren(_boxes);
                foreach (var box in _boxes)
                {
                    if (!box.isTrigger && box.gameObject.layer == block.gameObject.layer)
                    {
                        Edges(box);
                    }
                }
            }

            _outline.Clear();
            _outline.SetVertices(_vertices);
            _outline.SetIndices(_indices, MeshTopology.Lines, 0);
            _outline.RecalculateBounds();
            _outlineObject.SetActive(_vertices.Count > 0);
        }

        private void Edges(BoxCollider box)
        {
            var first = _vertices.Count;
            var t = box.transform;
            for (var i = 0; i < 8; i++)
            {
                var corner = box.center + Vector3.Scale(box.size, new Vector3((i & 1) - 0.5f, ((i >> 1) & 1) - 0.5f, ((i >> 2) & 1) - 0.5f));
                _vertices.Add(t.TransformPoint(corner));
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

        private void OnDestroy()
        {
            End();
            if (_outline != null)
            {
                Destroy(_outline);
            }
        }
    }
}
