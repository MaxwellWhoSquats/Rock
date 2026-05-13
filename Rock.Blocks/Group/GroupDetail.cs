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
using Rock.ViewModels.Controls;
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
            box.QualifiedAttributeProperties = AttributeCache.GetAttributeQualifiedColumns<Model.Group>();

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
            bag.RsvpReminderSystemCommunication = BuildSystemCommunicationListItem( entity.RSVPReminderSystemCommunicationId );

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

            bag.LoadAttributesAndValuesForPublicEdit( entity, RequestContext.CurrentPerson, enforceSecurity: true );

            bag.GroupMemberAttributes = LoadGroupMemberAttributes( entity );

            // Section 7 / 9 / 10 — Phase 5 list payloads.
            bag.GroupRequirements = LoadGroupRequirements( entity );
            bag.GroupSyncs = LoadGroupSyncs( entity );
            bag.GroupMemberWorkflowTriggers = LoadGroupMemberWorkflowTriggers( entity );

            // Section 4 Stack 2 — Phase 6 location payloads.
            bag.GroupLocations = LoadGroupLocations( entity );
            bag.FamilyMemberLocationOptions = BuildFamilyMemberLocationOptions( entity );

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

            // Peer Network overrides (S13) - only when the group type
            // enables peer-network AND the user checks the override box.
            // When the user opts to override but selects None as the strength,
            // the growth flag and matrix multipliers must also be nulled so
            // stale UI state from a previously chosen non-None strength does
            // not leak through.
            box.IfValidProperty( nameof( box.Bag.OverrideRelationshipStrength ), () =>
            {
                var isPeerNetworkEnabled = groupType?.IsPeerNetworkEnabled == true;
                if ( !isPeerNetworkEnabled )
                {
                    return;
                }

                var bagStrength = box.Bag.RelationshipStrengthOverride ?? RelationshipStrength.None;
                var isStrengthOverridden = box.Bag.OverrideRelationshipStrength
                    && bagStrength != RelationshipStrength.None;

                if ( box.Bag.OverrideRelationshipStrength )
                {
                    // User wants an override; persist the chosen strength
                    // (which may legitimately be None).
                    entity.RelationshipStrengthOverride = ( int ) bagStrength;
                }
                else
                {
                    // No override at all; fall back to group type default.
                    entity.RelationshipStrengthOverride = null;
                }

                // Growth and the matrix only apply when a non-None strength
                // is actively overridden.
                entity.RelationshipGrowthEnabledOverride = isStrengthOverridden
                    ? box.Bag.RelationshipGrowthEnabledOverride
                    : null;
                entity.LeaderToLeaderRelationshipMultiplierOverride = isStrengthOverridden
                    ? box.Bag.LeaderToLeaderRelationshipMultiplierOverride
                    : null;
                entity.LeaderToNonLeaderRelationshipMultiplierOverride = isStrengthOverridden
                    ? box.Bag.LeaderToNonLeaderRelationshipMultiplierOverride
                    : null;
                entity.NonLeaderToLeaderRelationshipMultiplierOverride = isStrengthOverridden
                    ? box.Bag.NonLeaderToLeaderRelationshipMultiplierOverride
                    : null;
                entity.NonLeaderToNonLeaderRelationshipMultiplierOverride = isStrengthOverridden
                    ? box.Bag.NonLeaderToNonLeaderRelationshipMultiplierOverride
                    : null;
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

            box.IfValidProperty( nameof( box.Bag.RsvpReminderSystemCommunication ), () =>
            {
                if ( groupType?.EnableRSVP == true )
                {
                    entity.RSVPReminderSystemCommunicationId = groupType.RSVPReminderSystemCommunicationId.HasValue
                        ? ( int? ) null
                        : box.Bag.RsvpReminderSystemCommunication?.GetEntityId<SystemCommunication>( RockContext );
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

            box.IfValidProperty( nameof( box.Bag.AttributeValues ), () =>
            {
                entity.LoadAttributes( RockContext );
                entity.SetPublicAttributeValues( box.Bag.AttributeValues, RequestContext.CurrentPerson, enforceSecurity: true );
            } );

            return true;
        }

        /// <summary>
        /// Performs cross-field / collection-level validation that
        /// cannot be expressed by <c>Group.IsValid</c> alone.
        /// </summary>
        /// <param name="group">The group entity (already mutated from the bag).</param>
        /// <param name="bag">The bag containing the data from the client.</param>
        /// <param name="errorMessage">On <c>false</c> return, contains the error message.</param>
        /// <returns><c>true</c> if the bag passes Group-specific validation, <c>false</c> otherwise.</returns>
        private bool ValidateGroup( Model.Group group, GroupBag bag, out string errorMessage )
        {
            errorMessage = null;

            if ( group == null || bag == null )
            {
                return true;
            }

            // A Group Type must be chosen. Nothing else on the entity
            // is meaningful without one, so this fires first.
            if ( group.GroupTypeId <= 0 )
            {
                errorMessage = WarningMessage.CannotBeBlank( Model.GroupType.FriendlyTypeName );
                return false;
            }

            // A saved group cannot list itself as its own parent.
            if ( group.Id != 0 && group.ParentGroupId == group.Id )
            {
                errorMessage = "Group cannot be a Parent Group of itself.";
                return false;
            }

            // The chosen Group Type must be in the parent group's
            // AllowedChildGroupTypes list (when there is a parent).
            // Reuses the already-loaded ParentGroup navigation when
            // available (e.g. when ApplyNewGroupDefaultValues
            // pre-populated it on the Add-from-tree path) to avoid a
            // redundant query.
            if ( group.ParentGroupId.HasValue )
            {
                var parentGroup = group.ParentGroup ?? new GroupService( RockContext ).Get( group.ParentGroupId.Value );
                if ( parentGroup != null )
                {
                    var allowedGroupTypeIds = GetAllowedGroupTypes( GroupTypeCache.Get( parentGroup.GroupTypeId ), RockContext )
                        .Select( gt => gt.Id )
                        .ToList();
                    if ( !allowedGroupTypeIds.Contains( group.GroupTypeId ) )
                    {
                        var groupTypeForError = GroupTypeCache.Get( group.GroupTypeId );
                        errorMessage = $"The '{parentGroup.Name}' group does not allow child groups with a '{groupTypeForError?.Name ?? string.Empty}' group type.";
                        return false;
                    }
                }
            }

            // Re-check EDIT now that GroupType / ParentGroup may have
            // been swapped: the initial EDIT auth granted at block
            // entry may no longer apply.
            if ( !group.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) )
            {
                errorMessage = $"Not authorized to edit {Model.Group.FriendlyTypeName}.";
                return false;
            }

            // Model-level rules (e.g. GroupsRequireCampus). Runs
            // before the collection checks below so a fundamentally
            // unsaveable entity surfaces its own error first.
            if ( !group.IsValid )
            {
                errorMessage = group.ValidationResults
                    .Select( r => r.ErrorMessage )
                    .ToList()
                    .AsDelimited( "; " );
                return false;
            }

            // Every Group Sync row must carry its required foreign keys.
            // The modal enforces this with rules="required" on Role and
            // DataView, but the server refuses to silently coerce missing
            // values to id 0, which would otherwise persist an orphaned
            // sync row.
            foreach ( var sync in bag.GroupSyncs ?? new List<GroupSyncBag>() )
            {
                if ( sync.GroupTypeRole?.Value.IsNullOrWhiteSpace() != false )
                {
                    errorMessage = "Each group sync rule must specify a sync role.";
                    return false;
                }

                if ( sync.SyncDataView?.Value.IsNullOrWhiteSpace() != false )
                {
                    errorMessage = "Each group sync rule must specify a sync data view.";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Synchronizes related entities by comparing existing entities
        /// with incoming bags, deleting removed items, and adding or
        /// updating per the incoming list. Mirrors the canonical
        /// pattern at <c>GroupTypeDetail.cs:760</c>.
        /// </summary>
        private void SyncRelatedEntities<TEntity, TBag, TKey>(
            Service<TEntity> service,
            IQueryable<TEntity> existingEntitiesQuery,
            IEnumerable<TBag> incomingBags,
            Func<TEntity, TKey> existingKeySelector,
            Func<TBag, TKey> incomingKeySelector,
            Func<TBag, TEntity> createNew,
            Action<TEntity, TBag> updateEntity )
            where TEntity : Entity<TEntity>, new()
        {
            var existingEntities = existingEntitiesQuery.ToList();
            var existingByKey = existingEntities.ToDictionary( existingKeySelector );

            var incomingList = ( incomingBags ?? Enumerable.Empty<TBag>() ).ToList();
            var incomingKeys = incomingList.Select( incomingKeySelector ).ToHashSet();

            foreach ( var entity in existingEntities.Where( e => !incomingKeys.Contains( existingKeySelector( e ) ) ).ToList() )
            {
                service.Delete( entity );
            }

            foreach ( var bag in incomingList )
            {
                var key = incomingKeySelector( bag );

                if ( !existingByKey.TryGetValue( key, out var entity ) )
                {
                    entity = createNew( bag );
                    service.Add( entity );
                }

                updateEntity( entity, bag );
            }
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

        /// <summary>
        /// Loads the editable per-group member attribute definitions
        /// for the supplied entity. Returns an empty list for new groups
        /// (Id == 0) since the qualifier value depends on the persisted
        /// Id. Mirrors the WebForms <c>ShowEditDetails</c> hydration at
        /// <c>GroupDetail.ascx.cs:2117-2126</c>.
        /// </summary>
        private List<PublicEditableAttributeBag> LoadGroupMemberAttributes( Model.Group entity )
        {
            if ( entity == null || entity.Id == 0 )
            {
                return new List<PublicEditableAttributeBag>();
            }

            var attributeService = new AttributeService( RockContext );
            var qualifierValue = entity.Id.ToString();

            return attributeService.GetByEntityTypeId( new GroupMember().TypeId, true )
                .AsNoTracking()
                .Where( a =>
                    a.EntityTypeQualifierColumn.Equals( "GroupId", StringComparison.OrdinalIgnoreCase ) &&
                    a.EntityTypeQualifierValue.Equals( qualifierValue ) )
                .OrderBy( a => a.Order )
                .ThenBy( a => a.Name )
                .ToList()
                .ConvertAll( a => PublicAttributeHelper.GetPublicEditableAttribute( a ) );
        }

        /// <summary>
        /// Saves the per-group member attribute definitions for the
        /// specified qualifier.
        /// </summary>
        /// <param name="qualifierColumn">The attribute qualifier column.</param>
        /// <param name="qualifierValue">The qualifier value.</param>
        /// <param name="attributes">The attributes as edited in the UI.</param>
        private void SaveGroupMemberAttributes( string qualifierColumn, string qualifierValue, List<PublicEditableAttributeBag> attributes )
        {
            if ( attributes == null )
            {
                return;
            }

            var entityTypeId = new GroupMember().TypeId;

            // Get the existing attributes for this entity type and qualifier value
            var attributeService = new AttributeService( RockContext );
            var existingAttributes = attributeService.GetByEntityTypeQualifier( entityTypeId, qualifierColumn, qualifierValue, true ).ToList();

            // Delete any of those attributes that were removed in the UI
            var remainingAttributeGuids = attributes.Select( a => a.Guid );
            foreach ( var attr in existingAttributes.Where( a => !remainingAttributeGuids.Contains( a.Guid ) ) )
            {
                attributeService.Delete( attr );
                RockContext.SaveChanges();
            }

            // The attributes are coming from the frontend already sorted in the correct order.
            int attributeOrder = 0;
            foreach ( var attrBag in attributes )
            {
                var attr = Helper.SaveAttributeEdits( attrBag, entityTypeId, qualifierColumn, qualifierValue, RockContext );
                if ( attr != null )
                {
                    attr.Order = attributeOrder++;
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

            // Apply scalar field assignments.
            if ( !UpdateEntityFromBox( entity, box ) )
            {
                return ActionBadRequest( "Invalid data." );
            }

            // Apply photo / chat-avatar / inline-schedule mutations
            // outside the IfValidProperty pattern because each requires
            // contextual state (orphan tracking, schedule lifecycle) that
            // doesn't fit the partial-update pattern.
            ApplyPhotoBinaryFile( entity, box.Bag );
            ApplyChatChannelAvatarBinaryFile( entity, box.Bag );
            ApplyInlineSchedule( entity, box.Bag );

            // Validation
            if ( !ValidateGroup( entity, box.Bag, out var validationMessage ) )
            {
                return ActionBadRequest( validationMessage );
            }

            var isNew = entity.Id == 0;
            var addAdministrateSecurity = isNew
                && GetAttributeValue( AttributeKey.AddAdministrateSecurityToGroupCreator ).AsBoolean();
            var triggersUpdated = false;
            var checkinDataUpdated = false;

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

                // Step 4a — Persist Group attribute values (Section 5).
                // Mirrors WebForms GroupDetail.ascx.cs:1336. SaveAttributeValues
                // calls SaveChanges internally; the WrapTransaction keeps every
                // SaveChanges in this block atomic.
                entity.SaveAttributeValues( RockContext );

                // Step 4b — Sync per-group member attribute definitions
                // (Section 6). Mirrors WebForms GroupDetail.ascx.cs:1338-1357.
                SaveGroupMemberAttributes( "GroupId", entity.Id.ToString(), box.Bag.GroupMemberAttributes );

                // Step 4c — Sync per-group Group Requirements
                // (Section 7). Deferred-insert pattern; group.Id is
                // already assigned by step 3. Mirrors WebForms
                // GroupDetail.ascx.cs:845-886 + 1330-1334.
                SaveGroupRequirements( entity, box.Bag.GroupRequirements );

                // Step 4d — Sync per-group Group Syncs (Section 9).
                // Mirrors WebForms GroupDetail.ascx.cs:861-867 +
                // 1011-1022.
                SaveGroupSyncs( entity, box.Bag.GroupSyncs );

                // Step 4e — Sync per-group Member Workflow Triggers
                // (Section 10). Tracks whether any add/update/delete
                // occurred so the post-transaction
                // RemoveCachedTriggers() invalidation fires. Mirrors
                // WebForms GroupDetail.ascx.cs:853-858 + 1024-1041.
                if ( SaveGroupMemberWorkflowTriggers( entity, box.Bag.GroupMemberWorkflowTriggers ) )
                {
                    triggersUpdated = true;
                }

                // Step 4f — Sync per-group Group Locations (Section 4
                // Stack 2). Encapsulates the GroupLocationScheduleConfig
                // diff + GroupMemberAssignment cleanup + inactive-
                // schedule preservation + Location resolution. Mirrors
                // WebForms GroupDetail.ascx.cs:810-991 (delete-then-
                // upsert) per Q6.5 lock. Sets checkinDataUpdated when
                // any GroupLocation change occurred so the post-
                // transaction KioskDevice.Clear() fires.
                if ( SaveGroupLocations( entity, box.Bag.GroupLocations ) )
                {
                    checkinDataUpdated = true;
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

            // Cache invalidation — workflow-trigger registry. Mirrors
            // WebForms GroupDetail.ascx.cs:1427-1430.
            if ( triggersUpdated )
            {
                GroupMemberWorkflowTriggerService.RemoveCachedTriggers();
            }

            // Cache invalidation — KioskDevice cache. Mirrors WebForms
            // GroupDetail.ascx.cs:1432-1436. Fires when any
            // GroupLocation change occurred AND the group type takes
            // attendance (otherwise the kiosk cache is irrelevant).
            // Re-resolve the group-type cache from the entity since
            // checkinDataUpdated only matters when the active group
            // type actually feeds the check-in kiosk surface.
            if ( checkinDataUpdated )
            {
                var groupTypeCacheForKiosk = GetGroupTypeCache( entity );
                if ( groupTypeCacheForKiosk?.TakesAttendance == true )
                {
                    Rock.CheckIn.KioskDevice.Clear();
                }
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
                // Reuse the existing inline schedule when present so
                // Schedule.Id stays stable across Weekly ↔ Custom switches
                // (Q6.2-b); otherwise create a new one with Name =
                // string.Empty (the inline marker per webforms/07).
                //
                // The "inline" qualifier matters: when the group was
                // previously on a Named schedule (entity.Schedule != null
                // but Name is set), reusing that entity would overwrite a
                // shared Named schedule's iCal / Weekly fields, corrupting
                // every other group that points to it. WebForms avoids
                // this with the hfUniqueScheduleId hidden field
                // (GroupDetail.ascx.cs:1203-1212) which is only set for
                // inline schedules; the equivalent here is the
                // Name == string.Empty check on the loaded navigation.
                if ( entity.Schedule == null || !string.IsNullOrEmpty( entity.Schedule.Name ) )
                {
                    entity.Schedule = new Schedule
                    {
                        Name = string.Empty
                    };
                    entity.ScheduleId = null;
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
                InheritedMemberAttributes = new List<GroupMemberInheritedAttributeBag>(),
                InheritedGroupRequirements = new List<InheritedGroupRequirementBag>()
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
            bag.AllowMultipleLocations = groupType.AllowMultipleLocations;

            // Phase 6 — Section 4 Stack 2 cascade payload. The Location
            // Type dropdown sources its options from the group type's
            // configured LocationTypeValues. The MapStyleValueGuid is
            // surfaced here so the LocationPicker inside the modal can
            // honor the admin's configured map style without an extra
            // round-trip per GroupType change.
            bag.LocationTypeValueOptions = ( groupType.LocationTypeValues ?? new List<DefinedValueCache>() )
                .OrderBy( dv => dv.Order )
                .ThenBy( dv => dv.Value )
                .ToListItemBagList();

            bag.MapStyleValueGuid = GetAttributeValue( AttributeKey.MapStyle ).AsGuidOrNull();

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
            bag.RsvpSystemCommunicationOptions = BuildRsvpSystemCommunicationOptions();
            bag.RsvpReminderSystemCommunication = BuildSystemCommunicationListItem( groupType.RSVPReminderSystemCommunicationId );

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
                    .ToListItemBagList();
            }

            // Inherited group-member attribute definitions (Section 6 read-only grid).
            // Walks the group type's InheritedGroupTypeId chain server-side and
            // emits each inherited member attribute with the immediate
            // ancestor's name + URL for the link cell. Mirrors the WebForms
            // BindInheritedAttributes walk at GroupDetail.ascx.cs:3157-3203 and
            // the canonical GroupTypeDetail.GetInheritedAttributes pattern.
            bag.InheritedMemberAttributes = BuildInheritedMemberAttributes( groupType );

            // Section 6 / 7 / 9 / 10 panel-level visibility gates and
            // dropdown sources (Phase 5).
            bag.AllowSpecificGroupMemberAttributes = groupType.AllowSpecificGroupMemberAttributes;
            bag.EnableSpecificGroupRequirements = groupType.EnableSpecificGroupRequirements;
            bag.AllowGroupSync = groupType.AllowGroupSync;
            bag.AllowSpecificGroupMemberWorkflows = groupType.AllowSpecificGroupMemberWorkflows;

            bag.InheritedGroupRequirements = BuildInheritedGroupRequirements( groupType );

            bag.GroupRequirementTypeOptions = BuildGroupRequirementTypeOptions();
            bag.GroupRoleOptions = BuildGroupRoleOptions( groupType );
            bag.GroupAttributeOptions = BuildGroupDateAttributeOptions( groupType );
            bag.SystemCommunicationOptions = BuildSystemCommunicationOptions();

            return bag;
        }

        /// <summary>
        /// Walks the GroupType inheritance chain starting from the
        /// supplied group type (inclusive) and collects every
        /// group-member attribute definition reachable. From this
        /// Group's perspective every GroupType-level attribute is
        /// inherited - the Group entity does not define them itself;
        /// only attributes qualified by GroupId belong to the Group.
        /// Mirrors WebForms BindInheritedAttributes at
        /// RockWeb/Blocks/Groups/GroupDetail.ascx.cs:3157 which is
        /// invoked with group.GroupTypeId (not its parent). Each entry
        /// carries the source ancestor's name and a navigation URL
        /// resolved via the <see cref="EntityType.LinkUrlLavaTemplate"/>
        /// for GroupType (falling back to the current page's IdKey-based
        /// URL when no template is configured). Guards against circular
        /// inheritance with a visited-id set.
        /// </summary>
        /// <param name="groupType">The active group type whose chain is walked (inclusive).</param>
        private List<GroupMemberInheritedAttributeBag> BuildInheritedMemberAttributes( GroupTypeCache groupType )
        {
            var inheritedAttributes = new List<GroupMemberInheritedAttributeBag>();

            if ( groupType == null )
            {
                return inheritedAttributes;
            }

            var attributeService = new AttributeService( RockContext );
            var groupMemberEntityTypeId = new GroupMember().TypeId;
            var groupTypeService = new GroupTypeService( RockContext );

            // Resolve the URL template once per cascade.
            var urlTemplate = EntityTypeCache.Get( typeof( GroupType ) )?.LinkUrlLavaTemplate;

            var visitedGroupTypeIds = new HashSet<int>();
            var inheritedGroupType = groupTypeService.Get( groupType.Id );

            if ( inheritedGroupType == null )
            {
                return inheritedAttributes;
            }

            do
            {
                if ( !visitedGroupTypeIds.Add( inheritedGroupType.Id ) )
                {
                    break;
                }

                var qualifierValue = inheritedGroupType.Id.ToString();

                string inheritedFromUrl = null;
                if ( urlTemplate.IsNotNullOrWhiteSpace() )
                {
                    inheritedFromUrl = urlTemplate.ResolveMergeFields( new Dictionary<string, object>
                    {
                        ["Entity"] = inheritedGroupType
                    } );
                    inheritedFromUrl = this.RequestContext.ResolveRockUrl( inheritedFromUrl );
                }

                // Fallback intentionally left null when no
                // LinkUrlLavaTemplate is configured. The current page
                // here is GroupDetail (not GroupTypeDetail), so
                // GetCurrentPageUrl with a GroupTypeId param would just
                // reload the same group with a stray query string -
                // worse than no link. The Vue grid renders the
                // inherited-from name as plain text in this case.

                inheritedAttributes.AddRange(
                    attributeService.GetByEntityTypeId( groupMemberEntityTypeId, false )
                        .AsNoTracking()
                        .Where( a =>
                            a.EntityTypeQualifierColumn.Equals( "GroupTypeId", StringComparison.OrdinalIgnoreCase ) &&
                            a.EntityTypeQualifierValue.Equals( qualifierValue ) )
                        .OrderBy( a => a.Order )
                        .ThenBy( a => a.Name )
                        .Select( a => new GroupMemberInheritedAttributeBag
                        {
                            Name = a.Name,
                            Description = a.Description,
                            Key = a.Key,
                            Guid = a.Guid,
                            InheritedFromGroupTypeName = inheritedGroupType.Name,
                            InheritedFromGroupTypeUrl = inheritedFromUrl
                        } )
                        .ToList() );

                inheritedGroupType = inheritedGroupType.InheritedGroupTypeId.HasValue
                    ? groupTypeService.Get( inheritedGroupType.InheritedGroupTypeId.Value )
                    : null;
            } while ( inheritedGroupType != null );

            return inheritedAttributes;
        }

        /// <summary>
        /// Walks the GroupType inheritance chain starting from the
        /// supplied group type (inclusive) and collects every group
        /// requirement defined at any ancestor GroupType level. From
        /// this Group's perspective every GroupType-level requirement
        /// is inherited - the Group entity contributes its own
        /// requirements via the editable Section 7 stack. Mirrors the
        /// inheritance pattern <see cref="BuildInheritedMemberAttributes"/>
        /// uses for Section 6 inherited attributes. Each entry carries
        /// the source ancestor's name and a navigation URL resolved via
        /// the <see cref="EntityType.LinkUrlLavaTemplate"/> for
        /// GroupType. Guards against circular inheritance with a
        /// visited-id set.
        /// </summary>
        /// <param name="groupType">The group type to start from.</param>
        private List<InheritedGroupRequirementBag> BuildInheritedGroupRequirements( GroupTypeCache groupType )
        {
            var inheritedRequirements = new List<InheritedGroupRequirementBag>();

            if ( groupType == null )
            {
                return inheritedRequirements;
            }

            var groupRequirementService = new GroupRequirementService( RockContext );
            var groupTypeService = new GroupTypeService( RockContext );

            // Resolve the URL template once per cascade.
            var urlTemplate = EntityTypeCache.Get( typeof( Model.GroupType ) )?.LinkUrlLavaTemplate;

            var visitedGroupTypeIds = new HashSet<int>();
            var inheritedGroupType = groupTypeService.Get( groupType.Id );

            if ( inheritedGroupType == null )
            {
                return inheritedRequirements;
            }

            do
            {
                if ( !visitedGroupTypeIds.Add( inheritedGroupType.Id ) )
                {
                    break;
                }

                string inheritedFromUrl = null;
                if ( urlTemplate.IsNotNullOrWhiteSpace() )
                {
                    inheritedFromUrl = urlTemplate.ResolveMergeFields( new Dictionary<string, object>
                    {
                        ["Entity"] = inheritedGroupType
                    } );
                    inheritedFromUrl = this.RequestContext.ResolveRockUrl( inheritedFromUrl );
                }

                var ancestorId = inheritedGroupType.Id;
                var ancestorName = inheritedGroupType.Name;

                inheritedRequirements.AddRange(
                    groupRequirementService.Queryable()
                        .AsNoTracking()
                        .Include( r => r.GroupRequirementType )
                        .Include( r => r.GroupRole )
                        .Where( r => r.GroupTypeId.HasValue && r.GroupTypeId.Value == ancestorId )
                        .ToList()
                        .Select( r => new InheritedGroupRequirementBag
                        {
                            Guid = r.Guid,
                            Name = r.GroupRequirementType?.Name ?? string.Empty,
                            GroupRoleName = r.GroupRole?.Name ?? string.Empty,
                            AppliesToAgeClassification = r.AppliesToAgeClassification,
                            InheritedFromGroupTypeName = ancestorName,
                            InheritedFromGroupTypeUrl = inheritedFromUrl
                        } )
                        .OrderBy( r => r.Name )
                        .ToList() );

                inheritedGroupType = inheritedGroupType.InheritedGroupTypeId.HasValue
                    ? groupTypeService.Get( inheritedGroupType.InheritedGroupTypeId.Value )
                    : null;
            } while ( inheritedGroupType != null );

            return inheritedRequirements;
        }

        /// <summary>
        /// Builds the GroupRequirementType dropdown options for the
        /// Section 7 modal. Each entry carries the type's
        /// <see cref="Model.DueDateType"/> so the modal's Due Date
        /// conditional well reacts to the selection without a server
        /// round-trip. Mirrors the canonical pattern at
        /// <c>GroupTypeDetail.cs:122-130</c>.
        /// </summary>
        private List<GroupRequirementTypeBag> BuildGroupRequirementTypeOptions()
        {
            return new GroupRequirementTypeService( RockContext ).Queryable()
                .OrderBy( req => req.Name )
                .Select( req => new GroupRequirementTypeBag
                {
                    Text = req.Name,
                    Value = req.Guid.ToString(),
                    DueDateType = req.DueDateType
                } )
                .ToList();
        }

        /// <summary>
        /// Builds the Group Role dropdown options for the
        /// Section 7 / 9 / 10 modals. Sourced from
        /// <c>GroupType.Roles</c> on the immediate group type — no
        /// inheritance walk, matching WebForms parity at
        /// <c>GroupDetail.ascx.cs:4337-4344</c>. Returns the role's
        /// <see cref="GroupTypeRole.Guid"/> as the value to align with
        /// how WebForms persists the role in <c>TypeQualifier</c>.
        /// </summary>
        /// <param name="groupType">The active group type cache.</param>
        private List<ListItemBag> BuildGroupRoleOptions( GroupTypeCache groupType )
        {
            if ( groupType?.Roles == null )
            {
                return new List<ListItemBag>();
            }

            return groupType.Roles
                .OrderBy( r => r.Order )
                .ThenBy( r => r.Name )
                .ToListItemBagList();
        }

        /// <summary>
        /// Builds the date-typed group-attribute dropdown options for
        /// the Section 7 modal's Due Date Attribute conditional well.
        /// Walks the inherited attribute chain (the GroupType has no
        /// inherent date attributes; only the inherited member-attribute
        /// list contains them). Mirrors WebForms parity at
        /// <c>GroupDetail.ascx.cs:3979-3984</c> using the same
        /// Date / DateTime field-type filter at
        /// <c>GroupDetail.ascx.cs:278-289</c>.
        /// </summary>
        /// <param name="groupType">The active group type cache.</param>
        private List<ListItemBag> BuildGroupDateAttributeOptions( GroupTypeCache groupType )
        {
            var results = new List<ListItemBag>();

            if ( groupType == null )
            {
                return results;
            }

            var dateFieldTypeIds = new List<int>();
            var dateFieldTypeId = FieldTypeCache.GetId( Rock.SystemGuid.FieldType.DATE.AsGuid() );
            var dateTimeFieldTypeId = FieldTypeCache.GetId( Rock.SystemGuid.FieldType.DATE_TIME.AsGuid() );

            if ( dateFieldTypeId.HasValue )
            {
                dateFieldTypeIds.Add( dateFieldTypeId.Value );
            }
            if ( dateTimeFieldTypeId.HasValue )
            {
                dateFieldTypeIds.Add( dateTimeFieldTypeId.Value );
            }

            if ( !dateFieldTypeIds.Any() )
            {
                return results;
            }

            // Walk the GroupType inheritance chain to gather every
            // group-scope attribute (qualifier column "GroupTypeId"
            // on the Group entity type). The Group entity itself does
            // not own attributes here; every Group attribute is
            // inherited from one of the ancestors. Matches WebForms
            // BindInheritedAttributes which collects attributes for the
            // DueDate dropdown.
            var attributeService = new AttributeService( RockContext );
            var groupEntityTypeId = new Model.Group().TypeId;
            var groupTypeService = new GroupTypeService( RockContext );

            var visitedGroupTypeIds = new HashSet<int>();
            var inheritedGroupType = groupTypeService.Get( groupType.Id );

            if ( inheritedGroupType == null )
            {
                return results;
            }

            do
            {
                if ( !visitedGroupTypeIds.Add( inheritedGroupType.Id ) )
                {
                    break;
                }

                var qualifierValue = inheritedGroupType.Id.ToString();

                results.AddRange(
                    attributeService.GetByEntityTypeId( groupEntityTypeId, false )
                        .Where( a =>
                            a.EntityTypeQualifierColumn.Equals( "GroupTypeId", StringComparison.OrdinalIgnoreCase ) &&
                            a.EntityTypeQualifierValue.Equals( qualifierValue ) &&
                            dateFieldTypeIds.Contains( a.FieldTypeId ) )
                        .OrderBy( a => a.Order )
                        .ThenBy( a => a.Name )
                        .Select( a => new ListItemBag
                        {
                            Value = a.Guid.ToString(),
                            Text = a.Name
                        } )
                        .ToList() );

                inheritedGroupType = inheritedGroupType.InheritedGroupTypeId.HasValue
                    ? groupTypeService.Get( inheritedGroupType.InheritedGroupTypeId.Value )
                    : null;
            } while ( inheritedGroupType != null );

            return results;
        }

        /// <summary>
        /// Builds the SystemCommunication dropdown options for the
        /// Section 9 Welcome / Exit dropdowns. Returns every
        /// SystemCommunication regardless of category, mirroring
        /// WebForms <c>CreateSystemCommunicationDropDownLists</c> at
        /// <c>GroupDetail.ascx.cs:4291-4313</c> (which feeds both the
        /// Sync modal and the RSVP reminder dropdown).
        /// </summary>
        private List<ListItemBag> BuildSystemCommunicationOptions()
        {
            return new SystemCommunicationService( RockContext ).Queryable()
                .OrderBy( c => c.Title )
                .Select( c => new ListItemBag
                {
                    Value = c.Guid.ToString(),
                    Text = c.Title
                } )
                .ToList();
        }

        /// <summary>
        /// Builds a <see cref="ListItemBag"/> for a single SystemCommunication
        /// by Id. Returns null when the id is null or the row is missing.
        /// Used to hydrate the per-group RSVP reminder selection and the
        /// group-type's pinned readonly label.
        /// </summary>
        /// <param name="systemCommunicationId">The system communication Id.</param>
        private ListItemBag BuildSystemCommunicationListItem( int? systemCommunicationId )
        {
            if ( !systemCommunicationId.HasValue )
            {
                return null;
            }

            return new SystemCommunicationService( RockContext )
                .Queryable()
                .Where( c => c.Id == systemCommunicationId.Value )
                .Select( c => new ListItemBag { Value = c.Guid.ToString(), Text = c.Title } )
                .FirstOrDefault();
        }

        /// <summary>
        /// Builds the SystemCommunication dropdown options filtered to the
        /// RSVP Confirmation category. Mirrors WebForms
        /// <c>CreateSystemCommunicationDropDownLists</c> at
        /// <c>GroupDetail.ascx.cs:3036</c>, which populates the
        /// <c>ddlRsvpReminderSystemCommunication</c> dropdown from that
        /// single category.
        /// </summary>
        private List<ListItemBag> BuildRsvpSystemCommunicationOptions()
        {
            var rsvpCategoryGuid = Rock.SystemGuid.Category.SYSTEM_COMMUNICATION_RSVP_CONFIRMATION.AsGuid();

            return new SystemCommunicationService( RockContext ).Queryable()
                .Where( c => c.Category.Guid == rsvpCategoryGuid )
                .OrderBy( c => c.Title )
                .Select( c => new ListItemBag { Value = c.Guid.ToString(), Text = c.Title } )
                .ToList();
        }

        /// <summary>
        /// Loads the per-group requirements for the supplied entity.
        /// Returns an empty list for new groups. Mirrors WebForms
        /// <c>ShowEditDetails</c> hydration at
        /// <c>GroupDetail.ascx.cs:2066</c> filtered to
        /// <c>GroupId.HasValue</c>.
        /// </summary>
        /// <param name="entity">The group entity.</param>
        private List<GroupRequirementBag> LoadGroupRequirements( Model.Group entity )
        {
            if ( entity == null || entity.Id == 0 )
            {
                return new List<GroupRequirementBag>();
            }

            return new GroupRequirementService( RockContext ).Queryable()
                .AsNoTracking()
                .Include( r => r.GroupRequirementType )
                .Include( r => r.GroupRole )
                .Include( r => r.AppliesToDataView )
                .Include( r => r.DueDateAttribute )
                .Where( r => r.GroupId.HasValue && r.GroupId.Value == entity.Id )
                .ToList()
                .Select( r => new GroupRequirementBag
                {
                    Guid = r.Guid,
                    GroupRequirementType = r.GroupRequirementType.ToListItemBag(),
                    Role = r.GroupRole != null ? new ListItemBag { Value = r.GroupRole.Guid.ToString(), Text = r.GroupRole.Name } : null,
                    AppliesToAgeClassification = r.AppliesToAgeClassification,
                    AppliesToDataView = r.AppliesToDataView.ToListItemBag(),
                    AllowLeadersToOverride = r.AllowLeadersToOverride,
                    MustMeetRequirementToAddMember = r.MustMeetRequirementToAddMember,
                    DueDateType = r.GroupRequirementType?.DueDateType ?? Model.DueDateType.Immediate,
                    DueDateStaticDate = r.DueDateStaticDate?.ToRockDateTimeOffset(),
                    DueDateAttribute = r.DueDateAttribute != null
                        ? new ListItemBag { Value = r.DueDateAttribute.Guid.ToString(), Text = r.DueDateAttribute.Name }
                        : null
                } )
                .OrderBy( r => r.GroupRequirementType?.Text )
                .ToList();
        }

        /// <summary>
        /// Loads the per-group sync rows for the supplied entity.
        /// Returns an empty list for new groups. Mirrors the WebForms
        /// <c>GroupSyncState</c> hydration at
        /// <c>GroupDetail.ascx.cs:2006-2017</c>.
        /// </summary>
        /// <param name="entity">The group entity.</param>
        private List<GroupSyncBag> LoadGroupSyncs( Model.Group entity )
        {
            if ( entity == null || entity.Id == 0 )
            {
                return new List<GroupSyncBag>();
            }

            return new GroupSyncService( RockContext ).Queryable()
                .AsNoTracking()
                .Include( s => s.GroupTypeRole )
                .Include( s => s.SyncDataView )
                .Include( s => s.WelcomeSystemCommunication )
                .Include( s => s.ExitSystemCommunication )
                .Where( s => s.GroupId == entity.Id )
                .ToList()
                .Select( s => new GroupSyncBag
                {
                    Guid = s.Guid,
                    GroupTypeRole = s.GroupTypeRole != null
                        ? new ListItemBag { Value = s.GroupTypeRole.Guid.ToString(), Text = s.GroupTypeRole.Name }
                        : null,
                    SyncDataView = s.SyncDataView.ToListItemBag(),
                    WelcomeSystemCommunication = s.WelcomeSystemCommunication != null
                        ? new ListItemBag { Value = s.WelcomeSystemCommunication.Guid.ToString(), Text = s.WelcomeSystemCommunication.Title }
                        : null,
                    ExitSystemCommunication = s.ExitSystemCommunication != null
                        ? new ListItemBag { Value = s.ExitSystemCommunication.Guid.ToString(), Text = s.ExitSystemCommunication.Title }
                        : null,
                    AddUserAccountsDuringSync = s.AddUserAccountsDuringSync,
                    ScheduleIntervalMinutes = s.ScheduleIntervalMinutes,
                    LastRefreshDateTime = s.LastRefreshDateTime?.ToRockDateTimeOffset()
                } )
                .ToList();
        }

        /// <summary>
        /// Loads the per-group member workflow triggers for the
        /// supplied entity. Parses the pipe-delimited 7-tuple
        /// <c>TypeQualifier</c> into typed bag fields. Mirrors the
        /// canonical pattern at <c>GroupTypeDetail.cs:1063-1152</c>.
        /// </summary>
        /// <param name="entity">The group entity.</param>
        private List<GroupMemberWorkflowTriggerBag> LoadGroupMemberWorkflowTriggers( Model.Group entity )
        {
            if ( entity == null || entity.Id == 0 )
            {
                return new List<GroupMemberWorkflowTriggerBag>();
            }

            var triggers = new GroupMemberWorkflowTriggerService( RockContext ).Queryable()
                .AsNoTracking()
                .Include( t => t.WorkflowType )
                .Where( t => t.GroupId.HasValue && t.GroupId.Value == entity.Id )
                .OrderBy( t => t.Name )
                .ToList();

            var bags = new List<GroupMemberWorkflowTriggerBag>( triggers.Count );

            foreach ( var t in triggers )
            {
                // {ToStatus}|{ToRoleGuid}|{FromStatus}|{FromRoleGuid}|{TriggerOnFirstAttendance}|{ShowNoteOnPlacement}|{RequireNoteOnPlacement}
                var parts = ( t.TypeQualifier ?? string.Empty ).Split( '|' );

                var bag = new GroupMemberWorkflowTriggerBag
                {
                    Guid = t.Guid,
                    Name = t.Name,
                    IsActive = t.IsActive,
                    WorkflowType = t.WorkflowType?.ToListItemBag(),
                    TriggerType = t.TriggerType
                };

                GroupMemberStatus? toStatus = parts.Length > 0
                    ? ( GroupMemberStatus? ) parts[0].AsIntegerOrNull()
                    : null;

                Guid? toRoleGuid = parts.Length > 1
                    ? parts[1].AsGuidOrNull()
                    : null;

                GroupMemberStatus? fromStatus = parts.Length > 2
                    ? ( GroupMemberStatus? ) parts[2].AsIntegerOrNull()
                    : null;

                Guid? fromRoleGuid = parts.Length > 3
                    ? parts[3].AsGuidOrNull()
                    : null;

                var triggerOnFirstAttendance = parts.Length > 4 && parts[4].AsBoolean();
                var showNoteOnPlacement = parts.Length > 5 && parts[5].AsBoolean();
                var requireNoteOnPlacement = parts.Length > 6 && parts[6].AsBoolean();

                switch ( t.TriggerType )
                {
                    case GroupMemberWorkflowTriggerType.MemberAddedToGroup:
                    case GroupMemberWorkflowTriggerType.MemberRemovedFromGroup:
                        /*
                             1/23/2026 - MSE

                             For these trigger types, the UI displays these qualifiers using the label "With Status/ With Role",
                             However, the persisted qualifier format actually stores these values in the "To" slots (part[0] and part[1]).
                        */
                        bag.ToStatus = toStatus;
                        bag.ToRoleGuid = toRoleGuid;
                        break;

                    case GroupMemberWorkflowTriggerType.MemberStatusChanged:
                        bag.FromStatus = fromStatus;
                        bag.ToStatus = toStatus;
                        break;

                    case GroupMemberWorkflowTriggerType.MemberRoleChanged:
                        bag.FromRoleGuid = fromRoleGuid;
                        bag.ToRoleGuid = toRoleGuid;
                        break;

                    case GroupMemberWorkflowTriggerType.MemberAttendedGroup:
                        bag.TriggerOnFirstAttendance = triggerOnFirstAttendance;
                        break;

                    case GroupMemberWorkflowTriggerType.MemberPlacedElsewhere:
                        bag.ShowNoteOnPlacement = showNoteOnPlacement;
                        bag.RequireNoteOnPlacement = requireNoteOnPlacement;
                        break;
                }

                bags.Add( bag );
            }

            return bags;
        }

        /// <summary>
        /// Serializes a workflow trigger bag back into the pipe-delimited
        /// 7-tuple <c>TypeQualifier</c> string. Mirrors the canonical
        /// pattern at <c>GroupTypeDetail.cs:1157</c>. The format is
        /// load-bearing: WebForms blocks consume the same shape.
        /// </summary>
        /// <param name="bag">The trigger bag to serialize.</param>
        private static string BuildGroupMemberWorkflowTriggerTypeQualifier( GroupMemberWorkflowTriggerBag bag )
        {
            // Format:
            // {ToStatus}|{ToRoleGuid}|{FromStatus}|{FromRoleGuid}|{TriggerOnFirstAttendance}|{ShowNoteOnPlacement}|{RequireNoteOnPlacement}
            // Even though the UI renders some trigger types as "With Status/Role of", the persisted qualifier format
            // stores values in the "to" slots (part[0] and part[1]).

            string toStatus = string.Empty;
            string toRoleGuid = string.Empty;
            string fromStatus = string.Empty;
            string fromRoleGuid = string.Empty;
            bool triggerOnFirstAttendance = false;
            bool showNoteOnPlacement = false;
            bool requireNoteOnPlacement = false;

            if ( bag != null )
            {
                switch ( bag.TriggerType )
                {
                    case GroupMemberWorkflowTriggerType.MemberAddedToGroup:
                    case GroupMemberWorkflowTriggerType.MemberRemovedFromGroup:
                        /*
                             5/11/2026 - MSE

                             For these trigger types, the UI displays these qualifiers using the label "With Status/ With Role",
                             However, the persisted qualifier format actually stores these values in the "To" slots (part[0] and part[1]).
                        */
                        toStatus = bag.ToStatus.HasValue ? ( ( int ) bag.ToStatus.Value ).ToString() : string.Empty;
                        toRoleGuid = bag.ToRoleGuid?.ToString() ?? string.Empty;
                        break;

                    case GroupMemberWorkflowTriggerType.MemberStatusChanged:
                        toStatus = bag.ToStatus.HasValue ? ( ( int ) bag.ToStatus.Value ).ToString() : string.Empty;
                        fromStatus = bag.FromStatus.HasValue ? ( ( int ) bag.FromStatus.Value ).ToString() : string.Empty;
                        break;

                    case GroupMemberWorkflowTriggerType.MemberRoleChanged:
                        toRoleGuid = bag.ToRoleGuid?.ToString() ?? string.Empty;
                        fromRoleGuid = bag.FromRoleGuid?.ToString() ?? string.Empty;
                        break;

                    case GroupMemberWorkflowTriggerType.MemberAttendedGroup:
                        triggerOnFirstAttendance = bag.TriggerOnFirstAttendance;
                        break;

                    case GroupMemberWorkflowTriggerType.MemberPlacedElsewhere:
                        showNoteOnPlacement = bag.ShowNoteOnPlacement;
                        requireNoteOnPlacement = bag.RequireNoteOnPlacement;
                        break;
                }
            }

            return string.Format(
                "{0}|{1}|{2}|{3}|{4}|{5}|{6}",
                toStatus,
                toRoleGuid,
                fromStatus,
                fromRoleGuid,
                triggerOnFirstAttendance,
                showNoteOnPlacement,
                requireNoteOnPlacement );
        }

        /// <summary>
        /// Checks whether the incoming workflow trigger bags differ
        /// from the persisted set (additions, deletions, or any field
        /// change). Used to gate the post-save
        /// <c>RemoveCachedTriggers()</c> invalidation. Mirrors the
        /// canonical pattern at <c>GroupTypeDetail.cs:1220-1254</c>.
        /// </summary>
        /// <param name="entity">The group entity.</param>
        /// <param name="incomingBags">The incoming trigger bag list.</param>
        private bool HaveGroupMemberWorkflowTriggersChanged( Model.Group entity, List<GroupMemberWorkflowTriggerBag> incomingBags )
        {
            if ( entity == null || entity.Id == 0 )
            {
                // New group: any trigger row is a change.
                return incomingBags.Any();
            }

            var existing = new GroupMemberWorkflowTriggerService( RockContext ).Queryable()
                .AsNoTracking()
                .Where( t => t.GroupId.HasValue && t.GroupId.Value == entity.Id )
                .ToList();

            // Deletions.
            if ( existing.Any( e => !incomingBags.Any( b => b.Guid == e.Guid ) ) )
            {
                return true;
            }

            // Additions / mutations.
            foreach ( var bag in incomingBags )
            {
                var match = existing.FirstOrDefault( e => e.Guid == bag.Guid );
                if ( match == null )
                {
                    return true;
                }

                if ( match.Name != bag.Name
                    || match.IsActive != bag.IsActive
                    || match.WorkflowTypeId != ( bag.WorkflowType?.GetEntityId<WorkflowType>( RockContext ) ?? 0 )
                    || match.TriggerType != bag.TriggerType
                    || match.TypeQualifier != BuildGroupMemberWorkflowTriggerTypeQualifier( bag ) )
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Persists the Section 7 group requirements list inside the
        /// Save block action's <c>WrapTransaction</c> step 4c. Mirrors
        /// the WebForms deferred-insert pattern at
        /// <c>GroupDetail.ascx.cs:845-886</c> and
        /// <c>GroupDetail.ascx.cs:1330-1334</c> but expressed via the
        /// <see cref="SyncRelatedEntities{TEntity, TBag, TKey}"/>
        /// helper since the group's Id is already assigned by the
        /// preceding SaveChanges in the transaction.
        /// </summary>
        /// <param name="entity">The group entity.</param>
        /// <param name="bags">The group requirement bags from the save payload.</param>
        private void SaveGroupRequirements( Model.Group entity, List<GroupRequirementBag> bags )
        {
            var service = new GroupRequirementService( RockContext );
            var bagList = ( bags ?? new List<GroupRequirementBag>() ).Where( b => b != null ).ToList();

            foreach ( var b in bagList.Where( b => b.Guid == Guid.Empty ) )
            {
                b.Guid = Guid.NewGuid();
            }

            SyncRelatedEntities(
                service,
                service.Queryable().Where( r => r.GroupId.HasValue && r.GroupId.Value == entity.Id ),
                bagList,
                existingKeySelector: r => r.Guid,
                incomingKeySelector: b => b.Guid,
                createNew: b => new GroupRequirement { Guid = b.Guid, GroupId = entity.Id },
                updateEntity: ( requirement, bag ) =>
                {
                    requirement.GroupId = entity.Id;
                    requirement.GroupRequirementTypeId = bag.GroupRequirementType?.GetEntityId<GroupRequirementType>( RockContext ) ?? 0;
                    requirement.GroupRoleId = bag.Role?.GetEntityId<GroupTypeRole>( RockContext );
                    requirement.MustMeetRequirementToAddMember = bag.MustMeetRequirementToAddMember;
                    requirement.AppliesToAgeClassification = bag.AppliesToAgeClassification;
                    requirement.AppliesToDataViewId = bag.AppliesToDataView?.GetEntityId<DataView>( RockContext );
                    requirement.AllowLeadersToOverride = bag.AllowLeadersToOverride;

                    requirement.DueDateStaticDate = null;
                    requirement.DueDateAttributeId = null;

                    if ( bag.DueDateType == Model.DueDateType.ConfiguredDate )
                    {
                        requirement.DueDateStaticDate = bag.DueDateStaticDate?.DateTime;
                    }
                    else if ( bag.DueDateType == Model.DueDateType.GroupAttribute )
                    {
                        requirement.DueDateAttributeId = bag.DueDateAttribute?.GetEntityId<Rock.Model.Attribute>( RockContext );
                    }
                } );
        }

        /// <summary>
        /// Persists the Section 9 group sync rows inside the Save block
        /// action's <c>WrapTransaction</c> step 4d. Mirrors WebForms
        /// <c>btnSave_Click</c> body at
        /// <c>GroupDetail.ascx.cs:861-867</c> (removal) and
        /// <c>GroupDetail.ascx.cs:1011-1022</c> (add/update).
        /// </summary>
        /// <param name="entity">The group entity.</param>
        /// <param name="bags">The group sync bags from the save payload.</param>
        private void SaveGroupSyncs( Model.Group entity, List<GroupSyncBag> bags )
        {
            var service = new GroupSyncService( RockContext );
            var bagList = ( bags ?? new List<GroupSyncBag>() ).Where( b => b != null ).ToList();

            foreach ( var b in bagList.Where( b => b.Guid == Guid.Empty ) )
            {
                b.Guid = Guid.NewGuid();
            }

            SyncRelatedEntities(
                service,
                service.Queryable().Where( s => s.GroupId == entity.Id ),
                bagList,
                existingKeySelector: s => s.Guid,
                incomingKeySelector: b => b.Guid,
                createNew: b => new GroupSync { Guid = b.Guid, GroupId = entity.Id },
                updateEntity: ( sync, bag ) =>
                {
                    sync.GroupId = entity.Id;
                    sync.GroupTypeRoleId = bag.GroupTypeRole?.GetEntityId<GroupTypeRole>( RockContext ) ?? 0;
                    sync.SyncDataViewId = bag.SyncDataView?.GetEntityId<DataView>( RockContext ) ?? 0;
                    sync.WelcomeSystemCommunicationId = bag.WelcomeSystemCommunication?.GetEntityId<SystemCommunication>( RockContext );
                    sync.ExitSystemCommunicationId = bag.ExitSystemCommunication?.GetEntityId<SystemCommunication>( RockContext );
                    sync.AddUserAccountsDuringSync = bag.AddUserAccountsDuringSync;
                    sync.ScheduleIntervalMinutes = bag.ScheduleIntervalMinutes;
                    // LastRefreshDateTime is owned by the sync job; do
                    // not overwrite from the UI bag.
                } );
        }

        /// <summary>
        /// Persists the Section 10 group member workflow triggers
        /// inside the Save block action's <c>WrapTransaction</c> step
        /// 4e. Returns true when any add / update / delete occurred so
        /// the post-transaction
        /// <c>GroupMemberWorkflowTriggerService.RemoveCachedTriggers()</c>
        /// invalidation fires. Mirrors WebForms parity at
        /// <c>GroupDetail.ascx.cs:853-858, 1024-1041, 1427-1430</c>.
        /// </summary>
        /// <param name="entity">The group entity.</param>
        /// <param name="bags">The trigger bags from the save payload.</param>
        private bool SaveGroupMemberWorkflowTriggers( Model.Group entity, List<GroupMemberWorkflowTriggerBag> bags )
        {
            var service = new GroupMemberWorkflowTriggerService( RockContext );
            var bagList = ( bags ?? new List<GroupMemberWorkflowTriggerBag>() ).Where( b => b != null ).ToList();

            foreach ( var b in bagList.Where( b => b.Guid == Guid.Empty ) )
            {
                b.Guid = Guid.NewGuid();
            }

            if ( !HaveGroupMemberWorkflowTriggersChanged( entity, bagList ) )
            {
                return false;
            }

            SyncRelatedEntities(
                service,
                service.Queryable().Where( t => t.GroupId.HasValue && t.GroupId.Value == entity.Id ),
                bagList,
                existingKeySelector: t => t.Guid,
                incomingKeySelector: b => b.Guid,
                createNew: b => new GroupMemberWorkflowTrigger { Guid = b.Guid, GroupId = entity.Id },
                updateEntity: ( trigger, bag ) =>
                {
                    trigger.GroupId = entity.Id;
                    trigger.Name = bag.Name;
                    trigger.IsActive = bag.IsActive;
                    trigger.WorkflowTypeId = bag.WorkflowType?.GetEntityId<WorkflowType>( RockContext ) ?? 0;
                    trigger.TriggerType = bag.TriggerType;
                    trigger.TypeQualifier = BuildGroupMemberWorkflowTriggerTypeQualifier( bag );
                } );

            return true;
        }

        /// <summary>
        /// Loads the editable Group Locations for the supplied entity.
        /// Active schedules only per Q6.3 - inactive schedules attached
        /// to a GroupLocation never round-trip through the bag; the
        /// save flow re-merges them server-side. Mirrors WebForms
        /// <c>GroupLocationsState</c> hydration at
        /// <c>GroupDetail.ascx.cs:2003</c>.
        /// </summary>
        /// <param name="entity">The group entity.</param>
        private List<GroupLocationStateBag> LoadGroupLocations( Model.Group entity )
        {
            if ( entity == null || entity.Id == 0 )
            {
                return new List<GroupLocationStateBag>();
            }

            var groupLocations = new GroupLocationService( RockContext ).Queryable()
                .AsNoTracking()
                .Include( gl => gl.Location )
                .Include( gl => gl.Schedules )
                .Include( gl => gl.GroupLocationTypeValue )
                .Include( gl => gl.GroupLocationScheduleConfigs.Select( c => c.Schedule ) )
                .Include( gl => gl.GroupMemberPersonAlias )
                .Where( gl => gl.GroupId == entity.Id )
                .OrderBy( gl => gl.Order )
                .ThenBy( gl => gl.Id )
                .ToList();

            return groupLocations.Select( BuildGroupLocationStateBag ).ToList();
        }

        /// <summary>
        /// Builds a single <see cref="GroupLocationStateBag"/> from an
        /// existing <see cref="GroupLocation"/>. The
        /// <see cref="GroupLocationStateBag.SelectedLocation"/> +
        /// <see cref="GroupLocationStateBag.SelectedLocationMode"/>
        /// discriminator is rebuilt from the underlying Location's geo
        /// state and the GroupMember alias presence per the same four-
        /// mode classification used by
        /// <see cref="BuildMeetingLocationBag(GroupLocation, bool, string)"/>.
        /// Active schedules only per Q6.3.
        /// </summary>
        private GroupLocationStateBag BuildGroupLocationStateBag( GroupLocation gl )
        {
            var location = gl.Location;

            // Mode classification, in priority order:
            //   1. GroupMember (the row was added via the Member tab; the
            //      PersonAlias FK is the discriminator).
            //   2. Polygon / Point (geo data, unambiguous).
            //   3. Named (the Location has a Name - the user picked a
            //      pre-existing Location tree node). Named takes priority
            //      over Address because Named Locations can carry an
            //      attached address (e.g., a Building room with a street),
            //      and the user's original choice was the Name.
            //   4. Address (no Name; user typed an address).
            //   5. None (defensive fallback for rows with a null Location;
            //      should not happen in practice but keeps the hydration
            //      total).
            GroupLocationPickerMode mode;
            object selectedLocation;
            if ( gl.GroupMemberPersonAliasId.HasValue && location != null )
            {
                mode = GroupLocationPickerMode.GroupMember;
                selectedLocation = new ListItemBag
                {
                    Value = location.Guid.ToString(),
                    Text = location.ToString( false )
                };
            }
            else if ( location?.GeoFence != null )
            {
                mode = GroupLocationPickerMode.Polygon;
                selectedLocation = location.GeoFence.AsText();
            }
            else if ( location?.GeoPoint != null )
            {
                mode = GroupLocationPickerMode.Point;
                selectedLocation = location.GeoPoint.AsText();
            }
            else if ( location != null && location.Name.IsNotNullOrWhiteSpace() )
            {
                mode = GroupLocationPickerMode.Named;
                selectedLocation = new ListItemBag
                {
                    Value = location.Guid.ToString(),
                    Text = location.ToString( false )
                };
            }
            else if ( location != null && ( location.Street1.IsNotNullOrWhiteSpace() || location.City.IsNotNullOrWhiteSpace() ) )
            {
                mode = GroupLocationPickerMode.Address;
                selectedLocation = new AddressControlBag
                {
                    Street1 = location.Street1,
                    Street2 = location.Street2,
                    City = location.City,
                    State = location.State,
                    Locality = location.County,
                    PostalCode = location.PostalCode,
                    Country = location.Country
                };
            }
            else
            {
                mode = GroupLocationPickerMode.None;
                selectedLocation = null;
            }

            var bag = new GroupLocationStateBag
            {
                Guid = gl.Guid,
                LocationName = location?.ToString( false ) ?? string.Empty,
                LocationDescription = null,
                SelectedLocationMode = mode,
                SelectedLocation = selectedLocation,
                GroupLocationTypeValueGuid = gl.GroupLocationTypeValue?.Guid,
                GroupLocationTypeValueName = gl.GroupLocationTypeValue?.Value,
                GroupMemberPersonAliasGuid = gl.GroupMemberPersonAlias?.Guid,
                Order = gl.Order,
                Schedules = ( gl.Schedules ?? new List<Schedule>() )
                    .Where( s => s.IsActive )
                    .OrderBy( s => s.Order )
                    .ThenBy( s => s.Id )
                    .Select( s => new ListItemBag
                    {
                        Value = s.Guid.ToString(),
                        Text = s.Name.IsNotNullOrWhiteSpace() ? s.Name : s.FriendlyScheduleText
                    } )
                    .ToList(),
                ScheduleConfigs = ( gl.GroupLocationScheduleConfigs ?? new List<GroupLocationScheduleConfig>() )
                    .Where( c => c.Schedule != null )
                    .Select( c => new GroupLocationScheduleConfigBag
                    {
                        ScheduleGuid = c.Schedule.Guid,
                        MinimumCapacity = c.MinimumCapacity,
                        DesiredCapacity = c.DesiredCapacity,
                        MaximumCapacity = c.MaximumCapacity
                    } )
                    .ToList()
            };

            return bag;
        }

        /// <summary>
        /// Builds the Location modal's Member-tab dropdown source per
        /// Q6.11 - one row per (Group Member, Family, Mapped Address)
        /// tuple, excluding Previous-type addresses. Mirrors WebForms
        /// parity at <c>GroupDetail.ascx.cs:3525-3549</c>.
        /// </summary>
        /// <param name="entity">The group entity.</param>
        private List<FamilyMemberLocationBag> BuildFamilyMemberLocationOptions( Model.Group entity )
        {
            if ( entity == null || entity.Id == 0 )
            {
                return new List<FamilyMemberLocationBag>();
            }

            var groupMemberService = new GroupMemberService( RockContext );
            var personService = new PersonService( RockContext );

            var previousLocationTypeGuid = Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_PREVIOUS.AsGuid();

            var options = new List<FamilyMemberLocationBag>();
            var seen = new HashSet<(Guid LocationGuid, Guid PersonAliasGuid)>();

            foreach ( var member in groupMemberService.GetByGroupId( entity.Id ) )
            {
                if ( member.Person == null )
                {
                    continue;
                }

                var primaryAlias = member.Person.PrimaryAlias;
                if ( primaryAlias == null )
                {
                    continue;
                }

                foreach ( var family in personService.GetFamilies( member.PersonId ) )
                {
                    foreach ( var familyGroupLocation in family.GroupLocations
                        .Where( l => l.IsMappedLocation
                            && l.GroupLocationTypeValue != null
                            && l.GroupLocationTypeValue.Guid != previousLocationTypeGuid
                            && l.Location != null ) )
                    {
                        var key = (familyGroupLocation.Location.Guid, primaryAlias.Guid);
                        if ( !seen.Add( key ) )
                        {
                            continue;
                        }

                        options.Add( new FamilyMemberLocationBag
                        {
                            LocationGuid = familyGroupLocation.Location.Guid,
                            PersonAliasGuid = primaryAlias.Guid,
                            Text = $"{member.Person.FullName} {familyGroupLocation.GroupLocationTypeValue.Value} ({familyGroupLocation.Location})"
                        } );
                    }
                }
            }

            return options;
        }

        /// <summary>
        /// Resolves the LocationPicker emit + discriminator pair on a
        /// <see cref="GroupLocationStateBag"/> to a tracked
        /// <see cref="Location"/> entity per Q6.9. Routes by
        /// <see cref="GroupLocationStateBag.SelectedLocationMode"/>:
        /// <list type="bullet">
        ///   <item><c>Named</c> / <c>GroupMember</c>: <see cref="ListItemBag"/> Guid lookup.</item>
        ///   <item><c>Address</c>: <see cref="LocationService.Get(string, string, string, string, string, string, string, string)"/>.</item>
        ///   <item><c>Point</c> / <c>Polygon</c>: <see cref="LocationService.GetByGeoPoint"/> / <see cref="LocationService.GetByGeoFence"/> with WKT.</item>
        /// </list>
        /// Returns null when the bag is missing required data. Persists
        /// any newly-created Location through the LocationService so
        /// downstream <c>SaveChanges</c> assigns its <c>Id</c>.
        /// </summary>
        private Location ResolveLocationFromBag( GroupLocationStateBag bag, LocationService locationService )
        {
            if ( bag?.SelectedLocation == null )
            {
                return null;
            }

            switch ( bag.SelectedLocationMode )
            {
                case GroupLocationPickerMode.Named:
                case GroupLocationPickerMode.GroupMember:
                {
                    // The Vue side wraps Named and GroupMember picks as a
                    // ListItemBag (value = Location.Guid). Round-trip the
                    // raw payload through ToJson so we accept whatever
                    // shape System.Text.Json or Newtonsoft handed us.
                    var listItem = bag.SelectedLocation.ToJson().FromJsonOrNull<ListItemBag>();
                    var locationGuid = listItem?.Value.AsGuidOrNull();
                    return locationGuid.HasValue ? locationService.Get( locationGuid.Value ) : null;
                }

                case GroupLocationPickerMode.Address:
                {
                    var address = bag.SelectedLocation.ToJson().FromJsonOrNull<AddressControlBag>();
                    if ( address == null )
                    {
                        return null;
                    }

                    if ( address.Street1.IsNullOrWhiteSpace() && address.City.IsNullOrWhiteSpace() )
                    {
                        return null;
                    }

                    return locationService.Get(
                        address.Street1,
                        address.Street2,
                        address.City,
                        address.State,
                        address.PostalCode,
                        address.Country,
                        verifyLocation: false );
                }

                case GroupLocationPickerMode.Point:
                {
                    var wkt = bag.SelectedLocation as string ?? bag.SelectedLocation.ToString();
                    if ( wkt.IsNullOrWhiteSpace() )
                    {
                        return null;
                    }
                    System.Data.Entity.Spatial.DbGeography point;
                    try
                    {
                        point = System.Data.Entity.Spatial.DbGeography.FromText( wkt );
                    }
                    catch
                    {
                        // The picker emits invalid WKT for empty / partial
                        // selections; treat it as a no-op rather than
                        // throwing into the save flow.
                        return null;
                    }
                    return point != null ? locationService.GetByGeoPoint( point ) : null;
                }

                case GroupLocationPickerMode.Polygon:
                {
                    var wkt = bag.SelectedLocation as string ?? bag.SelectedLocation.ToString();
                    if ( wkt.IsNullOrWhiteSpace() )
                    {
                        return null;
                    }
                    System.Data.Entity.Spatial.DbGeography fence;
                    try
                    {
                        fence = System.Data.Entity.Spatial.DbGeography.PolygonFromText( wkt, System.Data.Entity.Spatial.DbGeography.DefaultCoordinateSystemId );
                    }
                    catch
                    {
                        return null;
                    }
                    return fence != null ? locationService.GetByGeoFence( fence ) : null;
                }

                default:
                    return null;
            }
        }

        /// <summary>
        /// Persists the Section 4 Stack 2 group locations list inside
        /// the Save block action's <c>WrapTransaction</c> step 4f. Per
        /// Q6.5 lock - encapsulates the SyncRelatedEntities pattern +
        /// <c>GroupLocationScheduleConfig</c> diff
        /// (existing/modified/new/deleted) +
        /// <c>GroupMemberAssignment</c> cleanup + inactive-schedule
        /// preservation (Q6.3) + Location resolution from picker bag
        /// (Q6.9) as a single atomic unit. Mirrors WebForms parity at
        /// <c>GroupDetail.ascx.cs:810-991</c>. Returns true when any
        /// add / update / delete occurred so the caller knows to flush
        /// <c>KioskDevice</c> post-transaction.
        /// </summary>
        /// <param name="entity">The group entity.</param>
        /// <param name="bags">The location bags from the save payload.</param>
        private bool SaveGroupLocations( Model.Group entity, List<GroupLocationStateBag> bags )
        {
            if ( entity == null || entity.Id == 0 )
            {
                // Locations cannot be saved against a group whose Id is
                // still 0 - the FK requires a persisted parent. The
                // step-3 SaveChanges in the outer transaction has
                // already assigned the Id by the time this method runs.
                return false;
            }

            var bagList = ( bags ?? new List<GroupLocationStateBag>() ).Where( b => b != null ).ToList();
            foreach ( var b in bagList.Where( b => b.Guid == Guid.Empty ) )
            {
                b.Guid = Guid.NewGuid();
            }

            var groupLocationService = new GroupLocationService( RockContext );
            var groupMemberAssignmentService = new GroupMemberAssignmentService( RockContext );
            var locationService = new LocationService( RockContext );
            var scheduleService = new ScheduleService( RockContext );
            var personAliasService = new PersonAliasService( RockContext );

            // Reload the persisted GroupLocations with their navigations so
            // we can diff against the incoming bags. The entity's
            // GroupLocations navigation may or may not be hydrated depending
            // on the code path that loaded the entity.
            var existingLocations = groupLocationService.Queryable()
                .Include( gl => gl.Schedules )
                .Include( gl => gl.GroupLocationScheduleConfigs )
                .Where( gl => gl.GroupId == entity.Id )
                .ToList();

            var incomingGuids = bagList.Select( b => b.Guid ).ToHashSet();
            var changed = false;

            // 1. Delete removed locations - cascade-clean their
            // GroupLocationScheduleConfigs and any
            // GroupMemberAssignments that reference (scheduleId,
            // locationId, groupId).
            foreach ( var existing in existingLocations.Where( gl => !incomingGuids.Contains( gl.Guid ) ).ToList() )
            {
                foreach ( var cfg in existing.GroupLocationScheduleConfigs.ToList() )
                {
                    existing.GroupLocationScheduleConfigs.Remove( cfg );
                }

                foreach ( var schedule in existing.Schedules )
                {
                    var assignmentsToDelete = groupMemberAssignmentService.Queryable()
                        .Where( a => a.ScheduleId == schedule.Id
                            && a.LocationId == existing.LocationId
                            && a.GroupMember.GroupId == existing.GroupId )
                        .ToList();
                    groupMemberAssignmentService.DeleteRange( assignmentsToDelete );
                }

                groupLocationService.Delete( existing );
                changed = true;
            }

            // Compute the next Order value for any new rows (Q6.14 lock:
            // Order is assigned once on Add, never via UI reorder).
            var nextOrder = existingLocations.Any()
                ? existingLocations.Max( gl => gl.Order ) + 1
                : 0;

            // 2. Upsert each incoming location.
            foreach ( var bag in bagList )
            {
                var existing = existingLocations.FirstOrDefault( gl => gl.Guid == bag.Guid );
                var isNewLocation = existing == null;
                int? oldLocationId = isNewLocation ? null : ( int? ) existing.LocationId;

                if ( isNewLocation )
                {
                    existing = new GroupLocation
                    {
                        Guid = bag.Guid,
                        GroupId = entity.Id,
                        Order = nextOrder++
                    };
                    groupLocationService.Add( existing );
                    existingLocations.Add( existing );
                }

                // Resolve the LocationPicker bag (Q6.9). Skip the
                // GroupLocation entirely if the resolver returns null:
                // the picker hasn't selected a valid location and there
                // is nothing to persist.
                var resolvedLocation = ResolveLocationFromBag( bag, locationService );
                if ( resolvedLocation == null )
                {
                    if ( isNewLocation )
                    {
                        groupLocationService.Delete( existing );
                        existingLocations.Remove( existing );
                    }
                    continue;
                }

                // Newly-created Location entities have Id == 0 until
                // the next SaveChanges. SaveChanges will run at the end
                // of the outer transaction, so the FK reference works
                // either way (EF assigns the Id on flush).
                if ( !isNewLocation && resolvedLocation.Id != existing.LocationId )
                {
                    // The user swapped the Location attached to this row.
                    // Cascade-clean any GroupMemberAssignments that
                    // referenced the previous (scheduleId, oldLocationId,
                    // groupId) tuple.
                    //
                    // We iterate the currently-attached schedules rather
                    // than the bag's incoming list — intentionally tighter
                    // than WebForms parity at GroupDetail.ascx.cs:909-916.
                    // WebForms only cleans assignments for schedules that
                    // survive the swap, leaving assignments for removed
                    // schedules orphaned at the old location. Using the
                    // currently-attached set catches both in one pass.
                    foreach ( var schedule in existing.Schedules )
                    {
                        var assignmentsToDelete = groupMemberAssignmentService.Queryable()
                            .Where( a => a.ScheduleId == schedule.Id
                                && a.LocationId == oldLocationId.Value
                                && a.GroupMember.GroupId == existing.GroupId )
                            .ToList();
                        groupMemberAssignmentService.DeleteRange( assignmentsToDelete );
                    }
                }

                // Set the navigation reference; EF resolves LocationId on
                // flush. When resolvedLocation is newly created its
                // Location.Id == 0 and assigning LocationId directly
                // would fail the FK constraint. EF's relationship fix-up
                // copies the Id from the principal once it is generated.
                existing.Location = resolvedLocation;
                if ( resolvedLocation.Id != 0 )
                {
                    existing.LocationId = resolvedLocation.Id;
                }

                // Capture the LocationId that downstream cleanup queries
                // should target. For a swap to a brand-new Location row
                // (Id == 0 until EF flushes), no GroupMemberAssignment
                // can yet reference the unflushed Id, so we treat it as
                // null and short-circuit the schedule-removal cleanup
                // below. This avoids relying on the now-stale
                // existing.LocationId when the conditional scalar
                // assignment above was skipped.
                var cleanupLocationId = resolvedLocation.Id != 0
                    ? ( int? ) resolvedLocation.Id
                    : null;

                existing.GroupLocationTypeValueId = bag.GroupLocationTypeValueGuid.HasValue
                    ? DefinedValueCache.GetId( bag.GroupLocationTypeValueGuid.Value )
                    : null;

                // Resolve the Member-tab PersonAlias if present (Q6.13).
                existing.GroupMemberPersonAliasId = bag.GroupMemberPersonAliasGuid.HasValue
                    ? personAliasService.GetSelect( bag.GroupMemberPersonAliasGuid.Value, pa => ( int? ) pa.Id )
                    : null;

                // 3. Schedule reconciliation (Q6.3) — union active
                // (bag) + inactive (DB) so previously-attached but now-
                // inactive schedules are NOT silently dropped.
                var incomingActiveScheduleGuids = ( bag.Schedules ?? new List<ListItemBag>() )
                    .Select( s => s.Value.AsGuidOrNull() )
                    .Where( g => g.HasValue )
                    .Select( g => g.Value )
                    .ToHashSet();

                // Detach any currently-attached schedules that the bag
                // does NOT include and are active (inactive ones survive).
                var deletedScheduleIds = new List<int>();
                foreach ( var attached in existing.Schedules.ToList() )
                {
                    if ( !attached.IsActive )
                    {
                        // Inactive schedules survive bag round-trip per Q6.3.
                        continue;
                    }

                    if ( !incomingActiveScheduleGuids.Contains( attached.Guid ) )
                    {
                        deletedScheduleIds.Add( attached.Id );
                        existing.Schedules.Remove( attached );
                    }
                }

                // Attach any active schedules the bag introduces that
                // are not already attached.
                var currentlyAttachedGuids = existing.Schedules.Select( s => s.Guid ).ToHashSet();
                foreach ( var newGuid in incomingActiveScheduleGuids.Where( g => !currentlyAttachedGuids.Contains( g ) ) )
                {
                    var schedule = scheduleService.Get( newGuid );
                    if ( schedule != null )
                    {
                        existing.Schedules.Add( schedule );
                    }
                }

                // 4. GroupLocationScheduleConfig diff
                // (existing/modified/new/deleted). Mirrors WebForms
                // parity at GroupDetail.ascx.cs:942-988.
                var incomingConfigs = bag.ScheduleConfigs ?? new List<GroupLocationScheduleConfigBag>();
                var incomingByGuid = incomingConfigs
                    .GroupBy( c => c.ScheduleGuid )
                    .ToDictionary( g => g.Key, g => g.First() );

                // Resolve Schedule.Guid -> Schedule.Id for the configs.
                var attachedScheduleByGuid = existing.Schedules.ToDictionary( s => s.Guid, s => s );

                // Drop existing configs not present in the incoming list
                // (covers schedule removals + capacity-row removals).
                foreach ( var cfg in existing.GroupLocationScheduleConfigs.ToList() )
                {
                    var cfgScheduleGuid = cfg.Schedule?.Guid
                        ?? attachedScheduleByGuid.FirstOrDefault( kv => kv.Value.Id == cfg.ScheduleId ).Key;

                    if ( cfgScheduleGuid == Guid.Empty || !incomingByGuid.ContainsKey( cfgScheduleGuid ) )
                    {
                        existing.GroupLocationScheduleConfigs.Remove( cfg );
                    }
                }

                // Upsert each incoming config.
                foreach ( var incoming in incomingConfigs )
                {
                    if ( !attachedScheduleByGuid.TryGetValue( incoming.ScheduleGuid, out var schedule ) )
                    {
                        // Capacity row references a schedule that is no
                        // longer attached; skip it (the schedule was
                        // removed in the same edit pass).
                        continue;
                    }

                    var existingCfg = existing.GroupLocationScheduleConfigs
                        .FirstOrDefault( c => c.ScheduleId == schedule.Id );
                    if ( existingCfg == null )
                    {
                        existing.GroupLocationScheduleConfigs.Add( new GroupLocationScheduleConfig
                        {
                            ScheduleId = schedule.Id,
                            MinimumCapacity = incoming.MinimumCapacity,
                            DesiredCapacity = incoming.DesiredCapacity,
                            MaximumCapacity = incoming.MaximumCapacity
                        } );
                    }
                    else
                    {
                        existingCfg.MinimumCapacity = incoming.MinimumCapacity;
                        existingCfg.DesiredCapacity = incoming.DesiredCapacity;
                        existingCfg.MaximumCapacity = incoming.MaximumCapacity;
                    }
                }

                // 5. GroupMemberAssignment cleanup for schedules removed
                // from this location (deletedScheduleIds collected above).
                // Mirrors WebForms parity at GroupDetail.ascx.cs:823-836.
                // Skipped when cleanupLocationId is null (the swap
                // landed on a not-yet-flushed Location row, so no
                // assignments can yet reference it).
                if ( cleanupLocationId.HasValue )
                {
                    foreach ( var deletedScheduleId in deletedScheduleIds )
                    {
                        var assignmentsToDelete = groupMemberAssignmentService.Queryable()
                            .Where( a => a.ScheduleId == deletedScheduleId
                                && a.LocationId == cleanupLocationId.Value
                                && a.GroupMember.GroupId == existing.GroupId )
                            .ToList();
                        groupMemberAssignmentService.DeleteRange( assignmentsToDelete );
                    }
                }

                changed = true;
            }

            return changed;
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
