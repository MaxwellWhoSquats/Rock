# Sub-feature: Archive, Delete, and Copy (plus terminal/scalar cluster)

These three terminal actions live in the view-mode toolbar. Each has nuanced rules. This file also documents two additional features that have no better home: **Group Capacity** and **Signature Document Template** (both scalar fields on the General panel).

## Delete

### Code-behind

[`btnDelete_Click`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L675) lines 675-729.

### Algorithm

1. Authorize EDIT. If not, show modal alert "You are not authorized to delete this group."
2. `groupService.CanDelete(group, out errorMessage, includeSecondaryRelatedEntities=true)`. If false, show modal alert with the error.
3. If group has a non-named (inline) `Schedule` and no other group uses it, delete the schedule too.
4. If `group.IsSecurityRoleOrSecurityGroupType()`, call `GroupService.DeleteSecurityRoleGroup(group.Id)` (which has different cascade semantics).
5. Otherwise call `groupService.Delete(group)`.
6. `SaveChanges`.
7. Navigate to GroupListPage (or returnUrl) with the parent group selected.

### Inline schedule cleanup on delete

Lines [`700-713`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L700):

```csharp
// If group has a non-named schedule, delete the schedule record.
if ( group.ScheduleId.HasValue )
{
    var scheduleService = new ScheduleService( rockContext );
    var schedule = scheduleService.Get( group.ScheduleId.Value );
    if ( schedule != null && schedule.ScheduleType != ScheduleType.Named )
    {
        // Make sure this is the only group trying to use this schedule.
        if ( !groupService.Queryable()
                          .Where( g => g.ScheduleId == schedule.Id && g.Id != group.Id )
                          .Any() )
        {
            scheduleService.Delete( schedule );
        }
    }
}
```

The orphan-Schedule check uses `ScheduleType != Named` (Custom or Weekly), and only verifies that no other Group is referencing it. It does NOT check other entity types that may reference the Schedule (e.g., Attendance.ScheduleId, RegistrationInstance.ScheduleId). This is more permissive than the save-time `CanDelete` used during inline schedule changes.

### Side effect

`groupService.Delete` automatically *archives* instead of deletes if the group has GroupHistory enabled. This block already prevents that path by hiding the Delete button when GroupHistory + history-rows-exist, so the auto-archive is just a safety net.

### Visibility

```csharp
btnDelete.Visible = !group.IsSystem
                    && Authorization.EDIT
                    && (!groupType.EnableGroupHistory
                        || (!groupHistorical.Any() && !groupMemberHistorical.Any()));
```

Plus hidden if `group.IsArchived`.

The history existence check ([`2580-2592`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2580)):

```csharp
if ( groupType != null && groupType.EnableGroupHistory )
{
    bool hasGroupHistory = new GroupHistoricalService( rockContext )
                              .Queryable()
                              .Any( a => a.GroupId == group.Id )
                          || new GroupMemberHistoricalService( rockContext )
                              .Queryable()
                              .Any( a => a.GroupId == group.Id );
    if ( hasGroupHistory )
    {
        btnDelete.Visible = false;
        btnArchive.Visible = !group.IsSystem && group.IsAuthorized( Authorization.EDIT, CurrentPerson );
    }
}
```

This runs both `GroupHistorical` and `GroupMemberHistorical` queries. If either has any rows for this group, Archive replaces Delete.

## Archive

Archive is a soft-delete alternative for groups with history tracking. It keeps the row but flips `IsArchived = true` and other archive metadata.

### Code-behind

[`btnArchive_Click`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L636) lines 636-648.

### Algorithm

1. If group has child groups (`groupService.Queryable().Any(r => r.ParentGroupId == groupId)`), show `mdArchive` dialog with two save buttons:
   - Save (`OnSaveClick="mdArchive_AllChildGroupsClick"`) labeled `"Yes"` -> archive group + all descendants.
   - SaveThenAdd (`OnSaveThenAddClick="mdArchive_SingleGroupClick"`) labeled `"No"` -> archive only this group.
2. If no child groups, skip the dialog and archive directly via `ArchiveSingleGroup`.

The dialog asks: `"Would you like to archive this group's children?"`

### ArchiveSingleGroup

Lines [`3378-3402`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3378):

```csharp
groupService.Archive( group, this.CurrentPersonAliasId, true );
rockContext.SaveChanges();
NavigateAfterDeleteOrArchive( parentGroupId );
```

The `true` flag is `withFullTransaction`.

### ArchiveAllChildGroups

Lines [`3407-3438`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3407):

```csharp
var childGroups = groupService.GetAllDescendentGroups( group.Id, true );
foreach ( var childGroup in childGroups )
    groupService.Archive( childGroup, this.CurrentPersonAliasId, true );
groupService.Archive( group, this.CurrentPersonAliasId, true );
```

`GetAllDescendentGroups(id, true)` returns the full recursive descendant tree. The `true` argument is `includeArchived` (so previously-archived descendants are not re-archived but are still touched).

### Visibility

```csharp
btnArchive.Visible = !group.IsSystem
                  && !group.IsArchived
                  && Authorization.EDIT
                  && groupType.EnableGroupHistory
                  && (groupHistorical.Any() || groupMemberHistorical.Any());
```

I.e., Archive shows when Delete would otherwise be the option but history tracking is on with existing rows.

### JS confirmation

The Archive button has class `js-archive-group` which ties to a JS confirmation handler in `.ascx` lines [`783-790`](../../RockWeb/Blocks/Groups/GroupDetail.ascx#L783):

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

Note this confirmation runs only for the toolbar button. The mdArchive dialog (for parent + children archive) does NOT go through this confirmation.

### Complete archive cascade

When `groupService.Archive(group, ..., withFullTransaction: true)` is called, the entity-level cascade depends on `GroupService.Archive`, not this block. Conceptually, the cascade includes:

| Entity | What happens | Source |
|---|---|---|
| `Group` | `IsArchived = true`, `ArchivedDateTime`, `ArchivedByPersonAliasId` set | Archive method |
| `Group.GroupMembers` | Each member's `IsArchived = true` | Archive method (recursive) |
| `GroupHistorical`, `GroupMemberHistorical` | Untouched (preserved as the whole point of archiving) | Untouched |
| Child groups | Untouched by `ArchiveSingleGroup`. Archived by `ArchiveAllChildGroups`. | Block decision |
| `GroupLocation`, `GroupSync`, `GroupRequirement`, `GroupMemberWorkflowTrigger` | Untouched (preserved on the row) | N/A |
| Inline `Schedule` | Untouched | N/A |
| `GroupMemberAssignment` | Untouched | N/A |

Archive is fundamentally non-destructive. The block makes no extra cleanup calls.

## Copy

### Code-behind

[`btnCopy_Click`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1507) lines 1507-1510 (just shows the modal).
[`mdCopyGroup_SaveClick`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1517) lines 1517-1545.

### Algorithm

1. Show `mdCopyGroup` modal with `cbCopyGroupIncludeChildGroups` (default checked).
2. On Save: authorize EDIT (otherwise show "You are not authorized to copy the group" via `nbEditModeMessage`).
3. Build `CopyGroupOptions { GroupId, IncludeChildGroups, CreatedByPersonAliasId }`.
4. Call `GroupService.CopyGroup(options)` (static).
5. Navigate to the new group's detail page.

### What gets copied (per `mdCopyWarning` text in markup)

Markup at lines [`475-501`](../../RockWeb/Blocks/Groups/GroupDetail.ascx#L475):

```
This action will copy the selected group and (optionally) it's child groups.
Group members will not be copied.

The following items will be included for every copied group:
- Group member attributes configuration
- Group attributes configuration and their values
- Schedules
- Authorization (both authorizations to the group and access to other entities based on membership to the group)
- Locations (except when the location is a group member address)
- Group requirements
- Group syncs
```

### What is newly generated (Guids, audit columns)

Source: `GroupService.GenerateGroupCopy` ([`Rock/Model/Group/Group/GroupService.cs:1942`](../../Rock/Model/Group/Group/GroupService.cs#L1942)):

| Aspect | Behavior |
|---|---|
| Group `Guid` | New Guid via `CloneWithoutIdentity()`. |
| Group `Id` | Reset to 0; assigned by EF on Add. |
| Group `Name` | For root copy: `sourceGroup.Name + " - Copy"`. For child groups: name is preserved. |
| Group `IsSystem` | Forced to `false`. |
| Group `CreatedByPersonAliasId` | Set to `copyGroupOptions.CreatedByPersonAliasId`. |
| Group `ModifiedByPersonAliasId` | Set to `copyGroupOptions.CreatedByPersonAliasId`. |
| `GroupLocation` rows | Cloned without identity. Schedules are referenced (not copied). Member-address locations (where `GroupMemberPersonAliasId.HasValue`) are excluded. |
| `Schedule` (inline) | Referenced via `GetSchedule(sourceGroup.Schedule)` — same instance, not duplicated. |
| `GroupMember` attributes | Cloned with new Guid; `EntityTypeQualifierValue` set to new GroupId; `IsSystem = false`. Qualifiers cloned similarly. |
| `Auth` rows | Cloned without identity. EntityId remapped to new group, GroupId remapped to new group. |
| `GroupSync` rows | Cloned without identity. |
| `GroupRequirement` rows | Cloned without identity. |
| Group attributes & values | Copied via `Helper.CopyAttributes`. Group-typed attributes are remapped through `groupGuidDictionary` so internal cross-references remain consistent. |
| Child groups | Recursive call to `GenerateGroupCopy` with new `parentGroupId` for each. |

### What does NOT get copied

- Group members (the warning text says so explicitly).
- `GroupMemberHistorical` and `GroupHistorical` rows.
- `GroupMemberAssignment` (no scheduling assignments are duplicated).
- `LastRefreshDateTime` on GroupSync (handled by sync job).
- Member-address `GroupLocation` rows (per the `Where( l => l.GroupMemberPersonAliasId == null )` filter).

### CopyGroupOptions structure

```csharp
[RockInternal( "17.0" )]
public class CopyGroupOptions
{
    public int GroupId { get; set; }
    public bool IncludeChildGroups { get; set; }
    public int? CreatedByPersonAliasId { get; set; }
}
```

Located at [`Rock/Model/Group/Group/Options/CopyGroupOptions.cs`](../../Rock/Model/Group/Group/Options/CopyGroupOptions.cs).

### Auth check behavior

The auth check is local to `mdCopyGroup_SaveClick`:

```csharp
var currentGroup = GetGroup( hfGroupId.Value.AsInteger() );
if ( currentGroup != null && !currentGroup.IsAuthorized( Authorization.EDIT, CurrentPerson ) )
{
    nbEditModeMessage.Visible = true;
    nbEditModeMessage.Text = "You are not authorized to copy the group";
    return;
}
```

There is no per-child-group auth check during the copy. If the user has EDIT on the parent but not on a child group, the child still gets copied. Whether that is a security concern depends on org policy.

After the copy completes, `Rock.Security.Authorization.Clear()` is called (since auth rows were inserted).

### Visibility

```csharp
btnCopy.Visible = ShowCopyButton block attribute && Authorization.EDIT;
```

Set in two places:
- [`504`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L504) on initial load (just checks the block attribute).
- [`1656`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1656) on `Block_BlockUpdated` (also checks Authorization.EDIT).

The first-load visibility lets non-admin users see the button before the auth check in `mdCopyGroup_SaveClick` blocks them. Slight asymmetry; the new block should consolidate to "always check both at render time."

## Navigation after Delete or Archive

Lines [`735-761`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L735):

```csharp
private void NavigateAfterDeleteOrArchive( int? parentGroupId )
{
    var returnUrl = PageParameter( ReturnUrl );
    if ( returnUrl.IsNotNullOrWhiteSpace() )
    {
        Response.Redirect( returnUrl );
        return;
    }

    var qryParams = new Dictionary<string, string>();
    if ( parentGroupId != null )
        qryParams[GroupId] = parentGroupId.ToString();
    qryParams[ExpandedIds] = PageParameter( ExpandedIds );

    if ( GetAttributeValue( GroupListPage ).AsGuid() != Guid.Empty )
        NavigateToLinkedPage( GroupListPage, qryParams );
    else
        NavigateToPage( RockPage.Guid, qryParams );
}
```

This pattern (returnUrl-or-parent-or-page-attr) recurs throughout the block.

## Phase considerations

Archive and Delete are conceptually simple but tightly tied to the View panel. They should land in whichever phase delivers the View panel.

Copy is the most isolated of the three and has the lowest risk surface. Could go in an early phase as soon as the view panel exists.

---

## Group Capacity (lives in wpGeneral)

Group Capacity does not have a dedicated panel; it is a single number-box control in the General panel. Documenting it here because no other research file covers it.

### Markup

[`RockWeb/Blocks/Groups/GroupDetail.ascx#L114`](../../RockWeb/Blocks/Groups/GroupDetail.ascx#L114):

```xml
<Rock:NumberBox ID="nbGroupCapacity" runat="server"
                Label="Group Capacity"
                NumberType="Integer"
                MinimumValue="0" />
```

There is also a `nbGroupCapacityMessage` notification box declared in markup at line [`56`](../../RockWeb/Blocks/Groups/GroupDetail.ascx#L56):

```xml
<Rock:NotificationBox ID="nbGroupCapacityMessage" runat="server"
                      NotificationBoxType="Warning" Visible="false" />
```

**This notification box is never set or shown by the code-behind.** A search for `nbGroupCapacityMessage` in `GroupDetail.ascx.cs` returns zero matches. It appears to be dead markup. The new block can omit it.

### Visibility and Required state

In `ShowEditDetails` at [`2071-2072`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2071) and `ShowGroupTypeEditDetails` at [`2205-2212`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2205):

```csharp
nbGroupCapacity.Visible = groupType != null && groupType.GroupCapacityRule != GroupCapacityRule.None;
nbGroupCapacity.Help    = nbGroupCapacity.Visible ? GetGroupCapacityHelpText( groupType.GroupCapacityRule ) : string.Empty;
nbGroupCapacity.Required = groupType.IsCapacityRequired;
```

### GroupCapacityRule enum

Located at [`Rock.Enums/Group/GroupCapacityRule.cs`](../../Rock.Enums/Group/GroupCapacityRule.cs):

```csharp
public enum GroupCapacityRule
{
    None = 0,  // Group does not have capacity limitations
    Hard = 1,  // Group cannot go over capacity
    Soft = 2   // Warning shown if over, but additional members can still be added
}
```

### GetGroupCapacityHelpText

Lines [`1914-1927`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1914):

```csharp
private string GetGroupCapacityHelpText( GroupCapacityRule groupCapacityRule )
{
    if ( groupCapacityRule == GroupCapacityRule.Soft )
    {
        return "The number of people that can be added to the group. " +
               "Once the capacity is reached, a warning will appear in the Group Toolbox " +
               "but additional group members can still be added.";
    }

    if ( groupCapacityRule == GroupCapacityRule.Hard )
    {
        return "The number of people that can be added to the group. " +
               "Once the capacity is reached no additional group members can be added.";
    }

    return string.Empty;
}
```

The help text is per-rule. For `None`, the field is hidden so help is irrelevant.

### Interaction with GroupType.GroupCapacityRule

| `GroupType.GroupCapacityRule` | `GroupType.IsCapacityRequired` | UI | Save |
|---|---|---|---|
| `None` | (any) | Field hidden | `group.GroupCapacity = nbGroupCapacity.Text.AsIntegerOrNull()` (effectively null since hidden) |
| `Soft` | false | Field shown, help = soft text, optional | Number or null saved |
| `Soft` | true | Field shown, required marker, validation enforced | Number required |
| `Hard` | false | Field shown, help = hard text, optional | Number or null saved |
| `Hard` | true | Field shown, required, validation | Number required |

Note: `IsCapacityRequired` is a separate `GroupType` flag. It does not imply the rule is Hard or Soft. It just determines whether the group capacity field is required.

### Persisted

| Column | Type | Notes |
|---|---|---|
| `Group.GroupCapacity` | int? | Null if rule is None or user left field empty (and IsCapacityRequired is false). |

The actual enforcement of the rule happens in other Rock code (Group Toolbox, GroupMember add validation) and not in this block. This block only collects the configured value.

### Save logic

[`1056`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1056):

```csharp
group.GroupCapacity = nbGroupCapacity.Text.AsIntegerOrNull();
```

Always runs regardless of `GroupCapacityRule`. If the user left the field blank or hidden, this stores null.

---

## Communication preference / signature document template (lives in wpGeneral)

The "Require Signed Document" dropdown in the General panel ties the group to a legacy `SignatureDocumentTemplate`. Documenting here because no other research file covers it.

### Markup

[`RockWeb/Blocks/Groups/GroupDetail.ascx#L129`](../../RockWeb/Blocks/Groups/GroupDetail.ascx#L129):

```xml
<Rock:RockDropDownList ID="ddlSignatureDocumentTemplate" runat="server"
                       Label="Require Signed Document"
                       Help="If members of this group need to have signed a document, select that document type here." />
```

### Population

`LoadDropDowns` ([`3019-3027`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3019)):

```csharp
ddlSignatureDocumentTemplate.Items.Clear();
ddlSignatureDocumentTemplate.Items.Add( new ListItem() );

foreach ( var documentType in new SignatureDocumentTemplateService( rockContext ).GetLegacyTemplates() )
{
    ddlSignatureDocumentTemplate.Items.Add( new ListItem( documentType.Name, documentType.Id.ToString() ) );
}
```

### Why "legacy templates" specifically

[`Rock/Model/Core/SignatureDocumentTemplate/SignatureDocumentTemplateService.cs:125-128`](../../Rock/Model/Core/SignatureDocumentTemplate/SignatureDocumentTemplateService.cs#L125):

```csharp
public IQueryable<SignatureDocumentTemplate> GetLegacyTemplates()
{
    return Queryable()
        .AsNoTracking()
        .Where( t => t.ProviderEntityTypeId.HasValue )
        .OrderBy( t => t.Name );
}
```

A "legacy" template is one with a non-null `ProviderEntityTypeId`. These are document types that integrate with an external e-signature provider (e.g., the deprecated SignNow integration).

Modern (electronic) signature document templates have `ProviderEntityTypeId == null` and use Rock's built-in signature flow. The Group's `RequiredSignatureDocumentTemplateId` only supports the legacy flow because the modern flow is event-driven, not persistent on the group.

### Save logic

[`1057`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1057):

```csharp
group.RequiredSignatureDocumentTemplateId = ddlSignatureDocumentTemplate.SelectedValueAsInt();
```

Stores null if user picked the blank item.

### Persisted

| Column | Type | Notes |
|---|---|---|
| `Group.RequiredSignatureDocumentTemplateId` | int? | FK to `SignatureDocumentTemplate.Id`. No cascade. |

### Relationship to PersonSignatureDocument entity

Setting this column does NOT itself create any `PersonSignatureDocument` rows. The behavior is policy-only: the group's setting tells the rest of Rock that members of this group should have signed this document. Other systems (workflow, reports, batch jobs) consume `Group.RequiredSignatureDocumentTemplateId` to enforce the policy. This block does not create, validate, or modify any `PersonSignatureDocument` entity.

### Edit-mode value population

[`1997`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1997):

```csharp
ddlSignatureDocumentTemplate.SetValue( group.RequiredSignatureDocumentTemplateId );
```

If the group's saved template Id is no longer in the legacy list (e.g., template was migrated to the new system), the dropdown will silently display blank.

### Open question for spec phase

- Should the new block expose modern signature document templates here too, or stay aligned with the legacy semantics?

---

## Open questions / flag for spec phase

- The `nbGroupCapacityMessage` markup is dead. Confirm it can be omitted in the new block.
- The Copy auth check is per-root-group only. Confirm whether per-child auth should be added.
- The Archive/Delete visibility logic runs `GroupHistorical` and `GroupMemberHistorical` queries on every render of the view panel. Consider whether a single existence query is acceptable or if both need to remain separate.
- The `Schedule.ScheduleType != Named` check on delete is functionally similar to `Name == empty` used during edit. Confirm whether they should be unified.
- The legacy-templates filter on Signature Document Template excludes modern templates. Spec needs to confirm whether the new block should expose both.
