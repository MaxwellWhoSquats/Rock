# Sub-feature: Group Member Attribute Definitions

## What it is

This panel manages the **definitions** of custom group-member attributes scoped to this single group. (Distinct from the Group Member detail block, which manages **values** of those attributes for individual members.)

Two grids:

1. **Inherited Group Member Attributes** (read-only): attributes defined on this group type or any of its ancestors via the `InheritedGroupTypeId` chain.
2. **Group Member Attribute(s)** (editable): attributes defined directly on this group instance, with Add / Edit / Delete / Reorder / Security.

See [24-grouptype-inheritance.md](24-grouptype-inheritance.md) for the full inheritance walk used to populate the inherited grid.

## Trigger conditions

The "Member Attributes" PanelWidget is gated by:

| Gate | Source | Effect |
|---|---|---|
| `canAdministrate = group.IsAuthorized( Authorization.ADMINISTRATE, CurrentPerson )` | [GroupDetail.ascx.cs#L2001-L2004](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2001) | Hidden entirely for users without ADMINISTRATE on the group. |
| `GroupMemberAttributesInheritedState.Any() OR GroupMemberAttributesState.Any() OR CurrentGroupTypeCache.AllowSpecificGroupMemberAttributes` | [GroupDetail.ascx.cs#L4515](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4515) | Hidden if there are no inherited or custom attributes AND the group type does not allow specific member attributes. |
| Edit mode | `pnlEditDetails.Visible` | Only shown in edit mode. |

The modal `dlgGroupMemberAttribute` opens when:
- The user clicks the Add button (only visible if `gGroupMemberAttributes.Actions.ShowAdd = true`, set unconditionally in [`OnInit`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L439)).
- The user clicks the Edit pencil on a row.

## Markup region

[`RockWeb/Blocks/Groups/GroupDetail.ascx`](../../RockWeb/Blocks/Groups/GroupDetail.ascx) lines 293-325 plus the modal at lines 510-514.

```
wpGroupMemberAttributes (visible only with ADMINISTRATE auth) (title "Member Attributes")
├── NotificationBox "Member Attributes apply to members in this group..."
├── rcwGroupMemberAttributesInherited (visible if any inherited)
│   └── gGroupMemberAttributesInherited grid (light, no paging, ShowAdd=false)
│       └── columns: Name, Description, "Inherited from <a>{GroupType-link}</a>"
└── rcwGroupMemberAttributes (label flips to "Group Member Attributes" when inherited grid present, else empty)
    └── gGroupMemberAttributes grid (light, no paging, ShowAdd=true, ShowConfirmDeleteDialog=false)
        └── columns: Reorder, Name, Description, Required (BoolField), Security, Edit, Delete

dlgGroupMemberAttribute (modal, ValidationGroup="GroupMemberAttribute")
└── AttributeEditor edtGroupMemberAttributes (entity = GroupMember, ShowActions=false)
```

## UI surface

| Control | Type | Notes |
|---|---|---|
| `nbGroupMemberAttributes` | NotificationBox (Info) | "Member Attributes apply to members in this group. Each member will have their own value for these attributes." |
| Inherited grid header label | RockControlWrapper | "Inherited Group Member Attributes" |
| Inherited grid: Attribute | RockBoundField (`Name`) | Required column. |
| Inherited grid: Description | RockBoundField (`Description`) | |
| Inherited grid: Inherited | RockTemplateField | Renders as `(Inherited from <a href="{Url}" target="_blank" rel="noopener noreferrer">{GroupType}</a>)`. The Url is `~/GroupType/{InheritedGroupTypeId}` (resolved via `Page.ResolveUrl` at [GroupDetail.ascx.cs#L3180](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3180)). |
| Inherited grid empty text | EmptyDataText = `None.Text` (HtmlEncoded) | Set in `OnInit` at [line 435](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L435). |
| Editable grid header label | RockControlWrapper | "Group Member Attribute(s)" or "Group Member Attributes" depending on whether inherited grid is shown. |
| Editable grid: Reorder | ReorderField | Drives `gGroupMemberAttributes_GridReorder` event. |
| Editable grid: Attribute | RockBoundField (`Name`) | |
| Editable grid: Description | RockBoundField (`Description`) | |
| Editable grid: Required | BoolField (`IsRequired`) | |
| Editable grid: Security | SecurityField (TitleField=`Name`, EntityTypeId set to `Attribute`'s entity-type id at [line 446](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L446)) | Drives the standard SecurityField popup against the Attribute entity. |
| Editable grid: Edit | EditField | Drives `gGroupMemberAttributes_Edit`. |
| Editable grid: Delete | DeleteField | Drives `gGroupMemberAttributes_Delete`. |
| Modal | ModalDialog | Title "Group Member Attributes" (note plural, the title doesn't change between Add and Edit). The `AttributeEditor.ActionTitle` IS updated; see below. |
| `edtGroupMemberAttributes` | AttributeEditor | `ShowActions=false`, `ValidationGroup="GroupMemberAttribute"`. The full attribute-editor surface (Key, Name, Description, Categories, Field Type, Default, IsRequired, ShowInGrid, ShowOnBulk, IsAnalytic, IsIndexEnabled, etc.). |

## Code-behind summary

| Method | File:Line | Notes |
|---|---|---|
| `BindInheritedAttributes` | [GroupDetail.ascx.cs#L3157-L3203](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3157) | Walks group type inheritance chain via `inheritedGroupType.InheritedGroupTypeId`, fills `GroupMemberAttributesInheritedState` and `GroupDateAttributesState`. Each inherited attribute is added with `Url = "~/GroupType/{inheritedGroupTypeId}"`. |
| `ShowEditDetails` | [GroupDetail.ascx.cs#L2117-L2126](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2117) | Loads `GroupMemberAttributesState` from DB by qualifier `(GroupMember, "GroupId", group.Id)`; calls `BindGroupMemberAttributesGrid` and `BindInheritedAttributes`. |
| `gGroupMemberAttributes_Add` / `_Edit` / `_ShowEdit` | [GroupDetail.ascx.cs#L4374-L4417](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4374) | Standard CRUD + ReservedKeyNames calc (from inherited + custom-excluding-self). New attribute defaults `FieldTypeId = TEXT`. |
| `gGroupMemberAttributes_GridReorder` | [GroupDetail.ascx.cs#L4424-L4428](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4424) | `ReorderAttributeList` then bind. Custom-only, does not affect inherited grid. |
| `gGroupMemberAttributes_Delete` | [GroupDetail.ascx.cs#L4435-L4441](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4435) | Remove from state + bind. No confirm dialog (`ShowConfirmDeleteDialog="false"` on the grid). |
| `dlgGroupMemberAttribute_SaveClick` | [GroupDetail.ascx.cs#L4468-L4505](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4468) | Preserves Order/CreatedDateTime/CreatedByPersonAliasId/Foreign* across saves. New attributes get `Order = max+1`. |
| `BindGroupMemberAttributesInheritedGrid` | [GroupDetail.ascx.cs#L4510-L4525](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4510) | Visibility + label-flip logic. |
| `BindGroupMemberAttributesGrid` | [GroupDetail.ascx.cs#L4530-L4536](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4530) | Sort by Order then Name + bind. |
| `btnSave_Click` body | [GroupDetail.ascx.cs#L1338-L1359](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1338) | Inside the WrapTransaction: get DB attributes for this Group's qualifier, delete any in DB but not in state, save the rest via `Rock.Attribute.Helper.SaveAttributeEdits`. |

## Server flow (Edit -> Save)

1. **Enter edit mode** ([`ShowEditDetails`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1933)):
   - Set `wpGroupMemberAttributes.Visible = canAdministrate` ([line 2004](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2004)).
   - Load `GroupMemberAttributesState` from DB (case-insensitive match on qualifier column) ordered by `Order, Name`.
   - Call `BindInheritedAttributes(group.GroupTypeId, attributeService)` to walk the chain and fill `GroupMemberAttributesInheritedState` and `GroupDateAttributesState`.
   - Call `BindGroupMemberAttributesGrid` and `BindGroupMemberAttributesInheritedGrid` (the latter triggers visibility recompute and label flip).
2. **Add / Edit a definition** (`gGroupMemberAttributes_ShowEdit`):
   - Compute `ReservedKeyNames` = (all inherited keys) ∪ (all custom keys excluding self).
   - For an existing attribute, pull from `GroupMemberAttributesState` (NOT from DB) so unsaved edits survive.
   - For a new attribute, default `FieldTypeId = TEXT`.
   - `edtGroupMemberAttributes.ActionTitle = "Add attribute for group members of {tbName.Text}"` (or "Edit attribute for..."), driven by the live group name textbox, not the saved name.
   - `SetAttributeProperties( attribute, typeof(GroupMember) )`.
3. **Save modal** (`dlgGroupMemberAttribute_SaveClick`):
   - `attribute.IsValid` short-circuits.
   - Preserve immutable fields (`Order`, `CreatedDateTime`, `CreatedByPersonAliasId`, `ForeignGuid/Id/Key`) from prior state.
   - Replace by Guid in `GroupMemberAttributesState`. New definitions: `Order = max+1`.
   - `BindGroupMemberAttributesGrid; HideDialog;`
4. **Reorder** (`gGroupMemberAttributes_GridReorder`):
   - `ReorderAttributeList(state, e.OldIndex, e.NewIndex)` ([GroupDetail.ascx.cs#L3221](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3221)), adjusts `Order` on the moved item and shifts neighbors.
5. **Delete** (`gGroupMemberAttributes_Delete`):
   - Remove from state by Guid + bind. No confirm prompt.
6. **Save group** (`btnSave_Click`):
   - Inside the `rockContext.WrapTransaction` ([GroupDetail.ascx.cs#L1339-L1359](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1339)):
     ```csharp
     var entityTypeId = EntityTypeCache.Get( typeof( GroupMember ) ).Id;
     string qualifierColumn = "GroupId";
     string qualifierValue = group.Id.ToString();
     var attributes = attributeService.GetByEntityTypeQualifier( entityTypeId, qualifierColumn, qualifierValue, true );

     // Delete any not in state
     var selectedAttributeGuids = GroupMemberAttributesState.Select( a => a.Guid );
     foreach ( var attr in attributes.Where( a => !selectedAttributeGuids.Contains( a.Guid ) ) )
         attributeService.Delete( attr );

     // Save the rest
     foreach ( var attributeState in GroupMemberAttributesState )
         Rock.Attribute.Helper.SaveAttributeEdits( attributeState, entityTypeId, qualifierColumn, qualifierValue, rockContext );

     rockContext.SaveChanges();
     ```
   - Note: this means after the user adds an attribute and clicks Save on the dialog (not on the page), nothing has hit the DB yet. The actual `[Attribute]` row create/update only happens on `btnSave_Click`.

## Persisted state (database)

Custom group-member attributes are `[Attribute]` rows where:
- `EntityTypeId = EntityTypeCache.Get(typeof(GroupMember)).Id`
- `EntityTypeQualifierColumn = "GroupId"`
- `EntityTypeQualifierValue = thisGroupId.ToString()`

Inherited attributes (read-only grid) are `[Attribute]` rows where:
- `EntityTypeId = EntityTypeCache.Get(typeof(GroupMember)).Id`
- `EntityTypeQualifierColumn = "GroupTypeId"`
- `EntityTypeQualifierValue = ancestorGroupTypeId.ToString()` (any ancestor in the `InheritedGroupTypeId` chain).

These two sets share the `EntityTypeId` (GroupMember) but differ in qualifier column. They are mutually exclusive, the qualifier column determines whether an attribute attaches to a single group or to a group type.

The `[Attribute]` rows have child `[AttributeQualifier]` and `[Category]` join rows (handled by `Helper.SaveAttributeEdits`). Deletion via `attributeService.Delete()` cascades to those children via standard EF cascade rules.

## ReservedKeyNames

When opening the attribute editor at [GroupDetail.ascx.cs#L4409-L4412](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4409):

```csharp
var reservedKeyNames = new List<string>();
GroupMemberAttributesInheritedState.Select( a => a.Key ).ToList().ForEach( a => reservedKeyNames.Add( a ) );
GroupMemberAttributesState.Where( a => !a.Guid.Equals( attributeGuid ) ).Select( a => a.Key ).ToList().ForEach( a => reservedKeyNames.Add( a ) );
edtGroupMemberAttributes.ReservedKeyNames = reservedKeyNames.ToList();
```

So the union of inherited keys + other custom keys (excluding self) is passed to the editor as `ReservedKeyNames` to prevent collisions. A user editing an existing attribute can still keep its original key. The AttributeEditor enforces this client-side and re-validates server-side via `attribute.IsValid`.

Note: keys collide across the entire chain, not just the immediate parent. If GroupType A inherits B inherits C, an attribute defined on C with key "FavoriteColor" cannot be redefined on the group itself.

## Inherited grid loading

```csharp
while ( inheritedGroupTypeId.HasValue )
{
    var inheritedGroupType = GroupTypeCache.Get( inheritedGroupTypeId.Value );
    foreach ( var attribute in attributeService.GetByEntityTypeId( new GroupMember().TypeId, false ).AsQueryable()
        .Where( a => a.EntityTypeQualifierColumn.Equals( "GroupTypeId" )
                  && a.EntityTypeQualifierValue.Equals( inheritedGroupTypeId.Value.ToString() ) ) )
    {
        GroupMemberAttributesInheritedState.Add( new InheritedAttribute(
            attribute.Name,
            attribute.Key,
            attribute.Description,
            Page.ResolveUrl( "~/GroupType/" + attribute.EntityTypeQualifierValue ),
            inheritedGroupType.Name ) );
    }
    inheritedGroupTypeId = inheritedGroupType.InheritedGroupTypeId;
}
```

(extracted from [GroupDetail.ascx.cs#L3162-L3199](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3162))

So inherited attributes can come from **any ancestor** in the chain, not just the immediate parent. The "Inherited from {GroupType-link}" label correctly identifies which ancestor the attribute originates from. See [24-grouptype-inheritance.md](24-grouptype-inheritance.md).

## State

| Property | Type | Notes |
|---|---|---|
| `GroupMemberAttributesState` | `List<Attribute>` | Full Attribute entities, JSON-serialized into ViewState. Source of truth between postbacks. |
| `GroupMemberAttributesInheritedState` | `List<InheritedAttribute>` | Display projection (`Name`, `Key`, `Description`, `Url`, `GroupType`). JSON-serialized into ViewState. Re-derived on every GroupType change. |

Both are saved/restored at [GroupDetail.ascx.cs#L345-L363, L571-L572](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L345).

## Visibility

Final visibility decisions live in [`BindGroupMemberAttributesInheritedGrid`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L4510-L4525):

```csharp
if ( CurrentGroupTypeCache != null && wpGroupMemberAttributes.Visible )
{
    wpGroupMemberAttributes.Visible = GroupMemberAttributesInheritedState.Any()
                                      || GroupMemberAttributesState.Any()
                                      || CurrentGroupTypeCache.AllowSpecificGroupMemberAttributes;
    rcwGroupMemberAttributes.Visible = GroupMemberAttributesInheritedState.Any()
                                       || GroupMemberAttributesState.Any()
                                       || CurrentGroupTypeCache.AllowSpecificGroupMemberAttributes;
}

rcwGroupMemberAttributesInherited.Visible = GroupMemberAttributesInheritedState.Any();
rcwGroupMemberAttributes.Label = GroupMemberAttributesInheritedState.Any() ? "Group Member Attributes" : string.Empty;
```

The "if `wpGroupMemberAttributes.Visible`" guard preserves the ADMINISTRATE gate set in `ShowEditDetails`. Once `wpGroupMemberAttributes.Visible` is false (no admin), nothing here flips it back on.

## Edge cases

- **Add button always visible**: `gGroupMemberAttributes.Actions.ShowAdd = true` is set in `OnInit` and never flipped, even if `AllowSpecificGroupMemberAttributes` is false. The user can still add attributes on a GroupType that disallows specific attributes; the disallow flag controls the panel-visibility threshold, not the button. **This may be a bug**, but it's existing behavior. Open question for spec.
- **Adding a key that collides with inherited**: blocked by `ReservedKeyNames` in the AttributeEditor. The editor surfaces an inline validation message.
- **Self-edit retains key**: when editing an attribute, that attribute's own `Guid` is excluded from `ReservedKeyNames`, so the editor accepts the same key.
- **Reorder is local-only**: only the editable list reorders; the inherited list is always sorted by ancestor walk order then by attribute Order/Name.
- **Delete in dialog has no confirm**: `ShowConfirmDeleteDialog="false"` on the grid markup. Differs from most Rock grids.
- **Deleted attribute with values**: when an attribute is removed from `GroupMemberAttributesState` and the group is then saved, `attributeService.Delete()` cascades to delete the `[AttributeValue]` rows. Group members lose the value silently. No confirmation surfaces this.
- **GroupType change discards unsaved member-attribute edits**: switching GroupType in the dropdown reloads `GroupMemberAttributesState` from DB only on initial entry to edit mode, but subsequent GroupType changes only call `BindInheritedAttributes`, NOT a reload of `GroupMemberAttributesState`. **This means** unsaved custom member-attribute additions persist across GroupType changes. They will all be saved with the qualifier `GroupId = group.Id` regardless of the GroupType. This is consistent with the "GroupType is fixed once chosen" semantics for new groups, but worth flagging.
- **Group with `Id == 0` (new, unsaved)**: the qualifier value `group.Id.ToString()` evaluates to `"0"` during the staging phase. Since the Save flow only writes attributes after `groupService.Add(group)` and `SaveChanges()`, by the time `SaveAttributeEdits` runs, `group.Id` is the real id. Verified at [GroupDetail.ascx.cs#L1311-L1359](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1311).
- **Name collision via case**: qualifier matches use `Equals( "GroupId", StringComparison.OrdinalIgnoreCase )` ([line 2119](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2119)) so a stray lowercase "groupid" qualifier in the DB would also match. Saving via `Helper.SaveAttributeEdits` writes the canonical "GroupId" value.
- **Security button uses Attribute entity-type**: `groupMemberAttributeSecurityField.EntityTypeId = EntityTypeCache.GetId<Attribute>() ?? 0;` ([line 446](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L446)). So security on a member-attribute definition is per-attribute, separate from the parent Group's security.

## JS interactions

- Modal confirm cancel uses `OnCancelScript="clearActiveDialog();"` which clears `hfActiveDialog` so postbacks don't re-show the modal.
- The grid reorder handle is the standard `ReorderField` drag-drop affordance.
- No bespoke JS for the inherited grid links, they are plain `<a target="_blank" rel="noopener noreferrer">` links built server-side at [GroupDetail.ascx#L303](../../RockWeb/Blocks/Groups/GroupDetail.ascx#L303).

## Permission inheritance

| Scope | Auth |
|---|---|
| Panel visible | `group.IsAuthorized( ADMINISTRATE, CurrentPerson )` (block-level). |
| Add / Edit / Delete (in-modal) | Once panel is visible, no further auth check. |
| Security button per row | Uses Rock's standard SecurityField against the Attribute entity. The user can grant per-attribute auth (READ/EDIT) for downstream Group Member detail views. |
| Save (final) | Implicit, group save runs in same auth context. The `attributeService.Delete()` and `Helper.SaveAttributeEdits` calls do not re-check auth. |

## Phase considerations

Self-contained sub-feature. Could be its own phase, but it's small enough to bundle with another related phase (e.g., Group Requirements or Member Workflow Triggers).

## Reference

[`groupTypeDetail.partial.obs`](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupTypeDetail/) and the broader [`Rock.Blocks/Group/GroupTypeDetail.cs`](../../Rock.Blocks/Group/GroupTypeDetail.cs) handle the analogous "GroupType-level member attributes" for the GroupType detail block. The Obsidian shape there is:

- `InheritedAttributeBag` (or equivalent) carries `{ Name, Description, Key, Guid, InheritedFromGroupTypeName, InheritedFromGroupTypeUrl }`.
- The block action `GetInheritedAttributes(Guid inheritedGroupTypeGuid)` walks the chain server-side.
- Two grids, one inherited (read-only), one editable (Add/Edit/Delete/Reorder/Security).

This GroupDetail conversion can mirror that structure with one substitution: the `InheritedFromGroupTypeUrl` URL points to the GroupType detail page (Page Guid `5CD8E024-710B-4EDE-8C8C-4C9E15E6AFAB`).

## Open questions / flag for spec phase

- **Add button visibility under `AllowSpecificGroupMemberAttributes = false`**: the WebForms block keeps the Add button visible. Should the Obsidian conversion conditionally hide it (consistent with the panel-level `AllowSpecificGroupMemberAttributes` check) or preserve current behavior?
- **Delete confirmation**: WebForms block has `ShowConfirmDeleteDialog="false"`. Should the Obsidian conversion add a confirmation prompt when deleting a definition that has existing values? Answering "yes" would require a count query at delete time.
- **`InheritedFromGroupTypeUrl` for attributes inherited from a deeper ancestor**: confirm the Obsidian bag conveys the immediate ancestor (where the attribute is defined) vs the chain root. WebForms uses the immediate-ancestor's GroupType in the link (`inheritedGroupType.Name` at [line 3181](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3181)). Preserve this exactly.
- **OptionsBag size implications**: see [24-grouptype-inheritance.md](24-grouptype-inheritance.md) Open Questions section.
