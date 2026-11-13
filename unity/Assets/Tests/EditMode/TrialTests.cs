using System.Collections.Generic;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Dialogue;
using GameDev.Items;
using GameDev.Player.Progression;
using GameDev.Slice.EditorTools;
using GameDev.Slice.Trial;
using GameDev.World.Poi;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5c.4 (SLICE_TRIAL §1; 19 §6.2): the trial told in the world's state, the Faction Gift set by the World Seed, the
    /// three conversations as data, and Thomas Wren's room as placed.
    /// </summary>
    public sealed class TrialTests
    {
        private const string PoiFolder = "Assets/Data/Slice/Poi/";

        [Test]
        public void TheForkGivesWhatEachStepBrings()
        {
            var (trial, state, bag, tips, director) = Make(70413);
            var entered = 0;
            trial.Entered += () => entered++;

            state.Raise(SliceTrial.OfferedKey);
            Assert.IsTrue(tips.HasHeard(tips.Find(SliceTrial.MissingTip)), "the offer gives the note that Thomas Wren is missing");
            state.Raise(SliceTrial.KeyGivenKey);
            Assert.AreEqual(1, bag.Count(SliceTrial.ThomasKeyItem), "Lottie gives her brother's key");
            Assert.IsTrue(trial.HasThomasKey);

            Assert.IsFalse(trial.TakeCache(), "a board not noticed is not lifted");
            state.Raise(SliceTrial.BoardsArgument);
            Assert.IsTrue(trial.CacheNoticed, "Lottie's line finds the board with no roll");
            Assert.IsTrue(trial.TakeCache());
            Assert.IsFalse(trial.TakeCache(), "the cache is taken once");
            foreach (var formula in SliceTrial.CacheFormulas)
            {
                Assert.AreEqual(1, bag.Count(formula), formula);
            }

            Assert.AreEqual(1, bag.Count(SliceTrial.LetterItem));
            Assert.AreEqual(SliceTrial.Savings, bag.Bronze, "Thomas's savings");
            Assert.IsTrue(tips.HasHeard(tips.Find(SliceTrial.CacheTip)), "a note of what lay under the board");

            state.Raise(SliceTrial.HandedKey);
            Assert.IsTrue(SliceTrial.CacheFormulas.All(f => bag.Count(f) == 0), "Ashgrave takes the formulas");
            Assert.AreEqual(0, bag.Count(SliceTrial.LetterItem), "and the letter");
            Assert.AreEqual(SliceTrial.Savings, bag.Bronze, "the coins stay the character's");

            state.Raise(SliceTrial.VouchedKey);
            Assert.IsTrue(tips.HasHeard(tips.Find(SliceTrial.VouchTip)), "Father Hale's word is a note");
            Assert.IsFalse(trial.GiftDue);
            state.Raise(SliceTrial.MemberKey);
            Assert.AreEqual(1, entered, "Faction Entry");
            Assert.AreEqual(1, bag.Count(SliceTrial.PriestessNine), "the formula of Limit 9, Жрица");
            Assert.AreEqual(SliceTrial.Savings + SliceTrial.RecruitPurse, bag.Bronze, "the recruit's purse");
            Assert.IsTrue(trial.GiftDue);

            var offer = trial.GiftOffer();
            var pieces = bag.Pieces.Count;
            Assert.IsTrue(trial.TakeGift(1));
            Assert.IsFalse(trial.TakeGift(0), "one gift of three");
            Assert.IsFalse(trial.GiftDue);
            Assert.AreEqual(offer[1].item, bag.Pieces[pieces].item, "the item taken");
            Assert.AreEqual(SliceTrial.IsFirearm(offer[1].item) ? pieces + 2 : pieces + 1, bag.Pieces.Count, "a firearm comes with its pouch");

            trial.Advanced(Arcana.Emperor, 9);
            Assert.IsFalse(state.IsSet(SliceTrial.PriestessNineKey));
            trial.Advanced(Arcana.Priestess, 9);
            Assert.IsTrue(state.IsSet(SliceTrial.PriestessNineKey), "a member at Limit 9 of Жрица has Ashgrave's next step");
            state.Raise(SliceTrial.NextFormulaKey);
            Assert.AreEqual(1, bag.Count(SliceTrial.PriestessEight), "the formula of Limit 8, Жрица");
            director.Dispose();
        }

        [Test]
        public void ALoadGivesNothingTwice()
        {
            var (_, state, _, _, director) = Make(70413);
            state.Raise(SliceTrial.MemberKey);
            director.Dispose();

            var loaded = new WorldState();
            var (trial, again, bag, _, otherDirector) = Make(70413, loaded);
            again.Restore(state.Capture());
            Assert.AreEqual(0, bag.Count(SliceTrial.PriestessNine), "a restored key is not raised again");
            Assert.AreEqual(0, bag.Bronze);
            Assert.IsTrue(trial.IsMember);
            otherDirector.Dispose();
        }

        [Test]
        public void TheGiftIsThreeDifferentBasesSetByTheSeed()
        {
            var items = Items();
            var shares = new int[5];
            for (var seed = 1; seed <= 2000; seed++)
            {
                var offer = SliceTrial.Offer(new WorldSeed(seed), items);
                Assert.AreEqual(SliceTrial.GiftCount, offer.Count);
                Assert.AreEqual(3, offer.Select(p => p.item).Distinct().Count(), $"seed {seed}: three different items");
                foreach (var piece in offer)
                {
                    CollectionAssert.Contains(SliceTrial.GiftBases, piece.item);
                    Assert.AreEqual(9, piece.stage);
                    Assert.AreNotEqual(ItemQuality.Normal, piece.Quality, "a gift is never plain");
                    Assert.LessOrEqual(piece.modifiers.Count, 4);
                    shares[(int)piece.Quality]++;
                }

                if (seed <= 20)
                {
                    var again = SliceTrial.Offer(new WorldSeed(seed), items);
                    CollectionAssert.AreEqual(offer.Select(Describe), again.Select(Describe), "the same seed offers the same three");
                }
            }

            var total = (double)shares.Sum();
            Assert.AreEqual(0.70d, shares[(int)ItemQuality.Magic] / total, 0.03d, "Magic in 70 %");
            Assert.AreEqual(0.28d, shares[(int)ItemQuality.Rare] / total, 0.03d, "Rare in 28 %");
            Assert.AreEqual(0.02d, shares[(int)ItemQuality.Epic] / total, 0.01d, "Epic in 2 %");
            Assert.AreEqual(0, shares[(int)ItemQuality.Legendary]);
        }

        [Test]
        public void TheModifiersFollowTheQualityDraw()
        {
            Assert.AreEqual(1, SliceTrial.Count(0.10d, 0.40d));
            Assert.AreEqual(2, SliceTrial.Count(0.69d, 0.60d));
            Assert.AreEqual(3, SliceTrial.Count(0.70d, 0.10d));
            Assert.AreEqual(3, SliceTrial.Count(0.979d, 0.90d));
            Assert.AreEqual(4, SliceTrial.Count(0.98d, 0.10d));
        }

        [Test]
        public void TheConversationsAreSoundData()
        {
            foreach (var id in new[] { SliceTrial.Lottie, SliceTrial.Ashgrave, SliceTrial.Hale })
            {
                var dialogue = SlicePeople.DialogueOf(id);
                Assert.IsNotNull(dialogue, id);
                Assert.AreEqual(id, dialogue.Id);
                CollectionAssert.IsEmpty(dialogue.Problems(), id);
                foreach (var key in dialogue.Keys())
                {
                    Assert.IsTrue(Loc.Exists(key), key);
                }
            }

            var tips = AssetDatabase.LoadAssetAtPath<TipCatalogue>(PoiFolder + "Tips.asset");
            foreach (var tip in new[] { SliceTrial.MissingTip, SliceTrial.CacheTip, SliceTrial.VouchTip, "trial.ledger", "trial.letter", "trial.grate" })
            {
                Assert.IsNotNull(tips.Tips.FirstOrDefault(t => t.id == tip), tip);
                Assert.IsTrue(Loc.Exists($"tip.{tip}.note"), $"{tip} enters the Character Notes");
            }

            foreach (var item in new[] { SliceTrial.ThomasKeyItem, SliceTrial.LetterItem })
            {
                Assert.IsNotNull(Items().Find(item), item);
                Assert.AreEqual(0f, Items().Find(item).weight, item);
            }
        }

        [Test]
        public void ThomasWrensRoomHoldsTheTracesAndTheBoard()
        {
            EditorSceneManager.OpenScene(VeilmouthDressing.MissingRoomScenePath, OpenSceneMode.Single);
            var root = GameObject.Find(SliceTrialPlaces.RootName);
            Assert.IsNotNull(root, "the trial is placed in Thomas Wren's room");
            var traces = root.GetComponentsInChildren<TraceSpot>(true);
            CollectionAssert.AreEquivalent(new[] { "trial.ledger", "trial.letter", "trial.grate" }, traces.Select(t => t.Tip));
            Assert.IsTrue(traces.All(t => t.Poi == null), "the trial's traces belong to no POI");
            var board = root.GetComponentsInChildren<TrialCache>(true).Single();
            var bed = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(t => t.name.StartsWith("Kit_ViFurniture_Bed"));
            var frame = bed.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            var at = board.transform.position;
            Assert.IsTrue(at.x > frame.min.x && at.x < frame.max.x && at.z > frame.min.z && at.z < frame.max.z, $"the loose board at {at} is under the bed {frame}");
            var grate = traces.Single(t => t.Tip == "trial.grate").transform.position;
            Assert.Greater(grate.sqrMagnitude, 1f, "the burnt page lies in the room's grate, not at the origin");
            Assert.IsTrue(Physics.Raycast(board.transform.position + Vector3.up, Vector3.down, out var floor, 1.5f), "on the floor");
            Assert.AreEqual(floor.point.y, board.transform.position.y, 0.15f);
        }

        private static string Describe(ItemPiece piece) =>
            piece.item + ":" + string.Join(",", piece.modifiers.Select(m => $"{m.type}/{m.major}/{m.tier}/{m.value:0.000}"));

        private static ItemCatalogue Items() => AssetDatabase.LoadAssetAtPath<ItemCatalogue>("Assets/Data/Items/Items.asset");

        private static (SliceTrial, WorldState, Bag, TipBook, PoiDirector) Make(int seed, WorldState state = null)
        {
            state ??= new WorldState();
            var pois = new[] { "PoiA", "PoiB", "PoiC" }.Select(n => AssetDatabase.LoadAssetAtPath<PoiDefinition>(PoiFolder + n + ".asset")).ToArray();
            var clock = new GameClock(1d, 480);
            var director = new PoiDirector(pois, state, new WorldSeed(seed), SliceSeedStreams.PoiUnattended, clock);
            var tips = new TipBook(AssetDatabase.LoadAssetAtPath<TipCatalogue>(PoiFolder + "Tips.asset"), state, director);
            var bag = new Bag(Items(), new CharacterSheet());
            return (new SliceTrial(state, bag, tips, new WorldSeed(seed), Items(), () => clock.Tick), state, bag, tips, director);
        }
    }
}
