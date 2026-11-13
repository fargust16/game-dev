using System.Collections.Generic;
using System.Linq;
using GameDev.Confront.Rules;
using GameDev.Dialogue;
using GameDev.Player.Progression;
using GameDev.Player.Standing;
using GameDev.Slice.Fights;
using GameDev.Slice.People;
using GameDev.World.Poi;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using NUnit.Framework;
using UnityEditor;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5b.8 (05.1 *Вне боя*): which abilities have a use out of a fight; Внушение's bonus inside a Social Check's
    /// modifiers; the lines Чуткое ухо marks as lies; and the memory Чужой сон reads from a source.
    /// </summary>
    public sealed class OutsideAbilityTests
    {
        private const string PoiFolder = "Assets/Data/Slice/Poi/";
        private const string DialogueFolder = "Assets/Data/Slice/Dialogue/";

        [Test]
        public void TheSlicesAbilitiesOfExplorationAreTheTablesSix()
        {
            var outside = AssetDatabase.FindAssets("t:FightAbility", new[] { "Assets/Data/Confront/Abilities" })
                .Select(g => AssetDatabase.LoadAssetAtPath<FightAbility>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(SliceConfront.HasOutsideUse).Select(a => a.Key).OrderBy(k => k).ToList();
            CollectionAssert.AreEqual(new[] { "double", "dream_touch", "drowse", "halt", "screen", "suggestion" }, outside,
                "05.1: the active abilities of the slice's Limits with a use out of a fight; Чуткое ухо and Родство are passive");
        }

        [Test]
        public void SuggestionsBonusCountsInsideTheChecksModifiers()
        {
            var state = new WorldState();
            var clock = new GameClock(1d, 14 * 60);
            var seed = new WorldSeed(110201);
            var pois = new[] { "PoiA", "PoiB", "PoiC" }.Select(n => AssetDatabase.LoadAssetAtPath<PoiDefinition>(PoiFolder + n + ".asset")).ToArray();
            using var director = new PoiDirector(pois, state, seed, SliceSeedStreams.PoiUnattended, clock);
            director.Evaluate(clock.Tick);
            director.ResolveParticipated(pois[0]);
            using var standing = new SocialStanding(state, Load<StandingRules>("Assets/Data/Progression/StandingRules.asset"), clock, seed, SliceSeedStreams.Lies);
            var bonus = 0;
            var world = new DialogueContext
            {
                State = state,
                Clock = clock,
                Seed = seed,
                CheckStream = SliceSeedStreams.SocialChecks,
                Rules = Load<SocialCheckRules>("Assets/Data/Progression/SocialCheckRules.asset"),
                Standing = standing,
                Character = new CharacterSheet(),
                Director = director,
                Tips = new TipBook(Load<TipCatalogue>(PoiFolder + "Tips.asset"), state, director),
                Withheld = new HashSet<Approach>(),
                SkillBonus = skill => skill == CharacterSkill.Persuasion ? bonus : 0,
            };
            state.Set("tip.trace.wax.heard", 1); // an Argument, so the sweep's checks are offered
            var talk = new DialogueSession(Load<DialogueDefinition>(DialogueFolder + "SweepDialogue.asset"), world);
            var convince = talk.Options.Single(o => o.id == "convince");
            var threaten = talk.Options.Single(o => o.id == "threaten");
            var plain = talk.Preview(convince);
            var plainThreat = talk.Preview(threaten);
            bonus = SliceConfront.SuggestionBonus;
            Assert.AreEqual(plain.Modifiers + 2, talk.Preview(convince).Modifiers, "+2 to the next Persuasion check");
            Assert.Greater(talk.Preview(convince).Chance, plain.Chance);
            Assert.AreEqual(plainThreat.Modifiers, talk.Preview(threaten).Modifiers, "never to Intimidation");
            talk.Leave();
        }

        [Test]
        public void KeenEarHearsTheSlicesThreeLies()
        {
            var lies = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:DialogueDefinition", new[] { "Assets/Data/Slice/Dialogue" }))
            {
                var dialogue = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                lies.AddRange(dialogue.Nodes.Where(n => n.lies).Select(dialogue.LineKey));
            }

            CollectionAssert.AreEquivalent(new[] { "dialogue.sweep.shrug", "dialogue.nell.refused", "dialogue.poia.rebuffed" }, lies,
                "the sweep saw the coach, Nell knows her stories, and the Wardens came for the list, not the cabinet");
        }

        [Test]
        public void EveryoneDreamsTheirOwnFragmentOnce()
        {
            var catalogue = Load<TipCatalogue>(PoiFolder + "Tips.asset");
            var dreams = catalogue.Tips.Where(t => t.source == TipSource.Dream).ToList();
            Assert.AreEqual(20, dreams.Count, "SLICE_PEOPLE §5: fifteen named people and five kinds");
            foreach (var dream in dreams)
            {
                StringAssert.StartsWith("dream.", dream.id);
                Assert.IsTrue(dream.needsApproach, "no source's panel reads it out");
                Assert.IsNull(dream.poi, "never a Weak Point's trace, which is C6's");
                Assert.AreEqual(0, dream.day);
                Assert.AreEqual(0, dream.fromMinute);
                Assert.IsTrue(string.IsNullOrWhiteSpace(dream.requiresKey));
            }

            foreach (var entry in SliceRoster.All)
            {
                Assert.IsTrue(dreams.Any(d => d.id == SliceConfront.DreamTip(entry.Id)), entry.Id);
            }

            Assert.IsTrue(dreams.Any(d => d.id == SliceConfront.DreamTip("kettle")) && dreams.Any(d => d.id == SliceConfront.DreamTip("hale")));
            Assert.AreEqual("dream.warden", SliceConfront.DreamTip(ResidentRole.Warden, false));
            Assert.AreEqual("dream.hand", SliceConfront.DreamTip(ResidentRole.Dockworker, false));
            Assert.AreEqual("dream.hand", SliceConfront.DreamTip(ResidentRole.FactoryWorker, true));
            Assert.AreEqual("dream.drunkard", SliceConfront.DreamTip(ResidentRole.Vagrant, false));
            Assert.AreEqual("dream.townswoman", SliceConfront.DreamTip(ResidentRole.Townsfolk, true));
            Assert.AreEqual("dream.townsman", SliceConfront.DreamTip(ResidentRole.Moneylender, false));

            var state = new WorldState();
            var clock = new GameClock(1d, 23 * 60);
            var seed = new WorldSeed(110201);
            var pois = new[] { "PoiA", "PoiB", "PoiC" }.Select(n => AssetDatabase.LoadAssetAtPath<PoiDefinition>(PoiFolder + n + ".asset")).ToArray();
            using var director = new PoiDirector(pois, state, seed, SliceSeedStreams.PoiUnattended, clock);
            var book = new TipBook(catalogue, state, director);
            CollectionAssert.IsEmpty(book.Available(TipSource.Dream, clock.Tick), "told only by a touch");
            Assert.IsNotNull(book.Tell("dream.nell", clock.Tick));
            Assert.IsTrue(book.HasHeard(book.Find("dream.nell")), "heard, so in the Character Notes");
            Assert.IsNull(book.Tell("dream.nell", clock.Tick), "noted once");
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.IsNotNull(asset, path);
            return asset;
        }
    }
}
