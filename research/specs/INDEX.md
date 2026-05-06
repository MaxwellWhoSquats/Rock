---
author: Maxwell Eley
date_created: 2026-05-06
summary: >-
  Live status tracker for the multi-phase Obsidian conversion of
  RockWeb/Blocks/Groups/GroupDetail.ascx. Lists every spec, current
  status, completion commit, and pointers to research and roadmap.
  Read first by every implementation session.
contributors: []
---

# GroupDetail Conversion: Spec Tracker

This is the live status tracker for the conversion. Every Claude session reads this file first to know where the effort stands.

If you are starting a new phase session, the opening prompt should always include this file's path.

## Reading order

1. **[ROADMAP.md](ROADMAP.md)** — strategic plan, phase rationale, alternatives considered, risks. Read once when joining the effort.
2. **[00-architecture.md](00-architecture.md)** — locked cross-phase decisions (block class shape, GUID strategy, IdKey policy, etc.). Read every session.
3. This file (`INDEX.md`) — current phase status. Read every session.
4. The current phase's spec — the work for this session.
5. Research files — read as referenced.

## Current state

| # | Spec | Status | Completed in commit | Notes |
|---|---|---|---|---|
| — | [ROADMAP.md](ROADMAP.md) | locked | — | Strategic phase plan |
| 00 | [00-architecture.md](00-architecture.md) | **draft** | — | Q1-Q12 awaiting Phase 0 session |
| 01 | `01-phase-1-shell-and-view.md` | not authored | — | Authored at end of Phase 0 |
| 02 | `02-phase-2-edit-core.md` | not authored | — | Authored at end of Phase 1 |
| 03 | `03-phase-3-attributes.md` | not authored | — | Authored at end of Phase 2 |
| 04 | `04-phase-4-requirements-sync-workflows.md` | not authored | — | Authored at end of Phase 3 |
| 05 | `05-phase-5-locations-schedules.md` | not authored | — | Authored at end of Phase 4 |
| 06 | `06-phase-6-new-features.md` | conditional | — | Likely empty unless Trailblazer Settings adds scope |
| 07 | `07-cutover.md` | not authored | — | Authored at end of Phase 5 (or 6 if used) |

Status legend:
- **not authored** — file does not exist yet
- **draft** — exists but has open questions or unresolved sections
- **locked** — finalized, treated as canonical
- **in flight** — implementation session running (rare; specs are normally locked before implementation starts)
- **completed** — phase implemented; "Completed" section appended to the spec; code committed (see commit hash column)

## How specs are authored

Phase specs are NOT authored upfront. Each phase's spec is drafted at the END of the prior phase's session, while context on what just shipped is fresh.

Sequence:
1. **Phase 0 session** outputs `00-architecture.md` (locked) + `01-phase-1-shell-and-view.md` (locked).
2. **Phase 1 session** implements per the Phase 1 spec, then drafts `02-phase-2-edit-core.md` as its closing step.
3. **Phase 2 session** reviews + locks the Phase 2 spec at session start (with user approval), implements, then drafts `03-phase-3-attributes.md`.
4. Same pattern through Phase 5.
5. **Phase 6** runs only if Trailblazer Settings or other Figma items demand net-new work.
6. **Phase 7** is the cutover — verify chop, delete WebForms files, smoke test, release notes.

The user reviews each newly-drafted spec in the gap between sessions before the next session starts.

## Per-session protocol

Every implementation phase runs in a fresh Claude session. Opening prompt must include:

1. Path to [ROADMAP.md](ROADMAP.md) (read for context).
2. Path to [00-architecture.md](00-architecture.md) (locked, read first).
3. Path to this `INDEX.md` (current status).
4. Path to that phase's implementation spec.
5. Pointers to relevant `research/webforms/` and `research/design/` files.
6. Directive: "implement only the scope defined in the phase spec; flag any ambiguity and stop for clarification."

After implementation, the session ends with:

1. Build verification (`/build`).
2. Manual test pointers from the spec.
3. A "Completed" section appended to the phase spec recording what was actually built and any deviations.
4. Update this `INDEX.md`: change phase status to `completed`, fill in the commit hash.
5. Draft the NEXT phase's spec (so it's ready for review).
6. Commit per `/commit`.

## Conventions

- Specs use the same YAML frontmatter format as flat specs in `specs/`.
- File:line references use markdown links (e.g., `[GroupDetail.ascx.cs:1556](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1556)`).
- Cross-references between specs and research use relative paths.
- No em-dashes or `--` in writing.
- "Latent bugs" surfaced during research are listed in [00-architecture.md](00-architecture.md) for triage; each gets a fix-during / defer-to-bugfix-spec / drop classification before any code starts.

## Research foundation

This effort is informed by two completed research passes:

- **WebForms parity baseline** — [../webforms/](../webforms/) (26 files, 7,572 lines).
- **Design intent** — [../design/](../design/) (7 docs + 25 screenshots).

The phase partition recommendation lives at [ROADMAP.md](ROADMAP.md). The partition was validated by both research passes and does not need restructuring; the design pass tightened scope per phase but did not move boundaries.

## Completion

When all phases land and the cutover spec ships, this directory moves to `specs/completed/Group/GroupDetail-Conversion/` per the `/spec` skill convention. The research artifacts under `research/webforms/` and `research/design/` are archived alongside.
