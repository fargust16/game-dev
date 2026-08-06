# 99 Open Questions

Version: 0.16

Status: Draft

Last Updated: 2026-08-05

Depends On:
All Canonical Documents

Referenced By:
Future Design, [02 World Laws](02%20World%20Laws.md), [03 Decision Engine](03%20Decision%20Engine.md), [03.1 Decision Node Template](03.1%20Decision%20Node%20Template.md), [04 Points of Interest](04%20Points%20of%20Interest.md), [05 Progression](05%20Progression.md), [06 Approaches](06%20Approaches.md), [07 Character Life](07%20Character%20Life.md), [08 Confront Constraints](08%20Confront%20Constraints.md), [09 Information Sources](09%20Information%20Sources.md), [10 Turn Action Economy](10%20Turn%20Action%20Economy.md), [11 Vertical Slice](11%20Vertical%20Slice.md), [11.1 Vertical Slice POIs](11.1%20Vertical%20Slice%20POIs.md), [AGENTS.md](AGENTS.md)

---

# Resolved Decisions

These were answered in design discussion and written into Draft documents. They remain here so agents do not reopen them casually.

## World Laws

**What changes Balance?**  
Faction Influence, raised mainly by completed Main Goals and shifted by Side Events / Allegiance choices. Documented in [02 World Laws](02%20World%20Laws.md).

**How does the World Engine choose the next faction operation?**  
From World Seed starting state plus each faction's current Main Goal, requirements, and eligible Operations. Documented in [02 World Laws](02%20World%20Laws.md).

**How does the player read Influence without a meter?**  
Through everyday Symptoms: trade, services, encounters, rumours, otherworldly pressure, and related world changes. Exact stage-to-symptom tables remain open below.

**Starting faction?**  
Only the first character in a new world begins with a Starting Faction. Later characters begin Factionless and must earn Faction Entry. Documented in [07 Character Life](07%20Character%20Life.md).

**Difficulty direction?**  
Easy favours the player's faction; Normal slightly favours opponents; Hard and Hell strongly favour opponents. Exact numbers remain open below.

## Decision Engine

**Are the five axes accepted?**  
Yes: Attention, Approach, Allegiance, Commitment, Bargain.

**Which axis does V1 teach first?**  
Attention first, Approach immediately with it; Bargain as the second major lesson. Documented in [03 Decision Engine](03%20Decision%20Engine.md).

**Do Side Events give free bonuses?**  
No. They are Allegiance forks with useful different outcomes, hidden Influence residue, and possible path open/close effects.

**What is Knowledge for?**  
Player Knowledge unlocks reasoning, important places, methods, and prediction. It is not primarily a success-rate bonus. Character pathway power is Progression design constrained by Price of Power and Bargain.

**External powers / churches / curses?**  
Always Bargains with readable, often delayed escalating prices. Detailed systems deferred to content lists; rules documented in [05 Progression](05%20Progression.md).

**Two-axis rule for V1 nodes?**  
Important Decision Nodes require at least two axes. Rare one-axis moments are allowed only as non-major onboarding, not as main forks. Documented in [03.1 Decision Node Template](03.1%20Decision%20Node%20Template.md).

**How are Side Events delivered in the world?**  
As timed Points of Interest with lazy activation, readable Active Windows, and Influence-weighted unattended resolution if missed. Documented in [04 Points of Interest](04%20Points%20of%20Interest.md).

## Progression

**How many Pathways in V1?**  
Three, distinct by way of acting, expandable later. Documented in [05 Progression](05%20Progression.md).

**Which stages in V1?**  
Only 9 → 8 → 7. Deeper stages later.

**How is a Pathway obtained?**  
Starting Faction soft-nudges toward one Pathway; Pathway may also be gained through Bargain, quest, or discovery. Not a prison. Pathless remains valid.

**What belongs in the first Progression draft?**  
Pathway rules plus churches/entities as External Power Bargains. Lore names, full lists, and stages below 7 come later.

## Approaches

**What methods exist in V1?**  
Six Approaches: Confront, Convince, Threaten, Exchange, Veil, Withdraw. Documented in [06 Approaches](06%20Approaches.md).

**Is combat the default best method?**  
No. Confront is one Approach among several and must not have the universal best payoff.

## Character Life

**When does the next character appear after death?**  
After a fixed short Death Gap during which the world resolves advanced state. Documented in [07 Character Life](07%20Character%20Life.md).

**Can Legacy be prepared intentionally?**  
Yes, via a simple V1 Legacy Cache with a next-life map marker. Ordinary leftovers remain unguaranteed.

**What connects lives besides Knowledge and Legacy?**  
Soft Memory: rumours, dialogue hooks, and opinion shifts. No bloodline power inheritance.

## Confront

**May combat become the main progression engine?**  
No. Confront constraints forbid XP-from-kills, abstract loot tables, and universal best payoff. Documented in [08 Confront Constraints](08%20Confront%20Constraints.md).

**What is V1 Confront presentation?**  
In-location Turn-Based Mode, with reference feel from Baldur's Gate 3 / Fallout 1–2. Turn-Based Mode is also available outside combat for deliberate control. Documented in [08 Confront Constraints](08%20Confront%20Constraints.md).

**How does the player learn about POIs and world pressure?**  
Through reusable Information Sources with bias and partial truth: boards, newspapers, bartenders, rumours, faction contacts, witnesses, physical traces, Soft Memory. Documented in [09 Information Sources](09%20Information%20Sources.md).

**What is the V1 turn structure?**  
Move + Action + optional short Bonus. Same economy in Confront and non-combat Turn-Based Mode. Documented in [10 Turn Action Economy](10%20Turn%20Action%20Economy.md).

**What is the spatial model?**  
Square grid from S0; levels and props are grid-native; Environment Actions cover vault/climb/window/etc. Documented in [10.1 Grid World and Environment Actions](10.1%20Grid%20World%20and%20Environment%20Actions.md).

**Which engine?**  
Unity locked. URP, C#, project folder `unity/`, custom grid TB. Documented in [12 Engine Choice](12%20Engine%20Choice.md).

**What is the first playable proof?**  
A Vertical Slice in Lower Veil District with 2 selectable factions (Wardens / Veinmarket), 3 POIs, Information Sources, Turn-Based Mode, one Bargain tease, and Lord of the Mysteries-like industrial-era mood. Death Gap is out of slice scope. Placeholder names may change. Documented in [11 Vertical Slice](11%20Vertical%20Slice.md).

---

# High Priority

## World Laws

Can Balance partially recover without player intervention?

What exact Symptoms correspond to each band of Faction Influence?

What determines the concrete winning / terminal condition of each faction?

Can multiple factions unintentionally cooperate?

What are the V1 numeric ranges for Difficulty Pressure?

## Decision Engine

When and how do Hidden consequences become discoverable across one Character Life versus across Legacy?

What is the V1 readable vocabulary for communicating Opportunity Cost without UI overload?

## Points of Interest

What exact in-world durations belong to Brief / Short / Standard / Long / Persistent in V1?

How exactly does the UI/map express urgency without becoming a stressful quest-timer game?

How many active Marked POIs should a V1 district show at once?

## Information Sources

How many tips should a V1 district generate per day from boards / bartenders / rumours?

When does a tip upgrade from Hinted to Marked by rule?

How much false or slanted information is healthy in V1 before trust collapses?

Can a Persistent POI still be finite without feeling like a fake timer?

## Progression

What are the exact V1 Conduct Progress thresholds for stages 9 → 8 and 8 → 7?

How strongly may a Starting Faction nudge before it becomes a soft lock?

How do External Power Bargains interact with Pathway conduct when they pull in opposite directions?

What happens if a character tries to abandon or switch Pathways after stage 9?

## Approaches

How much V1 support does each Approach need before it feels real (skills, UI verbs, simple checks)?

When does Threaten escalate into Confront by rule rather than by writer taste?

Should Bound Path get a light Rite/Invoke extension in V1, or remain under Exchange until V2?

## Confront / Turn Economy

How does the player enter and exit Turn-Based Mode outside combat without making exploration tedious?  
**Partial lock:** Free Exploration is smooth continuous move (+ click-to-move); Turn-Based is discrete cells + Move/Action/Bonus. Manual toggle exists in S0 (`T`). Remaining: diegetic UX (when Confront auto-locks, prompts, fatigue of manual TB).

How often may important nodes offer Confront as one option versus scenes that begin already in Confront?

Exact initiative formula and Move Speed units / cells per Move for V1?

Diagonal move cost: 1 or 1.5?

Exact fail chance bands for vault/climb/window in V1?

Is Bonus-to-Dash enabled in V1 or deferred?

## Vertical Slice

What exact prop/language checklist best communicates early radio/steam novelty without turning the slice into a tech museum?

How visible should crown/nobility pressure be inside Lower Veil without adding a third faction?

## Character

What exact V1 length is the Death Gap in days?

How large may a Legacy Cache be before it undermines death?

What is the minimal Faction Entry pattern reusable by all V1 factions?

How many Soft Memory dialogue hooks are required in V1 versus optional flavour?

---

# Medium Priority

## Magic

Where does mystical power originate?

Can power disappear?

Can the world lose magic?

## Factions

How do factions emerge?

Can factions disappear naturally?

Can civilizations recover?

## Economy

How exactly do Influence-controlled niches set prices and availability?

How do resources disappear?

## Exploration

How are secrets generated?

Can secrets vanish forever?

---

# Low Priority

Achievements

Exact difficulty number tuning beyond the directional rule

Localization

Accessibility

Mod support

Pathway lore names and full church/entity catalogues

Stages 6 → 0 content

---

# Rule

Whenever a future document depends on one of these questions,
that question must be resolved before continuing.

No design decision should rely on undefined concepts.

Resolved Decisions may be revisited only through the Canonical / Draft change policy, with explicit reason and document updates.
