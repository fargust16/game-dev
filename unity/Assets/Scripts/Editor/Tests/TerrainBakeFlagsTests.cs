using GameDev.World;
using GameDev.World.Terrain;
using NUnit.Framework;

namespace GameDev.World.Terrain.Tests
{
    /// <summary>
    /// The bake-flag contract the pipeline documents: a step flag has to actually reach the bake.
    /// </summary>
    /// <remarks>
    /// These flags were set by <c>OverworldPipeline</c> and never read, so "Mountains off" still
    /// baked massifs and the "Zones" step was a full duplicate bake with no effect. The behavioural
    /// half is asserted by the terrain bake tests (an island-only bake has no massif uplift); this
    /// fixture guards the wiring that made it possible to go unnoticed.
    /// </remarks>
    public sealed class TerrainBakeFlagsTests
    {
        [Test]
        public void Full_IncludesMountains()
        {
            Assert.IsTrue((TerrainBakeFlags.Full & TerrainBakeFlags.IncludeMountains) != 0);
        }

        [Test]
        public void None_ExcludesMountains()
        {
            Assert.IsTrue((TerrainBakeFlags.None & TerrainBakeFlags.IncludeMountains) == 0);
        }

        [Test]
        public void ClearingMountains_LeavesTheRestOfTheBakeIntact()
        {
            // How ClearStep(Mountains) re-bakes: drop the massifs, keep everything else.
            var withoutMountains = TerrainBakeFlags.Full & ~TerrainBakeFlags.IncludeMountains;
            Assert.AreEqual(TerrainBakeFlags.None, withoutMountains);
        }

        [Test]
        public void EveryPipelineStep_HasASkipFlag()
        {
            // A step that IsStepEnabled does not know about cannot be skipped from the window, and
            // silently runs on every Generate All.
            var allOff = WorldGenSettings.Default;
            allOff.RunSurface = false;
            allOff.RunMountains = false;
            allOff.RunHydrology = false;
            allOff.RunMarkers = false;
            allOff.RunSettlements = false;
            allOff.RunRoads = false;
            allOff.RunPlayer = false;

            foreach (OverworldPipelineStepId step in
                     System.Enum.GetValues(typeof(OverworldPipelineStepId)))
            {
                Assert.IsFalse(
                    allOff.IsStepEnabled(step),
                    $"step {step} has no WorldGenSettings flag, so it can never be skipped");
            }
        }

        [Test]
        public void OrderedSteps_CoverEveryStepExactlyOnce()
        {
            var declared = System.Enum.GetValues(typeof(OverworldPipelineStepId));
            var ordered = GameDev.EditorTools.OverworldPipeline.OrderedSteps;

            Assert.AreEqual(declared.Length, ordered.Length);
            CollectionAssert.AllItemsAreUnique(ordered);
            foreach (OverworldPipelineStepId step in declared)
            {
                CollectionAssert.Contains(ordered, step);
            }
        }

        [Test]
        public void SettlementRunsBeforeRoads()
        {
            // Roads route to OverworldSettlementAnchor, which only exists once the settlement has
            // landed; the other order silently sends the road to the raw POI marker instead.
            var ordered = GameDev.EditorTools.OverworldPipeline.OrderedSteps;
            var settlements = System.Array.IndexOf(ordered, OverworldPipelineStepId.Settlements);
            var roads = System.Array.IndexOf(ordered, OverworldPipelineStepId.Roads);

            Assert.Greater(settlements, -1);
            Assert.Greater(roads, settlements);
        }
    }
}
