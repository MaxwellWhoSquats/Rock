# Markup Structure

Source: `RockWeb/Blocks/Groups/GroupDetail.ascx` (826 lines).

This file maps every visible region of the WebForms markup so that the Obsidian conversion has a 1:1 inventory before any redesign.

## Top-level layout

```
UpdatePanel (upnlGroupDetail)
└── NotificationBox "not found or archived"
└── pnlDetails (visible if user has VIEW)
    ├── HiddenField hfGroupId
    └── div.panel.panel-block
        ├── panel-heading
        │   ├── Title (icon + name)
        │   ├── Highlight labels (Inactive | Archived | Private | Elevated Security | Peer Network | Chat | Type | Campus)
        │   └── Following control
        ├── PanelDrawer pdAuditDetails (audit info)
        ├── BadgeListControl blBadgeList (Group badges)
        └── panel-body
            ├── Notification boxes (edit-mode message, role-limit warning, not-allowed-to-edit, invalid-parent-group, capacity)
            ├── ValidationSummary + CustomValidator
            ├── pnlEditDetails (edit mode, see "Edit Panel" below)
            └── fieldsetViewDetails (view mode, see "View Panel" below)

Modal dialogs (siblings, not nested):
├── mdCopyGroup
├── dlgGroupMemberAttribute
├── dlgLocations
├── mdGroupRequirement
├── mdGroupSyncSettings
├── dlgMemberWorkflowTriggers
└── mdArchive
```

## Edit Panel (`pnlEditDetails`, line 60-442)

### Top fields (always visible)

- Name (DataTextBox) + IsActive checkbox + IsPublic checkbox
- Inactive Reason dropdown (visible when group type has `EnableInactiveReason`, controlled by IsActive checkbox via JS)
- Inactive Note textarea (visible same conditions)
- Inactivate Child Groups checkbox (JS shows only when toggling Active->Inactive AND `hfHasChildGroups=true`)
- Description textarea

### Panel widgets (collapsible sections)

| ID | Title | Visibility driver | Contents |
|---|---|---|---|
| `wpGeneral` | General | Always | GroupType ddl + IsSecurityRole + ParentGroup picker + Status (defined value) + Capacity + Administrator picker + ElevatedSecurityLevel radio + Campus + SignatureDocumentTemplate ddl + RecordSource ddl + Peer Network override sub-panel |
| `wpRsvp` | RSVP | `GroupType.EnableRSVP` | Reminder system communication ddl + Reminder offset days slider |
| `wpMeetingDetails` | Meeting Details | `GroupType.LocationSelectionMode != None` OR scheduling enabled | Locations grid + Schedule sub-panel |
| `wpScheduling` | Scheduling | `GroupType.IsSchedulingEnabled` | DisableScheduling cb + MustMeetRequirements cb + DisableScheduleToolboxAccess cb + Check-in Requirements ddl + ScheduleConfirmationLogic ddl + ScheduleCoordinator person picker + ScheduleCoordinatorNotificationTypes checkbox list (None\|Accept\|Decline\|Self-Schedule) |
| `wpGroupAttributes` | Group Attribute Values | Has any group-attribute-value defined | DynamicPlaceholder for runtime-rendered attribute editors |
| `wpGroupMemberAttributes` | Member Attributes | Authorization.ADMINISTRATE + group type allows custom OR has any | Inherited grid (read-only) + custom attribute grid (Add/Edit/Delete/Reorder/Security) |
| `wpGroupRequirements` | Group Requirements | Authorization.ADMINISTRATE + group type allows specific OR has any | Inherited grid + custom requirements grid (Add/Edit/Delete) |
| `wpChat` | Chat | `ChatHelper.IsChatEnabled && GroupType.IsChatAllowed` | 5 inherit/yes/no dropdowns + push notification mode + channel avatar uploader |
| `wpGroupSync` | Group Sync Settings | Authorization.ADMINISTRATE + group type allows OR has any | Group syncs grid (Add/Edit/Delete) |
| `wpMemberWorkflowTriggers` | Group Member Workflows | group type allows OR has any | Triggers grid (Add/Edit/Delete/Reorder) |

### Save / Cancel actions

- `btnSave` (Alt+S), validation group default, `OnClick="btnSave_Click"`
- `btnCancel` (Alt+C), `CausesValidation="false"`, `OnClick="btnCancel_Click"`

### Peer Network sub-panel (inside `wpGeneral`, line 136-193)

```
pnlPeerNetworkOverride (visible if GroupType.IsPeerNetworkEnabled)
├── cbOverrideRelationshipStrength (autopostback)
└── pnlPeerNetwork (visible if Override checked)
    ├── rblRelationshipStrength radio (autopostback)
    ├── pnlRelationshipGrowth (visible if strength != 0)
    │   └── cbEnableRelationshipGrowth
    ├── pnlShowPeerNetworkAdvancedSettings (visible if strength != 0)
    │   └── swShowPeerNetworkAdvancedSettings (Switch, autopostback)
    └── pnlPeerNetworkAdvanced (visible if Switch on)
        └── 4 multiplier textboxes (Leader/NonLeader x Leader/NonLeader matrix)
```

## View Panel (`fieldsetViewDetails`, line 444-469)

```
fieldsetViewDetails
├── TagList (controlled by EnableGroupTags attribute + GroupType.EnableGroupTag)
├── lContent literal (server-rendered Lava using GroupType.GroupViewLavaTemplate)
└── actions row
    ├── btnEdit (Alt+E)
    ├── ModalAlert mdDeleteWarning
    ├── btnDelete
    ├── btnArchive (visible only when group type has GroupHistory + history exists)
    └── pull-right span:
        ├── hlGroupPlacement (link to GroupPlacementPage)
        ├── hlGroupRSVP (link to GroupRSVPPage, visible if EnableRSVP)
        ├── hlGroupScheduler (link to GroupSchedulerPage, visible if IsSchedulingEnabled, disabled if DisableScheduling)
        ├── hlGroupHistory (link to GroupHistoryPage, visible if EnableGroupHistory)
        ├── hlFundraisingProgress (link to FundraisingProgressPage, visible if group type is fundraising)
        ├── hlAttendance (link to AttendancePage, visible if TakesAttendance)
        ├── hlMap (link to GroupMapPage, visible if URL configured)
        ├── btnCopy (visible if ShowCopyButton attribute true)
        └── btnSecurity (visible if Authorization.ADMINISTRATE)
```

## Modal dialogs

### mdCopyGroup (line 473-504)

- Notice text describing what is and isn't copied.
- `cbCopyGroupIncludeChildGroups` (default true).
- Save button label: "Copy".

### dlgGroupMemberAttribute (line 510-514)

- Wraps an `Rock:AttributeEditor edtGroupMemberAttributes` for the GroupMember entity type.

### dlgLocations (line 517-600)

Two-tab dialog:
- **Member Location** tab: dropdown of family addresses for current members.
- **Other Location** tab: full LocationPicker (Named / Address / Point / Polygon / GroupMember modes determined per group type).

Below tabs:
- Location Type (defined value picker, scoped to group type's LocationTypeValues).
- Schedules picker (multi-select).
- Group Location Schedule Capacities repeater (one row per selected schedule with Min/Desired/Max number boxes). Visible only if `groupType.IsSchedulingEnabled`.

### mdGroupRequirement (line 603-630)

- GroupRequirementType dropdown (autopostback to update due-date controls).
- GroupRolePicker (filters by current group type).
- AppliesToAgeClassification radio.
- AppliesToDataView picker.
- AllowLeadersToOverride checkbox.
- DueDate picker OR DueDateGroupAttribute dropdown (one of, depending on `GroupRequirementType.DueDateType`).
- MembersMustMeetRequirementOnAdd checkbox.

### mdGroupSyncSettings (line 633-672)

- DataView picker (Person entity type).
- Group Role dropdown (filtered to group type, excluding already-synced roles).
- Sync Interval (IntervalPicker, default 12 hour).
- Welcome Communication dropdown.
- Exit Communication dropdown.
- Create Login During Sync checkbox.

### dlgMemberWorkflowTriggers (line 675-709)

- Trigger Name + IsActive.
- WorkflowType picker.
- TriggerType dropdown (autopostback).
- Conditional sub-controls per trigger type:
  - **MemberAddedToGroup / MemberRemovedFromGroup**: ToStatus + ToRole.
  - **MemberAttendedGroup**: TriggerFirstTime checkbox.
  - **MemberPlacedElsewhere**: ShowNote + RequireNote checkboxes.
  - **MemberRoleChanged**: FromRole + ToRole.
  - **MemberStatusChanged**: FromStatus + ToStatus.

### mdArchive (line 711-715)

- Body: "Would you like to archive this group's children?"
- "Yes" button: archives all children too.
- "No" button: archives only this group.
- Cancel: hidden.

### mdDeleteWarning (line 453)

ModalAlert used for "you are not authorized" and "cannot delete" messages.

## Inline JavaScript (line 716-822)

- `setIsActiveControls` toggles the visibility of inactive-related panels and the inactivate-child-groups checkbox based on the IsActive checkbox state and `hfHasChildGroups`.
- `setPrivateLabel` toggles the Private label based on IsPublic.
- `enableRequiredField` flips RequiredFieldValidator for the Inactive Reason dropdown.
- Archive button confirmation dialog.
- `addRelationshipStrengthTooltips` adds context-sensitive tooltips to relationship-strength radio options.

In Obsidian, all of this becomes reactive `<script setup>` watchers on `ref` state.
