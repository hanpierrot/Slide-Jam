# AGENTS.md: Instructions for AI Agents

This file is the operating manual for AI coding agents (Claude Code, Codex, Cursor, Copilot, etc.) working in the **Slide Jam** Unity project. **`DOCS/DESIGN.md` is the project reference** (what the game is, the invariants, the rules, the numbers and the interfaces). This file assumes you will read the parts of `DESIGN.md` you need; it tells you *how* to work here.

The game rules were designed and measured in a throwaway HTML prototype (three levels, three boosters, a fruit-garden skin). **That prototype is not part of this repository.** This repository is the Unity project; the rules in `DESIGN.md` are what you implement. Anything marked **[PROPOSED]** is a plan that has not been built yet. The **theme** (`DESIGN.md` §9) changes display names and art only: code keeps the neutral engine terms of `DESIGN.md` §8.

---

## 1. Ground rules

These rules are not optional. A puzzle game breaks quietly: a changed refill order, a rule that depends on hidden randomness or a preview that disagrees with the real move doesn't crash, it just makes a level unfair or unwinnable.

**You MUST:**

- **Keep `DOCS/DESIGN.md` true.** If your change makes any statement in it false (a rule, a number, a table, a level, an interface), update it **in the same commit**. Say in your final report which sections you changed, or why none needed it.
- Read `DESIGN.md` §1–§2 (pitch, invariants) before your first change, then only the sections for the area you touch.
- Respect the invariants I1–I11 (`DESIGN.md` §2). If a request would break one, say so and stop. Don't work around it.
- Keep the engine **pure and deterministic** (I4): no `UnityEngine` types, no `MonoBehaviour`, no `Time`, no `UnityEngine.Random`, no `System.Random`, no `DateTime`, no hidden or static mutable state. The only randomness is the seeded column streams (§4), built with integer-only arithmetic.
- Keep the view **dumb**: it plays the phase list the engine returns and decides nothing. The drag preview calls the real move on a copy; never write a second implementation of a rule for the preview or the animation (I5).
- Keep tuning values in data (I10): `GameConfig` and `LevelConfig` assets. No literals for numbers that appear in `DESIGN.md` §5.
- Keep **display names in a text table**, not in code. Identifiers, tests, commits and docs use the terms of `DESIGN.md` §8 (`Queue`, `Vault`, `Chain`, `SealedBlock`, `Hammer`...), never the theme names (Khay chờ, Hầm trữ, Dây leo, ...).
- Keep boosters inside the rules of `DESIGN.md` §5.9: a booster is not a move, never advances a column stream, never puts a unit in the queue, and its effect is part of every snapshot, so undo takes the effect back. **A used booster is never refunded**, by undo or otherwise (§5.7) (I3, I4, I8).
- **After any change to a rule, a number or a level, check that the levels are still solvable** (I11) and say how you checked. Label every claim about difficulty as arithmetic, a simulation or an observed run (there is no player playtest data yet).
- State assumptions and uncertainties. Ask before acting when the request is ambiguous.
- Work on a branch created from an up-to-date `main`. Never commit to `main`.
- Before every commit: the checks in §7 pass and the diff shows only the files you meant to change.
- Commit `.meta` files together with their assets, and never hand-edit a `.unity`, `.prefab` or `.asset` file when a script or the Editor can do it.

**You MUST NOT, unless the user explicitly asks for that specific action in this conversation:**

- Push, open PRs, merge into `main`, delete branches, rebase shared branches or force-push.
- Change a **[LOCKED]** item: an invariant (I1–I11) or the resolution order in `DESIGN.md` §5.4.
- Let a booster add a unit to the queue, advance a column stream or make a random choice (I3, I4).
- Choose the fruit art, the booster prices or the number of booster uses on your own: `DESIGN.md` §9 lists the fruit set (its assets already exist) and §5.9 says prices are product settings.
- Put a number other than 3 to 5 in a target's `cap`, or a fixed 3 anywhere in code: `cap` is per target (`DESIGN.md` §5.1).
- Change how a column stream is built (the seed formula, the generator, the order in which colours are drawn). Every existing seed would produce a different level and every recorded move list and solution would break.
- Change the initial-grid generation or a level's special block positions without checking that the level is still solvable and showing how.
- Add randomness during play. A new random effect needs a stream, a seed rule and a place in `DESIGN.md` first (I4).
- Make the view or the preview silently change something the player has already seen (I5): a spawn colour, a queue order, a target.
- Rename, reorder or remove an `EndReason` member or a `TutorialId`. Saves and recorded runs depend on them (append only).
- Disable, skip or weaken a test, a simulation check or an assertion to make something pass.
- Change the Unity version, the packages in `Packages/manifest.json` or Project Settings.
- Commit generated or machine-specific files (`Library/`, `Temp/`, `Logs/`, `UserSettings/`, `obj/`, build output, IDE settings).
- Make a release build or upload a build.

Read-only actions are always fine: `git status/diff/log/show`, reading files, running the Edit Mode tests.

## 2. Setup

| Item | Value |
|---|---|
| Unity | **2022.3.62f3** (LTS), recorded in `ProjectSettings/ProjectVersion.txt` |
| Template | 2D (`com.unity.feature.2d` 2.0.1), built-in render pipeline |
| Input | the classic Input Manager (`activeInputHandler: 0`); the Input System package is not installed |
| Text | TextMeshPro 3.0.7 · uGUI 1.0.0 |
| Tests | Unity Test Framework 1.1.33 (NUnit) |
| IDE | Rider 3.0.36 or Visual Studio 2.0.22 packages |
| Platforms | target iOS and Android, portrait only (default orientation Portrait; the other auto-rotate options are off) |

If a Unity MCP or the Unity CLI is connected, prefer it for inspecting scenes, prefabs and the Console. Check your tool list for the exact tools. After editing scripts: refresh, wait for the recompile, then read the Console.

## 3. How a run is checked

You normally check the game **without a person at the Editor**: the engine is plain C#, so the Edit Mode tests cover the rules. If you cannot run Unity (no Editor, no CLI, no MCP), say so and list which checks you could not run. Don't fall back to reasoning as if you had tested it.

## 4. How this project works (read before writing code)

| Piece | Lives in | Consequence |
|---|---|---|
| Rules and state | the pure engine assembly (`SlideEngine`) | no Unity types, no clock, no random source; one entry point `MoveResolver.Move(state, axis, index, notches)` that returns a **new** state and a phase list; the old state is never mutated |
| Determinism | seed + per-column streams | stream `c` is `mulberry32(seed + (c + 1) × 7919)` in `uint` arithmetic; a column's *i*-th spawn is its *i*-th stream value; only a spawn advances a stream |
| Resolution | `DESIGN.md` §5.4 | `shift` → `clear` → `complete` → `pull` → overflow check → win check → `fall`, repeated until no match; round 1 is the player's slide, later rounds are cascades |
| Settle | `DESIGN.md` §5.5 | falling plus sand flow around chain blocks; after a settle the board is full (I9) |
| View | `MonoBehaviour`s outside the engine assembly | plays phases in order, animates between snapshots, handles drag, never decides a rule |
| Snapshots | value copies of `BoardState` | undo restores a snapshot **including the stream pointers**, so the same move after an undo gives the same result |
| Tuning values | `GameConfig` / `LevelConfig` ScriptableObjects | numbers in `DESIGN.md` §5 are read from data; a test may build a config in code |
| Level content | `LevelConfig` | seed, target sequence (each target `{color, cap}`, `cap` 3 to 5, any number of targets), special block cells, `queueMax`, `qOpenStart`, `qGrow`; the grid never starts with a match |
| Boosters | the engine (`MoveResolver.Hammer`, `SwapTarget`, `Stash`), `DESIGN.md` §5.9 | state carries `Seq`, `Pre` and `Vault`; the multiset of `Seq` never changes; the vault is outside the loss rule; a used booster is not refunded |
| Theme and tutorial | a text table, art and a tutorial data asset in the view | display names only (`DESIGN.md` §9); the fruit set is cherry, blueberry, pear, orange, plum; tutorial steps are shown once, from save data (`DESIGN.md` §5.8) |

Invariants and the reasons behind them: `DESIGN.md` §2.

## 5. Repository map

Current state: a fresh Unity project. Only the folders exist; there is no script yet.

```
Assets/
  Art/  Audio/  Design/  Resources/  Scenes/  Scripts/     (empty except Scenes/SampleScene)
Packages/manifest.json      packages (do not change without being asked)
ProjectSettings/            Unity settings (do not change without being asked)
DOCS/DESIGN.md              project reference
AGENTS.md  CLAUDE.md        this manual, Claude entry point
```

**[PROPOSED]** layout once code exists (names may change; fix this section when they do):

```
Assets/Scripts/
  Engine/                   assembly SlideJam.Engine, "No Engine References" ticked, pure C#
    BoardState.cs  MoveResolver.cs  SandSettler.cs  ColumnStream.cs  Phase*.cs
  Game/                     assembly SlideJam.Game: view, input, animation, UI (references Engine)
Assets/Design/              GameConfig.asset, Levels/LevelConfig_*.asset
Assets/Tests/
  EditMode/                 assembly SlideJam.Tests.EditMode: rules, invariants, determinism
  PlayMode/                 assembly SlideJam.Tests.PlayMode: a real drag against the engine's prediction
```

## 6. Recipes

### Run the checks

Prefer the Test Runner window or the Unity MCP. From a shell, with the Editor path of **2022.3.62f3** on your machine:

```bash
"<UnityEditorPath>/Unity.exe" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Temp/editmode.xml -logFile Temp/editmode.log
```

Read the result file and the log, don't assume. Close the Editor first if it has the project open (Unity refuses a second instance on the same project).

### Change a rule

1. Find the rule in `DESIGN.md` §5. If it touches the resolution order (§5.4), the settle (§5.5) or an invariant, **stop and ask**: those are LOCKED.
2. Change the engine first. Keep it pure.
3. Make the view play any new phase data. Add the new data to the phase's fields (`DESIGN.md` §6.1), never to a global.
4. Run the checks (§7), update `DESIGN.md` §5.4 or §5.6 and the tutorial text if it changes.

### Tune a number

1. Find the field in `DESIGN.md` §5.1. Change it in the config asset only (I10).
2. Check that the level can still be won (I11). A change to `qOpenStart`, `queueMax`, the number or the `cap` of the targets or the number of special blocks changes how hard it is.
3. Update the number in `DESIGN.md`. If a rule's text mentions the number, update that too.

### Add a mechanic

1. It needs: a data field on a block or a slot, a rule in the resolution order, a way out for the player (I7), a drawing, a tutorial step that fires the first time a level contains it (`DESIGN.md` §5.8), and a row in `DESIGN.md` §5.6 and the glossary.
2. Test it alone in a small level before combining it with others.

### Add or change a booster

1. It must follow §5.9: not a move, no stream advance, no unit into the queue, deterministic ties, one atomic resolution, effect in the snapshot, no refund after use.
2. Add its phase data to `DESIGN.md` §6.1, its row to §5.9 and the glossary, a tutorial step shown the first time it is offered (§5.8) and a booster-invariant test (no hole, `Seq` multiset unchanged, no stream pointer moved, undo restores the board and does not refund).

### Change the theme, the art or a display string

Edit the text table and the view only. Do not rename engine identifiers. Keep the five fruits different in **silhouette** as well as hue and re-check them with a simulated colour-vision deficiency (`DESIGN.md` §9) when an icon changes colour or shape. Do not switch the fruit set without a decision from the user.

### Add or change a level

1. A level is a `LevelConfig`: seed, target sequence (any number of targets, each with a `cap` from 3 to 5), `queueMax`, `qOpenStart`, `qGrow`, optional `lockedSlotIndex` and a list of special blocks with cells.
2. The initial grid must have no match (the generator re-rolls plain blocks). Check that no special block overlaps another.
3. Show that the level can be won (I11) and say how you checked. A level nobody can win is not shippable.
4. Check that every lock has an exit in this level (I7): a chain can be touched by a match, a sealed block has enough neighbours that can be cleared, a locked slot has a neighbour that can complete.

### Change settle, refill or the streams

These are the riskiest changes. Re-read I4, I5 and I9 first. After the change: the board is full after every non-terminal move in a test run (a check counts holes and expects 0), the next-spawn dots equal the real spawns, and undo followed by the same move gives the same result.

### Change the view or the input

The view may change freely as long as it only plays phases. Check the drag at both ends: a locked line (red flash), a spring line (2 cells per notch) and a drag that ends at notch 0 (no move). Keep touch working: use one code path for mouse and touch (the Input Manager's pointer or touch), no hover-only behaviour.

### Work in scenes, prefabs and assets

- Don't edit serialized YAML by hand. Use the Editor, a Unity MCP or an editor script.
- Keep references to config assets in serialized fields, not in `Resources.Load` strings, unless a `Resources/` load is the documented choice.
- A script file's name equals its main class. Don't rename a `MonoBehaviour` or `ScriptableObject` that is already used in an asset without checking the `.meta` GUID reference survives.

## 7. Verifying your work

Your change is not done until all of these hold:

1. The project compiles with no errors and no new warnings in the Console, and the Edit Mode tests pass.
2. **Invariant checks (tests):** no empty cell after a non-terminal move (I9); same seed and moves give the same state hash (I4); preview and result agree (I5); undo then the same move gives the same result. After any **booster** use: no hole, the multiset of `Seq` unchanged, no stream pointer moved, no unit added to the queue, and undo restores the board state exactly and does not refund the use. A target with `cap` 3, 4 and 5 completes at exactly its own `cap`.
3. **Play Mode check [PROPOSED until it exists]:** a real drag on the board gives the same grid, queue and completed count as the engine predicted for that move, with no errors in the Console.
4. **Solvability:** if the rules, numbers or levels changed, each level touched can still be won (I11) and the report says how this was checked.
5. No new randomness, no literals for tuning values, no second implementation of a rule in the view.
6. `DESIGN.md` updated, and the report lists the sections changed.
7. The diff shows only intended files, with their `.meta` files, and nothing from `Library/`, `Temp/`, `Logs/` or `UserSettings/`.

## 8. Workflow

- **Branches:** one per feature, from `main` after the previous one merged. Never stack feature branches. Merge `main` into your branch before finishing.
- **Commits:** small, focused, the message says what and why. Examples: `engine: cascade surplus vanishes`, `level2: remove colour-keyed chain`, `docs: update §5.1 after changing a target cap`.
- **Final report:** what changed · files changed · checks run and their results · deviations and why · problems found but not fixed · assumptions and defaults used · `DESIGN.md` sections updated · open questions.
- Don't fix unrelated bugs inside a feature change. Note them.

## 9. Troubleshooting

| Symptom | Cause / fix |
|---|---|
| The next-spawn dots show a colour that doesn't appear | A spawn is being recoloured after the dots were computed, or a stream is advanced somewhere other than a spawn |
| An undo followed by the same move gives a different result | The snapshot doesn't include the stream pointers (`ptr`), or something outside the snapshot holds state (a static field, a cache) |
| An empty cell stays in the board after a move | The settle's sand rule doesn't reach it (§5.5), or you are looking at a lost state (a loss is reported before the final settle) |
| A cascade's leftovers went to the queue | The round number is wrong (it must restart at 1 on each slide and be 2 or more for cascades), or the unit was placed before the check (I6) |
| A move with a spring does nothing | The shift is zero (an even line length with step 2 at half a revolution). Keep spring lines odd or handle the case |
| The row doesn't move but the lock looks cleared | The chain was removed during the move; the preview ran on the old state. Re-run the preview after the state changes |
| A chain block moves when a column slides | Expected for a `row` or `col` chain along its free axis. "Locked" is not "immovable"; only the settle never moves it |
| A seed gives a different level in the editor and in a test | Something other than `uint` arithmetic is in the generator (a `float`, `int` overflow checks, `System.Random`), or the stream order changed |
| The Play Mode check reports mismatches | The view applied a different shift than the engine (check the notch rounding and the spring step), or the check read the state before the animation finished |
| A level cannot be won | A lock has no exit (I7) or the level is too hard. Play a game by hand in a test, print the board and check each lock |
| A booster changed the next-spawn dots | It advanced a column stream. Only a spawn may do that (I4) |
| After Swap target the next strip shows the wrong counts | `Seq` and `Pre` were not spliced together (`DESIGN.md` §5.9) |
| A stored unit never leaves the vault | No target of that colour is left (harmless), or `pull` is not scanning the vault before the queue (§5.4) |
| Undo after a booster leaves vault units behind | The snapshot misses `Vault`, `Seq` or `Pre` |
| Undo gave a booster back | The booster count was put in the snapshot. It is not part of the state (`DESIGN.md` §5.7) |
| A target completes at 3 units whatever its `cap` | A fixed 3 is used instead of the slot's own `Cap` (`DESIGN.md` §5.1) |
| A theme name appears in code or a test | Display names belong in the text table; use the engine term from `DESIGN.md` §8 |
| A `ScriptableObject` edit doesn't show in a test | The asset isn't saved (`AssetDatabase.SaveAssets`), or the test builds its own config |
| Unity says the project is already open | Another Editor instance holds it. Close it before running batch mode |
