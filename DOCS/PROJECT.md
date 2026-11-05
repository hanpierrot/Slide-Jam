# Slide Jam — Project Reference

> ## ⚠️ MAINTENANCE RULE (mandatory for every human and agent)
> **This file must stay true.** Any change to code, `GameConfig` / `LevelConfig` assets, prefabs, the scene, Project Settings, packages or a design decision that makes a statement here false **must update this file in the same commit**. A commit that changes behaviour without updating `PROJECT.md` is incomplete and should not be merged.
> Before finishing any task, run the **update checklist in §17** and bump the snapshot line below.

| Snapshot | Value |
|---|---|
| Last verified against the repository | **2026-11-05**: `main` @ `e56dae2` (`Merge branch 'BoardCore'`), working tree clean before this docs edit (`git status`). The scripts and the scene were read in earlier sessions; the `Block` and `Board` prefabs are committed (`git ls-files`) but their contents were not re-read. The user ran the 38 Edit Mode tests and the Phase 1 view in Play mode and reported no errors (told in chat; no result file; the agent did not run Unity). The pool, the Edit Mode preview and the Edit Mode camera (S24–S26) are still unconfirmed by the user (§16) |
| State of the game | **Phase 1 is complete and merged. Phase 2 (targets, queue, win and loss) is planned and its decisions are fixed (S27–S34); no Phase 2 code exists yet.** Engine of Phase 0 and 1 done, and the Phase 1 view built: a drag slides a line, `Move` runs, the phases play with animation (snap, `Clear` pulse, `Fall`), and a move counter and a Restart button exist. `GameController` is the one wired root; the camera refits when the screen changes shape. Still no slot, queue, win or loss, config asset, art or audio. §6 lists what exists and what is planned |
| Branch / commit | `main` @ `e56dae2` (includes the merged `BoardCore`); local and remote branch `BoardCore` still exist; remote `https://github.com/hanpierrot/Slide-Jam.git`. Uncommitted: `DOCS/DESIGN.md` and `DOCS/PROJECT.md` (this edit). The next feature branch is cut from `main` |
| Engine | Unity **2022.3.62f3** (LTS), 2D template, built-in render pipeline, single scene |
| Design spec | `DOCS/DESIGN.md` **v0.8** (2026-11-05) is the source of truth for rules, numbers and interfaces. This file records the **project state**: setup, code map, decisions, issues and what is built |
| Prototype | A throwaway HTML prototype (three levels, three boosters, a fruit-garden skin) produced the rules. It is **not in this repository** and nothing here depends on it |

**Source-of-truth order:** *behaviour* = code and live assets → this file → plan notes. *Intent* = `DESIGN.md` (invariants and rules). When the code differs from `DESIGN.md`, that is either a bug (§12) or a design feature not built yet (§14): never record a deliberate design rule as a bug. When this file and the code disagree, the code wins: fix this file and say so in the commit.

**Tags used below:** `[verified]` read directly from a file in this repository · `[unverified]` not confirmed · `[planned]` decided but not built · **OPEN** known defect or undecided question.

Paths are relative to the repository root. Line numbers drift, so search by symbol name first.

---

## Contents

1. [Pitch and core loop](#1-pitch-and-core-loop)
2. [Invariants I1–I11: status](#2-invariants-i1i11-status)
3. [Coding conventions](#3-coding-conventions)
4. [Team, ownership and workflow](#4-team-ownership-and-workflow)
5. [Project setup](#5-project-setup)
6. [Code map](#6-code-map)
7. [Game flow and persistence](#7-game-flow-and-persistence)
8. [Input, UI and tutorial](#8-input-ui-and-tutorial)
9. [Content status: levels, boosters, theme](#9-content-status-levels-boosters-theme)
10. [Tuning values](#10-tuning-values)
11. [Tools and testing](#11-tools-and-testing)
12. [Known issues (open)](#12-known-issues-open)
13. [Fixed issues ledger](#13-fixed-issues-ledger)
14. [Roadmap](#14-roadmap)
15. [Decision log](#15-decision-log)
16. [Verification gaps](#16-verification-gaps)
17. [Update checklist](#17-update-checklist)
18. [Document history](#18-document-history)
19. [Glossary](#19-glossary)

---

## 1. Pitch and core loop

A grid of coloured blocks. You **slide one row or one column**, blocks wrap around the edge, three or more in a line **vanish on their own** and fly into the **target slots** that want their colour. What no slot wants waits in a small **queue**; if the queue overflows you lose. Obstacles (chains, sealed blocks, locked slots, a growing queue) change how you plan, and three optional **boosters** (Hammer, Swap target, Stash) help when the queue fills up. Display theme: the fruit garden "Vườn trái cây" (display strings and art only).

- Genre: single-screen turn-based puzzle, mobile portrait, one finger (drag only). Target level length 20–40 slides (design target, unmeasured).
- Full pitch, loop diagram and HUD: `DESIGN.md` §1, §3, §4.

---

## 2. Invariants I1–I11: status

The invariants are **[LOCKED]** and worded in `DESIGN.md` §2. Do not copy the wording here (two copies drift). This table records only whether the code honours each one.

| # | Short name | Status in code |
|---|---|---|
| I1 | One win, one loss | not built |
| I2 | Turn-based | not built |
| I3 | One core verb; boosters are not moves | slide is one move and a zero shift is refused (tested); the drag path runs in Play mode (no errors reported); boosters not built |
| I4 | Deterministic (seeded column streams, no hidden randomness) | partly: streams, grid generation and `Move` are pure and tested through `Hash()`; undo and replays not built |
| I5 | No lies (next-spawn dots and preview are real) | not built |
| I6 | Combos never punish (cascade surplus vanishes) | not built |
| I7 | Locks close a door, never end the run | not built |
| I8 | Resolution is atomic | partly: `Move` returns the whole phase list at once and the view ignores input while it plays (the controller disables `DragInput`); slots, win and loss do not exist yet |
| I9 | Board full after settle | tested after every `Move` in the sweep tests; the chain case is Phase 4 |
| I10 | Tuning in data | not built |
| I11 | Every shipped level is solvable | not built; the way to check it is **OPEN** (§12) |

Update this table as each invariant gets a test (§11). "Built" means a passing test exists, not that the code looks right.

---

## 3. Coding conventions

The conventions that bind every change are in `AGENTS.md` §1 and §6. Summary of the ones that came from the design, so the code starts clean:

| Rule | Why |
|---|---|
| The engine assembly has **no `UnityEngine` reference**: no `MonoBehaviour`, `Time`, `UnityEngine.Random`, `System.Random`, `DateTime`, no mutable statics | Determinism (I4), testable in Edit Mode |
| The only randomness is the column stream: `mulberry32(seed + (c + 1) × 7919)` in `uint` arithmetic (`unchecked`), no `float` | The same seed gives the same level on every platform |
| Every move and every booster returns a **new** state and a **phase list**; the old state is never mutated | Undo, preview, replays |
| The view only plays phases. The drag preview calls the real move on a copy | One implementation of each rule (I5) |
| Tuning values live in `GameConfig` / `LevelConfig`, never as literals (`cap` is per target, 3 to 5, never a fixed 3) | I10 |
| Display names live in a text table; code uses the neutral terms of `DESIGN.md` §8 | Theme can change without touching the engine |
| `EndReason` and `TutorialId` are **append-only**; never rename, reorder or remove | Saves and recorded runs depend on them |
| Never iterate a `Dictionary` or `HashSet` to decide a rule; use a fixed order; use floor-mod for negative shifts | Determinism |
| Compare Unity objects with explicit `!= null`, never `?.` / `??` | Unity's fake-null on destroyed objects |
| Rename a serialized field only with `[FormerlySerializedAs("old")]` | Keeps asset values |
| Build new things as **prefabs**; keep `SampleScene.unity` edits minimal | Single shared scene means merge conflicts |
| Wire scene references **only on `GameController`** (the composition root); the other view components receive their dependencies through `Init` and report through events, and never reference the controller | Cross-references between components on one GameObject made the setup confusing; one place to wire means one place to check |
| Commit every `.meta` with its asset; do not hand-edit scene / prefab / asset YAML | GUID references |

---

## 4. Team, ownership and workflow

- **Ownership is not recorded.** `DESIGN.md` has no ownership table and no one has told the agents who owns what. **Agents: ask who you are talking to before assigning, prioritising or estimating work.** When this is decided, record it here as a table (person, owns, notes).
- Language: the user writes Vietnamese and English; documents, identifiers and commits are English; in-game text is Vietnamese (display strings, §9).
- Workflow (details in `AGENTS.md` §8): one branch per feature from an up-to-date `main`, never commit to `main`, never stack feature branches, merge `main` back in before finishing. Agents never push, open PRs, merge or force-push unless asked in that conversation.
- Commit message style: short, says what and why, e.g. `engine: cascade surplus vanishes`, `docs: update §5.1 after changing a target cap`.

---

## 5. Project setup

### 5.1 Engine and packages (`Packages/manifest.json`) [verified]

| Package | Version | Use |
|---|---|---|
| Unity | **2022.3.62f3** (`96770f904ca7`) | `ProjectSettings/ProjectVersion.txt` |
| `com.unity.feature.2d` | 2.0.1 | 2D feature set (sprite, tilemap, animation, ...) |
| `com.unity.ugui` | 1.0.0 | UI |
| `com.unity.textmeshpro` | 3.0.7 | text |
| `com.unity.test-framework` | 1.1.33 | Edit Mode / Play Mode tests |
| `com.unity.timeline` | 1.7.7 | available, no planned use |
| `com.unity.visualscripting` | 1.9.4 | default template package, no planned use |
| `com.unity.ide.rider` / `ide.visualstudio` | 3.0.36 / 2.0.22 | IDE |
| `com.unity.collab-proxy` | 2.12.4 | default template package |
| Built-in modules | the default 2D template set (physics, physics2d, audio, ui, particlesystem, ...) | `physics` / `physics2d` are not needed by the rules |

- **Not installed:** the Input System package (the project uses the classic Input Manager, `activeInputHandler: 0`), URP, DOTween, any Unity AI or MCP package. The engine must not depend on any of them. Adding one is a package change: ask first.
- Assemblies: `SlideJam.Engine` (No Engine References, not auto-referenced), `SlideJam.Game` (references `SlideJam.Engine`, `UnityEngine.UI` and `Unity.TextMeshPro`; not auto-referenced) and `SlideJam.Tests.EditMode`. The PlayMode tests are planned (§6).

### 5.2 Repository

- Remote: `https://github.com/hanpierrot/Slide-Jam.git`. Branches: `main` and local `BoardCore`. Commits: `f566452 Init project` (`main`), `6bfb37b Phase 0: Setup` and `706d99c Phase 1 - Engine done` (`BoardCore`).
- `.gitattributes`: Git LFS rules for images, audio, video, fonts and archives (74 `lfs` lines); Unity YAML files use `merge=unityyamlmerge eol=lf`. `.gitignore` is the standard Unity one.
- **The `unityyamlmerge` merge driver is not defined in this clone's git config** (`git config --get merge.unityyamlmerge.driver` prints nothing). Unless it is set in a global gitconfig, the `.gitattributes` rule falls back to a text merge. Set it up before the first scene or prefab merge. **OPEN** (§12).
- Agent instructions: `AGENTS.md` (all agents), `CLAUDE.md` (Claude; imports `AGENTS.md`). There is no `.claude/` folder yet.
- Docs: `DOCS/DESIGN.md`, `DOCS/PROJECT.md` (this file).

### 5.3 Assets layout [verified]

| Path | Contents today |
|---|---|
| `Assets/Scenes/SampleScene.unity` | the only scene, in the build list (index 0) |
| `Assets/Art/Sprites/Square.png` | the placeholder sprite used for blocks, the board background and the mask (from *Create > 2D > Sprites > Square*) |
| `Assets/Prefabs/Block.prefab` | the block view: a `SpriteRenderer` (the `Square` sprite, Mask Interaction Visible Inside Mask) and `BlockView`, scale 0.92. Its pre-made, inactive copies sit under `Board/Blocks` in the scene **[recorded from delivered setup steps; not yet read in the repository]** |
| `Assets/Prefabs/Board.prefab` | the board root made by the user: `Board` (`BoardView`) with `Background`, `Mask` and `Blocks`; the scene holds one instance of it, with the preview size and the pre-made blocks as scene overrides |
| `Assets/Audio/`, `Design/`, `Resources/` | empty (only `.meta` files) |
| `Assets/TextMesh Pro/` | TMP Essentials, imported when the first TMP object was created |
| `Assets/Scripts/Game/` | assembly `SlideJam.Game` (§6) |
| `Assets/Scripts/Engine/` | assembly `SlideJam.Engine`: `Rng/`, `Board/`, `Moves/` (§6) |
| `Assets/Tests/EditMode/` | assembly `SlideJam.Tests.EditMode`: `Rng/`, `Board/`, `Moves/` (§11) |

Planned layout (names may change; update §6 when they do):

```
Assets/Scripts/Engine/   assembly SlideJam.Engine (pure C#)
Assets/Scripts/Game/     assembly SlideJam.Game (view, input, UI, config ScriptableObjects)
Assets/Design/           GameConfig.asset, Levels/LevelConfig_*.asset
Assets/Tests/EditMode/   assembly SlideJam.Tests.EditMode
Assets/Tests/PlayMode/   assembly SlideJam.Tests.PlayMode
```

### 5.4 Project settings [verified in `ProjectSettings/ProjectSettings.asset`]

| Setting | Value |
|---|---|
| Product name · company | `Slide Jam` · `DefaultCompany` (placeholder) |
| Version · Android version code | `1.0` · 1 |
| Application identifier | `com.DefaultCompany.2DProject` (Standalone, placeholder) **OPEN** |
| Default orientation | **Portrait**; auto-rotate to Portrait only (changed 2026-11-03; the other three auto-rotate options are off) |
| Color space | 1 (Linear) |
| API compatibility level | 6 (.NET Standard 2.1) |
| Android min SDK | 22; target SDK 0 (automatic) |
| Default screen size | 1920 × 1080 (Standalone window; irrelevant on mobile) |
| Tags · layers | no tags; layers: 0 Default, 1 TransparentFX, 2 Ignore Raycast, 4 Water, 5 UI; no custom layers |
| Sorting layers | `Default` only |
| Scripting backend | not set for any platform yet |

Linear colour space on mobile needs OpenGL ES 3 / Vulkan on Android. Revisit when the first device build is made (§12).

### 5.5 Scene `SampleScene.unity` [verified in the scene file]

The single scene. Roots, with the scripts attached to each:

| Root | Contents |
|---|---|
| `Main Camera` | orthographic, solid colour background, `CameraFit` (fits the grid and refits when the screen size changes; in Edit Mode it follows the Game view while you resize it) |
| `Board` | `BoardView`; children `Background` (`SpriteRenderer`, `Square`, sorting order −1), `Mask` (`SpriteMask`, `Square`) and `Blocks` (the pre-made inactive `Block` instances that `BlockPool` adopts at `Awake`, created with the `Pre-create Blocks` context menu of `BoardView`), all placed in the scene; `BoardView` scales `Background` and `Mask` and, in Edit Mode, activates and positions the first `rows × cols` pre-made blocks as Play mode does (prefab colours), so the grid is visible without pressing Play **[recorded from delivered setup steps; not yet read in the scene file]** |
| `GameController` | `GameController` (the composition root: the only component with serialized references to the other view components), `PhasePlayer`, `DragInput`; the last two have no scene references |
| `Canvas` | Screen Space Overlay, Canvas Scaler Scale With Screen Size 360 × 640, match width; `HudView`; children `MovesText` (TMP) and `RestartButton` (OnClick → `GameController.Restart`) |
| `EventSystem` | created with the Canvas (classic input module) |

**Not built yet:** slots, queue, vault and next-spawn dots, the booster bar, the tutorial and end overlays, a `MANAGERS` root. Restart rebuilds the state in place; no `DontDestroyOnLoad`.

---

## 6. Code map

What exists is marked [built]; the rest is the plan from `DESIGN.md` §6. Update this table as files appear.

| Area | Planned contents |
|---|---|
| Engine `Rng/` [built] | `Mulberry32` (`At`, closed form), `ColumnStream` (`At`) |
| Engine `Board/` [built] | `Block`, `Axis`, `Cell`, `LevelData`, `BoardState` (`New`, `Clone`, `Hash`), `Board` (`Shift`, `ShiftAmount`, `LineLength`, `FloorMod`), `Matcher` (`Find`, `HasMatch`), `GridGenerator` and `SandSettler` (both internal; `SandSettler` is gravity only) |
| Engine `Moves/` [built] | `MoveResolver.Move`, `MoveResult`, `Phase`, `PhaseKind` (`Shift`, `Clear`, `Fall`), `FallStart` |
| Engine [planned] | slots, queue, `Seq` / `Pre` / `Vault`, complete / pull / overflow / win and undo (Phase 2), `Preview` (Phase 3), dual / ×2 / locked slot / `qGrow` / spring / sealed / chain with sand flow (Phase 4), `Hammer`, `SwapTarget`, `Stash` (Phase 5) |
| Game `SlideJam.Game` [built] | `GameController` (composition root: `Awake` calls `Init` on `PhasePlayer` and `DragInput` and subscribes to `DragInput.Dragging` / `Released`; owns the `BoardState`: `TryMove`, `Restart`, `IsBusy`; temporary level fields `_rows`, `_cols`, `_colors`, `_seed`), `BoardView` (a `BlockPool` over the pre-made `Block` objects under `Blocks`, instantiating only when the pool is short, with a warning; the editor command `Pre-create Blocks` and an Edit Mode preview of the grid (white, prefab-coloured blocks laid out by the `rows × cols` that `GameController.OnValidate` pushes); `Build`, `ShowState`, `ShowLineOffset`, `ShowBlock`, `TryGetCell`, `Matches`), `BlockView` (`Bind`, `SetScaleFactor`, `ResetScale`), `BlockPool` (plain class: `Adopt`, `Get`, `Release`, `Total`, `Instantiated`), `DragInput` + `LineDrag`, `PhasePlayer` (snap, snap back, `Clear` pulse, `Fall`), `CameraFit` (`ExecuteAlways`; `Fit`, refit in `Update` when the screen size changes; in Edit Mode an editor tick follows the Game view size and `SetPreviewSize` receives the level size from `GameController.OnValidate`), `HudView` (`ShowMoves`) |
| Game [planned] | slot and queue views, `BoosterBar`, `TutorialOverlay`, `TextTable` (display names), `SaveData` (tutorial-seen flags, booster counts), the `GameConfig` and `LevelConfig` ScriptableObjects and their conversion to engine data |
| Tests [Edit Mode built] | `Rng/Mulberry32Tests`, `Board/ColumnStreamTests` (it tests `Rng/ColumnStream` but lives in `Board/`), `Board/MatcherTests`, `Board/BoardTests`, `Board/BoardStateTests` (+ the `TestGrids` helper), `Moves/MoveResolverTests`. Planned: caps, boosters, per-mechanic rules; Play Mode: real drag vs engine prediction |

Singletons and services are **not decided**. Prefer plain objects owned by one game controller over `static` singletons; any static must be reset on scene load.

---

## 7. Game flow and persistence

- **Run lifecycle:** [built, Phase 1] `GameController.Restart` builds `BoardState.New` from temporary serialized level fields; `DragInput` raises `Dragging` and `Released` (a `LineDrag`), the controller previews the line and calls `TryMove`, which runs `MoveResolver.Move`; the new state is applied at once and `PhasePlayer` plays the phases while the controller keeps `DragInput` disabled (`IsBusy`); afterwards `BoardView.Matches` checks the screen against the state (it logs an error if they differ) and `ShowState` redraws it. A refused move snaps the line back. [planned, Phase 2] load a `LevelConfig`, slots and queue, `Over` is `Win` or `Lose`, an end overlay with Undo and Restart.
- **Endings:** `EndReason` = `Win`, `QueueOverflow` (append-only). `DESIGN.md` §5.7.
- **Undo:** an unlimited stack of state snapshots in the first build; a **used booster is never refunded** (the booster count is not in the snapshot). `DESIGN.md` §5.7, §5.9.
- **Persistence [planned, keys not chosen]:** tutorial steps already shown (`TutorialId`), the player's booster counts, the level reached. Nothing from the engine state is saved mid-run. Currency and prices are product settings, **OPEN** (§12).

---

## 8. Input, UI and tutorial

- **Input [built]:** `DragInput`, one pointer path (`Input.GetMouseButton*`; a touch works through the simulated mouse). On press it ignores the pointer when it is over uGUI (`EventSystem.IsPointerOverGameObject`, with the finger id on touch); while a move plays the controller disables the component, and `OnDisable` drops a half-made drag. Axis lock after 8 logical units of the 360-wide reference (`_lockDistance`), `notches = round(offset)`. Snap 130 ms and snap back 140 ms are played by `PhasePlayer`. The locked-line flash is planned (Phase 4). Rules: `DESIGN.md` §4.1.
- **Preview [planned, Phase 3]:** runs the real move on a copy at every pointer move; white / orange outlines and the overflow warning. `DESIGN.md` §4.2.
- **Tutorial [planned]:** overlay steps shown once, stored in save data, listed by append-only `TutorialId` in a data asset: `Slide`, `Targets`, `Queue` at game start; `Dual`, `X2`, `LockedSlot`, `QueueGrow`, `Chain`, `Seal`, `Spring` at the first level that has the mechanic; `Hammer`, `SwapTarget`, `Stash` when the booster is first offered. `DESIGN.md` §5.8.
- **Layout [partly built]:** portrait, 360 logical units reference width, top to bottom: slots, next strip, queue and vault, next-spawn dots, grid, booster bar, undo / restart. `DESIGN.md` §4.
- **HUD [built]:** a Screen Space Overlay canvas (Canvas Scaler 360 × 640, match width) with `MovesText` (TMP, shown by `HudView.ShowMoves`, which `GameController` calls; placeholder format "Moves: {0}") and `RestartButton` (OnClick → `GameController.Restart`). The strings are English placeholders until the text table exists: the default TMP font (LiberationSans SDF) lacks some Vietnamese glyphs.
- **Camera [partly built]:** `CameraFit` fits the grid (width first, height if taller) with a margin and refits when `Screen` changes size, also in Edit Mode (it follows the Game view size through `Handles.GetMainGameViewSize`, using the level size pushed by `GameController.OnValidate`). The camera size saved in the scene is therefore only a preview value that Play mode recomputes. Not layout-aware yet: slots, queue and the booster bar will need a content height, and `Screen.safeArea` (notch) is not handled (Q15).

---

## 9. Content status: levels, boosters, theme

| Item | Status |
|---|---|
| Level 1 "Vườn nhỏ" (6×6, no obstacles) | specified in `DESIGN.md` §5.1; runs today only as the temporary fields on `GameController` (6 × 6, 5 colours, seed 20261031) with the plain board; **no `LevelConfig` asset yet** (Phase 2 task 6) |
| Level 2 "Vườn ươm" (6×6, ×2, dual, locked slot, growing queue) | specified; no asset |
| Level 3 "Nhà kính" (7×7, adds chain, sealed block, spring) | specified; the special block cells of the prototype are **not** recorded in `DESIGN.md` v0.4 (the level-data section was dropped), so Level 3's block layout must be re-decided **OPEN** (§12) |
| Boosters Hammer, Swap target, Stash | rules specified (`DESIGN.md` §5.9); no code. Prices, currency and free uses are undecided product settings |
| Fruit set (strawberry, blueberry, pear, orange, plum) | decided (`DESIGN.md` §9). `DESIGN.md` says the art "already exists", but **no fruit art is in this repository**; where it lives is **OPEN**. Today a block is the `Block` prefab (the `Square` sprite) tinted with a 5-colour placeholder palette in `BoardView` (no shape per colour yet) |
| Audio | none; the first build has no sound |
| Fonts | only the default TMP font (LiberationSans SDF) is imported; Nunito (text) and Fredoka (headings) are planned |

---

## 10. Tuning values

All numbers are in `DESIGN.md` §5.1 (global table) and §5.9 (boosters). There are no config assets yet, so there are no live values to record. When `GameConfig` / `LevelConfig` exist, list here, per field: the asset, the live value, and whether it is serialized in the asset (not only a C# default). Fields planned so far:

- `GameConfig`: `capMin` 3, `capMax` 5, `swapMin` 2, booster prices and free uses (undecided), animation timings (`DESIGN.md` §7 starting values).
- `LevelConfig`: `rows`, `cols`, `colors`, `sequence` (targets `{color, cap}`), `activeSlots`, `lockedSlotIndex`, `queueMax`, `qOpenStart`, `qGrow`, `seed`, special blocks.

A `cap` outside `capMin..capMax` is a data error and must fail on load.

**Temporary values outside any asset (an I10 debt).** They are serialized fields or prefab / scene values, not literals in logic, but they are not in `GameConfig` / `LevelConfig` yet: `PhasePlayer` timings (snap 0.13 s, snap back 0.14 s, clear 0.17 s, pulse 1.15, fall 0.16 s + 0.09 s per cell); `DragInput._lockDistance` 8; the 5-colour palette on `BoardView`; the block scale (0.92) on the `Block` prefab and the board colour on the `Background` object (art, not rules); `BoardView._preMadeBlocks` 49 (the size of the pre-made pool: a 7 × 7 grid); `CameraFit._margin` 0.5 cell; `GameController` level fields (6 × 6, 5 colours, seed 20261031).

---

## 11. Tools and testing

| Tool | Where | Use |
|---|---|---|
| Unity Test Framework 1.1.33 | `Assets/Tests/EditMode` (38 tests, all passing as reported by the user), `Assets/Tests/PlayMode` (none yet); Window > General > Test Runner | the engine is pure C#, so rule and invariant tests run in Edit Mode |
| Batch mode | see `AGENTS.md` §6 | `Unity.exe -batchmode -nographics -projectPath . -runTests -testPlatform EditMode ...`; the Editor must be closed |
| Unity CLI / MCP | whatever the session provides | inspect scenes, prefabs and the Console; refresh and wait for the recompile after script edits |

Testing approach:

- Put every rule in the pure engine and test it headless. Tests read numbers from configs and do not pin balance values.
- Planned invariant tests: same seed and moves give the same `Hash()` (I4); preview equals result (I5); no hole after a non-terminal move (I9, not on a lost state); a target of `cap` 3, 4 and 5 completes at exactly its own `cap`; after any booster use: no hole, the multiset of `Seq` unchanged, `(Seq.Length − SeqI) + open slots = total − Done`, no stream pointer moved, no unit added to the queue, undo restores the board and does not refund the use.
- Covered so far (Edit Mode, 38 tests, all passing): `ColumnStream` range, determinism, column separation and colour use; the closed-form generator against a stepwise reference; matcher runs; slide wrap, direction and floor-mod; grid generation (full, unique ids, never a match over 60 seeds and two sizes); `Clone` independence and `Hash`; `Move` (zero shift refused, input not mutated, two hand-calculated scenarios, full and match-free board after every move, spawns equal clears, round numbering, determinism, same result after "undo").
- The view has no automated test. It is checked by hand in Play mode, and `GameController` runs `BoardView.Matches(State)` after every move and logs an error if the screen differs from the state.
- No bot and no solver exist. How solvability is checked (I11) is **OPEN** (§12).
- There is no player playtest data. Any difficulty claim must be labelled arithmetic, simulation or observed run.

---

## 12. Known issues (open)

Only genuine defects and undecided questions live here. When you fix one, move it to §13 in the same commit.

| # | Sev | Issue | Where | Fix / owner |
|---|---|---|---|---|
| Q1 | High | **Level 3's special block layout is lost**: `DESIGN.md` v0.4 dropped the level-data section, and the prototype is not in the repo. Level 3 cannot be built as specified | `DESIGN.md` §5.1 | decide the cells, or restore them from the prototype |
| Q2 | High | **How to check solvability (I11)** is undecided now that the balance section is gone | `DESIGN.md` I11 | choose a bot or solver on the Unity engine |
| Q3 | Med | The five fruit art assets are said to exist but are not in the repo; the silhouette check (colour-vision) has not been done | `DESIGN.md` §9 | locate and import; check |
| Q4 | Med | Booster currency, prices and free uses are undecided | `DESIGN.md` §5.9 | product decision |
| Q5 | Med | `unityyamlmerge` driver not configured in this clone | `.git/config` | run the setup before the first scene / prefab merge |
| Q6 | Low | Application identifier is the placeholder `com.DefaultCompany.2DProject`; company name `DefaultCompany` | Project Settings | ask before changing (Project Settings) |
| Q7 | Low | Linear colour space and scripting backend not reviewed for mobile | Project Settings | review at the first device build |
| Q8 | Low | Ownership of areas is not recorded (§4) | this file | ask the team |
| Q9 | Low | `DESIGN.md` §1 says "about 20–40 slides" per level as a design target; nothing has measured it on the Unity engine | `DESIGN.md` | re-state when levels exist |
| Q10 | Low | No golden values from the prototype exist for `Mulberry32` / `ColumnStream`: the tests prove closed form == stepwise form, not equality with the prototype (see S12) | `Assets/Tests/EditMode/Rng/` | add golden values if the prototype is found |
| Q11 | Low | While a line is dragged, a block jumps to the other edge when its centre crosses the edge (no ghost copy at the seam) | `BoardView.ShowLineOffset` | polish (level 2 of the view work) |
| Q12 | Low | Animation curves and timings are first guesses; a refused move only snaps back (no shake or hint) | `PhasePlayer` | tune in Play mode; move the values to `GameConfig` |
| Q13 | Low | HUD text is English and the default TMP font lacks some Vietnamese glyphs; the display text table does not exist | `HudView`, TMP | choose a font with Vietnamese support, build the text table (Phase 6) |
| Q15 | Low | The camera fits only the grid: it ignores the height the slots, queue and booster bar will need, and `Screen.safeArea` (a notch can cover the move counter at the top) | `CameraFit`, `Canvas` | layout-aware fit on the Stage in Phase 2 task 7 (S34); a safe-area fitter before a device build (Phase 6) |

---

## 13. Fixed issues ledger

| # | Fixed | What | Where |
|---|---|---|---|
| — | 2026-11-03 | Default orientation was Auto Rotation; set to Portrait with the other auto-rotate options off | `ProjectSettings/ProjectSettings.asset` |
| — | 2026-11-04 | `ColumnStreamTests` (5 tests) was missing after Phase 0; it is now in the repository | `Assets/Tests/EditMode/Board/ColumnStreamTests.cs` |
| Q14 | 2026-11-05 | The editor-generated files `ProjectSettings/SceneTemplateSettings.json` and `Assets/TextMesh Pro/` are committed (they are in `git ls-files`; the tree was clean at `e56dae2`) | repository |

---

## 14. Roadmap

Nothing is built. Suggested order, following `CLAUDE.md` §5 (engine first, then view, then data, then docs). **Treat as a proposal; the team sets priorities.**

1. **Engine core:** [Phase 0 and 1 done, 38 tests pass] `BoardState`, `ColumnStream`, `Matcher`, shift, clear, `SandSettler`, `Move`. [Phase 2, planned below] unit routing, complete, pull, overflow and win checks, the cap rule; undo is a stack of states kept by the game assembly.
2. **Mechanics:** ×2, dual, locked slot, growing queue, then chain, sealed block, spring, with a test each (I7 exits).
3. **Boosters:** Hammer, Swap target, Stash with `Seq` / `Pre` / `Vault` in the snapshot, and the booster invariant tests.
4. **Preview and undo** on the engine side.
5. **View:** [Phase 1 built: board, drag, `PhasePlayer`, move counter, Restart, composition root, camera refit, pre-made block pool] remaining: slot and queue views and the end overlay (Phase 2), booster bar, then the tutorial and the text table.
6. **Config assets and levels:** `GameConfig`, three `LevelConfig`s (after Q1), solvability check (after Q2).
7. **Theme and art:** import the fruit set (Q3), fonts, background.

### Phase 2 plan: targets, queue, win and loss (decisions S27–S34; nothing below is built)

Goal: Level 1 can be played from start to a win or a loss, with Undo and Restart. Order matters: each task is finished and checked before the next. "Task n" below is the number in this list.

| # | Task | Done when |
|---|---|---|
| 0 | Docs for the decisions (this edit) | `DESIGN.md` v0.8 and this file agree |
| **Engine** | | |
| 1 | Data model: `Target`, `Slot`, `EndReason`, `LevelData` extended (`ActiveSlots`, `QueueMax`, `QOpenStart`, `QGrow`, `Seq`), `BoardState` gains `Slots`, `Queue`, `QOpen`, `Seq`, `Pre`, `Vault`, `SeqI`, `Done`, `Ended` with `New` / `Clone` / `Hash` updated, `InternalsVisibleTo`, the old tests moved to a sandbox-level helper | the 38 existing tests pass unchanged in behaviour; new `Clone` / `Hash` tests for the added fields |
| 2 | Unit routing inside `Clear` (slot, queue, vanish) with `Flights` and `Vanished`, in the order of `DESIGN.md` §5.4 | tests: routing rules, round 1 queues and round 2 or later vanishes, round numbers restart at 1 on every slide |
| 3 | `Complete` and `Pull` loop, `qGrow`, exact `cap` 3 / 4 / 5 | tests: a target completes at exactly its own `cap`, pull order, chained completions, `qOpen` growth |
| 4 | Overflow check, win check, `End` phase, `Move` refuses an ended state | tests: a completion rescues an overflow, win on the last target, loss on overflow, no `Fall` after an end |
| 5 | Invariant tests: unit conservation (spawned = in slots + in queue + vanished + consumed by completions), same seed and moves give the same `Hash` (I4), holding the old state and moving again gives the same result, no hole after a non-terminal move (I9) | all pass |
| **Game** | | |
| 6 | `GameConfig` (`capMin`, `capMax`) and `LevelConfig` ScriptableObjects, the Level 1 asset (6 × 6, 5 colours, seed 20261031, 12 targets with colours 0 1 2 3 4 1 0 3 2 4 0 1 and `cap` 3, `queueMax` 5, `qOpenStart` 5, no `qGrow`), the conversion with the `cap` range check; `GameController` reads the `LevelConfig` instead of its temporary fields | a test for the conversion; Play mode runs as before |
| 7 | Stage with anchor roots, layout-aware `CameraFit` (closes Q15), `SlotView`, `QueueView`, next strip and "targets left" in the HUD, drawn from the state with placeholder art | after every move the slots and queue on screen match the state |
| 8 | End overlay (win, loss), Undo (shown on loss only) and Restart, a stack of `BoardState` snapshots | undo restores the board, queue, slots and stream positions; nothing refunded |
| 9 | `PhasePlayer` plays `Clear` flights, `Complete`, `Pull` and `End`; the slots and queue are drawn from the state before a phase until the flight lands; `BoardView.Matches` also checks slots and queue | no error in the Console after a full game; the screen equals the state |

Deliberately not in Phase 2: the drag preview and next-spawn dots (Phase 3, so I5 is not met for the queue consequence of a move until then), the locked slot, dual, ×2, spring, seal and chain (Phase 4), boosters (Phase 5), tutorial, text table and art (Phase 6). The Phase 1 sweep tests assume a run never ends; task 1 moves them to the sandbox helper rather than weakening them.

Not built, proposed as later content (`DESIGN.md` §5.6): one-way rows, heavy blocks, a second slot-lock type, bigger grids, moving obstacles. Also later: limit or monetise undo, a tutorial spread over more levels, a seeded daily level.

---

## 15. Decision log

Condensed; newest last. The rule decisions behind the prototype are in `DESIGN.md` (§2, §5.4, §5.9, §5.12 for rejected mechanics).

| ID | Date | Decision | Status |
|---|---|---|---|
| S1 | 2026-11-02 | Move from HTML prototype to a Unity 2D project; the prototype is not kept in the repo | final |
| S2 | 2026-11-02 | One settler (`SandSettler`): ordinary gravity when no chain exists, sand flow around chains | final |
| S3 | 2026-11-02 | The Level 1 "anti-match" spawn recolour is **not** ported (I5) | final |
| S4 | 2026-11-02 | Unity version **2022.3.62f3**, classic Input Manager, no new packages | final until the team changes it |
| S5 | 2026-11-03 | Portrait only | final |
| S6 | 2026-11-03 | Each target's `cap` is 3 to 5 and a level picks its number of targets (`DESIGN.md` v0.4) | final |
| S7 | 2026-11-03 | A used booster is never refunded, not by undo, restart or a loss | final |
| S8 | 2026-11-03 | Hints replaced by a tutorial shown once per step and stored in save data | final |
| S9 | 2026-11-03 | Fruit set: strawberry, blueberry, pear, orange, plum; theme is display only | final; art not in repo (Q3) |
| S10 | 2026-11-03 | Balance tables and level data removed from `DESIGN.md` | final; consequences in Q1, Q2 |
| S11 | 2026-11-04 | The starting grid is rolled from the separate stream "column −1", row by row, re-rolling a cell that would complete a run of 3 | final |
| S12 | 2026-11-04 | Colour of a stream value is `(value × colors) >> 32` | final; not compared with the prototype, so seeds may give different levels |
| S13 | 2026-11-04 | Positive `notches` moves a row right and a column down | final |
| S14 | 2026-11-04 | An empty cell is a `Block` with `Id == 0`; real ids start at 1 | final |
| S15 | 2026-11-04 | `Fall` reports `Starts` as an ordered `FallStart` list; guards `MaxRounds` and 500 settle passes throw | final |
| S16 | 2026-11-04 | The stream generator is stateless (closed form), so no cache and no shared state | final |
| S17 | 2026-11-04 | The board is drawn with `SpriteRenderer`s and a `SpriteMask` in world space; uGUI is used for the HUD only | final |
| S18 | 2026-11-04 | Animation uses coroutines and curves; DOTween is not installed and adding a package needs a decision | final |
| S19 | 2026-11-04 | The new state is applied at once and the view catches up; `BoardView.Matches` checks the screen against the state after every move | final |
| S20 | 2026-11-04 | Level fields stay on `GameController` until `LevelConfig` exists; blocks are a tinted `Square` sprite with a placeholder palette until the art decision (Q3) | temporary |
| S21 | 2026-11-04 | `GameController` is the composition root: the only component wired in the Inspector. `PhasePlayer` and `DragInput` get dependencies through `Init`; `DragInput` reports through `Dragging` / `Released` events and is disabled while a move plays; `HudView` is driven by `ShowMoves`. No static singletons | final |
| S22 | 2026-11-04 | Blocks are pooled with `UnityEngine.Pool.ObjectPool<BlockView>` over a `Block` prefab in `Assets/Prefabs/`, prewarmed to `rows × cols` | superseded by S24 |
| S23 | 2026-11-04 | `CameraFit` refits when the screen size changes; the layout-aware fit and the safe area wait for Phase 2 and 6 (Q15) | final |
| S24 | 2026-11-04 | Blocks are **pre-created in the scene** under `Board/Blocks` (editor command `Pre-create Blocks`, 49 = 7 × 7) and adopted by a small `BlockPool` class; `Instantiate` runs only when the pool is short, and a warning says so. The built-in `ObjectPool<T>` cannot adopt objects it did not create, so S22 is replaced. Cost: the scene file grows by the 49 objects (making `Blocks` a prefab would keep the scene small, but then the Edit Mode preview writes its positions back as scene overrides, see S25) | final; unverified in Play mode |
| S25 | 2026-11-04 | In Edit Mode the scene shows the grid like Play mode: when `Pre Made Blocks` or the level size (`GameController` `_rows`, `_cols`) changes, `BoardView` creates the missing pre-made blocks, activates the first `rows × cols` of them and lays them out with the prefab's own colour; `GameController.OnValidate` pushes the size down (the controller still owns the level data, the view never references it). It never deletes blocks and does nothing in Play mode | final; unverified in the Editor |
| S26 | 2026-11-04 | `CameraFit` is `ExecuteAlways`: in Edit Mode an `EditorApplication.update` tick reads the Game view size (`Handles.GetMainGameViewSize`) and fits the camera to the level size pushed by `GameController.OnValidate`, so the board is responsive without pressing Play. The aspect comes from that size in both modes, not from `Camera.aspect`, so the Camera inspector preview cannot disturb it. Side effect: the orthographic size stored in the scene follows the last Game view shape | final; unverified in the Editor |
| S27 | 2026-11-05 | The end of a run is a nullable state field `Ended` (`EndReason?`: `Win`, `QueueOverflow`), not a separate `Over` enum; `DESIGN.md` §6.1 follows | final |
| S28 | 2026-11-05 | Units of a round are routed in the row-major order of `Matcher.Find`, a ×2 block's two units one after the other, all before the round's completions; completions run left to right and repeat (`DESIGN.md` §5.4) | final; this fills a gap in the locked resolution order, it does not change it |
| S29 | 2026-11-05 | The engine declares `InternalsVisibleTo("SlideJam.Tests.EditMode")` so tests can build states with chosen slots, queue and `Seq` | final |
| S30 | 2026-11-05 | `LevelData` is extended with `ActiveSlots`, `QueueMax`, `QOpenStart`, `QGrow`, `Seq`; the Phase 1 tests move to a sandbox-level helper (very long `Seq`, very large queue) instead of the old four-argument constructor | final |
| S31 | 2026-11-05 | `Seq`, `Pre` and `Vault` are in the state from Phase 2 (the vault stays empty until Phase 5) so `Clone` and `Hash` change once | final |
| S32 | 2026-11-05 | `qGrow` is built in Phase 2; the locked slot, dual, ×2, spring, seal and chain stay in Phase 4 | final |
| S33 | 2026-11-05 | A minimal `GameConfig` (`capMin` 3, `capMax` 5) is created in Phase 2; the 3 to 5 range is checked in the game assembly when a `LevelConfig` is converted, and the engine only requires `Cap ≥ 1` | final |
| S34 | 2026-11-05 | World-space layout is a Stage with anchor roots (`Slots`, `Strip`, `Queue`, `Grid`) in cell units; `CameraFit` fits the Stage bounds (closes Q15 except the safe area) | final |

---

## 16. Verification gaps

Everything in this file marked `[verified]` was read from a file. Not checked:

| Gap | How to check |
|---|---|
| The project opens and compiles in Unity 2022.3.62f3 with the current packages | open in the Editor; read the Console |
| The Portrait setting survived (the Editor was open when the file was edited and may rewrite it) | Project Settings > Player > Resolution and Presentation |
| `unityyamlmerge` availability | `git config --get merge.unityyamlmerge.driver` and the Unity install's `UnityYAMLMerge` |
| Phase 1 was merged to `main` at `e56dae2`; the user has not yet said that the pool, the Edit Mode preview and the Edit Mode camera (S24–S26) were checked after the merge. They stay unconfirmed until they do | confirm in the Editor, then change S24–S26 to verified |
| The Edit Mode results (38 pass) are the user's report; the agent has not seen a result file | Test Runner > EditMode > Run All, or the batch command in `AGENTS.md` §6 |
| The Phase 1 view in every case (snap back, a full revolution, cascades, Restart in the middle of an animation, touch on a device): the user ran it without errors but did not report each case | Play mode checklist in the Phase 1 view notes; build to a phone for touch |
| The pre-made block pool (`BlockPool`, batch 3): same Play checklist; no `Block(Clone)` appears while the level fits in the pre-made blocks, a level larger than the pool logs a warning and instantiates the rest, and no "released twice" error | Play mode; then read the prefab and the scene file |
| The Edit Mode grid preview (S25): changing `Pre Made Blocks` or `GameController` rows / columns redraws the white grid in the Scene view; a level larger than the pool logs a warning; nothing changes in Play mode | change the fields in the Inspector in Edit Mode |
| The Edit Mode camera (S26): resizing the Game view or changing its aspect in Edit Mode refits the camera at once; changing `GameController` rows / columns refits it; Play mode behaves as before | resize the Game view and change the aspect dropdown in Edit Mode |
| `CameraFit` refits when the Game view is resized during Play | resize the Game view in Play mode |
| LFS is installed and the rules match the planned art / audio | `git lfs env` |

---

## 17. Update checklist

Run this before every commit. If a box applies, update that section **in the same commit** and add a history row (§18) for anything bigger than a value tweak.

- [ ] Changed a `GameConfig` / `LevelConfig` value or field → §10 (and `DESIGN.md` §5).
- [ ] Added, renamed or removed a script, assembly, folder or service → §6 (and §5.3).
- [ ] Changed packages, Project Settings, layers, the scene or prefabs → §5.
- [ ] Changed flow, endings, undo or persistence → §7.
- [ ] Changed input, preview, tutorial or layout → §8.
- [ ] Added or changed a level, booster, art or theme item → §9.
- [ ] Added or changed a test or tool → §11, and the invariant table in §2.
- [ ] Fixed a known issue → move it from §12 to §13. Found a new one → add it to §12.
- [ ] Made or changed a design decision → §15 (and `DESIGN.md` §2 if it touches an invariant: owner sign-off required).
- [ ] Finished or started roadmap work → §14.
- [ ] Verified something from §16 → record it and delete the row.
- [ ] Bump the snapshot table at the top (date, branch, commit).

---

## 18. Document history

| Date | Change |
|---|---|
| 2026-11-03 | First version. Written from `DESIGN.md` v0.4, the repository at `f566452`, `Packages/manifest.json` and `ProjectSettings/`; structure adapted from another project's `PROJECT.md`. Records that no game code exists yet |
| 2026-11-04 | Phase 0 and 1 engine recorded: §2 statuses, §5, §6 code map, §11 tests, §12 Q10, §14, decisions S11–S16. Written from the files in the repository at `BoardCore`; nothing was run |
| 2026-11-04 | Tests run by the user (38 pass): statuses in §2, §11, §14, §16 changed from "not yet run"; snapshot moved to `706d99c`; `ColumnStreamTests` found; Q10 reduced to the missing golden values |
| 2026-11-04 | Phase 1 view recorded (levels 0 and 1 of the view plan: `BoardView`, `DragInput`, `PhasePlayer`, `HudView`, scene wiring read from the scene file): §1 snapshot, §2 I3 and I8, §5.1 assemblies, §5.3, §5.5, §6, §7, §8, §9, §10 temporary values, §11, §12 Q11–Q14, §14, decisions S17–S20 |
| 2026-11-04 | View refactor recorded: composition root and events (S21), pooled `Block` prefab (S22, recorded from delivered code and not yet read in the repository), camera refit (S23); §3 wiring convention, §2 I8, §5.3, §5.5, §6, §7, §8, §9, §10, §12 Q15, §14, §16 |
| 2026-11-04 | Block pool changed to pre-made scene objects adopted by `BlockPool` (S24, supersedes S22): §5.3, §5.5, §6, §10, §15, §16. Recorded from delivered code; not yet read in the repository |
| 2026-11-04 | Edit Mode grid preview recorded (S25): §5.5, §6, §15, §16. Recorded from delivered code; not yet read in the repository |
| 2026-11-04 | Edit Mode camera (S26): `CameraFit` rewritten as `ExecuteAlways` with an editor tick; `GameController.OnValidate` also pushes the size to it: §5.5, §6, §8, §15, §16. Written to the repository by the agent; not run. Also noted: the scene now has `Assets/Prefabs/Board.prefab` (Board, Background, Mask, Blocks, `BoardView`) with the preview size as scene overrides |
| 2026-11-05 | Phase 1 recorded as complete and merged (`main` @ `e56dae2`); Q14 moved to §13; Phase 2 plan and decisions S27–S34 recorded before any code (§12 Q15, §14, §15, §16, snapshot); `DESIGN.md` raised to v0.8 (§4, §5.1, §5.4, §6, §6.1, §6.2, §8). Docs only; nothing was run |

---

## 19. Glossary

The project terms are defined in `DESIGN.md` §8 (Slide, Line, Notch, Block, Match, Round, Cascade, Unit, Target / Slot, Pip, Cap, Queue, Vault, Booster, `Seq` / `Pre`, Theme, `qOpen`, Pull, Vanish, Overflow, Settle, Sand flow, Stream, Next-spawn dot, Chain, Sealed block, Spring block, ×2 block, Dual block, Locked slot, Preview, `EndReason`). Use those exact names in code, commits and agent prompts; the Vietnamese display names are in `DESIGN.md` §9 and never appear in identifiers.
