using GameDev.Confront.Rules;
using GameDev.Items;
using GameDev.Player.Progression;
using GameDev.Slice.Gear;
using NUnit.Framework;
using UnityEditor;

namespace GameDev.Slice.Tests
{
    /// <summary>S5a.2: what each place takes, the hand rules of 10.2 §5b, and a fight's fighter from what is worn and held.</summary>
    public sealed class OutfitTests
    {
        private const string Data = "Assets/Data/Confront/";
        private ItemCatalogue _catalogue;
        private FightRules _rules;
        private FightWeapon _fists;
        private CharacterSheet _sheet;
        private Bag _bag;
        private Outfit _outfit;

        [SetUp]
        public void SetUp()
        {
            _catalogue = AssetDatabase.LoadAssetAtPath<ItemCatalogue>("Assets/Data/Items/Items.asset");
            _rules = AssetDatabase.LoadAssetAtPath<FightRules>(Data + "FightRules.asset");
            _fists = AssetDatabase.LoadAssetAtPath<FightWeapon>(Data + "Weapons/Fists.asset");
            _sheet = new CharacterSheet();
            _sheet.Set(CharacterAttribute.Strength, 20);
            _bag = new Bag(_catalogue, _sheet);
            _outfit = new Outfit(_bag);
        }

        private ItemPiece Give(string id)
        {
            var piece = new ItemPiece(id);
            Assert.IsTrue(_bag.AddPiece(piece), id);
            return piece;
        }

        private T Asset<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(Data + path);

        [Test]
        public void EveryPieceIsLinkedToItsFightNumbers()
        {
            foreach (var item in _catalogue.Items)
            {
                if (item.kind != ItemKind.Equipment)
                {
                    Assert.IsNull(item.fight, item.id);
                    continue;
                }

                switch (item.slot)
                {
                    case EquipSlot.OneHand:
                    case EquipSlot.TwoHands:
                        var weapon = item.fight as FightWeapon;
                        Assert.IsNotNull(weapon, item.id);
                        Assert.AreEqual(item.slot == EquipSlot.TwoHands, weapon.TwoHanded, item.id);
                        break;
                    case EquipSlot.OffHand:
                        Assert.IsInstanceOf<FightOffHand>(item.fight, item.id);
                        break;
                    default:
                        if (string.IsNullOrEmpty(item.family))
                        {
                            Assert.IsNull(item.fight, $"{item.id} gives no defence");
                        }
                        else
                        {
                            Assert.IsInstanceOf<FightArmour>(item.fight, item.id);
                        }

                        break;
                }
            }
        }

        [Test]
        public void EachPieceGoesToItsPlace()
        {
            var clothes = Give("plain_clothes");
            var cap = Give("leather_head");
            Assert.IsNull(_outfit.Wear(clothes));
            Assert.IsNull(_outfit.Wear(cap));
            Assert.AreSame(clothes, _outfit[WearSlot.Body]);
            Assert.AreSame(cap, _outfit[WearSlot.Head]);

            var jerkin = Give("leather_body");
            Assert.IsNull(_outfit.Wear(jerkin));
            Assert.AreSame(jerkin, _outfit[WearSlot.Body], "the body takes one piece");
            Assert.IsFalse(_outfit.IsWorn(clothes), "the plain clothes come off and stay in the bag");
            CollectionAssert.Contains(_bag.Pieces, clothes);

            var rings = new[] { Give("ring"), Give("ring"), Give("ring") };
            foreach (var ring in rings)
            {
                Assert.IsNull(_outfit.Wear(ring));
            }

            Assert.AreSame(rings[2], _outfit[WearSlot.RingOne], "a third ring replaces the first");
            Assert.AreSame(rings[1], _outfit[WearSlot.RingTwo]);
            Assert.IsNull(_outfit.Wear(Give("necklace")));
            Assert.IsNull(_outfit.Wear(Give("belt")));
            Assert.IsNotNull(_outfit[WearSlot.Necklace]);
            Assert.IsNotNull(_outfit[WearSlot.Belt]);
            Assert.AreEqual("ui.bag.fault.not_worn", _outfit.Wear(new ItemPiece("sword")), "only a carried piece is worn");
            Assert.IsNull(_outfit.Toggle(cap));
            Assert.IsFalse(_outfit.IsWorn(cap), "a click on a worn piece takes it off");
        }

        [Test]
        public void TheHandsKeepTheRulesOf10_2()
        {
            var dagger = Give("dagger");
            var shield = Give("metal_shield");
            Assert.IsNull(_outfit.Wear(dagger));
            Assert.AreSame(dagger, _outfit[WearSlot.WeaponOne]);
            Assert.AreEqual("ui.bag.fault.shield", _outfit.Wear(shield), "a shield never goes with a dagger");
            Assert.AreEqual("ui.bag.fault.pouch", _outfit.Wear(Give("cartridge_pouch")), "a pouch goes only with a firearm");

            var sword = Give("sword");
            Assert.IsNull(_outfit.Wear(sword));
            Assert.AreSame(sword, _outfit[WearSlot.WeaponTwo], "the second set takes a weapon while the first holds one");
            Assert.IsNull(_outfit.Wear(shield));
            Assert.AreSame(shield, _outfit[WearSlot.OffHandTwo], "the shield goes beside the sword");

            var greatsword = Give("greatsword");
            Assert.IsNull(_outfit.Wear(greatsword));
            Assert.AreSame(greatsword, _outfit[WearSlot.WeaponOne], "with both sets full a weapon replaces the first set's");
            Assert.IsNull(_outfit.TakeOff(sword));
            Assert.IsNull(_outfit[WearSlot.OffHandTwo], "a weapon taken off takes its shield with it");

            Assert.AreEqual("ui.bag.fault.shield", _outfit.Wear(shield), "nothing goes beside the greatsword, and no other set holds a weapon");
            Assert.IsFalse(_outfit.IsWorn(shield));
        }

        [Test]
        public void AFirearmIsNeverHeldWithoutItsPouch()
        {
            var pistol = Give("pistol");
            Assert.AreEqual("ui.bag.fault.firearm", _outfit.Wear(pistol));
            var pouch = Give("cartridge_pouch");
            Assert.IsNull(_outfit.Wear(pistol));
            Assert.AreSame(pouch, _outfit[WearSlot.OffHandOne], "the firearm takes the pouch with it");

            var rifle = Give("rifle");
            Assert.AreEqual("ui.bag.fault.firearm", _outfit.Wear(rifle), "one pouch holds one firearm, and the second set has none");
            Assert.IsNull(_outfit.TakeOff(pouch));
            Assert.IsFalse(_outfit.IsWorn(pistol), "taking off the pouch takes the firearm off too");
            Assert.IsNull(_outfit.Wear(rifle));
            Assert.AreSame(pouch, _outfit[WearSlot.OffHandOne], "now the rifle holds the pouch");
        }

        [Test]
        public void ThePiecesThatLeaveTheBagAreNoLongerWorn()
        {
            var sword = Give("sword");
            var shield = Give("wooden_shield");
            _outfit.Wear(sword);
            _outfit.Wear(shield);
            Assert.IsTrue(_bag.RemovePiece(sword));
            Assert.IsNull(_outfit[WearSlot.WeaponOne]);
            Assert.IsNull(_outfit[WearSlot.OffHandOne], "the shield cannot stay without its sword");
        }

        [Test]
        public void NothingChangesInAFight()
        {
            var sword = Give("sword");
            _outfit.Locked = () => true;
            Assert.AreEqual("ui.bag.fault.fight", _outfit.Wear(sword));
            _outfit.Locked = null;
            _outfit.Wear(sword);
            _outfit.Locked = () => true;
            Assert.AreEqual("ui.bag.fault.fight", _outfit.TakeOff(sword));
            Assert.IsTrue(_outfit.IsWorn(sword));
        }

        [Test]
        public void WithNoWeaponTheCharacterFightsWithFists()
        {
            var dagger = Asset<FightWeapon>("Weapons/Dagger.asset");
            Assert.AreEqual(dagger.BaseDamage / 2f, _fists.BaseDamage, 1e-5f, "the dagger's numbers at half damage");
            Assert.AreEqual(dagger.AttackAp, _fists.AttackAp);
            Assert.AreEqual(dagger.ToHit, _fists.ToHit);
            Assert.AreEqual(dagger.Critical, _fists.Critical);
            Assert.AreEqual(0, _fists.Requirements.Count);

            var fighter = _outfit.Fighter(_rules, _sheet, _fists);
            Assert.AreSame(_fists, fighter.Weapon);
            Assert.IsTrue(fighter.Second.IsEmpty);
            Assert.IsNull(fighter.Armour);
        }

        [Test]
        public void AFightersGearIsWhatItWearsAndHolds()
        {
            _outfit.Wear(Give("sword"));
            _outfit.Wear(Give("cartridge_pouch"));
            _outfit.Wear(Give("rifle"));
            foreach (var place in new[] { "head", "body", "shoulders", "gloves", "trousers", "boots" })
            {
                Assert.IsNull(_outfit.Wear(Give("leather_" + place)));
            }

            var fighter = _outfit.Fighter(_rules, _sheet, _fists);
            Assert.AreSame(Asset<FightWeapon>("Weapons/Sword.asset"), fighter.Weapon);
            Assert.AreSame(Asset<FightWeapon>("Weapons/Rifle.asset"), fighter.Second.Weapon);
            var whole = _rules.Fighter(_sheet, null, new WeaponSet(fighter.Weapon), Asset<FightArmour>("Armour/Leather.asset"));
            Assert.AreEqual(_rules.PhysicalDefence(whole), _rules.PhysicalDefence(fighter), 1e-4, "the six leather pieces are the whole set");
            Assert.AreEqual(_bag.Weight / _bag.Comfortable, _rules.LoadShare(fighter), 1e-4, "a fight's load is the bag's");

            _sheet.Set(CharacterAttribute.Dexterity, 1);
            var shortOf = _rules.PhysicalDefence(_outfit.Fighter(_rules, _sheet, _fists));
            Assert.AreEqual(8d / 3d, shortOf, 1e-3, "leather asks Dexterity 3, so Dexterity 1 gets a third");
        }

        [Test]
        public void APieceGivesItsShareOfItsFamilysDefence()
        {
            _sheet.Set(CharacterAttribute.Dexterity, 20);
            _sheet.Set(CharacterAttribute.Strength, 20);
            _outfit.Wear(Give("leather_body"));
            _outfit.Wear(Give("plate_head"));
            var armour = _outfit.Armour(_rules, _sheet);
            Assert.AreEqual((8f * 4f / 11.3f) + (24f * 2.8f / 31.8f), armour.Physical, 1e-3f, "each piece by its weight in its family's set");
            Assert.AreEqual(8f * 4f / 11.3f, armour.Magical, 1e-3f, "plate has no magical defence");
            Assert.AreEqual(6.8f, armour.Weight, 1e-4f);
            Assert.AreEqual(1d, _outfit.FitOf(_outfit[WearSlot.Body], _sheet), 1e-6);
        }
    }
}
