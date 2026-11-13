using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GameDev.Locomotion;
using GameDev.Slice.Kit;
using GameDev.Slice.People;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Every person, the player too, is one of the owner's Tripo people, by role (owner, 2026-11-10; SLICE_ASSET_MAP.md
    /// §1), playing Kevin Iglesias' and Quaternius' clips (ANIMATION_PIPELINE_PLAN.md §0 and §8; <see cref="SliceClips"/>).
    /// <list type="bullet">
    /// <item>Every clip is Humanoid and in place, a woman playing the women's takes; a Warden stands at attention. The player's locomotion plays each clip at the player's pace, measured from the
    /// feet.</item>
    /// <item>A model's prefab is fitted in its first pose: turned to face +Z by its shoulders, scaled to its height by
    /// its eyes (eye height 93.6 % of stature, an anthropometric mean) where the avatar maps them, else by its mesh, so
    /// a hat does not shorten a man; its feet on the root.</item>
    /// <item>The Dummies, and the POI casts standing in the townsfolk's walking prefabs, take their role's model in place
    /// (<see cref="PrefabUtility.ReplacePrefabAssetOfPrefabInstance(GameObject, GameObject, PrefabReplacingSettings, InteractionMode)"/>),
    /// so their names, places, NamedPerson and PersonTalk stay. Townsfolk walking by a rich block wear the gentleman's
    /// and the lady's models, by a middling one half of them, by a poor one or the works the working people's.</item>
    /// <item>The blacksmith stands at Bawling Row's anvil beside Edda Brann; walkers whose route now meets the bronze
    /// statue or a new street lamp walk a route planned again.</item>
    /// </list>
    /// Batch, the clips imported, every controller and person built again, the scenes and the player:
    /// -executeMethod GameDev.Slice.EditorTools.SlicePeopleModels.Run
    /// </summary>
    public static class SlicePeopleModels
    {
        public const string Folder = "Assets/Prefabs/Slice/People";
        public const string ManController = SliceClips.ControllerFolder + "/Man.controller";
        public const string WomanController = SliceClips.ControllerFolder + "/Woman.controller";
        private const string StandIns = "Assets/Animation/StandIn";
        /// <summary>The woman player's controller: the player's, with every Kevin Iglesias clip in its women's take.</summary>
        public const string WomanPlayerController = "Assets/Settings/OverworldPlayerAnimator_Woman.overrideController";
        private const string ManWalkerController = "Assets/Settings/Slice/Townsfolk/SliceTownsfolk.controller";
        private const string WomanWalkerController = "Assets/Settings/Slice/Townsfolk/Woman/SliceTownswoman.controller";
        public const string PlayerController = "Assets/Settings/OverworldPlayerAnimator.controller";
        private const string OutputFolder = "Logs/SliceSurvey";

        /// <summary>
        /// S5b.4: the work states the player's controller lacks (brewing's Interact) added in place, and the woman's override
        /// renewed over them; batch runs use it through -executeMethod.
        /// </summary>
        [MenuItem("GameDev/Slice/People/Add the Player's Missing Work States")]
        public static void AddPlayerWorkStates()
        {
            var added = SliceClips.AddWorkStates(PlayerController);
            var player = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerController);
            if (player != null)
            {
                SliceClips.WomanOverride(player, WomanPlayerController);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[SlicePeopleModels] work states added to the player's controller: {(added.Count > 0 ? string.Join(", ", added) : "none")}.");
        }

        /// <summary>
        /// Authoring default: the player's locomotion plays a clip at most this much faster, so a step keeps a human
        /// cadence; where a clip's in-place stride is shorter than the player's speed, the feet slide the rest.
        /// </summary>
        private const float MostTimeScale = 1.3f;

        /// <summary>The slowest a moving clip plays to keep pace with the body (authoring default).</summary>
        private const float LeastTimeScale = 0.6f;

        /// <summary>Authoring default: eye height as a share of stature (adult mean).</summary>
        private const float EyeShare = 0.936f;

        /// <summary>One of the owner's people: its model, its sex, its height (authoring default, asset map §1).</summary>
        public sealed class Look
        {
            public readonly string Name;
            public readonly string File;
            public readonly bool Female;
            public readonly float Height;

            /// <summary>The role its prefab gives the person (10.2 §5a *Roles*).</summary>
            public readonly ResidentRole Role;

            public Look(string name, string file, bool female, float height, ResidentRole role = ResidentRole.Townsfolk)
            {
                Name = name;
                File = file;
                Female = female;
                Height = height;
                Role = role;
            }

            public string Model => SliceCharacterRigs.ModelPath(File);

            public string Standing => $"{Folder}/{Name}.prefab";

            /// <summary>The walking prefab.</summary>
            public string Walking => $"{Folder}/{Name}Walker.prefab";
        }

        public static readonly Look Gentleman = new Look("Gentleman", "vintage_gentleman_3d_model", false, 1.78f);
        public static readonly Look Lady = new Look("Lady", "victorian_dress_3d_model", true, 1.66f);
        public static readonly Look WorkingWoman = new Look("WorkingWoman", "victorian_woman_3d_model", true, 1.63f);
        public static readonly Look Officer = new Look("Officer", "military_officer_3d_model", false, 1.80f, ResidentRole.Warden);
        public static readonly Look Detective = new Look("Detective", "steampunk_detective_3d_model", false, 1.79f, ResidentRole.Cult);
        public static readonly Look Blacksmith = new Look("Blacksmith", "medieval_blacksmith_3d_model", false, 1.82f);

        /// <summary>
        /// The first townsfolk models, the stylized man and the historical woman, are retired as out of style (owner,
        /// 2026-11-12): the working men wear the gentleman's model and the older women the working woman's until the
        /// owner brings a working man's and an older woman's (asset map §1).
        /// </summary>
        public static readonly string[] Retired =
        {
            "Assets/Prefabs/Slice/Townsperson.prefab", "Assets/Prefabs/Slice/Townswoman.prefab", Folder + "/Townsman.prefab", Folder + "/Matron.prefab",
        };

        public static readonly Look WardenWoman = new Look("WardenWoman", "military_uniform_3d_model", true, 1.70f, ResidentRole.Warden);

        // The owner's third list of 2026-11-12 (asset map §1), each file renamed by its role; heights are authoring defaults.
        public static readonly Look OldBeliever = new Look("OldBeliever", "old_believer_woman", true, 1.58f, ResidentRole.OldBeliever);
        public static readonly Look ChurchMaid = new Look("ChurchMaid", "church_maid", true, 1.60f, ResidentRole.ChurchMaid);
        public static readonly Look OldPriest = new Look("OldPriest", "old_priest", false, 1.72f, ResidentRole.OldPriest);
        public static readonly Look Acolyte = new Look("Acolyte", "young_acolyte", false, 1.76f, ResidentRole.Acolyte);
        public static readonly Look FactoryWorker = new Look("FactoryWorker", "old_factory_worker", false, 1.70f, ResidentRole.FactoryWorker);
        public static readonly Look Vagrant = new Look("Vagrant", "street_vagrant", false, 1.74f, ResidentRole.Vagrant);
        public static readonly Look Dockworker = new Look("Dockworker", "young_dockworker", false, 1.82f, ResidentRole.Dockworker);
        public static readonly Look OldDockworker = new Look("OldDockworker", "old_dockworker", false, 1.74f, ResidentRole.OldDockworker);
        public static readonly Look Moneylender = new Look("Moneylender", "moneylender", false, 1.76f, ResidentRole.Moneylender);
        public static readonly Look Innkeeper = new Look("Innkeeper", "innkeeper", false, 1.72f, ResidentRole.Innkeeper);
        public static readonly Look Tailor = new Look("Tailor", "tailor", false, 1.75f, ResidentRole.Tailor);

        /// <summary>
        /// Nell Garrow's own model since the owner's answer to S5-9 (2026-11-13; asset map §1): the owner's tavern keeper,
        /// Tripo's <c>realistic_fantasy_character_3d_model</c> renamed <c>tavern_keeper</c>; she keeps the tavern, so her
        /// role is the innkeeper's. Height an authoring default.
        /// </summary>
        public static readonly Look TavernKeeper = new Look("TavernKeeper", "tavern_keeper", true, 1.66f, ResidentRole.Innkeeper);

        /// <summary>The third list's looks.</summary>
        public static readonly Look[] Roles = { OldBeliever, ChurchMaid, OldPriest, Acolyte, FactoryWorker, Vagrant, Dockworker, OldDockworker, Moneylender, Innkeeper, Tailor };

        public static readonly Look[] All =
        {
            Gentleman, Lady, WorkingWoman, Officer, Detective, Blacksmith, WardenWoman,
            OldBeliever, ChurchMaid, OldPriest, Acolyte, FactoryWorker, Vagrant, Dockworker, OldDockworker, Moneylender, Innkeeper, Tailor,
            TavernKeeper,
        };

        /// <summary>The player's two bodies (owner, 2026-11-10): worn by the player's prefabs, with no standing prefab of their own.</summary>
        public static readonly Look PlayerMan = new Look("PlayerMan", "human_figure_3d_model", false, 1.80f);
        public static readonly Look PlayerWoman = new Look("PlayerWoman", "human_body_3d_model", true, 1.68f);
        public const string PlayerWomanPrefab = "Assets/Prefabs/Slice/SlicePlayerWoman.prefab";

        /// <summary>The looks with a walking prefab: the first three, and the third list's people of the streets.</summary>
        private static readonly Look[] NewWalkers = { Gentleman, Lady, WorkingWoman, OldBeliever, FactoryWorker, Vagrant, Dockworker, OldDockworker, Moneylender, Tailor };

        /// <summary>The third list's walking looks.</summary>
        private static readonly Look[] RoleWalkers = { OldBeliever, FactoryWorker, Vagrant, Dockworker, OldDockworker, Moneylender, Tailor };

        /// <summary>
        /// Who wears which model (asset map §1): by the person's name in the scene, the Dummies' role names before
        /// SlicePeople named them among them, else by the stand-in colour of their role.
        /// </summary>
        public static Look LookFor(string name, bool female, string colour = null)
        {
            if (name.Contains("Warden") || name == "Sergeant Coyle")
            {
                return female ? WardenWoman : Officer;
            }

            if (name.StartsWith("Development opponent") || name.StartsWith("Cult"))
            {
                return Detective;
            }

            switch (name)
            {
                case "Blacksmith":
                    return Blacksmith;
                case "Old priest": return OldPriest;
                case "Acolyte": return Acolyte;
                case "Church maid": return ChurchMaid;
                case "Innkeeper": case "Market trader": return Innkeeper;
                case "Moneylender": case "Mortimer Kettle": case "The pawnbroker": return Moneylender;
                case "Nell Garrow": return TavernKeeper;
                case "Father Ambrose Hale": return OldPriest;
                case "Josiah Harrow": case "Harrow": case "POI A - Harrow": case "Silas Quill": case "Tobias Penn": case "Mr Ashgrave":
                case "The trader": case "Master of Enchanting":
                    return Gentleman;
                case "Ada Merrow": case "Madame Ilse": case "Master of First Aid": case "The seer":
                    return Lady;
                case "Lottie Wren": case "Edda Brann": case "Tavern keeper": case "Master of Item Creation":
                    return WorkingWoman;
                case "Pip Vance": case "Kit Mallow": case "Dace": case "Veinmarket": case "Veinmarket at a table": case "Drunkard": case "Farmer":
                    return Gentleman;
                case "POI B - Old woman in a doorway": case "POI A - Neighbour (middle)":
                    return WorkingWoman;
                case "Faction member":
                    return female ? Lady : Gentleman;
            }

            if (name.StartsWith("POI B - Collector") || name.StartsWith("POI A - Neighbour"))
            {
                return Gentleman;
            }

            if (name.StartsWith("POI B - Mill girl"))
            {
                return WorkingWoman;
            }

            return colour switch
            {
                "Blue" => Officer,
                "Red" => Detective,
                "Green" or "Orange" or "Teal" => female ? Lady : Gentleman,
                _ => female ? WorkingWoman : Gentleman,
            };
        }

        /// <summary>
        /// A walking townsperson's model by the wealth of the C5 block nearest its place (§29.2): rich, the gentleman and
        /// the lady; poor or the works, the working people; else either, by the place's cell, so a rebuild picks the same.
        /// </summary>
        public static Look WalkerLook(bool female, Vector3 at, VeilmouthLayout layout)
        {
            var (px, py) = (at.x + 500f, 500f - at.z);
            var block = layout.blocks?.OrderBy(b => Outside(b, px, py)).FirstOrDefault();
            var hash = ((Mathf.FloorToInt(px) * 73856093) ^ (Mathf.FloorToInt(py) * 19349663)) & 0x7fffffff;
            var odd = (hash & 1) == 1;
            var third = (hash >> 1) % 3;

            // The third list (owner, 2026-11-12): the dock workers by the sea's quays, the old factory workers and old
            // dock workers in the works, the vagrants, factory workers and old believers in the poor streets, a tailor in
            // three middling men; the one moneylender is chosen apart (<see cref="OneMoneylender"/>).
            if (!female && layout.water != null && layout.OpenCoast - px < QuayReach)
            {
                return odd ? Dockworker : OldDockworker;
            }

            return (block?.wealth ?? "middling") switch
            {
                "rich" => female ? Lady : Gentleman,
                "works" => female ? WorkingWoman : (odd ? FactoryWorker : OldDockworker),
                "poor" => female ? (odd ? OldBeliever : WorkingWoman) : (third == 0 ? FactoryWorker : Vagrant),
                _ => female ? (odd ? WorkingWoman : Lady) : (third == 0 ? Tailor : Gentleman),
            };
        }

        /// <summary>Authoring default, m: a man walking this near the open sea's edge works the quays.</summary>
        private const float QuayReach = 70f;

        private static float Outside(VeilmouthLayout.BlockSpec b, float px, float py)
        {
            var dx = Mathf.Max(b.x0 - px, 0f, px - b.x1);
            var dy = Mathf.Max(b.y0 - py, 0f, py - b.y1);
            return (dx * dx) + (dy * dy);
        }

        /// <summary>A standing person of <paramref name="name"/>'s role under <paramref name="parent"/>, its feet at <paramref name="at"/>.</summary>
        public static GameObject Instantiate(string name, bool female, string colour, Transform parent, Vector3 at, float yaw)
        {
            var look = LookFor(name, female, colour);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(look.Standing), parent);
            go.name = name;
            go.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            return go;
        }

        // ------------------------------------------------------------------------------------------- the pass

        public static void Run()
        {
            var report = new StringBuilder();
            SliceClips.Import(report);
            Controllers(report);
            var speeds = new Dictionary<string, float>();
            foreach (var look in All)
            {
                Build(look, false, null, report);
            }

            foreach (var look in NewWalkers)
            {
                speeds[look.Walking] = Build(look, true, null, report).GetComponent<TownsfolkWalker>().Speed;
            }

            Scenes(speeds, report);
            Opponent();
            WearPlayer(SliceTestScenes.PlayerPrefabPath, PlayerMan, report);
            WearPlayer(PlayerWomanPrefab, PlayerWoman, report);
            Directory.CreateDirectory(OutputFolder);
            File.WriteAllText($"{OutputFolder}/people.txt", report.ToString());
            Debug.Log("[SlicePeopleModels]\n" + report);
            CheckSheet();
            PlayerSheet();
            RenderPeople();
            SliceCharacterRigs.Check();
        }

        /// <summary>
        /// Batch, once (owner, 2026-11-12): the stylized man and the historical woman leave the game. In every slice scene
        /// each person in one of their prefabs takes the gentleman's or the working woman's in place, walking or standing,
        /// its name, place, route and parts kept, and walks at its new prefab's pace; then their four prefabs are deleted.
        /// The owner's model files stay in <c>Assets/TripoModels</c>. -executeMethod GameDev.Slice.EditorTools.SlicePeopleModels.RetireOldLooks
        /// </summary>
        public static void RetireOldLooks()
        {
            var report = new StringBuilder();
            var speeds = NewWalkers.ToDictionary(l => l.Walking, l => AssetDatabase.LoadAssetAtPath<GameObject>(l.Walking).GetComponent<TownsfolkWalker>().Speed);
            foreach (var path in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes/Slice" }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p))
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var lines = new List<string>();
                var old = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(t => t.gameObject)
                    .Where(g => PrefabUtility.IsOutermostPrefabInstanceRoot(g) && Retired.Contains(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g))).ToList();
                foreach (var go in old)
                {
                    var source = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                    var female = source.EndsWith("Townswoman.prefab") || source.EndsWith("Matron.prefab");
                    var look = female ? WorkingWoman : Gentleman;
                    var prefab = go.GetComponent<TownsfolkWalker>() != null ? look.Walking : look.Standing;
                    Replace(go, AssetDatabase.LoadAssetAtPath<GameObject>(prefab));
                    lines.Add($"{go.name}: {Path.GetFileNameWithoutExtension(source)} to {Path.GetFileNameWithoutExtension(prefab)}");
                }

                Paces(speeds, lines);
                if (lines.Count > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    report.AppendLine($"{path}:\n  " + string.Join("\n  ", lines));
                }
            }

            foreach (var path in Retired.Where(p => AssetDatabase.LoadAssetAtPath<GameObject>(p) != null))
            {
                AssetDatabase.DeleteAsset(path);
                report.AppendLine($"{path}: deleted");
            }

            AssetDatabase.SaveAssets();
            Directory.CreateDirectory(OutputFolder);
            File.WriteAllText($"{OutputFolder}/retired_looks.txt", report.ToString());
            Debug.Log("[SlicePeopleModels] the old looks retired:\n" + report);
        }

        /// <summary>
        /// Batch, once (the owner's third list of 2026-11-12, PB2; asset map §1, 10.2 §5a *Roles*): the eleven new people,
        /// their files renamed by role, made Humanoid and built as standing prefabs, and those of the streets as walking ones,
        /// each with its role; every earlier person's prefab given its role; Veilmouth's walkers dressed again by
        /// <see cref="WalkerLook"/> and one rich man made the moneylender; the chapel's priest, acolyte, maid and an old
        /// believer, and the tavern's innkeeper, placed (<see cref="SliceActivities.Interiors"/>); the check sheets drawn.
        /// -executeMethod GameDev.Slice.EditorTools.SlicePeopleModels.ThirdList
        /// </summary>
        public static void ThirdList()
        {
            var report = new StringBuilder();
            SliceCharacterRigs.Convert();
            foreach (var look in Roles)
            {
                Build(look, false, null, report);
            }

            foreach (var look in RoleWalkers)
            {
                Build(look, true, null, report);
            }

            foreach (var look in All.Except(Roles))
            {
                GiveRole(look.Standing, look.Role, report);
            }

            foreach (var look in NewWalkers.Except(RoleWalkers))
            {
                GiveRole(look.Walking, look.Role, report);
            }

            AssetDatabase.SaveAssets();
            var speeds = NewWalkers.ToDictionary(l => l.Walking, l => AssetDatabase.LoadAssetAtPath<GameObject>(l.Walking).GetComponent<TownsfolkWalker>().Speed);
            Scenes(speeds, report);
            NoMoneylender(report);
            SliceActivities.Interiors(report);
            Directory.CreateDirectory(OutputFolder);
            File.WriteAllText($"{OutputFolder}/people_third_list.txt", report.ToString());
            Debug.Log("[SlicePeopleModels] the third list:\n" + report);
            CheckSheet();
            RenderRoles();
        }

        /// <summary>A prefab's role, set on its root.</summary>
        private static void GiveRole(string prefabPath, ResidentRole role, StringBuilder report)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (!root.TryGetComponent<PersonRole>(out var part))
            {
                part = root.AddComponent<PersonRole>();
            }

            part.Role = role;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            report.AppendLine($"{prefabPath}: role {role}");
        }

        /// <summary>
        /// No nameless moneylender (VS-ROLES (a), settled by the owner's answer to S5-9, 2026-11-13): the moneylender's
        /// model is the pawnbroker Mortimer Kettle's, so every walker in it walks as a gentleman again.
        /// </summary>
        private static void NoMoneylender(StringBuilder report)
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var changed = false;
            foreach (var walker in Roots().Where(g => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g) == Moneylender.Walking).ToList())
            {
                Replace(walker, AssetDatabase.LoadAssetAtPath<GameObject>(Gentleman.Walking));
                report.AppendLine($"{walker.name}: no moneylender, a gentleman again");
                changed = true;
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        /// <summary>
        /// The owner's answer to S5-9 (2026-11-13; asset map §1), built in S5c.3: the tavern keeper's model made Humanoid and
        /// built, and worn by Nell Garrow; the pawnbroker the gentleman Mortimer Kettle in the moneylender's model, and no
        /// nameless moneylender walks; the old innkeeper no longer behind the Soot &amp; Bell's bar (the hours' pass sets
        /// him at Bawling Row's food stall). The Tripo folder was renamed on disk first, its <c>.meta</c> files keeping
        /// their GUIDs.
        /// </summary>
        public static void S5Nine(StringBuilder report)
        {
            SliceCharacterRigs.Convert();
            Build(TavernKeeper, false, null, report);
            AssetDatabase.SaveAssets();

            var tavern = EditorSceneManager.OpenScene(VeilmouthScene.SootAndBellScenePath, OpenSceneMode.Single);
            var nell = Object.FindObjectsByType<NamedPerson>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(p => p.Id == "nell");
            if (nell != null && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(nell.gameObject) != TavernKeeper.Standing)
            {
                Replace(nell.gameObject, AssetDatabase.LoadAssetAtPath<GameObject>(TavernKeeper.Standing));
                report.AppendLine($"Nell Garrow: the tavern keeper's model at {nell.transform.position}");
            }

            foreach (var innkeeper in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                         .Where(t => t.name == "Innkeeper" && PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)).ToList())
            {
                Object.DestroyImmediate(innkeeper.gameObject);
                report.AppendLine("The innkeeper: no longer behind the bar");
            }

            EditorSceneManager.MarkSceneDirty(tavern);
            EditorSceneManager.SaveScene(tavern);

            var city = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var pawnbroker = Object.FindObjectsByType<NamedPerson>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(p => p.Id == "ruth" || p.Id == "kettle");
            if (pawnbroker != null)
            {
                if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(pawnbroker.gameObject) != Moneylender.Standing)
                {
                    Replace(pawnbroker.gameObject, AssetDatabase.LoadAssetAtPath<GameObject>(Moneylender.Standing));
                }

                pawnbroker.gameObject.name = "Mortimer Kettle";
                pawnbroker.Configure("kettle");
                EditorUtility.SetDirty(pawnbroker);
                report.AppendLine($"Mortimer Kettle: the pawnbroker in the moneylender's model at {pawnbroker.transform.position}");
                EditorSceneManager.MarkSceneDirty(city);
                EditorSceneManager.SaveScene(city);
            }
            else
            {
                report.AppendLine("The pawnbroker: NOT FOUND in Veilmouth");
            }

            NoMoneylender(report);
        }

        /// <summary>One of each of the third list's walkers in Veilmouth, as the player would meet it (`people_role_*.png`).</summary>
        private static void RenderRoles()
        {
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var people = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var look in RoleWalkers)
            {
                var t = people.FirstOrDefault(p => PrefabUtility.IsOutermostPrefabInstanceRoot(p.gameObject) && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(p.gameObject) == look.Walking);
                if (t == null)
                {
                    Debug.Log($"[SlicePeopleModels] no {look.Name} walking in Veilmouth");
                    continue;
                }

                var at = t.position + (Vector3.up * 1.1f);
                VeilmouthRenders.Eye($"people_role_{look.Name}", at + (t.forward * 3.4f) + (t.right * 1.2f) + (Vector3.up * 0.3f), at, 50f);
            }
        }

        /// <summary>
        /// Batch: the clips imported and cut again and every controller built again, the people and the scenes as they
        /// stand. -executeMethod GameDev.Slice.EditorTools.SlicePeopleModels.ClipsAndControllers
        /// </summary>
        public static void ClipsAndControllers()
        {
            var report = new StringBuilder();
            SliceClips.Import(report);
            Controllers(report);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory(OutputFolder);
            File.WriteAllText($"{OutputFolder}/controllers.txt", report.ToString());
            Debug.Log("[SlicePeopleModels] clips and controllers: " + report);
        }

        /// <summary>
        /// Batch: every controller built again on the clips as they stand, with no import, as when a state is added
        /// (the people's run, PB5b). -executeMethod GameDev.Slice.EditorTools.SlicePeopleModels.ControllersOnly
        /// </summary>
        public static void ControllersOnly()
        {
            var report = new StringBuilder();
            Controllers(report);
            AssetDatabase.SaveAssets();
            Debug.Log("[SlicePeopleModels] controllers: " + report);
        }


        /// <summary>
        /// Batch, after the city is built again: the people's looks, their paces, the blacksmith and the walkers' routes in
        /// the slice's scenes, with the prefabs as they stand. -executeMethod GameDev.Slice.EditorTools.SlicePeopleModels.ScenesOnly
        /// </summary>
        public static void ScenesOnly()
        {
            var speeds = NewWalkers.Select(l => l.Walking)
                .ToDictionary(p => p, p => AssetDatabase.LoadAssetAtPath<GameObject>(p).GetComponent<TownsfolkWalker>().Speed);
            var report = new StringBuilder();
            Scenes(speeds, report);
            Debug.Log("[SlicePeopleModels] scenes:\n" + report);
        }

        // ------------------------------------------------------------------------------------------- controllers

        /// <summary>
        /// The controllers on the clips of <see cref="SliceClips"/>: the standing people's, a woman's in the women's
        /// takes, the Wardens' at attention, the townsfolk's walk and idle, and the player's locomotion at the player's
        /// own pace. The A0 stand-ins go.
        /// </summary>
        private static void Controllers(StringBuilder report)
        {
            Tidy();
            SliceClips.Person(ManController, "Idle", false);
            SliceClips.Person(WomanController, "Idle", true);
            SliceClips.Person(SliceClips.WardenController, "Attention", false);
            SliceClips.Person(SliceClips.WardenWomanController, "Attention", true);
            SliceClips.Person(SliceClips.BlacksmithController, "HammerDown", false, "HammerUp");
            SliceClips.Person(SliceCharacterLook.PersonIdlePath, "Idle", false);
            SliceClips.Walker(ManWalkerController, false);
            SliceClips.Walker(WomanWalkerController, true);

            // The player's pace in each moving clip: how fast its planted feet slide on both bodies at their statures (the
            // mean, as one controller plays both), against PlayerLocomotion's speed for its gait. The pack's eight
            // directions keep the place their names give them in the blend: the feet's own direction reads up to 55 deg off
            // the name as the hips swing (measured), while their speed is one within 10 % for every direction of a gait.
            var locomotion = AssetDatabase.LoadAssetAtPath<GameObject>(SliceTestScenes.PlayerPrefabPath).GetComponentInChildren<PlayerLocomotion>(true);
            var so = new SerializedObject(locomotion);
            float Field(string name) => so.FindProperty(name).floatValue;
            var speeds = new[] { 0f, Field("walkSpeed"), Field("runSpeed"), Field("sprintSpeed") };
            var scales = new Dictionary<string, float>();
            foreach (var (use, gait, way) in SliceClips.Moves())
            {
                var clip = SliceClips.Get(use);
                var travels = new[] { PlayerMan, PlayerWoman }.Select(look =>
                {
                    var animator = Fitted(look, clip);
                    var travel = Travel(animator, clip, animator.transform.parent);
                    Object.DestroyImmediate(animator.transform.parent.gameObject);
                    return travel;
                }).ToList();
                var mean = travels.Average(t => t.magnitude);
                scales[use] = mean > 0.2f ? Mathf.Clamp(speeds[gait] / mean, LeastTimeScale, MostTimeScale) : 1f;
                report.AppendLine($"Player {use}: paced {travels[0].magnitude:0.00} m/s on the man, {travels[1].magnitude:0.00} on the woman (measured), " +
                                  $"the man's feet {Mathf.Atan2(travels[0].x, travels[0].y) * Mathf.Rad2Deg:0} deg against the name's {Mathf.Atan2(way.x, way.y) * Mathf.Rad2Deg:0}; " +
                                  (mean > 0.2f ? $"{speeds[gait]:0.0} m/s would play it x{speeds[gait] / mean:0.00}, " : "too slow to measure, ") +
                                  $"plays it x{scales[use]:0.00}");
            }

            // The turn on the spot is no longer played: the body turns to where it goes (owner, 2026-11-12). Its states stay
            // in the controller at their own pace, Turn01 having no root rotation of its own (measured 0 deg/s).
            const float turnRate = 1f;

            // The sneak's own pace, which PlayerLocomotion matches by the Crouch state's Pace while it creeps.
            var sneak = SliceClips.Get("Sneak");
            var sneakPace = new[] { PlayerMan, PlayerWoman }.Select(look =>
            {
                var animator = Fitted(look, sneak);
                var pace = Pace(animator, sneak, animator.transform.parent);
                Object.DestroyImmediate(animator.transform.parent.gameObject);
                return pace;
            }).Average();
            var player = SliceClips.Player(PlayerController, scales, sneakPace, turnRate);
            SliceClips.WomanOverride(player, WomanPlayerController);
            report.AppendLine($"Player sneak: paced {sneakPace:0.00} m/s (measured, the mean of both bodies), matched by the Crouch state's Pace");
            AssetDatabase.SaveAssets();
        }

        /// <summary>The A0 stand-ins leave: their four clips and the woman player's override go, and the people's two controllers move beside the Wardens'.</summary>
        private static void Tidy()
        {
            if (!AssetDatabase.IsValidFolder(SliceClips.ControllerFolder))
            {
                AssetDatabase.CreateFolder("Assets/Animation", "Controllers");
            }

            foreach (var name in new[] { "Man.controller", "Woman.controller" })
            {
                var old = $"{StandIns}/{name}";
                if (AssetDatabase.LoadAssetAtPath<Object>(old) != null && AssetDatabase.LoadAssetAtPath<Object>($"{SliceClips.ControllerFolder}/{name}") == null)
                {
                    AssetDatabase.MoveAsset(old, $"{SliceClips.ControllerFolder}/{name}");
                }
            }

            foreach (var old in new[] { "ManIdle", "ManWalk", "WomanIdle", "WomanWalk" }.Select(n => $"{StandIns}/{n}.anim"))
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(old) != null)
                {
                    AssetDatabase.DeleteAsset(old);
                }
            }

            if (AssetDatabase.IsValidFolder(StandIns) && AssetDatabase.FindAssets(string.Empty, new[] { StandIns }).Length == 0)
            {
                AssetDatabase.DeleteAsset(StandIns);
            }
        }

        // ------------------------------------------------------------------------------------------- prefabs

        /// <summary>The townsfolk's walking prefab fitted again to its Humanoid walk, its pace measured anew.</summary>
        private static float Refit(string prefabPath, Look look, bool rescale, StringBuilder report)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            var model = root.transform.Find("Model").gameObject;
            var before = Mathf.DeltaAngle(0f, model.transform.localEulerAngles.y);
            var animator = Animated(model, look);
            var walk = SliceClips.Get("Walk", look.Female);
            var stature = Fit(model, walk, look.Height, !rescale);
            var speed = Speed(animator, walk, root.transform);
            var head = Keep(model, animator);
            var capsule = root.GetComponent<CapsuleCollider>();
            capsule.height = look.Height;
            capsule.center = new Vector3(0f, look.Height * 0.5f, 0f);
            var walker = root.GetComponent<TownsfolkWalker>();
            walker.Configure(walker.Route, speed, SliceTownsfolk.GroundMask);
            var after = Mathf.DeltaAngle(0f, model.transform.localEulerAngles.y);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            report.AppendLine($"{prefabPath}: {stature:0.00} m (head bone at {head:0.00} m), walking {speed:0.00} m/s in {walk.name}, " +
                              $"the model turned from {before:+0.0;-0.0} to {after:+0.0;-0.0} deg");
            return speed;
        }

        private static GameObject Build(Look look, bool walking, Vector3? scale, StringBuilder report)
        {
            var idle = SliceClips.Get(IdleUse(look), look.Female);
            var walk = SliceClips.Get("Walk", look.Female);
            var root = new GameObject(look.Name);
            root.AddComponent<PersonRole>().Role = look.Role;
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(look.Model), root.transform);
            model.name = "Model";
            SliceKitCatalogue.LinkTripoTextures(model, look.Model, look.File);
            var animator = Animated(model, look);
            if (scale.HasValue)
            {
                model.transform.localScale = scale.Value;
            }

            // The controller first: assigning it binds the Animator again, so the pose comes after.
            var controllerPath = walking ? (look.Female ? WomanWalkerController : ManWalkerController) : StandingController(look);
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            var stature = Fit(model, walking ? walk : idle, look.Height, scale.HasValue);
            var speed = walking ? Speed(animator, walk, root.transform) : 0f;
            var head = Keep(model, animator);
            var armLive = ArmDrop(animator);
            var armNames = (animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).name, animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).name);

            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = look.Height;
            capsule.radius = 0.25f;
            capsule.center = new Vector3(0f, look.Height * 0.5f, 0f);
            if (walking)
            {
                SliceTownsfolk.ShoulderLooseProps(root);
                root.AddComponent<TownsfolkWalker>().Configure(new Vector3[0], speed, SliceTownsfolk.GroundMask);
            }

            SliceKitCatalogue.SetLayer(root, KitLayers.Character);
            Directory.CreateDirectory(Folder);
            var path = walking ? look.Walking : look.Standing;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            if (look == Blacksmith && !walking)
            {
                SliceBlacksmith.Arm(report); // his hammer in his right hand
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            var saved = prefab.GetComponentsInChildren<Transform>(true);
            var upper = saved.FirstOrDefault(t => t.name == armNames.Item1);
            var lower = saved.FirstOrDefault(t => t.name == armNames.Item2);
            var armSaved = upper != null && lower != null ? Vector3.Angle(lower.position - upper.position, Vector3.down) : -1f;
            report.AppendLine($"{path}: {look.File}, {stature:0.00} m (head bone at {head:0.00} m){(walking ? $", walking {speed:0.00} m/s" : string.Empty)}, " +
                              $"upper arm {armLive:0}° from down as posed, {armSaved:0}° as saved");
            return prefab;
        }

        /// <summary>A standing look's idle: a Warden's at attention, a man's or a woman's.</summary>
        private static string IdleUse(Look look) => look == Officer || look == WardenWoman ? "Attention" : "Idle";

        /// <summary>A standing look's controller: the Wardens' at attention, the blacksmith's hammering at the anvil (A3).</summary>
        private static string StandingController(Look look) =>
            look == Officer ? SliceClips.WardenController : look == WardenWoman ? SliceClips.WardenWomanController :
            look == Blacksmith ? SliceClips.BlacksmithController : look.Female ? WomanController : ManController;

        /// <summary>A loose copy of <paramref name="look"/>'s model under a root of its own, at its stature in the clip's first pose; the caller destroys the root.</summary>
        internal static Animator Fitted(Look look, AnimationClip clip)
        {
            var root = new GameObject(look.Name);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(look.Model), root.transform);
            var animator = Animated(model, look);
            Fit(model, clip, look.Height, false);
            return animator;
        }

        /// <summary>The left upper arm's angle from straight down: about 90° in a T-pose, under 30° at rest.</summary>
        internal static float ArmDrop(Animator animator)
        {
            var upper = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            var lower = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            return upper != null && lower != null ? Vector3.Angle(lower.position - upper.position, Vector3.down) : -1f;
        }

        /// <summary>
        /// The pose the last <see cref="Pose"/> set, written into the prefab: the pose handler moves the bones from
        /// native code, which a nested model's instance does not record, so the people stood in their T-pose in the scene
        /// (2026-11-10). The Animator drives the human bones in play; this is the pose the editor and the renders show.
        /// Returns the head bone's height over the root, a check on the stature.
        /// </summary>
        private static float Keep(GameObject model, Animator animator)
        {
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
            {
                if (PrefabUtility.IsPartOfPrefabInstance(t))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(t);
                }
            }

            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            return head != null ? model.transform.parent.InverseTransformPoint(head.position).y : 0f;
        }

        /// <summary>The model's Animator with its own Humanoid avatar.</summary>
        private static Animator Animated(GameObject model, Look look)
        {
            var animator = model.GetComponent<Animator>();
            if (animator == null)
            {
                animator = model.AddComponent<Animator>();
            }

            if (animator.avatar == null || !animator.avatar.isHuman)
            {
                animator.avatar = AssetDatabase.LoadAllAssetsAtPath(look.Model).OfType<Avatar>().First();
            }

            return animator;
        }

        /// <summary>The model in the clip's first pose: its height (unless kept), facing +Z, its feet on the root. Returns its stature.</summary>
        private static float Fit(GameObject model, AnimationClip clip, float height, bool keepScale)
        {
            var animator = model.GetComponent<Animator>();
            var root = model.transform.parent;
            model.transform.localPosition = Vector3.zero;
            if (!keepScale && PrefabUtility.GetCorrespondingObjectFromSource(model) is GameObject source)
            {
                model.transform.localScale = source.transform.localScale; // from the file's own size, never compounding a refit
            }

            Pose(animator, clip, 0f);
            if (!keepScale)
            {
                model.transform.localScale *= height / Stature(animator, root);
                Pose(animator, clip, 0f);
            }

            // The facing is the avatar's own, read in its neutral pose. A clip's first pose turns the shoulders with its
            // stride or stance, and that turn stayed in play: every walker went 13–30° off its way, the owner saw them walk
            // sideways (2026-11-12; ActivityPlayTests.MeasureTheWalkFacing).
            Neutral(animator);
            var l = root.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position);
            var r = root.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position);
            var facing = Vector3.Cross(Vector3.up, l - r);
            facing.y = 0f;
            model.transform.localRotation = Quaternion.AngleAxis(Vector3.SignedAngle(facing, Vector3.forward, Vector3.up), Vector3.up) * model.transform.localRotation;
            Pose(animator, clip, 0f);
            var seen = Seen(model, root);
            model.transform.localPosition -= new Vector3(seen.center.x, seen.min.y, seen.center.z);
            return Stature(animator, root);
        }

        /// <summary>The person's stature: from the eyes where the avatar maps them, else the mesh's height.</summary>
        private static float Stature(Animator animator, Transform root)
        {
            var seen = Seen(animator.gameObject, root);
            var left = animator.GetBoneTransform(HumanBodyBones.LeftEye);
            var right = animator.GetBoneTransform(HumanBodyBones.RightEye);
            if (left == null || right == null)
            {
                return seen.size.y;
            }

            var eyes = root.InverseTransformPoint((left.position + right.position) * 0.5f).y - seen.min.y;
            return eyes / EyeShare;
        }

        /// <summary>The avatar's neutral pose: every muscle at rest, the body upright and facing the animator's forward.</summary>
        private static void Neutral(Animator animator)
        {
            var handler = new HumanPoseHandler(animator.avatar, animator.transform);
            var pose = new HumanPose { bodyPosition = Vector3.up, bodyRotation = Quaternion.identity, muscles = new float[HumanTrait.MuscleCount] };
            handler.SetHumanPose(ref pose);
            handler.Dispose();
        }

        internal static void Pose(Animator animator, AnimationClip clip, float time)
        {
            var handler = new HumanPoseHandler(animator.avatar, animator.transform);
            var pose = SliceCharacterRigs.PoseAt(clip, time);
            handler.SetHumanPose(ref pose);
            handler.Dispose();
        }

        /// <summary>A townsperson's walking pace, the clip's own within the walkers' bounds (authoring default, 0.6–2 m/s).</summary>
        private static float Speed(Animator animator, AnimationClip walk, Transform root) => Mathf.Clamp(Pace(animator, walk, root), 0.6f, 2f);

        /// <summary>The clip is in place: the lower foot moves back as fast as the body would go forward (median over the clip).</summary>
        internal static float Pace(Animator animator, AnimationClip walk, Transform root) => LowerFootPace(animator, walk, root, Vector3.forward);

        /// <summary>
        /// The pace of a clip that moves the body along <paramref name="way"/>, in the body's own frame: the lower foot
        /// moves against it as fast as the body would go (median over the clip).
        /// </summary>
        internal static float Pace(Animator animator, AnimationClip walk, Transform root, Vector3 way)
        {
            var travel = Travel(animator, walk, root);
            return Vector2.Dot(travel, new Vector2(way.x, way.z).normalized);
        }

        /// <summary>
        /// Where an in-place clip would carry the body, x to the right and y forward (m/s): each foot counts only while it
        /// is planted, within 3 cm of its lowest point over the clip (authoring default), and the body goes against the
        /// planted feet's slide (median of each component).
        /// </summary>
        internal static Vector2 Travel(Animator animator, AnimationClip walk, Transform root)
        {
            const int samples = 120;
            const float planted = 0.03f;
            var feet = new[] { animator.GetBoneTransform(HumanBodyBones.LeftFoot), animator.GetBoneTransform(HumanBodyBones.RightFoot) };
            var dt = walk.length / samples;
            var track = new Vector3[2, samples + 1];
            for (var i = 0; i <= samples; i++)
            {
                Pose(animator, walk, i * dt);
                for (var f = 0; f < 2; f++)
                {
                    track[f, i] = root.InverseTransformPoint(feet[f].position);
                }
            }

            Pose(animator, walk, 0f);
            var xs = new List<float>();
            var zs = new List<float>();
            for (var f = 0; f < 2; f++)
            {
                var lowest = Enumerable.Range(0, samples + 1).Min(i => track[f, i].y);
                for (var i = 1; i <= samples; i++)
                {
                    if (track[f, i].y <= lowest + planted && track[f, i - 1].y <= lowest + planted)
                    {
                        xs.Add(-(track[f, i].x - track[f, i - 1].x) / dt);
                        zs.Add(-(track[f, i].z - track[f, i - 1].z) / dt);
                    }
                }
            }

            if (xs.Count == 0)
            {
                return Vector2.zero;
            }

            xs.Sort();
            zs.Sort();
            return new Vector2(xs[xs.Count / 2], zs[zs.Count / 2]);
        }

        /// <summary>The old lower-foot pace, kept for the walkers' own measure.</summary>
        private static float LowerFootPace(Animator animator, AnimationClip walk, Transform root, Vector3 way)
        {
            way = way.normalized;
            var left = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var right = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            var hips = animator.GetBoneTransform(HumanBodyBones.Hips);

            // The feet against the hips, so a clip whose curves carry the body along, its travel left out of the pose,
            // reads as the game shows it.
            Vector3 At(Transform foot)
            {
                var under = root.InverseTransformPoint(hips.position);
                under.y = 0f;
                return root.InverseTransformPoint(foot.position) - under;
            }

            const int samples = 120;
            var dt = walk.length / samples;
            var speeds = new List<float>();
            Pose(animator, walk, 0f);
            var before = (L: At(left), R: At(right));
            for (var i = 1; i <= samples; i++)
            {
                Pose(animator, walk, i * dt);
                var now = (L: At(left), R: At(right));
                var lower = now.L.y < now.R.y;
                speeds.Add(-Vector3.Dot(lower ? now.L - before.L : now.R - before.R, way) / dt);
                before = now;
            }

            Pose(animator, walk, 0f);
            speeds.Sort();
            return speeds[speeds.Count / 2];
        }

        /// <summary>The skinned mesh as posed, in <paramref name="space"/>.</summary>
        private static Bounds Seen(GameObject model, Transform space)
        {
            var min = Vector3.positiveInfinity;
            var max = Vector3.negativeInfinity;
            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = new Mesh();
                // The baked vertices leave out the model's own scale (measured 2026-11-10: a model scaled ×1.78 still
                // measured 1.00 m with a scale-free matrix, and the Townswoman's refit compounded to ×2.93), so the
                // renderer's whole matrix carries them into the world.
                smr.BakeMesh(mesh, true);
                var m = smr.transform.localToWorldMatrix;
                foreach (var v in mesh.vertices)
                {
                    var w = space.InverseTransformPoint(m.MultiplyPoint3x4(v));
                    min = Vector3.Min(min, w);
                    max = Vector3.Max(max, w);
                }

                Object.DestroyImmediate(mesh);
            }

            var b = new Bounds();
            b.SetMinMax(min, max);
            return b;
        }

        // ------------------------------------------------------------------------------------------- scenes

        private static void Scenes(Dictionary<string, float> speeds, StringBuilder report)
        {
            var layout = VeilmouthLayout.Load();
            var paths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes/Slice" }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p != VeilmouthDwellings.ScenePath).OrderBy(p => p).ToList();
            foreach (var path in paths)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var lines = new List<string>();
                foreach (var go in Roots().ToList())
                {
                    var source = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                    var dummy = source.Contains("Human Character Dummy");
                    var female = dummy ? source.Contains("HumanDummy_F") : NewWalkers.Any(l => l.Female && l.Walking == source);
                    var walker = go.GetComponent<TownsfolkWalker>();
                    Look look;
                    string prefab;
                    if (dummy)
                    {
                        look = LookFor(go.name, female);
                        prefab = look.Standing;
                    }
                    else if (walker != null && go.name != "Farmer" && walker.Route.Length > 0)
                    {
                        look = WalkerLook(female, go.transform.position, layout);
                        prefab = look.Walking;
                        if (prefab == source)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }

                    Replace(go, AssetDatabase.LoadAssetAtPath<GameObject>(prefab));
                    lines.Add($"{go.name}: {Path.GetFileNameWithoutExtension(source)} to {look.Name}{(walker != null ? " (walking)" : string.Empty)}");
                }

                Paces(speeds, lines);
                if (path == VeilmouthScene.ScenePath)
                {
                    PlaceBlacksmith(lines);
                    Reroute(lines);
                }

                if (lines.Count > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    report.AppendLine($"{path}:\n  " + string.Join("\n  ", lines));
                }
            }
        }

        /// <summary>Every walker in the open scene at its prefab's measured pace, the refitted townsfolk's among them.</summary>
        private static void Paces(Dictionary<string, float> speeds, List<string> lines)
        {
            foreach (var w in Object.FindObjectsByType<TownsfolkWalker>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var source = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(w.gameObject);
                if (speeds.TryGetValue(source, out var speed) && Mathf.Abs(w.Speed - speed) > 0.001f)
                {
                    w.Configure(w.Route, speed, SliceTownsfolk.GroundMask);
                    EditorUtility.SetDirty(w);
                    lines.Add($"{w.name}: walking {speed:0.00} m/s");
                }
            }
        }

        /// <summary>
        /// Batch, after the owner's play test of 2026-11-12 (the walkers went sideways): every person's model and both
        /// player bodies turned to the avatar's own forward by <see cref="Fit"/>, the walkers' paces measured again, and
        /// each walker in the slice's scenes set to its prefab's new pace; nothing else in a scene changes.
        /// -executeMethod GameDev.Slice.EditorTools.SlicePeopleModels.FaceForward
        /// </summary>
        public static void FaceForward()
        {
            var report = new StringBuilder();
            var speeds = new Dictionary<string, float>();
            foreach (var look in NewWalkers)
            {
                speeds[look.Walking] = Refit(look.Walking, look, false, report);
            }

            foreach (var look in All)
            {
                Turn(look.Standing, look, SliceClips.Get(IdleUse(look), look.Female), report);
            }

            Turn(SliceTestScenes.PlayerPrefabPath, PlayerMan, SliceClips.Get("Idle"), report);
            Turn(PlayerWomanPrefab, PlayerWoman, SliceClips.Get("Idle", true), report);

            foreach (var path in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes/Slice" }).Select(AssetDatabase.GUIDToAssetPath)
                         .Where(p => p != VeilmouthDwellings.ScenePath).OrderBy(p => p))
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var lines = new List<string>();
                Paces(speeds, lines);
                if (lines.Count > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    report.AppendLine($"{path}: {lines.Count} walkers at their new pace");
                }
            }

            Directory.CreateDirectory(OutputFolder);
            File.WriteAllText($"{OutputFolder}/people_facing.txt", report.ToString());
            Debug.Log("[SlicePeopleModels] facing:\n" + report);
        }

        /// <summary>A standing person's or the player's prefab with its model turned to the avatar's forward, its size kept.</summary>
        private static void Turn(string prefabPath, Look look, AnimationClip clip, StringBuilder report)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            var model = root.transform.Find("Model");
            var before = Mathf.DeltaAngle(0f, model.localEulerAngles.y);
            Fit(model.gameObject, clip, look.Height, true);
            Keep(model.gameObject, model.GetComponent<Animator>());
            var after = Mathf.DeltaAngle(0f, model.localEulerAngles.y);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            report.AppendLine($"{prefabPath}: the model turned from {before:+0.0;-0.0} to {after:+0.0;-0.0} deg");
        }

        /// <summary>Every person's prefab instance in the open scene: the Dummies and the townsfolk's prefabs.</summary>
        private static IEnumerable<GameObject> Roots() =>
            Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(t => t.gameObject)
                .Where(g => PrefabUtility.IsOutermostPrefabInstanceRoot(g))
                .Where(g =>
                {
                    var p = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g);
                    return p.Contains("Human Character Dummy") || NewWalkers.Any(l => l.Walking == p);
                });

        /// <summary>
        /// The instance takes the new prefab in place: its object stays, with its name, place, turn and added components
        /// (NamedPerson, PersonTalk); the Dummy's own collider, added at its dressing, goes, as the new prefab has its own,
        /// and so does its scale, which sized the Dummy.
        /// </summary>
        private static void Replace(GameObject go, GameObject prefab)
        {
            var settings = new PrefabReplacingSettings
            {
                changeRootNameToAssetName = false,
                objectMatchMode = ObjectMatchMode.ByHierarchy,
                prefabOverridesOptions = PrefabOverridesOptions.KeepAllPossibleOverrides,
            };
            PrefabUtility.ReplacePrefabAssetOfPrefabInstance(go, prefab, settings, InteractionMode.AutomatedAction);
            foreach (var c in go.GetComponents<Collider>().Where(c => PrefabUtility.IsAddedComponentOverride(c)).ToList())
            {
                Object.DestroyImmediate(c);
            }

            go.transform.localScale = Vector3.one;
            var scale = new SerializedObject(go.transform).FindProperty("m_LocalScale");
            if (scale.prefabOverride)
            {
                PrefabUtility.RevertPropertyOverride(scale, InteractionMode.AutomatedAction);
            }
        }

        /// <summary>
        /// The blacksmith at Bawling Row's anvil (owner, 2026-11-10), beside Edda Brann's Item Creation stall: on the
        /// first side of the anvil with ground and room for a person, behind it toward the plinth first, facing it.
        /// </summary>
        internal static void PlaceBlacksmith(List<string> lines)
        {
            if (Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(t => t.name == "Blacksmith"
                    && PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject) && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) == Blacksmith.Standing))
            {
                return; // placed by an earlier run
            }

            var edda = Object.FindObjectsByType<NamedPerson>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(p => p.Id == "edda");
            var anvil = Object.FindObjectsByType<KitBlock>(FindObjectsSortMode.None).Where(b => b.name.StartsWith("Kit_Market_Anvil"))
                .OrderBy(b => edda == null ? 0f : (b.transform.position - edda.transform.position).sqrMagnitude).FirstOrDefault();
            if (anvil == null)
            {
                lines.Add("Blacksmith: NO ANVIL found; not placed");
                return;
            }

            Physics.SyncTransforms();
            var b = anvil.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((p, q) => { p.Encapsulate(q); return p; });
            var ground = LayerMask.GetMask(KitLayers.Ground, KitLayers.Structure);
            var things = ~LayerMask.GetMask(KitLayers.Ground, KitLayers.Character);
            foreach (var side in new[] { Vector3.right, Vector3.forward, Vector3.back, Vector3.left })
            {
                var at = b.center + Vector3.Scale(side, b.extents) + (side * 0.55f);
                if (!Physics.Raycast(new Vector3(at.x, b.max.y + 3f, at.z), Vector3.down, out var hit, 10f, ground, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                var feet = hit.point;
                if (Physics.CheckCapsule(feet + (Vector3.up * 0.45f), feet + (Vector3.up * 1.6f), 0.28f, things, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Blacksmith.Standing),
                    edda != null ? edda.transform.parent : anvil.transform.parent);
                go.name = "Blacksmith";
                go.transform.SetPositionAndRotation(feet, Quaternion.LookRotation(-side));
                lines.Add($"Blacksmith: at {feet} on the anvil's {side} side, facing it");
                return;
            }

            lines.Add("Blacksmith: NO ROOM round the anvil; not placed");
        }

        /// <summary>A walker whose route now meets the bronze statue or a street lamp, this pass's new pieces, walks a route planned again.</summary>
        private static void Reroute(List<string> lines)
        {
            Physics.SyncTransforms();
            var mask = LayerMask.GetMask("Default", KitLayers.Structure, KitLayers.Prop, KitLayers.Vegetation, KitLayers.Interactable);
            foreach (var w in Object.FindObjectsByType<TownsfolkWalker>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var hit = Meets(w.Route, mask);
                if (hit == null)
                {
                    continue;
                }

                // From where it stands, else from a point of its old route, else from a cell or two aside: the first route
                // planned that meets no new lamp or statue.
                var starts = new[] { w.transform.position }.Concat(w.Route)
                    .Concat(new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back }.SelectMany(d => new[] { w.transform.position + d, w.transform.position + (d * 2f) }));
                var route = starts.Select(SliceTownsfolk.PlanRoute).FirstOrDefault(r => r.Length >= 3 && Meets(r, mask) == null) ?? new Vector3[0];
                if (route.Length < 3)
                {
                    lines.Add($"{w.name} at {w.transform.position}: its route meets {hit}; NO NEW ROUTE");
                    continue;
                }

                w.Configure(route, w.Speed, SliceTownsfolk.GroundMask);
                w.transform.position = route[0];
                EditorUtility.SetDirty(w);
                lines.Add($"{w.name} at {route[0]}: its route met {hit}; planned again, {w.Length:0} m{(Meets(route, mask) is { } still ? $", STILL MEETS {still}" : string.Empty)}");
            }
        }

        private static string Meets(Vector3[] route, int mask)
        {
            if (route == null || route.Length < 2)
            {
                return null;
            }

            for (var i = 0; i < route.Length; i++)
            {
                var a = route[i];
                var d = route[(i + 1) % route.Length] - a;
                if (d.magnitude < 0.01f)
                {
                    continue;
                }

                foreach (var hit in Physics.CapsuleCastAll(a + (Vector3.up * 0.6f), a + (Vector3.up * 1.5f), 0.25f, d.normalized, d.magnitude, mask, QueryTriggerInteraction.Ignore))
                {
                    var block = hit.collider.GetComponentInParent<KitBlock>();
                    if (block != null && (block.name.StartsWith("Kit_Street_Lamp") || block.name.StartsWith("Kit_Street_BronzeStatue")))
                    {
                        return block.name;
                    }
                }
            }

            return null;
        }

        /// <summary>The development opponent wears the Cult Forces' model (asset map §1).</summary>
        private static void Opponent()
        {
            var settings = AssetDatabase.LoadAssetAtPath<SliceSettings>(SliceConfrontSetup.SettingsPath);
            var so = new SerializedObject(settings);
            so.FindProperty("opponentModel").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Detective.Standing);
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------------------------------- the female Warden and the player

        /// <summary>
        /// The owner's second models of 2026-11-10: the female Warden's prefab, C5's two female Wardens in it, and the
        /// player's two bodies. Batch: -executeMethod GameDev.Slice.EditorTools.SlicePeopleModels.WardensAndPlayer
        /// </summary>
        public static void WardensAndPlayer()
        {
            var report = new StringBuilder();
            Build(WardenWoman, false, null, report);
            FemaleWardens(report);
            WearPlayer(SliceTestScenes.PlayerPrefabPath, PlayerMan, report);
            WearPlayer(PlayerWomanPrefab, PlayerWoman, report);
            Directory.CreateDirectory(OutputFolder);
            File.WriteAllText($"{OutputFolder}/people_wardens_player.txt", report.ToString());
            Debug.Log("[SlicePeopleModels]\n" + report);
            CheckSheet();
            PlayerSheet();
            RenderPeople();
        }

        /// <summary>
        /// The Wardens placed as women, who wore the Officer until the owner's female Warden came: C5's at the Black
        /// Puddle (VeilmouthCity.Districts) and the lock-up's in its office (VeilmouthInteriors.Lockup). A rebuild places
        /// them through <see cref="LookFor"/>, which now gives a woman the female Warden.
        /// </summary>
        private static void FemaleWardens(StringBuilder report)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WardenWoman.Standing);
            foreach (var (scenePath, name, at) in new[]
                     {
                         (VeilmouthScene.ScenePath, "Warden at the Black Puddle", (Vector2?)null),
                         (VeilmouthDressing.LockupScenePath, "Warden", new Vector2(18.3f, 15.5f)),
                     })
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var wardens = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(t => t.name == name && PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)
                                && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) == Officer.Standing)
                    .Where(t => at == null || Vector2.Distance(new Vector2(t.localPosition.x, t.localPosition.z), at.Value) < 0.6f)
                    .ToList();
                foreach (var t in wardens)
                {
                    Replace(t.gameObject, prefab);
                }

                report.AppendLine($"{scenePath}: {name}: {wardens.Count} to {WardenWoman.Name}");
                if (wardens.Count > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }
        }

        /// <summary>The player prefab at <paramref name="path"/> in <paramref name="look"/>'s body; the woman's is made from the man's.</summary>
        private static void WearPlayer(string path, Look look, StringBuilder report)
        {
            var root = PrefabUtility.LoadPrefabContents(SliceTestScenes.PlayerPrefabPath);
            report.AppendLine($"{path}: {Wear(root, look)}");
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        /// <summary>
        /// <paramref name="look"/>'s body under the player's <paramref name="root"/>, named Model in place of the old one:
        /// at its stature, in the idle's first pose, playing the player's controller, the same for both bodies. The root
        /// and its components stay. Returns a line for the report.
        /// </summary>
        internal static string Wear(GameObject root, Look look)
        {
            var old = root.transform.Find("Model");
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(look.Model), root.transform);
            model.name = "Model";
            model.transform.SetSiblingIndex(0);
            SliceKitCatalogue.LinkTripoTextures(model, look.Model, look.File);
            var animator = Animated(model, look);
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(look.Female ? WomanPlayerController : PlayerController);
            animator.applyRootMotion = false;
            var stature = Fit(model, SliceClips.Get("Idle", look.Female), look.Height, false);
            var head = Keep(model, animator);
            SliceKitCatalogue.SetLayer(model, KitLayers.Character);
            return $"{look.File}, {stature:0.00} m (head bone at {head:0.00} m), {animator.runtimeAnimatorController.name}, upper arm {ArmDrop(animator):0}° from down";
        }

        // ------------------------------------------------------------------------------------------- checks

        /// <summary>The player's two prefabs standing in their idle and stepping in their walk, front and side (`people_player.png`).</summary>
        private static void PlayerSheet()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var light = new GameObject("Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
            const int tile = 256;
            var players = new[] { (SliceTestScenes.PlayerPrefabPath, PlayerMan), (PlayerWomanPrefab, PlayerWoman) };
            var sheet = new Texture2D(4 * tile, players.Length * tile, TextureFormat.RGB24, false);
            for (var row = 0; row < players.Length; row++)
            {
                var (path, look) = players[row];
                var column = 0;
                foreach (var (clip, at) in new[] { (SliceClips.Get("Idle", look.Female), 0.5f), (SliceClips.Get("Walk", look.Female), 0.25f) })
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                    Pose(go.GetComponentInChildren<Animator>(), clip, clip.length * at);
                    SliceCharacterRigs.Draw(go, sheet, row, ref column, tile, players.Length);
                    Object.DestroyImmediate(go);
                }
            }

            sheet.Apply();
            File.WriteAllBytes($"{OutputFolder}/people_player.png", sheet.EncodeToPNG());
        }

        /// <summary>Every look standing in its idle and stepping in its walk, front and side (`people_check.png`).</summary>
        private static void CheckSheet()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var light = new GameObject("Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
            const int tile = 256;
            var sheet = new Texture2D(4 * tile, All.Length * tile, TextureFormat.RGB24, false);
            for (var row = 0; row < All.Length; row++)
            {
                var look = All[row];
                var column = 0;
                foreach (var (clip, at) in new[] { (SliceClips.Get(IdleUse(look), look.Female), 0.5f), (SliceClips.Get("Walk", look.Female), 0.25f) })
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(look.Standing));
                    Pose(go.GetComponentInChildren<Animator>(), clip, clip.length * at);
                    SliceCharacterRigs.Draw(go, sheet, row, ref column, tile, All.Length);
                    Object.DestroyImmediate(go);
                }
            }

            sheet.Apply();
            File.WriteAllBytes($"{OutputFolder}/people_check.png", sheet.EncodeToPNG());
        }

        /// <summary>Veilmouth's people where the pass changed them: the blacksmith and Edda, the Wardens, the market, a lady and a gentleman walking.</summary>
        private static void RenderPeople()
        {
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var people = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var shots = new[] { "Blacksmith", "Warden at the station", "Warden at the Black Puddle", "Tobias Penn", "Madame Ilse", "Drunkard" }
                .Select(n => people.FirstOrDefault(t => t.name == n)).Where(t => t != null).ToList();
            foreach (var path in new[] { Gentleman.Walking, Lady.Walking, WorkingWoman.Walking })
            {
                var one = people.FirstOrDefault(t => PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject) && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) == path);
                if (one != null)
                {
                    shots.Add(one);
                }
            }

            var i = 0;
            foreach (var t in shots)
            {
                Debug.Log($"[SlicePeopleModels] {t.name} in the scene: upper arm {ArmDrop(t.GetComponentInChildren<Animator>()):0}° from down");
                var at = t.position + (Vector3.up * 1.1f);
                VeilmouthRenders.Eye($"people_{i++:00}_{t.name.Replace(' ', '_')}", at + (t.forward * 3.4f) + (t.right * 1.2f) + (Vector3.up * 0.3f), at, 50f);
            }
        }
    }
}
