# Unity Prototype — Lower Veil Vertical Slice

Engine lock: Unity (`6000.3.9f1` observed on the creator machine), URP, C#.

Design source of truth lives in the repo root docs, especially:

- [11.2 Milestone S0 Greybox](../11.2%20Milestone%20S0%20Greybox.md)
- [10.1 Grid World and Environment Actions](../10.1%20Grid%20World%20and%20Environment%20Actions.md)
- [12 Engine Choice](../12%20Engine%20Choice.md)

## First open

1. Open **Unity Hub** (licensed Editor GUI — batchmode create failed here without headless license)
2. Add project from `unity/`
3. Use editor `6000.3.9f1` (or closest installed Unity 6)
4. Let Unity regenerate `ProjectSettings/` / `.meta` / solution files
5. Install **URP** via Package Manager if not already present, create a URP asset, assign it in Graphics/Quality settings
6. Commit generated ProjectSettings + `.meta` files after the first successful open
7. Prefabs live in `Assets/Prefabs/S0_Greybox/` (materials in `Assets/Materials/S0_Greybox/`)
8. In Unity menu: **GameDev → S0 → Create Lower Veil Greybox Scene** (or **Rebuild Greybox In Open Scene**)
9. Press Play — WASD/Arrows move on grid, **E** interacts (board, bartender, POI shells, env stubs)
10. Continue Milestone S0 polish from [11.2](../11.2%20Milestone%20S0%20Greybox.md)

If Unity asks to upgrade ProjectSettings on first open, accept and commit once stable.

## Rules

- Custom grid TB owned by this project
- No marketplace tactics/combat kit as architecture
- S0 before POI timers / full Confront
