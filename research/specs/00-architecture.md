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

Foundational research lives at [research/webforms/](../webforms/) and [research/design/](../design/). All claims here trace back to those files.

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

**Background.** The WebForms block declares `[ContextAware(typeof(Group))]` and inherits `ContextEntityBlock`. The deep-research pass confirmed that the block never reads `this.Entity` or calls `ContextEntity<Group>()`. Every code path goes through the URL parameter. Grep across the entire codebase did not surface a single caller relying on the context-aware pathway. See [research/webforms/18-cross-block-dependencies.md](../webforms/18-cross-block-dependencies.md).

User's earlier decision: "Keep it, assume production pages rely on it."
Research finding: dead code with no observable consumers.

**Open question:** with the deep-research data in hand, does the user revise the decision?

**Resolved:** drop. Research confirmed zero callers and no `this.Entity` reads, so the attribute and base class are dead weight; dropping simplifies the Phase 1 shell.

### Q2. GroupType-change reactive cascade — Approach A, B, or C?

**Background.** When `currentGroupTypeId` changes mid-edit, the form reshapes itself dramatically (panels show/hide, options repopulate, inherited attributes refresh). The cascade map is in [research/webforms/22-grouptype-cascade.md](../webforms/22-grouptype-cascade.md).

| Approach | Tradeoff |
|---|---|
| A: Front-load all GroupType options into initial `OptionsBag` | Zero round trips, ~3.75 KB per GroupType, ~187 KB for 50 GroupTypes |
| B: Server round-trip per change | Smaller payload, ~50-300ms latency per change |
| C: Hybrid — initial-current + lazy-cache on change | Best UX for "settle on right type" workflows; most code complexity |

**Resolved:** Approach B (server round-trip per change). GroupType rarely changes mid-edit on real groups, so the initial-payload cost of Approach A is overkill; a single server fetch on change is the right tradeoff.

### Q3. View panel — Pure Vue (no Lava)

**Resolved:** Pure Vue, no `GroupViewLavaTemplate`, no fallback. Confirmed by user. Customer customizations of the system setting `core_templates_GroupViewTemplate` will be lost on conversion; release notes must call this out. See [research/webforms/17-view-panel.md](../webforms/17-view-panel.md).

### Q4. IdKey adoption — accept and write, with a dedicated dependencies phase

**Resolved:** GroupDetail accepts both integer Id and IdKey on inbound page parameters and writes IdKey on **all 11** outbound LinkedPage URLs uniformly (no per-destination special-casing). The 5 still-WebForms destinations (GroupListPage, FundraisingProgressPage, GroupHistoryPage, GroupMapPage, GroupSchedulerPage) will break under IdKey URLs until they are updated to accept IdKey, which is now scoped as a new "Update dependencies" phase preceding cutover. See [research/webforms/18-cross-block-dependencies.md](../webforms/18-cross-block-dependencies.md).

### Q5. Block-type chop strategy — startup chop

**Resolved:** No migration is written. The Rock startup chop replaces the WebForms BlockType row in place. Confirmed by user. The C# class declares:

```csharp
[Rock.SystemGuid.EntityTypeGuid( "<NEW-GUID>" )]
// was [Rock.SystemGuid.BlockTypeGuid( "<DISCARDED-GUID>" )]
[Rock.SystemGuid.BlockTypeGuid( "582BEEA1-5B27-444D-BC0A-F60CEB053981" )]
public class GroupDetail : RockEntityDetailBlockType<Group, GroupBag>
```

Run `node .claude/skills/convert-block/scripts/generate-guids.js` to obtain the new EntityTypeGuid and discarded BlockTypeGuid. Active BlockTypeGuid reuses the WebForms GUID `582BEEA1-5B27-444D-BC0A-F60CEB053981` (read from [GroupDetail.ascx.cs:190](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:190)). `BlockTypeService.StagePossibleMigrateWebFormsToObsidianBlock` performs the chop at Rock startup.

### Q6. "Trailblazer Settings" — visual-decoration prop, not a separate feature

**Background.** Listed under "New" in the edit-mode designer notes in the Figma. No corresponding visual element observed in any captured screenshot. Could be a feature flag, a hidden region, or internal Triumph terminology.

**Resolved:** "Trailblazer" settings are individual fields that the Figma highlights in blue on the edit panel (advanced / power-user fields). User confirmed inspection of the Figma shows them only in the General content section. Implementation: the affected controls take a `trailBlazerField` prop that drives the blue-highlight styling. NOT a net-new feature, NOT a separate panel; Phase 2 (which owns the General section) wires this up where applicable. Phase 6 stays empty unless something else surfaces.

### Q7. Audit modal — content and visual

**Background.** The view panel design replaces the WebForms audit drawer with an "Audit Details" modal opened from the panel-header kebab menu. The modal body is not in any captured Figma frame.

**Resolved:** The Figma does include the Audit Details frame; user supplied the screenshot. Trigger: kebab menu in the panel header with a single entry, "Audit Details" (no other kebab actions). Modal title: "Audit Details". Body is a single horizontal row with three columns:
- **Created By**: person name + relative time (e.g., "Alisha Marble (1 month ago)").
- **Modified By**: person name + relative time (e.g., "Alisha Marble (1 week ago)").
- **Id**: the numeric `Group.Id` (display only; not the IdKey).

The footer has standard Modal chrome (Cancel + Save shown in Figma; since the modal is read-only, Phase 1 ships with Cancel only; flag if a writable variant is intended later).

### Q8. Group Image data source — new column required

**Background.** The redesign adds a 16:9 image at the top of the View panel Overview card and an image uploader in Edit Section 1. **The Group entity does NOT currently have a photo column** (verified: grep for `Photo` in [Rock/Model/Group/Group/](Rock/Model/Group/Group/) returns zero matches). Earlier drafts of this spec and several design docs incorrectly assumed `Group.PhotoId` existed; that was a pattern-match from `Person.PhotoId` ([Person.cs:242](Rock/Model/CRM/Person/Person.cs:242)) and is not real. The only image-shaped FK on Group today is `ChatChannelAvatarBinaryFileId` ([Group.cs:623](Rock/Model/Group/Group/Group.cs:623)), which is chat-specific and not appropriate to reuse for the redesign hero image.

So this is not a "reuse vs. new" question; it is a "what do we name the new column" question.

| Option | Name | Argument |
|---|---|---|
| **(a)** | `PhotoId` (int? → BinaryFile) | Mirrors `Person.PhotoId`. Per the Prime Directive (follow existing patterns), this is the cross-Rock convention for "primary entity image." |
| **(b)** | `HeroImageBinaryFileId` (int? → BinaryFile) | Group-internal-consistent with `ChatChannelAvatarBinaryFileId`. More descriptive of intent (the image is a 16:9 hero, not a person-style headshot). |

**Resolved:** Add `Group.PhotoId` (nullable int → BinaryFile, FK with `WillCascadeOnDelete(false)` and `ON DELETE SET NULL` per data-model rules), mirroring `Person.PhotoId`. Per the Prime Directive, follow the existing cross-Rock convention. Apply the same `IsTemporary` toggle pattern the chat-channel-avatar uses for orphan cleanup. The column add (entity + migration + EntityTypeConfiguration nav property + codegen regen) lands in **Phase 2** alongside the chat-avatar editing work. See [research/webforms/14-chat.md](../webforms/14-chat.md) and [research/webforms/23-validations-and-cascades.md](../webforms/23-validations-and-cascades.md). Phase 1 (View panel) wires up the hero region but the bag's photo URL is always null until Phase 2 ships, so the region omits during the gap.

### Q9. Sync Frequency control

**Background.** The Add Group Sync Rule modal redesigns the sync-frequency control: segmented `Mins / Hours / Days` toggle plus a slider (1-31) instead of the WebForms `IntervalPicker`.

**Open question:** build a new dedicated component, restyle `IntervalPicker`, or inline a segmented control plus a basic `<RangeSlider>`?

**Resolved:** Reuse the existing Obsidian `<IntervalPicker>` (option b). It already supports the Mins / Hours / Days unit segmentation natively; the work is purely a styling change so the unit toggle renders above the numeric/slider input instead of inline. Phase 4 owns the restyle. Do this via a scoped variant (prop or local style override) so other `<IntervalPicker>` consumers across Rock are not affected; Phase 4 spec records the exact mechanism after a quick component audit.

### Q10. Sections & Stacks + Conditional Well — reuse existing core components

**Resolved:** All three patterns ship as shared core components today; reuse them, do not rebuild. Verified by inspecting `Rock.JavaScript.Obsidian/Framework/Controls/`:

- **`<ContentSection>`** at [contentSection.obs](Rock.JavaScript.Obsidian/Framework/Controls/contentSection.obs). Collapsible/headered section with `title`, `icon`, `description`, `headerActions` / `headerSecondaryActions` slots, and `disableCollapse` prop. Anchor-aware (works with `<ContentSectionContainer>` for sidebar nav). The redesign explicitly removes the section nav (per [research/design/03-net-new-features.md](../design/03-net-new-features.md) item R5), so use `<ContentSection>` standalone, not inside a container.
- **`<ContentStack>`** at [contentStack.obs](Rock.JavaScript.Obsidian/Framework/Controls/contentStack.obs). Horizontal description-on-the-left, controls-on-the-right layout. Props: `title`, `description`, `help`, plus a `header` slot.
- **`<ConditionalWell>`** at [conditionalWell.obs](Rock.JavaScript.Obsidian/Framework/Controls/conditionalWell.obs). Pure styled container (`<div class="well well-conditional">`) with a default slot. Use this for every left-bordered subdued conditional region called out in the redesign (Inactive flow, Security Level, Coordinator Notifications sub-fields, Workflow trigger qualifier sub-fields, Capacity matrix, etc.).

The Phase 1 spec must call out that GroupDetail uses these core components (no per-block reimplementation, no per-block CSS overrides beyond minor scoped tweaks).

### Q11. Coordinator Notifications — empty selection means None?

**Background.** WebForms checkbox list has four entries: None / Accept / Decline / Self-Schedule. None is mutually exclusive with the others. The redesign drops the None entry and shows only Accept / Decline / Self-Schedule.

**Open question:** does empty selection (no boxes ticked) now mean None?

**Resolved:** Yes. Empty selection (no boxes ticked) means `ScheduleCoordinatorNotificationType.None`. The redesign drops the explicit None checkbox; the save logic treats zero-selected as None. Existing groups with `ScheduleCoordinatorNotificationTypes == None` render as zero boxes ticked.

### Q12. Latent bug triage

Six pre-existing bugs surfaced during research. Each needs a Phase 0 classification: **fix-during-conversion**, **defer-to-bugfix-spec**, or **drop**.

**Resolved:** All six classifications below confirmed by user. The "fix-during" rows are scoped into the phase that owns the affected feature (per the "Lands in" column).

| # | Bug | Source | Resolution | Lands in |
|---|---|---|---|---|
| L1 | Duplicate code block at [GroupDetail.ascx.cs:2245-2273](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2245) inside `ShowGroupTypeEditDetails` (same logic appears twice). | [research/webforms/04-code-behind-walkthrough.md](../webforms/04-code-behind-walkthrough.md) | fix-during (trivial; the new code structure naturally avoids it). | Phase 2 (edit core / GroupType edit details). |
| L2 | Possible duplicate-edit corruption in group requirements. | [research/webforms/11-group-requirements.md](../webforms/11-group-requirements.md) | defer-to-bugfix-spec for confirmation; not blocking. | Separate `/bugfix` spec. |
| L3 | Hard-coded `EntityTypeId=15` in `mdGroupRequirement` markup. | [research/webforms/11-group-requirements.md](../webforms/11-group-requirements.md) | fix-during (use `EntityTypeCache.Get<DataView>().Id`). | Phase 4 (requirements modal). |
| L4 | XSS hole in `FormatTriggerType` (user-controlled input HTML-interpolated without encoding). | [research/webforms/13-member-workflow-triggers.md](../webforms/13-member-workflow-triggers.md) | fix-during. Per memory, "HTML-encode user-controlled values during conversion review." | Phase 4 (member workflow triggers). |
| L5 | Missing `TagCategory` block attribute (referenced at [GroupDetail.ascx.cs:543](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:543) but never declared). | [research/webforms/27-misc-surfaces.md](../webforms/27-misc-surfaces.md) | drop the reference; latent dead code with no consumer. | Phase 1 (block-attribute declarations); simply do not port the reference. |
| L6 | Open-redirect risk on `returnUrl` parameter (no validation). | [research/webforms/18-cross-block-dependencies.md](../webforms/18-cross-block-dependencies.md) | fix-during. Validate same-origin or reject. | Phase 1 (Delete/Archive/Copy redirect handling reads `returnUrl`). |

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
| `Delete` | redirect URL string | Auth-checked, runs the WebForms delete logic verbatim (see [research/webforms/16-archive-delete-copy.md](../webforms/16-archive-delete-copy.md)). |
| `Archive` | redirect URL string | Single-group archive. |
| `ArchiveWithChildren` | redirect URL string | Cascade archive. |
| `Copy` | redirect URL string | Includes `IncludeChildGroups` parameter; default UNCHECKED (changed from WebForms which had it CHECKED). |
| `GetGroupTypeOptions` | `GroupTypeOptionsBag` | Required by Q2 Approach B; called from the Vue layer when `currentGroupTypeId` changes mid-edit. |

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
  GroupDetailOptionsBag.cs                 # block options + initial GroupType options (for the current GroupType only, per Q2 Approach B)
  GroupTypeOptionsBag.cs                   # per-GroupType reactive options; returned by GetGroupTypeOptions block action on cascade
  GroupLocationBag.cs                      # state collection
  GroupRequirementBag.cs
  GroupSyncBag.cs
  GroupMemberWorkflowTriggerBag.cs
  GroupMemberAttributeBag.cs
  CopyGroupRequestBag.cs
```

### IdKey policy

- **Inbound**: page parameter `GroupId` accepts both integer and IdKey form. `GetInitialEntity<Group, GroupService>(RockContext, "GroupId")` handles both.
- **Outbound**: every URL `GroupDetail` writes uses IdKey for the `GroupId` parameter, uniformly across all 11 outbound LinkedPage destinations. The 5 still-WebForms destinations (GroupListPage, FundraisingProgressPage, GroupHistoryPage, GroupMapPage, GroupSchedulerPage) are updated to accept IdKey in the new "Update dependencies" phase before cutover; until that phase ships, those 5 links are temporarily broken under IdKey URLs (acknowledged tradeoff).

### Phase roadmap

Per [ROADMAP.md](ROADMAP.md), tightened by [research/design/](../design/):

| Phase | Title | Output |
|---|---|---|
| 0 | Architecture & Phase 1 spec | This document + `01-phase-1-shell-and-view.md` |
| 1 | Block shell + View panel + Delete/Archive/Copy + Audit modal + Linkages bag | Working view-mode block. Edit mode is placeholder. |
| 2 | Edit panel core (Top fields + General + RSVP + Scheduling + Chat sections) | All scalar field editing + cascade reactivity. |
| 3 | Attributes (Group + Member definitions) | Attribute editor working. |
| 4 | Requirements + Sync + Member Workflows | Three sub-feature panels with modals. |
| 5 | Locations & Schedules (incl. Map Cards in view) | Most complex sub-feature; meeting details fully editable. |
| 6 | Net-new features beyond the parity-plus-design baseline | Likely empty unless Trailblazer Settings becomes substantial. |
| 7 | Update dependencies | Update the 5 still-WebForms destinations (GroupListPage, FundraisingProgressPage, GroupHistoryPage, GroupMapPage, GroupSchedulerPage) so each accepts IdKey on its `GroupId` page parameter. Required because Phase 1+ writes IdKey to all 11 outbound URLs (per Q4) and these 5 destinations will be broken until updated. |
| 8 | Cutover and cleanup | Verify chop, delete WebForms files, smoke test cross-block callers, release notes. |

Per [research/design/03-net-new-features.md](../design/03-net-new-features.md), the design pass folded the prior Phase 6 (View panel redesign) into Phase 1 because the Figma is locked.

## Cross-block follow-on tracking

### Outbound destinations needing IdKey acceptance (scoped into Phase 7)

The 5 still-WebForms outbound destinations are now in scope for the new Phase 7 ("Update dependencies"). Each will be updated to accept IdKey on its `GroupId` page parameter. Until Phase 7 ships, GroupDetail's IdKey URLs to these destinations are broken (acknowledged tradeoff per Q4).

| Block setting | Destination block | Path |
|---|---|---|
| GroupListPage | `Groups/GroupList.ascx` (the tree-view sidebar) | RockWeb/Blocks/Groups/ |
| FundraisingProgressPage | Fundraising progress block | location TBD |
| GroupHistoryPage | Group history block | location TBD |
| GroupMapPage | Group map block | location TBD |
| GroupSchedulerPage | Group scheduler block | location TBD |

### Inbound callers writing integer Id (out of scope)

11 callers continue to write integer GroupId. GroupDetail accepts both forms so this is non-blocking. Eventually each should be updated to write IdKey, but that work is NOT in scope for this conversion or for Phase 7 (which is focused on the broken outbound side).

See [research/webforms/18-cross-block-dependencies.md](../webforms/18-cross-block-dependencies.md) for the full list.

## Out of scope

- Convert any of the 5 still-WebForms outbound destinations to Obsidian. Phase 7 only adds IdKey acceptance to those WebForms blocks; full Obsidian conversion is separate work.
- Convert any of the 11 still-integer-Id inbound callers.
- Add new functional features beyond what the Figma defines.
- Restyle non-block-specific components (TagList, FollowingsHelper, etc.) unless required by a phase.
- Migrate any cross-entity references that would touch the new `Group.PhotoId` column added in Phase 2 (e.g., Person profile photo surfaces). The new column is GroupDetail-scoped only.
- Touch the `core_templates_GroupViewTemplate` SystemSetting (it stays in place; just unused by GroupDetail).

## Verification (cross-phase)

The full conversion is verified when:

- `Rock.sln` builds clean.
- Every block-attribute combination from research is exercised manually (or via tests if present).
- Every state from [research/webforms/02-block-states.md](../webforms/02-block-states.md) renders correctly.
- Every save scenario from [research/webforms/23-validations-and-cascades.md](../webforms/23-validations-and-cascades.md) preserves the same database side effects.
- All 11 outbound LinkedPage links navigate correctly.
- The Rock startup chop runs successfully on a fresh database (BlockType row's EntityType swapped, Path cleared, all page Block instances retain their attribute values).
- Smoke test from each of the 14+ inbound callers documented in [research/webforms/18-cross-block-dependencies.md](../webforms/18-cross-block-dependencies.md).
- WebForms files (`GroupDetail.ascx`, `.ascx.cs`, `.ascx.designer.cs`) deleted from source control.

Per-phase verification lives in each phase spec.

## Phase 0 session checklist

When the user runs the Phase 0 session, this document is the working surface. The session follows [SESSION-PROTOCOL.md](SESSION-PROTOCOL.md) opening + closing steps (Phase 0 is spec-only so it skips the implementation and build sections).

Steps:

1. Read [SESSION-PROTOCOL.md](SESSION-PROTOCOL.md) (this is the procedure for every session).
2. Read [PHASE-SPEC-TEMPLATE.md](PHASE-SPEC-TEMPLATE.md) (this is the template you'll use to author the Phase 1 spec).
3. Read this document end to end.
4. Read [ROADMAP.md](ROADMAP.md).
5. Read both research halves end to end (or selectively; the doc links each region).
6. Resolve Q1-Q12 by direct user input. For each: state the question, summarize the default recommendation, ask for confirm/override, update the spec to record the resolution.
7. Mark this document's status as `Locked` at the bottom.
8. Author `01-phase-1-shell-and-view.md` using PHASE-SPEC-TEMPLATE.md as the starting structure. Pay special attention to the "Research coverage" and "Implementation checklist" sections — these drive the self-review at session close, so they must be specific.
9. Update [INDEX.md](INDEX.md): mark architecture as `locked`, mark Phase 1 spec as `draft` awaiting user review.
10. Output a suggested commit message and tell the user the changes are uncommitted. Do NOT run `git commit` or `/commit`. Suggested message: `- (Group) Phase 0: Architecture spec locked + Phase 1 implementation spec drafted.`

After Phase 0, every later phase reads this locked document at session start, follows SESSION-PROTOCOL.md, and produces a coverage report at session close. The user owns git history at phase boundaries.

## Status

**Locked** on 2026-05-06. All twelve open questions (Q1-Q12) resolved by user during the Phase 0 session. Cross-phase contract is now canonical for all later implementation phases.

Notable resolutions worth flagging here:
- Q1: dropped `[ContextAware(typeof(Group))]` and `ContextEntityBlock` base.
- Q2: GroupType cascade uses Approach B (server round-trip); the `GetGroupTypeOptions` block action is required by Phase 2.
- Q4: GroupDetail writes IdKey uniformly to all 11 outbound destinations; the 5 still-WebForms destinations are scoped into the new **Phase 7 ("Update dependencies")** to accept IdKey before cutover. The phase roadmap was renumbered to 9 phases (0-8) as a result.
- Q8: `Group.PhotoId` does not exist today; a new column is added in Phase 2 (mirroring `Person.PhotoId`).
- Q9: Sync Frequency reuses the existing Obsidian `<IntervalPicker>` with a Phase 4 styling pass.
- Q10: `<ContentSection>`, `<ContentStack>`, `<ConditionalWell>` already ship as core components; reuse, do not rebuild.
- Q12: latent bugs assigned per-phase landing slots; L2 is the only bug deferred to a separate `/bugfix` spec.
