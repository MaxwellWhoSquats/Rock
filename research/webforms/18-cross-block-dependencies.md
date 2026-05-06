# Cross-Block Dependencies

This file documents the full navigation graph for `RockWeb/Blocks/Groups/GroupDetail.ascx.cs` (block-type GUID `582BEEA1-5B27-444D-BC0A-F60CEB053981`, page GUID `4E237286-B715-4109-A578-C1445EC02707` aka `Page.GROUP_VIEWER`, route `Group/{groupId}`, PageId 113).

The graph has three layers:
1. **Inbound callers** - blocks and templates that build a URL pointing at this block's page.
2. **Outbound destinations** - pages that this block builds links to via its 11 `LinkedPage` block attributes.
3. **Internal navigation** - same-page reload after Save / Cancel / Delete / Archive / Copy, plus breadcrumb / context behavior.

---

## Outbound: where GroupDetail navigates to

Each is a configured `LinkedPage` block attribute resolved at runtime via `LinkedPageUrl()` or `LinkedPageRoute()`. All construction happens in `ShowReadonlyDetails` ([GroupDetail.ascx.cs:2567-2843](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)).

| Block attribute | Quick-link control | Page parameters written | Construction site (file:line) |
|---|---|---|---|
| `AttendancePage` | `hlAttendance` | `GroupId={Id}` | [line 2725](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) |
| `RegistrationInstancePage` | (Lava merge) | `RegistrationInstanceId={Id}` (built inside `RegistrationInstanceUrl()`) | [line 2729](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs), helper at [line 2983-2988](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) |
| `EventItemOccurrencePage` | (Lava merge) | `EventItemOccurrenceId={Id}` (built inside `EventItemOccurrenceUrl()`) | [line 2730](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs), helper at [line 2995-3000](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) |
| `ContentItemPage` | (Lava merge) | `ContentItemId={Id}` (built inside `ContentItemUrl()`) | [line 2731](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs), helper at [line 3007-3012](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) |
| `GroupListPage` | (none, used after Cancel-on-Add or Delete/Archive) | `GroupId={parentGroupId}`, `ExpandedIds` | [NavigateAfterDeleteOrArchive at line 753-755](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs), [Cancel at line 1485-1487](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) |
| `FundraisingProgressPage` | `hlFundraisingProgress` | `GroupId={Id}` | [line 2827](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) |
| `GroupHistoryPage` | `hlGroupHistory` | `GroupId={Id}` | [line 2809](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) |
| `GroupMapPage` | `hlMap` | `GroupId={Id}` | [line 2742](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) |
| `GroupRSVPPage` | `hlGroupRSVP` | `GroupId={Id}` | [line 2755](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) |
| `GroupSchedulerPage` | `hlGroupScheduler` | `GroupId={Id}` | [line 2766](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) |
| `GroupPlacementPage` | `hlGroupPlacement` | `SourceGroup={Id}`, `AllowMultiplePlacements=false`, `ReturnUrl={current page url}` | [line 2785-2790](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) |

The `RegistrationInstancePage`, `EventItemOccurrencePage`, `ContentItemPage` URLs are emitted as plain *routes* (no parameters baked in) via `LinkedPageRoute(...)` at lines 2729-2731 and exposed to the View panel Lava template under those merge field keys. The Lava template (typically `groupType.GroupViewLavaTemplate`) is what concatenates the IDs onto the URL.

### Outbound destinations: IdKey acceptance status

When GroupDetail starts writing IdKeys (per the IdKey adoption decision in [01-block-configuration.md](01-block-configuration.md)), each receiving block must accept the new format on its corresponding page parameter, otherwise the link arrives with a value the block can't parse.

| LinkedPage attribute | Receiving block | Block path | Status today | IdKey acceptance |
|---|---|---|---|---|
| `AttendancePage` | Group Attendance List (Obsidian) | [Rock.Blocks/Group/GroupAttendanceList.cs](../../Rock.Blocks/Group/GroupAttendanceList.cs) | Obsidian | **Accepts Id, IdKey, and Guid.** [line 584-608](../../Rock.Blocks/Group/GroupAttendanceList.cs) - falls back through `Guid.TryParse` -> integer (gated on `DisablePredictableIds`) -> `IdHasher.Instance.GetId(...)`. |
| `RegistrationInstancePage` | RegistrationInstance detail | RegistrationInstance detail block (RockWeb/Blocks/Event/RegistrationInstanceDetail.ascx.cs) | WebForms | Pass-through only (param is `RegistrationInstanceId`, not GroupId). Not impacted by the IdKey decision for GroupDetail. |
| `EventItemOccurrencePage` | EventItemOccurrence detail | RockWeb/Blocks/Event/EventItemOccurrenceDetail.ascx.cs | WebForms | Pass-through only (param is `EventItemOccurrenceId`). Not impacted. |
| `ContentItemPage` | ContentChannelItem detail | RockWeb/Blocks/Cms/ContentChannelItemDetail.ascx.cs | WebForms | Pass-through only (param is `ContentItemId`). Not impacted. |
| `GroupListPage` | Group Tree View (sidebar) | [RockWeb/Blocks/Groups/GroupTreeView.ascx.cs:171,352](../../RockWeb/Blocks/Groups/GroupTreeView.ascx.cs) | WebForms | **Integer only.** `_groupId.AsInteger()` at line 352. Cancel/Delete/Archive that hands `parentGroupId` to GroupListPage will break if GroupDetail writes IdKey there. |
| `FundraisingProgressPage` | Fundraising Progress | [RockWeb/Blocks/Fundraising/FundraisingProgress.ascx.cs:70](../../RockWeb/Blocks/Fundraising/FundraisingProgress.ascx.cs) | WebForms | **Integer only.** `PageParameter("GroupId").AsIntegerOrNull()`. |
| `GroupHistoryPage` | Group History | [RockWeb/Blocks/Groups/GroupHistory.ascx.cs:75](../../RockWeb/Blocks/Groups/GroupHistory.ascx.cs) | WebForms | **Integer only.** `PageParameter("GroupId").AsIntegerOrNull()`. |
| `GroupMapPage` | Group Map | [RockWeb/Blocks/Groups/GroupMap.ascx.cs:740](../../RockWeb/Blocks/Groups/GroupMap.ascx.cs) | WebForms | **Integer only.** `PageParameter("GroupId").AsIntegerOrNull()`. |
| `GroupRSVPPage` | RSVP List (Obsidian) | [Rock.Blocks/Rsvp/RsvpList.cs:190-191](../../Rock.Blocks/Rsvp/RsvpList.cs) | Obsidian | **Accepts string** via `groupService.Get(groupId)` overload that parses Id, IdKey, or Guid. |
| `GroupSchedulerPage` | Group Scheduler | [RockWeb/Blocks/GroupScheduling/GroupScheduler.ascx.cs:456](../../RockWeb/Blocks/GroupScheduling/GroupScheduler.ascx.cs) | WebForms | **Integer only.** `PageParameter(PageParameterKey.GroupId).AsIntegerOrNull()`. |
| `GroupPlacementPage` | Group Placement (Obsidian) | [Rock.Blocks/Group/GroupPlacement.cs:184](../../Rock.Blocks/Group/GroupPlacement.cs) | Obsidian | **Accepts Id, IdKey, Guid** via `GetIdFromPageParameter` at [line 871-875](../../Rock.Blocks/Group/GroupPlacement.cs) (`IdHasher.Instance.GetId(...) ?? .AsIntegerOrNull()`). |

**Net effect**: 5 outbound destinations are still WebForms and integer-only (`GroupListPage`, `FundraisingProgressPage`, `GroupHistoryPage`, `GroupMapPage`, `GroupSchedulerPage`). When GroupDetail starts writing IdKeys, links to those five will break unless either (a) those destinations are updated to accept IdKey, or (b) GroupDetail special-cases them by emitting integer Id when targeting them. Per the existing memory `feedback_idkey_is_new_format.md`, the preferred path is to update the WebForms destinations to accept IdKey rather than regress.

---

## Internal (same-page) navigation

After Save, Cancel, Delete, Archive, or Copy, the block reloads the current page with new GroupId and preserves `ExpandedIds`. Each path also honors `returnUrl` if present. See "Return URL behavior" below for all sites.

| Action | Method | Lines | Behavior |
|---|---|---|---|
| Save (success) | `btnSave_Click` | [1438-1450](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) | If `returnUrl`, redirect; else `NavigateToPage(RockPage.Guid, { GroupId, ExpandedIds })`. |
| Cancel (Add) | `btnCancel_Click` | [1467-1493](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) | If `returnUrl`, redirect. Else if `ParentGroupId`, reload with `{GroupId=parentGroupId, ExpandedIds}` (treeview mode). Else if `GroupListPage` set, navigate to it. Else `NavigateToPage(RockPage.Guid, null)`. |
| Cancel (Edit) | `btnCancel_Click` | [1496-1499](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) | Returns to read-only details (no navigation). |
| Delete | `btnDelete_Click` -> `NavigateAfterDeleteOrArchive` | [675-761](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) | If `returnUrl`, redirect. Else build `{GroupId=parentGroupId, ExpandedIds}` and navigate to `GroupListPage` if set, otherwise reload current page. |
| Archive (no children) | `btnArchive_Click` -> `ArchiveSingleGroup` -> `NavigateAfterDeleteOrArchive` | [636-647, 3378-3402, 735-761](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) | Same path as Delete. |
| Archive (with children) | `mdArchive_*Click` -> `ArchiveSingleGroup` or `ArchiveAllChildGroups` -> `NavigateAfterDeleteOrArchive` | [655-668, 3378-3438](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) | Same path as Delete. |
| Copy | `mdCopyGroup_SaveClick` | [1517-1545](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) | Reload current page with new group's `GroupId` plus `ExpandedIds`. **Does not honor `returnUrl`.** |

---

## Return URL behavior

The `returnUrl` query parameter overrides default navigation in three places:

1. **After Save** ([line 1438-1442](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)): if non-empty, `Response.Redirect(returnUrl)` and skip default reload.
2. **After Cancel** ([line 1460-1464](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)): if non-empty, `Response.Redirect(returnUrl)` and skip the parent/GroupListPage fallback chain.
3. **After Delete or Archive** ([NavigateAfterDeleteOrArchive line 737-742](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)): if non-empty, `Response.Redirect(returnUrl)` and skip GroupListPage/parent reload.

Edge cases:
- Empty / whitespace -> falls through to default behavior (verified at line 1439, 1461, 1738 via `IsNotNullOrWhiteSpace`).
- Copy (`mdCopyGroup_SaveClick` line 1517-1545) does **not** consult `returnUrl`. Copy always reloads to the new group's detail page.
- The block does not validate that `returnUrl` is same-origin or otherwise safe - `Response.Redirect` is given the raw value. **This is an open redirect vector** if a hostile party can lure an admin to `/Group/{id}?returnUrl=https://attacker.example`. The receiving Obsidian block should either (a) preserve identical behavior to maintain back-compat, or (b) tighten by restricting to relative URLs or whitelisted hosts. Decision needed in spec phase.
- The block trusts `returnUrl` over `GroupListPage` and over the in-progress edit. This is the user-facing flow that callers like `GroupPlacement` rely on (it sets `returnUrl=current page url` so the user goes back where they came from).

---

## Inbound callers

Files that build a URL targeting the GroupDetail page. Each row documents the exact construction site and what query-string format it produces.

| # | Caller | File:line | Code excerpt | Param format | Extras | Will need IdKey update? |
|---|---|---|---|---|---|---|
| 1 | DataView Detail (group sync rendering) | [RockWeb/Blocks/Reporting/DataViewDetail.ascx.cs:1093-1098](../../RockWeb/Blocks/Reporting/DataViewDetail.ascx.cs) | `var groupDetailPageParameters = new Dictionary<string, string>() { { PageParameterKey.GroupId, groupSync.Group.IdKey } };` | **IdKey** | None | **Already writes IdKey.** Conversion must accept IdKey to not regress this caller. |
| 2 | Group Type Map (Lava + code-behind) | [RockWeb/Blocks/Groups/GroupTypeMap.ascx.cs:307-310](../../RockWeb/Blocks/Groups/GroupTypeMap.ascx.cs) | `groupPageParams.Add( "GroupId", group.GroupId.ToString() );` | **integer** | None | No (will be transparent once GroupDetail accepts integer). |
| 3 | Group Finder (row-click) | [RockWeb/Blocks/Groups/GroupFinder.ascx.cs:656](../../RockWeb/Blocks/Groups/GroupFinder.ascx.cs) | `NavigateToLinkedPage( AttributeKey.GroupDetailPage, "GroupId", e.RowKeyId )` | **integer** (RowKeyId is an int) | None | No (transparent). |
| 4 | Group Finder (Lava grid) | [RockWeb/Blocks/Groups/GroupFinder.ascx.cs:326](../../RockWeb/Blocks/Groups/GroupFinder.ascx.cs) | `<a href='{{ LinkedPages.GroupDetailPage }}?GroupId={{ Group.Id }}'>` (default Lava template) | **integer** | None | No (transparent). |
| 5 | RegistrationInstance Wait List | [RockWeb/Blocks/Event/RegistrationInstanceWaitList.ascx.cs:1235](../../RockWeb/Blocks/Event/RegistrationInstanceWaitList.ascx.cs) | `LinkedPageUrl( "GroupDetailPage", new Dictionary<string, string> { { "GroupId", groupMember.GroupId.ToString() } } );` | **integer** | None | No (transparent). |
| 6 | RegistrationInstance Registrant List | [RockWeb/Blocks/Event/RegistrationInstanceRegistrantList.ascx.cs:1840](../../RockWeb/Blocks/Event/RegistrationInstanceRegistrantList.ascx.cs) | `LinkedPageUrl( AttributeKey.GroupDetailPage, new Dictionary<string, string> { { "GroupId", groupMember.GroupId.ToString() } } );` | **integer** | None | No (transparent). |
| 7 | RegistrationInstance Group Placement (WebForms) | [RockWeb/Blocks/Event/RegistrationInstanceGroupPlacement.ascx.cs:497](../../RockWeb/Blocks/Event/RegistrationInstanceGroupPlacement.ascx.cs) | `hfGroupDetailUrl.Value = this.LinkedPageUrl( AttributeKey.GroupDetailPage );` | (URL stored, JS appends `?GroupId={Id}` client-side) | None | No (transparent - server-side just emits the base URL). |
| 8 | Registration Detail | [RockWeb/Blocks/Event/RegistrationDetail.ascx.cs:1736-1738](../../RockWeb/Blocks/Event/RegistrationDetail.ascx.cs) | `qryParams.Add( "GroupId", registration.Group.Id.ToString() );` then `LinkedPageUrl( "GroupDetailPage", qryParams )` | **integer** | None | No (transparent). |
| 9 | Event Item Occurrence Detail | [RockWeb/Blocks/Event/EventItemOccurrenceDetail.ascx.cs:431-432, 438-439](../../RockWeb/Blocks/Event/EventItemOccurrenceDetail.ascx.cs) | `qryParams = new Dictionary<string, string> { { "GroupId", linkage.Group.Id.ToString() } };` | **integer** | None | No (transparent). |
| 10 | Connection Request Detail (WebForms) | [RockWeb/Blocks/Connection/ConnectionRequestDetail.ascx.cs:2317-2319](../../RockWeb/Blocks/Connection/ConnectionRequestDetail.ascx.cs) | `qryParams.Add( "GroupId", connectionRequest.AssignedGroup.Id.ToString() );` then `LinkedPageUrl( "GroupDetailPage", qryParams );` | **integer** | None | No (transparent). |
| 11 | Check-in Group List (Lava block) | [RockWeb/Blocks/CheckIn/CheckinGroupList.ascx.cs:285](../../RockWeb/Blocks/CheckIn/CheckinGroupList.ascx.cs) | `groupPageParams.Add( "GroupId", group.Id.ToString() );` then `LinkedPageUrl( "GroupDetailPage", groupPageParams )` | **integer** | None | No (transparent). |
| 12 | Event Registration Wizard (final confirmation) | [RockWeb/Blocks/Event/EventRegistrationWizard.ascx.cs:876-878](../../RockWeb/Blocks/Event/EventRegistrationWizard.ascx.cs) | `qryGroup.Add( "GroupId", result.GroupId );` then `GetPageUrl( GetAttributeValue( AttributeKey.GroupViewerPage ), qryGroup );` | **integer** (result.GroupId is int.ToString) | None | No (transparent). Block-attribute key is `GroupViewerPage`, but it points at the same `GROUP_VIEWER` page. |
| 13 | Group Tree View (sidebar) - Add Root | [RockWeb/Blocks/Groups/GroupTreeView.ascx.cs:520-527](../../RockWeb/Blocks/Groups/GroupTreeView.ascx.cs) | `qryParams.Add( PageParameterKey.GroupId, 0.ToString() ); qryParams.Add( "ParentGroupId", hfRootGroupId.Value ); qryParams.Add( "ExpandedIds", hfInitialGroupParentIds.Value );` | **integer** ParentGroupId | `GroupId=0`, `ParentGroupId={Id}`, `ExpandedIds={csv}` | No (Id=0 is not really an Id, just a "new group" sentinel). |
| 14 | Group Tree View (sidebar) - Add Child | [RockWeb/Blocks/Groups/GroupTreeView.ascx.cs:535-545](../../RockWeb/Blocks/Groups/GroupTreeView.ascx.cs) | Same as Add Root, with `ParentGroupId = groupId.ToString()`. | **integer** ParentGroupId | `GroupId=0`, `ParentGroupId={Id}`, `ExpandedIds={csv}` | No. |
| 15 | Group Placement (Obsidian) | [Rock.Blocks/Group/GroupPlacement.cs:457](../../Rock.Blocks/Group/GroupPlacement.cs) | `[NavigationUrlKey.GroupDetailPage] = this.GetLinkedPageUrl( AttributeKey.GroupDetailPage, new Dictionary<string, string> { ["GroupId"] = "((Key))", ["autoEdit"] = "true", ["returnUrl"] = this.GetCurrentPageUrl() } );` then client-side `.replace("((Key))", key.toString())` at [groupPlacement.obs:1265-1268](../../Rock.JavaScript.Obsidian.Blocks/src/Group/groupPlacement.obs) | **IdKey** (`((Key))` is replaced client-side with row's `idKey`) | `autoEdit=true`, `returnUrl={current url}` | **Already writes IdKey.** |
| 16 | RegistrationInstance Linkage List (Obsidian) | [Rock.Blocks/Event/RegistrationInstanceLinkageList.cs:189](../../Rock.Blocks/Event/RegistrationInstanceLinkageList.cs) | `[NavigationUrlKey.GroupDetailPage] = this.GetLinkedPageUrl( AttributeKey.GroupDetailPage, "GroupID", "((Key))" )` then client-side `.replace("((Key))", key)` at [registrationInstanceLinkageList.obs:198-200](../../Rock.JavaScript.Obsidian.Blocks/src/Event/registrationInstanceLinkageList.obs) | **IdKey** (note `GroupID` casing - see "Open questions") | None | **Already writes IdKey.** |
| 17 | Event Item Occurrence List (Obsidian) | [Rock.Blocks/Event/EventItemOccurrenceList.cs:136](../../Rock.Blocks/Event/EventItemOccurrenceList.cs) | `GroupDetailPageUrl = this.GetLinkedPageUrl( AttributeKey.GroupDetailPage )` (URL stored, then consumer constructs final URL) | (base URL only; client appends `?GroupId={Id}` per consumer) | (depends on consumer) | Verify what consumer appends. |
| 18 | Connections Hub (Obsidian) | [Rock.Blocks/Engagement/ConnectionsHub.cs:232](../../Rock.Blocks/Engagement/ConnectionsHub.cs) | `[NavigationUrlKey.GroupDetailPage] = this.GetLinkedPageUrl( AttributeKey.GroupDetailPage, "GroupId", "((Key))" )` then client-side `.replace("((Key))", key)` at [connectionRequestDockedPanel.partial.obs:597](../../Rock.JavaScript.Obsidian.Blocks/src/Engagement/ConnectionsHub/connectionRequestDockedPanel.partial.obs) | **IdKey** | None | **Already writes IdKey.** |
| 19 | Theme Lava: GroupListSidebar (4 themes) | [Rock.Frontend.Styles/src/themes/RockNextGen/Assets/Lava/GroupListSidebar.lava:43](../../Rock.Frontend.Styles/src/themes/RockNextGen/Assets/Lava/GroupListSidebar.lava), and identical files under `RockWeb/Themes/Stark`, `Flat`, `RockManager` | `<a href="{{ LinkedPages.DetailPage }}?GroupId={{group.Group.Id}}">` | **integer** | None | No (transparent). The Lava is rendered by `GroupListPersonalizedLava.ascx.cs` (its `DetailPage` attribute points at `GROUP_VIEWER`). |
| 20 | Theme Lava: GroupDetail.lava (4 themes) | [Rock.Frontend.Styles/src/themes/RockNextGen/Assets/Lava/GroupDetail.lava:110-120](../../Rock.Frontend.Styles/src/themes/RockNextGen/Assets/Lava/GroupDetail.lava) | These templates are rendered *by* GroupDetail itself (via `groupType.GroupViewLavaTemplate`) and reference `LinkedPages.RosterPage`, `AttendancePage`, etc. They emit *outbound* links from this block's view panel, not inbound. **Not a true inbound caller** - listed for completeness. | n/a | n/a | n/a |
| 21 | Mobile: SmartSearch | [Rock/Blocks/Types/Mobile/Core/SmartSearch.cs:181, 274, 413](../../Rock/Blocks/Types/Mobile/Core/SmartSearch.cs) | Block attribute named `GroupDetailPage`, but the linked page is the **mobile `GroupView` block** (block-type GUID `3F34AE03-9378-4363-A232-0318139C3BD3`), not the WebForms GroupDetail. Uses `GroupGuid` query param, not `GroupId`. | n/a | n/a | **Not an inbound caller** of WebForms GroupDetail. |
| 22 | Mobile: GroupEdit | [Rock/Blocks/Types/Mobile/Groups/GroupEdit.cs:160, 471, 659](../../Rock/Blocks/Types/Mobile/Groups/GroupEdit.cs) | Same situation as SmartSearch - the `GroupDetailPage` block-attribute on this Mobile block points at the Mobile `GroupView`, not WebForms GroupDetail. Uses `GroupGuid`. | n/a | n/a | **Not an inbound caller** of WebForms GroupDetail. |
| 23 | Mobile: ConnectionRequestDetail | [Rock/Blocks/Types/Mobile/Connection/ConnectionRequestDetail.cs:80, 164, 197](../../Rock/Blocks/Types/Mobile/Connection/ConnectionRequestDetail.cs) | Same - Mobile-only navigation to Mobile `GroupView`. | n/a | n/a | **Not an inbound caller** of WebForms GroupDetail. |
| 24 | EventItemOccurrenceListOptionsBag (data carrier) | [Rock.ViewModels/Blocks/Event/EventItemOccurrenceList/EventItemOccurrenceListOptionsBag.cs:42](../../Rock.ViewModels/Blocks/Event/EventItemOccurrenceList/EventItemOccurrenceListOptionsBag.cs) | `public string GroupDetailPageUrl { get; set; }` - passive carrier of the URL produced by `EventItemOccurrenceList.cs:136`. Not a navigation source itself. | n/a | n/a | n/a |

### Inbound caller summary

- 12 callers write integer Id today (rows 2-14 minus 1 IdKey writer = 11 plus 1 sentinel "0" for Add).
- 4 callers already write IdKey (rows 1, 15, 16, 18).
- 3 callers labeled "Mobile" (rows 21, 22, 23) point at a different block (Mobile `GroupView`) and are NOT inbound to WebForms GroupDetail - the existing research wrongly listed these as inbound.

This means the converted GroupDetail **must** accept both integer `GroupId` and IdKey `GroupId` to (a) avoid breaking the 11 integer-writers that won't be updated as part of this conversion, and (b) keep working with the 4 IdKey-writers that already exist.

---

## Group-tree navigation - the `ExpandedIds` protocol

GroupDetail is normally rendered alongside `GroupTreeView` (the sidebar tree) on the same page. The sidebar uses query string `?ExpandedIds=1,5,12` to remember which tree nodes the user had open. Every navigation out of GroupDetail must echo `ExpandedIds` back so the tree state survives the round-trip.

### Where ExpandedIds is read

- **GroupTreeView (the sidebar)** reads from a posted form value, not from query string: [GroupTreeView.ascx.cs:398](../../RockWeb/Blocks/Groups/GroupTreeView.ascx.cs) `string postedExpandedIds = this.Request.Params["ExpandedIds"];`. This is built by the jQuery `treeview` plugin client-side as the user expands nodes.
- **GroupDetail itself** reads `ExpandedIds` from the URL (via `PageParameter`) only to *echo it back* on every redirect - it never interprets the value.

### Where ExpandedIds is set (by GroupTreeView)

- **Add Root Group** ([line 525](../../RockWeb/Blocks/Groups/GroupTreeView.ascx.cs)): `qryParams.Add( "ExpandedIds", hfInitialGroupParentIds.Value );`
- **Add Child Group** ([line 542](../../RockWeb/Blocks/Groups/GroupTreeView.ascx.cs)): same.
- The hidden field `hfInitialGroupParentIds` is populated by the tree's own JS to capture the currently-open nodes.

### Where ExpandedIds is echoed (by GroupDetail)

| Path | Line | Code |
|---|---|---|
| Save -> reload | [1447](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) | `qryParams[PageParameterKey.ExpandedIds] = PageParameter( PageParameterKey.ExpandedIds );` |
| Cancel-on-Add (treeview mode) | [1479](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) | same |
| Delete or Archive -> NavigateAfterDeleteOrArchive | [751](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) | same |
| Copy -> reload | [1543](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) | same |

The protocol is purely pass-through. The Obsidian conversion must keep this pattern: read `ExpandedIds` from the URL on every navigation and echo it into the destination URL. The format (CSV of integer Ids) remains unchanged because the receiving block (GroupTreeView) is still WebForms.

---

## Direct-DOM navigation (inline JavaScript)

Most navigation runs through C# (`NavigateToPage`, `Response.Redirect`, `LinkedPageUrl`). One client-side flow exists:

### Archive button confirm dialog

[GroupDetail.ascx:783-790](../../RockWeb/Blocks/Groups/GroupDetail.ascx):

```javascript
$('.js-archive-group').on('click', function (e) {
    e.preventDefault();
    Rock.dialogs.confirm('Are you sure you want to archive this group?', function (result) {
        if (result) {
            window.location = e.target.href ? e.target.href : e.target.parentElement.href;
        }
    });
});
```

The `btnArchive` LinkButton ([line 455](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) carries the CssClass `js-archive-group`. Because it's an `<asp:LinkButton>`, ASP.NET emits an `href="javascript:__doPostBack(...)"`. The client-side handler intercepts the click, shows a confirm dialog, then if confirmed assigns `window.location = href` - which actually executes the postback handler `btnArchive_Click` on the server.

Note: `window.location = "javascript:..."` is unusual but works; the browser executes the JS. The Obsidian replacement should use a normal Vue confirm dialog before invoking the archive block-action, not preserve this trick.

No other client-side `window.location.href` or `location.href` mutations exist in `GroupDetail.ascx`.

### Inline `onclick` on Delete

[GroupDetail.ascx.cs:466](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs):

```csharp
btnDelete.Attributes["onclick"] = string.Format( "javascript: return Rock.dialogs.confirmDelete(event, '{0}');", Group.FriendlyTypeName );
```

A standard "Are you sure you want to delete?" confirm; on confirm the LinkButton's normal postback fires `btnDelete_Click`. Convert to a Vue confirm in Obsidian.

---

## Breadcrumb behavior - `GetBreadCrumbs(PageReference)`

[GroupDetail.ascx.cs:592-615](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs):

```csharp
public override List<BreadCrumb> GetBreadCrumbs( PageReference pageReference )
{
    var breadCrumbs = new List<BreadCrumb>();

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
    else
    {
        // don't show a breadcrumb if we don't have a pageparam to work with
    }

    return breadCrumbs;
}
```

Pattern notes:
- Uses the `PageParameter(pageReference, key)` overload - reads the parameter from the *given* PageReference (which may be the breadcrumb-build-time reference, not the live request) rather than the live `PageParameter(key)`. This matters because `GetBreadCrumbs` runs during page init and must work even if the navigation is not yet complete.
- Reads `GroupId` as integer only (`AsIntegerOrNull`). Will need to change to accept IdKey when the conversion lands.
- `groupId == null` (no parameter): returns an **empty** list (the comment explicitly says "don't show a breadcrumb").
- `groupId != null` but group not found: shows `"New Group"`. This is a quirky behavior - a request to view a deleted group `?GroupId=5` shows breadcrumb `"New Group"` even though the user is not on the Add path. The fall-through is also used for legitimate Add (`?GroupId=0`, since `Get(0)` returns null).
- Archived groups: `GroupService.Get` does include archived groups by default, so an archived group's name will show in the breadcrumb. The block's body separately renders `nbNotFoundOrArchived` if the user lacks permission to see archived groups.

Edge cases the Obsidian conversion needs to preserve:
- Empty list when no `GroupId` parameter (do NOT add a "Group Detail" placeholder).
- "New Group" text when group can't be loaded (covers both `0` and stale Ids).
- Use the *given* PageReference, not the live request.

---

## `Block_BlockUpdated` handler

[GroupDetail.ascx.cs:1653-1673](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs):

```csharp
protected void Block_BlockUpdated( object sender, EventArgs e )
{
    var currentGroup = GetGroup( hfGroupId.Value.AsInteger() );
    btnCopy.Visible = GetAttributeValue( AttributeKey.ShowCopyButton ).AsBoolean()
                      && currentGroup.IsAuthorized( Authorization.EDIT, CurrentPerson );
    if ( currentGroup != null )
    {
        ShowReadonlyDetails( currentGroup );
    }
    else
    {
        string groupId = PageParameter( PageParameterKey.GroupId );
        if ( !string.IsNullOrWhiteSpace( groupId ) )
        {
            ShowDetail( groupId.AsInteger(), PageParameter( PageParameterKey.ParentGroupId ).AsIntegerOrNull() );
        }
        else
        {
            pnlDetails.Visible = false;
        }
    }
}
```

When fires:
- After an admin saves new block-instance attribute settings (e.g., changes `GroupListPage`, toggles `ShowCopyButton`, etc.) via the block's gear-icon edit panel.
- The `BlockUpdated` event is wired in `OnInit` at [line 472-473](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs):
  ```csharp
  this.BlockUpdated += Block_BlockUpdated;
  this.AddConfigurationUpdateTrigger( upnlGroupDetail );
  ```
  The `AddConfigurationUpdateTrigger` on the UpdatePanel makes the block partial-postback when its config changes (no full page reload).

What it does:
- Re-resolves `currentGroup` from `hfGroupId` (the hidden form field that holds the active group's Id during view).
- Re-evaluates `btnCopy.Visible` (ShowCopyButton attribute may have flipped, and EDIT auth may have changed if SecurityRole was changed elsewhere).
- Calls `ShowReadonlyDetails(currentGroup)` to repaint the view panel - this re-resolves every `LinkedPageUrl` because if the admin just changed `AttendancePage`, the new URL needs to render.
- Falls back to `ShowDetail` from the URL if no group is loaded yet.

Obsidian equivalent: when block settings change, the Obsidian framework reloads the entire page (the gear-icon save triggers a full reload). The conversion does **not** need to preserve `Block_BlockUpdated` - Obsidian handles this case for free.

---

## Group context-aware behavior: `[ContextAware(typeof(Group))]`

The block declares:

```csharp
[ContextAware( typeof( Group ) )]
public partial class GroupDetail : ContextEntityBlock
```

[ContextEntityBlock at Rock/Web/UI/ContextEntityBlock.cs:28-52](../../Rock/Web/UI/ContextEntityBlock.cs) sets `this.Entity = ContextEntity()` automatically in `OnInit`. The base class falls back to `ContextEntity<Person>()` if no specific entity is found.

**Critical finding**: `GroupDetail.ascx.cs` *never reads* `this.Entity` or calls `ContextEntity<Group>()` directly. A grep on the entire 5,124-line file shows zero references to `this.Entity`, `Entity`, or `ContextEntity` outside the class declaration. This means:

- The `[ContextAware(typeof(Group))]` attribute and the `ContextEntityBlock` inheritance are **effectively dead code** in the WebForms version.
- The block always reads `?GroupId=N` from the URL and never honors a context-set Group.
- A page with a Group-context-providing block plus this GroupDetail block will not auto-populate GroupDetail from context - the user still needs `?GroupId=` in the URL.

Implications for conversion:
- **Drop the inheritance** in the new C# class. Use `RockEntityDetailBlockType<Group, GroupBag>` or similar Obsidian base.
- **Drop the `[ContextAware(typeof(Group))]` attribute** unless we explicitly want to add context support that doesn't exist today (out of scope for a conversion).
- The conversion is functionally equivalent without these.

This frees the conversion from the legacy `System.Web` dependency that `ContextEntityBlock` carries.

---

## Block-type chop concerns

No conversion migration is written. Rock chops the WebForms block to its Obsidian replacement at startup via `BlockTypeService.StagePossibleMigrateWebFormsToObsidianBlock`. Requirements for the chop to work cleanly:

1. The new C# class's active `[BlockTypeGuid]` MUST reuse the WebForms block's GUID `582BEEA1-5B27-444D-BC0A-F60CEB053981`.
2. The new class declares a freshly generated `[EntityTypeGuid]` and keeps the discarded would-have-been BlockTypeGuid in a `// was [Rock.SystemGuid.BlockTypeGuid("...")]` comment above the active attribute. See [01-block-configuration.md](01-block-configuration.md) for the exact pattern.
3. All 21 block-attribute keys (the string values inside `AttributeKey`) stay identical so existing block-instance attribute values continue to resolve.
4. Page parameter `GroupId` accepts both integer Id and IdKey forms.
5. The `ExpandedIds` parameter is preserved through every save/cancel/delete redirect.
6. The `returnUrl` parameter override is preserved.

---

## IdKey follow-on work (downstream blocks)

Per the chosen approach, `GroupDetail` will both **accept** and **write** IdKeys. Each block reachable through one of GroupDetail's outbound LinkedPage attributes must also accept IdKey form on its corresponding parameter, otherwise the link will arrive with a value its block can't parse.

Status table (verified above in "Outbound destinations: IdKey acceptance status"):

| LinkedPage attribute | Param GroupDetail will write | Receiving block | Status | Verdict |
|---|---|---|---|---|
| `AttendancePage` | `GroupId` | Rock.Blocks/Group/GroupAttendanceList.cs | Obsidian | **OK - accepts IdKey already.** |
| `RegistrationInstancePage` | `RegistrationInstanceId` | RegistrationInstanceDetail (WebForms) | n/a | Pass-through, not affected by GroupDetail's IdKey decision. |
| `EventItemOccurrencePage` | `EventItemOccurrenceId` | EventItemOccurrenceDetail (WebForms) | n/a | Pass-through, not affected. |
| `ContentItemPage` | `ContentItemId` | ContentChannelItemDetail (WebForms) | n/a | Pass-through, not affected. |
| `GroupListPage` | `GroupId` (parent), `ExpandedIds` | GroupTreeView (WebForms) | **needs update** | `_groupId.AsInteger()` at line 352. |
| `FundraisingProgressPage` | `GroupId` | FundraisingProgress (WebForms) | **needs update** | `AsIntegerOrNull` at line 70. |
| `GroupHistoryPage` | `GroupId` | GroupHistory (WebForms) | **needs update** | `AsIntegerOrNull` at line 75. |
| `GroupMapPage` | `GroupId` | GroupMap (WebForms) | **needs update** | `AsIntegerOrNull` at line 740. |
| `GroupRSVPPage` | `GroupId` | RsvpList (Obsidian) | OK | Accepts via `groupService.Get(string)` overload. |
| `GroupSchedulerPage` | `GroupId` | GroupScheduler (WebForms) | **needs update** | `AsIntegerOrNull` at line 456. |
| `GroupPlacementPage` | `SourceGroup` | GroupPlacement (Obsidian) | OK | Accepts via `GetIdFromPageParameter`. |

5 destinations need IdKey-acceptance fixes. The fixes are tiny (replace `.AsIntegerOrNull()` with `IdHasher.Instance.GetId(key) ?? key.AsIntegerOrNull()`). Decision needed in spec phase: do we (a) fix the 5 WebForms destinations as part of this effort, (b) ship GroupDetail writing integer when targeting those 5 destinations and IdKey otherwise, or (c) defer the entire IdKey-write decision until those destinations are themselves converted?

---

## Inbound caller IdKey writes (separate follow-on)

Most inbound callers (rows 2-14 above) continue to write integer GroupId. GroupDetail will accept both forms, so no immediate breakage. Eventually each inbound caller could be updated to write IdKey instead, but that is not blocking and is not part of this effort.

The 4 callers that already write IdKey (DataViewDetail, GroupPlacement, RegistrationInstanceLinkageList, ConnectionsHub) confirm that IdKey-acceptance on the converted GroupDetail is **non-negotiable** - without it those callers break.

---

## Page-parameter format expectations

| Param | Format expected | Notes |
|---|---|---|
| `GroupId` | int OR IdKey | Today: int. Conversion must accept both. Convention: write IdKey on outbound `((Key))` substitution patterns. |
| `ParentGroupId` | int | Today: int. Used only on Add. Conversion should accept IdKey for parity. |
| `ExpandedIds` | comma-delimited int list | Tree state. Pass-through only. Stays integer because GroupTreeView (the consumer) is still WebForms. |
| `EventItemOccurrenceId` | int | Pass-through only (referenced from `EventItemOccurrenceUrl()` helper). |
| `RegistrationInstanceId` | int | Pass-through only. |
| `ContentItemId` | int | Pass-through only. |
| `autoEdit` | bool | Today: `?autoEdit=true`. Used by GroupPlacement to deep-link straight into Edit. |
| `returnUrl` | string | Full URL. Honored on Save / Cancel / Delete / Archive (NOT Copy). |
| `SourceGroup` | int (outbound to GroupPlacement) | GroupPlacement accepts both Id and IdKey. |
| `AllowMultiplePlacements` | bool | Outbound to GroupPlacement. Always `false` from GroupDetail. |
| `ReturnUrl` | string | Outbound to GroupPlacement (note PascalCase). Set to current page URL. |

---

## Open questions / flag for spec phase

1. **`returnUrl` open-redirect risk.** The block currently `Response.Redirect`s to `returnUrl` without validating same-origin. Decide whether the Obsidian conversion preserves identical behavior (back-compat) or tightens (security improvement). Recommend preserving for now; track as a separate spec.
2. **5 WebForms destinations integer-only.** Decide whether to update `GroupTreeView`, `FundraisingProgress`, `GroupHistory`, `GroupMap`, `GroupSchedulerPage` to accept IdKey as part of this effort, defer to follow-on, or have GroupDetail emit integer Id when targeting them.
3. **Drop `[ContextAware(typeof(Group))]`?** It's dead code in the WebForms version. Confirm with team that no production page actually relies on context (a fresh scan would be needed). Recommend dropping as part of conversion to simplify the new block class.
4. **`returnUrl` parameter casing on outbound.** The block uses both `returnUrl` (lowercase, when receiving) and `ReturnUrl` (PascalCase, when writing to GroupPlacement at line 2789). The CLAUDE.md convention is PascalCase. The Obsidian conversion should standardize on `returnUrl` (lowercase) for inbound (preserving back-compat with callers like `GroupPlacement` Obsidian's `["returnUrl"] = ...` at line 457) and PascalCase only when targeting destinations that already expect it.
5. **`GroupID` (uppercase D) vs `GroupId` casing.** `RegistrationInstanceLinkageList.cs:189` writes `GroupID` (uppercase D), every other caller writes `GroupId`. The CLAUDE.md rule says page parameters are PascalCase (`GroupId`). The case is inconsistent in callers, but `PageParameter` is case-insensitive on lookup, so both work today. The conversion should not break either. Track separately whether the LinkageList writer is itself a typo to fix.
6. **Copy doesn't honor `returnUrl`.** Intentional? Or oversight? Decide whether the Obsidian Copy flow should honor `returnUrl` (treat copy as terminal action like Delete) or stay with current behavior (always go to new group's detail).
7. **Mobile blocks were wrongly listed as inbound.** The previous version of this file listed three Mobile blocks as inbound callers; in fact they target the Mobile `GroupView` block (separate block-type GUID), not WebForms GroupDetail. No action needed for the conversion, but this clarifies that no Mobile-side change is required.
