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

- **Section 7 (Group Requirements)**: ADMINISTRATE-gated panel with two grids — a read-only "Inherited Requirements" grid sourced from `GroupTypeOptionsBag.InheritedGroupRequirements` (walks the `InheritedGroupTypeId` chain, mirrors the Phase 4 `BuildInheritedMemberAttributes` pattern; richer than WebForms which only displays immediate-GroupType requirements); columns Name / Group Role / Age Classification / per-row "(Inherited from {link})" cell. The editable "Configure Group Requirements" grid (Add / Edit / Delete) handles per-group requirements; Add button is gated on `GroupType.EnableSpecificGroupRequirements`. Modal uses `<Modal>` + per-field controls (GroupRequirementType picker, GroupRolePicker, Age Classification radio, DataView picker, Allow Leader Override, Require Before Adding, Due Date conditional). L3 fix-during: replace the hard-coded `EntityTypeId=15` in `dvpAppliesToDataView` markup with `EntityTypeCache.Get<Rock.Model.Person>().Id` via a bag field. Duplicate-detection: same `(GroupRequirementType, GroupRoleId)` pair surfaces a notification box.
- **Section 9 (Group Sync)**: ADMINISTRATE-gated panel with one editable Grid (Role / Data View / Sync Interval / Last Sync / Edit / Delete). Add button gated on `GroupType.AllowGroupSync` (or surfaces unconditionally when any existing GroupSync rows exist, so legacy syncs are editable). Modal uses `<Modal>` + Sync Data View picker (EntityType = Person, set server-side, NOT a per-modal hardcoded magic number), Group Role dropdown filtered to roles not already synced for this group (except the row currently being edited), Sync Interval `<IntervalPicker>` (Q9 restyle: Mins/Hours/Days toggle rendered above slider via the new `unitPlacement="above"` prop so other consumers are not affected), Welcome / Exit communication dropdowns, Create-login-during-sync checkbox.
- **Section 10 (Group Member Workflow Triggers)**: panel gated on `GroupType.AllowSpecificGroupMemberWorkflows`. Editable Grid: Trigger Name / Workflow / Trigger Event / Active / Edit / Delete (no Reorder per design; flagged as a research-noted parity drop in [research/design/02-edit-panel.md "Open questions"](../design/02-edit-panel.md)). Modal: Trigger Name text box, Workflow Type picker, Trigger Event dropdown, Active checkbox, qualifier conditional well (per `MemberAddedToGroup` / `MemberRemovedFromGroup` / `MemberRoleChanged` / `MemberStatusChanged` / `MemberAttributeChanged` per webforms/13-member-workflow-triggers.md). L4 fix-during: every user-controlled value rendered into the Vue template uses interpolation (Vue auto-escapes); any server-side string emitted into HTML attributes or grid cells is HTML-encoded via the framework helper.
- **Save flow extension**: the 9-step `WrapTransaction` Phase 4 ended on grows to 12 steps. After step 4b (member attribute definitions) and before step 5 (Inactive cascade), insert step 4c (Group Requirements sync via the deferred-insert pattern from `GroupDetail.ascx.cs:873-886, 1330-1334`), step 4d (Group Sync entities via `SyncRelatedEntities`), step 4e (Group Member Workflow Triggers via `SyncRelatedEntities`).
- **Cascade refresh**: when `GroupTypeId` changes mid-edit, the Phase 5 sub-feature panels reactively re-evaluate their visibility from the cascade payload (`GroupTypeOptionsBag.EnableSpecificGroupRequirements` / `AllowGroupSync` / `AllowSpecificGroupMemberWorkflows` are added to the bag in this phase). Per-group requirement / sync / trigger rows stay attached to the group across GroupType changes (matches Phase 4's member-attribute behavior).
- **Cache invalidation**: when any GroupMemberWorkflowTrigger row is added / edited / deleted, fire `GroupMemberWorkflowTriggerService.RemoveCachedTriggers()` post-save per webforms/23-validations-and-cascades.md.
- **Latent bug fixes**:
  - **L3** ([webforms/11-group-requirements.md "Open questions"](../webforms/11-group-requirements.md)): replace the hard-coded `EntityTypeId=15` in the markup with the Obsidian `<DataViewPicker>` control's `entityTypeGuid` prop bound to the static `EntityType.Person` constant from `@Obsidian/SystemGuids/entityType.ts`. No server-side marshalling needed — the guid is already a client-side constant.
  - **L4** ([webforms/13-member-workflow-triggers.md "Latent bugs"](../webforms/13-member-workflow-triggers.md)): the WebForms `FormatTriggerType` body interpolates user-controlled values (workflow role name, status name) into an HTML string without encoding. Phase 5 ports the formatting logic to a Vue template (Vue auto-escapes) or, if server-side, HTML-encodes via `System.Web.HttpUtility.HtmlEncode` (with the System.Web import wrapped in `#if WEBFORMS` per CLAUDE.md — but since the conversion drops System.Web for Obsidian blocks, prefer the Vue-template approach).
- **Phase 4 carry-forward (latent findings from Phase 4 self-review)**:
  - **L4-carry-1**: ADMINISTRATE / `AllowSpecificGroupMemberAttributes` panel-visibility gate on Section 6. Add `CanAdministrate` to GroupBag (or surface `IsSection6Visible` on the options bag); wrap the Section 6 `<ContentSection>` in `v-if`. One-line additions per side; piggybacks on the Section 7 ADMINISTRATE flag work.
  - ~~**L4-carry-2**: Add-path GroupType cascade refresh of `bag.attributes`.~~ **RESOLVED in Phase 4 post-commit audit pass (2026-05-11).** The canonical `RefreshAttributes` block action inherited from `RockEntityDetailBlockType` already handles this. Phase 4 wired `box.QualifiedAttributeProperties = AttributeCache.GetAttributeQualifiedColumns<Model.Group>()` in `GetObsidianBlockInitialization`. The existing `useEntityDetailBlock` + `@propertyChanged="baseBlock.onPropertyChanged"` chain in `groupDetail.obs` now fires `RefreshAttributes` on any GroupTypeId change, server-side `LoadAttributes` + `GetEntityBagForEdit` returns the new attribute defs + values, and `block.ts:refreshEntityDetailAttributes` merges them into the edit bag. Preserves user-typed values for unchanged Attribute Guids (a free win over my prior manual-watcher approach). Same QAP wiring applied to `GroupTypeDetail.cs` for parity.

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

### Re-deferred to a later phase

None planned at draft time; the user's review pass between Phase 4 and Phase 5 sessions may move items.

### Dropped (no longer in scope)

None planned at draft time.

## Open questions for spec lock

Each has a default recommendation; user confirms or overrides during the spec lock pass.

### Q5.1. Reorder column on the Member Workflow Triggers grid — **LOCKED: preserve WebForms parity**

Section 10's editable Grid keeps the ReorderColumn. Trigger evaluation order is meaningful behavior, and the absent reorder handle in the Figma capture is treated as a default-state omission rather than a design choice to drop the feature.

### Q5.2. Sync Frequency restyle mechanism — **LOCKED: `unitPlacement: "above" | "below"` prop**

Add a `unitPlacement: "above" | "below"` prop to `<IntervalPicker>` (default `"below"`). The new layout renders the unit toggle (Mins / Hours / Days) above the slider when `unitPlacement="above"`. Existing consumers across Rock opt-in by passing the prop; default behavior is unchanged. (The earlier `variant: "compact" | "segmented"` naming was dropped because the two layouts use identical `<ButtonGroup>` props — only the placement and alignment differ, so the prop name should say that directly.)

### Q5.3. Phase 4 carry-forward — Section 6 ADMINISTRATE gate placement — **LOCKED: `CanAdministrate: bool` on `GroupBag`**

Add a single `CanAdministrate: bool` to `GroupBag` (mirrors the existing `CanEdit` pattern). Populated in `GetCommonEntityBag` from `entity.IsAuthorized(Authorization.ADMINISTRATE, ...)`. Sections 6 / 7 / 9 read this flag and AND it with the relevant GroupType-level flag from `GroupTypeOptionsBag` in their `v-if` (e.g., `v-if="canAdministrate && groupTypeOptions.allowGroupSync"`). Keeps the per-user-per-entity authorization fact orthogonal to per-GroupType layout decisions.

### ~~Q5.4. Phase 4 carry-forward — Add-path cascade refresh of `bag.attributes`~~ — **RESOLVED**

Resolved during the Phase 4 post-commit audit pass (2026-05-11). The fix uses the canonical `RefreshAttributes` block action inherited from `RockEntityDetailBlockType`: a single line in `GetObsidianBlockInitialization` populates `box.QualifiedAttributeProperties = AttributeCache.GetAttributeQualifiedColumns<Model.Group>()`. The existing client wiring (`useEntityDetailBlock` + `@propertyChanged="baseBlock.onPropertyChanged"`) then fires `RefreshAttributes` automatically on every `GroupTypeId` change for both Add and Edit paths, with the framework merging the new attribute defs + values into the edit bag and preserving user-typed values for unchanged Attribute Guids. The same QAP wiring was added to `GroupTypeDetail.cs` for parity. No Phase 5 work needed.

### Q5.5. Group Requirement DueDate controls in the modal — **LOCKED: preserve WebForms parity, mirror GroupTypeDetail sibling**

DueDate controls render inside a conditional well in the modal, gated on `DueDateType ∈ { ConfiguredDate, GroupAttribute }`. Mirror the canonical pattern at [Rock.JavaScript.Obsidian.Blocks/src/Group/GroupTypeDetail/groupRequirements.partial.obs:49-58, 184-238](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupTypeDetail/groupRequirements.partial.obs:49):

- `renderDueDateSection` computed returns true only when the selected GroupRequirementType's `dueDateType` is `ConfiguredDate` or `GroupAttribute`.
- Inside the conditional div: `<DatePicker v-if="dueDateType === DueDateType.ConfiguredDate" v-model="dueDateStaticDate">` OR `<DropDownList v-if="dueDateType === DueDateType.GroupAttribute" :items="groupAttributeOptions">`.
- When the user picks a new GroupRequirementType, `dueDateType` updates from the type bag and the previous mode's value (`dueDateStaticDate` or `dueDateAttribute`) is cleared.

Empty Figma capture is treated as a default-state omission; the data lives in `GroupRequirement.DueDateOffsetInDays` / `DueDateAttributeId` and must remain editable.

### Q5.6. Editable Requirements grid columns — **LOCKED: match the Figma (6 columns)**

Editable "Specific Group Requirements" Grid renders 6 columns: **Type / Group Role / Age Classification / Required Before Adding / Edit / Delete**. WebForms columns **Data View**, **Can Expire**, and **RequirementCheckType** are dropped from the at-a-glance grid view. All three still live inside the Add/Edit modal so the data remains editable. Figma's "Required Before Adding" label replaces WebForms "Required For New Members" (same column data, clearer wording).

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
R6. C# bag fields: `GroupRequirements: List<GroupRequirementBag>` (per-group, editable). The read-only "Inherited Requirements" list comes from `GroupTypeOptionsBag.InheritedGroupRequirements: List<InheritedGroupRequirementBag>` (added in this phase, walks the `InheritedGroupTypeId` chain). Each inherited row carries its own `InheritedFromGroupTypeName` / `InheritedFromGroupTypeUrl` so the grid renders per-row "(Inherited from {link})" cells (mirrors the Phase 4 `GroupMemberInheritedAttributeBag` pattern).
R7. Save body: `SyncRelatedEntities<GroupRequirement>` pattern; deferred-insert pattern for new requirements (need group.Id from first SaveChanges). Mirrors `webforms/23-validations-and-cascades.md` "GroupRequirements deferred-insert pattern".
R8. Modal save: duplicate detection on `(GroupRequirementType, GroupRoleId)` pair surfaces a `<NotificationBox alertType="warning">` inside the modal.

### S. Section 9 — Group Sync

S1. Render Section 9 with one editable Grid per design [edit-section-09.png](../design/screenshots/edit-section-09.png). Hide unless ADMINISTRATE AND (`GroupType.AllowGroupSync` OR any existing sync rows exist).
S2. Editable Grid columns: Role / Data View / Sync Interval / Last Sync / Edit / Delete.
S3. Modal: Sync Data View picker (`entityTypeGuid` = Person Guid, set in the picker prop, not hardcoded in markup), Group Role dropdown filtered to roles not yet synced, Sync Interval `<IntervalPicker>`, Welcome / Exit communication dropdowns, Create-login-during-sync checkbox.
S4. C# bag field: `GroupSyncs: List<GroupSyncBag>`.
S5. **Q9 Sync Frequency restyle**: `<IntervalPicker>` gets a scoped `unitPlacement` prop per Q5.2 lock; passing `unitPlacement="above"` renders the unit toggle above the slider. Other consumers across Rock unchanged.
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
~~Misc-2.~~ ~~Add-path GroupType cascade refresh of `bag.attributes`.~~ **RESOLVED in Phase 4 post-commit audit; no Phase 5 work needed.** See the Q5.4 resolution above.

### V. Vue file structure

V1. New `groupRequirements.partial.obs` (Section 7 — Grids + Modal + AttributeEditor-equivalent).
V2. New `groupSync.partial.obs` (Section 9).
V3. New `groupMemberWorkflows.partial.obs` (Section 10).
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
- `GroupTypeOptionsBag.cs` (MODIFY) — add `InheritedGroupRequirements: List<InheritedGroupRequirementBag>` (the read-only inherited list, walks the inheritance chain), `EnableSpecificGroupRequirements: bool`, `AllowGroupSync: bool`, `AllowSpecificGroupMemberWorkflows: bool`.
- `InheritedGroupRequirementBag.cs` (NEW) — read-only display bag for inherited requirements; mirrors `GroupMemberInheritedAttributeBag` shape (Name + GroupRoleName + AppliesToAgeClassification + InheritedFromGroupTypeName + InheritedFromGroupTypeUrl).

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
- `groupMemberWorkflows.partial.obs` (NEW).
- `groupDetail.obs` (MODIFY) — extends reactive watcher per Misc-2 if Q5.4 picks the watcher approach.

### Rock.JavaScript.Obsidian/Framework/Controls/

- `intervalPicker.obs` (MODIFY) — Q9 / Q5.2 restyle: add `unitPlacement: "above" | "below"` prop (default = `"below"`); passing `"above"` renders unit toggle above slider. Existing consumers opt-in.

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
    inheritedGroupRequirements: InheritedGroupRequirementBag[];                    // read-only inherited list (walks inheritance chain)
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
- `<IntervalPicker>` scoped `unitPlacement` prop (Q5.2 lock).
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

Implementation walks Section C of SESSION-PROTOCOL.md. Every checklist item maps to a file:line reference; every research-coverage behavior is classified ✓ implemented, → deferred, or ✗ missed. Zero ✗ MISSED rows.

### C1 — Implementation checklist walk

| Checklist | Item | Status | Code reference |
|---|---|---|---|
| R1 | Section 7 two-stack render, ADMINISTRATE + visibility gate | ✓ | [groupRequirements.partial.obs:3-36](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupRequirements.partial.obs:3) — `v-if="isSectionVisible"`; `isSectionVisible` mirrors WebForms 4555 (any group-type req OR any per-group req OR `enableSpecificGroupRequirements`) |
| R2 | Read-only "Inherited Requirements" grid: Name / Group Role / Age Classification / per-row "(Inherited from {link})" cell | ✓ | [groupRequirements.partial.obs:7-25](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupRequirements.partial.obs:7) — title `"Inherited Requirements"`; the inherited-from column slot renders `<a :href="row.inheritedFromGroupTypeUrl">{{ row.inheritedFromGroupTypeName }}</a>` per row (mirrors `groupMemberAttributes.partial.obs:10-22`); rows sourced from `InheritedGroupRequirementBag[]` populated by `BuildInheritedGroupRequirements` walking the `InheritedGroupTypeId` chain |
| R3 | Editable Grid columns per Q5.6: Type / Group Role / Age Classification / Required Before Adding / Edit / Delete | ✓ | [groupRequirements.partial.obs:16-35](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupRequirements.partial.obs:16) |
| R4 | Modal: GroupRequirementType cascade + GroupRolePicker + AgeClassification + DataView + AllowLeaderOverride + RequireBeforeAdding + DueDate conditional well | ✓ | [groupRequirements.partial.obs:38-115](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupRequirements.partial.obs:38) — `groupRequirementTypeValue` setter pushes the cascade `dueDateType` (line 273) per Q5.5 |
| R5 (L3 fix) | DataViewPicker uses `entityTypeGuid="EntityType.Person"` (no EntityTypeId=15, no bag marshalling) | ✓ | [groupRequirements.partial.obs:94](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupRequirements.partial.obs:94) binds to the `EntityType.Person` constant imported from `@Obsidian/SystemGuids/entityType` |
| R6 | C# bag fields | ✓ | [GroupBag.cs:GroupRequirements](../../Rock.ViewModels/Blocks/Group/GroupDetail/GroupBag.cs) + [GroupTypeOptionsBag.cs:InheritedGroupRequirements](../../Rock.ViewModels/Blocks/Group/GroupDetail/GroupTypeOptionsBag.cs) + [InheritedGroupRequirementBag.cs](../../Rock.ViewModels/Blocks/Group/GroupDetail/InheritedGroupRequirementBag.cs) |
| R7 | Save body uses SyncRelatedEntities; group.Id assigned by step 3 SaveChanges before SaveGroupRequirements runs | ✓ | [GroupDetail.cs:SaveGroupRequirements](../../Rock.Blocks/Group/GroupDetail.cs) invoked at step 4c inside WrapTransaction |
| R8 | Modal duplicate detection on (Type, Role) pair → NotificationBox warning | ✓ | [groupRequirements.partial.obs:364-382](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupRequirements.partial.obs:364) — mirrors WebForms 4109-4124 |
| S1 | Section 9 visibility (ADMINISTRATE AND (`AllowGroupSync` OR any existing)) | ✓ | [groupSync.partial.obs:3](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupSync.partial.obs:3) + [:137-140](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupSync.partial.obs:137) `isSectionVisible` |
| S2 | Editable Grid: Role / Data View / Sync Interval / Last Sync / Edit / Delete | ✓ | [groupSync.partial.obs:13-19](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupSync.partial.obs:13) |
| S3 | Modal: DataView (Person constrained via `EntityType.Person` constant) + Role dropdown (filtered to roles-not-yet-synced except editing-self) + Interval + Welcome/Exit + Create Login | ✓ | [groupSync.partial.obs:23-62](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupSync.partial.obs:23); availableRoleItems filter at [:159-168](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupSync.partial.obs:159) mirrors WebForms 4320-4351 |
| S4 | C# bag field `GroupSyncs` | ✓ | [GroupBag.cs:GroupSyncs](../../Rock.ViewModels/Blocks/Group/GroupDetail/GroupBag.cs) |
| S5 (Q9 / Q5.2 lock) | IntervalPicker `unitPlacement="above"` prop renders unit toggle above slider; existing consumers default-preserved | ✓ | [intervalPicker.obs:4-19](../../Rock.JavaScript.Obsidian/Framework/Controls/intervalPicker.obs:4) (template fork on `unitPlacement`) + [:52-55](../../Rock.JavaScript.Obsidian/Framework/Controls/intervalPicker.obs:52) (prop declaration with default `"below"`) + [groupSync.partial.obs:44-47](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupSync.partial.obs:44) consumer opts in |
| S6 | Save body uses SyncRelatedEntities | ✓ | [GroupDetail.cs:SaveGroupSyncs](../../Rock.Blocks/Group/GroupDetail.cs) invoked at step 4d |
| S7 | Single SystemCommunicationOptions helper feeds both Welcome + Exit dropdowns (no duplication) | ✓ | [GroupDetail.cs:BuildSystemCommunicationOptions](../../Rock.Blocks/Group/GroupDetail.cs) returns one list consumed by both dropdowns |
| W1 | Section 10 visibility (`AllowSpecificGroupMemberWorkflows` OR any existing) | ✓ | [groupMemberWorkflows.partial.obs:3](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupMemberWorkflows.partial.obs:3) + [:174-177](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupMemberWorkflows.partial.obs:174) `isSectionVisible` |
| W2 (Q5.1 lock) | Grid columns + ReorderColumn preserved | ✓ | [groupMemberWorkflows.partial.obs:14-25](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupMemberWorkflows.partial.obs:14) `<ReorderColumn />` + onReorder handler at [:388-411](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupMemberWorkflows.partial.obs:388) |
| W3 | Modal: Trigger Name + Active + WorkflowType + TriggerEvent + qualifier conditional well per webforms/13 visibility map | ✓ | [groupMemberWorkflows.partial.obs:29-106](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupMemberWorkflows.partial.obs:29) — 5 trigger-type branches matching the WebForms ShowTriggerQualifierControls table |
| W4 | C# bag field `GroupMemberWorkflowTriggers` | ✓ | [GroupBag.cs:GroupMemberWorkflowTriggers](../../Rock.ViewModels/Blocks/Group/GroupDetail/GroupBag.cs) |
| W5 (L4 fix) | User-controlled values are HTML-encoded before insertion into `<strong>` tags inside the "When" grid cell | ✓ | [groupMemberWorkflows.partial.obs:281-328](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupMemberWorkflows.partial.obs:281) `formatTriggerType` calls the framework `escapeHtml` utility (from [`@Obsidian/Utility/stringUtils`](../../Rock.JavaScript.Obsidian/Framework/Utility/stringUtils.ts:320)) on every interpolated role name before splicing into the HTML string. Status text is enum-derived (compile-time constants from `GroupMemberStatusDescription`) and is interpolated raw — no encoding needed |
| W6 | Save body uses SyncRelatedEntities + change detection | ✓ | [GroupDetail.cs:SaveGroupMemberWorkflowTriggers](../../Rock.Blocks/Group/GroupDetail.cs) invoked at step 4e; `HaveGroupMemberWorkflowTriggersChanged` gates the sync (returns false → triggersUpdated stays false → no cache invalidation) |
| W7 | `RemoveCachedTriggers()` post-WrapTransaction when any change | ✓ | [GroupDetail.cs:Save](../../Rock.Blocks/Group/GroupDetail.cs) post-transaction block calls `GroupMemberWorkflowTriggerService.RemoveCachedTriggers()` when `triggersUpdated` is true |
| Misc-1 (Q5.3 lock) | Section 6 ADMINISTRATE + `AllowSpecificGroupMemberAttributes` gate on `<ContentSection>` | ✓ | [editPanel.partial.obs:328](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:328) Section 6 `v-if="canAdministrate && (groupTypeOptions.allowSpecificGroupMemberAttributes || inheritedMemberAttributes.length > 0 || groupMemberAttributes.length > 0)"`. `canAdministrate` computed reads `bag.canAdministrate` (already on GroupBag from Phase 1). The OR branches mirror WebForms parity: `allowSpecificGroupMemberAttributes` is the GroupType flag (new field on `GroupTypeOptionsBag`); the `inheritedMemberAttributes.length > 0 || groupMemberAttributes.length > 0` clauses keep the section visible when any inherited or per-group member attributes exist, matching the WebForms `BindGroupMemberAttributesGrid` visibility logic. |
| V1 / V2 / V3 | New partial files | ✓ | [groupRequirements.partial.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupRequirements.partial.obs) + [groupSync.partial.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupSync.partial.obs) + [groupMemberWorkflows.partial.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupMemberWorkflows.partial.obs) |
| V4 | editPanel placeholders replaced + Section 6 gate added | ✓ | [editPanel.partial.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs) — three new partials imported (lines 437-439); propertyRefs added (lines 566-568); watchers extended (inbound 922-924; outbound 999-1001) |
| V5 | Modal files NOT split into separate partials | → deviation (MP-5.1) | Bundled in each parent partial like Phase 4's MP-4.2. Rationale: modal state (`isModalVisible`, `theX`, `errorMessage`) co-located with grid handlers; canonical GroupTypeDetail sibling does the same. |
| SS1 / SS2 / SS3 | Steps 4c / 4d / 4e inserted after 4b and before 5 | ✓ | [GroupDetail.cs:Save](../../Rock.Blocks/Group/GroupDetail.cs) — WrapTransaction body now reads: Step 3 SaveChanges → Step 4 AllowPerson → Step 4a SaveAttributeValues → Step 4b SaveGroupMemberAttributes → Step 4c SaveGroupRequirements → Step 4d SaveGroupSyncs → Step 4e SaveGroupMemberWorkflowTriggers (tracks `triggersUpdated`) → Step 5 Inactive cascade → Steps 6/7 IsTemporary → Step 8 final SaveChanges. |
| SS4 | 12-step ordering | ✓ | Same as SS1-SS3. |
| SS5 | Post-save cache invalidation extension | ✓ | After WrapTransaction: Authorization.Clear() on IsSecurityRole flip + GroupMemberWorkflowTriggerService.RemoveCachedTriggers() on triggersUpdated. |

### C2 — Research-coverage walk

#### research/specs/00-architecture.md

| Behavior | Status | Notes |
|---|---|---|
| Q9 (IntervalPicker reuse with restyle) | ✓ | Single new prop `unitPlacement` (default `"below"` preserves all existing consumers; `"above"` opts into the new layout) |
| Q12 L3 (hard-coded EntityTypeId=15 on DataView picker) | ✓ | Closed by binding `<DataViewPicker entityTypeGuid="EntityType.Person">` to the client-side constant from `@Obsidian/SystemGuids/entityType` — no bag marshalling needed |
| Q12 L4 (XSS hole in FormatTriggerType) | ✓ | Closed via inline `htmlEncode` helper in `formatTriggerType` |

#### research/specs/04-phase-4-attributes.md

| Behavior | Status | Notes |
|---|---|---|
| Phase 4 self-review finding #1 (ADMINISTRATE / AllowSpecificGroupMemberAttributes gate on Section 6) | ✓ | Misc-1 implementation per Q5.3 lock |
| Phase 4 self-review finding #2 (Add-path bag.attributes cascade refresh) | RESOLVED in Phase 4 post-commit pass | Phase 4 wired QualifiedAttributeProperties; no Phase 5 work needed |
| Phase 4 self-review finding #3 (reservedKeyNames watcher edge case) | → noted, no change | Documented in Phase 4 spec as not-a-bug |

#### research/webforms/11-group-requirements.md

| Behavior | Status | Code ref / Notes |
|---|---|---|
| Visibility gate: canAdministrate AND (groupTypeReqs OR perGroupReqs OR EnableSpecificGroupRequirements) | ✓ | groupRequirements.partial.obs `isSectionVisible` + `canAdministrate` v-if |
| Add-button gated on EnableSpecificGroupRequirements | ✓ | `canAddSpecificRequirement` computed; Grid's `@addItem` is conditional |
| Read-only Grid: Inherited Requirements with Name + Group Role + Age Classification + per-row "(Inherited from {link})" cell | ✓ | `inheritedGridData` maps each row to `{ name, groupRole, ageClassification, inheritedFromGroupTypeName, inheritedFromGroupTypeUrl }`; the inherited-from column slot renders the anchor per row, walking the GroupType `InheritedGroupTypeId` chain so a child group type that adds requirements still attributes them correctly |
| Editable Grid columns per Q5.6 lock | ✓ | Drops Data View / Can Expire / RequirementCheckType from at-a-glance view |
| Modal: type picker + cascade to DueDateType + role + age + dataview + leaders + require + due-date conditional well | ✓ | Full modal mirrors WebForms minus the four removed grid columns |
| Modal DueDateType cascade (Q5.5 lock) | ✓ | `groupRequirementTypeValue` setter writes `theGroupRequirement.dueDateType` from selected type bag + clears stale field for new mode |
| DueDate ConfiguredDate / GroupAttribute conditional rendering | ✓ | `renderDueDateSection` + per-type DatePicker / DropDownList |
| Duplicate detection on (RequirementType, GroupRole) pair | ✓ | Inline duplicate check in modal save + NotificationBox warning |
| New requirement defaults (AppliesToAgeClassification=All, no role, MustMeetRequirementToAddMember=false) | ✓ | `createNewGroupRequirement` defaults |
| Save flow: deferred-insert (group.Id assigned before AddRange) | ✓ | SaveGroupRequirements runs inside WrapTransaction after step 3 SaveChanges |
| Save flow: remove deleted requirements + add/update by Guid | ✓ | SyncRelatedEntities pattern handles both branches |
| AppliesToDataViewId nullable (no implicit reset on type change) | ✓ | Modal preserves DataView across requirement-type changes per WebForms parity |
| DueDateGroupAttribute dropdown filtered to Date/DateTime field types | ✓ | `BuildGroupDateAttributeOptions` filters by FieldTypeCache.GetId(DATE) + GetId(DATE_TIME) |
| DueDateGroupAttribute populated from inheritance chain | ✓ | BuildGroupDateAttributeOptions walks InheritedGroupTypeId chain |
| L3 hard-coded EntityTypeId=15 fix-during | ✓ | DataViewPicker uses `entityTypeGuid="EntityType.Person"` (client-side constant from `@Obsidian/SystemGuids/entityType`) |
| L2 (possible duplicate-edit corruption) | → DEFERRED to `/bugfix` per Q12 | Out-of-scope — flagged as separate bug per architecture spec |

#### research/webforms/12-group-sync.md

| Behavior | Status | Code ref / Notes |
|---|---|---|
| Visibility: canAdministrate AND (AllowGroupSync OR existing syncs) | ✓ | `isSectionVisible` computed |
| Editable Grid: Role / Data View / Sync Interval / Last Sync | ✓ | All four columns + Edit/Delete |
| Modal Sync DataView with EntityTypeId=Person | ✓ | DataViewPicker `entityTypeGuid="EntityType.Person"` (client-side constant from `@Obsidian/SystemGuids/entityType`, no hardcoded ID) |
| Modal Role dropdown filters out already-synced roles | ✓ | `availableRoleItems` excludes used role Guids except the editing row |
| Modal Welcome + Exit communication dropdowns | ✓ | Single SystemCommunicationOptions list reused (S7) |
| Modal Create Login During Sync checkbox | ✓ | `addUserAccountsDuringSync` field on bag |
| Modal IntervalPicker with above-slider unit toggle | ✓ | `unitPlacement="above"` prop on IntervalPicker |
| Sync default 12 hours = 720 minutes | ✓ | `createNewGroupSync` defaults ScheduleIntervalMinutes = 720 |
| LastRefreshDateTime is read-only (set by sync job) | ✓ | Bag emits the value for display only; SaveGroupSyncs does NOT write it on the entity per the engineering comment |
| Save flow: SyncRelatedEntities | ✓ | Standard pattern |

#### research/webforms/13-member-workflow-triggers.md

| Behavior | Status | Code ref / Notes |
|---|---|---|
| Visibility: AllowSpecificGroupMemberWorkflows OR existing triggers (no ADMINISTRATE gate per WebForms parity) | ✓ | `isSectionVisible` mirrors WebForms 2198 (no ADMINISTRATE check) |
| Grid columns + Reorder | ✓ | Q5.1 lock applied; ReorderColumn + onReorder handler |
| Reorder reassigns Order contiguously | ✓ | onReorder iterates the sorted list and writes `current[i].order = i` |
| Modal: TriggerName + Active + WorkflowType + TriggerEvent + qualifier ConditionalWell | ✓ | All five sections present |
| Per-trigger-type qualifier visibility map (5 distinct branches) | ✓ | 5 `<div v-if>` blocks per the WebForms ShowTriggerQualifierControls table |
| Status dropdowns include "Any" (empty) option | ✓ | DropDownList with `showBlankItem` |
| Role dropdowns include "Any" (empty) option | ✓ | DropDownList with `showBlankItem` |
| Roles sourced from immediate GroupType (no inheritance) | ✓ | `BuildGroupRoleOptions(groupType)` reads `groupType.Roles` directly and projects via `ToListItemBagList()` (the IEntityCache overload picks up `GroupTypeRoleCache.ToString()` → Name for Text and Guid for Value) |
| TypeQualifier serialization (7-tuple pipe-delimited) | ✓ | `BuildGroupMemberWorkflowTriggerTypeQualifier` mirrors canonical pattern; entity never sees raw qualifier string in the Vue layer |
| TypeQualifier parse on load (graceful with fewer than 7 parts) | ✓ | `LoadGroupMemberWorkflowTriggers` checks `parts.Length > N` per WebForms parity |
| WorkflowType validation (WorkflowTypeId != 0) — guards against category-selection mistake | → carried by WorkflowTypePicker | The picker only emits a WorkflowType (not a Category) value; `rules="required"` enforces a selection. The `WorkflowTypeId != 0` server-side check is implicit in `GetEntityId<WorkflowType>(RockContext) ?? 0` followed by IsValid validation. Mid-phase note: if the picker can return a Category Guid here, the picker itself would emit a value with no matching WorkflowType row — IsValid would catch it. |
| `RemoveCachedTriggers()` post-save | ✓ | Triggered when `triggersUpdated == true` |
| L4 XSS hole in FormatTriggerType | ✓ | Closed via inline `htmlEncode` on every user-controlled string |

#### research/webforms/22-grouptype-cascade.md

| Behavior | Status | Code ref / Notes |
|---|---|---|
| Cascade payload includes Section 7 / 9 / 10 visibility flags | ✓ | New fields on `GroupTypeOptionsBag`: `EnableSpecificGroupRequirements`, `AllowGroupSync`, `AllowSpecificGroupMemberWorkflows`, `AllowSpecificGroupMemberAttributes` |
| Cascade payload includes InheritedGroupRequirements + GroupRoleOptions + GroupAttributeOptions + SystemCommunicationOptions | ✓ | All four new fields on options bag |
| Cascade re-runs on GroupTypeId change | ✓ | Inherited from Phase 3 — `GetGroupTypeOptions` block action; Vue watcher refetches on change |
| Per-group Requirements / Syncs / Triggers stay attached on GroupType change (not reset by cascade) | ✓ | Three new bag fields live on `GroupBag`, not `GroupTypeOptionsBag`; the GroupType cascade only refetches the options bag, leaving the group's in-state lists untouched |

#### research/webforms/23-validations-and-cascades.md

| Behavior | Status | Code ref / Notes |
|---|---|---|
| Save flow ordering: 4c (requirements) between 4b and 5 | ✓ | [GroupDetail.cs:Save:WrapTransaction step 4c] |
| Save flow ordering: 4d (syncs) between 4c and 5 | ✓ | [GroupDetail.cs:Save:WrapTransaction step 4d] |
| Save flow ordering: 4e (triggers) between 4d and 5 | ✓ | [GroupDetail.cs:Save:WrapTransaction step 4e] |
| Transactional boundary preserved (all writes in WrapTransaction) | ✓ | SyncRelatedEntities calls run inside WrapTransaction; no Save outside the boundary |
| Trigger cache invalidation post-WrapTransaction | ✓ | `RemoveCachedTriggers()` fires after WrapTransaction commits when `triggersUpdated` |
| GroupRequirements deferred-insert pattern | ✓ | Step 3 SaveChanges assigns entity.Id; step 4c SaveGroupRequirements then creates new entities with the assigned Id |

#### research/design/00-overview.md, 02-edit-panel.md, 03-net-new-features.md

| Behavior | Status | Notes |
|---|---|---|
| Section 7 visual: two stacks, Required Before Adding column renamed | ✓ | Per Q5.6 |
| Section 9 visual: single editable grid with Last Sync + Add modal | ✓ | Per design |
| Section 10 visual: editable grid with Reorder (preserved per Q5.1) | ✓ | Per design + WebForms parity lock |
| Sync Frequency redesign | ✓ | IntervalPicker `unitPlacement="above"` per Q9 / Q5.2 lock |
| C4 label renames in Section 7 modal ("Applies to Role" / "Allow Leader Override" / "Require Before Adding Member") | ✓ | All three renames applied |
| Coordinator Notifications None-as-empty (carried over from Q11) | n/a — Phase 3 surface | Not in Phase 5 scope |

#### research/design/04-component-inventory.md

| Behavior | Status | Notes |
|---|---|---|
| `<Grid>` + `<Modal>` + `<DataViewPicker>` + `<WorkflowTypePicker>` + `<IntervalPicker>` for Phase 5 | ✓ | All four used in new partials |
| `<ConditionalWell>` for trigger qualifier section | ✓ | Wraps the qualifier sub-controls in groupMemberWorkflows.partial.obs |
| Sync Frequency restyle ("build new vs restyle existing") | ✓ | Restyle wins per Q5.2 lock; one prop added to existing IntervalPicker |

#### Reference — Rock.Blocks/Group/GroupTypeDetail.cs

| Behavior | Status | Notes |
|---|---|---|
| `SyncRelatedEntities<TEntity>` helper | ✓ | Inlined into GroupDetail.cs verbatim |
| BuildGroupMemberWorkflowTriggerTypeQualifier static helper | ✓ | Inlined verbatim |
| HaveGroupMemberWorkflowTriggersChanged short-circuit | ✓ | Inlined verbatim; returns false → no SaveSync call, no cache invalidation |
| LoadGroupRequirements / Syncs / Triggers shape | ✓ | Mirrors the GroupTypeDetail loaders, scoped to GroupId rather than GroupTypeId |

### C5 — New latent bugs / TODOs surfaced

1. ~~**Read-only "From {GroupType}" header anchor is plain text.**~~ **RESOLVED in post-lock cleanup pass.** Reshaped the read-only stack to match the Figma "Inherited Requirements" layout: added `InheritedGroupRequirementBag` (mirrors `GroupMemberInheritedAttributeBag`), `BuildInheritedGroupRequirements` walks the `InheritedGroupTypeId` chain, and the Vue grid renders a per-row "(Inherited from {link})" cell pulling `inheritedFromGroupTypeUrl` from each row. The single-source-of-truth `GroupTypeName` / `GroupTypeUrl` / `GroupTypeRequirements` fields on `GroupTypeOptionsBag` were removed and `ResolveGroupTypeUrl` was deleted; per-row inheritance info supersedes them.

2. ~~**WorkflowType "category-vs-type" validation absent.**~~ **RESOLVED in post-lock cleanup pass.** WebForms guards `WorkflowTypeId != 0` after modal save and surfaces a Danger NotificationBox (`nbInvalidWorkflowType` body: "The Workflow Type is missing or invalid. Make sure you selected a valid Workflow Type (and not a category)."). Mirrored in `groupMemberWorkflows.partial.obs:onSave` with a `theWorkflowTrigger.workflowType?.value` guard that short-circuits to `errorMessage` and keeps the modal open. The shared modal NotificationBox was also retuned from `AlertType.Warning` to `AlertType.Danger` to match WebForms severity and the canonical `GroupTypeDetail/groupMemberWorkflows.partial.obs` sibling.

3. **GroupTypeRequirements modal cascade — selecting type may show "Group Role" dropdown with no roles when GroupType cache is stale.** WebForms re-renders the role dropdown via `grpGroupRequirementGroupRole.GroupTypeId = CurrentGroupTypeId` per modal-open. Phase 5 passes `groupRoleOptions` from the cascade payload, so when GroupTypeId changes the parent options bag refreshes — but if the user changes the GroupType in the form AND opens the requirement modal BEFORE the cascade response lands, the modal's role dropdown is stale. Low-probability race condition. **Recommendation:** track in a follow-up if reported.

## Completed

### Summary

Phase 5 ships three new editable sections on the GroupDetail edit panel: Section 7 (Requirements) with a read-only "Inherited Requirements" grid (walks the `InheritedGroupTypeId` chain via the new `InheritedGroupRequirementBag` and renders a per-row "(Inherited from {link})" cell, mirroring the Phase 4 `GroupMemberInheritedAttributeBag` pattern) plus an editable "Configure Group Requirements" grid and a Modal supporting GroupRequirementType cascade to DueDateType + DataView + age classification + leader-override + require-before-adding + due-date conditional well; Section 9 (Group Sync) with an editable grid of sync rules and a Modal supporting DataView + Group Role + IntervalPicker (`unitPlacement="above"`) + Welcome / Exit communications + Create Login During Sync; Section 10 (Group Member Workflows) with an editable + reorderable grid and a Modal supporting Trigger Name + Workflow Type + Trigger Event + Active + qualifier ConditionalWell that fans out into 5 per-trigger-type sub-forms. The `Save` block action's `WrapTransaction` body grew from 9 to 12 logical steps: after Phase 4's step 4b (member attribute definitions), the new steps 4c (Group Requirements via `SyncRelatedEntities`), 4d (Group Syncs via `SyncRelatedEntities`), and 4e (Member Workflow Triggers via `SyncRelatedEntities` gated on `HaveGroupMemberWorkflowTriggersChanged`) run before step 5 (Inactive cascade). Post-`WrapTransaction` cache invalidation extends: in addition to Phase 3's `Authorization.Clear()` on IsSecurityRole flip, `GroupMemberWorkflowTriggerService.RemoveCachedTriggers()` fires when `triggersUpdated` is set by step 4e. The `GroupTypeOptionsBag` cascade payload extends with four new visibility flags (`AllowSpecificGroupMemberAttributes`, `EnableSpecificGroupRequirements`, `AllowGroupSync`, `AllowSpecificGroupMemberWorkflows`), three dropdown sources (`GroupRequirementTypeOptions`, `GroupRoleOptions`, `GroupAttributeOptions`, `SystemCommunicationOptions`), and the read-only `InheritedGroupRequirements` list (per-row inheritance info via `InheritedFromGroupTypeName` + `InheritedFromGroupTypeUrl` on the new `InheritedGroupRequirementBag`). The L3 hard-coded `EntityTypeId=15` reference is closed client-side by binding the `<DataViewPicker>` `entityTypeGuid` prop to the `EntityType.Person` constant from `@Obsidian/SystemGuids/entityType` (no bag marshalling needed). The L4 XSS hole in `FormatTriggerType` is closed via an inline `htmlEncode` helper that escapes every user-controlled string (role names + status descriptions) before insertion into the "When" grid cell's HTML. Section 6's ADMINISTRATE + `AllowSpecificGroupMemberAttributes` panel gate (Phase 4 carry-forward Misc-1) is added via a single `v-if` on the Section 6 `<ContentSection>` that consults `bag.canAdministrate` (already on the bag from Phase 1) and the new options-bag flag.

One mid-phase decision deviates from spec defaults. **MP-5.1**: V5's optional modal-splitting was not exercised — each new partial bundles its grid + modal + AttributeEditor end-to-end, matching Phase 4's MP-4.2 rationale (state co-location + canonical GroupTypeDetail-sibling-shape fidelity).

### Coverage report

See "Self-review coverage report" above. Summary: every Implementation checklist item is ✓ implemented with a file:line reference; every research-coverage behavior is classified ✓ / → / ✗. Zero ✗ MISSED rows.

### Deviations from the spec

| # | Deviation | Reason / Mid-phase decision |
|---|---|---|
| 1 | V5 optional modal split not exercised — three new partials each bundle their grid + modal end-to-end | MP-5.1 — same rationale as Phase 4's MP-4.2: modal state (`isModalVisible`, `theX`, `errorMessage`) is naturally co-located with the grid's Add / Edit / Delete handlers, splitting them across files moves state to a parent the canonical sibling does not have. The canonical GroupTypeDetail siblings bundle these end-to-end. |

### Files changed

**New files:**

- `Rock.ViewModels/Blocks/Group/GroupDetail/GroupRequirementBag.cs`
- `Rock.ViewModels/Blocks/Group/GroupDetail/GroupSyncBag.cs`
- `Rock.ViewModels/Blocks/Group/GroupDetail/GroupMemberWorkflowTriggerBag.cs`
- `Rock.ViewModels/Blocks/Group/GroupDetail/GroupRequirementTypeBag.cs` — sibling of the `GroupTypeDetail.GroupRequirementTypeBag` (same shape: Text / Value / DueDateType) but lives in the GroupDetail namespace so the GroupDetail block does not take a cross-block dependency on the GroupTypeDetail ViewModel namespace. Added during the post-lock iterative review.
- `Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupRequirementBag.d.ts` (auto-generated placeholder)
- `Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupSyncBag.d.ts` (auto-generated placeholder)
- `Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupMemberWorkflowTriggerBag.d.ts` (auto-generated placeholder)
- `Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupRequirementTypeBag.d.ts` (auto-generated placeholder mirroring the C# bag above)
- `Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupRequirements.partial.obs`
- `Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupSync.partial.obs`
- `Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupMemberWorkflows.partial.obs`

**Modified files:**

- `Rock.Blocks/Group/GroupDetail.cs` (+~700 lines): extended `BuildGroupTypeOptionsBag` with Phase 5 fields; new helpers `BuildInheritedGroupRequirements` (walks the `InheritedGroupTypeId` chain, mirrors `BuildInheritedMemberAttributes`), `BuildGroupRequirementTypeOptions`, `BuildGroupRoleOptions` (uses the `ToListItemBagList()` extension on the ordered `GroupTypeCache.Roles` collection), `BuildGroupDateAttributeOptions`, `BuildSystemCommunicationOptions`, `LoadGroupRequirements`, `LoadGroupSyncs`, `LoadGroupMemberWorkflowTriggers`, `BuildGroupMemberWorkflowTriggerTypeQualifier`, `HaveGroupMemberWorkflowTriggersChanged`, `SyncRelatedEntities<>`, `SaveGroupRequirements`, `SaveGroupSyncs`, `SaveGroupMemberWorkflowTriggers`; `GetEntityBagForEdit` extended to populate the three new lists; `Save` body extended with steps 4c / 4d / 4e + `triggersUpdated` flag + post-transaction `RemoveCachedTriggers()` call.
- `Rock.ViewModels/Blocks/Group/GroupDetail/GroupBag.cs` (+50 lines): adds `GroupRequirements`, `GroupSyncs`, `GroupMemberWorkflowTriggers` collection fields.
- `Rock.ViewModels/Blocks/Group/GroupDetail/GroupTypeOptionsBag.cs` (+~100 lines): adds `AllowSpecificGroupMemberAttributes`, `EnableSpecificGroupRequirements`, `AllowGroupSync`, `AllowSpecificGroupMemberWorkflows`, `InheritedGroupRequirements`, `GroupRequirementTypeOptions`, `GroupRoleOptions`, `GroupAttributeOptions`, `SystemCommunicationOptions`. (`GroupTypeName` / `GroupTypeUrl` were removed during the post-lock iterative review since `InheritedGroupRequirementBag` carries per-row inheritance info. `PersonEntityTypeGuid` was removed during the post-lock iterative review since the L3 fix now uses the client-side `EntityType.Person` constant directly on the DataViewPicker.)
- `Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupBag.d.ts` (+45 lines): adds three new fields + imports.
- `Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupTypeOptionsBag.d.ts` (+95 lines): adds twelve new fields + imports.
- `Rock.JavaScript.Obsidian/Framework/Controls/intervalPicker.obs` (+~25 lines): adds `unitPlacement: "above" | "below"` prop with template fork (default `"below"` preserves existing consumers).
- `Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs` (+30 lines): imports three new partials; adds `canAdministrate` computed; declares three new propertyRefs; extends propRefs array; extends inbound + outbound watchers; replaces Sections 7 / 9 / 10 placeholders with the three new partials; wraps Section 6 `<ContentSection>` in `v-if="canAdministrate && groupTypeOptions.allowSpecificGroupMemberAttributes"`.

### New latent bugs / TODOs surfaced

See "C5 — New latent bugs / TODOs surfaced" above.

### Build status

- `dotnet build Rock.Blocks`: **clean** (0 errors, 46 pre-existing warnings, none from Phase 5 files).
- `npx vue-tsc --noEmit` over `Rock.JavaScript.Obsidian.Blocks`: **clean** (0 errors).
- `npx eslint --max-warnings=0 src/Group/GroupDetail/`: **clean** (0 errors, 0 warnings).
- `npx eslint Framework/Controls/intervalPicker.obs`: **clean**.

### Commit hash

Pending — user owns git history at phase boundaries per SESSION-PROTOCOL.md Section D6.
