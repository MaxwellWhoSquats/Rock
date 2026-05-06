# Component Inventory

Obsidian components and controls implied by the design, with mapping to existing components where they exist and flags for anything new. Cross-reference [../webforms/20-reference-blocks.md](../webforms/20-reference-blocks.md) for the WebForms-to-Obsidian component map.

## Existing Obsidian components (likely sufficient)

| Design element | Obsidian component | Notes |
|---|---|---|
| Panel header (icon + title + chip labels + follow + kebab) | `<DetailBlock>` | The detail block template handles header, badges, audit, follow. Need to confirm whether chip labels in header are supported or require panel header customization. |
| Section (collapsible, headered) | `<Panel>` (collapsible variant) OR new "Section" component if Sections-and-Stacks is a different shared component | Pattern is used elsewhere (refreshed blocks). Confirm component name. |
| Section Stack (left description + right controls horizontal layout) | New shared component (likely already built for refreshed blocks; verify by reading `groupTypeDetail` and other Fall-2025/Spring-2026 refresh blocks) | Treat as available. |
| Conditional Well (left-bordered, indented sub-block) | New shared component (likely available; check refreshed blocks) | Treat as available. |
| Text input | `<TextBox>` | Standard. |
| Textarea | `<TextBox>` with `text-mode="multi-line"` | Standard. |
| Checkbox + label | `<CheckBox>` | Standard. |
| Inline Checkbox (with help icon) | `<CheckBox>` with help slot | Standard. |
| Radio group | `<RadioButtonList>` | Standard. |
| Inline Switch | `<Switch>` | Standard. |
| Dropdown | `<DropDownList>` | Standard. |
| Person Picker | `<PersonPicker>` | Standard. |
| Group Picker | `<GroupPicker>` | Standard. |
| Group Role Picker | `<GroupRolePicker>` | Standard. |
| Campus Picker | `<CampusPicker>` (or DropDownList bound to campus options) | Standard. |
| Defined Value Picker | `<DefinedValuePicker>` | Standard. |
| DataView Picker | `<DataViewPicker>` | Standard. |
| Workflow Type Picker | `<WorkflowTypePicker>` | Standard. |
| Schedule Picker (multi-select) | `<SchedulePicker>` | Standard. |
| Schedule Builder (iCal) | `<ScheduleBuilder>` | Standard. |
| Date picker / Time picker | `<DatePicker>` / `<TimePicker>` | Standard. |
| Day-of-week picker | (custom dropdown bound to DaysOfWeek enum) | Standard. |
| Image uploader (with placeholder + Upload button) | `<ImageUploader>` | Standard. |
| Notification box / alert | `<NotificationBox>` | Standard. |
| Modal | `<Modal>` | Standard. |
| Number input + suffix label | `<NumberBox>` with suffix slot OR `<NumberBox>` paired with a label component | Confirm suffix support. |
| Editable grid (with add row, edit pencil, delete X, optional reorder) | `<Grid>` | Standard. The reorder handle for Member Attributes appears in the design; confirm Member Workflows behavior. |
| Read-only inherited grid | `<Grid>` configured as light/read-only | Standard. |
| Tag list | `<TagList>` | Standard. Repositioned to subheader in design — confirm component supports being placed in a subheader slot. |
| Helper callout (within conditional wells) | Probably `<Alert>` or a generic callout component | Standard. |
| Person profile link (Group Administrator) | Standard person-link rendering | Need to confirm there's a shared "person link" component. |
| Group profile link (Parent Group) | Standard group-link rendering | Same. |
| Static map render (inside View Meeting Locations card) | Existing GroupMap-style component? | Confirm — could be a new "GroupLocationMapCard" component. |
| Polygon map render | Existing map renderer | Confirm. |
| AttributeValuesContainer | `<AttributeValuesContainer>` | Standard. |
| AttributeEditor (inside member-attribute add modal) | `<AttributeEditor>` | Standard. |
| Quick-link row (icon + label, inside Group Tools card) | Possibly a generic action-list component or simple `<a>` rows | Confirm. |
| Pill chip / label | `<HighlightLabel>` | Standard. |
| Follow star | Standard following control | Standard. |
| Audit menu (kebab) | `<Menu>` or panel-header action button | Confirm. |

## New / custom components (flagged for review)

| Component | Where used | Why new |
|---|---|---|
| **Sync Frequency control** | Add Group Sync Rule modal | Segmented `Mins / Hours / Days` toggle + slider 1-31 with the active unit highlighted. WebForms' `IntervalPicker` is a free-form dropdown + number input. The design control combines unit selection + numeric value into a more visual interface. May warrant either: (a) a new component, or (b) a re-skin of the existing IntervalPicker. |
| **Group Location Map Card** | View panel right column | A composite card with: 16:9 map image at top, address text below, schedule text at bottom, hover-revealed expand button. Could be assembled from primitives but the design suggests a packaged component. |
| **Section Stack / Conditional Well primitives** | Edit panel throughout | If not already shipped to the shared component set, these need to be added. Confirm by inspecting recently-converted blocks. |
| **Linkages list rendering** | View panel Overview card bottom | Sub-headers (Registrations, Event Item Occurrences, Content Items) + list of linked items. Likely simple row composition; flag in case there's a shared "linkage list" pattern. |
| **Quick-link action list** | View panel Group Tools card | Each row is icon + label, behaves as a link. May be representable with existing components. |

## Form input style mapping

| WebForms control | Design replacement | Notes |
|---|---|---|
| `RockRadioButtonList` (vertical) | Same | No change. |
| `RockRadioButtonList` (horizontal) | Same | No change. |
| `RockCheckBoxList` | Checkbox group | Same. Coordinator Notifications drops "None" entry. |
| `RockDropDownList` (Inherit/No/Yes) for Chat | Radio group (Inherit / No / Yes) | Visual change; semantics same. |
| `Switch` for "Show Advanced" | Same | No change. |
| Number with suffix (Capacity, RSVP Lead Time) | `<NumberBox>` + suffix label "Members" / "Days" | Confirm suffix display pattern. |
| `RangeSlider` for RSVP Reminder Offset Days | `<NumberBox>` + "Days" suffix | Slider replaced with input. |
| `IntervalPicker` for Sync interval | Custom Sync Frequency control | Major visual redesign. |

## Iconography

The design uses a consistent icon set (`ti-*` Tabler icons appear in the WebForms version; the new design icons should follow whatever the refreshed blocks use). Specific icon usage observed:

| Region | Icon |
|---|---|
| Section 2 General | gear / settings icon |
| Section 3 RSVP | RSVP / calendar-check icon |
| Section 4 Meeting & Scheduling | calendar icon |
| Section 5 Group Attributes | people / attribute icon |
| Section 6 Member Attributes | people-with-gear icon |
| Section 7 Requirements | requirement / clipboard icon |
| Section 8 Chat | message bubble icon |
| Section 9 Group Sync | sync / refresh icon |
| Section 10 Member Workflows | gear / workflow icon |
| Group Tools card | wrench / tool icon |
| Meeting Locations card | map pin icon |
| Overview card | bullet-list icon |

Each section header icon is small + monochromatic. Not blocking for implementation but should be locked down during Phase 0 when the spec for Section header design is finalized.

## Open questions / flag for spec phase

1. **Sections & Stacks shared component** — confirm name, location, and props. If not yet shipped, decide whether to build first or inline structurally.
2. **Conditional Well shared component** — same.
3. **Number suffix label** — confirm whether `<NumberBox>` supports a suffix slot or whether to render a static "Members" / "Days" / "%" beside it.
4. **Sync Frequency** — build new or restyle IntervalPicker.
5. **Map Card** — build new or assemble from primitives.
6. **Tag list in subheader** — confirm `<TagList>` can render in the subheader slot.
7. **Quick-link rows** — identify the right shared component or build minimally.
8. **Audit menu kebab** — confirm right pattern (Menu component, panel-header action button, etc.).
