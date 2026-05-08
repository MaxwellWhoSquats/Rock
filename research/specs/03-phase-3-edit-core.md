---
author: Maxwell Eley
date_created: 2026-05-07
summary: >-
  Phase 3 implementation spec for the GroupDetail Obsidian conversion. Replaces
  the Phase 1 edit-panel placeholder with the full edit core: top fields,
  General / RSVP / Scheduling / Chat sections, Save block action with all
  scalar persistence, GroupType reactive cascade (Q2 Approach B via the new
  GetGroupTypeOptions block action), Group photo + chat-channel-avatar
  uploaders (IsTemporary BinaryFile pattern), the Add path with
  `?ParentGroupId=N` defaulting and `?autoEdit=true` handling, Trailblazer
  per-field styling on General-section controls (Q6), and the L1 fix-during
  for the WebForms duplicate-code-block bug. Attributes / Requirements / Sync
  / Triggers / Locations editing all stay deferred to later phases.
contributors: []
---

# Phase 3: Edit panel core (Top fields + General + RSVP + Scheduling + Chat)

## Context

Phase 1 ([01-phase-1-shell-and-view.md](01-phase-1-shell-and-view.md)) shipped the block shell, the View panel, the terminal actions (Delete / Archive / Copy), and a placeholder edit-panel partial that just renders a "coming in Phase 3" notification. Phase 2 ([02-phase-2-complete-view-panel.md](02-phase-2-complete-view-panel.md)) closed the view-panel gaps (Group Image hero + Meeting Locations card) and added the new `Group.PhotoId` column. Phase 3 replaces the edit-panel placeholder with the full edit core, lights up the Save block action, and ships the photo uploader against the column Phase 2 added. By the end of Phase 3 the View → Edit → Save flow round-trips for all scalar fields on Group: Name, Description, IsActive (with Inactive flow conditional well), IsPublic, GroupType, ParentGroup, Campus, GroupCapacity, GroupAdministratorPersonAlias, status, peer-network overrides, RSVP fields, scheduling fields, and chat fields. Sub-feature panels (Attributes, Requirements, Sync, Triggers, Locations) stay deferred. Architectural decisions for this phase are governed by [00-architecture.md](00-architecture.md), specifically Q2 (cascade Approach B), Q6 (Trailblazer per-field), Q8 (photo uploader pattern), Q10 (Sections & Stacks + Conditional Well core components), Q11 (Coordinator Notifications no-None), and Q12 (L1 fix-during).

## Behavior delivered

- **Edit transition**: clicking Edit (or visiting `/Group/{N}?autoEdit=true`) opens the edit-mode partial populated with the full editable bag. Cancel returns to view mode without saving (existing groups) or navigates to `GroupListPage` / parent (Add path).
- **Add path**: visiting `/Group/0` (or `/Group/0?ParentGroupId=N`) opens the Add panel. Group Type is a required dropdown; ParentGroup defaults from `?ParentGroupId=N` per Phase 1's declared `PageParameterKey.ParentGroupId`. The header chrome simplifies to "Add Group" (per design 03-net-new-features.md item #13).
- **Section 1 (Top fields)**: Name (required), Active (with Inactive flow conditional well — Inactive Reason required + Inactive Note + "Also Inactivate Child Groups" checkbox), Description, "Show Publicly" (renamed from "Public" per design field-level-rename map), Image uploader (writes `Group.PhotoId` using the IsTemporary BinaryFile pattern from Phase 2 / chat-avatar parity).
- **Section 2 Stack 1 (Overview)**: Group Type (read-only on existing groups, required dropdown on Add), Parent Group, Campus, Status. Honors `LimittoSecurityRoleGroups`, `GroupTypes` / `GroupTypesExclude`, `LimitToShowInNavigationGroupTypes`, `PreventSelectingInactiveCampus` block attributes.
- **Section 2 Stack 2 (Administration & Security)**: Group Administrator (auto-labeled from `GroupType.AdministratorTerm`; hidden when `GroupType.ShowAdministrator == false`), Group Capacity (with "Members" suffix per design rename map), Required Signature Document, Member Record Source (when `GroupType.AllowGroupSpecificRecordSource`), Enable as Security Role (visible only to GROUP_ADMINISTRATORS members per WebForms parity), Security Level (visible only when IsSecurityRole && admin user, conditional well; renders as radio per design C6).
- **Section 2 Stack 3 (Relationships)**: Override Relationship Strength (conditional well wrapping the four multipliers + relationship-strength dropdown using renamed Casual / Close / Deep terms; underlying enum unchanged). "Show Advanced Relationship Settings" toggle reveals the four `*RelationshipMultiplierOverride` decimal inputs.
- **Section 3 (RSVP)**: visible only when `GroupType.EnableRSVP == true`. RSVP Reminder Communication (override; null when group type pins it), RSVP Reminder Lead Time (numeric input + "Days" suffix per design C7).
- **Section 4 Stack 1 (Overall Group Schedule)**: ScheduleType radio (None / Weekly / Custom / Named) gated on `GroupType.AllowedScheduleTypes` flags. Conditional sub-fields per type (DayOfWeek + Time for Weekly, ScheduleBuilder for Custom, SchedulePicker for Named). Inline schedule entity management on save: create / update / delete the inline `Schedule` row with `Name = string.Empty` convention; switch-to-Named or switch-to-None deletes the inline schedule when no other consumer exists (per webforms/07-locations-and-schedules.md and webforms/23-validations-and-cascades.md).
- **Section 4 Stack 3 (Member Scheduling & Check-in)**: Disable Group Member Scheduling, Hide from Schedule Toolbox, Require Member Requirements for Scheduling, Confirmation Behavior, Schedule Coordinator (PersonPicker), Coordinator Notifications (3 checkboxes Decline / Accept / Self-Schedule per Q11; empty selection = `ScheduleCoordinatorNotificationType.None`), Who Can Check-in (radio per design C8).
- **Section 8 (Chat)**: visible only when `ChatHelper.IsChatEnabled && GroupType.IsChatAllowed`. Five tri-state Yes/No/Inherit override radios per design C5 (Enable Chat, Allow Members to Leave, Public Channel, Always Show, Push Notification Mode), plus a chat-channel-avatar ImageUploader. Read-only mode when `IsSystem == true`.
- **Save block action**: persists every scalar field above. Wraps in `RockContext.WrapTransaction` per webforms/23-validations-and-cascades.md "Transactional boundary": (1) Add(group) + SaveChanges if new; (2) Authorization.AllowPerson(...) on Add when `AddAdministrateSecurityToGroupCreator`; (3) chat-avatar IsTemporary toggle; (4) photo IsTemporary toggle (mirrors chat-avatar pattern). Cache invalidations: `Authorization.Clear()` on IsSecurityRole flip; `KioskDevice.Clear()` is Phase 6 (it's tied to GroupLocation changes, not scalar saves).
- **GroupType reactive cascade**: when `currentGroupTypeId` changes mid-edit, the Vue side calls a new `GetGroupTypeOptions` block action that returns a `GroupTypeOptionsBag` with the per-GroupType options (panel visibility flags, allowed schedule types, location selection mode, peer-network defaults, etc.). Per Q2 Approach B (server round-trip per change). Phase 1's `GetGroupTypeCache(entity)` helper extends to support this lookup pattern.
- **Validation gates**: every gate from webforms/23-validations-and-cascades.md fires in order: (1) GroupType chosen, (2) self-parent check, (3) schedule iCalendar parse, (4) schedule weekly DayOfWeek chosen, (5) parent allows this group type, (6) re-check EDIT auth, (7) Page.IsValid (server-side), (8) `Group.IsValid` model-level (GroupsRequireCampus).
- **Inactive cascade to children**: when IsActive flips to false AND "Also Inactivate Child Groups" is checked, run `GetAllDescendentGroupIds(group.Id, false)` and bulk-update each active descendant's IsActive / InactiveReasonValueId / InactiveReasonNote (with "Parent Deactivated" prefix). SaveHook bulk-updates each descendant's GroupMembers (Active → Inactive) automatically.
- **Trailblazer per-field styling**: Q6 confirmed Trailblazer is a per-field visual prop (blue highlight), not a separate panel. Phase 3 wires the `trailBlazerField` prop on the General-section controls flagged in the Figma. Phase 3 spec lock identifies the exact list during user review.
- **L1 fix-during**: the WebForms `ShowGroupTypeEditDetails` duplicate-code-block at `[GroupDetail.ascx.cs:2245-2273]` is naturally avoided because the cascade is now a single block-action call, not a UI-rebinding postback chain.

## Behavior NOT delivered

- Group attribute editing (the AttributeValuesContainer in Section 5): **Phase 4**.
- Group member attribute definitions (Section 6): **Phase 4**.
- Group requirements panel + modal (Section 7): **Phase 5**.
- Group sync settings panel + modal (Section 9), including the Sync Frequency `<IntervalPicker>` restyle per Q9: **Phase 5**.
- Group member workflow triggers panel + modal (Section 10): **Phase 5**.
- Locations editing modal + inline schedule entity management on locations (Section 4 Stack 2): **Phase 6**. Phase 3 covers Section 4 Stack 1 (the inline group-level schedule) and Stack 3 (Member Scheduling); Stack 2 (the Locations grid + dialog) stays Phase 6.
- L3 (hard-coded `EntityTypeId=15` in mdGroupRequirement): **Phase 5**.
- L4 (XSS hole in `FormatTriggerType`): **Phase 5**.
- Update dependencies (5 still-WebForms outbound destinations to accept IdKey): **Phase 7**.
- Cutover: **Phase 8**.

## Deferred behaviors inherited from prior phases

Walks Phase 1 + Phase 2 coverage reports for `→ DEFERRED to Phase 3` rows.

### Inherited (covered in this phase's Implementation checklist)

| Source phase | Behavior                                                               | Coverage-report origin                     | Checklist item that handles it                                                                                  |
| ------------ | ---------------------------------------------------------------------- | ------------------------------------------ | --------------------------------------------------------------------------------------------------------------- |
| Phase 1      | `GroupTypes` / `GroupTypesExclude` filter helpers                      | webforms/01-block-configuration.md row     | Section 2 Stack 1 — checklist B1, B2                                                                            |
| Phase 1      | `LimittoSecurityRoleGroups` save-time `IsSecurityRole = true` force    | webforms/01-block-configuration.md row     | Save flow — checklist S6                                                                                        |
| Phase 1      | `AddAdministrateSecurityToGroupCreator` Add-mode cascade               | webforms/01-block-configuration.md row     | Save flow — checklist S5                                                                                        |
| Phase 1      | `PreventSelectingInactiveCampus` campus-picker behavior                | webforms/01-block-configuration.md row     | Section 2 Stack 1 — checklist B5                                                                                |
| Phase 1      | `LimitToShowInNavigationGroupTypes` filter helper                      | webforms/01-block-configuration.md row     | Section 2 Stack 1 — checklist B3                                                                                |
| Phase 1      | Page parameter `ParentGroupId` Add-path defaulting                     | webforms/01-block-configuration.md row     | Add path — checklist A1, A2                                                                                     |
| Phase 1      | Page parameter `autoEdit` handling                                     | webforms/01-block-configuration.md row     | Add path — checklist A3, A4                                                                                     |
| Phase 1      | GROUP_ADMINISTRATORS membership for IsSecurityRole checkbox visibility | webforms/01-block-configuration.md row     | Section 2 Stack 2 — checklist C5                                                                                |
| Phase 1      | GroupType-driven panel visibility (RSVP / Scheduling / Chat)           | webforms/02-block-states.md row            | Sections 3, 4, 8 — checklist D, E, G                                                                            |
| Phase 1      | GroupType-change reactive cascade                                      | webforms/02-block-states.md row            | GetGroupTypeOptions — checklist GT1-GT5                                                                         |
| Phase 1      | Edit Panel pnlEditDetails with all panel widgets                       | webforms/03-markup-structure.md row        | Section partials — checklist V1-V5                                                                              |
| Phase 1      | `btnEdit_Click` → `ShowEditDetails` (full bag)                         | webforms/04-code-behind-walkthrough.md row | Edit block action — checklist E1                                                                                |
| Phase 1      | `btnSave_Click` (768-1451)                                             | webforms/04-code-behind-walkthrough.md row | Save block action — checklist S1-S15                                                                            |
| Phase 1      | `ShowEditDetails` flow                                                 | webforms/04-code-behind-walkthrough.md row | GetEntityBagForEdit — checklist E2                                                                              |
| Phase 1      | `ShowGroupTypeEditDetails` flow                                        | webforms/04-code-behind-walkthrough.md row | GetGroupTypeOptions — checklist GT2                                                                             |
| Phase 1      | `LoadDropDowns` flow                                                   | webforms/04-code-behind-walkthrough.md row | GetEntityBagForEdit (initial dropdowns) — checklist E2; GetGroupTypeOptions (cascade dropdowns) — checklist GT2 |
| Phase 1      | Group Capacity scalar field (edit-mode)                                | webforms/16-archive-delete-copy.md row     | Section 2 Stack 2 — checklist C2                                                                                |
| Phase 1      | Signature Document Template (legacy templates filter)                  | webforms/16-archive-delete-copy.md row     | Section 2 Stack 2 — checklist C3                                                                                |
| Phase 1      | InetCalendarHelper.CreateCalendarEvent (schedule validity probe)       | webforms/27-misc-surfaces.md row           | Save flow — checklist S8                                                                                        |
| Phase 1      | Section structure (10 collapsible sections)                            | design/00-overview.md row                  | Edit panel partial — checklist V1                                                                               |
| Phase 1      | Inactive flow conditional well                                         | design/00-overview.md row                  | Section 1 — checklist A4                                                                                        |
| Phase 1      | Add-new flow header treatment                                          | design/00-overview.md row                  | Add path — checklist A5                                                                                         |
| Phase 1      | Strength labels Casual / Close / Deep (edit dropdown)                  | design/00-overview.md row                  | Section 2 Stack 3 — checklist B7                                                                                |
| Phase 1      | Schedule structure merge                                               | design/00-overview.md row                  | Section 4 Stack 1 — checklist E1                                                                                |
| Phase 1      | R4 "Capacities" label removed                                          | design/03-net-new-features.md row          | Section 2 Stack 3 — note in implementation                                                                      |
| Phase 1      | #1 Group Image uploader                                                | design/03-net-new-features.md row          | Section 1 — checklist A6                                                                                        |
| Phase 1      | #15 Renamed relationship strength labels (edit)                        | design/03-net-new-features.md row          | Section 2 Stack 3 — checklist B7                                                                                |
| Phase 1      | #18 Trailblazer Settings per-field prop                                | design/03-net-new-features.md row          | Trailblazer — checklist T1, T2                                                                                  |

### Re-deferred to a later phase

| Source phase | Behavior                                                 | Originally targeted | New target | Rationale                                                                           |
| ------------ | -------------------------------------------------------- | ------------------- | ---------- | ----------------------------------------------------------------------------------- |
| Phase 1      | #17 Group attribute categories surfaced as stack headers | Phase 3             | Phase 4    | Group attribute editing IS Phase 4; the per-category stacking is part of that work. |

### Dropped (no longer in scope)

None.

## Open questions for spec lock

Each has a default recommendation; user confirms or overrides during the spec lock pass.

### Q3.1. Edit panel partial structure

How should the 8 in-scope edit-panel sections be split across files?

**Locked (2026-05-08)**: a **single** `editPanel.partial.obs` file. All in-scope sections (1, 2 with its 3 stacks, 3, 4 stacks 1+3, 8) render inline within the orchestrating shell — no per-section partials. Sections 5 (Group Attributes), 6 (Member Attributes), 7 (Requirements), 9 (Sync), 10 (Triggers) render inline `<NotificationBox>` "Coming in Phase 4 / 5" placeholders inside the same file. Phase 4 / 5 replaces the inline placeholders with actual section markup (or extracts a partial at that point, if the file has grown unwieldy).

Trade: keeps Phase 3 focused on a single deliverable file, avoids premature decomposition. Risk: the file grows long. Mitigation: section ordering follows the canvas; each section is a self-contained `<ContentSection>` block; the file's section structure is easy to navigate.

### Q3.2. `GetGroupTypeOptions` block action — bag shape

Per Q2 Approach B, the GroupType cascade fetches per-GroupType options on each change. What fields go into `GroupTypeOptionsBag`?

**Default recommendation**: every flag, every dropdown source, every pin-by-group-type value referenced by the edit panel:

```csharp
public class GroupTypeOptionsBag
{
    // Visibility flags driving section/sub-section render
    public bool IsRsvpSectionVisible { get; set; }                    // GroupType.EnableRSVP
    public bool IsChatSectionVisible { get; set; }                    // ChatHelper.IsChatEnabled && GroupType.IsChatAllowed
    public bool IsSchedulingSectionVisible { get; set; }              // any AllowedScheduleTypes flag set
    public bool IsPeerNetworkSectionVisible { get; set; }             // GroupType.IsPeerNetworkEnabled
    public bool IsAdministratorVisible { get; set; }                  // GroupType.ShowAdministrator
    public bool IsGroupSpecificRecordSourceVisible { get; set; }      // GroupType.AllowGroupSpecificRecordSource
    public bool IsScheduleConfirmationLogicVisible { get; set; }      // GroupType.IsSchedulingEnabled
    public bool IsScheduleCoordinatorVisible { get; set; }            // GroupType.IsSchedulingEnabled
    public bool IsCoordinatorNotificationsVisible { get; set; }       // GroupType.IsSchedulingEnabled
    public bool IsCheckInRequirementsVisible { get; set; }            // GroupType.TakesAttendance
    public bool RequiresCampus { get; set; }                          // GroupType.GroupsRequireCampus

    // Allowed-flags
    public ScheduleType AllowedScheduleTypes { get; set; }            // [Flags]
    public GroupLocationPickerMode LocationSelectionMode { get; set; }
    public bool EnableLocationSchedules { get; set; }
    public bool IsSchedulingEnabled { get; set; }

    // Localization
    public string AdministratorTerm { get; set; }                     // GroupType.AdministratorTerm
    public string IconCssClass { get; set; }                          // GroupType's icon for header

    // Defaults / placeholder text for peer-network multipliers + relationship strength
    public RelationshipStrength RelationshipStrengthDefault { get; set; }
    public bool RelationshipGrowthEnabledDefault { get; set; }
    public decimal LeaderToLeaderMultiplierDefault { get; set; }
    public decimal LeaderToNonLeaderMultiplierDefault { get; set; }
    public decimal NonLeaderToLeaderMultiplierDefault { get; set; }
    public decimal NonLeaderToNonLeaderMultiplierDefault { get; set; }

    // Pinned values that null out per-group overrides
    public int? RsvpReminderOffsetDays { get; set; }                  // null = group can override
    public Guid? RsvpReminderSystemCommunicationGuid { get; set; }    // null = group can override

    // Allowed group-status defined values (Status dropdown)
    public List<ListItemBag> StatusValues { get; set; }

    // Inherited member-attribute definitions (read-only display in Section 6 in Phase 4; surfacing here so Phase 4 doesn't re-fetch)
    public List<PublicAttributeBag> InheritedMemberAttributes { get; set; }
}
```

This is a one-call payload for everything the edit panel needs to know about the new GroupType. Phase 4-5 may extend with attribute-related fields when those sections light up.

**Locked (2026-05-08)**: take the default shape as-is.

### Q3.3. Image uploader binary-file-type Guid

What `BinaryFileTypeGuid` does the photo uploader use?

**Locked (2026-05-08)**: `Rock.SystemGuid.BinaryFiletype.DEFAULT`. Matches the chat-channel-avatar pattern at [research/webforms/14-chat.md "Open questions"](../webforms/14-chat.md). A dedicated `BinaryFiletype.GROUP_PHOTO` constant was considered but not warranted (no requirement on the table for distinct retention / cleanup rules).

### Q3.4. Add-path mode discrimination

How does the Vue side know it's in Add mode (no entity yet) vs Edit mode (existing entity)?

**Locked (2026-05-08)**: drop `bag.IsAddMode` from the bag. Use the canonical Rock detail-block convention sourced from sibling blocks ([connectionTypeDetail.obs:300-302](../../Rock.JavaScript.Obsidian.Blocks/src/Engagement/connectionTypeDetail.obs:300), [groupTypeDetail.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/groupTypeDetail.obs), [contentChannelDetail.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Cms/contentChannelDetail.obs), [campusDetail.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Core/campusDetail.obs)):

- C# returns the bag with `IdKey = ""` when `entity.Id == 0` (Phase 1 already does this via `GetInitialEntity`).
- At the bottom of `groupDetail.obs`'s `<script setup>`, after error-handling guards, add an `else if (!config.entity.idKey) { panelMode.value = DetailPanelMode.Add; ... }` block — same shape as `connectionTypeDetail.obs`.
- Thereafter `panelMode.value === DetailPanelMode.Add` is the in-template check. No bag field needed.

Phase 3's Add-mode chrome (Add Group title, hidden header / subheader labels, hidden title icon, hidden audit kebab, hidden follow star) all derive from `panelMode.value === DetailPanelMode.Add` rather than `bag.IsAddMode`. The Bag fields contributed table below also drops `isAddMode` accordingly.

### Q3.5. Save flow transactional ordering

The WebForms `btnSave_Click` wraps in `WrapTransaction` with this ordering: Add(group) → SaveChanges → AllowPerson(if Add+AdminToCreator) → SaveAttributeValues → group-member-attribute defs → inactive-children cascade → chat-avatar IsTemporary. Per webforms/23-validations-and-cascades.md "Transactional boundary".

Phase 3 doesn't ship attribute editing or member-attribute defs, so its save body is shorter:

1. Add(group) if new
2. UpdateEntityFromBox (scalar field assignments)
3. SaveChanges (assigns group.Id on Add)
4. If Add && `AddAdministrateSecurityToGroupCreator`: `Authorization.AllowPerson(...)` then SaveChanges
5. Inactive cascade if `cbInactivateChildGroups.Checked && !group.IsActive`
6. Chat-avatar IsTemporary toggle (existing pattern from research/webforms/14-chat.md)
7. **NEW**: Photo IsTemporary toggle (mirroring chat-avatar)
8. SaveChanges

**Locked (2026-05-08)**: implement the 8-step ordering above inside `RockContext.WrapTransaction(...)`. Phase 4 extends with attribute-value save (between AllowPerson and Inactive cascade). Phase 5 extends with sub-feature save bodies (requirements, sync, triggers).

### Q3.6. Trailblazer per-field flag list

Q6 in 00-architecture.md confirmed Trailblazer is a `trailblazerField` per-field prop. Which exact controls in the General section (Section 2) get the prop?

**Locked (2026-05-08)**: the controls highlighted in blue on Figma frame `4670-25350` ("Add/Edit Mode") are the Trailblazer-gated controls:

1. **Group Administrator** (`<PersonPicker>`) — Section 2 Stack 2.
2. **Required Signature Document** (`<DropDownList>`) — Section 2 Stack 2.
3. **Member Record Source** (`<DefinedValuePicker>`) — Section 2 Stack 2.
4. **Show Advanced Relationship Settings** (`<InlineSwitch>` — *not* a checkbox) — Section 2 Stack 3.
5. **Four relationship multipliers** (`<NumberBox>` × 4: LeaderToLeader, LeaderToNonLeader, NonLeaderToLeader, NonLeaderToNonLeader) — transitively gated. The conditional well that contains them is revealed by control #4 above; when Trailblazer is off, control #4 is hidden and the well closes itself.

**Framework audit (2026-05-08)**: the prop already exists at the base form-field level — [rockFormField.obs:101](../../Rock.JavaScript.Obsidian/Framework/Controls/rockFormField.obs:101) computes `isVisible = !props.trailblazerField || store.state.trailblazerMode`. All controls that wrap `RockFormField` (`<RockTextBox>`, `<RockDropDownList>`, `<NumberBox>`, `<DefinedValuePicker>`, `<PersonPicker>`, etc.) inherit it. `<InlineCheckBox>` and `<InlineSwitch>` have their own `trailblazerField` prop with the same semantics ([inlineCheckBox.obs:40](../../Rock.JavaScript.Obsidian/Framework/Controls/inlineCheckBox.obs:40)). No Framework change needed.

Each gated control gets `trailblazerField={true}` on the control. When `store.state.trailblazerMode === false`, the control hides automatically; the conditional well wrapping the multipliers closes naturally because the toggle that reveals it is hidden.

### Q3.7. View-mode notification surface persistence in edit mode

Phase 2 added two `<NotificationBox>` instances at the top of `viewPanel.partial.obs` (system-group banner + role-limit warning). The WebForms block declares the equivalent controls inside `pnlDetails` outside the mode-specific panels, so they render in **both** view and edit modes.

**Locked (2026-05-08)**: Option B — view-mode only. The notifications stay in `viewPanel.partial.obs` and do not render during edit mode. This is a deliberate departure from WebForms parity:

- The role-limit warning is computed from current member counts; it becomes stale immediately as the user starts editing roles or membership.
- The system-group banner repeats info the user already learned at view time.
- The edit panel's own validation gates surface relevant errors (read-only-system disables fields, IsValid checks fire on save).

If a future need surfaces (e.g., the role-limit warning is needed during the edit session), the surface can be hoisted up to `groupDetail.obs` so it renders in both modes. Tracked but not Phase 3 work.

## Research coverage

Every implementation session reads these files in full at session start (per SESSION-PROTOCOL.md Section A6) and audits implementation against them at session close (Section C2).

- [research/specs/00-architecture.md](00-architecture.md): always relevant; Q2 (cascade Approach B), Q6 (Trailblazer), Q8 (photo column / uploader pattern), Q10 (Sections & Stacks core components), Q11 (Coordinator Notifications), Q12 (L1 fix-during).
- [research/specs/01-phase-1-shell-and-view.md](01-phase-1-shell-and-view.md): the Phase 1 spec, especially the Self-review coverage report (the source of inherited-deferred rows above) and the Mid-phase decisions log.
- [research/specs/02-phase-2-complete-view-panel.md](02-phase-2-complete-view-panel.md): Phase 2's Self-review coverage report and Mid-phase decisions (e.g., Group.PhotoUrl shape, NO ACTION cascade decision).
- [research/webforms/01-block-configuration.md](../webforms/01-block-configuration.md): all 21 block attributes (the ones Phase 1 deferred land here), page parameters (`ParentGroupId`, `autoEdit`).
- [research/webforms/02-block-states.md](../webforms/02-block-states.md): edit-mode states, GroupType-conditional visibility matrix, mode visibility table.
- [research/webforms/03-markup-structure.md](../webforms/03-markup-structure.md): pnlEditDetails layout, all panel widgets in scope (wpGeneral, wpRsvp, wpScheduling, wpChat).
- [research/webforms/04-code-behind-walkthrough.md](../webforms/04-code-behind-walkthrough.md): `ShowEditDetails`, `ShowGroupTypeEditDetails`, `btnSave_Click`, `LoadDropDowns`, `ddlGroupType_SelectedIndexChanged`, `ddlParentGroup_SelectedIndexChanged`, `cbIsSecurityRole_CheckedChanged`.
- [research/webforms/06-peer-network.md](../webforms/06-peer-network.md): peer-network override panel, four multipliers, relationship-strength override.
- [research/webforms/07-locations-and-schedules.md](../webforms/07-locations-and-schedules.md): inline (group's primary) Schedule lifecycle ONLY (Section 4 Stack 1). Locations grid + dialog (Section 4 Stack 2) is Phase 6 and explicitly out of scope here.
- [research/webforms/08-scheduling.md](../webforms/08-scheduling.md): Section 4 Stack 3 (Member Scheduling & Check-in).
- [research/webforms/14-chat.md](../webforms/14-chat.md): Section 8. Five tri-state overrides + chat-channel-avatar `IsTemporary` toggle pattern.
- [research/webforms/15-rsvp.md](../webforms/15-rsvp.md): Section 3.
- [research/webforms/22-grouptype-cascade.md](../webforms/22-grouptype-cascade.md): the cascade map driving `GetGroupTypeOptions`.
- [research/webforms/23-validations-and-cascades.md](../webforms/23-validations-and-cascades.md): every save-flow cascade, every cache invalidation, every binary-file IsTemporary toggle, every validation gate. The most load-bearing research file for Phase 3.
- [research/design/00-overview.md](../design/00-overview.md): edit-panel layout shape, designer-stated changes for edit mode.
- [research/design/02-edit-panel.md](../design/02-edit-panel.md): full edit panel walkthrough, every section, every stack, every conditional well.
- [research/design/03-net-new-features.md](../design/03-net-new-features.md): items #1, #10, #11, #12, #13, #15, #18 (Trailblazer per-field prop), and behavior changes C2-C8.
- [research/design/04-component-inventory.md](../design/04-component-inventory.md): which Obsidian core controls map to which fields.
- [research/design/05-design-system-deltas.md](../design/05-design-system-deltas.md): patterns the design uses that may not have a 1:1 Obsidian equivalent.
- [research/design/06-mapping-to-webforms.md](../design/06-mapping-to-webforms.md): edit-panel mapping table.
- Reference: [Rock.Blocks/Group/GroupTypeDetail.cs](../../Rock.Blocks/Group/GroupTypeDetail.cs) — closest sibling block; shows the pattern for `UpdateEntityFromBox`, `IfValidProperty`, the cache-invalidation flow, and edit-mode field plumbing.

## Implementation checklist

### A. Add path

A1. Phase 1 declared `PageParameterKey.ParentGroupId`. In `Edit` block action's Add branch (`entity.Id == 0`), pre-populate `bag.ParentGroupId` from the page parameter so the Vue ParentGroup picker defaults correctly.
A2. The Add path's `entity` instance is created via `entityService.Add( entity )` in `TryGetEntityForEditAction` (already in Phase 1). On Save, the `UpdateEntityFromBox` body assigns `entity.ParentGroupId = bag.ParentGroupId` (with self-parent validation gate).
A3. Re-declare `PageParameterKey.AutoEdit` (dropped in Phase 1 per the mid-phase decision); read it in `GetObsidianBlockInitialization` and surface as `box.PanelMode = Edit` when `bag.IdKey != "" && page.Param("autoEdit").AsBoolean()`.
A4. Section 1's Inactive flow conditional well (`<ConditionalWell>`) reveals when Active is unchecked. Contains: Inactive Reason (`<DefinedValuePicker>` filtered to `GroupType.GetInactiveReasonsForGroupType(groupTypeId)` — see webforms/01-block-configuration.md "InactiveReason" lookup), Inactive Note (`<TextBox>` multi-line), "Also Inactivate Child Groups" (`<CheckBox>` per design rename C2). Default Inactive Note: empty.
A5. Add-mode header chrome (per Q3.4 lock). At the bottom of `groupDetail.obs`'s `<script setup>`, after the existing error guards, add `else if (!config.entity.idKey) { panelMode.value = DetailPanelMode.Add; }`. Then derive header chrome from `panelMode.value === DetailPanelMode.Add`:

- **Title:** "Add Group" instead of `bag.name` (computed via the existing `panelName` computed).
- **Title icon (`titleIconCssClass`):** empty string (no GroupType chosen yet, so no icon).
- **Header labels (`headerLabels`):** empty array (no GroupType / Campus chip).
- **Subheader labels (`labels` aka `blockLabels`):** empty array (no RelationshipStrength / Public / Inactive labels).
- **`isAuditHidden`:** true (no entity yet).
- **`isFollowVisible`:** false (no entity to follow).
- **Footer secondary actions (Copy):** hidden via the existing `panelMode.value === DetailPanelMode.View` gate that already exists.

### B. Section 2 Stack 1 (Overview)

B1. Group Type: read-only label on existing groups (`bag.IsAddMode == false`). Required `<DropDownList>` on Add. Source: `GetAllowedGroupTypes(parentGroupType, RockContext)` filtered by `LimittoSecurityRoleGroups`, `GroupTypes`, `GroupTypesExclude`, `LimitToShowInNavigationGroupTypes` block attributes.
B2. Build a `GetAllowedGroupTypes(GroupTypeCache parentGroupType, RockContext rockContext)` private helper in `#region Helper Methods` mirroring the WebForms behavior at [GroupDetail.ascx.cs:2940-2982](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2940). Filters: (1) parent's `ChildGroupTypes`, (2) attribute-included `GroupTypes`, (3) attribute-excluded `GroupTypesExclude`, (4) `LimittoSecurityRoleGroups`, (5) `LimitToShowInNavigationGroupTypes`.
B3. Parent Group: `<GroupPicker>` core control. Excludes the current group (self-parent guard pre-validation gate).
B4. Campus: `<CampusPicker>` core control. Honors `PreventSelectingInactiveCampus` block attribute by passing `IncludeInactive: !attribute.Value`.
B5. Status: `<DefinedValuePicker>` filtered to `groupType.GroupStatusDefinedType`.

### C. Section 2 Stack 2 (Administration & Security)

C1. Group Administrator: `<PersonPicker>`. Hidden when `groupType.ShowAdministrator == false`. Label uses `groupType.AdministratorTerm` (default "Administrator") via the `GroupTypeOptionsBag.AdministratorTerm` channel.
C2. Group Capacity: `<NumberBox>`. Suffix "Members" per design rename map.
C3. Required Signature Document: `<DropDownList>` populated via `SignatureDocumentTemplateService.GetLegacyTemplates()` per webforms/05-entity-and-services.md.
C4. Member Record Source: `<DefinedValuePicker>`. Visible only when `groupType.AllowGroupSpecificRecordSource == true`.
C5. Enable as Security Role: `<CheckBox>`. Visible only when current user is in GROUP_ADMINISTRATORS (`groupService.GroupHasMember(GROUP_ADMINISTRATORS, currentPersonId)` per Phase 1's existing `GroupService` reference). On `LimittoSecurityRoleGroups == true`, force-checked + disabled.
C6. Security Level: `<RadioButtonList>` (per design C6). Visible only when IsSecurityRole is checked. Inside a `<ConditionalWell>`. Source: `ElevatedSecurityLevel` enum.

### D. Section 2 Stack 3 (Relationships)

D1. Override Relationship Strength: `<CheckBox>` ("Override Relationship Strength"). Reveals the inner `<ConditionalWell>` containing the override controls.
D2. Relationship Strength: `<RadioButtonList>` (or `<DropDownList>`; design TBD) with renamed Casual / Close / Deep options. Underlying enum integers unchanged. Bag emits `RelationshipStrengthOverride: RelationshipStrength?`.
D3. Enable Relationship Growth Over Time: `<CheckBox>`. Bag emits `RelationshipGrowthEnabledOverride: bool?`.
D4. Show Advanced Relationship Settings: `<CheckBox>` (rename from "Show Advanced Settings" per design). Reveals the four-multiplier matrix.
D5. Multipliers: four `<NumberBox>`(decimal) controls for `LeaderToLeader`, `LeaderToNonLeader`, `NonLeaderToLeader`, `NonLeaderToNonLeader`. Placeholder text reads the GroupType default per `GroupTypeOptionsBag.*MultiplierDefault`. Decimal range 0-100% per the WebForms `AsDecimalPercentageOrNull(0, 100)` parser.

### E. Section 4 Stack 1 (Overall Group Schedule)

E1. ScheduleType: `<RadioButtonList>` with options driven by `groupType.AllowedScheduleTypes` flags.
E2. Weekly: `<DayOfWeekPicker>` + `<TimePicker>`. Visible only when ScheduleType == Weekly.
E3. Custom: `<ScheduleBuilder>` (iCalendar editor). Visible only when ScheduleType == Custom.
E4. Named: `<SchedulePicker>` (single-select). Visible only when ScheduleType == Named.
E5. Save logic per webforms/07-locations-and-schedules.md "Inline schedule entity management": create / reuse / delete the inline Schedule with `Name = string.Empty` convention. Switch-to-Custom-or-Weekly: create new Schedule if none exists, else mutate. Switch-to-None-or-Named: delete the inline Schedule via `scheduleService.CanDelete` guard.

### F. Section 4 Stack 3 (Member Scheduling & Check-in)

F1. Disable Group Member Scheduling, Hide from Schedule Toolbox, Require Member Requirements for Scheduling: `<CheckBox>` controls (renamed labels per design).
F2. Confirmation Behavior: `<DropDownList>` (renamed from "Schedule Confirmation Logic"). Source: `ScheduleConfirmationLogic` enum.
F3. Schedule Coordinator: `<PersonPicker>`. Visible only when `groupType.IsSchedulingEnabled`.
F4. Coordinator Notifications: 3 `<CheckBox>` controls (Decline / Accept / Self-Schedule). Per Q11, empty selection → `ScheduleCoordinatorNotificationType.None`. No "None" checkbox per design.
F5. Who Can Check-in: `<RadioButtonList>` (per design C8). Visible only when `groupType.TakesAttendance`. Source: `AttendanceRecordRequiredForCheckIn` enum.

### G. Section 8 (Chat)

G1. Five tri-state Yes/No/Inherit override radios (per design C5): Enable Chat, Allow Members to Leave Channel, Public Channel, Always Show, Push Notification Mode. Bag emits `IsChatEnabledOverride: bool?` (etc.). Render as `<RadioButtonList>` with three options ("Inherit from Group Type", "Yes", "No") — empty selection maps to null.
G2. Chat-channel-avatar uploader: `<ImageUploader>` with `BinaryFileTypeGuid = Rock.SystemGuid.BinaryFiletype.DEFAULT`. Read-only (disabled) when `IsSystem == true`. Save logic per webforms/14-chat.md: capture `orphanedChatChannelAvatarId` if changing, then post-save toggle IsTemporary.

### H. Photo uploader (Section 1)

H1. `<ImageUploader>` for Group Photo. `BinaryFileTypeGuid = Rock.SystemGuid.BinaryFiletype.DEFAULT` (per Q3.3 default). Bag emits `PhotoBinaryFileGuid: Guid?`.
H2. Save logic: in `UpdateEntityFromBox`, capture `orphanedPhotoId` if `bag.PhotoBinaryFileGuid` resolves to a different `BinaryFile.Id` than `entity.PhotoId`. Post-save (inside the same `WrapTransaction`), toggle `BinaryFile.IsTemporary` (orphan → true, current → false). Mirrors the chat-avatar pattern at webforms/14-chat.md.

### I. Save block action

S1. Implement `Save(ValidPropertiesBox<GroupBag> box)` returning `ValidPropertiesBox<GroupBag>` (200) for Update or `string` redirect URL (201) for Create. Mirrors the standard `RockEntityDetailBlockType<>.Save` shape.
S2. Resolve / construct the entity via `TryGetEntityForEditAction(box.Bag.IdKey, ...)`.
S3. Call `UpdateEntityFromBox(entity, box)`. Body uses `IfValidProperty(nameof(box.Bag.X), () => entity.X = box.Bag.X)` for every scalar field to support partial-update saves.
S4. Validation gates 1-8 from webforms/23-validations-and-cascades.md. Each returns an actionable error message in the `ValidPropertiesBox`'s response.
S5. On Add + `AddAdministrateSecurityToGroupCreator == true`: call `Authorization.AllowPerson(group, ADMINISTRATE, currentPerson, RockContext)`.
S6. On `LimittoSecurityRoleGroups == true`: force `entity.IsSecurityRole = true` regardless of UI value.
S7. Inactive cascade: if `box.Bag.InactivateChildGroups && !entity.IsActive`, run `GetAllDescendentGroupIds(entity.Id, includeInactiveChildGroups: false)` and bulk-update active descendants per webforms/23-validations-and-cascades.md.
S8. Schedule iCalendar parse / weekly DayOfWeek validation: if user picks Custom but the iCal is invalid, silently downgrade to None per webforms/23-validations-and-cascades.md gate 3-4.
S9. `Group.IsValid` model validation: gate 8.
S10. Wrap (1) Add+SaveChanges, (2) AllowPerson if applicable, (3) Inactive cascade, (4) chat-avatar IsTemporary, (5) photo IsTemporary in `RockContext.WrapTransaction`.
S11. Cache invalidations: `Authorization.Clear()` on IsSecurityRole flip (was/now check). `KioskDevice.Clear()` is Phase 6 (location-driven).
S12. Save-flow `WasSecurityRole` snapshot: capture before any mutation per webforms/23-validations-and-cascades.md.
S13. Peer network override save: per webforms/23-validations-and-cascades.md "Peer Network overrides reset cascade", only assign overrides when `cbOverrideRelationshipStrength == true` AND `groupType.IsPeerNetworkEnabled == true`. Otherwise nullify.
S14. RSVP override save: per webforms/23-validations-and-cascades.md "RSVP overrides precedence cascade", null out overrides when groupType has its own values; null out everything when `groupType.EnableRSVP == false`.
S15. Record source override save: per webforms/23-validations-and-cascades.md "RecordSource precedence cascade", save only when `groupType.AllowGroupSpecificRecordSource == true`.

### GT. GetGroupTypeOptions block action (Q2 cascade)

GT1. Implement `GetGroupTypeOptions(int groupTypeId)` returning `GroupTypeOptionsBag`.
GT2. Body resolves `GroupTypeCache.Get(groupTypeId)` then maps to the bag fields per Q3.2 default. Mirrors the WebForms `ShowGroupTypeEditDetails` rebinding logic at [GroupDetail.ascx.cs:2173-2295](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2173).
GT3. Auth: re-check EDIT on the entity (read from page parameter `GroupId` per Phase 1's existing `GetInitialEntity`).
GT4. The Vue side calls this on `currentGroupTypeId` change. The bag becomes the canonical source for all section-visibility flags during the edit session; sections reactively re-render.
GT5. Edit-side dropdown sources that depend on group type (Status defined values, allowed schedule types, etc.) refresh on each cascade.

### V. Vue file structure

V1. Replace the Phase 1 placeholder `editPanel.partial.obs` with a single orchestrating file containing all in-scope sections inline (per Q3.1 lock). Sections 1, 2 (3 stacks), 3, 4 stacks 1+3, and 8 each render as a top-level `<ContentSection>` block in the order specified by the figma. Sections 5, 6, 7, 9, 10 render inline `<NotificationBox alertType="info">` "Coming in Phase 4 / 5" placeholders within the same file (no separate placeholder partials).
V2. The file accepts `modelValue: GroupBag` and `groupTypeOptions: GroupTypeOptionsBag` props. Emits `update:modelValue` on each field change. Reads section visibility from `groupTypeOptions.is*Visible` flags. Reads localization (`AdministratorTerm`, etc.) from `groupTypeOptions`.
V3. Each section uses `<ContentSection>` (per Q10 and architecture Q10).
V4. Conditional Wells: use `<ConditionalWell>` core control for Inactive flow, Security Level (under IsSecurityRole), Override Relationship Strength, Override Coordinator (if shown), and any other left-bordered subdued region per design.
V5. Trailblazer styling: per Q3.6 / Q6, set `trailblazerField={true}` on the controls listed in Q3.6's lock. The base `<RockFormField>` and `<InlineCheckBox>` / `<InlineSwitch>` controls already wire through to `store.state.trailblazerMode`; no Framework changes required.

### T. Trailblazer per-field flag wiring

T1. The `trailblazerField` prop already exists at the base form-field level ([rockFormField.obs:101](../../Rock.JavaScript.Obsidian/Framework/Controls/rockFormField.obs:101)) and on inline controls ([inlineCheckBox.obs:40](../../Rock.JavaScript.Obsidian/Framework/Controls/inlineCheckBox.obs:40)). No Framework change required.
T2. Set `trailblazerField={true}` on the five controls Q3.6 enumerated:
   - Group Administrator (`<PersonPicker>`)
   - Required Signature Document (`<DropDownList>`)
   - Member Record Source (`<DefinedValuePicker>`)
   - Show Advanced Relationship Settings (`<InlineSwitch>`)
   - Four relationship multipliers — transitively gated; `trailblazerField={true}` is set on each `<NumberBox>` for clarity, even though the wrapping conditional well closes itself when the toggle is hidden.

### L. Latent bug L1 fix-during

L1. The WebForms `ShowGroupTypeEditDetails` body at [GroupDetail.ascx.cs:2245-2273](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2245) contains a duplicate code block. Phase 3's `GetGroupTypeOptions` rewrites this as a single bag-construction call; the duplicate is naturally avoided. Confirm during implementation that no equivalent duplication is introduced server-side.

## Out-of-scope items

- Group attribute editing (Section 5 — `AttributeValuesContainer`): **Phase 4**.
- Member attribute definitions (Section 6 — inherited + custom + modal): **Phase 4**.
- Group Requirements panel + modal (Section 7): **Phase 5**.
- Group Sync panel + modal (Section 9): **Phase 5**, including the Sync Frequency `<IntervalPicker>` restyle per Q9.
- Group Member Workflow Triggers panel + modal (Section 10): **Phase 5**.
- Locations editing modal + Section 4 Stack 2 (the Locations grid): **Phase 6**. Phase 3 handles only Stack 1 (inline schedule) and Stack 3 (Member Scheduling). Stack 2 renders a "Locations editing coming in Phase 6" placeholder.
- L3 (hard-coded `EntityTypeId=15`): **Phase 5** (lives in the Requirements modal markup).
- L4 (XSS hole in `FormatTriggerType`): **Phase 5** (lives in the Triggers list).
- Updating the 5 still-WebForms outbound destinations: **Phase 7**.
- Cutover and WebForms file deletion: **Phase 8**.

## Files to create / modify

### Rock.Blocks/Group/

- `GroupDetail.cs` (MODIFY) — add `Save` and `GetGroupTypeOptions` block actions; expand `GetEntityBagForEdit`; add `UpdateEntityFromBox` body; add `GetAllowedGroupTypes` helper; declare `PageParameterKey.AutoEdit` constant.

### Rock.ViewModels/Blocks/Group/GroupDetail/

- `GroupBag.cs` (MODIFY) — add edit-mode scalar fields, peer-network override fields, RSVP override fields, scheduling fields, chat tri-state override fields, ScheduleType + sub-mode fields, photo uploader field.
- `GroupTypeOptionsBag.cs` (NEW) — see Q3.2 default shape.

### Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/

- `groupBag.d.ts` (REGEN via Rock.CodeGeneration) — picks up the new fields.
- `groupTypeOptionsBag.d.ts` (NEW; placeholder until regen).

### Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/

- `editPanel.partial.obs` (REPLACE Phase 1 placeholder) — single file containing the orchestrating shell + all in-scope sections (1, 2 with its 3 stacks, 3, 4 stacks 1+3, 8). Sections 5/6/7/9/10 render inline `<NotificationBox>` "Coming in Phase 4 / 5" placeholders within the same file. Per Q3.1 lock.
- `types.partial.ts` (MODIFY) — add cascade-related types if needed (e.g., a `SectionVisibilityKey` enum).

### Rock.Migrations/Migrations/

- No new migrations.

## Bag fields contributed

```typescript
interface GroupBag {
    // (existing Phase 1 + Phase 2 fields unchanged)

    // Section 1 (Top fields)
    inactiveReasonValueId: number | null;
    inactiveReasonNote: string | null;
    inactivateChildGroups: boolean;
    photoBinaryFileGuid: Guid | null;       // for the uploader; null = no photo

    // Section 2 Stack 1 (Overview)
    parentGroupId: number | null;
    parentGroupName: string | null;          // for picker pre-population
    campusId: number | null;
    statusValueId: number | null;

    // Section 2 Stack 2 (Admin & Security)
    groupAdministratorPersonAliasGuid: Guid | null;
    requiredSignatureDocumentTemplateId: number | null;
    groupMemberRecordSourceValueId: number | null;
    isSecurityRole: boolean;
    elevatedSecurityLevel: ElevatedSecurityLevel;

    // Section 2 Stack 3 (Relationships)
    overrideRelationshipStrength: boolean;
    relationshipStrengthOverride: RelationshipStrength | null;
    relationshipGrowthEnabledOverride: boolean | null;
    leaderToLeaderRelationshipMultiplierOverride: number | null;
    leaderToNonLeaderRelationshipMultiplierOverride: number | null;
    nonLeaderToLeaderRelationshipMultiplierOverride: number | null;
    nonLeaderToNonLeaderRelationshipMultiplierOverride: number | null;

    // Section 3 (RSVP)
    rsvpReminderOffsetDays: number | null;
    rsvpReminderSystemCommunicationGuid: Guid | null;

    // Section 4 Stack 1 (Inline Schedule)
    scheduleType: ScheduleType;
    weeklyDayOfWeek: DayOfWeek | null;
    weeklyTimeOfDay: string | null;          // ISO time
    iCalendarContent: string | null;
    namedScheduleId: number | null;

    // Section 4 Stack 3 (Member Scheduling & Check-in)
    schedulingMustMeetRequirements: boolean;
    disableScheduling: boolean;
    disableScheduleToolboxAccess: boolean;
    scheduleConfirmationLogic: ScheduleConfirmationLogic | null;
    scheduleCoordinatorPersonAliasGuid: Guid | null;
    scheduleCoordinatorNotificationTypes: ScheduleCoordinatorNotificationType;
    attendanceRecordRequiredForCheckIn: AttendanceRecordRequiredForCheckIn;

    // Section 8 (Chat)
    isChatEnabledOverride: boolean | null;
    isLeavingChatChannelAllowedOverride: boolean | null;
    isChatChannelPublicOverride: boolean | null;
    isChatChannelAlwaysShownOverride: boolean | null;
    chatPushNotificationModeOverride: ChatNotificationMode | null;
    chatChannelAvatarBinaryFileGuid: Guid | null;
}

interface GroupTypeOptionsBag {
    // See Q3.2 for the full default shape.
}
```

## Block actions

| Action                | Request                        | Returns                                                                           | Notes                                                    |
| --------------------- | ------------------------------ | --------------------------------------------------------------------------------- | -------------------------------------------------------- |
| `Save`                | `ValidPropertiesBox<GroupBag>` | `ValidPropertiesBox<GroupBag>` (200 update) OR `string` redirect URL (201 create) | All scalar persistence + cascades + cache invalidations. |
| `GetGroupTypeOptions` | `int groupTypeId`              | `GroupTypeOptionsBag`                                                             | Q2 Approach B server round-trip.                         |

`Edit`, `Delete`, `Archive`, `ArchiveWithChildren`, `Copy` are unchanged from Phase 1 / 2.

## Save action contributions

See checklist S1-S15 for the full save flow. Cascades and cache invalidations:

- `IsActive` flip — handled by SaveHook (bulk-update GroupMembers).
- `IsActive == false && InactivateChildGroups` — block-level cascade through descendants.
- `IsSecurityRole` flip — `Authorization.Clear()`.
- Inline schedule create / reuse / delete — per webforms/07-locations-and-schedules.md inline schedule lifecycle.
- Chat-avatar IsTemporary — pre-existing pattern from webforms/14-chat.md.
- Photo IsTemporary — new, mirroring chat-avatar.

## Code patterns to follow

- `RockEntityDetailBlockType<TEntity, TBag>` overrides: full bag in `GetEntityBagForEdit`. `UpdateEntityFromBox` body mirrors `IfValidProperty(nameof(box.Bag.X), () => entity.X = box.Bag.X)` per the standard pattern.
- Use `RockContext.WrapTransaction(...)` for the save flow per webforms/23-validations-and-cascades.md.
- Use `<ContentSection>`, `<ConditionalWell>` core components per Q10.
- Use `RockDateTime` (not `DateTime`) for timestamp captures.
- Reference: [Rock.Blocks/Group/GroupTypeDetail.cs](../../Rock.Blocks/Group/GroupTypeDetail.cs) for the closest sibling save flow (mirrors the WrapTransaction pattern, cache invalidation flow, and edit-mode plumbing).
- The cascade pattern: emit a typed `GroupTypeOptionsBag` from a server action; the Vue side stores it in a ref and section partials read from it. Avoid prop-drilling individual flags.

## Design references

- Edit panel canvas: `4670-25350` ("Add/Edit Mode") in `N60VRdhtRtjO9EA9nba9fB` Figma.
- Edit Section 1 (Top fields + Image): [research/design/screenshots/edit-section-01.png](../design/screenshots/edit-section-01.png).
- Edit Section 2 (General — 3 stacks): [research/design/screenshots/edit-section-02.png](../design/screenshots/edit-section-02.png).
- Edit Section 3 (RSVP): [research/design/screenshots/edit-section-03.png](../design/screenshots/edit-section-03.png).
- Edit Section 4 (Scheduling): [research/design/screenshots/edit-section-04.png](../design/screenshots/edit-section-04.png).
- Edit Section 8 (Chat): [research/design/screenshots/edit-section-08.png](../design/screenshots/edit-section-08.png).
- Add-mode header treatment: per design 03-net-new-features.md item #13.

## Mid-phase decisions log

(initially empty; populate during implementation per SESSION-PROTOCOL.md Section B4)

## Verification plan

Manual test scenarios for the user's review playbook (per SESSION-PROTOCOL.md Section D7):

1. Edit existing group: navigate to `/Group/{N}`, click Edit. Confirm panel transitions to edit mode with all 5 sections (1, 2, 3, 4, 8) populated from the existing values. Sections 5, 6, 7, 9, 10 render placeholders.
2. Cancel from Edit: click Cancel. Confirm view-mode panel returns with original values intact (no save).
3. Save scalar changes: edit Name, Description, GroupCapacity. Save. Confirm View panel renders the updated values and the bag round-trips correctly.
4. Add path: navigate to `/Group/0`. Confirm "Add Group" header (no labels, no follow star, no audit kebab). Group Type is required. Confirm Save creates the group and redirects to its detail URL.
5. Add path with `?ParentGroupId=N`: navigate to `/Group/0?ParentGroupId=42`. Confirm Parent Group picker pre-populates with group 42.
6. `?autoEdit=true`: navigate to `/Group/{N}?autoEdit=true`. Confirm Edit panel opens directly without clicking Edit.
7. IsActive cascade: edit a group with child groups, uncheck Active, fill Inactive Reason, check "Also Inactivate Child Groups", Save. Confirm parent + all active descendants are now Inactive.
8. IsSecurityRole flip: as a GROUP_ADMINISTRATORS member, toggle "Enable as Security Role" and Save. Confirm `Authorization.Clear()` fires (test by re-entering the page and confirming the Security button now shows for the new state).
9. GroupType cascade: in Add mode, change Group Type. Confirm sections re-render: Sections that depend on the new GroupType's flags become visible/hidden. Schedule types radio updates. Peer-network defaults update.
10. Inline schedule create: pick Weekly schedule with a DayOfWeek. Save. Confirm `Group.ScheduleId` resolves to a new Schedule row with `Name = ""`. View panel renders the friendly schedule text.
11. Inline schedule switch to Named: edit, switch to Named, pick a named schedule, Save. Confirm the previous inline schedule is deleted (assuming no other consumer). New `Group.ScheduleId` points to the named one.
12. Inline schedule switch to None: edit, switch to None, Save. Confirm `Group.ScheduleId == null` and the inline schedule is deleted.
13. Photo upload: upload a photo, Save. Confirm the photo renders in the View panel hero. Refresh the BinaryFile cleanup job and confirm the new photo's `IsTemporary` is false.
14. Photo replacement: upload a different photo, Save. Confirm the OLD photo's `IsTemporary` flips to true and the NEW photo's `IsTemporary` is false.
15. Chat avatar (when chat is enabled): upload a chat avatar, Save. Same IsTemporary semantics. Replacement and removal scenarios mirror photo.
16. Coordinator Notifications empty selection: deselect all 3 boxes. Save. Confirm `entity.ScheduleCoordinatorNotificationTypes == ScheduleCoordinatorNotificationType.None`.
17. Trailblazer styling: confirm the fields the user identified in Q3.6 render with the blue-highlight class. (Visual check.)
18. Save validation gate: try to save with no Group Type set (Add mode) — confirm error message. Try to save with self-parent — confirm error message. Try to save without required Campus when GroupType.GroupsRequireCampus — confirm error message.
19. RSVP override: with a GroupType that pins RSVPReminderOffsetDays, confirm the RSVP control is read-only / disabled. With a GroupType that doesn't pin, confirm the override saves to `entity.RSVPReminderOffsetDays`.
20. Confirm Phase 1 + Phase 2 surfaces are unchanged: View panel still works, Delete / Archive / Copy still work, Meeting Locations card still renders, photo hero still works.

## Self-review coverage report

(initially empty; populated by the implementing model per SESSION-PROTOCOL.md Section C)

## Completed

(initially empty; populated by the implementing model per SESSION-PROTOCOL.md Section D)
