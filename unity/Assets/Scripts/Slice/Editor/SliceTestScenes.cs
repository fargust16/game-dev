using System.Linq;
using GameDev.Cameras;
using GameDev.Locomotion;
using GameDev.Slice.Kit;
using GameDev.UI;
using GameDev.World.Interiors;
using GameDev.World.Portals;
using GameDev.World.Timekeeping;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Builds the slice player prefab and the S0.3 movement test scene from stand-ins (VERTICAL_SLICE_IMPLEMENTATION_PLAN
    /// S0.3). The test scene is a lab, not Veilmouth: a paved square with a corner of a house, a doorway, a fence,
    /// crates, and trees, so movement, the camera's spring arm, and later the grid have something to meet. Its door
    /// leads to the S0.4 tavern test interior, a stand-in room whose door leads back.
    /// </summary>
    public static class SliceTestScenes
    {
        public const string PlayerPrefabPath = "Assets/Prefabs/Slice/SlicePlayer.prefab";
        public const string MovementScenePath = "Assets/Scenes/Slice/S0_Movement_Test.unity";
        public const string TavernScenePath = "Assets/Scenes/Slice/S0_Tavern_Test.unity";

        public const string TavernBuildingId = "tavern";
        public const string TavernStreetPortalId = "tavern.street";
        public const string TavernInsidePortalId = "tavern.inside";

        /// <summary>Where the movement test puts the player: a clear run of 18 m to the north.</summary>
        public static readonly Vector3 MovementSpawn = new Vector3(-2f, 0.1f, -12f);

        private const string CharacterPrefabPath = "Assets/Kevin Iglesias/Human Character Dummy/Prefabs/HumanDummy_M White.prefab";
        private const string AnimatorPath = "Assets/Settings/OverworldPlayerAnimator.controller";
        private const string ClockSettingsPath = "Assets/Data/Slice/GameClockSettings.asset";
        private const string PanelSettingsPath = "Assets/UI/SlicePanelSettings.asset";
        private const string ThemePath = "Assets/UI/SliceRuntimeTheme.tss";
        private const string HudPath = "Assets/UI/Hud/Hud.uxml";

        // Kit standard §3, measured: the walker of the grid test.
        private const float PlayerHeight = 1.8f;
        private const float PlayerRadius = 0.3f;
        private const float PlayerStep = 0.4f;
        private const float PlayerSlopeLimit = 45f;

        /// <summary>Stand-ins, then both test scenes; batch runs use it through -executeMethod.</summary>
        [MenuItem("GameDev/Slice/Build All Test Scenes")]
        public static void BuildAll()
        {
            KitStandIns.CreateAll();
            BuildPlayerPrefab();
            BuildMovementScene();
            BuildTavernScene();
        }

        [MenuItem("GameDev/Slice/Build Player Prefab")]
        public static void BuildPlayerPrefab()
        {
            var character = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPrefabPath);
            var animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorPath);
            if (character == null || animatorController == null)
            {
                Debug.LogError($"Slice player needs '{CharacterPrefabPath}' and '{AnimatorPath}'.");
                return;
            }

            var root = new GameObject("SlicePlayer");
            var controller = root.AddComponent<CharacterController>();
            controller.height = PlayerHeight;
            controller.radius = PlayerRadius;
            controller.center = new Vector3(0f, PlayerHeight * 0.5f, 0f);
            controller.stepOffset = PlayerStep;
            controller.slopeLimit = PlayerSlopeLimit;
            controller.skinWidth = 0.03f;

            // Unity's default of 0.001 m drops every step shorter than that, so at a very high frame rate the player
            // stands still and loses contact with the ground; Unity's own guidance is to leave it at 0.
            controller.minMoveDistance = 0f;
            root.AddComponent<PlayerLocomotion>();

            var model = (GameObject)PrefabUtility.InstantiatePrefab(character, root.transform);
            model.name = "Model";
            foreach (var collider in model.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(collider);
            }

            var animator = model.GetComponent<Animator>();
            if (animator == null)
            {
                animator = model.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = animatorController;
            animator.applyRootMotion = false;
            FitHeight(model, PlayerHeight);

            SetLayer(root, KitLayers.Character);
            KitStandIns.EnsureFolder("Assets/Prefabs/Slice");
            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"Slice player written to {PlayerPrefabPath}.");
        }

        [MenuItem("GameDev/Slice/Build Movement Test Scene")]
        public static void BuildMovementScene()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) == null)
            {
                BuildPlayerPrefab();
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            for (var x = -20; x < 20; x += 4)
            {
                for (var z = -20; z < 20; z += 4)
                {
                    Place("Kit_Street_StandIn_4x4", x, z);
                }
            }

            // The corner of a house: a front along X at z = 8 with a doorway, a side wall along +Z, posts at the nodes.
            Place("Kit_Post_StandIn_0x3", 0, 8);
            Place("Kit_Wall_StandIn_4x3", 0, 8);
            Place("Kit_Post_StandIn_0x3", 4, 8);
            PlaceDoor(4, 8, TavernStreetPortalId, TavernScenePath, TavernInsidePortalId, false);
            Place("Kit_Post_StandIn_0x3", 7, 8);
            Place("Kit_Wall_StandIn_2x3", 7, 8);
            Place("Kit_Post_StandIn_0x3", 9, 8);
            Place("Kit_Wall_StandIn_4x3", 0, 8, -90f);
            Place("Kit_Post_StandIn_0x3", 0, 12);

            Place("Kit_Fence_StandIn_2x1", -8, -4);
            Place("Kit_Fence_StandIn_2x1", -6, -4);
            Place("Kit_Crate_StandIn_1x1", 2, 2);
            Place("Kit_Crate_StandIn_1x1", 3, 2);
            Place("Kit_Tree_StandIn_1x4", -10, 6);
            Place("Kit_Tree_StandIn_1x4", -12, 10);
            Place("Kit_Tree_StandIn_1x4", -8, 12);

            AddPlayerRig(MovementSpawn);
            Save(scene, MovementScenePath);
        }

        /// <summary>
        /// The tavern test interior: an 8 × 8 m stand-in room whose door, in the south wall, leads back to the street.
        /// The real tavern is built from the owner's kit in S0.6.
        /// </summary>
        [MenuItem("GameDev/Slice/Build Tavern Test Interior")]
        public static void BuildTavernScene()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) == null)
            {
                BuildPlayerPrefab();
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            for (var x = 0; x < 8; x += 4)
            {
                for (var z = 0; z < 8; z += 4)
                {
                    Place("Kit_Street_StandIn_4x4", x, z);
                }
            }

            // South wall, 2 + 3 + 2 + 1 m, with the door; posts at every node where blocks meet.
            Place("Kit_Post_StandIn_0x3", 0, 0);
            Place("Kit_Wall_StandIn_2x3", 0, 0);
            Place("Kit_Post_StandIn_0x3", 2, 0);
            PlaceDoor(2, 0, TavernInsidePortalId, MovementScenePath, TavernStreetPortalId, true);
            Place("Kit_Post_StandIn_0x3", 5, 0);
            Place("Kit_Wall_StandIn_2x3", 5, 0);
            Place("Kit_Post_StandIn_0x3", 7, 0);
            Place("Kit_Wall_StandIn_1x3", 7, 0);
            Place("Kit_Post_StandIn_0x3", 8, 0);

            // North wall and the two side walls, 4 + 4 m each.
            Place("Kit_Post_StandIn_0x3", 0, 8);
            Place("Kit_Wall_StandIn_4x3", 0, 8);
            Place("Kit_Post_StandIn_0x3", 4, 8);
            Place("Kit_Wall_StandIn_4x3", 4, 8);
            Place("Kit_Post_StandIn_0x3", 8, 8);
            Place("Kit_Wall_StandIn_4x3", 0, 0, -90f);
            Place("Kit_Post_StandIn_0x3", 0, 4);
            Place("Kit_Wall_StandIn_4x3", 0, 4, -90f);
            Place("Kit_Wall_StandIn_4x3", 8, 0, -90f);
            Place("Kit_Post_StandIn_0x3", 8, 4);
            Place("Kit_Wall_StandIn_4x3", 8, 4, -90f);

            // Two tables.
            Place("Kit_Crate_StandIn_1x1", 2, 5);
            Place("Kit_Crate_StandIn_1x1", 5, 5);

            new GameObject("Interior").AddComponent<InteriorRuntime>().Configure(TavernBuildingId);
            AddPlayerRig(new Vector3(4f, 0.1f, 3f));
            Save(scene, TavernScenePath);
        }

        /// <summary>The scene's name as SceneManager knows it.</summary>
        public static string SceneName(string scenePath) => System.IO.Path.GetFileNameWithoutExtension(scenePath);

        /// <summary>The player, the camera behind them, a clock, and the HUD, so a scene also plays when opened alone.</summary>
        private static void AddPlayerRig(Vector3 spawn)
        {
            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath));
            player.transform.position = spawn;

            var cameraRig = Camera.main.gameObject.AddComponent<ThirdPersonCamera>();
            cameraRig.SetTarget(player.transform);
            cameraRig.SetCollisionMask(~LayerMask.GetMask(KitLayers.Character, KitLayers.NoGrid, "Ignore Raycast"));

            var clock = new GameObject("GameClock").AddComponent<GameClockDriver>();
            var serializedClock = new SerializedObject(clock);
            serializedClock.FindProperty("settings").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameClockSettings>(ClockSettingsPath);
            serializedClock.ApplyModifiedPropertiesWithoutUndo();

            var hud = new GameObject("HUD");
            var document = hud.AddComponent<UIDocument>();
            document.panelSettings = PanelSettingsAsset();
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(HudPath);
            hud.AddComponent<HudView>();
        }

        /// <summary>The slice's one panel: Unity's default runtime theme, scaled from 1920 × 1080 (authoring default).</summary>
        private static PanelSettings PanelSettingsAsset()
        {
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panel != null)
            {
                return panel;
            }

            panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1920, 1080);
            AssetDatabase.CreateAsset(panel, PanelSettingsPath);
            return panel;
        }

        private static void Save(UnityEngine.SceneManagement.Scene scene, string path)
        {
            KitStandIns.EnsureFolder("Assets/Scenes/Slice");
            EditorSceneManager.SaveScene(scene, path);
            AddToBuildSettings(path);

            var faults = KitLayoutCheck.Check(Object.FindObjectsByType<KitBlock>(FindObjectsInactive.Exclude));
            Debug.Log($"Test scene written to {path}; kit layout faults: {faults.Count}.");
        }

        /// <summary>A door loads its target by name, which needs the scene in Build Settings.</summary>
        private static void AddToBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes;
            if (scenes.Any(s => s.path == path))
            {
                return;
            }

            EditorBuildSettings.scenes = scenes.Append(new EditorBuildSettingsScene(path, true)).ToArray();
        }

        private static void PlaceDoor(int x, int z, string id, string targetScenePath, string targetPortalId, bool arriveOnPositiveZ)
        {
            var portal = Place("Kit_PortalDoor_StandIn_3x3", x, z).GetComponent<Portal>();
            portal.Configure(id, SceneName(targetScenePath), targetPortalId, arriveOnPositiveZ);

            // A field set from code on a prefab instance is saved only once recorded as an override.
            PrefabUtility.RecordPrefabInstancePropertyModifications(portal);
        }

        private static GameObject Place(string standIn, int x, int z, float yaw = 0f)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{KitStandIns.Folder}/{standIn}.prefab");
            var block = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            block.transform.SetPositionAndRotation(new Vector3(x, 0f, z), Quaternion.Euler(0f, yaw, 0f));
            return block;
        }

        private static void FitHeight(GameObject model, float height)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            var bounds = renderers.Skip(1).Aggregate(renderers[0].bounds, (b, r) =>
            {
                b.Encapsulate(r.bounds);
                return b;
            });
            if (bounds.size.y > 0.05f)
            {
                model.transform.localScale = Vector3.one * (height / bounds.size.y);
            }
        }

        private static void SetLayer(GameObject root, string layer)
        {
            var index = LayerMask.NameToLayer(layer);
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = index;
            }
        }
    }
}
