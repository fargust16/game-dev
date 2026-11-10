# Vertical Slice — People (content piece C1)

Status: Draft — proposed by the agent on 2026-11-06 and **accepted by the owner on 2026-11-07**: all fourteen, with the agent's placeholder names, which stay mutable.
Created: 2026-11-06
Last Updated: 2026-11-10 (names since C4: Bell Street is the Black Puddle, Loom Lane Draught Lane, the Market square Bawling Square; the records keep their day's names (city plan §28.10). Earlier: S2.7: Nell's lie and her day-2 lines by outcome, authoring defaults. Earlier: S2.4: Nell and Kit are talked to; Nell's Approaches, their Arguments and the Exchange, authoring defaults. Earlier: S2.0: the names, roles and lines are in the string tables, in English and Russian. Earlier: accepted by the owner: the roster and the placeholder names. Earlier: first draft for the owner)
Authority: [11 Vertical Slice](../../../11%20Vertical%20Slice.md), [11.1 Vertical Slice POIs](../../../11.1%20Vertical%20Slice%20POIs.md), [09 Information Sources](../../../09%20Information%20Sources.md), [VERTICAL_SLICE_IMPLEMENTATION_PLAN.md](VERTICAL_SLICE_IMPLEMENTATION_PLAN.md) §5
Related: [SLICE_ASSET_MAP.md](SLICE_ASSET_MAP.md) §1 (colours), [VEILMOUTH_CITY_PLAN.md](VEILMOUTH_CITY_PLAN.md) §21 (places)

Content piece C1 of the implementation plan's §5, not the city plan C1. S1 needs it: the named people placed in the city (A.1), and the tips of the Notice Board, the bartender, and Street Rumour (Q.2).

Since S2.0 (17 X.7) the game reads every name, role and line below from the string tables, in English and Russian: `Resources/Localization/people.csv` (`person.<id>.name`, `.role`) and `poi.csv` (`tip.<id>.line`, `.note`). This document keeps the roster and the English placeholders; a name or a line is changed in the table, and here when its English changes.

**Names since C4** ([VEILMOUTH_CITY_PLAN.md](VEILMOUTH_CITY_PLAN.md) §28.10, 2026-11-10): Bell Street is the Black Puddle, Loom Lane is Draught Lane, and the Market square is Bawling Square. The records below keep the names and places of the city they were built in.

---

## 1. Who the slice already needs

Every person below is required by a document above; none is new content. The plan asks for 6–10 named people besides the Masters: this draft has 10 and four Masters. The missing person is absent and counts as nobody.

| # | Role | Placeholder name | Colour | Place (C2) | Why the slice needs them |
|---|---|---|---|---|---|
| 1 | Publican of the Soot & Bell | Nell Garrow | yellow until S5c | Behind the bar, Soot & Bell | The bartender source (09 §3; 11.1) |
| 2 | Apothecary, teaches Alchemy | Josiah Harrow | green | Harrow & Tincture, Bell Street | POI A; Основа ритуала's first trace; the slice's potion shop |
| 3 | Harrow's apprentice | Pip Vance | green | The apothecary's back room | Opens the shop if Harrow is lost, teaching at double price (11) |
| 4 | Master of Item Creation | Edda Brann | green | Her stall and yard, Market square | Lessons, Skinning, tools (11, 19 §6.4) |
| 5 | Master of Enchanting | Silas Quill | green | His stall, Market square | Magical ingredients (11) |
| 6 | Master of First Aid | Ada Merrow | green | Her stall, Market square | Lessons, bandages, splints, antidotes (11) |
| 7 | The trader | Tobias Penn | orange | His stall, Market square | Ordinary goods (11, 19 §6.1) |
| 8 | The pawnbroker | Ruth Kettle | orange | Pawn Corner, Loom Lane | Посланник's token in pawn (11) |
| 9 | The seer | Madame Ilse | orange | Her stall, Market square | Predicts the invasion's hour for a price (11, 19 §6.3) |
| 10 | Warden sergeant, brass number 14 | Sergeant Coyle | blue | The Wardens' station, Ridge | POI A's arrest; the lock-up; the Arrest (10.3 §6.2) |
| 11 | The faction's recruiter | Mr Ashgrave | teal | The faction's house, Ridge Street | Offers the first-day trial (11) |
| 12 | The missing person's sister | Lottie Wren | yellow | The Soot & Bell in the evening, her tenement by day | Reports the disappearance, so the trial has a door in; can be Сомневающийся's kin at the tavern (11) |
| 13 | Veinmarket's contact | Dace | brown | A table in the Soot & Bell; Loom Lane at night | Sells the senior's paper (Грязная тайна); POI A and B's other side |
| 14 | Newsboy, carries Street Rumour | Kit Mallow | yellow | The Market square | Street Rumour's two or three lines a day, moved there by the owner (11.1) |
| — | The missing person | Thomas Wren, mill clerk | — | His room in the tenement, Loom Lane | Left to join the General; his cache holds the Император formulas (11). Absent |

Masters keep 08:00–20:00 (11); other schedules come in S5c, and until then each stands at the place above.

## 2. What each source says in S1

The lines are 11.1's, sorted by source; S1 needs nothing more. Times are 11's authoring defaults.

| Source | Day 1 | Night 1 | Day 2 |
|---|---|---|---|
| Notice Board (renewed 08:00) | Morning: "Missing — mill girls, near Loom Lane", greased by soot thumbs (POI B, Hints); "Missing — Thomas Wren, clerk", posted by his sister (the trial's door); one task for money. 14:00, pinned: "Warden seizure notice — Harrow & Tincture, before second-shift bell" (POI A, Marks) | — | Morning: a new task; the Wren notice stays |
| Nell Garrow, one tip each evening, more for Convince, Threaten, or Exchange | From 16:00: "Brass-numbers are taking old Harrow tonight…" (POI A, Marks); asked again with an Approach: "After the eleventh bell they take mill girls off Loom Lane. Tonight again." (POI B, Marks) | — | "Lane's wrong. Coal stairs got fresh chalk." (POI C, Marks) |
| Kit Mallow, two or three lines a day, one a lead | "They want the drawers more than the man." (POI A, Hints); "Collectors in coach-coats. Not proper cabmen." (POI B, Hints) | — | "Wax seal melted funny on Harrow's door" or "glove by the coach ruts", by what happened (POI C, Hints) |

Since S2.4 Nell and Kit are talked to in the dialogue column (`NellDialogue` and `KitDialogue` in `Assets/Data/Slice/Dialogue/`, their words in the `dialogue` table): each says the lines above on arriving, and the line for nothing new when there are none. Nell's Loom Lane tip, authoring defaults of 2026-11-09 the owner may change:

| Approach | What it is | Argument (any one; each further +1) |
|---|---|---|
| Convince | Persuasion, a Small request: the girls vanish, say who takes them | The board's missing girls, the Wren notice, Kit's coach-coats |
| Threaten | Intimidation, a Small request: she warned of the Wardens' raid, so they could hear who warns their marks | Her own Harrow tip, the seizure notice |
| Exchange | A service, no check: an hour's work behind the bar, the world running on by 60 minutes | — |
| Convince, a lie (S2.7) | Deception, an Ordinary request: Lottie Wren sent the character, because one of the girls is her cousin. Found out at midnight 20 % of the time; the next morning Nell is cold and, asked why, says she knows, once | The Wren notice, the board's missing girls |

Her Listener Value is 1 for every check (Strength, Intelligence and Will 10), Kit's 0. A failed check leaves the other ways; Threaten costs a step of her Attitude either way (10.4 §5).

Since S2.7 Nell also says, from 08:00 on day 2, what became of Harrow's shop (open, sealed or emptied) and of the girls (freed, taken, or the snatch spoiled), one line for each outcome of POI A and B: the tips `nell.harrow_*` and `nell.lane_*`, authoring defaults.

At most one rumour a day may be false, never about a Ritual Weak Point (09). The traces of the run's Weak Points are placed by the seed among these people (11 *Finding the traces*) and come with S6.

## 3. The owner's answers

Owner, 2026-11-07: all fourteen; the agent's placeholder names, which the owner may change at any time. Schedules, what each person knows beyond §2, and the traces they carry come with their milestones (S5c, S2, S6).
