# Shell Non-Overlap Rule — HARD LOCK

Status: **Mandatory**  
Authority: [15 Procedural Building Generation](../../../15%20Procedural%20Building%20Generation.md), this file  
Code: `ShellOverlapGuard`, `ShellGrammarAssembler`, `ShellMassingBuilder`

---

## Law (non-negotiable)

> **No two shell pieces may occupy the same space.**  
> Every piece’s axis-aligned bounds must end where another piece’s owned space begins.  
> Overlap / nesting / “tiles stuck inside tiles” is a **generator bug**, not a style choice.

If a generated shell fails `ShellOverlapGuard.Validate`, it is **illegal** and must not be shown as success.

---

## Assembly order (floor-stack)

Build **bottom-up by storey**. **Exactly one rectangle per storey** (no side-by-side annex slabs on the same floor).

1. Foundation under the ground footprint once.
2. For `floor = 0 .. N-1`:
   - Place walls / openings / corners for that storey’s single footprint.
   - Piece ids are prefixed `F{floor}_` so Hierarchy proves floor-stack order.
   - Door / shop only on **floor 0** street front.
   - Step bays (outward sample covered on a lower floor) → wall + **AtticWindow**.
   - After the storey: roof **vacated** regions (`footprint[f] − footprint[f+1]`); on the top storey roof the remaining body.
3. Chimneys **flush** on the primary top roof (chimney bottom = roof top).

Console proof: `[ShellFloorStack] massing=… | F0=WxD@… | F1=…`

---

## Definitions

| Term | Meaning |
|------|---------|
| **Floor slab** | One rectangle on one storey. Same-floor slabs are an XZ partition (touch OK, overlap forbidden). |
| **Piece** | One placed module (`ShellPieceInstance`). |
| **Owner slab** | Slab id that emitted the piece. Piece XZ ⊆ that slab’s footprint. |
| **Bay cell** | One face × floor × bay index on a slab. |
| **Party bay** | Bay whose outward sample lies inside another slab on the same floor. |
| **Step / attic bay** | Bay that is not party, but whose outward sample was covered on a lower floor. |

---

## Geometric rules

### V1 — Floor partition

1. Same-floor slabs **must not overlap** in XZ. Touching on a shared plane is allowed.
2. No nested slabs. No “link wedged between towers”.

### V2 — Inward placement

1. Walls sit **inside** their slab: centre inset by `WallThickness/2`.
2. Face runs leave corner columns to `Corner` pieces (or shortened walls) so wall∩wall at corners does not overlap interiors.
3. Roofs inset by at least `WallThickness` from slab edges.
4. **Forbidden:** roof/foundation overshoot into a neighbor slab.

### V3 — Party / step

1. Party bay → emit nothing (no double wall, no double window).
2. Step bay → wall + `AtticWindow` only (readable as “above the lower roof”).
3. Never emit both sides of a shared plane on the same floor.

### V4 — Bay cells

1. Bay intervals tile the usable face length without overlap.
2. At most **one wall** and **one opening** per bay cell.
3. Door bay: Door/Shopfront — never also Window.

### V5 — Allowed stacks (only exceptions)

```text
Opening ∩ Wall (same slab, face, floor, bay) — opening is a thin plaque
  proud of the outer facade (greybox visibility); along-face stays inside the bay.
  Opening ∈ {Window, Door, Shopfront, AtticWindow}
Corner may touch Wall (same slab)
Trim may touch Wall/Corner (same slab)
Foundation may touch same-slab structure
Chimney may intersect its owning roof (must sit flush: chimney.bottom ≈ roof.top)
Stoop may kiss Door / Shopfront (entry dress)
```

### V6 — Validation

After assembly: `ShellOverlapGuard.Validate(shell, stack)` must pass.

---

## What we will NOT do

- Full-height side volumes each drawing their own party wall stack
- Standard windows on step faces above a lower roof
- Floating chimneys
- Roof `width + padding` overflows

---

## Agent / human checklist

- [ ] Floor-stack assembly (bottom-up)
- [ ] Party bays empty; step bays attic-only
- [ ] Chimneys flush on primary roof
- [ ] `ShellOverlapGuard` green
- [ ] Visual check Elite L / Stepped / RearWing
