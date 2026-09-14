using UnityEngine;

namespace GameDev.World.Hydrology
{
    /// <summary>
    /// Scene-view visualisation of a generated drainage network.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Disabled by default and never part of a production generation. It exists so a bake can be
    /// judged by looking at it — which reach is which order, where the sources and outlets are, which
    /// drops became waterfall candidates — instead of only by reading a count in the console.
    /// </para>
    /// <para>
    /// The component holds no geography of its own; it renders whatever
    /// <see cref="HydrologyDefinition"/> it was handed. Nothing reads water position back from here.
    /// </para>
    /// </remarks>
    [ExecuteAlways]
    public sealed class HydrologyDiagnostics : MonoBehaviour
    {
        [SerializeField] private bool drawReaches = true;
        [SerializeField] private bool drawSources = true;
        [SerializeField] private bool drawOutlets = true;
        [SerializeField] private bool drawLakes = true;
        [SerializeField] private bool drawWaterfalls = true;

        /// <summary>Reach polylines are drawn at the water surface, lifted so they clear the mesh.</summary>
        [SerializeField] private float lift = 1.5f;

        private HydrologyDefinition definition;

        public void Bind(HydrologyDefinition value)
        {
            definition = value;
        }

        public HydrologyDefinition Definition => definition;

        private void OnDrawGizmos()
        {
            if (definition == null || definition.IsEmpty)
            {
                return;
            }

            if (drawReaches)
            {
                DrawReaches();
            }

            if (drawLakes)
            {
                DrawLakes();
            }

            if (drawWaterfalls)
            {
                DrawWaterfalls();
            }
        }

        private void DrawReaches()
        {
            for (var r = 0; r < definition.Reaches.Count; r++)
            {
                var reach = definition.Reaches[r];
                var nodes = reach.Nodes;

                // Order drives the colour, so the hierarchy the terrain produced is readable at a
                // glance: pale headwaters darkening into the main stems.
                var t = Mathf.Clamp01((reach.StrahlerOrder - 1) / 4f);
                Gizmos.color = Color.Lerp(new Color(0.5f, 0.85f, 1f), new Color(0f, 0.25f, 0.9f), t);

                for (var i = 1; i < nodes.Count; i++)
                {
                    Gizmos.DrawLine(Point(nodes[i - 1]), Point(nodes[i]));
                }

                if (drawSources && reach.IsSource && nodes.Count > 0)
                {
                    Gizmos.color = new Color(0.6f, 1f, 0.6f);
                    Gizmos.DrawWireSphere(Point(nodes[0]), 6f);
                }

                if (drawOutlets && reach.Termination == ReachTermination.Ocean && nodes.Count > 0)
                {
                    Gizmos.color = new Color(1f, 0.85f, 0.3f);
                    Gizmos.DrawWireSphere(Point(nodes[nodes.Count - 1]), 10f);
                }
            }
        }

        private void DrawLakes()
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f);
            for (var l = 0; l < definition.Lakes.Count; l++)
            {
                var lake = definition.Lakes[l];
                var y = lake.SurfaceElevation + lift;
                for (int i = 0, j = lake.Boundary.Count - 1; i < lake.Boundary.Count; j = i++)
                {
                    Gizmos.DrawLine(
                        new Vector3(lake.Boundary[j].x, y, lake.Boundary[j].y),
                        new Vector3(lake.Boundary[i].x, y, lake.Boundary[i].y));
                }

                Gizmos.DrawWireSphere(
                    new Vector3(lake.SpillPointXZ.x, y, lake.SpillPointXZ.y), 8f);
            }
        }

        private void DrawWaterfalls()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.9f);
            for (var w = 0; w < definition.Waterfalls.Count; w++)
            {
                var candidate = definition.Waterfalls[w];
                var reach = definition.FindReach(candidate.Reach);
                if (reach == null || candidate.NodeIndex >= reach.Nodes.Count)
                {
                    continue;
                }

                var node = reach.Nodes[candidate.NodeIndex];
                var top = new Vector3(
                    node.PositionXZ.x, node.WaterElevation + lift, node.PositionXZ.y);
                Gizmos.DrawLine(top, top + Vector3.down * candidate.DropMeters);
                Gizmos.DrawWireCube(top, new Vector3(8f, 0.5f, 8f));
            }
        }

        private Vector3 Point(RiverNode node) =>
            new Vector3(node.PositionXZ.x, node.WaterElevation + lift, node.PositionXZ.y);
    }
}
