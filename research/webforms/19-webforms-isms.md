# WebForms-isms / Portability Gaps

Things in `GroupDetail.ascx[.cs]` that don't translate cleanly to Obsidian, with the recommended Obsidian replacement for each.

## ViewState

The block stores ~12 collections in `ViewState` as JSON, hydrated on every postback. In Obsidian, these become fields on the bag round-tripping between client and server, OR they become server-side state (re-derived on each block action call).

Pattern in Obsidian:
- The bag holds the in-progress edit.
- Client-side `ref<TBag>` is the source of truth during an edit session.
- Modal sub-edits update the bag's collection, which immediately reflects in the grid.
- Save sends the entire bag to the server, which diffs against DB and persists.

Affected collections:
- `GroupLocationsState`
- `GroupMemberAttributesInheritedState`
- `GroupMemberAttributesState`
- `GroupRequirementsState`
- `GroupDateAttributesState`
- `GroupSyncState`
- `MemberWorkflowTriggersState`
- `LocationTypeTab` (UI-only state, should stay client-side only)
- `CurrentGroupTypeId` (drives reactive visibility, becomes a `ref` in Vue)
- `AllowMultipleLocations` (derived from `CurrentGroupTypeId.AllowMultipleLocations`, becomes a `computed`)
- `IsScheduleTabVisible` (becomes a `computed`)
- `ScheduleCoordinatorNotificationTypes` (used only to detect None-vs-other change; becomes a watcher)

## UpdatePanel + ContentTemplate semantics

The entire markup is wrapped in a single `<asp:UpdatePanel ID="upnlGroupDetail">` ([`GroupDetail.ascx:13-826`](../../RockWeb/Blocks/Groups/GroupDetail.ascx)). Every server-side interaction is a **partial postback** that re-renders the block region without reloading the page.

What is inside the UpdatePanel `<ContentTemplate>`:

| Region | Purpose |
|---|---|
| `nbNotFoundOrArchived` | Top-level "not found" notification |
| `pnlDetails` | The whole panel-block (heading, audit drawer, badges, body, modals) |
| `hfActiveDialog` | Hidden field tracking which modal is currently open |
| All seven `<Rock:ModalDialog>` controls | Member attribute, Location, Requirement, GroupSync, Trigger, Archive, CopyGroup |

The result: every grid Add/Edit/Delete event, every dropdown autopostback, every modal Save click, replays the full server lifecycle and re-renders the entire block markup. There is nothing important outside the UpdatePanel.

Configuration update trigger: `OnInit` calls `this.AddConfigurationUpdateTrigger( upnlGroupDetail )` ([`GroupDetail.ascx.cs:473`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)). This wires an `AsyncPostBackTrigger` on a virtual `rock-config-trigger` control so block-settings changes (Block Properties dialog) re-render the block immediately. Implementation: [`Rock/Web/UI/RockPage.cs:3434-3440`](../../Rock/Web/UI/RockPage.cs).

**Obsidian replacement:** None. The block is mounted once into a Vue tree and DOM updates are reactive. There is no partial-postback envelope; block actions return JSON, not HTML.

### AutoPostBack controls inside the panel

Every interaction below currently round-trips:

- `ddlGroupType` ([`GroupDetail.ascx:105`](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) `OnSelectedIndexChanged="ddlGroupType_SelectedIndexChanged"`
- `gpParentGroup` ([line 112](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) `OnSelectItem="ddlParentGroup_SelectedIndexChanged"`
- `cbIsSecurityRole` ([line 109](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) `OnCheckedChanged="cbIsSecurityRole_CheckedChanged"`
- `rblScheduleSelect` ([line 231](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) `OnSelectedIndexChanged="rblScheduleSelect_SelectedIndexChanged"`
- `cbOverrideRelationshipStrength`, `rblRelationshipStrength`, `swShowPeerNetworkAdvancedSettings` ([lines 137, 141, 149](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) for peer network reactive
- `cblScheduleCoordinatorNotificationTypes` ([line 279](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) for None-vs-other handling
- `ddlGroupRequirementType` (in modal, [line 611](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) for DueDate controls
- `ddlTriggerType` (in modal, [line 695](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) for qualifier controls
- `lbLocationType` (member/other tab, [line 530](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) for tab switch
- `locpGroupLocation` (in modal, [line 542](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) for duplicate location guard
- `spSchedules` (in modal, [line 552](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) for capacities repeater rebuild

In Obsidian, all of these become reactive `ref` values + `watch()` blocks. No round trip needed.

## Server controls + dynamically rendered controls

- `<Rock:DynamicPlaceholder ID="phGroupAttributes">` ([line 290](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) is filled at runtime via `Rock.Attribute.Helper.AddEditControls`. Replaced in Obsidian by `<AttributeValuesContainer>` reading the bag.
- `<Rock:Grid>` controls in markup with `OnClick="..."` server-side handlers, replaced by `<Grid>` with Vue event handlers (`@addItem`, `@editItem`, `@deleteItem`).
- `<asp:UpdatePanel>`: gone.
- `<asp:Repeater>` (capacities, [line 557](../../RockWeb/Blocks/Groups/GroupDetail.ascx)): replaced with `v-for`.
- `<asp:Panel>` for collapsible sections (PanelWidget): replaced by `<Panel>` Obsidian component.
- `<Rock:ModalDialog>`: replaced by `<Modal>` Obsidian component.

## RockPage.GetSharedItem / SaveSharedItem

`GetGroup(int groupId)` ([line 2913](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)) caches the loaded `Group` in the request's shared-item dictionary so multiple methods on the same request don't re-load it. This is `System.Web.Caching` style request-scoped caching. Obsidian doesn't have an equivalent; instead, load the entity once at the top of an action and pass it around as a parameter, or use a simple field on the block class for the duration of a single block action call.

## ContextEntityBlock

`[ContextAware( typeof( Group ) )]` ([line 54](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)) lets another block on the same page hand it a Group context. Obsidian's `RequestContext` has `GetContextEntity<Group>()` for the equivalent. Confirm whether any production page actually uses this before keeping it.

## Response.Redirect

```csharp
var returnUrl = PageParameter( PageParameterKey.ReturnUrl );
if ( returnUrl.IsNotNullOrWhiteSpace() )
{
    Response.Redirect( returnUrl );
    return;
}
```

In Obsidian block actions, redirects are returned as a `string` (redirect URL) from the block action and the client navigates to it. There is no direct `Response.Redirect` because the block action runs through the Obsidian API endpoint.

## Server.HtmlEncode

`Server` is `HttpServerUtility` (from `System.Web`). The block uses it for grid empty-data text:

| Line | Code |
|---|---|
| [435](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) | `gGroupMemberAttributesInherited.EmptyDataText = Server.HtmlEncode( None.Text );` |
| [441](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) | `gGroupMemberAttributes.EmptyDataText = Server.HtmlEncode( None.Text );` |
| [451](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) | `gGroupRequirements.EmptyDataText = Server.HtmlEncode( None.Text );` |
| [462](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) | `gMemberWorkflowTriggers.EmptyDataText = Server.HtmlEncode( None.Text );` |

`None.Text` resolves to `string.Empty` ([`Rock/Constants/DisplayStrings.cs:109`](../../Rock/Constants/DisplayStrings.cs)), so the encode call is a defensive no-op against future changes.

**Obsidian replacement:** Vue text interpolation already HTML-encodes by default. Use `WebUtility.HtmlEncode(...)` from `System.Net` if explicit encoding is needed server-side; never `System.Web.HttpServerUtility`.

## ResolveClientIds

```csharp
template.ResolveMergeFields( mergeFields ).ResolveClientIds( upnlGroupDetail.ClientID );
```

[`GroupDetail.ascx.cs:2824`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs). `ResolveClientIds` rewrites Lava-rendered `clientId="..."` placeholders to the unique IDs assigned by the WebForms control tree. With no UpdatePanel, this isn't needed. The Lava template can use static IDs.

## ScriptManager interactions

`Sys.Application.add_load(...)` is the ASP.NET AJAX equivalent of `$(document).ready()` that ALSO fires on every UpdatePanel partial postback. The `.ascx` registers two of them:

| Source | Lines | What it registers |
|---|---|---|
| `<script>` block at top of `.ascx` | [`8-10`](../../RockWeb/Blocks/Groups/GroupDetail.ascx) | Bootstrap-tooltip on `.js-follow-status` so the Following control's "Click to Follow" tooltip is wired after every postback |
| `<script>` block at bottom of `.ascx` | [`716-822`](../../RockWeb/Blocks/Groups/GroupDetail.ascx) | All inline reactivity: `setIsActiveControls`, `setPrivateLabel`, `enableRequiredField`, archive confirm dialog, relationship-strength tooltips. Wires click handlers on `.js-isactivegroup`, `.js-ispublicgroup`, `.js-archive-group`, then runs the active/public handlers once on initial load |

Indirect registrations (server-side `ScriptManager.RegisterStartupScript` from controls used by this block):
- `PanelDrawer.OnInit` ([`Rock/Web/UI/Controls/PanelDrawer.cs:139-161`](../../Rock/Web/UI/Controls/PanelDrawer.cs)) registers `RockPanelDrawerScript` to wire `.js-drawerpull` click and `.js-date-rollover` tooltip.
- `FollowingsHelper.SetFollowing` ([`Rock/Web/UI/Controls/FollowingsHelper.cs:120-130`](../../Rock/Web/UI/Controls/FollowingsHelper.cs)) registers a per-control startup script `Rock.controls.followingsToggler.initialize(...)` for the follow panel.
- The Lava `AddQuickReturn` filter ([`Rock/Lava/Filters/LavaFilters.cs:4441-4483`](../../Rock/Lava/Filters/LavaFilters.cs)) registers an inline `Rock.personalLinks.addQuickReturn(...)` script either via `ScriptManager.RegisterStartupScript` (during async postback) or `RockPage.AddScriptToHead` (initial load).

**Obsidian replacement:** None of the above is needed. Vue components mount once; tooltips, click handlers, and follow state are all component-local. The `addQuickReturn` helper exists in TS at [`Rock.JavaScript.Obsidian/Framework/Utility/page.ts:173-184`](../../Rock.JavaScript.Obsidian/Framework/Utility/page.ts) and is called directly from `<script setup>` (see [`financialBatchDetail.obs:50,321`](../../Rock.JavaScript.Obsidian.Blocks/src/Finance/financialBatchDetail.obs)).

## Inline JavaScript

The 130-line inline `<script>` block at the bottom of `.ascx` ([`716-822`](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) handles:
- `setIsActiveControls(activeCheckbox)` ([`719-745`](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) toggles `.js-inactivegroup-label`, `.js-inactivateoptions`, `.js-inactivatechildgroups` based on IsActive, and flips the Inactive Reason `RequiredFieldValidator` via `enableRequiredField`. Reads `hfHasChildGroups` to decide whether to surface the "Inactivate Child Groups" checkbox.
- `setPrivateLabel(publicCheckbox)` ([`747-755`](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) toggles `.js-privategroup-label` based on IsPublic.
- `enableRequiredField(validatorId, enable)` ([`757-765`](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) finds the validator DOM element by id and calls `ValidatorEnable(domObj, enable)` (a global from ASP.NET's `WebUIValidation.js`).
- Archive confirmation ([`783-790`](../../RockWeb/Blocks/Groups/GroupDetail.ascx)): prevents default on `.js-archive-group` click, shows `Rock.dialogs.confirm`, navigates if confirmed.
- `addRelationshipStrengthTooltips()` ([`796-820`](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) finds the `.js-relationship-strength` radios and attaches contextual Bootstrap tooltips (None=0, Some=5, Strong=10, Closest=20). Tooltip text:
  - 0: "No established relationship or interaction."
  - 5: "Basic interactions with a familiar but limited bond."
  - 10: "Frequent interactions characterized by a strong and supportive relationship."
  - 20: "Intense and trusted relationship with a high level of personal engagement and understanding."

Top inline `<script>` ([`3-11`](../../RockWeb/Blocks/Groups/GroupDetail.ascx)):
- `clearActiveDialog()` clears `hfActiveDialog.value` so on the next postback `ShowDialog()` knows no modal is open.
- `Sys.Application.add_load(function () { $('.js-follow-status').tooltip(); });` re-wires the "Click to Follow" tooltip after every partial postback.

**Obsidian replacement:** Reactive Vue logic (watch + computed) replaces all of this.
- Active/Public label visibility: `<HighlightLabel v-if="!isActive" ... />` and `<HighlightLabel v-if="!isPublic" ... />`.
- Inactivate-child-groups checkbox: `v-if="!isActive && hasChildGroups"`.
- RequiredFieldValidator flip: an Obsidian validator's `:rules="isActive ? '' : 'required'"` does the equivalent.
- Archive confirm: a `<Modal>` with v-model boolean.
- Relationship strength tooltips: data-driven prop on the `<RadioButtonList>` items, or an array map keyed by value.
- `clearActiveDialog`: gone (modal visibility is just `ref<boolean>` per modal).
- Follow status tooltip: built-in to Obsidian's detail-block following control.

## Client-side validation

All in-page validation is WebForms validators:
- `<Rock:DataTextBox>` / `<Rock:NumberBox>` / etc auto-emit `RequiredFieldValidator` and pattern validators when `Required="true"` is set.
- `<asp:CustomValidator ID="cvGroup" runat="server" Display="None" />` ([`GroupDetail.ascx:58`](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) is server-validated only via `cvGroup.IsValid = group.IsValid;` ([`GroupDetail.ascx.cs:1300`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)).
- Multiple `<asp:ValidationSummary>` controls per validation group: default (`vsGroup`), `Location`, `vg_GroupRequirement`, `GroupSyncSettings`, `Trigger`.
- The `enableRequiredField(rfvId, true|false)` JS pattern dynamically toggles the Inactive Reason validator based on the IsActive checkbox state, calling ASP.NET's global `ValidatorEnable(domObj, enable)`.

**Obsidian replacement:** `<RockForm>` with `submit` event, one validation context per modal or block. Conditional required is `:rules="isActive ? '' : 'required'"`. Server-side cross-validation moves into the block action and returns errors as a normal failure response.

## hfActiveDialog + ShowDialog/HideDialog + clearActiveDialog

The block tracks which modal is open via a hidden field `hfActiveDialog` ([`GroupDetail.ascx:507`](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) that survives postbacks. Methods at [`3061-3118`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs):
- `ShowDialog(name, setValues)` sets `hfActiveDialog.Value = name` then calls `ShowDialog(setValues)`.
- `ShowDialog(setValues)` is a switch over `hfActiveDialog.Value` that calls `.Show()` on the matching modal.
- `HideDialog()` is a similar switch that calls `.Hide()` and clears the field.

When the user clicks "Cancel" on a `<Rock:ModalDialog>`, the modal is dismissed client-side without a postback, so `hfActiveDialog` would still hold the modal name. The `OnCancelScript="clearActiveDialog();"` attribute on each modal ([`GroupDetail.ascx:510, 517, 603, 633, 675`](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) clears it via the JS function defined at [`GroupDetail.ascx:4-6`](../../RockWeb/Blocks/Groups/GroupDetail.ascx) so the next postback's `ShowDialog()` doesn't re-open the dismissed modal.

**Obsidian replacement:** Each modal has its own `ref<boolean>` for visibility. No tracking field. Cancel just sets the ref to false.

## HiddenFieldWithClass / hfHasChildGroups

`<Rock:HiddenFieldWithClass ID="hfHasChildGroups" runat="server" CssClass="js-haschildgroups" />` ([`GroupDetail.ascx:91`](../../RockWeb/Blocks/Groups/GroupDetail.ascx)). Class definition: [`Rock/Web/UI/Controls/HiddenFieldWithClass.cs:25`](../../Rock/Web/UI/Controls/HiddenFieldWithClass.cs).

This is a `HiddenField` that also renders a `class` attribute, so client-side jQuery can find it by class instead of by ClientID. The block's only consumer:
- Server side: `hfHasChildGroups.Value = groupService.HasDescendantGroups( group.Id, false ) ? "true" : "false";` ([`GroupDetail.ascx.cs:1989`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)).
- Client side: `var hasChildren = $('.js-haschildgroups').val();` then `if (hasChildren === "true") { $('.js-inactivatechildgroups').show(); }` ([`GroupDetail.ascx:729,741-743`](../../RockWeb/Blocks/Groups/GroupDetail.ascx)).

**Obsidian replacement:** A computed property of the bag and a `v-if` on the template. `hasChildGroups` becomes a server-resolved boolean returned in the initialization box (or in the bag returned by `Edit`), and the template uses `v-if="!isActive && hasChildGroups"` directly on the inactivate-child-groups checkbox.

## js-* CSS class conventions

The inline JS finds DOM elements exclusively by class because element IDs change when the block is in different parent containers. Full inventory:

| Class | Defined on | Read by |
|---|---|---|
| `.js-group-panel` | `pnlDetails` ([line 17](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) | `setIsActiveControls`, `setPrivateLabel` use `closest('.js-group-panel')` to scope label searches |
| `.js-isactivegroup` | `cbIsActive` ([line 66](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) | Click handler (line 767), initial state run (line 775) |
| `.js-ispublicgroup` | `cbIsPublic` ([line 69](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) | Click handler (line 771), initial state run (line 779) |
| `.js-archive-group` | `btnArchive` ([line 455](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) | Click handler intercepts navigation, shows confirm dialog (line 783) |
| `.js-haschildgroups` | `hfHasChildGroups` ([line 91](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) | Read by `setIsActiveControls` (line 729) |
| `.js-inactivateoptions` | Two `<div class="row">` wrappers around inactive reason / inactive note ([lines 73, 80](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) | Show/hide by `setIsActiveControls` |
| `.js-inactivatechildgroups` | `cbInactivateChildGroups`'s `ContainerCssClass` ([line 90](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) | Show/hide by `setIsActiveControls` based on hasChildren |
| `.js-relationship-strength` | `rblRelationshipStrength` ([line 141](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) | `addRelationshipStrengthTooltips` finds radio inputs by value within this container |
| `.js-follow-status` | `pnlFollowing` ([line 39](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) | Top inline script (line 9) attaches Bootstrap tooltip |
| `.js-inactivegroup-label` | `hlInactive` ([line 29](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) | Show/hide by `setIsActiveControls` |
| `.js-archivedgroup-label` | `hlArchived` ([line 30](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) | Defined but no JS reads it; `SetHighlightLabelVisibility` controls it server-side |
| `.js-privategroup-label` | `hlIsPrivate` ([line 31](../../RockWeb/Blocks/Groups/GroupDetail.ascx)) | Show/hide by `setPrivateLabel` |

**Obsidian replacement:** None of these classes is needed. Vue's reactive `v-if` / `v-show` and component-scoped logic remove the need for cross-DOM querying. Keep utility classes only when they participate in CSS styling (Rock or Bootstrap).

## SignalR / real-time hooks

None. `signalR`, `signalr`, and `Hub` produce no matches in `GroupDetail.ascx` or `.ascx.cs`. The only real-time-ish behavior is the chat-panel content driven by `ChatHelper`, which is just a server-side configuration check in `SetChatControls`. No Obsidian-side wiring needed.

## Grid binding patterns

`gGroupMemberAttributes.Actions.AddClick += ... ;`, `GridReorder += ...`, `GridRebind += ...` ([`GroupDetail.ascx.cs:430-464`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)). In Obsidian, these are `<Grid @addItem="..." @reorderItem="..." @rebindGrid="...">` events.

`SecurityField.EntityTypeId = ...` ([`GroupDetail.ascx.cs:445-446`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)). In Obsidian, the SecurityField becomes part of the grid column config (or replaced with row actions).

## ValidationGroup + ValidationSummary

WebForms has multiple validation groups (e.g., `vgCopyGroup`, `Trigger`, `Location`, etc.). Obsidian uses `Rock.Obsidian.UI`'s form validation (e.g., `RockForm` with `submit` event), one validation context per modal or block.

## SaveSharedItem (the static `lava` resolution at line 1770-1773)

```csharp
string lava = "{{ Group.Name | AddQuickReturn:'Groups', 20 }}";
var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson, new Rock.Lava.CommonMergeFieldsOptions() );
mergeFields.Add( "Group", group );
lava.ResolveMergeFields( mergeFields );
```

[`GroupDetail.ascx.cs:1770-1773`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs). This is a side-effect-only Lava run to push the group into the user's Quick Return menu. The `AddQuickReturn` Lava filter ([`Rock/Lava/Filters/LavaFilters.cs:4441-4483`](../../Rock/Lava/Filters/LavaFilters.cs)) doesn't return anything useful; it injects an inline script that calls `Rock.personalLinks.addQuickReturn(...)`.

**Obsidian replacement:** Call the typed helper `addQuickReturn(title, "Groups", 20)` from `@Obsidian/Utility/page` directly in `<script setup>`. See pattern in [`financialBatchDetail.obs:50, 321`](../../Rock.JavaScript.Obsidian.Blocks/src/Finance/financialBatchDetail.obs). See `27-misc-surfaces.md` for full details.

## What survives unchanged

- All entity service code (`GroupService`, `ScheduleService`, etc.) is fine to call from Obsidian.
- `Rock.Security.Authorization.AllowPerson(...)` and `Clear()`: fine.
- `Rock.CheckIn.KioskDevice.Clear()`: fine.
- `GroupMemberWorkflowTriggerService.RemoveCachedTriggers()`: fine.
- `BinaryFileService.Get(int)` and the `IsTemporary` toggle: fine.
- `Rock.Attribute.Helper.SaveAttributeEdits(...)` and `SaveAttributeValues(...)`: fine.
- `WrapTransaction(...)`: fine.
- `InetCalendarHelper.CreateCalendarEvent( iCalendarContent )` ([`GroupDetail.ascx.cs:1188`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)): fine.

## Save-flow patterns to preserve

The save flow is intricate but mostly portable. Key invariants:

1. **Order matters**: GroupLocations diff must run before SaveChanges so cascades execute correctly.
2. **Schedule cleanup must happen if the inline schedule is being abandoned**.
3. **GroupRequirements have a deferred-insert pattern**: they need the new group's Id before they can be inserted.
4. **SaveAttributeValues calls SaveChanges internally**: that's why the whole save is in `WrapTransaction`.
5. **Cache invalidations come AFTER the transaction** completes successfully.

These can all be preserved verbatim in the C# block class for Obsidian; only the trigger (`btnSave_Click` to `Save` block action) changes.

## Open questions / flag for spec phase

- The `TagCategory` block attribute is read at [`GroupDetail.ascx.cs:543`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs) via `GetAttributeValue("TagCategory")` but is **not declared** in the `AttributeKey` static class ([lines 205-228](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs)) and has no `[CategoryField]` block-attribute decorator on the class. See `27-misc-surfaces.md` for details.
- Should the Obsidian conversion preserve the partial-postback behavior of `ddlGroupType_SelectedIndexChanged` (which calls `ShowGroupTypeEditDetails` to rebuild much of the form's visibility/required state) verbatim as server-side block action invocations, or hoist the logic client-side into reactive computed values? The latter is faster and simpler, but the source of truth for "which fields apply to this group type" is `GroupTypeCache`, which lives server-side.
- Confirm whether `[ContextAware( typeof( Group ) )]` is exercised on any production page before deciding to keep it in the Obsidian block.
