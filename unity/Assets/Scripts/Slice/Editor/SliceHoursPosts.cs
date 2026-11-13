using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GameDev.Slice.Kit;
using GameDev.Slice.People;
using GameDev.World.Portals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// S5c.3, once (SLICE_PEOPLE §4; 18 *Townsfolk in the slice*): the owner's answer to S5-9 built
    /// (<see cref="SlicePeopleModels.S5Nine"/>), the old chapel's priest named Father Ambrose Hale, the nameless market
    /// trader at Bawling Row's food stall, and every person's posts: the roster's body marked as its primary post, and a
    /// body of the same person, switched off, at each other post its hours name: a table of the Soot &amp; Bell for the
    /// evening's drinkers, the Soot &amp; Bell's street door for Kit Mallow, the old chapel's forecourt for Madame Ilse,
    /// Lottie Wren's tenement door, Fishlip Quay and Veinmarket's den door for Dace. A post already placed is left as it is.
    /// -executeMethod GameDev.Slice.EditorTools.SliceHoursPosts.Run
    /// </summary>
    public static class SliceHoursPosts
    {
        public const string HaleName = "Father Ambrose Hale";
        public const string TraderName = "Market trader";
        private const string RootName = "People on their hours";
        private const string ReportPath = "Logs/SliceSurvey/hours_posts.txt";
        private const string ApothecaryScenePath = "Assets/Scenes/Slice/Interiors/HarrowApothecary.unity";
        private const string LockupScenePath = "Assets/Scenes/Slice/Interiors/WardensLockup.unity";
        private const string FactionHouseScenePath = "Assets/Scenes/Slice/Interiors/FactionHouse.unity";
        private const string BakeryPrefab = "Assets/Prefabs/SliceKit/Market/Kit_Market_Bakery_6x4.prefab";

        /// <summary>Veinmarket's den, a dwelling whose street door stands at world (148.0, 31.5) m (SliceAlchemy.DenDwelling).</summary>
        private static readonly Vector3 DenDoor = new Vector3(148f, 0f, 31.5f);

        /// <summary>Fishlip Quay at Tipsy Pier's root, plan (672, 440) of the C5 layout, world (172, 60) m.</summary>
        private static readonly Vector3 QuayAtPier = new Vector3(172f, 0f, 60f);

        /// <summary>The post the roster's body stands at: Lottie Wren's and Dace's is the Soot &amp; Bell, everyone else's its work.</summary>
        public static string PrimaryPost(string person) => person == "lottie" || person == "dace" ? SliceHours.Tavern : SliceHours.Work;

        /// <summary>The scene a person's post stands in.</summary>
        public static string SceneOf(string person, string post)
        {
            if (post == SliceHours.Work)
            {
                return person == SliceHours.MarketTrader ? SliceRoster.Veilmouth : SliceRoster.All.First(e => e.Id == person).Scene;
            }

            return post == SliceHours.Tavern ? SliceRoster.Tavern : SliceRoster.Veilmouth;
        }

        [MenuItem("GameDev/Slice/People/Hours: Posts and the S5-9 Models (S5c.3, once)")]
        public static void Run()
        {
            var report = new StringBuilder();
            SlicePeopleModels.S5Nine(report);
            foreach (var path in new[]
                     {
                         SliceActivities.ChapelScenePath, ApothecaryScenePath, LockupScenePath, FactionHouseScenePath,
                         VeilmouthScene.SootAndBellScenePath, VeilmouthScene.ScenePath,
                     })
            {
                Posts(path, report);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? ".");
            File.WriteAllText(ReportPath, report.ToString());
            Debug.Log("[SliceHoursPosts]\n" + report);
        }

        private static void Posts(string path, StringBuilder report)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Physics.SyncTransforms();
            report.AppendLine($"== {scene.name}");
            if (scene.name == SliceRoster.Chapel)
            {
                NameHale(scene, report);
            }

            if (scene.name == SliceRoster.Veilmouth)
            {
                PlaceTrader(scene, report);
            }

            foreach (var person in Object.FindObjectsByType<NamedPerson>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(p => p.gameObject.scene == scene))
            {
                if (SliceHours.Keeps(person.Id) && person.GetComponent<PersonPost>() == null)
                {
                    person.gameObject.AddComponent<PersonPost>().Configure(person.Id, PrimaryPost(person.Id), true);
                    report.AppendLine($"  {person.name}: its {PrimaryPost(person.Id)} post, primary");
                }
            }

            var tavernPlaces = scene.name == SliceRoster.Tavern ? TavernPlaces(scene, report) : new List<(Vector3, float)>();
            foreach (var pair in SliceHours.Days)
            {
                foreach (var post in SliceHours.PostsOf(pair.Key).Where(p => p != PrimaryPost(pair.Key) && SceneOf(pair.Key, p) == scene.name))
                {
                    if (Object.FindObjectsByType<PersonPost>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                        .Any(p => p.gameObject.scene == scene && p.Person == pair.Key && p.Post == post))
                    {
                        report.AppendLine($"  {pair.Key} at {post}: already placed");
                        continue;
                    }

                    (Vector3 at, float yaw)? place = post == SliceHours.Tavern ? Take(tavernPlaces) : PlaceOf(pair.Key, post);
                    if (place == null)
                    {
                        report.AppendLine($"  {pair.Key} at {post}: NO PLACE");
                        continue;
                    }

                    NewPost(scene, pair.Key, post, place.Value.at, place.Value.yaw);
                    report.AppendLine($"  {pair.Key} at {post}: {place.Value.at}, facing {place.Value.yaw:0}°");
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>The old chapel's priest is Father Ambrose Hale (SLICE_PEOPLE §1, row 15): named where he stands, keeping his hours.</summary>
        public static void NameHale(Scene scene, StringBuilder report)
        {
            var priest = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.gameObject.scene == scene && (t.name == HaleName || t.name == "Old priest") && PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject));
            if (priest == null)
            {
                report.AppendLine("  NO old priest in the chapel");
                return;
            }

            priest.name = HaleName;
            var named = priest.GetComponent<NamedPerson>() ?? priest.gameObject.AddComponent<NamedPerson>();
            named.Configure("hale");
            var post = priest.GetComponent<PersonPost>() ?? priest.gameObject.AddComponent<PersonPost>();
            post.Configure("hale", SliceHours.Work, true);
            EditorUtility.SetDirty(priest.gameObject);
            report.AppendLine($"  {HaleName} before the rostrum at {priest.position}");
        }

        // ------------------------------------------------------------------------------------------- the market trader

        /// <summary>
        /// The nameless old market trader (S5-9) in the innkeeper's model at Bawling Row's food stall, standing to it as
        /// Tobias Penn stands to his own stall: measured from the stall's front, which faces out from the market's middle.
        /// </summary>
        private static void PlaceTrader(Scene scene, StringBuilder report)
        {
            if (Object.FindObjectsByType<PersonPost>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(p => p.Person == SliceHours.MarketTrader))
            {
                report.AppendLine("  the market trader: already placed");
                return;
            }

            var stalls = SliceActivities.StallPrefabs.SelectMany(SliceActivities.Instances).ToList();
            var bakery = SliceActivities.Instances(BakeryPrefab).FirstOrDefault();
            var tobias = Object.FindObjectsByType<NamedPerson>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(p => p.Id == "tobias");
            if (bakery == null || tobias == null || stalls.Count == 0)
            {
                report.AppendLine("  the market trader: NO food stall or no Tobias Penn");
                return;
            }

            var his = stalls.OrderBy(s => SliceActivities.Flat(SliceActivities.Bounds(s).center - tobias.transform.position).sqrMagnitude).First();
            var (hisFront, hisOut, _) = Front(his, stalls);
            var outward = Vector3.Dot(SliceActivities.Flat(tobias.transform.position - hisFront), hisOut);
            var turn = Mathf.DeltaAngle(SliceActivities.Yaw(hisOut), tobias.transform.eulerAngles.y);
            var (front, out_, along) = Front(bakery, stalls);
            var at = new[] { 0f, 1f, -1f, 2f, -2f }.Select(d => SliceActivities.Grounded(front + (out_ * outward) + (along * d))).Where(Free).Cast<Vector3?>().FirstOrDefault();
            if (at == null)
            {
                report.AppendLine("  the market trader: NO ROOM before the food stall");
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlicePeopleModels.Innkeeper.Standing);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            go.name = TraderName;
            go.transform.SetParent(Root(scene), true);
            go.transform.SetPositionAndRotation(at.Value, Quaternion.Euler(0f, SliceActivities.Yaw(out_) + turn, 0f));
            if (go.TryGetComponent<PersonRole>(out var role))
            {
                // A trader of the market, no longer an innkeeper (VS-ROLES (a)): the model keeps the innkeeper's role for another city.
                role.Role = ResidentRole.Townsfolk;
                PrefabUtility.RecordPrefabInstancePropertyModifications(role);
            }

            go.AddComponent<PersonPost>().Configure(SliceHours.MarketTrader, SliceHours.Work, true);
            report.AppendLine($"  the market trader at the food stall: {at.Value}, {outward:0.00} m out from its front as Tobias Penn from his, turned {turn:0}°");
        }

        /// <summary>A stall's front, the way out from its market's middle, and the way along it.</summary>
        private static (Vector3 front, Vector3 out_, Vector3 along) Front(GameObject stall, List<GameObject> stalls)
        {
            var near = stalls.Where(s => SliceActivities.Flat(s.transform.position - stall.transform.position).magnitude < 30f).ToList();
            var market = near.Aggregate(Vector3.zero, (sum, s) => sum + SliceActivities.Bounds(s).center) / Mathf.Max(1, near.Count);
            var b = SliceActivities.Bounds(stall);
            var out_ = SliceActivities.Snap(SliceActivities.Flat(b.center - market));
            var along = Vector3.Cross(Vector3.up, out_);
            var front = b.center + (out_ * ((Mathf.Abs(out_.x) * b.extents.x) + (Mathf.Abs(out_.z) * b.extents.z)));
            return (front, out_, along);
        }

        // ------------------------------------------------------------------------------------------- the posts' places

        /// <summary>A post's place in Veilmouth, beside the door or on the quay it belongs to.</summary>
        private static (Vector3 at, float yaw)? PlaceOf(string person, string post)
        {
            switch (post)
            {
                case SliceHours.TavernDoor:
                    return BesideDoor(Door(VeilmouthScene.SootAndBellStreetPortalId), new[] { 3f, -3f, 4f, -4f }, 0.6f, false);
                case SliceHours.Forecourt:
                    return BesideDoor(Door(SliceActivities.ChapelStreetPortalId), new[] { 1.5f, -1.5f, 2.5f, -2.5f }, 3f, true);
                case SliceHours.Lane when person == "lottie":
                    return BesideDoor(Door("missing-room.street"), new[] { 1.6f, -1.6f, 2.4f, -2.4f }, 0.6f, false);
                case SliceHours.Lane:
                    return BesideDoor(DoorNear(DenDoor), new[] { 2.5f, -2.5f, 3.5f, -3.5f }, 0.8f, false);
                case SliceHours.Quay:
                    foreach (var dz in new[] { 0f, 2f, -2f, 4f, -4f, 6f, -6f })
                    {
                        foreach (var dx in new[] { 0f, -2f, 2f })
                        {
                            var at = Ground(QuayAtPier + new Vector3(dx, 0f, dz));
                            if (at.HasValue && at.Value.y > 24.5f && Free(at.Value))
                            {
                                return (at.Value, 90f); // facing the sea
                            }
                        }
                    }

                    return null;
                default:
                    return null;
            }
        }

        private static Portal Door(string id) =>
            Object.FindObjectsByType<Portal>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(p => p.Id == id);

        private static Portal DoorNear(Vector3 at) =>
            Object.FindObjectsByType<Portal>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(p => SliceActivities.Flat(p.transform.position - at).magnitude < 4f)
                .OrderBy(p => SliceActivities.Flat(p.transform.position - at).sqrMagnitude).FirstOrDefault();

        /// <summary>
        /// Beside a door's arrival place, <paramref name="out_"/> m out into the street and the first free of
        /// <paramref name="sides"/> m to its side, out of the door's own reach; facing the street, or the door.
        /// </summary>
        private static (Vector3 at, float yaw)? BesideDoor(Portal door, float[] sides, float out_, bool facingDoor)
        {
            if (door == null)
            {
                return null;
            }

            var pose = door.ArrivalPose;
            var forward = SliceActivities.Flat(pose.forward).normalized;
            var side = Vector3.Cross(Vector3.up, forward);
            foreach (var s in sides)
            {
                var at = Ground(pose.position + (forward * out_) + (side * s));
                if (at.HasValue && Free(at.Value) && !door.InReach(at.Value))
                {
                    return (at.Value, SliceActivities.Yaw(facingDoor ? -forward : forward));
                }
            }

            return null;
        }

        /// <summary>
        /// The Soot &amp; Bell's places for the evening's drinkers: beside a table on Nell Garrow's floor, facing it, clear of
        /// furniture, of every person and every activity's place, of its doors, and 1.5 m from one another; nearest Lottie
        /// Wren first, where the evening sits.
        /// </summary>
        private static List<(Vector3 at, float yaw)> TavernPlaces(Scene scene, StringBuilder report)
        {
            var people = Object.FindObjectsByType<NamedPerson>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(p => p.gameObject.scene == scene).ToList();
            var nell = people.FirstOrDefault(p => p.Id == "nell");
            var lottie = people.FirstOrDefault(p => p.Id == "lottie");
            var floor = nell != null ? nell.transform.position.y : 0f;
            var taken = people.Select(p => p.transform.position)
                .Concat(Object.FindObjectsByType<TownsfolkWalker>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(w => w.gameObject.scene == scene).Select(w => w.transform.position))
                .ToList();
            var places = Object.FindObjectsByType<ActivitySpot>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(s => s.gameObject.scene == scene)
                .SelectMany(s => Enumerable.Range(0, s.Count).SelectMany(i => new[] { s.PlaceAt(i).Stand, s.PlaceAt(i).Seat })).ToList();
            var doors = Object.FindObjectsByType<Portal>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(p => p.gameObject.scene == scene).ToList();
            var tables = Object.FindObjectsByType<KitBlock>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(b => b.gameObject.scene == scene && (b.name.StartsWith("Kit_ViFurniture_TableRoundSet") || b.name.StartsWith("Kit_ViFurniture_Table_") || b.name.StartsWith("Kit_ViFurniture_TableChess")))
                .ToList();
            var found = new List<(Vector3 at, float yaw)>();
            foreach (var table in tables)
            {
                var b = SliceActivities.Bounds(table.gameObject);
                foreach (var way in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
                {
                    var reach = (Mathf.Abs(way.x) * b.extents.x) + (Mathf.Abs(way.z) * b.extents.z) + 0.55f;
                    var p = SliceActivities.Grounded(new Vector3(b.center.x, floor, b.center.z) + (way * reach));
                    if (Mathf.Abs(p.y - floor) > 0.15f || !Free(p)
                        || taken.Any(t => SliceActivities.Flat(t - p).magnitude < 1.2f)
                        || places.Any(t => SliceActivities.Flat(t - p).magnitude < 0.8f)
                        || doors.Any(d => d.InReach(p) || SliceActivities.Flat(d.transform.position - p).magnitude < 2f))
                    {
                        continue;
                    }

                    found.Add((p, SliceActivities.Yaw(-way)));
                }
            }

            var from = lottie != null ? lottie.transform.position : Vector3.zero;
            var chosen = new List<(Vector3 at, float yaw)>();
            foreach (var place in found.OrderBy(f => SliceActivities.Flat(f.at - from).sqrMagnitude).ThenBy(f => f.at.x).ThenBy(f => f.at.z))
            {
                if (chosen.All(c => SliceActivities.Flat(c.at - place.at).magnitude >= 1.5f))
                {
                    chosen.Add(place);
                }
            }

            report.AppendLine($"  the Soot & Bell: {tables.Count} tables, {found.Count} free places beside them, {chosen.Count} 1.5 m apart");
            return chosen;
        }

        private static (Vector3 at, float yaw)? Take(List<(Vector3 at, float yaw)> places)
        {
            if (places.Count == 0)
            {
                return null;
            }

            var first = places[0];
            places.RemoveAt(0);
            return first;
        }

        // ------------------------------------------------------------------------------------------- the bodies

        /// <summary>A body of the person at another post: the roster body's model, named and tagged as it, switched off.</summary>
        private static void NewPost(Scene scene, string id, string post, Vector3 at, float yaw)
        {
            var entry = SliceRoster.All.First(e => e.Id == id);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlicePeopleModels.LookFor(entry.ObjectName, entry.Female).Standing);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            go.name = entry.ObjectName;
            go.transform.SetParent(Root(scene), true);
            go.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            go.AddComponent<NamedPerson>().Configure(id);
            var reach = SlicePeople.TalkReach(id);
            if (reach.HasValue)
            {
                go.AddComponent<PersonTalk>().Configure(SlicePeople.DialogueOf(id), reach.Value);
            }

            go.AddComponent<PersonPost>().Configure(id, post, false);
            go.SetActive(false);
        }

        private static Transform Root(Scene scene)
        {
            var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
            if (root == null)
            {
                root = new GameObject(RootName);
                SceneManager.MoveGameObjectToScene(root, scene);
            }

            return root.transform;
        }

        /// <summary>Room for a person: nothing solid and no one from the knees to the head.</summary>
        private static bool Free(Vector3 p) =>
            !Physics.CheckCapsule(p + (Vector3.up * 0.45f), p + (Vector3.up * 1.5f), 0.3f, SliceActivities.Blocking | LayerMask.GetMask(KitLayers.Character), QueryTriggerInteraction.Ignore);

        /// <summary>The ground under a place of the open city, from above its roofs.</summary>
        private static Vector3? Ground(Vector3 p) =>
            Physics.Raycast(new Vector3(p.x, p.y + 60f, p.z), Vector3.down, out var hit, 160f, SliceTownsfolk.GroundMask, QueryTriggerInteraction.Ignore)
                ? hit.point
                : (Vector3?)null;
    }
}
