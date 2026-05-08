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

using Rock.Model;

namespace Rock.ViewModels.Blocks.Group.GroupDetail
{
    /// <summary>
    /// One Meeting Location card on the right rail of the Group Detail
    /// view panel. Each <c>GroupLocation</c> on the group renders as one
    /// instance of this bag in <c>GroupBag.MeetingLocations</c>. The Vue
    /// layer renders the variants (Address / Point / Polygon / GroupMember)
    /// from the <see cref="Mode"/> field. See Phase 2 spec section C2 for
    /// the full field contract and Q2.5 for the WKT pass-through shape of
    /// <see cref="MapData"/>.
    /// </summary>
    public class GroupMeetingLocationBag
    {
        /// <summary>
        /// Gets or sets the <c>GroupLocation.Guid</c> for the row backing
        /// this card. Stable per-render identifier used as the Vue
        /// <c>v-for</c> key on the location list. Phase 6's editing modal
        /// will use this Guid to identify rows in the in-progress state
        /// collection (matching the WebForms <c>hfGroupLocationGuid</c>
        /// pattern at <c>research/webforms/07-locations-and-schedules.md</c>);
        /// new unsaved rows will be created with <c>Guid.NewGuid()</c>.
        /// </summary>
        public Guid Guid { get; set; }

        /// <summary>
        /// Gets or sets the optional display name from
        /// <c>GroupLocation.Location.Name</c>. May be null when the
        /// location has no Name (e.g., address-only or polygon-only
        /// locations); the Vue layer handles the null gracefully.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the multi-line formatted address text for the
        /// card. Null for polygon-style locations (which intentionally
        /// render no address) and null when the
        /// <c>ShowLocationAddresses</c> block attribute is false (per
        /// Q2.6 — suppresses address text uniformly across every card
        /// variant). Address-style cards source this from
        /// <c>Location.FormattedAddress</c>; member-address cards source
        /// from the family's home address.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets the friendly schedule text (e.g.,
        /// <em>"Saturday 4:00pm"</em>) sourced from the first associated
        /// schedule's <c>FriendlyScheduleText</c>. Null when no schedule
        /// is attached to this <c>GroupLocation</c>; the row hides in
        /// that case.
        /// </summary>
        public string ScheduleText { get; set; }

        /// <summary>
        /// Gets or sets the location-picker mode classification used to
        /// drive the per-card render variant on the Vue side. Address /
        /// Point / Polygon / GroupMember each render slightly differently
        /// (marker vs polygon, address text source, etc.). Mirrors the
        /// underlying <c>GroupLocation</c> classification described in
        /// research/webforms/07-locations-and-schedules.md.
        /// </summary>
        public GroupLocationPickerMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the raw Well-Known Text (WKT) geometry that the
        /// Vue map renderer passes through <c>wellKnownToCoordinates</c>
        /// from <c>@Obsidian/Utility/geo</c>. Format mirrors what
        /// <c>Location.GeoPoint</c> / <c>Location.GeoFence</c> store as
        /// <c>DbGeography</c> (e.g., <c>POINT(-112.130946 33.600114)</c>
        /// or <c>POLYGON((-112.157058 33.598563, ...))</c>). Empty
        /// string when the location has no geo data; the map still
        /// instantiates but no shape renders.
        /// </summary>
        public string MapData { get; set; }

        /// <summary>
        /// Gets or sets the URL the hover-expand button (or full-card tap
        /// on touch) navigates to. Per Q2.3 every card on a given group
        /// shares the same group-level URL pointing at
        /// <c>GroupMapPage?GroupId={IdKey}</c>; the field is per-card on
        /// the bag for forward-compat in case the destination later
        /// supports a per-location parameter.
        /// </summary>
        public string MapUrl { get; set; }
    }
}
