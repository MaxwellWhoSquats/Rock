---
author: Maxwell Eley
date_created: 2026-05-06
summary: >-
  Procedural checklist every Claude Code session must execute when working on
  the GroupDetail conversion. Defines opening, implementation, self-review,
  closing, and user-review steps. Treat as a literal procedure, not advice.
contributors: []
---

# Session Protocol

This file is the procedural contract between you (the model running a session) and the conversion effort. It is read at the start of every session and treated as a step-by-step procedure. Skipping a step is a quality failure.

The opening prompt for any session on this branch should be:

> Follow `research/specs/SESSION-PROTOCOL.md` to run Phase N for the GroupDetail conversion.

Everything else is in this file.

---

## Section A — Opening (every session, in order)

Before writing any code, read these files in this exact order. Read in full, not skim:

1. **`research/specs/SESSION-PROTOCOL.md`** (this file) — the procedure.

2. **`research/specs/INDEX.md`** — current status, identifies which phase you're in.

3. **`research/specs/00-architecture.md`** — master cross-phase decisions. If status is still "Draft", surface the open questions to the user before proceeding.

4. **`research/specs/ROADMAP.md`** — strategic context.

5. **The current phase's spec file** (e.g., `research/specs/0N-phase-N-<name>.md`).

6. **Every file listed in the phase spec's "Research coverage" section**, in full.

7. **The "Completed" section of every prior phase spec** that is marked `completed` in INDEX.md.

8. **Verify deferred-behavior carry-forward.** Walk every prior phase's coverage report and find every row whose Status is `→ DEFERRED to Phase N` where N matches the current phase. Confirm the current phase spec's "Deferred behaviors inherited from prior phases" section enumerates each. Cross-check that each one is either addressed by an Implementation checklist item OR explicitly re-deferred / dropped (with rationale) under the relevant subsection of inherited-behaviors. If any deferred row points at this phase but isn't accounted for, **stop and ask** — the spec is incomplete.

After reading, build a `TodoWrite` plan that mirrors the phase spec's "Implementation checklist". Each checklist item becomes a todo. Confirm with the user that the plan matches their expectations before starting implementation.

If any step surfaces a contradiction, ambiguity, or missing input, **stop and ask**.

---

## Section B — Implementation rules (during the session)

While writing code, these rules apply continuously:

1. **The phase spec is the contract.** Do not expand scope. Do not collapse scope.
2. **The "Implementation checklist" is the work.** Track each item via `TodoWrite`. Never silently skip an item. Mark `in_progress` when starting, `completed` when finished.
3. **When the spec is silent on a detail, consult research.** Open the file listed in "Research coverage" that covers the behavior. Do NOT guess.
4. **When research reveals a behavior the spec did not anticipate, STOP.**
   - Append the discovery to the phase spec's "Mid-phase decisions log" with date, summary, and reason.
   - Ask the user: "Should this be implemented now, deferred to Phase X, or marked out-of-scope?"
   - Wait for user direction before proceeding.
5. **Leave traceable references.** When implementing each checklist item, the implementation should be findable by file:line. The self-review depends on this.
6. **Run `/build` periodically.** Not just at the end. Catches type / import / namespace issues early.
7. **Honor the locked decisions in 00-architecture.md.** They were resolved deliberately; do not re-litigate.

---

## Section C — Self-review (mandatory, before closing)

Self-review is the completeness check. It runs AFTER implementation, BEFORE closing the session. It produces the **coverage report** that the user reads to confirm nothing was missed.

Procedure:

### C1. Walk the Implementation checklist

For every item in the phase spec's "Implementation checklist":

- Identify the file:line where it's implemented in your changes.
- If you cannot find it, that item is **NOT done** — implement it before continuing.

### C2. Walk every Research coverage file

For every file listed in the phase spec's "Research coverage" section:

- Re-read the file (you read it in Section A; re-read with implementation eyes now).
- Identify every behavior, validation, side effect, edge case, error path, and explicit "Open question" relevant to this phase's scope.
- Classify each as one of:
  - **✓ IMPLEMENTED** — with file:line reference.
  - **→ DEFERRED to Phase N** — with brief rationale (must match a future phase's intended scope).
  - **✗ MISSED** — not implemented and not deferred. Must be zero before phase closes.

### C3. Produce the coverage report

Build the coverage report as a markdown table. This goes into the phase spec's "Completed" section verbatim.

```markdown
| Research file | Behavior | Status | Code ref | Notes |
|---|---|---|---|---|
| webforms/05-entity-and-services.md | GroupService.Get with IdKey | ✓ | Rock.Blocks/Group/GroupDetail.cs:78 | Uses RockEntityDetailBlockType helper |
| webforms/05-entity-and-services.md | RockContext.WrapTransaction in save | → Phase 3 | — | Save flow is Phase 3 scope |
| webforms/16-archive-delete-copy.md | Delete with auth check | ✓ | Rock.Blocks/Group/GroupDetail.cs:289 | |
| webforms/16-archive-delete-copy.md | DeleteSecurityRoleGroup branch | ✓ | Rock.Blocks/Group/GroupDetail.cs:301 | |
```

### C4. Halt criteria

The phase is **not done** if any of these hold:

- Any row in the coverage report shows ✗ MISSED.
- `/build` reports errors.
- `/test` (if applicable) reports failures.
- Any `Implementation checklist` item is unchecked in `TodoWrite`.

If any halt criterion holds, fix it before proceeding to closing.

### C5. Surface anything else

If during self-review you noticed a bug, a security concern, a stale comment, or anything else worth flagging that is OUT of this phase's scope, list it under "New latent bugs / TODOs surfaced" in the Completed section. Do not fix it — that's for the next phase or a separate bugfix spec.

---

## Section D — Closing (every session, in order)

After self-review passes, execute these steps:

1. **Build verification.** Run `/build`. Confirm errors = 0.
2. **Test verification.** Run `/test` if relevant. Confirm pass.
3. **Append "Completed" section to the phase spec.** Use the template structure from PHASE-SPEC-TEMPLATE.md. Include:
   - Summary of what shipped (1-2 paragraphs).
   - The coverage report from Section C3 (verbatim).
   - Any deviations from the spec (with rationale).
   - List of files changed (paths).
   - "New latent bugs / TODOs surfaced" entries from C5.
   - Leave the "Commit hash" field blank — the user fills it in after they commit.
4. **Update `research/specs/INDEX.md`:**
   - Change this phase's status from `in flight` to `completed`.
   - Add the completion date column.
   - Leave the commit hash column blank — the user fills it in after they commit.
5. **Draft the next phase's spec.**
   - Copy `research/specs/PHASE-SPEC-TEMPLATE.md` as the starting point.
   - Save as `research/specs/0(N+1)-phase-(N+1)-<name>.md`.
   - Author every required section with care. Especially:
     - "Research coverage" must list every research file that informs the next phase's scope.
     - "Deferred behaviors inherited from prior phases" must enumerate every row from any prior phase's coverage report (this phase's, plus any earlier phases') whose Status is `→ DEFERRED to Phase (N+1)`. Each inherited row must map to either an Implementation checklist item OR an explicit re-deferral / drop with rationale. See PHASE-SPEC-TEMPLATE.md for the subsection structure.
     - "Implementation checklist" must enumerate every specific behavior to be implemented, including each inherited-deferred behavior now in scope.
   - Mark status as `draft` in INDEX.md.
   - The user will review this draft before the next session starts.
6. **Output a commit-ready summary to the user.** Do NOT run `git commit`, `/commit`, or any other git-history-mutating command. Instead:
   - Show a diff summary (file count, lines added / removed, list of new files).
   - Suggest a commit message in the Rock format (`+ (Group) Phase N: <title>.` for release-note phases, `- Phase N: <title>` for internal phases). Include a multi-line body that references the phase spec path and the coverage-report status.
   - Tell the user the changes are staged-ready / uncommitted and waiting for their review and commit.
7. **Output a manual test plan to the user.** Concrete numbered steps they can execute in a browser to verify each item under "Behavior delivered". This is the user's review playbook.

The user takes it from here: review diff, run `git commit` themselves, then optionally update INDEX.md and the phase spec's "Completed" section with the commit hash. The model does not own git history at phase boundaries.

Section D is mandatory. Skipping any step is a quality failure. If a step blocks (e.g., `/build` keeps failing), surface the blocker rather than silently dropping the step.

---

## Section E — User review (between sessions, executed by the user)

Steps the user runs after a phase session ends:

1. **Read the coverage report** in the phase spec's "Completed" section. Confirm all rows are ✓ or →. Any ✗ is a hard stop.
2. **Spot-check 2-3 high-risk areas** in the code by clicking the file:line links from the coverage report.
3. **Run the manual test plan** from step D7.
4. **Read the next phase's draft spec** that was authored in step D5. Push back if "Implementation checklist" looks incomplete or "Research coverage" is missing files.
5. **Review the diff and commit** (the model left changes uncommitted on purpose). Use `git diff`, `git diff --stat`, or your tool of choice. Run `git commit` when satisfied. The suggested commit message from step D6 is a starting point; edit freely.
6. **Update INDEX.md and the phase spec's "Completed" section with the commit hash** after committing. This can also be done as the first step of the next session if you'd rather not bother manually.
7. **Approve to proceed**, OR **course-correct** by raising specific issues. Course corrections happen in a fresh session, framed as: "the Phase N coverage report shows X but Y is broken — fix and re-run self-review."

The user is the final completeness check AND the owner of git history at phase boundaries. The model's self-review is the model's best effort; the user's review and commit are canonical.

---

## What this protocol does NOT cover

- Strategic decisions about phase scope. Those are in `00-architecture.md` and `ROADMAP.md`.
- Code style and conventions. Those are in `CLAUDE.md` and `.claude/rules/`.
- Spec authoring style. That is in `PHASE-SPEC-TEMPLATE.md`.
- Bug-fix workflow. Use the `/bugfix` skill if a real bug is discovered mid-implementation that's out of phase scope.

If a procedural question comes up that this protocol does not address, ask the user before improvising.

---

## Why this protocol exists

The conversion is too large for a single session. Phasing makes it reviewable. Phasing also creates a risk: a phase spec that's missing a behavior leads to an implementation that's missing that behavior. The Implementation checklist + Research coverage + Self-review machinery exists to make missed behaviors visible by the time the phase closes.

The user's stated requirement: "ensure all behavior, functionality, logic is accounted for for the new Obsidian block." Sections A, B, C, D collectively enforce that. Section E is the user's confirmation lever.

If a session ends without a coverage report, the protocol failed and the phase needs to re-run.
