using System;
using System.Collections.Generic;
using GameDev.Core.Localization;
using GameDev.Locomotion;
using GameDev.Slice.Fights;
using GameDev.Slice.Stealth;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;

namespace GameDev.Slice.Beasts
{
    /// <summary>
    /// The beasts' territories in free movement (10.2 *Beasts of the slice*, S3b.2). Each pack, or a boar alone, holds a
    /// territory. A character not in Stealth Mode who comes inside is noticed; one in Stealth Mode only when a beast's
    /// Notice Meter fills, by 10.3 §3 with the beast's own Perception. Having noticed it, the pack warns at once and at the
    /// next tick, and attacks at the tick after, or at once when the character comes within 3 cells; leaving the territory
    /// ends the warning. After the fight a fallen beast's carcass lies where it fell, a broken pack's wolves run and are
    /// gone for the run, and the rest go back to rest. The seed makes two or three beasts glow, each alone: a glowing
    /// wolf's pack shuns it and is gone.
    /// </summary>
    public sealed class SliceBeasts : MonoBehaviour
    {
        private const float RescanSeconds = 2f;
        private const float LineSeconds = 3f;
        private const float WarnSeconds = 2f;    // the warning's clip
        private const float TurnRate = 240f;     // degrees a second toward the character it watches
        private const float WalkHome = 1.4f;     // m/s
        private const float FleeSpeed = 6f;      // m/s
        private const float FleeDistance = 45f;  // m away from the character before it is gone

        private readonly List<SliceBeast> _beasts = new List<SliceBeast>();
        private readonly SortedDictionary<int, Pack> _packs = new SortedDictionary<int, Pack>();
        private readonly Dictionary<SliceBeast, StealthObserver> _eyes = new Dictionary<SliceBeast, StealthObserver>();
        private readonly List<SliceBeast> _fighting = new List<SliceBeast>();
        private BeastRules _rules;
        private SliceStealth _hiding;
        private SliceConfront _fights;
        private IWorldClock _clock;
        private IWorldState _state;
        private IWorldSeed _seed;
        private PlayerLocomotion _player;
        private HashSet<int> _glowing;
        private float _rescan;

        public BeastRules Rules => _rules;

        public IReadOnlyList<SliceBeast> Beasts => _beasts;

        /// <summary>The packs the seed made glow, once the beasts were found; null before.</summary>
        public IReadOnlyCollection<int> GlowingPacks => _glowing;

        /// <summary>A pack warned: its number and the warning's, from 1.</summary>
        public event Action<int, int> Warned;

        /// <summary>A pack attacked: its number.</summary>
        public event Action<int> Attacked;

        public void Configure(BeastRules rules, SliceStealth hiding, SliceConfront fights, IWorldClock clock, IWorldState state, IWorldSeed seed)
        {
            _rules = rules;
            _hiding = hiding;
            _fights = fights;
            _clock = clock;
            _state = state;
            _seed = seed;
            if (_clock != null)
            {
                _clock.TickBoundary += OnTick;
            }

            if (_fights != null)
            {
                _fights.BeastFightEnded += OnFightEnded;
            }
        }

        /// <summary>The warnings a pack has given since it noticed the character.</summary>
        public int WarningsOf(int pack) => _packs.TryGetValue(pack, out var p) ? p.Warnings : 0;

        public bool HasNoticed(int pack) => _packs.TryGetValue(pack, out var p) && p.Noticed;

        /// <summary>The beast's Vision Cone and Notice Meter.</summary>
        public StealthObserver EyesOf(SliceBeast beast) => _eyes.TryGetValue(beast, out var o) ? o : null;

        /// <summary>Whether a place lies inside a beast's territory, measured on the ground.</summary>
        public bool Inside(SliceBeast beast, Vector3 at)
        {
            var way = at - beast.Home;
            way.y = 0f;
            return way.sqrMagnitude <= _rules.TerritoryRadius * _rules.TerritoryRadius;
        }

        private void Update()
        {
            if (_rules == null || _hiding == null || _hiding.Rules == null)
            {
                return;
            }

            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerLocomotion>();
                if (_player == null)
                {
                    return;
                }
            }

            _rescan -= Time.unscaledDeltaTime;
            if (_rescan <= 0f)
            {
                _rescan = RescanSeconds;
                Enlist();
            }

            if ((_fights != null && _fights.InFight) || (_clock != null && _clock.IsPaused))
            {
                return;
            }

            foreach (var pack in _packs.Values)
            {
                Look(pack, _player.transform.position, Time.deltaTime);
            }
        }

        /// <summary>One frame of a pack's watch over its territory.</summary>
        private void Look(Pack pack, Vector3 feet, float seconds)
        {
            if (pack.Attacking || !pack.Present(out var first))
            {
                return;
            }

            if (!Inside(first, feet))
            {
                pack.Noticed = false;
                pack.Warnings = 0;
                foreach (var b in pack.Members)
                {
                    EyesOf(b)?.Meter?.Unseen(seconds);
                }

                return;
            }

            StealthObserver noticer = null;
            foreach (var b in pack.Members)
            {
                if (!b.isActiveAndEnabled || b.State != BeastState.Resting || !(EyesOf(b) is StealthObserver o) || o.Meter == null)
                {
                    continue;
                }

                if (!_hiding.Active)
                {
                    noticer ??= o; // a character walking upright in a territory is noticed (S3b.2 working reading)
                }
                else if (_hiding.Sees(o, feet, true))
                {
                    if (o.Meter.See(_hiding.TimeToFill(o, feet), seconds, feet) || o.Meter.Noticed)
                    {
                        noticer ??= o;
                    }
                }
                else
                {
                    o.Meter.Unseen(seconds);
                }
            }

            if (!pack.Noticed && noticer != null)
            {
                pack.Noticed = true;
                if (_hiding.Active)
                {
                    _hiding.NoticedBy(noticer, Line(first));
                }

                Warn(pack);
            }

            if (!pack.Noticed)
            {
                return;
            }

            foreach (var b in pack.Members)
            {
                if (b.isActiveAndEnabled && b.State == BeastState.Resting)
                {
                    b.Face(feet, TurnRate * seconds);
                    var way = b.transform.position - feet;
                    way.y = 0f;
                    if (way.magnitude <= _rules.AttackCells)
                    {
                        Attack(pack);
                        return;
                    }
                }
            }
        }

        /// <summary>10.2: a pack that noticed the character warns once a tick while it stays inside, then attacks.</summary>
        private void OnTick(long tick)
        {
            if ((_fights != null && _fights.InFight) || _player == null)
            {
                return;
            }

            foreach (var pack in _packs.Values)
            {
                if (!pack.Noticed || pack.Attacking || !pack.Present(out var first) || !Inside(first, _player.transform.position))
                {
                    continue;
                }

                if (pack.Warnings < _rules.Warnings)
                {
                    Warn(pack);
                }
                else
                {
                    Attack(pack);
                }
            }
        }

        private void Warn(Pack pack)
        {
            pack.Warnings++;
            foreach (var b in pack.Members)
            {
                if (b.isActiveAndEnabled && b.State == BeastState.Resting)
                {
                    b.Act("Warn", WarnSeconds);
                    EyesOf(b)?.Say(Line(b), LineSeconds);
                }
            }

            Warned?.Invoke(pack.Number, pack.Warnings);
        }

        private static string Line(SliceBeast b) => Loc.Get(b.Kind == BeastKind.Wolf ? "ui.beast.growl" : "ui.beast.snort");

        private void Attack(Pack pack)
        {
            _fighting.Clear();
            foreach (var b in pack.Members)
            {
                if (b.isActiveAndEnabled && b.State == BeastState.Resting)
                {
                    b.Stop();
                    _fighting.Add(b);
                }
            }

            if (_fighting.Count > 0 && _fights != null && _fights.StartBeastFight(_fighting, _rules))
            {
                pack.Attacking = true;
                Attacked?.Invoke(pack.Number);
            }
        }

        /// <summary>After a beast fight: carcasses, a broken pack's flight, and the rest gone back to rest.</summary>
        private void OnFightEnded(IReadOnlyList<SliceBeast> fought)
        {
            var from = _player != null ? _player.transform.position : Vector3.zero;
            foreach (var b in fought)
            {
                var c = b != null ? b.Combatant : null;
                if (c == null)
                {
                    continue;
                }

                b.Combatant = null;

                if (c.Fighter.Down)
                {
                    b.Die(false);
                    _state?.Set(DeadKey(b), 1);
                }
                else if (c.Departure == GameDev.Confront.Turns.Departure.Fled)
                {
                    b.State = BeastState.Gone;
                    _state?.Set(GoneKey(b), 1);
                    var away = b.transform.position - from;
                    away.y = 0f;
                    away = away.sqrMagnitude > 1e-4f ? away.normalized : b.transform.forward;
                    b.Go(b.transform.position + (away * FleeDistance), FleeSpeed, true);
                }
                else
                {
                    b.Hp = c.Fighter.Hp;
                    b.Go(b.Spot, WalkHome, false);
                }
            }

            foreach (var pack in _packs.Values)
            {
                if (pack.Attacking)
                {
                    pack.Attacking = false;
                    pack.Noticed = false;
                    pack.Warnings = 0;
                    foreach (var b in pack.Members)
                    {
                        EyesOf(b)?.Meter?.Reset();
                    }
                }
            }
        }

        /// <summary>Finds the beasts in the loaded scenes, makes the seed's beasts glow, and restores what the run did to them.</summary>
        private void Enlist()
        {
            _beasts.RemoveAll(b => b == null);
            foreach (var pack in _packs.Values)
            {
                pack.Members.RemoveAll(b => b == null);
            }

            var found = new List<SliceBeast>();
            foreach (var b in FindObjectsByType<SliceBeast>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!_beasts.Contains(b))
                {
                    found.Add(b);
                }
            }

            if (found.Count == 0)
            {
                return;
            }

            found.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            foreach (var b in found)
            {
                _beasts.Add(b);
                if (!_packs.TryGetValue(b.Pack, out var pack))
                {
                    _packs[b.Pack] = pack = new Pack(b.Pack);
                }

                pack.Members.Add(b);
            }

            _glowing ??= Choose();
            foreach (var b in found)
            {
                Prepare(b);
            }
        }

        /// <summary>11: two or three territories, chosen by the seed, hold a glowing beast alone.</summary>
        private HashSet<int> Choose()
        {
            var packs = new List<int>(_packs.Keys);
            var rng = _seed != null ? _seed.Fork(SliceSeedStreams.GlowingBeasts) : new Random(0);
            var count = Mathf.Min(packs.Count, _rules.GlowingLeast + rng.Next(_rules.GlowingMost - _rules.GlowingLeast + 1));
            for (var i = 0; i < count; i++)
            {
                var j = i + rng.Next(packs.Count - i);
                (packs[i], packs[j]) = (packs[j], packs[i]);
            }

            return new HashSet<int>(packs.GetRange(0, count));
        }

        private void Prepare(SliceBeast b)
        {
            var pack = _packs[b.Pack];
            var glowing = _glowing.Contains(b.Pack);
            if (glowing && pack.Members[0] != b)
            {
                b.State = BeastState.Gone; // a pack shuns a glowing wolf (10.2)
                b.gameObject.SetActive(false);
                return;
            }

            if (glowing)
            {
                b.Glow(_rules.Glow, _rules.GlowLight, _rules.GlowRange, _rules.GlowEmission);
            }

            if (_state != null && _state.Get(GoneKey(b)) > 0)
            {
                b.State = BeastState.Gone;
                b.gameObject.SetActive(false);
                return;
            }

            if (_state != null && _state.Get(DeadKey(b)) > 0)
            {
                b.Die(true);
                return;
            }

            var eyes = b.GetComponent<StealthObserver>();
            if (eyes == null)
            {
                eyes = b.gameObject.AddComponent<StealthObserver>();
            }

            eyes.Managed = true;
            eyes.Perception = _rules.Of(b.Kind, glowing).perception;
            eyes.EyeHeight = _rules.EyesOf(b.Kind);
            eyes.Bind(_hiding.Rules);
            _eyes[b] = eyes;
        }

        public static string DeadKey(SliceBeast b) => $"beast.{b.Id}.dead";

        public static string GoneKey(SliceBeast b) => $"beast.{b.Id}.gone";

        private void OnEnable() => SceneManager.activeSceneChanged += OnScene;

        private void OnDisable() => SceneManager.activeSceneChanged -= OnScene;

        private void OnDestroy()
        {
            if (_clock != null)
            {
                _clock.TickBoundary -= OnTick;
            }

            if (_fights != null)
            {
                _fights.BeastFightEnded -= OnFightEnded;
            }
        }

        private void OnScene(Scene from, Scene to)
        {
            _player = null;
            _rescan = 0f;
        }

        private sealed class Pack
        {
            public Pack(int number) => Number = number;

            public int Number { get; }

            public List<SliceBeast> Members { get; } = new List<SliceBeast>();

            public bool Noticed { get; set; }

            public int Warnings { get; set; }

            public bool Attacking { get; set; }

            /// <summary>Whether a member stands in the scene, alive and at rest, the first of them given.</summary>
            public bool Present(out SliceBeast first)
            {
                foreach (var b in Members)
                {
                    if (b != null && b.isActiveAndEnabled && b.State == BeastState.Resting)
                    {
                        first = b;
                        return true;
                    }
                }

                first = null;
                return false;
            }
        }
    }
}
