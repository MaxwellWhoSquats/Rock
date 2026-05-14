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

using Rock.Enums.Communication.Chat;
using Rock.Enums.Group;
using Rock.Model;
using Rock.Utility.Enums;
using Rock.ViewModels.Utility;

using RelationshipStrength = Rock.Enums.Group.RelationshipStrength;

namespace Rock.ViewModels.Blocks.Group.GroupDetail
{
    /// <summary>
    /// The bag returned by the Group Detail block. Phase 1 ships the
    /// view-mode fields, Phase 2 added the photo URL + view-mode
    /// notifications + meeting-locations payload, Phase 3 extends with
    /// edit-mode scalar fields covering Section 1 (Top fields), Section 2
    /// (General — Overview / Admin &amp; Security / Relationships stacks),
    /// Section 3 (RSVP), Section 4 Stacks 1 and 3 (Inline Schedule and
    /// Member Scheduling), and Section 8 (Chat). Sub-feature payloads
    /// (Attributes / Requirements / Sync / Triggers / Locations editing)
    /// land in Phases 4-6.
    /// </summary>
    public class GroupBag : EntityBagBase
    {
        #region Header chrome

        /// <summary>
        /// Gets or sets the friendly name of the group.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the icon CSS class sourced from the group type
        /// (e.g., <c>ti ti-users-group</c>) and used in the panel header.
        /// </summary>
        public string IconCssClass { get; set; }

        /// <summary>
        /// Gets or sets the group type reference rendered as a chip in
        /// the panel header. <see cref="GroupDetailGroupTypeBag.Url"/>
        /// is null when the current user lacks ADMINISTRATE on the group
        /// type; the Vue layer renders the chip as plain text in that
        /// case.
        /// </summary>
        public GroupDetailGroupTypeBag GroupType { get; set; }

        /// <summary>
        /// Gets or sets the campus name. Null when the group has no campus
        /// assigned, which hides the campus chip in the header.
        /// </summary>
        public string CampusName { get; set; }

        #endregion

        #region Subheader

        /// <summary>
        /// Gets or sets a value indicating whether the group is publicly
        /// visible. Drives the <em>Public</em> chip in the subheader and
        /// is editable from the General section ("Show Publicly").
        /// </summary>
        public bool IsPublic { get; set; }

        /// <summary>
        /// Gets or sets the effective relationship strength for the
        /// subheader chip: the group's override falling back to the group
        /// type default. Null when the group type lacks peer-network
        /// support (chip hides) or when the stored integer does not map
        /// to a known enum bucket. The Vue layer resolves the
        /// human-readable label via <c>RelationshipStrengthDescription</c>
        /// from the auto-generated TS enum so the mapping stays
        /// single-sourced.
        /// </summary>
        public RelationshipStrength? RelationshipStrength { get; set; }

        #endregion

        #region Overview body

        /// <summary>
        /// Gets or sets the URL of the 16:9 hero image at the top of the
        /// Overview card, populated from <c>Group.PhotoUrl</c> (which
        /// resolves <c>Group.PhotoId</c> through <c>FileUrlHelper</c>).
        /// Null when the group has no photo set; the Vue layer omits the
        /// hero region entirely in that case (no placeholder, per design).
        /// View-mode only.
        /// </summary>
        public string PhotoUrl { get; set; }

        /// <summary>
        /// Gets or sets the group's description text rendered below the
        /// image in the Overview card.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the group administrator reference. Inherits
        /// <c>Value</c> (PersonAlias Guid) and <c>Text</c> (friendly name)
        /// from <see cref="ListItemBag"/>; adds a server-resolved profile
        /// <c>Url</c> for the view-mode link. Bound to
        /// <c>&lt;PersonPicker&gt;</c> in edit mode (the picker reads
        /// only <c>Value</c> and <c>Text</c>; <c>Url</c> is repopulated
        /// on the next view-mode load). Null when no administrator is
        /// set or when <c>GroupType.ShowAdministrator</c> is false.
        /// </summary>
        public GroupAdministratorBag Administrator { get; set; }

        /// <summary>
        /// Gets or sets the parent group reference. Inherits <c>Value</c>
        /// (parent group Id as a string) and <c>Text</c> (friendly name)
        /// from <see cref="ListItemBag"/>; adds a server-resolved detail
        /// <c>Url</c> for the view-mode link. Bound to
        /// <c>&lt;GroupPicker&gt;</c> in edit mode. Null when the group
        /// has no parent.
        /// </summary>
        public ParentGroupBag ParentGroup { get; set; }

        /// <summary>
        /// Gets or sets the friendly schedule text for the group's primary
        /// schedule (e.g., <em>"Monday at 8:00pm"</em>). Null when the
        /// group has no schedule, which hides the row.
        /// </summary>
        public string ScheduleFriendlyText { get; set; }

        /// <summary>
        /// Gets or sets the group's capacity. Null when not configured,
        /// which hides the row in the view panel and keeps the input
        /// blank in edit mode.
        /// </summary>
        public int? GroupCapacity { get; set; }

        #endregion

        #region Linkages

        /// <summary>
        /// Gets or sets the linkages section content (registrations, event
        /// item occurrences, content items). Null when the group has no
        /// linkages of any kind, which omits the section entirely.
        /// </summary>
        public GroupLinkagesBag Linkages { get; set; }

        #endregion

        #region Meeting Locations

        /// <summary>
        /// Gets or sets the per-<c>GroupLocation</c> meeting location
        /// cards rendered on the right rail of the View panel. Always
        /// emitted; the Vue layer uses a <c>v-if</c> on
        /// <c>length &gt; 0</c> to omit the entire card when the group
        /// has no locations. Each entry renders as one 16:9 map card with
        /// optional address and schedule below.
        /// </summary>
        public List<GroupMeetingLocationBag> MeetingLocations { get; set; }

        #endregion

        #region View-mode Notifications

        /// <summary>
        /// Gets or sets the role-limit warning HTML rendered in the view
        /// panel's top notification surface when one or more
        /// <c>GroupTypeRole</c>s violate their <c>MinCount</c> /
        /// <c>MaxCount</c> against the group's current active member
        /// counts. Null / empty hides the notification. Populated from
        /// <c>Group.GetGroupTypeRoleLimitWarnings(out string)</c>;
        /// mirrors the WebForms <c>nbRoleLimitWarning</c> at
        /// <c>GroupDetail.ascx.cs:1849-1851</c>.
        /// </summary>
        public string RoleLimitWarning { get; set; }

        #endregion

        #region State

        /// <summary>
        /// Gets or sets a value indicating whether the group is active.
        /// Editable from the General section (drives the Inactive
        /// conditional well).
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the group is archived.
        /// Archived groups hide the Delete button.
        /// </summary>
        public bool IsArchived { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the group is a system
        /// group. System groups suppress Edit / Delete / Archive and
        /// disable the chat-channel-avatar uploader.
        /// </summary>
        public bool IsSystem { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the current user has
        /// EDIT auth on the group. Drives Edit / Delete / Archive / Copy
        /// visibility.
        /// </summary>
        public bool CanEdit { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the current user has
        /// ADMINISTRATE auth on the group. Drives the Security button.
        /// </summary>
        public bool CanAdministrate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the group has any child
        /// groups. Drives whether the Vue layer prompts the user to choose
        /// between archiving the group alone or cascading the archive to
        /// descendants. Mirrors the WebForms <c>btnArchive_Click</c>
        /// children check at <c>GroupDetail.ascx.cs:641</c>.
        /// </summary>
        public bool HasChildGroups { get; set; }

        /// <summary>
        /// Gets or sets the count of currently-active members of this
        /// group. Surfaced for the edit-mode capacity-below-members
        /// warning banner. Zero for new (unsaved) groups.
        /// </summary>
        public int MemberCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the current user is a
        /// member of the GROUP_ADMINISTRATORS system group. Drives the
        /// visibility of the "Enable as Security Role" checkbox in
        /// Section 2 Stack 2 per WebForms parity.
        /// </summary>
        public bool IsCurrentPersonGroupAdministrator { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the
        /// <c>LimittoSecurityRoleGroups</c> block attribute is enabled,
        /// surfaced so the edit panel can disable / force-check the
        /// "Enable as Security Role" checkbox per WebForms parity.
        /// </summary>
        public bool IsLimitedToSecurityRoleGroups { get; set; }

        #endregion

        #region Section 1 (Top fields) — edit-mode

        /// <summary>
        /// Gets or sets the inactive-reason DefinedValue Id assigned when
        /// the group is inactive. Null when the group is active or no
        /// reason was chosen. Persisted to <c>Group.InactiveReasonValueId</c>.
        /// </summary>
        public int? InactiveReasonValueId { get; set; }

        /// <summary>
        /// Gets or sets the free-text inactive note. Persisted to
        /// <c>Group.InactiveReasonNote</c>.
        /// </summary>
        public string InactiveReasonNote { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to cascade the inactive
        /// flag to all active descendant groups on save. UI-only flag (not
        /// persisted on the Group entity). When true and IsActive is being
        /// flipped to false, the save flow walks descendants and updates
        /// each.
        /// </summary>
        public bool InactivateChildGroups { get; set; }

        /// <summary>
        /// Gets or sets the BinaryFile reference for the Group hero image.
        /// The <c>ListItemBag.value</c> is the BinaryFile Guid; the
        /// <c>ListItemBag.text</c> is the file name. Bound to the
        /// <c>&lt;ImageUploader&gt;</c> in Section 1. Null when the group
        /// has no photo set. Save logic toggles <c>BinaryFile.IsTemporary</c>
        /// to mirror the chat-channel-avatar pattern.
        /// </summary>
        public ListItemBag PhotoBinaryFile { get; set; }

        #endregion

        #region Section 2 Stack 1 (Overview) — edit-mode

        /// <summary>
        /// Gets or sets the GroupType Id for the active edit session.
        /// Required on Add; read-only on existing groups (the WebForms
        /// block surfaces a label instead of the dropdown). Drives the
        /// reactive cascade via the <c>GetGroupTypeOptions</c> block
        /// action.
        /// </summary>
        public int? GroupTypeId { get; set; }

        /// <summary>
        /// Gets or sets the campus selected on the General section's
        /// Campus picker. <c>ListItemBag.value</c> is the campus Id (as
        /// a string); <c>ListItemBag.text</c> is the campus name. Null
        /// when no campus is selected. Resolved server-side to
        /// <c>Group.CampusId</c>. Honors <c>PreventSelectingInactiveCampus</c>
        /// block attribute. Distinct from <see cref="CampusName"/>, the
        /// header-chrome scalar.
        /// </summary>
        public ListItemBag Campus { get; set; }

        /// <summary>
        /// Gets or sets the Status DefinedValue Id selected on the
        /// General section's Status dropdown. Source list comes from
        /// <c>GroupTypeOptionsBag.StatusValues</c>. Null = no status.
        /// Persisted to <c>Group.StatusValueId</c>.
        /// </summary>
        public int? StatusValueId { get; set; }

        #endregion

        #region Section 2 Stack 2 (Admin &amp; Security) — edit-mode

        /// <summary>
        /// Gets or sets the Required Signature Document Template Id
        /// selected on the dropdown. Source list:
        /// <c>SignatureDocumentTemplateService.GetLegacyTemplates()</c>
        /// per webforms/05. Null clears the requirement. Persisted to
        /// <c>Group.RequiredSignatureDocumentTemplateId</c>.
        /// </summary>
        public int? RequiredSignatureDocumentTemplateId { get; set; }

        /// <summary>
        /// Gets or sets the Member Record Source selected on the
        /// dropdown. <c>ListItemBag.value</c> is the DefinedValue Id (as
        /// a string); <c>ListItemBag.text</c> is the value's display
        /// name. Source: the <c>RECORD_SOURCE_TYPE</c> defined type.
        /// Resolved server-side to
        /// <c>Group.GroupMemberRecordSourceValueId</c> only when
        /// <c>GroupType.AllowGroupSpecificRecordSource</c> is true; the
        /// save flow nulls this otherwise.
        /// </summary>
        public ListItemBag GroupMemberRecordSource { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the group is enabled
        /// as a security role. Visible only to GROUP_ADMINISTRATORS
        /// members per WebForms parity. Forced to true when
        /// <c>LimittoSecurityRoleGroups</c> block attribute is enabled.
        /// </summary>
        public bool IsSecurityRole { get; set; }

        /// <summary>
        /// Gets or sets the elevated-security level applied when the
        /// group is acting as a security role. Visible only when
        /// <see cref="IsSecurityRole"/> is true.
        /// </summary>
        public ElevatedSecurityLevel ElevatedSecurityLevel { get; set; }

        #endregion

        #region Section 2 Stack 3 (Relationships) — edit-mode

        /// <summary>
        /// Gets or sets a value indicating whether the user has chosen to
        /// override the group type's peer-network settings. UI-only flag;
        /// when false, the save flow nulls every override field below
        /// regardless of UI value.
        /// </summary>
        public bool OverrideRelationshipStrength { get; set; }

        /// <summary>
        /// Gets or sets the relationship-strength override (Casual / Close
        /// / Deep per the design rename map). Underlying enum integers are
        /// unchanged. Null = inherit from group type. Persisted to
        /// <c>Group.RelationshipStrengthOverride</c>.
        /// </summary>
        public RelationshipStrength? RelationshipStrengthOverride { get; set; }

        /// <summary>
        /// Gets or sets the relationship-growth-enabled override. Null =
        /// inherit from group type. Persisted to
        /// <c>Group.RelationshipGrowthEnabledOverride</c>.
        /// </summary>
        public bool? RelationshipGrowthEnabledOverride { get; set; }

        /// <summary>
        /// Gets or sets the leader-to-leader relationship multiplier
        /// override (0-1 decimal). Null = inherit from group type's
        /// <c>LeaderToLeaderRelationshipMultiplier</c>. Persisted to
        /// <c>Group.LeaderToLeaderRelationshipMultiplierOverride</c>.
        /// </summary>
        public decimal? LeaderToLeaderRelationshipMultiplierOverride { get; set; }

        /// <summary>
        /// Gets or sets the leader-to-non-leader relationship multiplier
        /// override (0-1 decimal). Null = inherit from group type.
        /// Persisted to
        /// <c>Group.LeaderToNonLeaderRelationshipMultiplierOverride</c>.
        /// </summary>
        public decimal? LeaderToNonLeaderRelationshipMultiplierOverride { get; set; }

        /// <summary>
        /// Gets or sets the non-leader-to-leader relationship multiplier
        /// override (0-1 decimal). Null = inherit from group type.
        /// Persisted to
        /// <c>Group.NonLeaderToLeaderRelationshipMultiplierOverride</c>.
        /// </summary>
        public decimal? NonLeaderToLeaderRelationshipMultiplierOverride { get; set; }

        /// <summary>
        /// Gets or sets the non-leader-to-non-leader relationship
        /// multiplier override (0-1 decimal). Null = inherit from group
        /// type. Persisted to
        /// <c>Group.NonLeaderToNonLeaderRelationshipMultiplierOverride</c>.
        /// </summary>
        public decimal? NonLeaderToNonLeaderRelationshipMultiplierOverride { get; set; }

        #endregion

        #region Section 3 (RSVP) — edit-mode

        /// <summary>
        /// Gets or sets the RSVP reminder offset days override. Null =
        /// inherit from group type's pinned value (read-only when the
        /// group type pins it). Persisted to
        /// <c>Group.RSVPReminderOffsetDays</c>.
        /// </summary>
        public int? RsvpReminderOffsetDays { get; set; }

        /// <summary>
        /// Gets or sets the RSVP reminder system communication override.
        /// <c>ListItemBag.value</c> is the SystemCommunication Guid;
        /// <c>ListItemBag.text</c> is the communication title. Null =
        /// inherit from group type's pinned value (read-only when the
        /// group type pins it). Resolved server-side to
        /// <c>Group.RSVPReminderSystemCommunicationId</c>.
        /// </summary>
        public ListItemBag RsvpReminderSystemCommunication { get; set; }

        #endregion

        #region Section 4 Stack 1 (Inline Schedule) — edit-mode

        /// <summary>
        /// Gets or sets the schedule type radio selection (None /
        /// Weekly / Custom / Named). Drives which sub-fields render and
        /// the inline-schedule lifecycle on save.
        /// </summary>
        public ScheduleType ScheduleType { get; set; }

        /// <summary>
        /// Gets or sets the day-of-week selected when
        /// <see cref="ScheduleType"/> is <c>Weekly</c>. Null otherwise.
        /// Persisted to <c>Schedule.WeeklyDayOfWeek</c> on the inline
        /// schedule. Stored as <see cref="int"/> rather than
        /// <see cref="DayOfWeek"/> because the Obsidian code-gen tool
        /// has no TypeScript mapping for the BCL <c>System.DayOfWeek</c>
        /// enum; the values still align 1:1 with the enum members
        /// (Sunday = 0 ... Saturday = 6).
        /// </summary>
        public int? WeeklyDayOfWeek { get; set; }

        /// <summary>
        /// Gets or sets the time-of-day selected when
        /// <see cref="ScheduleType"/> is <c>Weekly</c>. Serialized as
        /// the standard ISO-8601 time string ("HH:mm:ss"). Persisted to
        /// <c>Schedule.WeeklyTimeOfDay</c>.
        /// </summary>
        public string WeeklyTimeOfDay { get; set; }

        /// <summary>
        /// Gets or sets the iCalendar content emitted by the Schedule
        /// Builder when <see cref="ScheduleType"/> is <c>Custom</c>.
        /// Persisted to <c>Schedule.iCalendarContent</c>.
        /// </summary>
        public string ICalendarContent { get; set; }

        /// <summary>
        /// Gets or sets the named Schedule selected when
        /// <see cref="ScheduleType"/> is <c>Named</c>.
        /// <c>ListItemBag.value</c> is the Schedule Id (as a string);
        /// <c>ListItemBag.text</c> is the schedule's name. Null
        /// otherwise. Resolved server-side to <c>Group.ScheduleId</c>.
        /// </summary>
        public ListItemBag NamedSchedule { get; set; }

        #endregion

        #region Section 4 Stack 3 (Member Scheduling &amp; Check-in) — edit-mode

        /// <summary>
        /// Gets or sets a value indicating whether members must meet
        /// requirements to be schedulable. Persisted to
        /// <c>Group.SchedulingMustMeetRequirements</c>.
        /// </summary>
        public bool SchedulingMustMeetRequirements { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether group-member scheduling
        /// is disabled at the group level. Persisted to
        /// <c>Group.DisableScheduling</c>.
        /// </summary>
        public bool DisableScheduling { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the group is hidden
        /// from the Schedule Toolbox. Persisted to
        /// <c>Group.DisableScheduleToolboxAccess</c>.
        /// </summary>
        public bool DisableScheduleToolboxAccess { get; set; }

        /// <summary>
        /// Gets or sets the schedule confirmation logic (Ask /
        /// AutoAccept). Null = inherit. Persisted to
        /// <c>Group.ScheduleConfirmationLogic</c>.
        /// </summary>
        public ScheduleConfirmationLogic? ScheduleConfirmationLogic { get; set; }

        /// <summary>
        /// Gets or sets the schedule coordinator selected on the
        /// <c>&lt;PersonPicker&gt;</c>. <c>ListItemBag.value</c> is the
        /// PersonAlias Guid; <c>ListItemBag.text</c> is the person's
        /// friendly name. Null clears the coordinator. Resolved
        /// server-side to <c>Group.ScheduleCoordinatorPersonAliasId</c>.
        /// </summary>
        public ListItemBag ScheduleCoordinatorPerson { get; set; }

        /// <summary>
        /// Gets or sets whether this group explicitly overrides the
        /// GroupType's <c>ScheduleCoordinatorNotificationTypes</c>. When
        /// <c>false</c>, <see cref="ScheduleCoordinatorNotificationTypes"/>
        /// is ignored on save and the entity column is set to
        /// <c>null</c> (inherit). When <c>true</c>, the bitmask in
        /// <see cref="ScheduleCoordinatorNotificationTypes"/> is persisted
        /// (zero flags = explicit None override).
        /// </summary>
        public bool HasCoordinatorNotificationOverride { get; set; }

        /// <summary>
        /// Gets or sets the bitmask of coordinator notification types
        /// (Accept / Decline / SelfSchedule). Only persisted when
        /// <see cref="HasCoordinatorNotificationOverride"/> is <c>true</c>;
        /// an empty selection (zero flags set) means
        /// <see cref="Rock.Model.ScheduleCoordinatorNotificationType.None"/>.
        /// Persisted to <c>Group.ScheduleCoordinatorNotificationTypes</c>.
        /// </summary>
        public ScheduleCoordinatorNotificationType ScheduleCoordinatorNotificationTypes { get; set; }

        /// <summary>
        /// Gets or sets the attendance-record-required-for-check-in
        /// behavior. Visible only when
        /// <c>GroupType.TakesAttendance</c>. Persisted to
        /// <c>Group.AttendanceRecordRequiredForCheckIn</c>.
        /// </summary>
        public AttendanceRecordRequiredForCheckIn AttendanceRecordRequiredForCheckIn { get; set; }

        #endregion

        #region Section 8 (Chat) — edit-mode

        /// <summary>
        /// Gets or sets the chat-enabled override (null = inherit, true
        /// = yes, false = no). Persisted to
        /// <c>Group.IsChatEnabledOverride</c>.
        /// </summary>
        public bool? IsChatEnabledOverride { get; set; }

        /// <summary>
        /// Gets or sets the leaving-chat-channel-allowed override (null =
        /// inherit). Persisted to
        /// <c>Group.IsLeavingChatChannelAllowedOverride</c>.
        /// </summary>
        public bool? IsLeavingChatChannelAllowedOverride { get; set; }

        /// <summary>
        /// Gets or sets the public-channel override (null = inherit).
        /// Persisted to <c>Group.IsChatChannelPublicOverride</c>.
        /// </summary>
        public bool? IsChatChannelPublicOverride { get; set; }

        /// <summary>
        /// Gets or sets the always-show override (null = inherit).
        /// Persisted to <c>Group.IsChatChannelAlwaysShownOverride</c>.
        /// </summary>
        public bool? IsChatChannelAlwaysShownOverride { get; set; }

        /// <summary>
        /// Gets or sets the chat push-notification-mode override (null =
        /// inherit). Persisted to
        /// <c>Group.ChatPushNotificationModeOverride</c>.
        /// </summary>
        public ChatNotificationMode? ChatPushNotificationModeOverride { get; set; }

        /// <summary>
        /// Gets or sets the BinaryFile reference for the chat-channel
        /// avatar. The <c>ListItemBag.value</c> is the BinaryFile Guid;
        /// the <c>ListItemBag.text</c> is the file name. Bound to the
        /// <c>&lt;ImageUploader&gt;</c> in Section 8. Null when no avatar
        /// is set. Save logic toggles <c>BinaryFile.IsTemporary</c> per
        /// the chat-avatar pattern at webforms/14-chat.md.
        /// </summary>
        public ListItemBag ChatChannelAvatarBinaryFile { get; set; }

        /// <summary>
        /// Gets or sets the effective chat-enabled state for view-mode
        /// rendering. True only when the system chat feature is enabled
        /// AND <see cref="Rock.Model.Group.GetIsChatEnabled"/> resolves
        /// to true (group type allows chat AND the override / group-type
        /// default evaluate to true). Populated by
        /// <c>GetEntityBagForView</c>; left false for edit mode.
        /// Drives the "Chat-Enabled" view-mode label.
        /// </summary>
        public bool IsChatEnabled { get; set; }

        #endregion

        #region Section 6 (Group Member Attribute Definitions)

        /// <summary>
        /// Gets or sets the editable per-group member attribute
        /// definitions. Each entry is a full
        /// <see cref="PublicEditableAttributeBag"/> so the
        /// <c>&lt;AttributeEditor&gt;</c> modal can read and write every
        /// configurable field (key, name, description, field type,
        /// configuration values, categories, default value, etc.).
        /// Persisted by the Save action via
        /// <c>Rock.Attribute.Helper.SaveAttributeEdits</c> with
        /// <c>entityTypeId = GroupMember.TypeId</c>,
        /// <c>qualifierColumn = "GroupId"</c>, and
        /// <c>qualifierValue = group.Id.ToString()</c>. Mirrors the
        /// WebForms <c>GroupMemberAttributesState</c> at
        /// <c>GroupDetail.ascx.cs:1338-1357</c>.
        /// </summary>
        public List<PublicEditableAttributeBag> GroupMemberAttributes { get; set; }

        #endregion

        #region Section 7 (Group Requirements)

        /// <summary>
        /// Gets or sets the per-group requirements rendered in the
        /// editable "Specific Group Requirements" grid. The read-only
        /// "From Group Type" grid is sourced from
        /// <c>GroupTypeOptionsBag.GroupTypeRequirements</c> instead.
        /// Persisted by the Save action's step 4c using the
        /// <c>SyncRelatedEntities</c> pattern; new entries land in the
        /// queue and are <c>AddRange</c>'d after the group's Id is
        /// assigned (deferred-insert per
        /// <c>webforms/23-validations-and-cascades.md</c>).
        /// </summary>
        public List<GroupRequirementBag> GroupRequirements { get; set; }

        #endregion

        #region Section 9 (Group Sync)

        /// <summary>
        /// Gets or sets the per-group sync rules rendered in the
        /// Section 9 grid. Persisted by the Save action's step 4d using
        /// the <c>SyncRelatedEntities</c> pattern. Each entry maps a
        /// DataView to a role with a sync interval and optional
        /// welcome / exit communications.
        /// </summary>
        public List<GroupSyncBag> GroupSyncs { get; set; }

        #endregion

        #region Section 10 (Group Member Workflow Triggers)

        /// <summary>
        /// Gets or sets the per-group member workflow triggers rendered
        /// in the Section 10 grid. Persisted by the Save action's step
        /// 4e using the <c>SyncRelatedEntities</c> pattern. The
        /// <c>TypeQualifier</c> 7-tuple is built server-side from each
        /// bag's typed qualifier fields; the Vue layer never touches
        /// the raw qualifier string.
        /// </summary>
        public List<GroupMemberWorkflowTriggerBag> GroupMemberWorkflowTriggers { get; set; }

        #endregion

        #region Section 4 Stack 2 (Locations editing)

        /// <summary>
        /// Gets or sets the editable per-group meeting locations
        /// rendered in the Section 4 Stack 2 grid. Each entry carries
        /// the Location-picker emit (Q6.9 discriminator), the active
        /// schedules attached to it (Q6.3 inactive reconciliation runs
        /// server-side), and the capacity matrix configs. Persisted by
        /// the Save action's step 4f via the
        /// <c>SaveGroupLocations</c> helper inside
        /// <c>WrapTransaction</c>.
        /// </summary>
        public List<GroupLocationStateBag> GroupLocations { get; set; }

        /// <summary>
        /// Gets or sets the per-group dropdown source for the Location
        /// modal's Member tab. Built server-side via
        /// <c>BuildFamilyMemberLocationOptions</c> walking
        /// <c>GroupMemberService.GetByGroupId(groupId) →
        /// PersonService.GetFamilies(memberId) →
        /// family.GroupLocations.Where(IsMappedLocation &amp;&amp;
        /// !Previous)</c>. Per-group (not per-GroupType) so it lives on
        /// <see cref="GroupBag"/> instead of
        /// <see cref="GroupTypeOptionsBag"/> per Q6.11.
        /// </summary>
        public List<FamilyMemberLocationBag> FamilyMemberLocationOptions { get; set; }

        #endregion
    }
}
