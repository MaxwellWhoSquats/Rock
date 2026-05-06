# Sub-feature: Group Scheduling (Rock Scheduler)

## What it is

This is **separate** from the inline-group-schedule feature documented in [`07-locations-and-schedules.md`](07-locations-and-schedules.md). Group Scheduling is the system that lets schedule coordinators assign group members to specific schedule instances at specific locations, with confirmation/decline flows.

The block exposes group-level overrides for the group type's scheduling behavior.

## Trigger conditions

| Condition | Result |
|---|---|
| `groupType.IsSchedulingEnabled == false` | `wpScheduling` panel is hidden ([`2294`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2294)). All scheduling fields below are still saved if the group already had values, since the save block does not gate on this flag. |
| `groupType.IsSchedulingEnabled == true` | Panel shown. All seven fields editable. |
| `group.DisableScheduling == true` (view mode) | `hlGroupScheduler` view-toolbar link is rendered but disabled ([`2770-2778`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2770)). |
| `group.DisableScheduling == true` (view mode) | `hlGroupPlacement` view-toolbar link is rendered but disabled ([`2794-2802`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2794)). |

## Markup region

[`RockWeb/Blocks/Groups/GroupDetail.ascx`](../../RockWeb/Blocks/Groups/GroupDetail.ascx) lines [`250-287`](../../RockWeb/Blocks/Groups/GroupDetail.ascx#L250) (panel widget).

```
wpScheduling (visible if groupType.IsSchedulingEnabled)
├── Row 1
│   ├── col-md-6
│   │   ├── cbDisableGroupScheduling
│   │   │     Label: "Disable Group Scheduling"
│   │   │     Help:  "Checking this box will opt the group out from the group scheduling system."
│   │   └── cbSchedulingMustMeetRequirements
│   │         Label: "Scheduling Must Meet Requirements"
│   │         Help:  "Indicates whether group members must meet the group member requirements before they can be scheduled."
│   └── col-md-6
│       └── cbDisableScheduleToolboxAccess
│             Label: "Disable Schedule Toolbox Access"
│             Help:  "Checking this will hide the group from the schedule toolbox."
├── Row 2
│   └── col-md-6
│       └── ddlAttendanceRecordRequiredForCheckIn
│             Label: "Check-in Requirements"
│             Help:  "Determines if the person must be scheduled prior to checking in."
├── Row 3
│   └── col-md-6
│       └── ddlScheduleConfirmationLogic
│             Label: "Schedule Confirmation Logic"
│             Help:  "Determines if the individual will be asked to Accept or Decline, or if their request will be auto accepted. This setting overrides the group type's setting."
└── Row 4
    ├── col-md-6
    │   └── ppScheduleCoordinatorPerson (EnableSelfSelection)
    │         Label: "Schedule Coordinator"
    │         Help:  "The person who receives notifications about changes to scheduled individuals."
    └── col-md-6
        └── cblScheduleCoordinatorNotificationTypes (CheckBoxList, autopostback)
              Label: "Schedule Coordinator Notification Options"
              Help:  "Specifies the types of notifications the coordinator receives about scheduled individuals.
                     Leave blank to use the Group Type settings, or select one or more options to override them."
              Items: None=0, Accept=1, Decline=2, Self-Schedule=4
```

## Code-behind

| Method | Lines | Notes |
|---|---|---|
| Save block | [`1150-1182`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1150) | Persists scheduling fields and builds `ScheduleCoordinatorNotificationTypes` flag. |
| [`cblScheduleCoordinatorNotificationTypes_SelectedIndexChanged`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1690) | 1690-1730 | Mutually exclusive None vs others. |
| Save Notification Types loop | [`1159-1181`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1159) | Iterates checkboxes and combines into bitmask, treating `None` as exclusive. |
| `LoadDropDowns` | [`3052-3053`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3052) | `BindToEnum<AttendanceRecordRequiredForCheckIn>()` and `BindToEnum<ScheduleConfirmationLogic>(true)` (with blank for "inherit"). |
| `ShowEditDetails` | [`2080-2107`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2080) | Sets all checkbox/dropdown values. Iterates `cblScheduleCoordinatorNotificationTypes` to set `Selected` based on bitmask. |
| `SaveViewState` | [`578`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L578) | Stores `cblScheduleCoordinatorNotificationTypes.SelectedValuesAsInt` in ViewState (used to detect None-vs-other transitions). |

## Persisted fields on Group

| Field | Type | Notes |
|---|---|---|
| `DisableScheduling` | bool | Hides this group from the scheduler. |
| `SchedulingMustMeetRequirements` | bool | Schedulable only if member meets requirements. |
| `DisableScheduleToolboxAccess` | bool | Hides from Schedule Toolbox UI. |
| `AttendanceRecordRequiredForCheckIn` | enum | Determines if scheduling is required prior to check-in. Enum: `Rock.Enums.Group.AttendanceRecordRequiredForCheckIn`. |
| `ScheduleConfirmationLogic` | enum (nullable) | Auto-accept vs Ask vs default. Enum: `Rock.Enums.Group.ScheduleConfirmationLogic`. **Null = inherit from group type.** |
| `ScheduleCoordinatorPersonAliasId` | int? | Recipient of scheduling notifications. |
| `ScheduleCoordinatorNotificationTypes` | flags enum (nullable) | Flags: None=0, Accept=1, Decline=2, SelfSchedule=4. **Null = inherit from group type.** |

## ScheduleConfirmationLogic enum (nullable semantics)

```csharp
public enum ScheduleConfirmationLogic
{
    Ask = 0,
    AutoAccept = 1
}
```

Stored as `Group.ScheduleConfirmationLogic` (`ScheduleConfirmationLogic?`).

| Stored value | Meaning |
|---|---|
| `null` | Inherit from group type. UI shows blank in the dropdown. |
| `Ask` (0) | Explicit override: ask the individual to accept or decline. |
| `AutoAccept` (1) | Explicit override: auto-accept on assignment. |

`LoadDropDowns` calls `ddlScheduleConfirmationLogic.BindToEnum<ScheduleConfirmationLogic>( true )` where the `true` arg adds a blank first item. The blank value maps to null on save.

`ShowEditDetails` ([`2084`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2084)):

```csharp
ddlScheduleConfirmationLogic.SetValue(
    group.ScheduleConfirmationLogic.HasValue
        ? group.ScheduleConfirmationLogic.ConvertToInt().ToString()
        : null
);
```

Save ([`1156`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1156)):

```csharp
group.ScheduleConfirmationLogic =
    ddlScheduleConfirmationLogic.SelectedValueAsEnumOrNull<ScheduleConfirmationLogic>();
```

## ScheduleCoordinatorNotificationTypes flag enum

```csharp
[Flags]
public enum ScheduleCoordinatorNotificationType
{
    None = 0,
    Accept = 1,
    Decline = 2,
    [Description("Self-Schedule")]
    SelfSchedule = 4
}
```

Stored as `Group.ScheduleCoordinatorNotificationTypes` (`ScheduleCoordinatorNotificationType?`).

| Stored value | Meaning |
|---|---|
| `null` | Inherit from group type. CheckBoxList has nothing checked. |
| `None` | Explicit override "send no notifications". The single `None` checkbox is checked. |
| Any combination of `Accept`, `Decline`, `SelfSchedule` (1, 2, 4) | Bitwise OR of selected flags. |

### "None" exclusivity logic

The CheckBoxList has 4 items: None(0), Accept(1), Decline(2), Self-Schedule(4). The `_SelectedIndexChanged` handler at [`1690-1730`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1690) enforces:

1. Capture previous selection from `ViewState["ScheduleCoordinatorNotificationTypes"]`.
2. Determine `wasNoneSelected`, `isNoneSelected`, `anyOthersSelected`.
3. If None just got selected (was off, now on), deselect all others.
4. If None was on, but others are now selected too, deselect None.

Saved as a bitwise flag in [`1159-1181`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1159):

```csharp
ScheduleCoordinatorNotificationType? notificationTypes = null;
foreach ( var li in cblScheduleCoordinatorNotificationTypes.Items.Cast<ListItem>() )
{
    if ( !li.Selected ) continue;

    var selectedType = (ScheduleCoordinatorNotificationType)li.Value.AsInteger();
    if ( selectedType == ScheduleCoordinatorNotificationType.None )
    {
        // Ensure that if "None" is selected, it's the only value that can be saved.
        notificationTypes = ScheduleCoordinatorNotificationType.None;
        break;
    }

    notificationTypes = notificationTypes.HasValue
        ? notificationTypes | selectedType
        : selectedType;
}

group.ScheduleCoordinatorNotificationTypes = notificationTypes;
```

If nothing was checked, `notificationTypes` stays null (inherit). If None was checked, the loop breaks early so other accidentally-checked items are ignored.

### Edit-mode bitmask reading

`ShowEditDetails` ([`2086-2098`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2086)):

```csharp
foreach ( var li in cblScheduleCoordinatorNotificationTypes.Items.Cast<ListItem>() )
{
    var notificationType = (ScheduleCoordinatorNotificationType)li.Value.AsInteger();
    if ( notificationType == ScheduleCoordinatorNotificationType.None )
    {
        // "None" must be explicitly evaluated, otherwise the bitwise operator
        // could return false positives (anything & 0 == 0).
        li.Selected = group.ScheduleCoordinatorNotificationTypes == ScheduleCoordinatorNotificationType.None;
    }
    else
    {
        li.Selected = ( group.ScheduleCoordinatorNotificationTypes & notificationType ) == notificationType;
    }
}
```

In Obsidian, this becomes a watcher on the array model that re-applies exclusivity rules.

## Visibility

`wpScheduling.Visible = groupType != null && groupType.IsSchedulingEnabled` ([`2294`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2294)).

## View-mode quick links

| Link | Visibility | Disabled |
|---|---|---|
| `hlGroupScheduler` | `groupType.IsSchedulingEnabled` AND `GroupSchedulerPage` block attribute set | If `group.DisableScheduling` |
| `hlGroupPlacement` | `groupType != null` AND `GroupPlacementPage` block attribute set | If `group.DisableScheduling` |

The Placement link is special: it carries `SourceGroup`, `AllowMultiplePlacements=false`, and `ReturnUrl` query params ([`2785-2790`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2785)).

## Persisted state

All fields are scalar columns on the `Group` table. No collections, no foreign-key cascade beyond `ScheduleCoordinatorPersonAliasId -> PersonAlias`.

## Permission inheritance

No separate gate; visible whenever the user has EDIT on the group AND the group type allows scheduling.

## Edge cases

- The save block always runs these field assignments unconditionally (no `if IsSchedulingEnabled` guard). If the group type is changed from a scheduling-enabled type to a non-scheduling type, the existing values stay in the database but the panel hides. Toggling back exposes them.
- `ScheduleCoordinatorPersonAliasId` is set from `ppScheduleCoordinatorPerson.PersonAliasId`, which is null if no person selected. Clearing the person picker explicitly nulls this column.
- The CheckBoxList autopostback reloads the entire panel; this is jarring UX in WebForms. Vue should mimic the rules without postbacks.

## Phase considerations

Pure scalar settings on the Group entity. No state collections, no dialogs. Good candidate for the same phase that handles "core group fields" (Name, Description, IsActive, etc.) since it's all just form fields on the same panel.

## Open questions / flag for spec phase

- The save runs unconditionally regardless of `IsSchedulingEnabled`. Should the new block guard this, or preserve the existing "always save" behavior to keep parity with WebForms?
- The View-mode toolbar links are disabled (not hidden) when `DisableScheduling` is true. Worth UX review whether to fully hide them.

## Notes

- The `ScheduleConfirmationLogic` enum has a "(blank)" first item meaning "inherit from group type". Keep this dual-state in the bag (nullable enum).
- `ScheduleCoordinatorNotificationTypes` is similarly nullable. null = inherit from group type, anything else = explicit override (including None which means "no notifications").
- The `[Description("Self-Schedule")]` on the SelfSchedule enum value means `EnumExtensions.ConvertToString(true)` produces "Self-Schedule" (with hyphen), but the markup hardcodes the same text. Either approach works.
