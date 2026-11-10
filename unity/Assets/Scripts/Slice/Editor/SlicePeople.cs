using System.Collections.Generic;
using System.Linq;
using GameDev.Dialogue;
using GameDev.Slice.People;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// S1.5, once (SLICE_PEOPLE §1): the fourteen named people in the owner's scenes. Eleven take the place and pose of
    /// the Dummy that stood for their role; Pip, Lottie, and Kit are new, at the first free place of their list. Each
    /// is a Dummy of the role's colour and the person's sex, named, with <see cref="NamedPerson"/>; Nell and Kit, the
    /// bartender and Street Rumour sources of S1.3, are talked to through their conversations since S2.4
    /// (<see cref="PersonTalk"/>). A person already placed is left as it is.
    /// </summary>
    public static class SlicePeople
    {
        private const string DummyFolder = "Human Character Dummy";

        /// <summary>The Dummy each named person replaces, by its name in the scene.</summary>
        private static readonly Dictionary<string, string> Replaces = new Dictionary<string, string>
        {
            { "nell", "Tavern keeper" },
            { "harrow", "Harrow" },
            { "edda", "Master of Item Creation" },
            { "silas", "Master of Enchanting" },
            { "ada", "Master of First Aid" },
            { "tobias", "The trader" },
            { "ruth", "The pawnbroker" },
            { "ilse", "The seer" },
            { "coyle", "Warden" },
            { "ashgrave", "Faction member" },
            { "dace", "Veinmarket at a table" },
        };

        /// <summary>Where the new people may stand, first free first (authoring defaults, m), with the facing, and the person whose group they join.</summary>
        private static readonly Dictionary<string, (Vector3[] at, float yaw, string beside)> New = new Dictionary<string, (Vector3[], float, string)>
        {
            // The apothecary's laboratory, between the lab bench, the cauldron, and the work table (Victorian rebuild, VI-2).
            { "pip", (new[] { new Vector3(5.5f, 0f, 15.5f), new Vector3(9.5f, 0f, 14.5f), new Vector3(6.5f, 0f, 11.5f), new Vector3(9f, 0f, 12.5f) }, 180f, "harrow") },
            // The Soot & Bell, by a table of the public bar or the saloon (pub plan, VI-7); the evening and her tenement come with S5c.
            { "lottie", (new[] { new Vector3(15.5f, 0f, 1.5f), new Vector3(15.5f, 0f, 9.5f), new Vector3(6.6f, 0f, 2.5f) }, 270f, "nell") },
            // Bawling Row on the Penny's island (city plan C5, §29.4): on the walk between the trader's stall and the Master of
            // Enchanting's, or at the island's north corners beside the armour stand and the seer's candles.
            { "kit", (new[] { new Vector3(40f, 0f, 71.5f), new Vector3(32f, 0f, 68f), new Vector3(48f, 0f, 68f) }, 0f, "tobias") },
        };

        /// <summary>The people one talks to (S2.4): their conversation in <c>Assets/Data/Slice/Dialogue/</c>, and their reach, m.</summary>
        private static readonly Dictionary<string, (string dialogue, float reach)> Talks = new Dictionary<string, (string, float)>
        {
            { "nell", ("NellDialogue", 3f) },
            { "kit", ("KitDialogue", 2f) },
        };

        /// <summary>The conversation of the person <paramref name="id"/>, or null for one nobody talks to yet.</summary>
        public static DialogueDefinition DialogueOf(string id) => Talks.TryGetValue(id, out var talk)
            ? AssetDatabase.LoadAssetAtPath<DialogueDefinition>($"Assets/Data/Slice/Dialogue/{talk.dialogue}.asset")
            : null;

        public static string ScenePath(string scene) =>
            scene == SliceRoster.Veilmouth ? "Assets/Scenes/Slice/Veilmouth.unity" : $"Assets/Scenes/Slice/Interiors/{scene}.unity";

        [MenuItem("GameDev/Slice/People/Place the Named People in Veilmouth and the Interiors (once)")]
        public static void PlaceAll()
        {
            foreach (var group in SliceRoster.All.GroupBy(e => e.Scene))
            {
                var scene = EditorSceneManager.OpenScene(ScenePath(group.Key), OpenSceneMode.Single);
                Physics.SyncTransforms();
                var placed = new Dictionary<string, GameObject>();
                var changed = false;
                foreach (var entry in group.OrderBy(e => New.ContainsKey(e.Id)))
                {
                    placed[entry.Id] = Place(entry, placed, ref changed);
                }

                // A scene where everyone already stood is not saved: it may be the owner's (Veilmouth is).
                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }

            Debug.Log("[SlicePeople] The fourteen named people are placed.");
        }

        private static GameObject Place(SliceRoster.Entry entry, Dictionary<string, GameObject> placed, ref bool changed)
        {
            var existing = Object.FindObjectsByType<NamedPerson>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(p => p.Id == entry.Id);
            if (existing != null)
            {
                Debug.Log($"[SlicePeople] {entry.ObjectName} already stands in {entry.Scene}; left as it is.");
                return existing.gameObject;
            }

            changed = true;
            Transform parent;
            Vector3 position;
            Quaternion rotation;
            var sibling = -1;
            if (Replaces.TryGetValue(entry.Id, out var oldName))
            {
                var old = Dummies().Where(d => d.name == oldName).OrderBy(d => IsFemale(d) == entry.Female ? 0 : 1).FirstOrDefault();
                if (old == null)
                {
                    Debug.LogError($"[SlicePeople] {entry.Scene} has no Dummy '{oldName}' for {entry.ObjectName}.");
                    return null;
                }

                parent = old.transform.parent;
                position = old.transform.position;
                rotation = old.transform.rotation;
                sibling = old.transform.GetSiblingIndex();
                Object.DestroyImmediate(old);
            }
            else
            {
                var (at, yaw, beside) = New[entry.Id];
                parent = placed.TryGetValue(beside, out var friend) && friend != null ? friend.transform.parent : null;
                position = FirstFree(entry, at);
                rotation = Quaternion.Euler(0f, yaw, 0f);
            }

            VeilmouthDressing.Person(entry.Colour, entry.Female, position.x, position.z, rotation.eulerAngles.y, entry.ObjectName, position.y);
            var person = Dummies().Last(d => d.name == entry.ObjectName);
            person.transform.SetParent(parent, true);
            if (sibling >= 0)
            {
                // The old Dummy stood on its floor already; a new person keeps the height Person gave its feet.
                person.transform.SetPositionAndRotation(position, rotation);
                person.transform.SetSiblingIndex(sibling);
            }

            person.AddComponent<NamedPerson>().Configure(entry.Id);
            if (Talks.TryGetValue(entry.Id, out var talk))
            {
                person.AddComponent<PersonTalk>().Configure(DialogueOf(entry.Id), talk.reach);
            }

            Debug.Log($"[SlicePeople] {entry.ObjectName}, {entry.Role}: {entry.Scene} at {position}{(oldName != null ? $", in place of '{oldName}'" : string.Empty)}.");
            return person;
        }

        /// <summary>The first place of the list with ground under it and room for a person; the first when none is free.</summary>
        private static Vector3 FirstFree(SliceRoster.Entry entry, Vector3[] places)
        {
            var notCharacters = ~LayerMask.GetMask("Character");
            // Outside, from above the roofs; inside, from under the ceiling (3.48 m).
            var from = entry.Scene == SliceRoster.Veilmouth ? 40f : 1.2f;
            foreach (var place in places)
            {
                if (!Physics.Raycast(place + (Vector3.up * from), Vector3.down, out var hit, from + 40f, notCharacters, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                var feet = hit.point;
                var blocked = Physics.OverlapCapsule(feet + (Vector3.up * 0.4f), feet + (Vector3.up * 1.5f), 0.3f, notCharacters, QueryTriggerInteraction.Ignore);
                if (blocked.Length == 0)
                {
                    return feet;
                }

                Debug.Log($"[SlicePeople] {entry.ObjectName}: {place} is taken by {string.Join(", ", blocked.Select(c => c.name).Distinct())}.");
            }

            Debug.LogWarning($"[SlicePeople] {entry.ObjectName}: no free place on the list; the first is used.");
            return places[0];
        }

        // The people standing for roles: the Dummies before 2026-11-10, the owner's Tripo people since (asset map §1).
        private static IEnumerable<GameObject> Dummies() =>
            Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(t => t.gameObject)
                .Where(g => PrefabUtility.IsOutermostPrefabInstanceRoot(g)
                            && (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g).Contains(DummyFolder)
                                || PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g).StartsWith(SlicePeopleModels.Folder)));

        private static bool IsFemale(GameObject dummy)
        {
            var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(dummy);
            return path.Contains("HumanDummy_F") || SlicePeopleModels.All.Any(l => l.Female && l.Standing == path);
        }
    }
}
