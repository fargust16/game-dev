using GameDev.Confront.Grid;
using GameDev.Slice.EditorTools;
using GameDev.World;
using NUnit.Framework;

namespace GameDev.Grid.Tests
{
    /// <summary>
    /// The Combat Grid lives in GameDev.Confront, which cannot reference the world's scale table, so it carries the
    /// person, the sea and the relief as its own numbers (S3a.3). They must stay WorldScale's.
    /// </summary>
    public sealed class CombatGridScaleTests
    {
        [Test]
        public void TheGridsPersonSeaAndReliefAreWorldScales()
        {
            Assert.AreEqual(WorldScale.CharacterRadiusMeters, CombatGridReader.Radius, "radius");
            Assert.AreEqual(WorldScale.CharacterHeightMeters, CombatGridReader.Height, "height");
            Assert.AreEqual(WorldScale.SeaLevelMeters, CombatGridReader.SeaLevel, "sea level");
            Assert.AreEqual(VeilmouthScene.SeaLevel, CombatGridReader.SeaLevel, "Veilmouth's sea");
            Assert.AreEqual(WorldScale.TerrainHeightBudgetMeters + 20f, CombatGridReader.TopY, "the top of a column");
        }
    }
}
