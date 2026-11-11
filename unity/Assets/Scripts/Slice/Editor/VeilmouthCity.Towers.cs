using System.Collections.Generic;
using System.Linq;
using GameDev.Slice.Kit;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The towers' dress (VEILMOUTH_CITY_PLAN.md §38; SLICE_MODELS_WANTED.md, the clock face and the lighthouse's lantern
    /// cap). The kit's white towers stay as they are, with their colliders:
    /// <list type="bullet">
    /// <item>the clock tower gets a clock face on each of its four sides in its top storey: a stone ring, a cream dial,
    /// twelve hour marks and two iron hands at ten past ten;</item>
    /// <item>the One-Eye, the lighthouse at the end of its pier, gets a lantern over its roof: a drum, a gallery, eight
    /// panes of dark glass between iron posts, an iron dome and a finial. The lantern is unlit for now.</item>
    /// </list>
    /// One merged mesh for each material, on NoGrid with no collider, in a group of its own beside the houses, so no kit
    /// prefab is changed. Every size is an authoring default, in metres; the kit's storey of 3.75 m (3 m at ×1.25) is
    /// measured.
    /// </summary>
    public static partial class VeilmouthCity
    {
        private const string TowersDressName = "The towers' dress", ClockTowerName = "The clock tower", LighthouseName = "The One-Eye";
        private const string DialFace = "VIC_Stucco_Cream", DialRing = "VIC_Stone_Light";
        private const float KitStorey = 3.75f, TowerHalf = 2.5f, DialRadius = 1.25f, DialRim = 0.22f, DialBelowEaves = 2.1f;
        private const float DrumRadius = 1.6f, GalleryRadius = 2.2f, LanternRadius = 1.25f, LanternHeight = 2.2f, DomeHeight = 1.3f;

        /// <summary>
        /// Dresses the saved Veilmouth scene's clock tower and lighthouse again, and saves the scene.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthCity.DressTowers
        /// </summary>
        public static void DressTowers()
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var city = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
            if (city == null)
            {
                Debug.LogError($"[VeilmouthCity] no {RootName} in the scene.");
                return;
            }

            TowersDressOf(city.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>Builds the towers' dress under <paramref name="root"/>, replacing an older one.</summary>
        private static void TowersDressOf(Transform root)
        {
            var old = root.Find(TowersDressName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var owner = new GameObject(TowersDressName).transform;
            owner.SetParent(root, false);
            var dress = new Works(owner, false);
            Physics.SyncTransforms();
            var all = root.GetComponentsInChildren<Transform>(true);
            var clock = all.FirstOrDefault(t => t.name == ClockTowerName);
            var lighthouse = all.FirstOrDefault(t => t.name == LighthouseName);
            if (clock != null)
            {
                ClockFaces(dress, clock, 6);
            }

            if (lighthouse != null)
            {
                Lantern(dress, lighthouse, 6);
            }

            dress.Finish(DialRing);
            Debug.Log($"[VeilmouthCity] the towers' dress: clock faces {(clock != null ? "on" : "missing from")} {ClockTowerName}, a lantern {(lighthouse != null ? "on" : "missing from")} {LighthouseName}.");
        }

        /// <summary>The kit tower's middle and its walls' top, from its renderers and its storeys.</summary>
        private static (Vector3 Centre, float Eaves, float Apex) TowerFrame(Transform house, int storeys)
        {
            var renderers = house.GetComponentsInChildren<MeshRenderer>(true);
            var bounds = renderers[0].bounds;
            foreach (var r in renderers.Skip(1))
            {
                bounds.Encapsulate(r.bounds);
            }

            return (new Vector3(bounds.center.x, house.position.y, bounds.center.z), house.position.y + (storeys * KitStorey), bounds.max.y);
        }

        /// <summary>Where a wall of the tower stands from its middle toward <paramref name="dir"/> at height <paramref name="y"/>: measured by a ray at its colliders, else the kit's half width.</summary>
        private static float WallOut(Vector3 centre, Vector3 dir, float y)
        {
            var from = new Vector3(centre.x, y, centre.z) + (dir * 12f);
            return Physics.Raycast(from, -dir, out var hit, 12f, LayerMask.GetMask(KitLayers.Structure), QueryTriggerInteraction.Ignore)
                ? 12f - hit.distance
                : TowerHalf;
        }

        private static void ClockFaces(Works dress, Transform house, int storeys)
        {
            var (centre, eaves, _) = TowerFrame(house, storeys);
            var y = eaves - DialBelowEaves;
            foreach (var dir in new[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left })
            {
                var wall = WallOut(centre, dir, y);
                if (wall < 1f || wall > 4f)
                {
                    wall = TowerHalf; // a ray that met another house or nothing near: the kit's half width
                }

                var at = new Vector3(centre.x, y, centre.z) + (dir * wall);
                var right = Vector3.Cross(Vector3.up, -dir).normalized; // the viewer's right, facing the wall
                Disc(dress, at + (dir * 0.08f), right, dir, DialRadius + DialRim, DialRing);
                Disc(dress, at + (dir * 0.11f), right, dir, DialRadius, DialFace);
                for (var h = 0; h < 12; h++)
                {
                    var a = h * 30f * Mathf.Deg2Rad;
                    var u = (right * Mathf.Sin(a)) + (Vector3.up * Mathf.Cos(a));
                    var v = Vector3.Cross(dir, u).normalized;
                    var mark = h % 3 == 0 ? 0.16f : 0.1f;
                    Rect(dress, at + (dir * 0.13f) + (u * (DialRadius - 0.08f - mark)), u, v, mark, h % 3 == 0 ? 0.045f : 0.03f, dir, DarkGlass);
                }

                foreach (var (angle, length, width) in new[] { (300f, 0.62f, 0.06f), (60f, 0.98f, 0.04f) })
                {
                    var a = angle * Mathf.Deg2Rad;
                    var u = (right * Mathf.Sin(a)) + (Vector3.up * Mathf.Cos(a));
                    var v = Vector3.Cross(dir, u).normalized;
                    Rect(dress, at + (dir * 0.15f) + (u * ((length - 0.12f) / 2f)), u, v, (length + 0.12f) / 2f, width, dir, DarkGlass);
                }

                Disc(dress, at + (dir * 0.17f), right, dir, 0.09f, DarkGlass);
            }
        }

        private static void Lantern(Works dress, Transform house, int storeys)
        {
            var (centre, eaves, apex) = TowerFrame(house, storeys);
            var foot = new Vector3(centre.x, 0f, centre.z);
            var drumTop = apex + 0.3f;
            Frustum(dress, foot, eaves - 0.2f, DrumRadius, drumTop, DrumRadius, 0f, DialRing);
            // The gallery: an eight-sided deck with its edge, and a low iron rail round it.
            var deck = Ring(foot + (Vector3.up * (drumTop + 0.3f)), GalleryRadius, 0f);
            dress.Poly(deck, Vector3.up, DialRing);
            Frustum(dress, foot, drumTop, GalleryRadius, drumTop + 0.3f, GalleryRadius, 0f, DialRing);
            dress.Poly(Ring(foot + (Vector3.up * drumTop), GalleryRadius, 0f).Reverse().ToList(), Vector3.down, DialRing);
            Frustum(dress, foot, drumTop + 1.15f, GalleryRadius - 0.05f, drumTop + 1.25f, GalleryRadius - 0.05f, 0f, DarkGlass);
            var deckTop = drumTop + 0.3f;
            for (var k = 0; k < 8; k++)
            {
                var a = (22.5f + (k * 45f)) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                dress.Box(foot + (dir * (GalleryRadius - 0.05f)) + (Vector3.up * (deckTop + 0.45f)), new Vector3(0.06f, 0.9f, 0.06f), 0f, false, DarkGlass);
                dress.Box(foot + (dir * LanternRadius) + (Vector3.up * (deckTop + (LanternHeight / 2f))), new Vector3(0.12f, LanternHeight, 0.12f), -a * Mathf.Rad2Deg, false, DarkGlass);
            }

            // The panes, a low iron sill under them, and the dome over them with its finial.
            Frustum(dress, foot, deckTop, LanternRadius, deckTop + 0.5f, LanternRadius, 0f, DarkGlass);
            Frustum(dress, foot, deckTop + 0.5f, LanternRadius - 0.02f, deckTop + LanternHeight, LanternRadius - 0.02f, 0f, Glazing);
            var top = deckTop + LanternHeight;
            Frustum(dress, foot, top, LanternRadius + 0.2f, top + 0.15f, LanternRadius + 0.2f, 0f, DarkGlass);
            var rim = Ring(foot + (Vector3.up * (top + 0.15f)), LanternRadius + 0.2f, 0f);
            var crown = foot + (Vector3.up * (top + 0.15f + DomeHeight));
            for (var k = 0; k < 8; k++)
            {
                var mid = ((rim[k] + rim[(k + 1) % 8]) / 2f) - foot;
                mid.y = 0f;
                dress.Poly(new[] { rim[k], rim[(k + 1) % 8], crown }, mid.normalized + (Vector3.up * 1.1f), DarkGlass);
            }

            dress.Box(crown + (Vector3.up * 0.18f), new Vector3(0.3f, 0.3f, 0.3f), 45f, false, DarkGlass);
            dress.Box(crown + (Vector3.up * 0.75f), new Vector3(0.05f, 0.9f, 0.05f), 0f, false, DarkGlass);
        }
    }
}
