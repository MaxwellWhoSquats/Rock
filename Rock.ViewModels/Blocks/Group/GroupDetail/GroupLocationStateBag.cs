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

using Rock.Model;
using Rock.ViewModels.Utility;

namespace Rock.ViewModels.Blocks.Group.GroupDetail
{
    /// <summary>
    /// Represents a single editable Group Location row in the Section 4
    /// Stack 2 grid. Mirrors WebForms <c>GroupLocationsState</c> at
    /// <c>GroupDetail.ascx.cs:3525-3848</c> in bag form, with the
    /// LocationPicker emit shape locked per Q6.9
    /// (<see cref="SelectedLocation"/> + <see cref="SelectedLocationMode"/>
    /// discriminator) so server-side
    /// <see cref="LocationService"/> resolution happens once at save time.
    /// Active schedules only per Q6.3; inactive schedules are reconciled
    /// server-side via DB load + union at save time.
    /// </summary>
    public class GroupLocationStateBag
    {
        /// <summary>
        /// Gets or sets the unique identifier of the group location.
        /// New rows arrive with <see cref="Guid.Empty"/>; the save flow
        /// assigns a fresh Guid before persisting.
        /// </summary>
        public Guid Guid { get; set; }

        /// <summary>
        /// Gets or sets the friendly text used to render the Location
        /// column in the grid. Server-side build pulls
        /// <c>Location.ToString()</c> or the formatted family-member
        /// label so the grid never needs to refetch.
        /// </summary>
        public string LocationName { get; set; }

        /// <summary>
        /// Gets or sets the optional location description (rendered as a
        /// secondary cell or hover tooltip; not required by the design).
        /// </summary>
        public string LocationDescription { get; set; }

        /// <summary>
        /// Gets or sets the discriminator describing how
        /// <see cref="SelectedLocation"/> was emitted by the
        /// <c>&lt;LocationPicker&gt;</c> or the Member-tab dropdown.
        /// Drives the server-side <c>ResolveLocationFromBag</c> branch.
        /// </summary>
        public GroupLocationPickerMode SelectedLocationMode { get; set; }

        /// <summary>
        /// Gets or sets the raw picker emit. The runtime shape is
        /// determined by <see cref="SelectedLocationMode"/> per Q6.9:
        /// <see cref="ListItemBag"/> when Named or GroupMember (a
        /// <see cref="FamilyMemberLocationBag.LocationGuid"/> wrapper);
        /// <see cref="Rock.ViewModels.Controls.AddressControlBag"/> when
        /// Address; a Well-Known Text string when Point or Polygon.
        /// Typed as <see cref="object"/> so System.Text.Json can
        /// round-trip the heterogeneous shape; the server-side resolver
        /// branches on the discriminator and casts.
        /// </summary>
        public object SelectedLocation { get; set; }

        /// <summary>
        /// Gets or sets the Location Type DefinedValue Guid scoped to
        /// <c>GroupType.LocationTypeValues</c>. Resolved server-side to
        /// <c>GroupLocation.GroupLocationTypeValueId</c>.
        /// </summary>
        public Guid? GroupLocationTypeValueGuid { get; set; }

        /// <summary>
        /// Gets or sets the friendly name of the selected location type,
        /// used by the grid's Type column without a refetch.
        /// </summary>
        public string GroupLocationTypeValueName { get; set; }

        /// <summary>
        /// Gets or sets the active schedules attached to this
        /// <c>GroupLocation</c>. <c>ListItemBag.value</c> is the
        /// Schedule Guid; <c>ListItemBag.text</c> is the schedule's
        /// friendly name. Inactive schedules are excluded from the bag
        /// round-trip per Q6.3 and reconciled server-side.
        /// </summary>
        public List<ListItemBag> Schedules { get; set; } = new List<ListItemBag>();

        /// <summary>
        /// Gets or sets the PersonAlias Guid for the family member who
        /// owns this location. Non-null when the row was added via the
        /// Member tab; drives the Edit-mode default tab inference per
        /// Q6.13. Resolved server-side to
        /// <c>GroupLocation.GroupMemberPersonAliasId</c>.
        /// </summary>
        public Guid? GroupMemberPersonAliasGuid { get; set; }

        /// <summary>
        /// Gets or sets the per-schedule capacity matrix entries (one
        /// row per selected schedule). Persisted as
        /// <c>GroupLocationScheduleConfig</c> rows owned by the
        /// containing <c>GroupLocation</c>.
        /// </summary>
        public List<GroupLocationScheduleConfigBag> ScheduleConfigs { get; set; } = new List<GroupLocationScheduleConfigBag>();
    }
}
