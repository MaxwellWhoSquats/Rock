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
- **Location modal (Add / Edit)**: tabbed dialog with Member tab + Other tab. Member tab populates a dropdown of `{Member} {AddressType} ({Address})` rows from the group's members' families. Other tab uses a `<LocationPicker>` with mode flags from `GroupType.LocationSelectionMode`. Tab visibility per `GroupLocationPickerMode` flags. Below the tabs: Location Type defined-value dropdown (scoped to `GroupType.LocationTypeValues`), Schedule(s) multi-picker (visible when `GroupType.EnableLocationSchedules`), Capacity repeater (Min / Desired / Max per selected schedule, visible when `GroupType.IsSchedulingEnabled`).
- **Active-vs-inactive schedule reconciliation**: schedules that are attached to the location but are globally inactive remain attached across save. Mirrors WebForms `hfInactiveGroupLocationSchedules` at `GroupDetail.ascx.cs:3591-3594, 3810-3820`.
- **Duplicate-location detection on Add**: Other-tab adds run `ExistingLocationOnAdd` detection (compare by `Name + Guid`) and surface a notification inside the modal. Mirrors WebForms `locpGroupLocation_SelectLocation` at `GroupDetail.ascx.cs:3907-3919`.
- **Save body — Locations (step 4f)**: replaces the deleted GroupLocations + adds new + updates existing in state, reconciles `GroupLocationScheduleConfigs` (existing-vs-modified-vs-new-vs-deleted via the WebForms diff at `GroupDetail.ascx.cs:942-988`), cleans up `GroupMemberAssignment` rows for removed location/schedule combos (at `GroupDetail.ascx.cs:829-837, 906-917`), and sets `checkinDataUpdated = true` for the post-transaction `KioskDevice.Clear()` invalidation.
- **Save body — Inline schedule lifecycle**: extends Phase 3's `ApplyInlineSchedule` to handle the full create/reuse/delete pattern at `GroupDetail.ascx.cs:1184-1252`. Inline schedule has `Name == string.Empty` convention; created when switching to Weekly / Custom from None / Named, reused when switching between Weekly / Custom, deleted (gated on `ScheduleService.CanDelete`) when switching away from Weekly / Custom.
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

## Open questions for spec lock

Each has a default recommendation; the user confirms or overrides during the spec lock pass.

### Q6.1. Member-tab dropdown shape — full friendly text vs. structured row?

WebForms emits `{Member.FullName} {AddressType.Value} ({Address})` as a flat ListItem. With 100+ members and 2-3 addresses each, the dropdown can hit 300+ rows. **Default recommendation:** preserve WebForms parity (flat dropdown with full friendly text); revisit only if scale-test surfaces a UX problem. Alternative: 2-stage picker (Member → Address) — defer to a follow-up bug if requested.

### Q6.2. Inline-schedule `Name == string.Empty` convention — preserve or migrate to `ScheduleType` discriminator?

WebForms relies on `Schedule.Name == string.Empty` to detect inline schedules. The data model has a `ScheduleType` enum that could be used as a discriminator instead. **Default recommendation:** preserve the WebForms convention. Migrating would require a data migration to back-fill `ScheduleType` on every existing inline schedule and is out-of-scope for the conversion (it's a refactor of the entity model). Phase 6 reads + writes the convention as-is.

### Q6.3. Active-vs-inactive schedule reconciliation surface in the bag — separate field or embed in Schedules?

WebForms uses a `hfInactiveGroupLocationSchedules` hidden field carrying comma-delimited inactive schedule Ids. The Obsidian bag could either (a) surface `inactiveScheduleIds: number[]` as a separate field per GroupLocation bag, or (b) embed the inactive flag on each schedule in the `Schedules` list. **Default recommendation:** option (a) — separate `inactiveSchedules: ListItemBag[]` field per `GroupLocationStateBag`, mirroring WebForms' separation of active (user-controlled via picker) vs inactive (preserved across save).

### Q6.4. Capacity repeater rebuild on schedule change — preserve in-progress edits?

WebForms' `spSchedules_SelectItem` handler at `GroupDetail.ascx.cs:3294-3348` tries to preserve in-progress capacity edits when the user changes the schedule list. The mechanism is fragile (re-binding the repeater while reading current values). **Default recommendation:** preserve the WebForms semantic — when a schedule is added to / removed from the schedule list, persist the existing capacity rows for unchanged schedules and reset only the changed rows. Vue's reactive model makes this easier than WebForms' repeater rebind.

### Q6.5. `GroupMemberAssignment` cascade cleanup — block action vs Save body?

WebForms handles `GroupMemberAssignment` cleanup inside `btnSave_Click` at `GroupDetail.ascx.cs:829-837, 906-917`. **Default recommendation:** keep the cleanup inside the `Save` block action's `WrapTransaction` body as a new step 4f-cleanup phase, immediately before the standard `SaveChanges`. Co-locates the cleanup with the location writes so it's atomic.

### Q6.6. Group Schedule sub-fields (Section 4 Stack 1) — already wired in Phase 3?

Phase 3 wired the radio + DayOfWeek + Time + ScheduleBuilder + SchedulePicker. Phase 6 only needs to extend `ApplyInlineSchedule` to do the full lifecycle. **Default recommendation:** confirm at spec lock that the radio + sub-fields are in place; Phase 6 owns only the persistence-side semantics (create / reuse / delete inline schedule + clear `Group.ScheduleId` when switching away).

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

L1. Render Section 4 Stack 2 with editable `<Grid>` per design [edit-section-04.png](../design/screenshots/edit-section-04.png). Hidden when `GroupType.LocationSelectionMode == None`. Add button visibility gated on `AllowMultipleLocations || locations.length === 0`.
L2. Grid columns: Location (string) / Type (string) / Schedule(s) (comma-delimited friendly text) / Edit / Delete.
L3. Modal: tabbed dialog (Member tab + Other tab). Tab visibility per `GroupType.LocationSelectionMode & GroupMember` and `GroupType.LocationSelectionMode != None`. Tab default per WebForms: Member if available + has members; otherwise Other.
L4. Member tab: dropdown of `{Member.FullName} {AddressType.Value} ({Address})` rows. Source from `GroupMemberService.GetByGroupId(groupId) → PersonService.GetFamilies(memberId) → family.GroupLocations.Where(l => l.IsMappedLocation && l.GroupLocationTypeValue.Guid != GROUP_LOCATION_TYPE_PREVIOUS)`. Value format `{LocationId}|{PersonId}`. Resolves to `GroupLocation.GroupMemberPersonAliasId` via `PersonAliasService.GetPrimaryAliasId(personId)` server-side.
L5. Other tab: `<LocationPicker>` with `AllowedPickerModes` from `GroupType.LocationSelectionMode` flags. `MapStyleValueGuid` from the `MapStyle` block attribute. On select, run duplicate-add detection (compare by `Name + Guid` against current locations); surface a notification box inside the modal on conflict.
L6. Below tabs: Location Type defined-value dropdown sourced from `GroupType.LocationTypeValues`. Schedule(s) multi-picker visible when `GroupType.EnableLocationSchedules`. Capacity repeater (Min / Desired / Max per selected schedule) visible when `GroupType.IsSchedulingEnabled`.
L7. Active-vs-inactive schedule reconciliation per Q6.3 lock — separate `inactiveSchedules` field per GroupLocation bag, populated on load + preserved through save.
L8. C# bag fields: `GroupLocations: List<GroupLocationStateBag>` on `GroupBag`; new `GroupLocationStateBag` with `Guid`, `LocationId`, `LocationName`, `LocationDescription`, `GroupLocationTypeValueId`, `Schedules: List<ListItemBag>`, `InactiveSchedules: List<ListItemBag>`, `GroupMemberPersonAliasId`, `Order`, `ScheduleConfigs: List<GroupLocationScheduleConfigBag>`.
L9. C# helpers: `LoadGroupLocations(entity)`, `BuildLocationTypeOptions(groupType)`, `BuildFamilyMemberLocationOptions(entity)`, `SaveGroupLocations(entity, bags)`.
L10. Save body step 4f: `SyncRelatedEntities<GroupLocation>` extended with `GroupLocationScheduleConfig` diff logic (existing-vs-modified-vs-new-vs-deleted per WebForms `GroupDetail.ascx.cs:942-988`) and `GroupMemberAssignment` cleanup (delete rows matching old (scheduleId, locationId, groupId) tuples).
L11. Cache invalidation: `checkinDataUpdated` flag tracked through SaveGroupLocations; post-transaction fires `KioskDevice.Clear()` when `checkinDataUpdated && GroupType.TakesAttendance`.
L12. GroupType cascade extension: `GroupTypeOptionsBag` extends with `AllowMultipleLocations`, `LocationTypeValueOptions: List<ListItemBag>`, `MapStyleValueGuid` (already on `GroupDetailOptionsBag`; resurface on the options bag for cascade reactivity).

### IS. Inline Schedule lifecycle (Section 4 Stack 1 — extend Phase 3 `ApplyInlineSchedule`)

IS1. Save body — Inline schedule entity management. Mirrors WebForms `GroupDetail.ascx.cs:1184-1252`. When `bag.ScheduleType` is `Custom` or `Weekly`:
   - If `oldScheduleId.HasValue && entity.Schedule != null`: reuse the existing inline schedule entity (preserves `Schedule.Id` across switches between Custom and Weekly).
   - Else create a new `Schedule` with `Name = string.Empty` (the inline-marker convention per Q6.2 lock).
   - For Custom: write `iCalendarContent` from `bag.ICalendarContent`; null out `WeeklyDayOfWeek` + `WeeklyTimeOfDay`.
   - For Weekly: write `WeeklyDayOfWeek` + `WeeklyTimeOfDay` from bag; null out `iCalendarContent`.
IS2. When `bag.ScheduleType` is `None` or `Named` AND `oldScheduleId.HasValue`:
   - Look up the old `Schedule` via `ScheduleService.Get(oldScheduleId)`.
   - If `schedule.Name == string.Empty` (inline) AND `ScheduleService.CanDelete(schedule, out _)` returns true: `ScheduleService.Delete(schedule)`.
   - Otherwise leave the schedule entity in place (Group.ScheduleId clears, but the schedule entity persists if it has other consumers).
IS3. When `bag.ScheduleType` is `Named`: write `entity.ScheduleId = NamedSchedule.GetEntityId<Schedule>(RockContext)`. When `None`: clear `entity.ScheduleId = null`.
IS4. Defensive demotions per WebForms `GroupDetail.ascx.cs:1184-1201`: if `bag.ScheduleType == Custom` but `iCalendarContent` fails to parse via `InetCalendarHelper.CreateCalendarEvent`, silently demote to `None`. If `bag.ScheduleType == Weekly` but `WeeklyDayOfWeek == null`, silently demote to `None`.
IS5. Phase 3 dead-return removal: Phase 3's `ApplyInlineSchedule` had a placeholder return signature with a dead path. Phase 6 replaces the body with the full lifecycle so the return is meaningful (returns `int? oldScheduleId` to surface for the post-save schedule-delete check).

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
- Member-tab dropdown scale optimization (typeahead / virtualization): out of scope; preserve WebForms parity per Q6.1 default.
- Inline-schedule `Name`-vs-`ScheduleType` discriminator migration: out of scope per Q6.2 default.

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
}

interface GroupLocationStateBag {
    guid: string;
    locationId: number;
    locationName: string;
    locationDescription?: string | null;
    groupLocationTypeValueId?: number | null;
    groupLocationTypeValueName?: string | null;
    schedules: ListItemBag[];               // user-controlled (active picker)
    inactiveSchedules: ListItemBag[];       // preserved across save, hidden from UI
    groupMemberPersonAliasId?: number | null;
    order: number;
    scheduleConfigs: GroupLocationScheduleConfigBag[];
}

interface GroupLocationScheduleConfigBag {
    scheduleId: number;
    minimumCapacity?: number | null;
    desiredCapacity?: number | null;
    maximumCapacity?: number | null;
}

interface FamilyMemberLocationBag {
    value: string;       // "{LocationId}|{PersonId}"
    text: string;        // friendly text
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
| `Save` (extended) | `ValidPropertiesBox<GroupBag>` | Same as Phase 5 | Adds step 4f (SaveGroupLocations) + post-transaction `KioskDevice.Clear()` invalidation. |
| `Edit`, `GetGroupTypeOptions`, `Delete`, `Archive`, `ArchiveWithChildren`, `Copy` | unchanged | unchanged | The cascade payload `GroupTypeOptionsBag` extends with three new fields. |
| `GetFamilyMemberLocations` (NEW, optional) | `{ groupKey: string }` | `List<FamilyMemberLocationBag>` | Optional lazy-load if the dropdown row count is too large for the initial bag. Phase 6 implementation can choose initial-load (simpler) or lazy-load (scalable). Default: initial-load, surfaced in `GroupTypeOptionsBag` or a sibling field. |

## Save action contributions

See checklist L10 / IS1-IS3 / SS1-SS3 for the extended save flow. Cascades and cache invalidations:

- `SyncRelatedEntities<GroupLocation>` + per-location config + schedule diff — handled by SaveGroupLocations.
- `GroupMemberAssignment.DeleteRange` for removed (location, schedule) tuples — fires inside SaveGroupLocations.
- `KioskDevice.Clear()` — post-transaction when `checkinDataUpdated && GroupType.TakesAttendance`.
- Inline schedule create / reuse / delete — handled by `ApplyInlineSchedule` before transaction opens; the actual `SaveChanges` runs inside step 8.

## Code patterns to follow

- `SyncRelatedEntities<TEntity>` helper for state-list saves (already inlined into GroupDetail.cs in Phase 5).
- Per-location config diff pattern (mirror WebForms exactly per checklist L10 — this is the most error-prone diff in the block).
- `Schedule.Name == string.Empty` convention for inline-schedule detection (Q6.2 lock).
- `ScheduleService.CanDelete` gate before deleting an inline schedule (IS2).
- Tab-switching state pattern in the location modal (single `activeTab: "member" | "other"` ref + computed visibility).

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

(initially empty; populated by the implementing model per SESSION-PROTOCOL.md Section C)

## Completed

(initially empty; populated by the implementing model per SESSION-PROTOCOL.md Section D)
