using System.Linq;
using GameDev.Slice.Atmosphere;
using GameDev.Slice.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GameDev.Slice.Tests.EditMode
{
    /// <summary>
    /// The height fog (reference plan R3 step 5): its pass stands once on the project's renderer with its shader, and
    /// each camera's fog shows the lit lamps nearest the eye, leaving out those behind it and those beyond its reach.
    /// </summary>
    public sealed class HeightFogTests
    {
        [TearDown]
        public void ClearLamps() => HeightFog.SetLamps(null);

        [Test]
        public void TheRendererCarriesTheHeightFogOnce()
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(VeilmouthHeightFogSetup.RendererPath);
            var fogs = data.rendererFeatures.OfType<HeightFogFeature>().ToList();
            Assert.AreEqual(1, fogs.Count);
            Assert.IsTrue(fogs[0].isActive);
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Shader>(VeilmouthHeightFogSetup.ShaderPath), fogs[0].Shader);
            Assert.IsTrue(fogs[0].Shader.isSupported, "the fog's shader compiles");
        }

        [Test]
        public void TheNearestLampsAheadComeFirst()
        {
            var lamps = Enumerable.Range(0, 30).Select(i => (new Vector3(0f, 4f, 5f + (4f * i)), new Color(i, 0f, 0f, 1.5f))).ToList();
            lamps.Add((new Vector3(0f, 4f, -20f), Color.blue));  // behind the eye
            lamps.Add((new Vector3(0f, 4f, -2f), Color.green));  // just behind: still lights the fog ahead
            lamps.Add((new Vector3(0f, 4f, HeightFog.LampReach + 10f), Color.white)); // beyond the reach
            HeightFog.SetLamps(Enumerable.Reverse(lamps));

            var positions = new Vector4[HeightFog.MaxLamps];
            var colours = new Vector4[HeightFog.MaxLamps];
            var count = HeightFog.Nearest(Vector3.zero, Vector3.forward, positions, colours);

            Assert.AreEqual(HeightFog.MaxLamps, count);
            Assert.AreEqual(Color.green.g, colours[0].y, "the lamp just behind the eye is the nearest");
            for (var i = 1; i < count; i++)
            {
                Assert.AreEqual(i - 1, colours[i].x, $"lamp {i} is the {i}th nearest ahead");
                Assert.AreEqual(1.5f, colours[i].w, "the halo's radius rides with the colour");
            }

            Assert.IsFalse(colours.Take(count).Any(c => c.z > 0f), "the lamp well behind the eye is left out");
        }

        [Test]
        public void NoLampsByDay()
        {
            HeightFog.SetLamps(null);
            Assert.AreEqual(0, HeightFog.Nearest(Vector3.zero, Vector3.forward, new Vector4[HeightFog.MaxLamps], new Vector4[HeightFog.MaxLamps]));
        }
    }
}
