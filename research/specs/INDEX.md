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

1. **[SESSION-PROTOCOL.md](SESSION-PROTOCOL.md)** — the procedural checklist every session must follow. Opening, implementation, self-review, closing. Read first, every session.
2. **[ROADMAP.md](ROADMAP.md)** — strategic plan, phase rationale, alternatives considered, risks. Read once when joining the effort.
3. **[00-architecture.md](00-architecture.md)** — locked cross-phase decisions (block class shape, GUID strategy, IdKey policy, etc.). Read every session.
4. This file (`INDEX.md`) — current phase status. Read every session.
5. The current phase's spec — the work for this session.
6. Research files — read in full as listed under the phase spec's "Research coverage" section.

[PHASE-SPEC-TEMPLATE.md](PHASE-SPEC-TEMPLATE.md) is the template every phase spec must follow. Read it when authoring a new phase spec (per SESSION-PROTOCOL.md Section D5).

## Current state

| # | Spec | Status | Completion date | Commit | Notes |
|---|---|---|---|---|---|
| - | [SESSION-PROTOCOL.md](SESSION-PROTOCOL.md) | locked | 2026-05-06 | - | Procedural contract for every session |
| - | [PHASE-SPEC-TEMPLATE.md](PHASE-SPEC-TEMPLATE.md) | locked | 2026-05-06 | - | Template for every phase spec |
| - | [ROADMAP.md](ROADMAP.md) | locked | 2026-05-06 | - | Strategic phase plan. View-first reordering applied 2026-05-06 after Phase 1 self-review. |
| 00 | [00-architecture.md](00-architecture.md) | **locked** | 2026-05-06 | - | Q1-Q12 resolved during the Phase 0 session. View-first reordering note added 2026-05-06. |
| 01 | [01-phase-1-shell-and-view.md](01-phase-1-shell-and-view.md) | **completed** | 2026-05-07 | - | Block shell + view panel core + Delete/Archive/Copy + Audit modal + Linkages shipped. Completed section appended to spec. Awaiting user commit; commit hash to be filled in after `git commit` lands. |
| 02 | [02-phase-2-complete-view-panel.md](02-phase-2-complete-view-panel.md) | **completed** | 2026-05-08 | - | `Group.PhotoId` column + nav + EF config + plug-in migration `289_AddGroupPhotoId.cs` (v19.0 branch lock; NO ACTION cascade per Person.PhotoId pattern). `Group.PhotoUrl` computed property. `bag.PhotoUrl` populated server-side. `GroupMeetingLocationBag` + `BuildMeetingLocations` server-side helper (4-mode classification, `ShowLocationAddresses` gate, pre-resolved `MapUrl`). `locationCard.partial.obs` with raw Google Maps API in static mode + hover-expand. Meeting Locations card wired into `viewPanel.partial.obs`. `MapStyleValueGuid` surfaced on options bag. Top-of-panel notification surface added (system-group info banner + role-limit warning from `Group.GetGroupTypeRoleLimitWarnings`). DetailBlock framework extensions: `headerLabels` prop, `titleIconCssClass` prop, `PanelAction.style` field. GroupType chip tinted by `GroupType.GroupTypeColor`. Build clean (Rock + Rock.Blocks + Obsidian.Blocks TS). Awaiting user commit. |
| 03 | [03-phase-3-edit-core.md](03-phase-3-edit-core.md) | **locked** | 2026-05-08 | - | Drafted 2026-05-07; locked 2026-05-08 after Q3.1-Q3.7 resolution. Scope: edit panel core (Top fields + General + RSVP + Scheduling + Chat sections), Save block action, GroupType cascade reactivity (Q2 Approach B), Group photo + chat-channel-avatar uploaders (IsTemporary BinaryFile pattern), Add path with `?ParentGroupId=N` defaulting, `?autoEdit=true` handling, Trailblazer per-field styling (Q6), L1 fix-during. Locked decisions: single `editPanel.partial.obs` file (all sections inline; no per-section partials), full default `GroupTypeOptionsBag` shape, `BinaryFiletype.DEFAULT` for photo, `!config.entity.idKey` for Add-mode discrimination (drops `bag.isAddMode`), 8-step `WrapTransaction` save, five Trailblazer-gated controls (Group Administrator / Required Signature Document / Member Record Source / Show Advanced Relationship Settings inline switch / four multipliers transitively), view-mode-only notification surface (no edit-mode persistence). Ready for implementation session. |
| 04 | `04-phase-4-attributes.md` | not authored | - | - | Attributes (was Phase 3). Authored at end of Phase 3. |
| 05 | `05-phase-5-requirements-sync-workflows.md` | not authored | - | - | Requirements + Sync + Member Workflows (was Phase 4). Authored at end of Phase 4. |
| 06 | `06-phase-6-locations-editing.md` | not authored | - | - | Locations editing modal + inline schedule logic (was Phase 5). The view-side map cards already shipped in Phase 2. Authored at end of Phase 5. |
| 07 | `07-phase-7-update-dependencies.md` | not authored | - | - | Per Q4: update the 5 still-WebForms outbound destinations (GroupListPage, FundraisingProgressPage, GroupHistoryPage, GroupMapPage, GroupSchedulerPage) to accept IdKey on `GroupId`. Authored at end of Phase 6. |
| 08 | `08-cutover.md` | not authored | - | - | Authored at end of Phase 7. Verify chop, delete WebForms files, smoke test cross-block callers, release notes. |

**View-first reordering (2026-05-06).** After Phase 1's self-review the user requested that the entire view panel be completed before any edit-panel work begins. New Phase 2 ("Complete the view panel") was inserted; original Phases 2-5 each shifted down by one number; old Phase 6 (Net-new features, Q6-confirmed empty) was dropped. See [ROADMAP.md "Why view-first"](ROADMAP.md) for rationale.

Status legend:
- **not authored** — file does not exist yet
- **draft** — exists but has open questions or unresolved sections
- **locked** — finalized, treated as canonical
- **in flight** — implementation session running (rare; specs are normally locked before implementation starts)
- **completed** — phase implemented; "Completed" section appended to the spec; code committed (see commit hash column)

## How specs are authored

Phase specs are NOT authored upfront. Each phase's spec is drafted at the END of the prior phase's session, while context on what just shipped is fresh.

Sequence (post-reordering):
1. **Phase 0 session** outputs `00-architecture.md` (locked) + `01-phase-1-shell-and-view.md` (draft awaiting user review).
2. **Phase 1 session** locks the Phase 1 spec at session start (with user approval), implements per the spec, then drafts `02-phase-2-complete-view-panel.md` as its closing step.
3. **Phase 2 session** locks the Phase 2 spec, implements (Group.PhotoId column + Group Image hero + Meeting Locations card), then drafts `03-phase-3-edit-core.md`.
4. **Phase 3 session** locks Phase 3 spec, implements edit core, drafts `04-phase-4-attributes.md`.
5. Same pattern through Phase 6 (Locations editing). At each phase close, the next phase's spec is drafted.
6. **Phase 7** is the dependencies update: the 5 still-WebForms outbound destinations are updated to accept IdKey on their `GroupId` page parameter (per Q4).
7. **Phase 8** is the cutover: verify chop, delete WebForms files, smoke test, release notes.

The user reviews each newly-drafted spec in the gap between sessions before the next session starts.

## Per-session protocol

The opening prompt for every session is one line:

> Follow `research/specs/SESSION-PROTOCOL.md` to run Phase N for the GroupDetail conversion.

The protocol file enforces opening (Section A), implementation (Section B), self-review (Section C — the completeness check that produces a coverage report), and closing (Section D). The user reviews per Section E between sessions.

The completeness contract is:
- Each phase spec has a "Research coverage" list and an "Implementation checklist".
- The model's self-review walks both at session close and produces a coverage report classifying every research-derived behavior as ✓ implemented, → deferred, or ✗ missed.
- A phase does not close while any ✗ missed exists.

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
