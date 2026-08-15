using System.Collections.Generic;
using GameDev.World.Settlement;
using GameDev.World.Vegetation;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Vegetation.Tests
{
    public sealed class GrassMaskTests
    {
        [Test]
        public void WaterShoreFactor_NoWater_IsOne()
        {
            Assert.AreEqual(1f, GrassMask.WaterShoreFactor(null, 0f, 0f, 4f), 1e-5f);
        }

        [Test]
        public void Sample_ZeroClearance_IsZero()
        {
            var ecology = new VegetationEcology.Sample(0.5f, 0.5f, 0.9f, 0.2f, 0f);
            var density = GrassMask.Sample(
                0f, 0f, 10f, ecology, 0f, null, GrassExclusion.None);
            Assert.AreEqual(0f, density, 1e-5f);
        }

        [Test]
        public void Sample_LowGrass_BelowMin_IsZero()
        {
            var ecology = new VegetationEcology.Sample(0f, 0f, 0.02f, 0f, 0f);
            var density = GrassMask.Sample(
                0f, 0f, 10f, ecology, 1f, null, GrassExclusion.None);
            Assert.AreEqual(0f, density, 1e-5f);
        }

        [Test]
        public void Sample_AllowedLand_IsNearFullCarpet()
        {
            var ecology = new VegetationEcology.Sample(0.05f, 0.2f, 0.95f, 0.1f, 0f);
            var density = GrassMask.Sample(
                0f, 0f, 10f, ecology, 1f, null, GrassExclusion.None);
            Assert.Greater(density, 0.7f);
        }

        [Test]
        public void Sample_DenseForest_IsPatchyUnderstory()
        {
            var open = new VegetationEcology.Sample(0.05f, 0.2f, 0.9f, 0.1f, 0f);
            var dense = new VegetationEcology.Sample(0.9f, 0.4f, 0.55f, 0.2f, 0f);
            var openD = GrassMask.Sample(0f, 0f, 10f, open, 1f, null, GrassExclusion.None);
            var denseD = GrassMask.Sample(0f, 0f, 10f, dense, 1f, null, GrassExclusion.None);
            Assert.Greater(openD, denseD);
            Assert.Less(denseD, 0.45f);
        }

        [Test]
        public void Exclusion_StreetRect_ForbidsGrass()
        {
            var exclusion = new GrassExclusion(new List<GrassExclusion.RectZone>
            {
                new GrassExclusion.RectZone(
                    Vector3.zero, 20f, 8f, 0f, 0f)
            });
            Assert.AreEqual(0f, exclusion.Multiplier(5f, 4f), 1e-5f);
            Assert.AreEqual(1f, exclusion.Multiplier(50f, 50f), 1e-5f);
        }

        [Test]
        public void Exclusion_GardenRect_ReducesGrass()
        {
            var exclusion = new GrassExclusion(new List<GrassExclusion.RectZone>
            {
                new GrassExclusion.RectZone(
                    Vector3.zero, 10f, 10f, 0f, GrassExclusion.GardenDensityMul)
            });
            Assert.AreEqual(GrassExclusion.GardenDensityMul, exclusion.Multiplier(2f, 2f), 1e-5f);
        }

        [Test]
        public void DensityForZoneKind_BuildingPad_IsForbidden()
        {
            Assert.AreEqual(0f, GrassExclusion.DensityForZoneKind(PropertyZoneKind.BuildingPad));
            Assert.AreEqual(0f, GrassExclusion.DensityForZoneKind(PropertyZoneKind.Drive));
        }

        [Test]
        public void DensityForZoneKind_OpenSpace_IsReduced()
        {
            Assert.AreEqual(
                GrassExclusion.OpenSpaceDensityMul,
                GrassExclusion.DensityForZoneKind(PropertyZoneKind.OpenSpace));
        }

        [Test]
        public void EdgeHeightFactor_IsSCurve()
        {
            Assert.AreEqual(1f - GrassMask.HeightReduceIntensity, GrassMask.EdgeHeightFactor(0f), 1e-5f);
            Assert.AreEqual(1f, GrassMask.EdgeHeightFactor(1f), 1e-5f);
            var mid = GrassMask.EdgeHeightFactor(0.5f);
            Assert.Less(GrassMask.EdgeHeightFactor(0.25f), mid);
            Assert.Greater(GrassMask.EdgeHeightFactor(0.75f), mid);
        }

        [Test]
        public void EdgePlaceChance_ThinsSoftBand()
        {
            Assert.Greater(GrassMask.EdgePlaceChance(1f), 0.95f);
            Assert.Less(GrassMask.EdgePlaceChance(0.2f), GrassMask.EdgePlaceChance(0.8f));
            Assert.Greater(GrassMask.EdgePlaceChance(0.2f), 0.15f);
        }

        [Test]
        public void StampTreeInfluence_ClearsTrunkCore()
        {
            var mask = new float[8, 8];
            for (var z = 0; z < 8; z++)
            {
                for (var x = 0; x < 8; x++)
                {
                    mask[x, z] = 1f;
                }
            }

            var origin = Vector3.zero;
            var trunks = new List<Vector2> { new Vector2(3f, 3f) };
            GrassMask.StampTreeInfluence(mask, 8, origin, 12f, 1.5f, trunks);

            // Cell covering ~3,3 should be near zero (inside 0.5 m clear core).
            Assert.Less(mask[2, 2], 0.15f);
            Assert.Greater(mask[7, 7], 0.85f);
        }

        [Test]
        public void RoadFalloff_IsWithinMemoBand()
        {
            Assert.GreaterOrEqual(GrassMask.RoadEdgeFalloffMeters, 0.5f);
            Assert.LessOrEqual(GrassMask.RoadEdgeFalloffMeters, 1.5f);
        }

        [Test]
        public void RoadLawnSample_OnCenterline_IsBare()
        {
            var samples = new List<Vector2> { new Vector2(10f, 10f) };
            var lawn = RoadLawnMask.SampleLawn01(
                10f, 10f, samples, GrassMask.RoadClearCoreMeters, GrassMask.RoadEdgeFalloffMeters);
            Assert.AreEqual(0f, lawn, 1e-5f);
        }

        [Test]
        public void RoadLawnSample_BeyondFalloff_IsLawn()
        {
            var samples = new List<Vector2> { new Vector2(0f, 0f) };
            var far = GrassMask.RoadClearCoreMeters + GrassMask.RoadEdgeFalloffMeters + 0.5f;
            var lawn = RoadLawnMask.SampleLawn01(
                far, 0f, samples, GrassMask.RoadClearCoreMeters, GrassMask.RoadEdgeFalloffMeters);
            Assert.AreEqual(1f, lawn, 1e-5f);
        }

        [Test]
        public void RoadLawnSample_MidShoulder_IsPartial()
        {
            var samples = new List<Vector2> { new Vector2(0f, 0f) };
            var mid = GrassMask.RoadClearCoreMeters + GrassMask.RoadEdgeFalloffMeters * 0.5f;
            var lawn = RoadLawnMask.SampleLawn01(
                mid, 0f, samples, GrassMask.RoadClearCoreMeters, GrassMask.RoadEdgeFalloffMeters);
            Assert.Greater(lawn, 0.2f);
            Assert.Less(lawn, 0.8f);
        }

        [Test]
        public void RoadLawnSample_AlongSegment_IsSmoothNotScalloped()
        {
            var samples = new List<Vector2>
            {
                new Vector2(0f, 0f),
                new Vector2(8f, 0f),
            };
            var lateral = GrassMask.RoadClearCoreMeters + GrassMask.RoadEdgeFalloffMeters * 0.5f;
            var midOff = RoadLawnMask.SampleLawn01(
                4f, lateral, samples, GrassMask.RoadClearCoreMeters, GrassMask.RoadEdgeFalloffMeters);
            var nearPoint = RoadLawnMask.SampleLawn01(
                0.5f, lateral, samples, GrassMask.RoadClearCoreMeters, GrassMask.RoadEdgeFalloffMeters);
            // Mid-span lateral sample should match near-endpoint lateral sample (segment SDF).
            Assert.AreEqual(nearPoint, midOff, 0.05f);
        }
    }
}
