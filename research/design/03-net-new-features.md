# Net-New Features (Beyond WebForms Parity)

This file enumerates everything the new design adds that does NOT exist in the WebForms `GroupDetail.ascx` block. Each item includes a brief description, where it lives in the UI, suspected data/service requirements, and an initial phase-partition recommendation per [../webforms/21-phase-partitioning.md](../webforms/21-phase-partitioning.md).

| # | Feature | UI location | Data / service requirements | Phase recommendation |
|---|---|---|---|---|
| 1 | **Group Image** (16:9 ratio uploader) | Edit Section 1 (top fields); View Overview card top | Either reuse `Group.PhotoId` (existing column) or add new column. Image uploader bound to BinaryFile entity. IsTemporary toggle on save. View renders if image present, omits region entirely if not. | Phase 2 (core edit fields) — folds into the same image-uploader work as the chat avatar. |
| 2 | **Audit Details modal** (replaces audit drawer) | Header kebab menu → "Audit Details" item | Modal showing CreatedDateTime, ModifiedDateTime, CreatedByPersonAlias, ModifiedByPersonAlias, plus probably history rows. Replaces `PanelDrawer pdAuditDetails`. | Phase 1 (shell + view) — the audit drawer was in the shell phase. |
| 3 | **Public state subheader label** | View panel subheader, beside relationship strength + tags | New "Public" pill chip when `IsPublic == true`. Uses existing field. | Phase 1 (shell + view). |
| 4 | **Group Tools card with sub-headers** | View panel right column | Replaces hyperlink toolbar in WebForms footer. Sub-headers ("Participation", "Views") group the quick-link items. Each item is conditional on the corresponding LinkedPage block setting AND the corresponding GroupType flag. | Phase 1 (shell + view). |
| 5 | **Map Cards for Meeting Locations** | View panel right column (own card) | Per-`GroupLocation` map card. Hover reveals expand button. Click navigates to `GroupMapPage`. Renders street address + schedule text. Polygon-style locations render the polygon visualization with no address. | Phase 5 (locations + schedules). |
| 6 | **Linkages section in Overview** | View panel left column, bottom of Overview card | Three groups: Registrations, Event Item Occurrences, Content Items. Each is a list of linked items with name + URL. Hidden entirely if all three lists are empty. Top-bordered region. | Phase 1 (shell + view) — the bag must populate the linkage lists from the same EF queries that fed the Lava merge fields. |
| 7 | **Tags in subheader (conditional on block setting)** | View panel subheader | TagList control moved out of the body and into the subheader. Hidden if `EnableGroupTags` block setting is false. | Phase 1 (shell + view). |
| 8 | **Group Type label in panel header** | Both panels' header (right side, beside follow star) | Persistent chip showing the group type's name. Linked to GroupType detail page if user has ADMINISTRATE on the group type. | Phase 1 (shell + view). |
| 9 | **Campus label in panel header** | Both panels' header | Persistent chip showing the group's campus name. Hidden when no campus set. | Phase 1 (shell + view). |
| 10 | **Sections & Stacks pattern** | Edit panel | Standard Obsidian "Sections & Stacks" UI. Each section is collapsible, each stack is a horizontal description+controls layout. | Phase 1-2 (shell + edit core) — must be wired up early since every later phase depends on it. |
| 11 | **Conditional Wells** | Edit panel | Subdued left-bordered blocks that wrap conditional sub-content. Used in: Inactive flow, Security Level (under IsSecurityRole), Advanced Relationship Settings, Group Schedule sub-types, Coordinator Notifications, Member Workflow Trigger qualifier sub-fields, Capacity matrix. | Phase 1-2 (shell + edit core). |
| 12 | **Inactive flow conditional well** | Edit panel Section 1 | When Active is unchecked, the well shows Inactive Reason (required), Inactive Note, "Also Inactivate Child Groups" with no `hfHasChildGroups` gate (always shown). | Phase 2. |
| 13 | **"Add Group" header treatment** | Edit panel header on Add | Clean "Add Group" title only — no labels, no chrome icons. Group Type becomes a required dropdown in Section 2 (not a static field). | Phase 1 (header) + Phase 2 (dropdown placement). |
| 14 | **Sync Frequency control redesign** | Add Group Sync Rule modal | Segmented `Mins / Hours / Days` + slider 1-31. Replaces `IntervalPicker`. May need a custom Obsidian component. | Phase 4 (sub-features bundle). |
| 15 | **Renamed relationship strength labels** | Edit panel Section 2 stack 3; View panel subheader | None / Casual / Close / Deep replacing None / Basic / Strong / Intense. Underlying enum integer values unchanged. Every view rendering relationship strength must use the new strings. | Phase 2 (edit) + Phase 1 (view subheader). |
| 16 | **Coordinator Notifications without None** | Edit panel Section 4 stack 3 | Checkbox list with only Decline / Accept / Self-Schedule. "None" expressed as "no boxes ticked". Save logic updates to detect zero selection as None. | Phase 2 (scheduling settings). |
| 17 | **Group attribute categories surfaced as stack headers** | Edit panel Section 5 | Each `Attribute.Category` becomes a sub-stack with its own description text. Today's WebForms renders all in one DynamicPlaceholder. | Phase 3 (attributes). |
| 18 | **"Trailblazer Settings"** | Unknown | Listed as "New" by designer; not visible in captured frames. **Open question** — see [00-overview.md](00-overview.md). | TBD pending clarification. |
| 19 | **Block settings reorganization into named sections** | Block Settings panel (admin) | Two named sections: General Settings + Page Routing. Several attribute labels renamed (underlying keys preserved). | Phase 1 — implemented when block attributes are declared on the C# class. |
| 20 | **Linked Administrator and Parent Group in Overview** | View panel Overview card | Both render as clickable links to person profile / group detail. WebForms shows them as plain text inside the Lava template. | Phase 1 (shell + view). |
| 21 | **Group Image displayed in 16:9 hero in Overview** | View panel Overview card top | Renders `Group.PhotoId` (or new field) as the visual hero. If no image, region omitted entirely (no placeholder). | Phase 1 (shell + view). |
| 22 | **Compact / minimal-data view state** | View panel | When the group has no locations, no linkages, minimal attributes, the right column collapses to just Group Tools (single sub-header, fewer items). Designer's alt state shows this. Two-column layout maintained. | Phase 1 (shell + view). |

## Behavior changes (not strictly net-new but worth flagging)

| # | Change | Phase recommendation |
|---|---|---|
| C1 | Copy modal default for "Include Child Groups" flips from CHECKED (WebForms) to UNCHECKED (design). | Phase 1. |
| C2 | "Inactivate Child Groups" renamed to "Also Inactivate Child Groups". | Phase 2. |
| C3 | Several block-attribute LABELS renamed (underlying keys preserved). | Phase 1. |
| C4 | Many control labels renamed for clarity (purple labels in Figma). Several listed in [02-edit-panel.md](02-edit-panel.md). | Phase 2-5 per section. |
| C5 | Yes/No/Inherit for Chat fields render as radios (not dropdowns). | Phase 2. |
| C6 | Security Level for security roles renders as radio (not dropdown). | Phase 2. |
| C7 | RSVP Reminder Lead Time renders as numeric input + "Days" suffix (not slider). | Phase 2. |
| C8 | Who Can Check-in renders as radio (was dropdown bound to enum). | Phase 2. |

## Removed / dropped from WebForms

| # | Item | Replacement |
|---|---|---|
| R1 | Audit drawer (`PanelDrawer pdAuditDetails`) | Audit Details modal (#2 above). |
| R2 | View Mode Lava Template (`GroupType.GroupViewLavaTemplate`) | Pure Vue View panel. Customer customizations lost — see [../webforms/17-view-panel.md](../webforms/17-view-panel.md) for migration story. |
| R3 | "Has Requirements" icon in subheader | Removed — designer-confirmed. |
| R4 | "Capacities" label above relationship multiplier matrix | Removed — designer-confirmed cosmetic. |
| R5 | Section nav (panel section navigation widget) | Removed — designer reasoning: "did not include section nav since page includes the group tree view block on left". |
| R6 | Reorder handle on Member Workflows table | Possibly removed — flagged as open question. |
| R7 | Group Requirements grid columns (Data View, Can Expire, RequirementCheckType) | Possibly collapsed into the row's modal-only or icon view. Open question. |

## Phase-partition impact

These additions and changes do NOT require restructuring the 8-phase partition in [../webforms/21-phase-partitioning.md](../webforms/21-phase-partitioning.md). Each item slots into an existing phase. Most net-new items land in Phase 1 (shell + view) or Phase 2 (core edit fields).

The most consequential additions for spec scope:
- **#5 Map cards** — adds visual + interaction complexity to Phase 5.
- **#6 Linkages** — adds three new bag collections to Phase 1.
- **#11 Conditional Wells pattern** — must be a shared component before Phase 2.
- **#14 Sync Frequency control** — may require new component work in Phase 4.

## Open questions / flag for spec phase

1. **Trailblazer Settings**: missing visual definition.
2. **Image data source**: `Group.PhotoId` or new column.
3. **Audit modal contents**: not captured in design.
4. **Compact-state right-column behavior**: stay or collapse-and-fill.
5. **Sync Frequency component**: build new or restyle existing.
6. **Linkage bag schema**: confirm shape before Phase 1 implementation.
7. **Removed grid columns**: confirm vs. captured state.
