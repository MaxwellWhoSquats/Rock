# GroupDetail Conversion Research

This directory captures the research findings used to plan a phased Obsidian conversion of `RockWeb/Blocks/Groups/GroupDetail.ascx`.

The block is exceptionally large (5,950 lines across markup + code-behind) and is also being meaningfully redesigned (View panel overhaul, new features). For that reason it is being broken into multiple specs rather than handled with a single `/convert-block` pass.

This research feeds into:
1. A phase-partitioning recommendation (the immediate output of this session).
2. A series of phase-specific specs that will live under `specs/` once the partitioning is approved.

---

## Status

| # | File | Lines | Status |
|---|---|---|---|
| 00 | [Block overview](00-overview.md) | 60 | done |
| 01 | [Block configuration](01-block-configuration.md) | 634 | deepened |
| 02 | [Block states / modes](02-block-states.md) | 475 | deepened |
| 03 | [Markup structure](03-markup-structure.md) | 178 | done |
| 04 | [Code-behind walkthrough](04-code-behind-walkthrough.md) | 246 | done |
| 05 | [Entity model and services](05-entity-and-services.md) | 468 | deepened |
| 06 | [Sub-feature: Peer Network](06-peer-network.md) | 170 | deepened |
| 07 | [Sub-feature: Locations & Schedules](07-locations-and-schedules.md) | 435 | deepened |
| 08 | [Sub-feature: Group Scheduling](08-scheduling.md) | 239 | deepened |
| 09 | [Sub-feature: Group Attributes](09-group-attributes.md) | 126 | deepened |
| 10 | [Sub-feature: Member Attributes](10-member-attributes.md) | 257 | deepened |
| 11 | [Sub-feature: Group Requirements](11-group-requirements.md) | 276 | deepened |
| 12 | [Sub-feature: Group Sync](12-group-sync.md) | 193 | deepened |
| 13 | [Sub-feature: Member Workflow Triggers](13-member-workflow-triggers.md) | 229 | deepened |
| 14 | [Sub-feature: Chat](14-chat.md) | 192 | deepened |
| 15 | [Sub-feature: RSVP](15-rsvp.md) | 155 | deepened |
| 16 | [Sub-feature: Archive / Delete / Copy + Capacity + Signature Doc](16-archive-delete-copy.md) | 504 | deepened |
| 17 | [View panel (Pure Vue / Figma-driven)](17-view-panel.md) | 143 | done |
| 18 | [Cross-block dependencies + IdKey follow-on](18-cross-block-dependencies.md) | 382 | deepened |
| 19 | [WebForms-isms / portability gaps](19-webforms-isms.md) | 267 | deepened |
| 20 | [Reference: similar converted blocks](20-reference-blocks.md) | 332 | done |
| 21 | Phase-partitioning recommendation | — | moved to [research/specs/ROADMAP.md](../specs/ROADMAP.md) |
| 22 | [Open question: GroupType cascade](22-grouptype-cascade.md) | 315 | open |
| 23 | [Server-side validations and side-effects cascade](23-validations-and-cascades.md) | 354 | new |
| 24 | [GroupType inheritance behavior](24-grouptype-inheritance.md) | 282 | new |
| 27 | [Misc surfaces (badges, tags, following, audit, QuickReturn)](27-misc-surfaces.md) | 349 | new |

Total: 7,572 lines across 26 files (excluding INDEX). Numbers 25 and 26 are unused — Group Capacity and Signature Document Template were absorbed into [16-archive-delete-copy.md](16-archive-delete-copy.md) instead of getting their own files.

## Deep-research pass added

The October research-push pass extended 16 files and added 3 new ones (23, 24, 27). Highlights:

- **Block configuration (01)**: 87 → 634 lines. Per-attribute deep dive, ValidationGroup family, attribute qualifier persistence, ContextAware behavior (flagged as latent dead code).
- **State matrix (02)**: 152 → 475 lines. Composite scenario walkthroughs, hfActiveDialog state machine, dialog-vs-modal table, three signature-state visibility maps.
- **Validations and cascades (23)**: New, 354 lines. Eight save-flow validation gates, full cascade map (GroupType change, ParentGroup change, IsActive child cascade, IsSecurityRole flip, schedule deletion, GroupLocation removal, GroupRequirement deferred-insert, BinaryFile IsTemporary toggle, archive flow, etc.).
- **GroupType inheritance (24)**: New, 282 lines. Only attribute definitions inherit (not roles, requirements, settings). OptionsBag size analysis: ~3.75 KB per GroupType, ~187 KB for 50 — sharpens the [22](22-grouptype-cascade.md) decision.
- **Cross-block (18)**: 92 → 382 lines. 24-row inbound caller table (4 already write IdKey, 11 don't); outbound destination IdKey-acceptance verified (5 still WebForms + integer-only); critical finding that `[ContextAware(typeof(Group))]` is dead code; open-redirect risk on returnUrl.
- **WebForms-isms (19)** + **Misc surfaces (27)**: Full `js-*` class inventory, 4 inline relationship-strength tooltips enumerated, no SignalR; badges, tags (TagCategory missing-attribute bug), following, audit drawer, QuickReturn Lava, HideSecondaryBlocks (ISecondaryBlock), InetCalendarHelper.

Latent bugs surfaced (independent of conversion, flagged for spec phase):
- Duplicate code block at GroupDetail.ascx.cs:2245-2273 (`ShowGroupTypeEditDetails`).
- Possible duplicate-edit corruption in group requirements.
- Hard-coded `EntityTypeId=15` in mdGroupRequirement markup.
- XSS hole in `FormatTriggerType` (HTML-not-encoded user input).
- Missing `TagCategory` block attribute (referenced but never declared).
- Open-redirect risk on `returnUrl` parameter (no validation).

---

---

## Conventions used in research files

- **File:line** anchors point into the WebForms source (`RockWeb/Blocks/Groups/GroupDetail.ascx[.cs]`) by line number for verification.
- **DB Touches**: list of every entity service / direct query / SaveChanges() involved in a feature.
- **State Matrix**: rows are block state, columns are visible/disabled/hidden controls.
- **Side Effects**: anything that mutates state outside the immediate save (cache flush, task queue, history audit, peer-network rebuild).
