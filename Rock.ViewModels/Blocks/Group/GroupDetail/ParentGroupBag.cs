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

namespace Rock.ViewModels.Blocks.Group.GroupDetail
{
    /// <summary>
    /// Reference to the group's parent group surfaced in the Overview
    /// card. Carries the parent's friendly name and a server-resolved
    /// link URL. The URL prefers the Group <c>EntityType.LinkUrlLavaTemplate</c>
    /// (so customer-customized link rules are honored) and falls back to
    /// <c>/Group/{IdKey}</c>.
    /// </summary>
    public class ParentGroupBag
    {
        /// <summary>
        /// Gets or sets the parent group's friendly name. Rendered as
        /// the link text.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the pre-resolved detail URL. Null / empty when
        /// no link can be constructed; the Vue layer falls through to
        /// rendering the name as plain text.
        /// </summary>
        public string Url { get; set; }
    }
}
