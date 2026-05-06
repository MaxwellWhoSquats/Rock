---
author: Maxwell Eley
date_created: 2026-05-06
summary: >-
  Master architecture spec for the multi-phase Obsidian conversion of
  RockWeb/Blocks/Groups/GroupDetail.ascx. Locks cross-phase decisions
  (block class shape, GUID strategy, partial structure, IdKey handling,
  open architectural questions) so per-phase implementation specs can stay
  tight. Read first by every implementation session.
contributors: []
---

# GroupDetail Conversion: Architecture

## Summary

`RockWeb/Blocks/Groups/GroupDetail.ascx` is being replaced with an Obsidian block class plus Vue partials, executing the redesign in the Fall-2025/Spring-2026 Figma. The conversion runs across multiple phases, each gated by its own implementation spec. This document is the cross-phase contract.

The block is exceptionally large (5,950 lines across markup + code-behind, 11 panel widgets, 4 modals, ~12 sub-features), and the redesign is non-trivial (Pure-Vue View panel, Sections & Stacks pattern, ~30 control rename, new Group Image, Linkages section, Map Cards, Audit modal). Both factors argue against single-shot conversion. The phased plan delivers reviewable slices.

Foundational research lives at [research/webforms/](../../../research/webforms/) and [research/design/](../../../research/design/). All claims here trace back to those files.

## Requirements

This spec MUST lock the following before any implementation phase begins:

- The block class base type, generic parameters, and entity-context behavior.
- The block-type GUID strategy (chop-on-startup, no migration).
- The partial structure for the Obsidian SFC.
- The bag and box shapes used by every block action.
- IdKey acceptance and writing policy.
- The GroupType-change reactive cascade strategy.
- A triage decision for every latent bug surfaced in research.
- The handling of every cross-block follow-on the conversion creates.
- Out-of-scope items.

## Open questions for Phase 0 resolution

These items must be resolved during the Phase 0 session by direct user input. Each has a default recommendation drawn from research; the user either confirms or overrides.

### Q1. ContextEntityBlock — keep or drop?

**Background.** The WebForms block declares `[ContextAware(typeof(Group))]` and inherits `ContextEntityBlock`. The deep-research pass confirmed that the block never reads `this.Entity` or calls `ContextEntity<Group>()`. Every code path goes through the URL parameter. Grep across the entire codebase did not surface a single caller relying on the context-aware pathway. See [research/webforms/18-cross-block-dependencies.md](../../../research/webforms/18-cross-block-dependencies.md).

User's earlier decision: "Keep it, assume production pages rely on it."
Research finding: dead code with no observable consumers.

**Open question:** with the deep-research data in hand, does the user revise the decision?

**Default recommendation:** drop. Saves shell complexity in Phase 1.

### Q2. GroupType-change reactive cascade — Approach A, B, or C?

**Background.** When `currentGroupTypeId` changes mid-edit, the form reshapes itself dramatically (panels show/hide, options repopulate, inherited attributes refresh). The cascade map is in [research/webforms/22-grouptype-cascade.md](../../../research/webforms/22-grouptype-cascade.md).

| Approach | Tradeoff |
|---|---|
| A: Front-load all GroupType options into initial `OptionsBag` | Zero round trips, ~3.75 KB per GroupType, ~187 KB for 50 GroupTypes |
| B: Server round-trip per change | Smaller payload, ~50-300ms latency per change |
| C: Hybrid — initial-current + lazy-cache on change | Best UX for "settle on right type" workflows; most code complexity |

**Default recommendation:** Approach A for typical sites; treat C as known fallback if real-world data shows pain. Reference: GroupTypeDetail.cs already implements C with cycle guard.

### Q3. View panel — confirmed Pure Vue (no Lava)

**Resolved by user.** Pure Vue, no `GroupViewLavaTemplate`, no fallback. Customer customizations of the system setting `core_templates_GroupViewTemplate` will be lost on conversion; release notes must call this out. See [research/webforms/17-view-panel.md](../../../research/webforms/17-view-panel.md).

### Q4. IdKey adoption — confirmed accept and write

**Resolved by user.** GroupDetail accepts both integer Id and IdKey on inbound page parameters. GroupDetail writes IdKey on all outbound LinkedPage URLs. Five outbound destinations are still WebForms and integer-only (GroupListPage, FundraisingProgressPage, GroupHistoryPage, GroupMapPage, GroupSchedulerPage); they break if GroupDetail writes IdKey to them. See [research/webforms/18-cross-block-dependencies.md](../../../research/webforms/18-cross-block-dependencies.md).

**Open question:** for the 5 still-WebForms destinations, does the conversion (a) write integer specifically when targeting them, (b) update them to accept IdKey as part of this scope, or (c) defer those updates as separate follow-on work?

**Default recommendation:** (a) write integer specifically when targeting those 5 destinations, defer their IdKey acceptance as follow-on. The conversion stays scoped.

### Q5. Block-type chop strategy — confirmed startup chop

**Resolved by user.** No migration is written. The C# class declares:

```csharp
[Rock.SystemGuid.EntityTypeGuid( "<NEW-GUID>" )]
// was [Rock.SystemGuid.BlockTypeGuid( "<DISCARDED-GUID>" )]
[Rock.SystemGuid.BlockTypeGuid( "582BEEA1-5B27-444D-BC0A-F60CEB053981" )]
public class GroupDetail : RockEntityDetailBlockType<Group, GroupBag>
```

Run `node .claude/skills/convert-block/scripts/generate-guids.js` to obtain the new EntityTypeGuid and discarded BlockTypeGuid. Active BlockTypeGuid reuses the WebForms GUID `582BEEA1-5B27-444D-BC0A-F60CEB053981` (read from [GroupDetail.ascx.cs:190](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:190)). `BlockTypeService.StagePossibleMigrateWebFormsToObsidianBlock` performs the chop at Rock startup.

### Q6. "Trailblazer Settings" — what is it?

**Background.** Listed under "New" in the edit-mode designer notes in the Figma. No corresponding visual element observed in any captured screenshot. Could be a feature flag, a hidden region, or internal Triumph terminology.

**Open question:** what does this refer to, and where does it live in the implementation?

**Default recommendation:** flag for designer clarification; defer until clarified.

### Q7. Audit modal — content and visual

**Background.** The view panel design replaces the WebForms audit drawer with an "Audit Details" modal opened from the panel-header kebab menu. The modal body is not in any captured Figma frame.

**Open question:** is there a separate Figma frame for this modal? If not, what content goes in it?

**Default recommendation:** ask designer for a frame or content spec. As a fallback, port the existing audit-drawer content (Created / Modified date and person) into a vertical stack layout.

### Q8. Group Image data source

**Background.** The redesign adds a 16:9 image at the top of the View panel Overview card and an image uploader in Edit Section 1. The Group entity has an existing `Group.PhotoId` (nullable int → BinaryFile).

**Open question:** is `Group.PhotoId` the right field, or does this require a new column?

**Default recommendation:** reuse `Group.PhotoId`. Apply the same `IsTemporary` toggle pattern used for the chat-channel-avatar binary file. See [research/webforms/14-chat.md](../../../research/webforms/14-chat.md) and [research/webforms/23-validations-and-cascades.md](../../../research/webforms/23-validations-and-cascades.md).

### Q9. Sync Frequency control

**Background.** The Add Group Sync Rule modal redesigns the sync-frequency control: segmented `Mins / Hours / Days` toggle plus a slider (1-31) instead of the WebForms `IntervalPicker`.

**Open question:** build a new dedicated component, restyle `IntervalPicker`, or inline a segmented control plus a basic `<RangeSlider>`?

**Default recommendation:** build a new component scoped to this single modal during Phase 4. The unified visual is a clear net improvement and the cost is bounded.

### Q10. Sections & Stacks + Conditional Well — already shipped or new?

**Background.** The redesign uses three patterns extensively: a `Section` (collapsible, headered), a `Section Stack` (horizontal description + controls layout inside a section), and a `Conditional Well` (left-bordered subdued block wrapping conditional content). These patterns appear in the design system but it is not confirmed whether shared Obsidian components exist.

**Open question:** do shared `<Section>`, `<SectionStack>`, `<ConditionalWell>` components exist in `Rock.JavaScript.Obsidian/Framework/Controls/` (or similar)? If not, do we build them once in Phase 1 as shared primitives, or inline-build per block?

**Default recommendation:** inspect already-converted refresh blocks (e.g., `groupTypeDetail.obs` partials) to confirm. If shared components exist, reuse. If not, build minimal versions in Phase 1 since every later phase depends on them. Do NOT inline-build per block.

### Q11. Coordinator Notifications — empty selection means None?

**Background.** WebForms checkbox list has four entries: None / Accept / Decline / Self-Schedule. None is mutually exclusive with the others. The redesign drops the None entry and shows only Accept / Decline / Self-Schedule.

**Open question:** does empty selection (no boxes ticked) now mean None?

**Default recommendation:** yes. The save logic treats empty selection as `ScheduleCoordinatorNotificationType.None`. Existing groups with `ScheduleCoordinatorNotificationTypes == None` render as zero boxes ticked.

### Q12. Latent bug triage

Six pre-existing bugs surfaced during research. Each needs a Phase 0 classification: **fix-during-conversion**, **defer-to-bugfix-spec**, or **drop**.

| # | Bug | Source | Recommendation |
|---|---|---|---|
| L1 | Duplicate code block at [GroupDetail.ascx.cs:2245-2273](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2245) inside `ShowGroupTypeEditDetails` (same logic appears twice). | [research/webforms/04-code-behind-walkthrough.md](../../../research/webforms/04-code-behind-walkthrough.md) | fix-during (trivial; the new code structure naturally avoids it). |
| L2 | Possible duplicate-edit corruption in group requirements. | [research/webforms/11-group-requirements.md](../../../research/webforms/11-group-requirements.md) | defer-to-bugfix-spec for confirmation; not blocking. |
| L3 | Hard-coded `EntityTypeId=15` in `mdGroupRequirement` markup. | [research/webforms/11-group-requirements.md](../../../research/webforms/11-group-requirements.md) | fix-during (use `EntityTypeCache.Get<DataView>().Id`). |
| L4 | XSS hole in `FormatTriggerType` (user-controlled input HTML-interpolated without encoding). | [research/webforms/13-member-workflow-triggers.md](../../../research/webforms/13-member-workflow-triggers.md) | fix-during. Per memory, "HTML-encode user-controlled values during conversion review." |
| L5 | Missing `TagCategory` block attribute (referenced at [GroupDetail.ascx.cs:543](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:543) but never declared). | [research/webforms/27-misc-surfaces.md](../../../research/webforms/27-misc-surfaces.md) | drop the reference; latent dead code with no consumer. |
| L6 | Open-redirect risk on `returnUrl` parameter (no validation). | [research/webforms/18-cross-block-dependencies.md](../../../research/webforms/18-cross-block-dependencies.md) | fix-during. Validate same-origin or reject. |

## Locked decisions

These items are confirmed and not open for re-discussion in implementation phases.

### Block class

```csharp
[DisplayName( "Group Detail" )]
[Category( "Groups" )]
[Description( "Displays the details of the given group." )]
[IconCssClass( "ti ti-users-group" )]
[SupportedSiteTypes( Model.SiteType.Web )]

[Rock.SystemGuid.EntityTypeGuid( "<run generate-guids.js>" )]
// was [Rock.SystemGuid.BlockTypeGuid( "<run generate-guids.js>" )]
[Rock.SystemGuid.BlockTypeGuid( "582BEEA1-5B27-444D-BC0A-F60CEB053981" )]
public class GroupDetail : RockEntityDetailBlockType<Group, GroupBag>, IBreadCrumbBlock
```

**Why `RockEntityDetailBlockType`**: matches `GroupTypeDetail.cs`, the closest reference block. Provides `GetEntityBagForView`, `GetEntityBagForEdit`, `UpdateEntityFromBox`, `GetInitialEntity`, `TryGetEntityForEditAction` overrides.

**Why `IBreadCrumbBlock`**: GroupDetail must contribute to the breadcrumb trail per `GetBreadCrumbs(PageReference)` in WebForms.

### Block actions

| Action | Returns | Notes |
|---|---|---|
| `Edit` | `ValidPropertiesBox<GroupBag>` | Returns full edit-mode bag for an existing group, or a new bag for `groupId == 0`. |
| `Save` | `ValidPropertiesBox<GroupBag>` (200) OR redirect URL string (201) | 200 on update (stay on page in view mode), 201 on create with redirect to the new group's URL. |
| `Delete` | redirect URL string | Auth-checked, runs the WebForms delete logic verbatim (see [research/webforms/16-archive-delete-copy.md](../../../research/webforms/16-archive-delete-copy.md)). |
| `Archive` | redirect URL string | Single-group archive. |
| `ArchiveWithChildren` | redirect URL string | Cascade archive. |
| `Copy` | redirect URL string | Includes `IncludeChildGroups` parameter; default UNCHECKED (changed from WebForms which had it CHECKED). |
| `GetGroupTypeOptions` | `GroupTypeOptions` (Approach B/C only) | Conditional on Q2 resolution. |

### Partial structure

Mirror `groupTypeDetail.obs`:

```
Rock.JavaScript.Obsidian.Blocks/src/Group/groupDetail.obs              # top-level shell
Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/
  viewPanel.partial.obs                                                # Phase 1
  editPanel.partial.obs                                                # Phase 2 (orchestrates section partials)
  mapCard.partial.obs                                                  # Phase 5 (per-location card; conditional)
  groupAttributesPanel.partial.obs                                     # Phase 3
  memberAttributesPanel.partial.obs                                    # Phase 3
  requirementsPanel.partial.obs                                        # Phase 4
  syncPanel.partial.obs                                                # Phase 4
  workflowsPanel.partial.obs                                           # Phase 4
  locationsPanel.partial.obs                                           # Phase 5 (Section 4 stacks)
  syncRuleModal.partial.obs                                            # Phase 4
  workflowModal.partial.obs                                            # Phase 4
  requirementModal.partial.obs                                         # Phase 4
  locationModal.partial.obs                                            # Phase 5
  copyModal.partial.obs                                                # Phase 1
  auditModal.partial.obs                                               # Phase 1
  types.partial.ts                                                     # NavigationUrlKey, enums, helpers
  utility.partial.ts                                                   # formatters, defaults
```

Final names may shift slightly during phase specs but the structure stays.

### ViewModels (bags)

```
Rock.ViewModels/Blocks/Group/GroupDetail/
  GroupBag.cs                              # main edit/view bag
  GroupDetailOptionsBag.cs                 # block options + (Approach A) per-GroupType options dictionary
  GroupTypeOptionsBag.cs                   # per-GroupType reactive options
  GroupLocationBag.cs                      # state collection
  GroupRequirementBag.cs
  GroupSyncBag.cs
  GroupMemberWorkflowTriggerBag.cs
  GroupMemberAttributeBag.cs
  CopyGroupRequestBag.cs
  GroupTypeOptionsResponseBag.cs           # (Approach B/C only)
```

### IdKey policy

- **Inbound**: page parameter `GroupId` accepts both integer and IdKey form. `GetInitialEntity<Group, GroupService>(RockContext, "GroupId")` handles both.
- **Outbound**: every URL `GroupDetail` writes uses IdKey for the `GroupId` parameter, EXCEPT when targeting the 5 still-WebForms destinations (per Q4 default recommendation): GroupListPage, FundraisingProgressPage, GroupHistoryPage, GroupMapPage, GroupSchedulerPage. For those, write integer Id specifically.

### Phase roadmap

Per [research/webforms/21-phase-partitioning.md](../../../research/webforms/21-phase-partitioning.md), tightened by [research/design/](../../../research/design/):

| Phase | Title | Output |
|---|---|---|
| 0 | Architecture & Phase 1 spec | This document + `01-phase-1-shell-and-view.md` |
| 1 | Block shell + View panel + Delete/Archive/Copy + Audit modal + Linkages bag | Working view-mode block. Edit mode is placeholder. |
| 2 | Edit panel core (Top fields + General + RSVP + Scheduling + Chat sections) | All scalar field editing + cascade reactivity. |
| 3 | Attributes (Group + Member definitions) | Attribute editor working. |
| 4 | Requirements + Sync + Member Workflows | Three sub-feature panels with modals. |
| 5 | Locations & Schedules (incl. Map Cards in view) | Most complex sub-feature; meeting details fully editable. |
| 6 | Net-new features beyond the parity-plus-design baseline | Likely empty unless Trailblazer Settings becomes substantial. |
| 7 | Cutover and cleanup | Verify chop, delete WebForms files, smoke test cross-block callers, release notes. |

Per [research/design/03-net-new-features.md](../../../research/design/03-net-new-features.md), the design pass folded the prior Phase 6 (View panel redesign) into Phase 1 because the Figma is locked.

## Cross-block follow-on tracking

These do NOT ship in this conversion but must be tracked separately so they're not forgotten:

### Outbound destinations needing IdKey acceptance

5 still-WebForms blocks. GroupDetail writes integer Id when targeting them. Eventually each should be updated to accept IdKey.

| Block setting | Destination block | Path |
|---|---|---|
| GroupListPage | `Groups/GroupList.ascx` (the tree-view sidebar) | RockWeb/Blocks/Groups/ |
| FundraisingProgressPage | Fundraising progress block | location TBD |
| GroupHistoryPage | Group history block | location TBD |
| GroupMapPage | Group map block | location TBD |
| GroupSchedulerPage | Group scheduler block | location TBD |

### Inbound callers writing integer Id

11 callers continue to write integer GroupId. GroupDetail accepts both forms so this is non-blocking. Eventually each should be updated to write IdKey.

See [research/webforms/18-cross-block-dependencies.md](../../../research/webforms/18-cross-block-dependencies.md) for the full list.

## Out of scope

- Convert any of the 5 still-WebForms outbound destinations.
- Convert any of the 11 still-integer-Id inbound callers.
- Add new functional features beyond what the Figma defines.
- Restyle non-block-specific components (TagList, FollowingsHelper, etc.) unless required by a phase.
- Migrate `Group.PhotoId` users on the Person profile or other surfaces.
- Touch the `core_templates_GroupViewTemplate` SystemSetting (it stays in place; just unused by GroupDetail).

## Verification (cross-phase)

The full conversion is verified when:

- `Rock.sln` builds clean.
- Every block-attribute combination from research is exercised manually (or via tests if present).
- Every state from [research/webforms/02-block-states.md](../../../research/webforms/02-block-states.md) renders correctly.
- Every save scenario from [research/webforms/23-validations-and-cascades.md](../../../research/webforms/23-validations-and-cascades.md) preserves the same database side effects.
- All 11 outbound LinkedPage links navigate correctly.
- The Rock startup chop runs successfully on a fresh database (BlockType row's EntityType swapped, Path cleared, all page Block instances retain their attribute values).
- Smoke test from each of the 14+ inbound callers documented in [research/webforms/18-cross-block-dependencies.md](../../../research/webforms/18-cross-block-dependencies.md).
- WebForms files (`GroupDetail.ascx`, `.ascx.cs`, `.ascx.designer.cs`) deleted from source control.

Per-phase verification lives in each phase spec.

## Phase 0 session checklist

When the user runs the Phase 0 session, this document is the working surface. Steps:

1. Read this document end to end.
2. Read both research halves end to end (or selectively; the doc links each region).
3. Resolve Q1-Q12 by direct user input.
4. Update this document to record each resolution (replace "Default recommendation" with "Resolved: <decision>").
5. Mark this document as locked at the bottom of the file.
6. Optionally: author `01-phase-1-shell-and-view.md` while context is fresh, OR defer to the Phase 1 session.
7. Commit and end the session.

After Phase 0, every later phase reads this locked document at session start.

## Status

**Draft** — awaiting Phase 0 session for user input on Q1-Q12.

When all open questions are resolved, change to "Locked".
