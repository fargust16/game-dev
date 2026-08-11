using System.Linq;
using GameDev.World.Water;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Water.Tests
{
    public sealed class WaterMeshBuilderTests
    {
        [Test]
        public void Build_IsolatedCell_SkirtsDropToRealTerrainOnAllFourSides()
        {
            var heights = new float[3, 3];
            var flooded = new bool[3, 3];
            flooded[1, 1] = true;
            var zone = new WaterZone("z", 10f, 0f, 0f, 1f, flooded, heights);

            var mesh = WaterMeshBuilder.Build(zone, new[] { zone });

            // Top quad + 4 skirts, none of them touching another zone.
            Assert.AreEqual(20, mesh.vertexCount);
            Assert.IsTrue(mesh.vertices.Any(v => Mathf.Approximately(v.y, 10f)), "expected the flooded top surface");
            Assert.IsTrue(mesh.vertices.Any(v => Mathf.Approximately(v.y, -0.25f)),
                "expected skirts down to the (zero-height) surrounding terrain minus the skirt margin");
        }

        [Test]
        public void Build_AdjacentZonesAtDifferentLevels_HigherZoneStepsDownToLowerZonesSurface_NotBareTerrain()
        {
            var heights = new float[3, 1];
            for (var x = 0; x < 3; x++)
            {
                heights[x, 0] = 1f;
            }

            heights[0, 0] = 100f; // decoy: would leak into a bare-terrain skirt if the fix regressed

            var floodedLow = new bool[3, 1];
            floodedLow[0, 0] = true;
            var zoneLow = new WaterZone("low", 5f, 0f, 0f, 1f, floodedLow, heights);

            var floodedHigh = new bool[3, 1];
            floodedHigh[1, 0] = true;
            var zoneHigh = new WaterZone("high", 10f, 0f, 0f, 1f, floodedHigh, heights);

            var allZones = new WaterZone[] { zoneLow, zoneHigh };

            var highMesh = WaterMeshBuilder.Build(zoneHigh, allZones);
            Assert.IsTrue(highMesh.vertices.Any(v => Mathf.Approximately(v.y, 5f)),
                "the higher zone should step down to the lower zone's own water level");
            Assert.IsFalse(highMesh.vertices.Any(v => v.y > 50f),
                "it must not fall through to the bare-terrain skirt (decoy height 100) on the shared edge");

            var lowMesh = WaterMeshBuilder.Build(zoneLow, allZones);
            Assert.IsFalse(lowMesh.vertices.Any(v => Mathf.Approximately(v.y, 10f)),
                "the lower zone must not mirror the same step - only the higher zone draws it, or it would be built twice");
        }

        [Test]
        public void Build_DryNeighborAboveWaterLevel_NoSkirtDrawnTowardIt()
        {
            var heights = new float[3, 3];
            heights[2, 1] = 15f; // an island/high ground to the east, already above the water level

            var flooded = new bool[3, 3];
            flooded[1, 1] = true;
            var zone = new WaterZone("z", 10f, 0f, 0f, 1f, flooded, heights);

            var mesh = WaterMeshBuilder.Build(zone, new[] { zone });

            // Top quad + 3 skirts (west/north/south, all genuinely below water) - none east,
            // because that neighbour already sits above the water surface.
            Assert.AreEqual(16, mesh.vertexCount);
            Assert.IsFalse(mesh.vertices.Any(v => v.y > 10f),
                "no vertex should rise above the flooded top surface - a skirt toward the island would climb up to it instead of down");
        }
    }
}
