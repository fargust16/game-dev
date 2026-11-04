using GameDev.Slice.Kit;
using UnityEditor;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Builds the kit's stand-ins (SLICE_MODULAR_KIT_STANDARD §9): plain boxes at a block's exact size, collider, layer,
    /// and name, pivoted on a grid node. A linear block (wall, fence) lies on its grid line and stops half its thickness
    /// short of each node; a post fills the node where walls meet (§2).
    /// </summary>
    public static class KitStandIns
    {
        public const string Folder = "Assets/Prefabs/SliceKit/_StandIn";

        /// <summary>Kit standard §2: walls are at most 0.3 m thick.</summary>
        public const float WallThickness = 0.3f;

        /// <summary>Authoring default, m.</summary>
        public const float FenceThickness = 0.1f;

        [MenuItem("GameDev/Slice/Create Kit Stand-ins")]
        public static void CreateAll()
        {
            EnsureFolder(Folder);
            Save(Linear("Kit_Wall_StandIn_4x3", KitLayers.Structure, 4f, 3f, WallThickness));
            Save(Linear("Kit_Wall_StandIn_2x3", KitLayers.Structure, 2f, 3f, WallThickness));
            Save(Post("Kit_Post_StandIn_0x3", 3f));
            Save(Doorway("Kit_Door_StandIn_3x3"));
            Save(Linear("Kit_Fence_StandIn_2x1", KitLayers.Prop, 2f, 1f, FenceThickness));
            Save(Crate("Kit_Crate_StandIn_1x1"));
            Save(Street("Kit_Street_StandIn_4x4", 4f));
            Save(Trunk("Kit_Tree_StandIn_1x4", 4f));
            AssetDatabase.SaveAssets();
            Debug.Log($"Kit stand-ins written to {Folder}.");
        }

        /// <summary>A wall or fence along local +X on the grid line through the pivot.</summary>
        public static GameObject Linear(string name, string layer, float length, float height, float thickness)
        {
            var root = Root(name, layer);
            var half = thickness * 0.5f;
            Box(root, new Vector3(half, 0f, -half), new Vector3(length - half, height, half));
            return root;
        }

        /// <summary>A post that fills the grid node at the pivot.</summary>
        public static GameObject Post(string name, float height)
        {
            var root = Root(name, KitLayers.Structure);
            var half = WallThickness * 0.5f;
            Box(root, new Vector3(-half, 0f, -half), new Vector3(half, height, half));
            return root;
        }

        /// <summary>
        /// A 3 m wall with a 1.2 × 2.2 m opening over its middle cell edge (kit standard §3), so one cell passes.
        /// The leaf, which opens, comes with the portal door.
        /// </summary>
        public static GameObject Doorway(string name)
        {
            var root = Root(name, KitLayers.Structure);
            var half = WallThickness * 0.5f;
            const float openingLeft = 0.9f, openingRight = 2.1f, openingTop = 2.2f, height = 3f;
            Box(root, new Vector3(half, 0f, -half), new Vector3(openingLeft, height, half));
            Box(root, new Vector3(openingRight, 0f, -half), new Vector3(3f - half, height, half));
            Box(root, new Vector3(openingLeft, openingTop, -half), new Vector3(openingRight, height, half));
            return root;
        }

        /// <summary>A crate that fills the cell beyond the pivot's node.</summary>
        public static GameObject Crate(string name)
        {
            var root = Root(name, KitLayers.Prop);
            Box(root, Vector3.zero, Vector3.one);
            return root;
        }

        /// <summary>A street tile whose top is the pivot's height.</summary>
        public static GameObject Street(string name, float size)
        {
            var root = Root(name, KitLayers.Ground);
            Box(root, new Vector3(0f, -0.1f, 0f), new Vector3(size, 0f, size));
            return root;
        }

        /// <summary>A tree trunk of radius 0.3 m in the middle of the cell beyond the pivot's node: Full Cover.</summary>
        public static GameObject Trunk(string name, float height)
        {
            var root = Root(name, KitLayers.Vegetation);
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            trunk.name = "Trunk";
            trunk.layer = root.layer;
            trunk.transform.SetParent(root.transform, false);
            trunk.transform.localPosition = new Vector3(0.5f, height * 0.5f, 0.5f);
            trunk.transform.localScale = new Vector3(0.6f, height * 0.5f, 0.6f);
            return root;
        }

        private static GameObject Root(string name, string layer)
        {
            var root = new GameObject(name);
            root.layer = LayerMask.NameToLayer(layer);
            root.AddComponent<KitBlock>();
            return root;
        }

        private static void Box(GameObject root, Vector3 min, Vector3 max)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Box";
            box.layer = root.layer;
            box.transform.SetParent(root.transform, false);
            box.transform.localPosition = (min + max) * 0.5f;
            box.transform.localScale = max - min;
        }

        private static void Save(GameObject root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>())
            {
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
            }

            PrefabUtility.SaveAsPrefabAsset(root, $"{Folder}/{root.name}.prefab");
            Object.DestroyImmediate(root);
        }

        private static void EnsureFolder(string path)
        {
            var parent = "Assets";
            foreach (var part in path.Substring("Assets/".Length).Split('/'))
            {
                var next = $"{parent}/{part}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(parent, part);
                }

                parent = next;
            }
        }
    }
}
