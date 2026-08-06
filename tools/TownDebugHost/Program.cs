using System;
using System.Collections.Generic;
using System.IO;
using GameDev.World;
using GameDev.World.Town;
using UnityEngine;

namespace TownDebugHost
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var seed = WorldSeed.DefaultPrototypeSeed;
            if (args.Length > 0 && int.TryParse(args[0], out var parsed))
            {
                seed = parsed;
            }

            var profile = ScriptableObject.CreateInstance<TownProfile>();
            var model = Generate(profile, seed);

            var outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "Plans", "debug"));
            Directory.CreateDirectory(outDir);
            var result = TownDebugReport.Write(model, outDir, $"town_seed_{seed}");

            Console.WriteLine("=== TownDebugHost ===");
            Console.WriteLine($"seed={seed}");
            Console.WriteLine($"md={result.MarkdownPath}");
            Console.WriteLine($"svg={result.SvgPath}");
            Console.WriteLine(
                $"roads_in_buildings={result.RoadSamplesInsideBuildings}/{result.RoadSamples} " +
                $"roads_in_blocks={result.RoadSamplesInsideBlocks}/{result.RoadSamples}");
            Console.WriteLine($"building_coverage={result.BuildingCoverageOfBounds * 100f:F1}%");
            Console.WriteLine($"templated={result.BuildingsWithTemplate}/{result.Buildings}");
            Console.WriteLine(
                $"sockets={result.SocketCount} npc={result.NpcSocketCount} " +
                $"prop={result.PropSocketCount} keepers={result.KeeperSocketCount}");
            foreach (var f in result.Findings)
            {
                Console.WriteLine("FINDING: " + f);
            }

            return 0;
        }

        private static TownModel Generate(TownProfile profile, int seed)
        {
            var model = new TownModel();
            model.Reset(seed);
            var ws = new WorldSeed(seed);
            var log = new List<string>();
            Stage_SiteAndLandmark.Run(model, profile, ws.Fork("site"));
            Stage_PrimaryRoads.Run(model, profile, ws.Fork("primaryRoads"));
            Stage_Districts.Run(model, profile, ws.Fork("districts"));
            Stage_Streets.Run(model, profile, ws.Fork("streets"));
            Stage_Plots.Run(model, profile, ws.Fork("plots"));
            Stage_Buildings.Run(model, profile, ws.Fork("buildings"));
            Stage_AssignTemplates.Run(model, profile, ws.Fork("templates"));
            Stage_OpenSpaces.Run(model, profile, ws.Fork("openSpaces"));
            Stage_ConnectivityAndScale.Run(model, profile, ws.Fork("connectivity"), log);
            return model;
        }
    }
}
