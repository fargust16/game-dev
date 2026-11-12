using System;
using System.Linq;
using GameDev.Player.Progression;
using GameDev.Slice.EditorTools;
using GameDev.Slice.People;
using GameDev.World.Seed;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// 10.2 §5a *Roles* (PB2): every role has no Limit or one of 9 to 7 (owner, 2026-11-12); its points over 07.1's
    /// starting attributes stay in the table's range, two thirds of them on the leaning attributes; the seed makes the
    /// same person from the same name; and a role's firearm follows its row.
    /// </summary>
    public sealed class ResidentRolesTests
    {
        private static readonly CharacterAttribute[] Attributes = (CharacterAttribute[])Enum.GetValues(typeof(CharacterAttribute));

        [Test]
        public void EveryRoleHasNoLimitOrALowOne()
        {
            foreach (ResidentRole role in Enum.GetValues(typeof(ResidentRole)))
            {
                for (var i = 0; i < 200; i++)
                {
                    var made = ResidentRoles.Make(role, new Random(i));
                    Assert.IsTrue(made.Limit == null || (made.Limit >= 7 && made.Limit <= 9), $"{role}: Limit {made.Limit}");
                }
            }
        }

        [Test]
        public void ThePointsStayInTheRowAndLeanToItsAttributes()
        {
            foreach (ResidentRole role in Enum.GetValues(typeof(ResidentRole)))
            {
                var row = ResidentRoles.RowOf(role);
                for (var i = 0; i < 200; i++)
                {
                    var sheet = ResidentRoles.Make(role, new Random(i)).Sheet;
                    var points = Attributes.Sum(a => sheet.Get(a) - CharacterSheet.Lowest);
                    Assert.That(points, Is.InRange(row.LeastPoints, row.MostPoints), $"{role}: {points} points");
                    if (row.First is CharacterAttribute first && row.Second is CharacterAttribute second)
                    {
                        var leaning = sheet.Get(first) + sheet.Get(second) - (2 * CharacterSheet.Lowest);
                        Assert.AreEqual((points * 2 + 2) / 3, leaning, $"{role}: two thirds of {points} on {first} and {second}");
                    }
                }
            }
        }

        [Test]
        public void TheSeedMakesTheSamePersonFromTheSameName()
        {
            var seed = new WorldSeed(12345);
            var a = ResidentRoles.Make(ResidentRole.OldPriest, "Old priest", seed);
            var b = ResidentRoles.Make(ResidentRole.OldPriest, "Old priest", seed);
            Assert.AreEqual(a.Limit, b.Limit);
            Assert.AreEqual(a.Firearm, b.Firearm);
            CollectionAssert.AreEqual(Attributes.Select(x => a.Sheet.Get(x)), Attributes.Select(x => b.Sheet.Get(x)));
        }

        /// <summary>The owner's eleven models of 2026-11-12 are named by their roles, and each prefab gives its person that role.</summary>
        [Test]
        public void EveryRoleModelIsNamedByItsRoleAndGivesIt()
        {
            foreach (var look in SlicePeopleModels.Roles)
            {
                StringAssert.EndsWith($"/{look.File}/{look.File}.fbx", look.Model, $"{look.Name}: the model's file is its role's name");
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(look.Model), look.Model);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(look.Standing);
                Assert.IsNotNull(prefab, look.Standing);
                Assert.AreEqual(look.Role, prefab.GetComponent<PersonRole>()?.Role, $"{look.Name}'s role");
                Assert.IsTrue(prefab.GetComponentInChildren<Animator>().isHuman, $"{look.Name} is Humanoid");
            }

            foreach (var (look, role) in new[] { (SlicePeopleModels.Officer, ResidentRole.Warden), (SlicePeopleModels.WardenWoman, ResidentRole.Warden), (SlicePeopleModels.Gentleman, ResidentRole.Townsfolk) })
            {
                Assert.AreEqual(role, AssetDatabase.LoadAssetAtPath<GameObject>(look.Standing).GetComponent<PersonRole>()?.Role, look.Name);
            }
        }

        [Test]
        public void TheFirearmFollowsTheRow()
        {
            var dockers = Enumerable.Range(0, 900).Select(i => ResidentRoles.Make(ResidentRole.Dockworker, new Random(i)).Firearm).ToList();
            Assert.That(dockers.Count(f => f == RoleFirearm.Pistol), Is.InRange(240, 360), "a pistol one dock worker in three");
            Assert.IsTrue(dockers.All(f => f != RoleFirearm.Rifle));
            Assert.AreEqual(RoleFirearm.Rifle, ResidentRoles.Make(ResidentRole.Innkeeper, new Random(1)).Firearm, "the innkeeper's rifle");
            Assert.AreEqual(RoleFirearm.None, ResidentRoles.Make(ResidentRole.Tailor, new Random(1)).Firearm);
            var wardens = Enumerable.Range(0, 900).Select(i => ResidentRoles.Make(ResidentRole.Warden, new Random(i)).Firearm).ToList();
            Assert.IsTrue(wardens.All(f => f != RoleFirearm.None), "every Warden carries a firearm (10.2 §5d)");
            Assert.That(wardens.Count(f => f == RoleFirearm.Rifle), Is.InRange(240, 360), "a rifle one Warden in three");
        }
    }
}
