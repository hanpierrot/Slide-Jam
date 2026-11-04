# Slide Jam — Project Reference

> ## ⚠️ MAINTENANCE RULE (mandatory for every human and agent)
> **This file must stay true.** Any change to code, `GameConfig` / `LevelConfig` assets, prefabs, the scene, Project Settings, packages or a design decision that makes a statement here false **must update this file in the same commit**. A commit that changes behaviour without updating `PROJECT.md` is incomplete and should not be merged.
> Before finishing any task, run the **update checklist in §17** and bump the snapshot line below.

| Snapshot | Value |
|---|---|
| Last verified against the repository | **2026-11-03**, static read of `main` @ `f566452` ("Init project") plus uncommitted edits to the three manual files and `ProjectSettings.asset` (orientation set to Portrait). Nothing was compiled or run: the Unity Editor was not driven in this pass |
| State of the game | **No game code, no config asset, no art and no audio exist yet.** `Assets/` holds only `SampleScene.unity` and empty folders. Everything in §6–§8 and §10 is the **plan**, not a description of code |
| Branch / commit | `main` @ `f566452`; remote `https://github.com/hanpierrot/Slide-Jam.git`; the manual files and the Portrait setting are uncommitted |
| Engine | Unity **2022.3.62f3** (LTS), 2D template, built-in render pipeline, single scene |
| Design spec | `DOCS/DESIGN.md` **v0.4** (2026-11-03) is the source of truth for rules, numbers and interfaces. This file records the **project state**: setup, code map, decisions, issues and what is built |
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
6. [Code map (planned)](#6-code-map-planned)
7. [Game flow and persistence (planned)](#7-game-flow-and-persistence-planned)
8. [Input, UI and tutorial (planned)](#8-input-ui-and-tutorial-planned)
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
| I3 | One core verb; boosters are not moves | not built |
| I4 | Deterministic (seeded column streams, no hidden randomness) | not built |
| I5 | No lies (next-spawn dots and preview are real) | not built |
| I6 | Combos never punish (cascade surplus vanishes) | not built |
| I7 | Locks close a door, never end the run | not built |
| I8 | Resolution is atomic | not built |
| I9 | Board full after settle | not built |
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
- No `.asmdef` files yet. The planned assemblies are in §6.

### 5.2 Repository

- Remote: `https://github.com/hanpierrot/Slide-Jam.git`. Branches: `main` only. One commit: `f566452 Init project`.
- `.gitattributes`: Git LFS rules for images, audio, video, fonts and archives (74 `lfs` lines); Unity YAML files use `merge=unityyamlmerge eol=lf`. `.gitignore` is the standard Unity one.
- **The `unityyamlmerge` merge driver is not defined in this clone's git config** (`git config --get merge.unityyamlmerge.driver` prints nothing). Unless it is set in a global gitconfig, the `.gitattributes` rule falls back to a text merge. Set it up before the first scene or prefab merge. **OPEN** (§12).
- Agent instructions: `AGENTS.md` (all agents), `CLAUDE.md` (Claude; imports `AGENTS.md`). There is no `.claude/` folder yet.
- Docs: `DOCS/DESIGN.md`, `DOCS/PROJECT.md` (this file).

### 5.3 Assets layout [verified]

| Path | Contents today |
|---|---|
| `Assets/Scenes/SampleScene.unity` | the only scene, in the build list (index 0) |
| `Assets/Art/`, `Audio/`, `Design/`, `Resources/`, `Scripts/` | empty (only `.meta` files) |

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

### 5.5 Scene `SampleScene.unity` [verified: default template scene]

Camera and light only, no game objects. **Planned** (not built): a `MANAGERS` root (game controller, config references), a `Board` root (grid, slots, queue, vault, next-spawn dots), a `Canvas` (HUD, booster bar, tutorial overlay, end overlay) and a fixed orthographic camera for the 360-unit reference width (`DESIGN.md` §4). Restart = reload the scene or rebuild the state; no `DontDestroyOnLoad`.

---

## 6. Code map (planned)

There is no code. This is the intended map, taken from `DESIGN.md` §6; replace it with the real one as files appear.

| Area | Planned contents |
|---|---|
| Engine (`SlideJam.Engine`, "No Engine References") | `BoardState` (state, `Clone`, `Hash`), `MoveResolver` (`Move`, `Preview`, `Hammer`, `SwapInfo`, `SwapTarget`, `Stash`), `SandSettler`, `ColumnStream`, `Matcher`, `Board` (line helpers), phase and flight types, `LevelData` |
| Game (`SlideJam.Game`) | `BoardView`, `DragInput`, `PhasePlayer` (animation), `HUD`, `BoosterBar`, `TutorialOverlay`, `TextTable` (display names), `SaveData` (tutorial-seen flags, booster counts), the `GameConfig` and `LevelConfig` ScriptableObjects and their conversion to engine data |
| Tests | Edit Mode: per-mechanic rules, invariants, determinism, caps, boosters. Play Mode: real drag vs engine prediction |

Singletons and services are **not decided**. Prefer plain objects owned by one game controller over `static` singletons; any static must be reset on scene load.

---

## 7. Game flow and persistence (planned)

- **Run lifecycle [planned]:** load a `LevelConfig` → `BoardState.New` → player slides or uses a booster → engine returns a phase list → view plays it → repeat until `Over` is `Win` or `Lose` → end overlay (Undo on loss, Restart). Input is ignored while phases play (I8).
- **Endings:** `EndReason` = `Win`, `QueueOverflow` (append-only). `DESIGN.md` §5.7.
- **Undo:** an unlimited stack of state snapshots in the first build; a **used booster is never refunded** (the booster count is not in the snapshot). `DESIGN.md` §5.7, §5.9.
- **Persistence [planned, keys not chosen]:** tutorial steps already shown (`TutorialId`), the player's booster counts, the level reached. Nothing from the engine state is saved mid-run. Currency and prices are product settings, **OPEN** (§12).

---

## 8. Input, UI and tutorial (planned)

- **Input:** one pointer path for mouse and touch through the classic Input Manager. Axis lock after 8 logical units, `notches = round(offset / cellSize)`, snap in 130 ms, locked lines flash red. Full rules: `DESIGN.md` §4.1.
- **Preview:** runs the real move on a copy at every pointer move; white / orange outlines and the overflow warning. `DESIGN.md` §4.2.
- **Tutorial:** overlay steps shown once, stored in save data, listed by append-only `TutorialId` in a data asset: `Slide`, `Targets`, `Queue` at game start; `Dual`, `X2`, `LockedSlot`, `QueueGrow`, `Chain`, `Seal`, `Spring` at the first level that has the mechanic; `Hammer`, `SwapTarget`, `Stash` when the booster is first offered. `DESIGN.md` §5.8.
- **Layout:** portrait, 360 logical units reference width, top to bottom: slots, next strip, queue and vault, next-spawn dots, grid, booster bar, undo / restart. `DESIGN.md` §4.

---

## 9. Content status: levels, boosters, theme

| Item | Status |
|---|---|
| Level 1 "Vườn nhỏ" (6×6, no obstacles) | specified in `DESIGN.md` §5.1; **no `LevelConfig` asset yet** |
| Level 2 "Vườn ươm" (6×6, ×2, dual, locked slot, growing queue) | specified; no asset |
| Level 3 "Nhà kính" (7×7, adds chain, sealed block, spring) | specified; the special block cells of the prototype are **not** recorded in `DESIGN.md` v0.4 (the level-data section was dropped), so Level 3's block layout must be re-decided **OPEN** (§12) |
| Boosters Hammer, Swap target, Stash | rules specified (`DESIGN.md` §5.9); no code. Prices, currency and free uses are undecided product settings |
| Fruit set (strawberry, blueberry, pear, orange, plum) | decided (`DESIGN.md` §9). `DESIGN.md` says the art "already exists", but **no art file is in this repository**; where the assets live is **OPEN** |
| Audio | none; the first build has no sound |
| Fonts | Nunito (text) and Fredoka (headings) planned; not imported |

---

## 10. Tuning values

All numbers are in `DESIGN.md` §5.1 (global table) and §5.9 (boosters). There are no config assets yet, so there are no live values to record. When `GameConfig` / `LevelConfig` exist, list here, per field: the asset, the live value, and whether it is serialized in the asset (not only a C# default). Fields planned so far:

- `GameConfig`: `capMin` 3, `capMax` 5, `swapMin` 2, booster prices and free uses (undecided), animation timings (`DESIGN.md` §7 starting values).
- `LevelConfig`: `rows`, `cols`, `colors`, `sequence` (targets `{color, cap}`), `activeSlots`, `lockedSlotIndex`, `queueMax`, `qOpenStart`, `qGrow`, `seed`, special blocks.

A `cap` outside `capMin..capMax` is a data error and must fail on load.

---

## 11. Tools and testing

| Tool | Where | Use |
|---|---|---|
| Unity Test Framework 1.1.33 | `Assets/Tests/EditMode`, `Assets/Tests/PlayMode` (none yet); Window > General > Test Runner | the engine is pure C#, so rule and invariant tests run in Edit Mode |
| Batch mode | see `AGENTS.md` §6 | `Unity.exe -batchmode -nographics -projectPath . -runTests -testPlatform EditMode ...`; the Editor must be closed |
| Unity CLI / MCP | whatever the session provides | inspect scenes, prefabs and the Console; refresh and wait for the recompile after script edits |

Testing approach:

- Put every rule in the pure engine and test it headless. Tests read numbers from configs and do not pin balance values.
- Planned invariant tests: same seed and moves give the same `Hash()` (I4); preview equals result (I5); no hole after a non-terminal move (I9, not on a lost state); a target of `cap` 3, 4 and 5 completes at exactly its own `cap`; after any booster use: no hole, the multiset of `Seq` unchanged, `(Seq.Length − SeqI) + open slots = total − Done`, no stream pointer moved, no unit added to the queue, undo restores the board and does not refund the use.
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

---

## 13. Fixed issues ledger

| # | Fixed | What | Where |
|---|---|---|---|
| — | 2026-11-03 | Default orientation was Auto Rotation; set to Portrait with the other auto-rotate options off | `ProjectSettings/ProjectSettings.asset` |

---

## 14. Roadmap

Nothing is built. Suggested order, following `CLAUDE.md` §5 (engine first, then view, then data, then docs). **Treat as a proposal; the team sets priorities.**

1. **Engine core:** `BoardState`, `ColumnStream` (`mulberry32`), `Matcher`, shift, clear, complete, pull, overflow and win checks, `SandSettler`. Edit Mode tests for I4, I9 and the cap rule.
2. **Mechanics:** ×2, dual, locked slot, growing queue, then chain, sealed block, spring, with a test each (I7 exits).
3. **Boosters:** Hammer, Swap target, Stash with `Seq` / `Pre` / `Vault` in the snapshot, and the booster invariant tests.
4. **Preview and undo** on the engine side.
5. **View:** board, drag input, `PhasePlayer`, HUD, booster bar, end overlay, then the tutorial and the text table.
6. **Config assets and levels:** `GameConfig`, three `LevelConfig`s (after Q1), solvability check (after Q2).
7. **Theme and art:** import the fruit set (Q3), fonts, background.

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

---

## 16. Verification gaps

Everything in this file marked `[verified]` was read from a file. Not checked:

| Gap | How to check |
|---|---|
| The project opens and compiles in Unity 2022.3.62f3 with the current packages | open in the Editor; read the Console |
| The Portrait setting survived (the Editor was open when the file was edited and may rewrite it) | Project Settings > Player > Resolution and Presentation |
| `unityyamlmerge` availability | `git config --get merge.unityyamlmerge.driver` and the Unity install's `UnityYAMLMerge` |
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

---

## 19. Glossary

The project terms are defined in `DESIGN.md` §8 (Slide, Line, Notch, Block, Match, Round, Cascade, Unit, Target / Slot, Pip, Cap, Queue, Vault, Booster, `Seq` / `Pre`, Theme, `qOpen`, Pull, Vanish, Overflow, Settle, Sand flow, Stream, Next-spawn dot, Chain, Sealed block, Spring block, ×2 block, Dual block, Locked slot, Preview, `EndReason`). Use those exact names in code, commits and agent prompts; the Vietnamese display names are in `DESIGN.md` §9 and never appear in identifiers.
