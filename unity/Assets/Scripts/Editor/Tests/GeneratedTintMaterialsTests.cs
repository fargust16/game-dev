using GameDev.World;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Tests
{
    public sealed class GeneratedTintMaterialsTests
    {
        [SetUp]
        public void ResetCache() => GeneratedTintMaterials.Reset();

        [TearDown]
        public void ClearCache() => GeneratedTintMaterials.Reset();

        [Test]
        public void SameTintAndQueue_ReturnsTheSameInstance()
        {
            var tint = new Color(0.7f, 0.22f, 0.2f, 1f);
            var a = GeneratedTintMaterials.Get(tint);
            var b = GeneratedTintMaterials.Get(tint);

            Assert.AreSame(a, b, "the whole point is that one definition is one material");
            Assert.AreEqual(1, GeneratedTintMaterials.CachedCount);
        }

        [Test]
        public void DifferentTint_ReturnsADifferentMaterial()
        {
            var a = GeneratedTintMaterials.Get(new Color(0.7f, 0.22f, 0.2f, 1f));
            var b = GeneratedTintMaterials.Get(new Color(0.53f, 0.49f, 0.45f, 1f));

            Assert.AreNotSame(a, b);
            Assert.AreEqual(2, GeneratedTintMaterials.CachedCount);
        }

        [Test]
        public void SameTintDifferentRenderQueue_ReturnsADifferentMaterial()
        {
            // The audit found exactly two axes; keying on colour alone would merge a wall with the
            // window plaque that has to draw after it.
            var tint = new Color(0.63f, 0.882f, 1f, 1f);
            var wall = GeneratedTintMaterials.Get(tint);
            var opening = GeneratedTintMaterials.Get(tint, 3001);

            Assert.AreNotSame(wall, opening);
            Assert.AreEqual(2, GeneratedTintMaterials.CachedCount);
            Assert.AreEqual(3001, opening.renderQueue);
        }

        [Test]
        public void CachedMaterial_CarriesTheExactRequestedProperties()
        {
            var tint = new Color(0.42f, 0.4f, 0.36f, 1f);
            var mat = GeneratedTintMaterials.Get(tint, 3001);

            Assert.IsNotNull(mat);
            Assert.IsNotNull(mat.shader);
            Assert.AreEqual(3001, mat.renderQueue);
            if (mat.HasProperty("_BaseColor"))
            {
                Assert.AreEqual(tint, mat.GetColor("_BaseColor"));
            }

            if (mat.HasProperty("_Color"))
            {
                Assert.AreEqual(tint, mat.GetColor("_Color"));
            }
        }

        [Test]
        public void DefaultQueue_LeavesTheShaderQueueAlone()
        {
            var mat = GeneratedTintMaterials.Get(new Color(0.58f, 0.51f, 0.45f, 1f));
            Assert.IsNotNull(mat.shader);
            Assert.AreEqual(
                mat.shader.renderQueue,
                mat.renderQueue,
                "no explicit queue means the material must serialise as m_CustomRenderQueue: -1");
        }

        [Test]
        public void Reset_MakesTheNextGenerationBuildFreshMaterials()
        {
            var tint = new Color(0.7f, 0.22f, 0.2f, 1f);
            var first = GeneratedTintMaterials.Get(tint);
            Assert.AreEqual(1, GeneratedTintMaterials.CachedCount);

            GeneratedTintMaterials.Reset();
            Assert.AreEqual(0, GeneratedTintMaterials.CachedCount);

            var second = GeneratedTintMaterials.Get(tint);
            Assert.AreNotSame(first, second, "a cleared cache must not hand the old object back");
            Assert.AreEqual(1, GeneratedTintMaterials.CachedCount);
        }

        [Test]
        public void DestroyedMaterial_IsReplacedRatherThanHandedBack()
        {
            // The safety property: a cached entry whose object Unity destroyed must never ship.
            var tint = new Color(0.35f, 0.38f, 0.42f, 1f);
            var first = GeneratedTintMaterials.Get(tint);
            Object.DestroyImmediate(first);

            var second = GeneratedTintMaterials.Get(tint);

            Assert.IsNotNull(second);
            Assert.IsTrue(second, "the replacement must be a live Unity object");
            Assert.AreEqual(1, GeneratedTintMaterials.CachedCount);
        }

        [Test]
        public void TwoRenderersCanShareOneMaterialSafely()
        {
            var tint = new Color(0.62f, 0.58f, 0.52f, 1f);
            var a = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                a.GetComponent<Renderer>().sharedMaterial = GeneratedTintMaterials.Get(tint);
                b.GetComponent<Renderer>().sharedMaterial = GeneratedTintMaterials.Get(tint);

                Assert.AreSame(
                    a.GetComponent<Renderer>().sharedMaterial,
                    b.GetComponent<Renderer>().sharedMaterial);
                Assert.AreEqual(1, GeneratedTintMaterials.CachedCount);
                Assert.AreEqual(tint, a.GetComponent<Renderer>().sharedMaterial.GetColor("_BaseColor"));
            }
            finally
            {
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
            }
        }
    }
}
