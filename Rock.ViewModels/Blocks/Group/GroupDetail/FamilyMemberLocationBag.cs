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
    /// One row in the Location modal's Member tab dropdown source. Maps
    /// a Group Member's family address to the underlying Location plus
    /// the primary PersonAlias for the member, so the save flow can
    /// stamp <c>GroupLocation.GroupMemberPersonAliasId</c> alongside
    /// <c>GroupLocation.LocationId</c>. Mirrors WebForms
    /// <c>ddlMember</c> rows at <c>GroupDetail.ascx.cs:3525-3549</c>.
    /// </summary>
    public class FamilyMemberLocationBag
    {
        /// <summary>
        /// Gets or sets the Guid of the underlying <c>Location</c> row.
        /// Resolved server-side to <c>Location.Id</c> when writing
        /// <c>GroupLocation.LocationId</c>.
        /// </summary>
        public Guid LocationGuid { get; set; }

        /// <summary>
        /// Gets or sets the primary <c>PersonAlias.Guid</c> for the
        /// member who owns this family address. Resolved server-side to
        /// <c>PersonAlias.Id</c> when writing
        /// <c>GroupLocation.GroupMemberPersonAliasId</c>.
        /// </summary>
        public Guid PersonAliasGuid { get; set; }

        /// <summary>
        /// Gets or sets the friendly dropdown text, formatted as
        /// <c>"{Member.FullName} {AddressType.Value} ({Address})"</c>
        /// per WebForms parity at <c>GroupDetail.ascx.cs:3540-3543</c>.
        /// </summary>
        public string Text { get; set; }
    }
}
