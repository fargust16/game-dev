using System.Collections.Generic;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Input;
using GameDev.Items;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Slice.Shops;
using GameDev.UI;
using GameDev.World.Portals;
using UnityEngine;

namespace GameDev.Slice.Alchemy
{
    /// <summary>
    /// An alchemy bench (S5b.4; 20 §3 *Where brewing is done*; SLICE_TRIAL §4): <c>E  Brew at the bench</c> within reach opens
    /// its page of the formulas carried; a click brews the potion as the character stands at the bench and works it six
    /// times, each stroke's key frame bringing one Reaction Check with the world paused (07.1 §8a). The sixth makes the potion,
    /// then the brewing's 6 ticks pass with the world running, and the page opens again with what came of it. A fight that
    /// begins between two checks breaks the brewing off, nothing spent. E again, Escape, walking away or a fight closes the
    /// page. The driver puts one on Harrow's lab bench, on the faction house archive's table and on the desk of Veinmarket's
    /// den when their scenes load, so no scene is changed for it.
    /// </summary>
    public sealed class AlchemyBench : MonoBehaviour, IPromptSource
    {
        /// <summary>
        /// Authoring default, m: how far from the bench's edge the character may stand to use it. A kit piece's pivot is
        /// its footprint's corner, often by a wall, so the reach is measured from what the bench occupies, not its pivot.
        /// </summary>
        public const float Reach = 1.2f;

        /// <summary>Authoring default, m: how far beyond the bench's edge the character stands, the hands reaching over it.</summary>
        public const float WorkReach = 0.35f;

        /// <summary>The work clip of a brewing check (ANIMATION_PIPELINE_PLAN.md §8: Q1 Interact, kept for brewing).</summary>
        public const string Clip = "Interact";

        /// <summary>As every interaction's (<see cref="InteractReach"/>), m: the height within which the character counts as level with it.</summary>
        private const float SameLevel = 1.5f;

        private PlayerLocomotion _player;
        private HudView _hud;
        private Portal[] _doors;
        private string _note;
        private bool _open;
        private Transform _middle;
        private Bounds _footprint;

        public BenchKind Kind { get; private set; }

        public bool IsOpen => _open && _hud != null && _hud.IsShopOpen;

        public void Configure(BenchKind kind)
        {
            Kind = kind;
            Measure();
        }

        /// <summary>
        /// Within <see cref="Reach"/> of the bench's edge, level with it, and no wall or floor between: the line at the
        /// bench's middle height meets the bench first.
        /// </summary>
        public bool InReach(Vector3 position)
        {
            if (_middle == null)
            {
                Measure();
            }

            var b = _footprint;
            if (Mathf.Abs(position.y - b.min.y) > SameLevel)
            {
                return false;
            }

            var dx = Mathf.Max(0f, Mathf.Abs(position.x - b.center.x) - b.extents.x);
            var dz = Mathf.Max(0f, Mathf.Abs(position.z - b.center.z) - b.extents.z);
            if ((dx * dx) + (dz * dz) > Reach * Reach)
            {
                return false;
            }

            var walls = LayerMask.GetMask("Ground", "Structure");
            var from = new Vector3(position.x, b.center.y, position.z);
            return !Physics.Linecast(from, b.center, out var hit, walls, QueryTriggerInteraction.Ignore) || hit.collider.transform.IsChildOf(transform);
        }

        /// <summary>The bench's footprint from its solid colliders (its renderers where it has none), and a point at its middle on the floor.</summary>
        private void Measure()
        {
            var any = false;
            foreach (var c in GetComponentsInChildren<Collider>())
            {
                if (c.enabled && !c.isTrigger)
                {
                    _footprint = any ? Grow(_footprint, c.bounds) : c.bounds;
                    any = true;
                }
            }

            if (!any)
            {
                foreach (var r in GetComponentsInChildren<Renderer>())
                {
                    _footprint = any ? Grow(_footprint, r.bounds) : r.bounds;
                    any = true;
                }
            }

            if (!any)
            {
                _footprint = new Bounds(transform.position, new Vector3(0.6f, 1f, 0.6f));
            }

            if (_middle == null)
            {
                _middle = new GameObject("BenchMiddle").transform;
                _middle.SetParent(transform, false);
            }

            _middle.position = new Vector3(_footprint.center.x, _footprint.min.y, _footprint.center.z);
        }

        private static Bounds Grow(Bounds bounds, Bounds more)
        {
            bounds.Encapsulate(more);
            return bounds;
        }

        /// <summary>How far the bench's edge lies from its middle toward <paramref name="from"/>, m.</summary>
        private float EdgeToward(Vector3 from)
        {
            var toward = from - _footprint.center;
            toward.y = 0f;
            if (toward.sqrMagnitude < 1e-6f)
            {
                return Mathf.Min(_footprint.extents.x, _footprint.extents.z);
            }

            toward.Normalize();
            var alongX = Mathf.Abs(toward.x) > 1e-4f ? _footprint.extents.x / Mathf.Abs(toward.x) : float.MaxValue;
            var alongZ = Mathf.Abs(toward.z) > 1e-4f ? _footprint.extents.z / Mathf.Abs(toward.z) : float.MaxValue;
            return Mathf.Min(alongX, alongZ);
        }

        /// <summary>
        /// Puts the den's bench on its desk when the shared Dwelling scene builds Veinmarket's den, to one who heard of it
        /// (SLICE_TRIAL §4); the Dwelling scene calls it with every dwelling it builds. True when a bench was put.
        /// </summary>
        public static bool AtDwelling(string dwellingId, Transform built)
        {
            var world = SliceWorldDriver.Running;
            if (dwellingId != SliceAlchemy.DenDwelling || built == null || world == null || world.Alchemy == null || !world.Alchemy.KnowsDen)
            {
                return false;
            }

            var desk = built.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("Kit_") && t.name.Contains("_Desk"));
            if (desk == null || desk.GetComponent<AlchemyBench>() != null)
            {
                return false;
            }

            desk.gameObject.AddComponent<AlchemyBench>().Configure(BenchKind.Den);
            FindAnyObjectByType<HudView>()?.FindPromptSources();
            return true;
        }

        public string PromptFor(Vector3 player)
        {
            var world = SliceWorldDriver.Running;
            if (world == null || world.Alchemy == null || IsOpen || Working(world) || !InReach(player) || Busy())
            {
                return null;
            }

            var fault = world.Alchemy.BenchFault(Kind);
            return fault switch
            {
                null or "ui.brew.fault.lesson" => Loc.Get("ui.prompt.brew"),
                "ui.brew.fault.closed" => Loc.Format("ui.prompt.brew_closed", world.Alchemy.Opens(Kind)),
                "ui.brew.fault.member" => Loc.Get("ui.prompt.brew_members"),
                "ui.brew.fault.barred" => Loc.Get("ui.prompt.brew_barred"),
                _ => null,
            };
        }

        /// <summary>Opens the page, as E does.</summary>
        public bool Open()
        {
            var world = SliceWorldDriver.Running;
            _hud ??= FindAnyObjectByType<HudView>();
            if (world == null || world.Alchemy == null || _hud == null || Busy() || Working(world))
            {
                return false;
            }

            var fault = world.Alchemy.BenchFault(Kind);
            if (fault != null && fault != "ui.brew.fault.lesson")
            {
                return false;
            }

            _open = true;
            Show();
            return true;
        }

        public void Close()
        {
            if (_open && _hud != null)
            {
                _hud.CloseShop();
            }

            _open = false;
        }

        private static bool Working(SliceWorldDriver world) => (world.Reaction != null && world.Reaction.Running) || (world.Work != null && world.Work.Busy);

        private static bool Busy()
        {
            var world = SliceWorldDriver.Running;
            return world != null && world.Fights != null && world.Fights.InFight;
        }

        /// <summary>Every formula carried, once Alchemy is learned.</summary>
        private void Show()
        {
            var world = SliceWorldDriver.Running;
            var alchemy = world.Alchemy;
            var fee = SliceAlchemy.Fee(Kind);
            var heading = fee > 0 ? Loc.Format("ui.brew.heading", Coins.Show(fee)) : Loc.Get("ui.brew.heading.free");
            var lines = new List<PageLine> { new PageLine(heading, heading: true) };
            if (!alchemy.Knows)
            {
                lines.Add(new PageLine(Loc.Get("ui.brew.unlearned")));
            }
            else if (!alchemy.Known.Any())
            {
                lines.Add(new PageLine(Loc.Get("ui.brew.no_formula")));
            }
            else
            {
                foreach (var potion in alchemy.Known)
                {
                    var p = potion;
                    var parts = string.Join(", ", p.recipe.Select(r => Loc.Format("ui.craft.part", r.count, Loc.Get("item." + r.item))));
                    lines.Add(new PageLine(Loc.Format("ui.brew.recipe", Loc.Get(p.NameKey), parts, SliceAlchemy.BrewTicks), () => Brew(p)));
                }
            }

            _hud.ShowShop(Loc.Get(TitleKey(Kind)), BagPage.Money(world.Bag), lines, _note ?? Loc.Get("ui.brew.hint"));
        }

        public static string TitleKey(BenchKind kind) => "ui.brew.title." + kind.ToString().ToLowerInvariant();

        /// <summary>
        /// Brews the potion: six strokes at the bench, a check at each one's key frame, then the potion and the brewing's
        /// ticks; a fight between two checks breaks it off with nothing spent.
        /// </summary>
        private void Brew(ItemDefinition potion)
        {
            var world = SliceWorldDriver.Running;
            var alchemy = world.Alchemy;
            var fault = alchemy.Fault(Kind, potion);
            if (fault != null || world.Work == null)
            {
                _note = Loc.Get(fault ?? "ui.brew.interrupted");
                Show();
                return;
            }

            Close();
            if (_middle == null)
            {
                Measure();
            }

            _player ??= FindAnyObjectByType<PlayerLocomotion>();
            var standOff = (_player != null ? EdgeToward(_player.transform.position) : PlayerWork.Radius(transform)) + WorkReach;
            var results = new List<ReactionResult>();
            var stroke = new WorkStroke
            {
                Clip = Clip,
                Steady = true,
                Bar = () => alchemy.Bar(potion, results.Count),
                CaptionKey = "ui.brew.caption",
                Settle = results.Add,
                Again = () => results.Count < SliceAlchemy.Checks && !Busy(),
                CalledOff = Busy,
                Ended = broken => Finish(potion, results),
                Face = _middle,
                StandOff = standOff,
            };
            if (!world.Work.Begin(stroke))
            {
                _note = Loc.Get("ui.brew.interrupted");
                Open();
            }
        }

        private void Finish(ItemDefinition potion, IReadOnlyList<ReactionResult> results)
        {
            var world = SliceWorldDriver.Running;
            if (world == null || world.Alchemy == null)
            {
                return;
            }

            if (results.Count < SliceAlchemy.Checks || world.Alchemy.Brew(Kind, potion, results, out var made) != BrewOutcome.Brewed)
            {
                _note = Loc.Get("ui.brew.interrupted");
                Open();
                return;
            }

            var ticks = world.Alchemy.PassTicks(Busy);
            _note = Loc.Format(ticks < SliceAlchemy.BrewTicks ? "ui.brew.made_cut" : "ui.brew.made", BagPage.Potion(potion, made), ticks);
            _hud ??= FindAnyObjectByType<HudView>();
            _hud?.ShowMessage(Loc.Format("ui.brew.message", Loc.Get(potion.NameKey), made.perfect, made.bad));
            Open();
        }

        private void Update()
        {
            if (!_open)
            {
                Listen();
                return;
            }

            var input = GameInputService.Instance;
            _player ??= FindAnyObjectByType<PlayerLocomotion>();
            var world = SliceWorldDriver.Running;
            var fault = world != null && world.Alchemy != null ? world.Alchemy.BenchFault(Kind) : "ui.brew.fault.closed";
            if (_hud == null || !_hud.IsShopOpen || _player == null || !InReach(_player.transform.position) || Busy()
                || (fault != null && fault != "ui.brew.fault.lesson") || (input != null && (input.CancelPressed || input.InteractPressed)))
            {
                Close();
            }
        }

        /// <summary>E within reach opens the page while the HUD shows the bench's prompt, so a counter or a door beside it keeps E.</summary>
        private void Listen()
        {
            var input = GameInputService.Instance;
            _player ??= FindAnyObjectByType<PlayerLocomotion>();
            if (input == null || _player == null || !input.InteractPressed || !InReach(_player.transform.position))
            {
                return;
            }

            _hud ??= FindAnyObjectByType<HudView>();
            if (_hud != null && _hud.PromptText != Loc.Get("ui.prompt.brew"))
            {
                return;
            }

            _doors ??= FindObjectsByType<Portal>(FindObjectsInactive.Exclude);
            if (!_doors.Any(d => d != null && d.isActiveAndEnabled && d.InReach(_player.transform.position)))
            {
                _note = null;
                Open();
            }
        }

        private void OnEnable() => Loc.Changed += Relabel;

        private void OnDisable()
        {
            Loc.Changed -= Relabel;
            Close();
        }

        private void Relabel()
        {
            if (IsOpen)
            {
                Show();
            }
        }
    }
}
