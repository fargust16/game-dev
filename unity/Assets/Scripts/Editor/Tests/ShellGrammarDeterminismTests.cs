using System.Collections.Generic;
using GameDev.World.Buildings.Shell;
using GameDev.World.Settlement;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Buildings.Shell.Tests
{
    public sealed class ShellGrammarDeterminismTests
    {
        [Test]
        public void SameSeed_ProducesIdenticalPieceFingerprints()
        {
            var a = GenerateStrip(42, DistrictKind.Worker, 12);
            var b = GenerateStrip(42, DistrictKind.Worker, 12);
            Assert.AreEqual(a.Count, b.Count);
            for (var i = 0; i < a.Count; i++)
                Assert.AreEqual(Fingerprint(a[i]), Fingerprint(b[i]), $"Mismatch at plot {i}");
        }

        [Test]
        public void DifferentDistricts_ProduceDifferentAverageCharacter()
        {
            var poor = GenerateStrip(42, DistrictKind.Poor, 10);
            var rich = GenerateStrip(42, DistrictKind.Rich, 10);
            Assert.Greater(AvgWealth(rich), AvgWealth(poor));
            Assert.Greater(AvgFloors(rich), AvgFloors(poor) - 0.01f);
            Assert.Greater(AvgTrim(rich), AvgTrim(poor));
        }

        [Test]
        public void Elite_PrefersNonBoxMassing()
        {
            var shells = GenerateStrip(77, DistrictKind.Elite, 12);
            var nonBox = 0;
            for (var i = 0; i < shells.Count; i++)
            {
                if (shells[i].Dna.Massing != MassingType.Box)
                    nonBox++;
            }

            Assert.Greater(nonBox, shells.Count / 2, "Elite should mostly use L/Stepped/RearWing.");
        }

        [Test]
        public void FloorStack_SameFloorSlabsDoNotOverlap_AndSingleGroundDoorBay()
        {
            var kinds = new[]
            {
                MassingType.Box, MassingType.LShape, MassingType.Stepped, MassingType.RearWing
            };
            for (var k = 0; k < kinds.Length; k++)
            {
                var dna = BuildingDna.CreateDefault("Townhouse", 11, k);
                dna.FloorCount = 3;
                dna.SecondaryFloors = 2;
                dna.Massing = kinds[k];
                var plot = PlotFootprint.Rect(4, 3, Vector3.zero);
                var stack = ShellMassingBuilder.BuildFloorStack(plot, dna);
                Assert.Greater(stack.FloorCount, 0, kinds[k].ToString());

                var doors = 0;
                for (var f = 0; f < stack.FloorCount; f++)
                {
                    var slabs = stack.SlabsByFloor[f];
                    Assert.AreEqual(
                        1,
                        slabs.Count,
                        $"{kinds[k]} floor {f} must be a single footprint (true floor-stack).");
                    if (slabs[0].DoorBay >= 0)
                        doors++;
                }

                Assert.AreEqual(1, doors, $"{kinds[k]} must have exactly one ground door bay.");
                if (kinds[k] != MassingType.Box)
                {
                    var ground = stack.SlabsByFloor[0][0];
                    var top = stack.SlabsByFloor[stack.FloorCount - 1][0];
                    Assert.Less(
                        top.WidthMeters * top.DepthMeters + 0.01f,
                        ground.WidthMeters * ground.DepthMeters,
                        $"{kinds[k]} upper footprint must shrink vs ground.");
                }
            }
        }

        [Test]
        public void AssembledShell_HasZeroIllegalPieceOverlaps_AllMassingTypes()
        {
            var kinds = new[]
            {
                MassingType.Box, MassingType.LShape, MassingType.Stepped, MassingType.RearWing
            };
            var district = ShellCatalogFactory.CreateDistrict(DistrictKind.Elite);
            var arch = ShellCatalogFactory.CreateDefaultArchetypes().Find(a => a.Id == "NobleHouse");
            var street = StreetContext.StreetFrontYardBack();
            var binder = new BuildingShellBinder();

            for (var k = 0; k < kinds.Length; k++)
            {
                for (var seed = 0; seed < 8; seed++)
                {
                    var plot = PlotFootprint.Rect(5, 4, Vector3.zero);
                    var dna = ShellDnaSampler.Sample(
                        arch, district, plot, street, 200 + seed, seed, new System.Random(200 + seed));
                    dna.Massing = kinds[k];
                    dna.FloorCount = 3;
                    dna.SecondaryFloors = 2;
                    dna.ChimneyCount = Mathf.Max(1, Mathf.Min(dna.ChimneyCount, 2));
                    var shell = binder.Bind(dna, plot, street, arch, district);
                    var stack = ShellMassingBuilder.BuildFloorStack(plot, dna);
                    var report = ShellOverlapGuard.Validate(shell, stack);
                    Assert.IsTrue(report.Ok, $"{kinds[k]} seed={seed}: {report.Error}");
                    AssertChimneysFlushOnRoof(shell);
                }
            }
        }

        [Test]
        public void EliteStrip_AssembledShells_PassOverlapGuard()
        {
            var shells = GenerateStrip(901, DistrictKind.Elite, 10);
            for (var i = 0; i < shells.Count; i++)
            {
                var shell = shells[i];
                var stack = ShellMassingBuilder.BuildFloorStack(shell.Plot, shell.Dna);
                var report = ShellOverlapGuard.Validate(shell, stack);
                Assert.IsTrue(report.Ok, $"Elite plot {i} massing={shell.Dna.Massing}: {report.Error}");
                AssertChimneysFlushOnRoof(shell);
            }
        }

        [Test]
        public void AssembledShell_PlacesVisibleFacadeOpenings()
        {
            var district = ShellCatalogFactory.CreateDistrict(DistrictKind.Worker);
            var arch = ShellCatalogFactory.CreateDefaultArchetypes().Find(a => a.Id == "Townhouse");
            var plot = PlotFootprint.Rect(4, 3, Vector3.zero);
            var street = StreetContext.StreetFrontYardBack();
            var dna = BuildingDna.CreateDefault("Townhouse", 12, 0);
            dna.FloorCount = 2;
            dna.Massing = MassingType.Box;
            dna.DistrictId = DistrictIds.Worker;
            var shell = new BuildingShellBinder().Bind(dna, plot, street, arch, district);
            var windows = 0;
            var doors = 0;
            for (var i = 0; i < shell.Pieces.Count; i++)
            {
                if (shell.Pieces[i].Category == ExteriorPieceCategory.Window)
                    windows++;
                if (shell.Pieces[i].Category == ExteriorPieceCategory.Door
                    || shell.Pieces[i].Category == ExteriorPieceCategory.Shopfront)
                    doors++;
            }

            Assert.Greater(windows, 0, "Expected facade windows on a box townhouse.");
            Assert.AreEqual(1, doors, "Expected one ground door/shop.");
        }

        [Test]
        public void StepFaces_UseAtticWindows_NotStandardWindows()
        {
            var district = ShellCatalogFactory.CreateDistrict(DistrictKind.Elite);
            var arch = ShellCatalogFactory.CreateDefaultArchetypes().Find(a => a.Id == "NobleHouse");
            var plot = PlotFootprint.Rect(5, 4, Vector3.zero);
            var street = StreetContext.StreetFrontYardBack();
            var dna = BuildingDna.CreateDefault("NobleHouse", 44, 0);
            dna.Massing = MassingType.LShape;
            dna.FloorCount = 3;
            dna.SecondaryFloors = 2;
            dna.DistrictId = DistrictIds.Elite;
            var shell = new BuildingShellBinder().Bind(dna, plot, street, arch, district);
            var attic = 0;
            for (var i = 0; i < shell.Pieces.Count; i++)
            {
                if (shell.Pieces[i].Category == ExteriorPieceCategory.AtticWindow)
                    attic++;
            }

            Assert.Greater(attic, 0, "LShape step face should place attic windows above the lower wing roof.");
        }

        [Test]
        public void AssembledShell_HasSingleGroundStreetDoor()
        {
            var district = ShellCatalogFactory.CreateDistrict(DistrictKind.Elite);
            var arch = ShellCatalogFactory.CreateDefaultArchetypes().Find(a => a.Id == "NobleHouse");
            var plot = PlotFootprint.Rect(5, 4, Vector3.zero);
            var street = StreetContext.StreetFrontYardBack();
            var dna = ShellDnaSampler.Sample(
                arch, district, plot, street, 99, 0, new System.Random(99));
            dna.Massing = MassingType.LShape;
            var shell = new BuildingShellBinder().Bind(dna, plot, street, arch, district);
            var doors = 0;
            for (var i = 0; i < shell.Pieces.Count; i++)
            {
                var p = shell.Pieces[i];
                if ((p.Category == ExteriorPieceCategory.Door || p.Category == ExteriorPieceCategory.Shopfront)
                    && p.LocalPosition.y < ModularGrid.FoundationHeight + ModularGrid.StoreyMeters)
                {
                    doors++;
                }
            }

            Assert.AreEqual(1, doors, "Ground floor must have exactly one door/shop opening.");
        }

        private static bool AabbOverlapXZ(FloorSlab a, FloorSlab b)
        {
            return a.LocalX < b.MaxX - 0.001f
                   && a.MaxX > b.LocalX + 0.001f
                   && a.LocalZ < b.MaxZ - 0.001f
                   && a.MaxZ > b.LocalZ + 0.001f;
        }

        private static void AssertChimneysFlushOnRoof(ShellInstance shell)
        {
            float roofTop = float.NegativeInfinity;
            for (var i = 0; i < shell.Pieces.Count; i++)
            {
                var p = shell.Pieces[i];
                if (p.Category != ExteriorPieceCategory.RoofFlat && p.Category != ExteriorPieceCategory.RoofGable)
                    continue;
                var top = p.LocalPosition.y + p.LocalScale.y * 0.5f;
                if (top > roofTop)
                    roofTop = top;
            }

            for (var i = 0; i < shell.Pieces.Count; i++)
            {
                var p = shell.Pieces[i];
                if (p.Category != ExteriorPieceCategory.Chimney)
                    continue;
                var bottom = p.LocalPosition.y - p.LocalScale.y * 0.5f;
                Assert.AreEqual(
                    roofTop,
                    bottom,
                    0.05f,
                    $"Chimney {p.PieceId} must sit flush on roof (bottom={bottom}, roofTop={roofTop}).");
            }
        }

        [Test]
        public void Commercial_HasHighShopShare()
        {
            var shells = GenerateStrip(110201, DistrictKind.Commercial, 16);
            var shops = 0;
            for (var i = 0; i < shells.Count; i++)
            {
                if (shells[i].Dna.ArchetypeId == "Shop")
                    shops++;
            }

            Assert.GreaterOrEqual(shops, shells.Count / 3, "Commercial should place many shops.");
        }

        [Test]
        public void ShopArchetype_PlacesStreetDoorOrShopfrontOnGround()
        {
            var archetypes = ShellCatalogFactory.CreateDefaultArchetypes();
            BuildingArchetypeDef shop = null;
            for (var i = 0; i < archetypes.Count; i++)
            {
                if (archetypes[i].Id == "Shop")
                    shop = archetypes[i];
            }

            Assert.IsNotNull(shop);
            var district = ShellCatalogFactory.CreateDistrict(DistrictKind.Commercial);
            var plot = PlotFootprint.Rect(3, 2, Vector3.zero);
            var street = StreetContext.StreetFrontYardBack();
            var dna = BuildingDna.CreateDefault("Shop", 7, 0);
            dna.FloorCount = 2;
            dna.InteriorPolicy = InteriorPolicy.Portal;
            dna.RoofType = RoofType.Flat;
            dna.DistrictId = DistrictIds.Commercial;
            var shell = new BuildingShellBinder().Bind(dna, plot, street, shop, district);
            var found = false;
            for (var i = 0; i < shell.Pieces.Count; i++)
            {
                var p = shell.Pieces[i];
                if ((p.Category == ExteriorPieceCategory.Door || p.Category == ExteriorPieceCategory.Shopfront)
                    && Mathf.Abs(p.LocalPosition.z) < 0.2f
                    && p.LocalPosition.y < ModularGrid.FoundationHeight + ModularGrid.StoreyMeters)
                {
                    found = true;
                    break;
                }
            }

            Assert.IsTrue(found, "Expected street-facing ground door/shopfront.");
            Assert.IsTrue(shell.HasPortal);
        }

        [Test]
        public void SettlementBridge_BindsDeterministically()
        {
            var archetypes = ShellCatalogFactory.CreateDefaultArchetypes();
            var districts = ShellCatalogFactory.CreateDefaultDistricts();
            var binder = new BuildingShellBinder();
            var bridge = new SettlementShellBridge(binder, archetypes, districts);
            var layout = SettlementLayoutStub.CreateDemo(110201, 6);
            var a = bridge.BindAll(layout, 110201);
            var b = bridge.BindAll(layout, 110201);
            Assert.AreEqual(a.Count, b.Count);
            Assert.Greater(a.Count, 0);
            for (var i = 0; i < a.Count; i++)
                Assert.AreEqual(Fingerprint(a[i]), Fingerprint(b[i]));
        }

        private static List<ShellInstance> GenerateStrip(int seed, DistrictKind kind, int count)
        {
            var archetypes = ShellCatalogFactory.CreateDefaultArchetypes();
            var district = ShellCatalogFactory.CreateDistrict(kind);
            var selector = new ShellArchetypeSelector(archetypes);
            var binder = new BuildingShellBinder();
            var ws = new GameDev.World.Seed.WorldSeed(seed);
            var list = new List<ShellInstance>(count);
            var x = 0f;
            for (var i = 0; i < count; i++)
            {
                var plotRng = ws.Fork($"plot_{district.Id}_{i}");
                var plot = PlotFootprint.Rect(
                    district.RollWidthBays(plotRng),
                    district.RollDepthBays(plotRng),
                    new Vector3(x, 0f, 0f));
                var street = i % district.CornerEveryN == district.CornerEveryN - 1
                    ? StreetContext.CornerFrontLeft()
                    : StreetContext.StreetFrontYardBack();
                if (street.Left == FaceKind.Street)
                    plot = PlotFootprint.Corner(plot.WidthBays, plot.DepthBays, plot.Origin);
                var arch = selector.Select(plot, district, ws.Fork($"arch_{district.Id}_{i}"));
                var dna = ShellDnaSampler.Sample(
                    arch, district, plot, street, seed, i, ws.Fork($"dna_{district.Id}_{i}"));
                list.Add(binder.Bind(dna, plot, street, arch, district));
                x += plot.WidthMeters + district.EffectiveGapMeters();
            }

            return list;
        }

        private static float AvgWealth(List<ShellInstance> shells)
        {
            var s = 0f;
            for (var i = 0; i < shells.Count; i++)
                s += shells[i].Dna.Wealth;
            return s / shells.Count;
        }

        private static float AvgFloors(List<ShellInstance> shells)
        {
            var s = 0f;
            for (var i = 0; i < shells.Count; i++)
                s += shells[i].Dna.FloorCount;
            return s / shells.Count;
        }

        private static float AvgTrim(List<ShellInstance> shells)
        {
            var s = 0f;
            for (var i = 0; i < shells.Count; i++)
                s += shells[i].Dna.TrimLevel;
            return s / shells.Count;
        }

        private static string Fingerprint(ShellInstance shell)
        {
            var h = shell.Dna.ArchetypeId.GetHashCode();
            h = (h * 397) ^ shell.Dna.DistrictId.GetHashCode();
            h = (h * 397) ^ shell.Dna.FloorCount;
            h = (h * 397) ^ shell.Dna.Wealth;
            h = (h * 397) ^ (int)shell.Dna.RoofType;
            h = (h * 397) ^ shell.Dna.ChimneyCount;
            h = (h * 397) ^ shell.Dna.TrimLevel;
            h = (h * 397) ^ (int)shell.Dna.Massing;
            h = (h * 397) ^ shell.Pieces.Count;
            for (var i = 0; i < shell.Pieces.Count; i++)
            {
                var p = shell.Pieces[i];
                h = (h * 397) ^ (int)p.Category;
                h = (h * 397) ^ Mathf.RoundToInt(p.LocalPosition.x * 100f);
                h = (h * 397) ^ Mathf.RoundToInt(p.LocalPosition.y * 100f);
                h = (h * 397) ^ Mathf.RoundToInt(p.LocalPosition.z * 100f);
            }

            return h.ToString("X8");
        }
    }
}
