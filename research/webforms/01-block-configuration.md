# Block Configuration

Source: `RockWeb/Blocks/Groups/GroupDetail.ascx.cs` lines 51-229.

## Class metadata

```csharp
[DisplayName( "Group Detail" )]
[Category( "Groups" )]
[Description( "Displays the details of the given group." )]
[ContextAware( typeof( Group ) )]
[Rock.Cms.DefaultBlockRole( Rock.Enums.Cms.BlockRole.Primary )]
[Rock.SystemGuid.BlockTypeGuid( "582BEEA1-5B27-444D-BC0A-F60CEB053981" )]
public partial class GroupDetail : ContextEntityBlock
```

Notes:
- `ContextEntityBlock` lets this block receive a `Group` from a context-providing block on the same page (Person profile pages, etc.) and skip the URL parameter.
- The DefaultBlockRole `Primary` means it is the page's main content block.

## Block attributes (21 total)

| Order | Key | Type | Default | Purpose |
|---|---|---|---|---|
| 0 | `GroupTypes` | GroupTypesField | none | Whitelist of group types this block can edit. Empty = all (subject to exclude). |
| 1 | `GroupTypesExclude` | GroupTypesField | none | Blacklist (only used if include is empty). |
| 2 | `LimittoSecurityRoleGroups` | Boolean | false | Force the block to only show / create security-role groups. Disables the IsSecurityRole checkbox. |
| 3 | `LimitToShowInNavigationGroupTypes` | Boolean | false | Filter group-type dropdown to types with `ShowInNavigation`. |
| 4 | `MapStyle` | DefinedValue (MAP_STYLES) | MAP_STYLE_ROCK | Map styling Lava merge field. |
| 5 | `GroupMapPage` | LinkedPage | none | "Interactive Map" quick-link target. |
| 6 | `AttendancePage` | LinkedPage | none | "Attendance" quick-link target. |
| 7 | `RegistrationInstancePage` | LinkedPage | none | Used inside the View panel Lava template. |
| 8 | `EventItemOccurrencePage` | LinkedPage | none | Used inside the View panel Lava template. |
| 9 | `ContentItemPage` | LinkedPage | none | Used inside the View panel Lava template. |
| 10 | `ShowCopyButton` | Boolean | false | Whether to render the Copy action. |
| 11 | `GroupListPage` | LinkedPage | none | Where to navigate after Cancel-on-Add or Delete (otherwise reload current page). |
| 12 | `FundraisingProgressPage` | LinkedPage | none | "Fundraising" quick-link, only shown for fundraising group types. |
| 13 | `ShowLocationAddresses` | Boolean | true | Lava merge flag for the View panel. |
| 14 | `PreventSelectingInactiveCampus` | Boolean | false | Filter campus picker to active only. |
| 15 | `GroupHistoryPage` | LinkedPage | none | "Group History" quick-link target. |
| 16 | `GroupSchedulerPage` | LinkedPage | `1815D8C6-...,D0F198E2-...` | "Group Scheduler" quick-link target. |
| 17 | `GroupRSVPPage` | LinkedPage | `Page.GROUP_RSVP_LIST` | "Group RSVP" quick-link target. |
| 18 | `EnableGroupTags` | Boolean | true | Show the tag list in view mode. |
| 19 | `AddAdministrateSecurityToGroupCreator` | Boolean | false | If true, group creator gets Administrate auth on the new group. |
| 20 | `GroupPlacementPage` | LinkedPage | none | "Group Placement" quick-link target (passes `SourceGroup`, `AllowMultiplePlacements=false`, `ReturnUrl`). |

The `IsScheduleTabVisible` key listed in `AttributeKey` is **not** an attribute but a ViewState marker (see line 314-317).

## Page parameters

| Key | Direction | Source | Notes |
|---|---|---|---|
| `GroupId` | in | URL | Primary parameter. `0` = add. Currently parsed as integer (`AsIntegerOrNull`). Conversion should accept idKey too. |
| `ParentGroupId` | in | URL | Used on Add to default ParentGroup, GroupType, and edit-auth check. |
| `ExpandedIds` | in/out | URL | Tree-view state passthrough for `GroupTreeView` block. Always echoed back on every navigation. |
| `EventItemOccurrenceId` | in (linked) | URL | Used only inside `EventItemOccurrenceUrl()` helper. |
| `autoEdit` | in | URL | If `true`, jumps directly to edit mode for an existing group. |
| `returnUrl` | in | URL | Override navigation after Save / Cancel / Delete / Archive. |

## Security model

- **View** authorization via `Authorization.VIEW`. Read-only details show even without edit.
- **Edit** authorization via `Authorization.EDIT`. Hides Edit/Delete/Archive/Copy buttons when missing.
- **Administrate** authorization gates: GroupSync panel, GroupRequirements panel, GroupMemberAttributes panel, Security button, and (for new groups only) granting Administrate to the group creator.
- IsSecurityRole checkbox visible only to members of `GROUP_ADMINISTRATORS` group (see line 2022).
- `IsSystem` groups disable IsChat* dropdowns and the Delete button (line 2545-2553, 2569).

## Field-level state stored in ViewState (WebForms)

These compose the entire in-progress edit and must be serialized into the bag for Obsidian:

| ViewState key | Type | Purpose |
|---|---|---|
| `LocationTypeTab` | string | Which tab in the Location modal is active. |
| `CurrentGroupTypeId` | int | Driver of all conditional visibility. |
| `GroupLocationsState` | List<GroupLocation> JSON | In-flight edits to group locations. |
| `GroupMemberAttributesInheritedState` | List<InheritedAttribute> JSON | Computed from group-type chain on init. |
| `GroupMemberAttributesState` | List<Attribute> JSON | Custom group-member attribute definitions for this group. |
| `GroupRequirementsState` | List<GroupRequirement> JSON | In-flight edits to group requirements. |
| `GroupDateAttributesState` | List<Attribute> JSON | Cached date attributes for due-date dropdowns. |
| `AllowMultipleLocations` | bool | Mirrors group type setting. Drives Locations grid Add button. |
| `GroupSyncState` | List<GroupSyncViewModel> JSON | In-flight edits to group sync rows. |
| `MemberWorkflowTriggersState` | List<GroupMemberWorkflowTrigger> JSON | In-flight edits to triggers. |
| `ScheduleCoordinatorNotificationTypes` | List<int> | Used to detect None-vs-other conflicts. |
| `IsScheduleTabVisible` | bool | Whether the schedule sub-panel under Meeting Details is shown. |

## Block type registration

WebForms block-type GUID: `582BEEA1-5B27-444D-BC0A-F60CEB053981`.

**No migration is written for the conversion.** Rock does the swap at startup.

`BlockTypeService.StagePossibleMigrateWebFormsToObsidianBlock` (called during `RegisterEntityBlockTypes`) finds existing WebForms BlockType rows whose Guid matches a new entity-based block class's `[BlockTypeGuid]` attribute, swaps the EntityType over to the new Obsidian entity, clears the Path column, and re-points every `Block` instance on every page to the now-Obsidian BlockType. All block-instance attribute values stay attached because the BlockType row's Id and Guid are preserved.

The new C# class for `GroupDetail` must therefore declare:

```csharp
[Rock.SystemGuid.EntityTypeGuid( "<NEW-GUID>" )]
// was [Rock.SystemGuid.BlockTypeGuid( "<WOULD-HAVE-BEEN-GUID>" )]
[Rock.SystemGuid.BlockTypeGuid( "582BEEA1-5B27-444D-BC0A-F60CEB053981" )]
public class GroupDetail : RockEntityDetailBlockType<Group, GroupBag>
```

- `EntityTypeGuid` is freshly generated for this new entity-typed block class.
- The active `[BlockTypeGuid]` reuses the WebForms block's existing GUID, which is what triggers the chop.
- The commented `// was [BlockTypeGuid(...)]` line preserves the discarded "would-have-been" GUID for traceability (in case a developer ever needs to reconstruct the conversion history).

Generate the new EntityTypeGuid and the discarded would-have-been BlockTypeGuid via:

```
node .claude/skills/convert-block/scripts/generate-guids.js
```

**Do not** call `AddOrUpdateEntityBlockType()` here. That helper creates a *second* BlockType row alongside the WebForms one and existing pages would not pick up the Obsidian block. It is correct for net-new blocks, wrong for conversions.

All 21 attribute keys must remain identical (string values in the `AttributeKey` constants) so existing block-instance attribute values continue to resolve.

---

## Block attribute deep dive

This section enumerates each block attribute's trigger conditions, behavior dependencies, defaults, downstream effects, and edge cases. All attribute key constants live at [GroupDetail.ascx.cs:205-229](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:205).

### 0. `GroupTypes` (Group Types Include): `GroupTypesField`

- Declared at [GroupDetail.ascx.cs:58-62](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:58). Default: empty.
- Storage shape: delimited list of GroupType GUIDs (`SplitDelimitedValues().AsGuidList()`).
- Used only in `GetAllowedGroupTypes` at [GroupDetail.ascx.cs:2948-2957](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2948).
- Trigger: every time the `ddlGroupType` dropdown is bound (Add path, parent-group change, type change).
- Effect: when non-empty, restricts the Group Type picker to only those GUIDs. Excludes wins over includes only when includes is empty (else-if at line 2954).
- Edge case: a group already has a GroupTypeId not in the include list. The picker has no option matching the existing value, so on edit `ddlGroupType.SelectedValue` is null (existing edit path uses `lGroupType.Visible = group.Id != 0` so the literal label shows the actual type name regardless; only the underlying form-state assumes the picker can't change).
- Edge case: empty list means "no filter", same as ALL group types (subject to `GroupTypesExclude`).

### 1. `GroupTypesExclude` (`GroupTypesField`)

- Declared at [GroupDetail.ascx.cs:64-68](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:64). Default: empty.
- Used only in `GetAllowedGroupTypes` at [GroupDetail.ascx.cs:2954-2957](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2954).
- Trigger: only consulted when `GroupTypes` (include list) is empty (else-if).
- Effect: removes the listed types from the picker.
- Edge case: when both include and exclude are populated, exclude is silently ignored.

### 2. `LimittoSecurityRoleGroups` (`BooleanField`)

- Declared at [GroupDetail.ascx.cs:70-73](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:70). Default: false.
- Triggers/effects:
  - On Add path with no group type pre-selected, defaults to `GroupTypeCache.GetSecurityRoleGroupType()` instead of forcing the user to choose. [GroupDetail.ascx.cs:2030-2043](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2030).
  - In edit mode, disables and force-checks the IsSecurityRole checkbox. [GroupDetail.ascx.cs:2110-2114](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2110).
  - In `btnSave_Click`, force-overrides `group.IsSecurityRole = true` regardless of checkbox state. [GroupDetail.ascx.cs:1071-1074](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1071).
- Edge case: if security-role group type is missing from the system, falls back to `ddlGroupType.SelectedIndex = 0` (line 2041), which leaves an empty selection.
- Edge case: this attribute does NOT itself filter `GetAllowedGroupTypes`. A user could still pick a non-security-role group type from the dropdown unless `GroupTypes` is also constrained.

### 3. `LimitToShowInNavigationGroupTypes` (`BooleanField`)

- Declared at [GroupDetail.ascx.cs:75-78](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:75). Default: false.
- Used only in `GetAllowedGroupTypes` at [GroupDetail.ascx.cs:2970-2973](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2970).
- Effect: when true, restricts the Group Type picker to types where `ShowInNavigation = true`.
- Composes with `GroupTypes` and `GroupTypesExclude` (applied last; AND-combine).

### 4. `MapStyle` (`DefinedValueField`)

- Declared at [GroupDetail.ascx.cs:80-87](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:80). Default: `MAP_STYLE_ROCK`.
- Used in two places:
  - View-mode merge fields, passed to `groupType.GroupViewLavaTemplate`. Falls back to `MAP_STYLE_ROCK` if the value resolves to null. [GroupDetail.ascx.cs:2734-2740](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2734).
  - Locations modal: drives `locpGroupLocation.MapStyleValueGuid` for Address/Point/Polygon picker rendering. [GroupDetail.ascx.cs:3567](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:3567).
- Edge case: this is the entire reason View-mode rendering needs to know about MapStyle even though there's no map control in the markup; the Lava template itself decides what to do with it.

### 5. `GroupMapPage` (`LinkedPage`)

- Declared at [GroupDetail.ascx.cs:89-93](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:89). Default: none.
- Effects:
  - Renders the "Interactive Map" quick-link (hlMap) in view mode. Hidden if blank. [GroupDetail.ascx.cs:2742-2753](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2742).
  - Also passed into the Lava template as `GroupMapUrl` merge field for in-template usage. [GroupDetail.ascx.cs:2743](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2743).
- Query string params sent: `?GroupId=N`.

### 6. `AttendancePage` (`LinkedPage`)

- Declared at [GroupDetail.ascx.cs:95-99](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:95). Default: none.
- Trigger: rendered only when `groupType.TakesAttendance == true` AND a URL is set. [GroupDetail.ascx.cs:2724-2725](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2724).
- Edge case: visibility is gated by group-type setting only, not by Authorization. View users see the link; permission check happens on the destination page.

### 7. `RegistrationInstancePage` (`LinkedPage`)

- Declared at [GroupDetail.ascx.cs:101-105](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:101). Default: none.
- Used in two places:
  - Merge field `RegistrationInstancePage` passed to `GroupViewLavaTemplate`. Lava template resolves the URL itself. [GroupDetail.ascx.cs:2729](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2729).
  - Helper `RegistrationInstanceUrl(int)` builds `?RegistrationInstanceId=N` for the Lava template to call. [GroupDetail.ascx.cs:2983-2988](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2983).
- Note: `LinkedPageRoute` (route resolution only, no query string) is what's added to merge fields, not `LinkedPageUrl`.

### 8. `EventItemOccurrencePage` (`LinkedPage`)

- Declared at [GroupDetail.ascx.cs:107-111](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:107). Default: none.
- Same pattern as `RegistrationInstancePage`. Merge field added at [GroupDetail.ascx.cs:2730](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2730). Helper `EventItemOccurrenceUrl(int)` at [GroupDetail.ascx.cs:2995-3000](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2995).

### 9. `ContentItemPage` (`LinkedPage`)

- Declared at [GroupDetail.ascx.cs:113-117](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:113). Default: none.
- Same pattern. Merge field at [GroupDetail.ascx.cs:2731](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2731). Helper `ContentItemUrl(int)` at [GroupDetail.ascx.cs:3007-3012](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:3007).

### 10. `ShowCopyButton` (`BooleanField`)

- Declared at [GroupDetail.ascx.cs:119-123](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:119). Default: false.
- Two read points:
  - First non-postback `OnLoad` sets initial visibility on the LinkButton. [GroupDetail.ascx.cs:504](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:504).
  - `Block_BlockUpdated` (block-settings change at runtime) re-evaluates with auth check on the actual group. [GroupDetail.ascx.cs:1656](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1656).
- Edge case: the initial OnLoad path checks the attribute but NOT auth. `ShowReadonlyDetails` later runs `btnCopy.Visible = false` if `readOnly`, which overrides. So the effective visibility is `(ShowCopyButton && !readOnly && Authorization.EDIT)`. The first `OnLoad` line is wasted work because either `ShowReadonlyDetails` or `ShowEditDetails` will hide btnCopy implicitly.
- The Copy operation itself does its own auth check at [GroupDetail.ascx.cs:1526-1531](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1526) inside `mdCopyGroup_SaveClick`.

### 11. `GroupListPage` (`LinkedPage`)

- Declared at [GroupDetail.ascx.cs:125-129](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:125). Default: none.
- Effects (3 navigation paths):
  - **After Cancel on Add when no parent group**: navigates to `GroupListPage` if set, else current page. [GroupDetail.ascx.cs:1485-1492](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1485).
  - **After Delete or Archive**: navigates to `GroupListPage` if set, else current page (selecting parent if any). [GroupDetail.ascx.cs:753-760](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:753).
- Always overridden by `?returnUrl=` if set.
- Edge case: passes `ParentGroupId` and `ExpandedIds` query-string-style into the linked page so a tree-view list can re-expand to where you came from.

### 12. `FundraisingProgressPage` (`LinkedPage`)

- Declared at [GroupDetail.ascx.cs:131-135](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:131). Default: none.
- Trigger: shown only when (a) URL is set AND (b) the group's GroupTypeId is the FundraisingOpportunity group type OR a descendant via `InheritedGroupTypeId`. [GroupDetail.ascx.cs:2827-2839](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2827).
- Note: visibility query runs a database call every view-mode render: `Queryable().Where( a => a.Id == groupTypeIdFundraising || a.InheritedGroupTypeId == groupTypeIdFundraising )`. This could be cached in Obsidian.

### 13. `ShowLocationAddresses` (`BooleanField`)

- Declared at [GroupDetail.ascx.cs:137-141](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:137). Default: true.
- Used only as a Lava merge field for `groupType.GroupViewLavaTemplate` to decide whether to print address details. [GroupDetail.ascx.cs:2732](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2732).
- No code-side effect; passes through to template only.

### 14. `PreventSelectingInactiveCampus` (`BooleanField`)

- Declared at [GroupDetail.ascx.cs:143-147](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:143). Default: false.
- Used in `ShowEditDetails` at [GroupDetail.ascx.cs:2063](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2063):
  ```
  cpCampus.IncludeInactive = !GetAttributeValue( AttributeKey.PreventSelectingInactiveCampus ).AsBoolean();
  ```
- Inverted boolean: when block attribute is false, `IncludeInactive=true` (show inactive). When attribute is true, `IncludeInactive=false`.
- Edge case: an existing group with an inactive campus selected and this attribute set to true: the picker may not include the current campus in its options, but `cpCampus.SelectedCampusId` is still bound to the value. Display behavior depends on the Campus picker's handling of "selected but unavailable" cases.

### 15. `GroupHistoryPage` (`LinkedPage`)

- Declared at [GroupDetail.ascx.cs:149-153](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:149). Default: none.
- Two effects:
  - Renders the Group History quick-link in view mode, gated by `groupType.EnableGroupHistory` AND URL set. [GroupDetail.ascx.cs:2811-2819](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2811).
  - Also passed as `GroupHistoryUrl` merge field. [GroupDetail.ascx.cs:2810](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2810).

### 16. `GroupSchedulerPage` (`LinkedPage`)

- Declared at [GroupDetail.ascx.cs:155-160](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:155). Default: page Guid `1815D8C6-7C4A-4C05-A810-CF23BA937477` with route `D0F198E2-6111-4EC1-8D1D-55AC10E28D04`.
- Trigger: visibility gated by `groupType.IsSchedulingEnabled`. Disabled if `group.DisableScheduling`. [GroupDetail.ascx.cs:2766-2783](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2766).
- Edge case: when `group.DisableScheduling` is true, the link element is rendered visible but `Enabled = false` (so the user can see the affordance is intentional but not clickable).

### 17. `GroupRSVPPage` (`LinkedPage`)

- Declared at [GroupDetail.ascx.cs:162-167](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:162). Default: `Page.GROUP_RSVP_LIST` system Guid.
- Trigger: shown when URL set AND `groupType.EnableRSVP`. [GroupDetail.ascx.cs:2755-2764](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2755).

### 18. `EnableGroupTags` (`BooleanField`)

- Declared at [GroupDetail.ascx.cs:169-173](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:169). Default: true.
- Used in `OnLoad` at [GroupDetail.ascx.cs:545](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:545):
  ```
  taglGroupTags.Visible = GetAttributeValue( AttributeKey.EnableGroupTags ).AsBoolean() && group.GroupType.EnableGroupTag;
  ```
- Both block attribute AND group type setting must be true. The block attribute is the master kill switch; group type setting is the per-type override.
- Note: legacy attribute query `GetAttributeValue("TagCategory")` at line 543 reads an attribute key that is NOT declared in the AttributeKey class. This is a hidden / undocumented or block-instance-specific attribute that controls which tag category is shown. Conversion should preserve this lookup.

### 19. `AddAdministrateSecurityToGroupCreator` (`BooleanField`)

- Declared at [GroupDetail.ascx.cs:175-179](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:175). Default: false.
- Used only in the Save transaction when `adding` is true. [GroupDetail.ascx.cs:1324-1328](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1324):
  ```
  if ( adding && GetAttributeValue( AttributeKey.AddAdministrateSecurityToGroupCreator ).AsBoolean() )
  {
      Rock.Security.Authorization.AllowPerson( group, Authorization.ADMINISTRATE, this.CurrentPerson, rockContext );
  }
  ```
- Edge case: this is the ONLY mutation of `Auth` table from this block. It happens inside the `WrapTransaction`, so a transaction rollback also rolls it back.

### 20. `GroupPlacementPage` (`LinkedPage`)

- Declared at [GroupDetail.ascx.cs:181-185](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:181). Default: none.
- Trigger: shown when URL set AND `groupType != null`. Disabled if `group.DisableScheduling` (uses scheduling for the placement workflow). [GroupDetail.ascx.cs:2785-2807](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2785).
- Query string passed: `SourceGroup={Id}, AllowMultiplePlacements=false, ReturnUrl={current page URL}`.
- Note: the `SourceGroup` parameter is hardcoded to the integer Id, so on conversion to idKey-tolerant linking the Group Placement page must accept the Id form.

### Hidden/legacy attribute key

- `TagCategory`: used only at [GroupDetail.ascx.cs:543](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:543), retrieved as a Guid. This is NOT in the declared `AttributeKey` constants. Likely a block-instance attribute set elsewhere (e.g., via a deprecated attribute declaration that has since been removed, or set by a migration as a custom attribute on the BlockType). Conversion should preserve this lookup or document it as removed.

### Open questions / flag for spec phase

- `TagCategory` is read but never declared as an attribute. Spec needs to confirm whether it's a deprecated leftover, an undocumented site-specific override, or something to formally re-declare.
- `MapStyle` and `ShowLocationAddresses` are pure Lava-template merge inputs. If the View panel is being redesigned (per overview), these might move to a different surface.
- `ShowCopyButton` initial set in `OnLoad` doesn't include auth. Conversion should align this with the auth-aware version used in `Block_BlockUpdated`.
- `GroupSchedulerPage` and `GroupRSVPPage` have **system Guid defaults**. Conversion should preserve those defaults so existing instance attribute values can fall back to the same Rock-shipped pages.

---

## Page parameters: extraction code paths

All page parameters are read via `PageParameter(string key)`. This base helper canonicalizes against `Rock.Page.Request` and handles URL routing parameter names. URL-decoding happens within `PageParameter` itself.

### `GroupId` (PageParameterKey.GroupId = `"GroupId"`)

Read at:
- [GroupDetail.ascx.cs:497-499](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:497): first `OnLoad`. Parsed via `AsIntegerOrNull()`.
- [GroupDetail.ascx.cs:596](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:596): `GetBreadCrumbs` from `pageReference.Parameters` directly. Same parse.
- [GroupDetail.ascx.cs:1663](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1663): `Block_BlockUpdated`. Parsed via `AsInteger()`.
- [GroupDetail.ascx.cs:5044](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:5044): `cbIsSecurityRole_CheckedChanged`. Parsed via `AsIntegerOrNull()`.

Behavior:
- `null` or empty: `pnlDetails.Visible = false` (block disappears, no error). [GroupDetail.ascx.cs:511](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:511).
- `0` or invalid integer string: treated as Add path. The actual code at line 535 checks `groupId.HasValue && groupId.Value != 0`, so a `?GroupId=abc` (unparseable) collapses to 0 (no group displayed).
- Existing integer: looked up via `GetGroup(int)`. Group not found: `nbNotFoundOrArchived.Visible = true; pnlDetails.Visible = false`. [GroupDetail.ascx.cs:1762-1767](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1762).

URL-decoding edge cases:
- The current code only accepts integer GroupId. IdKey or Guid would silently fail to integer parse. Conversion to Obsidian must support all three identifier formats per Rock convention (`groupService.Get(key, !PageCache.Layout.Site.DisablePredictableIds)`).

### `ParentGroupId` (PageParameterKey.ParentGroupId = `"ParentGroupId"`)

Read at:
- [GroupDetail.ascx.cs:507](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:507): first `OnLoad`, passed as second arg to `ShowDetail(groupId, parentGroupId)`. Parsed via `AsIntegerOrNull()`.
- [GroupDetail.ascx.cs:1469](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1469): `btnCancel_Click` for Add path. Parsed via `AsIntegerOrNull()`.
- [GroupDetail.ascx.cs:1666](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1666): `Block_BlockUpdated`. Parsed via `AsIntegerOrNull()`.

Behavior on Add path (`groupId == 0` AND `parentGroupId.HasValue`):
1. Set `group.ParentGroup = parentGroupId`.
2. Get all `GetAllowedGroupTypes(parentGroupType)`.
3. For each candidate, set `group.GroupTypeId = candidate.Id` (mutating in place) and check `IsAuthorized(EDIT, CurrentPerson)`. Track `editAllowed = true` if any one passes.
4. If exactly one type is authorized, default it; if multiple, leave unset to force user to choose. [GroupDetail.ascx.cs:1782-1822](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1782).

Edge cases:
- `parentGroupId = 0` is treated as `0` (a real id) by `AsIntegerOrNull()`, which won't fall back to "no parent" via this helper. The downstream Get will return null, so it acts like no parent.
- The auth check loop calls `IsAuthorized` with the candidate group type. This relies on Authorization Inherited from group type. Side effect: `group.GroupType` is left bound to whichever type was last in the iteration (only matters for diagnostic).

### `ExpandedIds` (PageParameterKey.ExpandedIds = `"ExpandedIds"`)

Read at:
- [GroupDetail.ascx.cs:751](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:751): `NavigateAfterDeleteOrArchive`.
- [GroupDetail.ascx.cs:1447](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1447): Save success navigation.
- [GroupDetail.ascx.cs:1479](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1479): Cancel-on-Add navigation.
- [GroupDetail.ascx.cs:1543](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1543): Copy success navigation.

Pattern: read via `PageParameter` (raw string, may contain commas/encoded ids), store unmodified into `qryParams[ExpandedIds]`, navigate. Round-trip preserves whatever the originating tree-view block set.

Edge cases:
- The block never INTERPRETS the value, only echoes it back. URL-encoded delimited strings pass through untouched.
- If the URL had `ExpandedIds=1,2,3`, the round-tripped param is the same string; the receiving Group Tree View block re-parses.

### `EventItemOccurrenceId` (PageParameterKey.EventItemOccurrenceId = `"EventItemOccurrenceId"`)

Used only inside the helper `EventItemOccurrenceUrl(int eventItemOccurrenceId)` at [GroupDetail.ascx.cs:2998](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2998). Outputs the param into the `EventItemOccurrencePage` linked-page URL.

Note: the constant exists in `PageParameterKey` but the block does NOT consume an incoming `?EventItemOccurrenceId=` parameter. The constant is for OUTBOUND URLs only.

### `autoEdit` (PageParameterKey.AutoEdit = `"autoEdit"`)

Read at [GroupDetail.ascx.cs:1866](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1866) only. Parsed via `AsBoolean()` (so `true`, `True`, `1`, `yes` all pass; anything else is false).

Effect: when `group.Id > 0` and edit is allowed, `ShowEditDetails(group)` instead of `ShowReadonlyDetails(group)`.

Edge cases:
- Lowercase parameter name (`autoEdit`) is the Rock convention but URL parameters in WebForms are case-insensitive at lookup time.
- If user has VIEW only, `autoEdit=true` is ignored: the early-return at line 1853-1860 sets `readOnly=true` first.
- For new groups (`groupId=0`), `autoEdit` is always effectively true (the form always opens in edit mode for Add).

### `returnUrl` (PageParameterKey.ReturnUrl = `"returnUrl"`)

Read at:
- [GroupDetail.ascx.cs:737](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:737): after Delete or Archive.
- [GroupDetail.ascx.cs:1438](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1438): after Save.
- [GroupDetail.ascx.cs:1460](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1460): after Cancel.

Effect: if non-empty, calls `Response.Redirect(returnUrl)` and short-circuits the standard navigation. Used for "send the user back where they came from" workflows.

Edge cases:
- No URL validation. If `returnUrl=javascript:` is supplied, this would issue a JavaScript URL redirect (browsers usually block but it's an attack vector). Conversion should validate the URL's host or restrict to relative paths.
- URL-decoding: `PageParameter` decodes once. Double-encoded URLs would still need their consumer to decode.
- `returnUrl` overrides everything else (including the `GroupListPage` block attribute).

### Open questions / flag for spec phase

- Spec must decide whether to accept idKey/Guid in `GroupId` (and `ParentGroupId`). Memory rule: idKey is the canonical format in conversions; if a referrer block still uses integer Id, support both.
- `returnUrl` lacks URL safety validation in the WebForms version. Conversion should add scheme/host allow-listing.
- `TagCategory` block-attribute lookup at line 543 is undocumented. Spec phase needs to determine its provenance.
- Whether `ContextEntity()` (from `ContextEntityBlock` base) should be honored is unclear: see Context-Aware section below for the resolution.

---

## Security model: every check, every gate

There are exactly four authorization actions used: `VIEW`, `EDIT`, `ADMINISTRATE`, plus indirect membership checks against `GROUP_ADMINISTRATORS` and the security-role group type. This section enumerates every check.

### Block-level (RockBlock) checks

- [GroupDetail.ascx.cs:1755](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1755): `IsUserAuthorized(Authorization.EDIT)`. Block-level, before group is loaded. Initial gate; can be overridden true by per-group auth below.

### Per-group auth checks

Standard pattern: `group.IsAuthorized(Authorization.ACTION, CurrentPerson)`.

| Auth | Where | Purpose |
|---|---|---|
| `VIEW` | [GroupDetail.ascx.cs:1827](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1827) | Initial OR with editAllowed; sets `viewAllowed`. Only entire `pnlDetails` visibility depends on viewAllowed. |
| `EDIT` | [GroupDetail.ascx.cs:1755, 1802, 1828, 686, 1287, 1526, 1656, 2569, 2590, 3388, 3417](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1755) | Used at multiple points: initial block-level, parent-group iteration on Add, post-load auth, btnDelete_Click, save-time re-validation, copy auth, block-update copy auth, btnDelete visibility, btnArchive visibility, ArchiveSingleGroup, ArchiveAllChildGroups. |
| `ADMINISTRATE` | [GroupDetail.ascx.cs:1327, 2001, 2674, 2841](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1327) | Granting on Add, gating sync/requirements/member-attributes panels, type-detail link in view, Security button visibility. |

### What each gates exactly

- **VIEW (per-group):**
  - `pnlDetails.Visible = viewAllowed`. If false, the entire block is invisible. [GroupDetail.ascx.cs:1830](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1830).
- **EDIT (per-group):**
  - When false in `ShowDetail`: `readOnly = true`. Edit/Delete/Archive/Copy buttons hidden. [GroupDetail.ascx.cs:1853-1860](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1853).
  - In `ShowReadonlyDetails`: gates `btnDelete.Visible` and `btnArchive.Visible`. [GroupDetail.ascx.cs:2569, 2590](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2569).
  - In `btnDelete_Click`: re-checked, "not authorized" toast on fail. [GroupDetail.ascx.cs:686-690](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:686).
  - In `btnSave_Click`: re-checked AFTER scalar property assignment to catch group-type-changed scenarios. [GroupDetail.ascx.cs:1287-1291](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1287). Sets `nbNotAllowedToEdit.Visible = true`.
  - In `mdCopyGroup_SaveClick`: re-checked, message shown if denied. [GroupDetail.ascx.cs:1526-1531](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1526).
  - In `Block_BlockUpdated`: factored into `btnCopy.Visible`. [GroupDetail.ascx.cs:1656](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1656).
  - In `ArchiveSingleGroup` and `ArchiveAllChildGroups`: re-checked, modal dismissed and warning shown if denied. [GroupDetail.ascx.cs:3388, 3417](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:3388).
  - In Add path with `?ParentGroupId=`: tested per candidate group type to compute `editAllowed`. [GroupDetail.ascx.cs:1802](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1802).
- **ADMINISTRATE (per-group):**
  - In `ShowEditDetails`: gates wpGroupSync, wpGroupRequirements, wpGroupMemberAttributes panel visibility. [GroupDetail.ascx.cs:2001-2004](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2001).
  - In `ShowReadonlyDetails`: gates `btnSecurity.Visible`. [GroupDetail.ascx.cs:2841](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2841).
  - In Save transaction (Add path with `AddAdministrateSecurityToGroupCreator`): granted to current person. [GroupDetail.ascx.cs:1327](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1327).
- **ADMINISTRATE (per-grouptype):**
  - In `ShowReadonlyDetails`: hyperlinks the Type label to `GroupTypeDetail` page if user can administrate the type. [GroupDetail.ascx.cs:2674-2682](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2674).
  - In `ShowGroupTypeEditDetails`: factored into `wpGroupSync.Visible` re-check (only if not already visible). [GroupDetail.ascx.cs:2195](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2195).

### Membership-based checks (not Authorization.X)

- **GROUP_ADMINISTRATORS membership:** `groupService.GroupHasMember( new Guid( Rock.SystemGuid.Group.GROUP_ADMINISTRATORS ), CurrentUser.PersonId )` at [GroupDetail.ascx.cs:2022](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2022). Gates `cbIsSecurityRole.Visible`. The intent: only Rock platform admins can mark a group as a security role.
  - Edge case: if `LimittoSecurityRoleGroups` block attribute is true, the IsSecurityRole checkbox is force-enabled but disabled (line 2110-2114), bypassing the membership check.
- **Security-role group-type identity:** `groupType.Guid == Rock.SystemGuid.GroupType.GROUPTYPE_SECURITY_ROLE.AsGuid()` at [GroupDetail.ascx.cs:2214, 5057](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2214). When the group is OF this type, `pnlElevatedSecurity` is shown unconditionally even if `cbIsSecurityRole.Checked` is false.

### Authorized Campus

There is no `AUTHORIZED CAMPUS` action in this block. The only campus interaction is `cpCampus.IncludeInactive` and the `GroupsRequireCampus` fallback. Campus authorization is handled by the campus picker control itself (out of scope for this block).

### Order of auth precedence

For an existing group:

1. Block-level `IsUserAuthorized(EDIT)` runs first. Cheap, doesn't load the group.
2. After loading the group, per-group `EDIT` is OR-ed in (so a deny at block level can be overridden by a per-group allow, e.g., if `EntityType: Group` has a different rule).
3. `VIEW` defaults to OR with `editAllowed`. So edit access implies view access.
4. If neither, `pnlDetails.Visible = false`, block disappears.

For a new group (`groupId == 0` with `?ParentGroupId=`):

1. Block-level `IsUserAuthorized(EDIT)` first.
2. If parentGroupId is provided, iterate every authorized child group type and OR in `editAllowed` if any one passes.
3. If neither, the user is allowed to view (because editAllowed=false → readOnly=true → ShowReadonlyDetails on a brand new group; this is degenerate state and probably never happens in practice).

### Open questions / flag for spec phase

- Auth check ordering (block-level then per-group) means a person with `Block-level deny / per-group allow` ends up with edit. Confirm Obsidian conversion preserves this behavior and that bag-level resolution serializes the merged answer.
- `cbIsSecurityRole.Visible` membership check uses `CurrentUser.PersonId` rather than `CurrentPerson.Id`. The two should be equivalent for most users; impersonation cases may differ. The convention in newer code is `CurrentPerson.Id`.
- `AddAdministrateSecurityToGroupCreator` grants on Add only. Migration: existing pre-2020 groups still have these auth grants; new groups don't unless the attribute is enabled. Conversion preserves this semantic.

---

## ContextAware behavior

### Declaration

- `[ContextAware( typeof( Group ) )]` at [GroupDetail.ascx.cs:54](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:54).
- Inherits `ContextEntityBlock` at [GroupDetail.ascx.cs:191](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:191).

### Base class behavior

`ContextEntityBlock` (`Rock/Web/UI/ContextEntityBlock.cs`) sets `this.Entity = ContextEntity()` on `OnInit`, falling back to `ContextEntity<Person>()` if no group is in context.

### How GroupDetail uses Entity

Critically: **GroupDetail does NOT consume `this.Entity`**. The `Entity` property is set by the base class but never read in this block. The block exclusively uses the `?GroupId=` URL parameter via `PageParameter(PageParameterKey.GroupId)`.

This means:
- A page with a Group Context block but no `?GroupId=` URL parameter will show `pnlDetails.Visible = false` (no group displayed).
- A page with both `?GroupId=N` and a Group Context block: the URL parameter wins; the context entity is ignored.

### Why this matters for conversion

The `[ContextAware]` declaration is required for the block to APPEAR in the Context-Aware block list inside the page configuration UI, but it has no functional effect at runtime in this specific block. Removing the attribute would change the block's discoverability but not its behavior.

For the Obsidian conversion, the conventional pattern is:
- Honor `RequestContext.GetContextEntity<Group>()` as a fallback if `?GroupId=` is absent.
- This would be a **behavior change** but it would make the block actually use its declared context awareness.

### Open questions / flag for spec phase

- Decide whether the conversion should fix the latent bug (context-aware never reads context) or preserve the WebForms behavior of "URL parameter only".
- If preserving: the Obsidian block can still declare context-aware for discoverability.
- If fixing: confirm with the prompter, since this is a behavior change visible to admins who place the block on a Person profile page expecting it to auto-populate from a related Group context block.

---

## Breadcrumb behavior: `GetBreadCrumbs(PageReference)`

Defined at [GroupDetail.ascx.cs:592-615](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:592).

### Inputs

- `pageReference.Parameters[PageParameterKey.GroupId]`. Note: this reads from the page reference's parameters, NOT from the live `Request.QueryString`.

### Outputs

| Input scenario | Output |
|---|---|
| `?GroupId=` not set or non-integer | Empty list (no breadcrumb) |
| `?GroupId=0` | One crumb: `"New Group"` |
| `?GroupId=N` where group exists | One crumb: `group.Name` |
| `?GroupId=N` where group doesn't exist | One crumb: `"New Group"` |

### Code excerpt

```csharp
int? groupId = PageParameter( pageReference, PageParameterKey.GroupId ).AsIntegerOrNull();
if ( groupId != null )
{
    Group group = new GroupService( new RockContext() ).Get( groupId.Value );
    if ( group != null )
    {
        breadCrumbs.Add( new BreadCrumb( group.Name, pageReference ) );
    }
    else
    {
        breadCrumbs.Add( new BreadCrumb( "New Group", pageReference ) );
    }
}
```

### Edge cases

- The "New Group" fallback is shown for any non-zero GroupId where the group doesn't exist (deleted, archived). This is misleading; a deleted group breadcrumb should arguably differ from new.
- The breadcrumb always uses `group.Name` directly; no HTML encoding concerns are evident because BreadCrumb takes a string and renders it as text.
- The breadcrumb is reloaded on every page render (no caching). Each render does `groupService.Get(groupId)` separately from the OnLoad's `GetGroup(groupId)`.

### Open questions / flag for spec phase

- Conversion should consolidate breadcrumb resolution with the main group fetch to avoid the duplicate query.
- The `?GroupId=N where group doesn't exist → "New Group"` output is probably a bug. Spec phase decision: preserve as-is, or fix to "Group Not Found"?

---

## ValidationGroup family

WebForms validation groups partition validators so that submitting a modal doesn't trigger validation in the main form, and vice versa.

| ValidationGroup | Used by | Rendered controls | Save trigger |
|---|---|---|---|
| (none / default block group) | Main edit panel: `tbName`, `tbDescription`, `nbGroupCapacity`, `cpCampus`, validators inside DynamicPlaceholder for group attributes (passed `BlockValidationGroup` at line 2282), etc. | All required-fields on the main form | `btnSave_Click` |
| `vgCopyGroup` | `mdCopyGroup` modal (Copy dialog) | `cbCopyGroupIncludeChildGroups` only (no real validators; just the include-child-groups checkbox). | `mdCopyGroup_SaveClick` |
| `Trigger` | `dlgMemberWorkflowTriggers` modal | `tbTriggerName`, `wtpWorkflowType`, `ddlTriggerType`, plus optional from/to status/role dropdowns. | `dlgMemberWorkflowTriggers_SaveClick` |
| `Location` | `dlgLocations` modal | `ddlMember`, `locpGroupLocation`, `ddlLocationType`, `spSchedules`, capacity number boxes. | `dlgLocations_OkClick` |
| `GroupSyncSettings` | `mdGroupSyncSettings` modal | `dvipSyncDataView`, `ddlGroupRoles`, `ipScheduleIntervalMinutes`, `ddlWelcomeCommunication`, `ddlExitCommunication`, `cbCreateLoginDuringSync`. | `mdGroupSyncSettings_SaveClick` |
| `vg_GroupRequirement` | `mdGroupRequirement` modal | `ddlGroupRequirementType`, `dpDueDate`, `ddlDueDateGroupAttribute`. | `mdGroupRequirement_SaveClick` |
| `GroupMemberAttribute` | `dlgGroupMemberAttribute` modal | `edtGroupMemberAttributes` (which sets up its own validators internally). | `dlgGroupMemberAttribute_SaveClick` |

### Save button validation linkage

- `btnSave` (`runat="server"`) defaults to validating the page-level group (no `ValidationGroup` set in the markup at line 438). It triggers all default-group validators including the dynamic group-attribute placeholder.
- Each modal's save button (rendered by `Rock:ModalDialog`'s `OnSaveClick`) is wired to its declared `ValidationGroup`, so opening the modal and clicking save only validates the modal's controls.
- The `ValidationSummary` controls in each modal also have matching `ValidationGroup` so error rollups stay scoped.

### Special case: `BlockValidationGroup`

`BlockValidationGroup` (a property on `RockBlock`) is passed to `Helper.AddEditControls` at [GroupDetail.ascx.cs:2282](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2282). This is the validation group used for the dynamically-rendered group-attribute controls. It defaults to the block-instance's auto-generated validation group name (so block-attribute validators are submit-button scoped).

### Edge case

- `cvGroup` custom validator at line 1300 runs server-side AFTER `Page.IsValid` already passed. It captures cross-field rules from `group.IsValid` (e.g., DataAnnotation rules on the entity model) and writes the message back to `vsGroup` validation summary on next render.

### Open questions / flag for spec phase

- The Obsidian conversion uses bag-level validation; the WebForms validation-group concept maps poorly. Spec phase: each modal becomes its own bag with its own validation; main form has its own bag.

---

## Attribute qualifier behavior

Group-attribute and Group-Member-attribute definitions are stored in the `Attribute` table with `EntityTypeQualifierColumn` + `EntityTypeQualifierValue` constraints.

### Group attributes (rendered into `phGroupAttributes`)

- `EntityType` = `Group`. Standard `LoadAttributes()` resolves these via the chain `EntityTypeQualifierColumn = "GroupTypeId"` AND `EntityTypeQualifierValue = group.GroupTypeId` AND any inherited group types.
- These are persisted by `group.SaveAttributeValues(rockContext)` at [GroupDetail.ascx.cs:1336](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1336). The values land in `AttributeValue` keyed to the group's `Id`.
- Where read: at [GroupDetail.ascx.cs:1260-1261](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1260) for save, [GroupDetail.ascx.cs:2275-2292](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2275) for render via `Helper.AddEditControls`.

### Group member attributes (managed by the `wpGroupMemberAttributes` panel)

These are persistent ATTRIBUTE DEFINITION rows scoped to a single Group via:
```
EntityTypeId = EntityTypeCache.Get(typeof(GroupMember)).Id
EntityTypeQualifierColumn = "GroupId"
EntityTypeQualifierValue = group.Id.ToString()
```

Where written:
- Initial load: queried at [GroupDetail.ascx.cs:2117-2123](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2117). Filter: `EntityTypeId = GroupMember.TypeId AND Column="GroupId" AND Value=group.Id.ToString()`.
- Save: at [GroupDetail.ascx.cs:1339-1357](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1339):
  ```csharp
  var entityTypeId = EntityTypeCache.Get( typeof( GroupMember ) ).Id;
  string qualifierColumn = "GroupId";
  string qualifierValue = group.Id.ToString();

  var attributes = attributeService.GetByEntityTypeQualifier( entityTypeId, qualifierColumn, qualifierValue, true );
  // delete removed
  // SaveAttributeEdits per kept
  ```

### Inherited group-member attributes (from group type chain)

These are `Attribute` rows on `GroupMember` with `EntityTypeQualifierColumn = "GroupTypeId"`. Walked via `GroupTypeCache.InheritedGroupTypeId` chain. Read-only in this block.

Where read: `BindInheritedAttributes` at [GroupDetail.ascx.cs:3157-3203](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:3157):
```csharp
qualifierValue = inheritedGroupType.Id.ToString();
attributeService.GetByEntityTypeId( new GroupMember().TypeId, false ).AsQueryable()
    .Where( a => a.EntityTypeQualifierColumn.Equals( "GroupTypeId" ) && a.EntityTypeQualifierValue.Equals( qualifierValue ) )
```

### Group date-typed attributes (used for Requirement DueDate dropdown)

Same walk through inherited GroupTypeIds, but on `Group` entity type and filtered to `DateFieldTypeIds`. Read at [GroupDetail.ascx.cs:3185-3192](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:3185).

### Edge cases

- A new (unsaved) group has `group.Id = 0`. Group-member-attribute query at line 2117 returns rows where `Value = "0"`, which is essentially never (no rows). This means Adding a new group with custom member attributes requires the group to be saved first then re-edited. WebForms preserves the in-flight definitions in `GroupMemberAttributesState` (ViewState) and persists them on Save AFTER the group's Id is assigned (at line 1339, after `SaveChanges` for the group itself).
- If an attribute key collides with an inherited one, `ReservedKeyNames` catches it at [GroupDetail.ascx.cs:4409-4412](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:4409). Inherited keys are reserved AND existing custom keys (excluding the one being edited).

### Open questions / flag for spec phase

- `EntityTypeQualifierValue` is a string. Conversion bag should keep this consistent (all-int IDs as strings) so existing data layout is unchanged.
- Inherited-attributes query walks the cache chain on every render. Conversion should consider caching the resolved chain per `groupTypeId`.
