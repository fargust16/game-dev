# Settlement Property — Lab Plan (S1–S8)

Status: Implemented through **S8** (lab)

Authority: [17 Game Systems Architecture](../../../17%20Game%20Systems%20Architecture.md) W.5, [15 Procedural Building Generation](../../../15%20Procedural%20Building%20Generation.md)

## Goal

Prove the land layer between District and Exterior Shell Grammar, hook onto overworld, mix districts, dress streets:

> Seed → DistrictKind → PropertyArchetype → PlotParcel + zones + boundaries → BuildingPad → Shell  
> → Property props → rear lane / double-frontage → Street Spine  
> → TownApproach → SettlementPose + terrain snap + dry search  
> → District mix (Commercial + GardenSquare + Worker)  
> → Street props on SettlementStreet ribbons

## Naming (locked)

| Term | Meaning |
|------|---------|
| **PlotParcel** | Geometric land lot (frontage × depth) |
| **Property** | Lot + zones + boundaries + props + building pad |
| **BuildingPad** | Region where the shell builds — **`PlotFootprint`** |
| **Boundary** | Fence / iron fence / wall Start→End segments + optional gate |
| **SecondaryPad** | Coach house / stable pad inside Service (Elite) |
| **Property Prop** | Dressing inside a lot zone (`PropertyPropSpec`) |
| **Street Prop** | Dressing on a SettlementStreet ribbon (`StreetPropSpec`) — not lot props |
| **PropSemantic** | Domestic / Service / Industrial / Occult / Civic |
| **Settlement Lane** | Service / rear lane strip (`SettlementLaneSpec`) |
| **Settlement Street** | Primary / cross ribbon (`SettlementStreetSpec`) |
| **Settlement Fabric** | Spine + blocks (`SettlementFabricLayout`) |
| **Settlement Pose** | World pose from TownApproach (`SettlementPose`) |
| **GardenSquare** | Open-space property (`IsOpenSpace`) — props only, no Shell |

## Non-goals

- Full StreetNetwork / pathfinding graph
- Building-mounted props (Shell)
- Production TownDress meshes
- Heightmap carving / real terraces under lots (deferred — slope float OK until zone drawing)
- Ownership / economy / NPC simulation

## How to run

### Property sandbox

1. `GameDev/Settlement/Open Property Lab Scene`
2. `Generate/District Mix Fabric` — Commercial DF + green plaza + Worker DF (+ street lamps/benches)
3. Other modes: single strip / rear lane / double frontage / street spine

### Overworld hook (S6–S7)

1. Generate Terrain → Water Zones
2. `GameDev/Overworld/Place Settlement At TownApproach` — dry District Mix on terrain
3. Clear: `Clear Overworld Settlement`

## Pipeline

```text
CreateDistrictMix / CreateStreetSpine
  → block parcels + street ribbons
  → StreetPropPlacer along centerlines (S8)
  → (overworld) AtPose → SnapFabric → RebuildStreetProps → dry ring
  → SettlementShellBridge skips IsOpenSpace
  → PropertyFeatureSpawner (lot + street props)
```

## S1–S8 done

- [x] Parcels, zones, boundaries, props
- [x] Rear lane / double-frontage / street spine
- [x] TownApproach pose + terrain snap + dry search
- [x] District mix + GardenSquare open space
- [x] Street props (lamps, bollards, benches, paving markers)

## Code map (S8)

| Path | Role |
|------|------|
| `StreetPropKind` / `StreetPropSpec` / `StreetPropRule` | Street-dress types |
| `StreetPropCatalog` | Primary / Cross spacing tables |
| `StreetPropPlacer` | Place along centerline + curb offset |
| `SettlementFabricLayout.StreetProps` / `RebuildStreetProps` | Fabric list; rebuild after pose/snap |
| `PropertyFeatureSpawner.SpawnStreetProps` | Greybox capsules/cubes |

## Deferred

- Per-lot terraces / carved pads (fix remaining floaters on steep slopes)
- TownDress production meshes for street + property props
