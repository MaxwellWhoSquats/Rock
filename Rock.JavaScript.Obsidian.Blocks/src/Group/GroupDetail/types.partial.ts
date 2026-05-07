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

/**
 * Keys for the outbound navigation URL map populated by the C# block in
 * `GetBoxNavigationUrls`. Each URL contains the literal `((Key))` token
 * which the Vue layer substitutes with the active group's IdKey at
 * render time per the IdKey policy in 00-architecture.md.
 */
export const enum NavigationUrlKey {
    AttendancePage = "AttendancePage",
    GroupSchedulerPage = "GroupSchedulerPage",
    GroupRSVPPage = "GroupRSVPPage",
    GroupPlacementPage = "GroupPlacementPage",
    GroupMapPage = "GroupMapPage",
    GroupHistoryPage = "GroupHistoryPage",
    FundraisingProgressPage = "FundraisingProgressPage",
    RegistrationInstancePage = "RegistrationInstancePage",
    EventItemOccurrencePage = "EventItemOccurrencePage",
    ContentItemPage = "ContentItemPage",
    GroupListPage = "GroupListPage"
}

/**
 * Names of the C# `[BlockAction]` methods on the Group Detail block.
 * Centralized here so the `invokeBlockAction` call sites and the
 * archive-prompt resolution type don't pass magic strings around. The
 * string values must exactly match the C# method names.
 */
export const enum BlockActionName {
    Edit = "Edit",
    Delete = "Delete",
    Archive = "Archive",
    ArchiveWithChildren = "ArchiveWithChildren",
    Copy = "Copy"
}
