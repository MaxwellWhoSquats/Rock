---
author: Maxwell Eley
date_created: 2026-05-08
summary: >-
  Phase 4 implementation spec for the GroupDetail Obsidian conversion. Adds
  the Group Attribute Values editor (Section 5) and the Group Member
  Attribute Definitions editor (Section 6) to the edit panel, including the
  inherited member attributes display sourced from the GroupType cascade
  payload Phase 3 already returns. Phase 3's Save flow extends with the two
  attribute-save steps (between AllowPerson and Inactive cascade) per the
  WebForms WrapTransaction ordering.
contributors: []
---

# Phase 4: Attributes (Group + Member definitions)

## Context

Phase 3 ([03-phase-3-edit-core.md](03-phase-3-edit-core.md)) shipped the edit panel core — every scalar field on `Group` round-trips, the GroupType cascade reactively reshapes the panel, the Save block action wraps an 8-step `WrapTransaction`, and the photo / chat-channel-avatar uploaders use the IsTemporary BinaryFile pattern. The edit panel renders `<NotificationBox>` "Coming in Phase 4" placeholders for Section 5 (Group Attribute Values) and Section 6 (Group Member Attribute Definitions). Phase 4 replaces both placeholders with working editors and extends the Save flow with the two corresponding save steps. Sub-feature panels for Requirements / Sync / Triggers stay deferred to Phase 5.

Architectural decisions for this phase are governed by [00-architecture.md](00-architecture.md), specifically Q2 (the `GroupTypeOptionsBag.InheritedMemberAttributes` field already populated by Phase 3 surfaces inherited member attributes for Section 6) and Q10 (`<ContentSection>` / `<ContentStack>` reuse).

## Behavior delivered

- **Section 5 (Group Attribute Values)**: visible only in edit mode, only when the chosen GroupType has Group-targeted attribute definitions, and only when the user has EDIT auth on at least one. Renders `<AttributeValuesContainer isEditMode>` per the canonical Obsidian pattern. Excludes attribute keys the user lacks EDIT auth on (mirrors WebForms `excludeForEdit`). Categories surface as ContentStack headers per design item #17 (re-deferred from Phase 1).
- **Section 6 (Group Member Attribute Definitions)**: read-only display of inherited member attributes (sourced from `GroupTypeOptionsBag.InheritedMemberAttributes`) plus an edit grid for per-group member attribute definitions. Inherited attributes are NOT editable here (they're owned by the GroupType). Per-group definitions follow the standard `<Grid>` pattern with Add / Edit / Delete / Reorder columns.
- **Member Attribute modal**: Add / Edit modal for per-group member attribute definitions. Reuses `<AttributeEditor>` core control. Validates duplicate keys, reserved key names. The save body persists the attribute via `Rock.Attribute.Helper.SaveAttributeEdits` with `entityTypeId = GroupMember.TypeId`, `qualifierColumn = "GroupId"`, `qualifierValue = group.Id`.
- **Save flow extension**: the 8-step `WrapTransaction` body Phase 3 implemented now grows to 9 + 10. After Phase 3's step 4 (AllowPerson if Add+AdminToCreator) and before step 5 (Inactive cascade), Phase 4 inserts: (4a) `entity.SaveAttributeValues(RockContext)` for Group attribute values; (4b) per-group member attribute definitions sync (delete removed, upsert kept) per the WebForms parity at `GroupDetail.ascx.cs:1338-1357`.
- **Cascade refresh**: when `GroupTypeId` changes mid-edit, the inherited member attributes refresh (already in `GroupTypeOptionsBag.InheritedMemberAttributes` from Phase 3), and the Group attribute values editor re-renders. Per-group member attribute definitions stay attached to the group (not the group type) and do NOT refresh on GroupType change.

## Behavior NOT delivered

- Group Requirements panel + modal (Section 7): **Phase 5**.
- Group Sync settings panel + modal (Section 9), including Sync Frequency `<IntervalPicker>` restyle per Q9: **Phase 5**.
- Group Member Workflow Triggers panel + modal (Section 10), including L4 (XSS hole in `FormatTriggerType`): **Phase 5**.
- L3 (hard-coded `EntityTypeId=15` in `mdGroupRequirement` markup): **Phase 5**.
- Locations editing (Section 4 Stack 2): **Phase 6**.
- Update dependencies (5 still-WebForms outbound destinations): **Phase 7**.
- Cutover and WebForms file deletion: **Phase 8**.

## Deferred behaviors inherited from prior phases

Walks Phase 1, Phase 2, and Phase 3 coverage reports for `→ DEFERRED to Phase 4` rows.

### Inherited (covered in this phase's Implementation checklist)

| Source phase | Behavior | Coverage-report origin | Checklist item that handles it |
|---|---|---|---|
| Phase 1 | #17 Group attribute categories surfaced as stack headers | `design/03-net-new-features.md` row in Phase 1 coverage report | A1 (categorized stacks) |
| Phase 3 | Group Attribute Values editor (Section 5) | "Behavior NOT delivered" in Phase 3 spec | A1-A4 |
| Phase 3 | Group Member Attribute Definitions (Section 6) | "Behavior NOT delivered" in Phase 3 spec | M1-M5 |
| Phase 3 | Save flow attribute-value step (between AllowPerson and Inactive cascade) | Phase 3 spec's Q3.5 lock + Phase 3 coverage report S10 step ordering | S1-S2 |

### Re-deferred to a later phase

None.

### Dropped (no longer in scope)

None.

## Locked decisions (from spec-lock pass)

Each open question is now resolved.

### Q4.1. Section 5 categorization rendering — **LOCKED: one stack per category**

Render one `<ContentStack>` per attribute Category, with the category name as the stack title. Attributes without a category render in a default "General" stack at the top. Inside each stack, use `<AttributeValuesContainer>` filtered to that category. Empty categories collapse. Realizes Phase 1's deferred design item #17.

### Q4.2. Section 6 inherited-vs-custom layout — **LOCKED: two Grids, GroupTypeDetail pattern**

Section 6 mirrors the canonical pattern at [Rock.JavaScript.Obsidian.Blocks/src/Group/GroupTypeDetail/groupMemberAttributes.partial.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupTypeDetail/groupMemberAttributes.partial.obs) — two `<ContentStack>` blocks each wrapping a `<Grid>`:

- **Stack 1: "Inherited Attributes"** — read-only `<Grid>` with `light :isFooterHidden="true"` styling, columns Name (15%) / Description (60%) / Inherited From (25%, with linked GroupType anchor when `inheritedFromGroupTypeUrl` is set). Header description matches the GroupTypeDetail wording adapted for GroupDetail context. Hidden when `inheritedGridData.rows.length === 0`.
- **Stack 2: "Configure Group Member Attributes"** — editable `<Grid>` with `light` styling, columns Reorder / Name / Description / Required (BooleanColumn) / Security / Edit / Delete. Add toolbar at top (uses `entityTypeGuid="EntityType.Attribute"` + `@addItem` handler). Reorder reorders client-side; Save persists via the new save step.

**Implementation detail surfaced during the lock**: Phase 3's `GroupTypeOptionsBag.InheritedMemberAttributes` field is currently typed `List<PublicAttributeBag>`, which lacks the `inheritedFromGroupTypeName` / `inheritedFromGroupTypeUrl` fields the Grid's third column needs. Phase 4 must either (a) widen this field to `GroupTypeInheritedAttributeBag[]` (the canonical GroupTypeDetail bag) — possibly by relocating that bag to a shared `Rock.ViewModels.Blocks.Group` namespace so GroupDetail can reuse it without a cross-block import — or (b) define a parallel bag in the GroupDetail namespace. Decision deferred to Phase 4 implementation; the relocation option is preferred for cross-block reuse.

### Q4.3. Custom member attribute save shape — **LOCKED: split between EntityBagBase and new field**

Two distinct attribute concerns ride on different bag locations:

- **Section 5 (Group Attribute Values)**: `GroupBag` already inherits `Attributes: Record<string, PublicAttributeBag>` and `AttributeValues: Record<string, string>` from `EntityBagBase`. **Reuse those**; do NOT add new bag fields. Phase 4's `GetEntityBagForView` and `GetEntityBagForEdit` call `LoadAttributesAndValuesForPublicView` / `LoadAttributesAndValuesForPublicEdit` on the entity (the latter already runs in `GetEntityBagForEdit` via the canonical detail-block hook). Save is handled by the framework's standard attribute round-trip — no custom save body needed for Section 5 beyond the standard `entity.SaveAttributeValues(RockContext)` call inside the WrapTransaction.
- **Section 6 (Custom Member Attribute Definitions)**: add a new bag field `GroupMemberAttributes: PublicEditableAttributeBag[]` on `GroupBag` (matches GroupTypeDetail's `groupMemberAttributes` pattern at [groupTypeDetailBag.cs](../../Rock.ViewModels/Blocks/Group/GroupTypeDetail/groupTypeDetailBag.cs)). Save body uses `Rock.Attribute.Helper.SaveAttributeEdits` per attribute with `entityTypeId = GroupMember.TypeId`, `qualifierColumn = "GroupId"`, `qualifierValue = group.Id` (matches WebForms `GroupDetail.ascx.cs:1354-1357`). Delete removed attributes via `AttributeService.Delete`.

### Q4.4. Add/Edit modal vs inline editor — **LOCKED: Modal + AttributeEditor**

Reuse the standard `<Modal>` + `<AttributeEditor>` pattern from [groupMemberAttributes.partial.obs:43-46](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupTypeDetail/groupMemberAttributes.partial.obs:43). The grid's Add button opens an empty editor (`createNewAttribute()` helper); the Edit column opens populated with a deep-copied bag so cancel discards changes. Modal save merges into the local `groupMemberAttributes` array; persistence is deferred to the block-level Save action.

## Research coverage

- [research/specs/00-architecture.md](00-architecture.md): always relevant; Q2 (cascade payload includes inherited member attributes), Q10 (Sections & Stacks core components).
- [research/specs/03-phase-3-edit-core.md](03-phase-3-edit-core.md): the Phase 3 spec, especially the Self-review coverage report and Mid-phase decisions (notably MP-3.3 about the EditModeResponseBag composite that Phase 4 will extend).
- [research/webforms/09-group-attributes.md](../webforms/09-group-attributes.md): full Group Attribute Values flow (visibility gates, AddEditControls, GetEditValues, SaveAttributeValues). The most load-bearing research file for Phase 4.
- [research/webforms/10-member-attributes.md](../webforms/10-member-attributes.md): Group Member Attribute Definitions flow (state list, modal Add/Edit, save sync, qualifier column = "GroupId", qualifier value = group.Id).
- [research/webforms/22-grouptype-cascade.md](../webforms/22-grouptype-cascade.md): cascade behavior on GroupType change for the attribute editors.
- [research/webforms/23-validations-and-cascades.md](../webforms/23-validations-and-cascades.md): the save-flow ordering (Phase 4 inserts steps 4a + 4b between Phase 3's steps 4 and 5).
- [research/design/00-overview.md](../design/00-overview.md): Section 5 and Section 6 layout in the figma.
- [research/design/02-edit-panel.md](../design/02-edit-panel.md): full edit panel walkthrough including Sections 5 + 6.
- [research/design/03-net-new-features.md](../design/03-net-new-features.md): item #17 (categories as stack headers).
- [research/design/04-component-inventory.md](../design/04-component-inventory.md): `<AttributeValuesContainer>`, `<AttributeEditor>`, `<Grid>`, `<Modal>` core controls.
- Reference: [Rock.Blocks/Group/GroupTypeDetail.cs](../../Rock.Blocks/Group/GroupTypeDetail.cs) — closest sibling block; shows the pattern for `LoadAttributesAndValuesForPublicEdit`, `SaveAttributes` invocation, member-attribute-definition sync via `SyncRelatedEntities`.

## Implementation checklist

### A. Section 5 — Group Attribute Values

A1. Render Section 5 with categorized `<ContentStack>` groups per Q4.1. Each stack contains `<AttributeValuesContainer isEditMode :modelValue="attributeValues" :attributes="attributes" :numberOfColumns="2">` filtered to that category. Hide entire section when `bag.attributes` is null/empty OR the user lacks EDIT on every attribute.
A2. C# `GetEntityBagForEdit` calls `bag.LoadAttributesAndValuesForPublicEdit(entity, RequestContext.CurrentPerson, enforceSecurity: true)` (per `GroupTypeDetail.cs:423`). This populates `bag.Attributes` (PublicAttributeBag dictionary) and `bag.AttributeValues` (string dictionary).
A3. `UpdateEntityFromBox` sets `entity.AttributeValues = box.Bag.AttributeValues` via `IfValidProperty(nameof(box.Bag.AttributeValues), ...)`.
A4. Save body inside `WrapTransaction`: insert `entity.SaveAttributeValues(RockContext)` between Phase 3's step 4 (AllowPerson) and step 5 (Inactive cascade). Mirrors `GroupDetail.ascx.cs:1336`.

### M. Section 6 — Group Member Attribute Definitions

M1. Render two stacks per Q4.2: "Inherited Attributes" (read-only display) sourced from `groupTypeOptions.inheritedMemberAttributes`; "Custom Member Attributes" (`<Grid>` with reorder + edit + delete + add).
M2. C# `GetEntityBagForEdit` populates `bag.GroupMemberAttributes` (List<PublicEditableAttributeBag>) by querying `AttributeService.GetByEntityTypeQualifier(GroupMember.TypeId, "GroupId", entity.Id, true)` (mirrors `GroupDetail.ascx.cs:2117-2123`).
M3. Modal Add/Edit using `<Modal>` + `<AttributeEditor>` (same pattern as `GroupTypeDetail.cs` member-attribute editor).
M4. `UpdateEntityFromBox` sets `bag.GroupMemberAttributes` aside for the Save body to consume after `SaveChanges` (Group.Id required for the qualifier value).
M5. Save body inside `WrapTransaction`, immediately after step 4a (group attribute values save):
   - Get existing attributes for this entity-type-qualifier: `attributeService.GetByEntityTypeQualifier(GroupMember.TypeId, "GroupId", group.Id.ToString(), true)`.
   - Delete attributes the user removed: those whose Guid is NOT in `bag.GroupMemberAttributes`.
   - For each attribute in `bag.GroupMemberAttributes`: `Rock.Attribute.Helper.SaveAttributeEdits(state, GroupMember.TypeId, "GroupId", group.Id.ToString(), RockContext)`.
   - Mirrors `GroupDetail.ascx.cs:1338-1357`.

### V. Vue file structure

V1. `editPanel.partial.obs` (modify; replaces the Section 5 / 6 placeholders): inline categorized `<AttributeValuesContainer>` stacks for Section 5, inline `<Grid>` + `<Modal>` for Section 6. No new partial file (per Q3.1 lock — Phase 3 chose single-file for the orchestrating shell).
V2. New `groupMemberAttributeModal.partial.obs` (NEW) — Add/Edit modal for per-group member attribute definitions. Wraps `<AttributeEditor>` with the standard validate-then-emit pattern.

### S. Save block action — incremental additions

S1. After Phase 3's step 4 (AllowPerson) and before step 5 (Inactive cascade), insert step 4a: `entity.SaveAttributeValues(RockContext)`.
S2. After step 4a, insert step 4b: per-group member attribute definitions sync per checklist M5.
S3. The 9-step WrapTransaction ordering: (1) Add+SaveChanges, (2) UpdateEntityFromBox, (3) SaveChanges (assigns group.Id), (4) AllowPerson if Add+AdminToCreator, (4a) Group attribute values, (4b) Member attribute definitions, (5) Inactive cascade, (6) Chat-avatar IsTemporary toggle, (7) Photo IsTemporary toggle, (8) SaveChanges.

## Out-of-scope items

- Group Requirements editing (Section 7): **Phase 5**.
- Group Sync settings (Section 9): **Phase 5**.
- Group Member Workflow Triggers (Section 10): **Phase 5**.
- L3 (hard-coded `EntityTypeId=15`): **Phase 5**.
- L4 (XSS hole in `FormatTriggerType`): **Phase 5**.
- Locations editing modal: **Phase 6**.
- Updating still-WebForms outbound destinations: **Phase 7**.
- Cutover: **Phase 8**.

## Files to create / modify

### Rock.Blocks/Group/

- `GroupDetail.cs` (MODIFY) — extend `GetEntityBagForEdit` with `LoadAttributesAndValuesForPublicEdit` + `bag.GroupMemberAttributes` population; extend `UpdateEntityFromBox` with attribute-value assignment; extend `Save` body with steps 4a + 4b inside `WrapTransaction`.

### Rock.ViewModels/Blocks/Group/GroupDetail/

- `GroupBag.cs` (MODIFY) — add `GroupMemberAttributes: List<PublicEditableAttributeBag>` field. (`Attributes` and `AttributeValues` are already on `EntityBagBase`.)

### Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/

- `groupBag.d.ts` (REGEN — placeholder) — picks up `groupMemberAttributes`.

### Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/

- `editPanel.partial.obs` (MODIFY) — replace Section 5 + Section 6 placeholders with the categorized AttributeValuesContainer + member attribute grid implementations.
- `groupMemberAttributeModal.partial.obs` (NEW) — Add/Edit modal for per-group member attribute definitions.

### Rock.Migrations/Migrations/

- No new migrations.

## Bag fields contributed

```typescript
interface GroupBag {
    // (existing Phase 1 + Phase 2 + Phase 3 fields unchanged)

    // Section 6 (custom Member Attribute definitions)
    groupMemberAttributes: PublicEditableAttributeBag[];
}
```

`bag.attributes` (Group attribute definitions for the current GroupType) and `bag.attributeValues` (Group's stored values) are already on `EntityBagBase` and don't need bag-shape additions; Phase 3's `GetEntityBagForEdit` simply needs to call `LoadAttributesAndValuesForPublicEdit` to populate them.

## Block actions

| Action | Request | Returns | Notes |
|---|---|---|---|
| `Save` (extended) | `ValidPropertiesBox<GroupBag>` | Same as Phase 3 | Adds attribute-value save (4a) and member-attribute-definition sync (4b) inside `WrapTransaction`. |
| `Edit`, `GetGroupTypeOptions`, `Delete`, `Archive`, `ArchiveWithChildren`, `Copy` | unchanged | unchanged | The cascade payload `GroupTypeOptionsBag.InheritedMemberAttributes` is already populated by Phase 3. |

## Save action contributions

See checklist S1-S3 for the extended save flow. Cascades and cache invalidations:

- `entity.SaveAttributeValues` — handled by `SaveAttributeValues` itself; no manual invalidation.
- Per-group member attribute definitions — `Rock.Attribute.Helper.SaveAttributeEdits` invalidates `AttributeCache` for the affected attributes; no manual invalidation.

## Code patterns to follow

- `LoadAttributesAndValuesForPublicEdit` for Group attribute values (per `GroupTypeDetail.cs:423`).
- `Rock.Attribute.Helper.SaveAttributeEdits` for per-group member attribute definitions (per `GroupDetail.ascx.cs:1354-1357`).
- `<AttributeValuesContainer isEditMode>` for the Group attributes editor (sibling pattern from `SiteDetail/editPanel.partial.obs:301`).
- `<Grid>` + `<EditColumn>` + `<DeleteColumn>` + `<ReorderColumn>` for the member-attribute grid.
- `<Modal>` + `<AttributeEditor>` for the Add/Edit modal (sibling pattern from `GroupTypeDetail.cs` member-attribute editor).
- Reuse existing `GroupTypeOptionsBag.InheritedMemberAttributes` field that Phase 3 already populates — Phase 4 just renders it.

## Design references

- Edit panel canvas: `4670-25350` ("Add/Edit Mode") in `N60VRdhtRtjO9EA9nba9fB` Figma.
- Section 5 Group Attribute Values: [research/design/screenshots/edit-section-05.png](../design/screenshots/edit-section-05.png).
- Section 6 Group Member Attribute Definitions: [research/design/screenshots/edit-section-06.png](../design/screenshots/edit-section-06.png).

## Mid-phase decisions log

(initially empty; populate during implementation per SESSION-PROTOCOL.md Section B4)

## Verification plan

Manual test scenarios for the user's review playbook (per SESSION-PROTOCOL.md Section D7):

1. Edit existing group with Group Attribute Values: confirm Section 5 renders the attribute editors grouped by category (default category = "General"). Edit a value, Save. Refresh page; confirm value persisted via `entity.GetAttributeValue("Key")`.
2. Edit group whose user lacks EDIT on a specific attribute: confirm that attribute is hidden. Edit the visible attributes and Save. Confirm the excluded attribute's value is unchanged.
3. Edit existing group with Group Member Attribute Definitions: confirm Section 6 Stack 1 lists inherited attributes (from the GroupType) read-only, Stack 2 lists per-group attributes. Add a new per-group member attribute via the modal. Save. Re-enter edit mode; confirm the new attribute appears.
4. Edit group's per-group member attribute via the modal. Save. Confirm the change persists.
5. Delete a per-group member attribute via the grid Delete column. Save. Confirm the attribute is removed from the group's attribute pool.
6. Reorder per-group member attributes via the Reorder column. Save. Confirm the new order persists.
7. Change GroupType mid-edit on an Add: confirm Section 5 attribute editors re-populate from the new GroupType's attributes; Section 6 inherited list refreshes from the new GroupType's inherited attributes; per-group attributes in Stack 2 stay (they're attached to the group, not the type).
8. Confirm Phase 3 surfaces unchanged: every Phase 3 verification scenario still works (1-20 from Phase 3 spec).

## Self-review coverage report

(initially empty; populated by the implementing model per SESSION-PROTOCOL.md Section C)

## Completed

(initially empty; populated by the implementing model per SESSION-PROTOCOL.md Section D)
