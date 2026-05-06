# Code-Behind Walkthrough

A method-by-method tour of `GroupDetail.ascx.cs` (5,124 lines), grouped by responsibility. Line numbers are anchors into the WebForms source.

## Lifecycle

| Method | Lines | What it does |
|---|---|---|
| `LoadViewState` | 327-420 | Hydrates every `*State` collection from JSON (GroupLocations, GroupMemberAttributes Inherited+Custom, GroupRequirements, GroupDateAttributes, GroupSync, MemberWorkflowTriggers). Re-attaches GroupRoles to GroupRequirements that lost them through serialization. |
| `OnInit` | 426-488 | Wires up grid Add/Rebind/Reorder events; sets DataKeyNames; wires the SecurityField for member attributes; sets delete confirm onclick; binds elevated security and schedule-type radios; loads badges. |
| `OnLoad` | 494-552 | If not postback: read `GroupId` from URL and call `ShowDetail`. If postback and `pnlDetails.Visible`: rebuild attribute controls based on `CurrentGroupTypeId`. Resolves group again, sets up TagList + Following, replays modal dialog. |
| `SaveViewState` | 560-581 | Serializes every `*State` to JSON. |
| `GetBreadCrumbs` | 592-615 | Returns "{Group.Name}" or "New Group" breadcrumb. |

## Edit lifecycle methods

| Method | Lines | Notes |
|---|---|---|
| `btnEdit_Click` | 626-629 | `ShowEditDetails(GetGroup(hfGroupId))`. |
| `btnArchive_Click` | 636-648 | If group has child groups, show `mdArchive`. Otherwise `ArchiveSingleGroup`. |
| `mdArchive_SingleGroupClick` | 655-658 | "No" button -> archive only this group. |
| `mdArchive_AllChildGroupsClick` | 665-668 | "Yes" button -> archive group + descendants. |
| `btnDelete_Click` | 675-729 | Auth check; `groupService.CanDelete`; delete unique non-named schedule if no other group uses it; `DeleteSecurityRoleGroup` if security role else `groupService.Delete`. |
| `NavigateAfterDeleteOrArchive` | 735-761 | If `returnUrl`, redirect; else navigate to GroupListPage with parent group selected. |
| `btnSave_Click` | 768-1451 | The behemoth. See "Save flow" below. |
| `btnCancel_Click` | 1458-1500 | Returns to readonly view, GroupListPage, or returnUrl. |
| `btnCopy_Click` | 1507-1510 | `mdCopyGroup.Show()`. |
| `mdCopyGroup_SaveClick` | 1517-1545 | Auth check; `GroupService.CopyGroup(CopyGroupOptions)`; navigate to new group. |

### Save flow — `btnSave_Click` walkthrough

1. **Validate group type chosen** (line 791-795). Aborts if `CurrentGroupTypeId == 0`.
2. **Load or create group** (line 797-808). On existing, eagerly include `Schedule, GroupLocations.Schedules`. Capture `wasSecurityRole` for cache-flush logic.
3. **Sync GroupLocations against state** (line 810-868):
   - For each removed location: delete its `GroupLocationScheduleConfigs`, then any `GroupMemberAssignment` rows targeting that location/schedule, then the location itself. Set `checkinDataUpdated`.
   - For each removed group requirement: delete via service.
   - For each removed trigger: remove and delete; set `triggersUpdated`.
   - For each removed sync: remove and delete.
4. **Resolve group type** (line 871).
5. **Add/update group requirements** (line 873-886). New ones go into `groupRequirementsToInsert`.
6. **Add/update group locations** (line 888-991):
   - Find or create the GroupLocation.
   - If LocationId changed, delete `GroupMemberAssignment` for old config.
   - Remove schedules no longer selected; copy properties.
   - Diff `GroupLocationScheduleConfigs` (existing vs modified vs new) and apply.
7. **Chat overrides** (line 993-1009) if `ChatHelper.IsChatEnabled && GroupType.IsChatAllowed`. Tracks orphaned chat-channel-avatar binary file id for later cleanup.
8. **Add/update GroupSyncs** (line 1011-1022) and **GroupMemberWorkflowTriggers** (line 1024-1041).
9. **Resolve campus** (line 1043-1048) using single-campus fallback if `GroupsRequireCampus`.
10. **Apply scalar properties** (line 1050-1067): Name, Description, Campus, GroupTypeId, ParentGroupId, StatusValueId, GroupCapacity, RequiredSignatureDocumentTemplateId, GroupMemberRecordSourceValueId.
11. **IsSecurityRole + ElevatedSecurityLevel** (line 1068-1080). If `LimittoSecurityRoleGroups` block attribute, force IsSecurityRole=true. Force ElevatedSecurityLevel=None when not security role.
12. **IsActive, IsPublic, InactiveReason, InactiveReasonNote** (line 1082-1087).
13. **Peer Network overrides** (line 1089-1117). Only saved when `IsPeerNetworkEnabled`. Override fields set from controls when `cbOverrideRelationshipStrength.Checked`, otherwise nulled.
14. **RSVP overrides** (line 1119-1148). Only saved when `EnableRSVP`. Group fields nulled if group type already has values (group type wins).
15. **Scheduling fields** (line 1150-1252): SchedulingMustMeetRequirements, AttendanceRecordRequiredForCheckIn, ScheduleCoordinatorPersonAliasId, DisableScheduling, DisableScheduleToolboxAccess, ScheduleConfirmationLogic. Builds `ScheduleCoordinatorNotificationTypes` flags from checkbox list.
16. **Schedule entity management** (line 1184-1252):
    - Validates Custom (must have iCalendar) and Weekly (must have day-of-week).
    - Creates/updates a unique non-named `Schedule` for Weekly/Custom.
    - Deletes the prior unique schedule if no longer used.
    - Sets `group.ScheduleId` to the named schedule for ScheduleType.Named, or null otherwise.
17. **Self-parent guard** (line 1254-1258).
18. **Save group attributes** (line 1260-1261): `LoadAttributes`, `Helper.GetEditValues(phGroupAttributes, group)`.
19. **Validate group type allowed under parent** (line 1263-1284).
20. **Validate edit auth** (line 1286-1291) — re-checked because group type/parent may have changed.
21. **Page validation** (line 1293-1296).
22. **Cross-validate via Group.IsValid** (line 1298-1306).
23. **Wrap in transaction** (line 1308-1406):
    - If new, `groupService.Add` then `SaveChanges` to get Id.
    - If `AddAdministrateSecurityToGroupCreator` block attribute, `Authorization.AllowPerson(group, ADMINISTRATE, CurrentPerson)`.
    - Insert pending `groupRequirementsToInsert` with the new group Id.
    - `group.SaveAttributeValues`.
    - Save GroupMemberAttributes (delete removed; `Rock.Attribute.Helper.SaveAttributeEdits` per remaining); `SaveChanges`.
    - Cascade-inactivate child groups when checkbox checked.
    - Toggle `IsTemporary` on chat-channel avatar binary files (orphaned -> temporary, current -> permanent).
24. **Cache invalidation** (line 1408-1436):
    - `Authorization.Clear()` if security role status changed in either direction.
    - `GroupMemberWorkflowTriggerService.RemoveCachedTriggers()` if any triggers updated.
    - `KioskDevice.Clear()` if `checkinDataUpdated && groupType.TakesAttendance`.
25. **Navigate** (line 1438-1450). Back to current page with `GroupId` and `ExpandedIds`, unless `returnUrl` set.

## ShowDetail / ShowEditDetails / ShowReadonlyDetails

| Method | Lines | Notes |
|---|---|---|
| `ShowDetail(int)` overload | 1740-1743 | Forwards to two-arg version with null parent. |
| `ShowDetail(int, int?)` | 1750-1880 | Loads group; or constructs new group with parentGroupId-derived defaults. Verifies VIEW + EDIT auth. Picks edit vs readonly based on `autoEdit`. Handles "not found or archived" notification. Uses an unused `lava` variable that resolves an `AddQuickReturn` filter (line 1770-1773 — appears to be a side-effect-only Lava call). |
| `SetHighlightLabelVisibility` | 1886-1912 | Sets visibility for Inactive/Private/Archived labels. In edit mode, leaves them visible-but-hidden so JS can show/hide on checkbox toggle. |
| `GetGroupCapacityHelpText` | 1914-1927 | Per-rule (Soft/Hard) help text. |
| `ShowEditDetails` | 1933-2137 | Sets title (Add vs name); `SetEditMode(true)`; populates every form field; loads inactive reason dd; computes `hfHasChildGroups`; `LoadDropDowns`; sets sync/requirements/member-attribute panel visibility based on ADMINISTRATE; loads `GroupSyncState`; sets `cbIsSecurityRole` visibility based on GROUP_ADMINISTRATORS membership; conditional defaults; `SetRecordSourceControls`, `SetPeerNetworkControls`, `SetRsvpControls`, `SetScheduleControls`, `ShowGroupTypeEditDetails`, `SetChatControls`; loads scheduling fields and notification-type checkboxes; loads schedule coordinator; computes member-attribute state; binds inherited attributes; binds requirements; loads triggers. |
| `LoadElevatedSecurityRadioList` | 2139-2146 | Populates from `ElevatedSecurityLevel` enum. |
| `BindAdministratorPerson` | 2152-2165 | Visibility + label come from group type's `ShowAdministrator` and `AdministratorTerm`. |
| `ShowGroupTypeEditDetails` | 2173-2295 | Almost-everything-else: AllowMultipleLocations stash; sync panel visibility based on `selectedGroupType.AllowGroupSync OR has any`; member workflow triggers visibility based on `AllowSpecificGroupMemberWorkflows OR has any`; capacity required + help; pnlElevatedSecurity visibility; status defined-value picker; meeting details + locations grid visibility based on `LocationSelectionMode`; group attribute editor (DynamicPlaceholder); wpScheduling visibility based on `IsSchedulingEnabled`. NOTE: lines 2245-2258 and 2260-2273 are an apparent code duplication bug. |
| `SetScheduleControls` | 2302-2364 | Resets schedule controls; populates rblScheduleSelect from `AllowedScheduleTypes` flags; sets `IsScheduleTabVisible` true if any allowed; calls `SetScheduleDisplay`. |
| `SetRecordSourceControls` | 2371-2384 | Visibility + value based on `AllowGroupSpecificRecordSource`. |
| `SetPeerNetworkControls` | 2391-2442 | Override panel visibility; relationship strength fallback to group type; growth + advanced; placeholders set to group-type values. |
| `SetPeerNetworkSubControlVisibility` | 2449-2456 | Hides growth + advanced when strength == 0. |
| `SetRsvpControls` | 2463-2505 | Visibility + read-only state of offset + reminder communication based on group type. |
| `SetChatControls` | 2512-2561 | Visibility based on `ChatHelper.IsChatEnabled && IsChatAllowed`. Disables fields when `IsSystem`. |
| `ShowReadonlyDetails` | 2567-2843 | The view panel renderer. Computes label visibility; computes peer-network label text; computes chat label; auto-links Type label to GroupTypeDetail page if ADMINISTRATE; sets pdAuditDetails entity; sets Campus label; sets ElevatedSecurity label; sets all hyperlink quick-link URLs and visibility; renders `groupType.GroupViewLavaTemplate` into `lContent` with merge fields. |
| `GetRelationshipStrengthLabel` | 2866-2895 | Maps int strength -> {Article, Relationship} for display. |
| `SetEditMode` | 2901-2906 | Toggles pnlEditDetails vs fieldsetViewDetails; calls `HideSecondaryBlocks(editable)`. |
| `GetGroup(int, RockContext?)` | 2913-2931 | Cached via `RockPage.GetSharedItem` / `SaveSharedItem`. Eagerly includes GroupType, GroupLocations.Schedules, GroupSyncs. |
| `GetAllowedGroupTypes` | 2939-2976 | Applies block-attribute include/exclude; parent group type's `ChildGroupTypes` filter; `LimitToShowInNavigationGroupTypes` filter. |

## Helper URL methods

| Method | Lines | Notes |
|---|---|---|
| `RegistrationInstanceUrl` | 2983-2988 | `?RegistrationInstanceId=N`. |
| `EventItemOccurrenceUrl` | 2995-3000 | `?EventItemOccurrenceId=N`. |
| `ContentItemUrl` | 3007-3012 | `?ContentItemId=N`. |

These three are passed via merge fields to the View panel Lava template.

## Dropdown loading

| Method | Lines | Notes |
|---|---|---|
| `LoadDropDowns` | 3017-3054 | SignatureDocumentTemplate (legacy templates), RSVPReminderSystemCommunication (filtered to RSVP_CONFIRMATION category), AttendanceRecordRequiredForCheckIn enum, ScheduleConfirmationLogic enum (with blank). |

## Modal dialog management

| Method | Lines | Notes |
|---|---|---|
| `ShowDialog(string, bool)` | 3061-3065 | Sets `hfActiveDialog` and forwards. |
| `ShowDialog(bool)` | 3071-3091 | Switches on `hfActiveDialog.Value` to show the matching modal. |
| `HideDialog()` | 3096-3118 | Same switch but hides + clears. |
| `GetTabClass` | 3125-3133 | Returns "active" for the current LocationTypeTab. |
| `ShowSelectedPane` | 3138-3150 | Member tab vs Other tab in locations modal. |

## Inherited attributes

| Method | Lines | Notes |
|---|---|---|
| `BindInheritedAttributes` | 3157-3203 | Walks the group type's `InheritedGroupTypeId` chain, collects `Attribute` rows where `EntityTypeQualifierColumn = "GroupTypeId" && Value = thisGroupTypeId`. Also collects date-typed group attributes for the requirement-due-date dropdown. |
| `SetAttributeListOrder` | 3209-3213 | Re-numbers the Order field after reordering. |
| `ReorderAttributeList` | 3221-3245 | Standard reorder helper. |
| `SetMemberWorkflowTriggerListOrder` | 3251-3255 | Same for triggers. |
| `ReorderMemberWorkflowTriggerList` | 3263-3287 | Same. |

## Schedule capacities

| Method | Lines | Notes |
|---|---|---|
| `spSchedules_SelectItem` | 3294-3348 | When schedules change in the locations dialog, rebuild the capacities repeater preserving any in-progress edits. |
| `BindGroupLocationScheduleCapacities` | 3354-3363 | Orders schedules by next start (using SundayDate+1 as occurrence anchor). |
| `LocationSelected` | 3369-3373 | True if the location picker has a non-null Location. |

## Archive

| Method | Lines | Notes |
|---|---|---|
| `ArchiveSingleGroup` | 3378-3402 | Auth check; `groupService.Archive(group, currentPersonAliasId, true)`; `SaveChanges`; navigate. |
| `ArchiveAllChildGroups` | 3407-3438 | Same but `GetAllDescendentGroups(group.Id, true)` + archive each + then archive the group itself. |

## Locations grid + dialog

| Method | Lines | Notes |
|---|---|---|
| `gGroupLocations_Add` | 3449-3453 | hfAction = Add; show edit dialog with Guid.Empty. |
| `gGroupLocations_Edit` | 3460-3465 | hfAction = Edit; show edit dialog with row's Guid. |
| `gGroupLocations_ShowEdit` | 3471-3649 | The location-dialog populator. Builds member tab (one ListItem per family-address per member-of-group). Builds location picker modes from group type. Loads Location Type defined values. Pre-selects schedules (separating active vs inactive). Builds capacity repeater. |
| `rptGroupLocationScheduleCapacities_ItemDataBound` | 3656-3677 | Per-row capacity input population. |
| `gGroupLocations_Delete` | 3684-3689 | Removes from `GroupLocationsState` (in memory). |
| `gGroupLocations_GridRebind` | 3696-3699 | Calls BindGroupLocationsGrid. |
| `dlgLocations_OkClick` | 3706-3848 | Resolves selected location (member tab or other tab); finds or creates GroupLocation in state; rebuilds GroupLocationScheduleConfigs from repeater; reconciles selected vs inactive schedule ids. |
| `ExistingLocationOnAdd` | 3855-3865 | Detects duplicate add by Name+Guid. |
| `BindGroupLocationsGrid` | 3870-3886 | Materializes `GridLocation` projection (Guid, Location, Type, Order, Schedules string). Add button visibility from AllowMultipleLocations. |
| `ResetLocationDialog` | 3891-3899 | Clears LocationPicker, schedules, repeater, edit message. |
| `locpGroupLocation_SelectLocation` | 3907-3919 | "Already exists" guard + clears selection. |

## Group requirements

| Method | Lines | Notes |
|---|---|---|
| `gGroupRequirements_Add` / `_Edit` / `_ShowEdit` | 3930-4042 | Populates ddlGroupRequirementType, applies due-date controls based on RequirementType.DueDateType, populates AppliesToAgeClassification options, picks DataView, etc. |
| `lAppliesToDataViewId_OnDataBound` | 4049-4057 | Render check icon if DataView assigned. |
| `mdGroupRequirement_SaveClick` | 4064-4131 | Adds or updates the in-state requirement; checks for duplicate (RequirementType + GroupRole). |
| `gGroupRequirements_Delete` | 4138-4144 | Remove from state. |
| `BindGroupRequirementsGrid` | 4541-4565 | Splits into "from group type" (read-only) and "specific" (editable) grids. Add button visibility based on `EnableSpecificGroupRequirements`. |

## Group syncs

| Method | Lines | Notes |
|---|---|---|
| `gGroupSyncs_Add` / `_Edit` / `_Delete` | 4167-4227 | Standard CRUD, ClearGroupSyncModal, CreateRoleDropDownList, CreateSystemCommunicationDropDownLists. |
| `mdGroupSyncSettings_SaveClick` | 4247-4276 | Builds GroupSyncViewModel (extends GroupSync with TimeIntervalSetting); upserts in state. |
| `BindGroupSyncGrid` | 4281-4285 | DataBind. |
| `CreateSystemCommunicationDropDownLists` | 4291-4313 | Welcome, Exit, RsvpReminder communications loaded together. |
| `CreateRoleDropDownList` | 4320-4351 | Roles for the group type, excluding already-synced roles. |
| `ClearGroupSyncModal` | 4356-4363 | Reset controls. |

## Group member attributes

| Method | Lines | Notes |
|---|---|---|
| `gGroupMemberAttributes_*` events | 4374-4461 | Standard CRUD + Reorder; uses ReservedKeyNames combining inherited + custom (excluding self). |
| `dlgGroupMemberAttribute_SaveClick` | 4468-4505 | Preserves audit/foreign fields from prior state. |
| `BindGroupMemberAttributesInheritedGrid` | 4510-4525 | Sets panel + control wrapper visibility based on `AllowSpecificGroupMemberAttributes`. |
| `BindGroupMemberAttributesGrid` | 4530-4536 | Sort by Order, DataBind. |

## Group member workflow triggers

| Method | Lines | Notes |
|---|---|---|
| `gMemberWorkflowTriggers_*` events | 4617-4830 | CRUD + Reorder. ShowEdit populates trigger-type-specific dropdowns; loads via TypeQualifier `|`-delimited string format `ToStatus|ToRole|FromStatus|FromRole|FirstTime|ShowNote|RequireNote`. |
| `ShowTriggerQualifierControls` | 4705-4795 | Visibility per trigger type (see 02-block-states.md). |
| `dlgMemberWorkflowTriggers_SaveClick` | 4847-4914 | Builds TypeQualifier string; preserves Guid+Order; validates WorkflowType. |
| `BindMemberWorkflowTriggersGrid` | 4919-4924 | Sort by Order, DataBind. |
| `FormatTriggerType` | 4932-4996 | HTML formatting for the "When" column based on type+qualifier. |

## Schedule display + group type/parent change handlers

| Method | Lines | Notes |
|---|---|---|
| `ddlGroupType_SelectedIndexChanged` | 1556-1575 | Big re-evaluation. |
| `ddlParentGroup_SelectedIndexChanged` | 1582-1627 | Re-filter group types based on parent. |
| `lbLocationType_Click` | 1634-1646 | Switch member/other tab. |
| `Block_BlockUpdated` | 1653-1673 | Redraw view after block-settings change. |
| `rblScheduleSelect_SelectedIndexChanged` | 1680-1683 | Just calls SetScheduleDisplay. |
| `cblScheduleCoordinatorNotificationTypes_SelectedIndexChanged` | 1690-1730 | Mutually exclusive None vs others. |
| `SetScheduleDisplay` | 4570-4606 | Show/hide DayOfWeek+Time, SchedulePicker, ScheduleBuilder per radio. |

## Peer network handlers

| Method | Lines | Notes |
|---|---|---|
| `cbOverrideRelationshipStrength_CheckedChanged` | 5007-5010 | Toggle pnlPeerNetwork. |
| `rblRelationshipStrength_SelectedIndexChanged` | 5017-5023 | Toggle growth + advanced subpanels. |
| `swShowPeerNetworkAdvancedSettings_CheckedChanged` | 5030-5033 | Toggle advanced sub-panel. |

## Security role checked

| Method | Lines | Notes |
|---|---|---|
| `cbIsSecurityRole_CheckedChanged` | 5042-5059 | Re-runs `ShowGroupTypeEditDetails` so pnlElevatedSecurity flips accordingly. |

## Due-date qualifier (in requirement modal)

| Method | Lines | Notes |
|---|---|---|
| `ShowDueDateQualifierControls` | 5064-5107 | Hide/show DateAttribute dropdown vs DatePicker per RequirementType.DueDateType. |
| `ddlGroupRequirementType_SelectedIndexChanged` | 5109-5112 | Calls ShowDueDateQualifierControls. |

## GroupSyncViewModel

Lines 5115-5124. Extends `GroupSync` with a `ScheduleTimeInterval` getter wrapping `ScheduleIntervalMinutes` for display.
