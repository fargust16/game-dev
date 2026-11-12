using System;
using System.Collections.Generic;
using GameDev.Confront.Grid;
using GameDev.Core.Localization;
using GameDev.Locomotion;
using GameDev.Slice.Fights;
using GameDev.Slice.Kit;
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
    /// The beasts' territories in free movement (10.2 *Beasts of the slice*, S3b.2; the owner's play test of S5a). Each
    /// pack, or a boar alone, holds a territory 25 m round its den and wanders in it, the pack together, to a place the
    /// seed picks every 8–20 s. Each beast's Vision Cone shows near the character. A character not in Stealth Mode is
    /// noticed when it comes into a cone inside the territory; one in Stealth Mode only when a beast's Notice Meter fills,
    /// by 10.3 §3 with the beast's own Perception. Having noticed it, the pack warns at once and at the next tick, and
    /// attacks at the tick after, or at once when the character comes within 3 cells; leaving the territory ends the
    /// warning. An attack starts the fight when the character is within 10 cells; farther, the pack runs at it until it
    /// is, and gives up and goes home when the character leaves its territory. After the fight a fallen beast's
    /// carcass lies where it fell, a broken pack's wolves run and are gone for the run, and the rest go back to rest. The
    /// seed makes two or three beasts glow, each alone: a glowing wolf's pack shuns it and is gone.
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

        /// <summary>10.2 (owner, 2026-11-11): an attack starts the fight when the character is within this many cells.</summary>
        public const float FightCells = 10f;

        /// <summary>Authoring default, 10.2: a chasing beast's run, m/s; a sprinting character outruns it.</summary>
        public const float ChaseSpeed = 6f;

        /// <summary>Authoring defaults, 10.2: a pack sets out to a new place in its territory every 8 to 20 s.</summary>
        public const float WanderLeast = 8f;

        public const float WanderMost = 20f;

        private const float MaxSlope = 25f;      // a wander's place: ground the beast stands on (SliceBeastsSetup's)
        private const float BodyRadius = 0.5f;

        // A pack's places round its leader on a wander, east and north, as round its den (SliceBeastsSetup).
        private static readonly Vector3[] Around = { Vector3.zero, new Vector3(2.5f, 0f, -1.5f), new Vector3(-1.5f, 0f, -2.5f) };

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

        /// <summary>Whether the pack runs at the character, having decided to attack it from beyond 10 cells.</summary>
        public bool IsChasing(int pack) => _packs.TryGetValue(pack, out var p) && p.Chasing;

        /// <summary>Seconds until the pack sets out on its next wander.</summary>
        public float WanderIn(int pack) => _packs.TryGetValue(pack, out var p) ? p.WanderIn : 0f;

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

            var feet = _player.transform.position;
            foreach (var pack in _packs.Values)
            {
                if (pack.Chasing)
                {
                    Chase(pack, feet);
                    continue;
                }

                // 10.1: a swimmer is out of a beast's reach, and not noticed.
                if (!_player.InWater)
                {
                    Look(pack, feet, Time.deltaTime);
                }

                Roam(pack, Time.deltaTime);
            }
        }

        /// <summary>A pack at rest that has noticed nobody sets out on a wander when its time comes.</summary>
        private void Roam(Pack pack, float seconds)
        {
            if (pack.Noticed || pack.Attacking)
            {
                return;
            }

            pack.WanderIn -= seconds;
            if (pack.WanderIn <= 0f)
            {
                Wander(pack.Number);
            }
        }

        /// <summary>
        /// 10.2: the pack walks together to a place in its territory, 25 m round its den, that the seed picks, a place its
        /// leader can stand on above the water; its next wander comes 8–20 s later. False when it stays.
        /// </summary>
        public bool Wander(int number)
        {
            if (!_packs.TryGetValue(number, out var pack) || pack.Attacking || pack.Chasing || !pack.Present(out var first))
            {
                return false;
            }

            var rng = _seed != null ? _seed.Fork(SliceSeedStreams.BeastWander, pack.Number, pack.Wanders) : new Random(pack.Number * 7919 + pack.Wanders);
            pack.Wanders++;
            pack.WanderIn = WanderLeast + ((float)rng.NextDouble() * (WanderMost - WanderLeast));
            for (var tries = 0; tries < 6; tries++)
            {
                var angle = rng.NextDouble() * Math.PI * 2d;
                var reach = _rules.TerritoryRadius * Math.Sqrt(rng.NextDouble());
                var place = first.Home + new Vector3((float)(Math.Cos(angle) * reach), 0f, (float)(Math.Sin(angle) * reach));
                if (!Standable(ref place))
                {
                    continue;
                }

                var k = 0;
                foreach (var b in pack.Members)
                {
                    if (b.isActiveAndEnabled && b.State == BeastState.Resting)
                    {
                        b.Go(place + Around[k % Around.Length], WalkHome, false);
                        k++;
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>Ground a beast can stand on: under 25°, clear of trunks and rocks, and above the water.</summary>
        private static bool Standable(ref Vector3 place)
        {
            var ground = ~LayerMask.GetMask(KitLayers.Character, KitLayers.NoGrid);
            if (!Physics.Raycast(place + (Vector3.up * 40f), Vector3.down, out var hit, 80f, ground, QueryTriggerInteraction.Ignore)
                || Vector3.Angle(hit.normal, Vector3.up) > MaxSlope
                || hit.point.y < CombatGridReader.SeaLevel + 0.3f
                || Physics.CheckCapsule(hit.point + (Vector3.up * 0.85f), hit.point + (Vector3.up * 1.2f), BodyRadius, ground, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            place = hit.point;
            return true;
        }

        /// <summary>
        /// 10.2: a pack that decided to attack from beyond 10 cells runs at the character; within 10 cells the fight
        /// begins, and when the character leaves its territory it gives up and goes home, as 10.2 has it.
        /// </summary>
        private void Chase(Pack pack, Vector3 feet)
        {
            if (!pack.Present(out var first))
            {
                pack.Chasing = false;
                return;
            }

            // 10.1: a beast gives up a chase at deep water, as at its territory's edge.
            if (!Inside(first, feet) || _player.InWater)
            {
                GiveUp(pack);
                return;
            }

            if (Nearest(pack, feet) <= FightCells)
            {
                pack.Chasing = false;
                Attack(pack);
                return;
            }

            foreach (var b in pack.Members)
            {
                if (b.isActiveAndEnabled && b.State == BeastState.Resting)
                {
                    b.Go(feet, ChaseSpeed, false);
                }
            }
        }

        private void GiveUp(Pack pack)
        {
            pack.Chasing = false;
            pack.Noticed = false;
            pack.Warnings = 0;
            foreach (var b in pack.Members)
            {
                if (b.isActiveAndEnabled && b.State == BeastState.Resting)
                {
                    b.Go(b.Spot, WalkHome, false);
                }

                EyesOf(b)?.Meter?.Reset();
            }
        }

        /// <summary>The distance from the character to the pack's nearest beast at rest, m on the ground.</summary>
        private static float Nearest(Pack pack, Vector3 feet)
        {
            var nearest = float.MaxValue;
            foreach (var b in pack.Members)
            {
                if (b.isActiveAndEnabled && b.State == BeastState.Resting)
                {
                    nearest = Mathf.Min(nearest, Flat(b.transform.position - feet).magnitude);
                }
            }

            return nearest;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

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
                    // A character walking upright is noticed when it comes into a cone (10.2, the play test of S5a).
                    if (_hiding.Sees(o, feet, false))
                    {
                        noticer ??= o;
                    }
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
                foreach (var b in pack.Members)
                {
                    b.Stop(); // off its wander, to face the character
                }

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
                if (_player.InWater && pack.Noticed && !pack.Attacking && !pack.Chasing)
                {
                    GiveUp(pack);
                    continue;
                }

                if (!pack.Noticed || pack.Attacking || pack.Chasing || !pack.Present(out var first) || !Inside(first, _player.transform.position))
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
            // 10.2: the fight begins within 10 cells; farther, the pack runs at the character first.
            if (_player != null && Nearest(pack, _player.transform.position) > FightCells)
            {
                pack.Chasing = true;
                return;
            }

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
                    Blind(b);
                }
                else if (c.Departure == GameDev.Confront.Turns.Departure.Fled)
                {
                    b.State = BeastState.Gone;
                    _state?.Set(GoneKey(b), 1);
                    Blind(b);
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
                    pack.Chasing = false;
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

                    // Its first wander after the least wait, spread by the pack's number so the woods do not move at once.
                    pack.WanderIn = WanderLeast + ((b.Pack * 2.3f) % (WanderMost - WanderLeast));
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
            eyes.ConeAlwaysShown = true;
            eyes.enabled = true;
            eyes.Perception = _rules.Of(b.Kind, glowing).perception;
            eyes.EyeHeight = _rules.EyesOf(b.Kind);
            eyes.Bind(_hiding.Rules);
            _eyes[b] = eyes;
        }

        /// <summary>A fallen or fleeing beast notices no more, and its cone goes.</summary>
        private void Blind(SliceBeast b)
        {
            if (EyesOf(b) is StealthObserver eyes)
            {
                eyes.enabled = false;
            }
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

            public bool Chasing { get; set; }

            public float WanderIn { get; set; }

            public int Wanders { get; set; }

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
