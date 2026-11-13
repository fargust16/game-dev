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
            var body = AssetDatabase.LoadAssetAtPath<GameObject>(SlicePeopleModels.Gentleman.Standing);
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

        public const string NookName = "POI C - The nook";
        public const string StrangerName = "POI C - The stranger";
        public const string PoiCDialoguePath = "Assets/Data/Slice/Dialogue/PoiCDialogue.asset";
        private const string StealthRulesPath = "Assets/Data/Confront/StealthRules.asset";
        private const string CellarInside = "soot-stair-cellar.inside";

        // Authoring defaults, m. The cellar's plan (VeilmouthInteriors): the west vault 0–9 × 0–9, the circle vault 9–21 ×
        // 0–12, the open doorway between them on x = 9 about z = 4.5. The stranger stands in the circle vault looking
        // west through it; the nook is the wall place of the west vault he sees from farthest, while the stair's foot,
        // where the character comes in, is out of his sight, so the nook is reached unseen by keeping out of his cone.
        private static readonly Vector3[] StrangerPlaces =
        {
            new Vector3(12.5f, 0f, 4.5f), new Vector3(12f, 0f, 4.5f), new Vector3(11.5f, 0f, 4.5f), new Vector3(11f, 0f, 4.5f),
            new Vector3(10.5f, 0f, 4.5f), new Vector3(10f, 0f, 4.5f),
        };
        private static readonly Vector3 Doorway = new Vector3(9f, 0f, 4.5f);
        private static readonly Vector3 WestVault = new Vector3(4.5f, 0f, 4.5f);
        private const float NookOut = 0.6f;
        private const float NookReach = 1.2f;
        private const float StrangerReach = 2.2f;
        private const float StrangerArea = 40f;
        private const float SeenWithin = 9.5f;

        /// <summary>Authoring default, m: how far round the nook the places the character opens it from are checked for his sight.</summary>
        private const float ReachAround = 0.8f;

        /// <summary>
        /// S4.3: the cellar under the Soot Stair. The stranger in a coat too clean for the Reek, a gentleman of the asset map
        /// (§1) watching by his own Vision Cone with Perception 3, and the shuttered nook with the folded note and the black
        /// drop, placeholders of our own. Both talk through POI C's conversation; the drop and the stains follow the
        /// choice (S4.5). Placed again on each run.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.SlicePoiC.StageCellar
        /// </summary>
        [MenuItem("GameDev/Slice/POI/Stage POI C's nook and stranger in the cellar under the Soot Stair")]
        public static void StageCellar()
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthDressing.CellarScenePath, OpenSceneMode.Single);
            var poi = AssetDatabase.LoadAssetAtPath<PoiDefinition>(PoiCPath);
            var dialogue = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(PoiCDialoguePath);
            var rules = AssetDatabase.LoadAssetAtPath<GameDev.Confront.Stealth.StealthRules>(StealthRulesPath);
            var body = AssetDatabase.LoadAssetAtPath<GameObject>(SlicePeopleModels.Gentleman.Standing);
            var stair = Object.FindObjectsByType<Portal>(FindObjectsInactive.Include).FirstOrDefault(p => p.Id == CellarInside);
            if (poi == null || dialogue == null || rules == null || body == null || stair == null)
            {
                Debug.LogError("[SlicePoiC] POI C, its conversation, the stealth rules, the gentleman or the cellar's stair door is missing.");
                return;
            }

            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == NookName || g.name == StrangerName))
            {
                Object.DestroyImmediate(old);
            }

            Physics.SyncTransforms();
            var floor = stair.transform.position;
            var entry = new[] { 1f, 2f, 3f }.Select(d => stair.transform.position + (SlicePoiStages.Flat(stair.transform.forward) * d)).ToArray();
            foreach (var place in StrangerPlaces)
            {
                var stand = SlicePoiStages.Feet(floor, place);
                if (!SlicePoiStages.Free(floor, stand))
                {
                    continue;
                }

                var facing = SlicePoiStages.Flat(Doorway - stand);
                if (entry.Any(e => SeenFrom(rules, stand, facing, e)))
                {
                    continue;
                }

                if (!Nook(rules, floor, stand, facing, stair, out var nookAt, out var wall))
                {
                    continue;
                }

                Build(poi, dialogue, body, stand, facing, nookAt, wall);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[SlicePoiC] The stranger stands at {stand} facing {facing}; the nook at {nookAt}, its wall toward {wall}, {FlatDistance(stand, nookAt):0.0} m from him.");
                return;
            }

            Debug.LogError("[SlicePoiC] No place for the stranger that leaves the stair's foot unseen and sees a free wall of the west vault.");
        }

        /// <summary>Whether an observer standing at <paramref name="feet"/>, facing <paramref name="facing"/>, sees a standing character at <paramref name="at"/>.</summary>
        private static bool SeenFrom(GameDev.Confront.Stealth.StealthRules rules, Vector3 feet, Vector3 facing, Vector3 at)
        {
            if (FlatDistance(feet, at) > SeenWithin || !rules.InCone(feet, facing, at, 1f))
            {
                return false;
            }

            var notCharacters = ~LayerMask.GetMask(KitLayers.Character);
            var eye = feet + (Vector3.up * rules.EyeHeight);
            var head = at + (Vector3.up * 1.6f);
            return !Physics.Linecast(eye, head, notCharacters, QueryTriggerInteraction.Ignore);
        }

        /// <summary>The free place before a wall of the west vault that the stranger sees from farthest, at least 2 m from the stair door.</summary>
        private static bool Nook(GameDev.Confront.Stealth.StealthRules rules, Vector3 floor, Vector3 stand, Vector3 facing, Portal stair,
            out Vector3 at, out Vector3 wall)
        {
            at = Vector3.zero;
            wall = Vector3.zero;
            var best = -1f;
            // The walls, not the furniture standing before them.
            var walls = ~LayerMask.GetMask(KitLayers.Character, KitLayers.Prop, KitLayers.NoGrid, "Ignore Raycast");
            var middle = WestVault + (Vector3.up * 1.3f);
            foreach (var toward in new[] { Vector3.forward, Vector3.left, Vector3.back })
            {
                if (!Physics.Raycast(middle, toward, out var face, 8f, walls, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                var along = Vector3.Cross(Vector3.up, toward);
                for (var step = -3.5f; step <= 3.5f; step += 0.5f)
                {
                    var onWall = face.point + (along * step);
                    if (!Physics.Raycast(onWall - (toward * 0.5f), toward, out var here, 1f, walls, QueryTriggerInteraction.Ignore))
                    {
                        continue; // a doorway or a corner: no wall here
                    }

                    var place = SlicePoiStages.Feet(floor, here.point - (toward * NookOut) - (Vector3.up * 1.3f));
                    var distance = FlatDistance(stand, place);
                    // He must see the nook and every place the character opens it from, not only its edge of the doorway.
                    var reach = new[] { place, place - (toward * ReachAround), place + (along * ReachAround), place - (along * ReachAround) };
                    if (distance <= best || FlatDistance(place, stair.transform.position) < 2f || !SlicePoiStages.Free(floor, place)
                        || !reach.All(r => SeenFrom(rules, stand, facing, r)))
                    {
                        continue;
                    }

                    best = distance;
                    at = place;
                    wall = toward;
                }
            }

            return best > 0f;
        }

        private static void Build(PoiDefinition poi, DialogueDefinition dialogue, GameObject body, Vector3 stand, Vector3 facing, Vector3 nookAt, Vector3 wall)
        {
            // The stranger: his scene, his eyes, his conversation, and the stain left if the drop is struck from his hand.
            var strangerRoot = new GameObject(StrangerName);
            strangerRoot.transform.SetPositionAndRotation(stand, Quaternion.LookRotation(facing));
            var cast = new GameObject("Cast");
            cast.transform.SetParent(strangerRoot.transform, false);
            var gentleman = (GameObject)PrefabUtility.InstantiatePrefab(body, cast.transform);
            gentleman.name = "POI C - The stranger in the clean coat";
            gentleman.transform.SetPositionAndRotation(stand, Quaternion.LookRotation(facing));
            if (gentleman.TryGetComponent<TownsfolkWalker>(out var walker))
            {
                Object.DestroyImmediate(walker);
            }

            var eyes = gentleman.AddComponent<GameDev.Slice.Stealth.StealthObserver>();
            eyes.Perception = 3;
            strangerRoot.AddComponent<PersonTalk>().Configure(dialogue, StrangerReach, NookSpot.StrangerNode, true);
            var stage = strangerRoot.AddComponent<PoiStage>();
            stage.Configure(poi, cast, StrangerArea);
            stage.Keep(-1, -1, NookSpot.StrangerGone);
            cast.SetActive(false);
            var strangerStain = Stain(strangerRoot.transform, stand + (facing * 0.7f), "Stain - where the drop was struck");
            strangerRoot.AddComponent<WorldResidue>().Configure(new[] { new StateCondition("poi.c.smashed.stranger") }, 0, new[] { strangerStain }, new GameObject[0], new Portal[0]);

            // The nook: shutters on the wall, a shelf, the note and the drop; the place before it opens the conversation.
            var nook = new GameObject(NookName);
            nook.transform.SetPositionAndRotation(nookAt, Quaternion.LookRotation(wall));
            var look = new GameObject("Look");
            look.transform.SetParent(nook.transform, false);
            var turn = Quaternion.LookRotation(wall);
            var face = nookAt + (wall * NookOut);
            var wood = Material("Nook_Shutters", new Color(0.22f, 0.15f, 0.1f), 0.2f);
            Piece(look.transform, PrimitiveType.Cube, face + (Vector3.up * 1.05f) - (wall * 0.06f), turn, new Vector3(0.8f, 0.04f, 0.22f), Material("Nook_Shelf", new Color(0.3f, 0.29f, 0.27f), 0.1f));
            foreach (var side in new[] { -1f, 1f })
            {
                var leaf = face + (turn * new Vector3(side * 0.55f, 1.3f, -0.18f));
                Piece(look.transform, PrimitiveType.Cube, leaf, turn * Quaternion.Euler(0f, side * 70f, 0f), new Vector3(0.4f, 0.6f, 0.03f), wood);
            }

            var note = new GameObject("Note");
            note.transform.SetParent(look.transform, false);
            Piece(note.transform, PrimitiveType.Cube, face + (turn * new Vector3(-0.18f, 1.08f, -0.08f)), turn * Quaternion.Euler(0f, 12f, 0f), new Vector3(0.11f, 0.012f, 0.08f), Material("Nook_Note", new Color(0.82f, 0.78f, 0.68f), 0.05f));
            var drop = new GameObject("Drop");
            drop.transform.SetParent(look.transform, false);
            var bead = face + (turn * new Vector3(0.14f, 1.1f, -0.08f));
            Piece(drop.transform, PrimitiveType.Sphere, bead, turn, Vector3.one * 0.045f, Material("Nook_Glass", new Color(0.55f, 0.6f, 0.62f), 0.95f));
            Piece(drop.transform, PrimitiveType.Sphere, bead, turn, Vector3.one * 0.022f, Material("Nook_Drop", new Color(0.01f, 0.01f, 0.012f), 0.9f));
            var nookStain = Stain(nook.transform, nookAt + (wall * 0.35f), "Stain - where the drop was smashed");

            var talk = nook.AddComponent<PersonTalk>();
            talk.Configure(dialogue, NookReach, NookSpot.NookNode, false, "ui.prompt.open_shutters");
            nook.AddComponent<NookSpot>().Configure(poi, talk, stage, eyes, NookReach);
            var gone = new[] { "poi.c.drank", "poi.c.smashed", NookSpot.DropTaken, "poi.c.refused" }.Select(k => new StateCondition(k)).ToArray();
            nook.AddComponent<WorldResidue>().Configure(gone, 0, new GameObject[0], new[] { drop }, new Portal[0]);
            nook.AddComponent<WorldResidue>().Configure(new[] { new StateCondition("poi.c.smashed.nook") }, 0, new[] { nookStain }, new GameObject[0], new Portal[0]);
        }

        // The sweet black stain a smashed drop leaves on the flags (11.1 §6), hidden until it is smashed.
        private static GameObject Stain(Transform parent, Vector3 at, string name)
        {
            var stain = new GameObject(name);
            stain.transform.SetParent(parent, true);
            Piece(stain.transform, PrimitiveType.Cylinder, at + (Vector3.up * 0.004f), Quaternion.Euler(0f, 37f, 0f), new Vector3(0.55f, 0.003f, 0.42f), Material("Drop_Stain", new Color(0.02f, 0.018f, 0.02f), 0.75f));
            stain.SetActive(false);
            return stain;
        }

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
        internal static void Piece(Transform parent, PrimitiveType shape, Vector3 at, Quaternion turn, Vector3 size, Material material)
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
