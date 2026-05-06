---
author: Maxwell Eley
date_created: 2026-05-06
summary: >-
  Multi-phase conversion of the WebForms RockWeb/Blocks/Groups/GroupDetail.ascx
  block to Obsidian, executing the redesign captured in the Fall-2025/Spring-2026
  Figma file. Tracker for the architecture spec and per-phase implementation specs.
contributors: []
---

# GroupDetail Conversion: Spec Tracker

This directory holds the master architecture spec and per-phase implementation specs for converting `RockWeb/Blocks/Groups/GroupDetail.ascx` to Obsidian.

The conversion is broken into phases for iterative development with review checkpoints between each. Each phase is intended to fit a single fresh-context Claude session, gated by its own implementation spec.

## Research foundation

This effort is informed by two completed research passes:

- **WebForms parity baseline** — [research/webforms/](../../../research/webforms/) (26 files, 7,572 lines). Comprehensive coverage of the existing block: configuration, state matrix, code-behind walkthrough, every sub-feature, validation cascades, GroupType inheritance, cross-block dependencies, WebForms-isms.
- **Design intent** — [research/design/](../../../research/design/) (7 docs + 25 screenshots). Pure-Vue redesign of both panels: Sections & Stacks pattern, Group Tools card, Map Cards, Linkages section, ~30 label renames.

The phase partition recommendation lives at [research/webforms/21-phase-partitioning.md](../../../research/webforms/21-phase-partitioning.md). The partition was validated by both research passes and does not need restructuring; the design pass tightened scope per phase but did not move boundaries.

## Spec status

| # | Spec | Status | Output of |
|---|---|---|---|
| 00 | [Architecture](00-architecture.md) | draft (open questions pending user input) | Phase 0 session |
| 01 | Phase 1: Block shell + View panel + Delete/Archive/Copy | not yet authored | Phase 0 session (or start of Phase 1) |
| 02 | Phase 2: Edit panel core (Top fields + General + RSVP + Scheduling + Chat) | not yet authored | Start of Phase 2 |
| 03 | Phase 3: Attributes (Group + Member) | not yet authored | Start of Phase 3 |
| 04 | Phase 4: Requirements + Sync + Member Workflows | not yet authored | Start of Phase 4 |
| 05 | Phase 5: Locations & Schedules | not yet authored | Start of Phase 5 |
| 06 | Phase 6: New features beyond design (if any) | conditional — likely empty | After Phase 5 |
| 07 | Phase 7: Cutover and cleanup | not yet authored | After all earlier phases |

Phase 0 in this plan is the single session that produces (a) the architecture spec, (b) optionally the Phase 1 implementation spec. No code is written in Phase 0.

## Per-session protocol

Every implementation phase runs in a fresh Claude session. The opening prompt should always include:

1. Path to [00-architecture.md](00-architecture.md) (the master spec, locked).
2. Path to that phase's implementation spec.
3. Pointers to the relevant `research/webforms/` and `research/design/` files.
4. Directive: "implement only the scope defined in the phase spec; flag any ambiguity and stop for clarification."

After implementation, the session ends with:

1. Build verification (`/build` skill).
2. Manual test pointers from the spec.
3. A "completed" section appended to the phase spec recording what was actually built and any deviations.
4. Commits per the `/commit` skill.

The user reviews at every phase boundary, optionally course-corrects, and starts the next phase fresh.

## Conventions

- Specs use the same YAML frontmatter format as flat specs in `specs/`.
- File:line references use markdown links (e.g., `[GroupDetail.ascx.cs:1556](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1556)`).
- Cross-references between specs and research use relative paths.
- No em-dashes or `--` in writing.
- "Latent bugs" surfaced during research are listed in [00-architecture.md](00-architecture.md) for triage; each gets a fix-during / defer-to-bugfix-spec / drop classification before any code starts.

## Completion

When all phases land and the cutover spec ships, this directory moves to `specs/completed/Group/GroupDetail-Conversion/` per the `/spec` skill convention.
