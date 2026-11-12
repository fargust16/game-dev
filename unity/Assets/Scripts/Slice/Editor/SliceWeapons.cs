using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GameDev.Confront.Rules;
using GameDev.Slice.Fights;
using GameDev.Slice.Gear;
using GameDev.Slice.Kit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The weapons in hand (owner's play test of 2026-11-12; 10.2 §5c; PA10 WA1).
    /// <list type="bullet">
    /// <item><see cref="Build"/> makes each weapon's and the shield's model of the asset map's §3 into a held prefab, read
    /// from the model's own vertices band by band along its length: a blade's hilt is the end with the guard, the widest
    /// band of its nearer third, and the hand holds between the guard and the pommel; a haft's head is its wider end, and
    /// the hand holds a share of its length up from the butt; a firearm's muzzle is its thinner end, its barrel on top, and
    /// the hand holds the lower half of its grip; a shield's front is the side its middle stands out to, against its rim.
    /// Each prefab's origin is the grip, +Z the way the weapon leaves the fist, +Y its edge, blade or muzzle, as
    /// <see cref="HeldWeapons"/> sets it in the hand. They go into <see cref="WeaponModels"/> on the slice's settings.</item>
    /// <item><see cref="Render"/> draws the player man in each weapon's guard and blow, front and side, with the weapon's
    /// hand close up, so each grip is checked by eye; and reads when the fists' blows reach out furthest.</item>
    /// </list>
    /// Batch, with graphics: -executeMethod GameDev.Slice.EditorTools.SliceWeapons.Run (the clips and controllers again for
    /// the fists' blows, then the prefabs and the sheet).
    /// </summary>
    public static class SliceWeapons
    {
        public const string ModelsPath = "Assets/Data/Slice/WeaponModels.asset";
        private const string Output = "Logs/SliceSurvey";
        private const string HeldFolder = "Assets/Prefabs/Slice/Held";
        private const string Weapons = "Assets/Data/Confront/Weapons";
        private const string Blink = "Assets/Blink/Art/Weapons/LowPoly";
        private const int Bands = 40;

        /// <summary>Authoring default, m: a shield's back stands this far out from the fist that holds it.</summary>
        private const float ShieldStandOff = 0.05f;

        private enum Shape
        {
            Blade,
            Haft,
            Firearm,
            Shield,
        }

        /// <summary>
        /// Each held model: the fight asset it shows, its source, its shape, its length in the game, m (a shield's height),
        /// and where the right hand holds it: on a blade the share of its hilt from the guard toward the pommel, on a haft
        /// the share of its length up from the butt, on a firearm the share of its length from the butt's end. Authoring
        /// defaults: a dagger of 0.35 m, an arming sword of 0.95 m, a hand axe and a mace of 0.7 m, a greatsword of 1.35 m,
        /// a greataxe and a maul of 1.2 m, a flintlock pistol of 0.45 m and a musket of 1.45 m, a shield 0.65 m high; a
        /// two-handed weapon held high, the left hand below the right. Last, a blade's hilt end where its cross-sections
        /// show it (held.txt): -1 at the model's low end, +1 at its high end, 0 read from the sections, the end whose third
        /// holds the widest band. Measured on 2026-11-12: Sword15's grip is bands 1–6 at 0.02 of its length across and its
        /// guard band 9 at 0.18, its high end a broad blade at 0.19, so the widest band misleads; Sword1's grip runs 13
        /// bands at 0.02–0.04 to its guard at band 15, its high end tapering to 0.01; the dagger's grip 11 bands at
        /// 0.06–0.08 to its guard at bands 12–14, its high end tapering to 0.01.
        /// </summary>
        private static readonly (string Asset, string Source, Shape Shape, float Length, float Grip, int End)[] Models =
        {
            ("Dagger", "Assets/Low-Poly Weapons/Prefabs/Dagger.prefab", Shape.Blade, 0.35f, 0.5f, -1),
            ("Sword", Blink + "/FreeSwords/Prefabs/Sword15_Iron.prefab", Shape.Blade, 0.95f, 0.5f, -1),
            ("Greatsword", Blink + "/FreeSwords/Prefabs/Sword1_Silver.prefab", Shape.Blade, 1.35f, 0.3f, -1),
            ("Axe", Blink + "/FreeRPGWeapons/_PREFABS/Axe1H_Basic.prefab", Shape.Haft, 0.7f, 0.12f, 0),
            ("Greataxe", Blink + "/FreeRPGWeapons/_PREFABS/Axe2H_Basic.prefab", Shape.Haft, 1.2f, 0.3f, 0),
            ("Mace", Blink + "/FreeRPGWeapons/_PREFABS/Mace1H_Basic.prefab", Shape.Haft, 0.7f, 0.12f, 0),
            ("Maul", Blink + "/FreeRPGWeapons/_PREFABS/Mace2H_Basic.prefab", Shape.Haft, 1.2f, 0.3f, 0),
            ("Pistol", "Assets/TripoModels/flintlock_pistol_3d_model/flintlock_pistol_3d_model.fbx", Shape.Firearm, 0.45f, 0.12f, 0),
            ("Rifle", "Assets/TripoModels/ornate_flintlock_rifle_3d_model/ornate_flintlock_rifle_3d_model.fbx", Shape.Firearm, 1.45f, 0.3f, 0),
            ("Shield", Blink + "/FreeRPGWeapons/_PREFABS/Shield_Medium.prefab", Shape.Shield, 0.65f, 0f, 0),
            ("WoodenShield", Blink + "/FreeRPGWeapons/_PREFABS/Shield_Basic.prefab", Shape.Shield, 0.65f, 0f, 0),
        };

        public static void Run()
        {
            SlicePeopleModels.ClipsAndControllers();
            var report = new StringBuilder();
            Build(report);
            Render(report);
            Directory.CreateDirectory(Output);
            File.WriteAllText($"{Output}/held.txt", report.ToString());
            Debug.Log($"[SliceWeapons]\n{report}");
        }

        /// <summary>Just the prefabs and the sheet, the controllers as they stand.</summary>
        public static void BuildAndRender()
        {
            var report = new StringBuilder();
            Build(report);
            Render(report);
            Directory.CreateDirectory(Output);
            File.WriteAllText($"{Output}/held.txt", report.ToString());
            Debug.Log($"[SliceWeapons]\n{report}");
        }

        // ------------------------------------------------------------------------------------------- the held prefabs

        public static void Build(StringBuilder report)
        {
            Directory.CreateDirectory(HeldFolder);
            var entries = new List<(Object Fight, GameObject Held)>();
            foreach (var (asset, source, shape, length, grip, end) in Models)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(source);
                var fight = AssetDatabase.LoadAssetAtPath<Object>($"{Weapons}/{asset}.asset");
                if (prefab == null || fight == null)
                {
                    report.AppendLine($"{asset}: NO {(prefab == null ? source : asset + ".asset")}");
                    continue;
                }

                var root = new GameObject($"Held_{asset}");
                var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                foreach (var part in model.GetComponentsInChildren<Component>(true).Where(c => c is Collider || c is Rigidbody || c is MonoBehaviour || c is Animator).ToList())
                {
                    Object.DestroyImmediate(part);
                }

                model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var filters = model.GetComponentsInChildren<MeshFilter>().Where(f => f.sharedMesh != null).ToList();
                var points = filters.SelectMany(f => f.sharedMesh.vertices.Select(v => f.transform.TransformPoint(v))).ToList();
                var triangles = new List<(Vector3, Vector3, Vector3)>();
                foreach (var f in filters)
                {
                    var v = f.sharedMesh.vertices.Select(p => f.transform.TransformPoint(p)).ToArray();
                    var t = f.sharedMesh.triangles;
                    for (var i = 0; i + 2 < t.Length; i += 3)
                    {
                        triangles.Add((v[t[i]], v[t[i + 1]], v[t[i + 2]]));
                    }
                }
                if (points.Count == 0)
                {
                    report.AppendLine($"{asset}: NO MESH in {source}");
                    Object.DestroyImmediate(model);
                    Object.DestroyImmediate(root);
                    continue;
                }

                var frame = Frame(points, triangles, shape, length, grip, end);
                var scale = length / frame.Own;
                var turn = Quaternion.Inverse(Quaternion.LookRotation(frame.Z, frame.Y));
                var ownScale = model.transform.localScale;
                model.transform.SetParent(root.transform, false);
                model.transform.localRotation = turn;
                model.transform.localScale = ownScale * scale;
                model.transform.localPosition = -(turn * (frame.Grip * scale));
                SliceKitCatalogue.SetLayer(root, KitLayers.Character);
                foreach (var r in root.GetComponentsInChildren<Renderer>())
                {
                    r.receiveShadows = true;
                }

                var path = $"{HeldFolder}/Held_{asset}.prefab";
                var held = PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);
                entries.Add((fight, held));
                report.AppendLine($"{asset} ({Path.GetFileNameWithoutExtension(source)}, {shape}): {frame.Note}; {length:0.00} m in the game, ×{scale:0.000}");
            }

            var models = AssetDatabase.LoadAssetAtPath<WeaponModels>(ModelsPath);
            if (models == null)
            {
                models = ScriptableObject.CreateInstance<WeaponModels>();
                AssetDatabase.CreateAsset(models, ModelsPath);
            }

            var so = new SerializedObject(models);
            var list = so.FindProperty("entries");
            list.arraySize = entries.Count;
            for (var i = 0; i < entries.Count; i++)
            {
                var item = list.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("weapon").objectReferenceValue = entries[i].Fight as FightWeapon;
                item.FindPropertyRelative("offHand").objectReferenceValue = entries[i].Fight as FightOffHand;
                item.FindPropertyRelative("held").objectReferenceValue = entries[i].Held;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(models);

            var settings = AssetDatabase.LoadAssetAtPath<SliceSettings>(SliceConfrontSetup.SettingsPath);
            var set = new SerializedObject(settings);
            set.FindProperty("weaponModels").objectReferenceValue = models;
            set.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            report.AppendLine($"{entries.Count} held models in {ModelsPath}, on {SliceConfrontSetup.SettingsPath}");
            Sights(report);
        }

        // ------------------------------------------------------------------------------------------- the sights

        /// <summary>Batch: only the firearms' sights on the held prefabs as they stand (PB8b), the report in sights.txt.</summary>
        public static void SightsOnly()
        {
            var report = new StringBuilder();
            Sights(report);
            Directory.CreateDirectory(Output);
            File.WriteAllText($"{Output}/sights.txt", report.ToString());
            Debug.Log($"[SliceWeapons] sights\n{report}");
        }

        /// <summary>
        /// 10.2 §5e (PB8b): each firearm's rear sight, front sight and muzzle, measured on its held prefab and kept there as
        /// the children <see cref="PlayerAim.RearSight"/>, <see cref="PlayerAim.FrontSight"/> and <see cref="PlayerAim.Muzzle"/>.
        /// In the prefab's frame the muzzle lies along +Y and the barrel's top along +Z. The muzzle is the middle of the
        /// last section; the firearm's top is read section by section in the barrel's own plane; the front sight is the
        /// highest top within the last 6 % of the length. The rear sight is where the eye looks over the firearm: of the
        /// tops in front of the grip and behind the front sight, the one the front sight's top sees highest, so that the
        /// sight line from it to the front sight clears the whole firearm, as a front sight stands in its notch. A sight
        /// line along the barrel would pass through a revolver's frame or a rifle's lock, which hide the front sight.
        /// </summary>
        public static void Sights(StringBuilder report)
        {
            foreach (var asset in new[] { "Pistol", "Rifle" })
            {
                var path = $"{HeldFolder}/Held_{asset}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                {
                    report.AppendLine($"{asset} sights: NO {path}");
                    continue;
                }

                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (var old in new[] { PlayerAim.RearSight, PlayerAim.FrontSight, PlayerAim.Muzzle, PlayerAim.Forend })
                    {
                        var child = root.transform.Find(old);
                        if (child != null)
                        {
                            Object.DestroyImmediate(child.gameObject);
                        }
                    }

                    var triangles = new List<(Vector3, Vector3, Vector3)>();
                    foreach (var f in root.GetComponentsInChildren<MeshFilter>().Where(f => f.sharedMesh != null))
                    {
                        var v = f.sharedMesh.vertices.Select(p => root.transform.InverseTransformPoint(f.transform.TransformPoint(p))).ToArray();
                        var t = f.sharedMesh.triangles;
                        for (var i = 0; i + 2 < t.Length; i += 3)
                        {
                            triangles.Add((v[t[i]], v[t[i + 1]], v[t[i + 2]]));
                        }
                    }

                    if (triangles.Count == 0)
                    {
                        report.AppendLine($"{asset} sights: NO MESH");
                        continue;
                    }

                    var (rear, front, muzzle, note) = MeasureSights(triangles);
                    Mark(root.transform, PlayerAim.RearSight, rear);
                    Mark(root.transform, PlayerAim.FrontSight, front);
                    Mark(root.transform, PlayerAim.Muzzle, muzzle);
                    var forend = Forend(triangles, muzzle);
                    if (forend.HasValue)
                    {
                        Mark(root.transform, PlayerAim.Forend, forend.Value);
                        note += $"; the forend at {forend.Value.ToString("F3")}";
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    report.AppendLine($"{asset} sights: rear {rear.ToString("F3")}, front {front.ToString("F3")}, muzzle {muzzle.ToString("F3")} (m, the prefab's frame, the grip at 0); " +
                                      $"sight base {Vector3.Distance(rear, front):0.000} m, the sight line {Vector3.Angle(front - rear, Vector3.up):0.0} deg off the barrel's way; {note}");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        /// <summary>
        /// A long firearm's forend, where the left hand holds it: the middle of its section <see cref="ForendShare"/> of the
        /// way from the grip to the muzzle; none on a firearm shorter than <see cref="LongArm"/>, held in one hand.
        /// </summary>
        private static Vector3? Forend(List<(Vector3, Vector3, Vector3)> triangles, Vector3 muzzle)
        {
            if (muzzle.y < LongArm)
            {
                return null;
            }

            var y = ForendShare * muzzle.y;
            var section = Cut(triangles, 1, y).Where(p => Mathf.Abs(p.x - muzzle.x) <= 0.05f).ToList();
            return section.Count == 0 ? (Vector3?)null : new Vector3(muzzle.x, y, Mid(section, 2));
        }

        // Authoring defaults, m and share: a firearm reaching this far from the grip to its muzzle is held in both hands,
        // the left hand 0.4 of the way to the muzzle.
        private const float LongArm = 0.6f;
        private const float ForendShare = 0.4f;

        private static void Mark(Transform root, string name, Vector3 at)
        {
            var mark = new GameObject(name).transform;
            mark.SetParent(root, false);
            mark.localPosition = at;
        }

        private static (Vector3 Rear, Vector3 Front, Vector3 Muzzle, string Note) MeasureSights(List<(Vector3, Vector3, Vector3)> triangles)
        {
            const int bands = 120;
            var all = triangles.SelectMany(t => new[] { t.Item1, t.Item2, t.Item3 }).ToList();
            var low = all.Min(p => p.y);
            var high = all.Max(p => p.y);
            var length = high - low;
            float At(int k) => low + ((k + 0.5f) / bands * length);

            // The muzzle: the middle of the last section; the barrel's plane and thickness from it.
            var last = Cut(triangles, 1, high - (0.005f * length));
            var cx = Mid(last, 0);
            var cz = Mid(last, 2);
            var thick = Mathf.Max(Range(last, 0), Range(last, 2), 0.004f);
            var muzzle = new Vector3(cx, high, cz);

            // The barrel's top, section by section, in its own plane.
            var tops = new float?[bands];
            for (var k = 0; k < bands; k++)
            {
                var near = Cut(triangles, 1, At(k)).Where(p => Mathf.Abs(p.x - cx) <= thick * 0.75f).ToList();
                tops[k] = near.Count > 0 ? near.Max(p => p.z) : (float?)null;
            }

            int Band(float y) => Mathf.Clamp(Mathf.FloorToInt((y - low) / length * bands), 0, bands - 1);

            // The front sight: the highest top within the last 6 %.
            var frontK = -1;
            for (var k = Band(high - (0.06f * length)); k < bands; k++)
            {
                if (tops[k].HasValue && (frontK < 0 || tops[k].Value > tops[frontK].Value))
                {
                    frontK = k;
                }
            }

            var front = frontK >= 0 ? new Vector3(cx, At(frontK), tops[frontK].Value) : new Vector3(cx, high, cz + (thick * 0.5f));

            // The rear sight: the top in front of the grip and a tenth of the length behind the front sight that the front
            // sight's top sees highest; with none, the barrel's top straight behind the front sight.
            var rear = new Vector3(cx, Mathf.Max(0f, front.y - (0.5f * length)), front.z);
            var steepest = float.NegativeInfinity;
            for (var k = Band(0f); k < bands; k++)
            {
                var y = At(k);
                if (y < 0f || y > front.y - (0.1f * length) || !tops[k].HasValue)
                {
                    continue;
                }

                var rise = Mathf.Atan2(tops[k].Value - front.z, front.y - y);
                if (rise > steepest)
                {
                    steepest = rise;
                    rear = new Vector3(cx, y, tops[k].Value);
                }
            }

            var profile = string.Join(" ", Enumerable.Range(0, bands).Where(k => k % 4 == 0).Select(k => tops[k].HasValue ? $"{At(k):0.00}:{tops[k].Value:0.000}" : $"{At(k):0.00}:-"));
            return (rear, front, muzzle,
                $"barrel {thick * 1000f:0} mm thick, the sight line {Mathf.Rad2Deg * steepest:0.0} deg down from the rear sight to the front sight; tops (y:z): {profile}");
        }

        private struct HeldFrame
        {
            public Vector3 Grip;
            public Vector3 Z;
            public Vector3 Y;
            public float Own;
            public string Note;
        }

        private static Vector3 Axis(int i, float sign = 1f)
        {
            var v = Vector3.zero;
            v[i] = sign;
            return v;
        }

        private static float Range(IReadOnlyCollection<Vector3> points, int axis) => points.Count == 0 ? 0f : points.Max(p => p[axis]) - points.Min(p => p[axis]);

        private static float Mid(IReadOnlyCollection<Vector3> points, int axis) => points.Count == 0 ? 0f : (points.Max(p => p[axis]) + points.Min(p => p[axis])) / 2f;

        /// <summary>
        /// Where the model's surface crosses the plane at <paramref name="at"/> along <paramref name="axis"/>: its true
        /// cross-section. A low-poly model's vertices stand only at the ends of its long faces, so bands of vertices leave
        /// a blade empty and a hilt where the vertices crowd; the triangles' edges do not.
        /// </summary>
        private static List<Vector3> Cut(List<(Vector3, Vector3, Vector3)> triangles, int axis, float at)
        {
            var hits = new List<Vector3>();
            void Edge(Vector3 u, Vector3 v)
            {
                var du = u[axis] - at;
                var dv = v[axis] - at;
                if ((du < 0f && dv < 0f) || (du > 0f && dv > 0f))
                {
                    return;
                }

                hits.Add(Mathf.Abs(du - dv) < 1e-9f ? u : Vector3.Lerp(u, v, du / (du - dv)));
            }

            foreach (var (p, q, r) in triangles)
            {
                Edge(p, q);
                Edge(q, r);
                Edge(r, p);
            }

            return hits;
        }

        /// <summary>The model's held frame from its cross-sections: its grip, the way it leaves the fist, its edge, its own length.</summary>
        private static HeldFrame Frame(List<Vector3> points, List<(Vector3, Vector3, Vector3)> triangles, Shape shape, float length, float grip, int end)
        {
            var min = new Vector3(points.Min(p => p.x), points.Min(p => p.y), points.Min(p => p.z));
            var max = new Vector3(points.Max(p => p.x), points.Max(p => p.y), points.Max(p => p.z));
            var size = max - min;
            var order = new[] { 0, 1, 2 }.OrderByDescending(i => size[i]).ToArray();
            var a = order[0];
            var own = size[a];
            // Each band's cross-section at its middle; a band's points are the sections inside it.
            var sections = Enumerable.Range(0, Bands).Select(k => Cut(triangles, a, min[a] + ((k + 0.5f) / Bands * own))).ToArray();
            List<Vector3> Band(float from, float to) => Enumerable.Range(0, Bands).Where(k => (k + 0.5f) / Bands >= from && (k + 0.5f) / Bands <= to).SelectMany(k => sections[k]).ToList();
            float Width(List<Vector3> band) => Mathf.Max(Range(band, order[1]), Range(band, order[2]));
            var widths = sections.Select(Width).ToArray();
            var frame = new HeldFrame { Own = own };
            var profile = string.Join(" ", widths.Select(w => (w / own).ToString("0.00")));

            if (shape == Shape.Shield)
            {
                // The thinnest axis is the face's normal; the longer of the other two is up, its wider end the top.
                var n = order[2];
                var up = order[0];
                var across = order[1];
                var half = new Vector2(size[up], size[across]) / 2f;
                var centre = (min + max) / 2f;
                float Radial(Vector3 p) => new Vector2((p[up] - centre[up]) / half.x, (p[across] - centre[across]) / half.y).magnitude;
                var middle = points.Where(p => Radial(p) < 0.25f).ToList();
                var rim = points.Where(p => Radial(p) > 0.75f).ToList();
                var front = middle.Count > 0 && rim.Count > 0 && middle.Average(p => p[n]) < rim.Average(p => p[n]) ? -1f : 1f;
                var top = points.Where(p => p[up] > max[up] - (0.2f * size[up])).ToList();
                var bottom = points.Where(p => p[up] < min[up] + (0.2f * size[up])).ToList();
                var upSign = Range(top, across) >= Range(bottom, across) ? 1f : -1f;
                var x = Axis(n, front);
                frame.Z = Axis(up, upSign);
                frame.Y = Vector3.Cross(frame.Z, x);
                frame.Own = size[up];
                frame.Grip = centre - (x * ((size[n] / 2f) + (ShieldStandOff * size[up] / length)));
                frame.Note = $"face normal {AxisName(n, front)}, up {AxisName(up, upSign)}, {size[up]:0.000} × {size[across]:0.000} × {size[n]:0.000} own units; middle {(middle.Count > 0 ? middle.Average(p => p[n]) : 0f):0.000} against rim {(rim.Count > 0 ? rim.Average(p => p[n]) : 0f):0.000} along it";
                return frame;
            }

            var b = order[1];
            var c = order[2];
            if (shape == Shape.Blade)
            {
                var third = Bands * 2 / 5;
                var lowMax = widths.Take(third).Max();
                var highMax = widths.Skip(Bands - third).Max();
                var hiltHigh = end != 0 ? end > 0 : highMax > lowMax;
                var guardBand = hiltHigh
                    ? Enumerable.Range(Bands - third, third).OrderByDescending(k => widths[k]).First()
                    : Enumerable.Range(0, third).OrderByDescending(k => widths[k]).First();
                var guardAt = min[a] + ((guardBand + 0.5f) / Bands * own);
                var pommel = hiltHigh ? max[a] : min[a];
                var gripAt = Mathf.Lerp(guardAt, pommel, grip);
                var tipSide = hiltHigh ? Band(0f, (guardBand - 3) / (float)Bands) : Band((guardBand + 4) / (float)Bands, 1f);
                var edge = Range(tipSide, b) >= Range(tipSide, c) ? b : c;
                var around = Cut(triangles, a, gripAt);
                frame.Z = Axis(a, hiltHigh ? -1f : 1f);
                frame.Y = Axis(edge);
                frame.Grip = Axis(a) * gripAt;
                frame.Grip[b] = Mid(around, b);
                frame.Grip[c] = Mid(around, c);
                frame.Note = $"length along {AxisName(a, 1f)}, the hilt at its {(hiltHigh ? "high" : "low")} end ({(end != 0 ? "measured" : "read")}), the guard at band {guardBand} of {Bands} " +
                             $"({widths[guardBand] / own:0.00} of the length across), held {grip:0.00} of the hilt down from it, the edge along {AxisName(edge, 1f)}; widths: {profile}";
                return frame;
            }

            if (shape == Shape.Haft)
            {
                var tail = Bands * 3 / 10;
                var headHigh = widths.Skip(Bands - tail).Max() >= widths.Take(tail).Max();
                var butt = headHigh ? min[a] : max[a];
                var gripAt = butt + ((headHigh ? 1f : -1f) * grip * own);
                var head = headHigh ? Band(1f - 0.3f, 1f) : Band(0f, 0.3f);
                var around = Cut(triangles, a, gripAt);
                var line = new Vector2(Mid(around, b), Mid(around, c));
                var wide = Range(head, b) >= Range(head, c) ? b : c;
                var lineWide = wide == b ? line.x : line.y;
                var outward = head.Max(p => p[wide]) - lineWide;
                var inward = lineWide - head.Min(p => p[wide]);
                frame.Z = Axis(a, headHigh ? 1f : -1f);
                frame.Y = Axis(wide, outward >= inward ? 1f : -1f);
                frame.Grip = Axis(a) * gripAt;
                frame.Grip[b] = line.x;
                frame.Grip[c] = line.y;
                frame.Note = $"length along {AxisName(a, 1f)}, the head at its {(headHigh ? "high" : "low")} end, held {grip:0.00} of the length up from the butt, " +
                             $"the blade or face toward {AxisName(wide, outward >= inward ? 1f : -1f)} ({Mathf.Max(outward, inward) / own:0.00} of the length beyond the haft); widths: {profile}";
                return frame;
            }

            // A firearm: the barrel's thinner end is the muzzle, the barrel on top of the grip or the stock.
            var h = b;
            var t = c;
            var slab = Bands * 3 / 20;
            float Height(List<Vector3> band) => Range(band, h);
            var muzzleHigh = Height(Band(1f - 0.15f, 1f)) <= Height(Band(0f, 0.15f));
            var muzzle = muzzleHigh ? Band(1f - 0.15f, 1f) : Band(0f, 0.15f);
            var upSignF = Mid(muzzle, h) >= (min[h] + max[h]) / 2f ? 1f : -1f;
            var from = muzzleHigh ? min[a] : max[a];
            var gripAtF = from + ((muzzleHigh ? 1f : -1f) * grip * own);
            var bandF = Cut(triangles, a, gripAtF);
            var midH = Mid(bandF, h);
            var lower = bandF.Where(p => (p[h] - midH) * upSignF <= 0f).ToList();
            frame.Z = Axis(h, upSignF);
            frame.Y = Axis(a, muzzleHigh ? 1f : -1f);
            frame.Grip = Axis(a) * gripAtF;
            frame.Grip[h] = lower.Count > 0 ? lower.Average(p => p[h]) : midH;
            frame.Grip[t] = lower.Count > 0 ? lower.Average(p => p[t]) : Mid(bandF, t);
            frame.Note = $"barrel along {AxisName(a, 1f)}, the muzzle at its {(muzzleHigh ? "high" : "low")} end, up {AxisName(h, upSignF)}, held {grip:0.00} of the length from the butt in the lower half of the grip; heights: {profile} (slab {slab})";
            return frame;
        }

        private static string AxisName(int axis, float sign) => (sign < 0f ? "-" : "+") + "XYZ"[axis];

        // ------------------------------------------------------------------------------------------- the check sheet

        private static FightWeapon Weapon(string name) => AssetDatabase.LoadAssetAtPath<FightWeapon>($"{Weapons}/{name}.asset");

        /// <summary>Each row: a Weapon Set and its FightBeats value, as SliceConfront's Motion gives it.</summary>
        private static List<(string Name, WeaponSet Set, int Motion)> Rows()
        {
            var shield = AssetDatabase.LoadAssetAtPath<FightOffHand>($"{Weapons}/Shield.asset");
            var wooden = AssetDatabase.LoadAssetAtPath<FightOffHand>($"{Weapons}/WoodenShield.asset");
            var pouch = AssetDatabase.LoadAssetAtPath<FightOffHand>($"{Weapons}/CartridgePouch.asset");
            return new List<(string, WeaponSet, int)>
            {
                ("Fists", new WeaponSet(Weapon("Fists")), FightBeats.Fists),
                ("Dagger", new WeaponSet(Weapon("Dagger")), FightBeats.Dagger),
                ("Sword", new WeaponSet(Weapon("Sword")), FightBeats.OneHand),
                ("Axe", new WeaponSet(Weapon("Axe")), FightBeats.OneHand),
                ("Mace", new WeaponSet(Weapon("Mace")), FightBeats.OneHand),
                ("Sword and shield", new WeaponSet(Weapon("Sword"), shield), FightBeats.Shield),
                ("Mace and wooden shield", new WeaponSet(Weapon("Mace"), wooden), FightBeats.Shield),
                ("Axe and mace", new WeaponSet(Weapon("Axe"), null, Weapon("Mace")), FightBeats.PairMain),
                ("Greatsword", new WeaponSet(Weapon("Greatsword")), FightBeats.TwoHand),
                ("Greataxe", new WeaponSet(Weapon("Greataxe")), FightBeats.TwoHand),
                ("Maul", new WeaponSet(Weapon("Maul")), FightBeats.TwoHand),
                ("Pistol", new WeaponSet(Weapon("Pistol"), pouch), FightBeats.Pistol),
                ("Rifle", new WeaponSet(Weapon("Rifle"), pouch), FightBeats.Rifle),
            };
        }

        /// <summary>Each row's guard and blow, front and side, and its main hand close up; the fists' reach read.</summary>
        public static void Render(StringBuilder report)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var light = new GameObject("Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
            var models = AssetDatabase.LoadAssetAtPath<WeaponModels>(ModelsPath);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SliceTestScenes.PlayerPrefabPath));
            var animator = player.GetComponentInChildren<Animator>();
            var held = HeldWeapons.Of(player.transform);

            foreach (var (use, bone) in new[] { ("Punch", HumanBodyBones.RightHand), ("Jab", HumanBodyBones.LeftHand) })
            {
                var (time, reach) = Reach(animator, player.transform, SliceClips.Get(use), bone);
                report.AppendLine($"{use}: the striking hand furthest out, {reach:0.00} m from the hips over the ground, at {time:0.00} s of {SliceClips.Get(use).length:0.00} s");
            }

            var rows = Rows();
            const int tile = 300;
            const int columns = 6;
            var sheet = new Texture2D(tile * columns, tile * rows.Count, TextureFormat.RGB24, false);
            var warm = 0;
            SliceCharacterRigs.Draw(player, new Texture2D(tile * 2, tile, TextureFormat.RGB24, false), 0, ref warm, tile, 1);
            for (var row = 0; row < rows.Count; row++)
            {
                var (name, set, motion) = rows[row];
                held.Draw(models, set);
                var guard = SliceClips.Get(FightBeats.Guards[motion - 1]);
                var blow = SliceClips.Get(FightBeats.Blows[motion - 1]);
                var column = 0;
                Pose(animator, guard, Mathf.Min(0.5f, guard.length));
                SliceCharacterRigs.Draw(player, sheet, row, ref column, tile, rows.Count);
                Close(player, held, sheet, row, ref column, tile, rows.Count, Quaternion.Euler(0f, 30f, 0f));
                Close(player, held, sheet, row, ref column, tile, rows.Count, Quaternion.Euler(0f, 120f, 0f));
                Pose(animator, blow, Mathf.Min(blow.length * 0.4f, blow.length));
                SliceCharacterRigs.Draw(player, sheet, row, ref column, tile, rows.Count);
                report.AppendLine($"row {row + 1}: {name}: main {(held.Main != null ? held.Main.name : "none")}, other hand {(held.Off != null ? held.Off.name : "none")}; " +
                                  $"guard {guard.name}, blow {blow.name} at 0.4 of it");

                // Each row on its own too, to be read at full size.
                var strip = new Texture2D(tile * columns, tile, TextureFormat.RGB24, false);
                strip.SetPixels(sheet.GetPixels(0, (rows.Count - 1 - row) * tile, tile * columns, tile));
                strip.Apply();
                Directory.CreateDirectory(Output);
                File.WriteAllBytes($"{Output}/held_{row + 1:00}.png", strip.EncodeToPNG());
                Object.DestroyImmediate(strip);
            }

            held.Sheathe();
            sheet.Apply();
            Directory.CreateDirectory(Output);
            File.WriteAllBytes($"{Output}/held.png", sheet.EncodeToPNG());
            report.AppendLine($"The sheet, {Output}/held.png: each row the guard front and side, the weapon's hand close up from the front and the side, the blow front and side.");
            Object.DestroyImmediate(player);
        }

        /// <summary>The hand that holds the main model, or the right, close up from the given turn about the body.</summary>
        private static void Close(GameObject player, HeldWeapons held, Texture2D sheet, int row, ref int column, int tile, int rows, Quaternion turn)
        {
            var animator = player.GetComponentInChildren<Animator>();
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand).position;
            var baked = Bake(player);
            var centre = hand;
            var size = 0.3f;
            if (held.Main != null && held.Main.GetComponentInChildren<Renderer>() is Renderer r)
            {
                var bounds = r.bounds;
                foreach (var other in held.Main.GetComponentsInChildren<Renderer>())
                {
                    bounds.Encapsulate(other.bounds);
                }

                bounds.Encapsulate(hand);
                centre = bounds.center;
                size = Mathf.Clamp(bounds.extents.magnitude, 0.2f, 0.9f);
            }

            var dir = turn * Vector3.forward;
            var camera = new GameObject("HeldCamera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = size * 1.1f;
            camera.transform.SetPositionAndRotation(centre + (dir * 4f), Quaternion.LookRotation(-dir));
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.78f, 0.84f, 0.9f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 10f;
            var target = new RenderTexture(tile, tile, 24);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(tile, tile, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, tile, tile), 0, 0);
            image.Apply();
            RenderTexture.active = null;
            sheet.SetPixels(column * tile, (rows - 1 - row) * tile, tile, tile, image.GetPixels());
            column++;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(camera.gameObject);
            Unbake(player, baked);
        }

        /// <summary>The skin baked on the CPU as the bones stand, as SliceCharacterRigs.Draw does: a skinned renderer drawn in batch mode keeps its rest pose.</summary>
        private static List<GameObject> Bake(GameObject model)
        {
            var baked = new List<GameObject>();
            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = new Mesh();
                smr.BakeMesh(mesh, true);
                var copy = new GameObject("Baked", typeof(MeshFilter), typeof(MeshRenderer));
                copy.transform.SetPositionAndRotation(smr.transform.position, smr.transform.rotation);
                copy.transform.localScale = smr.transform.parent != null ? smr.transform.parent.lossyScale : Vector3.one;
                copy.GetComponent<MeshFilter>().sharedMesh = mesh;
                copy.GetComponent<MeshRenderer>().sharedMaterials = smr.sharedMaterials;
                baked.Add(copy);
                smr.enabled = false;
            }

            return baked;
        }

        private static void Unbake(GameObject model, List<GameObject> baked)
        {
            foreach (var copy in baked)
            {
                Object.DestroyImmediate(copy.GetComponent<MeshFilter>().sharedMesh);
                Object.DestroyImmediate(copy);
            }

            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.enabled = true;
            }
        }

        /// <summary>When the hand reaches furthest from the hips over the ground, and how far.</summary>
        private static (float Time, float Reach) Reach(Animator animator, Transform root, AnimationClip clip, HumanBodyBones bone)
        {
            const float step = 1f / 60f;
            var hand = animator.GetBoneTransform(bone);
            var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            float best = 0f, at = 0f;
            for (var t = 0f; t <= clip.length + 1e-4f; t += step)
            {
                Pose(animator, clip, t);
                var h = root.InverseTransformPoint(hand.position);
                var c = root.InverseTransformPoint(hips.position);
                var reach = new Vector2(h.x - c.x, h.z - c.z).magnitude;
                if (reach > best)
                {
                    best = reach;
                    at = t;
                }
            }

            return (at, best);
        }

        private static void Pose(Animator animator, AnimationClip clip, float time)
        {
            var handler = new HumanPoseHandler(animator.avatar, animator.transform);
            var pose = SliceCharacterRigs.PoseAt(clip, time);
            handler.SetHumanPose(ref pose);
            handler.Dispose();
        }
    }
}
