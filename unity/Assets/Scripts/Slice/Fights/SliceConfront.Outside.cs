using System;
using System.Collections.Generic;
using GameDev.Confront.Grid;
using GameDev.Confront.Rules;
using GameDev.Core.Localization;
using GameDev.Dialogue;
using GameDev.Input;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Slice.Alchemy;
using GameDev.Slice.People;
using GameDev.Slice.Stealth;
using GameDev.World.Seed;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// 05.1 *Вне боя* (S5b.8; owner, S5-3): the character's abilities in free exploration, at the price they have in a
    /// fight, while the world goes on in ticks. A key 1 to 9 uses the n-th active ability with a use out of a fight, as
    /// the page of Prepared Abilities numbers them; the Prepared slots bind in a fight alone (07.2 §4.5). Внушение keeps
    /// +2 for the next Persuasion check; Пелена raises a cloud that puts out the cones through it; Дрёма sleeps one person
    /// in front and Стой! freezes one, each by a Will Contest; Чужой сон reads a sleeper's memory into the Character Notes;
    /// Двойник leaves a still copy and hides the character for a tick. Чуткое ухо and Родство are passive and read where
    /// they act. Each use grows the Arcana Skill and Intelligence; spending the last Will Power is full exhaustion.
    /// </summary>
    public sealed partial class SliceConfront
    {
        /// <summary>Внушение waits for the next Persuasion check.</summary>
        public const string SuggestionKey = "player.suggestion";

        /// <summary>05.1: Внушение's bonus to the next Persuasion check, inside the modifiers' cap.</summary>
        public const int SuggestionBonus = 2;

        /// <summary>05.1, authoring default: the ticks a person sleeps by Дрёма.</summary>
        public const int SleepTicks = 10;

        /// <summary>05.1, authoring default: the ticks a person stands frozen by Стой!.</summary>
        public const int HaltTicks = 2;

        /// <summary>Authoring default, m: Чужой сон touches a sleeper within this.</summary>
        public const float TouchReach = 2f;

        /// <summary>05.1 Родство, m: 30 cells of 1 m.</summary>
        public const float KinReach = 30f;

        /// <summary>Authoring default, degrees either side of the camera's look: a person Дрёма or Стой! can be aimed at.</summary>
        public const float AimAngle = 35f;

        /// <summary>Authoring default: Чужой сон's Will Contest starts here, the sleeper's mind keeping its own.</summary>
        public const float DreamContest = 0.5f;

        /// <summary>Authoring default: the Sanity a failed Чужой сон costs, "a little" of 100.</summary>
        public const int DreamSanity = 2;

        private const float EyeHeight = 1.6f;
        private const float ChestHeight = 1.2f;
        private const float AimReach = 80f;

        private static readonly string[] OutsideKeys = { "suggestion", "screen", "drowse", "dream_touch", "double", "halt" };

        private readonly List<(GameObject Body, long Until)> _outsideClouds = new List<(GameObject, long)>();
        private long _doubleUntil = -1;
        private Transform _doubleCopy;
        private int _kinFrame = -1;
        private bool _kinKnown;

        /// <summary>A window, a work or a settling potion keeps the keys from the abilities; set by the world.</summary>
        public Func<bool> OutsideBusy { get; set; }

        /// <summary>For tests: every Will Contest out of a fight at this chance in place of the rules'.</summary>
        public double? ContestChance { get; set; }

        /// <summary>Двойник's veil is on.</summary>
        public bool Doubled => _doubleUntil >= 0;

        /// <summary>The clouds of Пелена raised out of a fight and standing.</summary>
        public int OutsideClouds => _outsideClouds.Count;

        /// <summary>The share of maximum Will Power the character has, 1 for one with none.</summary>
        public float WillPowerShare => _state == null || PlayerArcana == Arcana.None ? 1f : 1f - (Mathf.Clamp(_state.Get(WillPowerLostKey), 0, 1000) / 1000f);

        /// <summary>Whether the ability has a use in free exploration (05.1 *Вне боя*).</summary>
        public static bool HasOutsideUse(FightAbility a) => a != null && !a.Passive && Array.IndexOf(OutsideKeys, a.Key) >= 0;

        /// <summary>The character's abilities usable out of a fight, as the keys 1 to 9 take them.</summary>
        public List<FightAbility> OutsideAbilities() => KnownAbilities().FindAll(HasOutsideUse);

        /// <summary>Whether the character knows the ability of this key now.</summary>
        public bool Knows(string key) => KnownAbilities().Exists(a => a.Key == key);

        /// <summary>The bonus Внушение gives a check of <paramref name="skill"/> now.</summary>
        public int SuggestionFor(CharacterSkill skill) =>
            skill == CharacterSkill.Persuasion && _state != null && _state.IsSet(SuggestionKey) ? SuggestionBonus : 0;

        /// <summary>
        /// A check of <paramref name="skill"/> was rolled with <paramref name="listener"/>: Внушение is spent on it, and a
        /// failure is the attempt noticed, the listener's Attitude a step lower, which makes its next check harder (05.1).
        /// </summary>
        public void Persuaded(CharacterSkill skill, string listener, bool success)
        {
            if (SuggestionFor(skill) == 0)
            {
                return;
            }

            _state.Set(SuggestionKey, 0);
            if (!success && !string.IsNullOrEmpty(listener))
            {
                Standing?.ChangeAttitude(listener, -1);
                Note(Loc.Get("ui.ability.suggestion.noticed"));
            }
        }

        /// <summary>A cloud of Пелена between <paramref name="from"/> and <paramref name="to"/>, or round either end.</summary>
        public bool Screened(Vector3 from, Vector3 to)
        {
            var way = to - from;
            var length = way.magnitude;
            if (length < 1e-4f)
            {
                return false;
            }

            var ray = new Ray(from, way / length);
            foreach (var (body, _) in _outsideClouds)
            {
                if (body != null && body.TryGetComponent<BoxCollider>(out var box) && box.bounds.IntersectRay(ray, out var distance) && distance <= length)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>05.1 Родство: <paramref name="o"/> is a follower of Император within 30 m of a character of Император.</summary>
        public bool SensesKin(StealthObserver o)
        {
            if (o == null || _player == null || o.GetComponentInParent<EmperorFollower>() == null)
            {
                return false;
            }

            if (_kinFrame != Time.frameCount)
            {
                _kinFrame = Time.frameCount;
                _kinKnown = PlayerArcana == Arcana.Emperor && Knows("kinship");
            }

            return _kinKnown && (o.transform.position - _player.transform.position).sqrMagnitude <= KinReach * KinReach;
        }

        /// <summary>
        /// Uses <paramref name="a"/> out of a fight: its target found, its price paid, its effect carried out and said on
        /// the screen. The key of the reason it cannot, with nothing paid, or null.
        /// </summary>
        public string CastOutside(FightAbility a)
        {
            if (!HasOutsideUse(a) || !KnownAbilities().Contains(a) || _settings == null || _state == null || _clock == null)
            {
                return "ui.ability.fault.unknown";
            }

            if (InFight)
            {
                return "ui.bag.fault.fight";
            }

            _player = _player != null ? _player : FindAnyObjectByType<PlayerLocomotion>();
            if (_player == null || _died)
            {
                return "ui.ability.fault.unknown";
            }

            Transform target = null;
            switch (a.Key)
            {
                case "suggestion":
                    if (_state.IsSet(SuggestionKey))
                    {
                        return "ui.ability.fault.on";
                    }

                    break;
                case "double":
                    if (Doubled)
                    {
                        return "ui.ability.fault.on";
                    }

                    break;
                case "drowse":
                case "halt":
                    target = Facing(a.Range);
                    if (target == null)
                    {
                        return "ui.ability.fault.target";
                    }

                    break;
                case "dream_touch":
                    target = Sleeper();
                    if (target == null)
                    {
                        return "ui.ability.fault.sleeper";
                    }

                    break;
            }

            // The world keeps Will Power in thousandths of its maximum (WillPowerLostKey): within one it is paid in full,
            // and what would be left under one is none, so reaching zero is full exhaustion as in a fight.
            var caster = PlayerFighter();
            var cost = _settings.FightRules.AbilityCost(caster, a);
            var slack = caster.MaxWillPower / 1000d;
            if (caster.Arcana == Arcana.None || caster.WillPower + slack < cost)
            {
                return "ui.ability.fault.wp";
            }

            caster.WillPower = caster.WillPower - cost < slack ? 0d : caster.WillPower - cost;
            SaveWillPower(caster);
            var (worked, against) = Carry(a, caster, target);
            var said = DialogueColumn()?.MessageText;
            var stage = against != null ? Growth.StageOfTarget(against.Limit) : PlayerLimit ?? Growth.Stage(1);
            _character?.Train(CharacterSkill.Arcana, stage, worked ? CheckOutcome.Normal : CheckOutcome.Failure, new[] { CharacterAttribute.Intelligence });
            SpentOutside(caster, cost);

            // The outcome stays first on the screen, a level the use reached or exhaustion under it (07.1 §11).
            var now = DialogueColumn()?.MessageText;
            if (!string.IsNullOrEmpty(said) && !string.IsNullOrEmpty(now) && now != said)
            {
                Note(said + "\n" + now);
            }
            Debug.Log($"[Slice] {a.Key} out of a fight: {(worked ? "worked" : "failed")}, Will Power {caster.WillPower:0.#}/{caster.MaxWillPower:0.#}.");
            return null;
        }

        /// <summary>Each frame out of a fight: the keys 1 to 9 use the abilities, and what they left runs out by the ticks.</summary>
        private void Outside(GameInputService input)
        {
            ExpireClouds();
            if (_doubleUntil >= 0 && _clock != null && _clock.Tick >= _doubleUntil)
            {
                EndDouble(true);
            }

            if (input == null || PlayerArcana == Arcana.None || (OutsideBusy != null && OutsideBusy()) || (_clock != null && _clock.IsPaused))
            {
                return;
            }

            var list = OutsideAbilities();
            for (var n = 1; n <= Math.Min(9, list.Count); n++)
            {
                if (input.OptionPressed(n))
                {
                    var fault = CastOutside(list[n - 1]);
                    if (fault != null)
                    {
                        Note(Loc.Get(fault));
                    }

                    return;
                }
            }
        }

        /// <summary>
        /// A fight began: Двойник's veil falls, and those it takes who were held wake and stand where its guard begins,
        /// walking again when it is over. A cloud of Пелена stands on through it for its ticks.
        /// </summary>
        private void OutsideFightBegins()
        {
            EndDouble(false);
            foreach (var body in _peopleInFight)
            {
                var held = HeldStill.Of(body);
                if (held != null && held.Release(true) && body.TryGetComponent<TownsfolkWalker>(out var walker) && !_stopped.Contains(walker))
                {
                    _stopped.Add(walker);
                }
            }
        }

        private (bool Worked, Fighter Against) Carry(FightAbility a, Fighter caster, Transform target)
        {
            switch (a.Key)
            {
                case "suggestion":
                    _state.Raise(SuggestionKey);
                    Note(Loc.Get("ui.ability.suggestion.on"));
                    return (true, null);
                case "screen":
                    RaiseScreen(a);
                    return (true, null);
                case "double":
                    BeginDouble(a);
                    return (true, null);
                case "drowse":
                    return Drowse(a, caster, target);
                case "halt":
                    return Halt(a, caster, target);
                case "dream_touch":
                    return Dream(caster, target);
                default:
                    return (false, null);
            }
        }

        private (bool, Fighter) Drowse(FightAbility a, Fighter caster, Transform body)
        {
            var target = FighterOf(body);
            Puff(body.position);
            if (Wins(caster, target, a.Contest, a.Key, body))
            {
                HeldStill.Hold(body, true, _clock.Tick + SleepTicks);
                Note(Loc.Get("ui.ability.drowse.slept"));
                return (true, target);
            }

            Startle(body); // 05.1: it is on guard, and knows who tried
            Note(Loc.Get("ui.ability.drowse.failed"));
            return (false, target);
        }

        private (bool, Fighter) Halt(FightAbility a, Fighter caster, Transform body)
        {
            var target = FighterOf(body);
            Face(_player.transform, body.position);
            if (Wins(caster, target, a.Contest, a.Key, body))
            {
                HeldStill.Hold(body, false, _clock.Tick + HaltTicks);
                Conduct?.Deed(ConductDeed.Threaten, "halt." + body.name); // 05.1: a Threaten won for Conduct Progress
                Note(Loc.Get("ui.ability.halt.frozen"));
                return (true, target);
            }

            Startle(body);
            Note(Loc.Get("ui.ability.halt.failed"));
            return (false, target);
        }

        private (bool, Fighter) Dream(Fighter caster, Transform body)
        {
            var sleeper = FighterOf(body);
            if (!Wins(caster, sleeper, DreamContest, "dream_touch", body))
            {
                // 05.1: the sleeper wakes, and the character loses a little Sanity.
                HeldStill.Of(body)?.Release();
                Startle(body);
                Mind?.Sanity.Lose(DreamSanity);
                Note(Loc.Get("ui.ability.dream.failed"));
                return (false, sleeper);
            }

            Note(Dreamt(body) ?? Loc.Get("ui.ability.dream.empty"));
            return (true, sleeper);
        }

        /// <summary>
        /// A sleeper's memory into the Character Notes, a biased source like any (09): the first tip it would tell only when
        /// won over that the character has not heard and it could tell now; else its dream's fragment (SLICE_PEOPLE §5), a
        /// named person's own or its kind's, noted the first time. The line to show, or null for none.
        /// </summary>
        private string Dreamt(Transform body)
        {
            var tips = SliceWorldDriver.Running != null ? SliceWorldDriver.Running.Tips : null;
            if (tips == null)
            {
                return null;
            }

            var talk = body.GetComponentInChildren<PersonTalk>();
            if (talk == null)
            {
                talk = body.GetComponentInParent<PersonTalk>();
            }

            foreach (var node in talk != null && talk.Dialogue != null ? talk.Dialogue.Nodes : Array.Empty<DialogueNode>())
            {
                foreach (var option in node.options)
                {
                    foreach (var outcome in new[] { option.success, option.failure, option.won, option.lost })
                    {
                        foreach (var id in outcome != null ? outcome.tells : Array.Empty<string>())
                        {
                            if (tips.Tell(id, _clock.Tick) != null)
                            {
                                return Loc.Get("ui.ability.dream.noted");
                            }
                        }
                    }
                }
            }

            var named = body.GetComponentInParent<NamedPerson>();
            var dream = (named != null ? tips.Find(DreamTip(named.Id)) : null) ?? tips.Find(DreamTip(PersonRole.Of(body), Female(body)));
            if (dream == null)
            {
                return null;
            }

            tips.Tell(dream.id, _clock.Tick);
            return Loc.Format("ui.ability.dream.line", dream.Line);
        }

        /// <summary>SLICE_PEOPLE §5: the tip of a named person's own dream.</summary>
        public static string DreamTip(string personId) => "dream." + personId;

        /// <summary>SLICE_PEOPLE §5: the tip of the dream of a person of no name, by its kind.</summary>
        public static string DreamTip(ResidentRole role, bool female) => role switch
        {
            ResidentRole.Warden => "dream.warden",
            ResidentRole.Dockworker or ResidentRole.OldDockworker or ResidentRole.FactoryWorker => "dream.hand",
            ResidentRole.Vagrant => "dream.drunkard",
            _ => female ? "dream.townswoman" : "dream.townsman",
        };

        private void RaiseScreen(FightAbility a)
        {
            var point = LookedAt(a.Range);
            var width = Mathf.Max(1, a.Area); // the slice's cells are 1 m
            var cloud = Haze.Raise(point, width);
            Particles(cloud.transform, point + (Vector3.up * 1.1f), new Vector3(width, 1.6f, width), HazeColour, HazeLife, 0, HazeRate, 1.4f, 2.6f);
            _outsideClouds.Add((cloud, _clock.Tick + Math.Max(1, a.Rounds)));
            Face(_player.transform, point);
            Note(Loc.Get("ui.ability.screen.raised"));
        }

        private void ExpireClouds()
        {
            var tick = _clock != null ? _clock.Tick : 0;
            for (var i = _outsideClouds.Count - 1; i >= 0; i--)
            {
                var (body, until) = _outsideClouds[i];
                if (body == null || tick >= until)
                {
                    Haze.Clear(body);
                    _outsideClouds.RemoveAt(i);
                }
            }
        }

        private void BeginDouble(FightAbility a)
        {
            var body = _player.transform;
            _doubleCopy = Bake(body);
            Ghost(body, true);
            if (Hiding != null)
            {
                Hiding.Unseen = true;
            }

            Puff(body.position);
            _doubleUntil = _clock.Tick + Math.Max(1, a.Rounds);
            Note(Loc.Get("ui.ability.double.on"));
        }

        /// <summary>Двойник's veil falls: the character is seen again, and its copy scatters.</summary>
        private void EndDouble(bool said)
        {
            if (_doubleUntil < 0)
            {
                return;
            }

            _doubleUntil = -1;
            if (Hiding != null)
            {
                Hiding.Unseen = false;
            }

            if (_player != null)
            {
                Ghost(_player.transform, false);
            }

            if (_doubleCopy != null)
            {
                Puff(_doubleCopy.position);
                DropBaked(_doubleCopy);
                _doubleCopy = null;
            }

            if (said)
            {
                Note(Loc.Get("ui.ability.double.off"));
            }
        }

        /// <summary>A Will Contest out of a fight, rolled from the seed by the ability, the person and the tick.</summary>
        private bool Wins(Fighter caster, Fighter target, double start, string what, Transform body)
        {
            var chance = ContestChance ?? _settings.FightRules.WillContest(caster, target, start);
            var rolls = _seed.Fork(SliceSeedStreams.Abilities, StableHash.Of(what + "." + body.name), (int)_clock.Tick);
            return rolls.NextDouble() < chance;
        }

        /// <summary>The person as a fight would make it: a Warden, a follower of Император, or a resident by its role.</summary>
        private Fighter FighterOf(Transform body) =>
            Armed(body, PeopleAround.IsWarden(body) ? Wardens : body.GetComponentInParent<EmperorFollower>() != null ? Cult : Residents);

        /// <summary>A failed try on <paramref name="body"/>: it is Alert, turns to the character and says so.</summary>
        private void Startle(Transform body)
        {
            var eyes = body.GetComponentInChildren<StealthObserver>(true);
            if (eyes != null)
            {
                if (Hiding != null)
                {
                    eyes.Bind(Hiding.Rules);
                }

                eyes.Meter?.Alarm();
                eyes.Say(Loc.Get("ui.ability.startled"), 3f);
            }

            Face(body, _player.transform.position);
        }

        /// <summary>The person nearest the camera's look within <paramref name="range"/> m that the character sees, awake.</summary>
        private Transform Facing(float range)
        {
            var at = _player.transform.position;
            var camera = Camera.main;
            var look = Vector3.ProjectOnPlane(camera != null ? camera.transform.forward : _player.transform.forward, Vector3.up);
            Transform best = null;
            var bestScore = float.MaxValue;
            foreach (var body in PeopleAround.InSight())
            {
                if (body == null || IsPlayer(body) || Lying(body) || HeldStill.Sleeping(body))
                {
                    continue;
                }

                var to = Vector3.ProjectOnPlane(body.position - at, Vector3.up);
                var distance = to.magnitude;
                var angle = Vector3.Angle(look, to);
                if (distance > range || distance < 0.05f || angle > AimAngle || !InView(body))
                {
                    continue;
                }

                var score = angle + distance;
                if (score < bestScore)
                {
                    best = body;
                    bestScore = score;
                }
            }

            return best;
        }

        /// <summary>The nearest sleeper within the touch's reach.</summary>
        private Transform Sleeper()
        {
            var at = _player.transform.position;
            Transform best = null;
            var bestDistance = TouchReach;
            foreach (var held in HeldStill.All)
            {
                if (held == null || !held.Asleep)
                {
                    continue;
                }

                var distance = Vector3.Distance(held.transform.position, at);
                if (distance <= bestDistance)
                {
                    best = held.transform;
                    bestDistance = distance;
                }
            }

            return best;
        }

        /// <summary>No structure between the character's eyes and the person's chest; people stop no sight.</summary>
        private bool InView(Transform body)
        {
            var from = _player.transform.position + (Vector3.up * EyeHeight);
            var way = body.position + (Vector3.up * ChestHeight) - from;
            foreach (var hit in Physics.RaycastAll(from, way.normalized, way.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!PeopleAround.IsPerson(hit.collider))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>The point the camera looks at within <paramref name="range"/> m of the character, on the ground there.</summary>
        private Vector3 LookedAt(float range)
        {
            var at = _player.transform.position;
            var camera = Camera.main;
            var look = Vector3.ProjectOnPlane(camera != null ? camera.transform.forward : _player.transform.forward, Vector3.up).normalized;
            var point = at + (look * range);
            if (camera != null)
            {
                var hits = Physics.RaycastAll(camera.transform.position, camera.transform.forward, AimReach, ~0, QueryTriggerInteraction.Ignore);
                Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
                foreach (var hit in hits)
                {
                    if (!PeopleAround.IsPerson(hit.collider))
                    {
                        point = hit.point;
                        break;
                    }
                }
            }

            var flat = Vector3.ProjectOnPlane(point - at, Vector3.up);
            if (flat.magnitude > range)
            {
                point = at + (flat.normalized * range) + (Vector3.up * (point.y - at.y));
            }

            return PeopleAround.Ground(point);
        }

        private void SpentOutside(Fighter caster, double cost)
        {
            if (Mind == null || caster.MaxWillPower <= 0d)
            {
                return;
            }

            Mind.Spent(cost, caster.MaxWillPower);
            if (caster.WillPower > 0d)
            {
                return;
            }

            // 07.2 §3.3–3.4: reaching zero through use is full exhaustion.
            var (damage, reward) = Mind.Exhaust(caster.Values.Get(CharacterAttribute.Will), _settings.FightRules.LimitMultiplier(caster.Limit));
            if (reward && _character != null)
            {
                _character.Gain(CharacterAttribute.Will, Growth.ExhaustionWillXp);
            }

            Debug.Log($"[Slice] full exhaustion out of a fight: Sanity −{damage}, Will XP {(reward ? Growth.ExhaustionWillXp : 0)}.");
            Note(Loc.Get("ui.ability.exhausted"));
        }

        private void Puff(Vector3 at) => Particles(null, at + (Vector3.up * 0.9f), new Vector3(0.6f, 1.4f, 0.6f), PuffColour, PuffLife, 16, 0f, 0.5f, 1f);

        /// <summary>A baked copy goes, with the meshes baked for it.</summary>
        private static void DropBaked(Transform shown)
        {
            if (shown == null)
            {
                return;
            }

            foreach (var filter in shown.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh != null && filter.sharedMesh.name.StartsWith("Double ", StringComparison.Ordinal))
                {
                    Destroy(filter.sharedMesh); // the baked meshes are the copy's own
                }
            }

            Destroy(shown.gameObject);
        }
    }
}
