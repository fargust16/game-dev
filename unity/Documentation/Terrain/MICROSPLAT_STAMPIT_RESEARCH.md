# MicroSplat and StampIT — Research for the Veilmouth Island Overhaul

Version: 0.1

Status: Draft

Last Updated: 2026-11-13 (first version: what the three installed packages do, what they cannot do, and how the overhaul will use them)

Depends On:
[CURRENT_WORLD_AUDIT.md](CURRENT_WORLD_AUDIT.md), [AGENTS.md](../../../AGENTS.md), [VEILMOUTH_CITY_PLAN.md](../../Assets/Plans/VEILMOUTH_CITY_PLAN.md)

Referenced By:
[CURRENT_WORLD_AUDIT.md](CURRENT_WORLD_AUDIT.md)

---

# Purpose

This is Phase 0 of the terrain overhaul: the three packages read before anything is changed.

**How it was read.** Three research agents read the packages' PDFs, their source and the installed URP 17.6 source. The lead agent then checked the claims that decide the architecture against the source itself, and looked at the stamps' height and hillshade previews. Nothing was run in Unity, because the owner's editor was open, so every statement about behaviour in the editor is read from source and is marked **not run**.

**Marks used below.**

- *doc*: the vendor's PDF, with its page.
- *source*: the installed package's code, with file and line.
- *measured*: the agents' own decode of the files.
- *not run*: read from source, not yet seen working on Unity 6000.6.8f1.

**Paths.**

- `CORE` is `Packages/com.jbooth.microsplat.core/Scripts`.
- `URPM` is `Packages/com.jbooth.microsplat.urp6p3/Scripts/Editor`.
- `STAMPS` is `Assets/Rowlan/Terrain/Stamps/Examples`.

---

# 1. What is installed

| Package | Version | Where | In git |
|---|---|---|---|
| MicroSplat Core | 3.9.0 (store 3.9.50) | `Packages/com.jbooth.microsplat.core` | ignored |
| MicroSplat URP for Unity 6.3 | 3.9.0 | `Packages/com.jbooth.microsplat.urp6p3` | ignored |
| StampIT! Collection, the free set | 1.19.0 | `Assets/Rowlan/Terrain/Stamps/Examples` | ignored |
| Unity Terrain Tools | 5.3.3 | through `com.unity.feature.worldbuilding` | registry |

**No other MicroSplat module is installed.** Core shows a store tile for each module whose scripting define is absent, and the only MicroSplat define in the project is `__MICROSPLAT__` (*source*: `CORE/Editor/MicroSplatShaderGUI.cs:476-539`; `ProjectSettings/ProjectSettings.asset:604`). The module PDFs ship with Core as advertising; they describe software the project does not have.

---

# 2. MicroSplat Core

## 2.1 What it is

A shader generator for Unity terrains. It packs every layer's textures into two texture arrays and draws the terrain in one pass, however many layers there are. Unity's own terrain shader takes four layers a pass, so the slice's eight layers are drawn twice today.

## 2.2 Architecture

- **The component.** Each terrain gets a `MicroSplatTerrain` component that holds a template material (*doc*: Documentation p.19).
- **The material.** `Sync()` clones the template into a hidden, unsaved instance and assigns it to `terrain.materialTemplate` (*source*: `CORE/MicroSplatTerrain.cs:214-236`). Only the template is edited.
- **The generated files** lie in a `MicroSplatData` folder beside the terrain's data asset (*doc*: p.4; *source*: `CORE/Editor/MicroSplatUtilities.cs:534-573`):

| File | Holds |
|---|---|
| `MicroSplat.shader`, `MicroSplat_Base.shader` | the generated shader and its fallback |
| `MicroSplat.mat` | the template material |
| `MicroSplat_keywords.asset` | the chosen features |
| `MicroSplat_propdata.asset` | the per-texture values, a 32 × 32 float texture |
| `MicroSplatConfig.asset` | the list of source textures |
| `MicroSplatConfig_diff_tarray.asset`, `…_normSAO_tarray.asset` | the two packed texture arrays |

- **Names bind the files.** They find each other by name, so they are never renamed; the folder may be moved (*doc*: p.4, p.25; FAQ p.5).

## 2.3 Converting a terrain

1. Add `MicroSplatTerrain` to the terrain and press **Convert to MicroSplat** (*doc*: p.3).
2. It reads each `TerrainLayer`'s colour, normal and mask map (metal R, occlusion G, height B, smoothness A), and its tile size (*source*: `CORE/Editor/TextureArrayConfigEditor.cs:1815-1829`).
3. It reads nothing else from a layer: not its tint, normal scale, metallic or smoothness.

**The whole conversion is callable from a script.** `MicroSplatTerrainEditor.ConvertTerrains(Terrain[], TerrainLayer[])` is public and static, and so is every piece it calls (*source*: `CORE/Editor/MicroSplatTerrainEditor_Convert.cs:17`, checked by the lead). No dialog blocks it. It is **not run**.

## 2.4 The splat maps stay Unity's

MicroSplat binds the terrain's own alphamap textures as `_Control0…7` (*source*: `CORE/MicroSplatTerrain.cs:265-266`). `TerrainData.SetAlphamaps` keeps working, so the project's own code keeps painting the ground. This is the fact the whole plan stands on.

## 2.5 The texture arrays

| Setting | Options | Source |
|---|---|---|
| Maps for each texture | colour, height, normal, smoothness (or roughness, inverted), occlusion; emissive and metal optional | *source*: `CORE/TextureArrayConfig.cs:286-301` |
| Missing maps | generated into the array, never written to disk: a normal from the height or the colour, a height from the normal | *doc*: p.4; FAQ p.5 |
| Packing | height into the colour's alpha; smoothness and occlusion beside the normal | *source*: `TextureArrayConfigEditor.cs:1257-1274` |
| Size | 32 to 4096, one size for each array | *source*: `TextureArrayConfig.cs:51-61` |
| Compression | automatic gives DXT5 on a PC, BC7 at High quality | *source*: `TextureArrayConfigEditor.cs:1636-1643` |

**Three traps.**

- **The colour's alpha is overwritten with height.** The slice's setts carry their smoothness in the colour's alpha (`VeilmouthLook.cs:159`). After conversion that is lost unless the smoothness is mapped by hand.
- **The header's channels override each entry's** unless set to Custom, and Convert sets them to the mask-map convention (*source*: `Convert.cs:102-104`).
- **Packing touches the source textures.** Each compressed source is reimported uncompressed and back, and a source whose importer is capped at 256 px or less is set to 4096 uncompressed for good (*source*: `TextureArrayConfigEditor.cs:809-821, 1552-1576`).

## 2.6 Blending

Four modes (*doc*: p.10-11): Height Blended, Unity Linear, Hybrid Height Linear, Normalized Linear. Height blending lets the higher texture win at a transition, so stones stand out of mud and grass grows from between setts. It needs a real height map for each texture to look right; a generated one is a guess.

Convert turns on **Hybrid Height Linear**: height-blended near, linear from a distance set on the material.

## 2.7 Per-texture controls in Core

UV scale and offset, UV rotation, tint, brightness, contrast, saturation, colour intensity, normal strength, smoothness, occlusion strength, metallic, height offset, height contrast, interpolation contrast, and a few shading extras (*source*: `CORE/Editor/MicroSplatBaseFeatures.cs:1799-1900`).

Each is a shader feature: it costs nothing while off.

**A trap for scripts.** The value table starts at zero. Turning on normal strength without writing its row gives a terrain with no normals (*source*: `CORE/MicroSplatPropData.cs`; defaults at `MicroSplatBaseFeatures.cs:1786-1797`).

## 2.8 Limits and cost

- **32 layers at most**, in eight control maps.
- **Four textures a pixel.** The shader sorts the weights and samples the four heaviest. A fifth is not drawn at that pixel.
- **One control sample for every four layers.** Twelve layers cost three; sixteen cost four.
- **Eight array samples a pixel** at the best quality with normal maps (*source*: `MicroSplatBaseFeatures.cs:1702-1749`).

The vendor's advice (*doc*: Optimization Guide p.3-8): the lowest texture count that fits; aggressive branch sampling; unused per-texture features off; smaller textures before larger ones.

## 2.9 What Core does not have

| The request asks for | Core | Needs |
|---|---|---|
| Anti-tiling: detail and distance noise, distance resampling | no | the Anti-Tiling module |
| Stochastic sampling, texture variants | no | Texture Clusters |
| Automatic texturing by slope and height | no | Procedural Texturing |
| Blending meshes into the ground | no | Terrain Blending |
| Triplanar projection on steep ground | no | Triplanar |
| Wetness, puddles, streams | no | Puddles, Streams, Lava and Wetness |
| Tessellation and parallax | no | Tessellation and Parallax |
| A tint map over the whole terrain | no | Global Texturing |

Against tiling, Core has only **Control UV Noise**, which distorts the splat-map lookup and not the textures (*doc*: p.12), and each texture's own UV scale and rotation.

---

# 3. MicroSplat URP for Unity 6.3

## 3.1 What it is

An adapter that makes Core write its shader for URP. It adds no feature of its own. Its PDF is two pages and names no limitation (*doc*: URP p.1-2).

## 3.2 On Unity 6000.6.8f1 and URP 17.6

The package declares Unity 6000.3 as its minimum. The author supports LTS versions only (*doc*: FAQ p.2), and the project's Unity is three minor versions later.

**Read from source, the adapter is expected to work. It is not run.**

- **It is chosen.** Core asks for the 6.3 adapter on any Unity from 6000.3 up to 6000.7 (*source*: `CORE/Editor/MicroSplatShaderGUI_Compiler.cs:533-538`, checked by the lead).
- **Every include exists.** All 25 files the templates include are present in the installed URP 17.6.0 and Core RP packages.
- **Every function it calls exists** with a matching signature: `UniversalFragmentPBR`, `InitializeInputDataFog`, `SAMPLE_GI`, the GBuffer packing and the rest.
- **Forward+ is compiled in.** The forward pass declares `_CLUSTER_LIGHT_LOOP`, the keyword URP 17.6 uses (*source*: `URPM/fragments/URP6p3/microsplat_template_urp6p3_passforward.txt:52`, checked by the lead). So the gas lamps should light the setts.
- **Its passes** are forward, GBuffer, shadow caster, depth only, depth normals and meta. The project's SSAO reads depth normals, and that pass is there.

## 3.3 Defects found in the vendor's code

| Defect | Where | Consequence | What the overhaul does |
|---|---|---|---|
| Both of the adapter's `.asmref` files name GUIDs that do not exist, two characters transposed | `URPM/JBooth.MicroSplat.CoreURP6p3.Editor.asmref:2` against `CORE/Editor/JBooth.MicroSplat.Core.Editor.asmdef.meta:2`, checked by the lead | Unity compiles the adapter into `Assembly-CSharp-Editor`. It works, but it disappears whenever any editor script of the project fails to compile | names the adapter by its keyword string, never by type; checks which adapter compiled the shader |
| Core overwrites the URP 6.3 keyword in its own list | `Compiler.cs:343-344`, checked by the lead | a script that does not pass the keyword gets a built-in-pipeline shader, which draws pink, with no error | passes `_MSRENDERLOOP_UNITYURP6P3` explicitly |
| The rendering-layers output is a float where URP 17.6 expects an integer | the forward and depth-normals templates | wrong data, but only when rendering layers are on. They are off in this project | leaves rendering layers off; recorded as a limitation |
| Keywords newer than the templates are absent: the screen-space reflection and irradiance features, bicubic lightmap sampling | the templates | those features do not reach the terrain. None is used | none |

The two `.asmref` files are the vendor's and are left as they are.

## 3.4 What MicroSplat changes on the terrain in the editor

Read by the lead in `CORE/MicroSplatTerrain.cs`.

- **`materialTemplate`** becomes the hidden instance. The saved scene holds no terrain material; the component sets it at load.
- **`basemapDistance`** is set to 99999.
- **`terrainLayers` is rewritten** whenever the terrain's layers differ from the texture list in count or in any colour texture (`:335-437`). MicroSplat then builds bare layers, with colour only, and saves them through a delayed call.

The last point collides with the builder: `VeilmouthCity.PaintStreets` sets the terrain's layers itself. Two writers of one list would fight at every rebuild. §6.3 settles it.

---

# 4. StampIT! Collection

## 4.1 What it is

**A library of pictures. It ships no code.** The folder holds 73 height maps, 72 Unity brush assets that point at them, a PDF, two text files and one asset for MicroVerse: no script, shader, window or component (*measured*: a walk of `Assets/Rowlan`; `Extensions/` is empty).

The PDF is the manual of the paid Ultimate collection (*doc*: p.1); the free set is a sampler of it, one to four maps from each of 44 families.

**It makes no water.** It holds heights only: no ocean, no river surface, no texture.

## 4.2 The height maps

| Property | Finding |
|---|---|
| Size and depth | 4096 × 4096, 16 bits, every file (*measured*) |
| Range | each file spans the whole range, 0 to 65535 |
| Channels | 62 greyscale; 11 are RGBA with three equal channels |
| Importer | 4096, format R16, no mip maps, clamped, not sRGB (*source*: the `.meta` files) |
| Readable from script | **6 of 73.** The other 67 have Read/Write off |

## 4.3 The documented ways to use it

1. **Unity's Stamp Terrain tool**, by hand: choose a brush, set size, height and opacity, and click (*doc*: p.11).
2. **MicroVerse**, not installed.
3. **Gaia**, not installed.
4. **Any tool that reads a height map** (*doc*: p.15).

The manual gives no guidance on rotation, on heights for each landform or on blending one stamp into the next.

## 4.4 Why the hand tool is not the way here

- **The ground is written by code and the scene is rebuilt by code** (audit §4.4). A stamp clicked into the saved terrain would be overwritten the next time the ground is shaped, and could not be repeated.
- **Unity's brush type is internal**, so a script cannot drive the brush assets (*source*: Unity's `Brush.cs`, `internal partial class Brush`).
- **The tool's Set mode pulls the ground toward a level** and erases what is there (*source*: Terrain Tools' `PaintHeight.shader:113-132`), which is wrong beside a city that must not move.

So the stamps are read as height data and composited by the project's own code. That is the manual's fourth way.

## 4.5 Reading a stamp from code

| Way | Verdict |
|---|---|
| `Texture2D.GetPixels` on the imported asset | fails on 67 of 73: not readable |
| `ImageConversion.LoadImage` on the file | loads a PNG at 8 bits. At 30 m of height that is steps of 12 cm, visible terraces |
| Changing the vendor's `.meta` files | changes vendor files, which the project does not do |
| A GPU blit of the imported asset into a float target, then a read-back | works, but needs graphics and depends on the importer |
| **Decoding the PNG's bytes at 16 bits in C#** | **chosen**: exact, the same on every machine, needs no graphics, and touches no vendor file. Only two PNG row filters occur in the set (*measured*) |

## 4.6 The stamps, surveyed

All 73 were decoded and viewed as height and hillshade. The numbers that matter for a walkable island are the border, since a stamp whose edge is not zero leaves a square seam, and the steepness, since amplitude over footprint sets the slope.

**Chosen for the island** (the lead's judgement from the previews; footprints and amplitudes are authoring defaults, set when the layout is drawn):

| Stamp | The landform | Use | Border |
|---|---|---|---|
| Meadows 12 | very soft large hills and one winding valley | the main downs and the upper river's valley | whole tile: a radial mask |
| Meadows 28 | five soft rounded hills | the lower hills of the south | whole tile: a radial mask |
| Tuscany Rolling Hills 05 | parallel smooth ridges | rolling farmland behind the hamlet, at a low amplitude | whole tile: a radial mask |
| Layered Rocks 15 | a hill with a ridge to the north and a low shelf to the south | the north-west headland | zero: none needed |
| Rugged Shapes 12 | a near-round dome with a cracked surface | a single down | zero: none needed |
| Seaside Cliffs 02 | a coast in profile: high inland, spurs and coves falling to the sea | the coast's relief, laid along the shore; low for coves, higher for sea cliffs | directional: its two cut sides are faded |
| Seaside Cliffs 06 | the same with deeper ravines | the wilder north-west shore | directional |
| Sandy Beaches 05 | four very soft lobes round a hollow | dunes and soft ground behind the beach | a soft fade |
| Swamp 12 | a flat plain cut by a branching creek network | the salt marsh and its tidal creeks, at 3–4 m | whole tile: a radial mask |
| Rough Cliffs 18 | a craggy crest with a smooth apron | a rocky headland | a soft fade |
| Talus Rock Clusters 19 | a cluster of crags on an apron | rocky shore and skerries | a soft fade |
| Rock Shapes 22 | one knobbly rock massif | a single outcrop | zero: none needed |
| Chipped Plateaus 36 | a flat-topped plateau with a cliff rim | a low sea stack | zero: none needed |

**Looked at and set aside.**

- **Shores 09**, the nearest to a whole island, has a thin rim ridge round its plateau that reads as a built bank.
- **Islands 07** has a cliff rim higher than its interior.
- **Rolling Hills 06** and **Jungle Rivers 03** are far rougher than their names.
- **The alien, canyon, crater, moon, volcano, mesa and strata families** do not belong on an English island.

## 4.7 No stamp is the island

No whole-island stamp can be used as the base, for a reason that has nothing to do with quality: the city's level ground, the river along its stone banks and the quay are fixed (audit §5.1), and a ready island fits none of them. So the **outline** of the island, the city's ground and the river's course are drawn by the project's code from the layout, and the **landforms** on and around them come from the stamps. This is the split the request itself asks for: relief from StampIT, water and composition apart.

---

# 5. How the three fit together

| Job | Who does it |
|---|---|
| The island's outline, the city's ground, the river's course, the sea bed | the project's `VeilmouthGround`, from the layout |
| Hills, valleys, coast relief, marsh creeks, outcrops | StampIT's height maps, composited by `VeilmouthGround` with scale, rotation, amplitude and a mask |
| Which surface lies where | the project's painter, writing Unity's splat maps by slope, height, wetness, the shore and the forest |
| Drawing the surfaces | MicroSplat Core through the URP 6.3 adapter |
| The sea and the river's surface | WaterWorks, already the scene's water, on a larger sheet |

The packages do not touch one another. StampIT ends at the heightmap; MicroSplat begins at the splat maps; the project's code is the only thing that writes either.

**The order of work.**

1. Shape the ground: outline, then stamps, then the river and the coast cut in.
2. Paint the splat maps from the finished relief.
3. Convert the terrain to MicroSplat and build the texture arrays.
4. Rebuild the city and the outskirts on the new ground.
5. Lay the sea.
6. Render, measure and correct.

---

# 6. What the overhaul uses, and how

## 6.1 Used

| Feature | Why |
|---|---|
| MicroSplat's one-pass drawing | twelve layers in one pass, where eight now take two |
| Texture arrays with real height, occlusion and roughness maps | height blending and a wet look need them |
| Height blending (Hybrid Height Linear to begin with) | natural edges between grass, mud, gravel and rock |
| Per-texture UV scale and rotation | each surface at its true size; turned layers hide the grid of repeats |
| Per-texture tint, brightness and saturation | the muted palette of `WORLD_STYLE_GUIDE.md` without repainting textures |
| Per-texture smoothness | the wet look of mud and shore, in place of the Wetness module |
| Thirteen StampIT height maps | the landforms of §4.6 |
| WaterWorks | already in the scene |

## 6.2 Not used, and why

| Feature | Why not |
|---|---|
| Every MicroSplat module of §2.9 | not installed; the request forbids new paid assets |
| MicroSplat's Convert button | not repeatable; the same calls are made from a batch method |
| MicroSplat's weight limiting | destructive, and the painter already keeps to four layers a texel |
| Custom or compressed splat maps | they end painting from code |
| Control UV Noise, at first | it would bend the straight edges of the streets, which are painted in the same maps. Tried only if the land's transitions read as straight lines |
| Unity's Stamp Terrain tool and the brush assets | §4.4 |
| MicroVerse and Gaia content | not installed |
| The deferred path | the project renders Forward+ |

## 6.3 Decisions the research forces

1. **One list of surfaces, owned by the project.** A single table in code names each surface in order: its terrain layer, its source maps and its paint index. The terrain's layers, MicroSplat's texture list and the painter are all written from it, so MicroSplat finds nothing to rewrite (§3.4).
2. **The slice gets its own layer assets.** Layers 0–4 are the Overworld's (audit §3.4) and are not changed.
3. **Twelve textures at most**, three control samples: about ten natural surfaces and the city's setts and cobbles. The flagstone layer is no longer painted and is dropped.
4. **Tiling is fought with what exists**: two or three variants of grass and of earth mixed by a large noise in the splat maps; a different, turned UV for each layer; height blending; and the grass detail layer near the camera, which S3b.0 already planned. The haze covers the distance.
5. **Slope and height rules stay in the project's painter**, extended from the present one-layer-a-texel rules to soft bands broken by noise.
6. **Arrays at 1024 px to begin with**, about 34 MB for twelve layers in both arrays, raised to 2048 only if close views are soft. An authoring default, measured in the frame probe.
7. **The setts keep their smoothness.** It is mapped by hand from the colour's alpha, and the streets at night are compared before and after.
8. **Regenerable files stay out of git.** The two texture arrays are large and rebuild from their sources. The generated shader contains the vendor's shader source, which the repository, like every vendor pack, does not carry; it is written again from the keywords asset by the same batch method. This is put to the owner (§8).

---

# 7. Risks

| Risk | How likely | What happens | Answer |
|---|---|---|---|
| The generated shader does not compile on URP 17.6 | low, by the static check | a pink terrain under the whole city | the first batch step converts a copy of the terrain in a test scene and reads the shader's messages before the slice's scene is touched |
| The streets look different at night | medium | the setts lose their wet sheen under the lamps | §6.3 item 7; night renders before and after |
| The frame grows | low: one pass replaces two | the 4.6 ms margin shrinks | `FrameProbe` before and after, in the same seven views |
| Tiling shows on open ground | medium, with no anti-tiling module | repeats on the downs | §6.3 item 4; judged in renders from eye height and from above |
| A rebuild and MicroSplat fight over the layers | certain without §6.3 item 1 | bare layers, lost tuning | §6.3 item 1 |
| Height blending looks wrong on a texture with no real height map | medium | mushy or inverted transitions | real PBR sets for the natural surfaces (§8) |
| A stamp leaves a seam | certain without masks | a square outline in the hills | every whole-tile stamp is masked; the border median is subtracted |
| Slopes too steep to walk or fight on | medium | ground the Combat Grid closes at 45° | amplitude over footprint is set from the measured gradients; the painter marks what is left as rock, and a test bounds the share |

---

# 8. Open, for the owner

The questions that the research and the audit cannot settle are put to the owner in the session's report, with options and a recommendation each:

1. Whether this overhaul is the slice's Veilmouth alone, with the "MicroSplat is out of the pipeline" rule lifted for that one terrain.
2. Whether the river gains a north arm, so that water rings the city.
3. What replaces the snowy mountain backdrop of the city plan §26.2, which an island contradicts.
4. Where the West Road ends, now that it cannot leave the area.
5. Where the natural surfaces' PBR textures come from: the project holds none with height, occlusion and roughness.
6. Whether MicroSplat's generated shader and arrays stay out of git.
7. When the owner's editor is closed, so that the batch runs can begin.
