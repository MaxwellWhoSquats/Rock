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

namespace Rock.ViewModels.Blocks.Group.GroupDetail
{
    /// <summary>
    /// Reference to a group administrator. Inherited <c>Value</c> carries
    /// the PersonAlias Guid (as a string) and <c>Text</c> carries the
    /// administrator's friendly name; the picker-shaped base lets the
    /// edit-mode <c>&lt;PersonPicker&gt;</c> bind to the same field. The
    /// view-mode Overview card additionally renders a link using the
    /// pre-resolved <see cref="Url"/>, which prefers the Person
    /// <c>EntityType.LinkUrlLavaTemplate</c> (so customer-customized
    /// link rules are honored) and falls back to <c>/Person/{IdKey}</c>.
    /// </summary>
    public class GroupAdministratorBag : ListItemBag
    {
        /// <summary>
        /// Gets or sets the pre-resolved profile URL. Null / empty when
        /// no link can be constructed (also during edit-mode emits, where
        /// the picker overwrites <c>Value</c>/<c>Text</c> without a URL);
        /// the Vue layer falls through to rendering the name as plain
        /// text in either case.
        /// </summary>
        public string Url { get; set; }
    }
}
