# Sub-feature: Group Attribute Values

## What it is

Group attributes are dynamically configured custom fields attached to the Group entity, defined per group type. The block renders editors for each attribute the user is authorized to edit, and persists their values via the standard `AttributeValuesContainer` pattern.

This is the simpler of the two attribute panels. It manages **values**, not **definitions**. (Member attribute definitions are in [10-member-attributes.md](10-member-attributes.md).)

The same pool of attribute *definitions* is used by [11-group-requirements.md](11-group-requirements.md) (date-typed attributes feed the "Due Date Group Attribute" dropdown), see [`BindInheritedAttributes`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3157) for that companion role.

## Trigger conditions

The "Group Attribute Values" PanelWidget appears for both new and existing groups, with these gates:

| Gate | Source | Effect |
|---|---|---|
| `group.Attributes != null && group.Attributes.Any()` | [GroupDetail.ascx.cs#L2278](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2278) | Hidden if the chosen GroupType has no Group-targeted attribute definitions. |
| `excludeForEdit.Count() < group.Attributes.Count()` | [GroupDetail.ascx.cs#L2284](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2284) | Hidden if the user has no EDIT auth on any attribute definition. |
| Edit mode | `pnlEditDetails.Visible` (set by [`SetEditMode`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2901)) | Only ever shown in Edit mode. The view (read-only) panel renders attribute values via the GroupType's `GroupViewLavaTemplate` (see [GroupDetail.ascx.cs#L2823](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2823)) instead. |

The panel re-renders on **every GroupType dropdown change** because [`ShowGroupTypeEditDetails`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2173) is called from [`ddlGroupType_SelectedIndexChanged`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1556).

## Markup region

[`RockWeb/Blocks/Groups/GroupDetail.ascx`](../../RockWeb/Blocks/Groups/GroupDetail.ascx) lines 289-291.

```
wpGroupAttributes (PanelWidget, title "Group Attribute Values")
└── DynamicPlaceholder phGroupAttributes
```

The DynamicPlaceholder is filled at runtime by [`Rock.Attribute.Helper.AddEditControls`](../../Rock/Attribute/Helper.cs).

## Code-behind summary

| Method | File:Line | Notes |
|---|---|---|
| `ShowGroupTypeEditDetails` body | [GroupDetail.ascx.cs#L2275-L2292](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2275) | `phGroupAttributes.Controls.Clear(); group.LoadAttributes(); AddEditControls(group, ph, setValues, BlockValidationGroup, excludeForEdit)`. Excludes attributes the user can't EDIT. Hides the entire panel widget if all attributes are excluded. |
| `OnLoad` postback re-bind | [GroupDetail.ascx.cs#L522-L531](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L522) | On every postback while details panel is visible, reconstructs the attribute editor controls from a stub `Group { GroupTypeId = CurrentGroupTypeId }`. This is the standard WebForms attribute-control rehydration. |
| `btnSave_Click` getEditValues | [GroupDetail.ascx.cs#L1260-L1261](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1260) | `group.LoadAttributes(); Helper.GetEditValues( phGroupAttributes, group );`. |
| `btnSave_Click` save | [GroupDetail.ascx.cs#L1336](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1336) | `group.SaveAttributeValues( rockContext )`, called inside the WrapTransaction. |

## Server flow (Edit -> Save)

1. **Edit (or GroupType change)**:
   - `ShowGroupTypeEditDetails(groupType, group, setValues=true)` runs at [GroupDetail.ascx.cs#L2173](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2173).
   - Calls `phGroupAttributes.Controls.Clear()` then `group.LoadAttributes()`.
   - Computes `excludeForEdit` = list of attribute keys where the current user fails Authorization.EDIT on the AttributeCache for that attribute.
   - Calls `Rock.Attribute.Helper.AddEditControls( group, phGroupAttributes, setValues, BlockValidationGroup, excludeForEdit )`.
   - Sets `wpGroupAttributes.Visible` to false if every attribute was excluded for edit.
2. **Save (`btnSave_Click`)**:
   - At [GroupDetail.ascx.cs#L1260](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1260), reloads attributes onto the group instance and calls `Helper.GetEditValues( phGroupAttributes, group )` to read values back from the rendered controls into `group.AttributeValues`.
   - Inside the WrapTransaction at [GroupDetail.ascx.cs#L1336](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1336), calls `group.SaveAttributeValues( rockContext )`. This persists every attribute on the group regardless of which subset was rendered (the standard Rock pattern; values for excluded attributes are unchanged).

## Visibility expression

```csharp
wpGroupAttributes.Visible =
    group.Attributes != null
    && group.Attributes.Any()
    && excludeForEdit.Count() < group.Attributes.Count();
```

(reconstructed from [GroupDetail.ascx.cs#L2280-L2292](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2280))

## Persisted state (database)

Group attributes targeted to the Group entity live in the `[Attribute]` table with:
- `EntityTypeId = EntityTypeCache.Get(typeof(Group)).Id`
- `EntityTypeQualifierColumn = "GroupTypeId"`
- `EntityTypeQualifierValue = thisGroupType.Id.ToString()` (and qualifier is matched case-insensitively per [GroupDetail.ascx.cs#L3187](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3187))

Note: The `[Attribute]` rows are owned by the **GroupType**, not the group. The block does not create or modify these rows; that lives in the GroupType detail block ([Rock.Blocks/Group/GroupTypeDetail.cs](../../Rock.Blocks/Group/GroupTypeDetail.cs)). What this block writes to is the `[AttributeValue]` table, keyed by `(AttributeId, EntityId=group.Id)`.

Inheritance walk for Group attributes (used **only** for the "Due Date Group Attribute" dropdown when the group is null/new) is in [`BindInheritedAttributes`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3185-L3192). For an existing group, the dropdown reads `group.Attributes` directly, which already includes any inherited values via `LoadAttributes`. See [11-group-requirements.md](11-group-requirements.md).

## Edit-vs-View modes

- **Edit mode**: editable form controls rendered into `phGroupAttributes`.
- **View mode** (read-only, line 444 fieldset): the GroupType's `GroupViewLavaTemplate` renders attribute values inline as part of `lContent` ([GroupDetail.ascx.cs#L2821-L2825](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2821)).

## Permission inheritance

| Action | Auth gate |
|---|---|
| Render the panel at all | EDIT auth implied (`pnlEditDetails.Visible == true`). |
| Render an individual attribute editor | Per-attribute `IsAuthorized( Authorization.EDIT, CurrentPerson )` test against the `AttributeCache`. Attributes that fail are added to `excludeForEdit`, which `Helper.AddEditControls` skips. |
| Save attribute values | The block save unconditionally writes back what the rendered editors gave it. Values for `excludeForEdit` attributes are unchanged because they were not in the rendered set. There is **no** per-attribute auth re-check at save time. (Same as nearly every other Rock block.) |

## Edge cases

- **No attributes defined**: panel hidden ([GroupDetail.ascx.cs#L2289-L2292](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2289)).
- **All attributes excluded by EDIT auth**: panel hidden ([GroupDetail.ascx.cs#L2284-L2287](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2284)).
- **GroupType change while editing**: `ShowGroupTypeEditDetails(setValues=true)` clears and rebuilds the placeholder; any unsaved values from the prior GroupType are dropped on the floor (the user picked a different type, so this is expected). The `setValues=true` path in `AddEditControls` resets to `attribute.DefaultValue`.
- **Postback rebuild without setValues**: `ShowGroupTypeEditDetails(setValues=false)` from the OnLoad postback handler at [GroupDetail.ascx.cs#L529](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L529) reconstructs the controls from a stub `Group { GroupTypeId = CurrentGroupTypeId }` (`group.Id == 0`, no values populated) so that ASP.NET ViewState rehydrates the user's typed values back into the controls before save handlers run.
- **Required validation**: enforced inside `AddEditControls` based on each attribute's `IsRequired` flag.

## JS interactions

None specific to this sub-feature. Attribute editors include their own client-side validation (e.g., date pickers, integer textboxes) but nothing block-specific.

## In Obsidian

This becomes:
- Bag holds the attribute values via `AttributeValuesContainer` (or whatever the latest helper bag is).
- `.obs` template uses `<AttributeValuesContainer>` component, which reads/writes the bag.
- The C# block populates the bag during `GetEntityBagForView` / `GetEntityBagForEdit` and persists it during the save action.

See [`Rock.Blocks/Group/GroupTypeDetail.cs`](../../Rock.Blocks/Group/GroupTypeDetail.cs) for the canonical pattern in this domain.

## Phase considerations

- Trivial in isolation (one DynamicPlaceholder + value persistence) but only works in a phase that has the `Group` entity edit lifecycle wired up.
- Should be in the "core edit form" phase (alongside Name, Description, etc.).

## Notes

- `Helper.AddEditControls` excludes any attribute where the user does not have EDIT auth. The remaining are rendered editable.
- The save unconditionally persists everything that came back, regardless of which subset was rendered. (Standard Rock pattern.)
- Loaded values are always full-chain-resolved by `LoadAttributes`, so attributes inherited via `GroupType.InheritedGroupTypeId` are visible alongside locally-defined ones automatically. See [24-grouptype-inheritance.md](24-grouptype-inheritance.md).

## Open questions / flag for spec phase

- **Bag shape**: depending on the Approach A/B/C decision in [22-grouptype-cascade.md](22-grouptype-cascade.md), the attribute set delivered via the OptionsBag may need to support GroupType-keyed dictionaries OR be lazy-loaded per current GroupType. Approach A (front-load) means every GroupType's group-attribute definitions are bundled, see size implications in [24-grouptype-inheritance.md](24-grouptype-inheritance.md).
- **Save-time auth**: confirm whether the Obsidian conversion should add a per-attribute EDIT recheck at save time (defensive) or preserve the WebForms behavior (no recheck, relying on the rendered subset).
- **Excluded-for-edit semantics**: WebForms hides excluded attributes entirely. Should the Obsidian version render them as read-only instead, when the user has VIEW but not EDIT? Today the WebForms block does not, so changing it would be a behavior change. Flag for product decision.
