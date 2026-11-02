# Slide Jam — Design & Implementation Spec

**Origin:** self-proposed hybrid-casual mobile puzzle, built as two HTML prototypes (Level 1 and Level 2) before any Unity work
**Engine:** prototype in HTML5 canvas · target engine Unity (2D) · one finger: drag only · single screen
**Platform:** target iOS and Android, portrait. The prototypes run in any browser (pointer events, so mouse and touch both work)
**Working title:** Slide Jam (change freely; nothing in the rules depends on it)
**Version:** 0.1 (2026-11-02) — first spec, written from the two prototypes. It records what was built and measured, plus a few clearly marked proposals.
**Prototypes:** Level 1 `https://claude.ai/artifact/74cjnzC7cmy3WbNNQDCjX1` · Level 2 `https://claude.ai/artifact/WJvc3cPaUSM6xarp2d2zYw`

---

## 0. How to use this document

This doc is context for both humans and AI agents. If you are an agent reading this:

- **§2 (Invariants) is law.** Do not violate those rules to make a task easier. If a request conflicts with an invariant, say so instead of silently working around it.
- **§5 (Rules & Tuning) is the single source of truth for numbers and behaviour.** Every tuning value lives in a data asset (`LevelConfig` or `GameConfig` in Unity; the `CFG` object and the constants at the top of `logic.js` in the prototype), never in view or input code. The *Where* column of each table says which one holds a number. If you need a number that isn't in §5, add it to the right place and add it to §5 — don't invent a literal.
- **§8 (Glossary)** defines project terminology. Use these exact names for classes, files and variables.
- Anything marked **[LOCKED]** must not be redesigned.
- **[BUILT]** means the prototype implements it and it was tested. **[PROPOSED]** means it is a plan only; treat it as a suggestion, not as fact.

---

## 1. Pitch

A grid of coloured blocks. You **slide one row or one column** and the blocks wrap around the edge. Any three in a line **vanish on their own** and fly up into the **target slots**, which want a certain colour. What no slot wants goes into a small **queue**. If the queue overflows, you lose.

You never choose what to eat. You choose **what to line up, and when**: a match of the wrong colour is a cost, a match of the right colour is progress. Every level adds obstacles (chains, seals, locked slots) that change how you plan the next slide.

**Genre:** single-screen turn-based puzzle · **Level length:** 20–40 slides (measured, §5.10) · **Session:** instant restart, unlimited undo in the prototype

**Why it works [design thesis].** The slide is a physical, readable action. The queue gives a visible, shrinking budget for mistakes. The skill is defensive: avoid the wrong match, not chase every match.

---

## 2. Invariants [LOCKED]

These are the load-bearing rules. Breaking any one of them breaks the game.

| # | Rule | Why |
|---|---|---|
| I1 | A level is **won** when every target in its sequence (`SEQ`) is completed. It is **lost** the moment the queue holds more units than `qOpen` after a resolution step (§5.4, §5.9) | The one win and the one loss. No timers, no move limit |
| I2 | **Turn-based.** Nothing changes between inputs: no timers, no real-time pressure, no random events outside a move | The skill is planning. It also makes undo exact |
| I3 | **One input verb: slide a row or a column.** A drag of any length is **one move**. A drag whose net shift is zero is not a move and costs nothing | Keeps the game learnable in seconds and keeps the move counter honest |
| I4 | **Deterministic.** The same level seed and the same list of moves always give the same result, including every refill colour. No hidden randomness during play | Solver and bot checks, replays, regression tests, seeded daily levels |
| I5 | **No lies.** The next-spawn dots and the drag preview show what will really happen. Nothing the player has already seen is secretly recoloured | The player must be able to blame a loss on their own plan |
| I6 | **Combos never punish.** A match caused by a cascade (resolution round 2 or later) can only help: units no slot accepts **vanish**. Only a match caused directly by the player's slide can put units into the queue (§5.4) | Combos are hard to foresee and long ones are random. Losing to one reads as unfair |
| I7 | **Locks close a door, never end the run.** Every lock has an exit through normal play: a chain is removed by any match that touches it, a seal by clearing the cells around it, a locked slot by completing its neighbour, locked queue chairs by completing targets (§5.7) | The player must always be able to route around a block |
| I8 | **Resolution is atomic.** After a slide, all phases (§5.4) finish before the next input is accepted. Win and loss are checked at the fixed points in §5.4 and nowhere else | Overlapping animations and re-entrant input are the usual source of unrepeatable bugs |
| I9 | **The board is always full after settling.** No empty cell survives a settle, whatever the obstacles (§5.5). The only exception is a terminal state (a loss is reported before the final settle) | Holes under chains would make cells unreachable |
| I10 | All tuning values live in data, never in view or input code | The last hours of a project are for tuning, not refactoring |
| I11 | **Every shipped level is solvable and has measured stats** (random-play and bot win rates, §5.10) | A level nobody can win is a bug, not a difficulty setting. **[PROPOSED]**: today solvability is shown by a greedy bot, not by an exhaustive solver |

> **I4 detail — streams.** Each column owns an independent stream of colours, built from the level seed (`SEED + (column + 1) × 7919`). A refill takes the next colour from the stream of the column it spawns in, and a stream is only ever advanced by a spawn. So the colour of the next two spawns in a column is always known, and the dots above the grid show exactly those.
>
> **I5 deviation, Level 1.** The Level 1 prototype still applies an "anti-match" recolour at spawn time (a new block that would complete a match takes the next colour instead). That makes the dots occasionally wrong. Level 2 removed it. **[PROPOSED]** align Level 1 by removing it, or keep it and move the correction before the dots are computed.

> **I6 detail — what counts as a cascade.** Resolution runs in numbered rounds. Round 1 is the matches present right after the player's slide. Every later round (matches created by the settle, a seal opening or a chain breaking) is a cascade round. Units from a cascade round that fit a slot go to the slot as usual; units that do not fit disappear with a "Tan biến ×N" label.

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

**The difficulty target.** A level should be hard enough that random play rarely wins and easy enough that a thoughtful player wins. Measured values are in §5.10; the working target is **random play ≈ 5–20% wins** and **a player who looks ahead one slide wins with a lot of room to spare**. Tune with the queue size first, the target sequence second and the obstacle count last.

---

## 4. Board objects and HUD

The screen is one canvas, 360 logical units wide, drawn top to bottom in this order (Level 2). Level 1 differs in grid size and cell size (§5.1), has no locked chairs, and puts the **queue below the grid** instead of between the slots and the grid.

| Object | Role |
|---|---|
| **Target slots** ×3 | The slots currently asking for blocks. Each shows its colour, a count `n/3` and three pips. Filling the third pip completes it (§5.4) |
| **Next strip** ("Tiếp theo") | The colours of the targets still to come, so the player can plan ahead |
| **Queue** | A row of 6 chairs. Level 2 opens only the first `qOpen` (4 at the start); the rest show a padlock. Shows `n/qOpen` |
| **Next-spawn dots** | Two small blocks above each column: the next two colours that column will spawn (I5) |
| **Grid** | 7×7 (Level 2) or 6×6 (Level 1). Edge markers (red bars) show which rows and columns are locked |
| **HUD** | Slide count (`Lượt`) and targets left (`Mục tiêu còn`) |
| **Undo / Restart** | Two buttons under the grid. Both are disabled while a resolution is playing |
| **Toast** | A short message centred on the grid for rule feedback (§5.11) |
| **Overlay** | Win / loss card with Undo (loss only) and Restart |

### 4.1 Input

Drag only. Press on a cell of the grid, move, release.

- **Axis lock.** Once the pointer has moved more than **8 logical units** from the press point, the axis is chosen: row if `|dx| ≥ |dy|`, otherwise column. The line is the row or column of the pressed cell. The axis never changes during the drag.
- **Notches.** `notches = round(offset / cellSize)`. The line follows the finger continuously with wrap-around (a block leaving one edge appears on the other, clipped to the grid). On release it settles to the nearest whole notch in 130 ms. A negative notch count slides the other way.
- **A locked line** does not move: the drag is dead, the line flashes red for 0.5 s and a toast says why (`RowLocked` / `ColLocked`, §5.11).
- **Spring.** If the line contains a spring block, one notch moves 2 cells and the line follows the finger at the same ratio (a toast `SpringStep` appears once per drag). With an odd line length the shift is never zero unless the player slides a whole revolution.
- **A drag that ends on notch 0** (or a multiple of the line length) snaps back in 140 ms and counts as nothing (I3).
- Input is ignored while a resolution is playing, while a panel is open and after the run has ended.

### 4.2 Drag preview [BUILT]

While dragging, the game runs the real move on a copy of the state (`preview`) and shows its first matches:

- **White outline** (pulsing) on every cell of the first clear round: the units go to targets.
- **Orange outline** instead, when the whole resolution will add units to the queue. Dashed orange boxes appear on the queue chairs they will take, and the queue header reads `+N vào hàng chờ`.
- **Red text** `sẽ tràn → thua!` when the whole resolution would end the run by overflow.

The number in the header is the **net change of the queue after the whole chain**, not just the first round, so a move that overflows and is then rescued by a completion is not reported as a loss. The preview never shows a cascade's cells, only the first round's, so the drag shows "what I am about to cause", and the label shows "what that will cost in total" (I5).

---

## 5. Rules & Tuning [SOURCE OF TRUTH]

> *Where* names the asset or constant that holds a value. In the prototype that is the constants at the top of `logic.js` and the `CFG` object (`setCfg` changes it for tests). Numbers are tuning values and are expected to be adjusted.

### 5.1 Global

| Key | Level 1 | Level 2 | Where |
|---|---|---|---|
| `rows` × `cols` | 6 × 6 | 7 × 7 | `LevelConfig` |
| `colors` (`NCOL`) | 5 | 5 | `LevelConfig` |
| `cap` (units per slot) | 3 | 3 | `GameConfig` |
| `activeSlots` | 3 | 3 | `LevelConfig` |
| `sequence` (`SEQ`) | 12 targets: 0 1 2 3 4 1 0 3 2 4 0 1 | same | `LevelConfig` |
| `lockedSlotIndex` | none | slot 2 | `LevelConfig` |
| `queueMax` (`Q_MAX`) | 6 | 6 | `GameConfig` |
| `qOpenStart` (`qOpen`) | 6 (fixed) | 4, **+1 per completed target** up to `queueMax` | `LevelConfig` |
| `sealNeed` | none | 2 | `LevelConfig` |
| `seed` | 20261031 | 777123 | `LevelConfig` |
| Cell size · canvas width | 54 · 360 | 48 · 360 | view constants |
| Vertical layout | targets, next strip, dots, grid, queue | targets, next strip, queue, dots, grid | view constants |

Colours are numbered 0–4 and each has its own **shape** as well as its hue (circle, diamond, triangle, rounded square, plus) so the game is playable without colour vision.

### 5.2 Slide

A **line** is one whole row or one whole column. Sliding by `k` cells moves every block `k` places along the line, and the ones that pass the end re-enter at the start.

```
k = (notches × step) mod length      step = 2 if the line contains a spring block, else 1
```

- `k = 0` is not a move (I3). Otherwise the slide is applied and the move counter goes up by one.
- A line is **locked** when it contains a chain block whose lock covers that axis (§5.7). A locked line cannot be slid.
- A slide moves whatever is in the line, including chain blocks of another axis, sealed blocks and spring blocks.

### 5.3 Matching

- A **match** is a run of **3 or more** cells of the same colour in one row or one column. Horizontal and vertical runs are found independently; there are no L, T or square shapes (Level 1 and 2 do not use them). A cell in two runs counts once.
- A **dual block** counts as both of its colours, so it can complete a run of either colour. A **sealed block** counts as no colour until it opens. A **chained block** counts as its colour for finding matches but is **not consumed** (§5.7).
- All matches on the board are found at once and cleared together in one **round**.

### 5.4 Resolution order [LOCKED]

After a valid slide the engine runs these phases in order, and repeats from `clear` until a round finds no match. Each phase is one entry in the move result (§6.1) so the view can animate it.

| # | Phase | What happens |
|---|---|---|
| 1 | `shift` | The line is slid. The move counter goes up |
| 2 | `clear` (round *n*) | Find matches. **Chains:** every chained block inside a matched cell or next to one (4 neighbours) loses one layer (§5.7). **Seals:** every sealed block next to a consumed cell loses 1 (§5.7). **Units:** every consumed block gives 1 unit (2 for a ×2 block), placed by the rule below. The consumed cells become empty |
| 3 | `complete` (0 or more) | Every slot with `count ≥ cap` is completed (see below), one at a time |
| 4 | `pull` (0 or 1 after each completion) | Units waiting in the queue move into slots that accept them |
| 5 | overflow check | If the queue holds more than `qOpen` units: **loss** (`QueueOverflow`) and the move ends |
| 6 | win check | If every slot is empty: **win** and the move ends |
| 7 | `fall` | Settle and refill (§5.5) |

**Where a unit goes** (in this order):

1. The first slot, left to right, that is unlocked, has the unit's colour and has fewer than `cap` units.
2. If the block is a **dual** block and step 1 failed, the same test with its other colour.
3. Otherwise, in round 1: **the queue** (appended at the end). In round 2 or later: **the unit vanishes** (I6).

A ×2 block gives two separate units; each goes through the three steps on its own, so one can fill a slot while the other vanishes or queues.

**Completing a slot** (`complete`): the completed count goes up by one; the slot is replaced by the next target of `sequence` (or becomes empty when the sequence is finished); a **locked neighbour slot** (index ± 1) opens; `qOpen` goes up by one (up to `queueMax`). Then `pull` runs: the queue is scanned from the first unit to the last, and every unit that a slot now accepts (rule 1 above) flies to that slot, the rest keep their order. A pull can fill another slot, which then completes in turn, and so on until nothing is full.

**Check points.** The overflow check happens after the completions of a round and before the settle. The queue can exceed `qOpen` *inside* a round (units are added before the completions run); only the state after the completions counts. A completion that opens a queue chair and pulls units out can therefore rescue a move.

### 5.5 Settle: falling and sand flow [BUILT, Level 2]

Level 1 settles with ordinary gravity (blocks fall straight down, empty cells at the top refill). Level 2 settles with **sand flow** because chain blocks do not fall.

A settle repeats the following passes until nothing changes (a guard stops it after 500 passes):

1. **Move pass.** Scan rows from the second-to-last row up to row 0 (so lower blocks move first). Inside a row the scan direction alternates from row to row. For every block that is **not chained**:
   - if the cell below is empty: the block falls one cell;
   - otherwise, if the cell below holds a **chain block**, or the cell **beside it in the same row** (towards a free diagonal) holds a chain block, **and** the diagonal cell below on that side is empty: the block slides diagonally into it. Which side is tried first depends on `(row + column)`: odd tries left first, even tries right first. If both sides are possible only one is used.
2. **Spawn pass.** Every empty cell in row 0 gets the next colour of its column's stream (I4). The new block starts one row above the grid (row −1, −2 … when several spawn in the same column during one settle).

Chain blocks never move during a settle, and a block that is not next to a chain behaves exactly like ordinary gravity (it only ever moves diagonally when a chain forces it to). A cell under a chain is filled from the side: the neighbouring column's block flows in diagonally, and the neighbouring column refills from the top, so the cell is never left empty (I9).

The move result records, for each block that moved or spawned, its **start cell**, so the view can animate the flow.

### 5.6 Level data and generation

- The **initial grid** is rolled cell by cell from the seed, re-rolling a cell until it would not complete a 3-run horizontally or vertically. Special blocks (§5.7) are then placed at fixed cells, and a last pass re-rolls only plain blocks until the grid has no match at all. A level never starts with a match.
- The **refill streams** come from the same seed (§2, I4 detail). A stream is created on demand and extended as far as needed, so a level has no fixed length.
- The **target sequence** is part of the level. The first `activeSlots` entries start on screen; each completion brings in the next.
- **Level 2 special blocks** (`row, column`, from the top left, starting at 0):

| Block | Cell | Colour | Parameters |
|---|---|---|---|
| Chain, **both** axes | (4, 1) | green (2) | 1 layer |
| Chain, **row** axis | (1, 5) | yellow (3) | 2 layers |
| Spring | (6, 4) | purple (4) | — |
| ×2 | (2, 5) | blue (1) | — |
| ×2 | (5, 1) | red (0) | — |
| Dual | (3, 1) | red + blue | — |
| Dual | (1, 3) | green + yellow | — |
| Sealed | (3, 3) | yellow (3) | `sealNeed` 2 |
| Sealed | (5, 5) | red (0) | `sealNeed` 2 |
| Sealed | (0, 2) | purple (4) | `sealNeed` 2 |

Level 1 has no special blocks, no locks and no unlocking queue.

### 5.7 Mechanics catalogue

Each mechanic is a property of a block or a slot. **Level 2 uses all of them; Level 1 uses none.**

| Mechanic | Rule | How it ends (I7) | Where |
|---|---|---|---|
| **Chain** (`chain: {lock, layers, key}`) | The block is **immovable by settle** (§5.5) and cannot be consumed. `lock` is `both` (the row **and** the column of the block cannot be slid), `row` (only the row) or `col` (only the column). A `row` or `col` chain block can still be carried along the free axis by a slide | Every **round**, each chained block that is in a matched cell or next to one loses 1 layer; when `layers` reaches 0 the chain is removed and it becomes an ordinary block. `key` (a colour index, `-1` = none) can require that the matched colour equals the key; **no level currently uses a key** | `LevelConfig` |
| **Sealed block** (`seal: n`) | Keeps its colour on screen but counts as no colour: it cannot join a match. It can be slid and it falls normally | Each consumed cell next to it (4 neighbours) lowers `seal` by 1; at 0 it opens and counts as its colour from the next round. The number is drawn on the lock | `LevelConfig` |
| **×2 block** (`x2`) | Gives 2 units when consumed | — | `LevelConfig` |
| **Dual block** (`colors: [a, b]`) | Counts as both colours when finding matches; its units can fill either colour (§5.4) | — | `LevelConfig` |
| **Spring block** (`spring`) | A line containing it moves 2 cells per notch (§5.2) | The block can be slid out of the line or consumed | `LevelConfig` |
| **Locked target slot** | A slot that shows a padlock and accepts nothing | Opens when its **neighbour slot** (index ± 1) completes | `LevelConfig` (`lockedSlotIndex`) |
| **Locked queue chairs** | Only `qOpen` of the 6 chairs can hold units; the rest show a padlock | One chair opens with each completed target | `LevelConfig` (`qOpenStart`) |

**Display.** Chains are drawn across the block (two crossing chains for `both`, horizontal for `row`, vertical for `col`) with the layer count in a circle if it is above 1. A sealed block is dimmed with a dashed outline, a padlock and its number. ×2 has a `×2` badge, a spring a coil badge, a dual block is split diagonally with both colour shapes. Locked rows and columns carry red bars at the grid edge.

**Not built, proposed as later content [PROPOSED]:** one-way rows, heavy blocks, a second slot-lock type, bigger grids, moving obstacles. None has been prototyped, so none has measured stats.

### 5.8 Win and loss

| `EndReason` | Meaning | Trigger |
|---|---|---|
| `Win` | Win | Every slot is empty after a completion (§5.4 step 6) |
| `QueueOverflow` | Loss | The queue holds more than `qOpen` units at the overflow check (§5.4 step 5) |

Ending text (prototype, Vietnamese): win `Hoàn thành! 🎉` with "N targets after M slides"; loss `Hàng chờ tràn` with a one-line reason and an **Undo** button. There is no move limit and no timer (I2).

**Undo.** The prototype keeps an unlimited stack of earlier states, one per valid move. Undo restores the state before the last move (including the refill stream positions, so the same move gives the same result, I4). Restart clears the stack. **[PROPOSED]** limit or monetise undo in the product build.

### 5.9 Balance method

All balance numbers come from running the logic with bots, headless:

- **Random bot:** picks a random line and a random non-zero shift until a legal move is found.
- **Greedy bot:** tries every legal slide (every line, every shift) and keeps the one with the best score. Score: `done × 12 + units in slots × 3 − queue size × 4`; a win is +1,000,000 and a loss −1,000,000. Ties are broken at random.

The greedy bot sees exact outcomes of a slide, including the whole cascade, which a human cannot compute. Read its result as **an upper bound on skill**, not as a typical player. A move limit of 200 slides stops a game and counts it as a timeout (none occurred).

### 5.10 Measured balance (exact simulations, not playtest data)

| Level / rule set | Random bot wins | Random: avg slides to win | Greedy bot wins | Greedy: avg slides to win |
|---|---|---|---|---|
| Level 1, rule "cascade surplus goes to queue" | 16% (of 300) | 30.2 | 100% (of 120) | 5.9 |
| Level 1, rule "cascade surplus vanishes" (current) | 20% (of 300) | 31.0 | 100% (of 120) | 5.4 |
| Level 2, current build | about 6% (17–19 of 300) | about 22 (random losses end after 12–13 slides on average) | 100% (of 40) | 3–4 (longest cascade seen: 7 rounds) |

- **Reading Level 2.** It is much harder for random play (6% vs 20%) because of the smaller opening queue and the locks, and the greedy bot still wins in 3–4 slides because sand flow keeps producing cascades and cascade surplus is free (I6). If long cascades make the level too easy for skilled players, the options are: stop new blocks from completing matches as they spawn (this would break I5 unless the dots are corrected), lower `qOpenStart`, add more targets, or add more locks.
- **Earlier finding.** With ordinary gravity and Level 2's old rules, the greedy bot cleared 7 targets with one slide through a 4-round cascade; that was the reason Level 2 first moved to in-place refill and then to sand flow.
- Numbers must be re-measured after any change to §5.1–§5.7 (I11).

### 5.11 Hints and onboarding

The prototype has two kinds of help: a collapsible **rules panel** under the board, and **toasts** (one at a time, about 1.6 s). Toasts state the problem and the next step. **[PROPOSED]** for the product: replace the rules panel with a short first-run tutorial and keep the toasts as one-shot hints.

| `HintId` | When | Text (English; the prototype shows it in Vietnamese) |
|---|---|---|
| `RowLocked` | The pressed row is locked | This row is locked by a chain. |
| `ColLocked` | The pressed column is locked | This column is locked by a chain. |
| `SpringStep` | A drag starts on a line with a spring | Spring: each notch moves 2 cells. |
| `SealOpened` | A sealed block opens | A locked block opened! |
| `SlotUnlocked` | A locked slot opens | Target unlocked! |

**First-time guidance order [PROPOSED].** The first level teaches only slide, match and the queue. Then add, one per level or two: the locked slot and the growing queue, the sealed block, the chain, ×2 and dual, the spring. The two prototype levels do not follow this order: Level 2 shows everything at once on purpose, to test whether the rules coexist.

### 5.12 Removed or rejected mechanics

These were designed, prototyped (Level 2, first version) and then **removed on purpose**. Do not bring them back without a new decision.

| Removed | Reason |
|---|---|
| Anchor ("Ô neo"): a fixed cell the line slides around | Replaced by chains, which also stop the cell from falling |
| Trapdoor: a wall that opens and closes each turn and splits a row | Not in the filtered list |
| Stone blocks, and "overflow turns the oldest queue unit into a stone with 3 hearts" | Overflow is now a plain loss. No stone exists |
| Gift box (hidden colour) | Replaced by the sealed block, which keeps its colour visible |
| Chameleon (changes colour every turn) | Not in the filtered list |
| Junk block (sends a junk unit into the queue) | Not in the filtered list |
| Mixed-colour targets | Not in the filtered list |
| **Colour-keyed chain** (only a match of one colour breaks it) | It made the block "completely locked" and nothing in play pointed to the key (I7). The `key` field remains in the data but no level uses it |
| **Hidden recolour of new blocks** (spawn must not complete a match) | Breaks I5. Kept only in Level 1 as a known deviation |
| Free-fall gravity with an unbounded cascade in Level 2 | Gave 7-round cascades from a single slide; replaced by in-place refill and then sand flow |

---

## 6. Interfaces

The logic is a **pure module with no view code**. The public surface the view and the tests rely on (prototype: `window.SJ` / `module.exports`):

| Member | Purpose |
|---|---|
| `newState()` | A new level state: grid, slots, queue, stream pointers, counters |
| `move(state, axis, index, notches) → {ok, st, phases}` | Apply a slide to a **copy**; returns the new state and the ordered phase list (§5.4). `ok` is false for a locked line, a zero shift or a finished run |
| `preview(state, axis, index, notches) → {cells, toQueue, lose}` or `null` | The drag preview (§4.2). `null` when nothing matches |
| `lineLocked(state, axis, index)` | Whether a row or column can be slid (§5.2) |
| `lineCells(axis, index)` · `stepOf(state, axis, index)` | The cells of a line · the cells moved per notch (1 or 2) |
| `findMatches(state)` | The matched cells with their colour |
| `streamAt(column, i)` | The *i*-th colour a column will spawn (used for the next-spawn dots) |
| `clone(x)` | Deep copy (JSON) used for snapshots and undo |
| `setCfg(obj)` | Change tuning values in tests |

**Phase list.** `phases[0]` is always `shift`. Each phase carries a full snapshot of the state after it (`snap`), so the view can replay them in order without re-running logic.

| Phase | Extra data |
|---|---|
| `shift` | — |
| `clear` | `chain` (round number), `vanished` (count), `matchedCells`, `flights`, `unchained` (ids), `unsealed` (ids) |
| `complete` | `slot`, `unlocked` (slot indices), `qUnlocked` |
| `pull` | `flights` |
| `fall` | `start` (block id → start cell) |
| `end` | — (the final state has `over` = `'win'` or `'lose'`) |

A **flight** is `{color, from, to}` with `from` a cell or a queue index and `to` one of `{slot, pip}`, `{q}` (queue chair) or `{vanish: true}`.

**State shape.** `grid[r][c]` is `null` or a block `{id, kind: 'n' | 'dual', color | colors, x2?, spring?, chain?, seal?}`; `slots[i]` is `null` or `{color, count, locked}`; the rest is `queue`, `qOpen`, `seqI`, `ptr[]`, `moves`, `done`, `over`, `nextId`.

**Unity port [PROPOSED].** Keep the same split as `BilliardsSim` in OverTimer: a pure C# engine (`SlideEngine`: `BoardState`, `MoveResolver`, `SandSettler`, `ColumnStream`) with no `MonoBehaviour`, `Time` or `UnityEngine.Random`, driven by a `LevelConfig` ScriptableObject, and a view that only plays phases. Port the column streams with the same integer-only generator so a seed gives the same level in the prototype and in Unity, and test that with a stored move list and a state hash.

---

## 7. Implementation notes

- **Prototype layout.** `logic.js` (pure, no DOM), `view.js` (canvas drawing, animation, input), `shell.html` (page and rules panel), `build.py` (inlines the two scripts into one `index.html`). Level 1 is a single self-contained file with the logic and view together.
- **Animation.** The view plays the phase list one phase at a time and does not decide anything. Timings: match pulse 170 ms, a unit's flight 360 ms plus 38–40 ms stagger per unit, slot pop and slot enter 260 ms each, settle `160 + 90 × longest distance` ms, drag snap 130 ms, toast about 1.6 s, floating labels 900 ms. **[PROPOSED]** move these to `GameConfig` in Unity (I10).
- **Targets and queue during a flight.** While units fly, the slots and the queue are drawn from the state *before* the phase, and the new state appears when the flight ends, so a pip fills when its block lands, not when it leaves.
- **Preview cost.** The preview runs a full resolution on a copy at every pointer move. It is small on 7×7. **[PROPOSED]** cache it per notch count.
- **Tests (prototype).** A bot simulator (random and greedy, §5.9) and a browser test that plays real drags with the mouse and compares the resulting grid, queue and completed count with what the logic predicted for the same move (0 mismatches in the runs recorded for Level 2, including moves with a spring). A direct test of the sand flow checks that a chain block stays put, the cells under it are refilled and the grid has no empty cell.
- **Determinism.** The generator is `mulberry32`. Snapshots are JSON copies, so undo restores the stream pointers and a replay of the move list is exact (I4).

**Hazards — standing conventions**

- **Streams are memoised in the module.** `streamAt` extends a per-column array on demand and is a pure function of the seed, so it is safe to call from the view for the next-spawn dots. Do not advance a stream from anywhere but a spawn (I4).
- **Spring and line length.** With an even line length, a spring's step of 2 reaches `k = 0` at half a revolution and the move is refused as a zero shift. Level 2 has length 7 (odd) so it does not happen. Keep spring lines odd or handle it.
- **Row and column chains vs gravity.** A `row` or `col` chain block is carried by slides along its free axis but is immovable for the settle. Do not treat "locked" and "immovable" as the same thing.
- **Holes.** A loss is reported before the final settle, so a lost state may contain empty cells. Do not run the grid-full assertion on a lost state (I9).
- **Cascade round numbering.** Round numbers restart at 1 on every slide. The vanish rule (I6) depends on it, so a refactor that merges rounds across slides would change the rule.

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
| **Target / Slot** | A request for 3 units of one colour. The `sequence` is the list of targets; up to 3 are on screen as slots |
| **Pip** | One of the 3 places in a slot |
| **Queue** | The waiting row for units no slot accepts. Holds at most `qOpen` units |
| **`qOpen`** | The number of queue chairs currently usable (never above `queueMax`) |
| **Pull** | Moving queue units into slots after a completion |
| **Vanish** | A cascade unit that no slot accepts disappears (I6) |
| **Overflow** | The queue holding more than `qOpen` units at the overflow check; the only loss |
| **Settle** | Falling, sand flow and refill after blocks are consumed (§5.5) |
| **Sand flow** | A block diagonally sliding past a chain block instead of stacking on it |
| **Stream** | A column's fixed, seeded list of colours that it spawns, one per refill |
| **Next-spawn dot** | The small block above a column showing a colour its stream will spawn next |
| **Chain** | A block that cannot be consumed or settled, and that locks its row, its column or both; removed by touching matches |
| **Sealed block** | A block that cannot match until enough cells around it are cleared |
| **Spring block** | A block that makes its line move 2 cells per notch |
| **×2 block** | A block that gives 2 units when consumed |
| **Dual block** | A block that counts as two colours |
| **Locked slot** | A target that accepts nothing until its neighbour slot completes |
| **Preview** | The outline and label shown while dragging (§4.2) |
| **Greedy bot / Random bot** | The balance tools in §5.9 |
| **`EndReason`** | `Win` or `QueueOverflow` |
