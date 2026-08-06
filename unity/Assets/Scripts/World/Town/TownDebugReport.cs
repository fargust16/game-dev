using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using GameDev.World.Town.Geometry;
using UnityEngine;

namespace GameDev.World.Town
{
    /// <summary>
    /// Agent-readable top-down dump of <see cref="TownModel"/>: Markdown metrics + SVG map.
    /// Used to diagnose geometry viability without Scene view.
    /// </summary>
    public static class TownDebugReport
    {
        public const string DefaultOutputDir = "Assets/Plans/debug";

        public sealed class Result
        {
            public string MarkdownPath;
            public string SvgPath;
            public int RoadSamples;
            public int RoadSamplesInsideBuildings;
            public int RoadSamplesInsideBlocks;
            public int Buildings;
            public int BuildingsWithTemplate;
            public int BuildingsOverlappingRoads;
            public int RoadEdges;
            public int DeadEndEdges;
            public int OrphanDegree1Nodes;
            public int PortalCount;
            public int Blocks;
            public int Plots;
            public float BuildingCoverageOfBounds;
            public List<string> Findings;
        }

        public static Result Write(TownModel model, string outputDir, string fileStem)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (string.IsNullOrEmpty(outputDir))
            {
                outputDir = DefaultOutputDir;
            }

            if (string.IsNullOrEmpty(fileStem))
            {
                fileStem = $"town_seed_{model.Seed}";
            }

            Directory.CreateDirectory(outputDir);

            var result = Analyze(model);
            var mdPath = Path.Combine(outputDir, fileStem + "_report.md");
            var svgPath = Path.Combine(outputDir, fileStem + "_map.svg");
            File.WriteAllText(mdPath, BuildMarkdown(model, result), Encoding.UTF8);
            File.WriteAllText(svgPath, BuildSvg(model), Encoding.UTF8);
            result.MarkdownPath = mdPath.Replace('\\', '/');
            result.SvgPath = svgPath.Replace('\\', '/');
            return result;
        }

        public static Result Analyze(TownModel model)
        {
            var r = new Result
            {
                Buildings = model.Buildings.Count,
                RoadEdges = model.RoadEdges.Count,
                PortalCount = model.Portals.Count,
                Blocks = model.Blocks.Count,
                Plots = model.Plots.Count,
                Findings = new List<string>(16)
            };

            for (var i = 0; i < model.Buildings.Count; i++)
            {
                if (model.Buildings[i].HasTemplate)
                {
                    r.BuildingsWithTemplate++;
                }
            }

            for (var i = 0; i < model.RoadEdges.Count; i++)
            {
                if (model.RoadEdges[i].DeadEnd)
                {
                    r.DeadEndEdges++;
                }
            }

            var degree = new int[model.RoadNodes.Count];
            for (var i = 0; i < model.RoadEdges.Count; i++)
            {
                var e = model.RoadEdges[i];
                if (e.NodeA >= 0 && e.NodeA < degree.Length) degree[e.NodeA]++;
                if (e.NodeB >= 0 && e.NodeB < degree.Length) degree[e.NodeB]++;
            }

            for (var i = 0; i < degree.Length; i++)
            {
                if (degree[i] == 1)
                {
                    r.OrphanDegree1Nodes++;
                }
            }

            const float sampleStep = 3f;
            var buildingHit = new bool[model.Buildings.Count];
            for (var ei = 0; ei < model.RoadEdges.Count; ei++)
            {
                var poly = model.RoadEdges[ei].Polyline;
                if (poly == null || poly.Count < 2)
                {
                    continue;
                }

                for (var i = 0; i < poly.Count - 1; i++)
                {
                    var a = poly[i];
                    var b = poly[i + 1];
                    var len = Vector2.Distance(a, b);
                    var steps = Mathf.Max(1, Mathf.CeilToInt(len / sampleStep));
                    for (var s = 0; s <= steps; s++)
                    {
                        var p = Vector2.Lerp(a, b, s / (float)steps);
                        r.RoadSamples++;

                        var inBuilding = false;
                        for (var bi = 0; bi < model.Buildings.Count; bi++)
                        {
                            var foot = model.Buildings[bi].Footprint;
                            if (foot == null || foot.Count < 3)
                            {
                                continue;
                            }

                            if (Geo2.PointInPolygon(p, foot.Vertices))
                            {
                                inBuilding = true;
                                buildingHit[bi] = true;
                            }
                        }

                        if (inBuilding)
                        {
                            r.RoadSamplesInsideBuildings++;
                        }

                        for (var bi = 0; bi < model.Blocks.Count; bi++)
                        {
                            var outline = model.Blocks[bi].Outline;
                            if (outline != null && outline.Count >= 3 &&
                                Geo2.PointInPolygon(p, outline.Vertices))
                            {
                                r.RoadSamplesInsideBlocks++;
                                break;
                            }
                        }
                    }
                }
            }

            for (var i = 0; i < buildingHit.Length; i++)
            {
                if (buildingHit[i])
                {
                    r.BuildingsOverlappingRoads++;
                }
            }

            var boundsArea = model.Bounds != null ? model.Bounds.Area : 0f;
            var buildingArea = 0f;
            for (var i = 0; i < model.Buildings.Count; i++)
            {
                if (model.Buildings[i].Footprint != null)
                {
                    buildingArea += model.Buildings[i].Footprint.Area;
                }
            }

            r.BuildingCoverageOfBounds = boundsArea > 1f ? buildingArea / boundsArea : 0f;

            // Findings — plain language for agents.
            if (r.RoadSamples > 0)
            {
                var pctBuild = 100f * r.RoadSamplesInsideBuildings / r.RoadSamples;
                var pctBlock = 100f * r.RoadSamplesInsideBlocks / r.RoadSamples;
                if (pctBuild > 15f)
                {
                    r.Findings.Add(
                        $"FAIL roads-vs-buildings: {pctBuild:F1}% of road centreline samples lie inside building footprints " +
                        $"({r.RoadSamplesInsideBuildings}/{r.RoadSamples}). Streets are not carved out of fabric.");
                }
                else if (pctBuild > 5f)
                {
                    r.Findings.Add(
                        $"WARN roads-vs-buildings: {pctBuild:F1}% road samples inside footprints.");
                }
                else
                {
                    r.Findings.Add(
                        $"OK roads-vs-buildings: only {pctBuild:F1}% road samples inside footprints.");
                }

                if (pctBlock > 50f)
                {
                    r.Findings.Add(
                        $"FAIL roads-vs-blocks: {pctBlock:F1}% of road samples lie inside block polygons. " +
                        "Likely Stage_Streets draws edges through block interiors (centroid-to-centroid) " +
                        "instead of along split corridors with inset blocks.");
                }
                else if (pctBlock > 20f)
                {
                    r.Findings.Add($"WARN roads-vs-blocks: {pctBlock:F1}% road samples inside block polygons.");
                }
                else
                {
                    r.Findings.Add($"OK roads-vs-blocks: only {pctBlock:F1}% road samples inside block polygons.");
                }
            }

            if (r.BuildingCoverageOfBounds > 0.55f)
            {
                r.Findings.Add(
                    $"WARN building coverage: footprints cover {r.BuildingCoverageOfBounds * 100f:F1}% of town bounds " +
                    "(little residual street/plaza void in the data model).");
            }

            var expectedStubs = r.PortalCount + r.DeadEndEdges;
            if (r.OrphanDegree1Nodes > expectedStubs * 3 + 24)
            {
                r.Findings.Add(
                    $"WARN dangling nodes: {r.OrphanDegree1Nodes} degree-1 road nodes " +
                    $"(portals={r.PortalCount}, marked dead-ends={r.DeadEndEdges}). Many stubs go nowhere.");
            }
            else
            {
                r.Findings.Add(
                    $"OK stubs: degree-1 nodes={r.OrphanDegree1Nodes} " +
                    $"(portals={r.PortalCount}, dead-ends={r.DeadEndEdges}).");
            }

            if (model.Blocks.Count > 0 && model.RoadEdges.Count < model.Blocks.Count * 0.3f)
            {
                r.Findings.Add(
                    $"WARN sparse street graph vs blocks: roads={model.RoadEdges.Count}, blocks={model.Blocks.Count}.");
            }

            var assignable = 0;
            for (var i = 0; i < model.Buildings.Count; i++)
            {
                if (model.Buildings[i].Kind != BuildingKind.Landmark)
                {
                    assignable++;
                }
            }

            if (assignable > 0)
            {
                var pctT = 100f * r.BuildingsWithTemplate / assignable;
                if (pctT < 20f)
                {
                    r.Findings.Add(
                        $"WARN templates: only {r.BuildingsWithTemplate}/{assignable} non-landmark buildings " +
                        $"have interiors ({pctT:F0}%). Catalog may be too large vs footprints.");
                }
                else
                {
                    r.Findings.Add(
                        $"OK templates: {r.BuildingsWithTemplate}/{assignable} non-landmark buildings " +
                        $"have interiors ({pctT:F0}%).");
                }
            }

            if (r.Findings.Count == 0)
            {
                r.Findings.Add("No automatic red flags. Still review SVG layers visually.");
            }

            return r;
        }

        private static string BuildMarkdown(TownModel model, Result r)
        {
            var sb = new StringBuilder(4096);
            sb.AppendLine("# Town Debug Report");
            sb.AppendLine();
            sb.AppendLine($"- Seed: `{model.Seed}`");
            sb.AppendLine($"- Center: ({F(model.Center.x)}, {F(model.Center.y)})");
            sb.AppendLine($"- Bounds verts: {model.Bounds?.Count ?? 0}, area≈{F(model.Bounds?.Area ?? 0f)} m²");
            sb.AppendLine($"- Landmark: {(model.Landmark != null ? model.Landmark.Kind.ToString() : "none")}");
            sb.AppendLine();
            sb.AppendLine("## Counts");
            sb.AppendLine();
            sb.AppendLine("| Layer | Count |");
            sb.AppendLine("|-------|------:|");
            sb.AppendLine($"| Portals | {r.PortalCount} |");
            sb.AppendLine($"| Road nodes | {model.RoadNodes.Count} |");
            sb.AppendLine($"| Road edges | {r.RoadEdges} |");
            sb.AppendLine($"| Dead-end edges | {r.DeadEndEdges} |");
            sb.AppendLine($"| Degree-1 nodes | {r.OrphanDegree1Nodes} |");
            sb.AppendLine($"| Districts | {model.Districts.Count} |");
            sb.AppendLine($"| Blocks | {r.Blocks} |");
            sb.AppendLine($"| Plots | {r.Plots} |");
            sb.AppendLine($"| Buildings | {r.Buildings} |");
            sb.AppendLine($"| Buildings with template | {r.BuildingsWithTemplate} |");
            sb.AppendLine($"| Open spaces | {model.OpenSpaces.Count} |");
            sb.AppendLine();
            sb.AppendLine("## Overlap / coverage probes");
            sb.AppendLine();
            sb.AppendLine($"- Road centreline samples: **{r.RoadSamples}**");
            sb.AppendLine(
                $"- Samples inside **building** footprints: **{r.RoadSamplesInsideBuildings}** " +
                $"({Pct(r.RoadSamplesInsideBuildings, r.RoadSamples)})");
            sb.AppendLine(
                $"- Samples inside **block** polygons: **{r.RoadSamplesInsideBlocks}** " +
                $"({Pct(r.RoadSamplesInsideBlocks, r.RoadSamples)})");
            sb.AppendLine(
                $"- Buildings touched by a road sample: **{r.BuildingsOverlappingRoads}** / {r.Buildings}");
            sb.AppendLine($"- Building footprint area / bounds area: **{r.BuildingCoverageOfBounds * 100f:F1}%**");
            sb.AppendLine();
            sb.AppendLine("## Findings");
            sb.AppendLine();
            for (var i = 0; i < r.Findings.Count; i++)
            {
                sb.AppendLine($"{i + 1}. {r.Findings[i]}");
            }

            sb.AppendLine();
            sb.AppendLine("## How to read the SVG");
            sb.AppendLine();
            sb.AppendLine("- White stroke: town boundary");
            sb.AppendLine("- Amber thick: arterial roads; orange/yellow/grey: secondary→alley");
            sb.AppendLine("- Dark outlines: blocks");
            sb.AppendLine("- Brown filled: building footprints");
            sb.AppendLine("- Magenta: landmark footprint; light gold: landmark plaza");
            sb.AppendLine("- If amber lines cut through brown fills, streets are not corridors — data bug, not baker-only.");
            sb.AppendLine();
            sb.AppendLine("## Road class histogram");
            sb.AppendLine();
            var hist = new int[5];
            for (var i = 0; i < model.RoadEdges.Count; i++)
            {
                var c = (int)model.RoadEdges[i].Class;
                if (c >= 0 && c < hist.Length) hist[c]++;
            }

            sb.AppendLine($"- Arterial: {hist[0]}");
            sb.AppendLine($"- Secondary: {hist[1]}");
            sb.AppendLine($"- Tertiary: {hist[2]}");
            sb.AppendLine($"- Alley: {hist[3]}");
            sb.AppendLine($"- Service: {hist[4]}");
            sb.AppendLine();
            sb.AppendLine("## Sample building footprints (first 8)");
            sb.AppendLine();
            var n = Mathf.Min(8, model.Buildings.Count);
            for (var i = 0; i < n; i++)
            {
                var b = model.Buildings[i];
                var c = b.Footprint != null ? b.Footprint.Centroid : Vector2.zero;
                sb.AppendLine(
                    $"- #{i} {b.Kind} template={b.TemplateId} floors={F(b.Floors)} verts={b.Footprint?.Count ?? 0} " +
                    $"centroid=({F(c.x)},{F(c.y)}) area={F(b.Footprint?.Area ?? 0f)}");
            }

            return sb.ToString();
        }

        private static string BuildSvg(TownModel model)
        {
            GetWorldBounds(model, out var minX, out var minY, out var maxX, out var maxY);
            var pad = 20f;
            minX -= pad;
            minY -= pad;
            maxX += pad;
            maxY += pad;
            var w = Mathf.Max(1f, maxX - minX);
            var h = Mathf.Max(1f, maxY - minY);

            // SVG Y grows down; flip world Y so north stays visually up.
            string X(float x) => F(x - minX);
            string Y(float y) => F(maxY - y);

            var sb = new StringBuilder(64 * 1024);
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine(
                $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1200\" height=\"{F(1200f * h / w)}\" " +
                $"viewBox=\"0 0 {F(w)} {F(h)}\">");
            sb.AppendLine("<rect width=\"100%\" height=\"100%\" fill=\"#1a1a1e\"/>");
            sb.AppendLine("<!-- districts -->");
            for (var i = 0; i < model.Districts.Count; i++)
            {
                AppendPoly(sb, model.Districts[i].Area, X, Y, "none", WealthStroke(model.Districts[i].Wealth), 0.8f, 0.25f);
            }

            sb.AppendLine("<!-- blocks -->");
            for (var i = 0; i < model.Blocks.Count; i++)
            {
                AppendPoly(sb, model.Blocks[i].Outline, X, Y, "none", "#444450", 0.6f, 1f);
            }

            sb.AppendLine("<!-- buildings -->");
            for (var i = 0; i < model.Buildings.Count; i++)
            {
                var fill = model.Buildings[i].Kind == BuildingKind.Landmark ? "#aa44aa" : "#8a6a3a";
                AppendPoly(sb, model.Buildings[i].Footprint, X, Y, fill, "#5a4020", 0.55f, 0.4f);
            }

            if (model.Landmark != null)
            {
                sb.AppendLine("<!-- landmark plaza + footprint -->");
                AppendPoly(sb, model.Landmark.Plaza, X, Y, "#c9b86a", "#c9b86a", 0.25f, 1f);
                AppendPoly(sb, model.Landmark.Footprint, X, Y, "#d060d0", "#ff80ff", 0.7f, 1.2f);
            }

            sb.AppendLine("<!-- roads (on top so overlaps are obvious) -->");
            for (var i = 0; i < model.RoadEdges.Count; i++)
            {
                var e = model.RoadEdges[i];
                if (e.Polyline == null || e.Polyline.Count < 2)
                {
                    continue;
                }

                var stroke = RoadStroke(e.Class);
                var sw = Mathf.Max(1.2f, e.Width * 0.35f);
                sb.Append($"<polyline fill=\"none\" stroke=\"{stroke}\" stroke-width=\"{F(sw)}\" stroke-opacity=\"0.9\" points=\"");
                for (var p = 0; p < e.Polyline.Count; p++)
                {
                    if (p > 0) sb.Append(' ');
                    sb.Append(X(e.Polyline[p].x)).Append(',').Append(Y(e.Polyline[p].y));
                }

                sb.AppendLine("\"/>");
            }

            sb.AppendLine("<!-- portals -->");
            for (var i = 0; i < model.Portals.Count; i++)
            {
                var p = model.Portals[i].Pos;
                sb.AppendLine(
                    $"<circle cx=\"{X(p.x)}\" cy=\"{Y(p.y)}\" r=\"4\" fill=\"#40e0ff\" stroke=\"#fff\" stroke-width=\"1\"/>");
            }

            if (model.Bounds != null)
            {
                AppendPoly(sb, model.Bounds, X, Y, "none", "#ffffff", 1f, 2f);
            }

            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        private static void AppendPoly(
            StringBuilder sb,
            Polygon2 poly,
            Func<float, string> X,
            Func<float, string> Y,
            string fill,
            string stroke,
            float fillOpacity,
            float strokeWidth)
        {
            if (poly == null || poly.Count < 2)
            {
                return;
            }

            sb.Append($"<polygon fill=\"{fill}\" fill-opacity=\"{F(fillOpacity)}\" stroke=\"{stroke}\" " +
                      $"stroke-width=\"{F(strokeWidth)}\" points=\"");
            for (var i = 0; i < poly.Count; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(X(poly[i].x)).Append(',').Append(Y(poly[i].y));
            }

            sb.AppendLine("\"/>");
        }

        private static void GetWorldBounds(
            TownModel model, out float minX, out float minY, out float maxX, out float maxY)
        {
            var loX = float.MaxValue;
            var loY = float.MaxValue;
            var hiX = float.MinValue;
            var hiY = float.MinValue;

            if (model.Bounds != null)
            {
                for (var i = 0; i < model.Bounds.Count; i++)
                {
                    AccPoint(model.Bounds[i], ref loX, ref loY, ref hiX, ref hiY);
                }
            }

            for (var i = 0; i < model.RoadEdges.Count; i++)
            {
                var pl = model.RoadEdges[i].Polyline;
                if (pl == null)
                {
                    continue;
                }

                for (var j = 0; j < pl.Count; j++)
                {
                    AccPoint(pl[j], ref loX, ref loY, ref hiX, ref hiY);
                }
            }

            if (loX > hiX)
            {
                minX = -100f;
                minY = -100f;
                maxX = 100f;
                maxY = 100f;
                return;
            }

            minX = loX;
            minY = loY;
            maxX = hiX;
            maxY = hiY;
        }

        private static void AccPoint(Vector2 v, ref float loX, ref float loY, ref float hiX, ref float hiY)
        {
            if (v.x < loX) loX = v.x;
            if (v.y < loY) loY = v.y;
            if (v.x > hiX) hiX = v.x;
            if (v.y > hiY) hiY = v.y;
        }

        private static string RoadStroke(RoadClass c)
        {
            switch (c)
            {
                case RoadClass.Arterial: return "#e8a838";
                case RoadClass.Secondary: return "#e07a30";
                case RoadClass.Tertiary: return "#d4c04a";
                case RoadClass.Alley: return "#9a9aa0";
                default: return "#6a6a70";
            }
        }

        private static string WealthStroke(WealthClass w)
        {
            switch (w)
            {
                case WealthClass.Rich: return "#3d8f5a";
                case WealthClass.Poor: return "#a04040";
                default: return "#3d7a7a";
            }
        }

        private static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        private static string Pct(int part, int whole) =>
            whole <= 0 ? "n/a" : (100f * part / whole).ToString("0.0", CultureInfo.InvariantCulture) + "%";
    }
}
