using System.Collections.Generic;
using GameDev.World.Buildings.Shell;
using GameDev.World.Poi;
using GameDev.World.Settlement;
using GameDev.World.Terrain;
using GameDev.World.Water;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Settlement.Tests
{
    public sealed class SettlementPropertyParcelTests
    {
        [Test]
        public void Create_SameSeed_IsDeterministic()
        {
            var a = SettlementBlockLayout.Create(110201, DistrictKind.Worker, 8);
            var b = SettlementBlockLayout.Create(110201, DistrictKind.Worker, 8);
            Assert.AreEqual(a.Properties.Count, b.Properties.Count);
            for (var i = 0; i < a.Properties.Count; i++)
            {
                Assert.AreEqual(Fingerprint(a.Properties[i]), Fingerprint(b.Properties[i]));
                Assert.AreEqual(a.Plots[i].Footprint.Origin, b.Plots[i].Footprint.Origin);
            }
        }

        [Test]
        public void Create_BuildingPad_IsInsideParcel()
        {
            foreach (DistrictKind kind in System.Enum.GetValues(typeof(DistrictKind)))
            {
                var layout = SettlementBlockLayout.Create(42, kind, 6);
                for (var i = 0; i < layout.Properties.Count; i++)
                {
                    var prop = layout.Properties[i];
                    Assert.IsTrue(
                        prop.Parcel.ContainsPad(prop.BuildingPad),
                        $"{kind} plot {i}: pad outside parcel");
                }
            }
        }

        [Test]
        public void Create_Affluent_HasFrontGardenZone()
        {
            var layout = SettlementBlockLayout.Create(99, DistrictKind.Affluent, 5);
            Assert.Greater(layout.Properties.Count, 0);
            for (var i = 0; i < layout.Properties.Count; i++)
            {
                Assert.IsTrue(
                    HasZone(layout.Properties[i], PropertyZoneKind.FrontGarden),
                    $"Affluent plot {i} missing FrontGarden");
                Assert.IsTrue(
                    HasZone(layout.Properties[i], PropertyZoneKind.RearYard),
                    $"Affluent plot {i} missing RearYard");
            }
        }

        [Test]
        public void Create_Poor_PadMatchesParcelDepth()
        {
            var layout = SettlementBlockLayout.Create(7, DistrictKind.Poor, 6);
            for (var i = 0; i < layout.Properties.Count; i++)
            {
                var prop = layout.Properties[i];
                Assert.AreEqual(prop.Parcel.DepthBays, prop.BuildingPad.DepthBays);
                Assert.AreEqual(prop.Parcel.WidthBays, prop.BuildingPad.WidthBays);
                Assert.AreEqual(prop.Parcel.Origin, prop.BuildingPad.Origin);
                Assert.IsFalse(HasZone(prop, PropertyZoneKind.FrontGarden));
                Assert.IsFalse(HasZone(prop, PropertyZoneKind.RearYard));
            }
        }

        [Test]
        public void Create_Worker_HasRearYard()
        {
            var layout = SettlementBlockLayout.Create(11, DistrictKind.Worker, 6);
            for (var i = 0; i < layout.Properties.Count; i++)
                Assert.IsTrue(HasZone(layout.Properties[i], PropertyZoneKind.RearYard));
        }

        [Test]
        public void Plots_ProjectBuildingPads()
        {
            var layout = SettlementBlockLayout.Create(3, DistrictKind.Affluent, 4);
            Assert.AreEqual(layout.Properties.Count, layout.Plots.Count);
            for (var i = 0; i < layout.Plots.Count; i++)
            {
                Assert.AreEqual(layout.Properties[i].BuildingPad.Origin, layout.Plots[i].Footprint.Origin);
                Assert.AreEqual(layout.Properties[i].BuildingPad.WidthBays, layout.Plots[i].Footprint.WidthBays);
                Assert.AreEqual(layout.Properties[i].DistrictId, layout.Plots[i].DistrictId);
            }
        }

        [Test]
        public void Bridge_BindProperties_IsDeterministicAndPassesOverlap()
        {
            var layout = SettlementBlockLayout.Create(110201, DistrictKind.Worker, 6);
            var archetypes = ShellCatalogFactory.CreateDefaultArchetypes();
            var districts = ShellCatalogFactory.CreateDefaultDistricts();
            var binder = new BuildingShellBinder();
            var bridge = new SettlementShellBridge(binder, archetypes, districts);

            var a = bridge.BindProperties(layout, 110201);
            var b = bridge.BindAll(layout, 110201);
            Assert.AreEqual(a.Count, b.Count);
            Assert.Greater(a.Count, 0);
            for (var i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(ShellFingerprint(a[i]), ShellFingerprint(b[i]));
                var stack = ShellMassingBuilder.BuildFloorStack(a[i].Plot, a[i].Dna);
                var report = ShellOverlapGuard.Validate(a[i], stack);
                Assert.IsTrue(report.Ok, $"Shell {i} overlap: {report.Error}");
            }
        }

        [Test]
        public void Create_Affluent_HasClosedPerimeterWithGate()
        {
            var layout = SettlementBlockLayout.Create(55, DistrictKind.Affluent, 4);
            for (var i = 0; i < layout.Properties.Count; i++)
            {
                var prop = layout.Properties[i];
                Assert.IsTrue(HasGate(prop), $"Affluent plot {i} missing front gate");
                Assert.IsTrue(HasFrontEdge(prop), $"Affluent plot {i} missing street-front fence");
                Assert.IsTrue(HasEdge(prop, PlotFace.Back), $"Affluent plot {i} missing back fence");
                Assert.IsTrue(HasEdge(prop, PlotFace.Left), $"Affluent plot {i} missing left fence");
                Assert.IsTrue(HasEdge(prop, PlotFace.Right), $"Affluent plot {i} missing right fence");
                AssertPerimeterAxisAligned(prop);
            }
        }

        [Test]
        public void Create_AllFencedDistricts_BoundariesAreAxisAlignedOnParcel()
        {
            var kinds = new[]
            {
                DistrictKind.Worker, DistrictKind.Affluent, DistrictKind.Rich, DistrictKind.Elite
            };
            for (var k = 0; k < kinds.Length; k++)
            {
                var layout = SettlementBlockLayout.Create(110201, kinds[k], 4);
                for (var i = 0; i < layout.Properties.Count; i++)
                    AssertPerimeterAxisAligned(layout.Properties[i]);
            }
        }

        [Test]
        public void Create_Poor_HasNoBoundaries()
        {
            var layout = SettlementBlockLayout.Create(8, DistrictKind.Poor, 6);
            for (var i = 0; i < layout.Properties.Count; i++)
                Assert.AreEqual(0, layout.Properties[i].Boundaries.Count);
        }

        [Test]
        public void Create_Worker_HasSideOrRearFence_NoFrontGate()
        {
            var layout = SettlementBlockLayout.Create(12, DistrictKind.Worker, 5);
            for (var i = 0; i < layout.Properties.Count; i++)
            {
                var prop = layout.Properties[i];
                Assert.Greater(prop.Boundaries.Count, 0);
                Assert.IsFalse(HasGate(prop));
            }
        }

        [Test]
        public void Create_Elite_HasDriveSecondaryAndWallGate()
        {
            var layout = SettlementBlockLayout.Create(77, DistrictKind.Elite, 3);
            Assert.Greater(layout.Properties.Count, 0);
            for (var i = 0; i < layout.Properties.Count; i++)
            {
                var prop = layout.Properties[i];
                Assert.IsTrue(HasZone(prop, PropertyZoneKind.Drive), $"Elite {i} missing Drive");
                Assert.IsTrue(HasZone(prop, PropertyZoneKind.SecondaryPad), $"Elite {i} missing SecondaryPad");
                Assert.IsTrue(prop.HasSecondaryBuilding);
                Assert.IsTrue(prop.Parcel.ContainsPad(prop.BuildingPad));
                Assert.IsTrue(prop.Parcel.ContainsPad(prop.SecondaryBuildingPad));
                Assert.IsTrue(HasGate(prop), $"Elite {i} missing gate");
                Assert.IsTrue(HasBoundaryStyle(prop, BoundaryStyle.Wall));
            }
        }

        [Test]
        public void Create_ShopFront_HasAlleyRearAndService_NoGarden()
        {
            var layout = SettlementBlockLayout.Create(33, DistrictKind.Commercial, 6);
            for (var i = 0; i < layout.Properties.Count; i++)
            {
                var prop = layout.Properties[i];
                Assert.AreEqual(FaceKind.Alley, prop.Street.Back);
                Assert.IsTrue(HasZone(prop, PropertyZoneKind.Service));
                Assert.IsFalse(HasZone(prop, PropertyZoneKind.FrontGarden));
                Assert.AreEqual(prop.Parcel.Origin, prop.BuildingPad.Origin);
                Assert.AreEqual(0, prop.Boundaries.Count);
            }
        }

        [Test]
        public void Create_Affluent_PlacesDomesticPropsInGardens()
        {
            var layout = SettlementBlockLayout.Create(55, DistrictKind.Affluent, 5);
            var anyProps = false;
            for (var i = 0; i < layout.Properties.Count; i++)
            {
                var prop = layout.Properties[i];
                if (prop.Props.Count == 0)
                    continue;
                anyProps = true;
                for (var p = 0; p < prop.Props.Count; p++)
                {
                    var propSpec = prop.Props[p];
                    Assert.IsTrue(
                        propSpec.Zone == PropertyZoneKind.FrontGarden
                        || propSpec.Zone == PropertyZoneKind.RearYard,
                        $"Affluent prop in unexpected zone {propSpec.Zone}");
                    Assert.AreEqual(PropSemantic.Domestic, propSpec.Semantic);
                    Assert.IsTrue(
                        ParcelContainsPoint(prop.Parcel, propSpec.Position),
                        $"Affluent prop {propSpec.Kind} outside parcel");
                }
            }

            Assert.IsTrue(anyProps, "Expected Affluent gardens to receive props");
        }

        [Test]
        public void Create_Shop_PlacesIndustrialPropsInService()
        {
            var layout = SettlementBlockLayout.Create(33, DistrictKind.Commercial, 6);
            var any = false;
            for (var i = 0; i < layout.Properties.Count; i++)
            {
                var prop = layout.Properties[i];
                for (var p = 0; p < prop.Props.Count; p++)
                {
                    any = true;
                    Assert.AreEqual(PropertyZoneKind.Service, prop.Props[p].Zone);
                    Assert.AreEqual(PropSemantic.Industrial, prop.Props[p].Semantic);
                }
            }

            Assert.IsTrue(any, "Expected ShopFront service props");
        }

        [Test]
        public void Create_Props_AreDeterministic()
        {
            var a = SettlementBlockLayout.Create(110201, DistrictKind.Rich, 4);
            var b = SettlementBlockLayout.Create(110201, DistrictKind.Rich, 4);
            for (var i = 0; i < a.Properties.Count; i++)
            {
                Assert.AreEqual(a.Properties[i].Props.Count, b.Properties[i].Props.Count);
                for (var p = 0; p < a.Properties[i].Props.Count; p++)
                {
                    Assert.AreEqual(a.Properties[i].Props[p].Kind, b.Properties[i].Props[p].Kind);
                    Assert.AreEqual(a.Properties[i].Props[p].Position, b.Properties[i].Props[p].Position);
                }
            }
        }

        [Test]
        public void Create_Poor_HasNoProps()
        {
            var layout = SettlementBlockLayout.Create(7, DistrictKind.Poor, 6);
            for (var i = 0; i < layout.Properties.Count; i++)
                Assert.AreEqual(0, layout.Properties[i].Props.Count);
        }

        [Test]
        public void CreateWithRearLane_HasLaneAndAlleyBacks()
        {
            var layout = SettlementBlockLayout.CreateWithRearLane(110201, DistrictKind.Worker, 6);
            Assert.IsTrue(layout.HasRearLane);
            Assert.Greater(layout.Lane.WidthMeters, 0f);
            Assert.Greater(layout.Lane.LengthMeters, 0f);
            Assert.AreEqual(6, layout.Properties.Count);
            for (var i = 0; i < layout.Properties.Count; i++)
                Assert.AreEqual(FaceKind.Alley, layout.Properties[i].Street.Back);
        }

        [Test]
        public void CreateDoubleFrontage_HasTwoRowsCornersAndLane()
        {
            const int perSide = 4;
            var layout = SettlementBlockLayout.CreateDoubleFrontage(42, DistrictKind.Affluent, perSide);
            Assert.AreEqual(perSide * 2, layout.Properties.Count);
            Assert.IsTrue(layout.HasRearLane);
            Assert.Greater(layout.Lane.WidthMeters, 0f);

            for (var i = 0; i < layout.Properties.Count; i++)
            {
                Assert.AreEqual(FaceKind.Alley, layout.Properties[i].Street.Back);
                AssertPerimeterAxisAligned(layout.Properties[i]);
            }

            Assert.AreEqual(FaceKind.Street, layout.Properties[0].Street.Left);
            Assert.AreEqual(FaceKind.Street, layout.Properties[perSide - 1].Street.Right);
            Assert.AreEqual(FaceKind.Street, layout.Properties[perSide].Street.Left);
            Assert.AreEqual(FaceKind.Street, layout.Properties[perSide * 2 - 1].Street.Right);

            for (var i = 0; i < perSide; i++)
            {
                Assert.AreEqual(0f, layout.Properties[i].Parcel.YawDegrees, 0.01f);
                Assert.AreEqual(180f, layout.Properties[perSide + i].Parcel.YawDegrees, 0.01f);
                Assert.AreEqual(
                    layout.Properties[i].Parcel.WidthMeters,
                    layout.Properties[perSide + i].Parcel.WidthMeters,
                    0.01f);
            }
        }

        [Test]
        public void CreateDoubleFrontage_IsDeterministic()
        {
            var a = SettlementBlockLayout.CreateDoubleFrontage(110201, DistrictKind.Worker, 4);
            var b = SettlementBlockLayout.CreateDoubleFrontage(110201, DistrictKind.Worker, 4);
            Assert.AreEqual(a.Properties.Count, b.Properties.Count);
            for (var i = 0; i < a.Properties.Count; i++)
            {
                Assert.AreEqual(Fingerprint(a.Properties[i]), Fingerprint(b.Properties[i]));
                Assert.AreEqual(a.Properties[i].Parcel.Origin, b.Properties[i].Parcel.Origin);
            }
        }

        [Test]
        public void CreateStreetSpine_HasLoopStreetsAndAggregatedPlots()
        {
            const int blocks = 2;
            const int perSide = 3;
            var fabric = SettlementFabricLayout.CreateStreetSpine(
                110201, DistrictKind.Worker, blocks, perSide);

            Assert.AreEqual(blocks, fabric.BlockCount);
            Assert.AreEqual(blocks * perSide * 2, fabric.Properties.Count);
            Assert.AreEqual(fabric.Properties.Count, fabric.Plots.Count);
            Assert.GreaterOrEqual(fabric.Streets.Count, 4);

            var primary = 0;
            var cross = 0;
            for (var i = 0; i < fabric.Streets.Count; i++)
            {
                if (fabric.Streets[i].Kind == SettlementStreetKind.Primary)
                    primary++;
                else if (fabric.Streets[i].Kind == SettlementStreetKind.Cross)
                    cross++;
                Assert.Greater(fabric.Streets[i].LengthMeters, 0f);
                Assert.Greater(fabric.Streets[i].WidthMeters, 0f);
            }

            Assert.GreaterOrEqual(primary, 2);
            Assert.GreaterOrEqual(cross, 2);
            Assert.AreEqual(blocks, fabric.Lanes.Count);

            for (var i = 0; i < fabric.Properties.Count; i++)
            {
                Assert.AreEqual(i, fabric.Properties[i].PlotIndex);
                Assert.AreEqual(FaceKind.Alley, fabric.Properties[i].Street.Back);
                Assert.IsTrue(
                    fabric.Properties[i].Parcel.ContainsPad(fabric.Properties[i].BuildingPad),
                    $"plot {i} pad outside parcel after spine offset");
            }
        }

        [Test]
        public void CreateStreetSpine_IsDeterministic()
        {
            var a = SettlementFabricLayout.CreateStreetSpine(42, DistrictKind.Affluent, 2, 3);
            var b = SettlementFabricLayout.CreateStreetSpine(42, DistrictKind.Affluent, 2, 3);
            Assert.AreEqual(a.Properties.Count, b.Properties.Count);
            Assert.AreEqual(a.Streets.Count, b.Streets.Count);
            for (var i = 0; i < a.Properties.Count; i++)
            {
                Assert.AreEqual(Fingerprint(a.Properties[i]), Fingerprint(b.Properties[i]));
                Assert.AreEqual(a.Properties[i].Parcel.Origin, b.Properties[i].Parcel.Origin);
            }

            for (var i = 0; i < a.Streets.Count; i++)
            {
                Assert.AreEqual(a.Streets[i].Kind, b.Streets[i].Kind);
                Assert.AreEqual(a.Streets[i].Origin, b.Streets[i].Origin);
                Assert.AreEqual(a.Streets[i].LengthMeters, b.Streets[i].LengthMeters);
            }
        }

        [Test]
        public void CreateStreetSpine_StreetRibbonsStayOnFabricLoop()
        {
            var fabric = SettlementFabricLayout.CreateStreetSpine(110201, DistrictKind.Worker, 2, 3);
            Assert.Greater(fabric.Streets.Count, 0);

            for (var i = 0; i < fabric.Streets.Count; i++)
            {
                var street = fabric.Streets[i];
                var start = street.CenterlineStart;
                var end = street.CenterlineEnd;
                var dir = (end - start).normalized;

                if (street.Kind == SettlementStreetKind.Primary)
                {
                    Assert.Greater(
                        Mathf.Abs(Vector3.Dot(dir, Vector3.right)),
                        0.95f,
                        $"Primary street {i} not aligned E–W");
                    Assert.GreaterOrEqual(start.z, -0.01f);
                }
                else
                {
                    Assert.Greater(
                        Vector3.Dot(dir, Vector3.forward),
                        0.95f,
                        $"Cross street {i} must run north (+Z), not south");
                    Assert.Greater(end.z, start.z);
                    Assert.GreaterOrEqual(Mathf.Min(start.z, end.z), -0.01f);
                }
            }

            for (var i = 0; i < fabric.Lanes.Count; i++)
            {
                var lane = fabric.Lanes[i];
                Assert.Greater(lane.Origin.z, 0f, "Service lane must sit north of primary street");
                Assert.Greater(lane.LengthMeters, 0f);
                Assert.Greater(lane.WidthMeters, 0f);
            }
        }

        [Test]
        public void Translate_OffsetsParcelPadAndBoundaries()
        {
            var layout = SettlementBlockLayout.Create(7, DistrictKind.Affluent, 2);
            var src = layout.Properties[0];
            var delta = new Vector3(10f, 0f, 5f);
            var dst = SettlementPropertyTransform.Translate(src, delta, 99);
            Assert.AreEqual(99, dst.PlotIndex);
            Assert.AreEqual(src.Parcel.Origin + delta, dst.Parcel.Origin);
            Assert.AreEqual(src.BuildingPad.Origin + delta, dst.BuildingPad.Origin);
            Assert.AreEqual(src.Boundaries.Count, dst.Boundaries.Count);
            if (src.Boundaries.Count > 0)
            {
                Assert.AreEqual(src.Boundaries[0].StartWorld + delta, dst.Boundaries[0].StartWorld);
                Assert.AreEqual(src.Boundaries[0].EndWorld + delta, dst.Boundaries[0].EndWorld);
            }

            Assert.IsTrue(dst.Parcel.ContainsPad(dst.BuildingPad));
        }

        [Test]
        public void ApproachBinder_PicksStableTownApproach()
        {
            var markers = new[]
            {
                new PoiMarker(
                    new PoiId("Ruin_0"),
                    PoiKind.Ruin,
                    new Vector3(1f, 0f, 1f),
                    BiomeId.Forest),
                new PoiMarker(
                    new PoiId("TownApproach_1"),
                    PoiKind.TownApproach,
                    new Vector3(100f, 0f, 200f),
                    BiomeId.Meadow),
                new PoiMarker(
                    new PoiId("TownApproach_0"),
                    PoiKind.TownApproach,
                    new Vector3(50f, 0f, 50f),
                    BiomeId.Meadow),
            };

            Assert.IsTrue(SettlementApproachBinder.TryFindTownApproach(markers, out var picked));
            Assert.AreEqual("TownApproach_0", picked.Id.Value);
        }

        [Test]
        public void ApproachBinder_CreateFabricAtApproach_IsDeterministicAndNearPose()
        {
            var approach = new PoiMarker(
                new PoiId("TownApproach_0"),
                PoiKind.TownApproach,
                new Vector3(128f, 0f, 256f),
                BiomeId.Meadow);

            var a = SettlementApproachBinder.CreateFabricAtApproach(approach, 12f, 110201);
            var b = SettlementApproachBinder.CreateFabricAtApproach(approach, 12f, 110201);
            Assert.IsTrue(a.HasWorldPose);
            Assert.AreEqual(a.Pose.Position, b.Pose.Position);
            Assert.AreEqual(a.Pose.YawDegrees, b.Pose.YawDegrees);
            Assert.AreEqual(a.Properties.Count, b.Properties.Count);
            Assert.AreEqual(12f, a.Pose.Position.y, 0.01f);

            for (var i = 0; i < a.Properties.Count; i++)
            {
                Assert.AreEqual(a.Properties[i].Parcel.Origin, b.Properties[i].Parcel.Origin);
                // GardenSquare has no BuildingPad — Shell skip via IsOpenSpace.
                if (!a.Properties[i].IsOpenSpace)
                    Assert.IsTrue(a.Properties[i].Parcel.ContainsPad(a.Properties[i].BuildingPad));
                var dx = a.Properties[i].Parcel.Origin.x - a.Pose.Position.x;
                var dz = a.Properties[i].Parcel.Origin.z - a.Pose.Position.z;
                Assert.Less(Mathf.Sqrt(dx * dx + dz * dz), 250f, "parcel unexpectedly far from pose");
                Assert.AreEqual(a.Pose.Position.y, a.Properties[i].Parcel.Origin.y, 0.01f);
            }
        }

        [Test]
        public void ApplyPose_RotatesYawAndKeepsPadInsideParcel()
        {
            var local = SettlementBlockLayout.Create(3, DistrictKind.Worker, 2);
            var pose = SettlementPose.Create(new Vector3(40f, 5f, -20f), 90f, 99, "test");
            var src = local.Properties[0];
            var dst = SettlementPropertyTransform.ApplyPose(src, pose, src.PlotIndex);
            Assert.AreEqual(src.Parcel.YawDegrees + 90f, dst.Parcel.YawDegrees, 0.01f);
            Assert.AreEqual(5f, dst.Parcel.Origin.y, 0.01f);
            Assert.IsTrue(dst.Parcel.ContainsPad(dst.BuildingPad));
        }

        [Test]
        public void TerrainConform_SnapsEachLotToMaxCornerHeight()
        {
            var approach = new PoiMarker(
                new PoiId("TownApproach_0"),
                PoiKind.TownApproach,
                new Vector3(0f, 0f, 0f),
                BiomeId.Meadow);
            var fabric = SettlementApproachBinder.CreateFabricAtApproach(approach, 10f, 110201, plotsPerSide: 2);
            // Slope: Y = 10 + 0.1 * worldX — uphill lots must rise above the flat pose Y.
            SettlementTerrainConform.SnapFabric(fabric, new SlopeHeightSampler(baseY: 10f, slopeX: 0.1f));

            var sawLift = false;
            for (var i = 0; i < fabric.Properties.Count; i++)
            {
                var prop = fabric.Properties[i];
                if (!prop.IsOpenSpace)
                    Assert.IsTrue(prop.Parcel.ContainsPad(prop.BuildingPad));
                Assert.GreaterOrEqual(prop.Parcel.Origin.y, 10f - 0.01f);
                if (prop.Parcel.Origin.y > 10.05f)
                    sawLift = true;
            }

            Assert.IsTrue(sawLift, "Expected at least one lot lifted on the slope");
        }

        [Test]
        public void CreateDistrictMix_HasCommercialWorkerAndGardenSquare()
        {
            var fabric = SettlementFabricLayout.CreateDistrictMix(110201, plotsPerSide: 2);
            var commercial = 0;
            var worker = 0;
            var open = 0;
            for (var i = 0; i < fabric.Properties.Count; i++)
            {
                var p = fabric.Properties[i];
                if (p.IsOpenSpace)
                {
                    open++;
                    Assert.AreEqual(GardenSquareFactory.ArchetypeId, p.ArchetypeId);
                    Assert.IsTrue(HasZone(p, PropertyZoneKind.OpenSpace));
                }
                else if (p.ArchetypeId == "ShopFront")
                    commercial++;
                else if (p.ArchetypeId == "WorkerYard")
                    worker++;
            }

            Assert.Greater(commercial, 0);
            Assert.Greater(worker, 0);
            Assert.AreEqual(1, open);
            Assert.Less(fabric.Plots.Count, fabric.Properties.Count);

            var archetypes = ShellCatalogFactory.CreateDefaultArchetypes();
            var districts = ShellCatalogFactory.CreateDefaultDistricts();
            var bridge = new SettlementShellBridge(new BuildingShellBinder(), archetypes, districts);
            var shells = bridge.BindProperties(fabric, 110201);
            Assert.AreEqual(fabric.Plots.Count, shells.Count);
            Assert.AreEqual(fabric.Properties.Count - open, shells.Count);
        }

        [Test]
        public void CreateStreetSpine_PlacesStreetPropsDeterministically()
        {
            var a = SettlementFabricLayout.CreateStreetSpine(110201, DistrictKind.Worker, 2, 3);
            var b = SettlementFabricLayout.CreateStreetSpine(110201, DistrictKind.Worker, 2, 3);
            Assert.Greater(a.StreetProps.Count, 0);
            Assert.AreEqual(a.StreetProps.Count, b.StreetProps.Count);
            for (var i = 0; i < a.StreetProps.Count; i++)
            {
                Assert.AreEqual(a.StreetProps[i].Kind, b.StreetProps[i].Kind);
                Assert.AreEqual(a.StreetProps[i].StreetKind, b.StreetProps[i].StreetKind);
                Assert.AreEqual(a.StreetProps[i].Position, b.StreetProps[i].Position);
            }
        }

        [Test]
        public void CreateStreetSpine_StreetPropsStayNearRibbons()
        {
            var fabric = SettlementFabricLayout.CreateStreetSpine(110201, DistrictKind.Worker, 2, 3);
            Assert.Greater(fabric.StreetProps.Count, 0);
            for (var i = 0; i < fabric.StreetProps.Count; i++)
            {
                var prop = fabric.StreetProps[i];
                var near = false;
                for (var s = 0; s < fabric.Streets.Count; s++)
                {
                    var street = fabric.Streets[s];
                    if (street.Kind != prop.StreetKind)
                        continue;
                    var dist = DistancePointToSegmentXZ(
                        prop.Position,
                        street.CenterlineStart,
                        street.CenterlineEnd);
                    // Half ribbon + max catalog curb offset (~1.1) + slack.
                    if (dist <= street.WidthMeters * 0.5f + 1.5f)
                    {
                        near = true;
                        break;
                    }
                }

                Assert.IsTrue(near, $"Street prop {prop.Kind} far from {prop.StreetKind} ribbon");
            }
        }

        [Test]
        public void SiteValidator_RejectsLotsUnderWater()
        {
            var parcel = PlotParcel.Rect(2, 3, new Vector3(0f, 5f, 0f));
            var water = new FakeWaterSampler(waterY: 8f, minX: -10f, maxX: 10f, minZ: -10f, maxZ: 10f);
            Assert.IsFalse(SettlementSiteValidator.IsParcelDry(parcel, water));

            var dryParcel = PlotParcel.Rect(2, 3, new Vector3(0f, 12f, 0f));
            Assert.IsTrue(SettlementSiteValidator.IsParcelDry(dryParcel, water));
        }

        [Test]
        public void ApproachBinder_SkipsWetFootprintForDryOffset()
        {
            var approach = new PoiMarker(
                new PoiId("TownApproach_0"),
                PoiKind.TownApproach,
                new Vector3(0f, 0f, 0f),
                BiomeId.Meadow);
            // Small lake at origin; dry land outside ~±20m. Search ring should land on a dry pad.
            var height = new FlatHeightSampler(10f);
            var water = new FakeWaterSampler(waterY: 20f, minX: -20f, maxX: 20f, minZ: -20f, maxZ: 20f);

            Assert.IsTrue(
                SettlementApproachBinder.TryCreateDryFabricNearApproach(
                    approach,
                    110201,
                    height,
                    water,
                    out var fabric,
                    out var placedAt,
                    plotsPerSide: 2));
            Assert.IsNotNull(fabric);
            Assert.Greater(
                Mathf.Max(Mathf.Abs(placedAt.x), Mathf.Abs(placedAt.z)),
                20f);
            Assert.IsTrue(SettlementSiteValidator.IsFabricDry(fabric, water));
        }

        private sealed class SlopeHeightSampler : IWorldHeightSampler
        {
            private readonly float _baseY;
            private readonly float _slopeX;

            public SlopeHeightSampler(float baseY, float slopeX)
            {
                _baseY = baseY;
                _slopeX = slopeX;
            }

            public bool TrySampleWorldY(float worldX, float worldZ, out float worldY)
            {
                worldY = _baseY + _slopeX * worldX;
                return true;
            }
        }

        private sealed class FlatHeightSampler : IWorldHeightSampler
        {
            private readonly float _y;

            public FlatHeightSampler(float y) => _y = y;

            public bool TrySampleWorldY(float worldX, float worldZ, out float worldY)
            {
                worldY = _y;
                return true;
            }
        }

        private sealed class FakeWaterSampler : IWaterSampler
        {
            private readonly float _waterY;
            private readonly float _minX;
            private readonly float _maxX;
            private readonly float _minZ;
            private readonly float _maxZ;

            public FakeWaterSampler(float waterY, float minX, float maxX, float minZ, float maxZ)
            {
                _waterY = waterY;
                _minX = minX;
                _maxX = maxX;
                _minZ = minZ;
                _maxZ = maxZ;
            }

            public bool TryGetWaterLevel(float worldX, float worldZ, out float waterY)
            {
                if (worldX >= _minX && worldX <= _maxX && worldZ >= _minZ && worldZ <= _maxZ)
                {
                    waterY = _waterY;
                    return true;
                }

                waterY = 0f;
                return false;
            }
        }

        private static bool HasGate(PropertySpec prop)
        {
            for (var i = 0; i < prop.Boundaries.Count; i++)
            {
                if (prop.Boundaries[i].IsGate)
                    return true;
            }

            return false;
        }

        private static bool HasFrontEdge(PropertySpec prop) => HasEdge(prop, PlotFace.Front, requireNonGate: true);

        private static bool HasEdge(PropertySpec prop, PlotFace edge, bool requireNonGate = false)
        {
            for (var i = 0; i < prop.Boundaries.Count; i++)
            {
                var b = prop.Boundaries[i];
                if (b.Edge != edge)
                    continue;
                if (requireNonGate && b.IsGate)
                    continue;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Front/back segments must run along parcel +X; left/right along parcel +Z.
        /// Catches the "fins sticking out front" orientation bug.
        /// </summary>
        private static void AssertPerimeterAxisAligned(PropertySpec prop)
        {
            var yaw = Quaternion.Euler(0f, prop.Parcel.YawDegrees, 0f);
            var axisX = yaw * Vector3.right;
            var axisZ = yaw * Vector3.forward;
            for (var i = 0; i < prop.Boundaries.Count; i++)
            {
                var b = prop.Boundaries[i];
                var dir = (b.EndWorld - b.StartWorld).normalized;
                if (b.Edge == PlotFace.Front || b.Edge == PlotFace.Back)
                {
                    Assert.Greater(
                        Mathf.Abs(Vector3.Dot(dir, axisX)),
                        0.95f,
                        $"{prop.ArchetypeId} plot {prop.PlotIndex} {b.Edge} not along street axis");
                }
                else if (b.Edge == PlotFace.Left || b.Edge == PlotFace.Right)
                {
                    Assert.Greater(
                        Mathf.Abs(Vector3.Dot(dir, axisZ)),
                        0.95f,
                        $"{prop.ArchetypeId} plot {prop.PlotIndex} {b.Edge} not along lot depth");
                }
            }
        }

        private static bool HasBoundaryStyle(PropertySpec prop, BoundaryStyle style)
        {
            for (var i = 0; i < prop.Boundaries.Count; i++)
            {
                if (prop.Boundaries[i].Style == style && !prop.Boundaries[i].IsGate)
                    return true;
            }

            return false;
        }

        private static bool HasZone(PropertySpec prop, PropertyZoneKind kind)
        {
            for (var i = 0; i < prop.Zones.Count; i++)
            {
                if (prop.Zones[i].Kind == kind && prop.Zones[i].DepthMeters > 0.01f)
                    return true;
            }

            return false;
        }

        private static string Fingerprint(PropertySpec p)
        {
            return string.Join("|",
                p.PlotIndex,
                p.DistrictId,
                p.ArchetypeId,
                p.Parcel.WidthBays,
                p.Parcel.DepthBays,
                p.Parcel.Origin,
                p.BuildingPad.WidthBays,
                p.BuildingPad.DepthBays,
                p.BuildingPad.Origin,
                p.Zones.Count,
                p.Boundaries.Count,
                p.Props.Count,
                p.HasSecondaryBuilding,
                p.IsOpenSpace);
        }

        private static bool ParcelContainsPoint(PlotParcel parcel, Vector3 world)
        {
            var yaw = Quaternion.Euler(0f, parcel.YawDegrees, 0f);
            var local = Quaternion.Inverse(yaw) * (world - parcel.Origin);
            const float eps = 0.05f;
            return local.x >= -eps
                   && local.z >= -eps
                   && local.x <= parcel.WidthMeters + eps
                   && local.z <= parcel.DepthMeters + eps;
        }

        private static float DistancePointToSegmentXZ(Vector3 point, Vector3 a, Vector3 b)
        {
            var ax = a.x;
            var az = a.z;
            var bx = b.x;
            var bz = b.z;
            var px = point.x;
            var pz = point.z;
            var abx = bx - ax;
            var abz = bz - az;
            var len2 = abx * abx + abz * abz;
            if (len2 < 1e-6f)
            {
                var dx0 = px - ax;
                var dz0 = pz - az;
                return Mathf.Sqrt(dx0 * dx0 + dz0 * dz0);
            }

            var t = ((px - ax) * abx + (pz - az) * abz) / len2;
            if (t < 0f) t = 0f;
            else if (t > 1f) t = 1f;
            var qx = ax + abx * t;
            var qz = az + abz * t;
            var dx = px - qx;
            var dz = pz - qz;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static string ShellFingerprint(ShellInstance shell)
        {
            var parts = new List<string>(shell.Pieces.Count + 2)
            {
                shell.Dna.ArchetypeId,
                shell.Plot.Origin.ToString()
            };
            for (var i = 0; i < shell.Pieces.Count; i++)
            {
                var p = shell.Pieces[i];
                parts.Add($"{p.PieceId}:{p.LocalPosition}:{p.LocalScale}");
            }

            return string.Join(";", parts);
        }
    }
}
