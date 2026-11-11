using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// A cast-iron grate over each street tree's pit (VEILMOUTH_CITY_PLAN.md §43; SLICE_MODELS_WANTED.md, "Square planter
    /// or cast-iron tree grate for a street tree", reference plan R7.2). The street trees stood in bare earth inside their
    /// pits' stone edges. Over each pit's earth, inside its edge, lies a placeholder grate of the kit's iron: a square
    /// frame, a second square within it, eight bars from the frame to a ring round the trunk, and the ring, 0.03 m over
    /// the earth. One merged mesh, on NoGrid with no collider: the pit's own collider and the Combat Grid are as before.
    /// The pit is found under each street tree's trunk on the pits' collider; a tree with no pit under it is left as it
    /// is. Every size is an authoring default, in metres.
    /// </summary>
    public static partial class VeilmouthCity
    {
        private const string GratesName = "The tree grates", GrateIron = "VIC_Iron";

        // The grate's half width inside the pit's edge, its bars' width and height, the inner square's half width, and the
        // ring's radius round the trunk.
        private const float GrateHalf = VeilmouthPavements.PitSize / 2f - VeilmouthPavements.PitEdge - 0.02f, GrateBar = 0.045f, GrateHigh = 0.025f, GrateInner = 0.33f, GrateRing = 0.24f;

        /// <summary>
        /// Lays the saved Veilmouth scene's tree grates again, and saves the scene.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthCity.DressGrates
        /// </summary>
        public static void DressGrates()
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var city = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
            if (city == null)
            {
                Debug.LogError($"[VeilmouthCity] no {RootName} in the scene.");
                return;
            }

            GratesOf(city.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>Lays a grate in each street tree's pit under <paramref name="root"/>, replacing older ones.</summary>
        private static void GratesOf(Transform root)
        {
            var old = root.Find(GratesName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var trees = root.Find("Street Trees");
            var pits = Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(c => c.name == "Tree Pits");
            if (trees == null || pits == null)
            {
                Debug.Log("[VeilmouthCity] the tree grates: no street trees or no pits.");
                return;
            }

            Physics.SyncTransforms();
            var owner = new GameObject(GratesName).transform;
            owner.SetParent(root, false);
            var grates = new Works(owner, false);
            var laid = 0;
            foreach (var tree in trees.GetComponentsInChildren<Transform>(true))
            {
                var trunk = tree.Find("Model");
                if (!tree.name.StartsWith("Kit_Tree_") || trunk == null)
                {
                    continue;
                }

                var at = trunk.position;
                if (!pits.Raycast(new Ray(at + (Vector3.up * 5f), Vector3.down), out var hit, 10f) || Mathf.Abs(hit.normal.y) < 0.95f)
                {
                    continue;
                }

                Grate(grates, new Vector3(at.x, hit.point.y + 0.03f - (GrateHigh / 2f), at.z), 0f); // the pits lie square to the plan
                laid++;
            }

            grates.Finish(GrateIron);
            Debug.Log($"[VeilmouthCity] the tree grates: {laid} laid in the street trees' pits.");
        }

        /// <summary>One grate centred on <paramref name="c"/> (its bars' middle height), square to <paramref name="yaw"/>.</summary>
        private static void Grate(Works grates, Vector3 c, float yaw)
        {
            var turn = Quaternion.Euler(0f, yaw, 0f);
            Vector3 At(float x, float z) => c + (turn * new Vector3(x, 0f, z));
            foreach (var half in new[] { GrateHalf - (GrateBar / 2f), GrateInner })
            {
                grates.Box(At(0f, half), new Vector3((half * 2f) + GrateBar, GrateHigh, GrateBar), yaw, false);
                grates.Box(At(0f, -half), new Vector3((half * 2f) + GrateBar, GrateHigh, GrateBar), yaw, false);
                grates.Box(At(half, 0f), new Vector3(GrateBar, GrateHigh, half * 2f), yaw, false);
                grates.Box(At(-half, 0f), new Vector3(GrateBar, GrateHigh, half * 2f), yaw, false);
            }

            // Eight bars from the ring to the frame, along the sides' middles and the diagonals, and the ring of eight.
            for (var k = 0; k < 8; k++)
            {
                var a = k * 45f;
                var far = (k % 2 == 0 ? GrateHalf : GrateHalf * Mathf.Sqrt(2f)) - GrateBar;
                var dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                var mid = (GrateRing + far) / 2f;
                grates.Box(c + (turn * (dir * mid)), new Vector3(GrateBar, GrateHigh, far - GrateRing), yaw + a, false);
                var side = 2f * GrateRing * Mathf.Tan(22.5f * Mathf.Deg2Rad);
                var ringDir = Quaternion.Euler(0f, a + 22.5f, 0f) * Vector3.forward;
                grates.Box(c + (turn * (ringDir * (GrateRing * Mathf.Cos(22.5f * Mathf.Deg2Rad)))), new Vector3(side + GrateBar, GrateHigh, GrateBar), yaw + a + 22.5f, false);
            }
        }
    }
}
