using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GameDev.Slice.Kit;
using GameDev.Slice.People;
using GameDev.World.Portals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// AN6 (the owner's direction of 2026-11-11: the townsfolk sit on benches, bargain at stalls, go to the tavern and the
    /// chapel, drink and talk; "random activities"): the <see cref="ActivitySpot"/>s and who visits them, built again on
    /// every run.
    /// <list type="bullet">
    /// <item>Veilmouth: every street bench, its seat read from its own mesh (the top of its up-facing faces 0.25–0.8 m
    /// over its foot, its back from the faces above that), a place to every metre of seat, at most four; Bawling Row's six
    /// stalls, a customer before each seller, or two before a stall with none; the Soot &amp; Bell's and the old chapel's
    /// street doors, three places each where an arriving player stands. Each walker's visits are the places within 40 m
    /// of a turn of its route to which a straight walk is clear: ground under every half metre, no step over 0.6 m, and
    /// the route test's capsule clear of every structure, prop and tree but the spot's own piece and the last 0.6 m.</item>
    /// <item>The Soot &amp; Bell: two drinkers at the two nearest free chairs, talking, and one at the bar, drinking, 1.6 m
    /// or more along it from the place before Nell.</item>
    /// <item>The old chapel: three people standing quietly in its pews' rows, with no prayer clip to sit in them.</item>
    /// </list>
    /// Distances are authoring defaults. Writes <c>Logs/SliceSurvey/activities.txt</c>. Batch:
    /// -executeMethod GameDev.Slice.EditorTools.SliceActivities.Run; every seat of the kit, <see cref="SurveySeats"/>.
    /// </summary>
    public static class SliceActivities
    {
        public const string RootName = "Activity Spots";
        public const string BenchPrefab = "Assets/Prefabs/SliceKit/Street/Kit_Street_Bench_3x1.prefab";
        public const string PewPrefab = "Assets/Prefabs/SliceKit/Furniture/Kit_Furniture_Pew_4x1.prefab";
        public const string ChapelScenePath = "Assets/Scenes/Slice/Interiors/OldChapel.unity";
        public const string ChapelStreetPortalId = "old-chapel.street";
        public const string RostrumPrefab = "Assets/Prefabs/SliceKit/Furniture/Kit_Furniture_Rostrum_1x2.prefab";

        public static readonly string[] StallPrefabs =
        {
            "Assets/Prefabs/SliceKit/Market/Kit_Market_Goods_6x3.prefab", "Assets/Prefabs/SliceKit/Market/Kit_Market_Awning_6x4.prefab",
            "Assets/Prefabs/SliceKit/Market/Kit_Market_Bakery_6x4.prefab", "Assets/Prefabs/SliceKit/Market/Kit_Market_Fish_7x4.prefab",
            "Assets/Prefabs/SliceKit/Market/Kit_Market_Weapons_9x4.prefab", "Assets/Prefabs/SliceKit/Market/Kit_Market_Meat_7x4.prefab",
        };

        public static readonly string[] ChairPrefabs =
        {
            "Assets/Prefabs/SliceKit/BkFurniture/Kit_BkFurniture_Chair_1x1.prefab", "Assets/Prefabs/SliceKit/ViFurniture/Kit_ViFurniture_Chair_1x1.prefab",
        };

        /// <summary>The owner's seat size (2026-11-12): a metre of seat to a person, skirts and hands on the thighs 0.6–0.96 m wide (measured).</summary>
        public const float PlaceWidth = 1f;

        /// <summary>Authoring defaults, m: how far a visit may lead from its turn, and the walk's checks.</summary>
        public const float VisitReach = 40f;

        public const float Sample = 0.5f;
        public const float MaxStep = 0.6f;
        public const float FreeEnd = 0.6f;

        /// <summary>Authoring defaults, s: how long a visit lasts at each kind of spot.</summary>
        public static readonly (float shortest, float longest) Bench = (20f, 60f), Stall = (15f, 40f), Tavern = (60f, 180f), Chapel = (40f, 120f);

        private const string Report = "Logs/SliceSurvey/activities.txt";

        private static readonly Dictionary<string, int> Blockers = new Dictionary<string, int>();

        public static int Blocking => LayerMask.GetMask("Default", KitLayers.Structure, KitLayers.Prop, KitLayers.Vegetation, KitLayers.Interactable);

        [MenuItem("GameDev/Slice/Townsfolk/Place the Activity Spots and Their Visitors (AN6)")]
        public static void Run()
        {
            var report = new StringBuilder("AN6: the townsfolk's Activity Spots (SliceActivities)\n\n");
            City(report);
            Tavern_(report);
            Chapel_(report);
            Directory.CreateDirectory(Path.GetDirectoryName(Report) ?? ".");
            File.WriteAllText(Report, report.ToString());
            Debug.Log($"[SliceActivities] written {Report}");
        }

        private const string SeatReport = "Logs/SliceSurvey/seats.txt";

        private static readonly Regex SeatNames = new Regex("chair|(?<!lab)bench|pew|stool|sofa|settee", RegexOptions.IgnoreCase);

        /// <summary>
        /// Every seat of the kit read as the townsfolk read a bench, each on a floor of its own in an empty scene (the
        /// owner's question of 2026-11-12, which seats a person can sit on). Writes <c>Logs/SliceSurvey/seats.txt</c>. Batch:
        /// -executeMethod GameDev.Slice.EditorTools.SliceActivities.SurveySeats
        /// </summary>
        [MenuItem("GameDev/Slice/Townsfolk/Measure Every Seat of the Kit")]
        public static void SurveySeats()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.layer = LayerMask.NameToLayer(KitLayers.Ground);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(400f, 1f, 20f);
            var paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/SliceKit" }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => SeatNames.IsMatch(Path.GetFileNameWithoutExtension(p))).OrderBy(p => p).ToList();
            var report = new StringBuilder($"Every seat of the kit ({paths.Count}), read as the townsfolk read a bench (SliceActivities.Seats)\n\n");
            var places = new StringBuilder();
            var x = 0f;
            foreach (var path in paths)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                go.transform.position = new Vector3(x, 0f, 0f);
                x += 8f;
                Physics.SyncTransforms();
                var read = Seats(go, report);
                if (read.Length == 0)
                {
                    report.AppendLine($"    ({path})");
                }
                else
                {
                    // Turned to face -z for ActivityPlayTests.RenderTheKitSeats, and read again.
                    go.transform.RotateAround(go.transform.position, Vector3.up, 180f - read[0].Yaw);
                    Physics.SyncTransforms();
                    read = Seats(go, null);
                }

                places.AppendLine($"kit\t{path}\t{go.transform.eulerAngles.y.ToString("0.###", CultureInfo.InvariantCulture)}\t{Line(read, go.transform.position)}");
            }

            // Every seat's places in the Soot & Bell and the old chapel, for ActivityPlayTests.RenderTheSeatsInPlace.
            foreach (var scene in new[] { VeilmouthScene.SootAndBellScenePath, ChapelScenePath })
            {
                EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
                Physics.SyncTransforms();
                foreach (var go in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Select(t => t.gameObject)
                             .Where(g => PrefabUtility.IsOutermostPrefabInstanceRoot(g) && SeatNames.IsMatch(Path.GetFileNameWithoutExtension(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g)))))
                {
                    places.AppendLine($"room\t{scene}\t0\t{Line(Seats(go, null), Vector3.zero)}");
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(SeatReport) ?? ".");
            File.WriteAllText(SeatReport, report.ToString());
            File.WriteAllText(SeatPlaces, places.ToString());
            Debug.Log($"[SliceActivities] written {SeatReport}, {SeatPlaces}");
        }

        private const string SeatPlaces = "Logs/SliceSurvey/seat_places.txt";

        /// <summary>Places as text, relative to <paramref name="from"/>: seat x y z, stand x y z, yaw, separated by ';'.</summary>
        private static string Line(IEnumerable<ActivitySpot.Place> places, Vector3 from) =>
            string.Join(";", places.Select(p => string.Join(" ", new[] { p.Seat.x - from.x, p.Seat.y - from.y, p.Seat.z - from.z, p.Stand.x - from.x, p.Stand.y - from.y, p.Stand.z - from.z, p.Yaw }
                .Select(f => f.ToString("0.####", CultureInfo.InvariantCulture)))));

        // ------------------------------------------------------------------------------------------- the city

        private static void City(StringBuilder report)
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
            var root = Fresh(scene);
            var spots = new List<ActivitySpot>();

            var benches = Instances(BenchPrefab).OrderBy(b => b.transform.position.x).ThenBy(b => b.transform.position.z).ToList();
            foreach (var bench in benches)
            {
                var places = Seats(bench, report);
                if (places.Length > 0)
                {
                    spots.Add(Spot(root, $"Bench {spots.Count + 1}", ActivitySpot.Activity.Sit, false, Bench, places, bench.transform));
                }
            }

            report.AppendLine($"Veilmouth: {benches.Count} benches, {spots.Count} with a seat read, {spots.Sum(s => s.Count)} places");

            var stalls = StallPrefabs.SelectMany(Instances).OrderBy(s => s.transform.position.x).ThenBy(s => s.transform.position.z).ToList();
            var sellers = Object.FindObjectsByType<NamedPerson>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(p => p.transform.position).ToList();
            foreach (var stall in stalls)
            {
                var places = Customers(stall, stalls, sellers);
                if (places.Length > 0)
                {
                    spots.Add(Spot(root, $"Stall: {stall.name}", ActivitySpot.Activity.Bargain, false, Stall, places, stall.transform));
                }

                report.AppendLine($"  stall {stall.name}: {places.Length} customers' places");
            }

            foreach (var (id, name, length) in new[] { (VeilmouthScene.SootAndBellStreetPortalId, "The Soot & Bell's door", Tavern), (ChapelStreetPortalId, "The old chapel's door", Chapel) })
            {
                var portal = Object.FindObjectsByType<Portal>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(p => p.Id == id);
                if (portal == null)
                {
                    report.AppendLine($"  NO DOOR {id}");
                    continue;
                }

                var pose = portal.ArrivalPose;
                var facing = -Flat(pose.forward).normalized;
                var side = Vector3.Cross(Vector3.up, facing);
                var places = new[] { 0f, 0.7f, -0.7f }.Select(s => pose.position + (side * s))
                    .Where(p => !Physics.CheckCapsule(p + (Vector3.up * 0.75f), p + (Vector3.up * 1.45f), 0.25f, Blocking, QueryTriggerInteraction.Ignore))
                    .Select(p => new ActivitySpot.Place { Stand = Grounded(p), Seat = Grounded(p), Yaw = Yaw(facing) }).ToArray();
                spots.Add(Spot(root, name, ActivitySpot.Activity.GoIn, false, length, places, portal.transform));
                report.AppendLine($"  {name}: {places.Length} places");
            }

            // Each walker's visits.
            var walkers = Object.FindObjectsByType<TownsfolkWalker>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(w => w.Route.Length >= 3).ToList();
            var visits = spots.ToDictionary(s => s, _ => new List<ActivitySpot.Visit>());
            report.AppendLine();
            foreach (var w in walkers.OrderBy(w => w.name))
            {
                var seed = TownsfolkWalker.SeedOf(w.Route);
                var count = 0;
                var near = 0;
                for (var turn = 0; turn < w.Route.Length; turn++)
                {
                    foreach (var spot in spots)
                    {
                        for (var place = 0; place < spot.Count; place++)
                        {
                            var stand = spot.PlaceAt(place).Stand;
                            if (Flat(stand - w.Route[turn]).magnitude > VisitReach)
                            {
                                continue;
                            }

                            near++;
                            var blocker = Blocker(w.Route[turn], stand, spot.Piece);
                            if (blocker != null)
                            {
                                var key = System.Text.RegularExpressions.Regex.Replace(blocker, @" \(\d+\)", string.Empty);
                                Blockers[key] = (Blockers.TryGetValue(key, out var n) ? n : 0) + 1;
                            }
                            else
                            {
                                visits[spot].Add(new ActivitySpot.Visit { Walker = seed, Turn = turn, At = w.Route[turn], Place = place });
                                count++;
                            }
                        }
                    }
                }

                report.AppendLine($"  {w.name}: {count} visits from {w.Route.Length} turns, of {near} places within reach");
            }

            foreach (var pair in visits)
            {
                pair.Key.SetVisits(pair.Value.ToArray());
                EditorUtility.SetDirty(pair.Key);
            }

            report.AppendLine("What stops the walks within reach: " + string.Join(", ", Blockers.OrderByDescending(b => b.Value).Take(14).Select(b => $"{b.Key} {b.Value}")));
            report.AppendLine($"Veilmouth: {spots.Count} spots, {spots.Sum(s => s.Count)} places, {visits.Values.Sum(v => v.Count)} visits for {walkers.Count} walkers, " +
                              $"{walkers.Count(w => visits.Values.Any(v => v.Any(x => x.Walker == TownsfolkWalker.SeedOf(w.Route))))} of them with one at least");
            report.AppendLine();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>A customer's place before a stall: before its seller, facing it, or two before a stall with none.</summary>
        private static ActivitySpot.Place[] Customers(GameObject stall, List<GameObject> stalls, List<Vector3> sellers)
        {
            // The stall's front faces out from the middle of its market (VeilmouthCity.Market: every stall faces the ring's road).
            var market = stalls.Where(s => Flat(s.transform.position - stall.transform.position).magnitude < 30f).Select(Centre).Aggregate(Vector3.zero, (a, b) => a + b);
            market /= stalls.Count(s => Flat(s.transform.position - stall.transform.position).magnitude < 30f);
            var bounds = Bounds(stall);
            var out_ = Snap(Flat(bounds.center - market));
            var along = Vector3.Cross(Vector3.up, out_);
            var front = bounds.center + (out_ * ((Mathf.Abs(out_.x) * bounds.extents.x) + (Mathf.Abs(out_.z) * bounds.extents.z)));
            var halfLength = (Mathf.Abs(along.x) * bounds.extents.x) + (Mathf.Abs(along.z) * bounds.extents.z);
            var seller = sellers.Where(s => Mathf.Abs(Vector3.Dot(Flat(s - front), along)) < halfLength && Vector3.Dot(Flat(s - front), out_) is var o && o > 0f && o < 3f)
                .OrderBy(s => Flat(s - front).magnitude).Cast<Vector3?>().FirstOrDefault();
            var points = seller.HasValue
                ? new[] { seller.Value + (out_ * 1.2f), seller.Value + (out_ * 1.2f) + (along * 1.4f) }
                : new[] { front + (out_ * 0.9f) + (along * 1.2f), front + (out_ * 0.9f) - (along * 1.2f) };
            return points.Select(Grounded)
                .Where(p => !Physics.CheckCapsule(p + (Vector3.up * 0.75f), p + (Vector3.up * 1.45f), 0.25f, Blocking, QueryTriggerInteraction.Ignore))
                .Select(p => new ActivitySpot.Place { Stand = p, Seat = p, Yaw = Yaw(seller.HasValue ? Flat(seller.Value - p) : -out_) }).ToArray();
        }

        // ------------------------------------------------------------------------------------------- the interiors

        private static void Tavern_(StringBuilder report)
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.SootAndBellScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
            var root = Fresh(scene);
            var people = Object.FindObjectsByType<NamedPerson>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();

            // The chairs clear of the named people, the two nearest one another, at one table.
            var chairs = ChairPrefabs.SelectMany(Instances).Where(c => people.All(p => Flat(p.transform.position - c.transform.position).magnitude > 1f))
                .OrderBy(c => c.transform.position.x).ThenBy(c => c.transform.position.z).ToList();
            var seats = chairs.Select(c => Seats(c, report).FirstOrDefault()).Where(p => p.Seat != Vector3.zero).ToList();
            var pair = (from a in Enumerable.Range(0, seats.Count) from b in Enumerable.Range(0, seats.Count)
                        where a < b && Flat(seats[a].Seat - seats[b].Seat).magnitude > 0.5f
                        orderby Flat(seats[a].Seat - seats[b].Seat).magnitude
                        select (a, b)).FirstOrDefault();
            var chairSpot = Spot(root, "The taproom's chairs", ActivitySpot.Activity.Sit, false, Tavern, seats.ToArray());
            if (seats.Count >= 2)
            {
                Resident(scene, "Assets/Prefabs/Slice/People/GentlemanWalker.prefab", "Drinker at a table", chairSpot, pair.a);
                Resident(scene, "Assets/Prefabs/Slice/People/WorkingWomanWalker.prefab", "Drinker at a table (woman)", chairSpot, pair.b);
                report.AppendLine($"The Soot & Bell: {chairs.Count} chairs clear of its people, {seats.Count} seats read; the drinkers at chairs {pair.a} and {pair.b}, " +
                                  $"{Flat(seats[pair.a].Seat - seats[pair.b].Seat).magnitude:0.00} m apart");
            }
            else
            {
                report.AppendLine($"The Soot & Bell: {chairs.Count} chairs clear of its people, {seats.Count} seats read: NO PAIR");
            }

            // The bar: its customers' side away from the barkeep, a place 0.45 m before its front, and at least 1.6 m along it
            // from the place before her, so the player still walks up to her and talks (ConversationTests).
            var bar = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == "Bar" && PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject));
            var nell = people.FirstOrDefault(p => p.Id == "nell");
            if (bar != null && nell != null)
            {
                var bounds = Bounds(bar.gameObject);
                var out_ = Snap(Flat(bounds.center - nell.transform.position));
                var along = Vector3.Cross(Vector3.up, out_);
                var front = bounds.center + (out_ * ((Mathf.Abs(out_.x) * bounds.extents.x) + (Mathf.Abs(out_.z) * bounds.extents.z) + 0.45f));
                var length = (Mathf.Abs(along.x) * bounds.extents.x) + (Mathf.Abs(along.z) * bounds.extents.z);
                var herAlong = Vector3.Dot(Flat(nell.transform.position - bounds.center), along);
                var places = new[] { 2.4f, -2.4f, 1.6f, -1.6f }.Where(d => Mathf.Abs(herAlong + d) <= length + 0.2f).Select(d => Grounded(front + (along * (herAlong + d))))
                    .Where(p => !Physics.CheckCapsule(p + (Vector3.up * 0.75f), p + (Vector3.up * 1.45f), 0.25f, Blocking, QueryTriggerInteraction.Ignore))
                    .Select(p => new ActivitySpot.Place { Stand = p, Seat = p, Yaw = Yaw(-out_) }).ToArray();
                var barSpot = Spot(root, "The bar", ActivitySpot.Activity.Drink, false, Tavern, places);
                if (places.Length > 0)
                {
                    Resident(scene, SliceTownsfolk.PrefabPath, "Drinker at the bar", barSpot, 0);
                }

                report.AppendLine($"  the bar: {length * 2f:0.0} m long, {places.Length} places before it 1.6 m or more along it from Nell, {(places.Length > 0 ? "a drinker at the first" : "NO ROOM")}");
            }
            else
            {
                report.AppendLine("  NO BAR or no barkeep");
            }

            report.AppendLine();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// The old chapel's people stand quietly in its pews' rows, facing the rostrum, since neither clip pack has a prayer
        /// (99 VS-ANIM); the pew, 0.85 m before, has stood 0.42 m since the owner's seat trial (99 VS-SEAT).
        /// </summary>
        private static void Chapel_(StringBuilder report)
        {
            var scene = EditorSceneManager.OpenScene(ChapelScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
            var root = Fresh(scene);
            var rostrum = Instances(RostrumPrefab).FirstOrDefault();
            var pews = Instances(PewPrefab).ToList();
            foreach (var pew in pews.Take(1))
            {
                Seats(pew, report);
            }

            var places = new List<ActivitySpot.Place>();
            if (rostrum != null)
            {
                foreach (var pew in pews.OrderBy(p => Flat(p.transform.position - rostrum.transform.position).magnitude))
                {
                    var box = Bounds(pew);
                    var toward = Snap(Flat(rostrum.transform.position - box.center));
                    var along = Vector3.Cross(Vector3.up, toward);
                    var front = box.center + (toward * ((Mathf.Abs(toward.x) * box.extents.x) + (Mathf.Abs(toward.z) * box.extents.z) + 0.35f));
                    places.AddRange(new[] { -1f, 1f }.Select(o => Grounded(front + (along * o)))
                        .Where(p => !Physics.CheckCapsule(p + (Vector3.up * 0.75f), p + (Vector3.up * 1.45f), 0.25f, Blocking, QueryTriggerInteraction.Ignore))
                        .Select(p => new ActivitySpot.Place { Stand = p, Seat = p, Yaw = Yaw(toward) }));
                }
            }

            var spot = Spot(root, "Before the pews", ActivitySpot.Activity.Attend, true, Chapel, places.ToArray());
            var who = new[] { (SliceTownsfolk.WomanPrefabPath, "Worshipper (matron)"), ("Assets/Prefabs/Slice/People/LadyWalker.prefab", "Worshipper (lady)"), ("Assets/Prefabs/Slice/People/GentlemanWalker.prefab", "Worshipper") };
            var chosen = new[] { 0, places.Count / 2, places.Count - 1 }.Distinct().Take(places.Count).ToArray();
            for (var i = 0; i < who.Length && i < chosen.Length; i++)
            {
                Resident(scene, who[i].Item1, who[i].Item2, spot, chosen[i]);
            }

            report.AppendLine($"The old chapel: {pews.Count} pews, {places.Count} places before them facing {(rostrum != null ? "the rostrum" : "NO ROSTRUM")}, {Mathf.Min(who.Length, chosen.Length)} people standing there");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>A person with no route who keeps <paramref name="place"/> of <paramref name="spot"/>, under the scene's People.</summary>
        private static void Resident(UnityEngine.SceneManagement.Scene scene, string prefabPath, string name, ActivitySpot spot, int place)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null || place < 0 || place >= spot.Count)
            {
                Debug.LogWarning($"[SliceActivities] no {prefabPath} or place {place} for {name}");
                return;
            }

            var people = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "People") ?? new GameObject("People");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            go.name = name;
            go.transform.SetParent(people.transform, true);
            var p = spot.PlaceAt(place);
            go.transform.SetPositionAndRotation(p.Stand, Quaternion.Euler(0f, p.Yaw, 0f));
            var walker = go.GetComponent<TownsfolkWalker>();
            walker.Configure(new Vector3[0], walker.Speed, SliceTownsfolk.GroundMask);
            walker.SetHome(spot, place);
            EditorUtility.SetDirty(walker);
        }

        // ------------------------------------------------------------------------------------------- shared

        /// <summary>The scene's spots root emptied, and its people with no route gone, so a run starts afresh.</summary>
        private static Transform Fresh(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == RootName))
            {
                Object.DestroyImmediate(old);
            }

            foreach (var w in Object.FindObjectsByType<TownsfolkWalker>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                         .Where(w => w.gameObject.scene == scene && w.Route.Length == 0 && (w.Home != null || IsResidentName(w.name))).ToList())
            {
                Object.DestroyImmediate(w.gameObject);
            }

            var root = new GameObject(RootName);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            return root.transform;
        }

        private static bool IsResidentName(string name) => name.StartsWith("Drinker") || name.StartsWith("Worshipper");

        private static ActivitySpot Spot(Transform root, string name, ActivitySpot.Activity kind, bool quiet, (float shortest, float longest) length, ActivitySpot.Place[] places, Transform piece = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.position = places.Length > 0 ? places[0].Stand : root.position;
            var spot = go.AddComponent<ActivitySpot>();
            spot.Configure(kind, quiet, length.shortest, length.longest, places, piece);
            return spot;
        }

        /// <summary>
        /// A seat's places read from its own mesh: the top of its up-facing faces 0.25–0.8 m over its foot, most area at
        /// one height; its front away from its back (the faces standing 0.15 m and more over the seat), else its own
        /// forward; a place to every metre of its length, up to four, the hips' point <see cref="TownsfolkWalker.HipsBehindFront"/>
        /// behind its front edge; none on a seat outside <see cref="TownsfolkWalker.SeatLowest"/>–<see cref="TownsfolkWalker.SeatHighest"/>.
        /// </summary>
        public static ActivitySpot.Place[] Seats(GameObject seat, StringBuilder report)
        {
            var tris = Triangles(seat);
            if (tris.Count == 0)
            {
                return new ActivitySpot.Place[0];
            }

            // Heights over the floor under it, which a model sunk into a raised floor stands below (the chapel's pews, AN6);
            // over its own foot where no floor is found.
            var foot = tris.Min(t => Mathf.Min(t.a.y, Mathf.Min(t.b.y, t.c.y)));
            var box = Bounds(seat);
            var floorHit = Physics.RaycastAll(new Vector3(box.center.x, box.max.y + 0.5f, box.center.z), Vector3.down, box.size.y + 3f, SliceTownsfolk.GroundMask, QueryTriggerInteraction.Ignore)
                .Where(h => !h.collider.transform.IsChildOf(seat.transform) && h.point.y < box.center.y).OrderByDescending(h => h.point.y).FirstOrDefault();
            var sunk = floorHit.collider != null ? floorHit.point.y - foot : 0f;
            foot += sunk;
            var over = MostAreaUp(tris, foot, 0.8f);
            if (over == null)
            {
                // What faces up, by 5 cm of height over the foot, the most area first, and how much it leans.
                var up = tris.Select(t => (n: Vector3.Cross(t.b - t.a, t.c - t.a), h: ((t.a.y + t.b.y + t.c.y) / 3f) - foot))
                    .Where(t => t.n.magnitude > 2e-6f && Mathf.Abs(t.n.normalized.y) > 0.5f)
                    .GroupBy(t => (Mathf.RoundToInt(t.h / 0.05f), t.n.y > 0f))
                    .Select(g => (h: g.Key.Item1 * 0.05f, upward: g.Key.Item2, area: g.Sum(t => t.n.magnitude * 0.5f), lean: g.Average(t => Mathf.Abs(t.n.normalized.y))))
                    .OrderByDescending(g => g.area).Take(6);
                report?.AppendLine($"  {seat.name}: NO SEAT among {tris.Count} faces; facing up or down: {string.Join(", ", up.Select(g => $"{g.h:0.00} m {(g.upward ? "up" : "down")} {g.area:0.00} m² (n.y {g.lean:0.00})"))}");
                return new ActivitySpot.Place[0];
            }

            var top = foot + over.Value;
            Vector3 seatSum = Vector3.zero, backSum = Vector3.zero;
            float seatArea = 0f, backArea = 0f, backTop = top, armSum = 0f, armArea = 0f;
            var seatPoints = new List<Vector3>();
            foreach (var (a, b, c) in tris)
            {
                var n = Vector3.Cross(b - a, c - a);
                var area = n.magnitude * 0.5f;
                if (area < 1e-6f)
                {
                    continue;
                }

                var centre = (a + b + c) / 3f;
                if (n.normalized.y > 0.9f && Mathf.Abs(centre.y - top) < 0.03f)
                {
                    seatSum += centre * area;
                    seatArea += area;
                    seatPoints.Add(a);
                    seatPoints.Add(b);
                    seatPoints.Add(c);
                }
                else if (Mathf.Abs(n.normalized.y) < 0.5f && centre.y > top + 0.15f)
                {
                    backSum += centre * area;
                    backArea += area;
                    backTop = Mathf.Max(backTop, Mathf.Max(a.y, Mathf.Max(b.y, c.y)));
                }
                else if (n.normalized.y > 0.9f && centre.y > top + 0.1f && centre.y <= top + 0.45f)
                {
                    // Arms, or what else faces up a little over the seat.
                    armSum += centre.y * area;
                    armArea += area;
                }
            }

            var middle = seatSum / seatArea;
            var forward = backArea > 0.02f ? Flat(middle - (backSum / backArea)).normalized : Flat(seat.transform.forward).normalized;
            var along = Vector3.Cross(Vector3.up, forward);
            var ends = seatPoints.Select(p => Vector3.Dot(p - middle, along)).ToList();
            var length = ends.Max() - ends.Min();
            var count = Mathf.Clamp(Mathf.FloorToInt(length / PlaceWidth), 1, 4);
            var depths = seatPoints.Select(p => Vector3.Dot(p - middle, forward)).ToList();
            var mid = middle + (along * ((ends.Max() + ends.Min()) * 0.5f)) + (forward * (depths.Max() - TownsfolkWalker.HipsBehindFront));
            var places = new ActivitySpot.Place[count];
            for (var i = 0; i < count; i++)
            {
                var point = mid + (along * (length * (((i + 0.5f) / count) - 0.5f)));
                point.y = top;
                var stand = Grounded(point + (forward * TownsfolkWalker.SeatedHipsBack));
                places[i] = new ActivitySpot.Place { Seat = point, Stand = stand, Yaw = Yaw(forward) };
            }

            var arms = armArea > 0.005f ? $", something facing up {(armSum / armArea) - top:0.00} m over it" : string.Empty;
            var fits = top - foot >= TownsfolkWalker.SeatLowest && top - foot <= TownsfolkWalker.SeatHighest;
            var verdict = !fits ? $"NOT SAT ON: outside {TownsfolkWalker.SeatLowest:0.00}–{TownsfolkWalker.SeatHighest:0.00} m"
                : Mathf.Abs(top - foot - TownsfolkWalker.SeatTop) > 0.03f ? $"{count} places, {top - foot - TownsfolkWalker.SeatTop:+0.00;-0.00} m off the {TownsfolkWalker.SeatTop:0.00} m seat"
                : $"{count} places";
            report?.AppendLine($"  {seat.name}: seat top {top - foot:0.00} m over the floor{(Mathf.Abs(sunk) > 0.02f ? $" (its foot {sunk:0.00} m under it)" : string.Empty)}, " +
                               $"{length:0.00} m long, {depths.Max() - depths.Min():0.00} m deep, {(backArea > 0.02f ? $"a back to {backTop - top:0.00} m over the seat" : "no back")}{arms}, {verdict}");
            return fits ? places : new ActivitySpot.Place[0];
        }

        /// <summary>Whether a straight walk from <paramref name="from"/> to <paramref name="to"/> is clear (the class summary's checks).</summary>
        public static bool Clear(Vector3 from, Vector3 to, Transform piece = null) => Blocker(from, to, piece) == null;

        /// <summary>What stops a straight walk, or null when it is clear; the bench, stall or door walked to may stand beside it.</summary>
        public static string Blocker(Vector3 from, Vector3 to, Transform piece = null)
        {
            var d = Flat(to - from);
            var length = d.magnitude;
            var steps = Mathf.Max(1, Mathf.CeilToInt(length / Sample));
            var ground = SliceTownsfolk.GroundMask;
            var last = float.NaN;
            for (var i = 0; i <= steps; i++)
            {
                var t = (float)i / steps;
                if ((1f - t) * length < FreeEnd)
                {
                    break;
                }

                var p = from + (d * t);
                if (!Physics.Raycast(new Vector3(p.x, from.y + (to.y - from.y) * t + 1.5f, p.z), Vector3.down, out var hit, 4f, ground, QueryTriggerInteraction.Ignore))
                {
                    return "no ground";
                }

                if (!float.IsNaN(last) && Mathf.Abs(hit.point.y - last) > MaxStep)
                {
                    return "a step";
                }

                last = hit.point.y;
                var q = new Vector3(p.x, hit.point.y, p.z);
                var stop = Physics.OverlapCapsule(q + (Vector3.up * 0.75f), q + (Vector3.up * 1.45f), 0.25f, Blocking, QueryTriggerInteraction.Ignore)
                    .Where(c => piece == null || !c.transform.IsChildOf(piece)).ToArray();
                if (stop.Length > 0)
                {
                    var top = PrefabUtility.GetOutermostPrefabInstanceRoot(stop[0].gameObject);
                    return $"{(top != null ? top.name : stop[0].name)} ({LayerMask.LayerToName(stop[0].gameObject.layer)})";
                }
            }

            return null;
        }

        /// <summary>
        /// The top of a model's seat over its own foot, m: the height, in 2 cm steps, that holds the most up-facing area
        /// between 0.25 m and <paramref name="highest"/> over the foot, as <see cref="Seats"/> reads it; null with none.
        /// The kit sizes its seats by it (<c>SliceKitCatalogue</c>, <c>Spec.SeatTop</c>).
        /// </summary>
        public static float? SeatTopOf(GameObject model, float highest = 1f)
        {
            var tris = Triangles(model);
            return tris.Count == 0 ? null : MostAreaUp(tris, tris.Min(t => Mathf.Min(t.a.y, Mathf.Min(t.b.y, t.c.y))), highest);
        }

        private static float? MostAreaUp(List<(Vector3 a, Vector3 b, Vector3 c)> tris, float foot, float highest)
        {
            var bins = new Dictionary<int, float>();
            foreach (var (a, b, c) in tris)
            {
                var n = Vector3.Cross(b - a, c - a);
                var area = n.magnitude * 0.5f;
                var h = ((a.y + b.y + c.y) / 3f) - foot;
                if (area < 1e-6f || n.normalized.y < 0.9f || h < 0.25f || h > highest)
                {
                    continue;
                }

                var bin = Mathf.RoundToInt(h / 0.02f);
                bins[bin] = (bins.TryGetValue(bin, out var sum) ? sum : 0f) + area;
            }

            return bins.Count == 0 ? null : bins.OrderByDescending(p => p.Value).First().Key * 0.02f;
        }

        private static List<(Vector3 a, Vector3 b, Vector3 c)> Triangles(GameObject g)
        {
            var tris = new List<(Vector3, Vector3, Vector3)>();
            var lod = g.GetComponentInChildren<LODGroup>();
            var renderers = lod != null && lod.GetLODs().Length > 0 ? lod.GetLODs()[0].renderers.OfType<MeshRenderer>().ToArray() : g.GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers)
            {
                var mf = r != null ? r.GetComponent<MeshFilter>() : null;
                if (mf == null || mf.sharedMesh == null || !r.enabled)
                {
                    continue;
                }

                var mesh = mf.sharedMesh;
                var v = mesh.vertices;
                var t = mesh.triangles;
                var w = mf.transform.localToWorldMatrix;
                for (var i = 0; i + 2 < t.Length; i += 3)
                {
                    tris.Add((w.MultiplyPoint3x4(v[t[i]]), w.MultiplyPoint3x4(v[t[i + 1]]), w.MultiplyPoint3x4(v[t[i + 2]])));
                }
            }

            return tris;
        }

        private static IEnumerable<GameObject> Instances(string prefabPath) =>
            Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Select(t => t.gameObject)
                .Where(g => PrefabUtility.IsOutermostPrefabInstanceRoot(g) && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g) == prefabPath);

        private static Bounds Bounds(GameObject g)
        {
            var renderers = g.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
            var b = renderers.Length > 0 ? renderers[0].bounds : new Bounds(g.transform.position, Vector3.zero);
            foreach (var r in renderers)
            {
                b.Encapsulate(r.bounds);
            }

            return b;
        }

        private static Vector3 Centre(GameObject g) => Bounds(g).center;

        private static Vector3 Grounded(Vector3 p) =>
            Physics.Raycast(p + (Vector3.up * 1.5f), Vector3.down, out var hit, 4f, SliceTownsfolk.GroundMask, QueryTriggerInteraction.Ignore)
                ? new Vector3(p.x, hit.point.y, p.z)
                : p;

        private static Vector3 Snap(Vector3 v) => Mathf.Abs(v.x) > Mathf.Abs(v.z) ? new Vector3(Mathf.Sign(v.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(v.z));

        private static float Yaw(Vector3 forward) => Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
