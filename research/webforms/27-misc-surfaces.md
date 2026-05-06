# Miscellaneous Server-Control Surfaces

Server controls and helpers used by `GroupDetail.ascx[.cs]` that surround the main edit/view panels: badges, tags, following, audit drawer, QuickReturn, secondary-block hiding, edit-mode messages, and a handful of one-off helpers. Each entry includes the WebForms surface area, the runtime behavior, and the Obsidian equivalent.

---

## Badges (BadgeListControl)

**Markup** ([`GroupDetail.ascx:45-47`](../../RockWeb/Blocks/Groups/GroupDetail.ascx)):

```aspx
<div class="panel-badges" id="divBadgeContainer" runat="server">
    <Rock:BadgeListControl ID="blBadgeList" runat="server" />
</div>
```

**Wire-up** ([`GroupDetail.ascx.cs:475-485`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)):

```csharp
// Add all of the badges for Group to the badge list control
var badgeCaches = BadgeCache.All( typeof( Group ) );

if ( badgeCaches.Any() )
{
    blBadgeList.BadgeTypes.AddRange( badgeCaches );
}
else
{
    divBadgeContainer.Visible = false;
}
```

**Behavior:**
- `BadgeCache.All( typeof( Group ) )` returns every `Badge` cache entry whose `EntityTypeId` resolves to the `Group` entity type.
- Each cache entry is added to `blBadgeList.BadgeTypes`. The `BadgeListControl` ([`Rock/Web/UI/Controls/Badges/BadgeListControl.cs:31-120`](../../Rock/Web/UI/Controls/Badges/BadgeListControl.cs)) iterates these in `Order`, filters by `Authorization.VIEW`, and instantiates a `BadgeControl` per badge type. Each `BadgeControl` runs the badge component's `Render` against the entity (set per-control elsewhere; the block currently leaves `Entity` unset so badges fall back to the `ContextEntityBlock`'s context awareness).
- If no badges exist for `Group`, the entire wrapper `<div id="divBadgeContainer">` is hidden via `Visible = false`. This is important: the `panel-badges` div has its own visual styling and should not render empty.

**Obsidian replacement:** The Obsidian `<DetailBlock>` template includes `BadgeList` from `@Obsidian/Controls/badgeList.obs` ([`Rock.JavaScript.Obsidian/Framework/Templates/detailBlock.ts:28`](../../Rock.JavaScript.Obsidian/Framework/Templates/detailBlock.ts)) controlled by an `isBadgesVisible` prop ([line 113-117](../../Rock.JavaScript.Obsidian/Framework/Templates/detailBlock.ts)). The block sets `isBadgesVisible="true"` and the framework loads the badges automatically based on the entity type guid. No need to enumerate `BadgeCache.All` server-side.

---

## Tags (TagList)

**Markup** ([`GroupDetail.ascx:444-447`](../../RockWeb/Blocks/Groups/GroupDetail.ascx)):

```aspx
<div class="taglist">
    <Rock:TagList ID="taglGroupTags" runat="server" CssClass="clearfix" />
</div>
```

**Wire-up** ([`GroupDetail.ascx.cs:541-545`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)):

```csharp
taglGroupTags.EntityTypeId = group.TypeId;
taglGroupTags.EntityGuid = group.Guid;
taglGroupTags.CategoryGuid = GetAttributeValue( "TagCategory" ).AsGuidOrNull();
taglGroupTags.GetTagValues( CurrentPersonId );
taglGroupTags.Visible = GetAttributeValue( AttributeKey.EnableGroupTags ).AsBoolean() && group.GroupType.EnableGroupTag;
```

**Behavior:**
- `TagList` extends `TextBox` and renders into a Bootstrap-tagsinput-style input. Definition: [`Rock/Web/UI/Controls/TagList.cs:36`](../../Rock/Web/UI/Controls/TagList.cs).
- `EntityTypeId = group.TypeId` (the Group entity type id, NOT the GroupType row's id) and `EntityGuid = group.Guid` scope the tag list to this specific group.
- `CategoryGuid` (optional) restricts the tag picker to a category. Sourced from `GetAttributeValue( "TagCategory" )`.
- `GetTagValues( CurrentPersonId )` ([`Rock/Web/UI/Controls/TagList.cs:255`](../../Rock/Web/UI/Controls/TagList.cs)) loads the existing tags for the entity and current person (so personal vs organization tags resolve correctly).
- `Visible` is the AND of two conditions: the block-level `EnableGroupTags` attribute AND the GroupType's own `EnableGroupTag` flag. Both must be true.

**Latent bug / vestigial attribute:**
- `GetAttributeValue( "TagCategory" )` is read at [`GroupDetail.ascx.cs:543`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) but `"TagCategory"` is **not declared** in the `AttributeKey` static class ([`GroupDetail.ascx.cs:205-228`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)) and there is no `[CategoryField( "TagCategory", ... )]` decorator on the block class ([lines 51-188](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)).
- Effect: the call returns `string.Empty` or `null`, so `AsGuidOrNull()` returns `null`, and `taglGroupTags.CategoryGuid` is left null (no category filter). This appears to be intentional or vestigial; the block was probably going to expose a "Tag Category" block setting but the attribute was never added.
- Action for spec phase: decide whether to add the block attribute officially (with key `TagCategory` to preserve any external references, or with a properly cased key as new), or remove the dead `GetAttributeValue` call.

**Obsidian replacement:** The Obsidian Detail Block's `isTagsVisible` prop ([`detailBlock.ts:101-105`](../../Rock.JavaScript.Obsidian/Framework/Templates/detailBlock.ts)) wires to the `<EntityTagList>` control automatically. The block resolves the visibility AND-condition (`EnableGroupTags && GroupType.EnableGroupTag`) server-side and passes the resulting boolean. If `TagCategory` becomes a real attribute, it can be passed through to the tag-list control as a category filter.

---

## Following (FollowingsHelper)

**Markup** ([`GroupDetail.ascx:39`](../../RockWeb/Blocks/Groups/GroupDetail.ascx)):

```aspx
<asp:Panel runat="server" ID="pnlFollowing" CssClass="panel-follow-status js-follow-status" data-toggle="tooltip" data-placement="top" title="Click to Follow"></asp:Panel>
```

**Wire-up** ([`GroupDetail.ascx.cs:547`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)):

```csharp
FollowingsHelper.SetFollowing( group, pnlFollowing, this.CurrentPerson );
```

**Behavior** (helper at [`Rock/Web/UI/Controls/FollowingsHelper.cs:30-133`](../../Rock/Web/UI/Controls/FollowingsHelper.cs)):
- Queries `FollowingService` for an existing follow row matching `EntityTypeId == group.TypeId`, `EntityId == group.Id`, `PersonAlias.PersonId == follower.Id`, `PurposeKey == ""`.
- Adds or removes the `following` CSS class on `pnlFollowing` based on whether a follow row exists.
- Hides the panel if `entityId == 0` (the group hasn't been saved yet).
- Registers a startup script per control: `Rock.controls.followingsToggler.initialize($('#<panelClientId>'), entityTypeId, entityId, purposeKey, followerPersonId, followerPrimaryAliasId, callback)`. This client-side toggler handles the click that creates or deletes the follow row via REST.

**Obsidian replacement:** `<DetailBlock>` exposes `isFollowVisible` ([`detailBlock.ts:107-111`](../../Rock.JavaScript.Obsidian/Framework/Templates/detailBlock.ts)). The internal `getEntityFollowedState` posts to `/api/v2/Controls/FollowingGetFollowing` ([line 570](../../Rock.JavaScript.Obsidian/Framework/Templates/detailBlock.ts)) and the toggle posts to `/api/v2/Controls/FollowingSetFollowing` ([line 803](../../Rock.JavaScript.Obsidian/Framework/Templates/detailBlock.ts)). The block sets `isFollowVisible="true"` and `entityKey` on the `<DetailBlock>` element. No server-side wire-up needed in the C# block.

---

## Audit drawer (PanelDrawer pdAuditDetails)

**Markup** ([`GroupDetail.ascx:43`](../../RockWeb/Blocks/Groups/GroupDetail.ascx)):

```aspx
<Rock:PanelDrawer ID="pdAuditDetails" runat="server"></Rock:PanelDrawer>
```

**Wire-up:**
- Read mode: `pdAuditDetails.SetEntity( group, ResolveRockUrl( "~" ) );` ([`GroupDetail.ascx.cs:2691`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)) inside `ShowReadonlyDetails`.
- Edit mode (Add only): `pdAuditDetails.Visible = false;` ([`GroupDetail.ascx.cs:1940`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)) inside `ShowEditDetails` when `group.Id == 0`.

**Behavior** (control at [`Rock/Web/UI/Controls/PanelDrawer.cs:31-220`](../../Rock/Web/UI/Controls/PanelDrawer.cs)):
- `SetEntity( IModel entity, string rootUrl )` ([line 169](../../Rock/Web/UI/Controls/PanelDrawer.cs)) caches three values into the control's `ViewState`:
  - `EntityId` from `entity.Id`.
  - `_createdAuditHtml` from `entity.GetCreatedAuditHtml( rootUrl )`.
  - `_modifiedAuditHtml` from `entity.GetModifiedAuditHtml( rootUrl )`.
- The control renders a collapsed drawer with three columns: Created By, Last Modified By, Id. Click on the drawer-pull bar toggles `open` class via `RockPanelDrawerScript` (registered in `OnInit` at [line 139-161](../../Rock/Web/UI/Controls/PanelDrawer.cs)).
- The two audit-html strings include hover tooltips (rendered via `.js-date-rollover`) showing relative time ("6 days ago") and the absolute timestamp.

**Obsidian replacement:** The Detail Block template renders an Audit Details modal (not a drawer) via `<AuditDetail>` from `@Obsidian/Controls/auditDetail.obs` ([`detailBlock.ts:27`](../../Rock.JavaScript.Obsidian/Framework/Templates/detailBlock.ts)). It is gated on `isAuditHidden` ([line 119-123](../../Rock.JavaScript.Obsidian/Framework/Templates/detailBlock.ts)) and the modal opens via a header action (line 1048-1049). The block sets `isAuditHidden="false"` for existing groups and `true` for new groups.

---

## QuickReturn side effect (line 1770-1773)

**Code** ([`GroupDetail.ascx.cs:1770-1773`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)):

```csharp
string lava = "{{ Group.Name | AddQuickReturn:'Groups', 20 }}";
var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson, new Rock.Lava.CommonMergeFieldsOptions() );
mergeFields.Add( "Group", group );
lava.ResolveMergeFields( mergeFields );
```

The result of `ResolveMergeFields` is discarded; this is a side-effect-only Lava run.

**Behavior** (Lava filter at [`Rock/Lava/Filters/LavaFilters.cs:4441-4483`](../../Rock/Lava/Filters/LavaFilters.cs)):
- Resolves to a no-op string return.
- Side effect: registers a `<script>` block that calls `Rock.personalLinks.addQuickReturn( 'Groups', 20, '<groupName>' )` either inside an `Sys.Application.add_load(...)` (during async postback) or in a `$( document ).ready(...)` (initial load).
- The `Rock.personalLinks` object is exposed by the Personal Links block on the page ([`Rock.JavaScript.Obsidian.Blocks/src/Cms/personalLinks.obs:300-337`](../../Rock.JavaScript.Obsidian.Blocks/src/Cms/personalLinks.obs)). If that block is not present, the script silently no-ops.
- Net effect: the current group is added to the user's "Groups" section of the Quick Return menu with order 20.

**Obsidian replacement:** Direct call to `addQuickReturn(title, "Groups", 20)` from `@Obsidian/Utility/page` ([`Rock.JavaScript.Obsidian/Framework/Utility/page.ts:173-184`](../../Rock.JavaScript.Obsidian/Framework/Utility/page.ts)) inside the Vue `<script setup>`. Existing pattern: [`financialBatchDetail.obs:50, 313-322`](../../Rock.JavaScript.Obsidian.Blocks/src/Finance/financialBatchDetail.obs):

```ts
import { addQuickReturn } from "@Obsidian/Utility/page";
// ...
if (panelMode.value === DetailPanelMode.View && config.entity?.name) {
    addQuickReturn(config.entity.name, "Batches", 50);
}
```

For Group Detail, this would be `addQuickReturn(group.name, "Groups", 20)` once the block is in view mode for an existing group.

---

## AddConfigurationUpdateTrigger

**Code** ([`GroupDetail.ascx.cs:473`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)):

```csharp
this.AddConfigurationUpdateTrigger( upnlGroupDetail );
```

**Behavior** (helper at [`Rock/Web/UI/RockBlock.cs:658-661`](../../Rock/Web/UI/RockBlock.cs) which calls [`Rock/Web/UI/RockPage.cs:3434-3440`](../../Rock/Web/UI/RockPage.cs)):

```csharp
public void AddConfigurationUpdateTrigger( UpdatePanel updatePanel )
{
    AsyncPostBackTrigger trigger = new AsyncPostBackTrigger();
    trigger.ControlID = "rock-config-trigger";
    trigger.EventName = "Click";
    updatePanel.Triggers.Add( trigger );
}
```

This adds an `AsyncPostBackTrigger` for a virtual control id `rock-config-trigger`. When the user opens the Block Properties dialog and clicks Save, Rock simulates a click on `#rock-config-trigger`, which fires the trigger registered here, which causes the UpdatePanel to do a partial postback, which fires `Block_BlockUpdated` ([`GroupDetail.ascx.cs:1653-1673`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)) so the block can re-render with the new configuration without a full page reload.

**Obsidian replacement:** None. Obsidian blocks listen for the `BlockMessages.ConfigurationChanged` browser bus event automatically (see `useReloadBlock` and `onConfigurationValuesChanged` in `@Obsidian/Utility/block`). When the user changes block settings, the framework calls the registered handler to refresh.

---

## HideSecondaryBlocks

**Code** ([`GroupDetail.ascx.cs:2901-2906`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)):

```csharp
private void SetEditMode( bool editable )
{
    pnlEditDetails.Visible = editable;
    fieldsetViewDetails.Visible = !editable;
    this.HideSecondaryBlocks( editable );
}
```

**Behavior** (helper at [`Rock/Web/UI/RockBlock.cs:1028-1031`](../../Rock/Web/UI/RockBlock.cs) which calls [`Rock/Web/UI/RockPage.cs:2667-2676`](../../Rock/Web/UI/RockPage.cs)):

```csharp
public void HideSecondaryBlocks( RockBlock caller, bool hidden )
{
    foreach ( ISecondaryBlock secondaryBlock in this.RockBlocks.Where( a => a is ISecondaryBlock ) )
    {
        if ( secondaryBlock != caller )
        {
            secondaryBlock.SetVisible( !hidden );
        }
    }
}
```

When entering edit mode (`editable = true`), every block on the page that implements `ISecondaryBlock` (other than this one) is hidden. When returning to read mode, they are restored. This is how detail pages hide the surrounding "Group Members", "Group History", "Map" sub-blocks while the user is editing the parent group, so the user isn't tempted to interact with stale data.

**Obsidian replacement:** Obsidian blocks emit `BlockMessages.BeginEdit` and `BlockMessages.EndEdit` on the browser bus when they change mode (see [`detailBlock.ts:628`](../../Rock.JavaScript.Obsidian/Framework/Templates/detailBlock.ts)). Other blocks on the page can subscribe to these messages and hide themselves. The framework wires this for blocks built with `<DetailBlock>` automatically.

---

## EditModeMessage standard messages

**Usage** ([`GroupDetail.ascx.cs:1841, 1846`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)):

```csharp
nbEditModeMessage.Text = EditModeMessage.ReadOnlyEditActionNotAllowed( Group.FriendlyTypeName );
// ...
if ( group.IsSystem )
{
    nbEditModeMessage.Text = EditModeMessage.System( Group.FriendlyTypeName );
}
```

**Definitions** ([`Rock/Constants/DisplayStrings.cs:205-256`](../../Rock/Constants/DisplayStrings.cs)):

| Method | Returns |
|---|---|
| `ReadOnlyEditActionNotAllowed( name )` | `string.Empty` (intentional, current behavior is a silent no-message) |
| `System( name )` | `"<strong>Note</strong> Because this {name} is used by Rock, editing is restricted."` |
| `ReadOnlySystem( name )` | `"<strong>Note</strong> Because this {name} is used by Rock, editing is not enabled."` |
| `NotAuthorizedToView( name )` | `"<strong>Note</strong> You are not authorized to view this {name}."` |
| `NotAuthorizedToEdit( name )` | `"<strong>Note</strong> You are not authorized to edit this {name}."` |

**Obsidian replacement:** Same `EditModeMessage` static class is callable from the Obsidian C# block (it lives in `Rock.Constants` and has no WebForms dependency). The block returns the resolved message via the bag's notification or error message field; the Vue side renders it in a `<NotificationBox>`.

---

## ActionTitle

**Usage** ([`GroupDetail.ascx.cs:1937, 2154-2160`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)):

```csharp
lReadOnlyTitle.Text = ActionTitle.Add( Group.FriendlyTypeName ).FormatAsHtmlTitle();
```

**Definitions** ([`Rock/Constants/DisplayStrings.cs:169-200`](../../Rock/Constants/DisplayStrings.cs)):

| Method | Returns |
|---|---|
| `Add( name )` | `"Add {name}"` |
| `Edit( name )` | `"Edit {name}"` |
| `View( name )` | `"View {name}"` |

**Obsidian replacement:** Reuse the `ActionTitle` constants directly. No WebForms dependency.

---

## None.Text constant

**Usage:** Grid `EmptyDataText` for `gGroupMemberAttributesInherited`, `gGroupMemberAttributes`, `gGroupRequirements`, `gMemberWorkflowTriggers` (lines [435, 441, 451, 462](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)).

**Definition** ([`Rock/Constants/DisplayStrings.cs:94-129`](../../Rock/Constants/DisplayStrings.cs)):

```csharp
public static class None
{
    public const int Id = 0;
    public const string IdValue = "0";
    public const string Text = "";        // empty string
    public const string TextHtml = "";    // empty string
    public static ListItem ListItem { get { return new ListItem( None.Text, None.IdValue ); } }
}
```

`None.Text` is `""`, so the grids' empty-data state shows nothing rather than a "No items" message. This is intentional; the `<RockControlWrapper>` label above each grid already conveys what it represents.

**Obsidian replacement:** Pass an empty string (or omit the prop) to the `<Grid>` control. Constants still callable from C#.

---

## RockPage.GetSharedItem / SaveSharedItem (per-request caching)

Already documented in `19-webforms-isms.md`. Repeated here for completeness because the audit drawer + view-mode-Lava + several other helpers all funnel through `GetGroup()`:

- [`GroupDetail.ascx.cs:2913-2931`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)
- Key format: `string.Format( "Group:{0}", groupId )`.
- Stores the eagerly-loaded `Group` (with `GroupType`, `GroupLocations.Schedules`, `GroupSyncs`) for the duration of the current request, so subsequent `GetGroup` calls in the same lifecycle don't re-hit the database.

**Obsidian replacement:** Hold the loaded group in a local variable for the duration of a single block-action invocation, or use a private field cache on the C# block class with explicit reset.

---

## InetCalendarHelper.CreateCalendarEvent (Custom schedule validation)

**Usage** ([`GroupDetail.ascx.cs:1184-1193`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)):

```csharp
var scheduleType = rblScheduleSelect.SelectedValueAsEnum<ScheduleType>( ScheduleType.None );
if ( scheduleType == ScheduleType.Custom )
{
    iCalendarContent = sbSchedule.iCalendarContent;
    var calEvent = InetCalendarHelper.CreateCalendarEvent( iCalendarContent );
    if ( calEvent == null || calEvent.DtStart == null )
    {
        scheduleType = ScheduleType.None;
    }
}
```

**Behavior** ([`Rock/Model/Core/Schedule/InetCalendarHelper.cs:50`](../../Rock/Model/Core/Schedule/InetCalendarHelper.cs)):
- `CreateCalendarEvent( string iCalendarContent )` parses the iCalendar string (via `Ical.Net`) and returns the first `CalendarEvent` or null.
- Used here as a validity probe: if the parsed event has no `DtStart`, the user gave us garbage and the schedule must be downgraded to `ScheduleType.None` (the unique non-named schedule won't be created).

**Obsidian replacement:** Same. `InetCalendarHelper` has no `System.Web` dependency and is safe to call from an Obsidian block action.

---

## Other server controls and helpers worth flagging

| Control / helper | Where used | Note |
|---|---|---|
| `<Rock:HighlightLabel>` | Heading labels (Inactive, Archived, Private, Elevated Security, Peer Network, Chat, Type, Campus): [lines 29-36](../../RockWeb/Blocks/Groups/GroupDetail.ascx) | Server-rendered `<span class="label label-*">`. Visibility set in `SetHighlightLabelVisibility` for read mode and via inline JS in edit mode. Obsidian: `<HighlightLabel>` Vue component, visibility via `v-if`. |
| `<Rock:ModalAlert ID="mdDeleteWarning">` | [line 453](../../RockWeb/Blocks/Groups/GroupDetail.ascx) | "You are not authorized to delete" / "Group cannot be deleted because..." popups. Obsidian: a `<Modal>` or in-place `<NotificationBox>`. |
| `<Rock:SecurityButton ID="btnSecurity">` | [line 465](../../RockWeb/Blocks/Groups/GroupDetail.ascx) | Renders a security icon that opens the security-edit modal. `EntityTypeId` and `EntityId` set in code-behind (lines [467, 2841-2842](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)). Obsidian: built-in to `<DetailBlock>` via the `entityKey` and `entityTypeGuid` props (sets `isSecurityHidden` to false). |
| `<asp:HyperLink>` quick-action icons | [lines 457-463](../../RockWeb/Blocks/Groups/GroupDetail.ascx) | Group Placement, RSVP, Scheduler, History, Fundraising, Attendance, Map. Each gets `NavigateUrl` set in `ShowReadonlyDetails`. Obsidian: pass URLs through the bag and render `<RockButton>` or `<a>` with `:href`. |
| `<Rock:DynamicPlaceholder ID="phGroupAttributes">` | [line 290](../../RockWeb/Blocks/Groups/GroupDetail.ascx) | Filled at runtime via `Helper.AddEditControls`. Obsidian: `<AttributeValuesContainer>`. |
| `<Rock:NotificationBox>` (multiple) | [lines 15, 50-56, 294, 416, 475, 522, 607, 679](../../RockWeb/Blocks/Groups/GroupDetail.ascx) | Server-rendered Bootstrap alerts. Obsidian: `<NotificationBox>` Vue component. |
| `Rock.dialogs.confirm` (in inline JS) | [line 785](../../RockWeb/Blocks/Groups/GroupDetail.ascx) | Browser confirm dialog wrapper. Obsidian: a `<Modal>` or built-in confirmation utility. |
| `data-shortcut-key="s"` on Save/Cancel/Edit buttons | [lines 438-439, 452](../../RockWeb/Blocks/Groups/GroupDetail.ascx) | Alt+S / Alt+C / Alt+E hotkeys. Obsidian's `<RockButton>` doesn't surface this directly; check the conversion convention. |
| `data-shortcut-key + AccessKey="m"` on Edit | [line 452](../../RockWeb/Blocks/Groups/GroupDetail.ascx) | Hard-coded HTML `accesskey` attribute (browser-native). |

---

## Open questions / flag for spec phase

- **TagCategory attribute** is read at [`GroupDetail.ascx.cs:543`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) but never declared. Decide for the Obsidian conversion: (a) add a proper `[CategoryField( "Tag Category", Key = "TagCategory", ... )]` attribute and document the new behavior, or (b) drop the dead reference. Verify with a database query whether any production block instance actually has a saved value for `Attribute.Key = 'TagCategory'` against this block type before deciding.
- **Badges for new (unsaved) groups**: when `group.Id == 0`, the badge controls receive an entity with a zero id, and most badge components no-op or hide. Confirm the Obsidian Detail Block hides badges automatically in Add mode (likely via `isBadgesVisible="false"` when `entityKey` is null), or set this explicitly.
- **Audit drawer in Add mode**: [`GroupDetail.ascx.cs:1940`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) hides the drawer for `group.Id == 0`. Mirror this in Obsidian by passing `isAuditHidden="true"` when the bag's entity key is null.
- **Following control in Add mode**: `FollowingsHelper.SetFollowing` already hides the panel when `entityId == 0`. Mirror with `isFollowVisible="false"` on `<DetailBlock>` for new groups.
- **`btnSecurity` visibility logic** (`btnSecurity.Visible = group.IsAuthorized( Authorization.ADMINISTRATE, CurrentPerson );` at [line 2841](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)) is `ADMINISTRATE`, not `EDIT` or `VIEW`. Confirm this flows through the Obsidian `<DetailBlock>` via the same authorization check (it's a separate prop or computed inside the framework).
- **`hlGroupHistory` and the Archive button** are interlocked: archive is only shown when `groupType.EnableGroupHistory` AND there is at least one history row. The view-mode action panel needs both flags returned in the bag.
