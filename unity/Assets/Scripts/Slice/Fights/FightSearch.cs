using GameDev.Confront.Rules;
using GameDev.Confront.Stealth;
using GameDev.Slice.People;
using GameDev.Slice.Stealth;
using GameDev.World.Timekeeping;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// 10.2 §9, the search (owner, 2026-11-12; PB5b): an enemy the character broke away from runs to the place where it
    /// last saw the character, looks round there for <see cref="LookTicks"/> world ticks, and walks back to where it
    /// stood when the fight began. All the while it is Alert and its Vision Cone shows, and its host asks each frame
    /// whether it sees the character: out of Stealth Mode a cone in sight is enough, in Stealth Mode its Notice Meter
    /// must fill. It runs and walks straight, its feet on the ground under it, and waits while the world is paused. A
    /// wall in the way stops it where it stands, and it looks round from there (10.3, as <see cref="StealthObserver"/>).
    /// </summary>
    public sealed class FightSearch : MonoBehaviour
    {
        public enum Phase
        {
            Running,
            Looking,
            Returning,
            Done,
        }

        /// <summary>Authoring defaults: world ticks of looking round, about 30 s of the city's motion (10.2 §9).</summary>
        public const int LookTicks = 3;

        /// <summary>The run, m/s: the player's and the Wardens' run (4.5 m/s); the walk back, the Wardens' errand walk.</summary>
        public const float RunSpeed = SliceConfront.WardenRunSpeed;

        public const float WalkSpeed = WardenErrand.WalkSpeed;

        private const float Arrive = 0.8f;      // m from the place, where it stops to look
        private const float TurnRate = 360f;    // degrees a second toward where it goes
        private const float LookRate = 60f;     // degrees a second looking round
        private const float KneeHeight = 0.5f;  // m, the height of the way checked for a wall
        private const float ArriveSlack = 0.01f; // m, so a step that ends at the reach counts as there

        private static readonly int RunState = Animator.StringToHash("Run");
        private static readonly int WalkState = Animator.StringToHash("Walk");

        /// <summary>The Walk state's hash, played where a controller has no Run.</summary>
        public static int WalkStateHash => WalkState;
        private static readonly int IdleState = Animator.StringToHash("Idle");

        private Animator _animator;
        private TownsfolkWalker _walker;
        private StealthObserver _eyes;
        private bool _eyesManaged;
        private bool _eyesShown;
        private IWorldClock _clock;
        private int _looked;
        private int _playing;
        private bool _ended;

        public Phase Now { get; private set; }

        /// <summary>The world ticks it has looked round so far.</summary>
        public int Looked => _looked;

        /// <summary>Where it last saw the character, the place it runs to.</summary>
        public Vector3 Place { get; private set; }

        /// <summary>Where it stood and how it faced when the fight began, where it goes back to.</summary>
        public Vector3 Post { get; private set; }

        public Quaternion PostFacing { get; private set; }

        /// <summary>The fighter it was, its HP kept for a fight that starts again.</summary>
        public Fighter Fighter { get; private set; }

        /// <summary>Its group, by <see cref="SliceConfront"/>'s names.</summary>
        public string Group { get; private set; }

        /// <summary>The string table key of its name in a fight.</summary>
        public string NameKey { get; private set; }

        /// <summary>A development opponent, gone when its search is over.</summary>
        public bool Development { get; private set; }

        /// <summary>Its eyes, the cone its host reads.</summary>
        public StealthObserver Eyes => _eyes;

        public static FightSearch Begin(Transform body, Vector3 place, Vector3 post, Quaternion postFacing, Fighter fighter, string group, string nameKey,
            bool development, StealthRules rules, IWorldClock clock)
        {
            var search = body.gameObject.AddComponent<FightSearch>();
            search.Place = place;
            search.Post = post;
            search.PostFacing = postFacing;
            search.Fighter = fighter;
            search.Group = group;
            search.NameKey = nameKey;
            search.Development = development;
            search._clock = clock;
            search._animator = body.GetComponentInChildren<Animator>();
            search._walker = body.GetComponent<TownsfolkWalker>();
            search._eyes = body.GetComponent<StealthObserver>();
            if (search._eyes == null)
            {
                search._eyes = body.gameObject.AddComponent<StealthObserver>();
                search._eyes.Perception = fighter.Get(GameDev.Player.Progression.CharacterAttribute.Perception);
            }

            search._eyes.Bind(rules);
            search._eyesManaged = search._eyes.Managed;
            search._eyesShown = search._eyes.ConeAlwaysShown;
            search._eyes.Managed = true;          // its meter is the search's, not the city's
            search._eyes.ConeAlwaysShown = true;  // its cone shows while it searches (10.2 §9)
            search._eyes.Meter?.Reset();
            if (clock != null)
            {
                clock.TickBoundary += search.OnTick;
            }

            return search;
        }

        /// <summary>Its search is over or a fight takes it: its eyes go back to the city's, its walk too unless it fights.</summary>
        public void End(bool walkAgain)
        {
            if (_ended)
            {
                return;
            }

            _ended = true;
            Now = Phase.Done;
            if (_clock != null)
            {
                _clock.TickBoundary -= OnTick;
            }

            if (_eyes != null)
            {
                _eyes.Managed = _eyesManaged;
                _eyes.ConeAlwaysShown = _eyesShown;
                _eyes.Meter?.Reset();
            }

            if (walkAgain && _walker != null && !_walker.enabled && gameObject.activeInHierarchy)
            {
                _walker.enabled = true;
            }

            Destroy(this);
        }

        private void Update()
        {
            if (_ended || Now == Phase.Done || (_clock != null && _clock.IsPaused))
            {
                return;
            }

            if (_walker != null && _walker.enabled)
            {
                _walker.enabled = false; // the search holds its walk until it is over
            }

            if (_eyes != null)
            {
                _eyes.Meter?.Alarm(); // Alert while it searches (10.3)
            }
            switch (Now)
            {
                case Phase.Running:
                    if (Go(Place, RunSpeed, Arrive, RunState))
                    {
                        Now = Phase.Looking;
                        _looked = 0;
                    }

                    break;
                case Phase.Looking:
                    Play(IdleState);
                    transform.Rotate(0f, LookRate * Time.deltaTime, 0f);
                    break;
                case Phase.Returning:
                    if (Go(Post, WalkSpeed, 0.05f, WalkState))
                    {
                        transform.SetPositionAndRotation(Post, PostFacing);
                        Play(IdleState);
                        Now = Phase.Done; // back: the host ends it in its next look
                    }


                    break;
            }
        }

        /// <summary>A step toward <paramref name="target"/>; true once there, or once a wall stands in the way of a run.</summary>
        private bool Go(Vector3 target, float speed, float reach, int state)
        {
            if (Step(transform, target, speed, reach, state == RunState))
            {
                return true;
            }

            Play(_animator != null && _animator.HasState(0, state) ? state : WalkState);
            return false;
        }

        /// <summary>
        /// A body's step toward a place, straight, its feet on the ground under it; true once there, or, with
        /// <paramref name="stopAtWall"/>, once a wall at knee height stands in the way.
        /// </summary>
        public static bool Step(Transform body, Vector3 target, float speed, float reach, bool stopAtWall)
        {
            var at = body.position;
            var way = Vector3.ProjectOnPlane(target - at, Vector3.up);
            if (way.magnitude <= reach + ArriveSlack)
            {
                return true;
            }

            var knee = Vector3.up * KneeHeight;
            if (stopAtWall && Physics.Linecast(at + knee, target + knee, out var hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && !PeopleAround.IsPerson(hit.collider))
            {
                return true; // it stops where it stands
            }

            var step = Mathf.Min(way.magnitude - reach, speed * Time.deltaTime);
            var next = at + (way.normalized * step);
            next = PeopleAround.Ground(new Vector3(next.x, at.y, next.z));
            var turn = Quaternion.RotateTowards(body.rotation, Quaternion.LookRotation(way.normalized, Vector3.up), TurnRate * Time.deltaTime);
            body.SetPositionAndRotation(next, turn);
            return false;
        }

        private void OnTick(long tick)
        {
            if (Now == Phase.Looking && ++_looked >= LookTicks)
            {
                Now = Phase.Returning;
            }
        }

        private void Play(int state)
        {
            if (_playing == state || _animator == null || !_animator.isActiveAndEnabled || !_animator.HasState(0, state))
            {
                return;
            }

            _playing = state;
            _animator.CrossFadeInFixedTime(state, 0.2f, 0);
        }

        private void OnDestroy()
        {
            if (_clock != null)
            {
                _clock.TickBoundary -= OnTick;
            }
        }
    }
}
