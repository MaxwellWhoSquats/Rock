# Sub-feature: RSVP Configuration

## What it is

When the group type has `EnableRSVP = true`, this panel exposes two settings:
1. RSVP Reminder System Communication.
2. RSVP Reminder Offset Days (0-30).

Both are overridable at the group level. **But only when the group type doesn't have a value set.** When the group type has a value, the group type setting wins and the per-group control is read-only.

## Trigger conditions

| Condition | Result |
|---|---|
| `groupType.EnableRSVP == false` | `wpRsvp.Visible = false`. Save block at [`1144-1147`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1144) nulls both group fields. |
| `groupType.EnableRSVP == true` AND `groupType.RSVPReminderOffsetDays.HasValue` | Slider shown and disabled (read-only). Display value is the group type's value. Save block nulls `group.RSVPReminderOffsetDays` regardless of slider value. |
| `groupType.EnableRSVP == true` AND `groupType.RSVPReminderOffsetDays == null` | Slider editable. Default = group's value (or 0 if new). |
| `groupType.EnableRSVP == true` AND `groupType.RSVPReminderSystemCommunicationId.HasValue` | Communication dropdown shown and disabled. Save block nulls `group.RSVPReminderSystemCommunicationId`. |
| `groupType.EnableRSVP == true` AND `groupType.RSVPReminderSystemCommunicationId == null` | Communication dropdown editable. |

## Markup region

[`RockWeb/Blocks/Groups/GroupDetail.ascx`](../../RockWeb/Blocks/Groups/GroupDetail.ascx) lines [`198-209`](../../RockWeb/Blocks/Groups/GroupDetail.ascx#L198).

```
wpRsvp
├── col-md-6
│   └── ddlRsvpReminderSystemCommunication (filtered to RSVP_CONFIRMATION category)
│         Label: "RSVP Reminder System Communication"
│         Help:  "The System Communication that should be sent to remind group members to RSVP for group events."
└── col-md-6
    └── rsRsvpReminderOffsetDays (RangeSlider 0-30, default 1)
          Label: "RSVP Reminder Offset Days"
          Help:  "The number of days prior to a group event occurrence to send the RSVP reminder."
          MinValue: 0, MaxValue: 30, SelectedValue: 1
```

## Code-behind

| Method | Lines | Notes |
|---|---|---|
| [`SetRsvpControls`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2463) | 2463-2505 | Visibility + read-only. |
| `LoadDropDowns` body | [`3029-3050`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3029) | Populates `ddlRsvpReminderSystemCommunication` from `SystemCommunication` rows in the RSVP_CONFIRMATION category. |
| `ShowEditDetails` body | [`1993-1995`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1993) | Sets dropdown + slider values. |
| `btnSave_Click` body | [`1119-1148`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1119) | Persists with override-vs-inherit logic. |
| `ddlGroupType_SelectedIndexChanged` | [`1567`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1567) | `SetRsvpControls(groupType, null)` resets values when group type changes. |

## Persisted fields on Group

| Field | Type | Notes |
|---|---|---|
| `RSVPReminderOffsetDays` | int? | null = inherit |
| `RSVPReminderSystemCommunicationId` | int? | null = inherit |

No FK cascade. `RSVPReminderSystemCommunicationId` references `SystemCommunication.Id` with no cascade behavior.

## Save logic

```csharp
if ( group.GroupType.EnableRSVP )
{
    // Offset Days
    if ( group.GroupType.RSVPReminderOffsetDays.HasValue )
        group.RSVPReminderOffsetDays = null; // group type wins
    else
        group.RSVPReminderOffsetDays = rsRsvpReminderOffsetDays.SelectedValue;

    // Reminder communication
    if ( group.GroupType.RSVPReminderSystemCommunicationId.HasValue )
        group.RSVPReminderSystemCommunicationId = null; // group type wins
    else
        group.RSVPReminderSystemCommunicationId = ddlRsvpReminderSystemCommunication.SelectedValueAsInt();
}
else
{
    group.RSVPReminderOffsetDays = null;
    group.RSVPReminderSystemCommunicationId = null;
}
```

## SetRsvpControls (visibility / enabled-state)

```csharp
showRsvp = groupType.EnableRSVP;

// Defaults from group type
offsetDays = groupType.RSVPReminderOffsetDays;
reminderSystemCommunicationId = groupType.RSVPReminderSystemCommunicationId;

// Read-only if group type has values
isReadOnly_Offset = groupType.RSVPReminderOffsetDays.HasValue;
isReadOnly_Reminder = groupType.RSVPReminderSystemCommunicationId.HasValue;

if ( !isReadOnly_Offset )
{
    rsRsvpReminderOffsetDays.Enabled = true;
    offsetDays = group.RSVPReminderOffsetDays; // override exists
}

if ( !isReadOnly_Reminder )
{
    ddlRsvpReminderSystemCommunication.Enabled = true;
    reminderSystemCommunicationId = group.RSVPReminderSystemCommunicationId;
}

wpRsvp.Visible = showRsvp;
rsRsvpReminderOffsetDays.SelectedValue = offsetDays.GetValueOrDefault( 0 );
ddlRsvpReminderSystemCommunication.SetValue( reminderSystemCommunicationId );
```

## Visibility

`wpRsvp.Visible = groupType.EnableRSVP`. If false, the group's existing `RSVPReminder*` fields are nulled on save.

## View-mode quick link

`hlGroupRSVP` is visible when `groupType.EnableRSVP == true` and `GroupRSVPPage` block attribute is configured ([`2755-2764`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2755)).

## Category filter

The system communication dropdown is filtered by `Category.SYSTEM_COMMUNICATION_RSVP_CONFIRMATION` GUID ([`3036`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3036)). Only communications in that category are eligible reminders.

```csharp
var rsvpReminderCategoryId = CategoryCache.GetId( Rock.SystemGuid.Category.SYSTEM_COMMUNICATION_RSVP_CONFIRMATION.AsGuid() );
var rsvpReminderCommunications = communicationService.Queryable()
    .AsNoTracking()
    .Where( c => c.CategoryId == rsvpReminderCategoryId )
    .OrderBy( t => t.Title )
    .Select( a => new { a.Id, a.Title } );
```

## Permission inheritance

No separate gate beyond EDIT-on-group.

## Edge cases

- Slider default is 1 day, range 0-30. If group is new and group type has no override, slider shows 0 because `offsetDays.GetValueOrDefault(0)` evaluates to 0 (the type's null overrides the markup default of 1).
- The dropdown is populated from `CreateSystemCommunicationDropDownLists` ([`4291-4313`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4291)) which loads ALL system communications, NOT filtered by category. Then `LoadDropDowns` ([`3029-3050`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3029)) adds the category-filtered list. The two methods produce inconsistent dropdown contents depending on which one runs last (during postback flows). Worth verifying behavior.
- When `groupType.EnableRSVP` is true but BOTH group type fields are set (offset AND comm), the entire panel is essentially read-only because both controls are disabled.

## Phase considerations

Trivial sub-feature. Bundle with core edit form.

## Open questions / flag for spec phase

- The dropdown population is split between `LoadDropDowns` (filters by category) and `CreateSystemCommunicationDropDownLists` (loads all). The latter also populates the Welcome/Exit communication dropdowns for Group Sync. This shared method is fragile. The new block should populate each dropdown independently with its own query.
- Confirm whether the slider should default to 1 (the markup value) or 0 (the runtime fallback).
- Read-only-when-group-type-has-value is a legacy behavior. Confirm whether the new block should keep this or always allow override.

## Notes

- The "group type wins" pattern is: if the group type sets a value, the group's value is forced to null. This keeps "inherited" semantics clean.
- Slider default is 1 day, range 0-30.
