---
author: Maxwell Eley
date_created: 2026-05-06
summary: >-
  Phase 1 implementation spec for the GroupDetail Obsidian conversion. Delivers
  the block shell (C# class + GUID chop attributes), the full pure-Vue view
  panel (header chrome, two-column body, Overview card with Linkages,
  Group Tools card), the Audit Details modal, and the terminal actions
  (Delete / Archive / ArchiveWithChildren / Copy). Edit mode is intentionally
  a Phase 2 placeholder. No Save logic ships in this phase.
contributors: []
---

# Phase 1: Block shell + View panel + terminal actions

## Context

Phase 1 lands the foundation that every later phase builds on: a registered Obsidian block class that the Rock startup chop swaps in for the WebForms `GroupDetail.ascx` block, plus a complete view-mode experience. By the end of Phase 1 the block renders an existing group with full chrome and design fidelity, the audit details modal works, and Delete / Archive / Copy all execute the same backend semantics the WebForms block did. Edit mode opens a placeholder page that Phase 2 fills in. Architectural decisions for this phase are governed by [00-architecture.md](00-architecture.md), which is now locked.

## Behavior delivered

- Visiting `/Group/{N}` (integer Id) or `/Group/{idKey}` loads the group and renders the redesigned view panel: header (icon, name, GroupType chip, Campus chip, follow star, Audit kebab), subheader (relationship strength label, optional Public chip, conditional tags), two-column body (Overview card on left, Group Tools card on right), and footer (Edit, Cancel, optional Copy, Security).
- The Overview card renders Description, Group Administrator (linked), Parent Group (linked), Schedule, Group Capacity, Goal of Group, Neighborhood, Privacy, Group Preference, plus a Linkages section listing Registrations / Event Item Occurrences / Content Items pulled from the same EF queries the WebForms Lava read.
- The Group Tools card renders the eight quick links (Attendance, Scheduler, RSVP, Placement, Map, History, Fundraising, Plus any group-type-conditional items) under "Participation" / "Views" sub-headers, conditional on each LinkedPage block setting plus the corresponding GroupType flag.
- Clicking the Audit kebab opens an Audit Details modal showing Created By, Modified By, and Id in three horizontal columns (per Q7 Figma).
- Clicking Edit transitions the panel into edit mode and shows a Phase 2 placeholder. Cancel returns to view mode.
- Clicking Copy opens the Copy modal with the warning text and the "Include Child Groups" checkbox **unchecked by default** (flipped from WebForms). Copy uses `GroupService.CopyGroup` and navigates to the new group.
- Clicking Delete prompts for confirmation, runs the Delete auth check, runs `GroupService.CanDelete` (showing the error if it fails), deletes any orphaned non-named Schedule, and routes through `DeleteSecurityRoleGroup` for security role groups. Navigation honors `returnUrl` (now validated as same-origin) or falls back to `GroupListPage` with the parent group expanded.
- Clicking Archive (when GroupHistory is enabled and history rows exist) prompts for confirmation, then either archives the group alone or, when child groups exist, prompts whether to cascade to descendants.
- All outbound LinkedPage URLs from this block are written with IdKey (per Q4). The 5 still-WebForms destinations may emit broken links until Phase 7 ships; this is acknowledged.
- Quick-Return is registered for the loaded group via `addQuickReturn(name, "Groups", 20)` from `@Obsidian/Utility/page`.

## Behavior NOT delivered

- Edit form fields, validation, GroupType cascade, and the Save block action: **Phase 2**.
- Adding a new group from `/Group/0` (or via the GroupTreeView Add Root / Add Child paths): **Phase 2** (the Edit panel and Save body land together).
- Group attribute values render in the Overview card from data the bag already exposes; full attribute editing in the edit panel is **Phase 3**.
- Member attribute definitions panel: **Phase 3**.
- Group Requirements panel + modal: **Phase 4**.
- Group Sync panel + modal (including the Sync Frequency component restyle per Q9): **Phase 4**.
- Group Member Workflow Triggers panel + modal: **Phase 4**.
- Meeting Locations card with map cards (the per-`GroupLocation` 16:9 hover-to-expand design): **Phase 5**.
- Locations editing modal and inline schedule logic: **Phase 5**.
- Group Image hero region in the Overview card. The Vue partial wires up the conditional render in this phase, but the bag's `photoUrl` field is always null until Phase 2 ships the new `Group.PhotoId` column (per Q8). Until then the region always omits.
- Trailblazer-styled fields on the General section: **Phase 2** (per Q6 the only affected fields live in General).
- L2 (group-requirements duplicate-edit corruption): out of phase scope; tracked as a separate `/bugfix` spec per Q12.
- Updating the 5 still-WebForms outbound destinations to accept IdKey: **Phase 7** ("Update dependencies").

## Research coverage

The implementation session opens by reading these files in full (per SESSION-PROTOCOL.md Section A6) and audits implementation against them at session close (Section C2).

- [research/specs/00-architecture.md](00-architecture.md): always relevant; defines the block class, IdKey policy, GUID strategy, Q1-Q12 resolutions.
- [research/webforms/01-block-configuration.md](../webforms/01-block-configuration.md): all 21 block attributes (lines 21-46), page parameters (lines 49-58), security model (lines 61-67), block-type chop convention (lines 87-117). Phase 1 declares the block attributes and page parameters; the chop convention is implemented exactly per lines 95-114.
- [research/webforms/02-block-states.md](../webforms/02-block-states.md): high-level state machine (lines 6-41), authorization-driven states (lines 45-54), mode visibility matrix. Phase 1 owns view-mode states and the read-only / view-only branches; edit-mode field-controlled states are deferred.
- [research/webforms/03-markup-structure.md](../webforms/03-markup-structure.md): top-level layout (lines 8-35), View Panel (lines 82-103), Copy modal (lines 107-111), mdArchive modal (lines 159-164), mdDeleteWarning (lines 166-168). Phase 1 reproduces every view-mode markup region and the Copy / Archive / Delete-warning modals.
- [research/webforms/04-code-behind-walkthrough.md](../webforms/04-code-behind-walkthrough.md): lifecycle (lines 8-13), edit-lifecycle entry points (lines 18-29), `ShowDetail` / `ShowReadonlyDetails` (lines 84-98), `GetBreadCrumbs` (line 13). Phase 1 ports `ShowDetail` and `ShowReadonlyDetails` flows and the breadcrumb behavior. The Save flow and `ShowEditDetails` / `ShowGroupTypeEditDetails` are deferred to Phase 2.
- [research/webforms/16-archive-delete-copy.md](../webforms/16-archive-delete-copy.md): Delete (lines 5-80), Archive (lines 82-167), Copy (lines 169-273), `NavigateAfterDeleteOrArchive` (lines 275-301). Phase 1 ports all three terminal actions plus the navigation helper. The "Group Capacity" (lines 311-412) and "Signature Document Template" (lines 416-490) sections are Phase 2 (edit-panel scope) and are explicitly out of scope here.
- [research/webforms/17-view-panel.md](../webforms/17-view-panel.md): pure-Vue decision (lines 3-13), tag-list latent bug (lines 101-117), following helper (line 119-125), audit drawer (lines 127-133). Phase 1 implements the Vue view panel, drops the WebForms `lContent` Lava render, ports the tag list (with the L5 latent bug dropped per Q12), wires up Following via `<DetailBlock>` props, and replaces the audit drawer with the Audit Details modal.
- [research/webforms/18-cross-block-dependencies.md](../webforms/18-cross-block-dependencies.md): outbound destinations table (lines 12-28), IdKey acceptance status (lines 32-50), internal navigation (lines 54-67), return URL behavior (lines 70-83), inbound callers (lines 88-115). Phase 1 owns all outbound URL construction (writing IdKey uniformly per Q4) and all return-URL navigation paths, including the L6 same-origin validation per Q12.
- [research/webforms/27-misc-surfaces.md](../webforms/27-misc-surfaces.md): badges (lines 7-38), tags (lines 42-74), following (lines 78-99), audit drawer (lines 102-122), QuickReturn (lines 126-155), `HideSecondaryBlocks` (lines 186-213), `EditModeMessage` (lines 219-243), `ActionTitle` (lines 246-262), `<HighlightLabel>` (line 330), `<ModalAlert>` (line 331), `<SecurityButton>` (line 332). Phase 1 wires all of these into the Obsidian framework's `<DetailBlock>` template and supplies the C#-side calls (`addQuickReturn`, `EditModeMessage`, `ActionTitle`).
- [research/design/00-overview.md](../design/00-overview.md): page composition (top of file), open questions resolved by 00-architecture.md.
- [research/design/01-view-panel.md](../design/01-view-panel.md): full view-panel walkthrough (entire file). Phase 1 implements every region described.
- [research/design/03-net-new-features.md](../design/03-net-new-features.md): Phase 1-tagged items 2, 3, 4, 6, 7, 8, 9, 19, 20, 21, 22 (table on lines 5-28); behavior changes C1, C3 (lines 32-37); removed items R1, R2, R3, R4, R5 (lines 45-53). Phase 1 implements every Phase-1-tagged item.
- [research/design/06-mapping-to-webforms.md](../design/06-mapping-to-webforms.md): mapping table for Phase 1 surfaces (header chrome, view body, terminal actions).

## Implementation checklist

### A. Block class registration

1. Create `Rock.Blocks/Group/GroupDetail.cs` declaring `class GroupDetail : RockEntityDetailBlockType<Group, GroupBag>, IBreadCrumbBlock` per [00-architecture.md "Block class"](00-architecture.md). Decorations: `[DisplayName("Group Detail")]`, `[Category("Groups")]`, `[Description("Displays the details of the given group.")]`, `[IconCssClass("ti ti-users-group")]`, `[SupportedSiteTypes(Model.SiteType.Web)]`.
2. Generate the new `EntityTypeGuid` and the discarded "would-have-been" `BlockTypeGuid` via `node .claude/skills/convert-block/scripts/generate-guids.js` per [research/webforms/01-block-configuration.md:107-114](../webforms/01-block-configuration.md). Apply both as attributes; reuse the WebForms `BlockTypeGuid` `582BEEA1-5B27-444D-BC0A-F60CEB053981` per Q5 so `BlockTypeService.StagePossibleMigrateWebFormsToObsidianBlock` performs the chop at startup.
3. Per Q1, do **not** declare `[ContextAware(typeof(Group))]` and do **not** inherit `ContextEntityBlock`. The shell uses `RockEntityDetailBlockType` exclusively.
4. Implement `IBreadCrumbBlock.GetBreadCrumbs(PageReference pageReference)` mirroring the WebForms behavior at [GroupDetail.ascx.cs:592-615](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:592): return one breadcrumb whose name is `group.Name` for an existing group or `"New Group"` when `groupId == 0`. Resolve the group from the `GroupId` page parameter via `GroupService.Get(...)` accepting both integer Id and IdKey.
5. Override `GetInitialEntity()` using `GetInitialEntity<Group, GroupService>(rockContext, "GroupId")` so the block accepts both integer Id and IdKey forms uniformly per Q4 and the IdKey policy in [00-architecture.md "IdKey policy"](00-architecture.md).
6. Declare all 21 block attributes from [research/webforms/01-block-configuration.md:21-46](../webforms/01-block-configuration.md), preserving every `Key` constant value (no key renames). Group them into named attribute sections "General Settings" and "Page Routing" per design/03-net-new-features.md item #19; section assignments are: General Settings = `GroupTypes`, `GroupTypesExclude`, `LimittoSecurityRoleGroups`, `LimitToShowInNavigationGroupTypes`, `MapStyle`, `ShowCopyButton`, `ShowLocationAddresses`, `PreventSelectingInactiveCampus`, `EnableGroupTags`, `AddAdministrateSecurityToGroupCreator`. Page Routing = `GroupMapPage`, `AttendancePage`, `RegistrationInstancePage`, `EventItemOccurrencePage`, `ContentItemPage`, `GroupListPage`, `FundraisingProgressPage`, `GroupHistoryPage`, `GroupSchedulerPage`, `GroupRSVPPage`, `GroupPlacementPage`. (Per L5/Q12, do **not** declare a `TagCategory` attribute; the dead reference is dropped.)
7. Per L5/Q12, omit any code path that reads `GetAttributeValue("TagCategory")`. Tags render with no category filter (matches existing runtime behavior since `AsGuidOrNull()` was always returning null).

### B. ViewModels (bags) for Phase 1

8. Create `Rock.ViewModels/Blocks/Group/GroupDetail/GroupBag.cs` populated with the read-only / view-mode fields enumerated below in "Bag fields contributed". Phase 2 extends this bag.
9. Create `Rock.ViewModels/Blocks/Group/GroupDetail/GroupDetailOptionsBag.cs` carrying the eleven outbound URLs (built once per render via `GetLinkedPageUrl(...)` with `((Key))` placeholder), plus boolean visibility flags for each Group Tools row, plus the active GroupType's options scalar fields (per Q2 Approach B, only the current GroupType's options ship in the initial bag; cascade fetches `GetGroupTypeOptions` later).
10. Create `Rock.ViewModels/Blocks/Group/GroupDetail/CopyGroupRequestBag.cs` with `{ string Key, bool IncludeChildGroups }` to back the Copy modal.
11. Create supporting linkage bag types in the same folder: `GroupLinkageBag.cs` with `{ string Name, string Url }` and a `GroupLinkagesBag.cs` aggregate `{ List<GroupLinkageBag> Registrations, List<GroupLinkageBag> EventItemOccurrences, List<GroupLinkageBag> ContentItems }`. The aggregate hangs off `GroupBag.Linkages` (omitted from JSON when null).

### C. View panel rendering (Vue side)

12. Create `Rock.JavaScript.Obsidian.Blocks/src/Group/groupDetail.obs` as the top-level shell. It owns the `<DetailBlock>` props (`isAuditHidden`, `isBadgesVisible`, `isFollowVisible`, `isSecurityHidden`, `isTagsVisible`, `entityTypeGuid`, `entityKey`, `entityTypeName`), the panelMode state, and routes view / edit slots. Pattern reference: [Rock.JavaScript.Obsidian.Blocks/src/Group/groupTypeDetail.obs](Rock.JavaScript.Obsidian.Blocks/src/Group/groupTypeDetail.obs).
13. Create `Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/viewPanel.partial.obs` rendering the full design from [research/design/01-view-panel.md:10-127](../design/01-view-panel.md). Use `<ContentSection>` (per Q10) for the body's two-column wrapper; do not place the section inside a `<ContentSectionContainer>` because section nav is removed per design/03-net-new-features.md item R5.
14. Render the panel header per [design/01-view-panel.md:12-21](../design/01-view-panel.md): icon (`group.iconCssClass`), name, GroupType chip (linked to GroupTypeDetail page if `bag.canEditGroupType` is true), Campus chip (hidden when `bag.campusName` is empty), follow star (delegated to `<DetailBlock>` `isFollowVisible`), Audit kebab opening the audit modal.
15. Render the subheader per [design/01-view-panel.md:23-27](../design/01-view-panel.md): relationship strength label (using the renamed Casual / Close / Deep terms; underlying enum unchanged) hidden when group type lacks peer-network, conditional tags via `<DetailBlock>` `isTagsVisible`, conditional Public chip when `bag.isPublic` is true.
16. Render the Overview card per [design/01-view-panel.md:40-59](../design/01-view-panel.md): Image hero region (16:9, only when `bag.photoUrl` is non-empty - always omits in Phase 1 per Q8), Description, Group Administrator (linked to person profile, hidden when null or when `groupType.ShowAdministrator == false`), Parent Group (linked, hidden when null), Schedule (`bag.scheduleFriendlyText`), Group Capacity (hidden when null), four group attribute fields (Goal / Neighborhood / Privacy / Group Preference) sourced from existing group attribute values, the optional Category section repeating the same four for nested attribute groups, and the Linkages section.
17. Render the Linkages section per [design/01-view-panel.md:62-69](../design/01-view-panel.md): three sub-lists (Registrations / Event Item Occurrences / Content Items). The whole section is omitted entirely when all three lists are empty. Each item renders as a labeled link.
18. Render the Group Tools card per [design/01-view-panel.md:73-86](../design/01-view-panel.md) with two sub-headers ("Participation": Attendance, Scheduler, RSVP, Placement; "Views": Interactive Map, History; Fundraising appears under Participation when the group type is fundraising). Each item is conditional on the matching `bag.options.is{Tool}Visible` boolean and uses the URL from `bag.options.{tool}Url` (already substituted with the entity IdKey on the C# side via `((Key))` replacement).
19. Render the footer per [design/01-view-panel.md:30-34](../design/01-view-panel.md): Edit (primary), Cancel, Copy (when `ShowCopyButton` and EDIT auth), Security (delegated to `<DetailBlock>` via `isSecurityHidden=false` when ADMINISTRATE auth).
20. Honor every state from [design/01-view-panel.md:108-126](../design/01-view-panel.md): Image / Administrator / Parent / Capacity / Tags hide rows when their data is null or feature flag is off; the compact alt state collapses Group Tools sub-headers but keeps the two-column shell.
21. Compute and emit `addQuickReturn(group.name, "Groups", 20)` from `@Obsidian/Utility/page` on view-mode mount per [research/webforms/27-misc-surfaces.md:126-156](../webforms/27-misc-surfaces.md). Skip for new groups.
22. Per L6/Q12, the `returnUrl` parameter the block reads is validated server-side before any navigation reads it; the Vue side simply renders the data the bag returns.

### D. Audit Details modal

23. Create `Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/auditModal.partial.obs` rendering the design from Q7: title "Audit Details", body with three horizontal columns (`Created By`: name + relative time; `Modified By`: name + relative time; `Id`: numeric `Group.Id`), footer with Cancel only.
24. Hook the audit modal to the panel-header kebab menu's single "Audit Details" entry (Q7 specifies no other kebab actions in this phase). Open via local `ref` state controlled in `groupDetail.obs`.
25. Bag fields needed: `auditCreatedBy`, `auditCreatedDateTime`, `auditModifiedBy`, `auditModifiedDateTime`, `id` (the integer). Compute the relative time client-side via the standard Obsidian relative-time formatter; full timestamp shown in tooltip.
26. Audit modal is hidden in Add mode (no entity to audit). Phase 1 does not exercise this branch since Add lands in Phase 2, but the conditional render is in place from the start.

### E. Block actions: Edit (placeholder)

27. Implement the `Edit` block action returning `ValidPropertiesBox<GroupBag>`. For Phase 1, the body returns a minimal "edit mode active" bag plus `securityGrantToken` and any always-needed identity fields; the full edit-mode bag arrives in Phase 2. The Vue side switches `panelMode` to `Edit` and shows a placeholder partial.
28. Create `Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/editPanel.partial.obs` as a Phase-2 stub: a `<NotificationBox>` reading "Edit panel is coming in Phase 2. Click Cancel to return." plus a Cancel button that calls back into the shell to return to view mode.
29. Cancel from the edit-mode placeholder transitions back to view mode without any block action call (mirrors WebForms `btnCancel_Click` edit branch at [GroupDetail.ascx.cs:1496-1499](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1496)).

### F. Block action: Delete

30. Implement the `Delete` block action porting [GroupDetail.ascx.cs:675-729](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:675) per [research/webforms/16-archive-delete-copy.md:5-80](../webforms/16-archive-delete-copy.md). Authorize EDIT first (return error message "You are not authorized to delete this group." on failure).
31. Call `groupService.CanDelete(group, out errorMessage, includeSecondaryRelatedEntities: true)`. Return the error string on failure.
32. Inline-schedule cleanup per [GroupDetail.ascx.cs:700-713](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:700): if `group.ScheduleId.HasValue`, load the schedule. If `schedule.ScheduleType != Named` and no other Group references it, delete the schedule.
33. If `group.IsSecurityRoleOrSecurityGroupType()`, call `GroupService.DeleteSecurityRoleGroup(group.Id)`. Otherwise call `groupService.Delete(group)`. SaveChanges.
34. Compute the redirect URL via the navigation helper described in checklist item 36; return that string from the block action so the Vue layer redirects.
35. Visibility: the Vue layer hides Delete when any of `bag.options.isDeleteHidden == true`, `bag.isSystem`, `!bag.canEdit`, `bag.isArchived`, or `bag.options.shouldShowArchiveInsteadOfDelete` (the latter mirrors the GroupHistory + history-rows-exist branch at [GroupDetail.ascx.cs:2580-2592](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2580)).
36. Implement the navigation helper porting `NavigateAfterDeleteOrArchive` from [GroupDetail.ascx.cs:735-761](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:735): if `returnUrl` page parameter is present **and passes the L6/Q12 same-origin validation**, return it. Otherwise build a query string `{ GroupId = parentGroupId-as-IdKey, ExpandedIds = pageParameter("ExpandedIds") }` and resolve `GroupListPage` (writing IdKey uniformly per Q4). Fall back to current page reload.

### G. Block actions: Archive and ArchiveWithChildren

37. Implement the `Archive` block action porting [GroupDetail.ascx.cs:636-648](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:636) per [research/webforms/16-archive-delete-copy.md:82-167](../webforms/16-archive-delete-copy.md). Authorize EDIT. If the group has child groups (`groupService.Queryable().Any(r => r.ParentGroupId == groupId)`), return a discriminator value indicating "needs child-archive prompt"; the Vue layer shows the prompt modal.
38. Implement the `ArchiveSingleGroup` path (no children, or "No" answer): `groupService.Archive(group, this.CurrentPersonAliasId, true); SaveChanges; navigate via the helper from item 36`.
39. Implement the `ArchiveWithChildren` block action: enumerate `groupService.GetAllDescendentGroups(group.Id, true)`, archive each child, then archive the parent, save, navigate per item 36.
40. Visibility: the Vue layer shows the Archive button when `bag.options.isArchiveVisible` is true (server side: `!group.IsSystem && !group.IsArchived && Authorization.EDIT && groupType.EnableGroupHistory && (groupHistorical.Any() || groupMemberHistorical.Any())` per [GroupDetail.ascx.cs:2588-2592](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2588)).
41. Per [research/webforms/16-archive-delete-copy.md:140-148](../webforms/16-archive-delete-copy.md), wire a confirmation prompt for the toolbar Archive button ("Are you sure you want to archive this group?"); the per-children prompt modal does not double-confirm.

### H. Block action: Copy

42. Implement the `Copy` block action porting [GroupDetail.ascx.cs:1517-1545](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1517) per [research/webforms/16-archive-delete-copy.md:169-273](../webforms/16-archive-delete-copy.md). Take `CopyGroupRequestBag { Key, IncludeChildGroups }`. Authorize EDIT (return "You are not authorized to copy the group" on failure).
43. Build `CopyGroupOptions { GroupId = group.Id, IncludeChildGroups = bag.IncludeChildGroups, CreatedByPersonAliasId = this.GetCurrentPerson()?.PrimaryAliasId }` and call the static `GroupService.CopyGroup(options)`.
44. Return a redirect URL pointing at the new group's detail page (using the new group's IdKey). Copy navigation does **not** honor `returnUrl` per [research/webforms/18-cross-block-dependencies.md:80](../webforms/18-cross-block-dependencies.md).
45. Create `Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/copyModal.partial.obs` rendering the design from [design/01-view-panel.md:128-140](../design/01-view-panel.md): title "Copy Group", body with the warning/notice text (verbatim from [GroupDetail.ascx:475-501](RockWeb/Blocks/Groups/GroupDetail.ascx:475)), `<CheckBox>` "Include Child Groups" defaulting to **false** (flipped from WebForms per design C1), Save + Cancel footer.

### I. Linkages bag population

46. Server side: in `GetEntityBagForView`, populate `bag.Linkages` only when at least one of the three lists has rows (otherwise leave null so the Vue layer omits the section).
47. Registrations: query `RegistrationInstance` where `LinkageGroup.Id == group.Id` (or the existing pattern the WebForms Lava used; reference [GroupDetail.ascx.cs:2729](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2729) and `RegistrationInstanceUrl(int)` helper at lines 2983-2988). For each, build `{ Name = ri.Name, Url = LinkedPageUrl with IdKey form }`.
48. Event Item Occurrences: query `EventItemOccurrence` linked through `EventItemOccurrenceGroupMap`. For each, `{ Name = occurrence.EventItem.Name, Url = LinkedPageUrl(EventItemOccurrencePage) with IdKey form }`. Reference helper at [GroupDetail.ascx.cs:2995-3000](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2995).
49. Content Items: query `ContentChannelItemAssociation` where `Group.Id == group.Id`. For each, `{ Name = item.Title, Url = LinkedPageUrl(ContentItemPage) with IdKey form }`. Reference helper at [GroupDetail.ascx.cs:3007-3012](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:3007).

### J. Cross-block IdKey-uniform writing

50. Per Q4, write IdKey uniformly for `GroupId` parameters on every outbound URL the block emits. The 11 destinations enumerated at [research/webforms/18-cross-block-dependencies.md:14-28](../webforms/18-cross-block-dependencies.md) all receive IdKey. The 5 still-WebForms destinations (GroupListPage, FundraisingProgressPage, GroupHistoryPage, GroupMapPage, GroupSchedulerPage) will produce broken links until Phase 7 ships - this is expected and documented.
51. Use the `((Key))` placeholder + client-side replacement pattern observed in [Rock.Blocks/Group/GroupPlacement.cs:457](Rock.Blocks/Group/GroupPlacement.cs:457) and replicated in `groupPlacement.obs`. The C# side emits URLs once with `((Key))`, the Vue side substitutes the entity's IdKey at render time.

### K. Latent-bug fixes scoped here (Q12)

52. **L5 drop**: Confirm no `TagCategory` block-attribute declaration exists on the new C# class and no code path reads `GetAttributeValue("TagCategory")`. Tag list renders with `CategoryGuid = null` (no filter), matching pre-existing runtime behavior.
53. **L6 fix-during**: Validate the `returnUrl` page parameter is same-origin before any `Response.Redirect`-equivalent in the block. Implement a private helper (e.g., `IsSafeReturnUrl(string url)`) that returns true only if (a) the URL is null / whitespace (treated as "no return URL"), or (b) the URL parses as a relative path (no scheme, no host), or (c) the URL's host matches the current request host. Reject with a fall-through to the default navigation otherwise. Apply in both `Delete` and `Archive` block actions (and in Phase 2 `Save` / `Cancel`). Reference: [research/webforms/18-cross-block-dependencies.md:81](../webforms/18-cross-block-dependencies.md).

### L. types.partial.ts

54. Create `Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/types.partial.ts` with `NavigationUrlKey` enum (covering the 11 outbound URL keys plus the new-group redirect key), `PanelMode` enum (`View` / `Edit`), and any view-only computed-formatter helpers shared across partials.

## Out-of-scope items

- Group attribute **value editing** in the edit panel: Phase 3 (Phase 1 reads existing values for read-only render in the Overview card; it does not save).
- Member attribute definitions panel: Phase 3.
- Group Requirements editing modal and panel: Phase 4.
- Group Sync editing modal and panel: Phase 4.
- Group Member Workflow Triggers editing modal and panel: Phase 4.
- Inline schedule entity management on Save: Phase 2 (Save) for the scalar fields; Phase 5 for the per-location schedule cleanup.
- The map-card design for Meeting Locations: Phase 5.
- The new `Group.PhotoId` column migration + nav property + codegen regen: Phase 2 (per Q8).
- Updating the 5 still-WebForms outbound destinations to accept IdKey: Phase 7.
- L1 (duplicate code block in `ShowGroupTypeEditDetails`): Phase 2.
- L2 (group-requirements duplicate-edit corruption): separate `/bugfix` spec.
- L3 (hard-coded `EntityTypeId=15`): Phase 4.
- L4 (XSS hole in `FormatTriggerType`): Phase 4.
- Trailblazer `trailBlazerField` styling on General-section fields: Phase 2.
- Final Save block action body: Phase 2 (Edit returns a placeholder bag; Phase 1 does not Save).

## Files to create / modify

### Rock.Blocks/Group/
- `GroupDetail.cs` (NEW)

### Rock.ViewModels/Blocks/Group/GroupDetail/
- `GroupBag.cs` (NEW)
- `GroupDetailOptionsBag.cs` (NEW)
- `CopyGroupRequestBag.cs` (NEW)
- `GroupLinkageBag.cs` (NEW)
- `GroupLinkagesBag.cs` (NEW)

### Rock.JavaScript.Obsidian.Blocks/src/Group/
- `groupDetail.obs` (NEW)

### Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/
- `viewPanel.partial.obs` (NEW)
- `editPanel.partial.obs` (NEW; Phase 2 stub)
- `auditModal.partial.obs` (NEW)
- `copyModal.partial.obs` (NEW)
- `types.partial.ts` (NEW)

### Rock/SystemGuid/
- No new entries. The active `BlockTypeGuid` reuses the WebForms one; the new `EntityTypeGuid` and discarded would-have-been GUID live as attribute values on `GroupDetail.cs` only.

### Auto-generated TS types (after build)
- `Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupBag.d.ts` and siblings (regenerated via `Rock.CodeGeneration` after the bags compile). Not authored by hand.

## Bag fields contributed

Phase 1 ships these fields on `GroupBag` (read-only / view-mode):

```typescript
interface GroupBag {
    // Identity
    idKey: string;
    id: number;                                  // for Audit modal display per Q7

    // Header chrome
    name: string;
    iconCssClass: string;                        // group type's icon
    groupTypeName: string;
    groupTypeIdKey: string | null;               // null when user lacks ADMINISTRATE on group type
    canEditGroupType: boolean;                   // drives whether GroupType chip is linked
    campusName: string | null;

    // Subheader
    isPublic: boolean;
    relationshipStrengthLabel: string | null;    // localized to renamed terms; null when group type lacks peer-network

    // Overview body
    photoUrl: string | null;                     // always null until Phase 2 ships Group.PhotoId
    description: string | null;
    administrator: { name: string, personIdKey: string } | null;
    parentGroup: { name: string, idKey: string } | null;
    scheduleFriendlyText: string | null;
    groupCapacity: number | null;
    groupGoal: string | null;                    // attribute value
    neighborhood: string | null;                 // attribute value
    privacy: string | null;                      // attribute value
    groupPreference: string | null;              // attribute value

    // Linkages
    linkages: GroupLinkagesBag | null;

    // Audit modal (Q7)
    auditCreatedBy: { name: string, personIdKey: string } | null;
    auditCreatedDateTime: string | null;         // ISO 8601
    auditModifiedBy: { name: string, personIdKey: string } | null;
    auditModifiedDateTime: string | null;        // ISO 8601

    // State
    isActive: boolean;
    isArchived: boolean;
    isSystem: boolean;
    canEdit: boolean;                            // EDIT auth
    canAdministrate: boolean;                    // ADMINISTRATE auth
}

interface GroupDetailOptionsBag {
    // Per-tool URLs (each null when block setting unset OR group type flag false)
    attendanceUrl: string | null;
    schedulerUrl: string | null;
    rsvpUrl: string | null;
    placementUrl: string | null;
    mapUrl: string | null;
    historyUrl: string | null;
    fundraisingUrl: string | null;

    // Visibility flags (for the design's conditional rendering)
    isAttendanceVisible: boolean;
    isSchedulerVisible: boolean;
    isRsvpVisible: boolean;
    isPlacementVisible: boolean;
    isMapVisible: boolean;
    isHistoryVisible: boolean;
    isFundraisingVisible: boolean;

    // Action visibility
    isCopyButtonShown: boolean;                  // ShowCopyButton block setting + EDIT
    isArchiveVisible: boolean;
    shouldShowArchiveInsteadOfDelete: boolean;   // GroupHistory + history rows
    isDeleteHidden: boolean;                     // computed from system/archived/auth/history

    // Tags
    isTagListShown: boolean;                     // EnableGroupTags block setting && GroupType.EnableGroupTag
}

interface GroupLinkagesBag {
    registrations: GroupLinkageBag[];
    eventItemOccurrences: GroupLinkageBag[];
    contentItems: GroupLinkageBag[];
}

interface GroupLinkageBag {
    name: string;
    url: string;                                  // pre-resolved with IdKey
}

interface CopyGroupRequestBag {
    key: string;
    includeChildGroups: boolean;                  // default false in the modal
}
```

Phase 2 extends `GroupBag` with edit-mode scalar fields, GroupType cascade options, peer-network overrides, RSVP / Scheduling / Chat sections, and the new `Group.PhotoId`-driven uploader fields.

## Block actions

| Action | Request | Returns | Notes |
|---|---|---|---|
| `Edit` | `{ key: string }` | `ValidPropertiesBox<GroupBag>` (placeholder for Phase 1) | Returns minimal bag plus `securityGrantToken`. Phase 2 fills in the full edit-mode bag. |
| `Delete` | `{ key: string }` | `string` (redirect URL) | Auth-checked, `CanDelete` validated, inline-schedule cleanup, `DeleteSecurityRoleGroup` branch, returnUrl honored if same-origin (per L6). |
| `Archive` | `{ key: string }` | `{ status: "single" \| "needsChildPrompt" \| "redirect", redirectUrl?: string }` | `single` means archived directly; `needsChildPrompt` tells the Vue layer to show the "archive children?" modal; `redirect` includes the post-archive URL. |
| `ArchiveWithChildren` | `{ key: string }` | `string` (redirect URL) | Cascades through `GetAllDescendentGroups`. |
| `Copy` | `CopyGroupRequestBag` | `string` (redirect URL to new group) | Auth-checked. Does NOT honor returnUrl (matches WebForms). |

`Save` is **not** implemented in Phase 1.

## Save action contributions

n/a for Phase 1. Save lands in Phase 2.

## Code patterns to follow

Reference patterns from [Rock.Blocks/Group/GroupTypeDetail.cs](Rock.Blocks/Group/GroupTypeDetail.cs) (the closest sibling block) and [Rock.JavaScript.Obsidian.Blocks/src/Group/groupTypeDetail.obs](Rock.JavaScript.Obsidian.Blocks/src/Group/groupTypeDetail.obs):

- `RockEntityDetailBlockType<TEntity, TBag>` overrides: `GetInitialEntity`, `GetEntityBagForView`, `TryGetEntityForEditAction`, `RenderRequest`. Phase 1 implements the first two and stubs the third.
- Common-bag construction split: `GetCommonEntityBag(group)` populates fields shared by view and edit; `GetEntityBagForView(group)` adds view-only fields; (Phase 2) `GetEntityBagForEdit(group)` adds edit-only fields.
- Use `((Key))` placeholder + client-side replacement for outbound URLs (pattern documented at [research/webforms/18-cross-block-dependencies.md:106](../webforms/18-cross-block-dependencies.md), implemented in `Rock.Blocks/Group/GroupPlacement.cs`).
- Use `<DetailBlock>` template for header chrome, badges, follow, security, audit. Pass `entityTypeGuid="<Group entity type GUID>"`, `entityKey="{group.idKey}"`, plus the visibility flag props per [research/webforms/27-misc-surfaces.md](../webforms/27-misc-surfaces.md).
- Use `<ContentSection>` (collapsible) and `<ContentStack>` (description + controls) and `<ConditionalWell>` (subdued bordered region) per Q10 / [00-architecture.md Q10](00-architecture.md). Do not place the section inside a `<ContentSectionContainer>` because section nav is removed.
- Use `RockDateTime` (not `DateTime`) on the C# side per CLAUDE.md.
- `addQuickReturn(name, "Groups", 20)` on view-mode mount per the financialBatchDetail pattern documented at [research/webforms/27-misc-surfaces.md:147-153](../webforms/27-misc-surfaces.md).

## Design references

- View panel main: [research/design/screenshots/view-panel-main.png](../design/screenshots/view-panel-main.png) (Figma frame `4859-15223`).
- View panel alt / compact state: [research/design/screenshots/view-panel-alt-state.png](../design/screenshots/view-panel-alt-state.png) (Figma frame `4854-11658`).
- Overview region detail: [research/design/screenshots/view-overview-section.png](../design/screenshots/view-overview-section.png).
- Group Tools card: [research/design/screenshots/view-tools-section.png](../design/screenshots/view-tools-section.png).
- Locations region (Phase 5 reference, included for context): [research/design/screenshots/view-locations-section.png](../design/screenshots/view-locations-section.png).
- Copy modal: [research/design/screenshots/view-modal-copy.png](../design/screenshots/view-modal-copy.png) (Figma frame `5017-62518`).
- Audit Details modal: per Q7 in 00-architecture.md (user-supplied screenshot, not yet captured under research/design/).

## Mid-phase decisions log

(empty; populated during implementation if scope clarification happens)

## Verification plan

Manual test scenarios for the user's review playbook (per SESSION-PROTOCOL.md Section D7):

1. Navigate to `/Group/{N}` for an existing non-archived group with EDIT auth. Confirm the panel header renders icon, name, GroupType chip, Campus chip (when set), follow star, audit kebab.
2. Click the audit kebab. Confirm the Audit Details modal opens with three columns (Created By, Modified By, Id) and that close / Cancel dismisses it.
3. Confirm the subheader shows the relationship-strength label using the new term ("Casual" / "Close" / "Deep") for groups whose group type has peer-network enabled, and is hidden otherwise.
4. Confirm the Public chip appears in the subheader for groups with `IsPublic = true` and is absent otherwise.
5. Confirm the Tags region renders only when both `EnableGroupTags` block setting AND `GroupType.EnableGroupTag` are true; tag values can be added.
6. Confirm the Overview card renders Description, Group Administrator (linked, hidden when null or `ShowAdministrator == false`), Parent Group (linked, hidden when null), Schedule, Capacity, and the four group attribute values where present.
7. Confirm the Linkages section renders only when at least one linkage exists. Click each link; verify it navigates with IdKey for `GroupId`-style query params.
8. Confirm the Group Tools card renders only the items whose URL **and** group-type flag both apply. Items group under "Participation" / "Views" sub-headers per design.
9. Click each Group Tools item: Attendance, Scheduler, RSVP, Placement, Map, History, Fundraising. The 3 Obsidian destinations (Attendance, RSVP, Placement) load. The 5 WebForms destinations (List, Fundraising, History, Map, Scheduler) emit broken links because they do not yet accept IdKey - confirm this is the case (Phase 7 will fix). Document the observation rather than treating it as a bug.
10. Click Edit. Confirm the panel transitions to edit mode with the Phase 2 stub. Click Cancel. Confirm view mode returns.
11. Click Copy. Confirm the Copy modal renders with the warning text and the "Include Child Groups" checkbox **unchecked**. Submit; confirm the new group's detail page loads with the copied data.
12. Click Delete on a group with no GroupHistory. Confirm the confirmation dialog. Confirm Cancel dismisses; OK deletes and redirects per `NavigateAfterDeleteOrArchive` (parent group selected on GroupListPage if configured, else current page reload).
13. Try `/Group/{N}?returnUrl=/some/internal/path`: after Delete, confirm the redirect honors the same-origin URL.
14. Try `/Group/{N}?returnUrl=https://attacker.example/x`: after Delete, confirm the redirect ignores the unsafe `returnUrl` and falls through to default navigation (per L6 same-origin validation).
15. On a group whose GroupType has `EnableGroupHistory` and which has at least one history row, confirm Archive replaces Delete in the toolbar. Click Archive. With no children, confirm the single-archive flow. With children, confirm the "archive children?" prompt; verify both Yes and No paths.
16. Try `/Group/{idKey}` (IdKey form): confirm the same group loads as the integer-Id form.
17. Confirm the breadcrumb shows the group name (or "New Group" for `groupId == 0`, but Add is Phase 2 so this state is not reachable through the UI yet).
18. Open a previously-converted Obsidian block that links to `/Group/{groupIdKey}` (e.g., from the DataView Detail block per [18-cross-block-dependencies.md inbound caller #1](../webforms/18-cross-block-dependencies.md)). Confirm GroupDetail loads and renders correctly under IdKey.
19. Confirm `addQuickReturn` registers the group in the user's Quick Return menu under "Groups" with order 20.
20. Confirm `<DetailBlock>` framework features work end-to-end: Following toggle creates / removes the follow row; Security button (when ADMINISTRATE) opens the security modal; Badges render for the entity type. (These are framework-provided; Phase 1 just sets the props correctly.)

## Self-review coverage report

(initially empty; populated by the implementing model during SESSION-PROTOCOL.md Section C)

| Research file | Behavior | Status | Code ref | Notes |
|---|---|---|---|---|

## Completed

(initially empty; populated during SESSION-PROTOCOL.md Section D)
