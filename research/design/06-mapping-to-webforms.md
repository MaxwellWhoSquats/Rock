# Mapping: Design Regions to WebForms Research

A bridge document between the design and the WebForms research. For every region of the design, this maps to the corresponding WebForms research file so a spec author can hop between the two.

## View panel mapping

| Design region | WebForms research |
|---|---|
| Panel header (icon + name) | [../webforms/03-markup-structure.md#view-panel](../webforms/03-markup-structure.md) |
| Group Type label in header | [../webforms/04-code-behind-walkthrough.md](../webforms/04-code-behind-walkthrough.md) `ShowReadonlyDetails` (line ~2674) |
| Campus label in header | Same. (line ~2693) |
| Follow star | [../webforms/27-misc-surfaces.md](../webforms/27-misc-surfaces.md) "Following" |
| Audit kebab → Audit Details modal | [../webforms/27-misc-surfaces.md](../webforms/27-misc-surfaces.md) "Audit drawer" — replaces it |
| Subheader: Relationship Strength label | [../webforms/06-peer-network.md](../webforms/06-peer-network.md) "View-mode label (highlight label)" |
| Subheader: Tags | [../webforms/27-misc-surfaces.md](../webforms/27-misc-surfaces.md) "Tags" — repositioned |
| Subheader: Public label | New (designer note); maps to `group.IsPublic` |
| Overview card: Image | New — flag for data source |
| Overview card: Description | `group.Description` |
| Overview card: Group Administrator | `group.GroupAdministratorPersonAlias.Person` ([../webforms/05-entity-and-services.md](../webforms/05-entity-and-services.md)) |
| Overview card: Parent Group | `group.ParentGroup` |
| Overview card: Schedule | `group.Schedule.FriendlyScheduleText` |
| Overview card: Group Capacity | `group.GroupCapacity` |
| Overview card: Group Attributes | [../webforms/09-group-attributes.md](../webforms/09-group-attributes.md) |
| Overview card: Linkages section | New structured rendering of WebForms Lava-merged registration/event/content lists. See [../webforms/17-view-panel.md](../webforms/17-view-panel.md) "Merge fields supplied" |
| Group Tools card: Participation (Attendance, Scheduler, RSVP, Placement) | [../webforms/18-cross-block-dependencies.md](../webforms/18-cross-block-dependencies.md) "Outbound: where GroupDetail navigates to" |
| Group Tools card: Views (Map, History) | Same. |
| Meeting Locations card | [../webforms/07-locations-and-schedules.md](../webforms/07-locations-and-schedules.md) — fundamentally restructured for view |
| Footer Edit / Cancel buttons | Standard panel footer |
| Footer Copy button | [../webforms/16-archive-delete-copy.md](../webforms/16-archive-delete-copy.md) "Copy" |
| Footer Security button | Standard SecurityButton (`btnSecurity`) |
| Copy modal | [../webforms/16-archive-delete-copy.md](../webforms/16-archive-delete-copy.md) "Copy" — default for Include Child Groups flipped |

## Edit panel mapping

| Design region | WebForms research |
|---|---|
| Section 1: Name + Active + Description + Show Publicly + Image | [../webforms/03-markup-structure.md](../webforms/03-markup-structure.md) "Top fields"; Image is new |
| Section 1: Inactive flow conditional well | [../webforms/02-block-states.md](../webforms/02-block-states.md) "Field-controlled state" → IsActive |
| Section 2 Stack 1: Overview (Group Type, Parent, Campus, Status) | wpGeneral top half |
| Section 2 Stack 2: Administration & Security (Admin, Capacity, Signature Doc, Record Source, Security Role + Level) | wpGeneral middle |
| Section 2 Stack 3: Relationships | [../webforms/06-peer-network.md](../webforms/06-peer-network.md) |
| Section 3: RSVP | [../webforms/15-rsvp.md](../webforms/15-rsvp.md) |
| Section 4 Stack 1: Overall Group Schedule | [../webforms/07-locations-and-schedules.md](../webforms/07-locations-and-schedules.md) "Inline (group's primary) Schedule" |
| Section 4 Stack 2: Group Location Schedules | [../webforms/07-locations-and-schedules.md](../webforms/07-locations-and-schedules.md) "Locations grid" |
| Section 4 Stack 3: Member Scheduling & Check-in | [../webforms/08-scheduling.md](../webforms/08-scheduling.md) |
| Section 5: Group Attributes | [../webforms/09-group-attributes.md](../webforms/09-group-attributes.md) — design adds per-category stack grouping |
| Section 6: Member Attributes (inherited + custom) | [../webforms/10-member-attributes.md](../webforms/10-member-attributes.md) |
| Section 7: Requirements (inherited + custom) | [../webforms/11-group-requirements.md](../webforms/11-group-requirements.md); [../webforms/24-grouptype-inheritance.md](../webforms/24-grouptype-inheritance.md) |
| Section 8: Chat | [../webforms/14-chat.md](../webforms/14-chat.md) — Yes/No/Inherit dropdowns become radios |
| Section 9: Group Sync Settings | [../webforms/12-group-sync.md](../webforms/12-group-sync.md) |
| Section 10: Member Workflows | [../webforms/13-member-workflow-triggers.md](../webforms/13-member-workflow-triggers.md) |

## Modal mapping

| Design modal | WebForms modal | WebForms research |
|---|---|---|
| Add Group Location Schedules | dlgLocations | [../webforms/07-locations-and-schedules.md](../webforms/07-locations-and-schedules.md) |
| Add Group Requirement | mdGroupRequirement | [../webforms/11-group-requirements.md](../webforms/11-group-requirements.md) |
| Add Group Sync Rule | mdGroupSyncSettings | [../webforms/12-group-sync.md](../webforms/12-group-sync.md) |
| Add Group Member Workflow | dlgMemberWorkflowTriggers | [../webforms/13-member-workflow-triggers.md](../webforms/13-member-workflow-triggers.md) |
| Copy Group | mdCopyGroup | [../webforms/16-archive-delete-copy.md](../webforms/16-archive-delete-copy.md) |
| Audit Details (new) | (replaces audit drawer pdAuditDetails) | [../webforms/27-misc-surfaces.md](../webforms/27-misc-surfaces.md) |
| Archive confirmation | mdArchive | [../webforms/16-archive-delete-copy.md](../webforms/16-archive-delete-copy.md). Not captured in design — assume preserved. |
| Add Group Member Attribute (uses AttributeEditor) | dlgGroupMemberAttribute | [../webforms/10-member-attributes.md](../webforms/10-member-attributes.md). Not separately captured in design — assume parity. |
| Delete confirmation | Browser confirm via `Rock.dialogs.confirmDelete` | [../webforms/16-archive-delete-copy.md](../webforms/16-archive-delete-copy.md). Standard pattern. |

## Block settings mapping

WebForms attribute keys preserved, labels renamed:

| Design label (Section 1: General Settings) | WebForms attribute key |
|---|---|
| Group Types: Include | `GroupTypes` |
| Group Types: Exclude | `GroupTypesExclude` |
| Security Role Groups Only | `LimittoSecurityRoleGroups` |
| Navigation Group Types Only | `LimitToShowInNavigationGroupTypes` |
| Map Style | `MapStyle` |
| Show Copy Button | `ShowCopyButton` |
| Exclude Inactive Campuses | `PreventSelectingInactiveCampus` |
| Show Location Addresses | `ShowLocationAddresses` |
| Enable Group Tags | `EnableGroupTags` |
| Grant Creator Admin Rights | `AddAdministrateSecurityToGroupCreator` |

| Design label (Section 2: Page Routing) | WebForms attribute key |
|---|---|
| Group Map Page | `GroupMapPage` |
| Attendance Page | `AttendancePage` |
| Registration Instance Page | `RegistrationInstancePage` |
| Event Item Occurrence Page | `EventItemOccurrencePage` |
| Content Item Page | `ContentItemPage` |
| Group List Page | `GroupListPage` |
| Fundraising Progress Page | `FundraisingProgressPage` |
| Group History Page | `GroupHistoryPage` |
| Group Scheduler Page | `GroupSchedulerPage` |
| Group RSVP List Page | `GroupRSVPPage` |
| Group Placement Page | `GroupPlacementPage` |

The `TagCategory` block attribute referenced by WebForms (line 543, never declared) is not surfaced in the design notes. Either drop the reference (preferred) or formalize the attribute. See [../webforms/27-misc-surfaces.md](../webforms/27-misc-surfaces.md) "Tags".

## Field-level rename map

Cosmetic-only label changes (underlying data unchanged). Listed by WebForms label → Design label.

| WebForms | Design |
|---|---|
| Inactivate Child Groups | Also Inactivate Child Groups |
| Group Type | Group Type (unchanged) |
| Parent Group | Parent Group (unchanged) |
| Campus | Campus (unchanged) |
| Description | Description (unchanged) |
| Active | Active (unchanged) |
| Public | Show Publicly |
| Group Capacity | Group Capacity (with "Members" suffix) |
| Administrator (or whatever the AdministratorTerm is) | Group Administrator |
| Require Signed Document | Require Signed Document (unchanged) |
| Record Source Override | Member Record Source |
| Security Role | Enable as Security Role |
| Elevated Security Level | Security Level |
| Override Relationship Strength | Override Relationship Strength (unchanged) |
| Relationship Strength options None/Basic/Strong/Intense | None/Casual/Close/Deep |
| Show Advanced Settings | Show Advanced Relationship Settings |
| Enable Relationship Growth Over Time | Enable Relationship Growth Over Time (unchanged) |
| RSVP Reminder System Communication | RSVP Reminder Communication |
| RSVP Reminder Offset Days | RSVP Reminder Lead Time |
| Group Schedule | Group Schedule (unchanged) |
| Disable Group Scheduling | Disable Group Member Scheduling |
| Disable Schedule Toolbox Access | Hide from Schedule Toolbox |
| Scheduling Must Meet Requirements | Require Member Requirements for Scheduling |
| Schedule Confirmation Logic | Confirmation Behavior |
| Schedule Coordinator | Schedule Coordinator (unchanged) |
| Schedule Coordinator Notification Options | Coordinator Notifications |
| Check-in Requirements | Who Can Check-in |
| AttendanceRecordRequiredForCheckIn enum: Anyone / Anyone (Scheduled Members Pre-selected) / Only Scheduled Members | Same labels |
| Enable Chat | Enable Group Chat |
| Allow Members to Leave Channel | Allow Members to Leave Channel (unchanged) |
| Make Channel Public | Public Channel |
| Always Show Channel | Always Show Channel (unchanged) |
| Push Notification Mode | Push Notification Mode (unchanged) |
| Channel Avatar | Channel Avatar (unchanged) |
| Group Sync Settings → Sync Data View | Add Group Sync Rule → Data View |
| → Group Role to Assign | → Assigned Role |
| → Sync Interval | → Sync Frequency |
| → Welcome Communication | → Welcome Communication (unchanged) |
| → Exit Communication | → Exit Communication (unchanged) |
| → Create Login During Sync | → Create Login During Sync (unchanged) |
| Group Member Workflows / Add Trigger → Name | Add Group Member Workflow → Workflow Trigger Name |
| → Start Workflow | → Workflow Type |
| → When | → Trigger Event |
| → Active | → Active (unchanged) |
| Add Group Requirement → Applies to Group Role | Applies to Role |
| → Applies to Age Classification | Applies to Age Group |
| → Allow Leaders to Override | Allow Leader Override |
| → Members must meet this requirement before adding | Require Before Adding Member |

## Phase-partition impact

The 8-phase plan in [../specs/ROADMAP.md](../specs/ROADMAP.md) does NOT need restructuring. Each design item maps cleanly to the existing phases.

Recommended phase-by-phase deltas (in addition to the existing plan):

| Phase | Design-related additions |
|---|---|
| Phase 0 (architecture) | Confirm Sections & Stacks + Conditional Well shared components. Lock down audit modal scope. Decide on Image data source. Confirm Sync Frequency component approach. |
| Phase 1 (shell + view) | New Group Image field, Audit modal, Linkages bag, Public subheader label, Group Type + Campus header chips, Group Tools card. |
| Phase 2 (core edit) | All renamed labels for scalar fields. Inactive flow conditional well. RSVP, Scheduling, Chat radio refactor. Coordinator Notifications no-None handling. Capacity "Members" suffix. |
| Phase 3 (attributes) | Per-category stacks for group attributes. |
| Phase 4 (Requirements + Sync + Triggers) | Add Group Sync Rule modal with Sync Frequency control. Renamed Requirements modal labels. Possibly drop reorder on Member Workflows. |
| Phase 5 (Locations + Schedules) | Map cards in view. Section 4 stack merge of Meeting Details + Scheduling. Renamed labels. |
| Phase 6 (View panel polish) | Final visual polish to match Figma fidelity if not done in Phase 1. |
| Phase 8 (cutover) | Release notes for label renames + customer-template loss. |

## Open questions / flag for spec phase

1. **Designer-flagged "Trailblazer Settings"** — unknown reference. Spec phase needs clarification.
2. **Audit modal scope** — content not captured.
3. **Group Image data source** — `Group.PhotoId` or new column.
4. **Sync Frequency component approach** — build vs reuse.
5. **Group attribute category source** — `Attribute.Category` or designer-set captions.
6. **Member Workflows reorder** — preserve or drop.
7. **Requirements grid columns** — drop or just hidden in captured state.
