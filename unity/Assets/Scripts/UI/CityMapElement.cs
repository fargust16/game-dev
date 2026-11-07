using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameDev.UI
{
    /// <summary>A place on the map (S1.6): a Marked POI is a pin with its source's words, a Hinted one an area; never a countdown (04 rule 5).</summary>
    public readonly struct MapMark
    {
        public MapMark(Vector3 world, float radius, bool exact, string label)
        {
            World = world;
            Radius = radius;
            Exact = exact;
            Label = label;
        }

        public Vector3 World { get; }

        /// <summary>The area's radius, m, for a Hinted place.</summary>
        public float Radius { get; }

        /// <summary>True for a pin (Marked), false for an area (Hinted).</summary>
        public bool Exact { get; }

        public string Label { get; }
    }

    /// <summary>
    /// Draws the city plan from its SVG (the subset the plan uses: rect, circle, line, polyline, polygon, text, and g
    /// carrying fill, stroke, and font size) with UI Toolkit's vector API, then the marks and the player on top. Plan
    /// coordinates are the city plan's: world X = px − 500, Z = 500 − py.
    /// </summary>
    public sealed class CityMapElement : VisualElement
    {
        private const string MarkLabelClass = "hud__map-mark";
        private static readonly Color AreaFill = new Color(0.95f, 0.6f, 0.15f, 0.28f);
        private static readonly Color AreaEdge = new Color(0.75f, 0.4f, 0.05f, 0.9f);
        private static readonly Color PinColour = new Color(0.8f, 0.08f, 0.08f);
        private static readonly Color PlayerColour = new Color(0.1f, 0.35f, 0.9f);

        private readonly List<Shape> _shapes = new List<Shape>();
        private readonly List<Shape> _texts = new List<Shape>();
        private Rect _viewBox = new Rect(0, 0, 100, 100);
        private IReadOnlyList<MapMark> _marks = new MapMark[0];
        private Vector3? _player;

        public CityMapElement()
        {
            generateVisualContent += Draw;
            RegisterCallback<GeometryChangedEvent>(_ => Relabel());
        }

        /// <summary>The labels of the marks now drawn, for tests.</summary>
        public IReadOnlyList<string> MarkLabels => _marks.Select(m => m.Label).ToList();

        public IReadOnlyList<MapMark> Marks => _marks;

        public bool HasPlan => _shapes.Count > 0;

        public void SetPlan(string svg)
        {
            _shapes.Clear();
            _texts.Clear();
            var root = XElement.Parse(svg);
            var box = Numbers(root.Attribute("viewBox")?.Value);
            if (box.Length == 4)
            {
                _viewBox = new Rect(box[0], box[1], box[2], box[3]);
            }

            Read(root, new Style { Fill = Color.black, FontSize = 4f });
            Relabel();
            MarkDirtyRepaint();
        }

        /// <summary>Sets the marks and the player; called every frame while open, it relabels only when the marks change.</summary>
        public void SetMarks(IReadOnlyList<MapMark> marks, Vector3? player)
        {
            marks ??= new MapMark[0];
            var same = marks.Count == _marks.Count && marks.Zip(_marks, (a, b) => a.Exact == b.Exact && a.Label == b.Label && a.World == b.World).All(x => x);
            _marks = marks;
            _player = player;
            if (!same)
            {
                Relabel();
            }

            MarkDirtyRepaint();
        }

        private float Scale => Mathf.Min(contentRect.width / _viewBox.width, contentRect.height / _viewBox.height);

        private Vector2 ToPanel(float px, float py)
        {
            var s = Scale;
            var offset = new Vector2((contentRect.width - (_viewBox.width * s)) * 0.5f, (contentRect.height - (_viewBox.height * s)) * 0.5f);
            return offset + new Vector2((px - _viewBox.x) * s, (py - _viewBox.y) * s);
        }

        private Vector2 WorldToPanel(Vector3 world) => ToPanel(world.x + 500f, 500f - world.z);

        private void Draw(MeshGenerationContext context)
        {
            if (float.IsNaN(contentRect.width) || contentRect.width <= 0f)
            {
                return;
            }

            var p = context.painter2D;
            var s = Scale;
            foreach (var shape in _shapes)
            {
                p.BeginPath();
                switch (shape.Kind)
                {
                    case "circle":
                        p.Arc(ToPanel(shape.Points[0], shape.Points[1]), shape.Points[2] * s, 0f, 360f);
                        p.ClosePath();
                        break;
                    default:
                        p.MoveTo(ToPanel(shape.Points[0], shape.Points[1]));
                        for (var i = 2; i + 1 < shape.Points.Length; i += 2)
                        {
                            p.LineTo(ToPanel(shape.Points[i], shape.Points[i + 1]));
                        }

                        if (shape.Closed)
                        {
                            p.ClosePath();
                        }

                        break;
                }

                if (shape.Closed && shape.Style.Fill.HasValue)
                {
                    p.fillColor = shape.Style.Fill.Value;
                    p.Fill();
                }

                if (shape.Style.Stroke.HasValue)
                {
                    p.strokeColor = shape.Style.Stroke.Value;
                    p.lineWidth = Mathf.Max(1f, shape.Style.StrokeWidth * s);
                    p.Stroke();
                }
            }

            foreach (var mark in _marks.Where(m => !m.Exact))
            {
                p.BeginPath();
                p.Arc(WorldToPanel(mark.World), mark.Radius * s, 0f, 360f);
                p.ClosePath();
                p.fillColor = AreaFill;
                p.Fill();
                p.strokeColor = AreaEdge;
                p.lineWidth = 1.5f;
                p.Stroke();
            }

            foreach (var mark in _marks.Where(m => m.Exact))
            {
                var at = WorldToPanel(mark.World);
                p.BeginPath();
                p.MoveTo(at);
                p.LineTo(at + new Vector2(-6f, -14f));
                p.LineTo(at + new Vector2(6f, -14f));
                p.ClosePath();
                p.fillColor = PinColour;
                p.Fill();
                p.BeginPath();
                p.Arc(at + new Vector2(0f, -16f), 6f, 0f, 360f);
                p.ClosePath();
                p.Fill();
            }

            if (_player.HasValue)
            {
                p.BeginPath();
                p.Arc(WorldToPanel(_player.Value), 5f, 0f, 360f);
                p.ClosePath();
                p.fillColor = PlayerColour;
                p.Fill();
                p.strokeColor = Color.white;
                p.lineWidth = 1.5f;
                p.Stroke();
            }
        }

        // Text is Labels: the plan's names, then the marks' words beside their pins and areas.
        private void Relabel()
        {
            Clear();
            if (float.IsNaN(contentRect.width) || contentRect.width <= 0f)
            {
                return;
            }

            var s = Scale;
            foreach (var text in _texts)
            {
                var size = text.Style.FontSize * s;
                var label = new Label(text.Text) { pickingMode = PickingMode.Ignore };
                label.style.position = Position.Absolute;
                var at = ToPanel(text.Points[0], text.Points[1]);
                label.style.left = at.x;
                label.style.top = at.y - size;
                label.style.fontSize = size;
                label.style.color = text.Style.Fill ?? Color.black;
                label.style.paddingLeft = label.style.paddingRight = label.style.paddingTop = label.style.paddingBottom = 0;
                label.style.marginLeft = label.style.marginRight = label.style.marginTop = label.style.marginBottom = 0;
                if (text.Style.Bold)
                {
                    label.style.unityFontStyleAndWeight = FontStyle.Bold;
                }

                if (Mathf.Abs(text.Rotation) > 0.01f)
                {
                    label.style.transformOrigin = new TransformOrigin(Length.Percent(0), Length.Percent(100));
                    label.style.rotate = new Rotate(text.Rotation);
                }

                Add(label);
            }

            foreach (var mark in _marks)
            {
                var label = new Label(mark.Label) { pickingMode = PickingMode.Ignore };
                label.AddToClassList(MarkLabelClass);
                label.style.position = Position.Absolute;
                var at = WorldToPanel(mark.World);
                label.style.left = at.x + (mark.Exact ? 9f : (mark.Radius * s) + 4f);
                label.style.top = at.y - (mark.Exact ? 26f : 10f);
                if (!mark.Exact)
                {
                    label.style.unityFontStyleAndWeight = FontStyle.Italic;
                }

                Add(label);
            }
        }

        private void Read(XElement element, Style inherited)
        {
            foreach (var child in element.Elements())
            {
                var style = inherited.With(child);
                var a = (System.Func<string, float>)(name => Number(child.Attribute(name)?.Value));
                switch (child.Name.LocalName)
                {
                    case "g":
                        Read(child, style);
                        break;
                    case "rect":
                        var x = a("x");
                        var y = a("y");
                        var w = a("width");
                        var h = a("height");
                        _shapes.Add(new Shape("rect", new[] { x, y, x + w, y, x + w, y + h, x, y + h }, true, style));
                        break;
                    case "circle":
                        _shapes.Add(new Shape("circle", new[] { a("cx"), a("cy"), a("r") }, true, style));
                        break;
                    case "line":
                        _shapes.Add(new Shape("line", new[] { a("x1"), a("y1"), a("x2"), a("y2") }, false, style));
                        break;
                    case "polyline":
                    case "polygon":
                        _shapes.Add(new Shape(child.Name.LocalName, Numbers(child.Attribute("points")?.Value), child.Name.LocalName == "polygon", style));
                        break;
                    case "text":
                        var rotation = 0f;
                        var transform = child.Attribute("transform")?.Value;
                        if (transform != null && transform.StartsWith("rotate("))
                        {
                            rotation = Numbers(transform.Substring(7)).FirstOrDefault();
                        }

                        _texts.Add(new Shape("text", new[] { a("x"), a("y") }, false, style) { Text = child.Value, Rotation = rotation });
                        break;
                }
            }
        }

        private static float Number(string text) =>
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0f;

        private static float[] Numbers(string text) =>
            string.IsNullOrWhiteSpace(text)
                ? new float[0]
                : text.Split(new[] { ' ', ',', ')' }, System.StringSplitOptions.RemoveEmptyEntries).Select(Number).ToArray();

        private sealed class Shape
        {
            public Shape(string kind, float[] points, bool closed, Style style)
            {
                Kind = kind;
                Points = points;
                Closed = closed;
                Style = style;
            }

            public string Kind { get; }
            public float[] Points { get; }
            public bool Closed { get; }
            public Style Style { get; }
            public string Text { get; set; }
            public float Rotation { get; set; }
        }

        private struct Style
        {
            public Color? Fill;
            public Color? Stroke;
            public float StrokeWidth;
            public float FontSize;
            public bool Bold;

            public Style With(XElement element)
            {
                var style = this;
                var opacity = Number(element.Attribute("opacity")?.Value ?? "1");
                var fill = element.Attribute("fill")?.Value;
                if (fill != null)
                {
                    style.Fill = Colour(fill, opacity);
                }

                var stroke = element.Attribute("stroke")?.Value;
                if (stroke != null)
                {
                    style.Stroke = Colour(stroke, 1f);
                    style.StrokeWidth = style.StrokeWidth > 0f ? style.StrokeWidth : 1f;
                }

                var width = element.Attribute("stroke-width")?.Value;
                if (width != null)
                {
                    style.StrokeWidth = Number(width);
                }

                var size = element.Attribute("font-size")?.Value;
                if (size != null)
                {
                    style.FontSize = Number(size);
                }

                if (element.Attribute("font-weight")?.Value == "bold")
                {
                    style.Bold = true;
                }

                return style;
            }

            private static Color? Colour(string text, float opacity)
            {
                if (text == "none" || !ColorUtility.TryParseHtmlString(text, out var colour))
                {
                    return null;
                }

                colour.a *= opacity;
                return colour;
            }
        }
    }
}
