using GameDev.World.Vegetation;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Vegetation.Tests
{
    public sealed class TreePlacementTests
    {
        [Test]
        public void PickTargetHeight_Mature_IsAtLeastThreeTimesCharacter()
        {
            var rng = new System.Random(7);
            for (var i = 0; i < 40; i++)
            {
                var h = TreePlacement.PickTargetHeightMeters(young: false, rng);
                Assert.GreaterOrEqual(h, TreePlacement.CharacterHeightMeters * 3f);
                Assert.LessOrEqual(h, TreePlacement.MatureHeightMaxMeters + 0.01f);
            }
        }

        [Test]
        public void ApplyWorldHeight_ScalesRendererToTarget()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                go.transform.localScale = Vector3.one;
                TreePlacement.ApplyWorldHeight(go, 12f);
                var h = TreePlacement.MeasureRendererHeight(go);
                Assert.AreEqual(12f, h, 0.15f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void EnsureTrunkCollider_IsNarrowerThanCrownAndShorterThanFullHeight()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                go.transform.localScale = new Vector3(4f, 20f, 4f);
                TreePlacement.EnsureTrunkCollider(go);
                var capsule = go.GetComponent<CapsuleCollider>();
                Assert.IsNotNull(capsule);
                var worldRadius = capsule.radius * go.transform.lossyScale.x;
                var worldHeight = capsule.height * go.transform.lossyScale.y;
                Assert.Less(worldRadius, 0.5f, "trunk radius must not be a crown bubble");
                Assert.Less(worldHeight, 14f, "trunk capsule must not cover the full canopy");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void EnsureTrunkCollider_OffsetCrown_KeepsCapsuleOnPivotAxis()
        {
            var go = new GameObject("OffsetCrownTree");
            try
            {
                go.transform.position = new Vector3(100f, 0f, 50f);
                var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunk.name = "Trunk";
                trunk.transform.SetParent(go.transform, false);
                trunk.transform.localPosition = new Vector3(0f, 4f, 0f);
                trunk.transform.localScale = new Vector3(0.35f, 4f, 0.35f);

                var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crown.name = "Crown";
                crown.transform.SetParent(go.transform, false);
                crown.transform.localPosition = new Vector3(2.5f, 9f, 0f);
                crown.transform.localScale = new Vector3(6f, 5f, 6f);

                TreePlacement.EnsureTrunkCollider(go);
                var capsules = go.GetComponents<CapsuleCollider>();
                Assert.GreaterOrEqual(capsules.Length, 1);
                var capsule = capsules[0];
                var worldCenter = go.transform.TransformPoint(capsule.center);
                Assert.AreEqual(go.transform.position.x, worldCenter.x, 0.15f,
                    "capsule X must stay on pivot, not crown AABB center");
                Assert.AreEqual(go.transform.position.z, worldCenter.z, 0.15f,
                    "capsule Z must stay on pivot, not crown AABB center");

                var worldRadius = capsule.radius * go.transform.lossyScale.x;
                Assert.Less(worldRadius, 0.45f, "trunk radius must ignore offset crown width");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void EnsureTrunkCollider_ThinBole_DoesNotUseMaxClampAsDefault()
        {
            var go = new GameObject("ThinPine");
            try
            {
                go.transform.position = Vector3.zero;
                var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunk.transform.SetParent(go.transform, false);
                trunk.transform.localPosition = new Vector3(0f, 5f, 0f);
                trunk.transform.localScale = new Vector3(0.25f, 5f, 0.25f); // ~0.125 m radius

                TreePlacement.EnsureTrunkCollider(go);
                var capsule = go.GetComponent<CapsuleCollider>();
                var worldRadius = capsule.radius * go.transform.lossyScale.x;
                Assert.Less(worldRadius, 0.28f, "thin bole must not inflate toward TrunkRadiusMax");
                Assert.Greater(worldRadius, 0.08f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
