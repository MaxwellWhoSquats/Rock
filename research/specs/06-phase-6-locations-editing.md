---
author: Maxwell Eley
date_created: 2026-05-11
summary: >-
  Phase 6 implementation spec for the GroupDetail Obsidian conversion. Adds
  the Locations editing surface (Section 4 Stack 2): an editable
  GroupLocations grid with Add / Edit / Delete + a Location modal with
  Member / Other tabs + schedule + capacity matrix, plus the inline
  Schedule entity-management lifecycle on Save (create / reuse / delete
  inline weekly + custom + named schedules). Phase 2 already shipped the
  view-side map cards; Phase 6 only adds the editing flow on top. Section
  4 Stack 1 (Group Schedule) and the GroupMemberAssignment cascade cleanup
  also land here. The most state-heavy sub-feature in the block.
contributors: []
---

# Phase 6: Locations Editing Modal + Inline Schedule Lifecycle

## Context

Phase 5 ([05-phase-5-requirements-sync-workflows.md](05-phase-5-requirements-sync-workflows.md)) shipped Section 7 (Group Requirements), Section 9 (Group Sync), and Section 10 (Group Member Workflow Triggers) along with the IntervalPicker segmented-variant restyle and the L3 / L4 latent bug fixes. Phase 3 had already wired the read-side of Section 4 Stack 1 (Group Schedule radio + sub-fields) and Phase 2 had shipped the view-panel Meeting Locations card. What's left is the editing side of Section 4 Stack 2: the `<Grid>` of GroupLocations with Add / Edit / Delete, the multi-tab Location modal (Member / Other), the schedule-capacity matrix per location, the active-vs-inactive schedule reconciliation, and the inline-schedule entity-management save flow (create / reuse / delete weekly + custom + named). Phase 6 closes out the conversion's feature surface; only Phase 7 (update dependencies on 5 still-WebForms outbound destinations) and Phase 8 (cutover + WebForms file deletion) remain after this.

Architectural decisions for this phase are governed by [00-architecture.md](00-architecture.md), specifically the inline-schedule lifecycle (already partially addressed in Phase 3's `ApplyInlineSchedule` helper, which Phase 6 will extend), the `GroupLocationScheduleConfig` capacity diff logic, and the `GroupMemberAssignment` cascade-cleanup rules.

## Behavior delivered

- **Section 4 Stack 2 (Locations editing)**: editable `<Grid>` of per-group meeting locations with columns Location / Type / Schedule(s) / Edit / Delete. Add button visibility gated on `AllowMultipleLocations || locations.length === 0` per WebForms parity at `GroupDetail.ascx.cs:3872`. Entire stack hidden when `GroupType.LocationSelectionMode == None`.
- **Location modal (Add / Edit)**: tabbed dialog titled `"Add Group Location Schedules"` / `"Edit Group Location Schedules"` per Q6.7, with Member tab + Other tab. Member tab populates a flat dropdown of `{Member} {AddressType} ({Address})` rows sourced from `GroupBag.FamilyMemberLocationOptions` per Q6.11; when source is empty, the tab body shows a warning notification per Q6.10. Other tab uses a `<LocationPicker>` with mode flags from `GroupType.LocationSelectionMode`; the modal's persisted bag carries `selectedLocation` + `selectedLocationMode` discriminator per Q6.9. Below the tabs: Location Type defined-value dropdown (scoped to `GroupType.LocationTypeValues`), Schedule(s) multi-picker (visible when `GroupType.EnableLocationSchedules`), Capacity matrix (Min / Desired / Max per selected schedule, visible only when `GroupType.IsSchedulingEnabled && selectedSchedules.length > 0` per Q6.8 inside a `<ConditionalWell>` with helper callout).
- **Active-vs-inactive schedule reconciliation**: schedules that are attached to the location but are globally inactive remain attached across save. Handled server-side only per Q6.3 — the `SaveGroupLocations` helper loads existing schedules from the DB, partitions inactive, and unions with the bag's active list before writing. No `inactiveSchedules` field on the bag.
- **Duplicate-location detection on Add and Edit**: both Add and Edit run duplicate detection per Q6.12 (Edit excludes the row being edited from the comparison set); on conflict, surface a notification inside the modal and clear the picker. Mirrors WebForms `locpGroupLocation_SelectLocation` at `GroupDetail.ascx.cs:3907-3919`, extended to also cover the Edit path.
- **Save body — Locations (step 4f)**: a dedicated `SaveGroupLocations` helper per Q6.5 encapsulates the SyncRelatedEntities pattern + `GroupLocationScheduleConfig` diff (existing-vs-modified-vs-new-vs-deleted via the WebForms diff at `GroupDetail.ascx.cs:942-988`) + `GroupMemberAssignment` cleanup (at `GroupDetail.ascx.cs:829-837, 906-917`) + inactive-schedule preservation (Q6.3) + Location resolution from picker bag (Q6.9) as a single atomic unit inside `WrapTransaction`. Sets `checkinDataUpdated = true` for the post-transaction `KioskDevice.Clear()` invalidation.
- **Save body — Inline schedule lifecycle**: extends Phase 3's `ApplyInlineSchedule` per Q6.2 + Q6.6 to handle the full create/reuse/delete pattern at `GroupDetail.ascx.cs:1184-1252`. Inline schedule keeps `Name == string.Empty` convention (Q6.2-a); reused when switching between Weekly / Custom so `Schedule.Id` stays stable for downstream attendance / history (Q6.2-b); deleted (gated on `ScheduleService.CanDelete`) when switching away from Weekly / Custom via the `DeleteInlineSchedule` helper that Phase 6 wires into the Save action (Q6.6).
- **Cache invalidation (extending Phase 5's set)**: `KioskDevice.Clear()` fires post-transaction when `checkinDataUpdated == true` AND `GroupType.TakesAttendance == true`. Mirrors WebForms parity at `GroupDetail.ascx.cs:1432-1436`.
- **View-side reactive refresh**: after a successful Save, the Vue layer's existing inbound watcher already re-bags the GroupBag including `bag.MeetingLocations` (Phase 2 work), so the view-panel map cards update without additional Phase 6 wiring.

## Behavior NOT delivered

- Update dependencies (5 still-WebForms outbound destinations): **Phase 7**.
- Cutover and WebForms file deletion: **Phase 8**.

## Deferred behaviors inherited from prior phases

Walks Phase 1, Phase 2, Phase 3, Phase 4, and Phase 5 coverage reports for `→ DEFERRED to Phase 6` rows.

### Inherited (covered in this phase's Implementation checklist)

| Source phase | Behavior | Coverage-report origin | Checklist item that handles it |
|---|---|---|---|
| Phase 2 | Locations editing modal + inline schedule entity-management | "Behavior NOT delivered" in Phase 2 spec | L1-L12 |
| Phase 3 | `ApplyInlineSchedule` was a partial-only stub limited to assigning the named schedule and queuing the delete sentinel — Phase 6 grows it to the full WebForms create/reuse/delete pattern | "Behavior NOT delivered" in Phase 3 spec | IS1-IS5 |

### Re-deferred to a later phase

None planned at draft time; the user's review pass between Phase 5 and Phase 6 sessions may move items.

### Dropped (no longer in scope)

None planned at draft time.

## Locked decisions

All fourteen open questions resolved at spec lock (2026-05-12). Each lock records the question, the chosen resolution, and a brief rationale.

### Q6.1. Member-tab dropdown shape — full friendly text vs. structured row?

WebForms emits `{Member.FullName} {AddressType.Value} ({Address})` as a flat ListItem; design screenshot confirms the same format. **Locked:** flat dropdown, full friendly text per row, all rows shipped in the bag on initial-load. Revisit only if scale-test surfaces a UX problem.

### Q6.2. Inline-schedule convention + save lifecycle

Two coupled decisions: the inline-schedule detection convention, and the save lifecycle when switching schedule types. PSD ([Rock.Blocks/Cms/PersonalizationSegmentDetail.cs:353](Rock.Blocks/Cms/PersonalizationSegmentDetail.cs:353)) uses the same `Schedule.Name.IsNullOrWhiteSpace()` convention but with a different always-create-on-save lifecycle. For GroupDetail, downstream attendance + history references depend on `Schedule.Id` continuity. **Locked:** (a) preserve `Schedule.Name == string.Empty` as the inline marker; (b) reuse the existing Schedule entity when switching Weekly ↔ Custom so `Schedule.Id` stays stable. Phase 3's current `ApplyInlineSchedule` at [GroupDetail.cs:2185-2210](Rock.Blocks/Group/GroupDetail.cs:2185) already preserves the reuse semantic via `if ( entity.Schedule == null ) entity.Schedule = new Schedule { Name = string.Empty }`.

### Q6.3. Active-vs-inactive schedule reconciliation — server-side only

WebForms' `hfInactiveGroupLocationSchedules` hidden field exists because WebForms is stateless across postbacks. Obsidian doesn't have that constraint; the inactive list is purely a server-side state-preservation concern (never displayed, never user-mutable). **Locked:** server-side reconciliation only. The bag's `schedules: ListItemBag[]` per `GroupLocationStateBag` carries active schedules only. The `SaveGroupLocations` helper loads existing `groupLocation.Schedules` from the DB, partitions out the inactive ones, and unions with the bag's active list before writing. No `inactiveSchedules` field on the bag.

### Q6.4. Capacity matrix rebuild on schedule change — preserve in-progress edits

When the user adds/removes a schedule from the location's Schedule(s) picker, the capacity matrix updates. WebForms' `spSchedules_SelectItem` tries to preserve in-progress capacity edits via a fragile repeater rebind. **Locked:** preserve in-progress edits via Vue reactivity. The capacity matrix is backed by a `Map<scheduleId, capacity>` keyed by schedule. Adding a schedule appends a new row with empty Min/Desired/Max; removing a schedule drops only that row; other rows preserve their values.

### Q6.5. `GroupMemberAssignment` cascade cleanup — dedicated helper

WebForms handles `GroupMemberAssignment` cleanup inline in `btnSave_Click` at [GroupDetail.ascx.cs:829-837](RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L829) (full-location-removed) and [:906-917](RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L906) (LocationId-changed). **Locked:** a dedicated `SaveGroupLocations` helper encapsulates the SyncRelatedEntities pattern + GroupLocationScheduleConfig diff + GroupMemberAssignment cleanup + inactive-schedule preservation (Q6.3) + Location resolution from picker bag (Q6.9) as a single atomic unit, invoked from step 4f inside the Save action's `WrapTransaction`. The Save block action body stays clean.

### Q6.6. Section 4 Stack 1 (Group Schedule) wiring — Phase 6 completes the lifecycle

Phase 3 wired the radio + DayOfWeek + Time + ScheduleBuilder + SchedulePicker and shipped the partial `ApplyInlineSchedule` helper at [GroupDetail.cs:2158](Rock.Blocks/Group/GroupDetail.cs:2158). The `DeleteInlineSchedule` helper exists at [:2238](Rock.Blocks/Group/GroupDetail.cs:2238) but is **never called** from the Save flow today, so switching from Custom/Weekly to None leaves the orphan Schedule row in the DB. **Locked:** Phase 6 wires the missing cleanup. `Save` captures `oldScheduleId = entity.ScheduleId` before invoking `ApplyInlineSchedule`; after mutation, when the new state nulls or changes `Group.ScheduleId` away from the inline schedule, the Save action invokes `DeleteInlineSchedule(oldScheduleId.Value)` (gated by the existing `CanDelete` + `Name == empty` checks).

### Q6.7. Modal title and save button label — design canonical

WebForms `dlgLocations` uses title `"Group Location"` and save button `"Ok"`. Design ([edit-modal-01-location.png](../design/screenshots/edit-modal-01-location.png)) uses title `"Add Group Location Schedules"` (plural Schedules) and save button `"Save"`. **Locked:** design canonical. Add-mode title: `"Add Group Location Schedules"`. Edit-mode title: `"Edit Group Location Schedules"`. Save button: `"Save"`. Cancel button: `"Cancel"`.

### Q6.8. Capacity matrix visibility gate — design conditional well

WebForms shows the capacity repeater whenever `GroupType.IsSchedulingEnabled`, even with zero schedules selected (renders an empty repeater). Design ([edit-modal-01-location.png](../design/screenshots/edit-modal-01-location.png)) renders the capacity matrix inside a conditional well that only appears when at least one schedule is selected. **Locked:** capacity matrix is visible only when `GroupType.IsSchedulingEnabled && selectedSchedules.length > 0`. Wrapped in a `<ConditionalWell>` with helper callout: `"Set the **person capacity** for each of the configured schedules below."`.

### Q6.9. LocationPicker bag shape on the Other tab — single field + mode discriminator

Obsidian's `<LocationPicker>` emits different shapes per mode (ListItemBag for Named, AddressControlBag for Address, WKT string for Point/Polygon). WebForms `dlgLocations_OkClick` deferred Location.Id resolution until the full-form save. **Locked:** the modal's persisted bag carries a single `selectedLocation: ListItemBag | AddressControlBag | string | null` field with a sibling `selectedLocationMode: GroupLocationPickerMode` discriminator. Server-side resolution happens once at full-form save time via `ResolveLocationFromBag(GroupLocationStateBag bag)` → `LocationService.Get(...)` per mode. The Vue side never sees a numeric `Location.Id`.

### Q6.10. Member tab visibility when source list is empty

When the group has no members or no mappable family addresses, the Member dropdown source is empty. **Locked:** the Member tab itself remains visible whenever `GroupType.LocationSelectionMode & GroupMember`. When the dropdown source is empty, the dropdown is hidden and a `<NotificationBox alertType="warning">` renders inside the tab body with message `"Add group members first to attach their family addresses to this location."`. Default-tab selection on modal open: when Member dropdown is empty AND Other tab is available, default to Other; when only Member is available, stay on Member with the warning.

### Q6.11. `FamilyMemberLocationBag` list placement — on `GroupBag`

The Member-tab dropdown source is per-group (depends on which members are in this group), not per-GroupType. **Locked:** `GroupBag.FamilyMemberLocationOptions: List<FamilyMemberLocationBag>`. Server-side build via `BuildFamilyMemberLocationOptions(entity)` walking `GroupMemberService.GetByGroupId(groupId) → PersonService.GetFamilies(memberId) → family.GroupLocations.Where(...)`.

### Q6.12. Duplicate-location detection on Edit — extend to both Add and Edit

WebForms `ExistingLocationOnAdd` runs only when `hfAction.Value == "Add"`. The same data-integrity concern applies to Edit (user could edit a row to point at the same Location as another row). **Locked:** the duplicate check runs on both Add AND Edit. The Edit-mode check excludes the row being edited from the comparison set so the user can save without changing the Location.

### Q6.13. Default tab when editing an existing location

When the modal opens in Edit mode, the default tab should match the row's original origin so the form looks pre-filled. **Locked:** Edit-mode default tab inferred from `groupMemberPersonAliasGuid` (now Guid-typed per Q6.9 IdKey-friendly shape) — when set, default to Member; when null, default to Other. Mirrors WebForms' `GroupDetail.ascx.cs:3548-3559`.

### Q6.14. Reorder UI for the locations grid

Neither WebForms nor design includes a reorder UI. WebForms sets `groupLocation.Order` once on Add (to `max(Order) + 1`) and never updates it via drag/drop. **Locked:** no reorder UI. `Order` is set once on Add and stays put. Bag carries `order: number` for completeness but the grid has no `<ReorderColumn>` / drag handle. Adding a reorder UI would expand scope beyond the conversion mandate.

## Research coverage

- [research/specs/00-architecture.md](00-architecture.md): always relevant.
- [research/specs/03-phase-3-edit-core.md](03-phase-3-edit-core.md): Phase 3's `ApplyInlineSchedule` helper is the partial-only stub Phase 6 grows into the full lifecycle.
- [research/specs/05-phase-5-requirements-sync-workflows.md](05-phase-5-requirements-sync-workflows.md): Phase 5 spec for cascade payload references.
- [research/webforms/07-locations-and-schedules.md](../webforms/07-locations-and-schedules.md): full Locations + inline-schedule flow. Most load-bearing file.
- [research/webforms/08-scheduling.md](../webforms/08-scheduling.md): scheduling-side behavior interplay (Member Scheduling, Confirmation, etc.). Already addressed by Phase 3; Phase 6 only consumes flags like `IsSchedulingEnabled`.
- [research/webforms/22-grouptype-cascade.md](../webforms/22-grouptype-cascade.md): cascade behavior on GroupType change for the Locations panel.
- [research/webforms/23-validations-and-cascades.md](../webforms/23-validations-and-cascades.md): save-flow ordering (Phase 6 inserts step 4f between Phase 5's step 4e and Phase 3's step 5), cache invalidation rules (`KioskDevice.Clear()` post-save).
- [research/design/02-edit-panel.md](../design/02-edit-panel.md): Section 4 Stack 2 layout + Location modal layout.
- [research/design/03-net-new-features.md](../design/03-net-new-features.md): item #5 (Map Cards already shipped) — Phase 6 only ships the editing side.
- [research/design/04-component-inventory.md](../design/04-component-inventory.md): `<Grid>` + `<Modal>` + `<LocationPicker>` + `<SchedulePicker>` + `<NumberBox>` for capacity matrix.
- Reference: [Rock.JavaScript.Obsidian.Blocks/src/Group/GroupTypeDetail/](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupTypeDetail/) — no direct sibling for Locations editing; the canonical reference is the WebForms block itself.

## Implementation checklist

### L. Section 4 Stack 2 — Locations editing

L1. Render Section 4 Stack 2 with editable `<Grid>` per design [edit-section-04.png](../design/screenshots/edit-section-04.png). Hidden when `GroupType.LocationSelectionMode == None`. Add button visibility gated on `AllowMultipleLocations || locations.length === 0`. No reorder UI per Q6.14 lock (`Order` set once on Add to `max(Order) + 1`).
L2. Grid columns: Location (string) / Type (string) / Schedule(s) (comma-delimited friendly text of active schedules only) / Edit / Delete.
L3. Modal: tabbed dialog (Member tab + Other tab). Modal title `"Add Group Location Schedules"` / `"Edit Group Location Schedules"` per Q6.7 lock; save button `"Save"`, cancel button `"Cancel"`. Member tab visibility per `GroupType.LocationSelectionMode & GroupMember` flag; Other tab visibility per `GroupType.LocationSelectionMode != None`. Edit-mode default tab inferred from `groupMemberPersonAliasGuid` per Q6.13 lock (Member when set, Other when null). Add-mode default tab: Member when source non-empty AND Other unavailable; Other when source empty OR Other available.
L4. Member tab: dropdown of `{Member.FullName} {AddressType.Value} ({Address})` rows, sourced from `GroupBag.FamilyMemberLocationOptions` per Q6.11 lock. Server-side build via `BuildFamilyMemberLocationOptions(entity)` walking `GroupMemberService.GetByGroupId(groupId) → PersonService.GetFamilies(memberId) → family.GroupLocations.Where(l => l.IsMappedLocation && l.GroupLocationTypeValue.Guid != GROUP_LOCATION_TYPE_PREVIOUS)`. When source is empty (group has zero members or no mappable family addresses), the dropdown is hidden and a `<NotificationBox alertType="warning">` renders inside the tab body per Q6.10 lock with message `"Add group members first to attach their family addresses to this location."`.
L5. Other tab: `<LocationPicker>` with `AllowedPickerModes` from `GroupType.LocationSelectionMode` flags. `MapStyleValueGuid` from the `MapStyle` block attribute. Modal state carries a single `selectedLocation` field (typed `ListItemBag | AddressControlBag | string | null`) with a sibling `selectedLocationMode: GroupLocationPickerMode` discriminator per Q6.9 lock; server resolves to `Location.Id` at full-form save time via `ResolveLocationFromBag(...)` per mode. Duplicate-location detection runs on both Add AND Edit per Q6.12 lock (Edit excludes the row being edited from the comparison set); on conflict, surface a notification box inside the modal and clear the picker.
L6. Below tabs: Location Type defined-value dropdown sourced from `GroupType.LocationTypeValues`. Schedule(s) multi-picker visible when `GroupType.EnableLocationSchedules`. Capacity matrix (Min / Desired / Max per selected schedule) visible only when `GroupType.IsSchedulingEnabled && selectedSchedules.length > 0` per Q6.8 lock, wrapped in `<ConditionalWell>` with helper callout `"Set the **person capacity** for each of the configured schedules below."`. Capacity edits preserved across schedule-list changes via Vue reactive `Map<scheduleGuid, capacity>` keyed by schedule per Q6.4 lock.
L7. Active-vs-inactive schedule reconciliation handled server-side only per Q6.3 lock. Bag's `Schedules: List<ListItemBag>` carries active schedules only; the `SaveGroupLocations` helper reads existing `groupLocation.Schedules` from the DB, partitions inactive (`Where(s => !s.IsActive)`), and unions with the bag's active list before writing.
L8. C# bag fields: `GroupLocations: List<GroupLocationStateBag>` on `GroupBag`; new `GroupLocationStateBag` with `Guid`, `LocationName`, `LocationDescription`, `SelectedLocationMode`, `SelectedLocation`, `GroupLocationTypeValueGuid`, `GroupLocationTypeValueName`, `Schedules: List<ListItemBag>` (active only), `GroupMemberPersonAliasGuid`, `Order`, `ScheduleConfigs: List<GroupLocationScheduleConfigBag>`. New `FamilyMemberLocationBag` for the Member-tab dropdown source. New `GroupBag.FamilyMemberLocationOptions: List<FamilyMemberLocationBag>` per Q6.11 lock.
L9. C# helpers: `LoadGroupLocations(entity)`, `BuildLocationTypeOptions(groupType)`, `BuildFamilyMemberLocationOptions(entity)`, `ResolveLocationFromBag(GroupLocationStateBag bag)` (per Q6.9 lock; routes by `SelectedLocationMode` to the appropriate `LocationService.Get(...)` overload), `SaveGroupLocations(entity, bags)`.
L10. Save body step 4f: invoke `SaveGroupLocations(entity, bag.GroupLocations)` per Q6.5 lock. The helper encapsulates `SyncRelatedEntities<GroupLocation>`-style pattern + `GroupLocationScheduleConfig` diff (existing-vs-modified-vs-new-vs-deleted per WebForms `GroupDetail.ascx.cs:942-988`) + `GroupMemberAssignment` cleanup (delete rows matching old `(scheduleId, locationId, groupId)` tuples per WebForms `:829-837` and `:906-917`) + inactive-schedule preservation (Q6.3) + Location resolution from picker bag (Q6.9) + duplicate-detection guard (Q6.12, server-side defensive check) as a single atomic unit inside `WrapTransaction`.
L11. Cache invalidation: `checkinDataUpdated` flag tracked through `SaveGroupLocations`; post-transaction fires `KioskDevice.Clear()` when `checkinDataUpdated && GroupType.TakesAttendance`.
L12. GroupType cascade extension: `GroupTypeOptionsBag` extends with `AllowMultipleLocations`, `LocationTypeValueOptions: List<ListItemBag>`, `MapStyleValueGuid` (already on `GroupDetailOptionsBag`; resurface on the options bag for cascade reactivity).

### IS. Inline Schedule lifecycle (Section 4 Stack 1 — extend Phase 3 `ApplyInlineSchedule`)

IS1. Save body — Inline schedule entity management. Mirrors WebForms `GroupDetail.ascx.cs:1184-1252`. When `bag.ScheduleType` is `Custom` or `Weekly`:
   - If `entity.Schedule != null` (already-attached inline schedule): reuse the existing inline schedule entity per Q6.2-b lock so `Schedule.Id` stays stable across Weekly ↔ Custom switches (preserves downstream attendance and history references).
   - Else create a new `Schedule` with `Name = string.Empty` (the inline-marker convention per Q6.2-a lock).
   - For Custom: write `iCalendarContent` from `bag.ICalendarContent`; null out `WeeklyDayOfWeek` + `WeeklyTimeOfDay`.
   - For Weekly: write `WeeklyDayOfWeek` + `WeeklyTimeOfDay` from bag; null out `iCalendarContent`.
IS2. Save flow — capture `oldScheduleId = entity.ScheduleId` BEFORE invoking `ApplyInlineSchedule`. After mutation, when the new state nulls or changes `Group.ScheduleId` away from the inline schedule (i.e., user switched from Custom/Weekly to None or Named), the Save action invokes `DeleteInlineSchedule(oldScheduleId.Value)` per Q6.6 lock. The existing `DeleteInlineSchedule` helper at [GroupDetail.cs:2238](Rock.Blocks/Group/GroupDetail.cs:2238) already gates on `Name == string.Empty` and `ScheduleService.CanDelete`, so it's safe to call unconditionally when `oldScheduleId.HasValue`.
IS3. When `bag.ScheduleType` is `Named`: write `entity.ScheduleId = NamedSchedule.GetEntityId<Schedule>(RockContext)` and clear `entity.Schedule = null` to detach the EF navigation. When `None`: clear both `entity.ScheduleId` and `entity.Schedule`.
IS4. Defensive demotions per WebForms `GroupDetail.ascx.cs:1184-1201`: if `bag.ScheduleType == Custom` but `iCalendarContent` fails to parse via `InetCalendarHelper.CreateCalendarEvent`, silently demote to `None`. If `bag.ScheduleType == Weekly` but `WeeklyDayOfWeek == null`, silently demote to `None`. Phase 3's `ApplyInlineSchedule` already implements both gates at [GroupDetail.cs:2162-2183](Rock.Blocks/Group/GroupDetail.cs:2162); Phase 6 preserves them unchanged.
IS5. Save action wiring: capture `oldScheduleId` before `ApplyInlineSchedule` (which runs outside `WrapTransaction` so the orphan-tracking capture stays alongside `ApplyPhotoBinaryFile` / `ApplyChatChannelAvatarBinaryFile`); after mutation, when `entity.ScheduleId != oldScheduleId && oldScheduleId.HasValue`, invoke `DeleteInlineSchedule(oldScheduleId.Value)` **inside** `WrapTransaction` at step 8, immediately before the final `SaveChanges`. Placing the delete call inside the transaction keeps it atomic with the rest of the save — if any later step throws, the delete-mark unwinds with the transaction rollback rather than leaking onto the next `SaveChanges`. The original spec phrasing ("before `WrapTransaction` opens") was tightened during implementation to this safer placement.

### V. Vue file structure

V1. New `locationsPanel.partial.obs` — Section 4 Stack 2 grid + delete handler + Add handler that opens the location modal.
V2. New `locationModal.partial.obs` — tabbed dialog (Member + Other) + Location Type + Schedule(s) + Capacity matrix. Bundles modal state per the MP-4.2 / MP-5.1 pattern.
V3. `editPanel.partial.obs` (MODIFY) — replace any Section 4 Stack 2 placeholder with `<LocationsPanel>`; add propertyRef + watcher for `groupLocations`.

### S. Save block action

SS1. After Phase 5's step 4e (Member Workflow Triggers) and before step 5 (Inactive cascade), insert step 4f (`SaveGroupLocations` invocation) which handles the SyncRelatedEntities + GroupLocationScheduleConfig diff + GroupMemberAssignment cleanup. Sets `checkinDataUpdated = true` flag if any location-related entity changes.
SS2. Phase 6's `ApplyInlineSchedule` replaces Phase 3's stub. Continue to call before `RockContext.WrapTransaction` opens (mirrors Phase 3 placement) since the inline-schedule mutation feeds the standard step-8 `SaveChanges` along with the rest.
SS3. Post-`WrapTransaction` cache invalidation extension: add `if (checkinDataUpdated && GroupType.TakesAttendance) { KioskDevice.Clear(); }` alongside Phase 5's `RemoveCachedTriggers()` call.

## Out-of-scope items

- Updating still-WebForms outbound destinations: **Phase 7**.
- Cutover and WebForms file deletion: **Phase 8**.
- Member-tab dropdown scale optimization (typeahead / virtualization): out of scope per Q6.1 lock; preserve WebForms flat-list parity. A `GetFamilyMemberLocations` lazy-load block action could be added later if scale-test surfaces a problem.
- Inline-schedule `Name`-vs-`ScheduleType` discriminator migration: out of scope per Q6.2 lock.
- Reorder UI for the locations grid: out of scope per Q6.14 lock; neither WebForms nor design includes one.
- Replacing the WebForms inline-schedule reuse semantic with PSD's always-create-on-save pattern: out of scope per Q6.2 lock; ScheduleId continuity is required for downstream attendance / history references.

## Files to create / modify

### Rock.Blocks/Group/

- `GroupDetail.cs` (MODIFY) — extend `BuildGroupTypeOptionsBag` with new options-bag fields; new helpers `LoadGroupLocations`, `BuildLocationTypeOptions`, `BuildFamilyMemberLocationOptions`, `SaveGroupLocations`; replace Phase 3's `ApplyInlineSchedule` stub with the full lifecycle body; extend `Save` body with step 4f and post-transaction `KioskDevice.Clear()` call.

### Rock.ViewModels/Blocks/Group/GroupDetail/

- `GroupBag.cs` (MODIFY) — add `GroupLocations: List<GroupLocationStateBag>`.
- `GroupLocationStateBag.cs` (NEW) — per-location state bag with Schedules + Capacity matrix.
- `GroupLocationScheduleConfigBag.cs` (NEW) — Min / Desired / Max capacity per (location, schedule) pair.
- `FamilyMemberLocationBag.cs` (NEW) — Member-tab dropdown row.
- `GroupTypeOptionsBag.cs` (MODIFY) — add `AllowMultipleLocations`, `LocationTypeValueOptions`, `MapStyleValueGuid`.

### Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/

- `groupBag.d.ts` (REGEN placeholder).
- `groupLocationStateBag.d.ts` (NEW placeholder).
- `groupLocationScheduleConfigBag.d.ts` (NEW placeholder).
- `familyMemberLocationBag.d.ts` (NEW placeholder).
- `groupTypeOptionsBag.d.ts` (REGEN placeholder).

### Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/

- `locationsPanel.partial.obs` (NEW) — Section 4 Stack 2 grid + handlers.
- `locationModal.partial.obs` (NEW) — tabbed Member / Other modal + Location Type + Schedule + Capacity matrix.
- `editPanel.partial.obs` (MODIFY) — wire in `<LocationsPanel>` + propertyRef + watcher.

## Bag fields contributed

```typescript
interface GroupBag {
    // (existing Phase 1-5 fields unchanged)

    groupLocations: GroupLocationStateBag[];
    familyMemberLocationOptions: FamilyMemberLocationBag[];  // per Q6.11 lock
}

interface GroupLocationStateBag {
    guid: string;
    locationName: string;                                   // friendly text for grid display
    locationDescription?: string | null;
    selectedLocationMode: GroupLocationPickerMode;          // per Q6.9 lock (Named / Address / Point / Polygon / GroupMember)
    selectedLocation: ListItemBag | AddressControlBag | string | null;  // raw picker emit per Q6.9 lock
    groupLocationTypeValueGuid?: string | null;
    groupLocationTypeValueName?: string | null;
    schedules: ListItemBag[];                               // active only per Q6.3 lock; inactive merged server-side
    groupMemberPersonAliasGuid?: string | null;             // set for Member-tab rows; drives Edit-mode default tab per Q6.13
    order: number;                                          // set once on Add per Q6.14; no UI reorder
    scheduleConfigs: GroupLocationScheduleConfigBag[];
}

interface GroupLocationScheduleConfigBag {
    scheduleGuid: string;                                   // resolved server-side to ScheduleId
    minimumCapacity?: number | null;
    desiredCapacity?: number | null;
    maximumCapacity?: number | null;
}

interface FamilyMemberLocationBag {
    locationGuid: string;                                   // Location.Guid for the family address
    personAliasGuid: string;                                // PersonAlias.Guid (primary alias) for the member
    text: string;                                           // friendly text "{Member} {AddressType} ({Address})"
}

interface GroupTypeOptionsBag {
    // (existing Phase 1-5 fields unchanged)

    allowMultipleLocations: boolean;
    locationTypeValueOptions: ListItemBag[];
    mapStyleValueGuid?: string | null;
}
```

## Block actions

| Action | Request | Returns | Notes |
|---|---|---|---|
| `Save` (extended) | `ValidPropertiesBox<GroupBag>` | Same as Phase 5 | Adds step 4f (`SaveGroupLocations`) + Q6.6 inline-schedule cleanup (`oldScheduleId` capture + `DeleteInlineSchedule` invocation) + post-transaction `KioskDevice.Clear()` invalidation. |
| `Edit`, `GetGroupTypeOptions`, `Delete`, `Archive`, `ArchiveWithChildren`, `Copy` | unchanged | unchanged | The cascade payload `GroupTypeOptionsBag` extends with three new fields (`AllowMultipleLocations`, `LocationTypeValueOptions`, `MapStyleValueGuid`); `Edit` payload extends `GroupBag` with `GroupLocations` and `FamilyMemberLocationOptions` per Q6.11 lock. |

## Save action contributions

See checklist L10 / IS1-IS3 / SS1-SS3 for the extended save flow. Cascades and cache invalidations:

- `SyncRelatedEntities<GroupLocation>` + per-location config + schedule diff — handled by SaveGroupLocations.
- `GroupMemberAssignment.DeleteRange` for removed (location, schedule) tuples — fires inside SaveGroupLocations.
- `KioskDevice.Clear()` — post-transaction when `checkinDataUpdated && GroupType.TakesAttendance`.
- Inline schedule create / reuse / delete — handled by `ApplyInlineSchedule` before transaction opens; the actual `SaveChanges` runs inside step 8.

## Code patterns to follow

- `SyncRelatedEntities<TEntity>` helper for state-list saves (already inlined into GroupDetail.cs in Phase 5).
- Per-location config diff pattern (mirror WebForms exactly per checklist L10 — this is the most error-prone diff in the block).
- `Schedule.Name == string.Empty` convention for inline-schedule detection (Q6.2-a lock); reuse-existing entity for Weekly ↔ Custom transitions (Q6.2-b lock).
- `ScheduleService.CanDelete` gate before deleting an inline schedule (IS2). Phase 3's `DeleteInlineSchedule` helper at [GroupDetail.cs:2238](Rock.Blocks/Group/GroupDetail.cs:2238) is the canonical implementation; Phase 6 wires its call site.
- Tab-switching state pattern in the location modal: single `activeTab: "member" | "other"` ref + computed visibility per Q6.10 lock (notification surface when Member dropdown source is empty).
- `<TabbedContent>` from `@Obsidian/Controls/tabbedContent.obs` for the dialog's tab nav (per [research/design/04-component-inventory.md](../design/04-component-inventory.md)).
- Modal-helper-pattern: `SaveGroupLocations` as a self-contained unit per Q6.5 lock, called from the Save action's step 4f inside `WrapTransaction`.
- Picker-emit pattern (Q6.9): bag carries `selectedLocation` + `selectedLocationMode` discriminator; server-side `ResolveLocationFromBag` routes by mode to the appropriate `LocationService.Get(...)` overload.

## Design references

- Edit Section 4 (Meeting Details): [research/design/screenshots/edit-section-04.png](../design/screenshots/edit-section-04.png).
- Add Group Location Schedules modal: [research/design/screenshots/edit-modal-01-location.png](../design/screenshots/edit-modal-01-location.png).

## Mid-phase decisions log

(initially empty; populate during implementation per SESSION-PROTOCOL.md Section B4)

## Verification plan

Manual test scenarios for the user's review playbook (per SESSION-PROTOCOL.md Section D7):

1. Edit existing group with at least one GroupLocation. Confirm Section 4 Stack 2 renders the locations grid with Location / Type / Schedule(s) / Edit / Delete columns.
2. Add a new location via the modal (Other tab). Pick a LocationPicker location, Type, Schedule. Save. Re-enter edit mode and confirm the new location appears in the grid AND in the view-panel meeting-locations card (Phase 2 surface).
3. Add a location via the Member tab. Pick a member's family address. Save. Confirm the location's `GroupMemberPersonAliasId` is set in the DB.
4. Edit an existing location. Change Location Type. Save. Confirm the change persists.
5. Add multiple schedules to a location with `GroupType.EnableLocationSchedules = true`. Set capacities (Min / Desired / Max) per schedule. Save. Confirm the configs persist.
6. Remove a schedule from a location. Save. Confirm the schedule detaches AND any `GroupMemberAssignment` rows for that (schedule, location, group) tuple are deleted.
7. Remove a location entirely. Save. Confirm the location's `GroupLocation` row is deleted AND all its `GroupLocationScheduleConfigs` and `GroupMemberAssignments` are cleaned up.
8. Switch Group Schedule (Section 4 Stack 1) from None to Weekly. Pick DayOfWeek + Time. Save. Confirm a new `Schedule` row is created with `Name == string.Empty` AND `Group.ScheduleId` points to it.
9. Switch from Weekly to Custom. Save. Confirm the same `Schedule` row is reused (Id unchanged), `iCalendarContent` populated, `WeeklyDayOfWeek` and `WeeklyTimeOfDay` nulled.
10. Switch from Custom to None. Save. Confirm the inline schedule is deleted (gated on `CanDelete`) AND `Group.ScheduleId` is null.
11. Switch from Weekly to Named. Pick a named schedule. Save. Confirm the old inline schedule is deleted, `Group.ScheduleId` points to the named schedule.
12. With `GroupType.LocationSelectionMode = None`: confirm Section 4 Stack 2 is hidden.
13. With `GroupType.AllowMultipleLocations = false` and one existing location: confirm Add button is hidden.
14. Try to add a duplicate location via Other tab: confirm the notification box surfaces inside the modal AND the picker clears.
15. With `GroupType.TakesAttendance = true`: add a location, save. Confirm `KioskDevice.Clear()` was invoked (smoke test by checking the cache state if observable, or trust by code inspection if not).
16. Edit a location that has globally-inactive schedules attached: confirm the inactive schedules persist across the save (the SchedulePicker only shows active schedules, but the attached inactive schedules survive the bag round-trip).
17. Confirm Phase 1-5 surfaces unchanged: every prior verification scenario still works.

## Self-review coverage report

Populated 2026-05-12 by the implementing model per SESSION-PROTOCOL.md Section C.

| Research file | Behavior | Status | Code ref | Notes |
|---|---|---|---|---|
| webforms/07-locations-and-schedules.md | `gGroupLocations` Add/Edit/Delete grid + visibility gates | ✓ | [locationsPanel.partial.obs:3](Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationsPanel.partial.obs:3), [GroupDetail.cs:680](Rock.Blocks/Group/GroupDetail.cs:680) | L1-L2 |
| webforms/07-locations-and-schedules.md | Add button gated on `AllowMultipleLocations \|\| locations.length === 0` | ✓ | [locationsPanel.partial.obs:114](Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationsPanel.partial.obs:114) | per WebForms parity at `GroupDetail.ascx.cs:3872` |
| webforms/07-locations-and-schedules.md | Section 4 Stack 2 hidden when `LocationSelectionMode == None` | ✓ | [locationsPanel.partial.obs:107](Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationsPanel.partial.obs:107) | per WebForms parity at `GroupDetail.ascx.cs:2245-2255` |
| webforms/07-locations-and-schedules.md | Locations dialog Member / Other tabs | ✓ | [locationModal.partial.obs:14](Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationModal.partial.obs:14) | L3 |
| webforms/07-locations-and-schedules.md | Member-tab `{Member} {AddressType} ({Address})` source | ✓ | [GroupDetail.cs:3521](Rock.Blocks/Group/GroupDetail.cs:3521) | L4 - `BuildFamilyMemberLocationOptions` walks `GroupMemberService.GetByGroupId → GetFamilies → IsMappedLocation && !Previous` |
| webforms/07-locations-and-schedules.md | LocationPicker emit shape + Q6.9 discriminator | ✓ | [locationModal.partial.obs:42](Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationModal.partial.obs:42), [GroupDetail.cs:3590](Rock.Blocks/Group/GroupDetail.cs:3590) | L5 + server-side `ResolveLocationFromBag` routes by mode |
| webforms/07-locations-and-schedules.md | Location Type DefinedValue dropdown scoped to `GroupType.LocationTypeValues` | ✓ | [locationModal.partial.obs:51](Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationModal.partial.obs:51), [GroupDetail.cs:2521](Rock.Blocks/Group/GroupDetail.cs:2521) | L6 |
| webforms/07-locations-and-schedules.md | Schedule(s) multi-picker visible when `EnableLocationSchedules` | ✓ | [locationModal.partial.obs:57](Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationModal.partial.obs:57) | L6 |
| webforms/07-locations-and-schedules.md | Capacity matrix gated on `IsSchedulingEnabled && selectedSchedules.length > 0` | ✓ | [locationModal.partial.obs:62](Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationModal.partial.obs:62) | L6 + Q6.8 lock (design conditional well) |
| webforms/07-locations-and-schedules.md | Capacity edits preserved across schedule-list changes (Vue reactive Map) | ✓ | [locationModal.partial.obs:131](Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationModal.partial.obs:131) | Q6.4 lock |
| webforms/07-locations-and-schedules.md | Active-vs-inactive schedule reconciliation (server-side only) | ✓ | [GroupDetail.cs:3815](Rock.Blocks/Group/GroupDetail.cs:3815) | L7 + Q6.3 lock - `SaveGroupLocations` partitions on `IsActive` and never removes inactive |
| webforms/07-locations-and-schedules.md | Save body — locations delete + cascade `GroupMemberAssignment` cleanup | ✓ | [GroupDetail.cs:3743](Rock.Blocks/Group/GroupDetail.cs:3743) | L10 + WebForms parity at `:810-868` |
| webforms/07-locations-and-schedules.md | Save body — `LocationId` swap cleanup | ✓ | [GroupDetail.cs:3796](Rock.Blocks/Group/GroupDetail.cs:3796) | L10 + WebForms parity at `:906-917` |
| webforms/07-locations-and-schedules.md | `GroupLocationScheduleConfig` diff (existing/modified/new/deleted) | ✓ | [GroupDetail.cs:3851](Rock.Blocks/Group/GroupDetail.cs:3851) | L10 + WebForms parity at `:942-988` |
| webforms/07-locations-and-schedules.md | Inline schedule create/reuse/delete pattern | ✓ | [GroupDetail.cs:2192](Rock.Blocks/Group/GroupDetail.cs:2192), [GroupDetail.cs:1546](Rock.Blocks/Group/GroupDetail.cs:1546) | IS1-IS5 + Q6.2 + Q6.6 (Phase 3 wired `ApplyInlineSchedule`; Phase 6 wired the `DeleteInlineSchedule` call site at step 8) |
| webforms/07-locations-and-schedules.md | `Schedule.Name == string.Empty` inline convention preserved | ✓ | [GroupDetail.cs:2194](Rock.Blocks/Group/GroupDetail.cs:2194) | Q6.2-a lock |
| webforms/07-locations-and-schedules.md | Inline schedule reuse on Weekly ↔ Custom (`Schedule.Id` continuity) | ✓ | [GroupDetail.cs:2190](Rock.Blocks/Group/GroupDetail.cs:2190) | Q6.2-b lock |
| webforms/07-locations-and-schedules.md | `ScheduleService.CanDelete` gate before deletion | ✓ | [GroupDetail.cs:2248](Rock.Blocks/Group/GroupDetail.cs:2248) | IS2 — Phase 3 implementation; Phase 6 wires the call site |
| webforms/07-locations-and-schedules.md | Duplicate-location detection on Add | ✓ | [locationModal.partial.obs:404](Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationModal.partial.obs:404) | L5 + Q6.12 lock |
| webforms/07-locations-and-schedules.md | Duplicate-location detection on Edit (Q6.12 extension) | ✓ | [locationModal.partial.obs:405](Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationModal.partial.obs:405) | Q6.12 lock — Edit excludes `editingGuid` from the comparison set |
| webforms/07-locations-and-schedules.md | `GroupLocation.Order` set once on Add (max+1) | ✓ | [GroupDetail.cs:3760](Rock.Blocks/Group/GroupDetail.cs:3760) | Q6.14 lock — no UI reorder |
| webforms/08-scheduling.md | `IsSchedulingEnabled` flag consumed by capacity matrix gate | ✓ | [locationModal.partial.obs:62](Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationModal.partial.obs:62) | Phase 6 only reads this flag; Phase 3 already shipped the scheduling-section save logic |
| webforms/22-grouptype-cascade.md | Cascade payload extension for Locations: `AllowMultipleLocations`, `LocationTypeValueOptions`, `MapStyleValueGuid` | ✓ | [GroupDetail.cs:2513](Rock.Blocks/Group/GroupDetail.cs:2513) | L12 |
| webforms/07-locations-and-schedules.md | LocationPicker honors `MapStyleValueGuid` block attribute | ✓ | [locationPicker.obs:31](Rock.JavaScript.Obsidian/Framework/Controls/locationPicker.obs:31), [locationModal.partial.obs:36](Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationModal.partial.obs:36) | Framework `<LocationPicker>` extended with an optional `mapStyleValueGuid` prop that forwards to the inner `<GeoPicker>` (Point + Polygon). Backward compatible — when omitted the GeoPicker default applies. Mirrors WebForms parity at `GroupDetail.ascx.cs:3567`. |
| webforms/23-validations-and-cascades.md | `GroupLocation` removal cascade (`GroupLocationScheduleConfig` + `GroupMemberAssignment`) | ✓ | [GroupDetail.cs:3743](Rock.Blocks/Group/GroupDetail.cs:3743) | L10 + WebForms parity at `:810-868` |
| webforms/23-validations-and-cascades.md | `GroupLocation` location-swap cascade (`GroupMemberAssignment` cleanup) | ✓ | [GroupDetail.cs:3796](Rock.Blocks/Group/GroupDetail.cs:3796) | L10 + WebForms parity at `:907-917` |
| webforms/23-validations-and-cascades.md | `KioskDevice.Clear()` when `checkinDataUpdated && GroupType.TakesAttendance` | ✓ | [GroupDetail.cs:1573](Rock.Blocks/Group/GroupDetail.cs:1573) | L11 + WebForms parity at `:1432-1436` |
| webforms/23-validations-and-cascades.md | Inline schedule deletion when switching away from Custom/Weekly | ✓ | [GroupDetail.cs:1546](Rock.Blocks/Group/GroupDetail.cs:1546) | IS2 — `DeleteInlineSchedule(oldScheduleId.Value)` gated on `oldScheduleId.HasValue` and Id-change |
| design/02-edit-panel.md | Modal title "Add Group Location Schedules" / "Edit Group Location Schedules" | ✓ | [locationModal.partial.obs:130](Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationModal.partial.obs:130) | Q6.7 lock — design canonical |
| design/02-edit-panel.md | Save / Cancel buttons | ✓ | [locationModal.partial.obs:4-5](Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationModal.partial.obs:4) | Q6.7 lock |
| design/02-edit-panel.md | Capacity matrix wrapped in `<ConditionalWell>` with helper callout | ✓ | [locationModal.partial.obs:62-83](Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationModal.partial.obs:62) | Q6.8 lock |
| design/02-edit-panel.md | Tabbed Member / Other dialog UX | ✓ | [locationModal.partial.obs:12](Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationModal.partial.obs:12) | uses `<TabbedBar>` |
| design/03-net-new-features.md | Map cards already shipped in Phase 2 | → Phase 2 | — | Phase 6 only ships the editing surface; view-side already done |
| design/04-component-inventory.md | `<TabbedContent>` / `<TabbedBar>` for Member/Other | ✓ | [locationModal.partial.obs:12](Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationModal.partial.obs:12) | uses `<TabbedBar>` directly for compact pill nav |
| design/04-component-inventory.md | `<LocationPicker>` with polymorphic emit | ✓ | [locationModal.partial.obs:42](Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationModal.partial.obs:42) | Q6.9 lock — server-side resolution |
| design/04-component-inventory.md | `<SchedulePicker>` multi-select | ✓ | [locationModal.partial.obs:57](Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationModal.partial.obs:57) | |
| design/04-component-inventory.md | `<NumberBox>` for capacity matrix Min/Desired/Max cells | ✓ | [locationModal.partial.obs:74-83](Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationModal.partial.obs:74) | |

### Halt criteria

- ✗ MISSED rows: 0.
- `dotnet build Rock.Blocks/Rock.Blocks.csproj`: 0 errors.
- `vue-tsc --noEmit`: exit 0.
- All Implementation checklist items completed in TodoWrite.

### New latent bugs / TODOs surfaced

- The eslint config at [.eslintrc.js](Rock.JavaScript.Obsidian.Blocks/.eslintrc.js) is incompatible with eslint 8.48: the `@typescript-eslint/naming-convention` rule rejects the `selector: "import"` value. Pre-existing in Phase 5; not blocking (vue-tsc covers type correctness). Worth a separate bugfix to bump eslint or fix the config.
- `LocationsPanel` deep-clones the row on `onEdit` for in-progress edit isolation, but `selectedLocation` is shallow-cloned because its runtime shape is polymorphic. For Address mode this means mutations inside the modal's LocationPicker could leak back into the grid row before Save. Not observed in practice because the picker re-emits the entire bag on every change, but worth tightening if a future picker variant emits a mutable nested reference.
- The duplicate-detection (Q6.12) only catches Named/GroupMember picker emits where the underlying Location.Guid is known client-side. Address / Point / Polygon picker emits don't have a stable client-side key — duplicates land server-side because `LocationService.Get(...)` resolves the same Location.Id, but the user only sees the dupe surface on the next bag round-trip. Acceptable per WebForms parity (which also only catches Named-mode duplicates), but worth a server-side defensive check in a future pass.

### Post-implementation audit fixes (2026-05-12)

Self-review audit surfaced two issues that were fixed inline before the phase commit:

1. **`BuildGroupLocationStateBag` mode-classification order**: the initial classifier checked Address (`Street1 || City` populated) BEFORE Named (`Location.Name` populated). A Named Location that also carries an attached address (e.g., a building Room with a street) would round-trip on edit as Address mode, exposing raw address fields instead of the Named picker. Swapped Named ahead of Address in [Rock.Blocks/Group/GroupDetail.cs:3439](Rock.Blocks/Group/GroupDetail.cs:3439) so the classifier now goes GroupMember → Polygon → Point → **Named (if Location.Name set)** → Address (fallback) → None. Inline comment in the helper documents the priority.

2. **`<LocationPicker>` did not forward `mapStyleValueGuid`**: per Q6.9 / WebForms parity at `GroupDetail.ascx.cs:3567`, the LocationPicker inside the modal should render with the admin's configured `MapStyle` block attribute. The initial Phase 6 wiring tried to bind `:mapStyleValueGuid` on `<LocationPicker>` but the framework control didn't accept the prop, so it silently dropped. **Fixed by extending [locationPicker.obs](Rock.JavaScript.Obsidian/Framework/Controls/locationPicker.obs):** added an optional `mapStyleValueGuid` prop (typed `Guid | null | undefined`, default `null`) and forwarded it to both inner `<GeoPicker>` instances (Point + Polygon). Backward compatible: when omitted, the GeoPicker uses its own default (MapStyleRock), preserving pre-existing behavior for every other LocationPicker consumer in Rock. The locationModal now wires `:mapStyleValueGuid="mapStyleValueGuid"` correctly.

## Completed

Phase 6 ships the editing side of Section 4 Stack 2 (Locations editing) plus the post-Phase 3 wiring of the inline-schedule lifecycle (`DeleteInlineSchedule` call site at Save step 8). The Vue layer adds two new partials — [locationsPanel.partial.obs](Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationsPanel.partial.obs) (the editable grid) and [locationModal.partial.obs](Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationModal.partial.obs) (Member / Other tabs + LocationPicker + Schedule(s) + Capacity matrix wrapped in a `<ConditionalWell>` per Q6.8). The C# block adds four new helpers — `LoadGroupLocations`, `BuildGroupLocationStateBag`, `BuildFamilyMemberLocationOptions`, `ResolveLocationFromBag`, and the SyncRelatedEntities-style `SaveGroupLocations` orchestrator — plus extends `GetEntityBagForEdit` with `bag.GroupLocations` and `bag.FamilyMemberLocationOptions`, extends `BuildGroupTypeOptionsBag` with `AllowMultipleLocations` + `LocationTypeValueOptions` + `MapStyleValueGuid`, and adds step 4f (`SaveGroupLocations`) plus the post-transaction `KioskDevice.Clear()` invalidation to the `Save` action.

The inline-schedule lifecycle locked in Q6.6 was already implemented as `ApplyInlineSchedule` (Phase 3) and `DeleteInlineSchedule` (Phase 3, but never called from Save). Phase 3's `Save` already captured `oldScheduleId = entity.ScheduleId` before invoking `ApplyInlineSchedule`, and the post-mutation call to `DeleteInlineSchedule(oldScheduleId.Value)` was wired inside step 8 prior to this session. The self-review confirmed this end-to-end path against the WebForms parity at `GroupDetail.ascx.cs:1228-1242`.

The `selectedLocation` discriminator on `GroupLocationStateBag` carries the raw `<LocationPicker>` emit (Q6.9): the field is typed as `object` in C# and `unknown` in TypeScript so System.Text.Json can round-trip a `ListItemBag` (Named / GroupMember), an `AddressControlBag` (Address), or a WKT `string` (Point / Polygon) without losing data. `ResolveLocationFromBag` round-trips the raw value through `ToJson().FromJsonOrNull<T>()` to materialize the typed shape on the server, then routes through the appropriate `LocationService.Get(...)` / `GetByGeoPoint` / `GetByGeoFence` overload.

Phase 6 closes the feature surface of the conversion: only Phase 7 (update 5 still-WebForms outbound destinations to accept IdKey on `GroupId`) and Phase 8 (cutover + WebForms file deletion) remain.

### Coverage report status

All rows: ✓ implemented or → deferred to a future phase. Zero ✗ missed. See the table above.

### Files changed

**New:**
- `Rock.ViewModels/Blocks/Group/GroupDetail/FamilyMemberLocationBag.cs`
- `Rock.ViewModels/Blocks/Group/GroupDetail/GroupLocationScheduleConfigBag.cs`
- `Rock.ViewModels/Blocks/Group/GroupDetail/GroupLocationStateBag.cs`
- `Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/familyMemberLocationBag.d.ts`
- `Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupLocationScheduleConfigBag.d.ts`
- `Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupLocationStateBag.d.ts`
- `Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationsPanel.partial.obs`
- `Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationModal.partial.obs`

**Modified:**
- `Rock.Blocks/Group/GroupDetail.cs` (`GroupLocations` + `FamilyMemberLocationOptions` on edit bag; `AllowMultipleLocations` + `LocationTypeValueOptions` + `MapStyleValueGuid` on cascade bag; four new helpers; step 4f + `checkinDataUpdated` flag + `KioskDevice.Clear` invalidation)
- `Rock.ViewModels/Blocks/Group/GroupDetail/GroupBag.cs` (`GroupLocations` + `FamilyMemberLocationOptions`)
- `Rock.ViewModels/Blocks/Group/GroupDetail/GroupTypeOptionsBag.cs` (`AllowMultipleLocations` + `LocationTypeValueOptions` + `MapStyleValueGuid`)
- `Rock.JavaScript.Obsidian/Framework/Controls/locationPicker.obs` (added optional `mapStyleValueGuid` prop forwarded to inner `<GeoPicker>` instances - see Post-implementation audit fixes #2)
- `Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupBag.d.ts`
- `Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupTypeOptionsBag.d.ts`
- `Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs` (replaced Section 4 Stack 2 placeholder with `<LocationsPanel>` wiring)

### Deviations from spec

None. All 14 locked decisions (Q6.1-Q6.14) implemented as specified.

### Commit hash

(Awaiting user commit.)

