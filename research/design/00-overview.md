# Design Overview

Source: Figma file `N60VRdhtRtjO9EA9nba9fB` ("Obsidian Block Refreshes - Fall 2025 - Spring 2026"), two top-level frames:

- **Edit panel**: node `4670-25350` (frame name "Add/Edit Mode"), 6203 × 8415 canvas containing the main edit form, four modal mockups, alternate-state variants, designer-notes column, and help callouts.
- **View panel**: node `4670-25349` (frame name "View Mode"), 4129 × 2839 canvas containing the main view layout, an alternate compact state, the Copy Group modal, and a designer-notes column.

Visual references for every frame and modal live in [screenshots/](screenshots/).

## High-level shape

The new GroupDetail block is a structural rewrite of the WebForms parity baseline ([../webforms/03-markup-structure.md](../webforms/03-markup-structure.md)) using the Obsidian "Sections & Stacks" pattern that has rolled out to other refreshed blocks.

**Edit panel** is a single scrollable surface broken into 10 collapsible sections, each containing one or more horizontally-laid-out "stacks" (left-side description column, right-side controls). Conditional sub-content is rendered inside "conditional wells" (a left-bordered subdued block) that appears/disappears based on neighboring control state.

**View panel** is a two-column layout: a 7-column "Overview" card on the left (image, description, scalar fields, group attributes, linkages) and a 5-column right rail with stacked "Group Tools" and "Meeting Locations" cards. The footer holds Edit / Cancel actions on the left and a Copy / Security pair on the right.

Both panels follow a common chrome: panel header (icon + name + group-type label + campus label + follow star + audit menu), subheader (relationship-strength label + tags + add-tag button), body, and footer.

## Source-of-truth status

Per Phase 0 resolutions in [../specs/ROADMAP.md](../specs/ROADMAP.md), the Figma is the **source of truth** for:

- Visual layout of both panels.
- Every label and helper-text string (designer-supplied).
- Inclusion/exclusion of features beyond WebForms parity.
- The decision to drop server-rendered Lava in the View panel (confirmed).

Where the design conflicts with WebForms behavior, the design wins unless flagged here as an "open question" awaiting product clarification.

## Key shifts from WebForms parity

| Area | Shift |
|---|---|
| View panel rendering | Pure Vue with structured fields. Server-rendered Lava (`GroupViewLavaTemplate`) is dropped. |
| Section structure | 11 collapsible PanelWidgets → 10 named sections grouped semantically; Meeting Details and Scheduling merged into one. |
| Audit access | Audit drawer removed; audit details now opened via a button + modal. |
| Linkages | Registrations / Event Item Occurrences / Content Items now have a dedicated section in the Overview card with a top border, only shown if linkages exist. |
| Map presentation | Each Meeting Location renders as its own map card; hover reveals an expand button that links to the interactive map. |
| Tag placement | Moved from below the panel header to the subheader, beside the relationship-strength label. Only shown if `EnableGroupTags` block setting is true. |
| Quick links | Hyperlink toolbar in WebForms footer becomes a "Group Tools" section in the right rail. Copy and Security buttons remain in the panel footer (right). |
| Panel header chrome | Added persistent `Group Type` label and `Campus` label in the header (as small chips on the right side, beside the follow star). |
| Group image | New first-class field. 16:9 ratio, displayed at the top of the Overview card. If not set, no image renders. |
| Inactive flow | Active checkbox unchecked → conditional well slides in containing Inactive Reason (required) + Inactive Note + "Also Inactivate Child Groups" checkbox. |
| Add-new flow | When `groupId == 0`, the panel header shows "Add Group" with no labels or quick-link icons. Group Type becomes a required dropdown. |
| Block settings | 21 attributes regrouped into two named sections: "General Settings" and "Page Routing". Several attributes renamed for clarity. |
| Strength labels | Relationship-strength radio options renamed: None / Basic / Strong / Intense → None / Casual / Close / Deep (underlying integer values likely unchanged). |
| Schedule structure | "Meeting & Scheduling Details" merges WebForms' Meeting Details + Scheduling panels with internal stacks. |
| Group Type dropdown | Read-only on existing groups, required dropdown on new. (Same as WebForms but more deliberate styling.) |

## Designer-stated changes (verbatim from the UX/UI Notes columns)

### Edit mode

**New**:
- Trailblazer Settings (unclear; flagged as open question — see [05-design-system-deltas.md](05-design-system-deltas.md))
- Sections & stacks patterns
- Added Campus label to header
- Image uploader component & data

**Changes**:
- Purple = changed labels (designer-flagged in the Figma)
- Merged Meeting Details & Scheduling sections into 1 section with multiple stacks
- Moved Group Attributes above Member Attributes for better hierarchy
- Added conditional rendering to certain fields as noted by conditional wells
- Removed Capacities label from the relationship matrix
- Updated Group Attributes / attribute value container for this section
- Updated Block Settings labels & helper text, and overall Block Settings layout

**Removed**:
- Audit drawer
- Has Requirements icon in subheader (and subheader)

**Other**:
- Did not include section nav since page includes the group tree view block on the left

**Variant notes**:
1. First-time / Adding group: Panel Header shows just "Add Group" (no badges/buttons/chrome). Group Type is a required dropdown (not the static field that appears during edit). No Group Type or Campus labels in header.
2. When Active is deselected: a new conditional well appears for reason / note / child groups deactivation.

### View mode

**New**:
- Sections
- Group Image (16:9 ratio); if not set, show nothing
- Additional Actions (Audit Details button + modal)
- Map Cards for Meeting Locations
- New label in subheader for when Groups are set as "Public"

**Changes**:
- Two columns in View Panel: Column 1 (7 cols) Overview; Column 2 (5 cols) Group Tools + Meeting Locations
- Show Meeting Locations section ONLY IF at least one Meeting Location is configured
- Moved Relationship Strength label to panel subheader
- Moved Tags control to beside the relationship label; only shown if configured in block settings
- Linked Administrator and Parent Group (clickable to person/group profiles)
- Migrated Meeting Locations / Maps into their own section
  - Meeting Location has its own map card
  - On hover, expand button appears
  - On click, user navigates to the interactive map view of the meeting location
- Moved group buttons out of the panel footer into their own Group Tools section. Exceptions: security and copy/duplicate buttons remain in the footer.
- Linkages (registrations / event item occurrences / content items) now have their own area with a top border, shown only if linkages exist.

**Removed**:
- Has Requirements icon
- View Mode Lava Template

**Other**:
- Keep current alerts / validation messages

## File map

| File | Purpose |
|---|---|
| [00-overview.md](00-overview.md) | This file. Cross-cutting summary. |
| [01-view-panel.md](01-view-panel.md) | View panel walkthrough, frame-by-frame. |
| [02-edit-panel.md](02-edit-panel.md) | Edit panel walkthrough, section-by-section. |
| [03-net-new-features.md](03-net-new-features.md) | Features in the design that are NOT in WebForms parity. |
| [04-component-inventory.md](04-component-inventory.md) | Obsidian component inventory implied by the design. |
| [05-design-system-deltas.md](05-design-system-deltas.md) | Patterns / components in the design that don't map cleanly to existing Obsidian. |
| [06-mapping-to-webforms.md](06-mapping-to-webforms.md) | Explicit mapping of design regions to corresponding WebForms research. |
| [screenshots/](screenshots/) | All Figma frame screenshots referenced from these docs. |

## Open questions / flag for spec phase

1. **"Trailblazer Settings"** — listed under "New" in the edit-mode designer notes. No corresponding visual element observed in the captured screenshots. Could be a feature flag, a hidden region, or internal Triumph terminology. Need designer clarification before spec writing.
2. **Relationship strength label rename** — None / Casual / Close / Deep replaces None / Basic / Strong / Intense. The underlying enum integer values (0, 5, 10, 20) should stay the same to preserve data, but every UI surface that renders these labels (peer-network calculation results, exports, history) needs to render the new strings consistently. Confirm with product.
3. **"Also Inactivate Child Groups"** — relabeled from "Inactivate Child Groups". Behavior unchanged. Trivial.
4. **Tag-only-when-configured** — design says tags only show in the subheader if `EnableGroupTags` block setting is true. WebForms ALSO checks `GroupType.EnableGroupTag`. Confirm both gates apply.
5. **Map card hover behavior** — the design specifies hover expand and click-to-interactive-map. Need to confirm the interactive-map URL (presumably the existing `GroupMapPage` block setting).
6. **Linkages source** — Registrations / Event Item Occurrences / Content Items today come from Lava merge fields driven by `RegistrationInstancePage` / `EventItemOccurrencePage` / `ContentItemPage` block settings rendering server-side. The design implies these are now structured bag fields rendered by Vue. Confirm bag schema for each.
7. **Audit modal** — replaces the audit drawer. Visual and content unspecified in the captured frames; need a separate Figma node or clarification.
8. **Coordinator Notifications** — design shows the checkbox list as Decline / Accept / Self-Schedule with no "None" option. WebForms has None as an explicit checkbox (mutually-exclusive with the others). Likely the design intends "no checkboxes selected" to mean None, simplifying the mutual-exclusion logic. Confirm.
9. **Sync Frequency control** — segmented `Mins / Hours / Days` plus a slider. WebForms uses an IntervalPicker. The new control may need a custom component if no Obsidian equivalent exists. See [05-design-system-deltas.md](05-design-system-deltas.md).
10. **Group Image source** — new image uploader on the Group itself. Underlying field: existing `Group.PhotoId` if it exists, or a new column. Verify the data model.
