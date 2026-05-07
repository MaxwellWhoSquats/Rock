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
        /// Overview card. Always null in Phase 1 because the
        /// <c>Group.PhotoId</c> column ships in Phase 2 (per Q8). The
        /// Vue layer omits the hero region entirely when this is null.
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

        /// <summary>
        /// Gets or sets the value of the <em>Goal</em> group attribute, if
        /// configured. Rendered in the Overview card.
        /// </summary>
        public string GroupGoal { get; set; }

        /// <summary>
        /// Gets or sets the value of the <em>Neighborhood</em> group
        /// attribute, if configured.
        /// </summary>
        public string Neighborhood { get; set; }

        /// <summary>
        /// Gets or sets the value of the <em>Privacy</em> (private or
        /// public space) group attribute, if configured.
        /// </summary>
        public string Privacy { get; set; }

        /// <summary>
        /// Gets or sets the value of the <em>Group Preference</em> group
        /// attribute, if configured.
        /// </summary>
        public string GroupPreference { get; set; }

        #endregion

        #region Linkages

        /// <summary>
        /// Gets or sets the linkages section content (registrations, event
        /// item occurrences, content items). Null when the group has no
        /// linkages of any kind, which omits the section entirely.
        /// </summary>
        public GroupLinkagesBag Linkages { get; set; }

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
