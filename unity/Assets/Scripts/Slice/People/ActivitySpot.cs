using System;
using System.Collections.Generic;
using GameDev.Slice.Kit;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameDev.Slice.People
{
    /// <summary>
    /// A place where townsfolk pass a while (AN6, the owner's direction of 2026-11-11 that the townsfolk sit on benches,
    /// bargain at stalls, go to the tavern and the chapel, and greet one another): a bench, a stall, a door, or a bar, each
    /// with its places, one person to a place. <see cref="TownsfolkWalker"/> leaves its route for one of the visits the
    /// editor found it (<c>SliceActivities</c>), a straight clear walk from a turn of its route, and comes back to that
    /// turn; a person who walks no route keeps one place, as the drinkers of the Soot &amp; Bell and the people in the
    /// chapel's pews.
    /// </summary>
    public sealed class ActivitySpot : MonoBehaviour
    {
        public enum Activity
        {
            /// <summary>Sits on a bench, a chair or a pew, and talks while someone else sits at the same spot.</summary>
            Sit,

            /// <summary>Stands at a stall and bargains: talks, nods, shakes its head.</summary>
            Bargain,

            /// <summary>Goes in at a door and stays inside a while, out of sight.</summary>
            GoIn,

            /// <summary>Stands at a bar, drinks and talks.</summary>
            Drink,

            /// <summary>Stands quietly, facing the place's way, as the chapel's people stand in their pews.</summary>
            Attend,
        }

        /// <summary>One person's place: where it stands, facing <see cref="Yaw"/>; for a seat, the seat's top under its hips.</summary>
        [Serializable]
        public struct Place
        {
            public Vector3 Stand;
            public Vector3 Seat;
            public float Yaw;
        }

        /// <summary>A walk the editor found clear: the walker of route seed <see cref="Walker"/>, from its turn <see cref="Turn"/> at <see cref="At"/>, to place <see cref="Place"/>.</summary>
        [Serializable]
        public struct Visit
        {
            public uint Walker;
            public int Turn;
            public Vector3 At;
            public int Place;
        }

        private static readonly List<ActivitySpot> Registered = new List<ActivitySpot>();

        [SerializeField] private Activity activity;

        [Tooltip("The bench, stall, door or furniture it belongs to: a walk to it may pass beside it.")]
        [SerializeField] private Transform piece;

        [Tooltip("Nobody talks here, as in the chapel's pews.")]
        [SerializeField] private bool quiet;

        [Tooltip("The shortest and the longest a visit lasts, s of the world's time (authoring defaults).")]
        [SerializeField] private float shortest = 20f;

        [SerializeField] private float longest = 60f;

        [SerializeField] private Place[] places = new Place[0];

        [SerializeField] private Visit[] visits = new Visit[0];

        private Object[] _taken = new Object[0];

        public static IReadOnlyList<ActivitySpot> All => Registered;

        public Activity Kind => activity;

        public Transform Piece => piece;

        public bool Quiet => quiet;

        public float Shortest => shortest;

        public float Longest => longest;

        public int Count => places.Length;

        public IReadOnlyList<Visit> Visits => visits;

        public Place PlaceAt(int place) => places[place];

        /// <summary>Editor setup.</summary>
        public void Configure(Activity kind, bool isQuiet, float shortestSeconds, float longestSeconds, Place[] placeList, Transform of = null)
        {
            activity = kind;
            piece = of;
            quiet = isQuiet;
            shortest = shortestSeconds;
            longest = longestSeconds;
            places = placeList;
            visits = new Visit[0];
            _taken = new Object[places.Length];
        }

        /// <summary>Editor setup: the walks found clear to this spot.</summary>
        public void SetVisits(Visit[] list) => visits = list;

        public bool Free(int place)
        {
            Fit();
            return place >= 0 && place < places.Length && _taken[place] == null;
        }

        /// <summary>The place taken by <paramref name="who"/>, if it was free.</summary>
        public bool Take(int place, Object who)
        {
            if (!Free(place) && (place < 0 || place >= places.Length || _taken[place] != who))
            {
                return false;
            }

            _taken[place] = who;
            return true;
        }

        public void Leave(int place, Object who)
        {
            Fit();
            if (place >= 0 && place < places.Length && _taken[place] == who)
            {
                _taken[place] = null;
            }
        }

        /// <summary>Authoring default, m: a loose seat whose home lies this near a place's seat point over the ground is that place's chair.</summary>
        public const float ChairReach = 0.5f;

        /// <summary>
        /// Authoring default, m: a chair knocked no farther than this from its home is set back up by whoever comes to sit
        /// there (owner, 2026-11-12); one farther off leaves its place empty until the hour brings it back.
        /// </summary>
        public const float FetchReach = 2.5f;

        private LooseProp[] _chairs;

        /// <summary>
        /// The loose seat of a place: the spot's own piece where it is loose (a street bench), or else the loose chair whose
        /// home lies under the place's seat (the taproom's chairs); null for a fixed bench or pew, and for a spot that is no
        /// seat. A bench's home lies far from its end places, so the piece is asked first: found by its home alone, a moved
        /// bench was read as fixed, and the townsfolk sat on the air where it had stood (owner's play test, 2026-11-12).
        /// </summary>
        public LooseProp Chair(int place)
        {
            if (_chairs == null || _chairs.Length != places.Length)
            {
                _chairs = new LooseProp[places.Length];
                var own = activity == Activity.Sit && piece != null ? piece.GetComponentInParent<LooseProp>() : null;
                for (var i = 0; activity == Activity.Sit && i < places.Length; i++)
                {
                    _chairs[i] = own != null ? own : LooseProp.NearestHome(places[i].Seat, ChairReach);
                }
            }

            return place >= 0 && place < _chairs.Length ? _chairs[place] : null;
        }

        /// <summary>Authoring default, degrees: a seat tilted from its home's upright no more than this still stands to be sat on where it is.</summary>
        public const float UprightAngle = 10f;

        /// <summary>Whether a loose seat stands upright, however far it was pushed or turned over the ground.</summary>
        public static bool Upright(LooseProp chair) =>
            chair != null && Vector3.Angle(chair.transform.rotation * Quaternion.Inverse(chair.HomeRotation) * Vector3.up, Vector3.up) <= UprightAngle;

        /// <summary>
        /// A place as it stands now: on a loose seat pushed upright to a new spot, its stand point, seat and facing go with
        /// the seat, as a person on a moving platform goes with it (owner, 2026-11-12: the townsfolk sit on the very bench,
        /// wherever it was pushed); otherwise the place as authored.
        /// </summary>
        public Place Now(int place)
        {
            var p = places[place];
            var chair = Chair(place);
            if (chair == null || !chair.Displaced || !Upright(chair))
            {
                return p;
            }

            var home = chair.HomePosition;
            var at = chair.transform.position;
            var yaw = Mathf.DeltaAngle(chair.HomeRotation.eulerAngles.y, chair.transform.rotation.eulerAngles.y);
            var turn = Quaternion.Euler(0f, yaw, 0f);
            return new Place { Stand = at + (turn * (p.Stand - home)), Seat = at + (turn * (p.Seat - home)), Yaw = p.Yaw + yaw };
        }

        /// <summary>Whether a place can be sat on now: a fixed seat, or its loose seat upright, at home or pushed.</summary>
        public bool SeatReady(int place)
        {
            var chair = Chair(place);
            return chair == null || !chair.Displaced || Upright(chair);
        }

        /// <summary>Whether the place's seat lies knocked over near its home, for whoever comes to sit to set it up first.</summary>
        public bool ChairNear(int place)
        {
            var chair = Chair(place);
            if (chair == null || !chair.Displaced || Upright(chair) || chair.Held)
            {
                return false;
            }

            var off = chair.transform.position - chair.HomePosition;
            return new Vector2(off.x, off.z).magnitude <= FetchReach;
        }

        /// <summary>Whether a person could sit at a place now: its seat ready, or its chair near enough to set up first.</summary>
        public bool Usable(int place) => activity != Activity.Sit || SeatReady(place) || ChairNear(place);

        /// <summary>Whether someone other than the one at <paramref name="place"/> is at this spot now.</summary>
        public bool Company(int place)
        {
            Fit();
            for (var i = 0; i < _taken.Length; i++)
            {
                if (i != place && _taken[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void Fit()
        {
            if (_taken.Length != places.Length)
            {
                Array.Resize(ref _taken, places.Length);
            }
        }

        private void OnEnable() => Registered.Add(this);

        private void OnDisable() => Registered.Remove(this);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.9f, 1f);
            foreach (var p in places)
            {
                Gizmos.DrawWireSphere(p.Stand + (Vector3.up * 0.1f), 0.25f);
                Gizmos.DrawLine(p.Stand + (Vector3.up * 0.1f), p.Stand + (Vector3.up * 0.1f) + (Quaternion.Euler(0f, p.Yaw, 0f) * Vector3.forward * 0.6f));
            }
        }
    }
}
