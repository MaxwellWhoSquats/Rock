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

using System.Collections.Generic;

using Rock.ViewModels.Utility;

using RelationshipStrength = Rock.Enums.Group.RelationshipStrength;

namespace Rock.ViewModels.Blocks.Group.GroupDetail
{
    /// <summary>
    /// The bag returned by the Group Detail block. Phase 1 ships the
    /// view-mode fields. Phase 2 extends this bag with edit-mode scalar
    /// fields, GroupType cascade options, peer-network overrides, RSVP /
    /// Scheduling / Chat sections, and the new <c>Group.PhotoId</c>-driven
    /// uploader fields.
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
        /// visible. Drives the <em>Public</em> chip in the subheader.
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
        /// </summary>
        public string PhotoUrl { get; set; }

        /// <summary>
        /// Gets or sets the group's description text rendered below the
        /// image in the Overview card.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the group administrator reference. Null when no
        /// administrator is set or when <c>GroupType.ShowAdministrator</c>
        /// is false; the row is hidden in either case.
        /// </summary>
        public GroupAdministratorBag Administrator { get; set; }

        /// <summary>
        /// Gets or sets the parent group reference. Null when the group
        /// has no parent; the row is hidden in that case.
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
        /// which hides the row.
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
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the group is archived.
        /// Archived groups hide the Delete button.
        /// </summary>
        public bool IsArchived { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the group is a system
        /// group. System groups suppress Edit / Delete / Archive.
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

        #endregion
    }
}
