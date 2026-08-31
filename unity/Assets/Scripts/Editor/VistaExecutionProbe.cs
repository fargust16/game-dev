#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using Pinwheel.Vista.Graph;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Diagnostic: logs which nodes Vista actually executes, and in what order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A wired edge is not proof of a runtime binding. Vista resolves what to run in
    /// <c>GraphContext.BuildExecutionSequence</c>, walking every input slot back from the requested
    /// output nodes; a producer that the walk misses is never executed, and the consumer then binds
    /// <c>Texture2D.blackTexture</c> silently - no warning, no error, just a mask of zeros. That
    /// failure mode has cost this project several bakes, most recently a hardness mask that was
    /// present in the asset and had exactly zero effect on the terrain.
    /// </para>
    /// <para>
    /// <c>TerrainGraph.onBeforeGraphExecution</c> and <c>onBeforeNodeExecution</c> are public static
    /// hooks, so this needs no change to Vista and nothing in the pipeline behaves differently while
    /// it is attached.
    /// </para>
    /// </remarks>
    public static class VistaExecutionProbe
    {
        private static bool s_attached;
        private static readonly List<string> s_executed = new List<string>();
        private static string s_graphName;

        /// <summary>Node ids to call out by name in the report, with a label each.</summary>
        public static readonly Dictionary<string, string> Watched = new Dictionary<string, string>();

        public static void Attach(string graphNameFilter)
        {
            if (s_attached)
            {
                return;
            }

            s_graphName = graphNameFilter;
            TerrainGraph.onBeforeGraphExecution += OnGraphStart;
            TerrainGraph.onBeforeNodeExecution += OnNode;
            TerrainGraph.onAfterGraphExecution += OnGraphEnd;
            s_attached = true;
            Debug.Log($"[ExecProbe] Attached, watching graphs whose name contains '{graphNameFilter}'.");
        }

        private static bool Matches(TerrainGraph graph) =>
            graph != null && !string.IsNullOrEmpty(graph.name) && graph.name.Contains(s_graphName);

        private static void OnGraphStart(
            TerrainGraph graph, TerrainGenerationConfigs configs, string[] nodeIds)
        {
            if (!Matches(graph))
            {
                return;
            }

            s_executed.Clear();
            var roots = new StringBuilder();
            foreach (var id in nodeIds)
            {
                var node = graph.GetNode(id);
                roots.Append($"{node?.GetType().Name ?? "?"}({id.Substring(0, 8)}) ");
            }

            Debug.Log(
                $"[ExecProbe] '{graph.name}' requested at {configs.resolution} px, "
                + $"{nodeIds.Length} root output(s): {roots}");
        }

        private static void OnNode(TerrainGraph graph, TerrainGenerationConfigs configs, INode node)
        {
            if (!Matches(graph))
            {
                return;
            }

            s_executed.Add(node.id);
        }

        private static void OnGraphEnd(
            TerrainGraph graph, TerrainGenerationConfigs configs, string[] nodeIds)
        {
            if (!Matches(graph))
            {
                return;
            }

            Debug.Log(
                $"[ExecProbe] '{graph.name}' executed {s_executed.Count} node(s).");

            foreach (var pair in Watched)
            {
                var ran = s_executed.Contains(pair.Key);
                var suffix = pair.Key.Length >= 8 ? pair.Key.Substring(0, 8) : pair.Key;
                Debug.Log(
                    $"[ExecProbe]   {pair.Value} ({suffix}): "
                    + (ran ? "EXECUTED" : "*** NOT IN EXECUTION PLAN ***"));
            }
        }
    }
}
#endif
