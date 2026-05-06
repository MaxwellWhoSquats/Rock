# View Panel Walkthrough

Figma node: `4670-25349` ("View Mode" canvas).
Main frame: `4859-15223` (View, 948 × 1705).
Alternate compact state: `4854-11658` (View, 948 × 499).
Copy modal: `5017-62518`.

Visual references: [screenshots/view-panel-main.png](screenshots/view-panel-main.png), [view-panel-alt-state.png](screenshots/view-panel-alt-state.png), [view-modal-copy.png](screenshots/view-modal-copy.png), [view-overview-section.png](screenshots/view-overview-section.png), [view-tools-section.png](screenshots/view-tools-section.png), [view-locations-section.png](screenshots/view-locations-section.png).

## Chrome (top to bottom)

### Panel header

- **Icon** (left): the group type's `IconCssClass`. Renders inside the panel title slot.
- **Name** (text): the group's `Name`. Title-style, alongside the icon.
- **Group Type label** (right): a chip-style label with the group type's name. Linked to the GroupType detail page if the user has ADMINISTRATE on that group type.
- **Campus label** (right): a chip-style label with the group's campus name. Hidden if `group.Campus == null`.
- **Follow star** (right): the existing `FollowingsHelper`-driven control, repositioned as a star icon.
- **Audit menu** (right, ⋮ kebab): hosts "Audit Details" entry that opens the audit modal. **Replaces** the WebForms audit drawer (`PanelDrawer`). See open questions.

The panel labels for **Inactive**, **Archived**, **Private**, **Elevated Security**, **Peer Network**, and **Chat** that exist in WebForms are NOT shown directly in the captured header. The "Public" label is mentioned in the designer notes as a new subheader item for public groups, suggesting the labels migrate either to the subheader OR are surfaced contextually elsewhere. Treat label placement as needing designer clarification.

### Subheader

- **Relationship Strength label** (left, with icon): displays the effective relationship strength using the renamed terminology (Casual / Close / Deep). Hidden when group type doesn't have peer network enabled.
- **Tags** (beside relationship label): the `TagList` control. Only rendered if `EnableGroupTags` block setting is true (per designer notes). "+" button appended to add a tag.
- **(Designer note)** "New label in subheader for when Groups are set as Public". Indicates a "Public" pill chip is shown in the subheader for groups with `IsPublic = true`.

### Footer

- **Edit** button (left, primary).
- **Cancel** button (left, secondary).
- **Copy / Duplicate** button (right, square icon button) — only visible if `ShowCopyButton` block setting and EDIT auth.
- **Security** button (right, square lock icon) — only visible with ADMINISTRATE auth.

## Body — two-column layout

The body uses a 12-column grid: left column 7/12, right column 5/12.

### Left column: Overview card

The Overview card holds everything that was previously rendered by the `GroupViewLavaTemplate`, but as structured fields.

| Region | Content | Source / behavior |
|---|---|---|
| Image | 16:9 ratio image at the top of the card. | New field on Group. If not set, the image region is omitted entirely (not just blank). |
| Description | Plain text below the image. | `group.Description`. |
| Group Administrator | Linked person name. | `group.GroupAdministratorPersonAlias.Person`. Linked to the person profile. Hidden when not set or when `GroupType.ShowAdministrator == false`. |
| Parent Group | Linked group name. | `group.ParentGroup`. Linked to the parent group's detail page. Hidden when null. |
| Schedule | Friendly schedule text. | `group.Schedule.FriendlyScheduleText` for inline schedules. |
| Group Capacity | Numeric value. | `group.GroupCapacity`. Hidden when null. |
| Goal of Group | Defined-value text. | A group attribute value. Rendered as a label/value pair. |
| Neighborhood | Defined-value text. | A group attribute value. |
| Private or Public Space | Defined-value text. | A group attribute value. |
| Group Preference | Text. | A group attribute value. |
| **Category section** ("Category Name") | Repeats the same Goal / Neighborhood / Privacy / Preference fields for any nested attribute group. | Group attribute editor, post-Lava. |
| **Linkages section** | List of registrations, event item occurrences, content items. | Each linkage has a top border separating it. Hidden entirely when no linkages exist. See "Linkages" below. |

The captured screenshot shows a real example with Description, Group Administrator (Alisha Marble, linked), Parent Group (Community Groups, linked), Schedule (Monday at 8:00pm), Group Capacity (25), four group attributes, an embedded "Category Name" sub-section repeating four attributes, and a Linkages section with three registration / event-item-occurrence / content-item rows.

### Linkages section

A bordered region within the Overview card, labeled by category:

- **Registrations** — list of `RegistrationInstance` items with name + linked URL via the `RegistrationInstancePage` block setting.
- **Event Item Occurrences** — list of `EventItemOccurrence` items with name + linked URL via the `EventItemOccurrencePage` block setting.
- **Content Items** — list of `ContentItem` items with name + linked URL via the `ContentItemPage` block setting.

In WebForms these were Lava-rendered. In the design they are structured. Per the designer notes, this section is **only shown if at least one linkage exists** in any of the three categories.

### Right column: stacked cards

#### Group Tools card

Replaces the WebForms hyperlink toolbar in the panel footer. The captured screenshot groups items under sub-headers:

| Sub-header | Items | Source |
|---|---|---|
| Participation | Attendance, Scheduler, RSVP, Placement | `AttendancePage`, `GroupSchedulerPage`, `GroupRSVPPage`, `GroupPlacementPage` block settings; visibility per the corresponding GroupType flags (e.g., Scheduler hidden if `IsSchedulingEnabled == false`). |
| Views | Interactive Map, History | `GroupMapPage`, `GroupHistoryPage` block settings; visibility per group type (Map hidden when no URL configured; History hidden when `EnableGroupHistory == false`). |

Each item is a row with an icon and a label, behaves like a link.

The Fundraising Progress link mentioned in WebForms isn't visible in the captured layout but is documented in the edit-side designer notes as a Page Routing block setting. Likely lives under "Participation" or appears conditionally for fundraising group types.

The captured **alternate compact state** shows the Group Tools card collapsed to a single "Interactive Map" entry under "Views" because the group type has only the map page configured. This confirms each item is conditionally visible.

#### Meeting Locations card

Per designer notes: **only rendered if at least one `GroupLocation` is configured.**

For each `GroupLocation`:

- A 16:9 map card (uses the `MapStyle` block setting).
- On hover, an expand button appears overlaid on the map.
- Click expand → navigate to `GroupMapPage?GroupId=...` (interactive map view).
- Address text below the map (multi-line for full address).
- Schedule text below the address (e.g., "Saturday 4:00pm") if a schedule is associated with this location.

Captured screenshot shows three cards:
1. Standard street-address location: "1701 N Fillmore St, Amarillo, TX 79146" + "Saturday 4:00pm" + map.
2. Hybrid virtual-or-in-person location: full address + map.
3. Geofenced location (polygon): map with the polygon, label "Geofenced Location" (no address since it's a polygon).

This design directly handles the WebForms `GroupLocationPickerMode` flags (Address / Point / Polygon / GroupMember) — the rendering varies based on what kind of `Location` the GroupLocation references.

## States

| State | Effect |
|---|---|
| Group has no GroupLocations | Meeting Locations card omitted. |
| Group has no GroupAdministrator | Row hidden in Overview. |
| Group has no Parent Group | Row hidden in Overview. |
| Group has no GroupCapacity | Row hidden in Overview. |
| Group has no Image | Image region omitted (no placeholder per designer notes). |
| GroupType not peer-network-enabled | Relationship strength label hidden in subheader. |
| `EnableGroupTags` block setting false OR `GroupType.EnableGroupTag` false | Tags hidden in subheader. |
| User lacks EDIT auth | Edit / Cancel / Copy buttons hidden in footer. |
| User lacks ADMINISTRATE auth | Security button hidden in footer. |
| `ShowCopyButton` block setting false | Copy button hidden in footer. |
| GroupType doesn't enable scheduling/RSVP/etc. | Corresponding rows in Group Tools hidden. |
| No linkages exist | Linkages section omitted. |
| `IsArchived` true | Likely shows Archived label in subheader/header (placement TBD). |
| `IsActive` false | Likely shows Inactive label in subheader/header (placement TBD). |
| `IsSystem` true | Edit button still shown (depending on auth) but Delete/Archive likely suppressed in the menu. |
| Compact group (no group attributes, no locations, single tools entry) | Two columns still shown but contents collapse to minimal data. |

## Copy modal

Trigger: Copy button in panel footer.
Frame: `5017-62518`. Screenshot: [screenshots/view-modal-copy.png](screenshots/view-modal-copy.png).

| Element | Content |
|---|---|
| Title | "Copy Group" |
| Body | Alert / notification box: "The selected group will be copied. Group members will not be included. The following will be copied for each group:" followed by a list (Group member attribute configuration, Group attribute configuration and values, Schedules, Authorizations (...), Locations (excluding group member addresses), Group requirements, Group syncs). |
| Control | "Include Child Groups" inline checkbox. **Default: UNCHECKED.** WebForms default was CHECKED. |
| Footer | Save (primary) + Cancel. |

The default-flip on "Include Child Groups" is a behavior change from WebForms parity. Confirm intent with product.

## Audit modal

Triggered from the kebab menu in the panel header. Visual content NOT captured in the explored frames — likely a separate Figma node or unbuilt at design time. Open question.

## Group Member List Block placeholder

Below the main view panel (in the captured top-level overview), there is a placeholder rectangle labeled "Group Member List Block". This is a SEPARATE block on the page; the GroupDetail block does not render it. It is included in the design overview to show how the page composes (GroupTreeView on the left, GroupDetail on the right, GroupMemberList below). No conversion implication for GroupDetail itself.

## Open questions / flag for spec phase

1. **Header label placement** — The captured frame shows Group Type and Campus labels in the header (right side, beside the follow star) but the Inactive / Archived / Private / Elevated Security / Peer Network / Chat labels are NOT visible. The designer notes mention a "Public" subheader label. Need clarification: are the other state labels in the subheader, removed, or simply not rendered in the captured non-archived non-elevated example?
2. **Audit modal content** — Replaces the WebForms PanelDrawer. Need a Figma frame or content spec for the modal body (audit trail format, fields shown).
3. **Group image data source** — Resolved by 00-architecture.md Q8: the Group entity has no photo column today; a new `Group.PhotoId` (nullable int → BinaryFile) is added in Phase 2 mirroring `Person.PhotoId` ([Person.cs:242](Rock/Model/CRM/Person/Person.cs:242)). Image uploader behavior follows the chat-channel-avatar `IsTemporary` toggle pattern.
4. **Linkages bag schema** — Need a defined shape: `{ registrations: { name, url }[], eventItemOccurrences: { name, url }[], contentItems: { name, url }[] }`. Confirm the C# block populates these from the same EF queries the WebForms Lava had.
5. **Group Tools sub-header grouping** — Captured screenshot shows "Participation" and "Views" sub-headers. Are these designer-fixed labels or do they vary by group type? Need confirmation.
6. **Map card hover-and-click contract** — Hover reveals expand; click navigates. Mobile behavior (no hover) is unspecified — clarify whether tap goes directly to interactive map.
7. **Geofenced map card** — Polygon-style location renders with no address. Confirm rendering rules for `LocationPickerMode.Polygon` and `Point`.
8. **Copy modal default** — "Include Child Groups" flipped from CHECKED (WebForms) to UNCHECKED (design). Intentional? Most groups don't have children, so unchecked is the safer default; confirm.
9. **Edit/Cancel buttons in view mode footer** — A "Cancel" button in view mode is unusual (there's nothing to cancel). It may be vestigial from the design system's panel template. Verify whether this should be hidden in view mode.
10. **Compact state layout** — In the alt state with minimal data, the right column is mostly empty. Should the left column expand to fill, or does the empty right rail stay? Captured screenshot suggests the latter; confirm.
