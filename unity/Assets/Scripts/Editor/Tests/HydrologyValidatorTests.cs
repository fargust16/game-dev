using System.Collections.Generic;
using GameDev.World.Hydrology;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Hydrology.Tests
{
    /// <summary>
    /// The validator is the safety net for invariants the algorithm currently guarantees by
    /// construction. These tests feed it hand-built networks that break each rule, so a future
    /// change to the algorithm cannot quietly take the net away with it.
    /// </summary>
    public sealed class HydrologyValidatorTests
    {
        private static readonly IReadOnlyList<LakeBasin> NoLakes = new LakeBasin[0];

        [Test]
        public void ValidNetwork_Passes()
        {
            var reaches = new[]
            {
                HydrologyTestTerrain.Reach(
                    0, -1, ReachTermination.Ocean, (0f, 0f, 30f), (10f, 0f, 26f), (20f, 0f, 22f)),
                HydrologyTestTerrain.Reach(
                    1, 0, ReachTermination.Confluence, (0f, 20f, 44f), (0f, 10f, 36f)),
            };

            Assert.IsTrue(HydrologyValidator.Validate(reaches, NoLakes, null).IsValid);
        }

        [Test]
        public void UphillReach_IsRejected()
        {
            var reaches = new[]
            {
                HydrologyTestTerrain.Reach(
                    0, -1, ReachTermination.Ocean, (0f, 0f, 30f), (10f, 0f, 42f)),
            };

            var report = HydrologyValidator.Validate(reaches, NoLakes, null);

            Assert.IsFalse(report.IsValid);
            StringAssert.Contains("uphill", report.Message);
        }

        [Test]
        public void DownstreamCycle_IsRejected()
        {
            var reaches = new[]
            {
                HydrologyTestTerrain.Reach(
                    0, 1, ReachTermination.Confluence, (0f, 0f, 30f), (10f, 0f, 26f)),
                HydrologyTestTerrain.Reach(
                    1, 0, ReachTermination.Confluence, (10f, 0f, 26f), (20f, 0f, 22f)),
            };

            var report = HydrologyValidator.Validate(reaches, NoLakes, null);

            Assert.IsFalse(report.IsValid);
            StringAssert.Contains("cycle", report.Message);
        }

        [Test]
        public void ReachThatEndsInlandWithNoDownstream_IsRejected()
        {
            // The exact failure the hard world rule exists to prevent: a river that simply stops.
            var reaches = new[]
            {
                HydrologyTestTerrain.Reach(
                    0, -1, ReachTermination.Confluence, (0f, 0f, 30f), (10f, 0f, 26f)),
            };

            var report = HydrologyValidator.Validate(reaches, NoLakes, null);

            Assert.IsFalse(report.IsValid);
            StringAssert.Contains("never reaches the ocean", report.Message);
        }

        [Test]
        public void MissingDownstreamReach_IsRejected()
        {
            var reaches = new[]
            {
                HydrologyTestTerrain.Reach(
                    0, 7, ReachTermination.Confluence, (0f, 0f, 30f), (10f, 0f, 26f)),
            };

            var report = HydrologyValidator.Validate(reaches, NoLakes, null);

            Assert.IsFalse(report.IsValid);
            StringAssert.Contains("missing downstream", report.Message);
        }

        [Test]
        public void UnorderedReaches_AreRejected()
        {
            var reaches = new[]
            {
                HydrologyTestTerrain.Reach(
                    1, -1, ReachTermination.Ocean, (0f, 0f, 30f), (10f, 0f, 26f)),
                HydrologyTestTerrain.Reach(
                    0, -1, ReachTermination.Ocean, (0f, 40f, 30f), (10f, 40f, 26f)),
            };

            var report = HydrologyValidator.Validate(reaches, NoLakes, null);

            Assert.IsFalse(report.IsValid);
            StringAssert.Contains("ascending id order", report.Message);
        }

        [Test]
        public void DuplicateNodePosition_IsRejected()
        {
            var reaches = new[]
            {
                HydrologyTestTerrain.Reach(
                    0, -1, ReachTermination.Ocean, (0f, 0f, 30f), (0f, 0f, 26f), (10f, 0f, 22f)),
            };

            var report = HydrologyValidator.Validate(reaches, NoLakes, null);

            Assert.IsFalse(report.IsValid);
            StringAssert.Contains("repeats a node position", report.Message);
        }

        private static LakeBasin Lake(
            int id, int outletReach, bool spillsDirectlyToOcean = false)
        {
            return new LakeBasin(
                new LakeId(id),
                new[] { new Vector2(0f, 0f), new Vector2(10f, 0f), new Vector2(10f, 10f) },
                40f,
                4f,
                900f,
                outletReach >= 0 ? new ReachId(outletReach) : ReachId.None,
                new ReachId[0],
                Vector2.zero,
                spillsDirectlyToOcean);
        }

        private static RiverReach ReachIntoLake(int id, int lakeId, params (float, float, float)[] pts)
        {
            var built = HydrologyTestTerrain.Reach(id, -1, ReachTermination.Lake, pts);
            return new RiverReach(
                built.Id,
                ReachId.None,
                new LakeId(lakeId),
                built.Nodes,
                built.StrahlerOrder,
                built.ContributingAreaSqm,
                built.WidthMeters,
                ReachTermination.Lake,
                true);
        }

        [Test]
        public void RiverThroughALakeToTheSea_Passes()
        {
            // The topology the first real bake produced and the validator originally rejected: an
            // inlet has no downstream reach of its own, because its water leaves through the lake.
            var reaches = new[]
            {
                ReachIntoLake(0, 0, (0f, 0f, 60f), (10f, 0f, 52f)),
                HydrologyTestTerrain.Reach(
                    1, -1, ReachTermination.Ocean, (40f, 0f, 50f), (60f, 0f, 30f)),
            };

            var report = HydrologyValidator.Validate(reaches, new[] { Lake(0, outletReach: 1) }, null);

            Assert.IsTrue(report.IsValid, report.Message);
        }

        [Test]
        public void LakeSpillingStraightOntoTheShore_Passes()
        {
            var reaches = new[]
            {
                ReachIntoLake(0, 0, (0f, 0f, 60f), (10f, 0f, 52f)),
            };

            var report = HydrologyValidator.Validate(
                reaches,
                new[] { Lake(0, outletReach: -1, spillsDirectlyToOcean: true) },
                null);

            Assert.IsTrue(report.IsValid, report.Message);
        }

        [Test]
        public void LakeThatDrainsIntoNothing_IsRejected()
        {
            var reaches = new[]
            {
                ReachIntoLake(0, 0, (0f, 0f, 60f), (10f, 0f, 52f)),
            };

            var report = HydrologyValidator.Validate(
                reaches,
                new[] { Lake(0, outletReach: -1) },
                null);

            Assert.IsFalse(report.IsValid);
            StringAssert.Contains("never reaches the ocean", report.Message);
        }

        [Test]
        public void ReachPointingAtAMissingLake_IsRejected()
        {
            var reaches = new[]
            {
                ReachIntoLake(0, 7, (0f, 0f, 60f), (10f, 0f, 52f)),
            };

            var report = HydrologyValidator.Validate(reaches, new[] { Lake(0, 0) }, null);

            Assert.IsFalse(report.IsValid);
            StringAssert.Contains("does not exist", report.Message);
        }

        [Test]
        public void LakeWithADegenerateShoreline_IsRejected()
        {
            var reaches = new[]
            {
                HydrologyTestTerrain.Reach(
                    0, -1, ReachTermination.Ocean, (0f, 0f, 30f), (10f, 0f, 26f)),
            };
            var lakes = new[]
            {
                new LakeBasin(
                    new LakeId(0),
                    new[] { new Vector2(0f, 0f), new Vector2(1f, 0f) },
                    40f,
                    3f,
                    900f,
                    new ReachId(0),
                    new ReachId[0],
                    Vector2.zero),
            };

            var report = HydrologyValidator.Validate(reaches, lakes, null);

            Assert.IsFalse(report.IsValid);
            StringAssert.Contains("fewer than three vertices", report.Message);
        }

        [Test]
        public void LakePointingAtAMissingOutlet_IsRejected()
        {
            var reaches = new[]
            {
                HydrologyTestTerrain.Reach(
                    0, -1, ReachTermination.Ocean, (0f, 0f, 30f), (10f, 0f, 26f)),
            };
            var lakes = new[]
            {
                new LakeBasin(
                    new LakeId(0),
                    new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f) },
                    40f,
                    3f,
                    900f,
                    new ReachId(9),
                    new ReachId[0],
                    Vector2.zero),
            };

            var report = HydrologyValidator.Validate(reaches, lakes, null);

            Assert.IsFalse(report.IsValid);
            StringAssert.Contains("missing outlet", report.Message);
        }
    }
}
