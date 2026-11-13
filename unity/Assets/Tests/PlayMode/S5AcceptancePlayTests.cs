#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GameDev.Core.Localization;
using GameDev.Core.Scenes;
using GameDev.Items;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Slice.Alchemy;
using GameDev.Slice.Creation;
using GameDev.Slice.Save;
using GameDev.Slice.Theft;
using GameDev.Slice.Trial;
using GameDev.UI;
using GameDev.World.Portals;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5.8, S5's acceptance through the real scene (the plan's *Accepted when (S5 as a whole)*); S5a's part, gathering,
    /// learning and crafting, buying and stealing, is <see cref="S5aAcceptancePlayTests"/>. A new character made on the
    /// creation page learns Alchemy from a Master, brews its Limit 9 potion at a bench, drinks it and settles, earns its
    /// Conduct Progress, and brews and drinks its Limit 8 potion within two game days, either joining the faction through
    /// the trial or keeping Thomas's cache; a Suspend Save then restores the run exactly. The ingredients are put in the
    /// bag: their gathering and skinning are S5a's paths, and the two days measure the run from the trial to Limit 8.
    /// </summary>
    public sealed class S5AcceptancePlayTests : SliceInputTestBase
    {
        private const string Veilmouth = "Assets/Scenes/Slice/Veilmouth.unity";
        private const int TwoDays = 2 * 24 * 60;

        private string _language;

        [SetUp]
        public void OfferCreation()
        {
            _language = Loc.Language;
            Loc.Language = Loc.English;
            CharacterCreation.Forced = true;
            SliceSaveSlot.Path = Path.Combine(Application.temporaryCachePath, "suspend-s5-acceptance.json");
            SliceSaveSlot.Delete();
        }

        [TearDown]
        public void Forget()
        {
            Loc.Language = _language;
            CharacterCreation.Forced = false;
            SliceSaveSlot.Delete();
            SliceSaveSlot.Path = null;
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator ANewCharacterJoinsTheFactionAndReachesLimit8WithinTwoDays()
        {
            // 07 *Creation choices in V1*: every attribute at 1, then the Sleuth's 11 points for the Priestess.
            yield return BeginRun();
            var world = SliceWorldDriver.Running;
            Assert.IsTrue(world.Creation.IsOpen, "a new run starts from the creation page");
            foreach (CharacterAttribute attribute in Enum.GetValues(typeof(CharacterAttribute)))
            {
                Assert.AreEqual(1, world.Character.Get(attribute), $"{attribute} starts at 1");
            }

            world.Creation.TakeArchetype(1);
            yield return Begin(world, "Mira");
            Assert.AreEqual(6, world.Character.Get(CharacterAttribute.Perception), "the Sleuth's archetype");
            var purses = Purses();
            var start = world.Clock.Tick;

            // The trial: Thomas's cache found, the Alchemy lesson paid from his savings, the cache handed to Ashgrave.
            world.State.Raise(SliceTrial.NoticedKey); // Lottie's line and the board, TrialPlayTests
            Assert.IsTrue(world.Trial.TakeCache());
            yield return Morning(world);
            Assert.IsNull(world.Shops.Learn("harrow", SliceAlchemy.Lesson), "Alchemy from Harrow, every character's first profession");

            var house = Object.FindObjectsByType<Portal>(FindObjectsInactive.Exclude).First(p => p.Id == "faction-house.street");
            Assert.IsTrue(SceneTransition.Travel(house.Target));
            yield return TrialPlayTests.WaitFor(SliceWorldDriver.FactionHouseScene);
            var ashgrave = Object.FindObjectsByType<PersonTalk>(FindObjectsInactive.Exclude).First(t => t.Dialogue != null && t.Dialogue.Id == SliceTrial.Ashgrave);
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            player.Teleport(ashgrave.transform.position + (ashgrave.transform.forward * 1.2f), Quaternion.LookRotation(-ashgrave.transform.forward));
            yield return new WaitForSeconds(0.2f);
            yield return TrialPlayTests.Talk(ashgrave, "work", "accept");
            yield return TrialPlayTests.Talk(ashgrave, "cache", "handed_go");
            world.State.Raise(SliceTrial.VouchedKey); // Father Hale's word, his conversation tested as data
            yield return TrialPlayTests.Talk(ashgrave, "vouched", "entry_thanks");
            Assert.IsTrue(world.Trial.IsMember, "Faction Entry through the trial");
            Assert.AreEqual(1, world.Bag.Count(SliceTrial.PriestessNine), "the Priestess's Limit 9 formula with it");
            yield return null;
            yield return null;
            Assert.IsTrue(Object.FindAnyObjectByType<HudView>().ClickShopLine(2), "a Faction Gift taken");

            // Brews its Limit 9 potion at the faction's free bench, drinks it, and settles into the Priestess.
            var bench = Object.FindObjectsByType<AlchemyBench>(FindObjectsInactive.Exclude).Single(b => b.Kind == BenchKind.Faction);
            Ingredients(world, ("herb", 3), ("wolf_fang", 1), ("glow_essence_9", 1));
            yield return Brew(world, bench, "potion_priestess_9");
            yield return Drink(world, "potion_priestess_9", Arcana.Priestess, 9);

            // Ashgrave gives the next step at Limit 9.
            player = Object.FindAnyObjectByType<PlayerLocomotion>();
            player.Teleport(ashgrave.transform.position + (ashgrave.transform.forward * 1.2f), Quaternion.LookRotation(-ashgrave.transform.forward));
            yield return new WaitForSeconds(0.2f);
            yield return TrialPlayTests.Talk(ashgrave, "step", "step_thanks");
            Assert.AreEqual(1, world.Bag.Count(SliceTrial.PriestessEight), "the Limit 8 formula");

            // Conduct Progress: three purses lifted unseen, then three conversations overheard and two watched places
            // gone through unnoticed, each kind's own path tested in TheftPlayTests, PoiEavesdrop's and the Dwellings'.
            var potion8 = world.Bag.Catalogue.Find("potion_priestess_8");
            Assert.AreEqual("ui.drink.fault.ready", world.Advancement.Fault(new ItemPiece("potion_priestess_8"), potion8), "not before its 8 points");
            foreach (var person in purses.Take(3))
            {
                Assert.AreEqual(TheftOutcome.Lifted, world.Theft.Resolve(person, ReactionResult.Good, out _));
            }

            Assert.AreEqual(3, world.Conduct.Points, "a purse lifted unseen is the Priestess's deed");
            for (var i = 0; i < 3; i++)
            {
                Assert.IsTrue(world.Conduct.Deed(ConductDeed.Overhear, "overheard.acceptance." + i));
            }

            for (var i = 0; i < 2; i++)
            {
                Assert.IsTrue(world.Conduct.Deed(ConductDeed.Unnoticed, "dwelling.acceptance." + i));
            }

            Assert.AreEqual(SliceAdvancement.ConductForLimit8, world.Conduct.Points);

            // Brews and drinks its Limit 8 potion, within two game days of the run's start.
            Ingredients(world, ("herb", 4), ("wolf_fang", 2), ("flax", 1), ("glow_essence_8", 1));
            yield return Brew(world, bench, "potion_priestess_8");
            yield return Drink(world, "potion_priestess_8", Arcana.Priestess, 8);
            Assert.Less(world.Clock.Tick - start, TwoDays, $"Limit 8 within two game days: {world.Clock.Tick - start} ticks");

            // The Suspend Save restores the run exactly: the clock, the world's keys, the character, the bag and who it is.
            yield return null;
            Assert.IsNull(world.Save.RefusalKey(), "a save may be written at peace");
            var saved = Snapshot(world);
            Assert.IsNull(world.Save.Write());
            world.Bag.Earn(77);
            Assert.IsTrue(world.Bag.Add("herb", 2));
            world.State.Set(SliceAdvancement.ConductKey, 5);
            world.State.Set(SliceTrial.MemberKey, 0);
            world.Character.Set(CharacterAttribute.Perception, 1);
            Assert.AreNotEqual(saved, Snapshot(world));
            yield return world.Save.Load(SliceSaveSlot.Read());
            Assert.AreEqual(saved, Snapshot(world), "the run restored exactly");
            Assert.AreEqual(8, world.Advancement.CurrentLimit);
            Assert.IsTrue(world.Trial.IsMember);
        }

        [UnityTest]
        public IEnumerator ACharacterWhoKeepsTheCacheReachesTheEmperorsLimit8()
        {
            // 9 free points for the Emperor: Will, Strength and Endurance.
            yield return BeginRun();
            var world = SliceWorldDriver.Running;
            foreach (var (attribute, points) in new[] { (CharacterAttribute.Will, 4), (CharacterAttribute.Strength, 3), (CharacterAttribute.Endurance, 2) })
            {
                for (var i = 0; i < points; i++)
                {
                    world.Creation.AddPoint(attribute);
                }
            }

            Assert.AreEqual(0, world.Creation.PointsLeft);
            yield return Begin(world, "Rook");
            Assert.AreEqual(5, world.Character.Get(CharacterAttribute.Will));
            var purses = Purses();
            var start = world.Clock.Tick;

            // Keeps the cache: the Emperor's formulas and Thomas's savings, and never joins.
            world.State.Raise(SliceTrial.NoticedKey);
            Assert.IsTrue(world.Trial.TakeCache());
            Assert.IsTrue(SliceTrial.CacheFormulas.All(f => world.Bag.Count(f) == 1), "the Emperor's two formulas");
            foreach (var person in purses.Take(4))
            {
                world.Theft.Resolve(person, ReactionResult.Good, out _); // Uninitiated, a theft is no act against the Emperor yet
            }

            yield return Morning(world);
            Assert.IsNull(world.Shops.Learn("harrow", SliceAlchemy.Lesson));
            Assert.GreaterOrEqual(world.Bag.Bronze, 200, "Harrow's fee for two brewings");

            // Harrow's bench, for his fee.
            var door = Object.FindObjectsByType<Portal>(FindObjectsInactive.Exclude).First(p => p.Id == "harrow-apothecary.street");
            Assert.IsTrue(SceneTransition.Travel(door.Target));
            yield return TrialPlayTests.WaitFor(door.Target.SceneName);
            var bench = Object.FindObjectsByType<AlchemyBench>(FindObjectsInactive.Exclude).Single(b => b.Kind == BenchKind.Harrow);
            Ingredients(world, ("herb", 2), ("boar_tusk", 1), ("glow_essence_9", 1));
            yield return Brew(world, bench, "potion_emperor_9");
            yield return Drink(world, "potion_emperor_9", Arcana.Emperor, 9);
            Assert.IsFalse(world.Trial.IsMember, "the cache kept, the faction never joined");

            // Conduct Progress: the Emperor's deeds, each kind's own path tested with the fights and the halt.
            foreach (var deed in new[] { ConductDeed.Threaten, ConductDeed.StrongerSide, ConductDeed.Stand })
            {
                for (var i = 0; i < 3 && world.Conduct.Points < SliceAdvancement.ConductForLimit8; i++)
                {
                    Assert.IsTrue(world.Conduct.Deed(deed, $"{deed}.acceptance.{i}"));
                }
            }

            Assert.AreEqual(SliceAdvancement.ConductForLimit8, world.Conduct.Points);
            Ingredients(world, ("herb", 3), ("boar_tusk", 2), ("iron_ore", 1), ("glow_essence_8", 1));
            yield return Brew(world, bench, "potion_emperor_8");
            yield return Drink(world, "potion_emperor_8", Arcana.Emperor, 8);
            Assert.Less(world.Clock.Tick - start, TwoDays, $"Limit 8 within two game days: {world.Clock.Tick - start} ticks");
        }

        /// <summary>A run from its start: no world before it, the city loaded, the creation page opened at its first frames.</summary>
        private static IEnumerator BeginRun()
        {
            yield return ForgetTheWorld();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Veilmouth, new LoadSceneParameters(LoadSceneMode.Single));
            yield return new WaitForSecondsRealtime(0.5f); // real time: the page stops the frame's time
            DebugSight.On = false;
        }

        private static IEnumerator Begin(SliceWorldDriver world, string name)
        {
            Object.FindAnyObjectByType<HudView>().TypeShopField(name);
            Assert.IsTrue(world.Creation.Begin(), "the life begins");
            Assert.IsFalse(world.Creation.IsOpen);
            Assert.AreEqual(name, world.Identity.name);
            yield return null;
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            for (var t = 0f; t < 2f && !player.IsGrounded; t += Time.deltaTime)
            {
                yield return null;
            }
        }

        // The city's purses, kept by name for the thefts made indoors later.
        private static List<string> Purses() =>
            Object.FindObjectsByType<PurseTarget>(FindObjectsInactive.Exclude).Select(t => t.Person).Distinct().OrderBy(p => p, StringComparer.Ordinal).ToList();

        // The shops open at 8: the first morning's 9 o'clock, when the run starts earlier.
        private static IEnumerator Morning(SliceWorldDriver world)
        {
            if (world.Clock.MinuteOfDay < 9 * 60)
            {
                world.Clock.AdvanceTicks((9 * 60) - world.Clock.MinuteOfDay);
            }

            yield return null;
        }

        private static void Ingredients(SliceWorldDriver world, params (string item, int count)[] items)
        {
            foreach (var (item, count) in items)
            {
                Assert.IsTrue(world.Bag.Add(item, count), item);
            }
        }

        /// <summary>At the bench: its page, the potion's line, six checks stopped in their zone, and the potion in the bag.</summary>
        private static IEnumerator Brew(SliceWorldDriver world, AlchemyBench bench, string potion)
        {
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var hud = Object.FindAnyObjectByType<HudView>();
            yield return AlchemyPlayTests.Stand(player, bench);
            Assert.IsTrue(bench.Open(), $"{bench.Kind}'s bench opens");
            yield return null;
            var lines = hud.ShopLines.ToList();
            var line = lines.FindIndex(l => l.StartsWith($"Brew {Loc.Get("item." + potion)}: ", StringComparison.Ordinal));
            Assert.Greater(line, 0, string.Join(" | ", lines));
            Assert.IsTrue(hud.ClickShopLine(line));
            yield return null;
            Assert.IsTrue(world.Work.Busy, "the character works the bench");
            for (var check = 0; check < SliceAlchemy.Checks; check++)
            {
                yield return GatheringPlayTests.AtKey(world);
                Assert.IsTrue(world.Reaction.Running, $"check {check + 1}'s bar");
                yield return GatheringPlayTests.StopInZone(world.Reaction);
            }

            yield return GatheringPlayTests.PlayedOut(world);
            Assert.IsFalse(world.Work.Busy);
            Assert.AreEqual(1, world.Bag.Pieces.Count(p => p.item == potion), $"{potion} brewed");
            bench.Close();
            yield return null;
        }

        /// <summary>Drinks the potion; its Assimilation's ticks pass, the character crouched, and the Limit settles.</summary>
        private static IEnumerator Drink(SliceWorldDriver world, string potion, Arcana arcana, int limit)
        {
            var piece = world.Bag.Pieces.Single(p => p.item == potion);
            var definition = world.Bag.Catalogue.Find(potion);
            Assert.IsNull(world.Advancement.Fault(piece, definition), $"{potion} may be drunk");
            var tick = world.Clock.Tick;
            Assert.IsTrue(world.Advancement.Drink(piece, definition));
            Assert.IsTrue(world.Advancement.Assimilating, "Assimilation begins");
            for (var t = 0f; t < 60f && world.Advancement.CurrentLimit != limit; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.AreEqual(arcana, world.Advancement.CurrentArcana);
            Assert.AreEqual(limit, world.Advancement.CurrentLimit, $"{arcana} at Limit {limit}");
            Assert.GreaterOrEqual(world.Clock.Tick, tick + SliceAdvancement.AssimilationTicks, "its 60 ticks passed");
            Assert.AreEqual(0, world.Conduct.Points, "Conduct Progress counts afresh toward the next Limit");
            yield return null;
        }

        // What a Suspend Save keeps of the run, written out to compare whole.
        private static string Snapshot(SliceWorldDriver world)
        {
            var text = new StringBuilder();
            text.Append("tick ").Append(world.Clock.Tick).Append('\n');
            text.Append("who ").Append(world.Identity.name).Append(' ').Append(world.Identity.look).Append(' ').Append(world.Identity.female).Append('\n');
            foreach (var key in world.State.Keys)
            {
                text.Append(key).Append('=').Append(world.State.Get(key)).Append('\n');
            }

            foreach (CharacterAttribute attribute in Enum.GetValues(typeof(CharacterAttribute)))
            {
                text.Append(attribute).Append('=').Append(world.Character.Get(attribute)).Append('\n');
            }

            foreach (CharacterSkill skill in Enum.GetValues(typeof(CharacterSkill)))
            {
                text.Append(skill).Append('=').Append(world.Character.Get(skill)).Append('\n');
            }

            text.Append("bronze ").Append(world.Bag.Bronze).Append('\n');
            foreach (var (item, count) in world.Bag.Stacks.OrderBy(s => s.item.id, StringComparer.Ordinal))
            {
                text.Append(item.id).Append(' ').Append(count).Append('\n');
            }

            foreach (var piece in world.Bag.Pieces.OrderBy(p => p.item, StringComparer.Ordinal).ThenBy(p => p.perfect).ThenBy(p => p.bad))
            {
                text.Append(piece.item).Append(' ').Append(piece.perfect).Append(' ').Append(piece.bad).Append(' ').Append(world.Outfit.IsWorn(piece)).Append('\n');
            }

            return text.ToString();
        }
    }
}
#endif
