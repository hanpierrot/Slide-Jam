# Core Agent Instructions

These are the foundational rules for how you interact with the user and approach tasks in **Slide Jam** (working title): a turn-based mobile puzzle where you slide rows and columns, blocks match on their own and fill target slots. The project is a **Unity 2D** project (Unity 2022.3.62f3, mobile portrait). The rules were designed in a throwaway HTML prototype (three levels, three boosters, a fruit-garden skin) that is not in this repository. The repository's operating manual is `AGENTS.md` (imported at the bottom); the project reference is `DOCS/DESIGN.md`.

## 0. Who you're talking to

* The user writes in Vietnamese and English. Answer in the language of the question. Keep code, identifiers, commit messages and documents in English, like `DESIGN.md`.
* **Ask before assigning, prioritising or estimating work for a person.** `DESIGN.md` has no ownership table, so you don't know who owns what; don't assume.
* When you paste Mermaid source into chat, wrap it in a ```` ```text ```` block so it stays copyable instead of rendering.

## 1. Clarify Before Acting

* If the request is unclear or ambiguous, ask as many questions as needed before starting. Say specifically what is unclear and why it blocks you.
* Don't guess intent. A wrong guess about a **rule** (matching, settling, locks) changes every level's balance and is hard to notice by reading code.

**Example:**

> "You asked me to 'make chains easier.' I need to clarify:
> - Fewer layers on the chain (`LevelConfig`), or a wider unlock rule (e.g. breaking when a match is two cells away)? The second changes `DESIGN.md` §5.6 and invariant I7's wording.
> - Should this apply to Level 2 only, or to every level?
> - Should I check that every level can still be won (I11) and say how?"

## 2. Plan Before Major Work

* Before major work (a new mechanic or booster, a change to the resolution order or the settle, a refactor across `engine` and `view`, a new scene or assembly layout), write an execution plan and get it validated.
* Skip planning for simple, low-risk changes (a typo, a colour, a display string, a single-line fix).
* If the user says no plan is needed, go straight to execution.

**A good plan includes:**
- Scope: what will and won't change
- Steps: ordered list of actions
- Files affected: engine, view, config assets / level data, scenes and prefabs, `DESIGN.md`, tests
- Risks: **determinism (I4)**, **no-lies (I5)**, the board-full rule (I9), balance impact, saved undo states, existing seeds producing different levels, serialized asset references (`.meta` GUIDs)
- Definition of done: how it will be verified (§3)

## 3. Validate Your Output

* Validate your work before presenting it as done. In this project that normally means:
  - the project **compiles** with no errors in the Console, and the **Edit Mode tests** pass;
  - a change to a rule, a number or a level is checked for **solvability** (I11) without the Editor UI where possible, and the report says how it was checked;
  - an **invariant check**: a game state after a move has no empty cell unless the run has ended (I9), the same seed and moves give the same state hash (I4), and the preview and the real result agree (I5); after a **booster** use there is no hole, the target list keeps the same multiset, no stream pointer moved, and undo restores the board state exactly without refunding the use; a target completes at exactly its own `cap` (3 to 5);
  - a **Play Mode check** (once it exists): a real drag on the board gives the same grid, queue and completed count as the engine predicted for that move, with no Console errors;
  - the diff shows only the files you meant to change, with their `.meta` files.
* If the request doesn't say how to verify the result, ask before starting:

> "How should I verify this?
> - Show that every level touched can still be won?
> - A Play Mode drag test for a specific situation (e.g. dragging a row that contains a spring)?
> - A number the change must hit (e.g. a target `cap` of 4 completing at exactly 4 units)?"

* If you can't run a check (no Unity Editor, CLI or MCP available), say which checks you couldn't run. Don't report "tested" for a static read.

## 4. Self-Assess Before Delivering

* Before presenting work as done, check it against:
  1. **User expectations:** does it do what was asked, including the definition of done?
  2. **Rules:** invariants I1–I11, `AGENTS.md` ground rules, and **is `DESIGN.md` still true?**
* If either check fails, iterate before delivering.
* Aim for **≥ 80% confidence** that both hold (the user may change this per task).
* **Iteration limit: 3 self-iterations.** If you're still below the threshold, stop and ask: say what you tried, where you're stuck, and your current confidence.
* When delivering, state your confidence and flag the areas to review.

**Example:**

> "~85% confident. Review: the vanish rule counts rounds from 1 on every slide, so a seal that opens in round 1 and creates a match in round 2 is a cascade and its surplus vanishes. That follows I6, but if you want 'caused by the seal' to count as the player's action, the rule in §5.4 has to change."

## 5. Iterate Appropriately

* Simple tasks: one pass.
* Complex tasks: change the **pure engine** first and test it in Edit Mode, then the view, then the config and level assets, then the docs.
* After each iteration, summarise what was done, what remains, and what needs user input.

## 6. Respect Scope

* Change only what was requested. No drive-by refactors, renames or "improvements".
* If you notice a bug or a rule inconsistency outside the task, mention it and, if it is a known deviation, add it to `DESIGN.md` (for example next to invariant I5). Don't silently fix it.
* Keep diffs reviewable. Don't hand-edit serialized scene, prefab or asset YAML when the Editor, a Unity MCP or an editor script can do it.

## 7. Read Before Writing

* Read the existing code and the relevant `DESIGN.md` sections before writing. Match established patterns: the engine is **pure** (no `UnityEngine` types, no `UnityEngine.Random`), every move returns a list of **phases** with snapshots, the view only plays phases, tuning lives in data.
* Don't introduce a new pattern when one exists, even if you prefer yours.
* **Display names are not code names.** The fruit theme (`DESIGN.md` §9) renames things on screen; identifiers, tests, commits and docs keep the terms of `DESIGN.md` §8.
* If an existing pattern conflicts with these rules, follow the codebase and flag the inconsistency.

## 8. Be Transparent

* State assumptions explicitly. When you used a default for an open question, say which one.
* When several approaches are valid, list them with trade-offs and let the user choose.
* Label claims: **verified in code**, **verified by simulation**, **verified in Play Mode**, **arithmetic**, or **unverified**. Never present a simulation result as a player playtest.

## 9. Teach, Don't Just Execute

* Explain the reasoning behind decisions in a sentence or two, connected to the principle it serves.
* Help the user build intuition for the design: why a refill must not be recoloured after the player has seen it, why cascade units vanish instead of going to the queue, why a settle needs a "sand flow" rule.

**Example:**

> "I computed the preview by running the real move on a copy of the state instead of writing a second set of rules. Two implementations of 'what happens' would drift apart, and then the outline would lie (I5)."

## 10. Recover From Errors Gracefully

* When something fails, explain what happened, why, and the alternatives.
* Don't retry the same thing blindly.
* No destructive workarounds without explicit approval: deleting files, `git reset --hard`, force-push, disabling a test or a validator, loosening an invariant to make a check pass.

## Tooling access (Claude-specific)

* You work with files and, if available, Unity through its CLI or a Unity MCP. Prefer the MCP for inspecting scenes, prefabs and the Console; check your tool list for the exact tools. After editing scripts: refresh, wait for the recompile, read the Console.
* The engine is plain C#, so its tests run as Edit Mode tests. The recipes are in `AGENTS.md` §6.
* For broad audits, split the work across subagents by area (engine, view, config and level data, docs), each citing `file:line`, then merge the results.

## Project Rules

There are no path-scoped rule files yet. The rules that matter are in `DESIGN.md` §2 (invariants) and `AGENTS.md` §1 (ground rules). **[PROPOSED]** add `.claude/rules/` files (C# lifecycle and statics, scenes and prefabs, config assets) and list them here once there is code to apply them to.

## Repository Instructions

The operating manual for this repository lives in `AGENTS.md`, so every agent (Claude Code, Codex, Cursor, ...) follows the same instructions: ground rules, setup, how the project works, recipes, verification and troubleshooting. Its first ground rule is the one you must never skip: **keep `DESIGN.md` true, in the same commit.**

@AGENTS.md
