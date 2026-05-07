---
author: Maxwell Eley
date_created: 2026-05-07
summary: >-
  Phase 2 implementation spec for the GroupDetail Obsidian conversion. Adds
  the new `Group.PhotoId` column (mirroring `Person.PhotoId`) plus EF
  migration and Rock.CodeGeneration regen, populates `bag.PhotoUrl` server
  side so the Phase 1 hero region begins rendering, and adds the Meeting
  Locations card (read only) on the right rail with per-`GroupLocation` 16:9
  map cards and hover-expand to `GroupMapPage`. Locations editing modal stays
  Phase 6; Group photo uploader stays Phase 3.
contributors: []
---

# Phase 2: Complete the view panel (Group Image hero + Meeting Locations card)

## Context

Phase 1 ([01-phase-1-shell-and-view.md](01-phase-1-shell-and-view.md)) shipped the block shell and the view-panel core but left the view panel half-finished in two places. The Group Image hero region was wired up but renders nothing because `bag.PhotoUrl` is always null: `Group` has no photo column today (Q8 confirmed this and locked the column add for Phase 2). And the Meeting Locations card was deferred. After Phase 1 self-review, the user requested view-first reordering: complete the entire read-only experience before any edit-panel work begins. Phase 2 closes both gaps so the read-only side is frozen for the rest of the conversion. The block class at [Rock.Blocks/Group/GroupDetail.cs](../../Rock.Blocks/Group/GroupDetail.cs) and the partial at [viewPanel.partial.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/viewPanel.partial.obs) are the foundation Phase 2 extends; [00-architecture.md Q8](00-architecture.md) drives the column add.

## Behavior delivered

- New `Group.PhotoId` column (nullable int FK to `BinaryFile`) added on the `[Group]` table via EF migration. Mirrors `Person.PhotoId` ([Person.cs:242](../../Rock/Model/CRM/Person/Person.cs:242)). FK is `WillCascadeOnDelete(false)` with `ON DELETE SET NULL`.
- Auto-generated bag types regenerate via Rock.CodeGeneration. The 8 hand-authored placeholder `.d.ts` files Phase 1 created get rewritten by the canonical generator; byte-shape parity confirmed.
- Server-side `bag.PhotoUrl` populated when `entity.PhotoId.HasValue`. With this, the Phase 1 hero region (`v-if="modelValue?.photoUrl"` at [viewPanel.partial.obs:5-7](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/viewPanel.partial.obs:5)) begins rendering the 16:9 image. When PhotoId is null, the region continues to omit entirely (no placeholder, per the design).
- New Meeting Locations card on the right rail below the Group Tools card. Card-level visibility: omitted entirely when the group has no `GroupLocation` rows.
- Each `GroupLocation` renders as a 16:9 map card showing: a map at top (uses `MapStyle` block setting), address text below the map (multi-line, conditional), and schedule text below the address (conditional). On hover, an expand-button overlays the map; click navigates to `GroupMapPage` with the group's IdKey on `GroupId` per Q4.
- Polygon-style locations (where `Location.GeoFence != null`) render the polygon visualization with no address text.
- Member-address locations (where `GroupLocation.GroupMemberPersonAliasId.HasValue`) render the family address.
- Standard address locations render `Location.FormattedAddress` (multi-line). Honors the `ShowLocationAddresses` block attribute (default true): when false, address text is suppressed on all card variants but map and schedule still render.

## Behavior NOT delivered

- Group photo uploader (writes `Group.PhotoId`): **Phase 3**, alongside the chat-channel-avatar uploader. Both use the `IsTemporary` BinaryFile pattern.
- Chat-channel-avatar uploader: **Phase 3**.
- Locations editing modal (Add / Edit / Delete): **Phase 6**.
- Inline schedule entity management on locations: **Phase 6**.
- All edit-mode field editing, Save flow, GroupType cascade, Add path: **Phase 3**.
- Cross-entity surfaces consuming `Group.PhotoId` (Person profile widgets, search results, etc.): explicitly out of scope per [00-architecture.md "Out of scope"](00-architecture.md).

## Deferred behaviors inherited from prior phases

Phase 1's coverage report has three rows targeting this phase. All three are addressed by Implementation checklist items below.

### Inherited (covered in this phase's Implementation checklist)

| Source phase | Behavior                                                             | Coverage-report origin                                            | Checklist item that handles it           |
| ------------ | -------------------------------------------------------------------- | ----------------------------------------------------------------- | ---------------------------------------- |
| Phase 1      | Map presentation (each Meeting Location renders as its own map card) | `design/00-overview.md` row in Phase 1 coverage report            | E1-E5 (Meeting Locations card rendering) |
| Phase 1      | Meeting Locations card (read-only)                                   | `design/01-view-panel.md` row in Phase 1 coverage report          | E1-E5                                    |
| Phase 1      | Meeting Locations card mapping                                       | `design/06-mapping-to-webforms.md` row in Phase 1 coverage report | E1-E5                                    |

The `Group.PhotoId` column add and `bag.PhotoUrl` population are not formally "deferred from Phase 1's coverage report" because Phase 1 wired the hero region but the column did not exist. They land here per the architecture-spec inheritance:

| Source                | Behavior                                                                                 | How addressed |
| --------------------- | ---------------------------------------------------------------------------------------- | ------------- |
| 00-architecture.md Q8 | New `Group.PhotoId` column add                                                           | A1-A5         |
| Phase 1 hero wiring   | Hero region was wired with `v-if="modelValue?.photoUrl"`; `bag.PhotoUrl` was always null | B1-B2         |

### Re-deferred to a later phase

None.

### Dropped (no longer in scope)

None.

## Open questions for spec lock

These items must be resolved by direct user input before the Phase 2 implementation session starts. Each has a default recommendation; the user either confirms or overrides. Pattern lifted from [00-architecture.md](00-architecture.md) Q1-Q12.

### Q2.1. Map component

Which Obsidian control renders the per-location map?

**Resolved (2026-05-07):** Custom [`locationCard.partial.obs`](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationCard.partial.obs). Reuse `@Obsidian/Utility/geo` ([geo.ts](../../Rock.JavaScript.Obsidian/Framework/Utility/geo.ts)) — specifically `loadMapResources` (loads the Google Maps JS API + fetches map-style settings via REST) and `wellKnownToCoordinates` (parses the DB's WKT format). Render the map directly with the raw Google Maps API in static mode: `disableDefaultUI: true`, `gestureHandling: "none"`, `clickableIcons: false`, with shapes created `clickable: false, editable: false`. No fork of `geoPickerMap.obs` (editing-first by design — `DrawingManager` is unconditional, polygons always render `editable: true`, the clear-shape button is always present). No new shared control.

**Why not `geoPickerMap.obs`:** the picker map has no `disabled` / `readOnly` / `interactive` prop ([geoPickerMap.obs:30-79](../../Rock.JavaScript.Obsidian/Framework/Controls/geoPickerMap.obs:30)); always instantiates `DrawingManager` with `drawingControl: true` ([line 204-219](../../Rock.JavaScript.Obsidian/Framework/Controls/geoPickerMap.obs:204)); polygons always render `editable: true` ([line 228](../../Rock.JavaScript.Obsidian/Framework/Controls/geoPickerMap.obs:228)); clear-shape button unconditionally added to map controls. CSS/prop suppression of all that is fragile.

### Q2.2. BinaryFile URL helper

Which Rock helper resolves a BinaryFile URL from `Group.PhotoId`?

**Resolved (2026-05-07):** Add a computed `PhotoUrl` property on the `Group` entity that mirrors `Person.PhotoUrl`'s **structure** ([Person.Logic.cs:374](../../Rock/Model/CRM/Person/Person.Logic.cs:374) — `[NotMapped] public virtual string PhotoUrl => Person.GetPersonPhotoUrl( this )`) but without the person-aware no-photo fallback. Returns null when `PhotoId` is null:

```csharp
[LavaVisible]
[NotMapped]
public virtual string PhotoUrl => PhotoId.HasValue
    ? FileUrlHelper.GetImageUrl( PhotoId.Value )
    : null;
```

The block bag does `bag.PhotoUrl = entity.PhotoUrl` (no inline computation in the block).

**Why this shape:**

- Sibling pattern with `Person.PhotoUrl`. Cross-Rock convention.
- `[LavaVisible]` so future Lava templates can read `{{ Group.PhotoUrl }}` without per-block plumbing.
- Returns null when `PhotoId` is null, preserving the design intent that the hero region omits entirely (no placeholder).
- Single source of truth — if PhotoId resolution rules ever change, only one place updates.

**Why not Person's helper or fallback:**

- `Person.GetPersonPhotoUrl(...)` includes person-aware fallback by age / gender / record-type — Person-specific. Group has no equivalent fallback semantics; the design omits the region entirely.
- Inline-in-the-block would scatter the resolution logic if a future cross-block consumer appears (the architecture spec's "GroupDetail-scoped only" out-of-scope clause is for Phase 2 reach, not for "make it impossible to share later"; an entity property keeps the door open at zero cost).

**Helper:** `FileUrlHelper.GetImageUrl( int binaryFileId )` at [FileUrlHelper.cs:109](../../Rock/Utility/FileUrlHelper.cs:109). Canonical binary-file URL helper; no person-specific behavior baked in.

### Q2.3. GroupMapPage per-location deep-link

Does `GroupMapPage` accept a per-location parameter so each card's expand-button targets a specific location, or do all expand buttons target the same group-level map URL?

**Resolved (2026-05-07):** Same group-level URL on every card. Every card's `mapUrl` resolves to `GroupMapPage?GroupId={IdKey}` (per Q4 IdKey policy). No per-location parameter. The destination block (`Groups/GroupMap.ascx`) is still WebForms today and accepts only `GroupId`; per-location selection is a UX nicety, not blocking. If the destination later adds `LocationId` deep-link support, Phase 2's `mapUrl` shape can pass it through trivially without breaking existing consumers. Practical consequence: every `<LocationCard>` on a given group renders the same `mapUrl` value; that's expected.

### Q2.4. Mobile / touch behavior

Hover-to-reveal expand button is a desktop-only affordance. What happens on touch devices?

**Resolved (2026-05-07):** Tap-on-card navigates directly to `modelValue.mapUrl`. Skip the separate expand button on touch — the hover overlay is desktop-only. Implementation: `@click` on the card root is the canonical navigation handler (fires on both click and tap); the expand-button overlay sits inside a `@media (hover: hover)` CSS block so it doesn't render at all on touch. Closes the open question previously flagged at [design/01-view-panel.md:157](../design/01-view-panel.md).

### Q2.5. `MapData` shape on the bag

What's the shape of `GroupMeetingLocationBag.MapData`?

**Resolved (2026-05-07):** `MapData: string` carrying the raw WKT format the database stores (e.g., `POINT(-112.130946 33.600114)` or `POLYGON((-112.157058 33.598563, ...))`). Parse client-side via `wellKnownToCoordinates(mapData, mode)` from `@Obsidian/Utility/geo`. The `mode` field on the bag (Address / Point / Polygon / GroupMember) drives which parse path runs. No custom JSON shape, no per-block transformation; the WKT format is what `Location.GeoPoint` and `Location.GeoFence` already store as `DbGeography`, so this is a direct passthrough. Member-address locations resolve their map data from the family's `Location.GeoPoint` (same WKT shape).

### Q2.6. `ShowLocationAddresses` scope

Does `ShowLocationAddresses=false` suppress address text on member-address locations too, or only on Address-type locations?

**Resolved (2026-05-07):** Suppress on all card variants uniformly. The block attribute reads as a global "hide addresses in the view panel" toggle — when false, no card renders address text regardless of `Mode`. Map and schedule still render. Polygon-style cards already render no address (independent of this flag). Server-side: in `BuildMeetingLocations`, gate the `Address` field assignment on `GetAttributeValue(AttributeKey.ShowLocationAddresses).AsBoolean(true)` for every card type (Address / Point / GroupMember). Polygon: always null.

## Research coverage

Every implementation session reads these files in full at session start (per SESSION-PROTOCOL.md Section A6) and audits implementation against them at session close (Section C2).

- [research/specs/00-architecture.md](00-architecture.md): always relevant. Q8 drives the column add. Q4 IdKey policy applies to the hover-expand URLs.
- [research/specs/01-phase-1-shell-and-view.md](01-phase-1-shell-and-view.md): the Phase 1 spec, including the Self-review coverage report (the source of the inherited-deferred rows above) and the Mid-phase decisions log (so Phase 2 doesn't re-litigate Phase 1 conventions).
- [research/webforms/01-block-configuration.md](../webforms/01-block-configuration.md): `MapStyle` (lines 159-166), `ShowLocationAddresses` (lines 223-228), `GroupMapPage` (lines 167-174). Phase 2 consumes all three.
- [research/webforms/05-entity-and-services.md](../webforms/05-entity-and-services.md): `GroupLocation` entity model, `Location.GeoPoint` / `Location.GeoFence` / `GroupLocation.GroupMemberPersonAliasId`, `LocationPickerMode` flags. Phase 2 reads these to choose the four card variants.
- [research/webforms/07-locations-and-schedules.md](../webforms/07-locations-and-schedules.md): `GroupLocation` query path (`Group.GroupLocations.Include(Location, Schedules)`), `Schedule.FriendlyScheduleText`, address formatting. Phase 2 uses the read-only side; the editing modal is Phase 6 scope and explicitly out of scope here.
- [research/webforms/14-chat.md](../webforms/14-chat.md): `Group.ChatChannelAvatarBinaryFileId` shape, `IsTemporary` toggle pattern. Phase 2 mirrors the column-shape decisions when adding `Group.PhotoId` (FK + `WillCascadeOnDelete(false)`); the IsTemporary toggle on the uploader side is Phase 3.
- [research/webforms/23-validations-and-cascades.md](../webforms/23-validations-and-cascades.md): orphan binary-file cleanup pattern; FK cascade conventions for the new column.
- [research/design/00-overview.md](../design/00-overview.md): Map presentation cross-cut.
- [research/design/01-view-panel.md](../design/01-view-panel.md): full Meeting Locations card walkthrough (lines 88-105) with three captured variants; "Group image (16:9 hero)" notes (line 43).
- [research/design/03-net-new-features.md](../design/03-net-new-features.md): items #1 (Group Image), #5 (Map Cards for Meeting Locations), #21 (Group Image hero render).
- [research/design/06-mapping-to-webforms.md](../design/06-mapping-to-webforms.md): Meeting Locations card mapping; Image mapping.
- Reference: `Rock/Model/CRM/Person/Person.cs:242` — canonical `Person.PhotoId` shape that `Group.PhotoId` mirrors.
- Reference: `Rock/Model/Group/Group/Group.cs:623` — `ChatChannelAvatarBinaryFileId` for cross-comparison.

## Implementation checklist

### A. `Group.PhotoId` column add (entity model + EF config + migration + codegen)

A1. Add `int? PhotoId` property to [Rock/Model/Group/Group/Group.cs](../../Rock/Model/Group/Group/Group.cs) mirroring `Person.PhotoId` at [Person.cs:242](../../Rock/Model/CRM/Person/Person.cs:242). Include `[DataMember]` and the standard XML doc comment.
A2. Add the navigation property `public virtual BinaryFile Photo { get; set; }` to `Group.cs` for EF lazy-loading.
A3. Add the computed `PhotoUrl` property to `Group.cs` (likely in `Group.Logic.cs` if the partial-class pattern is used, mirroring [Person.Logic.cs:374](../../Rock/Model/CRM/Person/Person.Logic.cs:374)) per Q2.2 resolution: `[LavaVisible] [NotMapped] public virtual string PhotoUrl => PhotoId.HasValue ? FileUrlHelper.GetImageUrl( PhotoId.Value ) : null;`. Standard XML doc comment.
A4. Add the FK configuration to the `Group`'s `EntityTypeConfiguration` (typically the inner `GroupConfiguration` class or wherever the EF config lives in `Rock/Model/Group/Group/`). Use `HasOptional(g => g.Photo).WithMany().HasForeignKey(g => g.PhotoId).WillCascadeOnDelete(false)` per `.claude/rules/data-model.md`.
A5. Generate an EF migration via `Add-Migration AddGroupPhotoId -ProjectName Rock.Migrations`. Author the Up() body to add the nullable `PhotoId` column on `[Group]` plus the FK to `[BinaryFile]` with `ON DELETE SET NULL`. Author the Down() body to drop the FK then the column. Both must follow the SQL formatting rules in `.claude/rules/code-conventions.md` (uppercase keywords, bracket-wrapped names, IF NOT EXISTS guards, `AS` aliases).
A6. Run Rock.CodeGeneration (the WPF tool) to regenerate auto-generated bag types under `Rock.JavaScript.Obsidian/Framework/ViewModels/`. This regen also rewrites the 8 hand-authored placeholder `.d.ts` files Phase 1 created with canonical generated content. Confirm byte-shape parity (regen should be a no-op on those 8 files except for the new `photoId` field on the Group entity bag).

### B. `bag.PhotoUrl` population

B1. In `GroupDetail.cs`'s `GetCommonEntityBag(group)`, populate `bag.PhotoUrl = entity.PhotoUrl` per Q2.2 (the entity property handles null → null and PhotoId → URL via `FileUrlHelper.GetImageUrl`).
B2. The Phase 1 view partial's hero region already gates on `v-if="modelValue?.photoUrl"`. Verify behavior at runtime: hero shows when `PhotoUrl` is set, omits when null.

### C. Bag fields backing the Meeting Locations card

C1. Add `public List<GroupMeetingLocationBag> MeetingLocations { get; set; }` to [GroupBag.cs](../../Rock.ViewModels/Blocks/Group/GroupDetail/GroupBag.cs). Always emit (empty list when no locations); the Vue layer's `v-if="meetingLocations?.length"` hides the entire card.
C2. Create [Rock.ViewModels/Blocks/Group/GroupDetail/GroupMeetingLocationBag.cs](../../Rock.ViewModels/Blocks/Group/GroupDetail/GroupMeetingLocationBag.cs) with these fields:
    - `string Name` — `GroupLocation.Location.Name` or formatted variant.
    - `string Address` — `Location.FormattedAddress` multi-line; null for polygon-style or when `ShowLocationAddresses == false` (per Q2.6 resolution).
    - `string ScheduleText` — friendly text from associated schedules (`GroupLocation.Schedules.FirstOrDefault()?.FriendlyScheduleText`); null when no schedule.
    - `LocationPickerMode Mode` — Address / Point / Polygon / GroupMember. Drives Vue's per-card render variant.
    - `string MapData` — raw WKT format (`POINT(...)` or `POLYGON((...))`) per Q2.5; parsed client-side via `wellKnownToCoordinates`. Direct passthrough from `Location.GeoPoint` / `Location.GeoFence`. For Member-address locations, resolve to the family `Location.GeoPoint`. Empty string when no geo data.
    - `string MapUrl` — pre-resolved URL to `GroupMapPage` with the group's IdKey on `GroupId` per Q4. Per Q2.3, all cards share the same URL (no per-location deep-link parameter). The field is per-card on the bag for forward-compat in case the destination later supports `LocationId`.
C3. Create [Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupMeetingLocationBag.d.ts](../../Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupMeetingLocationBag.d.ts) as a hand-authored placeholder; A5's Rock.CodeGeneration regen overwrites it with the canonical version.
C4. Server-side population in `GetEntityBagForView` of `bag.MeetingLocations` from `group.GroupLocations` (eagerly include `Location` and `Schedules`):
    - For each `GroupLocation`, build a `GroupMeetingLocationBag` with the fields above.
    - Polygon-style locations (`Location.GeoFence != null`): emit `Mode = Polygon`, `Address = null`.
    - Point locations (`Location.GeoPoint != null && Location.GeoFence == null`): emit `Mode = Point`; address conditional on `ShowLocationAddresses`.
    - Member-address locations (`GroupLocation.GroupMemberPersonAliasId.HasValue`): emit `Mode = GroupMember`; render the family's home address.
    - Address locations: emit `Mode = Address`; render `Location.FormattedAddress`.
    - Honor `ShowLocationAddresses` block attribute (default true): when false, suppress Address text on all card types per Q2.6 resolution.

### D. Existing `MapStyle` plumbing surfaced for Vue

D1. The `MapStyle` block attribute is already declared in Phase 1 (per coverage report row at `Rock.Blocks/Group/GroupDetail.cs:84-93`). Phase 2 surfaces the resolved DefinedValue Guid on `GroupDetailOptionsBag` as `mapStyleValueGuid: Guid?` so the Vue map renderer can read it.
D2. The `GroupMapPage` block attribute is already declared in Phase 1 with the `((Key))` substitution pattern. Phase 2 reuses it for hover-expand URLs. Per Q2.3, all cards on a given group share the same URL — same value emitted into each `GroupMeetingLocationBag.MapUrl` field; if forward-compat ever requires per-location URLs, only the server-side resolution changes.

### E. Vue rendering — Meeting Locations card + per-location card partial

E1. Create [Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationCard.partial.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationCard.partial.obs) — per-location card component encapsulating one Meeting Location render. Takes a `modelValue: GroupMeetingLocationBag` prop plus the `mapStyleValueGuid` from the parent. The Meeting Locations card wrapper (in `viewPanel.partial.obs`, see E5) renders one `<LocationCard>` per `bag.meetingLocations[]` item via `v-for`.
E2. Inside `locationCard.partial.obs`, render in this order from top to bottom:
    - 16:9 aspect-ratio map container. Use the raw Google Maps API directly (per Q2.1) — call `loadMapResources({ mapStyleValueGuid })` from `@Obsidian/Utility/geo` once per component to async-load Google Maps and fetch map-style settings; instantiate `new google.maps.Map(...)` with `disableDefaultUI: true, gestureHandling: "none", keyboardShortcuts: false, clickableIcons: false`. Set the styled map type from the resolved `mapStyleValueGuid`.
    - Render the shape:
      - `Mode = Point` or `GroupMember` (with geo data): `wellKnownToCoordinates(mapData, "Point")` → `new google.maps.Marker({ position, map, clickable: false })`.
      - `Mode = Polygon`: `wellKnownToCoordinates(mapData, "Polygon")` → `new google.maps.Polygon({ paths, map, clickable: false, editable: false, strokeWeight: 2 })`.
      - `Mode = Address` with `mapData == ""`: skip the marker; just center the map on the address-derived coordinate (resolved server-side and passed via `mapData`, or omit the map if `mapData` is empty — confirm in implementation).
    - Address text below the map (`v-if="modelValue.address"`), multi-line via `white-space: pre-line`.
    - Schedule text below the address (`v-if="modelValue.scheduleText"`).
    - Hover overlay: an absolutely-positioned expand button (icon `ti-arrows-maximize` or design-confirmed equivalent), `opacity: 0` by default, `opacity: 1` on `:hover` of the card. Clicking navigates to `modelValue.mapUrl` via `window.location` or a Vue Router push (sibling-block convention TBD during implementation).
E3. Touch / no-hover behavior per Q2.4: tap-on-card navigates directly to `modelValue.mapUrl`. Implement via `@click` on the card root plus the desktop-only hover overlay (controlled by CSS `@media (hover: hover)`).
E4. Three card variants must render correctly per [design/01-view-panel.md:100-105](../design/01-view-panel.md):
    - Standard street-address (`Mode = Address`): full address + schedule + point-marker map.
    - Polygon-style (`Mode = Polygon`): polygon overlay + label "Geofenced Location" or design-equivalent. No address.
    - Member-address (`Mode = GroupMember`): family's home address + point-marker map at the home coordinate.
E5. In `viewPanel.partial.obs`, add the Meeting Locations card to the right rail below the Group Tools card:
    - Card-level visibility: `v-if="modelValue?.meetingLocations?.length"` — entire card omitted when no locations.
    - Card heading "Meeting Locations" follows the right-rail heading style established for the Group Tools card.
    - Body: `<LocationCard v-for="loc in modelValue.meetingLocations" :key="loc.idKey ?? loc.name" :modelValue="loc" :mapStyleValueGuid="modelValue.options?.mapStyleValueGuid" />`.
    - Card spacing matches the Group Tools card above (right-rail consistency).

### F. Group Image hero verification

F1. With B1 in place, navigate to a group whose `Group.PhotoId` is set (after running A4 migration; before Phase 3's uploader ships, set the column manually via SQL using a real `BinaryFile.Id`). Confirm the hero region in the Overview card renders the 16:9 image.
F2. With `Group.PhotoId == null`, confirm the hero region omits entirely (no placeholder, per design `00-overview.md` line 43).

## Out-of-scope items

- Group photo uploader (writes `Group.PhotoId`): **Phase 3**.
- Chat-channel-avatar uploader: **Phase 3**.
- Locations editing modal: **Phase 6**.
- Inline schedule entity management on locations: **Phase 6**.
- Group attribute value editing: **Phase 4**.
- Edit panel and Save flow: **Phase 3**.
- Cross-entity surfaces consuming `Group.PhotoId` (Person profile widgets, search results): explicitly out of scope per [00-architecture.md "Out of scope"](00-architecture.md).
- Updating still-WebForms outbound destinations to accept IdKey: **Phase 7**.

## Files to create / modify

### Rock/Model/Group/Group/

- `Group.cs` (MODIFY) — add `PhotoId` property + `Photo` nav property + `PhotoUrl` computed property (the latter likely in `Group.Logic.cs` if the partial-class pattern is used, mirroring `Person.Logic.cs`).
- `GroupConfiguration.cs` (MODIFY, or wherever the EF config lives) — add the FK configuration.

### Rock.Migrations/Migrations/

- `<timestamp>_AddGroupPhotoId.cs` (NEW) — Up() / Down() bodies. Scaffolded by `Add-Migration`.

### Rock.Blocks/Group/

- `GroupDetail.cs` (MODIFY) — populate `bag.PhotoUrl` and `bag.MeetingLocations`. Add `MapStyleValueGuid` to `GetBoxOptions` if Q2.1 calls for it.

### Rock.ViewModels/Blocks/Group/GroupDetail/

- `GroupBag.cs` (MODIFY) — add `PhotoUrl` and `MeetingLocations` fields.
- `GroupDetailOptionsBag.cs` (MODIFY) — add `MapStyleValueGuid` if needed.
- `GroupMeetingLocationBag.cs` (NEW)

### Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/

- `groupBag.d.ts` (REGEN via Rock.CodeGeneration) — adds `photoUrl`, `meetingLocations`.
- `groupDetailOptionsBag.d.ts` (REGEN, conditional on D1).
- `groupMeetingLocationBag.d.ts` (NEW; placeholder until Rock.CodeGeneration regen).
- All 8 Phase 1 placeholders rewritten to canonical via Rock.CodeGeneration; confirm byte-shape parity.

### Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/

- `locationCard.partial.obs` (NEW) — per-location card component. Renders one Meeting Location: 16:9 map (via `@Obsidian/Utility/geo` + raw Google Maps API in static mode), address, schedule, hover-expand overlay.
- `viewPanel.partial.obs` (MODIFY) — Group Tools card retained; new Meeting Locations card wrapper added below it on the right rail with `v-for` over `bag.meetingLocations` rendering `<LocationCard>` per item.

## Bag fields contributed

```typescript
interface GroupBag {
    // (existing Phase 1 fields unchanged)

    // NEW in Phase 2
    photoUrl: string | null;                                // populated when Group.PhotoId.HasValue; null otherwise
    meetingLocations: GroupMeetingLocationBag[];            // empty list when no locations; card omitted via length-zero check
}

interface GroupMeetingLocationBag {
    name: string | null;                                    // GroupLocation.Location.Name or null
    address: string | null;                                 // multi-line; null per Q2.6
    scheduleText: string | null;                            // friendly schedule text; null when no schedule
    mode: LocationPickerMode;                               // Address / Point / Polygon / GroupMember; drives parse path
    mapData: string;                                        // raw WKT (POINT(...) or POLYGON((...))); empty string when no geo data; parse via wellKnownToCoordinates
    mapUrl: string;                                         // GroupMapPage URL with IdKey per Q4; per Q2.3 same value across all cards on a group
}
```

`GroupDetailOptionsBag` may add (conditional on Q2.1):

```typescript
interface GroupDetailOptionsBag {
    // (existing Phase 1 fields unchanged)

    // NEW in Phase 2 if Vue map component needs it
    mapStyleValueGuid: string | null;
}
```

## Block actions

No new block actions in Phase 2. The existing `Edit`, `Delete`, `Archive`, `ArchiveWithChildren`, `Copy` are unchanged. Phase 3 introduces `Save` and `GetGroupTypeOptions`.

## Save action contributions

n/a for Phase 2. Save lands in Phase 3.

## Code patterns to follow

- `Group.PhotoId` mirrors `Person.PhotoId` shape and FK configuration ([Person.cs:242](../../Rock/Model/CRM/Person/Person.cs:242)). Same nullable-int + `WillCascadeOnDelete(false)` + nav property pattern.
- FK cascade convention: `WillCascadeOnDelete(false)` per `.claude/rules/data-model.md` "Foreign Key Cascade Conventions" table.
- Migration SQL formatting per `.claude/rules/code-conventions.md`: uppercase keywords, bracket-wrapped names, IF NOT EXISTS / IF EXISTS guards, `AS` aliases on JOINs.
- Bag type naming: `GroupMeetingLocationBag` follows the sibling-bag convention (`GroupAdministratorBag`, `ParentGroupBag`, `GroupDetailGroupTypeBag`) — describes what the type represents without a `Ref` suffix.
- Map rendering: per Q2.1, custom `locationCard.partial.obs` using `@Obsidian/Utility/geo` (`loadMapResources` + `wellKnownToCoordinates`) + raw Google Maps API in static mode (`disableDefaultUI: true`, `gestureHandling: "none"`, `clickableIcons: false`; shapes `clickable: false, editable: false`).
- The `((Key))` substitution pattern for outbound URLs (already established in Phase 1) extends naturally to `mapUrl` if the destination needs per-instance substitution.
- Hand-authored `.d.ts` placeholders are unblocking only; always re-run Rock.CodeGeneration as part of the implementation pass to canonicalize.

## Design references

- View panel main: [research/design/screenshots/view-panel-main.png](../design/screenshots/view-panel-main.png) (Figma frame `4859-15223`) — Meeting Locations card on the right rail with three card variants.
- Meeting Locations region detail: [research/design/screenshots/view-locations-section.png](../design/screenshots/view-locations-section.png).
- Group image hero: visible at the top of the Overview card in `view-panel-main.png`.
- Compact alt state (no locations, hero omitted): [research/design/screenshots/view-panel-alt-state.png](../design/screenshots/view-panel-alt-state.png).

## Mid-phase decisions log

(initially empty; populate during implementation per SESSION-PROTOCOL.md Section B4)

## Verification plan

Manual test scenarios for the user's review playbook (per SESSION-PROTOCOL.md Section D7):

1. After running the Phase 2 migration, confirm `[Group].[PhotoId]` exists with type `int NULL`, FK to `[BinaryFile].[Id]`, and `ON DELETE SET NULL` cascade. Check via `sp_help [Group]` or SSMS.
2. Navigate to a group whose `PhotoId` is null (every existing group post-migration). Confirm the Overview card's hero region is omitted entirely (no placeholder, no broken-image icon).
3. Manually set `Group.PhotoId` to a valid `BinaryFile.Id` via SQL (or upload via Phase 3 uploader once it ships). Reload the group. Confirm the hero renders the 16:9 image at the top of the Overview card.
4. Delete the underlying `BinaryFile` row that PhotoId references. Confirm `Group.PhotoId` is set to NULL automatically (`ON DELETE SET NULL` behavior). Reload the group. Confirm hero omits.
5. Navigate to a group with no `GroupLocation` rows. Confirm the Meeting Locations card is omitted entirely from the right rail (only Group Tools card visible there).
6. Navigate to a group with one or more standard street-address locations. Confirm:
   - Each location renders as a 16:9 map card.
   - The map shows the correct location.
   - The full multi-line address renders below the map.
   - Schedule text (e.g., "Saturday 4:00pm") renders below the address when a schedule is associated.
   - Hover the card; confirm the expand button overlays the map.
   - Click the expand button; confirm navigation to `GroupMapPage?GroupId={IdKey}` per Q4.
7. Navigate to a group with a polygon-style location. Confirm:
   - The polygon renders inside the map card.
   - No address text below the map.
   - "Geofenced Location" label or design-equivalent appears.
8. Navigate to a group with a member-address location. Confirm:
   - The map shows the family's home address.
   - The address text renders the family address.
9. Toggle the `ShowLocationAddresses` block attribute to false. Reload. Confirm address text is suppressed on all card variants per Q2.6 resolution; map and schedule still render.
10. On a touch device, tap a Meeting Location card. Confirm Q2.4 resolution: tap navigates directly to `GroupMapPage` (no separate expand button).
11. Confirm the Phase 1 view-panel behaviors still work: header chrome, subheader chips, Overview card fields, Linkages section, Group Tools card, Audit modal, Copy / Delete / Archive flows. Phase 2 should not regress any Phase 1 surface.
12. Confirm Q4 IdKey policy still applies on every outbound URL (hover-expand to GroupMapPage; existing Group Tools card links). 5 still-WebForms destinations (`GroupListPage`, `FundraisingProgressPage`, `GroupHistoryPage`, `GroupMapPage`, `GroupSchedulerPage`) still produce broken links until Phase 7; document the observation rather than treating it as a regression.
13. Run Rock.CodeGeneration before opening the Phase 2 PR. Confirm the 8 Phase 1 placeholder `.d.ts` files plus the new `groupMeetingLocationBag.d.ts` regenerate without surprise diffs (only the new `photoId` / `photoUrl` / `meetingLocations` fields should appear).

## Self-review coverage report

(initially empty; populated by the implementing model per SESSION-PROTOCOL.md Section C)

## Completed

(initially empty; populated by the implementing model per SESSION-PROTOCOL.md Section D)
