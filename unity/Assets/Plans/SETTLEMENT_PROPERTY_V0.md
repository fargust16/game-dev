# Settlement Property — Lab Plan (S1–S5)

Status: Implemented through **S5** (lab)

Authority: [17 Game Systems Architecture](../../../17%20Game%20Systems%20Architecture.md) W.5, [15 Procedural Building Generation](../../../15%20Procedural%20Building%20Generation.md)

## Goal

Prove the land layer between District and Exterior Shell Grammar:

> Seed → DistrictKind → PropertyArchetype → PlotParcel + zones + boundaries → BuildingPad → Shell  
> → Property props (zone tables + semantics)  
> → Rear lane / double-frontage block  
> → Street Spine fabric (primary + cross ribbons + block loop)

## Naming (locked)

| Term | Meaning |
|------|---------|
| **PlotParcel** | Geometric land lot (frontage × depth) |
| **Property** | Lot + zones + boundaries + props + building pad |
| **BuildingPad** | Region where the shell builds — **`PlotFootprint`** |
| **Boundary** | Fence / iron fence / wall Start→End segments + optional gate |
| **SecondaryPad** | Coach house / stable pad inside Service (Elite) |
| **Property Prop** | Dressing inside a lot zone (`PropertyPropSpec`) |
| **PropSemantic** | Domestic / Service / Industrial / Occult / Civic — reason to exist |
| **Settlement Lane** | Service / rear lane strip between frontages (`SettlementLaneSpec`) |
| **Settlement Street** | Primary / cross street ribbon (`SettlementStreetSpec`) — not a road graph |
| **Settlement Fabric** | Spine + blocks aggregated as `SettlementFabricLayout` |

## Non-goals

- Full StreetNetwork / pathfinding graph (no TownRoadGraph revival)
- Street props / paving materials (S8)
- Building-mounted props (Shell / ExteriorPiece)
- Production TownDress meshes (placeholders OK)
- Overworld TownApproach placement (S6)
- Ownership / economy / NPC simulation

## How to run

1. `GameDev/Settlement/Open Property Lab Scene`
2. District: `Generate/Poor|Worker|Affluent|Rich|Commercial|Elite Block` (single strip)
3. Block modes: `With Rear Lane`, `Double Frontage Block`, `Single Strip Block`
4. S5: `Generate/Street Spine Fabric` — 2 blocks, primary + north streets, west/east/mid crosses
5. Scene gizmos: grey primary streets, brown crosses, gold lane center strips

## Pipeline

```text
PropertyArchetypeFactory.ForDistrict(kind)
  → SettlementBlockLayout.Create | CreateWithRearLane | CreateDoubleFrontage
  → SettlementFabricLayout.CreateStreetSpine (S5: offset blocks + street ribbons)
  → PropertySpec { Parcel, Zones, Boundaries, Props, BuildingPad, Street… }
  → SettlementShellBridge (BuildingPad → Shell)
  → PropertyFeatureSpawner (boundaries + secondary + props)
```

## Archetypes

| Id | Boundary | Props (S3) |
|----|----------|------------|
| PoorRow | None | none |
| WorkerYard | LowFence rear U | crates / firewood / barrel in RearYard |
| AffluentGarden | IronFence closed + gate | planters, lamp, path, bench, hedge |
| RichTownhouse | IronFence closed + gate | formal front dress + service crates |
| ShopFront | None | industrial crates/barrels/cart in Service |
| EliteEstate | Wall closed + gate | drive lamps, statue, occult ward, service |

## S1–S5 done

- [x] Plot ≠ BuildingPad; zones; Shell bind
- [x] Boundaries Start→End; closed perimeter / rear U
- [x] Elite drive + secondary; Shop alley
- [x] Property props catalog + placer + semantic tags + placeholders
- [x] Rear lane strip + alley backs (`CreateWithRearLane`)
- [x] Double-frontage block (2× plots, north yaw=180, mirrored frontages, corners)
- [x] Street Spine: `SettlementStreetSpec` + fabric loop + translate helper; lab/tests

## Code map (S5 additions)

| Path | Role |
|------|------|
| `SettlementStreetKind.cs` / `SettlementStreetSpec.cs` | Primary / Cross ribbons + centerline |
| `SettlementPropertyTransform.cs` | Translate property / lane for composition |
| `SettlementFabricLayout.CreateStreetSpine` | Block loop along E–W spine |
| `SettlementPropertyLab` mode `StreetSpine` | Menu + street gizmos |

## Next (S6+)

Overworld `TownApproach` hook; district mix + open space; street props + TownDress meshes.
