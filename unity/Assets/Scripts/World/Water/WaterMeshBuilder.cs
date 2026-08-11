using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// Builds a Unity mesh directly from a <see cref="WaterZone"/>'s flooded-cell mask: one flat
    /// quad per flooded cell at the zone's own <see cref="WaterZone.WaterLevelY"/>, so the surface
    /// only exists where sampled terrain is actually at or below that height - never a plane
    /// bleeding across dry ground. Boundary cells get a thin vertical skirt down to the adjacent
    /// unflooded terrain's real sampled height, closing the seam between water edge and shore
    /// instead of leaving a visible gap or an underwater view straight through the shoreline.
    /// Shoreline follows the sampling grid resolution (blocky at low cell counts) - smoothing it
    /// (marching squares) is a follow-up polish item, not required to fix the water/terrain
    /// mismatch this replaces.
    /// </summary>
    public static class WaterMeshBuilder
    {
        private const float SkirtMargin = 0.25f;

        /// <param name="zone">The zone this mesh is being built for.</param>
        /// <param name="allZones">
        /// Every zone from the same generation, sharing the same grid. Skirts must check "is this
        /// neighbour cell wet in ANY zone", not just this one - two basins can be geometrically
        /// adjacent while still being separate <see cref="WaterZone"/> instances (each flood only
        /// claims its own cells), and checking only <paramref name="zone"/>'s own mask would read
        /// the other zone's water as "dry land" and drop a spurious wall down to real terrain right
        /// through the middle of what should be one continuous surface.
        /// </param>
        public static Mesh Build(WaterZone zone, IReadOnlyList<WaterZone> allZones)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                var baseIndex = vertices.Count;
                vertices.Add(a);
                vertices.Add(b);
                vertices.Add(c);
                vertices.Add(d);
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(0f, 1f));
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 3);
            }

            float? FindWaterLevelAt(int nx, int nz)
            {
                for (var i = 0; i < allZones.Count; i++)
                {
                    if (allZones[i].IsFlooded(nx, nz))
                    {
                        return allZones[i].WaterLevelY;
                    }
                }

                return null;
            }

            void AddSkirt(int nx, int nz, Vector3 topA, Vector3 topB)
            {
                var otherLevel = FindWaterLevelAt(nx, nz);
                if (otherLevel.HasValue)
                {
                    // Cell edges already meet at (near enough) the same height - no seam to cover.
                    if (Mathf.Abs(otherLevel.Value - zone.WaterLevelY) < 0.01f)
                    {
                        return;
                    }

                    // Neighbour belongs to a different zone sitting at a different level - connect
                    // down to that zone's own surface, not bare terrain, so the two water sheets
                    // meet cleanly instead of leaving a gap between them. Only the higher zone
                    // draws this (never the lower one looking back) so the step isn't built twice.
                    if (zone.WaterLevelY > otherLevel.Value)
                    {
                        var stepA = new Vector3(topA.x, otherLevel.Value, topA.z);
                        var stepB = new Vector3(topB.x, otherLevel.Value, topB.z);
                        AddQuad(topA, topB, stepB, stepA);
                    }

                    return;
                }

                var neighborHeight = zone.HeightAt(nx, nz);

                // Dry neighbour already sits at or above the water surface - an island or the
                // mainland naturally rising out of the lake. The terrain mesh already draws that
                // slope; a skirt down (really up) to it would just glue a redundant vertical wall
                // onto ground that was never a gap in the first place (the bug behind rings of
                // "cliffs" appearing around islands and along otherwise normal shoreline).
                if (neighborHeight >= zone.WaterLevelY)
                {
                    return;
                }

                var bottomY = neighborHeight - SkirtMargin;
                var bottomA = new Vector3(topA.x, bottomY, topA.z);
                var bottomB = new Vector3(topB.x, bottomY, topB.z);
                AddQuad(topA, topB, bottomB, bottomA);
            }

            var y = zone.WaterLevelY;
            for (var x = 0; x < zone.Width; x++)
            {
                for (var z = 0; z < zone.Depth; z++)
                {
                    if (!zone.IsFlooded(x, z))
                    {
                        continue;
                    }

                    var x0 = zone.OriginX + x * zone.CellSize;
                    var x1 = x0 + zone.CellSize;
                    var z0 = zone.OriginZ + z * zone.CellSize;
                    var z1 = z0 + zone.CellSize;

                    var p00 = new Vector3(x0, y, z0);
                    var p10 = new Vector3(x1, y, z0);
                    var p11 = new Vector3(x1, y, z1);
                    var p01 = new Vector3(x0, y, z1);

                    AddQuad(p00, p01, p11, p10);

                    AddSkirt(x - 1, z, p01, p00);
                    AddSkirt(x + 1, z, p10, p11);
                    AddSkirt(x, z - 1, p00, p10);
                    AddSkirt(x, z + 1, p11, p01);
                }
            }

            var mesh = new Mesh { name = $"WaterZoneMesh_{zone.Id}" };
            if (vertices.Count > 65000)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
