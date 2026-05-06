# Sub-feature: Group Requirements

## What it is

Group requirements are conditions that group members must meet (e.g., "Background Check completed", "Lives within 25 miles", "Date Field X is set"). They can be defined on the **group type** (apply to all groups of that type) or on the **group itself** (specific to this group).

The block exposes both:
1. **Read-only** grid of group-type-level requirements ("From {GroupType}").
2. **Editable** grid of group-level requirements (Add / Edit / Delete via modal).

Note: the read-only "From group type" grid in this block does NOT walk the inheritance chain. It queries `Where( a => a.GroupTypeId == CurrentGroupTypeId )` directly. See [`BindGroupRequirementsGrid`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4541) and [24-grouptype-inheritance.md](24-grouptype-inheritance.md). Group requirements at runtime DO consider inheritance via the `Group.GetGroupRequirements()` method (which only returns the immediate `GroupTypeId` requirements but is consumed alongside `GroupRequirementService` which can join through the chain), verify this in spec phase if precision matters.

## Trigger conditions

The "Group Requirements" PanelWidget is gated by:

| Gate | Source | Effect |
|---|---|---|
| `canAdministrate = group.IsAuthorized( Authorization.ADMINISTRATE, CurrentPerson )` | [GroupDetail.ascx.cs#L2003](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2003) | Hidden entirely for users without ADMINISTRATE on the group. |
| `groupTypeGroupRequirements.Any() OR groupGroupRequirements.Any() OR CurrentGroupTypeCache.EnableSpecificGroupRequirements` | [GroupDetail.ascx.cs#L4555](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4555) | Hidden if there are no requirements (group-type or group-specific) AND the group type does not allow specific group requirements. |
| Edit mode | `pnlEditDetails.Visible` | Only shown in edit mode. |

The Add button visibility is set separately:
```csharp
gGroupRequirements.Actions.ShowAdd = CurrentGroupTypeCache.EnableSpecificGroupRequirements;
```
([GroupDetail.ascx.cs#L4554](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4554))

The modal `mdGroupRequirement` opens when the user clicks Add or Edit on the editable grid.

## Markup region

[`RockWeb/Blocks/Groups/GroupDetail.ascx`](../../RockWeb/Blocks/Groups/GroupDetail.ascx) lines 327-357 plus modal at lines 603-630.

```
wpGroupRequirements (visible only with ADMINISTRATE)
├── rcwGroupTypeGroupRequirements (read-only, label "Group Requirements for Group Type")
│   ├── lGroupTypeGroupRequirementsFrom: "(From <a target='_blank' rel='noopener noreferrer'>{GroupType}</a>)"
│   └── gGroupTypeGroupRequirements (light, no paging)
│       └── columns: GroupRequirementType.Name, GroupRole.Name
└── rcwGroupRequirements (editable, label flips between empty and "Specific Group Requirements")
    └── gGroupRequirements (light, no paging, ShowConfirmDeleteDialog=false)
        └── columns: GroupRequirementType.Name (Header "Name"), GroupRole (Header "Group Role"), AppliesToAgeClassification, lAppliesToDataViewId (custom literal field), MustMeetRequirementToAddMember (Header "Required For New Members"), GroupRequirementType.CanExpire, GroupRequirementType.RequirementCheckType (EnumField, Header "Type"), Edit, Delete

mdGroupRequirement (modal, ValidationGroup="vg_GroupRequirement")
├── nbDuplicateGroupRequirement (warning notification, hidden by default)
├── ddlGroupRequirementType (autopostback to ddlGroupRequirementType_SelectedIndexChanged → ShowDueDateQualifierControls)
├── grpGroupRequirementGroupRole (GroupRolePicker, label "Applies to Group Role", help "Select the group role that this requirement applies to. Leave blank if it applies to all group roles.")
├── rblAppliesToAgeClassification (radio, horizontal)
├── dvpAppliesToDataView (DataView picker, EntityTypeId=15 [Person], in col-md-6)
├── cbAllowLeadersToOverride (checkbox, "Allow Leaders to Override")
├── dpDueDate (visible iff RequirementType.DueDateType = ConfiguredDate)
├── ddlDueDateGroupAttribute (visible iff DueDateType = GroupAttribute, help "The group attribute that contains the due date for requirements.")
└── cbMembersMustMeetRequirementOnAdd (checkbox, "Members must meet this requirement before adding")
```

## UI surface

### Read-only "From {GroupType}" grid

| Control | Type | Notes |
|---|---|---|
| Header link | Literal `lGroupTypeGroupRequirementsFrom` | Server-rendered HTML at [GroupDetail.ascx.cs#L4552](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4552): `(From <a href='{ResolveUrl("~/GroupType/" + Id)}' target='_blank' rel='noopener noreferrer'>{Name}</a>)`. |
| Name | RockBoundField (`GroupRequirementType.Name`) | |
| Role | RockBoundField (`GroupRole.Name`) | |

### Editable grid

| Control | Type | Header | Notes |
|---|---|---|---|
| Name | RockBoundField (`GroupRequirementType.Name`) | "Name" | |
| Group Role | RockBoundField (`GroupRole`) | "Group Role" | The DataField references the entity, so default `.ToString()` is used. |
| Age Classification | RockBoundField (`AppliesToAgeClassification`) | "Age Classification" | Uses the enum's auto-stringification. |
| Data View | `Rock:RockLiteralField` `lAppliesToDataViewId` | "Data View" | Bound via `OnDataBound="lAppliesToDataViewId_OnDataBound"`. Renders `<i class='ti ti-check'></i>` if `AppliesToDataViewId.HasValue`, else empty ([GroupDetail.ascx.cs#L4049-L4057](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4049)). |
| Required For New Members | BoolField (`MustMeetRequirementToAddMember`) | | |
| Can Expire | BoolField (`GroupRequirementType.CanExpire`) | | |
| Type | EnumField (`GroupRequirementType.RequirementCheckType`) | "Type" | Enum: Manual / DataView / SQL. |
| Edit | EditField | | |
| Delete | DeleteField | | `ShowConfirmDeleteDialog="false"` on the grid. |

### Modal

| Control | Type | Notes |
|---|---|---|
| `nbDuplicateGroupRequirement` | NotificationBox (Warning) | Set to "This group already has a group requirement of {Name} for group role {Role.Name}" when a duplicate is detected. |
| `ddlGroupRequirementType` | RockDropDownList | Required. AutoPostBack triggers `ddlGroupRequirementType_SelectedIndexChanged` → `ShowDueDateQualifierControls`. Items populated from `GroupRequirementTypeService.Queryable().OrderBy(Name)` plus an empty first item. |
| `grpGroupRequirementGroupRole` | GroupRolePicker | `GroupTypeId` set to `CurrentGroupTypeId`. Optional. Help: "Select the group role that this requirement applies to. Leave blank if it applies to all group roles." |
| `rblAppliesToAgeClassification` | RockRadioButtonList (Horizontal) | Items from `AppliesToAgeClassification` enum (Adults, Children, All). Help: "Determines which age classifications this requirement applies to." Default `All`. |
| `dvpAppliesToDataView` | DataViewItemPicker | EntityTypeId hard-coded to 15 in markup (Person). Optional. Help: "An optional data view to determine who the requirement applies to." |
| `cbAllowLeadersToOverride` | RockCheckBox | "Allow Leaders to Override". Help: "Determines if the leader should be allowed to override meeting the requirement." |
| `dpDueDate` | DatePicker | Visible only when `DueDateType = ConfiguredDate`. Required when visible. |
| `ddlDueDateGroupAttribute` | RockDropDownList | Visible only when `DueDateType = GroupAttribute`. Required when visible. Items: see "DueDateGroupAttribute population" below. |
| `cbMembersMustMeetRequirementOnAdd` | RockCheckBox | "Members must meet this requirement before adding". Help: "If this is enabled, a person can only become a group member if this requirement is met. Note: only applies to Data View and SQL type requirements since manual ones can't be checked until after the person is added." |

## Code-behind summary

| Method | File:Line | Notes |
|---|---|---|
| `gGroupRequirements_Add` / `_Edit` / `_ShowEdit` | [GroupDetail.ascx.cs#L3930-L4042](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3930) | Builds requirement type ddl, sets due-date controls based on type, builds AppliesToAgeClassification options, sets DataView/role pickers. |
| `lAppliesToDataViewId_OnDataBound` | [GroupDetail.ascx.cs#L4049-L4057](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4049) | Renders check icon if DataView set. |
| `mdGroupRequirement_SaveClick` | [GroupDetail.ascx.cs#L4064-L4131](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4064) | Adds or updates the in-state requirement; checks for duplicate (RequirementType + GroupRole). |
| `gGroupRequirements_Delete` | [GroupDetail.ascx.cs#L4138-L4144](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4138) | Remove from state. |
| `BindGroupRequirementsGrid` | [GroupDetail.ascx.cs#L4541-L4565](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4541) | Splits into "from group type" (read-only) and "specific" (editable) grids. Add button visibility based on `EnableSpecificGroupRequirements`. |
| `ShowDueDateQualifierControls` | [GroupDetail.ascx.cs#L5064-L5107](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L5064) | Shows DueDate picker / DueDateGroupAttribute / neither based on `DueDateType`. |
| `ddlGroupRequirementType_SelectedIndexChanged` | [GroupDetail.ascx.cs#L5109-L5112](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L5109) | Calls ShowDueDateQualifierControls. |
| `LoadViewState` GroupRole rehydration | [GroupDetail.ascx.cs#L386-L397](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L386) | Re-attaches GroupRoles to GroupRequirements after deserialization (because `JsonConvert` skips navigation properties). |
| `btnSave_Click` removal | [GroupDetail.ascx.cs#L845-L850](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L845) | Removes any DB requirements no longer in state (where GroupId is the only ownership). |
| `btnSave_Click` add/update | [GroupDetail.ascx.cs#L873-L886, L1330-L1334](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L873) | Add/update via `groupRequirementsToInsert` queue (filled in pre-save, drained in WrapTransaction). |

## Server flow (Edit -> Save)

1. **Enter edit mode** ([`ShowEditDetails`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1933)):
   - Set `wpGroupRequirements.Visible = canAdministrate` ([line 2003](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2003)).
   - Load `GroupRequirementsState = group.GetGroupRequirements( rockContext ).Where( a => a.GroupId.HasValue ).ToList();` ([line 2066](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2066)).
   - Call `BindGroupRequirementsGrid()` ([line 2128](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2128)).
2. **GroupType change**: `BindGroupRequirementsGrid()` re-runs to refresh the read-only "From group type" grid for the new GroupType, and to flip Add-button visibility based on `EnableSpecificGroupRequirements`.
3. **Add / Edit modal** (`gGroupRequirements_ShowEdit`):
   - Build requirement-type ddl from `GroupRequirementTypeService.Queryable()` (all types, sorted by Name).
   - Build age-classification radio from enum.
   - Build DueDateGroupAttribute dropdown:
     - If `group == null` (new group): use `GroupDateAttributesState` (computed by [`BindInheritedAttributes`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3185-L3192), date-typed attributes from any GroupType in the inheritance chain).
     - Else: `group.LoadAttributes()` then filter to `DateFieldTypeIds.Contains(a.FieldTypeId)`. This includes attributes defined directly on the group's GroupType plus all inherited ancestors (because `LoadAttributes` resolves the chain).
   - Set `grpGroupRequirementGroupRole.GroupTypeId = CurrentGroupTypeId`. The picker walks the GroupType to retrieve roles.
   - On edit, populate fields from `selectedGroupRequirement` (in `GroupRequirementsState`).
   - On add, defaults: `AppliesToAgeClassification = All`, no role, `MustMeetRequirementToAddMember = false`.
   - `ShowDueDateQualifierControls()` toggles dpDueDate / ddlDueDateGroupAttribute.
4. **Save modal** (`mdGroupRequirement_SaveClick`):
   - Find existing in `GroupRequirementsState` by Guid; if not found, create new with fresh Guid and add to state.
   - Copy form values onto the entity, including reloading `GroupRequirementType` and `GroupRole` from DB (so display works in the grid).
   - Set `DueDateStaticDate` if `DueDateType == ConfiguredDate`.
   - Set `DueDateAttributeId` if `DueDateType == GroupAttribute` (looked up from `AttributeCache.AllForEntityType<Group>().Where(a => a.Id == ddl.SelectedValue.AsIntegerOrNull())`).
   - Duplicate check at [GroupDetail.ascx.cs#L4109-L4124](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4109): if any other requirement in state has the same `(GroupRequirementTypeId, GroupRoleId)`, show `nbDuplicateGroupRequirement`, **remove** the just-added entity from state, and return without calling `BindGroupRequirementsGrid` or `HideDialog`. The user sees the warning and can adjust.
   - Otherwise: hide notification, bind grid, hide dialog.
5. **Delete** (`gGroupRequirements_Delete`):
   - Remove from state by Guid + bind. No confirm dialog (`ShowConfirmDeleteDialog="false"`).
6. **Save group** (`btnSave_Click`):
   - **Pre-Save** (before `WrapTransaction`):
     - At [GroupDetail.ascx.cs#L845-L850](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L845): for each requirement in `group.GetGroupRequirements(rockContext).Where( a => a.GroupId.HasValue )` whose Guid is NOT in state, `groupRequirementService.Delete(req)`. Note: only group-owned ones, not group-type-owned.
     - At [GroupDetail.ascx.cs#L873-L886](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L873): for each in state, find by Guid in `group.GetGroupRequirements(...).Where( a => a.GroupId.HasValue )`. If found, `CopyPropertiesFrom(state)`. If not found, create new and add to local `groupRequirementsToInsert` queue.
   - **In `WrapTransaction`** ([line 1330-L1334](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1330)):
     ```csharp
     if ( groupRequirementsToInsert.Any() )
     {
         groupRequirementsToInsert.ForEach( a => a.GroupId = group.Id );
         groupRequirementService.AddRange( groupRequirementsToInsert );
     }
     ```

## DueDateType enum

`Rock.Enums.Group.DueDateType` (referenced in `Rock.Model.GroupRequirementType`):
| Value | UI behavior in modal |
|---|---|
| `Immediate` | dpDueDate hidden, ddlDueDateGroupAttribute hidden. |
| `DaysAfterJoining` | dpDueDate hidden, ddlDueDateGroupAttribute hidden. (Days are configured on the GroupRequirementType, not here.) |
| `ConfiguredDate` | dpDueDate **visible & required**, ddlDueDateGroupAttribute hidden. |
| `GroupAttribute` | dpDueDate hidden, ddlDueDateGroupAttribute **visible & required**. |

## Duplicate guard

A requirement is considered a duplicate if `(GroupRequirementTypeId, GroupRoleId)` matches another requirement in state. The save click rolls back the add and shows `nbDuplicateGroupRequirement`:

```
"This group already has a group requirement of {RequirementType.Name} for group role {GroupRole.Name}"
```

(message at [GroupDetail.ascx.cs#L4117-L4120](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4117); when `GroupRoleId` is null the second clause is omitted.)

## DueDateGroupAttribute population

When the modal opens, the dropdown is populated from:

- **If group is null/new**: use `GroupDateAttributesState` (computed in `BindInheritedAttributes`, walks the full chain, see [24-grouptype-inheritance.md](24-grouptype-inheritance.md)).
- **Otherwise**: `group.LoadAttributes()` then filter `group.Attributes.Select(a => a.Value).Where(a => DateFieldTypeIds.Contains(a.FieldTypeId))` ([GroupDetail.ascx.cs#L3979-L3984](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3979)). Because `LoadAttributes` already resolves inheritance, this list includes attributes from all ancestors.

`DateFieldTypeIds` is a static computed list of `FieldTypeCache.GetId(DATE)` and `FieldTypeCache.GetId(DATE_TIME)` ([GroupDetail.ascx.cs#L278-L289](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L278)).

## State

| Property | Type | Notes |
|---|---|---|
| `GroupRequirementsState` | `List<GroupRequirement>` | Full GroupRequirement entities. JSON-serialized to ViewState. After deserialize, GroupRoles are re-attached via lookup at [GroupDetail.ascx.cs#L386-L397](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L386). |
| `GroupDateAttributesState` | `List<Attribute>` | Date-typed attributes from the GroupType inheritance chain, used for the DueDateGroupAttribute dropdown when group is new. JSON-serialized to ViewState. |

## Visibility

```csharp
wpGroupRequirements.Visible = canAdministrate
                              && (groupTypeGroupRequirements.Any()
                                  || groupGroupRequirements.Any()
                                  || CurrentGroupTypeCache.EnableSpecificGroupRequirements);
gGroupRequirements.Actions.ShowAdd = CurrentGroupTypeCache.EnableSpecificGroupRequirements;
```

The "From {GroupType}" link wrapper is visible only if there are any group-type requirements:
```csharp
rcwGroupTypeGroupRequirements.Visible = groupTypeGroupRequirements.Any();
```

The editable grid wrapper is visible if either there are existing group-specific requirements OR the GroupType allows specific requirements:
```csharp
rcwGroupRequirements.Visible = CurrentGroupTypeCache.EnableSpecificGroupRequirements || groupGroupRequirements.Any();
```

(all from [GroupDetail.ascx.cs#L4541-L4555](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4541))

## Save logic specifics

```
For each GroupRequirement in DB.GetGroupRequirements(rockContext) where GroupId.HasValue and Guid not in state:
    Delete via service.

For each in state:
    Find by Guid in DB. If found, copy properties. If not, create new and add to insert queue.

After group is saved (so we have an Id):
    Set GroupId on each insert-queued, then service.AddRange.
```

## Persisted state (database)

Requirements live in the `[GroupRequirement]` table. Group-specific rows have `GroupId IS NOT NULL` and `GroupTypeId IS NULL`. Group-type-level rows have the inverse. The block only ever creates/updates/deletes group-specific (`GroupId`-owned) rows. GroupType-level rows are created in the GroupType detail block.

Columns written by Save:
- `GroupId` (set after group save)
- `GroupRequirementTypeId`
- `GroupRoleId` (nullable)
- `AppliesToAgeClassification`
- `AppliesToDataViewId` (nullable)
- `AllowLeadersToOverride`
- `MustMeetRequirementToAddMember`
- `DueDateStaticDate` (if ConfiguredDate)
- `DueDateAttributeId` (if GroupAttribute)
- `Guid`

## Edge cases

- **Duplicate detection includes role-null collisions**: `null == null` is true in C# `Equals`, so two role-null requirements of the same type collide.
- **Saving with a stale duplicate**: the duplicate-removal code at [GroupDetail.ascx.cs#L4122](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4122) **removes** the new entity from state via `GroupRequirementsState.Remove(groupRequirement)`. If editing (not adding), this leaves the state without the original entity, a subtle corruption. Confirm in spec phase whether the WebForms code intends to revert vs remove.
- **Switching DueDateType after setting a value**: if you set ConfiguredDate, pick a date, then change to Immediate, the underlying state's `DueDateStaticDate` keeps its value. Save flow only re-writes it when type matches. The DB row preserves stale data.
- **GroupTypeId on new group with no chosen GroupType**: `gGroupRequirements_Add` goes through `gGroupRequirements_ShowEdit(Guid.Empty)` which sets `grpGroupRequirementGroupRole.GroupTypeId = CurrentGroupTypeId`. If `CurrentGroupTypeId == 0`, the picker shows zero roles. The save flow does not reject this case, the requirement is saved with `GroupRoleId = null` automatically.
- **DataView selection persists across requirement-type changes**: if you set a DataView, then change the requirement type, the DataView remains. Save flow writes it regardless of whether the requirement type uses it.
- **No `IsAuthorized` check on the requirement type or DataView**: a user with ADMINISTRATE on the group can attach any requirement type/data view they can see in the dropdowns. The dropdowns show **all** requirement types regardless of permission.
- **Hard-coded EntityTypeId=15 on DataView picker**: this matches the Person entity. If Person's id ever changes (it won't, but defensively), this breaks. Convert to `EntityTypeCache.GetId(typeof(Person))` in Obsidian.
- **DueDateGroupAttribute filtered by FieldTypeId**: the dropdown shows only Date / DateTime field types. Other date-like field types (e.g., custom) are excluded.

## JS interactions

- Modal cancel uses `OnCancelScript="clearActiveDialog();"` to reset the active-dialog hidden field.
- The autopostback on `ddlGroupRequirementType` triggers `ddlGroupRequirementType_SelectedIndexChanged` server-side which calls `ShowDueDateQualifierControls`. In Obsidian this becomes a Vue computed property reacting to the requirement-type selection.

## Permission inheritance

| Scope | Auth |
|---|---|
| Panel visible | `group.IsAuthorized( ADMINISTRATE, CurrentPerson )` (block-level). |
| Add / Edit / Delete | Once panel is visible, no further auth check. |
| GroupRequirementType list | All types from `GroupRequirementTypeService.Queryable()` regardless of READ auth on the requirement type. |
| DataView selection | The DataView picker enforces the user's permissions on data views. |
| Save (final) | Implicit. |

## Phase considerations

Self-contained, but depends on:
- Group entity baseline (so the foreign key can resolve).
- Group attribute editor (so DueDateGroupAttribute can resolve to date attributes).

Good candidate for a mid-stage phase, after core edit + attributes.

## Open questions / flag for spec phase

- **Does the read-only "From group type" grid intentionally NOT walk inheritance?** WebForms queries `Where( a => a.GroupTypeId == CurrentGroupTypeId )` directly. If a parent GroupType has requirements, those are NOT shown here today. The runtime evaluation in `Group.GetGroupRequirements()` only joins to `this.GroupTypeId` as well ([Group.Logic.cs#L538](../../Rock/Model/Group/Group/Group.Logic.cs#L538)), so this is internally consistent. But ProductSec or design might want inherited group-type requirements to appear in this grid. **Confirm intent.**
- **Duplicate-edit corruption**: the `GroupRequirementsState.Remove(groupRequirement)` at [line 4122](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4122) likely should `RestorePropertiesFrom(originalState)` instead, when editing. Flag as a possible bug.
- **DueDateGroupAttribute filtered for FieldType ID set**: if other date-like field types ship in the future, the filter must be updated.
- **Hard-coded `EntityTypeId="15"` on `dvpAppliesToDataView` in markup**: convert to a server-resolved value in Obsidian.
- **`MustMeetRequirementToAddMember` semantics**: help text says "only applies to Data View and SQL type requirements". The save flow does not enforce this constraint. A user can check the box on a Manual-type requirement and the save succeeds. The runtime check (`GroupMember.Logic.cs#L337` etc) reads the flag for any type. Confirm whether the conversion should validate and reject.
