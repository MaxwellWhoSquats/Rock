// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//

using System;
using System.Collections.Generic;

using Rock.Enums.Group;
using Rock.Model;
using Rock.ViewModels.Utility;

namespace Rock.ViewModels.Blocks.Group.GroupDetail
{
    /// <summary>
    /// Per-GroupType options consumed by the edit panel. Returned by the
    /// <c>GetGroupTypeOptions</c> block action when <c>currentGroupTypeId</c>
    /// changes mid-edit (Q2 Approach B). Carries every flag, dropdown source,
    /// and pin-by-group-type value the edit panel needs to reshape itself
    /// when the GroupType changes. Also returned alongside the initial
    /// <c>Edit</c> bag so the edit panel can render its first frame without
    /// a second round-trip.
    /// </summary>
    public class GroupTypeOptionsBag
    {
        #region Visibility flags driving section/sub-section render

        /// <summary>
        /// Gets or sets a value indicating whether the RSVP section
        /// renders. Mirrors <c>GroupType.EnableRSVP</c>.
        /// </summary>
        public bool IsRsvpSectionVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Chat section
        /// renders. Combines <c>ChatHelper.IsChatEnabled</c> (the system
        /// flag) with <c>GroupType.IsChatAllowed</c>.
        /// </summary>
        public bool IsChatSectionVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Scheduling section's
        /// "Overall Group Schedule" stack renders. True when any
        /// <c>GroupType.AllowedScheduleTypes</c> flag is set.
        /// </summary>
        public bool IsSchedulingSectionVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Peer Network override
        /// stack renders inside the General section. Mirrors
        /// <c>GroupType.IsPeerNetworkEnabled</c>.
        /// </summary>
        public bool IsPeerNetworkSectionVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Group Administrator
        /// picker renders. Mirrors <c>GroupType.ShowAdministrator</c>.
        /// </summary>
        public bool IsAdministratorVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Member Record Source
        /// picker renders. Mirrors
        /// <c>GroupType.AllowGroupSpecificRecordSource</c>.
        /// </summary>
        public bool IsGroupSpecificRecordSourceVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Confirmation Behavior
        /// dropdown renders within the Member Scheduling stack. Mirrors
        /// <c>GroupType.IsSchedulingEnabled</c>.
        /// </summary>
        public bool IsScheduleConfirmationLogicVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Schedule Coordinator
        /// person picker renders. Mirrors <c>GroupType.IsSchedulingEnabled</c>.
        /// </summary>
        public bool IsScheduleCoordinatorVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Coordinator
        /// Notifications checkboxes render. Mirrors
        /// <c>GroupType.IsSchedulingEnabled</c>.
        /// </summary>
        public bool IsCoordinatorNotificationsVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Who Can Check-in
        /// radio renders. Mirrors <c>GroupType.TakesAttendance</c>.
        /// </summary>
        public bool IsCheckInRequirementsVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Group Capacity field
        /// renders. Mirrors <c>GroupType.GroupCapacityRule != None</c>.
        /// </summary>
        public bool IsGroupCapacityVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Group Capacity field
        /// is required. Mirrors <c>GroupType.IsCapacityRequired</c>.
        /// </summary>
        public bool IsGroupCapacityRequired { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Inactive Reason
        /// dropdown renders inside the Inactive flow conditional well.
        /// Mirrors <c>GroupType.EnableInactiveReason</c>.
        /// </summary>
        public bool IsInactiveReasonVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Inactive Reason
        /// dropdown is required. Mirrors
        /// <c>GroupType.RequiresInactiveReason</c>.
        /// </summary>
        public bool IsInactiveReasonRequired { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Status defined-value
        /// picker renders. Mirrors
        /// <c>GroupType.GroupStatusDefinedTypeId.HasValue</c>.
        /// </summary>
        public bool IsStatusVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether selecting a Campus is
        /// required when saving a group of this type. Mirrors
        /// <c>GroupType.GroupsRequireCampus</c>.
        /// </summary>
        public bool RequiresCampus { get; set; }

        #endregion

        #region Allowed-flags

        /// <summary>
        /// Gets or sets the bitmask of <c>ScheduleType</c> values the group
        /// type permits. Drives which radio options render under Section 4
        /// Stack 1.
        /// </summary>
        public ScheduleType AllowedScheduleTypes { get; set; }

        /// <summary>
        /// Gets or sets the location-selection mode for the group type.
        /// Drives the Section 4 Stack 2 visibility (hidden when
        /// <c>None</c>) plus the
        /// <c>&lt;LocationPicker&gt;</c>'s <c>allowedPickerModes</c>
        /// inside the Location modal.
        /// </summary>
        public GroupLocationPickerMode LocationSelectionMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether per-location schedule
        /// configuration is enabled. Drives the visibility of the
        /// Schedule(s) multi-picker in the Location modal. Mirrors
        /// <c>GroupType.EnableLocationSchedules</c>.
        /// </summary>
        public bool EnableLocationSchedules { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether scheduling is enabled
        /// at the group-type level. Drives the master switch for the
        /// Member Scheduling stack.
        /// </summary>
        public bool IsSchedulingEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether more than one
        /// <c>GroupLocation</c> may be added to groups of this type.
        /// Drives the Section 4 Stack 2 grid's Add-button visibility per
        /// WebForms parity at <c>GroupDetail.ascx.cs:3872</c>: the Add
        /// button is shown only when
        /// <c>allowMultipleLocations || locations.length == 0</c>.
        /// Mirrors <c>GroupType.AllowMultipleLocations</c>.
        /// </summary>
        public bool AllowMultipleLocations { get; set; }

        #endregion

        #region Localization

        /// <summary>
        /// Gets or sets the localized term for the group administrator
        /// (default: "Administrator"). Used as the label of the Group
        /// Administrator picker.
        /// </summary>
        public string AdministratorTerm { get; set; }

        /// <summary>
        /// Gets or sets the icon CSS class sourced from the group type
        /// (e.g., <c>ti ti-users-group</c>). Drives the panel header icon
        /// when the GroupType changes mid-edit.
        /// </summary>
        public string IconCssClass { get; set; }

        #endregion

        #region Defaults / placeholder text for peer-network multipliers and relationship strength

        /// <summary>
        /// Gets or sets the group-type's relationship strength default,
        /// shown as the radio's resolved value when the group has no
        /// override. Falls back to <c>None</c> when the group type has
        /// no value set.
        /// </summary>
        public RelationshipStrength RelationshipStrengthDefault { get; set; }

        /// <summary>
        /// Gets or sets the group-type's relationship-growth-enabled
        /// default, used when the group has no override.
        /// </summary>
        public bool RelationshipGrowthEnabledDefault { get; set; }

        /// <summary>
        /// Gets or sets the group-type's leader-to-leader multiplier as a
        /// 0-1 decimal. Rendered as the placeholder ("inherited") on the
        /// override input.
        /// </summary>
        public decimal LeaderToLeaderMultiplierDefault { get; set; }

        /// <summary>
        /// Gets or sets the group-type's leader-to-non-leader multiplier
        /// as a 0-1 decimal. Rendered as the placeholder ("inherited") on
        /// the override input.
        /// </summary>
        public decimal LeaderToNonLeaderMultiplierDefault { get; set; }

        /// <summary>
        /// Gets or sets the group-type's non-leader-to-leader multiplier
        /// as a 0-1 decimal. Rendered as the placeholder ("inherited") on
        /// the override input.
        /// </summary>
        public decimal NonLeaderToLeaderMultiplierDefault { get; set; }

        /// <summary>
        /// Gets or sets the group-type's non-leader-to-non-leader
        /// multiplier as a 0-1 decimal. Rendered as the placeholder
        /// ("inherited") on the override input.
        /// </summary>
        public decimal NonLeaderToNonLeaderMultiplierDefault { get; set; }

        #endregion

        #region Pinned values that null out per-group overrides

        /// <summary>
        /// Gets or sets the group-type's pinned RSVP reminder offset days.
        /// Non-null means the group-type pins this value and the per-group
        /// override is read-only. Null means the group can override.
        /// </summary>
        public int? RsvpReminderOffsetDays { get; set; }

        /// <summary>
        /// Gets or sets the group-type's pinned RSVP reminder system
        /// communication. Non-null means the group-type pins this value
        /// and the per-group override is read-only; the
        /// <c>ListItemBag.text</c> is shown as the readonly label.
        /// Null means the group can override.
        /// </summary>
        public ListItemBag RsvpReminderSystemCommunication { get; set; }

        /// <summary>
        /// Gets or sets the list of system communications in the RSVP
        /// Confirmation category. Populates the per-group dropdown when
        /// the group-type does not pin the value.
        /// </summary>
        public List<ListItemBag> RsvpSystemCommunicationOptions { get; set; }

        #endregion

        #region Allowed group-status defined values (Status dropdown)

        /// <summary>
        /// Gets or sets the list of <see cref="DefinedValue"/> rows under
        /// <c>GroupType.GroupStatusDefinedType</c>, used to populate the
        /// Status dropdown. Empty list when the group type does not
        /// configure a status defined type.
        /// </summary>
        public List<ListItemBag> StatusValues { get; set; }

        #endregion

        #region Inactive reasons

        /// <summary>
        /// Gets or sets the list of allowed inactive reason
        /// <c>DefinedValue</c> rows for this group type, sourced from
        /// <c>GroupTypeService.GetInactiveReasonsForGroupType</c>. Empty
        /// list when <c>GroupType.EnableInactiveReason</c> is false.
        /// </summary>
        public List<ListItemBag> InactiveReasons { get; set; }

        #endregion

        #region Inherited member-attribute definitions (Section 6, read-only grid)

        /// <summary>
        /// Gets or sets the inherited group-member attribute definitions
        /// for the active group type. Walks the
        /// <c>GroupType.InheritedGroupTypeId</c> chain server-side and
        /// emits each attribute with the immediate ancestor's name + URL
        /// for the Section 6 "Inherited Attributes" grid. Empty when the
        /// chain yields no inherited member attributes.
        /// </summary>
        public List<GroupMemberInheritedAttributeBag> InheritedMemberAttributes { get; set; }

        #endregion

        #region Section 6 / 7 / 9 / 10 visibility flags (Phase 5 additions)

        /// <summary>
        /// Gets or sets a value indicating whether the group type
        /// permits per-group attribute definitions for members. Drives
        /// the Section 6 panel-visibility gate (along with the per-user
        /// ADMINISTRATE flag on the group bag) per WebForms parity at
        /// <c>GroupDetail.ascx.cs:2001-2004</c>. Mirrors
        /// <c>GroupType.AllowSpecificGroupMemberAttributes</c>.
        /// </summary>
        public bool AllowSpecificGroupMemberAttributes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the group type
        /// permits per-group group requirements. Drives the Section 7
        /// "Specific Group Requirements" Add-button visibility plus the
        /// editable-grid wrapper visibility per WebForms parity at
        /// <c>GroupDetail.ascx.cs:4554</c>. Mirrors
        /// <c>GroupType.EnableSpecificGroupRequirements</c>.
        /// </summary>
        public bool EnableSpecificGroupRequirements { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the group type
        /// permits group sync rules. Drives the Section 9 panel
        /// visibility (combined with ADMINISTRATE on the group bag and
        /// the presence of any existing syncs).
        /// <para>
        /// Mirrors <c>GroupType.AllowGroupSync</c>. The corresponding
        /// WebForms gate at <c>GroupDetail.ascx.cs:2195</c> uses
        /// ADMINISTRATE on the <em>GroupType</em>, with a sticky
        /// override at <c>:2002</c> that keeps the panel visible for an
        /// existing group when the user has ADMINISTRATE on the
        /// <em>Group</em>. The Obsidian impl simplifies both into a
        /// single ADMINISTRATE-on-the-Group check on the group bag,
        /// which is the dominant code path under standard Rock auth
        /// (Group auth inherits from GroupType) and avoids the
        /// ViewState stickiness that has no analog here.
        /// </para>
        /// </summary>
        public bool AllowGroupSync { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the group type
        /// permits per-group member workflow triggers. Drives the
        /// Section 10 panel visibility (combined with the presence of
        /// any legacy triggers) per WebForms parity at
        /// <c>GroupDetail.ascx.cs:2198</c>. Mirrors
        /// <c>GroupType.AllowSpecificGroupMemberWorkflows</c>.
        /// </summary>
        public bool AllowSpecificGroupMemberWorkflows { get; set; }

        #endregion

        #region Section 7 — Inherited Group Requirements (read-only grid)

        /// <summary>
        /// Gets or sets the read-only inherited group-requirement rows
        /// for the current group type. Walks the
        /// <c>InheritedGroupTypeId</c> chain, mirroring the inheritance
        /// pattern used by <c>InheritedMemberAttributes</c> on
        /// <see cref="GroupBag"/>. Each row carries its own
        /// <c>InheritedFromGroupTypeName</c> / <c>InheritedFromGroupTypeUrl</c>
        /// so the grid can render per-row "(Inherited from {link})"
        /// cells. Empty when no ancestor group type defines a
        /// requirement.
        /// </summary>
        public List<InheritedGroupRequirementBag> InheritedGroupRequirements { get; set; }

        #endregion

        #region Section 7 / 9 / 10 dropdown sources

        /// <summary>
        /// Gets or sets the group requirement type dropdown options for
        /// the Section 7 modal. Each entry carries the type's
        /// <see cref="Rock.Model.DueDateType"/> so the modal's DueDate
        /// conditional well reacts to the selection without an extra
        /// round-trip.
        /// </summary>
        public List<GroupRequirementTypeBag> GroupRequirementTypeOptions { get; set; }

        /// <summary>
        /// Gets or sets the group role dropdown options for the
        /// Section 7 / 9 / 10 modals. <c>ListItemBag.value</c> is the
        /// GroupTypeRole Guid; <c>ListItemBag.text</c> is the role name.
        /// Sourced from <c>GroupType.Roles</c> on the active group type
        /// (no inheritance walk per WebForms parity).
        /// </summary>
        public List<ListItemBag> GroupRoleOptions { get; set; }

        /// <summary>
        /// Gets or sets the date-typed group attribute dropdown options
        /// for the Section 7 modal's Due Date Attribute conditional
        /// well. <c>ListItemBag.value</c> is the Attribute Guid. Sourced
        /// from the inherited attribute chain filtered to
        /// Date / DateTime field types.
        /// </summary>
        public List<ListItemBag> GroupAttributeOptions { get; set; }

        /// <summary>
        /// Gets or sets the system communication dropdown options for
        /// the Section 9 Welcome / Exit dropdowns.
        /// <c>ListItemBag.value</c> is the SystemCommunication Guid;
        /// <c>ListItemBag.text</c> is the communication title. Drives
        /// both Welcome and Exit dropdowns in the Sync modal.
        /// </summary>
        public List<ListItemBag> SystemCommunicationOptions { get; set; }

        #endregion

        #region Section 4 Stack 2 — Locations editing (Phase 6)

        /// <summary>
        /// Gets or sets the Location Type DefinedValue dropdown options
        /// scoped to <c>GroupType.LocationTypeValues</c>. Empty when the
        /// group type configures no location types.
        /// <c>ListItemBag.value</c> is the DefinedValue Guid;
        /// <c>ListItemBag.text</c> is the display value.
        /// </summary>
        public List<ListItemBag> LocationTypeValueOptions { get; set; }

        /// <summary>
        /// Gets or sets the map-style DefinedValue Guid sourced from
        /// the <c>MapStyle</c> block attribute. Carried on this cascade
        /// bag so the LocationPicker inside the modal can render the
        /// admin's configured map style without an extra round-trip on
        /// GroupType change. Mirrors WebForms parity at
        /// <c>GroupDetail.ascx.cs:3567</c>.
        /// </summary>
        public Guid? MapStyleValueGuid { get; set; }

        #endregion
    }
}
