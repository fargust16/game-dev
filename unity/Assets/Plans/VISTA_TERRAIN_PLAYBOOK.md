# Vista Terrain Playbook

**Status:** Living document
**Read this before touching anything under `Assets/Scripts/Editor/*Graph*`, `OverworldLpbBake`,
`OverworldBiomeTemplates`, `OverworldTerrainLayers` or `VistaVegetationTemplates`.**

**Related:** [VISTA_OVERWORLD_REFERENCE.md](VISTA_OVERWORLD_REFERENCE.md) (scene/biome reference),
[OVERWORLD_VISUAL_DIRECTION.md](OVERWORLD_VISUAL_DIRECTION.md) (look targets),
[ASSET_LIBRARY_REFERENCE.md](ASSET_LIBRARY_REFERENCE.md) (which pack for what).

This file exists because the same handful of traps cost several days. Every entry below was paid for
with a broken bake. None of them are guesses — each one names the file and the mechanism.

---

## 0. The one rule

**Vista generates the terrain. Our C# composes and configures Vista, and does nothing else.**

C# may: create and configure Vista components, assign graphs and templates, decide which biome exists
where, trigger generation.

C# may not: write heights, paint alphamaps, assign `terrainData.terrainLayers`, place trees or grass,
recolour layers, or "fix up" Vista's output afterwards. If the result is wrong, the graph, the biome
template, or the layer asset is wrong — fix it there.

---

## 1. Architecture at a glance

```
OverworldPipeline / OverworldTerrainMenu      orchestration only
        │
        ▼
VistaManager  (terrainMaxHeight 700, 2x2 tiles of 1400 m)
        │
        ├── LPB_SeaFloor    order  0   SnapBiomeToTiles   worldHeight  20   Replace
        ├── LPB_IslandBase  order 10   SetBiomeArea 2240  worldHeight 140   Replace   Y = 24
        └── LPB_Mountain_n  order 20+  SetBiomeArea       worldHeight 530   Raise
                │
                ▼
        Terrain Graph  →  height + 5 layer weights + trees + grass density
                │
                ▼
        URP TerrainLit over 5 canonical TerrainLayer assets
```

Biomes spawn from **native Vista biome templates** in `Assets/Settings/Vista/BiomeTemplates/`.
`OverworldLpbBake` sets **only** seed, XZ position and the anchor silhouette.

---

## 2. Traps that cost us days

### 2.1 `SetBiomeHeightOnSpawn` / `SetBiomeAreaOnSpawn` are one-shots

They run from `OnSpawnedFromBiomeTemplate`, write `transform.localScale.y` and the anchor polygon
**once**, and never read their own fields again. The live authority is
`transform.localScale.y × VistaManager.terrainMaxHeight` plus `blendOptions.useTransformForHeightBlend`.

- Editing `World Height` on a biome **already in the scene** does nothing.
  Use `GameDev/Overworld/Vista/Apply Biome Inspector Settings + Regenerate`.
- `Generate All` **respawns every biome from the prefab**, so scene edits are discarded.
  The durable place to tune is `Assets/Settings/Vista/BiomeTemplates/*.prefab`.

### 2.2 Rebuilding a graph asset by delete-then-create breaks every reference

`AssetDatabase.DeleteAsset` takes the `.meta` with it, so the re-created asset gets a **new GUID** and
the `terrainGraph` field on the biome prefabs silently becomes "Missing". The bake then produces a
completely flat world with zero layers.

Always overwrite in place — `VistaGraphBuild.SaveGraph` uses `EditorUtility.CopySerialized`.

### 2.3 `SetDetailResolution` and `heightmapResolution` destroy data

Both discard existing content. Calling them **after** generation erased the detail layers Vista had
just written, which is why grass never appeared for several iterations.
`TerrainHeightWrite.ForceTileResolutionAndSize` now guards every setter against a no-op write.

### 2.4 Vista Personal caps the biome canvas at 512 px

1K/2K/4K are Indie/Pro only. The reference biome is authored at 1024, so **its erosion feature sizes
do not transfer**. A `featureSize` finer than `biomeWidth / 512` is silently clamped and the flow
simulation carves grid-aligned parallel rills — the corduroy striping.

The node tells you: *"Feature Size 2 m needs a 1000 px sim canvas, above the 512 px budget."*
Never ignore that line. `VistaGraphBuild.MinFeatureSizeMeters` is the floor for our biome sizes.

### 2.5 Trees and details want **opposite** prefab shapes

| Prototype | Unity requires on the prefab **root** | Breaks on |
|---|---|---|
| `TreePrototype` | `LODGroup` **or** `MeshRenderer` | renderer on a child, nested prefab instances |
| `DetailPrototype` (mesh mode) | `MeshFilter` + `MeshRenderer` | an `LODGroup` root |

Both failures cost a debugging cycle:

- The **Stylized Trees Pack** is authored as nested prefab instances with nothing on the root, so it
  fails the tree rule. The failure is not graceful — one invalid prototype makes Unity reject the
  whole `treePrototypes` assignment, so the tiles end up with **no trees at all**. It logs
  *"couldn't be instanced because the prefab contains no valid mesh renderer"*.
- Every **Polytope** prefab is an LOD Group, which passes the tree rule and fails the detail rule.
  Feeding `PT_Grass_02` straight to a `DetailPrototype` produced a prototype that rendered nothing,
  **silently, with no console message at all**. That is why there was no grass at any distance.

`VistaVegetationTemplates.KeepTreePrototypeCompatible` filters tree prefabs and logs.
`LoadOrCreateGrassDetailPrefab` generates a flat one-object prefab from the source LOD0 mesh for the
detail path. To use a pack that fails the tree rule, add a root LOD Group or place it with
`ObjectOutputNode`.

### 2.6 A detail prototype that asks for instancing needs a material that allows it

`DetailPrototype.useInstancing = true` is only legal when **every** material on the prefab has
*Enable GPU Instancing* checked. Unity rejects the prototype otherwise, with **no console message**,
and the tile gets no grass. `PT_Grass_Mat` ships with instancing off and is shared by props across
the project, so `LoadOrCreateGrassMaterial` takes a copy into
`Assets/Settings/Vista/Vegetation/` and enables it there instead of flipping a flag on a vendor
asset. This was the second, independent reason there was no grass — 2.5 was the first.

### 2.7 Prototype sizes are multipliers, not metres

`DetailPrototype.minHeight/maxHeight/minWidth/maxWidth` and `TreeInstance.heightScale/widthScale`
scale the **source mesh**. Feeding `WorldScale.GrassShortMinMeters` (0.05) into `minHeight` asks for
5 % of a 1.24 m clump, not 5 cm of grass. Divide the prefab's own metre size out first — the sizes
in `WorldScale` are review targets for the finished look, not values to assign.

Related: **`TreeTemplate.baseScale` does nothing on Unity terrain.**
`TerrainTile.ParseTreeInstances` copies `heightScale` straight out of the instance buffer, so the
only size levers for trees are `TreeOutputNode.verticalScaleMultiplier` /
`horizontalScaleMultiplier` and the scale mask inputs beside them. Because a bare multiplier gives
every instance of a species exactly one size, a coarse noise remapped to `[1-variation, 1]` feeds
both scale inputs; the mask multiplies the multiplier, so it must stay inside `[0,1]`.

### 2.8 Vista's detail-density writer loses the map in the editor

The graph produces a correct density map and the terrain still ends up with zero detail instances.
Everything in between measures clean — template valid, prototype accepted, green weight reaching 1.0,
output shader present, read-back synchronous, detail resolution pinned before the bake so nothing is
destroyed after it. The probe on `VistaManager.detailDensityPopulated` reads
`512x512 RFloat max=1.000 nonZero=40%` going in.

`TerrainTile.PopulateLayerDensityProgressive` writes one patch row per editor frame from a
`Texture2D` that its parent coroutine destroys as soon as its own wait returns — and that wait is
`yield return c.coroutine` on an `EditorCoroutine`, which is neither an `IEnumerator` nor a
`CustomYieldInstruction`, so it resumes after one tick rather than when the writer finishes.

`VistaDetailDensityRelay` captures the map at the event and writes the whole layer in one
`SetDetailLayer` call, reusing Vista's own base density and stochastic rounding. It decides nothing
about placement. Delete it when the vendor path works.

**Related, and worth avoiding regardless:** two graphs emitting the *same* `DetailTemplate` give
Vista two density maps for one template on every shared tile, which routes it through its merge path
instead of the direct one. The massif now opts out of ground cover (`includeGroundCover: false`) —
alpine scree should not grow meadow grass anyway.

### 2.9 Two different AnimatorController symptoms, two different causes

**`Animator is not playing an AnimatorController`, thousands of lines** — ours. Rebuilding deletes
the asset, and any `Animator` already referencing it is left holding a destroyed object, so every
`SetFloat`/`SetBool` in `Update` warns once per frame. Running the rebuild *after* spawning the
player is what opens the window. Build the controller only when missing, keep the rebuild as an
explicit destructive item, and reassign it to the player in the scene at the end of a rebuild.

**`NullReferenceException` at `UnityEditor.Graphs.Edge.WakeUp` → `Graph.OnEnable`** — *not* ours.
`UnityEditor.Graphs` is the **Animator window's** graph view, not the controller asset. When that
window is open, every domain reload — compiling, entering play mode — makes it deserialize a graph
whose backing objects the reload just destroyed. Closing the Animator window tab stops it. Before
blaming an asset, check: a scan of all 77 `.controller` files for transitions whose `m_DstState` and
`m_DstStateMachine` are both `{fileID: 0}` found none, and `UserSettings/Layouts/default-6000.dwlt`
contains `AnimatorControllerTool`.

### 2.10 A Built-in RP material in URP is invisible, not magenta

The Human Character Dummy ships on the Built-in `Standard` shader. URP only runs passes tagged
`RenderPipeline = UniversalPipeline`, so it walks past the mesh and draws nothing. Magenta means a
*missing* shader; this shader exists and simply has no pass URP will run, so there is no error
anywhere. The tell is a **selection outline over empty ground** — the editor draws the outline from
the mesh, independently of the material. Fix:
`GameDev/Overworld/Player/Fix Character Materials (Built-in -> URP)`.

### 2.11 Half of "it looks wrong" is a view setting, not generation

`OverworldTerrainMenu.ApplyTerrainRenderDistances` sets these after the bake. All of them change what
you see and none of them change what Vista produced.

| Setting | Default | Why it matters here |
|---|---|---|
| `detailObjectDistance` | 80 m (max 250) | grass exists in the data and is simply never drawn from an aerial view |
| `heightmapPixelError` | 5 | geometry LOD simplifies in strips across a large gentle surface |
| `basemapDistance` | 1000 m | past it the terrain drops the real splat blend for a low-res basemap |

Diagnostic that separates the two: **look at the same spot from the ground.** If it is clean up close
and striped from far away, it is a distance artifact, not geometry.

---

## 3. Graph contracts

### 3.1 Weight Blend

`WeightsBlend.Blend` computes `output[i] = in[i] × Π(1 − in[j])` for every `j > i`.

- Priority **rises** with index: the last connected layer wins.
- **Layer 0 must stay unconnected.** Vista fills it white, and that white base is the only thing
  making the weights sum to 1. Nothing downstream normalizes them.
- Overlapping masks are fine and expected — higher layers erode lower ones.

A layer whose mask is saturated (mostly 1.0) erases everything under it. Check with the histogram in
`GameDev/Overworld/Vista/Dump Layer Weights`.

### 3.2 Texture Output and the layer stack

- `TextureOutputNode.order = (int)OverworldTerrainLayers.SurfaceLayer`.
- Layers merge by **`TerrainLayer` asset identity**, not by index.
- Vista rewrites `terrainData.terrainLayers` **per tile** from the distinct layers of the biomes that
  covered it (`TerrainTile.PopulateLayerWeights`). **C# must never assign that array.**
- Therefore **every graph emits the full canonical set of five outputs**, unused ones simply
  unconnected → black. A graph that emits fewer makes tiles disagree about what channel *n* means.

### 3.3 Height spaces

| Quantity | Space |
|---|---|
| Graph height map | normalized 0..1 |
| `Args.TERRAIN_HEIGHT` | the **biome's own** world height (`LocalProceduralBiome.cs`), not the manager's |
| `HeightMaskNode` min/max | metres against that biome height |
| Biome world Y | offsets the whole band; `IslandBase` sits at `Y = SeaLevelMeters` so the island is not drowned |

Rule: **proportional** bands (a foothill skirt) use normalized `Curve`/`Levels` so they rescale with
the biome. **Sea-anchored** bands would need absolute metres — but see 3.4 first.

### 3.4 The graph CAN see the biome mask — use it

Vista supplies the biome coverage mask to every terrain graph as an input named
`"Biome Mask Input"` (`GraphConstants.BIOME_MASK_INPUT_NAME`, pushed by `LPBInputProvider.SetInput`).
An `InputNode` carrying that name receives it. `VistaGraphBuild.PublishBiomeMask` does exactly that
and republishes it as the `BiomeMask` variable.

This matters because the visible terrain is `mask × graphHeight` blended over whatever is beneath.
Inside the Outer falloff ring the mask decays and the surface sinks toward the seabed, crossing the
waterline **partway through the ring**. The graph's own height knows nothing about that, so anything
keyed on height alone spills into the sea — measured twice: sand weight 0.000 on three tiles of four,
and bushes growing in open water.

`VistaGraphBuild.AddDryLandGate` thresholds the mask so vegetation only lands inland of the crossing.
It is applied once to `GreenAreaMask`, so every layer downstream inherits it. The same input is the
proper way to build a real waterline beach.

### 3.5 Node semantics that are not obvious

| Node | Behaviour |
|---|---|
| `SlopeMask` | `(angle ≥ min) × (angle ≤ max) × curve(t)` — a hard-gated band |
| `HeightMask` | `saturate(inverseLerp(...))` then curve — a ramp, **not** gated |
| `Utilities.EaseInOutCurve` | a **plateau that reaches 0 at both ends**, not a rising ramp |
| `RemapNode(min,max)` | auto-levels: stretches the input's actual range into `[min,max]` |
| `MathNode` ops 4 | one-minus |
| `NoiseNode` output | `1 − (perlin × 0.5 + 0.5)` — a **bell around 0.5**, not a sweep across `[0,1]` |

The noise row is the one that keeps biting. Perlin practically never reaches ±1, and stacking
octaves narrows the distribution further, so the working range is roughly `[0.35, 0.65]`. Anything
that maps noise onto an output band **must contrast-stretch it first** (`Levels` with
`inLow 0.35 / inHigh 0.65`) or it will only traverse the middle of that band. Feeding raw noise into
a tree-scale band of `[0.67, 1.0]` produced instances between 0.78 and 0.88 — a forest of identical
trees, from a node graph that looked correct. `VistaGraphBuild.NoiseWorkingLow/High` hold the range.

The same trap kills **thresholds**, and there it is total rather than subtle: a cluster gate at 0.68
sits above anything the noise ever produces, so that species is absent from the world. The orchard
placed zero instances on all four tiles for exactly this reason, while pine at 0.42 placed
thousands. Any `Levels` used as a gate on raw noise has to be stretched first.

**A detail layer has no scatter step to forgive this.** Trees pass their mask through `ScatterNode`,
which samples it against a per-point random, so a mask that is merely low still places some trees.
`DetailDensityOutput` multiplies the mask by the template density and rounds — a mask below the
density node's floor is uniformly zero, and "the floor was slightly too high" and "there is no grass
anywhere" are the same picture. Keep detail floors near zero and shape density with the ceiling.

Consequence of rows 1 and 3 together: `SlopeMask(0..4°)` with the default curve is **zero on flat
ground**. Flat gates need `VistaGraphBuild.ConstantCurve()`. The same trap made `GeometryMask(0..28°)`
leave level ground treeless.

---

## 4. Do not override node defaults without a reason

Vista's node defaults **are** the reference biome's values. `NoiseNode` ships 8 octaves with
`flipSign` and `applyRemapPerLayer`; `HydraulicErosionNode2` ships the Macro preset.

Set only what is genuinely per-biome: feature `scale` and `seed`.

Three substitutions cost us a full debugging cycle each:

| We wrote | The default / reference | What it produced |
|---|---|---|
| 3–4 noise octaves, no remap curve | 8 octaves, `applyRemapPerLayer` | smooth blobs instead of ridged rock |
| `Levels(0, 0.5, 0.55)` contrast stretch | `Levels(0, 0.5, 1, outLow 0.05)` | clipped the top half flat |
| Curve boosting erosion mid-tones | pass-through | soil covering 75 % of the island |

Copying the reference's *topology* while substituting our own numbers produces terrain that looks
nothing like it.

---

## 5. Vegetation

**Everything is placed by the terrain graph. There is no GameObject scatter pass.** The old
`VegetationScatterMenu` and its CPU mask cluster (`GrassMask`, `VegetationEcology`,
`VegetationClearance`, `GrassExclusion`, `RoadLawnMask`, `TreePlacement`, `VegetationDensity`) are
deleted, along with the `GameDev/Overworld/Grass` shader they fed.

```
Layer 1  Canopy        Polytope trees    TREE OUTPUT, one template per species, own cluster noise
Layer 2  Understory    Polytope shrubs   TREE OUTPUT, gated by the blurred CanopyMask
Layer 3  Ground cover  Polytope grass    DETAIL DENSITY OUTPUT via the generated flat prefab
Layer 4  Dressing      Polytope rocks    TREE OUTPUT, driven by the inverse of the green mask
```

**Scrub belongs to the trees.** The species cluster gates are unioned with `Max`, blurred, and
published as `CanopyMask`; the understory multiplies by it. Driving bushes from their own noise —
worse, from the *inverse* of one, which is what this did first — spreads them evenly over open
ground, which is both the wrong place and far too many. `ScatterNode.spacing` is the ceiling on the
count, so that is the knob to turn when there are simply too many of something.

**`prefabVariants` does nothing in Vista Personal.** Every variant getter on `TreeTemplate`,
`DetailTemplate` and `ObjectTemplate` runs through `TemplateUtils.IsVariantsSupported()`, which
returns true only when a runtime component has registered `enableVariantsSupportCallback` — and
nothing in the Personal package ever registers it. The getter therefore returns an empty array no
matter what the field holds, silently, and **each template contributes exactly one prototype**. The
dump is how to spot it: five templates, five prototypes, and the orchard's pears and plums nowhere.

Consequences: one prefab per template, and anything that needs to vary — colour, a dead tree, a
fruit type — has to be **its own species with its own template and Tree Output**. That is not just a
workaround: a separate species gets its own cluster noise, so the variants form interleaved stands
instead of a per-tree lottery. `VistaVegetationTemplates` generates HSV-shifted prefab copies into
`Assets/Settings/Vista/Vegetation/Tints/` for the tinted species.

Colour cannot come from `TreeInstance.color` either — it only reaches the shader for Tree Creator
and SpeedTree assets, and Vista leaves it at zero.

Polytope only, for now — see 2.5 for why the Stylized pack cannot be a terrain tree as shipped.

- **Cluster, do not scatter.** Scattering every species from the same density field gives evenly
  spaced lone trees. Multiply the green mask by a coarse noise thresholded hard, one noise per
  species — that produces forest masses with real gaps.
- `prefabVariants` is variation **within** a species. Different species need different templates and
  different cluster masks, otherwise they sprinkle evenly.
- `TerrainTile` implements `IDetailDensityPopulator` but **not** a detail-instance populator, so the
  reference's Polaris `DetailInstanceOutputNode` branch would compute a buffer nothing reads.
- **Gate the green mask once, at the top.** Everything downstream reads `GreenAreaMask`, so
  multiplying the dry-land gate into it there keeps every layer off the drowned part of the falloff
  ring — see 3.4.

### 5.1 Sizes

Prefab sizes at scale 1, and the bands the graph asks for:

| Species | Prefab | Multiplier | World height |
|---|---|---|---|
| Pine | 9.2 m | 1.17–2.6 | 10.8–23.9 m |
| Fruit | 7.8 m | 1.23–2.45 | 9.6–19.1 m |
| Orchard | 7.8 m | 1.12–1.6 | 8.7–12.5 m (pruned, deliberately smaller) |
| Shrub | 1.86 m | 1 | 1.86 m |
| Grass clump | 1.24 m | 0.7–1.3 | 0.9–1.6 m |

The spread inside a species is what separates saplings from old growth; a stand of one size reads as
wallpaper however good the tree is. It comes from a noise fed into the output's scale inputs — read
the `NoiseNode` row in 3.5 before touching `scaleVariation`, because the band is only traversed if
the noise is stretched first.

A canopy reads as majestic when the trunk is tall enough to walk under and the crown is well above
head height, so the band matters more than the count. Against the 1.8 m character, a 9 m tree is a
sapling. Read 2.7 before changing any of these — the units are not metres.

### 5.2 Templates are never restamped, so they need a repair path

Template assets are created once and then left alone, so Inspector tuning survives a graph rebuild.
The cost is that an asset authored before a bug was found keeps the bug forever, and the destructive
recreate is too blunt because it also discards deliberate tuning.
`GameDev/Overworld/Vista/Repair Vegetation Templates` fixes only what makes Unity *refuse* the asset
— a rejected prototype, an instancing flag the material cannot honour, sizes still carrying the
metres-as-multiplier values — reports everything else, and changes nothing else.

---

## 6. Diagnostics — measure before changing

1. **`GameDev/Overworld/Vista/Dump Layer Weights`** — per tile: layer stack, per-layer histogram with
   `zero%`/`saturated%`, weight sum on painted pixels, land-height distribution, one PNG per layer.
   An average cannot tell a soft gradient from a saturated blanket; the histogram can.
2. **`GameDev/Overworld/Vista/Dump Vegetation State`** — per tile, the whole vegetation chain with a
   number at each link: template valid → prototype accepted by Unity (`DetailPrototype.Validate`,
   material instancing) → density actually written into the detail layer → instance size in metres →
   drawn at this distance. Every one of those failures looks identical from the scene view ("there is
   no grass"); the first line that reads zero is the one that matters.
3. **`Window/Vista/Diagnostics/Session Viewer`** — Vista's own recorder. Captures **every node's
   output** as an image. This is the fastest way to see whether a mask is wrong before the weights
   are even blended. Use it first.
4. Node previews in the Vista graph editor.
5. The console. Vista warns about clamped simulations and rejected prototypes in plain English, and
   both warnings turned out to be exact descriptions of a bug we were hunting blind.

---

## 7. First-time setup and safe rebuilds

```
GameDev/Overworld/Vista/First-Time Setup (graphs + templates)
GameDev/Overworld/Generate All
```

| Menu | Destructive? | When |
|---|---|---|
| First-Time Setup | no (creates only what is missing) | fresh checkout |
| Rebuild *X* Graph | **yes** — discards graph-editor edits, keeps the GUID | after changing a graph builder |
| Recreate Biome Templates | **yes** — discards prefab Inspector edits | after changing template defaults |
| Recreate Vegetation Templates | **yes** | after changing species/prefab wiring |
| Repair Vegetation Templates | no (fixes only what Unity refuses) | grass or trees missing, see 5.2 |
| Apply Biome Inspector Settings + Regenerate | no | iterating on height/area in the scene |
| Player/Fix Character Materials (Built-in -> URP) | rewrites vendor `.mat` files in place | character invisible, see 2.8 |

Changing a `*GraphBuilder` requires the matching Rebuild. Changing an `OverworldBiomeTemplates`
default requires Recreate — the bake will not overwrite an existing prefab, by design.

---

## 8. Current world budget

| Quantity | Value |
|---|---|
| World | 2×2 tiles of 1400 m = 2800 m, 7.84 km² |
| Height budget | 700 m (`WorldScale`, `VistaManager.terrainMaxHeight`) |
| Sea level | 24 m |
| Island area (authored) | 2240 m — the ceiling for this world (`2800 − 2 × ocean margin`) |
| Dry land | ~2.5–3 km² |
| Buildable after mountains and coast | roughly half of that |

A large city with walkable districts is 0.16–0.5 km²; medieval London was ~1.1 km². **Two** large
cities fit comfortably. **Three** with real travel between them needs 4–6 km², which means growing the
world (3×3 tiles, or 2×2 at 2000 m) — not just the island.

---

## 9. Known open items

Defects with measurements attached live in **[OVERWORLD_TECH_DEBT.md](OVERWORLD_TECH_DEBT.md)** —
road planning at 141 s, the single-terrain assumption, the detail-density relay, the Animator
exception. This list is the terrain-shaped remainder.

- Roads and settlements append their own `TerrainLayer`s after generation. That is sanctioned, but
  they must never reorder or repaint biome layer indices 0–4. One tile was observed carrying 9
  layers, past URP TerrainLit's ceiling of 8 — the ninth silently does not render.
- The beach is graph-low-ground, not a true waterline band — see 3.4.
- Stylized Trees Pack prefabs need root LOD Groups before they can be terrain trees. Until then the
  canopy is Polytope-only and the pack sits unused.
- No flowers or mushrooms layer yet; the ground cover is a single grass detail.
