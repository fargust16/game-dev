using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Locomotion;
using GameDev.Player.Standing;
using GameDev.Slice.Alchemy;
using GameDev.Slice.BreakIns;
using GameDev.Slice.People;
using GameDev.Slice.Stealth;
using GameDev.UI;
using GameDev.World.Seed;
using UnityEngine;

namespace GameDev.Slice.Dwellings
{
    /// <summary>
    /// Who is at home in a dwelling (S5c.5; 10.3 §6.1, 18, VS-BREAKIN): a household of one or two by the seed, a rich
    /// house's servants, and the named person whose home it is (VS-HOURS (f)). By night everyone sleeps in a bed; in the
    /// morning and the evening they stand awake in the rooms; by day each is home one day in four, a servant all day.
    /// Offices, warehouses and Veinmarket's den have none. A resident who sees the character inside notices it at once,
    /// or by the meter in Stealth Mode, a sleeper's at half Perception: it cries out, runs out of the house to the
    /// Wardens, and the household stays out until morning. Going through unnoticed a home where someone was, or the den
    /// while shut, is Жрица's deed of a watched or forbidden place (S5b.7).
    /// </summary>
    public sealed class DwellingResidents : MonoBehaviour
    {
        /// <summary>Authoring default: a resident is at home by day one day in four.</summary>
        public const double HomeByDayChance = 0.25;

        /// <summary>Authoring default: a household of one to this many, besides the servants.</summary>
        public const int HouseholdMost = 2;

        /// <summary>Authoring defaults, hours: asleep from 22:00 to 06:00, at home awake until 08:00 and from 18:00.</summary>
        public const int SleepHour = 22, WakeHour = 6, AwayHour = 8, BackHour = 18;

        /// <summary>Authoring default, s: a resident who noticed the character is out of the house so long after its cry.</summary>
        public const float RunOutSeconds = 2.5f;

        /// <summary>10.3 §3 and S3b.1: a resident watches with Perception 2, a Warden with 4.</summary>
        public const int ResidentPerception = 2, WardenPerception = 4;

        private const int Lines = 3;
        private const float MattressHeight = 0.55f; // authoring default, m: a bed's top above its floor

        /// <summary>One person at home now: who, a servant or not, asleep or not.</summary>
        public readonly struct Member
        {
            public Member(string person, bool servant, bool asleep)
            {
                Person = person;
                Servant = servant;
                Asleep = asleep;
            }

            /// <summary>The named person's roster id, or null for one of no name.</summary>
            public string Person { get; }

            public bool Servant { get; }

            public bool Asleep { get; }
        }

        private sealed class Resident
        {
            public Transform Body;
            public StealthObserver Eyes;
            public float GoneAt = -1f;
            public int Index;
        }

        private readonly List<Resident> _people = new List<Resident>();
        private DwellingKey _key;
        private SliceWorldDriver _world;
        private PlayerLocomotion _player;
        private bool _watched;
        private bool _noticed;
        private bool _hooked;

        /// <summary>A dwelling people live in: no office, warehouse or the den.</summary>
        public static bool IsHome(DwellingKey key) =>
            key.Archetype != DwellingArchetype.Office && key.Archetype != DwellingArchetype.Warehouse && key.DwellingId != SliceAlchemy.DenDwelling;

        /// <summary>World state: the day, plus one, the household ran out to the Wardens; it stays out until the next day.</summary>
        public static string FledKey(string place) => $"breakin.{place}.fled";

        /// <summary>The dwelling's people standing in it now.</summary>
        public IEnumerable<Transform> People => _people.Where(p => p.Body != null && p.Body.gameObject.activeSelf).Select(p => p.Body);

        /// <summary>Whether someone at home or the shut den made this a watched or forbidden place when the character came in.</summary>
        public bool Watched => _watched;

        /// <summary>Whether a resident noticed the character on this visit.</summary>
        public bool NoticedHere => _noticed;

        /// <summary>
        /// Who is at home in the dwelling at <paramref name="minute"/> of <paramref name="day"/>: pure, so the same key,
        /// seed and hour always give the same people (VS-BREAKIN). <paramref name="gone"/> says a named person has fallen.
        /// </summary>
        public static List<Member> Household(DwellingKey key, int servants, string named, int day, int minute, IWorldSeed seed, Func<string, bool> gone = null)
        {
            var home = new List<Member>();
            if (!IsHome(key) || seed == null)
            {
                return home;
            }

            var hour = minute / 60;
            var night = hour >= SleepHour || hour < WakeHour;
            var size = 1 + seed.Fork(SliceSeedStreams.Residents, key.DoorX, key.DoorZ).Next(HouseholdMost);
            for (var i = 0; i < size; i++)
            {
                if (i == 0 && named != null)
                {
                    if ((gone == null || !gone(named)) && SliceHours.PostAt(named, minute) == null)
                    {
                        home.Add(new Member(named, false, SliceHours.Asleep(named, minute)));
                    }

                    continue;
                }

                var morningOrEvening = (hour >= WakeHour && hour < AwayHour) || (hour >= BackHour && hour < SleepHour);
                var byDay = !night && !morningOrEvening
                            && seed.Fork(SliceSeedStreams.Residents, key.DoorX ^ ((day * 16) + i), key.DoorZ).NextDouble() < HomeByDayChance;
                if (night || morningOrEvening || byDay)
                {
                    home.Add(new Member(null, false, night));
                }
            }

            for (var s = 0; s < servants; s++)
            {
                home.Add(new Member(null, true, night)); // 10.3 §6.1: home 06:00–22:00, asleep in the servant's room at night
            }

            return home;
        }

        /// <summary>Puts the dwelling's people at home now in its rooms and beds, and watches over the visit.</summary>
        public static DwellingResidents Populate(DwellingKey key, DwellingPlan plan, Transform built, SliceSettings settings)
        {
            var watch = built.gameObject.AddComponent<DwellingResidents>();
            watch._key = key;
            watch._world = SliceWorldDriver.Running;
            var world = watch._world;
            if (world == null || settings == null)
            {
                return watch;
            }

            var place = key.DwellingId;
            watch._watched = key.DwellingId == SliceAlchemy.DenDwelling && !DwellingLocks.DenOpen();
            if (!IsHome(key) || world.State.Get(FledKey(place)) == world.Clock.Day + 1)
            {
                return watch;
            }

            var named = SliceHomes.PersonAt(place, world.Seed);
            var members = Household(key, plan.Servants, named, world.Clock.Day, world.Clock.MinuteOfDay, world.Seed,
                p => world.State.IsSet(PersonHours.GoneKey(p)));
            var beds = Beds(built, plan);
            var spots = Spots(built, plan, world.Seed.Fork(SliceSeedStreams.Residents, key.DoorX, key.DoorZ ^ 0x5a5a));
            var looks = world.Seed.Fork(SliceSeedStreams.Residents, key.DoorZ, key.DoorX);
            for (var i = 0; i < members.Count; i++)
            {
                var m = members[i];
                var prefab = m.Person != null ? settings.NamedLookOf(m.Person)
                    : Pick(m.Servant ? settings.Servants : settings.Residents((int)key.Wealth), looks);
                Pose? pose = m.Asleep ? Bed(beds, m.Servant) : spots.Count > 0 ? spots[i % spots.Count] : (Pose?)null;
                if (prefab == null || pose == null)
                {
                    continue; // no model, or no bed left: it sleeps where the slice's rooms do not show
                }

                var body = Instantiate(prefab, pose.Value.position, pose.Value.rotation, built).transform;
                var entry = m.Person != null ? SliceRoster.All.FirstOrDefault(e => e.Id == m.Person) : null;
                body.name = entry != null ? entry.ObjectName : prefab.name;
                var eyes = body.gameObject.AddComponent<StealthObserver>();
                eyes.Perception = m.Person == "coyle" ? WardenPerception : ResidentPerception;
                if (world.Hiding != null)
                {
                    eyes.Bind(world.Hiding.Rules);
                }

                if (entry != null)
                {
                    body.gameObject.AddComponent<NamedPerson>().Configure(m.Person); // the market trader has no name of its own
                }

                if (m.Asleep)
                {
                    var capsule = body.GetComponent<CapsuleCollider>();
                    if (capsule != null)
                    {
                        capsule.isTrigger = true; // lying in a bed, it stops nobody
                    }

                    Slumber.Lay(body);
                }

                watch._people.Add(new Resident { Body = body, Eyes = eyes, Index = i });
            }

            watch._watched |= watch._people.Count > 0;
            FindAnyObjectByType<HudView>()?.FindPromptSources();
            Debug.Log($"[Dwelling] {place}: {watch._people.Count} at home{(named != null ? ", " + named + "'s home" : string.Empty)}.");
            return watch;
        }

        private static GameObject Pick(GameObject[] prefabs, System.Random rng)
        {
            var some = prefabs != null ? prefabs.Where(p => p != null).ToList() : new List<GameObject>();
            return some.Count > 0 ? some[rng.Next(some.Count)] : null;
        }

        // Each bed's pose: on its top, along its length; the servant's room's first.
        private static List<(Pose Pose, bool Servants)> Beds(Transform built, DwellingPlan plan)
        {
            var beds = new List<(Pose, bool)>();
            foreach (var t in built.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("Kit_ViFurniture_Bed", StringComparison.Ordinal))
                {
                    continue;
                }

                var renderers = t.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0)
                {
                    continue;
                }

                var bounds = renderers[0].bounds;
                foreach (var r in renderers)
                {
                    bounds.Encapsulate(r.bounds);
                }

                var along = bounds.size.x > bounds.size.z ? Vector3.right : Vector3.forward;
                var at = new Vector3(bounds.center.x, bounds.min.y + MattressHeight, bounds.center.z);
                var local = built.InverseTransformPoint(at);
                var servants = plan.Rooms.Any(r => r.Kind == RoomKind.ServantRoom && r.Contains(local.x, local.z)
                                                   && Mathf.Abs(local.y - (r.Level * DwellingPlan.Storey)) < 1f);
                beds.Add((new Pose(at, Quaternion.LookRotation(along, Vector3.up)), servants));
            }

            return beds;
        }

        private static Pose? Bed(List<(Pose Pose, bool Servants)> beds, bool servant)
        {
            var i = beds.FindIndex(b => b.Servants == servant);
            i = i >= 0 ? i : beds.Count > 0 ? 0 : -1;
            if (i < 0)
            {
                return null;
            }

            var pose = beds[i].Pose;
            beds.RemoveAt(i);
            return pose;
        }

        // Free places to stand on the rooms' floors, a module cell's centre clear of the furniture, in the seed's order.
        private static List<Pose> Spots(Transform built, DwellingPlan plan, System.Random rng)
        {
            var spots = new List<Pose>();
            foreach (var r in plan.Rooms)
            {
                for (var cx = r.X; cx < r.X + r.Width; cx += DwellingPlan.Module)
                {
                    for (var cz = r.Z; cz < r.Z + r.Depth; cz += DwellingPlan.Module)
                    {
                        if (plan.InWell(cx, cz, r.Level))
                        {
                            continue;
                        }

                        var local = new Vector3(cx + (DwellingPlan.Module * 0.5f), r.Level * DwellingPlan.Storey, cz + (DwellingPlan.Module * 0.5f));
                        var at = built.TransformPoint(local);
                        if (Physics.CheckCapsule(at + (Vector3.up * 0.45f), at + (Vector3.up * 1.5f), 0.35f, ~0, QueryTriggerInteraction.Ignore))
                        {
                            continue;
                        }

                        var centre = built.TransformPoint(new Vector3(r.X + (r.Width * 0.5f), local.y, r.Z + (r.Depth * 0.5f)));
                        var look = Vector3.ProjectOnPlane(centre - at, Vector3.up);
                        var facing = look.sqrMagnitude > 0.01f ? Quaternion.LookRotation(look) : Quaternion.Euler(0f, rng.Next(4) * 90f, 0f);
                        spots.Add(new Pose(at, facing));
                    }
                }
            }

            // The seed's order: Fisher–Yates.
            for (var i = spots.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (spots[i], spots[j]) = (spots[j], spots[i]);
            }

            return spots;
        }

        private void OnEnable()
        {
            if (_world != null && _world.Hiding != null && !_hooked)
            {
                _world.Hiding.Noticed += OnNoticed;
                _hooked = true;
            }
        }

        private void Start() => OnEnable();

        private void OnDisable()
        {
            if (_hooked && _world != null && _world.Hiding != null)
            {
                _world.Hiding.Noticed -= OnNoticed;
            }

            _hooked = false;
        }

        // Leaving: a watched or forbidden place gone through unnoticed (S5b.7). The scene's unload destroys the dwelling.
        private void OnDestroy()
        {
            if (_watched && !_noticed && _world != null && _world.Conduct != null && Application.isPlaying)
            {
                _world.Conduct.Deed(ConductDeed.Unnoticed, "dwelling." + _key.DwellingId);
            }
        }

        private void OnNoticed(StealthObserver o)
        {
            var resident = _people.Find(p => p.Eyes == o);
            if (resident != null && !_noticed)
            {
                Notice(resident);
            }
        }

        private void Update()
        {
            foreach (var p in _people)
            {
                if (p.GoneAt > 0f && Time.time >= p.GoneAt && p.Body != null)
                {
                    p.Body.gameObject.SetActive(false); // out of the house, to the Wardens
                    p.GoneAt = -1f;
                }
            }

            var hiding = _world != null ? _world.Hiding : null;
            if (_noticed || hiding == null || hiding.Active || _world.Clock.IsPaused || (_world.Fights != null && _world.Fights.InFight))
            {
                return; // in Stealth Mode the meter does it (OnNoticed)
            }

            _player = _player != null ? _player : FindAnyObjectByType<PlayerLocomotion>();
            if (_player == null)
            {
                return;
            }

            // Out of Stealth Mode a resident who sees the character notices it at once, asleep or awake (VS-BREAKIN).
            var seeing = CrimeWatch.Seeing(_world, _player.transform.position, _people.Where(p => p.Body != null && p.Body.gameObject.activeSelf).Select(p => p.Eyes));
            if (seeing.Count > 0)
            {
                Notice(_people.Find(p => p.Eyes == seeing[0]));
            }
        }

        private void Notice(Resident r)
        {
            if (r == null || r.Body == null)
            {
                return;
            }

            _noticed = true;
            Slumber.Of(r.Body)?.Wake();
            var feet = _player != null ? _player.transform.position : r.Body.position;
            var line = $"ui.breakin.resident.{1 + (r.Index % Lines)}";
            CrimeWatch.Noticed(_world, r.Eyes, feet, _key.DwellingId, line, Deed.BreakInNoticed, "ui.breakin.noticed.inside");
            _world.BreakIns.ReportLater(_key.DwellingId, _key.DoorX, _key.DoorZ);
            _world.State.Set(FledKey(_key.DwellingId), _world.Clock.Day + 1);
            var door = FindStreetDoor();
            if (door != null)
            {
                var way = Vector3.ProjectOnPlane(door.position - r.Body.position, Vector3.up);
                if (way.sqrMagnitude > 0.01f)
                {
                    r.Body.rotation = Quaternion.LookRotation(way);
                }
            }

            r.GoneAt = Time.time + RunOutSeconds;
        }

        private Transform FindStreetDoor()
        {
            foreach (var t in GetComponentsInChildren<Transform>())
            {
                if (t.name == "Street door")
                {
                    return t;
                }
            }

            return null;
        }

        /// <summary>A Stash taken on this visit: unseen when someone was home and nobody noticed (S5b.7, Жрица's deed).</summary>
        public void Took()
        {
            if (_watched && !_noticed && _world != null)
            {
                _world.Conduct?.Deed(ConductDeed.TakeUnseen, "stash." + _key.DwellingId);
            }
        }

        /// <summary>The line the household's eyes say: for tests.</summary>
        public static string LineKey(int index) => $"ui.breakin.resident.{1 + (index % Lines)}";
    }
}
