#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Slice.Beasts;
using GameDev.Slice.Fights;
using GameDev.Slice.Gathering;
using GameDev.Slice.Gear;
using GameDev.Slice.Shops;
using GameDev.Slice.Theft;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5a.9, S5a's part of S5's acceptance through the real scene: a new character with 11's start steals, buys, gathers,
    /// sells, learns and crafts, binds a wound, and fights a wolf pack with what it carries.
    /// </summary>
    public sealed class S5aAcceptancePlayTests : SliceInputTestBase
    {
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
        public IEnumerator ANewCharacterEarnsBuysGathersCraftsStealsBindsAndFights()
        {
            yield return ForgetTheWorld();
            yield return LoadScene("Assets/Scenes/Slice/Veilmouth.unity");
            var world = SliceWorldDriver.Running;
            if (world.Clock.MinuteOfDay < 9 * 60)
            {
                world.Clock.AdvanceTicks(9 * 60 - world.Clock.MinuteOfDay);
            }

            // 11's start: plain clothes and 500 bronze, nothing else.
            var bag = world.Bag;
            Assert.AreEqual(500, bag.Bronze);
            Assert.AreEqual(1, bag.Pieces.Count);
            Assert.AreEqual("plain_clothes", bag.Pieces[0].item);
            Assert.IsTrue(world.Outfit.IsWorn(bag.Pieces[0]));
            Assert.IsEmpty(bag.Stacks, "no supplies, tools or resources");

            // A deft, strong body: S5b's character creation will give its values.
            var character = world.Character;
            character.Set(CharacterAttribute.Strength, 16);
            character.Set(CharacterAttribute.Endurance, 14);
            character.Set(CharacterAttribute.Dexterity, 30);
            character.Set(CharacterAttribute.Perception, 5);
            character.Set(CharacterSkill.MeleeSkill, 14);
            character.Set(CharacterSkill.Theft, 30);
            character.Set(CharacterSkill.Stealth, 30);
            character.Set(CharacterSkill.FirstAid, 30);
            character.Set(CharacterSkill.Woodcutting, 30);

            // Steals: a purse lifted by hand, then the rest of the walkers' purses.
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var targets = Object.FindObjectsByType<PurseTarget>(FindObjectsInactive.Exclude);
            PurseTarget first = null;
            yield return TheftPlayTests.Choose(targets, null, player, t => first = t);
            Assert.IsNotNull(first);
            yield return Tap(Key.E);
            Assert.IsTrue(world.Reaction.Running);
            var bar = world.Reaction.Bar;
            for (var t = 0f; t < 3f && world.Reaction.Marker < bar.GoodStart + (bar.GoodWidth * 0.3d); t += Time.deltaTime)
            {
                yield return null;
            }

            yield return Tap(Key.Space);
            Assert.Greater(bag.Bronze, 500, "the first purse lifted");
            world.Hiding.SetActive(false);
            foreach (var target in targets.Where(t => t != first))
            {
                if (bag.Bronze >= 2800)
                {
                    break;
                }

                world.Theft.Resolve(target.Person, ReactionResult.Good, out _);
            }

            Assert.GreaterOrEqual(bag.Bronze, 2800, "enough stolen for the day's buying");

            // Buys: the axe and iron ore from Edda Brann, a bandage from Ada Merrow, waiting a day for ore if the shelf is short.
            var shops = world.Shops;
            for (var day = 0; day < 5 && shops.Shelf("edda").Where(s => s.Item.id == "iron_ore").Sum(s => s.Count) < 2; day++)
            {
                world.Clock.AdvanceTicks((24 * 60) - world.Clock.MinuteOfDay + (9 * 60));
            }

            Assert.IsNull(shops.Buy("edda", shops.Shelf("edda").First(s => s.Item.id == "woodcutting_axe")), "the axe");
            for (var i = 0; i < 2; i++)
            {
                Assert.IsNull(shops.Buy("edda", shops.Shelf("edda").First(s => s.Item.id == "iron_ore" && s.Count > 0)), "iron ore");
            }

            Assert.IsNull(shops.Buy("ada", shops.Shelf("ada").First(s => s.Item.id == "bandage")), "a bandage");
            Assert.AreEqual((1, 2, 1), (bag.Count("woodcutting_axe"), bag.Count("iron_ore"), bag.Count("bandage")));

            // Gathers: beside the tree farthest from every beast, E fells it, a cycle a tick, until it is gone.
            var homes = world.Beasts.Beasts.Select(b => b.Home).ToList();
            var tree = Object.FindObjectsByType<GatherNode>(FindObjectsInactive.Exclude).Where(n => n.Spot.Rule.kind == GatherKind.Tree && n.Shown)
                .OrderByDescending(n => homes.Min(h => Vector3.Distance(h, n.transform.position))).First();
            Assert.IsTrue(GatheringPlayTests.Stand(tree, out var by), "room beside the tree");
            var trunk = tree.transform.position - by;
            player.Teleport(by, Quaternion.LookRotation(new Vector3(trunk.x, 0f, trunk.z)));
            yield return new WaitForSeconds(0.5f);
            yield return Tap(Key.E);
            Assert.IsTrue(tree.IsActive, "E begins felling");
            for (var i = 0; i < 40 && tree.Shown; i++)
            {
                world.Clock.AdvanceTicks(1);
                yield return GatheringPlayTests.StopInZone(world.Reaction);
            }

            Assert.IsFalse(tree.Shown, "the tree felled");
            Assert.AreEqual(6, bag.Count("wood"));

            // Earns: sells two wood back to Edda Brann.
            var bronze = bag.Bronze;
            var wood = bag.Catalogue.Find("wood");
            Assert.IsNull(shops.Sell("edda", wood, null));
            Assert.IsNull(shops.Sell("edda", wood, null));
            Assert.AreEqual(bronze + (2 * SliceShops.SalePrice(wood, null)), bag.Bronze, "two wood sold at a quarter");

            // Learns and crafts: Item Creation from Edda Brann, then a dagger at her workshop.
            Assert.IsNull(shops.Learn("edda", SliceWorkshop.Lesson));
            var dagger = bag.Catalogue.Find("dagger");
            Assert.AreEqual(CraftOutcome.Made, world.Workshop.Make("edda", dagger, 9, null, ReactionResult.Good, out var piece));
            Assert.AreEqual((0, 3), (bag.Count("iron_ore"), bag.Count("wood")), "its iron ore and a wood used");

            // Holds it: the fight's fighter is made from what the character wears and holds.
            Assert.IsNull(world.Outfit.Wear(piece));
            Assert.AreSame(world.Outfit[WearSlot.WeaponOne], piece);
            var fights = world.Fights;
            Assert.AreSame(dagger.fight, fights.PlayerFighter().Weapon, "it fights with the dagger it made");

            // Binds a wound: Bleeding, and B with the bandage it bought.
            // A failed check keeps the bandage, so it is tried again.
            world.State.Set(SliceConfront.BleedingKey, 1);
            for (var i = 0; i < 6 && world.State.IsSet(SliceConfront.BleedingKey); i++)
            {
                Assert.IsTrue(fights.TreatOutside());
            }

            Assert.IsFalse(world.State.IsSet(SliceConfront.BleedingKey), "the bandage stops the bleeding");
            Assert.AreEqual(0, bag.Count("bandage"), "and is used");

            // Fights a wolf pack with what it carries, and the pack breaks.
            var beasts = world.Beasts;
            var wolves = beasts.Beasts.Where(b => b.Kind == BeastKind.Wolf && b.isActiveAndEnabled && b.State == BeastState.Resting && !beasts.GlowingPacks.Contains(b.Pack))
                .GroupBy(b => b.Pack).OrderBy(g => g.Count()).First().ToList();
            fights.TurnPause = 0f;
            fights.AutoPlayer = true;
            fights.BeatPace = 6f;
            Assert.IsTrue(BeastPlayTests.Spot(wolves, out var at), "a place in the territory");
            player.Teleport(at + (Vector3.up * 0.05f), Quaternion.LookRotation(new Vector3(wolves[0].Home.x - at.x, 0f, wolves[0].Home.z - at.z)));
            for (var t = 0f; t < 20f && !fights.InFight; t += Time.deltaTime)
            {
                yield return null;
                if (beasts.HasNoticed(wolves[0].Pack) && !fights.InFight)
                {
                    world.Clock.AdvanceTicks(1);
                }
            }

            Assert.IsTrue(fights.InFight, "the pack attacks");
            Assert.AreSame(dagger.fight, fights.PlayerCombatant.Fighter.Weapon, "the character holds its dagger in the fight");
            for (var t = 0f; t < 120f && fights.InFight; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsFalse(fights.InFight);
            Assert.IsTrue(wolves.Any(w => w.State == BeastState.Dead), "a wolf falls and the pack breaks");
            Assert.IsFalse(fights.Dead, "and the character lives");
        }
    }
}
#endif
