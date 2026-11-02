# AGENTS.md: Instructions for AI Agents

This file is the operating manual for AI coding agents (Claude Code, Codex, Cursor, Copilot, etc.) working in the **Slide Jam** repository. **`DESIGN.md` is the project reference** (what the game is, the invariants, the rules, the live numbers and the interfaces). This file assumes you will read the parts of `DESIGN.md` you need; it tells you *how* to work here.

The project has two phases. **Phase A (today):** HTML/canvas prototypes with a pure-JavaScript logic module. **Phase B [PROPOSED]:** a Unity (2D) port with a pure C# engine. Sections marked **[PROPOSED]** describe Phase B and have not been tried.

---

## 1. Ground rules

These rules are not optional. A puzzle game breaks quietly: a changed refill order, a rule that depends on hidden randomness or a preview that disagrees with the real move doesn't crash, it just makes a level unfair or unwinnable.

**You MUST:**

- **Keep `DESIGN.md` true.** If your change makes any statement in it false (a rule, a number, a table, a measured result, a level, an interface, a removed mechanic), update it **in the same commit**. Say in your final report which sections you changed, or why none needed it.
- Read `DESIGN.md` §1–§2 (pitch, invariants) before your first change, then only the sections for the area you touch.
- Respect the invariants I1–I11 (`DESIGN.md` §2). If a request would break one, say so and stop. Don't work around it.
- Keep the logic **pure and deterministic** (I4): no DOM, no `Date`, no `Math.random`, no engine random calls, no hidden state, in the logic. The only randomness is the seeded column streams (§4).
- Keep the view **dumb**: it plays the phase list the logic returns and decides nothing. The drag preview calls the real move on a copy; never write a second implementation of a rule for the preview or the animation (I5).
- Keep tuning values in data (I10): the constants and `CFG` at the top of the logic (Phase A), `LevelConfig` / `GameConfig` (Phase B). No literals for numbers that appear in `DESIGN.md` §5.
- **Re-measure balance after any change to a rule, a number or a level** (§7), and update the table in `DESIGN.md` §5.10. Label every balance claim as arithmetic, a bot simulation or an observed run (there is no player playtest data yet).
- State assumptions and uncertainties. Ask before acting when the request is ambiguous.
- Work on a branch created from an up-to-date `main`. Never commit to `main`. **(Team standard carried over from OverTimer; confirm it still applies to this repository.)**
- Before every commit: the checks in §7 pass and the diff shows only the files you meant to change.

**You MUST NOT, unless the user explicitly asks for that specific action in this conversation:**

- Push, open PRs, merge into `main`, delete branches, rebase shared branches or force-push.
- Change a **[LOCKED]** item: an invariant (I1–I11) or the resolution order in `DESIGN.md` §5.4.
- Bring back a mechanic from `DESIGN.md` §5.12 (anchor, trapdoor, stone, gift box, chameleon, junk block, mixed targets, colour-keyed chain, hidden recolour, free-fall gravity with unbounded cascade, hearts) without a new decision from the user.
- Change how a column stream is built (the seed formula, the generator, the order in which colours are drawn). Every existing seed would produce a different level and every recorded move list and solution would break.
- Change the initial-grid generation or a level's special block positions without re-running the simulations and showing the result.
- Add randomness during play. A new random effect needs a stream, a seed rule and a place in `DESIGN.md` first (I4).
- Make the view or the preview silently change something the player has already seen (I5): a spawn colour, a queue order, a target.
- Rename, reorder or remove an `EndReason` member or a `HintId`. Saves and recorded runs depend on them (append only).
- Disable, skip or weaken a test, a simulation check or an assertion to make something pass.
- Edit a generated file (the single `index.html` built from sources) instead of its sources.
- Commit generated or machine-specific files (`node_modules/`, browser caches, build output, editor settings).
- Change the Unity version, packages or Project Settings once the Unity project exists **[PROPOSED]**.
- Make a release build or upload a build.

Read-only actions are always fine: `git status/diff/log/show`, reading files, running the headless simulation and the browser check.

## 2. Setup

**Phase A (prototype):**

1. **Node.js** (22 was used) for the logic tests and the bot simulation.
2. **Python 3** for the build script that inlines the scripts into one HTML page.
3. **Playwright** with Chromium for the browser check (`require('playwright')`). Don't run `playwright install` if a Chromium is already provided by the environment.
4. No package manager state is needed otherwise; the prototype has no dependencies and the page loads nothing from the network.

**Phase B [PROPOSED]:** Unity 2D with a version chosen when the project is created (record it here and in `DESIGN.md`). The engine code is a pure C# assembly so it can be tested without the Editor.

## 3. How a run is checked

You usually run the game **headless**, not by hand. If a tool says no browser is available, say so and list which checks you could not run. Don't fall back to reasoning as if you had tested it.

## 4. How this project works (read before writing code)

| Piece | Lives in | Consequence |
|---|---|---|
| Rules and state | the pure logic module (`logic.js`) | no DOM, no clock, no `Math.random`; one entry point `move(state, axis, index, notches)` that returns a **new** state and a phase list; the old state is never mutated |
| Determinism | seed + per-column streams | stream `c` is `mulberry32(SEED + (c + 1) × 7919)`; a column's *i*-th spawn is its *i*-th stream value; only a spawn advances a stream |
| Resolution | `DESIGN.md` §5.4 | `shift` → `clear` → `complete` → `pull` → overflow check → win check → `fall`, repeated until no match; round 1 is the player's slide, later rounds are cascades |
| Settle | `DESIGN.md` §5.5 | falling plus sand flow around chain blocks; after a settle the board is full (I9) |
| View | `view.js` (canvas) | plays phases in order, animates between snapshots, handles drag, never decides a rule |
| Snapshots | JSON copies | undo restores a snapshot **including the stream pointers**, so the same move after an undo gives the same result |
| Tuning values | constants + `CFG` (Phase A), `LevelConfig` / `GameConfig` (Phase B) | numbers in `DESIGN.md` §5 are read from data; a test may override them with `setCfg` |
| Level content | `newState()` in the logic | seed, target sequence, special block cells; the grid never starts with a match |

Invariants and the reasons behind them: `DESIGN.md` §2.

## 5. Repository map

Phase A as built in the working session (names may differ in your clone; fix this section if they do):

```
index.html                     Level 1: single self-contained file (logic and view together)
l2/
  logic.js                     Level 2 pure logic (the module `SJ`)
  view.js                      Level 2 canvas view, animation and input
  shell.html                   page frame, rules and legend panels; contains /*LOGIC*/ and /*VIEW*/ markers
  build.py                     inlines logic.js and view.js into l2/index.html (generated)
  index.html                   GENERATED. Do not edit by hand
tests/
  sim3.js                      bot simulation (random and greedy) for Level 2
  sim.js                       bot simulation for Level 1
  verify3.js                   browser check: real drags vs the logic's prediction
DESIGN.md  AGENTS.md  CLAUDE.md    reference, this manual, Claude entry point
```

**[PROPOSED]** Phase B: a pure C# assembly (`SlideEngine`: `BoardState`, `MoveResolver`, `SandSettler`, `ColumnStream`), a `LevelConfig` ScriptableObject per level, a view layer that plays phases, and Edit Mode tests that replay a stored move list and compare a state hash.

## 6. Recipes

### Run the checks

```bash
cd l2 && python3 build.py                 # rebuild the page after editing logic.js / view.js / shell.html
cd ../tests
node sim3.js                              # Level 2 bots (random N=300, greedy N=40); prints win/lose counts and slide averages
node sim3.js '{"qOpen":5}'                # same, with CFG overridden for the run (also: sealNeed)
node verify3.js                           # browser drags vs logic; expect "errors":[] and "mismatches":0
```

Read the output, don't assume. For Level 1: `node sim.js`.

### Change a rule

1. Find the rule in `DESIGN.md` §5. If it touches the resolution order (§5.4), the settle (§5.5) or an invariant, **stop and ask**: those are LOCKED.
2. Change the logic first. Keep it pure.
3. Make the view play any new phase data. Add the new data to the phase's fields (§6.1 of `DESIGN.md`), never to a global.
4. Run the checks (§7), re-measure balance, update `DESIGN.md` §5.4/§5.7 and §5.10 and the legend panel.

### Tune a number

1. Find the field in `DESIGN.md` §5.1 or §5.6. Change it in data only (I10).
2. Re-run the bots. A change to `qOpenStart`, the target sequence or the number of special blocks moves the random-play win rate; record the before and after.
3. Update the number and the measured table in `DESIGN.md`. If a rule's text mentions the number, update that too.

### Add a mechanic

1. Check `DESIGN.md` §5.12 first: it may have been rejected.
2. It needs: a data field on a block or a slot, a rule in the resolution order, a way out for the player (I7), a drawing, a legend line, a hint if it can refuse input, bot coverage, and a row in `DESIGN.md` §5.7 and the glossary.
3. Test it alone in a small level before combining it with others.
4. Say in the report whether the bot sees any change in win rates.

### Add or change a level

1. A level is a seed, a target sequence, `qOpenStart`, optional `lockedSlotIndex` and a list of special blocks with cells (§5.6).
2. The initial grid must have no match (the generator re-rolls plain blocks). Check that no special block overlaps another.
3. Run both bots and write the result in `DESIGN.md` §5.10 (I11). A level with 0 wins from the greedy bot is not shippable.
4. Check that every lock has an exit in this level (I7): a chain can be touched by a match, a sealed block has enough neighbours that can be cleared, a locked slot has a neighbour that can complete.

### Change settle, refill or the streams

These are the riskiest changes. Re-read I4, I5 and I9 first. After the change: the board is full after every non-terminal move in a bot run (the simulation counts holes and expects 0), the next-spawn dots equal the real spawns, and undo followed by the same move gives the same result.

### Change the view or the input

The view may change freely as long as it only plays phases. Check the drag at both ends: a locked line (flash and toast), a spring line (2 cells per notch) and a drag that ends at notch 0 (no move). Keep touch working: pointer events with capture, no hover-only behaviour.

### Edit the built page

Never edit a generated `index.html`. Edit the sources and rebuild.

## 7. Verifying your work

Your change is not done until all of these hold:

1. The logic runs headless with no exceptions, and the bot simulation finishes with no timeouts.
2. **Invariant checks:** no empty cell after a non-terminal move (I9); same seed and moves give the same state (I4); preview and result agree (I5).
3. **Browser check:** `node verify3.js` reports no console errors and `mismatches: 0`.
4. **Balance:** the numbers in `DESIGN.md` §5.10 are re-measured if the rules, numbers or levels changed, and the doc matches the measured output.
5. The generated page was rebuilt, and no generated file was edited by hand.
6. No new randomness, no literals for tuning values, no second implementation of a rule in the view.
7. `DESIGN.md` updated, and the report lists the sections changed.
8. The diff shows only intended files.

## 8. Workflow

- **Branches:** one per feature, from `main` after the previous one merged. Never stack feature branches. Merge `main` into your branch before finishing. *(Carried over from OverTimer; adjust to this repository's habits.)*
- **Commits:** small, focused, the message says what and why. Examples: `rules: cascade surplus vanishes`, `level2: remove colour-keyed chain`, `docs: update §5.10 after rebalance`.
- **Final report:** what changed · files changed · checks run and their results (with the measured numbers) · deviations and why · problems found but not fixed · assumptions and defaults used · `DESIGN.md` sections updated · open questions.
- Don't fix unrelated bugs inside a feature change. Note them.

## 9. Troubleshooting

| Symptom | Cause / fix |
|---|---|
| The next-spawn dots show a colour that doesn't appear | A spawn is being recoloured after the dots were computed (the Level 1 anti-match recolour, an I5 deviation), or a stream is advanced somewhere other than a spawn |
| An undo followed by the same move gives a different result | The snapshot doesn't include the stream pointers (`ptr`), or something outside the snapshot holds state |
| An empty cell stays in the board after a move | The settle's sand rule doesn't reach it (§5.5), or you are looking at a lost state (a loss is reported before the final settle) |
| A cascade's leftovers went to the queue | The round number is wrong (it must restart at 1 on each slide and be 2 or more for cascades), or the unit was placed before the check (I6) |
| A move with a spring does nothing | The shift is zero (an even line length with step 2 at half a revolution). Keep spring lines odd or handle the case |
| The row doesn't move but the lock looks cleared | The chain was removed during the move; the preview ran on the old state. Re-run the preview after the state changes |
| A chain block moves when a column slides | Expected for a `row` or `col` chain along its free axis. "Locked" is not "immovable"; only the settle never moves it |
| The browser check reports mismatches | The view applied a different shift than the logic (check the notch rounding and the spring step), or the check read the state before the animation finished |
| The bot never wins | The level is too hard or a lock has no exit (I7). Run a game by hand in the logic, print the board and check each lock |
| The page doesn't change after an edit | You edited a source but didn't run `build.py`, or you edited the generated file and it was overwritten |
| Balance numbers in the doc don't match the output | Re-run the bots; the doc must be updated by the person who changed the rule (§1) |
