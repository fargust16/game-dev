# 15 Procedural Building Generation

Version: 0.2

Status: Draft

Last Updated: 2026-08-07

Depends On:
[11 Vertical Slice](11%20Vertical%20Slice.md), [14 World Generation Roadmap](14%20World%20Generation%20Roadmap.md), [16 Architecture Pivot](16%20Architecture%20Pivot.md), [90 Glossary](90%20Glossary.md)

Referenced By:
[AGENTS.md](AGENTS.md), `unity/Assets/Plans/PROCEDURAL_TOWN_ASSET_LIBRARY.md`

---

# Purpose

Long-horizon architecture for **procedural, modular buildings** after the Architecture Pivot:

- **Exterior shells / landmarks** on the seeded overworld;
- **Interior scenes** (separate, free third-person, no grid);
- optional later DNA / room recipes for interior scene content.

This document is the authority for *how building content is generated as systems*.  
[16 Architecture Pivot](16%20Architecture%20Pivot.md) is the authority for *scene graph / movement / combat presentation*.  
[14 World Generation Roadmap](14%20World%20Generation%20Roadmap.md) is the authority for *implementation order* (**next code default: Overworld V0**).

Success question:

> Can we generate distinct exterior POIs and interior scenes from reusable recipes and Building DNA — without authoring finished whole-building prefabs or baking continuous in-world GridMap interiors?

---

# Design Philosophy

## Goal

Not beautiful facades.  
**Explorable buildings as two products:** overworld exterior presence + separate interior scenes with secrets, NPC anchors, and believable Victorian / industrial-occult mood.

## Authority chain (locked — post-pivot)

```text
Building DNA
    ↓
District / Biome Rules + Plot or POI slot
    ↓
Exterior Footprint / Shell (overworld)
    ↓
Door / portal link → Interior Scene id
    ↓
Interior Floor Stack + Floor Plans (scene-local)
    ↓
Room Graph (interior scene data)
    ↓
Vertical Connections
    ↓
Openings (doors / windows)
    ↓
Facade Modules + Roof Modules (overworld shell)
    ↓
Materials / Age / Damage
    ↓
Interior Dress (furniture / props / lighting)
    ↓
Secrets Overlay
    ↓
Gameplay Bindings (sockets, loot, quests, NPC roles)
```

Confront combat does **not** use interior geometry as grid truth — arenas use Theme Packs ([16](16%20Architecture%20Pivot.md)).

## Hard rules

1. **Never ship whole buildings as prefabs.** Prefabs are only *modules* (door, window, wall trim, stair flight, prop).
2. **Overworld / interiors:** free third-person collision spaces — not GridMap walkability as world law.
3. **Interiors are separate scenes** — not continuous in-world enterable fabric.
4. **Deterministic** from world seed + building id / plot id forks.
5. **Reuse rooms, not buildings.** Archetypes are recipes over a shared Room Library.
6. **Secrets are DNA overlays**, not a late optional art pass.
7. **Scale via systems first**, archetypes second. Do not write 50 full building docs before Room Graph exists.
8. Pre-pivot OrganicTown continuous GridMap bake path is **archive**, not the primary product.

## Nuance on “no prefabs”

Forbidden: drop-in Victorian house / tavern / church kits as generation authority.  
Allowed and required: modular mesh library (`Assets/Art/TownDress/Modules/…`) snapped to generated openings and shells — see `PROCEDURAL_TOWN_ASSET_LIBRARY.md`.

---

# Current Reality (do not ignore)

Pre-pivot OrganicTown v2 prototype (archive under `unity/`):

| Layer | Status | Notes |
|-------|--------|-------|
| Plot / footprint | ✅ prototype | Stage pipeline |
| Solid shell fallback | ✅ prototype | Non-templated buildings |
| BuildingTemplate cell paint | ✅ D1 prototype | Continuous-world enterable schematics — superseded as primary |
| Building Sockets | ✅ D1.3 prototype | Useful ideas for interior scene anchors |
| Occlusion HideWalls | ✅ D2 prototype | Not needed when interiors are separate scenes |
| Playable GridMap radius | ✅ P0 prototype | Superseded as world movement truth |
| Room Graph generator | ❌ | Still the long-horizon generator for **interior scenes** |
| Building DNA | ❌ | Still the upstream driver |
| Exterior shell on overworld | ❌ | Post-pivot V0+ product |
| Interior scene export | ❌ | Post-pivot primary product |

**Bridge strategy:** reuse DNA / Room Library / archetype cards for **interior scene generation**. Treat continuous `BuildingTemplate` → in-world GridMap as research only.

---

# Critique of Category-First Roadmaps

A roadmap that does Residential → Commercial → … → Interiors → Secrets is inverted.

Problems:

- Interiors and Room Graph are **core systems**, not Phase 10.
- Secrets/story are **cross-cutting DNA layers**, not after every civic type exists.
- Faction / Special buildings are usually **overlays** (cult basement under warehouse), not unique engines.
- Writing a full AAA spec for every archetype before Vertical Slice wastes design budget.

Improved order: **systems → Vertical Slice archetypes → district expansion → overlays → scale**.

---

# Architecture Roadmap (improved)

Implementation respects [16](16%20Architecture%20Pivot.md) + [14](14%20World%20Generation%20Roadmap.md): **Overworld V0 → Interior V0** before deep building DNA. Building-system phases below can be designed now; code lands in micro-plans after those rails exist.

## Phase 0 — Contracts & Truth (post-pivot)

**Deliverables**

- Exterior shell / POI slot on overworld
- Door link → Interior Scene id
- Interior scene free-move contract (no grid)
- Theme Pack ids for arenas spawned from building contexts (combat only)

**Acceptance**

- Player can approach an exterior, enter an interior scene, exit back
- Confront from interior/overworld uses arena Theme Pack — not interior grid bake

**Pre-pivot note:** Continuous OrganicTown enterable + HideWalls + GridMap path is archive only.

## Phase 1 — Core Architecture Systems

Reusable generators before new building categories.

| System | Responsibility |
|--------|----------------|
| **Building DNA** | Seeded identity that drives all later stages |
| **District Rules** | Wealth, style, density, crime, occult pressure |
| **Plot → Footprint** | Already largely present; add yard / court / rear alley tags |
| **Floor Stack** | Storey count, basement/attic flags, floor-to-floor rules |
| **Room Library** | Parametric room recipes (size ranges, adjacency, sockets) |
| **Room Graph Generator** | Nodes + edges from DNA + footprint |
| **Corridor Generator** | Circulation from graph |
| **Opening Generator** | Doors, windows, service hatches |
| **Stair / Vertical Generator** | Stairs, ladders, trapdoors, dumbwaiters |
| **Shell / Facade Module Binder** | Snap modules to opening sockets |
| **Roof Generator** | Hip/gable/flat + chimneys by footprint + wealth |
| **Material / Age / Damage** | Palette from DNA |
| **Interior Dress** | Furniture/prop/light recipes per room type |
| **Secrets Overlay** | Hidden rooms, false walls, tunnels (DNA-gated) |
| **Gameplay Binder** | NPC/loot/quest markers onto sockets |
| **Interior Scene Compiler** | Room graph → interior scene layout / greybox (free-move) |
| **Exterior Shell Binder** | Footprint → overworld shell + door portal |

**Exit criteria:** one DNA → one Worker House and one Tavern, deterministic exterior + interior scene, socketted, roofed greybox shell, no whole-building prefab.

## Phase 2 — Vertical Slice Archetypes

Prove the full stack on few buildings that teach the world.

| Archetype | Why first |
|-----------|-----------|
| Worker House | Dense residential fabric |
| Alley Shop / Booth | Tiny commercial, street edge |
| Tavern / Inn | Social hub + possible smuggler DNA |
| Apothecary | Curiosity + alchemy secret overlay |
| Small Church / Chapel | Public ritual + crypt potential |

These already exist as D1 templates — Phase 2 **upgrades** them to DNA + Room Graph + modules, not a rewrite from zero.

## Phase 3 — Commercial Fabric

Shop recipes sharing Shop Floor / Back Room / Storage / Flat Above:

Pharmacy, Bakery, Grocery, Book Store, Tailor, Jeweler, Pawn Shop, Restaurant, Newspaper Office, Divination Parlor, Pawn/Appraisal House.

Bank and Market Hall are larger graph templates (hall + offices + vault room recipe).

## Phase 4 — Industrial & Infrastructure

Factory, Warehouse, Brewery, Textile Works, Foundry, Gas Works, Water Station, Train Depot, Carriage House, Dock Warehouse.

Shared rooms: Machine Floor, Loading Bay, Overseer Office, Worker Barracks, Chemical Store.

## Phase 5 — Civic / Public

School, University wing, Library, Hospital / Sanatorium, Orphanage, Theatre, Museum, Post Office, Asylum annex.

Shared rooms: Classroom, Ward, Archive Stack, Stage House, Waiting Hall.

## Phase 6 — Authority & Force

Police Station, Guard Barracks, Prison, Watch Tower, Arsenal, Court, Tax Office, Archive.

Shared rooms: Cell, Interrogation, Evidence Locker, Muster Hall.

## Phase 7 — Sacred & Death

Church, Cathedral, Chapel, Cemetery grounds building, Monastery, Mortuary / Dead House, Underground Temple (special).

Shared rooms: Nave, Vestry, Crypt, Ossuary, Confession Booth, Reliquary.

## Phase 8 — Government & Elite

City Hall, Ministry, Governor annex, Noble Mansion, Estate wing, Clubhouse.

## Phase 9 — Faction & Occult Overlays (not separate cities)

Safe House, Intelligence Office, Smuggler Hideout, Cult Base, Secret Society HQ, Alchemy Laboratory, Artifact Vault, Spirit Medium House, Dream Parlor, Nighthawk / Red Glove safehouse analogues (placeholder faction names per Vertical Slice).

**Rule:** prefer injecting these as DNA `Secrets` / `FactionOwnership` into ordinary archetypes before spawning standalone hero buildings.

## Phase 10 — City-Scale Performance

- Bake caches / streaming by block
- LOD: exterior module → simplified shell beyond playable focus
- Interior generation lazy-on-enter for far blocks (data always seeded; meshes deferred)
- Memory budgets for props

## Phase 11 — Story Integration (with C1+)

POI / TipGiver / Decision Node attachment through sockets and DNA `StoryImportance`.  
Architecture creates opportunities; narrative content remains authored cards, not generated novels.

---

# Building DNA

Every instance carries a seeded DNA record. DNA is the single upstream driver.

## Fields (V1 set)

| Field | Type | Influences |
|-------|------|------------|
| BuildingType / Archetype | enum | Room recipe table |
| DistrictId | id | Style, materials, street manners |
| Wealth | Poor → Noble | Size, finish, prop density |
| Purpose | enum | Public hours, socket roles |
| FactionOwnership | none / soft / secret | Guard sockets, locked doors, overlays |
| ConstructionEra | decade band | Facade language, materials |
| ArchitecturalStyle | Victorian / Industrial / Gothic / Hybrid | Module set |
| OccupantProfile | count + roles | NPC sockets |
| CrimeLevel | 0–3 | Bars, rear exits, loot risk |
| OccultLevel | 0–3 | Hidden rooms, ritual props, wrong geometry hints |
| StoryImportance | 0–3 | Whether C1 may bind a Decision Node |
| DamageLevel | 0–3 | Broken modules, boarded windows |
| SecretsMask | flags | Tunnel, false wall, attic cache, basement lab… |
| Seed | ulong | Fork root for all child RNGs |

## Propagation rule

No stage may invent identity that contradicts DNA.  
If a tavern has `SecretsMask.SmugglerTunnel`, Corridor/Secret stages must reserve space; Facade must not seal the only escape without a readable alternate.

---

# Core Systems (detail)

## Room Library

Rooms are parametric generators, not unique meshes per building.

### Shared room recipes (starter set)

Living: Bedroom, Kitchen, Dining, Parlour, Hall, Stairwell, Landing, Attic Room, Basement Store  
Work: Shop Floor, Counter Room, Workshop, Office, Study, Library Stack, Classroom, Dormitory  
Care: Ward, Surgery, Mortuary Prep  
Force: Cell, Guard Room, Armoury  
Industry: Machine Floor, Loading Bay, Boiler Room, Chemical Store  
Hospitality: Bar, Taproom, Guest Room, Kitchen (service)  
Occult: Alchemy Lab, Ritual Room, Reliquary, Crypt, Hidden Vault, Séance Parlour, Dream Den  
Circulation: Corridor, Courtyard, Service Alley Access

Each recipe defines:

- Size range (cells)
- Required / optional adjacency
- Opening rules (street door? barred window?)
- Socket role hints
- Furniture / light recipes
- Wealth/occult variants
- Whether it may host a Secret child room

## Room Graph Generator

1. Select recipe kit from Archetype + DNA.
2. Fit rooms into footprint using subdivision / squarified split (deterministic).
3. Ensure street-facing public room when Purpose requires it.
4. Build adjacency edges; insert corridors if degree/distance fails.
5. Place vertical cores before facade lock.
6. Compile to per-floor cell paint (`BuildingTemplate`-compatible).

Failure = shrink optional rooms, never orphan the entrance.

## Vertical Connections

- Stair flights occupy reserved cores
- Basement if DNA or archetype allows (Wealth/Occult/Industrial)
- Attic if roof type + Wealth allow
- Ladders/trapdoors for poor / secret variants

## Facade & Roof

- Openings from Room Graph street edges
- Module bind: door/window/trim from TownDress library
- Roof from footprint OBB + style; chimneys from hearth/kitchen/boiler rooms

## Secrets Overlay

Runs after Room Graph, before final bake:

| Secret | Typical host | Readable residue |
|--------|--------------|------------------|
| False-wall room | Study, Vestry, Cellar | Misaligned trim, cold draft prop |
| Smuggler tunnel | Tavern cellar, Warehouse | Hatch under crates |
| Alchemy lab | Apothecary back, Mansion basement | Chemical smell props, locked door |
| Crypt extension | Church | Worn steps, memorial mismatch |
| Attic cache | Worker House, Medium House | Disturbed dust, padlock |
| Cult circle | Warehouse, Abandoned chapel | Wrong geometry, stained floor decal |

Secrets must leave **world residue** (Confront Constraints / Information Sources aligned) — not invisible designer teleports.

## Interior Scene Compiler

Compile rules (post-pivot):

- Room graph → navigable free-move interior greybox (collision meshes, not GridMap world truth)
- Door cells become portal anchors back to overworld / nested scenes
- Secret doors become diegetic interacts with residue
- Arena Theme Pack id may be suggested by DNA (tavern → `Tavern` arena) but combat geometry is separate

---

# Building Categories & Archetypes

## Residential

Poor House, Worker House, Middle Class House, Noble Mansion, Estate Wing, Tenement Flat Stack, Boarding House

## Commercial

Tavern, Inn, Underground Bar, Pharmacy, Bakery, Grocery, Book Store, Tailor, Jeweler, Pawn Shop, Bank, Restaurant, Newspaper / Print Shop, Divination / Tarot Parlor, Relic Appraisal House, Carriage Hire

## Industrial

Factory, Brewery, Warehouse, Textile Factory, Foundry, Power / Gas Works, Water Station, Train Depot, Dock Warehouse, Sewer Maintenance Access

## Public

School, University, Library, Hospital, Sanatorium / Asylum, Orphanage, Theatre, Opera, Museum, Post Office, Charity Relief House

## Military / Order

Guard Barracks, Police Station, Prison, Arsenal, Watch Tower, Fog Watch Post

## Religious

Church, Cathedral, Chapel, Cemetery Lodge, Monastery, Mortuary

## Government

City Hall, Governor Palace / Annex, Ministry, Court, Tax Office, Civic Archive

## Faction / Occult (LotM-leaning additions)

Secret Church Base, Secret Society HQ, Smuggler Hideout, Cult Base, Alchemy Laboratory, Artifact Vault, Safe House, Intelligence Office, Spirit Medium House, Dream / Opium Parlor, Beyonder Gathering Hall (placeholder naming), Detective Agency, Black Market Exchange (under Theatre / Warehouse)

## Special

Castle remnant, Palace wing, Lighthouse, Observatory, Haunted / Abandoned Mansion, Antique Collector House, Underground Temple, Clocktower, University Observatory Annex

### Additional LotM-suitable archetypes

- Steam Company Counting House  
- Harbour Customs Shed  
- Workhouse  
- Penny Dreadful Publisher  
- Funeral Tram Depot  
- Rain/Fog Pumping Station  
- Private Club with Veil membership  
- Abandoned Quarantine Ward  

---

# Archetype Specification Process

Do **not** create fifty full documents on day one.

## Process

1. Stabilize Phase 1 systems + Room Library.
2. For each archetype that enters production: fill an **Archetype Card** (short) then expand to full spec only when scheduled.
3. Store cards under `unity/Assets/Plans/buildings/` as `ARCHETYPE_<Name>.md` when written.

## Archetype Card (required fields)

- Purpose / gameplay role  
- Typical district  
- Footprint & floors  
- Room graph kit (list of Room Library ids)  
- Vertical rules  
- DNA defaults & rare variants  
- Socket roles  
- Secret overlays allowed  
- Dependencies on reusable systems  
- V1 / V2 / V3 depth  

## Full Spec (later, per scheduled archetype)

Use the user’s full checklist when a card graduates:

Purpose, gameplay role, district, footprint, floors, exterior, interior room graph, vertical circulation, room types, furniture/prop/lighting, window/door/roof rules, basement/attic, hidden rooms, passages, rituals, faction ownership, NPC archetypes, loot, quest hooks, rare/district/wealth/destroyed variants, performance, generation algorithm, system dependencies.

### V1 production set (write full specs first)

1. Worker House  
2. Tavern  
3. Apothecary  
4. Alley Shop  
5. Small Church  

---

# Story Integration

Architecture creates **decision space**, not cutscenes.

Examples (DNA-driven):

| Surface building | Secret / story hook |
|------------------|---------------------|
| Pharmacist | OccultLevel→Alchemy Lab; Bargain ingredients |
| Noble Mansion | Forbidden archive; Allegiance / Attention |
| Church | Crypt; ritual residue; faction soft ownership |
| Tavern | Smuggler tunnel; rumour Information Source |
| Warehouse | Cult circle; Side Event POI lazy window |
| Medium House | Séance parlour; Soft Memory / Veil |

Narrative layer consumes sockets + StoryImportance inside interior scenes / overworld POIs; it does not rescan continuous-world ASCII interiors.

---

# Performance Principles

- Generate **data** for the town; **mesh** near player / playable radius first  
- Cap prop instances by Wealth and room importance  
- Share materials; vary via tint / decal / damage  
- No per-building unique lightmap dependency for V1  
- Secrets that are never near player stay as data flags until activation  

---

# Mapping: Suggested Phases → This Doc

| Original suggestion | Disposition |
|---------------------|-------------|
| Phase 0 Core Systems | Kept as Phase 0–1 (expanded) |
| Phase 1–9 by building category | Reordered to Phase 2–9 after systems; categories are recipe packs |
| Phase 10 Interior | Pulled forward into Phase 1 Room Library / Graph |
| Phase 11 Secrets | Phase 1 overlay + Phase 9 faction pack |
| Phase 12 Story | Phase 11 + Vertical Slice narrative on new rails |

---

# Near-Term Project Alignment

| Now (code roadmap 14 / 16) | This document |
|----------------------------|---------------|
| **Overworld V0** | Exterior POI shells only |
| **Interior V0** | One hand or templated interior scene |
| Dress library import | Facade/Roof modules depend on it |
| Deep DNA / Room Graph | Design OK; implement after Interior V0 rails |

**Do not** jump to Phase 4–10 implementation before Vertical Slice archetypes prove DNA → Room Graph → Interior Scene.

---

# Open Design Forks

Recorded in [99 Open Questions](99%20Open%20Questions.md):

1. When does Room Graph replace hand-authored interior templates as authority?  
2. Additive interior load vs single-scene swap?  
3. How aggressive is OccultLevel visual “wrongness” in V1 without spoiling Veil?

---

# Documentation Transaction Checklist

- [x] Target document created  
- [x] Glossary terms added (Building DNA, Room Graph, Modular Building Pipeline)  
- [x] Open Questions updated  
- [x] Hierarchy / AGENTS / 14 / handoff linked  
- [ ] Per-archetype full specs — deferred to production schedule  

---

# Related Documents

- [16 Architecture Pivot](16%20Architecture%20Pivot.md)  
- [14 World Generation Roadmap](14%20World%20Generation%20Roadmap.md)  
- [13 City Generation](13%20City%20Generation.md) — legacy mood reference  
- `unity/Assets/Plans/PROCEDURAL_TOWN_ASSET_LIBRARY.md`  

- `unity/Assets/Plans/PLAYABLE_TOWN_P0_PLAN.md`  
