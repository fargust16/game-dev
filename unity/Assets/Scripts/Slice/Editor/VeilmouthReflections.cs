using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameDev.Slice.Atmosphere;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// What Veilmouth's glass, water and glossy stone reflect (reference plan R2 step 5): the painted sky as the scene's
    /// environment reflection, and baked reflection probes over the streets, so a window shows the street and the houses
    /// across it, not a sky that does not match. Both are baked at noon; the sky's clock dims them toward night
    /// (<see cref="VeilmouthSky"/>), since they hold the day.
    /// <para>
    /// The sky: the scene's own sky drawn alone into a cubemap, with its haze and muted colour, replacing Unity's default
    /// sky, which the scene had never baked. The streets: one probe in each 30 m cell over the city inside its walls (C4,
    /// city plan §28.10), at the open point nearest the cell's middle, 3 m over the ground, influence 34 × 50 × 34 m with a 4 m
    /// blend; a cell with no open point within 12 m (a block of houses) has none. No box projection: a cell's box would
    /// not match its street's fronts. The probes stand under their own root, outside the city's, and are baked again
    /// after the city is rebuilt. Every size here is an authoring default. Batch:
    /// -executeMethod GameDev.Slice.EditorTools.VeilmouthReflections.Bake
    /// </para>
    /// </summary>
    public static class VeilmouthReflections
    {
        public const string RootName = "Veilmouth Reflections";
        public const string Folder = "Assets/Scenes/Slice/VeilmouthReflections";

        // The city inside the walls, C4 (city plan §28.10): px 420…680, py 260…556 (to the embankment's river walk), so
        // X −80…180, Z −56…240. The works' brick blocks keep the sky's reflection.
        private const float West = -80f, East = 180f, South = -56f, North = 240f;
        private const float Cell = 30f, Search = 12f, Step = 3f, Eye = 3f;
        private static readonly Vector3 Influence = new(34f, 50f, 34f);
        private const float Blend = 4f;
        private const int Resolution = 128;

        /// <summary>A point is open when nothing stands over it more than this above the ground, m (a kerb or a step may).</summary>
        private const float OpenAbove = 1f;

        [MenuItem("GameDev/Slice/Reference Look/Bake Reflections")]
        public static void Bake()
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var sky = Object.FindFirstObjectByType<VeilmouthSky>();
            var terrain = Terrain.activeTerrain;
            if (sky == null || terrain == null)
            {
                Debug.LogError("[VeilmouthReflections] Veilmouth has no VeilmouthSky or no terrain.");
                return;
            }

            Directory.CreateDirectory(Folder);
            sky.Apply(12 * 60);
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == RootName))
            {
                Object.DestroyImmediate(old);
            }

            Physics.SyncTransforms();
            var root = new GameObject(RootName).transform;

            // The sky alone, from high over the city.
            var skyProbe = Probe(root, "Sky", new Vector3(40f, VeilmouthScene.StreetLevel + 60f, 60f)); // over the Penny
            skyProbe.cullingMask = 0;
            var skyMap = BakeOne(skyProbe, $"{Folder}/Sky.exr");
            Object.DestroyImmediate(skyProbe.gameObject);
            if (skyMap != null)
            {
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
                RenderSettings.customReflectionTexture = skyMap;
            }

            // The streets.
            var probes = new List<ReflectionProbe>();
            var cells = 0;
            for (var x = West + (Cell * 0.5f); x < East; x += Cell)
            {
                for (var z = South + (Cell * 0.5f); z < North; z += Cell)
                {
                    cells++;
                    if (Open(terrain, x, z) is not { } at)
                    {
                        continue;
                    }

                    var probe = Probe(root, $"Street {probes.Count:00}", at + (Vector3.up * Eye));
                    probe.size = Influence;
                    probe.center = new Vector3(0f, (Influence.y * 0.5f) - Eye - 2f, 0f);
                    probe.blendDistance = Blend;
                    probes.Add(probe);
                }
            }

            foreach (var file in Directory.GetFiles(Folder, "Street *.exr"))
            {
                AssetDatabase.DeleteAsset(file.Replace('\\', '/'));
            }

            // A probe inside a building would reflect its walls: none may stand in a collider.
            foreach (var p in probes.Where(p => Physics.CheckSphere(p.transform.position, 0.5f, ~0, QueryTriggerInteraction.Ignore)))
            {
                Debug.LogWarning($"[VeilmouthReflections] {p.name} at {p.transform.position} stands in a collider.");
            }

            var baked = probes.Count(p => BakeOne(p, $"{Folder}/{p.name}.exr") != null);
            sky.SetProbes(probes.ToArray());
            sky.Apply(sky.PreviewMinute);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[VeilmouthReflections] sky {(skyMap != null ? "baked" : "FAILED")}; {probes.Count} street probes in {cells} cells, {baked} baked.");
        }

        private static ReflectionProbe Probe(Transform root, string name, Vector3 at)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.position = at;
            var probe = go.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Custom;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.resolution = Resolution;
            probe.hdr = true;
            probe.boxProjection = false;
            probe.importance = 1;
            probe.clearFlags = ReflectionProbeClearFlags.Skybox;
            probe.nearClipPlane = 0.3f;
            probe.farClipPlane = 1000f;
            return probe;
        }

        private static Texture BakeOne(ReflectionProbe probe, string path)
        {
            // A second try: the file can be held a moment by another process (2026-11-09, "could not write" once in 70).
            if (!Lightmapping.BakeReflectionProbe(probe, path) && !Lightmapping.BakeReflectionProbe(probe, path))
            {
                Debug.LogError($"[VeilmouthReflections] {path} did not bake.");
                return null;
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var map = AssetDatabase.LoadAssetAtPath<Texture>(path);
            probe.customBakedTexture = map;
            return map;
        }

        /// <summary>The open point of the cell round (x, z) nearest its middle, on the ground, or null.</summary>
        private static Vector3? Open(Terrain terrain, float x, float z)
        {
            Vector3? best = null;
            var bestDistance = float.MaxValue;
            for (var dx = -Search; dx <= Search; dx += Step)
            {
                for (var dz = -Search; dz <= Search; dz += Step)
                {
                    var d = (dx * dx) + (dz * dz);
                    if (d > Search * Search || d >= bestDistance)
                    {
                        continue;
                    }

                    var p = new Vector3(x + dx, 0f, z + dz);
                    var ground = terrain.SampleHeight(p) + terrain.transform.position.y;
                    var floor = Mathf.Max(ground, VeilmouthScene.SeaLevel); // over the river, the water
                    var hits = Physics.RaycastAll(new Vector3(p.x, floor + 200f, p.z), Vector3.down, 200f - OpenAbove, ~0, QueryTriggerInteraction.Ignore);
                    if (hits.Length > 0)
                    {
                        continue;
                    }

                    // Nor within 0.75 m of anything at the probe's height: a lamp's head beside it fills its cubemap.
                    if (Physics.CheckSphere(new Vector3(p.x, floor + Eye, p.z), 0.75f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }

                    best = new Vector3(p.x, floor, p.z);
                    bestDistance = d;
                }
            }

            return best;
        }
    }
}
