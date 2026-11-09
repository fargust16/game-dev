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
    /// Once each, the POIs' scenes in Veilmouth, posed and hidden until the player enters the area within the window
    /// (<see cref="PoiStage"/>), each talking through <see cref="PersonTalk"/> with its POI's conversation. S2.5, POI A
    /// before Harrow &amp; Tincture (11.1 POI A §1): Harrow held by two Wardens on the cobbles, a third in his door, and three
    /// neighbours facing them, measured from the shop's street door. S2.6, POI B on Loom Lane (11.1 POI B §1): two
    /// Veinmarket collectors dragging a mill girl toward the coal stairs, another girl torn free, and an old woman in a
    /// doorway, measured from the lane's middle found near the POI's place. A scene that already has its stage is left as it
    /// is, as Veilmouth is the owner's.
    /// </summary>
    public static class SlicePoiStages
    {
        public const string PoiAStage = "POI A - Brass-Number Arrest";
        public const string PoiAPath = "Assets/Data/Slice/Poi/PoiA.asset";
        public const string PoiADialoguePath = "Assets/Data/Slice/Dialogue/PoiADialogue.asset";
        private const string HarrowStreetDoor = "harrow-apothecary.street";

        // Authoring defaults, m: VeilmouthCity.Entrance sets the leaf 1.5 m to the left of the door it opens.
        private const float LeafToDoor = 1.5f;
        private const float WardenInTheDoor = 0.7f;
        private const float HarrowOut = 2.4f;
        private const float HoldingSide = 0.75f;
        private const float NeighboursOut = 5.8f;
        private const float NeighboursSide = 2.2f;
        private const float TalkReach = 3f;
        private const float AppearRadius = 60f;

        public const string PoiBStage = "POI B - Eleventh-Bell Collectors";
        public const string PoiBPath = "Assets/Data/Slice/Poi/PoiB.asset";
        public const string PoiBDialoguePath = "Assets/Data/Slice/Dialogue/PoiBDialogue.asset";
        private const string PoiCPath = "Assets/Data/Slice/Poi/PoiC.asset";

        // Authoring defaults, m: where the lane's middle is sought around POI B's place, how far a wall is looked for,
        // and how wide a lane may be.
        private const float LaneSearch = 8f;
        private const float Probe = 30f;
        private const float NarrowestLane = 4f;
        private const float WidestLane = 16f;

        [MenuItem("GameDev/Slice/POI/Stage POI A before Harrow & Tincture in Veilmouth (once)")]
        public static void StagePoiA()
        {
            var scene = EditorSceneManager.OpenScene(SlicePeople.ScenePath(SliceRoster.Veilmouth), OpenSceneMode.Single);
            var poi = AssetDatabase.LoadAssetAtPath<PoiDefinition>(PoiAPath);
            var dialogue = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(PoiADialoguePath);
            if (poi == null || dialogue == null)
            {
                Debug.LogError("[SlicePoiStages] POI A or its conversation is missing.");
                return;
            }

            if (Object.FindObjectsByType<PoiStage>(FindObjectsInactive.Include).Any(s => s.Poi == poi))
            {
                Debug.Log("[SlicePoiStages] POI A is already staged in Veilmouth; left as it is.");
                return;
            }

            var portal = Object.FindObjectsByType<Portal>(FindObjectsInactive.Include).FirstOrDefault(p => p.Id == HarrowStreetDoor);
            if (portal == null)
            {
                Debug.LogError($"[SlicePoiStages] Veilmouth has no door '{HarrowStreetDoor}'.");
                return;
            }

            Physics.SyncTransforms();
            var door = portal.transform.position + (portal.transform.right * LeafToDoor);
            var outward = Flat(portal.transform.forward);
            if (!Free(door, door + (outward * HarrowOut)))
            {
                outward = -outward;
                if (!Free(door, door + (outward * HarrowOut)))
                {
                    Debug.LogError($"[SlicePoiStages] No free street before the door at {door}.");
                    return;
                }
            }

            var right = Vector3.Cross(Vector3.up, outward);
            var harrow = Feet(door, door + (outward * HarrowOut));
            var stage = new GameObject(PoiAStage);
            stage.transform.SetPositionAndRotation(harrow, Quaternion.LookRotation(outward));
            var cast = new GameObject("Cast");
            cast.transform.SetParent(stage.transform, false);

            var facing = Yaw(outward);
            Person(cast, "Blue", Feet(door, door + (outward * WardenInTheDoor)), facing, "POI A - Warden in the door");
            Person(cast, "Green", harrow, facing, "POI A - Harrow");
            Person(cast, "Blue", Feet(door, harrow - (right * HoldingSide)), facing, "POI A - Warden holding him (left)");
            Person(cast, "Blue", Feet(door, harrow + (right * HoldingSide)), facing, "POI A - Warden holding him (right)");

            var crowd = door + (outward * NeighboursOut);
            foreach (var (path, at, name) in new[]
                     {
                         (SliceTownsfolk.PrefabPath, Feet(door, crowd - (right * NeighboursSide)), "POI A - Neighbour (left)"),
                         (SliceTownsfolk.WomanPrefabPath, Feet(door, crowd + (outward * 0.6f)), "POI A - Neighbour (middle)"),
                         (SliceTownsfolk.PrefabPath, Feet(door, crowd + (right * NeighboursSide)), "POI A - Neighbour (right)"),
                     })
            {
                Townsfolk(cast, path, at, harrow - at, name);
            }

            stage.AddComponent<PersonTalk>().Configure(dialogue, TalkReach);
            stage.AddComponent<PoiStage>().Configure(poi, cast, AppearRadius);
            cast.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SlicePoiStages] POI A staged before the door at {door}, the street {outward}, Harrow at {harrow}.");
        }

        private static void Person(GameObject cast, string colour, Vector3 at, float yaw, string name)
        {
            VeilmouthDressing.Person(colour, false, at.x, at.z, yaw, name, at.y);
            var person = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Last(t => t.name == name);
            person.SetParent(cast.transform, true);
        }

        [MenuItem("GameDev/Slice/POI/Stage POI B on Loom Lane in Veilmouth (once)")]
        public static void StagePoiB()
        {
            var scene = EditorSceneManager.OpenScene(SlicePeople.ScenePath(SliceRoster.Veilmouth), OpenSceneMode.Single);
            var poi = AssetDatabase.LoadAssetAtPath<PoiDefinition>(PoiBPath);
            var stairs = AssetDatabase.LoadAssetAtPath<PoiDefinition>(PoiCPath);
            var dialogue = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(PoiBDialoguePath);
            if (poi == null || stairs == null || dialogue == null)
            {
                Debug.LogError("[SlicePoiStages] POI B, POI C or B's conversation is missing.");
                return;
            }

            if (Object.FindObjectsByType<PoiStage>(FindObjectsInactive.Include).Any(s => s.Poi == poi))
            {
                Debug.Log("[SlicePoiStages] POI B is already staged in Veilmouth; left as it is.");
                return;
            }

            Physics.SyncTransforms();
            if (!LaneMiddle(poi.Place, out var middle, out var along))
            {
                Debug.LogError($"[SlicePoiStages] No lane found within {LaneSearch} m of POI B's place {poi.Place}.");
                return;
            }

            // Along the lane toward the coal stairs (POI C), where the collectors drag her.
            if (Vector3.Dot(Flat(stairs.Place - middle), along) < 0f)
            {
                along = -along;
            }

            var side = Vector3.Cross(Vector3.up, along);
            var stage = new GameObject(PoiBStage);
            stage.transform.SetPositionAndRotation(middle, Quaternion.LookRotation(-along));
            var cast = new GameObject("Cast");
            cast.transform.SetParent(stage.transform, false);

            var dragged = Feet(middle, middle + (along * 2f));
            Townsfolk(cast, SliceTownsfolk.WomanPrefabPath, dragged, along, "POI B - Mill girl, dragged");
            Person(cast, "Brown", Feet(middle, dragged + (side * 0.7f)), Yaw(along), "POI B - Collector, holding her");
            Person(cast, "Brown", Feet(middle, dragged + (along * 0.6f) - (side * 0.6f)), Yaw(-along), "POI B - Collector, watching the lane");
            var free = Feet(middle, middle - (along * 1f) + (side * 1.2f));
            Townsfolk(cast, SliceTownsfolk.WomanPrefabPath, free, dragged - free, "POI B - Mill girl, torn free");

            // The old woman in a doorway on the nearer side of the lane, looking into it.
            var wallSide = Clearance(middle, side) <= Clearance(middle, -side) ? side : -side;
            var doorway = Feet(middle, middle - (along * 2.5f) + (wallSide * Mathf.Max(0f, Clearance(middle, wallSide) - 0.6f)));
            Townsfolk(cast, SliceTownsfolk.WomanPrefabPath, doorway, -wallSide, "POI B - Old woman in a doorway");

            stage.AddComponent<PersonTalk>().Configure(dialogue, TalkReach);
            stage.AddComponent<PoiStage>().Configure(poi, cast, AppearRadius);
            cast.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SlicePoiStages] POI B staged in the lane's middle at {middle}, toward the stairs {along}, the old woman at {doorway}.");
        }

        /// <summary>
        /// The middle of the lane near <paramref name="place"/>: of the free points within <see cref="LaneSearch"/> m, the one
        /// most nearly between the walls either side of its longest view, on a lane neither too narrow nor a square.
        /// </summary>
        private static bool LaneMiddle(Vector3 place, out Vector3 middle, out Vector3 along)
        {
            middle = place;
            along = Vector3.forward;
            var best = float.MaxValue;
            for (var dx = -LaneSearch; dx <= LaneSearch; dx += 1f)
            {
                for (var dz = -LaneSearch; dz <= LaneSearch; dz += 1f)
                {
                    var at = place + new Vector3(dx, 0f, dz);
                    if (!Free(place, at))
                    {
                        continue;
                    }

                    var feet = Feet(place, at);
                    var axis = Vector3.forward;
                    var longest = 0f;
                    for (var k = 0; k < 8; k++)
                    {
                        var direction = Quaternion.Euler(0f, k * 22.5f, 0f) * Vector3.forward;
                        var length = Clearance(feet, direction) + Clearance(feet, -direction);
                        if (length > longest)
                        {
                            longest = length;
                            axis = direction;
                        }
                    }

                    var across = Vector3.Cross(Vector3.up, axis);
                    var left = Clearance(feet, across);
                    var right = Clearance(feet, -across);
                    if (left + right < NarrowestLane || left + right > WidestLane)
                    {
                        continue;
                    }

                    var score = Mathf.Abs(left - right) + (0.25f * new Vector2(dx, dz).magnitude);
                    if (score < best)
                    {
                        best = score;
                        middle = feet;
                        along = axis;
                    }
                }
            }

            return best < float.MaxValue;
        }

        /// <summary>How far a wall stands from <paramref name="feet"/> along <paramref name="direction"/>, at chest height, up to <see cref="Probe"/> m.</summary>
        private static float Clearance(Vector3 feet, Vector3 direction)
        {
            var walls = LayerMask.GetMask(KitLayers.Structure);
            return Physics.Raycast(feet + (Vector3.up * 1.4f), direction, out var hit, Probe, walls, QueryTriggerInteraction.Ignore) ? hit.distance : Probe;
        }

        /// <summary>An ordinary townsperson standing, facing <paramref name="facing"/>; the stage gives the pose (PoiStage).</summary>
        private static void Townsfolk(GameObject cast, string prefabPath, Vector3 at, Vector3 facing, string name)
        {
            var person = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), cast.transform);
            person.name = name;
            person.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, Yaw(Flat(facing)), 0f));
            if (person.TryGetComponent<TownsfolkWalker>(out var walker))
            {
                Object.DestroyImmediate(walker);
            }
        }

        /// <summary>Whether a person can stand at <paramref name="at"/>: ground under it near the door's level, and no wall in the way.</summary>
        private static bool Free(Vector3 door, Vector3 at)
        {
            var notCharacters = ~LayerMask.GetMask(KitLayers.Character);
            if (!Physics.Raycast(at + (Vector3.up * 1.6f), Vector3.down, out var floor, 3.2f, notCharacters, QueryTriggerInteraction.Ignore)
                || Mathf.Abs(floor.point.y - door.y) > 1f)
            {
                return false;
            }

            return !Physics.CheckCapsule(floor.point + (Vector3.up * 0.4f), floor.point + (Vector3.up * 1.5f), 0.3f, notCharacters, QueryTriggerInteraction.Ignore);
        }

        /// <summary>The ground under <paramref name="at"/>, or the door's level when none is found.</summary>
        private static Vector3 Feet(Vector3 door, Vector3 at)
        {
            var ground = LayerMask.GetMask(KitLayers.Ground, KitLayers.Structure);
            return Physics.Raycast(at + (Vector3.up * 1.6f), Vector3.down, out var floor, 3.2f, ground, QueryTriggerInteraction.Ignore)
                ? floor.point
                : new Vector3(at.x, door.y, at.z);
        }

        private static Vector3 Flat(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.forward;
        }

        private static float Yaw(Vector3 direction) => Quaternion.LookRotation(direction).eulerAngles.y;
    }
}
