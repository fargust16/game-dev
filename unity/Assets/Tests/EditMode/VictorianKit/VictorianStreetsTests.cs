using System.Collections.Generic;
using System.Linq;
using GameDev.VictorianKit.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.VictorianKit.Tests
{
    /// <summary>The checks of Plans/VICTORIAN_STREETS_PLAN.md §8: the assembler against House U, brick seams, street rules.</summary>
    public sealed class VictorianStreetsTests
    {
        private static IEnumerable<VicHouseSpec> AllHouses() => VicHousePlans.RichStreet().Concat(VicHousePlans.PoorStreet());

        [Test]
        public void HouseU_EqualsTheKitsManifestPieceForPiece()
        {
            var manifest = VicManifest.Load(VicKitPaths.KitManifestFolder + "/House_U_Modular.json").Pieces().ToList();
            var mine = VicHousePlans.HouseU().Pieces;
            Assert.That(VicHousePlans.Missing(manifest, mine), Is.Empty, "pieces of the manifest the assembler leaves out");
            Assert.That(VicHousePlans.Missing(mine, manifest), Is.Empty, "pieces the assembler adds");
        }

        [Test]
        public void HouseU_BrickJointsMeetAtEveryModuleSeam()
        {
            if (VicKitPaths.LoadModule("SM_Vic_Wall_Plain_2m") == null)
            {
                Assert.Ignore("The kit's prefabs are not built (Victorian Kit / Build Prefabs).");
            }

            var house = VicHouseFactory.Build("House_U_SeamTest", VicHousePlans.HouseU().Pieces);
            try
            {
                var result = VicSeamCheck.Run(house);
                Assert.That(result.Faces, Is.GreaterThan(500), "outline brick faces measured");
                Assert.That(result.Broken, Is.Zero, result.ToString());
            }
            finally
            {
                Object.DestroyImmediate(house);
            }
        }

        [Test]
        public void EveryPieceIsAModuleTheKitShips()
        {
            var ids = new HashSet<string>(VicKitPaths.ModuleIds());
            foreach (var house in AllHouses())
            {
                var unknown = house.House.Pieces.Select(p => p.Id).Where(id => !ids.Contains(id)).Distinct().ToList();
                Assert.That(unknown, Is.Empty, house.Name);
            }
        }

        [Test]
        public void RichStreet_HasThreeDifferentComplexPlansUnderOneMansardEach()
        {
            var rich = VicHousePlans.RichStreet();
            Assert.That(rich.Count, Is.EqualTo(3));
            Assert.That(rich.Select(h => h.PlanKind).Distinct().Count(), Is.EqualTo(3));
            foreach (var h in rich)
            {
                Assert.That(h.Floors, Is.EqualTo(3), h.Name);
                Assert.That(h.Roof, Is.EqualTo("mansard"), h.Name);
                var ids = h.House.Pieces.Select(p => p.Id).ToList();
                Assert.That(ids, Has.Some.StartsWith("SM_Vic_Quoin_Out"), h.Name + " quoins");
                Assert.That(ids, Has.Some.StartsWith("SM_Vic_String_"), h.Name + " string courses");
                Assert.That(ids, Has.Some.StartsWith("SM_Vic_Cornice_"), h.Name + " cornice");
                Assert.That(ids, Has.Some.EqualTo("SM_Vic_Wall_Window_Arch_Single_2m"), h.Name + " arched top windows");
                Assert.That(ids, Has.Some.StartsWith("SM_Vic_Balcony_Stone"), h.Name + " stone balcony");
            }

            Assert.That(rich.Count(h => h.House.Pieces.Any(p => p.Id == "SM_Vic_Column_3m")), Is.GreaterThanOrEqualTo(1), "a porch with columns");
            Assert.That(rich.Count(h => h.House.Pieces.Any(p => p.Id == "SM_Vic_Roof_Oriel_Cap")), Is.EqualTo(1), "one oriel house");
        }

        [Test]
        public void StuccoOnlyOnTheRichStreet()
        {
            Assert.That(VicHousePlans.PoorStreet().SelectMany(h => h.House.Pieces), Has.None.Matches<VicPiece>(p => p.Stucco));
            Assert.That(VicHousePlans.PoorStreet().Select(h => h.Palette.Stucco), Has.All.Null);
        }

        [Test]
        public void PoorStreet_NeighboursAreNeverCopies()
        {
            var poor = VicHousePlans.PoorStreet();
            Assert.That(poor.Count, Is.GreaterThanOrEqualTo(6));
            for (var i = 1; i < poor.Count; i++)
            {
                var a = poor[i - 1];
                var b = poor[i];
                Assert.That(b.PlanKind, Is.Not.EqualTo(a.PlanKind), $"{a.Name}/{b.Name} plan");
                Assert.That(b.Ridge, Is.Not.EqualTo(a.Ridge).Within(0.05), $"{a.Name}/{b.Name} ridge");
                Assert.That(b.Palette.Key, Is.Not.EqualTo(a.Palette.Key), $"{a.Name}/{b.Name} palette");
            }

            Assert.That(poor.Select(h => h.PlanKind).Distinct().Count(), Is.EqualTo(poor.Count), "no plan repeats, so no mirrored repeat");
            // Distinct: NUnit's SubsetOf counts repeats, and fronts repeat along the street.
            Assert.That(poor.Select(h => h.Width).Distinct(), Is.SubsetOf(new[] { 6, 8, 10, 12 }));
            Assert.That(poor.Select(h => h.Depth).Distinct(), Is.SubsetOf(new[] { 4, 6 }));
            Assert.That(poor.Select(h => h.Floors).Distinct(), Is.EquivalentTo(new[] { 2, 3 }));
            Assert.That(poor.Select(h => h.Roof).Distinct(), Is.EquivalentTo(new[] { "gable", "hip" }));
            Assert.That(poor.Count(h => h.House.Pieces.Any(p => p.Id.StartsWith("SM_Vic_Quoin"))), Is.LessThan(poor.Count), "quoins not on every house");
        }

        [Test]
        public void Streets_StandOnTheTwoMetreGridWithGaps()
        {
            foreach (var street in new[] { VicHousePlans.RichStreet(), VicHousePlans.PoorStreet() })
            {
                for (var i = 0; i < street.Count; i++)
                {
                    Assert.That(street[i].X % 2, Is.Zero, street[i].Name);
                    if (i > 0)
                    {
                        Assert.That(street[i].X - (street[i - 1].X + street[i - 1].Width), Is.GreaterThanOrEqualTo(0.6), street[i].Name);
                    }
                }
            }
        }

        [Test]
        public void Roofs_KeepTheKitsRules()
        {
            foreach (var h in AllHouses())
            {
                var ids = h.House.Pieces.Select(p => p.Id).ToList();
                Assert.That(ids, Has.None.Contains("Plinth"), h.Name);
                Assert.That(ids, Has.None.EqualTo("SM_Vic_Chimney_1"), h.Name);
                Assert.That(ids, Has.None.EqualTo("SM_Vic_Roof_Valley_2m"), h.Name);
                var spring = VicKit.Spring(h.Floors);
                foreach (var p in h.House.Pieces.Where(p => p.Id == "SM_Vic_Chimney_4"))
                {
                    Assert.That(p.Z, Is.EqualTo(spring + VicKit.MansardRise).Within(1e-6), h.Name + " Chimney_4 on the flat");
                }

                foreach (var p in h.House.Pieces.Where(p => p.Id == "SM_Vic_Chimney_2"))
                {
                    Assert.That(p.Y, Is.LessThan((h.Depth * 0.5) - 0.6), h.Name + " Chimney_2 off the ridge");
                }

                foreach (var p in h.House.Pieces.Where(p => p.Id.StartsWith("SM_Vic_Lucarne")))
                {
                    var lift = p.Z - spring;
                    Assert.That(
                        Mathf.Abs((float)(lift - (0.40 * VicKit.Tan))) < 1e-4f || Mathf.Abs((float)(lift - (0.05 * VicKit.Tan))) < 1e-4f,
                        Is.True,
                        h.Name + " lucarne at the proof houses' offsets");
                }
            }
        }
    }
}
