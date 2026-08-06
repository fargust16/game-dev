using GameDev.Interaction;
using GameDev.World;
using UnityEngine;

namespace GameDev.Decisions
{
    /// <summary>
    /// S2 playable Decision Node UI. Keys 1-5 choose; Esc cancels without resolving.
    /// </summary>
    public sealed class DecisionNodeSession : MonoBehaviour
    {
        public static DecisionNodeSession Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance._active;

        private DecisionNodeDef _node;
        private bool _active;
        private GUIStyle _box;
        private GUIStyle _title;
        private GUIStyle _body;
        private GUIStyle _option;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public bool TryOpen(PoiShellId poiId)
        {
            var def = DecisionNodeCatalog.Get(poiId);
            if (def == null || def.Options == null || def.Options.Length == 0)
            {
                return false;
            }

            _node = def;
            _active = true;
            InteractionFeedback.Show(
                "Decision Node",
                $"{def.Title}. Press 1-{def.Options.Length} to choose an Approach. Esc cancels.",
                4f);
            return true;
        }

        public void Close()
        {
            _active = false;
            _node = null;
        }

        private void Update()
        {
            if (!_active || _node == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                InteractionFeedback.Show("Decision cancelled", "The moment is still open if the Active Window allows.");
                return;
            }

            for (var i = 0; i < _node.Options.Length && i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                {
                    Resolve(_node.Options[i]);
                    return;
                }
            }
        }

        private void Resolve(DecisionOptionDef option)
        {
            var failed = !option.CountsAsWithdraw && Random.value < 0.28f;
            var body = failed
                ? $"FAIL (valuable): {option.Failure}\nStill happened: {option.Consequence}"
                : $"{option.Consequence}\nCost: {option.Cost}\nOpens: {option.Opens}\nCloses: {option.Closes}";

            if (option.PrimaryApproach == ApproachType.Confront)
            {
                body += "\n(Confront stub — full Turn-Based combat arrives in S3.)";
            }

            if (DistrictResidue.Instance != null && option.ResidueFlags != null)
            {
                DistrictResidue.Instance.Add(option.ResidueFlags);
            }

            if (PoiDirector.Instance != null)
            {
                PoiDirector.Instance.CompleteFromDecision(
                    _node.PoiId,
                    option.CountsAsWithdraw,
                    $"{option.Name} via {option.PrimaryApproach}",
                    body);
            }

            InteractionFeedback.Show($"{_node.Title} — {option.Name}", body, 10f);
            Close();
        }

        private void OnGUI()
        {
            if (!_active || _node == null)
            {
                return;
            }

            EnsureStyles();
            const float width = 640f;
            var height = 120f + _node.Options.Length * 54f;
            var rect = new Rect((Screen.width - width) * 0.5f, 70f, width, height);
            GUI.Box(rect, GUIContent.none, _box);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 12f, width - 32f, 24f), _node.Title, _title);
            GUI.Label(
                new Rect(rect.x + 16f, rect.y + 40f, width - 32f, 48f),
                $"{_node.Situation}\nAxes: {_node.PrimaryAxis} + {_node.SecondaryAxis}",
                _body);

            var y = rect.y + 100f;
            for (var i = 0; i < _node.Options.Length; i++)
            {
                var opt = _node.Options[i];
                var line =
                    $"[{i + 1}] {opt.Name}  ({opt.PrimaryApproach}) — {opt.ApproachNote}\n     {opt.Axes}. Risk: {opt.Cost}";
                GUI.Label(new Rect(rect.x + 16f, y, width - 32f, 50f), line, _option);
                y += 54f;
            }
        }

        private void EnsureStyles()
        {
            if (_box != null)
            {
                return;
            }

            _box = new GUIStyle(GUI.skin.box);
            _box.normal.background = Texture2D.grayTexture;

            _title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.85f, 0.55f) }
            };
            _body = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = new Color(0.9f, 0.9f, 0.88f) }
            };
            _option = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = new Color(0.8f, 0.95f, 0.85f) }
            };
        }
    }
}
