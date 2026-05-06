# Reference Blocks (Already-Converted)

The single best reference for converting GroupDetail is the existing `GroupTypeDetail` Obsidian block. It is the most structurally similar block in the codebase and lives in the same domain.

## GroupTypeDetail (primary reference)

| Aspect | Value |
|---|---|
| C# class | `Rock.Blocks/Group/GroupTypeDetail.cs` (2,031 lines) |
| Obsidian SFC | `Rock.JavaScript.Obsidian.Blocks/src/Group/groupTypeDetail.obs` (239 lines) |
| Bag | `Rock.ViewModels/Blocks/Group/GroupTypeDetail/GroupTypeBag.cs` |
| Options bag | `Rock.ViewModels/Blocks/Group/GroupTypeDetail/GroupTypeDetailOptionsBag.cs` |
| Partials directory | `Rock.JavaScript.Obsidian.Blocks/src/Group/GroupTypeDetail/` |
| Base class | `RockEntityDetailBlockType<GroupType, GroupTypeBag>` |
| Implements | `IBreadCrumbBlock` |

### Partial files structure

`Rock.JavaScript.Obsidian.Blocks/src/Group/GroupTypeDetail/`:
- `editPanel.partial.obs` — main edit form (analogous to `pnlEditDetails`).
- `viewPanel.partial.obs` — main view rendering (replaces server-rendered Lava).
- `groupAttributes.partial.obs` — embedded sub-panel for group-attribute defs.
- `groupMemberAttributes.partial.obs` — embedded sub-panel for member-attribute defs.
- `groupTypeAttributes.partial.obs` — embedded sub-panel for group-type-attribute defs.
- `groupMemberWorkflows.partial.obs` — embedded sub-panel for triggers.
- `groupRequirements.partial.obs` — embedded sub-panel for requirements.
- `roles.partial.obs` — embedded sub-panel for roles.
- `types.partial.ts` — TypeScript helpers (NavigationUrlKey enum, etc.).
- `utility.partial.ts` — TypeScript helpers (defaults, formatters).

This is the partials pattern GroupDetail will follow. Each major panel widget will become a `.partial.obs` file.

### Top-level .obs structure (canonical)

From `groupTypeDetail.obs`:

```vue
<template>
    <NotificationBox v-if="blockError" alertType="warning" v-html="blockError" />
    <NotificationBox v-if="errorMessage" alertType="danger" v-html="errorMessage" />

    <DetailBlock v-if="!blockError"
        v-model:mode="panelMode"
        :name="panelName"
        :labels="blockLabels"
        :entityKey="entityKey"
        :isAuditHidden="false"
        :isBadgesVisible="true"
        :isDeleteVisible="isEditable"
        :isEditVisible="isEditable"
        :isFollowVisible="true"
        :isSecurityHidden="false"
        :showExperienceMode="true"
        @cancelEdit="onCancelEdit"
        @delete="onDelete"
        @edit="onEdit"
        @save="onSave">
        <template #view>
            <ViewPanel :modelValue="groupTypeViewBag" :options="options" />
        </template>
        <template #edit>
            <EditPanel v-model="groupTypeEditBag" :options="options" @propertyChanged="baseBlock.onPropertyChanged" />
        </template>
    </DetailBlock>
</template>
```

This same shell is what GroupDetail's top-level `.obs` will look like.

### C# block patterns to copy

From `GroupTypeDetail.cs`:

#### Initialization

```csharp
public override object GetObsidianBlockInitialization()
{
    var box = new DetailBlockBox<GroupTypeBag, GroupTypeDetailOptionsBag>();
    SetBoxInitialEntityState( box );
    box.NavigationUrls = GetBoxNavigationUrls();
    box.Options = GetBoxOptions();
    return box;
}
```

#### Bag construction (split common / view / edit)

```csharp
private GroupTypeBag GetCommonEntityBag( GroupType entity ) { ... }

protected override GroupTypeBag GetEntityBagForView( GroupType entity )
{
    var bag = GetCommonEntityBag( entity );
    bag.LoadAttributesAndValuesForPublicView( entity, RequestContext.CurrentPerson, enforceSecurity: true );
    return bag;
}

protected override GroupTypeBag GetEntityBagForEdit( GroupType entity )
{
    var bag = GetCommonEntityBag( entity );
    bag.LoadAttributesAndValuesForPublicEdit( entity, RequestContext.CurrentPerson, enforceSecurity: true );
    LoadAttributesForLocalEntities( entity.Id, bag ); // group/groupmember/grouptype attribute DEFINITIONS
    return bag;
}
```

#### Partial-update via ValidPropertiesBox

```csharp
protected override bool UpdateEntityFromBox( GroupType entity, ValidPropertiesBox<GroupTypeBag> box )
{
    box.IfValidProperty( nameof( box.Bag.Name ), () => entity.Name = box.Bag.Name );
    box.IfValidProperty( nameof( box.Bag.Description ), () => entity.Description = box.Bag.Description );
    // ... one IfValidProperty per scalar field
    box.IfValidProperty( nameof( box.Bag.AttributeValues ), () => {
        entity.LoadAttributes( RockContext );
        entity.SetPublicAttributeValues( box.Bag.AttributeValues, RequestContext.CurrentPerson, enforceSecurity: true );
    });
    return true;
}
```

This pattern enables partial bag updates (only fields the client touched).

#### Initial entity loading

```csharp
protected override GroupType GetInitialEntity()
{
    var entity = GetInitialEntity<GroupType, GroupTypeService>( RockContext, PageParameterKey.GroupTypeId );
    ApplyNewGroupTypeDefaultValues( entity );
    return entity;
}
```

#### Edit-action entity loading (with auth check)

```csharp
protected override bool TryGetEntityForEditAction( string idKey, out GroupType entity, out BlockActionResult error )
{
    var entityService = new GroupTypeService( RockContext );
    error = null;

    if ( idKey.IsNotNullOrWhiteSpace() )
    {
        entity = entityService.Get( idKey, !PageCache.Layout.Site.DisablePredictableIds );
    }
    else
    {
        entity = new GroupType();
        entityService.Add( entity );
        ApplyNewGroupTypeDefaultValues( entity, entityService );
    }

    if ( entity == null )
    {
        error = ActionBadRequest( $"{GroupType.FriendlyTypeName} not found." );
        return false;
    }

    if ( !entity.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) )
    {
        error = ActionBadRequest( $"Not authorized to edit {GroupType.FriendlyTypeName}." );
        return false;
    }

    return true;
}
```

#### Related-entity diff/upsert helper

```csharp
private void SyncRelatedEntities<TEntity, TBag, TKey>(
    Service<TEntity> service,
    IQueryable<TEntity> existingEntitiesQuery,
    IEnumerable<TBag> incomingBags,
    Func<TEntity, TKey> existingKeySelector,
    Func<TBag, TKey> incomingKeySelector,
    Func<TBag, TEntity> createNew,
    Action<TEntity, TBag> updateEntity ) where TEntity : Entity<TEntity>, new()
{
    var existingEntities = existingEntitiesQuery.ToList();
    var existingByKey = existingEntities.ToDictionary( existingKeySelector );

    var incomingList = ( incomingBags ?? Enumerable.Empty<TBag>() ).ToList();
    var incomingKeys = incomingList.Select( incomingKeySelector ).ToHashSet();

    foreach ( var entity in existingEntities.Where( e => !incomingKeys.Contains( existingKeySelector( e ) ) ).ToList() )
    {
        service.Delete( entity );
    }

    foreach ( var bag in incomingList )
    {
        var key = incomingKeySelector( bag );
        if ( !existingByKey.TryGetValue( key, out var entity ) )
        {
            entity = createNew( bag );
            service.Add( entity );
        }
        updateEntity( entity, bag );
    }
}
```

This generic is reusable for GroupLocations, GroupRequirements, GroupSyncs, MemberWorkflowTriggers — every collection in GroupDetail.

#### Options bag

```csharp
private GroupTypeDetailOptionsBag GetBoxOptions()
{
    var options = new GroupTypeDetailOptionsBag()
    {
        IsChatEnabledSystem = ChatHelper.IsChatEnabled,
        // ... pre-loaded dropdown options
    };
    options.IsIndexingOptionAvailable = IndexContainer.IndexingEnabled;
    return options;
}
```

This is the Obsidian way to push "platform-wide configuration" to the client (e.g., "is chat enabled?", "what categories of communications exist?").

#### Validation pre-save

```csharp
private bool ValidateGroupType( GroupType groupType, GroupTypeBag bag, out string errorMessage )
{
    // Cross-cutting business rules that need both the entity and the bag.
    // Returns false with errorMessage on failure.
}
```

### Markup conversions

These Obsidian components substitute for WebForms equivalents:

| WebForms | Obsidian |
|---|---|
| `<Rock:DataTextBox>` | `<TextBox>` (or `<RockTextBox>`) |
| `<Rock:RockCheckBox>` | `<CheckBox>` |
| `<Rock:RockDropDownList>` | `<DropDownList>` |
| `<Rock:DataDropDownList>` | `<DropDownList>` with options bag |
| `<Rock:GroupPicker>` | `<GroupPicker>` |
| `<Rock:DefinedValuePicker>` | `<DefinedValuePicker>` |
| `<Rock:NumberBox>` | `<NumberBox>` |
| `<Rock:PersonPicker>` | `<PersonPicker>` |
| `<Rock:CampusPicker>` | `<CampusPicker>` |
| `<Rock:Switch>` | `<Switch>` |
| `<Rock:ImageUploader>` | `<ImageUploader>` |
| `<Rock:DataViewItemPicker>` | `<DataViewPicker>` |
| `<Rock:DayOfWeekPicker>` | (use a custom dropdown bound to DaysOfWeek enum) |
| `<Rock:TimePicker>` | `<TimePicker>` |
| `<Rock:SchedulePicker>` | `<SchedulePicker>` |
| `<Rock:ScheduleBuilder>` | `<ScheduleBuilder>` |
| `<Rock:LocationPicker>` | `<LocationPicker>` |
| `<Rock:WorkflowTypePicker>` | `<WorkflowTypePicker>` |
| `<Rock:GroupRolePicker>` | `<GroupRolePicker>` |
| `<Rock:RangeSlider>` | `<RangeSlider>` |
| `<Rock:IntervalPicker>` | `<IntervalPicker>` |
| `<Rock:RockRadioButtonList>` | `<RadioButtonList>` |
| `<Rock:RockCheckBoxList>` | `<CheckBoxList>` |
| `<Rock:HighlightLabel>` | `<HighlightLabel>` |
| `<Rock:NotificationBox>` | `<NotificationBox>` |
| `<Rock:PanelWidget>` | `<Panel>` (collapsible) |
| `<Rock:Grid>` | `<Grid>` with column config |
| `<Rock:ModalDialog>` | `<Modal>` |
| `<Rock:TagList>` | `<TagList>` |
| `<Rock:BadgeListControl>` | (handled by DetailBlock's `:isBadgesVisible="true"`) |
| `<Rock:PanelDrawer>` | (handled by DetailBlock's `:isAuditHidden="false"`) |
| `<Rock:DynamicPlaceholder>` (attributes) | `<AttributeValuesContainer>` |
| `<Rock:AttributeEditor>` | `<AttributeEditor>` (reused inside modals) |

### onSave with redirect-on-create

From `groupTypeDetail.obs` `onSave`:

```typescript
async function onSave(): Promise<boolean | string> {
    // ...
    const result = await invokeBlockAction<ValidPropertiesBox<GroupTypeBag> | string>("Save", {
        box: groupTypeEditBag.value
    });

    if (result.isSuccess && result.data) {
        if (result.statusCode === 200 && typeof result.data === "object") {
            // 200 = update; stay on page in view mode
            groupTypeViewBag.value = result.data.bag;
            return true;
        }
        else if (result.statusCode === 201 && typeof result.data === "string") {
            // 201 = create; redirect to the new entity's URL
            return result.data;
        }
    }

    errorMessage.value = result.errorMessage ?? "Unknown error while trying to save group type.";
    return false;
}
```

So the C# Save block action returns either a bag (200) or a redirect URL (201) depending on whether it was an update or a create.

## Other potential references

| Block | Why useful | Notes |
|---|---|---|
| `Rock.Blocks/Group/GroupRequirementTypeDetail.cs` | Same domain, simpler | Pure-CRUD, single entity |
| `Rock.Blocks/Group/GroupArchivedList.cs` | Group archive surface | List-style, lighter |
| `Rock.Blocks/Cms/PageDetail.cs` (if exists) | Big detail block, similar size | Confirm with grep |
| `Rock.Blocks/Crm/PersonDetail/...` | Most complex detail block in the codebase | If converted, gives multi-tab patterns |

For sub-feature shapes:

| Sub-feature | Reference block (in same project, Obsidian) |
|---|---|
| Group Requirements editor | `groupRequirements.partial.obs` (in GroupTypeDetail) |
| Member Workflow Triggers | `groupMemberWorkflows.partial.obs` (in GroupTypeDetail) |
| Roles | `roles.partial.obs` (in GroupTypeDetail) |
| Group Member Attribute defs | `groupMemberAttributes.partial.obs` (in GroupTypeDetail) |
| Group Attribute defs | `groupAttributes.partial.obs` (in GroupTypeDetail) |

These partials can be near-verbatim ported when their corresponding GroupDetail panels need to ship.

## What to NOT copy from GroupTypeDetail

- It's a single Save action that handles all child collections in one transaction. GroupDetail is bigger and might benefit from breaking some sub-features into their own block actions (e.g., `SaveGroupSyncs`, `SaveLocations`). However, the current Rock convention is single-Save with `ValidPropertiesBox`, so this should only be deviated from with strong reason.
- It does not have an Archive action (GroupType isn't archivable). GroupDetail needs an additional `Archive` block action.
- It does not have a Copy action. GroupDetail needs an additional `Copy` block action.
