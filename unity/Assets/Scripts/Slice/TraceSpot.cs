using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Input;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.UI;
using GameDev.World.Poi;
using GameDev.World.Portals;
using GameDev.World.State;
using UnityEngine;
using Random = System.Random;

namespace GameDev.Slice
{
    /// <summary>
    /// A Physical Trace where it lies (09 §7; 11.1 POI C; VERTICAL_SLICE_IMPLEMENTATION_PLAN S4.1): it lies once its POI has
    /// opened while any of its conditions holds, the outcome of an earlier POI that left it, until a sweeper clears it.
    /// Interact in reach, the nearest trace taking it, shows what is seen in the HUD's plain panel and hears its line as a
    /// tip, so the fact enters the Character Notes and the first trace marks its POI. A trace that allows a close look
    /// offers it once afterwards: minutes pass and an action check of 07.1 decides between the solid tell and a
    /// misreading that still notices something. Reading does not pause the world.
    /// </summary>
    public sealed class TraceSpot : MonoBehaviour, IPromptSource
    {
        private static readonly List<TraceSpot> Spots = new List<TraceSpot>();

        [SerializeField] private PoiDefinition poi;

        [Tooltip("The tip it tells, a Physical Trace in the catalogue, e.g. trace.chalk.")]
        [SerializeField] private string tip = "";

        [Tooltip("It lies while any of these holds; none for always once its POI has opened.")]
        [SerializeField] private StateCondition[] anyOf = new StateCondition[0];

        [Tooltip("Authoring default: the tick a sweeper clears it, minutes from day 1's midnight; -1 for never (11.1: the chalk).")]
        [SerializeField] private long clearedAt = -1;

        [Tooltip("What is seen, shown while it lies.")]
        [SerializeField] private GameObject look;

        [Tooltip("Authoring default, m: horizontal reach from this point.")]
        [SerializeField] private float interactRadius = 1.2f;

        [Header("A close look (11.1 POI C, Study the Physical Trace)")]
        [SerializeField] private bool closeLook;

        [Tooltip("Told when the close look succeeds.")]
        [SerializeField] private string linkTip = "";

        [Tooltip("Told when it fails: a misreading that still notices something (09 §7).")]
        [SerializeField] private string misreadTip = "";

        [Tooltip("Raised once the close look is taken: it is tried once.")]
        [SerializeField] private string lookedKey = "";

        [Tooltip("Authoring default: the close look's difficulty on 07.1's scale, Perception alone with Intelligence as its secondary attribute.")]
        [SerializeField] private int lookDifficulty = 3;

        [Tooltip("Authoring default: game minutes the close look takes.")]
        [Min(0)] [SerializeField] private int lookMinutes = 20;

        private PlayerLocomotion _player;
        private HudView _hud;
        private Portal[] _doors;
        private string _shownTip;
        private bool _lying;
        private bool _applied;

        public PoiDefinition Poi => poi;

        public string Tip => tip;

        public bool CloseLook => closeLook;

        public bool IsOpen { get; private set; }

        /// <summary>Whether it lies where it is now.</summary>
        public bool Lying => _lying;

        /// <summary>Setup for scenes built by code; a placed trace is set in the Inspector.</summary>
        public void Configure(PoiDefinition definition, string tipId, StateCondition[] conditions, long clearedAtTick, GameObject shown, float radius)
        {
            poi = definition;
            tip = tipId;
            anyOf = conditions ?? new StateCondition[0];
            clearedAt = clearedAtTick;
            look = shown;
            interactRadius = radius;
        }

        /// <summary>Setup of a close look for scenes built by code.</summary>
        public void AllowCloseLook(string link, string misread, string looked, int difficulty, int minutes)
        {
            closeLook = true;
            linkTip = link;
            misreadTip = misread;
            lookedKey = looked;
            lookDifficulty = difficulty;
            lookMinutes = minutes;
        }

        /// <summary>
        /// Whether a trace lies: its POI has opened (it may have resolved since: what was left stays), any of its
        /// conditions holds or it has none, and its sweeper has not come yet.
        /// </summary>
        public static bool Lies(IWorldState state, PoiPhase phase, long tick, IReadOnlyList<StateCondition> conditions, long clearedAtTick) =>
            phase != PoiPhase.Pending
            && (conditions == null || conditions.Count == 0 || conditions.Any(c => c.Holds(state)))
            && (clearedAtTick < 0 || tick < clearedAtTick);

        /// <summary>
        /// The close look's chance (07.1 §4–6): Perception alone against the difficulty, Intelligence its secondary
        /// attribute, Luck; it has no critical outcome.
        /// </summary>
        public static double LookChance(ActionCheckRules rules, ICharacterValues character, int difficulty) =>
            ActionCheck.Chances(rules, character.Get(CharacterAttribute.Perception), difficulty, character.Get(CharacterAttribute.Perception),
                character.Get(CharacterAttribute.Luck), false, new[] { character.Get(CharacterAttribute.Intelligence) }).Success;

        /// <summary>Whether the close look succeeds, one draw from <paramref name="rolls"/>.</summary>
        public static bool LookSucceeds(ActionCheckRules rules, ICharacterValues character, int difficulty, Random rolls) =>
            rolls.NextDouble() < LookChance(rules, character, difficulty);

        public bool InReach(Vector3 position) => InteractReach.Within(transform, position, interactRadius);

        public string PromptFor(Vector3 player)
        {
            if (IsOpen || !_lying || !InReach(player) || Nearest(player) != this)
            {
                return null;
            }

            return Loc.Get(CanLookCloser() ? "ui.prompt.look_closer" : "ui.prompt.examine");
        }

        /// <summary>What Interact does: closes an open panel, or shows what is seen, telling its fact the first time and taking the close look after.</summary>
        public void Use()
        {
            if (IsOpen)
            {
                Close();
                return;
            }

            var world = SliceWorldDriver.Running;
            if (world == null || Hud() == null || !_lying)
            {
                return;
            }

            var heard = world.Tips.Find(tip);
            if (heard != null && !world.Tips.HasHeard(heard))
            {
                world.Tips.Tell(tip, world.Clock.Tick);
                _shownTip = tip;
            }
            else if (CanLookCloser())
            {
                _shownTip = TakeCloseLook(world);
            }
            else
            {
                _shownTip = LastHeard(world);
            }

            IsOpen = true;
            Show();
        }

        private bool CanLookCloser()
        {
            var world = SliceWorldDriver.Running;
            var found = world?.Tips.Find(tip);
            return closeLook && world != null && found != null && world.Tips.HasHeard(found)
                   && !string.IsNullOrEmpty(lookedKey) && !world.State.IsSet(lookedKey) && world.ActionChecks != null;
        }

        // 07.1 §8: one draw, from the action checks' stream forked by the trace and the tick, so no two looks share it.
        private string TakeCloseLook(SliceWorldDriver world)
        {
            var rolls = world.Seed.Fork(SliceSeedStreams.ActionChecks, GameDev.World.Seed.StableHash.Of(tip), (int)world.Clock.Tick);
            var told = LookSucceeds(world.ActionChecks, world.Character, lookDifficulty, rolls) ? linkTip : misreadTip;
            world.State.Raise(lookedKey);
            world.Tips.Tell(told, world.Clock.Tick);
            if (lookMinutes > 0)
            {
                world.Clock.AdvanceTicks(lookMinutes);
            }

            return told;
        }

        // A trace looked at again shows what was last made of it: the close look's reading, or its first sight.
        private string LastHeard(SliceWorldDriver world)
        {
            foreach (var id in new[] { linkTip, misreadTip })
            {
                var found = string.IsNullOrEmpty(id) ? null : world.Tips.Find(id);
                if (found != null && world.Tips.HasHeard(found))
                {
                    return id;
                }
            }

            return tip;
        }

        private void Show()
        {
            var world = SliceWorldDriver.Running;
            var found = world?.Tips.Find(_shownTip);
            if (IsOpen && found != null)
            {
                Hud()?.ShowReading(Loc.Get("ui.trace.title"), found.Line);
            }
        }

        private void Close()
        {
            IsOpen = false;
            Hud()?.CloseReading();
        }

        private void Update()
        {
            var world = SliceWorldDriver.Running;
            var lying = world != null && poi != null && Lies(world.State, world.Director.Phase(poi), world.Clock.Tick, anyOf, clearedAt);
            if (!_applied || lying != _lying)
            {
                _applied = true;
                _lying = lying;
                if (look != null)
                {
                    look.SetActive(lying);
                }
            }

            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerLocomotion>();
                if (_player == null)
                {
                    return;
                }
            }

            // Closed from outside, by Esc or by another window (owner, 2026-11-12): the next E opens it again, not closes nothing.
            if (IsOpen && Hud() != null && !Hud().IsReadingOpen)
            {
                IsOpen = false;
            }

            var at = _player.transform.position;
            var inReach = _lying && InReach(at);
            if (IsOpen && !inReach)
            {
                Close();
                return;
            }

            // The nearest trace takes Interact, and a door in reach takes it before any (as PersonTalk).
            var input = GameInputService.Instance;
            _doors ??= FindObjectsByType<Portal>(FindObjectsInactive.Exclude);
            if (inReach && input != null && input.InteractPressed && Nearest(at) == this
                && !_doors.Any(d => d != null && d.isActiveAndEnabled && d.InReach(at)))
            {
                Use();
            }
        }

        private static TraceSpot Nearest(Vector3 at)
        {
            TraceSpot best = null;
            var bestDistance = float.MaxValue;
            foreach (var spot in Spots)
            {
                if (!spot._lying || !spot.InReach(at))
                {
                    continue;
                }

                var distance = (spot.transform.position - at).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = spot;
                }
            }

            return best;
        }

        private void OnEnable()
        {
            Spots.Add(this);
            Loc.Changed += Show;
        }

        private void OnDisable()
        {
            Spots.Remove(this);
            Loc.Changed -= Show;
            if (IsOpen)
            {
                Close();
            }
        }

        private HudView Hud()
        {
            if (_hud == null)
            {
                _hud = FindAnyObjectByType<HudView>();
            }

            return _hud;
        }
    }
}
