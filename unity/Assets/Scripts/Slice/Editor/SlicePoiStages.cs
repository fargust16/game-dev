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
    /// S2.5, once: POI A's scene before Harrow &amp; Tincture in Veilmouth (11.1 POI A §1): Harrow held by two Wardens on the
    /// cobbles, a third in his door, and three neighbours facing them, all posed and hidden until the player enters the
    /// area within the window (<see cref="PoiStage"/>). The stage talks through <see cref="PersonTalk"/> with POI A's
    /// conversation. Places are measured from the shop's street door, so they follow the house; a scene that already has
    /// the stage is left as it is, as Veilmouth is the owner's.
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
            Neighbour(cast, SliceTownsfolk.PrefabPath, Feet(door, crowd - (right * NeighboursSide)), harrow, "POI A - Neighbour (left)");
            Neighbour(cast, SliceTownsfolk.WomanPrefabPath, Feet(door, crowd + (outward * 0.6f)), harrow, "POI A - Neighbour (middle)");
            Neighbour(cast, SliceTownsfolk.PrefabPath, Feet(door, crowd + (right * NeighboursSide)), harrow, "POI A - Neighbour (right)");

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

        private static void Neighbour(GameObject cast, string prefabPath, Vector3 at, Vector3 lookAt, string name)
        {
            var neighbour = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), cast.transform);
            neighbour.name = name;
            neighbour.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, Yaw(Flat(lookAt - at)), 0f));

            // A neighbour stands; the stage gives the pose (PoiStage).
            if (neighbour.TryGetComponent<TownsfolkWalker>(out var walker))
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
