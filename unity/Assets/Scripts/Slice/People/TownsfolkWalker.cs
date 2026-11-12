using System.Collections.Generic;
using System.Linq;
using GameDev.Slice.Kit;
using GameDev.World.Timekeeping;
using UnityEngine;

namespace GameDev.Slice.People
{
    /// <summary>
    /// An ordinary townsperson walking a closed route, round and round (owner, 2026-11-06): a stand-in for the schedules
    /// of S5c, which will replace the route. The route is planned in the editor over walkable ground, so the walker only
    /// follows it, keeps its feet on the ground under it, and turns smoothly. People do not collide with one another.
    /// <para>
    /// The walker keeps the world's time (owner's play test of 2026-11-11: townsfolk walked on through a fight). While the
    /// clock is paused, by a fight or a conversation, it stands still mid-stride; each whole tick the clock is moved while
    /// paused, a fight's round or a talk's end, it walks that tick's time at once, faster than life, so the world moves
    /// only after the round. Everything it does below takes its time from the same seconds.
    /// </para>
    /// <para>
    /// At one turn of its route in three the walker stops and stands 4 to 12 s in one of four idles before it walks on (the
    /// owner's play test of S5a: townsfolk who sometimes stop), the same turns and idles on every run.
    /// </para>
    /// <para>
    /// AN6 (the owner's direction of 2026-11-11, "random activities"): at a turn from which the editor found a clear walk to
    /// an <see cref="ActivitySpot"/>, one turn in two the walker goes there if the place is free, passes a while there, and
    /// walks back to that turn: it sits on a bench and talks while someone else sits there, bargains at a stall, or goes
    /// in at the tavern's or the chapel's door and stays inside out of sight. Two walkers who meet face to face stop and
    /// nod to one another, each at most once in 45 s. A person with no route keeps its <see cref="Home"/> place for ever:
    /// seated, at a bar, or standing quietly in the chapel.
    /// </para>
    /// </summary>
    public sealed class TownsfolkWalker : MonoBehaviour
    {
        /// <summary>How many times faster than life a walker walks the ticks a paused world was moved by (authoring default).</summary>
        public const float CatchUp = 6f;

        [Tooltip("The closed route in world space; the last point leads back to the first.")]
        [SerializeField] private Vector3[] route = new Vector3[0];

        [Tooltip("Walking speed, m/s: measured from the walk clip's stride so the feet do not slide.")]
        [SerializeField] private float speed = 1.2f;

        [Tooltip("What the feet stand on.")]
        [SerializeField] private LayerMask ground = ~0;

        [SerializeField] private float turnDegreesPerSecond = 270f;

        [Tooltip("A person who walks no route keeps this place for ever, as a drinker in the Soot & Bell (AN6).")]
        [SerializeField] private ActivitySpot home;

        [SerializeField] private int homePlace;

        /// <summary>Authoring default, the owner's play test of S5a: one turn in this many brings a stop.</summary>
        public const int StopOneIn = 3;

        /// <summary>Authoring default: a stop's shortest and longest standing, s.</summary>
        public const float StopShortest = 4f;

        public const float StopLongest = 12f;

        /// <summary>Authoring default (AN6): at a turn with a visit, one turn in this many goes on it.</summary>
        public const int VisitOneIn = 2;

        /// <summary>Authoring defaults (AN6): two walkers greet within this many metres, facing one another, at most once in <see cref="GreetEvery"/> s each.</summary>
        public const float GreetReach = 2.2f;

        public const float GreetEvery = 45f;

        /// <summary>The player man the sitting clips were measured on, m (SlicePeopleModels.PlayerMan).</summary>
        public const float MeasuredStature = 1.8f;

        /// <summary>Measured: the player man's hips over the root, standing in the idle, m; a body's own over this scales its seat.</summary>
        public const float StandingHipsUp = 0.97f;

        /// <summary>
        /// Measured on the sitting clips (SliceAnimationCheck, animation_check.txt): seated, the hips stand this high over
        /// the root and this far behind it, for a body of <see cref="MeasuredStature"/>.
        /// </summary>
        public const float SeatedHipsUp = 0.61f;

        public const float SeatedHipsBack = 0.26f;

        /// <summary>
        /// Seat sizes (the owner, 2026-11-12, from the seated bodies measured in play, ANIMATION_PIPELINE_PLAN §13): a
        /// seated body keeps its feet on the floor, its hip joint 0.54–0.61 m up and the skin under it 0.35–0.45 m up, so a
        /// seat's top stands 0.42 m over the floor (0.40–0.45), 0.45 m from its front edge to its back, the hips this far
        /// behind the front edge: its back 0.14–0.21 m behind them, its knees 0.40–0.46 m before them.
        /// </summary>
        public const float SeatTop = 0.42f;

        public const float HipsBehindFront = 0.225f;

        /// <summary>
        /// The seats a walker sits on, m over the floor: the kit's seats, scaled to SeatTop on the owner's trial (0.40–0.42 m,
        /// 99 VS-SEAT), with room for a seat at the old 0.50–0.56 m and a street bench on a slope standing higher over the
        /// ground under its place; never a stool (0.70 m) or a footstool (0.34 m) (authoring defaults).
        /// </summary>
        public const float SeatLowest = 0.36f;

        public const float SeatHighest = 0.62f;

        /// <summary>The one-shot clips' lengths, s (measured, animation_check.txt), and how long the walker turns to a place first.</summary>
        public const float StandToSitSeconds = 1.3f;

        public const float SitToStandSeconds = 1.03f;

        public const float NodSeconds = 2.5f;

        public const float DrinkSeconds = 1.33f;

        public const float SettleSeconds = 0.5f;

        private static readonly List<TownsfolkWalker> Walkers = new List<TownsfolkWalker>();
        private static readonly int WalkState = Animator.StringToHash("Walk");
        private static readonly int IdleState = Animator.StringToHash("Idle");
        private static readonly int StandToSitState = Animator.StringToHash("StandToSit");
        private static readonly int SitState = Animator.StringToHash("Sit");
        private static readonly int SitTalkState = Animator.StringToHash("SitTalk");
        private static readonly int SitToStandState = Animator.StringToHash("SitToStand");
        private static readonly int TalkIdleState = Animator.StringToHash("TalkIdle");
        private static readonly int NodState = Animator.StringToHash("Nod");
        private static readonly int ShakeHeadState = Animator.StringToHash("ShakeHead");
        private static readonly int DrinkState = Animator.StringToHash("Drink");
        private static readonly int GatherState = Animator.StringToHash("Gather2");

        /// <summary>
        /// The gathering clip's length, s, with which a person sets a knocked chair back up: Gather2, its hands lowest,
        /// 0.73 m, at 0.53 s, when the chair is put back (measured, clip_survey.txt).
        /// </summary>
        public const float FetchSeconds = 1f;

        /// <summary>Authoring default, m: how far from a knocked chair a person stands to set it up.</summary>
        public const float FetchStandOff = 0.6f;

        /// <summary>Authoring default, s: how long a person with no route and no seat to sit on stands before it looks again.</summary>
        public const float SeatWait = 5f;

        /// <summary>A stop's idle, one of four by the turn's hash (AN6): three of Kevin Iglesias' idles and Quaternius' folded arms.</summary>
        public static readonly string[] StopIdles = { "Idle", "Idle2", "Idle3", "FoldArms" };
        private static readonly int[] StopStates = { IdleState, Animator.StringToHash("Idle2"), Animator.StringToHash("Idle3"), Animator.StringToHash("FoldArms") };

        /// <summary>A stall's bargaining and a bar's drinking, in turn: each step's state and seconds (TalkIdle and ShakeHead loop; authoring defaults).</summary>
        private static readonly (int state, float seconds)[] Bargaining = { (TalkIdleState, 3f), (NodState, NodSeconds), (TalkIdleState, 3f), (ShakeHeadState, NodSeconds) };
        private static readonly (int state, float seconds)[] Drinking = { (TalkIdleState, 4f), (DrinkState, DrinkSeconds), (TalkIdleState, 3f), (NodState, NodSeconds) };

        private enum Mode
        {
            Route,
            Stand,
            Greet,
            Going,
            Busy,
            Back,
        }

        private readonly List<(int turn, ActivitySpot spot, int place)> _visits = new List<(int, ActivitySpot, int)>();
        private int _next;
        private IWorldClock _clock;
        private Animator _animator;
        private float _owed;
        private float _standing;
        private uint _seed;
        private int _turns;
        private bool _started;
        private bool _replay;
        private int _state = WalkState; // the controller's default state, so a stroll paces its stride from the first step
        private float _k = 1f;

        // Seated, this body's own hips behind and beside its root, read once it first sits (LateUpdate); the measured man's,
        // scaled, until then.
        private float _seatedBack = float.NaN;
        private float _seatedSide;

        private Mode _mode;
        private ActivitySpot _spot;
        private int _place;
        private Vector3 _back;
        private int _step;
        private float _phase;
        private float _left;
        private bool _seated;
        private float _sinceGreet = GreetEvery;
        private TownsfolkWalker _partner;
        private LooseProp _heldChair;
        private LooseProp _fetch;
        private int _fetchStage;
        private readonly List<(Behaviour part, bool was)> _hiddenParts = new List<(Behaviour, bool)>();
        private readonly List<(GameObject part, bool was)> _hiddenChildren = new List<(GameObject, bool)>();
        private readonly List<(Collider part, bool was)> _hiddenColliders = new List<(Collider, bool)>();

        public Vector3[] Route => route;

        /// <summary>The walk clip's own pace, m/s, at which a walker in a hurry walks.</summary>
        public float Speed => speed;

        /// <summary>
        /// Authoring default, m/s: a person's unhurried walk through the city (owner, 2026-11-12: the people walked as if
        /// they ran). The walk clips' own strides measure 1.7–1.9 m/s and play slower to match it.
        /// </summary>
        public const float StrollSpeed = 1.3f;

        /// <summary>Whether the walker hurries, walking at the walk clip's own pace, as one sent for help does.</summary>
        public bool Hurry { get; set; }

        /// <summary>The pace the walker walks at now, m/s: a stroll, or <see cref="Speed"/> in a hurry.</summary>
        public float WalkingSpeed => Hurry ? speed : Mathf.Min(speed, StrollSpeed);

        /// <summary>Seconds of walking the walker still owes the ticks a paused world was moved by.</summary>
        public float Owed => _owed;

        /// <summary>Seconds the walker still stands at a turn of its route.</summary>
        public float Standing => _standing;

        /// <summary>How many times the walker has stopped at a turn.</summary>
        public int Stops { get; private set; }

        /// <summary>How many turns of its route the walker has reached.</summary>
        public int Turns => _turns;

        /// <summary>The place a person with no route keeps.</summary>
        public ActivitySpot Home => home;

        public int HomePlace => homePlace;

        /// <summary>Whether the walker is off its route: going to a place, passing its while there, or coming back.</summary>
        public bool Away => _mode == Mode.Going || _mode == Mode.Busy || _mode == Mode.Back;

        /// <summary>Whether it passes its while at <see cref="Spot"/> now, arrived.</summary>
        public bool Busy => _mode == Mode.Busy;

        /// <summary>Whether it greets another walker now.</summary>
        public bool Greeting => _mode == Mode.Greet;

        /// <summary>Whether it sits now, seated or sitting down or getting up.</summary>
        public bool Seated => _seated;

        /// <summary>Whether it is inside a door now, out of sight.</summary>
        public bool Inside { get; private set; }

        /// <summary>The spot it goes to, is at, or comes back from; null on its route.</summary>
        public ActivitySpot Spot => _spot;

        /// <summary>How many visits and greetings it has begun.</summary>
        public int VisitsBegun { get; private set; }

        public int Greetings { get; private set; }

        /// <summary>The body's size against the measured man's, by its hips: the seat's offsets scale by it.</summary>
        public float BodyScale => _k;

        /// <summary>The visits the editor found from its route's turns.</summary>
        public int VisitsKnown => _visits.Count;

        /// <summary>The seed of a route: an FNV-1a hash of its start, by which stops and visits are the same on every run.</summary>
        public static uint SeedOf(Vector3[] points) =>
            points == null || points.Length == 0 ? 0u : Fnv((uint)Mathf.RoundToInt(points[0].x * 10f), (uint)Mathf.RoundToInt(points[0].z * 10f));

        /// <summary>
        /// Whether the walker stops at its <paramref name="turn"/>th turn, and for how long: one turn in
        /// <see cref="StopOneIn"/>, for 4 to 12 s, by an FNV-1a hash of its route's start and the turn, so a walker
        /// stops at the same turns on every run.
        /// </summary>
        public static float StopAt(uint seed, int turn)
        {
            var h = Fnv(seed, (uint)turn);
            return h % StopOneIn != 0 ? 0f : StopShortest + ((StopLongest - StopShortest) * ((h / StopOneIn) % 1000u) / 999f);
        }

        /// <summary>Whether the walker goes on a visit at its <paramref name="turn"/>th turn when one is there: one turn in <see cref="VisitOneIn"/>.</summary>
        public static bool VisitsAt(uint seed, int turn) => Fnv(seed ^ 0x9E3779B9u, (uint)turn) % VisitOneIn == 0;

        /// <summary>Editor setup.</summary>
        public void Configure(Vector3[] points, float metresPerSecond, LayerMask groundMask)
        {
            route = points;
            speed = metresPerSecond;
            ground = groundMask;
        }

        /// <summary>Editor setup: a person with no route keeps <paramref name="place"/> of <paramref name="spot"/>.</summary>
        public void SetHome(ActivitySpot spot, int place)
        {
            home = spot;
            homePlace = place;
        }

        /// <summary>The route's length, m, round the whole loop.</summary>
        public float Length
        {
            get
            {
                var length = 0f;
                for (var i = 0; i < route.Length; i++)
                {
                    length += Flat(route[(i + 1) % route.Length] - route[i]).magnitude;
                }

                return length;
            }
        }

        /// <summary>
        /// Sends the walker to <paramref name="place"/> of <paramref name="spot"/> from where it is now, if the place is free;
        /// it comes back here after. The schedules of S5c will send people this way.
        /// </summary>
        public bool Send(ActivitySpot spot, int place)
        {
            if (!_started || spot == null || _mode != Mode.Route && _mode != Mode.Stand)
            {
                return false;
            }

            _standing = 0f;
            return Begin(spot, place, transform.position);
        }

        private void Start()
        {
            if (route.Length < 2 && home == null)
            {
                enabled = false;
                return;
            }

            _started = true;
            _animator = GetComponentInChildren<Animator>();
            // The seat scaled by the body's own hips, standing in the idle its prefab keeps, against the measured man's; by
            // its height where it has no hips to read.
            var capsule = GetComponent<CapsuleCollider>();
            var hips = _animator != null && _animator.isHuman ? _animator.GetBoneTransform(HumanBodyBones.Hips) : null;
            var up = hips != null ? transform.InverseTransformPoint(hips.position).y : 0f;
            _k = up > 0.5f ? up / StandingHipsUp : capsule != null ? capsule.height / MeasuredStature : 1f;
            if (route.Length < 2)
            {
                // A person with no route: seated or at the bar from the start; with its chair away, it looks for a seat.
                var stand = home.Now(homePlace).Stand;
                if (home.SeatReady(homePlace) && Begin(home, homePlace, stand))
                {
                    Arrive();
                    Skip();
                }
                else
                {
                    SitAgain(stand);
                }

                return;
            }

            _seed = SeedOf(route);
            foreach (var spot in ActivitySpot.All)
            {
                foreach (var v in spot.Visits)
                {
                    if (v.Walker == _seed && v.Turn >= 0 && v.Turn < route.Length && Flat(route[v.Turn] - v.At).sqrMagnitude < 0.01f)
                    {
                        _visits.Add((v.Turn, spot, v.Place));
                    }
                }
            }

            // Join the route at its nearest point and walk on from there.
            var best = float.MaxValue;
            for (var i = 0; i < route.Length; i++)
            {
                var d = Flat(route[i] - transform.position).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    _next = i;
                }
            }

            // Standing on a turning point already, as a placed walker does, it sets out for the next: no stop before its
            // first step.
            if (best < 0.01f)
            {
                _next = (_next + 1) % route.Length;
            }
        }

        private void OnEnable()
        {
            Walkers.Add(this);
            _replay = _started;
        }

        private void OnDisable()
        {
            Walkers.Remove(this);
            Watch(null);

            // Its component alone stopped, by an observer that noticed the character: it leaves what it was doing at once.
            // Its scene switched off behind an interior keeps it as it was.
            if (gameObject.activeInHierarchy)
            {
                Abandon();
            }
        }

        private void OnDestroy()
        {
            Watch(null);
            Release();
        }

        private void Update()
        {
            Watch(GameClockDriver.Running);
            if (_replay)
            {
                _replay = false;
                Play(_state);
            }

            var paused = _clock != null && _clock.IsPaused;
            float seconds;
            var pace = 1f;
            if (_owed > 0f)
            {
                seconds = Mathf.Min(_owed, Time.deltaTime * CatchUp);
                _owed -= seconds;
                pace = CatchUp;
            }
            else if (paused)
            {
                Pace(0f);
                return;
            }
            else
            {
                seconds = Time.deltaTime;
            }

            // The walk clip's stride played slower for a stroll, so the feet keep the ground.
            Pace(_state == WalkState ? pace * WalkingSpeed / Mathf.Max(0.01f, speed) : pace);
            Advance(seconds);
        }

        /// <summary>A whole tick moved while the world was paused: the walker owes its time.</summary>
        private void OnTick(long tick)
        {
            if (_clock != null && _clock.IsPaused)
            {
                _owed += GameClockDriver.RealSecondsPerTick;
            }
        }

        private void Watch(IWorldClock clock)
        {
            if (clock == _clock)
            {
                return;
            }

            if (_clock != null)
            {
                _clock.TickBoundary -= OnTick;
            }

            _clock = clock;
            if (_clock != null)
            {
                _clock.TickBoundary += OnTick;
            }
        }

        private void Pace(float pace)
        {
            if (_animator != null)
            {
                _animator.speed = pace;
            }
        }

        private void Advance(float seconds)
        {
            _sinceGreet += seconds;
            for (var guard = 0; seconds > 1e-5f && guard < 16; guard++)
            {
                switch (_mode)
                {
                    case Mode.Route:
                        seconds = Walk(seconds);
                        break;
                    case Mode.Stand:
                        seconds = Stand(seconds);
                        break;
                    case Mode.Greet:
                        seconds = Greet(seconds);
                        break;
                    case Mode.Going:
                        seconds = Going(seconds);
                        break;
                    case Mode.Busy:
                        seconds = Pass(seconds);
                        break;
                    case Mode.Back:
                        seconds = Back(seconds);
                        break;
                }
            }

            if (_mode == Mode.Route)
            {
                Meet();
            }
        }

        // ------------------------------------------------------------------------------------------- the route

        private float Walk(float seconds)
        {
            var position = transform.position;
            var step = WalkingSpeed * seconds;
            var target = route[_next];
            var to = Flat(target - position);
            for (var guard = 0; to.magnitude <= step && guard < route.Length; guard++)
            {
                var reached = _next;
                _next = (_next + 1) % route.Length;
                var turn = _turns++;
                if (Visit(reached, turn, target))
                {
                    return 0f;
                }

                var stop = StopAt(_seed, turn);
                if (stop > 0f)
                {
                    // The turn reached, the walker stands there a while before it walks on.
                    target.y = GroundAt(target, target.y);
                    transform.position = target;
                    _standing = stop;
                    _mode = Mode.Stand;
                    Stops++;
                    Play(StopStates[(Fnv(_seed, (uint)turn) >> 12) % (uint)StopStates.Length]);
                    return 0f;
                }

                target = route[_next];
                to = Flat(target - position);
            }

            if (to.sqrMagnitude < 1e-6f)
            {
                return 0f;
            }

            var direction = to.normalized;
            position += direction * step;
            position.y = GroundAt(position, Mathf.Lerp(position.y, target.y, 0.5f));
            transform.position = position;
            Face(direction, turnDegreesPerSecond * seconds);
            return 0f;
        }

        /// <summary>A stop at a turn takes its time from the walk, the time a paused world was moved by too.</summary>
        private float Stand(float seconds)
        {
            var stood = Mathf.Min(_standing, seconds);
            _standing -= stood;
            if (_standing > 0f)
            {
                return 0f;
            }

            // A person with no route who found no seat looks again.
            _mode = route.Length < 2 ? Mode.Back : Mode.Route;
            Play(WalkState);
            return seconds - stood;
        }

        /// <summary>
        /// A person with no route back at its spot: its own place if it can sit there, else another free one at the same
        /// spot, its chair set up first where it lies near; with none, it stands a while and looks again (owner,
        /// 2026-11-12: nobody sits where a chair was).
        /// </summary>
        private void SitAgain(Vector3 at)
        {
            var pick = home.Free(homePlace) && home.Usable(homePlace) ? homePlace : -1;
            for (var i = 0; pick < 0 && i < home.Count; i++)
            {
                pick = home.Free(i) && home.Usable(i) ? i : -1;
            }

            if (pick < 0 || !Begin(home, pick, at))
            {
                _mode = Mode.Stand;
                _standing = SeatWait;
                Play(IdleState);
                return;
            }

            if (pick == homePlace && _fetchStage == 0)
            {
                Arrive();
            }
        }

        /// <summary>At a turn with visits, one turn in two: a free one, chosen by the hash.</summary>
        private bool Visit(int reached, int turn, Vector3 at)
        {
            if (_visits.Count == 0 || !VisitsAt(_seed, turn))
            {
                return false;
            }

            var free = 0;
            foreach (var v in _visits)
            {
                free += v.turn == reached && v.spot != null && v.spot.isActiveAndEnabled && v.spot.Free(v.place) && v.spot.Usable(v.place) ? 1 : 0;
            }

            if (free == 0)
            {
                return false;
            }

            var pick = (int)(Fnv(_seed, (uint)turn ^ 0x5bd1e995u) % (uint)free);
            foreach (var v in _visits)
            {
                if (v.turn == reached && v.spot != null && v.spot.isActiveAndEnabled && v.spot.Free(v.place) && v.spot.Usable(v.place) && pick-- == 0)
                {
                    at.y = GroundAt(at, at.y);
                    transform.position = at;
                    return Begin(v.spot, v.place, at);
                }
            }

            return false;
        }

        // ------------------------------------------------------------------------------------------- greeting

        /// <summary>Two walkers on their routes, near and face to face, both free to greet: they stop and nod.</summary>
        private void Meet()
        {
            if (_sinceGreet < GreetEvery || Inside)
            {
                return;
            }

            var at = transform.position;
            foreach (var other in Walkers)
            {
                if (other == this || other._mode != Mode.Route || other._sinceGreet < GreetEvery || !other._started)
                {
                    continue;
                }

                var to = Flat(other.transform.position - at);
                if (to.magnitude > GreetReach || to.sqrMagnitude < 1e-4f)
                {
                    continue;
                }

                var dir = to.normalized;
                if (Vector3.Dot(Flat(transform.forward).normalized, dir) > 0.7f && Vector3.Dot(Flat(other.transform.forward).normalized, -dir) > 0.7f)
                {
                    StartGreeting(other);
                    other.StartGreeting(this);
                    return;
                }
            }
        }

        private void StartGreeting(TownsfolkWalker other)
        {
            _mode = Mode.Greet;
            _partner = other;
            _phase = NodSeconds;
            _sinceGreet = 0f;
            Greetings++;
            Play(NodState);
        }

        private float Greet(float seconds)
        {
            if (_partner != null)
            {
                Face(Flat(_partner.transform.position - transform.position), turnDegreesPerSecond * seconds);
            }

            var used = Mathf.Min(_phase, seconds);
            _phase -= used;
            if (_phase > 0f)
            {
                return 0f;
            }

            _partner = null;
            _mode = Mode.Route;
            Play(WalkState);
            return seconds - used;
        }

        // ------------------------------------------------------------------------------------------- visits

        /// <summary>
        /// Takes a place. A chair standing at home is held from now until the person leaves it, so nobody knocks it from
        /// under the sitter (owner, 2026-11-12); a chair knocked near its home is set back up first; a place whose chair is
        /// gone farther is never taken.
        /// </summary>
        private bool Begin(ActivitySpot spot, int place, Vector3 back)
        {
            if (spot == null || !spot.Usable(place) || !Reachable(spot, place) || !spot.Take(place, this))
            {
                return false;
            }

            _spot = spot;
            _place = place;
            _fetch = null;
            _fetchStage = 0;
            if (spot.Kind == ActivitySpot.Activity.Sit)
            {
                if (spot.SeatReady(place))
                {
                    HoldSeat();
                }
                else
                {
                    _fetch = spot.Chair(place);
                    _fetchStage = 1;
                }
            }

            _back = back;
            _mode = Mode.Going;
            VisitsBegun++;
            Play(WalkState);
            return true;
        }

        /// <summary>Authoring default, m: the height of the clear-way check to a pushed seat's place, about a person's knee.</summary>
        private const float KneeHeight = 0.5f;

        /// <summary>
        /// Whether a straight walk to a place reaches it: the editor found the walks to the places as they were built, and a
        /// seat pushed against a wall puts its new place behind it, where a person walking through would sit inside the wall.
        /// </summary>
        private bool Reachable(ActivitySpot spot, int place)
        {
            var chair = spot.Kind == ActivitySpot.Activity.Sit ? spot.Chair(place) : null;
            if (chair == null || !chair.Displaced)
            {
                return true;
            }

            var knee = Vector3.up * KneeHeight;
            return !Physics.Linecast(transform.position + knee, spot.Now(place).Stand + knee, LayerMask.GetMask(KitLayers.Structure), QueryTriggerInteraction.Ignore);
        }

        private float Going(float seconds)
        {
            if (_fetchStage > 0)
            {
                return Fetching(seconds);
            }

            if (MoveTo(StandPoint(), ref seconds))
            {
                Arrive();
            }

            return seconds;
        }

        /// <summary>
        /// A knocked chair set up before the person sits: it walks to the chair, stoops as at gathering, and halfway through
        /// the chair stands in its place again (owner, 2026-11-12); then it goes on to sit.
        /// </summary>
        private float Fetching(float seconds)
        {
            if (_fetch == null || (_fetchStage == 1 && !_fetch.Displaced))
            {
                _fetchStage = 0;
                _fetch = null;
                HoldSeat();
                return seconds;
            }

            if (_fetchStage == 1)
            {
                var chair = Flat(_fetch.transform.position);
                var from = Flat(transform.position) - chair;
                var stand = chair + ((from.sqrMagnitude > 1e-4f ? from.normalized : -Flat(transform.forward).normalized) * FetchStandOff);
                if (!MoveTo(new Vector3(stand.x, transform.position.y, stand.z), ref seconds))
                {
                    return seconds;
                }

                Face(chair - Flat(transform.position), 360f);
                _fetchStage = 2;
                _phase = FetchSeconds;
                Play(GatherState);
                return seconds;
            }

            var used = Mathf.Min(_phase, seconds);
            _phase -= used;
            if (_fetchStage == 2 && _phase <= FetchSeconds / 2f)
            {
                _fetch.PutBack();
                _fetchStage = 3;
            }

            if (_phase > 0f)
            {
                return 0f;
            }

            _fetchStage = 0;
            _fetch = null;
            HoldSeat();
            Play(WalkState);
            return seconds - used;
        }

        /// <summary>The place's chair held still and the person's body clear of the seat, for the whole visit.</summary>
        private void HoldSeat()
        {
            var chair = _spot != null ? _spot.Chair(_place) : null;
            if (chair != null && chair != _heldChair)
            {
                if (_heldChair != null)
                {
                    _heldChair.Hold(false);
                }

                chair.Hold(true);
                _heldChair = chair;
            }

            IgnoreSeat(true);
        }

        private void Arrive()
        {
            _mode = Mode.Busy;
            _step = 0;
            _phase = SettleSeconds;
            var hash = Fnv(_seed ^ (uint)VisitsBegun, (uint)_place);
            _left = home != null && _spot == home ? float.PositiveInfinity
                : Mathf.Lerp(_spot.Shortest, _spot.Longest, (hash % 1000u) / 999f);
            Play(IdleState);
        }

        /// <summary>A person with no route starts in its place: seated, or at the bar, without the walk or the sitting down.</summary>
        private void Skip()
        {
            var place = _spot.Now(_place);
            transform.rotation = Quaternion.Euler(0f, place.Yaw, 0f);
            if (_spot.Kind == ActivitySpot.Activity.Sit)
            {
                _seated = true;
                transform.position = SeatRoot();
                _step = 2;
                _phase = 4f;
                Play(SitState);
            }
            else if (_spot.Kind == ActivitySpot.Activity.Attend)
            {
                transform.position = place.Stand;
                _step = 1;
                _phase = _left;
                Play(IdleState);
            }
            else
            {
                transform.position = place.Stand;
                _step = 1;
                Cycle(_spot.Kind == ActivitySpot.Activity.Drink ? Drinking : Bargaining, 0);
            }
        }

        /// <summary>The while at a spot, step by step, each step's time taken from the walk's seconds.</summary>
        private float Pass(float seconds)
        {
            var place = _spot.Now(_place);
            if (_step == 0)
            {
                // Settling: it turns to the place's facing.
                Face(Quaternion.Euler(0f, place.Yaw, 0f) * Vector3.forward, 360f * seconds);
                var used = Mathf.Min(_phase, seconds);
                _phase -= used;
                if (_phase > 0f)
                {
                    return 0f;
                }

                transform.rotation = Quaternion.Euler(0f, place.Yaw, 0f);
                _step = 1;
                switch (_spot.Kind)
                {
                    case ActivitySpot.Activity.Sit:
                        _seated = true;
                        _phase = StandToSitSeconds;
                        Play(StandToSitState);
                        break;
                    case ActivitySpot.Activity.GoIn:
                        _phase = _left;
                        Hide();
                        break;
                    case ActivitySpot.Activity.Attend:
                        _phase = _left;
                        break;
                    default:
                        Cycle(_spot.Kind == ActivitySpot.Activity.Drink ? Drinking : Bargaining, 0);
                        break;
                }

                return seconds - used;
            }

            switch (_spot.Kind)
            {
                case ActivitySpot.Activity.Sit:
                    return Sitting(seconds, place);
                case ActivitySpot.Activity.GoIn:
                case ActivitySpot.Activity.Attend:
                {
                    var used = Mathf.Min(_phase, seconds);
                    _phase -= used;
                    if (_phase > 0f)
                    {
                        return 0f;
                    }

                    if (Inside)
                    {
                        Show();
                    }

                    Leave();
                    return seconds - used;
                }

                default:
                    return Talking(seconds, _spot.Kind == ActivitySpot.Activity.Drink ? Drinking : Bargaining);
            }
        }

        /// <summary>Sitting down from the stand point to the seat, sitting, talking while someone else sits there, and getting up.</summary>
        private float Sitting(float seconds, ActivitySpot.Place place)
        {
            var used = Mathf.Min(_phase, seconds);
            _phase -= used;
            var seat = SeatRoot(); // the feet stay where it stood: the clip lowers the hips onto the seat behind them
            switch (_step)
            {
                case 1:
                    if (_phase > 0f)
                    {
                        return 0f;
                    }

                    _step = 2;
                    _phase = 4f;
                    Play(SitState);
                    return seconds - used;
                case 2:
                    // Onto its own seat root once its hips are read, at 0.5 m/s.
                    transform.position = Vector3.MoveTowards(transform.position, seat, 0.5f * used);
                    _left -= used;
                    if (_left <= 0f)
                    {
                        _step = 3;
                        _phase = SitToStandSeconds;
                        Play(SitToStandState);
                        return seconds - used;
                    }

                    if (_phase <= 0f)
                    {
                        // A seated person only sits: the sitting talk's hands moved oddly (owner, 2026-11-12).
                        _phase = 4f;
                        Play(SitState);
                    }

                    return seconds - used;
                default:
                    if (_phase > 0f)
                    {
                        return 0f;
                    }

                    _seated = false;
                    Leave();
                    return seconds - used;
            }
        }

        /// <summary>Bargaining or drinking: its steps in turn until its while is over, the step under way finished first.</summary>
        private float Talking(float seconds, (int state, float seconds)[] steps)
        {
            var used = Mathf.Min(_phase, seconds);
            _phase -= used;
            _left -= used;
            if (_phase > 0f)
            {
                return 0f;
            }

            if (_left <= 0f)
            {
                Leave();
            }
            else
            {
                Cycle(steps, _step);
            }

            return seconds - used;
        }

        private void Cycle((int state, float seconds)[] steps, int step)
        {
            var s = steps[step % steps.Length];
            _step = (step % steps.Length) + 1;
            _phase = s.seconds;
            Play(s.state);
        }

        /// <summary>Its while over, it leaves the place and walks back to its route, or to its place again if it keeps one.</summary>
        private void Leave()
        {
            Release();
            _mode = Mode.Back;
            Play(WalkState);
        }

        private float Back(float seconds)
        {
            var target = home != null && route.Length < 2 ? home.Now(homePlace).Stand : _back;
            if (!MoveTo(target, ref seconds))
            {
                return seconds;
            }

            if (home != null && route.Length < 2)
            {
                SitAgain(target);
                return seconds;
            }

            _mode = Mode.Route;
            Play(WalkState);
            return seconds;
        }

        /// <summary>
        /// Whatever it was doing left at once, as an observer that noticed the character stops it: up from a seat, out of a
        /// door, its place freed; it walks back once it walks again.
        /// </summary>
        private void Abandon()
        {
            if (!_started)
            {
                return;
            }

            if (_mode == Mode.Greet)
            {
                _partner = null;
                _mode = Mode.Route;
                _state = WalkState;
                return;
            }

            if (_mode != Mode.Going && _mode != Mode.Busy)
            {
                return;
            }

            if (Inside)
            {
                Show();
            }

            if (_seated)
            {
                _seated = false;
                transform.position = StandPoint();
            }

            Release();
            _mode = Mode.Back;
            _state = WalkState;
        }

        private void Release()
        {
            if (_spot != null)
            {
                _spot.Leave(_place, this);
                IgnoreSeat(false);
                _spot = null;
            }

            // A chair unloaded with its scene before the person is gone already: Unity's null, not the language's.
            if (_heldChair != null)
            {
                _heldChair.Hold(false);
            }

            _heldChair = null;
            _fetch = null;
            _fetchStage = 0;
        }

        private readonly List<Collider> _seatParts = new List<Collider>();
        private readonly List<LooseProp> _heldUnder = new List<LooseProp>();

        /// <summary>
        /// A seated walker never shoves its seat: its capsule, round its root, reaches about 0.2 m into the seat, and a
        /// loose chair or armchair it sat on was pushed aside or over (measured, ActivityPlayTests.RenderTheKitSeats,
        /// 2026-11-12). From the visit's start to its end its colliders ignore whatever is within 0.3 m of its place's seat.
        /// </summary>
        private void IgnoreSeat(bool ignore)
        {
            var own = GetComponents<Collider>().Where(c => c.enabled).ToArray();
            if (ignore)
            {
                _seatParts.Clear();
                _seatParts.AddRange(Physics.OverlapSphere(_spot.Now(_place).Seat, 0.3f, ~0, QueryTriggerInteraction.Ignore)
                    .Where(c => !c.transform.IsChildOf(transform)));

                // Whatever loose piece stands under the seat is held too, the place's own chair or not: the player still
                // knocked a chair from under a sitter (owner's play test, 2026-11-12), and a chair found by its home alone
                // misses one whose home lies off the seat.
                foreach (var prop in _seatParts.Select(c => c.attachedRigidbody != null ? c.attachedRigidbody.GetComponent<LooseProp>() : c.GetComponentInParent<LooseProp>())
                             .Where(p => p != null && !p.IsThing && p != _heldChair && !_heldUnder.Contains(p)).ToList())
                {
                    prop.Hold(true);
                    _heldUnder.Add(prop);
                }
            }

            foreach (var part in _seatParts.Where(c => c != null && c.enabled && c.gameObject.activeInHierarchy))
            {
                foreach (var mine in own)
                {
                    Physics.IgnoreCollision(mine, part, ignore);
                }
            }

            if (!ignore)
            {
                _seatParts.Clear();
                foreach (var prop in _heldUnder.Where(p => p != null))
                {
                    prop.Hold(false);
                }

                _heldUnder.Clear();
            }
        }

        /// <summary>Where it stands before it sits or at the place, on the ground; for a seat, in front of it by the body's own size.</summary>
        private Vector3 StandPoint()
        {
            var place = _spot.Now(_place);
            if (_spot.Kind != ActivitySpot.Activity.Sit)
            {
                return place.Stand;
            }

            var seat = SeatRoot();
            return new Vector3(seat.x, place.Stand.y, seat.z);
        }

        /// <summary>
        /// The root seated: its feet on the floor before the seat, the hips over the place's seat point (which the seat's
        /// reader sets <see cref="HipsBehindFront"/> behind its front edge), by the body's own seated hips.
        /// </summary>
        private Vector3 SeatRoot()
        {
            var place = _spot.Now(_place);
            var turn = Quaternion.Euler(0f, place.Yaw, 0f);
            var back = float.IsNaN(_seatedBack) ? SeatedHipsBack * _k : _seatedBack;
            var root = place.Seat + (turn * new Vector3(-_seatedSide, 0f, back));
            return new Vector3(root.x, place.Stand.y, root.z);
        }

        /// <summary>
        /// Seated for the first time, the body reads its own hips: its proportions move them off the measured man's scaled
        /// by its hips, by 0.15 m on the working woman (measured, <c>ActivityPlayTests</c>). A pose out of reason, as
        /// a culled animator's last one, is never read.
        /// </summary>
        private void LateUpdate()
        {
            if (!_seated || _step != 2 || !float.IsNaN(_seatedBack) || _animator == null || !_animator.isHuman || _animator.IsInTransition(0))
            {
                return;
            }

            var info = _animator.GetCurrentAnimatorStateInfo(0);
            if (info.shortNameHash != SitState && info.shortNameHash != SitTalkState)
            {
                return;
            }

            var hips = transform.InverseTransformPoint(_animator.GetBoneTransform(HumanBodyBones.Hips).position);
            if (hips.y > 0.4f * _k && hips.y < 0.8f * _k && -hips.z > 0.05f * _k && -hips.z < 0.6f * _k && Mathf.Abs(hips.x) < 0.3f * _k)
            {
                _seatedBack = -hips.z;
                _seatedSide = hips.x;
            }
        }

        /// <summary>Out of sight inside a door: its body, its collider, and what else it carries (its eyes, its purse) put away.</summary>
        private void Hide()
        {
            Inside = true;
            _hiddenParts.Clear();
            _hiddenChildren.Clear();
            _hiddenColliders.Clear();
            foreach (Transform child in transform)
            {
                _hiddenChildren.Add((child.gameObject, child.gameObject.activeSelf));
                child.gameObject.SetActive(false);
            }

            foreach (var part in GetComponents<Behaviour>())
            {
                if (part != this)
                {
                    _hiddenParts.Add((part, part.enabled));
                    part.enabled = false;
                }
            }

            foreach (var part in GetComponents<Collider>())
            {
                _hiddenColliders.Add((part, part.enabled));
                part.enabled = false;
            }
        }

        private void Show()
        {
            Inside = false;
            foreach (var (part, was) in _hiddenChildren)
            {
                if (part != null)
                {
                    part.SetActive(was);
                }
            }

            foreach (var (part, was) in _hiddenParts)
            {
                if (part != null)
                {
                    part.enabled = was;
                }
            }

            foreach (var (part, was) in _hiddenColliders)
            {
                if (part != null)
                {
                    part.enabled = was;
                }
            }

            _hiddenParts.Clear();
            _hiddenChildren.Clear();
            _hiddenColliders.Clear();
            _animator = GetComponentInChildren<Animator>();
            Play(IdleState);
        }

        // ------------------------------------------------------------------------------------------- moving

        /// <summary>A straight walk toward <paramref name="target"/> on the ground; true once there, the seconds left over kept.</summary>
        private bool MoveTo(Vector3 target, ref float seconds)
        {
            var position = transform.position;
            var to = Flat(target - position);
            var step = WalkingSpeed * seconds;
            if (to.magnitude <= step)
            {
                seconds -= to.magnitude / Mathf.Max(0.01f, WalkingSpeed);
                transform.position = new Vector3(target.x, GroundAt(target, target.y), target.z);
                return true;
            }

            var direction = to.normalized;
            position += direction * step;
            position.y = GroundAt(position, Mathf.Lerp(position.y, target.y, 0.5f));
            transform.position = position;
            Face(direction, turnDegreesPerSecond * seconds);
            seconds = 0f;
            return false;
        }

        private void Face(Vector3 direction, float degrees)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 1e-6f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction, Vector3.up), degrees);
            }
        }

        private void Play(int state)
        {
            _state = state;
            if (_animator != null && _animator.isActiveAndEnabled && _animator.HasState(0, state))
            {
                _animator.CrossFadeInFixedTime(state, 0.3f);
            }
        }

        private static uint Fnv(uint a, uint b)
        {
            var h = 2166136261u;
            foreach (var v in new[] { a, b })
            {
                for (var i = 0; i < 4; i++)
                {
                    h = (h ^ ((v >> (8 * i)) & 0xFFu)) * 16777619u;
                }
            }

            return h;
        }

        private float GroundAt(Vector3 at, float fallback)
        {
            return Physics.Raycast(at + (Vector3.up * 1.5f), Vector3.down, out var hit, 4f, ground, QueryTriggerInteraction.Ignore)
                ? hit.point.y
                : fallback;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f);
            for (var i = 0; i < route.Length; i++)
            {
                Gizmos.DrawLine(route[i] + (Vector3.up * 0.1f), route[(i + 1) % route.Length] + (Vector3.up * 0.1f));
            }
        }
    }
}
