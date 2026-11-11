using System.Linq;
using GameDev.Dialogue;
using GameDev.Slice.Kit;
using GameDev.Slice.People;
using GameDev.World.Poi;
using GameDev.World.Portals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// POI C, the Soot Stair Trace (11.1 POI C and *Slice build (S4)*; VERTICAL_SLICE_IMPLEMENTATION_PLAN S4). S4.1: the
    /// Physical Traces on Deaf Boiler Yard before the cellar door, each a <see cref="TraceSpot"/> with a placeholder of our
    /// own until small models come (SLICE_MODELS_WANTED, priority C): Veinmarket's chalk on the wall beside the door, which
    /// is never cleared and allows the close look, and the tells that A's and B's outcomes leave, which a sweeper clears.
    /// Placed again on each run, replacing the earlier group; every size and place is an authoring default, in metres.
    /// </summary>
    public static class SlicePoiC
    {
        public const string PoiCPath = "Assets/Data/Slice/Poi/PoiC.asset";
        public const string TracesName = "POI C - Traces on Deaf Boiler Yard";
        private const string CellarStreetDoor = "soot-stair-cellar.street";
        private const string MaterialFolder = "Assets/Materials/Slice/Poi";

        // VeilmouthCity.Entrance sets the leaf 1.5 m to the left of the door it opens.
        private const float LeafToDoor = 1.5f;

        // 11.1 *Slice build (S4)*: a sweeper clears the easy tells at day 2, 20:00, four hours after Nell's chalk tip.
        public const long SweptAt = 1440 + (20 * 60);
        private const float Reach = 1.2f;

        /// <summary>Each tell: its tip, how far out from the wall and along it from the door's middle, and what leaves it.</summary>
        private static readonly (string tip, float outward, float along, string[] anyOf)[] Tells =
        {
            ("trace.wax", 1.8f, -2.6f, new[] { "aftermath.harrow.shop_sealed" }),
            ("trace.glass", 3.2f, -1.0f, new[] { "aftermath.harrow.drawers_emptied", "aftermath.harrow.defended" }),
            ("trace.oil", 5.0f, 1.2f, new[] { "aftermath.loom_lane.girls_taken" }),
            ("trace.glove", 3.6f, 3.4f, new[] { "aftermath.loom_lane.girls_taken" }),
            ("trace.rag", 2.6f, -3.8f, new[] { "aftermath.loom_lane.girls_freed", "aftermath.loom_lane.snatch_spoiled" }),
        };

        /// <summary>
        /// Lays POI C's traces on Deaf Boiler Yard in the saved Veilmouth scene, replacing earlier ones, and saves it.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.SlicePoiC.PlaceTraces
        /// </summary>
        [MenuItem("GameDev/Slice/POI/Lay POI C's traces on Deaf Boiler Yard in Veilmouth")]
        public static void PlaceTraces()
        {
            var scene = EditorSceneManager.OpenScene(SlicePeople.ScenePath(SliceRoster.Veilmouth), OpenSceneMode.Single);
            var poi = AssetDatabase.LoadAssetAtPath<PoiDefinition>(PoiCPath);
            var portal = Object.FindObjectsByType<Portal>(FindObjectsInactive.Include).FirstOrDefault(p => p.Id == CellarStreetDoor);
            if (poi == null || portal == null)
            {
                Debug.LogError($"[SlicePoiC] POI C or the door '{CellarStreetDoor}' is missing.");
                return;
            }

            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == TracesName))
            {
                Object.DestroyImmediate(old);
            }

            Physics.SyncTransforms();
            if (!Yard(portal, out var door, out var outward))
            {
                Debug.LogError($"[SlicePoiC] No free yard before the cellar door at {portal.transform.position}.");
                return;
            }

            var along = Vector3.Cross(Vector3.up, outward);
            var root = new GameObject(TracesName);
            root.transform.SetPositionAndRotation(door, Quaternion.LookRotation(outward));

            // The chalk on the wall beside the door, 3 m along it, out of the door's reach of 1.5 m; looked at from 0.4 m out.
            var chalkAt = door + (along * 3f);
            var chalk = Spot(root, "Trace - Veinmarket chalk", SlicePoiStages.Feet(door, chalkAt + (outward * 0.4f)), outward);
            ChalkMark(chalk, chalkAt + (Vector3.up * 1.3f) + (outward * 0.02f), outward);
            var chalkSpot = chalk.AddComponent<TraceSpot>();
            chalkSpot.Configure(poi, "trace.chalk", new StateCondition[0], -1, chalk.transform.Find("Look").gameObject, Reach);
            chalkSpot.AllowCloseLook("trace.link", "trace.misread", "poi.c.looked", 3, 20);

            var laid = 1;
            foreach (var (tip, out_, side, anyOf) in Tells)
            {
                var at = door + (outward * out_) + (along * side);
                if (!SlicePoiStages.Free(door, at))
                {
                    at = door + (outward * out_) - (along * side);
                    if (!SlicePoiStages.Free(door, at))
                    {
                        Debug.LogWarning($"[SlicePoiC] {tip}: no free ground near {at}; not laid.");
                        continue;
                    }
                }

                var spot = Spot(root, "Trace - " + tip.Substring("trace.".Length), SlicePoiStages.Feet(door, at), outward);
                Tell(tip, spot.transform);
                spot.AddComponent<TraceSpot>().Configure(poi, tip, anyOf.Select(k => new StateCondition(k)).ToArray(), SweptAt,
                    spot.transform.Find("Look").gameObject, Reach);
                laid++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SlicePoiC] {laid} traces laid before the cellar door at {door}, the yard toward {outward}.");
        }

        public const string SweepName = "POI C - The sweep on Deaf Boiler Yard";
        public const string SweepDialoguePath = "Assets/Data/Slice/Dialogue/SweepDialogue.asset";

        /// <summary>11.1 *Slice build (S4)*: a threat, worked or failed, sends the sweep away for good.</summary>
        public const string SweepGone = "poi.c.sweep_gone";

        // Authoring defaults: his hours on the yard, minutes of the day; his reach, m; how far he keeps from any trace, m,
        // so a trace's reach of 1.2 m and his of 2 m never meet and one Interact does one thing.
        public const int SweepFrom = 8 * 60;
        public const int SweepUntil = 18 * 60;
        private const float SweepReach = 2f;
        public const float SweepClear = 4f;

        /// <summary>Where the sweep may stand, out from the wall and along it from the door's middle, first free first.</summary>
        private static readonly (float outward, float along)[] SweepPlaces = { (3f, 8f), (3f, -8f), (5f, 7.5f), (5f, -7.5f), (7f, 0f) };

        /// <summary>
        /// S4.2: the chimney sweep, a Witness, on Deaf Boiler Yard by day while C is open: a working man of the asset map
        /// (§1) at his rods, a <see cref="PoiStage"/> keeping his hours, and his conversation. Placed again on each run.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.SlicePoiC.PlaceSweep
        /// </summary>
        [MenuItem("GameDev/Slice/POI/Place POI C's sweep on Deaf Boiler Yard in Veilmouth")]
        public static void PlaceSweep()
        {
            var scene = EditorSceneManager.OpenScene(SlicePeople.ScenePath(SliceRoster.Veilmouth), OpenSceneMode.Single);
            var poi = AssetDatabase.LoadAssetAtPath<PoiDefinition>(PoiCPath);
            var dialogue = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(SweepDialoguePath);
            var body = AssetDatabase.LoadAssetAtPath<GameObject>(SlicePeopleModels.Townsman.Standing);
            var portal = Object.FindObjectsByType<Portal>(FindObjectsInactive.Include).FirstOrDefault(p => p.Id == CellarStreetDoor);
            if (poi == null || dialogue == null || body == null || portal == null)
            {
                Debug.LogError("[SlicePoiC] POI C, the sweep's conversation, the townsman or the cellar door is missing.");
                return;
            }

            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == SweepName))
            {
                Object.DestroyImmediate(old);
            }

            Physics.SyncTransforms();
            if (!Yard(portal, out var door, out var outward))
            {
                Debug.LogError($"[SlicePoiC] No free yard before the cellar door at {portal.transform.position}.");
                return;
            }

            var along = Vector3.Cross(Vector3.up, outward);
            var traces = Object.FindObjectsByType<TraceSpot>(FindObjectsInactive.Include).Select(t => t.transform.position).ToList();
            Vector3? found = null;
            foreach (var (out_, side) in SweepPlaces)
            {
                var at = door + (outward * out_) + (along * side);
                if (!SlicePoiStages.Free(door, at))
                {
                    continue;
                }

                var feet = SlicePoiStages.Feet(door, at);
                if (traces.All(t => FlatDistance(t, feet) >= SweepClear))
                {
                    found = feet;
                    break;
                }
            }

            if (found == null)
            {
                Debug.LogError("[SlicePoiC] No free place on the yard for the sweep, clear of the traces.");
                return;
            }

            var stand = found.Value;
            var facing = SlicePoiStages.Flat(door - stand);
            var stage = new GameObject(SweepName);
            stage.transform.SetPositionAndRotation(stand, Quaternion.LookRotation(facing));
            var cast = new GameObject("Cast");
            cast.transform.SetParent(stage.transform, false);

            var sweep = (GameObject)PrefabUtility.InstantiatePrefab(body, cast.transform);
            sweep.name = "POI C - The sweep";
            sweep.transform.SetPositionAndRotation(stand, Quaternion.LookRotation(facing));
            if (sweep.TryGetComponent<TownsfolkWalker>(out var walker))
            {
                Object.DestroyImmediate(walker);
            }

            Rods(cast.transform, stand, facing);
            stage.AddComponent<PersonTalk>().Configure(dialogue, SweepReach);
            var keeps = stage.AddComponent<PoiStage>();
            keeps.Configure(poi, cast, 60f);
            keeps.Keep(SweepFrom, SweepUntil, SweepGone);
            cast.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SlicePoiC] The sweep stands at {stand}, {Vector3.Distance(stand, door):0.0} m from the cellar door.");
        }

        public static float FlatDistance(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;

        // His rods and brush, laid at his side: placeholders of our own until a model comes (SLICE_MODELS_WANTED).
        private static void Rods(Transform cast, Vector3 feet, Vector3 facing)
        {
            var side = Vector3.Cross(Vector3.up, facing);
            var turn = Quaternion.LookRotation(facing);
            var rod = Material("Sweep_Rods", new Color(0.09f, 0.075f, 0.06f), 0.15f);
            for (var k = 0; k < 5; k++)
            {
                var at = feet + (side * (0.55f + (k * 0.025f))) + (Vector3.up * (0.012f + ((k % 2) * 0.02f)));
                Piece(cast, PrimitiveType.Cylinder, at, turn * Quaternion.Euler(90f, k * 2f, 0f), new Vector3(0.022f, 0.75f, 0.022f), rod);
            }

            var brush = feet + (side * 0.6f) + (facing * 0.8f) + (Vector3.up * 0.09f);
            Piece(cast, PrimitiveType.Sphere, brush, turn, new Vector3(0.36f, 0.18f, 0.36f), Material("Sweep_Brush", new Color(0.03f, 0.03f, 0.03f), 0.05f));
        }

        /// <summary>The door's middle and the way out into the yard, the side of the wall where a person can stand.</summary>
        internal static bool Yard(Portal portal, out Vector3 door, out Vector3 outward)
        {
            door = portal.transform.position + (portal.transform.right * LeafToDoor);
            outward = SlicePoiStages.Flat(portal.transform.forward);
            if (SlicePoiStages.Free(door, door + (outward * 2f)))
            {
                return true;
            }

            outward = -outward;
            return SlicePoiStages.Free(door, door + (outward * 2f));
        }

        private static GameObject Spot(GameObject root, string name, Vector3 at, Vector3 outward)
        {
            var spot = new GameObject(name);
            spot.transform.SetParent(root.transform, false);
            spot.transform.SetPositionAndRotation(at, Quaternion.LookRotation(outward));
            var look = new GameObject("Look");
            look.transform.SetParent(spot.transform, false);
            return spot;
        }

        // A crossed loop of chalk on the brick, as Veinmarket marks a way (11.1 POI B *Veinmarket chalk*).
        private static void ChalkMark(GameObject spot, Vector3 centre, Vector3 outward)
        {
            var look = spot.transform.Find("Look");
            var facing = Quaternion.LookRotation(-outward);
            var chalk = Material("Trace_Chalk", new Color(0.86f, 0.84f, 0.78f), 0.05f);
            for (var k = 0; k < 10; k++)
            {
                var angle = k * Mathf.PI * 2f / 10f;
                var at = centre + (facing * new Vector3(Mathf.Cos(angle) * 0.13f, Mathf.Sin(angle) * 0.13f, 0f));
                Piece(look, PrimitiveType.Cube, at, facing * Quaternion.Euler(0f, 0f, (angle * Mathf.Rad2Deg) + 90f), new Vector3(0.09f, 0.025f, 0.006f), chalk);
            }

            Piece(look, PrimitiveType.Cube, centre, facing * Quaternion.Euler(0f, 0f, 45f), new Vector3(0.44f, 0.025f, 0.006f), chalk);
            Piece(look, PrimitiveType.Cube, centre, facing * Quaternion.Euler(0f, 0f, -45f), new Vector3(0.44f, 0.025f, 0.006f), chalk);
        }

        private static void Tell(string tip, Transform spot)
        {
            var look = spot.Find("Look");
            var at = spot.position;
            var turn = spot.rotation;
            switch (tip)
            {
                case "trace.wax":
                    Piece(look, PrimitiveType.Cylinder, at + (Vector3.up * 0.004f), turn, new Vector3(0.07f, 0.004f, 0.06f), Material("Trace_Wax", new Color(0.62f, 0.45f, 0.16f), 0.55f));
                    break;
                case "trace.glass":
                    var glass = Material("Trace_Glass", new Color(0.03f, 0.035f, 0.03f), 0.92f);
                    Piece(look, PrimitiveType.Cylinder, at + (Vector3.up * 0.015f), turn * Quaternion.Euler(0f, 30f, 90f), new Vector3(0.03f, 0.05f, 0.03f), glass);
                    for (var k = 0; k < 5; k++)
                    {
                        var shard = at + (turn * new Vector3((k - 2) * 0.07f, 0.004f, ((k * 37) % 5 - 2) * 0.05f));
                        Piece(look, PrimitiveType.Cube, shard, turn * Quaternion.Euler(0f, k * 47f, 0f), new Vector3(0.05f, 0.006f, 0.03f), glass);
                    }

                    break;
                case "trace.oil":
                    var oil = Material("Trace_Oil", new Color(0.025f, 0.022f, 0.02f), 0.88f);
                    foreach (var side in new[] { -0.7f, 0.7f })
                    {
                        Piece(look, PrimitiveType.Cube, at + (turn * new Vector3(side, 0.003f, 0f)), turn, new Vector3(0.12f, 0.004f, 1.8f), oil);
                    }

                    break;
                case "trace.glove":
                    var wool = Material("Trace_Glove", new Color(0.33f, 0.29f, 0.25f), 0.08f);
                    Piece(look, PrimitiveType.Capsule, at + (Vector3.up * 0.012f), turn * Quaternion.Euler(90f, 20f, 0f), new Vector3(0.09f, 0.1f, 0.025f), wool);
                    Piece(look, PrimitiveType.Capsule, at + (turn * new Vector3(0.06f, 0.012f, 0.03f)), turn * Quaternion.Euler(90f, 60f, 0f), new Vector3(0.03f, 0.04f, 0.02f), wool);
                    break;
                case "trace.rag":
                    Piece(look, PrimitiveType.Sphere, at + (Vector3.up * 0.03f), turn * Quaternion.Euler(0f, 25f, 0f), new Vector3(0.2f, 0.06f, 0.15f), Material("Trace_Rag", new Color(0.52f, 0.48f, 0.4f), 0.1f));
                    break;
            }
        }

        // Placeholders of our own: no collider, on NoGrid, so neither the Combat Grid nor walking reads them.
        private static void Piece(Transform parent, PrimitiveType shape, Vector3 at, Quaternion turn, Vector3 size, Material material)
        {
            var piece = GameObject.CreatePrimitive(shape);
            Object.DestroyImmediate(piece.GetComponent<Collider>());
            piece.name = shape.ToString();
            piece.layer = LayerMask.NameToLayer(KitLayers.NoGrid);
            piece.transform.SetParent(parent, true);
            piece.transform.SetPositionAndRotation(at, turn);
            piece.transform.localScale = size;
            var renderer = piece.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        internal static Material Material(string name, Color colour, float smoothness)
        {
            var path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            if (!AssetDatabase.IsValidFolder(MaterialFolder))
            {
                AssetDatabase.CreateFolder("Assets/Materials/Slice", "Poi");
            }

            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetColor("_BaseColor", colour);
            material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
