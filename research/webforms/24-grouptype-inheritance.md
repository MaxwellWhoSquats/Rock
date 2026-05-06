# GroupType Inheritance Behavior

This document covers how `GroupType.InheritedGroupTypeId` chain walking works inside `GroupDetail.ascx.cs`, what inherits via the chain, and what does NOT. It is a companion to:
- [22-grouptype-cascade.md](22-grouptype-cascade.md), covers the GroupType-change reactive cascade decision (Approach A/B/C).
- [09-group-attributes.md](09-group-attributes.md), group attribute values.
- [10-member-attributes.md](10-member-attributes.md), group member attribute definitions.
- [11-group-requirements.md](11-group-requirements.md), group requirements.
- [13-member-workflow-triggers.md](13-member-workflow-triggers.md), member workflow triggers.

## How the chain works

`GroupType.InheritedGroupTypeId` is a nullable self-FK on `[GroupType]`. When set, queries that walk the chain follow `child.InheritedGroupTypeId -> parent.InheritedGroupTypeId -> ...` until null or a cycle is detected.

Inside `GroupTypeCache`, the navigation property is exposed at [GroupTypeCache.cs#L286-L297](../../Rock/Web/Cache/Entities/GroupTypeCache.cs#L286):

```csharp
public GroupTypeCache InheritedGroupType
{
    get
    {
        if ( InheritedGroupTypeId.HasValue && InheritedGroupTypeId.Value != 0 )
        {
            return Get( InheritedGroupTypeId.Value );
        }
        return null;
    }
}
```

The cache also exposes [`GetInheritedGroupTypeIds()`](../../Rock/Web/Cache/Entities/GroupTypeCache.cs#L1052-L1073) which walks the chain and returns the list of GroupType IDs from the innermost (root) to the leaf, with a cycle guard:

```csharp
internal List<int> GetInheritedGroupTypeIds()
{
    var groupTypeIds = new List<int>();
    var groupType = this;

    while ( groupType != null && !groupTypeIds.Contains( groupType.Id ) )
    {
        groupTypeIds.Insert( 0, groupType.Id );
        groupType = groupType.InheritedGroupType;
    }

    return groupTypeIds;
}
```

The cycle guard (`!groupTypeIds.Contains( groupType.Id )`) prevents infinite loops on bad data (A inherits B inherits A).

## BindInheritedAttributes walkthrough

The single inheritance walk in `GroupDetail.ascx.cs` lives in [`BindInheritedAttributes`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3157-L3203).

Signature:
```csharp
private void BindInheritedAttributes( int? inheritedGroupTypeId, AttributeService attributeService )
```

Called from:
| Caller | File:Line | Argument | Notes |
|---|---|---|---|
| `ddlGroupType_SelectedIndexChanged` | [GroupDetail.ascx.cs#L1570](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1570) | `CurrentGroupTypeId` | Re-runs on every dropdown change. |
| `ShowEditDetails` | [GroupDetail.ascx.cs#L2126](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2126) | `group.GroupTypeId` | Initial entry to edit mode. |

**Important detail**: the `inheritedGroupTypeId` argument name is misleading. Despite the name, the caller passes `CurrentGroupTypeId` / `group.GroupTypeId`, the leaf GroupType, NOT the parent. The method walks the chain starting from the leaf via `inheritedGroupType.InheritedGroupTypeId` at the end of each iteration.

Body:
```csharp
GroupMemberAttributesInheritedState = new List<InheritedAttribute>();
GroupDateAttributesState = new List<Attribute>();

while ( inheritedGroupTypeId.HasValue )
{
    var inheritedGroupType = GroupTypeCache.Get( inheritedGroupTypeId.Value );
    if ( inheritedGroupType != null )
    {
        string qualifierValue = inheritedGroupType.Id.ToString();

        // 1. GroupMember attributes scoped to this GroupType -> inherited member-attribute grid.
        foreach ( var attribute in attributeService.GetByEntityTypeId( new GroupMember().TypeId, false ).AsQueryable()
            .Where( a =>
                a.EntityTypeQualifierColumn.Equals( "GroupTypeId", StringComparison.OrdinalIgnoreCase ) &&
                a.EntityTypeQualifierValue.Equals( qualifierValue ) )
            .OrderBy( a => a.Order )
            .ThenBy( a => a.Name )
            .ToList() )
        {
            GroupMemberAttributesInheritedState.Add( new InheritedAttribute(
                attribute.Name,
                attribute.Key,
                attribute.Description,
                Page.ResolveUrl( "~/GroupType/" + attribute.EntityTypeQualifierValue ),
                inheritedGroupType.Name ) );
        }

        // 2. Group attributes (date-typed only) scoped to this GroupType -> due-date dropdown source.
        GroupDateAttributesState.AddRange( attributeService.GetByEntityTypeId( new Group().TypeId, true ).AsQueryable()
            .Where( a =>
                a.EntityTypeQualifierColumn.Equals( "GroupTypeId", StringComparison.OrdinalIgnoreCase ) &&
                a.EntityTypeQualifierValue.Equals( qualifierValue ) &&
                DateFieldTypeIds.Contains( a.FieldTypeId ) )
            .OrderBy( a => a.Order )
            .ThenBy( a => a.Name )
            .ToList() );

        // 3. Walk up to next ancestor.
        inheritedGroupTypeId = inheritedGroupType.InheritedGroupTypeId;
    }
    else
    {
        inheritedGroupTypeId = null;
    }
}

BindGroupMemberAttributesInheritedGrid();
```

### What happens per iteration

1. **GroupMember attributes** are queried where `EntityTypeQualifierColumn == "GroupTypeId"` AND `EntityTypeQualifierValue == thisAncestor.Id`. Each row is wrapped as an `InheritedAttribute` POCO with:
   - `Name` = attribute display name
   - `Key` = attribute key (used for ReservedKeyNames)
   - `Description`
   - `Url = ~/GroupType/{ancestorId}` (resolved via `Page.ResolveUrl`)
   - `GroupType = ancestorName` (for the "Inherited from {GroupType-link}" label).
2. **Group date-typed attributes** are queried with the same qualifier scheme but for `EntityTypeId = Group` and additional filter `DateFieldTypeIds.Contains(FieldTypeId)`. These feed `GroupDateAttributesState`, used in the requirement-modal "Due Date Group Attribute" dropdown when the group is null/new.
3. The loop advances via `inheritedGroupTypeId = inheritedGroupType.InheritedGroupTypeId`. The leaf GroupType's own attributes are added in iteration 1; its parent's in iteration 2; and so on.

**No cycle guard** in this implementation. If the chain has a loop (A -> B -> A), this method runs forever. The newer GroupTypeDetail Obsidian block at [Rock.Blocks/Group/GroupTypeDetail.cs#L1928-L1935](../../Rock.Blocks/Group/GroupTypeDetail.cs#L1928) added a `HashSet<int>` cycle guard with a clear error message, the GroupDetail block does NOT have this and would hang. Flag for spec.

## Permission semantics

The inheritance walk does NOT filter for `IsAuthorized`. All inherited attributes appear in the read-only grid regardless of user permissions on the ancestor GroupType. This is consistent with Rock's standard "inherited attributes are advisory display" pattern.

## What inherits via the chain

| Setting / Collection | Inherits via chain? | Source |
|---|---|---|
| **Group member attribute definitions** | **Yes** | `BindInheritedAttributes` at [line 3168-L3182](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3168). Collected as `GroupMemberAttributesInheritedState`. |
| **Date-typed group attributes (for due-date dropdown)** | **Yes** | `BindInheritedAttributes` at [line 3185-L3192](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3185). Collected as `GroupDateAttributesState`. Used only when group is null/new. |
| **Group attributes (the runtime values editor)** | **Yes** (via `LoadAttributes`) | `group.LoadAttributes()` resolves the chain transparently for both definitions and values. The `phGroupAttributes` placeholder includes attributes from all ancestors automatically. |
| **Group requirements** | **No** | `BindGroupRequirementsGrid` queries `Where( a => a.GroupTypeId == CurrentGroupTypeId )` ([GroupDetail.ascx.cs#L4544](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4544)). Only the immediate GroupType's requirements appear in the read-only "From {GroupType}" grid. `Group.GetGroupRequirements()` at runtime ([Group.Logic.cs#L538](../../Rock/Model/Group/Group/Group.Logic.cs#L538)) likewise only joins to `this.GroupTypeId`. |
| **GroupType roles** | **No** | `groupType.Roles` getter at [GroupTypeCache.cs#L650-L675](../../Rock/Web/Cache/Entities/GroupTypeCache.cs#L650) queries roles `Where( r => r.GroupTypeId == Id )` only. No chain walk. Roles must be defined on each GroupType locally. |
| **Inactive reasons** | **No** | `GetInactiveReasonsForGroupType(groupTypeId)` ([GroupDetail.ascx.cs#L1977](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1977)) is a single-GroupType query. |
| **Location type values** | **No** | `groupType.LocationTypeValues` is a per-GroupType collection on `GroupTypeCache`. |
| **Allowed schedule types** | **No** | `groupType.AllowedScheduleTypes` is a per-GroupType `[Flags]` enum. |
| **GroupCapacityRule, IsCapacityRequired** | **No** | Per-GroupType. |
| **EnableRSVP, RSVPReminderOffsetDays, RSVPReminderSystemCommunicationId** | **No** | Per-GroupType (the group can override). |
| **IsPeerNetworkEnabled, RelationshipStrength, multipliers** | **No** | Per-GroupType (the group can override). |
| **AllowSpecificGroupMemberAttributes, AllowSpecificGroupMemberWorkflows, EnableSpecificGroupRequirements, AllowGroupSync** | **No** | Per-GroupType. |
| **IsSchedulingEnabled, AttendanceRecordRequiredForCheckIn, ScheduleConfirmationLogic** | **No** | Per-GroupType. |
| **IsChatAllowed and chat-related fields** | **No** | Per-GroupType. |
| **AdministratorTerm, ShowAdministrator** | **No** | Per-GroupType. |
| **GroupStatusDefinedTypeId** | **No** | Per-GroupType. |
| **AllowGroupSpecificRecordSource** | **No** | Per-GroupType. |
| **GroupViewLavaTemplate** | **No** | Per-GroupType ([GroupDetail.ascx.cs#L2823](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2823)). |
| **GroupsRequireCampus, EnableLocationSchedules, AllowMultipleLocations, LocationSelectionMode** | **No** | Per-GroupType. |
| **EnableGroupHistory, EnableGroupTag, TakesAttendance, EnableInactiveReason, RequiresInactiveReason** | **No** | Per-GroupType. |

### Summary

In `GroupDetail.ascx.cs`, **only attribute definitions** (member attrs as the inherited grid; date-typed group attrs as the due-date source) walk the chain. Everything else reads off the immediate `GroupTypeCache`.

This is consistent with Rock's design: GroupType inheritance was added primarily to support a small attribute-related use case (the "Check-in Configuration" template/area pattern) where shared attributes need to live on a parent template. Block configuration flags, roles, and other settings are NOT inherited.

## The "(Inherited from {GroupType-link})" labeling pattern

Rendered in two places:

### Member attribute inherited grid

Markup ([GroupDetail.ascx#L302-L304](../../RockWeb/Blocks/Groups/GroupDetail.ascx#L302)):
```html
<Rock:RockTemplateField HeaderText="Inherited">
    <ItemTemplate>(Inherited from <a href='<%# Eval("Url") %>' target='_blank' rel='noopener noreferrer'><%# Eval("GroupType") %></a>)</ItemTemplate>
</Rock:RockTemplateField>
```

The `Url` is `Page.ResolveUrl( "~/GroupType/" + attribute.EntityTypeQualifierValue )`. The `~/GroupType/{Id}` route is mapped to the GroupType detail page (Page Guid `5CD8E024-710B-4EDE-8C8C-4C9E15E6AFAB`, see [Rock/SystemGuid/Page.cs#L1204](../../Rock/SystemGuid/Page.cs#L1204)).

The link points to the **immediate ancestor where the attribute is defined**, not the chain root, and not the leaf. So if A -> B -> C and the attribute is on B, the link goes to B's GroupType detail page.

### "From group type" requirements grid

Markup ([GroupDetail.ascx#L329](../../RockWeb/Blocks/Groups/GroupDetail.ascx#L329)) renders a placeholder; the actual link is built server-side in [`BindGroupRequirementsGrid`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4552):

```csharp
lGroupTypeGroupRequirementsFrom.Text = string.Format(
    "(From <a href='{0}' target='_blank' rel='noopener noreferrer'>{1}</a>)",
    this.ResolveUrl( "~/GroupType/" + CurrentGroupTypeCache.Id ),
    CurrentGroupTypeCache.Name );
```

Always points to the **leaf** GroupType (the group's immediate type), since the requirements grid does not walk the chain.

## ReservedKeyNames spans inherited + custom for member attributes

In [`gGroupMemberAttributes_ShowEdit`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4409-L4412):

```csharp
var reservedKeyNames = new List<string>();
GroupMemberAttributesInheritedState.Select( a => a.Key ).ToList().ForEach( a => reservedKeyNames.Add( a ) );
GroupMemberAttributesState.Where( a => !a.Guid.Equals( attributeGuid ) ).Select( a => a.Key ).ToList().ForEach( a => reservedKeyNames.Add( a ) );
edtGroupMemberAttributes.ReservedKeyNames = reservedKeyNames.ToList();
```

This means a key defined on **any ancestor** is reserved. A user editing the immediate group's member attributes cannot define a key that collides with anything in the chain. This prevents downstream attribute-resolution ambiguity.

The list is recomputed each time the modal opens, so adding a new custom key in one modal session is reflected in the next session's reserved set automatically.

## Implication for the OptionsBag size discussion

Per [22-grouptype-cascade.md](22-grouptype-cascade.md), Approach A (front-load) bundles per-GroupType options for every GroupType the user could pick. The relevant inheritance-related fields:

| Field | Inheritance walk? | Per-GroupType cost |
|---|---|---|
| `inheritedMemberAttributes: InheritedAttributeBag[]` | **Yes** (walks full chain) | Sum of GroupMember attributes across all ancestors. |
| `dateAttributesForGroup: ListItemBag[]` | **Yes** (walks full chain, filtered to date-typed) | Sum of date-typed Group attributes across all ancestors. |
| `groupRequirementsForGroupType: GroupRequirementBag[]` | **No** (single GroupType) | Just this GroupType's requirements (likely small). |
| `roles: ListItemBag[]` | **No** (single GroupType) | Just this GroupType's roles. |
| `inactiveReasons: ListItemBag[]` | **No** | Single GroupType. |

### Does inheritance significantly increase per-GroupType payload?

**Yes, but bounded.** For typical Rock installs:
- A leaf GroupType inheriting from a "Small Group" template might have ~5 GroupMember attributes locally + ~10 inherited = 15 total.
- A leaf in a longer chain (rare) might have 30+ inherited.

Per-attribute cost is roughly:
- Name (~30 chars) + Key (~20 chars) + Description (~80 chars) + URL (~30 chars) + GroupType name (~30 chars) = **~190 bytes raw**, or ~250 bytes JSON-serialized with structure.

Per-GroupType inherited-attribute payload: 15 attrs × 250 bytes = **~3.75 KB**.
For 50 GroupTypes (the realistic-upper-end for one block instance after filters): 50 × 3.75 KB = **~187 KB**.

This is significant but not catastrophic. For Rock installations with many GroupTypes (e.g., 100+ check-in configurations sharing a common base), this can climb to 350+ KB.

### Mitigation in Approach A

If profiling shows the inheritance walk dominates payload size:

1. **Hash inherited attributes once**: many leaf GroupTypes share the same parent. The OptionsBag could include a `Map<int /* groupTypeId */, InheritedAttributeBag[]>` keyed by ancestor, and per-GroupType options reference parent-IDs by chain. Each leaf would carry only its OWN attributes plus a chain-id list. This trades JSON repetition for one indirection.
2. **Lazy-load inherited only**: front-load everything except the inherited walk; fetch inherited attributes via block action when the GroupType changes. The inherited walk is the most expensive part of `GetBoxOptions()` (most queries in cold cache), so this is also a perf-win.
3. **Approach C with a per-GroupType cache key including the ancestor chain hash**: invalidate when any GroupType in the chain changes. More complex, only worth it for very large installs.

### What the existing GroupType detail Obsidian block does

[`Rock.Blocks/Group/GroupTypeDetail.cs`](../../Rock.Blocks/Group/GroupTypeDetail.cs) uses the **lazy-load via block action** approach (option 2 above). Its `GetInheritedAttributes(Guid inheritedGroupTypeGuid)` action ([line 1900](../../Rock.Blocks/Group/GroupTypeDetail.cs#L1900)) walks the chain on-demand and is invoked only when the inherited GroupType selection changes. This is a strong precedent for the GroupDetail conversion: avoid front-loading inherited attributes, fetch on-demand instead.

## Other inheritance-related behavior

### `Group.LoadAttributes` resolves the chain transparently

Calling `group.LoadAttributes()` reads the attribute definitions defined for the immediate GroupType AND every ancestor in the chain. The `group.Attributes` dictionary contains all of them, with values from `[AttributeValue]` joined on `(AttributeId, EntityId=group.Id)`. The block uses this in:
- [`ShowGroupTypeEditDetails`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2276) to populate `phGroupAttributes`.
- [`gGroupRequirements_ShowEdit`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3979) to populate the DueDateGroupAttribute dropdown.
- [`btnSave_Click`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1260) to read attribute values out of the editors.

### `IsAuthorized` does NOT walk the chain

`group.IsAuthorized(...)` only considers the group's direct auth rules and the GroupType's auth rules (the immediate GroupType, via the entity's auth-cascade pattern). Ancestor GroupType auth is NOT consulted. So an admin on a parent GroupType has no implicit authority over groups whose leaf GroupType inherits from it.

### `GroupTypeCache.GetInheritedAttributesForQualifier` exists for runtime use

[`GetInheritedAttributesForQualifier`](../../Rock/Web/Cache/Entities/GroupTypeCache.cs#L995-L1045) is the cache-friendly version of the chain walk for general inherited attributes. The block does NOT use this method (it has its own walk in `BindInheritedAttributes`). Confirm in spec phase whether the Obsidian conversion should use the cache helper instead of duplicating the loop.

### `Rock.Blocks/Group/GroupTypeDetail.cs` GetInheritedAttributes pattern

When converting GroupDetail to Obsidian, copy the pattern from [`GroupTypeDetail.cs#L1900-L2027`](../../Rock.Blocks/Group/GroupTypeDetail.cs#L1900):
- Block action returns three lists: `InheritedGroupAttributes`, `InheritedGroupMemberAttributes`, `InheritedGroupTypeAttributes`. (For `GroupDetail` we only need the first two.)
- Each item is `GroupTypeInheritedAttributeBag { Name, Description, Key, Guid, InheritedFromGroupTypeName, InheritedFromGroupTypeUrl }`.
- The URL is built from `EntityTypeCache.Get(typeof(GroupType)).LinkUrlLavaTemplate` resolved with the ancestor as the `Entity` merge field, falling back to `GetCurrentPageUrl(GroupTypeId=ancestorIdKey)`. **More flexible** than GroupDetail's hard-coded `~/GroupType/{Id}` URL, likely worth adopting.
- `HashSet<int> visitedGroupTypeIds` cycle guard with a clear "circular inheritance" error response.

## Open questions / flag for spec phase

- **No cycle guard in `BindInheritedAttributes`**: WebForms loops forever if a cycle exists. The GroupTypeDetail Obsidian block added a guard. **Add one in the conversion.**
- **`~/GroupType/{Id}` hard-coded URL**: should switch to `EntityTypeCache.Get(typeof(GroupType)).LinkUrlLavaTemplate` resolution like the new GroupTypeDetail block. This respects custom routes and falls back gracefully. Use `IdKey` rather than integer Id for cross-site predictable-ID safety.
- **Front-load vs lazy-load inherited attributes**: see Approach A/B/C in [22-grouptype-cascade.md](22-grouptype-cascade.md). The inherited-attribute walk is the most expensive part of building per-GroupType options. Recommend lazy-load via block action even within Approach A's overall front-loading model.
- **Does `BindGroupRequirementsGrid` need to walk inheritance?** Today it does not (only shows requirements where `GroupTypeId == leafType.Id`). If product wants requirements from ancestor GroupTypes to display, this would need to change, see [11-group-requirements.md](11-group-requirements.md) Open Questions.
- **`GroupTypeCache.GetInheritedAttributesForQualifier` reuse**: the Obsidian conversion could call this cache method instead of writing a fresh loop. Trade-off: the cache method returns `AttributeCache` not `Attribute`, and the block's `InheritedAttribute` projection includes ancestor name/URL data that the cache method doesn't have. Likely the conversion writes its own loop or uses `GroupTypeDetail.GetInheritedAttributes`'s pattern. Document the choice.
- **OptionsBag size**: see "Does inheritance significantly increase per-GroupType payload?" above. Profile with realistic data before committing to Approach A's full enumeration.
- **`hlType` link respects ADMINISTRATE on the GroupType**: at [GroupDetail.ascx.cs#L2674-L2682](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2674), the GroupType label is wrapped in a hyperlink only if the user has `Authorization.ADMINISTRATE` on the GroupType. This is unrelated to inheritance but is the only place a GroupType-level auth check appears in this block. Inherited "Inherited from" links are shown unconditionally, possible information disclosure if ancestor names are sensitive. Flag for security review during spec.
