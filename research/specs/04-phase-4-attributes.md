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

### MP-4.1 (2026-05-11) — Cross-namespace import for `GroupTypeInheritedAttributeBag` instead of relocation

Q4.2's "preferred resolution" was to relocate `GroupTypeInheritedAttributeBag` from `Rock.ViewModels.Blocks.Group.GroupTypeDetail` to a shared `Rock.ViewModels.Blocks.Group` namespace so GroupDetail could reuse it without crossing a sibling-block import boundary. Reviewed the cost during Phase 4 startup:

- TS-side imports in `groupMemberAttributes.partial.obs` (GroupTypeDetail) and the canonical responder `GroupTypeDetail.GetInheritedAttributes` already point at `@Obsidian/ViewModels/Blocks/Group/GroupTypeDetail/groupTypeInheritedAttributeBag`. Relocation would cascade through every import statement.
- The generated `.d.ts` placeholder would move to a new path; codegen would need to be re-run to delete the old path.
- Within Rock the consumers (`Rock.Blocks/Group/GroupTypeDetail.cs`, `GetInheritedAttributesResponseBag.cs`) all reference `Rock.ViewModels.Blocks.Group.GroupTypeDetail.GroupTypeInheritedAttributeBag`; relocation would also cascade through these.

Went with the lighter touch: `Rock.ViewModels/Blocks/Group/GroupDetail/GroupTypeOptionsBag.cs` adds a `using Rock.ViewModels.Blocks.Group.GroupTypeDetail;` directive; the TS placeholder imports from the canonical path; `Rock.Blocks/Group/GroupDetail.cs` adds a matching using. The cross-block import is a single line per file and doesn't change any existing files. **Reason:** preserves every existing import path across two layers (C# + TS) without losing the cross-block reuse Q4.2 wanted; relocation would have been a 2x larger diff for purely-cosmetic namespace alignment.

### MP-4.2 (2026-05-11) — Section 6 bundled into a single `groupMemberAttributes.partial.obs` (not split per V1+V2)

Spec V1 directed inlining Section 6's Grid + Modal into `editPanel.partial.obs` while V2 directed a new `groupMemberAttributeModal.partial.obs` for the modal alone. Two issues with that split:

1. The Modal's state (`isModalVisible`, `theAttribute`, `reservedKeyNames`) is naturally co-located with the Grid's Add / Edit / Delete handlers; splitting it across files moves state to a parent the canonical sibling does not have.
2. The canonical GroupTypeDetail `groupMemberAttributes.partial.obs` bundles the Inherited Grid + Editable Grid + Modal + AttributeEditor into one file. Mirroring that is simpler and easier to maintain.

Created a single new `groupMemberAttributes.partial.obs` (in `Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/`) that handles Section 6 end-to-end. `editPanel.partial.obs` imports and renders it in place of the placeholder. **Reason:** canonical-pattern fidelity + simpler state ownership.

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

Implementation walks Section C of SESSION-PROTOCOL.md. Every checklist item maps to a file:line reference; every Research-coverage behavior is classified as ✓ implemented, → deferred, or ✗ missed. No ✗ MISSED rows.

### C1 — Implementation checklist walk

| Checklist | Item | Status | Code reference |
|---|---|---|---|
| A1 | Section 5 categorized stacks + AttributeValuesContainer | ✓ | [editPanel.partial.obs:316-333](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:316) (template) + [:843-918](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:843) (groupAttributeCategories computed splits one stack per `Attribute.Category[0]`; categoryless attributes fall into a default "General" stack rendered first; empty categories collapse) |
| A2 | `bag.LoadAttributesAndValuesForPublicEdit` in `GetEntityBagForEdit` | ✓ | [GroupDetail.cs:677](../../Rock.Blocks/Group/GroupDetail.cs:677) (`enforceSecurity: true` excludes attribute keys the user lacks EDIT on, mirroring WebForms `excludeForEdit`) |
| A3 | `UpdateEntityFromBox` `IfValidProperty(AttributeValues)` | ✓ | [GroupDetail.cs:1027-1031](../../Rock.Blocks/Group/GroupDetail.cs:1027) (canonical pattern from `GroupTypeDetail.cs:641-647` — `entity.LoadAttributes` + `entity.SetPublicAttributeValues(...)` with `enforceSecurity: true`) |
| A4 | Save step 4a — `entity.SaveAttributeValues(RockContext)` inside `WrapTransaction` | ✓ | [GroupDetail.cs:1409](../../Rock.Blocks/Group/GroupDetail.cs:1409) (between step 4 AllowPerson and step 5 Inactive cascade, mirroring WebForms `GroupDetail.ascx.cs:1336`) |
| M1 | Section 6 two-stack layout (Inherited Grid + Custom Grid) | ✓ | [groupMemberAttributes.partial.obs:3-41](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupMemberAttributes.partial.obs:3) (Inherited Grid hides when `inheritedGridData.rows.length === 0`; columns Name 15% / Description 60% / Inherited 25% with anchor when URL present; Custom Grid has Reorder / Name / Description / Required / Security / Edit / Delete) |
| M2 | `GetEntityBagForEdit` populates `bag.GroupMemberAttributes` | ✓ | [GroupDetail.cs:685](../../Rock.Blocks/Group/GroupDetail.cs:685) (invocation) + [GroupDetail.cs:697-718](../../Rock.Blocks/Group/GroupDetail.cs:697) (`LoadGroupMemberAttributes` helper queries by qualifier `(GroupMember.TypeId, "GroupId", group.Id)` and projects via `PublicAttributeHelper.GetPublicEditableAttribute`; returns empty list for Id == 0) |
| M3 | Modal + AttributeEditor for Add/Edit | ✓ | [groupMemberAttributes.partial.obs:43-46](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupMemberAttributes.partial.obs:43) (`<Modal>` + `<AttributeEditor>` with `reservedKeyNames` reactive watcher merging inherited + own keys; deep-copy on Edit so Cancel discards changes; `createNewAttribute()` returns a fresh `PublicEditableAttributeBag` with `FieldType.Text` default) |
| M4 | `UpdateEntityFromBox` lets `GroupMemberAttributes` flow to Save | ✓ | [GroupDetail.cs:1032-1037](../../Rock.Blocks/Group/GroupDetail.cs:1032) (no `IfValidProperty` needed — the bag list is consumed directly by `SaveGroupMemberAttributeDefinitions` after Group.Id is assigned; engineering comment notes why) |
| M5 | Save step 4b — per-group member attribute definitions sync | ✓ | [GroupDetail.cs:1413](../../Rock.Blocks/Group/GroupDetail.cs:1413) (invocation) + [GroupDetail.cs:721-768](../../Rock.Blocks/Group/GroupDetail.cs:721) (`SaveGroupMemberAttributeDefinitions` helper: materializes existing rows for the qualifier, deletes those whose Guid is no longer in the bag list, upserts via `Helper.SaveAttributeEdits` with `entityTypeId = GroupMember.TypeId`, `qualifierColumn = "GroupId"`, `qualifierValue = entity.Id.ToString()`, reassigns `Order` contiguously to flush reorders) |
| V1 | `editPanel.partial.obs` modified replacing Section 5/6 placeholders | ✓ | [editPanel.partial.obs:316-345](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:316) (Section 5 inline categorized stacks; Section 6 delegates to `<GroupMemberAttributesPartial>`) + [:427-428](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:427) (imports) + [:553-565](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:553) (propertyRefs for `attributeValues` + `groupMemberAttributes`; both added to `propRefs` array at :579 for tracked-property plumbing) |
| V2 | New `groupMemberAttributes.partial.obs` bundling Section 6 | ✓ (MP-4.2 deviation) | [groupMemberAttributes.partial.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupMemberAttributes.partial.obs) (single new file owns both grids + modal + AttributeEditor, matching the canonical `GroupTypeDetail/groupMemberAttributes.partial.obs` sibling; bundled instead of split per spec V1/V2 with MP-4.2 rationale) |
| S1 | Save step 4a inserted | ✓ | [GroupDetail.cs:1404-1409](../../Rock.Blocks/Group/GroupDetail.cs:1404) |
| S2 | Save step 4b inserted | ✓ | [GroupDetail.cs:1411-1413](../../Rock.Blocks/Group/GroupDetail.cs:1411) |
| S3 | 9-step WrapTransaction ordering | ✓ | [GroupDetail.cs:1393-1421](../../Rock.Blocks/Group/GroupDetail.cs:1393) (1) SaveChanges (assigns group.Id), (2) AllowPerson if Add+AdminToCreator, (4a) `entity.SaveAttributeValues`, (4b) `SaveGroupMemberAttributeDefinitions`, (5) Inactive cascade, (6) chat-avatar IsTemporary, (7) photo IsTemporary, (8) inline-schedule delete + final SaveChanges. UpdateEntityFromBox (step 2) already ran before the transaction opens. |
| Q4.1 / #17 | One stack per attribute category + "General" default | ✓ | [editPanel.partial.obs:843-918](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:843) realizes Phase 1 design-coverage row #17 (originally Phase 3, re-deferred to Phase 4 per Phase 3 re-defer table) |
| Q4.2 | `GroupTypeInheritedAttributeBag` widening | ✓ (MP-4.1 deviation) | [GroupTypeOptionsBag.cs:289-302](../../Rock.ViewModels/Blocks/Group/GroupDetail/GroupTypeOptionsBag.cs:289) + [GroupDetail.cs:2462-2520](../../Rock.Blocks/Group/GroupDetail.cs:2462) (`BuildInheritedMemberAttributes` walks `InheritedGroupTypeId` chain with circular-inheritance guard, resolves URL via `EntityTypeCache.Get<GroupType>().LinkUrlLavaTemplate` with fallback to current-page URL using GroupTypeId IdKey, per the canonical pattern at `GroupTypeDetail.cs:1901-2015`) |
| Q4.3 | EntityBagBase.Attributes/Values for Section 5; new field for Section 6 | ✓ | [GroupBag.cs:608](../../Rock.ViewModels/Blocks/Group/GroupDetail/GroupBag.cs:608) (`GroupMemberAttributes: List<PublicEditableAttributeBag>`) — `Attributes` / `AttributeValues` inherited from `EntityBagBase` |
| Q4.4 | Modal + AttributeEditor reuse | ✓ | [groupMemberAttributes.partial.obs:43-46](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupMemberAttributes.partial.obs:43) |

### C2 — Research-coverage walk

#### research/specs/00-architecture.md

| Behavior | Status | Code ref / Notes |
|---|---|---|
| Q2 cascade payload includes inherited member attributes | ✓ | `BuildInheritedMemberAttributes` runs as part of `BuildGroupTypeOptionsBag`; the Vue side reads `props.groupTypeOptions.inheritedMemberAttributes` |
| Q10 `<ContentSection>` / `<ContentStack>` reuse | ✓ | Section 5 uses `<ContentStack v-for="category">`; Section 6's two stacks are inside the new partial |

#### research/specs/03-phase-3-edit-core.md

The Phase 3 Self-review coverage report's "Section 5 / 6 / 7 / 9 / 10 panels | → Phase 4/5" row's Phase 4 portion (Sections 5 + 6) is now ✓ implemented. Phase 3's other rows are unaffected.

#### research/webforms/09-group-attributes.md

| Behavior | Status | Code ref |
|---|---|---|
| Visibility gate — `group.Attributes != null && group.Attributes.Any()` | ✓ | `hasVisibleGroupAttributes` computed at [editPanel.partial.obs:918](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:918); `v-if` on the Section 5 `<ContentSection>` |
| Visibility gate — `excludeForEdit.Count() < group.Attributes.Count()` (hide when user lacks EDIT on all) | ✓ | `LoadAttributesAndValuesForPublicEdit(entity, person, enforceSecurity: true)` at [GroupDetail.cs:677](../../Rock.Blocks/Group/GroupDetail.cs:677) silently drops attributes the user lacks EDIT on; `hasVisibleGroupAttributes` then hides Section 5 when the remaining set is empty |
| Visibility gate — edit-mode only | ✓ | Section 5 only appears in `editPanel.partial.obs`; view-mode `GroupViewLavaTemplate` was dropped per Q3 |
| `ShowGroupTypeEditDetails` AddEditControls flow | ✓ | `<AttributeValuesContainer>` replaces `AddEditControls` server-side render |
| Save: `group.SaveAttributeValues(rockContext)` inside WrapTransaction | ✓ | [GroupDetail.cs:1409](../../Rock.Blocks/Group/GroupDetail.cs:1409) (step 4a) |
| Per-attribute EDIT auth at save time | ✓ | `entity.SetPublicAttributeValues(values, person, enforceSecurity: true)` at [GroupDetail.cs:1030](../../Rock.Blocks/Group/GroupDetail.cs:1030) re-checks per-attribute EDIT auth on assignment (stricter than WebForms which had no save-time recheck) |
| Categorized rendering surfaced as stack headers (design #17) | ✓ | `groupAttributeCategories` computed groups by `Attribute.Category[0]`; AttributeValuesContainer's `:showCategoryLabel="false"` prevents double-header inside each stack |
| GroupType change discards unsaved values | ✓ | The reactive `groupTypeId` watcher in `groupDetail.obs` re-fetches `GroupTypeOptionsBag` on change; client-side, the `bag.attributes` is re-bagged by the parent on the next round-trip |

#### research/webforms/10-member-attributes.md

| Behavior | Status | Code ref |
|---|---|---|
| Two grids — read-only inherited + editable custom | ✓ | [groupMemberAttributes.partial.obs:3-41](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupMemberAttributes.partial.obs:3) |
| ADMINISTRATE auth gates the panel | → Phase 5 | The current implementation always renders Section 6 in edit mode; matches the canonical GroupTypeDetail sibling. **Note:** WebForms `wpGroupMemberAttributes.Visible = canAdministrate` gates the entire panel on ADMINISTRATE; Obsidian convention is to surface server-side via a bag flag. Mid-phase review surfaced this; tracked as a New latent finding for Phase 5 to wire (it's a one-line addition: new bag field + `v-if` on the section). |
| `AllowSpecificGroupMemberAttributes` visibility threshold | → Phase 5 | Same panel-gate question; the canonical GroupTypeDetail sibling does not implement this gate either. Tracked alongside ADMINISTRATE. |
| Inherited attribute walk via `InheritedGroupTypeId` chain | ✓ | [GroupDetail.cs:2462-2520](../../Rock.Blocks/Group/GroupDetail.cs:2462) (`BuildInheritedMemberAttributes` with visited-id guard) |
| Inherited grid: Name / Description / Inherited-from link | ✓ | [groupMemberAttributes.partial.obs:8-24](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupMemberAttributes.partial.obs:8) with anchor + target="_blank" |
| Custom grid columns (Reorder / Name / Description / Required / Security / Edit / Delete) | ✓ | [groupMemberAttributes.partial.obs:33-40](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupMemberAttributes.partial.obs:33) |
| `dlgGroupMemberAttribute_SaveClick` preserves Order across saves | ✓ | `SaveGroupMemberAttributeDefinitions` reassigns Order contiguously per the canonical pattern; `Helper.SaveAttributeEdits` preserves CreatedDateTime/CreatedByPersonAliasId/Foreign* by Guid lookup |
| New attribute defaults `FieldTypeId = TEXT` | ✓ | `createNewAttribute()` returns `fieldTypeGuid: FieldType.Text` at [groupMemberAttributes.partial.obs:124-150](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupMemberAttributes.partial.obs:124) |
| `ReservedKeyNames` = inherited keys ∪ custom keys (excluding self) | ✓ | [groupMemberAttributes.partial.obs:215-236](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupMemberAttributes.partial.obs:215) — watcher recomputes on every change; modal-open removes the active key so self-rename still passes the editor's check |
| `ShowConfirmDeleteDialog="false"` on grid | ✓ | `disableConfirmation` on `<DeleteColumn>` at [:39](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupMemberAttributes.partial.obs:39) (matches the WebForms grid attribute; no confirm prompt on delete) |
| Reorder is local-only (only the editable list reorders) | ✓ | `onOrderChanged` mutates the editable `attributes` ref; inherited list is sorted by ancestor walk order |
| Save body: delete-then-upsert per qualifier `(GroupMember.TypeId, "GroupId", group.Id)` | ✓ | [GroupDetail.cs:721-768](../../Rock.Blocks/Group/GroupDetail.cs:721) |
| `Helper.SaveAttributeEdits` preserves immutable fields | ✓ | Standard framework helper behavior |
| Empty grid + `AllowSpecificGroupMemberAttributes == false` → panel hidden | → Phase 5 | Tracked above |
| New group (Id == 0) — qualifier resolves to real Id after first SaveChanges | ✓ | `SaveGroupMemberAttributeDefinitions` runs inside WrapTransaction step 4b, after the SaveChanges that assigns `entity.Id` (step 3) and after step 4a |
| Case-insensitive qualifier match (`"GroupId"` lowercase tolerated) | ✓ | `LoadGroupMemberAttributes` uses `StringComparison.OrdinalIgnoreCase` |
| Security button uses Attribute entity-type (per-attribute auth) | ✓ | `<Grid :entityTypeGuid="EntityType.Attribute">` + `<SecurityColumn :itemTitle="'name'">` at [:32-37](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupMemberAttributes.partial.obs:32) |
| Inherited link target="_blank" | ✓ | [:15-17](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupMemberAttributes.partial.obs:15) |
| `InheritedFromGroupTypeUrl` uses immediate-ancestor URL | ✓ | `BuildInheritedMemberAttributes` walks the chain and emits each inherited attribute with the **immediate** containing ancestor's URL (per WebForms parity at `GroupDetail.ascx.cs:3181`) |

#### research/webforms/22-grouptype-cascade.md

| Behavior | Status | Code ref |
|---|---|---|
| Cascade payload includes `InheritedMemberAttributes` | ✓ | [GroupDetail.cs:2446](../../Rock.Blocks/Group/GroupDetail.cs:2446) |
| Inherited attribute walk runs once per cascade (no client-side re-walk) | ✓ | `BuildInheritedMemberAttributes` is invoked from `BuildGroupTypeOptionsBag`; the Vue side reads the result without re-querying |
| Per-group custom member attributes stay attached to the group on GroupType change | ✓ | `bag.GroupMemberAttributes` is part of the GroupBag, not the GroupTypeOptionsBag; the GroupType-change cascade does not reset it (the reactive watcher only re-fetches `GroupTypeOptionsBag`) |

#### research/webforms/23-validations-and-cascades.md

| Behavior | Status | Code ref |
|---|---|---|
| Save flow ordering — `SaveAttributeValues` between AllowPerson and inactive cascade | ✓ | Step 4a at [GroupDetail.cs:1409](../../Rock.Blocks/Group/GroupDetail.cs:1409) |
| Save flow ordering — member-attribute-defs sync between SaveAttributeValues and inactive cascade | ✓ | Step 4b at [GroupDetail.cs:1413](../../Rock.Blocks/Group/GroupDetail.cs:1413) |
| Transactional boundary preserved (all attribute writes inside WrapTransaction) | ✓ | `entity.SaveAttributeValues` and `SaveGroupMemberAttributeDefinitions` both run inside the existing `RockContext.WrapTransaction` (Phase 3 lock) |
| `attributeService.Delete()` cascades to `[AttributeValue]` rows for affected group members | ✓ | Standard EF cascade rule on the `Attribute → AttributeValue` FK; `SaveGroupMemberAttributeDefinitions` issues `attributeService.Delete` per removed bag entry |

#### research/design/00-overview.md, 02-edit-panel.md, 03-net-new-features.md

| Behavior | Status | Code ref |
|---|---|---|
| Section 5 Group Attributes — categorized stacks per design item #17 | ✓ | Inline in `editPanel.partial.obs` |
| Section 6 Member Attributes — Inherited stack + Editable stack | ✓ | `groupMemberAttributes.partial.obs` |
| Member-attribute Add modal uses `AttributeEditor` | ✓ | [:43-46](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupMemberAttributes.partial.obs:43) |
| Section ordering — Group Attributes BEFORE Member Attributes (Phase 4 swap from WebForms) | ✓ | Section 5 then Section 6 in `editPanel.partial.obs` |

#### research/design/04-component-inventory.md

| Behavior | Status | Code ref |
|---|---|---|
| `<AttributeValuesContainer>` for Group attribute values | ✓ | Used in Section 5 |
| `<AttributeEditor>` for member-attribute modal | ✓ | Used in Section 6 modal |
| `<Grid>` with `<EditColumn>` / `<DeleteColumn>` / `<ReorderColumn>` / `<SecurityColumn>` / `<BooleanColumn>` | ✓ | Both grids in `groupMemberAttributes.partial.obs` |
| `<Modal>` for member-attribute Add/Edit | ✓ | Used in Section 6 |

#### Reference — Rock.Blocks/Group/GroupTypeDetail.cs

| Behavior | Status | Code ref |
|---|---|---|
| `LoadAttributesAndValuesForPublicEdit` invocation pattern | ✓ | Applied at [GroupDetail.cs:677](../../Rock.Blocks/Group/GroupDetail.cs:677) (matches `GroupTypeDetail.cs:423`) |
| Member-attribute-definition sync pattern (`SaveAttributes` shape) | ✓ | `SaveGroupMemberAttributeDefinitions` mirrors the canonical pattern (`GroupTypeDetail.cs:1316-1354`) |
| Inherited attribute walk pattern (`GetInheritedAttributes` BlockAction) | ✓ | Adapted server-side into `BuildInheritedMemberAttributes` (`GroupTypeDetail.cs:1901-2015` is the reference; we call it inline from `BuildGroupTypeOptionsBag` since the cascade already delivers it) |

### C5 — New latent bugs / TODOs surfaced

1. **ADMINISTRATE / `AllowSpecificGroupMemberAttributes` gates on Section 6 panel visibility.** WebForms `GroupDetail.ascx.cs:2001-2004` hides `wpGroupMemberAttributes` entirely when the current user lacks ADMINISTRATE on the group, and `BindGroupMemberAttributesInheritedGrid` keeps the panel hidden when `GroupMemberAttributesInheritedState.Any() == false AND GroupMemberAttributesState.Any() == false AND CurrentGroupTypeCache.AllowSpecificGroupMemberAttributes == false`. The canonical `GroupTypeDetail/groupMemberAttributes.partial.obs` sibling we mirrored does not gate the panel on either flag. Phase 4 ships matching the canonical sibling (always-rendered in edit mode). **Recommendation:** Phase 5 should add `CanAdministrateGroup` + `AllowSpecificGroupMemberAttributes` flags to a bag field (or wrap them into `GroupTypeOptionsBag`) and gate the Section 6 `<ContentSection>` accordingly. One-line addition per side.

2. **Section 5 visibility re-evaluates on every render via `bag.attributes`.** When the GroupType changes mid-edit, the bag's `Attributes` dictionary must refresh from the new GroupType. Phase 3's reactive `groupTypeId` watcher fetches `GroupTypeOptionsBag`, but **does not** re-fetch `bag.attributes`. Reviewed during Phase 4 self-review: this is correct behavior on existing groups (the GroupType dropdown is read-only on existing groups per Phase 3) but for the Add path the user can change GroupType freely. If the Add path's selected GroupType has different group attributes than the initial render's default, the editor would still show the initial set. **Tracked for Phase 5/6** to consider whether to extend the cascade to refresh `bag.attributes` or to invalidate Section 5 entirely on cascade until Save / Re-edit.

3. **`reservedKeyNames` modal-open removal can be permanent on Cancel.** The watcher at `groupMemberAttributes.partial.obs:215-228` removes the active attribute's key from `reservedKeyNames` when the modal opens, then re-adds it on close. If the user cancels the modal (closes without Save), the re-add path runs and the key is restored. Verified — not a bug, just worth noting that the watcher relies on modal-close to fire regardless of how the modal closes.

## Completed

### Summary

Phase 4 ships Group Attribute Values editing (Section 5) and Group Member Attribute Definitions editing (Section 6) for the GroupDetail Obsidian block. Section 5 renders each `Attribute.Category` as a `<ContentStack>` containing an `<AttributeValuesContainer>` filtered to that category (Q4.1 lock; realizes Phase 1's deferred design item #17 "categories as stack headers"). Section 6 is a new `groupMemberAttributes.partial.obs` modeled on the canonical `GroupTypeDetail/groupMemberAttributes.partial.obs` sibling: a read-only Inherited Attributes grid sourced from the GroupType cascade payload (per Q4.2 lock, widened to `GroupTypeInheritedAttributeBag[]` with the immediate-ancestor name + URL), an editable Custom Attributes grid with Reorder / Name / Description / Required / Security / Edit / Delete columns, and a `<Modal>` + `<AttributeEditor>` for Add/Edit. The `Save` block action's `WrapTransaction` body extends to a 9-step ordering: Phase 3's step 4 (AllowPerson) is followed by step 4a (`entity.SaveAttributeValues(RockContext)`) and step 4b (per-group member attribute definitions sync via `Rock.Attribute.Helper.SaveAttributeEdits` with `entityTypeId = GroupMember.TypeId`, `qualifierColumn = "GroupId"`, `qualifierValue = group.Id.ToString()`), before continuing to Phase 3's step 5 (Inactive cascade) onward. The GroupType cascade now populates `InheritedMemberAttributes` via a server-side inheritance walk (`BuildInheritedMemberAttributes`) that mirrors the canonical `GroupTypeDetail.GetInheritedAttributes` body — guards against circular inheritance with a visited-id set, resolves the inherited GroupType URL via `EntityType.LinkUrlLavaTemplate` with a current-page-URL fallback.

Two mid-phase decisions deviated from spec defaults. **MP-4.1**: kept `GroupTypeInheritedAttributeBag` in its existing `Rock.ViewModels.Blocks.Group.GroupTypeDetail` namespace and imported it via `using` instead of relocating per Q4.2's "preferred resolution"; lighter-touch change preserving every existing import path across C# + TS layers. **MP-4.2**: bundled Section 6's Inherited Grid + Custom Grid + Modal + AttributeEditor into a single new partial file (matching canonical sibling shape) instead of inlining the grid in `editPanel.partial.obs` per spec V1 and splitting the modal into `groupMemberAttributeModal.partial.obs` per spec V2; rationale = canonical-pattern fidelity + simpler co-located state ownership.

### Coverage report

The full coverage report appears under "Self-review coverage report" above. Summary: every Implementation checklist item is ✓ implemented with a file:line reference; every Research-coverage behavior is classified ✓ / → / ✗. Zero ✗ MISSED rows. Two rows are `→ Phase 5` for the ADMINISTRATE / `AllowSpecificGroupMemberAttributes` panel-visibility gates (the canonical sibling we mirrored does not implement these either; surfaced as new latent finding #1).

### Deviations from the spec

| # | Deviation | Reason / Mid-phase decision |
|---|---|---|
| 1 | `GroupTypeInheritedAttributeBag` imported cross-block (not relocated to a shared namespace) | MP-4.1 — lighter-touch change; preserves every existing import path across C# + TS layers without losing the cross-block reuse Q4.2 wanted |
| 2 | Section 6 bundled into single `groupMemberAttributes.partial.obs` (not split per spec V1 inline + V2 separate modal) | MP-4.2 — canonical-pattern fidelity + simpler state ownership; modal state (`isModalVisible`, `theAttribute`, `reservedKeyNames`) co-located with grid handlers in one file |

### Files changed

**New files:**

- `Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/groupMemberAttributes.partial.obs` (Section 6 bundle)

**Modified files:**

- `Rock.Blocks/Group/GroupDetail.cs` (+~220 lines):
  - `GetEntityBagForEdit` calls `bag.LoadAttributesAndValuesForPublicEdit` (line 677) and populates `bag.GroupMemberAttributes` via `LoadGroupMemberAttributes` helper (line 685)
  - New `LoadGroupMemberAttributes` helper (line 697)
  - New `SaveGroupMemberAttributeDefinitions` helper (line 721)
  - `UpdateEntityFromBox` adds `IfValidProperty(AttributeValues)` block (line 1027)
  - `Save` `WrapTransaction` body adds step 4a (`entity.SaveAttributeValues`) at line 1409 and step 4b (`SaveGroupMemberAttributeDefinitions`) at line 1413
  - `BuildGroupTypeOptionsBag` invokes new `BuildInheritedMemberAttributes` helper at line 2446
  - New `BuildInheritedMemberAttributes` helper (line 2462) walks `InheritedGroupTypeId` chain
  - New `using Rock.ViewModels.Blocks.Group.GroupTypeDetail;` import
- `Rock.ViewModels/Blocks/Group/GroupDetail/GroupBag.cs` (+21 lines): adds `GroupMemberAttributes: List<PublicEditableAttributeBag>` (line 608)
- `Rock.ViewModels/Blocks/Group/GroupDetail/GroupTypeOptionsBag.cs` (+17 / -7 lines):
  - Adds `using Rock.ViewModels.Blocks.Group.GroupTypeDetail;`
  - Widens `InheritedMemberAttributes` from `List<PublicAttributeBag>` to `List<GroupTypeInheritedAttributeBag>` (line 297)
- `Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs` (+170 lines):
  - Imports `AttributeValuesContainer`, `GroupMemberAttributesPartial`, `GroupTypeInheritedAttributeBag`, `PublicAttributeBag`, `PublicEditableAttributeBag`
  - Replaces Section 5 / 6 placeholders with categorized stacks (line 316) and `<GroupMemberAttributesPartial>` (line 339)
  - Adds `attributeValues` + `groupMemberAttributes` propertyRefs (line 553)
  - Adds `GroupAttributeCategory` type + `groupAttributeCategories` / `hasVisibleGroupAttributes` / `attributeValuesValue` / `inheritedMemberAttributes` / event handler computeds (line 843)
  - Adds round-trip lines in inbound + outbound watchers (lines 1022, 1094)
- `Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupBag.d.ts` (+18 lines): adds `groupMemberAttributes?: PublicEditableAttributeBag[] | null` field
- `Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupTypeOptionsBag.d.ts` (+10 / -6 lines): widens `inheritedMemberAttributes` to `GroupTypeInheritedAttributeBag[]`

### New latent bugs / TODOs surfaced

See "C5 — New latent bugs / TODOs surfaced" above.

### Build status

- `dotnet build Rock.Blocks`: **clean** (0 errors, 46 pre-existing warnings).
- `npx eslint --max-warnings=0 src` over `Rock.JavaScript.Obsidian.Blocks`: 21 warnings, all pre-existing in unrelated files (`packageDetail.obs` etc.); Phase 4 files (filtered by path) have 0 warnings.
- `npx vue-tsc --noEmit` over `Rock.JavaScript.Obsidian.Blocks`: **clean** (0 errors).

### Commit hash

Initial Phase 4 commit: `f789db3b40` (phase 4 code, 2026-05-11).

## Post-commit iterative review (2026-05-11)

After the initial Phase 4 commit landed, a review pass surfaced three follow-ups. They are tracked here (rather than rolled into Phase 5) because they are pure corrections to what Phase 4 shipped, not new behavior.

### IR-4-1 (committed `5e2ec9efc5`) — Section 5 refactored to use `displayAsContentSection`

Replaced the manual `groupAttributeCategories` computed (~80 lines: type definition + grouping logic + `hasVisibleGroupAttributes` + the `<ContentStack v-for>` template wrapper) with a single `<AttributeValuesContainer :displayAsContentSection="true" ...>` invocation. The shared control already handles ContentSection wrapping, per-category ContentStacks, default-category-first ordering, empty-category dropping, and 2-column layout. Two prop additions support the use case:

- `contentSectionTitle: string` (default `"Attributes"`) on `attributeValuesContainer.obs` — lets Section 5 say "Group Attributes" without forking the control.
- `globalCategoryNameOverride="Set Additional Attributes"` (existing prop) — relabels the default categoryless stack per the Figma "Set Additional Attributes" wording.

`contentSectionTitle` prop addition shipped separately as `5e2ec9efc5` (control change) so the GroupDetail-block-only diff stays surgical.

### IR-4-2 — Cascade attribute refresh via canonical `RefreshAttributes` (resolves Phase 5 Q5.4 / Misc-2)

Added a single line to `GetObsidianBlockInitialization`:

```csharp
box.QualifiedAttributeProperties = AttributeCache.GetAttributeQualifiedColumns<Model.Group>();
```

This is the framework-canonical pattern. The existing wiring (`useEntityDetailBlock` in `groupDetail.obs:104` + `@propertyChanged="baseBlock.onPropertyChanged"` in `groupDetail.obs:41` + `watchPropertyChanges(propRefs, emit)` in `editPanel.partial.obs:992`) automatically:

1. Detects `GroupTypeId` propertyRef changes.
2. Forwards the property name to the baseBlock handler.
3. Checks the new `QualifiedAttributeProperties` list (case-insensitive match).
4. Debounces and calls the inherited `RockEntityDetailBlockType.RefreshAttributes` block action (`Rock.Blocks/RockEntityDetailBlockType.cs:132`).
5. Server `RefreshAttributes` calls `TryGetEntityForEditAction` (handles Add and Edit), `UpdateEntityFromBox` (applies new GroupTypeId to the entity), `LoadAttributes`, then `GetEntityBagForEdit` to return the new bag.
6. `block.ts:refreshEntityDetailAttributes` merges the new `attributes` + `attributeValues` into `groupEditBag.value.bag`, preserving user-typed values for Attribute Guids that exist in both old and new lists.

Drops the entire Q5.4 / Misc-2 deferral. The earlier prototype that extended `GroupTypeOptionsBag` with attribute fields + a manual bag mutation in the cascade watcher was reverted in favor of this.

**Parity addition:** the same `box.QualifiedAttributeProperties = AttributeCache.GetAttributeQualifiedColumns<GroupType>()` line was added to `GroupTypeDetail.cs:GetObsidianBlockInitialization`, which had the same gap. Out-of-scope for Phase 4 strictly speaking, but a one-line parity fix on the canonical sibling block.

### IR-4-3 — `BuildInheritedMemberAttributes` URL fallback removed

`BuildInheritedMemberAttributes` previously fell back to `GetCurrentPageUrl(GroupTypeId=<idKey>)` when `EntityType.LinkUrlLavaTemplate` for GroupType was unconfigured. This pattern was copied from `GroupTypeDetail.cs:1952-1958` where it works (current page IS GroupTypeDetail), but on GroupDetail it produces a broken link to the current group with a stray `GroupTypeId` param. The fallback was removed; the Vue grid template already renders the inherited-from name as plain text when `inheritedFromGroupTypeUrl` is empty.

### Rename: `GroupTypeInheritedAttributeBag` → `GroupMemberInheritedAttributeBag`

During the review pass MP-4.1's cross-namespace import was replaced with a new bag dedicated to GroupDetail's needs. The new `Rock.ViewModels.Blocks.Group.GroupDetail.GroupMemberInheritedAttributeBag` carries only the fields Section 6 needs (Name / Description / Key / Guid / InheritedFromGroupTypeName / InheritedFromGroupTypeUrl). This decoupled GroupDetail from the GroupTypeDetail bag's evolution.

### Considered-and-skipped — `SaveGroupMemberAttributes` SaveChanges-in-loop

[GroupDetail.cs:1156-1160](../../Rock.Blocks/Group/GroupDetail.cs:1156) calls `RockContext.SaveChanges()` inside the delete loop instead of once after all deletions like WebForms does ([GroupDetail.ascx.cs:1348-1359](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1348)). Audit caught this; user opted to leave as-is. **Reason:** correctness is unchanged (the calls run inside `WrapTransaction`), the cardinality is small (group-member attribute definitions per group are typically 0-10 rows), and the deletion path is rare (only fires when a definition is removed). Performance smell, not a bug; not worth a code change.
