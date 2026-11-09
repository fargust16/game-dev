using System.Collections.Generic;
using System.Linq;
using GameDev.Slice.Kit;
using GameDev.Slice.People;
using GameDev.World.Poi;
using GameDev.World.Portals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// S2.7, once: what the next day shows of POI A and B (11.1 §6; 11 *World residue*), as <see cref="WorldResidue"/>s
    /// read from the world state. In Veilmouth: Harrow &amp; Tincture's street door sealed with wax and brass once the
    /// Wardens seal it; two Warden pairs on Bell Street, 11.1's Tincture Row, from the next morning once they have taken
    /// him; and the women who walk near Loom Lane kept at home from the next morning once the collectors have taken a
    /// girl. In the apothecary: Harrow away while the Wardens hold him in the street, and once he is taken or gone. A scene
    /// that already has its residue is left as it is, as Veilmouth is the owner's.
    /// </summary>
    public static class SliceResidue
    {
        public const string SealedName = "Residue - Harrow & Tincture sealed";
        public const string PairsName = "Residue - Warden pairs on Bell Street";
        public const string FearName = "Residue - Fear on Loom Lane";
        public const string HarrowAwayName = "Residue - Harrow away";

        /// <summary>Day 2, 08:00: the next morning in the slice, whose POIs all fall on day 1.</summary>
        public const long NextMorning = 1440 + 480;

        private const string HarrowStreetDoor = "harrow-apothecary.street";
        private const string MaterialFolder = "Assets/Materials/Slice/Residue";

        // Authoring defaults, m: the seal's height on the door, how far out and along the street the Warden pairs
        // stand, and how near Loom Lane a woman's walk must come for her to stay at home.
        private const float SealHeight = 1.3f;
        private const float LeafToDoor = 1.5f;
        private const float PairsOut = 3.5f;
        private static readonly float[] PairsAlong = { -7f, 9f };
        private const float LaneFear = 30f;

        [MenuItem("GameDev/Slice/Residue/Place the next day's residue in Veilmouth and the apothecary (once)")]
        public static void PlaceAll()
        {
            PlaceInVeilmouth();
            PlaceInTheApothecary();
        }

        private static void PlaceInVeilmouth()
        {
            var scene = EditorSceneManager.OpenScene(SlicePeople.ScenePath(SliceRoster.Veilmouth), OpenSceneMode.Single);
            if (Object.FindObjectsByType<WorldResidue>(FindObjectsInactive.Include).Any(r => r.name == SealedName))
            {
                Debug.Log("[SliceResidue] Veilmouth already has its residue; left as it is.");
                return;
            }

            var portal = Object.FindObjectsByType<Portal>(FindObjectsInactive.Include).FirstOrDefault(p => p.Id == HarrowStreetDoor);
            var poiB = AssetDatabase.LoadAssetAtPath<PoiDefinition>(SlicePoiStages.PoiBPath);
            if (portal == null || poiB == null)
            {
                Debug.LogError($"[SliceResidue] Veilmouth has no door '{HarrowStreetDoor}', or POI B is missing.");
                return;
            }

            Physics.SyncTransforms();
            var door = portal.transform.position + (portal.transform.right * LeafToDoor);
            var outward = SlicePoiStages.Flat(portal.transform.forward);
            if (!SlicePoiStages.Free(door, door + (outward * 2.4f)))
            {
                outward = -outward;
            }

            Sealed(door, outward, portal);
            Pairs(door, outward);
            Fear(poiB.Place);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>A disc of red wax with a brass seal on it and a cord across the leaf, on the door's outer face.</summary>
        private static void Sealed(Vector3 door, Vector3 outward, Portal portal)
        {
            var notCharacters = ~LayerMask.GetMask(KitLayers.Character);
            var from = door + (outward * 1.5f) + (Vector3.up * SealHeight);
            var face = Physics.Raycast(from, -outward, out var hit, 3f, notCharacters, QueryTriggerInteraction.Ignore)
                ? hit.point
                : door + (Vector3.up * SealHeight);

            var root = new GameObject(SealedName);
            root.transform.position = door;
            var seal = new GameObject("Seal");
            seal.transform.SetParent(root.transform, false);
            var wax = Material("Wax", new Color(0.45f, 0.05f, 0.04f), 0f, 0.35f);
            var brass = Material("Brass", new Color(0.71f, 0.53f, 0.22f), 0.85f, 0.6f);
            var facing = Quaternion.FromToRotation(Vector3.up, outward);
            Piece(seal, PrimitiveType.Cube, "Sealing cord", face + (outward * 0.008f), Quaternion.LookRotation(outward), new Vector3(1.1f, 0.012f, 0.012f), wax);
            Piece(seal, PrimitiveType.Cylinder, "Wax", face + (outward * 0.014f), facing, new Vector3(0.2f, 0.008f, 0.2f), wax);
            Piece(seal, PrimitiveType.Cylinder, "Brass seal", face + (outward * 0.026f), facing, new Vector3(0.1f, 0.005f, 0.1f), brass);
            seal.SetActive(false);

            root.AddComponent<WorldResidue>().Configure(new[] { new StateCondition("aftermath.harrow.shop_sealed") }, 0L,
                new[] { seal }, new GameObject[0], new[] { portal });
            Debug.Log($"[SliceResidue] The seal on Harrow & Tincture's door at {face}.");
        }

        /// <summary>Two pairs of Wardens on the street, either side of the shop, facing its door.</summary>
        private static void Pairs(Vector3 door, Vector3 outward)
        {
            var along = Vector3.Cross(Vector3.up, outward);
            var root = new GameObject(PairsName);
            root.transform.position = door;
            var pairs = new GameObject("Pairs");
            pairs.transform.SetParent(root.transform, false);
            var n = 0;
            foreach (var step in PairsAlong)
            {
                n++;
                var middle = FreeNear(door, door + (outward * PairsOut) + (along * step), outward, along);
                var yaw = SlicePoiStages.Yaw(SlicePoiStages.Flat(door - middle));
                SlicePoiStages.Person(pairs, "Blue", SlicePoiStages.Feet(door, middle - (along * 0.45f)), yaw, $"Residue - Warden pair {n} (left)");
                SlicePoiStages.Person(pairs, "Blue", SlicePoiStages.Feet(door, middle + (along * 0.45f)), yaw, $"Residue - Warden pair {n} (right)");
                Debug.Log($"[SliceResidue] Warden pair {n} at {middle}.");
            }

            pairs.SetActive(false);
            root.AddComponent<WorldResidue>().Configure(new[] { new StateCondition("aftermath.harrow.taken") }, NextMorning,
                new[] { pairs }, new GameObject[0], new Portal[0]);
        }

        /// <summary>The first place near <paramref name="wanted"/> where two people stand side by side.</summary>
        private static Vector3 FreeNear(Vector3 door, Vector3 wanted, Vector3 outward, Vector3 along)
        {
            foreach (var outStep in new[] { 0f, 1f, -0.7f, 2f })
            {
                foreach (var alongStep in new[] { 0f, 2f, -2f, 4f, -4f })
                {
                    var at = wanted + (outward * outStep) + (along * alongStep);
                    if (SlicePoiStages.Free(door, at - (along * 0.45f)) && SlicePoiStages.Free(door, at + (along * 0.45f)))
                    {
                        return at;
                    }
                }
            }

            Debug.LogWarning($"[SliceResidue] No free place for a Warden pair near {wanted}; placed there anyway.");
            return wanted;
        }

        /// <summary>The townswomen whose walk comes within <see cref="LaneFear"/> m of Loom Lane stay at home.</summary>
        private static void Fear(Vector3 lane)
        {
            var women = Object.FindObjectsByType<TownsfolkWalker>(FindObjectsInactive.Include)
                .Where(w => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(w.gameObject) == SliceTownsfolk.WomanPrefabPath)
                .Where(w => w.Route.Append(w.transform.position).Any(p => Flat(p - lane).magnitude <= LaneFear))
                .Select(w => w.gameObject)
                .ToArray();
            if (women.Length == 0)
            {
                Debug.LogWarning($"[SliceResidue] No townswoman walks within {LaneFear} m of Loom Lane; its fear shows only in talk.");
            }

            var root = new GameObject(FearName);
            root.transform.position = lane;
            root.AddComponent<WorldResidue>().Configure(new[] { new StateCondition("aftermath.loom_lane.girls_taken") }, NextMorning,
                new GameObject[0], women, new Portal[0]);
            Debug.Log($"[SliceResidue] Fear on Loom Lane keeps at home: {string.Join(", ", women.Select(w => w.name))}.");
        }

        private static void PlaceInTheApothecary()
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthDressing.HarrowScenePath, OpenSceneMode.Single);
            if (Object.FindObjectsByType<WorldResidue>(FindObjectsInactive.Include).Any(r => r.name == HarrowAwayName))
            {
                Debug.Log("[SliceResidue] The apothecary already has its residue; left as it is.");
                return;
            }

            var harrow = Object.FindObjectsByType<NamedPerson>(FindObjectsInactive.Include).FirstOrDefault(p => p.Id == "harrow");
            if (harrow == null)
            {
                Debug.LogError("[SliceResidue] Harrow does not stand in his apothecary.");
                return;
            }

            var root = new GameObject(HarrowAwayName);
            root.AddComponent<WorldResidue>().Configure(
                new[]
                {
                    new StateCondition("poi.a.phase", (int)PoiPhase.Open, (int)PoiPhase.Open),
                    new StateCondition("aftermath.harrow.taken"),
                    new StateCondition("aftermath.harrow.vanished"),
                },
                0L, new GameObject[0], new[] { harrow.gameObject }, new Portal[0]);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SliceResidue] Harrow leaves his apothecary while held in the street, and once taken or gone.");
        }

        private static void Piece(GameObject parent, PrimitiveType type, string name, Vector3 at, Quaternion rotation, Vector3 scale, Material material)
        {
            var piece = GameObject.CreatePrimitive(type);
            piece.name = name;
            Object.DestroyImmediate(piece.GetComponent<Collider>());
            piece.transform.SetParent(parent.transform, true);
            piece.transform.SetPositionAndRotation(at, rotation);
            piece.transform.localScale = scale;
            piece.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Material Material(string name, Color colour, float metallic, float smoothness)
        {
            var path = $"{MaterialFolder}/Residue_{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            if (!AssetDatabase.IsValidFolder(MaterialFolder))
            {
                AssetDatabase.CreateFolder("Assets/Materials/Slice", "Residue");
            }

            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = $"Residue_{name}" };
            material.SetColor("_BaseColor", colour);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
