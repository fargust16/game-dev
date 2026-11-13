using System;
using System.Collections.Generic;
using GameDev.Confront;
using GameDev.Confront.Rules;
using GameDev.Confront.Turns;
using GameDev.Locomotion;
using GameDev.Player.Standing;
using GameDev.Slice.People;
using GameDev.UI;
using GameDev.World.Timekeeping;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// 10.2 §5c: an Assault. A blow at a person who is not fighting starts a fight in place, the blow its opening strike;
    /// the one struck fights back with what 10.2 §5d gives it, a Warden its firearm and a resident its role's or its fists
    /// (<see cref="Armed"/>). Its cry is an Alarm (10.2 §5e, PB8a): every free Warden within its 30 cells joins on its side
    /// at the end of the round by which it has run there at 4.5 m/s, a round moving the world one tick, and enters at the
    /// grid's edge on its side; others come by a whistle or a runner (<see cref="OnAlarm"/>). A Warden still on its way
    /// when the fight is over comes to the place, and if the character is within 20 m of it, fights it at once. A Warden left standing goes
    /// back to its post once the character is out of sight of both. Reputation falls by the Assault, by the harm the people
    /// who were not enemies took, and by each kill of one of them, unseen too; their fallen lie a game day, or until the
    /// nearest free Warden walks to them and takes them up (<see cref="WardenErrand"/>).
    /// </summary>
    public sealed partial class SliceConfront
    {
        /// <summary>The ordinary residents of the city, as a fight's group.</summary>
        public const string Residents = "residents";

        // Authoring defaults, 10.2 §5c, §5e: the Wardens run at 4.5 m/s; one come late fights a character within 20 m.
        public const float WardenRunSpeed = 4.5f;
        public const float LateWardenReach = 20f;

        // Authoring defaults: a Warden enters the grid this far from the player, m, and comes late this far from the
        // place; it is back at its post once the character is this far from it and from the post, m; a tick's ten real
        // seconds where no clock runs.
        private const float EdgeReach = 16f;
        private const float LateStandOff = 3f;
        private const float PostReturnDistance = 40f;
        private const float FallbackSecondsPerTick = 10f;

        /// <summary>10.2 §5c: a body an Assault left lies a game day at most, in ticks.</summary>
        public const int BodyTicks = 1440;

        private static readonly int IdleState = Animator.StringToHash("Idle");

        private readonly List<Coming> _coming = new List<Coming>();
        private readonly List<Coming> _late = new List<Coming>();
        private readonly List<Coming> _returning = new List<Coming>();
        private readonly List<TownsfolkWalker> _stopped = new List<TownsfolkWalker>();
        private readonly HashSet<Combatant> _innocents = new HashSet<Combatant>();
        private readonly List<Fallen> _fallen = new List<Fallen>();
        private WardenErrand _errand;
        private Vector3 _assaultAt;
        private int _lastRound;

        /// <summary>The HP the people who were not enemies lost in the last Assault's fight.</summary>
        public double LastHarm { get; private set; }

        /// <summary>The bodies an Assault left that still lie where they fell.</summary>
        public int FallenLying => _fallen.Count;

        /// <summary>Raised as an Assault begins, with the body struck.</summary>
        public event Action<Transform> Assaulted;

        /// <summary>True from an Assault's start until its fight is over.</summary>
        public bool Assaulting { get; private set; }

        /// <summary>Where the last Assault was, the one struck's place.</summary>
        public Vector3 AssaultPlace => _assaultAt;

        /// <summary>The people of an Assault who were not the character's enemies: the one struck and the Wardens who came.</summary>
        public bool Innocent(Combatant c) => c != null && _innocents.Contains(c);

        /// <summary>The Wardens on their way to the place, during the fight or after it.</summary>
        public int WardensComing => _coming.Count + _late.Count;

        /// <summary>
        /// 10.2 §5c: the character strikes <paramref name="victim"/>, who is not fighting: the fight starts with that blow.
        /// False when no fight can start here.
        /// </summary>
        /// <param name="fighter">The one struck as it already is, as one a shot wounded; null makes it from who it is.</param>
        /// <param name="opened">
        /// The opening strike was dealt already (10.2 §5e: a shot seen by the one it touched, rolled on the touch), so the
        /// fight strikes it no more.
        /// </param>
        public bool StartAssault(Transform victim, Fighter fighter = null, bool opened = false)
        {
            _player = FindAnyObjectByType<PlayerLocomotion>();
            if (_lock == null || InFight || _died || _terms != null || _player == null || _player.InWater || victim == null ||
                _settings.Fists == null || _settings.PeoplePistol == null || _settings.KitRifle == null || _settings.KitPouch == null)
            {
                return false;
            }

            var at = _player.transform.position;
            _grid.Follow(at);
            if (!_grid.TryCellAt(at, out _))
            {
                return false;
            }

            _fights++;
            _bodies.Clear();
            _names.Clear();
            _groups.Clear();
            _peopleInFight.Clear();
            _innocents.Clear();
            _coming.Clear();
            _stopped.Clear();
            _assaultAt = victim.position;
            var player = new Combatant(PlayerId, PlayerSide, PlayerFighter(), at, takesIntents: !AutoPlayer);
            Equip(player);
            var combatants = new List<Combatant> { player };
            _bodies[player] = _player.transform;
            _names[player] = "ui.fight.you";
            _playerCombatant = player;

            var warden = PeopleAround.IsWarden(victim);
            var frozen = HeldStill.Frozen(victim); // 05.1 Стой! out of a fight (S5b.8): it calls no help
            var asleep = Slumber.Asleep(victim);
            var struck = Person(victim, warden ? Wardens : Residents, 1, fighter);
            if (frozen)
            {
                struck.Fighter.LostTurns = Math.Max(struck.Fighter.LostTurns, 1); // it stands frozen, its turn lost
            }

            if (asleep)
            {
                struck.Fighter.Asleep = Math.Max(struck.Fighter.Asleep, 1); // Дрёма: struck asleep, the blow wakes it
            }
            _bodies[struck] = victim;
            combatants.Add(struck);
            _nextId = 2;
            _lastRound = 0;
            Whistled = false;
            _there.Clear();
            if (warden)
            {
                _there.Add(struck);
            }

            // 10.2 §5e: the cry is an Alarm, and every free Warden who hears it runs to the place, joining at the end of
            // the round by which it is there.
            Assaulting = true;
            if (!frozen)
            {
                SliceAlarm.Raise(new Alarm(AlarmKind.Call, _assaultAt, victim));
            }

            if (!warden)
            {
                // 10.2 §5a: a resident fights back only with a chance, and otherwise runs for help to a post that did not answer.
                struck.FightsAt = FightsBackAt;
                struck.HelpAt = frozen ? null : HelpFor(victim);
            }

            var lawful = new List<Fighter> { struck.Fighter };
            lawful.AddRange(_coming.ConvertAll(k => k.Combatant.Fighter));
            var joined = frozen ? 0 : JoinTheCall(player.Fighter, lawful, victim, ref _nextId); // no call, nobody joins it
            Debug.Log($"[Confront] the call: {_coming.Count} Wardens coming, {joined} residents join.");

            // The blow is the fight's opening strike, a stealth attack only from Stealth Mode (10.3 §7); a shot rolled on
            // its touch is struck already.
            player.Hidden = !opened && Hiding != null && Hiding.Active;
            var request = new EncounterRequest(at, combatants, _seed.Fork(SliceSeedStreams.Combat, _fights), "assault")
            {
                Opener = opened ? null : player,
                Target = opened ? null : struck,
            };
            if (Begin(request, assault: true))
            {
                Standing?.Record(Deed.Assault);
                _peopleEnded = AssaultOver;
                Assaulted?.Invoke(victim);
                return true;
            }

            Assaulting = false;
            WalkAgain();
            _coming.Clear();
            _innocents.Clear();
            Unarm();
            _playerCombatant = null;
            return false;
        }

        /// <summary>
        /// A person of <paramref name="group"/> as a fighter with what it carries (<see cref="Armed"/>), out of its walk;
        /// on the character's side, a Warden come to its help (VS-ALARM (i)), its harm is no harm the character did.
        /// </summary>
        private Combatant Person(Transform body, string group, int id, Fighter fighter = null, int side = HostileSide)
        {
            var c = new Combatant(id, side, fighter ?? Armed(body, group), body.position) { AcceptsYield = TakesYield(group) };
            if (!body.TryGetComponent<SliceFighter>(out var part))
            {
                part = body.gameObject.AddComponent<SliceFighter>();
            }

            part.Combatant = c;
            if (body.GetComponentInParent<INameTag>() == null)
            {
                _names[c] = group == Wardens ? "ui.fight.warden" : "ui.fight.resident";
            }

            _groups[c] = group;
            if (side == HostileSide)
            {
                _innocents.Add(c);
            }

            _peopleInFight.Add(body);

            // A walker stops what it does, and stands where its fight's guard begins.
            if (body.TryGetComponent<TownsfolkWalker>(out var walker) && walker.enabled)
            {
                walker.enabled = false;
                _stopped.Add(walker);
                var animator = body.GetComponentInChildren<Animator>();
                if (animator != null && animator.HasState(0, IdleState))
                {
                    animator.CrossFadeInFixedTime(IdleState, 0.15f, 0);
                }
            }

            return c;
        }

        /// <summary>At a round's end, before the bodies on the grid join: the Wardens who have run here enter at its edge.</summary>
        private void ArriveWardens(int round)
        {
            _lastRound = round;
            for (var i = _coming.Count - 1; i >= 0; i--)
            {
                var coming = _coming[i];
                if (coming.Round > round)
                {
                    continue;
                }

                _coming.RemoveAt(i);
                if (coming.Body == null)
                {
                    continue;
                }

                if (!_grid.TryCellAt(coming.Body.position, out _) && Edge(coming.Body.position) is Vector3 edge)
                {
                    Place(coming.Body, edge, _player.transform.position);
                }

                if (coming.Combatant != null)
                {
                    _there.Add(coming.Combatant);
                }

                _returning.Add(coming);
            }
        }

        /// <summary>The Assault's fight is over: the walkers still standing walk again, and the Wardens on their way come late.</summary>
        private void AssaultOver(bool won, int spared)
        {
            Assaulting = false;
            Aftermath();
            WalkAgain();
            ComeLate();
        }

        private void WalkAgain()
        {
            foreach (var walker in _stopped)
            {
                // A searcher and one gone for help walk again when they are done (10.2 §5a, §9).
                if (walker != null && walker.gameObject.activeInHierarchy && !Lying(walker.transform)
                    && walker.GetComponent<FightSearch>() == null && walker.GetComponent<HelpRun>() == null)
                {
                    walker.enabled = true;
                }
            }

            _stopped.Clear();
        }

        /// <summary>
        /// Out of a fight, each frame: a Warden come late stands at the place and, with the character within 20 m, fights
        /// it; a Warden left standing is back at its post once the character is far from it and from the post.
        /// </summary>
        private void WardensAfterAssault()
        {
            if (_player == null)
            {
                return;
            }

            List<Transform> arrived = null;
            for (var i = _late.Count - 1; i >= 0; i--)
            {
                var late = _late[i];
                if (late.Body == null || !late.Body.gameObject.activeInHierarchy || Lying(late.Body))
                {
                    _late.RemoveAt(i);
                    continue;
                }

                if (Time.time < late.Arrives)
                {
                    continue;
                }

                _late.RemoveAt(i);
                var way = Vector3.ProjectOnPlane(late.Post - late.To, Vector3.up);
                way = way.sqrMagnitude > 1e-4f ? way.normalized : Vector3.forward;
                Place(late.Body, Ground(late.To + (way * LateStandOff)), _player.transform.position);
                late.LookUntil = (_clock != null ? _clock.Tick : 0) + FightSearch.LookTicks; // 10.2 §5e: it looks round
                _returning.Add(late);
                if (!late.OnlyLooks)
                {
                    (arrived ??= new List<Transform>()).Add(late.Body); // one come to help or to a shot fights nobody
                }
            }

            if (arrived != null && !_died && !InFight && (_player.transform.position - _assaultAt).sqrMagnitude <= LateWardenReach * LateWardenReach)
            {
                _innocents.Clear();
                if (StartPeopleFight(Wardens, arrived, (_, _) => Aftermath()))
                {
                    foreach (var body in arrived)
                    {
                        if (body.TryGetComponent<SliceFighter>(out var fighter) && fighter.Combatant != null)
                        {
                            _innocents.Add(fighter.Combatant);
                        }
                    }
                }
            }

            var at = _player.transform.position;
            for (var i = _returning.Count - 1; i >= 0; i--)
            {
                var r = _returning[i];
                if (r.Body == null || Lying(r.Body) || _late.Contains(r))
                {
                    _returning.RemoveAt(i);
                    continue;
                }

                if (r.Body.TryGetComponent<FightSearch>(out _) || LooksRound(r))
                {
                    continue; // a searcher goes back by its own walk (10.2 §9), one come late after its look (§5e)
                }

                if ((r.Body.position - at).sqrMagnitude > PostReturnDistance * PostReturnDistance &&
                    (r.Post - at).sqrMagnitude > PostReturnDistance * PostReturnDistance)
                {
                    // One of the church's people goes back in at the old chapel's door (PB8d); a Warden stands at its post.
                    if (!GoesBackIn(r.Body))
                    {
                        r.Body.SetPositionAndRotation(r.Post, r.PostFacing);
                    }

                    _returning.RemoveAt(i);
                }
            }

            TakeUpTheFallen();
        }

        /// <summary>10.2 §5c: the harm the people who were not enemies took costs Reputation, and their fallen lie where they fell.</summary>
        private void Aftermath()
        {
            var harm = 0d;
            foreach (var c in _innocents)
            {
                harm += Math.Max(0d, c.Fighter.MaxHp - Math.Max(0d, c.Fighter.Hp));
                if (c.Fighter.Down && _bodies.TryGetValue(c, out var body) && body != null && !_fallen.Exists(f => f.Body == body.gameObject))
                {
                    // A body lies on the ground and stops nobody.
                    foreach (var part in body.GetComponents<Collider>())
                    {
                        part.enabled = false;
                    }

                    _fallen.Add(new Fallen { Body = body.gameObject, Tick = _clock != null ? _clock.Tick : 0 });
                }
            }

            LastHarm = harm;
            Standing?.RecordHarm(harm);
            Debug.Log($"[Confront] the Assault's harm {harm:0.#} HP, {_fallen.Count} fallen.");
            _innocents.Clear();
        }

        /// <summary>A body gone after its day; the nearest free Warden sent for the rest once no Warden is still on its way.</summary>
        private void TakeUpTheFallen()
        {
            for (var i = _fallen.Count - 1; i >= 0; i--)
            {
                var f = _fallen[i];
                if (f.Body == null || !f.Body.activeSelf)
                {
                    _fallen.RemoveAt(i);
                }
                else if (_clock != null && _clock.Tick >= f.Tick + BodyTicks)
                {
                    f.Body.SetActive(false);
                    _fallen.RemoveAt(i);
                }
            }

            if (_errand != null || _late.Count > 0 || InFight)
            {
                return;
            }

            var waiting = _fallen.FindAll(f => !f.Sent);
            if (waiting.Count == 0)
            {
                return;
            }

            var taker = PeopleAround.Wardens(waiting[0].Body.transform.position)
                .Find(w => !Lying(w) && w.GetComponent<WardenErrand>() == null && w.GetComponent<SliceFighter>() == null);
            if (taker == null)
            {
                return;
            }

            var post = _returning.Find(r => r.Body == taker);
            _returning.RemoveAll(r => r.Body == taker);
            foreach (var f in waiting)
            {
                f.Sent = true;
            }

            _errand = WardenErrand.Send(taker, waiting.ConvertAll(f => f.Body), post != null ? post.Post : taker.position, post != null ? post.PostFacing : taker.rotation);
            Debug.Log($"[Confront] {taker.name} goes to take up {waiting.Count} fallen.");
        }

        /// <summary>The cell nearest the grid's edge toward <paramref name="from"/>, or null.</summary>
        private Vector3? Edge(Vector3 from)
        {
            var centre = _player.transform.position;
            var way = Vector3.ProjectOnPlane(from - centre, Vector3.up);
            way = way.sqrMagnitude > 1e-4f ? way.normalized : Vector3.forward;
            for (var m = EdgeReach; m >= 2f; m -= 1f)
            {
                if (_grid.TryCellAt(centre + (way * m), out var cell))
                {
                    return _grid.Center(cell);
                }
            }

            return null;
        }

        private static void Place(Transform body, Vector3 at, Vector3 facing)
        {
            var look = Vector3.ProjectOnPlane(facing - at, Vector3.up);
            body.SetPositionAndRotation(at, look.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(look.normalized, Vector3.up) : body.rotation);
        }

        private static Vector3 Ground(Vector3 at)
        {
            return PeopleAround.Ground(at, 3f, 8f);
        }

        /// <summary>A body that lies fallen: its controller's Down is set.</summary>
        private static bool Lying(Transform body)
        {
            var animator = body != null ? body.GetComponentInChildren<Animator>() : null;
            return animator != null && animator.isActiveAndEnabled && Array.Exists(animator.parameters, p => p.nameHash == DownHash) && animator.GetBool(DownHash);
        }

        private sealed class Fallen
        {
            public GameObject Body;
            public long Tick;
            public bool Sent;
        }

        private sealed class Coming
        {
            public Transform Body;
            public Combatant Combatant;
            public int Round;
            public float Arrives;
            public long LookUntil;

            /// <summary>The place it runs to, the Alarm's.</summary>
            public Vector3 To;

            /// <summary>
            /// It fights nobody when it comes and only looks round: one come to the character's help (VS-ALARM (i)), or
            /// one come to a shot heard out of a fight (10.2 §5e).
            /// </summary>
            public bool OnlyLooks;

            public Vector3 Post;
            public Quaternion PostFacing;
        }
    }
}
