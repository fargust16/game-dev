using System.Linq;
using GameDev.Core.Scenes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// The shader warm-up's collection (Plans/VEILMOUTH_REFERENCE_PLAN.md R1.5): the desktop build warms what the
    /// development build traced on Direct3D 12 at the quality level High, so that collection must be there.
    /// </summary>
    public sealed class ShaderWarmUpTests
    {
        [Test]
        public void TheDesktopBuildHasACollectionToWarm()
        {
            var all = Resources.LoadAll<GraphicsStateCollection>(ShaderWarmUp.Folder);
            var desktop = all.SingleOrDefault(c => c.graphicsDeviceType == GraphicsDeviceType.Direct3D12 && c.runtimePlatform == RuntimePlatform.WindowsPlayer);

            Assert.That(desktop, Is.Not.Null, $"no Direct3D 12 Windows collection among {all.Length} in Resources/{ShaderWarmUp.Folder}");
            Assert.That(desktop.qualityLevelName, Is.EqualTo("High"));
            Assert.That(desktop.variantCount, Is.GreaterThan(0));
            Assert.That(desktop.totalGraphicsStateCount, Is.GreaterThanOrEqualTo(desktop.variantCount));
        }
    }
}
