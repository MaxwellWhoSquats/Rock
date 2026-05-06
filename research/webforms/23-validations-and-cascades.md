# Validations and Side-Effect Cascades

This document is a comprehensive map of every validation gate, every side-effect cascade, and every cache invalidation in `RockWeb/Blocks/Groups/GroupDetail.ascx.cs`. Anything that the Obsidian block must replicate or restructure lives here.

Reference points throughout: `[GroupDetail.ascx.cs:NNNN](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:NNNN)` for the WebForms block, `[Group.Logic.cs:NNNN](Rock/Model/Group/Group/Group.Logic.cs:NNNN)` for the entity partial.

---

## Validation gates in `btnSave_Click`

Order matters. Each gate runs in this sequence and an early `return` short-circuits the rest of the save body.

| # | Gate | Lines | What is checked | What displays on failure |
|---|------|-------|------|---------|
| 1 | GroupType chosen | [791-795](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:791) | `CurrentGroupTypeId == 0` | `ddlGroupType.ShowErrorMessage("GroupType cannot be blank.")` |
| 2 | Self-parent | [1254-1258](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1254) | `group.ParentGroupId == group.Id` | `gpParentGroup.ShowErrorMessage("Group cannot be a Parent Group of itself.")` |
| 3 | Schedule iCalendar parses | [1185-1192](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1185) | If `scheduleType == Custom`, `InetCalendarHelper.CreateCalendarEvent(iCal)` returns null OR `calEvent.DtStart == null` | Silently demotes `scheduleType` to `None` (no error message). |
| 4 | Schedule weekly DayOfWeek chosen | [1195-1201](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1195) | If `scheduleType == Weekly`, `dowWeekly.SelectedDayOfWeek == null` | Silently demotes to `None`. |
| 5 | Parent group allows this group type | [1273-1284](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1273) | `GetAllowedGroupTypes(parentGroupType, ctx).Select(t => t.Id).Contains(group.GroupTypeId) == false` | `nbInvalidParentGroup.Text = "The 'X' group does not allow child groups with a 'Y' group type."`; `nbInvalidParentGroup.Visible = true`. |
| 6 | Re-check EDIT auth | [1286-1291](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1286) | `group.IsAuthorized(EDIT, CurrentPerson) == false` | `nbNotAllowedToEdit.Visible = true`. (Re-checked because the GroupType or ParentGroup may have changed since the page loaded.) |
| 7 | ASP.NET page validators | [1293-1296](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1293) | `Page.IsValid == false` | Built-in validator messages (RequiredFieldValidator etc.). |
| 8 | `Group.IsValid` (model + DB-backed validation) | [1298-1306](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1298) | `group.IsValid == false` | `cvGroup.IsValid = false`; `cvGroup.ErrorMessage = group.ValidationResults.Select(r => r.ErrorMessage).AsDelimited("<br />")`. Custom validator `cvGroup` is used because RuntimeMessages from `IsValid` may not surface through any individual control. |

### `Group.IsValid` body

[Group.Logic.cs:566-593](Rock/Model/Group/Group/Group.Logic.cs:566). Inherits the base `Model<T>.IsValid` (which runs `[Required]`, `[MaxLength]`, `[DefinedValue]`, etc., on the entity attributes), then adds:

```csharp
// validate that a campus is not required
var groupType = this.GroupType ?? new GroupTypeService( rockContext ).Queryable().Where( gt => gt.Id == this.GroupTypeId ).FirstOrDefault();

if ( groupType != null )
{
    if ( groupType.GroupsRequireCampus && this.CampusId == null )
    {
        errorMessage = string.Format( "{0} require a campus.", groupType.Name.Pluralize() );
        ValidationResults.Add( new ValidationResult( errorMessage ) );
        result = false;
    }
}
```

So `IsValid` adds exactly one extra rule: `GroupsRequireCampus -> CampusId is required`. Note that the base annotations cover `Name` (Required + MaxLength 100), `IsSystem`, `GroupTypeId`, `IsSecurityRole`, `IsActive`, `Order`, `IsPublic`, plus `[DefinedValue]` on `InactiveReasonValueId` and `StatusValueId`, and `[DecimalPrecision]` on the four multiplier overrides.

### `cvGroup` CustomValidator

The CustomValidator at the .ascx level participates in `Page.IsValid` (gate 7). On gate 8 the code-behind manually flips `cvGroup.IsValid = group.IsValid`, then sets the error message. There is no server-side `OnServerValidate` handler — the block treats `cvGroup` as a passive container for the `Group.IsValid`/ValidationResults message.

### Custom dialog-level validations

These live inside their own `*_SaveClick` handlers, not `btnSave_Click`:

- `mdGroupRequirement_SaveClick` ([4109-4124](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:4109)): duplicate (`GroupRequirementType` + `GroupRoleId`) check; `nbDuplicateGroupRequirement` shown on failure.
- `dlgMemberWorkflowTriggers_SaveClick` ([4885-4889](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:4885)): `WorkflowTypeId == 0` check; `nbInvalidWorkflowType` shown on failure. Also `memberWorkflowTrigger.IsValid` is checked at line 4904 (silently returns).
- `dlgGroupMemberAttribute_SaveClick` ([4474-4477](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:4474)): `attribute.IsValid` check; controls show their own warnings.
- `mdGroupSyncSettings_SaveClick`: no explicit validation. Relies on dropdown required attributes.
- `gGroupRequirements_ShowEdit` (modal load): re-binds due-date qualifier controls based on `RequirementType.DueDateType` so they're required when the type demands them.

---

## Critical save-time cascades

### `IsActive == false` cascade to children (in-block cascade)

[1361-1380](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1361). Triggered when `cbInactivateChildGroups.Checked` and `group.IsActive == false`.

```
GetAllDescendentGroupIds(group.Id, false)  // active descendants only
GetByIds(...)                              // hydrate the descendants
foreach descendant: if IsActive { 
    childGroup.IsActive = false;
    childGroup.InactiveReasonValueId = ddlInactiveReason.SelectedValueAsInt();
    childGroup.InactiveReasonNote = "Parent Deactivated" + (": " + tbInactiveNote.Text if not blank);
}
SaveChanges()
```

Each child save fires its own `PreSave` hook ([Group.SaveHook.cs:88-153](Rock/Model/Group/Group/Group.SaveHook.cs:88)) which in turn:
- Stamps the descendant's `InactiveDateTime` to `RockDateTime.Now`.
- Bulk-updates every Active/Pending `GroupMember` of that descendant to `Inactive` with the same timestamp.

Net: a single user click can cascade across N descendant groups and M*GroupMembers in one transaction. The Obsidian block must keep the bulk semantics intact.

### `IsActive` flip on the group itself

Handled entirely by the SaveHook ([Group.SaveHook.cs:107-123](Rock/Model/Group/Group/Group.SaveHook.cs:107)):

| Direction | Effect |
|---|---|
| Active -> Inactive | `InactiveDateTime = caller-provided ?? RockDateTime.Now`; bulk-update all non-Inactive `GroupMember.GroupMemberStatus = Inactive` and stamp matching `InactiveDateTime`. |
| Inactive -> Active | `InactiveDateTime = null`; queue a deferred reactivation pipeline (`Task.Run` after PostSave) that picks `GroupMember`s whose `InactiveDateTime` is within 24 hours of the original group `InactiveDateTime`, validates each via `IsValidGroupMember`, and bulk-updates valid ones back to Active. |

### `IsSecurityRole` change

Two places:

1. **Block-level cache flush** ([1408-1425](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1408)): `Rock.Security.Authorization.Clear()` is called whenever the security-role status flips in either direction. Logic uses the captured `wasSecurityRole` (computed pre-save line 809) and the post-save `isNowSecurityRole`.
2. **ElevatedSecurityLevel reset** ([1077-1080](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1077)): `if (!group.IsSecurityRole) group.ElevatedSecurityLevel = ElevatedSecurityLevel.None`.

In addition, the `Group.UpdateCache` partial method ([Group.Logic.cs:444-466](Rock/Model/Group/Group/Group.Logic.cs:444)) flushes `RoleCache` for the group whenever `IsSecurityRole` was or now is true, or when the group type was or now is the security-role group type.

### GroupType change cascade (`ddlGroupType_SelectedIndexChanged`)

[1556-1575](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1556). Triggered only on **new groups** (the `ddlGroupType` is hidden once `group.Id != 0`, line 1949). When fired, the block re-runs the following methods in order, each driven by the new `GroupTypeCache`:

| Method | Lines | Effect |
|---|---|---|
| `SetRecordSourceControls` | [2371-2384](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2371) | Show/hide `dvpRecordSource`, populate options. |
| `SetPeerNetworkControls` | [2391-2442](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2391) | Show/hide entire peer-network override panel; load relationship strength, growth, and 4 multipliers; set placeholder text from group-type defaults. |
| `SetRsvpControls` | [2463-2505](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2463) | Show/hide RSVP panel; set read-only state on offset days and reminder communication based on whether group type has values. |
| `SetScheduleControls` | [2302-2364](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2302) | Reset schedule controls; rebuild ScheduleSelect radio options from `groupType.AllowedScheduleTypes` flags; set `IsScheduleTabVisible`. |
| `ShowGroupTypeEditDetails` | [2173-2295](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2173) | Locations panel visibility (`LocationSelectionMode`, `EnableLocationSchedules`); capacity required + help; status `DefinedType` picker label and options; group attributes editor (DynamicPlaceholder rebuild); scheduling panel visibility; sync panel visibility; member workflow triggers panel visibility. **Note: Lines 2245-2258 and 2260-2273 are an apparent duplicate-block bug.** |
| `BindInheritedAttributes` | [3157-3203](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:3157) | Walk the GroupType.InheritedGroupTypeId chain; collect inherited member-attribute definitions; collect group-attribute date fields for due-date dropdown. |
| `BindGroupRequirementsGrid` | [4541-4565](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:4541) | Re-bind the read-only "From {GroupType}" requirements grid; recompute Add-button visibility based on `EnableSpecificGroupRequirements`. |
| `BindAdministratorPerson` | [2152-2165](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2152) | Show/hide admin picker; set label to `groupType.AdministratorTerm`. |
| `SetChatControls` | [2512-2561](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2512) | Show/hide chat panel; populate values. |

The properties read off `GroupTypeCache` to drive all of this are enumerated in [22-grouptype-cascade.md](research/webforms/22-grouptype-cascade.md).

### ParentGroup change cascade (`ddlParentGroup_SelectedIndexChanged`)

[1582-1627](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1582). Triggered on every parent-group change (and also called manually with `null, null` from `ShowEditDetails` line 2025). What it does:

1. Resolve `parentGroupGroupTypeId` via `groupService.GetSelect(parentGroupId, s => (int?)s.GroupTypeId)`.
2. Compute `groupTypeQry = GetAllowedGroupTypes(GroupTypeCache.Get(parentGroupGroupTypeId ?? 0), ctx)`.
3. Sort by name; if more than one is allowed, prepend an empty placeholder so the user is forced to pick.
4. Rebind `ddlGroupType.DataSource = groupTypes`.
5. If the previously-selected `GroupType` is no longer in the allowed list, clear `CurrentGroupTypeId = 0` and `ddlGroupType.SelectedValue = null`.

There is no separate auth recheck inside this handler; auth is re-checked at save time (gate 6).

### `IsSecurityRole` checked change cascade (`cbIsSecurityRole_CheckedChanged`)

[5042-5059](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:5042). When toggled, re-runs `ShowGroupTypeEditDetails(groupType, group, true)` so that `pnlElevatedSecurity` re-evaluates its visibility (line 2214-2226).

### Schedule deletion cascade (when leaving Custom/Weekly mode)

[1228-1242](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1228). When the user switches the schedule type away from Custom/Weekly:

1. Read `oldScheduleId = hfUniqueScheduleId.Value`.
2. Look up the `Schedule` row.
3. If `string.IsNullOrEmpty(schedule.Name)` (i.e., it's a non-named schedule, not a shared `Named` one):
   1. Run `scheduleService.CanDelete(schedule, out errorMessage)` (built-in DB-relations check).
   2. If allowed, `scheduleService.Delete(schedule)`.

`scheduleService.CanDelete` is the auto-generated `Service<T>.CanDelete` which checks every FK that references `Schedule.Id`. Failure is silent (the schedule stays in place).

The same logic runs in `btnDelete_Click` ([700-712](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:700)): when the group is being deleted and has a `ScheduleId` whose `ScheduleType != Named`, the inline schedule is also deleted, **but only if no other group uses it** (line 707-711 — the explicit `groupService.Queryable().Where(g => g.ScheduleId == schedule.Id && g.Id != group.Id).Any() == false` guard).

### GroupLocation removal cascade

[810-868](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:810). When a `GroupLocation` is removed in the UI:

1. Iterate every `GroupLocationScheduleConfig` in that location and `groupLocation.GroupLocationScheduleConfigs.Remove(...)` (in-memory).
2. For each `Schedule` attached to that location, query `GroupMemberAssignment` for any rows matching (scheduleId, locationId, groupId) and `groupMemberAssignmentService.DeleteRange(...)`.
3. Remove the location from the `Group.GroupLocations` collection.
4. `groupLocationService.Delete(groupLocation)`.
5. Set `checkinDataUpdated = true` (drives the eventual `KioskDevice.Clear`).

### GroupLocation update cascade (location changed)

[907-917](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:907). When a `GroupLocation`'s `LocationId` changed (same Guid, different Location):

1. For each schedule attached to the location, query `GroupMemberAssignment` for any rows matching (scheduleId, oldLocationId, groupId) and `DeleteRange(...)`.
2. Then proceed with the schedule diff (remove no-longer-selected, add new, modify capacities) ([919-988](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:919)).

### `GroupRequirements` deferred-insert pattern

[873-886](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:873) + [1330-1334](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1330). New requirements are collected into `groupRequirementsToInsert` during the pre-save sync, but cannot be `Add`ed yet because they need `GroupId`. After the initial `SaveChanges` (which assigns `group.Id`), the block sets `groupRequirement.GroupId = group.Id` for each, then `groupRequirementService.AddRange(...)`. The next `SaveChanges` (inside the same `WrapTransaction`) commits them.

### `SaveAttributeValues` semantics

[1336](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1336) — `group.SaveAttributeValues(rockContext)` calls `SaveChanges` internally. That's the cited reason ([1308](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1308)) for wrapping the block in `WrapTransaction`: so that the multiple `SaveChanges` calls (FK setup -> attributes -> cascade-inactivate -> chat-avatar IsTemporary) commit atomically.

### Cache invalidations after save

[1408-1436](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1408):

| Trigger | Cache | Notes |
|---|---|---|
| `wasSecurityRole && !isNowSecurityRole` | `Authorization.Clear()` | Was a security role, no longer is. |
| `!wasSecurityRole && isNowSecurityRole` | `Authorization.Clear()` | New security role created. |
| `triggersUpdated == true` (any add/update/delete of `GroupMemberWorkflowTrigger`) | `GroupMemberWorkflowTriggerService.RemoveCachedTriggers()` | Flushes the in-memory trigger cache. |
| `checkinDataUpdated == true && group.GroupType.TakesAttendance == true` | `Rock.CheckIn.KioskDevice.Clear()` | Whole-cache flush. |

In addition, the SaveHook's `UpdateCache` ([Group.Logic.cs:444-466](Rock/Model/Group/Group/Group.Logic.cs:444)) flushes `GroupCache` and `RoleCache` (when relevant) on every save.

### BinaryFile `IsTemporary` toggle (chat avatar)

[1003-1009](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1003) + [1382-1404](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1382). The semantics are:

1. Pre-save: capture the orphaned id if the user changed the avatar (`group.ChatChannelAvatarBinaryFileId != imgChatChannelAvatar.BinaryFileId`).
2. Always update `group.ChatChannelAvatarBinaryFileId = imgChatChannelAvatar.BinaryFileId`.
3. Post-attribute-save (still inside `WrapTransaction`):
   - If `orphanedChatChannelAvatarId.HasValue`, set its `BinaryFile.IsTemporary = true` (it will be cleaned up by the standard temp-file housekeeping job).
   - If `group.ChatChannelAvatarBinaryFileId.HasValue`, set its `BinaryFile.IsTemporary = false` (so the housekeeping job won't reap it).
   - `SaveChanges`.

This pattern avoids a stranded reference and avoids a stranded file. Mirrors `Person.PhotoId` and other binary-file references.

### `AddAdministrateSecurityToGroupCreator` cascade

[1324-1328](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1324). On Add only, when the block-attribute is `true`: `Authorization.AllowPerson(group, ADMINISTRATE, CurrentPerson, rockContext)` writes a single `Auth` row granting the creating person ADMINISTRATE on the new group. Note this happens between the first and second `SaveChanges` inside the transaction.

### `GroupHistory` + `GroupMemberHistory` existence checks (Delete vs Archive button)

[2576-2598](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2576). On view-mode render:

- If the group is `IsSystem`, both Delete and Archive are hidden.
- Otherwise: if `groupType.EnableGroupHistory == true` AND the group has any `GroupHistorical` OR `GroupMemberHistorical` rows, **hide Delete, show Archive**. Otherwise show Delete, hide Archive.
- If `IsArchived == true`, hide Delete (the group is already soft-deleted).

### `DeleteSecurityRoleGroup` vs `Delete`

[716-722](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:716). Branch is via `group.IsSecurityRoleOrSecurityGroupType()`:

- **Security role**: `GroupService.DeleteSecurityRoleGroup(group.Id)` — bulk-deletes `Auth` rows referencing this group, bulk-deletes `AuthAuditLog` rows, calls `Authorization.Clear()`, then `groupService.Delete(group)`, then `SaveChanges`. All wrapped in its own transaction.
- **Non-security role**: `groupService.Delete(group)` — auto-archives instead of deleting if the group's GroupType has `EnableGroupHistory` enabled (per the `groupService.Delete` overload's documentation at [1739-1755](Rock/Model/Group/Group/GroupService.cs:1739) — though note the block already gates on history at the UI level, so this is a defense-in-depth fallback).
- `rockContext.SaveChanges()` follows.

### Archive flow (`ArchiveSingleGroup` and `ArchiveAllChildGroups`)

[3378-3402](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:3378), [3407-3438](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:3407).

- Single: re-checks EDIT auth; calls `groupService.Archive(group, CurrentPersonAliasId, removeFromAuthTables: true)`; `SaveChanges`.
- All children: same EDIT recheck; `GetAllDescendentGroups(group.Id, includeInactiveChildGroups: true)` (note: includes already-inactive descendants); archives each descendant; archives the group itself; `SaveChanges`.

Inside `GroupService.Archive` ([1763-1781](Rock/Model/Group/Group/GroupService.cs:1763)): sets `IsArchived = true`, `ArchivedByPersonAliasId`, `ArchivedDateTime`. If the group was a security role, deletes its `Auth` rows and clears `Authorization`. The SaveHook's `IsArchived` flip then bulk-archives every `GroupMember` of the group.

### Reorder-column maintenance

- `SetAttributeListOrder(List<Attribute>)` ([3209-3213](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:3209)) — reassigns `Order` to a contiguous `0..N-1` based on current ordering then name. Called by `BindGroupMemberAttributesGrid` and `dlgGroupMemberAttribute_SaveClick` (indirectly).
- `ReorderAttributeList(List, int oldIdx, int newIdx)` ([3221-3245](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:3221)) — standard "swap and shift adjacent items" body. Same code shape for triggers.
- `SetMemberWorkflowTriggerListOrder(List<GroupMemberWorkflowTrigger>)` ([3251-3255](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:3251)).
- `ReorderMemberWorkflowTriggerList(List, int, int)` ([3263-3287](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:3263)).

These are pure in-memory operations against the per-block `*State` collections; persistence happens at save time via `CopyPropertiesFrom` per item.

### `ScheduleCoordinatorNotificationTypes` mutual-exclusion logic

[1690-1730](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1690) — when the `cblScheduleCoordinatorNotificationTypes` checkbox list is updated:

- If `None` was just selected (and wasn't before), deselect every other option.
- If `None` is selected and any other option is also selected, deselect `None`.

The previously-selected snapshot is read from `ViewState["ScheduleCoordinatorNotificationTypes"]` (saved at line 578 of `SaveViewState`). On save, the flags are summed into a `ScheduleCoordinatorNotificationType?` and assigned to `group.ScheduleCoordinatorNotificationTypes`.

### `Peer Network` overrides reset cascade

[1089-1117](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1089). When `cbOverrideRelationshipStrength` is unchecked, every override is set to `null`. When it is checked, multipliers are read via `tb*.Text.AsDecimalPercentageOrNull(0, 100)`. Note the explicit comment at [1097-1098](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1097):

```
// This approach will only apply overrides if they're explicitly assigned (and will allow null values
// so the parent group type's values can take effect where needed).
```

The save body never writes the overrides if the group type has `IsPeerNetworkEnabled == false` — it leaves whatever was previously saved untouched (line 1116-1117 explicit comment). This is a deliberate choice so that disabling peer network at the group-type level doesn't wipe out per-group overrides.

### `RSVP overrides` precedence cascade

[1119-1148](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1119). For each of the two override fields (`RSVPReminderOffsetDays`, `RSVPReminderSystemCommunicationId`):

| Group type has value? | Group field set to |
|---|---|
| Yes | `null` (group type wins) |
| No | UI value |

If the group type has `EnableRSVP == false`, both fields are nulled regardless.

### `RecordSource` precedence cascade

[1059-1066](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1059). If `group.GroupType.AllowGroupSpecificRecordSource == true`, save the picker value; otherwise null it out. The runtime resolution is in `Group.GetGroupMemberRecordSourceValueId()` ([Group.Logic.cs:803-813](Rock/Model/Group/Group/Group.Logic.cs:803)) which falls back to the GroupType's value when allowed.

### `LimittoSecurityRoleGroups` block-attribute cascade

[1071-1074](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1071), [2030-2043](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2030), [2110-2114](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2110). When the block attribute is `true`:

1. The default GroupType for new groups is forced to `GroupTypeCache.GetSecurityRoleGroupType()`.
2. `cbIsSecurityRole.Enabled = false` and `cbIsSecurityRole.Checked = true` (read-only true).
3. On save, `group.IsSecurityRole = true` is forced regardless of UI state.

### `cbIsSecurityRole.Visible` admin-only gating

[2022](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2022). The "Is Security Role" checkbox is only visible when the current user is a member of the `GROUP_ADMINISTRATORS` system group:

```
cbIsSecurityRole.Visible = groupService.GroupHasMember( new Guid( Rock.SystemGuid.Group.GROUP_ADMINISTRATORS ), CurrentUser.PersonId );
```

If the user can't see the checkbox, they can't change the value — which means non-admins editing a non-security group cannot turn it into one.

---

## Other side effects to track

### Transactional boundary

The save body is wrapped in `rockContext.WrapTransaction(...)` at [1308-1406](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1308). What happens inside (in order):

1. `Add(group)` if new -> `SaveChanges` (to populate `group.Id`).
2. If new and `AddAdministrateSecurityToGroupCreator`: `Authorization.AllowPerson(...)`.
3. `groupRequirementsToInsert.ForEach(a => a.GroupId = group.Id); groupRequirementService.AddRange(groupRequirementsToInsert)`.
4. `group.SaveAttributeValues(rockContext)` (this calls `SaveChanges` itself).
5. Loop through removed group-member attribute definitions, `attributeService.Delete(...)`. Loop through current ones, `Helper.SaveAttributeEdits(...)`.
6. `SaveChanges`.
7. Cascade-inactivate child groups (if checkbox checked + group is now inactive). `SaveChanges`.
8. Chat-avatar `IsTemporary` flips. `SaveChanges`.

The `WrapTransaction` ensures all steps either commit or roll back together. Outside the transaction is the cache-flush block (1408-1436) and navigation (1438-1450).

### Following / Tag / Audit-detail side effects

- `FollowingsHelper.SetFollowing(group, pnlFollowing, CurrentPerson)` ([547](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:547)) wires the follow-star UI. Pure read.
- `pdAuditDetails.SetEntity(group, ...)` ([2691](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2691)) sets up the audit-detail panel-drawer.
- `taglGroupTags.GetTagValues(CurrentPersonId)` ([544](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:544)) loads tags. The `EnableGroupTags` block attribute and `groupType.EnableGroupTag` both must be true for the tag list to render.

### `lava` side effect on load

[1770-1773](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1770) constructs and discards a Lava string `"{{ Group.Name | AddQuickReturn:'Groups', 20 }}"`. The result is unused but `AddQuickReturn` is a side-effect filter that records the visited group in the user's quick-return cache. The Obsidian conversion should preserve this side effect on view-mode load (likely through a single block-action call that resolves the same Lava expression).

---

## Cascade summary table (trigger -> effects)

| Trigger | Validation effect | Field effect | Cascade effect | Cache effect |
|---|---|---|---|---|
| GroupType change (new group only) | (none) | Reset elevated security level (when not a security role); reset capacity required+help; reset peer network defaults; reset RSVP defaults; reset record source defaults; reset chat | Re-bind inherited member attributes; re-bind type-level requirement grid; re-bind admin-person picker label | (none — purely UI) |
| Parent group change | (none) | Reset GroupType list | Re-filter `ddlGroupType.DataSource` against parent's `ChildGroupTypes` | (none) |
| `IsSecurityRole` toggled (cbIsSecurityRole) | (none) | If unchecked, force `ElevatedSecurityLevel = None` (only on save) | Re-evaluate `pnlElevatedSecurity.Visible` via `ShowGroupTypeEditDetails` | (post-save) `Authorization.Clear()` if status flips |
| `IsActive` toggled and unchecked + cbInactivateChildGroups | None on this group; SaveHook handles InactiveDateTime | Save: `InactiveReasonValueId = ddl`, `InactiveReasonNote = tb`. Children: `IsActive = false`, `InactiveReasonValueId = same ddl`, `InactiveReasonNote = "Parent Deactivated[: <orig>]"` | SaveHook bulk-updates this group's GroupMembers (Active->Inactive). For each cascaded child, SaveHook bulk-updates its GroupMembers too. | None special |
| Schedule type changed (Custom/Weekly -> None or Named) | (none — parsing failures silently demote) | Save: `group.ScheduleId = null` (or named id). Old schedule entity: deleted if `Name == ""` AND `CanDelete()`. | SaveHook deletes group's schedule if applicable | None |
| Group deleted | EDIT auth re-checked. `CanDelete(includeSecondLvl: true)` (RegistrationRegistrant + EventItemOccurrence linkages). | Sets `IsArchived = true` if `EnableGroupHistory` && history rows exist (via `Delete` overload's auto-archive); otherwise hard-deletes. Inline non-named `ScheduleId` schedule deleted if no other group uses it. | SaveHook: bulk-deletes Attendance, GroupMemberAssignment, GroupRequirement (for the group). | `Authorization.Clear` if security role |
| Group archived | EDIT auth re-checked. | `IsArchived = true`, `ArchivedDateTime`, `ArchivedByPersonAliasId`. | SaveHook bulk-updates GroupMember.IsArchived. | `Authorization.Clear` if security role |
| `GroupLocation` removed | (none) | `GroupLocation` deleted; `GroupLocationScheduleConfigs` removed in-memory; `GroupMemberAssignment` deleted via service | None | `KioskDevice.Clear()` post-save (if `groupType.TakesAttendance`) |
| `GroupLocation` location changed | (none) | Schedules re-attached/diff'd; `GroupMemberAssignment` deleted for old (locationId, scheduleId, groupId) tuples | None | `KioskDevice.Clear()` post-save |
| `GroupSync` removed | (none) | `GroupSync` deleted via service | None | None |
| `GroupMemberWorkflowTrigger` add/edit/delete | (none) | Trigger row written | None | `GroupMemberWorkflowTriggerService.RemoveCachedTriggers()` post-save |
| `GroupMemberAttribute` (custom) add/edit/delete | (none) | Attribute row written via `SaveAttributeEdits`; orphaned Attribute rows deleted | None | None special |
| Chat avatar changed | (none) | `BinaryFile.IsTemporary = true` for orphaned id; `false` for new id | None | None |
| New group created with `AddAdministrateSecurityToGroupCreator` | (none) | (none on group) | `Auth` row written: ADMINISTRATE for CurrentPerson | None special (the next `Authorization` check loads the new row) |

---

## Open questions / flag for spec phase

- The `lava` side-effect call at [1770-1773](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1770) (`AddQuickReturn`) should be preserved. Decide whether to invoke this from the block's `GetEntityBag`/`GetBox` helper or expose a `RecordVisit` block action.
- The duplicate-block bug at [2245-2273](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2245) (the same locations-panel visibility logic appears twice in `ShowGroupTypeEditDetails`) should be flagged for cleanup in the conversion. It is harmless in WebForms (idempotent) but no need to carry it over.
- The block-level cache flush at [1408-1425](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1408) is technically redundant given `Group.UpdateCache` ([Group.Logic.cs:451-465](Rock/Model/Group/Group/Group.Logic.cs:451)) already flushes `RoleCache` per save. But the block also calls the broader `Authorization.Clear()` (full cache wipe) for any security-role status flip. Confirm whether the Obsidian block should keep both or rely solely on the SaveHook + UpdateCache path.
- `KioskDevice.Clear()` is a global cache wipe. If the codebase later adopts a more granular API, this is a candidate for refinement.
- The `IsActive == false && cbInactivateChildGroups.Checked` cascade is in-block and uses `GetAllDescendentGroupIds(false)` which excludes already-inactive descendants. The SaveHook has its own `IsActive` flip cascade, so this code only affects the descendants. Confirm whether the Obsidian bag should expose `inactivateChildGroups: bool` and re-implement this loop on the C# side, or push it down to a `GroupService` helper.
- `mdGroupRequirement_SaveClick` removes the in-progress requirement from state on duplicate detection ([4122](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:4122)) before showing the error. Slightly surprising semantics — the user has to re-enter the data. Spec should decide whether the Obsidian behavior is the same, or to leave the entry in place and just show the error.
- `cvGroup`'s ErrorMessage is a `<br />`-delimited string of `ValidationResult.ErrorMessage` values, embedded in the standard ASP.NET ValidationSummary. Obsidian's equivalent will need its own message-list pattern (probably an `errors[]` field on the response bag).
- The order in which gates 5/6 fire (allowed-group-type vs. EDIT auth) matters: a user could be denied parent-allows-this-type before being told they have no edit rights. In Obsidian this should remain consistent so messaging matches WebForms.
- `GroupService.Delete(group)` (no overload taking `removeFromAuthTables`) is called at line 722. This auto-archives instead of deleting if `GroupType.EnableGroupHistory` is true. The block's UI logic (Delete vs Archive button) tries to prevent this scenario, but if the user's local copy is stale they could hit the auto-archive fallback. Spec should confirm this behavior is intentional and document it for the Obsidian block.
