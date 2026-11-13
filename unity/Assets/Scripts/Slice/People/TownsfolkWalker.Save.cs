using System;
using GameDev.Core.Save;
using GameDev.Slice.Save;
using UnityEngine;

namespace GameDev.Slice.People
{
    /// <summary>
    /// A walker in a Suspend Save (17 X.3; the owner's answer to S5-7): where it stands and faces, how far round its route
    /// it has come, and what it is doing there: on its way to a place, passing its while at it seated, inside a door or at
    /// a stall, or walking back. A nod and a look toward an Alarm last seconds and are not kept: the walker walks on from
    /// where it stood. A place gone since, its chair taken away, sends it back to its route.
    /// </summary>
    public sealed partial class TownsfolkWalker : ISavedObject
    {
        [Serializable]
        private sealed class Saved
        {
            public Vector3 at;
            public float yaw;
            public bool on;
            public int mode;
            public int next;
            public int turns;
            public int stops;
            public int visitsBegun;
            public int greetings;
            public float standing;
            public float owed;
            public float sinceGreet;
            public string spot;
            public int place;
            public Vector3 back;
            public int step;
            public float phase;
            public float left;
            public bool seated;
            public bool hurry;
        }

        public string SaveId => SavedObjects.PathOf(transform);

        public string CaptureState()
        {
            if (!_started)
            {
                return null;
            }

            var away = _mode == Mode.Going || _mode == Mode.Busy;
            return JsonUtility.ToJson(new Saved
            {
                at = transform.position,
                yaw = transform.eulerAngles.y,
                on = enabled,
                mode = (int)_mode,
                next = _next,
                turns = _turns,
                stops = Stops,
                visitsBegun = VisitsBegun,
                greetings = Greetings,
                standing = _standing,
                owed = _owed,
                sinceGreet = _sinceGreet,
                spot = away && _spot != null ? SavedObjects.PathOf(_spot.transform) : string.Empty,
                place = _place,
                back = _back,
                step = _step,
                phase = _phase,
                left = float.IsInfinity(_left) ? -1f : _left,
                seated = _seated,
                hurry = _mode == Mode.Heed ? _wasHurried : Hurry,
            });
        }

        public void ClearState()
        {
            if (!_started)
            {
                return;
            }

            if (Inside)
            {
                Show();
            }

            _seated = false;
            _partner = null;
            Release();
        }

        public void RestoreState(string payload)
        {
            if (!_started)
            {
                return;
            }

            var s = JsonUtility.FromJson<Saved>(payload);
            transform.SetPositionAndRotation(s.at, Quaternion.Euler(0f, s.yaw, 0f));
            _next = route.Length > 0 ? Mathf.Clamp(s.next, 0, route.Length - 1) : 0;
            _turns = s.turns;
            Stops = s.stops;
            Greetings = s.greetings;
            _standing = s.standing;
            _owed = s.owed;
            _sinceGreet = s.sinceGreet;
            _back = s.back;
            Hurry = s.hurry;
            _heedStage = 0;

            var mode = (Mode)s.mode;
            var spot = string.IsNullOrEmpty(s.spot) ? null : SavedObjects.FindPath(s.spot)?.GetComponent<ActivitySpot>();
            if ((mode == Mode.Going || mode == Mode.Busy) && spot != null && Begin(spot, s.place, s.back))
            {
                transform.SetPositionAndRotation(s.at, Quaternion.Euler(0f, s.yaw, 0f));
                if (mode == Mode.Busy)
                {
                    Resume(s);
                }
            }
            else if (mode == Mode.Going || mode == Mode.Busy || mode == Mode.Back)
            {
                _mode = Mode.Back;
                Play(WalkState);
            }
            else if (mode == Mode.Stand)
            {
                _mode = Mode.Stand;
                Play(IdleState);
            }
            else
            {
                _mode = route.Length < 2 ? Mode.Back : Mode.Route; // a nod or a look is not kept
                Play(WalkState);
            }

            VisitsBegun = s.visitsBegun;
            if (!s.on)
            {
                enabled = false;
            }
        }

        /// <summary>The while at a place taken up where it was: seated on its seat, inside its door, at its stall's step.</summary>
        private void Resume(Saved s)
        {
            _mode = Mode.Busy;
            _step = s.step;
            _phase = s.phase;
            _left = s.left < 0f ? float.PositiveInfinity : s.left;
            if (_step == 0)
            {
                Play(IdleState); // still settling to the place's facing
                return;
            }

            switch (_spot.Kind)
            {
                case ActivitySpot.Activity.Sit:
                    _seated = true;
                    if (_step == 1)
                    {
                        // Sitting down: seated at once, the clip's second left out.
                        _step = 2;
                        _phase = 4f;
                    }

                    Play(_step >= 3 ? SitToStandState : SitState);
                    break;
                case ActivitySpot.Activity.GoIn:
                    Hide();
                    break;
                case ActivitySpot.Activity.Attend:
                    Play(IdleState);
                    break;
                default:
                    var steps = _spot.Kind == ActivitySpot.Activity.Drink ? Drinking : Bargaining;
                    Play(steps[(Mathf.Max(1, _step) - 1) % steps.Length].state);
                    break;
            }
        }
    }
}
