---
author: Maxwell Eley
date_created: 2026-05-11
summary: >-
  Phase 5 implementation spec for the GroupDetail Obsidian conversion. Adds
  three sub-features in a single phase because they share the same panel
  shape (editable Grid + Add/Edit Modal + SyncRelatedEntities save):
  Section 7 (Group Requirements), Section 9 (Group Sync), Section 10
  (Group Member Workflow Triggers). Includes the Sync Frequency restyle
  on the existing <IntervalPicker> per Q9, the L3 (hard-coded
  EntityTypeId=15) fix-during, and the L4 (XSS hole in
  FormatTriggerType) fix-during with HTML encoding of user-controlled
  values. Locations editing modal stays deferred to Phase 6.
contributors: []
---

# Phase 5: Group Requirements + Group Sync + Member Workflow Triggers

## Context

Phase 4 ([04-phase-4-attributes.md](04-phase-4-attributes.md)) shipped Section 5 (Group Attribute Values) and Section 6 (Group Member Attribute Definitions). The edit panel now renders inline `<NotificationBox>` "Coming in Phase 5" placeholders for Sections 7, 9, and 10. Phase 5 replaces all three placeholders with working editors and extends the `Save` block action with three new save bodies. The three sub-features are bundled because they share the same panel shape (editable Grid + Add/Edit Modal + state-list save pattern) and the same `SyncRelatedEntities<TEntity>` helper pattern from `GroupTypeDetail.cs`. Sub-feature panels for Locations editing (Section 4 Stack 2) stay deferred to Phase 6.

Architectural decisions for this phase are governed by [00-architecture.md](00-architecture.md), specifically Q9 (Sync Frequency restyle on the existing `<IntervalPicker>`) and Q12 (L3 + L4 fix-during).

## Behavior delivered

- **Section 7 (Group Requirements)**: ADMINISTRATE-gated panel with two grids — a read-only "Group Requirements for Group Type" grid (no inheritance walk, mirrors WebForms parity) sourced from `GroupTypeOptionsBag.GroupTypeRequirements`; an editable "Specific Group Requirements" grid (Add / Edit / Delete) for per-group requirements. Add button is gated on `GroupType.EnableSpecificGroupRequirements`. Modal uses `<Modal>` + per-field controls (GroupRequirementType picker, GroupRolePicker, Age Classification radio, DataView picker, Allow Leader Override, Require Before Adding, Due Date conditional). L3 fix-during: replace the hard-coded `EntityTypeId=15` in `dvpAppliesToDataView` markup with `EntityTypeCache.Get<Rock.Model.Person>().Id` via a bag field. Duplicate-detection: same `(GroupRequirementType, GroupRoleId)` pair surfaces a notification box.
- **Section 9 (Group Sync)**: ADMINISTRATE-gated panel with one editable Grid (Role / Data View / Sync Interval / Last Sync / Edit / Delete). Add button gated on `GroupType.AllowGroupSync` (or surfaces unconditionally when any existing GroupSync rows exist, so legacy syncs are editable). Modal uses `<Modal>` + Sync Data View picker (EntityType = Person, set server-side, NOT a per-modal hardcoded magic number), Group Role dropdown filtered to roles not already synced for this group (except the row currently being edited), Sync Interval `<IntervalPicker>` (Q9 restyle: segmented Mins/Hours/Days toggle above slider; scoped variant prop so other consumers are not affected), Welcome / Exit communication dropdowns, Create-login-during-sync checkbox.
- **Section 10 (Group Member Workflow Triggers)**: panel gated on `GroupType.AllowSpecificGroupMemberWorkflows`. Editable Grid: Trigger Name / Workflow / Trigger Event / Active / Edit / Delete (no Reorder per design; flagged as a research-noted parity drop in [research/design/02-edit-panel.md "Open questions"](../design/02-edit-panel.md)). Modal: Trigger Name text box, Workflow Type picker, Trigger Event dropdown, Active checkbox, qualifier conditional well (per `MemberAddedToGroup` / `MemberRemovedFromGroup` / `MemberRoleChanged` / `MemberStatusChanged` / `MemberAttributeChanged` per webforms/13-member-workflow-triggers.md). L4 fix-during: every user-controlled value rendered into the Vue template uses interpolation (Vue auto-escapes); any server-side string emitted into HTML attributes or grid cells is HTML-encoded via the framework helper.
- **Save flow extension**: the 9-step `WrapTransaction` Phase 4 ended on grows to 12 steps. After step 4b (member attribute definitions) and before step 5 (Inactive cascade), insert step 4c (Group Requirements sync via the deferred-insert pattern from `GroupDetail.ascx.cs:873-886, 1330-1334`), step 4d (Group Sync entities via `SyncRelatedEntities`), step 4e (Group Member Workflow Triggers via `SyncRelatedEntities`).
- **Cascade refresh**: when `GroupTypeId` changes mid-edit, the Phase 5 sub-feature panels reactively re-evaluate their visibility from the cascade payload (`GroupTypeOptionsBag.EnableSpecificGroupRequirements` / `AllowGroupSync` / `AllowSpecificGroupMemberWorkflows` are added to the bag in this phase). Per-group requirement / sync / trigger rows stay attached to the group across GroupType changes (matches Phase 4's member-attribute behavior).
- **Cache invalidation**: when any GroupMemberWorkflowTrigger row is added / edited / deleted, fire `GroupMemberWorkflowTriggerService.RemoveCachedTriggers()` post-save per webforms/23-validations-and-cascades.md.
- **Latent bug fixes**:
  - **L3** ([webforms/11-group-requirements.md "Open questions"](../webforms/11-group-requirements.md)): replace the hard-coded `EntityTypeId=15` in the markup with `EntityTypeCache.Get<Rock.Model.Person>().Id` (or use the Obsidian `<DataViewPicker>` control's `entityTypeGuid` prop with `EntityType.Person`).
  - **L4** ([webforms/13-member-workflow-triggers.md "Latent bugs"](../webforms/13-member-workflow-triggers.md)): the WebForms `FormatTriggerType` body interpolates user-controlled values (workflow role name, status name) into an HTML string without encoding. Phase 5 ports the formatting logic to a Vue template (Vue auto-escapes) or, if server-side, HTML-encodes via `System.Web.HttpUtility.HtmlEncode` (with the System.Web import wrapped in `#if WEBFORMS` per CLAUDE.md — but since the conversion drops System.Web for Obsidian blocks, prefer the Vue-template approach).
- **Phase 4 carry-forward (latent findings from Phase 4 self-review)**:
  - **L4-carry-1**: ADMINISTRATE / `AllowSpecificGroupMemberAttributes` panel-visibility gate on Section 6. Add `CanAdministrate` to GroupBag (or surface `IsSection6Visible` on the options bag); wrap the Section 6 `<ContentSection>` in `v-if`. One-line additions per side; piggybacks on the Section 7 ADMINISTRATE flag work.
  - **L4-carry-2**: Add-path GroupType cascade refresh of `bag.attributes`. Extend the reactive `groupTypeId` watcher in `groupDetail.obs` to also re-fetch `bag.attributes` (and `attributeValues`) on cascade. Alternatively: invalidate Section 5 entirely until Save / Re-edit. Decide during Phase 5 spec lock.

## Behavior NOT delivered

- Locations editing modal + inline schedule logic (Section 4 Stack 2): **Phase 6**.
- Update dependencies (5 still-WebForms outbound destinations): **Phase 7**.
- Cutover and WebForms file deletion: **Phase 8**.

## Deferred behaviors inherited from prior phases

Walks Phase 1, Phase 2, Phase 3, and Phase 4 coverage reports for `→ DEFERRED to Phase 5` rows.

### Inherited (covered in this phase's Implementation checklist)

| Source phase | Behavior | Coverage-report origin | Checklist item that handles it |
|---|---|---|---|
| Phase 3 | Section 7 Group Requirements panel | Phase 3 "Behavior NOT delivered" | R1-R8 |
| Phase 3 | Section 9 Group Sync panel | Phase 3 "Behavior NOT delivered" | S1-S7 |
| Phase 3 | Section 10 Group Member Workflow Triggers panel | Phase 3 "Behavior NOT delivered" | W1-W7 |
| Phase 3 | Sync Frequency restyle per Q9 | Phase 3 "Behavior NOT delivered" | S5 |
| Phase 3 | L3 (hard-coded EntityTypeId=15) fix-during | Phase 3 "Behavior NOT delivered" | R5 |
| Phase 3 | L4 (XSS hole in FormatTriggerType) fix-during | Phase 3 "Behavior NOT delivered" | W5 |
| Phase 4 | ADMINISTRATE / `AllowSpecificGroupMemberAttributes` panel-visibility gate on Section 6 | Phase 4 C5 finding #1 | Misc-1 |
| Phase 4 | Add-path GroupType cascade refresh of `bag.attributes` | Phase 4 C5 finding #2 | Misc-2 |

### Re-deferred to a later phase

None planned at draft time; the user's review pass between Phase 4 and Phase 5 sessions may move items.

### Dropped (no longer in scope)

None planned at draft time.

## Open questions for spec lock

Each has a default recommendation; user confirms or overrides during the spec lock pass.

### Q5.1. Reorder column on the Member Workflow Triggers grid

**Background.** WebForms `gMemberWorkflowTriggers.GridReorder` supports drag-to-reorder. The design (Figma frame [research/design/screenshots/edit-section-10.png](../design/screenshots/edit-section-10.png)) shows NO reorder handle in the captured grid.

**Default recommendation:** preserve the WebForms parity (add Reorder column). The design's screenshot may be a default-state capture; the underlying behavior is meaningful (trigger order affects evaluation sequence). Confirm with the user.

### Q5.2. Sync Frequency restyle mechanism

**Background.** Q9 in 00-architecture.md confirmed reusing the existing `<IntervalPicker>` with a scoped styling tweak. The styling change must NOT affect other `<IntervalPicker>` consumers across Rock.

**Default recommendation:** add a `variant: "segmented" | "default"` prop on `<IntervalPicker>` that drives the new layout. Document the prop on the component; existing consumers opt-in. Alternative: a `verticalLayout: boolean` prop with a clearer name. Decide during spec lock.

### Q5.3. Phase 4 carry-forward — Section 6 ADMINISTRATE gate placement

**Background.** Two possible bag locations for the ADMINISTRATE flag the Vue side needs:

- (a) Add `CanAdministrate: boolean` to `GroupBag` (mirrors `CanEdit` / `CanAdministrate` patterns elsewhere). Reusable across multiple panels.
- (b) Add `IsMemberAttributesVisible` / `IsRequirementsVisible` / `IsSyncVisible` / `IsTriggersVisible` to `GroupTypeOptionsBag`. Per-panel granularity; piggybacks on the existing cascade.

**Default recommendation:** option (a). Authorization is a per-user fact about the entity, not a per-GroupType fact, so it belongs on the GroupBag rather than the GroupTypeOptionsBag. Phase 5 sub-features ALSO use ADMINISTRATE; one flag covers Sections 6, 7, 9 plus Section 6 visibility.

### Q5.4. Phase 4 carry-forward — Add-path cascade refresh of `bag.attributes`

**Background.** When the user picks a different GroupType in Add mode, the Group Attribute definitions should refresh to match the new GroupType's qualified attribute set. Phase 3's reactive `groupTypeId` watcher fetches `GroupTypeOptionsBag` but does not re-fetch `bag.attributes`.

**Default recommendation:** extend the reactive watcher to additionally call `Edit` block action on cascade (re-fetching the full bag for the entity, which would surface the new attribute set). Cost: one extra round-trip per cascade in Add mode. Alternative: extend `GroupTypeOptionsBag` to carry `GroupAttributes: Record<string, PublicAttributeBag>` for Add-mode cascade refresh; only Add mode needs this (Edit mode's GroupType is read-only).

### Q5.5. Group Requirement DueDate controls in the modal

**Background.** WebForms shows DatePicker OR DueDateGroupAttribute dropdown conditionally based on `GroupRequirementType.DueDateType`. The captured design (frame [edit-modal-02-requirement.png](../design/screenshots/edit-modal-02-requirement.png)) does NOT show these controls.

**Default recommendation:** preserve WebForms parity — DueDate controls render inside a conditional well in the modal when `DueDateType ∈ { ConfiguredDate, GroupAttribute }`. Captured screenshot is likely a default-state capture.

### Q5.6. Editable Requirements grid columns

**Background.** WebForms grid has 9 columns (Name / Group Role / Age Classification / Data View / Required For New Members / Can Expire / Type / Edit / Delete). Design ([edit-section-07.png](../design/screenshots/edit-section-07.png)) shows 6 (Type / Group Role / Age Classification / Required Before Adding / Edit / Delete). "Data View", "Can Expire", "RequirementCheckType" columns are dropped.

**Default recommendation:** match the design (drop the three columns from the grid). The dropped data is still surfaced inside the Add/Edit modal; the grid just doesn't show them at a glance.

## Research coverage

- [research/specs/00-architecture.md](00-architecture.md): always relevant; Q9 (Sync Frequency), Q12 (L3 + L4).
- [research/specs/04-phase-4-attributes.md](04-phase-4-attributes.md): the Phase 4 spec, especially the Self-review coverage report and Mid-phase decisions (carry-forward findings).
- [research/webforms/11-group-requirements.md](../webforms/11-group-requirements.md): full Group Requirements flow.
- [research/webforms/12-group-sync.md](../webforms/12-group-sync.md): full Group Sync flow.
- [research/webforms/13-member-workflow-triggers.md](../webforms/13-member-workflow-triggers.md): full Member Workflow Triggers flow.
- [research/webforms/22-grouptype-cascade.md](../webforms/22-grouptype-cascade.md): cascade behavior on GroupType change for the sub-feature panels.
- [research/webforms/23-validations-and-cascades.md](../webforms/23-validations-and-cascades.md): save-flow ordering (Phase 5 inserts steps 4c + 4d + 4e between Phase 4's step 4b and Phase 3's step 5), cache invalidation rules (Triggers → `RemoveCachedTriggers`).
- [research/design/00-overview.md](../design/00-overview.md): Section 7 / 9 / 10 layout in the figma.
- [research/design/02-edit-panel.md](../design/02-edit-panel.md): full edit-panel walkthrough.
- [research/design/03-net-new-features.md](../design/03-net-new-features.md): item #14 (Sync Frequency redesign), C4 (label renames), R6 (Workflow Triggers reorder dropped).
- [research/design/04-component-inventory.md](../design/04-component-inventory.md): `<Grid>` / `<Modal>` / `<DataViewPicker>` / `<WorkflowTypePicker>` / `<GroupRolePicker>` / `<IntervalPicker>`.
- Reference: [Rock.Blocks/Group/GroupTypeDetail.cs](../../Rock.Blocks/Group/GroupTypeDetail.cs) — `SyncRelatedEntities<TEntity>` helper pattern for state-list saves.

## Implementation checklist

### R. Section 7 — Group Requirements

R1. Render Section 7 with two stacks per design [edit-section-07.png](../design/screenshots/edit-section-07.png) — read-only "From Group Type" grid + editable "Specific Group Requirements" grid. Hide entire section unless ADMINISTRATE auth (Q5.3 lock) AND the visibility expression from `webforms/11-group-requirements.md` evaluates true.
R2. Read-only Grid columns: Name / Group Role. Header includes a linked GroupType anchor.
R3. Editable Grid columns: Type / Group Role / Age Classification / Required Before Adding / Edit / Delete (per Q5.6 lock; drops Data View, Can Expire, RequirementCheckType from WebForms).
R4. Modal: GroupRequirementType picker (auto-postback equivalent fires on selection), GroupRolePicker, Age Classification radio, DataView picker (per R5 fix), Allow Leader Override, Require Before Adding, Due Date conditional well (per Q5.5 lock).
R5. **L3 fix-during**: DataView picker uses `EntityType.Person` Guid via the `<DataViewPicker>` `entityTypeGuid` prop. Drop the WebForms `EntityTypeId=15` magic number.
R6. C# bag fields: `GroupRequirements: List<GroupRequirementBag>` (per-group, editable). The "From GroupType" list comes from `GroupTypeOptionsBag.GroupTypeRequirements` (added in this phase).
R7. Save body: `SyncRelatedEntities<GroupRequirement>` pattern; deferred-insert pattern for new requirements (need group.Id from first SaveChanges). Mirrors `webforms/23-validations-and-cascades.md` "GroupRequirements deferred-insert pattern".
R8. Modal save: duplicate detection on `(GroupRequirementType, GroupRoleId)` pair surfaces a `<NotificationBox alertType="warning">` inside the modal.

### S. Section 9 — Group Sync

S1. Render Section 9 with one editable Grid per design [edit-section-09.png](../design/screenshots/edit-section-09.png). Hide unless ADMINISTRATE AND (`GroupType.AllowGroupSync` OR any existing sync rows exist).
S2. Editable Grid columns: Role / Data View / Sync Interval / Last Sync / Edit / Delete.
S3. Modal: Sync Data View picker (`entityTypeGuid` = Person Guid, set in the picker prop, not hardcoded in markup), Group Role dropdown filtered to roles not yet synced, Sync Interval `<IntervalPicker>`, Welcome / Exit communication dropdowns, Create-login-during-sync checkbox.
S4. C# bag field: `GroupSyncs: List<GroupSyncBag>`.
S5. **Q9 Sync Frequency restyle**: `<IntervalPicker>` gets a scoped variant prop per Q5.2 lock; the new layout renders the unit toggle above the slider. Other consumers across Rock unchanged.
S6. Save body: `SyncRelatedEntities<GroupSync>` pattern.
S7. The same `SystemCommunication` dropdown source feeds both Sections 3 (RSVP) and 9 (Sync); reuse a single bag field or a single helper rather than duplicating.

### W. Section 10 — Group Member Workflow Triggers

W1. Render Section 10 with one editable Grid per design [edit-section-10.png](../design/screenshots/edit-section-10.png). Hide unless `GroupType.AllowSpecificGroupMemberWorkflows`.
W2. Editable Grid columns per Q5.1 lock: Trigger Name / Workflow / Trigger Event / Active / Edit / Delete. Reorder column added if Q5.1 confirms WebForms parity.
W3. Modal: Trigger Name text box, Workflow Type picker, Trigger Event dropdown (`MemberAddedToGroup` / `MemberRemovedFromGroup` / `MemberRoleChanged` / `MemberStatusChanged` / `MemberAttributeChanged`), Active checkbox, qualifier conditional well per `webforms/13-member-workflow-triggers.md` (different controls per event type).
W4. C# bag field: `GroupMemberWorkflowTriggers: List<GroupMemberWorkflowTriggerBag>`.
W5. **L4 fix-during**: the `TypeQualifier` 7-tuple serialization is internal-server-only; the Vue template surfaces user-controlled values (Workflow Type name, Trigger Event description) via Vue interpolation (auto-escapes). Any server-rendered HTML attribute or text uses `System.Web.HttpUtility.HtmlEncode` if it surfaces user-controlled values (prefer Vue-template handling).
W6. Save body: `SyncRelatedEntities<GroupMemberWorkflowTrigger>` pattern.
W7. Post-save: invoke `GroupMemberWorkflowTriggerService.RemoveCachedTriggers()` when any trigger was added / edited / deleted, per `webforms/23-validations-and-cascades.md` cache-invalidation rules.

### Misc — Phase 4 carry-forwards

Misc-1. Section 6 ADMINISTRATE / `AllowSpecificGroupMemberAttributes` gate. Add `bag.CanAdministrate` flag (per Q5.3 lock) populated in `GetCommonEntityBag` from `entity.IsAuthorized(Authorization.ADMINISTRATE, ...)`. Wrap the Section 6 `<ContentSection>` in `v-if="canAdministrate && (allowsCustom || hasAnyMemberAttributes)"`. One-line additions per side.
Misc-2. Add-path GroupType cascade refresh of `bag.attributes`. Implement per Q5.4 lock — extend the reactive watcher OR extend the cascade payload. Decide during spec lock.

### V. Vue file structure

V1. New `groupRequirements.partial.obs` (Section 7 — Grids + Modal + AttributeEditor-equivalent).
V2. New `groupSync.partial.obs` (Section 9).
V3. New `groupMemberWorkflowTriggers.partial.obs` (Section 10).
V4. `editPanel.partial.obs` (MODIFY): replace Sections 7 / 9 / 10 placeholders with the three new partials.
V5. New `groupRequirementModal.partial.obs`, `groupSyncModal.partial.obs`, `memberWorkflowTriggerModal.partial.obs` IF the bundled-into-parent approach (MP-4.2 pattern) creates files too large to navigate. Re-evaluate at implementation time per file size.

### S. Save block action

SS1. After Phase 4's step 4b (member attribute defs) and before step 5 (Inactive cascade), insert step 4c (Group Requirements sync).
SS2. After 4c, insert step 4d (Group Sync entities sync).
SS3. After 4d, insert step 4e (Group Member Workflow Triggers sync; track `triggersUpdated` flag for post-save cache invalidation).
SS4. The 12-step WrapTransaction ordering: (1) Add+SaveChanges, (2) UpdateEntityFromBox, (3) SaveChanges (assigns group.Id), (4) AllowPerson if Add+AdminToCreator, (4a) Group attribute values, (4b) Member attribute definitions, (4c) Group Requirements sync, (4d) Group Sync entities, (4e) Member Workflow Triggers sync, (5) Inactive cascade, (6) Chat-avatar IsTemporary, (7) Photo IsTemporary, (8) SaveChanges (+ inline-schedule delete if applicable).
SS5. Post-`WrapTransaction` cache invalidations: extend Phase 4's IsSecurityRole check with `if (triggersUpdated) { GroupMemberWorkflowTriggerService.RemoveCachedTriggers(); }`.

## Out-of-scope items

- Locations editing modal + Section 4 Stack 2 (Locations grid): **Phase 6**.
- Updating still-WebForms outbound destinations: **Phase 7**.
- Cutover and WebForms file deletion: **Phase 8**.

## Files to create / modify

### Rock.Blocks/Group/

- `GroupDetail.cs` (MODIFY) — bag-population helpers + `UpdateEntityFromBox` IfValidProperty blocks for the three new list fields + `WrapTransaction` steps 4c / 4d / 4e + cache-invalidation extension + L3 / L4 fixes.

### Rock.ViewModels/Blocks/Group/GroupDetail/

- `GroupBag.cs` (MODIFY) — add `GroupRequirements: List<GroupRequirementBag>`, `GroupSyncs: List<GroupSyncBag>`, `GroupMemberWorkflowTriggers: List<GroupMemberWorkflowTriggerBag>`, `CanAdministrate: bool` (per Q5.3).
- `GroupRequirementBag.cs` (NEW).
- `GroupSyncBag.cs` (NEW).
- `GroupMemberWorkflowTriggerBag.cs` (NEW).
- `GroupTypeOptionsBag.cs` (MODIFY) — add `GroupTypeRequirements: List<...>` (the read-only "From GroupType" list), `EnableSpecificGroupRequirements: bool`, `AllowGroupSync: bool`, `AllowSpecificGroupMemberWorkflows: bool`.

### Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/

- `groupBag.d.ts` (REGEN placeholder).
- `groupTypeOptionsBag.d.ts` (REGEN placeholder).
- `groupRequirementBag.d.ts` (NEW placeholder).
- `groupSyncBag.d.ts` (NEW placeholder).
- `groupMemberWorkflowTriggerBag.d.ts` (NEW placeholder).

### Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/

- `editPanel.partial.obs` (MODIFY) — replace Sections 7 / 9 / 10 placeholders with the three new partials. Also adds the Section 6 `v-if` gate per Misc-1.
- `groupRequirements.partial.obs` (NEW).
- `groupSync.partial.obs` (NEW).
- `groupMemberWorkflowTriggers.partial.obs` (NEW).
- `groupDetail.obs` (MODIFY) — extends reactive watcher per Misc-2 if Q5.4 picks the watcher approach.

### Rock.JavaScript.Obsidian/Framework/Controls/

- `intervalPicker.obs` (MODIFY) — Q9 / Q5.2 restyle: add `variant: "segmented" | "default"` prop (default = "default"); the new layout renders unit toggle above slider. Existing consumers opt-in.

## Bag fields contributed

```typescript
interface GroupBag {
    // (existing Phase 1-4 fields unchanged)

    canAdministrate: boolean;                                                      // per Q5.3 / Misc-1
    groupRequirements: GroupRequirementBag[];                                      // Section 7
    groupSyncs: GroupSyncBag[];                                                    // Section 9
    groupMemberWorkflowTriggers: GroupMemberWorkflowTriggerBag[];                  // Section 10
}

interface GroupTypeOptionsBag {
    // (existing Phase 3-4 fields unchanged)

    enableSpecificGroupRequirements: boolean;
    allowGroupSync: boolean;
    allowSpecificGroupMemberWorkflows: boolean;
    groupTypeRequirements: GroupRequirementBag[];                                  // read-only "From GroupType" list
}
```

## Block actions

| Action | Request | Returns | Notes |
|---|---|---|---|
| `Save` (extended) | `ValidPropertiesBox<GroupBag>` | Same as Phase 4 | Adds steps 4c / 4d / 4e + Trigger cache invalidation. |
| `Edit`, `GetGroupTypeOptions`, `Delete`, `Archive`, `ArchiveWithChildren`, `Copy` | unchanged | unchanged | The cascade payload `GroupTypeOptionsBag` extends with three new visibility flags + the GroupType requirements list. |

## Save action contributions

See checklist SS1-SS5 for the extended save flow. Cascades and cache invalidations:

- `SyncRelatedEntities<GroupRequirement>` — standard pattern, no per-entity invalidation.
- `SyncRelatedEntities<GroupSync>` — standard pattern.
- `SyncRelatedEntities<GroupMemberWorkflowTrigger>` — standard pattern + `RemoveCachedTriggers()` post-save if any trigger row changed.

## Code patterns to follow

- `SyncRelatedEntities<TEntity>` helper for state-list saves (see `GroupTypeDetail.cs` for the canonical pattern).
- Deferred-insert pattern for new-entity-needs-parent-Id (Group Requirements specifically — see `webforms/23-validations-and-cascades.md`).
- `<DataViewPicker>` for the L3 fix (avoids the hard-coded EntityTypeId).
- Vue interpolation for L4 (auto-escapes user-controlled values).
- `<IntervalPicker>` scoped variant prop (Q5.2 lock).
- Modal duplicate-detection: `<NotificationBox>` inside the modal body for inline error display.

## Design references

- Edit Section 7 Requirements: [research/design/screenshots/edit-section-07.png](../design/screenshots/edit-section-07.png).
- Edit Section 9 Sync: [research/design/screenshots/edit-section-09.png](../design/screenshots/edit-section-09.png).
- Edit Section 10 Triggers: [research/design/screenshots/edit-section-10.png](../design/screenshots/edit-section-10.png).
- Add Group Requirement modal: [research/design/screenshots/edit-modal-02-requirement.png](../design/screenshots/edit-modal-02-requirement.png).
- Add Group Sync Rule modal: [research/design/screenshots/edit-modal-03-sync.png](../design/screenshots/edit-modal-03-sync.png).
- Add Group Member Workflow modal: [research/design/screenshots/edit-modal-04-workflow.png](../design/screenshots/edit-modal-04-workflow.png).

## Mid-phase decisions log

(initially empty; populate during implementation per SESSION-PROTOCOL.md Section B4)

## Verification plan

Manual test scenarios for the user's review playbook (per SESSION-PROTOCOL.md Section D7):

1. Edit group with ADMINISTRATE auth: confirm Section 7 / 9 / 10 visible (subject to GroupType flags). Without ADMINISTRATE, confirm Sections 6 / 7 / 9 hidden; Section 10 visible only when `AllowSpecificGroupMemberWorkflows`.
2. Add a per-group requirement via the modal. Save. Re-enter edit mode; confirm the new requirement appears in the editable grid.
3. Edit a per-group requirement: change Allow Leader Override, Save. Confirm change persists.
4. Delete a per-group requirement. Save. Confirm row removed from the group's requirement pool.
5. Duplicate requirement: try to add a requirement with the same Type + Role as an existing one. Confirm the warning notification surfaces inside the modal and the entry is rejected.
6. Add a Group Sync rule with a Data View and a Role. Save. Confirm the row appears; the Sync Frequency renders per the new layout (Q9).
7. Edit a Group Sync: change Sync Frequency. Save. Confirm Last Sync stays unchanged (only writes on actual sync runs).
8. Delete a Group Sync. Save. Confirm removed.
9. Add a Member Workflow Trigger (`MemberAddedToGroup`). Pick Workflow Type, set Active. Save. Confirm the row appears; the trigger fires on the next member add (cache invalidation working).
10. Edit a Member Workflow Trigger. Save. Confirm cache invalidates (the post-save `RemoveCachedTriggers` ran).
11. Reorder triggers (if Q5.1 confirms Reorder): drag, Save. Confirm new order persists.
12. GroupType cascade: switch GroupType in Add mode. Confirm Sections 7 / 9 / 10 visibility re-evaluates. Confirm `bag.attributes` refreshes (Misc-2 working).
13. Section 6 gate verification: as a non-ADMINISTRATE user, confirm Section 6 is hidden (Misc-1 working).
14. L4 verification: create a workflow trigger with a Workflow Type whose name contains `<script>`. Confirm the grid renders the name escaped (no script execution).
15. Confirm Phase 1 / 2 / 3 / 4 surfaces unchanged: every prior verification scenario still works.

## Self-review coverage report

(initially empty; populated by the implementing model per SESSION-PROTOCOL.md Section C)

## Completed

(initially empty; populated by the implementing model per SESSION-PROTOCOL.md Section D)
