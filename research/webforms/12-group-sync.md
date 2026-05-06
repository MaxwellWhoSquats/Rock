# Sub-feature: Group Sync

## What it is

Group Sync periodically reconciles group membership against a Person DataView. Each sync row maps:
- A DataView (Person entity type)
- To a Role (within this group's group type)
- With a sync interval, and optional Welcome / Exit communications, and optional create-login behavior.

A scheduled Rock job ("Group Sync") runs and applies the rules.

## Trigger conditions

| Condition | Result |
|---|---|
| User does NOT have ADMINISTRATE on the group | `wpGroupSync.Visible = false` ([`2002`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2002)). |
| User has ADMINISTRATE AND `groupType.AllowGroupSync == false` AND no existing syncs | `wpGroupSync.Visible = false` ([`2195`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2195)). |
| User has ADMINISTRATE AND `groupType.AllowGroupSync == true` | Panel shown. Add button enabled. |
| User has ADMINISTRATE AND existing sync rows present | Panel shown regardless of `AllowGroupSync` (so admins can edit/delete legacy syncs). |

The visibility check runs twice with slightly different logic, which is a small inconsistency that the conversion can clean up.

## Markup region

[`RockWeb/Blocks/Groups/GroupDetail.ascx`](../../RockWeb/Blocks/Groups/GroupDetail.ascx) lines [`400-413`](../../RockWeb/Blocks/Groups/GroupDetail.ascx#L400) plus modal at [`633-672`](../../RockWeb/Blocks/Groups/GroupDetail.ascx#L633).

```
wpGroupSync (visible only with ADMINISTRATE; further gated by group type's AllowGroupSync)
└── gGroupSyncs grid
    Title (row item text): "Group Sync for Role"
    Columns:
      - GroupTypeRole.Name      header: "Role Name"
      - SyncDataView.Name       header: "Data View Name"
      - ScheduleTimeInterval    header: "Sync Interval"
      - LastRefreshDateTime     header: "Last Sync"   (DateTimeField)
      - Edit, Delete

mdGroupSyncSettings (modal)
  Title: "Group Sync Settings"
  ValidationGroup: "GroupSyncSettings"
  ├── dvipSyncDataView
  │     Label: "Sync Data View"
  │     Help:  "Select the Data View for the sync"
  │     Required: true
  │     EntityTypeId: Person (set in code-behind)
  ├── ddlGroupRoles
  │     Label: "Group Role to Assign"
  │     Help:  "Select the role to assign the members added by the selected Data View"
  │     Required: true
  ├── ipScheduleIntervalMinutes (IntervalPicker)
  │     Label: "Sync Interval"
  │     Help:  "Controls how often the group should sync to the Data View. It will never be less then the Group Sync job execution interval."
  │     DefaultValue: 12, DefaultInterval: Hour
  ├── ddlWelcomeCommunication
  │     Label: "Welcome Communication"
  ├── ddlExitCommunication
  │     Label: "Exit Communication"
  └── cbCreateLoginDuringSync
        Label: "Create Login During Sync"
        Help:  "If the individual does not have a login, should one be created during the sync process?"
```

## Code-behind

| Method | Lines | Notes |
|---|---|---|
| [`gGroupSyncs_Add`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4167) | 4167-4179 | ClearGroupSyncModal, CreateRoleDropDownList, CreateSystemCommunicationDropDownLists, EntityTypeId=Person. |
| [`gGroupSyncs_Edit`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4188) | 4188-4212 | ClearGroupSyncModal, set values from state. |
| [`gGroupSyncs_Delete`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4222) | 4222-4227 | Remove from state. |
| [`mdGroupSyncSettings_SaveClick`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4247) | 4247-4276 | Build/update GroupSyncViewModel; assign Guid; bind grid. |
| [`BindGroupSyncGrid`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4281) | 4281-4285 | Standard data bind. |
| [`CreateSystemCommunicationDropDownLists`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4291) | 4291-4313 | Loads all SystemCommunications (also populates `ddlRsvpReminderSystemCommunication`. Note the cross-feature population). |
| [`CreateRoleDropDownList`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4320) | 4320-4351 | Roles for the group type, excluding any role already synced (except the one currently being edited, if any). |
| [`ClearGroupSyncModal`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4356) | 4356-4363 | Reset controls. |
| `ShowEditDetails` | [`2006-2017`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2006) | Materializes `GroupSyncState` from `group.GroupSyncs`, including GroupTypeRole + SyncDataView lookups for grid display. |
| `btnSave_Click` body | [`861-867`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L861) | Remove DB syncs not in state. |
| `btnSave_Click` body | [`1011-1022`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1011) | Add/update from state. |

## GroupSyncViewModel

Lines [`5115-5124`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L5115). Extends `GroupSync` with:

```csharp
public TimeIntervalSetting ScheduleTimeInterval
{
    get { return new TimeIntervalSetting( ScheduleIntervalMinutes, null ); }
}
```

This is purely a display helper for the grid column.

## Save logic

```csharp
// Pre-transaction: remove any syncs not in state
var selectedGroupSyncs = GroupSyncState.Select( s => s.Guid );
foreach ( var groupSync in group.GroupSyncs.Where( s => !selectedGroupSyncs.Contains( s.Guid ) ).ToList() )
{
    group.GroupSyncs.Remove( groupSync );
    groupSyncService.Delete( groupSync );
}

// Pre-transaction: add/update
foreach ( var groupSyncState in GroupSyncState )
{
    GroupSync groupSync = group.GroupSyncs.Where( s => s.Guid == groupSyncState.Guid ).FirstOrDefault();
    if ( groupSync == null )
    {
        groupSync = new GroupSync();
        group.GroupSyncs.Add( groupSync );
    }
    groupSync.CopyPropertiesFrom( groupSyncState );
}
```

The role-dropdown logic prevents duplicates at the picker level by excluding already-synced roles. There is no separate duplicate check at save.

## Modal save flow (`mdGroupSyncSettings_SaveClick`)

```csharp
groupSync.GroupId = hfGroupId.ValueAsInt();
groupSync.GroupTypeRoleId = ddlGroupRoles.SelectedValue.AsInteger();
groupSync.GroupTypeRole = new GroupTypeRoleService( rockContext ).Get( groupSync.GroupTypeRoleId );
groupSync.SyncDataViewId = dvipSyncDataView.SelectedValueAsInt() ?? 0;
groupSync.SyncDataView = new DataViewService( rockContext ).Get( groupSync.SyncDataViewId );
groupSync.ExitSystemCommunicationId = ddlExitCommunication.SelectedValue.AsIntegerOrNull();
groupSync.WelcomeSystemCommunicationId = ddlWelcomeCommunication.SelectedValue.AsIntegerOrNull();
groupSync.AddUserAccountsDuringSync = cbCreateLoginDuringSync.Checked;
groupSync.ScheduleIntervalMinutes = ipScheduleIntervalMinutes.IntervalInMinutes;
```

The full GroupTypeRole and SyncDataView entities are looked up so the grid's display columns work without an extra query on rebind.

## Persisted state

| Entity | Fields written | Cascade |
|---|---|---|
| `GroupSync` | GroupId, GroupTypeRoleId, SyncDataViewId, WelcomeSystemCommunicationId, ExitSystemCommunicationId, AddUserAccountsDuringSync, ScheduleIntervalMinutes | Standard FK behavior. Removed via `GroupSyncService.Delete`. `LastRefreshDateTime` is not editable from this UI; set by the sync job. |

GroupSync FK relationships:
- `GroupId` -> `Group.Id` (cascade delete is handled by the parent group's removal).
- `GroupTypeRoleId` -> `GroupTypeRole.Id` (no cascade. If a role is deleted, the sync row points to a missing role).
- `SyncDataViewId` -> `DataView.Id`.
- `WelcomeSystemCommunicationId` and `ExitSystemCommunicationId` -> `SystemCommunication.Id` (nullable).

## State

`GroupSyncState : List<GroupSyncViewModel>`. JSON-serialized into ViewState during postbacks.

## Visibility

```csharp
// In ShowEditDetails (line 2002):
wpGroupSync.Visible = canAdministrate;

// Then in ShowGroupTypeEditDetails (overrides at line 2195):
wpGroupSync.Visible = (selectedGroupType.IsAuthorized(ADMINISTRATE, person)
                       && (selectedGroupType.AllowGroupSync || GroupSyncState.Any()));
```

The visibility is checked twice with slightly different criteria, which is a small inconsistency that the conversion can clean up.

## Validation

The role dropdown excludes already-synced roles to prevent duplicates at the picker level (no separate duplicate check needed in save). When editing an existing sync, the current role is preserved by passing `roleId` to `CreateRoleDropDownList` so it isn't filtered out.

## Permission inheritance

`wpGroupSync.Visible` requires both:
1. `Authorization.ADMINISTRATE` on the group, AND
2. Either `GroupType.AllowGroupSync == true` OR existing `GroupSync` rows.

The modal is also gated by ADMINISTRATE (it would not be reachable without the panel being visible). No additional per-modal auth check.

## Side effects

- Removing a GroupSync only takes effect on save; the row stays in `group.GroupSyncs` but the state list does not include it.
- The Add and Update both write `LastRefreshDateTime` is NOT cleared. This is fine because the Group Sync job will pick up the new sync on its next run regardless.

## Phase considerations

Self-contained, depends on Group baseline. Could be its own phase or bundled with another sub-feature. The DataView picker is a heavy dependency and may benefit from its own complete sub-spec.

## Open questions / flag for spec phase

- The duplicate visibility check (`ShowEditDetails` then `ShowGroupTypeEditDetails`) can collapse to one decision in the new block.
- Consider whether ChangeWelcome/Exit communications should be filtered by category (currently shows all SystemCommunications). The legacy block does no filter.
- The `CreateSystemCommunicationDropDownLists` method also populates `ddlRsvpReminderSystemCommunication`. This is a hidden cross-feature coupling that the new block should disentangle.

## Notes

- `IntervalInMinutes` on `IntervalPicker` returns minutes. Default is 12 hours = 720 minutes.
- `GroupSync.LastRefreshDateTime` is set by the sync job, not this UI.
