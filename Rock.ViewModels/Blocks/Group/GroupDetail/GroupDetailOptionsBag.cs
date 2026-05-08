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

namespace Rock.ViewModels.Blocks.Group.GroupDetail
{
    /// <summary>
    /// Per-block options consumed by the Group Detail Vue layer. Carries
    /// the visibility flags driving the Group Tools card and the
    /// action-button visibility helpers used by the panel footer. The
    /// outbound URLs themselves live on <c>DetailBlockBox.NavigationUrls</c>
    /// (each containing the literal <c>((Key))</c> placeholder per Q4 /
    /// IdKey policy).
    /// </summary>
    public class GroupDetailOptionsBag
    {
        #region Group Tools visibility flags

        /// <summary>
        /// Gets or sets a value indicating whether the Attendance row
        /// renders in the Group Tools card. Combines the URL presence with
        /// <c>GroupType.TakesAttendance</c>.
        /// </summary>
        public bool IsAttendanceVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Scheduler row
        /// renders. Combines the URL presence with
        /// <c>GroupType.IsSchedulingEnabled</c>.
        /// </summary>
        public bool IsSchedulerVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the RSVP row renders.
        /// Combines the URL presence with <c>GroupType.EnableRSVP</c>.
        /// </summary>
        public bool IsRsvpVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Placement row
        /// renders. Driven by URL presence and a non-null group type.
        /// </summary>
        public bool IsPlacementVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Interactive Map row
        /// renders. Driven solely by URL presence; map renders for any
        /// group type when configured.
        /// </summary>
        public bool IsMapVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the History row
        /// renders. Combines the URL presence with
        /// <c>GroupType.EnableGroupHistory</c>.
        /// </summary>
        public bool IsHistoryVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Fundraising Progress
        /// row renders. Combines the URL presence with the group type
        /// being a Fundraising Opportunity (or descendant).
        /// </summary>
        public bool IsFundraisingVisible { get; set; }

        #endregion

        #region Action-button visibility

        /// <summary>
        /// Gets or sets a value indicating whether the Copy button shows
        /// in the panel footer. Combines the <c>ShowCopyButton</c> block
        /// attribute with EDIT auth.
        /// </summary>
        public bool IsCopyButtonShown { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Archive button is
        /// visible in place of Delete. Mirrors the WebForms condition:
        /// <c>!IsSystem &amp;&amp; !IsArchived &amp;&amp; EDIT &amp;&amp;
        /// EnableGroupHistory &amp;&amp; (groupHistorical || groupMemberHistorical
        /// rows exist)</c>.
        /// </summary>
        public bool IsArchiveVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Delete button is
        /// suppressed because Archive replaces it (i.e., GroupHistory is
        /// enabled and at least one history row exists).
        /// </summary>
        public bool ShouldShowArchiveInsteadOfDelete { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Delete button is
        /// shown in the panel footer. Mirrors the inverse of the Archive
        /// visibility condition (system, archived, no auth, or
        /// archive-replacement above all hide Delete).
        /// </summary>
        public bool IsDeleteVisible { get; set; }

        #endregion

        #region Other display flags

        /// <summary>
        /// Gets or sets a value indicating whether the tag list renders in
        /// the subheader. Combines the <c>EnableGroupTags</c> block
        /// attribute with <c>GroupType.EnableGroupTag</c>.
        /// </summary>
        public bool IsTagListShown { get; set; }

        /// <summary>
        /// Gets or sets the resolved DefinedValue Guid of the
        /// <c>MapStyle</c> block attribute. Passed to the Vue map renderer
        /// (<c>locationCard.partial.obs</c>) so <c>loadMapResources</c>
        /// from <c>@Obsidian/Utility/geo</c> can fetch the matching
        /// map-style settings via the geo-picker REST endpoint. Null when
        /// the block setting is unset, which falls back to the default
        /// Rock map style on the client.
        /// </summary>
        public Guid? MapStyleValueGuid { get; set; }

        #endregion
    }
}
