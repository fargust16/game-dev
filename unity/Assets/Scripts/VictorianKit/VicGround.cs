using UnityEngine;

namespace GameDev.VictorianKit
{
    /// <summary>
    /// The ground the facade shader measures its plinth dirt and wall-foot moss from (reference plan R3 step 2): the
    /// scene's terrain heightmap, given to the shader as globals. Without it, as in a lab at world height 0 or in an
    /// interior, the shader measures from world height 0 as before. Veilmouth's streets lie at 26 m, so there the
    /// dirt and the moss never showed until the ground was given. It changes no material, so the SRP Batcher keeps
    /// every facade in one batch.
    /// </summary>
    [ExecuteAlways]
    public sealed class VicGround : MonoBehaviour
    {
        public static readonly int HeightId = Shader.PropertyToID("_VicGroundHeight");
        public static readonly int RectId = Shader.PropertyToID("_VicGroundRect");
        public static readonly int LevelId = Shader.PropertyToID("_VicGroundLevel");

        // Unity's heightmap texture keeps a height of the terrain's full size at 32766/65535 (TerrainLit's kMaxHeight).
        private const float MaxHeight = 32766f / 65535f;

        public Terrain Terrain;

        private void OnEnable() => Apply();

        private void Update() => Apply(); // the heightmap's texture may be made again after an edit

        private void OnDisable() => Shader.SetGlobalVector(LevelId, Vector4.zero);

        /// <summary>Gives the shader the terrain's heightmap and where it lies; with no terrain the shader measures from 0.</summary>
        public void Apply()
        {
            var data = Terrain != null ? Terrain.terrainData : null;
            var heights = data != null ? data.heightmapTexture : null;
            if (heights == null)
            {
                Shader.SetGlobalVector(LevelId, Vector4.zero);
                return;
            }

            var (rect, level) = Globals(Terrain.GetPosition(), data.size, data.heightmapResolution);
            Shader.SetGlobalTexture(HeightId, heights);
            Shader.SetGlobalVector(RectId, rect);
            Shader.SetGlobalVector(LevelId, level);
        }

        /// <summary>
        /// The shader's two vectors for a terrain at <paramref name="position"/> of <paramref name="size"/>: Rect maps
        /// world XZ to the heightmap's texel centres, (x0, z0, scale x, scale z); Level is (base height, height scale,
        /// 1 for on, half a texel), so the ground's height is base + texel × scale.
        /// </summary>
        public static (Vector4 Rect, Vector4 Level) Globals(Vector3 position, Vector3 size, int resolution)
        {
            var texels = (resolution - 1f) / resolution;
            var rect = new Vector4(position.x, position.z, texels / size.x, texels / size.z);
            var level = new Vector4(position.y, size.y / MaxHeight, 1f, 0.5f / resolution);
            return (rect, level);
        }
    }
}
