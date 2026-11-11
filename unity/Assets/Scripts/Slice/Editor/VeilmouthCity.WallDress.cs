using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The town wall's dress (VEILMOUTH_CITY_PLAN.md §42). The town wall and the works' wall stood as plain slabs of dirty
    /// stone, 6 m high with flat tops, and read from the river as a long blank box. Over each of the wall's slabs, read
    /// from its own collider, stands a coping of light stone along its top and a plinth course at its foot on both faces;
    /// on its outer face only, the one away from the Penny, a pilaster every 8 m, so nothing stands proud toward the city's
    /// streets. One merged mesh for each material, on NoGrid with no collider: the plinth and the pilasters stand at most
    /// 0.15 m proud on faces nobody walks along, and the wall's colliders and the Combat Grid are as before. Every size is
    /// an authoring default, in metres.
    /// </summary>
    public static partial class VeilmouthCity
    {
        private const string WallDressName = "The town wall's dress", WallBand = "VIC_Stone_Light", PilasterStone = "VIC_Stone_Medium";

        // The coping's height and overhang, the plinth's height over the ground and its proud, the pilasters' width,
        // proud and spacing; the Penny, toward which a wall's inner face looks.
        private const float CopingHigh = 0.45f, CopingOver = 0.15f, PlinthHigh = 0.7f, PlinthProud = 0.08f, PilasterWide = 0.7f, PilasterProud = 0.15f, PilasterEvery = 8f;
        private static readonly Vector3 WallInward = new Vector3(40f, 0f, 60f);

        /// <summary>
        /// Dresses the saved Veilmouth scene's town wall again, and saves the scene.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthCity.DressWall
        /// </summary>
        public static void DressWall()
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var city = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
            if (city == null)
            {
                Debug.LogError($"[VeilmouthCity] no {RootName} in the scene.");
                return;
            }

            WallDressOf(city.transform, VeilmouthLayout.Load());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>Builds the town wall's dress under <paramref name="root"/> over the layout's walls' slabs, replacing an older one.</summary>
        private static void WallDressOf(Transform root, VeilmouthLayout layout)
        {
            var old = root.Find(WallDressName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var stone = root.Find("Stone Works");
            if (stone == null)
            {
                Debug.Log("[VeilmouthCity] the town wall's dress: no stone works under the city.");
                return;
            }

            var owner = new GameObject(WallDressName).transform;
            owner.SetParent(root, false);
            var dress = new Works(owner, false);
            var boxes = stone.GetComponents<BoxCollider>().Select(b => b.bounds).ToList();
            var slabs = 0;
            var pilasters = 0;
            var length = 0f;
            foreach (var w in layout.walls)
            {
                var alongY = w.axis == "y";
                var mid = (w.t0 + w.t1) / 2f;
                var thick = w.t1 - w.t0;
                // The wall's line in the world, and its axes: along it, and across it toward its outer face.
                var line = alongY ? World(mid, 0f).x : World(0f, mid).z;
                var along = alongY ? Vector3.forward : Vector3.right;
                var across = alongY ? Vector3.right : Vector3.forward;
                var outward = Vector3.Dot(WallInward, across) < line ? 1f : -1f;
                var span0 = Mathf.Min(alongY ? World(0f, w.a0).z : World(w.a0, 0f).x, alongY ? World(0f, w.a1).z : World(w.a1, 0f).x);
                var span1 = Mathf.Max(alongY ? World(0f, w.a0).z : World(w.a0, 0f).x, alongY ? World(0f, w.a1).z : World(w.a1, 0f).x);
                var gaps = (w.gaps ?? new VeilmouthLayout.Gap[0]).Select(g => alongY ? (Lo: World(0f, g.b).z, Hi: World(0f, g.a).z) : (Lo: World(g.a, 0f).x, Hi: World(g.b, 0f).x)).ToList();
                foreach (var b in boxes)
                {
                    var c = Vector3.Dot(b.center, across);
                    var size = Vector3.Dot(b.size, across);
                    var u0 = Vector3.Dot(b.min, along);
                    var u1 = Vector3.Dot(b.max, along);
                    // A slab of this wall: its thickness, on its line, within its run, and not a gate's slab.
                    if (Mathf.Abs(size - thick) > 0.05f || Mathf.Abs(c - line) > 0.05f || u0 < span0 - 0.5f || u1 > span1 + 0.5f ||
                        gaps.Any(g => u0 >= g.Lo - 0.05f && u1 <= g.Hi + 0.05f))
                    {
                        continue;
                    }

                    slabs++;
                    length += u1 - u0;
                    var um = (u0 + u1) / 2f;
                    var top = b.max.y;
                    // The plinth's foot: the slab is sunk 1 m below its lowest ground, so its ground is about 1 m over its bottom.
                    var foot = b.min.y + 1f;
                    Vector3 P(float u, float y, float v) => (along * u) + (across * v) + (Vector3.up * y);
                    Vector3 S(float u, float y, float v) => new Vector3(Mathf.Abs((along * u + across * v).x), y, Mathf.Abs((along * u + across * v).z));
                    dress.Box(P(um, top + (CopingHigh / 2f) - 0.02f, line), S(u1 - u0, CopingHigh, thick + (CopingOver * 2f)), 0f, false, WallBand);
                    dress.Box(P(um, foot + (PlinthHigh / 2f) - 0.5f, line), S(u1 - u0, PlinthHigh + 1f, thick + (PlinthProud * 2f)), 0f, false, WallBand);
                    // A pilaster at each multiple of 8 m along the wall within this slab, on the outer face only.
                    for (var u = Mathf.Ceil((u0 + 0.5f) / PilasterEvery) * PilasterEvery; u < u1 - 0.5f; u += PilasterEvery)
                    {
                        var height = top - foot + 1f;
                        dress.Box(P(u, foot - 1f + (height / 2f), line + (outward * ((thick / 2f) + (PilasterProud / 2f)))), S(PilasterWide, height, PilasterProud), 0f, false, PilasterStone);
                        pilasters++;
                    }
                }
            }

            dress.Finish(WallBand);
            Debug.Log($"[VeilmouthCity] the town wall's dress: {slabs} slabs over {length:0} m, {pilasters} pilasters on the outer faces.");
        }
    }
}
