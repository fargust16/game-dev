using System.Collections.Generic;
using GameDev.Confront.Rules;
using GameDev.Core.Localization;
using GameDev.Locomotion;
using GameDev.Player.Standing;
using GameDev.Slice.Beasts;
using GameDev.Slice.People;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// 10.2 §5e, the Aimed Shot out of a fight (owner, 2026-11-12; PB8b). Only a projectile that touches someone is rolled,
    /// by the fight's chance to hit at that distance: a Point-Blank Shot within the next cell, armour counting double
    /// beyond it, higher ground, and a stealth attack from Stealth Mode. A roll that misses is a graze, the Screen Message
    /// «Вскользь», with no harm. The one touched turns toward the shot's source: if it sees the shooter the shot is an
    /// Assault, the fight starting in place with the shot already dealt; if not, it is Alert and searches toward the
    /// source as §9's search does, and finding the shooter later it starts the Assault then. The wound stays on the body
    /// for any later fight. A shot that kills is a kill seen or unseen (§5c), and the body lies where it fell.
    /// </summary>
    public sealed partial class SliceConfront
    {
        /// <summary>Authoring default, m: within this the one touched stands in the next cell, a Point-Blank Shot.</summary>
        public const float BesideReach = 1.5f;

        private static readonly int DeathHash = Animator.StringToHash("Death");

        private readonly Dictionary<Transform, double> _wounds = new Dictionary<Transform, double>();
        private int _shots;
        private int _shotDeaths;

        /// <summary>The last shot's roll out of a fight, for the tests and the log.</summary>
        public StrikeResult LastShot { get; private set; }

        /// <summary>The HP a shot out of a fight took from a person, kept for any later fight; 0 for none.</summary>
        public double Wound(Transform body) => body != null && _wounds.TryGetValue(body, out var lost) ? lost : 0d;

        /// <summary>
        /// The roll of a shot out of a fight at <paramref name="target"/>, who stands at <paramref name="at"/>, from the
        /// character at <paramref name="from"/>: the fight's chance at that distance, its damage taken from the target.
        /// </summary>
        public StrikeResult RollShot(Fighter target, Vector3 from, Vector3 at)
        {
            var rules = _settings.FightRules;
            var shooter = PlayerFighter();
            var flat = new Vector3(at.x - from.x, 0f, at.z - from.z).magnitude;
            var context = new StrikeContext { Beyond = flat > BesideReach, Stealth = Hiding != null && Hiding.Active };
            if (!context.Beyond)
            {
                context.HitBonus -= rules.PointBlank;
            }

            if (from.y - at.y >= rules.HigherGroundRise)
            {
                context.HitBonus += rules.HigherGround;
                context.CriticalBonus += rules.HigherGroundCritical;
            }

            var chance = rules.HitChance(shooter, target, context.HitBonus);
            var result = rules.Strike(shooter, target, context, _seed.Fork(SliceSeedStreams.Shots, ++_shots));
            LastShot = result;
            Debug.Log($"[Confront] a shot out of a fight at {flat:0.#} m: {chance:P0} to hit, {(result.Hit ? "hit" : "missed")}.");
            if (!result.Hit)
            {
                Note(Loc.Get("ui.shot.graze"));
            }

            return result;
        }

        /// <summary>
        /// The roll of a shot out of a fight at a beast, its HP kept on the beast for a later fight; the beast's turn on
        /// the shot is its host's (<c>SliceBeasts.Shot</c>).
        /// </summary>
        public StrikeResult BeastShot(SliceBeast beast, BeastRules rules, Vector3 from)
        {
            var fighter = rules.Fighter(_settings.FightRules, beast.Kind, beast.Glowing);
            if (!double.IsNaN(beast.Hp))
            {
                fighter.Hp = System.Math.Min(beast.Hp, fighter.MaxHp);
            }

            var result = RollShot(fighter, from, beast.transform.position);
            beast.Hp = fighter.Hp;
            Debug.Log($"[Confront] a shot touched the {beast.Kind}: {(result.Hit ? $"hit for {result.Damage:0.#}" : "grazed")}, {fighter.Hp:0.#} of {fighter.MaxHp:0.#} HP left.");
            return result;
        }

        /// <summary>A shot out of a fight touched the person <paramref name="body"/>; the character fired it from <paramref name="from"/>.</summary>
        public void PersonShot(Transform body, Vector3 from)
        {
            if (body == null || InFight || _died || Lying(body))
            {
                return;
            }

            var group = PeopleAround.IsWarden(body) ? Wardens : Residents;
            var fighter = Armed(body, group);
            var result = RollShot(fighter, from, body.position);
            if (result.Hit)
            {
                _wounds[body] = fighter.MaxHp - fighter.Hp;
            }

            Debug.Log($"[Confront] a shot touched {body.name}: {(result.Hit ? $"hit for {result.Damage:0.#}{(result.Critical ? ", critical" : string.Empty)}" : "grazed")}, {fighter.Hp:0.#} of {fighter.MaxHp:0.#} HP left.");

            // Hit or grazed, the one touched turns toward the shot's source.
            var way = Vector3.ProjectOnPlane(from - body.position, Vector3.up);
            if (way.sqrMagnitude > 1e-4f)
            {
                body.rotation = Quaternion.LookRotation(way.normalized, Vector3.up);
            }

            if (fighter.Down)
            {
                ShotDead(body);
                return;
            }

            if (SeesTheShooter(body))
            {
                StartAssault(body, fighter, opened: true);
                return;
            }

            SearchForTheShooter(body, from, fighter, group);
        }

        /// <summary>
        /// 10.3 through the eyes of one just shot, turned toward the shot: out of Stealth Mode the character in its cone
        /// and in sight is seen; in Stealth Mode its Notice Meter has not filled yet, and it searches.
        /// </summary>
        private bool SeesTheShooter(Transform body)
        {
            _player = _player != null ? _player : FindAnyObjectByType<PlayerLocomotion>();
            var eyes = body.GetComponentInChildren<GameDev.Slice.Stealth.StealthObserver>();
            return _player != null && Hiding != null && eyes != null && !Hiding.Active && Hiding.Sees(eyes, _player.transform.position, false);
        }

        /// <summary>The one shot did not see the shooter: Alert, it runs to the shot's source and looks round there (10.2 §9).</summary>
        private void SearchForTheShooter(Transform body, Vector3 from, Fighter fighter, string group)
        {
            var old = body.GetComponent<FightSearch>();
            if (old != null)
            {
                _searching.Remove(old);
                old.End(walkAgain: false);
            }

            var errand = body.GetComponent<WardenErrand>();
            var walker = body.GetComponent<TownsfolkWalker>();
            if (walker != null && walker.enabled)
            {
                walker.enabled = false;
            }

            var search = FightSearch.Begin(body, from, body.position, body.rotation, fighter, group, group == Wardens ? "ui.fight.warden" : "ui.fight.resident",
                false, _settings.StealthRules, _clock);
            search.Shot = true;
            _searching.Add(search);
            if (errand != null)
            {
                Destroy(errand);
            }
        }

        /// <summary>A searcher the character shot sees it: the Assault begins with its wound, no blow struck again.</summary>
        private bool ShotSearcherSees(FightSearch s)
        {
            var body = s.transform;
            _searching.Remove(s);
            s.End(walkAgain: false);
            if (StartAssault(body, s.Fighter, opened: true))
            {
                return true;
            }

            // No fight can start here: it walks its round again, its wound kept.
            if (body.TryGetComponent<TownsfolkWalker>(out var walker))
            {
                walker.enabled = true;
            }

            return false;
        }

        /// <summary>
        /// A shot out of a fight killed the person <paramref name="body"/>: a kill known even unseen, as the goddess tells
        /// the Wardens (10.2 §5c), and a body that lies where it fell until a Warden takes it up or its day is over.
        /// </summary>
        private void ShotDead(Transform body)
        {
            _wounds.Remove(body);
            Standing?.RecordKill();
            Standing?.Record(Deed.InnocentKilled);
            if (body.TryGetComponent<TownsfolkWalker>(out var walker))
            {
                walker.enabled = false;
            }

            var animator = body.GetComponentInChildren<Animator>();
            if (animator != null && animator.isActiveAndEnabled)
            {
                animator.SetFloat(DeathHash, 1 + (_shotDeaths++ % FightBeats.Deaths.Length));
                animator.SetBool(DownHash, true);
            }

            foreach (var part in body.GetComponents<Collider>())
            {
                part.enabled = false; // a body lies on the ground and stops nobody
            }

            if (!_fallen.Exists(f => f.Body == body.gameObject))
            {
                _fallen.Add(new Fallen { Body = body.gameObject, Tick = _clock != null ? _clock.Tick : 0 });
            }

            Debug.Log($"[Confront] a shot killed {body.name}.");
        }

        /// <summary>A person's fighter with the wound a shot left on it out of a fight.</summary>
        private Fighter Wounded(Transform body, Fighter fighter)
        {
            if (body != null && _wounds.TryGetValue(body, out var lost))
            {
                fighter.Hp = System.Math.Max(fighter.MaxHp - lost, 1d);
            }

            return fighter;
        }
    }
}
