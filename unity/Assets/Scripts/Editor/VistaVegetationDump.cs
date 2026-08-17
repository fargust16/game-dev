#if UNITY_EDITOR
using System.Text;
using GameDev.World.Terrain;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Read-only inspection of the vegetation Vista actually wrote into each tile.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Vegetation dies quietly. A tree prototype Unity refuses takes the whole array with it; a
    /// detail prototype Unity refuses is dropped without a console line; a detail layer full of
    /// density is invisible if the view distance is short or the instances are scaled to a
    /// centimetre. Each of those looks identical from the scene view — "there is no grass" — so this
    /// walks the whole chain and prints the number that separates them.
    /// </para>
    /// <para>
    /// The chain, in order: template valid → prototype accepted by Unity → density written into the
    /// detail layer → instance size in metres → drawn at this distance. The first line that reads
    /// zero is the failure.
    /// </para>
    /// </remarks>
    public static class VistaVegetationDump
    {
        [MenuItem("GameDev/Overworld/Vista/Dump Vegetation State")]
        public static void DumpMenu()
        {
            var terrains = OverworldTerrainAccess.FindTerrains();
            if (terrains == null || terrains.Length == 0)
            {
                Debug.LogWarning("[VegDump] No overworld terrains in the scene.");
                return;
            }

            for (var i = 0; i < terrains.Length; i++)
            {
                DumpTile(terrains[i]);
            }
        }

        /// <summary>
        /// One line at the end of a bake saying what vegetation actually landed.
        /// </summary>
        /// <remarks>
        /// The whole reason this exists is that a missing detail prototype produces no console
        /// output whatsoever, so a bake that placed no grass at all looks exactly like a successful
        /// one in the log. Counting what reached the terrain turns that into a visible failure.
        /// <para>
        /// Detail layers are written by Vista's own progressive coroutines, which have not
        /// necessarily finished by the time the bake returns, so a zero here is a prompt rather than
        /// a verdict — confirm it with the menu dump, which runs long afterwards.
        /// </para>
        /// </remarks>
        public static void LogSummary(UnityEngine.Terrain[] terrains)
        {
            if (terrains == null || terrains.Length == 0)
            {
                return;
            }

            var treePrototypes = 0;
            var treeInstances = 0;
            var detailPrototypes = 0;
            long detailInstances = 0;

            foreach (var terrain in terrains)
            {
                if (terrain == null || terrain.terrainData == null)
                {
                    continue;
                }

                var data = terrain.terrainData;
                treePrototypes += data.treePrototypes.Length;
                treeInstances += data.treeInstances.Length;
                detailPrototypes += data.detailPrototypes.Length;

                var res = data.detailResolution;
                for (var i = 0; i < data.detailPrototypes.Length; i++)
                {
                    var layer = data.GetDetailLayer(0, 0, res, res, i);
                    for (var y = 0; y < res; y++)
                    {
                        for (var x = 0; x < res; x++)
                        {
                            detailInstances += layer[y, x];
                        }
                    }
                }

                ExportDetailDensityMaps(terrain, data, res);
            }

            var message =
                $"[Vegetation] After bake: treePrototypes={treePrototypes} trees={treeInstances} " +
                $"detailPrototypes={detailPrototypes} detailInstances={detailInstances}. " +
                $"Per-tile density maps exported to '{DetailDumpFolder}' — a total says grass exists, " +
                "only the map says where it does not.";

            if (detailPrototypes == 0 || detailInstances == 0)
            {
                Debug.LogWarning(
                    message + "\nNo ground cover at the end of the bake. Detail layers are written " +
                    "progressively, so confirm with 'GameDev/Overworld/Vista/Dump Vegetation State' " +
                    "before treating this as a failure — it reports the green weight the density " +
                    "mask was thresholded against, which says whether the graph or the write is at fault.");
            }
            else
            {
                Debug.Log(message);
            }
        }

        private static void DumpTile(UnityEngine.Terrain terrain)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                return;
            }

            var data = terrain.terrainData;
            var report = new StringBuilder();
            var pos = terrain.transform.position;
            report.AppendLine($"[VegDump] tile '{terrain.name}' at ({pos.x:F0},{pos.z:F0})");

            report.AppendLine(
                $"  view: detailObjectDistance={terrain.detailObjectDistance:F0}m " +
                $"detailObjectDensity={terrain.detailObjectDensity:F2} " +
                $"treeDistance={terrain.treeDistance:F0}m " +
                $"drawTreesAndFoliage={terrain.drawTreesAndFoliage} " +
                $"detailRes={data.detailResolution}/{data.detailResolutionPerPatch}");

            AppendGreenWeight(report, data);
            AppendDetails(report, terrain, data);
            AppendTrees(report, data);
            Debug.Log(report.ToString());
        }

        /// <summary>
        /// The painted grass weight, which stands in for the graph's <c>GreenAreaMask</c>.
        /// </summary>
        /// <remarks>
        /// Vegetation and the grass texture output are fed by the same Weight Blend slot, so the
        /// range of the painted layer is the range the vegetation masks were thresholded against.
        /// That makes this the number that separates the two ways ground cover can come out empty:
        /// a mask that never reaches the density node's floor, or a density map that reached the
        /// terrain as black for reasons outside the graph. A healthy green weight next to zero
        /// detail instances means the graph is fine and the write is not.
        /// </remarks>
        private static void AppendGreenWeight(StringBuilder report, TerrainData data)
        {
            var layers = data.terrainLayers;
            var green = System.Array.FindIndex(
                layers, layer => layer != null && layer.name.IndexOf("Grass", System.StringComparison.OrdinalIgnoreCase) >= 0);
            if (green < 0)
            {
                report.AppendLine("  green weight: no grass layer in this tile's stack");
                return;
            }

            var width = data.alphamapWidth;
            var height = data.alphamapHeight;
            var maps = data.GetAlphamaps(0, 0, width, height);
            var min = 1f;
            var max = 0f;
            var sum = 0.0;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var w = maps[y, x, green];
                    sum += w;
                    if (w < min)
                    {
                        min = w;
                    }

                    if (w > max)
                    {
                        max = w;
                    }
                }
            }

            var avg = sum / (width * (double)height);
            report.AppendLine(
                $"  green weight (layer {green} '{layers[green].name}'): min={min:F3} avg={avg:F3} max={max:F3}");
        }

        private static void AppendDetails(StringBuilder report, UnityEngine.Terrain terrain, TerrainData data)
        {
            var prototypes = data.detailPrototypes;
            report.AppendLine($"  detailPrototypes: {prototypes.Length}");
            if (prototypes.Length == 0)
            {
                report.AppendLine(
                    "    NONE — Vista wrote no detail prototype. Either the graph has no Detail " +
                    "Density Output, its Detail Template is null/invalid, or Unity rejected the " +
                    "prototype. Run 'Repair Vegetation Templates'.");
                return;
            }

            var res = data.detailResolution;
            for (var i = 0; i < prototypes.Length; i++)
            {
                var proto = prototypes[i];
                var valid = proto.Validate(out var error);
                var prefab = proto.prototype;
                var meshSize = MeshSizeMeters(prefab);

                report.AppendLine(
                    $"    [{i}] prefab='{(prefab != null ? prefab.name : "<null>")}' " +
                    $"mode={proto.renderMode} usePrototypeMesh={proto.usePrototypeMesh} " +
                    $"useInstancing={proto.useInstancing} " +
                    $"scale=[{proto.minHeight:F2}..{proto.maxHeight:F2}]x " +
                    $"mesh={meshSize:F2}m -> world=[{proto.minHeight * meshSize:F2}..{proto.maxHeight * meshSize:F2}]m " +
                    $"valid={valid}{(valid ? string.Empty : " ERROR: " + error)}");

                AppendMaterialInstancing(report, prefab, proto.useInstancing);

                var layer = data.GetDetailLayer(0, 0, res, res, i);
                long total = 0;
                var max = 0;
                var cells = 0;
                for (var y = 0; y < res; y++)
                {
                    for (var x = 0; x < res; x++)
                    {
                        var v = layer[y, x];
                        if (v <= 0)
                        {
                            continue;
                        }

                        total += v;
                        cells++;
                        if (v > max)
                        {
                            max = v;
                        }
                    }
                }

                var coverage = 100f * cells / Mathf.Max(1, res * res);
                report.AppendLine(
                    $"         density: instances={total} cells={cells} ({coverage:F1}% of map) max/cell={max}");
                if (total == 0)
                {
                    report.AppendLine(
                        "         ZERO — the prototype exists but no density was written. The graph's " +
                        "green mask reached the Detail Density Output as black, or the detail data was " +
                        "cleared after the bake (SetDetailResolution destroys it).");
                }
                else if (terrain.detailObjectDistance < 100f)
                {
                    report.AppendLine(
                        "         Density exists but detailObjectDistance is short — the grass is in " +
                        "the data and simply not drawn from here.");
                }
            }
        }

        /// <summary>
        /// Reports whether the prototype's materials can actually be instanced.
        /// </summary>
        /// <remarks>
        /// A detail prototype with <c>useInstancing</c> set is only legal when every material on the
        /// prefab has GPU instancing enabled. When it is not, Unity rejects the prototype, and the
        /// only symptom is missing grass.
        /// </remarks>
        private static void AppendMaterialInstancing(StringBuilder report, GameObject prefab, bool useInstancing)
        {
            if (prefab == null || !useInstancing)
            {
                return;
            }

            var renderers = prefab.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        report.AppendLine("         material=<null>");
                    }
                    else if (!material.enableInstancing)
                    {
                        report.AppendLine(
                            $"         material '{material.name}' has GPU instancing OFF while the " +
                            "prototype asks for instancing — Unity will reject this prototype.");
                    }
                }
            }
        }

        private static void AppendTrees(StringBuilder report, TerrainData data)
        {
            var prototypes = data.treePrototypes;
            var instances = data.treeInstances;
            report.AppendLine($"  treePrototypes: {prototypes.Length} instances: {instances.Length}");
            if (prototypes.Length == 0)
            {
                return;
            }

            for (var i = 0; i < prototypes.Length; i++)
            {
                var prefab = prototypes[i].prefab;
                var meshSize = MeshSizeMeters(prefab);
                var count = 0;
                var minScale = float.MaxValue;
                var maxScale = 0f;
                foreach (var instance in instances)
                {
                    if (instance.prototypeIndex != i)
                    {
                        continue;
                    }

                    count++;
                    minScale = Mathf.Min(minScale, instance.heightScale);
                    maxScale = Mathf.Max(maxScale, instance.heightScale);
                }

                if (count == 0)
                {
                    report.AppendLine($"    [{i}] '{(prefab != null ? prefab.name : "<null>")}' — no instances");
                    continue;
                }

                report.AppendLine(
                    $"    [{i}] '{(prefab != null ? prefab.name : "<null>")}' n={count} " +
                    $"scale=[{minScale:F2}..{maxScale:F2}]x mesh={meshSize:F1}m -> " +
                    $"world=[{minScale * meshSize:F1}..{maxScale * meshSize:F1}]m");
            }
        }

        /// <summary>Largest dimension of the prefab in metres, the way an LOD Group measures it.</summary>
        private static float MeshSizeMeters(GameObject prefab)
        {
            if (prefab == null)
            {
                return 0f;
            }

            var renderers = prefab.GetComponentsInChildren<Renderer>(includeInactive: true);
            if (renderers.Length == 0)
            {
                return 0f;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            var size = bounds.size;
            return Mathf.Max(size.x, Mathf.Max(size.y, size.z));
        }

        /// <summary>Scratch folder outside Assets for the exported density maps.</summary>
        private const string DetailDumpFolder = "VistaDetailDump";

        /// <summary>
        /// Writes each detail layer's density map as a PNG, white = maximum density.
        /// </summary>
        /// <remarks>
        /// The instance total answers "is there grass at all"; it cannot answer "why is there no
        /// grass *here*", and that is the question that actually gets asked. The map can: lay it
        /// against where the forest stands and the answer is immediate — holes that follow the tree
        /// stands mean something upstream is removing grass under canopy, holes that follow neither
        /// the stands nor the terrain mean the density map was written incompletely (the writer bug
        /// in playbook 2.8), and a full map means the grass is placed and the problem is rendering.
        /// </remarks>
        private static void ExportDetailDensityMaps(
            UnityEngine.Terrain terrain, TerrainData data, int res)
        {
            if (res < 2 || data.detailPrototypes.Length == 0)
            {
                return;
            }

            var projectRoot = System.IO.Directory.GetParent(Application.dataPath).FullName;
            var folder = System.IO.Path.Combine(projectRoot, DetailDumpFolder);
            System.IO.Directory.CreateDirectory(folder);

            for (var i = 0; i < data.detailPrototypes.Length; i++)
            {
                var layer = data.GetDetailLayer(0, 0, res, res, i);
                var peak = 1;
                for (var y = 0; y < res; y++)
                {
                    for (var x = 0; x < res; x++)
                    {
                        if (layer[y, x] > peak)
                        {
                            peak = layer[y, x];
                        }
                    }
                }

                var texture = new Texture2D(res, res, TextureFormat.RGB24, mipChain: false);
                var pixels = new Color32[res * res];
                for (var y = 0; y < res; y++)
                {
                    for (var x = 0; x < res; x++)
                    {
                        var v = (byte)Mathf.Clamp(Mathf.RoundToInt(255f * layer[y, x] / peak), 0, 255);
                        pixels[y * res + x] = new Color32(v, v, v, 255);
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply(updateMipmaps: false);
                var file = System.IO.Path.Combine(
                    folder, $"{terrain.name}_detail{i}_peak{peak}.png");
                System.IO.File.WriteAllBytes(file, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
            }
        }
    }
}
#endif
