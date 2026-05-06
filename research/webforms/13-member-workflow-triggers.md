# Sub-feature: Group Member Workflow Triggers

## What it is

Triggers that launch a Workflow when a group member changes in some way. Triggers are scoped to this single group (in addition to group-type-level triggers, which live elsewhere).

Trigger types:
- `MemberAddedToGroup`
- `MemberRemovedFromGroup`
- `MemberAttendedGroup`
- `MemberPlacedElsewhere`
- `MemberRoleChanged`
- `MemberStatusChanged`

Each trigger has a name, IsActive flag, WorkflowType, and a `TypeQualifier` string that encodes additional conditions in a `|`-delimited format.

## TypeQualifier format

Pipe-delimited 7-tuple, parsed at [GroupDetail.ascx.cs#L4689](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4689) and rebuilt at [GroupDetail.ascx.cs#L4893](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4893):
```
{ToStatus}|{ToRole}|{FromStatus}|{FromRole}|{FirstTime}|{ShowNote}|{RequireNote}
```

Each part can be empty. Different trigger types use different parts:

| Trigger Type | Visible parts |
|---|---|
| MemberAddedToGroup / MemberRemovedFromGroup | ToStatus, ToRole |
| MemberAttendedGroup | FirstTime |
| MemberPlacedElsewhere | ShowNote, RequireNote |
| MemberRoleChanged | FromRole, ToRole |
| MemberStatusChanged | FromStatus, ToStatus |

The `ToStatus` / `FromStatus` segments hold `GroupMemberStatus` enum names (e.g., "Active", "Inactive", "Pending") or empty for "Any".
The `ToRole` / `FromRole` segments hold `GroupTypeRole.Guid` strings (NOT integer IDs) or empty for "Any". This matters for serialization across upgrades and restorations.

## Trigger conditions

The "Group Member Workflows" PanelWidget is gated by:

| Gate | Source | Effect |
|---|---|---|
| `selectedGroupType.AllowSpecificGroupMemberWorkflows OR group.GroupMemberWorkflowTriggers.Any()` | [GroupDetail.ascx.cs#L2198](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2198) | Shown if the group type allows specific triggers OR there's already at least one trigger in DB. |
| `setValues == true` (called from `ShowGroupTypeEditDetails`) | [GroupDetail.ascx.cs#L2185](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2185) | Visibility is only flipped when entering edit mode or changing GroupType. Otherwise the prior value persists. |
| Edit mode | `pnlEditDetails.Visible` | Only shown in edit mode. |

Note: There is **no ADMINISTRATE gate** on this panel, unlike requirements/member attributes. EDIT auth on the group is sufficient (since `pnlDetails.Visible` requires VIEW + edit-mode requires EDIT).

The modal `dlgMemberWorkflowTriggers` opens when:
- The user clicks the Add button.
- The user clicks the Edit pencil on a row.

## Markup region

[`RockWeb/Blocks/Groups/GroupDetail.ascx`](../../RockWeb/Blocks/Groups/GroupDetail.ascx) lines 415-435 plus modal at 675-709.

```
wpMemberWorkflowTriggers (visible if group type allows OR has any) (title "Group Member Workflows")
├── NotificationBox3 (Info): "The workflow(s) that should be launched when group members are changed in this group."
└── gMemberWorkflowTriggers grid (light, no paging, EnableResponsiveTable=false)
    └── columns: Reorder, Name, Workflow (`WorkflowType.Name`), When (HTML via FormatTriggerType), Active (BoolField), Edit, Delete

dlgMemberWorkflowTriggers (modal, ValidationGroup="Trigger")
├── nbInvalidWorkflowType (Danger notification, hidden by default)
├── tbTriggerName (Required) + cbTriggerIsActive
├── wtpWorkflowType (WorkflowTypePicker, Required, help "The workflow type to start.")
├── ddlTriggerType (Required, AutoPostBack → ShowTriggerQualifierControls, label "When")
└── conditional sub-controls per trigger type:
    ├── ddlTriggerFromStatus (with "Any" first, label "From Status of")
    ├── ddlTriggerToStatus (with "Any" first, label "To Status of" or "With Status of" depending on trigger)
    ├── ddlTriggerFromRole (with "Any" first, DataTextField="Name" DataValueField="Guid", label "From Role of")
    ├── ddlTriggerToRole (with "Any" first, DataTextField="Name" DataValueField="Guid", label "To Role of" or "With Role of")
    ├── cbTriggerFirstTime ("First Time", help)
    ├── cbTriggerPlacedElsewhereShowNote ("Show Note", help)
    └── cbTriggerPlacedElsewhereRequireNote ("Require Note", help)
```

## UI surface

| Control | Type | Help text |
|---|---|---|
| `tbTriggerName` | RockTextBox | Required. |
| `cbTriggerIsActive` | RockCheckBox | Default true on Add. |
| `wtpWorkflowType` | WorkflowTypePicker | "The workflow type to start." Required (validated server-side at SaveClick, empty is rejected). |
| `ddlTriggerType` | RockDropDownList | Required. Items from `GroupMemberWorkflowTriggerType` enum, AutoPostBack. |
| `ddlTriggerFromStatus` | RockDropDownList | Items: empty/"Any" then `GroupMemberStatus` enum names. |
| `ddlTriggerToStatus` | RockDropDownList | Items: empty/"Any" then `GroupMemberStatus` enum names. Label flips between "To Status of" and "With Status of" by trigger type. |
| `ddlTriggerFromRole` | RockDropDownList | Items: empty/"Any" then `groupType.Roles` (DataTextField=Name, DataValueField=Guid). |
| `ddlTriggerToRole` | RockDropDownList | Items: empty/"Any" then `groupType.Roles`. Label flips. |
| `cbTriggerFirstTime` | RockCheckBox | "Select this option if workflow should only be started when person attends the group for the first time. Leave this option unselected if the workflow should be started whenever a person attends the group." |
| `cbTriggerPlacedElsewhereShowNote` | RockCheckBox | "Select this option if workflow should show UI for entering a note when the member is placed." |
| `cbTriggerPlacedElsewhereRequireNote` | RockCheckBox | "Select this option if workflow should show UI for entering a note and make it required when the member is placed." |
| `nbInvalidWorkflowType` | NotificationBox | "The Workflow Type is missing or invalid. Make sure you selected a valid Workflow Type (and not a category)." Shown when `WorkflowTypeId == 0` after attempted save. |

Modal title is "Add Trigger" or "Edit Trigger" depending on whether a trigger Guid was passed.

## Code-behind summary

| Method | File:Line | Notes |
|---|---|---|
| `gMemberWorkflowTriggers_Add` / `_Edit` / `_ShowEdit` | [GroupDetail.ascx.cs#L4617-L4700](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4617) | Populates From/To Status/Role dropdowns; loads existing or creates new with IsActive=true; parses TypeQualifier into individual controls. |
| `ShowTriggerQualifierControls` | [GroupDetail.ascx.cs#L4705-L4795](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4705) | Visibility per trigger type (table below). |
| `gMemberWorkflowTriggers_GridReorder` | [GroupDetail.ascx.cs#L4802-L4806](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4802) | ReorderMemberWorkflowTriggerList + bind. |
| `gMemberWorkflowTriggers_Delete` | [GroupDetail.ascx.cs#L4814-L4820](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4814) | Remove from state. |
| `ddlTriggerType_SelectedIndexChanged` | [GroupDetail.ascx.cs#L4837-L4840](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4837) | Calls ShowTriggerQualifierControls. |
| `dlgMemberWorkflowTriggers_SaveClick` | [GroupDetail.ascx.cs#L4847-L4914](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4847) | Builds TypeQualifier; preserves Guid+Order; validates WorkflowTypeId != 0. |
| `BindMemberWorkflowTriggersGrid` | [GroupDetail.ascx.cs#L4919-L4924](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4919) | Sort by Order, DataBind. |
| `FormatTriggerType` | [GroupDetail.ascx.cs#L4932-L4996](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4932) | HTML formatting for the "When" column. Outputs strings like `"Member Status Changed from status of <strong>Active</strong> to status of <strong>Inactive</strong>"`. Reads `CurrentGroupTypeCache.Roles` to resolve role-Guid → role-Name. |
| `btnSave_Click` removal | [GroupDetail.ascx.cs#L853-L858](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L853) | Remove from DB any not in state. Set `triggersUpdated = true`. |
| `btnSave_Click` add/update | [GroupDetail.ascx.cs#L1024-L1041](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1024) | Add/update from state. Sets `triggersUpdated = true`. |
| `btnSave_Click` post-save | [GroupDetail.ascx.cs#L1427-L1430](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1427) | If `triggersUpdated`, `GroupMemberWorkflowTriggerService.RemoveCachedTriggers()`. |

## Server flow (Edit -> Save)

1. **Enter edit mode** (`ShowEditDetails`):
   - Initialize `MemberWorkflowTriggersState` from `group.GroupMemberWorkflowTriggers`. ([GroupDetail.ascx.cs#L2130-L2134](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2130))
   - Call `BindMemberWorkflowTriggersGrid()`.
2. **GroupType change**: `wpMemberWorkflowTriggers.Visible` is recomputed in `ShowGroupTypeEditDetails` based on the new GroupType's `AllowSpecificGroupMemberWorkflows` flag OR existing triggers.
3. **Add / Edit modal** (`gMemberWorkflowTriggers_ShowEdit`):
   - Bind ddlTriggerType to `GroupMemberWorkflowTriggerType` enum.
   - Bind ddlTriggerFromStatus / ddlTriggerToStatus to `GroupMemberStatus` enum, prepend "Any" with empty value.
   - Bind ddlTriggerFromRole / ddlTriggerToRole to `CurrentGroupTypeCache.Roles` (DataTextField=Name, DataValueField=Guid), prepend "Any".
   - **Note**: roles are read from the **immediate** GroupType, not from inherited ancestors. See [24-grouptype-inheritance.md](24-grouptype-inheritance.md).
   - On Add: new GroupMemberWorkflowTrigger with `IsActive=true`.
   - On Edit: pull from `MemberWorkflowTriggersState`, set form fields including parsed `TypeQualifier`.
   - Call `ShowTriggerQualifierControls()`.
4. **Save modal** (`dlgMemberWorkflowTriggers_SaveClick`):
   - Find existing in state by Guid; if found, `CopyPropertiesFrom`; else new with `Order = max+1` and `GroupId = hfGroupId.ValueAsInt()`.
   - Set `Name`, `IsActive` from form.
   - Resolve `WorkflowType` via `WorkflowTypeService.Queryable().FirstOrDefault(a => a.Id == workflowTypeId.Value)`. If `wtpWorkflowType.SelectedValueAsInt()` is null, sets `WorkflowTypeId = 0`.
   - **Validation**: if `WorkflowTypeId == 0`, show `nbInvalidWorkflowType` and return without saving. (Catches the case where the user picks a Category instead of a WorkflowType in the picker.)
   - Set `TriggerType` from `ddlTriggerType.SelectedValueAsEnum<...>`.
   - Build `TypeQualifier` via `string.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}", ToStatus, ToRole, FromStatus, FromRole, FirstTime, ShowNote, RequireNote)`. **Note** the order matches parsing in step 3, but does not match the visual layout; e.g., "ToStatus" comes before "FromStatus" in the qualifier.
   - `memberWorkflowTrigger.IsValid` short-circuits silently if false.
   - **Replace by Guid**: `MemberWorkflowTriggersState.RemoveEntity(guid)` then `Add(trigger)`. The replace pattern means the trigger always lands at the end of the list. **Caveat**: this means editing a trigger moves it to the bottom of the order. The `BindMemberWorkflowTriggersGrid` then re-sorts by `Order`, but since the trigger preserves its `Order` from `CopyPropertiesFrom`, it stays where it should. Verify in spec phase.
5. **Reorder** (`gMemberWorkflowTriggers_GridReorder`): `ReorderMemberWorkflowTriggerList(state, oldIndex, newIndex)` ([line 3263](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3263)) shifts neighbor `Order` values then sets the moved trigger's `Order = newIndex`.
6. **Delete** (`gMemberWorkflowTriggers_Delete`): remove from state by Guid + bind. No confirm dialog.
7. **Save group** (`btnSave_Click`):
   - **Pre-Save** ([line 853-L858](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L853)): for each trigger in `group.GroupMemberWorkflowTriggers` whose Guid is NOT in state, `groupMemberWorkflowTriggerService.Delete(t)` and remove from `group.GroupMemberWorkflowTriggers`. Set `triggersUpdated = true`.
   - **Add/Update** ([line 1024-L1041](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1024)): for each in state, find by Guid in `group.GroupMemberWorkflowTriggers`. If not found, create new and add to collection. `CopyPropertiesFrom(state)`. Set `triggersUpdated = true`.
   - **Post-Save** ([line 1427-L1430](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1427)): if `triggersUpdated`, call `GroupMemberWorkflowTriggerService.RemoveCachedTriggers()` to flush the in-memory cache.

## ShowTriggerQualifierControls (visibility map)

| Trigger Type | FromStatus | ToStatus label | FromRole | ToRole label | FirstTime | ShowNote | RequireNote |
|---|---|---|---|---|---|---|---|
| MemberAddedToGroup | hidden | "With Status of" | hidden | "With Role of" | hidden | hidden | hidden |
| MemberRemovedFromGroup | hidden | "With Status of" | hidden | "With Role of" | hidden | hidden | hidden |
| MemberAttendedGroup | hidden | hidden | hidden | hidden | visible | hidden | hidden |
| MemberPlacedElsewhere | hidden | hidden | hidden | hidden | hidden | visible | visible |
| MemberRoleChanged | hidden | hidden | visible | "To Role of" | hidden | hidden | hidden |
| MemberStatusChanged | visible | "To Status of" | hidden | hidden | hidden | hidden | hidden |

## State

| Property | Type | Notes |
|---|---|---|
| `MemberWorkflowTriggersState` | `List<GroupMemberWorkflowTrigger>` | JSON-serialized into ViewState. Source of truth between postbacks. |

## Visibility

```csharp
wpMemberWorkflowTriggers.Visible = selectedGroupType.AllowSpecificGroupMemberWorkflows
                                    || group.GroupMemberWorkflowTriggers.Any();
```

(line 2198, only flipped when `setValues=true`. Otherwise preserves prior visibility.)

## Side effects

Any trigger add/update/delete sets `triggersUpdated = true`, which after save calls `GroupMemberWorkflowTriggerService.RemoveCachedTriggers()` to flush the in-memory cache. **Without this**, runtime trigger evaluation would use the stale cache and either fail to fire newly-added triggers or fire just-deleted ones until the cache TTL expires.

## Persisted state (database)

Triggers live in the `[GroupMemberWorkflowTrigger]` table. Group-specific rows have `GroupId IS NOT NULL` and `GroupTypeId IS NULL`.

Columns written by Save:
- `GroupId` (set in modal-save, used after group has Id)
- `Name`
- `IsActive`
- `WorkflowTypeId` (FK to `[WorkflowType]`)
- `TriggerType` (enum int)
- `TypeQualifier` (nvarchar, the pipe-delimited 7-tuple)
- `Order` (int)
- `Guid`

## Edge cases

- **Workflow Category vs Workflow Type**: the `WorkflowTypePicker` allows the user to expand categories. If they accidentally pick a category, the `wtpWorkflowType.SelectedValueAsInt()` returns null, so `WorkflowTypeId` ends up 0. The `nbInvalidWorkflowType` notification surfaces this. Important to preserve in Obsidian.
- **Roles tied to GroupType**: `ddlTriggerFromRole` / `ddlTriggerToRole` are populated from `CurrentGroupTypeCache.Roles`. If the GroupType changes after a trigger is configured, the role-Guid stored in `TypeQualifier` may no longer exist in `groupType.Roles`. `FormatTriggerType` handles this by checking `groupType.Roles.FirstOrDefault(r => r.Guid.Equals(roleGuid))` and silently omitting the part if not found ([line 4965-L4988](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4965)).
- **Status enum values**: `ddlTriggerFromStatus` / `ToStatus` use enum **names**, not values. If `GroupMemberStatus` enum names ever change, existing TypeQualifiers break. Treat as stable.
- **TypeQualifier with fewer than 7 parts**: `qualifierParts.Length > N` checks defensively at parse time. Older data with fewer pipes is tolerated; missing parts default to empty string / false.
- **Edited trigger reorders to end of list (false alarm)**: `MemberWorkflowTriggersState.RemoveEntity(guid)` + `Add(trigger)` puts the trigger at the end of the list, but `CopyPropertiesFrom(existing)` preserves its `Order`. `BindMemberWorkflowTriggersGrid` re-sorts by `Order`, so the visual position is preserved.
- **No confirm on delete**: `gMemberWorkflowTriggers_Delete` removes immediately. Differs from typical Rock grids.
- **`GroupId = hfGroupId.ValueAsInt()` on new triggers in modal save**: if the group is new (`Id == 0`), `groupId` is 0 in state. The `btnSave_Click` flow assigns the real `GroupId` on save through `group.GroupMemberWorkflowTriggers.Add(trigger)` which sets the FK via the navigation property. Verify in spec phase whether this is robust if the `GroupId = 0` value persists into the entity.
- **Reorder of inactive triggers**: re-orderable, no different from active. The `IsActive` flag does not affect ordering.

## JS interactions

- Modal cancel uses `OnCancelScript="clearActiveDialog();"`.
- ddlTriggerType has `AutoPostBack="true"`, postback drives `ShowTriggerQualifierControls`. In Obsidian, this becomes a Vue computed.

## Permission inheritance

| Scope | Auth |
|---|---|
| Panel visible | EDIT auth on the group (block-level), AND `selectedGroupType.AllowSpecificGroupMemberWorkflows` OR existing triggers. **No ADMINISTRATE check.** |
| Add / Edit / Delete | Once panel visible, no further check. |
| Workflow Type selection | The picker enforces the user's permissions (calls `WorkflowType.IsAuthorized` on rendering). |
| Save (final) | Implicit. |

## Phase considerations

Self-contained. The TypeQualifier parsing/serialization logic is self-contained too. Good candidate for a focused phase.

In Obsidian, the TypeQualifier should likely be deserialized into a typed bag rather than a string passed across the wire, let the C# block handle the serialization.

## Notes

- The "Any" option for FromStatus/ToStatus/FromRole/ToRole maps to empty string in the TypeQualifier.
- Reorder is supported by Grid.GridReorder event with Order field on the entity.
- `FormatTriggerType` returns HTML (with `<strong>` tags). In Obsidian this should be a structured Vue cell renderer with safe templating. DO NOT pass HTML strings through the wire if the GroupType's role names can contain user-supplied content. Today's WebForms uses Eval+Format which means an `<` in a role name renders as raw HTML. Mitigate with `Server.HtmlEncode` or a Vue `<template>` slot.

## Open questions / flag for spec phase

- **Roles do not inherit**: `ddlTriggerFromRole` / `ToRole` use `CurrentGroupTypeCache.Roles` directly, not the inherited chain. If a role from an ancestor GroupType is desired, the user has to define it on the immediate GroupType. Confirm intent (likely correct: triggers are scoped to this group's type).
- **HTML-injection in `FormatTriggerType`**: the `<strong>{role.Name}</strong>` rendering in [line 4968 etc.](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4968) is unencoded. Could leak HTML if a role name contains tags. Treat as a real bug to fix during conversion (not a regression, same hole exists in WebForms today).
- **`GroupId = 0` on triggers added before group save**: the modal save assigns `GroupId = hfGroupId.ValueAsInt()`, which is 0 for new groups. The pre-save flow sets the FK via collection.Add. Verify the entity's GroupId is correctly written to DB.
- **OptionsBag implications**: `groupType.Roles` is part of the per-GroupType options. See [24-grouptype-inheritance.md](24-grouptype-inheritance.md) and [22-grouptype-cascade.md](22-grouptype-cascade.md).
