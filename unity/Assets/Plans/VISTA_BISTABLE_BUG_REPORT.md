# Bug report draft — for Pinwheel / Vista

Send as-is. Written for a Vista developer with no knowledge of our project.

---

## TITLE

Identical graph, configuration and execution plan produce two different stable
heightmaps; reading intermediate node outputs changes which one you get

## ENVIRONMENT

```
Unity            6000.5.7f1
Graphics API     D3D12
GPU              NVIDIA GeForce RTX 5070 Ti Laptop (ID=0x2f18)
Render pipeline  URP, TerrainLit
Vista            Personal
World            4 tiles, 1400 × 1400 m each, heightmap resolution 513
```

## EXPECTED RESULT

Generation is a pure function of the graph, the biome configuration and the seed.
Repeating it in a fresh Unity process yields the same heightmap.

## ACTUAL RESULT

It yields **one of exactly two heightmaps**. Which one appears varies between runs with
nothing changed. Anomalous runs are byte-for-byte identical to one another, so this is
not floating-point noise or partial GPU work — it is two distinct, deterministic outputs.

Tile heightmap hashes (16-bit dumps, first 12 hex chars of sha256):

```
STATE A ("canonical")   0_0 5bb160b43fc3   0_1 618277545584   1_0 ecf2cc8715e4   1_1 339b29d3cb79
STATE B ("anomalous")   0_0 4bde50e84ad5   0_1 eab3485133ae   1_0 b66a9f21a75f   1_1 16f1ffabfd59
```

Height range read straight off the terrain after generation:

```
STATE A   max 318.3 / 285.7 / 357.1 / 342.9 m     min 0.0 / 0.0 / 1.7 / 1.9 m
STATE B   max 354.0 / 321.2 / 393.1 / 378.6 m     min 0.0 / 0.0 / 1.7 / 1.9 m
```

State B is **+35.4 … +36.0 m** higher at every tile maximum while the minima are
unchanged. About 37 % of the world's cells differ, in large connected regions rather than
scattered pixels.

Observed frequency: roughly 30 % over ~60 full generations, but it clusters — individual
batches of six ranged from 0/6 to 6/6.

## REPRODUCTION

```bash
Unity.exe -batchmode -projectPath <project> \
  -executeMethod GameDev.EditorTools.OverworldBatchDiagnostics.BakeAndDump \
  -overworldSeed 110201 -overworldRebuildGraphs true -overworldExportPng true
```

**Each generation runs in its own Unity process.** No recompilation is involved. The
project is rebuilt from the same template graph at the start of every run.

## MINIMAL REPRODUCTION CONTEXT

Three `LocalProceduralBiome` instances, all spawned from code with fixed seeds:

```
order  0   SeaFloor     worldHeight  34   scaleY 0.049   heightBlend Replace
order 10   IslandBase   worldHeight 140   scaleY 0.200   heightBlend Replace
order 20   Mountain     worldHeight 530   scaleY 0.757   heightBlend Raise
```

The graph under investigation is the island one:

```
IslandBase_IslandPlateau_TerrainGraph
  105 nodes, 87 edges
  node structure hash  bde85a18a31ed6a2
  edge structure hash  09d7d3d625684ff2

execution
  80 executed nodes, order digest 1E691596, ONE graph execution per generation

request
  512 px, terrainHeight 700.0, seed 1383877999
```

Both hashes are computed with node/edge ids excluded, because Vista assigns fresh ids on
every graph rebuild.

## FIRST DIVERGENCE

Immediately after Vista's generation completes and the heightmap is read back — before
any seam matching or `TerrainData` post-processing on our side. The height ranges above
are taken at exactly that point.

## INVESTIGATION / EXCLUSIONS

All of the following were measured, not assumed:

| checked | result |
|---|---|
| graph rebuilt/patched twice | no — 24/24 runs, the patch never ran on an already-patched graph |
| executed per-biome graph differs | no — id-invariant structure identical in state B |
| duplicated geological branch | no — exactly one branch in both states |
| execution plan differs | no — 80 nodes, identical node-for-node, same digest |
| seed / configuration / scene differ | no — byte-identical |
| `GraphExecutionCache` | not applicable — one execution per fresh process, cache is instance-local |
| stale render-target contents | excluded — `clearContent = true` at 111 of 114 creation sites, and the clear path is real |
| render-target pool reuse | **not necessary** — with reuse disabled entirely, state B still occurred 3/6 |
| biome height blending applied twice | excluded — `Replace` and `Raise` are both idempotent |
| rigid vertical offset of a biome | excluded — a `Replace` biome moving up would move the minima, which do not move |
| GPU work incomplete at readback | explicit `GraphicsFence(CPUSynchronisation, AllGPUOperations)` before the readback: 5/6 still in state B, vs 6/6 without the fence |

## IMPORTANT OBSERVATION

**Any diagnostic that reads intermediate node outputs changes the outcome.**

Three independent probes each produced the *same third* stable heightmap
(`cdb816ac2e2e` for tile 0_0) and removed the bistability entirely:

```
VistaDebugger.BeginSession() node capture                         6/6 → third state
GPU min/max probe (Remap.compute FindMinMax), buffer per node     8/8 → third state
same probe with all buffers preallocated, no sync inside the loop 8/8 → third state
```

The last variant issues one extra compute dispatch per node output and reads nothing back
until the whole graph has finished, so it is a very small perturbation — yet it is enough.

For contrast, disabling render-target pool reuse also adds GPU work (fresh allocations and
clears) and the anomaly **persisted**. What the three probes have in common is not extra
work but an additional read of each node's output.

This makes the problem effectively unobservable from outside: every attempt to look at an
intermediate result moves the system into a different state.

## OPEN QUESTION

We are **not** claiming a proven race condition. The exact producer/consumer pair was
never localized, and we could not observe intermediate state without changing it.

The strongest accurate formulation:

> An identical graph, configuration and execution plan can produce two stable GPU
> outputs. Reading intermediate node outputs changes the resulting state, which suggests
> sensitivity to internal GPU execution or resource ordering, but the exact mechanism is
> not localized.

Questions we would value answers to:

1. Does `GenerationTask` completion imply that all GPU commands for the graph have
   retired, or only that the coordinator has finished issuing them? We read
   `GenerationTask.isCompleted` (`ProgressiveTask.cs:40`) as a CPU-side status flag set by
   `MarkCompleted()`, with no GPU semantics — is there a supported way to wait for actual
   GPU completion of a graph?
2. Is there any node type, or any node-to-node data dependency, whose result can depend on
   dispatch ordering or on when a resource is read?
3. Are there known interactions on D3D12 between the `DataPool` render-target lifetime and
   compute dispatch ordering?

## ATTACHMENTS

Available on request:

- 24 complete four-tile 16-bit heightmap dump sets in state A
- 8 in state B
- 14 in the probe-perturbed third state
- 108 generation logs covering every experiment above, including the graph structure
  census, the execution-order digest and the per-tile height ranges for each run
