# Open Question: GroupType-Change Reactive Cascade

This document explains the remaining open Phase 0 decision the user asked to defer. Read this in full before deciding.

## Why this is a decision

`GroupDetail`'s edit form is heavily shaped by the currently selected GroupType. Many panels appear or disappear, several controls change their values or options, the inherited-attributes list is re-derived, the requirements grid re-renders, and the administrator picker's label changes. Today this happens via a WebForms autopostback every time the user picks a different GroupType from the dropdown.

In Obsidian we don't have autopostbacks. We have to choose how the client reshapes itself when `currentGroupTypeId` changes:
- **Approach A**: front-load every group type's settings into the initial OptionsBag, and let Vue computed properties drive everything reactively (zero round trips).
- **Approach B**: fire a Block Action on each GroupType change to fetch just that type's settings (one round trip per change).
- **Approach C**: hybrid — initial-load for the current type, lazy-load + cache on subsequent changes.

Each is implementable. They differ on payload size, latency, complexity, and freshness semantics.

---

## What the WebForms cascade actually does

When `ddlGroupType_SelectedIndexChanged` fires (line 1556-1575 of `GroupDetail.ascx.cs`), the block re-runs the following methods in order, all driven by the new `GroupTypeCache`:

| Method | Lines | Effect |
|---|---|---|
| `SetRecordSourceControls` | 2371-2384 | Show/hide RecordSource ddl, populate options |
| `SetPeerNetworkControls` | 2391-2442 | Show/hide entire peer network override panel; load relationship strength, growth, and 4 multipliers; set placeholder text from group type defaults |
| `SetRsvpControls` | 2463-2505 | Show/hide RSVP panel; set read-only state on offset days and reminder communication based on whether group type has values |
| `SetScheduleControls` | 2302-2364 | Reset schedule controls; rebuild ScheduleSelect radio options from `groupType.AllowedScheduleTypes` flags; set `IsScheduleTabVisible` |
| `ShowGroupTypeEditDetails` | 2173-2295 | Locations panel visibility (LocationSelectionMode, EnableLocationSchedules), capacity required + help, status DefinedType picker label and options, group attributes editor (DynamicPlaceholder rebuild), scheduling panel visibility, sync panel visibility, member workflow triggers panel visibility |
| `BindInheritedAttributes` | 3157-3203 | Walk the GroupType.InheritedGroupTypeId chain; collect inherited member-attribute definitions; collect group-attribute date fields for due-date dropdown |
| `BindGroupRequirementsGrid` | 4541-4565 | Re-bind the read-only "From {GroupType}" requirements grid; recompute Add-button visibility based on `EnableSpecificGroupRequirements` |
| `BindAdministratorPerson` | 2152-2165 | Show/hide admin picker; set label to `groupType.AdministratorTerm` |
| `SetChatControls` | 2512-2561 | Show/hide chat panel; populate values |

The properties read off `GroupTypeCache` to drive all of this:

```
IsPeerNetworkEnabled, RelationshipStrength, RelationshipGrowthEnabled,
LeaderToLeaderRelationshipMultiplier, LeaderToNonLeaderRelationshipMultiplier,
NonLeaderToLeaderRelationshipMultiplier, NonLeaderToNonLeaderRelationshipMultiplier,
EnableRSVP, RSVPReminderOffsetDays, RSVPReminderSystemCommunicationId,
LocationSelectionMode, EnableLocationSchedules, AllowMultipleLocations,
LocationTypeValues (collection of DefinedValues),
AllowedScheduleTypes (Weekly|Custom|Named flags),
EnableInactiveReason, RequiresInactiveReason, InactiveReasons (collection),
GroupCapacityRule, IsCapacityRequired,
GroupStatusDefinedTypeId, GroupStatusDefinedType,
AllowGroupSpecificRecordSource,
AllowGroupSync,
AllowSpecificGroupMemberWorkflows,
AllowSpecificGroupMemberAttributes,
EnableSpecificGroupRequirements,
IsSchedulingEnabled,
IsChatAllowed,
ShowAdministrator, AdministratorTerm,
GroupsRequireCampus,
Roles (collection of GroupTypeRole)
```

Plus database lookups on each change:
- Inherited attribute walk (1 query per ancestor in the InheritedGroupTypeId chain).
- GroupRequirement rows where `GroupTypeId = thisGroupType` for the read-only grid.

When the WebForms block does this, it's "free" because everything is already loaded server-side and the autopostback rebinds controls in place. There's a roundtrip, but no perceptible latency on a local network.

---

## Approach A: Front-load (client-side OptionsBag)

### Shape

The block's `GroupDetailOptionsBag` carries a per-GroupType nested structure:

```typescript
interface GroupDetailOptionsBag {
    // Block-level options (applies to all group types)
    quickLinkUrls: { ... };
    isChatEnabledSystem: boolean;
    
    // Per-GroupType options, keyed by IdKey
    groupTypes: Record<string, GroupTypeOptions>;
}

interface GroupTypeOptions {
    name: string;
    iconCssClass: string;
    
    // Boolean flags
    isPeerNetworkEnabled: boolean;
    enableRsvp: boolean;
    isSchedulingEnabled: boolean;
    isChatAllowed: boolean;
    showAdministrator: boolean;
    enableInactiveReason: boolean;
    requiresInactiveReason: boolean;
    enableLocationSchedules: boolean;
    allowMultipleLocations: boolean;
    isCapacityRequired: boolean;
    groupsRequireCampus: boolean;
    allowGroupSpecificRecordSource: boolean;
    allowGroupSync: boolean;
    allowSpecificGroupMemberWorkflows: boolean;
    allowSpecificGroupMemberAttributes: boolean;
    enableSpecificGroupRequirements: boolean;
    
    // Flags / enums
    locationSelectionMode: number; // GroupLocationPickerMode flags
    allowedScheduleTypes: number;  // ScheduleType flags
    groupCapacityRule: number;     // GroupCapacityRule enum
    
    // Strings
    administratorTerm: string;
    
    // Defaults for peer network
    relationshipStrength: number;
    relationshipGrowthEnabled: boolean;
    leaderToLeaderRelationshipMultiplier: number;
    leaderToNonLeaderRelationshipMultiplier: number;
    nonLeaderToLeaderRelationshipMultiplier: number;
    nonLeaderToNonLeaderRelationshipMultiplier: number;
    
    // Defaults for RSVP
    rsvpReminderOffsetDays: number | null;
    rsvpReminderSystemCommunicationId: number | null;
    
    // Collections
    inactiveReasons: ListItemBag[];
    locationTypeValues: ListItemBag[];
    roles: ListItemBag[];
    inheritedMemberAttributes: InheritedAttributeBag[];
    dateAttributesForGroup: ListItemBag[];
    groupRequirementsForGroupType: GroupRequirementBag[];
    statusDefinedType: { id: number; name: string; values: ListItemBag[] } | null;
}
```

The `GetBoxOptions()` C# method enumerates every GroupType the user could pick (subject to block-attribute filters) and builds one `GroupTypeOptions` per type.

The Vue layer has:

```typescript
const currentGroupTypeId = computed(() => editBag.value.bag.groupType?.value ?? "");
const currentTypeOptions = computed(() => 
    options.value.groupTypes[currentGroupTypeId.value] ?? null);

// Every visibility check is a computed that reads currentTypeOptions.value:
const isPeerNetworkPanelVisible = computed(() => 
    currentTypeOptions.value?.isPeerNetworkEnabled ?? false);
// ... etc
```

### Pros

- **Zero round trips** for any UI change. Switching group types is instant.
- **Deterministic, no async race conditions.** A user spamming the dropdown can't get into a weird state.
- **All visibility logic is reactive computed properties.** Easy to read, test, and reason about.
- **Easier to test.** Mocking the OptionsBag covers all scenarios.
- **No new block action needed** beyond the standard `Edit`/`Save`/`Delete`/etc.

### Cons

- **Bigger initial payload.** A site with 50 GroupTypes pays for all of them upfront.
  - Realistic per-GroupTypeOptions size: ~1.5-3 KB JSON depending on collection counts.
  - 50 × 2 KB = 100 KB extra in the initial bag. Not catastrophic on broadband, perceptible on slow connections.
  - For sites with 200+ GroupTypes (rare, but they exist for large platforms) this becomes meaningful.
- **Slower initial-load query.** Building options for every group type means querying:
  - All GroupTypes (already cached).
  - All InactiveReasons per GroupType (DefinedValue queries, cacheable).
  - All Roles per GroupType (already on cache).
  - All InheritedAttributes per GroupType (walks ancestor chain — uncached, runs queries).
  - All DateAttributes per GroupType (uncached, runs queries).
  - All GroupRequirements per GroupType (uncached, runs queries).
  - At ~50 GroupTypes that's ~150 queries on cold cache. Probably fast (most queries are simple), but profile.
- **Stale data risk.** If an admin changes a GroupType's settings while another admin is editing a Group, the latter's cached OptionsBag is stale. Mitigation: the OptionsBag is only loaded once per page navigation, and any change requires re-edit.

### Best for

- Typical Rock installs (under ~20 GroupTypes the user can pick from in any single edit session, given block-attribute filters).
- High-responsiveness UX requirement.
- Single-developer maintenance.

---

## Approach B: Server round-trip (block action per change)

### Shape

The initial `OptionsBag` carries only block-level options and the options for the currently selected GroupType (if any). When `currentGroupTypeId` changes, the Vue layer fires a block action:

```typescript
const result = await invokeBlockAction<GroupTypeOptions>("GetGroupTypeOptions", {
    groupTypeIdKey: newGroupTypeIdKey
});
currentTypeOptions.value = result.data;
```

The C# block exposes a corresponding `BlockAction`:

```csharp
[BlockAction]
public BlockActionResult GetGroupTypeOptions( string groupTypeIdKey )
{
    var groupType = ...load by IdKey...
    if ( groupType == null ) return ActionNotFound();
    if ( !this.GroupCanBeEditedAsGroupType(groupType) ) return ActionForbidden();
    return ActionOk( BuildGroupTypeOptions(groupType) );
}
```

### Pros

- **Smaller initial payload.** Only the current GroupType's options are sent.
- **Always-fresh options.** A change to GroupType configuration is reflected on the next dropdown change.
- **Lower initial-load query cost** because only the current GroupType's lookups run.
- **Simpler bag schema.** Single `currentTypeOptions: GroupTypeOptions` rather than a dictionary.

### Cons

- **Round trip on every dropdown change.** ~50-300ms latency depending on network and server. The UI is unresponsive during this.
- **Loading-state UX** required for every change (spinners, disabled controls during fetch).
- **More moving parts.** A new block action with its own auth, error handling, race-condition management (e.g., user clicks GroupType A, then B before A's response arrives — must handle out-of-order responses).
- **Harder to test client-side.** Need to mock async block-action responses.
- **Needs same auth checks at the action level** that the initial-load auth does, otherwise you'd allow load-by-IdKey for GroupTypes the user can't actually pick.

### Best for

- Sites with 100+ GroupTypes the user could pick.
- Cases where freshness matters more than UX responsiveness.

---

## Approach C: Hybrid (initial-current + lazy-cache on change)

### Shape

Initial OptionsBag carries options only for the currently selected GroupType. On dropdown change, fetch via block action and cache in memory. Subsequent visits to the same GroupType in the same session don't re-fetch.

### Pros

- **Smallest initial payload.**
- **Subsequent visits to a previously-selected GroupType are instant** (cached client-side).
- **Best of both worlds for a "settle on the right type" workflow** — a user switches a few times and converges.

### Cons

- **Most code complexity.** Both initial-load and lazy-load code paths exist.
- **First visit per GroupType still pays the round trip.**
- **In-session staleness** (if the cached value becomes wrong mid-session, the cache pollutes the rest of the session).
- **More state management** (cache invalidation, fetch-in-progress tracking).

### Best for

- Edge case: very large GroupType counts where Approach A is too heavy AND users routinely flip back and forth.

---

## Edge cases that affect every approach

These apply to all three and are worth mentioning:

1. **Existing groups: GroupType is read-only.** Per `ShowEditDetails`, the GroupType dropdown is hidden when `group.Id != 0` (line 1949). The cascade only matters for **new groups** (or when the underlying group type changes via a different mechanism, which doesn't happen in this block today).

2. **Block-attribute filters.** The GroupTypes the user can pick are filtered by:
   - `GroupTypesInclude` block attribute (whitelist).
   - `GroupTypesExclude` block attribute (blacklist).
   - `LimitToShowInNavigationGroupTypes` block attribute.
   - The parent group's `ChildGroupTypes` (or `AllowAnyChildGroupType`).
   - Per-user EDIT auth on each GroupType.
   
   Approach A must apply all these filters server-side and only emit options for the survivors. Approach B/C apply them in the block action's auth check.

3. **Parent-group dependent filtering.** Currently `ddlParentGroup_SelectedIndexChanged` re-filters `ddlGroupType` based on the parent group's `ChildGroupTypes`. In Approach A, this is a Vue computed that filters `Object.keys(options.groupTypes)` against the parent's allowed children. In Approach B/C, the dropdown options can be similarly filtered client-side without round-tripping.

4. **`InactiveReasons` collection per GroupType.** This is a per-GroupType `GroupTypeService.GetInactiveReasonsForGroupType(int)` call (a database query joining DefinedValues). For Approach A this is N queries upfront. For Approach B/C it's per-fetch.

5. **Inherited attributes walk.** `BindInheritedAttributes` walks `InheritedGroupTypeId` recursively. For Approach A, this runs once per group type at initial-load. For Approach B/C, once per group-type fetch.

6. **GroupRequirements for the read-only grid.** Each GroupType may have its own group-type-level requirements. For Approach A, query upfront. For Approach B/C, query per fetch.

---

## Recommendation

**Approach A (front-load)**, for these reasons:

1. The vast majority of Rock installations have a manageable number of GroupTypes (under 20 user-pickable per block instance after filters).
2. The biggest single cost is the inherited-attribute walk and per-type requirements query, but those are still cheap in absolute terms (single-digit ms each).
3. Group editing is admin work, not high-traffic public-facing. A 100 KB initial bag is acceptable.
4. The UX of zero-latency GroupType switching is meaningfully better than even a 100ms round trip.
5. Code complexity is dramatically lower (no async state machine, no race conditions).

**Caveats**:

- **If profile reveals the initial-load query cost is too high** for some installations, fall back to Approach C.
- **If the OptionsBag size exceeds a threshold** (e.g., > 250 KB), fall back to Approach C.
- **If a customer reports specific pain** with a customized GroupType set, the implementation can switch approaches without changing the bag contract dramatically (only the loading mechanism changes).

The recommendation is to **start with Approach A** and treat fallback to Approach C as a known-possible mitigation if real-world data demonstrates a problem.

---

## What this changes about the partition plan

Whichever approach is chosen:

- **Phase 0** must lock down the choice and document the bag shape.
- **Phase 1** must build the C# `GetBoxOptions()` for the chosen approach (full enumeration for A, or single-current-type for B/C).
- **Phase 2** is where the cascade is wired (Vue computeds for A, block action + handler for B/C).

If the choice is deferred past Phase 0, Phase 2 cannot proceed cleanly because the bag shape isn't settled.

---

## What I need from the user

A choice among A / B / C, ideally with reasoning so future readers (and future Claudes) understand the why. If the user wants more detail on any specific tradeoff, ask and we'll deepen this doc.
