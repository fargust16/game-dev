#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Locomotion;
using GameDev.Slice.Alchemy;
using GameDev.Slice.Dwellings;
using GameDev.Slice.Kit;
using GameDev.Slice.Shops;
using GameDev.UI;
using GameDev.World.Poi;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5b.4 (20 §3; SLICE_TRIAL §4): a Limit 9 potion brewed at each of the three benches, Harrow's lab bench for his fee, the
    /// faction house archive's table free for members, and the desk of Veinmarket's den at night to one who heard of it; six
    /// checks with the world paused at each, then the brewing's ticks, and Alchemy grows.
    /// </summary>
    public sealed class AlchemyPlayTests : SliceInputTestBase
    {
        private const string Veilmouth = "Assets/Scenes/Slice/Veilmouth.unity";
        private const string Interiors = "Assets/Scenes/Slice/Interiors/";

        private string _language;

        [SetUp]
        public void KeepLanguage()
        {
            _language = Loc.Language;
            Loc.Language = Loc.English;
        }

        [TearDown]
        public void RestoreLanguage()
        {
            Loc.Language = _language;
        }

        [UnityTest]
        public IEnumerator HarrowsBenchBrewsForHisFee()
        {
            var world = default(SliceWorldDriver);
            yield return World(9 * 60, w => world = w);
            world.State.Set(SliceShops.LessonKey(SliceAlchemy.Lesson), 1);
            yield return LoadScene(Interiors + "HarrowApothecary.unity");

            var bench = Bench(BenchKind.Harrow);
            yield return Brew(world, bench, 100, "Alchemy: 1 silver a brewing");
        }

        [UnityTest]
        public IEnumerator TheFactionsBenchIsFreeForMembers()
        {
            var world = default(SliceWorldDriver);
            yield return World(9 * 60, w => world = w);
            world.State.Set(SliceShops.LessonKey(SliceAlchemy.Lesson), 1);
            yield return LoadScene(Interiors + "FactionHouse.unity");

            var bench = Bench(BenchKind.Faction);
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            yield return Stand(player, bench);
            Assert.AreEqual("The faction's bench: members only", Object.FindAnyObjectByType<HudView>().PromptText, Describe(player));
            Assert.IsFalse(bench.Open(), "not for one who has not joined");

            world.State.Set(SliceAlchemy.MemberKey, 1);
            yield return Brew(world, bench, 0, "Alchemy: free for members");
        }

        [UnityTest]
        public IEnumerator TheDensBenchBrewsAtNightToOneWhoHeardOfIt()
        {
            var world = default(SliceWorldDriver);
            yield return World(22 * 60 + 30, w => world = w);
            world.State.Set(SliceShops.LessonKey(SliceAlchemy.Lesson), 1);
            yield return LoadScene(Interiors + "Dwelling.unity");
            var runtime = DwellingRuntime.Current;
            Assert.IsNotNull(runtime, "the Dwelling scene");
            var den = new DwellingKey(SliceAlchemy.DenDwelling, "Veilmouth", 1480, 315, DwellingArchetype.Warehouse, DwellingWealth.Poor);

            runtime.Build(den);
            yield return new WaitForFixedUpdate();
            Assert.IsEmpty(Object.FindObjectsByType<AlchemyBench>(FindObjectsInactive.Exclude), "an ordinary warehouse until heard of");

            world.State.Set(TipBook.HeardKeyOf("kit.den"), 1);
            runtime.Build(den);
            yield return new WaitForFixedUpdate();
            var bench = Bench(BenchKind.Den);
            yield return Brew(world, bench, 300, "Alchemy: 3 silver a brewing");
        }

        /// <summary>A fresh world in Veilmouth at <paramref name="minute"/> of the first day.</summary>
        private static IEnumerator World(int minute, System.Action<SliceWorldDriver> found)
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var world = SliceWorldDriver.Running;
            Assert.IsNotNull(world?.Alchemy, "the world keeps alchemy");
            if (world.Clock.MinuteOfDay < minute)
            {
                world.Clock.AdvanceTicks(minute - world.Clock.MinuteOfDay);
            }

            found(world);
        }

        private static AlchemyBench Bench(BenchKind kind)
        {
            var benches = Object.FindObjectsByType<AlchemyBench>(FindObjectsInactive.Exclude);
            Assert.AreEqual(1, benches.Length, $"one bench in the scene: {string.Join(", ", benches.Select(b => b.name))}");
            Assert.AreEqual(kind, benches[0].Kind);
            return benches[0];
        }

        /// <summary>Opens the bench's page, clicks the Limit 9 Priestess potion and stops each of the six bars in its perfect zone.</summary>
        private IEnumerator Brew(SliceWorldDriver world, AlchemyBench bench, int fee, string heading)
        {
            if (world.Bag.Count("formula_priestess_9") == 0)
            {
                world.Bag.Add("formula_priestess_9"); // a member has it from Faction Entry (S5c.4)
            }

            world.Bag.Add("herb", 3);
            world.Bag.Add("wolf_fang", 1);
            world.Bag.Add("glow_essence_9", 1);
            var bronze = world.Bag.Bronze;
            var alchemy = world.Character.Get(GameDev.Player.Progression.CharacterSkill.Alchemy);

            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var hud = Object.FindAnyObjectByType<HudView>();
            yield return Stand(player, bench);
            Assert.AreEqual("E  Brew at the bench", hud.PromptText, Describe(player));
            Assert.IsTrue(bench.Open());
            yield return null;

            var lines = hud.ShopLines.ToList();
            Assert.AreEqual(heading, lines[0]);
            var potion = lines.FindIndex(l => l.StartsWith("Brew Limit 9 potion, Priestess: "));
            Assert.Greater(potion, 0, string.Join(" | ", lines));
            StringAssert.EndsWith("six checks, then 6 min", lines[potion]);
            Assert.IsTrue(hud.ClickShopLine(potion));
            yield return null;
            Assert.IsTrue(world.Work.Busy, "the character works the bench");
            Assert.IsFalse(hud.IsShopOpen, "the page gives way to the work");

            var tick = world.Clock.Tick;
            for (var check = 0; check < SliceAlchemy.Checks; check++)
            {
                yield return GatheringPlayTests.AtKey(world);
                Assert.IsTrue(world.Reaction.Running, $"check {check + 1}'s bar");
                Assert.IsTrue(world.Clock.IsPaused, "the world paused at each check");
                Assert.IsTrue(player.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsName(AlchemyBench.Clip));
                Assert.Greater(world.Reaction.Bar.PerfectWidth, 0d, "brewing has a perfect zone");
                yield return GatheringPlayTests.StopInZone(world.Reaction);
            }

            yield return GatheringPlayTests.PlayedOut(world);
            Assert.IsFalse(world.Work.Busy);

            var made = world.Bag.Pieces.SingleOrDefault(p => p.item == "potion_priestess_9");
            Assert.IsNotNull(made, "the potion is in the bag");
            Assert.AreEqual(6, made.perfect, "six perfect checks");
            Assert.AreEqual(bronze - fee, world.Bag.Bronze, "the bench's fee");
            Assert.AreEqual(0, world.Bag.Count("herb"));
            Assert.AreEqual(1, world.Bag.Count("formula_priestess_9"), "the formula stays");
            Assert.GreaterOrEqual(world.Clock.Tick, tick + SliceAlchemy.BrewTicks, "the brewing's ticks pass");
            Assert.Greater(world.Character.Get(GameDev.Player.Progression.CharacterSkill.Alchemy), alchemy, "Alchemy grows by use");
            Assert.IsTrue(hud.IsShopOpen, "the page again, with what came of it");
            StringAssert.StartsWith("Brewed: Limit 9 potion, Priestess: 6 perfect", hud.ShopNote);
            bench.Close();
        }

        /// <summary>Puts the character on free ground beside the bench, within its reach.</summary>
        internal static IEnumerator Stand(PlayerLocomotion player, AlchemyBench bench)
        {
            var ground = LayerMask.GetMask(KitLayers.Ground, KitLayers.Structure);
            var things = ~LayerMask.GetMask(KitLayers.Ground, KitLayers.Character);
            // A kit piece's pivot is its footprint's corner: search round the middle of what it occupies.
            var colliders = bench.GetComponentsInChildren<Collider>();
            var bounds = colliders.Length > 0 ? colliders[0].bounds : new Bounds(bench.transform.position, Vector3.zero);
            foreach (var c in colliders)
            {
                bounds.Encapsulate(c.bounds);
            }

            var centre = new Vector3(bounds.center.x, bench.transform.position.y, bounds.center.z);
            foreach (var distance in new[] { 1.0f, 1.3f, 1.6f, 1.9f, 2.2f, 2.5f })
            {
                for (var a = 0; a < 16; a++)
                {
                    var p = centre + (Quaternion.Euler(0f, a * 22.5f, 0f) * (Vector3.forward * distance));
                    if (Physics.Raycast(p + (Vector3.up * 2f), Vector3.down, out var hit, 4f, ground, QueryTriggerInteraction.Ignore)
                        && Mathf.Abs(hit.point.y - centre.y) < 0.5f
                        && !Physics.CheckCapsule(hit.point + (Vector3.up * 0.45f), hit.point + (Vector3.up * 1.6f), 0.3f, things, QueryTriggerInteraction.Ignore)
                        && bench.InReach(hit.point))
                    {
                        var facing = Vector3.ProjectOnPlane(centre - hit.point, Vector3.up);
                        player.Teleport(hit.point + (Vector3.up * 0.05f), Quaternion.LookRotation(facing, Vector3.up));
                        yield return new WaitForSeconds(0.3f);
                        Assert.IsTrue(bench.InReach(player.transform.position), Describe(player));
                        yield break;
                    }
                }
            }

            Assert.Fail($"no room beside {bench.name} at {centre}");
        }
    }
}
#endif
