using System.Collections.Generic;
using System.Linq;
using GameDev.Confront.Rules;
using GameDev.Confront.Turns;
using GameDev.Locomotion;
using GameDev.Slice.News;
using GameDev.Slice.People;
using GameDev.Slice.Stealth;
using GameDev.World.Portals;
using UnityEngine;
using Random = System.Random;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// 10.2 §5e, deaths, the god and the news (owner, 2026-11-12, VS-ALARM (a); PB8d). Every person who dies, by a blow
    /// or a shot in a fight or by the Aimed Shot out of one, enters Veilmouth's News Pool 1–2 game hours later, with the
    /// place and who killed, if a person saw it (<see cref="SliceNews"/>). When two or more die within a game hour, each
    /// death that makes it so is noticed by the goddess one time in two, by the seed, and the church's people initiated
    /// into Жрица who carry a firearm come to the place if the old chapel, where they stand, is within 100 cells of it:
    /// they come out of its door and run as Wardens do, join a fight under way by the Wardens' rule, or look round 3
    /// ticks when none is, and go back in.
    /// </summary>
    public sealed partial class SliceConfront
    {
        /// <summary>The old chapel's door in the street (SliceActivities.ChapelStreetPortalId), where the church's people stand behind.</summary>
        public const string ChurchDoorId = "old-chapel.street";

        /// <summary>10.2 §5e, authoring defaults: two deaths within this many ticks, a game hour, may be noticed, one in two.</summary>
        public const int GodHourTicks = 60;

        public const int GodNoticesOneIn = 2;

        /// <summary>10.2 §5e, authoring default: the church's people come from within this many cells of the place.</summary>
        public const int ChurchReachCells = 100;

        private readonly List<long> _deathTicks = new List<long>();
        private readonly HashSet<Combatant> _dead = new HashSet<Combatant>();
        private readonly List<Transform> _church = new List<Transform>();

        /// <summary>Veilmouth's News Pool, which every death enters.</summary>
        public SliceNews News { get; set; }

        /// <summary>The persons who have died this run, and how many times the goddess noticed.</summary>
        public int Deaths { get; private set; }

        public int GodNoticed { get; private set; }

        /// <summary>The church's people out of the old chapel now.</summary>
        public IReadOnlyList<Transform> ChurchOut => _church;

        /// <summary>
        /// A person died at <paramref name="at"/> at this tick: its death enters the news, and when it makes two or more
        /// within the hour the goddess may notice.
        /// </summary>
        public void PersonDied(Vector3 at, NewsVictim victim, NewsKiller killer)
        {
            var tick = _clock != null ? _clock.Tick : 0L;
            Deaths++;
            News?.AddDeath(tick, at, victim, killer);
            _deathTicks.RemoveAll(t => tick - t >= GodHourTicks);
            _deathTicks.Add(tick);
            if (_deathTicks.Count < 2)
            {
                return;
            }

            var rolls = _seed != null ? _seed.Fork(SliceSeedStreams.GodNotices, Deaths) : new Random(Deaths);
            if (rolls.Next(GodNoticesOneIn) != 0)
            {
                Debug.Log($"[Confront] {_deathTicks.Count} deaths within the hour; the goddess does not notice this one.");
                return;
            }

            GodNoticed++;
            Debug.Log($"[Confront] {_deathTicks.Count} deaths within the hour: the goddess notices, and {SendTheChurch(at)} of the church's people come.");
        }

        /// <summary>The church's initiated who carry a firearm come out of the old chapel's door to <paramref name="at"/>; how many.</summary>
        private int SendTheChurch(Vector3 at)
        {
            var door = ChurchDoor();
            if (door == null || _settings == null || _settings.ChurchPeople == null
                || TownsfolkWalker.Cells(at, door.position) > ChurchReachCells)
            {
                return 0;
            }

            _church.RemoveAll(b => b == null);
            _player = _player != null ? _player : FindAnyObjectByType<PlayerLocomotion>(); // they go back in by the character's distance
            var came = 0;
            foreach (var prefab in _settings.ChurchPeople)
            {
                var part = prefab != null ? prefab.GetComponent<PersonRole>() : null;
                if (part == null || _church.Exists(b => b.name == prefab.name))
                {
                    continue;
                }

                var made = ResidentRoles.Make(part.Role, prefab.name, _seed);
                if (made.Limit == null || made.Firearm == RoleFirearm.None)
                {
                    continue; // only the initiated who carry a firearm (10.2 §5e)
                }

                var outside = Ground(door.position + (door.forward * 1.5f));
                var body = Instantiate(prefab, outside, door.rotation).transform;
                body.name = prefab.name;
                _church.Add(body);
                Summon(body, at);
                came++;
            }

            return came;
        }

        /// <summary>One sent by the goddess runs to <paramref name="at"/> as a Warden does: into a fight the Wardens join, or to look round.</summary>
        private void Summon(Transform body, Vector3 at)
        {
            var side = !InFight ? -1 : Assaulting ? HostileSide : WardensJoinTheCharacter() ? PlayerSide : -1;
            if (side >= 0)
            {
                Come(body, at, side, Residents, "ui.fight.god.came");
                return;
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
            Say(body, "ui.fight.god.came");
        }

        /// <summary>One of the church's people back at the door goes in again.</summary>
        private bool GoesBackIn(Transform body)
        {
            if (body == null || !_church.Remove(body))
            {
                return false;
            }

            Destroy(body.gameObject);
            return true;
        }

        private static Transform ChurchDoor()
        {
            foreach (var portal in FindObjectsByType<Portal>(FindObjectsInactive.Exclude))
            {
                if (portal.Id == ChurchDoorId)
                {
                    return portal.transform;
                }
            }

            return null;
        }

        /// <summary>
        /// A fighter brought down by a blow or a shot in a fight: a death, once, unless it is the character, a beast, or one
        /// a Warden's baton subdued, who is taken away alive (10.2 §5f).
        /// </summary>
        private void FightDeath(Combatant attacker, Combatant target)
        {
            if (target == null || target == _playerCombatant || target.Beast || !target.Fighter.Down || target.Subdued || !_dead.Add(target)
                || !_bodies.TryGetValue(target, out var body) || body == null)
            {
                return;
            }

            // Seen when a person still stands in the fight besides the one who struck: a beast tells nobody.
            var seen = false;
            foreach (var c in _bodies.Keys)
            {
                if (c != attacker && c != target && c != _playerCombatant && !c.Beast && c.Standing)
                {
                    seen = true;
                    break;
                }
            }

            PersonDied(body.position, VictimOf(body, Group(target)), seen ? KillerOf(attacker) : NewsKiller.Unseen);
            Recovery?.Leave(body, GearOf(target.Fighter));
        }

        /// <summary>19 §6 (S5c.7): a fallen person's equipment, recovered piece by piece when its body is searched; set by the world.</summary>
        public SliceRecovery Recovery { get; set; }

        /// <summary>The items of a fighter's equipment, both sets, by id: those the catalogue makes, so neither fists nor the baton.</summary>
        public List<string> GearOf(Fighter fighter)
        {
            var ids = new List<string>();
            if (fighter == null || _settings == null || _settings.Items == null)
            {
                return ids;
            }

            foreach (var set in new[] { fighter.Set, fighter.Second })
            {
                foreach (var part in new ScriptableObject[] { set.Weapon, set.OffHand, set.OffWeapon })
                {
                    var item = part != null ? _settings.Items.Items.FirstOrDefault(i => i != null && i.fight == part) : null;
                    if (item != null)
                    {
                        ids.Add(item.id);
                    }
                }
            }

            return ids;
        }

        /// <summary>The Aimed Shot killed a person: seen if anyone else in sight sees the character, out of Stealth Mode.</summary>
        private void ShotDeath(Transform body)
        {
            var seen = false;
            if (_player != null && Hiding != null && !Hiding.Active)
            {
                foreach (var other in PeopleAround.InSight())
                {
                    var eyes = other != body && !IsPlayer(other) && !Lying(other) ? other.GetComponentInChildren<StealthObserver>() : null;
                    if (eyes != null && eyes.isActiveAndEnabled && Hiding.Sees(eyes, _player.transform.position, false))
                    {
                        seen = true;
                        break;
                    }
                }
            }

            var group = PeopleAround.IsWarden(body) ? Wardens : Residents;
            PersonDied(body.position, VictimOf(body, group), seen ? NewsKiller.Character : NewsKiller.Unseen);
            Recovery?.Leave(body, GearOf(Armed(body, group))); // what it would have fought with (19 §6, S5c.7)
        }

        private NewsKiller KillerOf(Combatant attacker)
        {
            if (attacker == null)
            {
                return NewsKiller.Unseen;
            }

            if (attacker == _playerCombatant)
            {
                return NewsKiller.Character;
            }

            if (attacker.Beast)
            {
                return NewsKiller.Beast;
            }

            var group = Group(attacker);
            return group == Wardens ? NewsKiller.Warden : group == Residents ? NewsKiller.Resident : NewsKiller.Outsider;
        }

        private static NewsVictim VictimOf(Transform body, string group)
        {
            if (group == Wardens || PeopleAround.IsWarden(body))
            {
                return NewsVictim.Warden;
            }

            if (group != Residents)
            {
                return NewsVictim.Stranger;
            }

            var role = PersonRole.Of(body);
            if (role == ResidentRole.OldPriest || role == ResidentRole.Acolyte || role == ResidentRole.ChurchMaid)
            {
                return NewsVictim.Church;
            }

            return Female(body) ? NewsVictim.Townswoman : NewsVictim.Townsman;
        }

        /// <summary>A woman: a named person by the roster, anyone else by its model's name (a lady, a working woman, a maid).</summary>
        private static bool Female(Transform body)
        {
            var named = body.GetComponentInParent<NamedPerson>();
            if (named != null)
            {
                foreach (var entry in SliceRoster.All)
                {
                    if (entry.Id == named.Id)
                    {
                        return entry.Female;
                    }
                }
            }

            var name = body.name.ToLowerInvariant();
            return name.Contains("woman") || name.Contains("lady") || name.Contains("maid") || name.Contains("girl");
        }
    }
}
