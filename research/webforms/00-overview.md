# GroupDetail Overview

Source: `RockWeb/Blocks/Groups/GroupDetail.ascx` + `GroupDetail.ascx.cs`
Size: 826 lines markup + 5,124 lines code-behind = 5,950 total.

## What this block does

The `GroupDetail` block is the canonical admin UI for managing a single `Rock.Model.Group` record. It is launched from group lists, the group tree view, and various redirects with a `?GroupId=N` parameter. It supports five high-level operations:

1. **View** an existing group (rendered via the group type's `GroupViewLavaTemplate`).
2. **Edit** the group (a heavily conditional form with up to 11 panel widgets).
3. **Create** a new group (same form, with most defaults inferred from a `?ParentGroupId=N` if supplied).
4. **Delete** or **Archive** the group (Archive replaces Delete when the group type has `EnableGroupHistory = true` and history rows already exist).
5. **Copy** the group (with optional include-child-groups), via `GroupService.CopyGroup`.

It also renders a row of "quick link" hyperlinks in view mode that route to other blocks: Group Placement, Group RSVP, Group Scheduler, Group History, Fundraising, Attendance, Map. These are configured per block instance via Linked Page attributes.

## Block class

```csharp
[ContextAware( typeof( Group ) )]
public partial class GroupDetail : ContextEntityBlock
```

Inherits `ContextEntityBlock` (a WebForms-only base) which means it can also be context-aware: another block on the same page can hand it a `Group` via context, in which case the URL parameter is bypassed.

## Why it is so large

The block is essentially a single-page editor for the entire `Group` aggregate, including:
- The base `Group` record (~50 fields).
- Up to N `GroupLocation` rows, each with up to N `Schedule` rows and up to N `GroupLocationScheduleConfig` rows.
- N `GroupRequirement` rows defined on the group itself (separate from inherited ones).
- N `Attribute` rows that define group-member attributes scoped to this group instance.
- Live group-attribute values rendered through the dynamic attribute editor.
- N `GroupSync` rows with welcome/exit communications and roles.
- N `GroupMemberWorkflowTrigger` rows.
- An override surface for Peer Network relationship configuration.
- A chat-channel override surface (when chat is enabled platform-wide and on the group type).
- An RSVP override surface (when the group type enables RSVP).
- A scheduling override surface (when the group type has IsSchedulingEnabled).

Many of those areas have their own modal dialog editors (Locations, Group Member Attribute, Group Requirement, Group Sync, Member Workflow Trigger, Archive confirmation, Copy confirmation).

Additionally, almost everything is gated by the configuration of the group's `GroupType`. The block has elaborate per-GroupType show/hide logic that fires on `GroupType` change to reshape the form.

## Why it can't use plain `/convert-block`

- **Size.** The auto-generated bag and partials would be large enough that a single review pass can't keep them all coherent.
- **New design.** The user has indicated the View panel will be redesigned beyond a 1:1 port and new features will land. That design work belongs in spec discussions, not in a single conversion session.
- **Independent sub-domains.** Several sub-features (Group Sync, Member Workflow Triggers, Group Requirements, Locations) are self-contained enough that they can ship independently behind a phased rollout, which is incompatible with all-or-nothing conversion.

## Block configuration summary

- 21 block attributes (linked pages, group-type filters, behavior toggles, security toggles).
- 6 page parameters (`GroupId`, `ParentGroupId`, `ExpandedIds`, `EventItemOccurrenceId`, `autoEdit`, `returnUrl`).
- Inherits `ContextEntityBlock` (Group context).
- Block type GUID: `582BEEA1-5B27-444D-BC0A-F60CEB053981`.
- DefaultBlockRole: `Primary` (CMS).

See `01-block-configuration.md` for the full enumeration.
