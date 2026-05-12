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
using Rock.ViewModels.Utility;

namespace Rock.ViewModels.Blocks.Group.GroupDetail
{
    /// <summary>
    /// Represents a single Group Requirement row in the Section 7 grid.
    /// Used by both the read-only "From Group Type" grid (sourced from
    /// <c>GroupTypeOptionsBag.GroupTypeRequirements</c>) and the editable
    /// "Specific Group Requirements" grid (sourced from
    /// <c>GroupBag.GroupRequirements</c>). Mirrors the canonical
    /// <c>GroupTypeGroupRequirementBag</c> sibling in
    /// <see cref="Rock.ViewModels.Blocks.Group.GroupTypeDetail"/>, scoped
    /// to per-group requirements.
    /// </summary>
    public class GroupRequirementBag
    {
        /// <summary>
        /// Gets or sets the unique identifier of the group requirement.
        /// </summary>
        public Guid Guid { get; set; }

        /// <summary>
        /// Gets or sets the group requirement type. <c>ListItemBag.value</c>
        /// is the GroupRequirementType Guid.
        /// </summary>
        public ListItemBag GroupRequirementType { get; set; }

        /// <summary>
        /// Gets or sets the group role this requirement applies to. Null
        /// applies the requirement to all roles. <c>ListItemBag.value</c>
        /// is the GroupTypeRole Guid.
        /// </summary>
        public ListItemBag Role { get; set; }

        /// <summary>
        /// Gets or sets the age classification to which this requirement
        /// applies. Default <c>All</c>.
        /// </summary>
        public AppliesToAgeClassification AppliesToAgeClassification { get; set; }

        /// <summary>
        /// Gets or sets the data view that determines who the requirement
        /// applies to. Null = applies to all members.
        /// </summary>
        public ListItemBag AppliesToDataView { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether leaders are allowed to
        /// override the requirement.
        /// </summary>
        public bool AllowLeadersToOverride { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether members must meet this
        /// requirement before being added. WebForms label "Required For
        /// New Members"; redesign label "Required Before Adding".
        /// </summary>
        public bool MustMeetRequirementToAddMember { get; set; }

        /// <summary>
        /// Gets or sets the due date type sourced from the selected
        /// <see cref="GroupRequirementType"/>. Drives which of
        /// <see cref="DueDateStaticDate"/> / <see cref="DueDateAttribute"/>
        /// the modal renders.
        /// </summary>
        public DueDateType DueDateType { get; set; }

        /// <summary>
        /// Gets or sets the static due date when
        /// <see cref="DueDateType"/> is <c>ConfiguredDate</c>. Null
        /// otherwise.
        /// </summary>
        public DateTimeOffset? DueDateStaticDate { get; set; }

        /// <summary>
        /// Gets or sets the group attribute holding the due date when
        /// <see cref="DueDateType"/> is <c>GroupAttribute</c>.
        /// <c>ListItemBag.value</c> is the Attribute Guid. Null otherwise.
        /// </summary>
        public ListItemBag DueDateAttribute { get; set; }
    }
}
