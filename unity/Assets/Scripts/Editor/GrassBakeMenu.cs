#if UNITY_EDITOR
using GameDev.World.Settlement;
using GameDev.World.Terrain;
using GameDev.World.Vegetation;
using GameDev.World.Water;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Configures GPU grass coverage (lazy per-chunk bake at draw time — no 3M matrix dump).
    /// </summary>
    public static class GrassBakeMenu
    {
        private const string GrassPrefabPath =
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Plants/PT_Grass_02.prefab";
        private const string GrassTexturePath =
            "Assets/Polytope Studio/Lowpoly_Environments/Sources/Textures/PT_Grass_01.png";
        private const string MaterialAssetPath = "Assets/Settings/OverworldGrass.mat";

        public static GrassField Bake(
            IBiomeSampler sampler,
            UnityEngine.Terrain terrain,
            IWaterSampler water,
            VegetationClearance clearance,
            VegetationEcology.NoiseOffsets offsets,
            float minX,
            float minZ,
            float sizeX,
            float sizeZ,
            int worldSeed)
        {
            // sampler/water/clearance are resolved again at draw time from the scene.
            _ = sampler;
            _ = terrain;
            _ = water;
            _ = clearance;

            GrassField.ClearInScene();

            if (!TryLoadGrassMesh(out var mesh))
            {
                Debug.LogError("[Grass] Could not load PT_Grass_02 LOD mesh.");
                return null;
            }

            var material = LoadOrCreateMaterial();
            if (material == null)
            {
                Debug.LogError(
                    "[Grass] Could not create OverworldGrass material (shader GameDev/Overworld/Grass missing?).");
                return null;
            }

            var field = GrassField.FindOrCreate();
            field.ConfigureBake(mesh, material, worldSeed, minX, minZ, sizeX, sizeZ, offsets);
            EditorUtility.SetDirty(field);
            EditorUtility.SetDirty(field.gameObject);

            // Warm nearby chunks so Scene view shows coverage without entering Play.
            var focus = new Vector3(minX + sizeX * 0.5f, 0f, minZ + sizeZ * 0.5f);
            var cam = SceneView.lastActiveSceneView != null
                ? SceneView.lastActiveSceneView.camera
                : null;
            if (cam != null)
            {
                focus = cam.transform.position;
            }

            field.WarmBakeAround(focus, GrassField.CullDistanceMeters);
            SceneView.RepaintAll();

            Debug.Log(
                $"[Grass] GPU coverage configured seed={worldSeed}: " +
                $"mesh={mesh.name}, material={material.name}, " +
                $"warmChunks={field.ChunkCount}, warmInstances={field.TotalInstances} " +
                $"(remaining chunks bake lazily near camera)");
            return field;
        }

        public static void Clear()
        {
            GrassField.ClearInScene();
        }

        private static bool TryLoadGrassMesh(out Mesh mesh)
        {
            mesh = null;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GrassPrefabPath);
            if (prefab == null)
            {
                return false;
            }

            var lod2 = prefab.transform.Find("PT_Grass_02_LOD2");
            if (lod2 != null)
            {
                var mf = lod2.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    mesh = mf.sharedMesh;
                    return true;
                }
            }

            var lod1 = prefab.transform.Find("PT_Grass_02_LOD1");
            if (lod1 != null)
            {
                var mf = lod1.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    mesh = mf.sharedMesh;
                    return true;
                }
            }

            var any = prefab.GetComponentInChildren<MeshFilter>();
            if (any != null && any.sharedMesh != null)
            {
                mesh = any.sharedMesh;
                return true;
            }

            return false;
        }

        private static Material LoadOrCreateMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialAssetPath);
            if (existing != null && existing.shader != null
                && existing.shader.name == "GameDev/Overworld/Grass")
            {
                existing.enableInstancing = true;
                EnsureTexture(existing);
                return existing;
            }

            var shader = Shader.Find("GameDev/Overworld/Grass");
            if (shader == null)
            {
                // Force import/compile discovery.
                var shaderAsset = AssetDatabase.LoadAssetAtPath<Shader>(
                    "Assets/Shaders/Grass/OverworldGrass.shader");
                if (shaderAsset != null)
                {
                    shader = shaderAsset;
                }
            }

            if (shader == null)
            {
                return null;
            }

            var mat = existing != null ? existing : new Material(shader);
            mat.shader = shader;
            mat.name = "OverworldGrass";
            mat.enableInstancing = true;
            mat.doubleSidedGI = true;
            mat.SetColor("_BaseColor", new Color(0.34f, 0.46f, 0.26f, 1f));
            mat.SetFloat("_Cutoff", 0.15f);
            mat.SetFloat("_ColorVariation", 0.28f);
            if (mat.HasProperty("_DryEdgeColor"))
            {
                mat.SetColor("_DryEdgeColor", new Color(0.42f, 0.36f, 0.22f, 1f));
            }
            EnsureTexture(mat);

            if (existing == null)
            {
                AssetDatabase.CreateAsset(mat, MaterialAssetPath);
            }
            else
            {
                EditorUtility.SetDirty(mat);
            }

            AssetDatabase.SaveAssets();
            return mat;
        }

        private static void EnsureTexture(Material mat)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(GrassTexturePath);
            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
            }
        }
    }
}
#endif
