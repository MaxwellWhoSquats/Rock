# Entities and Services Touched

This is the surface that the Obsidian conversion's C# block class will need to call. Every entity, service, cache, navigation property, and helper that GroupDetail touches is listed here.

## Primary entity

`Rock.Model.Group` — declared at [Group.cs](Rock/Model/Group/Group/Group.cs); partials at [Group.Logic.cs](Rock/Model/Group/Group/Group.Logic.cs) and [Group.SaveHook.cs](Rock/Model/Group/Group/Group.SaveHook.cs).

### Scalar fields read or written by the block

Written on save (see [GroupDetail.ascx.cs:1050-1182](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1050)):
- `Name`, `Description`, `IsActive`, `IsPublic`.
- `IsSecurityRole`, `IsSystem` (read-only display only; never written).
- `GroupTypeId`, `ParentGroupId`, `CampusId`, `StatusValueId`, `GroupCapacity`.
- `RequiredSignatureDocumentTemplateId`, `GroupMemberRecordSourceValueId`.
- `ElevatedSecurityLevel`.
- `InactiveReasonValueId`, `InactiveReasonNote` (cleared when `IsActive == true`).
- `GroupAdministratorPersonAliasId` (only when `groupType.ShowAdministrator`).
- `RelationshipStrengthOverride`, `RelationshipGrowthEnabledOverride`, `LeaderToLeaderRelationshipMultiplierOverride`, `LeaderToNonLeaderRelationshipMultiplierOverride`, `NonLeaderToLeaderRelationshipMultiplierOverride`, `NonLeaderToNonLeaderRelationshipMultiplierOverride` (peer network).
- `RSVPReminderOffsetDays`, `RSVPReminderSystemCommunicationId` (only when group type has RSVP enabled and the group type does not pin its own value).
- `SchedulingMustMeetRequirements`, `AttendanceRecordRequiredForCheckIn`, `ScheduleCoordinatorPersonAliasId`, `DisableScheduling`, `DisableScheduleToolboxAccess`, `ScheduleConfirmationLogic`, `ScheduleCoordinatorNotificationTypes` (built up as a flags enum from the `cblScheduleCoordinatorNotificationTypes` checkbox list, with mutual-exclusion logic for the `None` option).
- `ScheduleId` (set or cleared based on schedule type).
- `IsChatEnabledOverride`, `IsLeavingChatChannelAllowedOverride`, `IsChatChannelPublicOverride`, `IsChatChannelAlwaysShownOverride`, `ChatPushNotificationModeOverride`, `ChatChannelAvatarBinaryFileId` (only when `ChatHelper.IsChatEnabled && groupType.IsChatAllowed`).

Computed / accessed (not written):
- `Id`, `Guid`, `Order`, `IsArchived`, `ArchivedDateTime`, `ArchivedByPersonAliasId`, `InactiveDateTime`, `ChatChannelKey`.
- `IsValid`, `ValidationResults` (line 1300-1305 — combined model + db-level validation).
- `IsSecurityRoleOrSecurityGroupType()` (extension on `Group`, line 716, used in delete branch selection).
- `GetGroupRequirements(rockContext)` (line 847, 878, 2066 — joins service-side `GroupRequirementService`).
- `GetGroupTypeRoleLimitWarnings(out string)` (line 1850 — computes role-min/role-max warnings against current `GroupMember` counts).
- `GetIsChatEnabled()` (line 2664 — falls back to group-type defaults).
- `IsOverridingGroupTypePeerNetworkConfiguration` (computed property on partial — used in label rendering line 2402-2403, 2641-2643).
- `AreAnyRelationshipMultipliersCustomized` (computed property on partial — used in advanced-panel-default toggle line 2412).
- `LoadAttributes()` (line 1260, 2276, 3979 — populates `group.Attributes` for the dynamic editor and for due-date attribute filtering).
- `SaveAttributeValues(rockContext)` (line 1336 — internally calls `SaveChanges`; that's the reason the save loop is wrapped in `WrapTransaction`).

### Navigation properties traversed

| Property | Type | Touched at | Why |
|---|---|---|---|
| `group.GroupType` | `GroupType` | line 871, 1044, 1120, 1268, 2058 etc. | All cascading edit logic reads off `GroupTypeCache`, but the entity nav property itself is set on save (line 871) so that `IsValid` can re-resolve. |
| `group.ParentGroup` | `Group` | line 1264-1266, 1274-1284, 1789-1823 | Used to validate parent is allowed to host this group type. New group inherits ParentGroupId from query string. |
| `group.Campus` | `Campus` | Read in view-mode label (line 2693). | Not written through the navigation; only via `CampusId`. |
| `group.Schedule` | `Schedule` | Eagerly included from `Queryable("Schedule, GroupLocations.Schedules")` line 808, then re-bound during weekly/custom/named schedule processing line 1206-1226. New `Schedule` instance constructed inline when required. |
| `group.GroupLocations` | `ICollection<GroupLocation>` | line 813, 840, 893-897 etc. | Each location's child collections (`GroupLocationScheduleConfigs`, `Schedules`, `GroupLocationTypeValue`) are reconciled in memory. |
| `group.GroupLocations[i].Schedules` | `ICollection<Schedule>` | line 830, 904, 919, 922, 932-939 | Each schedule attached/detached based on UI state. |
| `group.GroupLocations[i].GroupLocationScheduleConfigs` | `ICollection<GroupLocationScheduleConfig>` | line 818, 930, 941-988 | In-memory diff: existing-with-modified-capacity, new, deleted. |
| `group.GroupSyncs` | `ICollection<GroupSync>` | line 863, 1014-1022, 2007-2017 | Each sync is hydrated with `GroupTypeRole` and `SyncDataView` for grid rendering. |
| `group.GroupMemberWorkflowTriggers` | `ICollection<GroupMemberWorkflowTrigger>` | line 854-859, 1027-1041, 2131-2134, 2198 | Removed/added/reordered from in-memory state. |
| `group.GroupAdministratorPersonAlias` -> `Person` | `PersonAlias`, `Person` | line 2160-2163 | Read for picker pre-population. |
| `group.ScheduleCoordinatorPersonAlias` -> `Person` | `PersonAlias`, `Person` | line 2100-2103 | Same. |
| `group.GroupRequirements` | `ICollection<GroupRequirement>` (via `GetGroupRequirements`) | line 847, 876-886, 2066 | Group-only requirements come back via the service-helper which unions group + group-type rows. |
| `group.Attributes` | `Dictionary<string, AttributeCache>` (added by LoadAttributes) | line 2278, 3980 | Iterated to find date-typed attributes for the requirement-due-date dropdown. |

### Computed properties referenced (LavaVisible / NotMapped)

- `IsOverridingGroupTypePeerNetworkConfiguration` ([Group.Logic.cs:64-113](Rock/Model/Group/Group/Group.Logic.cs:64))
- `AreAnyRelationshipMultipliersCustomized` ([Group.Logic.cs:128-153](Rock/Model/Group/Group/Group.Logic.cs:128))
- `IsValid` ([Group.Logic.cs:566-593](Rock/Model/Group/Group/Group.Logic.cs:566)) — adds `GroupsRequireCampus` rule on top of base.
- `SupportedActions` (`VIEW`, `MANAGE_MEMBERS`, `EDIT`, `ADMINISTRATE`, `SCHEDULE`).
- `ParentAuthority` returns `GroupCache.Get(ParentGroupId)`; `ParentAuthorityPre` returns `GroupTypeCache.Get(GroupTypeId)`.

## Related entities touched

- `GroupLocation` (FK targets `Group.Id`, `Location.Id`, `DefinedValue.Id`, optional `PersonAlias.Id`).
- `GroupLocationScheduleConfig` (`ScheduleId`, `GroupLocationId`, capacity ints).
- `GroupRequirement` (`GroupId` or `GroupTypeId` populated, plus `GroupRequirementType`, `GroupRole`, `AppliesToDataView`, `DueDateAttributeId`).
- `GroupSync` (`GroupId`, `GroupTypeRoleId`, `SyncDataViewId`, `WelcomeSystemCommunicationId`, `ExitSystemCommunicationId`, `AddUserAccountsDuringSync`, `ScheduleIntervalMinutes`).
- `GroupMemberWorkflowTrigger` (`GroupId`, `WorkflowTypeId`, `TriggerType`, `TypeQualifier`, `Order`, `Name`, `IsActive`).
- `GroupMemberAssignment` (deleted in cascade when location/schedule removed).
- `GroupHistorical`, `GroupMemberHistorical` (existence-checked only — drive Delete vs Archive button visibility).
- `Schedule` (lifecycle for inline non-named schedules — see schedule-deletion side effect).
- `Attribute` and `AttributeValue` (for both group attributes and group-member attribute definitions, qualifier `EntityTypeQualifierColumn`/`Value`).
- `BinaryFile` (`IsTemporary` toggle for chat-channel avatar — orphan vs current).
- `Auth` (created when `AddAdministrateSecurityToGroupCreator`; deleted by `DeleteSecurityRoleGroup`).
- `GroupMember` (queried indirectly — for member-address picker via `PersonService.GetFamilies`).
- `Person` / `PersonAlias` — picker resolution.
- `Location` and `DefinedValue` (location type) — group location dialog.
- `EventItemOccurrenceGroupMap` — read indirectly via `CanDelete(includeSecondLvl: true)` to block delete when linkages exist.
- `RegistrationRegistrant` — same.

## Services used (every method called, with signature)

### GroupService

| Method | Signature | Where used |
|---|---|---|
| Constructor | `GroupService(rockContext)` | Save flow (line 777), btnDelete (680), Archive flows (3382, 3411), `ShowDetail` (line 1785), and many more. |
| `Get(int)` | `Group Get(int id)` | line 599, 682, 871, 1265, 1785, 2336, 3385, 3414. |
| `Queryable()` | `IQueryable<Group> Queryable()` | line 641, 708, 2829. |
| `Queryable(string)` | `IQueryable<Group> Queryable(string includes)` | line 808 — `"Schedule,GroupLocations.Schedules"`. |
| `Add(Group)` | inherited from `Service<T>` | line 1314. |
| `Delete(Group)` | inherited | line 722. |
| `CanDelete(Group, out string, bool)` | `bool CanDelete(Group, out string, bool includeSecondLvl)` ([GroupService.cs:1709](Rock/Model/Group/Group/GroupService.cs:1709)) | line 694 — `includeSecondLvl: true` triggers RegistrationRegistrant + EventItemOccurrence linkage checks. |
| `Archive(Group, int?, bool)` | `void Archive(Group, int? currentPersonAliasId, bool removeFromAuthTables)` ([GroupService.cs:1763](Rock/Model/Group/Group/GroupService.cs:1763)) | line 3396, 3429, 3432. Sets `IsArchived = true`, stamps `ArchivedDateTime` and `ArchivedByPersonAliasId`, and if it was a security role flushes Auth rows + clears `Authorization` cache. |
| `GetAllDescendentGroupIds(int, bool)` | `List<int> GetAllDescendentGroupIds(int parentGroupId, bool includeInactiveChildGroups)` ([GroupService.cs:689](Rock/Model/Group/Group/GroupService.cs:689)) — runs raw CTE SQL via `Database.SqlQuery<int>`. | line 1363 (cascade-inactivate). |
| `GetAllDescendentGroups(int, bool)` | `List<Group> GetAllDescendentGroups(int parentGroupId, bool includeInactiveChildGroups)` ([GroupService.cs:672](Rock/Model/Group/Group/GroupService.cs:672)) — `ExecuteQuery(sql)` over CTE. | line 3426 (archive-all-children). |
| `GetByIds(List<int>)` | inherited from `Service<T>` | line 1364. |
| `HasDescendantGroups(int, bool)` | `bool HasDescendantGroups(int parentGroupId, bool includeInactiveChildGroups)` ([GroupService.cs:706](Rock/Model/Group/Group/GroupService.cs:706)) — CTE-based `SELECT 1 WHERE EXISTS`. | line 1989 (sets `hfHasChildGroups`). |
| `GroupHasMember(Guid, int?)` | `bool GroupHasMember(Guid groupGuid, int? personId)` ([GroupService.cs:856](Rock/Model/Group/Group/GroupService.cs:856)) | line 2022 — checks if `CurrentUser.PersonId` is a member of the GROUP_ADMINISTRATORS group. |
| `GetSelect<T>(int, expr)` | `T GetSelect<T>(int id, Expression<Func<Group, T>> selector)` (inherited) | line 1589 — `s => (int?)s.GroupTypeId`. |
| `DeleteSecurityRoleGroup(int)` | `static void DeleteSecurityRoleGroup(int groupId)` ([GroupService.cs:2192](Rock/Model/Group/Group/GroupService.cs:2192)) | line 718. Wraps a transaction: bulk deletes `Auth` and `AuthAuditLog` for this `GroupId`, calls `Authorization.Clear()`, then `groupService.Delete(group)`, then `SaveChanges`. |
| `CopyGroup(CopyGroupOptions)` | `static int? CopyGroup(CopyGroupOptions options)` ([GroupService.cs:1888](Rock/Model/Group/Group/GroupService.cs:1888)) | line 1538. Wraps a transaction; copies group + locations (excluding member addresses) + schedules + attributes/qualifiers + auths + sync settings + group requirements. Recursively copies child groups when `IncludeChildGroups`. |

### GroupTypeService

| Method | Signature | Where used |
|---|---|---|
| Constructor | `GroupTypeService(rockContext)` | line 389, 871, 1276, 1592, 1968, 2189, 2829, 2943, 3955. |
| `Get(int)` | `GroupType Get(int)` | line 871 — re-resolves type for save. |
| `Queryable()` | `IQueryable<GroupType>` | line 1593, 2829, 2945. |
| `GetInactiveReasonsForGroupType(int)` | `List<DefinedValueCache> GetInactiveReasonsForGroupType(int groupTypeId)` ([GroupTypeService.cs:297](Rock/Model/Group/GroupType/GroupTypeService.cs:297)) — internally calls the Guid overload. | line 1977 — populates `ddlInactiveReason`. |

### GroupTypeRoleService

| Method | Signature | Where used |
|---|---|---|
| `GetByIds(List<int>)` | inherited | line 389. |
| `Get(int)` | `GroupTypeRole Get(int)` | line 2013, 4083, 4263. |
| `Queryable()` | `IQueryable<GroupTypeRole>` | line 4339-4342 — filters by current GroupTypeId, excluding already-synced roles. |

### GroupLocationService

| Method | Where used |
|---|---|
| Constructor | line 778. |
| `Delete(GroupLocation)` | line 841 — final step of removing a `GroupLocation` from a saved group. |

### GroupRequirementService

| Method | Where used |
|---|---|
| Constructor | line 779. |
| `Queryable()` | line 4544 — fetches GroupType-level requirements for the read-only grid. |
| `AddRange(IEnumerable<GroupRequirement>)` | line 1333 — inserts deferred new group-level requirements after the new group has its Id. |
| `Delete(GroupRequirement)` | line 849 — removes UI-deleted group-level requirements. |

### GroupRequirementTypeService

| Method | Where used |
|---|---|
| Constructor | line 3954, 5068. |
| `Queryable()` | line 3955 — populates `ddlGroupRequirementType` and resolves a `GroupRequirementType` by Id. |

### GroupMemberWorkflowTriggerService

| Method | Where used |
|---|---|
| Constructor | line 780. |
| `Delete(GroupMemberWorkflowTrigger)` | line 857 — removes UI-deleted triggers. |
| `static RemoveCachedTriggers()` | line 1429 — flushes the `GroupMemberWorkflowTrigger` cache after any trigger update. |

### GroupMemberAssignmentService

| Method | Where used |
|---|---|
| Constructor | line 786. |
| `Queryable()` | line 832, 911 — looks up assignments tied to (`scheduleId`, `locationId`, `groupId`) tuples that are about to become stale. |
| `DeleteRange(IEnumerable<GroupMemberAssignment>)` | line 836, 915. |

### GroupMemberService

| Method | Where used |
|---|---|
| Constructor | line 3533. |
| `GetByGroupId(int)` | line 3533 — enumerated for the member-address picker (per saved member, find their family addresses). |

### GroupSyncService

| Method | Where used |
|---|---|
| Constructor | line 785. |
| `Delete(GroupSync)` | line 866. |

### GroupHistoricalService

| Method | Where used |
|---|---|
| Constructor | line 2582. |
| `Queryable()` | line 2582 — existence check (`Any(a => a.GroupId == group.Id)`) to drive Delete vs Archive button. |

### GroupMemberHistoricalService

| Method | Where used |
|---|---|
| Constructor | line 2583. |
| `Queryable()` | line 2583 — same existence check. |

### ScheduleService

| Method | Where used |
|---|---|
| Constructor | line 703, 781, 3304, 3610, 3765, 3817. |
| `Get(int)` | line 704, 1232. |
| `Get(Guid)` | line 934 — used when adding a new schedule to a `GroupLocation`. |
| `GetByIds(List<int>)` | line 3309, 3610, 3765. |
| `Queryable()` | line 3818 — orders by group-location selected/inactive ids. |
| `Delete(Schedule)` | line 710, 1239. |
| `CanDelete(Schedule, out string)` | line 1237 — checks no other group references this schedule before deleting an inline non-named one. |

### LocationService

| Method | Where used |
|---|---|
| `Get(int)` | line 3571, 3721 — re-fetches the `Location` after picking an existing one. |

### DefinedValueService

| Method | Where used |
|---|---|
| `Get(int)` | line 3825 — re-fetches `DefinedValue` for `GroupLocationTypeValue`. |

### AttributeService

| Method | Where used |
|---|---|
| Constructor | line 782, 1969, 3157. |
| `GetByEntityTypeId(int, bool)` | line 2117, 3168, 3185 — used for both group-member attributes (qualified by GroupId) and inherited group attributes. |
| `GetByEntityTypeQualifier(int, string, string, bool)` | line 1344 — fetches attributes already persisted for `EntityTypeId = GroupMember`, `qualifierColumn = GroupId`, `qualifierValue = group.Id` so they can be diff'd against UI state. |
| `Delete(Attribute)` | line 1350 — removes group-member attributes the user dropped from the list. |

### AttributeQualifierService

Instantiated at line 783, declared but never used in the rest of the block. (Carried for parity.)

### CategoryService

Instantiated at line 784, declared but never used in the rest of the block. (Carried for parity.)

### DataViewService

| Method | Where used |
|---|---|
| Constructor | line 2014, 4265. |
| `Get(int)` | line 2014 — hydrates `GroupSync.SyncDataView` for the grid; line 4265 — same on save. |

### WorkflowTypeService

| Method | Where used |
|---|---|
| `Queryable()` | line 4679, 4868 — used as `FirstOrDefault(a => a.Id == triggerId)`. |

### SignatureDocumentTemplateService

| Method | Where used |
|---|---|
| `GetLegacyTemplates()` | line 3024 — populates the legacy signature-document template dropdown. |

### SystemCommunicationService

| Method | Where used |
|---|---|
| Constructor | line 3034, 4294. |
| `Queryable()` | line 3037 — filtered to `CategoryId == rsvpReminderCategoryId` for the RSVP reminder template; line 4294 — full list for welcome/exit/RSVP-reminder pickers in the GroupSync modal. |

### BinaryFileService

| Method | Where used |
|---|---|
| Constructor | line 1384. |
| `Get(int)` | line 1388, 1397 — fetches both orphaned and current chat-channel-avatar to flip their `IsTemporary` flag. |

### PersonService

| Method | Where used |
|---|---|
| Constructor | line 3530. |
| `GetFamilies(int)` | line 3535 — for each member of the group, enumerates the families they belong to so that the member-tab in the location picker can show family addresses. |

### PersonAliasService

| Method | Where used |
|---|---|
| `GetPersonId(int)` | line 3578 — translate a `GroupMemberPersonAliasId` back to `PersonId` for the dropdown selected-value format `{LocationId}|{PersonId}`. |
| `GetPrimaryAliasId(int)` | line 3728 — translate a chosen `personId` (from the member-tab dropdown) into the primary alias id stored on `GroupLocation.GroupMemberPersonAliasId`. |

### AuthService

Instantiated at line 681, 3383, 3412 but never invoked beyond construction in this block. (Carried for parity. Actual auth manipulation goes through the static `Rock.Security.Authorization` class.)

### Rock.Security.Authorization (static helper)

| Method | Where used |
|---|---|
| `AllowPerson(ISecured, action, Person, RockContext)` | line 1327 — only when `AddAdministrateSecurityToGroupCreator` is true; grants ADMINISTRATE to the creator. |
| `Clear()` | line 1415, 1423 — flushes the entire authorization cache when security-role status flips. |
| Constants `VIEW`, `EDIT`, `ADMINISTRATE` | passed throughout. |

### Rock.Attribute.Helper (static helper)

| Method | Where used |
|---|---|
| `AddEditControls(group, ph, setValues, validationGroup, excludeForEdit)` | line 2282 — populates `phGroupAttributes` with one input control per group-level attribute. |
| `GetEditValues(ph, group)` | line 1261 — pulls values back out of `phGroupAttributes` into `group.AttributeValues` (in memory). |
| `SaveAttributeEdits(attribute, entityTypeId, qualifierColumn, qualifierValue, rockContext)` | line 1356 — per-row upsert/insert for group-member attribute definitions. |

### Rock.CheckIn.KioskDevice (static helper)

| Method | Where used |
|---|---|
| `Clear()` | line 1435 — only when `checkinDataUpdated && groupType.TakesAttendance`. Conditional invalidation of the kiosk-device cache. |

### Rock.Lava.LavaHelper (static helper)

| Method | Where used |
|---|---|
| `GetCommonMergeFields(rockPage, person, options)` | line 1771, 2727 — assembles merge fields for the view-mode Lava template. |

### InetCalendarHelper (static helper)

| Method | Where used |
|---|---|
| `CreateCalendarEvent(string)` | line 1188 — validates the iCalendar content for a Custom schedule. |

### FollowingsHelper (static helper)

| Method | Where used |
|---|---|
| `SetFollowing(group, panel, person)` | line 547 — wires up the "Follow this group" star UI. |

### Rock.Communication.Chat.ChatHelper (static guard)

| Property | Where used |
|---|---|
| `IsChatEnabled` (static) | line 995, 2514, 2664 — guards every chat-related code path. |

## Caches used (every read)

| Cache | Methods called | Where |
|---|---|---|
| `GroupTypeCache` | `Get(int)` | line 297-301 (property), 788, 1276, 2058, 2069, 2164, 2189, 2484, 2573, 3164, 3484, 4649, 5050. |
| | `Get(Guid)` | line 1592 (resolves parent's group type — caller passes int? but goes through the int overload via the property). |
| | `GetSecurityRoleGroupType()` | line 2033 — for the "limit to security role groups" default selection, and line 527 of Group.Logic.cs (extension). |
| | `Get(typeof(Group))` (rare) | indirect via BadgeCache. |
| | `All()` | indirect via BadgeCache. |
| `EntityTypeCache` | `Get<T>()` / `Get(typeof(...))` / `GetId<T>()` | line 446, 467, 1339, 4176, 4198. |
| `BadgeCache` | `All(typeof(Group))` | line 476 — enumerates badges for the `BadgeListControl`. |
| `FieldTypeCache` | `Get(Guid)` | line 4400 — default field type for new group-member attribute. |
| | `GetId(Guid)` | line 285-286 — caches the int Ids of `DATE` and `DATE_TIME` field types in a static. |
| `DefinedTypeCache` | `Get(Guid)` | line 2376 — record-source defined-type id. |
| `CategoryCache` | `GetId(Guid)` | line 3036 — RSVP confirmation category for filtering system communications. |
| `DefinedValueCache` | `Get(Guid)`, `Get(string)` (via attribute value) | line 2734, 2737 — map style defined value. |
| `AttributeCache` | `AllForEntityType<Group>()` | line 4102 — looks up group-level date attributes for the requirement-due-date dropdown when saving the requirement. |
| `CampusCache` | `SingleCampusId` | line 1047 — fallback campus when group type requires campus and only one exists. |
| `PageCache` | indirect through `Layout.Site.DisablePredictableIds` | not explicitly used; relevant if the conversion adopts IdKey routing. |

## RockContext usage patterns

- A new `RockContext` is constructed at the entry of every event handler (`btnSave_Click`, `btnDelete_Click`, `ddlParentGroup_SelectedIndexChanged`, `ShowDetail`, `ShowEditDetails`, etc.).
- Most save / delete flows construct one `RockContext`, instantiate every needed service against it, then `SaveChanges` once at the end.
- The save flow specifically wraps its multi-step `SaveChanges` block in `rockContext.WrapTransaction(...)` (line 1308-1406). The reason: `SaveAttributeValues` and the save logic each call `SaveChanges` independently, so the transaction is needed to keep them atomic.
- `GetGroup(int, RockContext?)` (line 2913-2931) caches the `Group` instance via `RockPage.GetSharedItem` / `SaveSharedItem` keyed on `"Group:{id}"`, including `GroupType`, `GroupLocations.Schedules`, and `GroupSyncs`. Lazy loading is relied on for everything else (e.g., `Members`, `Attributes`, `GroupAdministratorPersonAlias.Person`).
- `LoadViewState` constructs its own `RockContext` to re-hydrate `GroupTypeRole` instances on `GroupRequirement` rows that lost them through JSON serialization (line 389).
- `BindGroupRequirementsGrid` constructs its own `RockContext` for the type-level requirements query (line 4543).
- `cbIsSecurityRole_CheckedChanged` calls `GetGroup` (which uses its own context) and then mutates `IsSecurityRole` on the in-memory entity but never persists.

No direct SQL or stored procedures are called from the block. The only raw SQL is inside `GroupService.GetAllDescendent*` and `HasDescendantGroups` (CTE) and `Group.Logic.cs` indexing helpers, all of which are inside the service layer.

## `.Include()` calls

- `GetGroup` (line 2913): `.Include(g => g.GroupType).Include(g => g.GroupLocations.Select(s => s.Schedules)).Include(g => g.GroupSyncs)`.
- Save flow (line 808): `groupService.Queryable("Schedule,GroupLocations.Schedules")` — the comma-delimited string form of `Include`.
- `GetGroupRequirements` ([Group.Logic.cs:538](Rock/Model/Group/Group/Group.Logic.cs:538)): `.Include(a => a.GroupRequirementType)` (called from save and from edit-form bind).
- `CopyGroup` ([GroupService.cs:1898](Rock/Model/Group/Group/GroupService.cs:1898)): `.Include(g => g.GroupType)`.

Lazy loading is relied on for everything else: `group.Schedule`, `group.Campus`, `group.GroupAdministratorPersonAlias`, `group.GroupAdministratorPersonAlias.Person`, `group.GroupMemberWorkflowTriggers`, `group.GroupRequirements`, `group.Attributes` (via `LoadAttributes`), `group.ChatChannelAvatarBinaryFile`, every `GroupLocation`'s `Location`, `GroupLocationTypeValue`, etc.

## Direct LINQ queries (outside of services)

- Line 641: `groupService.Queryable().Any(r => r.ParentGroupId == groupId)` — direct child existence check before showing archive dialog.
- Line 708: `groupService.Queryable().Where(g => g.ScheduleId == schedule.Id && g.Id != group.Id).Any()` — checks if another group still uses an inline non-named schedule before deleting it.
- Line 813: `group.GroupLocations.Where(l => !selectedLocations.Contains(l.Guid))` — in-memory filter of locations to remove.
- Line 832: `groupMemberAssignmentService.Queryable().Where(a => a.ScheduleId == schedule.Id && a.LocationId == groupLocation.LocationId && a.GroupMember.GroupId == groupLocation.GroupId)` — finds member assignments tied to a removed location/schedule.
- Line 854-867: similar in-memory filters for triggers and syncs.
- Line 911-913: same as 832 but for the location-changed branch.
- Line 1276: `GetAllowedGroupTypes(...).Select(t => t.Id).ToList()` — cross-validate parent group's allowed child types.
- Line 2117-2123: fetching group-member attributes by EntityType + qualifier.
- Line 2829: cross-checks group type's `Id == groupTypeIdFundraising || InheritedGroupTypeId == groupTypeIdFundraising` for the fundraising-progress hyperlink.
- Line 4544: `groupRequirementService.Queryable().Where(a => a.GroupTypeId.HasValue && a.GroupTypeId == CurrentGroupTypeId)` — type-level requirements for the read-only grid.
- Line 5069: `groupRequirementTypeService.Queryable().Where(grt => grt.Id == groupRequirementTypeId)` — used to drive due-date qualifier UI.
- Line 3168, 3185: inherited-attribute walk filtered by `EntityTypeQualifierColumn` and `EntityTypeQualifierValue`.

## Transactions (`WrapTransaction`)

- Save flow body (line 1309-1406) wraps Add (if new) -> SaveChanges (to get Id) -> AllowPerson -> AddRange GroupRequirements -> SaveAttributeValues -> diff/SaveAttributeEdits for member attributes -> SaveChanges -> cascade-inactivate child groups + SaveChanges -> chat avatar IsTemporary flip + SaveChanges. Reason cited inline: `// use WrapTransaction since SaveAttributeValues does its own RockContext.SaveChanges()`.
- `DeleteSecurityRoleGroup` ([GroupService.cs:2192](Rock/Model/Group/Group/GroupService.cs:2192)) wraps its bulk-delete + `Authorization.Clear` + `Delete` + `SaveChanges` block.
- `CopyGroup` ([GroupService.cs:1888](Rock/Model/Group/Group/GroupService.cs:1888)) wraps the entire clone-and-relate pipeline.

## Lazy loading reliance

The block intentionally relies on `Group`'s lazy navigation properties to walk the model:

| Spot | Navigation walk |
|---|---|
| line 1268 | `group.GroupType.ShowAdministrator` (post-Get) |
| line 1090 | `group.GroupType.IsPeerNetworkEnabled` |
| line 1120, 1123, 1134 | `group.GroupType.EnableRSVP`, `RSVPReminderOffsetDays`, `RSVPReminderSystemCommunicationId` |
| line 1433 | `group.GroupType.TakesAttendance` |
| line 2100-2103 | `group.ScheduleCoordinatorPersonAlias.Person` |
| line 2160-2163 | `group.GroupAdministratorPersonAlias.Person` |
| line 2278 | `group.Attributes` (added by `LoadAttributes`) |
| line 2693-2697 | `group.Campus.Name` |
| line 2823 | `groupType.GroupViewLavaTemplate` (already on cache) |
| line 813-841 | `group.GroupLocations[i].GroupLocationScheduleConfigs`, `Schedules` |
| line 854-866 | `group.GroupMemberWorkflowTriggers`, `group.GroupSyncs` |

## Authorization model (per entity)

| Action | Entity | Where checked |
|---|---|---|
| `VIEW` | Group | `ShowDetail`, line 1827 |
| `EDIT` | Group | `ShowDetail`, `btnDelete_Click` (line 686), `mdCopyGroup_SaveClick` (line 1526), `ArchiveSingleGroup` (line 3388), `ArchiveAllChildGroups` (line 3417), pre-save (line 1287). |
| `ADMINISTRATE` | Group | `ShowReadonlyDetails` Security button (line 2841), various panel visibility (line 2001-2004). |
| `ADMINISTRATE` | GroupType | View-mode hyperlink to GroupTypeDetail page (line 2674). Also `ShowGroupTypeEditDetails` (line 2195) for Sync panel visibility. |
| `EDIT` | Attribute | Group attribute editor's "edit values" subset (line 2281). |

## Options POCOs

- `Rock.Model.Groups.Group.Options.CopyGroupOptions` — `{ GroupId, IncludeChildGroups, CreatedByPersonAliasId }`.

## Inherited attributes pattern

Walks the GroupType inheritance chain via `inheritedGroupType.InheritedGroupTypeId`. For each ancestor, queries `Attribute` rows where `EntityTypeQualifierColumn = "GroupTypeId"` and `Value = thatAncestorId`. This populates two collections (line 3157-3203):

1. `GroupMemberAttributesInheritedState` (member attributes from ancestor group types) — read-only display.
2. `GroupDateAttributesState` (group attributes with FieldType `DATE` or `DATE_TIME`) — used by the requirement-due-date dropdown when the group has not yet been saved.

## Custom group-member attributes pattern

`Attribute` rows where `EntityTypeId = new GroupMember().TypeId`, `EntityTypeQualifierColumn = "GroupId"`, `Value = thisGroupId`. Persisted via `Rock.Attribute.Helper.SaveAttributeEdits`.

## Group-attribute values pattern

The `phGroupAttributes` DynamicPlaceholder hosts dynamically generated controls one per Attribute. Authoring is read via `Rock.Attribute.Helper.GetEditValues(phGroupAttributes, group)`. Save is via `group.SaveAttributeValues(rockContext)`. In Obsidian this maps to `AttributeValuesContainer` / `AttributeValuesEditor` patterns used in other detail blocks.

## Save flow side effects (cache invalidations)

- `Rock.Security.Authorization.Clear()` whenever security-role status flips in either direction (line 1410-1425).
- `GroupMemberWorkflowTriggerService.RemoveCachedTriggers()` whenever any trigger row was added/updated/deleted (line 1427-1430).
- `Rock.CheckIn.KioskDevice.Clear()` whenever check-in data was updated AND the group type takes attendance (line 1432-1436).

## SaveHook side effects (Group.SaveHook.cs)

The block does not call these directly, but every save passes through them and the conversion plan must be aware of them. See [Group.SaveHook.cs](Rock/Model/Group/Group/Group.SaveHook.cs):

- `PreSave` writes a `HistoryChangeList` for Name, Description, Group Type, Campus, Security Role, Active, Allow Guests, Public, Group Capacity, Archived.
- On Add with `IsActive == false` it stamps `InactiveDateTime`.
- On Modify, when `IsActive` flips, stamps/clears `InactiveDateTime`, and cascades the change to all Active/Pending `GroupMember`s (bulk update to Inactive when the group goes inactive) or fires a deferred re-validation pipeline when the group goes active again.
- On Modify, when `IsArchived` flips, stamps/clears `ArchivedDateTime`, and cascades to `GroupMember.IsArchived` (bulk update).
- On family-Group Campus change, sets `_FamilyCampusIsChanged = true`, which triggers `PersonService.UpdatePrimaryFamilyByGroup` in `PostSave` (downstream of the Obsidian conversion).
- On Delete: bulk-deletes `GroupMemberAssignment`, attendance records, attendance occurrences, group requirements (because cascade is disabled).
- `PostSave`: writes group history, kicks off chat sync (`SyncGroupsToChatProviderAsync`).
- `Group.UpdateCache` (in `Group.Logic.cs`): invalidates `GroupCache` and `RoleCache` per save.

## Cross-entity FK cascades

- Deleting a `GroupLocation` cascades to all `GroupLocationScheduleConfig` rows (handled in app code, not by FK), and to `GroupMemberAssignment` rows (also app-code).
- Deleting an inline non-named `Schedule` is conditional on no other group using it (`scheduleService.CanDelete`).
- Save hook handles GroupRequirement deletion when the entire Group is deleted.

## Open questions / flag for spec phase

- The unused `lava` variable at line 1770 (an `AddQuickReturn` Lava call) needs a decision: keep the side effect on Obsidian load, or drop. Likely kept and translated to a single block-action call that invokes the same Lava filter.
- `AuthService` and `AttributeQualifierService` are instantiated but never used in the save flow. These can be dropped from the conversion.
- `CategoryService` is instantiated but never used; same.
- `RockPage.SaveSharedItem`/`GetSharedItem` cannot be ported; the Obsidian block should rely on a single `Get(...)` per box assembly. Per the project memory ("No System.Web in Obsidian blocks"), do not shim this.
- The `GetSelect` overload signature is `T GetSelect<T>(int id, Expression<Func<TEntity, T>>)`. The call at line 1589 wraps the result in `(int?)` — confirm Obsidian conversion preserves this nullability.
- `KioskDevice.Clear()` is global; if Obsidian moves to a more granular cache invalidation API, this should be revisited.
