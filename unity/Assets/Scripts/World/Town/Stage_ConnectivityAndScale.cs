using System.Collections.Generic;
using GameDev.World.Town.Geometry;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Town
{
    public static class Stage_ConnectivityAndScale
    {
        public static void Run(TownModel model, TownProfile profile, Random rng, List<string> log)
        {
            EnsureGraphConnected(model, profile, rng, log);
            ValidateScale(model, profile, log);
            ValidateFrontage(model, log);
        }

        private static void EnsureGraphConnected(
            TownModel model, TownProfile profile, Random rng, List<string> log)
        {
            if (model.RoadNodes.Count == 0)
            {
                log.Add("No road nodes.");
                return;
            }

            // Find node closest to center as root.
            var root = 0;
            var best = float.MaxValue;
            for (var i = 0; i < model.RoadNodes.Count; i++)
            {
                var d = (model.RoadNodes[i].Pos - model.Center).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    root = i;
                }
            }

            var reachable = Flood(model, root);
            for (var i = 0; i < model.Portals.Count; i++)
            {
                var pi = FindNearestNode(model, model.Portals[i].Pos);
                if (!reachable[pi])
                {
                    ConnectNodes(model, profile, rng, root, pi);
                    reachable = Flood(model, root);
                    if (!reachable[pi])
                    {
                        log.Add($"Portal {i} still unreachable after corrective link.");
                    }
                }
            }

            // Landmark node ~ center.
            if (!reachable[root])
            {
                log.Add("Center/landmark hub unreachable.");
            }
        }

        private static bool[] Flood(TownModel model, int root)
        {
            var seen = new bool[model.RoadNodes.Count];
            var q = new Queue<int>();
            q.Enqueue(root);
            seen[root] = true;
            while (q.Count > 0)
            {
                var n = q.Dequeue();
                var edges = model.RoadNodes[n].Edges;
                for (var e = 0; e < edges.Count; e++)
                {
                    var edge = model.RoadEdges[edges[e]];
                    var other = edge.NodeA == n ? edge.NodeB : edge.NodeA;
                    if (other < 0 || other >= seen.Length || seen[other])
                    {
                        continue;
                    }

                    seen[other] = true;
                    q.Enqueue(other);
                }
            }

            return seen;
        }

        private static int FindNearestNode(TownModel model, Vector2 pos)
        {
            var best = 0;
            var bestD = float.MaxValue;
            for (var i = 0; i < model.RoadNodes.Count; i++)
            {
                var d = (model.RoadNodes[i].Pos - pos).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = i;
                }
            }

            return best;
        }

        private static void ConnectNodes(
            TownModel model, TownProfile profile, Random rng, int a, int b)
        {
            var from = model.RoadNodes[a].Pos;
            var to = model.RoadNodes[b].Pos;
            var poly = TownGraphUtil.BuildCurvedArterial(from, to, profile.Alley, rng, 2);
            TownGraphUtil.AddEdge(model, a, b, poly, RoadClass.Alley,
                TownGraphUtil.RoadWidth(profile, RoadClass.Alley, rng));
        }

        private static void ValidateScale(TownModel model, TownProfile profile, List<string> log)
        {
            for (var i = 0; i < model.RoadEdges.Count; i++)
            {
                var e = model.RoadEdges[i];
                var s = profile.GetRoadClassSettings(e.Class);
                if (e.Width < s.WidthMin * 0.75f || e.Width > s.WidthMax * 1.35f)
                {
                    log.Add($"Road {i} width {e.Width:F1}m outside soft bounds for {e.Class}.");
                }
            }

            if (model.Landmark != null)
            {
                var pr = Mathf.Sqrt(model.Landmark.Plaza.Area / Mathf.PI);
                if (pr < profile.PlazaSizeMin * 0.5f)
                {
                    log.Add("Landmark plaza smaller than expected.");
                }
            }
        }

        private static void ValidateFrontage(TownModel model, List<string> log)
        {
            var missing = 0;
            for (var i = 0; i < model.Plots.Count; i++)
            {
                if (!model.Plots[i].HasFrontage)
                {
                    missing++;
                }
            }

            if (missing > 0)
            {
                log.Add($"{missing} plots marked without frontage.");
            }
        }
    }
}
