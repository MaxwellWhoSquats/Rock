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
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;

using Rock.Attribute;
using Rock.Communication.Chat;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Model.Groups.Group.Options;
using Rock.Security;
using Rock.ViewModels.Blocks;
using Rock.ViewModels.Blocks.Group.GroupDetail;
using Rock.ViewModels.Utility;
using Rock.Web;
using Rock.Web.Cache;

using RelationshipStrength = Rock.Enums.Group.RelationshipStrength;

namespace Rock.Blocks.Group
{
    /// <summary>
    /// Displays the details of a particular group. Phase 1 shipped the
    /// block shell, the redesigned pure-Vue view panel, the Audit Details
    /// modal, and the terminal actions (Delete, Archive, ArchiveWithChildren,
    /// Copy). Phase 2 closed the view-panel gaps (Group Image hero +
    /// Meeting Locations card). Phase 3 ships the edit panel core (Top
    /// fields + General + RSVP + Scheduling + Chat sections), the Save
    /// block action, the GroupType reactive cascade
    /// (<c>GetGroupTypeOptions</c>) per Q2 Approach B, the photo and
    /// chat-channel-avatar uploaders, the Add path with
    /// <c>?ParentGroupId=N</c> defaulting, and <c>?autoEdit=true</c>
    /// handling.
    /// </summary>
    [DisplayName( "Group Detail" )]
    [Category( "Groups" )]
    [Description( "Displays the details of the given group." )]
    [IconCssClass( "ti ti-users-group" )]
    [SupportedSiteTypes( Model.SiteType.Web )]

    #region Block Attributes

    [GroupTypesField(
        "Group Types Include",
        Description = "Select group types to show in this block. Leave all unchecked to show all group types.",
        IsRequired = false,
        Order = 0,
        Category = AttributeCategory.GeneralSettings,
        Key = AttributeKey.GroupTypes )]

    [GroupTypesField(
        "Group Types Exclude",
        Description = "Select group types to exclude from this block. Note that this setting is only effective if 'Group Types Include' has no specific group types selected.",
        IsRequired = false,
        Order = 1,
        Category = AttributeCategory.GeneralSettings,
        Key = AttributeKey.GroupTypesExclude )]

    [BooleanField(
        "Limit to Security Role Groups",
        Description = "Any group can be flagged as a security group (even if their group type is not a security role).  Should this block be limited to only show security role groups and group types?",
        DefaultBooleanValue = false,
        Order = 2,
        Category = AttributeCategory.GeneralSettings,
        Key = AttributeKey.LimittoSecurityRoleGroups )]

    [BooleanField(
        "Limit to Group Types that are shown in navigation",
        Description = "Limits the Group Type list to those that are configured to be 'shown in navigation'.",
        DefaultBooleanValue = false,
        Order = 3,
        Category = AttributeCategory.GeneralSettings,
        Key = AttributeKey.LimitToShowInNavigationGroupTypes )]

    [DefinedValueField(
        "Map Style",
        Description = "The style of map to use when displaying maps for this block.",
        DefinedTypeGuid = Rock.SystemGuid.DefinedType.MAP_STYLES,
        IsRequired = true,
        AllowMultiple = false,
        DefaultValue = Rock.SystemGuid.DefinedValue.MAP_STYLE_ROCK,
        Order = 4,
        Category = AttributeCategory.GeneralSettings,
        Key = AttributeKey.MapStyle )]

    [BooleanField(
        "Show Copy Button",
        Description = "Set this to true to show a Copy button at the bottom of the panel that will allow you to copy this group (with options for copying child groups).",
        DefaultBooleanValue = false,
        Order = 5,
        Category = AttributeCategory.GeneralSettings,
        Key = AttributeKey.ShowCopyButton )]

    [BooleanField(
        "Show Location Addresses",
        Description = "Determines if the location address should be shown when viewing the group details.",
        DefaultBooleanValue = true,
        Order = 6,
        Category = AttributeCategory.GeneralSettings,
        Key = AttributeKey.ShowLocationAddresses )]

    [BooleanField(
        "Prevent Selecting Inactive Campus",
        Description = "Should inactive campuses be excluded from the campus field when editing a group?",
        DefaultBooleanValue = false,
        Order = 7,
        Category = AttributeCategory.GeneralSettings,
        Key = AttributeKey.PreventSelectingInactiveCampus )]

    [BooleanField(
        "Enable Group Tags",
        Description = "If enabled, the tags will be shown.",
        DefaultBooleanValue = true,
        Order = 8,
        Category = AttributeCategory.GeneralSettings,
        Key = AttributeKey.EnableGroupTags )]

    [BooleanField(
        "Add Administrate Security to Group Creator",
        Description = "If enabled, the person who created the group will be granted 'Administrate' security rights to the group.  This was the default behavior in previous versions of Rock.  If disabled, the group creator will not be granted 'Administrate' security rights to the group.",
        DefaultBooleanValue = false,
        Order = 9,
        Category = AttributeCategory.GeneralSettings,
        Key = AttributeKey.AddAdministrateSecurityToGroupCreator )]

    [LinkedPage(
        "Group Map Page",
        Description = "The page to display detailed group map.",
        IsRequired = false,
        Order = 10,
        Category = AttributeCategory.PageRouting,
        Key = AttributeKey.GroupMapPage )]

    [LinkedPage(
        "Attendance Page",
        Description = "The page to display attendance list.",
        IsRequired = false,
        Order = 11,
        Category = AttributeCategory.PageRouting,
        Key = AttributeKey.AttendancePage )]

    [LinkedPage(
        "Registration Instance Page",
        Description = "The page to display registration details.",
        IsRequired = false,
        Order = 12,
        Category = AttributeCategory.PageRouting,
        Key = AttributeKey.RegistrationInstancePage )]

    [LinkedPage(
        "Event Item Occurrence Page",
        Description = "The page to display event item occurrence details.",
        IsRequired = false,
        Order = 13,
        Category = AttributeCategory.PageRouting,
        Key = AttributeKey.EventItemOccurrencePage )]

    [LinkedPage(
        "Content Item Page",
        Description = "The page to display registration details.",
        IsRequired = false,
        Order = 14,
        Category = AttributeCategory.PageRouting,
        Key = AttributeKey.ContentItemPage )]

    [LinkedPage(
        "Group List Page",
        Description = "The page to display related Group List.",
        IsRequired = false,
        Order = 15,
        Category = AttributeCategory.PageRouting,
        Key = AttributeKey.GroupListPage )]

    [LinkedPage(
        "Fundraising Progress Page",
        Description = "The page to display fundraising progress for all its members.",
        IsRequired = false,
        Order = 16,
        Category = AttributeCategory.PageRouting,
        Key = AttributeKey.FundraisingProgressPage )]

    [LinkedPage(
        "Group History Page",
        Description = "The page to display group history.",
        IsRequired = false,
        Order = 17,
        Category = AttributeCategory.PageRouting,
        Key = AttributeKey.GroupHistoryPage )]

    [LinkedPage(
        "Group Scheduler Page",
        Description = "The page to schedule the group's resources.",
        DefaultValue = "1815D8C6-7C4A-4C05-A810-CF23BA937477,D0F198E2-6111-4EC1-8D1D-55AC10E28D04",
        IsRequired = false,
        Order = 18,
        Category = AttributeCategory.PageRouting,
        Key = AttributeKey.GroupSchedulerPage )]

    [LinkedPage(
        "Group RSVP List Page",
        Description = "The page to manage RSVPs for the group.",
        DefaultValue = Rock.SystemGuid.Page.GROUP_RSVP_LIST,
        IsRequired = false,
        Order = 19,
        Category = AttributeCategory.PageRouting,
        Key = AttributeKey.GroupRSVPPage )]

    [LinkedPage(
        "Group Placement Page",
        Description = "The page used for managing group placements.",
        IsRequired = false,
        Order = 20,
        Category = AttributeCategory.PageRouting,
        Key = AttributeKey.GroupPlacementPage )]

    #endregion Block Attributes

    [Rock.SystemGuid.EntityTypeGuid( "A0F8323D-B1A3-4DED-A6F6-B2483C0917D2" )]
    // was [Rock.SystemGuid.BlockTypeGuid( "87EEE2AD-F876-4827-9327-280FB1FDA1D1" )]
    [Rock.SystemGuid.BlockTypeGuid( "582BEEA1-5B27-444D-BC0A-F60CEB053981" )]
    public class GroupDetail : RockEntityDetailBlockType<Model.Group, GroupBag>, IBreadCrumbBlock
    {
        #region Keys

        private static class PageParameterKey
        {
            public const string GroupId = "GroupId";
            public const string ParentGroupId = "ParentGroupId";
            public const string ExpandedIds = "ExpandedIds";
            public const string ReturnUrl = "returnUrl";
        }

        private static class NavigationUrlKey
        {
            public const string AttendancePage = "AttendancePage";
            public const string GroupSchedulerPage = "GroupSchedulerPage";
            public const string GroupRSVPPage = "GroupRSVPPage";
            public const string GroupPlacementPage = "GroupPlacementPage";
            public const string GroupMapPage = "GroupMapPage";
            public const string GroupHistoryPage = "GroupHistoryPage";
            public const string FundraisingProgressPage = "FundraisingProgressPage";
            public const string RegistrationInstancePage = "RegistrationInstancePage";
            public const string EventItemOccurrencePage = "EventItemOccurrencePage";
            public const string ContentItemPage = "ContentItemPage";
            public const string GroupListPage = "GroupListPage";
        }

        private static class AttributeKey
        {
            public const string GroupTypes = "GroupTypes";
            public const string GroupTypesExclude = "GroupTypesExclude";
            public const string LimittoSecurityRoleGroups = "LimittoSecurityRoleGroups";
            public const string LimitToShowInNavigationGroupTypes = "LimitToShowInNavigationGroupTypes";
            public const string MapStyle = "MapStyle";
            public const string GroupMapPage = "GroupMapPage";
            public const string AttendancePage = "AttendancePage";
            public const string RegistrationInstancePage = "RegistrationInstancePage";
            public const string EventItemOccurrencePage = "EventItemOccurrencePage";
            public const string ContentItemPage = "ContentItemPage";
            public const string ShowCopyButton = "ShowCopyButton";
            public const string GroupListPage = "GroupListPage";
            public const string FundraisingProgressPage = "FundraisingProgressPage";
            public const string ShowLocationAddresses = "ShowLocationAddresses";
            public const string PreventSelectingInactiveCampus = "PreventSelectingInactiveCampus";
            public const string GroupHistoryPage = "GroupHistoryPage";
            public const string GroupSchedulerPage = "GroupSchedulerPage";
            public const string GroupRSVPPage = "GroupRSVPPage";
            public const string EnableGroupTags = "EnableGroupTags";
            public const string AddAdministrateSecurityToGroupCreator = "AddAdministrateSecurityToGroupCreator";
            public const string GroupPlacementPage = "GroupPlacementPage";
        }

        private static class AttributeCategory
        {
            public const string GeneralSettings = "General Settings";
            public const string PageRouting = "Page Routing";
        }

        #endregion Keys

        #region Fields

        /// <summary>
        /// Per-request memo for the active <see cref="GroupTypeCache"/>.
        /// Resolved lazily by <see cref="GetGroupTypeCache(Model.Group)"/>
        /// and self-invalidated when the entity's <c>GroupTypeId</c>
        /// changes mid-request (e.g., during <c>UpdateEntityFromBox</c>'s
        /// GroupTypeId reassignment).
        /// </summary>
        private GroupTypeCache _cachedGroupType;

        #endregion Fields

        #region Methods

        /// <inheritdoc/>
        public override object GetObsidianBlockInitialization()
        {
            var box = new DetailBlockBox<GroupBag, GroupDetailOptionsBag>();
            var entity = GetInitialEntity();

            SetBoxInitialEntityState( box, entity );

            box.NavigationUrls = GetBoxNavigationUrls();
            box.Options = GetBoxOptions( entity, box.NavigationUrls );

            return box;
        }

        /// <inheritdoc/>
        protected override Model.Group GetInitialEntity()
        {
            var entity = GetInitialEntity<Model.Group, GroupService>( RockContext, PageParameterKey.GroupId );

            ApplyNewGroupDefaultValues( entity );

            return entity;
        }

        /// <summary>
        /// Sets the initial entity state on the box. Mirrors the standard
        /// shape used by RockEntityDetailBlockType blocks: gate visibility
        /// on VIEW/EDIT auth and either populate
        /// <see cref="DetailBlockBox{TBag, TOptions}.Entity"/> or set
        /// <see cref="DetailBlockBox{TBag, TOptions}.ErrorMessage"/>.
        /// </summary>
        private void SetBoxInitialEntityState( DetailBlockBox<GroupBag, GroupDetailOptionsBag> box, Model.Group entity )
        {
            if ( entity == null )
            {
                box.ErrorMessage = $"The {Model.Group.FriendlyTypeName} was not found.";
                return;
            }

            var isViewable = entity.IsAuthorized( Authorization.VIEW, RequestContext.CurrentPerson );
            box.IsEditable = entity.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson );

            if ( entity.Id != 0 )
            {
                if ( isViewable )
                {
                    box.Entity = GetEntityBagForView( entity );
                }
                else
                {
                    box.ErrorMessage = EditModeMessage.NotAuthorizedToView( Model.Group.FriendlyTypeName );
                }
            }
            else
            {
                // New entity is being created, prepare for edit mode by default.
                if ( box.IsEditable )
                {
                    box.Entity = GetEntityBagForEdit( entity );
                }
                else
                {
                    box.ErrorMessage = EditModeMessage.NotAuthorizedToEdit( Model.Group.FriendlyTypeName );
                }
            }

            PrepareDetailBox( box, entity );
        }

        /// <summary>
        /// Builds the per-block <see cref="GroupDetailOptionsBag"/> for
        /// the active group: the visibility flags driving the Group Tools
        /// card and the action-button visibility helpers. Outbound URLs
        /// themselves live on <c>DetailBlockBox.NavigationUrls</c>; this
        /// method consults that dictionary only to know whether a given
        /// LinkedPage attribute is configured.
        /// </summary>
        private GroupDetailOptionsBag GetBoxOptions( Model.Group entity, Dictionary<string, string> navigationUrls )
        {
            var options = new GroupDetailOptionsBag
            {
                PreventSelectingInactiveCampus = GetAttributeValue( AttributeKey.PreventSelectingInactiveCampus ).AsBoolean(),
                AllowedGroupTypes = BuildAllowedGroupTypeListItems( entity ),
                SignatureDocumentTemplates = BuildSignatureDocumentTemplateListItems( entity )
            };

            var groupType = GetGroupTypeCache( entity );

            if ( entity == null || entity.Id == 0 || groupType == null )
            {
                return options;
            }

            // Group Tools visibility.
            bool IsToolVisible( string urlKey, bool gateCondition )
                => navigationUrls.TryGetValue( urlKey, out var url )
                    && url.IsNotNullOrWhiteSpace()
                    && gateCondition;

            options.IsAttendanceVisible  = IsToolVisible( NavigationUrlKey.AttendancePage,          groupType.TakesAttendance );
            options.IsSchedulerVisible   = IsToolVisible( NavigationUrlKey.GroupSchedulerPage,      groupType.IsSchedulingEnabled );
            options.IsRsvpVisible        = IsToolVisible( NavigationUrlKey.GroupRSVPPage,           groupType.EnableRSVP );
            options.IsPlacementVisible   = IsToolVisible( NavigationUrlKey.GroupPlacementPage,      true );
            options.IsMapVisible         = IsToolVisible( NavigationUrlKey.GroupMapPage,            true );
            options.IsHistoryVisible     = IsToolVisible( NavigationUrlKey.GroupHistoryPage,        groupType.EnableGroupHistory );
            options.IsFundraisingVisible = IsToolVisible( NavigationUrlKey.FundraisingProgressPage, IsFundraisingGroupType( entity.GroupTypeId ) );

            var canEdit = entity.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson );
            options.IsCopyButtonShown = GetAttributeValue( AttributeKey.ShowCopyButton ).AsBoolean() && canEdit;

            /*
                5/9/2026 - CLAUDE

                Two separate EXISTS queries against GroupHistorical and
                GroupMemberHistorical. Mirrors the WebForms check at
                GroupDetail.ascx.cs:2582-2583 verbatim. Combining into a
                single query (UNION ALL with a TOP 1) doesn't yield a
                cleaner shape in EF and the EnableGroupHistory short-
                circuit already prevents the queries from running on
                groups whose group type has history disabled.

                Reason: matches WebForms parity; the two-query shape is
                acceptable given the EnableGroupHistory short-circuit.
            */
            var hasHistory = groupType.EnableGroupHistory
                && (
                    new GroupHistoricalService( RockContext ).Queryable().Any( a => a.GroupId == entity.Id )
                    || new GroupMemberHistoricalService( RockContext ).Queryable().Any( a => a.GroupId == entity.Id )
                );

            // Archive replaces Delete when the group type has history enabled and at least one
            // history row exists for the group
            options.ShouldShowArchiveInsteadOfDelete = hasHistory;
            options.IsArchiveVisible = !entity.IsSystem && !entity.IsArchived && canEdit && hasHistory;
            options.IsDeleteVisible  = !entity.IsSystem && !entity.IsArchived && canEdit && !hasHistory;

            options.IsTagListShown = GetAttributeValue( AttributeKey.EnableGroupTags ).AsBoolean() && groupType.EnableGroupTag;
            options.MapStyleValueGuid = GetAttributeValue( AttributeKey.MapStyle ).AsGuidOrNull();

            return options;
        }

        /// <summary>
        /// Builds the read-only / view-mode bag fields that are common
        /// across both view and edit modes. Edit mode extends in
        /// <see cref="GetEntityBagForEdit(Model.Group)"/>; view mode
        /// extends in <see cref="GetEntityBagForView(Model.Group)"/>.
        /// </summary>
        /// <remarks>
        /// 5/9/2026 - CLAUDE
        ///
        /// Several navigation accesses in this method
        /// (<c>entity.Schedule</c>, <c>entity.GroupAdministratorPersonAlias.Person</c>,
        /// <c>entity.ParentGroup</c>, <c>entity.Photo</c>) trigger lazy
        /// loads — one round-trip each. WebForms <c>GetGroup</c> at
        /// GroupDetail.ascx.cs:2913 follows the same lazy-load pattern,
        /// so this is intentional parity and not a bug. The block's
        /// page-load path does ~5-6 SQL round-trips per view render.
        ///
        /// A future optimization is to override <c>GetInitialEntity</c>
        /// (or query directly with explicit <c>.Include(...)</c> calls)
        /// to collapse these into a single round-trip. Deferred — touch
        /// when Phase 5 / 6 work is already in this neighborhood.
        ///
        /// Reason: lazy-load matches WebForms parity; eager-load is a
        /// future optimization opportunity.
        /// </remarks>
        private GroupBag GetCommonEntityBag( Model.Group entity )
        {
            if ( entity == null )
            {
                return null;
            }

            if ( entity.Attributes == null )
            {
                entity.LoadAttributes( RockContext );
            }

            var groupType = GetGroupTypeCache( entity );
            var canEdit = entity.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson );
            var canAdministrate = entity.IsAuthorized( Authorization.ADMINISTRATE, RequestContext.CurrentPerson );

            /*
                5/9/2026 - CLAUDE

                The bag's HasChildGroups field is used by the Vue layer
                in two places with subtly different semantic needs:
                  1. Archive cascade prompt (groupDetail.obs) — needs
                     "any descendant exists, regardless of state".
                  2. Inactivate-children checkbox visibility
                     (editPanel.partial.obs) — needs "any ACTIVE
                     descendant exists" (matching the cascade's own
                     `GetAllDescendentGroupIds(includeInactive: false)`
                     filter).

                The WebForms block uses two distinct queries: an
                immediate-children-any-state check at
                GroupDetail.ascx.cs:641 (Archive prompt) and a recursive
                active-descendants check at GroupDetail.ascx.cs:1989
                (Inactivate cascade).

                The single-field shape collapses these into one. The
                immediate-children-any-state semantic chosen here is
                correct for #1 and an over-approximation for #2 — the
                Inactivate-children checkbox may render in the narrow
                case where a group has only inactive direct descendants;
                toggling it is harmless (the cascade no-ops) but
                slightly confusing UX. Splitting into two bag fields is
                the right long-term shape; deferred until Phase 5 since
                Phase 5 touches the cascade neighborhood.

                Reason: deliberate one-field shape with documented
                imperfect parity for the Inactivate prompt.
            */
            var hasChildGroups = entity.Id > 0 && new GroupService( RockContext ).Queryable().Any( g => g.ParentGroupId == entity.Id );

            return new GroupBag
            {
                IdKey = entity.IdKey,
                IsActive = entity.IsActive,
                IsSystem = entity.IsSystem,
                IsArchived = entity.IsArchived,
                CanEdit = canEdit,
                CanAdministrate = canAdministrate,
                HasChildGroups = hasChildGroups,

                // Header chrome.
                Name = entity.Name,
                IconCssClass = groupType?.IconCssClass,
                GroupType = BuildGroupTypeRef( entity.GroupType ),
                CampusName = entity.CampusId.HasValue ? CampusCache.Get( entity.CampusId.Value )?.Name : null,

                // Subheader.
                IsPublic = entity.IsPublic,
                RelationshipStrength = GetRelationshipStrength( entity, groupType ),

                // Overview body. PhotoUrl resolves through the entity's
                // computed property which mirrors Person.PhotoUrl shape
                // and returns null when PhotoId is null (see Q2.2 in the
                // Phase 2 spec). Null PhotoUrl hides the hero region in
                // the Vue partial.
                PhotoUrl = entity.PhotoUrl,
                Description = entity.Description,
                Administrator = BuildAdministratorRef( entity.GroupAdministratorPersonAlias, groupType ),
                ParentGroup = BuildParentGroupRef( entity.ParentGroup ),
                ScheduleFriendlyText = entity.Schedule?.FriendlyScheduleText,
                GroupCapacity = entity.GroupCapacity
            };
        }

        /// <inheritdoc/>
        protected override GroupBag GetEntityBagForView( Model.Group entity )
        {
            if ( entity == null )
            {
                return null;
            }

            var bag = GetCommonEntityBag( entity );
            bag.Linkages = BuildLinkages( entity );
            bag.MeetingLocations = BuildMeetingLocations( entity );

            if ( entity.GetGroupTypeRoleLimitWarnings( out var roleLimitWarning ) )
            {
                bag.RoleLimitWarning = roleLimitWarning;
            }

            bag.LoadAttributesAndValuesForPublicView( entity, RequestContext.CurrentPerson, enforceSecurity: true );

            return bag;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Phase 3 returns the full edit-mode bag: scalar fields covering
        /// Section 1 (Top fields), Section 2 (General — Overview / Admin
        /// &amp; Security / Relationships stacks), Section 3 (RSVP),
        /// Section 4 Stacks 1 + 3 (Inline Schedule + Member Scheduling),
        /// and Section 8 (Chat). The <c>Edit</c> block action also
        /// pre-populates <c>ParentGroupId</c> on the Add path from the
        /// <c>?ParentGroupId=N</c> page parameter (per A1).
        /// </remarks>
        protected override GroupBag GetEntityBagForEdit( Model.Group entity )
        {
            if ( entity == null )
            {
                return null;
            }

            var bag = GetCommonEntityBag( entity );

            bag.IsCurrentPersonGroupAdministrator = IsCurrentPersonGroupAdministrator();
            bag.IsLimitedToSecurityRoleGroups = GetAttributeValue( AttributeKey.LimittoSecurityRoleGroups ).AsBoolean();

            // Section 1 — Inactive flow + photo.
            bag.InactiveReasonValueId = entity.InactiveReasonValueId;
            bag.InactiveReasonNote = entity.InactiveReasonNote;
            bag.InactivateChildGroups = false;
            bag.PhotoBinaryFile = BuildBinaryFileRef( entity.Photo, entity.PhotoId );

            // Section 2 Stack 1 — Overview.
            bag.GroupTypeId = entity.GroupTypeId > 0 ? entity.GroupTypeId : ( int? ) null;
            bag.Campus = BuildCampusListItem( entity.CampusId );
            bag.StatusValueId = entity.StatusValueId;

            // Section 2 Stack 2 — Admin & Security.
            bag.RequiredSignatureDocumentTemplateId = entity.RequiredSignatureDocumentTemplateId;
            bag.GroupMemberRecordSource = BuildDefinedValueListItem( entity.GroupMemberRecordSourceValueId );
            bag.IsSecurityRole = entity.IsSecurityRole;
            bag.ElevatedSecurityLevel = entity.ElevatedSecurityLevel;

            // Section 2 Stack 3 — Relationships.
            bag.OverrideRelationshipStrength = entity.IsOverridingGroupTypePeerNetworkConfiguration;
            bag.RelationshipStrengthOverride = entity.RelationshipStrengthOverride.HasValue
                ? ( RelationshipStrength? ) entity.RelationshipStrengthOverride.Value
                : null;
            bag.RelationshipGrowthEnabledOverride = entity.RelationshipGrowthEnabledOverride;
            bag.LeaderToLeaderRelationshipMultiplierOverride = entity.LeaderToLeaderRelationshipMultiplierOverride;
            bag.LeaderToNonLeaderRelationshipMultiplierOverride = entity.LeaderToNonLeaderRelationshipMultiplierOverride;
            bag.NonLeaderToLeaderRelationshipMultiplierOverride = entity.NonLeaderToLeaderRelationshipMultiplierOverride;
            bag.NonLeaderToNonLeaderRelationshipMultiplierOverride = entity.NonLeaderToNonLeaderRelationshipMultiplierOverride;

            // Section 3 — RSVP.
            bag.RsvpReminderOffsetDays = entity.RSVPReminderOffsetDays;
            bag.RsvpReminderSystemCommunicationGuid = entity.RSVPReminderSystemCommunicationId.HasValue
                ? new SystemCommunicationService( RockContext ).GetSelect( entity.RSVPReminderSystemCommunicationId.Value, c => ( Guid? ) c.Guid )
                : null;

            // Section 4 Stack 1 — Inline Schedule.
            HydrateScheduleFields( bag, entity );

            // Section 4 Stack 3 — Member Scheduling.
            bag.SchedulingMustMeetRequirements = entity.SchedulingMustMeetRequirements;
            bag.DisableScheduling = entity.DisableScheduling;
            bag.DisableScheduleToolboxAccess = entity.DisableScheduleToolboxAccess;
            bag.ScheduleConfirmationLogic = entity.ScheduleConfirmationLogic;
            bag.ScheduleCoordinatorPerson = BuildPersonAliasListItemBag( entity.ScheduleCoordinatorPersonAlias );
            bag.ScheduleCoordinatorNotificationTypes = entity.ScheduleCoordinatorNotificationTypes ?? ScheduleCoordinatorNotificationType.None;
            bag.AttendanceRecordRequiredForCheckIn = entity.AttendanceRecordRequiredForCheckIn;

            // Section 8 — Chat.
            bag.IsChatEnabledOverride = entity.IsChatEnabledOverride;
            bag.IsLeavingChatChannelAllowedOverride = entity.IsLeavingChatChannelAllowedOverride;
            bag.IsChatChannelPublicOverride = entity.IsChatChannelPublicOverride;
            bag.IsChatChannelAlwaysShownOverride = entity.IsChatChannelAlwaysShownOverride;
            bag.ChatPushNotificationModeOverride = entity.ChatPushNotificationModeOverride;
            bag.ChatChannelAvatarBinaryFile = BuildBinaryFileRef( entity.ChatChannelAvatarBinaryFile, entity.ChatChannelAvatarBinaryFileId );

            // The Phase 1 / 2 view-mode notification surface is intentionally
            // not surfaced in edit mode (per Q3.7 lock).
            bag.RoleLimitWarning = null;

            return bag;
        }

        /// <inheritdoc/>
        protected override bool TryGetEntityForEditAction( string idKey, out Model.Group entity, out BlockActionResult error )
        {
            var entityService = new GroupService( RockContext );
            error = null;

            if ( idKey.IsNotNullOrWhiteSpace() )
            {
                entity = entityService.Get( idKey, !PageCache.Layout.Site.DisablePredictableIds );
            }
            else
            {
                entity = new Model.Group();
                entityService.Add( entity );

                ApplyNewGroupDefaultValues( entity, entityService );
            }

            if ( entity == null )
            {
                error = ActionBadRequest( $"{Model.Group.FriendlyTypeName} not found." );
                return false;
            }

            if ( !entity.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) )
            {
                error = ActionBadRequest( $"Not authorized to edit {Model.Group.FriendlyTypeName}." );
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Implements the scalar field assignments per Phase 3 spec
        /// checklist S3, with the GroupType-conditional cascades for
        /// peer-network (S13), RSVP (S14), and record source (S15).
        /// Inline-schedule lifecycle (S8) and inactive cascade (S7) /
        /// IsTemporary toggles (steps 6/7 of the WrapTransaction
        /// ordering) run inside <c>Save</c> rather than here so they can
        /// observe pre-vs-post save state.
        /// </remarks>
        protected override bool UpdateEntityFromBox( Model.Group entity, ValidPropertiesBox<GroupBag> box )
        {
            if ( box.ValidProperties == null )
            {
                return false;
            }

            box.IfValidProperty( nameof( box.Bag.Name ),
                () => entity.Name = box.Bag.Name );

            box.IfValidProperty( nameof( box.Bag.Description ),
                () => entity.Description = box.Bag.Description );

            box.IfValidProperty( nameof( box.Bag.IsActive ), () =>
            {
                entity.IsActive = box.Bag.IsActive;

                // Don't persist inactive properties when active.
                if ( box.Bag.IsActive )
                {
                    entity.InactiveReasonValueId = null;
                    entity.InactiveReasonNote = null;
                }
                else
                {
                    entity.InactiveReasonValueId = box.Bag.InactiveReasonValueId;
                    entity.InactiveReasonNote = box.Bag.InactiveReasonNote;
                }
            } );

            box.IfValidProperty( nameof( box.Bag.IsPublic ),
                () => entity.IsPublic = box.Bag.IsPublic );

            box.IfValidProperty( nameof( box.Bag.GroupTypeId ), () =>
            {
                if ( box.Bag.GroupTypeId.HasValue && box.Bag.GroupTypeId.Value > 0 )
                {
                    entity.GroupTypeId = box.Bag.GroupTypeId.Value;
                }
            } );

            // Resolve the GroupType AFTER the GroupTypeId has been assigned.
            var groupType = GetGroupTypeCache( entity );

            box.IfValidProperty( nameof( box.Bag.ParentGroup ),
                () => entity.ParentGroupId = box.Bag.ParentGroup?.GetEntityId<Model.Group>( RockContext ) );

            box.IfValidProperty( nameof( box.Bag.Campus ),
                () => entity.CampusId = box.Bag.Campus?.GetEntityId<Campus>( RockContext ) );

            box.IfValidProperty( nameof( box.Bag.StatusValueId ),
                () => entity.StatusValueId = box.Bag.StatusValueId );

            box.IfValidProperty( nameof( box.Bag.GroupCapacity ),
                () => entity.GroupCapacity = box.Bag.GroupCapacity );

            box.IfValidProperty( nameof( box.Bag.RequiredSignatureDocumentTemplateId ),
                () => entity.RequiredSignatureDocumentTemplateId = box.Bag.RequiredSignatureDocumentTemplateId );

            box.IfValidProperty( nameof( box.Bag.Administrator ), () =>
            {
                if ( groupType != null && groupType.ShowAdministrator )
                {
                    var aliasGuid = box.Bag.Administrator?.Value.AsGuidOrNull();
                    entity.GroupAdministratorPersonAliasId = aliasGuid.HasValue
                        ? new PersonAliasService( RockContext ).GetSelect( aliasGuid.Value, pa => ( int? ) pa.Id )
                        : null;
                }
            } );

            // Member Record Source (S15) — only when the group type allows.
            box.IfValidProperty( nameof( box.Bag.GroupMemberRecordSource ), () =>
            {
                if ( groupType != null && groupType.AllowGroupSpecificRecordSource )
                {
                    entity.GroupMemberRecordSourceValueId = box.Bag.GroupMemberRecordSource?.GetEntityId<DefinedValue>( RockContext );
                }
                else
                {
                    entity.GroupMemberRecordSourceValueId = null;
                }
            } );

            // IsSecurityRole (S6) — force-true when block attribute set.
            box.IfValidProperty( nameof( box.Bag.IsSecurityRole ), () =>
            {
                entity.IsSecurityRole = box.Bag.IsSecurityRole;
                if ( GetAttributeValue( AttributeKey.LimittoSecurityRoleGroups ).AsBoolean() )
                {
                    entity.IsSecurityRole = true;
                }
            } );

            box.IfValidProperty( nameof( box.Bag.ElevatedSecurityLevel ), () =>
            {
                entity.ElevatedSecurityLevel = box.Bag.ElevatedSecurityLevel;
                if ( !entity.IsSecurityRole )
                {
                    entity.ElevatedSecurityLevel = Rock.Utility.Enums.ElevatedSecurityLevel.None;
                }
            } );

            // Peer Network overrides (S13) — only when the group type
            // enables peer-network AND the user checks the override box.
            box.IfValidProperty( nameof( box.Bag.OverrideRelationshipStrength ), () =>
            {
                var isPeerNetworkEnabled = groupType?.IsPeerNetworkEnabled == true;

                if ( isPeerNetworkEnabled && box.Bag.OverrideRelationshipStrength )
                {
                    entity.RelationshipStrengthOverride = ( int? ) ( box.Bag.RelationshipStrengthOverride ?? RelationshipStrength.None );
                    entity.RelationshipGrowthEnabledOverride = box.Bag.RelationshipGrowthEnabledOverride;

                    entity.LeaderToLeaderRelationshipMultiplierOverride = box.Bag.LeaderToLeaderRelationshipMultiplierOverride;
                    entity.LeaderToNonLeaderRelationshipMultiplierOverride = box.Bag.LeaderToNonLeaderRelationshipMultiplierOverride;
                    entity.NonLeaderToLeaderRelationshipMultiplierOverride = box.Bag.NonLeaderToLeaderRelationshipMultiplierOverride;
                    entity.NonLeaderToNonLeaderRelationshipMultiplierOverride = box.Bag.NonLeaderToNonLeaderRelationshipMultiplierOverride;
                }
                else if ( isPeerNetworkEnabled )
                {
                    entity.RelationshipStrengthOverride = null;
                    entity.RelationshipGrowthEnabledOverride = null;
                    entity.LeaderToLeaderRelationshipMultiplierOverride = null;
                    entity.LeaderToNonLeaderRelationshipMultiplierOverride = null;
                    entity.NonLeaderToLeaderRelationshipMultiplierOverride = null;
                    entity.NonLeaderToNonLeaderRelationshipMultiplierOverride = null;
                }
            } );

            // RSVP overrides (S14) — null out per group type pinning.
            box.IfValidProperty( nameof( box.Bag.RsvpReminderOffsetDays ), () =>
            {
                if ( groupType?.EnableRSVP == true )
                {
                    entity.RSVPReminderOffsetDays = groupType.RSVPReminderOffsetDays.HasValue
                        ? ( int? ) null
                        : box.Bag.RsvpReminderOffsetDays;
                }
                else
                {
                    entity.RSVPReminderOffsetDays = null;
                }
            } );

            box.IfValidProperty( nameof( box.Bag.RsvpReminderSystemCommunicationGuid ), () =>
            {
                if ( groupType?.EnableRSVP == true )
                {
                    entity.RSVPReminderSystemCommunicationId = groupType.RSVPReminderSystemCommunicationId.HasValue
                        ? ( int? ) null
                        : ( box.Bag.RsvpReminderSystemCommunicationGuid.HasValue
                            ? new SystemCommunicationService( RockContext ).GetSelect( box.Bag.RsvpReminderSystemCommunicationGuid.Value, c => ( int? ) c.Id )
                            : null );
                }
                else
                {
                    entity.RSVPReminderSystemCommunicationId = null;
                }
            } );

            // Member Scheduling.
            box.IfValidProperty( nameof( box.Bag.SchedulingMustMeetRequirements ),
                () => entity.SchedulingMustMeetRequirements = box.Bag.SchedulingMustMeetRequirements );

            box.IfValidProperty( nameof( box.Bag.DisableScheduling ),
                () => entity.DisableScheduling = box.Bag.DisableScheduling );

            box.IfValidProperty( nameof( box.Bag.DisableScheduleToolboxAccess ),
                () => entity.DisableScheduleToolboxAccess = box.Bag.DisableScheduleToolboxAccess );

            box.IfValidProperty( nameof( box.Bag.ScheduleConfirmationLogic ),
                () => entity.ScheduleConfirmationLogic = box.Bag.ScheduleConfirmationLogic );

            box.IfValidProperty( nameof( box.Bag.ScheduleCoordinatorPerson ), () =>
            {
                var aliasGuid = box.Bag.ScheduleCoordinatorPerson?.Value.AsGuidOrNull();
                entity.ScheduleCoordinatorPersonAliasId = aliasGuid.HasValue
                    ? new PersonAliasService( RockContext ).GetSelect( aliasGuid.Value, pa => ( int? ) pa.Id )
                    : null;
            } );

            box.IfValidProperty( nameof( box.Bag.ScheduleCoordinatorNotificationTypes ),
                () => entity.ScheduleCoordinatorNotificationTypes = box.Bag.ScheduleCoordinatorNotificationTypes );

            box.IfValidProperty( nameof( box.Bag.AttendanceRecordRequiredForCheckIn ),
                () => entity.AttendanceRecordRequiredForCheckIn = box.Bag.AttendanceRecordRequiredForCheckIn );

            // Chat overrides — only when the group type allows.
            box.IfValidProperty( nameof( box.Bag.IsChatEnabledOverride ), () =>
            {
                if ( ChatHelper.IsChatEnabled && groupType?.IsChatAllowed == true )
                {
                    entity.IsChatEnabledOverride = box.Bag.IsChatEnabledOverride;
                    entity.IsLeavingChatChannelAllowedOverride = box.Bag.IsLeavingChatChannelAllowedOverride;
                    entity.IsChatChannelPublicOverride = box.Bag.IsChatChannelPublicOverride;
                    entity.IsChatChannelAlwaysShownOverride = box.Bag.IsChatChannelAlwaysShownOverride;
                    entity.ChatPushNotificationModeOverride = box.Bag.ChatPushNotificationModeOverride;
                }
            } );

            return true;
        }

        /// <inheritdoc/>
        public BreadCrumbResult GetBreadCrumbs( PageReference pageReference )
        {
            /*
                5/6/2026 - CLAUDE

                The WebForms block at GroupDetail.ascx.cs:592-615 returned
                "New Group" both for ?GroupId=0 (real Add) and for any
                ?GroupId=N where the group could not be loaded (deleted /
                stale Id). The latter is misleading: a user navigating to
                a deleted group's URL would see a breadcrumb implying they
                were creating something. Phase 1 fixes this so the Add
                crumb is only shown when the page parameter is actually
                an Add sentinel (integer 0 or empty Guid); a stale Id
                returns no crumb (same as the no-parameter branch).

                Reason: lookup-miss should not be rendered as Add.
            */

            var key = pageReference.GetPageParameter( PageParameterKey.GroupId );

            if ( key.IsNullOrWhiteSpace() )
            {
                return new BreadCrumbResult { BreadCrumbs = new List<IBreadCrumb>() };
            }

            var id = key.AsIntegerOrNull();
            var guid = key.AsGuidOrNull();
            var isAddPath = ( id.HasValue && id.Value == 0 )
                || ( guid.HasValue && guid.Value == Guid.Empty );

            if ( isAddPath )
            {
                var addCrumb = new BreadCrumbLink( "New Group", new PageReference( pageReference.PageId, 0 ) );
                return new BreadCrumbResult { BreadCrumbs = new List<IBreadCrumb> { addCrumb } };
            }

            // Per Q4 (00-architecture.md), outbound GroupId parameters
            // must be normalized to IdKey regardless of the inbound form.
            // Project to Id (a real SQL column) and hash to IdKey in C#
            // memory; Entity<T>.IdKey is [NotMapped] and cannot be
            // translated to SQL by GetSelect.
            var info = new GroupService( RockContext ).GetSelect( key, g => new { g.Name, g.Id } );
            if ( info == null )
            {
                return new BreadCrumbResult { BreadCrumbs = new List<IBreadCrumb>() };
            }

            var pageParameters = new Dictionary<string, string>
            {
                [PageParameterKey.GroupId] = Rock.Utility.IdHasher.Instance.GetHash( info.Id )
            };
            var breadCrumbPageRef = new PageReference( pageReference.PageId, 0, pageParameters );
            var breadCrumb = new BreadCrumbLink( info.Name, breadCrumbPageRef );

            return new BreadCrumbResult
            {
                BreadCrumbs = new List<IBreadCrumb> { breadCrumb }
            };
        }

        /// <summary>
        /// Builds the navigation URL dictionary for the block. Outbound
        /// URLs are emitted with the <c>((Key))</c> placeholder so the
        /// Vue layer can substitute the active group's IdKey at render
        /// time per Q4. The 5 still-WebForms destinations (GroupListPage,
        /// FundraisingProgressPage, GroupHistoryPage, GroupMapPage,
        /// GroupSchedulerPage) will produce broken links until Phase 7
        /// updates them to accept IdKey - acknowledged tradeoff.
        /// </summary>
        private Dictionary<string, string> GetBoxNavigationUrls()
        {
            var groupIdParam = new Dictionary<string, string>
            {
                [PageParameterKey.GroupId] = "((Key))"
            };

            return new Dictionary<string, string>
            {
                [NavigationUrlKey.AttendancePage]           = this.GetLinkedPageUrl( AttributeKey.AttendancePage, groupIdParam ),
                [NavigationUrlKey.GroupSchedulerPage]       = this.GetLinkedPageUrl( AttributeKey.GroupSchedulerPage, groupIdParam ),
                [NavigationUrlKey.GroupRSVPPage]            = this.GetLinkedPageUrl( AttributeKey.GroupRSVPPage, groupIdParam ),
                [NavigationUrlKey.GroupPlacementPage]       = this.GetLinkedPageUrl( AttributeKey.GroupPlacementPage, groupIdParam ),
                [NavigationUrlKey.GroupMapPage]             = this.GetLinkedPageUrl( AttributeKey.GroupMapPage, groupIdParam ),
                [NavigationUrlKey.GroupHistoryPage]         = this.GetLinkedPageUrl( AttributeKey.GroupHistoryPage, groupIdParam ),
                [NavigationUrlKey.FundraisingProgressPage]  = this.GetLinkedPageUrl( AttributeKey.FundraisingProgressPage, groupIdParam ),
                [NavigationUrlKey.RegistrationInstancePage] = this.GetLinkedPageUrl( AttributeKey.RegistrationInstancePage, groupIdParam ),
                [NavigationUrlKey.EventItemOccurrencePage]  = this.GetLinkedPageUrl( AttributeKey.EventItemOccurrencePage, groupIdParam ),
                [NavigationUrlKey.ContentItemPage]          = this.GetLinkedPageUrl( AttributeKey.ContentItemPage, groupIdParam ),
                [NavigationUrlKey.GroupListPage]            = this.GetLinkedPageUrl( AttributeKey.GroupListPage, groupIdParam ),
            };
        }

        /// <summary>
        /// Applies default values to a new <see cref="Model.Group"/>:
        /// (1) pre-populates the parent group from the
        /// <c>?ParentGroupId=N</c> page parameter; (2) defaults
        /// <c>GroupTypeId</c> to the security-role group type when the
        /// <c>LimittoSecurityRoleGroups</c> block attribute is on; (3)
        /// auto-picks <c>GroupTypeId</c> when the parent group narrows
        /// the allowed child types to exactly one auth-survivable
        /// option (matches WebForms <c>ShowDetail</c> at
        /// <see href="../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1782"/>).
        /// Add path only — returns immediately when the entity is null
        /// or already has an Id.
        /// </summary>
        /// <param name="entity">The group entity.</param>
        /// <param name="groupService">An optional service instance to use for queries.</param>
        private void ApplyNewGroupDefaultValues( Model.Group entity, GroupService groupService = null )
        {
            if ( entity == null || entity.Id != 0 )
            {
                return;
            }

            groupService = groupService ?? new GroupService( RockContext );

            // (1) Pre-populate parent group from ?ParentGroupId=N.
            var parentGroupParam = PageParameter( PageParameterKey.ParentGroupId );
            if ( parentGroupParam.IsNotNullOrWhiteSpace() )
            {
                var parentGroup = groupService.Get( parentGroupParam, !PageCache.Layout.Site.DisablePredictableIds );
                if ( parentGroup != null )
                {
                    entity.ParentGroupId = parentGroup.Id;
                    entity.ParentGroup = parentGroup;
                }
            }

            // (2) Block locked to security-role groups → default
            // GroupType to the security-role group type. Skip the
            // parent-driven auto-pick below since the dropdown is
            // already constrained to a single option.
            if ( GetAttributeValue( AttributeKey.LimittoSecurityRoleGroups ).AsBoolean() )
            {
                var securityRoleGroupType = GroupTypeCache.GetSecurityRoleGroupType();
                if ( securityRoleGroupType != null )
                {
                    entity.GroupTypeId = securityRoleGroupType.Id;
                }
                return;
            }

            // (3) Parent narrows allowed child types — auto-pick the
            // single auth-survivable option, otherwise leave blank so
            // the user is forced to choose.
            if ( entity.ParentGroup != null )
            {
                var allowedChildGroupTypes = GetAllowedGroupTypes( GroupTypeCache.Get( entity.ParentGroup.GroupTypeId ), RockContext ).ToList();

                var authorizedGroupTypes = new List<Model.GroupType>();
                foreach ( var allowedGroupType in allowedChildGroupTypes )
                {
                    // Probe auth by temporarily assigning the group type
                    // and asking the entity. Mirrors WebForms parity.
                    entity.GroupTypeId = allowedGroupType.Id;
                    entity.GroupType = allowedGroupType;

                    if ( entity.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) )
                    {
                        authorizedGroupTypes.Add( allowedGroupType );
                    }
                }

                if ( authorizedGroupTypes.Count == 1 )
                {
                    entity.GroupType = authorizedGroupTypes[0];
                    entity.GroupTypeId = authorizedGroupTypes[0].Id;
                }
                else
                {
                    // Reset so the user picks. If zero are authorized,
                    // the downstream IsAuthorized check on the entity
                    // will fall back to ParentGroup-based auth.
                    entity.GroupType = null;
                    entity.GroupTypeId = 0;
                }
            }
        }

        #endregion Methods

        #region Block Actions

        /// <summary>
        /// Returns the per-GroupType options payload for the active
        /// GroupType selection (Q2 Approach B server round-trip per
        /// change). Re-checks EDIT auth using the page-parameter entity
        /// before resolving the requested GroupType.
        /// </summary>
        [BlockAction]
        public BlockActionResult GetGroupTypeOptions( int groupTypeId )
        {
            // Re-check EDIT on the entity. Add path uses a fresh entity
            // and inherits page-level EDIT auth.
            var entity = GetInitialEntity();
            if ( entity == null )
            {
                return ActionBadRequest( $"{Model.Group.FriendlyTypeName} not found." );
            }

            if ( !entity.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) )
            {
                return ActionBadRequest( $"Not authorized to edit {Model.Group.FriendlyTypeName}." );
            }

            if ( groupTypeId <= 0 )
            {
                return ActionBadRequest( "Group Type is required." );
            }

            var groupType = GroupTypeCache.Get( groupTypeId );
            if ( groupType == null )
            {
                return ActionBadRequest( "Group Type not found." );
            }

            return ActionOk( BuildGroupTypeOptionsBag( groupTypeId ) );
        }

        /// <summary>
        /// Returns the bag for entering edit mode. Existing groups load via
        /// <see cref="TryGetEntityForEditAction(string, out Model.Group, out BlockActionResult)"/>;
        /// the Add path constructs a fresh entity in that same call and
        /// applies Add-mode defaults via <see cref="ApplyNewGroupDefaultValues"/>.
        /// The Vue side reactively fetches <see cref="GroupTypeOptionsBag"/>
        /// via the <c>GetGroupTypeOptions</c> action whenever
        /// <c>bag.groupTypeId</c> changes, so this action only returns the
        /// standard properties bag.
        /// </summary>
        [BlockAction]
        public BlockActionResult Edit( string key )
        {
            if ( !TryGetEntityForEditAction( key, out var entity, out var actionError ) )
            {
                return actionError;
            }

            var bag = GetEntityBagForEdit( entity );

            return ActionOk( new ValidPropertiesBox<GroupBag>
            {
                Bag = bag,
                ValidProperties = bag.GetType().GetProperties().Select( p => p.Name ).ToList()
            } );
        }

        /// <summary>
        /// Saves the group from its edit-mode bag. Implements the
        /// 8-step <c>WrapTransaction</c> ordering locked in Q3.5:
        /// (1) Add(group) if new + (2) UpdateEntityFromBox + (3)
        /// SaveChanges; (4) AllowPerson when Add and the
        /// <c>AddAdministrateSecurityToGroupCreator</c> attribute is on;
        /// (5) Inactive cascade through descendants when "Also
        /// Inactivate Child Groups" is checked; (6) chat-avatar
        /// IsTemporary toggle; (7) photo IsTemporary toggle (mirroring
        /// chat-avatar); (8) SaveChanges. Validation gates 1-8 from
        /// webforms/23-validations-and-cascades.md fire before the
        /// transaction opens.
        /// </summary>
        [BlockAction]
        public BlockActionResult Save( ValidPropertiesBox<GroupBag> box )
        {
            if ( box?.Bag == null )
            {
                return ActionBadRequest( "Invalid request." );
            }

            if ( !TryGetEntityForEditAction( box.Bag.IdKey, out var entity, out var actionError ) )
            {
                return actionError;
            }

            var roleGroupTypeId = GroupTypeCache.GetId( Rock.SystemGuid.GroupType.GROUPTYPE_SECURITY_ROLE.AsGuid() ) ?? int.MinValue;

            // Capture before-state for IsSecurityRole flip detection (S12).
            var wasSecurityRole = entity.Id != 0
                && entity.IsActive
                && ( entity.IsSecurityRole || entity.GroupTypeId == roleGroupTypeId );

            // Capture orphan candidates before mutation.
            var oldPhotoId = entity.PhotoId;
            var oldChatChannelAvatarId = entity.ChatChannelAvatarBinaryFileId;
            var oldScheduleId = entity.ScheduleId;

            // Validation gate 1 — Group Type chosen.
            if ( !box.Bag.GroupTypeId.HasValue || box.Bag.GroupTypeId.Value <= 0 )
            {
                return ActionBadRequest( WarningMessage.CannotBeBlank( Model.GroupType.FriendlyTypeName ) );
            }

            // Apply scalar field assignments.
            if ( !UpdateEntityFromBox( entity, box ) )
            {
                return ActionBadRequest( "Invalid data." );
            }

            // Validation gate 2 — self-parent check.
            if ( entity.Id != 0 && entity.ParentGroupId == entity.Id )
            {
                return ActionBadRequest( "Group cannot be a Parent Group of itself." );
            }

            // Apply photo / chat-avatar / inline-schedule mutations
            // outside the IfValidProperty pattern because each requires
            // contextual state (orphan tracking, schedule lifecycle) that
            // doesn't fit the partial-update pattern.
            ApplyPhotoBinaryFile( entity, box.Bag );
            ApplyChatChannelAvatarBinaryFile( entity, box.Bag );
            ApplyInlineSchedule( entity, box.Bag );

            // Validation gate 5 — parent allows this group type. Reuse
            // the already-loaded ParentGroup navigation when available
            // (e.g., when ApplyNewGroupDefaultValues pre-populated it on
            // the Add-from-tree path) to avoid a redundant query.
            if ( entity.ParentGroupId.HasValue )
            {
                var parentGroup = entity.ParentGroup ?? new GroupService( RockContext ).Get( entity.ParentGroupId.Value );
                if ( parentGroup != null )
                {
                    var allowedGroupTypeIds = GetAllowedGroupTypes( GroupTypeCache.Get( parentGroup.GroupTypeId ), RockContext )
                        .Select( gt => gt.Id )
                        .ToList();
                    if ( !allowedGroupTypeIds.Contains( entity.GroupTypeId ) )
                    {
                        var groupTypeForError = GroupTypeCache.Get( entity.GroupTypeId );
                        return ActionBadRequest( $"The '{parentGroup.Name}' group does not allow child groups with a '{groupTypeForError?.Name ?? string.Empty}' group type." );
                    }
                }
            }

            // Validation gate 6 — re-check EDIT now that GroupType /
            // ParentGroup may have been swapped.
            if ( !entity.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) )
            {
                return ActionBadRequest( $"Not authorized to edit {Model.Group.FriendlyTypeName}." );
            }

            // Validation gate 8 — Group.IsValid (model-level rules,
            // including GroupsRequireCampus).
            if ( !entity.IsValid )
            {
                return ActionBadRequest( entity.ValidationResults.Select( r => r.ErrorMessage ).ToList().AsDelimited( "; " ) );
            }

            var isNew = entity.Id == 0;
            var addAdministrateSecurity = isNew
                && GetAttributeValue( AttributeKey.AddAdministrateSecurityToGroupCreator ).AsBoolean();

            RockContext.WrapTransaction( () =>
            {
                // Step 1 — Add(group) implicitly handled by EF tracking
                //          (TryGetEntityForEditAction calls Service.Add for
                //          new entities). Step 2 (UpdateEntityFromBox)
                //          already ran above. Step 3 — SaveChanges to
                //          assign group.Id.
                RockContext.SaveChanges();

                // Step 4 — Add ADMINISTRATE to group creator on Add.
                if ( addAdministrateSecurity )
                {
                    Authorization.AllowPerson( entity, Authorization.ADMINISTRATE, RequestContext.CurrentPerson, RockContext );
                }

                // Step 5 — Inactive cascade to descendants.
                if ( !entity.IsActive && box.Bag.InactivateChildGroups )
                {
                    var groupService = new GroupService( RockContext );
                    var allActiveDescendantIds = groupService.GetAllDescendentGroupIds( entity.Id, includeInactiveChildGroups: false );
                    var allActiveDescendants = groupService.GetByIds( allActiveDescendantIds );
                    foreach ( var descendant in allActiveDescendants )
                    {
                        if ( descendant.IsActive )
                        {
                            descendant.IsActive = false;
                            descendant.InactiveReasonValueId = box.Bag.InactiveReasonValueId;
                            descendant.InactiveReasonNote = "Parent Deactivated";
                            if ( box.Bag.InactiveReasonNote.IsNotNullOrWhiteSpace() )
                            {
                                descendant.InactiveReasonNote += ": " + box.Bag.InactiveReasonNote;
                            }
                        }
                    }
                }

                // Steps 6 + 7 — IsTemporary toggles for orphaned + active
                // BinaryFiles (chat avatar + photo).
                ToggleBinaryFileIsTemporary( oldChatChannelAvatarId, entity.ChatChannelAvatarBinaryFileId );
                ToggleBinaryFileIsTemporary( oldPhotoId, entity.PhotoId );

                // Step 8 — Persist the inactive cascade + IsTemporary
                // toggles + (if any) inline schedule deletion.
                if ( oldScheduleId.HasValue && oldScheduleId.Value != ( entity.ScheduleId ?? 0 ) )
                {
                    DeleteInlineSchedule( oldScheduleId.Value );
                }

                RockContext.SaveChanges();
            } );

            // Cache invalidation — IsSecurityRole flip (S11).
            var isNowSecurityRole = entity.IsActive && ( entity.IsSecurityRole || entity.GroupTypeId == roleGroupTypeId );
            if ( wasSecurityRole != isNowSecurityRole )
            {
                Authorization.Clear();
            }

            // Honor a same-origin ?returnUrl=N if set, mirroring WebForms
            // parity at GroupDetail.ascx.cs:1438-1441 (unconditional, no
            // autoEdit gate). The <DetailBlock> framework template only
            // honors ?returnUrl= when ?autoEdit=true is also set
            // (detailBlock.ts:736-744 gates on isAutoEditMode), so for
            // non-autoEdit Save flows the redirect must be echoed by
            // the block action. The framework's onSave handler treats
            // string results as redirect URLs.
            var saveReturnUrl = PageParameter( PageParameterKey.ReturnUrl );
            if ( saveReturnUrl.IsNotNullOrWhiteSpace() && IsSafeReturnUrl( saveReturnUrl ) )
            {
                return ActionContent( System.Net.HttpStatusCode.OK, saveReturnUrl );
            }

            if ( isNew )
            {
                // Preserve ExpandedIds so the post-Add reload keeps the
                // tree-navigation context the user came in with. Mirrors
                // WebForms btnSave_Click at GroupDetail.ascx.cs:1447 and
                // the Copy block action's same handling.
                var qryParams = new Dictionary<string, string>
                {
                    [PageParameterKey.GroupId] = entity.IdKey
                };

                var expandedIds = PageParameter( PageParameterKey.ExpandedIds );
                if ( expandedIds.IsNotNullOrWhiteSpace() )
                {
                    qryParams[PageParameterKey.ExpandedIds] = expandedIds;
                }

                var redirectUrl = this.GetCurrentPageUrl( qryParams );
                return ActionContent( System.Net.HttpStatusCode.Created, redirectUrl );
            }

            // Refresh navigation properties before re-bagging.
            entity = new GroupService( RockContext ).Get( entity.Id );
            var refreshedBag = GetEntityBagForEdit( entity );

            return ActionOk( new ValidPropertiesBox<GroupBag>
            {
                Bag = refreshedBag,
                ValidProperties = refreshedBag.GetType().GetProperties().Select( p => p.Name ).ToList()
            } );
        }

        /// <summary>
        /// Deletes the specified group. Mirrors the WebForms parity at
        /// <see href="../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L675"/>:
        /// EDIT auth, <c>CanDelete</c> validation, inline-schedule
        /// cleanup, and the <c>DeleteSecurityRoleGroup</c> branch for
        /// security-role groups.
        /// </summary>
        [BlockAction]
        public BlockActionResult Delete( string key )
        {
            var groupService = new GroupService( RockContext );
            var entity = groupService.Get( key, !PageCache.Layout.Site.DisablePredictableIds );

            if ( entity == null )
            {
                return ActionBadRequest( $"{Model.Group.FriendlyTypeName} not found." );
            }

            if ( !entity.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) )
            {
                return ActionBadRequest( "You are not authorized to delete this group." );
            }

            if ( !groupService.CanDelete( entity, out var errorMessage, includeSecondLvl: true ) )
            {
                return ActionBadRequest( errorMessage );
            }

            var parentGroupId = entity.ParentGroupId;

            // If the group has a non-named (inline) schedule and no other
            // group is using it, delete the schedule too. Mirrors the
            // WebForms cleanup at GroupDetail.ascx.cs:700-713.
            if ( entity.ScheduleId.HasValue )
            {
                var scheduleService = new ScheduleService( RockContext );
                var schedule = scheduleService.Get( entity.ScheduleId.Value );
                if ( schedule != null && schedule.ScheduleType != ScheduleType.Named )
                {
                    var isReferencedByOtherGroup = groupService.Queryable()
                        .Any( g => g.ScheduleId == schedule.Id && g.Id != entity.Id );
                    if ( !isReferencedByOtherGroup )
                    {
                        scheduleService.Delete( schedule );
                    }
                }
            }

            // Security-role groups need the dedicated cleanup path so the
            // related Auth rows and global cache invalidation happen
            // correctly. Non-security groups go through the standard
            // Service.Delete flow.
            if ( entity.IsSecurityRoleOrSecurityGroupType() )
            {
                GroupService.DeleteSecurityRoleGroup( entity.Id );
            }
            else
            {
                groupService.Delete( entity );
            }

            RockContext.SaveChanges();

            return ActionOk( NavigateAfterDeleteOrArchive( parentGroupId ) );
        }

        /// <summary>
        /// Archives the specified group only (no descendant cascade).
        /// The Vue layer reads <see cref="GroupBag.HasChildGroups"/> and
        /// prompts the user before deciding whether to call this action
        /// or <see cref="ArchiveWithChildren(string)"/>. Mirrors the
        /// WebForms <c>ArchiveSingleGroup</c> worker at
        /// <see href="../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3378"/>.
        /// </summary>
        [BlockAction]
        public BlockActionResult Archive( string key )
        {
            var groupService = new GroupService( RockContext );
            var entity = groupService.Get( key, !PageCache.Layout.Site.DisablePredictableIds );

            if ( entity == null )
            {
                return ActionBadRequest( $"{Model.Group.FriendlyTypeName} not found." );
            }

            if ( !entity.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) )
            {
                return ActionBadRequest( "You are not authorized to archive this group." );
            }

            var personAliasId = RequestContext.CurrentPerson?.PrimaryAliasId;

            groupService.Archive( entity, personAliasId, true );
            RockContext.SaveChanges();

            return ActionOk( NavigateAfterDeleteOrArchive( entity.ParentGroupId ) );
        }

        /// <summary>
        /// Archives the specified group plus every descendant group via
        /// <c>GetAllDescendentGroups</c>. Mirrors the WebForms parity at
        /// <see href="../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3407"/>.
        /// </summary>
        [BlockAction]
        public BlockActionResult ArchiveWithChildren( string key )
        {
            var groupService = new GroupService( RockContext );
            var entity = groupService.Get( key, !PageCache.Layout.Site.DisablePredictableIds );

            if ( entity == null )
            {
                return ActionBadRequest( $"{Model.Group.FriendlyTypeName} not found." );
            }

            if ( !entity.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) )
            {
                return ActionBadRequest( "You are not authorized to archive this group." );
            }

            var personAliasId = RequestContext.CurrentPerson?.PrimaryAliasId;
            var descendants = groupService.GetAllDescendentGroups( entity.Id, true );
            foreach ( var descendant in descendants )
            {
                groupService.Archive( descendant, personAliasId, true );
            }

            groupService.Archive( entity, personAliasId, true );
            RockContext.SaveChanges();

            return ActionOk( NavigateAfterDeleteOrArchive( entity.ParentGroupId ) );
        }

        /// <summary>
        /// Copies the specified group via <see cref="GroupService.CopyGroup"/>.
        /// The Vue modal defaults <c>IncludeChildGroups</c> to <c>false</c>,
        /// flipped from the WebForms default per design behavior change C1.
        /// </summary>
        [BlockAction]
        public BlockActionResult Copy( CopyGroupRequestBag bag )
        {
            if ( bag == null || bag.Key.IsNullOrWhiteSpace() )
            {
                return ActionBadRequest( "Invalid request." );
            }

            var groupService = new GroupService( RockContext );
            var entity = groupService.Get( bag.Key, !PageCache.Layout.Site.DisablePredictableIds );

            if ( entity == null )
            {
                return ActionBadRequest( $"{Model.Group.FriendlyTypeName} not found." );
            }

            if ( !entity.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) )
            {
                return ActionBadRequest( "You are not authorized to copy the group." );
            }

            var copyOptions = new CopyGroupOptions
            {
                GroupId = entity.Id,
                IncludeChildGroups = bag.IncludeChildGroups,
                CreatedByPersonAliasId = RequestContext.CurrentPerson?.PrimaryAliasId
            };

            var newGroupId = GroupService.CopyGroup( copyOptions );

            // Copy navigation does NOT honor returnUrl per the WebForms
            // parity at GroupDetail.ascx.cs:1517-1545. Always reload to
            // the new group.
            var newGroupKey = newGroupId.HasValue && newGroupId.Value > 0
                ? Rock.Utility.IdHasher.Instance.GetHash( newGroupId.Value )
                : entity.IdKey;

            var qryParams = new Dictionary<string, string>
            {
                [PageParameterKey.GroupId] = newGroupKey
            };

            var expandedIds = PageParameter( PageParameterKey.ExpandedIds );
            if ( expandedIds.IsNotNullOrWhiteSpace() )
            {
                qryParams[PageParameterKey.ExpandedIds] = expandedIds;
            }

            return ActionOk( this.GetCurrentPageUrl( qryParams ) );
        }

        #endregion Block Actions

        #region Helper Methods

        /// <summary>
        /// Returns the <see cref="GroupTypeCache"/> for the entity, or null
        /// when no group type is set. Memoized per-request; re-resolves if
        /// the entity's <c>GroupTypeId</c> changes.
        /// </summary>
        private GroupTypeCache GetGroupTypeCache( Model.Group entity )
        {
            if ( entity == null || entity.GroupTypeId <= 0 )
            {
                return null;
            }

            if ( _cachedGroupType?.Id == entity.GroupTypeId )
            {
                return _cachedGroupType;
            }

            _cachedGroupType = GroupTypeCache.Get( entity.GroupTypeId );
            return _cachedGroupType;
        }

        /// <summary>
        /// Resolves the effective <see cref="RelationshipStrength"/> for
        /// the subheader chip: the group's override falling back to the
        /// group type default. Returns null when the group type lacks
        /// peer-network support, hiding the chip. Non-canonical stored
        /// integers round-trip as-is; the Vue side's
        /// <c>RelationshipStrengthDescription</c> lookup returns
        /// undefined for those, which also hides the chip.
        /// </summary>
        private static RelationshipStrength? GetRelationshipStrength( Model.Group entity, GroupTypeCache groupType )
        {
            return groupType?.IsPeerNetworkEnabled == true
                ? ( RelationshipStrength? )( entity.RelationshipStrengthOverride ?? groupType.RelationshipStrength )
                : null;
        }

        /// <summary>
        /// Returns true when the supplied group type Id is the
        /// <c>FundraisingOpportunity</c> group type or its immediate
        /// inheritor via <c>InheritedGroupTypeId</c>. Mirrors the WebForms
        /// visibility check at
        /// <see href="../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2827"/>,
        /// which only checks one level of inheritance.
        /// </summary>
        private static bool IsFundraisingGroupType( int groupTypeId )
        {
            var fundraisingGroupTypeId = GroupTypeCache.GetId( Rock.SystemGuid.GroupType.GROUPTYPE_FUNDRAISINGOPPORTUNITY.AsGuid() );
            if ( !fundraisingGroupTypeId.HasValue )
            {
                return false;
            }

            var groupType = GroupTypeCache.Get( groupTypeId );
            if ( groupType == null )
            {
                return false;
            }

            return groupType.Id == fundraisingGroupTypeId.Value
                || groupType.InheritedGroupTypeId == fundraisingGroupTypeId.Value;
        }

        /// <summary>
        /// Returns true when the current user is a member of the
        /// GROUP_ADMINISTRATORS system group. Per WebForms parity, only
        /// such members can toggle the "Enable as Security Role" box.
        /// </summary>
        private bool IsCurrentPersonGroupAdministrator()
        {
            var currentPersonId = RequestContext.CurrentPerson?.Id;
            if ( !currentPersonId.HasValue )
            {
                return false;
            }

            return new GroupService( RockContext ).GroupHasMember(
                Rock.SystemGuid.Group.GROUP_ADMINISTRATORS.AsGuid(),
                currentPersonId.Value );
        }

        /// <summary>
        /// Builds the administrator reference for the Overview card.
        /// Returns null when the person is missing OR when the group
        /// type's <c>ShowAdministrator</c> flag is false (which hides
        /// the row). The link URL is server-resolved via
        /// <see cref="ResolveEntityUrl"/> so any customer-customized
        /// Person <c>LinkUrlLavaTemplate</c> is honored; the fallback is
        /// <c>/Person/{IdKey}</c>.
        /// </summary>
        private static GroupAdministratorBag BuildAdministratorRef( PersonAlias personAlias, GroupTypeCache groupType )
        {
            var person = personAlias?.Person;

            if ( person == null || groupType == null || !groupType.ShowAdministrator )
            {
                return null;
            }

            return new GroupAdministratorBag
            {
                Value = personAlias.Guid.ToString(),
                Text = person.FullName,
                Url = ResolveEntityUrl( typeof( Person ), person, fallbackUrl: $"/Person/{person.IdKey}" )
            };
        }

        /// <summary>
        /// Builds the parent group reference for the Overview card.
        /// Returns null when the group has no parent (the consuming row
        /// hides). The link URL is server-resolved via
        /// <see cref="ResolveEntityUrl"/> so any customer-customized
        /// Group <c>LinkUrlLavaTemplate</c> is honored; the fallback is
        /// <c>/Group/{IdKey}</c>.
        /// </summary>
        private static ParentGroupBag BuildParentGroupRef( Model.Group parentGroup )
        {
            if ( parentGroup == null )
            {
                return null;
            }

            return new ParentGroupBag
            {
                Value = parentGroup.Guid.ToString(),
                Text = parentGroup.Name,
                Url = ResolveEntityUrl( typeof( Model.Group ), parentGroup, fallbackUrl: $"/Group/{parentGroup.IdKey}" )
            };
        }

        /// <summary>
        /// Builds the group type reference for the panel-header chip.
        /// Returns null when no group type is supplied. <c>Url</c> is
        /// populated only when the current user has ADMINISTRATE on the
        /// group type (otherwise the chip renders as plain text). The URL
        /// is server-resolved via
        /// <see cref="ResolveEntityUrl(System.Type, object, string)"/>
        /// so any customer-customized GroupType <c>LinkUrlLavaTemplate</c>
        /// is honored; the fallback is the
        /// <c>/page/GroupTypeDetail?GroupTypeId={IdKey}</c> admin route.
        /// </summary>
        private GroupDetailGroupTypeBag BuildGroupTypeRef( Model.GroupType groupType )
        {
            if ( groupType == null )
            {
                return null;
            }

            var canAdministrate = groupType.IsAuthorized( Authorization.ADMINISTRATE, RequestContext.CurrentPerson );

            return new GroupDetailGroupTypeBag
            {
                Name = groupType.Name,
                Url = canAdministrate
                    ? ResolveEntityUrl(
                        typeof( Model.GroupType ),
                        groupType,
                        fallbackUrl: $"/page/GroupTypeDetail?GroupTypeId={Rock.Utility.IdHasher.Instance.GetHash( groupType.Id )}" )
                    : null,
                Color = groupType.GroupTypeColor
            };
        }

        /// <summary>
        /// Resolves a server-side detail URL for the supplied entity.
        /// Prefers the entity type's <c>LinkUrlLavaTemplate</c> (so
        /// customer-customized link rules are honored) with
        /// <paramref name="lavaEntity"/> merged in as <c>Entity</c>;
        /// rebases <c>~/</c> against the <c>InternalApplicationRoot</c>
        /// global attribute. Falls back to <paramref name="fallbackUrl"/>
        /// when the template is null / empty or resolves to whitespace.
        /// </summary>
        private static string ResolveEntityUrl( System.Type entityType, object lavaEntity, string fallbackUrl )
        {
            if ( lavaEntity == null )
            {
                return null;
            }

            var entityTypeCache = EntityTypeCache.Get( entityType );
            if ( !string.IsNullOrWhiteSpace( entityTypeCache?.LinkUrlLavaTemplate ) )
            {
                var mergeFields = new Dictionary<string, object>
                {
                    ["Entity"] = lavaEntity
                };

                var url = entityTypeCache.LinkUrlLavaTemplate.ResolveMergeFields( mergeFields );
                if ( !string.IsNullOrWhiteSpace( url ) )
                {
                    if ( url.StartsWith( "~/" ) )
                    {
                        var baseUrl = GlobalAttributesCache.Value( "InternalApplicationRoot" );
                        url = url.Replace( "~/", baseUrl.EnsureTrailingForwardslash() );
                    }
                    return url;
                }
            }

            return fallbackUrl;
        }

        /// <summary>
        /// Builds a <see cref="ListItemBag"/> reference for an
        /// <c>&lt;ImageUploader&gt;</c> bound BinaryFile. Returns null
        /// when no file is attached. Prefers a fully-loaded navigation
        /// property when available (avoids an extra fetch); falls back
        /// to a service lookup when only the FK is set.
        /// </summary>
        private ListItemBag BuildBinaryFileRef( BinaryFile attachedFile, int? binaryFileId )
        {
            if ( attachedFile != null )
            {
                return new ListItemBag
                {
                    Value = attachedFile.Guid.ToString(),
                    Text = attachedFile.FileName
                };
            }

            if ( !binaryFileId.HasValue )
            {
                return null;
            }

            var file = new BinaryFileService( RockContext ).GetSelect( binaryFileId.Value, bf => new { bf.Guid, bf.FileName } );
            if ( file == null )
            {
                return null;
            }

            return new ListItemBag
            {
                Value = file.Guid.ToString(),
                Text = file.FileName
            };
        }

        /// <summary>
        /// Builds a <see cref="ListItemBag"/> reference for the
        /// <c>&lt;CampusPicker&gt;</c>. The picker filters by Guid
        /// (per <c>CampusPickerGetCampuses</c>), so <c>Value</c> is the
        /// campus Guid via the canonical <c>IEntity.ToListItemBag()</c>
        /// extension. Returns null when <paramref name="campusId"/> is
        /// null or the cache lookup misses.
        /// </summary>
        private static ListItemBag BuildCampusListItem( int? campusId )
        {
            return campusId.HasValue
                ? CampusCache.Get( campusId.Value )?.ToListItemBag()
                : null;
        }

        /// <summary>
        /// Builds a <see cref="ListItemBag"/> reference for a
        /// <c>&lt;DefinedValuePicker&gt;</c>. The picker filters by
        /// Guid, so <c>Value</c> is the DefinedValue Guid via the
        /// canonical <c>IEntity.ToListItemBag()</c> extension. Returns
        /// null when <paramref name="definedValueId"/> is null or the
        /// cache lookup misses.
        /// </summary>
        private static ListItemBag BuildDefinedValueListItem( int? definedValueId )
        {
            return definedValueId.HasValue
                ? DefinedValueCache.Get( definedValueId.Value )?.ToListItemBag()
                : null;
        }

        /// <summary>
        /// Builds a <see cref="ListItemBag"/> reference for a
        /// <c>&lt;PersonPicker&gt;</c>. <c>Value</c> is the PersonAlias
        /// Guid emitted by the picker; <c>Text</c> is the person's
        /// friendly name. Returns null when the alias is null or the
        /// underlying person is unresolvable.
        /// </summary>
        private static ListItemBag BuildPersonAliasListItemBag( PersonAlias personAlias )
        {
            var person = personAlias?.Person;
            if ( person == null )
            {
                return null;
            }

            return new ListItemBag
            {
                Value = personAlias.Guid.ToString(),
                Text = person.FullName
            };
        }

        /// <summary>
        /// Hydrates the bag's Section 4 Stack 1 fields from the entity's
        /// <c>Schedule</c> navigation. Inline (string.Empty Name)
        /// schedules surface as Weekly or Custom per the Schedule's own
        /// type; named schedules surface their Id; null schedule
        /// surfaces as <c>ScheduleType.None</c>.
        /// </summary>
        private static void HydrateScheduleFields( GroupBag bag, Model.Group entity )
        {
            bag.ScheduleType = ScheduleType.None;
            bag.WeeklyDayOfWeek = null;
            bag.WeeklyTimeOfDay = null;
            bag.ICalendarContent = null;
            bag.NamedSchedule = null;

            var schedule = entity.Schedule;
            if ( schedule == null )
            {
                return;
            }

            switch ( schedule.ScheduleType )
            {
                case ScheduleType.Named:
                    bag.ScheduleType = ScheduleType.Named;
                    bag.NamedSchedule = schedule.ToListItemBag();
                    break;

                case ScheduleType.Custom:
                    bag.ScheduleType = ScheduleType.Custom;
                    bag.ICalendarContent = schedule.iCalendarContent;
                    break;

                case ScheduleType.Weekly:
                    bag.ScheduleType = ScheduleType.Weekly;
                    bag.WeeklyDayOfWeek = schedule.WeeklyDayOfWeek;
                    bag.WeeklyTimeOfDay = schedule.WeeklyTimeOfDay?.ToString();
                    break;
            }
        }

        /// <summary>
        /// Applies the photo BinaryFile change to the entity. Resolves
        /// the bag's ListItemBag (which carries the BinaryFile Guid) to
        /// a BinaryFile.Id and assigns to <c>Group.PhotoId</c>. Null
        /// clears the assignment.
        /// </summary>
        private void ApplyPhotoBinaryFile( Model.Group entity, GroupBag bag )
        {
            var newGuid = bag.PhotoBinaryFile?.Value.AsGuidOrNull();
            entity.PhotoId = newGuid.HasValue
                ? new BinaryFileService( RockContext ).GetSelect( newGuid.Value, bf => ( int? ) bf.Id )
                : null;
        }

        /// <summary>
        /// Applies the chat-channel-avatar BinaryFile change to the
        /// entity, gated on the chat feature being enabled and the
        /// active group type allowing chat. Mirrors the WebForms gate at
        /// <c>GroupDetail.ascx.cs:995</c>.
        /// </summary>
        private void ApplyChatChannelAvatarBinaryFile( Model.Group entity, GroupBag bag )
        {
            var groupType = GetGroupTypeCache( entity );
            if ( !ChatHelper.IsChatEnabled || groupType?.IsChatAllowed != true )
            {
                return;
            }

            var newGuid = bag.ChatChannelAvatarBinaryFile?.Value.AsGuidOrNull();
            entity.ChatChannelAvatarBinaryFileId = newGuid.HasValue
                ? new BinaryFileService( RockContext ).GetSelect( newGuid.Value, bf => ( int? ) bf.Id )
                : null;
        }

        /// <summary>
        /// Applies the inline-schedule lifecycle on save: gate-3 / gate-4
        /// downgrade Custom / Weekly to None when their required inputs
        /// are missing, then mutate <see cref="Model.Group.Schedule"/>
        /// or <see cref="Model.Group.ScheduleId"/> accordingly. Mirrors
        /// the WebForms inline-schedule cascade at
        /// <c>GroupDetail.ascx.cs:1184-1252</c>.
        /// </summary>
        private void ApplyInlineSchedule( Model.Group entity, GroupBag bag )
        {
            var scheduleType = bag.ScheduleType;

            // Validation gate 3 — Custom requires parseable iCal.
            if ( scheduleType == ScheduleType.Custom )
            {
                if ( bag.ICalendarContent.IsNullOrWhiteSpace() )
                {
                    scheduleType = ScheduleType.None;
                }
                else
                {
                    var calEvent = InetCalendarHelper.CreateCalendarEvent( bag.ICalendarContent );
                    if ( calEvent == null || calEvent.DtStart == null )
                    {
                        scheduleType = ScheduleType.None;
                    }
                }
            }

            // Validation gate 4 — Weekly requires a DayOfWeek.
            if ( scheduleType == ScheduleType.Weekly && !bag.WeeklyDayOfWeek.HasValue )
            {
                scheduleType = ScheduleType.None;
            }

            if ( scheduleType == ScheduleType.Custom || scheduleType == ScheduleType.Weekly )
            {
                // Reuse existing inline schedule when present; otherwise
                // create a new one with Name = string.Empty (the inline
                // marker per webforms/07).
                if ( entity.Schedule == null )
                {
                    entity.Schedule = new Schedule
                    {
                        Name = string.Empty
                    };
                }

                if ( scheduleType == ScheduleType.Custom )
                {
                    entity.Schedule.iCalendarContent = bag.ICalendarContent;
                    entity.Schedule.WeeklyDayOfWeek = null;
                    entity.Schedule.WeeklyTimeOfDay = null;
                }
                else // Weekly
                {
                    entity.Schedule.iCalendarContent = null;
                    entity.Schedule.WeeklyDayOfWeek = bag.WeeklyDayOfWeek;
                    entity.Schedule.WeeklyTimeOfDay = ParseTimeSpanOrNull( bag.WeeklyTimeOfDay );
                }
            }
            else if ( scheduleType == ScheduleType.Named )
            {
                var namedScheduleId = bag.NamedSchedule?.GetEntityId<Schedule>( RockContext );
                entity.ScheduleId = namedScheduleId;
                if ( namedScheduleId.HasValue )
                {
                    // Detach the EF reference; the navigation will refresh
                    // on the next read. Without this clear, EF treats the
                    // current Schedule navigation as the still-attached
                    // tracked entity and tries to update its FK.
                    entity.Schedule = null;
                }
            }
            else // None
            {
                entity.ScheduleId = null;
                entity.Schedule = null;
            }
        }

        /// <summary>
        /// Deletes the inline schedule referenced by
        /// <paramref name="oldScheduleId"/> when no other consumer holds
        /// it. Mirrors the WebForms cleanup at
        /// <c>GroupDetail.ascx.cs:1230-1242</c>. Named schedules are
        /// excluded by the <c>schedule.Name == string.Empty</c> check.
        /// </summary>
        private void DeleteInlineSchedule( int oldScheduleId )
        {
            var scheduleService = new ScheduleService( RockContext );
            var schedule = scheduleService.Get( oldScheduleId );
            if ( schedule == null || !string.IsNullOrEmpty( schedule.Name ) )
            {
                return;
            }

            if ( !scheduleService.CanDelete( schedule, out _ ) )
            {
                return;
            }

            scheduleService.Delete( schedule );
        }

        /// <summary>
        /// Toggles the <c>BinaryFile.IsTemporary</c> flag on the orphaned
        /// (true) and current (false) BinaryFiles per the chat-avatar
        /// pattern at webforms/14-chat.md, mirroring the same pattern
        /// for the photo. No-op when <paramref name="oldId"/> equals
        /// <paramref name="newId"/>.
        /// </summary>
        private void ToggleBinaryFileIsTemporary( int? oldId, int? newId )
        {
            if ( oldId == newId )
            {
                return;
            }

            var binaryFileService = new BinaryFileService( RockContext );

            if ( oldId.HasValue )
            {
                var orphanedFile = binaryFileService.Get( oldId.Value );
                if ( orphanedFile != null )
                {
                    orphanedFile.IsTemporary = true;
                }
            }

            if ( newId.HasValue )
            {
                var currentFile = binaryFileService.Get( newId.Value );
                if ( currentFile != null )
                {
                    currentFile.IsTemporary = false;
                }
            }
        }

        /// <summary>
        /// Parses an ISO-8601 time-of-day string (e.g., <c>"13:30:00"</c>)
        /// into a <see cref="TimeSpan"/>. Returns null on parse failure
        /// or whitespace input. The Vue layer's <c>&lt;TimePicker&gt;</c>
        /// emits this format.
        /// </summary>
        private static TimeSpan? ParseTimeSpanOrNull( string isoTime )
        {
            if ( isoTime.IsNullOrWhiteSpace() )
            {
                return null;
            }

            return TimeSpan.TryParse( isoTime, out var ts ) ? ts : ( TimeSpan? ) null;
        }

        /// <summary>
        /// Builds the Group Type dropdown payload for the Add panel. Uses
        /// the entity's parent group type (when known) as the "parent"
        /// filter argument so the dropdown only contains group types the
        /// parent allows as children.
        /// </summary>
        private List<ListItemBag> BuildAllowedGroupTypeListItems( Model.Group entity )
        {
            var parentGroupType = entity?.ParentGroup != null
                ? GroupTypeCache.Get( entity.ParentGroup.GroupTypeId )
                : null;

            return GetAllowedGroupTypes( parentGroupType, RockContext )
                .OrderBy( gt => gt.Order )
                .ThenBy( gt => gt.Name )
                .Select( gt => new { gt.Id, gt.Name } )
                .ToList()
                .Select( gt => new ListItemBag
                {
                    Value = gt.Id.ToString(),
                    Text = gt.Name
                } )
                .ToList();
        }

        /// <summary>
        /// Builds the Required Signature Document dropdown payload.
        /// Returns active templates plus the currently-bound template (if
        /// any) so an existing group bound to a deactivated template still
        /// surfaces its current value. Mirrors the post-modernization
        /// pattern at <c>RegistrationTemplateDetail.ascx.cs:2983</c>.
        /// </summary>
        private List<ListItemBag> BuildSignatureDocumentTemplateListItems( Model.Group entity )
        {
            var currentTemplateId = entity?.RequiredSignatureDocumentTemplateId;

            return new SignatureDocumentTemplateService( RockContext )
                .Queryable()
                .Where( t => t.IsActive || t.Id == currentTemplateId )
                .OrderBy( t => t.Name )
                .Select( t => new { t.Id, t.Name } )
                .ToList()
                .Select( t => new ListItemBag
                {
                    Value = t.Id.ToString(),
                    Text = t.Name
                } )
                .ToList();
        }

        /// <summary>
        /// Returns the <see cref="GroupType"/> queryable filtered by the
        /// block's <c>GroupTypes</c> / <c>GroupTypesExclude</c>
        /// attributes, the parent group's allowed child group types,
        /// the <c>LimittoSecurityRoleGroups</c> attribute, and the
        /// <c>LimitToShowInNavigationGroupTypes</c> attribute. Mirrors
        /// the WebForms helper at
        /// <c>GroupDetail.ascx.cs:2939-2976</c>.
        /// </summary>
        /// <remarks>
        /// 5/9/2026 - CLAUDE
        ///
        /// Within a single page-load this is invoked from
        /// <see cref="BuildAllowedGroupTypeListItems"/>; within a single
        /// Save it's invoked from validation gate 5; for the Add-from-
        /// tree path <see cref="ApplyNewGroupDefaultValues"/> also calls
        /// it (and within that loop, hits <see cref="Model.Group.IsAuthorized"/>
        /// once per allowed type). Each call is a fresh DB query — the
        /// cache-backed <c>GroupTypeCache.Get</c> calls inside the
        /// authorization probe minimize the per-iteration cost.
        ///
        /// Per-request memoization (similar to <c>_cachedGroupType</c>)
        /// is the natural future optimization if profiling shows this
        /// in the hot path. Keyed by <c>parentGroupGroupType?.Id</c>.
        ///
        /// Reason: keep the queryable signature simple; memoization is
        /// available if needed but not yet justified.
        /// </remarks>
        private IQueryable<Model.GroupType> GetAllowedGroupTypes( GroupTypeCache parentGroupGroupType, RockContext rockContext )
        {
            var groupTypeService = new GroupTypeService( rockContext );
            var groupTypeQry = groupTypeService.Queryable();

            // Block attribute include/exclude.
            var includeGuids = GetAttributeValue( AttributeKey.GroupTypes ).SplitDelimitedValues().AsGuidList();
            var excludeGuids = GetAttributeValue( AttributeKey.GroupTypesExclude ).SplitDelimitedValues().AsGuidList();
            if ( includeGuids.Any() )
            {
                groupTypeQry = groupTypeQry.Where( a => includeGuids.Contains( a.Guid ) );
            }
            else if ( excludeGuids.Any() )
            {
                groupTypeQry = groupTypeQry.Where( a => !excludeGuids.Contains( a.Guid ) );
            }

            // Parent group type's allowed child group types.
            if ( parentGroupGroupType != null && !parentGroupGroupType.AllowAnyChildGroupType )
            {
                var allowedChildGroupTypeIds = parentGroupGroupType.ChildGroupTypes.Select( a => a.Id ).ToList();
                groupTypeQry = groupTypeQry.Where( a => allowedChildGroupTypeIds.Contains( a.Id ) );
            }

            // LimitToShowInNavigationGroupTypes.
            if ( GetAttributeValue( AttributeKey.LimitToShowInNavigationGroupTypes ).AsBoolean() )
            {
                groupTypeQry = groupTypeQry.Where( a => a.ShowInNavigation );
            }

            // LimittoSecurityRoleGroups.
            if ( GetAttributeValue( AttributeKey.LimittoSecurityRoleGroups ).AsBoolean() )
            {
                var securityRoleGroupTypeId = GroupTypeCache.GetId( Rock.SystemGuid.GroupType.GROUPTYPE_SECURITY_ROLE.AsGuid() );
                if ( securityRoleGroupTypeId.HasValue )
                {
                    groupTypeQry = groupTypeQry.Where( a => a.Id == securityRoleGroupTypeId.Value );
                }
            }

            return groupTypeQry;
        }

        /// <summary>
        /// Resolves <see cref="GroupTypeOptionsBag"/> for the supplied
        /// group type id. The Vue side calls this on each cascade.
        /// Mirrors the WebForms <c>ShowGroupTypeEditDetails</c>
        /// rebinding at <c>GroupDetail.ascx.cs:2173-2295</c> but emits
        /// a typed bag instead of mutating UI state.
        /// </summary>
        private GroupTypeOptionsBag BuildGroupTypeOptionsBag( int groupTypeId )
        {
            var bag = new GroupTypeOptionsBag
            {
                StatusValues = new List<ListItemBag>(),
                InactiveReasons = new List<ListItemBag>(),
                InheritedMemberAttributes = new List<PublicAttributeBag>()
            };

            if ( groupTypeId <= 0 )
            {
                return bag;
            }

            var groupType = GroupTypeCache.Get( groupTypeId );
            if ( groupType == null )
            {
                return bag;
            }

            // Visibility flags.
            bag.IsRsvpSectionVisible = groupType.EnableRSVP;
            bag.IsChatSectionVisible = ChatHelper.IsChatEnabled && groupType.IsChatAllowed;
            bag.IsSchedulingSectionVisible = ( groupType.AllowedScheduleTypes & ( ScheduleType.Weekly | ScheduleType.Custom | ScheduleType.Named ) ) != 0;
            bag.IsPeerNetworkSectionVisible = groupType.IsPeerNetworkEnabled;
            bag.IsAdministratorVisible = groupType.ShowAdministrator;
            bag.IsGroupSpecificRecordSourceVisible = groupType.AllowGroupSpecificRecordSource;
            bag.IsScheduleConfirmationLogicVisible = groupType.IsSchedulingEnabled;
            bag.IsScheduleCoordinatorVisible = groupType.IsSchedulingEnabled;
            bag.IsCoordinatorNotificationsVisible = groupType.IsSchedulingEnabled;
            bag.IsCheckInRequirementsVisible = groupType.TakesAttendance;
            bag.IsGroupCapacityVisible = groupType.GroupCapacityRule != GroupCapacityRule.None;
            bag.IsGroupCapacityRequired = groupType.IsCapacityRequired;
            bag.IsInactiveReasonVisible = groupType.EnableInactiveReason;
            bag.IsInactiveReasonRequired = groupType.RequiresInactiveReason;
            bag.IsStatusVisible = groupType.GroupStatusDefinedTypeId.HasValue;
            bag.RequiresCampus = groupType.GroupsRequireCampus;

            // Allowed flags.
            bag.AllowedScheduleTypes = groupType.AllowedScheduleTypes;
            bag.LocationSelectionMode = groupType.LocationSelectionMode;
            bag.EnableLocationSchedules = groupType.EnableLocationSchedules ?? false;
            bag.IsSchedulingEnabled = groupType.IsSchedulingEnabled;

            // Localization.
            bag.AdministratorTerm = groupType.AdministratorTerm.IsNotNullOrWhiteSpace() ? groupType.AdministratorTerm : "Administrator";
            bag.IconCssClass = groupType.IconCssClass;

            // Peer network defaults / placeholders.
            bag.RelationshipStrengthDefault = ( RelationshipStrength ) groupType.RelationshipStrength;
            bag.RelationshipGrowthEnabledDefault = groupType.RelationshipGrowthEnabled;
            bag.LeaderToLeaderMultiplierDefault = groupType.LeaderToLeaderRelationshipMultiplier;
            bag.LeaderToNonLeaderMultiplierDefault = groupType.LeaderToNonLeaderRelationshipMultiplier;
            bag.NonLeaderToLeaderMultiplierDefault = groupType.NonLeaderToLeaderRelationshipMultiplier;
            bag.NonLeaderToNonLeaderMultiplierDefault = groupType.NonLeaderToNonLeaderRelationshipMultiplier;

            // RSVP pinned values (group-type wins; null = group can override).
            bag.RsvpReminderOffsetDays = groupType.RSVPReminderOffsetDays;
            if ( groupType.RSVPReminderSystemCommunicationId.HasValue )
            {
                bag.RsvpReminderSystemCommunicationGuid = new SystemCommunicationService( RockContext )
                    .GetSelect( groupType.RSVPReminderSystemCommunicationId.Value, c => ( Guid? ) c.Guid );
            }

            // Status defined values.
            if ( groupType.GroupStatusDefinedTypeId.HasValue )
            {
                var definedType = DefinedTypeCache.Get( groupType.GroupStatusDefinedTypeId.Value );
                if ( definedType != null )
                {
                    bag.StatusValues = definedType.DefinedValues
                        .Where( dv => dv.IsActive )
                        .OrderBy( dv => dv.Order )
                        .Select( dv => new ListItemBag
                        {
                            Value = dv.Id.ToString(),
                            Text = dv.Value
                        } )
                        .ToList();
                }
            }

            // Inactive reasons.
            if ( groupType.EnableInactiveReason )
            {
                bag.InactiveReasons = new GroupTypeService( RockContext )
                    .GetInactiveReasonsForGroupType( groupType.Id )
                    .Select( dv => new ListItemBag
                    {
                        Value = dv.Id.ToString(),
                        Text = dv.Value
                    } )
                    .ToList();
            }

            return bag;
        }

        /// <summary>
        /// Builds the Linkages payload for the Overview card. Returns
        /// null when the group has no linkages of any kind, which omits
        /// the entire section in the view.
        /// </summary>
        /// <remarks>
        /// 5/6/2026 - CLAUDE
        ///
        /// The Phase 1 spec checklist item 49 references
        /// <c>ContentChannelItemAssociation</c> but that table holds only
        /// parent/child relationships between two content items, not
        /// group-to-content-item associations. The actual link path is
        /// Group.Linkages → EventItemOccurrenceGroupMap → EventItemOccurrence
        /// → EventItemOccurrenceChannelItem → ContentChannelItem, which is
        /// what the WebForms default Lava template walks. Logged as a
        /// mid-phase decision.
        ///
        /// Reason: the spec entity reference was incorrect.
        /// </remarks>
        private GroupLinkagesBag BuildLinkages( Model.Group entity )
        {
            if ( entity == null || entity.Id == 0 )
            {
                return null;
            }

            // EventItemOccurrenceGroupMap rows are the basis for both
            // registrations and event item occurrences (and indirectly
            // content items via EventItemOccurrenceChannelItem). One query
            // surfaces all three.
            var linkageRows = new EventItemOccurrenceGroupMapService( RockContext ).Queryable()
                .AsNoTracking()
                .Include( l => l.RegistrationInstance )
                .Include( l => l.EventItemOccurrence )
                .Include( l => l.EventItemOccurrence.EventItem )
                .Include( l => l.EventItemOccurrence.ContentChannelItems.Select( c => c.ContentChannelItem ) )
                .Where( l => l.GroupId == entity.Id )
                .ToList();

            // Per Q4, every outbound Id-style page parameter is IdKey -
            // not only GroupId. Each linkage URL emits the receiving
            // entity's IdKey instead of its integer Id. The Obsidian
            // RegistrationInstanceDetail and the WebForms
            // EventItemOccurrenceDetail / ContentChannelItemDetail all
            // accept IdKey on their corresponding page parameters today
            // (verified during the Q4 audit).
            var registrations = linkageRows
                .Where( l => l.RegistrationInstance != null )
                .Select( l => l.RegistrationInstance )
                .GroupBy( r => r.Id )
                .Select( g => g.First() )
                .Select( r => new GroupLinkageBag
                {
                    Name = r.Name,
                    Url = this.GetLinkedPageUrl( AttributeKey.RegistrationInstancePage, "RegistrationInstanceId", r.IdKey )
                } )
                .Where( l => l.Url.IsNotNullOrWhiteSpace() )
                .ToList();

            var eventItemOccurrences = linkageRows
                .Where( l => l.EventItemOccurrence != null && l.EventItemOccurrence.EventItem != null )
                .Select( l => l.EventItemOccurrence )
                .GroupBy( e => e.Id )
                .Select( g => g.First() )
                .Select( e => new GroupLinkageBag
                {
                    Name = e.EventItem.Name,
                    Url = this.GetLinkedPageUrl( AttributeKey.EventItemOccurrencePage, "EventItemOccurrenceId", e.IdKey )
                } )
                .Where( l => l.Url.IsNotNullOrWhiteSpace() )
                .ToList();

            var contentItems = linkageRows
                .Where( l => l.EventItemOccurrence != null )
                .SelectMany( l => l.EventItemOccurrence.ContentChannelItems )
                .Select( c => c.ContentChannelItem )
                .Where( c => c != null )
                .GroupBy( c => c.Id )
                .Select( g => g.First() )
                .Select( c => new GroupLinkageBag
                {
                    Name = c.Title,
                    Url = this.GetLinkedPageUrl( AttributeKey.ContentItemPage, "ContentItemId", c.IdKey )
                } )
                .Where( l => l.Url.IsNotNullOrWhiteSpace() )
                .ToList();

            if ( !registrations.Any() && !eventItemOccurrences.Any() && !contentItems.Any() )
            {
                return null;
            }

            return new GroupLinkagesBag
            {
                Registrations = registrations,
                EventItemOccurrences = eventItemOccurrences,
                ContentItems = contentItems
            };
        }

        /// <summary>
        /// Builds the per-<c>GroupLocation</c> Meeting Location card data
        /// rendered on the right rail of the View panel. Always emits a
        /// (possibly empty) list so the Vue layer can decide whether to
        /// render the card via a length check. Card-level visibility:
        /// the entire card omits when the list is empty.
        ///
        /// Card variants per Q2.5 / spec C4:
        ///   <list type="bullet">
        ///     <item><c>GroupMember</c> when <c>GroupMemberPersonAliasId</c> is set; address resolves from the family Location.</item>
        ///     <item><c>Polygon</c> when <c>Location.GeoFence</c> is set; address suppressed.</item>
        ///     <item><c>Point</c> when <c>Location.GeoPoint</c> is set; address from <c>FormattedAddress</c>.</item>
        ///     <item><c>Address</c> otherwise; address from <c>FormattedAddress</c>; map shows only if a coordinate is present.</item>
        ///   </list>
        ///
        /// <c>ShowLocationAddresses</c> (block attribute, default true) gates
        /// the address text uniformly across every card variant per Q2.6:
        /// when false, no card renders an address regardless of mode.
        /// Polygon cards always render no address regardless of the flag.
        /// Per Q2.3 every card on a given group shares the same
        /// <see cref="GroupMeetingLocationBag.MapUrl"/> pointing at
        /// <c>GroupMapPage?GroupId={IdKey}</c>.
        /// </summary>
        private List<GroupMeetingLocationBag> BuildMeetingLocations( Model.Group entity )
        {
            if ( entity == null || entity.Id == 0 )
            {
                return new List<GroupMeetingLocationBag>();
            }

            var groupLocations = new GroupLocationService( RockContext ).Queryable()
                .AsNoTracking()
                .Include( gl => gl.Location )
                .Include( gl => gl.Schedules )
                .Where( gl => gl.GroupId == entity.Id )
                .OrderBy( gl => gl.Order )
                .ThenBy( gl => gl.Id )
                .ToList();

            if ( !groupLocations.Any() )
            {
                return new List<GroupMeetingLocationBag>();
            }

            var showAddresses = GetAttributeValue( AttributeKey.ShowLocationAddresses ).AsBoolean( true );

            // Q2.3: per-card MapUrl is the same group-level URL across all
            // cards on this group. Build once with the entity's IdKey
            // substituted (per Q4 outbound IdKey policy) so the Vue layer
            // doesn't need to substitute.
            var mapUrl = this.GetLinkedPageUrl(
                AttributeKey.GroupMapPage,
                new Dictionary<string, string>
                {
                    [PageParameterKey.GroupId] = entity.IdKey
                } );

            return groupLocations
                .Select( gl => BuildMeetingLocationBag( gl, showAddresses, mapUrl ) )
                .ToList();
        }

        /// <summary>
        /// Materializes one <see cref="GroupMeetingLocationBag"/> from a
        /// <see cref="GroupLocation"/>, applying the four-mode
        /// classification and the <c>ShowLocationAddresses</c> gate. The
        /// <c>MapData</c> field carries raw Well-Known Text that
        /// <c>@Obsidian/Utility/geo</c>'s <c>wellKnownToCoordinates</c>
        /// parses on the Vue side.
        /// </summary>
        private static GroupMeetingLocationBag BuildMeetingLocationBag( GroupLocation gl, bool showAddresses, string mapUrl )
        {
            var location = gl.Location;
            var hasGeoFence = location?.GeoFence != null;
            var hasGeoPoint = location?.GeoPoint != null;

            // GroupMember > Polygon > Point > Address. The classification
            // matches the WebForms research (07-locations-and-schedules.md):
            // GroupMember is set by the Member tab regardless of the
            // underlying Location's geo state, so it takes priority over
            // the geo-derived modes.
            GroupLocationPickerMode mode;
            if ( gl.GroupMemberPersonAliasId.HasValue )
            {
                mode = GroupLocationPickerMode.GroupMember;
            }
            else if ( hasGeoFence )
            {
                mode = GroupLocationPickerMode.Polygon;
            }
            else if ( hasGeoPoint )
            {
                mode = GroupLocationPickerMode.Point;
            }
            else
            {
                mode = GroupLocationPickerMode.Address;
            }

            // Polygon cards never render an address (the design renders the
            // shape itself plus a "Geofenced Location" label). Other modes
            // render the formatted address conditional on the
            // ShowLocationAddresses block attribute.
            string address = null;
            if ( mode != GroupLocationPickerMode.Polygon && showAddresses )
            {
                address = location?.FormattedAddress;
            }

            // Polygon cards use the GeoFence WKT; everything else uses the
            // GeoPoint WKT. Empty string when no geo data is available -
            // the Vue side renders the map but no marker / shape.
            string mapData;
            if ( mode == GroupLocationPickerMode.Polygon )
            {
                mapData = location?.GeoFence?.AsText() ?? string.Empty;
            }
            else
            {
                mapData = location?.GeoPoint?.AsText() ?? string.Empty;
            }

            // Schedule text takes the first attached schedule's friendly
            // text. Multi-schedule locations show only the first per the
            // captured design; the editing-side Phase 6 surface manages
            // the full schedule list.
            var scheduleText = gl.Schedules
                .OrderBy( s => s.Order )
                .ThenBy( s => s.Id )
                .Select( s => s.FriendlyScheduleText )
                .FirstOrDefault( t => t.IsNotNullOrWhiteSpace() );

            return new GroupMeetingLocationBag
            {
                Guid = gl.Guid,
                Name = location?.Name,
                Address = address,
                ScheduleText = scheduleText,
                Mode = mode,
                MapData = mapData,
                MapUrl = mapUrl
            };
        }

        /// <summary>
        /// Validates a candidate <c>returnUrl</c> page parameter is
        /// same-origin before any redirect uses it. Treats null /
        /// whitespace as "no return URL" (caller falls through to default
        /// navigation) and accepts relative paths (no scheme, no host).
        /// Absolute URLs are accepted only when their host matches the
        /// current request host. Closes the L6 open-redirect risk per
        /// 00-architecture.md Q12.
        /// </summary>
        private bool IsSafeReturnUrl( string url )
        {
            if ( url.IsNullOrWhiteSpace() )
            {
                return true;
            }

            if ( !Uri.TryCreate( url, UriKind.RelativeOrAbsolute, out var parsedUri ) )
            {
                return false;
            }

            if ( !parsedUri.IsAbsoluteUri )
            {
                // Reject protocol-relative ("//attacker.example/...") URLs
                // that some parsers treat as relative.
                return !url.StartsWith( "//", StringComparison.Ordinal );
            }

            var requestHost = RequestContext?.RequestUri?.Host;
            return requestHost.IsNotNullOrWhiteSpace()
                && string.Equals( parsedUri.Host, requestHost, StringComparison.OrdinalIgnoreCase );
        }

        /// <summary>
        /// Computes the post-Delete / post-Archive redirect URL using the
        /// same precedence as the WebForms block at
        /// <see href="../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L735"/>:
        /// honor a same-origin <c>returnUrl</c> first; otherwise build a
        /// query string with the parent group's IdKey plus
        /// <c>ExpandedIds</c> and resolve <c>GroupListPage</c>; otherwise
        /// reload the current page.
        /// </summary>
        private string NavigateAfterDeleteOrArchive( int? parentGroupId )
        {
            var returnUrl = PageParameter( PageParameterKey.ReturnUrl );
            if ( returnUrl.IsNotNullOrWhiteSpace() && IsSafeReturnUrl( returnUrl ) )
            {
                return returnUrl;
            }

            var qryParams = new Dictionary<string, string>();
            if ( parentGroupId.HasValue && parentGroupId.Value > 0 )
            {
                qryParams[PageParameterKey.GroupId] = Rock.Utility.IdHasher.Instance.GetHash( parentGroupId.Value );
            }

            var expandedIds = PageParameter( PageParameterKey.ExpandedIds );
            if ( expandedIds.IsNotNullOrWhiteSpace() )
            {
                qryParams[PageParameterKey.ExpandedIds] = expandedIds;
            }

            if ( GetAttributeValue( AttributeKey.GroupListPage ).AsGuid() != Guid.Empty )
            {
                return this.GetLinkedPageUrl( AttributeKey.GroupListPage, qryParams );
            }

            return this.GetCurrentPageUrl( qryParams );
        }

        #endregion Helper Methods
    }
}
