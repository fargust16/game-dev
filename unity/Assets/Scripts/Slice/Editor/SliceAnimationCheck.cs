using System.IO;
using System.Linq;
using System.Text;
using GameDev.Slice.Fights;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// A diagnostic of the people's animation (ANIMATION_PIPELINE_PLAN.md §7; owner's play test of 2026-11-11: the feet
    /// float, the knees and ankles bend wrongly). For each avatar it reports its rest pose: the arm's drop from straight
    /// down (about 90° in a T-pose), the knees' angle (180° straight) and the feet's pitch. Then it plays the player's
    /// controller on the player's prefab, as the game does, and reports the feet's height over the prefab's root and
    /// the knees' angle in the idle, the walk and the run. Writes <c>Logs/SliceSurvey/animation_check.txt</c>.
    /// Batch: -executeMethod GameDev.Slice.EditorTools.SliceAnimationCheck.Run
    /// </summary>
    public static class SliceAnimationCheck
    {
        private const string Report = "Logs/SliceSurvey/animation_check.txt";

        public static void Run()
        {
            var report = new StringBuilder();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            report.AppendLine("Rest poses (arm drop from down, 90 = T-pose; knee angle, 180 = straight; foot pitch, toes below ankle +):");
            foreach (var path in new[] { SlicePeopleModels.PlayerWoman.Model, SlicePeopleModels.PlayerMan.Model, SliceClips.PathOf("Idle"), SliceClips.PathOf("Walk"), SliceClips.PathOf("Run"), SliceClips.Q1, SliceClips.Q2 })
            {
                report.AppendLine($"  {Path.GetFileNameWithoutExtension(path)}: {Rest(path)}");
            }

            // The clip's first frame retargeted onto the player woman: the knees and the feet as the clip bends them.
            report.AppendLine();
            report.AppendLine("First frame retargeted onto the player woman: knees and feet pitch:");
            foreach (var use in new[] { "Idle", "Walk", "Run" })
            {
                var animator = SlicePeopleModels.Fitted(SlicePeopleModels.PlayerWoman, SliceClips.Get(use, true));
                SlicePeopleModels.Pose(animator, SliceClips.Get(use, true), 0f);
                report.AppendLine($"  {use}: knees {Knee(animator, true):0} / {Knee(animator, false):0}, feet pitch {Pitch(animator, true):0} / {Pitch(animator, false):0}");
                Object.DestroyImmediate(animator.transform.parent.gameObject);
            }

            // AN4: FightBeats' tables. A blow's contact: the frame the striking hand, the left for the other hand's blow, is
            // furthest out ahead of the chest; a shot's: the frame the hand jumps back fastest (the recoil). A shot's beat holds
            // for the rest of the shot and the whole load. A hit's onset: the first frame the head moves faster than 0.6 m/s.
            // The Block clip's height: the frame the right hand is highest over the chest.
            report.AppendLine();
            report.AppendLine("Blow contact and hit onset, measured on the player man (seconds into each clip), FightBeats' tables (AN4):");
            var contact = new float[FightBeats.Blows.Length];
            var hold = new float[FightBeats.Blows.Length];
            for (var i = 0; i < FightBeats.Blows.Length; i++)
            {
                var use = FightBeats.Blows[i];
                var clip = SliceClips.Get(use);
                contact[i] = Measure(clip, use.EndsWith("Shot") ? Reading.Recoil : use.EndsWith("Left") ? Reading.LeftReach : Reading.Reach);
                var load = FightBeats.Loads[i] != null ? SliceClips.Get(FightBeats.Loads[i]) : null;
                hold[i] = load != null ? clip.length - contact[i] + load.length : 0f;
                report.AppendLine($"  {i + 1} {use}: {clip.length:0.00} s, contact at {contact[i]:0.00} s" +
                                  (load != null ? $", then {FightBeats.Loads[i]} {load.length:0.00} s: the beat holds {hold[i]:0.00} s" : string.Empty));
            }

            var onset = new float[FightBeats.Hits.Length];
            for (var i = 0; i < FightBeats.Hits.Length; i++)
            {
                var clip = SliceClips.Get(FightBeats.Hits[i]);
                onset[i] = FightBeats.Hits[i] == "Block" ? 0f : Measure(clip, Reading.Onset);
                report.AppendLine($"  hit {i + 1} {FightBeats.Hits[i]}: {clip.length:0.00} s" + (FightBeats.Hits[i] == "Block" ? ", raised before the blow" : $", the head starts to move at {onset[i]:0.00} s"));
            }

            var block = Measure(SliceClips.Get("Block"), Reading.Height);
            var knock = Measure(SliceClips.Get("Knockback"), Reading.Onset);
            report.AppendLine($"  Block: the right hand highest over the chest at {block:0.00} s; Knockback {SliceClips.Get("Knockback").length:0.00} s, the head starts to move at {knock:0.00} s; GetUp {SliceClips.Get("GetUp").length:0.00} s");
            report.AppendLine($"  Contact = {{ {string.Join(", ", contact.Select(v => $"{v:0.00}f"))} }}");
            report.AppendLine($"  ShotHold = {{ {string.Join(", ", hold.Select(v => $"{v:0.00}f"))} }}");
            report.AppendLine($"  Onset = {{ {string.Join(", ", onset.Select(v => $"{Mathf.Max(0f, v - 0.03f):0.00}f"))} }} (entered 0.03 s before)");
            report.AppendLine($"  BlockPeak = {block:0.00}f, KnockOnset = {Mathf.Max(0f, knock - 0.03f):0.00}f");

            // AN5: how high the swimming clips hold the chest and the head over the root, on the player man fitted standing,
            // so PlayerLocomotion floats the root that far under the water and the head stays out of it.
            report.AppendLine();
            report.AppendLine("Swimming, the player man fitted standing (m over the root, mean and range over the clip; AN5):");
            foreach (var use in new[] { "Swim", "SwimIdle", "ClimbOut" })
            {
                var clip = SliceClips.Get(use);
                var animator = SlicePeopleModels.Fitted(SlicePeopleModels.PlayerMan, SliceClips.Get("Idle"));
                var root = animator.transform.parent;
                var frames = Mathf.Max(1, Mathf.RoundToInt(clip.length * clip.frameRate));
                float chestSum = 0f, headLow = float.MaxValue, headHigh = float.MinValue, chestLow = float.MaxValue, chestHigh = float.MinValue;
                for (var f = 0; f <= frames; f++)
                {
                    SlicePeopleModels.Pose(animator, clip, clip.length * f / frames);
                    var chest = root.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.Chest).position).y;
                    var head = root.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.Head).position).y;
                    chestSum += chest;
                    chestLow = Mathf.Min(chestLow, chest);
                    chestHigh = Mathf.Max(chestHigh, chest);
                    headLow = Mathf.Min(headLow, head);
                    headHigh = Mathf.Max(headHigh, head);
                }

                Object.DestroyImmediate(root.gameObject);
                report.AppendLine($"  {use}: {clip.length:0.00} s, the chest at {chestSum / (frames + 1):0.00} m ({chestLow:0.00}–{chestHigh:0.00}), the head {headLow:0.00}–{headHigh:0.00} m");
            }

            report.AppendLine();
            report.AppendLine("The player's prefab playing its controller (feet = the lower ankle's height over the root, minus its rest height; knees in degrees):");
            foreach (var prefab in new[] { SliceTestScenes.PlayerPrefabPath, SlicePeopleModels.PlayerWomanPrefab })
            {
                var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefab));
                var animator = player.GetComponentInChildren<Animator>();
                var cc = player.GetComponent<CharacterController>();
                report.AppendLine($"  {Path.GetFileNameWithoutExtension(prefab)}: capsule bottom {(cc.center.y - (cc.height / 2f)):0.000} m over the root, skin {cc.skinWidth:0.000} m; " +
                                  $"avatar {animator.avatar?.name}, model at {player.transform.InverseTransformPoint(animator.transform.position).y:0.000} m");
                var rest = AnkleRest(animator, player.transform);
                foreach (var speed in new[] { 0f, 1f, 2f, 3f })
                {
                    animator.Rebind();
                    animator.SetFloat("Speed", speed);
                    animator.SetFloat("MoveZ", speed); // the eight-way blend, straight ahead
                    animator.Update(0f);
                    float low = float.MaxValue, high = float.MinValue, kneeMin = 999f;
                    for (var i = 0; i < 60; i++)
                    {
                        animator.Update(1f / 30f);
                        var feet = Mathf.Min(Up(animator, player.transform, HumanBodyBones.LeftFoot), Up(animator, player.transform, HumanBodyBones.RightFoot)) - rest;
                        low = Mathf.Min(low, feet);
                        high = Mathf.Max(high, feet);
                        kneeMin = Mathf.Min(kneeMin, Knee(animator, true), Knee(animator, false));
                    }

                    report.AppendLine($"    Speed {speed}: lower ankle {low:+0.000;-0.000} to {high:+0.000;-0.000} m from its rest height; the knees bend to {kneeMin:0} degrees");
                }

                Object.DestroyImmediate(player);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Report) ?? ".");
            File.WriteAllText(Report, report.ToString());
            Debug.Log($"[SliceAnimationCheck] written to {Report}\n{report}");
        }

        private enum Reading
        {
            Reach,
            LeftReach,
            Recoil,
            Onset,
            Height,
        }

        /// <summary>A moment of a clip on the player man, s, as <paramref name="reading"/> reads it.</summary>
        private static float Measure(AnimationClip clip, Reading reading)
        {
            var animator = SlicePeopleModels.Fitted(SlicePeopleModels.PlayerMan, clip);
            var root = animator.transform.parent;
            var frames = Mathf.RoundToInt(clip.length * clip.frameRate);
            var dt = 1f / clip.frameRate;
            float best = float.MinValue, at = 0f;
            var onset = -1f;
            Vector3 lastHand = default, lastHead = default;
            for (var f = 0; f <= frames; f++)
            {
                SlicePeopleModels.Pose(animator, clip, f * dt);
                var chest = root.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.Chest).position);
                var hand = root.InverseTransformPoint(animator.GetBoneTransform(reading == Reading.LeftReach ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand).position);
                var head = root.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.Head).position);
                var value = float.MinValue;
                switch (reading)
                {
                    case Reading.Reach:
                    case Reading.LeftReach:
                        value = new Vector2(hand.x - chest.x, hand.z - chest.z).magnitude + Mathf.Max(0f, hand.z - chest.z);
                        break;
                    case Reading.Recoil:
                        value = f > 0 ? (lastHand.z - hand.z) / dt : float.MinValue;
                        break;
                    case Reading.Height:
                        value = hand.y - chest.y;
                        break;
                    case Reading.Onset:
                        if (f > 0 && onset < 0f && (head - lastHead).magnitude / dt > 0.6f)
                        {
                            onset = f * dt;
                        }

                        break;
                }

                if (value > best)
                {
                    best = value;
                    at = f * dt;
                }

                lastHand = hand;
                lastHead = head;
            }

            Object.DestroyImmediate(root.gameObject);
            return reading == Reading.Onset ? Mathf.Max(0f, onset) : at;
        }

        private static string Rest(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            var avatar = importer?.avatarSetup == ModelImporterAvatarSetup.CopyFromOther ? importer.sourceAvatar
                : AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            if (asset == null || avatar == null || !avatar.isHuman)
            {
                return "no human avatar";
            }

            var copy = (GameObject)Object.Instantiate(asset);
            if (!copy.TryGetComponent<Animator>(out var animator))
            {
                animator = copy.AddComponent<Animator>();
            }

            animator.avatar = avatar;
            var arm = SlicePeopleModels.ArmDrop(animator);
            var line = $"arm {arm:0}, knees {Knee(animator, true):0} / {Knee(animator, false):0}, feet pitch {Pitch(animator, true):0} / {Pitch(animator, false):0}; " +
                       $"source {importer?.avatarSetup}{(importer?.avatarSetup == ModelImporterAvatarSetup.CopyFromOther ? $" ({avatar.name})" : string.Empty)}";
            Object.DestroyImmediate(copy);
            return line;
        }

        private static float Knee(Animator a, bool left)
        {
            var hip = a.GetBoneTransform(left ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg);
            var knee = a.GetBoneTransform(left ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg);
            var ankle = a.GetBoneTransform(left ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot);
            return Vector3.Angle(hip.position - knee.position, ankle.position - knee.position);
        }

        private static float Pitch(Animator a, bool left)
        {
            var ankle = a.GetBoneTransform(left ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot);
            var toes = a.GetBoneTransform(left ? HumanBodyBones.LeftToes : HumanBodyBones.RightToes);
            if (toes == null)
            {
                return float.NaN;
            }

            var d = toes.position - ankle.position;
            return Mathf.Atan2(-d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
        }

        private static float Up(Animator a, Transform root, HumanBodyBones bone) => root.InverseTransformPoint(a.GetBoneTransform(bone).position).y;

        /// <summary>The lower ankle's height over the root in the model's own rest pose, scaled as the prefab has it.</summary>
        private static float AnkleRest(Animator animator, Transform root)
        {
            animator.Rebind();
            animator.enabled = false;
            var handler = new HumanPoseHandler(animator.avatar, animator.transform);
            var pose = new HumanPose();
            handler.GetHumanPose(ref pose);
            handler.Dispose();
            // The rest height of the ankle over the sole: the ankle's height over the lowest vertex of the skin.
            var lowest = animator.GetComponentsInChildren<SkinnedMeshRenderer>().Select(r =>
            {
                var mesh = new Mesh();
                r.BakeMesh(mesh, false);
                var min = mesh.vertices.Select(v => root.InverseTransformPoint(r.transform.TransformPoint(v)).y).DefaultIfEmpty(0f).Min();
                Object.DestroyImmediate(mesh);
                return min;
            }).DefaultIfEmpty(0f).Min();
            var ankle = Mathf.Min(Up(animator, root, HumanBodyBones.LeftFoot), Up(animator, root, HumanBodyBones.RightFoot));
            animator.enabled = true;
            return ankle - lowest;
        }
    }
}
