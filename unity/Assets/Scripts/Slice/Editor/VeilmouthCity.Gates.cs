using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The gatehouse's dress over each gate of the town wall (VEILMOUTH_CITY_PLAN.md §41; SLICE_MODELS_WANTED.md § C4,
    /// "Town gatehouse over a bridge", whose placeholder is two kit towers and a wall with the opening). The gate stood as
    /// two kit towers with a bare slab of the wall's stone between them over the road, and read as a gap in the wall, not
    /// as a gatehouse. Between the towers' inner faces, over the slab, rises a gate wall of the towers' brick with a light
    /// stone string course at its foot and a crenellated parapet on a coping at its top; under the slab, on both faces, a
    /// segmental arch with a ring of light stone and a keystone, its spandrels and soffit in the wall's stone. One merged
    /// mesh for each material, on NoGrid with no collider: the arch springs 2.9 m over the road at the towers' faces and
    /// everything else stands over the slab, so the road, its pavements and the Combat Grid are as before. Every size is an
    /// authoring default, in metres.
    /// </summary>
    public static partial class VeilmouthCity
    {
        private const string GatesDressName = "The gates' dress", GateBand = "VIC_Stone_Light", GateFallbackBrick = "VIC_Brick_Poor";

        // The gate wall over the slab, its string course, its coping and its merlons; the arch's rise, how far its crown
        // stands under the slab, and its stone ring's width; how far each piece stands proud of the wall's face.
        private const float GateAttic = 3.4f, GateCourse = 0.3f, GateCoping = 0.25f, GateMerlon = 0.85f, GateMerlonWide = 0.8f, GateCrenel = 0.55f;
        private const float GateRise = 1.15f, GateCrownUnder = 0.45f, GateRing = 0.45f, GateProud = 0.15f;

        /// <summary>
        /// Dresses the saved Veilmouth scene's gates again, and saves the scene.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthCity.DressGates
        /// </summary>
        public static void DressGates()
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var city = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
            if (city == null)
            {
                Debug.LogError($"[VeilmouthCity] no {RootName} in the scene.");
                return;
            }

            GatesDressOf(city.transform, VeilmouthLayout.Load());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>Builds the gates' dress under <paramref name="root"/> from <paramref name="layout"/>'s walls and towers, replacing an older one.</summary>
        private static void GatesDressOf(Transform root, VeilmouthLayout layout)
        {
            var old = root.Find(GatesDressName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var owner = new GameObject(GatesDressName).transform;
            owner.SetParent(root, false);
            var dress = new Works(owner, false);
            Physics.SyncTransforms();
            var all = root.GetComponentsInChildren<Transform>(true);
            var towers = layout.towers.Select(t => all.FirstOrDefault(x => x.name == t.name)).Where(t => t != null).ToList();
            var dressed = new List<string>();
            foreach (var w in layout.walls)
            {
                foreach (var gap in w.gaps ?? new VeilmouthLayout.Gap[0])
                {
                    var name = GateDress(dress, w, gap, towers);
                    if (name != null)
                    {
                        dressed.Add(name);
                    }
                }
            }

            dress.Finish(WallStone);
            Debug.Log($"[VeilmouthCity] the gates' dress: {dressed.Count} gate(s){(dressed.Count > 0 ? ": " + string.Join("; ", dressed) : "")}.");
        }

        /// <summary>One gate's dress between its two towers; returns a line for the log, or null where the gate has no two towers or no slab.</summary>
        private static string GateDress(Works dress, VeilmouthLayout.WallSpec w, VeilmouthLayout.Gap gap, List<Transform> towers)
        {
            var alongY = w.axis == "y";
            var mid = (w.t0 + w.t1) / 2f;
            var middle = alongY ? World(mid, (gap.a + gap.b) / 2f) : World((gap.a + gap.b) / 2f, mid);
            var along = alongY ? Vector3.forward : Vector3.right;
            var across = alongY ? Vector3.right : Vector3.forward;
            var half = (w.t1 - w.t0) / 2f;

            // The towers whose bodies stand within the gap, by their colliders' bounds along the wall.
            var sides = new List<(float Near, float Far, Transform Tower)>();
            foreach (var t in towers)
            {
                var boxes = t.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger).Select(c => c.bounds).ToList();
                if (boxes.Count == 0)
                {
                    continue;
                }

                var b = boxes.Aggregate((x, y) => { x.Encapsulate(y); return x; });
                var u0 = Vector3.Dot(b.min - middle, along);
                var u1 = Vector3.Dot(b.max - middle, along);
                var v = Vector3.Dot(b.center - middle, across);
                if (Mathf.Abs(v) < 6f && Mathf.Min(Mathf.Abs(u0), Mathf.Abs(u1)) < 12f)
                {
                    sides.Add((Mathf.Min(u0, u1), Mathf.Max(u0, u1), t));
                }
            }

            var low = sides.Where(s => s.Far <= 0.5f).OrderByDescending(s => s.Far).FirstOrDefault();
            var high = sides.Where(s => s.Near >= -0.5f).OrderBy(s => s.Near).FirstOrDefault();
            if (low.Tower == null || high.Tower == null || low.Tower == high.Tower)
            {
                return null;
            }

            // The slab over the road: the highest collider straight over the gate's middle.
            var hits = Physics.RaycastAll(middle + (Vector3.up * 400f), Vector3.down, 500f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance).ToList();
            var slab = hits.FirstOrDefault(h => h.collider is BoxCollider && h.collider.bounds.size.y < 4f && h.collider.bounds.min.y > middle.y - 50f);
            if (slab.collider == null)
            {
                return null;
            }

            var bottom = slab.collider.bounds.min.y;
            var top = slab.collider.bounds.max.y;
            var s0 = low.Far;
            var s1 = high.Near;
            var span = s1 - s0;
            var centre = (s0 + s1) / 2f;
            Vector3 P(float u, float y, float v) => new Vector3(middle.x, 0f, middle.z) + (along * u) + (across * v) + (Vector3.up * y);
            Vector3 Size(float u, float y, float v)
            {
                var s = (along * u) + (across * v) + (Vector3.up * y);
                return new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
            }

            var ground = hits[hits.Count - 1].point.y;

            var brick = low.Tower.GetComponentsInChildren<MeshRenderer>().SelectMany(r => r.sharedMaterials).Where(m => m != null && m.name.StartsWith("VIC_Brick")).Select(m => m.name).FirstOrDefault() ?? GateFallbackBrick;

            // The gate wall over the slab, its string course at its foot, its coping and its merlons.
            var thick = half + GateProud;
            dress.Box(P(centre, top + (GateAttic / 2f), 0f), Size(span, GateAttic, thick * 2f), 0f, false, brick);
            dress.Box(P(centre, top, 0f), Size(span, GateCourse, (thick + 0.1f) * 2f), 0f, false, GateBand);
            var coping = top + GateAttic;
            dress.Box(P(centre, coping + (GateCoping / 2f), 0f), Size(span, GateCoping, (thick + 0.08f) * 2f), 0f, false, GateBand);
            var merlons = Mathf.Max(2, Mathf.FloorToInt((span + GateCrenel) / (GateMerlonWide + GateCrenel)));
            var crenel = (span - (merlons * GateMerlonWide)) / (merlons - 1);
            for (var k = 0; k < merlons; k++)
            {
                var u = s0 + (GateMerlonWide / 2f) + (k * (GateMerlonWide + crenel));
                dress.Box(P(u, coping + GateCoping + (GateMerlon / 2f), 0f), Size(GateMerlonWide, GateMerlon, thick * 2f), 0f, false, brick);
                dress.Box(P(u, coping + GateCoping + GateMerlon + 0.05f, 0f), Size(GateMerlonWide + 0.1f, 0.1f, (thick + 0.05f) * 2f), 0f, false, GateBand);
            }

            // Under the slab: a segmental arch on both faces, its spandrels and soffit in the wall's stone, its ring and
            // keystone in light stone.
            var crown = bottom - GateCrownUnder;
            var radius = ((span * span / 4f) + (GateRise * GateRise)) / (2f * GateRise);
            var hub = crown - radius;
            var reach = Mathf.Asin(Mathf.Clamp01(span / 2f / radius));
            const int steps = 16;
            var inner = new Vector2[steps + 1];
            var outer = new Vector2[steps + 1];
            for (var k = 0; k <= steps; k++)
            {
                var a = Mathf.Lerp(-reach, reach, k / (float)steps);
                inner[k] = new Vector2(centre + (Mathf.Sin(a) * radius), hub + (Mathf.Cos(a) * radius));
                outer[k] = new Vector2(centre + (Mathf.Sin(a) * (radius + GateRing)), hub + (Mathf.Cos(a) * (radius + GateRing)));
            }

            for (var k = 0; k < steps; k++)
            {
                var (a, b) = (inner[k], inner[k + 1]);
                var inward = new Vector2(centre - ((a.x + b.x) / 2f), hub - ((a.y + b.y) / 2f)).normalized;
                var soffit = (along * inward.x) + (Vector3.up * inward.y);
                foreach (var face in new[] { -1f, 1f })
                {
                    var v = face * half;
                    dress.Poly(new[] { P(a.x, a.y, v), P(b.x, b.y, v), P(b.x, bottom, v), P(a.x, bottom, v) }, across * face);
                    // The ring stands proud of the spandrel, and is cut at the slab's bottom.
                    var vr = face * (half + 0.06f);
                    var (oa, ob) = (outer[k], outer[k + 1]);
                    var ringA = new Vector2(oa.x, Mathf.Min(oa.y, bottom));
                    var ringB = new Vector2(ob.x, Mathf.Min(ob.y, bottom));
                    dress.Poly(new[] { P(a.x, a.y, vr), P(b.x, b.y, vr), P(ringB.x, ringB.y, vr), P(ringA.x, ringA.y, vr) }, across * face, GateBand);
                    dress.Poly(new[] { P(a.x, a.y, v), P(b.x, b.y, v), P(b.x, b.y, vr), P(a.x, a.y, vr) }, soffit, GateBand);
                }

                // The soffit, facing down into the passage.
                dress.Poly(new[] { P(a.x, a.y, -half), P(b.x, b.y, -half), P(b.x, b.y, half), P(a.x, a.y, half) }, soffit);
            }

            foreach (var face in new[] { -1f, 1f })
            {
                dress.Box(P(centre, crown + 0.2f, face * (half + 0.05f)), Size(0.55f, 0.85f, 0.22f), 0f, false, GateBand);
            }

            var gateName = $"{low.Tower.name} and {high.Tower.name}: {span:0.0} m between, the slab at {bottom - ground:0.0}–{top - ground:0.0} m over the road, the arch's springing at {crown - GateRise - ground:0.0} m, the parapet to {coping + GateCoping + GateMerlon + 0.1f - ground:0.0} m";
            return gateName;
        }
    }
}
