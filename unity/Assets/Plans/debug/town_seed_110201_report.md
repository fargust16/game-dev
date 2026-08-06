# Town Debug Report

- Seed: `110201`
- Center: (0, 0)
- Bounds verts: 38, area≈213665 m²
- Landmark: MarketSquare

## Counts

| Layer | Count |
|-------|------:|
| Portals | 3 |
| Road nodes | 446 |
| Road edges | 456 |
| Dead-end edges | 45 |
| Degree-1 nodes | 168 |
| Districts | 3 |
| Blocks | 408 |
| Plots | 862 |
| Buildings | 824 |
| Buildings with template | 451 |
| Placed sockets | 1323 |
| NPC sockets | 509 |
| Prop sockets | 814 |
| Keeper roles | 176 |
| Open spaces | 208 |

## Overlap / coverage probes

- Road centreline samples: **15950**
- Samples inside **building** footprints: **708** (4.4%)
- Samples inside **block** polygons: **916** (5.7%)
- Buildings touched by a road sample: **88** / 824
- Building footprint area / bounds area: **19.3%**

## Findings

1. OK roads-vs-buildings: only 4.4% road samples inside footprints.
2. OK roads-vs-blocks: only 5.7% road samples inside block polygons.
3. OK stubs: degree-1 nodes=168 (portals=3, dead-ends=45).
4. OK templates: 451/823 non-landmark buildings have interiors (55%).
5. OK sockets: total=1323 npc=509 prop=814 keepers=176.

## How to read the SVG

- White stroke: town boundary
- Amber thick: arterial roads; orange/yellow/grey: secondary→alley
- Dark outlines: blocks
- Brown filled: building footprints
- Magenta: landmark footprint; light gold: landmark plaza
- If amber lines cut through brown fills, streets are not corridors — data bug, not baker-only.

## Road class histogram

- Arterial: 3
- Secondary: 3
- Tertiary: 57
- Alley: 393
- Service: 0

## Sample building footprints (first 8)

- #0 Commercial template=AlleyShop floors=3.18 verts=3 centroid=(41.233,-112.126) area=58.821
- #1 Commercial template=MarketHall floors=4.501 verts=5 centroid=(33.276,-70.767) area=81.7
- #2 Residential template=WorkerHouse floors=4.358 verts=5 centroid=(101.746,-80.252) area=68.308
- #3 Commercial template=AlleyShop floors=2.649 verts=4 centroid=(55.737,-96.634) area=42.739
- #4 Commercial template=Tavern floors=2.757 verts=4 centroid=(2.534,-123.876) area=157.528
- #5 Commercial template=Shop floors=5.296 verts=4 centroid=(-12.372,-129.555) area=26.014
- #6 Residential template=WorkerHouse floors=3.959 verts=4 centroid=(110.529,-146.056) area=137.949
- #7 Commercial template=MarketHall floors=2.657 verts=3 centroid=(79.194,-97.444) area=209.714
