#if UNITY_EDITOR
using Pinwheel.Vista;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Our vegetation content in Vista's template wrappers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The terrain graphs decide <b>where</b> vegetation goes (GreenAreaMask → cluster noise →
    /// Geometry Mask → Scatter → Tree/Detail outputs), exactly as Vista's own biome graphs do. These
    /// templates only say <b>what</b> gets placed — architecture from Vista, content from us.
    /// </para>
    /// <para>
    /// The two content packs are kept to separate visual layers on purpose: <b>Stylized Trees Pack</b>
    /// for canopy and shrubs, <b>Polytope Low Poly Environment</b> for ground cover and dressing.
    /// Mixing two art styles inside one layer is what makes a scene read as two games stitched
    /// together.
    /// </para>
    /// <para>
    /// An existing template asset is returned untouched. Prefab choice, sizes, shadows and render
    /// mode are Inspector settings on the asset, so re-running a graph rebuild must not stamp over
    /// them. Use the Recreate menu item to go back to the defaults on purpose.
    /// </para>
    /// </remarks>
    public static class VistaVegetationTemplates
    {
        public const string Folder = "Assets/Settings/Vista/Vegetation";
        public const string MeadowGrassPath = Folder + "/Meadow_DetailTemplate.asset";
        public const string UndergrowthBushPath = Folder + "/Undergrowth_TreeTemplate.asset";
        public const string RockPath = Folder + "/Rock_TreeTemplate.asset";

        /// <summary>Prefab generated from the grass mesh so Unity accepts it as a detail prototype.</summary>
        private const string GrassDetailPrefabPath = Folder + "/Overworld_GrassDetail.prefab";

        /// <summary>Instancing-enabled copy of the grass material, see <see cref="LoadOrCreateGrassMaterial"/>.</summary>
        private const string GrassMaterialPath = Folder + "/Overworld_GrassDetail.mat";

        /// <summary>
        /// Build the meadow template the way Vista's own sample does: a texture, not a mesh.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>VistaSample_GrassDetail</c> is <c>DetailRenderMode.Grass</c> with a texture and
        /// instancing off; ours was <c>VertexLit</c> with a Polytope prefab and instancing on. Those
        /// are two different renderers inside Unity's terrain, and the texture one is the path
        /// Vista's own content exercises — so it is the honest control for "is the problem our grass
        /// or Vista's writer".
        /// </para>
        /// <para>
        /// The control has been run, and it came back conclusive: with Vista's own texture template
        /// and the relay off, the terrain still received zero detail instances. The render mode is
        /// not what loses the grass, so this is back on the Polytope clump — flip it to
        /// <see langword="true"/> to re-run the comparison.
        /// </para>
        /// </remarks>
        /// <remarks>
        /// <c>static readonly</c> rather than <c>const</c> so the branch it guards stays reachable
        /// to the compiler. A <c>const false</c> makes the other half of every <c>if</c> dead code,
        /// and the point of keeping both paths is that flipping one value re-runs the comparison —
        /// which is worth two suppressed warnings, not two deleted branches.
        /// </remarks>
        private static readonly bool UseVistaSampleGrass = false;

        private const string VistaSampleGrassTexture =
            "Assets/PinwheelStudio/Vista/Personal/Samples/Textures/VistaSample_GrassClump.png";

        /// <summary>
        /// Detail size band, as a multiple of the source mesh — <b>not</b> metres.
        /// </summary>
        /// <remarks>
        /// <c>DetailPrototype.minHeight</c> and friends scale the prototype mesh; they are not a
        /// height in world units. Feeding metres into them (0.05 for "5 cm grass") asks Unity for
        /// instances at 5 % of the mesh, which is a few centimetres of geometry lost in the terrain
        /// texture. The Polytope clump is already authored at a sensible ground-cover size, so the
        /// band sits around 1.
        /// </remarks>
        private const float GrassScaleMin = 0.7f;
        private const float GrassScaleMax = 1.3f;

        /// <summary>The metres-as-multiplier values shipped before the fix, used to migrate assets.</summary>
        private const float LegacyGrassScaleMin = 0.05f;
        private const float LegacyGrassScaleMax = 1f;

        /// <summary>Canopy species. Each gets its own template and its own cluster mask.</summary>
        /// <remarks>
        /// Vista's <c>prefabVariants</c> mixes prefabs inside one template, which is right for
        /// variation <em>within</em> a species but wrong across species: it would sprinkle all of them
        /// evenly. Separate templates let the graph give each species its own patches.
        /// </remarks>
        /// <remarks>
        /// <para>
        /// The colour variants are separate species rather than <c>prefabVariants</c> entries
        /// because <b>Vista Personal does not support template variants at all</b>.
        /// <c>TreeTemplate.prefabVariants</c> runs every access through
        /// <c>TemplateUtils.IsVariantsSupported()</c>, which returns true only when a runtime
        /// component has registered <c>enableVariantsSupportCallback</c> — and nothing in the
        /// Personal package ever registers it. So the getter returns an empty array however the
        /// field is set, and every template contributes exactly one prototype. The layer dump
        /// confirmed it: five templates, five prototypes, with the orchard's pears and plums
        /// nowhere.
        /// </para>
        /// <para>
        /// Being separate species is not a workaround with a cost — each one gets its own cluster
        /// noise, so the tints form interleaved stands instead of a per-tree lottery, which is
        /// closer to how one part of a wood differs from another anyway.
        /// </para>
        /// </remarks>
        public enum Species
        {
            Pine,
            PinePale,
            Fruit,
            FruitDeep,
            Orchard,
        }

        private const string PolytopeRoot = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs";
        private const string GrassSourcePath = PolytopeRoot + "/Plants/PT_Grass_02.prefab";

        [MenuItem("GameDev/Overworld/Vista/Create Vegetation Templates (if missing)")]
        public static void CreateIfMissingMenu()
        {
            var report = new System.Text.StringBuilder("[Vegetation] Vista templates ready:");
            foreach (Species species in System.Enum.GetValues(typeof(Species)))
            {
                var template = LoadOrCreateCanopyTree(species);
                report.Append($" {species}={(template != null && template.IsValid())}");
            }

            var bush = LoadOrCreateUndergrowthBush();
            var rock = LoadOrCreateRock();
            var grass = LoadOrCreateMeadowGrass();
            report.Append($" bush={(bush != null && bush.IsValid())}");
            report.Append($" rock={(rock != null && rock.IsValid())}");
            report.Append($" grass={(grass != null && grass.IsValid())}");
            Debug.Log(report.ToString());
        }

        [MenuItem("GameDev/Overworld/Vista/Recreate Vegetation Templates (destructive)")]
        public static void RecreateMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Recreate the Vista vegetation templates?",
                    "This deletes the canopy, undergrowth, rock and grass templates and rebuilds them " +
                    "from code defaults. Inspector edits on those assets are lost.",
                    "Recreate",
                    "Cancel"))
            {
                return;
            }

            foreach (Species species in System.Enum.GetValues(typeof(Species)))
            {
                AssetDatabase.DeleteAsset(CanopyTreePath(species));
            }

            AssetDatabase.DeleteAsset(TintFolder);
            AssetDatabase.DeleteAsset(UndergrowthBushPath);
            AssetDatabase.DeleteAsset(RockPath);
            AssetDatabase.DeleteAsset(MeadowGrassPath);
            AssetDatabase.DeleteAsset(GrassDetailPrefabPath);
            AssetDatabase.DeleteAsset(GrassMaterialPath);
            CreateIfMissingMenu();
        }

        private static string CanopyTreePath(Species species)
        {
            return $"{Folder}/{species}_TreeTemplate.asset";
        }

        /// <summary>
        /// Prefab paths per species, most representative first — that one becomes the main prefab.
        /// </summary>
        /// <remarks>
        /// Polytope only, on purpose. The Stylized Trees Pack is authored as nested prefab instances
        /// with no renderer on the root, which Unity rejects as a tree prototype — see the playbook.
        /// Polytope prefabs carry a root LOD Group and work.
        /// </remarks>
        /// <summary>
        /// The prefab a species is built from. One per species — see the note on <see cref="Species"/>
        /// for why listing several here would be pointless.
        /// </summary>
        private static string SpeciesPrefabPath(Species species)
        {
            switch (species)
            {
                case Species.Pine:
                case Species.PinePale:
                    return PolytopeRoot + "/Trees/PT_Pine_Tree_03_green.prefab";
                case Species.Fruit:
                case Species.FruitDeep:
                    return PolytopeRoot + "/Trees/PT_Fruit_Tree_01_green.prefab";
                case Species.Orchard:
                    return PolytopeRoot + "/Trees/PT_Fruit_Tree_01_apples.prefab";
                default:
                    return null;
            }
        }

        /// <summary>The colour shift a species applies to its prefab, if it is a tinted variant.</summary>
        private static bool TryGetSpeciesTint(Species species, out Tint tint)
        {
            switch (species)
            {
                case Species.PinePale:
                    tint = new Tint("Pale", -0.03f, 0.86f, 1.22f);
                    return true;
                case Species.FruitDeep:
                    tint = new Tint("Deep", 0.02f, 1.14f, 0.78f);
                    return true;
                default:
                    tint = default;
                    return false;
            }
        }

        /// <summary>
        /// One canopy species: a Polytope prefab, tinted if the species is a colour variant.
        /// </summary>
        /// <remarks>
        /// Per-instance tinting is not an option either — <c>TreeInstance.color</c> only reaches the
        /// shader for Tree Creator and SpeedTree assets, and Vista leaves it at zero regardless. The
        /// Polytope foliage shader drives a vertical gradient from <c>_TopColor</c> and
        /// <c>_GroundColor</c> with <c>_CUSTOMCOLORSTINTING</c> already on, so a tint is a copy of
        /// the prefab whose materials shift those two colours. The shifts are small on purpose:
        /// enough to break the cloned look, not enough to read as a different tree.
        /// </remarks>
        public static TreeTemplate LoadOrCreateCanopyTree(Species species)
        {
            var assetPath = CanopyTreePath(species);
            var existing = AssetDatabase.LoadAssetAtPath<TreeTemplate>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpeciesPrefabPath(species));
            if (basePrefab == null)
            {
                Debug.LogError($"[Vegetation] No prefab for '{species}' at '{SpeciesPrefabPath(species)}'.");
                return null;
            }

            var prefab = TryGetSpeciesTint(species, out var tint)
                ? LoadOrCreateTintedVariant(basePrefab, tint) ?? basePrefab
                : basePrefab;

            return CreateTreeTemplate(
                assetPath,
                new System.Collections.Generic.List<GameObject> { prefab },
                species.ToString(),
                UnityEngine.Rendering.ShadowCastingMode.On);
        }

        /// <summary>A colour shift applied to a foliage material, in HSV.</summary>
        private readonly struct Tint
        {
            public readonly string Suffix;
            public readonly float HueShift;
            public readonly float SaturationScale;
            public readonly float ValueScale;

            public Tint(string suffix, float hueShift, float saturationScale, float valueScale)
            {
                Suffix = suffix;
                HueShift = hueShift;
                SaturationScale = saturationScale;
                ValueScale = valueScale;
            }
        }

        private const string TintFolder = Folder + "/Tints";

        /// <summary>Copy of <paramref name="source"/> whose foliage colours are shifted.</summary>
        private static GameObject LoadOrCreateTintedVariant(GameObject source, Tint tint)
        {
            if (source == null)
            {
                return null;
            }

            var path = $"{TintFolder}/{source.name}_{tint.Suffix}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                return existing;
            }

            VistaGraphBuild.EnsureFolder(TintFolder);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>(includeInactive: true))
                {
                    var materials = renderer.sharedMaterials;
                    var changed = false;
                    for (var i = 0; i < materials.Length; i++)
                    {
                        var tinted = LoadOrCreateTintedMaterial(materials[i], tint);
                        if (tinted != null)
                        {
                            materials[i] = tinted;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        renderer.sharedMaterials = materials;
                    }
                }

                return PrefabUtility.SaveAsPrefabAsset(instance, path);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        /// <summary>
        /// Tinted copy of a Polytope foliage material, or <see langword="null"/> if the material
        /// does not use the gradient tint this shifts.
        /// </summary>
        private static Material LoadOrCreateTintedMaterial(Material source, Tint tint)
        {
            if (source == null || !source.HasProperty(TopColor) || !source.HasProperty(GroundColor))
            {
                return null;
            }

            var path = $"{TintFolder}/{source.name}_{tint.Suffix}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            VistaGraphBuild.EnsureFolder(TintFolder);
            var copy = new Material(source) { name = $"{source.name}_{tint.Suffix}" };
            copy.SetColor(TopColor, Shift(source.GetColor(TopColor), tint));
            copy.SetColor(GroundColor, Shift(source.GetColor(GroundColor), tint));
            AssetDatabase.CreateAsset(copy, path);
            return copy;
        }

        private static readonly int TopColor = Shader.PropertyToID("_TopColor");
        private static readonly int GroundColor = Shader.PropertyToID("_GroundColor");

        /// <summary>
        /// Shifts a colour in HSV, keeping the alpha and wrapping the hue.
        /// </summary>
        /// <remarks>
        /// HSV rather than a per-channel multiply because the foliage colours are HDR and nearly
        /// saturated: scaling channels directly drags them towards grey or clips them, while a hue
        /// and value shift keeps the leaf reading as a leaf.
        /// </remarks>
        private static Color Shift(Color color, Tint tint)
        {
            Color.RGBToHSV(color, out var h, out var s, out var v);
            h = Mathf.Repeat(h + tint.HueShift, 1f);
            s = Mathf.Clamp01(s * tint.SaturationScale);
            v = Mathf.Clamp01(v * tint.ValueScale);
            var shifted = Color.HSVToRGB(h, s, v);
            shifted.a = color.a;
            return shifted;
        }

        /// <summary>Bushes for the layer between the trunks and the grass.</summary>
        public static TreeTemplate LoadOrCreateUndergrowthBush()
        {
            return LoadOrCreateTreeTemplate(
                UndergrowthBushPath,
                new[]
                {
                    PolytopeRoot + "/Shrubs/PT_Generic_Shrub_01_green.prefab",
                    PolytopeRoot + "/Shrubs/PT_Generic_Shrub_01_dead.prefab",
                },
                "undergrowth",
                // Bushes are small and numerous; their shadows cost more than they add.
                UnityEngine.Rendering.ShadowCastingMode.Off);
        }

        /// <summary>Rocks, for breaking up open ground and marking rugged terrain.</summary>
        public static TreeTemplate LoadOrCreateRock()
        {
            return LoadOrCreateTreeTemplate(
                RockPath,
                new[]
                {
                    PolytopeRoot + "/Rocks/PT_Generic_Rock_01.prefab",
                    PolytopeRoot + "/Rocks/PT_Ore_Rock_01.prefab",
                },
                "rocks",
                UnityEngine.Rendering.ShadowCastingMode.On);
        }

        private static TreeTemplate LoadOrCreateTreeTemplate(
            string assetPath,
            string[] prefabPaths,
            string label,
            UnityEngine.Rendering.ShadowCastingMode shadows)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TreeTemplate>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            return CreateTreeTemplate(assetPath, LoadExplicitPrefabs(prefabPaths), label, shadows);
        }

        /// <summary>Writes a new tree template from prefabs Unity will accept as prototypes.</summary>
        private static TreeTemplate CreateTreeTemplate(
            string assetPath,
            System.Collections.Generic.List<GameObject> candidates,
            string label,
            UnityEngine.Rendering.ShadowCastingMode shadows)
        {
            var prefabs = KeepTreePrototypeCompatible(candidates, label);
            if (prefabs.Count == 0)
            {
                Debug.LogError($"[Vegetation] No usable prefab for '{label}'; the graph will place none.");
                return null;
            }

            VistaGraphBuild.EnsureFolder(Folder);
            var template = ScriptableObject.CreateInstance<TreeTemplate>();
            template.Reset();
            template.prefab = prefabs[0];
            template.prefabVariants = prefabs.Count > 1 ? prefabs.GetRange(1, prefabs.Count - 1).ToArray() : null;
            template.shadowCastingMode = shadows;
            template.receiveShadow = true;

            AssetDatabase.CreateAsset(template, assetPath);
            AssetDatabase.SaveAssets();
            return template;
        }

        /// <summary>
        /// Drops prefabs Unity cannot use as a terrain tree prototype, and says why.
        /// </summary>
        /// <remarks>
        /// A <c>TreePrototype</c> needs an <c>LODGroup</c> or a <c>MeshRenderer</c> <b>on the prefab
        /// root</b>. The failure is not graceful: one invalid prototype makes Unity reject the whole
        /// <c>treePrototypes</c> assignment, so the tiles end up with <em>no trees at all</em>.
        /// </remarks>
        private static System.Collections.Generic.List<GameObject> KeepTreePrototypeCompatible(
            System.Collections.Generic.List<GameObject> prefabs, string label)
        {
            var usable = new System.Collections.Generic.List<GameObject>(prefabs.Count);
            var rejected = 0;
            foreach (var prefab in prefabs)
            {
                if (prefab == null)
                {
                    continue;
                }

                if (prefab.GetComponent<LODGroup>() != null || prefab.GetComponent<MeshRenderer>() != null)
                {
                    usable.Add(prefab);
                }
                else
                {
                    rejected++;
                }
            }

            if (rejected > 0)
            {
                Debug.LogWarning(
                    $"[Vegetation] {rejected} '{label}' prefab(s) skipped: a Unity terrain tree needs " +
                    "an LOD Group or a Mesh Renderer on the prefab root, and these have neither.");
            }

            return usable;
        }

        private static System.Collections.Generic.List<GameObject> LoadExplicitPrefabs(params string[] paths)
        {
            var found = new System.Collections.Generic.List<GameObject>(paths.Length);
            foreach (var path in paths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    found.Add(prefab);
                }
            }

            return found;
        }

        /// <summary>
        /// Grass clump for the graph's DETAIL DENSITY output.
        /// </summary>
        /// <remarks>
        /// VertexLit render mode because the source is a mesh, not a billboard texture. Sizes are
        /// multiples of that mesh — see <see cref="GrassScaleMin"/> for why they are not metres.
        /// </remarks>
        public static DetailTemplate LoadOrCreateMeadowGrass()
        {
            var existing = AssetDatabase.LoadAssetAtPath<DetailTemplate>(MeadowGrassPath);
            if (existing != null)
            {
                return existing;
            }

            VistaGraphBuild.EnsureFolder(Folder);
            var template = ScriptableObject.CreateInstance<DetailTemplate>();
            template.Reset();
            template.alignToSurface = false;
            template.castShadow = UnityEngine.Rendering.ShadowCastingMode.Off;
            template.receiveShadow = true;

            if (UseVistaSampleGrass)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(VistaSampleGrassTexture);
                if (texture == null)
                {
                    Debug.LogError($"[Vegetation] Missing Vista sample grass texture at '{VistaSampleGrassTexture}'.");
                    Object.DestroyImmediate(template);
                    return null;
                }

                // Vista's own values, deliberately unaltered: this is the control case.
                template.renderMode = DetailRenderMode.Grass;
                template.texture = texture;
                template.minHeight = 0.5f;
                template.maxHeight = 1f;
                template.minWidth = 0.5f;
                template.maxWidth = 1f;
                template.useInstancing = false;
            }
            else
            {
                var grass = LoadOrCreateGrassDetailPrefab();
                if (grass == null)
                {
                    Object.DestroyImmediate(template);
                    return null;
                }

                template.renderMode = DetailRenderMode.VertexLit;
                template.prefab = grass;
                template.minHeight = GrassScaleMin;
                template.maxHeight = GrassScaleMax;
                template.minWidth = GrassScaleMin;
                template.maxWidth = GrassScaleMax;
                template.useInstancing = CanInstance(grass);
            }

            AssetDatabase.CreateAsset(template, MeadowGrassPath);
            AssetDatabase.SaveAssets();
            return template;
        }

        /// <summary>
        /// Builds a flat prefab Unity will accept as a terrain detail prototype.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the counterpart of the tree-prototype rule, and it points the opposite way. A
        /// terrain <b>detail</b> in mesh mode needs a <c>MeshFilter</c> and <c>MeshRenderer</c>
        /// directly on the prefab root; an <c>LODGroup</c> root does not work. Every Polytope prefab
        /// is authored as an LOD Group with LOD0/1/2 children, so feeding one straight to a
        /// <c>DetailPrototype</c> produced a prototype that rendered nothing — silently, with no
        /// console message. That is why there was no grass at any distance.
        /// </para>
        /// <para>
        /// So we generate a one-object prefab carrying the LOD0 mesh and its materials. Details are
        /// already distance-culled and instanced by the terrain, so losing the LOD chain costs
        /// nothing here.
        /// </para>
        /// </remarks>
        private static GameObject LoadOrCreateGrassDetailPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(GrassDetailPrefabPath);
            if (existing != null)
            {
                return existing;
            }

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(GrassSourcePath);
            if (source == null)
            {
                Debug.LogError($"[Vegetation] Missing grass source prefab at '{GrassSourcePath}'.");
                return null;
            }

            var sourceFilter = FindHighestDetailMesh(source);
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
            {
                Debug.LogError($"[Vegetation] '{GrassSourcePath}' has no mesh to build a detail from.");
                return null;
            }

            var sourceRenderer = sourceFilter.GetComponent<MeshRenderer>();
            VistaGraphBuild.EnsureFolder(Folder);

            var go = new GameObject("Overworld_GrassDetail");
            try
            {
                go.AddComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
                var renderer = go.AddComponent<MeshRenderer>();
                var material = LoadOrCreateGrassMaterial(sourceRenderer);
                if (material != null)
                {
                    renderer.sharedMaterial = material;
                }

                return PrefabUtility.SaveAsPrefabAsset(go, GrassDetailPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Our own copy of the grass material, with GPU instancing enabled.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A <c>DetailPrototype</c> with <c>useInstancing</c> set is only legal when every material
        /// on the prefab has "Enable GPU Instancing" checked. Unity rejects the prototype otherwise,
        /// and the tile ends up with no grass — no console message, nothing to see. The Polytope
        /// material ships with instancing off, and it is shared by props all over the project, so we
        /// take a copy instead of flipping a flag on a vendor asset that other scenes rely on.
        /// </para>
        /// <para>
        /// Instancing is worth the copy: ground cover over a 2.2 km island is the one thing in the
        /// scene with instance counts in the millions.
        /// </para>
        /// </remarks>
        private static Material LoadOrCreateGrassMaterial(MeshRenderer sourceRenderer)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(GrassMaterialPath);
            if (existing != null)
            {
                return existing;
            }

            var source = sourceRenderer != null ? sourceRenderer.sharedMaterial : null;
            if (source == null)
            {
                Debug.LogError("[Vegetation] The grass source prefab has no material to copy.");
                return null;
            }

            VistaGraphBuild.EnsureFolder(Folder);
            var copy = new Material(source) { name = "Overworld_GrassDetail", enableInstancing = true };
            AssetDatabase.CreateAsset(copy, GrassMaterialPath);
            AssetDatabase.SaveAssets();
            return copy;
        }

        /// <summary>True when every material on <paramref name="prefab"/> supports GPU instancing.</summary>
        private static bool CanInstance(GameObject prefab)
        {
            if (prefab == null)
            {
                return false;
            }

            foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>(includeInactive: true))
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null || !material.enableInstancing)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Reports what is wrong with the existing vegetation assets and repairs only what makes
        /// Unity refuse them.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The templates are deliberately never restamped on a graph rebuild, so a template authored
        /// before a bug was found keeps the bug forever. That is the right default — Inspector edits
        /// must survive — but it needs an explicit way out, and a destructive recreate is too blunt
        /// because it also throws away deliberate tuning on the tree templates.
        /// </para>
        /// <para>
        /// So this fixes exactly three classes of defect, each of which makes the asset unusable
        /// rather than merely different from the code default: a prototype Unity rejects, an
        /// instancing flag the material cannot honour, and detail sizes still carrying the
        /// metres-as-multiplier values. Everything else is reported and left alone.
        /// </para>
        /// </remarks>
        [MenuItem("GameDev/Overworld/Vista/Repair Vegetation Templates")]
        public static void RepairMenu()
        {
            var repairs = 0;

            var grass = AssetDatabase.LoadAssetAtPath<DetailTemplate>(MeadowGrassPath);
            if (grass == null)
            {
                Debug.Log("[Vegetation] No grass template yet — creating from defaults.");
                LoadOrCreateMeadowGrass();
                repairs++;
            }
            else
            {
                repairs += RepairGrass(grass);
            }

            foreach (Species species in System.Enum.GetValues(typeof(Species)))
            {
                repairs += RepairTree(AssetDatabase.LoadAssetAtPath<TreeTemplate>(CanopyTreePath(species)), species.ToString());
            }

            repairs += RepairTree(AssetDatabase.LoadAssetAtPath<TreeTemplate>(UndergrowthBushPath), "undergrowth");
            repairs += RepairTree(AssetDatabase.LoadAssetAtPath<TreeTemplate>(RockPath), "rocks");

            AssetDatabase.SaveAssets();
            Debug.Log(
                repairs == 0
                    ? "[Vegetation] Templates checked: nothing needed repairing."
                    : $"[Vegetation] Templates checked: {repairs} defect(s) repaired. Re-run Generate All.");
        }

        private static int RepairGrass(DetailTemplate template)
        {
            var repairs = 0;

            // The texture path has none of the failure modes below — no prefab shape rule, no
            // instancing contract, and its sizes are Vista's own. Only report it.
            if (UseVistaSampleGrass)
            {
                if (template.renderMode == DetailRenderMode.VertexLit || template.texture == null)
                {
                    Debug.LogWarning(
                        "[Vegetation] Grass template is still the mesh variant while the code is set to " +
                        "Vista's sample texture. Run 'Recreate Vegetation Templates (destructive)'.");
                }

                return 0;
            }

            if (template.renderMode != DetailRenderMode.VertexLit)
            {
                Debug.LogWarning(
                    $"[Vegetation] Grass template render mode is {template.renderMode}, which needs a " +
                    "texture, not a prefab. Switching to VertexLit.");
                template.renderMode = DetailRenderMode.VertexLit;
                repairs++;
            }

            var prefab = template.prefab;
            var needsPrefab = prefab == null ||
                              prefab.GetComponent<MeshFilter>() == null ||
                              prefab.GetComponent<MeshRenderer>() == null ||
                              prefab.GetComponent<LODGroup>() != null;
            if (needsPrefab)
            {
                Debug.LogWarning(
                    "[Vegetation] Grass prefab is not a valid detail prototype (a detail in mesh mode " +
                    "needs MeshFilter + MeshRenderer on the root and must not have an LOD Group). " +
                    "Regenerating it.");
                AssetDatabase.DeleteAsset(GrassDetailPrefabPath);
                prefab = LoadOrCreateGrassDetailPrefab();
                template.prefab = prefab;
                repairs++;
            }

            if (prefab != null && template.useInstancing && !CanInstance(prefab))
            {
                var material = LoadOrCreateGrassMaterial(prefab.GetComponent<MeshRenderer>());
                if (material != null)
                {
                    var renderer = prefab.GetComponent<MeshRenderer>();
                    renderer.sharedMaterial = material;
                    PrefabUtility.SavePrefabAsset(prefab);
                    Debug.LogWarning(
                        "[Vegetation] Grass asked for GPU instancing but its material had instancing " +
                        "off, so Unity was rejecting the prototype — that is why there was no grass. " +
                        $"Repointed the prefab at '{material.name}'.");
                }
                else
                {
                    template.useInstancing = false;
                    Debug.LogWarning("[Vegetation] Grass instancing disabled: no instancing-capable material.");
                }

                repairs++;
            }

            if (Mathf.Approximately(template.minHeight, LegacyGrassScaleMin) &&
                Mathf.Approximately(template.maxHeight, LegacyGrassScaleMax))
            {
                Debug.LogWarning(
                    "[Vegetation] Grass sizes were still the metres-as-multiplier values " +
                    $"({LegacyGrassScaleMin}..{LegacyGrassScaleMax}), which asks Unity for instances at " +
                    $"5 % of the mesh. Set to {GrassScaleMin}..{GrassScaleMax} of the source clump.");
                template.minHeight = GrassScaleMin;
                template.maxHeight = GrassScaleMax;
                template.minWidth = GrassScaleMin;
                template.maxWidth = GrassScaleMax;
                repairs++;
            }

            EditorUtility.SetDirty(template);
            return repairs;
        }

        private static int RepairTree(TreeTemplate template, string label)
        {
            if (template == null)
            {
                return 0;
            }

            var kept = KeepTreePrototypeCompatible(
                new System.Collections.Generic.List<GameObject> { template.prefab }, label);
            if (kept.Count == 0)
            {
                Debug.LogError(
                    $"[Vegetation] '{label}' main prefab cannot be a tree prototype — one bad prototype " +
                    "makes Unity drop the whole treePrototypes array, so no tile gets any trees.");
                return 0;
            }

            var variants = KeepTreePrototypeCompatible(
                new System.Collections.Generic.List<GameObject>(template.prefabVariants), label);
            if (variants.Count == template.prefabVariants.Length)
            {
                return 0;
            }

            template.prefabVariants = variants.ToArray();
            EditorUtility.SetDirty(template);
            return 1;
        }

        /// <summary>Picks LOD0 if the source is an LOD Group, otherwise the first mesh found.</summary>
        private static MeshFilter FindHighestDetailMesh(GameObject source)
        {
            var lodGroup = source.GetComponent<LODGroup>();
            if (lodGroup != null)
            {
                var lods = lodGroup.GetLODs();
                if (lods.Length > 0 && lods[0].renderers != null)
                {
                    foreach (var renderer in lods[0].renderers)
                    {
                        if (renderer == null)
                        {
                            continue;
                        }

                        var filter = renderer.GetComponent<MeshFilter>();
                        if (filter != null && filter.sharedMesh != null)
                        {
                            return filter;
                        }
                    }
                }
            }

            return source.GetComponentInChildren<MeshFilter>();
        }
    }
}
#endif
