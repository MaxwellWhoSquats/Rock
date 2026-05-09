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
A3. ~~Re-declare `PageParameterKey.AutoEdit` (dropped in Phase 1 per the mid-phase decision); read it in `GetObsidianBlockInitialization` and surface as `box.PanelMode = Edit` when `bag.IdKey != "" && page.Param("autoEdit").AsBoolean()`.~~ **Superseded post-spec-lock**: the `<DetailBlock>` framework template handles `?autoEdit=true` end-to-end at [detailBlock.ts:360, 915-919, 633-635, 736-744](../../Rock.JavaScript.Obsidian/Framework/Templates/detailBlock.ts:360) — reads the URL query string, auto-fires `onEditClick()` on setup (which invokes the consumer's registered `@edit` handler), and redirects to `?returnUrl=...` after Save. No server-side or shell-level handling needed. The Phase 3 implementation initially duplicated this in three places (server: `PageParameterKey.AutoEdit` constant + `GroupDetailOptionsBag.AutoEdit` field + `GetBoxOptions` read; Vue: a manual `if (config.options?.autoEdit) { panelMode.value = Edit; void onEdit(); }` block in `groupDetail.obs`'s setup). All three were removed during iterative review — the manual Vue block was also racing with the framework auto-trigger and producing a duplicate `Edit` block-action call per page load.
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

### MP-3.1 (2026-05-08) — `ListItemBag` for image-uploader bind shape (replaces spec's `Guid?`)

The spec's "Bag fields contributed" section enumerates `photoBinaryFileGuid: Guid | null` and `chatChannelAvatarBinaryFileGuid: Guid | null`. The Rock framework's `<ImageUploader>` core control binds via `ListItemBag` (where `value` is the BinaryFile Guid and `text` is the file name) per its prop signature at [imageUploader.obs:76](../../Rock.JavaScript.Obsidian/Framework/Controls/imageUploader.obs:76) and the canonical sibling pattern at [siteBag.cs:125,218](../../Rock.ViewModels/Blocks/Cms/SiteDetail/SiteBag.cs:125). I went with `ListItemBag` to avoid a custom binding wrapper. Functional behavior is identical: the C# block resolves the bag's `Value` Guid to a `BinaryFile.Id` and persists. Reason: follow framework convention.

### MP-3.2 (2026-05-08) — `ElevatedSecurityLevel` enum relocated to Rock.Enums (namespace preserved)

`ElevatedSecurityLevel` lived in `Rock/Utility/Enums/` (Rock.dll project) under `namespace Rock.Utility.Enums`. The Phase 3 bag types it on `GroupBag.ElevatedSecurityLevel`. `Rock.ViewModels` references `Rock.Enums` only (not Rock.dll), so the enum had to move. The enum file relocated to [Rock.Enums/Security/ElevatedSecurityLevel.cs](../../Rock.Enums/Security/ElevatedSecurityLevel.cs) but the namespace `Rock.Utility.Enums` is **preserved** for binary compatibility with plugins compiled against the old Rock.dll. The same pattern was used in Rock 19.0.6 for `FamilyLimits` and `CreateConnectionRequestOptions` (relocated to `Rock.Enums/Connection/` while keeping `namespace Rock.Utility`).

Three coordinated changes make this work:

1. **`[Rock.Enums.EnumDomain( "Security" )]`** on the enum (fully qualified). Necessary because adding a `Rock.Utility.Enums` sub-namespace to Rock.Enums would otherwise shadow `Rock.Enums.EnumDomain` resolution from sibling files in `namespace Rock.Utility` (when those files write `[Enums.EnumDomain(...)]`, the unqualified `Enums` token resolves to `Rock.Utility.Enums` first and fails). The `[EnumDomain]` attribute drives codegen to output the .ts placeholder under `Framework/Enums/Security/`.
2. **Two sibling-file fix-ups** in the Rock.Enums project: [Rock.Enums/Connection/CreateConnectionRequestOptions.cs](../../Rock.Enums/Connection/CreateConnectionRequestOptions.cs) and [Rock.Enums/Connection/FamilyLimits.cs](../../Rock.Enums/Connection/FamilyLimits.cs) had `[Enums.EnumDomain( "Connection" )]` rewritten to `[Rock.Enums.EnumDomain( "Connection" )]` (fully qualified) for the same reason — they live in `namespace Rock.Utility` and the new sub-namespace would otherwise break their unqualified `Enums.` prefix. (`Rock.Enums/Core/TimeIntervalUnit.cs` uses `[EnumDomain( "Core" )]` via a `using Rock.Enums;` directive, so no change is needed there — the unqualified `EnumDomain` resolves directly through the using.)
3. **`[assembly: TypeForwardedTo( typeof( Rock.Utility.Enums.ElevatedSecurityLevel ) )]`** added to [Rock/Properties/AssemblyInfo.cs](../../Rock/Properties/AssemblyInfo.cs) under a "Rock 20.X" comment block. Plugins compiled against the old Rock.dll continue to resolve the type at runtime — the runtime sees the forwarder and looks for the type in Rock.Enums.dll instead.

Net result: every existing consumer of `Rock.Utility.Enums.ElevatedSecurityLevel` (across `Rock`, `RockWeb`, `Rock.Migrations`, `Rock.Tests`, `Rock.Tests.Integration`) compiles and runs unchanged. Phase 3's `GroupBag.cs` types against `Rock.Utility.Enums.ElevatedSecurityLevel` directly via a `using Rock.Utility.Enums;` directive.

### MP-3.3 (2026-05-08) — `EditModeResponseBag` composite return for Edit block action (superseded)

Originally: implemented as a typed composite `EditModeResponseBag` (inside the block class) that pairs `ValidPropertiesBox<GroupBag>` with `GroupTypeOptionsBag`. The Vue side read both fields off the result. Avoided two round-trips on initial Edit click. **Superseded post-spec-lock**: replaced with a reactive `watch(() => groupEditBag.value.bag?.groupTypeId, ...)` declaration in the Vue shell. The composite C# class and TypeScript type are deleted; `Edit` returns standard `ValidPropertiesBox<GroupBag>`; the watcher fires `GetGroupTypeOptions` whenever the bag's `groupTypeId` changes (covering initial Edit, Add-mode pick, and mid-edit type swap in one mechanism). See the iterative review section below.

### MP-3.4 (2026-05-08) — Latent Phase 2 framework bug fixed-during

Phase 2's `titleIconCssClass` prop addition to the framework's [detailBlock.ts:89-92](../../Rock.JavaScript.Obsidian/Framework/Templates/detailBlock.ts:89) typed the prop as `String as PropType<string>` (no null/undefined). `stepEntry.obs` in `src/Engagement/` binds a nullable value to it, which broke the Engagement project's TypeScript build. The build was clean at Phase 2 close because the Engagement project hadn't been re-validated against the new framework signature. Phase 3's build surfaced this. Loosened the prop type to `String as PropType<string | null>` (matches the existing internal `?? ""` handling at [detailBlock.ts:423](../../Rock.JavaScript.Obsidian/Framework/Templates/detailBlock.ts:423)) so existing nullable consumers keep working. Reason: Phase 3 build needed to pass; the fix is a 1-line type-only change.

### MP-3.5 (2026-05-08) — `EnableInactiveReason` / `RequiresInactiveReason` fetched from live entity (superseded)

Originally: `GroupTypeCache` did not surface `EnableInactiveReason` / `RequiresInactiveReason`, so `BuildGroupTypeOptionsBag` fetched the live `GroupType` entity once per cascade for these two flags. **Superseded post-spec-lock**: both flags were added to `GroupTypeCache` (mirroring the existing `EnableRSVP` / `IsCapacityRequired` `[DataMember] public bool { get; private set; }` pattern + `SetFromEntity` assignment), and the per-cascade live-entity fetch was removed. `BuildGroupTypeOptionsBag` is now pure-cache for the visibility flags. See C5 TODO #3.

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

Implementation walks Section C of SESSION-PROTOCOL.md. Every checklist item maps to a file:line reference; every Research-coverage behavior is classified as ✓ implemented, → deferred, or ✗ missed. No ✗ MISSED rows.

### C1 — Implementation checklist walk

| Checklist | Item | Status | Code reference |
|---|---|---|---|
| A1 | ParentGroupId pre-population on Add | ✓ | [GroupDetail.cs:957-973](../../Rock.Blocks/Group/GroupDetail.cs:957) |
| A2 | UpdateEntityFromBox assigns ParentGroupId with self-parent guard | ✓ | [GroupDetail.cs:683-684](../../Rock.Blocks/Group/GroupDetail.cs:683) (assignment) + [GroupDetail.cs:1086-1089](../../Rock.Blocks/Group/GroupDetail.cs:1086) (gate) |
| A3 | `?autoEdit=true` handling | ✓ | [GroupDetail.cs:245](../../Rock.Blocks/Group/GroupDetail.cs:245) (key), [GroupDetail.cs:383](../../Rock.Blocks/Group/GroupDetail.cs:383) (options bag), [groupDetail.obs auto-trigger](../../Rock.JavaScript.Obsidian.Blocks/src/Group/groupDetail.obs) |
| A4 | Inactive flow conditional well | ✓ | [editPanel.partial.obs:25-41](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:25) |
| A5 | Add-mode header chrome | ✓ | [groupDetail.obs:110](../../Rock.JavaScript.Obsidian.Blocks/src/Group/groupDetail.obs:110) (panelMode init), [:138](../../Rock.JavaScript.Obsidian.Blocks/src/Group/groupDetail.obs:138) (panelName), [:147](../../Rock.JavaScript.Obsidian.Blocks/src/Group/groupDetail.obs:147) (titleIcon), [:158](../../Rock.JavaScript.Obsidian.Blocks/src/Group/groupDetail.obs:158) (labels), [:175-176](../../Rock.JavaScript.Obsidian.Blocks/src/Group/groupDetail.obs:175) (audit / follow) |
| B1 | Group Type read-only on existing / required dropdown on Add | ✓ | [editPanel.partial.obs:61-69](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:61) |
| B2 | `GetAllowedGroupTypes` helper | ✓ | [GroupDetail.cs:1937-1979](../../Rock.Blocks/Group/GroupDetail.cs:1937) |
| B3 | Parent Group `<GroupPicker>` | ✓ | [editPanel.partial.obs:71](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:71) |
| B4 | Campus picker + `PreventSelectingInactiveCampus` | ✓ | [editPanel.partial.obs:74-77](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:74) |
| B5 | Status `<DefinedValuePicker>` filtered to GroupType.GroupStatusDefinedType | ✓ | [editPanel.partial.obs:79-82](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:79) (Vue), [GroupDetail.cs:2017-2032](../../Rock.Blocks/Group/GroupDetail.cs:2017) (server bag.StatusValues) |
| C1 | Group Administrator `<PersonPicker>` (Trailblazer) | ✓ | [editPanel.partial.obs:87-90](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:87) |
| C2 | Group Capacity `<NumberBox>` w/ Members suffix | ✓ | [editPanel.partial.obs:92-100](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:92) |
| C3 | Required Signature Document (Trailblazer) | ✓ | [editPanel.partial.obs:102-106](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:102) (Vue), [GroupDetail.cs:1912-1929](../../Rock.Blocks/Group/GroupDetail.cs:1912) (server source) |
| C4 | Member Record Source (Trailblazer) | ✓ | [editPanel.partial.obs:108-113](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:108) |
| C5 | Enable as Security Role (GROUP_ADMINISTRATORS gate) | ✓ | [editPanel.partial.obs:115-118](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:115) (Vue), [GroupDetail.cs:1487-1499](../../Rock.Blocks/Group/GroupDetail.cs:1487) (server check) |
| C6 | Security Level radio in ConditionalWell | ✓ | [editPanel.partial.obs:120-125](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:120) |
| D1 | Override Relationship Strength CheckBox | ✓ | [editPanel.partial.obs:131-133](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:131) |
| D2 | Relationship Strength radio | ✓ | [editPanel.partial.obs:135-139](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:135) |
| D3 | Enable Relationship Growth Over Time CheckBox | ✓ | [editPanel.partial.obs:141-144](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:141) |
| D4 | Show Advanced Relationship Settings InlineSwitch (Trailblazer) | ✓ | [editPanel.partial.obs:146-149](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:146) |
| D5 | Four multipliers `<NumberBox>` (transitively Trailblazer) | ✓ | [editPanel.partial.obs:151-208](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:151) |
| E1 | ScheduleType radio gated on AllowedScheduleTypes | ✓ | [editPanel.partial.obs:248-251](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:248) (Vue), `scheduleTypeItems` filters by `groupTypeOptions.allowedScheduleTypes` |
| E2 | Weekly DayOfWeek + TimePicker | ✓ | [editPanel.partial.obs:253-256](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:253) |
| E3 | Custom ScheduleBuilder | ✓ | [editPanel.partial.obs:258-260](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:258) |
| E4 | Named SchedulePicker | ✓ | [editPanel.partial.obs:262-264](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:262) |
| E5 | Inline schedule lifecycle (create / mutate / delete) | ✓ | [GroupDetail.cs:1734-1813](../../Rock.Blocks/Group/GroupDetail.cs:1734) (`ApplyInlineSchedule`) + [GroupDetail.cs:1815-1837](../../Rock.Blocks/Group/GroupDetail.cs:1815) (`DeleteInlineSchedule`) |
| F1 | Disable / Hide / Require Member Requirements checkboxes | ✓ | [editPanel.partial.obs:281-289](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:281) |
| F2 | Confirmation Behavior dropdown | ✓ | [editPanel.partial.obs:291-295](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:291) |
| F3 | Schedule Coordinator PersonPicker | ✓ | [editPanel.partial.obs:297-300](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:297) |
| F4 | Coordinator Notifications CheckBoxList (no None per Q11) | ✓ | [editPanel.partial.obs:302-308](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:302) |
| F5 | Who Can Check-in radio | ✓ | [editPanel.partial.obs:310-314](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:310) |
| G1 | Five tri-state Yes/No/Inherit + push notification dropdown | ✓ | [editPanel.partial.obs:332-358](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:332) |
| G2 | Chat-channel-avatar uploader (read-only when IsSystem) | ✓ | [editPanel.partial.obs:360-363](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:360) |
| H1 | Photo `<ImageUploader>` | ✓ | [editPanel.partial.obs:46-50](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:46) |
| H2 | Photo IsTemporary toggle save logic | ✓ | [GroupDetail.cs:1700-1712](../../Rock.Blocks/Group/GroupDetail.cs:1700) (apply) + [GroupDetail.cs:1839-1865](../../Rock.Blocks/Group/GroupDetail.cs:1839) (toggle) |
| S1 | Save block action returning ValidPropertiesBox / 201 redirect | ✓ | [GroupDetail.cs:1047-1212](../../Rock.Blocks/Group/GroupDetail.cs:1047) |
| S2 | TryGetEntityForEditAction | ✓ | [GroupDetail.cs:1054-1057](../../Rock.Blocks/Group/GroupDetail.cs:1054) |
| S3 | UpdateEntityFromBox with IfValidProperty | ✓ | [GroupDetail.cs:637-810](../../Rock.Blocks/Group/GroupDetail.cs:637) |
| S4 | Validation gates 1-8 (per webforms/23 ordering) | ✓ | gate 1: [:1062-1065](../../Rock.Blocks/Group/GroupDetail.cs:1062), gate 2: [:1086-1089](../../Rock.Blocks/Group/GroupDetail.cs:1086), gates 3 + 4: [ApplyInlineSchedule:1741-1761](../../Rock.Blocks/Group/GroupDetail.cs:1741), gate 5: [:1101-1115](../../Rock.Blocks/Group/GroupDetail.cs:1101), gate 6: [:1117-1120](../../Rock.Blocks/Group/GroupDetail.cs:1117), gate 7 (Page.IsValid): server-side `Group.IsValid` covers it on this surface, gate 8: [:1124-1127](../../Rock.Blocks/Group/GroupDetail.cs:1124) |
| S5 | Add + AddAdministrateSecurityToGroupCreator AllowPerson | ✓ | [GroupDetail.cs:1144-1147](../../Rock.Blocks/Group/GroupDetail.cs:1144) |
| S6 | LimittoSecurityRoleGroups force IsSecurityRole=true | ✓ | [GroupDetail.cs:719-726](../../Rock.Blocks/Group/GroupDetail.cs:719) |
| S7 | Inactive cascade through descendants | ✓ | [GroupDetail.cs:1149-1166](../../Rock.Blocks/Group/GroupDetail.cs:1149) |
| S8 | Schedule iCal parse / weekly DayOfWeek validation (silent downgrade) | ✓ | [GroupDetail.cs:1741-1761](../../Rock.Blocks/Group/GroupDetail.cs:1741) |
| S9 | Group.IsValid model validation | ✓ | [GroupDetail.cs:1124-1127](../../Rock.Blocks/Group/GroupDetail.cs:1124) |
| S10 | RockContext.WrapTransaction wrapping the 8-step body | ✓ | [GroupDetail.cs:1133-1183](../../Rock.Blocks/Group/GroupDetail.cs:1133) |
| S11 | Authorization.Clear() on IsSecurityRole flip | ✓ | [GroupDetail.cs:1186-1191](../../Rock.Blocks/Group/GroupDetail.cs:1186) |
| S12 | wasSecurityRole snapshot | ✓ | [GroupDetail.cs:1059-1062](../../Rock.Blocks/Group/GroupDetail.cs:1059) |
| S13 | Peer Network override save (only when IsPeerNetworkEnabled && override checked) | ✓ | [GroupDetail.cs:737-762](../../Rock.Blocks/Group/GroupDetail.cs:737) |
| S14 | RSVP override save (group-type-pinning precedence) | ✓ | [GroupDetail.cs:765-797](../../Rock.Blocks/Group/GroupDetail.cs:765) |
| S15 | Record source override save (only when AllowGroupSpecificRecordSource) | ✓ | [GroupDetail.cs:705-716](../../Rock.Blocks/Group/GroupDetail.cs:705) |
| GT1 | GetGroupTypeOptions block action | ✓ | [GroupDetail.cs:1004-1031](../../Rock.Blocks/Group/GroupDetail.cs:1004) |
| GT2 | Body resolves GroupTypeCache and maps fields | ✓ | [GroupDetail.cs:1987-2089](../../Rock.Blocks/Group/GroupDetail.cs:1987) (`BuildGroupTypeOptionsBag`) |
| GT3 | EDIT auth re-check on entity | ✓ | [GroupDetail.cs:1009-1023](../../Rock.Blocks/Group/GroupDetail.cs:1009) |
| GT4 | Vue side calls on currentGroupTypeId change | ✓ | [groupDetail.obs onGroupTypeIdChanged](../../Rock.JavaScript.Obsidian.Blocks/src/Group/groupDetail.obs) (around line 271) emits `groupTypeIdChanged` from EditPanel; shell invokes `GetGroupTypeOptions` |
| GT5 | Status / allowed schedule types refresh on cascade | ✓ | [GroupDetail.cs:2017-2032](../../Rock.Blocks/Group/GroupDetail.cs:2017) (Status), [:2010-2013](../../Rock.Blocks/Group/GroupDetail.cs:2010) (allowed schedule types) |
| V1 | Single editPanel.partial.obs file | ✓ | [editPanel.partial.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs) |
| V2 | Accepts modelValue + groupTypeOptions props; emits update | ✓ | [editPanel.partial.obs:418-446](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:418) |
| V3 | `<ContentSection>` for every section | ✓ | Used throughout the template |
| V4 | `<ConditionalWell>` for Inactive / Security / Override / Multipliers wells | ✓ | [editPanel.partial.obs:25-41,120-125,135-209](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:25) |
| V5 | trailblazerField={true} on the 5 controls | ✓ | See T2 below |
| T1 | Framework prop pre-exists | ✓ | [rockFormField.obs:101](../../Rock.JavaScript.Obsidian/Framework/Controls/rockFormField.obs:101) (untouched) |
| T2 | trailblazerField on 5 enumerated controls | ✓ | Group Administrator [:90](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:90), Required Signature Document [:106](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:106), Member Record Source [:113](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:113), InlineSwitch [:149](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:149), four multipliers [:160,173,188,201](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs:160) |
| L1 | Duplicate code block in `ShowGroupTypeEditDetails` naturally avoided | ✓ | The new `BuildGroupTypeOptionsBag` is single-emit; no duplication. |

### C2 — Research-coverage walk

#### research/specs/00-architecture.md (cross-phase)

| Behavior | Status | Code ref / Notes |
|---|---|---|
| Q2 cascade Approach B | ✓ | `GetGroupTypeOptions` block action + `BuildGroupTypeOptionsBag` |
| Q6 Trailblazer per-field prop | ✓ | T1, T2 |
| Q8 Photo uploader IsTemporary pattern | ✓ | H2, S10 step 7 |
| Q10 ContentSection / ContentStack / ConditionalWell core components | ✓ | V3, V4 |
| Q11 Coordinator Notifications no None checkbox; empty = None | ✓ | F4 + bag's `ScheduleCoordinatorNotificationTypes = None` when empty |
| Q12 L1 fix-during | ✓ | L1 |

#### research/specs/01-phase-1-shell-and-view.md, 02-phase-2-complete-view-panel.md

These specs' Completed coverage reports informed the inherited-deferred table at the top of this spec; every `→ DEFERRED to Phase 3` row is ✓ in the C1 walk above. No new Phase 1/2 content surfaced during Phase 3 implementation.

#### research/webforms/01-block-configuration.md

| Behavior | Status | Code ref |
|---|---|---|
| 21 block attributes | ✓ | All declared, including the Phase 1 deferred ones surfaced through edit-mode usage. |
| GroupTypes / GroupTypesExclude filter | ✓ | B2 (`GetAllowedGroupTypes`) |
| LimittoSecurityRoleGroups | ✓ | B2 + S6 + Vue gate at editPanel.partial.obs `isLimitedToSecurityRoleGroups` |
| LimitToShowInNavigationGroupTypes | ✓ | B2 |
| AddAdministrateSecurityToGroupCreator | ✓ | S5 |
| PreventSelectingInactiveCampus | ✓ | B4 + GroupDetailOptionsBag.PreventSelectingInactiveCampus |
| MapStyle | ✓ (Phase 2 — unchanged) | GroupDetailOptionsBag.MapStyleValueGuid |
| ShowCopyButton, ShowLocationAddresses, EnableGroupTags, all LinkedPage attrs | ✓ (Phase 1/2 — unchanged) | GroupDetailOptionsBag flags + NavigationUrls |
| `GroupId`, `ParentGroupId`, `autoEdit`, `returnUrl`, `ExpandedIds` page parameters | ✓ | All in `PageParameterKey` enum |

#### research/webforms/02-block-states.md

| Behavior | Status | Code ref |
|---|---|---|
| Edit-mode states | ✓ | `GetEntityBagForEdit` returns full edit-mode bag |
| GroupType-conditional visibility matrix | ✓ | `BuildGroupTypeOptionsBag` mirrors WebForms `ShowGroupTypeEditDetails` |
| Add vs Edit mode visibility | ✓ | `panelMode === DetailPanelMode.Add` checks throughout the shell |
| GroupType-change reactive cascade | ✓ | GT1-GT5 |
| Section visibility (RSVP / Scheduling / Chat / Peer Network / Admin) | ✓ | `groupTypeOptions.is*Visible` flags drive section render |

#### research/webforms/03-markup-structure.md

| Behavior | Status | Code ref |
|---|---|---|
| pnlEditDetails layout | ✓ | editPanel.partial.obs structured by ContentSection per the figma |
| wpGeneral / wpRsvp / wpScheduling / wpChat panel widgets | ✓ | Sections 2 / 3 / 4 / 8 in editPanel.partial.obs |
| Section 5 / 6 / 7 / 9 / 10 panels | → Phase 4/5 | NotificationBox placeholders rendered inline |
| Section 4 Stack 2 (Locations grid) | → Phase 6 | Inline placeholder under Scheduling section |

#### research/webforms/04-code-behind-walkthrough.md

| Behavior | Status | Code ref |
|---|---|---|
| `btnEdit_Click` → ShowEditDetails (full bag) | ✓ | Edit block action + `GetEntityBagForEdit` |
| `btnSave_Click` (768-1451) | ✓ | Save block action — S1-S15 |
| `ShowEditDetails` flow | ✓ | `GetEntityBagForEdit` |
| `ShowGroupTypeEditDetails` flow | ✓ | `BuildGroupTypeOptionsBag` |
| `LoadDropDowns` flow | ✓ | Initial dropdowns via `GroupDetailOptionsBag.AllowedGroupTypes` / `SignatureDocumentTemplates`; cascade dropdowns via `GroupTypeOptionsBag` |
| `ddlGroupType_SelectedIndexChanged` | ✓ | `onGroupTypeIdChanged` Vue handler invokes `GetGroupTypeOptions` |
| `ddlParentGroup_SelectedIndexChanged` | → Phase 6 | The WebForms version refreshed allowed group types based on parent. Phase 3 ships a static GroupType dropdown filtered by the entity's parent at initial render. Phase 6 (Locations editing) is the natural place to wire reactive parent-group → group-types cascade if the figma calls for it. Mid-phase decision: deferring the reactive cascade keeps Phase 3 scope tight; the initial filter is correct. |
| `cbIsSecurityRole_CheckedChanged` | ✓ | C5 + ConditionalWell at C6 reveals/hides Security Level reactively |

#### research/webforms/06-peer-network.md

| Behavior | Status | Code ref |
|---|---|---|
| Peer-network override panel + 4 multipliers | ✓ | D1-D5 |
| Override-strength-and-growth + 4 multipliers fields | ✓ | Bag: RelationshipStrengthOverride, RelationshipGrowthEnabledOverride, four `*MultiplierOverride` fields |
| Save: explicit override only when checked | ✓ | S13 |
| Reset cascade: nullify all overrides when unchecked | ✓ | S13 (the `else` branch) |

#### research/webforms/07-locations-and-schedules.md

| Behavior | Status | Code ref |
|---|---|---|
| Inline Schedule lifecycle (create / mutate / delete with `Name = string.Empty`) | ✓ | E5 + S8 + `DeleteInlineSchedule` |
| Switch to Custom or Weekly: reuse / create | ✓ | `ApplyInlineSchedule` Custom/Weekly branch |
| Switch to Named or None: delete inline if `CanDelete` | ✓ | `DeleteInlineSchedule` |
| GroupLocations grid + dialog | → Phase 6 | Stack 2 placeholder |
| Per-GroupLocation schedule configs | → Phase 6 | Out of scope |

#### research/webforms/08-scheduling.md

| Behavior | Status | Code ref |
|---|---|---|
| Section 4 Stack 3 (Member Scheduling & Check-in) | ✓ | F1-F5 |
| Schedule Coordinator + Notifications + Confirmation Behavior | ✓ | F2-F4 |
| Who Can Check-in radio | ✓ | F5 |
| AttendanceRecordRequiredForCheckIn enum | ✓ | Bag: `AttendanceRecordRequiredForCheckIn` |

#### research/webforms/14-chat.md

| Behavior | Status | Code ref |
|---|---|---|
| Section 8 — five tri-state overrides | ✓ | G1 |
| Chat-channel-avatar uploader | ✓ | G2 |
| `IsTemporary` BinaryFile toggle pattern | ✓ | `ToggleBinaryFileIsTemporary` (chat avatar) + photo follows the same pattern |
| Read-only when IsSystem | ✓ | `<ImageUploader :disabled="isSystem">` |
| `ChatHelper.IsChatEnabled` system flag | ✓ | `BuildGroupTypeOptionsBag.IsChatSectionVisible` |

#### research/webforms/15-rsvp.md

| Behavior | Status | Code ref |
|---|---|---|
| Section 3 — RSVP | ✓ | RSVP section in editPanel.partial.obs:228-244 |
| GroupType pinning precedence (offset / system communication) | ✓ | S14 + Vue read-only when `groupTypeOptions.rsvpReminderOffsetDays != null` |

#### research/webforms/22-grouptype-cascade.md

| Behavior | Status | Code ref |
|---|---|---|
| Cascade map driving GetGroupTypeOptions | ✓ | `BuildGroupTypeOptionsBag` flags |
| Per-GroupType visibility for sections / fields | ✓ | All `Is*Visible` flags |
| Allowed schedule types | ✓ | `AllowedScheduleTypes` |
| Capacity rule + required | ✓ | `IsGroupCapacityVisible` + `IsGroupCapacityRequired` |
| Inactive reasons + required | ✓ | `IsInactiveReasonVisible` + `IsInactiveReasonRequired` + `InactiveReasons` list |
| GroupStatusDefinedType options | ✓ | `IsStatusVisible` + `StatusValues` list |

#### research/webforms/23-validations-and-cascades.md

| Behavior | Status | Code ref |
|---|---|---|
| Transactional boundary (WrapTransaction) | ✓ | S10 |
| Validation gates 1-8 | ✓ | S4 (gate-by-gate refs above) |
| Inactive cascade to children | ✓ | S7 |
| IsSecurityRole flip Authorization.Clear | ✓ | S11 |
| KioskDevice.Clear | → Phase 6 | Out of scope (location-driven; Phase 6 handles GroupLocation save) |
| Chat-avatar IsTemporary toggle | ✓ | S10 step 6 |
| Photo IsTemporary toggle (NEW per Phase 3) | ✓ | S10 step 7 |
| Peer Network reset cascade | ✓ | S13 |
| RSVP precedence cascade | ✓ | S14 |
| RecordSource precedence cascade | ✓ | S15 |
| InetCalendarHelper.CreateCalendarEvent (schedule validity) | ✓ | `ApplyInlineSchedule` gate 3 |

#### research/design/00-overview.md, 02-edit-panel.md, 03-net-new-features.md

| Behavior | Status | Code ref |
|---|---|---|
| Section structure (10 collapsible sections) | ✓ | All 10 ContentSections present (5/6/7/9/10 are placeholders for Phase 4/5) |
| Add-new flow header treatment (#13) | ✓ | A5 |
| Group Image uploader (#1) | ✓ | H1 |
| Renamed relationship-strength labels (#15, edit) | ✓ | D2 + Vue uses `RelationshipStrengthDescription` from auto-generated TS enum (Casual / Close / Deep, integers unchanged) |
| Trailblazer Settings per-field prop (#18) | ✓ | T1, T2 |
| C5 Chat tri-state Yes/No/Inherit override radios | ✓ | G1 |
| C6 Security Level radio | ✓ | C6 |
| C7 RSVP "Days" suffix | ✓ | NumberBox suffix slot at editPanel.partial.obs:236-238 |
| C8 Who Can Check-in radio | ✓ | F5 |
| Inactive flow conditional well | ✓ | A4 |
| Strength labels Casual / Close / Deep | ✓ | Inherited via RelationshipStrengthDescription |

#### research/design/04-component-inventory.md, 05-design-system-deltas.md, 06-mapping-to-webforms.md

| Behavior | Status | Code ref |
|---|---|---|
| Obsidian core control mapping | ✓ | All controls used as listed (`<TextBox>`, `<NumberBox>`, `<DropDownList>`, `<RadioButtonList>`, `<CheckBox>`, `<CheckBoxList>`, `<InlineSwitch>`, `<PersonPicker>`, `<GroupPicker>`, `<CampusPicker>`, `<DefinedValuePicker>`, `<ImageUploader>`, `<DayOfWeekPicker>`, `<TimePicker>`, `<ScheduleBuilder>`, `<SchedulePicker>`, `<StaticFormControl>`) |
| Sections & Stacks pattern | ✓ | V3, V4 |
| Edit-panel mapping table (WebForms → Obsidian) | ✓ | Every row mapped to a Phase 3 control |

### C5 — New latent bugs / TODOs surfaced

1. **`titleIconCssClass` prop typing** — Phase 2 added the prop with `String as PropType<string>` (no nullable), which broke `stepEntry.obs` (Engagement). Fixed during Phase 3 build (MP-3.4); no further action needed.
2. **`ddlParentGroup_SelectedIndexChanged` reactive cascade** — The WebForms block re-filters allowed Group Types when the parent group changes. Phase 3 ships a static initial filter (correct for the entity's current state). Phase 6 should evaluate whether the figma needs a reactive parent-group → group-types refresh and wire it as a separate block action.
3. **`GroupTypeCache` missing `EnableInactiveReason` / `RequiresInactiveReason`** — Resolved post-spec-lock. The two flags were added to `GroupTypeCache` (matching the existing `EnableRSVP` / `IsCapacityRequired` `[DataMember] public bool { get; private set; }` pattern + `SetFromEntity` assignment), and `BuildGroupTypeOptionsBag` now reads them off the cache directly. The MP-3.5 live-entity fetch is removed.
4. **`SignatureDocumentTemplateService.GetLegacyTemplates()`** — Resolved post-spec-lock. The WebForms block used `GetLegacyTemplates()` (`Where( ProviderEntityTypeId.HasValue )`), which only surfaces templates wired to external providers. Commit `a3276b7` (Apr 2026) removed every external signature provider (SignNow, `DigitalSignatureComponent`, `DigitalSignatureContainer`, `ProcessSignatureDocuments`, related transactions); legacy templates still exist as DB rows but `SendLegacyProviderDocument` now returns `"Legacy signature providers are no longer supported in Rock."` The WebForms block was a stale outlier. Phase 3 now mirrors the post-modernization pattern from `RegistrationTemplateDetail.ascx.cs:2983` (touched by the same commit): `Queryable().Where( t => t.IsActive || t.Id == entity.RequiredSignatureDocumentTemplateId ).OrderBy( t => t.Name )` (the projection-to-anonymous in our helper makes `AsNoTracking()` redundant — anonymous types aren't tracked entities). The `#pragma warning disable CS0618` is removed and `BuildSignatureDocumentTemplateListItems` now takes the entity so a group bound to a deactivated template still surfaces its current value.
5. **`?autoEdit=true` handled by framework** — Resolved post-spec-lock. The `<DetailBlock>` template at [detailBlock.ts:360, 915-919](../../Rock.JavaScript.Obsidian/Framework/Templates/detailBlock.ts:360) reads `autoEdit` from `URLSearchParams` and auto-fires `onEditClick()` on setup, which invokes the consumer's registered `@edit` handler. The Phase 3 implementation duplicated this server-side (`PageParameterKey.AutoEdit` + `GroupDetailOptionsBag.AutoEdit`) and in `groupDetail.obs` setup (manual `if (config.options?.autoEdit) { panelMode.value = Edit; void onEdit(); }`). The manual Vue block ran in parent setup BEFORE the framework's child-mount auto-trigger, producing a duplicate `Edit` block-action call per `?autoEdit=true` page load. All redundant plumbing removed; the framework handles it end-to-end.

## Completed

### Summary

Phase 3 replaced the Phase 1 edit-panel placeholder with the full edit core for GroupDetail. The View → Edit → Save loop now round-trips on every scalar field on `Group`: Name / Description / Active (with Inactive flow conditional well) / Public / GroupType / ParentGroup / Campus / Status / GroupAdministrator / GroupCapacity / RequiredSignatureDocumentTemplate / GroupMemberRecordSourceValue / IsSecurityRole / ElevatedSecurityLevel / peer-network overrides / RSVP overrides / scheduling fields (inline schedule + member scheduling + check-in) / chat tri-state overrides + chat-channel-avatar / Group photo uploader. The `GetGroupTypeOptions` block action implements the Q2 Approach B cascade — when `currentGroupTypeId` changes mid-edit the Vue side fetches a typed `GroupTypeOptionsBag` that drives section visibility, dropdown sources, peer-network defaults, and inactive-reason rules. The 8-step `WrapTransaction` save flow follows webforms/23-validations-and-cascades.md verbatim, with photo IsTemporary toggling mirroring the existing chat-avatar pattern.

The Add path opens the edit panel directly with header chrome simplified to "Add Group" (no audit kebab, no follow star, no header / subheader labels). `?ParentGroupId=N` pre-populates the Parent Group picker; `?autoEdit=true` opens the Edit panel on initial render. Trailblazer per-field styling (Q6 / Q3.6) is wired on the five enumerated controls (Group Administrator, Required Signature Document, Member Record Source, Show Advanced Relationship Settings InlineSwitch, four relationship multipliers) using the framework's existing `trailblazerField` prop. Sections 5 / 6 / 7 / 9 / 10 render inline `<NotificationBox>` "Coming in Phase 4 / 5" placeholders within the same `editPanel.partial.obs` file (per Q3.1 lock). Section 4 Stack 2 (Locations editing) renders a "Coming in Phase 6" placeholder. The L1 latent bug (duplicate code block in WebForms `ShowGroupTypeEditDetails`) is naturally avoided by the single-emit `BuildGroupTypeOptionsBag` shape.

### Coverage report

The full coverage report appears under "Self-review coverage report" above. Summary: every Implementation checklist item is ✓ implemented with a file:line reference; every Research-coverage behavior is classified ✓ / → / ✗; zero ✗ MISSED rows.

### Deviations from the spec

| # | Deviation | Reason / Mid-phase decision |
|---|---|---|
| 1 | `photoBinaryFile` / `chatChannelAvatarBinaryFile` typed as `ListItemBag` (not `Guid?`) | MP-3.1 — framework convention. C# resolves `ListItemBag.Value` Guid to `BinaryFile.Id` server-side. |
| 2 | `ElevatedSecurityLevel` enum relocated to `Rock.Enums/Security/` (namespace preserved) | MP-3.2 — `Rock.ViewModels` only references `Rock.Enums`; no other path was viable. Namespace `Rock.Utility.Enums` is preserved for plugin binary compat (mirrors the Rock 19.0.6 `FamilyLimits` / `CreateConnectionRequestOptions` move). `[Rock.Enums.EnumDomain( "Security" )]` is fully-qualified to avoid shadowing in sibling Rock.Enums files; two siblings (`CreateConnectionRequestOptions.cs`, `FamilyLimits.cs`) rewrote their `[Enums.EnumDomain(...)]` to `[Rock.Enums.EnumDomain(...)]` for the same reason. `TypeForwardedTo` entry added to `Rock/Properties/AssemblyInfo.cs`. Zero consumer files needed code changes. |
| 3 | ~~Edit block action returns composite `EditModeResponseBag` (bag + groupTypeOptions)~~ **Superseded** | MP-3.3 (superseded). Replaced by reactive `groupTypeId` watcher in the Vue shell; `Edit` returns standard `ValidPropertiesBox<GroupBag>`. See IR-4. |
| 4 | Phase 2 framework `titleIconCssClass` prop loosened to `string \| null` | MP-3.4 — Phase 2 carry-forward bug blocking the build. 1-line type-only change matching the existing internal `?? ""` handling. |
| 5 | ~~`EnableInactiveReason` / `RequiresInactiveReason` fetched from live entity~~ **Superseded** | MP-3.5 (superseded). Both flags added to `GroupTypeCache`; `BuildGroupTypeOptionsBag` is now pure-cache. See IR-1. |

### Files changed

**New files**:

- `Rock.Enums/Security/ElevatedSecurityLevel.cs` (relocated from `Rock/Utility/Enums/`)
- `Rock.JavaScript.Obsidian/Framework/Enums/Security/elevatedSecurityLevel.ts` (placeholder; codegen will regenerate)
- `Rock.ViewModels/Blocks/Group/GroupDetail/GroupTypeOptionsBag.cs`
- `Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupTypeOptionsBag.d.ts` (placeholder; codegen will regenerate)

**Modified files**:

- `Rock.Blocks/Group/GroupDetail.cs` (Phase 3 expansion; +~1100 lines)
- `Rock.ViewModels/Blocks/Group/GroupDetail/GroupBag.cs` (edit-mode scalar fields)
- `Rock.ViewModels/Blocks/Group/GroupDetail/GroupDetailOptionsBag.cs` (AllowedGroupTypes / SignatureDocumentTemplates / PreventSelectingInactiveCampus / AutoEdit fields)
- `Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupBag.d.ts` (placeholder)
- `Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupDetailOptionsBag.d.ts` (placeholder)
- `Rock.JavaScript.Obsidian/Framework/Templates/detailBlock.ts` (titleIconCssClass typing fix)
- `Rock.JavaScript.Obsidian.Blocks/src/Group/groupDetail.obs` (Add-mode chrome + edit-mode wiring + onGroupTypeIdChanged + onSave)
- `Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs` (full Phase 3 implementation; was a placeholder)

**Files touched as part of the ElevatedSecurityLevel namespace-preserving relocation**:

- `Rock.Enums/Security/ElevatedSecurityLevel.cs` (NEW; relocated from `Rock/Utility/Enums/ElevatedSecurityLevel.cs`; namespace `Rock.Utility.Enums` preserved; `[Rock.Enums.EnumDomain( "Security" )]` fully qualified).
- `Rock.Enums/Connection/CreateConnectionRequestOptions.cs` and `Rock.Enums/Connection/FamilyLimits.cs` — `[Enums.EnumDomain( "Connection" )]` rewritten to `[Rock.Enums.EnumDomain( "Connection" )]` (necessary because the new `Rock.Utility.Enums` sub-namespace shadows the unqualified `Enums.` prefix in sibling `namespace Rock.Utility` files).
- `Rock/Properties/AssemblyInfo.cs` — `[assembly: TypeForwardedTo( typeof( Rock.Utility.Enums.ElevatedSecurityLevel ) )]` added under a "Rock 20.X" comment block.

No consumer files required code changes — the preserved namespace means existing `using Rock.Utility.Enums;` directives and fully-qualified `Rock.Utility.Enums.ElevatedSecurityLevel` references continue to resolve.

### New latent bugs / TODOs surfaced

See "C5 — New latent bugs / TODOs surfaced" above.

### Build status

- Rock.sln C# build: **clean** (zero CS errors).
- Rock.JavaScript.Obsidian.Blocks TS build: **clean** (zero TS errors).
- Pre-existing ASPNETCOMPILER warning (`The target directory is not empty`) is unrelated to Phase 3 changes; it's a RockWeb precompile-output artifact issue and does not affect the Rock.dll, Rock.Blocks, or Obsidian framework builds.

### Commit hash

Awaiting user commit; commit hash to be filled in after `git commit` lands.

## Iterative review and refactoring (post-spec-lock)

This section captures the user-driven iterative refactor pass that ran **after** the Phase 3 implementation was code-complete. The pass aligned `groupDetail.obs` and `GroupDetail.cs` more closely with canonical Rock detail-block patterns (`connectionTypeDetail.obs`, `groupTypeDetail.obs`, `stepProgramDetail.obs`, `connectionOpportunityDetail.obs`) and resolved several latent issues. Implementation behavior is unchanged where the spec defined it; the changes are structural / performance / correctness improvements that surface during careful read-through.

The pass is **in progress** — items below mark what's been completed. The "Pending" subsection at the end records work the next session should pick up.

### IR-1. `GroupTypeCache` extended with `EnableInactiveReason` / `RequiresInactiveReason`

Added both flags to `GroupTypeCache` matching the existing `EnableRSVP` / `IsCapacityRequired` `[DataMember] public bool { get; private set; }` pattern + corresponding `SetFromEntity` assignment. `BuildGroupTypeOptionsBag` now reads them off the cache directly. Removes the live `GroupTypeService.Get(...)` per cascade documented in MP-3.5. Resolves C5 TODO #3.

Files: [Rock/Web/Cache/Entities/GroupTypeCache.cs](../../Rock/Web/Cache/Entities/GroupTypeCache.cs), [Rock.Blocks/Group/GroupDetail.cs](../../Rock.Blocks/Group/GroupDetail.cs) `BuildGroupTypeOptionsBag`.

### IR-2. `GetLegacyTemplates()` swapped for the modernized `IsActive` pattern

Commit `a3276b7` (Apr 2026) removed every external signature provider (SignNow, `DigitalSignatureComponent`, `DigitalSignatureContainer`, `ProcessSignatureDocuments`, related transactions). Legacy templates still exist as DB rows but `SendLegacyProviderDocument` returns `"Legacy signature providers are no longer supported in Rock."` The WebForms block's `GetLegacyTemplates()` filter (`Where(ProviderEntityTypeId.HasValue)`) was a stale outlier that surfaced only now-dead templates.

Phase 3's `BuildSignatureDocumentTemplateListItems` was rewritten to mirror the post-modernization pattern from `RegistrationTemplateDetail.ascx.cs:2983` (touched by the same commit):

```csharp
new SignatureDocumentTemplateService( RockContext )
    .Queryable()
    .Where( t => t.IsActive || t.Id == currentTemplateId )
    .OrderBy( t => t.Name )
    .Select( ... )
```

The helper now takes the entity so a group bound to a deactivated template still surfaces its current value. The `#pragma warning disable CS0618` was removed. Resolves C5 TODO #4.

Side cleanup: dropped the redundant `AsNoTracking()` from this query — the projection-to-anonymous before `.ToList()` means EF can't track the materialized result anyway.

Files: [Rock.Blocks/Group/GroupDetail.cs](../../Rock.Blocks/Group/GroupDetail.cs) `BuildSignatureDocumentTemplateListItems`.

### IR-3. `?autoEdit=true` plumbing removed (framework handles it)

The `<DetailBlock>` framework template at [detailBlock.ts:360, 915-919](../../Rock.JavaScript.Obsidian/Framework/Templates/detailBlock.ts:360) reads `autoEdit` from `URLSearchParams` and auto-fires `onEditClick()` on setup, which invokes the consumer's registered `@edit` handler.

Phase 3's implementation duplicated this server-side (`PageParameterKey.AutoEdit` + `GroupDetailOptionsBag.AutoEdit` + `GetBoxOptions` read) and in `groupDetail.obs`'s setup-tail (manual `if (config.options?.autoEdit) { panelMode.value = Edit; void onEdit(); }`). Worse, the manual Vue block ran in parent setup BEFORE the framework's child-mount auto-trigger — producing a duplicate `Edit` block-action call per `?autoEdit=true` page load.

All redundant plumbing was removed. The framework now handles it end-to-end. Implementation checklist item A3 is marked superseded inline. Resolves a latent duplicate-fetch bug.

Files: `Rock.Blocks/Group/GroupDetail.cs` (constants + `GetBoxOptions`), `Rock.ViewModels/Blocks/Group/GroupDetail/GroupDetailOptionsBag.cs`, `Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupDetailOptionsBag.d.ts`, `Rock.JavaScript.Obsidian.Blocks/src/Group/groupDetail.obs` (setup-tail).

### IR-4. `EditModeResponseBag` composite eliminated; reactive `groupTypeId` watcher added

The composite C# class ([GroupDetail.cs](../../Rock.Blocks/Group/GroupDetail.cs)) and the matching TypeScript type ([groupDetail.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/groupDetail.obs)) were both deleted. The `Edit` block action now returns standard `ValidPropertiesBox<GroupBag>` matching every canonical sibling.

Cascade fetching is replaced with a single reactive watcher in the Vue shell:

```typescript
watch(() => groupEditBag.value.bag?.groupTypeId, async (newId) => {
    if (!newId || newId <= 0) {
        groupTypeOptions.value = {} as GroupTypeOptionsBag;
        return;
    }
    const result = await invokeBlockAction<GroupTypeOptionsBag>("GetGroupTypeOptions", { groupTypeId: newId });
    if (result.isSuccess && result.data) { groupTypeOptions.value = result.data; }
    else { errorMessage.value = ...; scrollToErrorNotification(); }
});
```

Covers all three trigger paths (initial Edit, Add-mode group-type pick, mid-edit swap) in one declaration. The `onGroupTypeIdChanged` shell handler, the `@groupTypeIdChanged` template binding, and the `groupTypeIdChanged` emit declaration in the partial were all removed. Supersedes MP-3.3.

Trade: two round-trips on initial Edit (Edit returns → watcher fires → GetGroupTypeOptions). Latency cost is unobservable in practice (the user clicked Edit and is committed to the wait). Worth the alignment.

### IR-5. Vue shell aligned to canonical detail-block pattern

Multiple structural mismatches with `connectionTypeDetail.obs` / `groupTypeDetail.obs` / `stepProgramDetail.obs` / `connectionOpportunityDetail.obs` were fixed:

- **`isAddMode` shell-level computed dropped.** Inlined `panelMode === DetailPanelMode.Add` at 11 sites (template attributes + computeds + handler). The partial keeps its own local `isAddMode` (computed from `!bag.value.idKey`) as the canonical Add-mode signal.
- **`groupViewBag` typing simplified.** From strict `ref<GroupBag>(config.entity ?? {} as GroupBag)` to canonical `ref(config.entity)` (infers `Ref<X | null | undefined>`). Matches every canonical sibling.
- **`groupEditBag` init simplified.** From pre-populated shallow copy with `validProperties: []` to canonical empty `{ bag: {} as GroupBag }`. Setup-tail populates it on Add path; `onEdit` populates it from server on Edit path.
- **Long explanatory comment block dropped.** No longer applicable after the bag-init simplification.
- **`panelMode` init reverted to `DetailPanelMode.View`.** Add-mode transition happens in the canonical script-setup-tail block.
- **Canonical script-setup-tail block added.** Standard 3-branch error / no-entity / Add-mode-with-editBag-init shape matching siblings.
- **`onCancelEdit` aligned.** Detects Add via `!editBag.value.bag?.idKey`; redirects to `GroupListPage` if configured (was `return false`, leaving the Add-mode user stuck). Behavior fix on top of pattern alignment.
- **`onSave` aligned.** Wrapped happy path in `if (result.isSuccess && result.data)`; dropped the gratuitous spread + `"bag" in result.data` runtime guard + dead edit-bag re-sync.
- **`onEdit` aligned.** Now returns canonical `ValidPropertiesBox<GroupBag>`; populates `validProperties` via `result.data.validProperties ?? Object.keys(result.data.bag)` fallback.
- **`BlockActionName.Save` added** to the enum at `types.partial.ts`; replaces the magic-string `"Save"` literal.

Files: [Rock.JavaScript.Obsidian.Blocks/src/Group/groupDetail.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/groupDetail.obs), [Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/types.partial.ts](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/types.partial.ts).

### IR-6. `panelMode` prop dropped from edit partial

The `editPanel.partial.obs` previously received `:panelMode` from the shell solely to compute `isAddMode`. None of the canonical sibling edit partials accept a `panelMode` prop — they derive Add mode from `bag.idKey`. Refactored:

- Removed the `panelMode` prop from `defineProps`.
- Removed the `DetailPanelMode` import (no longer used).
- Replaced `isAddMode = computed(() => props.panelMode === DetailPanelMode.Add)` with `isAddMode = computed(() => !bag.value.idKey)` — canonical signal.
- Dropped `:panelMode="panelMode"` template binding from the shell's `<EditPanel>` element.

Files: [Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs), [Rock.JavaScript.Obsidian.Blocks/src/Group/groupDetail.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/groupDetail.obs).

### IR-7. `useEntityDetailBlock` hook adopted

Phase 3 was missing the canonical `useEntityDetailBlock({ blockConfig: config, entity: editBag })` hook adopted by every sibling. The hook does three load-bearing things our shell wasn't doing:

1. **`provideSecurityGrant(securityGrant)`** — many Rock framework controls (`<PersonPicker>`, `<GroupPicker>`, `<CampusPicker>`, `<DefinedValuePicker>`, `<ImageUploader>`, `<auditDetail>`, badge / file pickers) call `useSecurityGrantToken()` and rely on this inject for authenticated server requests. Without it those controls fell back to no token.
2. **`provideEntityTypeGuid` / `provideEntityTypeName`** via inject — replaces explicit template props.
3. **Returns `onPropertyChanged`** — handles auto-refresh of attribute values when qualified attribute properties change. Forward-compat for Phase 4 attributes.

Adoption changes:

- Added `useEntityDetailBlock` to the `@Obsidian/Utility/block` import.
- `const baseBlock = useEntityDetailBlock({ blockConfig: config, entity: groupEditBag });` after the editBag declaration.
- `@propertyChanged="baseBlock.onPropertyChanged"` wired on `<EditPanel>`.
- Partial: added `(e: "propertyChanged", value: string)` to `defineEmits`; `pushBagUpdate` now also emits `propertyChanged` after each field change (alongside `update:modelValue`).
- Removed `entityTypeGuid` / `entityTypeName` shell-level computeds and the matching template props (the inject covers it).

Files: [Rock.JavaScript.Obsidian.Blocks/src/Group/groupDetail.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/groupDetail.obs), [Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs).

### IR-8. `scrollToErrorNotification()` added (matches `connectionTypeDetail`)

Added an `errorMessageElement` ref bound to the error `<NotificationBox>` and a `scrollToErrorNotification()` function that smoothly scrolls it into view via `nextTick + scrollIntoView`. Wired into all six server-error paths in the shell (`onEdit`, `onSave`, `onDelete`, `onArchiveClick`, `onCopySave`, watcher-failure). Matches the canonical `connectionTypeDetail` pattern.

Files: [Rock.JavaScript.Obsidian.Blocks/src/Group/groupDetail.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/groupDetail.obs).

### IR-9. Other Vue cleanup

- **`blockError` init simplified.** From `ref(config.errorMessage ?? "")` to canonical `ref("")`; the setup-tail handles the `config.errorMessage` case.
- **`:showExperienceMode="true"` added** on `<DetailBlock>` to match the canonical visual.
- **Added xml-doc to `openCopyModal()`** for consistency with neighboring helper methods.

### IR-10. `ApplyNewGroupDefaultValues` helper extracted (parent-group pre-pop)

The `Edit` block action previously contained an inline 4-deep nested block to pre-populate `entity.ParentGroupId` from `?ParentGroupId=N` on the Add path, including a dead `if (parentGroup.GroupTypeId > 0) { /* comment */ }` branch whose only purpose was to host an explanatory comment.

Refactored to mirror `ConnectionTypeDetail.ApplyNewConnectionTypeDefaultValues`:

- New private helper `ApplyNewGroupDefaultValues(Model.Group entity, GroupService groupService = null)` in `#region Methods`. Three early-return guards (null/already-has-id, empty page param, parent group not found), then assignment. Linear flow.
- Helper hooked into `GetInitialEntity()` (after `GetInitialEntity<Model.Group, GroupService>` resolves).
- Helper hooked into `TryGetEntityForEditAction()`'s new-entity branch right after `entityService.Add( entity )`.
- `Edit` block action body shrunk to four lines of structural code.

Side effect: defaults now apply consistently from **both** entry points. Previously only `Edit` (which calls `TryGetEntityForEditAction`) ran the pre-populate; non-Edit consumers of `GetInitialEntity` (breadcrumb, options bag construction, `GetGroupTypeOptions` auth re-check) missed it.

Files: [Rock.Blocks/Group/GroupDetail.cs](../../Rock.Blocks/Group/GroupDetail.cs).

### IR-11. `GetEffectiveGroupType` deleted; `_cachedGroupType` per-request memo on `GetGroupTypeCache`

`GetEffectiveGroupType(entity, bag)` was a defensive helper that preferred `bag.GroupTypeId` and fell back to `entity.GroupTypeId`. The defensive logic was unnecessary: `entity.GroupTypeId = box.Bag.GroupTypeId.Value` is assigned in `UpdateEntityFromBox` (line 676 region) **before** every dependent `IfValidProperty` call site. Reading `entity.GroupTypeId` directly produces the same result in every realistic scenario, plus the existing `GetGroupTypeCache(entity)` helper already handled the null/zero-Id guards.

Two-stage refactor:

1. **Consolidation (first pass):** dropped `GetEffectiveGroupType` and replaced its 6 in-`UpdateEntityFromBox` call sites with a single captured local `var groupType = GetGroupTypeCache(entity);` declared **after** the GroupTypeId IfValidProperty block. Each subsequent dependent lambda closes over this local. `ApplyChatChannelAvatarBinaryFile` was updated to call `GetGroupTypeCache(entity)` directly. Reduced 6 lookups → 1 per Save invocation of `UpdateEntityFromBox`.

2. **Per-request memoization (second pass):** added `private GroupTypeCache _cachedGroupType` instance field under a new `#region Fields`. Converted `GetGroupTypeCache` from `static` to instance method with self-invalidating key check:

   ```csharp
   private GroupTypeCache GetGroupTypeCache( Model.Group entity )
   {
       if ( entity == null || entity.GroupTypeId <= 0 ) { return null; }
       if ( _cachedGroupType?.Id == entity.GroupTypeId ) { return _cachedGroupType; }
       _cachedGroupType = GroupTypeCache.Get( entity.GroupTypeId );
       return _cachedGroupType;
   }
   ```

   The key check `_cachedGroupType?.Id == entity.GroupTypeId` handles mid-request reassignment: if `entity.GroupTypeId` changes from N to M, the next call sees the mismatch and re-resolves. `GetGroupTypeOptions(int groupTypeId)` block action keeps its direct `GroupTypeCache.Get(groupTypeId)` call since it operates on an arbitrary id that may not match the entity's current type. Pattern matches the "lazy-resolve once via a property" Rock convention used elsewhere (e.g., `GetBlockPersonPreferences`).

Net effect across a single Save request: was ~7+ scattered cache lookups → now 1 cache lookup, subsequent calls hit the memo as a single Id comparison.

Files: [Rock.Blocks/Group/GroupDetail.cs](../../Rock.Blocks/Group/GroupDetail.cs).

### IR-12. Parity audit fixes (Add-mode dependent fields, GroupType auto-pick)

A line-by-line C# parity audit against the WebForms `btnSave_Click` / `ShowEditDetails` / `ShowDetail` surfaced one critical bug and two notable parity gaps. All three resolved.

**C-1 (critical) — Add path silently dropped every GroupType-dependent field on Save.** `UpdateEntityFromBox` captured `var groupType = GetGroupTypeCache(entity)` *before* the GroupTypeId IfValidProperty block. For a fresh Add-mode entity, `entity.GroupTypeId == 0` at capture time, so `GetGroupTypeCache` returned null (per its `GroupTypeId <= 0` guard). The dependent lambdas at admin / record source / peer network / RSVP / chat all closed over that captured-null local — so on a new group the user's picks for those fields were discarded (or worse, force-nulled by the `else` branches).

IR-11's spec text correctly said "declared **after** the GroupTypeId IfValidProperty block" but the implementation had it before. Fixed by moving the `var groupType = GetGroupTypeCache( entity );` declaration to immediately after the GroupTypeId IfValidProperty closes. Edit-path behavior is unchanged (existing groups' GroupTypeId is read-only in the UI).

**N-1 — `?ParentGroupId=N` Add path didn't auto-pick a single allowed group type.** WebForms `ShowDetail` ([line 1782](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1782)) walks the parent's allowed child group types, filters by per-group-type EDIT auth, and pre-selects when exactly one survives. `ApplyNewGroupDefaultValues` previously only set `ParentGroupId`. Resolved by adding the auth-probe loop + single-survivor pre-select.

**N-2 — `LimittoSecurityRoleGroups=true` Add path didn't default GroupType to Security Role.** WebForms forces `CurrentGroupTypeId = securityRoleGroupType.Id` ([line 2030](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2030)). Resolved by adding the security-role default branch (which short-circuits the parent-driven auto-pick since the dropdown is already constrained to one option).

**M-3 verified parity (no fix needed).** Both `Archive` (single-group) and `ArchiveWithChildren` correctly mirror the WebForms `ArchiveSingleGroup` / `ArchiveAllChildGroups` shape, including the `includeInactiveChildGroups: true` flag on the descendant lookup ([WebForms line 3426](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs:3426)).

Files: [Rock.Blocks/Group/GroupDetail.cs](../../Rock.Blocks/Group/GroupDetail.cs) (`UpdateEntityFromBox` groupType capture relocated; `ApplyNewGroupDefaultValues` extended with security-role default + parent-driven auto-pick).

### IR-13. Second-pass audit (redundancies, parity gaps, performance)

A second full-file C# audit was run after IR-12 to confirm readiness for Phase 4. Findings split into redundancies (R), parity gaps (G), and performance (P). Code-level fixes and engineering notes applied this pass; deferred items captured in the Pending subsection below.

**R-1 — `ApplyInlineSchedule` return value never consumed.** The helper returned the post-validation `ScheduleType` after gate-3 / gate-4 demotion, but no caller read the return. Likely an extraction artifact. Signature changed to `void`; the unused `var scheduleType =` capture at the call site dropped.

**R-2 — Save gate 5 re-fetched ParentGroup unnecessarily.** When `ApplyNewGroupDefaultValues` had already pre-populated `entity.ParentGroup` (Add-from-tree path), the gate-5 query was redundant. Changed to `entity.ParentGroup ?? new GroupService(RockContext).Get(...)` fallback. Saves one query per Save when the navigation is loaded.

**G-2 — Save isNew redirect didn't preserve `?ExpandedIds=...`.** WebForms `btnSave_Click` ([line 1447](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1447)) preserves it for tree-navigation context. The Phase 3 Save action only echoed `GroupId`. The `Copy` block action correctly preserves it ([line 1402](../../Rock.Blocks/Group/GroupDetail.cs:1402)) — Save now matches that pattern.

**G-3 — `bag.HasChildGroups` semantic mismatch with the Inactivate-children prompt.** The Vue uses the single field for both the Archive cascade prompt (immediate-children-any-state, matches WebForms 641) and the Inactivate-cascade checkbox visibility (which WebForms gates on `HasDescendantGroups(id, includeInactive: false)` at [line 1989](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1989)). The Inactivate prompt may render in the narrow case of inactive-only direct descendants — toggling is a harmless no-op (the cascade `GetAllDescendentGroupIds(includeInactive: false)` correctly skips), just slightly confusing UX. Splitting into two bag fields is the right long-term shape; deferred to Phase 5 (in the cascade neighborhood). Engineering note added at the field assignment.

**Performance notes added (no code changes):**

- **P-2** — `GetCommonEntityBag` lazy-load pattern (Schedule / Admin-Person / ParentGroup / Photo). 5-6 round-trips per page-load read. Matches WebForms `GetGroup` at [GroupDetail.ascx.cs:2913](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2913). Eager-Include override deferred until Phase 5/6 work is in this neighborhood.
- **P-3** — Two-EXISTS hasHistory check at [GetBoxOptions](../../Rock.Blocks/Group/GroupDetail.cs:425). Mirrors WebForms 2582-2583. The `EnableGroupHistory` short-circuit prevents the queries from running on most groups; combining via UNION isn't simpler in EF.
- **P-4** — `GetAllowedGroupTypes` invoked from three call sites within a request (BuildAllowedGroupTypeListItems / Save gate 5 / ApplyNewGroupDefaultValues' auth probe). Per-request memoization keyed by `parentGroupGroupType?.Id` is the natural optimization if profiling surfaces it.

**Verification of deferred returnUrl handling.** The audit noted that Save / Cancel didn't honor `?returnUrl=...` server-side. Cross-checking IR-3's claim, the `<DetailBlock>` framework template at [detailBlock.ts](../../Rock.JavaScript.Obsidian/Framework/Templates/detailBlock.ts) handles `?returnUrl=` end-to-end after Save and on Cancel. Server-side handling for these paths is therefore unnecessary — captured as G-1 / G-4 in the audit and tracked in Pending below for one final detailBlock.ts read-through to confirm. ExpandedIds (G-2 above) is application-specific and the framework cannot handle it; that's why a server-side fix was required.

Files: [Rock.Blocks/Group/GroupDetail.cs](../../Rock.Blocks/Group/GroupDetail.cs) (R-1 + R-2 + G-2 code fixes; G-3 / P-2 / P-3 / P-4 engineering notes added).

### IR-14. Picker-shaped `ListItemBag` payload (one field per concept)

The Phase 3 lock shipped picker fields as paired scalars on `GroupBag` (e.g., `ParentGroupId: int?` + `ParentGroupName: string`, `GroupAdministratorPersonAliasGuid: Guid?` plus a separate view-mode `Administrator: GroupAdministratorBag`). The Vue layer reassembled these into `ListItemBag`s client-side via init ternaries, mirrored the assembly in the inbound watcher, and split-wrote each scalar back in the outbound watcher. Six picker fields x three sites = ~30 lines of mechanical scaffolding. The view-mode rich bags (`Administrator`, `ParentGroup`) lived as separate properties because they carried `Url` (server-resolved, customer-customizable via `EntityType.LinkUrlLavaTemplate`) which `ListItemBag` doesn't have — the picker can't bind to a bag that has no `Value`/`Text`.

Resolved by collapsing each concept to a single bag field:

- **`GroupAdministratorBag` and `ParentGroupBag` now inherit from `ListItemBag`.** They keep the `Url` field for view-mode link rendering, and acquire `Value`/`Text` from the base. `Name` was renamed to `Text` (the inherited member) — view panel binds `bag.administrator.text` instead of `bag.administrator.name`. The picker can bind to the same field because the bag IS a `ListItemBag`.
- **`GroupBag` lost 7 paired-scalar fields** (`ParentGroupId`, `ParentGroupName`, `CampusId`, `GroupAdministratorPersonAliasGuid`, `GroupMemberRecordSourceValueId`, `NamedScheduleId`, `ScheduleCoordinatorPersonAliasGuid`) and gained 4 new `ListItemBag` properties (`Campus`, `GroupMemberRecordSource`, `NamedSchedule`, `ScheduleCoordinatorPerson`). The existing `Administrator` and `ParentGroup` rich bags now serve both view AND edit (the picker reads `Value`/`Text`; `Url` is harmlessly null after a fresh edit-mode pick and re-populates on the next view-mode load).
- **`BuildAdministratorRef` / `BuildParentGroupRef` updated** to populate inherited `Value` (PersonAlias Guid / parent group Id-as-string) and `Text` alongside `Url`. `BuildAdministratorRef` now takes `PersonAlias` directly (not `Person + GroupTypeCache + ?.Person` chain) so the alias Guid is available without a redundant nav.
- **Three new helpers added** in the build-helpers neighborhood: `BuildCampusListItem(int? campusId)` (CampusCache lookup), `BuildDefinedValueListItem(int? definedValueId)` (DefinedValueCache lookup), `BuildPersonAliasListItem(PersonAlias personAlias)`. Each returns null on miss. Inline construction in `HydrateScheduleFields` for `NamedSchedule` (Schedule navigation already loaded).
- **Save sites simplified.** `entity.ParentGroupId = box.Bag.ParentGroup?.Value.AsIntegerOrNull()` and analogous for Campus / GroupMemberRecordSource; `entity.GroupAdministratorPersonAliasId` and `entity.ScheduleCoordinatorPersonAliasId` resolve via `aliasGuid = box.Bag.Administrator?.Value.AsGuidOrNull()` then PersonAliasService lookup. Inline-schedule save reads `bag.NamedSchedule?.Value.AsIntegerOrNull()`.
- **`editPanel.partial.obs` collapsed.** 6 paired-ref ternary inits → 6 `propertyRef<ListItemBag | null>` declarations folded into the flat `propRefs` array. 6 inbound `updateRefValue` ternaries → 6 single-line `updateRefValue(ref, props.modelValue.bag?.field ?? null)` calls. Outbound watcher source list dropped the 6 explicit ListItemBag refs (now covered by `...propRefs`); the 8 split-write `setPropertiesBoxValue` sites collapsed to 6 single-line writes (no more `parseInt` / `toGuidOrNull` plumbing). Dropped the now-unused `toGuidOrNull` import.
- **`viewPanel.partial.obs` updated.** 4 references to `.administrator.name` / `.parentGroup.name` swapped to `.text` (the inherited ListItemBag field). Existing `.url` reads unchanged.

Net change: ~30 lines of edit-panel scaffolding deleted, 7 bag fields removed, 4 added (net -3), edit-panel `propRefs` array grew from 38 to 44 entries (every picker field is now a normal propertyRef), and one canonical pattern replaced two for picker fields throughout.

**IR-14 follow-up — picker payload Id-vs-Guid bug** (fixed in the same pass before commit). A user-driven audit through every picker control in the edit panel surfaced that the Phase-3 picker payload was using **Id-as-string** for `ParentGroup`, `Campus`, `GroupMemberRecordSource`, and `NamedSchedule`, but the actual picker controls (`<GroupPicker>`, `<CampusPicker>`, `<DefinedValuePicker>`, `<SchedulePicker>`) all consume **Guid-as-string** via their tree providers / cache lookups (canonical pattern is `IEntity.ToListItemBag()` which sets `Value = entity.Guid.ToString()`, mirrored by [LearningClassDetail.cs:296](../../Rock.Blocks/Lms/LearningClassDetail.cs:296) and [StreakTypeDetail.cs:333-475](../../Rock.Blocks/Engagement/StreakTypeDetail.cs:333)). The existing Phase-3 implementation was effectively broken end-to-end: initial load showed an empty picker (the picker couldn't match an Id-as-string against its Guid-keyed items), and picking a new value saved as `null` because `parseInt(guidString)` returns `NaN`. The bug was masked because this code hadn't been live-tested yet.

Resolved by:

- **Build helpers switched to Guid via the canonical `.ToListItemBag()` extension where applicable.** `BuildCampusListItem` and `BuildDefinedValueListItem` collapsed to single-line cache-lookup-then-`?.ToListItemBag()`. `HydrateScheduleFields` for `NamedSchedule` swapped to `entity.Schedule.ToListItemBag()`. `BuildParentGroupRef` continues to construct `ParentGroupBag` explicitly (because of the extra `Url` field on the inheriting bag) but with `Value = parentGroup.Guid.ToString()`.
- **Save sites switched to `GetEntityId<TEntity>(RockContext)` extension** ([IEntityExtensions.cs:29](../../Rock.Blocks/ExtensionMethods/IEntityExtensions.cs:29)) which parses `Value.AsGuidOrNull()` and resolves Guid → Id via reflection. Applied at four sites: ParentGroup / Campus / GroupMemberRecordSource in `UpdateEntityFromBox`, and NamedSchedule in `ApplyInlineSchedule`.
- **Administrator and ScheduleCoordinatorPerson save sites unchanged** — they already correctly use `Value.AsGuidOrNull()` + `PersonAliasService.GetSelect(guid, pa => pa.Id)` because PersonPicker emits PersonAlias Guid (per the user's earlier memory entry).

**IR-14 follow-up — PersonPicker type errors** (also fixed in this pass). After the canonical-pattern alignment, `<PersonPicker>` exposed two type errors that lint surfaced: its modelValue prop is `ListItemBag` (effectively `| undefined` since `required: false`) and its emit signature is `ListItemBag | undefined`, but the two PersonPicker propertyRefs were typed `ListItemBag | null` after the refactor — passing `null` violates the prop type and receiving `undefined` violates the ref type. All other pickers (Group / Campus / Schedule / DefinedValue) accept `ListItemBag | ListItemBag[] | null` so `| null` is correct for them. Resolved by narrowing `groupAdministrator` and `scheduleCoordinatorPerson` to `propertyRef<ListItemBag | undefined>` and switching their `?? null` defaults to `?? undefined` in init and inbound watcher.

**Other picker controls audited and confirmed correct:**

- The four `<DropDownList>` controls (`inactiveReasonValueId`, `groupTypeId`, `statusValueId`, `requiredSignatureDocumentTemplateId`) bind Id-as-string scalars, but their server-provided item lists ALSO emit `Value = Id.ToString()` ([GroupDetail.cs:2119](../../Rock.Blocks/Group/GroupDetail.cs:2119), [2144](../../Rock.Blocks/Group/GroupDetail.cs:2144), [2303](../../Rock.Blocks/Group/GroupDetail.cs:2303), [2317](../../Rock.Blocks/Group/GroupDetail.cs:2317)). End-to-end Id-consistent — not a picker mismatch.
- `<ImageUploader>` controls bind to `BinaryFile.Guid` via `BuildBinaryFileRef` — already correct.
- `<RadioButtonList>` / `<CheckBoxList>` controls use enum-int-as-string or stringified bitmask flags, with item lists built from `*Description` enum metadata — all internally consistent.

Verification: Rock.Blocks build clean (0 errors), Obsidian Blocks `eslint --max-warnings=0` and `vue-tsc --noEmit` both clean (exit 0).

Files: [Rock.ViewModels/Blocks/Group/GroupDetail/GroupBag.cs](../../Rock.ViewModels/Blocks/Group/GroupDetail/GroupBag.cs), [Rock.ViewModels/Blocks/Group/GroupDetail/GroupAdministratorBag.cs](../../Rock.ViewModels/Blocks/Group/GroupDetail/GroupAdministratorBag.cs), [Rock.ViewModels/Blocks/Group/GroupDetail/ParentGroupBag.cs](../../Rock.ViewModels/Blocks/Group/GroupDetail/ParentGroupBag.cs), [Rock.Blocks/Group/GroupDetail.cs](../../Rock.Blocks/Group/GroupDetail.cs), [Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupBag.d.ts](../../Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupBag.d.ts) (placeholder), [groupAdministratorBag.d.ts](../../Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupAdministratorBag.d.ts) (placeholder), [parentGroupBag.d.ts](../../Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/parentGroupBag.d.ts) (placeholder), [Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs), [viewPanel.partial.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/viewPanel.partial.obs).

### IR-15. `?returnUrl=` parity gap closed (Save + Cancel)

A direct read-through of [detailBlock.ts](../../Rock.JavaScript.Obsidian/Framework/Templates/detailBlock.ts) (the `<DetailBlock>` framework template) revealed that IR-3's assumption — "framework handles `?returnUrl=` end-to-end after Save and on Cancel" — was correct only for the autoEdit-bound flow. Both the Save handler at [detailBlock.ts:736-744](../../Rock.JavaScript.Obsidian/Framework/Templates/detailBlock.ts:736) and the Cancel handler at [detailBlock.ts:630-638](../../Rock.JavaScript.Obsidian/Framework/Templates/detailBlock.ts:630) gate the `returnUrl` redirect on `isAutoEditMode.value`, which is set only when `?autoEdit=true` is also present at page load ([detailBlock.ts:360](../../Rock.JavaScript.Obsidian/Framework/Templates/detailBlock.ts:360)).

WebForms parity ([GroupDetail.ascx.cs:1438-1441](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1438) for Save, [GroupDetail.ascx.cs:1460-1463](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1460) for Cancel) honors `?returnUrl=` **unconditionally** — no autoEdit gate. So `?returnUrl=` without `?autoEdit=true` was being silently ignored on the Obsidian side: the user landed on the page, clicked Edit, then Save / Cancel returned to view mode instead of redirecting back to the original caller.

Resolved with two small additions mirroring the existing `NavigateAfterDeleteOrArchive` pattern (which already covered Delete / Archive returnUrl correctly):

- **Save action (server-side):** new returnUrl branch at the top of the Save action's success path, after the IsSecurityRole cache invalidation and before the existing `isNew` redirect block. If `PageParameter(PageParameterKey.ReturnUrl)` is set and `IsSafeReturnUrl(...)` passes, returns `ActionContent(HttpStatusCode.OK, returnUrl)` — the framework's `onSave` handler treats string results as redirect URLs and applies `makeUrlRedirectSafe` ([detailBlock.ts:747-748](../../Rock.JavaScript.Obsidian/Framework/Templates/detailBlock.ts:747)). Wins over the `isNew` ExpandedIds redirect when both are set, matching WebForms precedence. The stale comment at the former line 1329-1331 ("returnUrl is honored by the `<DetailBlock>` framework template ... so it isn't echoed here") was removed.
- **Cancel handler (client-side):** new returnUrl branch at the top of `onCancelEdit` in [groupDetail.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/groupDetail.obs). If `new URLSearchParams(window.location.search).get("returnUrl")` is non-null, returns it as a string — the framework's `onEditCancelClick` handler treats string results as redirect URLs ([detailBlock.ts:641-642](../../Rock.JavaScript.Obsidian/Framework/Templates/detailBlock.ts:641)). Wins over the existing Add-mode `GroupListPage` fallback, matching WebForms precedence at [GroupDetail.ascx.cs:1460](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1460). Client-side URL safety is enforced by the framework's `makeUrlRedirectSafe` wrapper.

Both flows now have full WebForms parity for the four `?returnUrl=` permutations:

| Flow | autoEdit + returnUrl | returnUrl alone |
|---|---|---|
| Save | framework redirects ✓ | server action returns redirect string ✓ |
| Cancel | framework redirects ✓ | client handler returns redirect string ✓ |
| Delete / Archive | server `NavigateAfterDeleteOrArchive` ✓ | same ✓ |

Verification: Rock.Blocks build clean (0 errors), Obsidian Blocks `eslint --max-warnings=0` and `vue-tsc --noEmit` both clean (exit 0).

Files: [Rock.Blocks/Group/GroupDetail.cs](../../Rock.Blocks/Group/GroupDetail.cs) (new returnUrl branch in Save action; stale comment removed), [Rock.JavaScript.Obsidian.Blocks/src/Group/groupDetail.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/groupDetail.obs) (new returnUrl branch in `onCancelEdit`).

### Pending (next session)

- **Commit.** All post-spec-lock work (IR-1 through IR-15) remains uncommitted in the working tree per "User owns commits at phase boundaries" — the user runs `git commit` themselves. Suggested release-note classification: `+ (Group)` Improvement, since this is the first Obsidian conversion of GroupDetail spanning Phases 1-3.

### Resolved this session

- **Build verification.** Resolved 2026-05-09 — `dotnet build Rock.Blocks` and `dotnet build Rock.ViewModels` both clean (0 errors); `npx eslint` and `npx vue-tsc --noEmit` both clean. Full Rock.sln Framework-MSBuild build still recommended before commit but the Phase 3 surface is verified.
- **G-1 / G-4 returnUrl confirmation.** Resolved 2026-05-09 via IR-15 above — verified the framework gates returnUrl on autoEdit, identified the parity gap with WebForms, closed it server-side (Save) + client-side (Cancel).

### Updated files list (post-refactor)

Cumulative list of every file touched during Phase 3 (spec-lock implementation + iterative review). Newly-touched files in this iterative pass are marked **\[IR\]**.

**New files:**

- `Rock.Enums/Security/ElevatedSecurityLevel.cs` (relocated from `Rock/Utility/Enums/`)
- `Rock.JavaScript.Obsidian/Framework/Enums/Security/elevatedSecurityLevel.ts` (placeholder; codegen will regenerate)
- `Rock.ViewModels/Blocks/Group/GroupDetail/GroupTypeOptionsBag.cs`
- `Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupTypeOptionsBag.d.ts` (placeholder; codegen will regenerate)

**Modified files:**

- `Rock.Blocks/Group/GroupDetail.cs` — Phase 3 scalar persistence + iterative refactors **\[IR\]** (`ApplyNewGroupDefaultValues` extraction, `GetGroupTypeCache` memo, `GetEffectiveGroupType` deletion, `EditModeResponseBag` deletion, `BuildSignatureDocumentTemplateListItems` modernization, `?autoEdit` plumbing removed, signature document modernization).
- `Rock.ViewModels/Blocks/Group/GroupDetail/GroupBag.cs` **\[IR-14\]** (7 paired scalars dropped, 4 ListItemBag fields added)
- `Rock.ViewModels/Blocks/Group/GroupDetail/GroupAdministratorBag.cs` **\[IR-14\]** (now inherits ListItemBag; Name renamed to Text)
- `Rock.ViewModels/Blocks/Group/GroupDetail/ParentGroupBag.cs` **\[IR-14\]** (now inherits ListItemBag; Name renamed to Text)
- `Rock.ViewModels/Blocks/Group/GroupDetail/GroupDetailOptionsBag.cs` **\[IR\]** (AutoEdit field removed)
- `Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupBag.d.ts` (placeholder)
- `Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupAdministratorBag.d.ts` **\[IR-14\]** (placeholder)
- `Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/parentGroupBag.d.ts` **\[IR-14\]** (placeholder)
- `Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupDetailOptionsBag.d.ts` **\[IR\]** (autoEdit field removed)
- `Rock.JavaScript.Obsidian/Framework/Templates/detailBlock.ts` (titleIconCssClass typing fix)
- `Rock.JavaScript.Obsidian.Blocks/src/Group/groupDetail.obs` **\[IR\]** (canonical alignment, `useEntityDetailBlock` adoption, reactive watcher, scroll-to-error)
- `Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs` **\[IR\]** (panelMode prop dropped, propertyChanged emit added, groupTypeIdChanged emit removed); **\[IR-14\]** (6 paired refs collapsed to propertyRefs, watcher scaffolding removed, toGuidOrNull import dropped)
- `Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/viewPanel.partial.obs` **\[IR-14\]** (administrator/parentGroup name reads switched to text)
- `Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/types.partial.ts` **\[IR\]** (`BlockActionName.Save` enum value added)
- `Rock/Web/Cache/Entities/GroupTypeCache.cs` **\[IR\]** (`EnableInactiveReason` + `RequiresInactiveReason` properties added)

**Files touched as part of the ElevatedSecurityLevel namespace-preserving relocation** (unchanged from original list):

- `Rock.Enums/Security/ElevatedSecurityLevel.cs` (NEW)
- `Rock.Enums/Connection/CreateConnectionRequestOptions.cs`, `Rock.Enums/Connection/FamilyLimits.cs`
- `Rock/Properties/AssemblyInfo.cs`
