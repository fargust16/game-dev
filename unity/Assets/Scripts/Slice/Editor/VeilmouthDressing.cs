using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameDev.Slice.Kit;
using GameDev.World.Interiors;
using GameDev.World.Portals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The agent's first dressing of Veilmouth from the slice's agreed packs (SLICE_ASSET_MAP), at the owner's request of
    /// 2026-11-05, on the C0 map of VERTICAL_SLICE_IMPLEMENTATION_PLAN §2. It is one pass, not a generator: everything it
    /// places goes under one root, <c>Dressing</c>, and from then on the owner moves, removes, and adds by hand.
    /// <para>
    /// It replaces S0.6a's stand-ins in Veilmouth with real blocks; lays streets and squares; puts up the districts'
    /// houses, the seven buildings that open, each with its door, the market's stalls and the Item Creation yard, the
    /// Notice Board, the old chapel and its graveyard, the bridge, the farms and the flax field; fills the forest with
    /// cover and fellable trees and herb patches, the hill with iron ore; marks the beasts' territories and stands their
    /// stand-ins in them; stands the townsfolk at their posts in the colours of the asset map; draws the magic circle at
    /// each Ritual Site Candidate, hidden until a ritual starts there; dresses the Soot &amp; Bell; and writes the six other
    /// interiors. It never shapes or paints the ground (kit standard §7), so the Ridge, the river bed, and the sea are
    /// still the owner's to dig and raise, and the blocks on them are then moved with the ground.
    /// </para>
    /// <para>
    /// It refuses wherever its root or an interior already exists. <see cref="DressReplacing"/> replaces them and is for
    /// the agent's own iterations in batch runs only. Layout randomness (where a tree stands) is an authoring choice with a
    /// fixed System.Random, never the world seed.
    /// </para>
    /// </summary>
    public static class VeilmouthDressing
    {
        public const string RootName = "Dressing";
        public const string InteriorFolder = "Assets/Scenes/Slice/Interiors";

        public const string HarrowScenePath = InteriorFolder + "/HarrowApothecary.unity";
        public const string FactionHouseScenePath = InteriorFolder + "/FactionHouse.unity";
        public const string LockupScenePath = InteriorFolder + "/WardensLockup.unity";
        public const string MissingRoomScenePath = InteriorFolder + "/MissingRoom.unity";
        public const string CellarScenePath = InteriorFolder + "/SootStairCellar.unity";
        public const string ChapelScenePath = InteriorFolder + "/OldChapel.unity";

        /// <summary>The six interiors this pass writes, after the Soot &amp; Bell of S0.6a: building id and scene.</summary>
        public static readonly (string Id, string Path)[] Interiors =
        {
            ("harrow-apothecary", HarrowScenePath),
            ("faction-house", FactionHouseScenePath),
            ("wardens-lockup", LockupScenePath),
            ("missing-room", MissingRoomScenePath),
            ("soot-stair-cellar", CellarScenePath),
            ("old-chapel", ChapelScenePath),
        };

        public static string[] InteriorScenePaths => Interiors.Select(i => i.Path).ToArray();

        private const float T = VeilmouthScene.TerrainLevel;
        private const float S = VeilmouthScene.StreetLevel;
        internal const string Dummies = "Assets/Kevin Iglesias/Human Character Dummy/Prefabs";
        private const string AnimatorPath = "Assets/Settings/OverworldPlayerAnimator.controller";
        internal const string WolfPath = "Assets/FourEvilDragonsHP/Prefab/DragonSoulEater/Grey.prefab";
        internal const string BoarPath = "Assets/FourEvilDragonsHP/Prefab/DragonNightmare/Albino.prefab";

        private static Dictionary<string, SliceKitCatalogue.Entry> _kit;
        private static Transform _parent;
        private static Transform _people;

        /// <summary>For another pass that stands people and circles with this one's helpers (VeilmouthInteriors).</summary>
        internal static void Use(Dictionary<string, SliceKitCatalogue.Entry> kit, Transform parent)
        {
            _kit = kit;
            _parent = parent;
            _people = parent;
        }

        [MenuItem("GameDev/Slice/Dress Veilmouth (one pass)")]
        public static void Dress() => Run(false);

        /// <summary>Batch only, for the agent's iterations: replaces this pass's own root and interiors.</summary>
        public static void DressReplacing() => Run(true);

        public static void DressReplacingAndRender()
        {
            Run(true);
            VeilmouthRenders.RenderAll();
        }

        private static void Run(bool replace)
        {
            _kit = SliceKitCatalogue.BuildAll();
            foreach (var (id, path) in Interiors)
            {
                BuildInterior(id, path, replace);
            }

            DressSootAndBell(replace);
            DressStreet(replace);
        }

        // ----------------------------------------------------------------------------------------------- placing

        /// <summary>
        /// A block whose rotated footprint's south-west corner is (x, z), turned by <paramref name="turns"/> × 90° about
        /// Y; a linear block and a door leaf start at the node (x, z) instead.
        /// </summary>
        private static GameObject Put(string key, float x, float z, int turns = 0, float y = T)
        {
            var entry = _kit[key];
            var rotation = Quaternion.Euler(0f, 90f * turns, 0f);
            var pivot = new Vector3(x, y, z);
            if (entry.Depth > 0)
            {
                var corners = new[]
                {
                    Vector3.zero, new Vector3(entry.Width, 0f, 0f), new Vector3(0f, 0f, entry.Depth),
                    new Vector3(entry.Width, 0f, entry.Depth),
                }.Select(c => rotation * c).ToArray();
                pivot -= new Vector3(corners.Min(c => c.x), 0f, corners.Min(c => c.z));
            }

            return At(key, pivot, rotation);
        }

        private static GameObject At(string key, Vector3 pivot, Quaternion rotation)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(_kit[key].Prefab, _parent);
            go.transform.SetPositionAndRotation(Snap(pivot), rotation);
            return go;
        }

        private static Vector3 Snap(Vector3 p) =>
            new Vector3(Mathf.Round(p.x), Mathf.Round(p.y * 10f) / 10f, Mathf.Round(p.z));

        /// <summary>4 × 4 m paving over [x0, x1) × [z0, z1), its top at street level.</summary>
        private static void Pave(int x0, int z0, int x1, int z1, string kind = "Street")
        {
            for (var x = x0; x < x1; x += 4)
            {
                for (var z = z0; z < z1; z += 4)
                {
                    Put("Paving_" + kind, x, z, 0, S);
                }
            }
        }

        /// <summary>A house; with a door, its leaf stands in the middle of the front, 0.1 m out, at street level.</summary>
        private static GameObject House(string key, int x, int z, int turns, string doorId = null, string targetScenePath = null,
            string targetDoorId = null)
        {
            var house = Put(key, x, z, turns);
            if (doorId != null)
            {
                var entry = _kit[key];
                var leaf = house.transform.position + house.transform.rotation * new Vector3(Mathf.Round(entry.DoorX - 1.5f), 0f, entry.Depth);
                leaf.y = S;
                Door(leaf, house.transform.rotation, doorId, targetScenePath, targetDoorId);
            }

            return house;
        }

        /// <summary>The row of houses on the north side of a street whose north edge is <paramref name="z"/>, fronts south.</summary>
        private static void North(int z, params (int X, string Key)[] row)
        {
            foreach (var (x, key) in row)
            {
                House(key, x, z, 2);
            }
        }

        /// <summary>The row on the south side of a street whose south edge is <paramref name="z"/>, fronts north.</summary>
        private static void South(int z, params (int X, string Key)[] row)
        {
            foreach (var (x, key) in row)
            {
                House(key, x, z - _kit[key].Depth, 0);
            }
        }

        private static void Door(Vector3 pivot, Quaternion rotation, string id, string targetScenePath, string targetId)
        {
            var portal = At("PortalLeaf", pivot, rotation).GetComponent<Portal>();
            portal.Configure(id, SliceTestScenes.SceneName(targetScenePath), targetId, true);
            PrefabUtility.RecordPrefabInstancePropertyModifications(portal);
        }

        private static void Group(Transform root, string name)
        {
            _parent = new GameObject(name).transform;
            _parent.SetParent(root, false);
        }

        /// <summary>
        /// A person in the asset map's colour for their role (§1), standing idle. Not a kit block: the Character layer.
        /// Teal and brown have no Dummy prefab, so a white Dummy wears a project material.
        /// </summary>
        internal static void Person(string colour, bool female, float x, float z, float yaw, string name, float y = S)
        {
            var custom = colour == "Teal" || colour == "Brown";
            var path = $"{Dummies}/HumanDummy_{(female ? "F" : "M")} {(custom ? "White" : colour)}.prefab";
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), _people);
            go.name = name;
            go.transform.SetPositionAndRotation(new Vector3(x, y, z), Quaternion.Euler(0f, yaw, 0f));
            Stand(go, 1.8f);
            SliceCharacterLook.Fix(go);

            if (custom)
            {
                var material = colour == "Teal"
                    ? SliceKitCatalogue.Colour("Dummy_Teal", new Color(0.1f, 0.58f, 0.58f), false)
                    : SliceKitCatalogue.Colour("Dummy_Brown", new Color(0.45f, 0.29f, 0.16f), false);
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                {
                    r.sharedMaterials = r.sharedMaterials.Select(_ => material).ToArray();
                }
            }
        }

        /// <summary>A beast's stand-in (asset map §6): a dragon scaled to a wolf's or a boar's length, feet on the ground.</summary>
        private static void Beast(string path, float length, float x, float z, float yaw, string name)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), _people);
            go.name = name;
            go.transform.SetPositionAndRotation(new Vector3(x, T, z), Quaternion.Euler(0f, yaw, 0f));
            var size = Seen(go).size;
            go.transform.localScale = Vector3.one * (length / Mathf.Max(size.x, size.z));
            Stand(go, 0f);
            SliceCharacterLook.Fix(go);
        }

        /// <summary>Scales a character to <paramref name="height"/> (0 keeps it), sets its feet on its y, and gives it a capsule.</summary>
        internal static void Stand(GameObject go, float height)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(c);
            }

            if (height > 0f)
            {
                go.transform.localScale = Vector3.one * (height / Mathf.Max(0.1f, Seen(go).size.y));
            }

            var seen = Seen(go);
            go.transform.position += Vector3.up * (go.transform.position.y - seen.min.y);
            seen = Seen(go);
            var scale = go.transform.lossyScale.y;
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.height = seen.size.y / scale;
            capsule.radius = Mathf.Min(seen.size.x, seen.size.z) * 0.35f / scale;
            capsule.center = go.transform.InverseTransformPoint(seen.center);
            SliceKitCatalogue.SetLayer(go, KitLayers.Character);
        }

        internal static Bounds Seen(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>().Where(r => !(r is ParticleSystemRenderer)).ToArray();
            var b = renderers[0].bounds;
            foreach (var r in renderers.Skip(1))
            {
                b.Encapsulate(r.bounds);
            }

            return b;
        }

        private static void Territory(Transform root, string name, float x, float z)
        {
            var go = new GameObject(name);
            go.layer = LayerMask.NameToLayer(KitLayers.NoGrid);
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(x, T, z);
            go.AddComponent<LayoutZone>().Configure(new Vector3(30f, 6f, 30f), new Color(0.6f, 0.35f, 0.15f, 0.9f));
        }

        /// <summary>The ritual's magic circle at a candidate, hidden: the ritual system shows the one the seed picks.</summary>
        internal static void Circle(Vector3 centre, string candidate)
        {
            var circle = At("RitualCircle", centre, Quaternion.identity);
            circle.name = $"Ritual Circle - {candidate}";
            circle.SetActive(false);
        }

        // ----------------------------------------------------------------------------------------------- the street

        private static void DressStreet(bool replace)
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var existing = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
            if (existing != null)
            {
                if (!replace)
                {
                    Debug.LogWarning($"{VeilmouthScene.ScenePath} is already dressed; the pass does not run twice.");
                    return;
                }

                Object.DestroyImmediate(existing);
            }

            // S0.6a's stand-ins are replaced in place by the real blocks.
            var standIns = scene.GetRootGameObjects().Where(g =>
                (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g) ?? "").StartsWith(KitStandIns.Folder)).ToArray();
            foreach (var g in standIns)
            {
                Object.DestroyImmediate(g);
            }

            var root = new GameObject(RootName).transform;
            _people = new GameObject("People").transform;
            _people.SetParent(root, false);

            Wind(root);
            Group(root, "Streets");
            Streets();
            Group(root, "Lower Veil");
            LowerVeil();
            Group(root, "Market");
            Market();
            Group(root, "Ridge");
            Ridge();
            Group(root, "Old Chapel");
            Chapel();
            Group(root, "Fields");
            Fields();
            Group(root, "Forest");
            Forest(root);
            Group(root, "Hill");
            Hill();
            Townsfolk();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Report(VeilmouthScene.ScenePath, standIns.Length);
        }

        public const string WindNoisePath = "Assets/Stylized Trees Pack/Shared/Textures/Noise/Wind Noise 001.png";

        /// <summary>
        /// The Stylized Trees Pack's shaders bend every vertex by a global wind that its WindController sets each
        /// frame; with none in the scene the wind is zero, its direction undefined, and every tree and bush vanishes
        /// (found 2026-11-05). One wind zone for the scene, at the pack's defaults.
        /// </summary>
        internal static void Wind(Transform root)
        {
            var wind = new GameObject("Wind");
            wind.transform.SetParent(root, false);
            wind.transform.rotation = Quaternion.Euler(0f, 60f, 0f);
            wind.AddComponent<WindZone>().mode = WindZoneMode.Directional;
            var type = Type.GetType("Growth.WindController, Assembly-CSharp");
            if (type == null)
            {
                Debug.LogError("Growth.WindController not found: the trees will not show.");
                return;
            }

            var controller = new SerializedObject(wind.AddComponent(type));
            controller.FindProperty("noise").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(WindNoisePath);
            controller.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Streets()
        {
            Pave(4, -192, 140, -184);                 // Bell Street, Lower Veil's main street
            Pave(-36, -224, 4, -180, "Square");       // the Market square
            Pave(4, -184, 12, -100);                  // the road up to the Ridge
            Pave(-80, -100, 140, -92);                // Ridge Street, and its turn west toward the chapel
            Pave(-80, -92, -72, -68);                 // the chapel road
            Pave(-84, -68, -68, -60, "Square");       // the chapel's forecourt, Ritual Site Candidate 3
            Pave(40, -152, 140, -148);                // Loom Lane
            Pave(88, -184, 92, -152);                 // the lane up to Loom Lane
            Pave(88, -148, 92, -136);                 // the foot of the Soot Stair and the cellar door
            Pave(116, -224, 124, -192);               // the lane down to the square
            Pave(108, -248, 132, -224, "Square");     // the square of Ritual Site Candidate 1
            Pave(116, -264, 124, -248);
            Pave(40, -272, 140, -264);                // the sea front (its quay is a missing model)
            Pave(136, -204, 140, -192);               // the bridge road
            Pave(140, -204, 156, -196);
            Pave(184, -204, 200, -196);               // across the river

            for (var x = 10; x < 140; x += 12)
            {
                if (x < 116 || x > 124)
                {
                    Put("LampPost", x, -192, 0, S);
                }
            }

            for (var x = -36; x < 140; x += 12)
            {
                Put("LampPost", x, -100, 0, S);
            }

            Put("LampPost", 136, -203, 0, S);
            Put("LampPost", 196, -203, 0, S);
            Put("Bridge", 158, -205, 1, T);
        }

        private static void LowerVeil()
        {
            var bell = VeilmouthScene.SootAndBellScenePath;
            const string street = VeilmouthScene.SootAndBellStreetPortalId, inside = VeilmouthScene.SootAndBellInsidePortalId;

            // Bell Street, north side, fronts south; the Soot & Bell where the S0.6a shell stood, Harrow & Tincture by it.
            North(-184, (14, "House_Wide"), (22, "House_Narrow"), (28, "House_Blue"), (40, "House_Green"), (48, "House_Narrow"),
                (54, "House_Wide"), (62, "House_Red"), (70, "House_Tall"), (76, "House_Arch"), (93, "House_Narrow"),
                (119, "House_Blue"), (127, "House_Tall"), (133, "House_Narrow"));
            House("House_Square", 99, -184, 2, street, bell, inside).name = "The Soot & Bell";
            House("House_Wide", 111, -184, 2, "harrow-apothecary.street", HarrowScenePath, "harrow-apothecary.inside").name =
                "Harrow & Tincture";
            Put("NoticeBoard", 108, -184, 2).name = "Notice Board";

            // Bell Street, south side, fronts north.
            South(-192, (14, "House_Green"), (22, "House_Tall"), (28, "House_Wide"), (40, "House_Red"), (48, "House_Arch"),
                (58, "House_Narrow"), (64, "House_Square"), (73, "House_Blue"), (81, "House_Tall"), (87, "House_Wide"),
                (95, "House_Narrow"), (101, "House_Green"), (109, "House_Narrow"), (125, "House_Wide"));

            // Loom Lane: tenements; the missing person's room opens, and the pawnbroker's counter stands in a gap.
            North(-148, (40, "House_Narrow"), (46, "House_Wide"), (54, "House_Tall"), (60, "House_Red"), (68, "House_Narrow"),
                (74, "House_Blue"), (82, "House_Narrow"), (93, "House_Wide"), (101, "House_Green"), (109, "House_Tall"),
                (115, "House_Wide"), (123, "House_Red"), (131, "House_Narrow"));
            South(-152, (40, "House_Blue"), (48, "House_Wide"), (56, "House_Narrow"), (71, "House_Tall"), (77, "House_Red"),
                (93, "House_Narrow"), (99, "House_Wide"), (111, "House_Blue"), (119, "House_Narrow"), (125, "House_Wide"),
                (133, "House_Tall"));
            House("House_Square", 62, -152 - _kit["House_Square"].Depth, 0, "missing-room.street", MissingRoomScenePath,
                "missing-room.inside").name = "The missing person's tenement";
            Put("Counter", 107, -153, 0, T).name = "Pawn shop counter";
            Put("Chest", 109, -155, 0, T).name = "Pawn shop chest";

            // The cellar under the Soot Stair: a stone wall at the stair's foot with its door.
            Put("RoomWall", 88, -135, 0, S);
            Door(new Vector3(92f, S, -135f), Quaternion.Euler(0f, 180f, 0f), "soot-stair-cellar.street", CellarScenePath,
                "soot-stair-cellar.inside");
            Put("Barrel", 86, -138, 0, T);
            Put("Box", 93, -137, 0, T);

            // Ritual Site Candidate 1, the square in Lower Veil.
            Put("Bench", 110, -226, 2, S);
            Put("Bench", 128, -226, 2, S);
            Put("LampPost", 108, -248, 0, S);
            Put("LampPost", 131, -248, 0, S);
            Circle(new Vector3(120f, S, -236f), "Candidate 1, the Lower Veil square");

            // The sea front: cargo where a quay will be.
            foreach (var x in new[] { 46, 66, 86, 106, 128 })
            {
                Put("Barrels", x, -271, 0, S);
                Put("Box", x + 4, -271, 0, S);
                Put("Crate", x + 5, -271, 0, S);
            }

            Put("Barrel", 108, -176, 0, T);
            Put("Firewood", 109, -176, 0, T);
            Put("TrashCan", 98, -183, 0, T);
        }

        private static void Market()
        {
            // North row of stalls, fronts south: the trader, the Masters of Enchanting and First Aid, the seer.
            Put("Stall_Goods", -32, -188, 2, S).name = "The trader's stall";
            Put("Stall_Empty", -24, -188, 2, S).name = "Master of Enchanting";
            Put("TablePlain", -17, -186, 0, S);
            Put("Stall_Empty", -13, -188, 2, S).name = "Master of First Aid";
            Put("BottleTable", -6, -185, 2, S);
            Put("Counter", -2, -185, 2, S).name = "The seer's counter";
            Put("Candles", 1, -185, 0, S);

            // South row, fronts north: food.
            Put("Stall_Food", -20, -224, 0, S);
            Put("Stall_Fish", -12, -224, 0, S);
            Put("Stall_Meat", -3, -224, 0, S);

            // The Master of Item Creation's counter and yard on the west side.
            Put("Stall_Weapons", -36, -210, 1, S).name = "Master of Item Creation";
            for (var x = -36; x < -26; x += 2)
            {
                Put("Fence", x, -212, 0, S);
            }

            for (var z = -222; z < -212; z += 2)
            {
                Put("Fence", -26, z, 3, S);
            }

            Put("Anvil", -33, -216, 0, S);
            Put("LeatherFrame", -30, -219, 0, S);
            Put("ArmourStand", -35, -219, 0, S);
            Put("Firewood", -28, -215, 0, S);
            Put("Barrel", -35, -214, 0, S);

            foreach (var (x, z) in new[] { (-20, -204), (-8, -204), (-20, -198), (-8, -198) })
            {
                Put("Bench", x, z, 0, S);
            }

            foreach (var (x, z) in new[] { (-36, -181), (3, -181), (-36, -224), (-26, -224) })
            {
                Put("LampPost", x, z, 0, S);
            }

            // Houses round the square.
            North(-180, (-36, "House_Wide"), (-28, "House_Green"), (-20, "House_Arch"), (-10, "House_Narrow"), (-4, "House_Narrow"));
            South(-224, (-36, "House_Blue"), (-28, "House_Tall"), (-22, "House_Wide"));
        }

        private static void Ridge()
        {
            North(-92, (-36, "House_European"), (-28, "House_TallWide"), (-20, "House_European"), (-12, "House_Green"),
                (-4, "House_TallWide"), (4, "House_Blue"), (12, "House_European"), (28, "House_Arch"), (53, "House_European"),
                (61, "House_TallWide"), (69, "House_Wide"), (77, "House_European"), (85, "House_Arch"), (95, "House_Red"),
                (103, "House_European"), (111, "House_TallWide"), (119, "House_Blue"), (127, "House_Tall"));
            House("House_European", 20, -92, 2, "faction-house.street", FactionHouseScenePath, "faction-house.inside").name =
                "The faction's house";
            House("House_Square", 44, -92, 2, "wardens-lockup.street", LockupScenePath, "wardens-lockup.inside").name =
                "The Wardens' station";
            South(-100, (-36, "House_TallWide"), (-28, "House_Wide"), (-20, "House_European"), (-12, "House_Narrow"),
                (-6, "House_Narrow"), (13, "House_European"), (21, "House_Wide"), (29, "House_Tall"), (35, "House_Green"),
                (43, "House_European"), (51, "House_Arch"), (61, "House_Wide"), (69, "House_European"), (77, "House_Narrow"),
                (95, "House_European"), (103, "House_TallWide"), (111, "House_Wide"), (119, "House_European"), (127, "House_Narrow"));
            Put("Bench", 38, -91, 2, T);
        }

        private static void Chapel()
        {
            House("Chapel", -82, -60, 2, "old-chapel.street", ChapelScenePath, "old-chapel.inside").name = "The old chapel";
            for (var x = -66; x <= -58; x += 2)
            {
                for (var z = -56; z <= -47; z += 3)
                {
                    Put("Grave", x, z, 2);
                }
            }

            for (var x = -67; x < -55; x += 4)
            {
                Put("GraveWall", x, -58);
                Put("GraveWall", x, -43);
            }

            for (var z = -58; z < -46; z += 4)
            {
                Put("GraveWall", -55, z, 3);
            }

            Put("LampPost", -84, -61, 0, S);
            Put("LampPost", -69, -61, 0, S);
            Circle(new Vector3(-76f, S, -64f), "Candidate 3, before the old chapel");
        }

        private static void Fields()
        {
            Put("House_Farm", -160, -150, 0);
            Put("House_Farm", -222, -120, 1);
            Put("House_Farm", -262, -205, 0);
            Put("Barrel", -152, -150);
            Put("Firewood", -153, -149);

            // The flax field, fenced, with a gap in the south side.
            for (var x = -200; x < -184; x += 2)
            {
                if (x != -192)
                {
                    Put("Fence", x, -190);
                }

                Put("Fence", x, -174);
            }

            for (var z = -190; z < -174; z += 2)
            {
                Put("Fence", -200, z, 3);
                Put("Fence", -184, z, 3);
            }

            for (var i = 0; i < 5; i++)
            {
                for (var j = 0; j < 2; j++)
                {
                    Put("Flax", -197 + 3 * i, -185 + 6 * j);
                }
            }
        }

        private static readonly string[] TreeKinds = { "Birch1", "Birch2", "Birch3", "Warped1", "Warped2", "Warped3" };

        private static void Forest(Transform root)
        {
            var rng = new Random(20261105); // authoring layout only, fixed; never the world seed
            var used = new List<Vector2>();
            bool Free(Vector2 p, float spacing) => used.All(u => (u - p).sqrMagnitude >= spacing * spacing);

            // Glades: about 40 fellable trees round them and 30 herb patches in them (plan §2).
            var glades = new[]
            {
                new Vector2(-200, 80), new Vector2(-120, 40), new Vector2(-40, 150), new Vector2(40, 90),
                new Vector2(-280, 170), new Vector2(-180, 260), new Vector2(80, 250), new Vector2(-60, 40),
            };
            var herbs = 0;
            foreach (var g in glades)
            {
                for (var placed = 0; placed < 5;)
                {
                    var a = rng.NextDouble() * Math.PI * 2;
                    var r = 9 + rng.NextDouble() * 6;
                    var p = new Vector2(Mathf.Round(g.x + (float)(Math.Cos(a) * r)), Mathf.Round(g.y + (float)(Math.Sin(a) * r)));
                    if (!Free(p, 4f))
                    {
                        continue;
                    }

                    used.Add(p);
                    Put("Fellable_" + TreeKinds[rng.Next(TreeKinds.Length)], p.x, p.y, rng.Next(4));
                    placed++;
                }

                for (var placed = 0; placed < 4 && herbs < 30;)
                {
                    var p = new Vector2(Mathf.Round(g.x + rng.Next(-6, 7)), Mathf.Round(g.y + rng.Next(-6, 7)));
                    if (!Free(p, 2.5f))
                    {
                        continue;
                    }

                    used.Add(p);
                    Put("Herb", p.x, p.y, rng.Next(4));
                    placed++;
                    herbs++;
                }
            }

            // Cover between the glades; none in the glades, by the chapel, or at the forest's city edge.
            for (var attempt = 0; attempt < 4000 && used.Count < 270; attempt++)
            {
                var p = new Vector2(rng.Next(-326, 146), rng.Next(-36, 326));
                if (glades.Any(g => (g - p).sqrMagnitude < 18 * 18) || (p.x > -100 && p.x < -45 && p.y < -20) || !Free(p, 7f))
                {
                    continue;
                }

                used.Add(p);
                if (rng.NextDouble() < 0.7)
                {
                    Put("Tree_" + TreeKinds[rng.Next(TreeKinds.Length)], p.x, p.y, rng.Next(4));
                }
                else
                {
                    Put(new[] { "Bush_006", "Bush_011", "Bush_021" }[rng.Next(3)], p.x, p.y, rng.Next(4));
                }
            }

            // Beast territories, about 30 m across and at least 60 m apart (plan §2), with the stand-ins in them.
            var territories = new GameObject("Beast Territories").transform;
            territories.SetParent(root, false);
            var wolves = new[] { (-250, 250), (-150, 200), (-40, 280), (60, 200), (-260, 60), (100, 60) };
            for (var i = 0; i < wolves.Length; i++)
            {
                var (x, z) = wolves[i];
                Territory(territories, $"Wolf Territory {i + 1}", x, z);
                foreach (var (dx, dz, yaw) in new[] { (0f, 0f, 30f), (2.5f, 1.5f, 80f), (-1.5f, 2.5f, 200f) })
                {
                    Beast(WolfPath, 1.6f, x + dx, z + dz, yaw, $"Wolf {i + 1} (dragon stand-in)");
                }
            }

            var boars = new[] { (-200, 0), (-100, 100), (0, 120), (20, 0) };
            for (var i = 0; i < boars.Length; i++)
            {
                var (x, z) = boars[i];
                Territory(territories, $"Boar Territory {i + 1}", x, z);
                Beast(BoarPath, 1.5f, x, z, 45f * i, $"Boar {i + 1} (dragon stand-in)");
            }
        }

        private static void Hill()
        {
            var rng = new Random(5);
            var used = new List<Vector2>();
            while (used.Count < 12)
            {
                var p = new Vector2(rng.Next(210, 318), rng.Next(52, 298));
                if (used.All(u => (u - p).sqrMagnitude >= 18 * 18))
                {
                    used.Add(p);
                    Put("IronOre", p.x, p.y, rng.Next(4)).name = $"Iron ore vein {used.Count}";
                }
            }

            for (var i = 0; i < 14; i++)
            {
                var p = new Vector2(rng.Next(205, 325), rng.Next(45, 305));
                if (used.All(u => (u - p).sqrMagnitude >= 6 * 6))
                {
                    used.Add(p);
                    Put("Bush_011", p.x, p.y, rng.Next(4));
                }
            }
        }

        private static void Townsfolk()
        {
            // Wardens, blue.
            Person("Blue", false, 46f, -93.2f, 180f, "Warden at the station");
            Person("Blue", false, 50f, -93.2f, 180f, "Warden at the station");
            Person("Blue", false, 60f, -188f, 90f, "Warden on Bell Street");
            Person("Blue", true, 126f, -188f, 270f, "Warden on Bell Street");

            // The faction, teal; Veinmarket, brown.
            Person("Teal", false, 22f, -93.2f, 180f, "Faction member");
            Person("Teal", true, 26f, -93.2f, 180f, "Faction member");
            Person("Brown", false, 70f, -150f, 0f, "Veinmarket");
            Person("Brown", false, 72f, -150f, 270f, "Veinmarket");

            // Masters, green; sellers, orange.
            Person("Green", false, -30f, -205.5f, 90f, "Master of Item Creation");
            Person("Green", true, -21f, -186.5f, 180f, "Master of Enchanting");
            Person("Green", false, -10f, -186.5f, 180f, "Master of First Aid");
            Person("Orange", false, -29f, -186.5f, 180f, "The trader");
            Person("Orange", true, -1f, -183.5f, 180f, "The seer");
            Person("Orange", false, 108f, -154.5f, 0f, "The pawnbroker", T);

            // Townsfolk, yellow.
            foreach (var (x, z, yaw, female) in new[]
                     {
                         (-15f, -200f, 0f, false), (-11f, -195f, 200f, true), (80f, -189f, 90f, true), (95f, -187f, 270f, false),
                         (60f, -96f, 90f, true), (100f, -95f, 270f, false), (100f, -150f, 90f, true), (80f, -268f, 180f, false),
                         (60f, -268f, 0f, true), (120f, -232f, 30f, false),
                     })
            {
                Person("Yellow", female, x, z, yaw, "Townsperson");
            }

            Person("Yellow", false, -158f, -152f, 180f, "Farmer", T);
        }

        // ----------------------------------------------------------------------------------------------- interiors

        private static void DressSootAndBell(bool replace)
        {
            var path = VeilmouthScene.SootAndBellScenePath;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var existing = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
            if (existing != null)
            {
                if (!replace)
                {
                    Debug.LogWarning($"{path} is already dressed; the pass does not run twice.");
                    return;
                }

                Object.DestroyImmediate(existing);
            }

            // S0.6a's two crate tables go; its stand-in walls and door stay until the owner's room replaces them.
            var crates = scene.GetRootGameObjects()
                .Where(g => (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g) ?? "").EndsWith("Kit_Crate_StandIn_1x1.prefab"))
                .ToArray();
            foreach (var g in crates)
            {
                Object.DestroyImmediate(g);
            }

            var root = new GameObject(RootName).transform;
            _parent = root;
            _people = root;
            Put("BarCabinet", 4, 7, 2, 0f);
            Put("Shelf", 1, 7, 2, 0f);
            Put("BarCounter", 3, 4, 2, 0f);
            Put("Candles", 3, 7, 0, 0f);
            Put("Table", 0, 1, 0, 0f);
            Put("Chair", 0, 3, 0, 0f);
            Put("Table", 6, 1, 0, 0f);
            Put("Chair", 5, 1, 0, 0f);
            Put("Barrel", 6, 3, 0, 0f);
            Put("Bed", 0, 5, 2, 0f).name = "Bed (a room for the night)";
            Person("Yellow", false, 5.5f, 6.3f, 180f, "Tavern keeper", 0f);
            Person("Brown", false, 2.5f, 2.2f, 270f, "Veinmarket at a table", 0f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Report(path, crates.Length);
        }

        private static void BuildInterior(string id, string path, bool replace)
        {
            if (File.Exists(path) && !replace)
            {
                Debug.LogWarning($"{path} exists and is hand-built from here on; it is not overwritten.");
                return;
            }

            var (width, depth) = id switch
            {
                "faction-house" => (12, 8),
                "soot-stair-cellar" => (12, 12),
                "old-chapel" => (8, 12),
                _ => (8, 8),
            };

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var root = new GameObject(RootName).transform;
            _parent = root;
            _people = root;
            for (var x = 0; x < width; x += 4)
            {
                for (var z = 0; z < depth; z += 4)
                {
                    Put("RoomFloor", x, z, 0, 0f);
                }

                Put("RoomWall", x, 0, 0, 0f);
                Put("RoomWall", x, depth, 0, 0f);
            }

            for (var z = 0; z < depth; z += 4)
            {
                Put("RoomWall", 0, z, 3, 0f);
                Put("RoomWall", width, z, 3, 0f);
            }

            Door(new Vector3(2f, 0f, 0f), Quaternion.identity, id + ".inside", VeilmouthScene.ScenePath, id + ".street");
            Furnish(id);

            new GameObject("Interior").AddComponent<InteriorRuntime>().Configure(id);
            SliceTestScenes.AddPlayerRig(new Vector3(3.5f, 0.1f, 1.5f));
            SliceTestScenes.Save(scene, path);
            Report(path, 0);
        }

        private static void Furnish(string id)
        {
            const float floor = 0f;
            switch (id)
            {
                case "harrow-apothecary":
                    Put("CupboardBig", 1, 7, 2, floor);
                    Put("Shelf", 5, 7, 2, floor);
                    Put("Table", 3, 3, 0, floor);
                    Put("Drawer", 0, 3, 1, floor);
                    Put("Barrels", 5, 1, 0, floor);
                    Put("Candles", 0, 1, 0, floor);
                    Put("Candles", 7, 5, 0, floor);
                    Person("Green", false, 4f, 5.7f, 180f, "Harrow", floor);
                    break;
                case "faction-house":
                    Put("Rostrum", 5, 5, 2, floor);
                    Put("Pew", 0, 2, 0, floor);
                    Put("Pew", 0, 4, 0, floor);
                    Put("Pew", 7, 2, 0, floor);
                    Put("Pew", 7, 4, 0, floor);
                    Put("CupboardBig", 8, 7, 2, floor);
                    Put("Table", 10, 1, 0, floor);
                    Put("Candles", 0, 7, 0, floor);
                    Put("Candles", 11, 5, 0, floor);
                    Person("Teal", false, 3.5f, 6.5f, 180f, "Faction member", floor);
                    Person("Teal", true, 9.5f, 5.6f, 200f, "Faction member", floor);
                    break;
                case "wardens-lockup":
                    Put("CellBars", 0, 4, 0, floor).name = "Cell front";
                    Put("RoomWall", 4, 4, 3, floor).name = "Cell wall";
                    Put("Pew", 0, 6, 0, floor);
                    Put("TablePlain", 5, 1, 0, floor);
                    Put("Chair", 4, 2, 0, floor);
                    Put("Cupboard", 0, 2, 1, floor);
                    Put("Candles", 7, 0, 0, floor);
                    Person("Blue", false, 6f, 3.4f, 180f, "Warden", floor);
                    Person("Blue", true, 2.5f, 3f, 90f, "Warden", floor);
                    break;
                case "missing-room":
                    Put("Bed", 0, 5, 2, floor);
                    Put("Drawer", 3, 7, 2, floor);
                    Put("TablePlain", 5, 2, 0, floor);
                    Put("Chair", 4, 2, 0, floor);
                    Put("Chest", 2, 6, 0, floor).name = "The cache";
                    Put("Candles", 7, 7, 0, floor);
                    Put("Books", 6, 5, 0, floor);
                    break;
                case "soot-stair-cellar":
                    Put("Barrels", 0, 9, 0, floor);
                    Put("Barrels", 9, 9, 0, floor);
                    Put("Barrel", 10, 2, 0, floor);
                    Put("Barrel", 10, 3, 0, floor);
                    Put("Box", 1, 7, 0, floor);
                    Put("Firewood", 1, 1, 0, floor);
                    Put("Candles", 1, 5, 0, floor);
                    Put("Candles", 10, 6, 0, floor);
                    Circle(new Vector3(6f, floor, 6f), "Candidate 2, the cellar under the Soot Stair");
                    break;
                case "old-chapel":
                    Put("Rostrum", 3, 9, 2, floor);
                    foreach (var z in new[] { 3, 5, 7 })
                    {
                        Put("Pew", 0, z, 0, floor);
                        Put("Pew", 5, z, 0, floor);
                    }

                    Put("Candles", 0, 10, 0, floor);
                    Put("Candles", 7, 10, 0, floor);
                    break;
            }
        }

        private static void Report(string path, int removed)
        {
            var blocks = Object.FindObjectsByType<KitBlock>(FindObjectsInactive.Exclude);
            var faults = KitLayoutCheck.Check(blocks);
            Debug.Log($"Dressing {path}: {blocks.Length} blocks, {removed} stand-ins removed, {faults.Count} kit faults" +
                      (faults.Count > 0 ? ":\n  " + string.Join("\n  ", faults.Take(40).Select(f => f.ToString())) : "."));
        }
    }
}
