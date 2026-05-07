---
author: Maxwell Eley
date_created: 2026-05-06
summary: >-
  Canonical template for authoring per-phase implementation specs. Used by every
  spec-authoring session (each phase drafts the next phase's spec at session
  close per SESSION-PROTOCOL.md Section D5).
contributors: []
---

# Phase Spec Template

Copy the structure below to draft each phase's implementation spec. Every phase spec MUST include every "required" section. Optional sections are marked.

The template is also the contract that `SESSION-PROTOCOL.md` Section C (self-review) checks against. If the template is missing the "Research coverage" or "Implementation checklist", the self-review machinery cannot run.

---

## Frontmatter (required)

```yaml
---
author: <author>
date_created: YYYY-MM-DD
summary: >-
  One- or two-sentence summary of what this phase delivers and what it
  intentionally defers.
contributors: []
---
```

## Title (required)

```markdown
# Phase N: <Phase Title>
```

## Section: Context (required)

Why this phase exists. What previous phases delivered (link to those phase specs). What this phase enables for later phases. Keep to one paragraph.

## Section: Behavior delivered (required)

Bullet list of user-visible / behaviorally-observable changes at the end of this phase. Be specific.

Bad: "Edit panel works."
Good: "Adding a new group with name, description, and parent group selection saves the entity, redirects to its detail URL, and renders the View panel with the group's name, description, audit timestamps."

## Section: Behavior NOT delivered (required)

Bullet list of what is intentionally deferred, with the phase number where it lands.

Bad: "Other things later."
Good: "Group attribute values: Phase 4. Group requirements editing: Phase 5. Locations editing modal: Phase 6."

## Section: Deferred behaviors inherited from prior phases (required)

This section enumerates every behavior marked `→ DEFERRED to Phase N` in any prior phase's "Self-review coverage report" (where N = this phase). The spec author MUST list every such row here at draft time so nothing falls off the radar across sessions.

The implementing model checks this section during opening (per SESSION-PROTOCOL.md Section A8) to verify every inherited-deferred behavior is either addressed by an "Implementation checklist" item OR explicitly re-deferred / dropped below with rationale. If an inherited row is missing, the spec is incomplete and the session must stop and ask.

For Phase 1 this section will be empty (no prior phases). For Phase 2+, populate from prior coverage reports.

### Inherited (covered in this phase's Implementation checklist)

The default case. Each row was deferred from a prior phase to this one and is now in scope.

```markdown
| Source phase | Behavior | Coverage-report origin | Checklist item that handles it |
|---|---|---|---|
| Phase 1 | Group Capacity (scalar field on General panel) | `webforms/16-archive-delete-copy.md` row in Phase 1 coverage report | Item N below |
| Phase 1 | Trailblazer styling on General-section fields | Phase 1 "Behavior NOT delivered" + design 03-net-new-features item C4 | Items M, M+1 below |
```

### Re-deferred to a later phase

If anything inherited cannot be addressed in this phase (scope creep, unresolved dependency, etc.), list it here with the new target phase and rationale. The next-phase spec will absorb it via its own "inherited" section.

```markdown
| Source phase | Behavior | Originally targeted | New target | Rationale |
|---|---|---|---|---|
| Phase 1 | <behavior> | Phase N | Phase N+1 | <reason> |
```

### Dropped (no longer in scope)

If anything inherited is now considered out-of-scope (feature dropped, design changed, parity revisited), list it here with rationale so future phases don't re-pick-it-up.

```markdown
| Source phase | Behavior | Reason |
|---|---|---|
| Phase 1 | <behavior> | <why dropped, who confirmed> |
```

## Section: Research coverage (required, mandatory for self-review)

Every implementation session reads these files in full at session start (per SESSION-PROTOCOL.md Section A6) and audits implementation against them at session close (per SESSION-PROTOCOL.md Section C2).

Format:

```markdown
- [research/webforms/X.md](../webforms/X.md) — relevant for: <which behaviors / sub-features in scope>
- [research/design/Y.md](../design/Y.md) — relevant for: <which UI regions in scope>
- [research/specs/00-architecture.md](00-architecture.md) — always relevant
```

Be honest about what's in scope. If a research file's content overlaps multiple phases, list it for both phases and use "Behavior NOT delivered" + "Implementation checklist" to disambiguate which slice is in scope here.

## Section: Implementation checklist (required, mandatory for self-review)

Enumerate every behavior, validation, side effect, and edge case from research that this phase implements. This is the working list. The model checks items off via TodoWrite during implementation, and self-review walks this list at session close.

Format as a numbered list grouped by sub-feature. Each item should be specific enough that a code reviewer can find the corresponding implementation.

```markdown
### <Sub-feature 1, e.g., Block shell + chrome>

1. Register block class with correct GUID attributes per architecture spec Q5 (active BlockTypeGuid reuses WebForms GUID 582BEEA1-..., new EntityTypeGuid).
2. Register `[ContextAware(typeof(Group))]` per architecture spec Q1 resolution.
3. Implement `GetInitialEntity()` accepting both integer Id and IdKey (via `GetInitialEntity<Group, GroupService>`).
4. Implement breadcrumb via `IBreadCrumbBlock.GetBreadCrumbs(PageReference)` returning group name or "New Group".
5. ... (continue per research)

### <Sub-feature 2, e.g., View panel rendering>

10. Render Overview card per design/01-view-panel.md "Left column".
11. Render Group Tools card with Participation + Views sub-headers per design.
12. Render Linkages section only if any registrations / event item occurrences / content items exist (per design "show only if linkages exist" rule).
13. ...
```

Bad checklist item: "Implement Save."
Good checklist item: "Save flow: persist group.Name, group.IsActive, group.IsPublic per webforms/04-code-behind-walkthrough.md:1050-1067, including the InactiveReason / InactiveNote conditional clear when IsActive is true."

## Section: Out-of-scope items (required)

Explicit list of behaviors mentioned in research that this phase does NOT cover, with the phase that does.

```markdown
- Inline-schedule deletion logic — covered by Phase 6.
- Chat-channel-avatar IsTemporary toggle — covered by Phase 3.
- Group requirements editing modal — covered by Phase 5.
```

This list is the explicit "out of scope by design" set. If a behavior is in research but is in NEITHER "Implementation checklist" NOR "Out-of-scope items", that's a gap to flag during user review.

## Section: Files to create / modify (required)

Organized by project. Mark new vs modify.

```markdown
### Rock.Blocks/Group/
- `GroupDetail.cs` (NEW)

### Rock.ViewModels/Blocks/Group/GroupDetail/
- `GroupBag.cs` (NEW)
- `GroupDetailOptionsBag.cs` (NEW)

### Rock.JavaScript.Obsidian.Blocks/src/Group/
- `groupDetail.obs` (NEW)

### Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/
- `viewPanel.partial.obs` (NEW)
- `editPanel.partial.obs` (NEW, placeholder for Phase 3)
- `copyModal.partial.obs` (NEW)
- `auditModal.partial.obs` (NEW)
- `types.partial.ts` (NEW)
```

## Section: Bag fields contributed (required if bag changes)

Exact field names + types added to GroupBag in this phase. TypeScript or C# type signatures.

```typescript
interface GroupBag {
    idKey: string;
    name: string;
    description: string;
    isActive: boolean;
    isPublic: boolean;
    // ...
}
```

## Section: Block actions (required if actions change)

List the block actions implemented in this phase with their request and response shapes.

```markdown
| Action | Request | Returns | Notes |
|---|---|---|---|
| Edit | { key: string } | ValidPropertiesBox<GroupBag> | Returns full edit-mode bag for an existing group, or empty bag for new |
| Delete | { key: string } | string (redirect URL) | Auth-checked, calls GroupService.Delete |
```

## Section: Save action contributions (required if Save scope changes)

Which fields the Save block action persists in this phase. Which cascade / cache invalidations fire. Order of operations.

## Section: Code patterns to follow (required)

Reference patterns from the canonical reference block (typically `Rock.Blocks/Group/GroupTypeDetail.cs`):

```markdown
- Bag construction split: `GetCommonEntityBag` / `GetEntityBagForView` / `GetEntityBagForEdit`.
- Partial-update via `IfValidProperty(nameof(box.Bag.X), () => entity.X = box.Bag.X)`.
- Related-entity diff/upsert via the `SyncRelatedEntities<>` helper.
- ...
```

## Section: Design references (required if UI changes)

Specific Figma frame IDs and screenshots covering this phase's UI:

```markdown
- Edit Section 1: [research/design/screenshots/edit-section-01.png](../design/screenshots/edit-section-01.png) (frame 4552:29308)
- View panel main: [research/design/screenshots/view-panel-main.png](../design/screenshots/view-panel-main.png) (frame 4859:15223)
- Copy modal: [research/design/screenshots/view-modal-copy.png](../design/screenshots/view-modal-copy.png) (frame 5017:62518)
```

## Section: Mid-phase decisions log (required, initially empty)

Initially empty. Append entries during implementation when scope clarification happens. Format:

```markdown
- 2026-MM-DD: <decision summary>. Reason: <why>. Direction from user: <user's input>. Affected: <which checklist items, files, etc.>
```

## Section: Verification plan (required)

Manual test scenarios the user can execute to confirm phase behavior. Numbered, step-by-step, with expected outcomes.

```markdown
1. Navigate to `/Group/123` (any existing group). Confirm View panel renders with group name, description, image (if set), administrator link, parent group link, schedule, capacity.
2. Click Edit. Confirm edit-mode panel opens (placeholder for Phase 3 OK).
3. Click Cancel. Confirm view mode returns.
4. Click Delete. Confirm confirmation dialog. Confirm cancel dismisses dialog. Confirm OK deletes the group and redirects per the WebForms NavigateAfterDeleteOrArchive logic.
5. With ?autoEdit=true URL parameter, confirm edit panel opens directly.
6. Confirm IdKey URLs work: /Group/abc12def (IdKey form) loads the same group as /Group/123.
7. ...
```

## Section: Self-review coverage report (required, initially empty)

Initially empty. Populated by the model during Section C of SESSION-PROTOCOL.md. Format:

```markdown
| Research file | Behavior | Status | Code ref | Notes |
|---|---|---|---|---|
```

## Section: Completed (required, initially empty)

Initially empty. Populated by the model during Section D of SESSION-PROTOCOL.md.

```markdown
### Summary
<2-3 sentences: what shipped>

### Coverage report
<verbatim from Self-review coverage report above>

### Deviations from spec
<any deviations + rationale, or "None">

### Files changed
<list of file paths>

### New latent bugs / TODOs surfaced
<any items observed but not fixed in this phase>

### Commit hash
<7-character SHA>

### Completion date
YYYY-MM-DD
```

---

## Authoring tips

- The "Implementation checklist" is the most important section. Spend time on it. A vague checklist creates a vague implementation.
- Items in the checklist should be cross-referenced to research. "Per webforms/X.md:NNN" makes the checklist auditable.
- "Research coverage" should never be empty. If a phase has no research foundation, that's a sign the phase is poorly scoped.
- "Behavior NOT delivered" is just as load-bearing as "Behavior delivered". Be explicit about deferrals.
- The user reads the spec before the next session starts. Write it for them, not for the implementing model.
- If you find yourself unsure whether a behavior is in scope, that is exactly the kind of thing to ask the user about during spec authoring, NOT during implementation.
