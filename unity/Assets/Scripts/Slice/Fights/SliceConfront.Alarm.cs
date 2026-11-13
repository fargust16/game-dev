using System.Collections.Generic;
using System.Linq;
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
    /// beasts they join the character (owner, 2026-11-12, VS-ALARM (i)); in any other fight an Alarm brings nobody. A shot
    /// out of a fight (PB8b) sends every free Warden who hears it to the shooter's place to look round. The townsfolk's part
    /// is in SliceConfront.Heed (PB8c).
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
                // 10.2 §5a: the residents who hear a call say so; what each then does is its own (PB8c, below).
                foreach (var body in PeopleAround.InSight())
                {
                    if (body != alarm.Source && !PeopleAround.IsWarden(body) && !IsPlayer(body) && SliceAlarm.Hears(alarm.At, body.position) && !Lying(body))
                    {
                        Say(body, "ui.fight.help.heard");
                    }
                }
            }

            TownsfolkHear(alarm);
            if (!InFight && alarm.Kind == AlarmKind.Shot)
            {
                Debug.Log($"[Confront] a shot out of a fight: {LookAt(alarm.At)} Wardens go to look.");
                return;
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

        /// <summary>
        /// A Warden runs to <paramref name="at"/> and joins the fight on <paramref name="side"/> at the end of the round by which
        /// it is there; one of the church's people sent by the goddess comes the same way (PB8d), of its own group and line.
        /// </summary>
        private void Come(Transform body, Vector3 at, int side, string group = Wardens, string line = "ui.fight.help.warden")
        {
            // One on its way to look at a shot, or standing where a fight drew it, keeps the post it goes back to.
            var back = _returning.Find(r => r.Body == body) ?? _late.Find(k => k.Body == body);
            _returning.RemoveAll(r => r.Body == body);
            _late.RemoveAll(k => k.Body == body);
            _coming.Add(new Coming
            {
                Body = body,
                Combatant = Person(body, group, _nextId++, side: side),
                Round = _lastRound + TicksToRun(body.position, at),
                To = at,
                OnlyLooks = side == PlayerSide,
                Post = back != null ? back.Post : body.position,
                PostFacing = back != null ? back.PostFacing : body.rotation,
            });
            Say(body, line);
        }

        /// <summary>
        /// 10.2 §5e: a shot out of a fight. Every free Warden who hears it goes to the shooter's place, reckoned at 4.5 m/s
        /// as ever, looks round 3 ticks and goes back; a fight that starts meanwhile takes it in as any free Warden.
        /// </summary>
        private int LookAt(Vector3 at)
        {
            var came = 0;
            foreach (var body in PeopleAround.Wardens(at))
            {
                if (!SliceAlarm.Hears(at, body.position) || !Free(body) || _late.Exists(k => k.Body == body))
                {
                    continue;
                }

                _late.Add(new Coming
                {
                    Body = body,
                    Arrives = Time.time + (TicksToRun(body.position, at) * SecondsPerTick),
                    To = at,
                    OnlyLooks = true,
                    Post = body.position,
                    PostFacing = body.rotation,
                });
                Say(body, "ui.fight.shot.heard");
                came++;
            }

            return came;
        }

        /// <summary>The fight is over: the Wardens still on their way come late, each when its run is done.</summary>
        private void ComeLate()
        {
            foreach (var coming in _coming)
            {
                coming.Arrives = Time.time + (Mathf.Max(0, coming.Round - _lastRound) * SecondsPerTick);
                coming.OnlyLooks |= _church.Contains(coming.Body); // the church's people come late only to look (PB8d)
                _late.Add(coming);
            }

            _coming.Clear();
        }

        /// <summary>
        /// A Warden who can answer: standing, in no fight, on no errand or search, and not already on its way to a fight;
        /// one on its way only to look answers a call.
        /// </summary>
        private bool Free(Transform body) =>
            body != null && body.gameObject.activeInHierarchy && !Lying(body) && !IsPlayer(body) && !_peopleInFight.Contains(body)
            && !_coming.Exists(k => k.Body == body) && !_late.Exists(k => k.Body == body && !k.OnlyLooks)
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
                    SendALooker(body.position);
                }

                return; // only the first Warden there weighs it
            }
        }

        /// <summary>
        /// 10.2 §5e (S5c.7): the whistling Warden also sends a resident who came to look running to the nearest post
        /// beyond the whistle's hearing whose Wardens are at it; with nobody come to look, only the whistle calls. Like
        /// any runner it runs straight, and its coming is reckoned in ticks. Whether one was sent.
        /// </summary>
        public bool SendALooker(Vector3 place)
        {
            _lookers.RemoveAll(b => b == null || !b.gameObject.activeInHierarchy || Lying(b) || _peopleInFight.Contains(b)
                                    || b.GetComponent<HelpRun>() != null || _runners.Exists(r => r.Body == b));
            if (_lookers.Count == 0)
            {
                return false;
            }

            Transform post = null;
            foreach (var warden in PeopleAround.Wardens(place))
            {
                if (!SliceAlarm.Hears(place, warden.position) && AtPost(warden))
                {
                    post = warden; // the nearest beyond the whistle, Wardens(place) being nearest first
                    break;
                }
            }

            if (post == null)
            {
                return false;
            }

            var looker = _lookers.OrderBy(b => (b.position - place).sqrMagnitude).First();
            _lookers.Remove(looker);
            HelpRun.Begin(looker, post.position, _clock);
            SendRunner(looker, post.position, place);
            Say(looker, "ui.fight.help.runner");
            Debug.Log($"[Confront] {looker.name}, come to look, runs for the Wardens at {post.position}.");
            return true;
        }

        /// <summary>
        /// A resident who broke away runs for help to the post it fled toward: the runner (10.2 §5a, §5e). It is there
        /// after the ticks a run of 4.5 m/s takes, and the post's Wardens set out unless it fell on its way.
        /// </summary>
        public void SendRunner(Transform body, Vector3 post) => SendRunner(body, post, _assaultAt);

        /// <summary>A runner sent for the fight at <paramref name="place"/> (S5c.7: one come to look, sent by a whistling Warden).</summary>
        public void SendRunner(Transform body, Vector3 post, Vector3 place)
        {
            if (_clock != null && body != null)
            {
                _runners.Add(new Runner { Body = body, Post = post, Place = place, ForAssault = Assaulting, Due = _clock.Tick + TicksToRun(body.position, post) });
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
                        SetOut(warden, runner.Place, runner.ForAssault);
                        set++;
                    }
                }

                Debug.Log(set == 0 ? "[Confront] the runner finds its post empty: nobody comes." : $"[Confront] the runner sets {set} Wardens out.");
            }
        }

        /// <summary>
        /// A post's Warden sets out for the runner's place: into its fight while it lasts, against the character in an
        /// Assault and beside it against the cult, the Veinmarket or beasts; or late, to look round, when it is over.
        /// </summary>
        private void SetOut(Transform warden, Vector3 place, bool forAssault)
        {
            var side = !InFight ? -1 : Assaulting ? HostileSide : WardensJoinTheCharacter() ? PlayerSide : -1;
            if (side >= 0)
            {
                Come(warden, place, side);
                return;
            }

            _late.Add(new Coming
            {
                Body = warden,
                Arrives = Time.time + (TicksToRun(warden.position, place) * SecondsPerTick),
                To = place,
                Post = warden.position,
                PostFacing = warden.rotation,
                OnlyLooks = !forAssault, // late to an Assault it fights the character near the place; to any other fight it only looks
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
            public Vector3 Place;
            public bool ForAssault;
            public long Due;
        }
    }
}
