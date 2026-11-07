using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameDev.Slice.Kit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// VI-1's sample rooms (VICTORIAN_INTERIORS_PLAN §8): a drawing room in the paper finish, a bedroom in the dark
    /// finish, a bathroom in the panelling, and a gallery of every other piece, built from the ViRoom and ViFurniture
    /// blocks in a new unsaved scene. It checks them with the kit layout check and renders them to Logs/SliceSurvey.
    /// It saves no scene. Batch: -executeMethod GameDev.Slice.EditorTools.VictorianInteriorsSamples.Run
    /// </summary>
    public static class VictorianInteriorsSamples
    {
        private const string Output = "Logs/SliceSurvey";
        private const int M = SliceKitCatalogue.ViModule;
        private static readonly Vector2Int N = new(0, 1), S = new(0, -1), E = new(1, 0), W = new(-1, 0);

        private static Dictionary<string, SliceKitCatalogue.Entry> _kit;
        private static Transform _parent;

        /// <summary>The rooms built, by name: their south-west corner and size, m.</summary>
        public static readonly List<(string Name, int X, int Z, int W, int D)> Rooms = new();

        public static void Run()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var kit = SliceKitCatalogue.BuildVictorian();
            var faults = Build(kit, out var blocks);
            Debug.Log($"[VictorianInteriorsSamples] {blocks} blocks, {faults.Count} kit faults" +
                      (faults.Count > 0 ? ":\n  " + string.Join("\n  ", faults.Take(60).Select(f => f.ToString())) : "."));
            WallFaces(kit);
            Render();
        }

        /// <summary>Builds the samples in a new scene and checks them; returns the faults and counts the blocks.</summary>
        public static List<KitFault> Build(Dictionary<string, SliceKitCatalogue.Entry> kit, out int blocks)
        {
            _kit = kit;
            Rooms.Clear();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            _parent = new GameObject("Victorian samples").transform;

            // A drawing room, 9 × 9 m, paper: the door on the south, two windows and the fireplace on the north.
            Room("Drawing room", 0, 0, 9, 9, "Paper", (S, 1, true), (N, 0, false), (N, 2, false), (E, 1, false));
            Against("ViFireplace", S, 9, 3);
            Against("ViBookcase", E, 0, 3);
            Against("ViClock", E, 0, 7);
            Put("ViSettee", 3, 4); // facing the hearth; a full-module piece in a corner meets the side wall's dado
            Put("ViTableRound", 4, 6);
            Put("ViArmchair", 6, 6, 3);
            Put("ViArmchair", 2, 6, 1);
            Put("ViCarpet", 2, 2);
            Put("ViPalm", 7, 1);
            Put("ViChandelier", 4, 4, 0, 3.3f);
            Hang("ViPicture5", W, 9, 0, 1.6f);
            Hang("ViPicture2", W, 9, 6, 1.6f);
            foreach (var a in new[] { 0, 6 })
            {
                Hang("ViCurtain", S, 9, a, 0.2f);
            }

            Hang("ViCurtain", W, 9, 3, 0.2f);

            // A bedroom, 9 × 9 m, dark: three windows on the north.
            Room("Bedroom", 12, 0, 9, 9, "Dark", (S, 1, true), (N, 0, false), (N, 1, false), (N, 2, false));
            Against("ViBed", W, 21, 3);
            Against("ViDesk", E, 12, 2);
            Against("ViBookcaseLow", N, 0, 12);
            Against("ViCupboardGlass", N, 0, 18);
            Put("ViChair", 14, 3, 3);
            Put("ViRug", 16, 6);
            Put("ViOilLamp", 13, 7);
            Hang("ViWallLamp", E, 12, 6, 2.0f);
            Hang("ViPicture1", W, 21, 0, 1.6f);
            foreach (var a in new[] { 12, 15, 18 })
            {
                Hang("ViCurtain", S, 9, a, 0.2f);
            }

            // A bathroom, 6 × 6 m, panelled, with the dark finish's doorway and window.
            Room("Bathroom", 24, 0, 6, 6, "Panel", (S, 1, true), (N, 1, false));
            Against("ViBath", S, 6, 24);
            Against("ViToilet", E, 24, 3);
            Against("ViBasin", W, 30, 3);
            Put("ViBucket", 25, 1);
            Hang("ViTowelBar", E, 24, 0, 1.0f);
            Hang("ViMirrorSquare", W, 30, 3, 1.6f);
            Hang("ViCurtain", S, 6, 27, 0.2f);

            Gallery();

            var kitBlocks = Object.FindObjectsByType<KitBlock>(FindObjectsInactive.Exclude);
            blocks = kitBlocks.Length;
            return KitLayoutCheck.Check(kitBlocks);
        }

        /// <summary>
        /// A room on the 3 m module: floor, ceiling, and walls of one finish. Each opening names the wall's side, its
        /// segment from the west or south, and whether it is a doorway (with a shut door) or a window.
        /// </summary>
        private static void Room(string name, int x, int z, int w, int d, string finish, params (Vector2Int Side, int Segment, bool Door)[] openings)
        {
            Rooms.Add((name, x, z, w, d));
            var (plain, doorway, window) = SliceKitCatalogue.ViFinishes[finish];
            doorway ??= SliceKitCatalogue.ViFinishes["Dark"].Doorway;
            window ??= SliceKitCatalogue.ViFinishes["Dark"].Window;
            for (var cx = x; cx < x + w; cx += M)
            {
                for (var cz = z; cz < z + d; cz += M)
                {
                    At("ViFloor", new Vector3(cx, 0f, cz), Quaternion.identity);
                    At("ViCeiling", new Vector3(cx, SliceKitCatalogue.ViStorey, cz), Quaternion.identity);
                }
            }

            void Side(Vector2Int facing, int count, System.Func<int, (int X, int Z)> node, bool alongZ)
            {
                for (var i = 0; i < count; i++)
                {
                    var o = openings.FirstOrDefault(p => p.Side == -facing && p.Segment == i);
                    var key = o.Side == Vector2Int.zero ? plain : o.Door ? doorway : window;
                    var (nx, nz) = node(i);
                    var wall = Segment(key, nx, nz, facing);
                    if (o.Side != Vector2Int.zero && o.Door)
                    {
                        // The shut leaf stands in its doorway's opening as a nested block (kit standard §6).
                        Segment("ViDoor", nx, nz, facing).transform.SetParent(wall.transform, true);
                    }
                }
            }

            // A side is named from inside: the south wall faces north into the room.
            Side(N, w / M, i => (x + i * M, z), false);
            Side(S, w / M, i => (x + i * M, z + d), false);
            Side(E, d / M, i => (x, z + i * M), true);
            Side(W, d / M, i => (x + w, z + i * M), true);
        }

        /// <summary>Every piece the rooms do not use, in a row on a floor, and the hung pieces on a wall behind.</summary>
        private static void Gallery()
        {
            const int z0 = -12;
            var used = new HashSet<string>(Object.FindObjectsByType<KitBlock>(FindObjectsInactive.Exclude)
                .Select(b => PrefabUtility.GetCorrespondingObjectFromSource(b.gameObject)).Where(p => p != null).Select(p => p.name));
            var standing = _kit.Where(kv => kv.Value.Path.Contains("/ViFurniture/") && !used.Contains(Path.GetFileNameWithoutExtension(kv.Value.Path)))
                .Where(kv => kv.Value.Depth > 0 || kv.Key.StartsWith("ViCarpet") || kv.Key.StartsWith("ViRug")).OrderBy(kv => kv.Key).ToList();
            var x = 0;
            foreach (var (key, entry) in standing)
            {
                Put(key, x, z0);
                x += Mathf.Max(1, entry.Width) + 1;
            }

            var length = ((x + M - 1) / M + 1) * M;
            for (var cx = 0; cx < length; cx += M)
            {
                for (var cz = z0 - 3; cz < z0 + 9; cz += M)
                {
                    At("ViFloor", new Vector3(cx, 0f, cz), Quaternion.identity);
                }

                Segment("ViPaper", cx, z0 + 9, S);
            }

            var hung = _kit.Keys.Where(k => _kit[k].Path.Contains("/ViFurniture/") && _kit[k].Depth == 0 && !k.StartsWith("ViCarpet") && !k.StartsWith("ViRug")
                && k != "ViChandelier" && !k.StartsWith("ViCandle") && k != "ViCandelabrum" && k != "ViOilLamp").OrderBy(k => k).ToList();
            for (var i = 0; i < hung.Count && i * M < length; i++)
            {
                Hang(hung[i], S, z0 + 9, i * M, hung[i] == "ViCurtain" ? 0.2f : 1.5f);
            }

            Put("ViChandelier", 1, z0 + 5, 0, 3.3f);
            Put("ViCandle", 4, z0 + 5);
            Put("ViCandelabrum", 5, z0 + 5);
            Put("ViOilLamp", 6, z0 + 5);
        }

        // ------------------------------------------------------------------------------------------------ placing

        private static int Turns(Vector2Int facing) => facing == N ? 0 : facing == E ? 1 : facing == S ? 2 : 3;

        /// <summary>A linear block on the segment of one module from node (x, z), its local +Z turned to <paramref name="facing"/>.</summary>
        private static GameObject Segment(string key, int x, int z, Vector2Int facing)
        {
            var pivot = facing == S ? new Vector3(x + M, 0f, z) : facing == E ? new Vector3(x, 0f, z + M) : new Vector3(x, 0f, z);
            return At(key, pivot, Quaternion.Euler(0f, 90f * Turns(facing), 0f));
        }

        private static GameObject Against(string key, Vector2Int facing, int line, int a)
        {
            var e = _kit[key];
            var turns = Turns(facing);
            if (facing == N) return Put(key, a, line, turns);
            if (facing == S) return Put(key, a, line - e.Depth, turns);
            if (facing == E) return Put(key, line, a, turns);
            return Put(key, line - e.Depth, a, turns);
        }

        /// <summary>A hung block centred on the 3 m segment that holds <paramref name="a"/>.</summary>
        private static GameObject Hang(string key, Vector2Int facing, int line, int a, float y)
        {
            var e = _kit[key];
            a = a / M * M + (M - e.Width) / 2;
            Vector3 pivot;
            if (facing == N) pivot = new Vector3(a, y, line);
            else if (facing == S) pivot = new Vector3(a + e.Width, y, line);
            else if (facing == E) pivot = new Vector3(line, y, a + e.Width);
            else pivot = new Vector3(line, y, a);
            return At(key, pivot, Quaternion.Euler(0f, 90f * Turns(facing), 0f));
        }

        private static GameObject Put(string key, int x, int z, int turns = 0, float y = 0f)
        {
            var e = _kit[key];
            var rotation = Quaternion.Euler(0f, 90f * turns, 0f);
            var pivot = new Vector3(x, y, z);
            if (e.Depth > 0)
            {
                var corners = new[] { Vector3.zero, new Vector3(e.Width, 0f, 0f), new Vector3(0f, 0f, e.Depth), new Vector3(e.Width, 0f, e.Depth) }
                    .Select(c => rotation * c).ToArray();
                pivot -= new Vector3(corners.Min(c => c.x), 0f, corners.Min(c => c.z));
            }

            return At(key, pivot, rotation);
        }

        private static GameObject At(string key, Vector3 pivot, Quaternion rotation)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(_kit[key].Prefab, _parent);
            go.transform.SetPositionAndRotation(new Vector3(Mathf.Round(pivot.x), Mathf.Round(pivot.y * 10f) / 10f, Mathf.Round(pivot.z)), rotation);
            return go;
        }

        // ------------------------------------------------------------------------------------------------ report

        /// <summary>Each wall block's seen depth after it is set by its infill: where its faces stand from its line.</summary>
        private static void WallFaces(Dictionary<string, SliceKitCatalogue.Entry> kit)
        {
            var lines = new List<string>();
            foreach (var (key, entry) in kit.Where(kv => kv.Value.Path.Contains("/ViRoom/")).OrderBy(kv => kv.Key))
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(entry.Prefab);
                var renderers = go.GetComponentsInChildren<Renderer>().Where(r => !(r is ParticleSystemRenderer)).ToArray();
                var b = renderers[0].bounds;
                foreach (var r in renderers.Skip(1))
                {
                    b.Encapsulate(r.bounds);
                }

                var bodies = go.GetComponents<BoxCollider>();
                var body = bodies.Length > 0 ? $"; body z {bodies.Min(c => c.bounds.min.z):0.00}..{bodies.Max(c => c.bounds.max.z):0.00}" : "";
                lines.Add($"  {key}: {entry.Width} x {entry.Depth}, seen x {b.min.x:0.00}..{b.max.x:0.00}, y {b.min.y:0.00}..{b.max.y:0.00}, z {b.min.z:0.00}..{b.max.z:0.00}{body}");
                Object.DestroyImmediate(go);
            }

            Debug.Log("[VictorianInteriorsSamples] room blocks as set:\n" + string.Join("\n", lines));
        }

        private static void Render()
        {
            Directory.CreateDirectory(Output);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.43f, 0.4f);
            var sun = new GameObject("Render sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 0.6f;
            sun.transform.rotation = Quaternion.Euler(60f, -30f, 0f);
            foreach (var (name, x, z, w, d) in Rooms)
            {
                var light = new GameObject("Render light").AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 14f;
                light.intensity = 2.5f;
                light.color = new Color(1f, 0.85f, 0.65f);
                light.transform.position = new Vector3(x + w * 0.5f, 2.8f, z + d * 0.5f);
            }

            var ceilings = Object.FindObjectsByType<KitBlock>(FindObjectsInactive.Exclude)
                .Where(b => b.name.StartsWith("Kit_ViRoom_Ceiling")).Select(b => b.gameObject).ToArray();
            foreach (var c in ceilings)
            {
                c.SetActive(false);
            }

            sun.shadows = LightShadows.Soft;
            Shot("vi_sample_top", new Vector3(15f, 30f, 4.5f), new Vector3(15f, 0f, 4.5f), 1800, 700, ortho: 9f);
            Shot("vi_gallery_top", new Vector3(30f, 30f, -8f), new Vector3(30f, 0f, -8f), 1800, 700, ortho: 12f);
            foreach (var c in ceilings)
            {
                c.SetActive(true);
            }

            sun.shadows = LightShadows.None;
            foreach (var (name, x, z, w, d) in Rooms)
            {
                var label = name.Replace(' ', '_').ToLowerInvariant();
                Shot($"vi_sample_{label}_sw", new Vector3(x + 0.6f, 1.7f, z + 0.6f), new Vector3(x + w - 0.5f, 1.2f, z + d - 0.5f), 1600, 900);
                Shot($"vi_sample_{label}_ne", new Vector3(x + w - 0.6f, 1.7f, z + d - 0.6f), new Vector3(x + 0.5f, 1.2f, z + 0.5f), 1600, 900);
            }

            Shot("vi_gallery_front_a", new Vector3(14f, 2.2f, -18f), new Vector3(14f, 1.2f, -8f), 1800, 900);
            Shot("vi_gallery_front_b", new Vector3(42f, 2.2f, -18f), new Vector3(42f, 1.2f, -8f), 1800, 900);
            Debug.Log($"[VictorianInteriorsSamples] renders written to {Output}/vi_*.png");
        }

        private static void Shot(string name, Vector3 from, Vector3 to, int width, int height, float ortho = 0f)
        {
            var camera = new GameObject("RenderCamera").AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 200f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.62f, 0.74f, 0.86f);
            camera.fieldOfView = 70f;
            if (ortho > 0f)
            {
                camera.orthographic = true;
                camera.orthographicSize = ortho;
            }

            camera.transform.SetPositionAndRotation(from, Quaternion.LookRotation(to - from, ortho > 0f ? Vector3.forward : Vector3.up));
            var target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes($"{Output}/{name}.png", image.EncodeToPNG());
            RenderTexture.active = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(camera.gameObject);
        }
    }
}
