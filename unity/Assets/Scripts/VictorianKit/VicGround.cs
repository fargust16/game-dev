using UnityEngine;

namespace GameDev.VictorianKit
{
    /// <summary>
    /// The ground the facade shader measures its plinth dirt and wall-foot moss from (reference plan R3 step 2): the
    /// scene's terrain heightmap, given to the shader as globals; the eaves map, each house's wall top seen from
    /// above, under which the shader gathers soot at the cornice; and the sill map, each column of windows seen from
    /// above, under whose sills the shader runs stains down the wall. Without them, as in a lab at world height 0 or in an
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
        public static readonly int EavesId = Shader.PropertyToID("_VicEaves");
        public static readonly int EavesRectId = Shader.PropertyToID("_VicEavesRect");
        public static readonly int EavesLevelId = Shader.PropertyToID("_VicEavesLevel");
        public static readonly int SillsId = Shader.PropertyToID("_VicSills");
        public static readonly int SillsRectId = Shader.PropertyToID("_VicSillsRect");
        public static readonly int SillsLevelId = Shader.PropertyToID("_VicSillsLevel");

        /// <summary>The eaves map's 8-bit texel: 0 where no house stands, else a wall top of base + texel × step, m.</summary>
        public const float EavesBase = 20f, EavesStep = 0.25f;

        /// <summary>
        /// The sill map's 8-bit heights: 0 for none, else base + texel × step, m. Above a column's second sill every sill
        /// stands one kit storey higher than the one under it, 3.75 m at ×1.25 (measured over the city's 3827 panes).
        /// </summary>
        public const float SillsBase = 20f, SillsStep = 0.125f, Storey = 3.75f;

        // Unity's heightmap texture keeps a height of the terrain's full size at 32766/65535 (TerrainLit's kMaxHeight).
        private const float MaxHeight = 32766f / 65535f;

        public Terrain Terrain;

        [Tooltip("Each house's wall top in metres, seen from above; 0 where no house stands.")]
        public Texture2D Eaves;

        [Tooltip("Where the eaves map lies: (x0, z0, 1 / size x, 1 / size z), in metres.")]
        public Vector4 EavesRect;

        [Tooltip("Each column of windows seen from above: its lowest, second and highest sill in R, G and B, how much of the texel it covers in A.")]
        public Texture2D Sills;

        [Tooltip("Where the sill map lies: (x0, z0, 1 / size x, 1 / size z), in metres.")]
        public Vector4 SillsRect;

        private void OnEnable() => Apply();

        private void Update() => Apply(); // the heightmap's texture may be made again after an edit

        private void OnDisable()
        {
            Shader.SetGlobalVector(LevelId, Vector4.zero);
            Shader.SetGlobalVector(EavesRectId, Vector4.zero);
            Shader.SetGlobalVector(SillsRectId, Vector4.zero);
        }

        /// <summary>Gives the shader the terrain's heightmap, the eaves map and the sill map, and where they lie; without them it measures from 0 and gathers no soot at the cornice or under the sills.</summary>
        public void Apply()
        {
            if (Sills != null)
            {
                Shader.SetGlobalTexture(SillsId, Sills);
                Shader.SetGlobalVector(SillsRectId, SillsRect);
                Shader.SetGlobalVector(SillsLevelId, new Vector4(SillsBase, SillsStep * 255f, Storey, 0f));
            }
            else
            {
                Shader.SetGlobalVector(SillsRectId, Vector4.zero);
            }

            if (Eaves != null)
            {
                Shader.SetGlobalTexture(EavesId, Eaves);
                Shader.SetGlobalVector(EavesRectId, EavesRect);
                Shader.SetGlobalVector(EavesLevelId, new Vector4(EavesBase, EavesStep * 255f, 0f, 0f));
            }
            else
            {
                Shader.SetGlobalVector(EavesRectId, Vector4.zero);
            }

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
