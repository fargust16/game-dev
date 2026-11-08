# Test Runs and Heavy Operations

Version: 0.4

Status: Draft

Last Updated: 2026-11-08 (§5: the scene load diagnostic also goes into a dwelling and photographs each arrival. Earlier: §2, §5: the scene load diagnostic, run by name; `slice-play` leaves the Diagnostic category out, since a run by assembly also runs explicit tests. Earlier: `run_tests.ps1` for PowerShell, since PowerShell cannot run the `.sh` and its `bash` is WSL's. Earlier: first version: test sets, the Slow category, and how heavy runs are done, by the owner's request for faster runs)

Depends On:
[AGENTS.md](../../../AGENTS.md)

Referenced By:
[AGENTS.md](../../../AGENTS.md), `.cursor/rules/unity-csharp.mdc`, `tools/unity/run_tests.ps1`, `tools/unity/run_tests.sh`

---

# 1. Why runs are slow

- Unity runs EditMode tests one at a time on the editor's main thread and ignores NUnit's `[Parallelizable]`, so a batch run keeps one core busy. The tests are CPU logic, and `-nographics` turns the GPU off, so a GPU does not help them.
- Two editors cannot open one project. A second Unity could run only from a second copy of the project with its own `Library`, which costs gigabytes and a long first import; it is not used.
- **Measured** on 2026-11-06 (`Logs/tw_edit.xml`): all 662 EditMode tests took 1870 s. 17 fixtures of TerrainGen and the terrain bake took 1858 s of it, their shared setup in `OneTimeSetUp` included (up to 184 s for one class); the other 50 fixtures, 369 tests, took 12 s.
- Making the world's generation itself parallel (Jobs, Burst, `Parallel.For`) or moving it to the GPU would change TerrainGen, so it stays under the standing stops of `AGENTS.md`: only with the owner's separate authorisation.

So runs get faster by running less: the narrowest set that covers a change.

# 2. The sets

`tools/unity/run_tests.ps1 <set>` in PowerShell, or `tools/unity/run_tests.sh <set>` in Git Bash, runs one set in batch mode and prints a summary and any failed test. Both scripts live in the repository's root, so from `unity\` the PowerShell call is `..\tools\unity\run_tests.ps1 world`. PowerShell cannot run the `.sh`, and the `bash` it finds is WSL's, not Git Bash. Extra arguments go to Unity, so `-testFilter <namespace, class, or test>` narrows any set further.

| Set | Unity arguments | What it holds | Measured, 2026-11-06 |
|---|---|---|---|
| `slice` | `-testPlatform EditMode -assemblyNames GameDev.Slice.Tests.EditMode` | the slice's EditMode tests, 67 | under 10 s of tests |
| `slice-play` | `-testPlatform PlayMode -assemblyNames GameDev.Slice.Tests.PlayMode -testCategory "!Diagnostic"` | the slice's PlayMode tests without the diagnostics, 39 | 58 s for 19 tests; 551 s for 40 with the scene load diagnostic (2026-11-08), which is why it is left out |
| `world` | `-testPlatform EditMode -assemblyNames Assembly-CSharp-Editor -testCategory "!Slow"` | the world's tests without the Slow ones, 302 | 5 s of tests, 19 s with Unity's start |
| `slow` | `-testPlatform EditMode -testCategory Slow` | the 17 Slow fixtures, 293 tests | about 31 min |
| `all` | `-testPlatform EditMode` | every EditMode test, 662 | about 31 min |

Every set also takes `-batchmode -nographics -projectPath . -runTests -testResults <xml> -logFile <log>`.

# 3. Which set to run

1. After a change, run the narrowest filter that covers it, then its set. Slice work runs `slice`, and `slice-play` too when run-time behaviour or a scene changed.
2. Run `world` after a change to shared code outside the slice: `Assets/Scripts/World`, `Grid`, the kit, seeds. It is cheap, so run it whenever in doubt.
3. Run `slow` or `all` only when a change touches TerrainGen, the terrain bake, or what they read; before a milestone's acceptance; or when the owner asks. It goes to the background, and the agent goes on with work that needs no Unity.
4. A slow test is never deleted or weakened to make a set fast; it moves to Slow.

# 4. The Slow category

- A fixture is Slow when its whole run, setup included, took over 5 s in a full run. **Measured:** the 17 fixtures took 5.8–406 s; the next one took 2.9 s.
- The category goes on the class, never on single tests: `OneTimeSetUp` runs whenever any one of the class's tests runs, so a single fast test left out of Slow would still pay the whole setup.
- The 17, all in `Assets/Scripts/Editor/Tests`: `TerrainGenStage1Tests`, `TerrainGenStage2Tests`, `TerrainGenStage2ATests`, `TerrainGenStage2BTests`, `TerrainGenStage2BRobustnessTests`, `TerrainGenStage3ATests`, `TerrainGenStage3BTests`, `TerrainGenStage3DTests`, `TerrainGenStage3ETests`, `TerrainGenStage3E2Tests`, `TerrainGenStage3FTests`, `TerrainGenStage3FVerificationTests`, `TerrainGenStage4ATests`, `TerrainGenValleyTrajectoryTests`, `TerrainGenValleyNetworkTests`, `TerrainGenValleyTopologyTests`, `OverworldTerrainBakeTests`.
- A new fixture that builds a world or bakes terrain gets `[Category("Slow")]` when it is written. After a full run, measure again from the XML's `test-suite` durations when a fast set grows past a minute.

# 5. Other heavy operations

- One Unity at a time on this project, and only while the owner's editor is closed. This holds for tests, batch passes, renders, and bakes alike.
- A run expected to take over two minutes goes to the background. The agent tells the owner what is running.
- Unity writes a test XML only at the end, so a missing file is not a hang. Progress is read from the log and from the process's CPU time; a run is stuck only if its process takes no CPU.
- Renders need graphics, so they leave out `-nographics`. PlayMode tests run with it.
- **The scene load diagnostic** (`SceneLoadTimingTests`, category Diagnostic, explicit) runs only by name, with graphics: `-testPlatform PlayMode -assemblyNames GameDev.Slice.Tests.PlayMode -testFilter GameDev.Slice.Tests.SceneLoadTimingTests`, and `-sceneLoadProfile` to add the profiler's breakdown. A batch run selected by assembly runs `[Explicit]` tests too (measured 2026-11-08), so the `slice-play` set leaves the category out, and the diagnostic is run with this command line, not through the scripts. It takes about 3 minutes and writes its report and a picture of each arrival to `Logs/SceneLoad/`: the city cold, the Soot & Bell twice and a dwelling, each in and out. Run it after any change to the city's content or to how scenes load ([VEILMOUTH_REFERENCE_PLAN.md](VEILMOUTH_REFERENCE_PLAN.md) R1).
- Python balance simulations run outside Unity and may run alongside it.

# 6. The agent's shell

lean-ctx's allowlist does not let an agent run either script in its own shell. The agent runs the same Unity command line from §2, or runs `run_tests.ps1` in a tab of the owner's Terminal panel, where the owner sees it. `lean-ctx allow` for the scripts would let agents run them in their own shell too; that is the owner's choice.
