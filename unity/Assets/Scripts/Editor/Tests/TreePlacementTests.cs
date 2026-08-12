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
                go.transform.localScale = Vector3.one; // 1m cube
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
                go.transform.localScale = new Vector3(4f, 20f, 4f); // wide crown proxy
                TreePlacement.EnsureTrunkCollider(go);
                var capsule = go.GetComponent<CapsuleCollider>();
                Assert.IsNotNull(capsule);
                var worldRadius = capsule.radius * go.transform.lossyScale.x;
                var worldHeight = capsule.height * go.transform.lossyScale.y;
                Assert.Less(worldRadius, 1.2f, "trunk radius must not be a crown bubble");
                Assert.Less(worldHeight, 14f, "trunk capsule must not cover the full canopy");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
