using System.Collections.Generic;
using GameDev.Core.Localization;
using GameDev.Player.Progression;
using GameDev.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameDev.Slice.Stealth
{
    /// <summary>
    /// What the character reads of stealth (10.3 §4, S3b.1), under 07.1's progressive disclosure: with Perception 4 the
    /// Notice Meter over each observer, a mark with no numbers; with Intelligence 3 the Vision Cones on the ground in
    /// Stealth Mode, faint, and clear at 6. Everyone sees the result: the observer's line over its head, and the
    /// character standing up. The marks are drawn in the HUD's own panel; the cones are one mesh, rebuilt each frame,
    /// stopped by walls.
    /// </summary>
    [RequireComponent(typeof(SliceStealth))]
    public sealed class StealthHud : MonoBehaviour
    {
        private const float MarkReach = 30f;   // m: farther marks are not drawn
        private const float ConeReach = 40f;   // m from the camera: farther cones are not drawn
        private const int Rays = 16;           // segments of a cone's fan
        private const float ConeLift = 0.06f;  // m over the observer's feet
        private const float RayHeight = 0.6f;  // m: the height a cone's ray is stopped by a wall

        // The LotM look: ink, parchment and old brass; a meter reddens toward the notice.
        private static readonly Color Ink = new Color(0.07f, 0.06f, 0.05f, 0.82f);
        private static readonly Color Parchment = new Color(0.91f, 0.86f, 0.74f, 1f);
        private static readonly Color Brass = new Color(0.78f, 0.62f, 0.32f, 1f);
        private static readonly Color Blood = new Color(0.72f, 0.12f, 0.1f, 1f);
        private static readonly Color ConeCalm = new Color(0.78f, 0.86f, 0.82f, 1f);
        private static readonly Color ConeAlarm = new Color(0.86f, 0.28f, 0.2f, 1f);

        private readonly Dictionary<StealthObserver, Mark> _marks = new Dictionary<StealthObserver, Mark>();
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Color> _colours = new List<Color>();
        private readonly List<int> _triangles = new List<int>();
        private readonly List<StealthObserver> _gone = new List<StealthObserver>();
        private SliceStealth _stealth;
        private VisualElement _root;
        private VisualElement _layer;
        private Label _badge;
        private Label _hint;
        private Mesh _mesh;
        private MeshRenderer _cones;

        private sealed class Mark
        {
            public VisualElement Box;
            public VisualElement Bar;
            public VisualElement Fill;
            public Label Line;
        }

        /// <summary>Whether a meter mark is shown over this observer now (for tests).</summary>
        public bool ShowsMeterOver(StealthObserver o) => _marks.TryGetValue(o, out var m) && m.Bar.style.display == DisplayStyle.Flex && m.Box.style.display == DisplayStyle.Flex;

        /// <summary>The line shown over this observer now, or null.</summary>
        public string LineOver(StealthObserver o) =>
            _marks.TryGetValue(o, out var m) && m.Box.style.display == DisplayStyle.Flex && m.Line.style.display == DisplayStyle.Flex ? m.Line.text : null;

        /// <summary>The cones' opacity now, 0 when none are drawn.</summary>
        public float ConeOpacity { get; private set; }

        private void Awake() => _stealth = GetComponent<SliceStealth>();

        private void LateUpdate()
        {
            var camera = Camera.main;
            if (_stealth.Rules == null || _stealth.Character == null || camera == null || !Bind())
            {
                Cones(null);
                return;
            }

            var character = _stealth.Character;
            var showMeter = _stealth.Rules.ShowsMeter(DebugSight.Read(character.Get(CharacterAttribute.Perception)));
            var opacity = _stealth.Active ? _stealth.Rules.ConeOpacity(DebugSight.Read(character.Get(CharacterAttribute.Intelligence))) : 0f;
            var eye = camera.transform.position;
            foreach (var o in StealthObserver.All)
            {
                Place(Of(o), o, camera, eye, showMeter);
            }

            _gone.Clear();
            foreach (var pair in _marks)
            {
                if (pair.Key == null || !pair.Key.isActiveAndEnabled)
                {
                    pair.Value.Box.RemoveFromHierarchy();
                    _gone.Add(pair.Key);
                }
            }

            foreach (var o in _gone)
            {
                _marks.Remove(o);
            }

            _badge.style.display = _stealth.Active ? DisplayStyle.Flex : DisplayStyle.None;
            var hint = Time.time < _stealth.HintUntil ? _stealth.Hint : null;
            _hint.text = hint ?? string.Empty;
            _hint.style.display = hint != null ? DisplayStyle.Flex : DisplayStyle.None;
            ConeOpacity = opacity;
            Cones(opacity > 0f ? camera : null);
        }

        private void Place(Mark mark, StealthObserver o, Camera camera, Vector3 eye, bool showMeter)
        {
            var at = o.MarkPosition;
            var line = Time.time < o.LineUntil ? o.Line : null;
            var meter = showMeter && o.Meter != null && o.Meter.Fill > 0f;
            var near = (at - eye).sqrMagnitude < MarkReach * MarkReach && Vector3.Dot(camera.transform.forward, at - eye) > 0f;
            if (!near || (!meter && line == null))
            {
                mark.Box.style.display = DisplayStyle.None;
                return;
            }

            mark.Box.style.display = DisplayStyle.Flex;
            mark.Bar.style.display = meter ? DisplayStyle.Flex : DisplayStyle.None;
            if (meter)
            {
                var fill = o.Meter.Fill;
                mark.Fill.style.width = Length.Percent(fill * 100f);
                mark.Fill.style.backgroundColor = o.Meter.Noticed ? Blood : Color.Lerp(Parchment, Brass, fill);
            }

            mark.Line.style.display = line != null ? DisplayStyle.Flex : DisplayStyle.None;
            mark.Line.text = line ?? string.Empty;
            var point = RuntimePanelUtils.CameraTransformWorldToPanel(_root.panel, at, camera);
            mark.Box.style.left = point.x;
            mark.Box.style.top = point.y;
        }

        private Mark Of(StealthObserver o)
        {
            if (_marks.TryGetValue(o, out var mark))
            {
                return mark;
            }

            mark = new Mark { Box = new VisualElement(), Bar = new VisualElement(), Fill = new VisualElement(), Line = new Label() };
            var box = mark.Box.style;
            box.position = Position.Absolute;
            box.alignItems = Align.Center;
            box.translate = new Translate(Length.Percent(-50), Length.Percent(-100));
            var bar = mark.Bar.style;
            bar.width = 46;
            bar.height = 6;
            bar.backgroundColor = Ink;
            bar.borderTopWidth = bar.borderBottomWidth = bar.borderLeftWidth = bar.borderRightWidth = 1;
            bar.borderTopColor = bar.borderBottomColor = bar.borderLeftColor = bar.borderRightColor = Brass;
            mark.Fill.style.height = Length.Percent(100);
            mark.Bar.Add(mark.Fill);
            var line = mark.Line.style;
            line.color = Parchment;
            line.backgroundColor = Ink;
            line.fontSize = 15;
            line.unityFontStyleAndWeight = FontStyle.Italic;
            line.paddingLeft = line.paddingRight = 8;
            line.paddingTop = line.paddingBottom = 3;
            line.marginBottom = 4;
            line.borderBottomWidth = 1;
            line.borderBottomColor = Brass;
            line.whiteSpace = WhiteSpace.Normal;
            line.maxWidth = 320;
            mark.Box.Add(mark.Line);
            mark.Box.Add(mark.Bar);
            mark.Box.pickingMode = PickingMode.Ignore;
            _layer.Add(mark.Box);
            _marks[o] = mark;
            return mark;
        }

        /// <summary>Finds the HUD's panel and adds this layer to it, again after a scene brings a new HUD.</summary>
        private bool Bind()
        {
            if (_root != null && _root.panel != null && _layer.parent == _root)
            {
                return true;
            }

            var hud = FindAnyObjectByType<HudView>();
            var document = hud != null ? hud.GetComponent<UIDocument>() : null;
            if (document == null || document.rootVisualElement == null)
            {
                return false;
            }

            _root = document.rootVisualElement;
            _marks.Clear();
            _layer = new VisualElement { name = "stealth", pickingMode = PickingMode.Ignore };
            _layer.style.position = Position.Absolute;
            _layer.style.left = _layer.style.top = _layer.style.right = _layer.style.bottom = 0;
            _badge = new Label(Loc.Get("ui.stealth.mode")) { pickingMode = PickingMode.Ignore };
            var badge = _badge.style;
            badge.position = Position.Absolute;
            badge.bottom = 92;
            badge.left = Length.Percent(50);
            badge.translate = new Translate(Length.Percent(-50), 0);
            badge.color = Parchment;
            badge.backgroundColor = Ink;
            badge.fontSize = 16;
            badge.letterSpacing = 2;
            badge.paddingLeft = badge.paddingRight = 14;
            badge.paddingTop = badge.paddingBottom = 4;
            badge.borderTopWidth = badge.borderBottomWidth = 1;
            badge.borderTopColor = badge.borderBottomColor = Brass;
            _hint = new Label { pickingMode = PickingMode.Ignore };
            var hint = _hint.style;
            hint.position = Position.Absolute;
            hint.bottom = 124;
            hint.left = Length.Percent(50);
            hint.translate = new Translate(Length.Percent(-50), 0);
            hint.color = Parchment;
            hint.backgroundColor = Ink;
            hint.fontSize = 15;
            hint.unityFontStyleAndWeight = FontStyle.Italic;
            hint.paddingLeft = hint.paddingRight = 10;
            hint.paddingTop = hint.paddingBottom = 3;
            _layer.Add(_badge);
            _layer.Add(_hint);
            _root.Add(_layer);
            Loc.Changed -= Relabel;
            Loc.Changed += Relabel;
            return true;
        }

        private void Relabel()
        {
            if (_badge != null)
            {
                _badge.text = Loc.Get("ui.stealth.mode");
            }
        }

        /// <summary>The Vision Cones of the observers near the character as one mesh, each ray stopped by a wall.</summary>
        private void Cones(Camera camera)
        {
            if (camera == null || _stealth.Rules == null)
            {
                if (_cones != null)
                {
                    _cones.enabled = false;
                }

                return;
            }

            Build();
            _vertices.Clear();
            _colours.Clear();
            _triangles.Clear();
            var rules = _stealth.Rules;
            var centre = camera.transform.position;
            var reach = rules.ConeCells * 1f;
            var half = rules.ConeDegrees * 0.5f;
            foreach (var o in StealthObserver.All)
            {
                var feet = o.transform.position;
                if ((feet - centre).sqrMagnitude > ConeReach * ConeReach)
                {
                    continue;
                }

                var alarm = o.Meter != null ? o.Meter.Fill : 0f;
                var tint = Color.Lerp(ConeCalm, ConeAlarm, alarm);
                var first = _vertices.Count;
                var origin = feet + (Vector3.up * ConeLift);
                _vertices.Add(origin);
                _colours.Add(new Color(tint.r, tint.g, tint.b, 0.22f * ConeOpacity));
                var forward = o.transform.forward;
                forward.y = 0f;
                for (var i = 0; i <= Rays; i++)
                {
                    var angle = Mathf.Lerp(-half, half, i / (float)Rays);
                    var direction = Quaternion.Euler(0f, angle, 0f) * forward.normalized;
                    var length = Physics.Raycast(feet + (Vector3.up * RayHeight), direction, out var hit, reach, ~0, QueryTriggerInteraction.Ignore)
                        && hit.transform.GetComponentInParent<StealthObserver>() == null && !hit.collider.GetComponent<CharacterController>()
                        ? hit.distance : reach;
                    _vertices.Add(origin + (direction * length));
                    _colours.Add(new Color(tint.r, tint.g, tint.b, 0.05f * ConeOpacity));
                    if (i > 0)
                    {
                        _triangles.Add(first);
                        _triangles.Add(first + i);
                        _triangles.Add(first + i + 1);
                    }
                }
            }

            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetColors(_colours);
            _mesh.SetTriangles(_triangles, 0);
            _mesh.RecalculateBounds();
            _cones.enabled = _triangles.Count > 0;
        }

        private void Build()
        {
            if (_cones != null)
            {
                return;
            }

            var go = new GameObject("Vision Cones");
            DontDestroyOnLoad(go);
            _mesh = new Mesh { name = "VisionCones" };
            _mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _cones = go.AddComponent<MeshRenderer>();
            _cones.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _cones.receiveShadows = false;
            var shader = Shader.Find("Sprites/Default");
            _cones.sharedMaterial = new Material(shader) { name = "VisionCones", renderQueue = 3100 };
        }

        private void OnDestroy()
        {
            Loc.Changed -= Relabel;
            if (_cones != null)
            {
                Destroy(_cones.gameObject);
            }
        }
    }
}
