# Phase Partitioning Recommendation

## Goal

Break the GroupDetail conversion into a sequence of independent specs, each delivering a coherent, reviewable, releasable slice of work. Each phase should:

1. Be small enough to fit in a single Claude Code session for the spec authoring + implementation.
2. End in a state that compiles, passes tests, and can be merged on its own.
3. Leave WebForms intact and functional until the very last phase (the cutover). Until then, the converted block runs on a separate page (or behind a feature flag) so it can be QA'd in parallel.

## Why I rejected "C# side / TS side"

The user's starter hypothesis was to split by language layer. After the research, I'm recommending against this because:

- The bag (defined in C#, consumed in TS) is the contract. Splitting them across phases means one side has a placeholder bag for weeks.
- C# without a UI is hard to test functionally. The Save flow has 14+ branches that need to be exercised; doing that without a UI means writing throwaway harnesses.
- TS without a real C# block can only mock; the moment real data arrives, half the assumptions break.
- The biggest risks (GroupType-driven reactive visibility, save-flow correctness) are inherently full-stack — they need the language boundary crossing to be tested.

I'm recommending **vertical slices by sub-feature instead**. Each slice ships a small piece of full-stack functionality.

## Why I rejected "panel-by-panel exact mirror"

The 11 panel widgets are not all equal. A panel-by-panel split would have phases ranging from "1 hour" (RSVP, 2 fields) to "5 days" (Locations + Schedules). I bundled smaller related panels.

## Why view-first

The plan finishes the **entire view panel** before any edit-panel work begins. The user requested this reordering after Phase 1's self-review. Reasons:

- **Cleaner reviewable state.** Phase 1 already shipped the view-panel chrome, Overview card, Group Tools card, and terminal actions. The remaining view-side surfaces (Group Image hero, Meeting Locations card with map cards) are tractable as a single follow-up. Finishing the view panel first means the read-only experience is frozen and reviewable across the rest of the conversion.
- **No half-finished cards.** The original plan deferred the Meeting Locations card to Phase 5 alongside the editing modal. View-first splits those: the read-side map cards land in Phase 2, and the location editing modal stays in the dedicated locations phase later.
- **Group.PhotoId column.** The `Group.PhotoId` migration was originally bundled with chat-avatar editing in Phase 2. Decoupling them is clean: the column add lands in the new Phase 2 (so the view-panel hero can render), and the uploader (using the same `IsTemporary` BinaryFile pattern as the chat avatar) lands with edit core.

## Recommended Phase Structure

### Phase 0: Architecture & Foundation Spec

**Output**: A spec document at `specs/` (no code).

Locked all architectural decisions, GUID strategy, IdKey policy, partial structure, latent-bug triage. Drafted Phase 1 spec at session close. See [00-architecture.md](00-architecture.md) for the canonical record.

**Estimated effort**: 1 session.

**Status**: Locked.

### Phase 1: Block shell + View panel core + Delete/Archive/Copy + Audit modal + Linkages

**Output**:

- `Rock.Blocks/Group/GroupDetail.cs` based on `RockEntityDetailBlockType<Group, GroupBag>` and `IBreadCrumbBlock`.
- `Rock.ViewModels/Blocks/Group/GroupDetail/GroupBag.cs` and four supporting bags (`GroupDetailOptionsBag`, `CopyGroupRequestBag`, `GroupLinkageBag`, `GroupLinkagesBag`).
- `Rock.JavaScript.Obsidian.Blocks/src/Group/groupDetail.obs` top-level shell.
- `Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/viewPanel.partial.obs` (Pure-Vue view panel per Q3).
- `editPanel.partial.obs` (Phase 3 placeholder), `copyModal.partial.obs`, `types.partial.ts`.
- Block actions: `Edit` (placeholder bag), `Delete`, `Archive`, `ArchiveSingleGroup`, `ArchiveWithChildren`, `Copy`.
- Correct GUID attributes so the Rock startup chop replaces the WebForms block in place. No migration file is written.

**Behavior delivered**:

- View an existing group with all chrome, badges, audit modal (framework-provided), tag list, following control.
- View panel renders the redesigned Overview card (Description, Administrator, Parent, Schedule, Capacity, four group attributes, Linkages section) and Group Tools card (Participation + Views sub-headers).
- Group Type / Campus / Public / Inactive / Archived / Relationship Strength labels render in the subheader.
- Delete works (with auth checks, `CanDelete` validation, inline schedule cleanup, security-role branch).
- Archive works (with optional cascade to children via the `Archive` / `ArchiveSingleGroup` / `ArchiveWithChildren` triple).
- Copy works (with the Include-Child-Groups checkbox flipped to default-unchecked per design C1).
- Outbound IdKey on all 11 LinkedPage URLs (the 5 still-WebForms destinations will produce broken links until Phase 7).
- L5 (TagCategory dead reference) dropped; L6 (returnUrl open-redirect) closed via same-origin validation.

**Behavior NOT yet delivered** (deferred to later phases):

- Group Image hero in the Overview card. The Vue partial wires up the conditional render, but `bag.photoUrl` is always null until Phase 2 ships the new `Group.PhotoId` column.
- Meeting Locations card with map cards. Phase 2 scope.
- All edit-mode field editing, Save flow, GroupType cascade, Add path. Phase 3 scope.

**Reference block**: `Rock.Blocks/Group/GroupTypeDetail.cs` for shell patterns. `groupTypeDetail.obs` for SFC structure.

**Estimated effort**: 1 session.

**Status**: Completed.

---

### Phase 2: Complete the view panel (Group Image + Meeting Locations card)

**Output**:

- New `Group.PhotoId` column (nullable int FK to `BinaryFile`, mirroring `Person.PhotoId`):
  - Entity-model property + `EntityTypeConfiguration` nav property + EF migration with `WillCascadeOnDelete(false)` and `ON DELETE SET NULL`.
  - Codegen regen (Rock.CodeGeneration WPF tool) to update auto-generated bag types.
- `GroupBag.PhotoUrl` populated when `entity.PhotoId.HasValue`. Vue Image hero renders the 16:9 image; omits when null.
- `meetingLocationsCard.partial.obs` (or equivalent) rendering each `GroupLocation` as a 16:9 map card with hover-expand → `GroupMapPage`.
- New bag fields backing the locations card: per-location `Name`, `Address` (multi-line), `ScheduleText`, map data, location-picker mode (Address / Point / Polygon / GroupMember).
- Server-side population in `GetEntityBagForView` of the locations bag fields. Polygon-style locations render with no address; member-address locations render the family address.

**Behavior delivered**:

- Group Image hero shows the configured photo at the top of the Overview card. Region omits entirely when no photo set.
- Meeting Locations card renders only when at least one `GroupLocation` is configured. Each location is a card with map, address, schedule.
- View panel design fidelity is now complete; the read-only experience is frozen for the remainder of the conversion.

**Behavior NOT yet delivered**:

- Locations editing modal (Add / Edit / Delete / inline schedule entity management). Phase 6 scope.
- Group photo uploader (writes `Group.PhotoId`). Phase 3 scope (bundled with chat-avatar uploader and the `IsTemporary` BinaryFile pattern).

**Reference patterns**: `Person.PhotoId` ([Person.cs:242](Rock/Model/CRM/Person/Person.cs:242)) for the column shape. Existing Obsidian map components for the card rendering.

**Estimated effort**: 1-2 sessions. The map card rendering is the variable.

---

### Phase 3: Edit panel core (Top fields + General + RSVP + Scheduling + Chat)

**Output**:

- `editPanel.partial.obs` with the always-visible top section + `wpGeneral` + `wpRsvp` + `wpScheduling` + `wpChat`.
- Bag extensions for all the new editable fields.
- `Save` block action body that handles all scalar fields on Group, including Peer Network overrides, RSVP, Scheduling, Chat.
- Group photo uploader and chat-channel-avatar uploader (both using the `IsTemporary` BinaryFile toggle pattern).
- `GetGroupTypeOptions` block action for the GroupType-change reactive cascade (Q2 Approach B).
- `IsActive`-driven conditional well (Inactive Reason / Note / Inactivate Child Groups).
- Add (`?GroupId=0`) path with `?ParentGroupId=N` defaulting.
- `?autoEdit=true` page parameter handling.
- Trailblazer per-field styling on General-section fields (Q6).

**Behavior delivered**:

- Add new group with full scalar field support.
- Edit existing group's scalar fields.
- Save / Cancel works.
- All authorization gates (VIEW, EDIT, ADMINISTRATE).
- All GroupType-conditional visibility for the panels covered.
- Cache invalidations on save (`Authorization.Clear` if security role flipped, `KioskDevice.Clear` if relevant).

**Behavior NOT yet delivered**:

- No attribute editing.
- No sub-feature panels (Locations editing, Requirements, Sync, Triggers).
- Member attribute definitions panel placeholder.

**Out of scope**: anything in the panels not listed.

**Estimated effort**: 1-2 sessions.

---

### Phase 4: Attributes (Group + Member definitions)

**Output**:

- `groupAttributes.partial.obs` for Group attribute values (`AttributeValuesContainer`).
- `groupMemberAttributes.partial.obs` for member attribute definitions (inherited + custom + modal).
- Bag extensions for inherited attributes, custom attribute definitions, and group attribute values.
- Save logic for attribute values and attribute definitions (the `SaveAttributeEdits` flow).

**Behavior delivered**:

- Group attribute values editable in the Group Attribute Values panel.
- Member attribute definitions editable (Add / Edit / Delete / Reorder / Security) in the Member Attributes panel.
- Inherited member attributes shown read-only.

**Reference**: `groupAttributes.partial.obs` and `groupMemberAttributes.partial.obs` from GroupTypeDetail.

**Estimated effort**: 1 session.

---

### Phase 5: Group Requirements + Group Sync + Member Workflow Triggers

**Output**:

- `groupRequirements.partial.obs`.
- `groupSync.partial.obs`.
- `memberWorkflowTriggers.partial.obs`.
- Bag extensions for each (using `SyncRelatedEntities` helper for save).
- Each with a modal editor (`groupRequirementModal.partial.obs`, etc.).
- Sync Frequency control restyle on the existing Obsidian `<IntervalPicker>` per Q9.
- L3 (hard-coded `EntityTypeId=15` in mdGroupRequirement) fix-during.
- L4 (XSS hole in `FormatTriggerType`) fix-during with HTML-encoding of user-controlled values.

**Behavior delivered**:

- All three sub-features fully functional end-to-end.

**Why bundled**: All three share the same shape (editable list of related entities with a modal editor) and the same save pattern (`SyncRelatedEntities`). Bundling amortizes the partial structure work.

**Reference**: `groupRequirements.partial.obs` and `groupMemberWorkflows.partial.obs` from GroupTypeDetail.

**Estimated effort**: 1-2 sessions.

---

### Phase 6: Locations editing modal + inline schedule logic

**Output**:

- `meetingDetails.partial.obs` with the Locations grid (edit-mode) and Schedule sub-panel.
- `locationModal.partial.obs` with member/other tabs and capacity repeater.
- Bag extensions for `GroupLocationsState`, schedules, capacities.
- Save logic for `GroupLocations` including:
  - `GroupLocationScheduleConfig` diff (add/update/remove).
  - `GroupMemberAssignment` cascade cleanup on location changes.
  - Inline schedule entity management (Weekly / Custom / Named).
  - Inline schedule cleanup on type change.

**Behavior delivered**:

- Meeting Details editing fully functional including all corner cases (member-address locations, scheduling capacities, inline schedules).
- The view-side map cards (already shipped in Phase 2) update reactively after any save.

**Why last (among feature phases)**: most complex sub-feature, with the heaviest cascade logic. Worth doing after all simpler patterns are established.

**Note**: the read-side Meeting Locations card already shipped in Phase 2. Phase 6 only adds the editing modal and save flow.

**Estimated effort**: 1-2 sessions.

---

### Phase 7: Update dependencies

**Output**: Update the 5 still-WebForms outbound destinations (`GroupListPage`, `FundraisingProgressPage`, `GroupHistoryPage`, `GroupMapPage`, `GroupSchedulerPage`) so each accepts IdKey on its `GroupId` page parameter. Required because Phase 1+ writes IdKey to all 11 outbound URLs (per Q4) and these 5 destinations will be broken under IdKey URLs until updated.

The fixes are tiny: replace `.AsIntegerOrNull()` with `IdHasher.Instance.GetId(key) ?? key.AsIntegerOrNull()` (or use `GetSelect(key, ...)` overloads).

**Behavior delivered**:

- Every link from GroupDetail's Group Tools card and post-action navigation works regardless of whether GroupDetail emits integer Id or IdKey.

**Estimated effort**: 1 session.

---

### Phase 8: Cutover & cleanup

**Output**:

- Verification that the Rock startup chop ran cleanly (BlockType row's EntityType swapped, Path cleared, all page block instances retained their attribute values).
- Deletion of `RockWeb/Blocks/Groups/GroupDetail.ascx`, `GroupDetail.ascx.cs`, and `GroupDetail.ascx.designer.cs`.
- Smoke tests across every cross-block caller (per [18-cross-block-dependencies.md](../webforms/18-cross-block-dependencies.md)).
- Final QA pass on every block-attribute combination and every GroupType-driven panel-visibility branch.
- Release notes (including the customer-customized `GroupViewLavaTemplate` deprecation callout).

No migration file is written for the cutover. The chop is automatic at startup and the WebForms files are deleted from source control as part of this phase.

**Estimated effort**: 1 session.

---

## Phase dependencies

```
Phase 0 (architecture spec)
    │
    ▼
Phase 1 (shell + view core + delete/archive/copy)
    │
    ▼
Phase 2 (complete view panel: Group.PhotoId + Meeting Locations card)
    │
    ▼
Phase 3 (edit panel core: top fields + general/rsvp/scheduling/chat)
    │
    ▼
Phase 4 (attributes) ────┐
    │                    │
    ▼                    ▼
Phase 5 (req+sync+triggers)   ────┐
    │                              │
    ▼                              │
Phase 6 (locations editing)    ───┤
                                   │
                                   ▼
Phase 7 (update dependencies: 5 IdKey fixes)
                                   │
                                   ▼
Phase 8 (cutover)
```

Phases 4, 5, and 6 could partially parallelize across multiple developers/sessions if needed, since their bag fields are largely orthogonal (only the Save action body has merge conflict risk).

## Total estimate

7-10 implementation sessions, plus the Phase 0 architecture spec session. So 8-11 sessions of coordinated development.

## Risks / open questions

1. **Map card rendering** — Phase 2 needs an Obsidian map component to render per-location cards with the design's hover-expand behavior. If no existing component fits, may need a new lightweight wrapper. Track during Phase 2 spec authoring.
2. **`Group.PhotoId` cross-cutting consumers** — Once the column ships in Phase 2, other surfaces (Person profile photo widgets, search results, etc.) might want to surface the group photo too. Phase 2 scopes the column to GroupDetail only; cross-entity surfaces are follow-on work.
3. **GroupType-change cascade** — Resolved by Q2 (Approach B, server round-trip). Phase 3 ships `GetGroupTypeOptions` block action.
4. **Customer-customized GroupViewLavaTemplate** — Per Q3 resolution, the new GroupDetail does not honor the template. Sites that customized it need release-note callouts and possibly a one-time data audit. See [17-view-panel.md](../webforms/17-view-panel.md). Phase 8 cutover release notes own this.
5. **Inline schedule + location schedules interplay** — The most subtle correctness risk in Phase 6. The save flow's `GroupMemberAssignment` cleanup logic and the `Schedule.Name = empty` convention must be preserved exactly.
6. **5 still-WebForms outbound destinations break under IdKey** — Acknowledged tradeoff per Q4. Phase 7 closes the gap before cutover.

## Alternatives considered

### Alternative A: Single big-bang conversion (rejected)

Drive a single `/convert-block` session through the whole thing. Rejected because:

- Estimated 8-12 hour session — too long for a single review pass.
- Generated bag/partials would be too large to review coherently.
- Any single bug blocks the entire conversion.

### Alternative B: Strict feature-flag rollout (deferred)

Each phase ships behind a feature flag (`UseObsidianGroupDetail = true`). Rejected as a primary partitioning strategy because Rock doesn't typically use feature flags this way; the convention is "convert and ship". For high-risk phases (specifically Phase 6 Locations) we may want a temporary flag.

### Alternative C: Bundled into 3 mega-phases (deferred)

Phase 1: shell + everything edit (huge), Phase 2: all sub-features, Phase 3: cutover. Rejected because Phase 1 alone would be 4-5x normal session size.

### Alternative D: Sub-feature first (rejected)

Build all sub-features first (Locations, Requirements, etc.) as standalone Vue components before the shell. Rejected because the bag contract isn't established until the shell exists; would force lots of rework.

### Alternative E: Edit-first (was the original plan; superseded by view-first reordering after Phase 1)

Original plan finished view-panel core in Phase 1, then Phase 2-5 worked the edit panel and sub-features, with Phase 5 grafting the Meeting Locations card onto the view panel. Superseded after Phase 1 self-review by the view-first reordering above. The old plan left the view panel half-finished for four phases; view-first freezes the read-only experience earlier.

## Recommendation summary

**Adopt the 9-phase structure above (0-8).** Phase 0 + Phase 1 are complete. Phase 2 is the next session: complete the view panel by adding `Group.PhotoId` and the Meeting Locations card. Phase 3 onward delivers edit-side functionality.
