# Block States and Conditional UI

This document enumerates every block state and the conditional UI branches it drives. The Obsidian conversion's bag must communicate enough state for the Vue layer to make the same decisions.

## High-level state machine

```
                ┌─────────────────────┐
                │    GroupId param    │
                └──────────┬──────────┘
                           │
        ┌──────────────────┼──────────────────┐
        │                  │                  │
        ▼                  ▼                  ▼
   GroupId=0          GroupId=N          GroupId not set
   (Add new)        (existing)            (block hidden)
        │                  │
        │           ┌──────┴──────┐
        │           ▼             ▼
        │      group found    group not found
        │           │             │
        │           │             ▼
        │           │      "not found or archived" notification
        │           │
        │           ▼
        │      ┌──────────────────────────┐
        │      │  authorize VIEW + EDIT   │
        │      └────────┬─────────────────┘
        │               │
        │       ┌───────┼────────┐
        │       ▼       ▼        ▼
        │   no view   readonly   editable
        │   (hidden)  (view)    │
        │                       │
        │               ┌───────┴────────┐
        │               ▼                ▼
        │        autoEdit=true        default
        │        → edit mode          → readonly mode
        │
        └─────► edit mode for new group
```

Once in edit mode, the form's contents depend on the **GroupType** in scope, which can change mid-edit via `ddlGroupType_SelectedIndexChanged`. That re-runs almost all of `ShowGroupTypeEditDetails`, `SetRecordSourceControls`, `SetPeerNetworkControls`, `SetRsvpControls`, `SetScheduleControls`, `BindInheritedAttributes`, `BindGroupRequirementsGrid`, `BindAdministratorPerson`, `SetChatControls`.

## Authorization-driven states

| State | Trigger | Effect |
|---|---|---|
| ViewAllowed = false | User has neither EDIT nor VIEW on the group | `pnlDetails.Visible = false` (block disappears) |
| EditAllowed = false (View only) | User has VIEW but not EDIT | Read-only view; Edit/Delete/Archive/Copy buttons hidden; `nbEditModeMessage` shows "read-only" message |
| IsSystem | `group.IsSystem == true` | Edit mode shows "System" message; Delete/Archive hidden; Chat dropdowns disabled |
| IsArchived | `group.IsArchived == true` | hlArchived label visible; Delete button hidden |
| Authorize ADMINISTRATE = false | User can EDIT but not ADMINISTRATE | wpGroupSync, wpGroupRequirements, wpGroupMemberAttributes panels hidden; Security button hidden |
| Not in GROUP_ADMINISTRATORS | Current user not in that system group | IsSecurityRole checkbox is hidden |

## GroupType-driven states (edit panel)

The block's primary conditional surface. All driven by `GroupTypeCache` properties (and the in-progress `CurrentGroupTypeId`).

| GroupType property | UI effect |
|---|---|
| `IsPeerNetworkEnabled` | Show pnlPeerNetworkOverride, hlPeerNetwork label |
| `EnableRSVP` | Show wpRsvp, hlGroupRSVP quick-link |
| `LocationSelectionMode != None` | Show wpMeetingDetails (always visible if `IsScheduleTabVisible`) |
| `EnableLocationSchedules` | Show Schedule(s) column on locations grid + spSchedules picker in dialog |
| `AllowMultipleLocations` | Locations grid Add button visible when no rows OR multiple allowed |
| `LocationSelectionMode` flags | Determine which LocationPicker modes are enabled |
| `LocationTypeValues` | Populate the location-type defined-value dropdown in the dialog |
| `IsSchedulingEnabled` | Show wpScheduling, hlGroupScheduler quick-link, scheduling sub-controls in locations dialog |
| `AllowedScheduleTypes` flags (Weekly\|Custom\|Named) | Which radio options appear under Meeting Details schedule selector |
| `EnableInactiveReason` | Show Inactive Reason dropdown |
| `RequiresInactiveReason` | Make Inactive Reason required |
| `EnableGroupHistory` | Show Archive button (instead of Delete) IF history rows exist |
| `EnableGroupTag` | Show TagList control (combined with EnableGroupTags block attribute) |
| `ShowAdministrator` | Show Administrator person picker (label uses `AdministratorTerm`) |
| `GroupCapacityRule` | Show Capacity number box, set required, set help text |
| `IsCapacityRequired` | Capacity required when GroupCapacityRule != None |
| `GroupStatusDefinedTypeId` | Show Status dropdown (label = DefinedType name) |
| `AllowGroupSpecificRecordSource` | Show RecordSource override dropdown |
| `AllowSpecificGroupMemberAttributes` | Allow adding custom group-member attributes |
| `AllowGroupSync` | Allow adding group syncs (combined with ADMINISTRATE auth) |
| `AllowSpecificGroupMemberWorkflows` | Show wpMemberWorkflowTriggers |
| `EnableSpecificGroupRequirements` | Allow adding group-specific requirements |
| `IsChatAllowed` (+ `ChatHelper.IsChatEnabled`) | Show wpChat |
| `GroupsRequireCampus` | If set and no Campus selected, Save defaults to single-campus Id |
| `Roles` | Populates ddlGroupRoles, ddlTriggerFromRole/ToRole inside dialogs |
| `RSVPReminderOffsetDays.HasValue` | Disables the per-group RSVP offset days override |
| `RSVPReminderSystemCommunicationId.HasValue` | Disables the per-group RSVP communication override |

## Field-controlled state (within edit panel)

| Field | Toggled UI |
|---|---|
| `cbIsActive` (unchecked) | Inactive Reason + Note rows visible; if `hfHasChildGroups=true`, Inactivate Child Groups checkbox visible |
| `cbIsPublic` (unchecked) | hlIsPrivate label visible |
| `cbIsSecurityRole` (checked) OR group type IS the security-role group type | pnlElevatedSecurity visible |
| `ddlGroupType` (changed) | Triggers full re-evaluation of all GroupType-driven flags |
| `gpParentGroup` (changed) | Filters GroupType options to those allowed under that parent (autopostback) |
| `cbOverrideRelationshipStrength` (checked) | pnlPeerNetwork visible |
| `rblRelationshipStrength` (changed) | If 0, hides RelationshipGrowth + Advanced sub-panels |
| `swShowPeerNetworkAdvancedSettings` (toggled) | Shows/hides 4 multiplier text boxes |
| `rblScheduleSelect` (changed) | Shows DayOfWeek+Time, Schedule picker, or ScheduleBuilder accordingly |
| `cblScheduleCoordinatorNotificationTypes` "None" | Selecting None deselects others; selecting another deselects None |
| `ddlGroupRequirementType` (in modal, changed) | Shows DueDate picker OR DueDateGroupAttribute dropdown OR neither based on `DueDateType` |
| `ddlTriggerType` (in modal, changed) | Shows different qualifier sub-controls per trigger type |
| `ddlMember` empty (locations dialog) | Forces "Other Location" tab if Member tab has no items |
| Tabs (Member/Other) in locations dialog | Switches between member ddl and location picker |

## Mode visibility matrix

| State | pnlEditDetails | fieldsetViewDetails | btnEdit | btnDelete | btnArchive | btnCopy | btnSecurity |
|---|---|---|---|---|---|---|---|
| Not authorized to view | hidden | hidden | n/a | n/a | n/a | n/a | n/a |
| View-only | hidden | visible | hidden | hidden | hidden | hidden | hidden |
| Edit, readonly mode (default) | hidden | visible | visible | conditional | conditional | conditional | conditional |
| Edit, edit mode (user clicked Edit OR autoEdit OR new) | visible | hidden | n/a | n/a | n/a | n/a | n/a |
| New (GroupId=0) | visible | hidden | n/a | n/a | n/a | n/a | n/a |
| Group not found | hidden | hidden | hidden | hidden | hidden | hidden | hidden |

Conditions for the "conditional" cells in readonly mode:
- **btnDelete**: `!IsSystem && Authorization.EDIT && (!EnableGroupHistory OR no history rows)`
- **btnArchive**: `!IsSystem && !IsArchived && Authorization.EDIT && EnableGroupHistory && (history rows exist)`
- **btnCopy**: `ShowCopyButton attribute && Authorization.EDIT`
- **btnSecurity**: `Authorization.ADMINISTRATE`

## Highlight label visibility

| Label | Shown when |
|---|---|
| Inactive | `!group.IsActive` (in view), or always-visible-with-JS-display-none (in edit) |
| Archived | `group.IsArchived` |
| Private | `!group.IsPublic` (in view), or always-visible-with-JS-display-none (in edit) |
| Elevated Security | `group.IsSecurityRole && group.ElevatedSecurityLevel > None` (warning, danger if Extreme) |
| Peer Network | `groupType.IsPeerNetworkEnabled` (text describes strength + override status) |
| Chat | `ChatHelper.IsChatEnabled && group.GetIsChatEnabled()` |
| Type | always (links to GroupTypeDetail if Authorize.ADMINISTRATE) |
| Campus | `group.Campus != null` |

## Notification visibility

| Notification | Shown when |
|---|---|
| nbNotFoundOrArchived | Group requested but not retrievable |
| nbEditModeMessage | Read-only or System group; reused for "not authorized to copy" message |
| nbRoleLimitWarning | `group.GetGroupTypeRoleLimitWarnings()` returns warnings |
| nbNotAllowedToEdit | Save attempted but auth lost mid-flow |
| nbInvalidParentGroup | Save attempted with parent group that doesn't allow this group type |
| nbGroupCapacityMessage | Capacity warnings (control declared but never set in code-behind in this version) |
| nbGroupLocationEditMessage | Inside Locations dialog: "Please select a location" or "{location} already exists in meeting details" |
| nbDuplicateGroupRequirement | Inside Group Requirement dialog when (RequirementType + GroupRole) duplicate exists |
| nbInvalidWorkflowType | Inside Member Workflow Trigger dialog when WorkflowType missing/invalid |

---

## Comprehensive state matrix

This section combines every meaningful axis of state into a single matrix. Rows are state keys (block attributes, group flags, page parameters, mode); columns indicate which UI elements / panels / controls are affected.

Notation: `Y` = visible/enabled; `N` = hidden/disabled; `~` = conditional further; `R` = required; `Disabled` = visible but locked; cell shows the affected element name. When a state purely composes with another, both rows apply.

### Top-level mode states

| State | pnlDetails | fieldsetViewDetails | pnlEditDetails | nbNotFoundOrArchived | nbEditModeMessage |
|---|---|---|---|---|---|
| GroupId param missing | N | N | N | N | N |
| GroupId valid, group not found | N | N | N | Y "does not exist or has been archived" | N |
| GroupId valid, view-only | Y | Y | N | N | Y "Read-only" |
| GroupId valid, edit allowed, readonly mode | Y | Y | N | N | ~ system-only |
| GroupId valid, edit allowed, edit mode | Y | N | Y | N | ~ system-only |
| GroupId = 0 (Add) with auth | Y | N | Y | N | N |
| GroupId = 0 (Add) without auth | Y | Y | N | N | Y "Read-only" |

Source: [GroupDetail.ascx.cs:1750-1880](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1750), [GroupDetail.ascx.cs:2901-2906](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2901).

### Authorization states (apply to all modes)

| State | btnEdit | btnDelete | btnArchive | btnCopy | btnSecurity | wpGroupSync | wpGroupRequirements | wpGroupMemberAttributes | cbIsSecurityRole |
|---|---|---|---|---|---|---|---|---|---|
| Block-level deny + per-group EDIT deny | n/a (readonly) | N | N | N | N | n/a | n/a | n/a | n/a |
| Per-group EDIT only | Y | ~ history | ~ history | ~ ShowCopyButton | ~ ADMIN | N | N | N | N |
| Per-group ADMINISTRATE | Y | ~ history | ~ history | ~ ShowCopyButton | Y | Y | Y | Y | ~ GROUP_ADMINISTRATORS |
| Member of GROUP_ADMINISTRATORS | inherits | inherits | inherits | inherits | inherits | inherits | inherits | inherits | Y (visible) |
| Group type IS security-role type | inherits | inherits | inherits | inherits | inherits | inherits | inherits | inherits | n/a (forced; see pnlElevatedSecurity) |

Source: [GroupDetail.ascx.cs:1853-1858](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1853), [GroupDetail.ascx.cs:2569-2598](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2569), [GroupDetail.ascx.cs:2001-2004](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2001), [GroupDetail.ascx.cs:2022](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2022).

### IsSystem state

| Affected | When `group.IsSystem = true` |
|---|---|
| `nbEditModeMessage` | Shows "System group" message |
| `btnDelete` | N (overrides EDIT auth) |
| `btnArchive` | N |
| Chat dropdowns + image uploader | Disabled (visible but locked) at [GroupDetail.ascx.cs:2545-2553](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2545) |

### IsArchived state

| Affected | When `group.IsArchived = true` |
|---|---|
| `hlArchived` | Y |
| `btnDelete` | N |
| `btnArchive` | N (already archived) |
| Group still loads and displays normally otherwise | |

Source: [GroupDetail.ascx.cs:2576-2598](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2576), [GroupDetail.ascx.cs:1901](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1901).

### Block attribute states (effects in edit mode)

| Block attribute | Effect when set / true |
|---|---|
| `LimittoSecurityRoleGroups` (T) | Force-defaults new group to security-role type; force-checks + disables `cbIsSecurityRole`; force-true at save. |
| `LimitToShowInNavigationGroupTypes` (T) | Filter group-type dropdown to ShowInNavigation=true. |
| `PreventSelectingInactiveCampus` (T) | `cpCampus.IncludeInactive = false`. |
| `ShowCopyButton` (T) | Render `btnCopy` (subject to readOnly + auth gates). |
| `EnableGroupTags` (T) AND `groupType.EnableGroupTag` (T) | Render `taglGroupTags`. |
| `AddAdministrateSecurityToGroupCreator` (T) | On Save when adding, grant ADMINISTRATE to creator. |
| `GroupTypes` (non-empty) | Restrict picker to listed types. |
| `GroupTypesExclude` (non-empty AND GroupTypes empty) | Exclude listed types. |

### GroupType-driven states (apply to edit mode)

(Existing matrix above) plus:

| GroupType property | Affects (additional) |
|---|---|
| `IsSystem` (on group, not group type) | See IsSystem row above |
| `IsArchived` | See IsArchived row above |
| `GroupCapacityRule == None` | `nbGroupCapacity.Visible = false` |
| `GroupCapacityRule == Soft/Hard` | `nbGroupCapacity.Visible = true`; sets help text per rule |
| `IsCapacityRequired` | `nbGroupCapacity.Required = true` |
| `EnableInactiveReason == false` | `ddlInactiveReason.Visible = false`; `tbInactiveNote.Visible = false` |
| `EnableLocationSchedules == true` | Locations grid Schedules column visible; spSchedules picker visible in dialog |
| `LocationSelectionMode == None` AND `IsScheduleTabVisible == false` | `wpMeetingDetails.Visible = false` |
| `LocationSelectionMode != None` | `wpMeetingDetails.Visible = true` |
| `LocationSelectionMode` flags | Determine LocationPicker mode set in dialog (Named, Address, Point, Polygon, GroupMember tab) |
| `AllowMultipleLocations == false` | Locations grid Add button hidden once one row exists |
| `AllowedScheduleTypes` flags (Weekly/Custom/Named) | Each flag adds the matching radio option in `rblScheduleSelect`; `IsScheduleTabVisible` true if any flag set |
| `AllowGroupSpecificRecordSource` | `dvpRecordSource.Visible = true` |
| `AllowSpecificGroupMemberAttributes` | `wpGroupMemberAttributes.Visible = true` (combined with ADMIN auth and existing attrs) |
| `AllowGroupSync` | `wpGroupSync.Visible` (combined with ADMIN auth and existing syncs) |
| `AllowSpecificGroupMemberWorkflows` | `wpMemberWorkflowTriggers.Visible = true` (or any existing) |
| `EnableSpecificGroupRequirements` | `gGroupRequirements.Actions.ShowAdd = true`; specific requirements grid visible |
| `IsSchedulingEnabled` | `wpScheduling.Visible = true`; Locations dialog shows capacity repeater |
| `EnableRSVP` | `wpRsvp.Visible = true` |
| `EnableGroupHistory` | When existing history rows: replace Delete with Archive |
| `IsPeerNetworkEnabled` | `pnlPeerNetworkOverride.Visible = true`; otherwise hidden |
| `IsChatAllowed` AND `ChatHelper.IsChatEnabled` | `wpChat.Visible = true` |
| `ShowAdministrator` | `ppAdministrator.Visible = true`; label = `AdministratorTerm` |
| `GroupStatusDefinedTypeId.HasValue` | `dvpGroupStatus.Visible = true`; label = the DefinedType's name |
| `GroupsRequireCampus` | At save, fall back to single-campus Id if no campus chosen |

### Field-level states (within edit panel)

(Same as existing field-controlled state table; collapse here.)

### Page-parameter-driven states

| Param state | Effect |
|---|---|
| `?autoEdit=true` (existing group + EDIT auth) | Open in edit mode instead of view |
| `?autoEdit=true` (existing group, view-only) | Ignored (readOnly path wins) |
| `?ParentGroupId=N` (Add path) | Default ParentGroup, filter group types via parent's allowed children, exactly-one auth-passing type defaults; otherwise force user to choose |
| `?ReturnUrl=URL` | Override post-save / cancel / delete navigation; redirects via `Response.Redirect(returnUrl)` |
| `?ExpandedIds=...` | Round-tripped on every navigation; not interpreted by this block |
| `?GroupId=0` AND no `ParentGroupId` | New group, no defaults, picker shows all allowed types |

### Composite states (key combinations)

#### "First-time security-role admin creates new role"

- `?GroupId=0`, `?ParentGroupId=` not set
- Block attribute `LimittoSecurityRoleGroups = true`
- User is in GROUP_ADMINISTRATORS
- `pnlEditDetails.Visible = true`
- `cbIsSecurityRole.Visible = true` (member of admins) but `Enabled = false; Checked = true` (forced by attribute)
- `pnlElevatedSecurity.Visible = true` (because IsSecurityRole checked OR group type is security-role type)
- `ddlGroupType` defaults to Security Roles type
- All other GroupType-driven panels evaluate against the security-role type's settings

#### "Existing system group, viewer with no edit auth"

- `?GroupId=N`, group exists, `group.IsSystem = true`, no per-group EDIT
- `readOnly = true`
- `nbEditModeMessage.Text = ReadOnlyEditActionNotAllowed` initially, then OVERWRITTEN by `EditModeMessage.System` (because IsSystem check at line 1844 is unconditional)
- Effect: the user sees "System group" message instead of "read-only", even though they ALSO can't edit. This is a UX quirk: the System message implies edit-restricted, so it's acceptable.
- All action buttons hidden
- `fieldsetViewDetails.Visible = true`

#### "Add child group under a parent that allows multiple types, user authorized for some"

- `?GroupId=0`, `?ParentGroupId=N`
- Iterate child types of parent's group type
- `editAllowed = true` if any one passes per-group EDIT auth
- `group.GroupType` left null (forces user to pick)
- `group.GroupTypeId = 0`
- `wpGeneral.Expanded = true` (line 1780)
- All GroupType-driven panels are hidden (no type selected)
- Save with `CurrentGroupTypeId == 0` shows "GroupType cannot be blank" error and aborts (line 791-795)

#### "Existing group, edit, mid-flow GroupType change"

- User changes `ddlGroupType` selection
- Autopostback fires `ddlGroupType_SelectedIndexChanged`
- Re-runs: `SetRecordSourceControls`, `SetPeerNetworkControls`, `SetRsvpControls(null)`, `SetScheduleControls(null)`, `ShowGroupTypeEditDetails`, `BindInheritedAttributes`, `BindGroupRequirementsGrid`, `BindAdministratorPerson`, `SetChatControls`. [GroupDetail.ascx.cs:1556-1575](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1556)
- IMPORTANT: passes `null` for the group to `SetRsvpControls` and `SetScheduleControls`, meaning user-entered values for those panels are LOST when type changes (only group-type defaults take effect).
- IMPORTANT: passes a fresh `new Group { GroupTypeId = CurrentGroupTypeId }` to `SetRecordSourceControls`, `SetPeerNetworkControls`, `ShowGroupTypeEditDetails`. So group-instance values for those panels are also DISCARDED on type change.
- IMPORTANT: `SetChatControls` is called with the new group object.

This is significant for conversion: the user's in-progress edits to RSVP, schedule, peer-network, record-source, and group-attribute values reset to defaults whenever they change group type. The Obsidian conversion needs to preserve this exact behavior or document a deliberate change.

### Modal dialog states (`hfActiveDialog` machine)

The `hfActiveDialog` HiddenField stores which modal is currently visible across postbacks.

#### State machine values

```
hfActiveDialog ∈ { "", "LOCATIONS", "GROUPMEMBERATTRIBUTES", "GROUPREQUIREMENTS", "MEMBERWORKFLOWTRIGGERS", "GROUPSYNCSETTINGS" }
```

Set via `ShowDialog(string, bool)` at [GroupDetail.ascx.cs:3061-3065](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:3061):
```csharp
hfActiveDialog.Value = dialog.ToUpper().Trim();
ShowDialog( setValues );
```

The toUpper ensures storage is canonical regardless of caller casing.

#### ShowDialog switch table

| `hfActiveDialog.Value` | Modal shown |
|---|---|
| `"LOCATIONS"` | `dlgLocations.Show()` |
| `"GROUPMEMBERATTRIBUTES"` | `dlgGroupMemberAttribute.Show()` |
| `"GROUPREQUIREMENTS"` | `mdGroupRequirement.Show()` |
| `"MEMBERWORKFLOWTRIGGERS"` | `dlgMemberWorkflowTriggers.Show()` |
| `"GROUPSYNCSETTINGS"` | `mdGroupSyncSettings.Show()` |
| (empty) | nothing |

Source: [GroupDetail.ascx.cs:3071-3091](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:3071).

#### HideDialog switch table

| `hfActiveDialog.Value` | Modal hidden |
|---|---|
| `"LOCATIONS"` | `dlgLocations.Hide()` |
| `"GROUPMEMBERATTRIBUTES"` | `dlgGroupMemberAttribute.Hide()` |
| `"GROUPREQUIREMENTS"` | `mdGroupRequirement.Hide()` |
| `"MEMBERWORKFLOWTRIGGERS"` | `dlgMemberWorkflowTriggers.Hide()` |
| `"GROUPSYNCSETTINGS"` | `mdGroupSyncSettings.Hide()` |

Then `hfActiveDialog.Value = string.Empty`.

Source: [GroupDetail.ascx.cs:3096-3118](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:3096).

#### How modal state survives postbacks

1. User clicks Add on a grid; handler calls `ShowDialog("GROUPSYNCSETTINGS", true)`.
2. `hfActiveDialog.Value = "GROUPSYNCSETTINGS"`. The matching modal `.Show()` is called, which the modal's CSS/JS will keep visible client-side after the partial postback.
3. User performs an action inside the modal that triggers ITS OWN postback (e.g., AutoPostBack on `ddlTriggerType`). The `hfActiveDialog` value persists because it's a server hidden field, not request data.
4. `OnLoad` runs. `Page.IsPostBack = true`. The `else` branch runs: `ShowDialog()` (no-arg) at [GroupDetail.ascx.cs:519](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:519). This re-opens the same modal because `hfActiveDialog.Value` still holds "GROUPSYNCSETTINGS".
5. When the user clicks save or cancel, `HideDialog()` clears `hfActiveDialog.Value`.

Cancel-via-script: every modal's `OnCancelScript` is `clearActiveDialog();` (defined inline in the markup at [GroupDetail.ascx:4](RockWeb/Blocks/Groups/GroupDetail.ascx:4)):
```javascript
function clearActiveDialog() {
    $('#<%=hfActiveDialog.ClientID %>').val('');
}
```
This client-side clear ensures the next postback doesn't re-open the dismissed modal.

#### Modal visibility constraint

Only ONE modal can be open at a time. The `hfActiveDialog` is a single string; opening a new modal overwrites the value. `mdCopyGroup` and `mdArchive` are NOT in the `hfActiveDialog` machine; they have their own `Show()` calls and are independent. So in practice up to TWO modals could overlap (one in the hf machine, plus mdCopyGroup or mdArchive). In practice the user never gets there because each modal is gated by an explicit user action.

#### Standalone modals (NOT in hfActiveDialog)

| Modal | Trigger | Cancel behavior |
|---|---|---|
| `mdCopyGroup` | `btnCopy_Click` | Default ModalDialog cancel; no script |
| `mdArchive` | `btnArchive_Click` if has child groups | Default ModalDialog cancel; no script |
| `mdDeleteWarning` | `groupService.CanDelete` returns errorMessage | n/a (alert, dismiss) |

### Conditional control visibility map per state

This subsection enumerates which specific controls are toggled by each state combination. Visibility states are encoded as Y/N/Conditional.

#### State: "New group, GroupType not yet chosen" (groupId=0, CurrentGroupTypeId=0)

```
ddlGroupType.Visible = Y          (line 1949)
lGroupType.Visible = N            (line 1950)
pnlElevatedSecurity.Visible = N   (CurrentGroupTypeCache is null in ShowGroupTypeEditDetails)
wpRsvp.Visible = N                (groupType is null)
wpMeetingDetails.Visible = N      (groupType is null OR IsScheduleTabVisible=false)
wpScheduling.Visible = N
wpGroupAttributes.Visible = N
wpGroupMemberAttributes.Visible = N
wpGroupRequirements.Visible = N
wpChat.Visible = N
wpGroupSync.Visible = N
wpMemberWorkflowTriggers.Visible = N
nbGroupCapacity.Visible = N       (no group type to evaluate rule)
ppAdministrator.Visible = N       (BindAdministratorPerson with null groupType)
dvpRecordSource.Visible = N
pnlPeerNetworkOverride.Visible = N
pdAuditDetails.Visible = N        (group.Id == 0)
```

#### State: "Existing group, edit mode, group type with everything enabled"

(All GroupType flags true: IsPeerNetworkEnabled, EnableRSVP, IsSchedulingEnabled, IsChatAllowed, EnableLocationSchedules, AllowMultipleLocations, etc.)

```
ddlGroupType.Visible = N          (existing group)
lGroupType.Visible = Y            (text label)
pnlElevatedSecurity.Visible = ~   (IsSecurityRole or security-role type)
pnlPeerNetworkOverride.Visible = Y
pnlPeerNetwork.Visible = ~ cbOverrideRelationshipStrength.Checked
wpRsvp.Visible = Y
wpMeetingDetails.Visible = Y
gGroupLocations.Visible = Y
gGroupLocations.Columns[2].Visible (Schedules) = Y
spSchedules.Visible = Y
wpScheduling.Visible = Y
wpGroupAttributes.Visible = ~     (group has attributes)
wpGroupMemberAttributes.Visible = Y  (Authorize.ADMIN)
wpGroupRequirements.Visible = Y    (Authorize.ADMIN)
wpChat.Visible = Y
wpGroupSync.Visible = Y           (Authorize.ADMIN)
wpMemberWorkflowTriggers.Visible = Y
nbGroupCapacity.Visible = Y
ppAdministrator.Visible = ~ ShowAdministrator
dvpRecordSource.Visible = ~ AllowGroupSpecificRecordSource
dvpGroupStatus.Visible = ~ GroupStatusDefinedTypeId.HasValue
ppScheduleCoordinatorPerson.Visible = Y
cblScheduleCoordinatorNotificationTypes.Visible = Y
```

#### State: "Existing group, view mode, full quick-link visibility"

```
btnEdit.Visible = Y
btnDelete.Visible = ~
btnArchive.Visible = ~
btnCopy.Visible = ~ ShowCopyButton + EDIT
btnSecurity.Visible = ~ ADMIN
hlMap.Visible = ~ MapPage URL set
hlGroupRSVP.Visible = ~ EnableRSVP + URL set
hlGroupScheduler.Visible = ~ IsSchedulingEnabled + URL set
hlGroupHistory.Visible = ~ EnableGroupHistory + URL set
hlAttendance.Visible = ~ TakesAttendance + URL set
hlFundraisingProgress.Visible = ~ Fundraising group type + URL set
hlGroupPlacement.Visible = ~ groupType != null + URL set
taglGroupTags.Visible = ~ EnableGroupTags + EnableGroupTag
hlInactive.Visible = ~ !IsActive
hlArchived.Visible = ~ IsArchived
hlIsPrivate.Visible = ~ !IsPublic
hlElevatedSecurityLevel.Visible = ~ IsSecurityRole + ElevatedSecurityLevel > None
hlPeerNetwork.Visible = ~ IsPeerNetworkEnabled
hlChat.Visible = ~ ChatHelper.IsChatEnabled + GetIsChatEnabled()
hlType.Visible = Y (always)
hlCampus.Visible = ~ Campus != null
divBadgeContainer.Visible = ~ BadgeCache.All for Group has any
pnlFollowing.Visible = Y (FollowingsHelper sets up)
```

### Open questions / flag for spec phase

- The "GroupType change discards in-flight values" behavior is non-obvious and probably the source of user confusion. Spec phase should confirm: preserve as-is (legacy compat), or warn the user / preserve user-entered values across type change?
- The `nbEditModeMessage` overwrite when both readOnly AND IsSystem: spec phase decision: preserve, or show both messages (e.g., "Read-only and System")?
- Modal state with `hfActiveDialog` is WebForms-only. Conversion design must decide: client-only modal state (Vue refs) vs server-tracked modal state (bag value). Client-only is simpler.
- The client-side scripts at the bottom of the markup (lines 716-822) hand-roll the Inactive/Private label show/hide on checkbox change, and the archive-confirm dialog. Conversion replaces these with Vue-reactive equivalents.
- `nbGroupCapacityMessage` is declared in markup (line 56) but never set anywhere in the code-behind. Either dead code or a feature-stub. Conversion should decide.
- The "ShowGroupTypeEditDetails has duplicate code" bug (lines 2245-2258 vs 2260-2273) should be fixed in the converted block, not preserved.
