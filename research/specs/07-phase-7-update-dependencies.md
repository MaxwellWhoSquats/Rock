---
author: Maxwell Eley
date_created: 2026-05-12
summary: >-
  Phase 7 of the GroupDetail Obsidian conversion: update the five still-WebForms
  outbound destinations (GroupListPage, FundraisingProgressPage, GroupHistoryPage,
  GroupMapPage, GroupSchedulerPage) to accept IdKey on their GroupId page
  parameter. Required because Phase 1 onward writes IdKey to every outbound URL
  GroupDetail emits per Q4 (00-architecture.md), and these five destinations
  remain broken under IdKey URLs until updated. Tiny per-destination fix; entire
  phase is one session.
contributors: []
---

# Phase 7: Update Dependencies (IdKey Acceptance on 5 WebForms Destinations)

## Context

Per [00-architecture.md Q4](00-architecture.md), the converted GroupDetail block writes IdKey to every Id-style outbound page parameter, uniformly across all 11 outbound LinkedPage destinations. Six destinations already accept IdKey today: AttendancePage, GroupRSVPPage, GroupPlacementPage, RegistrationInstancePage (Obsidian), EventItemOccurrencePage, ContentItemPage. The other five remain on `.AsIntegerOrNull()` and break under IdKey URLs.

Phase 6 closed the feature surface of the conversion. Only this Phase 7 (per-destination IdKey acceptance) and Phase 8 (cutover + WebForms file deletion) remain. The fixes here are mechanical: replace `key.AsIntegerOrNull()` with `IdHasher.Instance.GetId(key) ?? key.AsIntegerOrNull()` (or the equivalent `GroupService.Get(key, !DisablePredictableIds)` overload), then re-test the navigation from the converted GroupDetail to each destination.

## Behavior delivered

- Each of the five still-WebForms destinations on the `GroupId` path accepts both integer Id and IdKey forms without changing its existing behavior for integer-Id callers. Specifically:
  - GroupListPage destination (`RockWeb/Blocks/Groups/GroupTreeView.ascx.cs` and any expanded-node tree handlers that read `GroupId` from the route).
  - FundraisingProgressPage destination (the configured block at the page set by the `FundraisingProgressPage` attribute - location TBD during the session).
  - GroupHistoryPage destination (`RockWeb/Blocks/Groups/GroupHistory.ascx.cs`).
  - GroupMapPage destination (`RockWeb/Blocks/Groups/GroupMap.ascx.cs`).
  - GroupSchedulerPage destination (the configured block at the page set by the `GroupSchedulerPage` attribute - location TBD).
- After this phase ships, every link the converted GroupDetail emits navigates correctly regardless of whether the parameter is an integer Id or an IdKey.
- Smoke test: clicking each Group Tools card link from a converted GroupDetail loads the destination with the correct group data.

## Behavior NOT delivered

- Cutover and WebForms file deletion: **Phase 8**.
- Conversion of any of the 5 destination blocks to Obsidian: out of scope (acknowledged tradeoff per Q4; this phase only adds IdKey acceptance to the existing WebForms blocks).
- Conversion of any of the 11 still-integer-Id inbound callers to write IdKey: out of scope (those continue to write integer Id; GroupDetail accepts both forms so it's non-blocking).

## Deferred behaviors inherited from prior phases

### Inherited (covered in this phase's Implementation checklist)

| Source phase | Behavior | Coverage-report origin | Checklist item that handles it |
|---|---|---|---|
| Phase 1 | 5 still-WebForms outbound destinations break under IdKey URLs | Phase 1 "Behavior NOT delivered" + 00-architecture Q4 | D1-D5 below |

### Re-deferred to a later phase

None planned at draft time.

### Dropped (no longer in scope)

None planned at draft time.

## Research coverage

- [research/specs/00-architecture.md](00-architecture.md): always relevant - especially Q4 (IdKey policy) and the "Cross-block follow-on tracking" section listing the 5 destinations.
- [research/webforms/18-cross-block-dependencies.md](../webforms/18-cross-block-dependencies.md): definitive inventory of the cross-block callers + destinations.
- [research/specs/ROADMAP.md](ROADMAP.md): Phase 7 ("Update dependencies") scope rationale.

## Implementation checklist

### D. Per-destination IdKey acceptance

D1. **GroupTreeView.ascx.cs** (the GroupList page's tree navigation widget). Locate every `GroupId` page-parameter read and route the value through `IdHasher.Instance.GetId(key) ?? key.AsIntegerOrNull()` (or, if the block uses a `GroupService`, switch to the `Get(key, !DisablePredictableIds)` overload). Re-test the tree-view's "selected group" behavior under both integer-Id and IdKey URLs.

D2. **FundraisingProgress block** (path TBD - the block configured at the page set by the `FundraisingProgressPage` block attribute on GroupDetail). Same fix shape as D1.

D3. **GroupHistory.ascx.cs** at `RockWeb/Blocks/Groups/GroupHistory.ascx.cs`. Same fix shape.

D4. **GroupMap.ascx.cs** at `RockWeb/Blocks/Groups/GroupMap.ascx.cs`. Same fix shape.

D5. **GroupScheduler block** (path TBD - the block configured at the page set by the `GroupSchedulerPage` block attribute on GroupDetail). Same fix shape.

### V. Verification

V1. From a converted GroupDetail at `/Group/{id}` (integer Id), click each Group Tools card link and confirm the destination loads with the correct group context. Re-test from `/Group/{idkey}` (IdKey form) and confirm the same five destinations load correctly.
V2. Confirm no regression for callers writing integer Id - existing pages that hard-code `?GroupId=N` keep working.

## Out-of-scope items

- Converting any of the 5 still-WebForms destinations to Obsidian (each requires its own conversion spec; out of scope here).
- Touching any of the 11 still-integer-Id inbound callers (they keep writing integer Id; GroupDetail already accepts both forms).
- Adding IdKey acceptance to non-GroupId page parameters on these destinations (only the GroupId parameter is in scope).

## Files to create / modify

### RockWeb/Blocks/Groups/

- `GroupTreeView.ascx.cs` (MODIFY) — IdKey acceptance on `GroupId` parameter.
- `GroupHistory.ascx.cs` (MODIFY) — same.
- `GroupMap.ascx.cs` (MODIFY) — same.

### Other paths (locations TBD during session)

- FundraisingProgress block (file TBD) — same.
- GroupScheduler block (file TBD) — same.

## Bag fields contributed

None - this phase touches WebForms blocks; no Obsidian bags change.

## Block actions

None.

## Save action contributions

None.

## Code patterns to follow

- Standard Rock IdKey-acceptance idiom for WebForms: `IdHasher.Instance.GetId(key) ?? key.AsIntegerOrNull()`.
- When the block uses a `Service<T>` to load the entity, prefer the `Get(string key, bool allowIntegerIdentifier = true)` overload (or `Get(key, !PageCache.Layout.Site.DisablePredictableIds)` where the predictable-ids policy applies).
- Do NOT add new public method overloads (per `CLAUDE.md`'s "no new optional parameters that change public signatures" rule); the existing IdHasher / Service.Get overloads cover every case.

## Design references

None - WebForms block updates have no visual changes.

## Mid-phase decisions log

(initially empty; populate during implementation per SESSION-PROTOCOL.md Section B4)

## Verification plan

Manual test scenarios for the user's review playbook:

1. Navigate to `/Group/{integer-id}` (any existing group). Click each Group Tools card link (Attendance, Scheduler, RSVP, Map, History, etc.) and confirm each destination loads with the correct group context.
2. Navigate to `/Group/{idkey}` (same group, IdKey form). Repeat the click-through and confirm every destination loads correctly.
3. Compare the URL emitted by GroupDetail for each link - confirm it uses the IdKey form per Q4.
4. With the `PreventCachingForKey` site setting enabled (and any other DisablePredictableIds toggles), repeat the smoke test to confirm IdKey acceptance still works (or that the integer-Id fallback kicks in when predictable Ids are disabled, per the `Service.Get(key, !DisablePredictableIds)` semantics).
5. Open a still-integer-Id inbound caller's page (one of the 11 listed in webforms/18-cross-block-dependencies.md) and confirm the page still navigates correctly to GroupDetail. No regression for integer-Id callers.

## Self-review coverage report

(initially empty; populated by the implementing model per SESSION-PROTOCOL.md Section C)

## Completed

(initially empty; populated by the implementing model per SESSION-PROTOCOL.md Section D)
