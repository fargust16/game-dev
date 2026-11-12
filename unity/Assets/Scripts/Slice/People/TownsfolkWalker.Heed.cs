using System.Collections.Generic;
using GameDev.Slice.Kit;
using GameDev.World.Timekeeping;
using UnityEngine;

namespace GameDev.Slice.People
{
    /// <summary>
    /// 10.2 §5e, the townsfolk's part of the Alarm Check (owner, 2026-11-12; PB8c). A walker who heard an Alarm either
    /// goes to look or walks away; who does which is the hearer's (SliceConfront, by the person's Boldness). It leaves
    /// whatever it was doing at once, a greeting or a visit, and goes by its own route, which the editor planned over
    /// walkable ground, so it never cuts through a house: one who looks walks round its route, the shorter way, to the
    /// route's point nearest the place, and on to 4 m short of it when the way there is clear, looks round 3 ticks and
    /// walks back; one who walks away walks its route, the shorter way, until it is 30 cells from the place, counted as
    /// the Alarm's hearing is, waits a tick facing it, and walks back. Both hurry there and stroll back, and take up their
    /// round where they left it.
    /// </summary>
    public sealed partial class TownsfolkWalker
    {
        /// <summary>Authoring default, m: how far short of the Alarm's place one who goes to look stands.</summary>
        public const float LookStandOff = 4f;

        /// <summary>10.2 §5e: one who goes to look looks round this many ticks, as a Warden does (authoring default).</summary>
        public const int LookTicks = 3;

        /// <summary>10.2 §5e: one who walks away waits this many ticks once it is 30 cells from the place.</summary>
        public const int WaitTicks = 1;

        /// <summary>10.2 §5e: how far from the place one who walks away goes, in cells counted as an Alarm is heard.</summary>
        public const int ClearCells = 30;

        /// <summary>Authoring defaults: one looking round turns this far either side of the place, over this many seconds.</summary>
        private const float LookSwing = 55f;

        private const float LookPeriod = 7f;

        private const float FallbackSecondsPerTick = 10f;

        private readonly List<Vector3> _way = new List<Vector3>();
        private int _wayAt;
        private int _heedStage; // 0 on its way there, 1 there, 2 on its way back
        private bool _heedLooks;
        private float _heedLeft;
        private float _heedTime;
        private Vector3 _heedAt;
        private bool _wasHurried;

        /// <summary>Whether it answers an Alarm now: on its way to look or away, there, or on its way back.</summary>
        public bool Heeding => _mode == Mode.Heed;

        /// <summary>Whether the Alarm it answers it goes to look at, rather than away from.</summary>
        public bool GoesToLook => Heeding && _heedLooks;

        /// <summary>Whether it stands where it went, looking round or waiting.</summary>
        public bool HeedThere => Heeding && _heedStage == 1;

        /// <summary>Whether it walks back to its round.</summary>
        public bool HeedReturning => Heeding && _heedStage == 2;

        /// <summary>The point it walks to, the last of its way there.</summary>
        public Vector3 HeedGoal => _way.Count > 0 ? _way[_way.Count - 1] : transform.position;

        /// <summary>How many Alarms it has answered.</summary>
        public int Heeds { get; private set; }

        private static float SecondsPerTick => GameClockDriver.RealSecondsPerTick > 0f ? GameClockDriver.RealSecondsPerTick : FallbackSecondsPerTick;

        /// <summary>The distance, in cells, at which an Alarm at <paramref name="at"/> is heard from <paramref name="from"/>: either way or across.</summary>
        public static float Cells(Vector3 at, Vector3 from) => Mathf.Max(Mathf.Abs(from.x - at.x), Mathf.Abs(from.z - at.z));

        /// <summary>
        /// Answers an Alarm at <paramref name="at"/>: goes to look when <paramref name="look"/>, else walks away. False when
        /// it cannot: a person with no route, one inside a door, or one answering an Alarm already.
        /// </summary>
        public bool Heed(Vector3 at, bool look)
        {
            if (!_started || route.Length < 2 || Inside || _mode == Mode.Heed)
            {
                return false;
            }

            Abandon(); // a greeting or a visit left at once
            var start = _mode == Mode.Back ? _back : transform.position;
            var forward = Along(start, 1);
            var backward = Along(start, -1);
            _way.Clear();
            if (look)
            {
                var f = Nearest(forward, at);
                var b = Nearest(backward, at);
                var (way, end, seg) = b.walk < f.walk ? (backward, b.point, b.segment) : (forward, f.point, f.segment);
                Take(way, seg, end);
                var off = Flat(end - at);
                if (off.magnitude > LookStandOff)
                {
                    var stand = at + (off.normalized * LookStandOff);
                    stand.y = end.y;
                    var knee = Vector3.up * KneeHeight;
                    if (!Physics.Linecast(end + knee, stand + knee, LayerMask.GetMask(KitLayers.Structure), QueryTriggerInteraction.Ignore))
                    {
                        _way.Add(stand);
                    }
                }
            }
            else
            {
                var f = Clear(forward, at);
                var b = Clear(backward, at);
                var useBackward = b.leaves != f.leaves ? b.leaves : b.leaves ? b.walk < f.walk : b.cells > f.cells || (Mathf.Approximately(b.cells, f.cells) && b.walk < f.walk);
                var pick = useBackward ? b : f;
                Take(useBackward ? backward : forward, pick.segment, pick.point);
            }

            _heedAt = at;
            _heedLooks = look;
            _heedStage = 0;
            _wayAt = 0;
            _wasHurried = Hurry;
            Hurry = true;
            _mode = Mode.Heed;
            _standing = 0f;
            Heeds++;
            Play(WalkState);
            return true;
        }

        private float HeedStep(float seconds)
        {
            switch (_heedStage)
            {
                case 0:
                    while (_wayAt < _way.Count)
                    {
                        if (!MoveTo(_way[_wayAt], ref seconds))
                        {
                            return seconds;
                        }

                        _wayAt++;
                    }

                    _heedStage = 1;
                    _heedTime = 0f;
                    _heedLeft = (_heedLooks ? LookTicks : WaitTicks) * SecondsPerTick;
                    Hurry = _wasHurried;
                    Play(_heedLooks ? IdleState : StopStates[(Fnv(_seed, (uint)Heeds) >> 12) % (uint)StopStates.Length]);
                    return seconds;
                case 1:
                    var used = Mathf.Min(_heedLeft, seconds);
                    _heedLeft -= used;
                    _heedTime += used;
                    var toward = Flat(_heedAt - transform.position);
                    if (_heedLooks && toward.sqrMagnitude > 1e-4f)
                    {
                        toward = Quaternion.Euler(0f, LookSwing * Mathf.Sin(_heedTime * 2f * Mathf.PI / LookPeriod), 0f) * toward;
                    }

                    Face(toward, turnDegreesPerSecond * used);
                    if (_heedLeft > 0f)
                    {
                        return 0f;
                    }

                    _heedStage = 2;
                    _wayAt = _way.Count - 2;
                    Play(WalkState);
                    return seconds - used;
                default:
                    while (_wayAt >= 0)
                    {
                        if (!MoveTo(_way[_wayAt], ref seconds))
                        {
                            return seconds;
                        }

                        _wayAt--;
                    }

                    _way.Clear();
                    _mode = Mode.Route;
                    Play(WalkState);
                    return seconds;
            }
        }

        /// <summary>The route walked from <paramref name="start"/> one whole round, forward or backward, back to the start.</summary>
        private List<Vector3> Along(Vector3 start, int direction)
        {
            var n = route.Length;
            var points = new List<Vector3>(n + 2) { start };
            for (var i = 0; i < n; i++)
            {
                points.Add(route[direction > 0 ? (_next + i) % n : (((_next - 1 - i) % n) + n) % n]);
            }

            points.Add(start);
            return points;
        }

        /// <summary>The way out: the walk's points up to segment <paramref name="segment"/>, and <paramref name="end"/> on it.</summary>
        private void Take(List<Vector3> walk, int segment, Vector3 end)
        {
            for (var i = 0; i <= segment; i++)
            {
                _way.Add(walk[i]);
            }

            _way.Add(end);
        }

        /// <summary>The walk's point nearest <paramref name="at"/>, the first of equals, its segment and how far it is walked.</summary>
        private static (Vector3 point, int segment, float walk) Nearest(List<Vector3> walk, Vector3 at)
        {
            var best = (point: walk[0], segment: 0, walk: 0f);
            var bestDistance = float.MaxValue;
            var walked = 0f;
            for (var i = 0; i + 1 < walk.Count; i++)
            {
                var a = walk[i];
                var ab = Flat(walk[i + 1] - a);
                var length = ab.magnitude;
                var t = length > 1e-4f ? Mathf.Clamp01(Vector3.Dot(Flat(at - a), ab) / (length * length)) : 0f;
                var point = Vector3.Lerp(a, walk[i + 1], t);
                var distance = Flat(at - point).magnitude;
                if (distance < bestDistance - 1e-3f)
                {
                    bestDistance = distance;
                    best = (point, i, walked + (length * t));
                }

                walked += length;
            }

            return best;
        }

        /// <summary>
        /// Where the walk first leaves the Alarm's hearing, 30 cells from <paramref name="at"/>, and how far that is walked;
        /// where it never does, its point farthest from the place.
        /// </summary>
        private static (bool leaves, Vector3 point, int segment, float walk, float cells) Clear(List<Vector3> walk, Vector3 at)
        {
            var far = (leaves: false, point: walk[0], segment: 0, walk: 0f, cells: Cells(at, walk[0]));
            var walked = 0f;
            for (var i = 0; i + 1 < walk.Count; i++)
            {
                var a = walk[i];
                var b = walk[i + 1];
                var length = Flat(b - a).magnitude;
                if (Cells(at, b) > ClearCells)
                {
                    // Out between a and b: halved down to a few centimetres.
                    float lo = 0f, hi = 1f;
                    if (Cells(at, a) > ClearCells)
                    {
                        hi = 0f;
                    }

                    for (var k = 0; k < 12 && hi > 0f; k++)
                    {
                        var mid = (lo + hi) * 0.5f;
                        if (Cells(at, Vector3.Lerp(a, b, mid)) > ClearCells)
                        {
                            hi = mid;
                        }
                        else
                        {
                            lo = mid;
                        }
                    }

                    var point = Vector3.Lerp(a, b, hi);
                    return (true, point, i, walked + (length * hi), Cells(at, point));
                }

                walked += length;
                if (Cells(at, b) > far.cells + 1e-3f)
                {
                    far = (false, b, i, walked, Cells(at, b));
                }
            }

            return far;
        }
    }
}
