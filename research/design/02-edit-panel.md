# Edit Panel Walkthrough

Figma node: `4670-25350` ("Add/Edit Mode" canvas).
Main frame: `4532-20835` (Edit, 1400 × 7527).
10 collapsible sections + 4 modals + alternate-state variants + designer help callouts + UX/UI Notes column (`4900-7650`).

Visual references: top-level [screenshots/00-edit-panel-overview.png](screenshots/00-edit-panel-overview.png), per-section [screenshots/edit-section-NN.png](screenshots/), per-modal [screenshots/edit-modal-NN-*.png](screenshots/), inactive flow [screenshots/edit-section-alt-state.png](screenshots/), designer notes [screenshots/edit-designer-notes.png](screenshots/).

## Chrome

Same panel header / subheader / footer pattern as the View panel ([01-view-panel.md](01-view-panel.md)). Differences in edit mode:

- Footer holds Save (primary) + Cancel.
- Right rail of footer holds icon buttons that are mode-specific (e.g., shortcut shortcuts).
- Header labels for Group Type and Campus are still shown when editing an existing group.
- **Add new variant**: panel header shows just "Add Group" text; no labels, no chrome icons. Group Type label disappears from header (since it's now an editable required dropdown in Section 1).

## Section structure

The edit body is a single `Panel Body w/ Sections` container holding 10 collapsible sections in this order:

| # | Section title | Figma id | Approx height | Maps to WebForms |
|---|---|---|---|---|
| 1 | (no header) Top fields | `4552:29308` | 453 px | Top-of-form fields (Name, Active, Description, IsPublic) plus new Image |
| 2 | General | `4559:6184` | 1534 px | wpGeneral (split into 3 stacks) |
| 3 | RSVP | `4552:29310` | 230 px | wpRsvp |
| 4 | Meeting & Scheduling Details | `4566:15288` | 1273 px | wpMeetingDetails + wpScheduling MERGED |
| 5 | Group Attributes | `4670:22454` | 428 px | wpGroupAttributes |
| 6 | Member Attributes | `4576:15421` | 577 px | wpGroupMemberAttributes |
| 7 | Requirements | `4576:26533` | 625 px | wpGroupRequirements |
| 8 | Chat | `4578:14186` | 967 px | wpChat |
| 9 | Group Sync Settings | `4552:33489` | 333 px | wpGroupSync |
| 10 | Member Workflows | `4584:21233` | 389 px | wpMemberWorkflowTriggers |

The order has shifted slightly: WebForms put Member Attributes before Group Attributes; the design swaps them so Group Attributes (values) come first, then Member Attributes (definitions).

Each section renders as: `Section Header` (icon + name + collapse chevron) + `Section Body` (containing one or more `Section Stack`s).

A `Section Stack` is a horizontal layout: 25%-ish left column with a heading + description, 75%-ish right column with the controls.

A `Conditional Well` is a left-bordered subdued block that wraps controls visible only when a parent toggle is on.

## Section 1 — Top fields (no section header)

Frame: [edit-section-01.png](screenshots/edit-section-01.png).

Always-visible fields at the top of the panel body, before the first collapsible section.

- **Name** (required, marked with red dot) — text input. Half-width.
- **Active** — checkbox, top-right.
- **Description** — textarea, full-width.
- **Show Publicly** — inline checkbox with info icon. Purple label (changed).
- **Image** — image uploader with placeholder + Upload button. **NEW field.**

### Inactive flow (alternate state)

Frame: [edit-section-alt-state.png](screenshots/edit-section-alt-state.png).

When Active is unchecked, a conditional well slides in immediately below Name + Active and contains:

- **Inactive Reason** (required, with info icon) — dropdown.
- **Inactive Note** (with info icon) — textarea.
- **Also Inactivate Child Groups** — inline checkbox with info icon. Purple label (changed from "Inactivate Child Groups").

The conditional well is shown only if `Active === false` (and the user clicks the box). If the group has no descendants, the "Also Inactivate Child Groups" checkbox is still rendered (no longer gated by `hfHasChildGroups` like WebForms; the design suggests showing it regardless).

## Section 2 — General

Frame: [edit-section-02-large.png](screenshots/edit-section-02-large.png).

Three stacks:

### Stack 1: Overview

Description: "Basic information that identifies this group and places it within the organization."

- **Group Type** (info icon) — read-only static text on existing groups; required dropdown when adding. Per designer note, on Add the dropdown is required and the static field doesn't show.
- **Parent Group** — dropdown / picker.
- **Campus** — dropdown.
- **Status** — dropdown (DefinedValue picker, sourced from `GroupType.GroupStatusDefinedTypeId`). Hidden if group type doesn't define a status type.

### Stack 2: Administration & Security

Description: "Administrative, capacity, and security settings that control how this group is managed."

- **Group Administrator** (purple, info icon) — person picker. Hidden if `GroupType.ShowAdministrator == false`. Label uses `GroupType.AdministratorTerm`.
- **Group Capacity** (info icon) — number input with "Members" suffix label. Hidden if `GroupType.GroupCapacityRule == None`. Required if `GroupType.IsCapacityRequired`.
- **Require Signed Document** (purple, info icon) — dropdown. Filters to legacy templates per `SignatureDocumentTemplateService.GetLegacyTemplates()`.
- **Member Record Source** (purple, info icon) — DefinedValue dropdown. Hidden if `GroupType.AllowGroupSpecificRecordSource == false`.
- **Enable as Security Role** (purple, info icon) — inline checkbox. Hidden unless current user is in GROUP_ADMINISTRATORS.
- **Conditional well** when Enable as Security Role checked:
  - **Security Level** (info icon) — radio: None / High / Extreme.
  - Note: WebForms calls this "ElevatedSecurityLevel" with the same enum but the dropdown is the standard `[Flags]` enum binding. The design uses a radio.

### Stack 3: Relationships

Description: "Relationship settings that define how connection strength is calculated for this group."

- **Override Relationship Strength** — inline checkbox. Hidden if `GroupType.IsPeerNetworkEnabled == false`.
- **Conditional well** when Override Relationship Strength is checked:
  - **Relationship Strength** (info icon) — radio: **None / Casual / Close / Deep**. (Renamed from None / Basic / Strong / Intense; underlying integer values 0/5/10/20 likely unchanged.)
  - **Show Advanced Relationship Settings** (purple) — inline switch.
  - **Conditional well** when Show Advanced is on AND strength != None:
    - **Enable Relationship Growth Over Time** (info icon) — checkbox.
    - Helper callout: "Use the matrix to adjust relationship scores based on group member relationships. For example, if members have strong connections with group leaders but little interaction with other non-leaders, you can reduce or remove the score percentage between non-leaders."
    - 2x2 matrix: Leader/Non-Leader x Leader/Non-Leader; cells are percentage inputs (number + % suffix). The "Capacities" label that exists in WebForms above the matrix is removed.

## Section 3 — RSVP

Frame: [edit-section-03.png](screenshots/edit-section-03.png).

Single stack: **Reminder Settings**

Description: "Settings that control RSVP reminders for this group."

- **RSVP Reminder Communication** (purple, info icon) — labeled "RSVP Reminder" in the captured frame; this appears to be a static info text rather than a dropdown. Likely a placeholder; expect a `RockDropDownList` here when implemented (paralleling WebForms `ddlRsvpReminderSystemCommunication`).
- **RSVP Reminder Lead Time** (purple, info icon) — number input + "Days" suffix label. WebForms uses a slider 0-30; design uses a numeric input.

Hidden entirely if `GroupType.EnableRSVP == false`.

## Section 4 — Meeting & Scheduling Details

Frame: [edit-section-04.png](screenshots/edit-section-04.png).

This section MERGES WebForms wpMeetingDetails and wpScheduling per the designer notes.

Three stacks:

### Stack 1: Overall Group Schedule

Description: "Provide the schedule used to represent when this group meets as a whole."

- **Group Schedule** (info icon) — radio: None / Weekly / Custom / Named.
- **Conditional well** when Weekly:
  - **Date & Time** — date picker + time picker (rendered as DayOfWeek + Time controls).
- **Conditional well** when Custom:
  - ScheduleBuilder (iCal). Not visible in captured frame.
- **Conditional well** when Named:
  - SchedulePicker (single-select).

Visibility of the radio options follows `GroupType.AllowedScheduleTypes` flags.

### Stack 2: Group Location Schedules

Description: "Provide the location-specific schedules used for this group."

- Editable table with columns: Location, Type, Schedule(s), edit (pencil), delete (×).
- Captured rows:
  - "1122 W. Road St." — "Home" — "Friday 4:30pm"
  - "1122 W. Road St." — "Home" — "Saturday 6:00pm"
- Last row is empty + "+" button to add (opens the Add Group Location Schedules modal).

Hidden entirely if `GroupType.LocationSelectionMode == None`.

### Stack 3: Member Scheduling & Check-in

Description: "Scheduling behavior and check-in access for this group."

- **Disable Group Member Scheduling** (purple, info icon) — checkbox.
- **Hide from Schedule Toolbox** (purple, info icon) — checkbox. (Was "Disable Schedule Toolbox Access" in WebForms.)
- **Require Member Requirements for Scheduling** (purple, info icon) — checkbox.
- **Confirmation Behavior** (purple, info icon) — dropdown. Was "Schedule Confirmation Logic" in WebForms.
- **Schedule Coordinator** (purple, info icon) — person picker.
- **Enable Schedule Coordinator Notifications** (info icon) — checkbox.
- **Conditional well** when Enable Schedule Coordinator Notifications is checked:
  - **Coordinator Notifications** (purple, info icon) — checkbox list: Decline / Accept / Self-Schedule. (No "None" option per design — see open question.)
- **Who Can Check-in** (purple, info icon) — radio: Anyone / Anyone (Scheduled Members Pre-selected) / Only Scheduled Members. Was "Check-in Requirements" with `AttendanceRecordRequiredForCheckIn` enum.

Stack hidden entirely if `GroupType.IsSchedulingEnabled == false`.

## Section 5 — Group Attributes

Frame: [edit-section-05.png](screenshots/edit-section-05.png).

Two stacks observed (captured frame); structure depends on the group type's group-attribute definitions, so this is dynamic.

### Stack 1: Set Additional Attributes

Description: "Configure additional fields and settings for this item. These attributes store extra information and can be used for filtering, personalization, or display."

- One control per attribute defined for this group type.
- Captured example: **Study Preference** (info icon) — radio: Bible / Book / None.

### Stack 2: Group Goals

Description: "Set the spiritual purpose for this group meeting."

- **Goal of Group** (info icon) — radio: Growth / Evangelism / Community.

These two stacks are runtime-rendered from the group type's group-attribute set, similar to the existing WebForms `phGroupAttributes` DynamicPlaceholder. The design implies `AttributeValuesContainer` will render them, possibly with attribute categories (`GroupAttribute.Category`) becoming stack headers — that explains the "Set Additional Attributes" / "Group Goals" labels (captured group has its attributes split across two categories).

Hidden entirely if the group type has no group attributes or all attributes are excluded by EDIT auth.

## Section 6 — Member Attributes

Frame: [edit-section-06.png](screenshots/edit-section-06.png).

Two stacks:

### Stack 1: Inherited Attributes

Description: "Member Attributes inherited from this group's Group Type settings."

- Read-only table: Attribute name, Description, "(Inherited from {GroupType-link})".
- Captured rows: Stage of Life, Age Range — both inherited from "General".

### Stack 2: Configure Group Member Attributes

Description: "Group Member Attributes apply to all of the group members in every group of this type. Each member will have their own value for these attributes."

- Editable table: Attribute name, Description, Required (checkmark), edit (pencil), delete (×).
- Reorder handle (≡) on left of each row (suggesting reorder is preserved).
- Captured row: Spiritual Maturity — "How mature the person is." — Required ✓.
- "+" button to add new (opens the AttributeEditor modal — same pattern as WebForms).

Stack 2 visibility per `GroupType.AllowSpecificGroupMemberAttributes` and ADMINISTRATE auth.

The design preserves the WebForms two-grid pattern for inherited vs. custom attributes.

## Section 7 — Requirements

Frame: [edit-section-07.png](screenshots/edit-section-07.png).

Two stacks:

### Stack 1: Inherited Requirements

Description: "These requirements are inherited from the group type and apply to this group."

- Read-only table: name, group role, age classification, "(Inherited from {GroupType-link})".
- Captured rows: Background Check Required - Leader - Adults; Age Range - Member - Adults.

### Stack 2: Configure Group Requirements

Description: "Define the requirements that members must meet to join or remain in this group type."

- Editable table: Type, Group Role, Age Classification, Require Before Adding Members (checkmark), edit, delete.
- Captured rows: Background Check - Leader - Adults - ✓; Connection Status - Member - Adults - ✓.
- "+" button to add new (opens the Add Group Requirement modal).

Note: the design's editable grid has FEWER columns than the WebForms grid. Removed columns vs WebForms:
- Data View (icon)
- Can Expire
- Type (RequirementCheckType enum)

These may be moved into the modal-only or surfaced differently. Confirm with designer whether the grid is intentionally simplified or whether columns toggle in/out.

Stack 2 visibility per `GroupType.EnableSpecificGroupRequirements` and ADMINISTRATE auth.

## Section 8 — Chat

Frame: [edit-section-08.png](screenshots/edit-section-08.png).

Two stacks. Note: every Yes/No/Inherit dropdown in WebForms becomes a **radio button group** in the design.

### Stack 1: Group Chat

Description: "Enable groups of this type to participate in the chat system as chat channels."

- **Enable Group Chat** (info icon) — radio: Inherit from Group Type / No / Yes.

### Stack 2: Group Chat Settings

Description: "Control how group chat works for this group type."

- **Allow Members to Leave Channel** (info icon) — radio: Inherit / No / Yes.
- **Public Channel** (info icon) — radio: Inherit / No / Yes.
- **Always Show Channel** (info icon) — radio: Inherit / No / Yes.
- **Push Notification Mode** (info icon) — radio: Inherit from Group Type / All Messages / Mentions / Silent.
- **Channel Avatar** (info icon) — image uploader.

Section hidden entirely when `ChatHelper.IsChatEnabled == false` OR `GroupType.IsChatAllowed == false`. Controls disabled when `IsSystem == true`.

The control descriptions in the captured frame use "of this type" language ("Enable groups of this type to participate"). This wording is misleading — the controls are for the group, not the group type. May be a copy bug to flag.

## Section 9 — Group Sync Settings

Frame: [edit-section-09.png](screenshots/edit-section-09.png).

Single stack: **Configure Sync Rules**

Description: "Define the rules used to sync group members from data views into this group."

- Editable table: Role Name, Data View Name, Sync Interval, Last Sync, edit, delete.
- Captured row: Member - 35 and Older - 12 hours - 4/10/2026 11:00 AM.
- "+" button to add new (opens Add Group Sync Rule modal).

Section hidden when ADMINISTRATE auth missing or group type doesn't allow group sync.

## Section 10 — Member Workflows

Frame: [edit-section-10.png](screenshots/edit-section-10.png).

Single stack: **Configure Workflows**

Description: "Workflows that run automatically when a member is added, updated, or removed from the group."

- Editable table: Trigger Name, Workflow, Trigger Event, Active (checkmark), edit, delete.
- **No reorder handle visible** in the captured frame — this differs from WebForms which supports reorder via `gMemberWorkflowTriggers.GridReorder`. Confirm whether reorder is preserved or intentionally dropped.
- Captured rows: New Person Info - Get Initial Info - Member Added to Group - ✓; Member Permissions - Update Permissions - Member Role Changed - ✓.
- "+" button to add new (opens Add Group Member Workflow modal).

Section hidden when group type doesn't allow specific group member workflows.

## Footer

- **Save** (primary, orange).
- **Cancel** (secondary, link-style).

## Modals

### Add Group Location Schedules

Frame: `4584:30305`. Screenshot: [edit-modal-01-location.png](screenshots/edit-modal-01-location.png).

Title: **"Add Group Location Schedules"** (note plural "Schedules"; differs from WebForms "Group Location").

Tabs: Member Location / Other Location. (Tabs only shown when both modes are allowed by group type.)

**Member Location tab**:
- **Member** (info icon) — dropdown of `{Member} {AddressType} ({Address})` for each Group Member's family addresses.

**Other Location tab** (not in captured frame but present per WebForms):
- LocationPicker with mode flags from group type.

Below tabs:
- **Location Type** (purple, info icon) — dropdown.
- **Location Schedule(s)** (purple, info icon) — schedule picker (multi-select). Captured shows "4:30 (test), 6:00 (test)" with X to clear.
- **Conditional well** when at least one schedule is selected:
  - Helper callout: "Set the **person capacity** for each of the configured schedules below."
  - Table with columns: Minimum / Desired / Maximum.
  - One row per selected schedule with three number inputs each.

Footer: Save / Cancel.

### Add Group Requirement

Frame: `4584:30334`. Screenshot: [edit-modal-02-requirement.png](screenshots/edit-modal-02-requirement.png).

Title: **"Add Group Requirement"**.

Fields:
- **Group Requirement Type** (info icon) — dropdown.
- **Applies to Role** (purple, info icon) — dropdown. (Was "Applies to Group Role" in WebForms.)
- **Applies to Age Group** (purple, info icon) — radio: All / Adults / Children. (Was "Applies to Age Classification" in WebForms; same enum.)
- **Applies to Data View** (info icon) — DataView picker.
- **Allow Leader Override** (purple, info icon) — checkbox. (Was "Allow Leaders to Override".)
- **Require Before Adding Member** (purple, info icon) — checkbox. (Was "Members must meet this requirement before adding".)

Removed fields vs WebForms modal:
- Due Date picker (visible when DueDateType = ConfiguredDate)
- Due Date Group Attribute dropdown (visible when DueDateType = GroupAttribute)

These DueDate controls are not visible in the captured frame. Either:
(a) The design reuses the DueDateType-driven conditional well from WebForms but the captured screenshot is in a state where neither is shown.
(b) The design simplifies this to drop due-date overrides at the group level.

Confirm with designer.

### Add Group Sync Rule

Frame: `4584:30354`. Screenshot: [edit-modal-03-sync.png](screenshots/edit-modal-03-sync.png).

Title: **"Add Group Sync Rule"** (was "Group Sync Settings").

Fields:
- **Data View** (required, purple, info icon) — DataView picker. (Was "Sync Data View".)
- **Assigned Role** (required, purple, info icon) — dropdown. (Was "Group Role to Assign".)
- **Sync Frequency** (purple, info icon) — segmented control (Mins / Hours / Days) + slider 1-31.
  - The slider's label shows the current value with the segmented control's unit (e.g., "15 days" — the captured screenshot shows the slider at 15 with "Days" highlighted).
  - WebForms uses the standard `IntervalPicker` with default 12 hours. The design replaces it.
- **Create Login During Sync** (info icon) — checkbox.
- **Welcome Communication** (info icon) — dropdown.
- **Exit Communication** (info icon) — dropdown.

Footer: Save / Cancel.

### Add Group Member Workflow

Frame: `4584:30374`. Screenshot: [edit-modal-04-workflow.png](screenshots/edit-modal-04-workflow.png).

Title: **"Add Group Member Workflow"**.

Fields:
- **Workflow Trigger Name** (required, purple) — text input.
- **Workflow Type** (required, purple, info icon) — workflow type picker. (Was "Start Workflow".)
- **Trigger Event** (required, purple, info icon) — dropdown. Captured shows "Member Added to Group". (Was "When".)
- **Active** — checkbox in top-right of modal body.
- **Conditional well** below Trigger Event with sub-controls per trigger event type:
  - Captured shows MemberAddedToGroup which displays:
    - **Status** — dropdown showing "Pending".
    - **Role** — dropdown showing "Leader".
  - Other event types should show their respective sub-controls per WebForms `ShowTriggerQualifierControls`. Confirm visibility map for each.

Footer: Save / Cancel.

The 7-tuple TypeQualifier serialization from WebForms ([../webforms/13-member-workflow-triggers.md](../webforms/13-member-workflow-triggers.md)) is presumably preserved; the bag exposes individual fields (Status, Role, FromStatus, FromRole, FirstTime, ShowNote, RequireNote) and the C# block serializes them.

## Help callouts

Four `Callout/Help` instances are placed on the canvas alongside the modals (frames `4602:8901`, `4602:8910`, `4602:8928`, `4602:8937`). These are designer help notes embedded in the canvas, not part of the running UI. Useful as supporting context but not implementation requirements.

## Block settings

Per the right-side designer notes column, the 21 block attributes are reorganized into two named sections in the block-settings panel:

### Section 1: General Settings

| Attribute | Renamed from |
|---|---|
| Group Types: Include | Group Types Include |
| Group Types: Exclude | Group Types Exclude |
| Security Role Groups Only | LimittoSecurityRoleGroups |
| Navigation Group Types Only | LimitToShowInNavigationGroupTypes |
| Map Style | Map Style |
| Show Copy Button | Show Copy Button |
| Exclude Inactive Campuses | PreventSelectingInactiveCampus |
| Show Location Addresses | Show Location Addresses |
| Enable Group Tags | Enable Group Tags |
| Grant Creator Admin Rights | AddAdministrateSecurityToGroupCreator |

The underlying `AttributeKey` constants must NOT change (existing block-instance attribute values would be orphaned). Only the user-facing labels change.

### Section 2: Page Routing

All 11 LinkedPage attributes from WebForms preserved with possibly cleaner labels:

- Group Map Page, Attendance Page, Registration Instance Page, Event Item Occurrence Page, Content Item Page, Group List Page, Fundraising Progress Page, Group History Page, Group Scheduler Page, Group RSVP List Page, Group Placement Page.

## Open questions / flag for spec phase

1. **DueDate controls in Add Group Requirement modal** — Captured screenshot doesn't show DatePicker or GroupAttribute dropdown. Either captured in default state or intentionally removed. Confirm.
2. **Workflow trigger reorder** — Captured Member Workflows table has no reorder handle. WebForms supports reorder. Confirm intent.
3. **RSVP Reminder Communication is text not dropdown** — Captured frame shows "RSVP Reminder" as static text where a dropdown is expected. Check if this is a placeholder state in the design.
4. **Coordinator Notifications has no None option** — In WebForms, "None" is one of four choices in the checkbox list, mutually exclusive with the others. Design has only Decline / Accept / Self-Schedule. Confirm whether "no checkboxes ticked" is the new way to express None.
5. **Sync Frequency control type** — Segmented `Mins / Hours / Days` plus a 1-31 slider. WebForms uses an `IntervalPicker`. Need to confirm whether to build a new component or reuse / re-style the IntervalPicker.
6. **Group Requirement grid columns** — Design has fewer columns (Type, Role, Age Classification, Require Before Adding Members) than WebForms (also Data View, Can Expire, RequirementCheckType). Confirm whether columns are dropped or just collapsed in the captured screenshot.
7. **Section 8 Chat copy wording** — "Enable groups of this type to participate" reads like a group-type-level setting, not a per-group override. Likely a copy bug; should be "Enable this group" or similar.
8. **Image uploader data source** — Resolved by 00-architecture.md Q8: Group has no photo column today; a new `Group.PhotoId` (nullable int → BinaryFile, mirroring `Person.PhotoId` at [Person.cs:242](Rock/Model/CRM/Person/Person.cs:242)) is added in Phase 2 with a migration + nav property + codegen regen. The image uploader binds to that new FK and applies the chat-avatar `IsTemporary` toggle pattern.
9. **Section 5 Group Attributes structure** — The design splits group attributes into named stacks ("Set Additional Attributes" / "Group Goals"). Verify whether stack labels come from `Attribute.Category` or are designer-set captions. If from category, we need bag fields exposing per-category groupings.
10. **Add new vs Edit visual distinction** — "Add Group" panel header has no Group Type label but the dropdown is in Section 2 stack 1. Confirm whether Section 1 (top fields) is reordered for Add (e.g., Group Type pulled to the top) or whether the dropdown stays in Section 2.
11. **Trailblazer Settings** — designer-listed "New" item with no captured visual. Awaiting clarification.
12. **Hide existing buttons in archived state** — design doesn't show an Archived state explicitly. Need designer-confirmed treatment.
