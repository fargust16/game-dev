using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// A measurement of every clip of <see cref="SliceClips"/> posed on the player man (ANIMATION_PIPELINE_PLAN.md §8): its
    /// length; the right hand's lowest point and its furthest reach ahead of the chest, each with its time; the hips'
    /// lowest height and the moment they start to rise again (the last frame within <see cref="Rise"/> of their lowest).
    /// The key frames of the work clips and the blows are read from it. Writes <c>Logs/SliceSurvey/clip_survey.txt</c>.
    /// Batch: -executeMethod GameDev.Slice.EditorTools.SliceClipSurvey.Run
    /// </summary>
    public static class SliceClipSurvey
    {
        private const string Report = "Logs/SliceSurvey/clip_survey.txt";

        /// <summary>Authoring default, m: the hips this close to their lowest are still down.</summary>
        public const float Rise = 0.02f;

        public static void Run()
        {
            var report = new StringBuilder();
            SliceClips.Import(report);
            report.Clear();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            report.AppendLine("Use: length; right hand lowest (m over the soles) at s; right hand furthest ahead of the chest (m) at s; hips lowest (m) at s, rising from s; hips standing (m)");
            foreach (var e in SliceClips.Clips)
            {
                report.AppendLine(Measure(e.Use));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Report) ?? ".");
            File.WriteAllText(Report, report.ToString());
            Debug.Log($"[SliceClipSurvey] written to {Report}\n{report}");
        }

        public static string Measure(string use)
        {
            var clip = SliceClips.Get(use);
            var animator = SlicePeopleModels.Fitted(SlicePeopleModels.PlayerMan, clip);
            var root = animator.transform.parent;
            var frames = Mathf.Max(1, Mathf.RoundToInt(clip.length * clip.frameRate));
            var dt = clip.length / frames;
            float handLow = float.MaxValue, handLowAt = 0f, reach = float.MinValue, reachAt = 0f, hipsLow = float.MaxValue, hipsLowAt = 0f, hipsHigh = float.MinValue;
            float leftReach = float.MinValue, leftReachAt = 0f, handHigh = float.MinValue, handHighAt = 0f;
            SlicePeopleModels.Pose(animator, clip, 0f);
            // A bone's own axes are the rig's, so the body's way is read across the thighs: right thigh minus left, crossed with up.
            var across = root.InverseTransformDirection(animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).position
                - animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position);
            var facing = Vector3.Cross(across, Vector3.up);
            facing.y = 0f;
            facing = facing.sqrMagnitude > 1e-6f ? facing.normalized : Vector3.forward;
            var face = Mathf.Atan2(facing.x, facing.z) * Mathf.Rad2Deg;
            var hips = new float[frames + 1];
            for (var f = 0; f <= frames; f++)
            {
                SlicePeopleModels.Pose(animator, clip, f * dt);
                var hand = root.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.RightHand).position);
                var chest = root.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.Chest).position);
                hips[f] = root.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.Hips).position).y;
                if (hand.y < handLow)
                {
                    handLow = hand.y;
                    handLowAt = f * dt;
                }

                // Ahead along the way the body faced at the start, so a clip authored facing aside still reads.
                var ahead = Vector3.Dot(hand - chest, facing);
                if (ahead > reach)
                {
                    reach = ahead;
                    reachAt = f * dt;
                }

                // The left hand ahead, and either hand's highest over the chest: some clips work with the left hand or at the face.
                var left = root.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.LeftHand).position);
                var leftAhead = Vector3.Dot(left - chest, facing);
                if (leftAhead > leftReach)
                {
                    leftReach = leftAhead;
                    leftReachAt = f * dt;
                }

                var high = Mathf.Max(hand.y, left.y) - chest.y;
                if (high > handHigh)
                {
                    handHigh = high;
                    handHighAt = f * dt;
                }

                if (hips[f] < hipsLow)
                {
                    hipsLow = hips[f];
                    hipsLowAt = f * dt;
                }

                hipsHigh = Mathf.Max(hipsHigh, hips[f]);
            }

            var low = Mathf.RoundToInt(hipsLowAt / dt);
            var rising = low;
            while (rising < frames && hips[rising + 1] <= hipsLow + Rise)
            {
                rising++;
            }

            Object.DestroyImmediate(root.gameObject);
            return $"{use}: {clip.length:0.00} s{(clip.isLooping ? " loop" : string.Empty)}; hand low {handLow:0.00} at {handLowAt:0.00}; " +
                   $"reach {reach:0.00} at {reachAt:0.00}; hips low {hipsLow:0.00} at {hipsLowAt:0.00}, rising from {rising * dt:0.00}; hips high {hipsHigh:0.00}; " +
                   $"left reach {leftReach:0.00} at {leftReachAt:0.00}; hand over chest {handHigh:0.00} at {handHighAt:0.00}; faces {face:0} deg";
        }
    }
}
