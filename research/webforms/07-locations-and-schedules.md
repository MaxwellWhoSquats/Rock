# Sub-feature: Locations and Schedules

## Overview

The Meeting Details panel manages two related but distinct concerns:

1. `GroupLocation` rows. Physical/virtual places where the group meets, with optional schedule(s) attached.
2. The group's "primary" `Schedule`. An inline weekly, custom (iCal), or named schedule attached directly to `Group.ScheduleId`.

Plus, when the group type has scheduling enabled, each `GroupLocation` x `Schedule` pair has a `GroupLocationScheduleConfig` row holding capacity (Min, Desired, Max).

This is the most state-heavy sub-feature in the block.

## Markup region

[`RockWeb/Blocks/Groups/GroupDetail.ascx`](../../RockWeb/Blocks/Groups/GroupDetail.ascx) lines [`211-247`](../../RockWeb/Blocks/Groups/GroupDetail.ascx#L211) (panel widget) plus the location dialog at [`517-600`](../../RockWeb/Blocks/Groups/GroupDetail.ascx#L517).

## Trigger conditions for visibility

| Condition | Result |
|---|---|
| `groupType.LocationSelectionMode != GroupLocationPickerMode.None` | `wpMeetingDetails` and `gGroupLocations` shown ([`2245-2255`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2245)). |
| `groupType.LocationSelectionMode == None` AND `IsScheduleTabVisible == true` | `wpMeetingDetails` shown but `gGroupLocations` hidden. |
| `groupType.LocationSelectionMode == None` AND no schedule types allowed | `wpMeetingDetails` hidden entirely. |
| `groupType.EnableLocationSchedules == true` | `gGroupLocations.Columns[2]` (Schedules column) and `spSchedules` visible ([`2257-2258`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2257)). |
| `groupType.IsSchedulingEnabled == true` | `rcwGroupLocationScheduleCapacities` visible inside the location dialog ([`3607`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3607)). |
| `(groupType.AllowedScheduleTypes & X) == X` for X in {Weekly, Custom, Named} | Corresponding radio option appears in `rblScheduleSelect`. `pnlSchedule.Visible = IsScheduleTabVisible = true`. |

Note: lines [`2245-2273`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2245) contain a code duplication where the same visibility logic runs twice. Behavior is unaffected; conversion can collapse to one block.

## Locations grid

`gGroupLocations` columns:
- Location (string)
- Type (string)
- Schedule(s) (string, comma-separated)
- Edit + Delete

Add button visibility: `AllowMultipleLocations || !GroupLocationsState.Any()` ([`3872`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3872)).

`AllowMultipleLocations` is a per-postback ViewState boolean that mirrors `GroupType.AllowMultipleLocations` ([`2182`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2182)).

## Locations dialog (`dlgLocations`)

Two tabs ([`526-535`](../../RockWeb/Blocks/Groups/GroupDetail.ascx#L526)):
- **Member Location**: dropdown of `{Member} {AddressType} ({Address})` for each Group Member's family addresses. Excludes Previous-type. Format `"{LocationId}|{PersonId}"`.
- **Other Location**: full LocationPicker with mode set per group type.

Below tabs ([`546-597`](../../RockWeb/Blocks/Groups/GroupDetail.ascx#L546)):
- **Type**: defined-value dropdown scoped to `groupType.LocationTypeValues`.
- **Schedule(s)**: SchedulePicker (multi-select, `AllowMultiSelect="true"`, `AllowInactiveSelection="false"`). Visible only if `groupType.EnableLocationSchedules`.
- **Capacities** (`rcwGroupLocationScheduleCapacities`): repeater with one row per selected schedule. Min/Desired/Max number boxes. Visible only if `groupType.IsSchedulingEnabled`.

The dialog title is `"Group Location"` and the save button is labeled `"Ok"`. Validation group is `"Location"`.

### Member-tab data flow

Source: [`gGroupLocations_ShowEdit`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3525) lines 3525-3549.

```csharp
foreach ( GroupMember member in new GroupMemberService(rockContext).GetByGroupId( groupId ) )
{
    foreach ( Group family in personService.GetFamilies( member.PersonId ) )
    {
        foreach ( GroupLocation familyGroupLocation in family.GroupLocations
            .Where( l => l.IsMappedLocation
                      && !l.GroupLocationTypeValue.Guid.Equals( previousLocationType ) ) )
        {
            ListItem li = new ListItem(
                $"{member.Person.FullName} {familyGroupLocation.GroupLocationTypeValue.Value} ({familyGroupLocation.Location})",
                $"{familyGroupLocation.Location.Id}|{member.PersonId}" );

            ddlMember.Items.Add( li );
        }
    }
}
```

Notes:
- One row per (member x family x address). A member with two families each having Home + Work = four rows.
- Excludes any address with `GroupLocationType.Guid == GROUP_LOCATION_TYPE_PREVIOUS`.
- Excludes any non-mapped (`IsMappedLocation == false`) addresses.
- Value format `"{LocationId}|{PersonId}"` is parsed in `dlgLocations_OkClick` at [`3716-3729`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3716). The PersonId is converted to a `PrimaryAliasId` via `PersonAliasService.GetPrimaryAliasId(personId)` and stored on `GroupLocation.GroupMemberPersonAliasId`.

### Other-tab data flow

`locpGroupLocation` is a `LocationPicker` whose `AllowedPickerModes` are derived from the group type's `LocationSelectionMode` flags ([`3496-3516`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3496)):

| `GroupLocationPickerMode` flag | `LocationPickerMode` flag |
|---|---|
| `Named` | `Named` |
| `Address` | `Address` |
| `Point` | `Point` |
| `Polygon` | `Polygon` |
| `GroupMember` | (drives the Member tab, not LocationPicker mode) |

The picker's `MapStyleValueGuid` is set from the `MapStyle` block attribute ([`3567`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3567)). On select, `locpGroupLocation_SelectLocation` runs the duplicate-add detection.

### Tab visibility logic

```csharp
bool displayMemberTab = (groupTypeModes & GroupLocationPickerMode.GroupMember) == GroupLocationPickerMode.GroupMember;
bool displayOtherTab = modes != LocationPickerMode.None;

ulNav.Visible = displayOtherTab && displayMemberTab;   // both available -> show pill nav
pnlMemberSelect.Visible = displayMemberTab;
pnlLocationSelect.Visible = displayOtherTab && !displayMemberTab; // start on Other if Member not available
```

If both tabs are available and `ddlMember.Items.Count > 0`, default tab is Member; otherwise Other ([`3601`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3601)).

## Code-behind region

| Method | Lines | Notes |
|---|---|---|
| [`gGroupLocations_Add`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3449) | 3449-3453 | hfAction = "Add". |
| [`gGroupLocations_Edit`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3460) | 3460-3465 | hfAction = "Edit". |
| [`gGroupLocations_ShowEdit`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3471) | 3471-3649 | Builds member-tab options, populates location-type defined values, pre-selects schedules, builds capacity repeater. |
| [`rptGroupLocationScheduleCapacities_ItemDataBound`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3656) | 3656-3677 | Per-row capacity input population. |
| [`gGroupLocations_Delete`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3684) | 3684-3689 | Removes from `GroupLocationsState`. |
| [`dlgLocations_OkClick`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3706) | 3706-3848 | Resolves selected location, finds or creates GroupLocation in state, rebuilds capacities from repeater, reconciles selected-vs-inactive schedule ids. |
| [`ExistingLocationOnAdd`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3855) | 3855-3865 | Detects duplicate add by Name+Guid. |
| [`BindGroupLocationsGrid`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3870) | 3870-3886 | Materializes `GridLocation` projection. |
| [`ResetLocationDialog`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3891) | 3891-3899 | Clears LocationPicker, schedules, repeater, edit message. |
| [`locpGroupLocation_SelectLocation`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3907) | 3907-3919 | "Already exists" guard. |
| [`spSchedules_SelectItem`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3294) | 3294-3348 | Rebuilds capacity repeater on schedule change, preserving in-progress edits. |
| [`BindGroupLocationScheduleCapacities`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3354) | 3354-3363 | Orders by next start. |
| [`LocationSelected`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3369) | 3369-3373 | True if LocationPicker has a Location. |
| [`lbLocationType_Click`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1634) | 1634-1646 | Member/Other tab switch. |

## Save logic for GroupLocations

`btnSave_Click` lines [`810-991`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L810). Algorithm:

```
For each existing GroupLocation NOT in state (i.e., user removed):
    For each GroupLocationScheduleConfig: queue for removal.
    For each schedule: find GroupMemberAssignments matching (scheduleId, locationId, groupId), DeleteRange.
    Remove GroupLocation from group.GroupLocations.
    groupLocationService.Delete(groupLocation).
    Set checkinDataUpdated = true.

For each GroupLocation in state:
    If exists in DB: copy Guid + Id from existing; if location changed, delete GroupMemberAssignments for old location.
    Otherwise: create new and add to group.GroupLocations.
    Remove schedules no longer in state.
    CopyPropertiesFrom(state).
    Reconcile GroupLocationScheduleConfigs:
        Add new (any in state but not in existing).
        Update modified (any with different capacity values).
        Remove deleted (any in deletedSchedules list).
    Set checkinDataUpdated = true.
```

### GroupLocationScheduleConfig diff (existing vs modified vs new vs deleted)

Implemented at [`942-988`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L942).

| Bucket | Source | Determined by |
|---|---|---|
| **Existing schedules** (from DB) | `groupLocation.Schedules` | `existingSchedules = groupLocation.Schedules.Select(s => s.Guid)`. |
| **Modified configs** | `groupLocationState.GroupLocationScheduleConfigs` | Same `(ScheduleId, GroupLocationId)` exists in DB but Min/Desired/Max differs. |
| **New configs** | `groupLocationState.GroupLocationScheduleConfigs` | `ScheduleId` not present in `existingGroupLocationConfigs`. |
| **Deleted configs** | DB-only | Schedule was removed from `groupLocation.Schedules` (collected into `deletedSchedules` list). |

```csharp
// Get existing configurations with modified capacity values.
var modifiedScheduleConfigs = groupLocationState.GroupLocationScheduleConfigs
    .Where( s => groupLocation.GroupLocationScheduleConfigs
        .Where( exs => ( exs.ScheduleId == s.ScheduleId )
            && exs.GroupLocationId == s.GroupLocationId
            && ( exs.MinimumCapacity != s.MinimumCapacity
            || exs.DesiredCapacity != s.DesiredCapacity
            || exs.MaximumCapacity != s.MaximumCapacity ) ).Any() )
    .ToList();

// Add new scheduling configurations.
var newGroupLocationScheduleConfigs = groupLocationState.GroupLocationScheduleConfigs
    .Where( s => !existingGroupLocationConfigs.Any( a => a.ScheduleId == s.ScheduleId ) )
    .ToList();
```

Each new config is materialized with only `ScheduleId, MinimumCapacity, DesiredCapacity, MaximumCapacity`. Each modified config writes the three capacity fields back. Each deleted config is removed via `groupLocation.GroupLocationScheduleConfigs.Remove(associatedConfig)`.

### Duplicate-location detection on Add

[`ExistingLocationOnAdd`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3855) at lines 3855-3865:

```csharp
private bool ExistingLocationOnAdd( Location selectedLocation )
{
    if ( hfAction.Value == "Add" && selectedLocation != null )
    {
        List<GridLocation> existingLocations = gGroupLocations.DataSourceAsList as List<GridLocation>;

        return existingLocations
            .Where( x => x.Location.Name == selectedLocation.Name
                      && x.Location.Guid == selectedLocation.Guid )
            .Any();
    }

    return false;
}
```

When a duplicate is detected on the Other tab, [`locpGroupLocation_SelectLocation`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3907) clears the picker and shows `nbGroupLocationEditMessage` with the text:
```
{LocationName} already exists in meeting details and can not be selected again.
```

The Member tab does not run duplicate detection because each `(LocationId, PersonId)` pair is unique by construction.

### Save reconciliation in dlgLocations_OkClick

The dialog save handler at [`3706-3848`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3706) does NOT persist to the database. It updates `GroupLocationsState` (held in ViewState) only:

1. Resolve `location` from current tab:
   - **Member tab**: parse `"locId|personId"` from `ddlMember.SelectedValue`. Look up `Location` by id, copy properties (decoupling). Look up `PrimaryAliasId` via `PersonAliasService.GetPrimaryAliasId(personId)` and store as `memberPersonAliasId`.
   - **Other tab**: take `locpGroupLocation.Location` and copy properties.
2. Find or create the `GroupLocation` in state:
   - If `hfAddLocationGroupGuid` matches an existing state row, edit it.
   - Otherwise create a new `GroupLocation` and append; `Order = currentMax + 1`.
3. For each repeater item in `rptGroupLocationScheduleCapacities`, read Min/Desired/Max and either update the existing `GroupLocationScheduleConfig` or add a new one to the GroupLocation.
4. Set `groupLocation.GroupMemberPersonAliasId = memberPersonAliasId`, `groupLocation.Location = location`, `groupLocation.LocationId = location.Id`, `groupLocation.GroupLocationTypeValueId = ddlLocationType.SelectedValueAsId()`.
5. Reconstruct `groupLocation.Schedules` as the union of `selectedIds` and `inactiveSchedulesIds` (so that previously-attached but now-inactive schedules are NOT silently dropped on save).
6. Lookup `groupLocation.GroupLocationTypeValue` (DefinedValue) and copy properties so the grid display has a name.
7. If `location == null` and we were editing, remove the `GroupLocation` from state (treats "blank everything and save" as delete).
8. `BindGroupLocationsGrid()`, clear `spSchedules`, `HideDialog()`.

### Active vs inactive schedule reconciliation

Stored in hidden field `hfInactiveGroupLocationSchedules` (comma-delimited list of Schedule.Id values that are attached to the location but currently `Schedule.IsActive == false`).

Populated on edit-show ([`3591-3594`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3591)):

```csharp
var activeSchedules = groupLocation.Schedules.Where( s => s.IsActive );
var inactiveScheduleIds = groupLocation.Schedules.Where( s => !s.IsActive ).Select( s => s.Id ).ToList();
spSchedules.SetValues( activeSchedules );
hfInactiveGroupLocationSchedules.Value = inactiveScheduleIds.AsDelimited( "," );
```

Re-merged on save ([`3810-3820`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3810)):

```csharp
var selectedIds = spSchedules.SelectedValuesAsInt();
var inactiveSchedulesIds = ...; // parsed from hfInactiveGroupLocationSchedules

groupLocationSchedules = scheduleService.Queryable()
    .Where( s => selectedIds.Contains( s.Id ) || inactiveSchedulesIds.Contains( s.Id ) )
    .ToList();
groupLocation.Schedules = groupLocationSchedules;
```

This ensures a schedule that was deactivated globally but is still meaningful to this group location stays attached until the admin explicitly removes it.

## Inline (group's primary) Schedule

The "Group Schedule" radio (`rblScheduleSelect`) lets the user pick one of:
- **None** (always available): clears `group.ScheduleId`.
- **Weekly**: shows DayOfWeek + Time pickers (`dowWeekly`, `timeWeekly`).
- **Custom**: shows ScheduleBuilder (`sbSchedule`, iCalendar).
- **Named**: shows SchedulePicker (`spSchedule`, single-select).

Visibility per radio option is determined by `groupType.AllowedScheduleTypes` flags ([`2339-2361`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2339)).

The visibility of the entire schedule sub-panel (`pnlSchedule`) is driven by `IsScheduleTabVisible` (true if any of Weekly/Custom/Named is allowed by group type).

### Inline schedule entity management

The save logic at [`1184-1252`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1184) implements a "create new vs reuse vs delete" pattern. Key constants:

- `Schedule.Name = string.Empty` is the convention used to mark a schedule as inline (Custom or Weekly), distinguishing it from Named schedules which always have a non-empty name.
- `hfUniqueScheduleId` (HiddenField) tracks the Schedule.Id of the currently-attached inline schedule before the user changes anything. Set during `SetScheduleControls` at [`2320`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2320) and [`2324`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2324).

Flow:

```csharp
var scheduleType = rblScheduleSelect.SelectedValueAsEnum<ScheduleType>( ScheduleType.None );

// Force-fall-back if Custom/Weekly user selection is incomplete.
if ( scheduleType == ScheduleType.Custom )
{
    iCalendarContent = sbSchedule.iCalendarContent;
    var calEvent = InetCalendarHelper.CreateCalendarEvent( iCalendarContent );
    if ( calEvent == null || calEvent.DtStart == null )
    {
        scheduleType = ScheduleType.None;
    }
}

if ( scheduleType == ScheduleType.Weekly )
{
    if ( !dowWeekly.SelectedDayOfWeek.HasValue )
    {
        scheduleType = ScheduleType.None;
    }
}

int? oldScheduleId = hfUniqueScheduleId.Value.AsIntegerOrNull();

if ( scheduleType == ScheduleType.Custom || scheduleType == ScheduleType.Weekly )
{
    // Create new Schedule if no existing inline one. Reuse otherwise.
    if ( !oldScheduleId.HasValue || group.Schedule == null )
    {
        group.Schedule = new Schedule();
        group.Schedule.Name = string.Empty; // marks it as inline
    }

    if ( scheduleType == ScheduleType.Custom )
    {
        group.Schedule.iCalendarContent = iCalendarContent;
        group.Schedule.WeeklyDayOfWeek = null;
        group.Schedule.WeeklyTimeOfDay = null;
    }
    else
    {
        group.Schedule.iCalendarContent = null;
        group.Schedule.WeeklyDayOfWeek = dowWeekly.SelectedDayOfWeek;
        group.Schedule.WeeklyTimeOfDay = timeWeekly.SelectedTime;
    }
}
else
{
    // None or Named selected.
    // If group HAD an inline schedule, try to delete it.
    if ( oldScheduleId.HasValue )
    {
        var schedule = scheduleService.Get( oldScheduleId.Value );
        if ( schedule != null && string.IsNullOrEmpty( schedule.Name ) )
        {
            // Make sure this is the only thing using this schedule.
            string errorMessage;
            if ( scheduleService.CanDelete( schedule, out errorMessage ) )
            {
                scheduleService.Delete( schedule );
            }
        }
    }

    if ( scheduleType == ScheduleType.Named )
    {
        group.ScheduleId = spSchedule.SelectedValueAsId();
    }
    else
    {
        group.ScheduleId = null;
    }
}
```

Key edge cases:
- An inline Schedule is only deleted if `Name == string.Empty` AND `CanDelete` returns true. `CanDelete` checks for any other entity that references this Schedule (e.g., another group, attendance, etc.). If anything else still references it, we leave the row in the database orphaned to this group's ScheduleId clear.
- If user picks Custom but the iCal is empty/invalid, the type silently downgrades to None and the existing inline schedule is deleted (if no other consumer).
- When switching between Custom and Weekly, the existing inline Schedule is reused. The other-mode fields are nulled (e.g., switching Weekly -> Custom nulls `WeeklyDayOfWeek` and `WeeklyTimeOfDay`).
- When switching to Named, the inline Schedule is deleted and `ScheduleId` is set to the named schedule's Id.

### Inline schedule deletion logic on group delete

[`btnDelete_Click`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L700) at lines 700-713 mirrors the same pattern:

```csharp
// If group has a non-named schedule, delete the schedule record.
if ( group.ScheduleId.HasValue )
{
    var scheduleService = new ScheduleService( rockContext );
    var schedule = scheduleService.Get( group.ScheduleId.Value );
    if ( schedule != null && schedule.ScheduleType != ScheduleType.Named )
    {
        // Make sure this is the only group trying to use this schedule.
        if ( !groupService.Queryable().Where( g => g.ScheduleId == schedule.Id && g.Id != group.Id ).Any() )
        {
            scheduleService.Delete( schedule );
        }
    }
}
```

The delete-time check uses `ScheduleType != Named` rather than `Name == empty`, but functionally equivalent. The check also restricts the query to other Groups (not all consumers), which is slightly less strict than the save-time `CanDelete`.

### State stored in ViewState (carries across postbacks)

- `GroupLocationsState : List<GroupLocation>`. Full GroupLocation entities including Schedules and GroupLocationScheduleConfigs.
- `LocationTypeTab : string`. Which dialog tab is active.
- `AllowMultipleLocations : bool`. Mirrors group type setting; drives Add button.
- `hfUniqueScheduleId` (HiddenField). Id of the inline schedule before user changes.
- `hfInactiveGroupLocationSchedules` (HiddenField). Comma-delimited Ids of schedules attached but currently inactive (kept across saves so admins don't accidentally drop them).
- `hfGroupLocationGuid`, `hfAddLocationGroupGuid`, `hfAction`. Dialog state.

## Persisted state

| Entity | Fields written | Cascade |
|---|---|---|
| `GroupLocation` | LocationId, GroupLocationTypeValueId, GroupMemberPersonAliasId, Order | Standard FK behavior. Removed via `GroupLocationService.Delete`. |
| `GroupLocationScheduleConfig` | ScheduleId, MinimumCapacity, DesiredCapacity, MaximumCapacity | Owned by GroupLocation. Removed via collection `.Remove()`. |
| `GroupLocation.Schedules` (M:M) | Schedule references | Pure many-to-many; removing detaches but does not delete schedules. |
| `Schedule` (inline) | Name=empty, iCalendarContent OR WeeklyDayOfWeek+WeeklyTimeOfDay | Created on the fly. Deleted via `ScheduleService.Delete` only if no other consumer (per `CanDelete`). |
| `Schedule` (named) | Referenced via `Group.ScheduleId` | Never deleted by this block. |
| `GroupMemberAssignment` | (deleted) | Cascading cleanup when location/schedule combo is removed. |

## Side effects

- Removing a location/schedule combination cascades to `GroupMemberAssignment` deletions ([`829-837`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L829), [`906-917`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L906)).
- Adding/removing/updating any location sets `checkinDataUpdated = true`. After save, this flushes `Rock.CheckIn.KioskDevice.Clear()` if the group type takes attendance ([`1432-1436`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1432)).
- Deleting an inline (non-named) schedule is gated on `scheduleService.CanDelete` (save flow) or "no other group uses it" (delete flow).

## Permission inheritance

- Panel visibility follows the standard EDIT-on-group rules.
- The Add button further requires either no existing locations OR `AllowMultipleLocations = true`.
- Schedule editing has no separate permission; it falls under EDIT-on-group.

## JS interactions specific to this sub-feature

- `clearActiveDialog()` (top of `.ascx`) clears `hfActiveDialog` when a modal cancels.
- The `pdAuditDetails`, ScheduleBuilder, LocationPicker, SchedulePicker, and DayOfWeekPicker controls have their own embedded scripts. None are bespoke to GroupDetail.

## Phase considerations

This is the most database-heavy sub-feature and has nuanced cascade logic. It splits into two related but distinct concerns:

1. **Inline Group Schedule** (Group.ScheduleId + Schedule entity).
2. **GroupLocations + Schedules + Capacities** (a 3-way join).

Either could be its own phase, but they are likely best handled together because they share a UI panel and the `IsScheduleTabVisible` interlock.

## Open questions / flag for spec phase

- The Member-tab dropdown today populates one row per member-per-family-address. For a group with hundreds of members, this can be slow. Consider lazy-loading or typeahead in Obsidian.
- The `GridLocation` projection ([`247-258`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L247)) collapses Schedules to a comma-delimited string. Obsidian could surface schedules as a richer cell.
- The dual UI for inline schedule vs location-attached schedules is confusing for end users. Worth a UX revisit during redesign.
- The `Schedule.Name = string.Empty` convention is fragile. Consider whether the new block should use `ScheduleType` directly to detect inline-vs-named.
- The duplicate-detection compares both Name AND Guid, but Guid alone would be sufficient. Confirm the redundancy is intentional or simplify.
- The capacity repeater currently re-renders on every schedule change, losing any in-progress edits to non-shared capacity rows. The spSchedules_SelectItem handler tries to preserve them by reading from the repeater first, but timing is fragile. Vue will need an explicit reactive model.
