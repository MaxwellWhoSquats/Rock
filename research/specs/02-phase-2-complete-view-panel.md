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

- New `Group.PhotoId` column (nullable int FK to `BinaryFile`) added on the `[Group]` table via plug-in migration ([Rock/Plugin/HotFixes/289_AddGroupPhotoId.cs](../../Rock/Plugin/HotFixes/289_AddGroupPhotoId.cs)). Ships as a hotfix rather than an EF migration because v19.0 is locked on a separate branch. Mirrors `Person.PhotoId` ([Person.cs:242](../../Rock/Model/CRM/Person/Person.cs:242)). FK is `WillCascadeOnDelete(false)` (NO ACTION on delete), matching the `Person.PhotoId` sibling pattern.
- Auto-generated bag types regenerate via Rock.CodeGeneration. The 8 hand-authored placeholder `.d.ts` files Phase 1 created get rewritten by the canonical generator; byte-shape parity confirmed.
- Server-side `bag.PhotoUrl` populated when `entity.PhotoId.HasValue`. With this, the Phase 1 hero region (`v-if="modelValue?.photoUrl"` at [viewPanel.partial.obs:5-7](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/viewPanel.partial.obs:5)) begins rendering the 16:9 image. When PhotoId is null, the region continues to omit entirely (no placeholder, per the design).
- New Meeting Locations card on the right rail below the Group Tools card. Card-level visibility: omitted entirely when the group has no `GroupLocation` rows.
- Each `GroupLocation` renders as a 16:9 map card showing: a map at top (uses `MapStyle` block setting), address text below the map (multi-line, conditional), and schedule text below the address (conditional). On hover, an expand-button overlays the map; click navigates to `GroupMapPage` with the group's IdKey on `GroupId` per Q4.
- Polygon-style locations (where `Location.GeoFence != null`) render the polygon visualization with no address text.
- Member-address locations (where `GroupLocation.GroupMemberPersonAliasId.HasValue`) render the family address.
- Standard address locations render `Location.FormattedAddress` (multi-line). Honors the `ShowLocationAddresses` block attribute (default true): when false, address text is suppressed on all card variants but map and schedule still render.
- Top-of-panel notification surface in view mode. Two banners now render at the top of the view panel, mirroring WebForms' `nbRoleLimitWarning` and the system-group case of `nbEditModeMessage`:
  - **System-group banner** (info): rendered when `bag.IsSystem`. Hardcoded message ("Because this group is used by Rock, editing is restricted."), matching `EditModeMessage.System(Group.FriendlyTypeName)` ([DisplayStrings.cs:222-225](../../Rock/Constants/DisplayStrings.cs:222)). The read-only-no-edit-auth case from the same WebForms control resolves to `EditModeMessage.ReadOnlyEditActionNotAllowed`, which returns `string.Empty` ([DisplayStrings.cs:252-255](../../Rock/Constants/DisplayStrings.cs:252)) so it never produced visible text. Skipped to match.
  - **Role limit warning banner** (warning): rendered when `bag.RoleLimitWarning` is non-empty. Surfaced server-side from `entity.GetGroupTypeRoleLimitWarnings(out string)` ([Group.Logic.cs:495](../../Rock/Model/Group/Group/Group.Logic.cs:495)), mirroring [GroupDetail.ascx.cs:1849-1851](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1849).

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
A5. Author a plug-in migration (hotfix) at `Rock/Plugin/HotFixes/289_AddGroupPhotoId.cs` rather than an EF migration. v19.0 is locked on a separate branch and no longer accepts new EF migrations, so the schema change must ship as a hotfix. Use `[MigrationNumber( 289, "19.0" )]` and two `Sql()` blocks with IF NOT EXISTS guards (column add, FK add). FK name `FK_dbo.Group_dbo.BinaryFile_PhotoId` mirrors the EF naming convention used by every existing FK on the `[Group]` table (sibling consistency). No covering index on `PhotoId`: the column has no realistic query workload, so the index would be dead weight on a hot table. NO ACTION on delete (matches the `Person.PhotoId` sibling pattern). All SQL must follow the formatting rules in `.claude/rules/code-conventions.md` (uppercase keywords, bracket-wrapped names, `AS` aliases). Down() is a no-op per plug-in migration convention.
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

### G. Top-of-panel notification surface

G1. Add `RoleLimitWarning: string` to `GroupBag.cs` (and the matching `roleLimitWarning?: string | null` to the placeholder `groupBag.d.ts`).
G2. In `GroupDetail.cs` `GetEntityBagForView`, call `entity.GetGroupTypeRoleLimitWarnings(out var warning)`; assign to `bag.RoleLimitWarning` when the method returns true.
G3. In `viewPanel.partial.obs`, render two `<NotificationBox>` instances above the existing `.row` wrapper:
    - `v-if="modelValue?.isSystem"` with `alertType="info"` and a hardcoded HTML body `"<strong>Note</strong> Because this group is used by Rock, editing is restricted."`. The string is in-component because `Group.FriendlyTypeName` is a static class-level value ("Group"), so passing it through the bag would be redundant.
    - `v-if="modelValue?.roleLimitWarning"` with `alertType="warning"`, `heading="Role Limit Warning"`, and `v-html="modelValue.roleLimitWarning"`.
G4. The "user attempted to copy without auth" notification from WebForms ([GroupDetail.ascx.cs:1528-1529](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1528)) is intentionally not ported. Our Copy button is only rendered when `options.IsCopyButtonShown` is true, which already gates on EDIT auth — the unauthorized-click branch can't fire client-side.

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

### Rock/Plugin/HotFixes/

- `289_AddGroupPhotoId.cs` (NEW) — plug-in migration with two `Sql()` blocks for column add and FK add (each guarded by IF NOT EXISTS). Replaces the original Phase 2 plan's EF migration because v19.0 is locked on a separate branch. No covering index on `PhotoId` (no query workload to support).

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
    roleLimitWarning: string | null;                        // HTML from Group.GetGroupTypeRoleLimitWarnings; null when no role-limit violations
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

Populated 2026-05-07 per SESSION-PROTOCOL.md Section C. Walks every file listed under "Research coverage" and classifies each in-scope behavior as ✓ IMPLEMENTED, → DEFERRED, or ✗ MISSED. Re-read each file with implementation eyes before populating.

### `research/specs/00-architecture.md`

| Behavior                                                                       | Status          | Code ref                                                                                                                                                                                                                                                                                                                                                                                      | Notes                                                                                                                                                                                                                                                                                                                                     |
| ------------------------------------------------------------------------------ | --------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Q8: add `Group.PhotoId` (nullable int → BinaryFile) mirroring `Person.PhotoId` | ✓               | [Group.cs:179-186](../../Rock/Model/Group/Group/Group.cs:179)                                                                                                                                                                                                                                                                                                                                 | Property + nav + EF config + computed PhotoUrl + migration.                                                                                                                                                                                                                                                                               |
| Q8: `Photo` nav property                                                       | ✓               | [Group.cs:885-892](../../Rock/Model/Group/Group/Group.cs:885)                                                                                                                                                                                                                                                                                                                                 | Sits next to `ChatChannelAvatarBinaryFile` so the BinaryFile nav properties cluster.                                                                                                                                                                                                                                                      |
| Q8: `WillCascadeOnDelete(false)` per data-model rules                          | ✓               | [Group.cs:935](../../Rock/Model/Group/Group/Group.cs:935)                                                                                                                                                                                                                                                                                                                                     | `HasOptional(p => p.Photo).WithMany().HasForeignKey(p => p.PhotoId).WillCascadeOnDelete(false)` matches the Person pattern verbatim.                                                                                                                                                                                                      |
| Q8: schema migration                                                           | ✓               | [289_AddGroupPhotoId.cs](../../Rock/Plugin/HotFixes/289_AddGroupPhotoId.cs)                                                                                                                                                                                                                                                                                                                   | Plug-in migration (hotfix). v19.0 is locked on a separate branch and no longer accepts new EF migrations. Column add + FK add only; no covering index on `PhotoId` because there is no query workload to support. FK name `FK_dbo.Group_dbo.BinaryFile_PhotoId` matches the EF naming used by every existing FK on `[Group]`.             |
| Q8: codegen regen for new bag types                                            | ✓ (with caveat) | [groupBag.d.ts](../../Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupBag.d.ts), [groupMeetingLocationBag.d.ts](../../Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupMeetingLocationBag.d.ts), [groupDetailOptionsBag.d.ts](../../Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupDetailOptionsBag.d.ts) | Hand-authored placeholders; user re-runs Rock.CodeGeneration before merging to canonicalize. Same approach as Phase 1.                                                                                                                                                                                                                    |
| Q4: outbound IdKey on `MapUrl` (Q2.3 group-level URL)                          | ✓               | [GroupDetail.cs:1166-1172](../../Rock.Blocks/Group/GroupDetail.cs:1166)                                                                                                                                                                                                                                                                                                                       | `BuildMeetingLocations` substitutes `entity.IdKey` server-side; per-card `MapUrl` field carries the pre-resolved URL.                                                                                                                                                                                                                     |
| "ON DELETE SET NULL" per data-model rules (architecture spec language)         | DEVIATION       | —                                                                                                                                                                                                                                                                                                                                                                                             | Spec text is internally inconsistent: A4 says `WillCascadeOnDelete(false)` (NO ACTION) and A5 says SET NULL. Person.PhotoId uses NO ACTION, so Phase 2 follows the sibling pattern (Prime Directive). Logged in mid-phase decisions. Verification step #4 in the spec is unreachable as written and is dropped from the manual test plan. |

### `research/specs/01-phase-1-shell-and-view.md`

| Phase 1 deferred row                                                                                                                                      | Status | Code ref                                                                                                                                                                                                                     | Notes                                                                                                                                                                                     |
| --------------------------------------------------------------------------------------------------------------------------------------------------------- | ------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `design/00-overview.md`: "Map presentation (each location its own map card)" → DEFERRED to Phase 2                                                        | ✓      | [locationCard.partial.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationCard.partial.obs), [viewPanel.partial.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/viewPanel.partial.obs) | Per-card 16:9 map with hover overlay.                                                                                                                                                     |
| `design/01-view-panel.md`: "Meeting Locations card" → DEFERRED to Phase 2                                                                                 | ✓      | Same as above + [GroupDetail.cs `BuildMeetingLocations`](../../Rock.Blocks/Group/GroupDetail.cs:1162)                                                                                                                        | Card-level visibility via `v-if` length check; per-card via `v-for`.                                                                                                                      |
| `design/06-mapping-to-webforms.md`: "Meeting Locations card" → DEFERRED to Phase 2                                                                        | ✓      | Same                                                                                                                                                                                                                         |                                                                                                                                                                                           |
| Phase 1 coverage labels `MapStyle`, `ShowLocationAddresses`, `GroupMapPage` map-card rendering as "Phase 6" (stale label, predates view-first reordering) | ✓      | `BuildMeetingLocations` honors `ShowLocationAddresses`; `GetBoxOptions` surfaces `MapStyleValueGuid`; `GroupMapPage` URL feeds `MapUrl`                                                                                      | Mid-phase decisions log records that the Phase 1 row's "Phase 6" label is now obsolete; the view-first reordering moved the read-side use to Phase 2. The edit-side use stays in Phase 6. |

### `research/webforms/01-block-configuration.md`

| Behavior                                                                               | Status | Code ref                                                                | Notes                                                                                                               |
| -------------------------------------------------------------------------------------- | ------ | ----------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------- |
| `MapStyle` block attribute exposed for Vue map                                         | ✓      | [GroupDetail.cs:387-390](../../Rock.Blocks/Group/GroupDetail.cs:387)    | `GetBoxOptions` surfaces `MapStyleValueGuid` before the entity-null early-return so it works during Add too.        |
| `MapStyle` default `MAP_STYLE_ROCK` preserved                                          | ✓      | [GroupDetail.cs:84-93](../../Rock.Blocks/Group/GroupDetail.cs:84)       | Phase 1 declared the attribute. Phase 2 reads it.                                                                   |
| `ShowLocationAddresses` block attribute consumed                                       | ✓      | [GroupDetail.cs:1175](../../Rock.Blocks/Group/GroupDetail.cs:1175)      | `BuildMeetingLocations` reads with `AsBoolean(true)` (default true matches the `[BooleanField]` declaration).       |
| `ShowLocationAddresses` gates address text uniformly across all card variants per Q2.6 | ✓      | [GroupDetail.cs:1255-1259](../../Rock.Blocks/Group/GroupDetail.cs:1255) | Polygon cards always have null address regardless; Address/Point/GroupMember cards null out when the flag is false. |
| `GroupMapPage` LinkedPage attribute consumed for hover-expand                          | ✓      | [GroupDetail.cs:1188-1196](../../Rock.Blocks/Group/GroupDetail.cs:1188) | Per Q2.3 every card on a group shares the same URL (`GroupMapPage?GroupId={IdKey}`).                                |
| `GroupMapPage` URL passes `((Key))` substitution at server                             | ✓      | [GroupDetail.cs:1188-1192](../../Rock.Blocks/Group/GroupDetail.cs:1188) | `this.GetLinkedPageUrl(AttributeKey.GroupMapPage, { GroupId = entity.IdKey })` returns the resolved URL.            |

### `research/webforms/05-entity-and-services.md`

| Behavior                                                 | Status | Code ref                                                                | Notes                                                                                                                                                                                                                        |
| -------------------------------------------------------- | ------ | ----------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `GroupLocation` entity surfaced for view                 | ✓      | [GroupDetail.cs:1180-1186](../../Rock.Blocks/Group/GroupDetail.cs:1180) | `GroupLocationService.Queryable().Include(Location).Include(Schedules).Where(GroupId=...).OrderBy(Order).ThenBy(Id)`.                                                                                                        |
| `Location.GeoPoint` / `Location.GeoFence` classification | ✓      | [GroupDetail.cs:1213-1234](../../Rock.Blocks/Group/GroupDetail.cs:1213) | Polygon when `GeoFence != null`; Point when `GeoPoint != null`; Address otherwise. GroupMember takes priority over geo classification.                                                                                       |
| `GroupLocation.GroupMemberPersonAliasId` classification  | ✓      | [GroupDetail.cs:1213-1219](../../Rock.Blocks/Group/GroupDetail.cs:1213) | When set, mode = GroupMember regardless of GeoPoint/GeoFence presence. Matches the WebForms research's "GroupMember tab sets `GroupMemberPersonAliasId`" rule.                                                               |
| `Location.FormattedAddress` for address text             | ✓      | [GroupDetail.cs:1244](../../Rock.Blocks/Group/GroupDetail.cs:1244)      | Multi-line CR/LF format; Vue renders with `white-space: pre-line`.                                                                                                                                                           |
| `Schedule.FriendlyScheduleText` for schedule text        | ✓      | [GroupDetail.cs:1268-1273](../../Rock.Blocks/Group/GroupDetail.cs:1268) | First active schedule's friendly text via `gl.Schedules.OrderBy(Order).ThenBy(Id).Select(FriendlyScheduleText).FirstOrDefault(IsNotNullOrWhiteSpace)`. Multi-schedule locations show only the first per the captured design. |

### `research/webforms/07-locations-and-schedules.md`

| Behavior                                                                   | Status                                                                              | Code ref                                                                | Notes                                                                                                       |
| -------------------------------------------------------------------------- | ----------------------------------------------------------------------------------- | ----------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- |
| Read-side: query `GroupLocations` with `Location` and `Schedules` includes | ✓                                                                                   | [GroupDetail.cs:1180-1186](../../Rock.Blocks/Group/GroupDetail.cs:1180) | Eager loads avoid N+1.                                                                                      |
| Read-side: `GroupLocationPickerMode` four-mode classification              | ✓                                                                                   | [GroupDetail.cs:1213-1234](../../Rock.Blocks/Group/GroupDetail.cs:1213) | Maps to `GroupMeetingLocationBag.Mode`; the Vue side switches on it for the per-variant render.             |
| Locations editing modal (Add / Edit / Delete)                              | DEFERRED to Phase 6                                                                 | —                                                                       | Out of scope per Phase 2 "Behavior NOT delivered" line 36.                                                  |
| Inline schedule entity management on locations                             | DEFERRED to Phase 6                                                                 | —                                                                       | Same.                                                                                                       |
| `GroupLocationScheduleConfig` capacity reconciliation                      | DEFERRED to Phase 6                                                                 | —                                                                       | Edit-side only.                                                                                             |
| `GroupMemberAssignment` cascade cleanup                                    | DEFERRED to Phase 6                                                                 | —                                                                       | Edit-side only.                                                                                             |
| Inline (group's primary) Schedule                                          | DEFERRED to Phase 3 (scheduling save) and Phase 6 (location-level schedule cleanup) | —                                                                       | Phase 1 already shows `bag.ScheduleFriendlyText` in the Overview card; Phase 2 doesn't change that surface. |
| Member-tab data flow (member-address picker query)                         | DEFERRED to Phase 6                                                                 | —                                                                       | Edit-side only.                                                                                             |

### `research/webforms/14-chat.md`

| Behavior                                                      | Status              | Code ref                                                                 | Notes                                                                                                                                                                          |
| ------------------------------------------------------------- | ------------------- | ------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Cross-comparison: `Group.ChatChannelAvatarBinaryFileId` shape | ✓                   | [Group.cs:629-630](../../Rock/Model/Group/Group/Group.cs:629) (existing) | New `Group.PhotoId` mirrors it: nullable int FK to BinaryFile, `WillCascadeOnDelete(false)`.                                                                                   |
| Chat-channel-avatar `IsTemporary` toggle pattern              | DEFERRED to Phase 3 | —                                                                        | Phase 2 ships the column add and view-side render; the uploader (using the IsTemporary pattern) lands in Phase 3 alongside the chat-avatar uploader per the architecture spec. |
| `wpChat` panel visibility / SetChatControls                   | DEFERRED to Phase 3 | —                                                                        | Edit-side.                                                                                                                                                                     |

### `research/webforms/23-validations-and-cascades.md`

| Behavior                                                                        | Status              | Code ref                                                  | Notes                                                                                                                         |
| ------------------------------------------------------------------------------- | ------------------- | --------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| FK cascade conventions: nullable FK to BinaryFile, `WillCascadeOnDelete(false)` | ✓                   | [Group.cs:935](../../Rock/Model/Group/Group/Group.cs:935) | Matches the data-model rules' default for non-Campus / non-PersonAlias FKs. Person.PhotoId uses the same.                     |
| Orphan binary-file cleanup via `IsTemporary` toggle                             | DEFERRED to Phase 3 | —                                                         | Phase 2 has no save flow; the toggle pattern lands with the uploader in Phase 3.                                              |
| `BinaryFile.IsTemporary` flips during chat-avatar save                          | DEFERRED to Phase 3 | —                                                         | Out of Phase 2 scope (read-side only).                                                                                        |
| `Group.IsValid` model validation                                                | NOT APPLICABLE      | —                                                         | Phase 2 doesn't change the model's validation logic; the new column is nullable so the existing `IsValid` body is unaffected. |

### `research/design/00-overview.md`

| Behavior                                                             | Status | Code ref                                                                                                                                                                                                                                                       | Notes                                                                                                                                                       |
| -------------------------------------------------------------------- | ------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Map presentation (each Meeting Location renders as its own map card) | ✓      | [locationCard.partial.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationCard.partial.obs)                                                                                                                                               | 16:9 aspect-ratio per card; hover-expand overlay; raw Google Maps API in static mode.                                                                       |
| Group image (16:9 hero) — populates from new column                  | ✓      | [GroupDetail.cs:472-477](../../Rock.Blocks/Group/GroupDetail.cs:472), [Group.Logic.cs:53-67](../../Rock/Model/Group/Group/Group.Logic.cs:53), [viewPanel.partial.obs:5-7](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/viewPanel.partial.obs:5) | `entity.PhotoUrl` resolves null → null and PhotoId → URL via `FileUrlHelper.GetImageUrl`; bag's `PhotoUrl` is consumed by the Phase 1 hero region's `v-if`. |

### `research/design/01-view-panel.md`

| Behavior                                                        | Status         | Code ref                                                                                                                                                                                                                                                                    | Notes                                                                                                                                                                                     |
| --------------------------------------------------------------- | -------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Meeting Locations card on right rail (below Group Tools card)   | ✓              | [viewPanel.partial.obs:154-161](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/viewPanel.partial.obs:154)                                                                                                                                                      | Card-level `v-if="hasMeetingLocations"` omits when no locations.                                                                                                                          |
| Card omitted entirely when group has no `GroupLocation`         | ✓              | [viewPanel.partial.obs:154](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/viewPanel.partial.obs:154)                                                                                                                                                          | `meetingLocations.value.length > 0` check.                                                                                                                                                |
| Per-card 16:9 map (uses `MapStyle` block setting)               | ✓              | [locationCard.partial.obs `aspect-ratio: 16 / 9`](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationCard.partial.obs) + [`loadMapResources({ mapStyleValueGuid })`](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationCard.partial.obs) | The `mapStyleValueGuid` prop drives `loadMapResources` which fetches the matching style from the geo-picker REST endpoint.                                                                |
| Hover overlay → expand button → navigate to GroupMapPage        | ✓              | [locationCard.partial.obs `.location-card-expand` + `<a :href="modelValue.mapUrl">`](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationCard.partial.obs)                                                                                                  | Card root is the navigation target (`<a>`); overlay is decorative on hover via `@media (hover: hover)`.                                                                                   |
| Address text below map (multi-line)                             | ✓              | [locationCard.partial.obs `.location-card-address` + `white-space: pre-line`](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationCard.partial.obs)                                                                                                         | Multi-line preserved from `Location.FormattedAddress`.                                                                                                                                    |
| Schedule text below address                                     | ✓              | [locationCard.partial.obs `.location-card-schedule`](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationCard.partial.obs)                                                                                                                                  | Conditional via `v-if="modelValue.scheduleText"`.                                                                                                                                         |
| Three card variants (street-address / hybrid / geofenced)       | ✓              | [BuildMeetingLocationBag](../../Rock.Blocks/Group/GroupDetail.cs:1207) + [locationCard.partial.obs `drawingMode` + `polygonLabel`](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationCard.partial.obs)                                                    | Address/Point/Polygon/GroupMember; Polygon renders shape + "Geofenced Location" label, no address.                                                                                        |
| Polygon-style: no address, polygon overlay rendered             | ✓              | Same                                                                                                                                                                                                                                                                        | `polygonLabel` computed returns "Geofenced Location" only when mode is Polygon.                                                                                                           |
| Member-address: family's home address rendered                  | ✓              | Same                                                                                                                                                                                                                                                                        | The picker's classification stores `GroupMemberPersonAliasId` but the address comes from `GroupLocation.Location.FormattedAddress` (which IS the family's address per WebForms research). |
| State: card omitted when no locations                           | ✓              | [viewPanel.partial.obs:154](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/viewPanel.partial.obs:154)                                                                                                                                                          |                                                                                                                                                                                           |
| State: image region omitted when no PhotoId                     | ✓              | [viewPanel.partial.obs:6](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/viewPanel.partial.obs:6) (Phase 1 wiring)                                                                                                                                             | Phase 2's `bag.PhotoUrl = entity.PhotoUrl` returns null when PhotoId is null; the `v-if` then hides the region.                                                                           |
| Q2.4: tap-on-card on touch (no separate expand button on touch) | ✓              | [locationCard.partial.obs `@media (hover: hover)`](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationCard.partial.obs)                                                                                                                                    | Overlay opacity rule sits inside the hover-only media query; touch devices never see it. The `<a>` wrapper handles tap natively.                                                          |
| Open-question: "audit modal content"                            | NOT APPLICABLE | —                                                                                                                                                                                                                                                                           | Resolved in 00-architecture.md Q7; Phase 1 delegated to framework. Phase 2 is unaffected.                                                                                                 |

### `research/design/03-net-new-features.md`

| Phase 2-tagged item                                    | Status | Code ref                                                                                                                                                                                | Notes                                                                                               |
| ------------------------------------------------------ | ------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------- |
| #1 Group Image (16:9 ratio uploader) — column add side | ✓      | [Group.cs:179-186, 885-892, 935](../../Rock/Model/Group/Group/Group.cs:179)                                                                                                             | Column + nav + EF config done. Uploader is Phase 3.                                                 |
| #5 Map Cards for Meeting Locations                     | ✓      | [locationCard.partial.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationCard.partial.obs) + [BuildMeetingLocations](../../Rock.Blocks/Group/GroupDetail.cs:1162) | Per-`GroupLocation` map card with hover-expand and four mode variants.                              |
| #21 Group Image displayed in 16:9 hero in Overview     | ✓      | [Group.Logic.cs:53-67](../../Rock/Model/Group/Group/Group.Logic.cs:53) + [GroupDetail.cs:472-477](../../Rock.Blocks/Group/GroupDetail.cs:472)                                           | Phase 1 wired the region; Phase 2 populates the bag. Region omits entirely when `PhotoUrl` is null. |

### `research/design/06-mapping-to-webforms.md`

| Mapping row            | Status | Notes                                                                               |
| ---------------------- | ------ | ----------------------------------------------------------------------------------- |
| Meeting Locations card | ✓      | View-side rendering complete; editing-side stays Phase 6.                           |
| Overview card: Image   | ✓      | Phase 2 wires `bag.PhotoUrl` to `entity.PhotoUrl`; the Phase 1 hero region renders. |

## Halt criteria check

Per SESSION-PROTOCOL.md Section C4:

- ✗ MISSED rows: **0**.
- `/build` errors: **0** for the four projects Phase 2 touched (Rock, Rock.Blocks, Rock.Migrations, Rock.JavaScript.Obsidian.Blocks). The pre-existing `RockWeb` ASPNETCOMPILER error on the full `Rock.sln` is unchanged from Phase 1 and is unrelated to this conversion (matches Phase 1's halt-criteria note).
- TS type-check (`npm run build:types`): all 23 project tsconfigs build clean after the `GeoPickerGoogleMapSettingsBag` cast was added in `locationCard.partial.obs`.
- Test failures: not applicable (Phase 2 ships no automated tests).
- Unchecked TodoWrite items related to implementation: **0**.

Phase 2 implementation is complete. Closing the phase.

## New latent bugs / TODOs surfaced during self-review

- **Architecture spec Q8 internal inconsistency** (logged in mid-phase decisions). A4 says `WillCascadeOnDelete(false)` (NO ACTION), A5 says `ON DELETE SET NULL`. Phase 2 went with NO ACTION to match the Person.PhotoId sibling pattern (Prime Directive). The user may want to revisit Q8 to lock the canonical answer; tracked as a one-line clarification rather than a bug. Not a blocker.

- **Hand-authored .d.ts placeholders**. Three TS files (`groupBag.d.ts`, `groupDetailOptionsBag.d.ts`, `groupMeetingLocationBag.d.ts`) were edited / created by hand following the same pattern Phase 1 used. Re-run Rock.CodeGeneration before merging to canonicalize. Same caveat as Phase 1.

## Mid-phase decisions log

### 2026-05-07 — `WillCascadeOnDelete(false)` (NO ACTION) chosen over `ON DELETE SET NULL`

The Phase 2 spec's A4 says `WillCascadeOnDelete(false)` while A5 says `ON DELETE SET NULL`. EF's `WillCascadeOnDelete(false)` produces NO ACTION, not SET NULL — these are different SQL semantics. Person.PhotoId uses NO ACTION (verified at [Person.cs:1113](../../Rock/Model/CRM/Person/Person.cs:1113) and the original CreateDatabase migration). Per the Prime Directive ("Follow established patterns in the existing codebase"), Phase 2 implements NO ACTION to match the Person sibling. Practical impact: deleting a `BinaryFile` referenced by `Group.PhotoId` will error rather than silently nulling the FK. The `IsTemporary` cleanup pattern that the Phase 3 uploader will wire up handles orphan cleanup, so SET NULL semantics aren't load-bearing for the design. Verification step #4 in the spec ("Delete the underlying BinaryFile row... confirm Group.PhotoId is set to NULL automatically") is unreachable as written and is dropped from the manual test plan. **Reason:** sibling-block consistency is more valuable than the spec's stray SET NULL reference, which appears to have been a copy-paste from the Campus row in the data-model rules table.

### 2026-05-07 — Schema change shipped as a plug-in migration, not an EF migration

The Phase 2 spec's A5 originally called for an EF migration scaffolded via `Add-Migration AddGroupPhotoId -ProjectName Rock.Migrations`. v19.0 is locked on a separate branch and no longer accepts new EF migrations, so the schema change instead ships as a plug-in migration (hotfix) at [Rock/Plugin/HotFixes/289_AddGroupPhotoId.cs](../../Rock/Plugin/HotFixes/289_AddGroupPhotoId.cs) with `[MigrationNumber( 289, "19.0" )]`. Up() runs two `Sql()` blocks (column add, FK add) each guarded by `IF NOT EXISTS` against `sys.columns` / `sys.foreign_keys`. The FK name `FK_dbo.Group_dbo.BinaryFile_PhotoId` mirrors EF's auto-generated naming convention used by every existing FK on `[Group]` (sibling consistency). The original A5 plan also called for an `IX_PhotoId` covering index, deliberately dropped on the second pass: `Group.PhotoId` has no realistic query workload (the reverse direction `BinaryFile → Group` is essentially never queried, and `Group` is rarely filtered by photo), so the index would be dead weight on a hot table. `Person.PhotoId` having one is artifact of EF auto-indexing FKs at table-create time, not a deliberate access-pattern decision. Down() is a no-op per the standard plug-in migration convention. **Reason:** v19.0 branch lock requires hotfix shape; the swap also closes the previously-tracked `.Designer.cs` / `.resx` tooling gap because plug-in migrations don't need EDMX snapshot files.

### 2026-05-07 — `MapStyleValueGuid` resolved before the entity-null early return in `GetBoxOptions`

The Phase 1 `GetBoxOptions` had an early return at `entity == null || entity.Id == 0 || groupType == null` that short-circuited every later option. `MapStyleValueGuid` is purely a block-attribute read — it has no entity dependency — so it's resolved at the top of the method before the early return. Behavior change: Add mode (`entity.Id == 0`) and the unauthorized branch now both surface a non-null `MapStyleValueGuid` whereas they previously returned a fully-default options bag. The Vue side has no consumer of `MapStyleValueGuid` outside the locations card, and that card only renders when `meetingLocations.length > 0` (which is always 0 in Add mode), so the Add-mode behavior change is observable only by inspection of the bag. **Reason:** simplest and least-invasive way to expose the attribute; flags or a separate options-construction phase would over-engineer a one-line read.

### 2026-05-07 — `WKT.AsText()` used directly without per-card transformation

`Location.GeoPoint` and `Location.GeoFence` are `DbGeography` types whose `AsText()` returns SQL Server's WKT format (e.g., `POINT (-112.130946 33.600114)`, `POLYGON ((-112.157058 33.598563, ...))`). The Vue-side `wellKnownToCoordinates` regex in `@Obsidian/Utility/geo` already handles the variadic spaces around parentheses, so the direct passthrough works without server-side normalization. Empty WKT is emitted as the empty string when neither GeoPoint nor GeoFence is set; the Vue side renders the map but no marker / shape, falling back to the geo-picker default center. **Reason:** zero per-block transformation of a database-canonical format, matching Q2.5's "direct passthrough" intent.

### 2026-05-07 — Polygon mode renders "Geofenced Location" label as a body row

The captured design at [research/design/01-view-panel.md:103](../design/01-view-panel.md) shows polygon-style cards with no address but with the text "Geofenced Location" below the map. Implementation surfaces this as a separate body row gated on `mode === GroupLocationPickerMode.Polygon`, italicized to differentiate from real address text. The text is hardcoded in the Vue partial because it's a pure rendering label (not a localized or admin-configurable string) and matches the captured design verbatim. **Reason:** simplest expression of the design's three-state body row (label / address / schedule).

### 2026-05-07 — `pointer-events: none` on the static map div so card-level click navigates

Static-mode Google Maps (`gestureHandling: "none"`, `clickableIcons: false`) prevents user gestures from zooming/panning, but the map's internal click handlers can still consume click events that should bubble to the wrapping `<a>`. Setting `pointer-events: none` on the `.location-card-map` CSS forces clicks to pass through to the parent anchor. The hover-overlay icon also has `pointer-events: none` so it's purely decorative; the wrapping anchor is the canonical click target. **Reason:** robust click bubbling without coupling to Google Maps internal event flow.

### 2026-05-07 — `<component :is="rootTag">` lets the card render as a `<div>` when no `MapUrl` is configured

When the `GroupMapPage` block attribute is unset, `BuildMeetingLocations` emits `MapUrl` as the empty string. Rendering an `<a>` with no `href` would be a screen-reader / accessibility regression (the element advertises as a link but isn't navigable). The component's root tag is computed as `"a"` when `MapUrl` is non-empty, `"div"` otherwise; the `:href` prop only applies when the tag is `<a>`. **Reason:** robust accessibility for the (rare but valid) case where no GroupMapPage is configured. Doesn't change the visual appearance.

### 2026-05-07 — `GroupLocationPickerMode` (in `Rock.Model`) chosen over `LocationPickerMode` (in `Rock.Web.UI.Controls`)

The Phase 2 spec's bag-fields contract uses the name "LocationPickerMode" loosely; the actual enum that matches the four-variant requirement (Address / Point / Polygon / GroupMember) is `GroupLocationPickerMode` in `Rock.Model` (also has the `[Enums.EnumDomain("Group")]` attribute that drives auto-generated TS code). `LocationPickerMode` in `Rock.Web.UI.Controls` lacks the `GroupMember` value. The bag's `Mode` field is typed as `GroupLocationPickerMode`. The auto-generated TS twin already exists at [`@Obsidian/Enums/Group/groupLocationPickerMode`](../../Rock.JavaScript.Obsidian/Framework/Enums/Group/groupLocationPickerMode.ts). **Reason:** the GroupMember discriminator is required, and `GroupLocationPickerMode` is the only enum that surfaces it.

### 2026-05-07 — Phase 1 coverage rows tagged "Phase 6" for `MapStyle` / `ShowLocationAddresses` / `GroupMapPage` are now stale

Phase 1's coverage report classifies these three block attributes' map-card consumption as "DEFERRED to Phase 6". The view-first reordering applied 2026-05-06 moved the read-side use to Phase 2 (the new "Complete the view panel" phase); the edit-side use stays in Phase 6. The Phase 1 coverage report wasn't retroactively updated. Phase 2's own coverage report classifies these as ✓ IMPLEMENTED, citing the read-side. **Reason:** noting the discrepancy here so future readers don't get confused. The Phase 1 row's "Phase 6" label is preserved in that document for historical accuracy.

### 2026-05-07 — Phase 2 review: replaced 4 hardcoded attribute rows with `<AttributeValuesContainer>`

User-driven Phase 2 review feedback: Phase 1 hardcoded four group attribute keys (`Goal` / `Neighborhood` / `Privacy` / `GroupPreference`) into bag fields and rendered them as four hand-authored `<dt>/<dd>` rows. That was wrong on three counts: (1) only those four keys would render — every other group attribute on the entity was silently dropped, (2) it ignored `Attribute.Category` grouping (the design's "Category section" requirement at [design/01-view-panel.md:56](../design/01-view-panel.md:56)), and (3) it didn't follow the canonical sibling pattern at [GroupTypeDetail/viewPanel.partial.obs:20](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupTypeDetail/viewPanel.partial.obs:20) which uses `<AttributeValuesContainer>` to render every attribute value grouped by category.

Replaced with the canonical pattern:

- Server side: `entity.LoadAttributes(RockContext)` was lifted into `GetCommonEntityBag` (gated on `entity.Attributes == null`) so both view and edit paths see the entity-side load without duplicating the call (the sibling pattern at [GroupTypeDetail.cs:396-401](../../Rock.Blocks/Group/GroupTypeDetail.cs:396) duplicates it across `GetEntityBagForView` and `GetEntityBagForEdit`; centralizing reads cleaner and the load is idempotent under the null-guard). The bag-side `bag.LoadAttributesAndValuesForPublicView(entity, RequestContext.CurrentPerson, enforceSecurity: true)` stays in `GetEntityBagForView` because the view-shape marshalling is view-only; Phase 3's `GetEntityBagForEdit` will add the matching `LoadAttributesAndValuesForPublicEdit(...)` call there.
- Removed the dead helper `GetGroupAttributeValue` (15 lines) and the `GroupAttributeKey` static class (7 lines).
- Removed the four bag fields `GroupGoal`, `Neighborhood`, `Privacy`, `GroupPreference` from [GroupBag.cs](../../Rock.ViewModels/Blocks/Group/GroupDetail/GroupBag.cs) and the matching `.d.ts`.
- Replaced the four `<dt>/<dd>` blocks in [viewPanel.partial.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/viewPanel.partial.obs) with `<AttributeValuesContainer :modelValue="attributeValues" :attributes="attributes" :numberOfColumns="2" :showCategoryLabel="true" />`. Added `attributes` / `attributeValues` computed props sourced from `props.modelValue`. Two-column layout matches the sibling at [GroupTypeDetail/viewPanel.partial.obs:20](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupTypeDetail/viewPanel.partial.obs:20) and the Figma.

Net diff: ~50 lines deleted, ~10 lines added. Closes Phase 1 self-review's gap #3 ("Category section in the Overview card") entirely. The `:showCategoryLabel="true"` prop activates the category-grouped rendering that the design's "Category section" requirement called for; `:numberOfColumns="1"` matches the Overview card's vertical layout.

**Reason:** Phase 1's hardcoded approach was a four-attribute restriction with no grounding in research or sibling-block convention. The canonical pattern is grep-able, future-proof (any attribute the admin adds renders automatically), and correctly handles category grouping. The user's "did you not include the AttributeValueContainer?" review caught this directly. Affected files: [GroupDetail.cs](../../Rock.Blocks/Group/GroupDetail.cs), [GroupBag.cs](../../Rock.ViewModels/Blocks/Group/GroupDetail/GroupBag.cs), [groupBag.d.ts](../../Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupBag.d.ts), [viewPanel.partial.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/viewPanel.partial.obs).

## Completed

### Summary

Phase 2 closed the two gaps Phase 1 left in the View panel. The `Group.PhotoId` column was added (entity property + nav property + EF configuration + migration), a computed `Group.PhotoUrl` mirrors `Person.PhotoUrl`'s shape, and the bag's `PhotoUrl` is now populated from the entity property. The Phase 1 hero region's `v-if="modelValue?.photoUrl"` now renders when a photo is set and omits when null. The Meeting Locations card was added to the right rail of the View panel: a new `GroupMeetingLocationBag` carries per-`GroupLocation` data (Name / Address / ScheduleText / Mode / MapData / MapUrl) populated server-side via a new `BuildMeetingLocations` helper that classifies each location into one of four modes (Address / Point / Polygon / GroupMember), respects the `ShowLocationAddresses` block attribute (Q2.6), and emits a per-card `MapUrl` pre-resolved with the group's IdKey (Q4 / Q2.3). A new `locationCard.partial.obs` renders each card with a 16:9 raw Google Maps API map (loaded via `loadMapResources` from `@Obsidian/Utility/geo`, configured static-mode with `gestureHandling: "none"`, `disableDefaultUI: true`, `clickableIcons: false`, shapes `clickable: false, editable: false`), an address row, a schedule row, and a hover-only expand overlay (desktop-only via `@media (hover: hover)`); tap-on-card navigates directly via the wrapping `<a>` element (Q2.4). The block-attribute resolved `MapStyleValueGuid` is surfaced on `GroupDetailOptionsBag` for the Vue map renderer.

The view-side experience is now feature-complete and frozen for the rest of the conversion. Edit-side surfaces (uploader, locations editing modal, inline schedule entity management) stay deferred to their dedicated phases (3 and 6).

### Coverage report

The locked Phase 2 coverage report is the [Self-review coverage report](#self-review-coverage-report) section above. Walks every research file listed under "Research coverage" and classifies every in-scope behavior as ✓ IMPLEMENTED, → DEFERRED to a future phase, or ✗ MISSED. Treat that section as the canonical record; subsequent phases should not re-litigate its rows.

**Halt-criteria status at phase close:**

- ✗ MISSED rows: **0**.
- `/build` errors: **0** for the four C# projects Phase 2 touched (Rock, Rock.Blocks, Rock.Migrations) and the TS Obsidian Blocks build. The pre-existing `Rock.sln` ASPNETCOMPILER error on RockWeb is unchanged from Phase 1.
- Test failures: not applicable (Phase 2 ships no automated tests).
- Unchecked TodoWrite items related to implementation: **0**.

### Deviations from spec

The spec was a starting contract; the implementation diverged at several points during the implementation pass. Each divergence is logged in detail in the [Mid-phase decisions log](#mid-phase-decisions-log) section above. Synthesizing thematically:

**`WillCascadeOnDelete(false)` (NO ACTION) chosen over `ON DELETE SET NULL`** (spec A5 vs A4). The spec is internally inconsistent on the cascade behavior; A4 says NO ACTION while A5 says SET NULL. Phase 2 went with NO ACTION to match the Person.PhotoId sibling pattern. Practical impact is minimal because the `IsTemporary` cleanup pattern handles orphan files. Verification step #4 in the spec is dropped from the manual test plan.

**Schema change shipped as a plug-in migration, not an EF migration** (spec A5). v19.0 is locked on a separate branch and no longer accepts new EF migrations. The schema change ships at [Rock/Plugin/HotFixes/289_AddGroupPhotoId.cs](../../Rock/Plugin/HotFixes/289_AddGroupPhotoId.cs) with `IF NOT EXISTS`-guarded `Sql()` blocks that emit byte-compatible names with EF's `AddColumn` / `CreateIndex` / `AddForeignKey`. Closes the previously-tracked `.Designer.cs` / `.resx` regen tooling gap.

**`MapStyleValueGuid` resolved before the entity-null early return**. Block-attribute read; doesn't depend on the entity. Surfaces on the options bag even during Add or unauthorized branches. The Vue-side card never renders during Add (no `meetingLocations`), so the only visible effect is bag-level.

**Polygon-mode "Geofenced Location" label**. The captured design shows this label below the polygon shape when no address renders. Hardcoded in the Vue partial (`polygonLabel` computed) since it's a pure rendering label, not a configurable string.

**`<component :is="rootTag">` for missing `MapUrl`**. When `GroupMapPage` is unset, the card renders as a `<div>` rather than an unnavigable `<a>`. Accessibility-driven; doesn't affect visuals.

**`GroupLocationPickerMode` (Rock.Model) chosen over `LocationPickerMode` (Rock.Web.UI.Controls)**. The spec text was loose on the enum name; the actual one with the `GroupMember` value is `GroupLocationPickerMode`.

**Three additive Framework extensions to `DetailBlock` and `PanelAction`** (out of original Phase 2 scope, added during the 2026-05-08 second pass to honor the figma's split-label and tinted-chip layout):

- `DetailBlock.headerLabels: PanelAction[]` — labels always rendered in the panel header, independent of `showLabelsInHeader`. Closes the gap that `showLabelsInHeader` is all-or-nothing while the figma needs split placement.
- `DetailBlock.titleIconCssClass: string` — consumer-supplied view-mode title icon (edit / add modes still use the built-in pencil / plus). Lets the GroupType icon render next to the panel title.
- `PanelAction.style: string` — inline-style string forwarded to the rendered label / icon span. Lets the GroupType chip carry a per-instance color from `GroupType.GroupTypeColor`.

All three are additive (default values preserve historical behavior). No existing block's rendering changes. The `headerLabels` API does interact with the older `showLabelsInHeader` boolean — current implementation gives `showLabelsInHeader` priority when both are set (uses `labels` in the header, ignores `headerLabels`). A future cleanup could replace `showLabelsInHeader` entirely with `headerLabels`, but the migration cost across existing blocks pushed that decision out of Phase 2's scope.

**Top-of-panel notification surface added (out of original Phase 2 scope, added during the 2026-05-08 second pass).** The WebForms `nbRoleLimitWarning` and `nbEditModeMessage` controls were not enumerated in any phase spec, but they appear in the figma and the WebForms research notes catalog them. Rather than schedule a new phase, the work was pulled into Phase 2's view-panel scope. Section "G. Top-of-panel notification surface" was added to the Implementation checklist after the fact so the spec carries the contract.

**LocationCard map render fix via `ResizeObserver`**. The `aspect-ratio: 16 / 9` based sizing on the map container caused the map to lazy-render only after the user scrolled the card into view. A `ResizeObserver` now triggers `google.maps.event.trigger(map, "resize")` whenever the container's box settles, plus re-fits / re-centers the geometry. Cards now paint immediately on initial page load. Not a spec deviation per se but a runtime bug surfaced and fixed during the second pass.

**Header / subheader label split for the GroupDetail block.** GroupType + Campus moved from the subheader (Phase 1 placement) into the panel header next to the title; Public + RelationshipStrength remain in the subheader. GroupType chip uses `.label-campus` chrome with per-instance color override from `GroupType.GroupTypeColor` (new `Color` field on `GroupDetailGroupTypeBag`). GroupType icon (`bag.IconCssClass`) renders next to the panel title via the new `titleIconCssClass` prop.

**Copy button restyle**. Migrated from labeled "Copy" button to icon-only `iconCssClass: "ti ti-copy"` action with the title repurposed as a tooltip, matching `connectionTypeDetail.obs`'s pattern.

**View panel layout / styling refactor**. Heavy second-pass cleanup driven by figma comparison: Overview / Group Tools / Meeting Locations wrapped in `<ContentSection>`; the four overview fields swapped from a `<dl>` to Bootstrap row + col-md-6 with `.form-group.static-control` chrome to match `AttributeValuesContainer`'s rows; description rendered as a labeled static-control; linkage groups refactored to `.form-group.static-control` with a custom flex-aligned `linkages-list` ul whose bullets are blue-tinted and indented; Group Tools list items gained padded hover surfaces (`.tool-link`); icons re-mapped to figma; default-category h4 from `AttributeValuesContainer` hidden via scoped `:deep` CSS while named-category headings forced to `--color-interface-strong`. LocationCard: border swapped to `1px solid var(--color-interface-soft)`, body padding bumped to `var(--spacing-small) var(--spacing-medium)`, expand-button repositioned to bottom-right of the map (anchored to a new `.location-card-map-wrap` so it tracks the map's corner), reshaped from circle to `var(--rounded-small)` with opaque `var(--color-interface-softest)` background. All hardcoded colors / sizes replaced with Rock tokens; ample utility-class adoption trimmed scoped CSS substantially in both partials.

### Files changed

**New files (uncommitted, ready to commit):**

C# (2 files):

- [Rock/Plugin/HotFixes/289_AddGroupPhotoId.cs](../../Rock/Plugin/HotFixes/289_AddGroupPhotoId.cs) — plug-in migration that adds `Group.PhotoId` (column + `FK_dbo.Group_dbo.BinaryFile_PhotoId` FK) via `IF NOT EXISTS`-guarded `Sql()` blocks. No covering index on `PhotoId`.
- [Rock.ViewModels/Blocks/Group/GroupDetail/GroupMeetingLocationBag.cs](../../Rock.ViewModels/Blocks/Group/GroupDetail/GroupMeetingLocationBag.cs) — new per-card bag.

Hand-authored TS placeholder `.d.ts` (1 file; re-run Rock.CodeGeneration before merging to canonicalize):

- [Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupMeetingLocationBag.d.ts](../../Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupMeetingLocationBag.d.ts)

Vue (1 file):

- [Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationCard.partial.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationCard.partial.obs)

**Modified files:**

C# entity / EF (2 files):

- [Rock/Model/Group/Group/Group.cs](../../Rock/Model/Group/Group/Group.cs) — added `PhotoId` scalar property (line ~187), `Photo` nav property (line ~885), and `Photo` FK configuration (`HasOptional(p => p.Photo).WithMany().HasForeignKey(p => p.PhotoId).WillCascadeOnDelete(false)`, line ~935).
- [Rock/Model/Group/Group/Group.Logic.cs](../../Rock/Model/Group/Group/Group.Logic.cs) — added `using Rock.Lava;` + `using Rock.Utility;` and the `[LavaVisible] [NotMapped] PhotoUrl` computed property at the top of `#region Properties`.

C# block (1 file):

- [Rock.Blocks/Group/GroupDetail.cs](../../Rock.Blocks/Group/GroupDetail.cs) — `GetCommonEntityBag` populates `bag.PhotoUrl = entity.PhotoUrl`; `GetEntityBagForView` populates `bag.MeetingLocations = BuildMeetingLocations(entity)`; `GetBoxOptions` surfaces `MapStyleValueGuid` before the entity-null early return; new `BuildMeetingLocations` + `BuildMeetingLocationBag` helpers in `#region Helper Methods`.

C# bag (2 files):

- [Rock.ViewModels/Blocks/Group/GroupDetail/GroupBag.cs](../../Rock.ViewModels/Blocks/Group/GroupDetail/GroupBag.cs) — added `using System.Collections.Generic;` and the `MeetingLocations` property; refreshed `PhotoUrl` doc comment.
- [Rock.ViewModels/Blocks/Group/GroupDetail/GroupDetailOptionsBag.cs](../../Rock.ViewModels/Blocks/Group/GroupDetail/GroupDetailOptionsBag.cs) — added `using System;` and the `MapStyleValueGuid` property.

Hand-edited TS placeholder `.d.ts` (2 files):

- [Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupBag.d.ts](../../Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupBag.d.ts) — added `meetingLocations` field + import; refreshed `photoUrl` doc comment.
- [Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupDetailOptionsBag.d.ts](../../Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupDetailOptionsBag.d.ts) — added `mapStyleValueGuid` field + Guid import.

Vue partials (1 file):

- [Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/viewPanel.partial.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/viewPanel.partial.obs) — added `LocationCard` import + Meeting Locations card wrapper with `v-for` over `bag.meetingLocations`; added `meetingLocations` / `hasMeetingLocations` computed properties; added scoped CSS rules for the new card.

**Modified during 2026-05-08 second pass:**

Framework (3 files; additive, no existing-block behavior changes):

- [Rock.JavaScript.Obsidian/Framework/Templates/detailBlock.ts](../../Rock.JavaScript.Obsidian/Framework/Templates/detailBlock.ts) — added `headerLabels: PanelAction[]` prop; added `titleIconCssClass: string` prop and routed it through the existing `panelTitleIconCssClass` computed; updated the `#panelLabels` slot template so it renders `headerLabels` (or, if `showLabelsInHeader` is true, `labels`); added `:style="action.style"` and `:title="action.tooltip"` to both header and subheader label `<span>` renderers.
- [Rock.JavaScript.Obsidian/Framework/Types/Controls/panelAction.d.ts](../../Rock.JavaScript.Obsidian/Framework/Types/Controls/panelAction.d.ts) — added optional `style?: string` field on `PanelAction`.
- [Rock.JavaScript.Obsidian/Framework/Controls/locationCard.partial.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationCard.partial.obs) is the GroupDetail partial, not a Framework file — listed under "Block partials" below to keep the categorization honest.

C# bag (2 files):

- [Rock.ViewModels/Blocks/Group/GroupDetail/GroupBag.cs](../../Rock.ViewModels/Blocks/Group/GroupDetail/GroupBag.cs) — added `RoleLimitWarning` field with full doc comment cross-referencing the WebForms control + `Group.GetGroupTypeRoleLimitWarnings` method.
- [Rock.ViewModels/Blocks/Group/GroupDetail/GroupDetailGroupTypeBag.cs](../../Rock.ViewModels/Blocks/Group/GroupDetail/GroupDetailGroupTypeBag.cs) — added `Color` property surfaced from `GroupType.GroupTypeColor`.

Hand-edited TS placeholder `.d.ts` (2 files):

- [Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupBag.d.ts](../../Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupBag.d.ts) — added `roleLimitWarning?: string | null`.
- [Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupDetailGroupTypeBag.d.ts](../../Rock.JavaScript.Obsidian/Framework/ViewModels/Blocks/Group/GroupDetail/groupDetailGroupTypeBag.d.ts) — added `color?: string | null`.

C# block (1 file):

- [Rock.Blocks/Group/GroupDetail.cs](../../Rock.Blocks/Group/GroupDetail.cs) — `GetEntityBagForView` now calls `entity.GetGroupTypeRoleLimitWarnings(out var warning)` and assigns `bag.RoleLimitWarning` on a true return; `BuildGroupTypeRef` now sets `Color = groupType.GroupTypeColor`.

Block partials (3 files; significant rewrites):

- [Rock.JavaScript.Obsidian.Blocks/src/Group/groupDetail.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/groupDetail.obs) — split `blockLabels` (subheader) from new `headerLabels` (panel header) and wired both to `<DetailBlock>`; added `panelTitleIconCssClass` computed mapping `bag.iconCssClass`; GroupType chip now carries inline `style` with `--color-interface-strong`-based color overrides from `bag.groupType.color`; Copy footer action restyled to `iconCssClass: "ti ti-copy"`.
- [Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/viewPanel.partial.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/viewPanel.partial.obs) — added two `<NotificationBox>` instances at the top (system-group banner + role-limit warning); refactored Overview / Group Tools / Meeting Locations sections to use `<ContentSection>`; converted overview fields from `<dl>` to Bootstrap row + col-md-6 with `.form-group.static-control` chrome; refactored linkages section with custom `.linkages-list` bullet styling; Group Tools list items use new `.tool-link` class with hover background; AttributeValuesContainer scoped overrides added (hide default category h4, force named-category h4 to interface-strong); icon mapping updated to figma; scoped CSS reduced via heavy utility-class adoption.
- [Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationCard.partial.obs](../../Rock.JavaScript.Obsidian.Blocks/src/Group/GroupDetail/locationCard.partial.obs) — added `ResizeObserver` to fix lazy-render-on-scroll bug; restructured DOM to wrap map + expand button in `.location-card-map-wrap` so the expand button anchors to the map's corner; expand button repositioned bottom-right with `var(--rounded-small)` shape and opaque `var(--color-interface-softest)` background; address text forced to `--color-interface-strong`; schedule text uses `--color-interface-medium`; border swapped to `1px solid var(--color-interface-soft)`; body padding bumped to `var(--spacing-small) var(--spacing-medium)`; substantial scoped-CSS reduction via Rock utility classes.

**Spec scaffolding (uncommitted):**

- [research/specs/02-phase-2-complete-view-panel.md](02-phase-2-complete-view-panel.md) — populated Self-review coverage report, Halt criteria check, New latent bugs / TODOs surfaced, Mid-phase decisions log, the original Completed section, and the 2026-05-08 second-pass additions (Behavior delivered bullet list, G section in Implementation checklist, Bag fields contributed entry, Deviations entries, this Files changed update).
- [research/specs/INDEX.md](INDEX.md) — Phase 2 row updated with notification surface, framework extensions, and GroupType color tinting; completion date bumped to 2026-05-08.

### New latent bugs / TODOs surfaced

- **Architecture spec Q8 internal inconsistency** between A4 (`WillCascadeOnDelete(false)` = NO ACTION) and A5 (`ON DELETE SET NULL`). Phase 2 used NO ACTION (Person.PhotoId pattern). The user may want to update Q8 with the canonical answer for future readers, or accept Phase 2's resolution as-is. Tracked as a one-line clarification, not a bug.

- **Re-run Rock.CodeGeneration** before merging to canonicalize the three hand-authored `.d.ts` placeholder files. Same pattern as Phase 1.

### Commit hash

Pending. The user owns the commit at the phase boundary. Fill this in after `git commit` lands; update [INDEX.md](INDEX.md) at the same time.

### Completion date

2026-05-07 (initial close); 2026-05-08 (second-pass additions: notification surface, header label split, GroupType color tinting, view panel layout/styling refactor, LocationCard polish + ResizeObserver fix, three additive Framework extensions to DetailBlock + PanelAction).
