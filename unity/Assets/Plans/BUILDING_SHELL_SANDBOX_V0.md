# Building Shell Sandbox V0 — Micro-Plan

Status: Implemented (lab)

Authority: [15 Procedural Building Generation](../../../15%20Procedural%20Building%20Generation.md), [17 Game Systems Architecture](../../../17%20Game%20Systems%20Architecture.md) W.6

## Goal

Prove **Exterior Shell Grammar** in Editor without blocking Overworld V0:

> Seed → plots + StreetContext → archetype select → DNA sample → shell pieces → greybox strip

## Non-goals

- Full Settlement / city generator
- Interior Room Graph
- Production TownDress meshes (placeholders OK)
- Replacing Overworld V0 as the world-gen default

## How to run

1. Unity menu: `GameDev/Buildings/Shell Sandbox/Open Or Create Scene`
2. Generate by district:
   - `Generate/Poor` · `Worker` · `Affluent` · `Rich` · `Commercial`
   - `Generate/Compare All Districts` — five rows for side-by-side read
3. `Clear` / `Reseed And Generate`
4. Optional: `Bind Settlement Stub Demo` (contract hook)
5. Select a shell → gizmo shows bounds, street front, archetype/seed/district

Scene path: `Assets/Scenes/Building_Shell_Sandbox.unity` (created on first open)

## District profiles (fabric drivers)

| Id | Feel | Wealth | Floors | Density | Notes |
|----|------|--------|--------|---------|-------|
| Poor | cramped soot brick | 1–2 | 1–2 | very high | mostly Box; rare SideWing |
| Worker | terraced brick | 1–3 | 1–3 | high | mix Box / Wing / Bay |
| Affluent | cream plaster / slate | 2–4 | 2–4 | medium | Bay / Stepped / Wing |
| Rich | red brick + white trim | 4–5 | 2–4 | low | Bay / TwinGable / Stepped; fence+stoop |
| Elite | Victorian terrace / villa | 5 | 3–4 | very low | TwinGable / Bay dominant; never Box |
| Commercial | shop street | 2–4 | 1–3 | high | ShopChance high; mostly Box/Stepped |

### HARD LOCK — Non-overlap + floor-stack

**Authority:** [`SHELL_NON_OVERLAP_RULE.md`](SHELL_NON_OVERLAP_RULE.md) + `.cursor/rules/shell-non-overlap.mdc`

- Assemble **bottom-up**; **one footprint per storey** (Stepped/L/RearWing shrink upstairs — never two halves side-by-side).
- After each storey: roof vacated strip; continuing face → `AtticWindow`.
- Hierarchy piece names: `F0_WALL_…`, `F1_WINDOW_…` (proof of floor order).
- Console: `[ShellFloorStack] …`
- Chimneys flush on top roof; `ShellOverlapGuard` must pass.

### Massing types (anti-box, floor-stack footprints)

| Massing | Reads as | Typical districts |
|---------|----------|-------------------|
| Box | single rectangle every storey | Poor (always), Worker often |
| LShape | full L on lower storeys; wing roofs; main continues with attic step | Affluent / Rich / Elite |
| Stepped | both halves lower; one half continues with attic step | Worker+ / Commercial / Rich |
| RearWing | front+rear lower; rear roofs; front continues with attic back | Rich / Elite mansions |

**Door rule:** exactly one ground-floor street door on the primary slab.

Passing a `DistrictProfileDef` into selector + DNA sampler + binder is enough to retarget a whole street.

## Code map

| Path | Role |
|------|------|
| `Scripts/World/Buildings/Shell/` | Grammar, DNA, binder, sandbox lab |
| `Scripts/World/Settlement/` | `ISettlementLayout`, stub, shell bridge |
| `Scripts/Editor/ShellSandboxMenu.cs` | Editor menu |
| `Scripts/Editor/Tests/ShellGrammarDeterminismTests.cs` | Determinism + shop front + settlement bridge |

## Locked grid

- Bay = 2 m
- Storey = 3 m

## MVP archetypes

WorkerHouse · Shop · Townhouse

## MVP districts

Worker · Commercial · MixedOldTown

## Iterations covered in this lab

1. Rect strip + placeholders + determinism  
2. Floors 1–4 + wealth trim  
3. Gable/flat roofs + chimneys  
4. StreetContext + corner plots  
5. Archetype selector + district weights  
Later contract: Settlement stub → `IBuildingShellBinder`

## Done criteria

- [x] Menu generates 20+ distinct shells from seed 42  
- [x] Same seed → identical fingerprints (EditMode tests)  
- [x] Shops get street door/shopfront; portals optional  
- [x] No interior meshes in overworld shells  
- [x] Docs in [15](../../../15%20Procedural%20Building%20Generation.md) split Exterior vs Interior  

---

## Facade layering (playtest feedback — Rich row)

Observed: bay panels were either full `Window` or full `Blank`, so the street read as a glass curtain with odd blank doubles — not masonry with openings.

**Locked grammar fix:**

1. Every bay places a **Wall** first.  
2. Optional **Opening** (Window / Door / Shopfront) is a smaller overlay on that wall.  
3. Street upper floors use a **regular** window rhythm (no random Blank gaps).  
4. `Blank` as a full-bay facade fill is retired for public faces.

### Deferred dress (next art / grammar passes — not blocking hypothesis)

| Item | Notes |
|------|--------|
| Real window mesh variants A/B/C | Replace tint placeholders |
| Dormers / roof window arches | After gable roof modules exist |
| Entrance steps / stoop | Ground door bay prop |
| Gate / fence / plot boundary | Especially Rich / Affluent lots |
| Balconies, shutters, drainpipes | Wealth-gated trim layer |
| Cornice / pilaster kit meshes | Replace cube trim |

Keep these as Piece overlays + district props — do not invent a second generator.  
