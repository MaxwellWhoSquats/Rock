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
    /// Reference to the group's group type surfaced as a chip in the
    /// panel header. Carries the type's friendly name and a server-resolved
    /// link URL. The URL prefers the GroupType <c>EntityType.LinkUrlLavaTemplate</c>
    /// (so customer-customized link rules are honored) and falls back to
    /// the <c>/page/GroupTypeDetail?GroupTypeId={IdKey}</c> admin route.
    /// Url is null when the current user lacks ADMINISTRATE on the group
    /// type, which renders the chip as plain text instead of a link.
    /// </summary>
    public class GroupDetailGroupTypeBag
    {
        /// <summary>
        /// Gets or sets the group type's friendly name. Rendered as the
        /// chip text.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the pre-resolved detail URL. Null when the user
        /// lacks ADMINISTRATE on the group type, or when no link can be
        /// constructed; the Vue layer renders the chip as plain text in
        /// either case.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the GroupType's configured color (hex or named).
        /// Surfaced so the Vue layer can tint the GroupType chip in the
        /// panel header. Empty / null leaves the chip in its default
        /// styling.
        /// </summary>
        public string Color { get; set; }
    }
}
