using System.Collections.Generic;
using System.Linq;
using GameDev.Slice.Atmosphere;
using GameDev.Slice.Kit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Stone Grandad's figure as a veiled saint (VEILMOUTH_CITY_PLAN.md §37; the veiled statues its skyline waits for).
    /// The plinth and the pedestal stay §28.9's, with their colliders, until the patron saint's model comes
    /// (SLICE_MODELS_WANTED.md § C4). Over them, in the place of the plain 2 m box, stands a robed figure in dark stone:
    /// a robe widening to its hem, sloped shoulders, a pointed hood with no face in it, and an iron staff in its right
    /// hand with a lantern at its head, all facing west down Long Tongue toward the gate. The lantern burns at night
    /// with the gas lamps, as the One-Eye's does (§38), and is no street lamp. One merged mesh for each material with
    /// no collider: the figure stands 5 m up on its pedestal, out of reach. Every size is an authoring default, in
    /// metres.
    /// </summary>
    public static partial class VeilmouthCity
    {
        private const string SaintName = "Stone Grandad - the veiled figure", SaintStone = "VIC_Stone_Dirty";
        private const float RobeHem = 1.15f, RobeTop = 0.9f, RobeHeight = 4.4f, ShoulderFrom = 4.25f, ShoulderWide = 1.12f, ShoulderTo = 5.25f, ShoulderNarrow = 0.56f;
        private const float HoodFrom = 5.1f, HoodWide = 0.6f, HoodTo = 6.35f, HoodNarrow = 0.46f, HoodPoint = 7f, HoodDroop = 0.18f, StaffHeight = 7.6f;

        // The lantern's own glass, a copy of the grimy glass, so its panes glow at night and the hood's dark face does not.
        private const string LanternGlass = "VIC_Glass_Lantern", LanternName = "Stone Grandad's lantern";

        // Its light reaches the hood and the pedestal's top, m; a gas lamp is range 10, intensity 10. Authoring defaults.
        private const float LanternRange = 6f, LanternIntensity = 6f;

        /// <summary>
        /// Gives the saved Veilmouth scene's Stone Grandad its veiled figure in the place of the plain box, and saves the scene.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthCity.DressStatue
        /// </summary>
        public static void DressStatue()
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var city = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
            var group = city != null ? city.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.EndsWith(" (placeholder)") && t.name.StartsWith("Stone Grandad")) : null;
            if (group == null)
            {
                Debug.LogError($"[VeilmouthCity] no Stone Grandad placeholder under {RootName}.");
                return;
            }

            var colliders = group.GetComponents<BoxCollider>().OrderByDescending(c => c.size.x).ToList();
            if (colliders.Count < 2)
            {
                Debug.LogError("[VeilmouthCity] Stone Grandad has no pedestal to stand the figure on.");
                return;
            }

            // The plain figure was the narrowest box, in its own stone; the pedestal is the next.
            var pedestal = colliders[1];
            foreach (var box in colliders.Skip(2))
            {
                Object.DestroyImmediate(box);
            }

            // Works.Finish names each merged mesh's object by its material, and the plain figure alone wore this stone.
            var plain = group.Find(SaintStone);
            if (plain != null)
            {
                Object.DestroyImmediate(plain.gameObject);
                AssetDatabase.DeleteAsset($"{SliceKitCatalogue.Folder}/Works/{group.name} - {SaintStone}.asset");
            }

            var top = group.TransformPoint(pedestal.center + (Vector3.up * (pedestal.size.y / 2f)));
            SaintFigure(group, top, Vector3.left);
            var sky = Object.FindFirstObjectByType<VeilmouthSky>();
            if (sky != null)
            {
                sky.SetBeacons(Beacons());
                EditorUtility.SetDirty(sky);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>The veiled figure on the pedestal's top at <paramref name="foot"/>, facing <paramref name="facing"/>, replacing an older one.</summary>
        private static void SaintFigure(Transform group, Vector3 foot, Vector3 facing)
        {
            var old = group.Find(SaintName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var owner = new GameObject(SaintName).transform;
            owner.SetParent(group, false);
            var figure = new Works(owner, false);
            var turn = Mathf.Atan2(facing.z, facing.x) * Mathf.Rad2Deg;
            Frustum(figure, foot, 0f, RobeHem, RobeHeight, RobeTop, turn, null);
            Frustum(figure, foot, ShoulderFrom, ShoulderWide, ShoulderTo, ShoulderNarrow, turn, null);
            Frustum(figure, foot, HoodFrom, HoodWide, HoodTo, HoodNarrow, turn, null);

            // The hood's point droops back, away from where it faces.
            var apex = foot + (Vector3.up * HoodPoint) - (facing * HoodDroop);
            var ring = Ring(foot + (Vector3.up * HoodTo), HoodNarrow, turn);
            for (var k = 0; k < 8; k++)
            {
                var mid = (ring[k] + ring[(k + 1) % 8]) / 2f;
                var outward = (mid - (foot + (Vector3.up * HoodTo))).normalized + (Vector3.up * 0.7f);
                figure.Poly(new[] { ring[k], ring[(k + 1) % 8], apex }, outward);
            }

            // No face: a dark opening on the hood's front face.
            var front = foot + (Vector3.up * 5.72f) + (facing * ((HoodWide + HoodNarrow) / 2f * Mathf.Cos(22.5f * Mathf.Deg2Rad) + 0.03f));
            var side = Vector3.Cross(Vector3.up, facing).normalized;
            var face = new List<Vector3>();
            for (var k = 0; k < 10; k++)
            {
                var a = k * 36f * Mathf.Deg2Rad;
                face.Add(front + (side * (Mathf.Cos(a) * 0.17f)) + (Vector3.up * (Mathf.Sin(a) * 0.3f)));
            }

            figure.Poly(face, facing, Glazing);

            // The staff in the right hand, with a small lantern at its head.
            var hand = foot + (side * 1.3f) + (facing * 0.25f);
            var sleeve = new Vector3((Mathf.Abs(side.x) * 0.55f) + (Mathf.Abs(facing.x) * 0.35f), 0.35f, (Mathf.Abs(side.z) * 0.55f) + (Mathf.Abs(facing.z) * 0.35f));
            figure.Box(foot + (side * 1.05f) + (facing * 0.2f) + (Vector3.up * 3.7f), sleeve, 0f, false);
            figure.Box(hand + (Vector3.up * (StaffHeight / 2f)), new Vector3(0.12f, StaffHeight, 0.12f), 0f, false, DarkGlass);
            // The lantern: an iron base and cap, four iron corner posts, and its own glass between them.
            var flame = hand + (Vector3.up * (StaffHeight + 0.2f));
            figure.Box(flame + (Vector3.down * 0.18f), new Vector3(0.38f, 0.05f, 0.38f), turn, false, DarkGlass);
            figure.Box(flame + (Vector3.up * 0.18f), new Vector3(0.38f, 0.05f, 0.38f), turn, false, DarkGlass);
            figure.Box(flame, new Vector3(0.3f, 0.32f, 0.3f), turn, false, LanternGlass);
            var across = Quaternion.Euler(0f, turn, 0f); // as Works.Box turns the panes
            foreach (var (u, v) in new[] { (1f, 1f), (1f, -1f), (-1f, 1f), (-1f, -1f) })
            {
                figure.Box(flame + (across * new Vector3(u * 0.17f, 0f, v * 0.17f)), new Vector3(0.04f, 0.36f, 0.04f), turn, false, DarkGlass);
            }

            figure.Box(hand + (Vector3.up * (StaffHeight + 0.47f)), new Vector3(0.2f, 0.14f, 0.2f), turn, false, DarkGlass);
            figure.Finish(SaintStone);
            var light = new GameObject(LanternName) { layer = owner.gameObject.layer }.AddComponent<Light>();
            light.transform.SetParent(owner, false);
            light.transform.position = flame;
            light.type = LightType.Point;
            light.color = VeilmouthMystic.Gaslight;
            light.range = LanternRange;
            light.intensity = LanternIntensity;
            light.shadows = LightShadows.None;
            light.enabled = false;
            Debug.Log($"[VeilmouthCity] Stone Grandad's veiled figure stands on {foot}, facing {facing}, {HoodPoint:0.0} m tall with its staff at {StaffHeight + 0.54f:0.0} m.");
        }

        /// <summary>The eight corners of a level ring of radius <paramref name="r"/> about <paramref name="centre"/>, a face toward <paramref name="turn"/>°.</summary>
        private static Vector3[] Ring(Vector3 centre, float r, float turn)
        {
            var ring = new Vector3[8];
            for (var k = 0; k < 8; k++)
            {
                var a = (turn + 22.5f + (k * 45f)) * Mathf.Deg2Rad;
                ring[k] = centre + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            }

            return ring;
        }

        /// <summary>An eight-sided frustum's sides, from radius <paramref name="r0"/> at <paramref name="y0"/> to <paramref name="r1"/> at <paramref name="y1"/> over <paramref name="foot"/>.</summary>
        private static void Frustum(Works works, Vector3 foot, float y0, float r0, float y1, float r1, float turn, string material)
        {
            var low = Ring(foot + (Vector3.up * y0), r0, turn);
            var high = Ring(foot + (Vector3.up * y1), r1, turn);
            var lift = (r0 - r1) / (y1 - y0);
            for (var k = 0; k < 8; k++)
            {
                var n = (k + 1) % 8;
                var mid = ((low[k] + low[n]) / 2f) - (foot + (Vector3.up * y0));
                mid.y = 0f;
                works.Poly(new[] { low[k], low[n], high[n], high[k] }, mid.normalized + (Vector3.up * lift), material);
            }
        }
    }
}
