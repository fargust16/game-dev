using System.Linq;
using GameDev.Core.Localization;
using GameDev.Core.Scenes;
using GameDev.Input;
using GameDev.Items;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Slice.People;
using GameDev.Slice.Stealth;
using GameDev.UI;
using GameDev.World.Portals;
using GameDev.World.Seed;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDev.Slice.Tasks
{
    /// <summary>
    /// A place of SLICE_TRIAL §3 in the city (S5c.7): where a task's item is picked up or handed in, E within reach while
    /// the task wants it; the cemetery's angel, by whose plinth the widow's locket lies, found by one Perception check at
    /// stage 9 within 3 m, rolled again only after 10 m away as a Hidden Cache's; and the Reek's three warded doors, each
    /// counted when walked past at night unseen by any Warden or Veinmarket man, its chalk mark said on the screen.
    /// </summary>
    public sealed class TaskSpot : MonoBehaviour, IPromptSource
    {
        public const string RootName = "Task places";

        /// <summary>Authoring defaults, m: the reach of E, the reach of the locket's check and of a warded door, and the walk away that re-arms the check.</summary>
        public const float Reach = 2.5f;

        public const float NoticeReach = 3f;
        public const float DoorReach = 3f;
        public const float ReturnDistance = 10f;

        private const float SameLevel = 1.5f;
        private const float EyeHeight = 1.6f;

        /// <summary>
        /// The places, world m, at measured spots by what the table names (CacheSpots' measurements, 2026-11-13), authoring
        /// defaults the owner may move: the porter's door by the Brothers' bell tower, the angel's plinth, a villa's stoop on
        /// Widows' Row, the Customs House's door, a farm at Crooked Hamlet, a warehouse on Fishlip Quay, and three walls of the
        /// Reek: on Rats' Choir, in Deaf Boiler Yard and on Furnace Road.
        /// </summary>
        public static readonly (string Id, float X, float Y, float Z)[] Spots =
        {
            ("brothers", 134.5f, 26.2f, 189.1f),
            ("angel", -48.0f, 26.3f, 161.25f),
            ("widow", -49.9f, 26.5f, 117.0f),
            ("customs", 148.6f, 26.1f, 96.6f),
            ("farmer", -200.0f, 26.3f, 68.5f),
            ("warehouse", 196.5f, 26.0f, 54.5f),
            ("reek.1", -23.5f, 26.05f, -161.4f),
            ("reek.2", 150.0f, 25.9f, -160.6f),
            ("reek.3", 46.3f, 26.05f, -105.0f),
        };

        private PlayerLocomotion _player;
        private HudView _hud;
        private bool _armed = true;
        private bool _atDoor;

        public string Id { get; private set; }

        /// <summary>The warded door's number, 1–3, or 0.</summary>
        public int Door => Id != null && Id.StartsWith("reek.", System.StringComparison.Ordinal) ? int.Parse(Id.Substring(5)) : 0;

        private static SliceTasks Tasks => SliceWorldDriver.Running != null ? SliceWorldDriver.Running.Tasks : null;

        /// <summary>Puts the task places into the city's scene, under one root; true when placed.</summary>
        public static bool Place(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || scene.GetRootGameObjects().Any(r => r.name == RootName))
            {
                return false;
            }

            var root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            foreach (var (id, x, y, z) in Spots)
            {
                var spot = new GameObject($"Task place {id}").AddComponent<TaskSpot>();
                spot.transform.SetParent(root.transform, false);
                spot.Id = id;
                spot.transform.position = Ground(new Vector3(x, y, z));
            }

            Object.FindAnyObjectByType<HudView>()?.FindPromptSources();
            return true;
        }

        /// <summary>The highest ground under a spot, within 1.2 m above and 1.8 m below it.</summary>
        public static Vector3 Ground(Vector3 at)
        {
            var best = at;
            var top = float.MinValue;
            foreach (var hit in Physics.RaycastAll(at + (Vector3.up * 1.2f), Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<Animator>() == null && hit.collider.GetComponentInParent<PlayerLocomotion>() == null && hit.point.y > top)
                {
                    top = hit.point.y;
                    best = hit.point;
                }
            }

            return best;
        }

        public bool InReach(Vector3 position, float reach) =>
            Mathf.Abs(position.y - transform.position.y) <= SameLevel
            && new Vector2(position.x - transform.position.x, position.z - transform.position.z).sqrMagnitude <= reach * reach;

        public string PromptFor(Vector3 player)
        {
            var tasks = Tasks;
            var world = SliceWorldDriver.Running;
            if (tasks == null || !InReach(player, Reach) || (world.Fights != null && world.Fights.InFight))
            {
                return null;
            }

            var give = tasks.HandInAt(Id);
            if (give != null)
            {
                return Loc.Get($"ui.prompt.task.{give.Id}.give");
            }

            var pick = tasks.PickUpAt(Id);
            return pick != null ? Loc.Get($"ui.prompt.task.{pick.Id}.pick") : null;
        }

        /// <summary>Hands in or picks up what the place wants now, and says it on the screen; false when it wants nothing.</summary>
        public bool Use(SliceWorldDriver world)
        {
            var tasks = Tasks;
            if (tasks == null)
            {
                return false;
            }

            string line;
            var give = tasks.HandInAt(Id);
            if (give != null)
            {
                var pay = tasks.HandIn(give.Id, world.Clock.Tick);
                line = pay > 0 ? Loc.Format("ui.task.paid", Coins.Show(pay)) : Loc.Get("ui.task.late");
            }
            else
            {
                var pick = tasks.PickUpAt(Id);
                if (pick == null)
                {
                    return false;
                }

                line = tasks.PickUp(pick.Id) ? Loc.Get($"ui.task.{pick.Id}.picked") : Loc.Get("ui.task.heavy");
            }

            Hud()?.ShowMessage(line);
            return true;
        }

        private HudView Hud() => _hud = _hud != null && _hud.isActiveAndEnabled ? _hud : FindAnyObjectByType<HudView>();

        private void Update()
        {
            var world = SliceWorldDriver.Running;
            var tasks = Tasks;
            if (tasks == null || world.Clock.IsPaused || SceneTransition.IsTraveling || (world.Fights != null && world.Fights.InFight))
            {
                return;
            }

            _player = _player != null ? _player : FindAnyObjectByType<PlayerLocomotion>();
            if (_player == null)
            {
                return;
            }

            var at = _player.transform.position;
            if (new Vector2(at.x - transform.position.x, at.z - transform.position.z).magnitude > ReturnDistance)
            {
                _armed = true;
            }

            var hidden = tasks.HiddenAt(Id);
            if (hidden != null && _armed && InReach(at, NoticeReach))
            {
                _armed = false;
                Notice(world, tasks, hidden);
            }

            var near = Door > 0 && InReach(at, DoorReach);
            if (near && !_atDoor && tasks.PassDoor(Door, world.Clock.Tick, SeenByTheWatch(world, at)))
            {
                Hud()?.ShowMessage(Loc.Get("ui.task.chalk.door"));
            }

            _atDoor = near;
            var input = GameInputService.Instance;
            if (input == null || !input.InteractPressed || !InReach(at, Reach))
            {
                return;
            }

            // E belongs to the place only while the HUD shows its prompt, no door in reach keeps it, and the character is free.
            var prompt = PromptFor(at);
            if (prompt == null || Hud() == null || _hud.AnyWindowOpen || _hud.PromptText != prompt
                || FindObjectsByType<Portal>(FindObjectsInactive.Exclude).Any(d => d.isActiveAndEnabled && d.InReach(at))
                || (world.Work != null && world.Work.Busy))
            {
                return;
            }

            Use(world);
        }

        /// <summary>One Perception check of 07.1 at the task's stage, from the action checks' stream forked by the task and the tick.</summary>
        private void Notice(SliceWorldDriver world, SliceTasks tasks, PaidTask task)
        {
            if (world.ActionChecks == null)
            {
                return;
            }

            var rolls = world.Seed.Fork(SliceSeedStreams.ActionChecks, StableHash.Of(SliceTasks.Key(task.Id, "noticed")), (int)world.Clock.Tick);
            var difficulty = GameDev.Slice.Caches.SliceCaches.NoticeDifficulty(task.NoticeStage);
            var chance = TraceSpot.LookChance(world.ActionChecks, world.Character, difficulty, world.Mind != null ? world.Mind.CheckPenalty : 0);
            var noticed = rolls.NextDouble() < chance;
            world.Character.Train(null, task.NoticeStage, noticed ? CheckOutcome.Normal : CheckOutcome.Failure,
                new[] { CharacterAttribute.Perception }, new[] { CharacterAttribute.Intelligence });
            if (noticed)
            {
                tasks.Notice(task.Id);
                Hud()?.ShowMessage(Loc.Get($"ui.task.{task.Id}.noticed"));
            }
        }

        /// <summary>Whether a Warden or a Veinmarket man sees the character at <paramref name="at"/> now, by its cone and line of sight.</summary>
        private static bool SeenByTheWatch(SliceWorldDriver world, Vector3 at)
        {
            if (world.Hiding == null)
            {
                return false;
            }

            foreach (var body in PeopleAround.InSight())
            {
                var watch = PeopleAround.IsWarden(body) || body.name.Contains("Veinmarket")
                    || (body.GetComponentInParent<NamedPerson>() is NamedPerson named && named.Id == "dace");
                var eyes = watch ? body.GetComponentInChildren<StealthObserver>() : null;
                if (eyes != null && eyes.isActiveAndEnabled && world.Hiding.Sees(eyes, at, world.Hiding.Active))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
