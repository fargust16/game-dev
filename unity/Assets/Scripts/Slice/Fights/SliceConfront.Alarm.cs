using System.Collections.Generic;
using GameDev.Confront.Turns;
using GameDev.Slice.People;
using GameDev.World.Timekeeping;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// 10.2 §5e, the Wardens' part of the Alarm Check (owner, 2026-11-12; PB8a). Every free Warden who hears an Alarm of
    /// an Assault, the one struck's call, a shot in its fight or a whistle, runs to its place at 4.5 m/s and joins the
    /// fight at the end of the round by which it is there. At each round's end the first Warden there weighs the fight by
    /// the Force Estimate, and below 1 it blows its whistle, an Alarm of its own, once in a fight. A resident who breaks
    /// away is the runner: it runs to the nearest post whose Wardens did not answer, reckoned at 4.5 m/s as the Wardens
    /// are, and when it gets there the post's Wardens set out; at an empty post nothing happens, and a runner fallen on its
    /// way brings nobody. A Warden who comes when the fight is over looks round 3 ticks before it goes back. In an Assault
    /// the Wardens take the side of the one struck, against the character; in a fight with the cult, the Veinmarket or
    /// beasts they join the character (owner, 2026-11-12, VS-ALARM (i)); in any other fight an Alarm brings nobody.
    /// </summary>
    public sealed partial class SliceConfront
    {
        /// <summary>Authoring default, 10.2 §5e: the Force Estimate below which the first Warden there whistles.</summary>
        public const double WhistlesBelow = 1d;

        /// <summary>Authoring default: the Wardens within this many metres of a runner's goal are its post's, m.</summary>
        public const float PostReach = 6f;

        private const float LookRate = 60f; // degrees a second, a Warden looking round, as a searcher does

        private readonly List<Runner> _runners = new List<Runner>();
        private readonly List<Combatant> _there = new List<Combatant>();
        private int _nextId;

        /// <summary>Whether a Warden whistled in the Assault under way, or in the last.</summary>
        public bool Whistled { get; private set; }

        /// <summary>The runners still on their way to a post.</summary>
        public int Runners => _runners.Count;

        /// <summary>The Wardens on their way to the place, during the fight or after it.</summary>
        public List<Transform> ComingBodies
        {
            get
            {
                var bodies = _coming.ConvertAll(k => k.Body);
                bodies.AddRange(_late.ConvertAll(k => k.Body));
                return bodies;
            }
        }

        private static float SecondsPerTick => GameClockDriver.RealSecondsPerTick > 0f ? GameClockDriver.RealSecondsPerTick : FallbackSecondsPerTick;

        /// <summary>The world ticks a run of 4.5 m/s takes from one place to another, one at least.</summary>
        private static int TicksToRun(Vector3 from, Vector3 to) => Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(from, to) / (WardenRunSpeed * SecondsPerTick)));

        private void OnAlarm(Alarm alarm)
        {
            if (alarm.Source != null && alarm.Kind != AlarmKind.Shot)
            {
                Say(alarm.Source, alarm.Kind == AlarmKind.Call ? "ui.fight.help.call" : "ui.fight.whistle");
            }

            if (alarm.Kind == AlarmKind.Call)
            {
                // 10.2 §5a: the residents who hear a call say so; what each then does is its own (PB8c).
                foreach (var body in PeopleAround.InSight())
                {
                    if (body != alarm.Source && !PeopleAround.IsWarden(body) && !IsPlayer(body) && SliceAlarm.Hears(alarm.At, body.position) && !Lying(body))
                    {
                        Say(body, "ui.fight.help.heard");
                    }
                }
            }

            var side = Assaulting ? HostileSide : WardensJoinTheCharacter() ? PlayerSide : -1;
            if (side >= 0)
            {
                var came = Answer(alarm.At, side);
                Debug.Log($"[Confront] the {alarm.Kind.ToString().ToLowerInvariant()}: {came} Wardens set out, on side {side}.");
            }
        }

        /// <summary>
        /// VS-ALARM (i) (owner, 2026-11-12): whether the fight under way is one the Wardens join beside the character,
        /// every enemy in it of the cult, of the Veinmarket, or a beast.
        /// </summary>
        private bool WardensJoinTheCharacter()
        {
            if (!InFight || Assaulting || _playerCombatant == null)
            {
                return false;
            }

            var enemies = 0;
            foreach (var c in _bodies.Keys)
            {
                if (c.Side == _playerCombatant.Side)
                {
                    continue;
                }

                var group = Group(c);
                if (!c.Beast && group != Cult && group != Veinmarket)
                {
                    return false;
                }

                enemies++;
            }

            return enemies > 0;
        }

        /// <summary>Every free Warden who hears an Alarm at <paramref name="at"/> runs to it, to fight on <paramref name="side"/>; how many set out.</summary>
        private int Answer(Vector3 at, int side)
        {
            var came = 0;
            foreach (var body in PeopleAround.Wardens(at))
            {
                if (SliceAlarm.Hears(at, body.position) && Free(body))
                {
                    Come(body, at, side);
                    came++;
                }
            }

            return came;
        }

        /// <summary>A Warden runs to <paramref name="at"/> and joins the fight on <paramref name="side"/> at the end of the round by which it is there.</summary>
        private void Come(Transform body, Vector3 at, int side)
        {
            var back = _returning.Find(r => r.Body == body);
            _returning.RemoveAll(r => r.Body == body);
            _coming.Add(new Coming
            {
                Body = body,
                Combatant = Person(body, Wardens, _nextId++, side: side),
                Round = _lastRound + TicksToRun(body.position, at),
                To = at,
                Allied = side == PlayerSide,
                Post = back != null ? back.Post : body.position,
                PostFacing = back != null ? back.PostFacing : body.rotation,
            });
            Say(body, "ui.fight.help.warden");
        }

        /// <summary>The fight is over: the Wardens still on their way come late, each when its run is done.</summary>
        private void ComeLate()
        {
            foreach (var coming in _coming)
            {
                coming.Arrives = Time.time + (Mathf.Max(0, coming.Round - _lastRound) * SecondsPerTick);
                _late.Add(coming);
            }

            _coming.Clear();
        }

        /// <summary>A Warden who can answer: standing, in no fight, on no errand or search, and not already on its way.</summary>
        private bool Free(Transform body) =>
            body != null && body.gameObject.activeInHierarchy && !Lying(body) && !IsPlayer(body) && !_peopleInFight.Contains(body)
            && !_coming.Exists(k => k.Body == body) && !_late.Exists(k => k.Body == body)
            && body.GetComponent<FightSearch>() == null && body.GetComponent<WardenErrand>() == null
            && !(body.TryGetComponent<SliceFighter>(out var fighter) && fighter.Combatant != null);

        /// <summary>A Warden at its own post: free, and not still standing where a fight drew it.</summary>
        private bool AtPost(Transform body) => Free(body) && !_returning.Exists(r => r.Body == body);

        private bool IsPlayer(Transform body) => _player != null && body != null && _player.transform.IsChildOf(body);

        /// <summary>At a round's end: the first Warden there weighs the fight, and below 1 whistles, once in a fight.</summary>
        private void WeighTheFight()
        {
            if (Whistled || !_turns.IsRunning)
            {
                return;
            }

            foreach (var c in _there)
            {
                if (!c.Standing || c.Departure != Departure.None || !_bodies.TryGetValue(c, out var body) || body == null)
                {
                    continue;
                }

                if (_turns.ForceEstimate(c) < WhistlesBelow)
                {
                    Whistled = true;
                    SliceAlarm.Raise(new Alarm(AlarmKind.Whistle, body.position, body));
                }

                return; // only the first Warden there weighs it
            }
        }

        /// <summary>
        /// A resident who broke away runs for help to the post it fled toward: the runner (10.2 §5a, §5e). It is there
        /// after the ticks a run of 4.5 m/s takes, and the post's Wardens set out unless it fell on its way.
        /// </summary>
        public void SendRunner(Transform body, Vector3 post)
        {
            if (_clock != null && body != null)
            {
                _runners.Add(new Runner { Body = body, Post = post, Due = _clock.Tick + TicksToRun(body.position, post) });
            }
        }

        /// <summary>Each world tick, in a fight's rounds too: a runner come to its post sets the post's Wardens out.</summary>
        private void OnAlarmTick(long tick)
        {
            for (var i = _runners.Count - 1; i >= 0; i--)
            {
                var runner = _runners[i];
                if (runner.Due > tick)
                {
                    continue;
                }

                _runners.RemoveAt(i);
                if (runner.Body == null || !runner.Body.gameObject.activeInHierarchy || Lying(runner.Body))
                {
                    Debug.Log("[Confront] a runner fell on its way: nobody comes.");
                    continue;
                }

                var set = 0;
                foreach (var warden in PeopleAround.Wardens(runner.Post))
                {
                    if ((warden.position - runner.Post).sqrMagnitude > PostReach * PostReach)
                    {
                        break;
                    }

                    if (AtPost(warden))
                    {
                        SetOut(warden);
                        set++;
                    }
                }

                Debug.Log(set == 0 ? "[Confront] the runner finds its post empty: nobody comes." : $"[Confront] the runner sets {set} Wardens out.");
            }
        }

        /// <summary>A post's Warden sets out for the Assault's place: into its fight while it lasts, or late.</summary>
        private void SetOut(Transform warden)
        {
            if (Assaulting && InFight)
            {
                Come(warden, _assaultAt, HostileSide);
                return;
            }

            _late.Add(new Coming
            {
                Body = warden,
                Arrives = Time.time + (TicksToRun(warden.position, _assaultAt) * SecondsPerTick),
                To = _assaultAt,
                Post = warden.position,
                PostFacing = warden.rotation,
            });
            Say(warden, "ui.fight.help.warden");
        }

        /// <summary>A Warden come when the fight is over, looking round its ticks before it goes back; false once done.</summary>
        private bool LooksRound(Coming r)
        {
            if (_clock == null || _clock.Tick >= r.LookUntil)
            {
                return false;
            }

            if (!_clock.IsPaused)
            {
                r.Body.Rotate(0f, LookRate * Time.deltaTime, 0f);
            }

            return true;
        }

        private sealed class Runner
        {
            public Transform Body;
            public Vector3 Post;
            public long Due;
        }
    }
}
