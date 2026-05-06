# Design System Deltas

This file flags design elements that don't map cleanly to existing Obsidian components and recommends an approach for each.

## Patterns that may need new shared components

### 1. Sections & Stacks layout

**What it is**: a `Panel Body` containing multiple collapsible `Section`s. Each `Section` has a `Section Header` (icon + title + collapse chevron) and a `Section Body` containing one or more `Section Stack`s. A `Section Stack` is a horizontal layout: ~25% left column with a heading + description text, ~75% right column with controls (typically a vertical stack of form inputs).

**Where it's used**: every collapsible section in the edit panel uses this. It's also visible in the View panel where each card is a `Section`.

**Recommendation**:
- Inspect already-converted refreshed blocks (e.g., `groupTypeDetail.obs`) to confirm whether shared components for Section / Section Stack already exist. The Figma metadata XML uses literal names "Section", "Section Header", "Section Body", "Section Stack" — strong signal that the design system already has these primitives.
- If they exist: import and use.
- If they don't: build new during Phase 1, since every later phase depends on them. Don't inline-build per section.

### 2. Conditional Well

**What it is**: a left-bordered subdued block that wraps content visible only when a parent toggle is on. Visual style: 4px or 8px left border in a primary color, slightly subdued background tint.

**Where it's used**:
- Inactive flow (Active checkbox unchecked → reason / note / cascade well).
- Security Level (Enable as Security Role → level radio well).
- Show Advanced Relationship Settings (switch on → growth + matrix well).
- Group Schedule sub-types (Weekly → Date & Time well; Custom → builder well; Named → picker well).
- Coordinator Notifications (Enable Schedule Coordinator Notifications → checkbox list well).
- Member Workflow Trigger qualifier sub-fields (Trigger Event → Status/Role well per event type).
- Capacity matrix (Show Advanced → matrix well).
- Add Group Location Schedules modal capacities.

**Recommendation**:
- This is consistent enough across the design that it should be a shared component, named e.g. `<ConditionalWell>`. Inspect refreshed blocks for an existing primitive.
- If not yet shipped, build a small wrapper component during Phase 1.

### 3. Sync Frequency control

**What it is**: a segmented toggle (Mins / Hours / Days) next to a value slider. The active unit is highlighted; the slider's range and step adjust per unit. Captured state shows "Days" highlighted with slider at 15.

**Where it's used**: only the Add Group Sync Rule modal.

**Why it doesn't fit**:
- WebForms `IntervalPicker` is a number-input + dropdown of unit choices. The new control combines the two into a single visual.
- The slider with embedded value pill is uncommon in the existing Obsidian set.

**Recommendation**:
- Option A: Build a new component for this single use. Cost: bounded scope, but maintenance for one consumer.
- Option B: Restyle the existing `<IntervalPicker>` with a slider variant. Cost: cross-block component change risk.
- Option C: Inline the segmented control + a basic `<RangeSlider>` and live with two adjacent controls (drop the unified visual). Cost: minor design fidelity loss.

Recommended: **Option A** during Phase 4, contained to the Group Sync modal. Spec should call out the new component for a separate review.

### 4. Group Location Map Card

**What it is**: a card with an embedded map (16:9), street address text below, schedule text at bottom, and a hover-revealed expand button overlaid on the map. Clicking the expand button (or the card) navigates to the interactive map page for that location.

**Where it's used**: View panel Meeting Locations section, one card per `GroupLocation`.

**Why it may not fit**:
- The map render (with markers, polygons, geofences) is non-trivial.
- The hover-to-reveal interaction adds complexity beyond a static image card.
- The card varies its behavior based on the underlying `Location.Type` (Address / Point / Polygon / GroupMember).

**Recommendation**:
- Identify whether a shared `<GroupMapCard>` or similar already exists in the converted GroupMap or GroupAttendanceList blocks.
- If not, build during Phase 5. The interaction (hover overlay + click navigation) can use existing CSS patterns and a single button overlay.
- The static map image can use the same Map Style block setting and the existing map-rendering helper that powers the WebForms Lava template.

### 5. Linkage list with bordered group sections

**What it is**: at the bottom of the View panel Overview card, a region with a top border separating it from the rest of the Overview content. Inside, three sub-lists with section labels (Registrations, Event Item Occurrences, Content Items) and a list of linked items per section.

**Where it's used**: View panel only.

**Recommendation**:
- Composable from existing primitives (border + label + list of links). No new shared component required.
- Spec should define the bag shape: `linkages: { registrations, eventItemOccurrences, contentItems }: { name, url }[][]`.

## Style / behavior nuances

### Purple labels indicate "changed"

The Figma uses a purple label color to flag every control whose label has been renamed from WebForms. This is purely a designer convention; in production all labels render in the standard label color. Don't treat purple as a runtime style.

### Required-marker (red dot)

Required fields are marked with a red dot to the left of the label (instead of a red asterisk). The existing Obsidian forms use `*`. Confirm whether this is a design system update or a Figma-only convention.

### Helper callouts inside conditional wells

The Capacity matrix has a help callout "Set the **person capacity** for each of the configured schedules below" with the word "person capacity" in bold. This is a `<NotificationBox>` or callout component within the conditional well.

### Strength radio with renamed labels

The radio in Section 2 stack 3 shows None / Casual / Close / Deep instead of None / Basic / Strong / Intense. The underlying enum values map to:

| Display | Underlying value (presumed unchanged) |
|---|---|
| None | 0 |
| Casual | 5 |
| Close | 10 |
| Deep | 20 |

Ensure the bag transmits the integer value and the Vue layer holds a label-mapping table. The C# enum `RelationshipStrength` should keep its existing names (None, Basic, Strong, Intense) for backward compatibility unless the user wants to rename the enum members too.

### Inherit dropdowns become radios

WebForms uses three-option dropdowns ("Inherit from Group Type" / "No" / "Yes") for the chat controls. Design uses radios. Behavior identical.

## Data model deltas

| Field | WebForms data | Design data | Notes |
|---|---|---|---|
| Group image | (probably not used by GroupDetail) | First-class image with Upload button | Confirm whether `Group.PhotoId` (existing nullable column) or new column. |
| Coordinator Notifications None | Explicit checkbox list entry, mutually exclusive | Implicit "no boxes ticked" | Save logic shifts: empty selection → `ScheduleCoordinatorNotificationType.None`. |
| Sync Frequency | `ScheduleIntervalMinutes : int?` | Same field, but UI computes minutes from segmented unit + slider value | Bag shape unchanged. |
| Group attributes by category | All in one DynamicPlaceholder | Per-category sub-stacks | Bag may need to expose `attributesByCategory: { categoryName, attributes }[]` or the Vue layer can group them client-side. |

## Open questions / flag for spec phase

1. **Required marker style** — red dot vs asterisk. Design system update or Figma convention.
2. **`<TagList>` subheader placement** — confirm component supports rendering inside a panel subheader slot, OR plan to refactor TagList placement.
3. **Section / Stack / Conditional Well primitives** — are these already shipped components? Inventory the refreshed-blocks shared component set during Phase 0.
4. **Sync Frequency redesign** — is this a one-off (build new component) or part of a broader IntervalPicker refresh?
5. **Map card** — reuse existing or build new.
6. **Group attribute category grouping** — server-side bag grouping or client-side computed.
