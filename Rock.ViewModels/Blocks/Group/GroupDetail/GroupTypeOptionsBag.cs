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
        /// Phase 6 will use this when wiring up Section 4 Stack 2 (the
        /// Locations grid + dialog); surfaced now so the cascade payload is
        /// complete and Phase 6 does not re-fetch.
        /// </summary>
        public GroupLocationPickerMode LocationSelectionMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether per-location schedule
        /// configuration is enabled. Phase 6 surface; surfaced now to
        /// complete the cascade payload. Mirrors
        /// <c>GroupType.EnableLocationSchedules</c>.
        /// </summary>
        public bool EnableLocationSchedules { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether scheduling is enabled
        /// at the group-type level. Drives the master switch for the
        /// Member Scheduling stack.
        /// </summary>
        public bool IsSchedulingEnabled { get; set; }

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
        /// and the per-group override is read-only. Null means the group
        /// can override.
        /// </summary>
        public Guid? RsvpReminderSystemCommunicationGuid { get; set; }

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

        #region Phase 4 surfacing (read here so Phase 4 doesn't re-fetch on cascade)

        /// <summary>
        /// Gets or sets the inherited member-attribute definitions for the
        /// active group type. Surfaced here so Phase 4's Section 6 surface
        /// can consume the cascade payload directly. Phase 3 does not
        /// render these; Phase 4 wires the Section 6 panel.
        /// </summary>
        public List<PublicAttributeBag> InheritedMemberAttributes { get; set; }

        #endregion
    }
}
