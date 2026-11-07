using System.Collections.Generic;
using System.Linq;
using GameDev.Slice.Dwellings;
using GameDev.Slice.Kit;
using GameDev.World.Interiors;
using GameDev.World.Portals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The slice's Dwellings (15, *The slice's Dwellings*; city plan C1 §8, §16): the kit asset the Dwelling scene builds
    /// from, the shared Dwelling scene itself, and a street door on every dwelling of Veilmouth. The Dwelling scene holds
    /// no authored content, so it is written again whenever the kit changes; the doors are added once to the owner's
    /// Veilmouth and are also part of the city's build.
    /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthDwellings.BuildAll
    /// </summary>
    public static class VeilmouthDwellings
    {
        public const string KitPath = "Assets/Data/Slice/DwellingKit.asset";
        public const string ScenePath = "Assets/Scenes/Slice/Interiors/Dwelling.unity";
        public const string DoorsGroup = "Dwelling doors";
        private const string SettingsPath = "Assets/Data/Slice/SliceSettings.asset";


        /// <summary>The dwelling the Dwelling scene builds when opened alone.</summary>
        public static DwellingKey PreviewKey(DwellingArchetype archetype = DwellingArchetype.Terrace, DwellingWealth wealth = DwellingWealth.Middling, int x = 0, int z = 0) =>
            new DwellingKey(DwellingKey.IdAt(x, z), SliceTestScenes.SceneName(VeilmouthScene.ScenePath), x, z, archetype, wealth);

        [MenuItem("GameDev/Slice/Dwellings/Build Kit and Scene")]
        public static void BuildAll()
        {
            // Every other wrapper as it is on disk (the owner's edits stay); the Victorian ones built afresh (VI-4).
            var catalogue = SliceKitCatalogue.Load();
            foreach (var kv in SliceKitCatalogue.BuildVictorian())
            {
                catalogue[kv.Key] = kv.Value;
            }

            BuildKit(catalogue);
            BuildScene();
        }

        /// <summary>The kit, the scene and pictures of sample dwellings, storey by storey (VI-4).</summary>
        public static void BuildAllAndRender()
        {
            BuildAll();
            VeilmouthRenders.RenderDwellings();
        }

        /// <summary>The kit, the scene, the doors on Veilmouth (once), and pictures of sample dwellings.</summary>
        public static void BuildAllWithDoorsAndRender()
        {
            BuildAll();
            AddDoorsToVeilmouth();
            VeilmouthRenders.RenderDwellings();
        }

        // ------------------------------------------------------------------------------------------- kit

        public static DwellingKit BuildKit(Dictionary<string, SliceKitCatalogue.Entry> catalogue)
        {
            var keys = DwellingAssembler.Keys.Concat(DwellingPlanner.Keys).Distinct().ToList();
            var blocks = new List<DwellingKit.Block>();
            foreach (var key in keys)
            {
                if (!catalogue.TryGetValue(key, out var entry) || entry.Prefab == null)
                {
                    Debug.LogError($"[VeilmouthDwellings] the catalogue has no '{key}'.");
                    continue;
                }

                var (solid, body) = Measure(entry.Prefab);
                blocks.Add(new DwellingKit.Block
                {
                    key = key, prefab = entry.Prefab, width = entry.Width, depth = entry.Depth, solid = solid, body = body, height = entry.Height,
                });
            }

            var kit = AssetDatabase.LoadAssetAtPath<DwellingKit>(KitPath);
            if (kit == null)
            {
                kit = ScriptableObject.CreateInstance<DwellingKit>();
                AssetDatabase.CreateAsset(kit, KitPath);
            }

            kit.SetBlocks(blocks);
            var glass = new List<DwellingKit.Glass>();
            foreach (var key in VeilmouthInteriors.Glass.Keys)
            {
                if (catalogue.TryGetValue(key, out var entry) && entry.Prefab != null)
                {
                    glass.Add(MeasureGlass(key, entry.Prefab));
                }
            }

            kit.SetLight(AssetDatabase.LoadAssetAtPath<GameObject>(VeilmouthInteriors.FirePath),
                SliceKitCatalogue.Colour("ViWindowPane", new Color(0.78f, 0.86f, 0.95f), true), glass);
            EditorUtility.SetDirty(kit);
            AssetDatabase.SaveAssets();
            Debug.Log($"[VeilmouthDwellings] kit written: {blocks.Count} blocks.");
            return kit;
        }

        /// <summary>
        /// A window wall's glass (VICTORIAN_INTERIORS_PLAN §5): its rectangle as measured for the seven, and its depth into
        /// the room found by a ray onto the wall's own meshes, as `VeilmouthInteriors.Daylight` finds it.
        /// </summary>
        private static DwellingKit.Glass MeasureGlass(string key, GameObject prefab)
        {
            var g = VeilmouthInteriors.Glass[key];
            var go = (GameObject)Object.Instantiate(prefab);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var probes = new List<MeshCollider>();
            foreach (var filter in go.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh != null && filter.TryGetComponent<MeshRenderer>(out var r) && r.enabled)
                {
                    var c = filter.gameObject.AddComponent<MeshCollider>();
                    c.sharedMesh = filter.sharedMesh;
                    probes.Add(c);
                }
            }

            var backfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;
            Physics.SyncTransforms();
            float? best = null;
            var at = new Vector3((SliceKitCatalogue.ViModule * 0.5f) + g.ProbeU, g.ProbeY, 0f);
            foreach (var c in probes)
            {
                if (c.Raycast(new Ray(at + Vector3.forward, Vector3.back), out var hit, 2f) && (best == null || hit.distance < best))
                {
                    best = hit.distance;
                }
            }

            Physics.queriesHitBackfaces = backfaces;
            Object.DestroyImmediate(go);
            var depth = best.HasValue ? 1f - best.Value : SliceKitCatalogue.ViFace;
            Debug.Log($"[VeilmouthDwellings] {key}: glass u {g.U0:0.00}..{g.U1:0.00}, y {g.Y0:0.00}..{g.Y1:0.00}, at {depth:0.00} m.");
            return new DwellingKit.Glass { key = key, u0 = g.U0, u1 = g.U1, y0 = g.Y0, y1 = g.Y1, depth = depth };
        }

        /// <summary>A block's solid body in its own metres: the union of its solid colliders, as the layout check takes it.</summary>
        private static (bool Solid, Rect Body) Measure(GameObject prefab)
        {
            var go = (GameObject)Object.Instantiate(prefab);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Physics.SyncTransforms();
            // A set's loose things and a shelf's ThingSurface are no part of its body (kit standard §4).
            var colliders = go.GetComponentsInChildren<Collider>()
                .Where(c => c.enabled && !c.isTrigger && c.GetComponentInParent<ThingSurface>() == null &&
                            (c.GetComponentInParent<LooseProp>() is not { IsThing: true }))
                .ToArray();
            var solid = go.layer != LayerMask.NameToLayer(KitLayers.NoGrid) && colliders.Length > 0;
            var body = new Rect();
            if (solid)
            {
                var b = colliders[0].bounds;
                foreach (var c in colliders.Skip(1))
                {
                    b.Encapsulate(c.bounds);
                }

                body = Rect.MinMaxRect(b.min.x, b.min.z, b.max.x, b.max.z);
            }

            Object.DestroyImmediate(go);
            return (solid, body);
        }

        // ------------------------------------------------------------------------------------------- scene

        public static void BuildScene()
        {
            var kit = AssetDatabase.LoadAssetAtPath<DwellingKit>(KitPath);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            // No sun indoors: the pack's windows are opaque, and daylight comes through their lit panes (plan §5).
            foreach (var sun in Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
            {
                Object.DestroyImmediate(sun.gameObject);
            }

            var interior = new GameObject("Interior").AddComponent<InteriorRuntime>();
            interior.Configure(PreviewKey().StreetPortalId);
            var runtime = new GameObject("Dwelling").AddComponent<DwellingRuntime>();
            runtime.Configure(kit, AssetDatabase.LoadAssetAtPath<SliceSettings>(SettingsPath), interior, PreviewKey().ToContext());
            SliceTestScenes.AddPlayerRig(new Vector3(1.5f, 0.1f, 1.6f));
            SliceTestScenes.Save(scene, ScenePath);
        }

        // ------------------------------------------------------------------------------------------- doors

        [MenuItem("GameDev/Slice/Dwellings/Add Dwelling Doors to Veilmouth (once)")]
        public static void AddDoorsToVeilmouth()
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == VeilmouthCity.RootName);
            if (root == null)
            {
                Debug.LogError($"[VeilmouthDwellings] {VeilmouthScene.ScenePath} has no {VeilmouthCity.RootName}.");
                return;
            }

            if (root.transform.Find(DoorsGroup) != null)
            {
                Debug.LogWarning($"[VeilmouthDwellings] Veilmouth already has its {DoorsGroup}; they are the owner's now and are not added again.");
                return;
            }

            var count = AddDoors(root.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[VeilmouthDwellings] {count} dwelling doors added to {VeilmouthScene.ScenePath}.");
        }

        /// <summary>
        /// A street door on every dwelling under <paramref name="root"/>: at each house's door marker, unless a building
        /// that opens already has its own portal there. Returns how many were added.
        /// </summary>
        public static int AddDoors(Transform root)
        {
            var group = new GameObject(DoorsGroup).transform;
            group.SetParent(root, false);
            var existing = root.GetComponentsInChildren<Portal>(true).Select(p => p.transform.TransformPoint(new Vector3(1.5f, 0f, 0f))).ToList();
            var streetScene = SliceTestScenes.SceneName(VeilmouthScene.ScenePath);
            var dwellingScene = SliceTestScenes.SceneName(ScenePath);
            var ids = new HashSet<string>();
            var count = 0;
            foreach (var marker in root.GetComponentsInChildren<Transform>(true).Where(t => t.name == VictorianHouses.DoorMarker).ToList())
            {
                var at = marker.position;
                if (existing.Any(p => Flat(p - at) < 2.5f) || !Classify(marker, out var archetype, out var wealth))
                {
                    continue;
                }

                var x = Mathf.RoundToInt(at.x * 10f);
                var z = Mathf.RoundToInt(at.z * 10f);
                var key = new DwellingKey(DwellingKey.IdAt(x, z), streetScene, x, z, archetype, wealth);
                if (!ids.Add(key.StreetPortalId))
                {
                    Debug.LogWarning($"[VeilmouthDwellings] two doors share {key.StreetPortalId}; the second is left closed.");
                    continue;
                }

                var door = new GameObject($"Dwelling door - {key.StreetPortalId}");
                door.transform.SetParent(group, false);
                door.transform.SetPositionAndRotation(at - (marker.right * 1.5f), marker.rotation);
                door.AddComponent<Portal>().Configure(key.StreetPortalId, dwellingScene, DwellingRuntime.ArrivalPortalId, true, key.ToContext());
                count++;
            }

            return count;
        }

        private static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;

        /// <summary>
        /// A dwelling's kind from its house's baked type, and its wealth from where it stands (19 §6.3): the Ridge is
        /// rich, the High Street, the terraces and the shops middling, Lower Veil, the docks and the tenements poor.
        /// Civic buildings, the tavern and the chapel are not dwellings.
        /// </summary>
        public static bool Classify(Transform marker, out DwellingArchetype archetype, out DwellingWealth wealth)
        {
            archetype = default;
            wealth = default;
            var house = marker.parent;
            var source = house != null ? PrefabUtility.GetCorrespondingObjectFromSource(house.gameObject) : null;
            var name = source != null ? source.name : house != null ? house.name : string.Empty;
            const string prefix = "Kit_House_";
            if (!name.StartsWith(prefix))
            {
                return false;
            }

            var parts = name.Substring(prefix.Length).Split('_');
            var type = parts[0];
            var facade = parts.Length > 1 ? parts[1] : string.Empty;
            if (type.StartsWith("Shop")) archetype = DwellingArchetype.Shop;
            else if (type.StartsWith("Terrace")) archetype = DwellingArchetype.Terrace;
            else if (type.StartsWith("Tenement")) archetype = DwellingArchetype.Tenement;
            else if (type.StartsWith("Villa")) archetype = DwellingArchetype.Villa;
            else if (type.StartsWith("Warehouse")) archetype = DwellingArchetype.Warehouse;
            else if (type.StartsWith("Arch")) archetype = facade == "stucco" ? DwellingArchetype.Terrace : DwellingArchetype.Tenement;
            else return false;

            wealth = WealthAt(marker.position, archetype);
            return true;
        }

        /// <summary>City plan C2 (§3, §21), in plan coordinates (px = X + 500, py = 500 − Z).</summary>
        public static DwellingWealth WealthAt(Vector3 world, DwellingArchetype archetype)
        {
            var px = world.x + 500f;
            var py = 500f - world.z;
            if (py < 680f)
            {
                return DwellingWealth.Rich; // the Ridge
            }

            if (archetype == DwellingArchetype.Tenement || archetype == DwellingArchetype.Warehouse)
            {
                return DwellingWealth.Poor;
            }

            var lowerVeil = (px >= 620f && py >= 744f) || (px >= 648f && py > 680f && py < 726f);
            return lowerVeil ? DwellingWealth.Poor : DwellingWealth.Middling;
        }

        // ------------------------------------------------------------------------------------------- checks

        /// <summary>Builds <paramref name="key"/>'s dwelling in the open scene and returns its kit faults.</summary>
        public static List<KitFault> BuildAndCheck(DwellingKey key, DwellingKit kit, SliceSettings settings, out Transform root)
        {
            var plan = DwellingRuntime.PlanFor(key, settings, kit);
            root = new GameObject("Dwelling - " + key.StreetPortalId).transform;
            DwellingAssembler.Build(plan, kit, root, key, key.StreetPortalId + ".inside");
            Physics.SyncTransforms();
            return KitLayoutCheck.Check(root.GetComponentsInChildren<KitBlock>());
        }

        public static SliceSettings Settings => AssetDatabase.LoadAssetAtPath<SliceSettings>(SettingsPath);

        public static DwellingKit Kit => AssetDatabase.LoadAssetAtPath<DwellingKit>(KitPath);
    }
}
