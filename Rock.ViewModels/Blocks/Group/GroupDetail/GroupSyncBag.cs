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

using Rock.ViewModels.Utility;

namespace Rock.ViewModels.Blocks.Group.GroupDetail
{
    /// <summary>
    /// Represents a single Group Sync row in the Section 9 grid.
    /// Mirrors the WebForms <c>GroupSyncViewModel</c> shape at
    /// <c>GroupDetail.ascx.cs:5115</c> in bag form.
    /// </summary>
    public class GroupSyncBag
    {
        /// <summary>
        /// Gets or sets the unique identifier of the group sync.
        /// </summary>
        public Guid Guid { get; set; }

        /// <summary>
        /// Gets or sets the role members synced into this group are
        /// assigned. <c>ListItemBag.value</c> is the GroupTypeRole Guid;
        /// <c>ListItemBag.text</c> is the role name.
        /// </summary>
        public ListItemBag GroupTypeRole { get; set; }

        /// <summary>
        /// Gets or sets the data view that identifies group members.
        /// <c>ListItemBag.value</c> is the DataView Guid.
        /// </summary>
        public ListItemBag SyncDataView { get; set; }

        /// <summary>
        /// Gets or sets the welcome system communication sent on add.
        /// <c>ListItemBag.value</c> is the SystemCommunication Guid. Null
        /// to skip sending.
        /// </summary>
        public ListItemBag WelcomeSystemCommunication { get; set; }

        /// <summary>
        /// Gets or sets the exit system communication sent on removal.
        /// <c>ListItemBag.value</c> is the SystemCommunication Guid. Null
        /// to skip sending.
        /// </summary>
        public ListItemBag ExitSystemCommunication { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a Rock login account
        /// should be created for synced members that do not yet have one.
        /// </summary>
        public bool AddUserAccountsDuringSync { get; set; }

        /// <summary>
        /// Gets or sets the interval (in minutes) between sync runs.
        /// Bound to <c>&lt;IntervalPicker&gt;</c> in the modal.
        /// </summary>
        public int? ScheduleIntervalMinutes { get; set; }

        /// <summary>
        /// Gets or sets the date/time of the last successful sync run.
        /// Display only — populated by the Group Sync job, never written
        /// by this block.
        /// </summary>
        public DateTimeOffset? LastRefreshDateTime { get; set; }
    }
}
