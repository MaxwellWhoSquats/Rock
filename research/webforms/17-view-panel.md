# View Panel

## Decision: Pure Vue, no Lava

The conversion's View panel will be **a pure Vue component**, fed by structured fields on the `GroupBag`. **No server-rendered Lava** is used in the view panel. The visual design is defined in the Figma file the user is providing — Figma is the source of truth for what the View panel renders, including any net-new content beyond the WebForms parity.

This means:
- `groupType.GroupViewLavaTemplate` is **not used** by the new GroupDetail.
- Customer customizations to that template (or to the `core_templates_GroupViewTemplate` system setting) are **not honored** by the new GroupDetail. Customers who depend on a customized template need to be informed during release notes.
- The C# block populates structured fields on the bag (e.g., `Members.Count`, `LocationsForDisplay`, `ScheduleSummary`, etc.) per the Figma design. The Vue ViewPanel renders them.
- Any feature that today relied on Lava-template-only behavior must be re-implemented either as bag fields or as Vue logic.

The remainder of this document describes the **legacy** WebForms behavior, retained as a parity reference. None of the legacy mechanisms below ship into the Obsidian conversion.

## Legacy behavior reference (WebForms only)

In view mode, the WebForms block renders the body of the panel using the Lava template stored on `GroupType.GroupViewLavaTemplate`. The block itself only renders chrome (title, labels, badges, audit drawer, action buttons). The middle section is rendered server-side from Lava.

The default template lives in `SystemSetting` under key `core_templates_GroupViewTemplate`. When a GroupType has `GroupViewLavaTemplate` empty/null, that system setting is used.

## How it is rendered

`ShowReadonlyDetails` (line 2821-2825):

```csharp
if ( groupType != null )
{
    string template = groupType.GroupViewLavaTemplate;
    lContent.Text = template.ResolveMergeFields( mergeFields ).ResolveClientIds( upnlGroupDetail.ClientID );
}
```

`lContent` is a literal control inside `fieldsetViewDetails`:

```html
<asp:Literal ID="lContent" runat="server"></asp:Literal>
```

So the entire view body is HTML produced by Lava.

## Merge fields supplied

Built up across `ShowReadonlyDetails` lines 2727-2810:

| Key | Value |
|---|---|
| Common merge fields | `Rock.Lava.LavaHelper.GetCommonMergeFields(RockPage, CurrentPerson)` |
| `Group` | the group entity itself |
| `RegistrationInstancePage` | route from `LinkedPageRoute(AttributeKey.RegistrationInstancePage)` |
| `EventItemOccurrencePage` | route from `LinkedPageRoute(AttributeKey.EventItemOccurrencePage)` |
| `ContentItemPage` | route from `LinkedPageRoute(AttributeKey.ContentItemPage)` |
| `ShowLocationAddresses` | block attribute boolean |
| `MapStyle` | DefinedValue resolved from MapStyle block attribute |
| `GroupMapUrl` | URL from `LinkedPageUrl(AttributeKey.GroupMapPage, {GroupId})` |
| `GroupHistoryUrl` | URL from `LinkedPageUrl(AttributeKey.GroupHistoryPage, {GroupId})` |

The `RegistrationInstancePage`, `EventItemOccurrencePage`, `ContentItemPage` are passed as ROUTES (not full URLs). The Lava template appends query strings.

## Default Lava template

Stored in `Rock.Web.SystemSettings` key `core_templates_GroupViewTemplate`. Set in `ApplyNewGroupTypeDefaultValues` of `GroupTypeDetail.cs` (Obsidian) line 848-850:

```csharp
if ( entity.GroupViewLavaTemplate.IsNullOrWhiteSpace() )
{
    entity.GroupViewLavaTemplate = Rock.Web.SystemSettings.GetValue( "core_templates_GroupViewTemplate" );
}
```

The actual Lava body of that template is a SQL-seeded blob set in early Rock migrations and tweaked in subsequent rollups (e.g., `054_MigrationRollupsForV8_2.cs` references it). It typically renders:
- A description.
- Group statistics (member count, requirements progress, etc.).
- A linked schedule.
- Locations (with addresses if `ShowLocationAddresses`).
- Group attribute values.
- Possibly a child group tree.

The exact HTML/Lava is not critical for the conversion if the View panel is being redesigned — but the default template should still be honored as a fallback for sites that customized it.

## Side-effect Lava render in ShowDetail

`ShowDetail` at line 1770-1773 has a peculiar Lava resolution that doesn't write to any control:

```csharp
string lava = "{{ Group.Name | AddQuickReturn:'Groups', 20 }}";
var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields(...);
mergeFields.Add( "Group", group );
lava.ResolveMergeFields( mergeFields );
```

The `AddQuickReturn` filter has a side effect (it pushes into the Quick Return menu). The Lava is run purely for that side effect. In Obsidian, this can be replaced with a server-side `QuickReturnService.Add(...)` call or its current equivalent.

## Migration story for customer-customized templates

Sites that customized `GroupViewLavaTemplate` on a specific GroupType, or that overrode `core_templates_GroupViewTemplate` system-wide, will lose those customizations when this block ships. Action items:

- Release notes must call this out clearly so admins can plan.
- Consider a one-time data audit that detects non-default `GroupViewLavaTemplate` values on GroupType rows and surfaces them to the implementer for review.
- The `GroupViewLavaTemplate` column on `GroupType` itself is **not** removed (keeping it preserves the data and avoids breaking other code that may read it). It is simply unused by the new GroupDetail.

## Tag list (above lContent)

```html
<Rock:TagList ID="taglGroupTags" runat="server" CssClass="clearfix" />
```

Configured in `OnLoad` (lines 540-546):

```csharp
taglGroupTags.EntityTypeId = group.TypeId;
taglGroupTags.EntityGuid = group.Guid;
taglGroupTags.CategoryGuid = GetAttributeValue( "TagCategory" ).AsGuidOrNull();
taglGroupTags.GetTagValues( CurrentPersonId );
taglGroupTags.Visible = GetAttributeValue( EnableGroupTags ).AsBoolean() && group.GroupType.EnableGroupTag;
```

Note: `GetAttributeValue("TagCategory")` is referenced but **TagCategory is not in `AttributeKey`** and not declared as a block attribute. This appears to be a latent bug or a vestigial feature — `AsGuidOrNull()` returns null, which TagList accepts as "all categories". The conversion can ignore this branch unless we want to add a real TagCategory attribute.

## Following

```csharp
FollowingsHelper.SetFollowing( group, pnlFollowing, this.CurrentPerson );
```

Sets up the "follow this group" UI in the panel header. Standard pattern.

## Audit drawer

```html
<Rock:PanelDrawer ID="pdAuditDetails" runat="server"></Rock:PanelDrawer>
```

Set via `pdAuditDetails.SetEntity( group, ResolveRockUrl("~") )` (line 2691). Renders Created / Modified info. Standard.

## Phase considerations

The View panel is a single coherent surface with several quick-link hyperlinks computed per group. It **must** ship in the same phase as the readonly path (the C# `GetEntityBagForView`).

The Pure-Vue redesign is a substantial implementation effort driven by the Figma design. It can either:
- Land in Phase 1 (the shell phase) if the Figma is finalized when Phase 1 starts.
- Land in a dedicated later phase (Phase 6 in the current partitioning) if the design needs more iteration.

The dedicated phase approach is safer because it lets the rest of the conversion ship behind a working-but-temporary view panel and decouples implementation pace from design pace. Phase 1's view panel can be a structured but unstyled placeholder that fields the structured bag, and the visual polish phase replaces the markup later.
