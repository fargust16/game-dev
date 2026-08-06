using System.Collections.Generic;
using System.IO;
using GameDev.World;
using GameDev.World.Town;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Town.Tests
{
    public sealed class TownDebugReportTests
    {
        [Test]
        public void ExportPrototypeSeed_WritesReadableArtifacts()
        {
            var profile = ScriptableObject.CreateInstance<TownProfile>();
            const int seed = WorldSeed.DefaultPrototypeSeed;
            var model = Generate(profile, seed);

            var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "Plans/debug"));
            var result = TownDebugReport.Write(model, dir, $"town_seed_{seed}");

            Assert.IsTrue(File.Exists(result.MarkdownPath), "markdown missing");
            Assert.IsTrue(File.Exists(result.SvgPath), "svg missing");
            Assert.Greater(result.RoadSamples, 0, "expected road samples");
            Assert.Greater(result.Findings.Count, 0);

            // Persist findings into the console for batchmode logs.
            foreach (var finding in result.Findings)
            {
                Debug.Log("[TownDebugTest] " + finding);
            }

            Object.DestroyImmediate(profile);
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
