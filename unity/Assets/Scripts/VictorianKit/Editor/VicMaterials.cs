using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameDev.VictorianKit.EditorTools
{
    /// <summary>
    /// The variants of the kit's seven slots on two shaders (Plans/VICTORIAN_STREETS_PLAN.md §4.3), and the palettes
    /// that map a house's slots to them. Tint and roughness live in each material asset: no property blocks, so every
    /// facade stays in the SRP Batcher.
    /// </summary>
    public static class VicMaterials
    {
        public const string FacadeShader = "GameDev/VictorianKit/Facade";
        public const string WindowShader = "GameDev/VictorianKit/Window";

        /// <summary>The kit's slot names as its materials carry them.</summary>
        public static readonly string[] KitSlots = { "Brick", "Stucco", "Stone", "Slate", "Wood", "Iron", "Glass" };

        /// <summary>A facade variant: which texture set, its rapport tiling, and the material's numbers.</summary>
        private sealed class Spec
        {
            public string Set;
            public Color Tint = Color.white;
            public Color Tint2 = Color.white;
            public float Tint2Amount;
            public float Tint2Sharp = 1f; // 1 keeps the mask linear, so mip levels blend brick and mortar by area.
            public float Rough = 1f;
            public float Rough2 = 0.9f;
            public float Metallic;
            public float Bump = 1f;
            public float Dirt = 0.3f;
            public float DirtHeight = 2.5f;
            public float Moss = 0.1f;
            public float Tone = 0.08f;
            public Vector2 Tiling = new Vector2(-1f, -1f);
            public bool WorldUV;
            public float Puddles; // the street's paving only (reference plan R3 step 3)
        }

        /// <summary>
        /// The tint that turns a texture of linear mean <paramref name="mean"/> into the sRGB look (r, g, b): the shader
        /// multiplies in linear space, so the division happens there, and the tint is stored back as sRGB.
        /// </summary>
        private static Color Over(float r, float g, float b, float mean) => new Color(Lin(r, mean), Lin(g, mean), Lin(b, mean), 1f);

        private static float Lin(float srgb, float mean) => Mathf.LinearToGammaSpace(Mathf.Clamp01(Mathf.GammaToLinearSpace(srgb) / mean));

        private static readonly Dictionary<string, Spec> Facades = new Dictionary<string, Spec>
        {
            // The reference palette (Plans/VEILMOUTH_REFERENCE_PLAN.md R3 step 2; WORLD_STYLE_GUIDE.md §2), for the Linear
            // colour space: soot-browned brick, greyed cream, cold slate, dark paint. No look is above 40 % saturation
            // (HSV), so the grade, not the albedo, sets the warmth. Every value is an authoring default.

            // Brick: the texture's bricks average about 0.74 linear, its mortar 0.88. The mortar is soot-grey, never white.
            { "Brick_Rich", new Spec { Set = "Brick", Tint = Over(0.40f, 0.30f, 0.26f, 0.74f), Tint2 = Over(0.62f, 0.60f, 0.55f, 0.88f), Tint2Amount = 1f, Dirt = 0.4f, Moss = 0.04f } },
            { "Brick_Common", new Spec { Set = "Brick", Tint = Over(0.36f, 0.27f, 0.235f, 0.74f), Tint2 = Over(0.52f, 0.50f, 0.46f, 0.88f), Tint2Amount = 1f, Dirt = 0.6f, Moss = 0.12f } },
            { "Brick_Poor", new Spec { Set = "Brick", Tint = Over(0.28f, 0.22f, 0.19f, 0.74f), Tint2 = Over(0.38f, 0.36f, 0.33f, 0.88f), Tint2Amount = 1f, Rough = 1.08f, Dirt = 0.95f, DirtHeight = 3.2f, Moss = 0.35f, Tone = 0.14f } },
            { "Brick_Stock", new Spec { Set = "Brick", Tint = Over(0.40f, 0.36f, 0.29f, 0.74f), Tint2 = Over(0.48f, 0.46f, 0.42f, 0.88f), Tint2Amount = 1f, Rough = 1.05f, Dirt = 0.8f, DirtHeight = 3.0f, Moss = 0.28f, Tone = 0.12f } },

            // Stucco: mean 0.86; the score lines take a darker tint of the same render.
            // The palette sheet showed the noon sun and the warm grade adding about 12 points of saturation to a pale
            // surface, so stucco and stone start nearly grey and are lit warm.
            { "Stucco_Cream", new Spec { Set = "Stucco", Tint = Over(0.64f, 0.61f, 0.55f, 0.86f), Tint2 = Over(0.52f, 0.49f, 0.44f, 0.86f), Tint2Amount = 1f, Dirt = 0.45f, Moss = 0.04f, Tone = 0.05f } },
            { "Stucco_White", new Spec { Set = "Stucco", Tint = Over(0.66f, 0.65f, 0.62f, 0.86f), Tint2 = Over(0.54f, 0.53f, 0.50f, 0.86f), Tint2Amount = 1f, Dirt = 0.45f, Moss = 0.04f, Tone = 0.05f } },

            // Stone: linear mean 0.74; G collects grime in pits.
            { "Stone_Light", new Spec { Set = "Stone", Tint = Over(0.62f, 0.60f, 0.55f, 0.74f), Tint2 = Over(0.44f, 0.42f, 0.38f, 0.74f), Tint2Amount = 0.15f, Tint2Sharp = 3f, Dirt = 0.4f, Moss = 0.04f } },
            { "Stone_Medium", new Spec { Set = "Stone", Tint = Over(0.55f, 0.53f, 0.48f, 0.74f), Tint2 = Over(0.36f, 0.34f, 0.31f, 0.74f), Tint2Amount = 0.25f, Tint2Sharp = 3f, Dirt = 0.55f, Moss = 0.12f } },
            { "Stone_Dirty", new Spec { Set = "Stone", Tint = Over(0.47f, 0.45f, 0.41f, 0.74f), Tint2 = Over(0.26f, 0.25f, 0.23f, 0.74f), Tint2Amount = 0.4f, Tint2Sharp = 3f, Rough = 1.08f, Dirt = 0.85f, DirtHeight = 3.2f, Moss = 0.35f, Tone = 0.14f } },

            // Slate: linear mean 0.70; G picks odd slates for the second tint. Dry slate is matte, so roughness sits above the mask.
            { "Slate_Even", new Spec { Set = "Slate", Tint = Over(0.23f, 0.24f, 0.26f, 0.70f), Tint2 = Over(0.25f, 0.25f, 0.26f, 0.70f), Tint2Amount = 0.1f, Tint2Sharp = 40f, Rough = 1.15f, Dirt = 0.15f, Moss = 0.04f } },
            { "Slate_Common", new Spec { Set = "Slate", Tint = Over(0.21f, 0.22f, 0.23f, 0.70f), Tint2 = Over(0.23f, 0.24f, 0.22f, 0.70f), Tint2Amount = 0.15f, Tint2Sharp = 40f, Rough = 1.25f, Dirt = 0.3f, Moss = 0.15f } },
            { "Slate_Rough", new Spec { Set = "Slate", Tint = Over(0.18f, 0.185f, 0.19f, 0.70f), Tint2 = Over(0.22f, 0.19f, 0.19f, 0.70f), Tint2Amount = 0.3f, Tint2Sharp = 40f, Rough = 1.4f, Dirt = 0.5f, Moss = 0.5f, Tone = 0.14f } },

            // Wood: paint mean 0.85; G is the chip field, bare wood where the paint has gone.
            { "Wood_Rich", new Spec { Set = "Wood", Tint = Over(0.10f, 0.14f, 0.11f, 0.85f), Tint2 = Over(0.40f, 0.34f, 0.26f, 0.85f), Tint2Amount = 0f, Rough = 0.9f, Dirt = 0.15f, Moss = 0f } },
            { "Wood_Brown", new Spec { Set = "Wood", Tint = Over(0.20f, 0.16f, 0.125f, 0.85f), Tint2 = Over(0.30f, 0.26f, 0.21f, 0.85f), Tint2Amount = 0.06f, Tint2Sharp = 10f, Dirt = 0.35f, Moss = 0.02f } },
            { "Wood_Green", new Spec { Set = "Wood", Tint = Over(0.17f, 0.20f, 0.16f, 0.85f), Tint2 = Over(0.32f, 0.28f, 0.23f, 0.85f), Tint2Amount = 0.25f, Tint2Sharp = 10f, Rough = 1.3f, Dirt = 0.6f, Moss = 0.08f, Tone = 0.12f } },
            { "Wood_Oxblood", new Spec { Set = "Wood", Tint = Over(0.20f, 0.135f, 0.125f, 0.85f), Tint2 = Over(0.32f, 0.28f, 0.23f, 0.85f), Tint2Amount = 0.22f, Tint2Sharp = 10f, Rough = 1.3f, Dirt = 0.6f, Moss = 0.08f, Tone = 0.12f } },

            // Iron: mean 0.8; G is the rust field, a small share of the surface, so the rust alone may pass 40 %.
            { "Iron", new Spec { Set = "Iron", Tint = Over(0.07f, 0.075f, 0.08f, 0.8f), Tint2 = Over(0.30f, 0.19f, 0.13f, 0.8f), Tint2Amount = 0.12f, Tint2Sharp = 5f, Metallic = 0.55f, Dirt = 0.3f, Moss = 0f } },

            // Street ground, not a kit slot: flagstones from the stucco set's scoring, setts from the brick set.
            { "Ground_Pave", new Spec { Set = "Stucco", Tint = Over(0.52f, 0.50f, 0.47f, 0.86f), Tint2 = Over(0.22f, 0.21f, 0.20f, 0.86f), Tint2Amount = 1f, Rough = 1.1f, Bump = 2f, Dirt = 0.4f, Moss = 0.2f, Tone = 0.2f, Tiling = new Vector2(0.45f, 0.45f), WorldUV = true, Puddles = 1f } },
            { "Ground_Setts", new Spec { Set = "Brick", Tint = Over(0.36f, 0.35f, 0.34f, 0.74f), Tint2 = Over(0.18f, 0.17f, 0.16f, 0.88f), Tint2Amount = 1f, Rough = 1.05f, Bump = 1.5f, Dirt = 0.4f, Moss = 0.15f, Tone = 0.25f, Tiling = new Vector2(0.625f, 0.4167f), WorldUV = true } },
        };

        /// <summary>The window glass: rich clean, poor greyer and grimier, fewer rooms lit.</summary>
        private static readonly Dictionary<string, (float grime, float lit, float day, Color room, Color curtain)> Glasses =
            new Dictionary<string, (float, float, float, Color, Color)>
            {
                { "Glass_Rich", (0.08f, 0.55f, 0.5f, new Color(1f, 0.97f, 0.92f), new Color(0.50f, 0.14f, 0.12f)) },
                { "Glass_Poor", (0.35f, 0.35f, 0.38f, new Color(0.72f, 0.70f, 0.66f), new Color(0.48f, 0.44f, 0.36f)) },
            };

        public static IEnumerable<string> Names
        {
            get
            {
                foreach (var k in Facades.Keys)
                {
                    yield return k;
                }

                foreach (var k in Glasses.Keys)
                {
                    yield return k;
                }
            }
        }

        /// <summary>The built rooms' finishes, floor and ceiling, each the block VicRooms lines them with.</summary>
        private static readonly (string Property, string Block)[] RoomBlocks =
        {
            ("_WallDark", "Kit_ViRoom_Dark_3x3"), ("_WallPanel", "Kit_ViRoom_Panel_3x3"), ("_WallPaper", "Kit_ViRoom_Paper_3x3"),
            ("_FloorTone", "Kit_ViRoom_Floor_3x1"), ("_CeilingTone", "Kit_ViRoom_Ceiling_3x1"),
        };

        /// <summary>
        /// The colours the mapped rooms take (reference plan R2 step 4), measured: each room block's mean albedo, its
        /// main texture's linear mean times its colour, over the material of its largest part, given as sRGB for
        /// <see cref="Material.SetColor(string, Color)"/>. A block that is missing keeps the shader's default.
        /// </summary>
        private static IEnumerable<(string Property, Color Tone)> RoomTones()
        {
            foreach (var (property, block) in RoomBlocks)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{VicRooms.RoomFolder}/{block}.prefab");
                var part = prefab == null ? null : prefab.GetComponentsInChildren<MeshFilter>(true)
                    .Where(f => f.sharedMesh != null && f.GetComponent<MeshRenderer>() != null)
                    .SelectMany(f => Enumerable.Range(0, Mathf.Min(f.sharedMesh.subMeshCount, f.GetComponent<MeshRenderer>().sharedMaterials.Length))
                        .Select(s => (Material: f.GetComponent<MeshRenderer>().sharedMaterials[s], Size: f.sharedMesh.GetIndexCount(s))))
                    .Where(p => p.Material != null)
                    .OrderByDescending(p => p.Size)
                    .Select(p => p.Material)
                    .FirstOrDefault();
                if (part == null)
                {
                    Debug.LogWarning($"[VicKit] room tone: {block} or its material is missing.");
                    continue;
                }

                var map = part.HasProperty("_BaseMap") ? part.GetTexture("_BaseMap") : part.HasProperty("_MainTex") ? part.GetTexture("_MainTex") : null;
                var tint = part.HasProperty("_BaseColor") ? part.GetColor("_BaseColor") : part.HasProperty("_Color") ? part.GetColor("_Color") : Color.white;
                var mean = map != null ? VicTextures.LinearMean(map) : Color.white;
                var tone = (mean * tint.linear).gamma;
                tone.a = 1f;
                Debug.Log($"[VicKit] room tone {property}: {block} ({part.name}) sRGB {tone}");
                yield return (property, tone);
            }
        }

        public static string PathOf(string name) => $"{VicKitPaths.MaterialFolder}/VIC_{name}.mat";

        public static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>(PathOf(name));

        [MenuItem("GameDev/Victorian Kit/Build Materials")]
        public static void BuildAll()
        {
            VicTextures.EnsureAll();
            Directory.CreateDirectory(VicKitPaths.MaterialFolder);
            foreach (var pair in Facades)
            {
                var s = pair.Value;
                var m = Ensure(pair.Key, FacadeShader);
                var rapport = VicTextures.Rapport(s.Set);
                var tiling = s.Tiling.x > 0f ? s.Tiling : new Vector2(1f / rapport.x, 1f / rapport.y);
                Textures(m, s.Set, tiling);
                m.SetColor("_BaseColor", s.Tint);
                m.SetColor("_Tint2", s.Tint2);
                m.SetFloat("_Tint2Amount", s.Tint2Amount);
                m.SetFloat("_Tint2Sharp", s.Tint2Sharp);
                m.SetFloat("_Roughness", s.Rough);
                m.SetFloat("_Roughness2", s.Rough2);
                m.SetFloat("_Metallic", s.Metallic);
                m.SetFloat("_BumpScale", s.Bump);
                m.SetFloat("_DirtAmount", s.Dirt);
                m.SetFloat("_DirtHeight", s.DirtHeight);
                m.SetFloat("_MossAmount", s.Moss);
                m.SetFloat("_ToneVar", s.Tone);
                m.SetFloat("_WorldUV", s.WorldUV ? 1f : 0f);
                m.SetFloat("_Puddles", s.Puddles);
                EditorUtility.SetDirty(m);
            }

            var tones = RoomTones().ToList();
            foreach (var pair in Glasses)
            {
                var m = Ensure(pair.Key, WindowShader);
                Textures(m, "Glass", new Vector2(0.5f, 0.5f));
                m.SetFloat("_Grime", pair.Value.grime);
                m.SetFloat("_LitFraction", pair.Value.lit);
                m.SetFloat("_RoomDay", pair.Value.day);
                m.SetColor("_RoomTint", pair.Value.room);
                m.SetColor("_CurtainColor", pair.Value.curtain);
                foreach (var (property, tone) in tones)
                {
                    m.SetColor(property, tone);
                }

                // Transparent since the rooms behind were built (§4.4): the queue from the shader, less light through grimy glass.
                m.SetFloat("_Transmit", 0.85f - pair.Value.grime);
                m.renderQueue = -1;
                EditorUtility.SetDirty(m);
            }

            AssetDatabase.SaveAssets();
        }

        public static void EnsureAll()
        {
            foreach (var name in Names)
            {
                if (Load(name) == null)
                {
                    BuildAll();
                    return;
                }
            }
        }

        private static Material Ensure(string name, string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                throw new System.InvalidOperationException($"Shader {shaderName} is missing.");
            }

            var m = Load(name);
            if (m == null)
            {
                m = new Material(shader) { name = "VIC_" + name };
                AssetDatabase.CreateAsset(m, PathOf(name));
            }
            else if (m.shader != shader)
            {
                m.shader = shader;
            }

            return m;
        }

        private static void Textures(Material m, string set, Vector2 tiling)
        {
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(VicTextures.PathOf(set, "Base")));
            m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(VicTextures.PathOf(set, "Normal")));
            m.SetTexture("_MaskMap", AssetDatabase.LoadAssetAtPath<Texture2D>(VicTextures.PathOf(set, "Mask")));
            m.SetTextureScale("_BaseMap", tiling);
            m.SetTextureOffset("_BaseMap", Vector2.zero);
        }
    }

    /// <summary>A house's materials: one variant per kit slot, stucco for walls that ask for it.</summary>
    public sealed class VicPalette
    {
        public string Name;
        public string Brick, Stucco, Stone, Slate, Wood, Iron = "Iron", Glass;

        public static VicPalette Rich(string wall, string stucco = null) => new VicPalette
        {
            Name = "Rich", Brick = wall, Stucco = stucco, Stone = "Stone_Light", Slate = "Slate_Even", Wood = "Wood_Rich", Glass = "Glass_Rich",
        };

        public static VicPalette Common(string brick = "Brick_Common", string wood = "Wood_Brown") => new VicPalette
        {
            Name = "Common", Brick = brick, Stone = "Stone_Medium", Slate = "Slate_Common", Wood = wood, Glass = "Glass_Rich",
        };

        public static VicPalette Poor(string brick = "Brick_Poor", string wood = "Wood_Green") => new VicPalette
        {
            Name = "Poor", Brick = brick, Stone = "Stone_Dirty", Slate = "Slate_Rough", Wood = wood, Glass = "Glass_Poor",
        };

        /// <summary>A short key the street rule "neighbours differ" compares.</summary>
        public string Key => $"{Brick}|{Stucco}|{Wood}|{Slate}";

        /// <summary>The material for one of a piece's kit materials, or null to keep it.</summary>
        public Material For(VicPiece piece, Material kit)
        {
            if (kit == null)
            {
                return null;
            }

            var slot = kit.name.Replace("MAT_Vic_", string.Empty);
            var variant = slot switch
            {
                "Brick" => piece.Stucco && Stucco != null ? Stucco : Brick,
                "Stucco" => Stucco ?? Brick,
                "Stone" => Stone,
                "Slate" => Slate,
                "Wood" => Wood,
                "Iron" => Iron,
                "Glass" => Glass,
                _ => null,
            };
            return variant == null ? null : VicMaterials.Load(variant);
        }
    }
}
