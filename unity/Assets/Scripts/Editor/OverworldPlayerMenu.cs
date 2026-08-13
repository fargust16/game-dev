#if UNITY_EDITOR
using System.Linq;
using GameDev.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Wires the Blender-imported Char_UEZ model up to Human Basic Motions locomotion clips (see
    /// ASSET_LIBRARY_REFERENCE.md - locked default for Locomotion V0) so it can walk/run/sprint/jump
    /// around the Overworld terrain: configure the FBX as a Humanoid rig, build a locomotion Animator
    /// Controller (Idle/Walk/Run/Sprint 1D blend tree + a Jump-Begin/Fall/Land sub-sequence) from the
    /// Male clip set, then spawn the character with a CharacterController + OverworldPlayerController
    /// on top of the generated terrain and point the existing SimpleFollowCamera at it.
    /// <para>
    /// Step 1 (Humanoid rig) can only be verified so far by script - avatar auto-mapping depends on
    /// how closely Char_UEZ's bone names/hierarchy match Unity's Humanoid template, which a custom
    /// Blender export won't always hit. If the avatar doesn't come back valid, open
    /// Assets/Characters/Char_UEZ.fbx's Rig import tab and use "Configure Avatar" to fix any
    /// unmapped bones by hand before running the remaining steps.
    /// </para>
    /// </summary>
    public static class OverworldPlayerMenu
    {
        private const string CharacterFbxPath = "Assets/Characters/Char_UEZ.fbx";
        private const string ControllerPath = "Assets/Settings/OverworldPlayerAnimator.controller";
        private const string PlayerObjectName = "OverworldPlayer";

        private const string ClipsRoot = "Assets/Kevin Iglesias/Human Animations/Animations/Male";
        private const string IdleClipPath = ClipsRoot + "/Idles/HumanM@Idle01.fbx";
        private const string WalkClipPath = ClipsRoot + "/Movement/Walk/HumanM@Walk01_Forward.fbx";
        private const string RunClipPath = ClipsRoot + "/Movement/Run/HumanM@Run01_Forward.fbx";
        private const string SprintClipPath = ClipsRoot + "/Movement/Sprint/HumanM@Sprint01_Forward.fbx";
        private const string JumpBeginClipPath = ClipsRoot + "/Movement/Jump/HumanM@Jump01 - Begin.fbx";
        private const string FallClipPath = ClipsRoot + "/Movement/Jump/HumanM@Fall01.fbx";
        private const string JumpLandClipPath = ClipsRoot + "/Movement/Jump/HumanM@Jump01 - Land.fbx";

        [MenuItem("GameDev/Overworld/Player/Setup Player (All Steps)")]
        public static void SetupPlayer()
        {
            ConfigureCharacterAsHumanoid();
            BuildLocomotionAnimatorController();
            SpawnOrUpdatePlayerInScene();
        }

        [MenuItem("GameDev/Overworld/Player/1) Configure Char_UEZ As Humanoid")]
        public static void ConfigureCharacterAsHumanoid()
        {
            if (!(AssetImporter.GetAtPath(CharacterFbxPath) is ModelImporter importer))
            {
                Debug.LogError($"[Overworld] No model at '{CharacterFbxPath}'.");
                return;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();

            var avatar = AssetDatabase.LoadAllAssetsAtPath(CharacterFbxPath).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
            {
                Debug.LogWarning(
                    "[Overworld] Char_UEZ did not auto-map cleanly as Humanoid. Select the FBX, open " +
                    "the Rig tab, click 'Configure Avatar' and fix any unmapped bones by hand, then " +
                    "re-run 'Setup Player (All Steps)'.");
                return;
            }

            Debug.Log("[Overworld] Char_UEZ configured and auto-mapped as a valid Humanoid avatar.");
        }

        [MenuItem("GameDev/Overworld/Player/2) Build Locomotion Animator Controller")]
        public static void BuildLocomotionAnimatorController()
        {
            EnsureSettingsFolder();

            var idle = LoadClip(IdleClipPath);
            var walk = LoadClip(WalkClipPath);
            var run = LoadClip(RunClipPath);
            var sprint = LoadClip(SprintClipPath);
            var jumpBegin = LoadClip(JumpBeginClipPath);
            var fall = LoadClip(FallClipPath);
            var jumpLand = LoadClip(JumpLandClipPath);
            if (idle == null || walk == null || run == null || sprint == null ||
                jumpBegin == null || fall == null || jumpLand == null)
            {
                Debug.LogError("[Overworld] Missing one or more Human Basic Motions clips - aborting controller build.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);

            var stateMachine = controller.layers[0].stateMachine;

            var locomotionTree = new BlendTree { name = "Locomotion", blendType = BlendTreeType.Simple1D, blendParameter = "Speed" };
            AssetDatabase.AddObjectToAsset(locomotionTree, controller);
            locomotionTree.AddChild(idle, 0f);
            locomotionTree.AddChild(walk, 1f);
            locomotionTree.AddChild(run, 2f);
            locomotionTree.AddChild(sprint, 3f);

            var locomotionState = stateMachine.AddState("Locomotion", new Vector3(0, 0));
            locomotionState.motion = locomotionTree;
            stateMachine.defaultState = locomotionState;

            var jumpBeginState = stateMachine.AddState("JumpBegin", new Vector3(300, -80));
            jumpBeginState.motion = jumpBegin;

            var fallState = stateMachine.AddState("Fall", new Vector3(300, 80));
            fallState.motion = fall;

            var jumpLandState = stateMachine.AddState("JumpLand", new Vector3(600, 0));
            jumpLandState.motion = jumpLand;

            // Jump pressed while moving/idle -> leave the ground, regardless of Grounded (button
            // press already implies grounded - OverworldPlayerController only fires it then).
            var toJumpBegin = locomotionState.AddTransition(jumpBeginState);
            toJumpBegin.hasExitTime = false;
            toJumpBegin.duration = 0.05f;
            toJumpBegin.AddCondition(AnimatorConditionMode.If, 0, "Jump");

            // Walking off a ledge without pressing Jump: fall directly once Grounded drops.
            var toFallFromLocomotion = locomotionState.AddTransition(fallState);
            toFallFromLocomotion.hasExitTime = false;
            toFallFromLocomotion.duration = 0.15f;
            toFallFromLocomotion.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");

            // JumpBegin plays once, then always drops into the Fall loop.
            var beginToFall = jumpBeginState.AddTransition(fallState);
            beginToFall.hasExitTime = true;
            beginToFall.exitTime = 0.9f;
            beginToFall.duration = 0.1f;

            // Fall loops until the controller reports Grounded again.
            var fallToLand = fallState.AddTransition(jumpLandState);
            fallToLand.hasExitTime = false;
            fallToLand.duration = 0.05f;
            fallToLand.AddCondition(AnimatorConditionMode.If, 0, "Grounded");

            // Land plays once, then back to Idle/Walk/Run/Sprint.
            var landToLocomotion = jumpLandState.AddTransition(locomotionState);
            landToLocomotion.hasExitTime = true;
            landToLocomotion.exitTime = 0.85f;
            landToLocomotion.duration = 0.15f;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Overworld] Locomotion Animator Controller built at '{ControllerPath}'.");
        }

        [MenuItem("GameDev/Overworld/Player/3) Spawn Or Update Player In Scene")]
        public static void SpawnOrUpdatePlayerInScene()
        {
            var characterAsset = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterFbxPath);
            if (characterAsset == null)
            {
                Debug.LogError($"[Overworld] No model at '{CharacterFbxPath}'.");
                return;
            }

            var controllerAsset = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            if (controllerAsset == null)
            {
                Debug.LogError("[Overworld] No Animator Controller at '" + ControllerPath + "' - run step 2 first.");
                return;
            }

            var existing = GameObject.Find(PlayerObjectName);
            var player = existing != null ? existing : (GameObject)PrefabUtility.InstantiatePrefab(characterAsset);
            player.name = PlayerObjectName;

            var animator = player.GetComponent<Animator>();
            if (animator == null)
            {
                animator = player.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controllerAsset;
            animator.applyRootMotion = false;

            var characterController = player.GetComponent<CharacterController>();
            if (characterController == null)
            {
                characterController = player.AddComponent<CharacterController>();
            }

            // Visual mesh → WorldScale character height (controller matches the same metres).
            FitPlayerMeshToWorldScale(player);

            characterController.height = GameDev.World.WorldScale.CharacterHeightMeters;
            characterController.radius = GameDev.World.WorldScale.CharacterRadiusMeters;
            characterController.center = new Vector3(0f, characterController.height * 0.5f, 0f);
            characterController.skinWidth = 0.03f;
            characterController.stepOffset = 0.4f;

            if (player.GetComponent<OverworldPlayerController>() == null)
            {
                player.AddComponent<OverworldPlayerController>();
            }

            // Tile spans (0,0) to (tileSize,tileSize) - (700,700) is the centre for the 1400x1400
            // default OverworldTerrainMenu builds (see its own review-camera framing for the same
            // constant). Only re-placed if not already standing on something, so re-running this
            // step to pick up controller/CharacterController changes doesn't yank a manually
            // positioned player back to the tile centre.
            if (existing == null)
            {
                var spawnXZ = new Vector3(700f, 0f, 700f);
                if (TryFindGroundHeight(spawnXZ, out var groundY))
                {
                    player.transform.position = new Vector3(spawnXZ.x, groundY + 0.1f, spawnXZ.z);
                }
                else
                {
                    Debug.LogWarning(
                        "[Overworld] No ground found under the (700,700) spawn point - generate terrain " +
                        "first (GameDev/Overworld/Generate Terrain From Seed). Player placed at y=0.");
                    player.transform.position = spawnXZ;
                }
            }

            EnsureFollowCamera(player.transform);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = player;
            Debug.Log("[Overworld] Player spawned/updated in scene.");
        }

        private static void FitPlayerMeshToWorldScale(GameObject player)
        {
            var renderers = player.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            player.transform.localScale = Vector3.one;
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            var height = bounds.size.y;
            if (height < 0.05f)
            {
                return;
            }

            var scale = GameDev.World.WorldScale.CharacterHeightMeters / height;
            player.transform.localScale = Vector3.one * scale;
        }

        private static bool TryFindGroundHeight(Vector3 xz, out float groundY)
        {
            var origin = new Vector3(xz.x, 5000f, xz.z);
            if (Physics.Raycast(origin, Vector3.down, out var hit, 10000f))
            {
                groundY = hit.point.y;
                return true;
            }

            groundY = 0f;
            return false;
        }

        /// <summary>Genshin-style close third-person orbit (GameDev.Player.OverworldThirdPersonCamera)
        /// - distinct from GameDev.Player.SimpleFollowCamera, which stays the BG3-tactical top-down
        /// rig for the Town/Grid lab. Reuses whatever Camera already exists in the scene (e.g.
        /// OverworldTerrainMenu's straight-down review camera) rather than adding a second one, and
        /// strips any leftover SimpleFollowCamera from an older run so the two don't fight over the
        /// transform every LateUpdate.
        /// <para>
        /// Also forces Solid Color clear instead of Skybox: this lab scene was created via
        /// EditorSceneManager.NewScene(EmptyScene, ...), which has no Skybox material assigned in
        /// RenderSettings - a Camera left on its AddComponent default (Clear Flags = Skybox) then
        /// renders flat gray in Play mode instead of erroring, which is easy to mistake for "nothing
        /// is being rendered" when it's really just a missing sky.
        /// </para>
        /// </summary>
        private static void EnsureFollowCamera(Transform target)
        {
            var cameraObject = Camera.main != null ? Camera.main.gameObject : Object.FindFirstObjectByType<Camera>()?.gameObject;
            if (cameraObject == null)
            {
                cameraObject = new GameObject("Player Follow Camera");
                cameraObject.tag = "MainCamera";
            }

            var camera = cameraObject.GetComponent<Camera>();
            if (camera == null)
            {
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.55f, 0.75f, 0.95f);

            var staleFollowCamera = cameraObject.GetComponent<SimpleFollowCamera>();
            if (staleFollowCamera != null)
            {
                Object.DestroyImmediate(staleFollowCamera);
            }

            var thirdPersonCamera = cameraObject.GetComponent<OverworldThirdPersonCamera>();
            if (thirdPersonCamera == null)
            {
                thirdPersonCamera = cameraObject.AddComponent<OverworldThirdPersonCamera>();
            }

            thirdPersonCamera.SetTarget(target);
        }

        private static AnimationClip LoadClip(string fbxPath)
        {
            var clip = AssetDatabase.LoadAllAssetsAtPath(fbxPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            if (clip == null)
            {
                Debug.LogError($"[Overworld] No AnimationClip found at '{fbxPath}'.");
            }

            return clip;
        }

        private static void EnsureSettingsFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }
        }
    }
}
#endif
