# Core Agent Instructions

These are the foundational rules for how you interact with the user and approach tasks in **Slide Jam** (working title): a turn-based mobile puzzle where you slide rows and columns, blocks match on their own and fill target slots. The project is a set of HTML prototypes today, with a Unity (2D) port planned. The repository's operating manual is `AGENTS.md` (imported at the bottom); the project reference is `DESIGN.md`.

## 0. Who you're talking to

* The user writes in Vietnamese and English. Answer in the language of the question. Keep code, identifiers, commit messages and documents in English, like `DESIGN.md`.
* **Ask before assigning, prioritising or estimating work for a person.** `DESIGN.md` has no ownership table, so you don't know who owns what; don't assume.
* When you paste Mermaid source into chat, wrap it in a ```` ```text ```` block so it stays copyable instead of rendering.

## 1. Clarify Before Acting

* If the request is unclear or ambiguous, ask as many questions as needed before starting. Say specifically what is unclear and why it blocks you.
* Don't guess intent. A wrong guess about a **rule** (matching, settling, locks) changes every level's balance and is hard to notice by reading code.

**Example:**

> "You asked me to 'make chains easier.' I need to clarify:
> - Fewer layers on the chain (`LevelConfig`), or a wider unlock rule (e.g. breaking when a match is two cells away)? The second changes `DESIGN.md` §5.7 and invariant I7's wording.
> - Should this apply to Level 2 only, or to every level?
> - Should I re-measure balance (§5.10) and put the new numbers in the doc?"

## 2. Plan Before Major Work

* Before major work (a new mechanic, a change to the resolution order or the settle, a refactor across `logic` and `view`, the Unity port), write an execution plan and get it validated.
* Skip planning for simple, low-risk changes (a typo, a colour, a toast text, a single-line fix).
* If the user says no plan is needed, go straight to execution.

**A good plan includes:**
- Scope: what will and won't change
- Steps: ordered list of actions
- Files affected: logic, view, level data, `DESIGN.md`, tests
- Risks: **determinism (I4)**, **no-lies (I5)**, the board-full rule (I9), balance impact, saved undo states, existing seeds producing different levels
- Definition of done: how it will be verified (§3)

## 3. Validate Your Output

* Validate your work before presenting it as done. In this project that normally means:
  - the logic is checked **headless**: the bot simulation (random and greedy, `DESIGN.md` §5.9) runs without errors and without timeouts, and the numbers in §5.10 are still true or have been updated;
  - an **invariant check**: a game state after a move has no empty cell unless the run has ended (I9), the same seed and moves give the same state (I4), and the preview and the real result agree (I5);
  - a **browser check**: the real drag in a headless browser gives the same grid, queue and completed count as the logic predicted for that move, with no console errors;
  - the diff shows only the files you meant to change.
* If the request doesn't say how to verify the result, ask before starting:

> "How should I verify this?
> - Re-run the bot simulation and compare with `DESIGN.md` §5.10?
> - A browser drag test for a specific situation (e.g. dragging a row that contains a spring)?
> - A number the change must hit (e.g. random-play win rate between 5% and 20%)?"

* If you can't run a check (no browser, no Node), say which checks you couldn't run. Don't report "tested" for a static read.

## 4. Self-Assess Before Delivering

* Before presenting work as done, check it against:
  1. **User expectations:** does it do what was asked, including the definition of done?
  2. **Rules:** invariants I1–I11, `AGENTS.md` ground rules, the removed-mechanics list (§5.12), and **is `DESIGN.md` still true?**
* If either check fails, iterate before delivering.
* Aim for **≥ 80% confidence** that both hold (the user may change this per task).
* **Iteration limit: 3 self-iterations.** If you're still below the threshold, stop and ask: say what you tried, where you're stuck, and your current confidence.
* When delivering, state your confidence and flag the areas to review.

**Example:**

> "~85% confident. Review: the vanish rule counts rounds from 1 on every slide, so a seal that opens in round 1 and creates a match in round 2 is a cascade and its surplus vanishes. That follows I6, but if you want 'caused by the seal' to count as the player's action, the rule in §5.4 has to change."

## 5. Iterate Appropriately

* Simple tasks: one pass.
* Complex tasks: change the **pure logic** first and test it headless, then the view, then the level data, then the docs.
* After each iteration, summarise what was done, what remains, and what needs user input.

## 6. Respect Scope

* Change only what was requested. No drive-by refactors, renames or "improvements".
* If you notice a bug or a rule inconsistency outside the task, mention it and, if it is a known deviation, add it to `DESIGN.md` (for example next to invariant I5). Don't silently fix it.
* Keep diffs reviewable. The prototype builds one HTML file from several sources; edit the sources, not the generated file.

## 7. Read Before Writing

* Read the existing code and the relevant `DESIGN.md` sections before writing. Match established patterns: logic is **pure** (no DOM, no `Math.random`), every move returns a list of **phases** with snapshots, the view only plays phases, tuning lives in data.
* Don't introduce a new pattern when one exists, even if you prefer yours.
* If an existing pattern conflicts with these rules, follow the codebase and flag the inconsistency.

## 8. Be Transparent

* State assumptions explicitly. When you used a default for an open question, say which one.
* When several approaches are valid, list them with trade-offs and let the user choose.
* Label claims: **verified in code**, **verified by simulation**, **verified in the browser**, **arithmetic**, or **unverified**. Never present a bot result as a player playtest; the greedy bot is an upper bound on skill (§5.9).

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

* **Prototype phase:** you work with files, Node and a headless browser (Playwright). Use the bot simulation for balance and the browser test for the drag-to-logic check. Both are described in `AGENTS.md` §6.
* **Unity phase [PROPOSED]:** if a Unity MCP is connected, prefer it for inspecting scenes, prefabs and the Console. Check your tool list for the exact tools. After editing scripts: refresh, wait for the recompile, read the Console.
* For broad audits, split the work across subagents by area (logic, view, level data, docs), each citing `file:line`, then merge the results.

## Project Rules

There are no path-scoped rule files yet. The rules that matter are in `DESIGN.md` §2 (invariants) and `AGENTS.md` §1 (ground rules). **[PROPOSED]** when the Unity project exists, add `.claude/rules/` files (C# lifecycle and statics, scenes and prefabs, config assets) and list them here, like the OverTimer project does.

## Repository Instructions

The operating manual for this repository lives in `AGENTS.md`, so every agent (Claude Code, Codex, Cursor, ...) follows the same instructions: ground rules, setup, how the project works, recipes, verification and troubleshooting. Its first ground rule is the one you must never skip: **keep `DESIGN.md` true, in the same commit.**

@AGENTS.md
