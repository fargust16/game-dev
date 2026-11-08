# World Style Guide

Version: 0.2

Status: Draft — written by the agent from the owner's answers of 2026-11-08 (city plan §24); the light and the mystic layer built in Veilmouth (city plan §24.7); waits for the owner's walk of C3

Created: 2026-11-08

Last Updated: 2026-11-08 (§5, §6: built in Veilmouth by `VeilmouthSky` and `VeilmouthMystic`, as city plan §24.7 records; the moon rises at 20:00 and fades in over its first hour. Earlier: first draft: one style for the city, its interiors and the land round it, and the mystic layer the owner chose)

Depends On:
[00 Vision](../../../00%20Vision.md), [11 Vertical Slice](../../../11%20Vertical%20Slice.md) (*Era and Mood*), [VEILMOUTH_CITY_PLAN.md](VEILMOUTH_CITY_PLAN.md) §24, [VICTORIAN_STREETS_PLAN.md](VICTORIAN_STREETS_PLAN.md), [VICTORIAN_INTERIORS_PLAN.md](VICTORIAN_INTERIORS_PLAN.md), [SLICE_ASSET_MAP.md](SLICE_ASSET_MAP.md), [10.3 Stealth and Detection](../../../10.3%20Stealth%20and%20Detection.md)

Referenced By:
[VEILMOUTH_CITY_PLAN.md](VEILMOUTH_CITY_PLAN.md) §24, [SLICE_ASSET_MAP.md](SLICE_ASSET_MAP.md)

---

# Purpose

The owner asked for one common style for the game and its world, built through geometry, textures and the models themselves, with a more game-like mystical atmosphere (2026-11-08). This guide is that style. It binds every scene the slice builds: the city, the interiors and the land round them.

It is the art side of [11](../../../11%20Vertical%20Slice.md)'s *Era and Mood*: an 18th–19th-century industrial city at the turn to the occult, after Lord of the Mysteries. Every number here is an authoring default.

---

# 1. The look in one line

**Soot-dark brick and pale stucco under slate, lit by cold gas and a red moon; the occult shows in small, old, deliberate marks, never as spectacle.**

Three tests for any new piece:

1. Would it stand in a Victorian city of about 1870? If not, it needs a reason in the world.
2. Does it read at 30 m by its silhouette and colour block, before its detail?
3. If it glows, does it mark something: a lamp, a ward, a ritual, a shop of strange goods? If it marks nothing, it does not glow.

---

# 2. Palette

The kit's materials are the palette (`VicMaterials`): seven tiling sets on two SRP Batcher shaders. A new material is a copy of one of them with its own tint, never a new shader.

| Group | Materials | Where |
|---|---|---|
| Brick | `Brick_Rich` (warm red), `Brick_Common` (brown-red), `Brick_Poor` (dark, rough), `Brick_Stock` (yellow London stock) | rich, middling, poor and dock districts in that order |
| Stucco | `Stucco_Cream`, `Stucco_White` | the Ridge, the civic buildings, the palace |
| Stone | `Stone_Light`, `Stone_Medium`, `Stone_Dirty` | dressings, plinths, steps; dirtier toward the river |
| Slate | `Slate_Even`, `Slate_Common`, `Slate_Rough` | every roof; the city has no tiled or thatched roof |
| Wood | `Wood_Rich` (near black green), `Wood_Brown`, `Wood_Green`, `Wood_Oxblood` | doors, frames and shop woodwork |
| Iron | `Iron` | railings, rails, balconies, lamp posts |
| Glass | the kit's glass by wealth | windows; clear near the eye, the mapped room far off (streets plan §4.2) |

Accents, used sparingly:

| Accent | Colour (sRGB) | Use |
|---|---|---|
| Gaslight | teal green, about (0.55, 0.95, 0.85) | the flame of every street lamp |
| Hearth | warm amber, about (1.0, 0.72, 0.42) | lit windows by night, interiors |
| Crimson moon | deep red, about (0.85, 0.18, 0.16) | the moon and its light |
| Ward | pale violet-cyan, about (0.6, 0.75, 1.0), faint | old signs and runes |
| Brass | dull gold | door furniture, the clock's face when it exists |

What the city does not have: saturated blue, purple or pink walls, white marble outside the palace and the angels, and bright green paint.

---

# 3. Materials and surfaces

- **Brick everywhere it is affordable, stucco where it is rich.** The kit's grime runs down from sills and up from the plinth; it is never cleaned away for the rich.
- **The ground:** setts on carriageways, flagstones on pavements, cobbles on lanes, yards and squares (city plan §13). Asphalt waits for a texture. Mud and sand only outside the walls and on the shore.
- **Wear grows toward the water and down the slope.** The Ridge is clean, the High Street worn, Lower Veil and the docks dirty.
- **One scale of detail.** A brick is a brick at the same world size on every house. The kit at ×1.25 keeps its brick's world size by redrawing it (owner, 2026-11-08), so a brick never grows with the house.

---

# 4. Geometry and silhouette

- **Vertical, not wide.** Houses are narrow and tall: 10–15 m fronts, 3–4 storeys, roofs that add a storey's height again. The skyline is torn by chimneys, lucarnes, gables and towers, never one flat line.
- **Roofs by wealth:** mansards with lucarnes on the Ridge and the High Street, gables and hips in the poor districts, pyramids on towers.
- **Landmarks over the roofs:** the clock tower (the tallest), the palace, the gate towers. Spires, a dome and gargoyles are wanted (asset map §9). Until they exist, no landmark is faked from other pieces.
- **Straight lines and arches.** Walls are straight and at right angles or at 45° (city plan §12); curves only in arches, lamps and iron.
- **The 1 m grid** and the kit standard hold for everything placed in a scene.
- **Heights read the districts:** the Ridge stands 4 m above the low town; its houses stand alone in gardens. The High Street is one cornice height. Lower Veil is lower and denser, with courts.

---

# 5. Light

Veilmouth's light follows the game clock (one tick is one game minute). Authoring defaults:

| Time | Sun or moon | Sky and fog | Lamps |
|---|---|---|---|
| Day (07:00–18:00) | a pale, warm sun, soft shadows | overcast blue-grey sky, a thin haze | off |
| Dusk (18:00–20:30), dawn (05:00–07:00) | a low amber sun going under | violet sky toward the east, fog rising in the low town | the lamps come on at 19:00 |
| Night (20:30–05:00) | the crimson moon, low over the sea in the south-east, with soft red light from it | a deep blue-black sky; fog low and thick by the water | teal gas flames; warm windows |

- **The colour space is Gamma** (the project's setting). The kit's materials and light were set for it (streets plan). A change to Linear is the owner's choice ([99 Open Questions](../../../99%20Open%20Questions.md) VS-COLOR).
- **Darkness is a mechanic.** By night the streets between lamps must be dark enough to hide in ([10.3](../../../10.3%20Stealth%20and%20Detection.md): darkness adds to the Stealth skill). The moon gives shape, not reading light. Lamps are pools, not floodlights: Lower Veil keeps its dark stretches.
- **Interiors** keep their own warm light, lit by the clock (interiors plan).
- **Built** in Veilmouth by `VeilmouthSky`, a component that sets the sun or moon, sky, ambient light, haze, lamps and fog from the game clock (city plan §24.7). The moon is up 20:00–05:00 and fades in and out over an hour, so the night of the table begins with it already risen.

---

# 6. The mystic layer

The owner chose three parts on 2026-11-08: the crimson moon and fog, the Gothic skyline, and occult signs. The fourth offered part, alchemy and soot, was not chosen and is not built.

`VeilmouthMystic` builds the moon, fog, lamps, wards and strange windows once under the `Veilmouth Sky` root, with their counts in city plan §24.7. The Gothic skyline stands as far as the kit's towers, chimneys and railings allow; its wanted pieces (§6.2) are not built.

## 6.1 The crimson moon and fog

- **The moon** is a disc in the night sky, red and a little too large, rising from the sea in the south-east. It is the one red light of the night.
- **Fog** lies low, 1–3 m deep:
  - along the river and the basin;
  - over the docks and the Sea Quay;
  - in Lower Veil's courts and lanes;
  - in the graveyard and on the Soot Stair.
  It thickens by night and thins by day. Fog is only seen: the Combat Grid, sight and stealth do not read it.
- **The twilight grade** at dusk shifts the whole image toward violet and amber.

## 6.2 The Gothic skyline

- Towers, chimneys and lucarnes break every roofline.
- Spiked iron railings stand round every railed plot, the graveyard and the palace forecourt.
- The angels and lions stand where city plan §23 puts them.

Wanted, with no stand-in until the models exist:

- spires for the chapel and a church;
- a clock face for the clock tower;
- gargoyles for the civic roofs;
- statues with covered faces;
- pointed lucarnes.

## 6.3 Occult signs

- **Old wards.** A faint sign over the keystone of a door, and runes on old stone:
  - the town wall by the gates;
  - the Ridge's retaining wall;
  - the Soot Stair's blind walls;
  - the three Ritual Site Candidates.

  They are centuries old, barely lit, and seen only up close or by night.
- **Strange windows.** A dim violet-green glow behind the glass of Harrow & Tincture and a few shops in Lower Veil.
- **What they are not.**
  - A ward carries no rule.
  - It is not an Information Source or a trace of a Weak Point. Those traces are left by people who joined the General ([21](../../../21%20Cult%20Campaign%20and%20City%20Control.md) §4, [11](../../../11%20Vertical%20Slice.md)) and must not be confused with dressing.
  - Whether wards later carry knowledge is an open question (99 VS-WARD).
- **How few.** A ward stands over about one door in eight in Lower Veil and Under the Ridge, and over none on the Ridge. Signs are rare, so each one is noticed.

---

# 7. Don'ts

- No glow without a reason (§1), no neon, no fantasy colours on walls.
- No mesh merging, baked atlas or `MaterialPropertyBlock` on the kit's houses (the kit's rules); a variant is a material asset.
- No new mesh, material slot or house from another store pack in the city without the owner (the kit's rules); the medieval packs are stand-ins outside the walls only.
- No landmark pieced together from other modules; a missing piece is wanted, as the furnishing rules ask.
- No fog, moonlight or grade that hides what a fight or a sneak needs to read.
- No timber framing or thatch in the city.

---

# 8. Where it applies

| Place | What carries the style |
|---|---|
| The city | the kit's houses and palettes (§2–§4), the street surfaces, the light (§5), the mystic layer (§6) |
| Interiors | Victorian Interiors, warm lamp light, the same period |
| Outside the walls | Polytope's green trees and grass, farms from FantasyMedievalTown_LITE until Victorian ones exist, the same sky, moon and fog |
