using GameDev.Confront.Rules;
using GameDev.Slice.EditorTools;
using GameDev.Slice.Fights;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// 10.2 §5c (WA1): every weapon base and shield a character can carry has its held model, measured to the grip by
    /// <c>SliceWeapons</c>; fists, a Cartridge Pouch and a beast's weapons have none; fists guard and strike as fists.
    /// </summary>
    public sealed class HeldWeaponsTests
    {
        private const string Weapons = "Assets/Data/Confront/Weapons";

        [Test]
        public void EveryWeaponAndShieldHasItsHeldModel()
        {
            var settings = AssetDatabase.LoadAssetAtPath<SliceSettings>(SliceConfrontSetup.SettingsPath);
            var models = settings.WeaponModels;
            Assert.IsNotNull(models, "the settings carry the held models");
            foreach (var name in new[] { "Dagger", "Sword", "Greatsword", "Axe", "Greataxe", "Mace", "Maul", "Pistol", "Rifle" })
            {
                var weapon = AssetDatabase.LoadAssetAtPath<FightWeapon>($"{Weapons}/{name}.asset");
                var held = models.For(weapon);
                Assert.IsNotNull(held, $"{name} has its held model");
                Assert.IsNotNull(held.GetComponentInChildren<Renderer>(), $"{name}'s model draws");
                Assert.IsNull(held.GetComponentInChildren<Collider>(), $"{name}'s model pushes nothing");
            }

            foreach (var name in new[] { "Shield", "WoodenShield" })
            {
                Assert.IsNotNull(models.For(AssetDatabase.LoadAssetAtPath<FightOffHand>($"{Weapons}/{name}.asset")), $"{name} has its held model");
            }

            Assert.IsNull(models.For(AssetDatabase.LoadAssetAtPath<FightOffHand>($"{Weapons}/CartridgePouch.asset")), "a Cartridge Pouch shows none");
            foreach (var name in new[] { "Fists", "Bite", "GlowingBite", "Tusks" })
            {
                Assert.IsNull(models.For(AssetDatabase.LoadAssetAtPath<FightWeapon>($"{Weapons}/{name}.asset")), $"{name} shows none");
            }
        }

        [Test]
        public void FistsGuardAndStrikeAsFists()
        {
            var fists = AssetDatabase.LoadAssetAtPath<FightWeapon>($"{Weapons}/Fists.asset");
            Assert.IsTrue(fists.Unarmed, "fists are no weapon");
            Assert.AreEqual("Guard", FightBeats.Guards[FightBeats.Fists - 1]);
            Assert.AreEqual("Punch", FightBeats.Blows[FightBeats.Fists - 1], "the right's cross");
            Assert.AreEqual("Jab", FightBeats.Blows[FightBeats.FistsLeft - 1], "and the left's jab");
            Assert.AreEqual(FightBeats.PunchContact, FightBeats.ContactOf(FightBeats.Fists));
            Assert.AreEqual(FightBeats.StrikeHold, FightBeats.HoldOf(FightBeats.FistsLeft));
        }
    }
}
