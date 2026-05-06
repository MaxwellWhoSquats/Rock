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

## Recommended Phase Structure

### Phase 0: Architecture & Foundation Spec

**Output**: A spec document at `specs/` (no code).

**Decisions resolved by the user**:

1. **View panel redesign approach** — **Pure Vue, no Lava.** The new design lives in the user-supplied Figma file. `groupType.GroupViewLavaTemplate` is not used. See [17-view-panel.md](../webforms/17-view-panel.md) for the migration story.

2. **New features beyond 1:1 parity** — Enumerated by the Figma design. The Figma is the source of truth for what is parity vs. net-new. The phase plan must absorb whatever new features the design adds; they will land in whichever phase touches the same domain.

3. **`[ContextAware(typeof(Group))]` support** — **User decided: keep it.** Production pages are assumed to rely on it.

   **However**, the deep-research pass (see [18-cross-block-dependencies.md](../webforms/18-cross-block-dependencies.md)) confirmed that the WebForms block **never reads `this.Entity` or calls `ContextEntity<Group>()`**. The `ContextEntityBlock` inheritance is dead code — every code path goes through the URL parameter. Grep across the entire codebase did not surface a single caller relying on the context-aware pathway.

   This is flagged for a possible decision-revisit. If the user confirms after re-reading the analysis that ContextAware really should be kept, the conversion ports it via `RequestContext.GetContextEntity<Group>()` falling back to URL parameter. If the user is willing to drop it based on the dead-code finding, the conversion drops the attribute and base-class change, which simplifies the Phase 1 shell.

4. **IdKey support** — `GroupDetail` both **accepts** and **writes** IdKey. The page parameter `GroupId` accepts integer-or-IdKey form on the way in. Outbound LinkedPage URLs are written with IdKey.

   **Verified state of downstream IdKey support** (per the deep-research pass in [18-cross-block-dependencies.md](../webforms/18-cross-block-dependencies.md)):

   - **Outbound destinations** (11 total): 3 Obsidian-converted and accept IdKey (GroupAttendanceList, GroupRSVPPage, GroupPlacementPage). 5 still WebForms and integer-only — will break if GroupDetail starts writing IdKey to them: GroupListPage, FundraisingProgressPage, GroupHistoryPage, GroupMapPage, GroupSchedulerPage. 3 are pass-through (RegistrationInstancePage, EventItemOccurrencePage, ContentItemPage).

   - **Inbound callers** (24 total): 4 already write IdKey today. 11 still write integer Id. (Plus 4 themed Lava sidebars and 5 false-positives such as Mobile blocks that target a different block entirely.) GroupDetail will accept both forms so this is not blocking, but eventual normalization is follow-on work.

   The 5 still-WebForms outbound destinations are the immediate pain point. The conversion can either: (a) write integer Id specifically when targeting those 5, OR (b) update those 5 to accept IdKey (separate scope). Phase 0 must pick.

5. **Block-type chop strategy** — **No migration is written.** Rock chops the WebForms block to its Obsidian replacement at startup via `BlockTypeService.StagePossibleMigrateWebFormsToObsidianBlock`. The new C# class declares:
   - A freshly-generated `[Rock.SystemGuid.EntityTypeGuid("...")]`.
   - A `// was [Rock.SystemGuid.BlockTypeGuid("...")]` comment line carrying the discarded would-have-been GUID for traceability.
   - An active `[Rock.SystemGuid.BlockTypeGuid("582BEEA1-5B27-444D-BC0A-F60CEB053981")]` reusing the WebForms block's GUID — that is what triggers the chop.
   
   Generate the new EntityTypeGuid and the discarded BlockTypeGuid via:
   ```
   node .claude/skills/convert-block/scripts/generate-guids.js
   ```
   
   Do NOT call `AddOrUpdateEntityBlockType` here — that is for net-new blocks and would create a parallel BlockType row, leaving every existing page still pointing at the WebForms one. See [01-block-configuration.md](../webforms/01-block-configuration.md) for the full pattern.

6. **Partial structure** — Mirror GroupTypeDetail's pattern: one `editPanel.partial.obs`, one `viewPanel.partial.obs`, one partial per sub-feature panel. Confirm specific naming during Phase 0.

7. **Save action shape** — Single `Save` block action returning `ValidPropertiesBox<GroupBag>` (200) or redirect string (201) on create. Plus separate `Edit`, `Delete`, `Archive`, `Copy`, `ArchiveWithChildren` actions.

**Decision still open**:

8. **GroupType-change reactive cascade** — How will the Vue layer reshape itself when `currentGroupTypeId` changes mid-edit? Two main approaches with significantly different tradeoffs. See [22-grouptype-cascade.md](../webforms/22-grouptype-cascade.md) for the full explainer the user requested.

   **Updated payload data** from the deep-research pass ([24-grouptype-inheritance.md](../webforms/24-grouptype-inheritance.md)): per-GroupType options are roughly 3.75 KB serialized, so a 50-GroupType site loads ~187 KB extra in the initial bag for Approach A. This sharpens the tradeoff — the original "100 KB extra" estimate was conservative. For installations with 100+ user-pickable GroupTypes the payload is meaningful (375 KB+).

   **GroupTypeDetail precedent**: that block already uses a hybrid lazy-load pattern (cycle-guarded block action) for its inheritance chain. The pattern is portable and well-tested.

   This decision must be locked down during Phase 0.

**Latent bugs surfaced by the deep-research pass**:

The agents identified several pre-existing bugs in `GroupDetail.ascx[.cs]` that are independent of the conversion. They should be triaged during Phase 0:

- **Duplicate code block** at [GroupDetail.ascx.cs:2245-2273](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:2245) inside `ShowGroupTypeEditDetails` — same logic appears twice. Trivial cleanup during conversion.
- **Possible duplicate-edit corruption in group requirements** — flagged in [11-group-requirements.md](../webforms/11-group-requirements.md). Worth a closer look during Phase 4.
- **Hard-coded `EntityTypeId=15`** in the `mdGroupRequirement` markup. Should use `EntityTypeCache.GetId<DataView>()`. Trivial fix during conversion.
- **XSS hole in `FormatTriggerType`** — flagged in [13-member-workflow-triggers.md](../webforms/13-member-workflow-triggers.md). User-controlled data interpolated into HTML without encoding. Per memory, "HTML-encode user-controlled values during conversion review" — fix during conversion.
- **Missing `TagCategory` block attribute** — referenced at [GroupDetail.ascx.cs:543](RockWeb/Blocks/Groups/GroupDetail.ascx.cs:543) but never declared. Latent or vestigial. Either declare and ship, or drop the reference.
- **Open-redirect risk on `returnUrl`** — flagged in [18-cross-block-dependencies.md](../webforms/18-cross-block-dependencies.md). The block redirects to whatever URL the user supplies in the query string. Phase 0 should decide whether to validate (e.g., same-origin only).

These six are **not blocking** but should each be classified during Phase 0 as: fix-during-conversion, defer-to-bugfix-spec, or drop.

**Estimated effort**: 1 session.

### Phase 1: Block shell + View panel + Delete/Archive/Copy

**Output**:

- `Rock.Blocks/Group/GroupDetail.cs` skeleton based on `RockEntityDetailBlockType<Group, GroupBag>` and `IBreadCrumbBlock`.
- `Rock.ViewModels/Blocks/Group/GroupDetail/GroupBag.cs` with the read-only / view fields.
- `Rock.ViewModels/Blocks/Group/GroupDetail/GroupDetailOptionsBag.cs` with quick-link URLs and group-type-driven options.
- `Rock.JavaScript.Obsidian.Blocks/src/Group/groupDetail.obs` top-level shell.
- `Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/viewPanel.partial.obs` (View panel — per Phase 0 decision).
- Block actions: `Edit` (returns a placeholder bag for now), `Delete`, `Archive`, `ArchiveWithChildren`, `Copy`.
- `EditPanel.partial.obs` is an empty stub.
- Correct GUID attributes on the C# class so the Rock startup chop replaces the WebForms block in place. No migration file is written.

**Behavior delivered**:

- View an existing group with all chrome, badges, audit drawer, tag list, following control.
- View panel renders (full content per Phase 0 decision).
- Quick-link toolbar buttons all resolve to correct URLs.
- Delete works (with auth checks, CanDelete validation, schedule cleanup).
- Archive works (with optional cascade to children).
- Copy works (with the include-child-groups confirmation).

**Behavior NOT yet delivered**:

- Edit mode shows nothing useful (placeholder).
- Add new group is not yet possible.
- Save does nothing.

**Out of scope**: edit form fields, attributes, sub-feature panels.

**Reference block**: `Rock.Blocks/Group/GroupTypeDetail.cs` for shell patterns. `groupTypeDetail.obs` for SFC structure.

**Estimated effort**: 1 session.

---

### Phase 2: Edit panel — Core scalar fields + General panel

**Output**:

- `editPanel.partial.obs` with the always-visible top section + `wpGeneral` + `wpRsvp` + `wpScheduling` + `wpChat`.
- Bag extensions for all the new editable fields.
- Save block action body that handles all scalar fields on Group, including Peer Network overrides, RSVP, Scheduling, Chat (and chat avatar binary file IsTemporary toggle).
- GroupType-change reactive cascade (Vue computeds + watchers).
- IsActive-driven reactive sub-panels (inactive reason, note, child-cascade).

**Behavior delivered**:

- Add new group with full scalar field support.
- Edit existing group's scalar fields.
- Save / Cancel works.
- All authorization gates (VIEW, EDIT, ADMINISTRATE).
- All GroupType-conditional visibility for the panels covered.
- Cache invalidations on save (Authorization.Clear if security role flipped, KioskDevice.Clear if relevant).

**Behavior NOT yet delivered**:

- No attribute editing.
- No sub-feature panels (Locations, Requirements, Sync, Triggers).
- Member attribute definitions panel placeholder.

**Out of scope**: anything in the panels not listed.

**Estimated effort**: 1-2 sessions.

---

### Phase 3: Attributes (Group + Member definitions)

**Output**:

- `groupAttributes.partial.obs` for Group attribute values (AttributeValuesContainer).
- `groupMemberAttributes.partial.obs` for member attribute definitions (inherited + custom + modal).
- Bag extensions for inherited attributes, custom attribute definitions, and group attribute values.
- Save logic for attribute values and attribute definitions (the SaveAttributeEdits flow).

**Behavior delivered**:

- Group attribute values editable in the Group Attribute Values panel.
- Member attribute definitions editable (Add / Edit / Delete / Reorder / Security) in the Member Attributes panel.
- Inherited member attributes shown read-only.

**Reference**: `groupAttributes.partial.obs` and `groupMemberAttributes.partial.obs` from GroupTypeDetail.

**Estimated effort**: 1 session.

---

### Phase 4: Group Requirements + Group Sync + Member Workflow Triggers

**Output**:

- `groupRequirements.partial.obs`.
- `groupSync.partial.obs`.
- `memberWorkflowTriggers.partial.obs`.
- Bag extensions for each (using `SyncRelatedEntities` helper for save).
- Each with a modal editor (`groupRequirementModal.partial.obs`, etc.).

**Behavior delivered**:

- All three sub-features fully functional end-to-end.

**Why bundled**: All three share the same shape (editable list of related entities with a modal editor) and the same save pattern (`SyncRelatedEntities`). Bundling amortizes the partial structure work.

**Reference**: `groupRequirements.partial.obs` and `groupMemberWorkflows.partial.obs` from GroupTypeDetail.

**Estimated effort**: 1-2 sessions.

---

### Phase 5: Locations & Schedules

**Output**:

- `meetingDetails.partial.obs` with the Locations grid and Schedule sub-panel.
- `locationModal.partial.obs` with member/other tabs and capacity repeater.
- Bag extensions for `GroupLocationsState`, schedules, capacities.
- Save logic for GroupLocations including:
  - GroupLocationScheduleConfig diff (add/update/remove).
  - GroupMemberAssignment cascade cleanup on location changes.
  - Inline schedule entity management (Weekly / Custom / Named).
  - Inline schedule cleanup on type change.

**Behavior delivered**:

- Meeting Details panel fully functional including all corner cases (member-address locations, scheduling capacities, inline schedules).

**Why last**: Most complex sub-feature, with the heaviest cascade logic. Worth doing after all simpler patterns are established.

**Estimated effort**: 1-2 sessions.

---

### Phase 6: View Panel redesign (if Phase 0 chose redesign)

**Output**:

- New Vue ViewPanel structure with rich UI.
- Bag fields populated for everything the new view needs.
- Migration of users away from `GroupViewLavaTemplate` (or fallback path that still renders it).

**Behavior delivered**:

- View panel matches the redesigned design.
- Customers who customized `GroupViewLavaTemplate` either see their template (fallback path) or are migrated.

**Why separate phase**: Redesign is design work that's mostly orthogonal to the conversion. It can land after the rest of the block is shippable. If the design is ready in Phase 0, it could fold into Phase 1 instead.

**Estimated effort**: 1-2 sessions, plus design iteration.

---

### Phase 7: New features

**Output**: Each new feature gets its own spec section under `specs/` and lands in this phase.

**Behavior delivered**: Every new feature the user has in mind. (TBD — must be enumerated in Phase 0.)

**Why separate phase**: New features should not gate the conversion. The conversion delivers parity with WebForms; new features are additive on top.

**Estimated effort**: depends on the feature set.

---

### Phase 8: Cutover & cleanup

**Output**:

- Verification that the Rock startup chop ran cleanly (BlockType row's EntityType swapped, Path cleared, all page block instances retained their attribute values).
- Deletion of `RockWeb/Blocks/Groups/GroupDetail.ascx`, `GroupDetail.ascx.cs`, and `GroupDetail.ascx.designer.cs`.
- Smoke tests across every cross-block caller (per [18-cross-block-dependencies.md](../webforms/18-cross-block-dependencies.md)).
- Final QA pass on every block-attribute combination and every GroupType-driven panel-visibility branch.

No migration file is written for the cutover. The chop is automatic at startup and the WebForms files are deleted from source control as part of this phase.

**Estimated effort**: 1 session.

---

## Phase dependencies

```
Phase 0 (architecture spec)
    │
    ▼
Phase 1 (shell + view + delete/archive/copy)
    │
    ▼
Phase 2 (core edit fields + general/rsvp/scheduling/chat panels)
    │
    ▼
Phase 3 (attributes) ────┐
    │                    │
    ▼                    ▼
Phase 4 (req+sync+triggers)   ────┐
    │                              │
    ▼                              │
Phase 5 (locations + schedules) ───┤
                                   │
                                   ▼
Phase 6 (view panel redesign)      │
                                   │
                                   ▼
Phase 7 (new features)
                                   │
                                   ▼
Phase 8 (cutover)
```

Phases 3, 4, and 5 could partially parallelize across multiple developers/sessions if needed, since their bag fields are largely orthogonal (only the save action body has merge conflict risk).

## Total estimate

7-10 implementation sessions, plus the Phase 0 architecture spec session. So 8-11 sessions of coordinated development. The user's stated approach of treating each phase as its own session aligns well with this.

## Risks / open questions

1. **View panel design pace** — Pure Vue per Phase 0 resolution, but the design fidelity depends on the Figma being finalized. If the Figma lands later than Phase 1 starts, Phase 1 ships a structured-but-unstyled placeholder and Phase 6 replaces the markup. If the Figma is ready earlier, Phase 1 can ship the full design and Phase 6 collapses into Phase 1.
2. **New features driven by the Figma** — Each feature must be classified during Phase 0 (parity vs. net-new) and slotted into the most-related phase. Cross-cutting features (those that don't fit any single phase cleanly) become their own micro-phase or go into Phase 7.
3. **Inline schedule + location schedules interplay** — The most subtle correctness risk in Phase 5. The save flow's `GroupMemberAssignment` cleanup logic and the `Schedule.Name = empty` convention must be preserved exactly.
4. **GroupType-change cascade** — Still open. See [22-grouptype-cascade.md](../webforms/22-grouptype-cascade.md). Must be resolved before Phase 2 begins because it shapes both the OptionsBag and the C# block actions.
5. **Customer-customized GroupViewLavaTemplate** — Per Phase 0 resolution, the new GroupDetail does not honor the template. Sites that customized it need release-note callouts and possibly a one-time data audit. See [17-view-panel.md](../webforms/17-view-panel.md).
6. **Linked page IdKey acceptance** — Downstream blocks (AttendancePage, GroupListPage, etc.) need to accept IdKey form for the GroupId param GroupDetail will write. List documented in [18-cross-block-dependencies.md](../webforms/18-cross-block-dependencies.md). Not in scope for this effort but should be tracked as follow-on work.

## Alternatives considered

### Alternative A: Single big-bang conversion (rejected)

Drive a single `/convert-block` session through the whole thing. Rejected because:
- Estimated 8-12 hour session — too long for a single review pass.
- Generated bag/partials would be too large to review coherently.
- Any single bug blocks the entire conversion.

### Alternative B: Strict feature-flag rollout (deferred)

Each phase ships behind a feature flag (`UseObsidianGroupDetail = true`). Rejected as a primary partitioning strategy because Rock doesn't typically use feature flags this way; the convention is "convert and ship". But for high-risk phases (specifically Phase 5 Locations) we may want a temporary flag.

### Alternative C: Bundled into 3 mega-phases (deferred)

Phase 1: shell + everything edit (huge), Phase 2: all sub-features, Phase 3: cutover. Rejected because Phase 1 alone would be 4-5x normal session size.

### Alternative D: Sub-feature first (rejected)

Build all sub-features first (Locations, Requirements, etc.) as standalone Vue components before the shell. Rejected because the bag contract isn't established until the shell exists; would force lots of rework.

## Recommendation summary

**Adopt the 8-phase structure above (0-7) with cutover in Phase 8.** Start with Phase 0 — a foundational spec that locks down the architectural decisions and enumerates new features. Without that lock-down, every later phase has a moving target.
