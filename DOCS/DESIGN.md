# Slide Jam — Design & Implementation Spec

**Origin:** self-proposed hybrid-casual mobile puzzle. The rules were designed in a throwaway HTML prototype (three levels, three boosters, a fruit-garden skin) that is **not part of this repository**; this repository is the Unity implementation
**Engine:** Unity **2022.3.62f3** (LTS), 2D, built-in render pipeline, classic Input Manager · one finger: drag only · single screen
**Platform:** target iOS and Android, portrait only (the project's default orientation is Portrait). The Editor uses the mouse through the same pointer path as touch
**Working title:** Slide Jam (change freely; nothing in the rules depends on it) · **Theme:** "Vườn trái cây" (fruit garden), display only, §9
**Version:** 0.8 (2026-11-05) — v0.8 records the Phase 2 decisions before they are built: the order in which a round's units are routed (§5.4), the end of a run as a nullable `Ended` field (§6.1), the extended `LevelData`, the minimal `GameConfig`, `qGrow` in Phase 2 and the stage layout (§4, §6, §7). v0.7 adds the pooled `Block` prefab to the proposed layout (§6.2). v0.6 marks the Phase 1 view (board, drag, phase player, move counter, restart) as built in the Unity project (§4, §7). v0.5 records the decisions made while writing the first engine code: the colour mapping and the separate grid stream (I4 detail), the slide direction (§5.2), the empty cell, `MoveResult`, the reduced phase kinds and the `FallStart` list (§6) and the stateless stream generator (§7). v0.4 lets each target hold 3 to 5 units and lets every level choose its number of targets (§5.1), drops the level-data, balance and measurement sections, turns the hints into a tutorial (§5.8), makes a used booster non-refundable (§5.7, §5.9) and changes the fruit set (§9). v0.3 added the third reference level, the three boosters, the theme and the vault in the resolution order. v0.2 (2026-11-02) wrote the rules from the prototypes and rewrote interfaces and implementation notes for Unity. The engine code of Phase 0 and Phase 1 (random, board, slide, match, settle, `Move`) is written in `Assets/Scripts/Engine/` with 38 Edit Mode tests (run by the user, all passing); the Phase 1 view (`Assets/Scripts/Game/`) runs in Play mode without errors (reported by the user); everything else on the Unity side is **[PROPOSED]**.

---

## 0. How to use this document

This doc is context for both humans and AI agents. If you are an agent reading this:

- **§2 (Invariants) is law.** Do not violate those rules to make a task easier. If a request conflicts with an invariant, say so instead of silently working around it.
- **§5 (Rules & Tuning) is the single source of truth for numbers and behaviour.** Every tuning value lives in a data asset (`LevelConfig` or `GameConfig` ScriptableObjects), never in view or input code. The *Where* column of each table says which one holds a number. If you need a number that isn't in §5, add it to the right asset and add it to §5 — don't invent a literal.
- **§8 (Glossary)** defines project terminology. Use these exact names for classes, files and variables.
- Anything marked **[LOCKED]** must not be redesigned.
- **[BUILT]** marks a rule that was implemented and tested in the earlier prototypes (a settled rule, not code that exists here). **[PROPOSED]** means it is a plan only; treat it as a suggestion, not as fact.

---

## 1. Pitch

A grid of coloured blocks. You **slide one row or one column** and the blocks wrap around the edge. Any three in a line **vanish on their own** and fly up into the **target slots**, which want a certain colour. What no slot wants goes into a small **queue**. If the queue overflows, you lose.

You never choose what to eat. You choose **what to line up, and when**: a match of the wrong colour is a cost, a match of the right colour is progress. Every level adds obstacles (chains, seals, locked slots) that change how you plan the next slide. Three optional boosters (§5.9) help when the queue fills up.

**Genre:** single-screen turn-based puzzle · **Level length:** about 20–40 slides (design target) · **Session:** instant restart, unlimited undo in the first build

**Why it works [design thesis].** The slide is a physical, readable action. The queue gives a visible, shrinking budget for mistakes. The skill is defensive: avoid the wrong match, not chase every match.

---

## 2. Invariants [LOCKED]

These are the load-bearing rules. Breaking any one of them breaks the game.

| # | Rule | Why |
|---|---|---|
| I1 | A level is **won** when every target in its sequence (`SEQ`) is completed. It is **lost** the moment the queue holds more units than `qOpen` after a resolution step (§5.4) | The one win and the one loss. No timers, no move limit |
| I2 | **Turn-based.** Nothing changes between inputs: no timers, no real-time pressure, no random events outside a move | The skill is planning. It also makes undo exact |
| I3 | **One core verb: slide a row or a column.** A drag of any length is **one move**. A drag whose net shift is zero is not a move and costs nothing. **Boosters** (§5.9) are optional consumable actions outside the move counter: they never advance a column stream, never put units in the queue, and each runs as one atomic resolution (I8) | Keeps the game learnable in seconds and keeps the move counter honest; boosters stay a relief valve and do not become a second hidden ruleset |
| I4 | **Deterministic.** The same level seed and the same list of moves always give the same result, including every refill colour. No hidden randomness during play | Solver and bot checks, replays, regression tests, seeded daily levels |
| I5 | **No lies.** The next-spawn dots and the drag preview show what will really happen. Nothing the player has already seen is secretly recoloured | The player must be able to blame a loss on their own plan |
| I6 | **Combos never punish.** A match caused by a cascade (resolution round 2 or later) can only help: units no slot accepts **vanish**. Only a match caused directly by the player's slide can put units into the queue (§5.4) | Combos are hard to foresee and long ones are random. Losing to one reads as unfair |
| I7 | **Locks close a door, never end the run.** Every lock has an exit through normal play: a chain is removed by any match that touches it, a seal by clearing the cells around it, a locked slot by completing its neighbour, locked queue chairs by completing targets (§5.6) | The player must always be able to route around a block |
| I8 | **Resolution is atomic.** After a slide or a booster use, all phases (§5.4) finish before the next input is accepted. Win and loss are checked at the fixed points in §5.4 and nowhere else | Overlapping animations and re-entrant input are the usual source of unrepeatable bugs |
| I9 | **The board is always full after settling.** No empty cell survives a settle, whatever the obstacles (§5.5). The only exception is a terminal state (a loss is reported before the final settle) | Holes under chains would make cells unreachable |
| I10 | All tuning values live in data, never in view or input code | The last hours of a project are for tuning, not refactoring |
| I11 | **Every shipped level is solvable** | A level nobody can win is a bug, not a difficulty setting. **[PROPOSED]**: the way solvability is checked (a bot or a solver run on the Unity engine) is chosen when the engine exists |

> **I4 detail — boosters.** A booster contains no random choice: every tie is broken by a fixed order (§5.9), so a replay of the same moves and booster uses gives the same result.
>
> **I4 detail — streams.** Each column owns an independent stream of colours, built from the level seed (`SEED + (column + 1) × 7919`). A refill takes the next colour from the stream of the column it spawns in, and a stream is only ever advanced by a spawn. So the colour of the next two spawns in a column is always known, and the dots above the grid show exactly those.
>
> **I4 detail — colour mapping.** A stream value is a `uint` from `mulberry32` (`Mulberry32.At(seed, index)`, a closed form: the value after `index + 1` steps, so no state or cache is needed). The colour is `(value × colors) >> 32` in 64-bit integer arithmetic, a number in `0..colors-1`. `ColumnStream.At(seed, column, index, colors)` is the colour of the column's `index`-th spawn. This mapping has not been compared with the original prototype, so a seed may not give the prototype's level.
>
> **I4 detail — initial grid.** The starting grid is rolled from a separate stream, "column −1" (`ColumnStream.At(seed, -1, k, colors)`, whose seed is the level seed itself), so it never moves a real column stream. Cells are rolled row by row, left to right; a cell whose colour would complete a run of 3 to its left or above is re-rolled with the next value of that stream. Block ids start at 1 in the same order.

> **I6 detail — what counts as a cascade.** Resolution runs in numbered rounds. Round 1 is the matches present right after the player's slide. Every later round (matches created by the settle, a seal opening or a chain breaking) is a cascade round. Units from a cascade round that fit a slot go to the slot as usual; units that do not fit disappear with a "vanish ×N" label ("Chim nhặt ×N" in the fruit theme, §9). The side effects of a booster (the settle that follows a hammer cut, for example) count as cascade rounds too.

---

## 3. Core loop

```
        ┌──────────────────────────────────────┐
        │  Look: slots, queue room, next dots  │
        └───────────────────┬──────────────────┘
                            │
        ┌───────────────────▼──────────────────┐
        │  SLIDE one row or column (wraps)     │
        └───────────────────┬──────────────────┘
                            │
        ┌───────────────────▼──────────────────┐
        │  3+ in a line vanish → units         │
        │  slot accepts → fills a pip          │
        │  no slot accepts → queue (round 1)   │
        │                    vanish (cascade)  │
        └───────────────────┬──────────────────┘
                            │
        ┌───────────────────▼──────────────────┐
        │  Slot full → next target appears,    │
        │  queue units move into it,           │
        │  locks open                          │
        └───────────────────┬──────────────────┘
                            │
        ┌───────────────────▼──────────────────┐
        │  Settle: blocks fall / flow, refill  │
        │  from the column streams             │
        └──────────────────────────────────────┘
```

**The difficulty target.** A level should be hard enough that careless play often loses and easy enough that a thoughtful player wins. Tune with the queue size first, the target sequence (count and `cap` of the targets) second and the obstacle count last.

---

## 4. Board objects and HUD

The screen is one portrait layout on a reference width of **360 logical units**, top to bottom: target slots, next strip, queue and vault, next-spawn dots, grid, booster bar, undo and restart. Cell size is 54 for a 6×6 grid and 48 for 7×7 (§5.1). The layout is a view decision; the engine does not depend on it. **Built:** the board is drawn with sprites under an orthographic camera fitted to the grid width (`CameraFit`, margin 0.5 cell), and the HUD is a uGUI canvas with a Canvas Scaler at 360 × 640 matching width. **[PROPOSED, Phase 2]** The world-space part of the layout is a **Stage**: one parent object whose children are anchor roots (`Slots`, `Strip`, `Queue`, `Grid`) placed at fixed offsets in cell units, so the camera can fit the Stage's whole bounds (width first, then height) instead of the grid alone. The slot, strip and queue views are drawn under their anchors; the HUD and the buttons stay in the uGUI canvas.

| Object | Role |
|---|---|
| **Target slots** ×3 | The slots currently asking for blocks. Each shows its colour, a count `n/cap` and `cap` pips (3 to 5, set per target). Filling the last pip completes it (§5.4) |
| **Next strip** | The targets still to come, so the player can plan ahead. A target that already holds banked units (after a swap, §5.9) shows the number |
| **Queue** | A row of `queueMax` chairs (5 or 6). A level with `qGrow` opens only the first `qOpen` (3 at the start); the rest show a padlock. Shows `n/qOpen` |
| **Vault** | Next to the queue. Shows the units stored by the Stash booster (§5.9) in **two separate rows of five**, filled left to right and then top to bottom; the label carries the full count, and a unit past the tenth is counted but not drawn |
| **Next-spawn dots** | Two small blocks above each column: the next two colours that column will spawn (I5) |
| **Grid** | 6×6 (Levels 1 and 2) or 7×7 (Level 3), per `LevelConfig`. Edge markers (red bars) show which rows and columns are locked |
| **HUD** | Slide count and targets left (built so far: the slide count and a Restart button, with placeholder English text) |
| **Booster bar** | Three buttons (§5.9). Each shows what it will do, or why it is off |
| **Undo / Restart** | Two buttons under the grid. Both are disabled while a resolution is playing |
| **Tutorial** | A short overlay with a highlight, shown at the start of the game and the first time a mechanic or booster appears (§5.8) |
| **Overlay** | Win / loss card with Undo (loss only) and Restart |

### 4.1 Input

Drag only. Press on a cell of the grid, move, release.

- **Axis lock.** Once the pointer has moved more than **8 logical units** from the press point, the axis is chosen: row if `|dx| ≥ |dy|`, otherwise column. The line is the row or column of the pressed cell. The axis never changes during the drag.
- **Notches.** `notches = round(offset / cellSize)`. The line follows the finger continuously with wrap-around (a block leaving one edge appears on the other, clipped to the grid). On release it settles to the nearest whole notch in 130 ms. A negative notch count slides the other way.
- **A locked line** does not move: the drag is dead, the line flashes red for 0.5 s (the tutorial explains chains the first time, §5.8).
- **Spring.** If the line contains a spring block, one notch moves 2 cells and the line follows the finger at the same ratio (the tutorial explains it the first time, §5.8). With an odd line length the shift is never zero unless the player slides a whole revolution.
- **A drag that ends on notch 0** (or a multiple of the line length) snaps back in 140 ms and counts as nothing (I3).
- Input is ignored while a resolution is playing, while a panel is open and after the run has ended.
- **Built (Phase 1):** `DragInput` reads the classic Input Manager (a touch works through the simulated mouse), ignores a press over uGUI and while the view is busy, locks the axis after 8 logical units and rounds to the nearest notch on release. A refused move (zero shift) snaps the line back in 140 ms. Not built yet: the locked-line flash and the spring step (Phase 4).

### 4.2 Drag preview [BUILT in the prototypes]

While dragging, the game runs the real move on a copy of the state (`preview`) and shows its first matches:

- **White outline** (pulsing) on every cell of the first clear round: the units go to targets.
- **Orange outline** instead, when the whole resolution will add units to the queue. Dashed orange boxes appear on the queue chairs they will take, and the queue header reads "+N to the queue" (display strings follow the theme, §9).
- **Red text** ("will overflow → lose") when the whole resolution would end the run by overflow.

The number in the header is the **net change of the queue after the whole chain**, not just the first round, so a move that overflows and is then rescued by a completion is not reported as a loss. The preview never shows a cascade's cells, only the first round's, so the drag shows "what I am about to cause", and the label shows "what that will cost in total" (I5).

---

## 5. Rules & Tuning [SOURCE OF TRUTH]

> *Where* names the asset that holds a value: `LevelConfig` (one per level) or `GameConfig` (shared). Tests may build a config in code. Numbers are tuning values and are expected to be adjusted. Levels 1 to 3 below are the three reference levels from the prototype; they are not yet Unity assets.

### 5.1 Global

| Key | Level 1 | Level 2 | Level 3 | Where |
|---|---|---|---|---|
| `rows` × `cols` | 6 × 6 | 6 × 6 | 7 × 7 | `LevelConfig` |
| `colors` (`NCOL`) | 5 | 5 | 5 | `LevelConfig` |
| `cap` (units a target needs) | **3 to 5**, per target (3 in all three reference levels) | same | same | per target in `LevelConfig`; limits `capMin` 3 and `capMax` 5 in `GameConfig` |
| `activeSlots` | 3 | 3 | 3 | `LevelConfig` |
| `sequence` (`SEQ`): list of targets `{color, cap}` | any length; reference levels: 12 targets, colours 0 1 2 3 4 1 0 3 2 4 0 1, all `cap` 3 | same | same | `LevelConfig` |
| `lockedSlotIndex` | none | slot 2 | slot 2 | `LevelConfig` |
| `queueMax` | 5 | 5 | 6 | `LevelConfig` |
| `qOpenStart` (`qOpen`) | 5 (all open) | 3 | 3 | `LevelConfig` |
| `qGrow` (`qOpen` +1 per completed target, up to `queueMax`) | off | on | on | `LevelConfig` |
| `sealNeed` | none | none | 2 | `LevelConfig` |
| `swapMin` (Swap target needs this many same-colour units in the queue, §5.9) | 2 | 2 | 2 | `GameConfig` |
| `seed` | 20261031 | 20261102 | 777123 | `LevelConfig` |
| Cell size · reference width | 54 · 360 | 54 · 360 | 48 · 360 | view settings |

Colours are numbered 0–4 and the five items of a theme must also differ in **silhouette**, not only in hue, so the game is playable without colour vision (§9).

**Limits.** `rows`, `cols` and `colors` are at least 3 (a shorter line cannot hold a match and the generator could not avoid one); `LevelData` refuses smaller values.

**Targets.** A level is free to choose how many targets it has (the length of `sequence`) and the `cap` of each one, from `capMin` to `capMax` inclusive; a value outside that range is a data error and fails on load (I10). A target's `cap` stays with it when the strip is reordered (§5.9). A bigger `cap` means more units for the same target, so raise `queueMax` or `qOpenStart` with it only after checking the level.

**Data in the Unity project [PROPOSED, Phase 2].** `LevelData` (engine) gains `ActiveSlots`, `QueueMax`, `QOpenStart`, `QGrow` and `Seq` (an array of `Target {Color, Cap}`); the engine only requires `Cap ≥ 1`. The 3 to 5 range is a data rule and is checked where a `LevelConfig` is converted to `LevelData` (game assembly), using `capMin` and `capMax` from a minimal `GameConfig` ScriptableObject created in Phase 2 (`swapMin` and the booster settings join it in Phase 5). `qGrow` is built in Phase 2; the locked slot, dual, ×2, spring, seal and chain wait for Phase 4.

### 5.2 Slide

A **line** is one whole row or one whole column. Sliding by `k` cells moves every block `k` places along the line, and the ones that pass the end re-enter at the start.

```
k = (notches × step) mod length      step = 2 if the line contains a spring block, else 1
```

- `k = 0` is not a move (I3). Otherwise the slide is applied and the move counter goes up by one.
- **Direction.** A positive `notches` moves a row to the right and a column down; a negative one moves it the other way. `k` uses a floor modulo, so a negative shift equals `length − |k|`.
- A line is **locked** when it contains a chain block whose lock covers that axis (§5.6). A locked line cannot be slid.
- A slide moves whatever is in the line, including chain blocks of another axis, sealed blocks and spring blocks.

### 5.3 Matching

- A **match** is a run of **3 or more** cells of the same colour in one row or one column. Horizontal and vertical runs are found independently; there are no L, T or square shapes (none of the three levels uses them). A cell in two runs counts once.
- A **dual block** counts as both of its colours, so it can complete a run of either colour. A **sealed block** counts as no colour until it opens. A **chained block** counts as its colour for finding matches but is **not consumed** (§5.6).
- All matches on the board are found at once and cleared together in one **round**.

### 5.4 Resolution order [LOCKED]

After a valid slide the engine runs these phases in order, and repeats from `clear` until a round finds no match. Each phase is one entry in the move result (§6.1) so the view can animate it.

| # | Phase | What happens |
|---|---|---|
| 1 | `shift` | The line is slid. The move counter goes up |
| 2 | `clear` (round *n*) | Find matches. **Chains:** every chained block inside a matched cell or next to one (4 neighbours) loses one layer (§5.6). **Seals:** every sealed block next to a consumed cell loses 1 (§5.6). **Units:** every consumed block gives 1 unit (2 for a ×2 block), placed by the rule below. The consumed cells become empty |
| 3 | `complete` (0 or more) | Every slot whose `count` has reached its own `cap` is completed (see below), one at a time |
| 4 | `pull` (0 or 1 after each completion) | Units waiting in the vault (first) and then in the queue move into slots that accept them |
| 5 | overflow check | If the queue holds more than `qOpen` units: **loss** (`QueueOverflow`) and the move ends |
| 6 | win check | If every slot is empty: **win** and the move ends |
| 7 | `fall` | Settle and refill (§5.5) |

**Where a unit goes** (in this order):

1. The first slot, left to right, that is unlocked, has the unit's colour and has fewer units than its own `cap`.
2. If the block is a **dual** block and step 1 failed, the same test with its other colour.
3. Otherwise, in round 1: **the queue** (appended at the end). In round 2 or later: **the unit vanishes** (I6).

A ×2 block gives two separate units; each goes through the three steps on its own, so one can fill a slot while the other vanishes or queues.

**Order of the units inside a round.** Units are routed one at a time, so the order decides which unit takes the last free pip and which one is queued or vanishes. The order is fixed: the consumed cells in the row-major order of `Matcher.Find` (top row first, left to right), and for a ×2 block its two units one after the other. The units of a round are all routed before any completion of that round runs, so a slot that fills during the round stops accepting at its `cap` and the next unit goes to the next rule. Completions run in slot order, left to right, and repeat while some slot is full (see below).

**Completing a slot** (`complete`): the completed count goes up by one; the slot is replaced by the next target of `sequence` (or becomes empty when the sequence is finished); a **locked neighbour slot** (index ± 1) opens; `qOpen` goes up by one (up to `queueMax`). Then `pull` runs: the vault is scanned first, then the queue, each from the first unit to the last, and every unit that a slot now accepts (rule 1 above) flies to that slot, the rest keep their order. A pull can fill another slot, which then completes in turn, and so on until nothing is full.

**Check points.** The overflow check happens after the completions of a round and before the settle. The queue can exceed `qOpen` *inside* a round (units are added before the completions run); only the state after the completions counts. A completion that opens a queue chair and pulls units out can therefore rescue a move.

### 5.5 Settle: falling and sand flow [BUILT in the prototype, Level 3]

There is **one** settler (`SandSettler`). In a level with no chain block it is exactly ordinary gravity (blocks fall straight down, empty cells at the top refill), which is what Levels 1 and 2 do. Sand flow only matters where a chain block exists, as in Level 3.

A settle repeats the following passes until nothing changes (a guard stops it after 500 passes):

1. **Move pass.** Scan rows from the second-to-last row up to row 0 (so lower blocks move first). Inside a row the scan direction alternates from row to row (with gravity only the direction does not change the result, so the engine adds the alternation together with the chain rule). For every block that is **not chained**:
   - if the cell below is empty: the block falls one cell;
   - otherwise, if the cell below holds a **chain block**, or the cell **beside it in the same row** (towards a free diagonal) holds a chain block, **and** the diagonal cell below on that side is empty: the block slides diagonally into it. Which side is tried first depends on `(row + column)`: odd tries left first, even tries right first. If both sides are possible only one is used.
2. **Spawn pass.** Every empty cell in row 0 gets the next colour of its column's stream (I4). The new block starts above the grid: the *n*-th spawn in a column during one settle starts at row −*n* (−1, −2, −3 …). Earlier spawns have moved down by then, so the first spawn ends up lowest, exactly like ordinary gravity.

Chain blocks never move during a settle, and a block that is not next to a chain behaves exactly like ordinary gravity (it only ever moves diagonally when a chain forces it to). A cell under a chain is filled from the side: the neighbouring column's block flows in diagonally, and the neighbouring column refills from the top, so the cell is never left empty (I9).

The move result records, for each block that moved or spawned, its **start cell**, so the view can animate the flow.

### 5.6 Mechanics catalogue

Each mechanic is a property of a block or a slot. **Level 3 uses all of them. Level 2 uses ×2, dual, the locked slot and the growing queue. Level 1 uses none.**

| Mechanic | Rule | How it ends (I7) | Where |
|---|---|---|---|
| **Chain** (`chain: {lock, layers}`) | The block is **immovable by settle** (§5.5) and cannot be consumed. `lock` is `both` (the row **and** the column of the block cannot be slid), `row` (only the row) or `col` (only the column). A `row` or `col` chain block can still be carried along the free axis by a slide | Every **round**, each chained block that is in a matched cell or next to one loses 1 layer; when `layers` reaches 0 the chain is removed and it becomes an ordinary block | `LevelConfig` |
| **Sealed block** (`seal: n`) | Keeps its colour on screen but counts as no colour: it cannot join a match. It can be slid and it falls normally | Each consumed cell next to it (4 neighbours) lowers `seal` by 1; at 0 it opens and counts as its colour from the next round. The number is drawn on the lock | `LevelConfig` |
| **×2 block** (`x2`) | Gives 2 units when consumed | — | `LevelConfig` |
| **Dual block** (`colors: [a, b]`) | Counts as both colours when finding matches; its units can fill either colour (§5.4) | — | `LevelConfig` |
| **Spring block** (`spring`) | A line containing it moves 2 cells per notch (§5.2) | The block can be slid out of the line or consumed | `LevelConfig` |
| **Locked target slot** | A slot that shows a padlock and accepts nothing | Opens when its **neighbour slot** (index ± 1) completes | `LevelConfig` (`lockedSlotIndex`) |
| **Locked queue chairs** | Only `qOpen` of the 6 chairs can hold units; the rest show a padlock | One chair opens with each completed target | `LevelConfig` (`qOpenStart`) |

**Display.** Chains are drawn across the block (two crossing chains for `both`, horizontal for `row`, vertical for `col`) with the layer count in a circle if it is above 1. A sealed block is dimmed with a dashed outline, a padlock and its number. ×2 has a `×2` badge, a spring a coil badge, a dual block is split diagonally with both colour shapes. Locked rows and columns carry red bars at the grid edge.

**Not built, proposed as later content [PROPOSED]:** one-way rows, heavy blocks, a second slot-lock type, bigger grids, moving obstacles. None has been prototyped, so none has measured stats.

### 5.7 Win and loss

| `EndReason` | Meaning | Trigger |
|---|---|---|
| `Win` | Win | Every slot is empty after a completion (§5.4 step 6) |
| `QueueOverflow` | Loss | The queue holds more than `qOpen` units at the overflow check (§5.4 step 5) |

Ending texts are display strings (§9): a win card with the number of slides, a loss card with a one-line reason and an **Undo** button. There is no move limit and no timer (I2).

**Undo.** The first build keeps an unlimited stack of earlier states, one per valid move. Undo restores the state before the last move (including the refill stream positions, so the same move gives the same result, I4). The snapshot contains the vault, `Seq` and `Pre` (§5.9), so undoing a booster action takes back its effect on the board, but **a used booster is never refunded**: the player's booster count is not part of the snapshot, and undo, restart and losing do not give it back. Restart clears the stack. **[PROPOSED]** limit or monetise undo in the product build.

### 5.8 Tutorial

The game teaches through a **tutorial**, not through rule text. A tutorial step is a short overlay (a highlight on the object it explains and one or two lines of text) that the view shows at two moments:

1. **At the start of the game**: the first time the game is launched, before Level 1.
2. **Each time a new mechanic or booster opens**: the first time a level contains the mechanic, or the first time the booster is offered.

Each step is shown **once** and recorded in the player's save data, not in the engine state, so determinism and undo are unaffected (I4). Showing a step is not a move and changes nothing in the game (I2); input is ignored while it is open. Steps are listed by `TutorialId` in a data asset (I10); `TutorialId` values are append-only. The text lives in the text table (§9). A booster button that is off shows the reason on the button itself; that is part of the button, not a tutorial step.

| `TutorialId` | Shown | Content (English draft; the game shows it in Vietnamese) |
|---|---|---|
| `Slide` | game start | Drag a row or a column to slide it. Three or more of a kind in a line vanish. |
| `Targets` | game start | Matching blocks fill the targets at the top. Fill a target to finish it. |
| `Queue` | game start | Blocks no target wants wait in the queue. If it overflows, you lose. |
| `Dual` | first level with a dual block | A two-colour block counts as both of its colours. |
| `X2` | first level with a ×2 block | A ×2 block gives two units. |
| `LockedSlot` | first level with a locked target | This target opens when the one next to it is done. |
| `QueueGrow` | first level with `qGrow` | A queue chair opens each time you finish a target. |
| `Chain` | first level with a chain | A chain locks its row or column. A match next to it breaks the chain. |
| `Seal` | first level with a sealed block | A locked block opens when the blocks around it are cleared. |
| `Spring` | first level with a spring | Spring: each notch moves 2 cells. |
| `Hammer` | the booster is first offered | Hammer: tap the block to cut. |
| `SwapTarget` | the booster is first offered | Swap target: brings forward a target you already have units for. |
| `Stash` | the booster is first offered | Stash: moves the whole queue into the vault. |

**Order.** Level 1 shows the three start steps only. Level 2 adds `Dual`, `X2`, `LockedSlot` and `QueueGrow`. Level 3 adds `Chain`, `Seal` and `Spring`, all at once on purpose to test whether the rules coexist; a product build should spread them over more levels, and each step still appears at the first level that has its mechanic.

### 5.9 Boosters [BUILT in the prototype; economy PROPOSED]

Three optional, consumable actions. They are **not moves** (the slide counter does not change), they never advance a column stream, they **never put a unit in the queue**, and each runs as one atomic resolution (I3, I4, I8). Their effect on the board is part of every snapshot, so undo takes the effect back, but not the use (§5.7). They are display-named in §9; the code names below are the contract.

**Economy.** A booster use is bought and spent: a player may use as many as they own, there is no per-level cap, and a used booster is never refunded (not by undo, restart or a loss, §5.7). Prices, the currency and any free uses are product settings and live in `GameConfig`. The prototype gives unlimited uses, so its play says nothing about how often boosters will be used.

| Booster | Code name | Rule |
|---|---|---|
| **Hammer** | `Hammer(row, col)` | Cut **one** block, **ignoring chains and seals** (any block can be cut). Input: press the button, then tap a cell; a tap outside the grid cancels. The cut block's units go by §5.4 "Where a unit goes" rules 1 and 2 (a ×2 block gives two units; a dual block tries both colours) and otherwise **vanish**; they never go to the queue. The four neighbours of the cut cell lose 1 seal, like a consumed cell; chains of neighbours are **not** touched. Then the completions, the settle and further rounds run, and every one of those rounds is a **cascade** (round 2 or later), so surplus units vanish (I6). The `Clear` phase carries `Booster = Hammer` |
| **Swap target** | `SwapInfo` / `SwapTarget` | **Automatic.** Choose the colour with the most units in the queue among colours with at least `swapMin` units, **no unlocked open slot of that colour**, and an upcoming target of that colour in the strip. Ties: the colour whose upcoming target is **earliest in the strip**, then the lower colour number. That upcoming target takes the place of the unlocked open slot with the **fewest units** (ties: leftmost). The displaced target **keeps its units and its `cap`** and goes to the **head of the strip**. The `Swap` phase is followed by `pull` and any completions. With no valid choice the button is off and says why. The button shows its choice before it is pressed (colour, count, slot) |
| **Stash** | `Stash` | Move **every** queue unit into the **vault**. The vault has no capacity limit and is **not part of the loss rule**. From then on every `pull` scans the vault before the queue, so a stored unit flies into the first slot that accepts its colour. Units whose colour no target needs any more simply stay (they are harmless). The `Stash` phase is followed by `pull` and any completions. Off while the queue is empty |

**State.** `Seq` is the list of targets `{Color, Cap}` (a list, because Swap reorders it), `Pre[k]` is the number of units already banked in the target at `Seq[k]` (same length as `Seq`; 0 unless the target was displaced by a swap), and `Vault` is the list of stored units. A completion takes the next target from `Seq[SeqI]`, with `Pre[SeqI]` units already inside and its own `Cap`. A swap moves one entry out of the strip and one in, always in `Seq` and `Pre` together. **The multiset of `Seq` (colour and cap pairs) never changes**, so a level stays solvable (I11) whatever the player swaps.

---

## 6. Interfaces

The engine of Phase 0 and Phase 1 exists in `Assets/Scripts/Engine/` (rows listed as built after the table); the rest is **[PROPOSED]**. This section is the contract the engine exposes; use these names unless a decision changes them. The engine is a **pure C# assembly** (`SlideJam.Engine`, "No Engine References" ticked) with no `MonoBehaviour`, `Time`, `UnityEngine.Random` or other Unity type. The `LevelConfig` / `GameConfig` ScriptableObjects live in the game assembly and are converted to plain data structs that the engine reads. The view only plays phases.

| Member | Purpose |
|---|---|
| `BoardState.New(LevelData)` | A new level state: grid, stream pointers, counters. **[PROPOSED, Phase 2]** also the first `ActiveSlots` slots taken from `Seq`, an empty queue and vault, `QOpen = QOpenStart`, `Pre` of zeros and `Ended = null`; `Seq`, `Pre` and `Vault` are in the state from Phase 2 so `Clone` and `Hash` do not change again with the boosters of Phase 5 (the vault stays empty until then) |
| `MoveResolver.Move(state, axis, index, notches) → MoveResult` | Apply a slide to a **copy**; returns `{ Ok, State, Phases }`. `Ok` is false for a zero shift, a locked line or a finished run; a refused move returns the input state unchanged and no phases |
| `MoveResolver.Preview(state, axis, index, notches) → Preview?` | The drag preview (§4.2): `{ Cells, ToQueue, Lose }`, or `null` when nothing matches. It calls `Move` on a copy |
| `Board.Shift(grid, axis, index, k)` · `Board.ShiftAmount(notches, step, length)` · `Board.LineLength(level, axis)` · `Board.FloorMod` | The slide on a raw grid · `k = floorMod(notches × step, length)` · the length of a line |
| `Board.LineLocked(state, axis, index)` | Whether a row or column can be slid (§5.2) |
| `Board.LineCells(axis, index)` · `Board.StepOf(state, axis, index)` | The cells of a line · the cells moved per notch (1 or 2) |
| `MoveResolver.Hammer(state, row, col) → MoveResult` | Booster §5.9. `Ok` is false for an empty cell or a finished run |
| `MoveResolver.SwapInfo(state) → SwapChoice?` · `MoveResolver.SwapTarget(state) → MoveResult` | The choice Swap target would make (`Color`, `Count`, `Slot`, `StripIndex`) or `null` · apply it |
| `MoveResolver.Stash(state) → MoveResult` | Booster §5.9. `Ok` is false for an empty queue |
| `Matcher.Find(grid) → List<Cell>` · `Matcher.HasMatch(grid)` | Every cell of every run of `Matcher.MinRun` (3) or more, each once, in row-major order |
| `ColumnStream.At(seed, column, index, colors)` · `Mulberry32.At(seed, index)` | The colour of a column's `index`-th spawn (used for the next-spawn dots) · the raw stream value |
| `BoardState.Clone()` | Deep copy used for snapshots and undo |
| `BoardState.Hash()` | A stable hash of the whole state (including stream pointers) for determinism tests |

The stream generator is `mulberry32` in `uint` arithmetic with wrap-around (`unchecked`), so one seed gives the same colours on every platform. No `float`, no `System.Random`.

**Built so far (Phase 0 and 1):** every row above except `Preview`, `Hammer`, `SwapInfo`, `SwapTarget`, `Stash`, `LineLocked`, `LineCells` and `StepOf`. `Matcher` handles plain blocks only.

**Test access [PROPOSED, Phase 2].** The engine assembly declares `InternalsVisibleTo("SlideJam.Tests.EditMode")`, so a test can build a state with chosen slots, queue and `Seq` without a public setter. Nothing in the game assembly uses it. The tests that need a run that cannot end (the Phase 1 sweeps) use a sandbox-level helper (a very long `Seq` and a very large queue) instead of the old four-argument `LevelData`; they are not weakened.

### 6.1 Phase list

`Phases[0]` is always `Shift`. Each phase carries a full snapshot of the state after it (`Snap`), so the view can replay them in order without re-running logic.

| Phase | Extra data |
|---|---|
| `Shift` | — |
| `Clear` | `Round` (round number), `Booster` (`None` or `Hammer`), `Vanished` (count), `MatchedCells`, `Flights`, `Unchained` (ids), `Unsealed` (ids) |
| `Complete` | `Slot`, `Unlocked` (slot indices), `QUnlocked` |
| `Pull` | `Flights` (units may come from the vault) |
| `Swap` | `Slot`, `Color`, `From` (the displaced colour) |
| `Stash` | `Flights` (queue to vault) |
| `Fall` | `Starts`: a list of `FallStart {Id, From}` for every block that moved or spawned, in row-major order of its final cell (a list, so the order never depends on a dictionary). `From.Row` is negative for a block spawned above the grid |
| `End` | `Reason` (`EndReason`: `Win` or `QueueOverflow`); the final state has `Ended` set to the same value |

**Built so far (Phase 1):** `PhaseKind` is `Shift`, `Clear`, `Fall`; a move is `Shift` followed by a `Clear`, `Fall` pair per round. `Clear` carries `Round` and `MatchedCells` and only empties the cells (no units yet). The other kinds join at the end of the enum (append-only). `Phase` is one class with the fields of this table; fields that do not belong to its kind are empty.

**Planned for Phase 2:** `PhaseKind` gains `Complete`, `Pull` and `End` at the end of the enum, `Clear` gains `Flights` and `Vanished`, and a move is `Shift` followed, per round, by `Clear`, then a `Complete` / `Pull` group for each completion, then `Fall`. A win or a loss ends the move right after its check with an `End` phase and no `Fall` (the board may then contain empty cells, I9). `Move` refuses a state whose `Ended` is not null.

A **flight** is `{Color, From, To}` with `From` a cell, a queue index or a vault index (`{V}`) and `To` one of `{Slot, Pip}`, `{Q}` (queue chair), `Vault` or `Vanish`.

**State shape.** `Grid[r, c]` is a `Block` struct `{Id, Kind (Normal | Dual), Color | Colors, X2, Spring, Chain, Seal}` (Phase 1 has `Id` and `Color` only); an empty cell is `Id == 0` and real ids start at 1; `Slots[i]` is `null` or `{Color, Count, Cap, Locked}` (`Locked` is always false until Phase 4); the rest is `Queue`, `Vault`, `QOpen`, `Seq` (entries `{Color, Cap}`), `Pre`, `SeqI`, `Ptr[]`, `Moves`, `Done`, `Ended` (`EndReason?`, null while the run goes on), `NextId`. `LevelData` carries `QMax`, `QOpen`, `QGrow`, `LockedSlot`, `Seq` and the special blocks; `SwapMin` comes from `GameConfig`.

### 6.2 Proposed project layout

```
Assets/Scripts/Engine/   assembly SlideJam.Engine
  Rng/                   Mulberry32, ColumnStream
  Board/                 Block, Axis, Cell, LevelData, BoardState, Board, Matcher, GridGenerator, SandSettler (Phase 2 adds Target, Slot, EndReason)
  Moves/                 PhaseKind, Phase, FallStart, MoveResult, MoveResolver
Assets/Scripts/Game/     assembly SlideJam.Game: GameController (the one wired root), BoardView, BlockView, DragInput, PhasePlayer, HudView, CameraFit, config ScriptableObjects (`GameConfig`, `LevelConfig`, with the conversion to `LevelData`); Phase 2 adds SlotView, QueueView, the end overlay
Assets/Prefabs/          Block.prefab (the pooled block view)
Assets/Design/           GameConfig.asset, Levels/LevelConfig_*.asset
Assets/Tests/EditMode/   assembly SlideJam.Tests.EditMode (Rng/, Board/, Moves/): rules, invariants, determinism
Assets/Tests/PlayMode/   a real drag against the engine's prediction
```

---

## 7. Implementation notes

- **Animation [BUILT for Shift, Clear and Fall].** The view plays the phase list one phase at a time and does not decide anything. Starting timings from the prototypes: match pulse 170 ms, a unit's flight 360 ms plus 38–40 ms stagger per unit, slot pop and slot enter 260 ms each, settle `160 + 90 × longest distance` ms, drag snap 130 ms, floating labels 900 ms. They are serialized fields on `PhasePlayer` for now and belong in `GameConfig` (I10). `Clear` is a scale pulse of the matched blocks (1.15 at the middle, 0 at the end); `Fall` moves each block of `Starts` from its start cell, with a smooth-step curve. After the last phase `BoardView.Matches` checks the screen against the state and `ShowState` redraws it.
- **Targets and queue during a flight.** While units fly, the slots and the queue are drawn from the state *before* the phase, and the new state appears when the flight ends, so a pip fills when its block lands, not when it leaves.
- **Input [BUILT].** One pointer path for mouse and touch using the classic Input Manager (`Input.GetMouseButton*` / `Input.touches`); the Input System package is not installed. Input is ignored while a phase list is playing (I8).
- **Preview cost.** The preview runs a full resolution on a copy at every pointer move. It is small on 7×7. Cache it per notch count if it shows up in a profile.
- **Tests [PROPOSED].** Edit Mode (Unity Test Framework): a rule test per mechanic, the invariants (I4 same seed and moves give the same `Hash()`, I5 preview equals result, I9 no hole after a non-terminal move), a direct sand-flow test (a chain block stays put and the cells under it are refilled), a cap test (targets of 3, 4 and 5 units complete at exactly their own `cap`), a booster test (after any booster use: no hole, the multiset of `Seq` unchanged, `(Seq.Length − SeqI) + open slots = total − Done`, no stream pointer moved, no unit added to the queue, undo restores the board state exactly and does not refund the use). Play Mode: a real drag on the board compared with the engine's prediction for the same move, including a spring line.
- **Determinism.** Snapshots are value copies of `BoardState`, so undo restores the stream pointers and a replay of the move list is exact (I4). Do not use `Dictionary` or `HashSet` iteration order, or `float` maths, in anything that decides a rule.

**Hazards — standing conventions**

- **Streams are stateless.** `Mulberry32.At` and `ColumnStream.At` are pure functions of the seed, the column and the index (a closed form: no list, no cache), so they are safe to call from the view for the next-spawn dots. The only stream state is `Ptr[]` in the board state; do not advance it from anywhere but a spawn (I4).
- **Spring and line length.** With an even line length, a spring's step of 2 reaches `k = 0` at half a revolution and the move is refused as a zero shift. Level 3 has length 7 (odd) so it does not happen. Keep spring lines odd or handle it.
- **Row and column chains vs gravity.** A `row` or `col` chain block is carried by slides along its free axis but is immovable for the settle. Do not treat "locked" and "immovable" as the same thing.
- **Holes.** A loss is reported before the final settle, so a lost state may contain empty cells. Do not run the grid-full assertion on a lost state (I9).
- **Cascade round numbering.** Round numbers restart at 1 on every slide. The vanish rule (I6) depends on it, so a refactor that merges rounds across slides would change the rule.
- **Booster ties.** `SwapInfo` must break ties by the fixed order in §5.9, never by dictionary or set iteration order (I4).
- **`Seq` and `Pre` move together.** A swap that splices one without the other desynchronises the next strip. The vault is **not** counted in the overflow check.
- **`cap` is per target.** A constant 3 for the units of a target anywhere in the engine or the view is a bug (I10); read the slot's own `Cap`.
- **Guards.** The settle stops with an error after 500 passes and a resolution after 500 rounds (`MaxRounds`). Both guard against a bug and are not rules.
- **Negative modulo.** In C#, `%` of a negative number is negative. Use a floor-mod for the shift `k` and for wrap-around.

---

## 8. Glossary

Use these exact terms in code, commits and agent prompts.

| Term | Meaning |
|---|---|
| **Slide** | The one input: moving a whole row or column by a number of cells with wrap-around. One drag = one slide = one move |
| **Line** | A whole row or a whole column |
| **Notch** | One step of a drag, normally 1 cell, 2 cells on a line with a spring |
| **Block** | One cell's content: a normal block, a dual block, with optional ×2, spring, chain or seal |
| **Match** | 3 or more same-colour cells in a line (§5.3) |
| **Round** | One find-and-clear step in a resolution. Round 1 is caused by the player's slide; later rounds are cascades |
| **Cascade** | Any round after round 1 |
| **Unit** | One piece of colour produced by a consumed block (2 for ×2). Units go to slots, the queue or vanish |
| **Target / Slot** | A request for `cap` units of one colour (3 to 5, set per target). The `sequence` is the list of targets; up to 3 are on screen as slots |
| **Pip** | One of the places in a slot (as many as its `cap`) |
| **Cap** | The number of units a target needs, 3 to 5, set per target in `sequence` (§5.1) |
| **Queue** | The waiting row for units no slot accepts. Holds at most `qOpen` units |
| **Vault** | Unlimited storage filled by the Stash booster; not part of the loss rule; emptied into slots by `pull`, before the queue |
| **Booster** | An optional consumable action outside the move counter: Hammer, Swap target, Stash (§5.9) |
| **`Seq` / `Pre`** | The target list `{Color, Cap}` (reorderable by Swap) and the units already banked in each upcoming target |
| **Theme** | The display names, art and layout details of the fruit garden (§9); code keeps the terms in this glossary |
| **`qOpen`** | The number of queue chairs currently usable (never above `queueMax`) |
| **Pull** | Moving queue units into slots after a completion |
| **Vanish** | A cascade unit that no slot accepts disappears (I6) |
| **Overflow** | The queue holding more than `qOpen` units at the overflow check; the only loss |
| **Settle** | Falling, sand flow and refill after blocks are consumed (§5.5) |
| **Sand flow** | A block diagonally sliding past a chain block instead of stacking on it |
| **Stream** | A column's fixed, seeded list of colours that it spawns, one per refill |
| **Next-spawn dot** | The small block above a column showing a colour its stream will spawn next |
| **Chain** | A block that cannot be consumed or settled, and that locks its row, its column or both; removed by touching matches |
| **Sealed block** | A block that cannot match until enough cells around it are cleared ("Quả non" in the theme) |
| **Spring block** | A block that makes its line move 2 cells per notch |
| **×2 block** | A block that gives 2 units when consumed |
| **Dual block** | A block that counts as two colours |
| **Locked slot** | A target that accepts nothing until its neighbour slot completes |
| **Preview** | The outline and label shown while dragging (§4.2) |
| **`EndReason`** | `Win` or `QueueOverflow`; the state field `Ended` holds it, or null while the run goes on |
| **Stage** | The world-space parent of the slots, next strip, queue and grid; the camera fits its bounds (§4) |

---

## 9. Theme: "Vườn trái cây" (fruit garden) [display only]

The theme changes **display strings, art and layout details only**. Code, tests, commits and this document keep the terms of §8: do not rename an identifier to a theme name. Display strings live in a text table (Vietnamese first), so the theme can change without touching the engine.

| Engine term | Display name (Vietnamese) |
|---|---|
| Block | quả |
| Target slot | Giỏ |
| Next strip | Kệ giỏ sắp tới |
| Queue | Khay chờ |
| Vault | Hầm trữ |
| Next-spawn dots | hai quả treo trên cành, trên mỗi cột |
| Chain | Dây leo |
| Sealed block | Quả non (nhạt màu, nét đứt, số = số quả kề bên cần hái) |
| ×2 block | Quả đôi |
| Dual block | Quả ghép |
| Spring block | Tua cuốn |
| Locked target slot | Giỏ buộc dây |
| Locked queue chair | Ô buộc dây |
| Vanish (cascade surplus) | Chim nhặt ×N |
| Hammer | Kéo tỉa |
| Swap target | Đổi giỏ |
| Stash | Hầm trữ |
| Levels 1 / 2 / 3 | Vườn nhỏ / Vườn ươm / Nhà kính |
| Win / loss | "Thu hoạch xong!" / "Khay đầy rồi" |

**Fruit set.** Colour 0 **strawberry** (red), 1 **blueberry** (blue), 2 **pear** (green), 3 **orange** (orange), 4 **plum** (purple). Display names: Dâu tây, Việt quất, Lê, Cam, Mận. The art assets for these five already exist and may be replaced later; colour numbers 0–4 are the identity of a colour and the fruit is only its skin, so a replacement changes assets and the text table, not the engine. The five fruits must still differ in silhouette as well as hue (§5.1); this is not yet checked for the current assets, so check it with a simulated colour-vision deficiency once the assets are final.

**Art decisions [PROVISIONAL].** The view keeps the earlier concept for now and may change it when suitable real assets are found. Nothing in the engine depends on it.

- **Baskets** are seen from above: 106×84 units, three visible inner walls (top, left, right) with depth shading, a label pill on the front rim (fruit icon and `n/cap`), a lining tinted by the fruit with a coloured inner edge, dashed rings and a faint fruit for empty places, vines and a padlock when locked, a gold outline when full.
- **Blocks** sit on cream tiles. A sealed block is the faded fruit with a dashed outline and a number badge; a chain is green vines across the block (crossing for `both`, along the row or column for `row` and `col`) with a layer badge above 1; ×2 is two small fruits and a badge; dual is split diagonally; a spring has a coil badge.
- **Queue** is a wooden tray; a locked chair is tied with a vine knot. The **vault** is a wooden bin (two rows of five, §4). The next-spawn dots hang on a branch above the grid, the nearer one larger.
- **One background for all levels**, with light decoration only (drifting clouds, swaying leaves). Rounded fonts (Nunito for text, Fredoka for headings).
- **No sound** yet.
