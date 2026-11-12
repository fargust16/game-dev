using System.Collections.Generic;
using GameDev.World.Timekeeping;
using UnityEngine;

namespace GameDev.Slice.Kit
{
    /// <summary>
    /// A prop with mass and gravity (kit standard §4, loose props; owner, 2026-11-06). It sleeps from the scene's start,
    /// so it lies still until something touches it. A set's thing leaves its set on its first Start, since a body must
    /// not ride under another moving body: not on Awake, which runs as soon as a builder instantiates the set and before
    /// it puts the set in place, and left the things hanging where the set was made (owner's fourth play test,
    /// 2026-11-06: things hung in the air in a dwelling).
    /// <para>
    /// It remembers where it stood at the start, its home, and goes back there by itself once it has lain moved for
    /// <see cref="RestoreTicks"/> of the world's time (owner, 2026-11-12: furniture wrecked in an interior is all back in
    /// its place an hour later). A person sitting on it holds it (<see cref="Hold"/>): held, it is kinematic, so no push
    /// moves it from under the sitter.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class LooseProp : MonoBehaviour
    {
        /// <summary>The owner's (2026-11-12): one game hour, 60 ticks, from the prop's last move to its return.</summary>
        public const int RestoreTicks = 60;

        /// <summary>Authoring defaults: how far, m, and how much turned, degrees, a prop lies from its home to count as moved.</summary>
        public const float MovedDistance = 0.1f;

        public const float MovedAngle = 10f;

        private static readonly List<LooseProp> Registered = new List<LooseProp>();
        private static IWorldClock _watched;

        [SerializeField] private bool thing;

        private Rigidbody _body;
        private bool _homeSet;
        private Vector3 _homePosition;
        private Quaternion _homeRotation;
        private long _movedSince = -1;
        private int _holds;
        private bool _wasKinematic;

        /// <summary>A set's thing (a bottle, a book), as against the block it stood on.</summary>
        public bool IsThing => thing;

        /// <summary>Every loose prop in the loaded scenes.</summary>
        public static IReadOnlyList<LooseProp> All => Registered;

        public Vector3 HomePosition
        {
            get
            {
                SetHome();
                return _homePosition;
            }
        }

        public Quaternion HomeRotation
        {
            get
            {
                SetHome();
                return _homeRotation;
            }
        }

        /// <summary>Whether it lies away from its home or turned from it.</summary>
        public bool Displaced =>
            (transform.position - HomePosition).sqrMagnitude > MovedDistance * MovedDistance || Quaternion.Angle(transform.rotation, HomeRotation) > MovedAngle;

        /// <summary>Whether someone holds it still now.</summary>
        public bool Held => _holds > 0;

        public void Configure(bool isThing) => thing = isThing;

        /// <summary>
        /// Held, as a chair under a sitter: kinematic, so a push or a blow cannot move it. Each hold is let go once; the
        /// last lets it fall and slide again.
        /// </summary>
        public void Hold(bool hold)
        {
            var body = Body();
            if (hold)
            {
                if (_holds++ == 0)
                {
                    _wasKinematic = body.isKinematic;
                    if (!_wasKinematic)
                    {
                        body.linearVelocity = Vector3.zero;
                        body.angularVelocity = Vector3.zero;
                    }

                    body.isKinematic = true;
                }

                return;
            }

            if (_holds > 0 && --_holds == 0)
            {
                body.isKinematic = _wasKinematic;
                if (!_wasKinematic)
                {
                    body.Sleep();
                }
            }
        }

        /// <summary>Back to its home at once, still, as a person sets a chair up or as the hour has passed.</summary>
        public void PutBack()
        {
            var body = Body();
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            transform.SetPositionAndRotation(HomePosition, HomeRotation);
            body.position = HomePosition;
            body.rotation = HomeRotation;
            _movedSince = -1;
            if (!body.isKinematic)
            {
                body.Sleep();
            }
        }

        /// <summary>The loose prop whose home lies nearest <paramref name="at"/> over the ground, within <paramref name="reach"/> m, but a set's things; or null.</summary>
        public static LooseProp NearestHome(Vector3 at, float reach)
        {
            LooseProp best = null;
            var nearest = reach * reach;
            foreach (var prop in Registered)
            {
                if (prop == null || prop.thing)
                {
                    continue;
                }

                var home = prop.HomePosition;
                var d = new Vector2(home.x - at.x, home.z - at.z).sqrMagnitude;
                if (d <= nearest && Mathf.Abs(home.y - at.y) < 1f)
                {
                    nearest = d;
                    best = prop;
                }
            }

            return best;
        }

        private Rigidbody Body() => _body != null ? _body : _body = GetComponent<Rigidbody>();

        private void SetHome()
        {
            if (_homeSet)
            {
                return;
            }

            _homeSet = true;
            _homePosition = transform.position;
            _homeRotation = transform.rotation;
        }

        private void OnEnable()
        {
            Registered.Add(this);
            Watch(GameClockDriver.Running);
        }

        private void OnDisable() => Registered.Remove(this);

        private void Start()
        {
            if (thing)
            {
                var block = GetComponentInParent<KitBlock>();
                if (block != null)
                {
                    transform.SetParent(block.transform.parent, true);
                }
            }

            SetHome();
            Body().Sleep();
            Watch(GameClockDriver.Running);
        }

        /// <summary>One handler on the running clock for every loose prop, moved to a new clock when a scene brings one.</summary>
        private static void Watch(IWorldClock clock)
        {
            if (clock == null || clock == _watched)
            {
                return;
            }

            if (_watched != null)
            {
                _watched.TickBoundary -= OnTick;
            }

            _watched = clock;
            _watched.TickBoundary += OnTick;
        }

        /// <summary>
        /// Each tick, each prop lying moved counts the world's time from its last move, while it slides or is held the
        /// count starts again, and an hour after it went still it is back home.
        /// </summary>
        private static void OnTick(long tick)
        {
            for (var i = Registered.Count - 1; i >= 0; i--)
            {
                var prop = Registered[i];
                if (prop == null || !prop.isActiveAndEnabled)
                {
                    continue;
                }

                if (!prop.Displaced)
                {
                    prop._movedSince = -1;
                    continue;
                }

                var body = prop.Body();
                if (prop._movedSince < 0 || prop.Held || (!body.isKinematic && !body.IsSleeping()))
                {
                    prop._movedSince = tick;
                    continue;
                }

                if (tick - prop._movedSince >= RestoreTicks)
                {
                    prop.PutBack();
                }
            }
        }
    }
}
