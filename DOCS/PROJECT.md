# Slide Jam — Project Reference

> ## ⚠️ MAINTENANCE RULE (mandatory for every human and agent)
> **This file must stay true.** Any change to code, `GameConfig` / `LevelConfig` assets, prefabs, the scene, Project Settings, packages or a design decision that makes a statement here false **must update this file in the same commit**. A commit that changes behaviour without updating `PROJECT.md` is incomplete and should not be merged.
> Before finishing any task, run the **update checklist in §17** and bump the snapshot line below.

| Snapshot | Value |
|---|---|
| Last verified against the repository | **2026-11-09**: branch `GameLoop` @ `3a26b2d` (`Object Pooling for Slots, Strip, Queue & GameManager with GameConfig`); the working tree was clean (`git status`) before this docs-only edit. The scripts and tests were read and written by the agent in the sessions since Phase 2 task 5; the scene, the prefabs and the `.meta` import settings were only partly re-read. The user ran the Edit Mode tests and Play mode in earlier sessions and reported the results in chat (no result file; the agent did not run Unity): **101 test cases by count of the source** (96 `[Test]` methods, 5 `[TestCase]` rows). A full run after the latest changes (`GameConfig`, `ThemeSkin`, `SquarePool`, `GameManager`) was not reported, and the Play mode checks listed in §16 are open |
| State of the game | **Phases 0 and 1 are done; Phase 2 is built (tasks 1 to 9) and waits for its closing Play mode pass.** Engine: slots, queue, complete, pull, `qGrow`, overflow and win checks, `End`, and the Revive shuffle. Game: `LevelConfig` / `GameConfig`, the Stage with baskets, next strip, queue, an overflow row and a decoration branch drawn from a sprite pack through `ThemeSkin`, a HUD (level, targets left), an end overlay with Revive and Restart, and `GameManager`. `PhasePlayer` animates every phase: `Shift`, `Clear` with the unit flights into slots, queue or nowhere, `Complete` (slot pop and enter), `Pull`, `Fall` and `Revive`. The user ran task 9 in Play mode and reported it fine; the overflow row (S51) was written after that and the user checked its scene wiring in the Editor and reported it fine. The editor tools are now two menus (S52), not yet run. Not built: preview, next-spawn dots, mechanics (Phase 4), boosters, vault, tutorial, text table, audio, solvability check. §6 lists what exists |
| Branch / commit | Branch `GameLoop` @ `3a26b2d`, started from `main` @ `e56dae2` plus the Phase 2 commits (`807c23c`, `78f6faa`); remote `https://github.com/hanpierrot/Slide-Jam.git`. Agents never push, open PRs or merge unless asked in that conversation |
| Engine | Unity **2022.3.62f3** (LTS), 2D template, built-in render pipeline, single scene |
| Design spec | `DOCS/DESIGN.md` **v1.0** (2026-11-09) is the source of truth for rules, numbers and interfaces. This file records the **project state**: setup, code map, decisions, issues and what is built |
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
| I1 | One win, one loss | engine built and tested (`EndTests`: overflow loss, win on the last target, a finished run takes no move, the overflow check wins over the win check); the end overlay shows the result (Play mode; no automated test) |
| I2 | Turn-based | not built |
| I3 | One core verb; boosters are not moves | slide is one move and a zero shift is refused (tested); the drag path runs in Play mode (no errors reported); boosters not built |
| I4 | Deterministic (seeded column streams, no hidden randomness) | engine tested: streams, grid generation and `Move` are pure; the same moves give the same `Hash()`, and replaying from a held state gives the same result (`InvariantTests`); `Hash()` covers slots, queue, vault, `Seq`, `Pre`, `QOpen`, `SeqI`, `Done` and `Ended`. The Revive shuffle is a seeded stream (`ReviveTests`: deterministic, same blocks, no match, nothing else changes). There is no undo |
| I5 | No lies (next-spawn dots and preview are real) | not built |
| I6 | Combos never punish (cascade surplus vanishes) | engine tested: in a 750-move sweep no round 2 or later unit goes to the queue, and `Resolution.RouteUnits` is tested for rounds 1 and 2 (`ResolutionTests`) |
| I7 | Locks close a door, never end the run | not built (`Slot.Locked` exists and is skipped by routing and pull, tested with a hand-set lock; no level has a lock until Phase 4) |
| I8 | Resolution is atomic | built for moves: `Move` returns the whole phase list at once; `GameController` keeps `DragInput` disabled while `PhasePlayer` plays (`IsBusy`) and after a run has ended; boosters do not exist yet |
| I9 | Board full after settle | tested after **every** `Move`, the one that ends the run included, and after `Revive` (`InvariantTests`, `EndTests`, `ReviveTests`); there is no terminal exception since v1.0; the chain case is Phase 4 |
| I10 | Tuning in data | partly: rule limits, animation timings, input values and capacity buffers are in `GameConfig`, art-coupled numbers in `ThemeSkin`; layout metrics stay on scene components by decision (§10) |
| I11 | Every shipped level is solvable | not built; the approach is decided (S54): a stored solution or a bot, after Phase 4 |

Update this table as each invariant gets a test (§11). "Built" means a passing test exists, not that the code looks right.

---

## 3. Coding conventions

The conventions that bind every change are in `AGENTS.md` §1 and §6. Summary of the ones that came from the design, so the code starts clean:

| Rule | Why |
|---|---|
| The engine assembly has **no `UnityEngine` reference**: no `MonoBehaviour`, `Time`, `UnityEngine.Random`, `System.Random`, `DateTime`, no mutable statics | Determinism (I4), testable in Edit Mode |
| The only randomness is the column stream: `mulberry32(seed + (c + 1) × 7919)` in `uint` arithmetic (`unchecked`), no `float` | The same seed gives the same level on every platform |
| Every move and every booster returns a **new** state and a **phase list**; the old state is never mutated | Preview, replays, tests (a held state is never changed) |
| The view only plays phases. The drag preview calls the real move on a copy | One implementation of each rule (I5) |
| Tuning values live in `GameConfig` / `LevelConfig`, never as literals (`cap` is per target, 3 to 5, never a fixed 3) | I10 |
| Display names live in a text table; code uses the neutral terms of `DESIGN.md` §8 | Theme can change without touching the engine |
| `EndReason` and `TutorialId` are **append-only**; never rename, reorder or remove | Saves and recorded runs depend on them |
| Never iterate a `Dictionary` or `HashSet` to decide a rule; use a fixed order; use floor-mod for negative shifts | Determinism |
| Compare Unity objects with explicit `!= null`, never `?.` / `??` | Unity's fake-null on destroyed objects |
| Rename a serialized field only with `[FormerlySerializedAs("old")]` | Keeps asset values |
| Build new things as **prefabs**; keep `SampleScene.unity` edits minimal | Single shared scene means merge conflicts |
| Wire scene references **only on `GameController`** (the composition root); the other view components receive their dependencies through `Init` / `Build` parameters and report through events, and never reference the controller. `GameManager` is the one singleton (it holds `GameConfig`); views do not call it, the controller passes the config down | Cross-references between components on one GameObject made the setup confusing; one place to wire means one place to check |
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
| `Assets/Art/Sprites/` | the sprite pack, in `basket/`, `board/`, `booster/`, `fruits/`, `queue/`, `scene/`, `shelf/` and `ui/`, plus `Square.png` (the old placeholder). Pixels Per Unit are set per group so that one grid cell is one world unit: board 216, fruits 800, basket 280, queue 180, shelf 230, scene 216, UI 400 (the booster buttons are still 100) **[read from the `.meta` files]**. The menu *Slide Jam > Apply Sprite Quality* turns on mip maps and trilinear filtering. The `*_9slice*` sprites have no border set yet (Q17) |
| `Assets/Prefabs/Block.prefab` | the block view: a `SpriteRenderer` with `tile_bg_48` (scale 0.96, Mask Interaction Visible Inside Mask), a child `Icon` (`SpriteRenderer`, the fruit) and `BlockView`. Its pre-made inactive copies sit under `Board/Blocks` |
| `Assets/Prefabs/Board.prefab` | `Board` (`BoardView`) with `Background` (its renderer is switched off by the editor menu), `Mask` and `Blocks` |
| `Assets/Prefabs/Stage.prefab` | the Stage made from the scene by the user: `StageLayout`, the anchors, their views and `Pool_*` objects **[exists in `git ls-files`; its content was not re-read]** |
| `Assets/Design/ThemeSkin.asset` | the theme data: sprites per colour, the palette, the basket layout ratios |
| `Assets/Resources/Configs/` | `GameConfig.asset` and `LevelConfig_1.asset`, referenced from the scene (not loaded with `Resources.Load`). `DESIGN.md` §6.2 lists the same folder (S39) |
| `Assets/Audio/` | empty |
| `Assets/TextMesh Pro/` | TMP Essentials |
| `Assets/Scripts/` | assemblies `SlideJam.Engine`, `SlideJam.Game` and `SlideJam.Editor` (§6; folder tree in `DESIGN.md` §6.2) |
| `Assets/Tests/EditMode/` | assembly `SlideJam.Tests.EditMode`: `Engine/`, `Game/`, `Helpers/` (§11). `Assets/Tests/PlayMode/` does not exist yet |

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

The single scene. The roots were read from the scene file earlier; the Stage prefab and the Canvas children were recorded from the delivered setup steps and the editor menus, not re-read after the last edits:

| Root | Contents |
|---|---|
| `Main Camera` | orthographic, solid colour background (234, 248, 230), `CameraFit` (fits the Stage rectangle and refits when the screen size changes; in Edit Mode it follows the Game view) |
| `Stage` (prefab) | `StageLayout`; anchors `Slots` (`SlotsView`), `Strip` (`StripView`), `Queue` (`QueueView`), `Overflow` (a band under the queue with the `OverflowPlank` sprite, S51), `Branch` (decoration; the next-spawn dots will hang on it) and `Grid` (holds the `Board` instance and the `Panel` and `Vines` sprites); `Backdrop` with `Sky` and `Plank` (static: positions are computed by the *Setup Scene* and *Refresh Layout* menus, Q18); each view keeps its `Pool_*` objects |
| `GameManager` | `GameManager` (holds `GameConfig`) |
| `GameController` | `GameController` (the composition root: serialized references to the views, `GameManager` and the `LevelConfig`), `PhasePlayer`, `DragInput` |
| `Canvas` | Screen Space Overlay, Canvas Scaler 360 × 640, match width; `HudView` with `LevelText` and `TargetsText`; `RestartButton` (OnClick → `GameController.Restart`); `EndOverlay` (`EndOverlayView`: `Panel` with `Dim` and `Card` holding `TitleText`, `DetailText` and a `Buttons` row with `ReviveButton` and `RestartButton`) |
| `EventSystem` | created with the Canvas (classic input module) |

**Not built yet:** the vault, the next-spawn dots, the booster bar, the tutorial overlay and the `n/cap` label on the baskets. The project has two editor menus, both idempotent (S49, S52): *Slide Jam > Setup Scene* (sprite import settings, end overlay, `Overflow` anchor and plank, skin, block prefab, backdrop, then the Edit Mode preview of the slot, queue and strip objects) and *Slide Jam > Refresh Layout* (lays the Stage out again, moves the backdrop, rebuilds the preview objects; no asset is touched). Restart rebuilds the state in place; no `DontDestroyOnLoad`.

---

## 6. Code map

What exists is marked [built]; the rest is the plan from `DESIGN.md` §6. Update this table as files appear.

| Area | Contents |
|---|---|
| Engine `Rng/` [built] | `Mulberry32` (`At`, closed form), `ColumnStream` (`At`) |
| Engine `State/` [built] | `Block`, `Axis`, `Cell`, `Target`, `Slot` (`Slot.Empty`, `IsFull`, `WithCount`), `EndReason` (`Win`, `QueueOverflow`), `LevelData` (full constructor with validation, `Sandbox`), `BoardState` (`New`, `Clone`, `Hash`; slots, queue, vault, `Seq`, `Pre`, `QOpen`, `SeqI`, `Done`, `Ended`) |
| Engine `Rules/` [built] | `Board` (`Shift`, `ShiftAmount`, `LineLength`, `FloorMod`), `Matcher` (`Find`, `HasMatch`), `GridGenerator` (internal), `SandSettler` (internal; gravity only), `Resolution` (`RouteUnits`, `CompleteAndPull`, `CheckEnd`: it only sets `Ended`), `Shuffler` (internal; the Revive permutation) |
| Engine `Moves/` [built] | `MoveResolver.Move` and `MoveResolver.Revive`, `MoveResult`, `Phase`, `PhaseKind` (`Shift`, `Clear`, `Fall`, `Complete`, `Pull`, `End`, `Revive`), `FallStart`, `Place` / `PlaceKind`, `Flight`. `Assets/Scripts/Engine/AssemblyInfo.cs` makes internals visible to `SlideJam.Tests.EditMode` |
| Engine [planned] | `Preview` (Phase 3), dual / ×2 / locked slot / spring / sealed / chain with sand flow (Phase 4), `Hammer`, `SwapTarget`, `Stash` (Phase 5) |
| Game `Core/` [built] | `GameController` (composition root: `Awake` calls `Init` on `PhasePlayer` and `DragInput` and subscribes to the drag and overlay events; owns the `BoardState`: `Restart`, `TryMove`, `Revive`, `ShowAll`, `IsBusy`; `PreviewLayout()` (public, Edit Mode only, called by the editor menus) builds the slot, queue and strip objects in Edit Mode), `GameManager` (singleton holding `GameConfig`, S48) |
| Game `Config/` [built] | `GameConfig` (rules, feel, input, capacity), `LevelConfig` (`rows`, `cols`, `colors`, `seed`, `activeSlots`, `queueMax`, `qOpenStart`, `qGrow`, `number`, targets) and `LevelConverter` (checks each `cap` against `capMin`..`capMax`) |
| Game `Board/` [built] | `BoardView` (a `BlockPool` over the pre-made `Block` objects; `Build`, `ShowState`, `ShowLineOffset`, `ShowBlock`, `TryGetCell`, `Matches`, `ColorOf` from the `ThemeSkin` palette; the `Pre-create Blocks` command and an Edit Mode preview), `BlockView` (`Bind(id, colour, icon)`, `SetScaleFactor`), `BlockPool` |
| Game `Targets/` [built] | `SlotsView` (flat slots or skinned baskets; `SlotLocal/World`, `PipLocal/World`, `BeginPop` / `SetPop` / `EndPop` for the slot animations, `Matches`), `QueueView` (chairs and units; `ChairWorld`, `UnitWorld`, `HideUnit`, `Matches`; the overflow anchor and plank, `Build(chairCount, size, GameConfig)`), `StripView` (upcoming targets as shelf chips), `SquarePool` (scene objects `Pool_*`: `Obtain`, `Warm`, `Fit`, `Stretch`, `Peek`, `Hide`, `HideFrom`) |
| Game `Playback/` [built] | `PhasePlayer` (`Init(board, slots, queue, strip, config, showProgress)`, `PlayMove`, `SnapBack`, `PlayRevive`, `CancelFlights`; animates `Shift`, `Clear` with its flights, `Complete`, `Pull`, `Fall`; the flight icons are a `Pool_Flights` created at run time; `End` needs no animation, the controller opens the overlay; timings come from `GameConfig`) |
| Game `Input/` [built] | `DragInput` + `LineDrag` |
| Game `Layout/` [built] | `StageLayoutData` (a ScriptableObject per board size: the six anchor positions and scales, the sizes of the Stage views and an item layout of every other object below the anchors, S58), `StageLayout` (the six anchor references and the data; `Measure(rows, cols)` returns the Stage rectangle and moves nothing; `Apply` puts the anchors where the data says and does nothing in Play mode), `CameraFit` (`ExecuteAlways`; `Fit(Rect)`, `Compute`, refit in `Update`, an editor tick in Edit Mode) |
| Game `Theme/` [built] | `ThemeSkin` (sprites per colour, palette, `BasketLayout`, queue and basket sprites) |
| Game `UI/` [built] | `HudView` (`ShowLevel`, `ShowTargetsLeft`), `EndOverlayView` (`Show`, `Hide`, `ReviveClicked`, `RestartClicked`) |
| Editor [built] | `SlideJamMenu` (the menus *Slide Jam > Setup Scene*, *Refresh Layout* (S52) and *Save Stage Layout* (S58)), `StageLayoutTool` (finds or creates the `StageLayoutData`, saves the anchor positions), which call `SceneBuilder` (`ApplySpriteQuality`, `BuildArt`, `RefreshBackdrop`), `OverlayBuilder` (`Build`) and `OverflowBuilder` (`Build`); those three have no menu of their own; assembly `SlideJam.Editor`. The component context menus *Pre-create Blocks* (`BoardView`) and *Fill Reference Level 1* (`LevelConfig`) remain |
| Game [planned] | `BoosterBar`, `TutorialOverlay`, `TextTable` (display names), `SaveData` (tutorial-seen flags, booster counts, level reached) |
| Tests [Edit Mode built] | `Engine/Rng` (`Mulberry32Tests`, `ColumnStreamTests`), `Engine/Rules` (`MatcherTests`, `BoardTests`), `Engine/State` (`BoardStateTests`), `Engine/Moves` (`MoveResolverTests`, `ResolutionTests`, `EndTests`, `ReviveTests`), `Engine/Invariants` (`InvariantTests`), `Game/Config` (`GameConfigTests`, `LevelConverterTests`), `Game/Layout` (`CameraFitTests`, `StageLayoutTests`), `Game/Targets` (`SquarePoolTests`), `Helpers` (`TestGrids`, `TestLevels`). Planned: `GameManagerTests` (offered, not added), boosters, per-mechanic rules; Play Mode: a real drag against the engine's prediction |

`GameManager` is the only singleton (S48). Prefer plain objects owned by `GameController` over `static` singletons; any other static must be reset on scene load (`GameManager` resets `Instance` in a `RuntimeInitializeOnLoadMethod`).

---

## 7. Game flow and persistence

- **Run lifecycle [built]:** `GameController.Start` calls `Restart`, which builds a `LevelData` from the `LevelConfig` (`ToLevelData(GameConfig)`, with the `cap` range check) and `BoardState.New`, builds the views and shows everything (`ShowAll`: board, slots, queue, strip, targets left). A drag raises `Dragging` and `Released`; the controller previews the line and calls `TryMove` (`MoveResolver.Move`), applies the new state at once and starts `PhasePlayer`, keeping `DragInput` disabled (`IsBusy`). When the animation ends `BoardView.Matches` checks the board on screen against the state, and `SlotsView.Matches` and `QueueView.Matches` check the slots and the queue (each logs an error if they differ), then `ShowAll` redraws everything. Restart also calls `PhasePlayer.CancelFlights`. If `State.Ended` is set the end overlay opens and the drag stays off. A refused move snaps the line back.
- **Endings [built]:** `EndReason` = `Win`, `QueueOverflow` (append-only). The settle runs before a run ends; a win resolves every match first (`DESIGN.md` §5.4, §5.7).
- **Revive and Restart [built]:** the loss card offers Revive: `GameController.Revive` → `MoveResolver.Revive` (queue emptied, grid shuffled, `Ended` null) → `PhasePlayer.PlayRevive`. It is free and unlimited in the first build (`CanRevive` is always true); it will cost `reviveCost` coins, 0 until the coin economy exists (S55). Restart (the bottom button or the card) stops a running animation, hides the overlay and rebuilds the state. There is no undo and no stack of states (S42); a **used booster is never refunded** (`DESIGN.md` §5.7, §5.9).
- **Persistence [planned, keys not chosen]:** tutorial steps already shown (`TutorialId`), the player's booster counts, the level reached. Nothing from the engine state is saved mid-run. Currency and prices are product settings, **OPEN** (§12).

---

## 8. Input, UI and tutorial

- **Input [built]:** `DragInput`, one pointer path (`Input.GetMouseButton*`; a touch works through the simulated mouse). On press it ignores the pointer when it is over uGUI (`EventSystem.IsPointerOverGameObject`, with the finger id on touch); the controller disables the component while a move plays and after a run has ended, and `OnDisable` drops a half-made drag. Axis lock after `GameConfig.lockDistance` (8) logical units of the `referenceWidth` (360), `notches = round(offset)`. Snap and snap back are played by `PhasePlayer` (timings in `GameConfig`). The locked-line flash is planned (Phase 4). Rules: `DESIGN.md` §4.1.
- **Preview [planned, Phase 3]:** runs the real move on a copy at every pointer move; white / orange outlines and the overflow warning. `DESIGN.md` §4.2.
- **Tutorial [planned]:** overlay steps shown once, stored in save data, listed by append-only `TutorialId` in a data asset: `Slide`, `Targets`, `Queue` at game start; `Dual`, `X2`, `LockedSlot`, `QueueGrow`, `Chain`, `Seal`, `Spring` at the first level that has the mechanic; `Hammer`, `SwapTarget`, `Stash` when the booster is first offered. `DESIGN.md` §5.8.
- **Layout [partly built]:** portrait, 360 logical units reference width, top to bottom: slots, next strip, queue and vault, overflow row, next-spawn dots, grid, booster bar, restart (`DESIGN.md` §4). Built: baskets (slots), the next strip (shelf chips on a plank), the queue (chairs, fruit units), the overflow row (a small plank under the queue that appears only while a unit stands past the last chair, S51), a decoration branch, and the grid with its panel and vines. Planned: the vault, the next-spawn dots (they hang on the branch), the booster bar.
- **HUD [built]:** a Screen Space Overlay canvas (Canvas Scaler 360 × 640, match width): `HudView` shows `Level {n}` (`LevelConfig.Number`) and `Targets left: {n}`; the Restart button (OnClick → `GameController.Restart`); `EndOverlayView` (win and loss card, Revive on a loss). The move counter is no longer shown. The strings are English placeholders until the text table exists: the default TMP font (LiberationSans SDF) lacks some Vietnamese glyphs (Q13).
- **Camera [mostly built]:** `CameraFit.Fit(Rect)` fits the Stage rectangle (width first, height if taller) with `_margin`, `_padTop` and `_padBottom`, and refits when `Screen` changes size, also in Edit Mode (it follows the Game view size through `Handles.GetMainGameViewSize`, with the rectangle that `GameController` pushes as a preview bound). The camera size saved in the scene is only a preview value that Play mode recomputes. `Screen.safeArea` (a notch) is not handled (Q15).

---

## 9. Content status: levels, boosters, theme

| Item | Status |
|---|---|
| Level 1 "Vườn nhỏ" (6×6, no obstacles) | `LevelConfig_1.asset` (6 × 6, 5 colours, seed 20261031, 12 targets with colours 0 1 2 3 4 1 0 3 2 4 0 1 and `cap` 3, `queueMax` 5, `qOpenStart` 5, no `qGrow`, number 1; values from the `Fill Reference Level 1` command, the asset was not re-read). Built and playable to a win or a loss; solvability not checked (Q2) |
| Level 2 "Vườn ươm" (6×6, ×2, dual, locked slot, growing queue) | specified; no asset |
| Level 3 "Nhà kính" (7×7, adds chain, sealed block, spring) | specified; the special block cells of the prototype are **not** recorded in `DESIGN.md` v0.4 (the level-data section was dropped), so Level 3's block layout must be re-decided **OPEN** (§12) |
| Boosters Hammer, Swap target, Stash | rules specified (`DESIGN.md` §5.9); no code. Prices, currency and free uses are undecided product settings |
| Fruit set | decided in `DESIGN.md` §9 (cherry, blueberry, pear, orange, plum; the cherry replaced the strawberry, S53). The art is now in the repository: `Assets/Art/Sprites/fruits` plus baskets, shelf chips, queue slots, board panel, vines and UI, wired by `ThemeSkin`. The colour-vision silhouette check is not done (Q3). Colour order in the build: 0 cherry, 1 blueberry, 2 pear, 3 orange, 4 plum |
| Audio | none; the first build has no sound |
| Fonts | only the default TMP font (LiberationSans SDF) is imported; Nunito (text) and Fredoka (headings) are planned |

---

## 10. Tuning values

All numbers are in `DESIGN.md` §5.1 (global table) and §5.9 (boosters). The live values are in the assets named below; the values listed are the C# defaults, which are also what the assets held when this file was written (the `GameConfig` asset was not re-read after the new fields were added):

- `GameConfig` (`Assets/Resources/Configs/GameConfig.asset`): **Rules** `capMin` 3, `capMax` 5. **Feel** `snapSeconds` 0.13, `snapBackSeconds` 0.14, `clearSeconds` 0.17, `pulseScale` 1.15, `fallBaseSeconds` 0.16, `fallPerCellSeconds` 0.09, `reviveSeconds` 0.35, `flightSeconds` 0.36, `flightStaggerSeconds` 0.04, `flightEndScale` 0.45, `slotPopSeconds` 0.26, `slotPopScale` 1.12, `slotEnterSeconds` 0.26, `overflowUnitScale` 0.8, `overflowFadeSeconds` 0.15. **Input** `lockDistance` 8 (on a `referenceWidth` of 360). **Capacity** `queueUnitBuffer` 6, `stripVisible` 5, `flightPoolSize` 16. Still to come: `swapMin` 2, `reviveCost` (planned, 0 for now, S55), booster prices and free uses (undecided). Held by `GameManager`; read by `GameController`, which passes the config to `PhasePlayer` and `DragInput` (`Init`), to `QueueView.Build` and the capacity values to `StripView.Build`.
- `ThemeSkin` (art-coupled values, not rules): the 5-colour `palette` (read through `BoardView.ColorOf`, which now needs the skin) and the basket layout ratios (`InteriorWidth` 0.6, `PipY` 0.053, `PipWidth` 0.213, `PipFill` 0.95, `FruitInPip` 0.9).
- `LevelConfig`: `rows`, `cols`, `colors`, `seed`, `activeSlots`, `queueMax`, `qOpenStart`, `qGrow`, `number` (display only) and the target list `{color, cap}`; planned: `lockedSlotIndex` and special blocks.

A `cap` outside `capMin..capMax` is a data error and must fail on load.

**Layout values (S58, which replaces the layout part of S47).** `Assets/Design/StageLayoutData.asset` holds the six anchor positions (written by *Save Stage Layout*) and the sizes: slots, strip and queue band heights (1.6, 0.6, 0.9), slots extra width 0.4, slot gap 0.05, basket overflow 0.3, basket offset Y −0.2, vault box width 1.5 and gap 0.12, next-spawn dots near 0.4, far 0.3, far alpha 0.6, bottom pad 0.1, dot gap 0, hanger width 0.08 (the defaults of the class; the positions are the scene's at the time the asset was created). **Values that stay on scene components by decision (S47):** `CameraFit` margin and pads; `BoardView._preMadeBlocks` 49 (an authoring value used by the Edit Mode layout). The flat-mode colours of `SlotsView` and `QueueView` (the fallback when there is no skin). The block scale 0.96 on the `Block` prefab. Literal ratios inside `QueueView` and `StripView` (chair and unit size, chip size). The engine constants of the Revive shuffle (`Shuffler`: 104729 and 100 attempts, `DESIGN.md` §5.7.1), `MaxRounds` 500 and the sandbox numbers (`LevelData.Sandbox`, used by tests).

---

## 11. Tools and testing

| Tool | Where | Use |
|---|---|---|
| Unity Test Framework 1.1.33 | `Assets/Tests/EditMode` (101 test cases by count of the source: 96 `[Test]` methods plus 5 `[TestCase]` rows; the user reported earlier runs passing, the latest run was not reported), `Assets/Tests/PlayMode` (none yet); Window > General > Test Runner | the engine is pure C#, so rule and invariant tests run in Edit Mode |
| Batch mode | see `AGENTS.md` §6 | `Unity.exe -batchmode -nographics -projectPath . -runTests -testPlatform EditMode ...`; the Editor must be closed |
| Unity CLI / MCP | whatever the session provides | inspect scenes, prefabs and the Console; refresh and wait for the recompile after script edits |

Testing approach:

- Put every rule in the pure engine and test it headless. Tests read numbers from configs and do not pin balance values.
- Planned invariant tests: preview equals result (I5); a target of `cap` 3, 4 and 5 completes at exactly its own `cap` (built); after any booster use: no hole, the multiset of `Seq` unchanged, `(Seq.Length − SeqI) + open slots = total − Done`, no stream pointer moved, no unit added to the queue, an earlier held state unchanged and the use not refunded. Built: no hole after any move, the one that ends the run included (I9), and the same seed and moves give the same `Hash()` (I4).
- Covered by the Phase 2 engine tests (70 cases in all; the list after this one is the Phase 0 and 1 part): unit routing in row-major order and the round rule (round 1 queues, later rounds vanish), locked and full slots skipped, a slot taking units up to its own `cap` and completing at exactly `cap` 3, 4 and 5, pull order (vault, then queue) with chained completions, `qGrow` (opens a chair only when on, never above `queueMax`), overflow loss, win, a rescue by a pull and a rescue by an opened chair, the same move losing without the chair, a finished run refusing moves, unit conservation, target accounting `(Seq.Length − SeqI) + open slots = total − Done`, replay from a held state, end states in a sweep. Three sweep asserts are statistical lower bounds (`queued`, `cascadeClears`, `vanished` and `ended` greater than 0), so they prove the rule was exercised, not that a given rate holds.
- Covered in Phase 0 and 1 (38 tests): `ColumnStream` range, determinism, column separation and colour use; the closed-form generator against a stepwise reference; matcher runs; slide wrap, direction and floor-mod; grid generation (full, unique ids, never a match over 60 seeds and two sizes); `Clone` independence and `Hash`; `Move` (zero shift refused, input not mutated, two hand-calculated scenarios, full and match-free board after every move, spawns equal clears, round numbering, determinism, same result after "undo").
- Covered after the Phase 2 engine (all Edit Mode): `EndTests` (a loss settles and then ends; a win settles and resolves every match before `End`), `ReviveTests` (queue cleared, same blocks, no match, nothing else changes, deterministic, refused unless the run was lost to the queue, a 60-seed sweep), `GameConfigTests` (defaults), `LevelConverterTests` (the `cap` range check; every shipped level converts and starts without a match), `CameraFitTests` and `StageLayoutTests` (bounds), `SquarePoolTests` (warm, get, fit, adopt).
- The view has no automated test beyond the pure helpers above. It is checked by hand in Play mode, and `GameController` runs `BoardView.Matches(State)` after every move and logs an error if the screen differs from the state. Boosters and the singleton behaviour of `GameManager` have no test (Q21).
- No bot and no solver exist. How solvability is checked (I11) is **OPEN** (§12).
- There is no player playtest data. Any difficulty claim must be labelled arithmetic, simulation or observed run.

---

## 12. Known issues (open)

Only genuine defects and undecided questions live here. When you fix one, move it to §13 in the same commit.

| # | Sev | Issue | Where | Fix / owner |
|---|---|---|---|---|
| Q1 | High | **Level 3's special block layout is lost**: `DESIGN.md` v0.4 dropped the level-data section, and the prototype is not in the repo. Level 3 cannot be built as specified | `DESIGN.md` §5.1 | decide the cells, or restore them from the prototype |
| Q2 | High | **How to check solvability (I11)** is undecided now that the balance section is gone | `DESIGN.md` I11 | decided (S54): stored solution for a fixed seed, else a bot; built after Phase 4 (roadmap 4b) |
| Q3 | Med | The fruit art pack is in the repository (cherry, blueberry, pear, orange, plum, S53), but the silhouette check (colour-vision) has not been done | `DESIGN.md` §9 | check with a simulated colour-vision deficiency |
| Q4 | Med | Coin prices, the free booster uses and any limit on Revives are undecided; the coin economy is built before the boosters and Revive gets a price from it (S55) | `DESIGN.md` §5.7, §5.9 | product decision |
| Q5 | Med | `unityyamlmerge` driver not configured in this clone | `.git/config` | run the setup before the first scene / prefab merge |
| Q6 | Low | Application identifier is the placeholder `com.DefaultCompany.2DProject`; company name `DefaultCompany` | Project Settings | ask before changing (Project Settings) |
| Q7 | Low | Linear colour space and scripting backend not reviewed for mobile | Project Settings | review at the first device build |
| Q8 | Low | Ownership of areas is not recorded (§4) | this file | ask the team |
| Q9 | Low | `DESIGN.md` §1 says "about 20–40 slides" per level as a design target; nothing has measured it on the Unity engine | `DESIGN.md` | re-state when levels exist |
| Q10 | Low | No golden values from the prototype exist for `Mulberry32` / `ColumnStream`: the tests prove closed form == stepwise form, not equality with the prototype (see S12) | `Assets/Tests/EditMode/Rng/` | add golden values if the prototype is found |
| Q11 | Low | While a line is dragged, a block jumps to the other edge when its centre crosses the edge (no ghost copy at the seam) | `BoardView.ShowLineOffset` | polish (level 2 of the view work) |
| Q12 | Low | Animation curves and timings are first guesses; a refused move only snaps back (no shake or hint) | `PhasePlayer` | tune in Play mode; the values now live in `GameConfig` (Feel) |
| Q13 | Low | HUD text is English and the default TMP font lacks some Vietnamese glyphs; the display text table does not exist | `HudView`, TMP | choose a font with Vietnamese support, build the text table (Phase 6) |
| Q15 | Low | `Screen.safeArea` (a notch can cover the HUD at the top) is not handled; the layout-aware camera fit is built (S34) | `CameraFit`, `Canvas` | a safe-area fitter before a device build (Phase 6) |
| Q17 | Low | The `*_9slice*` sprites (button, card, tab, bubble, board panel) have no Border set, so the end overlay draws them as plain stretched images | `Assets/Art/Sprites/ui`, `OverlayBuilder` | set the borders in the Sprite Editor, then switch the images to Sliced |
| Q18 | Low | The `Sky` and `Plank` objects of the `Backdrop` are static: the menus compute their positions once, so a change to the layout numbers needs *Refresh Layout* (the views clear no decoration because the pools own their own containers) | `SceneBuilder.RefreshBackdrop` | run *Refresh Layout*, or make them follow the layout |
| Q19 | Low | Only the 6 × 6 board art is wired (PPU 216). A 7 × 7 level needs the 192-PPU art and a 0.89 scale of the tile and the fruit | `SceneBuilder`, `BlockView` | when a 7 × 7 level exists (Level 3) |
| Q20 | Low | Revive is free and unlimited in the first build (`CanRevive` is always true) and `reviveCost` is not in `GameConfig` yet (S55) | `GameController` | add `reviveCost` (0) with the coin economy (roadmap 4c) |
| Q21 | Low | No test covers `GameManager` (the singleton, the duplicate guard, the reset) nor the pool adoption after a scene reload nor the end overlay nor the flights and the overflow row: the project has no Play Mode test assembly, and the user asked for no new test files for now (2026-11-09) | tests | add `Assets/Tests/PlayMode` and `GameManagerTests` when the user asks |
| Q22 | Low | Flights from or to the vault have no view: `PhasePlayer` draws them from the first queue chair and does not change the vault. The vault is Phase 5 | `PhasePlayer.WorldOf`, `Land` | build the vault view with the Stash booster |
| Q23 | Low | The overflow plank width is a constant (5 units) in `OverflowBuilder`, which assumes at most 6 overflow units at spacing 0.8; a different `queueUnitBuffer`, `overflowUnitScale` or board width needs a re-tune and a re-run of the menu | `OverflowBuilder`, `QueueView` | when a level or the buffer changes |

---

## 13. Fixed issues ledger

| # | Fixed | What | Where |
|---|---|---|---|
| — | 2026-11-03 | Default orientation was Auto Rotation; set to Portrait with the other auto-rotate options off | `ProjectSettings/ProjectSettings.asset` |
| — | 2026-11-04 | `ColumnStreamTests` (5 tests) was missing after Phase 0; it is now in the repository | `Assets/Tests/EditMode/Board/ColumnStreamTests.cs` |
| Q14 | 2026-11-05 | The editor-generated files `ProjectSettings/SceneTemplateSettings.json` and `Assets/TextMesh Pro/` are committed (they are in `git ls-files`; the tree was clean at `e56dae2`) | repository |
| — | 2026-11-09 (recorded) | After Restart on a run that had ended, the drag stayed disabled: `SetBusy(false)` ran before the new state replaced the old `Ended` one. It now runs after `State` is set | `GameController.Restart` |
| — | 2026-11-09 (recorded) | The objects made by *Preview Layout* carried `DontSave` flags and vanished when Play started. The pools are now real scene objects adopted at run time (S46) | `SquarePool` |
| — | 2026-11-09 (recorded) | The `Board` sat 2.075 units above its anchor after being dragged under `Grid` (the old world position was kept as a local offset). `StageLayout.Apply` resets the children of the `Grid` anchor | `StageLayout` |
| — | 2026-11-09 (recorded) | The board was empty at the start and the first move logged "the board on screen differs": the controller had not shown the state; `ShowAll` shows the board, slots, queue and strip | `GameController` |
| Q16 | 2026-11-09 | Cherry or strawberry: **the cherry stays** (S53). `DESIGN.md` §9 and `AGENTS.md` now name the cherry (display name Anh đào); the colour order in the build is final | `DESIGN.md` §9, `AGENTS.md`, `ThemeSkin` |

---

## 14. Roadmap

Suggested order, following `CLAUDE.md` §5 (engine first, then view, then data, then docs). **Treat as a proposal; the team sets priorities.**

| Phase | Content | Status |
|---|---|---|
| 0 and 1 | `BoardState`, `ColumnStream`, `Matcher`, shift, clear, `SandSettler`, `Move`; the board view, drag, phase player, camera, block pool | done |
| 2 | Targets, queue, win and loss; `GameConfig` / `LevelConfig`; Stage and views; end overlay with Revive | **tasks 1 to 9 done**, overflow row (task 10) written; closing Play pass open (below) |
| 3 | Drag preview (the real move on a copy), next-spawn dots | not started; next |
| 4 | Mechanics: ×2, dual, locked slot, spring, sealed block, chain with sand flow; a test each (I7 exits) | not started |
| 4b | Solvability (S54): stored solutions for fixed-seed levels, a greedy bot then beam search for the rest, built on the real `MoveResolver.Move`; decide then whether a solution is saved in the level data | not started; after Phase 4 |
| 4c | Coin economy (S55): the coin balance and its save data, then the Revive price (`reviveCost`, 0 until now); prices and free uses are still to be decided | not started; before Phase 5 |
| 5 | Boosters Hammer, Swap target, Stash with `Seq` / `Pre` / `Vault` in the snapshot and the booster invariant tests; a bot simulation with boosters afterwards (S54) | not started |
| 6 | Theme and UX: text table and fonts, tutorial, 9-slice UI, safe area, basket labels, vault, polish | not started |
| 7 | Content and build: Levels 2 and 3 (Q1), solvability of every level, device build, audio | not started |

### Phase 2 plan: targets, queue, win and loss

Goal: Level 1 can be played from start to a win or a loss, with Revive and Restart. Order matters: each task is finished and checked before the next. **The rules changed during the work: Undo became Revive (S42) and the settle always runs before the end (S40, S41).**

| # | Task | State |
|---|---|---|
| 0 | Docs for the decisions | done |
| **Engine** | | |
| 1 to 5 ✔ | Data model, unit routing, `Complete` and `Pull`, overflow and win checks, the `End` phase, the invariant tests | done, tested |
| 5b ✔ | The end rule of S40 and S41 (settle first, a win resolves every match) and `MoveResolver.Revive` with its shuffle | built; tests written (`EndTests`, `ReviveTests`, `InvariantTests`) |
| **Game** | | |
| 6 ✔ | `GameConfig` and `LevelConfig` assets, Level 1, the conversion with the `cap` range check | done, tested |
| 7 ✔ | Stage with anchors, layout-aware `CameraFit`, slot, queue and strip views, targets-left HUD, then the sprite pack through `ThemeSkin` | built; checked in Play mode by the user |
| 8 ✔ | End overlay (win, loss), **Revive** and Restart | built; Play mode check open (§16) |
| 9 | `PhasePlayer` plays `Clear` flights (grid to slot, queue or vanish), `Complete`, `Pull` and `End`; the slots and queue are drawn from the previous state until each flight lands; `BoardView.Matches` also checks slots and queue; the flight and slot timings go into `GameConfig` | ✔ built; the user ran it in Play mode and reported it fine (S50) |
| 10 | Overflow row: units past the last chair stand on a small plank under the queue; `StageLayout` band and `queueLift`, `QueueView` anchor and plank, `GameConfig` scale and fade, the scene wiring | ✔ built (S51); the user checked it in the Editor and reported it fine |

### Next steps (proposal)

1. **Run the new menus once** (S52): *Slide Jam > Setup Scene* on the current scene, check that nothing changed, save.
2. **Close Phase 2:** a full Play mode pass (win, loss with units on the overflow plank, Revive, Restart during an animation, a scene reload), run all Edit Mode tests, then merge `GameLoop` into `main` (the team's call).
3. **Product decisions made on 2026-11-09:** the cherry stays (S53), the solvability approach (S54), the economy before the boosters and a priced Revive (S55), the config folder kept (S39). Still open: coin prices, free uses, the number of Revives if any (Q4).
4. **Phase 3:** the preview, the next-spawn dots (the branch and hangers art exist; the grid moves down about 15 to 20 px to make room for them), and the I5 guarantee for the queue consequence of a move.
5. **Phase 4** mechanics, then **solvability** (4b: every shipped level needs it, I11), then the **coin economy** with the Revive price (4c), then **Phase 5** boosters.
6. **Later, outside the phases:** a level generator and JSON level data (their relation to `LevelConfig` and I10 is to be decided then).
7. **Phase 6** polish: 9-slice borders (Q17), the `n/cap` label, the vault, the text table and fonts (Q13), the safe area (Q15), a tutorial; **Phase 7** content (Level 3 layout, Q1) and a device build (Q6, Q7).
8. **Housekeeping:** add `GameManagerTests` and a Play Mode assembly (Q21); make the backdrop follow the layout (Q18); 7 × 7 art when Level 3 starts (Q19).

Not built, proposed as later content (`DESIGN.md` §5.6): one-way rows, heavy blocks, a second slot-lock type, bigger grids, moving obstacles. Also later: a Revive limit or price, a tutorial spread over more levels, a seeded daily level.

---

## 15. Decision log

Condensed; newest last. The rule decisions behind the prototype are in `DESIGN.md` (§2, §5.4, §5.9, §5.12 for rejected mechanics). The dates of S39 to S49 are the day they were recorded here; the work itself spanned several days after 2026-11-05.

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
| S9 | 2026-11-03 | Fruit set: strawberry, blueberry, pear, orange, plum; theme is display only | the strawberry was replaced by the cherry (S53); the rest is final |
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
| S35 | 2026-11-05 | `LevelData.Sandbox` is a public engine factory, not a test helper: the Phase 1 view needed a level that cannot end until `LevelConfig` existed, and the old sweep tests use it too | superseded in use: the view reads `LevelConfig` since Phase 2 task 6; `Sandbox` stays for tests |
| S36 | 2026-11-05 | An empty slot is `Slot.Empty` (`Cap == 0`), not `null`, like `Block.Empty`; `Slots` is a `Slot[]` | final |
| S37 | 2026-11-05 | Flights use `Place {Kind, A, B}` with `PlaceKind` `Grid`, `Slot`, `Queue`, `Vault`, `Vanish`; a pulled unit's `From` index is its index in the list before the pull. `Phase` gets `Flights`, `Vanished`, `Slot`, `Reason` | final |
| S38 | 2026-11-05 | A completion opens a queue chair only when `LevelData.QGrow` is on (as the §5.1 table says); a `Pull` phase is emitted only if a unit moved | final |
| S39 | 2026-11-09 | `GameConfig` and `LevelConfig_1` live in `Assets/Resources/Configs/` (the user kept that folder); `ThemeSkin` is in `Assets/Design/`. `DESIGN.md` §6.2 lists both | final: kept as it is (decided 2026-11-09) |
| S40 | 2026-11-09 | The settle (`fall`) always runs before a run ends. A lost move stops right after it (no further round, so a lost board may keep a match); a won move keeps resolving rounds until no match is left. `End` is the last phase. This changes the locked order of `DESIGN.md` §5.4 and I9 at the user's explicit request | final |
| S41 | 2026-11-09 | `Resolution.CheckEnd` only sets `Ended` (the first reason wins); `MoveResolver` emits the `End` phase | final |
| S42 | 2026-11-09 | **Revive replaces Undo.** Not a move; only after `QueueOverflow`; clears the queue, shuffles the grid, keeps everything else; unlimited in the first build; there is no snapshot stack | final |
| S43 | 2026-11-09 | The Revive shuffle: its own seeded stream `Level.Seed + Moves × 104729 + 1`, Fisher–Yates over the cells in row-major order, up to 100 attempts for a grid with no match, plain blocks only (Phase 4 keeps special blocks in place). Proposed by the agent, approved by the user | final |
| S44 | 2026-11-09 | The HUD shows the level number and the targets left; the move counter is removed; `LevelConfig.Number` is display only | final |
| S45 | 2026-11-09 | The Stage gets an optional `Branch` band; art comes from a sprite pack through `ThemeSkin` (colour index to fruit art); Pixels Per Unit are set per sprite group so that a grid cell is one world unit | final; the fruit order is final (S53) |
| S46 | 2026-11-09 | Slots, strip and queue are drawn by `SquarePool`s: real scene objects (`Pool_*`) created in Edit Mode by *Preview Layout* and adopted at run time; `SquarePool.TrackEdits` lets tests skip undo and the dirty flag. Supersedes the transient `DontSave` objects | final |
| S47 | 2026-11-09 | `GameConfig` holds the rule limits, the Feel timings, the Input values and the Capacity buffers; `ThemeSkin` holds the palette and the basket layout ratios; the layout metrics, `_preMadeBlocks`, the flat-mode colours and the block scale stay on scene components or prefabs | the layout metrics part is superseded by S58; the rest is final |
| S48 | 2026-11-09 | `GameManager` is a scene singleton that holds `GameConfig`. `GameController` reads it through a serialized reference (so Edit Mode works) and injects the config into the views; views never call `Instance`; no `DontDestroyOnLoad`; scope is the config only, the game loop stays in `GameController`. Supersedes the "no static singletons" part of S21 | final; not tested in Play mode (Q21) |
| S49 | 2026-11-09 | Scene setup that cannot be done safely by hand is done by idempotent editor code (`SceneBuilder`, `OverlayBuilder`, `OverflowBuilder`); scene and prefab YAML is not edited by hand | final |
| S58 | 2026-11-11 | **Stage layout is data.** The position and size fields are removed from `StageLayout`, `SlotsView`, `QueueView`, `NextSpawnView` and `GameController`; a `StageLayoutData` asset (one per board size, `Assets/Design/StageLayoutData.asset`) holds them. The anchor positions are written by the menu *Slide Jam > Save Stage Layout* (a third menu item, on request) and put back by *Setup Scene* and *Refresh Layout*; the first *Setup Scene* creates the asset from the positions already in the scene. `StageLayout.Apply` does nothing in Play mode, `GameController.OnValidate` no longer moves anchors, and `Measure` gives `CameraFit` its rectangle. The gap, branch height and dots height fields are gone because saved positions make them unnecessary. `StageLayoutTests` was rewritten for the new rectangle (the Grid anchor is the board's centre, so an extra row lowers the bottom by half a cell). Limit: the saved positions are for one board size, so a 7 × 7 level needs its own data asset. **Item layout (added the same day):** *Save Stage Layout* also writes, for every object below the six anchors (the `Pool_*` squares, the backdrop art, the vault box, the preview label; the board's own blocks are skipped), its local position, scale, rotation, `RectTransform` size and text size, keyed by its path under the Stage; *Setup Scene* and *Refresh Layout* put them back as their last step, so what the builders and the views compute in Edit Mode no longer overwrites a hand-tuned scene. If the data has no item layout yet, the first menu run saves the scene as it is before changing anything. **In Play mode the views still compute the position and scale of the pool squares from the state** (a scale depends on the sprite drawn), so a hand-tuned pool square shows in Edit Mode only | final; not run yet |
| S52 | 2026-11-09 | The editor tools are reduced to **two menus** in `SlideJamMenu`: *Setup Scene* (everything, in dependency order, one undo step) and *Refresh Layout* (layout, backdrop and preview objects only). The old menus *Build Scene Art*, *Apply Sprite Quality*, *Build End Overlay*, *Build Overflow Row* and the `GameController` context menu *Preview Layout* are gone; their code stays as internal methods. `GameController.PreviewLayout()` is public (Edit Mode only) so the menu calls it without reflection | final; not run yet |
| S53 | 2026-11-09 | The **cherry** stays as colour 0 in place of the strawberry (display name Anh đào); the colour order 0 cherry, 1 blueberry, 2 pear, 3 orange, 4 plum is final. The colour-vision silhouette check is still to do (Q3) | final |
| S54 | 2026-11-09 | **Solvability (I11).** A level with a fixed seed is proved by a stored **solution** (a move list replayed on the engine); a level without one is checked by a **bot** (greedy first, then beam search) on the real `MoveResolver.Move`. A bot failure is a warning, not a proof. Decided after Phase 4 whether the solution is stored in the level data (a change to `queueMax`, `cap`, the targets or the special cells then invalidates it); a simulation with boosters follows Phase 5. A level generator and JSON level data come later and their relation to `LevelConfig` and I10 is decided then | final; not built |
| S55 | 2026-11-09 | **Economy before boosters; Revive has a price.** The coin economy (balance, save data) is built before Phase 5 and the Revive price (`GameConfig.reviveCost`, 0 until then) uses it; booster prices and free uses stay open (Q4). `CanRevive` will mean "the player can pay" | final; not built |
| S50 | 2026-11-09 | **Flights.** `PhasePlayer` plays `Clear` flights (grid to slot, queue or vanish), `Complete` (slot pop and enter) and `Pull` (queue to slot). It keeps its own copy of the state and applies each flight when its unit lands, so slots and queue are drawn from the state before the phase until then (I5); after the flights it redraws them from the phase's own state, and `SlotsView.Matches` / `QueueView.Matches` check the screen after the move. Flight icons are a run-time `Pool_Flights` (not saved in the scene). The timings are in `GameConfig`. Vault flights are ignored until Phase 5 | final; run in Play mode by the user, no automated test (Q21) |
| S51 | 2026-11-09 | **Overflow row.** Units past the last chair (`queueMax`) stand on a small plank under the queue (the shelf plank sprite, reused), in fixed places, scaled by `overflowUnitScale`; the plank fades in only while such a unit exists. `StageLayout` reserves an optional `Overflow` band under the queue and lifts the queue by `queueLift`. The long branch stays free for the next-spawn dots (Phase 3). Considered and rejected: moving the queue onto the branch, and putting the overflow on the branch (both collide with the dots) | final; the scene wiring was checked by the user in the Editor |

---

## 16. Verification gaps

Everything in this file marked `[verified]` was read from a file. Not checked:

| Gap | How to check |
|---|---|
| The project opens and compiles in Unity 2022.3.62f3 with the current packages | open in the Editor; read the Console |
| The Portrait setting survived (the Editor was open when the file was edited and may rewrite it) | Project Settings > Player > Resolution and Presentation |
| `unityyamlmerge` availability | `git config --get merge.unityyamlmerge.driver` and the Unity install's `UnityYAMLMerge` |
| The latest Edit Mode run (101 cases by count of the source) after the end-rule change, `Revive`, `GameConfig`, `ThemeSkin`, `SquarePool` and `GameManager`: the user reported earlier runs passing, not the latest | Test Runner > EditMode > Run All |
| Phase 1 was merged to `main` at `e56dae2`; the user has not yet said that the pool, the Edit Mode preview and the Edit Mode camera (S24–S26) were checked after the merge. They stay unconfirmed until they do | confirm in the Editor, then change S24–S26 to verified |
| The overflow row (S51): the user checked the scene wiring in the Editor and reported it fine; the Play behaviour (the plank appears when a unit lands past the last chair, disappears after Revive and Restart, no "differs from the state" error) was not reported separately | Play mode |
| The two menus (S52): *Setup Scene* on the current scene changes nothing that is already right and logs no error; *Refresh Layout* puts the anchors where the `StageLayoutData` says, then moves the backdrop and the preview objects; *Save Stage Layout* writes the anchors and the item layout of the objects below them into the data (S58), and Setup Scene puts the item layout back; none of the three runs in Play mode, and Play mode does not move an anchor | run them in Edit Mode, then Play |
| The Play mode behaviour after the latest changes: a full win and a full loss, Revive (queue empty, board shuffled, play goes on), Restart after an ended run and in the middle of an animation, the `Pool_*` objects surviving a scene reload, the config read through `GameManager` (also with the component missing or without a `GameConfig`: the log must say so) | Play mode; there are no Play Mode tests |
| The sprite import settings (Pixels Per Unit, mip maps) and the scene and prefabs built by the editor menus were read only partly (the `.meta` PPU values yes; the Stage prefab and the Canvas children no) | read the prefab and the scene file |
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
| 2026-11-05 | Phase 2 engine (tasks 1 to 5) recorded as built and tested: §1 snapshot, §2 (I1, I4, I6, I7, I9), §6, §7, §10, §11, §14 (tasks ticked), §15 (S35 to S38), §16; `DESIGN.md` raised to v0.9 (§5.1, §5.4, §6, §6.1, §6.2). Also corrected the code map: `SandSettler` lives in `Moves/`. Written from the files in the repository; the test result is the user's report |
| 2026-11-09 | Docs brought up to date with the work after Phase 2 task 5 (branch `GameLoop` @ `3a26b2d`; written from the repository and the conversation, nothing was run): the end rule (settle first, a win resolves every match), Revive and its shuffle in place of Undo, the Phase 2 view as built (Stage, `ThemeSkin`, `SquarePool`, `GameConfig`, `GameManager`, HUD, end overlay), the sprite pack and the new folder layout. `DESIGN.md` raised to v1.0 (§2 I2 and I9, §4, §5.4, §5.7, §5.7.1, §5.9, §6, §6.1, §6.2, §7, §8, §9); `PROJECT.md` §1 to §3, §5 to §16, decisions S39 to S49 |
| 2026-11-09 | Phase 2 task 9 (flights, `Complete`, `Pull`, S50) and task 10 (overflow row, S51) recorded: §1 state, §5.5, §6, §7, §8, §10, §12 Q21 to Q23, §14, §15, §16. `DESIGN.md` §4, §7 and §9 updated (stage anchors, overflow row, animation timings and flights). Written by the agent from the delivered code; the user reported task 9 working in Play mode, nothing else was run (Unity was not connected) |
| 2026-11-09 | Overflow row reported fine by the user; editor tools merged into two menus (S52): §5.5, §6, §12 Q18, §14, §15, §16. `DESIGN.md` §6.2 and §7 follow. Written by the agent; not compiled, the menus not run |
| 2026-11-09 | Product decisions recorded (S53 to S55, S39 closed): the cherry stays, solvability by stored solution or bot after Phase 4, coin economy before the boosters with a priced Revive; §2 I11, §5.5, §9, §10, §12 (Q2, Q3, Q4, Q16 moved to §13, Q20), §13, §14 (rows 4b and 4c, next steps), §15. `DESIGN.md` I11, §5.7, §5.9, §9 and `AGENTS.md` (fruit set) follow. Docs only; nothing was run |

---

## 19. Glossary

The project terms are defined in `DESIGN.md` §8 (Slide, Line, Notch, Block, Match, Round, Cascade, Unit, Target / Slot, Pip, Cap, Queue, Vault, Booster, `Seq` / `Pre`, Theme, `qOpen`, Pull, Vanish, Overflow, Settle, Sand flow, Stream, Next-spawn dot, Chain, Sealed block, Spring block, ×2 block, Dual block, Locked slot, Preview, `EndReason`). Use those exact names in code, commits and agent prompts; the Vietnamese display names are in `DESIGN.md` §9 and never appear in identifiers.
